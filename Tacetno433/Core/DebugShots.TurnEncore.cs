using System;
using System.Collections.Generic;
using System.Globalization;
using Tacetno433.Battle;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //DebugShots.TurnEncore : DEVELOPER TOOL, the checks of ENCORE (Beta1 1 Nov, docs/BETA1_DIRECTION.md 27, in place of
    //MAESTRO MODE), part of --shots turncheck. A won run opens its conductor's next level (Data/Encore.cs), and each level adds
    //one rule on top of the ones under it (BattleRules ENCORE). ENCORE 1 (harder notes) is checked in DebugShots.Turn.cs.
    //Every line is PASS or FAIL like DebugShots.Turn.cs.
    public static partial class DebugShots
    {
        private static string TurnCheckEncore()
        {
            string report = "";
            List<Musician> band = new List<Musician> { MusicianList.All[0], MusicianList.All[1], MusicianList.All[2] };
            MusicianList.ResetRehearsals();

            //Ladder : THE UNHEARING (ConductorList.All 3) has nothing open, a first concert won opens ENCORE 1 and picks it,
            //ENCORE 1 won opens 2, a level played again opens nothing, a win at 4 opens the top (5), the top won opens nothing.
            //FANNY (8) is not touched. E steps up from the top back to 0, then to 1. A pick never goes over what is open.
            int[] keptWon = (int[])Encore.Won.Clone();
            int[] keptPick = (int[])Encore.Pick.Clone();
            for (int i = 0; i < Encore.Won.Length; i++) Encore.Won[i] = -1;
            Encore.PickAll();
            Conductor unhearing = ConductorList.All[3];
            RunState won = new RunState();
            won.Conductor = unhearing;
            string ladder = Encore.Open(unhearing).ToString();
            won.Encore = 0;
            ladder += " " + Encore.Win(won) + " " + Encore.Open(unhearing) + " " + Encore.Picked(unhearing);
            won.Encore = 1;
            ladder += " " + Encore.Win(won) + " " + Encore.Open(unhearing);
            won.Encore = 0;
            ladder += " " + Encore.Win(won) + " " + Encore.Open(unhearing);
            won.Encore = 4;
            ladder += " " + Encore.Win(won) + " " + Encore.Open(unhearing);
            won.Encore = BattleRules.EncoreTop;
            ladder += " " + Encore.Win(won) + " " + Encore.Open(unhearing) + " " + Encore.WonLevels(unhearing);
            ladder += " " + Encore.Open(ConductorList.All[8]);
            Encore.StepUp(unhearing);
            ladder += " " + Encore.Picked(unhearing);
            Encore.StepUp(unhearing);
            ladder += " " + Encore.Picked(unhearing);
            Encore.Choose(unhearing, 9);
            ladder += " " + Encore.Picked(unhearing);
            report += Same("encore ladder", "0 1 1 1 2 2 0 2 5 5 0 5 5 0 0 1 5", ladder);
            for (int i = 0; i < keptWon.Length; i++) Encore.Won[i] = keptWon[i];
            for (int i = 0; i < keptPick.Length; i++) Encore.Pick[i] = keptPick[i];

            //ENCORE 2 : the old MAESTRO timing. A swing 0.09 seconds off is PERFECT on a first concert (window 0.10) and GOOD from
            //ENCORE 2 (0.08). A roll of 5 shakes is PERFECT, then GOOD (6 for a PERFECT). ENCORE 1 alone leaves the timing as it is.
            string timing = "";
            int[] levels = { BattleRules.EncoreHits, BattleRules.EncoreTiming };
            for (int k = 0; k < levels.Length; k++)
            {
                TurnBattle t = new TurnBattle(band, new List<Enemy> { EnemyNamed("HUSH") }, 1, new Random(433));
                t.Encore = levels[k];
                timing += (k > 0 ? " " : "") + t.TightTiming;
            }
            for (int k = 0; k < 2; k++)
            {
                TurnBattle t = new TurnBattle(band, new List<Enemy> { EnemyNamed("HUSH") }, 1, new Random(433));
                t.Encore = k == 0 ? 0 : BattleRules.EncoreTiming;
                timing += " " + t.Judge(0.09f, true) + " " + t.RollGrade(5);
            }
            report += Same("encore 2 timing", "False True Perfect Perfect Good Good", timing);

            //ENCORE 3 : one more enemy in an ordinary fight, never more than four. Floor 1 early 1 (was 0), floor 1 elite 0 (an
            //ELITE keeps its escort), floor 2 early 2 (was 1), floor 3 late 3 (four already), floor 3 boss 2 (a boss keeps its own).
            //ENCORE 2 leaves floor 2 early at 1.
            RunState run = new RunState();
            run.Encore = BattleRules.EncorePacks;
            run.StagesThisFloor = 8;
            run.Floor = 1;
            run.Stage = 2;
            string packs = run.PackEscorts(EnemyKind.Normal) + " " + run.PackEscorts(EnemyKind.Elite);
            run.Floor = 2;
            packs += " " + run.PackEscorts(EnemyKind.Normal);
            run.Floor = 3;
            run.Stage = 7;
            packs += " " + run.PackEscorts(EnemyKind.Normal) + " " + run.PackEscorts(EnemyKind.Boss);
            run.Encore = BattleRules.EncorePacks - 1;
            run.Floor = 2;
            run.Stage = 2;
            packs += " / " + run.PackEscorts(EnemyKind.Normal);
            report += Same("encore 3 packs", "1 0 2 3 2 / 1", packs);

            //ENCORE 4 : the free rests heal EncoreRestShare of what they heal without it (REST's RECOVER, THE WINGS on the way in)
            run.Encore = BattleRules.EncoreRest - 1;
            string rest = run.RestShare.ToString("0.##", CultureInfo.InvariantCulture);
            run.Encore = BattleRules.EncoreRest;
            rest += " " + run.RestShare.ToString("0.##", CultureInfo.InvariantCulture);
            report += Same("encore 4 rest", "1 0.6", rest);

            //ENCORE 5 : PHASE 2 (THE TURN) starts at three quarters of a boss's life. One ATTACK takes REQUIEM from just over
            //three quarters to under it : THE TURN on ENCORE 5, none without it
            string phase = "";
            for (int k = 0; k < 2; k++)
            {
                TurnBattle t = new TurnBattle(band, new List<Enemy> { EnemyNamed("REQUIEM") }, 1, new Random(433));
                t.Encore = k == 0 ? BattleRules.EncoreBoss : 0;
                Sturdy(t);
                Fighter requiem = t.Enemies[0];
                requiem.Hp = (int)(requiem.MaxHp * BattleRules.EncoreBigPhase) + 1;
                t.PlanAttack(t.Party[0], requiem);
                t.PlayBar();
                t.Attack();
                phase += (k > 0 ? " " : "") + requiem.SecondHalf;
            }
            report += Same("encore 5 phase", "True False", phase);

            //APPLAUSE : a run won with 10 fights is 85 (10 x 3 + 3 floors x 10 + 25), each level pays a quarter more,
            //ENCORE 2 pays x1.5 as MAESTRO MODE did
            RunState paid = new RunState();
            paid.BattlesWon = 10;
            string applause = "";
            int[] payLevels = { 0, 1, 2, BattleRules.EncoreTop };
            for (int k = 0; k < payLevels.Length; k++)
            {
                paid.Encore = payLevels[k];
                applause += (k > 0 ? " " : "") + Archive.EchoesFor(paid, true);
            }
            report += Same("encore applause", "85 106 128 191", applause);

            return report;
        }
    }
}
