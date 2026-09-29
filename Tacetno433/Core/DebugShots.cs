using System;
using System.Collections.Generic;
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
    public static partial class DebugShots
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

            //Balance Audit : whole runs split by conductor, motif, enemy and era (DebugShots.Audit.cs)
            if (names[0] == "audit")
            {
                Audit(game);
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
        //Round 12 : there is no plan. A habit is how the player sizes strokes (see Habit), and
        //"READS THE MARKS" answers p small, mf middle and f big, which is what the game teaches.
        //Round 15 : the size must match the note (the wrong size plays at half), and the whole band
        //plays every note, so the fixed-size habits show what not reading costs.
        private static void Simulate(TacetGame game)
        {
            string report = "TACET BALANCE CHECK  (floor 1, 300 fights per line)\r\n"
                          + "AUDIO LOADED  SFX " + Audio.SoundBank.LoadedSfx + "  MUSIC " + Audio.SoundBank.LoadedMusic + "\r\n\r\n";
            EnemyKind[] kinds = { EnemyKind.Normal, EnemyKind.Elite, EnemyKind.Boss };
            Habit[] habits =
            {
                new Habit("MIDDLE STROKE EVERY BEAT, GOOD",   1, false, 0, 100, 0),
                new Habit("NEVER STROKES",                     -2, true, 0, 0, 0),
                new Habit("READS THE MARKS, GOOD",            -1, true, 0, 100, 0),
                new Habit("SMALL STROKE EVERY BEAT, PERFECT",  0, false, 100, 0, 0),
                new Habit("BIG STROKE EVERY NOTE, PERFECT",    2, true, 100, 0, 0),
                new Habit("READS THE MARKS, PERFECT",         -1, true, 100, 0, 0),
                new Habit("READS, PERFECT, NEVER RESTS",      -1, false, 100, 0, 0),
                new Habit("NEWCOMER (20% PERFECT, 25% MISS)", -1, true, 20, 55, 20) { TempoSkill = 0.8f, NoSignature = true },
            };
            Random random = new Random(7);

            for (int k = 0; k < kinds.Length; k++)
            {
                for (int h = 0; h < habits.Length; h++)
                {
                    int wins = 0, roundsTotal = 0, staminaTotal = 0, lowest = 999, finales = 0, beatsTotal = 0;
                    int signaturesBefore = simSignatures;

                    for (int n = 0; n < 300; n++)
                    {
                        RunState run = SampleRun(game, "sim");
                        run.Chosen.Type = kinds[k] == EnemyKind.Boss ? NodeType.Boss : (kinds[k] == EnemyKind.Elite ? NodeType.Elite : NodeType.Battle);
                        run.BeginBattle();
                        run.RestoreAllStamina();
                        BattleState b = run.Battle;

                        while (!b.Finished)
                        {
                            //REPEATS : the phrase is played once, twice or three times
                            for (int g = 0; g < BattleRules.BeatsPerRound * b.Passes && !b.Finished; g++)
                            {
                                int beat = g % BattleRules.BeatsPerRound;
                                if (beat == 0) b.BeginPass(g / BattleRules.BeatsPerRound);
                                PlayHabitBeat(run, b, beat, habits[h], random);
                                beatsTotal++;
                                if (run.Stamina < lowest) lowest = run.Stamina;
                            }

                            //Finale : every habit that strokes at all tries it, and lands it (GOOD is enough)
                            if (b.FinaleOffered && habits[h].Size != -2) b.WinFinale();
                            if (!b.Finished) b.EndRound();
                        }

                        if (b.PlayerWon) wins++;
                        if (b.FinaleWon) finales++;
                        roundsTotal += Math.Min(b.Round, BattleRules.MaxRounds);
                        staminaTotal += run.Stamina;
                    }

                    report += kinds[k].ToString().ToUpper().PadRight(8)
                            + habits[h].Name.PadRight(36)
                            + "WIN " + (wins * 100 / 300).ToString().PadLeft(3) + "%"
                            + "   AVG ROUNDS " + (roundsTotal / 300f).ToString("0.0")
                            + "   AVG BEATS " + (beatsTotal / 300f).ToString("0.0").PadLeft(4)
                            + "   AVG STAMINA LEFT " + (staminaTotal / 300)
                            + "   LOWEST " + lowest
                            + "   FINALE " + (finales * 100 / 300) + "%"
                            + "   SIGNATURES " + ((simSignatures - signaturesBefore) / 300f).ToString("0.0") + "\r\n";
                }
                report += "\r\n";
            }

            report += SimulateRuns(game, random);
            File.WriteAllText(Path.Combine(outDir, "simulate.txt"), report);
        }

        //Habit : how a simulated player conducts (round 12).
        //   Size     -1 reads the marks (p small, mf middle, f big), 0 1 2 always that size, -2 never strokes
        //   Rest     lets TACET's silent beats pass to breathe, instead of stroking them too
        //   Perfect, Good   percent of strokes of each grade at 112 BPM, the rest are MISSes
        //   WrongSize       percent of strokes that come out a size off (a shaky hand), half as many
        //                   in the gentle fights where TACET only plays f and p
        //   TempoSkill      (round 15) how much the tempo matters to this player : for every BPM
        //                   under 112 this many percent of their strokes move from MISS and GOOD
        //                   towards PERFECT, and the other way above it. An assumption, not a
        //                   measurement : nobody has played the tempo curve with a real mouse yet.
        //   NoSignature     never presses SPACE (a player who has not found it yet)
        private class Habit
        {
            public string Name;
            public int Size, Perfect, Good, WrongSize;
            public bool Rest, NoSignature;
            public float TempoSkill;

            public Habit(string name, int size, bool rest, int perfect, int good, int wrongSize)
            {
                Name = name;
                Size = size;
                Rest = rest;
                Perfect = perfect;
                Good = good;
                WrongSize = wrongSize;
            }
        }

        //Run Habits : the whole run players, in the order the audit names them (GOOD, SKILLED, AVERAGE, STRONG)
        private static Habit[] runHabits =
        {
            new Habit("GOOD PLAY, MIDDLE STROKE EVERY BEAT",           1, false, 0, 100, 0),
            new Habit("SKILLED  (READS THE MARKS, PERFECT)",           -1, true, 100, 0, 0),
            new Habit("AVERAGE  (READS, 40% PERFECT, 10% MISS)",      -1, true, 40, 50, 15) { TempoSkill = 0.5f },
            new Habit("STRONG   (READS, 70% PERFECT, 5% MISS)",        -1, true, 70, 25, 5) { TempoSkill = 0.3f },
            new Habit("BIG EVERY NOTE (70% PERFECT, 5% MISS)",         2, true, 70, 25, 0),
            new Habit("NEWCOMER (READS, 20% PERFECT, 25% MISS)",       -1, true, 20, 55, 20) { TempoSkill = 0.8f, NoSignature = true },
        };

        //Read Mark : the stroke the game teaches for a note. p small, mf middle, f big. A hidden
        //??? note could be anything, so it is met with the whole band, and so are a roll and a held note.
        private static Choice ReadMark(BattleState b, int beat)
        {
            if (b.IsTremolo(beat) || b.IsFermata(beat) || b.EnemyHidden[beat]) return Choice.Boost;
            return b.ShownChoice[beat];
        }

        //Sim Signature : the simulated players press SPACE as soon as the recipe is full, on the
        //next beat that has a note (round 13, before that the sim never used the signature).
        //Set to false in a scratch copy to measure what the signatures are worth.
        private static bool SimSignature = true;
        private static int simSignatures;                     // how many were let loose, for the reports

        //Play Habit Beat : one beat the way this habit plays it
        private static void PlayHabitBeat(RunState run, BattleState b, int beat, Habit habit, Random random)
        {
            if (habit.Size == -2)
            {
                PlayBeat(b, beat, Choice.Normal, Grade.Hesitate, 0, true);
                return;
            }

            //SIGNATURE : let it loose on the next beat with a note
            if (SimSignature && !habit.NoSignature && b.SignatureReady && !b.SignatureOn && !b.IsSilent(beat))
            {
                b.StartSignature();
                simSignatures++;
            }

            //Grade : the habit's own shares at 112 BPM, moved by the tempo (TempoSkill)
            float shift = (112 - b.Tempo) * habit.TempoSkill;
            float perfect = Math.Min(100f, Math.Max(0f, habit.Perfect + shift));
            float miss = Math.Min(100f, Math.Max(0f, 100 - habit.Perfect - habit.Good - shift * 0.5f));
            int roll = random.Next(100);
            Grade grade = roll < perfect ? Grade.Perfect : (roll < 100 - miss ? Grade.Good : Grade.Miss);
            if (!b.IsTremolo(beat)) grade = b.SignatureGrade(grade);                         // CLOCKWORK

            //Size : read, or fixed, sometimes a size off
            Choice choice = habit.Size == 0 ? Choice.Ease : (habit.Size == 1 ? Choice.Normal : (habit.Size == 2 ? Choice.Boost : ReadMark(b, beat)));
            int wrong = b.Gentle ? habit.WrongSize / 2 : habit.WrongSize;
            if (random.Next(100) < wrong) choice = choice == Choice.Normal ? (random.Next(2) == 0 ? Choice.Ease : Choice.Boost) : Choice.Normal;

            //Silent Beat : a reader lets it pass, a fixed habit swings its usual size anyway (a small
            //one is still a rest there, SOFT REST). A reader who never rests takes the free hit
            //with a middle stroke, the smallest that is not a rest.
            bool rest = habit.Rest && b.IsSilent(beat);
            if (habit.Size == -1 && b.IsSilent(beat) && !habit.Rest) choice = Choice.Normal;

            int strokes = habit.Perfect >= 100 ? 8 : (habit.Perfect == 0 ? 5 : 5 + random.Next(4));
            int slip = habit.Perfect >= 100 ? 0 : (habit.Perfect >= 70 ? 10 : 30);
            if (b.EnemyDouble[beat] && random.Next(100) < slip) strokes = -1;                 // the flick back slips
            PlayBeat(b, beat, choice, grade, strokes, rest);
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
                report += Same("seats", Seats(run), Seats(back));
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

        private static string Seats(RunState run)
        {
            string s = "";
            for (int seat = 0; seat < StageLayout.SeatCount; seat++)
                s += (run.Formation.Seated[seat] == null ? "-" : run.Formation.Seated[seat].Name.Substring(0, 2)) + " ";
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
            string report = "WHOLE RUNS  (" + BattleRules.FloorsPerRun + " floors, 200 runs per line, conductor THE APPRENTICE, motifs taken)\r\n";

            for (int h = 0; h < runHabits.Length; h++)
            {
                RunRecord rec = new RunRecord();
                for (int n = 0; n < 200; n++)
                    PlayRun(game, ConductorList.All[0], h, null, true, random, rec);

                report += runHabits[h].Name.PadRight(50) + "RUN WON " + (rec.Won * 100 / rec.Runs).ToString().PadLeft(3) + "%"
                        + "   LOST ON FLOOR 1/2/3  " + rec.LostOnFloor[1] + " / " + rec.LostOnFloor[2] + " / " + rec.LostOnFloor[3]
                        + "   LOST TO NORMAL/ELITE/BOSS  " + rec.LostTo[0] + " / " + rec.LostTo[1] + " / " + rec.LostTo[2]
                        + "   OUT OF BREATH " + rec.OutOfBreath
                        + "   STAMINA AT BOSS " + (rec.BossCount > 0 ? rec.BossStamina / rec.BossCount : 0) + "%"
                        + "   BEATS PER FIGHT " + (rec.Fights > 0 ? rec.Beats / (float)rec.Fights : 0f).ToString("0.0")
                        + "   SIGNATURES PER FIGHT " + (rec.Fights > 0 ? rec.Signatures / (float)rec.Fights : 0f).ToString("0.0") + "\r\n";
            }
            return report;
        }

        //Run Record : what a batch of simulated runs added up to, for the reports
        private class RunRecord
        {
            public int Runs, Won, OutOfBreath, BossStamina, BossCount, Fights, Beats, Signatures;
            public int[] LostOnFloor = new int[BattleRules.FloorsPerRun + 1];
            public int[] LostTo = new int[3];                         // by EnemyKind
            public int[] EraRuns = new int[3], EraWon = new int[3];   // by the first era chosen
            public Dictionary<string, int[]> Enemies = new Dictionary<string, int[]>();   // "NAME (floor)" -> fights, losses
        }

        //Play Run : one whole run with a fixed habit, from the first era choice to the end.
        //startMotif (or null) is owned from the start, takeMotifs picks one after every win.
        //Everything that happened is added to rec. Returns true when the run was won.
        private static bool PlayRun(TacetGame game, Conductor conductor, int habit, Motif startMotif, bool takeMotifs, Random random, RunRecord rec)
        {
            RunState run = new RunState();
            run.Start(conductor, game.StoryFont, RouteNodeInfo.CaptionWrapWidth);
            run.BandName = "SIM";
            if (startMotif != null) run.AddMotif(startMotif);
            bool alive = true;
            int firstEra = -1;

            for (int guard = 0; guard < 200 && alive && !run.RunComplete; guard++)
            {
                if (run.AtFloorOpening)
                {
                    int era = random.Next(EraList.All.Length);
                    if (firstEra < 0) firstEra = era;
                    run.ChooseEra(era);
                    run.RecruitAtOpening();
                    run.LeaveOpening();
                    continue;
                }

                run.Chosen = run.Options[random.Next(run.Options.Length)];
                NodeType type = run.Chosen.Type;

                if (type == NodeType.Battle || type == NodeType.Elite || type == NodeType.Boss)
                {
                    run.BeginBattle();
                    if (type == NodeType.Boss) { rec.BossStamina += run.Stamina * 100 / run.MaxStamina; rec.BossCount++; }
                    string key = run.Battle.Enemy.Name + " (" + run.Floor + ")";
                    if (!rec.Enemies.ContainsKey(key)) rec.Enemies[key] = new int[2];
                    rec.Enemies[key][0]++;

                    if (!PlayFight(run, habit, random, rec))
                    {
                        alive = false;
                        rec.Enemies[key][1]++;
                        rec.LostOnFloor[run.Floor]++;
                        rec.LostTo[(int)run.Battle.Enemy.Kind]++;
                        if (run.Battle.Collapsed) rec.OutOfBreath++;
                        break;
                    }

                    //Win Rewards : the same as the result page hands out
                    run.AddShards(run.Battle.ShardsEarned());
                    float recover = BattleRules.RecoverAfterWin;
                    if (run.Has(MotifId.Encore)) recover += BattleRules.EncoreRecover;         // ENCORE
                    run.ChangeStamina((int)(run.MaxStamina * recover));
                    if (type == NodeType.Elite && run.RollPercent() < BattleRules.EliteRecruitChance) run.RecruitOne();
                    if (takeMotifs) TakeMotif(run, type, random);
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

            bool won = alive && run.RunComplete;
            rec.Runs++;
            if (won) rec.Won++;
            if (firstEra >= 0) { rec.EraRuns[firstEra]++; if (won) rec.EraWon[firstEra]++; }
            return won;
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
        //mean the flick back slipped (a MISS). A silent beat with rest set is let pass.
        private static void PlayBeat(BattleState b, int beat, Choice choice, Grade grade, int strokes, bool rest)
        {
            if (b.IsSilent(beat) && (rest || grade == Grade.Hesitate))
            {
                b.Resolve(beat, Choice.Normal, Grade.None);                             // a rest
                return;
            }

            Grade flick = strokes < 0 ? Grade.Miss : grade;
            if (strokes < 0) strokes = 5;
            if (b.IsTremolo(beat) && grade != Grade.Hesitate)
            {
                b.RollStrokes = strokes;
                grade = BattleState.RollGrade(strokes);
                choice = Choice.Boost;                                                  // the whole band rolls
            }

            //Fermata : a steady hand holds it to the end, a shaky one lets go part way
            if (b.IsFermata(beat)) b.HoldFraction = grade == Grade.Hesitate || grade == Grade.Miss ? 0f : (flick == Grade.Miss ? 0.5f : (grade == Grade.Perfect ? 1f : 0.8f));

            BeatResult r = b.Resolve(beat, choice, grade);
            b.AddNotes(r);                                                                  // SIGNATURE notes, as the duel gives them
            if (b.EnemyDouble[beat] && !b.Finished) b.ResolveGrace(beat, grade == Grade.Hesitate ? Grade.Hesitate : flick);
        }

        //Play Fight : one whole fight with a fixed habit. Returns true when it was won.
        private static bool PlayFight(RunState run, int habit, Random random, RunRecord rec)
        {
            BattleState b = run.Battle;
            rec.Fights++;
            int signaturesBefore = simSignatures;
            while (!b.Finished)
            {
                for (int g = 0; g < BattleRules.BeatsPerRound * b.Passes && !b.Finished; g++)
                {
                    //REPEATS : the phrase again, with TACET's marks for this time through
                    int beat = g % BattleRules.BeatsPerRound;
                    if (beat == 0) b.BeginPass(g / BattleRules.BeatsPerRound);
                    rec.Beats++;
                    PlayHabitBeat(run, b, beat, runHabits[habit], random);
                }

                //Finale : steady players land it, average ones and newcomers about half the time
                if (b.FinaleOffered)
                {
                    if ((habit != 2 && habit != 5) || random.Next(2) == 0) b.WinFinale();
                    else b.FailFinale();
                }
                if (!b.Finished) b.EndRound();
            }
            rec.Signatures += simSignatures - signaturesBefore;
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
            if (names[index] == "guide2" && frames == 10) Input.PretendPress = Keys.Right;
            if (names[index] == "guide3" && (frames == 10 || frames == 20)) Input.PretendPress = Keys.Right;
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

        //Conductor Shot : "detail3", "duelc3" or "duels3", a page name and one digit (not "duelcombo")
        private static bool IsConductorShot(string name, string page)
        {
            return name.Length == page.Length + 1 && name.StartsWith(page) && char.IsDigit(name[page.Length]);
        }

        //Shot Conductor : the digit at the end picks the conductor
        private static int ShotConductor(string name)
        {
            int index = name[name.Length - 1] - '0';
            return Math.Max(0, Math.Min(ConductorList.All.Length - 1, index));
        }

        //Page Open : build a sample run that suits the page, then show it.
        //Names: title titlecontinue titlequit guide guide3 guide4 guide5 settings calibrate gallery detail chapter era
        //       crossing recruit bandname route pause band (or view) duelbargain dueltrait duelmute duelmirror
        //       duel duelcombo duelboss duelcutin duelpause dueldouble dueltremolo duelfermata duelfire duelrepeat finale
        //       result defeat reward shop shopleave event eventchair rest curtain curtainwin
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
            if (IsConductorShot(name, "detail"))                                        // detail0 .. detail4 : one per conductor
                screen = new ConductorDetailScreen(ShotConductor(name), new Rectangle(475, 126, 330, 450));
            if (name == "chapter") screen = new ChapterScreen();
            if (name == "era") screen = new EraChoiceScreen(true);
            if (name == "crossing") screen = new EraChoiceScreen(false);
            if (name == "recruit") screen = new RecruitScreen();
            if (name == "bandname") screen = new BandNameScreen();
            if (name == "route" || name == "pause") screen = new RouteScreen();
            if (name == "view" || name == "band") screen = new BandScreen();
            if (name.StartsWith("duel") || name == "finale") screen = new DuelScreen();
            if (name == "result" || name == "defeat") screen = new ResultScreen();
            if (name == "reward") screen = new MotifRewardScreen(false);
            if (name == "shop" || name == "shopleave") screen = new ShopScreen();
            if (name.StartsWith("event")) screen = new EventScreen();
            if (name == "rest") screen = new RestScreen();
            if (name == "curtain" || name == "curtainwin") screen = new CurtainCallScreen(name == "curtainwin");

            game.Screens.ChangeNow(screen);

            //Picture Hooks : start the moment the picture is about
            if (name == "calibrate") ((SettingsScreen)screen).BeginTest();
            if (name == "finale") ((DuelScreen)screen).BeginFinaleForPicture();
            if (name == "dueltremolo" || name == "duelrepeat") ((DuelScreen)screen).JumpForPicture(BattleRules.BeatsPerRound - 1);
            if (name == "dueldouble") ((DuelScreen)screen).JumpForPicture(FirstPair(run.Battle));
            if (name == "duelfermata") ((DuelScreen)screen).HoldForPicture();
            if (name == "tutorialsize") ((TutorialScreen)screen).JumpForPicture(2, 0f);
            if (name == "tutorialtiming") ((TutorialScreen)screen).JumpForPicture(3, 5.6f);
            if (name == "tutorialloud") ((TutorialScreen)screen).JumpForPicture(4, 6.6f);
            if (name == "tutorialbreath") ((TutorialScreen)screen).JumpForPicture(5, 7.6f);
            if (name == "tutorialready") ((TutorialScreen)screen).JumpForPicture(6, 0f);
            if (name == "tutorialroll") ((TutorialScreen)screen).JumpForPicture(7, 5.8f);
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
            bool ownConductor = IsConductorShot(name, "duelc") || IsConductorShot(name, "duels");
            run.Start(ConductorList.All[ownConductor ? ShotConductor(name) : 2], game.StoryFont, RouteNodeInfo.CaptionWrapWidth);
            run.BandName = "THE SILENT CHOIR";

            //Ensemble : three from Siam on stage, one from the Classical era on the bench
            run.ChooseEra(1);
            run.RecruitOne();
            run.RecruitOne();
            run.RecruitOne();
            run.ChooseEra(0);
            run.RecruitOne();
            run.ChooseEra(name == "shop" ? 0 : 1);                                  // the shop picture : a Classical player still for hire

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
                run.CurrentEvent = EventList.All[name == "eventchair" ? EventList.All.Length - 1 : 1];   // eventchair : THE EMPTY CHAIR
                run.BattlesWon = 6;
                run.PerfectsTotal = 23;
                run.BestCombo = 5;

                //Journey : four places already behind the band, so the route's road shows its marks
                run.RecordStop(NodeType.Battle);
                run.RecordStop(NodeType.Event);
                run.RecordStop(NodeType.Battle);
                run.RecordStop(NodeType.Shop);
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
            if (name == "reward" || name == "dueltremolo" || name.EndsWith("repeat")) run.Chosen.Type = NodeType.Elite;
            if (name == "dueldouble") run.Floor = 3;                                  // pairs from floor two, more on the last
            if (name == "duelfermata") run.Floor = 2;                                 // held notes from floor two
            run.Chosen.Title = RouteNodeInfo.TitleOf(run.Chosen.Type);
            run.Chosen.Caption = RouteNodeInfo.CaptionOf(run.Chosen.Type);
            run.BeginBattle();
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

            //Repeat Pictures : round three, the phrase played three times (REPEATS)
            if (name.EndsWith("repeat"))
            {
                run.Battle.EndRound();
                run.Battle.EndRound();
            }


            //Trait Pictures : an ordinary enemy on floor two explains its trait on the first banner,
            //the devil makes its offer, THE MUTE CHOIR silences a part, REQUIEM mirrors the arrows
            //in round three (round 14 : all inside the duel, the STAGE page is gone)
            Random pick = new Random(5);
            if (name == "dueltrait" || name == "duelboss")
            {
                run.Floor = 2;
                run.Battle = new BattleState(run, name == "duelboss" ? EnemyList.All[7] : EnemyList.All[0], pick);
            }
            if (name == "duelbargain")
            {
                run.Battle = new BattleState(run, EnemyList.All[8], pick);
                run.Battle.EndRound();
            }
            if (name == "duelmute")
            {
                run.Battle = new BattleState(run, EnemyList.All[4], pick);
                run.Battle.EndRound();                                                // it silences from round two
            }
            if (name == "duelmirror")
            {
                run.Battle = new BattleState(run, EnemyList.All[6], pick);
                run.Battle.EndRound();
                run.Battle.EndRound();
            }

            //Signature Picture : the recipe is full, so SPACE (pressed by the tool) lets it loose
            if (name == "duelcutin")
                for (int f = 0; f < run.Battle.Notes.Length; f++) run.Battle.Notes[f] = run.Battle.NeedFor(f);

            //Running Signature : duels0 .. duels4, each conductor's move one stroke in (round 13).
            //THE UNHEARING's band is low on breath, so THE CLOSER THE LOUDER shows.
            if (IsConductorShot(name, "duels"))
            {
                run.Battle.StartSignature();
                run.Battle.SignatureLeft = BattleRules.SignatureStrokes - 1;
                if (run.Conductor.Perk == ConductorPerk.CloserLouder) run.ChangeStamina(-45);
            }

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
