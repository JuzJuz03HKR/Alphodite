using System;

namespace Tacetno433.Data
{
    //ArchiveUpgrade : one thing the ARCHIVE sells for ECHOES. Once bought it stays for every run after.
    public class ArchiveUpgrade
    {
        public string Name = "";
        public string Text = "";               // one short line, what one level does
        public int[] Prices = new int[0];      // one price per level, so the length is the top level
        public int Level;                      // levels bought so far (saved in archive.txt)

        public bool Maxed
        {
            get { return Level >= Prices.Length; }
        }

        public int NextPrice
        {
            get { return Maxed ? 0 : Prices[Level]; }
        }
    }

    //Archive : AFTER-RUN UPGRADES (Alpha 7, step 7 of docs/ALPHA7_DESIGN.md).
    //Every run, won or lost, leaves ECHOES behind : a few for each fight won, more for each floor
    //cleared and a bonus for finishing. They are spent on the title page's ARCHIVE, between runs,
    //and what is bought stays for good. Each level is small on purpose, so a run is still won by
    //playing well (the user asked for "upgrades and shopping after a run", 18 Oct).
    //KEEP THE ORDER : the save file keeps the levels in this order.
    public static class Archive
    {
        public const int Life = 0;          // more life for every musician
        public const int Breath = 1;        // more BREATH at the start of every fight
        public const int Signature = 2;     // the SIGNATURE gauge starts part full
        public const int Savings = 3;       // shards at the start of a run
        public const int Keepsake = 4;      // a common motif at the start of a run
        public const int FirstChair = 5;    // FIRST CHAIR (29 Oct) : the first musician picked starts with a better weapon. This place held FULL STAGE
                                            // (one more musician on the field) until the band of three, a level bought for it carries over

        public static ArchiveUpgrade[] All = new ArchiveUpgrade[]
        {
            new ArchiveUpgrade { Name = "STRONGER BAND", Text = "Every musician has 2 more life.",            Prices = new int[] { 30, 60, 90 } },
            new ArchiveUpgrade { Name = "DEEP BREATH",   Text = "Every fight starts with 1 more BREATH.",     Prices = new int[] { 40, 80 } },
            new ArchiveUpgrade { Name = "WARM UP",       Text = "The SIGNATURE starts 2 diamonds fuller.",    Prices = new int[] { 40, 80 } },
            new ArchiveUpgrade { Name = "SAVINGS",       Text = "Every run starts with 20 more shards.",      Prices = new int[] { 25, 50, 75 } },
            new ArchiveUpgrade { Name = "KEEPSAKE",      Text = "Every run starts with a gift for the band.",      Prices = new int[] { 100 } },
            new ArchiveUpgrade { Name = "FIRST CHAIR",   Text = "The first musician you pick starts with a tier I weapon.", Prices = new int[] { 120 } },
        };

        public static int Echoes;           // ECHOES to spend (saved in archive.txt)

        public static int Level(int upgrade)
        {
            return All[upgrade].Level;
        }

        //Buy : false when it is at the top level already or there are not enough echoes
        public static bool Buy(int upgrade)
        {
            ArchiveUpgrade u = All[upgrade];
            if (u.Maxed || Echoes < u.NextPrice) return false;
            Echoes -= u.NextPrice;
            u.Level++;
            return true;
        }

        //Pay : give a finished run's ECHOES once and save them at once. Called the moment a fight is lost (so
        //leaving from the result page keeps them) and again by the curtain call, which then only reads them.
        public static int Pay(RunState run, bool won)
        {
            if (run.EchoesPaid) return run.EchoesEarned;
            run.EchoesPaid = true;
            run.EchoesEarned = EchoesFor(run, won);
            Echoes += run.EchoesEarned;
            SaveFile.SaveArchive();
            return run.EchoesEarned;
        }

        //Echoes For : what a finished run leaves behind. MAESTRO pays more.
        public static int EchoesFor(RunState run, bool won)
        {
            int floors = won ? BattleRules.FloorsPerRun : Math.Max(0, run.Floor - 1);
            int echoes = run.BattlesWon * BattleRules.EchoesPerWin + floors * BattleRules.EchoesPerFloor;
            if (won) echoes += BattleRules.EchoesRunWon;
            if (run.Maestro) echoes = (int)Math.Round(echoes * BattleRules.EchoesMaestro);     // MAESTRO
            return echoes;
        }
    }
}
