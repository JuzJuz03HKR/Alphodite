namespace Tacetno433.Data
{
    //Formation : who sits in which seat.
    //
    //There is no beat plan any more (round 12). Where a musician sits is how they play:
    //   the ROW  decides which strokes bring them in, small the back row, middle the middle row
    //            too, big everybody (see BattleState.Joins)
    //   the SIDE decides on which beats the baton points at them (CUE, BattleRules.CueSide)
    public class Formation
    {
        public Musician[] Seated = new Musician[StageLayout.SeatCount];

        //Seat Unlocked : a seat is open if it is among the first seatsOwned in the unlock order
        public bool IsUnlocked(int seat, int seatsOwned)
        {
            for (int i = 0; i < seatsOwned && i < StageLayout.UnlockOrder.Length; i++)
                if (StageLayout.UnlockOrder[i] == seat) return true;
            return false;
        }

        //Seat Of : which seat a musician is in, or -1 when they are on the bench
        public int SeatOf(Musician m)
        {
            for (int s = 0; s < Seated.Length; s++)
                if (Seated[s] == m) return s;
            return -1;
        }

        public int SeatedCount
        {
            get
            {
                int count = 0;
                for (int s = 0; s < Seated.Length; s++)
                    if (Seated[s] != null) count++;
                return count;
            }
        }

        //Place : put a musician into a seat. If they were already sitting somewhere,
        //whoever was in the target seat moves over to their old seat.
        public void Place(Musician m, int seat)
        {
            int from = SeatOf(m);
            if (from == seat) return;

            Musician previous = Seated[seat];
            Seated[seat] = m;
            if (from >= 0) Seated[from] = previous;       // Seat Swap
        }

        //Remove : back to the bench
        public void Remove(int seat)
        {
            Seated[seat] = null;
        }

        //Auto Seat : first open empty seat, in unlock order. Returns false if the stage is full.
        public bool AutoSeat(Musician m, int seatsOwned)
        {
            for (int i = 0; i < seatsOwned && i < StageLayout.UnlockOrder.Length; i++)
            {
                int seat = StageLayout.UnlockOrder[i];
                if (Seated[seat] == null)
                {
                    Seated[seat] = m;
                    return true;
                }
            }
            return false;
        }

        //Tidy : anyone sitting in a seat that is not open moves to the first open empty seat, or
        //to the bench when the stage is full. A run saved before round 12 sat its first players
        //down the centre, and those seats open later now (StageLayout.UnlockOrder).
        public void Tidy(int seatsOwned)
        {
            for (int s = 0; s < Seated.Length; s++)
            {
                if (Seated[s] == null || IsUnlocked(s, seatsOwned)) continue;
                Musician m = Seated[s];
                Seated[s] = null;
                AutoSeat(m, seatsOwned);
            }
        }
    }
}
