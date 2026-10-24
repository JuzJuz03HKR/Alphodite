using System;
using System.Collections.Generic;
using Tacetno433.Battle;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //DebugShots.TurnContent : DEVELOPER TOOL, the checks of step 13 (Alpha 7), part of --shots turncheck.
    //The six ONE BAR motifs, the five new enemies and the bugs fixed with them.
    //The musicians' own traits are switched off here so the numbers stay plain (ANNA 10, KLARA 8, OTTO 12 an ATTACK).
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

            //Lists : 21 + 6 motifs, 9 + 5 enemies, 7 + 3 events, the new ones at the end (the save keeps their places)
            report += Same("content", "27 14 10 ROOM TONE", MotifList.All.Length + " " + EnemyList.All.Length + " " + EventList.All.Length + " " + EnemyList.All[9].Name);

            //DOWNBEAT : the first move of the bar x1.5 (ANNA 10 -> 15), the second plain (KLARA 8)
            TurnBattle b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.GiveMotifs(new List<Motif> { MotifList.Get(MotifId.Downbeat) });
            b.PlayBar();
            int down1 = b.Attack();
            int down2 = b.Attack();
            report += Same("downbeat", "15 8", down1 + " " + down2);

            //ENTRANCE : OTTO is called in bar 1, so in bar 2 he hits x2 (24), in bar 3 plainly (12)
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433), null, 0, 2);
            Tough(b);
            b.GiveMotifs(new List<Motif> { MotifList.Get(MotifId.Entrance) });
            b.Breath = 10;
            b.Call(0);
            PlayWholeBar(b);
            b.PlayBar();
            b.Attack();
            b.Attack();
            int entrance = b.Attack();
            PlayWholeBar(b);
            b.PlayBar();
            b.Attack();
            b.Attack();
            report += Same("entrance", "24 12", entrance + " " + b.Attack());

            //GREEN ROOM : OTTO waits on the BENCH with 20 life, one bar later he has 22
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433), new int[] { 30, 26, 20 }, 0, 2);
            Tough(b);
            b.GiveMotifs(new List<Motif> { MotifList.Get(MotifId.GreenRoom) });
            string before = b.BenchLife[0].ToString();
            PlayWholeBar(b);
            report += Same("green room", "20 22", before + " " + b.BenchLife[0]);

            //UNISON : the whole bar ATTACKs HUSH, so KLARA's last ATTACK is x2 (16). KLARA at DEAD AIR instead : no unison (8)
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
            report += Same("unison", "10 16 8", uni1 + " " + uni2 + " " + b.Attack());

            //SEGUE : HUSH has 5 life, ANNA's 10 silences it and the 5 left over go on to DEAD AIR (50 -> 45). Without it 50.
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
            report += Same("segue", "0 45 50", segue + " " + b.Enemies[1].Hp);

            //LAST WORD : KLARA plays the bar's last move, so she stands GUARD and ANNA does not
            b = new TurnBattle(Duo(), new List<Enemy> { hush }, 1, new Random(433));
            b.GiveMotifs(new List<Motif> { MotifList.Get(MotifId.LastWord) });
            b.PlayBar();
            b.Attack();
            b.Attack();
            report += Same("last word", "False True", b.Party[0].Guard + " " + b.Party[1].Guard);

            //HOLDS THE ROOM (ROOM TONE, floor 2) : HUSH takes 70 percent while it stands (ANNA 10 -> 7, 65 -> 58),
            //ROOM TONE itself takes all of it. On floor 1 the trait is off.
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
            report += Same("holds the room", "58 8 40", room + " " + b.Enemies[0].Hp);

            //ON CUE (THE CUE, floor 2) : it turns to OTTO the moment he is called
            Enemy cue = EnemyNamed("THE CUE");
            b = new TurnBattle(band, new List<Enemy> { cue }, 2, new Random(433), null, 0, 2);
            b.Breath = 10;
            string aimed = b.Enemies[0].IntentTarget.Name;
            b.Call(0);
            report += Same("on cue", "True OTTO", (aimed != "OTTO") + " " + b.Enemies[0].IntentTarget.Name);

            //FELT HAND (THE DAMPER, floor 2) : a CALL costs 3 + 1 while it stands, 3 once it is silenced
            b = new TurnBattle(band, new List<Enemy> { EnemyNamed("THE DAMPER") }, 2, new Random(433), null, 0, 2);
            int damped = b.CallCost;
            b.Enemies[0].Hp = 0;
            report += Same("felt hand", "4 3", damped + " " + b.CallCost);

            //TWICE A BAR (STUTTER, floor 2) : it cuts in after the 1st and the 3rd move with one note each time.
            //On floor 1 the trait is off and it is a plain CUT IN after the 2nd move.
            Enemy stutter = EnemyNamed("STUTTER");
            b = new TurnBattle(band, new List<Enemy> { stutter, hush }, 2, new Random(433));
            b.PlayBar();
            string twice = OrderNames(b) + " " + b.Enemies[0].IntentNotes.Length;
            b = new TurnBattle(band, new List<Enemy> { stutter, hush }, 1, new Random(433));
            b.PlayBar();
            report += Same("twice a bar", "ANNA STUTTER KLARA OTTO STUTTER HUSH 1 / ANNA KLARA STUTTER OTTO HUSH", twice + " / " + OrderNames(b));

            //FIRST IN LINE (THE ROUND) : it aims at the first musician of the bar, and turns as the bar is written
            b = new TurnBattle(band, new List<Enemy> { EnemyNamed("THE ROUND") }, 1, new Random(433));
            string line = b.Enemies[0].IntentTarget.Name;
            b.PlanAttack(b.Party[2], b.Enemies[0]);
            line += " " + b.Enemies[0].IntentTarget.Name;
            b.Unplan(b.Party[2]);
            report += Same("first in line", "ANNA OTTO ANNA", line + " " + b.Enemies[0].IntentTarget.Name);

            //Fallen Target (bug fix) : HUSH aims at someone after its attack, they fall later in the bar,
            //so when the next bar is written HUSH aims at somebody standing
            b = new TurnBattle(band, new List<Enemy> { hush, EnemyNamed("THE LULL") }, 1, new Random(433));
            PlayToEnemy(b);
            b.TakeEnemyAttack(null);
            Fighter aimedAt = b.Enemies[0].IntentTarget;
            aimedAt.Hp = 0;
            b.TakeEnemyAttack(null);
            report += Same("fallen target", "Plan False", b.Step + " " + b.Enemies[0].IntentTarget.Down);

            for (int i = 0; i < traits.Length; i++) MusicianList.All[i].Trait = traits[i];
            return report;
        }

        //Duo : ANNA and KLARA
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

        //Play Whole Bar : the bar is played to its end with plain ATTACKs and every note let through
        private static void PlayWholeBar(TurnBattle b)
        {
            int bar = b.Round;
            for (int i = 0; i < 30 && b.Round == bar && b.Step != TurnStep.Won && b.Step != TurnStep.Lost; i++)
                Step(b, Grade.Hesitate);
        }
    }
}
