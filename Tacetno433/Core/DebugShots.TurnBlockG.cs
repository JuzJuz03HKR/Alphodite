#if DEBUG
// BLOCK H (2 Nov, H2) : the capture and test tool is the team's, it builds only in Debug. A Release build (the game that
// ships) gets the small stand in of Core/DebugShots.Release.cs instead, everything off.
using System;
using System.Collections.Generic;
using System.Globalization;
using Tacetno433.Battle;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //DebugShots.TurnBlockG : DEVELOPER TOOL, the checks of BLOCK G (Beta1 2 Nov, docs/BETA1_DIRECTION.md 46), part of
    //--shots turncheck. FINALE's real cost and what it does now (every CHARGE broken, FORTE), the RUSH note (the band's RUSH
    //TAIL, TACET's deeper attacks, THE SILENCE GROWS), a HOLD parried, COMBO's little x and SOLO.
    //Like DebugShots.Turn.cs every line is PASS or FAIL with what was expected and what came out.
    public static partial class DebugShots
    {
        private static string TurnCheckBlockG(List<Musician> band, Enemy hush)
        {
            string report = "";
            MusicianList.ResetRehearsals();                                             // earlier checks rehearse some musicians
            NoteResult[] fourPerfect = { new NoteResult(Grade.Perfect, 1f), new NoteResult(Grade.Perfect, 1f),
                                         new NoteResult(Grade.Perfect, 1f), new NoteResult(Grade.Perfect, 1f) };

            //FINALE : the real cost, the playtest one is off (the user 13.124 : "set what fits")
            TurnBattle b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            report += Same("finale need", "10 0 10", BattleRules.TurnSignatureNeed + " " + BattleRules.PlaytestSignatureNeed + " " + b.SignatureNeed);

            //FINALE : it breaks every CHARGE (FUGUE does no damage, so the FINALE itself breaks it) and FORTE begins for this turn
            Enemy deadAir = EnemyNamed("DEAD AIR");
            b = new TurnBattle(band, new List<Enemy> { deadAir, hush }, 1, new Random(433));
            Tough(b);
            b.Enemies[0].Charge = 1;                                                    // wound up
            b.Enemies[0].IntentCharge = true;
            b.Kit = TurnKit.Bach;
            b.Signature = b.SignatureNeed;
            b.UseSignature(fourPerfect);
            report += Same("finale breaks", "True False 1", b.Enemies[0].ChargeBroken + " " + b.Enemies[0].IntentCharge + " " + b.Forte);

            //FORTE : the band's moves of this turn, the next turn plays as usual
            int round = b.Round;
            for (int i = 0; i < 60 && b.Round == round && b.Step != TurnStep.Won && b.Step != TurnStep.Lost; i++) Step(b, Grade.Good);
            report += Same("forte turns", "0 " + (round + 1), b.Forte + " " + b.Round);

            //FORTE : every hit of the band x1.25 (PAGANINI 5 x 2 x 4 = 40 -> 50), TACET's hits as they were
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            Tough(b);
            int plain = b.HitDamage(b.Party[0], b.Enemies[0], 4f);
            int theirs = b.HitDamage(b.Enemies[0], b.Party[0], 4f);
            b.Forte = 2;
            report += Same("forte hits", "40 50 " + theirs, plain + " " + b.HitDamage(b.Party[0], b.Enemies[0], 4f) + " " + b.HitDamage(b.Enemies[0], b.Party[0], 4f));

            //COMBO : x1.05 a step of 10 PERFECTs in a row, 3 steps at most
            report += Same("combo scale", "1 1 1.05 1.1 1.15 1.15",
                F2(TurnBattle.ComboScaleAt(0)) + " " + F2(TurnBattle.ComboScaleAt(9)) + " " + F2(TurnBattle.ComboScaleAt(10)) + " "
                + F2(TurnBattle.ComboScaleAt(25)) + " " + F2(TurnBattle.ComboScaleAt(30)) + " " + F2(TurnBattle.ComboScaleAt(99)));

            //RUSH TAIL : CAPRICE (L R L R) has 4 notes on floor 1, 5 on floor 2, 6 on floor 3, the RUSH notes going back and forth
            //from its last arrow (Right) : Left, then Right. SOFT AIR (a HEAL) never gets one, nor IL CANNONE (one big shot).
            Skill caprice = SkillList.First(MusicianList.All[0]);
            Skill softAir = SkillList.First(MusicianList.All[1]);
            TurnBattle f1 = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            TurnBattle f2 = new TurnBattle(band, new List<Enemy> { hush }, 2, new Random(433));
            TurnBattle f3 = new TurnBattle(band, new List<Enemy> { hush }, 3, new Random(433));
            Skill cannone = SkillList.Second(MusicianList.All[0]);
            report += Same("rush tail", "4 5 6 Rush Left Right 2 1",
                f1.NoteCount(f1.Party[0], caprice) + " " + f2.NoteCount(f2.Party[0], caprice) + " " + f3.NoteCount(f3.Party[0], caprice) + " "
                + f3.NoteAt(f3.Party[0], caprice, 4).Kind + " " + f3.NoteAt(f3.Party[0], caprice, 4).Way + " " + f3.NoteAt(f3.Party[0], caprice, 5).Way + " "
                + f3.NoteCount(f3.Party[1], softAir) + " " + f3.NoteCount(f3.Party[0], cannone));

            //RUSH TAIL : on floor 2 CAPRICE's fifth note (a RUSH) does 40 percent of a note (5.2 -> 5, 2.1 -> 2). PAGANINI alone, every note PERFECT.
            report += Same("rush share", "5 5 5 5 2", SkillHits(2, caprice, 5));

            //THE SILENCE GROWS : on floor 1 TACET's attacks are plain swipes, deeper down they bring RUSH, HOLD (x1.5) and ROLL
            //(THE ROUND opens with D D D : 2 notes on floor 1, 3 deeper. WHITE NOISE's L R L R and one more, NO REST : 5 on floor 3)
            Enemy round3 = EnemyNamed("THE ROUND");
            report += Same("silence floor 1", "Swipe Swipe / 10 10", KindsOf(new TurnBattle(band, new List<Enemy> { round3 }, 1, new Random(433)).Enemies[0]));
            report += Same("silence elite 2", "Swipe Rush Hold / 14 14 21", KindsOf(new TurnBattle(band, new List<Enemy> { round3 }, 2, new Random(433)).Enemies[0]));
            report += Same("silence elite 3", "Swipe Rush Hold / 17 17 26", KindsOf(new TurnBattle(band, new List<Enemy> { round3 }, 3, new Random(433)).Enemies[0]));
            report += Same("silence roll 3", "Roll Swipe Rush Swipe Hold", KindsOnly(new TurnBattle(band, new List<Enemy> { EnemyNamed("WHITE NOISE") }, 3, new Random(433)).Enemies[0]));
            report += Same("silence small 2", "Swipe Rush / 11 11", KindsOf(new TurnBattle(band, new List<Enemy> { hush }, 2, new Random(433)).Enemies[0]));

            //HOLD parried : kept down to the end blocks it all, half of it lets half through, less lets all of it through
            report += Same("hold parry", "Perfect Good Miss Good Miss", f1.HeldGrade(Grade.Perfect, 0.9f) + " " + f1.HeldGrade(Grade.Perfect, 0.5f) + " "
                + f1.HeldGrade(Grade.Perfect, 0.2f) + " " + f1.HeldGrade(Grade.Good, 1f) + " " + f1.HeldGrade(Grade.Miss, 1f));

            //SOLO : PAGANINI alone with 4 of their own PERFECTs in a row, one more is the SOLO. Their next CAPRICE is the solo : two
            //RUSH notes more (6), x1.3 (4 x 6.8 -> 7 and 2 x 2.7 -> 3 = 34), and it is spent (the solo's own notes do not count toward the next one)
            b = new TurnBattle(Solo(0), new List<Enemy> { hush }, 1, new Random(433));
            Tough(b);
            b.Breath = 10;
            Fighter p = b.Party[0];
            b.PlanSkill(p, caprice, b.Enemies[0]);
            b.PlayBar();
            p.SoloRun = BattleRules.TurnSoloNeed - 1;
            b.UseSkill(AllNotes(b.NoteCount(p, caprice), Grade.Perfect));
            string earned = p.Solo + " " + b.NoteCount(p, caprice);
            for (int i = 0; i < 60 && b.Step != TurnStep.Plan && b.Step != TurnStep.Won && b.Step != TurnStep.Lost; i++) Step(b, Grade.Good);
            int before = b.Enemies[0].Hp;
            b.Breath = 10;
            b.PlanSkill(p, caprice, b.Enemies[0]);
            b.PlayBar();
            b.UseSkill(AllNotes(b.NoteCount(p, caprice), Grade.Perfect));
            report += Same("solo", "True 6 / 34 False 0", earned + " / " + (before - b.Enemies[0].Hp) + " " + p.Solo + " " + p.SoloRun);

            //SOLO : an ATTACK played as the solo hits twice as hard (PAGANINI's ATTACK 6 -> 11)
            b = new TurnBattle(Solo(0), new List<Enemy> { hush }, 1, new Random(433));
            Tough(b);
            b.Party[0].Solo = true;
            b.PlayBar();
            int solo = b.Attack();
            report += Same("solo attack", "11 False", solo + " " + b.Party[0].Solo);
            return report;
        }

        //Skill Hits : musician 0 alone on this floor plays the skill with every note PERFECT at a tough HUSH, the damage of each note
        private static string SkillHits(int floor, Skill s, int notes)
        {
            TurnBattle b = new TurnBattle(Solo(0), new List<Enemy> { EnemyNamed("HUSH") }, floor, new Random(433));
            Tough(b);
            b.Breath = 10;
            b.PlanSkill(b.Party[0], s, b.Enemies[0]);
            b.PlayBar();
            b.BeginSkill();
            string hits = "";
            for (int n = 0; n < notes; n++)
            {
                int before = b.Enemies[0].Hp;
                b.SkillNote(n, new NoteResult(Grade.Perfect, 1f));
                hits += (hits.Length > 0 ? " " : "") + (before - b.Enemies[0].Hp);
            }
            b.EndSkill();
            return hits;
        }

        //Kinds Of : the kinds of the notes an enemy plans now, then how hard each note hits
        private static string KindsOf(Fighter enemy)
        {
            string kinds = "";
            string hits = "";
            for (int i = 0; i < enemy.IntentNotes.Length; i++)
            {
                NoteKind kind = i < enemy.IntentKinds.Length ? enemy.IntentKinds[i] : NoteKind.Swipe;
                kinds += (i > 0 ? " " : "") + kind;
                hits += (i > 0 ? " " : "") + enemy.IntentNotes[i];
            }
            return enemy.IntentNotes.Length > 1 || enemy.IntentKinds.Length > 0 ? kinds + " / " + hits : kinds;
        }

        //Kinds Only : the kinds of the notes an enemy plans now
        private static string KindsOnly(Fighter enemy)
        {
            string kinds = "";
            for (int i = 0; i < enemy.IntentNotes.Length; i++)
                kinds += (i > 0 ? " " : "") + (i < enemy.IntentKinds.Length ? enemy.IntentKinds[i] : NoteKind.Swipe);
            return kinds;
        }

        //All Notes : this many notes, each swiped this well
        private static NoteResult[] AllNotes(int count, Grade grade)
        {
            NoteResult[] all = new NoteResult[count];
            for (int i = 0; i < count; i++) all[i] = new NoteResult(grade, 1f);
            return all;
        }

        //F2 : a scale with at most two places, "1" "1.05" "1.1"
        private static string F2(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
#endif
