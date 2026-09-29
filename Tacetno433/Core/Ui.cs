using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //Ui : the shared pieces every page is built from, so all pages look like one game.
    //
    //   Button      a slanted blade, solid for the main action, outlined for the rest
    //   Tag         a small label in a box
    //   KeyChip     the keyboard key that also does the job
    //   KeyCap      a keyboard key drawn as a key, and MouseIcon, for the pages that teach the controls
    //   StepNav     diamonds on a line, which step of a process you are on
    //   Header      "01" plus a serif heading plus a rule
    //   Panel       the dark box with corner marks that holds content
    //   Pips        a row of small diamonds, used like rarity stars
    //   Tooltip     a box that explains whatever the mouse is over
    //   Slider      a track with a diamond knob, used by the settings page
    //   CheckBox    a small box with a tick in it when the option is on
    //
    //The fonts are handed over once by TacetGame, so every call stays short.
    public static class Ui
    {
        public static SpriteFont Font;
        public static SpriteFont BigFont;
        public static SpriteFont StoryFont;
        public static SpriteFont LogoFont;

        //Button Slant : how far every button leans
        public const int Slant = 14;

        //Button : returns nothing, pages test the click themselves with Input.ClickedOn(box)
        public static void Button(SpriteBatch sb, Rectangle box, string label, string key, bool primary, bool enabled, float alpha)
        {
            bool over = enabled && Input.MouseOver(box);
            float a = enabled ? alpha : alpha * 0.35f;

            //Hover Lean : the button steps forward a few pixels under the mouse
            Rectangle r = box;
            if (over) r.X += 4;

            Color text;
            if (primary)
            {
                Gfx.SlantBox(sb, new Rectangle(r.X + 5, r.Y + 5, r.Width, r.Height), Slant, Color.Black * (0.55f * a));
                Gfx.SlantBox(sb, r, Slant, (over ? Palette.Highlight : Palette.Paper) * a);
                text = Palette.Ink * a;
            }
            else
            {
                Gfx.SlantBox(sb, r, Slant, (over ? Palette.Stage : Palette.Panel) * (0.92f * a));
                Gfx.SlantOutline(sb, r, Slant, (over ? Palette.Highlight : Palette.LineGrey) * a, 1f);
                text = (over ? Palette.Highlight : Palette.Paper) * a;
            }

            //Label Area : between the slanted left edge and the key chip, if there is one
            float left = r.X + Slant + 8;
            float right = r.Right - Slant - 8;
            float chipRight = r.Right - Slant - 6;
            if (key.Length > 0) right = chipRight - (Gfx.TextWidth(Font, key, TextSize.Tiny) + 10f) - 8;

            //Label : centred in its area, dropped to a smaller size if it would not fit
            float scale = TextSize.Heading;
            float width = Gfx.SpacedWidth(Font, label, scale, 1.5f);
            if (width > right - left)
            {
                scale = TextSize.Body;
                width = Gfx.SpacedWidth(Font, label, scale, 1.5f);
            }
            float labelY = r.Center.Y - Font.LineSpacing * scale / 2f;
            Gfx.TextSpaced(sb, Font, label, (left + right) / 2f - width / 2f, labelY, text, scale, 1.5f);

            if (key.Length > 0)
                KeyChipRight(sb, key, chipRight, r.Center.Y, primary ? Palette.InkSoft * a : Palette.PaperDim * a);

            //Hover Mark : a small diamond points at the button
            if (over)
                Gfx.Diamond(sb, r.X - 10, r.Center.Y, 4, (primary ? Palette.Highlight : Palette.Accent) * a);
        }

        //Button (short form) : enabled and fully visible
        public static void Button(SpriteBatch sb, Rectangle box, string label, string key, bool primary)
        {
            Button(sb, box, label, key, primary, true, 1f);
        }

        //Key Chip : a small outlined key, drawn so its right edge ends at rightX
        public static void KeyChipRight(SpriteBatch sb, string key, float rightX, float cy, Color color)
        {
            float w = Gfx.TextWidth(Font, key, TextSize.Tiny) + 10f;
            Rectangle chip = new Rectangle((int)(rightX - w), (int)(cy - 9), (int)w, 18);
            Gfx.RectOutline(sb, chip, color, 1);
            Gfx.TextCentered(sb, Font, key, chip.Center.X, chip.Center.Y, color, TextSize.Tiny);
        }

        //Key Cap : a keyboard key with its name on it, lit like a key cap (round 15, the players
        //asked which button does what). Returns how wide it was, so a line can follow it.
        //This is an interface icon, not artwork, so it stays.
        public static float KeyCap(SpriteBatch sb, string key, float x, float cy, float alpha)
        {
            float w = System.Math.Max(34f, Gfx.TextWidth(Font, key, TextSize.Tiny) + 18f);
            Rectangle cap = new Rectangle((int)x, (int)(cy - 15), (int)w, 28);
            Gfx.Rect(sb, cap.X, cap.Y + 4, cap.Width, cap.Height, Palette.Void * alpha);          // the side of the key
            Gfx.Rect(sb, cap, Palette.Paper * alpha);
            Gfx.RectOutline(sb, cap, Palette.LineGrey * alpha, 1);
            Gfx.TextCentered(sb, Font, key, cap.Center.X, cap.Center.Y, Palette.Ink * alpha, TextSize.Tiny);
            return w;
        }

        //Mouse Icon : a mouse seen from above, the LEFT button filled, the one the baton needs
        public static void MouseIcon(SpriteBatch sb, float cx, float cy, float size, float alpha)
        {
            Rectangle body = new Rectangle((int)(cx - 14 * size), (int)(cy - 22 * size), (int)(28 * size), (int)(44 * size));
            Gfx.Capsule(sb, body, Palette.Void * alpha);
            Gfx.CapsuleOutline(sb, body, Palette.Paper * alpha, 2f);
            Gfx.Rect(sb, cx - 10 * size, cy - 14 * size, 9 * size, 12 * size, Palette.Highlight * alpha);   // the left button
            Gfx.Rect(sb, cx - 1, cy - 20 * size, 2, 18 * size, Palette.Paper * alpha);                        // between the buttons
            Gfx.Rect(sb, cx - 13 * size, cy - 2 * size, 26 * size, 2, Palette.Paper * alpha);
        }

        //Tag : a word in a small box. filled tags are for things that matter right now.
        public static void Tag(SpriteBatch sb, string text, float x, float y, bool filled, float alpha)
        {
            float w = Gfx.SpacedWidth(Font, text, TextSize.Tiny, 1.5f) + 14f;
            Rectangle box = new Rectangle((int)x, (int)y, (int)w, 18);

            if (filled)
            {
                Gfx.Rect(sb, box, Palette.Paper * alpha);
                Gfx.TextSpaced(sb, Font, text, box.X + 7, box.Y + 3, Palette.Ink * alpha, TextSize.Tiny, 1.5f);
            }
            else
            {
                Gfx.RectOutline(sb, box, Palette.LineGrey * alpha, 1);
                Gfx.TextSpaced(sb, Font, text, box.X + 7, box.Y + 3, Palette.PaperDim * alpha, TextSize.Tiny, 1.5f);
            }
        }

        //Tag Width : so pages can line several tags up
        public static float TagWidth(string text)
        {
            return Gfx.SpacedWidth(Font, text, TextSize.Tiny, 1.5f) + 14f;
        }

        //Header : a small index number, a serif heading, and a rule ending in a diamond
        public static void Header(SpriteBatch sb, string index, string title, float x, float y, float width, float alpha)
        {
            float titleX = x;
            if (index.Length > 0)
            {
                Gfx.Text(sb, Font, index, x, y + 8, Palette.PaperDim * alpha, TextSize.Label);
                titleX = x + 28;
            }
            Gfx.Text(sb, BigFont, title, titleX, y - 2, Palette.Paper * alpha, TextSize.Small);
            Ornament.Rule(sb, x, y + 28, width, Palette.LineGrey * alpha);
        }

        //Panel : the standard dark container with viewfinder corners
        public static void Panel(SpriteBatch sb, Rectangle r, float alpha)
        {
            Gfx.Rect(sb, r, Palette.Panel * (0.88f * alpha));
            Gfx.RectOutline(sb, r, Palette.LineGrey * (0.35f * alpha), 1);
            Ornament.CornerBrackets(sb, r, 10, Palette.PaperDim * alpha, 1);
        }

        //Pips : a row of diamonds, filled ones first
        public static void Pips(SpriteBatch sb, float x, float cy, int filled, int total, float size, float gap, float alpha)
        {
            for (int i = 0; i < total; i++)
            {
                float px = x + i * gap;
                if (i < filled) Gfx.Diamond(sb, px, cy, size, Palette.Paper * alpha);
                else Gfx.DiamondOutline(sb, px, cy, size, Palette.LineGrey * alpha, 1f);
            }
        }

        //Step Rect : the clickable area of step i on a StepNav
        public static Rectangle StepRect(int count, int i, float cx, float y, float spacing)
        {
            float x = cx - (count - 1) * spacing / 2f + i * spacing;
            return new Rectangle((int)(x - spacing / 2f), (int)(y - 16), (int)spacing, 44);
        }

        //Step Nav : diamonds joined by a line, labels underneath. Steps before current are
        //done, current is lit, the rest are waiting.
        public static void StepNav(SpriteBatch sb, string[] steps, int current, float cx, float y, float spacing)
        {
            int count = steps.Length;
            float left = cx - (count - 1) * spacing / 2f;

            Gfx.Rect(sb, left, y, (count - 1) * spacing, 1, Palette.LineGrey * 0.7f);

            for (int i = 0; i < count; i++)
            {
                float x = left + i * spacing;
                bool over = Input.MouseOver(StepRect(count, i, cx, y, spacing));

                if (i == current)
                {
                    Gfx.DrawGlow(sb, x, y, 22f, Palette.Accent * 0.35f);
                    Gfx.Diamond(sb, x, y, 9, Palette.Void);
                    Gfx.DiamondOutline(sb, x, y, 9, Palette.Accent, 2f);
                    Gfx.Diamond(sb, x, y, 4, Palette.Accent);
                }
                else if (i < current)
                {
                    Gfx.Diamond(sb, x, y, 5, over ? Palette.Highlight : Palette.PaperDim);
                }
                else
                {
                    Gfx.Diamond(sb, x, y, 5, Palette.Void);
                    Gfx.DiamondOutline(sb, x, y, 5, over ? Palette.Highlight : Palette.LineGrey, 1f);
                }

                Color label = (i == current) ? Palette.Highlight : (over ? Palette.Paper : Palette.LineGrey);
                Gfx.TextSpacedCentered(sb, Font, steps[i], x, y + 14, label, TextSize.Tiny, 2f);
            }
        }

        //Tooltip : a small box next to the mouse. body should already be wrapped.
        public static void Tooltip(SpriteBatch sb, string title, string body, float x, float y)
        {
            float w = Gfx.TextWidth(BigFont, title, TextSize.Small);
            float bodyW = Gfx.TextWidth(StoryFont, body, TextSize.StorySmall);
            if (bodyW > w) w = bodyW;
            float h = StoryFont.MeasureString(body).Y * TextSize.StorySmall;

            Rectangle box = new Rectangle((int)x + 16, (int)y + 16, (int)w + 28, (int)h + 54);

            //Screen Clamp : flip to the other side of the mouse near the edges
            if (box.Right > TacetGame.ScreenW - 8) box.X = (int)x - box.Width - 12;
            if (box.Bottom > TacetGame.ScreenH - 8) box.Y = (int)y - box.Height - 12;

            Gfx.Rect(sb, new Rectangle(box.X + 4, box.Y + 4, box.Width, box.Height), Color.Black * 0.5f);
            Gfx.Rect(sb, box, Palette.Void * 0.96f);
            Ornament.DoubleFrame(sb, box, Palette.PaperDim);
            Gfx.Text(sb, BigFont, title, box.X + 14, box.Y + 10, Palette.Highlight, TextSize.Small);
            Gfx.Text(sb, StoryFont, body, box.X + 14, box.Y + 40, Palette.PaperDim, TextSize.StorySmall);
        }

        //Slider : a rail with tick marks and a diamond knob sitting on it.
        //Like Button, this only draws. The page decides what a click or a drag means.
        public static void Slider(SpriteBatch sb, Rectangle track, float value, bool hot, float alpha)
        {
            if (value < 0f) value = 0f;
            if (value > 1f) value = 1f;

            float cy = track.Center.Y;
            Gfx.Rect(sb, track.X, cy - 1, track.Width, 2, Palette.LineGrey * alpha);
            Gfx.Rect(sb, track.X, cy - 1, track.Width * value, 2, Palette.Paper * alpha);

            //Gauge Marks : a tick every tenth, a taller one at each end and the middle
            for (int i = 0; i <= 10; i++)
                Gfx.Rect(sb, track.X + track.Width * i / 10f, cy + 7, 1, (i % 5 == 0) ? 7 : 4, Palette.LineGrey * alpha);

            //Knob
            float kx = track.X + track.Width * value;
            float size = hot ? 9f : 7f;
            if (hot) Gfx.DrawGlow(sb, kx, cy, 28f, Palette.Accent * (0.30f * alpha));
            Gfx.Diamond(sb, kx, cy, size, Palette.Void * alpha);
            Gfx.DiamondOutline(sb, kx, cy, size, (hot ? Palette.Accent : Palette.PaperDim) * alpha, 2f);
            Gfx.Diamond(sb, kx, cy, 3f, (hot ? Palette.Accent : Palette.PaperDim) * alpha);
        }

        //Check Box : empty when the option is off, ticked when it is on
        public static void CheckBox(SpriteBatch sb, Rectangle box, bool on, bool hot, float alpha)
        {
            Gfx.Rect(sb, box, Palette.Void * alpha);
            Gfx.RectOutline(sb, box, (hot ? Palette.Accent : Palette.LineGrey) * alpha, 1);
            if (!on) return;

            Color tick = (hot ? Palette.Highlight : Palette.Paper) * alpha;
            Gfx.Line(sb, box.X + 6, box.Center.Y, box.Center.X - 1, box.Bottom - 7, tick, 2f);
            Gfx.Line(sb, box.Center.X - 1, box.Bottom - 7, box.Right - 5, box.Y + 6, tick, 2f);
        }

        //Capsule Bar : a rounded bar, used for stamina everywhere
        public static void CapsuleBar(SpriteBatch sb, Rectangle r, float amount, Color fill, float alpha)
        {
            if (amount < 0f) amount = 0f;
            if (amount > 1f) amount = 1f;

            Gfx.Pill(sb, r, Palette.Void * alpha);
            Rectangle inner = r;
            inner.Inflate(-2, -2);
            int filledW = (int)(inner.Width * amount);
            if (filledW >= inner.Height)
                Gfx.Pill(sb, new Rectangle(inner.X, inner.Y, filledW, inner.Height), fill * alpha);
            else if (filledW > 0)
                Gfx.Circle(sb, inner.X + inner.Height / 2f, inner.Y + inner.Height / 2f, inner.Height / 2f * filledW / inner.Height, fill * alpha);

            //Segment Marks : a notch every tenth, so the bar reads like a gauge
            for (int i = 1; i < 10; i++)
                Gfx.Rect(sb, r.X + r.Width * i / 10f, r.Y + 2, 1, r.Height - 4, Palette.Void * (0.5f * alpha));
        }
    }
}
