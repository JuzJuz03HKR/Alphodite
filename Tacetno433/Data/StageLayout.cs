using Microsoft.Xna.Framework;

namespace Tacetno433.Data
{
    //StageRow : one PART of the orchestra, the players who come in on the same strokes
    public class StageRow
    {
        public string Name = "";          // "WINDS"
        public string Mark = "";          // the smallest stroke that brings the part in, "p"
        public string Strokes = "";       // "EVERY STROKE"
        public Family Family;             // the instruments that play this part
    }

    //StageLayout : THE PLACE TO EDIT THE STAGE.
    //
    //Round 14 : every musician plays a PART, printed on their card the way a note is printed
    //in the lane, a letter and an arrow:
    //   LETTER  p, mf or f, set by their instrument (BattleRules DYNAMICS). A small stroke brings
    //           the p players in, a middle one the mf players too, a big one everybody.
    //              WINDS p   every stroke        (cheap, quiet, always there)
    //              STRINGS mf   middle and big strokes
    //              PERCUSSION f   big strokes only (the heaviest hitters)
    //   ARROW   the way of the baton they answer, left, down and up, or right. When the baton
    //           goes their way they hit harder (BattleRules CUE).
    //
    //So there is nothing to arrange before a fight any more (the STAGE page and its seats are
    //gone): who plays is written on the players themselves. Every letter and arrow pair belongs
    //to exactly one musician of the nine, so each musician has one chair of their own, their
    //HOME SEAT, like a real orchestra where the second horn always sits in the same place.
    //The nine chairs are kept as seat numbers because the fight remembers players by seat.
    //   seat = part * 3 + arrow        part 0 p, 1 mf, 2 f        arrow 0 left, 1 down and up, 2 right
    public static class StageLayout
    {
        public const int SeatCount = 9;

        //Parts : index 0 is p, the quietest. The order is the order strokes bring them in.
        public static StageRow[] Rows = new StageRow[]
        {
            new StageRow { Name = "WINDS",      Mark = "p",  Strokes = "EVERY STROKE",   Family = Family.Wind },
            new StageRow { Name = "STRINGS",    Mark = "mf", Strokes = "MIDDLE AND BIG", Family = Family.String },
            new StageRow { Name = "PERCUSSION", Mark = "f",  Strokes = "BIG ONLY",       Family = Family.Percussion },
        };

        //Seat Row : which part each seat belongs to
        public static int[] SeatRow = { 0, 0, 0, 1, 1, 1, 2, 2, 2 };

        //Arrows : the three ways a player can answer. Down and up share one arrow, they are the
        //two beats the baton comes back to the middle on (BattleRules.CueSide).
        public static string[] SideNames = { "LEFT", "DOWN / UP", "RIGHT" };

        public static int SeatSide(int seat)
        {
            return seat % 3;
        }

        //Part Of : the part an instrument family plays
        public static int PartOf(Family family)
        {
            for (int r = 0; r < Rows.Length; r++)
                if (Rows[r].Family == family) return r;
            return 1;
        }

        //Home Seat : the one chair this musician always sits in
        public static int HomeSeat(Musician m)
        {
            return PartOf(m.Family) * 3 + m.Cue;
        }

        public static StageRow RowOf(int seat)
        {
            return Rows[SeatRow[seat]];
        }

        //Duel Stand : THE DUEL PAGE, seen from the side. The band stands in a line facing
        //TACET on the right. Each part is one column: p furthest left, f nearest the enemy
        //(the heavy hitters go first). The three players of a part stand at three depths:
        //further away is higher up the screen and a little smaller.
        //All numbers are screen pixels, because the duel page never moves its stage.
        //The boxes are sized for detailed pixel characters with room to swing an instrument,
        //and the f column stays clear of the timing ring in the middle of the screen.
        public static float[] DuelColumnX = { 196f, 316f, 436f };     // p, mf, f
        public static float[] DuelDepthY = { 506f, 530f, 554f };       // far, centre, near : the feet
        public static float[] DuelDepthX = { -20f, 0f, 20f };
        public static float[] DuelDepthScale = { 0.86f, 0.93f, 1f };   // placeholder boxes only, see CharacterArt
        public const int StandW = 100;
        public const int StandH = 170;
        public const float DuelFloorY = 452f;                          // where the stage floor starts

        //Duel Draw Order : the far players first, so nearer ones overlap them
        public static int[] DuelDrawOrder = { 0, 3, 6, 1, 4, 7, 2, 5, 8 };

        //Duel Stand Rect : the box a musician stands in on the duel page, feet on the floor
        public static Rectangle DuelStandRect(int seat)
        {
            int column = SeatRow[seat];
            int depth = seat % 3;
            float scale = DuelDepthScale[depth];
            int w = (int)(StandW * scale);
            int h = (int)(StandH * scale);
            float feetX = DuelColumnX[column] + DuelDepthX[depth];
            float feetY = DuelDepthY[depth];
            return new Rectangle((int)(feetX - w / 2f), (int)(feetY - h), w, h);
        }
    }
}
