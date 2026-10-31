using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Core;

namespace Tacetno433.Data
{
    //Family : the three instrument groups. The family is also the section of the stage they sit
    //in (round 15, see StageLayout) : strings at the front, winds in the middle, percussion behind.
    public enum Family { String, Wind, Percussion }

    //Spot Look (Beta1 round 4) : a musician's own colour and what it adds to their blows (Palette spot colours,
    //Core/HitFx.cs). GOLD = golden sparkles, RED = flames. Only two for now, the user picks the real ones with the art.
    public enum SpotLook { Gold, Red }

    //MusicianTrait : the one thing each musician does that nobody else does.
    //Every trait comes straight from the line written about them, and TurnBattle (BattleState in the old duel) checks
    //each one by name, so searching for a trait finds every place it works.
    //THE FIVE KITS (Beta1 29 Oct) : the five legends got traits of their own, the last five below. KeepsCount, QuietPart,
    //Momentum, FourBars and Forgiven were theirs before (ANNA's, KLARA's ... until then) and only the old rhythm duel
    //(Battle/BattleState.cs) still reads them. New traits go at the end.
    public enum MusicianTrait
    {
        None,
        KeepsCount,      // (old duel) beats the baton goes their way have a wider PERFECT window (COUNTS ALOUD)
        QuietPart,       // (old duel) twice as hard against TACET's p notes (THE QUIET PART)
        Momentum,        // (old duel) each beat in a row they play hits 1 harder, up to 3 (MOMENTUM)
        ByEar,           // MALI      50 percent harder on TACET's hidden beats (hidden since 29 Oct)
        HeldNote,        // CHAI      his note rings on into a beat nobody plays, at 40 percent (hidden since 29 Oct)
        OneStepBetter,   // NUAN      30 percent harder when TACET plays loud (hidden since 29 Oct)
        FourBars,        // (old duel) the fourth beat of a bar they play all of hits twice as hard (FOUR BARS STRAIGHT)
        Forgiven,        // (old duel) on their beats a late stroke still counts as GOOD (FASHIONABLY LATE)
        Thunder,         // BORIS     his big stroke on an f note knocks 2 off TACET's next note (hidden since 29 Oct)
        DevilsBargain,   // PAGANINI  short of BREATH, their skills take life instead (THE DEVIL'S BARGAIN)
        BreathControl,   // QUANTZ    their ATTACK gives more BREATH (BREATH CONTROL)
        TurnOfTheHand,   // PFUNDT    an enemy their skill hits plays one note fewer in its next attack (TURN OF THE HAND)
        TheScar,         // DUPORT    every attack that hurts them gives CRESCENDO (THE SCAR)
        HuntingCall      // PUNTO     the musician who plays right after them hits harder (THE HUNTING CALL)
    }

    //Musician : one player in the ensemble.
    //Their motif is written by us and the player never edits it, they only choose
    //which step it fires on.
    public class Musician
    {
        public string Name = "";
        public string Instrument = "";
        public string Culture = "";
        public Family Family = Family.String;
        public SpotLook Spot = SpotLook.Gold;  // their colour in a fight (round 4 : strings and percussion GOLD, winds RED for now)

        //Legend (Beta1, 29 Oct, docs/CHARACTER_DESIGN.md 5) : the user's five are real musicians who became legends on
        //their instrument. The name the legends give them, and the weapon their instrument becomes (the user's theme :
        //instruments are weapons). The user designs their look and decides who is a woman, so no word here says he or she.
        public string Epithet = "";           // "THE DEVIL'S VIOLINIST"
        public string Weapon = "";            // "IL CANNONE"

        //Cue : the side of the stage this musician sits on, 0 left, 1 centre, 2 right, where their
        //instrument sits in a real orchestra. The baton points at it on some beats of the 4/4 shape
        //(down and up the centre), and they hit harder then. Drawn as their arrow. With the
        //family it makes their chair, and each chair belongs to one musician only (StageLayout.HomeSeat).
        public int Cue = 1;
        public string Line = "";              // one short personality line

        //Instrument Sound (round 15) : one recorded note of their instrument, Content/Audio/Instruments/<Sample>,
        //played at the pitch of every note of the song (SoundBank.PlayInstrument). SampleNote is the
        //note the file was recorded on as a MIDI number (60 is middle C), 0 for a drum with no pitch.
        public string Sample = "";
        public int SampleNote = 60;
        public int Index;                     // where they are in MusicianList.All, set by PrepareText

        //Musician Battle Stats
        public int Power = 4;                 // how hard one note pushes (round 15 : playing costs no stamina, there is no cost)

        //Rehearsed : the TIER of their weapon in THIS run, 0 to BattleRules.RehearseMax (WEAPON TIERS, 29 Oct, each tier hits
        //harder). REHEARSAL (31 Oct, Data/Rehearsal.cs) : it is the stars of their ATTACK too, the third adds the weapon's legend.
        //Earned after fights won, at rest stops, the luthier, the shop, events and bosses.
        //RunState.Start sets it back to zero, so a new run starts clean. The save file keeps it (rehearsed=).
        public int Rehearsed = 0;

        //REHEARSAL (Beta1 31 Oct, Data/Rehearsal.cs) : the stars of each of their two skills in THIS run (0 to 3), the two skills
        //as those stars rewrote them (null : the plain ones, SkillList.First and Second give the right ones either way) and the
        //rules the stars gave them. RunState.Start sets them back like Rehearsed, the save file keeps the stars (stars=).
        public int[] SkillStars = new int[2];
        public Skill[] Kit;
        public bool[] Rules = new bool[32];

        //Has : a star gave them this rule (TurnBattle checks them by name). The ATTACK's third star is read from the weapon's tier,
        //so a tier set anywhere (the luthier, an event, the save file) counts at once.
        public bool Has(StarRule rule)
        {
            if (Rules[(int)rule]) return true;
            return rule != StarRule.None && Rehearsed >= RehearsalList.Most && RehearsalList.WeaponLegend(this) == rule;
        }

        //Life (Alpha 7) : this musician's life during THIS run, kept from one turn battle to the next.
        //-1 means full. RunState.Start sets it back, see RunState.LifeOf.
        public int Life = -1;

        //Trait : what makes them different in a duel, see MusicianTrait
        public MusicianTrait Trait = MusicianTrait.None;
        public string TraitName = "";
        public string TraitText = "";
        public string TurnTraitText = "";     // Alpha 7 step 10 : what the trait does in the turn battle (TurnBattle, search the name)

        //Musician Look : brightness stand-in until the real sprite goes in
        public Color ThemeColor = Palette.ToneC;

        //Prepared Text
        //Power is NOT prepared here: it changes with rehearsals and motifs, so every page shows
        //RunState.PowerOf through NumberText instead. One number everywhere.
        public string LineWrapped = "";
        public string TraitWrapped = "";
        public string FamilyLabel = "";
        public string PartLabel = "";         // "STRINGS   LEFT", their chair in words
        public string NameTag = "";           // "PAGANINI/"  the gacha style name with a slash
        public string[] WeaponAt = new string[0];   // their weapon at each tier : "IL CANNONE", "IL CANNONE  I" ... (WEAPON TIERS)

        //Weapon Now : their weapon at the tier it has now (a broken save's tier is kept inside the list)
        public string WeaponNow
        {
            get { return WeaponAt.Length == 0 ? Weapon : WeaponAt[Math.Max(0, Math.Min(Rehearsed, WeaponAt.Length - 1))]; }
        }
    }

    //MusicianList : THE PLACE TO EDIT MUSICIANS.
    //NO MORE ERAS (Beta1, 29 Oct) : the user took the eras out and kept five legends, one for each instrument that stays
    //(violin, flute, timpani, cello, horn, docs/CHARACTER_DESIGN.md 5). They took the places of ANNA, KLARA, OTTO, LUKA and
    //IRIS. THE FIVE KITS (29 Oct, BETA1_DIRECTION 11.6) : their skills (Data/Skill.cs) and traits come from their own
    //legends now. Those traits only work in the turn battle, so they have a TurnTraitText and no TraitText (the old duel's).
    //MALI, CHAI and NUAN (the Siam era) and BORIS (bass drum) stay in All, hidden
    //(Shown), because the save file keeps musicians by their place in All : NEVER REMOVE OR REORDER, only add at the end.
    //Rough guide for stats (round 15) : everybody plays every note and nobody costs stamina, so the powers sit close
    //together, percussion a little stronger, winds a little weaker. What sets players apart is their trait.
    //(Round 14 : winds were cheap and weak, percussion hit 8 to 10 and cost 7 to 8.)
    //Cue : where they sit, as in a real orchestra : violins left, cello right, flute centre. Every family has each side
    //once at most, so no two players share a chair.
    public static class MusicianList
    {
        public static Musician[] All = new Musician[]
        {
            //0 : violin. Niccolo Paganini (1782 - 1840) : rumour said he sold his soul to the devil. His Guarneri violin, the
            //one he called his cannon (Il Cannone), was left to Genoa in his will. IL CANNONE : the violin is the guard of a
            //sword and the bow its blade, and it hits like a cannon.
            new Musician { Name = "PAGANINI", Instrument = "VIOLIN",    Culture = "EUROPEAN", Family = Family.String,     Cue = 0, Spot = SpotLook.Gold,
                           Epithet = "THE DEVIL'S VIOLINIST", Weapon = "IL CANNONE",
                           Power = 5, Sample = "violin", SampleNote = 60, ThemeColor = Palette.ToneB,
                           Trait = MusicianTrait.DevilsBargain, TraitName = "THE DEVIL'S BARGAIN",
                           TurnTraitText = "Short of BREATH, their skills take " + BattleRules.TurnPaganiniLife + " life for each BREATH missing.",
                           Line = "They say the devil taught those hands. Nobody asks twice." },

            //1 : flute. Johann Joachim Quantz (1697 - 1773) : the flute teacher of Frederick the Great, about 300 concertos
            //for the king's evening concerts. The king was the one who gave Bach the theme of the Musical Offering (1747).
            new Musician { Name = "QUANTZ",   Instrument = "FLUTE",     Culture = "EUROPEAN", Family = Family.Wind,       Cue = 1, Spot = SpotLook.Red,
                           Epithet = "THE KING'S FLUTE MASTER", Weapon = "FLUTE RAPIER",
                           Power = 4, Sample = "flute", SampleNote = 72, ThemeColor = Palette.ToneC,
                           Trait = MusicianTrait.BreathControl, TraitName = "BREATH CONTROL",
                           TurnTraitText = "Their ATTACK gives " + BattleRules.TurnBreathControl + " BREATH more.",
                           Line = "Taught a king the flute. Three hundred concertos, one listener." },

            //2 : timpani. Ernst Pfundt (1806 - 1871) : the timpanist of the Leipzig Gewandhaus, credited with the crank timpani
            //that tune with a turn of the hand, and the author of a book on the timpani (Die Pauken, 1849).
            new Musician { Name = "PFUNDT",   Instrument = "TIMPANI",   Culture = "EUROPEAN", Family = Family.Percussion, Cue = 2, Spot = SpotLook.Gold,
                           Epithet = "WHO TUNES THE THUNDER", Weapon = "TWIN HAMMERS",
                           Power = 6, Sample = "timpani", SampleNote = 0, ThemeColor = Palette.ToneD,
                           Trait = MusicianTrait.TurnOfTheHand, TraitName = "TURN OF THE HAND",
                           TurnTraitText = "An enemy their skill hits plays " + BattleRules.TurnHandCut + " note fewer next time (" + BattleRules.TurnHandLeast + " at least).",
                           Line = "One turn of the hand, and the thunder changes key." },

            //3 to 5 : the Siam era, hidden since 29 Oct (Shown)
            new Musician { Name = "MALI",  Instrument = "SO DUANG",  Culture = "SIAM",     Family = Family.String,     Cue = 1, Spot = SpotLook.Gold,
                           Power = 5, Sample = "soduang", SampleNote = 60, ThemeColor = Palette.ToneA,
                           Trait = MusicianTrait.ByEar, TraitName = "BY EAR",
                           TraitText = "Hits 50 percent harder on TACET's hidden ??? beats.",
                           TurnTraitText = "Hits 50 percent harder against an enemy that is OFF-BEAT.",
                           Line = "Learned by ear in a courtyard and has never read a page." },
            new Musician { Name = "CHAI",  Instrument = "PI NAI",    Culture = "SIAM",     Family = Family.Wind,       Cue = 2, Spot = SpotLook.Red,
                           Power = 4, Sample = "pinai", SampleNote = 62, ThemeColor = Palette.ToneC,
                           Trait = MusicianTrait.HeldNote, TraitName = "HELD NOTE",
                           TraitText = "When nobody plays a beat, his last note rings on at 40 percent.",
                           TurnTraitText = "His ATTACK rings on : a second hit for 40 percent.",
                           Line = "Can hold one note longer than anyone thinks is possible." },
            new Musician { Name = "NUAN",  Instrument = "RANAT EK",  Culture = "SIAM",     Family = Family.Percussion, Cue = 0, Spot = SpotLook.Gold,
                           Power = 6, Sample = "ranatek", SampleNote = 72, ThemeColor = Palette.ToneB,
                           Trait = MusicianTrait.OneStepBetter, TraitName = "ONE STEP BETTER",
                           TraitText = "Hits 30 percent harder when TACET plays loud (f).",
                           TurnTraitText = "Hits 30 percent harder against an enemy whose next attack has " + BattleRules.TurnLongNotes + " notes or more.",
                           Line = "Answers whatever she hears, faster and one step better." },

            //6 : cello. Jean-Louis Duport (1749 - 1819) : thought to have played the first Cello Sonatas Op. 5 with Beethoven in
            //Berlin, 1796. The legend of the Duport Stradivarius : Napoleon tried it in 1812 and left a dent that is still there.
            //THE SCARRED SHIELD : the cello is a great shield with the emperor's dent in it, the bow a lance.
            new Musician { Name = "DUPORT",   Instrument = "CELLO",     Culture = "EUROPEAN", Family = Family.String,     Cue = 2, Spot = SpotLook.Gold,
                           Epithet = "SCARRED BY AN EMPEROR", Weapon = "THE SCARRED SHIELD",
                           Power = 6, Sample = "cello", SampleNote = 48, ThemeColor = Palette.ToneC,
                           Trait = MusicianTrait.TheScar, TraitName = "THE SCAR",
                           TurnTraitText = "Every attack that hurts them gives " + BattleRules.TurnScarCrescendo + " CRESCENDO.",
                           Line = "An emperor once tried this cello and left a scar. It plays louder for it." },

            //7 : horn. Giovanni Punto (1746 - 1803, born Jan Vaclav Stich in Bohemia) : Beethoven wrote the Horn Sonata Op. 17
            //for them and they played it first together in Vienna, 18 April 1800, when Punto was far more famous than
            //Beethoven. HORN CHAKRAM : the coiled horn is a ring of blades, thrown and caught again.
            new Musician { Name = "PUNTO",    Instrument = "HORN",      Culture = "EUROPEAN", Family = Family.Wind,       Cue = 0, Spot = SpotLook.Red,
                           Epithet = "LOUDER THAN BEETHOVEN", Weapon = "HORN CHAKRAM",
                           Power = 5, Sample = "horn", SampleNote = 48, ThemeColor = Palette.ToneA,
                           Trait = MusicianTrait.HuntingCall, TraitName = "THE HUNTING CALL",
                           TurnTraitText = "The musician who plays right after them in the bar hits harder.",
                           Line = "Once more famous than Beethoven, and plays like nobody said otherwise." },

            //8 : bass drum, hidden since 29 Oct (Shown)
            new Musician { Name = "BORIS", Instrument = "BASS DRUM", Culture = "EUROPEAN", Family = Family.Percussion, Cue = 1, Spot = SpotLook.Gold,
                           Power = 7, Sample = "bassdrum", SampleNote = 0, ThemeColor = Palette.ToneD,
                           Trait = MusicianTrait.Thunder, TraitName = "THE WHOLE FLOOR",
                           TraitText = "His big stroke on an f note knocks 2 off TACET's next note.",
                           TurnTraitText = "The PERFECT last note of his skills takes 2 off each note of the enemy's next attack.",
                           Line = "One hit from him and the whole floor knows about it." },
        };

        //Shown : the musicians a run can meet, the user's five legends. The others stay in All for old saves only.
        public static Musician[] Shown = { All[0], All[1], All[2], All[6], All[7] };

        private static string[] familyNames = { "STRING", "WIND", "PERCUSSION" };

        //Tier Names : WEAPON TIERS, the numeral after a weapon's name (tier 0 is the weapon as it starts)
        public static readonly string[] TierNames = { "", "I", "II", "III" };

        //Text Prepare : called once from LoadContent after the font exists
        public static void PrepareText(SpriteFont storyFont, float wrapWidth)
        {
            for (int i = 0; i < All.Length; i++)
            {
                Musician m = All[i];
                if (BattleRules.TurnBattleOn && m.TurnTraitText != "") m.TraitText = m.TurnTraitText;   // Alpha 7 : the turn battle's words
                m.LineWrapped = Gfx.WrapText(storyFont, m.Line, wrapWidth, TextSize.Story);
                m.TraitWrapped = Gfx.WrapText(storyFont, m.TraitText, wrapWidth, TextSize.StorySmall);
                m.FamilyLabel = familyNames[(int)m.Family] + "  /  " + m.Culture;
                m.PartLabel = StageLayout.Rows[StageLayout.SectionOf(m.Family)].Name + "   " + StageLayout.SideNames[m.Cue];
                m.NameTag = m.Name + "/";
                m.Index = i;

                //WEAPON TIERS : the weapon's name at each tier (a hidden musician from an old save has none, their instrument stands in)
                string weapon = m.Weapon.Length > 0 ? m.Weapon : m.Instrument;
                m.WeaponAt = new string[BattleRules.RehearseMax + 1];
                for (int t = 0; t < m.WeaponAt.Length; t++)
                    m.WeaponAt[t] = t == 0 ? weapon : weapon + "  " + TierNames[Math.Min(t, TierNames.Length - 1)];
            }
        }

        //Run Reset : forget everything the last run taught them
        public static void ResetRehearsals()
        {
            for (int i = 0; i < All.Length; i++)
            {
                All[i].Rehearsed = 0;
                All[i].Life = -1;
                RehearsalList.Reset(All[i]);             // REHEARSAL : no star on any skill
            }
        }

        //Musician Roll : someone a run can meet (Shown) who is not in the band yet, null when nobody is left
        public static Musician Roll(List<Musician> alreadyHave, Random random)
        {
            List<Musician> pool = new List<Musician>();
            for (int i = 0; i < Shown.Length; i++)
                if (!alreadyHave.Contains(Shown[i])) pool.Add(Shown[i]);

            if (pool.Count == 0) return null;
            return pool[random.Next(pool.Count)];
        }
    }
}
