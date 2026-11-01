using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Battle;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //TestRoomScreen : THE TEST ROOM (Beta1 31 Oct, docs/BETA1_DIRECTION.md 22, the user's words docs/USER_BRIEFS.md 13.51 13.52).
    //A page for the team, not for players : one click and the game stands on the page to test (a boss about to use its BIG ATTACK,
    //THE WINGS, a shop, the result of a fight) without playing a whole run to get there.
    //   F2 on the title page opens it (TitleScreen). F2 while a test plays comes back here (TacetGame). ESC here goes to the title.
    //   TEST RUN : every test is a real run made on the spot (MakeRun), so the game plays on as usual after the page
    //   (the reward, the map, the next floor).
    //   SAVES OFF : a test run is never written or deleted (SaveFile.TestRun), and the ARCHIVE's APPLAUSE is put back as it was
    //   when the tests end (StopTests), so the player's CONTINUE and APPLAUSE never change. Settings still save.
    //   The rows at the top pick who plays the tests : the conductor, the band of three, the floor, the stars and the gifts.
    public class TestRoomScreen : GameScreen
    {
        //TEST ROOM ON : the one switch. false takes F2 and this page out of the game (before it is sold).
        public static readonly bool On = true;

        //Test Mode : true from the moment this page opens until the title page opens again (TitleScreen.Load calls StopTests)
        public static bool Running;

        //Shards : every test run starts with this many, so the shop, THE WINGS and the rerolls can all be tried
        private const int Shards = 500;

        //Middle Stage : the row of the map where the rooms and the fights of the tests stand (rows 2 to the last but two are
        //ordinary ones, a floor has 7 to 10 stages)
        private const int MiddleStage = 4;

        //Tests : what a button of this page starts (Launch)
        public enum Test
        {
            BossStart, BossBig, BossHalf,                                   // value : the boss's place in EnemyList.All
            Wings, Rest, Shop, Workshop, Treasure, Rehearsal, Event,        // Event value : the event's place in EventList.All
            Fight, Elite, Enemy, Practice,                                  // Enemy value : the enemy's place in EnemyList.All
            Audition, FloorStart, BeforeBoss, BossReward, ResultWin, ResultLose, CurtainWin, CurtainLose
        }

        //Picks : the words of the top rows, which stay chosen
        private enum Pick { None, Conductor, Band, Floor, Stars, Gifts }

        //Fight Tweak : what the next fight changes once it is made (BattleScreen.Load calls TweakFight)
        private enum Tweak { None, BigFirst, HalfLife }

        private class Item
        {
            public Rectangle Box;
            public string Label = "";
            public Pick Pick;              // a word of the top rows, None for a button that starts a test
            public Test Test;
            public int Value;              // the conductor, the musician, the floor, the stars, the gifts, the boss, the event, the enemy
            public float LabelScale;       // a test button's word : one size for all, a long one smaller so it fits (AddButton)
            public float LabelWidth;
            public UiMotion Motion = new UiMotion();
        }

        //Chosen Picks : kept while the game is open, so coming back with F2 finds them as they were
        private static int conductorPick;                                   // ConductorList.Shown
        private static List<int> bandPick = new List<int> { 0, 1, 2 };     // MusicianList.Shown in the order picked (PAGANINI QUANTZ PFUNDT)
        private static int floorPick = 1;
        private static int starsPick;                                       // 0 none, 1 one star on every move, 2 all three
        private static int giftsPick;                                       // 0 none, 1 every gift the band can find
        private static int lastFocus;
        private static Tweak pendingTweak;

        //ARCHIVE kept : a test run's APPLAUSE is given in memory (Archive.Pay), so the values from before the tests come back after
        private static int keptEchoes;
        private static int[] keptLevels = new int[0];

        private static readonly string[] floorWords = { "1", "2", "3" };
        private static readonly string[] starsWords = { "NONE", "1 EACH", "3 EACH" };
        private static readonly int[] starsEach = { 0, 1, RehearsalList.Most };
        private static readonly string[] giftsWords = { "NONE", "ALL" };
        private static readonly string[] orderWords = { "1", "2", "3" };

        //Layout : the page is 1280 x 720, the title bar on the left, the picks at the top, four columns under them
        private const int Left = 168;
        private const int PickRow1 = 52;                // the chips of CONDUCTOR and FLOOR
        private const int PickRow2 = 114;               // the chips of BAND, STARS and GIFTS
        private const int ChipH = 28;
        private const int ChipGap = 8;
        private const int GroupGap = 36;
        private const int HeaderY = 166;                // the names of the four columns
        private const int ColumnTop = 198;              // the first button of each column
        private const int ButtonH = 28;
        private const int ButtonStep = 34;
        private const int SubHeader = 20;               // the room a small heading takes inside a column
        private const int BossStep = 68;
        private const int BossButtonY = 22;             // the boss's three buttons, under its name
        private const int LabelMargin = 22;             // a test button's word keeps this far from its slanted ends
        private static readonly int[] ColumnX = { 168, 632, 834, 1066 };
        private static readonly int[] ColumnW = { 442, 182, 210, 182 };

        private List<Item> items = new List<Item>();
        private int focus;
        private float time;
        private Vector2 lastMouse;
        private Rectangle backButton = new Rectangle(40, 646, 190, 46);
        private UiMotion backMotion = new UiMotion();

        //Prepared Text and Places : made in Load, never in Draw
        private float[] pickX = new float[6];           // where each pick's heading starts (by Pick)
        private List<string> bossNames = new List<string>();
        private List<string> bossBigs = new List<string>();
        private List<float> bossBigX = new List<float>();
        private List<float> bossY = new List<float>();
        private float chargeY, armorY, resultY, curtainY, eventsY;
        private string infoLine = "";

        public override bool CanPause
        {
            get { return false; }
        }

        public override bool UsesEscape
        {
            get { return true; }
        }

        //Wipe Word : the page's name in the wipe on the way here (WIPE WORD)
        public override string WipeWord
        {
            get { return "TEST ROOM"; }
        }

        public override void Load()
        {
            StartTests();
            Game.CurrentRun = null;                     // a test that was playing is dropped
            Game.IsMouseVisible = true;
            pendingTweak = Tweak.None;
            BuildItems();
            focus = Math.Max(0, Math.Min(items.Count - 1, lastFocus));
            lastMouse = Input.MousePos;
            infoLine = "SAVES ARE OFF.  EVERY TEST IS A NEW RUN WITH " + Shards + " " + Words.Shards + ".";

            Lens.Ambient = 0.62f;
            Lens.SpotPower = 0f;
            Lens.Vignette = 0.4f;
            SoundBank.PlayMusic(Music.Title);
        }

        //======================================================================================
        //Test Mode : saves off from the first test until the title page
        //======================================================================================

        //Start Tests : this page opens, or a test starts (the capture tool can start one straight away). Saving goes off.
        public static void StartTests()
        {
            if (Running) return;
            Running = true;
            SaveFile.TestRun = true;
            keptEchoes = Archive.Echoes;
            keptLevels = new int[Archive.All.Length];
            for (int i = 0; i < keptLevels.Length; i++) keptLevels[i] = Archive.All[i].Level;
        }

        //Stop Tests : the title page opens. Saving is back on and the ARCHIVE is as it was before the tests.
        public static void StopTests(TacetGame game)
        {
            if (!Running) return;
            Running = false;
            SaveFile.TestRun = false;
            Archive.Echoes = keptEchoes;
            for (int i = 0; i < keptLevels.Length && i < Archive.All.Length; i++) Archive.All[i].Level = keptLevels[i];
            pendingTweak = Tweak.None;
            game.CurrentRun = null;
        }

        //Tweak Fight : called by BattleScreen.Load once the fight is made. A boss test may start it at its BIG ATTACK (THE TELL on the
        //first plan) or just over half its life (the first hit on it starts THE TURN). Every other fight is left as it is.
        public static void TweakFight(TurnBattle battle)
        {
            Tweak tweak = pendingTweak;
            pendingTweak = Tweak.None;
            if (!Running || tweak == Tweak.None) return;

            if (tweak == Tweak.BigFirst) battle.BigAttackFirst();
            if (tweak == Tweak.HalfLife)
                for (int i = 0; i < battle.Enemies.Count; i++)
                {
                    Fighter f = battle.Enemies[i];
                    if (f.Enemy.Kind == EnemyKind.Boss) f.Hp = Math.Min(f.Hp, (int)(f.MaxHp * BattleRules.TurnBigPhase) + 1);
                }
        }

        //======================================================================================
        //Launch : the test's run, then its page
        //======================================================================================

        //Launch : public for the capture tool's pictures of the tests (Core/DebugShots.cs, the names that start with test)
        public static void Launch(TacetGame game, Test test, int value)
        {
            StartTests();
            pendingTweak = Tweak.None;

            //PRACTICE : the tutorial's fight needs no run (it goes back to the title page when it ends)
            if (test == Test.Practice)
            {
                game.CurrentRun = null;
                game.Screens.Change(new BattleScreen(true));
                return;
            }

            RunState run = MakeRun(game, test != Test.Audition);
            game.CurrentRun = run;

            switch (test)
            {
                case Test.BossStart:
                case Test.BossBig:
                case Test.BossHalf:
                    run.Bosses[run.Floor - 1] = value;                                      // BOSSES : the boss at the end of this floor
                    WalkTo(run, run.StagesThisFloor);
                    pendingTweak = test == Test.BossBig ? Tweak.BigFirst : (test == Test.BossHalf ? Tweak.HalfLife : Tweak.None);
                    Enter(game, run, NodeType.Boss, null, null);
                    break;
                case Test.Wings:
                    WalkTo(run, run.StagesThisFloor - 1);                                   // THE WINGS : the one room before the boss
                    Enter(game, run, NodeType.Rest, null, null);
                    break;
                case Test.Rest: InTheMiddle(game, run, NodeType.Rest); break;
                case Test.Shop: InTheMiddle(game, run, NodeType.Shop); break;
                case Test.Workshop: InTheMiddle(game, run, NodeType.Luthier); break;
                case Test.Treasure: InTheMiddle(game, run, NodeType.Treasure); break;
                case Test.Fight: InTheMiddle(game, run, NodeType.Battle); break;
                case Test.Elite: InTheMiddle(game, run, NodeType.Elite); break;
                case Test.Event:
                    WalkTo(run, MiddleStage);
                    Enter(game, run, NodeType.Event, null, EventList.All[value]);
                    break;
                case Test.Enemy:
                    Enemy enemy = EnemyList.All[value];
                    WalkTo(run, MiddleStage);
                    Enter(game, run, enemy.Kind == EnemyKind.Elite ? NodeType.Elite : NodeType.Battle, enemy, null);
                    break;
                case Test.Rehearsal:
                    WalkTo(run, MiddleStage);                                               // REHEARSAL : the star a won fight leaves
                    Place(run, NodeType.Battle);
                    run.RehearsalPending = true;
                    game.Screens.Change(new RehearsalScreen(RehearsalScreen.From.Fight));
                    break;
                case Test.Audition:
                    game.Screens.Change(new TeamPickScreen());                             // then the band's name, then the floor
                    break;
                case Test.FloorStart:
                    game.Screens.Change(new ChapterScreen());                              // the floor's card, then its map
                    break;
                case Test.BeforeBoss:
                    WalkTo(run, run.StagesThisFloor - 1);                                   // the map with THE WINGS next, the boss after
                    game.Screens.Change(new RouteScreen());
                    break;
                case Test.BossReward:
                    WalkTo(run, run.StagesThisFloor);                                       // the boss beaten : REFORGE and two great gifts
                    Place(run, NodeType.Boss);
                    run.BeginBattle();
                    game.Screens.Change(new MotifRewardScreen());
                    break;
                case Test.ResultWin:
                case Test.ResultLose:
                    WalkTo(run, MiddleStage);
                    Place(run, NodeType.Battle);
                    run.BeginBattle();
                    MadeUpResult(run, test == Test.ResultWin);
                    game.Screens.Change(new ResultScreen());
                    break;
                case Test.CurtainWin:
                case Test.CurtainLose:
                    MadeUpJourney(run, test == Test.CurtainWin);
                    game.Screens.Change(new CurtainCallScreen(test == Test.CurtainWin));
                    break;
            }
        }

        //Make Run : a new run with the picks of the top rows, on the picked floor, at its opening. The audition makes it without a band.
        private static RunState MakeRun(TacetGame game, bool withBand)
        {
            RunState run = new RunState();
            run.Start(ConductorList.Shown[conductorPick], game.StoryFont, RouteNodeInfo.CaptionWrapWidth);
            run.Maestro = Settings.Maestro;                                                 // MAESTRO MODE as the player set it
            run.BandName = "TEST BAND";
            run.Floor = floorPick;
            run.BeginFloor();                                                               // the picked floor's own map
            run.AddShards(Shards);

            if (withBand)
            {
                run.PickBand(BandOfThree());
                List<Musician> band = run.Lineup();

                //Stars : REHEARSAL, the same number on every move of the band (the ATTACK's star is the weapon's tier)
                for (int i = 0; i < band.Count; i++)
                    for (int b = 0; b < RehearsalList.Buttons; b++)
                        for (int k = 0; k < starsEach[starsPick]; k++) run.Raise(band[i], b);

                //Gifts : every gift this band can find (GIFTS, the band's own and its legends')
                if (giftsPick == 1)
                    for (int i = 0; i < MotifList.Pool.Length; i++)
                        if (MotifList.Fits(MotifList.Pool[i], band) && !run.Motifs.Contains(MotifList.Pool[i])) run.AddMotif(MotifList.Pool[i]);
            }
            run.RefreshLabels();
            return run;
        }

        //Band Of Three : the musicians picked, in the order picked (the first one stands in front). Fewer than three picked :
        //the first ones not picked fill the band, so a test always has its three.
        private static List<Musician> BandOfThree()
        {
            List<Musician> band = new List<Musician>();
            for (int i = 0; i < bandPick.Count; i++) band.Add(MusicianList.Shown[bandPick[i]]);
            for (int i = 0; i < MusicianList.Shown.Length && band.Count < BattleRules.BandSize; i++)
                if (!band.Contains(MusicianList.Shown[i])) band.Add(MusicianList.Shown[i]);
            return band;
        }

        //Walk To : the band walks the map's first ways up to this stage, the way a player would (every stop is written down)
        private static void WalkTo(RunState run, int stage)
        {
            if (run.AtFloorOpening) run.LeaveOpening();
            stage = Math.Min(stage, run.StagesThisFloor);
            while (run.Stage < stage && run.Options.Length > 0)
            {
                run.Chosen = run.Options[0];
                run.RecordStop(run.Chosen.Type);
                run.Advance();
            }
            run.Chosen = null;
        }

        //Place : the band picks a place of this kind among the ways offered now. With none of that kind the first way turns into
        //one (RunState.ReplaceChosen keeps its spot on the map and its ways on), so the floor carries on from there.
        private static RouteNode Place(RunState run, NodeType type)
        {
            RouteNode node = run.Options.Length > 0 ? run.Options[0] : null;
            for (int i = 0; i < run.Options.Length; i++)
                if (run.Options[i].Type == type)
                {
                    node = run.Options[i];
                    break;
                }
            run.Chosen = node;
            if (node == null || node.Type != type) run.ReplaceChosen(type);
            run.RecordStop(type);
            return run.Chosen;
        }

        //Enter : walk into the place the way the map does (RunFlow.Enter sets up the fight, the event or the room, and opens it).
        //The place's own dice are rolled fresh, so every test shop and chest is a new one.
        private static void Enter(TacetGame game, RunState run, NodeType type, Enemy enemy, GameEvent ev)
        {
            RouteNode node = Place(run, type);
            run.PlaceSeed = run.Rng.Next(1, int.MaxValue);
            RunFlow.Enter(game, node, enemy, ev);
        }

        private static void InTheMiddle(TacetGame game, RunState run, NodeType type)
        {
            WalkTo(run, MiddleStage);
            Enter(game, run, type, null, null);
        }

        //Made Up Result : what a fight hands to the result page (BattleScreen does this at the end of a real one). 3 PERFECTs
        //out of 5 notes and 80 percent of the life kept give RANK A. A lost fight ends the run at once, as in a real one.
        private static void MadeUpResult(RunState run, bool won)
        {
            BattleState record = run.Battle;
            record.Finished = true;
            record.PlayerWon = won;
            record.PerfectCount = 3;
            record.NotesPlayed = 5;
            record.LifeKept = 0.8f;
            record.BestCombo = 3;
            record.Line = won ? BattleRules.LineLimit : -BattleRules.LineLimit;
            run.LastRounds = 4;                                                             // TURNS on the result page
            run.RehearsalPending = won;
            if (!won) Archive.Pay(run, false);                                              // in memory only, StopTests puts it back
        }

        //Made Up Journey : the places of a whole run (won) or of a run lost part way into the picked floor, for the curtain call
        private static void MadeUpJourney(RunState run, bool won)
        {
            NodeType[] floorPath = { NodeType.Battle, NodeType.Event, NodeType.Battle, NodeType.Shop, NodeType.Elite,
                                     NodeType.Rest, NodeType.Battle, NodeType.Boss };
            int lastFloor = won ? BattleRules.FloorsPerRun : run.Floor;
            int fights = 0;
            for (int f = 1; f <= lastFloor; f++)
            {
                run.Floor = f;
                int count = won || f < lastFloor ? floorPath.Length : 3;                    // a lost run ends in its third place, a fight
                for (int i = 0; i < count; i++)
                {
                    run.RecordStop(floorPath[i]);
                    if (floorPath[i] == NodeType.Battle || floorPath[i] == NodeType.Elite || floorPath[i] == NodeType.Boss) fights++;
                }
            }
            if (!won) run.MarkLastStopLost();
            run.BattlesWon = won ? fights : fights - 1;
            run.PerfectsTotal = run.BattlesWon * 4;
            run.BestCombo = 6;
            run.RunComplete = won;
            run.RefreshLabels();
        }

        //======================================================================================
        //The Page : picks at the top, four columns of tests under them
        //======================================================================================

        private void BuildItems()
        {
            items.Clear();

            //Row 1 : CONDUCTOR and FLOOR
            float x = Left;
            pickX[(int)Pick.Conductor] = x;
            for (int i = 0; i < ConductorList.Shown.Length; i++) x = AddChip(Pick.Conductor, i, ConductorList.Shown[i].NameShort, x, PickRow1);
            x += GroupGap;
            pickX[(int)Pick.Floor] = x;
            for (int i = 0; i < floorWords.Length; i++) x = AddChip(Pick.Floor, i + 1, floorWords[i], x, PickRow1);

            //Row 2 : BAND, STARS and GIFTS
            x = Left;
            pickX[(int)Pick.Band] = x;
            for (int i = 0; i < MusicianList.Shown.Length; i++) x = AddChip(Pick.Band, i, MusicianList.Shown[i].Name, x, PickRow2);
            x += GroupGap;
            pickX[(int)Pick.Stars] = x;
            for (int i = 0; i < starsWords.Length; i++) x = AddChip(Pick.Stars, i, starsWords[i], x, PickRow2);
            x += GroupGap;
            pickX[(int)Pick.Gifts] = x;
            for (int i = 0; i < giftsWords.Length; i++) x = AddChip(Pick.Gifts, i, giftsWords[i], x, PickRow2);

            //Column 1 : BOSSES, each with its three moments
            bossNames.Clear();
            bossBigs.Clear();
            bossBigX.Clear();
            bossY.Clear();
            int bw = (ColumnW[0] - 2 * 10) / 3;
            for (int e = 0; e < EnemyList.All.Length; e++)
            {
                Enemy boss = EnemyList.All[e];
                if (boss.Kind != EnemyKind.Boss) continue;
                int y = ColumnTop + bossNames.Count * BossStep;
                bossNames.Add(boss.Name);
                bossBigs.Add(boss.BigName.Length > 0 ? Words.BigAttack + "  " + boss.BigName : "");
                bossBigX.Add(ColumnX[0] + Gfx.SpacedWidth(Game.Font, boss.Name, TextSize.Label, 2f) + 16f);
                bossY.Add(y);
                AddButton(Test.BossStart, e, "START", new Rectangle(ColumnX[0], y + BossButtonY, bw, ButtonH));
                AddButton(Test.BossBig, e, Words.BigAttack, new Rectangle(ColumnX[0] + bw + 10, y + BossButtonY, bw, ButtonH));
                AddButton(Test.BossHalf, e, "HALF LIFE", new Rectangle(ColumnX[0] + 2 * (bw + 10), y + BossButtonY, bw, ButtonH));
            }

            //Column 2 : MAP ROOMS
            int cy = ColumnTop;
            cy = AddRow(Test.Wings, 0, Words.Wings, 1, cy);
            cy = AddRow(Test.Rest, 0, RouteNodeInfo.TitleOf(NodeType.Rest), 1, cy);
            cy = AddRow(Test.Shop, 0, RouteNodeInfo.TitleOf(NodeType.Shop), 1, cy);
            cy = AddRow(Test.Workshop, 0, RouteNodeInfo.TitleOf(NodeType.Luthier), 1, cy);
            cy = AddRow(Test.Treasure, 0, RouteNodeInfo.TitleOf(NodeType.Treasure), 1, cy);
            cy = AddRow(Test.Rehearsal, 0, "REHEARSAL", 1, cy);

            //Column 3 : FIGHTS, then the enemies with a CHARGE and those that raise ARMOR (ENEMY MOVES)
            cy = ColumnTop;
            cy = AddRow(Test.Fight, 0, RouteNodeInfo.TitleOf(NodeType.Battle), 2, cy);
            cy = AddRow(Test.Elite, 0, RouteNodeInfo.TitleOf(NodeType.Elite), 2, cy);
            chargeY = cy;
            cy += SubHeader;
            for (int e = 0; e < EnemyList.All.Length; e++)
                if (EnemyList.All[e].Kind != EnemyKind.Boss && Array.IndexOf(EnemyList.All[e].Moves, EnemyMove.Charge) >= 0)
                    cy = AddRow(Test.Enemy, e, EnemyList.All[e].Name, 2, cy);
            armorY = cy;
            cy += SubHeader;
            for (int e = 0; e < EnemyList.All.Length; e++)
                if (EnemyList.All[e].Kind != EnemyKind.Boss && Array.IndexOf(EnemyList.All[e].Moves, EnemyMove.Guard) >= 0)
                    cy = AddRow(Test.Enemy, e, EnemyList.All[e].Name, 2, cy);

            //Events : under MAP ROOMS and FIGHTS, in two columns
            eventsY = cy + 14;
            int ex = ColumnX[1];
            int ew = (ColumnX[2] + ColumnW[2] - ColumnX[1] - 12) / 2;
            int rows = (EventList.All.Length + 1) / 2;
            int ey = (int)eventsY + SubHeader + 6;
            for (int i = 0; i < EventList.All.Length; i++)
            {
                int column = i / rows;
                int row = i % rows;
                AddButton(Test.Event, i, EventList.All[i].Title, new Rectangle(ex + column * (ew + 12), ey + row * (ButtonStep - 2), ew, ButtonH));
            }

            //Column 4 : RUN PAGES, then the two sides of the result and of the curtain call
            cy = ColumnTop;
            cy = AddRow(Test.Audition, 0, "THE AUDITION", 3, cy);
            cy = AddRow(Test.FloorStart, 0, "FLOOR START", 3, cy);
            cy = AddRow(Test.BeforeBoss, 0, "MAP BEFORE BOSS", 3, cy);
            cy = AddRow(Test.BossReward, 0, "BOSS REWARD", 3, cy);
            cy = AddRow(Test.Practice, 0, "PRACTICE", 3, cy);
            int half = (ColumnW[3] - 10) / 2;
            resultY = cy;
            cy += SubHeader;
            AddButton(Test.ResultWin, 0, "WIN", new Rectangle(ColumnX[3], cy, half, ButtonH));
            AddButton(Test.ResultLose, 0, "LOSE", new Rectangle(ColumnX[3] + half + 10, cy, half, ButtonH));
            cy += ButtonStep;
            curtainY = cy;
            cy += SubHeader;
            AddButton(Test.CurtainWin, 0, "WIN", new Rectangle(ColumnX[3], cy, half, ButtonH));
            AddButton(Test.CurtainLose, 0, "LOSE", new Rectangle(ColumnX[3] + half + 10, cy, half, ButtonH));
        }

        private float AddChip(Pick pick, int value, string label, float x, int y)
        {
            int w = (int)Math.Ceiling(Gfx.SpacedWidth(Game.Font, label, TextSize.Label, 2f)) + 30;
            Item it = new Item { Pick = pick, Value = value, Label = label, Box = new Rectangle((int)x, y, w, ChipH) };
            items.Add(it);
            return x + w + ChipGap;
        }

        private void AddButton(Test test, int value, string label, Rectangle box)
        {
            Item it = new Item { Test = test, Value = value, Label = label, Box = box };
            float room = box.Width - 2 * LabelMargin;
            it.LabelScale = TextSize.Body;
            it.LabelWidth = Gfx.SpacedWidth(Game.Font, label, it.LabelScale, 1.5f);
            if (it.LabelWidth > room)
            {
                it.LabelScale *= room / it.LabelWidth;
                it.LabelWidth = room;
            }
            items.Add(it);
        }

        //Add Row : a button the width of its column, returns where the next one goes
        private int AddRow(Test test, int value, string label, int column, int y)
        {
            AddButton(test, value, label, new Rectangle(ColumnX[column], y, ColumnW[column], ButtonH));
            return y + ButtonStep;
        }

        //Chosen : a word of the top rows that is picked now
        private static bool Chosen(Item it)
        {
            switch (it.Pick)
            {
                case Pick.Conductor: return conductorPick == it.Value;
                case Pick.Band: return bandPick.Contains(it.Value);
                case Pick.Floor: return floorPick == it.Value;
                case Pick.Stars: return starsPick == it.Value;
                case Pick.Gifts: return giftsPick == it.Value;
            }
            return false;
        }

        //Can Start : REHEARSAL needs a star still to give (every move at three stars has none)
        private static bool CanStart(Item it)
        {
            return !(it.Test == Test.Rehearsal && starsEach[starsPick] >= RehearsalList.Most);
        }

        public override void Update(float dt)
        {
            time += dt;

            //Point : the arrows move to the nearest word or button that way, the mouse points when it moves
            int before = focus;
            if (Input.KeyPressed(Keys.Left) || Input.KeyPressed(Keys.A)) Move(-1, 0);
            if (Input.KeyPressed(Keys.Right) || Input.KeyPressed(Keys.D)) Move(1, 0);
            if (Input.KeyPressed(Keys.Up) || Input.KeyPressed(Keys.W)) Move(0, -1);
            if (Input.KeyPressed(Keys.Down) || Input.KeyPressed(Keys.S)) Move(0, 1);
            if (Input.MousePos != lastMouse)
            {
                lastMouse = Input.MousePos;
                for (int i = 0; i < items.Count; i++)
                    if (Input.MouseOver(items[i].Box)) focus = i;
            }
            if (focus != before) SoundBank.Play(Sfx.UiMove);
            for (int i = 0; i < items.Count; i++) items[i].Motion.Update(dt, i == focus);
            backMotion.Update(dt, Input.MouseOver(backButton));

            //Use : a click, or ENTER on the one pointed at
            for (int i = 0; i < items.Count; i++)
                if (Input.ClickedOn(items[i].Box))
                {
                    focus = i;
                    Use(items[i]);
                    return;
                }
            if (Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Space))
            {
                Use(items[focus]);
                return;
            }

            if (Input.ClickedOn(backButton) || Input.KeyPressed(Keys.Escape))
            {
                backMotion.Press();
                SoundBank.Play(Sfx.UiConfirm);
                Game.Screens.Change(new TitleScreen());                                     // the title page ends the tests (StopTests)
            }
        }

        //Move : ADVANCED PART (a small one). The next word or button in the direction pressed is the one with the best mix of
        //"far enough that way" and "little off to the side". Off to the side counts 2.5 times as much, so DOWN stays in its column
        //rather than jumping to a closer button one column over. Nothing that way : the pointer stays.
        private void Move(int dx, int dy)
        {
            Vector2 from = items[focus].Box.Center.ToVector2();
            int best = -1;
            float bestScore = float.MaxValue;
            for (int i = 0; i < items.Count; i++)
            {
                if (i == focus) continue;
                Vector2 d = items[i].Box.Center.ToVector2() - from;
                float ahead = d.X * dx + d.Y * dy;
                float aside = Math.Abs(d.X * dy) + Math.Abs(d.Y * dx);
                if (ahead < 4f) continue;
                float score = ahead + aside * 2.5f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }
            if (best >= 0) focus = best;
        }

        private void Use(Item it)
        {
            it.Motion.Press();

            //Picks : the conductor, the floor, the stars and the gifts take one word each. THE BAND : a click picks a musician or
            //takes them out again, three at most, the first one picked stands in front.
            if (it.Pick != Pick.None)
            {
                bool ok = true;
                if (it.Pick == Pick.Conductor) conductorPick = it.Value;
                if (it.Pick == Pick.Floor) floorPick = it.Value;
                if (it.Pick == Pick.Stars) starsPick = it.Value;
                if (it.Pick == Pick.Gifts) giftsPick = it.Value;
                if (it.Pick == Pick.Band)
                {
                    if (bandPick.Contains(it.Value)) bandPick.Remove(it.Value);
                    else if (bandPick.Count < BattleRules.BandSize) bandPick.Add(it.Value);
                    else ok = false;
                }
                SoundBank.Play(ok ? Sfx.UiConfirm : Sfx.UiDenied);
                return;
            }

            if (!CanStart(it))
            {
                SoundBank.Play(Sfx.UiDenied);
                return;
            }
            SoundBank.Play(Sfx.UiConfirm);
            lastFocus = focus;
            Launch(Game, it.Test, it.Value);
        }

        //======================================================================================
        //Drawing
        //======================================================================================

        public override bool HasWorld
        {
            get { return true; }
        }

        public override bool ShowsGrain
        {
            get { return false; }
        }

        //World : a plain dark room, a faint plate behind the picks and behind each column
        public override void DrawWorld(SpriteBatch sb)
        {
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageDeep);
            float a = UiMotion.Between(time, 0f, 0.4f);
            Gfx.Rect(sb, Left - 14, 22, TacetGame.ScreenW - Left - 18, 138, Palette.Panel * (0.7f * a));
            for (int c = 0; c < ColumnX.Length; c++)
                Gfx.Rect(sb, ColumnX[c] - 10, HeaderY - 4, ColumnW[c] + 20, 474, Palette.Panel * (0.45f * a));
        }

        public override void Draw(SpriteBatch sb)
        {
            float a = UiMotion.Between(time, 0f, 0.35f);

            //Title Bar : the page's name, big and turned, on a tall bar at the left (like THE ARCHIVE)
            int bx = 40 + (int)Math.Round((1f - a) * -40f);
            Ui.TitleBar(sb, new Rectangle(bx, 60, 96, 540), 24, "TEST ROOM", a);

            //Picks
            Color head = Palette.LineGrey * a;
            Gfx.TextSpaced(sb, Game.Font, "CONDUCTOR", pickX[(int)Pick.Conductor], PickRow1 - 20, head, TextSize.Tiny, 2f);
            Gfx.TextSpaced(sb, Game.Font, "FLOOR", pickX[(int)Pick.Floor], PickRow1 - 20, head, TextSize.Tiny, 2f);
            Gfx.TextSpaced(sb, Game.Font, "BAND  PICK 3, THE FIRST STANDS IN FRONT", pickX[(int)Pick.Band], PickRow2 - 20, head, TextSize.Tiny, 2f);
            Gfx.TextSpaced(sb, Game.Font, "STARS ON EVERY MOVE", pickX[(int)Pick.Stars], PickRow2 - 20, head, TextSize.Tiny, 2f);
            Gfx.TextSpaced(sb, Game.Font, "GIFTS", pickX[(int)Pick.Gifts], PickRow2 - 20, head, TextSize.Tiny, 2f);

            //Columns
            Color title = Palette.Paper * a;
            Gfx.TextSpaced(sb, Game.Font, "BOSSES", ColumnX[0], HeaderY, title, TextSize.Label, 3f);
            Gfx.TextSpaced(sb, Game.Font, "MAP ROOMS", ColumnX[1], HeaderY, title, TextSize.Label, 3f);
            Gfx.TextSpaced(sb, Game.Font, "FIGHTS", ColumnX[2], HeaderY, title, TextSize.Label, 3f);
            Gfx.TextSpaced(sb, Game.Font, "RUN PAGES", ColumnX[3], HeaderY, title, TextSize.Label, 3f);
            for (int c = 0; c < ColumnX.Length; c++) Gfx.Rect(sb, ColumnX[c], HeaderY + 20, ColumnW[c], 1, Palette.LineGrey * (0.5f * a));

            //Small Headings : inside the columns
            Gfx.TextSpaced(sb, Game.Font, Words.Charge, ColumnX[2], chargeY + 4, head, TextSize.Tiny, 2f);
            Gfx.TextSpaced(sb, Game.Font, Words.ArmorUp, ColumnX[2], armorY + 4, head, TextSize.Tiny, 2f);
            Gfx.TextSpaced(sb, Game.Font, "RESULT", ColumnX[3], resultY + 4, head, TextSize.Tiny, 2f);
            Gfx.TextSpaced(sb, Game.Font, "CURTAIN CALL", ColumnX[3], curtainY + 4, head, TextSize.Tiny, 2f);
            Gfx.TextSpaced(sb, Game.Font, "EVENTS", ColumnX[1], eventsY, title, TextSize.Label, 3f);
            Gfx.Rect(sb, ColumnX[1], eventsY + 20, ColumnX[2] + ColumnW[2] - ColumnX[1], 1, Palette.LineGrey * (0.5f * a));

            //Bosses : the name, and its BIG ATTACK in grey after it
            for (int i = 0; i < bossNames.Count; i++)
            {
                Gfx.TextSpaced(sb, Game.Font, bossNames[i], ColumnX[0], bossY[i], title, TextSize.Label, 2f);
                Gfx.TextSpaced(sb, Game.Font, bossBigs[i], bossBigX[i], bossY[i] + 2, head, TextSize.Tiny, 2f);
            }

            //Words And Buttons : the one pointed at is lit (a white plate for a word, a white button for a test)
            for (int i = 0; i < items.Count; i++)
            {
                Item it = items[i];
                bool pointed = i == focus;
                if (it.Pick != Pick.None)
                {
                    bool chosen = Chosen(it);
                    Ui.Choice(sb, it.Box, it.Label, chosen, pointed, pointed || Input.MouseOver(it.Box), i * 7 + 3, a);
                    int order = it.Pick == Pick.Band ? bandPick.IndexOf(it.Value) : -1;
                    if (order >= 0) Gfx.TextSpacedCentered(sb, Game.Font, orderWords[order], it.Box.Center.X, it.Box.Bottom + 1, Palette.Paper * a, TextSize.Tiny, 0f);   // the order picked
                }
                else
                {
                    bool can = CanStart(it);
                    Ui.Button(sb, it.Box, "", "", pointed, can, a);                        // the plate, its word is drawn below at one size
                    Rectangle r = it.Box;
                    if (can && Input.MouseOver(r)) r.X += 4;                                // the plate steps forward under the mouse
                    Color ink = !can ? Palette.LineGrey * 0.5f : (pointed ? Palette.Ink : Palette.Paper);
                    Gfx.TextSpaced(sb, Game.Font, it.Label, r.Center.X - it.LabelWidth / 2f, r.Center.Y - Game.Font.LineSpacing * it.LabelScale / 2f,
                                   ink * a, it.LabelScale, 1.5f);
                }
            }

            //Footer
            Gfx.TextSpaced(sb, Game.Font, infoLine, Left, 616, head, TextSize.Tiny, 2f);
            Ui.Command(sb, backButton, "BACK", "ESC", backMotion, a, false);
            Gfx.TextSpacedRight(sb, Game.Font, "ARROWS  MOVE   /   ENTER OR CLICK  START   /   F2  BACK HERE FROM A TEST", 1240, 690, head, TextSize.Tiny, 2f);
        }
    }
}
