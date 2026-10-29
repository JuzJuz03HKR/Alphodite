using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
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
    //   SIGNATURE as soon as it is full. Then for each musician, the first that fits :
    //   a heal when someone is under half life,
    //   a guard (SHIELD, THUNDER CALL) when a friend under half life is the one an enemy aims at and they themself are not,
    //   the dearest skill that hits enemies when 3 BREATH or more are free (PAGANINI pays the rest in life while that leaves
    //   them above three quarters, THE DEVIL'S BARGAIN), a CHAKRAM only against two enemies or more,
    //   a boost (THE LESSON for the friend with the dearest skill that hits, FANFARE) when 4 or more are free,
    //   otherwise ATTACK the enemy with the least life.
    //   The band writes in field order, PUNTO first (THE HUNTING CALL : the musician after them gets the CUE).
    //   (Until 29 Oct it also CALLED the BENCH in. The guards, the boosts and the bargain came with THE FIVE KITS, 29 Oct.)
    //Reports :
    //   FIGHTS      one fight at full life with the PACKS of the run (BattleRules ONE BAR), the band of three
    //   RANKS       the RANK of the won fights above (29 Oct, BattleState.RankScore), S / A / B / C percent and the shards' scale
    //   RUNS        three floors in a row with life carried over : N N E N N, a rest (40 percent back), then the boss,
    //               after every win the result page's life back. THE BAND OF THREE (29 Oct) : 3 of the 5 legends picked at
    //               random for each run, every weapon one tier up after each floor (WEAPON TIERS, as a rehearsal each per
    //               floor before), THE UNHEARING, no motifs, shops, luthier or events (so it is harder than a real run)
    //   CONDUCTORS  the same runs with each of the six conductors of the turn battle
    //   MUSICIANS   the NORMAL STRONG runs above, won with each legend in the band and without them (THE FIVE KITS, 29 Oct)
    //How well real people swipe is a guess, so read the numbers side by side rather than as the truth.
    //Results move by a few percent between runs.
    //TACET_SIM_SEED picks another seed, TACET_SIM_KIT another conductor, TACET_SIM_SET other numbers (SimSet, Beta1 29 Oct).
    public static partial class DebugShots
    {
        private class SimPlayer
        {
            public string Name;
            public float Spread;        // timing error in seconds (one standard deviation)
            public float WrongWay;      // chance of swiping the wrong way
        }

        //With the normal windows (PERFECT 0.10, GOOD 0.22 seconds) these come out at about
        //EXPERT 79 / 20 / 1, STRONG 60 / 33 / 7, AVERAGE 37 / 34 / 29, NEWCOMER 24 / 26 / 50 (PERFECT / GOOD / MISS percent).
        //EXPERT (step 12) shows who MAESTRO is for.
        private static SimPlayer[] simPlayers =
        {
            new SimPlayer { Name = "FLAWLESS", Spread = 0f,    WrongWay = 0f },
            new SimPlayer { Name = "EXPERT",   Spread = 0.08f, WrongWay = 0.005f },
            new SimPlayer { Name = "STRONG",   Spread = 0.12f, WrongWay = 0.01f },
            new SimPlayer { Name = "AVERAGE",  Spread = 0.2f,  WrongWay = 0.03f },
            new SimPlayer { Name = "NEWCOMER", Spread = 0.3f,  WrongWay = 0.08f },
        };

        private const int SimFights = 200;

        //Damage Share (step 15) : the enemy life taken by ATTACKs and by skills in every simulated fight, so a change
        //to ATTACK shows how much the skills carry (the user asked for ATTACK to matter less than the other moves)
        private static long simAttackDamage;
        private static long simSkillDamage;
        private const int SimRuns = 300;

        //Musician Tally (THE FIVE KITS, 29 Oct) : while it is on, every run counts who was in its band and whether it was won
        private static bool simTally;
        private static int[] simRunsWith = new int[MusicianList.All.Length];
        private static int[] simWinsWith = new int[MusicianList.All.Length];
        private static int simTallyRuns;
        private static int simTallyWins;

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

            //Kit : the runs are played as THE UNHEARING. TACET_SIM_KIT (Beta1 29 Oct) plays them as another conductor instead,
            //Bach or Fanny, to compare the three kept ones (it is a TurnKit name).
            TurnKit runKit = TurnKit.Beethoven;
            string kitText = Environment.GetEnvironmentVariable("TACET_SIM_KIT");
            if (kitText != null) Enum.TryParse(kitText, out runKit);
            string changed = SimSet();
            simAttackDamage = 0;
            simSkillDamage = 0;
            simTallyRuns = 0;
            simTallyWins = 0;
            Array.Clear(simRunsWith, 0, simRunsWith.Length);
            Array.Clear(simWinsWith, 0, simWinsWith.Length);
            string report = "TACET TURN BATTLE BALANCE, ONE BAR  (" + SimFights + " fights per cell, " + SimRuns + " runs per player)\r\n";
            report += "Players : timing spread FLAWLESS 0 / EXPERT 0.08 / STRONG 0.12 / AVERAGE 0.2 / NEWCOMER 0.3 seconds\r\n";
            if (changed.Length > 0) report += "TACET_SIM_SET :" + changed + "\r\n";
            report += "\r\n";

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
            report += "\r\nRUNS (3 floors, life carried over, no motifs, " + runKit + ") : WIN PERCENT  /  LOST ON FLOOR 1, 2, 3\r\n";
            for (int m = 0; m < 2; m++)
            {
                bool maestro = m == 1;
                report += maestro ? "  MAESTRO\r\n" : "  NORMAL\r\n";
                for (int p = 0; p < simPlayers.Length; p++)
                {
                    int wins = 0;
                    int[] lostOn = new int[4];
                    simTally = !maestro && p == 2;                                         // MUSICIANS : the NORMAL STRONG runs
                    for (int r = 0; r < SimRuns; r++)
                    {
                        int reached = SimRun(simPlayers[p], rng, runKit, maestro);
                        if (reached > 3) wins++;
                        else lostOn[reached]++;
                    }
                    report += "    " + simPlayers[p].Name.PadRight(9) + (wins * 100 / SimRuns).ToString().PadLeft(3) + "%    "
                            + (lostOn[1] * 100 / SimRuns) + "% / " + (lostOn[2] * 100 / SimRuns) + "% / " + (lostOn[3] * 100 / SimRuns) + "%\r\n";
                    simTally = false;
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

            //MUSICIANS : the NORMAL STRONG runs above, won with each legend in the band, and without them
            report += "\r\nMUSICIANS (the NORMAL STRONG runs above) : WIN PERCENT  WITH them in the band / WITHOUT them\r\n";
            for (int i = 0; i < simRoster.Length; i++)
            {
                int m = simRoster[i];
                int withRuns = Math.Max(1, simRunsWith[m]);
                int withoutRuns = Math.Max(1, simTallyRuns - simRunsWith[m]);
                report += "  " + MusicianList.All[m].Name.PadRight(10) + (simWinsWith[m] * 100 / withRuns + "%").PadLeft(5)
                        + " / " + ((simTallyWins - simWinsWith[m]) * 100 / withoutRuns) + "%\r\n";
            }

            MusicianList.ResetRehearsals();
            File.WriteAllText(Path.Combine(outDir, "turnsim.txt"), report);
        }

        //Sim Set (Beta1 29 Oct) : TACET_SIM_SET tries other numbers for one run of the simulator, without building the game
        //again. "TurnLessonScale=1.2;IL CANNONE.Damage=1.5" sets a number of Data/BattleRules.cs by its name, or a number of a
        //skill (Data/Skill.cs) by the skill's name, a dot and the field. The game itself never reads it. Returns what it set
        //(a name it cannot find comes back with a ?), so the report says which numbers it played with.
        //ADVANCED PART : reflection. typeof(BattleRules).GetField finds a field by its name while the program runs, SetValue
        //writes it (null stands for "no object", the field is static), Convert.ChangeType turns the text into the field's own
        //type (int, float or bool), always with a dot for the decimals (InvariantCulture).
        private static string SimSet()
        {
            string text = Environment.GetEnvironmentVariable("TACET_SIM_SET");
            if (string.IsNullOrEmpty(text)) return "";
            string done = "";
            string[] pairs = text.Split(';');
            for (int i = 0; i < pairs.Length; i++)
            {
                int eq = pairs[i].IndexOf('=');
                if (eq <= 0) continue;
                string name = pairs[i].Substring(0, eq).Trim();
                string value = pairs[i].Substring(eq + 1).Trim();
                object owner = null;
                FieldInfo field = typeof(BattleRules).GetField(name);
                int dot = name.LastIndexOf('.');
                if (field == null && dot > 0)
                {
                    for (int k = 0; k < SkillList.All.Length && owner == null; k++)
                        if (SkillList.All[k].Name == name.Substring(0, dot)) owner = SkillList.All[k];
                    field = owner != null ? typeof(Skill).GetField(name.Substring(dot + 1)) : null;
                }
                if (field == null || field.IsInitOnly)
                {
                    done += " " + name + "=?";
                    continue;
                }
                field.SetValue(owner, Convert.ChangeType(value, field.FieldType, CultureInfo.InvariantCulture));
                done += " " + name + "=" + value;
            }
            return done;
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
                    if (b.Step != TurnStep.Won)
                    {
                        Tally(band, false);
                        return floor;
                    }

                    for (int m = 0; m < b.Party.Count; m++) life[b.Party[m].Musician] = b.HpAfterBattle(b.Party[m]);
                    SimHeal(band, life, BattleRules.RecoverAfterWin);                 // the result page's life back
                }
                for (int m = 0; m < band.Count; m++)                                 // WEAPON TIERS : every weapon one tier up per floor
                    band[m].Rehearsed = Math.Min(BattleRules.RehearseMax, band[m].Rehearsed + 1);
            }
            Tally(band, true);
            return 4;
        }

        //Tally : MUSICIANS, one run's band and whether it was won (only while simTally is on)
        private static void Tally(List<Musician> band, bool won)
        {
            if (!simTally) return;
            simTallyRuns++;
            if (won) simTallyWins++;
            for (int m = 0; m < band.Count; m++)
            {
                simRunsWith[band[m].Index]++;
                if (won) simWinsWith[band[m].Index]++;
            }
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
                    for (int i = 0; i < parries.Length; i++) parries[i] = SimSwipe(b, player, rng).Grade;
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
                for (int i = 0; i < 4; i++) four[i] = new NoteResult(SimSwipe(b, player, rng).Grade, 1f);
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

            //In Danger : a musician under half life that an enemy means to hit next (SHIELD and THUNDER CALL are for them).
            //Standing : the enemies still up (a CHAKRAM is for a pack).
            Fighter danger = null;
            int standing = 0;
            for (int i = 0; i < b.Enemies.Count; i++)
            {
                Fighter e = b.Enemies[i];
                if (e.Down) continue;
                standing++;
                Fighter t = e.IntentTarget;
                if (e.IntentRest || t == null || t.Down || t.Hp * 2 >= t.MaxHp) continue;
                if (danger == null || t.Hp < danger.Hp) danger = t;
            }

            //Write Order : the band in field order, but a musician with THE HUNTING CALL (PUNTO) writes first, so the one after
            //them gets the CUE (a player learns that quickly)
            List<Fighter> writeOrder = new List<Fighter>();
            for (int i = 0; i < b.Party.Count; i++)
                if (b.Party[i].Musician.Trait == MusicianTrait.HuntingCall) writeOrder.Add(b.Party[i]);
            for (int i = 0; i < b.Party.Count; i++)
                if (!writeOrder.Contains(b.Party[i])) writeOrder.Add(b.Party[i]);

            for (int i = 0; i < writeOrder.Count; i++)
            {
                Fighter me = writeOrder[i];
                if (!b.CanPlan(me)) continue;

                Skill heal = null;
                Skill hit = null;
                Skill guard = null;
                Skill boost = null;
                Skill[] mine = { SkillList.First(me.Musician), SkillList.Second(me.Musician) };
                for (int k = 0; k < mine.Length; k++)
                {
                    Skill s = mine[k];
                    if (!b.CanPlanSkill(me, s)) continue;
                    if (s.Heal > 0) heal = s;
                    else if (s.HitsEnemy && (s.Bounce == 0 || standing >= 2) && (hit == null || s.Cost > hit.Cost)) hit = s;
                    else if (s.Kind == SkillKind.Defend) guard = s;
                    else if (s.Kind == SkillKind.Boost) boost = s;
                }

                //DECREE (THE DEAF CROWN, step 14) : no skill in a NO SKILLS bar, a skill if there is BREATH for one in a NO ATTACKS bar
                bool noSkills = b.Decree == DecreeKind.NoSkills;
                bool noAttacks = b.Decree == DecreeKind.NoAttacks;
                bool steady = me.Hp * 2 >= me.MaxHp;
                bool written = false;
                if (!noSkills && heal != null && hurt != null && b.PlanSkill(me, heal, heal.Target == SkillTarget.Ally ? hurt : me))
                {
                    hurt = null;
                    written = true;
                }
                if (!written && !noSkills && guard != null && danger != null && danger != me && steady && b.PlanSkill(me, guard, guard.Shield ? danger : me))
                {
                    danger = null;
                    written = true;
                }
                if (!written && !noSkills && hit != null && (noAttacks || b.BreathFree >= 3 || SimBargain(b, me, hit))) written = b.PlanSkill(me, hit, weakest);
                if (!written && !noSkills && boost != null && (noAttacks || b.BreathFree >= 4)) written = b.PlanSkill(me, boost, boost.Lesson ? SimPupil(b, me) : me);
                if (!written && noAttacks && heal != null) written = b.PlanSkill(me, heal, me);
                if (!written) b.PlanAttack(me, weakest);
            }
        }

        //Sim Bargain : THE DEVIL'S BARGAIN, the skill is paid partly in life only while that leaves the musician above three
        //quarters of their life (paying down to half lost more runs than it won : 61 percent of NORMAL STRONG runs, 65 without it)
        private static bool SimBargain(TurnBattle b, Fighter me, Skill s)
        {
            if (!b.PaysInLife(me)) return false;
            int price = (s.Cost - Math.Max(0, b.BreathFree)) * BattleRules.TurnPaganiniLife;
            return (me.Hp - price) * 4 > me.MaxHp * 3;
        }

        //Sim Pupil : THE LESSON goes to the friend with the dearest skill that hits, who has no lesson waiting yet
        private static Fighter SimPupil(TurnBattle b, Fighter teacher)
        {
            Fighter pupil = null;
            int dearest = -1;
            for (int i = 0; i < b.Party.Count; i++)
            {
                Fighter f = b.Party[i];
                if (f == teacher || f.Down || f.Lesson) continue;
                Skill[] two = { SkillList.First(f.Musician), SkillList.Second(f.Musician) };
                int cost = 0;
                for (int k = 0; k < two.Length; k++)
                    if (two[k].HitsEnemy) cost = Math.Max(cost, two[k].Cost);
                if (cost > dearest)
                {
                    pupil = f;
                    dearest = cost;
                }
            }
            return pupil;
        }

        private static NoteResult[] SimNotes(TurnBattle b, Skill s, SimPlayer player, Random rng)
        {
            NoteResult[] results = new NoteResult[s.Notes.Length];
            for (int i = 0; i < results.Length; i++)
            {
                NoteResult r = SimSwipe(b, player, rng);
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
            simNotes++;                                                                      // RANK
            if (g == Grade.Perfect) simPerfects++;
            return new NoteResult(g, 1f);
        }
    }
}
