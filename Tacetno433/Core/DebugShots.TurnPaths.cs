using System;
using System.Collections.Generic;
using Tacetno433.Battle;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //DebugShots.TurnPaths : DEVELOPER TOOL, the checks of PATH, INSPIRED, SIGNATURE GROWS and BONDS (Beta1 30 Oct, docs/BETA1_DIRECTION.md 13), part
    //of --shots turncheck. Every path of the five legends walked to its end (Data/Path.cs), the floors that open the steps,
    //and INSPIRED. Like DebugShots.Turn.cs every line is PASS or FAIL with what was expected and what came out. The musicians'
    //traits are on here. Every musician is back to no step at the end (MusicianList.ResetRehearsals).
    public static partial class DebugShots
    {
        private static string TurnCheckPaths()
        {
            string report = "";
            NoteResult good = new NoteResult(Grade.Good, 1f);
            NoteResult perfect = new NoteResult(Grade.Perfect, 1f);
            Enemy hush = EnemyList.All[0];
            Enemy deadAir = EnemyList.All[1];
            Enemy staticEnemy = EnemyList.All[3];
            Musician paganini = MusicianList.All[0];
            Musician quantz = MusicianList.All[1];
            Musician pfundt = MusicianList.All[2];
            Musician duport = MusicianList.All[6];
            Musician punto = MusicianList.All[7];
            MusicianList.ResetRehearsals();

            //Steps Open : step I on floor 1, II from floor 2. Taking one rewrites the skill (THE VIRTUOSO's OP. 1 : 5 cuts),
            //the second waits for floor 2, and the plain skill in SkillList is never touched.
            string open = PathList.StepOpen(paganini, 1, 1) + " " + PathList.Take(paganini, 1, 1) + " " + SkillList.First(paganini).Notes.Length
                          + " " + PathList.StepOpen(paganini, 1, 1) + " " + PathList.StepOpen(paganini, 1, 2) + " " + SkillList.Plain(paganini, 0).Notes.Length;
            PathList.Reset(paganini);
            report += Same("path floors", "True True 5 False True 4 4", open + " " + SkillList.First(paganini).Notes.Length);

            //INSPIRED : CAPRICE's 4 cuts PERFECT make PAGANINI INSPIRED (a step waits), the next bar picks THE DEVIL's THE PACT
            Walk(paganini, 0, 0);
            TurnBattle b = new TurnBattle(Solo(0), new List<Enemy> { hush }, 1, new Random(433));
            Tough(b);
            b.Breath = 10;
            b.PlanSkill(b.Party[0], SkillList.First(paganini), b.Enemies[0]);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { perfect, perfect, perfect, perfect });
            Fighter pag = b.Party[0];
            string inspired = pag.Perfects + " " + pag.Inspired + " " + pag.InspirePending + " " + EventAmount(b, TurnEventKind.Inspired);
            b.TakeEnemyAttack(null);
            inspired += " " + (b.InspiredWaiting == pag) + " " + b.TakeStep(pag, 0) + " " + paganini.PathLevel[0] + " " + pag.InspirePending + " " + paganini.Has(PathRule.PaidInBlood);
            report += Same("inspired", "4 True True 1 True True 1 False True", inspired);

            //INSPIRED with no step open (both steps of floor 1 taken) : 2 BREATH instead (3 -> 1 after CAPRICE -> 3)
            Walk(paganini, 1, 1);
            b = new TurnBattle(Solo(0), new List<Enemy> { hush }, 1, new Random(433));
            Tough(b);
            b.PlanSkill(b.Party[0], SkillList.First(paganini), b.Enemies[0]);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { perfect, perfect, perfect, perfect, perfect });
            report += Same("inspired breath", "False 0 " + (3 - 2 + BattleRules.TurnInspiredBreath), b.Party[0].InspirePending + " " + EventAmount(b, TurnEventKind.Inspired) + " " + b.Breath);
            MusicianList.ResetRehearsals();

            //THE DEVIL : THE PACT, CAPRICE costs 4 life and plays x1.3 (each GOOD cut 4 x 1.3 = 5.2 -> 5), DIABOLUS 2 DISSONANCE a cut
            Walk(paganini, 0, 2);
            b = new TurnBattle(Solo(0), new List<Enemy> { hush }, 1, new Random(433));
            Tough(b);
            b.Breath = 10;
            b.PlanSkill(b.Party[0], SkillList.First(paganini), b.Enemies[0]);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { good, good, good, good });
            report += Same("the pact", "20 8 1 4", (999 - b.Enemies[0].Hp) + " " + b.Enemies[0].Dissonance + " " + CountPath(b, PathRule.PaidInBlood) + " " + (999 - b.Party[0].Hp));

            //THE DEVIL'S OWN : IL CANNONE at HUSH also hits STATIC (2 DISSONANCE), not DEAD AIR (none). 5 x 2 x 1.8 x 1.4 x 1.3 (THE PACT) = 32.76 -> 33 held GOOD to the end.
            Walk(paganini, 0, 3);
            b = new TurnBattle(Solo(0), new List<Enemy> { hush, staticEnemy, deadAir }, 1, new Random(433));
            Tough(b);
            b.Breath = 10;
            b.Enemies[1].Dissonance = 2;
            b.PlanSkill(b.Party[0], SkillList.Second(paganini), b.Enemies[0]);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { good });
            report += Same("devil's own", "33 33 0", (999 - b.Enemies[0].Hp) + " " + (999 - b.Enemies[1].Hp) + " " + (999 - b.Enemies[2].Hp));
            MusicianList.ResetRehearsals();

            //THE VIRTUOSO walked to its end : CAPRICE No. 24, 6 cuts all one way at 0.5 (ON ONE STRING : 5 x 2 x 0.5 = 5), the last x3 (5 x 5 + 15 = 40)
            Walk(paganini, 1, 3);
            Skill capriceNow = SkillList.First(paganini);
            b = new TurnBattle(Solo(0), new List<Enemy> { hush }, 1, new Random(433));
            Tough(b);
            b.Breath = 10;
            b.PlanSkill(b.Party[0], capriceNow, b.Enemies[0]);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { good, good, good, good, good, good });
            string ways = capriceNow.Notes.Length + " " + capriceNow.Notes[0].Way + " " + capriceNow.Notes[5].Way;
            report += Same("caprice no. 24", "6 Left Left 40 1", ways + " " + (999 - b.Enemies[0].Hp) + " " + CountPath(b, PathRule.LastCut));
            MusicianList.ResetRehearsals();

            //THE TEACHER walked to its end : THE LESSON reaches both friends at once (THE CLASS), each x1.6 (THE METHOD) and an ECHO
            //of half waits on each (EVERY EVENING). QUANTZ never teaches himself.
            Walk(quantz, 0, 3);
            b = new TurnBattle(new List<Musician> { quantz, paganini, pfundt }, new List<Enemy> { hush }, 1, new Random(433));
            Tough(b);
            b.Breath = 10;
            b.PlanSkill(b.Party[0], SkillList.Second(quantz), null);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { good, good });
            string taught = b.Party[0].Lesson + " " + b.Party[1].Lesson + " " + b.Party[2].Lesson + " " + b.Party[1].LessonScale + " " + b.Party[2].Echo;
            report += Same("the teacher", "False True True 1.6 0.5 3", taught + " " + SkillList.Second(quantz).Cost);

            //THE COURT walked to its end : SOFT AIR 3 notes of 4 (12 back) and SANSSOUCI clears DISSONANCE and MUTE
            Walk(quantz, 1, 3);
            b = new TurnBattle(new List<Musician> { quantz, paganini }, new List<Enemy> { hush }, 1, new Random(433));
            b.Breath = 10;
            b.Party[1].Hp = 20;
            b.Party[1].Dissonance = 3;
            b.Party[1].Mute = 1;
            b.PlanSkill(b.Party[0], SkillList.First(quantz), null);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { good, good, good });
            report += Same("the court", "32 0 0 3", b.Party[1].Hp + " " + b.Party[1].Dissonance + " " + b.Party[1].Mute + " " + SkillList.First(quantz).Cost);
            MusicianList.ResetRehearsals();

            //THE CRANK walked to its end : TIMPANI ROLL takes 2 notes (DIE PAUKEN) off a 4 note attack down to 2, THUNDER CALL then
            //takes 1 off every enemy (RETUNED, one whatever DIE PAUKEN says), down to 1 at least (ONE TURN) : HUSH 2 -> 1, DEAD AIR 3 -> 2.
            Walk(pfundt, 0, 3);
            b = new TurnBattle(Solo(2), new List<Enemy> { hush, deadAir }, 1, new Random(433));
            Tough(b);
            b.Breath = 10;
            b.PlanSkill(b.Party[0], SkillList.First(pfundt), b.Enemies[0]);
            b.PlayBar();
            LongPhrase(b.Enemies[0], 4);
            b.UseSkill(new NoteResult[] { good });
            string crank = b.Enemies[0].IntentNotes.Length.ToString();
            while (b.Step != TurnStep.Plan) b.TakeEnemyAttack(null);
            b.PlanSkill(b.Party[0], SkillList.Second(pfundt), null);
            b.PlayBar();
            LongPhrase(b.Enemies[0], 2);
            LongPhrase(b.Enemies[1], 3);
            b.UseSkill(new NoteResult[] { good });
            report += Same("the crank", "2 1 2", crank + " " + b.Enemies[0].IntentNotes.Length + " " + b.Enemies[1].IntentNotes.Length);
            MusicianList.ResetRehearsals();

            //THE THUNDER walked to its end : TIMPANI ROLL hits every enemy (THE STORM), PERFECT throws the first one OFF-BEAT at once
            //(THUNDERCLAP), so HUSH loses its turn of this bar and DEAD AIR only fills its gauge (40, FORTISSIMO). THE STORM costs 3.
            Walk(pfundt, 1, 3);
            b = new TurnBattle(Solo(2), new List<Enemy> { hush, deadAir }, 1, new Random(433));
            Tough(b);
            b.Breath = 10;
            b.PlanSkill(b.Party[0], SkillList.First(pfundt), null);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { perfect });
            report += Same("the thunder", "40 3 1 1", SkillList.First(pfundt).OffBeat + " " + SkillList.First(pfundt).Cost + " " + CountEvents(b, TurnEventKind.Break) + " " + CountEvents(b, TurnEventKind.Skip));
            MusicianList.ResetRehearsals();

            //THE SHIELD walked to its end : SHIELD stands in front of both friends (THE WALL), takes a third of each note (8 -> 3,
            //STRADIVARIUS) and the blows strike HUSH back as hard (NAPOLEON'S DENT)
            Walk(duport, 0, 3);
            b = new TurnBattle(new List<Musician> { duport, paganini, quantz }, new List<Enemy> { hush }, 1, new Random(433));
            Tough(b);
            b.Breath = 10;
            b.PlanSkill(b.Party[0], SkillList.First(duport), null);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { good });
            string wall = (b.Party[1].CoveredBy == b.Party[0]) + " " + (b.Party[2].CoveredBy == b.Party[0]);
            b.Enemies[0].IntentTarget = b.Party[1];
            while (b.Step == TurnStep.PlayerTurn) b.Attack();
            int notes = b.Current.IntentNotes.Length;
            int hushBefore = b.Enemies[0].Hp;
            b.TakeEnemyAttack(null);
            wall += " " + (999 - b.Party[0].Hp) + " " + (hushBefore - b.Enemies[0].Hp);
            report += Same("the shield", "True True " + (3 * notes) + " " + (3 * notes), wall);
            MusicianList.ResetRehearsals();

            //THE SCAR walked to its end : LANCE 3 thrusts, x3 after a hit (3 x 6 x 2 x 0.7 x 3 = 25.2 -> 25 each = 75), and THE SCAR gave
            //2 CRESCENDO for the attack that hurt him (CRESCENDO 2 makes each thrust x1.3 : 6 x 2 x 0.7 x 3 x 1.3 = 32.76 -> 33, 99)
            Walk(duport, 1, 3);
            b = new TurnBattle(Solo(6), new List<Enemy> { hush }, 1, new Random(433));
            Tough(b);
            b.Breath = 10;
            b.PlayBar();
            b.Attack();
            b.TakeEnemyAttack(null);
            string scar = SkillList.Second(duport).Notes.Length + " " + b.Party[0].Crescendo;
            b.PlanSkill(b.Party[0], SkillList.Second(duport), b.Enemies[0]);
            b.PlayBar();
            int before = b.Enemies[0].Hp;
            b.UseSkill(new NoteResult[] { good, good, good });
            report += Same("the scar", "3 2 99", scar + " " + (before - b.Enemies[0].Hp));
            MusicianList.ResetRehearsals();

            //THE HUNT : PUNTO first, the two after both get the CUE (THE WHOLE HUNT) at x1.5 (TALLY-HO) : PAGANINI 5.5 x 1.5 = 8.25 -> 8,
            //QUANTZ 4.4 x 1.5 = 6.6 -> 7
            Walk(punto, 0, 2);
            b = new TurnBattle(new List<Musician> { punto, paganini, quantz }, new List<Enemy> { hush }, 1, new Random(433));
            Tough(b);
            b.PlayBar();
            string hunt = b.Attack() + " " + b.Attack() + " " + b.Attack();
            report += Same("the hunt", "6 8 7 2", hunt + " " + CountEvents(b, TurnEventKind.Cue));
            MusicianList.ResetRehearsals();

            //THE CHAKRAM walked to its end : 4 throws at HUSH alone, the second to fourth whole (THE CATCH, 10 each), the last one at
            //every enemy (WHIRLWIND) : with HUSH and DEAD AIR, HUSH takes 10 + 10 (+ the last 10), DEAD AIR 10 + 10
            Walk(punto, 1, 3);
            b = new TurnBattle(Solo(7), new List<Enemy> { hush }, 1, new Random(433));
            Tough(b);
            b.Breath = 10;
            b.PlanSkill(b.Party[0], SkillList.Second(punto), b.Enemies[0]);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { good, good, good, good });
            string thrown = (999 - b.Enemies[0].Hp).ToString();
            b = new TurnBattle(Solo(7), new List<Enemy> { hush, deadAir }, 1, new Random(433));
            Tough(b);
            b.Breath = 10;
            b.PlanSkill(b.Party[0], SkillList.Second(punto), b.Enemies[0]);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { good, good, good, good });
            report += Same("the chakram", "40 30 20", thrown + " " + (999 - b.Enemies[0].Hp) + " " + (999 - b.Enemies[1].Hp));

            //SIGNATURE GROWS : 4 GOOD strokes (strength 1). LIGHTNING strikes HUSH once a bolt of 14 on floor 1, STORM twice on floor 2,
            //three times on floor 3 / THE ART OF FUGUE's echo is still there after one move on floor 2 / DAS JAHR gives CRESCENDO 2 on
            //floor 2, and on floor 3 strikes 12 x 1.5 = 18
            string grows = "";
            NoteResult[] strokes = { good, good, good, good };
            for (int floor = 1; floor <= 3; floor++)
            {
                b = new TurnBattle(Solo(0), new List<Enemy> { hush }, floor, new Random(433));
                Tough(b);
                b.Kit = TurnKit.Beethoven;
                b.Signature = b.SignatureNeed;
                b.UseSignature(strokes);
                grows += (floor > 1 ? " " : "") + (999 - b.Enemies[0].Hp);
            }
            b = new TurnBattle(Solo(0), new List<Enemy> { hush }, 2, new Random(433));
            Tough(b);
            b.Kit = TurnKit.Bach;
            b.Signature = b.SignatureNeed;
            b.UseSignature(strokes);
            b.PlayBar();
            b.Attack();
            grows += " " + (b.Party[0].Echo > 0f);
            b = new TurnBattle(Solo(0), new List<Enemy> { hush }, 2, new Random(433));
            Tough(b);
            b.Kit = TurnKit.Fanny;
            b.Signature = b.SignatureNeed;
            b.UseSignature(strokes);
            grows += " " + b.Party[0].Crescendo;
            b = new TurnBattle(Solo(0), new List<Enemy> { hush }, 3, new Random(433));
            Tough(b);
            b.Kit = TurnKit.Fanny;
            b.Signature = b.SignatureNeed;
            b.UseSignature(strokes);
            grows += " " + (999 - b.Enemies[0].Hp);
            report += Same("signature grows", "14 28 42 True 2 18", grows);

            //BONDS : with Bach, QUANTZ's first move of the fight is x1.5 (4 x 2 x 0.55 x 1.5 = 6.6 -> 7, the second 4.4 -> 4), with
            //Beethoven plain (4). PAGANINI's ATTACK on THE DEVIL'S STRING (floor 1, x1.5) : 5 x 2 x 0.55 x 1.5 = 8.25 -> 8
            Enemy devil = EnemyNamed("THE DEVIL'S STRING");
            b = new TurnBattle(Solo(1), new List<Enemy> { hush }, 1, new Random(433));
            Tough(b);
            b.Kit = TurnKit.Bach;
            b.PlayBar();
            string bonds = b.Attack().ToString();
            b.TakeEnemyAttack(null);
            b.PlayBar();
            bonds += " " + b.Attack();
            b = new TurnBattle(Solo(1), new List<Enemy> { hush }, 1, new Random(433));
            Tough(b);
            b.Kit = TurnKit.Beethoven;
            b.PlayBar();
            bonds += " " + b.Attack();
            b = new TurnBattle(Solo(0), new List<Enemy> { devil }, 1, new Random(433));
            b.PlayBar();
            bonds += " " + b.Attack();
            report += Same("bonds", "7 4 4 8", bonds);

            MusicianList.ResetRehearsals();
            return report;
        }

        //Walk : the musician has walked this many steps of path p (the floor does not matter here)
        private static void Walk(Musician m, int p, int steps)
        {
            m.PathLevel[p] = steps;
            PathList.Rebuild(m);
        }

        //Count Path : how many times a PATH rule was written down as working
        private static int CountPath(TurnBattle b, PathRule rule)
        {
            int count = 0;
            for (int i = 0; i < b.Events.Count; i++)
                if (b.Events[i].Kind == TurnEventKind.Path && b.Events[i].Amount == (int)rule) count++;
            return count;
        }
    }
}
