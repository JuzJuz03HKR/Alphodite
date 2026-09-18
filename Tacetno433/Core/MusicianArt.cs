using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //MusicianArt : every way a musician is drawn, in one file.
    //
    //ALL OF THESE ARE PLACEHOLDERS. When the art is ready, each method becomes one
    //sb.Draw of the right picture into the same rectangle:
    //   Figure  -> full body art        (recruit page, rest page)
    //   Capsule -> the seated stage sprite (formation page, duel page)
    //   Tile    -> a cropped portrait   (score rows, small lists)
    //   Card    -> a tall portrait card (roster, picking a musician), uses Tile's art
    //Every page only passes a rectangle and a brightness, so nothing else has to change.
    public static class MusicianArt
    {
        //Figure : full body stand-in. A head, a long coat, the instrument mark, and a thin
        //rim of light down one side so the shape reads against the dark.
        public static void Figure(SpriteBatch sb, Rectangle box, Musician m, float bright)
        {
            Color body = m.ThemeColor * (0.55f * bright);
            Color rim = Palette.Highlight * (0.5f * bright);
            float cx = box.Center.X;
            float headRadius = box.Width * 0.17f;
            float headY = box.Y + box.Height * 0.14f;

            //Neck and Head
            Gfx.Rect(sb, cx - headRadius * 0.35f, headY + headRadius * 0.6f, headRadius * 0.7f, headRadius, body);
            Gfx.Circle(sb, cx, headY, headRadius, body);
            Gfx.Arc(sb, cx, headY, headRadius, -1.3f, 0.9f, rim, 2f);

            //Coat : shoulders, then a long flare down to the hem
            int top = (int)(headY + headRadius * 1.35f);
            for (int y = top; y < box.Bottom; y += 2)
            {
                float t = (float)(y - top) / (box.Bottom - top);
                float half = box.Width * (0.30f + t * 0.18f);
                if (t < 0.08f) half = box.Width * (0.18f + t * 1.5f);
                Gfx.Rect(sb, cx - half, y, half * 2f, 2, body);
                Gfx.Rect(sb, cx + half - 2, y, 2, 2, rim);
            }

            //Collar Line
            Gfx.Line(sb, cx, top + 4, cx, box.Bottom - 6, Palette.Void * (0.35f * bright), 1.5f);

            FamilyGlyph(sb, m.Family, cx, box.Y + box.Height * 0.55f, box.Width / 110f, Palette.Void * (0.75f * bright));
            Gfx.TextSpacedCentered(sb, Ui.Font, "ART", cx, box.Y - 18, Palette.LineGrey * bright, TextSize.Tiny, 3f);
        }

        //Capsule : the tall pill a musician becomes on stage, like the storyboard.
        //lit goes from 0 to 1 while they are playing, and makes them glow.
        public static void Capsule(SpriteBatch sb, Rectangle box, Musician m, Color fill, Color line, float lit)
        {
            if (lit > 0f)
                Gfx.DrawGlow(sb, box.Center.X, box.Center.Y, box.Height * 0.75f, Palette.Highlight * (0.5f * lit));

            Gfx.Capsule(sb, box, fill);
            Gfx.CapsuleOutline(sb, box, line, 3f);

            if (m != null)
                FamilyGlyph(sb, m.Family, box.Center.X, box.Center.Y, box.Width / 58f, line);
        }

        //Tile : a small square portrait crop, lit from above
        public static void Tile(SpriteBatch sb, Rectangle box, Musician m, float bright)
        {
            Gfx.Rect(sb, box, Palette.CanvasDark * bright);
            Gfx.DrawGlowBox(sb, new Rectangle(box.X - box.Width / 4, box.Y - box.Height / 3, box.Width * 3 / 2, box.Height),
                            Palette.Paper * (0.12f * bright));

            Color body = m.ThemeColor * (0.6f * bright);
            float headRadius = box.Width * 0.22f;
            Gfx.Circle(sb, box.Center.X, box.Y + box.Height * 0.40f, headRadius, body);
            for (int y = (int)(box.Y + box.Height * 0.62f); y < box.Bottom; y += 2)
            {
                float t = (y - (box.Y + box.Height * 0.62f)) / (box.Height * 0.38f);
                float half = box.Width * (0.22f + t * 0.16f);
                Gfx.Rect(sb, box.Center.X - half, y, half * 2f, 2, body);
            }

            Gfx.RectOutline(sb, box, Palette.LineGrey * bright, 1);
        }

        //Card : the tall lineup card from the reference. Portrait on top, the name with its
        //slash over the bottom of the portrait, instrument and stats below.
        //seatWord is the band along the bottom (FRONT, BENCH, a chance like 65%), empty for none.
        public static void Card(SpriteBatch sb, Rectangle r, Musician m, RunState run, bool hover, bool dim, string seatWord)
        {
            float bright = dim ? 0.35f : 1f;
            if (hover) r.Y -= 4;

            Gfx.Rect(sb, r, Palette.Panel * bright);

            //Portrait : the top of the card, everything except the 62 pixels of details under it
            Rectangle art = new Rectangle(r.X + 1, r.Y + 1, r.Width - 2, r.Height - 62);
            Gfx.Rect(sb, art, Palette.CanvasDark * bright);
            Gfx.DrawGlowBox(sb, new Rectangle(art.X - 20, art.Y - 20, art.Width + 40, art.Height), Palette.Paper * (0.10f * bright));
            PortraitBust(sb, art, m, bright);
            Ornament.FadeUp(sb, new Rectangle(art.X, art.Bottom - 34, art.Width, 34), 0.9f);

            //Name : serif with the slash, sitting on the bottom of the portrait
            Gfx.Text(sb, Ui.BigFont, m.NameTag, r.X + 8, art.Bottom - 30, Palette.Highlight * bright, TextSize.Small);
            Gfx.Rect(sb, r.X + 8, art.Bottom + 1, r.Width - 16, 1, Palette.Paper * (0.5f * bright));

            //Details
            Gfx.TextSpaced(sb, Ui.Font, m.Instrument, r.X + 8, art.Bottom + 6, Palette.PaperDim * bright, TextSize.Tiny, 1f);
            float half = r.Width / 2f;
            Gfx.Text(sb, Ui.Font, "PWR", r.X + 8, art.Bottom + 22, Palette.LineGrey * bright, TextSize.Tiny);
            Gfx.Text(sb, Ui.Font, NumberText.Get(run.PowerOf(m)), r.X + 8 + Gfx.TextWidth(Ui.Font, "PWR", TextSize.Tiny) + 5, art.Bottom + 19, Palette.Paper * bright, TextSize.Body);
            Gfx.Text(sb, Ui.Font, "COST", r.X + half + 2, art.Bottom + 22, Palette.LineGrey * bright, TextSize.Tiny);
            Gfx.Text(sb, Ui.Font, NumberText.Get(run.CostOf(m)), r.X + half + 2 + Gfx.TextWidth(Ui.Font, "COST", TextSize.Tiny) + 5, art.Bottom + 19, Palette.Paper * bright, TextSize.Body);

            //Seat Word : FRONT / MIDDLE / BACK / BENCH, or a chance like "65%"
            if (seatWord.Length > 0)
            {
                Rectangle band = new Rectangle(r.X, r.Bottom - 18, r.Width, 18);
                bool onStage = seatWord != "BENCH";
                Gfx.Rect(sb, band, (onStage ? Palette.Paper : Palette.Stage) * bright);
                Gfx.TextSpacedCentered(sb, Ui.Font, seatWord, band.Center.X, band.Y + 3,
                                       (onStage ? Palette.Ink : Palette.PaperDim) * bright, TextSize.Tiny, 2f);
            }

            Gfx.RectOutline(sb, r, (hover ? Palette.Highlight : Palette.LineGrey * 0.6f) * bright, 1);
            if (m.Rehearsed > 0) Ui.Pips(sb, r.Right - 12 - (m.Rehearsed - 1) * 10, r.Y + 12, m.Rehearsed, m.Rehearsed, 3, 10, bright);
        }

        //Portrait Bust : head and shoulders filling a box, used by cards
        private static void PortraitBust(SpriteBatch sb, Rectangle box, Musician m, float bright)
        {
            Color body = m.ThemeColor * (0.55f * bright);
            float cx = box.Center.X;
            float headRadius = box.Width * 0.2f;
            float headY = box.Y + box.Height * 0.38f;
            Gfx.Circle(sb, cx, headY, headRadius, body);
            Gfx.Arc(sb, cx, headY, headRadius, -1.2f, 0.8f, Palette.Highlight * (0.45f * bright), 2f);

            int top = (int)(headY + headRadius * 1.1f);
            for (int y = top; y < box.Bottom; y += 2)
            {
                float t = (float)(y - top) / (box.Bottom - top);
                float half = box.Width * (0.24f + t * 0.2f);
                Gfx.Rect(sb, cx - half, y, half * 2f, 2, body);
            }
            FamilyGlyph(sb, m.Family, cx, box.Y + box.Height * 0.84f, 0.8f, Palette.Void * (0.7f * bright));
        }

        //Family Glyph : a tiny mark for the instrument group, readable at any size
        //   STRING      two tall lines, like strings
        //   WIND        a bar with three holes
        //   PERCUSSION  a drum head with a stick
        public static void FamilyGlyph(SpriteBatch sb, Family family, float cx, float cy, float size, Color color)
        {
            float s = size;

            if (family == Family.String)
            {
                Gfx.Rect(sb, cx - 6 * s, cy - 14 * s, 2 * s, 28 * s, color);
                Gfx.Rect(sb, cx + 4 * s, cy - 14 * s, 2 * s, 28 * s, color);
                Gfx.Rect(sb, cx - 10 * s, cy + 2 * s, 20 * s, 2 * s, color);
            }
            else if (family == Family.Wind)
            {
                Gfx.Rect(sb, cx - 14 * s, cy - 3 * s, 28 * s, 6 * s, color);
                for (int i = -1; i <= 1; i++)
                    Gfx.Circle(sb, cx + i * 8 * s, cy - 8 * s, 2 * s, color);
            }
            else
            {
                Gfx.Circle(sb, cx, cy + 2 * s, 9 * s, color);
                Gfx.Line(sb, new Vector2(cx + 4 * s, cy - 4 * s), new Vector2(cx + 14 * s, cy - 16 * s), color, 2 * s);
            }
        }
    }
}
