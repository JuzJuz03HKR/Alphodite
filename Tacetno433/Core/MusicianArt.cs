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
    //   SideBadge  the side of the stage they sit on, as an arrow in a disc (round 15)
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
        //every other page shows (RunState.PowerOf). Beside it, the section they sit in, in short.
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

            //Side : the arrow in the top corner, so every card says which way of the baton they answer.
            //A short card (a big ensemble, three rows of cards) has no room above the name, so the
            //arrow moves to the end of the name line and the name shrinks to fit beside it.
            bool shortCard = art.Height < 90;
            float nameSize = TextSize.Small;
            if (shortCard)
            {
                SideBadge(sb, m.Cue, r.Right - 16, art.Bottom - 16, 10f, Palette.Ink, Palette.Paper, bright);
                float room = r.Width - 16 - 26;
                float width = Gfx.TextWidth(Ui.BigFont, m.NameTag, nameSize);
                if (width > room) nameSize *= room / width;
            }
            else
            {
                SideBadge(sb, m.Cue, r.X + 22, r.Y + 24, 13f, Palette.Ink, Palette.Paper, bright);
            }

            //Name : serif with the slash, sitting on the bottom of the portrait
            Gfx.Text(sb, Ui.BigFont, m.NameTag, r.X + 8, art.Bottom - 30, Palette.Highlight * bright, nameSize);
            Gfx.Rect(sb, r.X + 8, art.Bottom + 1, r.Width - 16, 1, Palette.Paper * (0.5f * bright));

            //Details
            Gfx.TextSpaced(sb, Ui.Font, m.Instrument, r.X + 8, art.Bottom + 6, Palette.PaperDim * bright, TextSize.Tiny, 1f);
            Gfx.Text(sb, Ui.Font, "PWR", r.X + 8, art.Bottom + 22, Palette.LineGrey * bright, TextSize.Tiny);
            Gfx.Text(sb, Ui.Font, NumberText.Get(run.PowerOf(m)), r.X + 8 + Gfx.TextWidth(Ui.Font, "PWR", TextSize.Tiny) + 5, art.Bottom + 19, Palette.Paper * bright, TextSize.Body);
            Gfx.TextSpacedRight(sb, Ui.Font, StageLayout.Rows[StageLayout.SectionOf(m.Family)].Short, r.Right - 8, art.Bottom + 23, Palette.LineGrey * bright, TextSize.Tiny, 1f);

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

        //Side Badge : the side of the stage a musician sits on, the way the baton points to make
        //them hit harder (CUE) : a disc with an arrow in it, left, right, or down and up together
        //for the centre. Round 15 : only the arrow, the little drawn instrument icons that sat in
        //it were hard to read (the section is written out in words where it matters).
        //This is an interface icon, not artwork, so it stays.
        public static void SideBadge(SpriteBatch sb, int side, float cx, float cy, float radius, Color ink, Color paper, float alpha)
        {
            Gfx.Circle(sb, cx, cy, radius, paper * alpha);
            Gfx.CircleOutline(sb, cx, cy, radius, ink * alpha, radius >= 14f ? 2f : 1f);
            float length = radius * 1.3f;
            float head = radius * 0.5f;
            float thick = radius >= 14f ? 3f : 2f;
            if (side == 0) NoteGlyph.Arrow(sb, Flick.Left, cx, cy, length, head, ink * alpha, thick);
            else if (side == 2) NoteGlyph.Arrow(sb, Flick.Right, cx, cy, length, head, ink * alpha, thick);
            else
            {
                NoteGlyph.Arrow(sb, Flick.Down, cx - radius * 0.3f, cy, length * 0.9f, head * 0.9f, ink * alpha, thick);
                NoteGlyph.Arrow(sb, Flick.Up, cx + radius * 0.3f, cy, length * 0.9f, head * 0.9f, ink * alpha, thick);
            }
        }
    }
}
