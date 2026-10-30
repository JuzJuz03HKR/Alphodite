using System;
using System.Collections.Generic;
using Tacetno433.Battle;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //DebugShots.TurnLegends : DEVELOPER TOOL, the checks of THE FIVE KITS (Beta1 29 Oct, docs/BETA1_DIRECTION.md 11.6),
    //part of --shots turncheck. The five legends' skills and traits : THE DEVIL'S BARGAIN, BREATH CONTROL, TURN OF THE HAND,
    //THE HUNTING CALL, THE LESSON, SHIELD, LANCE (THE SCAR switched off there) and CHAKRAM.
    //Like DebugShots.Turn.cs every line is PASS or FAIL with what was expected and what came out. The traits are on here.
    public static partial class DebugShots
    {
        private static string TurnCheckLegends(List<Musician> band, Enemy hush, Enemy deadAir, Enemy staticEnemy)
        {
            string report = "";
            NoteResult good = new NoteResult(Grade.Good, 1f);
            NoteResult[] twoGood = { good, good };
            NoteResult[] threeGood = { good, good, good };
            NoteResult[] fourGood = { good, good, good, good };
            Musician paganini = MusicianList.All[0];
            Musician quantz = MusicianList.All[1];
            Musician pfundt = MusicianList.All[2];
            Musician duport = MusicianList.All[6];
            Musician punto = MusicianList.All[7];

            //The Five Kits : each legend's two skills and trait, in their places (2k and 2k + 1, the save keeps the musicians' order)
            string kits = "";
            Musician[] five = { paganini, quantz, pfundt, duport, punto };
            for (int i = 0; i < five.Length; i++)
                kits += (i > 0 ? " / " : "") + SkillList.First(five[i]).Name + ", " + SkillList.Second(five[i]).Name + ", " + five[i].TraitName;
            report += Same("five kits", "CAPRICE, IL CANNONE, THE DEVIL'S BARGAIN / SOFT AIR, THE LESSON, BREATH CONTROL / TIMPANI ROLL, THUNDER CALL, TURN OF THE HAND / "
                                      + "SHIELD, LANCE, THE SCAR / FANFARE, CHAKRAM, THE HUNTING CALL", kits);

            //THE DEVIL'S BARGAIN (PAGANINI) : no BREATH and no HEARTLESS, PAGANINI can still write CAPRICE (2 x 6 = 12 life), QUANTZ
            //cannot write SOFT AIR. Played, it takes the 12 (45 -> 33).
            Skill caprice = SkillList.First(paganini);
            TurnBattle b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Breath = 0;
            string bargain = b.CanPlanSkill(b.Party[0], caprice) + " " + b.CanPlanSkill(b.Party[1], SkillList.First(quantz));
            b.PlanSkill(b.Party[0], caprice, b.Enemies[0]);
            b.PlayBar();
            bargain += " " + b.LifeCost(caprice);
            b.UseSkill(fourGood);
            report += Same("devil's bargain", "True False 12 33", bargain + " " + b.Party[0].Hp);

            //BREATH CONTROL (QUANTZ) : an ATTACK gives 2 + 1 BREATH (3 -> 6), PAGANINI's the plain 2 (3 -> 5)
            b = new TurnBattle(Solo(1), new List<Enemy> { hush }, 1, new Random(433));
            b.PlayBar();
            b.Attack();
            string control = b.Breath.ToString();
            b = new TurnBattle(Solo(0), new List<Enemy> { hush }, 1, new Random(433));
            b.PlayBar();
            b.Attack();
            report += Same("breath control", "6 5", control + " " + b.Breath);

            //TURN OF THE HAND (PFUNDT) : his ATTACK leaves a 3 note attack alone (HUSH hits 3 x 8 = 24), his TIMPANI ROLL takes a note
            //off it (3 -> 2, written down as a CUT), and an attack of 2 notes keeps both (TurnHandLeast)
            Skill roll = SkillList.First(pfundt);
            b = new TurnBattle(Solo(2), new List<Enemy> { hush }, 1, new Random(433));
            Sturdy(b);
            LongPhrase(b.Enemies[0], 3);
            b.PlayBar();
            b.Attack();
            string hand = b.Enemies[0].IntentNotes.Length + " " + b.TakeEnemyAttack(null);
            b.PlanSkill(b.Party[0], roll, b.Enemies[0]);
            b.PlayBar();
            LongPhrase(b.Enemies[0], 3);
            b.UseSkill(new NoteResult[] { good });
            hand += " " + b.Enemies[0].IntentNotes.Length + " " + CountEvents(b, TurnEventKind.Cut);
            b = new TurnBattle(Solo(2), new List<Enemy> { hush }, 1, new Random(433));
            b.Breath = 10;
            b.PlanSkill(b.Party[0], roll, b.Enemies[0]);
            b.PlayBar();
            LongPhrase(b.Enemies[0], 2);
            b.UseSkill(new NoteResult[] { good });
            report += Same("turn of hand", "3 24 2 1 2", hand + " " + b.Enemies[0].IntentNotes.Length);

            //THE HUNTING CALL (PUNTO) : PUNTO plays first (5 x 2 x 0.55 = 5.5 -> 6), PAGANINI right after hits x1.3 (7.15 -> 7), one CUE
            b = new TurnBattle(new List<Musician> { punto, paganini }, new List<Enemy> { hush }, 1, new Random(433));
            b.PlayBar();
            string call = b.Attack() + " " + b.Attack();
            report += Same("hunting call", "6 7 1", call + " " + CountEvents(b, TurnEventKind.Cue));

            //THE LESSON (QUANTZ) : played first at PAGANINI, whose CAPRICE then has a wider PERFECT window (0.13 s off is GOOD for
            //QUANTZ, PERFECT for PAGANINI) and plays x1.3 (each GOOD note 5 x 2 x 0.4 x 1.3 = 5.2 -> 5, HUSH 80 -> 60).
            //The lesson is used up, and QUANTZ can never teach himself.
            Skill lesson = SkillList.Second(quantz);
            b = new TurnBattle(new List<Musician> { quantz, paganini }, new List<Enemy> { hush }, 1, new Random(433));
            b.Breath = 10;
            b.PlanSkill(b.Party[0], lesson, b.Party[1]);
            b.PlanSkill(b.Party[1], caprice, b.Enemies[0]);
            b.PlayBar();
            string taught = b.Judge(0.13f, true).ToString();
            b.UseSkill(twoGood);
            taught += " " + b.Party[1].Lesson + " " + b.Judge(0.13f, true);
            b.UseSkill(fourGood);
            report += Same("the lesson", "Good True Perfect 60 False False", taught + " " + b.Enemies[0].Hp + " " + b.Party[1].Lesson
                                                                             + " " + b.CanAim(b.Party[0], lesson, b.Party[0]));

            //SHIELD (DUPORT) : never at himself. Held up in front of PAGANINI, HUSH's notes aimed at her land on DUPORT at half
            //(8 -> 4 each), PAGANINI keeps her 45, THE SCAR gives DUPORT 1 CRESCENDO, and his next turn ends the SHIELD.
            Skill shield = SkillList.First(duport);
            b = new TurnBattle(new List<Musician> { duport, paganini }, new List<Enemy> { hush }, 1, new Random(433));
            b.Breath = 10;
            string covered = b.CanAim(b.Party[0], shield, b.Party[0]).ToString();
            b.PlanSkill(b.Party[0], shield, b.Party[1]);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { good });
            b.Enemies[0].IntentTarget = b.Party[1];
            covered += " " + b.HitTarget(b.Enemies[0]).Name;
            b.Attack();                                                                // PAGANINI
            int notes = b.Current.IntentNotes.Length;
            covered += " " + b.TakeEnemyAttack(null) + " " + b.Party[1].Hp + " " + b.Party[0].Hp + " " + b.Party[0].Crescendo;
            b.PlayBar();                                                               // bar 2 : DUPORT is up first
            report += Same("shield", "False DUPORT " + (4 * notes) + " 45 " + (45 - 4 * notes) + " 1 False", covered + " " + (b.Party[1].CoveredBy != null));

            //LANCE (DUPORT, THE SCAR off so CRESCENDO stays out) : hit since his last move, 2 x (6 x 2 x 0.7 x 2 = 16.8 -> 17) = 34,
            //one x2 written down. Every note of the next attack parried PERFECT, so the next LANCE is plain, 2 x 8.4 -> 16.
            MusicianTrait scar = duport.Trait;
            duport.Trait = MusicianTrait.None;
            Skill lance = SkillList.Second(duport);
            b = new TurnBattle(Solo(6), new List<Enemy> { hush }, 1, new Random(433));
            Tough(b);
            b.PlayBar();
            b.Attack();
            b.TakeEnemyAttack(null);
            b.PlanSkill(b.Party[0], lance, b.Enemies[0]);
            b.PlayBar();
            int before = b.Enemies[0].Hp;
            b.UseSkill(twoGood);
            string lanced = (before - b.Enemies[0].Hp).ToString();
            b.TakeEnemyAttack(AllGrades(b.Current.IntentNotes.Length, Grade.Perfect));
            b.PlanSkill(b.Party[0], lance, b.Enemies[0]);
            b.PlayBar();
            before = b.Enemies[0].Hp;
            b.UseSkill(twoGood);
            duport.Trait = scar;
            report += Same("lance", "34 16 1", lanced + " " + (before - b.Enemies[0].Hp) + " " + CountEvents(b, TurnEventKind.Revenge));

            //CHAKRAM (PUNTO) : picked at HUSH, it flies down the line, one enemy a throw (5 x 2 x 1 = 10 each). Against HUSH alone it
            //comes back for half on the second and third throws, 10 + 5 + 5 = 20.
            Skill chakram = SkillList.Second(punto);
            b = new TurnBattle(Solo(7), new List<Enemy> { hush, deadAir, staticEnemy }, 1, new Random(433));
            Tough(b);
            b.Breath = 10;
            b.PlanSkill(b.Party[0], chakram, b.Enemies[0]);
            b.PlayBar();
            b.UseSkill(threeGood);
            string thrown = (999 - b.Enemies[0].Hp) + " " + (999 - b.Enemies[1].Hp) + " " + (999 - b.Enemies[2].Hp);
            b = new TurnBattle(Solo(7), new List<Enemy> { hush }, 1, new Random(433));
            Tough(b);
            b.Breath = 10;
            b.PlanSkill(b.Party[0], chakram, b.Enemies[0]);
            b.PlayBar();
            b.UseSkill(threeGood);
            report += Same("chakram", "10 10 10 20", thrown + " " + (999 - b.Enemies[0].Hp));

            report += TurnCheckMoves();                                                // ENEMY MOVES (Beta1 30 Oct)
            report += TurnCheckPaths();                                                // PATH and INSPIRED (Beta1 30 Oct)
            report += TurnCheckPreview();                                              // PREVIEW (Beta1 30 Oct, block 1 item 4)
            return report;
        }

        //Long Phrase : the enemy's next attack is this many notes of 8 (so TURN OF THE HAND has something to cut)
        private static void LongPhrase(Fighter enemy, int notes)
        {
            enemy.IntentNotes = new int[notes];
            enemy.IntentWays = new Flick[notes];
            enemy.IntentLate = new bool[notes];
            for (int i = 0; i < notes; i++)
            {
                enemy.IntentNotes[i] = 8;
                enemy.IntentWays[i] = Flick.Down;
            }
        }

        //Count Events : how many of this kind the battle has written down so far
        private static int CountEvents(TurnBattle b, TurnEventKind kind)
        {
            int count = 0;
            for (int i = 0; i < b.Events.Count; i++)
                if (b.Events[i].Kind == kind) count++;
            return count;
        }
    }
}
