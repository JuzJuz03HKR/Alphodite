using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //TutorialScreen.Seats : the WHO PLAYS lesson (round 14, it was WHERE THEY SIT in round 12.1).
    //
    //A small band seen from the conductor's place, the same way round as the BAND page: the p
    //row nearest the conductor, f at the back, the arrows across the top. Three players of one
    //era, a wind (p), a string (mf) and a percussionist (f), each wearing their PART badge.
    //   The first three strokes are p, mf and f. The parts each size brings in light up while the
    //   stroke is drawn, so LETTER = STROKE SIZE can be seen (DYNAMICS).
    //   The next four draw the 4/4 shape. Each way sends a beam from the conductor to the player
    //   whose arrow it matches, who glows x1.5, so ARROW = CUE can be seen.
    //Nothing here is a real duel, the numbers are only words on the page.
    public partial class TutorialScreen
    {
        //Mini Stage : right of the hit point, clear of TACET's dark
        private static readonly Rectangle SeatsBox = new Rectangle(730, 150, 250, 256);
        private const float SeatsRowLabelX = 748f;         // p, mf, f at the start of each row
        private static float[] seatsColumnX = { 810f, 870f, 930f };   // left, down and up, right
        private static float[] seatsRowY = { 355f, 285f, 215f };      // p near the conductor, f at the back
        private const int SeatsCapsuleW = 26;
        private const int SeatsCapsuleH = 46;

        //The Band : the era of Siam, one player for every letter and every arrow (their home seats)
        private static string[] seatsNames = { "CHAI", "MALI", "NUAN" };
        private int[] seatsBand = new int[3];
        private static string[] seatsRowMarks = { "p", "mf", "f" };
        private const string CueBonusWord = "x1.5";
        private const string ConductorWord = "CONDUCTOR";

        //Hints : what just happened, one line each
        private static string[] seatsSizeDone =
        {
            "p : ONLY THE p PLAYER PLAYS. NOW A MIDDLE ONE.",
            "mf : THE mf PLAYER JOINS. NOW A LONG ONE.",
            "f : EVERYONE PLAYS. NOW THE 4/4 SHAPE."
        };
        private static string[] seatsCueDone =                                       // in pattern order
        {
            "DOWN : THE DOWN / UP PLAYER HITS HARDER.",
            "LEFT : THE LEFT PLAYER HITS HARDER.",
            "RIGHT : THE RIGHT PLAYER HITS HARDER.",
            "UP : THE DOWN / UP PLAYER AGAIN."
        };

        //Seats State
        private float[] seatsRowFlash = new float[3];     // a part that just played, fading
        private float seatsCueFlash;                     // the beam of the last way drawn, fading
        private int seatsCueSide = -1;                   // the arrow it matched
        private bool seatsPicture;                       // DEVELOPER TOOL : the beam stays for a picture

        private void ResetSeats()
        {
            for (int r = 0; r < seatsRowFlash.Length; r++) seatsRowFlash[r] = 0f;
            seatsCueFlash = 0f;
            seatsCueSide = -1;
            seatsPicture = false;

            //Home Seats : where each of the three sits, from their own part (StageLayout.HomeSeat)
            for (int i = 0; i < seatsNames.Length; i++)
                for (int k = 0; k < MusicianList.All.Length; k++)
                    if (MusicianList.All[k].Name == seatsNames[i]) seatsBand[i] = StageLayout.HomeSeat(MusicianList.All[k]);
        }

        private void UpdateSeats(float dt)
        {
            if (seatsPicture) return;
            for (int r = 0; r < seatsRowFlash.Length; r++) seatsRowFlash[r] = Math.Max(0f, seatsRowFlash[r] - dt * 1.2f);
            seatsCueFlash = Math.Max(0f, seatsCueFlash - dt * 0.8f);
        }

        //Seats Way : p, mf and f are all swung down, then the 4/4 shape
        private Flick SeatsWay()
        {
            return done < 3 ? Flick.Down : pattern[(done - 3) % 4];
        }

        //Seats Stroke : a stroke the lesson accepted, before done moves on
        private void SeatsStroke(int size)
        {
            if (done < 3)
            {
                //DYNAMICS : the p player on every stroke, the mf player from mf, the f player from f
                for (int r = 0; r <= size && r < seatsRowFlash.Length; r++) seatsRowFlash[r] = 1f;
                Hint(seatsSizeDone[done]);
                return;
            }

            //CUE : down and up share one arrow, left and right have their own
            int k = (done - 3) % 4;
            seatsCueSide = BattleRules.CueSide[k];
            seatsCueFlash = 1f;
            for (int r = 0; r < seatsRowFlash.Length; r++) seatsRowFlash[r] = Math.Max(seatsRowFlash[r], 0.4f);
            Hint(seatsCueDone[k]);
        }

        //Seats Draw : the mini stage, its labels, the band, and the beam
        private void DrawSeats(SpriteBatch sb)
        {
            Rectangle box = SeatsBox;
            Gfx.Rect(sb, new Rectangle(box.X + 5, box.Y + 6, box.Width, box.Height), Color.Black * 0.25f);
            Gfx.Rect(sb, box, Palette.Void * 0.9f);
            Ornament.DoubleFrame(sb, box, Palette.PaperDim);

            //Live Size : while a stroke is drawn, the parts it would bring in light up
            int live = -1;
            if (gesture.Held && gesture.InStroke)
                live = Baton.SizeOf(Vector2.Dot(gesture.LiveVector, NoteGlyph.Way(SeatsWay())));

            //Arrows : over the columns, the one the next stroke goes toward stands out
            int nextSide = done >= 3 && done < 7 ? BattleRules.CueSide[(done - 3) % 4] : -1;
            for (int c = 0; c < 3; c++)
            {
                bool marked = seatsCueFlash > 0f ? c == seatsCueSide : c == nextSide;
                Color ink = marked ? Palette.Highlight : Palette.LineGrey;
                float y = box.Y + 20;
                if (c == 0) NoteGlyph.Arrow(sb, Flick.Left, seatsColumnX[c], y, 22f, 9f, ink, 2f);
                else if (c == 2) NoteGlyph.Arrow(sb, Flick.Right, seatsColumnX[c], y, 22f, 9f, ink, 2f);
                else
                {
                    NoteGlyph.Arrow(sb, Flick.Down, seatsColumnX[c] - 6, y, 18f, 8f, ink, 2f);
                    NoteGlyph.Arrow(sb, Flick.Up, seatsColumnX[c] + 6, y, 18f, 8f, ink, 2f);
                }
            }

            //Rows : the letter of each part and its chairs
            for (int r = 0; r < 3; r++)
            {
                bool rowLit = live >= r || seatsRowFlash[r] > 0.05f;
                float y = seatsRowY[r];
                Gfx.TextCentered(sb, Game.BigFont, seatsRowMarks[r], SeatsRowLabelX, y - 2f,
                                 rowLit ? Palette.Highlight : Palette.LineGrey, TextSize.Small);
                if (live >= r)
                    Gfx.Rect(sb, box.X + 36, y - SeatsCapsuleH / 2 - 6, box.Width - 48, SeatsCapsuleH + 12, Palette.Highlight * 0.12f);
                for (int c = 0; c < 3; c++)
                    Gfx.CapsuleOutline(sb, SeatsCapsule(c, y), Palette.LineGrey * 0.5f, 1f);
            }

            //Conductor : under the stage, where every beam starts
            Vector2 podium = new Vector2(box.Center.X, box.Bottom + 22);
            Gfx.Diamond(sb, podium.X, podium.Y, 7, Palette.Ink);
            Gfx.TextSpacedCentered(sb, Game.Font, ConductorWord, podium.X, podium.Y + 12, Palette.InkSoft, TextSize.Tiny, 3f);

            //CUE Beam : from the conductor to the player whose arrow the last way matched,
            //drawn under the band so their capsules and names stay on top
            if (seatsCueFlash > 0f && seatsCueSide >= 0)
                for (int i = 0; i < seatsBand.Length; i++)
                {
                    int seat = seatsBand[i];
                    if (StageLayout.SeatSide(seat) != seatsCueSide) continue;
                    Rectangle cap = SeatsCapsule(seatsCueSide, seatsRowY[StageLayout.SeatRow[seat]]);
                    Vector2 to = new Vector2(cap.Center.X, cap.Bottom);
                    Gfx.Line(sb, podium, to, Palette.Highlight * (0.5f * seatsCueFlash), 7f);
                    Gfx.Line(sb, podium, to, Palette.Ink * seatsCueFlash, 2f);
                }

            //The Band : a capsule each with its badge, glowing while its part plays or its arrow is followed
            for (int i = 0; i < seatsBand.Length; i++)
            {
                int seat = seatsBand[i];
                int row = StageLayout.SeatRow[seat];
                int side = StageLayout.SeatSide(seat);
                Rectangle cap = SeatsCapsule(side, seatsRowY[row]);
                bool cued = seatsCueFlash > 0f && side == seatsCueSide;
                float glow = Math.Max(live >= row ? 0.9f : 0f, Math.Max(seatsRowFlash[row], cued ? seatsCueFlash : 0f));

                if (glow > 0f) Gfx.DrawGlow(sb, cap.Center.X, cap.Center.Y, 40f, Palette.Highlight * (0.55f * glow));
                Gfx.Capsule(sb, cap, glow > 0.3f ? Palette.Paper : Palette.Stage);
                Gfx.CapsuleOutline(sb, cap, Palette.Paper, 1f);
                MusicianArt.PartBadge(sb, row, side, cap.Center.X, cap.Center.Y - 4, 8f, Palette.Paper, Palette.Ink, 1f);
                Gfx.TextSpacedCentered(sb, Game.Font, seatsNames[i], cap.Center.X, cap.Bottom + 3, Palette.PaperDim, TextSize.Tiny * 0.85f, 1f);
                if (cued)
                    Gfx.TextSpacedCentered(sb, Game.Font, CueBonusWord, cap.Right + 16, cap.Y - 6, Palette.Highlight * seatsCueFlash, TextSize.Tiny, 1f);
            }
        }

        private Rectangle SeatsCapsule(int column, float rowY)
        {
            return new Rectangle((int)(seatsColumnX[column] - SeatsCapsuleW / 2), (int)(rowY - SeatsCapsuleH / 2 - 6),
                                 SeatsCapsuleW, SeatsCapsuleH);
        }

        //Seats For Picture : DEVELOPER TOOL hook, the lesson just after a LEFT stroke
        public void SeatsForPicture()
        {
            done = 5;
            seatsPicture = true;
            seatsCueSide = BattleRules.CueSide[1];
            seatsCueFlash = 1f;
            Hint(seatsCueDone[1]);
        }
    }
}
