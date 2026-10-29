namespace Tacetno433.Data
{
    //Formation : who is on stage.
    //
    //Round 14 : nobody chooses a seat any more. Each musician plays a PART written on their card
    //(a letter for the strokes that bring them in, an arrow for the way they answer, see
    //StageLayout), and each part belongs to one musician, so everyone on stage always sits in
    //their own HOME SEAT. THE BAND OF THREE (Beta1, 29 Oct) : the whole band is on stage, nobody waits
    //on a bench any more. The old rhythm duel still reads who sits where.
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
        //Returns false when the stage is full.
        public bool AutoSeat(Musician m, int seatsOwned)
        {
            if (OnStage(m)) return true;
            if (SeatedCount >= seatsOwned) return false;
            Seated[StageLayout.HomeSeat(m)] = m;
            return true;
        }

        //Tidy : everyone moves to their home seat, and when there are more players on stage than
        //room for them (a save from before round 14 where anyone could sit anywhere, or one from
        //before the band of three) the last ones step off.
        public void Tidy(int seatsOwned)
        {
            Musician[] before = (Musician[])Seated.Clone();
            for (int s = 0; s < Seated.Length; s++) Seated[s] = null;
            for (int s = 0; s < before.Length; s++)
                if (before[s] != null) AutoSeat(before[s], seatsOwned);
        }
    }
}
