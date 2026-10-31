using System;
using System.Collections.Generic;
using Tacetno433.Battle;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //DebugShots.TurnRehearsal : DEVELOPER TOOL, the checks of REHEARSAL (Beta1 31 Oct, docs/BETA1_DIRECTION.md 17), SIGNATURE
    //GROWS and BONDS, part of --shots turncheck (it took the place of the PATH and INSPIRED checks). How a star is given and
    //what it rewrites (Data/Rehearsal.cs), every button's three stars in a fight, the ATTACK's legends, and every button of the
    //five legends walked to its third star. Like DebugShots.Turn.cs every line is PASS or FAIL with what was expected and what
    //came out. The musicians' traits are on here. Every musician is back to no star at the end (MusicianList.ResetRehearsals).
    public static partial class DebugShots
    {
        private static string TurnCheckRehearsal()
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

            //Stars : CAPRICE's first star rewrites it (5 cuts) and leaves the plain skill in SkillList alone, the next star says what
            //it changes, the fourth cannot be given, the third renames it (its legend) and gives its rule. The ATTACK's three are the
            //weapon's tiers, the third gives the weapon's legend. With all nine nothing is left, and a new run forgets them all.
            string stars = RehearsalList.CanRaise(paganini, 1) + " " + RehearsalList.Raise(paganini, 1) + " " + SkillList.First(paganini).Notes.Length
                           + " " + SkillList.Plain(paganini, 0).Notes.Length + " " + RehearsalList.Next(paganini, 1).From;
            RehearsalList.Raise(paganini, 1);
            RehearsalList.Raise(paganini, 1);
            stars += " " + RehearsalList.Raise(paganini, 1) + " " + RehearsalList.StarsOf(paganini, 1) + " " + SkillList.First(paganini).Name + " " + paganini.Has(StarRule.LastCut);
            for (int k = 0; k < 3; k++) RehearsalList.Raise(paganini, 0);
            stars += " " + paganini.Rehearsed + " " + paganini.Has(StarRule.AttackDissonance) + " " + RehearsalList.CanRaise(paganini, 0);
            for (int k = 0; k < 3; k++) RehearsalList.Raise(paganini, 2);
            stars += " " + RehearsalList.AnyLeft(paganini) + " " + SkillList.Second(paganini).Name;
            MusicianList.ResetRehearsals();
            stars += " / " + SkillList.First(paganini).Name + " " + paganini.Rehearsed + " " + paganini.Has(StarRule.AttackDissonance) + " " + paganini.Has(StarRule.LastCut);
            report += Same("stars", "True True 5 4 1 DISSONANCE False 3 CAPRICE No. 24 True 3 True False False DEVIL'S CANNON / CAPRICE 0 False False", stars);

            //Star Words : what the page and the shop show. The ATTACK : the power (5 to 7). IL CANNONE's first star : its held hit
            //(5 x 2 x 1.8 x 1.4 = 25.2 -> 25, x2.2 : 30.8 -> 31). Its third : the name becoming the legend's.
            string words = RehearsalList.FromText(paganini, 0, 5) + " / " + RehearsalList.ToText(paganini, 0, 5) + " / "
                           + RehearsalList.FromText(paganini, 2, 5) + " / " + RehearsalList.ToText(paganini, 2, 5);
            Walk(paganini, 2, 2);
            words += " / " + RehearsalList.FromText(paganini, 2, 5) + " / " + RehearsalList.ToText(paganini, 2, 5);
            MusicianList.ResetRehearsals();
            report += Same("star words", "POWER 5 / POWER 7 / HIT 25 / HIT 31 / IL CANNONE / DEVIL'S CANNON", words);

            //CAPRICE's three stars : CAPRICE No. 24, 6 cuts all one way, each GOOD 5 x 2 x 0.4 = 4, the last x3 (5 x 4 + 12 = 32), 2
            //DISSONANCE a cut up to TurnDissonanceMost (9). The renamed skill is still CAPRICE for its rule (TurnBattle.IsSkill).
            Walk(paganini, 1, 3);
            Skill capriceNow = SkillList.First(paganini);
            TurnBattle b = new TurnBattle(Solo(0), new List<Enemy> { hush }, 1, new Random(433));
            Tough(b);
            b.Breath = 10;
            b.PlanSkill(b.Party[0], capriceNow, b.Enemies[0]);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { good, good, good, good, good, good });
            string ways = capriceNow.Notes.Length + " " + capriceNow.Notes[0].Way + " " + capriceNow.Notes[5].Way;
            report += Same("caprice no. 24", "6 Left Left 32 1 " + BattleRules.TurnDissonanceMost, ways + " " + (999 - b.Enemies[0].Hp) + " " + CountLegend(b, StarRule.LastCut) + " " + b.Enemies[0].Dissonance);
            MusicianList.ResetRehearsals();

            //IL CANNONE's three stars : DEVIL'S CANNON at HUSH also hits STATIC (2 DISSONANCE), not DEAD AIR (none), x2.6 held to
            //the end : 5 x 2 x 2.6 x 1.4 = 36.4 -> 36
            Walk(paganini, 2, 3);
            b = new TurnBattle(Solo(0), new List<Enemy> { hush, staticEnemy, deadAir }, 1, new Random(433));
            Tough(b);
            b.Breath = 10;
            b.Enemies[1].Dissonance = 2;
            b.PlanSkill(b.Party[0], SkillList.Second(paganini), b.Enemies[0]);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { good });
            report += Same("devil's cannon", "36 36 0", (999 - b.Enemies[0].Hp) + " " + (999 - b.Enemies[1].Hp) + " " + (999 - b.Enemies[2].Hp));
            MusicianList.ResetRehearsals();

            //THE LESSON's three stars : it reaches both friends at once for 3 BREATH, each x1.6 and an ECHO of half waits on each
            //(KING'S LESSON). QUANTZ never teaches themself.
            Walk(quantz, 2, 3);
            b = new TurnBattle(new List<Musician> { quantz, paganini, pfundt }, new List<Enemy> { hush }, 1, new Random(433));
            Tough(b);
            b.Breath = 10;
            b.PlanSkill(b.Party[0], SkillList.Second(quantz), null);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { good, good });
            string taught = b.Party[0].Lesson + " " + b.Party[1].Lesson + " " + b.Party[2].Lesson + " " + b.Party[1].LessonScale + " " + b.Party[2].Echo;
            report += Same("the lesson", "False True True 1.6 0.5 3 KING'S LESSON", taught + " " + SkillList.Second(quantz).Cost + " " + SkillList.Second(quantz).Name);

            //SOFT AIR's three stars : 3 notes of 4 (12 back) for 3 BREATH, and SANSSOUCI clears DISSONANCE and MUTE
            Walk(quantz, 1, 3);
            b = new TurnBattle(new List<Musician> { quantz, paganini }, new List<Enemy> { hush }, 1, new Random(433));
            b.Breath = 10;
            b.Party[1].Hp = 20;
            b.Party[1].Dissonance = 3;
            b.Party[1].Mute = 1;
            b.PlanSkill(b.Party[0], SkillList.First(quantz), null);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { good, good, good });
            report += Same("sanssouci", "32 0 0 3", b.Party[1].Hp + " " + b.Party[1].Dissonance + " " + b.Party[1].Mute + " " + SkillList.First(quantz).Cost);
            MusicianList.ResetRehearsals();

            //TIMPANI ROLL's three stars : it hits every enemy for 3 BREATH, a PERFECT throws the first one OFF-BEAT at once
            //(THUNDERCLAP), so HUSH loses its turn of this bar and DEAD AIR only fills its gauge (40)
            Walk(pfundt, 1, 3);
            b = new TurnBattle(Solo(2), new List<Enemy> { hush, deadAir }, 1, new Random(433));
            Tough(b);
            b.Breath = 10;
            b.PlanSkill(b.Party[0], SkillList.First(pfundt), null);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { perfect });
            report += Same("thunderclap", "40 3 1 1", SkillList.First(pfundt).OffBeat + " " + SkillList.First(pfundt).Cost + " " + CountEvents(b, TurnEventKind.Break) + " " + CountEvents(b, TurnEventKind.Skip));
            MusicianList.ResetRehearsals();

            //THUNDER CALL's three stars : every enemy's next attack loses 2 notes (DIE PAUKEN), never fewer than 2 left (HUSH 4 -> 2,
            //DEAD AIR 3 -> 2), and the notes that land on PFUNDT hurt a third (8 -> 3 each)
            Walk(pfundt, 2, 3);
            b = new TurnBattle(Solo(2), new List<Enemy> { hush, deadAir }, 1, new Random(433));
            Tough(b);
            b.Breath = 10;
            b.PlanSkill(b.Party[0], SkillList.Second(pfundt), null);
            b.PlayBar();
            LongPhrase(b.Enemies[0], 4);
            LongPhrase(b.Enemies[1], 3);
            b.UseSkill(new NoteResult[] { good });
            string call = b.Enemies[0].IntentNotes.Length + " " + b.Enemies[1].IntentNotes.Length + " " + SkillList.Second(pfundt).Name;
            b = new TurnBattle(Solo(2), new List<Enemy> { hush }, 1, new Random(433));
            Tough(b);
            b.Breath = 10;
            b.PlanSkill(b.Party[0], SkillList.Second(pfundt), null);
            b.PlayBar();
            LongPhrase(b.Enemies[0], 4);
            b.UseSkill(new NoteResult[] { good });
            int before = b.Party[0].Hp;
            call += " " + b.Step + " " + b.Current.IntentNotes.Length;
            b.TakeEnemyAttack(null);
            call += " " + (before - b.Party[0].Hp);
            report += Same("die pauken", "2 2 DIE PAUKEN EnemyTurn 2 6", call);
            MusicianList.ResetRehearsals();

            //SHIELD's three stars : SHIELD stands in front of both friends, takes a third of each note (8 -> 3) and the blows strike
            //HUSH back as hard (NAPOLEON'S DENT)
            Walk(duport, 1, 3);
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
            report += Same("napoleon's dent", "True True " + (3 * notes) + " " + (3 * notes), wall);
            MusicianList.ResetRehearsals();

            //LANCE's three stars : 3 thrusts, always x3 even when DUPORT was not hit (THE LAST LANCE) : 6 x 2 x 0.7 x 3 = 25.2 -> 25,
            //75 (the word says x3). With two stars and no blow taken : 8.4 -> 8, 24.
            Walk(duport, 2, 3);
            b = new TurnBattle(Solo(6), new List<Enemy> { hush }, 1, new Random(433));
            Tough(b);
            b.Breath = 10;
            b.PlanSkill(b.Party[0], SkillList.Second(duport), b.Enemies[0]);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { good, good, good });
            string lance = (999 - b.Enemies[0].Hp) + " " + EventAmount(b, TurnEventKind.Revenge);
            Walk(duport, 2, 2);
            b = new TurnBattle(Solo(6), new List<Enemy> { hush }, 1, new Random(433));
            Tough(b);
            b.Breath = 10;
            b.PlanSkill(b.Party[0], SkillList.Second(duport), b.Enemies[0]);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { good, good, good });
            report += Same("the last lance", "75 3 24", lance + " " + (999 - b.Enemies[0].Hp));
            MusicianList.ResetRehearsals();

            //FANFARE's three stars : PUNTO first, 2 CRESCENDO a note (4 each), then the two after PUNTO both get THE HUNTING CALL at x1.5
            //(THE WHOLE HUNT) : PAGANINI 5 x 2 x 0.55 x 1.5 x 1.6 = 13.2 -> 13, QUANTZ 4 x 2 x 0.55 x 1.5 x 1.6 = 10.56 -> 11
            Walk(punto, 1, 3);
            b = new TurnBattle(new List<Musician> { punto, paganini, quantz }, new List<Enemy> { hush }, 1, new Random(433));
            Tough(b);
            b.Breath = 10;
            b.PlanSkill(b.Party[0], SkillList.First(punto), null);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { good, good });
            string hunt = b.Party[1].Crescendo + " " + b.Attack() + " " + b.Attack();
            report += Same("the whole hunt", "4 13 11 2", hunt + " " + CountEvents(b, TurnEventKind.Cue));
            MusicianList.ResetRehearsals();

            //CHAKRAM's three stars : 4 throws at HUSH alone, the second to fourth whole (10 each), the last one at every enemy
            //(WHIRLWIND) : with HUSH and DEAD AIR, HUSH takes 10 + 10 (+ the last 10), DEAD AIR 10 + 10
            Walk(punto, 2, 3);
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
            report += Same("whirlwind", "40 30 20", thrown + " " + (999 - b.Enemies[0].Hp) + " " + (999 - b.Enemies[1].Hp));
            MusicianList.ResetRehearsals();

            //The ATTACK's Legends : three stars on the weapon (power + 6). PAGANINI 11 x 2 x 0.55 = 12.1 -> 12 and 1 DISSONANCE /
            //QUANTZ's ATTACK heals PAGANINI (20 -> 24) / PFUNDT 13.2 -> 13 twice / DUPORT stands GUARD (8 -> 4 a note) / PUNTO 12.1 -> 12
            //on HUSH and on DEAD AIR
            report += Same("diabolus in musica", "12 1 1", WeaponCheck(0, hush, null));
            Walk(quantz, 0, 3);
            b = new TurnBattle(new List<Musician> { quantz, paganini }, new List<Enemy> { hush }, 1, new Random(433));
            Tough(b);
            b.Party[1].Hp = 20;
            b.PlayBar();
            b.Attack(b.Enemies[0]);
            report += Same("evening concert", "24 1", b.Party[1].Hp + " " + CountLegend(b, StarRule.AttackHeals));
            MusicianList.ResetRehearsals();
            report += Same("twin thunder", "13 26 1", WeaponCheck(2, hush, null));
            Walk(duport, 0, 3);
            b = new TurnBattle(Solo(6), new List<Enemy> { hush }, 1, new Random(433));
            Tough(b);
            b.PlayBar();
            b.Attack(b.Enemies[0]);
            LongPhrase(b.Enemies[0], 3);
            string guarded = b.Party[0].Guard + " " + b.Step;
            before = b.Party[0].Hp;
            b.TakeEnemyAttack(null);
            report += Same("stradivarius", "True EnemyTurn 12", guarded + " " + (before - b.Party[0].Hp));
            MusicianList.ResetRehearsals();
            report += Same("wider arc", "12 12 1 12", WeaponCheck(7, hush, deadAir));

            //Every Star : every button of the five legends takes its three stars and no fourth, a skill's third gives it its legend's
            //name, the ATTACK's third the weapon's legend. A new run takes them all back.
            int walked = 0;
            for (int i = 0; i < MusicianList.Shown.Length; i++)
            {
                Musician m = MusicianList.Shown[i];
                for (int button = 0; button < RehearsalList.Buttons; button++)
                {
                    bool ok = RehearsalList.Raise(m, button) && RehearsalList.Raise(m, button) && RehearsalList.Raise(m, button) && !RehearsalList.Raise(m, button);
                    if (button == 0) ok &= RehearsalList.WeaponLegend(m) != StarRule.None && m.Has(RehearsalList.WeaponLegend(m));
                    else ok &= RehearsalList.SkillOf(m, button).Name == RehearsalList.Of(m)[button][RehearsalList.Most - 1].Name;
                    if (ok) walked++;
                }
            }
            MusicianList.ResetRehearsals();
            int left = 0;
            for (int i = 0; i < MusicianList.Shown.Length; i++)
                for (int button = 0; button < RehearsalList.Buttons; button++) left += RehearsalList.StarsOf(MusicianList.Shown[i], button);
            report += Same("every star", "15 0", walked + " " + left);

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

        //Weapon Check : musician i of MusicianList with the ATTACK's three stars hits HUSH (and the next enemy, if there is one) :
        //the ATTACK's damage, the first enemy's loss, the legend's words written down, and the second enemy's loss
        private static string WeaponCheck(int i, Enemy first, Enemy second)
        {
            Musician m = MusicianList.All[i];
            Walk(m, 0, 3);
            List<Enemy> pack = new List<Enemy> { first };
            if (second != null) pack.Add(second);
            TurnBattle b = new TurnBattle(Solo(i), pack, 1, new Random(433));
            Tough(b);
            b.PlayBar();
            int hit = b.Attack(b.Enemies[0]);
            string done = hit + " " + (999 - b.Enemies[0].Hp);
            if (RehearsalList.WeaponLegend(m) == StarRule.AttackDissonance) done = hit + " " + b.Enemies[0].Dissonance;
            done += " " + CountLegend(b, RehearsalList.WeaponLegend(m));
            if (second != null) done += " " + (999 - b.Enemies[1].Hp);
            MusicianList.ResetRehearsals();
            return done;
        }

        //Walk : button b of the musician has this many stars (0 the ATTACK : the weapon's tier)
        private static void Walk(Musician m, int button, int stars)
        {
            if (button == 0) m.Rehearsed = stars;
            else m.SkillStars[button - 1] = stars;
            RehearsalList.Rebuild(m);
        }

        //Count Legend : how many times the rule of a star was written down as working
        private static int CountLegend(TurnBattle b, StarRule rule)
        {
            int count = 0;
            for (int i = 0; i < b.Events.Count; i++)
                if (b.Events[i].Kind == TurnEventKind.Legend && b.Events[i].Amount == (int)rule) count++;
            return count;
        }
    }
}
