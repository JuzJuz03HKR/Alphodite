using System;
using System.Collections.Generic;
using System.IO;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //DebugShots.Audit : DEVELOPER TOOL, the balance audit (round 10).
    //   Tacetno433.exe --shots audit --out <folder>      writes audit.txt
    //
    //The same whole runs as the simulate report (PlayRun in DebugShots.cs), but split up so an
    //unfair piece stands out :
    //   BASELINE    THE APPRENTICE, how often each enemy on each floor ends a run, and the first era
    //   CONDUCTORS  every conductor with the same habits
    //   MOTIFS      one motif owned from the start and no others, against no motif at all
    //The sim hands in grades directly, lets the SIGNATURE loose as soon as it can (round 13, it
    //never did before), never buys in shops or plays events,
    //so motifs that change timing windows or shards (STEADY PULSE, PATRON'S PURSE, ENCORE) show
    //little here. Results swing by about 2 to 3 percent between runs.
    public static partial class DebugShots
    {
        private static string[] auditHabit = { "GOOD", "SKILLED", "AVERAGE", "STRONG" };   // PlayFight habits

        private static void Audit(TacetGame game)
        {
            Random random = new Random(11);
            string r = "TACET BALANCE AUDIT  (whole runs, motifs taken unless said)\r\n";

            //Baseline : where runs end, enemy by enemy
            for (int h = 3; h >= 2; h--)
            {
                RunRecord rec = new RunRecord();
                for (int n = 0; n < 1500; n++) PlayRun(game, ConductorList.All[0], h, null, true, random, rec);

                r += "\r\nBASELINE  THE APPRENTICE  " + auditHabit[h] + "   RUN WON " + Percent(rec.Won, rec.Runs)
                   + "   LOST ON FLOOR 1/2/3  " + rec.LostOnFloor[1] + " / " + rec.LostOnFloor[2] + " / " + rec.LostOnFloor[3]
                   + "   OUT OF BREATH " + rec.OutOfBreath + "   BEATS PER FIGHT " + (rec.Beats / (float)rec.Fights).ToString("0.0") + "\r\n";
                r += "   FIRST ERA   ";
                for (int e = 0; e < EraList.All.Length && e < rec.EraRuns.Length; e++)
                    r += EraList.All[e].Name + " " + Percent(rec.EraWon[e], rec.EraRuns[e]) + "   ";
                r += "\r\n   ENEMY (FLOOR)                  FIGHTS  LOSSES  LOSS RATE\r\n";

                List<string> names = new List<string>(rec.Enemies.Keys);
                names.Sort();
                foreach (string name in names)
                {
                    int[] v = rec.Enemies[name];
                    r += "   " + name.PadRight(30) + v[0].ToString().PadLeft(6) + v[1].ToString().PadLeft(8) + "   " + Percent(v[1], v[0]) + "\r\n";
                }
            }

            //Conductors : the same habits for everyone
            r += "\r\nCONDUCTORS  (800 runs per line)\r\n";
            for (int c = 0; c < ConductorList.All.Length; c++)
            {
                r += "   " + ConductorList.All[c].Name.PadRight(20);
                for (int h = 3; h >= 2; h--)
                {
                    RunRecord rec = new RunRecord();
                    for (int n = 0; n < 800; n++) PlayRun(game, ConductorList.All[c], h, null, true, random, rec);
                    r += auditHabit[h] + " " + Percent(rec.Won, rec.Runs).PadLeft(6)
                       + " (SIG " + (rec.Signatures / (float)Math.Max(1, rec.Fights)).ToString("0.0") + ")   ";
                }
                r += "\r\n";
            }

            //Motifs : one motif from the start and no others, STRONG habit
            r += "\r\nMOTIFS  (THE APPRENTICE, STRONG, only this motif from the start, 1000 runs per line)\r\n";
            RunRecord none = new RunRecord();
            for (int n = 0; n < 2000; n++) PlayRun(game, ConductorList.All[0], 3, null, false, random, none);
            float basePercent = none.Won * 100f / none.Runs;
            r += "   " + "NO MOTIF".PadRight(20) + Percent(none.Won, none.Runs).PadLeft(6) + "\r\n";

            for (int m = 0; m < MotifList.All.Length; m++)
            {
                RunRecord rec = new RunRecord();
                for (int n = 0; n < 1000; n++) PlayRun(game, ConductorList.All[0], 3, MotifList.All[m], false, random, rec);
                float gain = rec.Won * 100f / rec.Runs - basePercent;
                r += "   " + MotifList.All[m].Name.PadRight(20) + Percent(rec.Won, rec.Runs).PadLeft(6)
                   + "   " + (gain >= 0f ? "+" : "") + gain.ToString("0.0") + "   RARITY " + MotifList.All[m].Rarity + "\r\n";
            }

            File.WriteAllText(Path.Combine(outDir, "audit.txt"), r);
        }

        private static string Percent(int part, int whole)
        {
            return whole > 0 ? (part * 100f / whole).ToString("0.0") + "%" : "-";
        }
    }
}
