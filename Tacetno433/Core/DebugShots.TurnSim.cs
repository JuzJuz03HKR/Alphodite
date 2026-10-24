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
    //   SIGNATURE as soon as it is full. CALL the first one on the BENCH when the free BREATH covers it while
    //   fewer than four play or somebody fell. Then for each musician : a heal when someone is under half life,
    //   the dearest skill that hits enemies when 3 BREATH or more are free (step 13 : and it leaves enough for a
    //   CALL it is saving up for), otherwise ATTACK the enemy with the least life.
    //Reports :
    //   FIGHTS      one fight at full life with the PACKS of the run (BattleRules ONE BAR), four musicians, two on the field
    //   RUNS        three floors in a row with life carried over : N N E N N, a rest (40 percent back), then the boss,
    //               after every win the result page's life back. FirstFloorRecruits musicians on floor 1, LaterFloorRecruits
    //               more at each new floor, one more after an elite at EliteRecruitChance, one more rehearsal each per floor,
    //               THE UNHEARING, no motifs, shops or events (so it is harder than a real run)
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
        private const int SimRuns = 300;

        //Sim Roster : who joins a simulated run, in this order (two of them start floor 1)
        private static int[] simRoster = { 0, 1, 2, 3, 4, 5 };

        private static void TurnSim()
        {
            //Seed : the same every time, so two reports can be compared. TACET_SIM_SEED picks another one (step 13),
            //run a few seeds and take the average when a change is small, the results move by a few percent.
            int seed = 4330;
            string seedText = Environment.GetEnvironmentVariable("TACET_SIM_SEED");
            if (seedText != null) int.TryParse(seedText, out seed);
            Random rng = new Random(seed);
            string report = "TACET TURN BATTLE BALANCE, ONE BAR  (" + SimFights + " fights per cell, " + SimRuns + " runs per player)\r\n";
            report += "Players : timing spread FLAWLESS 0 / EXPERT 0.08 / STRONG 0.12 / AVERAGE 0.2 / NEWCOMER 0.3 seconds\r\n\r\n";

            MusicianList.ResetRehearsals();
            List<Musician> four = SimBand(4);
            string[] cells = { "EARLY", "LATE", "ELITE", "BOSS" };

            //FIGHTS : win percent / bars / life lost percent, with the packs of each place on each floor
            report += "FIGHTS at full life (4 musicians, 2 on the field) : WIN PERCENT  /  BARS  /  LIFE LOST PERCENT  /  CALLS\r\n";
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
                        int calls = 0;
                        for (int f = 0; f < SimFights; f++)
                        {
                            List<Enemy> pack = SimPack(cells[c], floor, rng, f);
                            TurnBattle b = SimFight(four, pack, floor, player, null, rng, TurnKit.Beethoven, false);
                            if (b.Step == TurnStep.Won) wins++;
                            bars += b.Round;
                            calls += b.Calls;
                            for (int i = 0; i < b.Party.Count; i++)
                            {
                                lost += b.Party[i].MaxHp - b.Party[i].Hp;
                                max += b.Party[i].MaxHp;
                            }
                        }
                        line += cells[c].PadRight(6) + (wins * 100 / SimFights).ToString().PadLeft(3) + "% "
                              + (bars / (float)SimFights).ToString("0.0").PadLeft(4) + "b " + (lost * 100 / Math.Max(1, max)).ToString().PadLeft(3) + "% "
                              + (calls / (float)SimFights).ToString("0.0") + "c   ";
                    }
                    report += line + "\r\n";
                }
            }

            //EACH ENEMY as the leader of its pack on floor 2 (bosses floor 3), AVERAGE player
            report += "\r\nEACH ENEMY leading its pack (normal floor 2 late, elite floor 2, boss floor 3), AVERAGE player : WIN PERCENT / BARS\r\n";
            for (int e = 0; e < EnemyList.All.Length; e++)
            {
                Enemy enemy = EnemyList.All[e];
                int floor = enemy.Kind == EnemyKind.Boss ? 3 : 2;
                int wins = 0;
                int bars = 0;
                for (int f = 0; f < SimFights; f++)
                {
                    List<Enemy> pack = new List<Enemy> { enemy };
                    int escorts = SimEscorts(enemy.Kind == EnemyKind.Normal ? "LATE" : (enemy.Kind == EnemyKind.Elite ? "ELITE" : "BOSS"), floor);
                    for (int k = 0; k < escorts; k++) pack.Add(EnemyList.PickOther(EnemyKind.Normal, 0, pack, rng));
                    TurnBattle b = SimFight(four, pack, floor, simPlayers[3], null, rng, TurnKit.Beethoven, false);
                    if (b.Step == TurnStep.Won) wins++;
                    bars += b.Round;
                }
                report += "  " + enemy.Name.PadRight(22) + (wins * 100 / SimFights).ToString().PadLeft(3) + "%  " + (bars / (float)SimFights).ToString("0.0") + "b\r\n";
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
            int members = BattleRules.FirstFloorRecruits;

            for (int floor = 1; floor <= 3; floor++)
            {
                if (floor > 1) members += BattleRules.LaterFloorRecruits;                 // joining at each new floor
                for (int i = 0; i < path.Length; i++)
                {
                    List<Musician> band = SimBand(members);
                    if (path[i] == "BOSS") SimHeal(band, life, BattleRules.BreatheRecover);   // a rest stop before the boss

                    int[] start = new int[band.Count];
                    for (int m = 0; m < band.Count; m++) start[m] = life.ContainsKey(band[m]) ? life[band[m]] : -1;

                    TurnBattle b = SimFight(band, SimPack(path[i], floor, rng, rng.Next(10)), floor, player, start, rng, kit, maestro);
                    if (b.Step != TurnStep.Won) return floor;

                    for (int m = 0; m < b.Party.Count; m++) life[b.Party[m].Musician] = b.HpAfterBattle(b.Party[m]);
                    SimHeal(band, life, BattleRules.RecoverAfterWin);                 // the result page's life back
                    if (path[i] == "ELITE" && rng.Next(100) < BattleRules.EliteRecruitChance) members++;   // the result page's elite recruit
                }
                for (int m = 0; m < members && m < simRoster.Length; m++) MusicianList.All[simRoster[m]].Rehearsed++;   // a rehearsal each per floor
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

        //Sim Fight : one fight to the end with the player's habit, BattleRules.TurnStartBand on the field
        private static TurnBattle SimFight(List<Musician> band, List<Enemy> enemies, int floor, SimPlayer player, int[] startHp, Random rng, TurnKit kit, bool maestro)
        {
            TurnBattle b = new TurnBattle(band, enemies, floor, rng, startHp, 0, BattleRules.TurnStartBand);
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
                    if (move.Skill == null) b.Attack();
                    else b.UseSkill(SimNotes(b, move.Skill, player, rng));
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

            //CALL : while fewer than four play, or someone fell
            int standing = 0;
            bool fallen = false;
            for (int i = 0; i < b.Party.Count; i++)
            {
                if (b.Party[i].Down) fallen = true;
                else standing++;
            }
            bool wantCall = b.Bench.Count > 0 && (standing < 4 || fallen) && b.Party.Count < BattleRules.TurnFieldMost;
            if (wantCall && b.CanCall(0))
            {
                b.Call(0);
                wantCall = false;
            }
            int keep = wantCall ? b.CallCost : 0;   // step 13 : saving up for a CALL it cannot pay yet (FELT HAND makes it dearer)

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

                if (heal != null && hurt != null)
                {
                    b.PlanSkill(me, heal, heal.Target == SkillTarget.Ally ? hurt : me);
                    hurt = null;
                }
                else if (hit != null && b.BreathFree >= 3 && b.BreathFree - hit.Cost >= keep) b.PlanSkill(me, hit, weakest);
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
            return new NoteResult(g, rightSize, 1f);
        }
    }
}
