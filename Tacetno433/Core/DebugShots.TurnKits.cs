using System;
using System.Collections.Generic;
using Tacetno433.Battle;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //DebugShots.TurnKits : DEVELOPER TOOL, the checks of step 10 (Alpha 7), part of --shots turncheck.
    //The four new conductor kits (THE METRONOME, THE HEARTLESS, THE KING'S BATON, FANNY MENDELSSOHN AND ???, once THE UNSIGNED),
    //the musician traits in the turn battle, and the intent that is decided right after an attack.
    //THE FIVE KITS (Beta1 29 Oct) : the five legends' new traits and skills are checked in DebugShots.TurnLegends.cs.
    //Like DebugShots.Turn.cs every line is PASS or FAIL with what was expected and what came out.
    public static partial class DebugShots
    {
        private static string TurnCheckStep10(List<Musician> band, Enemy hush, Enemy deadAir)
        {
            string report = "";
            MusicianList.ResetRehearsals();                                             // earlier checks rehearse some musicians
            Enemy staticEnemy = EnemyNamed("STATIC");
            NoteResult[] fourPerfect = { new NoteResult(Grade.Perfect, 1f), new NoteResult(Grade.Perfect, 1f),
                                         new NoteResult(Grade.Perfect, 1f), new NoteResult(Grade.Perfect, 1f) };

            //Conductors : the turn battle's gallery shows the three the user kept (Beta1, 29 Oct), Beethoven, Fanny, Bach
            report += Same("conductors shown", "3 THE UNHEARING FANNY MENDELSSOHN AND ??? THE METRONOME",
                ConductorList.Shown.Length + " " + ConductorList.Shown[0].Name + " " + ConductorList.Shown[1].Name + " " + ConductorList.Shown[2].Name);

            //Intent : decided again right after an attack, so HUSH now shows its second phrase (L R D, 2 notes on floor 1)
            TurnBattle b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            PlayToEnemy(b);
            b.TakeEnemyAttack(null);
            report += Same("intent after attack", "Left Right 1 True",
                b.Enemies[0].IntentWays[0] + " " + b.Enemies[0].IntentWays[1] + " " + b.Enemies[0].Attacks + " " + b.Enemies[0].Planned);

            //LOCKED TEMPO (THE METRONOME, Beta1 29 Oct) : the band's own swipes get a wider PERFECT window. A swipe half
            //way into the extra time is GOOD for anyone else and PERFECT for him (a fresh battle, so no musician's trait counts).
            TurnBattle tempo = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            float off = BattleRules.PerfectWindow + BattleRules.TurnLockedTempoWindow * 0.5f;
            Grade others = tempo.Judge(off, true);
            tempo.Kit = TurnKit.Bach;
            report += Same("locked tempo", "Good Perfect", others + " " + tempo.Judge(off, true));

            //FUGUE : all four PERFECT give everybody an echo of 1.3 x TurnFugueScale 1.8 = 2.34. PAGANINI's CAPRICE then lands more
            //than twice as hard as without it (once, then x2.34), and the echo is used up.
            List<Musician> anna = new List<Musician> { MusicianList.All[0] };
            Skill cuts = SkillList.First(MusicianList.All[0]);                     // CAPRICE
            NoteResult[] goods = new NoteResult[cuts.Notes.Length];
            for (int i = 0; i < goods.Length; i++) goods[i] = new NoteResult(Grade.Good, 1f);
            TurnBattle plain = new TurnBattle(anna, new List<Enemy> { deadAir }, 1, new Random(433));
            plain.Breath = 10;
            plain.PlanSkill(plain.Party[0], cuts, plain.Enemies[0]);
            plain.PlayBar();
            plain.UseSkill(goods);
            int lossPlain = plain.Enemies[0].MaxHp - plain.Enemies[0].Hp;
            b = new TurnBattle(anna, new List<Enemy> { deadAir }, 1, new Random(433));
            b.Kit = TurnKit.Bach;
            b.Signature = b.SignatureNeed;
            b.UseSignature(fourPerfect);
            string echo = b.Party[0].Echo.ToString("0.0");
            b.Breath = 10;
            b.PlanSkill(b.Party[0], cuts, b.Enemies[0]);
            b.PlayBar();
            b.UseSkill(goods);
            int lossEcho = b.Enemies[0].MaxHp - b.Enemies[0].Hp;
            report += Same("fugue", "2.3 True 0", echo + " " + (lossEcho > 2 * lossPlain) + " " + b.Party[0].Echo);

            //CAPRICE (THE HEARTLESS) : 2 strokes land, so 4 turns where every swipe goes one better (a wrong way is
            //GOOD, 0.15 s off is PERFECT, one shake is GOOD). Three ATTACKs and HUSH's turn (blocked PERFECT) use them up.
            //Then everybody pays 20 percent (step 13) : PAGANINI 45 -> 36, QUANTZ 39 -> 31, PFUNDT 54 -> 43 (the band's life x1.5, 29 Oct)
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Kit = TurnKit.Paganini;
            b.Signature = b.SignatureNeed;
            b.UseSignature(new NoteResult[] { new NoteResult(Grade.Perfect, 1f), new NoteResult(Grade.Perfect, 1f),
                                              new NoteResult(Grade.Miss, 1f), new NoteResult(Grade.Miss, 1f) });
            string caprice = b.CapriceTurns + " " + b.Judge(0.5f, false) + " " + b.Judge(0.15f, true) + " " + b.RollGrade(1);
            b.PlayBar();
            b.Attack();
            b.Attack();
            b.Attack();
            b.TakeEnemyAttack(AllGrades(b.Current.IntentNotes.Length, Grade.Perfect));
            report += Same("caprice", "4 Good Perfect Good 0 36 31 43", caprice + " " + b.CapriceTurns + " " + b.Party[0].Hp + " " + b.Party[1].Hp + " " + b.Party[2].Hp);

            //BORROWED BREATH (THE HEARTLESS) : no BREATH, QUANTZ's THE LESSON (2) for PAGANINI costs 2 x 6 = 12 life (THE FIVE KITS,
            //it was LONG BREATH). Without the kit it cannot be written, and with 12 life or less it cannot be paid (she has 6).
            Skill lesson = SkillList.Second(MusicianList.All[1]);
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Breath = 0;
            bool plainCan = b.CanPlanSkill(b.Party[1], lesson);
            b.Kit = TurnKit.Paganini;
            NoteResult[] twoGood = { new NoteResult(Grade.Good, 1f), new NoteResult(Grade.Good, 1f) };
            b.PlanSkill(b.Party[1], lesson, b.Party[0]);
            b.PlayBar();                                                               // QUANTZ is written first, so plays first
            int price = b.LifeCost(lesson);
            b.UseSkill(twoGood);
            string paid = plainCan + " " + price + " " + b.Party[1].Hp + " " + b.Breath;
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433), new int[] { 30, 6, 36 });
            b.Kit = TurnKit.Paganini;
            b.Breath = 0;
            report += Same("borrowed breath", "False 12 27 0 False", paid + " " + b.CanPlanSkill(b.Party[1], lesson));

            //STEADY HAND (THE KING'S BATON) : on HUSH's turn 0.225 s late is still GOOD (0.22 + 0.01), not without it
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            PlayToEnemy(b);
            b.Kit = TurnKit.Lully;
            string steady = b.Judge(0.225f, true) + " " + b.LateLimit.ToString("0.00");
            b.Kit = TurnKit.None;
            report += Same("steady hand", "Good 0.23 Miss", steady + " " + b.Judge(0.225f, true));

            //THE SUN : all four PERFECT, 6 x 1.3 = 7.8 -> 8 back for everybody (step 15), DISSONANCE and MUTE gone
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433), new int[] { 10, 10, 10 });
            b.Kit = TurnKit.Lully;
            b.Party[0].Dissonance = 2;
            b.Party[1].Mute = 1;
            b.Signature = b.SignatureNeed;
            b.UseSignature(fourPerfect);
            report += Same("the sun", "18 18 18 0 0", b.Party[0].Hp + " " + b.Party[1].Hp + " " + b.Party[2].Hp + " " + b.Party[0].Dissonance + " " + b.Party[1].Mute);

            //TWO VOICES (FANNY MENDELSSOHN AND ???) : QUANTZ (written first) in FANNY's voice 4 x 2 x 0.55 x 1.25 = 5.5 -> 6, then FELIX
            //leads and PAGANINI hits a plain 5.5 -> 6, then FANNY again.
            b = new TurnBattle(band, new List<Enemy> { deadAir }, 1, new Random(433));
            b.Kit = TurnKit.Fanny;
            b.PlanAttack(b.Party[1], b.Enemies[0]);
            b.PlayBar();
            b.Enemies[0].IntentTarget = b.Party[1];
            int fanny = b.Attack();
            bool felixNext = b.FelixVoice;
            int felix = b.Attack();
            report += Same("two voices", "6 True 6 False", fanny + " " + felixNext + " " + felix + " " + b.FelixVoice);

            //DUET : all four PERFECT, 12 x 1.3 = 15.6 -> 16 to DEAD AIR (80 -> 64), 8 x 1.3 = 10.4 -> 10 back for everybody (step 13)
            b = new TurnBattle(band, new List<Enemy> { deadAir }, 1, new Random(433), new int[] { 10, 10, 10 });
            b.Kit = TurnKit.Fanny;
            b.Signature = b.SignatureNeed;
            b.UseSignature(fourPerfect);
            report += Same("duet", "64 20 20 20", b.Enemies[0].Hp + " " + b.Party[0].Hp + " " + b.Party[1].Hp + " " + b.Party[2].Hp);

            //Musician Traits (each musician on their own, the bar is played so their move is up).
            //The five legends' traits are in TurnCheckKits, these are the hidden musicians' (old saves).

            //BY EAR (MALI) : ATTACK 5 x 2 x 0.55 = 5.5, OFF-BEAT x1.5 and BY EAR x1.5 = 12.375 -> 12
            b = new TurnBattle(Solo(3), new List<Enemy> { deadAir }, 1, new Random(433));
            b.Enemies[0].Staggered = true;
            b.PlayBar();
            report += Same("by ear", "12", b.Attack().ToString());

            //HELD NOTE (CHAI) : ATTACK 4.4 -> 4, then it rings on for 4 x 0.4 = 1.6 -> 2, DEAD AIR 80 -> 74
            b = new TurnBattle(Solo(4), new List<Enemy> { deadAir }, 1, new Random(433));
            b.PlayBar();
            int held = b.Attack();
            report += Same("held note", "4 74", held + " " + b.Enemies[0].Hp);

            //ONE STEP BETTER (NUAN, Beta1 29 Oct) : against an attack of 3 notes, 6 x 2 x 0.55 x 1.3 = 8.58 -> 9, against DEAD AIR's
            //own 2 notes 6.6 -> 7 (it was : against an enemy showing an f note)
            b = new TurnBattle(Solo(5), new List<Enemy> { deadAir }, 1, new Random(433));
            b.PlayBar();
            b.Enemies[0].IntentNotes = new int[] { 1, 1, 1 };
            b.Enemies[0].IntentWays = new Flick[] { Flick.Left, Flick.Right, Flick.Down };
            b.Enemies[0].IntentLate = new bool[3];
            report += Same("one step better", "9 7", b.Attack() + " " + SoloAttacks(5, deadAir, 1));

            //THE WHOLE FLOOR (BORIS) : his THE FLOOR held PERFECT (its last note) takes 2 off each note of HUSH's next attack (8 -> 6)
            b = new TurnBattle(Solo(8), new List<Enemy> { hush }, 1, new Random(433));
            b.PlanSkill(b.Party[0], SkillList.First(MusicianList.All[8]), null);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { new NoteResult(Grade.Perfect, 1f) });
            report += Same("the whole floor", "6", b.Enemies[0].IntentNotes[0].ToString());

            //Review fixes (step 10) ---------------------------------------------------------------

            //PRODIGY keeps its own count, so THE SHADOWLESS's BEST STREAK can pass 3 (PERFECT parries in a row,
            //an ATTACK has no swipe and does not break it)
            b = new TurnBattle(band, new List<Enemy> { EnemyNamed("THE MUTE CHOIR") }, 1, new Random(433));
            b.Kit = TurnKit.Mozart;
            for (int step = 0; step < 40 && b.BestRun < 5 && b.Step != TurnStep.Won && b.Step != TurnStep.Lost; step++)
            {
                b.Enemies[0].Hp = b.Enemies[0].MaxHp;                                 // it must keep attacking : never down
                b.Enemies[0].OffBeat = 0;                                             // and never OFF-BEAT
                Step(b, Grade.Perfect);
            }
            report += Same("prodigy streak", "True", (b.BestRun >= 5).ToString());

            //FUGUE : an ATTACK echoes too (step 12), PAGANINI 5.5 -> 6 then 6 x 2.34 = 14 more, HUSH 80 -> 60
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Kit = TurnKit.Bach;
            b.Signature = b.SignatureNeed;
            b.UseSignature(fourPerfect);
            b.PlayBar();
            b.Attack();
            report += Same("fugue attack", "60 0", b.Enemies[0].Hp + " " + b.Party[0].Echo);

            //FUGUE : THE LESSON has nothing to repeat, so QUANTZ's echo waits for her next move (it was LONG BREATH)
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Kit = TurnKit.Bach;
            b.Signature = b.SignatureNeed;
            b.UseSignature(fourPerfect);
            b.PlanSkill(b.Party[1], lesson, b.Party[0]);
            b.PlayBar();
            b.UseSkill(twoGood);
            report += Same("fugue waits", "2.3", b.Party[1].Echo.ToString("0.0"));

            //TAUNT : when PFUNDT's next move comes, an enemy that planned while taunted aims again
            //(PFUNDT alone cannot show it, so PAGANINI and PFUNDT : PFUNDT taunts, HUSH hits him and plans again at him,
            //then PFUNDT's next move ends the taunt and HUSH picks among both)
            int otherTargets = 0;
            for (int seed = 0; seed < 20; seed++)
            {
                b = new TurnBattle(new List<Musician> { MusicianList.All[0], MusicianList.All[2] }, new List<Enemy> { hush }, 1, new Random(seed));
                b.Breath = 10;
                b.PlanSkill(b.Party[1], SkillList.Second(MusicianList.All[2]), null);
                b.PlayBar();
                b.UseSkill(new NoteResult[] { new NoteResult(Grade.Good, 1f) });   // PFUNDT : THUNDER CALL
                b.Attack();                                                                // PAGANINI
                b.TakeEnemyAttack(null);                                                   // HUSH hits PFUNDT, plans again at PFUNDT
                b.PlayBar();
                b.Attack();                                                                // bar 2 : PAGANINI
                if (b.Step == TurnStep.PlayerTurn && b.Current == b.Party[1] && b.Enemies[0].IntentTarget == b.Party[0]) otherTargets++;   // PFUNDT is up : the taunt is over
            }
            report += Same("taunt ends", "True", (otherTargets > 0).ToString());

            report += TurnCheckLegends(band, hush, deadAir, staticEnemy);
            return report;
        }

        //Solo : a band of one, musician i of MusicianList
        private static List<Musician> Solo(int i)
        {
            return new List<Musician> { MusicianList.All[i] };
        }

        //Solo Attacks : musician i alone (and Sturdy) against the enemy, ATTACKing this many times, one a bar. The enemy's
        //turns in between are parried GOOD (no COUNTER). Returns the damage of each ATTACK, spaced.
        private static string SoloAttacks(int i, Enemy enemy, int count)
        {
            TurnBattle b = new TurnBattle(Solo(i), new List<Enemy> { enemy }, 1, new Random(433));
            Sturdy(b);
            string hits = "";
            int done = 0;
            for (int step = 0; step < 40 && done < count && b.Step != TurnStep.Won && b.Step != TurnStep.Lost; step++)
            {
                if (b.Step != TurnStep.PlayerTurn)
                {
                    Step(b, Grade.Good);
                    continue;
                }
                hits += (done > 0 ? " " : "") + b.Attack();
                done++;
            }
            return hits;
        }

        private static Enemy EnemyNamed(string name)
        {
            for (int i = 0; i < EnemyList.All.Length; i++)
                if (EnemyList.All[i].Name == name) return EnemyList.All[i];
            return EnemyList.All[0];
        }
    }
}
