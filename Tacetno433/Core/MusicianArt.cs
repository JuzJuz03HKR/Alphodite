using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //MusicianArt : every way a musician is shown outside the duel, in one file.
    //
    //   Figure  full body picture        (recruit page)         Art/Musicians/portrait_<name>
    //   Token   the pixel musician still (stage seats, bow, rest room)   see CharacterArt
    //   Tile    small square face        (score rows, panels)   Art/Musicians/face_<name>
    //   Card    a tall card with the portrait on top and the numbers below
    //   SeatBadge  where the musician sits : their section's mark and their arrow (round 15)
    //Each one draws the picture when it exists and an empty ArtSlot box when it does not.
    //Every page only passes a rectangle and a brightness, so nothing else has to change.
    public static class MusicianArt
    {
        //Figure : the full body picture
        public static void Figure(SpriteBatch sb, Rectangle box, Musician m, float bright)
        {
            ArtBank.DrawOrSlot(sb, ArtBank.PortraitOf(m), box, Palette.Paper, bright);
        }

        //Token : the musician as a small standing sprite. lit (0 to 1) makes them glow.
        public static void Token(SpriteBatch sb, Rectangle box, Musician m, float bright, float lit)
        {
            if (lit > 0f)
                Gfx.DrawGlow(sb, box.Center.X, box.Center.Y, box.Height * 0.75f, Palette.Highlight * (0.5f * lit));

            CharacterArt.DrawStill(sb, m, box, Palette.Paper, bright);
        }

        //Tile : the small square face
        public static void Tile(SpriteBatch sb, Rectangle box, Musician m, float bright)
        {
            Gfx.Rect(sb, box, Palette.CanvasDark * bright);
            ArtBank.DrawOrSlot(sb, ArtBank.FaceOf(m), box, Palette.Paper, bright);
        }

        //Card : the tall lineup card. Portrait on top, the name with its slash over the bottom
        //of the portrait, instrument and numbers below.
        //seatWord is the band along the bottom (FRONT, BENCH, a chance like 65%), empty for none.
        //The number is the musician's own power with rehearsals and motifs added, the same number
        //every other page shows (RunState.PowerOf). Beside it, the mark of the section they sit in.
        public static void Card(SpriteBatch sb, Rectangle r, Musician m, RunState run, bool hover, bool dim, string seatWord)
        {
            float bright = dim ? 0.35f : 1f;
            if (hover) r.Y -= 4;

            Gfx.Rect(sb, r, Palette.Panel * bright);

            //Portrait : the top of the card, everything except the 62 pixels of details under it
            Rectangle art = new Rectangle(r.X + 1, r.Y + 1, r.Width - 2, r.Height - 62);
            Gfx.Rect(sb, art, Palette.CanvasDark * bright);
            ArtBank.DrawOrSlot(sb, ArtBank.PortraitOf(m), art, Palette.Paper, bright);
            Ornament.FadeUp(sb, new Rectangle(art.X, art.Bottom - 34, art.Width, 34), 0.9f);

            //Seat : the section mark and the arrow in the top corner, so every card says where they sit
            SeatBadge(sb, m, r.X + 22, r.Y + 24, 13f, Palette.Ink, Palette.Paper, bright);

            //Name : serif with the slash, sitting on the bottom of the portrait
            Gfx.Text(sb, Ui.BigFont, m.NameTag, r.X + 8, art.Bottom - 30, Palette.Highlight * bright, TextSize.Small);
            Gfx.Rect(sb, r.X + 8, art.Bottom + 1, r.Width - 16, 1, Palette.Paper * (0.5f * bright));

            //Details
            Gfx.TextSpaced(sb, Ui.Font, m.Instrument, r.X + 8, art.Bottom + 6, Palette.PaperDim * bright, TextSize.Tiny, 1f);
            Gfx.Text(sb, Ui.Font, "PWR", r.X + 8, art.Bottom + 22, Palette.LineGrey * bright, TextSize.Tiny);
            Gfx.Text(sb, Ui.Font, NumberText.Get(run.PowerOf(m)), r.X + 8 + Gfx.TextWidth(Ui.Font, "PWR", TextSize.Tiny) + 5, art.Bottom + 19, Palette.Paper * bright, TextSize.Body);
            FamilyGlyph(sb, m.Family, r.Right - 16, art.Bottom + 31, 0.42f, Palette.LineGrey * bright);

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

        //Seat Badge : where a musician sits (round 15) : a disc with their section's mark (the
        //same little icons as the family, strings, winds, percussion) and an arrow on its edge,
        //the side the baton points at them : left, right, or up and down together for the centre.
        //Round 14 it held a letter, p, mf or f, the smallest stroke that brought them in.
        //This is an interface icon, not artwork, so it stays.
        public static void SeatBadge(SpriteBatch sb, Musician m, float cx, float cy, float radius, Color ink, Color paper, float alpha)
        {
            SeatBadge(sb, StageLayout.SectionOf(m.Family), m.Cue, cx, cy, radius, ink, paper, alpha);
        }

        //Seat Badge : any section and side, for REQUIEM's mirrored round and for empty chairs
        public static void SeatBadge(SpriteBatch sb, int section, int side, float cx, float cy, float radius, Color ink, Color paper, float alpha)
        {
            Gfx.Circle(sb, cx, cy, radius, paper * alpha);
            Gfx.CircleOutline(sb, cx, cy, radius, ink * alpha, radius >= 14f ? 2f : 1f);
            FamilyGlyph(sb, StageLayout.Rows[section].Family, cx, cy + radius * 0.05f, radius / 24f, ink * alpha);

            //Arrow : a head on the edge, pointing the way they answer. A dark head under a light
            //one, like the pointers on the notes, so it reads on a bright stage and on the dark.
            float head = radius * 0.62f;
            float gap = radius + head * 0.62f;
            ArrowHead(sb, side, cx, cy, gap, head * 1.45f, Palette.Ink * alpha);
            ArrowHead(sb, side, cx, cy, gap, head, Palette.Paper * alpha);
        }

        private static void ArrowHead(SpriteBatch sb, int side, float cx, float cy, float gap, float size, Color color)
        {
            if (side == 0) Gfx.Arrow(sb, cx - gap, cy, size, false, color);
            else if (side == 2) Gfx.Arrow(sb, cx + gap, cy, size, true, color);
            else
            {
                Gfx.Triangle(sb, cx, cy - gap, size, true, color);
                Gfx.Triangle(sb, cx, cy + gap, size, false, color);
            }
        }

        //Family Glyph : a tiny mark for the instrument group, readable at any size.
        //This is an interface icon, not artwork, so it stays.
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
