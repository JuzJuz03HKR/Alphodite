using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;

namespace Tacetno433.Screens
{
    //GuideScreen : HOW TO PLAY, five pages you flip through.
    //
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

        //Guide Text : THE PLACE TO EDIT THE TUTORIAL
        private static string[] steps = { "ROUTE", "STAGE", "STROKE", "DUEL", "LINE" };
        private static string[] numbers = { "01", "02", "03", "04", "05" };
        private static string[] titles =
        {
            "Choose a path",
            "Seat the ensemble",
            "Big stroke, whole band",
            "Play the duel",
            "Hold the line"
        };
        private static string[] bodies =
        {
            "Every stage offers two to four paths. Pick one, and there is no going back. "
            + "Duels pay shards and motifs, ??? can be anything, shops and rest stops keep the band alive. "
            + "The last stage of each floor is a boss. Clear three floors to win the run.",

            "Drag musicians onto the seats. Where they sit is how they play. The ROW says which strokes bring "
            + "them in: the back row plays on every stroke, the middle row on middle and big ones, the front row "
            + "only on big ones. The SIDE says on which beats the baton points at them, and then they hit harder. "
            + "The strip on top is TACET's next phrase, with the side each beat points at. Right click to bench.",

            "How big you swing decides who plays: small (p) the back row, "
            + "middle (mf) the middle row too, big (f) the whole band. Everyone who plays pays stamina, "
            + "so read TACET's marks and answer p small and f big. Where TACET is silent, let the beat pass or "
            + "stroke small to rest; a bigger swing is a free hit. Round two plays its phrase twice, round three three times.",

            "TACET plays a bar first, each note marked f (loud), mf or p (soft), then the band counts you in: 3, 2, 1. "
            + "Hold the left mouse button and answer each note the way the pointer on the bright note says. "
            + "The ruler on the baton marks p, mf and f, and the band lights up as you pass them. Stop as the ring closes. "
            + "A PERFECT f against a real f is a COUNTER. Rounds end on a zigzag bar to shake, or from floor two on an arch "
            + "to hold still. Sparks join on floor two.",

            "Each clash pushes the line between your light and TACET's dark. Push it all the way to win at once, "
            + "or be ahead after three rounds. Far enough ahead at a round's end, conduct the FINALE to finish it. "
            + "Every beat TACET wins knocks breath out of the band. Out of breath, the band collapses and the "
            + "fight is lost. Stamina carries across the whole run, so rest and choose your motifs well."
        };

        //Guide Layout
        private Rectangle artBox = new Rectangle(70, 110, 600, 440);
        private Rectangle backButton = new Rectangle(70, 640, 170, 46);
        private Rectangle prevButton = new Rectangle(880, 560, 150, 46);
        private Rectangle nextButton = new Rectangle(1050, 560, 170, 46);
        private const float NavY = 664f;
        private const float NavSpacing = 110f;

        //Drawing Data : the made-up examples in the pictures, kept out of Draw so drawing never allocates
        private static string[] demoRows = { "BACK", "MIDDLE", "FRONT" };
        private static string[] demoRowNotes = { "EVERY STROKE", "MIDDLE AND BIG", "BIG ONLY" };
        private static string[] demoSides = { "LEFT", "CENTRE", "RIGHT" };
        private static string[] demoSizes = { "p", "mf", "f" };
        private static string[] demoRowIn = { "SMALL  :  BACK ROW", "MIDDLE  :  AND THE MIDDLE ROW", "BIG  :  THE WHOLE BAND" };
        private static string[] demoBeats = { "1", "2", "3", "4" };

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
            if (page == 0) DrawRoute(sb, a);
            if (page == 1) DrawStage(sb, a);
            if (page == 2) DrawStroke(sb, a);
            if (page == 3) DrawDuel(sb, a);
            if (page == 4) DrawLine(sb, a);

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

        //Page 1 : three path panels and the floor track
        private void DrawRoute(SpriteBatch sb, float a)
        {
            for (int i = 0; i < 3; i++)
            {
                Rectangle p = new Rectangle(110 + i * 176, 150 + (i == 1 ? 0 : 24), 160 + (i == 1 ? 20 : 0), 280);
                Gfx.Rect(sb, p, (i == 1 ? Palette.Stage : Palette.CanvasDark) * a);
                if (i == 1) Gfx.Rect(sb, p.X, p.Y, p.Width, 3, Palette.Accent * a);
                Rectangle art = p;
                art.Inflate(-8, -8);
                ArtSlot.Draw(sb, art, Palette.Paper, a * (i == 1 ? 1f : 0.6f));
                Ornament.FadeUp(sb, new Rectangle(p.X, p.Bottom - 70, p.Width, 70), 0.9f * a);
            }

            Gfx.Text(sb, Game.BigFont, "ENCOUNTER", 122, 400, Palette.Paper * a, TextSize.Small);
            Gfx.Text(sb, Game.BigFont, "???", 300, 380, Palette.Highlight * a, TextSize.Subtitle);
            Gfx.Text(sb, Game.BigFont, "SHOP", 474, 400, Palette.Paper * a, TextSize.Small);

            //Track
            float y = 500;
            Gfx.Rect(sb, 130, y, 440, 1, Palette.LineGrey * a);
            for (int i = 0; i < 8; i++)
            {
                float x = 130 + i * 440 / 7f;
                if (i < 3) Gfx.Diamond(sb, x, y, 4, Palette.PaperDim * a);
                else if (i == 3) { Gfx.Diamond(sb, x, y, 7, Palette.Void * a); Gfx.DiamondOutline(sb, x, y, 7, Palette.Accent * a, 2f); }
                else Gfx.DiamondOutline(sb, x, y, i == 7 ? 7 : 4, Palette.LineGrey * a, 1f);
            }
            Gfx.CircleOutline(sb, 570, y, 13, Palette.PaperDim * a, 1f);
            Gfx.TextSpacedCentered(sb, Game.Font, "BOSS", 570, y + 18, Palette.PaperDim * a, TextSize.Tiny, 2f);
        }

        //Page 2 : three rows of seats
        private void DrawStage(SpriteBatch sb, float a)
        {
            for (int r = 0; r < 3; r++)
            {
                float y = 160 + r * 120;
                float size = 0.7f + r * 0.12f;
                Gfx.Rect(sb, 250 - r * 30, y + 96 * size, 360 + r * 60, 2, Palette.LineGrey * a);
                Gfx.TextSpaced(sb, Game.Font, demoRows[r], 100, y + 30, Palette.Paper * a, TextSize.Label, 3f);
                Gfx.Text(sb, Game.StoryFont, demoRowNotes[r], 100, y + 50, Palette.PaperDim * a, TextSize.StorySmall);

                for (int s = 0; s < 3; s++)
                {
                    int w = (int)(40 * size);
                    int h = (int)(110 * size);
                    Rectangle cap = new Rectangle(340 + (s - 1) * (130 + r * 20) - w / 2 + 90, (int)y, w, h);
                    bool filled = (r + s) % 2 == 0 || s == 1;
                    if (filled) ArtSlot.Draw(sb, cap, Palette.Paper, a);
                    else Gfx.RectOutline(sb, cap, Palette.LineGrey * a, 1);
                }
            }

            //Sides : the columns the baton points at
            for (int s = 0; s < 3; s++)
                Gfx.TextSpacedCentered(sb, Game.Font, demoSides[s], 430 + (s - 1) * 150, 132, Palette.PaperDim * a, TextSize.Tiny, 3f);

            //Drag Hint : a capsule on its way to a seat
            float t = (time * 0.5f) % 1f;
            Rectangle ghost = new Rectangle((int)(600 - t * 90), (int)(470 - t * 60), 30, 80);
            ArtSlot.Draw(sb, ghost, Palette.Highlight, 0.8f * a);
            Gfx.Arrow(sb, ghost.X - 14, ghost.Center.Y, 7, false, Palette.PaperDim * a);
        }

        //Page 3 : a stroke growing along the ruler, and the rows of the band lighting up with it,
        //the back row first (DYNAMICS)
        private void DrawStroke(SpriteBatch sb, float a)
        {
            //Ruler : three zones, p mf f, a stroke growing along it and falling back
            float rx = 150f;
            float ry = 470f;
            float rw = 420f;
            Gfx.Rect(sb, rx, ry, rw, 2, Palette.LineGrey * a);
            Gfx.Rect(sb, rx + rw / 3f, ry - 8, 2, 18, Palette.Paper * a);
            Gfx.Rect(sb, rx + rw * 2f / 3f, ry - 8, 2, 18, Palette.Paper * a);
            float grow = ((float)Math.Sin(time * 1.1f) * 0.5f + 0.5f) * rw;
            Gfx.Rect(sb, rx, ry - 2, grow, 6, Palette.Highlight * a);
            int zone = Math.Min(2, (int)(grow / (rw / 3f)));
            for (int k = 0; k < 3; k++)
            {
                float zx = rx + rw * (k + 0.5f) / 3f;
                Gfx.TextCentered(sb, Game.BigFont, demoSizes[k], zx, ry + 22, (k == zone ? Palette.Highlight : Palette.PaperDim) * a, TextSize.Small * 0.8f);
            }
            Gfx.TextSpacedCentered(sb, Game.Font, demoRowIn[zone], rx + rw / 2f, ry - 34, Palette.Paper * a, TextSize.Tiny, 3f);

            //Band : three rows seen from the conductor, back at the top. A row lights up once the
            //stroke is big enough to bring it in.
            for (int r = 0; r < 3; r++)
            {
                int row = 2 - r;                                   // drawn back row first, at the top
                bool lit = zone >= r;
                float y = 150 + r * 88;
                float size = 0.75f + row * 0.1f;
                Gfx.TextSpaced(sb, Game.Font, demoRows[r], 110, y + 26, (lit ? Palette.Highlight : Palette.PaperDim) * a, TextSize.Label, 3f);
                for (int s = 0; s < 3; s++)
                {
                    int w = (int)(34 * size);
                    int h = (int)(70 * size);
                    Rectangle cap = new Rectangle(400 + (s - 1) * (90 + row * 16) - w / 2, (int)y, w, h);
                    if (lit) Gfx.DrawGlow(sb, cap.Center.X, cap.Center.Y, h * 0.7f, Palette.Highlight * (0.35f * a));
                    ArtSlot.Draw(sb, cap, lit ? Palette.Highlight : Palette.LineGrey, a);
                }
            }
        }

        //Page 4 : the 4/4 pattern, the stroke sizes, the ring and the meter
        private void DrawDuel(SpriteBatch sb, float a)
        {
            //Pattern : four points, a line for each stroke, the baton tip travelling round them
            float px = 220f;
            float py = 270f;
            float arm = 80f;

            for (int i = 0; i < 4; i++)
            {
                Vector2 from = new Vector2(px, py) + demoPoints[(i + 3) % 4] * arm;
                Vector2 to = new Vector2(px, py) + demoPoints[i] * arm;
                Gfx.Line(sb, from, to, Palette.LineGrey * a, 2f);
                Gfx.Diamond(sb, to.X, to.Y, 6, Palette.Paper * a);
                Vector2 label = new Vector2(px, py) + demoPoints[i] * (arm + 22f);
                Gfx.TextCentered(sb, Game.Font, demoBeats[i], label.X, label.Y, Palette.Highlight * a, TextSize.Body);
            }

            float legTime = 0.6f;
            int leg = (int)(time / legTime) % 4;
            float t = MathHelper.Clamp((time % legTime) / legTime * 1.6f, 0f, 1f);
            t = t * t * (3f - 2f * t);
            Vector2 tip = new Vector2(px, py) + Vector2.Lerp(demoPoints[(leg + 3) % 4], demoPoints[leg], t) * arm;
            Gfx.DrawGlow(sb, tip.X, tip.Y, 24f, Palette.Paper * (0.4f * a));
            Gfx.Circle(sb, tip.X, tip.Y, 6f, Palette.Highlight * a);
            Gfx.TextSpacedCentered(sb, Game.Font, "THE 4/4 PATTERN", px, py + arm + 60, Palette.PaperDim * a, TextSize.Tiny, 3f);

            //Sizes : a ruler with three zones, a stroke growing along it
            float rx = 380f;
            float ry = 220f;
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
            Gfx.TextSpaced(sb, Game.Font, "HOW BIG  =  HOW LOUD", rx, ry - 34, Palette.PaperDim * a, TextSize.Tiny, 3f);

            //Ring : the stroke should stop just as the ring meets its mark
            float cx = 510f;
            float cy = 350f;
            float ringT = (time * 0.8f) % 1f;
            Gfx.CircleOutline(sb, cx, cy, 30, Palette.Highlight * a, 3f);
            Gfx.CircleOutline(sb, cx, cy, 90f - 60f * ringT, Palette.Paper * a, 2f);
            Gfx.Diamond(sb, cx, cy, 5, Palette.Highlight * a);
            Gfx.TextSpacedCentered(sb, Game.Font, ringT > 0.9f ? "PERFECT" : "STOP ON THE RING", cx, cy + 58,
                                   (ringT > 0.9f ? Palette.Highlight : Palette.PaperDim) * a, TextSize.Tiny, 2f);

            //Left Hand : the meter and the key that lets the signature loose
            Gfx.TextSpaced(sb, Game.Font, "LEFT HAND  /  SPACE WHEN THE RECIPE IS FULL", 110, 478, Palette.PaperDim * a, TextSize.Tiny, 2f);
            Rectangle meter = new Rectangle(110, 498, 300, 12);
            Gfx.Rect(sb, meter, Palette.Void * a);
            Gfx.RectOutline(sb, meter, Palette.PaperDim * a, 1);
            Gfx.Rect(sb, meter.X + 2, meter.Y + 2, meter.Width - 4, meter.Height - 4, Palette.Highlight * a);
            Ui.Tag(sb, "SIGNATURE", 426, 495, true, a);
        }

        //Page 5 : the line and the stamina bar
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

            Gfx.TextSpaced(sb, Game.Font, "STAMINA  /  AT ZERO THE BAND COLLAPSES", 120, 476, Palette.LineGrey * a, TextSize.Tiny, 2f);
            Ui.CapsuleBar(sb, new Rectangle(120, 494, 500, 10), 0.45f, Palette.PaperDim, a);
        }
    }
}
