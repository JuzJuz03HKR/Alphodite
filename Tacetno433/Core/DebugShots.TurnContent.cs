using System;
using System.Collections.Generic;
using Tacetno433.Battle;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //DebugShots.TurnContent : DEVELOPER TOOL, the checks of steps 13 and 14 (Alpha 7), part of --shots turncheck.
    //The six ONE BAR motifs, the five new enemies, the three bosses of step 14 and the bugs fixed with them.
    //The musicians' own traits are switched off here so the numbers stay plain (PAGANINI 6, QUANTZ 4, PFUNDT 7 an ATTACK, enemies x1.6 life on floor 1).
    public static partial class DebugShots
    {
        private static string TurnCheckStep13(List<Musician> band, Enemy hush, Enemy deadAir)
        {
            string report = "";
            MusicianList.ResetRehearsals();
            MusicianTrait[] traits = new MusicianTrait[MusicianList.All.Length];
            for (int i = 0; i < traits.Length; i++)
            {
                traits[i] = MusicianList.All[i].Trait;
                MusicianList.All[i].Trait = MusicianTrait.None;
            }

            //Lists : 21 + 6 motifs, 9 + 5 + 3 enemies, 7 + 3 events, the new ones at the end (the save keeps their places)
            report += Same("content", "27 17 10 ROOM TONE", MotifList.All.Length + " " + EnemyList.All.Length + " " + EventList.All.Length + " " + EnemyList.All[9].Name);

            //DOWNBEAT : the first move of the bar x1.5 (PAGANINI 5.5 x 1.5 = 8.25 -> 8), the second plain (QUANTZ 4)
            TurnBattle b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.GiveMotifs(new List<Motif> { MotifList.Get(MotifId.Downbeat) });
            b.PlayBar();
            int down1 = b.Attack();
            int down2 = b.Attack();
            report += Same("downbeat", "8 4", down1 + " " + down2);

            //ENTRANCE (29 Oct) : each musician's first move of the fight is x2 (PAGANINI 5.5 x 2 = 11, QUANTZ 4.4 x 2 = 8.8 -> 9,
            //PFUNDT 6.6 x 2 = 13.2 -> 13), the next bar plainly (PAGANINI 6). Until CALL went away : a called musician's first bar.
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            Tough(b);
            b.GiveMotifs(new List<Motif> { MotifList.Get(MotifId.Entrance) });
            b.PlayBar();
            string entrance = b.Attack() + " " + b.Attack() + " " + b.Attack();
            PlayWholeBar(b);
            b.PlayBar();
            report += Same("entrance", "11 9 13 6", entrance + " " + b.Attack());

            //GREEN ROOM (29 Oct) : after the bar the most hurt musician (PFUNDT at half his life) gets 3 back, nobody else.
            //Until the BENCH went away it healed the BENCH.
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            Tough(b);
            b.Party[2].Hp = 500;
            b.GiveMotifs(new List<Motif> { MotifList.Get(MotifId.GreenRoom) });
            PlayWholeBar(b);
            int roomHeal = 0;
            int otherHeal = 0;
            for (int i = 0; i < b.Events.Count; i++)
                if (b.Events[i].Kind == TurnEventKind.Heal)
                {
                    if (b.Events[i].Who == b.Party[2]) roomHeal += b.Events[i].Amount;
                    else otherHeal += b.Events[i].Amount;
                }
            report += Same("green room", "3 0", roomHeal + " " + otherHeal);

            //UNISON : the whole bar ATTACKs HUSH (PAGANINI 6), so QUANTZ's last ATTACK is x2 (8.8 -> 9). QUANTZ at DEAD AIR instead : no unison (4)
            b = new TurnBattle(Duo(), new List<Enemy> { hush, deadAir }, 1, new Random(433));
            b.GiveMotifs(new List<Motif> { MotifList.Get(MotifId.Unison) });
            b.PlayBar();
            int uni1 = b.Attack();
            int uni2 = b.Attack();
            b = new TurnBattle(Duo(), new List<Enemy> { hush, deadAir }, 1, new Random(433));
            b.GiveMotifs(new List<Motif> { MotifList.Get(MotifId.Unison) });
            b.PlanAttack(b.Party[0], b.Enemies[0]);
            b.PlanAttack(b.Party[1], b.Enemies[1]);
            b.PlayBar();
            b.Attack();
            report += Same("unison", "6 9 4", uni1 + " " + uni2 + " " + b.Attack());

            //SEGUE : HUSH has 5 life, PAGANINI's 6 silences it and the 1 left over goes on to DEAD AIR (80 -> 79). Without it 80.
            b = new TurnBattle(Duo(), new List<Enemy> { hush, deadAir }, 1, new Random(433));
            b.GiveMotifs(new List<Motif> { MotifList.Get(MotifId.Segue) });
            b.Enemies[0].Hp = 5;
            b.PlanAttack(b.Party[0], b.Enemies[0]);
            b.PlayBar();
            b.Attack();
            string segue = b.Enemies[0].Hp + " " + b.Enemies[1].Hp;
            b = new TurnBattle(Duo(), new List<Enemy> { hush, deadAir }, 1, new Random(433));
            b.Enemies[0].Hp = 5;
            b.PlanAttack(b.Party[0], b.Enemies[0]);
            b.PlayBar();
            b.Attack();
            report += Same("segue", "0 79 80", segue + " " + b.Enemies[1].Hp);

            //LAST WORD : QUANTZ plays the bar's last move, so she stands GUARD and PAGANINI does not
            b = new TurnBattle(Duo(), new List<Enemy> { hush }, 1, new Random(433));
            b.GiveMotifs(new List<Motif> { MotifList.Get(MotifId.LastWord) });
            b.PlayBar();
            b.Attack();
            b.Attack();
            report += Same("last word", "False True", b.Party[0].Guard + " " + b.Party[1].Guard);

            //HOLDS THE ROOM (ROOM TONE, floor 2) : HUSH takes 70 percent while it stands (PAGANINI 6 -> 4.2 -> 4, 105 -> 101),
            //ROOM TONE itself takes all of QUANTZ's 4. On floor 1 the trait is off (80 -> 74).
            Enemy roomTone = EnemyNamed("ROOM TONE");
            b = new TurnBattle(Duo(), new List<Enemy> { hush, roomTone }, 2, new Random(433));
            b.PlanAttack(b.Party[0], b.Enemies[0]);
            b.PlanAttack(b.Party[1], b.Enemies[1]);
            b.PlayBar();
            b.Attack();
            b.Attack();
            string room = b.Enemies[0].Hp + " " + (b.Enemies[1].MaxHp - b.Enemies[1].Hp);
            b = new TurnBattle(Duo(), new List<Enemy> { hush, roomTone }, 1, new Random(433));
            b.PlanAttack(b.Party[0], b.Enemies[0]);
            b.PlayBar();
            b.Attack();
            report += Same("holds the room", "101 4 74", room + " " + b.Enemies[0].Hp);

            //ON CUE (THE CUE, floor 2, 29 Oct) : it turns to the musician who plays a skill, the moment they play it
            Enemy cue = EnemyNamed("THE CUE");
            b = new TurnBattle(band, new List<Enemy> { cue }, 2, new Random(433));
            Tough(b);
            b.Breath = 10;
            Fighter solo = b.Enemies[0].IntentTarget == b.Party[0] ? b.Party[1] : b.Party[0];
            Skill soloSkill = SkillList.First(solo.Musician);
            b.PlanSkill(solo, soloSkill, soloSkill.Target == SkillTarget.Enemy ? b.Enemies[0] : solo);
            b.PlayBar();
            bool aimedElsewhere = b.Enemies[0].IntentTarget != solo;
            b.UseSkill(AllNotes(soloSkill, Grade.Good));
            report += Same("on cue", "True True True", aimedElsewhere + " " + (b.Enemies[0].IntentTarget == solo) + " " + (b.LastSoloist == solo));

            //FELT HAND (THE DAMPER, floor 2, 29 Oct) : an ATTACK gives 2 - 1 BREATH while it stands, 2 once it is silenced
            b = new TurnBattle(band, new List<Enemy> { EnemyNamed("THE DAMPER") }, 2, new Random(433));
            int damped = b.AttackBreath;
            b.Enemies[0].Hp = 0;
            report += Same("felt hand", "1 2", damped + " " + b.AttackBreath);

            //TWICE A BAR (STUTTER, floor 2) : it cuts in after the 1st and the 3rd move with one note each time.
            //On floor 1 the trait is off and it is a plain CUT IN after the 2nd move.
            Enemy stutter = EnemyNamed("STUTTER");
            b = new TurnBattle(band, new List<Enemy> { stutter, hush }, 2, new Random(433));
            b.PlayBar();
            string twice = OrderNames(b) + " " + b.Enemies[0].IntentNotes.Length;
            b = new TurnBattle(band, new List<Enemy> { stutter, hush }, 1, new Random(433));
            b.PlayBar();
            report += Same("twice a bar", "PAGANINI STUTTER QUANTZ PFUNDT STUTTER HUSH 1 / PAGANINI QUANTZ STUTTER PFUNDT HUSH", twice + " / " + OrderNames(b));

            //FIRST IN LINE (THE ROUND) : it aims at the first musician of the bar, and turns as the bar is written
            b = new TurnBattle(band, new List<Enemy> { EnemyNamed("THE ROUND") }, 1, new Random(433));
            string line = b.Enemies[0].IntentTarget.Name;
            b.PlanAttack(b.Party[2], b.Enemies[0]);
            line += " " + b.Enemies[0].IntentTarget.Name;
            b.Unplan(b.Party[2]);
            report += Same("first in line", "PAGANINI PFUNDT PAGANINI", line + " " + b.Enemies[0].IntentTarget.Name);

            //Fallen Target (bug fix) : HUSH aims at someone after its attack, they fall later in the bar,
            //so when the next bar is written HUSH aims at somebody standing
            b = new TurnBattle(band, new List<Enemy> { hush, EnemyNamed("THE LULL") }, 1, new Random(433));
            PlayToEnemy(b);
            b.TakeEnemyAttack(null);
            Fighter aimedAt = b.Enemies[0].IntentTarget;
            aimedAt.Hp = 0;
            b.TakeEnemyAttack(null);
            report += Same("fallen target", "Plan False", b.Step + " " + b.Enemies[0].IntentTarget.Down);

            report += TurnCheckBosses(band, hush);
            for (int i = 0; i < traits.Length; i++) MusicianList.All[i].Trait = traits[i];
            return report;
        }

        //Turn Check Bosses : step 14, option B. Each movement (floor, the eras' until 29 Oct) picks one of two bosses, and
        //the three new ones' rules.
        private static string TurnCheckBosses(List<Musician> band, Enemy hush)
        {
            string report = "";
            string movements = "";
            for (int movement = 0; movement < 3; movement++)
            {
                if (movement > 0) movements += " / ";
                for (int i = 0; i < EnemyList.All.Length; i++)
                    if (EnemyList.All[i].Kind == EnemyKind.Boss && EnemyList.All[i].Movement == movement) movements += EnemyList.All[i].Name + ",";
            }
            report += Same("bosses by movement", "REQUIEM,THE DEAF CROWN, / THE NAMELESS MASTER,THE STAFF, / THE DEVIL'S STRING,THE BLIND CANTOR,", movements);

            //BY TOUCH (THE BLIND CANTOR) : PAGANINI's seat first, then QUANTZ's. Both of its notes got through,
            //so its next attack is its own 2 (L R) and those 2 again (D U), 6 x 2 = 12 each on floor 1
            TurnBattle b = new TurnBattle(band, new List<Enemy> { EnemyNamed("THE BLIND CANTOR") }, 1, new Random(433));
            Sturdy(b);
            string first = b.Enemies[0].IntentTarget.Name;
            PlayToEnemy(b);
            b.TakeEnemyAttack(null);
            Fighter cantor = b.Enemies[0];
            report += Same("by touch", "PAGANINI QUANTZ 4 Left Right Down Up 12", first + " " + cantor.IntentTarget.Name + " " + cantor.IntentNotes.Length + " "
                + cantor.IntentWays[0] + " " + cantor.IntentWays[1] + " " + cantor.IntentWays[2] + " " + cantor.IntentWays[3] + " " + cantor.IntentNotes[3]);

            //THE COURT (THE STAFF) : with HUSH standing, PAGANINI's 6 is halved (250 x 1.6 = 400 -> 397)
            Enemy staffBoss = EnemyNamed("THE STAFF");
            b = new TurnBattle(Duo(), new List<Enemy> { staffBoss, hush }, 1, new Random(433));
            b.PlanAttack(b.Party[0], b.Enemies[0]);
            b.PlayBar();
            b.Attack();
            string court = b.Enemies[0].Hp.ToString();

            //THE COURT : its third attack is one note down that hits everybody, 7 x 2 = 14, x1.5 = 21
            b = new TurnBattle(Duo(), new List<Enemy> { staffBoss }, 1, new Random(433));
            Tough(b);
            for (int i = 0; i < 80 && !b.Enemies[0].IntentAll; i++) Step(b, Grade.Good);
            Fighter staff = b.Enemies[0];
            court += " " + staff.IntentAll + " " + staff.IntentNotes.Length + " " + staff.IntentWays[0] + " " + staff.IntentNotes[0];
            for (int i = 0; i < 20 && !(b.Step == TurnStep.EnemyTurn && b.Current == staff); i++) Step(b, Grade.Good);
            int anna = b.Party[0].Hp;
            int klara = b.Party[1].Hp;
            b.TakeEnemyAttack(null);
            report += Same("the court", "397 True 1 Down 21 21 21", court + " " + (anna - b.Party[0].Hp) + " " + (klara - b.Party[1].Hp));

            //DECREE (THE DEAF CROWN) : bar 1 forbids skills, PAGANINI's CAPRICE costs her 5 (45 -> 40). It CUTS IN after
            //QUANTZ, every note is parried PERFECT and still no COUNTER. Bar 2 forbids attacks, PAGANINI's ATTACK costs 5 more.
            Skill caprice = SkillList.First(MusicianList.All[0]);
            NoteResult[] goods = new NoteResult[caprice.Notes.Length];
            for (int i = 0; i < goods.Length; i++) goods[i] = new NoteResult(Grade.Good, 1f);
            b = new TurnBattle(band, new List<Enemy> { EnemyNamed("THE DEAF CROWN") }, 1, new Random(433));
            b.Breath = 10;
            string decree = b.Decree.ToString();
            b.PlanSkill(b.Party[0], caprice, b.Enemies[0]);
            b.PlayBar();
            b.UseSkill(goods);
            decree += " " + b.Party[0].Hp;
            b.Attack();
            b.Enemies[0].Dissonance = 0;                                               // CAPRICE's DISSONANCE would tick at the end of its turn
            int crownBefore = b.Enemies[0].Hp;
            b.TakeEnemyAttack(AllGrades(b.Current.IntentNotes.Length, Grade.Perfect));
            decree += " " + (crownBefore - b.Enemies[0].Hp);
            b.Attack();
            decree += " " + b.Decree;
            b.PlayBar();
            b.Attack();
            report += Same("decree", "NoSkills 40 0 NoAttacks 35", decree + " " + b.Party[0].Hp);
            return report;
        }

        //Duo : PAGANINI and QUANTZ
        private static List<Musician> Duo()
        {
            return new List<Musician> { MusicianList.All[0], MusicianList.All[1] };
        }

        //Tough : the enemies last long and the band never falls, for checks that play several bars
        private static void Tough(TurnBattle b)
        {
            for (int i = 0; i < b.Enemies.Count; i++)
            {
                b.Enemies[i].MaxHp = 999;
                b.Enemies[i].Hp = 999;
            }
            Sturdy(b);
        }

        //All Notes : every note of a skill swiped with this grade
        private static NoteResult[] AllNotes(Skill s, Grade grade)
        {
            NoteResult[] all = new NoteResult[s.Notes.Length];
            for (int i = 0; i < all.Length; i++) all[i] = new NoteResult(grade, 1f);
            return all;
        }

        //Play Whole Bar : the bar is played to its end with plain ATTACKs and every note let through
        private static void PlayWholeBar(TurnBattle b)
        {
            int bar = b.Round;
            for (int i = 0; i < 30 && b.Round == bar && b.Step != TurnStep.Won && b.Step != TurnStep.Lost; i++)
                Step(b, Grade.Hesitate);
        }
    }
}
