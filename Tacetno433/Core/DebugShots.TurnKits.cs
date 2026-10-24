using System;
using System.Collections.Generic;
using Tacetno433.Battle;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //DebugShots.TurnKits : DEVELOPER TOOL, the checks of step 10 (Alpha 7), part of --shots turncheck.
    //The four new conductor kits (THE METRONOME, THE HEARTLESS, THE KING'S BATON, THE UNSIGNED),
    //the nine musician traits in the turn battle, and the intent that is decided right after an attack.
    //Like DebugShots.Turn.cs every line is PASS or FAIL with what was expected and what came out.
    public static partial class DebugShots
    {
        private static string TurnCheckStep10(List<Musician> band, Enemy hush, Enemy deadAir)
        {
            string report = "";
            MusicianList.ResetRehearsals();                                             // earlier checks rehearse some musicians
            Enemy staticEnemy = EnemyNamed("STATIC");
            NoteResult[] fourPerfect = { new NoteResult(Grade.Perfect, true, 1f), new NoteResult(Grade.Perfect, true, 1f),
                                         new NoteResult(Grade.Perfect, true, 1f), new NoteResult(Grade.Perfect, true, 1f) };

            //Conductors : the turn battle's gallery shows the six of the lore, Beethoven first
            report += Same("conductors shown", "6 THE UNHEARING THE UNSIGNED",
                ConductorList.Shown.Length + " " + ConductorList.Shown[0].Name + " " + ConductorList.Shown[5].Name);

            //Intent : decided again right after an attack, so HUSH now shows its second phrase (Lp Rp Dp, 2 notes on floor 1)
            TurnBattle b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            PlayToEnemy(b);
            b.TakeEnemyAttack(null);
            report += Same("intent after attack", "Left Right 1 True",
                b.Enemies[0].IntentWays[0] + " " + b.Enemies[0].IntentWays[1] + " " + b.Enemies[0].Attacks + " " + b.Enemies[0].Planned);

            //LOCKED TEMPO (THE METRONOME) : any size is the right size
            b.Kit = TurnKit.Bach;
            bool bachAny = b.AnySize;
            b.Kit = TurnKit.None;
            report += Same("locked tempo", "True False", bachAny + " " + b.AnySize);

            //FUGUE : all four PERFECT give everybody an echo of 1.3 x TurnFugueScale 1.8 = 2.34. ANNA's QUICK BOW then lands more
            //than twice as hard as without it (once, then x2.34), and the echo is used up.
            List<Musician> anna = new List<Musician> { MusicianList.All[0] };
            Skill quickBow = SkillList.First(MusicianList.All[0]);
            NoteResult[] goods = new NoteResult[quickBow.Notes.Length];
            for (int i = 0; i < goods.Length; i++) goods[i] = new NoteResult(Grade.Good, true, 1f);
            TurnBattle plain = new TurnBattle(anna, new List<Enemy> { deadAir }, 1, new Random(433));
            plain.Breath = 10;
            plain.PlanSkill(plain.Party[0], quickBow, plain.Enemies[0]);
            plain.PlayBar();
            plain.UseSkill(goods);
            int lossPlain = plain.Enemies[0].MaxHp - plain.Enemies[0].Hp;
            b = new TurnBattle(anna, new List<Enemy> { deadAir }, 1, new Random(433));
            b.Kit = TurnKit.Bach;
            b.Signature = b.SignatureNeed;
            b.UseSignature(fourPerfect);
            string echo = b.Party[0].Echo.ToString("0.0");
            b.Breath = 10;
            b.PlanSkill(b.Party[0], quickBow, b.Enemies[0]);
            b.PlayBar();
            b.UseSkill(goods);
            int lossEcho = b.Enemies[0].MaxHp - b.Enemies[0].Hp;
            report += Same("fugue", "2.3 True 0", echo + " " + (lossEcho > 2 * lossPlain) + " " + b.Party[0].Echo);

            //CAPRICE (THE HEARTLESS) : 2 strokes land, so 4 turns where every swipe goes one better (a wrong way is
            //GOOD, 0.15 s off is PERFECT, one shake is GOOD). Three ATTACKs and HUSH's turn (blocked PERFECT) use them up.
            //Then everybody pays 20 percent (step 13) : ANNA 30 -> 24, KLARA 26 -> 21, OTTO 36 -> 29
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Kit = TurnKit.Paganini;
            b.Signature = b.SignatureNeed;
            b.UseSignature(new NoteResult[] { new NoteResult(Grade.Perfect, true, 1f), new NoteResult(Grade.Perfect, true, 1f),
                                              new NoteResult(Grade.Miss, true, 1f), new NoteResult(Grade.Miss, true, 1f) });
            string caprice = b.CapriceTurns + " " + b.Judge(0.5f, false) + " " + b.Judge(0.15f, true) + " " + b.RollGrade(1);
            b.PlayBar();
            b.Attack();
            b.Attack();
            b.Attack();
            b.TakeEnemyAttack(AllGrades(b.Current.IntentNotes.Length, Grade.Perfect));
            report += Same("caprice", "4 Good Perfect Good 0 24 21 29", caprice + " " + b.CapriceTurns + " " + b.Party[0].Hp + " " + b.Party[1].Hp + " " + b.Party[2].Hp);

            //BORROWED BREATH (THE HEARTLESS) : no BREATH, KLARA's LONG BREATH (2) costs 6 life and gives 4 BREATH.
            //Without the kit it cannot be written, and with 6 life or less it cannot be paid.
            Skill longBreath = SkillList.Second(MusicianList.All[1]);
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Breath = 0;
            bool plainCan = b.CanPlanSkill(b.Party[1], longBreath);
            b.Kit = TurnKit.Paganini;
            int price = b.LifeCost(longBreath);
            NoteResult[] fourGood = { new NoteResult(Grade.Good, true, 1f), new NoteResult(Grade.Good, true, 1f),
                                      new NoteResult(Grade.Good, true, 1f), new NoteResult(Grade.Good, true, 1f) };
            b.PlanSkill(b.Party[1], longBreath, null);
            b.PlayBar();
            b.UseSkill(fourGood);
            string paid = plainCan + " " + price + " " + b.Party[1].Hp + " " + b.Breath;
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433), new int[] { 30, 6, 36 });
            b.Kit = TurnKit.Paganini;
            b.Breath = 0;
            report += Same("borrowed breath", "False 6 20 4 False", paid + " " + b.CanPlanSkill(b.Party[1], longBreath));

            //STEADY HAND (THE KING'S BATON) : on HUSH's turn 0.225 s late is still GOOD (0.22 + 0.01), not without it
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            PlayToEnemy(b);
            b.Kit = TurnKit.Lully;
            string steady = b.Judge(0.225f, true) + " " + b.LateLimit.ToString("0.00");
            b.Kit = TurnKit.None;
            report += Same("steady hand", "Good 0.23 Miss", steady + " " + b.Judge(0.225f, true));

            //THE SUN : all four PERFECT, 8 x 1.3 = 10.4 -> 10 back for everybody (step 13), DISSONANCE and MUTE gone
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433), new int[] { 10, 10, 10 });
            b.Kit = TurnKit.Lully;
            b.Party[0].Dissonance = 2;
            b.Party[1].Mute = 1;
            b.Signature = b.SignatureNeed;
            b.UseSignature(fourPerfect);
            report += Same("the sun", "20 20 20 0 0", b.Party[0].Hp + " " + b.Party[1].Hp + " " + b.Party[2].Hp + " " + b.Party[0].Dissonance + " " + b.Party[1].Mute);

            //TWO VOICES (THE UNSIGNED) : KLARA (written first) in FANNY's voice 4 x 2 x 1.25 = 10, then FELIX leads
            //and ANNA hits a plain 10, then FANNY again
            b = new TurnBattle(band, new List<Enemy> { deadAir }, 1, new Random(433));
            b.Kit = TurnKit.Fanny;
            b.PlanAttack(b.Party[1], b.Enemies[0]);
            b.PlayBar();
            int fanny = b.Attack();
            bool felixNext = b.FelixVoice;
            int felix = b.Attack();
            report += Same("two voices", "10 True 10 False", fanny + " " + felixNext + " " + felix + " " + b.FelixVoice);

            //DUET : all four PERFECT, 12 x 1.3 = 15.6 -> 16 to DEAD AIR, 8 x 1.3 = 10.4 -> 10 back for everybody (step 13)
            b = new TurnBattle(band, new List<Enemy> { deadAir }, 1, new Random(433), new int[] { 10, 10, 10 });
            b.Kit = TurnKit.Fanny;
            b.Signature = b.SignatureNeed;
            b.UseSignature(fourPerfect);
            report += Same("duet", "34 20 20 20", b.Enemies[0].Hp + " " + b.Party[0].Hp + " " + b.Party[1].Hp + " " + b.Party[2].Hp);

            //Musician Traits (each musician on their own, the bar is played so their move is up)

            //COUNTS ALOUD (ANNA) : 0.13 s off is PERFECT on her skill notes (0.10 + 0.05), GOOD for KLARA
            b = new TurnBattle(anna, new List<Enemy> { deadAir }, 1, new Random(433));
            b.PlayBar();
            string counts = b.Judge(0.13f, true).ToString();
            b = new TurnBattle(Solo(1), new List<Enemy> { deadAir }, 1, new Random(433));
            b.PlayBar();
            report += Same("counts aloud", "Perfect Good", counts + " " + b.Judge(0.13f, true));

            //THE QUIET PART (KLARA) : ATTACK 4 x 2 = 8, x1.5 = 12 against HUSH's p notes, 8 against DEAD AIR's mf
            report += Same("the quiet part", "12 8", SoloAttacks(1, hush, 1) + " " + SoloAttacks(1, deadAir, 1));

            //MOMENTUM (OTTO) : 12, 14, 16, then 18 at most (power 6 + 3). A skill that misses every note starts again at 12.
            string momentum = SoloAttacks(2, EnemyNamed("THE MUTE CHOIR"), 5);                      // 120 life, so OTTO gets five
            b = new TurnBattle(Solo(2), new List<Enemy> { EnemyNamed("THE MUTE CHOIR") }, 1, new Random(433));
            Sturdy(b);
            b.Breath = 10;
            for (int i = 0; i < 12 && b.Party[0].Momentum < 3; i++) Step(b, Grade.Good);
            while (b.Step != TurnStep.Plan) Step(b, Grade.Good);
            b.PlanSkill(b.Party[0], SkillList.First(MusicianList.All[2]), b.Enemies[0]);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { new NoteResult(Grade.Hesitate, true, 1f) });
            report += Same("momentum", "12 14 16 18 18 0", momentum + " " + b.Party[0].Momentum);

            //BY EAR (MALI) : ATTACK 5 x 2 = 10, OFF-BEAT x1.5 and BY EAR x1.5 = 22.5 -> 22
            b = new TurnBattle(Solo(3), new List<Enemy> { deadAir }, 1, new Random(433));
            b.Enemies[0].Staggered = true;
            b.PlayBar();
            report += Same("by ear", "22", b.Attack().ToString());

            //HELD NOTE (CHAI) : ATTACK 8, then it rings on for 8 x 0.4 = 3.2 -> 3, DEAD AIR 50 -> 39
            b = new TurnBattle(Solo(4), new List<Enemy> { deadAir }, 1, new Random(433));
            b.PlayBar();
            int held = b.Attack();
            report += Same("held note", "8 39", held + " " + b.Enemies[0].Hp);

            //ONE STEP BETTER (NUAN) : STATIC shows Lf Rp (an f), so 6 x 2 x 1.3 = 15.6 -> 16, against DEAD AIR (mf only) 12
            report += Same("one step better", "16 12", SoloAttacks(5, staticEnemy, 1) + " " + SoloAttacks(5, deadAir, 1));

            //FOUR BARS STRAIGHT (LUKA) : 12, 12, 12, then the fourth ATTACK x2 = 24
            report += Same("four bars straight", "12 12 12 24", SoloAttacks(6, hush, 4));

            //FASHIONABLY LATE (IRIS) : on her skill notes 0.3 s late is still GOOD (0.22 + 0.12), 0.3 s early is not
            b = new TurnBattle(Solo(7), new List<Enemy> { deadAir }, 1, new Random(433));
            b.PlayBar();
            report += Same("fashionably late", "Good Miss 0.34", b.Judge(0.3f, true) + " " + b.Judge(-0.3f, true) + " " + b.LateLimit.ToString("0.00"));

            //THE WHOLE FLOOR (BORIS) : his THE FLOOR held PERFECT (an f note) takes 2 off each note of HUSH's next attack (9 -> 7)
            b = new TurnBattle(Solo(8), new List<Enemy> { hush }, 1, new Random(433));
            b.PlanSkill(b.Party[0], SkillList.First(MusicianList.All[8]), null);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { new NoteResult(Grade.Perfect, true, 1f) });
            report += Same("the whole floor", "7", b.Enemies[0].IntentNotes[0].ToString());

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

            //FUGUE : an ATTACK echoes too (step 12), ANNA 10 then 23 more (x2.34, step 13), HUSH 50 -> 17
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Kit = TurnKit.Bach;
            b.Signature = b.SignatureNeed;
            b.UseSignature(fourPerfect);
            b.PlayBar();
            b.Attack();
            report += Same("fugue attack", "17 0", b.Enemies[0].Hp + " " + b.Party[0].Echo);

            //FUGUE : LONG BREATH has nothing to repeat, so KLARA's echo waits for her next move
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Kit = TurnKit.Bach;
            b.Signature = b.SignatureNeed;
            b.UseSignature(fourPerfect);
            b.PlanSkill(b.Party[1], longBreath, null);
            b.PlayBar();
            b.UseSkill(fourGood);
            report += Same("fugue waits", "2.3", b.Party[1].Echo.ToString("0.0"));

            //TAUNT : when OTTO's next move comes, an enemy that planned while taunted aims again
            //(OTTO alone cannot show it, so ANNA and OTTO : OTTO taunts, HUSH hits him and plans again at him,
            //then OTTO's next move ends the taunt and HUSH picks among both)
            int otherTargets = 0;
            for (int seed = 0; seed < 20; seed++)
            {
                b = new TurnBattle(new List<Musician> { MusicianList.All[0], MusicianList.All[2] }, new List<Enemy> { hush }, 1, new Random(seed));
                b.Breath = 10;
                b.PlanSkill(b.Party[1], SkillList.Second(MusicianList.All[2]), null);
                b.PlayBar();
                b.UseSkill(new NoteResult[] { new NoteResult(Grade.Good, true, 1f) });   // OTTO : STAND FIRM
                b.Attack();                                                                // ANNA
                b.TakeEnemyAttack(null);                                                   // HUSH hits OTTO, plans again at OTTO
                b.PlayBar();
                b.Attack();                                                                // bar 2 : ANNA
                if (b.Step == TurnStep.PlayerTurn && b.Current == b.Party[1] && b.Enemies[0].IntentTarget == b.Party[0]) otherTargets++;   // OTTO is up : the taunt is over
            }
            report += Same("taunt ends", "True", (otherTargets > 0).ToString());

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
