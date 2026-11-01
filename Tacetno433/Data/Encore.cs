using System;

namespace Tacetno433.Data
{
    //Encore : ENCORE (Beta1 1 Nov, docs/BETA1_DIRECTION.md 27, the user's pick in place of MAESTRO MODE).
    //When a run is won the audience calls the band back for an encore. Every conductor has a ladder of their own :
    //a run won with a conductor opens the next level for that conductor, up to BattleRules.EncoreTop. Each level adds
    //ONE rule on top of the ones under it (BattleRules ENCORE), and the last boss of the run is the conductor's own
    //hollow, so each encore is a harder fight with your own demon. A first run has nothing to pick : the conductor
    //pages show the ENCORE only once that conductor has won a run.
    //Kept in archive.txt next to THE ARCHIVE (SaveFile.SaveArchive). KEEP THE ORDER : one number for each conductor of
    //ConductorList.All, in its order.
    public static class Encore
    {
        //Won : for each conductor of ConductorList.All, the highest level a run was won at, -1 when never won
        public static int[] Won = Fresh();

        //Pick : the level the conductor pages have set for each conductor's next run. Not saved : it starts at the
        //highest open level when the game starts (PickAll), and moves up when a win opens a new level.
        public static int[] Pick = new int[ConductorList.All.Length];

        //Rule Lines : what each level adds, in plain words. The conductor pages show the picked one (0 adds nothing).
        public static readonly string[] Lines =
        {
            "No extra rules.",
            "+ Enemies hit harder.",
            "+ A faster ring and tighter timing.",
            "+ One more enemy in normal fights.",
            "+ Resting heals less.",
            "+ Bosses reach " + Words.PhaseTwo + " sooner.",
        };

        //Labels : "ENCORE 1" to "ENCORE 5" ("" for 0) and the level numbers, made once here so nothing is built while drawing
        public static readonly string[] Labels = MakeLabels();
        public static readonly string[] Digits = { "0", "1", "2", "3", "4", "5" };

        private static int[] Fresh()
        {
            int[] won = new int[ConductorList.All.Length];
            for (int i = 0; i < won.Length; i++) won[i] = -1;
            return won;
        }

        private static string[] MakeLabels()
        {
            string[] labels = new string[BattleRules.EncoreTop + 1];
            labels[0] = "";
            for (int i = 1; i < labels.Length; i++) labels[i] = Words.Encore + " " + i;
            return labels;
        }

        //Index Of : where a conductor stands in ConductorList.All (-1 for none)
        public static int IndexOf(Conductor c)
        {
            return c == null ? -1 : Array.IndexOf(ConductorList.All, c);
        }

        //Open : the highest level this conductor can play now, 0 until a run is won with them
        public static int Open(Conductor c)
        {
            int i = IndexOf(c);
            if (i < 0 || Won[i] < 0) return 0;
            return Math.Min(BattleRules.EncoreTop, Won[i] + 1);
        }

        //Won Levels : how many levels of ENCORE this conductor has won (the gallery's filled diamonds)
        public static int WonLevels(Conductor c)
        {
            int i = IndexOf(c);
            return i < 0 ? 0 : Math.Max(0, Won[i]);
        }

        //Picked : the level the next run with this conductor starts at, never above what is open
        public static int Picked(Conductor c)
        {
            int i = IndexOf(c);
            if (i < 0) return 0;
            return Math.Max(0, Math.Min(Open(c), Pick[i]));
        }

        //Choose : the conductor pages set the level (a click on its number)
        public static void Choose(Conductor c, int level)
        {
            int i = IndexOf(c);
            if (i < 0) return;
            Pick[i] = Math.Max(0, Math.Min(Open(c), level));
        }

        //Step Up : the key goes one level up, and from the highest open level back to 0
        public static void StepUp(Conductor c)
        {
            int open = Open(c);
            if (open <= 0) return;
            int now = Picked(c);
            Choose(c, now >= open ? 0 : now + 1);
        }

        //Pick All : every conductor's pick goes to their highest open level (the game starts, archive.txt was read)
        public static void PickAll()
        {
            for (int i = 0; i < Pick.Length && i < Won.Length; i++)
                Pick[i] = Won[i] < 0 ? 0 : Math.Min(BattleRules.EncoreTop, Won[i] + 1);
        }

        //Win : a run was won at its level. Returns the level this opened, 0 when nothing new opened (a level played
        //again, or the top one). A new level is picked for that conductor's next run.
        public static int Win(RunState run)
        {
            int i = IndexOf(run.Conductor);
            if (i < 0) return 0;
            int before = Open(run.Conductor);
            Won[i] = Math.Max(Won[i], Math.Min(BattleRules.EncoreTop, run.Encore));
            int after = Open(run.Conductor);
            if (after <= before) return 0;
            Pick[i] = after;
            return after;
        }
    }
}
