using System;
using System.Collections.Generic;
using Tacetno433.Battle;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //DebugShots.TurnBosses : DEVELOPER TOOL, the checks of the BOSSES (Beta1 31 Oct, docs/BETA1_DIRECTION.md 21), part of
    //--shots turncheck. Each boss's named BIG ATTACK has its own shape (Data/Enemy.cs BossMove, numbers in BattleRules BOSSES),
    //comes every third attack, sooner after THE TURN (half life), and every note blocked PERFECT throws the boss off the beat.
    //Every line is PASS or FAIL like DebugShots.Turn.cs.
    public static partial class DebugShots
    {
        private static string TurnCheckBigs()
        {
            string report = "";
            List<Musician> band = new List<Musician> { MusicianList.All[0], MusicianList.All[1], MusicianList.All[2] };
            MusicianList.ResetRehearsals();

            //Shapes : the six BIG ATTACKs on floor 1, notes / the first note / fast / on everyone / arrows hidden / uneven gaps /
            //the SHAKE's place. REQUIEM 12 x 0.8 = 10, THE DEAF CROWN 12 x 0.75 = 9, THE NAMELESS MASTER 8 x 0.75 = 6 (no skill
            //played : its own 7 notes), THE STAFF 14 x 2.5 = 35, THE DEVIL'S STRING 16 x 0.6 = 10 all one way, THE BLIND CANTOR 12 x 0.55 = 7
            string shapes = "";
            string[] names = { "REQUIEM", "THE DEAF CROWN", "THE NAMELESS MASTER", "THE STAFF", "THE DEVIL'S STRING", "THE BLIND CANTOR" };
            for (int k = 0; k < names.Length; k++)
            {
                TurnBattle b = new TurnBattle(band, new List<Enemy> { EnemyNamed(names[k]) }, 1, new Random(433));
                Tough(b);
                Fighter boss = b.Enemies[0];
                for (int i = 0; i < 80 && !boss.IntentBig; i++) Step(b, Grade.Good);
                if (k > 0) shapes += " / ";
                shapes += BigShape(boss);
            }
            report += Same("big shapes", "8 10 True False 0 0 -1 mixed / 9 9 False False 0 9 -1 mixed / 7 6 False False 0 0 -1 mixed / "
                + "2 35 False True 0 0 1 mixed / 10 10 True False 0 0 -1 one way / 5 7 False True 5 0 -1 mixed", shapes);

            //Every Third : its third attack is the first BIG ATTACK, then the sixth (BigIn back to 3 after it)
            TurnBattle t = new TurnBattle(band, new List<Enemy> { EnemyNamed("REQUIEM") }, 1, new Random(433));
            Tough(t);
            Fighter requiem = t.Enemies[0];
            for (int i = 0; i < 80 && !requiem.IntentBig; i++) Step(t, Grade.Good);
            string every = requiem.Attacks.ToString();
            ToEnemyTurn(t, requiem);
            t.TakeEnemyAttack(null);
            every += " " + requiem.BigIn + " " + requiem.IntentBig;
            for (int i = 0; i < 80 && !requiem.IntentBig; i++) Step(t, Grade.Good);
            every += " " + requiem.Attacks;
            report += Same("big every third", "2 3 False 5", every);

            //THE TURN : one hit takes REQUIEM from 201 of 400 to half. The attack it shows stays, the next one is LACRIMOSA,
            //then every second attack
            t = new TurnBattle(band, new List<Enemy> { EnemyNamed("REQUIEM") }, 1, new Random(433));
            Sturdy(t);
            requiem = t.Enemies[0];
            requiem.Hp = 201;
            t.Events.Clear();
            t.PlanAttack(t.Party[0], requiem);
            t.PlayBar();
            t.Attack();
            string turned = requiem.SecondHalf + " " + CountEvents(t, TurnEventKind.BossTurn) + " " + requiem.BigIn + " " + requiem.IntentBig;
            requiem.Hp = requiem.MaxHp / 2;                                       // the band's other moves must not end the fight
            ToEnemyTurn(t, requiem);
            t.TakeEnemyAttack(null);
            turned += " " + requiem.IntentBig;
            requiem.Hp = requiem.MaxHp / 2;
            ToEnemyTurn(t, requiem);
            t.TakeEnemyAttack(null);
            turned += " " + requiem.BigIn + " " + requiem.IntentBig;
            report += Same("the turn", "True 1 2 False True 2 False", turned);

            //Blocked : every note of LACRIMOSA blocked PERFECT, REQUIEM is STUNNED and the band COUNTERs once
            t = new TurnBattle(band, new List<Enemy> { EnemyNamed("REQUIEM") }, 1, new Random(433));
            Tough(t);
            requiem = t.Enemies[0];
            for (int i = 0; i < 80 && !requiem.IntentBig; i++) Step(t, Grade.Good);
            ToEnemyTurn(t, requiem);
            t.Events.Clear();
            t.TakeEnemyAttack(AllGrades(requiem.IntentNotes.Length, Grade.Perfect));
            report += Same("big blocked", "True 1 1", requiem.Staggered + " " + CountEvents(t, TurnEventKind.BigParried) + " " + CountEvents(t, TurnEventKind.Counter));

            //THE NINTH : THE DEAF CROWN's DECREE refuses the COUNTER of a plain attack blocked whole, but not of THE NINTH
            t = new TurnBattle(band, new List<Enemy> { EnemyNamed("THE DEAF CROWN") }, 1, new Random(433));
            Tough(t);
            Fighter crown = t.Enemies[0];
            ToEnemyTurn(t, crown);
            t.Events.Clear();
            t.TakeEnemyAttack(AllGrades(crown.IntentNotes.Length, Grade.Perfect));
            string heard = CountEvents(t, TurnEventKind.Counter).ToString();
            for (int i = 0; i < 80 && !crown.IntentBig; i++) Step(t, Grade.Good);
            ToEnemyTurn(t, crown);
            t.Events.Clear();
            t.TakeEnemyAttack(AllGrades(crown.IntentNotes.Length, Grade.Perfect));
            heard += " " + CountEvents(t, TurnEventKind.Counter) + " " + CountEvents(t, TurnEventKind.BigParried);
            report += Same("the ninth heard", "0 1 1", heard);

            //TE DEUM : the staff's blow blocked PERFECT and the SHAKE missed, THE STAFF loses 8 percent of its 999 once its notes are done
            t = new TurnBattle(band, new List<Enemy> { EnemyNamed("THE STAFF") }, 1, new Random(433));
            Tough(t);
            Fighter staff = t.Enemies[0];
            for (int i = 0; i < 80 && !staff.IntentBig; i++) Step(t, Grade.Good);
            ToEnemyTurn(t, staff);
            staff.OffBeat = 0;                                                    // the band's hits so far must not throw it off the beat now
            int staffBefore = staff.Hp;
            t.Events.Clear();
            t.TakeEnemyAttack(new Grade[] { Grade.Perfect, Grade.Miss });
            report += Same("te deum foot", "80 1 0 False", (staffBefore - staff.Hp) + " " + CountEvents(t, TurnEventKind.OwnFoot) + " "
                + CountEvents(t, TurnEventKind.BigParried) + " " + staff.Staggered);

            //ON ONE STRING : 10 notes through take 10 BREATH one by one (no BARGAIN steal on top). All blocked : STUNNED, and
            //10 parries x TurnBreathPerParry + TurnStringBack back, at most TurnBreathMax
            t = new TurnBattle(band, new List<Enemy> { EnemyNamed("THE DEVIL'S STRING") }, 1, new Random(433));
            Tough(t);
            Fighter strings = t.Enemies[0];
            for (int i = 0; i < 80 && !strings.IntentBig; i++) Step(t, Grade.Good);
            ToEnemyTurn(t, strings);
            t.Breath = 10;
            t.Events.Clear();
            t.TakeEnemyAttack(null);
            string oneString = t.Breath + " " + CountEvents(t, TurnEventKind.Steal);
            t = new TurnBattle(band, new List<Enemy> { EnemyNamed("THE DEVIL'S STRING") }, 1, new Random(433));
            Tough(t);
            strings = t.Enemies[0];
            for (int i = 0; i < 80 && !strings.IntentBig; i++) Step(t, Grade.Good);
            ToEnemyTurn(t, strings);
            t.Breath = 0;
            t.TakeEnemyAttack(AllGrades(strings.IntentNotes.Length, Grade.Perfect));
            oneString += " " + strings.Staggered + " " + t.Breath;
            int back = Math.Min(BattleRules.TurnBreathMax, 10 * BattleRules.TurnBreathPerParry + BattleRules.TurnStringBack);
            report += Same("on one string", "0 10 True " + back, oneString);

            //ITALIEN : the turn THE NAMELESS MASTER shows it, PAGANINI plays CAPRICE, and ITALIEN becomes CAPRICE's notes
            //(round again to at least 5)
            Skill caprice = SkillList.First(MusicianList.All[0]);
            List<Flick> capriceWays = new List<Flick>();
            for (int n = 0; n < caprice.Notes.Length; n++)
                if (caprice.Notes[n].Kind == NoteKind.Swipe || caprice.Notes[n].Kind == NoteKind.Hold) capriceWays.Add(caprice.Notes[n].Way);
            int italienCount = Math.Max(BattleRules.TurnBigNotes[(int)BossMove.Italien], Math.Min(BattleRules.TurnItalienMost, capriceWays.Count));
            t = new TurnBattle(band, new List<Enemy> { EnemyNamed("THE NAMELESS MASTER") }, 1, new Random(433));
            Tough(t);
            Fighter master = t.Enemies[0];
            for (int i = 0; i < 80 && !(master.IntentBig && t.Step == TurnStep.Plan); i++) Step(t, Grade.Good);
            t.Breath = 10;
            t.PlanSkill(t.Party[0], caprice, master);
            t.PlayBar();
            t.UseSkill(AllNotes(caprice, Grade.Good));
            report += Same("italien", italienCount + " " + capriceWays[0] + " " + capriceWays[1 % capriceWays.Count],
                master.IntentNotes.Length + " " + master.IntentWays[0] + " " + master.IntentWays[1]);

            //Words : every boss has its BIG ATTACK, the lines the screen shows, and whose hollow it is
            int written = 0;
            for (int i = 0; i < EnemyList.All.Length; i++)
            {
                Enemy e = EnemyList.All[i];
                if (e.Kind != EnemyKind.Boss) continue;
                if (e.Big != BossMove.None && e.BigName.Length > 0 && e.BigText.Length > 0 && e.BigWin.Length > 0 && e.PhaseLine.Length > 0
                    && e.HollowOf != TurnKit.None) written++;
            }
            report += Same("boss lines", "6", written.ToString());
            return report;
        }

        //Big Shape : what the BIG ATTACK the boss shows asks for, in one line for the check above
        private static string BigShape(Fighter boss)
        {
            int late = 0;
            for (int i = 0; i < boss.IntentLate.Length; i++) if (boss.IntentLate[i]) late++;
            int shake = -1;
            for (int i = 0; i < boss.IntentKinds.Length; i++) if (boss.IntentKinds[i] == NoteKind.Roll) shake = i;
            bool oneWay = boss.IntentWays.Length > 1;
            for (int i = 1; i < boss.IntentWays.Length; i++) if (boss.IntentWays[i] != boss.IntentWays[0]) oneWay = false;
            int first = boss.IntentNotes.Length > 0 ? boss.IntentNotes[0] : 0;
            return boss.IntentNotes.Length + " " + first + " " + boss.IntentFast + " " + boss.IntentAll + " " + late + " "
                + boss.IntentGaps.Length + " " + shake + " " + (oneWay ? "one way" : "mixed");
        }

        //To Enemy Turn : play the fight on (the band ATTACKs, TACET's notes are answered GOOD) until it is this enemy's turn
        private static void ToEnemyTurn(TurnBattle b, Fighter enemy)
        {
            for (int i = 0; i < 40 && !(b.Step == TurnStep.EnemyTurn && b.Current == enemy) && b.Step != TurnStep.Won && b.Step != TurnStep.Lost; i++)
                Step(b, Grade.Good);
        }
    }
}
