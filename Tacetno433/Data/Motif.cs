using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Core;

namespace Tacetno433.Data
{
    //MotifId : one name per motif. The battle rules check for these by name.
    //KEEP THIS ORDER IN STEP WITH MotifList.All below.
    public enum MotifId
    {
        Resin,
        ReedCase,
        SpareSticks,
        Pianissimo,
        PatronsPurse,
        BreathMark,         // was Fermata, renamed so it is not mixed up with TACET's held note
        Tutti,
        SteadyPulse,
        Encore,
        Rubato,
        Crescendo,
        Sforzando,
        SecondWind,
        Overture,
        Counterpoint,

        //Special Notes (25 Sep) : one for each of the special notes and moments of the duel.
        //Added at the END, because the save file keeps motifs by their place in this list.
        Accelerando,
        Tenuto,
        GraceNote,
        Marcato,
        ConBrio,
        Coda
    }

    //Motif : a small thing the band carries for the rest of the run, which changes how
    //fights play out. The same idea as E.G.O Gifts or Blessings in other games.
    //Every motif is named after a mark written on sheet music.
    public class Motif
    {
        public MotifId Id;
        public string Name = "";
        public string Mark = "";          // the notation mark it is named after, like "sfz" (round 15 : no longer drawn)
        public string Initials = "";      // two letters on its badge, "SF" (round 15, the player found the notation marks hard to read)
        public int Rarity = 1;            // 1 common, 2 rare, 3 treasured
        public int FromFloor = 1;         // never offered before this floor (its note is not in play yet)
        public string Text = "";
        public string TurnText = "";      // what it does in the turn battle (Alpha 7), shown while BattleRules.TurnBattleOn

        //Prepared Text
        public string TextWrapped = "";   // for cards
        public string TipWrapped = "";    // for tooltips, a little wider
        public string RarityLabel = "";
    }

    //MotifList : THE PLACE TO EDIT MOTIFS.
    //The numbers each motif changes live in BattleRules, under Motif Effects.
    public static class MotifList
    {
        public static Motif[] All = new Motif[]
        {
            //Rarity 1
            new Motif { Id = MotifId.Resin,        Name = "ROSIN",         Mark = "arco", Rarity = 1,
                        Text = "String players hit 1 harder.",
                        TurnText = "String players hit 1 harder." },
            new Motif { Id = MotifId.ReedCase,     Name = "REED CASE",     Mark = "o",    Rarity = 1,
                        Text = "With a wind player on stage, every rest gives back 4 more breath.",
                        TurnText = "Wind players heal 50 percent more." },
            new Motif { Id = MotifId.SpareSticks,  Name = "SPARE STICKS",  Mark = "x",    Rarity = 1,
                        Text = "Percussion players hit 30 percent harder on TACET's f notes.",
                        TurnText = "Percussion skills fill OFF-BEAT 50 percent faster." },
            new Motif { Id = MotifId.Pianissimo,   Name = "PIANISSIMO",    Mark = "pp",   Rarity = 1,
                        Text = "A small stroke on a p note gives back 3 breath.",
                        TurnText = "A p note parried PERFECT gives 1 more BREATH." },
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
                        TurnText = "A PERFECT f note hits 30 percent harder." },
            new Motif { Id = MotifId.SecondWind,   Name = "SECOND WIND",   Mark = "V",    Rarity = 3,
                        Text = "Once per fight, when the band would collapse, 15 percent of its breath comes back.",
                        TurnText = "Once per fight, a musician who would fall stays up with 1 life." },
            new Motif { Id = MotifId.Overture,     Name = "OVERTURE",      Mark = "I",    Rarity = 3,
                        Text = "The first beat of every round hits 50 percent harder.",
                        TurnText = "The first hit of every fight is twice as hard." },
            new Motif { Id = MotifId.Counterpoint, Name = "COUNTERPOINT",  Mark = "+",    Rarity = 3, Initials = "CP",
                        Text = "Against TACET's f notes, your answer hits 20 percent harder.",
                        TurnText = "A COUNTER hits twice as hard." },

            //Special Notes : each bends one of the four notes or one big moment of the duel.
            //FromFloor keeps a motif away until its note has arrived (BattleRules teaching order).
            new Motif { Id = MotifId.Accelerando,  Name = "ACCELERANDO",   Mark = "accel.", Rarity = 1, FromFloor = BattleRules.TremoloFromFloor,
                        Text = "Every shake in a TREMOLO counts twice.",
                        TurnText = "Every shake in a roll counts twice." },
            new Motif { Id = MotifId.Tenuto,       Name = "TENUTO",        Mark = "-",    Rarity = 1, FromFloor = BattleRules.FermataFromFloor,
                        Text = "A FERMATA held to the end gives back 8 breath.",
                        TurnText = "A held note always counts as held to the end." },
            new Motif { Id = MotifId.GraceNote,    Name = "GRACE NOTE",    Mark = "gr.",  Rarity = 2, FromFloor = BattleRules.PairsFromFloor,
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
        };

        private static string[] rarityNames = { "", "COMMON", "RARE", "TREASURED" };

        //Text Prepare : called once from LoadContent
        public static void PrepareText(SpriteFont storyFont, float cardWidth, float tipWidth)
        {
            for (int i = 0; i < All.Length; i++)
            {
                Motif m = All[i];
                string text = BattleRules.TurnBattleOn && m.TurnText.Length > 0 ? m.TurnText : m.Text;
                m.TextWrapped = Gfx.WrapText(storyFont, text, cardWidth, TextSize.StorySmall, 4);
                m.TipWrapped = Gfx.WrapText(storyFont, text, tipWidth, TextSize.StorySmall);
                m.RarityLabel = rarityNames[m.Rarity];

                //Initials : the first letters of the first two words, or the first two letters of one word.
                //A motif can set its own when two would come out the same (COUNTERPOINT "CP", not CODA's "CO").
                if (m.Initials != "") continue;
                string[] words = m.Name.Split(' ');
                m.Initials = words.Length >= 2 ? "" + words[0][0] + words[1][0] : m.Name.Substring(0, System.Math.Min(2, m.Name.Length));
            }
        }

        public static Motif Get(MotifId id)
        {
            return All[(int)id];
        }

        //Motif Roll : up to count different motifs the band does not own yet.
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
