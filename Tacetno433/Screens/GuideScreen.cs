using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //GuideScreen : HOW TO PLAY, three pages you flip through (round 15, it was five pages of
    //text : playtesters found it far too much at once). Each page is one picture and a few lines.
    //   01 CONTROLS  the mouse and the four keys
    //   02 THE NOTE  what a note tells you : the arrow, the letter, the ring
    //   03 THE LINE  how a duel is won and lost
    //Alpha 7 (step 9) : with the turn battle on, page 3 is THE FIGHT (turns, blocking, BREATH)
    //and the controls page lists the keys of the turn battle.
    //Left  : a small drawing of the thing being explained
    //Right : the page number, a title and a few sentences
    //Bottom: the page track, click a diamond to jump
    public class GuideScreen : GameScreen
    {
        //Can Pause : a menu page outside the run, ESC here means going back
        public override bool CanPause
        {
            get { return false; }
        }

        //Guide Text : THE PLACE TO EDIT HOW TO PLAY. A few short lines a page.
        //Where the two games differ : turnBattle ? the turn battle's words : the old duel's (BattleRules.TurnBattleOn).
        private static bool turnBattle = BattleRules.TurnBattleOn;
        private static string[] steps = turnBattle ? new string[] { "CONTROLS", "THE NOTE", "THE FIGHT" }
                                             : new string[] { "CONTROLS", "THE NOTE", "THE LINE" };
        private static string[] numbers = { "01", "02", "03" };
        private static string[] titles =
        {
            "Your baton",
            "The note says it all",
            turnBattle ? "One bar at a time" : "Push back the silence"
        };
        private static string[] bodies =
        {
            turnBattle ? "Click to choose. When a ring shows, hold the LEFT MOUSE BUTTON and swing."
                 : "Hold the LEFT MOUSE BUTTON and swing. That is the whole game. "
                   + "SPACE : signature. TAB : your band. ESC : pause.",

            "ARROW : which way. LETTER : how far (p small, mf middle, f big). RING : stop as it closes.",

            turnBattle ? "Your bar : give each musician a move, ENTER plays them in order. ATTACK hits by itself, a skill asks for a swing. "
                   + "Their turn : swipe back at each note to block it. BREATH pays for skills and for calling the BENCH."
                 : "Win a note, push the line. Lose a note, lose breath. "
                   + "Push the line all the way to win. Let silent beats pass to breathe."
        };

        //Guide Layout
        private Rectangle artBox = new Rectangle(70, 110, 600, 440);
        private Rectangle backButton = new Rectangle(70, 640, 170, 46);
        private Rectangle prevButton = new Rectangle(880, 560, 150, 46);
        private Rectangle nextButton = new Rectangle(1050, 560, 170, 46);
        private const float NavY = 664f;
        private const float NavSpacing = 110f;

        //Drawing Data : the made-up examples in the pictures, kept out of Draw so drawing never allocates
        private static string[] demoSizes = { "p", "mf", "f" };
        private static string[] demoBeats = { "1", "2", "3", "4" };
        private static string[] keyNames = turnBattle ? new string[] { "1 2 3", "ENTER", "SPACE", "TAB", "ESC" } : new string[] { "SPACE", "TAB", "ESC" };
        private static string[] keyJobs = turnBattle
            ? new string[] { "ATTACK AND SKILLS", "PLAY THE BAR", "SIGNATURE  (WHEN THE DIAMONDS ARE FULL)", "THE BAND  (ON THE ROUTE)", "PAUSE" }
            : new string[] { "SIGNATURE  (WHEN ITS RECIPE IS FULL)", "THE BAND  (ON THE ROUTE)", "PAUSE" };
        private static string[] demoOrder = { "1", "2", "TACET", "3" };

        //Demo Pattern : the four points of the 4/4 shape, in beat order, around the centre
        private static Vector2[] demoPoints =
        {
            new Vector2(0f, 1f), new Vector2(-1f, 0f), new Vector2(1f, 0f), new Vector2(0f, -1f)
        };

        //Guide State
        private static string[] wrapped;
        private int page;
        private float time;
        private float turn;                 // 0 right after a page flip, 1 when settled

        public override void Load()
        {
            //Text Wrap : done once for the whole game, the first time the guide opens
            if (wrapped == null)
            {
                wrapped = new string[bodies.Length];
                for (int i = 0; i < bodies.Length; i++)
                    wrapped[i] = Gfx.WrapText(Game.StoryFont, bodies[i], 440, TextSize.Story);
            }

            SoundBank.PlayMusic(Music.Title);
        }

        public override void Update(float dt)
        {
            time += dt;
            turn = Math.Min(1f, turn + dt * 3f);

            if (Input.KeyPressed(Keys.Right) || Input.KeyPressed(Keys.D) || Input.ClickedOn(nextButton)) Flip(page + 1);
            if (Input.KeyPressed(Keys.Left) || Input.KeyPressed(Keys.A) || Input.ClickedOn(prevButton)) Flip(page - 1);

            for (int i = 0; i < steps.Length; i++)
                if (Input.ClickedOn(Ui.StepRect(steps.Length, i, 640, NavY, NavSpacing))) Flip(i);

            if (Input.KeyPressed(Keys.Escape) || Input.ClickedOn(backButton))
            {
                SoundBank.Play(Sfx.UiBack);
                Game.Screens.Change(new TitleScreen());
            }
        }

        private void Flip(int to)
        {
            if (to < 0 || to >= steps.Length || to == page) return;
            page = to;
            turn = 0f;
            SoundBank.Play(Sfx.PageTurn);
        }

        public override void Draw(SpriteBatch sb)
        {
            SceneBackdrop.Draw(sb, time);

            //Heading
            Gfx.TextSpaced(sb, Game.Font, "HOW TO PLAY", 70, 46, Palette.PaperDim, TextSize.Label, 6f);
            Ornament.Rule(sb, 70, 72, 1140, Palette.LineGrey);

            //Drawing
            Ui.Panel(sb, artBox, 1f);
            float a = turn;
            if (page == 0) DrawControls(sb, a);
            if (page == 1) DrawNote(sb, a);
            if (page == 2 && turnBattle) DrawFight(sb, a);
            if (page == 2 && !turnBattle) DrawLine(sb, a);

            //Text Column : slides in a little on every flip
            float x = 740 + (1f - a) * 20f;
            Gfx.Text(sb, Game.LogoFont, numbers[page], x - 4, 96, Palette.Paper * (0.25f * a), TextSize.Banner);
            Gfx.TextSpaced(sb, Game.Font, steps[page], x, 196, Palette.PaperDim * a, TextSize.Label, 5f);
            Gfx.Text(sb, Game.BigFont, titles[page], x, 214, Palette.Highlight * a, TextSize.Title);
            Ornament.Rule(sb, x, 266, 440, Palette.LineGrey * a);
            Gfx.Text(sb, Game.StoryFont, wrapped[page], x, 284, Palette.Paper * a, TextSize.Story);

            Ui.Button(sb, prevButton, "PREV", "A", false, page > 0, 1f);
            Ui.Button(sb, nextButton, "NEXT", "D", true, page < steps.Length - 1, 1f);
            Ui.Button(sb, backButton, "BACK", "ESC", false);

            Ui.StepNav(sb, steps, page, 640, NavY, NavSpacing);
        }

        //Page 1 : the mouse with its left button lit, and the three keys with their jobs
        private void DrawControls(SpriteBatch sb, float a)
        {
            //Mouse : hold and swing, the baton tip drawing the 4/4 shape beside it
            Ui.MouseIcon(sb, 180f, 230f, 2f, a);
            Gfx.TextSpaced(sb, Game.Font, "HOLD + SWING", 250, 204, Palette.Highlight * a, TextSize.Label, 3f);
            Gfx.Text(sb, Game.StoryFont, "the left button raises the baton", 250, 232, Palette.PaperDim * a, TextSize.StorySmall);
            DrawShape(sb, 520f, 230f, 56f, a);

            //Keys : five rows in the turn battle, so they sit a little closer
            float keyStep = keyNames.Length > 4 ? 40f : (keyNames.Length > 3 ? 46f : 56f);
            for (int i = 0; i < keyNames.Length; i++)
            {
                float y = 350f + i * keyStep;
                Ui.KeyCap(sb, keyNames[i], 130f, y, a);
                Gfx.TextSpaced(sb, Game.Font, keyJobs[i], 250, y - 8, Palette.Paper * a, TextSize.Tiny, 2f);
            }
        }

        //Page 2 : one note with its arrow and letter, the three sizes along a ruler, and the ring
        private void DrawNote(SpriteBatch sb, float a)
        {
            //The Note : a big one, its arrow on the edge, its letter inside
            float nx = 200f;
            float ny = 230f;
            Gfx.Circle(sb, nx, ny, 48f, Palette.Paper * a);
            Gfx.CircleOutline(sb, nx, ny, 48f, Palette.Ink * a, 4f);
            Gfx.TextCentered(sb, Game.BigFont, "f", nx, ny - 6, Palette.Ink * a, TextSize.Title);
            NoteGlyph.Pointer(sb, Flick.Right, nx, ny, 48f, a);
            Gfx.TextSpacedCentered(sb, Game.Font, "ARROW : SWING RIGHT", nx, ny + 70, Palette.Highlight * a, TextSize.Tiny, 2f);
            Gfx.TextSpacedCentered(sb, Game.Font, "LETTER f : A BIG STROKE", nx, ny + 90, Palette.Highlight * a, TextSize.Tiny, 2f);

            //Sizes : a ruler with three zones, a stroke growing along it
            float rx = 360f;
            float ry = 210f;
            float rw = 260f;
            Gfx.Rect(sb, rx, ry, rw, 2, Palette.LineGrey * a);
            Gfx.Rect(sb, rx + rw / 3f, ry - 8, 2, 18, Palette.Paper * a);
            Gfx.Rect(sb, rx + rw * 2f / 3f, ry - 8, 2, 18, Palette.Paper * a);
            float grow = ((float)Math.Sin(time * 1.3f) * 0.5f + 0.5f) * rw;
            Gfx.Rect(sb, rx, ry - 2, grow, 6, Palette.Highlight * a);
            for (int k = 0; k < 3; k++)
            {
                bool here = grow >= rw * k / 3f && grow < rw * (k + 1) / 3f;
                float zx = rx + rw * (k + 0.5f) / 3f;
                Gfx.TextSpacedCentered(sb, Game.Font, demoSizes[k], zx, ry + 16,
                                       (here ? Palette.Highlight : Palette.PaperDim) * a, TextSize.Label, 2f);
            }
            Gfx.TextSpaced(sb, Game.Font, "HOW FAR  =  HOW LOUD", rx, ry - 34, Palette.PaperDim * a, TextSize.Tiny, 3f);

            //Ring : the stroke should stop just as the ring meets its mark
            float cx = 490f;
            float cy = 400f;
            float ringT = (time * 0.8f) % 1f;
            Gfx.CircleOutline(sb, cx, cy, 30, Palette.Highlight * a, 3f);
            Gfx.CircleOutline(sb, cx, cy, 90f - 60f * ringT, Palette.Paper * a, 2f);
            Gfx.Diamond(sb, cx, cy, 5, Palette.Highlight * a);
            Gfx.TextSpacedCentered(sb, Game.Font, ringT > 0.9f ? "PERFECT" : "STOP ON THE RING", cx, cy + 100,
                                   (ringT > 0.9f ? Palette.Highlight : Palette.PaperDim) * a, TextSize.Tiny, 2f);

            //Shape : the four ways of the bar, in beat order
            DrawShape(sb, 200f, 420f, 50f, a);
        }

        //Shape : the 4/4 pattern, four points and a line for each stroke, the baton tip travelling round
        private void DrawShape(SpriteBatch sb, float px, float py, float arm, float a)
        {
            for (int i = 0; i < 4; i++)
            {
                Vector2 from = new Vector2(px, py) + demoPoints[(i + 3) % 4] * arm;
                Vector2 to = new Vector2(px, py) + demoPoints[i] * arm;
                Gfx.Line(sb, from, to, Palette.LineGrey * a, 2f);
                Gfx.Diamond(sb, to.X, to.Y, 6, Palette.Paper * a);
                Vector2 label = new Vector2(px, py) + demoPoints[i] * (arm + 20f);
                Gfx.TextCentered(sb, Game.Font, demoBeats[i], label.X, label.Y, Palette.Highlight * a, TextSize.Body);
            }

            float legTime = 0.6f;
            int leg = (int)(time / legTime) % 4;
            float t = MathHelper.Clamp((time % legTime) / legTime * 1.6f, 0f, 1f);
            t = t * t * (3f - 2f * t);
            Vector2 tip = new Vector2(px, py) + Vector2.Lerp(demoPoints[(leg + 3) % 4], demoPoints[leg], t) * arm;
            Gfx.DrawGlow(sb, tip.X, tip.Y, 24f, Palette.Paper * (0.4f * a));
            Gfx.Circle(sb, tip.X, tip.Y, 6f, Palette.Highlight * a);
        }

        //Page 3 (turn battle) : the bar (ONE BAR, a CUT IN in the middle), the band and TACET, one of its notes
        //flying in to be blocked on the ring, and the BREATH pips
        private void DrawFight(SpriteBatch sb, float a)
        {
            //The Bar : the band are filled plates, TACET an outline, like the fight page
            Gfx.TextSpaced(sb, Game.Font, "THE BAR", 110, 140, Palette.LineGrey * a, TextSize.Tiny, 2f);
            int now = (int)(time / 1.6f) % demoOrder.Length;
            for (int i = 0; i < demoOrder.Length; i++)
            {
                Rectangle plate = new Rectangle(110 + i * 92, 160, 84, 30);
                bool enemy = i == 2;
                Color ink = i == now ? Palette.Highlight : Palette.PaperDim;
                if (enemy) Gfx.RectOutline(sb, plate, ink * a, i == now ? 2 : 1);
                else Gfx.Rect(sb, plate, ink * a);
                Gfx.TextCentered(sb, Game.Font, demoOrder[i], plate.Center.X, plate.Center.Y, (enemy ? ink : Palette.Ink) * a, TextSize.Label);
            }

            //The Band and TACET : empty frames, the art comes later
            for (int i = 0; i < 3; i++)
                Gfx.RectOutline(sb, new Rectangle(110 + i * 70, 240, 56, 100), Palette.PaperDim * a, 1);
            Gfx.TextSpaced(sb, Game.Font, "BAND", 110, 350, Palette.Paper * a, TextSize.Label, 3f);
            Gfx.RectOutline(sb, new Rectangle(490, 220, 110, 130), Palette.Paper * a, 2);
            Gfx.TextSpaced(sb, Game.Font, "TACET", 490, 360, Palette.Paper * a, TextSize.Label, 3f);

            //Block : a note flies from TACET to the ring, which closes as it arrives
            float t = (time % 1.4f) / 1.4f;
            float cx = 400f;
            float cy = 290f;
            NoteGlyph.TimingRing(sb, cx, cy, Math.Min(1f, t / 0.8f), 30f, 80f, 0f, a);
            if (t < 0.8f) NoteGlyph.Note(sb, Game.BigFont, MathHelper.Lerp(480f, cx, t / 0.8f), cy, 1, Flick.Left, 1f, time, a);
            else Gfx.TextSpacedCentered(sb, Game.Font, "BLOCK", cx, cy + 50, Palette.Highlight * a, TextSize.Label, 3f);

            //BREATH : what skills spend and blocks give back
            Gfx.TextSpaced(sb, Game.Font, "BREATH  :  SKILLS AND CALLS SPEND IT, HITS GIVE IT BACK", 110, 440, Palette.LineGrey * a, TextSize.Tiny, 2f);
            Ui.Pips(sb, 116, 476, 3 + (int)(time * 0.8f) % 5, BattleRules.TurnBreathMax, 8f, 22f, a);
        }

        //Page 3 : the line and the breath bar
        private void DrawLine(SpriteBatch sb, float a)
        {
            float split = 370 + (float)Math.Sin(time * 1.2f) * 90f;
            Gfx.Rect(sb, 100, 150, split - 100, 250, Palette.StageLight * a);
            Gfx.Rect(sb, split, 150, 640 - split, 250, Palette.Void * a);
            for (int y = 150; y < 400; y += 4)
                Gfx.Rect(sb, split - 2 + (float)Math.Sin(y * 0.05f + time * 3f) * 4f, y, 3, 4, Palette.Paper * a);

            Gfx.TextSpaced(sb, Game.Font, "YOU", 116, 166, Palette.Ink * a, TextSize.Label, 3f);
            Gfx.TextSpacedRight(sb, Game.Font, "TACET", 624, 166, Palette.Paper * a, TextSize.Label, 3f);

            Ui.CapsuleBar(sb, new Rectangle(120, 430, 500, 14), (split - 100f) / 540f, Palette.Paper, a);
            Gfx.TextSpaced(sb, Game.Font, "THE LINE", 120, 412, Palette.LineGrey * a, TextSize.Tiny, 2f);

            Gfx.TextSpaced(sb, Game.Font, "BREATH  :  THE BAND'S LIFE  /  AT ZERO THE FIGHT IS LOST", 120, 476, Palette.LineGrey * a, TextSize.Tiny, 2f);
            Ui.CapsuleBar(sb, new Rectangle(120, 494, 500, 10), 0.45f, Palette.PaperDim, a);
        }
    }
}
