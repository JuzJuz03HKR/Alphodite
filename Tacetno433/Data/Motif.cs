using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Core;

namespace Tacetno433.Data
{
    //MotifId : one name per motif. The battle rules check for these by name.
    //KEEP THIS ORDER IN STEP WITH MotifList.All below.
    //GIFTS (Beta1 31 Oct) : on screen a motif is a GIFT now, the code keeps the word motif (like BREATH and Stamina).
    //Five of these places hold a gift with a new name (REED CASE is ON PLAYING THE FLUTE, GREEN ROOM is 300 CONCERTOS ...),
    //the gifts that are new come at the END, and the motifs no gift uses stay for old saves only (MotifList.Pool).
    public enum MotifId
    {
        Resin,
        ReedCase,           // GIFT : ON PLAYING THE FLUTE (QUANTZ)
        SpareSticks,        // GIFT : WOODEN STICKS (PFUNDT)
        Pianissimo,
        PatronsPurse,
        BreathMark,         // was Fermata, renamed so it is not mixed up with TACET's held note
        Tutti,
        SteadyPulse,
        Encore,
        Rubato,
        Crescendo,
        Sforzando,
        SecondWind,         // GIFT : FRONT ROW ROSES (THE BAND)
        Overture,           // GIFT : OPENING NIGHT (THE BAND)
        Counterpoint,

        //Special Notes (25 Sep) : one for each of the special notes and moments of the duel.
        //Added at the END, because the save file keeps motifs by their place in this list.
        Accelerando,
        Tenuto,
        GraceNote,
        Marcato,
        ConBrio,
        Coda,

        //ONE BAR (Alpha 7 step 13) : motifs about the order of the bar and packs of enemies (and the BENCH until it went away,
        //ENTRANCE and GREEN ROOM do something else since 29 Oct). Added at the END.
        Downbeat,
        Entrance,
        GreenRoom,          // GIFT : 300 CONCERTOS (QUANTZ)
        Unison,
        Segue,
        LastWord,

        //GIFTS (Beta1 31 Oct) : the new gifts, three for each legend and two for the band. Added at the END.
        LaCampanella,       // PAGANINI
        ThePact,
        SnappedStrings,
        KingsFavour,        // QUANTZ
        TheCrank,           // PFUNDT
        ThunderSheet,
        HardCase,           // DUPORT
        TourteBow,
        EmperorsSpur,
        HandStopping,       // PUNTO
        SilverHorn,
        Hallali,
        SpareStrings,       // THE BAND
        LuckyBow
    }

    //GiftKey : GIFTS (Beta1 31 Oct, docs/BETA1_DIRECTION.md 16.14 point 2 and 18), the mark of the legend a gift is for, like the
    //keywords of Limbus Company's E.G.O Gifts. Each legend has one (Musician.Key, what they are best at) and shows its icon at the
    //audition, every gift shows the icon of the legend it fits, so the same icon means "this one is for them". Band : for everyone.
    //   Dissonance  PAGANINI     Heal       QUANTZ      OffBeat  PFUNDT
    //   Shield      DUPORT       Crescendo  PUNTO
    public enum GiftKey { Band, Dissonance, Heal, OffBeat, Shield, Crescendo }

    //Motif : a small thing the band carries for the rest of the run, which changes how
    //fights play out. The same idea as E.G.O Gifts or Blessings in other games.
    //Until 31 Oct every motif was named after a mark written on sheet music. GIFTS (31 Oct) : the gifts the listeners give
    //and the things in a legend's bag, named after real things from their lives, one short line each.
    public class Motif
    {
        public MotifId Id;
        public string Name = "";
        public string Mark = "";          // the notation mark it is named after, like "sfz" (round 15 : no longer drawn)
        public string Initials = "";      // two letters on its badge, "SF" (round 15, the player found the notation marks hard to read)
        public int Rarity = 1;            // 1 common, 2 rare, 3 treasured
        public int FromFloor = 1;         // never offered before this floor (its note is not in play yet). The turn battle's skills
                                          // have every kind of note from the first fight, so there it is always 1 (step 15)
        public string Text = "";
        public string TurnText = "";      // what it does in the turn battle (Alpha 7), shown while BattleRules.TurnBattleOn
        public GiftKey Key = GiftKey.Band;// GIFTS : the legend it is for, Band for everyone

        //Prepared Text
        public string TextWrapped = "";   // for cards
        public string TipWrapped = "";    // for tooltips, a little wider
        public string RarityLabel = "";
        public string ForLabel = "";      // GIFTS : "PAGANINI" or "THE BAND", under its icon
        public string WareLabel = "";     // GIFTS : "PAGANINI  /  RARE" on the shop's shelf
    }

    //MotifList : THE PLACE TO EDIT MOTIFS (GIFTS on screen).
    //The numbers each motif changes live in BattleRules, under Motif Effects and GIFTS.
    public static class MotifList
    {
        public static Motif[] All = new Motif[]
        {
            //Rarity 1
            new Motif { Id = MotifId.Resin,        Name = "ROSIN",         Mark = "arco", Rarity = 1,
                        Text = "String players hit 1 harder.",
                        TurnText = "String players hit 1 harder." },
            //ON PLAYING THE FLUTE (GIFT, was REED CASE) : Quantz's book on playing the flute (Berlin, 1752). The same rule as REED CASE,
            //the winds heal more, and QUANTZ is the only wind of the five who heals.
            new Motif { Id = MotifId.ReedCase,     Name = "ON PLAYING THE FLUTE", Mark = "o", Rarity = 1, Key = GiftKey.Heal, Initials = "FL",
                        Text = "With a wind player on stage, every rest gives back 4 more breath.",
                        TurnText = "QUANTZ heals " + Percent(BattleRules.TurnReedCaseHeal) + " percent more." },
            //WOODEN STICKS (GIFT, was SPARE STICKS) : the hardest timpani sticks, the loudest blow. The same rule, PFUNDT is the only drummer.
            new Motif { Id = MotifId.SpareSticks,  Name = "WOODEN STICKS", Mark = "x",    Rarity = 1, Key = GiftKey.OffBeat,
                        Text = "Percussion players hit 30 percent harder on TACET's f notes.",
                        TurnText = "PFUNDT's skills fill OFF-BEAT " + Percent(BattleRules.TurnSpareSticksOffBeat) + " percent more." },
            new Motif { Id = MotifId.Pianissimo,   Name = "PIANISSIMO",    Mark = "pp",   Rarity = 1,
                        Text = "A small stroke on a p note gives back 3 breath.",
                        TurnText = "The first note of an attack parried PERFECT gives 1 more BREATH." },
            new Motif { Id = MotifId.PatronsPurse, Name = "PATRON'S PURSE", Mark = "$",   Rarity = 1,
                        Text = "Won fights pay 30 percent more shards.",
                        TurnText = "Won fights pay 30 percent more shards." },

            //Rarity 2
            new Motif { Id = MotifId.BreathMark,   Name = "BREATH MARK",   Mark = ",",    Rarity = 2,
                        Text = "A rest gives back 15 breath instead of 12.",
                        TurnText = "Every fight starts with 2 more BREATH." },
            new Motif { Id = MotifId.Tutti,        Name = "TUTTI",         Mark = "tutti", Rarity = 2,
                        Text = "Playing together is stronger. Each extra player adds 6 percent more.",
                        TurnText = "Skills that reach everyone are 25 percent stronger." },
            new Motif { Id = MotifId.SteadyPulse,  Name = "STEADY PULSE",  Mark = "=",    Rarity = 2,
                        Text = "The PERFECT and GOOD timing windows are wider.",
                        TurnText = "The PERFECT and GOOD timing windows are wider." },
            new Motif { Id = MotifId.Encore,       Name = "ENCORE",        Mark = "bis",  Rarity = 2,
                        Text = "After a win, 15 percent more breath comes back.",
                        TurnText = "After a win, 15 percent more life comes back." },
            new Motif { Id = MotifId.Rubato,       Name = "RUBATO",        Mark = "rit.", Rarity = 2,
                        Text = "A MISS costs no extra breath and loses less power.",
                        TurnText = "A skill note you MISS still plays, at 60 percent." },

            //Rarity 3
            new Motif { Id = MotifId.Crescendo,    Name = "CRESCENDO",     Mark = "<",    Rarity = 3,
                        Text = "The COMBO bonus grows twice as fast.",
                        TurnText = "Every fight starts with 1 CRESCENDO on each musician." },
            new Motif { Id = MotifId.Sforzando,    Name = "SFORZANDO",     Mark = "sfz",  Rarity = 3,
                        Text = "A big stroke on an f note hits 20 percent harder.",
                        TurnText = "The last note of a skill, PERFECT, hits 30 percent harder." },
            //FRONT ROW ROSES (GIFT, was SECOND WIND) : the flowers from the front row keep a musician on their feet. The same rule.
            new Motif { Id = MotifId.SecondWind,   Name = "FRONT ROW ROSES", Mark = "V",  Rarity = 3,
                        Text = "Once per fight, when the band would collapse, 15 percent of its breath comes back.",
                        TurnText = "Once a fight, a musician who would fall stays up with 1 life." },
            //OPENING NIGHT (GIFT, was OVERTURE) : the first night of a concert, the first blow lands hardest. The same rule.
            new Motif { Id = MotifId.Overture,     Name = "OPENING NIGHT", Mark = "I",    Rarity = 1,
                        Text = "The first beat of every round hits 50 percent harder.",
                        TurnText = "The band's first hit of every fight is twice as hard." },
            new Motif { Id = MotifId.Counterpoint, Name = "COUNTERPOINT",  Mark = "+",    Rarity = 3, Initials = "CP",
                        Text = "Against TACET's f notes, your answer hits 20 percent harder.",
                        TurnText = "A COUNTER hits twice as hard." },

            //Special Notes : each bends one of the four notes or one big moment of the duel.
            //FromFloor keeps a motif away until its note has arrived (BattleRules teaching order).
            new Motif { Id = MotifId.Accelerando,  Name = "ACCELERANDO",   Mark = "accel.", Rarity = 1, FromFloor = BattleRules.TurnBattleOn ? 1 : BattleRules.TremoloFromFloor,
                        Text = "Every shake in a TREMOLO counts twice.",
                        TurnText = "Every shake in a roll counts twice." },
            new Motif { Id = MotifId.Tenuto,       Name = "TENUTO",        Mark = "-",    Rarity = 1, FromFloor = BattleRules.TurnBattleOn ? 1 : BattleRules.FermataFromFloor,
                        Text = "A FERMATA held to the end gives back 8 breath.",
                        TurnText = "A held note always counts as held to the end." },
            new Motif { Id = MotifId.GraceNote,    Name = "GRACE NOTE",    Mark = "gr.",  Rarity = 2, FromFloor = BattleRules.TurnBattleOn ? 1 : BattleRules.PairsFromFloor,
                        Text = "A spark that lands on time hits three times as hard.",
                        TurnText = "A spark that lands does its skill twice." },
            new Motif { Id = MotifId.Marcato,      Name = "MARCATO",       Mark = "^",    Rarity = 2,
                        Text = "A COUNTER can push the line up to 16 in one beat, not just 8.",
                        TurnText = "An ATTACK fills OFF-BEAT twice as much." },
            new Motif { Id = MotifId.ConBrio,      Name = "CON BRIO",      Mark = "brio", Rarity = 3,
                        Text = "FORTISSIMO lights after 6 PERFECTs in a row instead of 8.",
                        TurnText = "The SIGNATURE needs 4 PERFECTs instead of 6." },
            new Motif { Id = MotifId.Coda,         Name = "CODA",          Mark = "coda", Rarity = 3,
                        Text = "The FINALE is offered from +60 on the line instead of +80.",
                        TurnText = "Enemies under a quarter of their life take 50 percent more." },

            //ONE BAR (step 13) : each one is about the order of the bar or a pack of enemies, so carrying it changes how a
            //bar is written. They only work in the turn battle. THE BAND OF THREE (29 Oct) : CALL and the BENCH are gone, so
            //ENTRANCE (was : a CALLED musician twice as strong in their first bar) and GREEN ROOM (was : the BENCH heals) changed.
            new Motif { Id = MotifId.Downbeat,     Name = "DOWNBEAT",      Mark = "1",    Rarity = 1,
                        Text = "The first move of every bar is 50 percent stronger.",
                        TurnText = "The first move of every bar is 50 percent stronger." },
            new Motif { Id = MotifId.Entrance,     Name = "ENTRANCE",      Mark = "ent.", Rarity = 1, Initials = "ET",
                        Text = "Each musician's first move of a fight is twice as strong.",
                        TurnText = "Each musician's first move of a fight is twice as strong." },
            //300 CONCERTOS (GIFT 31 Oct, was GREEN ROOM : the most hurt musician got 3 back after every bar) : Quantz wrote about 300
            //flute concertos for the king's evening concerts. While QUANTZ stands, everyone gets life back after every bar.
            new Motif { Id = MotifId.GreenRoom,    Name = "300 CONCERTOS", Mark = "gr",   Rarity = 3, Key = GiftKey.Heal, Initials = "300",
                        Text = "After every bar, the most hurt musician gets back 3 life.",
                        TurnText = "While QUANTZ stands, every musician heals " + BattleRules.TurnGreenRoomHeal + " after every bar." },
            new Motif { Id = MotifId.Unison,       Name = "UNISON",        Mark = "unis.", Rarity = 2,
                        Text = "When every move of a bar is an ATTACK at one enemy, the last one hits twice as hard.",
                        TurnText = "When every move of a bar is an ATTACK at one enemy, the last one hits twice as hard." },
            new Motif { Id = MotifId.Segue,        Name = "SEGUE",         Mark = "segue", Rarity = 2,
                        Text = "Damage left over when an enemy falls silent goes on to the next one.",
                        TurnText = "Damage left over when an enemy falls silent goes on to the next one." },
            new Motif { Id = MotifId.LastWord,     Name = "LAST WORD",     Mark = "fine", Rarity = 2,
                        Text = "The last musician to play in a bar gets GUARD.",
                        TurnText = "The last musician to play in a bar gets GUARD." },

            //GIFTS (Beta1 31 Oct) : three for each legend and two for the band, the rules are in TurnBattle (search the names).
            //PAGANINI (DISSONANCE) : rumour said the devil taught those hands, DISSONANCE is the devil's mark in this game.
            //   LA CAMPANELLA   "the little bell", the last movement of their second violin concerto : every blow rings the crack again
            //   THE PACT        the deal with the devil the rumour told of : blood for every skill, and the skill plays harder
            //   SNAPPED STRINGS the story that their strings broke one by one and they played on : a full crack bursts
            new Motif { Id = MotifId.LaCampanella, Name = "LA CAMPANELLA", Rarity = 1, Key = GiftKey.Dissonance,
                        Text = "Every hit on an enemy with DISSONANCE adds " + BattleRules.TurnCampanellaAdd + " more DISSONANCE." },
            new Motif { Id = MotifId.ThePact,      Name = "THE PACT",      Rarity = 2, Key = GiftKey.Dissonance,
                        Text = "PAGANINI's skills cost " + BattleRules.TurnPactLife + " life and hit " + Percent(BattleRules.TurnBloodScale) + " percent harder." },
            new Motif { Id = MotifId.SnappedStrings, Name = "SNAPPED STRINGS", Rarity = 3, Key = GiftKey.Dissonance, Initials = "SN",
                        Text = "At " + BattleRules.TurnDissonanceMost + " DISSONANCE an enemy cracks : " + BattleRules.TurnSnapDamage + " damage, then its DISSONANCE is gone." },

            //QUANTZ (HEAL) : the teacher of Frederick the Great, the only healer of the five.
            //   THE KING'S FAVOUR  the king's protection : healed to full is safe for a while
            new Motif { Id = MotifId.KingsFavour,  Name = "THE KING'S FAVOUR", Rarity = 2, Key = GiftKey.Heal,
                        Text = "A musician healed to full gets GUARD until their next turn." },

            //PFUNDT (OFF-BEAT) : the timpanist of the Leipzig Gewandhaus, credited with the crank timpani.
            //   THE CRANK      the crank that retunes the drum in one turn : TURN OF THE HAND takes more notes, down to one
            //   THUNDER SHEET  the theatre's sheet of metal shaken for thunder : an enemy thrown off the beat loses a share of its life
            new Motif { Id = MotifId.TheCrank,     Name = "THE CRANK",     Rarity = 2, Key = GiftKey.OffBeat, Initials = "CR",
                        Text = "PFUNDT's skills take " + BattleRules.TurnHandPaukenCut + " notes off an enemy's next attack, down to " + BattleRules.TurnHandOneLeast + "." },
            new Motif { Id = MotifId.ThunderSheet, Name = "THUNDER SHEET", Rarity = 3, Key = GiftKey.OffBeat,
                        Text = "An enemy thrown OFF-BEAT loses " + (int)Math.Round(BattleRules.TurnThunderSheetShare * 100f) + " percent of its life at once." },

            //DUPORT (SHIELD) : the guardian, the cello a shield with the dent Napoleon left (the story says his spurs did it, 1812).
            //   HARD CASE           a cello case : DUPORT takes a little less from every note
            //   TOURTE BOW          the bow of Francois Tourte, the maker of the modern bow : the LANCE (DUPORT's bow) sweeps
            //   THE EMPEROR'S SPUR  the scar it left : THE SCAR makes the whole band louder
            new Motif { Id = MotifId.HardCase,     Name = "HARD CASE",     Rarity = 1, Key = GiftKey.Shield,
                        Text = "DUPORT takes " + BattleRules.TurnHardCase + " less from every enemy note, 1 at least." },
            new Motif { Id = MotifId.TourteBow,    Name = "TOURTE BOW",    Rarity = 2, Key = GiftKey.Shield,
                        Text = "DUPORT's LANCE hits every enemy." },
            new Motif { Id = MotifId.EmperorsSpur, Name = "THE EMPEROR'S SPUR", Rarity = 3, Key = GiftKey.Shield,
                        Text = "When THE SCAR gives DUPORT CRESCENDO, the whole band gets it too." },

            //PUNTO (CRESCENDO) : the horn player Beethoven wrote a sonata for.
            //   HAND STOPPING    the hand in the bell that Punto was famous for, it saves a note the horn would lose
            //   THE SILVER HORN  the silver horn Raoux of Paris made for them : louder than any other
            //   HALLALI          the call of the hunting horn when the hunt ends : the loudest blow, then the horn is quiet again
            new Motif { Id = MotifId.HandStopping, Name = "HAND STOPPING", Rarity = 1, Key = GiftKey.Crescendo,
                        Text = "A MISS takes only half of a musician's CRESCENDO." },
            new Motif { Id = MotifId.SilverHorn,   Name = "THE SILVER HORN", Rarity = 2, Key = GiftKey.Crescendo,
                        Text = "CRESCENDO goes up to " + BattleRules.TurnSilverHornMost + ", not " + BattleRules.TurnCrescendoMost + "." },
            new Motif { Id = MotifId.Hallali,      Name = "HALLALI",       Rarity = 3, Key = GiftKey.Crescendo,
                        Text = "At full CRESCENDO a musician's next hit is twice as hard, then their CRESCENDO is gone." },

            //THE BAND : for whoever plays, they change the ring (docs/BETA1_DIRECTION.md 16.4 point 8)
            //   SPARE STRINGS  a string snaps, a spare goes on : the first MISS of a fight is let off
            //   LUCKY BOW      the first note of every skill is the lucky one
            new Motif { Id = MotifId.SpareStrings, Name = "SPARE STRINGS", Rarity = 1, Initials = "SP",
                        Text = "The first MISS of every fight counts as GOOD." },
            new Motif { Id = MotifId.LuckyBow,     Name = "LUCKY BOW",     Rarity = 2,
                        Text = "The first note of every skill is twice as strong." },
        };

        //Pool : GIFTS (Beta1 31 Oct), the gifts a run can find, three for each legend and four for the band. Fewer than the 27
        //motifs and each one stronger, so every gift is felt (the user : fewer but strong). The motifs not here stay in All for old
        //saves, their rules still work. A run only meets the gifts of its own band (Fits).
        public static Motif[] Pool = new Motif[]
        {
            Get(MotifId.LaCampanella), Get(MotifId.ThePact), Get(MotifId.SnappedStrings),           // PAGANINI
            Get(MotifId.ReedCase), Get(MotifId.KingsFavour), Get(MotifId.GreenRoom),                 // QUANTZ
            Get(MotifId.SpareSticks), Get(MotifId.TheCrank), Get(MotifId.ThunderSheet),              // PFUNDT
            Get(MotifId.HardCase), Get(MotifId.TourteBow), Get(MotifId.EmperorsSpur),                // DUPORT
            Get(MotifId.HandStopping), Get(MotifId.SilverHorn), Get(MotifId.Hallali),                 // PUNTO
            Get(MotifId.Overture), Get(MotifId.SpareStrings), Get(MotifId.LuckyBow), Get(MotifId.SecondWind),   // THE BAND
        };

        private static string[] rarityNames = { "", "COMMON", "RARE", "TREASURED" };

        //Percent : "30" for 1.3, a gift's line says how much more in words a player reads at once
        private static string Percent(float scale)
        {
            return ((int)Math.Round((scale - 1f) * 100f)).ToString();
        }

        //Text Prepare : called once from LoadContent
        public static void PrepareText(SpriteFont storyFont, float cardWidth, float tipWidth)
        {
            for (int i = 0; i < All.Length; i++)
            {
                Motif m = All[i];
                if (m.TurnText.Length == 0) m.TurnText = m.Text;                               // GIFTS : one line for both
                string text = BattleRules.TurnBattleOn && m.TurnText.Length > 0 ? m.TurnText : m.Text;
                Musician legend = LegendOf(m.Key);
                m.ForLabel = legend != null ? legend.Name : "THE BAND";
                m.RarityLabel = rarityNames[m.Rarity];
                m.WareLabel = m.ForLabel + "  /  " + m.RarityLabel;
                m.TextWrapped = Gfx.WrapText(storyFont, text, cardWidth, TextSize.StorySmall, 4);
                m.TipWrapped = Gfx.WrapText(storyFont, "FOR " + m.ForLabel + ".  " + text, tipWidth, TextSize.StorySmall);

                //Initials : the first letters of the first two words, or the first two letters of one word ("THE" left out).
                //A motif can set its own when two would come out the same (COUNTERPOINT "CP", not CODA's "CO").
                if (m.Initials != "") continue;
                string[] words = m.Name.Replace("THE ", "").Split(' ');
                m.Initials = words.Length >= 2 ? "" + words[0][0] + words[1][0] : words[0].Substring(0, System.Math.Min(2, words[0].Length));
            }
        }

        public static Motif Get(MotifId id)
        {
            return All[(int)id];
        }

        //Legend Of : the legend of the five whose mark this is (Musician.Key), null for THE BAND
        public static Musician LegendOf(GiftKey key)
        {
            if (key == GiftKey.Band) return null;
            for (int i = 0; i < MusicianList.Shown.Length; i++)
                if (MusicianList.Shown[i].Key == key) return MusicianList.Shown[i];
            return null;
        }

        //Fits : GIFTS, a gift is for this band when it is THE BAND's or one of its legends has its mark. Without a band (the run has
        //not picked one yet) only THE BAND's fit.
        public static bool Fits(Motif m, List<Musician> band)
        {
            if (m.Key == GiftKey.Band) return true;
            if (band == null) return false;
            for (int i = 0; i < band.Count; i++)
                if (band[i].Key == m.Key) return true;
            return false;
        }

        //Any Left : a gift of the Pool fits this band and is not carried yet
        public static bool AnyLeft(List<Motif> owned, List<Musician> band)
        {
            for (int i = 0; i < Pool.Length; i++)
                if (Fits(Pool[i], band) && !owned.Contains(Pool[i])) return true;
            return false;
        }

        //Gift Roll : GIFTS (Beta1 31 Oct), up to count different gifts of the Pool that fit the band and it does not carry yet.
        //minRarity lets elites and bosses skip the common ones. When too few are left, any rarity that fits.
        public static Motif[] Roll(List<Motif> owned, List<Musician> band, int count, int minRarity, Random random)
        {
            List<Motif> pool = new List<Motif>();
            for (int i = 0; i < Pool.Length; i++)
                if (Pool[i].Rarity >= minRarity && Fits(Pool[i], band) && !owned.Contains(Pool[i])) pool.Add(Pool[i]);

            //Fallback : nothing rare enough is left, take any that fits and the band does not own
            if (pool.Count < count)
                for (int i = 0; i < Pool.Length; i++)
                    if (Fits(Pool[i], band) && !owned.Contains(Pool[i]) && !pool.Contains(Pool[i])) pool.Add(Pool[i]);

            return Pick(pool, count, random);
        }

        //Motif Roll (the old rhythm duel and its simulator) : up to count different motifs of All the band does not own yet.
        //minRarity lets elites and bosses skip the common ones. floor is where the run is now,
        //so a motif for a note that has not arrived yet is never offered.
        public static Motif[] Roll(List<Motif> owned, int count, int minRarity, int floor, Random random)
        {
            List<Motif> pool = new List<Motif>();
            for (int i = 0; i < All.Length; i++)
                if (All[i].Rarity >= minRarity && All[i].FromFloor <= floor && !owned.Contains(All[i]))
                    pool.Add(All[i]);

            //Fallback : nothing rare enough is left, take any the band does not own
            if (pool.Count < count)
                for (int i = 0; i < All.Length; i++)
                    if (All[i].FromFloor <= floor && !owned.Contains(All[i]) && !pool.Contains(All[i]))
                        pool.Add(All[i]);

            return Pick(pool, count, random);
        }

        //Pick : count of the pool at random, each once
        private static Motif[] Pick(List<Motif> pool, int count, Random random)
        {
            if (count > pool.Count) count = pool.Count;
            Motif[] result = new Motif[count];

            for (int i = 0; i < count; i++)
            {
                int pick = random.Next(pool.Count);
                result[i] = pool[pick];
                pool.RemoveAt(pick);
            }
            return result;
        }

        //Price : what the shop asks for a motif
        public static int Price(Motif m)
        {
            return BattleRules.MotifPrice[m.Rarity];
        }
    }
}
