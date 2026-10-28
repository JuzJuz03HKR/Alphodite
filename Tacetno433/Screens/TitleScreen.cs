using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //TitleScreen : the first page of the game.
    //
    //Beta1 screens (28 Oct, docs/SCREENS_LEVEL_UP.md 3.4.8) : TACET's hole hangs on the right, the same dark as the
    //battle (Core/TacetVoid.cs), and a big thin ring round it runs down the left of the page. The menu sits on that ring,
    //so the lines step to the right as they go down : the menu orbits the silence. A smaller ring with ticks turns close
    //round the hole (the star map of menu4).
    //The left side is still a written score with a playhead running across it. When the playhead reaches a note the
    //note flashes, which is what the sequencer does during a duel. The staff runs off to the right and bends into TACET.
    //The page is drawn in two parts (THE LENS, see ScreenManager) : the world (score, rings, logo) is lit by a few stage
    //lights and glows, the words and the menu lie crisp on top of it.
    //The first time the game opens, the hole opens, the rings draw themselves, then the logo and the menu come in one
    //piece after another (TitleReveal, any key or click skips it). Coming back later, only the menu comes in.
    //A small notice strip at the bottom cycles through play tips, the way a launcher shows news.
    //When a run is saved, CONTINUE sits on top and says where the run was left.
    //Quitting, and starting over a saved run, both ask first.
    public class TitleScreen : GameScreen
    {
        //Can Pause : a menu page outside the run, ESC here means going back
        public override bool CanPause
        {
            get { return false; }
        }

        //Has World : the score, the rings and the logo are lit, the menu lies on top (THE LENS)
        public override bool HasWorld
        {
            get { return true; }
        }

        //Shows Grain : no specks over the words (the user found white specks cluttered, docs/SCREENS_LEVEL_UP.md 3.5)
        public override bool ShowsGrain
        {
            get { return false; }
        }

        //Title Menu Data : CONTINUE is only there when a run is saved
        private const int ItemContinue = 0;
        private const int ItemNewRun = 1;
        private const int ItemTutorial = 2;
        private const int ItemGuide = 3;
        private const int ItemSettings = 4;
        private const int ItemQuit = 5;
        private const int ItemArchive = 6;      // Alpha 7 : the after-run upgrades
        private static string[] allItems = { "CONTINUE", "NEW RUN", "TUTORIAL", "HOW TO PLAY", "SETTINGS", "QUIT", "ARCHIVE" };
        private static string[] allIndex = { "01", "02", "03", "04", "05", "06", "07" };
        private const string EnterKey = "ENTER";
        private int[] menu;                 // which items are on the menu, top to bottom
        private Rectangle[] menuBoxes;
        private Vector2[] itemAt;           // each line's circle, sitting on the big ring
        private UiMotion[] motion;          // each line's ink bar, ring and press (Core/UiMotion.cs)
        private int selected;
        private string continueNote = "";   // where the saved run was left
        private ConfirmBox askBox = new ConfirmBox();
        private int confirmFor = -1;        // the item waiting for a yes

        //Title Tips : the notice strip at the bottom, one every few seconds.
        //The turn battle (Alpha 7) has its own list, the old duel's is kept below it.
        private static string[] turnTips =
        {
            "A note TACET lands costs life. Block it as the ring closes.",
            "PERFECT blocks the whole note. GOOD blocks half.",
            "Block every note of an attack PERFECT and the musician hits back : COUNTER.",
            "Give each musician a move, then ENTER plays the bar in that order.",
            "ATTACK hits by itself. Only skills and blocks need a swing.",
            "f big, mf middle, p small. The wrong size hits weaker.",
            "Skills cost BREATH. ATTACKS and PERFECT blocks give it back.",
            "Names on the BENCH can be called in for BREATH, one a bar.",
            "A CUT IN enemy plays in the middle of your bar. Silence it early.",
            "Attacks and PERFECT blocks fill the enemy's OFF-BEAT bar. Full, it loses a turn.",
            "Every third attack most bosses play a CADENZA : more notes, and harder.",
            "PERFECTs fill the diamonds. Full, SPACE plays the SIGNATURE.",
            "A musician who falls gets up with 1 life after the fight.",
            "Every run leaves ECHOES. Spend them in the ARCHIVE.",
            "Strokes judged early or late? Settings has a STROKE TIMING test.",
        };

        private static string[] duelTips =
        {
            "A beat TACET wins costs breath. At zero, the band collapses.",
            "The whole band plays every note you hit.",
            "PERFECT strokes in a row build a combo. A miss breaks it.",
            "Swing a player's way (their arrow) and they hit harder.",
            "f big, mf middle, p small. The wrong size plays weaker.",
            "A ??? beat hides its strength until the clash.",
            "Motifs last the whole run. Pick the ones that suit your conductor.",
            "A PERFECT f against a real f note is a COUNTER. It throws part of the note back.",
            "A note tied to a spark : flick once more, half a beat later.",
            "Zigzag bar = roll. Shake as fast as you can.",
            "Note under an arch = hold. Stroke it, then keep still.",
            "Eight PERFECTs in a row set the band on fire. FORTISSIMO hits harder for four beats.",
            "Far enough ahead at the end of a round, conduct the FINALE and end the duel at once.",
            "Where TACET is silent, let the beat pass or stroke small to rest. A bigger swing is a free hit.",
            "Strokes judged early or late? Settings has a STROKE TIMING test.",
        };

        //Tips In Use : after both lists, since static fields are set in the order they are written
        private static string[] tips = BattleRules.TurnBattleOn ? turnTips : duelTips;
        private const float TipTime = 6f;

        //Title Layout : the logo top left, the menu down the big ring, the notice strip at the bottom
        private const float LogoX = 78f;
        private const float LogoY = 124f;
        private const float MenuTop = 372f;     // the first line's middle
        private int menuStep = 40;              // a little tighter with seven lines (Alpha 7 : ARCHIVE), see Load
        private const int NoticeY = 618;
        private const float InnerRing = 2.5f;   // the ring with ticks, this many times the hole's size

        //Title Lens : how the page is lit (the battle's numbers are in BattleRules, Stage Feel)
        private const float TitleAmbient = 0.3f;     // a dark stage ...
        private const float LogoLight = 0.62f;       // ... a light on the logo, so it reads white and glows ...
        private const float ChosenLight = 0.42f;     // ... one that follows the chosen line ...
        private const float HouseLight = 0.3f;       // ... and a wide soft one over the left half (power p adds p x p, PostChain.SpotLight)

        //Staff Layout : five lines, twelve pixels apart, so half a step is six pixels
        private const int StaffTop = 70;
        private const int StaffGap = 12;
        private const int StaffLeft = 80;
        private const int NoteFirstX = 110;
        private const int NoteStepW = 52;

        //Title Notation : the phrase written on the staff.
        //step is the slot along the staff, pitch is how far down the note head sits.
        private int[] noteStep = { 0, 1, 3, 4, 6, 7, 8, 10, 11, 13, 14, 16, 17, 19, 20 };
        private int[] notePitch = { 6, 4, 5, 3, 4, 2, 5, 3, 1, 4, 2, 5, 3, 4, 2 };

        //Opening : the first time the game opens the whole page reveals itself (BattleRules.TitleReveal)
        public static bool FirstOpen = true;
        private const float LaterStart = 0.9f;  // a later visit starts the opening here : the logo is up, the menu comes in
        private float clock;                    // seconds into the opening
        private bool revealing;                 // the first opening is playing, a key or a click skips it
        private bool opened;                    // the hole has thrown its first shock wave

        //Logo : TACET letter by letter, each letter's place measured once (Load)
        private static string[] logoLetters = { "T", "A", "C", "E", "T" };
        private float[] logoX = new float[5];

        //Title Animation
        private float time;
        private float playhead = StaffLeft;
        private Vector2 lastMouse;
        private Vector2 spot;                   // the light on the chosen line, easing after it

        public override void Load()
        {
            //No Run : the title page is outside any run
            Game.CurrentRun = null;

            //Menu Lines : CONTINUE first when there is a saved run, and it starts chosen
            SaveFile.CheckRunMode();                           // Alpha 7 : a run from the other battle is not offered
            bool saved = SaveFile.HasRun;
            if (saved) menu = new int[] { ItemContinue, ItemNewRun, ItemArchive, ItemTutorial, ItemGuide, ItemSettings, ItemQuit };
            else menu = new int[] { ItemNewRun, ItemArchive, ItemTutorial, ItemGuide, ItemSettings, ItemQuit };
            continueNote = saved ? SaveFile.RunSummary() : "";
            selected = 0;

            //Menu Places : each line's circle sits on the big ring, so the lines step right as they go down.
            //The clickable box, the motion of each line, all built once.
            menuStep = menu.Length > 6 ? 36 : 40;     // seven lines still clear the NOTE strip
            menuBoxes = new Rectangle[menu.Length];
            itemAt = new Vector2[menu.Length];
            motion = new UiMotion[menu.Length];
            for (int i = 0; i < menu.Length; i++)
            {
                float y = MenuTop + i * menuStep;
                itemAt[i] = new Vector2(OrbitX(y), y);
                menuBoxes[i] = new Rectangle((int)itemAt[i].X - 44, (int)(y - menuStep / 2f), 470, menuStep);
                motion[i] = new UiMotion();
            }

            //Logo Letters : where each letter starts inside the word, so the letters can come in one by one
            for (int i = 0; i < logoLetters.Length; i++)
                logoX[i] = Game.LogoFont.MeasureString("TACET".Substring(0, i)).X * TextSize.Logo;

            //TACET : the hole on the right, and the page's light
            Dark = new TacetVoid();
            Lens.Ambient = TitleAmbient;
            Lens.BloomThreshold = 0.66f;
            Lens.BloomStrength = 0.6f;
            Lens.Vignette = 0.6f;

            //Opening : the whole of it the first time, only the menu coming in after that
            revealing = FirstOpen;
            clock = FirstOpen ? 0f : LaterStart;
            opened = !FirstOpen;
            FirstOpen = false;

            spot = itemAt[selected];
            lastMouse = Input.MousePos;
            SoundBank.PlayMusic(Music.Title);
        }

        //Orbit X : where the big ring round the hole crosses the row y, on its left side
        private static float OrbitX(float y)
        {
            float dy = y - BattleRules.TitleHoleY;
            float r = BattleRules.TitleOrbit;
            return BattleRules.TitleHoleX - (float)Math.Sqrt(Math.Max(0f, r * r - dy * dy));
        }

        //Item In : how far menu line i has come in (ENTER), the lines one after another
        private float ItemIn(int i)
        {
            return UiMotion.Between(clock, LaterStart + i * BattleRules.UiEnterStagger, BattleRules.UiEnterTime);
        }

        public override void Update(float dt)
        {
            time += dt;
            clock += dt;
            int before = selected;

            //Playhead Sweep : runs left to right, then vanishes into TACET and starts over
            playhead += dt * 170f;
            if (playhead > TacetGame.ScreenW) playhead = StaffLeft;

            //TACET : calm, breathing slowly. The first time the hole opens, overshooting a little, and throws a shock wave.
            Vector2 hole = new Vector2(BattleRules.TitleHoleX, BattleRules.TitleHoleY);
            float open = UiMotion.EaseBack(MathHelper.Clamp(clock / 0.5f, 0f, 1f));
            float breath = 1f + 0.04f * (float)Math.Sin(time * 0.8f);
            Dark.Update(dt, 0.3f);
            Dark.PlaceOnPage(hole, BattleRules.TitleHoleRadius * open * breath, 0f, 0f, 0f);
            if (!opened && clock > 0.12f)
            {
                opened = true;
                Dark.Hit(hole + new Vector2(-1f, 0f), true, 0.8f);
            }
            spot += (itemAt[selected] - spot) * Math.Min(1f, dt * 10f);

            //Opening : a key or a click skips the rest of the first opening, and is not taken as a choice
            if (revealing)
            {
                if (clock >= BattleRules.TitleReveal) revealing = false;
                else if (Input.MouseClicked() || Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Space) || Input.KeyPressed(Keys.Escape))
                {
                    clock = BattleRules.TitleReveal;
                    revealing = false;
                    return;
                }
                else if (clock < LaterStart) return;               // nothing to choose yet
            }

            //Asking : while the box is open nothing else on the page moves
            if (askBox.Open)
            {
                int answer = askBox.Update(dt);
                if (answer == 1) Act(confirmFor);
                return;
            }

            //Menu Keyboard
            if (Input.KeyPressed(Keys.Down) || Input.KeyPressed(Keys.S))
                selected = (selected + 1) % menu.Length;
            if (Input.KeyPressed(Keys.Up) || Input.KeyPressed(Keys.W))
                selected = (selected - 1 + menu.Length) % menu.Length;

            //Menu Mouse : only take over the selection when the mouse actually moves,
            //otherwise a resting cursor would fight the arrow keys
            if (Input.MousePos != lastMouse)
            {
                lastMouse = Input.MousePos;
                for (int i = 0; i < menuBoxes.Length; i++)
                    if (Input.MouseOver(menuBoxes[i])) selected = i;
            }

            if (selected != before) SoundBank.Play(Sfx.UiMove);

            //POINT : the chosen line's ink bar shoots out once the line is in, the others pull theirs back
            for (int i = 0; i < motion.Length; i++)
                motion[i].Update(dt, i == selected && ItemIn(i) > 0.5f);

            //Menu Confirm
            bool confirm = Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Space);
            for (int i = 0; i < menuBoxes.Length; i++)
                if (Input.ClickedOn(menuBoxes[i])) { selected = i; confirm = true; }

            if (confirm)
            {
                motion[selected].Press();
                Choose(menu[selected]);
            }

            if (Input.KeyPressed(Keys.Escape)) Choose(ItemQuit);
        }

        //Menu Choose : the two lines that cannot be undone ask first
        private void Choose(int item)
        {
            if (item == ItemQuit)
            {
                Ask(item, "QUIT THE GAME?", "Your settings are kept. A run in progress is saved at its last path.", "QUIT", "STAY");
                return;
            }
            if (item == ItemNewRun && SaveFile.HasRun)
            {
                Ask(item, "START A NEW RUN?", "The saved run will be thrown away. There is only room for one.", "NEW RUN", "KEEP IT");
                return;
            }
            Act(item);
        }

        private void Ask(int item, string title, string line, string yes, string no)
        {
            confirmFor = item;
            askBox.Show(title, line, yes, no);
        }

        //Menu Action : what each line does
        private void Act(int item)
        {
            SoundBank.Play(Sfx.UiConfirm);

            if (item == ItemContinue)
            {
                //RING SWEEP : carrying on a run is one of the moments that matter, the light opens from the line's circle
                Game.Screens.SweepNext(itemAt[selected], RingSweep.Kind.Light);
                if (!RunFlow.Continue(Game))
                {
                    //Broken Save : a file from another version, or damaged. Start clean.
                    SaveFile.DeleteRun();
                    SoundBank.Play(Sfx.UiDenied);
                    Game.Screens.Change(new TitleScreen());
                }
            }
            else if (item == ItemNewRun)
            {
                SaveFile.DeleteRun();
                Game.Screens.Change(new ConductorSelectScreen());
            }
            else if (item == ItemTutorial)
                Game.Screens.Change(new TutorialScreen());
            else if (item == ItemGuide)
                Game.Screens.Change(new GuideScreen());
            else if (item == ItemSettings)
                Game.Screens.Change(new SettingsScreen());
            else if (item == ItemArchive)
                Game.Screens.Change(new ArchiveScreen());
            else
                Game.Exit();
        }

        //======================================================================================
        //The World : lit and glowing (THE LENS)
        //======================================================================================

        //Back : what TACET's hole bends. The dark stage, the margins of a printed programme, and the score.
        public override void DrawBack(SpriteBatch sb)
        {
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageDeep);
            Gfx.DrawGlowBox(sb, new Rectangle(-300, -200, 1300, 900), Palette.Paper * 0.06f);
            Gfx.DrawGlow(sb, 300, 250, 300, Palette.Paper * 0.05f);

            //Page Rules : thin lines like the margins of a printed programme
            Gfx.Rect(sb, 60, 30, 1, TacetGame.ScreenH - 60, Palette.Paper * 0.08f);
            Gfx.Rect(sb, 60, 30, 760, 1, Palette.Paper * 0.08f);

            DrawScore(sb);
        }

        //World : the rings round the hole, the dark bits it pulls in, and the logo
        public override void DrawWorld(SpriteBatch sb)
        {
            Dark.DrawDebris(sb, false);
            DrawRings(sb);
            DrawLogo(sb);
        }

        //Lights : a dark stage with a light on the logo and one following the chosen line (TACET adds its own)
        public override void DrawLights(SpriteBatch sb)
        {
            float up = UiMotion.Between(clock, 0f, 0.6f);
            Gfx.DrawGlowBox(sb, new Rectangle(-260, -120, 1060, 960), Color.White * (HouseLight * up));
            Gfx.DrawGlow(sb, 260, 190, 520, Color.White * (LogoLight * up));
            Gfx.DrawGlow(sb, spot.X + 140, spot.Y, 260, Color.White * (ChosenLight * ItemIn(selected)));
        }

        //Glow : a halo under the chosen line's circle, so it seems to light the ring it sits on
        public override void DrawGlow(SpriteBatch sb)
        {
            float e = motion[selected].Shown * ItemIn(selected);
            if (e > 0f) Gfx.DrawGlow(sb, itemAt[selected].X, itemAt[selected].Y, 30f, Color.White * (0.5f * e));
        }

        //Score : the staff, the written notes, and the playhead running across them
        private void DrawScore(SpriteBatch sb)
        {
            //Staff Lines : run all the way to the right edge, into TACET's hole
            Ornament.Stave(sb, StaffLeft, StaffTop, TacetGame.ScreenW - StaffLeft, StaffGap, Palette.LineGrey * 0.7f);

            //Bar Lines : a stroke every four slots, so it reads as real notation
            for (int step = 0; step <= 20; step += 4)
                Gfx.Rect(sb, NoteFirstX + step * NoteStepW - 26, StaffTop, 1, StaffGap * 4, Palette.LineGrey * 0.55f);

            //Treble Opening : a double bar at the very start of the staff
            Gfx.Rect(sb, StaffLeft, StaffTop, 3, StaffGap * 4, Palette.PaperDim);
            Gfx.Rect(sb, StaffLeft + 6, StaffTop, 1, StaffGap * 4, Palette.PaperDim);

            //Playhead : the needle. This is the same needle the duel page uses.
            Gfx.Rect(sb, playhead, StaffTop - 14, 2, StaffGap * 4 + 28, Palette.Paper * 0.55f);
            Gfx.DrawGlow(sb, playhead, StaffTop + StaffGap * 2, 46f, Palette.Paper * 0.10f);

            //Note Heads : flash as the playhead passes over them
            for (int i = 0; i < noteStep.Length; i++)
            {
                float x = NoteFirstX + noteStep[i] * NoteStepW;
                float y = StaffTop + notePitch[i] * (StaffGap / 2f);

                //Pulse : one when the playhead is exactly on the note, zero when far away
                float distance = Math.Abs(playhead - x);
                float pulse = 0f;
                if (distance < 40f) pulse = 1f - distance / 40f;

                Color noteColor = Color.Lerp(Palette.PaperDim, Palette.Highlight, pulse);

                Gfx.Rect(sb, x + 4, y - 22, 1, 22, noteColor * 0.8f);
                Gfx.Circle(sb, x, y, 4.5f + pulse * 2f, noteColor);

                if (pulse > 0.1f)
                    Gfx.DrawGlow(sb, x, y, 12f + pulse * 26f, Palette.Paper * (pulse * 0.30f));
            }
        }

        //Rings : the ring with ticks turning close round the hole, the big ring the menu sits on, and a faint one between.
        //The first time they draw themselves round.
        private void DrawRings(SpriteBatch sb)
        {
            float hx = BattleRules.TitleHoleX;
            float hy = BattleRules.TitleHoleY;

            //Inner Ring : starts from the top and goes round
            float inner = UiMotion.Between(clock, 0.15f, 0.6f);
            Ornament.TickRing(sb, hx, hy, BattleRules.TitleHoleRadius * InnerRing, 72, time * 0.03f, inner, Palette.Paper * 0.35f);

            //Middle Ring : faint, so the eye joins the hole to the menu
            float orbit = UiMotion.Between(clock, 0.25f, 0.7f);
            if (orbit > 0f) Gfx.Ring(sb, hx, hy, BattleRules.TitleOrbit * 0.56f, Palette.Paper * (0.06f * orbit), 1f);

            //Big Ring : only its left side is on the screen, drawn from the top down
            float r = BattleRules.TitleOrbit;
            float top = MathHelper.Pi + (float)Math.Asin(MathHelper.Clamp((hy + 10f) / r, -1f, 1f));
            float bottom = MathHelper.Pi - (float)Math.Asin(MathHelper.Clamp((TacetGame.ScreenH + 10f - hy) / r, -1f, 1f));
            float end = top - (top - bottom) * orbit;
            if (orbit <= 0f) return;
            Gfx.Arc(sb, hx, hy, r, top, end, Palette.Paper * 0.28f, 1f, (int)(r * (top - end) / 6f) + 1);

            //Ticks : out from the big ring, every fifth one longer, like a dial
            const float tick = 0.021f;                              // about 1.2 degrees apart
            int count = (int)((top - end) / tick);
            for (int i = 0; i <= count; i++)
            {
                float angle = top - i * tick;
                float cos = (float)Math.Cos(angle);
                float sin = (float)Math.Sin(angle);
                float length = i % 5 == 0 ? 9f : 4f;
                Gfx.Line(sb, hx + cos * (r + 2f), hy + sin * (r + 2f), hx + cos * (r + 2f + length), hy + sin * (r + 2f + length),
                         Palette.Paper * (i % 5 == 0 ? 0.3f : 0.16f), 1f);
            }
        }

        //Logo : the game name letter by letter, then 4'33 between its two lines
        private void DrawLogo(SpriteBatch sb)
        {
            for (int i = 0; i < logoLetters.Length; i++)
            {
                float a = UiMotion.Between(clock, 0.35f + i * 0.07f, 0.35f);
                if (a <= 0f) continue;
                float y = LogoY + (1f - a) * 14f;
                Gfx.Text(sb, Game.LogoFont, logoLetters[i], LogoX + 4f + logoX[i], y + 4f, Palette.Void * a, TextSize.Logo);   // drop shadow
                Gfx.Text(sb, Game.LogoFont, logoLetters[i], LogoX + logoX[i], y, Palette.Paper * a, TextSize.Logo);
            }

            //4'33 : the two lines grow out from it
            float line = UiMotion.Between(clock, 0.75f, 0.4f);
            if (line <= 0f) return;
            float ly = 248f;
            Gfx.Rect(sb, 144f - 60f * line, ly + 16f, 60f * line, 1, Palette.PaperDim * line);
            Gfx.TextSpaced(sb, Game.BigFont, "4'33", 156, ly, Palette.Paper * line, TextSize.Subtitle, 8f);
            Gfx.Rect(sb, 262f, ly + 16f, 180f * line, 1, Palette.PaperDim * line);
            Gfx.CircleOutline(sb, 262f + 180f * line + 5f, ly + 16f, 3f, Palette.PaperDim * line, 1f);
        }

        //======================================================================================
        //The Words : crisp on top of the world
        //======================================================================================

        public override void Draw(SpriteBatch sb)
        {
            DrawHeader(sb);
            DrawMenu(sb);
            DrawNotice(sb);
            DrawSide(sb);
            DrawFooter(sb);
            askBox.Draw(sb);
        }

        //Header : the company at the top, and the hook line under the logo
        private void DrawHeader(SpriteBatch sb)
        {
            float a = UiMotion.Between(clock, 1.2f, 0.4f);
            Gfx.TextSpaced(sb, Game.Font, "ALPHODITE", 80, 38, Palette.LineGrey * a, TextSize.Tiny, 4f);
            Gfx.TextSpaced(sb, Game.Font, BattleRules.TurnBattleOn ? "A BAND AGAINST SILENCE" : "A DUEL AGAINST SILENCE", 470, 38, Palette.LineGrey * a, TextSize.Tiny, 4f);
            Ornament.Crosses(sb, 740, 44, 4, 1, 16, Palette.LineGrey * a);

            float hook = UiMotion.Between(clock, 0.85f, 0.4f);
            Gfx.Text(sb, Game.StoryFont, "You do not play the notes.", 86, 296, Palette.Highlight * hook, TextSize.Story);
            Gfx.Text(sb, Game.StoryFont, "You lead the band that does.", 86, 318, Palette.PaperDim * hook, TextSize.Story);
        }

        //Menu : the lines on the big ring, sliding down it into place one after another
        private void DrawMenu(SpriteBatch sb)
        {
            for (int i = 0; i < menu.Length; i++)
            {
                float a = ItemIn(i);
                if (a <= 0f) continue;
                float y = itemAt[i].Y - (1f - a) * 30f;
                Vector2 at = new Vector2(OrbitX(y), y);
                float end = Ui.MenuItem(sb, at, allIndex[i], allItems[menu[i]], EnterKey, motion[i], a);

                //Continue Note : who, which floor, which stage, beside the line
                if (menu[i] == ItemContinue)
                    Gfx.TextSpaced(sb, Game.Font, continueNote, end + 18f, y - 7f, Palette.PaperDim * a, TextSize.Tiny, 2f);
            }
        }

        //Notice : the news strip, one tip at a time, fading between them
        private void DrawNotice(SpriteBatch sb)
        {
            float shown = UiMotion.Between(clock, 1.2f, 0.4f);
            if (shown <= 0f) return;
            int index = (int)(time / TipTime) % tips.Length;
            float phase = (time % TipTime) / TipTime;
            float a = phase < 0.1f ? phase / 0.1f : (phase > 0.9f ? (1f - phase) / 0.1f : 1f);

            Rectangle strip = new Rectangle(80, NoticeY, 720, 38);
            Gfx.Rect(sb, strip, Palette.Void * (0.6f * shown));
            Gfx.Rect(sb, strip.X, strip.Y, strip.Width, 1, Palette.LineGrey * (0.6f * shown));
            Gfx.CircleOutline(sb, strip.X, strip.Y, 3f, Palette.Paper * shown, 1f);
            Ui.Tag(sb, "NOTE", strip.X + 14, strip.Y + 10, true, shown);
            Gfx.Text(sb, Game.StoryFont, tips[index], strip.X + 76, strip.Y + 9, Palette.Paper * (a * shown), TextSize.StorySmall);

            //Tip Dots : which tip out of how many, the one on show a ring round a dot
            for (int i = 0; i < tips.Length; i++)
            {
                float x = strip.Right - 16 - (tips.Length - 1 - i) * 12;
                Gfx.Rect(sb, x - 1f, strip.Center.Y - 1f, 2f, 2f, (i == index ? Palette.Paper : Palette.LineGrey) * shown);
                if (i == index) Gfx.CircleOutline(sb, x, strip.Center.Y, 4f, Palette.Paper * shown, 1f);
            }
        }

        //Side : writing down the right edge and under TACET, the poster side of the page
        private void DrawSide(SpriteBatch sb)
        {
            float a = UiMotion.Between(clock, 1.2f, 0.4f);
            Gfx.TextVertical(sb, Game.Font, "SILENCE  IS  ALSO  A  SOUND", 1236, 90, Palette.PaperDim * (0.8f * a), TextSize.Label);
            Gfx.Rect(sb, 1222, 76, 1, 480, Palette.LineGrey * (0.4f * a));

            Gfx.TextRight(sb, Game.BigFont, "No.433", 1190, 600, Palette.Paper * (0.8f * a), TextSize.Subtitle);
            Ornament.Barcode(sb, 1060, 646, 130, 22, Palette.PaperDim * (0.6f * a));
            Gfx.TextSpacedRight(sb, Game.Font, "FOUR MINUTES  THIRTY THREE SECONDS", 1190, 676, Palette.LineGrey * a, TextSize.Tiny, 2f);
        }

        //Footer : controls and build tag
        private void DrawFooter(SpriteBatch sb)
        {
            float a = UiMotion.Between(clock, 1.2f, 0.4f);
            Gfx.TextSpaced(sb, Game.Font, "ARROWS OR MOUSE  /  ENTER TO CONFIRM  /  ESC TO QUIT", 82, 668, Palette.LineGrey * a, TextSize.Tiny, 2f);
            Gfx.TextSpaced(sb, Game.Font, "BETA BUILD", 82, 688, Palette.LineGrey * (0.7f * a), TextSize.Tiny, 2f);
        }
    }
}
