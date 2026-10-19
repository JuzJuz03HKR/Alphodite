using System;
using System.Collections.Generic;
using System.IO;
using Tacetno433.Battle;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //DebugShots.TurnSim : DEVELOPER TOOL, the balance check of the turn battle (Alpha 7, step 9).
    //   Tacetno433.exe --shots turnsim --out <folder>      writes turnsim.txt
    //
    //Plays fights with Battle/TurnBattle.cs and no screen. A simulated player is a set of chances
    //for every swipe (PERFECT, GOOD, the rest MISS, and how often a swipe on time is the wrong size)
    //and one plain habit for choosing what to do :
    //   SIGNATURE as soon as it is full, a heal when someone is under half life, the dearest skill
    //   that hits enemies when there are 3 BREATH or more, otherwise ATTACK.
    //Two reports :
    //   FIGHTS  one fight at full life, every enemy kind on every floor
    //   RUNS    three floors in a row with life carried over : N N E N, a rest (40 percent back),
    //           then the boss, after every win the result page's life back. The band is ANNA,
    //           KLARA and OTTO with THE UNHEARING, one more rehearsal each per floor, no motifs.
    //How well real people swipe is a guess (nobody has played the turn battle yet), so read the
    //numbers side by side rather than as the truth. Results move by a few percent between runs.
    public static partial class DebugShots
    {
        private class SimPlayer
        {
            public string Name;
            public float Perfect;       // chance of PERFECT
            public float Good;          // chance of GOOD, the rest is MISS
            public float WrongSize;     // chance a swipe on time is the wrong size
        }

        private static SimPlayer[] simPlayers =
        {
            new SimPlayer { Name = "FLAWLESS", Perfect = 1f,    Good = 0f,    WrongSize = 0f },
            new SimPlayer { Name = "STRONG",   Perfect = 0.6f,  Good = 0.3f,  WrongSize = 0.05f },
            new SimPlayer { Name = "AVERAGE",  Perfect = 0.35f, Good = 0.4f,  WrongSize = 0.15f },
            new SimPlayer { Name = "NEWCOMER", Perfect = 0.15f, Good = 0.4f,  WrongSize = 0.3f },
        };

        private const int SimFights = 200;
        private const int SimRuns = 300;

        private static void TurnSim()
        {
            Random rng = new Random(4330);
            string report = "TACET TURN BATTLE BALANCE  (" + SimFights + " fights per cell, " + SimRuns + " runs per player)\r\n";
            report += "Players : FLAWLESS all PERFECT / STRONG 60 30 / AVERAGE 35 40 / NEWCOMER 15 40 (PERFECT GOOD percent)\r\n\r\n";

            MusicianList.ResetRehearsals();
            List<Musician> band = new List<Musician> { MusicianList.All[0], MusicianList.All[1], MusicianList.All[2] };
            EnemyKind[] kinds = { EnemyKind.Normal, EnemyKind.Elite, EnemyKind.Boss };

            //FIGHTS : win percent / rounds / life lost percent
            report += "FIGHTS at full life : WIN PERCENT  /  ROUNDS  /  LIFE LOST PERCENT\r\n";
            for (int p = 0; p < simPlayers.Length; p++)
            {
                SimPlayer player = simPlayers[p];
                for (int floor = 1; floor <= 3; floor++)
                {
                    string line = "  " + player.Name.PadRight(9) + " FLOOR " + floor + "  ";
                    for (int k = 0; k < kinds.Length; k++)
                    {
                        int wins = 0;
                        int rounds = 0;
                        int lost = 0;
                        int max = 0;
                        for (int f = 0; f < SimFights; f++)
                        {
                            Enemy enemy = SimPick(kinds[k], f);
                            TurnBattle b = SimFight(band, enemy, floor, player, null, rng);
                            if (b.Step == TurnStep.Won) wins++;
                            rounds += b.Round;
                            for (int i = 0; i < b.Party.Count; i++)
                            {
                                lost += b.Party[i].MaxHp - b.Party[i].Hp;
                                max += b.Party[i].MaxHp;
                            }
                        }
                        line += kinds[k].ToString().ToUpper().PadRight(7) + (wins * 100 / SimFights).ToString().PadLeft(3) + "% "
                              + (rounds / (float)SimFights).ToString("0.0").PadLeft(4) + "r " + (lost * 100 / Math.Max(1, max)).ToString().PadLeft(3) + "%    ";
                    }
                    report += line + "\r\n";
                }
            }

            //FIGHTS by enemy on floor 1 for the AVERAGE player
            report += "\r\nEACH ENEMY on its own floor (normal floor 2, elite floor 2, boss floor 3), AVERAGE player : WIN PERCENT / ROUNDS\r\n";
            for (int e = 0; e < EnemyList.All.Length; e++)
            {
                Enemy enemy = EnemyList.All[e];
                int floor = enemy.Kind == EnemyKind.Boss ? 3 : 2;
                int wins = 0;
                int rounds = 0;
                for (int f = 0; f < SimFights; f++)
                {
                    TurnBattle b = SimFight(band, enemy, floor, simPlayers[2], null, rng);
                    if (b.Step == TurnStep.Won) wins++;
                    rounds += b.Round;
                }
                report += "  " + enemy.Name.PadRight(22) + (wins * 100 / SimFights).ToString().PadLeft(3) + "%  " + (rounds / (float)SimFights).ToString("0.0") + "r\r\n";
            }

            //RUNS : three floors with life carried over
            report += "\r\nRUNS (3 floors, life carried over, no motifs) : WIN PERCENT  /  LOST ON FLOOR 1, 2, 3\r\n";
            for (int p = 0; p < simPlayers.Length; p++)
            {
                int wins = 0;
                int[] lostOn = new int[4];
                for (int r = 0; r < SimRuns; r++)
                {
                    int reached = SimRun(band, simPlayers[p], rng);
                    if (reached > 3) wins++;
                    else lostOn[reached]++;
                }
                report += "  " + simPlayers[p].Name.PadRight(9) + (wins * 100 / SimRuns).ToString().PadLeft(3) + "%    "
                        + (lostOn[1] * 100 / SimRuns) + "% / " + (lostOn[2] * 100 / SimRuns) + "% / " + (lostOn[3] * 100 / SimRuns) + "%\r\n";
            }

            MusicianList.ResetRehearsals();
            File.WriteAllText(Path.Combine(outDir, "turnsim.txt"), report);
        }

        //Sim Pick : the enemies of one kind in turn, so every one of them is played the same amount
        private static Enemy SimPick(EnemyKind kind, int index)
        {
            List<Enemy> pool = new List<Enemy>();
            for (int i = 0; i < EnemyList.All.Length; i++)
                if (EnemyList.All[i].Kind == kind) pool.Add(EnemyList.All[i]);
            return pool[index % pool.Count];
        }

        //Sim Run : returns the floor the band fell on, or 4 for a won run
        private static int SimRun(List<Musician> band, SimPlayer player, Random rng)
        {
            MusicianList.ResetRehearsals();
            int[] life = { -1, -1, -1 };
            EnemyKind[] path = { EnemyKind.Normal, EnemyKind.Normal, EnemyKind.Elite, EnemyKind.Normal, EnemyKind.Boss };

            for (int floor = 1; floor <= 3; floor++)
            {
                for (int i = 0; i < path.Length; i++)
                {
                    //Rest Stop : before the boss, 40 percent back (BattleRules.BreatheRecover)
                    if (path[i] == EnemyKind.Boss) SimHeal(band, life, BattleRules.BreatheRecover);

                    Enemy enemy = SimPick(path[i], rng.Next(10));
                    int[] start = new int[band.Count];
                    for (int m = 0; m < band.Count; m++) start[m] = life[m] < 0 ? BattleRules.TurnHp[(int)band[m].Family] : life[m];

                    TurnBattle b = SimFight(band, enemy, floor, player, start, rng);
                    if (b.Step != TurnStep.Won) return floor;

                    for (int m = 0; m < band.Count; m++) life[m] = b.HpAfterBattle(b.Party[m]);
                    SimHeal(band, life, BattleRules.RecoverAfterWin);                 // the result page's life back
                }
                for (int m = 0; m < band.Count; m++) band[m].Rehearsed++;              // a rehearsal each per floor
            }
            return 4;
        }

        private static void SimHeal(List<Musician> band, int[] life, float share)
        {
            for (int m = 0; m < band.Count; m++)
            {
                int max = BattleRules.TurnHp[(int)band[m].Family];
                int now = life[m] < 0 ? max : life[m];
                life[m] = Math.Min(max, now + (int)Math.Round(share * max));
            }
        }

        //Sim Fight : one fight to the end with the player's habit
        private static TurnBattle SimFight(List<Musician> band, Enemy enemy, int floor, SimPlayer player, int[] startHp, Random rng)
        {
            TurnBattle b = new TurnBattle(band, new List<Enemy> { enemy }, floor, rng, startHp);
            b.Kit = TurnKit.Beethoven;

            for (int step = 0; step < 400 && (b.Step == TurnStep.PlayerTurn || b.Step == TurnStep.EnemyTurn); step++)
            {
                if (b.Step == TurnStep.EnemyTurn)
                {
                    Grade[] parries = new Grade[b.Current.IntentNotes.Length];
                    for (int i = 0; i < parries.Length; i++)
                    {
                        NoteResult r = SimSwipe(player, rng);
                        parries[i] = r.Grade == Grade.Perfect && !r.RightSize ? Grade.Good : r.Grade;
                    }
                    b.TakeEnemyAttack(parries);
                    continue;
                }

                //SIGNATURE first, it costs no turn
                if (b.CanSignature)
                {
                    NoteResult[] four = new NoteResult[4];
                    for (int i = 0; i < 4; i++) four[i] = new NoteResult(SimSwipe(player, rng).Grade, true, 1f);
                    b.UseSignature(four);
                    continue;
                }

                Fighter me = b.Current;
                Fighter enemyF = b.Enemies[0];
                Skill heal = null;
                Skill hit = null;
                Skill[] mine = { SkillList.First(me.Musician), SkillList.Second(me.Musician) };
                for (int i = 0; i < mine.Length; i++)
                {
                    if (!b.CanUse(mine[i])) continue;
                    if (mine[i].Heal > 0) heal = mine[i];
                    if (mine[i].HitsEnemy && (hit == null || mine[i].Cost > hit.Cost)) hit = mine[i];
                }

                Fighter hurt = null;
                for (int i = 0; i < b.Party.Count; i++)
                    if (!b.Party[i].Down && b.Party[i].Hp * 2 < b.Party[i].MaxHp && (hurt == null || b.Party[i].Hp < hurt.Hp)) hurt = b.Party[i];

                if (heal != null && hurt != null) b.UseSkill(heal, heal.Target == SkillTarget.Ally ? hurt : me, SimNotes(heal, player, rng));
                else if (hit != null && b.Breath >= 3) b.UseSkill(hit, enemyF, SimNotes(hit, player, rng));
                else
                {
                    NoteResult r = SimSwipe(player, rng);
                    b.Attack(enemyF, r.Grade, r.RightSize);
                }
            }
            return b;
        }

        private static NoteResult[] SimNotes(Skill s, SimPlayer player, Random rng)
        {
            NoteResult[] results = new NoteResult[s.Notes.Length];
            for (int i = 0; i < results.Length; i++)
            {
                NoteResult r = SimSwipe(player, rng);
                if (s.Notes[i].Kind == NoteKind.Spark || s.Notes[i].Kind == NoteKind.Roll) r.RightSize = true;
                if (s.Notes[i].Kind == NoteKind.Hold) r.Hold = r.Grade == Grade.Perfect ? 1f : 0.6f;
                results[i] = r;
            }
            return results;
        }

        private static NoteResult SimSwipe(SimPlayer player, Random rng)
        {
            double roll = rng.NextDouble();
            Grade g = roll < player.Perfect ? Grade.Perfect : (roll < player.Perfect + player.Good ? Grade.Good : Grade.Miss);
            bool rightSize = rng.NextDouble() >= player.WrongSize;
            return new NoteResult(g, rightSize, 1f);
        }
    }
}
