namespace Tacetno433.Data
{
    //Formation : who is on stage and who waits on the bench.
    //
    //Round 14 : nobody chooses a seat any more. Each musician plays a PART written on their card
    //(a letter for the strokes that bring them in, an arrow for the way they answer, see
    //StageLayout), and each part belongs to one musician, so everyone on stage always sits in
    //their own HOME SEAT. The only choice left is who plays: the stage has room for RunState.Seats
    //players, the rest wait on the bench. The band page (BandScreen) swaps them.
    public class Formation
    {
        public Musician[] Seated = new Musician[StageLayout.SeatCount];

        //Seat Of : which seat a musician is in, or -1 when they are on the bench
        public int SeatOf(Musician m)
        {
            for (int s = 0; s < Seated.Length; s++)
                if (Seated[s] == m) return s;
            return -1;
        }

        public bool OnStage(Musician m)
        {
            return SeatOf(m) >= 0;
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

        //Auto Seat : a musician steps onto the stage, into their home seat, while there is room.
        //Returns false when the stage is full (they wait on the bench).
        public bool AutoSeat(Musician m, int seatsOwned)
        {
            if (OnStage(m)) return true;
            if (SeatedCount >= seatsOwned) return false;
            Seated[StageLayout.HomeSeat(m)] = m;
            return true;
        }

        //Bench : off the stage
        public void Bench(Musician m)
        {
            int seat = SeatOf(m);
            if (seat >= 0) Seated[seat] = null;
        }

        //Swap : one on the bench takes the place of one on stage
        public void Swap(Musician fromBench, Musician fromStage)
        {
            Bench(fromStage);
            Seated[StageLayout.HomeSeat(fromBench)] = fromBench;
        }

        //Tidy : everyone moves to their home seat, and when there are more players on stage than
        //room for them (a smaller stage, or a save from before round 14 where anyone could sit
        //anywhere) the last ones go to the bench.
        public void Tidy(int seatsOwned)
        {
            Musician[] before = (Musician[])Seated.Clone();
            for (int s = 0; s < Seated.Length; s++) Seated[s] = null;
            for (int s = 0; s < before.Length; s++)
                if (before[s] != null) AutoSeat(before[s], seatsOwned);
        }
    }
}
