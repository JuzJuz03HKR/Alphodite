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
            b.Attack(b.Enemies[0], Grade.Good, true);
            b.Attack(b.Enemies[0], Grade.Good, true);
            b.TakeEnemyAttack(null);
            report += Same("intent after attack", "Left Right 1 True",
                b.Enemies[0].IntentWays[0] + " " + b.Enemies[0].IntentWays[1] + " " + b.Enemies[0].Attacks + " " + b.Enemies[0].Planned);

            //LOCKED TEMPO (THE METRONOME) : any size is the right size
            b.Kit = TurnKit.Bach;
            bool bachAny = b.AnySize;
            b.Kit = TurnKit.None;
            report += Same("locked tempo", "True False", bachAny + " " + b.AnySize);

            //FUGUE : all four PERFECT give everybody an echo of 1.3. ANNA's QUICK BOW then lands more
            //than twice as hard as without it (once, then x1.3), and the echo is used up.
            List<Musician> anna = new List<Musician> { MusicianList.All[0] };
            Skill quickBow = SkillList.First(MusicianList.All[0]);
            NoteResult[] goods = new NoteResult[quickBow.Notes.Length];
            for (int i = 0; i < goods.Length; i++) goods[i] = new NoteResult(Grade.Good, true, 1f);
            TurnBattle plain = new TurnBattle(anna, new List<Enemy> { deadAir }, 1, new Random(433));
            plain.Breath = 10;
            plain.UseSkill(quickBow, plain.Enemies[0], goods);
            int lossPlain = plain.Enemies[0].MaxHp - plain.Enemies[0].Hp;
            b = new TurnBattle(anna, new List<Enemy> { deadAir }, 1, new Random(433));
            b.Kit = TurnKit.Bach;
            b.Signature = b.SignatureNeed;
            b.UseSignature(fourPerfect);
            string echo = b.Party[0].Echo.ToString("0.0");
            b.Breath = 10;
            b.UseSkill(quickBow, b.Enemies[0], goods);
            int lossEcho = b.Enemies[0].MaxHp - b.Enemies[0].Hp;
            report += Same("fugue", "1.3 True 0", echo + " " + (lossEcho > 2 * lossPlain) + " " + b.Party[0].Echo);

            //CAPRICE (THE HEARTLESS) : 2 strokes land, so 2 turns where every swipe goes one better (a wrong way is
            //GOOD, 0.15 s off is PERFECT, one shake is GOOD).
            //Then everybody pays 10 percent : ANNA 30 -> 27, KLARA 26 -> 23, OTTO 36 -> 32
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Kit = TurnKit.Paganini;
            b.Signature = b.SignatureNeed;
            b.UseSignature(new NoteResult[] { new NoteResult(Grade.Perfect, true, 1f), new NoteResult(Grade.Perfect, true, 1f),
                                              new NoteResult(Grade.Miss, true, 1f), new NoteResult(Grade.Miss, true, 1f) });
            string caprice = b.CapriceTurns + " " + b.Judge(0.5f, false) + " " + b.Judge(0.15f, true) + " " + b.RollGrade(1);
            b.Attack(b.Enemies[0], Grade.Miss, true);
            b.Attack(b.Enemies[0], Grade.Miss, true);
            report += Same("caprice", "2 Good Perfect Good 0 27 23 32", caprice + " " + b.CapriceTurns + " " + b.Party[0].Hp + " " + b.Party[1].Hp + " " + b.Party[2].Hp);

            //BORROWED BREATH (THE HEARTLESS) : no BREATH, KLARA's LONG BREATH (2) costs 6 life and gives 4 BREATH.
            //Without the kit it cannot be played, and with 6 life or less it cannot be paid.
            Skill longBreath = SkillList.Second(MusicianList.All[1]);
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Breath = 0;
            bool plainCan = b.CanUse(longBreath);
            b.Kit = TurnKit.Paganini;
            int price = b.LifeCost(longBreath);
            NoteResult[] fourGood = { new NoteResult(Grade.Good, true, 1f), new NoteResult(Grade.Good, true, 1f),
                                      new NoteResult(Grade.Good, true, 1f), new NoteResult(Grade.Good, true, 1f) };
            b.UseSkill(longBreath, b.Party[1], fourGood);
            string paid = plainCan + " " + price + " " + b.Party[1].Hp + " " + b.Breath;
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433), new int[] { 30, 6, 36 });
            b.Kit = TurnKit.Paganini;
            b.Breath = 0;
            report += Same("borrowed breath", "False 6 20 4 False", paid + " " + b.CanUse(longBreath));

            //STEADY HAND (THE KING'S BATON) : on HUSH's turn 0.24 s late is still GOOD (0.22 + 0.03), not without it
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Attack(b.Enemies[0], Grade.Good, true);
            b.Attack(b.Enemies[0], Grade.Good, true);
            b.Kit = TurnKit.Lully;
            string steady = b.Judge(0.24f, true) + " " + b.LateLimit.ToString("0.00");
            b.Kit = TurnKit.None;
            report += Same("steady hand", "Good 0.25 Miss", steady + " " + b.Judge(0.24f, true));

            //THE SUN : all four PERFECT, 12 x 1.3 = 15.6 -> 16 back for everybody (KLARA tops out at 26), DISSONANCE and MUTE gone
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433), new int[] { 10, 10, 10 });
            b.Kit = TurnKit.Lully;
            b.Party[0].Dissonance = 2;
            b.Party[1].Mute = 1;
            b.Signature = b.SignatureNeed;
            b.UseSignature(fourPerfect);
            report += Same("the sun", "26 26 26 0 0", b.Party[0].Hp + " " + b.Party[1].Hp + " " + b.Party[2].Hp + " " + b.Party[0].Dissonance + " " + b.Party[1].Mute);

            //TWO VOICES (THE UNSIGNED) : KLARA in FANNY's voice 4 x 2 x 1.25 = 10, then FELIX leads and ANNA hits a plain 10,
            //then FANNY again
            b = new TurnBattle(band, new List<Enemy> { deadAir }, 1, new Random(433));
            b.Kit = TurnKit.Fanny;
            int fanny = b.Attack(b.Enemies[0], Grade.Good, true);
            bool felixNext = b.FelixVoice;
            int felix = b.Attack(b.Enemies[0], Grade.Good, true);
            report += Same("two voices", "10 True 10 False", fanny + " " + felixNext + " " + felix + " " + b.FelixVoice);

            //DUET : all four PERFECT, 9 x 1.3 = 11.7 -> 12 to DEAD AIR, 6 x 1.3 = 7.8 -> 8 back for everybody
            b = new TurnBattle(band, new List<Enemy> { deadAir }, 1, new Random(433), new int[] { 10, 10, 10 });
            b.Kit = TurnKit.Fanny;
            b.Signature = b.SignatureNeed;
            b.UseSignature(fourPerfect);
            report += Same("duet", "38 18 18 18", b.Enemies[0].Hp + " " + b.Party[0].Hp + " " + b.Party[1].Hp + " " + b.Party[2].Hp);

            //Musician Traits (each musician on their own, so their turn comes first or after one enemy turn)

            //COUNTS ALOUD (ANNA) : 0.13 s off is PERFECT on her own notes (0.10 + 0.05), GOOD for KLARA
            b = new TurnBattle(anna, new List<Enemy> { deadAir }, 1, new Random(433));
            string counts = b.Judge(0.13f, true).ToString();
            b = new TurnBattle(Solo(1), new List<Enemy> { deadAir }, 1, new Random(433));
            report += Same("counts aloud", "Perfect Good", counts + " " + b.Judge(0.13f, true));

            //THE QUIET PART (KLARA) : GOOD 4 x 2 = 8, x1.5 = 12 against HUSH's p notes, 8 against DEAD AIR's mf
            b = new TurnBattle(Solo(1), new List<Enemy> { hush }, 1, new Random(433));
            int quiet = b.Attack(b.Enemies[0], Grade.Good, true);
            b = new TurnBattle(Solo(1), new List<Enemy> { deadAir }, 1, new Random(433));
            report += Same("the quiet part", "12 8", quiet + " " + b.Attack(b.Enemies[0], Grade.Good, true));

            //MOMENTUM (OTTO) : GOOD 12, 14, 16, then a MISS at power 9 (9 x 2 x 0.4 = 7.2 -> 7) starts again at 12
            Grade[] swings = { Grade.Good, Grade.Good, Grade.Good, Grade.Miss, Grade.Good };
            report += Same("momentum", "12 14 16 7 12", SoloSwings(2, hush, swings));

            //BY EAR (MALI) : PERFECT 5 x 2 x 1.3 = 13, OFF-BEAT x1.5 and BY EAR x1.5 = 29.25 -> 29
            b = new TurnBattle(Solo(3), new List<Enemy> { deadAir }, 1, new Random(433));
            b.Enemies[0].Staggered = true;
            report += Same("by ear", "29", b.Attack(b.Enemies[0], Grade.Perfect, true).ToString());

            //HELD NOTE (CHAI) : GOOD 8, then it rings on for 8 x 0.4 = 3.2 -> 3, DEAD AIR 50 -> 39
            b = new TurnBattle(Solo(4), new List<Enemy> { deadAir }, 1, new Random(433));
            int held = b.Attack(b.Enemies[0], Grade.Good, true);
            report += Same("held note", "8 39", held + " " + b.Enemies[0].Hp);

            //ONE STEP BETTER (NUAN) : after STATIC's turn it shows Umf Df, so GOOD 6 x 2 x 1.3 = 15.6 -> 16, against DEAD AIR 12
            report += Same("one step better", "16 12", SoloSwings(5, staticEnemy, new Grade[] { Grade.Good }) + " " + SoloSwings(5, deadAir, new Grade[] { Grade.Good }));

            //FOUR BARS STRAIGHT (LUKA) : GOOD 12, 12, 12, then the fourth x2 = 24
            report += Same("four bars straight", "12 12 12 24", SoloSwings(6, hush, new Grade[] { Grade.Good, Grade.Good, Grade.Good, Grade.Good }));

            //FASHIONABLY LATE (IRIS) : 0.3 s late is still GOOD (0.22 + 0.12), 0.3 s early is not
            b = new TurnBattle(Solo(7), new List<Enemy> { deadAir }, 1, new Random(433));
            report += Same("fashionably late", "Good Miss 0.34", b.Judge(0.3f, true) + " " + b.Judge(-0.3f, true) + " " + b.LateLimit.ToString("0.00"));

            //THE WHOLE FLOOR (BORIS) : after HUSH's turn, his PERFECT f swing takes 2 off each note of its next attack (6 -> 4)
            b = new TurnBattle(Solo(8), new List<Enemy> { hush }, 1, new Random(433));
            b.TakeEnemyAttack(new Grade[] { Grade.Good, Grade.Good });
            b.Attack(b.Enemies[0], Grade.Perfect, true);
            report += Same("the whole floor", "4", b.Enemies[0].IntentNotes[0].ToString());

            //Review fixes (step 10) ---------------------------------------------------------------

            //PRODIGY keeps its own count, so THE SHADOWLESS's BEST STREAK can pass 3 (5 PERFECT attacks in a row)
            b = new TurnBattle(band, new List<Enemy> { EnemyNamed("THE MUTE CHOIR") }, 1, new Random(433));
            b.Kit = TurnKit.Mozart;
            for (int step = 0; step < 20 && b.BestRun < 5 && b.Step != TurnStep.Won && b.Step != TurnStep.Lost; step++)
            {
                if (b.Step == TurnStep.PlayerTurn) b.Attack(b.Enemies[0], Grade.Perfect, true);
                else
                {
                    Grade[] all = new Grade[b.Current.IntentNotes.Length];
                    for (int n = 0; n < all.Length; n++) all[n] = Grade.Perfect;
                    b.TakeEnemyAttack(all);
                }
            }
            report += Same("prodigy streak", "True", (b.BestRun >= 5).ToString());

            //FUGUE : LONG BREATH has nothing to repeat, so KLARA's echo waits for her next skill
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Kit = TurnKit.Bach;
            b.Signature = b.SignatureNeed;
            b.UseSignature(fourPerfect);
            b.UseSkill(longBreath, b.Party[1], fourGood);
            report += Same("fugue waits", "1.3", b.Party[1].Echo.ToString("0.0"));

            //TAUNT : when OTTO's next turn comes, an enemy that planned while taunted aims again
            //(OTTO alone cannot show it, so ANNA and OTTO : OTTO taunts, HUSH hits him and plans again at him,
            //then OTTO's turn ends the taunt and HUSH picks among both)
            int otherTargets = 0;
            for (int seed = 0; seed < 20; seed++)
            {
                b = new TurnBattle(new List<Musician> { MusicianList.All[0], MusicianList.All[2] }, new List<Enemy> { hush }, 1, new Random(seed));
                b.Breath = 10;
                b.Attack(b.Enemies[0], Grade.Good, true);                                  // ANNA
                b.TakeEnemyAttack(null);                                                   // HUSH
                b.UseSkill(SkillList.Second(MusicianList.All[2]), b.Party[1], new NoteResult[] { new NoteResult(Grade.Good, true, 1f) });   // OTTO : STAND FIRM
                b.Attack(b.Enemies[0], Grade.Good, true);                                  // round 2 : ANNA
                b.TakeEnemyAttack(null);                                                   // HUSH hits OTTO, plans again at OTTO
                b.Attack(b.Enemies[0], Grade.Good, true);                                  // OTTO : the taunt is over
                if (b.Enemies[0].IntentTarget == b.Party[0]) otherTargets++;
            }
            report += Same("taunt ends", "True", (otherTargets > 0).ToString());

            return report;
        }

        //Solo : a band of one, musician i of MusicianList
        private static List<Musician> Solo(int i)
        {
            return new List<Musician> { MusicianList.All[i] };
        }

        //Solo Swings : musician i alone against the enemy, ATTACKing with these grades in turn. The enemy's
        //turns in between are parried GOOD (no COUNTER). Returns the damage of each ATTACK, spaced.
        private static string SoloSwings(int i, Enemy enemy, Grade[] grades)
        {
            TurnBattle b = new TurnBattle(Solo(i), new List<Enemy> { enemy }, 1, new Random(433));
            string hits = "";
            int done = 0;
            for (int step = 0; step < 40 && done < grades.Length && (b.Step == TurnStep.PlayerTurn || b.Step == TurnStep.EnemyTurn); step++)
            {
                if (b.Step == TurnStep.EnemyTurn)
                {
                    Grade[] good = new Grade[b.Current.IntentNotes.Length];
                    for (int n = 0; n < good.Length; n++) good[n] = Grade.Good;
                    b.TakeEnemyAttack(good);
                    continue;
                }
                hits += (done > 0 ? " " : "") + b.Attack(b.Enemies[0], grades[done], true);
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
