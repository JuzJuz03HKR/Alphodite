using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Battle;
using Tacetno433.Data;
using Tacetno433.Screens;

namespace Tacetno433.Core
{
    //DebugShots : DEVELOPER TOOL, NOT PART OF THE GAME.
    //Safe to delete, together with the lines in TacetGame and Program that mention it.
    //
    //Starting the game like this
    //    Tacetno433.exe --shots title,route,duel@240 --out C:\pictures
    //opens each named page with a sample run, waits some frames (default 90, or the number
    //after @), saves a picture of it and quits. It lets pages be checked without anybody
    //touching the mouse or keyboard. With no arguments the game starts normally.
    //
    //ADVANCED PARTS : RenderTarget2D (drawing into a picture instead of the window) and
    //writing a PNG file. Neither is used anywhere else in the game.
    public static class DebugShots
    {
        public static bool Active;
        public static RenderTarget2D Target;

        private static string[] names = new string[0];
        private static int[] waits = new int[0];
        private static string outDir = "";
        private static int index;
        private static int frames;

        //Args Parse : called from Program before the game is made
        public static void Parse(string[] args)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--out") outDir = args[i + 1];

                if (args[i] == "--shots")
                {
                    string[] parts = args[i + 1].Split(',');
                    names = new string[parts.Length];
                    waits = new int[parts.Length];

                    for (int p = 0; p < parts.Length; p++)
                    {
                        //Name At Frames : "duel@240" waits 240 frames before the picture
                        string[] bits = parts[p].Split('@');
                        names[p] = bits[0];
                        waits[p] = bits.Length > 1 ? int.Parse(bits[1]) : 90;
                    }
                }
            }

            Active = names.Length > 0 && outDir.Length > 0;
        }

        //Shots Begin : called from LoadContent instead of opening the title page
        public static void Begin(TacetGame game)
        {
            Target = new RenderTarget2D(game.GraphicsDevice, TacetGame.ScreenW, TacetGame.ScreenH);
            Directory.CreateDirectory(outDir);

            //Balance Check : no pictures, just play many fights and write a report
            if (names[0] == "simulate")
            {
                Simulate(game);
                game.Exit();
                return;
            }

            index = 0;
            frames = 0;
            Open(game, names[0]);
        }

        //Simulate : plays 300 fights per line with a fixed habit, no screen involved.
        //Every run in the report uses THE INFERNO (conductor 3) with no motifs.
        //The report shows how often each habit wins, so BattleRules can be tuned on evidence.
        private static void Simulate(TacetGame game)
        {
            string report = "TACET BALANCE CHECK  (floor 1, 300 fights per line)\r\n"
                          + "AUDIO LOADED  SFX " + Audio.SoundBank.LoadedSfx + "  MUSIC " + Audio.SoundBank.LoadedMusic + "\r\n\r\n";
            EnemyKind[] kinds = { EnemyKind.Normal, EnemyKind.Elite, EnemyKind.Boss };
            string[] habits = { "AUTO PLAN, ALWAYS GOOD PLAY", "AUTO PLAN, NEVER PRESSES", "AUTO PLAN, PERFECT BOOST ON HEAVY",
                                "NO PLAN, ALWAYS EASE", "AUTO PLAN, ALWAYS PERFECT (COMBO)" };
            Random random = new Random(7);

            for (int k = 0; k < kinds.Length; k++)
            {
                for (int h = 0; h < habits.Length; h++)
                {
                    int wins = 0, roundsTotal = 0, staminaTotal = 0, lowest = 999;

                    for (int n = 0; n < 300; n++)
                    {
                        RunState run = SampleRun(game, "sim");
                        run.Chosen.Type = kinds[k] == EnemyKind.Boss ? NodeType.Boss : (kinds[k] == EnemyKind.Elite ? NodeType.Elite : NodeType.Battle);
                        run.BeginBattle();
                        run.RestoreAllStamina();
                        BattleState b = run.Battle;

                        while (!b.Finished)
                        {
                            if (h == 3) run.Formation.ClearAll(); else b.AutoPlan();

                            for (int beat = 0; beat < BattleRules.BeatsPerRound && !b.Finished; beat++)
                            {
                                Choice choice = Choice.Normal;
                                Grade grade = Grade.Good;
                                if (h == 1) grade = Grade.Hesitate;
                                if (h == 2 && b.IsHeavy(beat)) { choice = Choice.Boost; grade = Grade.Perfect; }
                                if (h == 3) choice = Choice.Ease;
                                if (h == 4) grade = Grade.Perfect;
                                if (!b.HasAction(beat)) grade = Grade.None;

                                b.Resolve(beat, choice, grade);
                                if (run.Stamina < lowest) lowest = run.Stamina;
                            }

                            if (!b.Finished) b.EndRound();
                        }

                        if (b.PlayerWon) wins++;
                        roundsTotal += Math.Min(b.Round, BattleRules.MaxRounds);
                        staminaTotal += run.Stamina;
                    }

                    report += kinds[k].ToString().ToUpper().PadRight(8)
                            + habits[h].PadRight(36)
                            + "WIN " + (wins * 100 / 300).ToString().PadLeft(3) + "%"
                            + "   AVG ROUNDS " + (roundsTotal / 300f).ToString("0.0")
                            + "   AVG STAMINA LEFT " + (staminaTotal / 300)
                            + "   LOWEST " + lowest + "\r\n";
                }
                report += "\r\n";
            }

            report += SimulateRuns(game, random);
            File.WriteAllText(Path.Combine(outDir, "simulate.txt"), report);
        }

        //Whole Runs : plays complete runs from the first era choice to the end, 200 per habit,
        //with THE APPRENTICE. Paths are picked at random. Rests breathe, shops buy stamina when
        //the band is low and a seat when someone waits on the bench, events and motifs are skipped.
        //So it is a slightly harsh picture of a real run, where the player also collects motifs.
        private static string SimulateRuns(TacetGame game, Random random)
        {
            string[] habits = { "GOOD PLAY EVERY BEAT, NEVER BOOSTS", "SKILLED  (PERFECT, BOOST HEAVY, EASE WHEN LOW)",
                                "AVERAGE  (40% PERFECT, 10% MISS, SAME CHOICES)" };
            string report = "WHOLE RUNS  (" + BattleRules.FloorsPerRun + " floors, 200 runs per line, conductor THE APPRENTICE)\r\n";

            for (int h = 0; h < habits.Length; h++)
            {
                int won = 0;
                int[] lostOnFloor = new int[BattleRules.FloorsPerRun + 1];
                int[] lostTo = new int[3];
                int bossStamina = 0, bossCount = 0;

                for (int n = 0; n < 200; n++)
                {
                    RunState run = new RunState();
                    run.Start(ConductorList.All[0], game.StoryFont, PanelStrip.CaptionWrapWidth);
                    run.BandName = "SIM";
                    bool alive = true;

                    for (int guard = 0; guard < 200 && alive && !run.RunComplete; guard++)
                    {
                        if (run.AtFloorOpening)
                        {
                            run.ChooseEra(random.Next(EraList.All.Length));
                            run.RecruitAtOpening();
                            run.LeaveOpening();
                            continue;
                        }

                        run.Chosen = run.Options[random.Next(run.Options.Length)];
                        NodeType type = run.Chosen.Type;

                        if (type == NodeType.Battle || type == NodeType.Elite || type == NodeType.Boss)
                        {
                            run.BeginBattle();
                            if (type == NodeType.Boss) { bossStamina += run.Stamina * 100 / run.MaxStamina; bossCount++; }

                            if (!PlayFight(run, h, random))
                            {
                                alive = false;
                                lostOnFloor[run.Floor]++;
                                lostTo[(int)run.Battle.Enemy.Kind]++;
                                break;
                            }

                            //Win Rewards : the same as the result page hands out
                            run.AddShards(run.Battle.ShardsEarned());
                            run.ChangeStamina((int)(run.MaxStamina * BattleRules.RecoverAfterWin));
                            if (type == NodeType.Elite && run.RollPercent() < BattleRules.EliteRecruitChance) run.RecruitOne();
                        }
                        else if (type == NodeType.Rest)
                        {
                            run.ChangeStamina((int)(run.MaxStamina * BattleRules.BreatheRecover));
                        }
                        else if (type == NodeType.Shop)
                        {
                            if (run.Stamina < run.MaxStamina * 0.6f && run.SpendShards(BattleRules.TuningPrice))
                                run.ChangeStamina((int)(run.MaxStamina * BattleRules.TuningRecover));
                            if (run.SeatForSale && run.Roster.Count > run.Seats && run.SpendShards(run.SeatPrice))
                            {
                                run.AddSeat();
                                for (int i = 0; i < run.Roster.Count; i++)
                                    if (run.Formation.SeatOf(run.Roster[i]) < 0) run.Formation.AutoSeat(run.Roster[i], run.Seats);
                            }
                        }
                        else if (type == NodeType.EraShift)
                        {
                            run.ChooseEra((run.Era + 1) % EraList.All.Length);
                        }

                        run.FinishBattle();
                        run.Advance();
                    }

                    if (alive && run.RunComplete) won++;
                }

                report += habits[h].PadRight(50) + "RUN WON " + (won * 100 / 200).ToString().PadLeft(3) + "%"
                        + "   LOST ON FLOOR 1/2/3  " + lostOnFloor[1] + " / " + lostOnFloor[2] + " / " + lostOnFloor[3]
                        + "   LOST TO NORMAL/ELITE/BOSS  " + lostTo[0] + " / " + lostTo[1] + " / " + lostTo[2]
                        + "   STAMINA AT BOSS " + (bossCount > 0 ? bossStamina / bossCount : 0) + "%\r\n";
            }
            return report;
        }

        //Play Fight : one whole fight with a fixed habit. Returns true when it was won.
        private static bool PlayFight(RunState run, int habit, Random random)
        {
            BattleState b = run.Battle;
            while (!b.Finished)
            {
                b.AutoPlan();
                for (int beat = 0; beat < BattleRules.BeatsPerRound && !b.Finished; beat++)
                {
                    Choice choice = Choice.Normal;
                    Grade grade = Grade.Good;

                    if (habit >= 1)
                    {
                        grade = Grade.Perfect;
                        if (habit == 2)
                        {
                            int roll = random.Next(100);
                            grade = roll < 40 ? Grade.Perfect : (roll < 90 ? Grade.Good : Grade.Miss);
                        }
                        bool low = run.Stamina < run.MaxStamina * 0.3f;
                        if (b.IsHeavy(beat) && !low) choice = Choice.Boost;
                        if (low && !b.IsHeavy(beat)) choice = Choice.Ease;
                    }
                    if (!b.HasAction(beat)) grade = Grade.None;

                    b.Resolve(beat, choice, grade);
                }
                if (!b.Finished) b.EndRound();
            }
            return b.PlayerWon;
        }

        //After Draw : count frames, save the picture, move on to the next page
        public static void AfterDraw(TacetGame game)
        {
            frames++;
            if (frames < waits[index]) return;

            string path = Path.Combine(outDir, names[index] + ".png");
            using (FileStream stream = File.Create(path))
                Target.SaveAsPng(stream, TacetGame.ScreenW, TacetGame.ScreenH);

            index++;
            frames = 0;
            if (index >= names.Length)
            {
                game.Exit();
                return;
            }
            Open(game, names[index]);
        }

        //Page Open : build a sample run that suits the page, then show it.
        //Names: title guide gallery detail era crossing recruit bandname route view stage score
        //       duel duelcombo duelboss result defeat reward shop event rest curtain curtainwin
        private static void Open(TacetGame game, string name)
        {
            RunState run = SampleRun(game, name);
            game.CurrentRun = run;

            GameScreen screen = new TitleScreen();
            if (name == "guide") screen = new GuideScreen();
            if (name == "gallery") screen = new ConductorSelectScreen(2);
            if (name == "detail") screen = new ConductorDetailScreen(2, new Rectangle(475, 126, 330, 450));
            if (name == "era") screen = new EraChoiceScreen(true);
            if (name == "crossing") screen = new EraChoiceScreen(false);
            if (name == "recruit") screen = new RecruitScreen();
            if (name == "bandname") screen = new BandNameScreen();
            if (name == "route") screen = new RouteScreen();
            if (name == "view") screen = new FormationScreen(false);
            if (name == "stage") screen = new FormationScreen(true);
            if (name == "score") screen = new ScoreScreen();
            if (name == "duel" || name == "duelboss" || name == "duelcombo") screen = new DuelScreen();
            if (name == "result" || name == "defeat") screen = new ResultScreen();
            if (name == "reward") screen = new MotifRewardScreen(false);
            if (name == "shop") screen = new ShopScreen();
            if (name == "event") screen = new EventScreen();
            if (name == "rest") screen = new RestScreen();
            if (name == "curtain" || name == "curtainwin") screen = new CurtainCallScreen(name == "curtainwin");

            game.Screens.ChangeNow(screen);
        }

        //Sample Run : a run part way through floor one, with four musicians and a fight ready
        private static RunState SampleRun(TacetGame game, string name)
        {
            RunState run = new RunState();
            run.Start(ConductorList.All[2], game.Font, PanelStrip.CaptionWrapWidth);
            run.BandName = "THE SILENT CHOIR";

            //Ensemble : three from Siam on stage, one from the Classical era on the bench
            run.ChooseEra(1);
            run.RecruitOne();
            run.RecruitOne();
            run.RecruitOne();
            run.ChooseEra(0);
            run.RecruitOne();
            run.ChooseEra(1);

            if (name == "recruit")
            {
                run.JustJoined.Clear();
                run.JustJoined.Add(run.Roster[0]);
                run.JustJoined.Add(run.Roster[1]);
            }

            //Extras : a few motifs, some shards and one rehearsed player, so the bars have content.
            //The balance check skips these so its numbers stay plain.
            if (name != "sim")
            {
                run.AddMotif(MotifList.Get(MotifId.Fermata));
                run.AddMotif(MotifList.Get(MotifId.Sforzando));
                run.AddMotif(MotifList.Get(MotifId.Resin));
                run.AddShards(120);
                run.Rehearse(run.Roster[0]);
                run.CurrentEvent = EventList.All[1];
                run.BattlesWon = 6;
                run.PerfectsTotal = 23;
                run.BestCombo = 5;
            }

            //Route : keep rolling until four paths come up, so the picture shows a full row
            run.Stage = 5;
            run.StagesThisFloor = 9;
            for (int tries = 0; tries < 60; tries++)
            {
                run.RollOptions();
                if (run.Options.Length == 4) break;
            }

            //Battle
            run.Chosen = new RouteNode();
            run.Chosen.Type = name == "duelboss" ? NodeType.Boss : NodeType.Battle;
            if (name == "reward") run.Chosen.Type = NodeType.Elite;
            run.Chosen.Title = RouteNodeInfo.TitleOf(run.Chosen.Type);
            run.Chosen.Caption = RouteNodeInfo.CaptionOf(run.Chosen.Type);
            run.BeginBattle();
            run.Battle.AutoPlan();
            run.ChangeStamina(-20);

            //Combo Picture : a fight already a few PERFECTs in
            if (name == "duelcombo")
            {
                run.Battle.Combo = 3;
                run.Battle.Line = -35f;
            }

            if (name == "result" || name == "defeat")
            {
                run.Battle.Finished = true;
                run.Battle.PlayerWon = name == "result";
                run.Battle.Line = name == "result" ? 64f : -100f;
                run.Battle.PerfectCount = 3;
            }

            run.RefreshLabels();
            return run;
        }
    }
}
