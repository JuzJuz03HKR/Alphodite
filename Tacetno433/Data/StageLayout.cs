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
    public static class StageLayout
    {
        public const int SeatCount = 9;

        //Rows : index 0 is the back, 2 is the front
        public static StageRow[] Rows = new StageRow[]
        {
            new StageRow { Name = "BACK",   Title = "BACK ROW",   Effect = "SUPPORT  -  POWER x0.8  /  STAMINA COST x0.7",
                           PowerScale = 0.8f, CostScale = 0.7f,  Size = 0.82f },
            new StageRow { Name = "MIDDLE", Title = "MIDDLE ROW", Effect = "STEADY  -  NO CHANGE",
                           PowerScale = 1.0f, CostScale = 1.0f,  Size = 0.91f },
            new StageRow { Name = "FRONT",  Title = "FRONT ROW",  Effect = "LEAD  -  POWER x1.3  /  STAMINA COST x1.15",
                           PowerScale = 1.3f, CostScale = 1.15f, Size = 1.0f },
        };

        //Seat Row : which row each seat belongs to
        public static int[] SeatRow = { 0, 0, 0, 1, 1, 1, 2, 2, 2 };

        //Seat Position : centre of each seat, from 0 to 1 across and down the stage box
        public static Vector2[] SeatPos =
        {
            new Vector2(0.26f, 0.20f), new Vector2(0.50f, 0.17f), new Vector2(0.74f, 0.20f),   // back
            new Vector2(0.18f, 0.48f), new Vector2(0.50f, 0.45f), new Vector2(0.82f, 0.48f),   // middle
            new Vector2(0.11f, 0.76f), new Vector2(0.50f, 0.73f), new Vector2(0.89f, 0.76f),   // front
        };

        //Unlock Order : seat numbers in the order they open up.
        //A run with 3 seats uses the first three in this list: front, middle and back centre.
        public static int[] UnlockOrder = { 7, 4, 1, 6, 8, 3, 5, 0, 2 };

        //Seat Size : the capsule size at full scale, before the row shrinks it
        public const int CapsuleW = 58;
        public const int CapsuleH = 150;

        public static StageRow RowOf(int seat)
        {
            return Rows[SeatRow[seat]];
        }

        //Duel Position : the same nine seats on the duel page. The rows are shifted sideways
        //so the band climbs up and to the right, away from the conductor's hand in the corner,
        //and no musician hides behind the one in front.
        public static Vector2[] DuelPos =
        {
            new Vector2(0.44f, 0.14f), new Vector2(0.64f, 0.12f), new Vector2(0.84f, 0.14f),   // back
            new Vector2(0.30f, 0.42f), new Vector2(0.50f, 0.40f), new Vector2(0.70f, 0.42f),   // middle
            new Vector2(0.16f, 0.72f), new Vector2(0.36f, 0.70f), new Vector2(0.56f, 0.72f),   // front
        };

        //Seat Rect : where a seat's capsule sits inside a stage box
        public static Rectangle SeatRect(int seat, Rectangle stageBox, float scale)
        {
            return PlaceSeat(seat, SeatPos[seat], stageBox, scale);
        }

        //Duel Seat Rect : the same, using the duel positions
        public static Rectangle DuelSeatRect(int seat, Rectangle stageBox, float scale)
        {
            return PlaceSeat(seat, DuelPos[seat], stageBox, scale);
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
