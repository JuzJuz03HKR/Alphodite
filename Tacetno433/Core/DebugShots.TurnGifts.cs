using System;
using System.Collections.Generic;
using Tacetno433.Battle;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //DebugShots.TurnGifts : DEVELOPER TOOL, the checks of the GIFTS (Beta1 31 Oct, docs/BETA1_DIRECTION.md 18), part of
    //--shots turncheck. The pool and who a gift is for (Data/Motif.cs), then every gift in a fight (Battle/TurnBattle.cs, search
    //the names), each with its name popping once (TurnEventKind.Gift). Like DebugShots.Turn.cs every line is PASS or FAIL with
    //what was expected and what came out. The musicians' traits are on here, the enemies have 999 life (Tough).
    //300 CONCERTOS and OPENING NIGHT are checked with the motifs whose places they took (DebugShots.TurnContent.cs, Turn.cs).
    public static partial class DebugShots
    {
        private static string TurnCheckGifts()
        {
            string report = "";
            NoteResult good = new NoteResult(Grade.Good, 1f);
            NoteResult miss = new NoteResult(Grade.Miss, 1f);
            Enemy hush = EnemyList.All[0];
            Enemy deadAir = EnemyList.All[1];
            Musician paganini = MusicianList.All[0];
            Musician quantz = MusicianList.All[1];
            Musician pfundt = MusicianList.All[2];
            Musician duport = MusicianList.All[6];
            Musician punto = MusicianList.All[7];
            MusicianList.ResetRehearsals();

            //Pool : three gifts for each legend and four for THE BAND, 19 in all, each legend has its own mark
            int[] perKey = new int[6];
            for (int i = 0; i < MotifList.Pool.Length; i++) perKey[(int)MotifList.Pool[i].Key]++;
            string pool = MotifList.Pool.Length + " " + perKey[1] + " " + perKey[2] + " " + perKey[3] + " " + perKey[4] + " " + perKey[5] + " " + perKey[0]
                          + " " + paganini.Key + " " + quantz.Key + " " + pfundt.Key + " " + duport.Key + " " + punto.Key;
            report += Same("gift pool", "19 3 3 3 3 3 4 Dissonance Heal OffBeat Shield Crescendo", pool);

            //Fits : a band of PAGANINI QUANTZ PFUNDT is never offered DUPORT's or PUNTO's (60 rolls of 3), an ELITE's (rare and up)
            //or a boss's (treasured) are of that rarity, and with no band only THE BAND's
            List<Musician> band = new List<Musician> { paganini, quantz, pfundt };
            List<Motif> none = new List<Motif>();
            Random dice = new Random(433);
            int wrong = 0;
            int rareOk = 0;
            for (int n = 0; n < 60; n++)
            {
                Motif[] offer = MotifList.Roll(none, band, 3, n % 2 == 0 ? 2 : 3, dice);
                for (int k = 0; k < offer.Length; k++)
                {
                    if (offer[k].Key == GiftKey.Shield || offer[k].Key == GiftKey.Crescendo) wrong++;
                    if (offer[k].Rarity >= (n % 2 == 0 ? 2 : 3)) rareOk++;
                }
            }
            Motif[] bandless = MotifList.Roll(none, null, 3, 1, dice);
            int bandlessWrong = 0;
            for (int k = 0; k < bandless.Length; k++) if (bandless[k].Key != GiftKey.Band) bandlessWrong++;
            report += Same("gift fits", "0 180 3 0 True", wrong + " " + rareOk + " " + bandless.Length + " " + bandlessWrong + " " + MotifList.AnyLeft(none, band));

            //Words : the legend a gift is for, on the card and the shelf, and the tooltip starts with it
            Motif pact = MotifList.Get(MotifId.ThePact);
            Motif lucky = MotifList.Get(MotifId.LuckyBow);
            report += Same("gift words", "PAGANINI / THE BAND / PAGANINI  /  RARE / True", pact.ForLabel + " / " + lucky.ForLabel + " / " + pact.WareLabel + " / " + pact.TipWrapped.StartsWith("FOR PAGANINI."));

            //LA CAMPANELLA : CAPRICE's four cuts at HUSH, the first adds 1, every later cut finds DISSONANCE and adds 1 more (7, not 4)
            TurnBattle b = GiftFight(Solo(0), new List<Enemy> { hush }, MotifId.LaCampanella);
            b.PlanSkill(b.Party[0], SkillList.First(paganini), b.Enemies[0]);
            ToTurnOf(b, b.Party[0]);
            b.UseSkill(new NoteResult[] { good, good, good, good });
            report += Same("la campanella", "7 1", b.Enemies[0].Dissonance + " " + CountGift(b, MotifId.LaCampanella));

            //THE PACT : PAGANINI's CAPRICE costs 4 life and plays x1.3 (5 x 2 x 0.4 x 1.3 = 5.2 -> 5 a cut, 20). It is PAGANINI's only,
            //QUANTZ would pay nothing.
            b = GiftFight(new List<Musician> { paganini, quantz }, new List<Enemy> { hush }, MotifId.ThePact);
            b.PlanSkill(b.Party[0], SkillList.First(paganini), b.Enemies[0]);
            ToTurnOf(b, b.Party[0]);
            int before = b.Enemies[0].Hp;
            int lifeBefore = b.Party[0].Hp;
            b.UseSkill(new NoteResult[] { good, good, good, good });
            report += Same("the pact", "20 4 True False 1", (before - b.Enemies[0].Hp) + " " + (lifeBefore - b.Party[0].Hp) + " " + b.Pacts(b.Party[0]) + " " + b.Pacts(b.Party[1]) + " " + CountGift(b, MotifId.ThePact));

            //SNAPPED STRINGS : HUSH at 7 DISSONANCE, CAPRICE's second cut brings it to 9 and it cracks : 27 more and back to 0, then
            //2 more cuts (4 x 4 + 27 = 43, DISSONANCE 2)
            b = GiftFight(Solo(0), new List<Enemy> { hush }, MotifId.SnappedStrings);
            b.PlanSkill(b.Party[0], SkillList.First(paganini), b.Enemies[0]);
            ToTurnOf(b, b.Party[0]);
            b.Enemies[0].Dissonance = 7;
            before = b.Enemies[0].Hp;
            b.UseSkill(new NoteResult[] { good, good, good, good });
            report += Same("snapped strings", "43 2 1", (before - b.Enemies[0].Hp) + " " + b.Enemies[0].Dissonance + " " + CountGift(b, MotifId.SnappedStrings));

            //ON PLAYING THE FLUTE : SOFT AIR heals 3 x 1.5 = 4.5 -> 4 a note (rounded to even, as every heal), 2 notes, PAGANINI 500 -> 508
            b = GiftFight(new List<Musician> { quantz, paganini }, new List<Enemy> { hush }, MotifId.ReedCase);
            b.PlanSkill(b.Party[0], SkillList.First(quantz), null);
            ToTurnOf(b, b.Party[0]);
            b.Party[1].Hp = 500;
            b.UseSkill(new NoteResult[] { good, good });
            report += Same("on playing the flute", "508 1", b.Party[1].Hp + " " + CountGift(b, MotifId.ReedCase));

            //THE KING'S FAVOUR : SOFT AIR fills PAGANINI (997 -> 999) and they stand GUARD, QUANTZ only gets to 506 and does not.
            //Read before the skill ends : the GUARD lasts until PAGANINI's next turn, which may come right after.
            b = GiftFight(new List<Musician> { quantz, paganini }, new List<Enemy> { hush }, MotifId.KingsFavour);
            b.PlanSkill(b.Party[0], SkillList.First(quantz), null);
            ToTurnOf(b, b.Party[0]);
            b.Party[0].Hp = 500;
            b.Party[1].Hp = 997;
            b.BeginSkill();
            b.SkillNote(0, good);
            b.SkillNote(1, good);
            string favour = b.Party[1].Hp + " " + b.Party[1].Guard + " " + b.Party[0].Hp + " " + b.Party[0].Guard;
            b.EndSkill();
            report += Same("the king's favour", "999 True 506 False 1", favour + " " + CountGift(b, MotifId.KingsFavour));

            //WOODEN STICKS : TIMPANI ROLL's 30 OFF-BEAT is 45
            b = GiftFight(Solo(2), new List<Enemy> { hush }, MotifId.SpareSticks);
            b.PlanSkill(b.Party[0], SkillList.First(pfundt), b.Enemies[0]);
            ToTurnOf(b, b.Party[0]);
            b.Enemies[0].OffBeat = 0;
            b.UseSkill(new NoteResult[] { good });
            report += Same("wooden sticks", "45 1", b.Enemies[0].OffBeat + " " + CountGift(b, MotifId.SpareSticks));

            //THE CRANK : TIMPANI ROLL takes 2 notes off HUSH's next attack (4 -> 2), and a phrase of 2 goes down to 1 (TURN OF THE HAND
            //alone : 4 -> 3, never below 2)
            b = GiftFight(Solo(2), new List<Enemy> { hush }, MotifId.TheCrank);
            b.PlanSkill(b.Party[0], SkillList.First(pfundt), b.Enemies[0]);
            ToTurnOf(b, b.Party[0]);
            LongPhrase(b.Enemies[0], 4);
            b.UseSkill(new NoteResult[] { good });
            string crank = b.Enemies[0].IntentNotes.Length + " " + CountGift(b, MotifId.TheCrank);
            b = GiftFight(Solo(2), new List<Enemy> { hush }, MotifId.TheCrank);
            b.PlanSkill(b.Party[0], SkillList.First(pfundt), b.Enemies[0]);
            ToTurnOf(b, b.Party[0]);
            LongPhrase(b.Enemies[0], 2);
            b.UseSkill(new NoteResult[] { good });
            report += Same("the crank", "2 1 / 1", crank + " / " + b.Enemies[0].IntentNotes.Length);

            //THUNDER SHEET : HUSH at 90 OFF-BEAT, TIMPANI ROLL (12) throws it once and it loses 10 percent of 999 (100) : 112. Thrown by a
            //PERFECT parry in its own attack (95 + 10), nothing goes until the attack is over, then the 100 does.
            b = GiftFight(Solo(2), new List<Enemy> { hush }, MotifId.ThunderSheet);
            b.PlanSkill(b.Party[0], SkillList.First(pfundt), b.Enemies[0]);
            ToTurnOf(b, b.Party[0]);
            b.Enemies[0].OffBeat = 90;
            before = b.Enemies[0].Hp;
            b.UseSkill(new NoteResult[] { good });
            string sheet = (before - b.Enemies[0].Hp) + " " + CountEvents(b, TurnEventKind.Break);
            b = GiftFight(Solo(2), new List<Enemy> { hush }, MotifId.ThunderSheet);
            PlayToEnemy(b);
            LongPhrase(b.Enemies[0], 2);
            b.Enemies[0].OffBeat = 95;
            before = b.Enemies[0].Hp;
            b.ParryNote(0, Grade.Perfect);
            sheet += " / " + (before - b.Enemies[0].Hp);
            b.ParryNote(1, Grade.Good);
            b.FinishEnemyTurn();
            report += Same("thunder sheet", "112 1 / 0 100", sheet + " " + (before - b.Enemies[0].Hp));

            //HARD CASE : HUSH's notes 8 8 2 1 on DUPORT, none parried : 6 6 1 1, 14 in all (999 -> 985)
            b = GiftFight(Solo(6), new List<Enemy> { hush }, MotifId.HardCase);
            PlayToEnemy(b);
            b.Current.IntentTarget = b.Party[0];
            b.Current.IntentNotes = new int[] { 8, 8, 2, 1 };
            before = b.Party[0].Hp;
            b.TakeEnemyAttack(null);
            report += Same("hard case", "14 1", (before - b.Party[0].Hp) + " " + CountGift(b, MotifId.HardCase));

            //TOURTE BOW : DUPORT's LANCE at HUSH reaches DEAD AIR too, both take 2 thrusts of 6 x 2 x 0.7 = 8.4 -> 8 (16 each)
            b = GiftFight(Solo(6), new List<Enemy> { hush, deadAir }, MotifId.TourteBow);
            b.PlanSkill(b.Party[0], SkillList.Second(duport), b.Enemies[0]);
            ToTurnOf(b, b.Party[0]);
            b.Party[0].Wounded = false;                                                     // LANCE : no blow to pay back
            b.Party[0].Crescendo = 0;
            int hushBefore = b.Enemies[0].Hp;
            int airBefore = b.Enemies[1].Hp;
            b.UseSkill(new NoteResult[] { good, good });
            report += Same("tourte bow", "16 16 1", (hushBefore - b.Enemies[0].Hp) + " " + (airBefore - b.Enemies[1].Hp) + " " + CountGift(b, MotifId.TourteBow));

            //THE EMPEROR'S SPUR : one note through to DUPORT, THE SCAR gives them 1 CRESCENDO and the whole band 1 as well
            b = GiftFight(new List<Musician> { duport, paganini, quantz }, new List<Enemy> { hush }, MotifId.EmperorsSpur);
            PlayToEnemy(b);
            for (int i = 0; i < b.Party.Count; i++) b.Party[i].Crescendo = 0;
            b.Current.IntentTarget = b.Party[0];
            b.Current.IntentNotes = new int[] { 5 };
            b.TakeEnemyAttack(null);
            report += Same("the emperor's spur", "1 1 1 1", b.Party[0].Crescendo + " " + b.Party[1].Crescendo + " " + b.Party[2].Crescendo + " " + CountGift(b, MotifId.EmperorsSpur));

            //HAND STOPPING : PAGANINI at 5 CRESCENDO plays CAPRICE MISS GOOD MISS GOOD : 5 -> 3 -> 2 (without it the first MISS takes all)
            b = GiftFight(Solo(0), new List<Enemy> { hush }, MotifId.HandStopping);
            b.PlanSkill(b.Party[0], SkillList.First(paganini), b.Enemies[0]);
            ToTurnOf(b, b.Party[0]);
            b.Party[0].Crescendo = 5;
            b.UseSkill(new NoteResult[] { miss, good, miss, good });
            report += Same("hand stopping", "2 1", b.Party[0].Crescendo + " " + CountGift(b, MotifId.HandStopping));

            //THE SILVER HORN : FANFARE takes PAGANINI from 7 to 8 (the most now) and PUNTO from 0 to 2. Without it PAGANINI stops at 5.
            b = GiftFight(new List<Musician> { punto, paganini }, new List<Enemy> { hush }, MotifId.SilverHorn);
            b.PlanSkill(b.Party[0], SkillList.First(punto), null);
            ToTurnOf(b, b.Party[0]);
            b.Party[0].Crescendo = 0;
            b.Party[1].Crescendo = 7;
            b.UseSkill(new NoteResult[] { good, good });
            string horn = b.Party[1].Crescendo + " " + b.Party[0].Crescendo + " " + CountGift(b, MotifId.SilverHorn) + " " + b.CrescendoMost;
            b = new TurnBattle(new List<Musician> { punto, paganini }, new List<Enemy> { hush }, 1, new Random(433));
            Tough(b);
            b.Breath = 10;
            b.PlanSkill(b.Party[0], SkillList.First(punto), null);
            ToTurnOf(b, b.Party[0]);
            b.Party[1].Crescendo = 4;
            b.UseSkill(new NoteResult[] { good, good });
            report += Same("the silver horn", "8 2 1 8 / 5", horn + " / " + b.Party[1].Crescendo);

            //HALLALI : PAGANINI at full CRESCENDO (5) ATTACKs : 5 x 2 x 0.55 x 2 x 1.75 = 19.25 -> 19, and the CRESCENDO is gone
            b = GiftFight(Solo(0), new List<Enemy> { hush }, MotifId.Hallali);
            ToTurnOf(b, b.Party[0]);
            b.Party[0].Crescendo = 5;
            int burst = b.Attack();
            report += Same("hallali", "19 0 1", burst + " " + b.Party[0].Crescendo + " " + CountGift(b, MotifId.Hallali));

            //SPARE STRINGS : CAPRICE all MISSed, the first counts as GOOD (4 and 1 DISSONANCE), the rest are MISSes (a MISS of a skill
            //does nothing). The next MISS of the fight is a MISS.
            b = GiftFight(Solo(0), new List<Enemy> { hush }, MotifId.SpareStrings);
            b.PlanSkill(b.Party[0], SkillList.First(paganini), b.Enemies[0]);
            ToTurnOf(b, b.Party[0]);
            before = b.Enemies[0].Hp;
            b.UseSkill(new NoteResult[] { miss, miss, miss, miss });
            string spare = (before - b.Enemies[0].Hp) + " " + b.Enemies[0].Dissonance + " " + b.Forgive(Grade.Miss);
            report += Same("spare strings", "4 1 Miss 1", spare + " " + CountGift(b, MotifId.SpareStrings));

            //LUCKY BOW : CAPRICE's first cut is twice as strong (8, then 4 4 4 : 20)
            b = GiftFight(Solo(0), new List<Enemy> { hush }, MotifId.LuckyBow);
            b.PlanSkill(b.Party[0], SkillList.First(paganini), b.Enemies[0]);
            ToTurnOf(b, b.Party[0]);
            before = b.Enemies[0].Hp;
            b.UseSkill(new NoteResult[] { good, good, good, good });
            report += Same("lucky bow", "20 1", (before - b.Enemies[0].Hp) + " " + CountGift(b, MotifId.LuckyBow));

            //OPENING NIGHT : the band's first hit of the fight is x2 and its name pops once
            b = GiftFight(Solo(0), new List<Enemy> { hush }, MotifId.Overture);
            ToTurnOf(b, b.Party[0]);
            report += Same("opening night", "11 1", b.Attack() + " " + CountGift(b, MotifId.Overture));

            MusicianList.ResetRehearsals();
            return report;
        }

        //Gift Fight : a fight on floor 1 with this one gift, the enemies with 999 life and the band too (Tough), BREATH full
        private static TurnBattle GiftFight(List<Musician> band, List<Enemy> enemies, MotifId gift)
        {
            TurnBattle b = new TurnBattle(band, enemies, 1, new Random(433));
            Tough(b);
            b.GiveMotifs(new List<Motif> { MotifList.Get(gift) });
            b.Breath = 10;
            return b;
        }

        //To Turn Of : the bar is played (others' moves GOOD, TACET's notes parried GOOD) until it is this musician's move
        private static void ToTurnOf(TurnBattle b, Fighter who)
        {
            for (int i = 0; i < 40 && !(b.Step == TurnStep.PlayerTurn && b.Current == who) && b.Step != TurnStep.Won && b.Step != TurnStep.Lost; i++)
                Step(b, Grade.Good);
        }

        //Count Gift : how many times this gift's name was written down to pop
        private static int CountGift(TurnBattle b, MotifId id)
        {
            int count = 0;
            for (int i = 0; i < b.Events.Count; i++)
                if (b.Events[i].Kind == TurnEventKind.Gift && b.Events[i].Amount == (int)id) count++;
            return count;
        }

        //Play Bar Perfect : the bar is played to its end, every note of TACET parried PERFECT (nobody in the band gets hurt)
        private static void PlayBarPerfect(TurnBattle b)
        {
            int bar = b.Round;
            for (int i = 0; i < 30 && b.Round == bar && b.Step != TurnStep.Won && b.Step != TurnStep.Lost; i++)
                Step(b, Grade.Perfect);
        }
    }
}
