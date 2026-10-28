using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //Ui : the shared pieces every page is built from, so all pages look like one game.
    //
    //Beta1 screens (28 Oct, docs/SCREENS_LEVEL_UP.md 3.4.1), moved by Core/UiMotion.cs :
    //   MenuItem    one line of a menu : number, circle on the page's line, word, the ink bar when pointed at
    //   Command     CONTINUE / BACK / NEXT : a slanted plate that turns white and tears like ink when pointed at
    //   Number      a number that matters, big and blocky, counting up over a thin line (menu7)
    //   InkBar      the slanted bar with a torn front end both of them use
    //   KeyBox      the key that does the job, a small filled box
    //Before Beta1 (pages not lifted yet) :
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
    //   ModeSwitch  NORMAL or MAESTRO, the chosen half filled (29 Sep, the conductor pages)
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

        //======================================================================================
        //Beta1 screens : the mixed buttons, moved by Core/UiMotion.cs
        //======================================================================================

        private const float TearDepth = 12f;      // INK BAR : how far back its torn end reaches (pixels)
        private const float ItemHeight = 30f;     // MENU ITEM : the ink bar's height
        private const float ItemSlant = 10f;      //   and its lean

        //Ink Bar : a slanted bar whose front end is torn like a stroke of ink (menu1). It shoots out under a pointed menu
        //line, and it is the face of a pointed button. Drawn in rows two pixels tall (PIXEL x2) : the top leans forward by
        //slant pixels like the blades, and each row stops at its own place at the front. tear 0..1 is how ragged
        //(0 = a clean slanted box), seed fixes the tear so it never flickers.
        public static void InkBar(SpriteBatch sb, float x, float y, float length, float height, float slant, Color color, int seed, float tear)
        {
            if (length < 2f) return;
            int rows = Math.Max(1, (int)(height / 2f));
            float depth = Math.Min(TearDepth, length * 0.5f) * tear;
            for (int i = 0; i < rows; i++)
            {
                float lean = slant * (1f - (i + 0.5f) / rows);
                float from = x + lean;
                float to = x + length + lean - depth * Hash(seed, i);
                if (to > from) Gfx.Rect(sb, from, y + i * 2f, to - from, 2f, color);
            }

            //Specks : drops of ink flung past the torn end, once the bar is most of the way out
            float specks = MathHelper.Clamp((length - 40f) / 40f, 0f, 1f) * tear;
            if (specks <= 0f) return;
            for (int k = 0; k < 4; k++)
            {
                float a = Hash(seed + 31, k);
                float b = Hash(seed + 57, k);
                float size = a > 0.55f ? 4f : 2f;
                float sx = (float)Math.Floor((x + length + 2f + a * 16f) * 0.5f) * 2f;
                float sy = (float)Math.Floor((y + 2f + b * (height - 6f)) * 0.5f) * 2f;
                Gfx.Rect(sb, sx, sy, size, size, color * specks);
            }
        }

        //Key Box : the key that does the job, a small filled box (menu7). Returns how wide it was.
        public static float KeyBox(SpriteBatch sb, string key, float x, float cy, Color fill, Color ink, float alpha)
        {
            float w = Gfx.TextWidth(Font, key, TextSize.Tiny) + 12f;
            Rectangle box = new Rectangle((int)x, (int)(cy - 9f), (int)w, 18);
            Gfx.Rect(sb, box, fill * alpha);
            Gfx.TextCentered(sb, Font, key, box.Center.X, box.Center.Y, ink * alpha, TextSize.Tiny);
            return w;
        }

        //Menu Item : one line of a menu (title, pause, settings). A small grey number, a hollow circle that sits on the
        //page's line, a thin leader, then the word. Pointed at : the ink bar shoots out of the circle under the word, the
        //word turns black, grows and leans (UiHoverScale, UiHoverTilt), a grey bar sits behind (UiShadow, Persona 5), the
        //circle fills and sends out one ring (menu4), and the key that does it waits at the end of the bar (menu7).
        //Pressed : squashed, bounced, a flash. at is the circle. Returns where the bar ends, for a note beside it.
        public static float MenuItem(SpriteBatch sb, Vector2 at, string index, string name, string key, UiMotion m, float alpha)
        {
            float e = m.Shown;
            float s = m.Scale;
            float cx = at.X;
            float cy = at.Y;
            int seed = index.Length > 0 ? index[index.Length - 1] * 7 : 7;

            //Measures : the word grows when pointed at and squashes when pressed
            float scale = TextSize.Heading * (1f + (BattleRules.UiHoverScale - 1f) * e) * s;
            const float tracking = 2.5f;
            float wordX = cx + 28f;
            float wordW = Gfx.SpacedWidth(Font, name, scale, tracking);
            float keyW = key.Length > 0 ? Gfx.TextWidth(Font, key, TextSize.Tiny) + 12f : 0f;
            float barX = cx + 6f;
            float full = wordX - barX + wordW + 16f + (keyW > 0f ? keyW + 12f : 0f);
            float barH = ItemHeight * s;

            //Bar : the grey one behind, then the white one, shooting out together
            if (e > 0f)
            {
                float length = full * e;
                InkBar(sb, barX + BattleRules.UiShadow, cy - barH / 2f + BattleRules.UiShadow, length, barH, ItemSlant, Palette.LineGrey * (0.5f * alpha), seed + 3, 1f);
                InkBar(sb, barX, cy - barH / 2f, length, barH, ItemSlant, Palette.Paper * alpha, seed, 1f);
            }

            //Number and leader
            Gfx.TextRight(sb, Font, index, cx - 14f, cy - Font.LineSpacing * TextSize.Label / 2f, Color.Lerp(Palette.LineGrey, Palette.Paper, e) * alpha, TextSize.Label);
            if (e < 1f) Gfx.Rect(sb, cx + 8f, cy, 14f, 1f, Palette.LineGrey * ((1f - e) * alpha));

            //Circle : hollow, filling white as it is pointed at, then one thin ring goes out
            Gfx.CircleOutline(sb, cx, cy, 5f, Color.Lerp(Palette.LineGrey, Palette.Paper, e) * alpha, 1.5f);
            if (e > 0f) Gfx.Circle(sb, cx, cy, 3.5f * e, Palette.Paper * (e * alpha));
            float ring = m.Ring;
            if (ring < 1f) Gfx.CircleOutline(sb, cx, cy, 5f + 22f * ring, Palette.Paper * ((1f - ring) * 0.8f * alpha), 1.5f);

            //Word : black on the bar, leaning a little
            float tilt = MathHelper.ToRadians(-BattleRules.UiHoverTilt) * e;
            Color word = Color.Lerp(Palette.PaperDim, Palette.Ink, e);
            Gfx.TextSpacedTurned(sb, Font, name, wordX, cy, word * alpha, scale, tracking, tilt);

            //Key : waits at the end of the bar once it is out
            float keyA = MathHelper.Clamp((e - 0.6f) / 0.4f, 0f, 1f);
            if (keyW > 0f && keyA > 0f) KeyBox(sb, key, wordX + wordW + 16f, cy, Palette.Ink, Palette.Paper, keyA * alpha);

            //Flash : the press
            float flash = m.Flash;
            if (flash > 0f) Gfx.DrawGlow(sb, wordX + wordW / 2f, cy, wordW * 0.7f + 50f, Color.White * (0.55f * flash * alpha));

            return barX + full;
        }

        //Command : CONTINUE, CURTAIN CALL, BACK, NEXT. A slanted plate, dark with a thin bright edge and a bright key box
        //(menu7). Pointed at : it turns white, its front torn like ink, a grey plate behind (Persona 5), and it steps
        //forward a little. Pressed : squashed, bounced, a flash. onPaper : on a bright page the plate stays black, the
        //strongest thing there, and pointing tears it and puts the grey plate behind.
        public static void Command(SpriteBatch sb, Rectangle box, string label, string key, UiMotion m, float alpha, bool onPaper)
        {
            float e = m.Shown;
            float s = m.Scale * (1f + 0.03f * e);
            float w = box.Width * s;
            float h = box.Height * s;
            float x = box.Center.X - w / 2f + 6f * e;
            float y = box.Center.Y - h / 2f;
            int seed = label.Length > 0 ? label[0] * 3 : 11;

            Color face = onPaper ? Palette.Ink : Color.Lerp(Palette.Panel, Palette.Highlight, e);
            Color word = onPaper ? Palette.Paper : Color.Lerp(Palette.Paper, Palette.Ink, e);

            //Plate : the grey one behind when pointed at, the face (clean at rest, torn when pointed at), the thin edge at rest
            if (e > 0f) InkBar(sb, x + BattleRules.UiShadow, y + BattleRules.UiShadow, w, h, Slant, Palette.LineGrey * (0.6f * e * alpha), seed + 5, e);
            InkBar(sb, x, y, w, h, Slant, face * ((onPaper ? 1f : 0.92f + 0.08f * e) * alpha), seed, e);
            if (e < 1f) Gfx.SlantOutline(sb, new Rectangle((int)x, (int)y, (int)w, (int)h), Slant, (onPaper ? Palette.Ink : Palette.Paper * 0.7f) * ((1f - e) * alpha), 1f);

            //Key Box : bright on the dark plate, dark on the white one
            float right = x + w - Slant - 10f;
            if (key.Length > 0)
            {
                float keyW = Gfx.TextWidth(Font, key, TextSize.Tiny) + 12f;
                right -= keyW;
                Color keyFill = onPaper ? Palette.Paper : Color.Lerp(Palette.Paper, Palette.Ink, e);
                Color keyInk = onPaper ? Palette.Ink : Color.Lerp(Palette.Ink, Palette.Paper, e);
                KeyBox(sb, key, right, y + h / 2f, keyFill, keyInk, alpha);
                right -= 10f;
            }

            //Label : centred between the slanted left end and the key box, smaller if it would not fit
            float left = x + Slant + 10f;
            float scale = TextSize.Heading * s;
            float width = Gfx.SpacedWidth(Font, label, scale, 1.5f);
            if (width > right - left)
            {
                scale = TextSize.Body * s;
                width = Gfx.SpacedWidth(Font, label, scale, 1.5f);
            }
            Gfx.TextSpaced(sb, Font, label, (left + right) / 2f - width / 2f, y + h / 2f - Font.LineSpacing * scale / 2f, word * alpha, scale, 1.5f);

            //Mark : a circle before the plate, filling and sending out a ring when pointed at
            float mx = x - 16f;
            float my = y + h / 2f;
            Color markColor = onPaper ? Palette.Ink : Palette.Paper;
            Gfx.CircleOutline(sb, mx, my, 5f, markColor * ((0.35f + 0.65f * e) * alpha), 1.5f);
            if (e > 0f) Gfx.Circle(sb, mx, my, 3.5f * e, markColor * (e * alpha));
            if (m.Ring < 1f) Gfx.CircleOutline(sb, mx, my, 5f + 22f * m.Ring, markColor * ((1f - m.Ring) * 0.8f * alpha), 1.5f);

            //Flash : the press
            if (m.Flash > 0f) Gfx.DrawGlow(sb, x + w / 2f, y + h / 2f, w * 0.75f, Color.White * (0.6f * m.Flash * alpha));
        }

        //Number : a number that matters (menu7). A small spaced label on the left, the number big and blocky on the
        //right (PixelDigits), and a thin line under them that fills as the number counts up, a small circle riding its end.
        //shown is the number this frame (UiMotion.Count), fill 0..1 how far the count has gone. Row height about 44.
        public static void Number(SpriteBatch sb, string label, int shown, bool sign, float x, float y, float width, float fill, Color ink, Color soft, float alpha)
        {
            Gfx.TextSpaced(sb, Font, label, x, y + 8f, soft * alpha, TextSize.Label, 3f);
            const float cell = 4f;
            float numberW = PixelDigits.Width(shown, sign, cell);
            PixelDigits.Draw(sb, shown, sign, x + width - numberW / 2f, y + 14f, cell, ink * alpha, Color.Transparent);

            float lineY = y + 36f;
            float to = width * MathHelper.Clamp(fill, 0f, 1f);
            Gfx.Rect(sb, x, lineY, width, 1f, soft * (0.4f * alpha));
            Gfx.Rect(sb, x, lineY - 1f, to, 2f, ink * (0.85f * alpha));
            Gfx.CircleOutline(sb, x + to, lineY, 3f, ink * alpha, 1f);
        }

        //Hash : a steady random 0..1 from two whole numbers (the sine trick of TacetField)
        private static float Hash(int a, int b)
        {
            float h = (float)Math.Sin(a * 12.9898f + b * 78.233f) * 43758.547f;
            return h - (float)Math.Floor(h);
        }

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

        //Mode Switch : NORMAL or MAESTRO side by side, the chosen half filled, a one line reminder
        //under it (MAESTRO MODE, 29 Sep). The conductor pages flip Settings.Maestro on a click or M.
        public const string ModeKey = "M";
        private const string NormalLine = "Slower. Easier to keep up.";
        private const string MaestroLine = "Full speed from the first fight.";

        public static void ModeSwitch(SpriteBatch sb, Rectangle box, bool maestro, bool hover, float alpha)
        {
            int half = box.Width / 2;
            ModeHalf(sb, new Rectangle(box.X, box.Y, half, box.Height), "NORMAL", !maestro, alpha);
            ModeHalf(sb, new Rectangle(box.X + half, box.Y, box.Width - half, box.Height), "MAESTRO", maestro, alpha);
            Gfx.RectOutline(sb, box, (hover ? Palette.Highlight : Palette.LineGrey) * alpha, 1);

            Gfx.TextSpaced(sb, Font, "MODE", box.X, box.Y - 20, Palette.LineGrey * alpha, TextSize.Tiny, 2f);
            KeyChipRight(sb, ModeKey, box.Right, box.Y - 13, Palette.PaperDim * alpha);
            Gfx.TextRight(sb, StoryFont, maestro ? MaestroLine : NormalLine, box.Right, box.Bottom + 5, Palette.PaperDim * alpha, TextSize.StorySmall);
        }

        private static void ModeHalf(SpriteBatch sb, Rectangle r, string word, bool on, float alpha)
        {
            if (on) Gfx.Rect(sb, r, Palette.Paper * alpha);
            Gfx.TextSpacedCentered(sb, Font, word, r.Center.X, r.Center.Y - 8, (on ? Palette.Ink : Palette.PaperDim) * alpha, TextSize.Label, 2f);
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
