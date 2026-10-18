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
    //BattleScreen : the turn based fight (Alpha 7, step 2 of docs/ALPHA7_DESIGN.md).
    //The rules live in Battle/TurnBattle.cs. This page only shows them and reads the baton.
    //   CHOOSE  a musician is up : click an enemy (or press 1) to attack it
    //   SWING   the attack note sits on the hit point and the ring closes : swipe as it touches
    //   PARRY   an enemy is up : its notes fly to the hit point one by one, swipe back at each
    //   PAUSE   a short breath after a turn, so the result can be read
    //   OVER    the banner, then the result page
    //Layout (docs/research/ALPHA7_battle_ui.md) : one hit point in the middle so the eye never
    //has to look for it, the band on the left, TACET on the right, the turn order along the top
    //and the shared BREATH along the bottom. While a swipe is wanted the stage dims and the
    //pointer becomes the baton. The ring, the notes and the baton never shake.
    public class BattleScreen : GameScreen
    {
        private enum View { Choose, Swing, Parry, Pause, Over }

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

        private RunState run;
        private TurnBattle battle;
        private View view = View.Pause;
        private float viewTime;       // seconds in the current view
        private float clock;          // seconds since the swing or the enemy attack began
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

        //Swing : the attack being played
        private Fighter swingTarget;
        private int swingSize;
        private Flick swingWay;

        //Parry : the next note of the enemy attack to be answered
        private int parryNote;

        //Choose : the enemy under the pointer
        private Fighter hover;

        //Prepared Text : every word on this page is made in Load or Update, never in Draw
        private Dictionary<Fighter, string> shortName = new Dictionary<Fighter, string>();
        private Dictionary<Fighter, string> maxLabel = new Dictionary<Fighter, string>();
        private string judgeWord = "";
        private float judgeLife;
        private string bannerText = "";

        private const string HintChoose = "CLICK AN ENEMY TO ATTACK   /   1";
        private const string HintSwing = "SWIPE AS THE RING TOUCHES THE NOTE";
        private const string HintParry = "SWIPE BACK AT EACH NOTE";

        public override bool EntersWithWave
        {
            get { return true; }
        }

        public override void Load()
        {
            run = Game.CurrentRun;

            //The Band : the first three on stage, in seat order
            List<Musician> band = new List<Musician>();
            for (int s = 0; s < run.Formation.Seated.Length && band.Count < 3; s++)
                if (run.Formation.Seated[s] != null) band.Add(run.Formation.Seated[s]);
            for (int i = 0; i < run.Roster.Count && band.Count == 0; i++) band.Add(run.Roster[i]);   // nobody seated : never happens in a real run

            List<Enemy> enemies = new List<Enemy> { run.Battle.Enemy };
            battle = new TurnBattle(band, enemies, run.Floor, run.Rng);
            battle.Maestro = run.Maestro;                                                    // MAESTRO

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

            gesture.Clear();
            baton.Reset(Input.MousePos);
            Game.IsMouseVisible = true;
            SoundBank.PlayMusic(RunFlow.FightMusic(run));

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

        public override void Resumed()
        {
            gesture.Clear();
            Game.IsMouseVisible = view != View.Swing && view != View.Parry;
        }

        //Ring Time : how long a ring takes to close (MAESTRO is quicker)
        private float RingTime
        {
            get { return battle.Maestro ? BattleRules.MaestroTurnRingTime : BattleRules.TurnRingTime; }
        }

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

            bool swiping = view == View.Swing || view == View.Parry;
            gesture.Update(dt, swiping && Input.MouseDown());
            baton.Update(dt, gesture.Held);

            if (view == View.Choose) UpdateChoose();
            else if (view == View.Swing) UpdateSwing(dt);
            else if (view == View.Parry) UpdateParry(dt);
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
                Game.IsMouseVisible = true;
                return;
            }

            if (battle.Step == TurnStep.PlayerTurn)
            {
                view = View.Choose;
                viewTime = 0f;
                Game.IsMouseVisible = true;
            }
            else StartParry();
        }

        //Player Choose : click an enemy, or press 1 for the first one standing
        private void UpdateChoose()
        {
            hover = null;
            for (int i = 0; i < battle.Enemies.Count; i++)
                if (battle.CanTarget(battle.Enemies[i]) && Input.MouseOver(enemyBox[i])) hover = battle.Enemies[i];

            if (hover != null && Input.MouseClicked()) StartSwing(hover);
            else if (Input.KeyPressed(Keys.D1) || Input.KeyPressed(Keys.NumPad1) || Input.KeyPressed(Keys.Enter)) StartSwing(FirstStanding());
        }

        private Fighter FirstStanding()
        {
            for (int i = 0; i < battle.Enemies.Count; i++)
                if (battle.CanTarget(battle.Enemies[i])) return battle.Enemies[i];
            return null;
        }

        //Player Swing : the musician's own PART is the note to swipe (TurnBattle ATTACK PART)
        private void StartSwing(Fighter target)
        {
            if (target == null) return;
            swingTarget = target;
            swingSize = TurnBattle.AttackSize(battle.Current.Musician);
            swingWay = TurnBattle.AttackWay(battle.Current.Musician);
            hover = null;
            BeginSwipe(View.Swing);
        }

        private void BeginSwipe(View next)
        {
            view = next;
            viewTime = 0f;
            clock = 0f;
            gesture.Clear();
            baton.Reset(Input.MousePos);
            Game.IsMouseVisible = false;
        }

        private void UpdateSwing(float dt)
        {
            clock += dt;
            float now = clock - Settings.TimingOffset;

            if (gesture.Read())
            {
                float offset = now - RingTime;
                if (offset < -BattleRules.EarlyTime) return;          // the hand getting ready, not the swipe

                Grade grade = battle.Judge(offset, gesture.Direction == swingWay);
                bool rightSize = Baton.SizeOf(gesture.Length) == swingSize;
                baton.Whip(gesture.Direction);
                effects.SpawnSlash(gesture.From, gesture.To, grade == Grade.Perfect ? 1f : 0.55f);
                LandSwing(grade, rightSize);
            }
            else if (now > RingTime + battle.LateLimit)
            {
                LandSwing(Grade.Hesitate, true);                       // no swipe at all
            }
        }

        private void LandSwing(Grade grade, bool rightSize)
        {
            Rectangle box = enemyBox[swingTarget.Slot];
            int damage = battle.Attack(swingTarget, grade, rightSize);

            effects.SpawnHit(HitX, HitY, grade);
            effects.SpawnPop(box.Center.X, box.Y - 34, NumberText.Get(damage), Palette.Highlight, 1.1f);

            bool onTime = grade == Grade.Perfect || grade == Grade.Good;
            if (onTime && !rightSize) judgeWord = "WRONG SIZE";
            else if (grade == Grade.Perfect) judgeWord = "PERFECT";
            else if (grade == Grade.Good) judgeWord = "GOOD";
            else judgeWord = "MISS";
            judgeLife = 1f;

            if (grade == Grade.Perfect && rightSize)
            {
                perfects++;
                effects.SpawnShatter(HitX, HitY, RingMark);
                effects.SpawnSparks(box.Center.X, box.Center.Y, 14, 1f);
                hitStop = BattleRules.TurnHitStop;
                shake = 0.55f;
            }
            else if (onTime)
            {
                effects.SpawnSparks(box.Center.X, box.Center.Y, 5, 1f);
                shake = 0.25f;
            }

            EnterPause();
        }

        //Enemy Parry : the enemy's notes leave it one by one and reach the hit point TurnNoteGap apart
        private void StartParry()
        {
            parryNote = 0;
            BeginSwipe(View.Parry);
        }

        private float NoteTime(int note)
        {
            return RingTime + note * BattleRules.TurnNoteGap;
        }

        private void UpdateParry(float dt)
        {
            clock += dt;
            float now = clock - Settings.TimingOffset;
            Fighter enemy = battle.Current;

            if (parryNote < enemy.IntentNotes.Length)
            {
                float due = NoteTime(parryNote);
                if (gesture.Read())
                {
                    float offset = now - due;
                    if (offset >= -BattleRules.EarlyTime)
                    {
                        Grade grade = battle.Judge(offset, gesture.Direction == enemy.IntentWays[parryNote]);
                        bool rightSize = Baton.SizeOf(gesture.Length) == enemy.IntentSizes[parryNote];
                        if (grade == Grade.Perfect && !rightSize) grade = Grade.Good;     // the wrong size only half blocks
                        baton.Whip(gesture.Direction);
                        effects.SpawnSlash(gesture.From, gesture.To, grade == Grade.Perfect ? 1f : 0.55f);
                        LandNote(enemy, grade);
                    }
                }
                else if (now > due + battle.LateLimit)
                {
                    LandNote(enemy, Grade.Hesitate);
                }
            }

            if (parryNote >= enemy.IntentNotes.Length)
            {
                battle.FinishEnemyTurn();
                EnterPause();
            }
        }

        private void LandNote(Fighter enemy, Grade grade)
        {
            Fighter target = enemy.IntentTarget;
            int damage = battle.ParryNote(parryNote, grade);
            parryNote++;

            effects.SpawnHit(HitX, HitY, grade);
            judgeLife = 1f;

            //PARRY
            if (grade == Grade.Perfect)
            {
                judgeWord = "PARRY";
                perfects++;
                effects.SpawnShatter(HitX, HitY, RingMark);
                effects.SpawnSparks(HitX, HitY, 10, 1f);
                hitStop = BattleRules.TurnHitStop;
                return;
            }

            judgeWord = grade == Grade.Good ? "GOOD" : "MISS";
            if (target != null)
            {
                Rectangle box = partyBox[target.Slot];
                effects.SpawnPop(box.Center.X, box.Y - 34, NumberText.Get(damage), Palette.Paper, 1f);
            }
            shake = grade == Grade.Good ? 0.3f : 0.65f;
        }

        private void EnterPause()
        {
            view = View.Pause;
            viewTime = 0f;
            Game.IsMouseVisible = true;
        }

        //Fight Over : the old duel's record carries the result to the result page for now
        //(step 6 of the design moves rewards to the turn battle)
        private void Finish()
        {
            BattleState record = run.Battle;
            record.Finished = true;
            record.PlayerWon = battle.Step == TurnStep.Won;
            record.PerfectCount = perfects;
            Game.Screens.Change(new ResultScreen());
            view = View.Pause;
            viewTime = -999f;                     // nothing more happens while the page fades
        }

        //Picture Hook : for --shots, jump straight to the moment the picture is about
        public void ShowForPicture(string name)
        {
            if (name == "battleswing")
            {
                while (battle.Step == TurnStep.EnemyTurn) battle.TakeEnemyAttack(null);
                StartSwing(FirstStanding());
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
                NextTurn();
            }
        }

        //======================================================================================
        //Draw
        //======================================================================================

        public override void Draw(SpriteBatch sb)
        {
            bool swiping = view == View.Swing || view == View.Parry;

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
            DrawTurnOrder(sb);
            if (view == View.Choose || view == View.Pause) DrawIntent(sb);

            //Focus : the stage dims while a swipe is wanted
            if (swiping) Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.Void * 0.5f);

            if (swiping) DrawHitPoint(sb);
            if (view == View.Swing) DrawSwingNote(sb);
            if (view == View.Parry) DrawParryNotes(sb);

            effects.DrawShards(sb);
            effects.DrawHits(sb);
            effects.DrawSlashes(sb);
            effects.DrawPops(sb, Game.Font);

            if (judgeLife > 0f)
                Gfx.TextSpacedCentered(sb, Game.BigFont, judgeWord, HitX, HitY - 150f, Palette.Highlight * Math.Min(1f, judgeLife * 2f), TextSize.Subtitle, 3f);

            DrawBottom(sb);

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
            if (battle.Current == battle.Party[i] && view == View.Choose) r.Y -= 14;
            return r;
        }

        private void DrawPartyPlates(SpriteBatch sb)
        {
            for (int i = 0; i < battle.Party.Count; i++)
            {
                Fighter f = battle.Party[i];
                Rectangle r = partyBox[i];
                float a = f.Down ? 0.4f : 1f;
                bool current = battle.Current == f;

                if (current && view == View.Choose)
                    Gfx.Rect(sb, r.X - 6, r.Bottom + 6, r.Width + 12, 3, Palette.Accent);

                Gfx.TextSpacedCentered(sb, Game.Font, f.Name, r.Center.X, r.Bottom + 14, Palette.Paper * a, TextSize.Body, 2f);
                DrawHp(sb, f, r.X, r.Bottom + 44, r.Width, a);

                //Target Mark : the musician an enemy means to hit next
                for (int e = 0; e < battle.Enemies.Count; e++)
                {
                    Fighter enemy = battle.Enemies[e];
                    if (!enemy.Down && enemy.IntentTarget == f && (view == View.Choose || view == View.Pause))
                        Gfx.Triangle(sb, r.Center.X, r.Y - 30, 12f, false, Palette.Accent * (0.6f + 0.4f * (float)Math.Sin(time * 5f)));
                }
            }
        }

        private void DrawEnemies(SpriteBatch sb)
        {
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                Fighter f = battle.Enemies[i];
                Rectangle r = enemyBox[i];
                float a = f.Down ? 0.25f : 1f;

                ArtBank.DrawOrSlot(sb, ArtBank.EnemyOf(f.Enemy), r, Palette.Paper, a);
                if (hover == f) Gfx.RectOutline(sb, new Rectangle(r.X - 6, r.Y - 6, r.Width + 12, r.Height + 12), Palette.Accent, 2);
                if (battle.Current == f && view != View.Choose) Gfx.Rect(sb, r.X, r.Bottom + 6, r.Width, 3, Palette.Accent);

                Gfx.TextSpacedCentered(sb, Game.Font, f.Name, r.Center.X, r.Bottom + 14, Palette.Paper * a, TextSize.Body, 2f);
                DrawHp(sb, f, r.X, r.Bottom + 44, r.Width, a);
            }
        }

        //HP : a bar and the number, the number big enough to read at a glance
        private void DrawHp(SpriteBatch sb, Fighter f, int x, int y, int width, float a)
        {
            Gfx.Bar(sb, new Rectangle(x, y, width, 6), f.Hp / (float)f.MaxHp, Palette.Stage * a, Palette.Paper * a);
            Gfx.Text(sb, Game.Font, NumberText.Get(f.Hp), x, y + 10, Palette.Highlight * a, TextSize.Body);
            Gfx.TextRight(sb, Game.Font, maxLabel[f], x + width, y + 12, Palette.LineGrey * a, TextSize.Label);
        }

        //Enemy Intent : how hard each note hits and how many notes, over its head (like Slay the Spire)
        private void DrawIntent(SpriteBatch sb)
        {
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                Fighter f = battle.Enemies[i];
                if (f.Down || f.IntentNotes.Length == 0) continue;

                Rectangle r = enemyBox[i];
                float cy = r.Y - 30;
                float dots = f.IntentNotes.Length * 14f;
                float left = r.Center.X - (40f + dots) / 2f;
                Gfx.Text(sb, Game.BigFont, NumberText.Get(f.IntentNotes[0]), left, cy - 16, Palette.Highlight, TextSize.Small);
                for (int n = 0; n < f.IntentNotes.Length; n++)
                    Gfx.Circle(sb, left + 40f + n * 14f, cy, 4f, Palette.Paper);
            }
        }

        //Turn Order : everybody this round, left to right. The band are filled plates, TACET only
        //an outline. The one acting now is bigger and lit, the ones done are dim.
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

        private void DrawSwingNote(SpriteBatch sb)
        {
            float progress = clock / RingTime;
            float a = MathHelper.Clamp(progress * 3f, 0f, 1f);
            NoteGlyph.Note(sb, Game.BigFont, HitX, HitY, swingSize, swingWay, NoteGrow, time, a);
            NoteGlyph.TimingRing(sb, HitX, HitY, progress, RingMark, RingStart, 0f, a);
        }

        //Parry Notes : each note flies from the enemy to the hit point, arriving as its ring closes.
        //Only the next note to answer has a ring, so there is one thing to time at once.
        private void DrawParryNotes(SpriteBatch sb)
        {
            Fighter enemy = battle.Current;
            if (enemy == null || !enemy.IsEnemy) return;
            Rectangle from = enemyBox[enemy.Slot];

            for (int n = enemy.IntentNotes.Length - 1; n >= parryNote; n--)
            {
                float progress = (clock - (NoteTime(n) - RingTime)) / RingTime;
                if (progress < 0f) continue;

                float p = Math.Min(1f, progress);
                float x = MathHelper.Lerp(from.Center.X, HitX, p);
                float y = MathHelper.Lerp(from.Center.Y, HitY, p);
                float a = n == parryNote ? 1f : 0.55f;
                NoteGlyph.Note(sb, Game.BigFont, x, y, enemy.IntentSizes[n], enemy.IntentWays[n], MathHelper.Lerp(1f, NoteGrow, p), time, a);
                if (n == parryNote) NoteGlyph.TimingRing(sb, HitX, HitY, progress, RingMark, RingStart, 0f, Math.Min(1f, progress * 3f));
            }

            //Target : a line from the hit point to the musician being attacked
            if (enemy.IntentTarget != null)
            {
                Rectangle t = partyBox[enemy.IntentTarget.Slot];
                Gfx.Line(sb, HitX - NoteGlyph.WellRadius, HitY, t.Center.X, t.Y + 20, Palette.Accent * 0.35f, 2f);
            }
        }

        //Ruler : from where the swipe began, the way the note wants, with the p / mf / f zones
        private void DrawRuler(SpriteBatch sb)
        {
            if (!gesture.Held) return;
            Flick way = swingWay;
            if (view == View.Parry)
            {
                Fighter enemy = battle.Current;
                if (enemy == null || parryNote >= enemy.IntentWays.Length) return;
                way = enemy.IntentWays[parryNote];
            }

            Vector2 d = NoteGlyph.Way(way);
            Vector2 anchor = gesture.InStroke ? gesture.StrokeStart : Input.MousePos;
            float along = Vector2.Dot(gesture.LiveVector, d);
            Baton.DrawRuler(sb, anchor, d, along, gesture.InStroke ? Baton.SizeOf(along) : -1, true);
        }

        //Bottom Bar : who is up, what to do, and the band's BREATH
        private void DrawBottom(SpriteBatch sb)
        {
            float y = 640f;
            Fighter f = battle.Current;

            if (view == View.Choose && f != null)
            {
                Gfx.TextSpaced(sb, Game.BigFont, f.Name, 40, y - 20, Palette.Highlight, TextSize.Subtitle, 3f);
                Gfx.TextSpaced(sb, Game.Font, HintChoose, 40, y + 22, Palette.PaperDim, TextSize.Label, 2f);
            }
            else if (view == View.Swing)
            {
                Gfx.TextSpaced(sb, Game.Font, HintSwing, 40, y + 22, Palette.PaperDim, TextSize.Label, 2f);
            }
            else if (view == View.Parry && f != null)
            {
                Gfx.TextSpaced(sb, Game.BigFont, f.Name, 40, y - 20, Palette.Paper, TextSize.Subtitle, 3f);
                Gfx.TextSpaced(sb, Game.Font, HintParry, 40, y + 22, Palette.PaperDim, TextSize.Label, 2f);
            }

            Gfx.TextSpacedRight(sb, Game.Font, "BREATH", 900, y - 6, Palette.LineGrey, TextSize.Label, 3f);
            Ui.Pips(sb, 930, y, battle.Breath, BattleRules.TurnBreathMax, 8f, 24f, 1f);
            Gfx.Text(sb, Game.BigFont, NumberText.Get(battle.Breath), 1180, y - 16, Palette.Highlight, TextSize.Small);
        }
    }
}
