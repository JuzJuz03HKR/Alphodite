namespace Tacetno433.Data
{
    //Formation : who sits in which seat, and which beats each seat plays.
    //
    //The beat plan belongs to the SEAT:
    //   moving a musician to another seat carries their beats with them
    //   swapping someone in from the bench hands them the beats of that seat
    //   sending someone to the bench clears that seat's beats
    public class Formation
    {
        public Musician[] Seated = new Musician[StageLayout.SeatCount];
        public bool[,] Plan = new bool[StageLayout.SeatCount, BattleRules.BeatsPerRound];

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

            if (from >= 0)
            {
                //Seat Swap : the two seats trade musicians and trade beat plans
                Seated[from] = previous;
                SwapRows(from, seat);
                if (previous == null) ClearRow(from);
            }
        }

        //Remove : back to the bench
        public void Remove(int seat)
        {
            Seated[seat] = null;
            ClearRow(seat);
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

        //Beat Queries
        public bool Plays(int seat, int beat)
        {
            return Seated[seat] != null && Plan[seat, beat];
        }

        public int PlayersOnBeat(int beat)
        {
            int count = 0;
            for (int s = 0; s < Seated.Length; s++)
                if (Plays(s, beat)) count++;
            return count;
        }

        public void Toggle(int seat, int beat)
        {
            if (Seated[seat] == null) return;
            Plan[seat, beat] = !Plan[seat, beat];
        }

        public void ClearRow(int seat)
        {
            for (int b = 0; b < BattleRules.BeatsPerRound; b++)
                Plan[seat, b] = false;
        }

        public void ClearAll()
        {
            for (int s = 0; s < Seated.Length; s++)
                ClearRow(s);
        }

        private void SwapRows(int a, int b)
        {
            for (int beat = 0; beat < BattleRules.BeatsPerRound; beat++)
            {
                bool keep = Plan[a, beat];
                Plan[a, beat] = Plan[b, beat];
                Plan[b, beat] = keep;
            }
        }
    }
}
