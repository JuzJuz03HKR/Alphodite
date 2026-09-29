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

    //MusicianTrait : the one thing each musician does that nobody else does.
    //Every trait comes straight from the line written about them, and BattleState checks
    //each one by name, so searching for a trait finds every place it works.
    public enum MusicianTrait
    {
        None,
        KeepsCount,      // ANNA   beats the baton goes her way have a wider PERFECT window
        QuietPart,       // KLARA  twice as hard against TACET's p notes (round 14, she used to play on every stroke)
        Momentum,        // OTTO   each beat in a row he plays hits 1 harder, up to 3
        ByEar,           // MALI   50 percent harder on TACET's hidden beats
        HeldNote,        // CHAI   his note rings on into a beat nobody plays, at 40 percent
        OneStepBetter,   // NUAN   30 percent harder when TACET plays loud
        FourBars,        // LUKA   the fourth beat of a bar he plays all of hits twice as hard
        Forgiven,        // IRIS   on her beats a late stroke still counts as GOOD
        Thunder          // BORIS  his big stroke on an f note knocks 2 off TACET's next note
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
        public int Era;                       // which era in EraList they come from

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

        //Rehearsed : extra power earned during THIS run at rest stops and events.
        //RunState.Start sets it back to zero, so a new run starts clean.
        public int Rehearsed = 0;

        //Trait : what makes them different in a duel, see MusicianTrait
        public MusicianTrait Trait = MusicianTrait.None;
        public string TraitName = "";
        public string TraitText = "";

        //Musician Look : brightness stand-in until the real sprite goes in
        public Color ThemeColor = Palette.ToneC;

        //Prepared Text
        //Power is NOT prepared here: it changes with rehearsals and motifs, so every page shows
        //RunState.PowerOf through NumberText instead. One number everywhere.
        public string LineWrapped = "";
        public string TraitWrapped = "";
        public string FamilyLabel = "";
        public string PartLabel = "";         // "STRINGS   LEFT", their chair in words
        public string NameTag = "";           // "ANNA/"  the gacha style name with a slash
    }

    //MusicianList : THE PLACE TO EDIT MUSICIANS.
    //Era is the index into EraList, so 0 is CLASSICAL, 1 is SIAM, 2 is ROMANTIC.
    //Rough guide for stats (round 15) : everybody plays every note and nobody costs stamina, so
    //the powers sit close together, percussion a little stronger, winds a little weaker, the
    //last era a little stronger than the first. What sets players apart is their trait.
    //(Round 14 : winds were cheap and weak, percussion hit 8 to 10 and cost 7 to 8.)
    //Cue : where they sit, as in a real orchestra : violins left, cello right, flute centre.
    //Every era has one player for each side, and every family has each side once, so a band
    //from one era always answers all three ways, and no two players share a chair.
    public static class MusicianList
    {
        public static Musician[] All = new Musician[]
        {
            //Era 0 : CLASSICAL
            new Musician { Name = "ANNA",  Instrument = "VIOLIN",    Culture = "EUROPEAN", Family = Family.String,     Era = 0, Cue = 0,
                           Power = 5, Sample = "violin", SampleNote = 60, ThemeColor = Palette.ToneB,
                           Trait = MusicianTrait.KeepsCount, TraitName = "COUNTS ALOUD",
                           TraitText = "On her beats, when the baton points her way, the PERFECT window is wider.",
                           Line = "Counts every bar out loud, even when nobody asked her to." },
            new Musician { Name = "KLARA", Instrument = "FLUTE",     Culture = "EUROPEAN", Family = Family.Wind,       Era = 0, Cue = 1,
                           Power = 4, Sample = "flute", SampleNote = 72, ThemeColor = Palette.ToneC,
                           Trait = MusicianTrait.QuietPart, TraitName = "THE QUIET PART",
                           TraitText = "Against TACET's soft p notes she plays twice as hard.",
                           Line = "Plays the quiet parts nobody else wants and never misses one." },
            new Musician { Name = "OTTO",  Instrument = "TIMPANI",   Culture = "EUROPEAN", Family = Family.Percussion, Era = 0, Cue = 2,
                           Power = 6, Sample = "timpani", SampleNote = 0, ThemeColor = Palette.ToneD,
                           Trait = MusicianTrait.Momentum, TraitName = "MOMENTUM",
                           TraitText = "Each beat in a row he plays hits 1 harder, up to 3.",
                           Line = "Slow to start, impossible to stop once he has." },

            //Era 1 : SIAM
            new Musician { Name = "MALI",  Instrument = "SO DUANG",  Culture = "SIAM",     Family = Family.String,     Era = 1, Cue = 1,
                           Power = 5, Sample = "soduang", SampleNote = 60, ThemeColor = Palette.ToneA,
                           Trait = MusicianTrait.ByEar, TraitName = "BY EAR",
                           TraitText = "Hits 50 percent harder on TACET's hidden ??? beats.",
                           Line = "Learned by ear in a courtyard and has never read a page." },
            new Musician { Name = "CHAI",  Instrument = "PI NAI",    Culture = "SIAM",     Family = Family.Wind,       Era = 1, Cue = 2,
                           Power = 4, Sample = "pinai", SampleNote = 62, ThemeColor = Palette.ToneC,
                           Trait = MusicianTrait.HeldNote, TraitName = "HELD NOTE",
                           TraitText = "When nobody plays a beat, his last note rings on at 40 percent.",
                           Line = "Can hold one note longer than anyone thinks is possible." },
            new Musician { Name = "NUAN",  Instrument = "RANAT EK",  Culture = "SIAM",     Family = Family.Percussion, Era = 1, Cue = 0,
                           Power = 6, Sample = "ranatek", SampleNote = 72, ThemeColor = Palette.ToneB,
                           Trait = MusicianTrait.OneStepBetter, TraitName = "ONE STEP BETTER",
                           TraitText = "Hits 30 percent harder when TACET plays loud (f).",
                           Line = "Answers whatever she hears, faster and one step better." },

            //Era 2 : ROMANTIC
            new Musician { Name = "LUKA",  Instrument = "CELLO",     Culture = "EUROPEAN", Family = Family.String,     Era = 2, Cue = 2,
                           Power = 6, Sample = "cello", SampleNote = 48, ThemeColor = Palette.ToneC,
                           Trait = MusicianTrait.FourBars, TraitName = "FOUR BARS STRAIGHT",
                           TraitText = "Playing all four beats of a bar, the fourth hits twice as hard.",
                           Line = "Says almost nothing and then plays for four bars straight." },
            new Musician { Name = "IRIS",  Instrument = "HORN",      Culture = "EUROPEAN", Family = Family.Wind,       Era = 2, Cue = 0,
                           Power = 5, Sample = "horn", SampleNote = 48, ThemeColor = Palette.ToneA,
                           Trait = MusicianTrait.Forgiven, TraitName = "FASHIONABLY LATE",
                           TraitText = "On her beats, when the baton points her way, a late stroke still counts as GOOD.",
                           Line = "Arrives late, plays louder than the rest, is forgiven." },
            new Musician { Name = "BORIS", Instrument = "BASS DRUM", Culture = "EUROPEAN", Family = Family.Percussion, Era = 2, Cue = 1,
                           Power = 7, Sample = "bassdrum", SampleNote = 0, ThemeColor = Palette.ToneD,
                           Trait = MusicianTrait.Thunder, TraitName = "THE WHOLE FLOOR",
                           TraitText = "His big stroke on an f note knocks 2 off TACET's next note.",
                           Line = "One hit from him and the whole floor knows about it." },
        };

        private static string[] familyNames = { "STRING", "WIND", "PERCUSSION" };

        //Text Prepare : called once from LoadContent after the font exists
        public static void PrepareText(SpriteFont storyFont, float wrapWidth)
        {
            for (int i = 0; i < All.Length; i++)
            {
                Musician m = All[i];
                m.LineWrapped = Gfx.WrapText(storyFont, m.Line, wrapWidth, TextSize.Story);
                m.TraitWrapped = Gfx.WrapText(storyFont, m.TraitText, wrapWidth, TextSize.StorySmall);
                m.FamilyLabel = familyNames[(int)m.Family] + "  /  " + m.Culture;
                m.PartLabel = StageLayout.Rows[StageLayout.SectionOf(m.Family)].Name + "   " + StageLayout.SideNames[m.Cue];
                m.NameTag = m.Name + "/";
                m.Index = i;
            }
        }

        //Run Reset : forget everything the last run taught them
        public static void ResetRehearsals()
        {
            for (int i = 0; i < All.Length; i++)
                All[i].Rehearsed = 0;
        }

        //Available In Era : how many musicians of this era are not in the band yet
        public static int AvailableInEra(int era, List<Musician> alreadyHave)
        {
            int count = 0;
            for (int i = 0; i < All.Length; i++)
                if (All[i].Era == era && !alreadyHave.Contains(All[i])) count++;
            return count;
        }

        //Musician Roll : pick someone from this era who is not already in the band.
        //Returns null when the era has nobody left to give.
        public static Musician RollFromEra(int era, List<Musician> alreadyHave, Random random)
        {
            List<Musician> pool = new List<Musician>();

            for (int i = 0; i < All.Length; i++)
                if (All[i].Era == era && !alreadyHave.Contains(All[i]))
                    pool.Add(All[i]);

            if (pool.Count == 0) return null;
            return pool[random.Next(pool.Count)];
        }
    }
}
