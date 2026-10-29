using System;
using System.Collections.Generic;
using System.IO;
using Tacetno433.Battle;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //DebugShots.TurnSim : DEVELOPER TOOL, the balance check of the turn battle (Alpha 7, step 9, ONE BAR since step 12).
    //   Tacetno433.exe --shots turnsim --out <folder>      writes turnsim.txt
    //
    //Plays fights with Battle/TurnBattle.cs and no screen. A simulated player swipes with a timing
    //error (a bell curve this many seconds wide), now and then the wrong way or the wrong size, and
    //TurnBattle.Judge grades it like the battle page does, so every wider window (STEADY HAND,
    //COUNTS ALOUD, FASHIONABLY LATE, STEADY PULSE, CAPRICE) works as in the game.
    //ONE BAR : an ATTACK needs no swipe, so only skills, the SIGNATURE and the parries use the timing error.
    //One plain habit writes each bar (SimPlan) :
    //   SIGNATURE as soon as it is full. Then for each musician : a heal when someone is under half life,
    //   the dearest skill that hits enemies when 3 BREATH or more are free, otherwise ATTACK the enemy with the least life.
    //   (Until 29 Oct it also CALLED the BENCH in.)
    //Reports :
    //   FIGHTS      one fight at full life with the PACKS of the run (BattleRules ONE BAR), the band of three
    //   RANKS       the RANK of the won fights above (29 Oct, BattleState.RankScore), S / A / B / C percent and the shards' scale
    //   RUNS        three floors in a row with life carried over : N N E N N, a rest (40 percent back), then the boss,
    //               after every win the result page's life back. THE BAND OF THREE (29 Oct) : 3 of the 5 legends picked at
    //               random for each run, every weapon one tier up after each floor (WEAPON TIERS, as a rehearsal each per
    //               floor before), THE UNHEARING, no motifs, shops, luthier or events (so it is harder than a real run)
    //   CONDUCTORS  the same runs with each of the six conductors of the turn battle
    //How well real people swipe is a guess, so read the numbers side by side rather than as the truth.
    //Results move by a few percent between runs.
    public static partial class DebugShots
    {
        private class SimPlayer
        {
            public string Name;
            public float Spread;        // timing error in seconds (one standard deviation)
            public float WrongWay;      // chance of swiping the wrong way
            public float WrongSize;     // chance of the wrong size
        }

        //With the normal windows (PERFECT 0.10, GOOD 0.22 seconds) these come out at about
        //EXPERT 79 / 20 / 1, STRONG 60 / 33 / 7, AVERAGE 37 / 34 / 29, NEWCOMER 24 / 26 / 50 (PERFECT / GOOD / MISS percent).
        //EXPERT (step 12) shows who MAESTRO is for.
        private static SimPlayer[] simPlayers =
        {
            new SimPlayer { Name = "FLAWLESS", Spread = 0f,    WrongWay = 0f,    WrongSize = 0f },
            new SimPlayer { Name = "EXPERT",   Spread = 0.08f, WrongWay = 0.005f, WrongSize = 0.03f },
            new SimPlayer { Name = "STRONG",   Spread = 0.12f, WrongWay = 0.01f, WrongSize = 0.05f },
            new SimPlayer { Name = "AVERAGE",  Spread = 0.2f,  WrongWay = 0.03f, WrongSize = 0.15f },
            new SimPlayer { Name = "NEWCOMER", Spread = 0.3f,  WrongWay = 0.08f, WrongSize = 0.3f },
        };

        private const int SimFights = 200;

        //Damage Share (step 15) : the enemy life taken by ATTACKs and by skills in every simulated fight, so a change
        //to ATTACK shows how much the skills carry (the user asked for ATTACK to matter less than the other moves)
        private static long simAttackDamage;
        private static long simSkillDamage;
        private const int SimRuns = 300;

        //Sim Roster : who joins a simulated run, in this order (two of them start floor 1)
        //NO MORE ERAS (29 Oct) : the five legends a run can meet (MusicianList.Shown), was ANNA KLARA OTTO MALI CHAI NUAN
        private static int[] simRoster = { 0, 1, 2, 6, 7 };

        private static void TurnSim()
        {
            //Seed : the same every time, so two reports can be compared. TACET_SIM_SEED picks another one (step 13),
            //run a few seeds and take the average when a change is small, the results move by a few percent.
            int seed = 4330;
            string seedText = Environment.GetEnvironmentVariable("TACET_SIM_SEED");
            if (seedText != null) int.TryParse(seedText, out seed);
            Random rng = new Random(seed);
            simAttackDamage = 0;
            simSkillDamage = 0;
            string report = "TACET TURN BATTLE BALANCE, ONE BAR  (" + SimFights + " fights per cell, " + SimRuns + " runs per player)\r\n";
            report += "Players : timing spread FLAWLESS 0 / EXPERT 0.08 / STRONG 0.12 / AVERAGE 0.2 / NEWCOMER 0.3 seconds\r\n\r\n";

            MusicianList.ResetRehearsals();
            List<Musician> three = SimBand(BattleRules.BandSize);
            string[] cells = { "EARLY", "LATE", "ELITE", "BOSS" };
            int[,] ranks = new int[simPlayers.Length, 4];
            float[] scales = new float[simPlayers.Length];
            int[] rankedWins = new int[simPlayers.Length];

            //FIGHTS : win percent / bars / life lost percent, with the packs of each place on each floor
            report += "FIGHTS at full life (the band of three) : WIN PERCENT  /  BARS  /  LIFE LOST PERCENT\r\n";
            for (int p = 0; p < simPlayers.Length; p++)
            {
                SimPlayer player = simPlayers[p];
                for (int floor = 1; floor <= 3; floor++)
                {
                    string line = "  " + player.Name.PadRight(9) + " FLOOR " + floor + "  ";
                    for (int c = 0; c < cells.Length; c++)
                    {
                        int wins = 0;
                        int bars = 0;
                        int lost = 0;
                        int max = 0;
                        for (int f = 0; f < SimFights; f++)
                        {
                            List<Enemy> pack = SimPack(cells[c], floor, rng, f);
                            simNotes = 0;
                            simPerfects = 0;
                            TurnBattle b = SimFight(three, pack, floor, player, null, rng, TurnKit.Beethoven, false);
                            if (b.Step == TurnStep.Won)
                            {
                                wins++;
                                int rank = SimRank(b);                                     // RANK
                                ranks[p, rank]++;
                                scales[p] += BattleRules.RankShards[rank];
                                rankedWins[p]++;
                            }
                            bars += b.Round;
                            for (int i = 0; i < b.Party.Count; i++)
                            {
                                lost += b.Party[i].MaxHp - b.Party[i].Hp;
                                max += b.Party[i].MaxHp;
                            }
                        }
                        line += cells[c].PadRight(6) + (wins * 100 / SimFights).ToString().PadLeft(3) + "% "
                              + (bars / (float)SimFights).ToString("0.0").PadLeft(4) + "b " + (lost * 100 / Math.Max(1, max)).ToString().PadLeft(3) + "%   ";
                    }
                    report += line + "\r\n";
                }
            }

            //Damage Share : from the FIGHTS above
            long allDamage = Math.Max(1, simAttackDamage + simSkillDamage);
            report += "  DAMAGE SHARE in all of the fights above : ATTACK " + (simAttackDamage * 100 / allDamage) + "%  SKILLS " + (simSkillDamage * 100 / allDamage) + "%\r\n";

            //RANKS : the won fights above, S / A / B / C and the average scale of their shards (29 Oct)
            report += "\r\nRANKS of the won fights above : S / A / B / C PERCENT  /  SHARDS SCALE\r\n";
            for (int p = 0; p < simPlayers.Length; p++)
            {
                int n = Math.Max(1, rankedWins[p]);
                report += "  " + simPlayers[p].Name.PadRight(9) + (ranks[p, 0] * 100 / n).ToString().PadLeft(3) + " / " + (ranks[p, 1] * 100 / n).ToString().PadLeft(3)
                        + " / " + (ranks[p, 2] * 100 / n).ToString().PadLeft(3) + " / " + (ranks[p, 3] * 100 / n).ToString().PadLeft(3)
                        + "   x" + (scales[p] / n).ToString("0.00") + "\r\n";
            }

            //EACH ENEMY as the leader of its pack on floor 2 (bosses floor 3), AVERAGE and STRONG players (step 14 : STRONG added,
            //every boss was 0 to 3 percent for AVERAGE, so they could not be told apart)
            report += "\r\nEACH ENEMY leading its pack (normal floor 2 late, elite floor 2, boss floor 3) : WIN PERCENT / BARS  AVERAGE, then STRONG\r\n";
            for (int e = 0; e < EnemyList.All.Length; e++)
            {
                Enemy enemy = EnemyList.All[e];
                int floor = enemy.Kind == EnemyKind.Boss ? 3 : 2;
                string line = "  " + enemy.Name.PadRight(22);
                for (int p = 3; p >= 2; p--)
                {
                    int wins = 0;
                    int bars = 0;
                    for (int f = 0; f < SimFights; f++)
                    {
                        List<Enemy> pack = new List<Enemy> { enemy };
                        int escorts = SimEscorts(enemy.Kind == EnemyKind.Normal ? "LATE" : (enemy.Kind == EnemyKind.Elite ? "ELITE" : "BOSS"), floor);
                        for (int k = 0; k < escorts; k++) pack.Add(EnemyList.PickOther(EnemyKind.Normal, 0, pack, rng));
                        TurnBattle b = SimFight(three, pack, floor, simPlayers[p], null, rng, TurnKit.Beethoven, false);
                        if (b.Step == TurnStep.Won) wins++;
                        bars += b.Round;
                    }
                    line += (wins * 100 / SimFights).ToString().PadLeft(3) + "%  " + (bars / (float)SimFights).ToString("0.0") + "b    ";
                }
                report += line + "\r\n";
            }

            //RUNS : three floors with life carried over, NORMAL and MAESTRO
            report += "\r\nRUNS (3 floors, life carried over, no motifs) : WIN PERCENT  /  LOST ON FLOOR 1, 2, 3\r\n";
            for (int m = 0; m < 2; m++)
            {
                bool maestro = m == 1;
                report += maestro ? "  MAESTRO\r\n" : "  NORMAL\r\n";
                for (int p = 0; p < simPlayers.Length; p++)
                {
                    int wins = 0;
                    int[] lostOn = new int[4];
                    for (int r = 0; r < SimRuns; r++)
                    {
                        int reached = SimRun(simPlayers[p], rng, TurnKit.Beethoven, maestro);
                        if (reached > 3) wins++;
                        else lostOn[reached]++;
                    }
                    report += "    " + simPlayers[p].Name.PadRight(9) + (wins * 100 / SimRuns).ToString().PadLeft(3) + "%    "
                            + (lostOn[1] * 100 / SimRuns) + "% / " + (lostOn[2] * 100 / SimRuns) + "% / " + (lostOn[3] * 100 / SimRuns) + "%\r\n";
                }
            }

            //CONDUCTORS : the six of the turn battle, STRONG and AVERAGE players, NORMAL
            report += "\r\nCONDUCTORS (runs as above, NORMAL) : WIN PERCENT  STRONG / AVERAGE\r\n";
            for (int c = 0; c < ConductorList.Shown.Length; c++)
            {
                Conductor conductor = ConductorList.Shown[c];
                report += "  " + conductor.Name.PadRight(18) + (SimRunWins(simPlayers[2], rng, conductor.Turn) + "%").PadLeft(5)
                        + " / " + SimRunWins(simPlayers[3], rng, conductor.Turn) + "%\r\n";
            }

            MusicianList.ResetRehearsals();
            File.WriteAllText(Path.Combine(outDir, "turnsim.txt"), report);
        }

        //Sim Band : the first count musicians of the simulated roster
        private static List<Musician> SimBand(int count)
        {
            List<Musician> band = new List<Musician>();
            for (int i = 0; i < count && i < simRoster.Length; i++) band.Add(MusicianList.All[simRoster[i]]);
            return band;
        }

        //Sim Pick : the enemies of one kind in turn, so every one of them is played the same amount
        private static Enemy SimPick(EnemyKind kind, int index)
        {
            List<Enemy> pool = new List<Enemy>();
            for (int i = 0; i < EnemyList.All.Length; i++)
                if (EnemyList.All[i].Kind == kind) pool.Add(EnemyList.All[i]);
            return pool[index % pool.Count];
        }

        //Sim Escorts : the ordinary enemies that come along (RunState.PackEscorts, by the same numbers)
        private static int SimEscorts(string place, int floor)
        {
            int f = floor - 1;
            if (place == "ELITE") return BattleRules.TurnEliteEscort[f];
            if (place == "BOSS") return BattleRules.TurnBossEscort[f];
            return (place == "LATE" ? BattleRules.TurnPackLate[f] : BattleRules.TurnPackEarly[f]) - 1;
        }

        //Sim Pack : a fight's enemies, the leader first
        private static List<Enemy> SimPack(string place, int floor, Random rng, int index)
        {
            EnemyKind kind = place == "ELITE" ? EnemyKind.Elite : (place == "BOSS" ? EnemyKind.Boss : EnemyKind.Normal);
            List<Enemy> pack = new List<Enemy> { SimPick(kind, index) };
            int escorts = SimEscorts(place, floor);
            for (int k = 0; k < escorts; k++) pack.Add(EnemyList.PickOther(EnemyKind.Normal, 0, pack, rng));   // as RunState.BeginBattle
            return pack;
        }

        //Sim Run Wins : the percent of SimRuns runs won (NORMAL)
        private static int SimRunWins(SimPlayer player, Random rng, TurnKit kit)
        {
            int wins = 0;
            for (int r = 0; r < SimRuns; r++)
                if (SimRun(player, rng, kit, false) > 3) wins++;
            return wins * 100 / SimRuns;
        }

        //Sim Run : returns the floor the band fell on, or 4 for a won run
        private static int SimRun(SimPlayer player, Random rng, TurnKit kit, bool maestro)
        {
            MusicianList.ResetRehearsals();
            Dictionary<Musician, int> life = new Dictionary<Musician, int>();
            string[] path = { "EARLY", "EARLY", "ELITE", "LATE", "LATE", "BOSS" };

            //THE BAND OF THREE : 3 of the 5 legends, picked at random (the order they are picked in is the order they play in)
            List<Musician> band = new List<Musician>(SimBand(simRoster.Length));
            while (band.Count > BattleRules.BandSize) band.RemoveAt(rng.Next(band.Count));

            for (int floor = 1; floor <= 3; floor++)
            {
                for (int i = 0; i < path.Length; i++)
                {
                    if (path[i] == "BOSS") SimHeal(band, life, BattleRules.BreatheRecover);   // a rest stop before the boss

                    int[] start = new int[band.Count];
                    for (int m = 0; m < band.Count; m++) start[m] = life.ContainsKey(band[m]) ? life[band[m]] : -1;

                    TurnBattle b = SimFight(band, SimPack(path[i], floor, rng, rng.Next(10)), floor, player, start, rng, kit, maestro);
                    if (b.Step != TurnStep.Won) return floor;

                    for (int m = 0; m < b.Party.Count; m++) life[b.Party[m].Musician] = b.HpAfterBattle(b.Party[m]);
                    SimHeal(band, life, BattleRules.RecoverAfterWin);                 // the result page's life back
                }
                for (int m = 0; m < band.Count; m++)                                 // WEAPON TIERS : every weapon one tier up per floor
                    band[m].Rehearsed = Math.Min(BattleRules.RehearseMax, band[m].Rehearsed + 1);
            }
            return 4;
        }

        private static void SimHeal(List<Musician> band, Dictionary<Musician, int> life, float share)
        {
            for (int m = 0; m < band.Count; m++)
            {
                int max = BattleRules.TurnHp[(int)band[m].Family];
                int now = life.ContainsKey(band[m]) && life[band[m]] >= 0 ? life[band[m]] : max;
                life[band[m]] = Math.Min(max, now + (int)Math.Round(share * max));
            }
        }

        //Sim Rank : the RANK of a won fight, as the result page gives it (BattleState.RankScore), from the notes this fight
        //swiped (simNotes, simPerfects) and the band's life kept
        private static int simNotes;
        private static int simPerfects;

        private static int SimRank(TurnBattle b)
        {
            int start = 0;
            int end = 0;
            for (int i = 0; i < b.Party.Count; i++)
            {
                start += b.Party[i].MaxHp;                                                 // the FIGHTS above start at full life
                end += b.Party[i].Hp;
            }
            float kept = start > 0 ? end / (float)start : 1f;
            return Math.Min(3, BattleState.RankIndexOf(BattleState.RankScoreOf(simPerfects, simNotes, kept)));
        }

        //Sim Fight : one fight to the end with the player's habit, the whole band on the field
        private static TurnBattle SimFight(List<Musician> band, List<Enemy> enemies, int floor, SimPlayer player, int[] startHp, Random rng, TurnKit kit, bool maestro)
        {
            TurnBattle b = new TurnBattle(band, enemies, floor, rng, startHp, 0);
            b.Kit = kit;
            b.Maestro = maestro;

            for (int step = 0; step < 900 && b.Step != TurnStep.Won && b.Step != TurnStep.Lost; step++)
            {
                if (b.Step == TurnStep.Plan)
                {
                    SimPlan(b, player, rng);
                    if (b.Step == TurnStep.Plan) b.PlayBar();                              // the SIGNATURE may have won it
                }
                else if (b.Step == TurnStep.PlayerTurn)
                {
                    BarMove move = b.CurrentMove;
                    int before = EnemyLife(b);
                    if (move.Skill == null) b.Attack();
                    else b.UseSkill(SimNotes(b, move.Skill, player, rng));
                    if (move.Skill == null) simAttackDamage += before - EnemyLife(b);
                    else simSkillDamage += before - EnemyLife(b);
                }
                else
                {
                    Grade[] parries = new Grade[b.Current.IntentNotes.Length];
                    for (int i = 0; i < parries.Length; i++)
                    {
                        NoteResult r = SimSwipe(b, player, rng);
                        parries[i] = r.Grade == Grade.Perfect && !r.RightSize ? Grade.Good : r.Grade;   // the wrong size only half blocks
                    }
                    b.TakeEnemyAttack(parries);
                }
            }
            return b;
        }

        private static int EnemyLife(TurnBattle b)
        {
            int life = 0;
            for (int i = 0; i < b.Enemies.Count; i++) life += b.Enemies[i].Hp;
            return life;
        }

        //Sim Plan : the habit that writes one bar (see the top of this file)
        private static void SimPlan(TurnBattle b, SimPlayer player, Random rng)
        {
            //SIGNATURE first, it costs no move
            if (b.CanSignature)
            {
                NoteResult[] four = new NoteResult[4];
                for (int i = 0; i < 4; i++) four[i] = new NoteResult(SimSwipe(b, player, rng).Grade, true, 1f);
                b.UseSignature(four);
                if (b.Step != TurnStep.Plan) return;
            }

            Fighter weakest = null;
            for (int i = 0; i < b.Enemies.Count; i++)
                if (!b.Enemies[i].Down && (weakest == null || b.Enemies[i].Hp < weakest.Hp)) weakest = b.Enemies[i];
            if (weakest == null) return;

            Fighter hurt = null;
            for (int i = 0; i < b.Party.Count; i++)
                if (!b.Party[i].Down && b.Party[i].Hp * 2 < b.Party[i].MaxHp && (hurt == null || b.Party[i].Hp < hurt.Hp)) hurt = b.Party[i];

            for (int i = 0; i < b.Party.Count; i++)
            {
                Fighter me = b.Party[i];
                if (!b.CanPlan(me)) continue;

                Skill heal = null;
                Skill hit = null;
                Skill[] mine = { SkillList.First(me.Musician), SkillList.Second(me.Musician) };
                for (int k = 0; k < mine.Length; k++)
                {
                    if (!b.CanPlanSkill(me, mine[k])) continue;
                    if (mine[k].Heal > 0) heal = mine[k];
                    if (mine[k].HitsEnemy && (hit == null || mine[k].Cost > hit.Cost)) hit = mine[k];
                }

                //DECREE (THE DEAF CROWN, step 14) : no skill in a NO SKILLS bar, a skill if there is BREATH for one in a NO ATTACKS bar
                bool noSkills = b.Decree == DecreeKind.NoSkills;
                bool noAttacks = b.Decree == DecreeKind.NoAttacks;
                if (!noSkills && heal != null && hurt != null)
                {
                    b.PlanSkill(me, heal, heal.Target == SkillTarget.Ally ? hurt : me);
                    hurt = null;
                }
                else if (!noSkills && hit != null && (noAttacks || b.BreathFree >= 3)) b.PlanSkill(me, hit, weakest);
                else if (noAttacks && heal != null) b.PlanSkill(me, heal, me);
                else b.PlanAttack(me, weakest);
            }
        }

        private static NoteResult[] SimNotes(TurnBattle b, Skill s, SimPlayer player, Random rng)
        {
            NoteResult[] results = new NoteResult[s.Notes.Length];
            for (int i = 0; i < results.Length; i++)
            {
                NoteResult r = SimSwipe(b, player, rng);
                if (s.Notes[i].Kind == NoteKind.Spark || s.Notes[i].Kind == NoteKind.Roll) r.RightSize = true;
                if (s.Notes[i].Kind == NoteKind.Hold) r.Hold = r.Grade == Grade.Perfect ? 1f : 0.6f;
                results[i] = r;
            }
            return results;
        }

        //Sim Swipe : one swipe, graded by the battle's own Judge. Later than LateLimit is no swipe at all,
        //earlier than EarlyTime is the hand getting ready and the note is missed.
        private static NoteResult SimSwipe(TurnBattle b, SimPlayer player, Random rng)
        {
            //ADVANCED PART : Box-Muller, two even random numbers make one on a bell curve
            double u1 = 1.0 - rng.NextDouble();
            double u2 = rng.NextDouble();
            float offset = (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2)) * player.Spread;

            bool rightWay = rng.NextDouble() >= player.WrongWay;
            Grade g;
            if (offset > b.LateLimit) g = Grade.Hesitate;
            else if (offset < -BattleRules.EarlyTime) g = Grade.Miss;
            else g = b.Judge(offset, rightWay);
            bool rightSize = b.AnySize || rng.NextDouble() >= player.WrongSize;              // LOCKED TEMPO
            simNotes++;                                                                      // RANK
            if (g == Grade.Perfect && rightSize) simPerfects++;
            return new NoteResult(g, rightSize, 1f);
        }
    }
}
