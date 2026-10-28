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
    //BattleScreen : the turn based fight (Alpha 7). The rules live in Battle/TurnBattle.cs.
    //This page only shows them and reads the baton.
    //ONE BAR (step 12, 24 Oct) : every round is a bar.
    //   PLAN    write the bar : click a musician, then an enemy (ATTACK) or a skill (1, 2, 3). The bar plays
    //           in the order the moves were written, a musician with no move ATTACKs. A name on the BENCH
    //           calls that musician in (one per bar, for BREATH). SPACE plays the SIGNATURE. ENTER plays the bar.
    //   TARGET  a skill or ATTACK waits for its enemy or musician to be clicked (right click goes back)
    //   STRIKE  an ATTACK : no swipe, the musician lunges and the hit lands with a freeze and a shake
    //   SWIPE   the notes of a skill, the SIGNATURE or an enemy's attack, one after another
    //   PAUSE   a short breath after a turn, so the result can be read
    //   OVER    the banner, then the result page
    //PRACTICE (step 9) : new BattleScreen(true) is the tutorial's practice fight. No run is needed and
    //nothing is saved. A card at the top teaches one thing at a time : PLAY, PARRY, a skill, CALL, the SIGNATURE.
    //Layout (Beta1, 28 Oct) : the conductor's view from behind the band, see BattleScreen.Stage.cs. The musician
    //pointed at stands big on the left, TACET further off on the right, the bar along the top with THE BAND
    //under it, and the commands with the BENCH and the shared BREATH along the bottom. While a swipe is wanted
    //the world goes dark and soft behind the ring (QTE FOCUS) and the pointer becomes the baton. The ring,
    //the notes and the baton never shake. (Alpha 7 had the band in six seats like Wildfrost's board.)
    public partial class BattleScreen : GameScreen
    {
        private enum View { Plan, Target, Strike, Swipe, Pause, Over }
        private enum SwipeFor { Skill, Parry, Signature }
        private enum Lesson { Play, Parry, Skill, Call, Signature }

        //Layout
        private const float HitX = 640f;
        private const float HitY = 360f;          // Beta1 round 4 : the ring sits in the middle of the screen (the user asked)
        private const float NoteGrow = 1.6f;      // a note on the hit point, this much bigger than in the duel
        private const float RingStart = 160f;     // the ring starts this wide
        private const float RingMark = 52f;       // and meets the note here
        private const int PanelTop = 590;         // Beta1 : the bottom panel starts here, the musician in front stands behind it
        private static readonly Rectangle[] commandBox =
        {
            new Rectangle(390, 602, 190, 50),
            new Rectangle(590, 602, 190, 50),
            new Rectangle(790, 602, 190, 50),
        };
        private static readonly string[] commandKey = { "1", "2", "3" };
        private static readonly Rectangle signatureBox = new Rectangle(1000, 630, 256, 40);
        private static readonly Rectangle playBox = new Rectangle(1000, 674, 256, 40);
        private const int BreathX = 1006;         // BREATH, the label at the top right of the panel
        private const int BreathY = 594;
        private const int BenchX = 480;
        private const int BenchY = 662;
        private const int BenchW = 110;
        private const int BenchH = 32;
        private const int BenchStep = 116;

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

        //Plan : who the player is writing a move for, and what is under the pointer
        private Fighter selected;
        private Skill[] commandSkill = new Skill[3];       // [0] is ATTACK (null)
        private Skill pending;                             // the skill waiting for a target, null for ATTACK
        private Fighter hover;
        private Skill hoverSkill;
        private int hoverBench = -1;

        //Bar Strip : who plays in what order, worked out again whenever the plan changes (never in Draw)
        private List<Fighter> previewOrder = new List<Fighter>();
        private List<BarMove> previewMoves = new List<BarMove>();

        //STRIKE : an ATTACK lunging at its enemy. The hit lands at TurnStrikeImpact.
        private Fighter striker;
        private BarMove strikeMove;
        private Fighter strikeTarget;
        private float strikeTime;
        private bool struck;

        //Look : each fighter's flash and knock back after a hit, fading by themselves (game feel)
        private class Look
        {
            public float Flash;
            public float Knock;
            public float Gone;         // SILENCED : 0 = standing, 0..1 while it dissolves into the hole, 1 = gone
        }
        private Dictionary<Fighter, Look> looks = new Dictionary<Fighter, Look>();

        //Bar Count : hits and damage the band landed this bar, shown while it plays
        private int barHits;
        private int barDamage;
        private bool barPlaying;      // a bar is playing : true from PLAY THE BAR until the next bar is written (step 13, the last
                                      // turn of a bar used to be left out, the rules are already back in Plan when its events show)
        private float barPulse;
        private string barHitsText = "";
        private string barDamageText = "";

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

        //MISS WAIT (step 14) : a stroke that would be a MISS (the wrong way, or too early) is kept, not judged, while the
        //note's window is still open. The user found many swipes counted the wrong way : with the button held, the hand
        //going back to the middle after a note is a stroke too, and it was taken as the answer to the next note, and the
        //real swipe then slid on to the note after. Now a better stroke before the window closes is judged instead, and
        //the kept one only counts if none comes.
        private bool missKept;
        private Grade missGrade;
        private bool missSize;

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

        private const string HintPlan = "CLICK A MUSICIAN, THEN AN ENEMY OR A SKILL   /   ENTER = PLAY THE BAR";
        private const string HintEnemy = "CLICK AN ENEMY   /   RIGHT CLICK = BACK";
        private const string HintAlly = "CLICK A MUSICIAN   /   RIGHT CLICK = BACK";
        private const string HintCalled = "JUST CALLED : PLAYS FROM THE NEXT BAR";
        private const string HintSwing = "SWIPE AS THE RING TOUCHES THE NOTE";
        private const string HintParry = "SWIPE BACK AT EACH NOTE";
        private const string HintHold = "HOLD STILL";
        private const string HintRoll = "SHAKE!";
        private const string HintMute = "MUTE : NO SKILLS THIS BAR";
        private const string HintNoSkills = "DECREE : A SKILL THIS BAR HURTS WHOEVER PLAYS IT";
        private const string HintNoAttacks = "DECREE : AN ATTACK THIS BAR HURTS WHOEVER PLAYS IT (NO MOVE = ATTACK)";
        private const string WordAttack = "ATTACK";

        //PRACTICE : what the card at the top says, and which of the five lessons are done
        private const string CoachPlay = "Your bar. Press ENTER (PLAY) and the whole band attacks.";
        private const string CoachParry = "Its turn. Swipe back at each note to block it. PERFECT blocks it all.";
        private const string CoachSkill = "Click a musician, then 2 or 3 for a skill. Skills cost BREATH.";
        private const string CoachSkillSwipe = "A skill is a few notes. Swing the way the arrow points as the ring meets it.";
        private const string CoachCall = "Click a name on the BENCH to call them in. They play from the next bar.";
        private const string CoachSignature = "The diamonds are full. Press SPACE for the SIGNATURE.";
        private const string CoachSignatureSwipe = "Draw the 4/4 shape : DOWN, LEFT, RIGHT, UP. Any size.";
        private const string CoachWin = "That is all of it. Now win the fight.";
        private static readonly Rectangle coachBox = new Rectangle(240, 64, 800, 60);
        private bool[] learned = new bool[5];              // one per Lesson
        private int learnedCount;
        private string coachLine = CoachPlay;

        public BattleScreen(bool practice = false)
        {
            this.practice = practice;
        }

        //Can Pause : not in PRACTICE, where ESC leaves like on every tutorial page
        //Shows Grain : the battle has its own film grain (Post.fx), the page-wide specks only cluttered it (round 4)
        public override bool ShowsGrain
        {
            get { return false; }
        }

        public override bool CanPause
        {
            get { return !practice && (battle == null || (battle.Step != TurnStep.Won && battle.Step != TurnStep.Lost)); }   // no pause once it is decided
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

            //The Band : the ones on stage in seat order, then the rest of the ensemble. Everybody seated on the
            //BAND page starts on the field, the others wait on the BENCH (ONE BAR, step 15 : it was always the first 2).
            List<Musician> band = new List<Musician>();
            for (int s = 0; s < run.Formation.Seated.Length; s++)
                if (run.Formation.Seated[s] != null) band.Add(run.Formation.Seated[s]);
            int onStage = Math.Max(1, Math.Min(band.Count, run.Seats));
            for (int i = 0; i < run.Roster.Count; i++)
                if (!band.Contains(run.Roster[i])) band.Add(run.Roster[i]);

            //BAND LIFE : everybody walks in with the life they left the last fight with (step 6)
            int[] life = new int[band.Count];
            for (int i = 0; i < band.Count; i++) life[i] = run.LifeOf(band[i]);

            //PACKS : every enemy of the fight (step 12)
            List<Enemy> enemies = new List<Enemy>(run.Battle.Pack);
            battle = new TurnBattle(band, enemies, run.Floor, run.Rng, life, Archive.Level(Archive.Life) * BattleRules.ArchiveLifePerLevel, onStage);
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

        //Practice Load : PRACTICE. The first three musicians at full life, two on the field and one on the
        //BENCH, THE UNHEARING's SIGNATURE, floor 1 and the soft PRACTICE enemy (EnemyList.Practice).
        //No motifs and no ARCHIVE.
        private void PracticeLoad()
        {
            List<Musician> band = new List<Musician> { MusicianList.All[0], MusicianList.All[1], MusicianList.All[2] };
            battle = new TurnBattle(band, new List<Enemy> { EnemyList.Practice }, 1, new Random(), null, 0, BattleRules.TurnStartBand);
            battle.Kit = TurnKit.Beethoven;
            for (int i = 0; i < ConductorList.All.Length; i++)
                if (ConductorList.All[i].Turn == TurnKit.Beethoven) signatureName = ConductorList.All[i].TurnSignatureName;
            Arrange();
            SoundBank.PlayMusic(Music.Battle);
        }

        //Arrange : the words on everybody's plates, then a moment before the first bar
        private void Arrange()
        {
            for (int i = 0; i < battle.Party.Count; i++) Name(battle.Party[i]);
            for (int i = 0; i < battle.Enemies.Count; i++) Name(battle.Enemies[i]);

            battle.Events.Clear();
            gesture.Clear();
            baton.Reset(Input.MousePos);
            Game.IsMouseVisible = true;
            LoadStage();

            //The first bar starts after a moment, so the page can be seen first
            view = View.Pause;
            viewTime = 0f;
        }

        //Name : a fighter's plate words and look, made once when they come onto the field
        private void Name(Fighter f)
        {
            shortName[f] = ShortName(f.Name);
            maxLabel[f] = "/ " + NumberText.Get(f.MaxHp);
            looks[f] = new Look();
        }

        //Short Name : what fits a plate of the bar strip. "THE" goes, and a long name keeps its
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
            if (barPulse > 0f) barPulse = Math.Max(0f, barPulse - dt * 3f);

            //Hit Stop : the band, TACET and the sparks freeze for a moment. The ring clock and the
            //baton never stop, so a swipe is never judged late because of it.
            if (hitStop > 0f) hitStop -= dt;
            else
            {
                effects.Update(dt);
                if (shake > 0f) shake = Math.Max(0f, shake - dt * 3.5f);
                foreach (Look look in looks.Values)
                {
                    look.Flash = Math.Max(0f, look.Flash - dt * 5f);
                    look.Knock = Math.Max(0f, look.Knock - dt * 6f);
                    if (look.Gone > 0f && look.Gone < 1f) look.Gone = Math.Min(1f, look.Gone + dt / BattleRules.TurnDissolveTime);
                }
            }

            UpdateStage(dt, hitStop > 0f);
            hits.Update(dt, hitStop > 0f);
            UpdateShow(dt);
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

            if (view == View.Plan) UpdatePlan();
            else if (view == View.Target) UpdateTarget();
            else if (view == View.Strike) UpdateStrike(dt);
            else if (view == View.Swipe) UpdateSwipe(dt);
            else if (view == View.Pause && viewTime >= BattleRules.TurnPause) NextStep();
            else if (view == View.Over && viewTime >= BattleRules.TurnEndTime) Finish();
        }

        //Next Step : the fight ends, a new bar is written, or the next move of the bar plays
        private void NextStep()
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

            if (battle.Step == TurnStep.Plan) EnterPlan();
            else if (battle.Step == TurnStep.PlayerTurn)
            {
                BarMove move = battle.CurrentMove;
                if (move.Skill == null) StartStrike(move);
                else StartSkill(move.Skill);
            }
            else StartParry();
        }

        //======================================================================================
        //Plan : writing the bar (ONE BAR)
        //======================================================================================

        private void EnterPlan()
        {
            if (practice) PracticeHelp();
            view = View.Plan;
            viewTime = 0f;
            pending = null;
            barPlaying = false;
            Game.IsMouseVisible = true;
            if (!battle.CanPlan(selected) || battle.Bar.Count == 0) Select(NextUnwritten(null));     // a new bar starts at the front seat
            RefreshPreview();
        }

        //Select : the musician whose move the commands write (null for nobody)
        private void Select(Fighter f)
        {
            selected = f;
            commandSkill[1] = f != null ? SkillList.First(f.Musician) : null;
            commandSkill[2] = f != null ? SkillList.Second(f.Musician) : null;
        }

        //Next Unwritten : the next musician on the field, after this one, who can still be given a move.
        //Falls back to anyone who can play, then to nobody.
        private Fighter NextUnwritten(Fighter after)
        {
            int start = after != null ? after.Slot + 1 : 0;
            for (int k = 0; k < battle.Party.Count; k++)
            {
                Fighter f = battle.Party[(start + k) % battle.Party.Count];
                if (battle.CanPlan(f) && battle.MoveOf(f) == null) return f;
            }
            for (int i = 0; i < battle.Party.Count; i++)
                if (battle.CanPlan(battle.Party[i])) return battle.Party[i];
            return null;
        }

        private void RefreshPreview()
        {
            battle.PreviewOrder(previewOrder, previewMoves);
        }

        //After Write : a move was written, on to the next musician without one
        private void AfterWrite()
        {
            SoundBank.Play(Sfx.UiConfirm);
            Fighter written = selected;
            Select(NextUnwritten(written));
            if (selected == null) Select(written);
            pending = null;
            view = View.Plan;
            RefreshPreview();
        }

        private void UpdatePlan()
        {
            hover = null;
            hoverSkill = null;
            hoverBench = -1;

            //SIGNATURE : SPACE, or a click on the gauge
            if (battle.CanSignature && (Input.KeyPressed(Keys.Space) || Input.ClickedOn(signatureBox)))
            {
                StartSignature();
                return;
            }

            //PLAY : the bar is written
            if (Input.KeyPressed(Keys.Enter) || Input.ClickedOn(playBox))
            {
                PlayBar();
                return;
            }

            //CALL : a name on the BENCH
            for (int i = 0; i < battle.Bench.Count; i++)
            {
                if (!Input.MouseOver(BenchBox(i))) continue;
                hoverBench = i;
                if (Input.MouseClicked())
                {
                    CallIn(i);
                    return;
                }
            }

            //A click on a musician selects them, a click on an enemy is the selected musician's ATTACK
            Fighter musician = MusicianUnderPointer(true);
            Fighter enemy = musician == null ? EnemyUnderPointer() : null;
            hover = musician ?? enemy;
            if (musician != null && Input.MouseClicked() && battle.CanPlan(musician))
            {
                SoundBank.Play(Sfx.UiMove);
                Select(musician);
                return;
            }
            if (enemy != null && Input.MouseClicked() && battle.PlanAttack(selected, enemy))
            {
                AfterWrite();
                return;
            }

            //TAB : the next musician. Right click or BACKSPACE rubs out the selected musician's move.
            if (Input.KeyPressed(Keys.Tab)) Select(NextUnwritten(selected) ?? selected);
            if ((Input.MouseRightClicked() || Input.KeyPressed(Keys.Back)) && selected != null && battle.MoveOf(selected) != null)
            {
                SoundBank.Play(Sfx.UiBack);
                battle.Unplan(selected);
                RefreshPreview();
            }

            if (!battle.CanPlan(selected)) return;
            for (int k = 1; k < 3; k++)
                if (Input.MouseOver(commandBox[k])) hoverSkill = commandSkill[k];

            if (Input.ClickedOn(commandBox[0]) || Input.KeyPressed(Keys.D1) || Input.KeyPressed(Keys.NumPad1))
            {
                if (StandingEnemies() == 1)
                {
                    if (battle.PlanAttack(selected, FirstStanding())) AfterWrite();
                }
                else
                {
                    pending = null;
                    view = View.Target;
                }
                return;
            }
            if (Input.ClickedOn(commandBox[1]) || Input.KeyPressed(Keys.D2) || Input.KeyPressed(Keys.NumPad2)) Pick(commandSkill[1]);
            else if (Input.ClickedOn(commandBox[2]) || Input.KeyPressed(Keys.D3) || Input.KeyPressed(Keys.NumPad3)) Pick(commandSkill[2]);
        }

        //Pick : a skill is chosen for the selected musician. Skills that need no choice are written at once.
        private void Pick(Skill s)
        {
            if (!battle.CanPlanSkill(selected, s))
            {
                SoundBank.Play(Sfx.UiDenied);
                return;
            }

            bool written = false;
            if (s.Target == SkillTarget.Self || s.Target == SkillTarget.AllEnemies || s.Target == SkillTarget.AllAllies)
                written = battle.PlanSkill(selected, s, null);
            else if (s.Target == SkillTarget.Enemy && StandingEnemies() == 1)
                written = battle.PlanSkill(selected, s, FirstStanding());
            else
            {
                pending = s;
                view = View.Target;
                return;
            }
            if (written) AfterWrite();
        }

        //Target : click the enemy (or the musician) the move is for, right click to go back
        private void UpdateTarget()
        {
            hoverSkill = pending;
            if (Input.MouseRightClicked())
            {
                view = View.Plan;
                return;
            }

            bool forAlly = pending != null && pending.Target == SkillTarget.Ally;
            hover = forAlly ? MusicianUnderPointer(false) : EnemyUnderPointer();
            if (hover == null || !Input.MouseClicked()) return;

            bool written = pending == null ? battle.PlanAttack(selected, hover) : battle.PlanSkill(selected, pending, hover);
            if (written) AfterWrite();
        }

        //Call In : CALL, the musician on the BENCH walks onto the field
        private void CallIn(int bench)
        {
            if (!battle.CanCall(bench))
            {
                SoundBank.Play(Sfx.UiDenied);
                return;
            }
            Fighter f = battle.Call(bench);
            hoverBench = -1;                                                           // the chip under the pointer is gone
            Name(f);
            if (practice) Learn(Lesson.Call);
            ShowEvents();
            RefreshPreview();
        }

        //Play Bar : ENTER. Musicians with no move ATTACK, then the bar plays from its first move.
        private void PlayBar()
        {
            SoundBank.Play(Sfx.UiConfirm);
            if (practice) Learn(Lesson.Play);
            battle.PlayBar();
            barHits = 0;
            barDamage = 0;
            barPlaying = true;
            pending = null;
            hover = null;
            NextStep();
        }

        //Musician Under Pointer : any also takes a musician who is down (the plan shows what is under the pointer)
        private Fighter MusicianUnderPointer(bool any)
        {
            for (int i = 0; i < battle.Party.Count; i++)
            {
                Fighter f = battle.Party[i];
                if (f.Down && !any) continue;
                if (Input.MouseOver(ChipBox(i))) return f;
            }
            if (fore != null && (any || !fore.Down) && Input.MouseOver(foreBox)) return fore;   // the one in front can be clicked too
            return null;
        }

        private Fighter EnemyUnderPointer()
        {
            for (int i = 0; i < battle.Enemies.Count; i++)
                if (battle.CanTarget(battle.Enemies[i]) && Input.MouseOver(BoxOf(battle.Enemies[i]))) return battle.Enemies[i];
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
        //STRIKE : an ATTACK, no swipe (step 12). The lunge, then the hit with a freeze, a shake and a flash.
        //======================================================================================

        private void StartStrike(BarMove move)
        {
            striker = battle.Current;
            hitBy = striker;
            hitSignature = false;
            PlayArt(striker, "attack");
            strikeMove = move;
            strikeTarget = battle.CanTarget(move.Target) ? move.Target : battle.FocusTarget();
            strikeTime = 0f;
            struck = false;
            view = View.Strike;
            viewTime = 0f;
            Game.IsMouseVisible = true;
        }

        //Update Strike : the lunge's clock stops during the hit stop, so the hit really freezes
        private void UpdateStrike(float dt)
        {
            if (hitStop <= 0f) strikeTime += dt;

            if (!struck && strikeTime >= BattleRules.TurnStrikeImpact)
            {
                struck = true;
                //Nothing flies across : the blow's light happens on the enemy (HitFx, from ShowEvents)
                Rectangle to = BoxOf(strikeTarget);
                battle.Attack(strikeTarget);
                StageHit(new Vector2(to.Center.X, to.Center.Y), true, 0.8f);
                hitStop = Math.Max(hitStop, BattleRules.TurnStrikeStop);
                shake = Math.Max(shake, 0.35f);
                ShowEvents();
            }

            if (struck && strikeTime >= BattleRules.TurnStrikeTime) FinishMove();
        }

        //Finish Move : after an ATTACK. The run ends at once when the fight is lost (see Finish Swipe).
        private void FinishMove()
        {
            striker = null;
            CheckLost();
            NextStep();
        }

        //======================================================================================
        //Swipe : the notes of a skill, the SIGNATURE or an enemy attack
        //======================================================================================

        //Skill : the notes written on the skill (Data/Skill.cs). Its target is in the bar's move.
        private void StartSkill(Skill s)
        {
            notes.Clear();
            for (int i = 0; i < s.Notes.Length; i++) AddNote(s.Notes[i].Kind, s.Notes[i].Way, s.Notes[i].Size);
            BeginSwipe(SwipeFor.Skill);
            hitBy = battle.Current;
            hitSignature = false;
            PlayArt(battle.Current, "skill");
        }

        //Signature : the 4/4 shape, any size (Size -1)
        private void StartSignature()
        {
            notes.Clear();
            for (int i = 0; i < TurnBattle.SignatureShape.Length; i++) AddNote(NoteKind.Swipe, TurnBattle.SignatureShape[i], -1);
            BeginSwipe(SwipeFor.Signature);
            cutIn = BattleRules.TurnCutInTime;                                         // CUT-IN first (BattleScreen.Show.cs)
            hitBy = null;
            hitSignature = true;
        }

        //Parry : the enemy's notes fly in one by one
        private void StartParry()
        {
            Fighter enemy = battle.Current;
            hitBy = enemy;
            hitSignature = false;
            PlayArt(enemy, "attack");
            PlayArt(enemy.IntentTarget, "parry");
            notes.Clear();
            for (int i = 0; i < enemy.IntentNotes.Length; i++)
            {
                AddNote(NoteKind.Swipe, enemy.IntentWays[i], enemy.IntentSizes[i]);
                if (i < enemy.IntentShown.Length) notes[i].Shown = enemy.IntentShown[i];      // FALSE NOTES
            }
            BeginSwipe(SwipeFor.Parry);
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
        private void BeginSwipe(SwipeFor what)
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
            holding = false;
            rolling = false;
            hover = null;

            view = View.Swipe;
            viewTime = 0f;
            clock = 0f;
            missKept = false;

            //Centre : the pointer is hidden while swiping and was left on a button (PLAY THE BAR is near the bottom right
            //corner), where the edge of the screen stops a swipe down or right short, or bends it into the wrong way.
            //It starts every swipe from the middle instead (step 14).
            Input.MoveMouseTo(new Vector2(TacetGame.ScreenW / 2f, TacetGame.ScreenH / 2f));
            gesture.Clear();
            baton.Reset(Input.MousePos);
            Game.IsMouseVisible = false;
        }

        private void UpdateSwipe(float dt)
        {
            //CUT-IN : the SIGNATURE's picture first. The ring waits, and any movement meanwhile never counts.
            if (cutIn > 0f)
            {
                cutIn -= dt;
                if (cutIn <= 0f) gesture.Clear();
                return;
            }

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
                bool rightWay = spark || gesture.Direction == n.Way;
                bool rightSize = spark || n.Size < 0 || battle.AnySize || Baton.SizeOf(gesture.Length) == n.Size;   // LOCKED TEMPO : any size
                baton.Whip(gesture.Direction);

                //MISS WAIT : kept until the window closes, a better stroke may still come (CAPRICE has already made a MISS a GOOD)
                Grade grade = battle.Judge(offset, rightWay);
                if (!rightWay || grade == Grade.Miss)
                {
                    missKept = true;
                    missGrade = grade;
                    missSize = rightSize;
                    return;
                }

                if (swipeFor == SwipeFor.Parry && grade == Grade.Perfect && !rightSize) grade = Grade.Good;   // the wrong size only half blocks

                qteEffects.SpawnSlash(gesture.From, gesture.To, grade == Grade.Perfect ? 1f : 0.55f);

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
                if (missKept) Land(missGrade, missSize, 1f);           // only a stroke the wrong way or too early came
                else Land(Grade.Hesitate, true, 0f);                   // no swipe at all
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
                qteEffects.SpawnSparks(HitX, HitY, 2, 1f);
            }
            if (now >= n.Due + BattleRules.TurnRollTime)
            {
                rolling = false;
                Land(battle.RollGrade(shakes), true, 1f);
            }
        }

        //Land : one note is done. A parried note counts at once, the notes of a skill or the SIGNATURE
        //are handed in together when the last one is done.
        private void Land(Grade grade, bool rightSize, float hold)
        {
            missKept = false;
            results[next] = new NoteResult(grade, rightSize, hold);
            qteEffects.SpawnHit(HitX, HitY, grade);
            judgeLife = 1f;

            bool perfect = grade == Grade.Perfect && rightSize;
            if (perfect)
            {
                perfects++;
                qteEffects.SpawnShatter(HitX, HitY, RingMark);
                hitStop = BattleRules.TurnHitStop;
            }

            if (swipeFor == SwipeFor.Parry)
            {
                battle.ParryNote(next, grade);
                judgeWord = grade == Grade.Perfect ? "PARRY" : (grade == Grade.Good ? "GOOD" : "MISS");
                if (grade == Grade.Perfect) qteEffects.SpawnSparks(HitX, HitY, 10, 1f);
                if (grade == Grade.Perfect || grade == Grade.Good)                       // the parry's clash, in the colours of the one who parried
                    qteHits.Clash(new Vector2(HitX, HitY), grade == Grade.Perfect && rightSize, SpotOf(battle.Current != null ? battle.Current.IntentTarget : null));
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
            //QTE FOCUS : let the world come back at once, so the blow's light is not dimmed by a fading ring (round 6)
            if (swipeFor != SwipeFor.Parry) focus = Math.Min(focus, 0.3f);

            if (swipeFor == SwipeFor.Skill)
            {
                if (practice) Learn(Lesson.Skill);
                battle.UseSkill(results);
                shake = Math.Max(shake, 0.4f);
            }
            else if (swipeFor == SwipeFor.Signature)
            {
                //SIGNATURE : the conductor's name for it fills the screen for a moment, then back to the bar
                if (practice) Learn(Lesson.Signature);
                battle.UseSignature(results);
                judgeWord = signatureName;
                judgeLife = 1.6f;
                qteEffects.SpawnFlare(HitX, HitY, 1f);
                shake = 0.8f;
            }
            else
            {
                if (practice) Learn(Lesson.Parry);
                battle.FinishEnemyTurn();
            }

            ShowEvents();
            view = View.Pause;
            viewTime = 0f;
            Game.IsMouseVisible = true;
            CheckLost();
        }

        //Lost : the run ends now, not when the result page opens, so quitting or closing the game during the
        //banner cannot bring the fight back. The ECHOES are given at the same moment (ARCHIVE).
        private void CheckLost()
        {
            if (practice || battle.Step != TurnStep.Lost || run.EchoesPaid) return;
            SaveFile.DeleteRun();
            Archive.Pay(run, false);
        }

        //Show Events : turn what the rules wrote down into numbers, words and game feel over the fighters.
        //Several on one fighter stack upward so they never cover each other.
        private void ShowEvents()
        {
            popStack.Clear();
            int bolts = 0;                                                                 // LIGHTNING : strikes so far
            for (int i = 0; i < battle.Events.Count; i++)
            {
                TurnEvent e = battle.Events[i];
                if (e.Kind == TurnEventKind.Breath)
                {
                    effects.SpawnPop(BreathX + 90f, BreathY - 16f, "+" + NumberText.Get(e.Amount), Palette.Highlight, 0.9f);
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
                    effects.SpawnPop(BreathX + 90f, BreathY - 16f, "-" + NumberText.Get(e.Amount) + " BREATH", Palette.PaperDim, 0.8f);
                    continue;
                }
                if (e.Kind == TurnEventKind.Ready)
                {
                    effects.SpawnPop(1120f, signatureBox.Y - 14f, "SIGNATURE READY", Palette.Highlight, 0.7f);
                    continue;
                }
                if (e.Kind == TurnEventKind.Extra)
                {
                    effects.SpawnPop(HitX, HitY - 60f, "+" + NumberText.Get(e.Amount) + " ATTACKS", Palette.Highlight, 0.9f);   // FEATHER
                    continue;
                }
                if (e.Who == null) continue;

                Rectangle box = BoxOf(e.Who);
                Look look;
                looks.TryGetValue(e.Who, out look);
                int stack;
                popStack.TryGetValue(e.Who, out stack);
                popStack[e.Who] = stack + 1;
                float y = box.Y - 34 - stack * 34;
                float x = box.Center.X;
                if (!e.Who.IsEnemy && e.Who != fore) y = box.Bottom + 22 + stack * 34;      // THE BAND row : under the card

                if (e.Kind == TurnEventKind.Damage)
                {
                    //The striker's colours : a musician's own colour on an enemy (round 4 : the number too),
                    //white for the SIGNATURE, TACET's INK on a musician
                    HitColors colors = !e.Who.IsEnemy ? HitColors.Plain : (hitSignature ? SignatureColors() : SpotOf(hitBy));

                    //A hit : a pixel number (bigger for a big hit), the fighter flashes and is knocked back
                    Color number = e.Who.IsEnemy ? colors.Tint : Palette.PaperDim;
                    SpawnNumber(new Vector2(x, y), e.Amount, false, e.Amount >= BattleRules.TurnBigHit || hitSignature, number);
                    if (look != null)
                    {
                        look.Flash = 1f;
                        look.Knock = 1f;
                    }
                    StageHit(new Vector2(x, box.Center.Y), e.Who.IsEnemy, 0.4f + Math.Min(0.6f, e.Amount / 30f), colors.Tint);
                    if (!e.Who.Down) PlayArt(e.Who, "hurt");

                    //The blow's light : the striker's style on an enemy, TACET's INK on a musician (Core/HitFx.cs).
                    //LIGHTNING strikes the enemies one after another, the first lights the whole stage.
                    HitStyle style = !e.Who.IsEnemy ? HitStyle.Ink : (hitSignature ? SignatureStyle() : StyleOf(hitBy));
                    float power = 0.4f + Math.Min(0.8f, e.Amount / 25f);
                    if (style == HitStyle.Lightning)
                    {
                        if (bolts == 0) AddStageLight(new Vector2(TacetGame.ScreenW / 2f, 300f), 1100f, BattleRules.HitBoltFlash, 0.4f, Color.Lerp(Color.White, colors.Tint, 0.6f));
                        hits.HitLater(style, DrawBox(e.Who), power, colors, BattleRules.HitBoltStagger * bolts);
                        bolts++;
                    }
                    else hits.Hit(style, DrawBox(e.Who), power, colors);

                    //IMPACT FRAME : the world holds still through the turns and the flash
                    if (hitSignature) hitStop = Math.Max(hitStop, BattleRules.HitImpactHold);
                    if (e.Who.IsEnemy)
                    {
                        if (barPlaying)
                        {
                            barHits++;
                            barDamage += e.Amount;
                            barHitsText = NumberText.Get(barHits);
                            barDamageText = NumberText.Get(barDamage);
                            barPulse = 1f;
                        }
                    }
                    else shake = Math.Max(shake, 0.45f);
                }
                else if (e.Kind == TurnEventKind.Down)
                {
                    PlayArt(e.Who, e.Who.IsEnemy ? "death" : "ko");
                    //Silenced : the biggest moment of a bar, so the biggest feedback
                    if (e.Who.IsEnemy)
                    {
                        if (look != null) look.Gone = 0.0001f;                                  // SILENCED : it starts to dissolve
                        hits.Silence(DrawBox(e.Who), dark.Hole + stageCam * BattleRules.StageParallax[3]);
                        effects.SpawnShatter(box.Center.X, box.Center.Y, 70f);
                        effects.SpawnFlare(box.Center.X, box.Center.Y, 1f);
                        effects.SpawnSparks(box.Center.X, box.Center.Y, 30, 0f);
                        effects.SpawnPop(x, box.Y - 90, "SILENCED", Palette.Highlight, 1.1f);
                        StageHit(new Vector2(box.Center.X, box.Center.Y), true, 1.2f);
                        shake = 0.9f;
                        hitStop = Math.Max(hitStop, BattleRules.TurnKillStop);
                    }
                    else effects.SpawnPop(x, box.Y - 90, "DOWN", Palette.PaperDim, 1f);
                }
                else if (e.Kind == TurnEventKind.Called)
                {
                    effects.SpawnFlare(box.Center.X, box.Center.Y, 0.8f);
                    effects.SpawnSparks(box.Center.X, box.Bottom, 20, 0f);
                    effects.SpawnPop(x, y, "CALLED", Palette.Highlight, 1f);
                }
                else if (e.Kind == TurnEventKind.Heal) SpawnNumber(new Vector2(x, y), e.Amount, true, false, Palette.Highlight);
                else if (e.Kind == TurnEventKind.Dissonance) effects.SpawnPop(x, y, "DISSONANCE", Palette.PaperDim, 0.6f);
                else if (e.Kind == TurnEventKind.Crescendo) effects.SpawnPop(x, y, "CRESCENDO", Palette.Paper, 0.6f);
                else if (e.Kind == TurnEventKind.Mute) effects.SpawnPop(x, y, "MUTE", Palette.PaperDim, 0.7f);
                else if (e.Kind == TurnEventKind.Skip) effects.SpawnPop(x, y, "OFF-BEAT", Palette.PaperDim, 0.7f);
                else if (e.Kind == TurnEventKind.Echo) effects.SpawnPop(x, box.Y - 60, "ECHO", Palette.Highlight, 0.7f);       // FUGUE
                else if (e.Kind == TurnEventKind.Quieter) effects.SpawnPop(x, box.Y - 70, "QUIETER", Palette.Paper, 0.7f);    // THE WHOLE FLOOR
                else if (e.Kind == TurnEventKind.Thorns) effects.SpawnPop(x, box.Y - 70, "DECREE", Palette.Accent, 0.8f);     // DECREE broken
                else if (e.Kind == TurnEventKind.Counter)
                {
                    effects.SpawnPop(x, y, "COUNTER", Palette.Highlight, 0.9f);
                    effects.SpawnFlare(x, box.Center.Y, 1f);
                }
                else if (e.Kind == TurnEventKind.Break)
                {
                    //OFF-BEAT : thrown off the beat, a big moment too
                    effects.SpawnPop(x, y, "OFF-BEAT", Palette.Highlight, 1.2f);
                    effects.SpawnSparks(x, box.Center.Y, 30, 0f);
                    shake = 0.9f;
                    hitStop = Math.Max(hitStop, BattleRules.HitStopTime);
                }
            }
            battle.Events.Clear();
        }

        //======================================================================================
        //Boxes : where each fighter stands, worked out from their slot (no list to keep in step)
        //======================================================================================

        //Box Of : where a fighter is, for clicks and words. The musician in front is the big figure, the rest of
        //the band are their cards in THE BAND row, an enemy stands on the floor (BattleScreen.Stage.cs).
        private Rectangle BoxOf(Fighter f)
        {
            if (f.IsEnemy) return EnemyBox(f.Slot);
            return f == fore ? foreBox : ChipBox(f.Slot);
        }

        //Draw Box : where a fighter is drawn this frame. The musician in front slides in and out (SWAP), lunges
        //at its enemy in an ATTACK and is knocked back by a hit. An enemy is knocked back by a hit.
        private Rectangle DrawBox(Fighter f)
        {
            Rectangle r = BoxOf(f);
            Look look;
            if (looks.TryGetValue(f, out look) && look.Knock > 0f) r.X += (int)((f.IsEnemy ? 14f : -18f) * look.Knock * look.Knock);
            if (!f.IsEnemy && f == fore)
            {
                r.X += (int)SwapOffset();
                if (view == View.Strike && f == striker)
                {
                    float lunge = Lunge(MathHelper.Clamp(strikeTime / BattleRules.TurnStrikeTime, 0f, 1f));
                    r.X += (int)(lunge * 120f);
                    r.Y -= (int)(Math.Max(0f, lunge) * 14f);
                }
                r.Y += (int)(Math.Sin(worldTime * 2.2f) * 2f);                  // breathing, one pixel of the art
            }
            return r;
        }

        //Lunge (Beta1 round 4) : an ATTACK's move, smoother and with weight. A small pull back (ANTICIPATION), a fast dash
        //that arrives as the hit lands, a moment held there, then an eased walk back. p is 0..1 of TurnStrikeTime,
        //the answer is how far forward (1 = all the way, below 0 = pulled back).
        private static float Lunge(float p)
        {
            float hit = BattleRules.TurnStrikeImpact / BattleRules.TurnStrikeTime;
            float wind = hit * 0.45f;
            if (p < wind) return -0.15f * Smooth(p / wind);
            if (p < hit)
            {
                float t = (p - wind) / (hit - wind);
                float u = 1f - t;
                return MathHelper.Lerp(-0.15f, 1f, 1f - u * u * u);
            }
            float hold = hit + 0.14f;
            if (p < hold) return 1f;
            return 1f - Smooth((p - hold) / (1f - hold));
        }

        //Smooth : 0..1 eased at both ends (smoothstep)
        private static float Smooth(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        //Bench Box : a long BENCH makes the chips narrower, so the row never runs under the SIGNATURE and
        //PLAY THE BAR boxes (a click there played the bar instead of calling, step 13)
        private Rectangle BenchBox(int i)
        {
            int step = Math.Min(BenchStep, (signatureBox.X - 16 - BenchX) / Math.Max(1, battle.Bench.Count));
            return new Rectangle(BenchX + i * step, BenchY, step - (BenchStep - BenchW), BenchH);
        }

        //Fight Over : every musician on the field keeps the life they end with (KNOCKED OUT gets up with 1),
        //the ones on the BENCH keep theirs, and the old duel's record carries the result to the result page,
        //which hands out the rewards. Line is set to the old duel's full win, which keeps its shards the same.
        //The rank reads the PERFECTs (BattleState.Rank : a win is B, with 6 PERFECTs or more S).
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
            for (int i = 0; i < battle.Bench.Count; i++)                                    // GREEN ROOM : the BENCH may have healed
                if (battle.BenchLife[i] >= 0) run.SetLife(battle.Bench[i], battle.BenchLife[i]);
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

        //Learn : one of the five lessons is done
        private void Learn(Lesson lesson)
        {
            if (learned[(int)lesson]) return;
            learned[(int)lesson] = true;
            learnedCount++;
            SoundBank.Play(Sfx.ComboUp, 1f, 0.5f);
        }

        //Practice Help : enough BREATH for the skill lesson, then for a CALL, then a full gauge for the SIGNATURE lesson
        private void PracticeHelp()
        {
            if (!learned[(int)Lesson.Play]) return;
            if (!learned[(int)Lesson.Skill]) battle.Breath = Math.Max(battle.Breath, 4);
            else if (!learned[(int)Lesson.Call] && battle.Bench.Count > 0) battle.Breath = Math.Max(battle.Breath, battle.CallCost);
            else if (!learned[(int)Lesson.Signature]) battle.Signature = battle.SignatureNeed;
        }

        //Update Coach : the card says what to do next. While a swipe runs it explains that swipe,
        //only until that kind is learned.
        private void UpdateCoach()
        {
            if (view == View.Swipe)
            {
                if (swipeFor == SwipeFor.Parry && !learned[(int)Lesson.Parry]) coachLine = CoachParry;
                else if (swipeFor == SwipeFor.Skill && !learned[(int)Lesson.Skill]) coachLine = CoachSkillSwipe;
                else if (swipeFor == SwipeFor.Signature && !learned[(int)Lesson.Signature]) coachLine = CoachSignatureSwipe;
            }
            else if (view == View.Plan || view == View.Target)
            {
                if (!learned[(int)Lesson.Play]) coachLine = CoachPlay;
                else if (!learned[(int)Lesson.Skill]) coachLine = CoachSkill;
                else if (!learned[(int)Lesson.Call] && battle.Bench.Count > 0) coachLine = CoachCall;
                else if (!learned[(int)Lesson.Signature]) coachLine = CoachSignature;
                else coachLine = CoachWin;
            }
        }

        //Coach Card : PRACTICE, the lessons done as diamonds, the line, and how to leave
        private void DrawCoach(SpriteBatch sb)
        {
            Ui.Panel(sb, coachBox, 1f);
            Ui.Tag(sb, "PRACTICE", coachBox.X + 18, coachBox.Y + 10, true, 1f);
            Ui.Pips(sb, coachBox.X + 26, coachBox.Y + 44, learnedCount, learned.Length, 5f, 15f, 1f);
            Gfx.Text(sb, Game.StoryFont, coachLine, coachBox.X + 130, coachBox.Y + 16, Palette.Paper, TextSize.StorySmall);
            Ui.KeyChipRight(sb, "ESC", coachBox.Right - 14, coachBox.Y + 30, Palette.PaperDim);
        }

        //Picture Hook : for --shots, jump straight to the moment the picture is about
        public void ShowForPicture(string name)
        {
            NextStep();                                                                 // the first bar is being written

            if (name == "battleplan")
            {
                //A written bar : the front musician's first skill, the next one ATTACKs
                battle.Breath = 10;
                Fighter front = battle.Party[0];
                Skill s = SkillList.First(front.Musician);
                Fighter at = s.Target == SkillTarget.Enemy ? FirstStanding() : (s.Target == SkillTarget.Ally ? front : null);
                battle.PlanSkill(front, s, at);
                if (battle.Party.Count > 1) battle.PlanAttack(battle.Party[1], battle.Enemies[battle.Enemies.Count - 1]);
                Select(NextUnwritten(battle.Party[0]));
                RefreshPreview();
            }
            else if (name == "battleswing" || name == "battlestrike" || name == "battlekill")
            {
                if (name == "battlekill")                                               // Beta1 round 4 : the blow SILENCES
                    for (int i = 0; i < battle.Enemies.Count; i++) battle.Enemies[i].Hp = 1;
                battle.PlayBar();
                NextStep();
                strikeTime = BattleRules.TurnStrikeImpact * 0.9f;
            }
            else if (name == "battleskill")
            {
                battle.Breath = 10;
                Fighter front = battle.Party[0];
                Skill s = SkillList.First(front.Musician);
                Fighter at = s.Target == SkillTarget.Enemy ? FirstStanding() : (s.Target == SkillTarget.Ally ? front : null);
                battle.PlanSkill(front, s, at);
                battle.PlayBar();
                NextStep();
                clock = RingTime * 0.6f;
            }
            else if (name == "battlesig" || name == "battlecutin")
            {
                battle.Kit = TurnKit.Beethoven;
                signatureName = "LIGHTNING";
                battle.Signature = battle.SignatureNeed;
                StartSignature();
                clock = RingTime * 0.6f;
                cutIn = name == "battlecutin" ? BattleRules.TurnCutInTime * 0.55f : 0f;  // CUT-IN : half way, or already over
            }
            else if (name == "battleparry")
            {
                battle.PlayBar();
                while (battle.Step == TurnStep.PlayerTurn) battle.Attack();
                battle.Events.Clear();
                StartParry();
                clock = RingTime * 0.75f;
            }
            else if (name == "battlecall" || name == "battlefull")
            {
                //CALL : one in from the BENCH, or (battlefull) everyone who fits, six on the field
                battle.Breath = 10;
                CallIn(0);
                while (name == "battlefull" && battle.Bench.Count > 0 && battle.Party.Count < BattleRules.TurnFieldMost)
                {
                    battle.CalledThisBar = false;
                    battle.Breath = 10;
                    CallIn(0);
                }
                if (name == "battlefull") battle.Breath = 6;
                RefreshPreview();
            }
            else if (name == "battlevoice" || name == "battlecaprice" || name == "battlefugue")
            {
                //Step 10 kits : FANNY's voice tag, CAPRICE's count, and an ECHO waiting on a musician
                battle.Kit = name == "battlevoice" ? TurnKit.Fanny : (name == "battlecaprice" ? TurnKit.Paganini : TurnKit.Bach);
                for (int i = 0; i < ConductorList.All.Length; i++)
                    if (ConductorList.All[i].Turn == battle.Kit) signatureName = ConductorList.All[i].TurnSignatureName;
                if (name == "battlecaprice") battle.CapriceTurns = 3;
                if (name == "battlefugue") battle.Party[0].Echo = 1f;
                battle.Breath = 0;                                                      // BORROWED BREATH shows its price
                battle.Events.Clear();
            }
            else if (name == "battlestatus")
            {
                battle.Enemies[0].Dissonance = 3;
                battle.Enemies[0].OffBeat = 60;
                battle.Party[0].Crescendo = 2;
                if (battle.Party.Count > 1) battle.Party[1].Mute = 1;
                battle.Party[0].Guard = true;
                battle.Events.Clear();
            }
            else if (name == "battleslash" || name == "battleshot" || name == "battlesmash" || name == "battlecross" || name == "battleink"
                     || name == "battlegold" || name == "battlefire")
            {
                //Beta1 round 4 : one blow of each style on the front enemy (or the front musician for INK), for pictures.
                //battlegold = a SLASH in GOLD with its sparkles, battlefire = a SHOT in RED with its flames.
                HitStyle style = name == "battleslash" || name == "battlegold" ? HitStyle.Slash : name == "battleshot" || name == "battlefire" ? HitStyle.Shot
                               : name == "battlesmash" ? HitStyle.Smash : name == "battlecross" ? HitStyle.Cross : HitStyle.Ink;
                HitColors colors = name == "battlegold" ? new HitColors(Palette.SpotGold, Palette.SpotGoldHot, HitFlair.Sparkle)
                                 : name == "battlefire" ? new HitColors(Palette.SpotRed, Palette.SpotRedHot, HitFlair.Flame) : HitColors.Plain;
                Fighter at = style == HitStyle.Ink ? battle.Party[0] : battle.Enemies[0];
                if (style == HitStyle.Ink) fore = at;
                Rectangle box = BoxOf(at);
                hits.Hit(style, box, 1f, colors);
                StageHit(box.Center.ToVector2(), style != HitStyle.Ink, 1f, colors.Tint);
                if (style != HitStyle.Ink) SpawnNumber(new Vector2(box.Center.X, box.Y - 34), 24, false, true, name == "battlegold" || name == "battlefire" ? colors.Tint : Palette.Highlight);
            }
            else if (name == "battlebolt" || name == "battleimpact" || name == "battleboltsoft")
            {
                if (name == "battleboltsoft") Settings.ReduceFlashing = true;                 // the bolt itself, without the turns
                if (name == "battleboltsoft") name = "battlebolt";
                //Beta1 round 6 : THE UNHEARING's LIGHTNING on every enemy (battlebolt), or a CROSS (battleimpact), as if the
                //SIGNATURE had just landed
                battle.Kit = name == "battlebolt" ? TurnKit.Beethoven : TurnKit.Mozart;
                for (int i = 0; i < battle.Enemies.Count; i++)
                {
                    Rectangle box = BoxOf(battle.Enemies[i]);
                    if (name == "battlebolt") hits.HitLater(HitStyle.Lightning, box, 1f, SignatureColors(), BattleRules.HitBoltStagger * i);
                    else if (i == 0) hits.Hit(HitStyle.Cross, box, 1f, HitColors.Plain);
                    SpawnNumber(new Vector2(box.Center.X, box.Y - 34), 14, false, true, SignatureColors().Tint);
                }
                AddStageLight(new Vector2(TacetGame.ScreenW / 2f, 300f), 1100f, BattleRules.HitBoltFlash, 0.4f, Color.Lerp(Color.White, SignatureColors().Tint, 0.6f));
                hitStop = BattleRules.HitImpactHold;
            }
            else if (name == "battlewin" || name == "battlelose")
            {
                //Beta1 round 4 : TACET's dark when one side is nearly beaten (battlewin = TACET is, battlelose = the band is)
                List<Fighter> low = name == "battlewin" ? battle.Enemies : battle.Party;
                for (int i = 0; i < low.Count; i++) low[i].Hp = Math.Max(1, low[i].MaxHp / 8);
                battle.Events.Clear();
            }
            else if (name == "practiceskill" || name == "practicecall" || name == "practicesig")
            {
                Learn(Lesson.Play);                                                     // PRACTICE
                Learn(Lesson.Parry);
                if (name != "practiceskill") Learn(Lesson.Skill);
                if (name == "practicesig") Learn(Lesson.Call);
                EnterPlan();
            }
        }

        //======================================================================================
        //Draw
        //======================================================================================

        public override void Draw(SpriteBatch sb)
        {
            bool swiping = view == View.Swipe;
            bool planning = view == View.Plan || view == View.Target;

            //The World : finished before the frame (BattleScreen.Stage.cs PreDraw, Core/PostChain.cs). The IMPACT FRAME
            //punches the camera in round the blow for a moment.
            if (PostChain.Final != null)
            {
                float zoom = 1f + hits.ImpactZoom * BattleRules.HitImpactZoom;
                Vector2 at = ImpactOnScreen();
                sb.Draw(PostChain.Final, at, null, Color.White, 0f, at, zoom, SpriteEffects.None, 0f);
            }
            else Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageDeep);

            //Panel : a dark band along the bottom, fading in from above, so the musician in front stands into it
            for (int i = 0; i < 8; i++)
                Gfx.Rect(sb, 0, PanelTop - 64 + i * 8, TacetGame.ScreenW, 8, Palette.Panel * (0.11f * (i + 1)));
            Gfx.Rect(sb, 0, PanelTop, TacetGame.ScreenW, TacetGame.ScreenH - PanelTop, Palette.Panel * 0.94f);
            Gfx.Rect(sb, 0, PanelTop, TacetGame.ScreenW, 1, Palette.LineGrey * 0.3f);

            DrawBand(sb, planning);
            DrawEnemyPlates(sb);
            DrawBarStrip(sb, planning);
            if (view != View.Over) DrawIntent(sb, planning, 1f - FocusEased);     // fades out as a ring comes up (round 4)

            //QTE FOCUS : the world behind is already dark, grey and soft (PostChain), the ring and the notes come on top
            if (swiping)
            {
                //QTE LAYER : a soft dark pool behind the ring, so the ring reads on any picture
                Gfx.DrawGlow(sb, HitX, HitY, 340f, Color.Black * (0.6f * FocusEased));
                DrawHitPoint(sb);
                if (swipeFor == SwipeFor.Parry) DrawParryNotes(sb);
                else DrawPlayNotes(sb);
            }
            DrawQteLight(sb);

            effects.DrawPops(sb, Game.Font);
            DrawNumbers(sb);

            if (judgeLife > 0f)
                Gfx.TextSpacedCentered(sb, Game.BigFont, judgeWord, HitX, HitY + 110f, Palette.Highlight * Math.Min(1f, judgeLife * 2f), TextSize.Subtitle, 3f);

            if (!planning && !swiping && barHits > 0) DrawBarCount(sb);
            DrawBottom(sb, planning);
            if (practice) DrawCoach(sb);

            if (view == View.Over)
            {
                float a = Math.Min(1f, viewTime * 3f);
                Gfx.Rect(sb, 0, 300, TacetGame.ScreenW, 120, Palette.Void * (0.8f * a));
                Gfx.TextCentered(sb, Game.LogoFont, bannerText, TacetGame.ScreenW / 2f, 360, Palette.Highlight * a, TextSize.Banner);
            }

            if (swiping)
            {
                if (cutIn > 0f) DrawCutIn(sb);
                else
                {
                    DrawRuler(sb);
                    baton.Draw(sb, gesture);
                }
            }
        }

        //THE BAND (Beta1) : everyone on the field as a card in a row under the bar strip, until the user's face GUI.
        //The name, the life, the move written in the plan and its place in the bar, the statuses under the card.
        //The card of the musician in front has a bright frame.
        private void DrawBand(SpriteBatch sb, bool planning)
        {
            bool picking = view == View.Target && pending != null && pending.Target == SkillTarget.Ally;

            for (int i = 0; i < battle.Party.Count; i++)
            {
                Fighter f = battle.Party[i];
                Rectangle r = ChipBox(i);
                float a = f.Down ? 0.4f : 1f;

                Gfx.Rect(sb, r, Palette.Panel * 0.9f);
                Gfx.RectOutline(sb, r, (f == fore ? Palette.Paper : Palette.LineGrey * 0.45f) * a, 1);
                if (planning && selected == f) Gfx.Rect(sb, r.X, r.Bottom - 3, r.Width, 3, Palette.Accent);
                if (!planning && battle.Current == f) Gfx.Rect(sb, r.X, r.Bottom - 3, r.Width, 3, Palette.Highlight);
                bool outline = hover == f && (picking || (view == View.Plan && battle.CanPlan(f)));
                if (outline)
                    Gfx.RectOutline(sb, new Rectangle(r.X - 3, r.Y - 3, r.Width + 6, r.Height + 6), Palette.Accent, 2);

                Gfx.TextSpaced(sb, Game.Font, f.Name, r.X + 8, r.Y + 6, Palette.Paper * a, TextSize.Label, 1f);
                Gfx.Bar(sb, new Rectangle(r.X + 8, r.Y + 26, r.Width - 16, 5), f.Hp / (float)f.MaxHp, Palette.Stage * a, Palette.Paper * a);
                Gfx.Text(sb, Game.Font, NumberText.Get(f.Hp), r.X + 8, r.Y + 33, Palette.Highlight * a, TextSize.Label);
                Gfx.Text(sb, Game.Font, maxLabel[f], r.X + 40, r.Y + 34, Palette.LineGrey * a, TextSize.Tiny);

                //The Move : in the plan, what this musician will play and where in the bar
                if (planning && !f.Down)
                {
                    BarMove move = battle.MoveOf(f);
                    int at = previewMoves.IndexOf(move);
                    if (move != null)
                    {
                        Gfx.TextRight(sb, Game.Font, move.Skill == null ? WordAttack : move.Skill.Name, r.Right - 8, r.Y + 35, Palette.Highlight, TextSize.Tiny);
                        if (at >= 0) Gfx.TextRight(sb, Game.BigFont, NumberText.Get(OrderNumber(at)), r.Right - 8, r.Y + 2, Palette.Highlight, TextSize.Small);
                    }
                    else if (battle.CanPlan(f))
                        Gfx.TextRight(sb, Game.Font, WordAttack, r.Right - 8, r.Y + 35, Palette.LineGrey, TextSize.Tiny);   // nothing written : it ATTACKs
                }

                //Statuses : under the card, a filled tag hurts the one who has it, an outlined tag helps them
                float x = r.X;
                float y = r.Bottom + 5;
                if (f.JustCalled) x = DrawTagRow(sb, "NEXT BAR", 0, false, x, y);
                if (f.Dissonance > 0) x = DrawTagRow(sb, "DISSONANCE", f.Dissonance, true, x, y);
                if (f.Crescendo > 0) x = DrawTagRow(sb, "CRESCENDO", f.Crescendo, false, x, y);
                if (f.Mute > 0) x = DrawTagRow(sb, "MUTE", 0, true, x, y);
                if (f.Guard) x = DrawTagRow(sb, "GUARD", 0, false, x, y);
                if (f.Echo > 0f) x = DrawTagRow(sb, "ECHO", 0, false, x, y);                  // FUGUE

                //Target Mark : the musician an enemy means to hit next
                for (int e = 0; e < battle.Enemies.Count; e++)
                {
                    Fighter enemy = battle.Enemies[e];
                    bool aimed = enemy.IntentTarget == f || (enemy.IntentAll && !f.Down);                   // THE COURT : everybody
                    if (!enemy.Down && aimed && view != View.Swipe && view != View.Over)
                    {
                        float pulse = 0.6f + 0.4f * (float)Math.Sin(time * 5f);
                        Gfx.RectOutline(sb, r, Palette.Accent * pulse, 2);
                        Gfx.Triangle(sb, r.Center.X, r.Bottom + 2, 8f, false, Palette.Accent * pulse);
                        break;
                    }
                }

                Look look;
                if (looks.TryGetValue(f, out look) && look.Flash > 0f) Gfx.Rect(sb, r, Palette.Paper * (0.45f * look.Flash));
            }
        }

        //Tag Row : a status tag with its count, laid left to right. Returns where the next tag goes.
        private float DrawTagRow(SpriteBatch sb, string word, int count, bool filled, float x, float y)
        {
            Ui.Tag(sb, word, x, y, filled, 1f);
            float next = x + Ui.TagWidth(word) + 6f;
            if (count > 0)
            {
                Gfx.Text(sb, Game.Font, NumberText.Get(count), next - 2f, y, Palette.Paper, TextSize.Label);
                next += 16f;
            }
            return next;
        }

        //Order Number : a move's place among the band's moves of the bar (1, 2, 3 ...), enemies not counted
        private int OrderNumber(int index)
        {
            int n = 0;
            for (int i = 0; i <= index && i < previewOrder.Count; i++)
                if (!previewOrder[i].IsEnemy) n++;
            return n;
        }

        //Enemy Plate (Beta1) : the name and life above an enemy (CZN style), never higher than the band row
        private Rectangle EnemyPlate(Rectangle r)
        {
            int w = battle.Enemies.Count >= 3 ? 124 : Math.Min(220, Math.Max(150, r.Width - 60));
            int y = Math.Max(150, r.Y - 42);
            int x = Math.Max(8, Math.Min(TacetGame.ScreenW - 8 - w, r.Center.X - w / 2));
            return new Rectangle(x, y, w, 34);
        }

        private void DrawEnemyPlates(SpriteBatch sb)
        {
            bool aiming = view == View.Plan || (view == View.Target && !(pending != null && pending.Target == SkillTarget.Ally));
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                Fighter f = battle.Enemies[i];
                Rectangle r = BoxOf(f);
                Rectangle plate = EnemyPlate(r);
                float a = f.Down ? 0.35f * (1f - GoneOf(f)) : 1f;                     // SILENCED : the plate goes with it
                if (battle.Current != f) a *= MathHelper.Lerp(1f, 0.35f, FocusEased);     // QTE FOCUS : only the one attacking stays bright

                if (aiming && hover == f)
                    Gfx.RectOutline(sb, new Rectangle(r.X - 5, r.Y - 5, r.Width + 10, r.Height + 10), Palette.Accent, 2);
                if (battle.Current == f) Gfx.Rect(sb, plate.X, plate.Bottom + 2, plate.Width, 3, Palette.Accent);

                string name = f.Name.Length > 12 || plate.Width < 150 ? shortName[f] : f.Name;   // a long name, or a crowded row : the short one
                Gfx.Rect(sb, plate, Palette.Void * 0.6f);
                Gfx.TextSpaced(sb, Game.Font, name, plate.X + 6, plate.Y + 4, Palette.Paper * a, TextSize.Label, 1f);
                if (plate.Width >= 150)
                {
                    float maxWidth = Gfx.TextWidth(Game.Font, maxLabel[f], TextSize.Tiny);
                    Gfx.TextRight(sb, Game.Font, maxLabel[f], plate.Right - 6, plate.Y + 6, Palette.LineGrey * a, TextSize.Tiny);
                    Gfx.TextRight(sb, Game.Font, NumberText.Get(f.Hp), plate.Right - 10 - maxWidth, plate.Y + 4, Palette.Highlight * a, TextSize.Label);
                }
                else Gfx.TextRight(sb, Game.Font, NumberText.Get(f.Hp), plate.Right - 6, plate.Y + 4, Palette.Highlight * a, TextSize.Label);   // a crowded row : no room for the most
                Gfx.Bar(sb, new Rectangle(plate.X + 6, plate.Y + 21, plate.Width - 12, 5), f.Hp / (float)f.MaxHp, Palette.Stage * a, Palette.Paper * a);
                if (f.Down) continue;

                //OFF-BEAT : a thin gauge under the life, a filled tag once the enemy is thrown
                Gfx.Bar(sb, new Rectangle(plate.X + 6, plate.Y + 28, plate.Width - 12, 3), f.OffBeat / (float)BattleRules.TurnOffBeatMax, Palette.Stage * a, Palette.PaperDim * a);
                float x = r.X + 10;
                float y = r.Bottom + 8;
                if (TurnBattle.CutsIn(f)) x = DrawTagRow(sb, "CUT IN", 0, false, x, y);
                if (f.Staggered) x = DrawTagRow(sb, "OFF-BEAT", 0, true, x, y);
                if (f.Dissonance > 0) x = DrawTagRow(sb, "DISSONANCE", f.Dissonance, true, x, y);
            }
        }

        //HP : a bar and the number, the number big enough to read at a glance
        private void DrawHp(SpriteBatch sb, Fighter f, int x, int y, int width, float a)
        {
            Gfx.Bar(sb, new Rectangle(x, y, width, 6), f.Hp / (float)f.MaxHp, Palette.Stage * a, Palette.Paper * a);
            Gfx.Text(sb, Game.Font, NumberText.Get(f.Hp), x, y + 9, Palette.Highlight * a, TextSize.Body);
            Gfx.TextRight(sb, Game.Font, maxLabel[f], x + width, y + 11, Palette.LineGrey * a, TextSize.Label);
        }

        //Enemy Intent : over its head, how hard each note hits and the notes themselves, the way and the size
        //of each, so a player can read the whole attack before it comes (like Slay the Spire).
        //CADENZA : a boss's big move says so above it. In the plan, the enemy under the pointer draws a line to its target.
        private void DrawIntent(SpriteBatch sb, bool planning, float ia)
        {
            if (ia <= 0.01f) return;
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                Fighter f = battle.Enemies[i];
                if (f.Down) continue;
                Rectangle r = EnemyPlate(BoxOf(f));                 // Beta1 : the intent sits above the name plate
                float cy = r.Y - 22;

                //LULLABY : this turn it rests
                if (f.IntentRest)
                {
                    Gfx.TextSpacedCentered(sb, Game.Font, "REST", r.Center.X, cy - 6, Palette.Paper * ia, TextSize.Body, 3f);
                    continue;
                }
                if (f.IntentNotes.Length == 0) continue;

                const float NoteStep = 28f;
                const float NumberRoom = 42f;                                          // the number, then room for a left pointer
                float row = f.IntentNotes.Length * NoteStep;
                float left = r.Center.X - (NumberRoom + row) / 2f;
                Gfx.Text(sb, Game.BigFont, NumberText.Get(f.IntentNotes[0]), left, cy - 16, Palette.Highlight * ia, TextSize.Small);
                for (int n = 0; n < f.IntentNotes.Length; n++)
                    NoteGlyph.Note(sb, Game.BigFont, left + NumberRoom + NoteStep * (n + 0.5f), cy, f.IntentShown[n], f.IntentWays[n], 0.46f, time, ia);
                if (f.IntentCadenza || f.IntentAll)
                    Gfx.TextSpacedCentered(sb, Game.Font, f.IntentAll ? "EVERYONE" : "CADENZA", r.Center.X, cy - 40, Palette.Highlight * (ia * (0.7f + 0.3f * (float)Math.Sin(time * 6f))), TextSize.Label, 3f);
                if (f.Staggered)
                    Gfx.Line(sb, left - 6, cy, left + NumberRoom + 6f + row, cy, Palette.Highlight * ia, 2f);   // OFF-BEAT : it loses its next turn, this move waits for the one after

                if (planning && hover == f && f.IntentTarget != null)
                {
                    Vector2 to = AimPoint(f.IntentTarget);
                    Gfx.Line(sb, r.Center.X, cy + 14, to.X, to.Y, Palette.Accent * (0.6f * ia), 2f);
                }
            }
        }

        //Bar Strip : the bar along the top, left to right. The band are filled plates with their move under the name,
        //TACET only an outline (a CUT IN sits in the middle of the band). In the plan it shows the bar as written so
        //far, while it plays the one playing now is lit and the ones done are dim. An enemy that will lose its turn
        //to OFF-BEAT is struck through.
        private void DrawBarStrip(SpriteBatch sb, bool planning)
        {
            List<Fighter> order = planning ? previewOrder : battle.Order;
            List<BarMove> moves = planning ? previewMoves : battle.OrderMoves;
            if (view == View.Swipe && swipeFor == SwipeFor.Signature) { order = previewOrder; moves = previewMoves; planning = true; }
            int count = order.Count;
            if (count > 0)
            {
                int w = Math.Max(30, Math.Min(108, 1096 / count - 6));             // up to 18 plates (FEATHER) still end before BAR
                float x = 24f;
                for (int i = 0; i < count; i++)
                {
                    Fighter f = order[i];
                    bool now = !planning && i == battle.OrderIndex && battle.Current == f;
                    bool done = !planning && (i < battle.OrderIndex || f.Down);
                    Rectangle r = new Rectangle((int)x, 10, w, now ? 48 : 42);
                    float a = done || f.Down ? 0.35f : 1f;

                    if (f.IsEnemy) Gfx.RectOutline(sb, r, (now ? Palette.Accent : Palette.PaperDim) * a, now ? 2 : 1);
                    else Gfx.Rect(sb, r, (now ? Palette.Highlight : Palette.PaperDim) * a);

                    Color ink = f.IsEnemy ? Palette.Paper : Palette.Ink;
                    Gfx.TextCentered(sb, Game.Font, shortName[f], r.Center.X, r.Y + 14, ink * a, TextSize.Label);
                    string word = f.IsEnemy ? (TurnBattle.CutsIn(f) ? "CUT IN" : "") : (i < moves.Count && moves[i] != null && moves[i].Skill != null ? moves[i].Skill.Name : WordAttack);
                    if (word.Length > 0) Gfx.TextCentered(sb, Game.Font, word, r.Center.X, r.Y + 31, ink * (0.8f * a), TextSize.Tiny * 0.85f);
                    if (f.Staggered && !done) Gfx.Line(sb, r.X + 4, r.Center.Y, r.Right - 4, r.Center.Y, Palette.Highlight, 2f);
                    x += w + 6f;
                }
            }

            Gfx.TextSpacedRight(sb, Game.Font, "BAR", TacetGame.ScreenW - 64, 22, Palette.LineGrey, TextSize.Label, 3f);
            Gfx.Text(sb, Game.BigFont, NumberText.Get(battle.Round), TacetGame.ScreenW - 52, 12, Palette.Highlight, TextSize.Small);

            //TWO VOICES (THE UNSIGNED) : whose voice leads the next musician's move
            if (battle.Kit == TurnKit.Fanny)
            {
                Gfx.TextSpacedRight(sb, Game.Font, "VOICE", TacetGame.ScreenW - 120, 62, Palette.LineGrey, TextSize.Label, 3f);
                Gfx.TextSpacedRight(sb, Game.Font, battle.FelixVoice ? "FELIX" : "FANNY", TacetGame.ScreenW - 24, 60, Palette.Highlight, TextSize.Body, 2f);
            }

            //DECREE (THE DEAF CROWN) : what this bar forbids
            if (battle.Decree != DecreeKind.None)
            {
                Gfx.TextSpacedRight(sb, Game.Font, "DECREE", TacetGame.ScreenW - 24, 92, Palette.LineGrey, TextSize.Label, 3f);
                Gfx.TextSpacedRight(sb, Game.Font, battle.Decree == DecreeKind.NoSkills ? "NO SKILLS" : "NO ATTACKS", TacetGame.ScreenW - 24, 110,
                                    Palette.Accent * (0.75f + 0.25f * (float)Math.Sin(time * 5f)), TextSize.Body, 2f);
            }

            //CAPRICE (THE HEARTLESS) : turns left with no MISS
            if (battle.CapriceTurns > 0)
            {
                Gfx.TextSpacedRight(sb, Game.Font, "CAPRICE", TacetGame.ScreenW - 64, 62, Palette.LineGrey, TextSize.Label, 3f);
                Gfx.Text(sb, Game.BigFont, NumberText.Get(battle.CapriceTurns), TacetGame.ScreenW - 52, 52, Palette.Highlight, TextSize.Small);
            }
        }

        //Bar Count : the hits the band landed this bar and their total, growing with each hit (game feel)
        private void DrawBarCount(SpriteBatch sb)
        {
            float grow = 1f + 0.25f * barPulse;
            Gfx.TextSpacedRight(sb, Game.Font, "HITS", 600, 82, Palette.LineGrey, TextSize.Label, 3f);
            Gfx.Text(sb, Game.BigFont, barHitsText, 612, 70, Palette.Paper, TextSize.Small * grow);
            Gfx.TextSpacedRight(sb, Game.Font, "BAR TOTAL", 760, 82, Palette.LineGrey, TextSize.Label, 3f);
            Gfx.Text(sb, Game.BigFont, barDamageText, 772, 66, Palette.Highlight, TextSize.Subtitle * grow);
        }

        private void DrawHitPoint(SpriteBatch sb)
        {
            float a = FocusEased;
            Gfx.Circle(sb, HitX, HitY, NoteGlyph.WellRadius, Palette.Void * (0.85f * a));
            Gfx.CircleOutline(sb, HitX, HitY, NoteGlyph.WellRadius, Palette.Paper * (0.3f * a), 2f);
        }

        //Ring Progress : 0 when a note's ring appears, 1 when it is due
        private float Progress(QteNote n)
        {
            return 1f - (n.Due - clock) / RingTime;
        }

        //Play Notes : the skill or the SIGNATURE. The note being played sits on the hit point with its ring,
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
            Rectangle from = BoxOf(enemy);

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

            //Target : a line from the hit point to the musician being attacked (THE COURT : to every one standing)
            for (int i = 0; i < battle.Party.Count; i++)
            {
                Fighter m = battle.Party[i];
                if (m != enemy.IntentTarget && !(enemy.IntentAll && !m.Down)) continue;
                Vector2 to = AimPoint(m);
                Gfx.Line(sb, HitX - NoteGlyph.WellRadius, HitY, to.X, to.Y, Palette.Accent * 0.35f, 2f);
            }
        }

        //Aim Point (Beta1) : where a line aimed at a musician ends. The one in front is aimed at the chest,
        //the others at the bottom of their card in THE BAND row.
        private Vector2 AimPoint(Fighter m)
        {
            if (m == fore) return new Vector2(foreBox.Center.X + 40f, foreBox.Y + 130f);
            Rectangle chip = ChipBox(m.Slot);
            return new Vector2(chip.Center.X, chip.Bottom + 2f);
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

        //Bottom Bar : in the plan, the selected musician's commands, the BENCH and a line about what is under
        //the pointer. While the bar plays, who is playing what. Always BREATH and the SIGNATURE on the right.
        private void DrawBottom(SpriteBatch sb, bool planning)
        {
            if (planning)
            {
                if (selected != null && battle.CanPlan(selected))
                {
                    Gfx.TextSpaced(sb, Game.BigFont, selected.Name, 40, 606, Palette.Highlight, TextSize.Subtitle, 3f);
                    BarMove move = battle.MoveOf(selected);
                    for (int k = 0; k < 3; k++)
                    {
                        Skill s = commandSkill[k];
                        bool usable = k == 0 || battle.CanPlanSkill(selected, s);
                        bool lit = (move != null && move.Skill == s) || (view == View.Target && pending == s);
                        Ui.Button(sb, commandBox[k], k == 0 ? WordAttack : s.Name, commandKey[k], lit, usable, 1f);
                        if (k > 0) Ui.Pips(sb, commandBox[k].X + 26, commandBox[k].Bottom - 9, s.Cost, s.Cost, 4f, 11f, usable ? 1f : 0.35f);
                    }
                }

                //BENCH : who can be CALLED, and what it costs
                if (battle.Bench.Count > 0)
                {
                    Gfx.TextSpaced(sb, Game.Font, "BENCH", 392, BenchY, Palette.LineGrey, TextSize.Label, 3f);
                    Gfx.TextSpaced(sb, Game.Font, "CALL", 392, BenchY + 17, Palette.PaperDim, TextSize.Label, 2f);
                    Gfx.Text(sb, Game.BigFont, NumberText.Get(battle.CallCost), 446, BenchY + 8, Palette.Highlight, TextSize.Small);
                    for (int i = 0; i < battle.Bench.Count; i++)
                        Ui.Button(sb, BenchBox(i), battle.Bench[i].Name, "", hoverBench == i && battle.CanCall(i), battle.CanCall(i), 1f);
                }

                //Info Line : one line only. MUTE first, then what is under the pointer, then what to do.
                string line = HintPlan;
                Color ink = Palette.LineGrey;
                if (view == View.Target) line = pending != null && pending.Target == SkillTarget.Ally ? HintAlly : HintEnemy;
                else if (hover != null && !hover.IsEnemy && hover.JustCalled) line = HintCalled;
                else if (selected != null && selected.Mute > 0) line = HintMute;
                else if (battle.Decree != DecreeKind.None) line = battle.Decree == DecreeKind.NoSkills ? HintNoSkills : HintNoAttacks;   // DECREE
                if (hoverSkill != null && view == View.Plan)                               // while aiming, the line says how to aim
                {
                    line = hoverSkill.Text;
                    ink = Palette.Paper;
                }
                else if (hover != null && hover.IsEnemy && battle.TraitOn(hover))
                {
                    line = hover.Enemy.TurnTraitLine;                                                    // what the enemy's trait does
                    ink = Palette.Paper;
                }
                else if (hoverBench >= 0 && hoverBench < battle.Bench.Count)
                {
                    line = battle.Bench[hoverBench].TraitText;
                    ink = Palette.Paper;
                }
                else if (hover != null && !hover.IsEnemy && !hover.JustCalled && view == View.Plan)
                {
                    line = hover.Musician.TraitText;                                                     // step 15 : the ones on the field too
                    ink = Palette.Paper;
                }
                Gfx.Text(sb, Game.Font, line, 40, 700, ink, TextSize.Label);

                //PLAY : the bar is written
                Ui.Button(sb, playBox, "PLAY THE BAR", "ENTER", true, true, 1f);
            }
            else
            {
                Fighter f = view == View.Strike && striker != null ? striker : battle.Current;      // the ATTACK still lunging
                if (view == View.Swipe && swipeFor == SwipeFor.Signature) Gfx.TextSpaced(sb, Game.BigFont, signatureName, 40, 606, Palette.Highlight, TextSize.Subtitle, 3f);
                else if (f != null)
                {
                    Gfx.TextSpaced(sb, Game.BigFont, f.Name, 40, 606, f.IsEnemy ? Palette.Paper : Palette.Highlight, TextSize.Subtitle, 3f);
                    BarMove move = view == View.Strike ? strikeMove : battle.CurrentMove;
                    if (move != null && !f.IsEnemy) Gfx.TextSpaced(sb, Game.Font, move.Skill == null ? WordAttack : move.Skill.Name, 40, 648, Palette.PaperDim, TextSize.Body, 2f);
                }

                if (view == View.Swipe)
                {
                    string line = swipeFor == SwipeFor.Parry ? HintParry : HintSwing;
                    if (holding) line = HintHold;
                    else if (rolling) line = HintRoll;
                    Gfx.TextSpaced(sb, Game.Font, line, 40, 680, Palette.PaperDim, TextSize.Label, 2f);
                }
            }

            //BREATH : the big number is what is free to write more moves with, the skills written already wait in "IN THE BAR"
            int free = Math.Max(0, battle.BreathFree);
            Gfx.TextSpaced(sb, Game.Font, "BREATH", BreathX, BreathY, Palette.LineGrey, TextSize.Label, 3f);
            Ui.Pips(sb, BreathX + 6, BreathY + 24, planning ? free : battle.Breath, BattleRules.TurnBreathMax, 7f, 20f, 1f);
            Gfx.Text(sb, Game.BigFont, NumberText.Get(planning ? free : battle.Breath), BreathX + 214, BreathY + 4, Palette.Highlight, TextSize.Small);
            if (planning && battle.Reserved > 0)
            {
                Gfx.TextSpacedRight(sb, Game.Font, "IN THE BAR", BreathX + 180, BreathY, Palette.PaperDim, TextSize.Label, 2f);
                Gfx.Text(sb, Game.Font, NumberText.Get(battle.Reserved), BreathX + 188, BreathY, Palette.Paper, TextSize.Label);
            }

            //SIGNATURE Gauge : one diamond per PERFECT. Full, it glows and SPACE plays it while the bar is written.
            if (battle.Kit != TurnKit.None)
            {
                bool ready = battle.Signature >= battle.SignatureNeed;
                float glow = ready ? 0.6f + 0.4f * (float)Math.Sin(time * 6f) : 1f;
                if (ready && view == View.Plan) Gfx.RectOutline(sb, signatureBox, Palette.Accent * glow, 1);
                Gfx.TextSpaced(sb, Game.Font, signatureName, signatureBox.X + 6, signatureBox.Y + 4, (ready ? Palette.Highlight : Palette.LineGrey) * glow, TextSize.Label, 3f);
                Ui.Pips(sb, signatureBox.X + 12, signatureBox.Y + 28, battle.Signature, battle.SignatureNeed, 6f, 20f, 1f);
                if (ready) Ui.KeyChipRight(sb, "SPACE", signatureBox.Right - 6, signatureBox.Y + 28, Palette.Highlight * glow);
            }
        }
    }
}
