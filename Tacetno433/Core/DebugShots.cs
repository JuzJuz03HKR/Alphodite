using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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

        //Fake Mouse : DEVELOPER TOOL ONLY.
        //While pictures are taken the window is parked off screen, so the real pointer is
        //nowhere near it. An imaginary pointer is swung about instead, which is enough for
        //the baton and its flicks to show up in the picture.
        public static void FakeMouse(float dt)
        {
            //The four corners of a conductor's 4/4 shape, visited in order: a quick move
            //to the next corner, then a short rest, so the baton makes real strokes
            mouseClock += dt;
            Input.PretendHeld = true;

            //Still Hand : the fermata picture needs the baton held still
            if (index < names.Length && names[index] == "duelfermata")
            {
                Input.MousePos = new Vector2(760f, 470f);
                return;
            }
            int leg = (int)(mouseClock / 0.7f) % 4;
            float t = MathHelper.Clamp((mouseClock % 0.7f) / 0.7f * 2f, 0f, 1f);
            t = t * t * (3f - 2f * t);
            Input.MousePos = Vector2.Lerp(fakeCorners[(leg + 3) % 4], fakeCorners[leg], t);
        }

        private static float mouseClock;
        private static Vector2[] fakeCorners =
        {
            new Vector2(640f, 560f), new Vector2(430f, 420f), new Vector2(850f, 420f), new Vector2(640f, 250f)
        };

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

            //Save Check : write a run and the settings to files in the output folder, read them
            //back, and report every value that did not come back the same
            if (names[0] == "savecheck")
            {
                SaveCheck(game);
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
            string[] habits = { "AUTO PLAN, ALWAYS GOOD PLAY", "AUTO PLAN, NEVER STROKES", "AUTO PLAN, PERFECT BOOST ON HEAVY",
                                "NO PLAN, ALWAYS EASE", "AUTO PLAN, ALWAYS PERFECT (COMBO)", "AUTO PLAN, PERFECT BOOST",
                                "FRONT 4, PERFECT BOOST" };
            Random random = new Random(7);

            for (int k = 0; k < kinds.Length; k++)
            {
                for (int h = 0; h < habits.Length; h++)
                {
                    int wins = 0, roundsTotal = 0, staminaTotal = 0, lowest = 999, finales = 0, beatsTotal = 0;

                    for (int n = 0; n < 300; n++)
                    {
                        RunState run = SampleRun(game, "sim");
                        run.Chosen.Type = kinds[k] == EnemyKind.Boss ? NodeType.Boss : (kinds[k] == EnemyKind.Elite ? NodeType.Elite : NodeType.Battle);
                        run.BeginBattle();
                        run.RestoreAllStamina();
                        BattleState b = run.Battle;

                        while (!b.Finished)
                        {
                            if (h == 3) run.Formation.ClearAll();
                            else if (h == 6) FrontLoad(run);
                            else b.AutoPlan();

                            for (int beat = 0; beat < BattleRules.BeatsPerRound && !b.Finished; beat++)
                            {
                                Choice choice = Choice.Normal;
                                Grade grade = Grade.Good;
                                if (h == 1) grade = Grade.Hesitate;
                                if (h == 2 && b.IsHeavy(beat)) { choice = Choice.Boost; grade = Grade.Perfect; }
                                if (h == 3) choice = Choice.Ease;
                                if (h == 4) grade = Grade.Perfect;
                                if (h >= 5) { choice = Choice.Boost; grade = Grade.Perfect; }

                                int strokes = h == 1 ? 0 : (h == 4 || h == 2 || h >= 5 ? 8 : 5);
                                PlayBeat(b, beat, choice, grade, strokes);
                                beatsTotal++;
                                if (run.Stamina < lowest) lowest = run.Stamina;
                            }

                            //Finale : every habit that strokes at all tries it, and lands it (GOOD is enough)
                            if (b.FinaleOffered && h != 1) b.WinFinale();
                            if (!b.Finished) b.EndRound();
                        }

                        if (b.PlayerWon) wins++;
                        if (b.FinaleWon) finales++;
                        roundsTotal += Math.Min(b.Round, BattleRules.MaxRounds);
                        staminaTotal += run.Stamina;
                    }

                    report += kinds[k].ToString().ToUpper().PadRight(8)
                            + habits[h].PadRight(36)
                            + "WIN " + (wins * 100 / 300).ToString().PadLeft(3) + "%"
                            + "   AVG ROUNDS " + (roundsTotal / 300f).ToString("0.0")
                            + "   AVG BEATS " + (beatsTotal / 300f).ToString("0.0").PadLeft(4)
                            + "   AVG STAMINA LEFT " + (staminaTotal / 300)
                            + "   LOWEST " + lowest
                            + "   FINALE " + (finales * 100 / 300) + "%\r\n";
                }
                report += "\r\n";
            }

            report += SimulateRuns(game, random);
            File.WriteAllText(Path.Combine(outDir, "simulate.txt"), report);
        }

        //Save Check : a sample run half way into a fight, saved and loaded again. The real save
        //folder is never used, the files go to the output folder instead.
        private static void SaveCheck(TacetGame game)
        {
            string report = "TACET SAVE CHECK\r\n";
            SaveFile.Enabled = true;
            SaveFile.FolderOverride = outDir;

            RunState run = SampleRun(game, "savecheck");
            run.Floor = 2;
            run.Rehearse(run.Roster[1]);
            run.RecordStop(NodeType.Battle);
            run.RecordStop(NodeType.Shop);
            run.Chosen = run.Options[1];
            run.CurrentEvent = EventList.All[2];
            run.Formation.Plan[run.Formation.SeatOf(run.Roster[0]), 3] = true;
            SaveFile.SaveRun(run);

            RunState back = SaveFile.LoadRun(game);
            if (back == null)
            {
                report += "FAIL  the run did not load at all\r\n";
            }
            else
            {
                report += Same("conductor", run.Conductor.Name, back.Conductor.Name);
                report += Same("band", run.BandName, back.BandName);
                report += Same("floor / stage", run.Floor + "/" + run.Stage + "/" + run.StagesThisFloor, back.Floor + "/" + back.Stage + "/" + back.StagesThisFloor);
                report += Same("era", run.Era.ToString(), back.Era.ToString());
                report += Same("shards / seats", run.Shards + "/" + run.Seats + "/" + run.SeatsBoughtThisFloor, back.Shards + "/" + back.Seats + "/" + back.SeatsBoughtThisFloor);
                report += Same("stamina", run.StaminaValue, back.StaminaValue);
                report += Same("record", run.BattlesWon + "/" + run.PerfectsTotal + "/" + run.BestCombo, back.BattlesWon + "/" + back.PerfectsTotal + "/" + back.BestCombo);
                report += Same("roster", Names(run), Names(back));
                report += Same("rehearsed", run.Roster[0].Rehearsed + "/" + run.Roster[1].Rehearsed, back.Roster[0].Rehearsed + "/" + back.Roster[1].Rehearsed);
                report += Same("seats and plan", Plan(run), Plan(back));
                report += Same("motifs", run.Motifs.Count + (run.Motifs.Count > 0 ? run.Motifs[0].Name : ""), back.Motifs.Count + (back.Motifs.Count > 0 ? back.Motifs[0].Name : ""));
                report += Same("journey", run.Journey.Count + "/" + run.Journey[run.Journey.Count - 1].Type, back.Journey.Count + "/" + back.Journey[back.Journey.Count - 1].Type);
                report += Same("options", Options(run), Options(back));
                report += Same("chosen", Array.IndexOf(run.Options, run.Chosen).ToString(), Array.IndexOf(back.Options, back.Chosen).ToString());
                report += Same("enemy", run.Battle.Enemy.Name, SaveFile.LoadedEnemy != null ? SaveFile.LoadedEnemy.Name : "none");
                report += Same("event", run.CurrentEvent.Title, SaveFile.LoadedEvent != null ? SaveFile.LoadedEvent.Title : "none");
                report += Same("summary", "THE INFERNO  /  FLOOR 2  /  STAGE 5", SaveFile.RunSummary());
            }

            //Settings : change them, save, scramble, load, compare, then put the defaults back
            Settings.Master = 0.35f; Settings.Sfx = 0.6f; Settings.Music = 0.1f; Settings.Language = 1; Settings.SetTiming(0.045f);
            SaveFile.SaveSettings();
            Settings.Master = 1f; Settings.Sfx = 1f; Settings.Music = 1f; Settings.Language = 0; Settings.SetTiming(0f);
            SaveFile.LoadSettings();
            report += Same("settings", "0.35/0.6/0.1/1/" + Settings.TimingLabel(0.045f),
                           Settings.Master + "/" + Settings.Sfx + "/" + Settings.Music + "/" + Settings.Language + "/" + Settings.TimingLabel(Settings.TimingOffset));
            Settings.Master = 0.8f; Settings.Sfx = 0.8f; Settings.Music = 0.55f; Settings.Language = 0; Settings.SetTiming(0f);

            SaveFile.DeleteRun();
            report += Same("delete", "False", SaveFile.HasRun.ToString());

            SaveFile.FolderOverride = "";
            SaveFile.Enabled = false;
            File.WriteAllText(Path.Combine(outDir, "savecheck.txt"), report);
        }

        private static string Same(string what, string saved, string loaded)
        {
            return (saved == loaded ? "PASS  " : "FAIL  ") + what.PadRight(16) + saved + "   ->   " + loaded + "\r\n";
        }

        private static string Names(RunState run)
        {
            string s = "";
            for (int i = 0; i < run.Roster.Count; i++) s += run.Roster[i].Name + " ";
            return s;
        }

        private static string Plan(RunState run)
        {
            string s = "";
            for (int seat = 0; seat < StageLayout.SeatCount; seat++)
            {
                s += run.Formation.Seated[seat] == null ? "-" : run.Formation.Seated[seat].Name.Substring(0, 1);
                for (int b = 0; b < BattleRules.BeatsPerRound; b++) s += run.Formation.Plan[seat, b] ? "1" : "0";
                s += " ";
            }
            return s;
        }

        private static string Options(RunState run)
        {
            string s = "";
            for (int i = 0; i < run.Options.Length; i++) s += run.Options[i].Type + " ";
            return s;
        }

        //Whole Runs : plays complete runs from the first era choice to the end, 200 per habit,
        //with THE APPRENTICE. Paths are picked at random. Rests breathe, shops buy stamina when
        //the band is low and a seat when someone waits on the bench, events are skipped.
        //After a win the sim takes a motif at random from the same offer the reward page makes
        //(round 9 : before that motifs were skipped, so late runs looked harder than they play).
        private static string SimulateRuns(TacetGame game, Random random)
        {
            string[] habits = { "GOOD PLAY EVERY BEAT, NEVER BOOSTS", "SKILLED  (PERFECT, BOOST, EASE LIGHT WHEN LOW)",
                                "AVERAGE  (40% PERFECT, 10% MISS, SAME CHOICES)", "STRONG   (70% PERFECT, 5% MISS, SAME CHOICES)",
                                "FRONT-LOAD (ALL ON BEATS 1-4, 70% PERFECT)" };
            string report = "WHOLE RUNS  (" + BattleRules.FloorsPerRun + " floors, 200 runs per line, conductor THE APPRENTICE, motifs taken)\r\n";

            for (int h = 0; h < habits.Length; h++)
            {
                int won = 0;
                int[] lostOnFloor = new int[BattleRules.FloorsPerRun + 1];
                int[] lostTo = new int[3];
                int bossStamina = 0, bossCount = 0, outOfBreath = 0;
                fightCount = 0;
                fightBeats = 0;

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
                                if (run.Battle.Collapsed) outOfBreath++;
                                break;
                            }

                            //Win Rewards : the same as the result page hands out
                            run.AddShards(run.Battle.ShardsEarned());
                            run.ChangeStamina((int)(run.MaxStamina * BattleRules.RecoverAfterWin));
                            if (type == NodeType.Elite && run.RollPercent() < BattleRules.EliteRecruitChance) run.RecruitOne();
                            TakeMotif(run, type, random);
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
                        + "   OUT OF BREATH " + outOfBreath
                        + "   STAMINA AT BOSS " + (bossCount > 0 ? bossStamina / bossCount : 0) + "%"
                        + "   BEATS PER FIGHT " + (fightCount > 0 ? fightBeats / (float)fightCount : 0f).ToString("0.0") + "\r\n";
            }
            return report;
        }

        //Fight Counters : how many beats the whole-run fights lasted, for the report
        private static int fightCount;
        private static int fightBeats;

        //Front Load : everyone seated plays the first four beats and rests after. Players found
        //this plan on 25 Sep and won in two to four beats, so the sim keeps checking it.
        private static void FrontLoad(RunState run)
        {
            Formation f = run.Formation;
            f.ClearAll();
            for (int s = 0; s < StageLayout.SeatCount; s++)
                if (f.Seated[s] != null)
                    for (int beat = 0; beat < 4; beat++) f.Plan[s, beat] = true;
        }

        //Take Motif : the reward page's offer after a win (elites and bosses always offer, normal
        //fights sometimes), and the sim picks one of the cards at random
        private static void TakeMotif(RunState run, NodeType type, Random random)
        {
            bool lastBoss = type == NodeType.Boss && run.Floor >= BattleRules.FloorsPerRun;
            if (lastBoss) return;
            if (type == NodeType.Battle && run.RollPercent() >= BattleRules.MotifChanceNormal) return;

            int minRarity = type == NodeType.Boss ? 3 : (type == NodeType.Elite ? 2 : 1);
            Motif[] offer = MotifList.Roll(run.Motifs, BattleRules.MotifChoices, minRarity, run.Floor, run.Rng);
            if (offer.Length > 0) run.AddMotif(offer[random.Next(offer.Length)]);
        }

        //Play Beat : one beat the way the duel plays it. A roll gets its strokes counted first,
        //a pair gets its flick back with the same grade as its first note. Strokes below zero
        //mean the flick back slipped (a MISS) on an ordinary beat.
        private static void PlayBeat(BattleState b, int beat, Choice choice, Grade grade, int strokes)
        {
            Grade flick = strokes < 0 ? Grade.Miss : grade;
            if (strokes < 0) strokes = 5;
            if (!b.HasAction(beat)) grade = Grade.None;
            if (b.IsTremolo(beat))
            {
                b.RollStrokes = strokes;
                grade = BattleState.RollGrade(strokes);
                choice = Choice.Normal;
            }

            //Fermata : a steady hand holds it to the end, a shaky one lets go part way
            if (b.IsFermata(beat)) b.HoldFraction = grade == Grade.Hesitate || grade == Grade.Miss ? 0f : (flick == Grade.Miss ? 0.5f : (grade == Grade.Perfect ? 1f : 0.8f));

            b.Resolve(beat, choice, grade);
            if (b.EnemyDouble[beat] && !b.Finished) b.ResolveGrace(beat, grade == Grade.None ? Grade.Hesitate : flick);
        }

        //Play Fight : one whole fight with a fixed habit. Returns true when it was won.
        private static bool PlayFight(RunState run, int habit, Random random)
        {
            BattleState b = run.Battle;
            fightCount++;
            while (!b.Finished)
            {
                if (habit == 4) FrontLoad(run); else b.AutoPlan();
                for (int beat = 0; beat < BattleRules.BeatsPerRound && !b.Finished; beat++)
                {
                    Choice choice = Choice.Normal;
                    Grade grade = Grade.Good;
                    fightBeats++;

                    if (habit >= 1)
                    {
                        grade = Grade.Perfect;
                        if (habit == 2)
                        {
                            int roll = random.Next(100);
                            grade = roll < 40 ? Grade.Perfect : (roll < 90 ? Grade.Good : Grade.Miss);
                        }
                        if (habit >= 3)
                        {
                            int roll = random.Next(100);
                            grade = roll < 70 ? Grade.Perfect : (roll < 95 ? Grade.Good : Grade.Miss);
                        }
                        //BOOST costs nothing extra (round 8), so a good player swings big on every
                        //beat, and only eases the light ones to catch breath when it runs low
                        bool low = run.Stamina < run.MaxStamina * 0.3f;
                        choice = Choice.Boost;
                        if (low && !b.IsHeavy(beat)) choice = Choice.Ease;
                    }
                    int strokes = habit == 0 ? 5 : (habit == 1 ? 8 : 5 + random.Next(4));
                    if (habit >= 2 && b.EnemyDouble[beat] && random.Next(100) < (habit == 2 ? 30 : 10)) strokes = -1;   // the flick back slips
                    PlayBeat(b, beat, choice, grade, strokes);
                }

                //Finale : steady players land it, average ones about half the time
                if (b.FinaleOffered)
                {
                    if (habit != 2 || random.Next(2) == 0) b.WinFinale();
                    else b.FailFinale();
                }
                if (!b.Finished) b.EndRound();
            }
            return b.PlayerWon;
        }

        //After Draw : count frames, save the picture, move on to the next page
        public static void AfterDraw(TacetGame game)
        {
            frames++;

            //Pretend Keys : a few pages need one key pressed shortly before their picture
            Input.PretendPress = Keys.None;
            if (names[index] == "duelcutin" && frames == waits[index] - 22) Input.PretendPress = Keys.Space;
            if ((names[index] == "duelpause" || names[index] == "pause") && frames == waits[index] - 30) Input.PretendPress = Keys.Escape;
            if (names[index] == "titlequit" && frames == waits[index] - 10) Input.PretendPress = Keys.Escape;
            if (names[index] == "shopleave" && frames == waits[index] - 10) Input.PretendPress = Keys.Enter;
            if (names[index] == "guide4" && (frames == 10 || frames == 20 || frames == 30)) Input.PretendPress = Keys.Right;
            if (names[index] == "guide5" && (frames == 10 || frames == 20 || frames == 30 || frames == 40)) Input.PretendPress = Keys.Right;

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
        //Names: title titlecontinue titlequit guide guide4 guide5 settings calibrate gallery detail chapter era
        //       crossing recruit bandname route pause view stage score scoretrait scorepairs bargain
        //       duel duelcombo duelboss duelcutin duelpause dueldouble dueltremolo duelfermata duelfire finale
        //       result defeat reward shop shopleave event rest curtain curtainwin
        private static void Open(TacetGame game, string name)
        {
            RunState run = SampleRun(game, name);
            game.CurrentRun = run;
            game.Pause.Open = false;
            SaveFile.PretendRun = name == "titlecontinue";

            GameScreen screen = new TitleScreen();
            if (name.StartsWith("guide")) screen = new GuideScreen();
            if (name.StartsWith("tutorial")) screen = new TutorialScreen();
            if (name == "settings" || name == "calibrate") screen = new SettingsScreen();
            if (name == "gallery") screen = new ConductorSelectScreen(2);
            if (name == "detail") screen = new ConductorDetailScreen(2, new Rectangle(475, 126, 330, 450));
            if (name == "chapter") screen = new ChapterScreen();
            if (name == "era") screen = new EraChoiceScreen(true);
            if (name == "crossing") screen = new EraChoiceScreen(false);
            if (name == "recruit") screen = new RecruitScreen();
            if (name == "bandname") screen = new BandNameScreen();
            if (name == "route" || name == "pause") screen = new RouteScreen();
            if (name == "view") screen = new FormationScreen(false);
            if (name == "stage") screen = new FormationScreen(true);
            if (name == "score" || name == "scoretrait" || name == "bargain" || name == "scorepairs") screen = new ScoreScreen();
            if (name.StartsWith("duel") || name == "finale") screen = new DuelScreen();
            if (name == "result" || name == "defeat") screen = new ResultScreen();
            if (name == "reward") screen = new MotifRewardScreen(false);
            if (name == "shop" || name == "shopleave") screen = new ShopScreen();
            if (name == "event") screen = new EventScreen();
            if (name == "rest") screen = new RestScreen();
            if (name == "curtain" || name == "curtainwin") screen = new CurtainCallScreen(name == "curtainwin");

            game.Screens.ChangeNow(screen);

            //Picture Hooks : start the moment the picture is about
            if (name == "calibrate") ((SettingsScreen)screen).BeginTest();
            if (name == "finale") ((DuelScreen)screen).BeginFinaleForPicture();
            if (name == "dueltremolo") ((DuelScreen)screen).JumpForPicture(BattleRules.BeatsPerRound - 1);
            if (name == "dueldouble") ((DuelScreen)screen).JumpForPicture(FirstPair(run.Battle));
            if (name == "duelfermata") ((DuelScreen)screen).HoldForPicture();
            if (name == "tutorialsize") ((TutorialScreen)screen).JumpForPicture(3, 0f);
            if (name == "tutorialtiming") ((TutorialScreen)screen).JumpForPicture(4, 5.6f);
            if (name == "tutorialloud") ((TutorialScreen)screen).JumpForPicture(6, 6.6f);
            if (name == "tutorialbreath") ((TutorialScreen)screen).JumpForPicture(7, 7.6f);
            if (name == "tutorialroll") ((TutorialScreen)screen).JumpForPicture(8, 5.8f);
            if (name == "tutorialready") ((TutorialScreen)screen).JumpForPicture(11, 0f);
        }

        //First Pair : the first beat of the round that TACET plays as a pair, for the picture
        private static int FirstPair(BattleState b)
        {
            for (int n = 0; n < BattleRules.BeatsPerRound; n++)
                if (b.EnemyDouble[n]) return n;
            return 0;
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
                run.AddMotif(MotifList.Get(MotifId.BreathMark));
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
            if (name == "reward" || name == "dueltremolo" || name == "scorepairs") run.Chosen.Type = NodeType.Elite;
            if (name == "dueldouble" || name == "scorepairs") run.Floor = 3;          // pairs from floor two, more on the last
            if (name == "duelfermata") run.Floor = 2;                                 // held notes from floor two
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

            //Fire Picture : the band is on fire
            if (name == "duelfire")
            {
                run.Battle.Combo = 6;
                run.Battle.FortissimoLeft = BattleRules.FortissimoBeats;
                run.Battle.Line = 20f;
            }

            //Finale Picture : far enough ahead to finish it
            if (name == "finale") run.Battle.Line = 86f;


            //Trait Pictures : an ordinary enemy on floor two shows its trait, the devil makes its offer
            Random pick = new Random(5);
            if (name == "scoretrait" || name == "duelboss")
            {
                run.Floor = 2;
                run.Battle = new BattleState(run, name == "duelboss" ? EnemyList.All[7] : EnemyList.All[0], pick);
                run.Battle.AutoPlan();
            }
            if (name == "bargain")
            {
                run.Battle = new BattleState(run, EnemyList.All[8], pick);
                run.Battle.AutoPlan();
                run.Battle.EndRound();
            }

            //Signature Picture : the recipe is full, so SPACE (pressed by the tool) lets it loose
            if (name == "duelcutin")
                for (int f = 0; f < run.Battle.Notes.Length; f++) run.Battle.Notes[f] = run.Battle.NeedFor(f);

            if (name == "result" || name == "defeat")
            {
                run.Battle.Finished = true;
                run.Battle.PlayerWon = name == "result";
                run.Battle.Line = name == "result" ? 64f : -100f;
                run.Battle.PerfectCount = 3;

                //Performance Sheet : two rounds and a half of made up beats
                run.Battle.Round = 3;
                for (int r = 0; r < 3; r++)
                    for (int b = 0; b < BattleRules.BeatsPerRound; b++)
                    {
                        if (r == 2 && b > 4) continue;
                        BeatResult mark = run.Battle.Sheet[r][b];
                        int roll = pick.Next(10);
                        mark.Done = true;
                        mark.Grade = roll < 1 ? Grade.None : (roll < 5 ? Grade.Perfect : (roll < 8 ? Grade.Good : (roll < 9 ? Grade.Miss : Grade.Hesitate)));
                        mark.PlayerChoice = (Choice)pick.Next(3);
                        mark.Push = pick.Next(3) == 0 ? -6 : 9;
                        mark.Signature = r == 1 && b == 6;
                    }
            }

            //Journey : a floor and a half of places, for the curtain call
            if (name == "curtain" || name == "curtainwin")
            {
                NodeType[] path = { NodeType.Battle, NodeType.Event, NodeType.Battle, NodeType.Shop, NodeType.Elite,
                                    NodeType.Rest, NodeType.Battle, NodeType.Boss };
                for (int i = 0; i < path.Length; i++) run.RecordStop(path[i]);
                run.Floor = 2;
                run.RecordStop(NodeType.Battle);
                run.RecordStop(NodeType.EraShift);
                run.RecordStop(NodeType.Elite);
                if (name == "curtain") run.MarkLastStopLost();
                run.Floor = 1;
            }

            run.RefreshLabels();
            return run;
        }
    }
}
