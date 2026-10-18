using System;
using System.Collections.Generic;
using System.IO;
using Tacetno433.Battle;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //DebugShots.Turn : DEVELOPER TOOL, checks the rules of the turn battle (Alpha 7).
    //   Tacetno433.exe --shots turncheck --out <folder>      writes turncheck.txt
    //
    //Plays short scripted fights with Battle/TurnBattle.cs, no screen involved, and writes one
    //PASS or FAIL line per rule : life, turn order, damage for each swipe, parry, breath,
    //knocked out, win, lose, two enemies and an enemy whose target fell. The random numbers
    //use a fixed seed, so every run gives the same report. The expected numbers follow the
    //TURN BATTLE section of Data/BattleRules.cs : change a number there and update them here.
    public static partial class DebugShots
    {
        private static void TurnCheck()
        {
            string report = "TACET TURN BATTLE CHECK\r\n";
            MusicianList.ResetRehearsals();

            //ANNA strings power 5, KLARA winds power 4, OTTO percussion power 6
            //HUSH : its Pattern notes average 4, so each of its notes hits round(4 x 1.5) = 6
            List<Musician> band = new List<Musician> { MusicianList.All[0], MusicianList.All[1], MusicianList.All[2] };
            Enemy hush = EnemyList.All[0];
            Enemy deadAir = EnemyList.All[1];

            //Life and turn order
            TurnBattle b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            report += Same("life", "30/26/36/50", b.Party[0].Hp + "/" + b.Party[1].Hp + "/" + b.Party[2].Hp + "/" + b.Enemies[0].Hp);
            report += Same("order", "KLARA ANNA HUSH OTTO", OrderNames(b));
            report += Same("first turn", "PlayerTurn KLARA", b.Step + " " + b.Current.Name);
            report += Same("breath start", "3", b.Breath.ToString());

            //Swipes : PERFECT 4 x 2 x 1.3 = 10.4, GOOD 5 x 2 = 10
            Fighter enemy = b.Enemies[0];
            int hit = b.Attack(enemy, Grade.Perfect, true);
            report += Same("perfect hit", "10 40 4", hit + " " + enemy.Hp + " " + b.Breath);
            hit = b.Attack(enemy, Grade.Good, true);
            report += Same("good hit", "10 30 5", hit + " " + enemy.Hp + " " + b.Breath);

            //Enemy turn : 1 or 2 notes of 6 on floor 1, at a musician who is standing
            report += Same("enemy turn", "EnemyTurn HUSH", b.Step + " " + b.Current.Name);
            int notes = enemy.IntentNotes.Length;
            bool intentOk = notes >= 1 && notes <= 2 && enemy.IntentNotes[0] == 6 && enemy.IntentTarget != null && !enemy.IntentTarget.Down;
            report += Same("intent", "True", intentOk.ToString());

            //PARRY every note PERFECT : no damage, 1 breath per note
            Grade[] allPerfect = new Grade[notes];
            for (int i = 0; i < notes; i++) allPerfect[i] = Grade.Perfect;
            int taken = b.TakeEnemyAttack(allPerfect);
            report += Same("parry perfect", "0 " + (5 + notes), taken + " " + b.Breath);

            //Wrong size : 6 x 2 x 0.7 = 8.4, then round two starts with KLARA again
            hit = b.Attack(enemy, Grade.Perfect, false);
            report += Same("wrong size", "8 22", hit + " " + enemy.Hp);
            report += Same("round two", "2 KLARA", b.Round + " " + b.Current.Name);

            //Miss : 4 x 2 x 0.4 = 3.2
            hit = b.Attack(enemy, Grade.Miss, true);
            report += Same("miss", "3 19", hit + " " + enemy.Hp);

            //Swipe table
            report += Same("swipe table", "1.3 1 0.7 0.4 0.4",
                TurnBattle.SwipeScale(Grade.Perfect, true) + " " + TurnBattle.SwipeScale(Grade.Good, true) + " " +
                TurnBattle.SwipeScale(Grade.Perfect, false) + " " + TurnBattle.SwipeScale(Grade.Miss, true) + " " +
                TurnBattle.SwipeScale(Grade.Hesitate, true));

            //PARRY GOOD lets half through (6 -> 3 a note), no swipe lets all of it through
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Attack(b.Enemies[0], Grade.Good, true);
            b.Attack(b.Enemies[0], Grade.Good, true);
            notes = b.Current.IntentNotes.Length;
            Grade[] allGood = new Grade[notes];
            for (int i = 0; i < notes; i++) allGood[i] = Grade.Good;
            report += Same("parry good", (3 * notes).ToString(), b.TakeEnemyAttack(allGood).ToString());
            b.Attack(b.Enemies[0], Grade.Good, true);
            b.Attack(b.Enemies[0], Grade.Good, true);
            b.Attack(b.Enemies[0], Grade.Good, true);
            notes = b.Current.IntentNotes.Length;
            report += Same("no parry", (6 * notes).ToString(), b.TakeEnemyAttack(null).ToString());

            //KNOCKED OUT : KLARA starts the fight down, sits out the turn order and gets up with 1 after it
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433), new int[] { 30, 0, 36 });
            report += Same("knocked out", "ANNA HUSH OTTO", OrderNames(b));
            report += Same("revive", "1 30", b.HpAfterBattle(b.Party[1]) + " " + b.HpAfterBattle(b.Party[0]));

            //Win : the last hit on the last enemy ends the fight
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Enemies[0].Hp = 1;
            b.Attack(b.Enemies[0], Grade.Miss, true);
            report += Same("win", "Won True", b.Step + " " + (b.Current == null));

            //Lose : only OTTO is standing with 1 life, HUSH is faster and hits him
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433), new int[] { 0, 0, 1 });
            report += Same("lose order", "HUSH OTTO", OrderNames(b));
            b.TakeEnemyAttack(null);
            report += Same("lose", "Lost", b.Step.ToString());

            //Two enemies : the first one down leaves the fight, the other carries on
            b = new TurnBattle(band, new List<Enemy> { hush, deadAir }, 1, new Random(433));
            report += Same("two order", "KLARA ANNA HUSH DEAD AIR OTTO", OrderNames(b));
            b.Enemies[0].Hp = 1;
            b.Attack(b.Enemies[0], Grade.Good, true);
            report += Same("one down", "PlayerTurn ANNA", b.Step + " " + b.Current.Name);
            b.Attack(b.Enemies[1], Grade.Good, true);
            report += Same("skip the fallen", "EnemyTurn DEAD AIR", b.Step + " " + b.Current.Name);
            b.TakeEnemyAttack(null);
            b.Attack(b.Enemies[1], Grade.Good, true);
            report += Same("next round", "KLARA ANNA DEAD AIR OTTO", OrderNames(b));

            //Target fell : an enemy whose chosen musician is down by its turn picks another one
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Enemies[0].IntentTarget.Hp = 0;
            while (b.Step == TurnStep.PlayerTurn) b.Attack(b.Enemies[0], Grade.Good, true);
            bool retarget = b.Step == TurnStep.EnemyTurn && b.Current.IntentTarget != null && !b.Current.IntentTarget.Down;
            report += Same("retarget", "True", retarget.ToString());

            //Breath never goes past the top
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            for (int i = 0; i < 40; i++)
            {
                b.Enemies[0].Hp = 50;
                if (b.Step == TurnStep.PlayerTurn) b.Attack(b.Enemies[0], Grade.Good, true);
                else if (b.Step == TurnStep.EnemyTurn) b.TakeEnemyAttack(new Grade[] { Grade.Perfect, Grade.Perfect, Grade.Perfect, Grade.Perfect });
            }
            report += Same("breath top", BattleRules.TurnBreathMax.ToString(), b.Breath.ToString());

            File.WriteAllText(Path.Combine(outDir, "turncheck.txt"), report);
        }

        private static string OrderNames(TurnBattle b)
        {
            string s = "";
            for (int i = 0; i < b.Order.Count; i++) s += (i > 0 ? " " : "") + b.Order[i].Name;
            return s;
        }
    }
}
