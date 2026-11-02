using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //CreditsScreen (Beta1 2 Nov, block E, docs/BETA1_DIRECTION.md 48) : who made the game and whose free work is in it.
    //Opened from the title page (CREDITS at the bottom left, or the C key). The words are in Data/Credits.cs.
    //The same dark, still room as HOW TO PLAY : the page's name turned on the tall white bar at the left, the page's number,
    //name and a line under it in the left column, the makers listed on the right, each with the licence in a small tag.
    //Four pages flipped with LEFT and RIGHT (or PREV and NEXT), the pages as circles on a line under the left column.
    public class CreditsScreen : GameScreen
    {
        //Can Pause : a menu page outside the run, ESC here means going back
        public override bool CanPause
        {
            get { return false; }
        }

        //Wipe Word : the page's name in the wipe on the way here (WIPE WORD)
        public override string WipeWord
        {
            get { return "CREDITS"; }
        }

        //Credits Layout
        private const float LeftX = 180f;               // the page's number, name and line
        private const float NoteWrap = 300f;
        private Rectangle listBox = new Rectangle(540, 140, 690, 404);   // the makers sit inside this thin frame
        private const float ListPad = 22f;              // inside the frame, left and right
        private const float RowGap = 14f;               // between two makers, the thin line in the middle
        private Rectangle backButton = new Rectangle(40, 646, 190, 46);
        private Rectangle prevButton = new Rectangle(830, 576, 170, 46);
        private Rectangle nextButton = new Rectangle(1030, 576, 190, 46);
        private const float NavX = 330f;                // the circles of the pages, under the left column
        private const float NavY = 600f;
        private const float NavSpacing = 70f;
        private static readonly Vector2 Hole = new Vector2(1180f, 76f);

        //Prepared Words : every line wrapped once, the first time the page opens, so drawing never builds a string
        private static string[] notes;
        private static string[][] whats;
        private static float[][] rowTops;                // where each maker starts inside the frame
        private float storyLine;                         // one line of the small sentences, in pixels

        //Credits State
        private int page;
        private float time;
        private float turn = 1f;                        // 0 right after a page flip, 1 when settled
        private UiMotion prevMotion = new UiMotion();
        private UiMotion nextMotion = new UiMotion();
        private UiMotion backMotion = new UiMotion();

        public override void Load()
        {
            //Prepare : the notes and every maker's line wrapped to fit, and where each maker's row starts
            storyLine = Game.StoryFont.LineSpacing * TextSize.StorySmall;
            if (notes == null)
            {
                int pages = Credits.Pages.Length;
                notes = new string[pages];
                whats = new string[pages][];
                rowTops = new float[pages][];
                float wrap = listBox.Width - ListPad * 2f;
                for (int p = 0; p < pages; p++)
                {
                    notes[p] = Gfx.WrapText(Game.StoryFont, Credits.Notes[p], NoteWrap, TextSize.Story);
                    Credit[] lines = Credits.Lines[p];
                    whats[p] = new string[lines.Length];
                    rowTops[p] = new float[lines.Length];
                    float y = 20f;
                    for (int i = 0; i < lines.Length; i++)
                    {
                        whats[p][i] = Gfx.WrapText(Game.StoryFont, lines[i].What, wrap, TextSize.StorySmall);
                        rowTops[p][i] = y;
                        y += RowHeight(whats[p][i]) + RowGap;
                    }
                }
            }

            //TACET and the page's light : the dark, still room of HOW TO PLAY
            Dark = new TacetVoid();
            StillRoom.Light(Lens);                                            // STILL ROOM : the menu pages' one room (Core/StillRoom.cs)
            nextMotion.Snap(true);

            SoundBank.PlayMusic(Music.Title);
        }

        //Row Height : the maker's name, then their wrapped line under it
        private float RowHeight(string what)
        {
            int lines = 1;
            for (int k = 0; k < what.Length; k++)
                if (what[k] == '\n') lines++;
            return 24f + lines * storyLine;
        }

        public override void Update(float dt)
        {
            time += dt;
            turn = Math.Min(1f, turn + dt * 3f);

            //TACET : far up in the corner, still
            Dark.Update(dt, 0.3f);
            StillRoom.Hole(Dark, Hole, 22f, time);

            //POINT : NEXT stays lit unless the mouse points at PREV or BACK
            bool last = page >= Credits.Pages.Length - 1;
            bool overPrev = page > 0 && Input.MouseOver(prevButton);
            bool overBack = Input.MouseOver(backButton);
            prevMotion.Update(dt, overPrev);
            nextMotion.Update(dt, !last && !overPrev && !overBack);
            backMotion.Update(dt, overBack);

            if (Input.KeyPressed(Keys.Right) || Input.KeyPressed(Keys.D) || Input.ClickedOn(nextButton))
            {
                if (!last) nextMotion.Press();
                Flip(page + 1);
            }
            if (Input.KeyPressed(Keys.Left) || Input.KeyPressed(Keys.A) || Input.ClickedOn(prevButton))
            {
                if (page > 0) prevMotion.Press();
                Flip(page - 1);
            }

            //NUMBER KEYS : 1 to 4 jump to that page, as a click on its circle does
            for (int i = 0; i < Credits.Pages.Length; i++)
                if (Input.NumberPressed(i) || Input.ClickedOn(Ui.StepRect(Credits.Pages.Length, i, NavX, NavY, NavSpacing))) Flip(i);

            if (Input.KeyPressed(Keys.Escape) || Input.ClickedOn(backButton))
            {
                backMotion.Press();
                SoundBank.Play(Sfx.UiBack);
                Game.Screens.Change(new TitleScreen());
            }
        }

        private void Flip(int to)
        {
            if (to < 0 || to >= Credits.Pages.Length || to == page) return;
            page = to;
            turn = 0f;
            SoundBank.Play(Sfx.PageTurn);
        }

        //======================================================================================
        //The World (THE LENS) : the dark, still room and the list's frame
        //======================================================================================

        public override bool HasWorld
        {
            get { return true; }
        }

        public override bool ShowsGrain
        {
            get { return false; }
        }

        //Back : the menu pages' one dark room (STILL ROOM, Core/StillRoom.cs), the same as HOW TO PLAY
        public override void DrawBack(SpriteBatch sb)
        {
            StillRoom.Back(sb, time);
        }

        //World : the bits the hole pulls in, and the list's frame (STILL ROOM)
        public override void DrawWorld(SpriteBatch sb)
        {
            Dark.DrawDebris(sb, false);
            StillRoom.Frame(sb, listBox, time);
        }

        //Lights : a soft light on the list, a softer one on the page's name
        public override void DrawLights(SpriteBatch sb)
        {
            float a = UiMotion.Between(time, 0.1f, 0.6f);
            Gfx.DrawGlow(sb, listBox.Center.X, listBox.Center.Y, 460f, Color.White * (0.24f * a));
            Gfx.DrawGlow(sb, 320f, 260f, 300f, Color.White * (0.14f * a));
        }

        //======================================================================================
        //The Words (crisp on top)
        //======================================================================================

        public override void Draw(SpriteBatch sb)
        {
            //Title Bar : the page's name turned on the tall white bar at the left (menu1)
            float t = UiMotion.Between(time, 0f, 0.45f);
            int barX = 40 + (int)Math.Round((1f - t) * -40f);
            Ui.TitleBar(sb, new Rectangle(barX, 80, 96, 540), 24, "CREDITS", t);

            //Left Column : the page's number big and faint, its name, its title and the line under it, sliding in on a flip
            float w = turn * UiMotion.Between(time, 0.2f, BattleRules.UiEnterTime);
            float x = LeftX + (1f - UiMotion.EaseOut(turn)) * 20f;
            Gfx.Text(sb, Game.LogoFont, Credits.Numbers[page], x - 4, 96, Palette.Paper * (0.25f * w), TextSize.Banner);
            Gfx.TextSpaced(sb, Game.Font, Credits.Pages[page], x, 196, Palette.PaperDim * w, TextSize.Label, 5f);
            Gfx.Text(sb, Game.BigFont, Credits.Titles[page], x, 214, Palette.Highlight * w, TextSize.Title);
            Gfx.CircleOutline(sb, x + 3, 266, 3f, Palette.LineGrey * w, 1f);
            Gfx.Rect(sb, x + 9, 266, NoteWrap - 9, 1, Palette.LineGrey * (0.6f * w));
            Gfx.Text(sb, Game.StoryFont, notes[page], x, 284, Palette.Paper * w, TextSize.Story);

            DrawList(sb, turn * UiMotion.Between(time, 0.25f, BattleRules.UiEnterTime));

            //Buttons : PREV and NEXT fade out where there is no page to go to
            float low = UiMotion.Between(time, 0.4f, BattleRules.UiEnterTime);
            Ui.Command(sb, prevButton, "PREV", "A", prevMotion, low * (page > 0 ? 1f : 0.35f), false);
            Ui.Command(sb, nextButton, "NEXT", "D", nextMotion, low * (page < Credits.Pages.Length - 1 ? 1f : 0.35f), false);
            Ui.Command(sb, backButton, "BACK", "ESC", backMotion, low, false);

            Ui.Steps(sb, Credits.Pages, page, NavX, NavY, NavSpacing, low);
            Gfx.TextSpacedRight(sb, Game.Font, "LEFT RIGHT  TURN THE PAGE   /   ESC  BACK", 1240, 692, Palette.LineGrey * low, TextSize.Tiny, 2f);
        }

        //List : each maker's name, the licence tag at the right end of the row, their line under it, a thin line between rows.
        //The rows come in one after another, a little after the page's name.
        private void DrawList(SpriteBatch sb, float a)
        {
            Credit[] lines = Credits.Lines[page];
            float left = listBox.X + ListPad;
            float right = listBox.Right - ListPad;
            for (int i = 0; i < lines.Length; i++)
            {
                float r = a * MathHelper.Clamp(turn * 3f - i * 0.25f, 0f, 1f);
                if (r <= 0f) continue;
                float y = listBox.Y + rowTops[page][i] + (1f - UiMotion.EaseOut(r)) * 10f;
                Credit c = lines[i];

                Gfx.Circle(sb, left - 9f, y + 9f, 2.5f, Palette.Paper * r);
                Gfx.TextSpaced(sb, Game.Font, c.Who, left, y, Palette.Highlight * r, TextSize.Body, 2f);
                bool ours = c.License == "OURS";
                Ui.Tag(sb, c.License, right - Gfx.SpacedWidth(Game.Font, c.License, TextSize.Tiny, 1.5f) - 14f, y + 2f, ours, r);
                Gfx.Text(sb, Game.StoryFont, whats[page][i], left, y + 24f, Palette.PaperDim * r, TextSize.StorySmall);

                if (i < lines.Length - 1)
                {
                    float lineY = listBox.Y + rowTops[page][i + 1] - RowGap / 2f;
                    Gfx.Rect(sb, left, lineY, right - left, 1, Palette.LineGrey * (0.3f * r));
                }
            }
        }
    }
}
