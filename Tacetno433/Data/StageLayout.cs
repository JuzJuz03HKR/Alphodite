using Microsoft.Xna.Framework;

namespace Tacetno433.Data
{
    //StageRow : one tier of the orchestra
    public class StageRow
    {
        public string Name = "";
        public string Title = "";         // the name as a heading, "FRONT ROW"
        public string Effect = "";        // one short line shown next to the row
        public float PowerScale = 1f;     // musicians sitting here hit this much harder
        public float CostScale = 1f;      // and pay this much stamina
        public float Size = 1f;           // drawn smaller further back, for depth
    }

    //StageLayout : THE PLACE TO EDIT THE STAGE.
    //
    //There are nine seats in three rows, like a small orchestra seen from the side.
    //Seat positions are written from 0 to 1 inside whatever box a page draws the stage in,
    //so the formation page and the duel page can show the same stage at different sizes.
    //
    //Where a musician sits is how they play (round 12, there is no beat plan any more):
    //   ROW   which strokes bring them in. A small stroke brings the back row, a middle one the
    //         middle row too, a big one everybody (BattleRules DYNAMICS).
    //   SIDE  the column, left, centre or right. On every beat the baton points at one side,
    //         and whoever sits there hits harder (BattleRules CUE).
    public static class StageLayout
    {
        public const int SeatCount = 9;

        //Rows : index 0 is the back, 2 is the front
        public static StageRow[] Rows = new StageRow[]
        {
            new StageRow { Name = "BACK",   Title = "BACK ROW",   Effect = "EVERY STROKE  -  POWER x0.8  /  STAMINA COST x0.7",
                           PowerScale = 0.8f, CostScale = 0.7f,  Size = 0.82f },
            new StageRow { Name = "MIDDLE", Title = "MIDDLE ROW", Effect = "MIDDLE AND BIG STROKES  -  NO CHANGE",
                           PowerScale = 1.0f, CostScale = 1.0f,  Size = 0.91f },
            new StageRow { Name = "FRONT",  Title = "FRONT ROW",  Effect = "BIG STROKES  -  POWER x1.3  /  STAMINA COST x1.15",
                           PowerScale = 1.3f, CostScale = 1.15f, Size = 1.0f },
        };

        //Seat Row : which row each seat belongs to
        public static int[] SeatRow = { 0, 0, 0, 1, 1, 1, 2, 2, 2 };

        //Sides : the stage's three columns as the conductor sees them, 0 left, 1 centre, 2 right.
        //The baton's CUE points at one of them on every beat (BattleRules.CueSide).
        public static string[] SideNames = { "LEFT", "CENTRE", "RIGHT" };

        public static int SeatSide(int seat)
        {
            return seat % 3;
        }

        //Seat Position : centre of each seat, from 0 to 1 across and down the stage box
        public static Vector2[] SeatPos =
        {
            new Vector2(0.26f, 0.20f), new Vector2(0.50f, 0.17f), new Vector2(0.74f, 0.20f),   // back
            new Vector2(0.18f, 0.48f), new Vector2(0.50f, 0.45f), new Vector2(0.82f, 0.48f),   // middle
            new Vector2(0.11f, 0.76f), new Vector2(0.50f, 0.73f), new Vector2(0.89f, 0.76f),   // front
        };

        //Unlock Order : seat numbers in the order they open up.
        //A run with 3 seats uses the first three in this list, a diagonal: front left, middle
        //centre and back right. So the first band already has a row for every stroke size and a
        //player on every side the baton points at (round 12, was the centre column 7, 4, 1).
        public static int[] UnlockOrder = { 6, 4, 2, 7, 1, 3, 5, 8, 0 };

        //Seat Size : the capsule size at full scale, before the row shrinks it
        public const int CapsuleW = 58;
        public const int CapsuleH = 150;

        public static StageRow RowOf(int seat)
        {
            return Rows[SeatRow[seat]];
        }

        //Duel Stand : THE DUEL PAGE, seen from the side. The band stands in a line facing
        //TACET on the right. Each orchestra row is one column: the back row furthest left,
        //the front row nearest the enemy. The three seats of a row stand at three depths:
        //further away is higher up the screen and a little smaller.
        //All numbers are screen pixels, because the duel page never moves its stage.
        //The boxes are sized for detailed pixel characters with room to swing an instrument,
        //and the front row stays clear of the timing ring in the middle of the screen.
        public static float[] DuelColumnX = { 196f, 316f, 436f };     // back, middle, front row
        public static float[] DuelDepthY = { 506f, 530f, 554f };       // far, centre, near : the feet
        public static float[] DuelDepthX = { -20f, 0f, 20f };
        public static float[] DuelDepthScale = { 0.86f, 0.93f, 1f };   // placeholder boxes only, see CharacterArt
        public const int StandW = 100;
        public const int StandH = 170;
        public const float DuelFloorY = 452f;                          // where the stage floor starts

        //Duel Draw Order : the far players first, so nearer ones overlap them
        public static int[] DuelDrawOrder = { 0, 3, 6, 1, 4, 7, 2, 5, 8 };

        //Seat Rect : where a seat's capsule sits inside a stage box
        public static Rectangle SeatRect(int seat, Rectangle stageBox, float scale)
        {
            return PlaceSeat(seat, SeatPos[seat], stageBox, scale);
        }

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

        private static Rectangle PlaceSeat(int seat, Vector2 pos, Rectangle stageBox, float scale)
        {
            float size = RowOf(seat).Size * scale;
            int w = (int)(CapsuleW * size);
            int h = (int)(CapsuleH * size);
            int cx = stageBox.X + (int)(pos.X * stageBox.Width);
            int cy = stageBox.Y + (int)(pos.Y * stageBox.Height);
            return new Rectangle(cx - w / 2, cy - h / 2, w, h);
        }
    }
}
