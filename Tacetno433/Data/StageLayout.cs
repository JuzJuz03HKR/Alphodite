using Microsoft.Xna.Framework;

namespace Tacetno433.Data
{
    //StageRow : one SECTION of the orchestra, the players of one instrument family
    public class StageRow
    {
        public string Name = "";          // "STRINGS"
        public string Short = "";         // "STR", where there is no room for the whole word
        public Family Family;             // the instruments that sit here
    }

    //StageLayout : THE PLACE TO EDIT THE STAGE.
    //
    //Round 15 : the stage is laid out like a real orchestra, seen from the conductor's podium.
    //   SECTIONS  strings at the front, winds in the middle, percussion at the back
    //   SIDES     left, centre and right. The baton points at a side on every beat of the 4/4
    //             shape (down and up to the centre, the second beat left, the third right), and
    //             the players sitting there hit harder (BattleRules CUE). A musician's side is
    //             where their instrument sits in a real orchestra, the violins on the left and
    //             the cellos on the right (Musician.Cue, drawn as their arrow).
    //Everybody on stage plays every note (round 15), so the stage decides who gets the CUE,
    //not who plays. Every section and side pair belongs to exactly one musician of the nine,
    //so each musician has one chair of their own, their HOME SEAT, like a real orchestra where
    //the second horn always sits in the same place. Nothing to arrange before a fight.
    //   seat = section * 3 + side        section 0 strings, 1 winds, 2 percussion        side 0 left, 1 centre, 2 right
    //(Round 14 : the rows were letters, winds p, strings mf, percussion f, and a small stroke only
    //brought the p players in. Round 12 to 13 : the player moved everybody around by hand.)
    public static class StageLayout
    {
        public const int SeatCount = 9;

        //Sections : index 0 is the front, nearest the conductor
        public static StageRow[] Rows = new StageRow[]
        {
            new StageRow { Name = "STRINGS",    Short = "STR", Family = Family.String },
            new StageRow { Name = "WINDS",      Short = "WND", Family = Family.Wind },
            new StageRow { Name = "PERCUSSION", Short = "PRC", Family = Family.Percussion },
        };

        //Seat Row : which section each seat belongs to
        public static int[] SeatRow = { 0, 0, 0, 1, 1, 1, 2, 2, 2 };

        //Sides : the three ways a player can answer. Down and up share the centre, they are the
        //two beats the baton comes back to the middle on (BattleRules.CueSide).
        public static string[] SideNames = { "LEFT", "CENTRE", "RIGHT" };
        public static string[] SideBeats = { "BEATS 2 6", "BEATS 1 4 5 8", "BEATS 3 7" };

        public static int SeatSide(int seat)
        {
            return seat % 3;
        }

        //Section Of : the section an instrument family sits in
        public static int SectionOf(Family family)
        {
            for (int r = 0; r < Rows.Length; r++)
                if (Rows[r].Family == family) return r;
            return 0;
        }

        //Home Seat : the one chair this musician always sits in
        public static int HomeSeat(Musician m)
        {
            return SectionOf(m.Family) * 3 + m.Cue;
        }

        public static StageRow RowOf(int seat)
        {
            return Rows[SeatRow[seat]];
        }

        //Duel Stand : THE DUEL PAGE, seen from the side of the stage and a little above it (round 15,
        //the player asked to see where everybody stands, like the parties of Slay the Spire or
        //Limbus Company). The band faces TACET on the right the way an orchestra faces its
        //conductor : the strings nearest, then the winds, the percussion at the back on the left.
        //Each section is a column and its three sides are three depths : further away is higher up
        //the screen, smaller, and set back to the left, so the nine places make a leaning grid on
        //the floor with a little room between everybody (see DuelScreen.DrawFloorSpots), with the
        //section's name on the floor in front of it.
        //All numbers are screen pixels, because the duel page never moves its stage.
        //The boxes are sized for detailed pixel characters with room to swing an instrument,
        //and the f column stays clear of the timing ring in the middle of the screen.
        public static float[] DuelColumnX = { 410f, 270f, 130f };     // strings, winds, percussion (round 15, was the other way round)
        public static float[] DuelDepthY = { 476f, 526f, 576f };       // far, centre, near : the feet (left, centre, right side)
        public static float[] DuelDepthX = { -45f, 0f, 45f };
        public static float[] DuelDepthScale = { 0.78f, 0.89f, 1f };   // placeholder boxes only, see CharacterArt
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
