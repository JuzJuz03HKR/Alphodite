using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Battle;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //BattleScreen : the turn based fight (Alpha 7, steps 2 to 4 of docs/ALPHA7_DESIGN.md).
    //The rules live in Battle/TurnBattle.cs. This page only shows them and reads the baton.
    //   CHOOSE  a musician is up : click an enemy to attack it, or pick ATTACK or a skill (1, 2, 3).
    //           SPACE plays the conductor's SIGNATURE once its gauge is full (step 5, costs no turn)
    //   TARGET  a skill waits for its enemy or musician to be clicked (right click goes back)
    //   SWIPE   the notes of the attack, the skill or the enemy's attack, one after another
    //   PAUSE   a short breath after a turn, so the result can be read
    //PRACTICE (step 9) : new BattleScreen(true) is the tutorial's practice fight. No run is needed and
    //nothing is saved. A card at the top teaches one thing at a time : ATTACK, PARRY, a skill, the SIGNATURE.
    //   OVER    the banner, then the result page
    //Layout (docs/research/ALPHA7_battle_ui.md) : one hit point in the middle so the eye never
    //has to look for it, the band on the left, TACET on the right, the turn order along the top
    //and the commands with the shared BREATH along the bottom. While a swipe is wanted the stage
    //dims and the pointer becomes the baton. The ring, the notes and the baton never shake.
    public class BattleScreen : GameScreen
    {
        private enum View { Choose, Target, Swipe, Pause, Over }
        private enum SwipeFor { Attack, Skill, Parry, Signature }

        //Layout
        private const float HitX = 640f;
        private const float HitY = 300f;
        private const float NoteGrow = 1.6f;      // a note on the hit point, this much bigger than in the duel
        private const float RingStart = 160f;     // the ring starts this wide
        private const float RingMark = 52f;       // and meets the note here
        private const int PartyTop = 250;
        private const int PartyW = 100;
        private const int PartyH = 170;
        private const int EnemyTop = 190;
        private static readonly int[] partyX = { 160, 300, 440 };     // middle of each musician
        private static readonly Rectangle[] commandBox =
        {
            new Rectangle(300, 600, 200, 60),
            new Rectangle(520, 600, 200, 60),
            new Rectangle(740, 600, 200, 60),
        };
        private static readonly string[] commandKey = { "1", "2", "3" };
        private static readonly Rectangle signatureBox = new Rectangle(960, 652, 280, 56);

        private RunState run;
        private TurnBattle battle;
        private bool practice;        // PRACTICE : the tutorial's fight, see Practice Load
        private View view = View.Pause;
        private float viewTime;       // seconds in the current view
        private float clock;          // seconds since the notes began
        private float time;           // always running, for glows
        private float hitStop;
        private float shake;
        private int perfects;

        private GestureReader gesture = new GestureReader();
        private Baton baton = new Baton();
        private DuelEffects effects = new DuelEffects();

        //Boxes : where each fighter stands, worked out once in Load
        private Rectangle[] partyBox;
        private Rectangle[] enemyBox;

        //Choose : the skills of the musician who is up, and what is under the pointer
        private Skill[] commandSkill = new Skill[3];       // [0] is ATTACK (null)
        private Skill pending;                             // the skill waiting for a target, null for ATTACK
        private Fighter hover;
        private Skill hoverSkill;

        //Swipe : the notes being played, when each is due, and how each went
        private class QteNote
        {
            public NoteKind Kind;
            public Flick Way;
            public int Size;
            public int Shown;          // the size drawn (FALSE NOTES can show a wrong one)
            public float Due;
        }
        private List<QteNote> notes = new List<QteNote>();
        private NoteResult[] results = new NoteResult[0];
        private int next;
        private SwipeFor swipeFor;
        private Skill swipeSkill;
        private Fighter swipeTarget;

        //HOLD : after a held note's swipe, the button stays down and the baton still
        private bool holding;
        private float holdTime;
        private float holdStill;
        private Grade holdGrade;
        private bool holdSize;

        //ROLL : shakes counted while a roll note lasts
        private bool rolling;
        private int shakes;

        //Prepared Text : every word on this page is made in Load or Update, never in Draw
        private Dictionary<Fighter, string> shortName = new Dictionary<Fighter, string>();
        private Dictionary<Fighter, string> maxLabel = new Dictionary<Fighter, string>();
        private Dictionary<Fighter, int> popStack = new Dictionary<Fighter, int>();
        private string judgeWord = "";
        private float judgeLife;
        private string bannerText = "";
        private string signatureName = "";

        private const string HintChoose = "CLICK AN ENEMY TO ATTACK, OR PICK A SKILL";
        private const string HintEnemy = "CLICK AN ENEMY   /   RIGHT CLICK = BACK";
        private const string HintAlly = "CLICK A MUSICIAN   /   RIGHT CLICK = BACK";
        private const string HintSwing = "SWIPE AS THE RING TOUCHES THE NOTE";
        private const string HintParry = "SWIPE BACK AT EACH NOTE";
        private const string HintHold = "HOLD STILL";
        private const string HintRoll = "SHAKE!";
        private const string HintMute = "MUTE : NO SKILLS THIS TURN";

        //PRACTICE : what the card at the top says, and which of the four lessons are done
        private const string CoachAttack = "Your turn. Click the enemy, or press 1, to attack.";
        private const string CoachSwing = "Swing the way the arrow points. Stop as the ring meets the note.";
        private const string CoachParry = "Its turn. Swipe back at each note to block it. PERFECT blocks it all.";
        private const string CoachSkill = "Skills cost BREATH (bottom right). Try one : press 2 or 3.";
        private const string CoachSkillSwipe = "A skill is a few notes. Same idea, one after another.";
        private const string CoachSignature = "The diamonds are full. Press SPACE for the SIGNATURE.";
        private const string CoachSignatureSwipe = "Draw the 4/4 shape : DOWN, LEFT, RIGHT, UP. Any size.";
        private const string CoachWin = "That is all of it. Now win the fight.";
        private static readonly Rectangle coachBox = new Rectangle(240, 64, 800, 60);
        private bool[] learned = new bool[4];              // one per SwipeFor : ATTACK, SKILL, PARRY, SIGNATURE
        private int learnedCount;
        private string coachLine = CoachAttack;

        public BattleScreen(bool practice = false)
        {
            this.practice = practice;
        }

        //Can Pause : not in PRACTICE, where ESC leaves like on every tutorial page
        public override bool CanPause
        {
            get { return !practice; }
        }

        public override bool EntersWithWave
        {
            get { return true; }
        }

        public override void Load()
        {
            if (practice)
            {
                PracticeLoad();
                return;
            }
            run = Game.CurrentRun;

            //The Band : the first three on stage, in seat order
            List<Musician> band = new List<Musician>();
            for (int s = 0; s < run.Formation.Seated.Length && band.Count < 3; s++)
                if (run.Formation.Seated[s] != null) band.Add(run.Formation.Seated[s]);
            for (int i = 0; i < run.Roster.Count && band.Count == 0; i++) band.Add(run.Roster[i]);   // nobody seated : never happens in a real run

            //BAND LIFE : everybody walks in with the life they left the last fight with (step 6)
            int[] life = new int[band.Count];
            for (int i = 0; i < band.Count; i++) life[i] = run.LifeOf(band[i]);

            List<Enemy> enemies = new List<Enemy> { run.Battle.Enemy };
            battle = new TurnBattle(band, enemies, run.Floor, run.Rng, life, Archive.Level(Archive.Life) * BattleRules.ArchiveLifePerLevel);
            battle.GiveMotifs(run.Motifs);                                                   // the motifs the band carries
            battle.Maestro = run.Maestro;                                                    // MAESTRO
            battle.Kit = run.Conductor.Turn;                                                 // the conductor's PASSIVE and SIGNATURE

            //ARCHIVE : DEEP BREATH and WARM UP, bought between runs (step 7)
            battle.Breath = Math.Min(BattleRules.TurnBreathMax, battle.Breath + Archive.Level(Archive.Breath) * BattleRules.ArchiveBreathPerLevel);
            if (battle.Kit != TurnKit.None)
                battle.Signature = Math.Min(battle.SignatureNeed, Archive.Level(Archive.Signature) * BattleRules.ArchiveSignaturePerLevel);
            signatureName = run.Conductor.TurnSignatureName;
            Arrange();
            SoundBank.PlayMusic(RunFlow.FightMusic(run));
        }

        //Practice Load : PRACTICE. The first three musicians at full life, THE UNHEARING's SIGNATURE,
        //floor 1 and the soft PRACTICE enemy (EnemyList.Practice). No motifs and no ARCHIVE.
        private void PracticeLoad()
        {
            List<Musician> band = new List<Musician> { MusicianList.All[0], MusicianList.All[1], MusicianList.All[2] };
            battle = new TurnBattle(band, new List<Enemy> { EnemyList.Practice }, 1, new Random());
            battle.Kit = TurnKit.Beethoven;
            for (int i = 0; i < ConductorList.All.Length; i++)
                if (ConductorList.All[i].Turn == TurnKit.Beethoven) signatureName = ConductorList.All[i].TurnSignatureName;
            Arrange();
            SoundBank.PlayMusic(Music.Battle);
        }

        //Arrange : where everybody stands and the words on their plates, then a moment before the first turn
        private void Arrange()
        {
            partyBox = new Rectangle[battle.Party.Count];
            for (int i = 0; i < partyBox.Length; i++)
                partyBox[i] = new Rectangle(partyX[Math.Min(i, partyX.Length - 1)] - PartyW / 2, PartyTop, PartyW, PartyH);

            //Enemies : one stands big on the right, more share the space
            int count = battle.Enemies.Count;
            int w = count == 1 ? 180 : 140;
            int h = count == 1 ? 240 : 200;
            enemyBox = new Rectangle[count];
            for (int i = 0; i < count; i++)
            {
                float cx = count == 1 ? 1000f : 860f + i * (360f / Math.Max(1, count - 1));
                enemyBox[i] = new Rectangle((int)(cx - w / 2f), EnemyTop + (240 - h), w, h);
            }

            List<Fighter> all = new List<Fighter>(battle.Party);
            all.AddRange(battle.Enemies);
            for (int i = 0; i < all.Count; i++)
            {
                Fighter f = all[i];
                shortName[f] = ShortName(f.Name);
                maxLabel[f] = "/ " + NumberText.Get(f.MaxHp);
            }

            battle.Events.Clear();
            gesture.Clear();
            baton.Reset(Input.MousePos);
            Game.IsMouseVisible = true;

            //The first turn starts after a moment, so the page can be seen first
            view = View.Pause;
            viewTime = 0f;
        }

        //Short Name : what fits a plate of the turn order. "THE" goes, and a long name keeps its
        //first word : THE LULL -> LULL, WHITE NOISE -> WHITE, THE NAMELESS MASTER -> NAMELESS
        private static string ShortName(string name)
        {
            if (name.StartsWith("THE ")) name = name.Substring(4);
            if (name.Length <= 9) return name;
            int space = name.IndexOf(' ');
            return space > 0 ? name.Substring(0, space) : name.Substring(0, 9);
        }

        public override void Leave()
        {
            Game.IsMouseVisible = true;
        }

        public override void Paused()
        {
            Game.IsMouseVisible = true;
        }

        //Resumed : a hold cannot survive a pause, it ends with what was held so far
        public override void Resumed()
        {
            gesture.Clear();
            if (holding)
            {
                holding = false;
                Land(holdGrade, holdSize, holdStill / BattleRules.TurnHoldTime);
            }
            Game.IsMouseVisible = view != View.Swipe;
        }

        //Ring Time : how long a ring takes to close (MAESTRO is quicker)
        private float RingTime
        {
            get { return battle.Maestro ? BattleRules.MaestroTurnRingTime : BattleRules.TurnRingTime; }
        }

        //======================================================================================
        //Update
        //======================================================================================

        public override void Update(float dt)
        {
            time += dt;
            viewTime += dt;
            if (judgeLife > 0f) judgeLife -= dt;

            //Hit Stop : the band, TACET and the sparks freeze for a moment. The ring clock and the
            //baton never stop, so a swipe is never judged late because of it.
            if (hitStop > 0f) hitStop -= dt;
            else
            {
                effects.Update(dt);
                if (shake > 0f) shake = Math.Max(0f, shake - dt * 3.5f);
            }

            gesture.Update(dt, view == View.Swipe && Input.MouseDown());
            baton.Update(dt, gesture.Held);

            //PRACTICE : ESC leaves for the title, the card follows what is on screen
            if (practice)
            {
                if (Input.KeyPressed(Keys.Escape) && view != View.Over)
                {
                    SoundBank.Play(Sfx.UiBack);
                    Game.Screens.Change(new TitleScreen());
                    view = View.Pause;
                    viewTime = -999f;
                    return;
                }
                UpdateCoach();
            }

            if (view == View.Choose) UpdateChoose();
            else if (view == View.Target) UpdateTarget();
            else if (view == View.Swipe) UpdateSwipe(dt);
            else if (view == View.Pause && viewTime >= BattleRules.TurnPause) NextTurn();
            else if (view == View.Over && viewTime >= BattleRules.TurnEndTime) Finish();
        }

        //Next Turn : after the pause, the fight ends or the next one is up
        private void NextTurn()
        {
            if (battle.Step == TurnStep.Won || battle.Step == TurnStep.Lost)
            {
                view = View.Over;
                viewTime = 0f;
                bannerText = battle.Step == TurnStep.Won ? "THE SILENCE BREAKS" : "THE SILENCE WINS";
                if (practice) bannerText = battle.Step == TurnStep.Won ? "PRACTICE CLEAR" : "TRY AGAIN";
                Game.IsMouseVisible = true;
                return;
            }

            if (battle.Step == TurnStep.PlayerTurn)
            {
                Musician m = battle.Current.Musician;
                commandSkill[1] = SkillList.First(m);
                commandSkill[2] = SkillList.Second(m);
                if (practice) PracticeHelp();
                view = View.Choose;
                viewTime = 0f;
                Game.IsMouseVisible = true;
            }
            else StartParry();
        }

        //Choose : click an enemy to attack it, or a command (1 attack, 2 and 3 the skills)
        private void UpdateChoose()
        {
            hover = EnemyUnderPointer();
            hoverSkill = null;
            for (int k = 1; k < 3; k++)
                if (Input.MouseOver(commandBox[k])) hoverSkill = commandSkill[k];

            if (hover != null && Input.MouseClicked()) { StartAttack(hover); return; }

            //SIGNATURE : SPACE, or a click on the gauge
            if (battle.CanSignature && (Input.KeyPressed(Keys.Space) || Input.ClickedOn(signatureBox)))
            {
                StartSignature();
                return;
            }

            if (Input.ClickedOn(commandBox[0]) || Input.KeyPressed(Keys.D1) || Input.KeyPressed(Keys.NumPad1))
            {
                if (StandingEnemies() == 1) StartAttack(FirstStanding());
                else Pick(null);
                return;
            }
            if (Input.ClickedOn(commandBox[1]) || Input.KeyPressed(Keys.D2) || Input.KeyPressed(Keys.NumPad2)) Pick(commandSkill[1]);
            else if (Input.ClickedOn(commandBox[2]) || Input.KeyPressed(Keys.D3) || Input.KeyPressed(Keys.NumPad3)) Pick(commandSkill[2]);
        }

        //Pick : a skill (or ATTACK, null) is chosen. Skills that need no choice start at once.
        private void Pick(Skill s)
        {
            if (s == null)
            {
                pending = null;
                view = View.Target;
                return;
            }
            if (!battle.CanUse(s)) return;

            if (s.Target == SkillTarget.Self) StartSkill(s, battle.Current);
            else if (s.Target == SkillTarget.AllEnemies || s.Target == SkillTarget.AllAllies) StartSkill(s, null);
            else if (s.Target == SkillTarget.Enemy && StandingEnemies() == 1) StartSkill(s, FirstStanding());
            else
            {
                pending = s;
                view = View.Target;
            }
        }

        //Target : click the enemy (or the musician) the skill is for, right click to go back
        private void UpdateTarget()
        {
            hoverSkill = pending;
            if (Input.MouseRightClicked())
            {
                view = View.Choose;
                return;
            }

            bool forAlly = pending != null && pending.Target == SkillTarget.Ally;
            hover = forAlly ? MusicianUnderPointer() : EnemyUnderPointer();
            if (hover == null || !Input.MouseClicked()) return;

            if (pending == null) StartAttack(hover);
            else StartSkill(pending, hover);
        }

        private Fighter EnemyUnderPointer()
        {
            for (int i = 0; i < battle.Enemies.Count; i++)
                if (battle.CanTarget(battle.Enemies[i]) && Input.MouseOver(enemyBox[i])) return battle.Enemies[i];
            return null;
        }

        private Fighter MusicianUnderPointer()
        {
            for (int i = 0; i < battle.Party.Count; i++)
                if (!battle.Party[i].Down && Input.MouseOver(partyBox[i])) return battle.Party[i];
            return null;
        }

        private int StandingEnemies()
        {
            int count = 0;
            for (int i = 0; i < battle.Enemies.Count; i++)
                if (!battle.Enemies[i].Down) count++;
            return count;
        }

        private Fighter FirstStanding()
        {
            for (int i = 0; i < battle.Enemies.Count; i++)
                if (battle.CanTarget(battle.Enemies[i])) return battle.Enemies[i];
            return null;
        }

        //======================================================================================
        //Swipe : the notes of an attack, a skill or an enemy attack
        //======================================================================================

        //Attack : one note, the musician's own PART (TurnBattle ATTACK PART)
        private void StartAttack(Fighter target)
        {
            if (target == null) return;
            Musician m = battle.Current.Musician;
            notes.Clear();
            AddNote(NoteKind.Swipe, TurnBattle.AttackWay(m), TurnBattle.AttackSize(m));
            BeginSwipe(SwipeFor.Attack, null, target);
        }

        //Skill : the notes written on the skill (Data/Skill.cs)
        private void StartSkill(Skill s, Fighter target)
        {
            notes.Clear();
            for (int i = 0; i < s.Notes.Length; i++) AddNote(s.Notes[i].Kind, s.Notes[i].Way, s.Notes[i].Size);
            BeginSwipe(SwipeFor.Skill, s, target);
        }

        //Signature : the 4/4 shape, any size (Size -1)
        private void StartSignature()
        {
            notes.Clear();
            for (int i = 0; i < TurnBattle.SignatureShape.Length; i++) AddNote(NoteKind.Swipe, TurnBattle.SignatureShape[i], -1);
            BeginSwipe(SwipeFor.Signature, null, null);
        }

        //Parry : the enemy's notes fly in one by one
        private void StartParry()
        {
            Fighter enemy = battle.Current;
            notes.Clear();
            for (int i = 0; i < enemy.IntentNotes.Length; i++)
            {
                AddNote(NoteKind.Swipe, enemy.IntentWays[i], enemy.IntentSizes[i]);
                if (i < enemy.IntentShown.Length) notes[i].Shown = enemy.IntentShown[i];      // FALSE NOTES
            }
            BeginSwipe(SwipeFor.Parry, null, enemy.IntentTarget);
        }

        private void AddNote(NoteKind kind, Flick way, int size)
        {
            QteNote n = new QteNote();
            n.Kind = kind;
            n.Way = way;
            n.Size = size;
            n.Shown = size;
            notes.Add(n);
        }

        //Swipe Begin : work out when each note is due. The first ring closes after RingTime, then a
        //note every TurnNoteGap (a spark sooner), and a HOLD or a ROLL pushes the next one back by its length.
        private void BeginSwipe(SwipeFor what, Skill s, Fighter target)
        {
            float due = RingTime;
            bool fast = what == SwipeFor.Parry && battle.Current != null && battle.Current.IntentFast;   // UNFINISHED
            float gap = BattleRules.TurnNoteGap * (fast ? BattleRules.TurnFastGap : 1f);
            for (int i = 0; i < notes.Count; i++)
            {
                if (i > 0)
                {
                    NoteKind before = notes[i - 1].Kind;
                    due += notes[i].Kind == NoteKind.Spark ? BattleRules.TurnSparkGap : gap;
                    if (before == NoteKind.Hold) due += BattleRules.TurnHoldTime;
                    if (before == NoteKind.Roll) due += BattleRules.TurnRollTime;
                }
                notes[i].Due = due;
            }

            results = new NoteResult[notes.Count];
            next = 0;
            swipeFor = what;
            swipeSkill = s;
            swipeTarget = target;
            holding = false;
            rolling = false;
            hover = null;

            view = View.Swipe;
            viewTime = 0f;
            clock = 0f;
            gesture.Clear();
            baton.Reset(Input.MousePos);
            Game.IsMouseVisible = false;
        }

        private void UpdateSwipe(float dt)
        {
            clock += dt;
            float now = clock - Settings.TimingOffset;

            if (next >= notes.Count)
            {
                FinishSwipe();
                return;
            }

            QteNote n = notes[next];
            if (holding)
            {
                UpdateHold(dt);
                return;
            }
            if (n.Kind == NoteKind.Roll)
            {
                UpdateRoll(n, now);
                return;
            }

            if (gesture.Read())
            {
                float offset = now - n.Due;
                if (offset < -BattleRules.EarlyTime) return;          // the hand getting ready, not the swipe

                bool spark = n.Kind == NoteKind.Spark;                // a spark takes any way and any size
                Grade grade = battle.Judge(offset, spark || gesture.Direction == n.Way);
                bool rightSize = spark || n.Size < 0 || Baton.SizeOf(gesture.Length) == n.Size;
                if (swipeFor == SwipeFor.Parry && grade == Grade.Perfect && !rightSize) grade = Grade.Good;   // the wrong size only half blocks

                baton.Whip(gesture.Direction);
                effects.SpawnSlash(gesture.From, gesture.To, grade == Grade.Perfect ? 1f : 0.55f);

                //HOLD : a held note that landed waits for the hold before it counts
                if (n.Kind == NoteKind.Hold && (grade == Grade.Perfect || grade == Grade.Good))
                {
                    holding = true;
                    holdTime = 0f;
                    holdStill = 0f;
                    holdGrade = grade;
                    holdSize = rightSize;
                    return;
                }
                Land(grade, rightSize, 1f);
            }
            else if (now > n.Due + battle.LateLimit)
            {
                Land(Grade.Hesitate, true, 0f);                        // no swipe at all
            }
        }

        //HOLD : the button down and the baton still. Letting go or the time running out ends it.
        private void UpdateHold(float dt)
        {
            gesture.Read();                                            // a stroke while holding is only the hand moving
            holdTime += dt;
            bool down = Input.MouseDown();
            if (down && baton.Velocity.Length() < BattleRules.TurnHoldStill) holdStill += dt;

            if (!down || holdTime >= BattleRules.TurnHoldTime)
            {
                holding = false;
                Land(holdGrade, holdSize, holdStill / BattleRules.TurnHoldTime);
            }
        }

        //ROLL : from the moment the ring closes, every stroke any way is one shake
        private void UpdateRoll(QteNote n, float now)
        {
            bool stroke = gesture.Read();
            if (now < n.Due) return;

            if (!rolling)
            {
                rolling = true;
                shakes = 0;
            }
            if (stroke)
            {
                shakes++;
                baton.Whip(gesture.Direction);
                effects.SpawnSparks(HitX, HitY, 2, 1f);
            }
            if (now >= n.Due + BattleRules.TurnRollTime)
            {
                rolling = false;
                Land(battle.RollGrade(shakes), true, 1f);
            }
        }

        //Land : one note is done. A parried note counts at once, the notes of an attack or a skill
        //are handed in together when the last one is done.
        private void Land(Grade grade, bool rightSize, float hold)
        {
            results[next] = new NoteResult(grade, rightSize, hold);
            effects.SpawnHit(HitX, HitY, grade);
            judgeLife = 1f;

            bool perfect = grade == Grade.Perfect && rightSize;
            if (perfect)
            {
                perfects++;
                effects.SpawnShatter(HitX, HitY, RingMark);
                hitStop = BattleRules.TurnHitStop;
            }

            if (swipeFor == SwipeFor.Parry)
            {
                battle.ParryNote(next, grade);
                judgeWord = grade == Grade.Perfect ? "PARRY" : (grade == Grade.Good ? "GOOD" : "MISS");
                if (grade == Grade.Perfect) effects.SpawnSparks(HitX, HitY, 10, 1f);
                ShowEvents();
            }
            else
            {
                bool onTime = grade == Grade.Perfect || grade == Grade.Good;
                if (onTime && !rightSize) judgeWord = "WRONG SIZE";
                else if (grade == Grade.Perfect) judgeWord = "PERFECT";
                else if (grade == Grade.Good) judgeWord = "GOOD";
                else judgeWord = "MISS";
            }
            next++;
        }

        private void FinishSwipe()
        {
            if (practice) Learn((int)swipeFor);                // SwipeFor is in the same order as learned
            if (swipeFor == SwipeFor.Attack) battle.Attack(swipeTarget, results[0].Grade, results[0].RightSize);
            else if (swipeFor == SwipeFor.Skill) battle.UseSkill(swipeSkill, swipeTarget, results);
            else if (swipeFor == SwipeFor.Signature)
            {
                //SIGNATURE : the conductor's name for it fills the screen for a moment
                battle.UseSignature(results);
                judgeWord = signatureName;
                judgeLife = 1.6f;
                effects.SpawnFlare(HitX, HitY, 1f);
                shake = 0.8f;
            }
            else battle.FinishEnemyTurn();

            ShowEvents();
            view = View.Pause;
            viewTime = 0f;
            Game.IsMouseVisible = true;
        }

        //Show Events : turn what the rules wrote down into numbers and words over the fighters.
        //Several on one fighter stack upward so they never cover each other.
        private void ShowEvents()
        {
            popStack.Clear();
            for (int i = 0; i < battle.Events.Count; i++)
            {
                TurnEvent e = battle.Events[i];
                if (e.Kind == TurnEventKind.Breath)
                {
                    effects.SpawnPop(1050f, 568f, "+" + NumberText.Get(e.Amount), Palette.Highlight, 0.9f);
                    continue;
                }
                if (e.Kind == TurnEventKind.Saved && e.Who != null)
                {
                    Rectangle saved = BoxOf(e.Who);
                    effects.SpawnPop(saved.Center.X, saved.Y - 60, "SECOND WIND", Palette.Highlight, 0.8f);
                    continue;
                }
                if (e.Kind == TurnEventKind.Steal)
                {
                    effects.SpawnPop(1050f, 568f, "-" + NumberText.Get(e.Amount) + " BREATH", Palette.PaperDim, 0.8f);
                    continue;
                }
                if (e.Kind == TurnEventKind.Ready)
                {
                    effects.SpawnPop(1100f, 640f, "SIGNATURE READY", Palette.Highlight, 0.7f);
                    continue;
                }
                if (e.Kind == TurnEventKind.Extra)
                {
                    effects.SpawnPop(HitX, HitY - 60f, "+" + NumberText.Get(e.Amount) + " TURNS", Palette.Highlight, 0.9f);
                    continue;
                }
                if (e.Who == null || e.Kind == TurnEventKind.Down) continue;

                Rectangle box = BoxOf(e.Who);
                int stack;
                popStack.TryGetValue(e.Who, out stack);
                popStack[e.Who] = stack + 1;
                float y = box.Y - 34 - stack * 26;
                float x = box.Center.X;

                if (e.Kind == TurnEventKind.Damage)
                {
                    effects.SpawnPop(x, y, NumberText.Get(e.Amount), e.Who.IsEnemy ? Palette.Highlight : Palette.Paper, 1.1f);
                    if (!e.Who.IsEnemy) shake = Math.Max(shake, 0.45f);
                }
                else if (e.Kind == TurnEventKind.Heal) effects.SpawnPop(x, y, "+" + NumberText.Get(e.Amount), Palette.Highlight, 1f);
                else if (e.Kind == TurnEventKind.Dissonance) effects.SpawnPop(x, y, "DISSONANCE", Palette.PaperDim, 0.6f);
                else if (e.Kind == TurnEventKind.Crescendo) effects.SpawnPop(x, y, "CRESCENDO", Palette.Paper, 0.6f);
                else if (e.Kind == TurnEventKind.Mute) effects.SpawnPop(x, y, "MUTE", Palette.PaperDim, 0.7f);
                else if (e.Kind == TurnEventKind.Skip) effects.SpawnPop(x, y, "OFF-BEAT", Palette.PaperDim, 0.7f);
                else if (e.Kind == TurnEventKind.Counter)
                {
                    effects.SpawnPop(x, y, "COUNTER", Palette.Highlight, 0.9f);
                    effects.SpawnFlare(x, box.Center.Y, 1f);
                }
                else if (e.Kind == TurnEventKind.Break)
                {
                    //OFF-BEAT : the biggest moment a player can make, so it gets the biggest feedback
                    effects.SpawnPop(x, y, "OFF-BEAT", Palette.Highlight, 1.2f);
                    effects.SpawnSparks(x, box.Center.Y, 30, 0f);
                    shake = 0.9f;
                    hitStop = BattleRules.HitStopTime;
                }
            }
            battle.Events.Clear();
        }

        private Rectangle BoxOf(Fighter f)
        {
            return f.IsEnemy ? enemyBox[f.Slot] : partyBox[f.Slot];
        }

        //Fight Over : every musician keeps the life they end with (KNOCKED OUT gets up with 1), and the
        //old duel's record carries the result to the result page, which hands out the rewards.
        //Line is set so the rank reads the PERFECTs : a win is B, with 6 PERFECTs or more S.
        private void Finish()
        {
            //PRACTICE : a win goes back to the title, a loss tries again
            if (practice)
            {
                if (battle.Step == TurnStep.Won) Game.Screens.Change(new TitleScreen());
                else Game.Screens.Change(new BattleScreen(true));
                view = View.Pause;
                viewTime = -999f;
                return;
            }

            for (int i = 0; i < battle.Party.Count; i++)
                run.SetLife(battle.Party[i].Musician, battle.HpAfterBattle(battle.Party[i]));
            run.LastRounds = battle.Round;
            run.RefreshLabels();

            BattleState record = run.Battle;
            record.Finished = true;
            record.PlayerWon = battle.Step == TurnStep.Won;
            record.PerfectCount = perfects;
            record.BestCombo = battle.BestRun;
            record.Line = record.PlayerWon ? BattleRules.LineLimit : -BattleRules.LineLimit;
            Game.Screens.Change(new ResultScreen());
            view = View.Pause;
            viewTime = -999f;                     // nothing more happens while the page fades
        }

        //======================================================================================
        //PRACTICE : the tutorial's fight
        //======================================================================================

        //Learn : one of the four lessons is done (a swipe of that kind was finished)
        private void Learn(int lesson)
        {
            if (learned[lesson]) return;
            learned[lesson] = true;
            learnedCount++;
            SoundBank.Play(Sfx.ComboUp, 1f, 0.5f);
        }

        //Practice Help : enough BREATH for the skill lesson, then a full gauge for the SIGNATURE lesson
        private void PracticeHelp()
        {
            if (learned[(int)SwipeFor.Attack] && !learned[(int)SwipeFor.Skill])
                for (int k = 1; k < 3; k++) battle.Breath = Math.Max(battle.Breath, commandSkill[k].Cost);
            if (learned[(int)SwipeFor.Attack] && learned[(int)SwipeFor.Skill] && !learned[(int)SwipeFor.Signature])
                battle.Signature = battle.SignatureNeed;
        }

        //Update Coach : the card says what to do next. While a swipe runs it explains that swipe,
        //only until that kind is learned.
        private void UpdateCoach()
        {
            if (view == View.Swipe)
            {
                if (learned[(int)swipeFor]) return;
                if (swipeFor == SwipeFor.Parry) coachLine = CoachParry;
                else if (swipeFor == SwipeFor.Skill) coachLine = CoachSkillSwipe;
                else if (swipeFor == SwipeFor.Signature) coachLine = CoachSignatureSwipe;
                else coachLine = CoachSwing;
            }
            else if (view == View.Choose || view == View.Target)
            {
                if (!learned[(int)SwipeFor.Attack]) coachLine = CoachAttack;
                else if (!learned[(int)SwipeFor.Skill]) coachLine = CoachSkill;
                else if (!learned[(int)SwipeFor.Signature]) coachLine = CoachSignature;
                else coachLine = CoachWin;
            }
        }

        //Coach Card : PRACTICE, the lessons done as diamonds, the line, and how to leave
        private void DrawCoach(SpriteBatch sb)
        {
            Ui.Panel(sb, coachBox, 1f);
            Ui.Tag(sb, "PRACTICE", coachBox.X + 18, coachBox.Y + 10, true, 1f);
            Ui.Pips(sb, coachBox.X + 30, coachBox.Y + 44, learnedCount, learned.Length, 5f, 16f, 1f);
            Gfx.Text(sb, Game.StoryFont, coachLine, coachBox.X + 130, coachBox.Y + 16, Palette.Paper, TextSize.StorySmall);
            Ui.KeyChipRight(sb, "ESC", coachBox.Right - 14, coachBox.Y + 30, Palette.PaperDim);
        }

        //Picture Hook : for --shots, jump straight to the moment the picture is about
        public void ShowForPicture(string name)
        {
            while (battle.Step == TurnStep.EnemyTurn && name != "battleparry") battle.TakeEnemyAttack(null);

            if (name == "battleswing")
            {
                StartAttack(FirstStanding());
                clock = RingTime * 0.6f;
            }
            else if (name == "battleskill")
            {
                Skill s = SkillList.First(battle.Current.Musician);
                Fighter target = s.HitsEnemy ? FirstStanding() : battle.Current;
                StartSkill(s, target);
                clock = RingTime * 0.6f;
            }
            else if (name == "battlesig")
            {
                battle.Kit = TurnKit.Beethoven;
                signatureName = "LIGHTNING";
                battle.Signature = battle.SignatureNeed;
                StartSignature();
                clock = RingTime * 0.6f;
            }
            else if (name == "battleparry")
            {
                while (battle.Step == TurnStep.PlayerTurn) battle.Attack(FirstStanding(), Grade.Good, true);
                StartParry();
                clock = RingTime * 0.75f;
            }
            else
            {
                if (name == "battlestatus")
                {
                    battle.Enemies[0].Dissonance = 3;
                    battle.Enemies[0].OffBeat = 60;
                    battle.Party[0].Crescendo = 2;
                    if (battle.Party.Count > 1) battle.Party[1].Mute = 1;
                    if (battle.Party.Count > 2) battle.Party[2].Guard = true;
                }
                if (name == "practiceskill" || name == "practicesig") Learn((int)SwipeFor.Attack);     // PRACTICE
                if (name == "practicesig") Learn((int)SwipeFor.Skill);
                battle.Events.Clear();
                NextTurn();
            }
        }

        //======================================================================================
        //Draw
        //======================================================================================

        public override void Draw(SpriteBatch sb)
        {
            bool swiping = view == View.Swipe;

            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageDeep);
            Gfx.Rect(sb, 0, 436, TacetGame.ScreenW, TacetGame.ScreenH - 436, Palette.Panel);
            Gfx.Rect(sb, 0, 436, TacetGame.ScreenW, 2, Palette.LineGrey * 0.35f);

            //World Shake : only the fighters and their sparks move, see DuelScreen for the same trick
            //ADVANCED PART : the SpriteBatch is begun again with a transform that offsets everything in it
            Matrix world = Matrix.Identity;
            if (shake > 0f)
            {
                float amount = shake * shake * 10f;
                world = Matrix.CreateTranslation((float)Math.Sin(time * 91f) * amount, (float)Math.Cos(time * 73f) * amount, 0f);
            }
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, world);

            CharacterArt.BeginPixels(sb, world);
            for (int i = 0; i < battle.Party.Count; i++)
            {
                Fighter f = battle.Party[i];
                CharacterArt.DrawStill(sb, f.Musician, PartyRect(i), Palette.Paper, f.Down ? 0.25f : 1f);
            }
            CharacterArt.EndPixels(sb, world);

            DrawEnemies(sb);
            effects.DrawSparks(sb);

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);

            DrawPartyPlates(sb);
            DrawEnemyPlates(sb);
            DrawTurnOrder(sb);
            if (!swiping && view != View.Over) DrawIntent(sb);

            //Focus : the stage dims while a swipe is wanted
            if (swiping)
            {
                Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.Void * 0.5f);
                DrawHitPoint(sb);
                if (swipeFor == SwipeFor.Parry) DrawParryNotes(sb);
                else DrawPlayNotes(sb);
            }

            effects.DrawShards(sb);
            effects.DrawHits(sb);
            effects.DrawSlashes(sb);
            effects.DrawFlares(sb);
            effects.DrawPops(sb, Game.Font);

            if (judgeLife > 0f)
                Gfx.TextSpacedCentered(sb, Game.BigFont, judgeWord, HitX, HitY + 96f, Palette.Highlight * Math.Min(1f, judgeLife * 2f), TextSize.Subtitle, 3f);

            DrawBottom(sb);
            if (practice) DrawCoach(sb);

            if (view == View.Over)
            {
                float a = Math.Min(1f, viewTime * 3f);
                Gfx.Rect(sb, 0, 300, TacetGame.ScreenW, 120, Palette.Void * (0.8f * a));
                Gfx.TextCentered(sb, Game.LogoFont, bannerText, TacetGame.ScreenW / 2f, 360, Palette.Highlight * a, TextSize.Banner);
            }

            if (swiping)
            {
                DrawRuler(sb);
                baton.Draw(sb, gesture);
            }
        }

        //Party Rect : the musician whose turn it is steps forward a little
        private Rectangle PartyRect(int i)
        {
            Rectangle r = partyBox[i];
            if (battle.Current == battle.Party[i] && (view == View.Choose || view == View.Target)) r.Y -= 14;
            return r;
        }

        private void DrawPartyPlates(SpriteBatch sb)
        {
            bool picking = view == View.Target && pending != null && pending.Target == SkillTarget.Ally;

            for (int i = 0; i < battle.Party.Count; i++)
            {
                Fighter f = battle.Party[i];
                Rectangle r = partyBox[i];
                float a = f.Down ? 0.4f : 1f;

                if (battle.Current == f && (view == View.Choose || view == View.Target))
                    Gfx.Rect(sb, r.X - 6, r.Bottom + 6, r.Width + 12, 3, Palette.Accent);
                if (picking && hover == f)
                    Gfx.RectOutline(sb, new Rectangle(r.X - 6, r.Y - 6, r.Width + 12, r.Height + 12), Palette.Accent, 2);

                Gfx.TextSpacedCentered(sb, Game.Font, f.Name, r.Center.X, r.Bottom + 14, Palette.Paper * a, TextSize.Body, 2f);
                DrawHp(sb, f, r.X, r.Bottom + 44, r.Width, a);

                //Statuses : a filled tag hurts the one who has it, an outlined tag helps them
                float y = r.Bottom + 80;
                if (f.Dissonance > 0) y = DrawTag(sb, "DISSONANCE", f.Dissonance, true, r.X, y);
                if (f.Crescendo > 0) y = DrawTag(sb, "CRESCENDO", f.Crescendo, false, r.X, y);
                if (f.Mute > 0) y = DrawTag(sb, "MUTE", 0, true, r.X, y);
                if (f.Guard) y = DrawTag(sb, "GUARD", 0, false, r.X, y);

                //Target Mark : the musician an enemy means to hit next
                for (int e = 0; e < battle.Enemies.Count; e++)
                {
                    Fighter enemy = battle.Enemies[e];
                    if (!enemy.Down && enemy.IntentTarget == f && view != View.Swipe && view != View.Over)
                        Gfx.Triangle(sb, r.Center.X, r.Y - 30, 12f, false, Palette.Accent * (0.6f + 0.4f * (float)Math.Sin(time * 5f)));
                }
            }
        }

        //Tag : a status word with its count after it. Returns where the next tag goes.
        private float DrawTag(SpriteBatch sb, string word, int count, bool filled, float x, float y)
        {
            Ui.Tag(sb, word, x, y, filled, 1f);
            if (count > 0) Gfx.Text(sb, Game.Font, NumberText.Get(count), x + Ui.TagWidth(word) + 5f, y, Palette.Paper, TextSize.Label);
            return y + 22f;
        }

        private void DrawEnemies(SpriteBatch sb)
        {
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                Fighter f = battle.Enemies[i];
                Rectangle r = enemyBox[i];
                ArtBank.DrawOrSlot(sb, ArtBank.EnemyOf(f.Enemy), r, Palette.Paper, f.Down ? 0.25f : 1f);
                if (f.Staggered) Gfx.RectOutline(sb, r, Palette.Highlight * (0.5f + 0.5f * (float)Math.Sin(time * 8f)), 3);
            }
        }

        private void DrawEnemyPlates(SpriteBatch sb)
        {
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                Fighter f = battle.Enemies[i];
                Rectangle r = enemyBox[i];
                float a = f.Down ? 0.4f : 1f;

                if (hover == f && !(view == View.Target && pending != null && pending.Target == SkillTarget.Ally))
                    Gfx.RectOutline(sb, new Rectangle(r.X - 6, r.Y - 6, r.Width + 12, r.Height + 12), Palette.Accent, 2);
                if (battle.Current == f && view != View.Choose) Gfx.Rect(sb, r.X, r.Bottom + 6, r.Width, 3, Palette.Accent);

                Gfx.TextSpacedCentered(sb, Game.Font, f.Name, r.Center.X, r.Bottom + 14, Palette.Paper * a, TextSize.Body, 2f);
                DrawHp(sb, f, r.X, r.Bottom + 44, r.Width, a);

                //OFF-BEAT : a thin gauge under the life, a filled tag once the enemy is thrown
                Gfx.Bar(sb, new Rectangle(r.X, r.Bottom + 80, r.Width, 4), f.OffBeat / (float)BattleRules.TurnOffBeatMax, Palette.Stage * a, Palette.PaperDim * a);
                float y = r.Bottom + 92;
                if (f.Staggered) y = DrawTag(sb, "OFF-BEAT", 0, true, r.X, y);
                if (f.Dissonance > 0) y = DrawTag(sb, "DISSONANCE", f.Dissonance, true, r.X, y);
            }
        }

        //HP : a bar and the number, the number big enough to read at a glance
        private void DrawHp(SpriteBatch sb, Fighter f, int x, int y, int width, float a)
        {
            Gfx.Bar(sb, new Rectangle(x, y, width, 6), f.Hp / (float)f.MaxHp, Palette.Stage * a, Palette.Paper * a);
            Gfx.Text(sb, Game.Font, NumberText.Get(f.Hp), x, y + 10, Palette.Highlight * a, TextSize.Body);
            Gfx.TextRight(sb, Game.Font, maxLabel[f], x + width, y + 12, Palette.LineGrey * a, TextSize.Label);
        }

        //Enemy Intent : how hard each note hits and how many notes, over its head (like Slay the Spire).
        //CADENZA : a boss's big move says so above it.
        private void DrawIntent(SpriteBatch sb)
        {
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                Fighter f = battle.Enemies[i];
                if (f.Down) continue;
                Rectangle r = enemyBox[i];
                float cy = r.Y - 30;

                //LULLABY : this turn it rests
                if (f.IntentRest)
                {
                    Gfx.TextSpacedCentered(sb, Game.Font, "REST", r.Center.X, cy - 10, Palette.Paper, TextSize.Body, 3f);
                    continue;
                }
                if (f.IntentNotes.Length == 0) continue;

                float dots = f.IntentNotes.Length * 14f;
                float left = r.Center.X - (40f + dots) / 2f;
                Gfx.Text(sb, Game.BigFont, NumberText.Get(f.IntentNotes[0]), left, cy - 16, Palette.Highlight, TextSize.Small);
                for (int n = 0; n < f.IntentNotes.Length; n++)
                    Gfx.Circle(sb, left + 40f + n * 14f, cy, 4f, Palette.Paper);
                if (f.IntentCadenza)
                    Gfx.TextSpacedCentered(sb, Game.Font, "CADENZA", r.Center.X, cy - 44, Palette.Highlight * (0.7f + 0.3f * (float)Math.Sin(time * 6f)), TextSize.Label, 3f);
                if (f.Staggered)
                    Gfx.Line(sb, left - 6, cy, left + 46f + dots, cy, Palette.Highlight, 2f);   // OFF-BEAT : this move will not happen
            }
        }

        //Turn Order : everybody this round, left to right. The band are filled plates, TACET only
        //an outline. The one acting now is bigger and lit, the ones done are dim, an enemy that
        //will lose its turn to OFF-BEAT is struck through.
        private void DrawTurnOrder(SpriteBatch sb)
        {
            float x = 24f;
            for (int i = 0; i < battle.Order.Count; i++)
            {
                Fighter f = battle.Order[i];
                bool now = i == battle.OrderIndex && battle.Current == f;
                bool done = i < battle.OrderIndex || f.Down;
                int w = now ? 112 : 86;
                int h = now ? 38 : 30;
                Rectangle r = new Rectangle((int)x, 14 + (38 - h) / 2, w, h);
                float a = done ? 0.35f : 1f;

                if (f.IsEnemy) Gfx.RectOutline(sb, r, (now ? Palette.Accent : Palette.PaperDim) * a, now ? 2 : 1);
                else Gfx.Rect(sb, r, (now ? Palette.Highlight : Palette.PaperDim) * a);

                Color ink = f.IsEnemy ? Palette.Paper : Palette.Ink;
                Gfx.TextCentered(sb, Game.Font, shortName[f], r.Center.X, r.Center.Y, ink * a, now ? TextSize.Body : TextSize.Label);
                if (f.Staggered && !done) Gfx.Line(sb, r.X + 4, r.Center.Y, r.Right - 4, r.Center.Y, Palette.Highlight, 2f);
                x += w + 8f;
            }

            Gfx.TextSpacedRight(sb, Game.Font, "ROUND", TacetGame.ScreenW - 64, 22, Palette.LineGrey, TextSize.Label, 3f);
            Gfx.Text(sb, Game.BigFont, NumberText.Get(battle.Round), TacetGame.ScreenW - 52, 12, Palette.Highlight, TextSize.Small);
        }

        private void DrawHitPoint(SpriteBatch sb)
        {
            Gfx.Circle(sb, HitX, HitY, NoteGlyph.WellRadius, Palette.Void * 0.85f);
            Gfx.CircleOutline(sb, HitX, HitY, NoteGlyph.WellRadius, Palette.Paper * 0.3f, 2f);
        }

        //Ring Progress : 0 when a note's ring appears, 1 when it is due
        private float Progress(QteNote n)
        {
            return 1f - (n.Due - clock) / RingTime;
        }

        //Play Notes : the attack or the skill. The note being played sits on the hit point with its ring,
        //the ones after it wait in a small row above, so the whole phrase can be read before it comes.
        private void DrawPlayNotes(SpriteBatch sb)
        {
            if (next >= notes.Count) return;
            QteNote n = notes[next];
            float progress = Progress(n);
            float a = MathHelper.Clamp(progress * 3f, 0f, 1f);

            DrawGlyph(sb, n, HitX, HitY, NoteGrow, a);
            if (holding)
            {
                //HOLD : the ring fills round the note as long as it is held
                float share = MathHelper.Clamp(holdTime / BattleRules.TurnHoldTime, 0f, 1f);
                Gfx.Arc(sb, HitX, HitY, RingMark + 14f, -MathHelper.PiOver2, -MathHelper.PiOver2 + MathHelper.TwoPi * share, Palette.Highlight, 4f);
            }
            else if (rolling)
            {
                //ROLL : the shakes so far, and the time left as a shrinking bar
                float left = MathHelper.Clamp((n.Due + BattleRules.TurnRollTime - (clock - Settings.TimingOffset)) / BattleRules.TurnRollTime, 0f, 1f);
                Gfx.Rect(sb, HitX - 60f, HitY + 66f, 120f * left, 4f, Palette.Highlight);
                Gfx.TextCentered(sb, Game.BigFont, NumberText.Get(shakes), HitX, HitY - 92f, Palette.Highlight, TextSize.Subtitle);
            }
            else
            {
                NoteGlyph.TimingRing(sb, HitX, HitY, progress, RingMark, RingStart, 0f, a);
            }

            //Coming Next : a small row above the hit point
            int waiting = notes.Count - next - 1;
            for (int i = 1; i <= waiting; i++)
            {
                float x = HitX - (waiting - 1) * 28f + (i - 1) * 56f;
                DrawGlyph(sb, notes[next + i], x, HitY - 130f, 0.75f, 0.6f);
            }
        }

        //Glyph : one note of any kind, see NoteGlyph
        private void DrawGlyph(SpriteBatch sb, QteNote n, float x, float y, float grow, float a)
        {
            if (n.Size < 0) NoteGlyph.ShapeNote(sb, x, y, n.Way, grow, a);                    // a SIGNATURE stroke
            else if (n.Kind == NoteKind.Spark) NoteGlyph.Spark(sb, x, y, 14f * grow, Palette.Highlight * a);
            else if (n.Kind == NoteKind.Roll) NoteGlyph.RollNote(sb, Game.BigFont, x, y, grow, a);
            else
            {
                NoteGlyph.Note(sb, Game.BigFont, x, y, n.Size, n.Way, grow, time, a);
                if (n.Kind == NoteKind.Hold) NoteGlyph.FermataSign(sb, x, y - 46f * grow, 10f * grow, Palette.Highlight * a);
            }
        }

        //Parry Notes : each note flies from the enemy to the hit point, arriving as its ring closes.
        //Only the next note to answer has a ring, so there is one thing to time at once.
        private void DrawParryNotes(SpriteBatch sb)
        {
            Fighter enemy = battle.Current;
            if (enemy == null || !enemy.IsEnemy) return;
            Rectangle from = enemyBox[enemy.Slot];

            for (int i = notes.Count - 1; i >= next; i--)
            {
                float progress = Progress(notes[i]);
                if (progress < 0f) continue;

                float p = Math.Min(1f, progress);
                float x = MathHelper.Lerp(from.Center.X, HitX, p);
                float y = MathHelper.Lerp(from.Center.Y, HitY, p);
                float a = i == next ? 1f : 0.55f;
                NoteGlyph.Note(sb, Game.BigFont, x, y, notes[i].Shown, notes[i].Way, MathHelper.Lerp(1f, NoteGrow, p), time, a);
                if (i == next) NoteGlyph.TimingRing(sb, HitX, HitY, progress, RingMark, RingStart, 0f, Math.Min(1f, progress * 3f));
            }

            //Target : a line from the hit point to the musician being attacked
            if (enemy.IntentTarget != null)
            {
                Rectangle t = partyBox[enemy.IntentTarget.Slot];
                Gfx.Line(sb, HitX - NoteGlyph.WellRadius, HitY, t.Center.X, t.Y + 20, Palette.Accent * 0.35f, 2f);
            }
        }

        //Ruler : from where the swipe began, the way the note wants, with the p / mf / f zones.
        //A spark, a roll and a hold being held need no ruler.
        private void DrawRuler(SpriteBatch sb)
        {
            if (!gesture.Held || holding || next >= notes.Count) return;
            QteNote n = notes[next];
            if (n.Kind == NoteKind.Spark || n.Kind == NoteKind.Roll) return;

            Vector2 d = NoteGlyph.Way(n.Way);
            Vector2 anchor = gesture.InStroke ? gesture.StrokeStart : Input.MousePos;
            float along = Vector2.Dot(gesture.LiveVector, d);
            Baton.DrawRuler(sb, anchor, d, along, gesture.InStroke ? Baton.SizeOf(along) : -1, n.Size >= 0);
        }

        //Bottom Bar : who is up, the commands, a line about the skill under the pointer, and BREATH
        private void DrawBottom(SpriteBatch sb)
        {
            Fighter f = battle.Current;

            if ((view == View.Choose || view == View.Target) && f != null)
            {
                Gfx.TextSpaced(sb, Game.BigFont, f.Name, 40, 600, Palette.Highlight, TextSize.Subtitle, 3f);

                for (int k = 0; k < 3; k++)
                {
                    Skill s = commandSkill[k];
                    bool usable = k == 0 || battle.CanUse(s);
                    bool lit = view == View.Target && pending == s && (k > 0 || pending == null);
                    Ui.Button(sb, commandBox[k], k == 0 ? "ATTACK" : s.Name, commandKey[k], lit, usable, 1f);
                    if (k > 0) Ui.Pips(sb, commandBox[k].X + 26, commandBox[k].Bottom - 9, s.Cost, s.Cost, 4f, 11f, usable ? 1f : 0.35f);
                }

                //Info Line : one line only. MUTE first, then the skill under the pointer, then what to do.
                if (f.Mute > 0 && view == View.Choose)
                    Gfx.TextSpaced(sb, Game.Font, HintMute, 40, 684, Palette.Paper, TextSize.Label, 2f);
                else if (hoverSkill != null)
                    Gfx.Text(sb, Game.Font, hoverSkill.Text, 40, 682, Palette.Paper, TextSize.Body);
                else if (hover != null && hover.IsEnemy && battle.TraitOn(hover))
                    Gfx.Text(sb, Game.Font, hover.Enemy.TurnTraitLine, 40, 682, Palette.Paper, TextSize.Body);   // what the enemy's trait does
                else
                {
                    string line = HintChoose;
                    if (view == View.Target) line = pending != null && pending.Target == SkillTarget.Ally ? HintAlly : HintEnemy;
                    Gfx.TextSpaced(sb, Game.Font, line, 40, 684, Palette.LineGrey, TextSize.Label, 2f);
                }
            }
            else if (view == View.Swipe)
            {
                if (swipeFor == SwipeFor.Parry && f != null) Gfx.TextSpaced(sb, Game.BigFont, f.Name, 40, 600, Palette.Paper, TextSize.Subtitle, 3f);
                else if (swipeFor == SwipeFor.Signature) Gfx.TextSpaced(sb, Game.BigFont, signatureName, 40, 600, Palette.Highlight, TextSize.Subtitle, 3f);
                else if (swipeSkill != null) Gfx.TextSpaced(sb, Game.BigFont, swipeSkill.Name, 40, 600, Palette.Highlight, TextSize.Subtitle, 3f);

                string line = swipeFor == SwipeFor.Parry ? HintParry : HintSwing;
                if (holding) line = HintHold;
                else if (rolling) line = HintRoll;
                Gfx.TextSpaced(sb, Game.Font, line, 40, 650, Palette.PaperDim, TextSize.Label, 2f);
            }

            Gfx.TextSpaced(sb, Game.Font, "BREATH", 970, 596, Palette.LineGrey, TextSize.Label, 3f);
            Ui.Pips(sb, 976, 636, battle.Breath, BattleRules.TurnBreathMax, 8f, 22f, 1f);
            Gfx.Text(sb, Game.BigFont, NumberText.Get(battle.Breath), 1196, 620, Palette.Highlight, TextSize.Small);

            //SIGNATURE Gauge : one diamond per PERFECT. Full, it glows and SPACE plays it.
            if (battle.Kit != TurnKit.None)
            {
                bool ready = battle.Signature >= battle.SignatureNeed;
                float glow = ready ? 0.6f + 0.4f * (float)Math.Sin(time * 6f) : 1f;
                if (ready && view == View.Choose) Gfx.RectOutline(sb, signatureBox, Palette.Accent * glow, 1);
                Gfx.TextSpaced(sb, Game.Font, signatureName, 970, 660, (ready ? Palette.Highlight : Palette.LineGrey) * glow, TextSize.Label, 3f);
                Ui.Pips(sb, 976, 694, battle.Signature, battle.SignatureNeed, 7f, 22f, 1f);
                if (ready) Ui.KeyChipRight(sb, "SPACE", 1232, 694, Palette.Highlight * glow);
            }
        }
    }
}
