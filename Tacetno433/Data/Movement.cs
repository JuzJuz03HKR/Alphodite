using System;

namespace Tacetno433.Data
{
    //Movement (Beta1, 29 Oct) : the floors of a run are the movements of one piece, I PRELUDE, II NOCTURNE and III CODA
    //(the card that opens each floor, Screens/ChapterScreen.cs). Each movement has its own battle backdrop
    //(Content/Art/Battle/Stages/<movement>_far.png and the rest, Screens/BattleScreen.Stage.cs) and its own two bosses
    //(Enemy.Movement), one of them picked at random.
    //NO MORE ERAS : until 29 Oct the band travelled between eras (CLASSICAL, SIAM, ROMANTIC), picked at the opening of every
    //floor and at a CROSSING on the map, and met that era's musicians. The user took the eras out (docs/BETA1_DIRECTION.md 2
    //and 3.3) : the run meets the five legends (MusicianList.Shown) wherever it is, and the bosses of an era became the
    //bosses of the movement with the same number (CLASSICAL's are PRELUDE's, SIAM's NOCTURNE's, ROMANTIC's CODA's).
    public static class MovementList
    {
        //Names : one per floor, THE PLACE TO EDIT them (the card, the route's corner, the backdrop's file names)
        public static readonly string[] Names = { "PRELUDE", "NOCTURNE", "CODA" };

        //Of : the movement of a floor (1 is the first), the last one for any floor past it
        public static int Of(int floor)
        {
            return Math.Max(0, Math.Min(Names.Length - 1, floor - 1));
        }
    }
}
