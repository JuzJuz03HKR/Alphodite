using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Core;

namespace Tacetno433.Data
{
    //ConductorPerk : the PASSIVE, the one rule each conductor bends all the time.
    //BattleState checks these by name.
    public enum ConductorPerk
    {
        None,            // no rule bent (no conductor uses it since round 13)
        LockedTempo,     // THE METRONOME   the band plays the note's written mark, whatever the stroke size
        RunawayFire,     // THE INFERNO     a miss is not weaker and powers up the next beat
        CloserLouder,    // THE UNHEARING   the less breath the band has left, the harder it hits
        EveryRoadHome,   // THE FOLK LEADER mixed cultures on a beat hit harder, one seat fewer
        ByTheBook        // THE APPRENTICE  GOOD beats fill the signature's recipe too, not only PERFECT ones
    }

    //SignatureMove : the SIGNATURE, what SPACE does once the recipe is full. Every conductor has
    //their own (round 13), and each lasts for the next few strokes (BattleRules.SignatureStrokes).
    //BattleState checks these by name.
    public enum SignatureMove
    {
        AbsolutePitch,   // THE APPRENTICE  every stroke on time is IN TUNE, whatever its size
        Clockwork,       // THE METRONOME   every stroke on time is PERFECT, a GOOD one too
        SetAlight,       // THE INFERNO     every stroke hits harder and may push past the PUSH CAP
        DeafEars,        // THE UNHEARING   TACET's blows take no breath
        VillageBand      // THE FOLK LEADER every stroke brings the whole band, and pays as a middle one
    }

    //Conductor : one playable class. Everything shown on the select pages comes from these fields.
    public class Conductor
    {
        //Conductor Identity
        public string Name = "";
        public string BasedOn = "";        // the real musician the class is bent from
        public string Role = "";           // the rough build, shown under the face icon
        public string Epithet = "";        // one short line under the painting

        //Conductor Text
        public string Story = "";          // who they were
        public string MechanicName = "";   // the PASSIVE's name
        public string MechanicText = "";   // what the PASSIVE does
        public string SignatureMark = "";  // the SIGNATURE as a marking written in a score, for the cut-in
        public string SignatureName = "";  // the SIGNATURE's name
        public string SignatureText = "";  // what the SIGNATURE does, for the profile page
        public string SignatureCall = "";  // the same in one short line, for the duel's story box

        //Conductor Stats : 0 to 10, turned into real numbers by BattleRules
        public int Stamina;
        public int PushPower;
        public ConductorPerk Perk = ConductorPerk.None;
        public SignatureMove Move = SignatureMove.AbsolutePitch;

        //Signature Recipe : how many notes of each instrument family the signature needs,
        //in Family order : strings, winds, percussion. A good beat gives one note to every
        //family that played on it.
        public int[] Recipe = { 1, 1, 1 };

        //Conductor Look : brightness stand-in until the real portrait art goes in
        public Color ThemeColor = Palette.ToneC;

        //Conductor Unlock
        public bool Unlocked = true;
        public string UnlockHint = "";

        //Conductor Prepared Text : built ONCE at startup by PrepareText below.
        //Wrapping and joining strings inside Draw would make new strings 60 times a
        //second, which piles up rubbish for the garbage collector and makes a long
        //play session stutter. So we do that work once and only read it afterwards.
        public string BasedOnLabel = "";
        public string StoryWrapped = "";
        public string MechanicWrapped = "";
        public string SignatureWrapped = "";
        public string IndexLabel = "";     // "No.03"
        public string StaminaLabel = "";   // the real number the stat turns into
        public string PushLabel = "";
        public string RecipeLabel = "";    // "NEEDS  1 STRING   3 PERCUSSION"
        public string SkillsLabel = "";    // "BY THE BOOK   /   SPACE  ABSOLUTE PITCH", under the gallery painting
    }

    //ConductorList : THE PLACE TO EDIT CHARACTERS.
    //Add, remove or rewrite any block below and both select pages update by themselves.
    public static class ConductorList
    {
        //Family Words : for the recipe label, in Family order
        public static string[] FamilyWords = { "STRINGS", "WINDS", "PERCUSSION" };

        public static Conductor[] All = new Conductor[]
        {
            //Conductor 1 : starter class
            new Conductor
            {
                Name = "THE APPRENTICE",
                BasedOn = "ORIGINAL CHARACTER",
                Role = "BALANCED",
                Epithet = "Nothing special, and that is the point.",
                Story = "The one Endchestra called by mistake. No gift, no legend, "
                      + "no story anybody wrote down. Only a stick, and a refusal to let the music stop.",
                MechanicName = "BY THE BOOK",
                MechanicText = "Learns from every beat that lands. GOOD beats fill the signature's recipe too, "
                             + "not only PERFECT ones, so the signature comes round sooner. The class to learn the game on.",
                SignatureMark = "Semplice",
                SignatureName = "ABSOLUTE PITCH",
                SignatureText = "For the next 4 strokes, every stroke on time is IN TUNE, whatever its size "
                              + "and whatever the note says. Swing big to hit hard, or small to save breath.",
                SignatureCall = "* ABSOLUTE PITCH! Four strokes, any size on time is IN TUNE.",
                Recipe = new int[] { 2, 2, 2 },     // a little of everything. Round 13 : every recipe about twice as long, the
                                                    // signature lasts 4 strokes now (BattleRules.SignatureStrokes)
                Stamina = 5, PushPower = 5,
                Perk = ConductorPerk.ByTheBook, Move = SignatureMove.AbsolutePitch,   // round 13 : had no perk (EVEN HAND)
                ThemeColor = Palette.ToneB,
                Unlocked = true
            },

            //Conductor 2 : stamina class
            new Conductor
            {
                Name = "THE METRONOME",
                BasedOn = "J. S. BACH",
                Role = "ENDURANCE",
                Epithet = "The beat is not yours to move.",
                Story = "He wrote music like clockwork, and the clock never asked permission. "
                      + "Under his baton the ensemble never tires, because it never hurries.",
                MechanicName = "LOCKED TEMPO",
                MechanicText = "The band plays the mark written on TACET's note, p, mf or f, whatever size "
                             + "you swing. You only keep time. Every stroke costs a little more, and a false mark fools him too.",
                SignatureMark = "Tempo giusto",
                SignatureName = "CLOCKWORK",
                SignatureText = "For the next 4 strokes, every stroke that lands on time is PERFECT, even a GOOD one. "
                              + "The combo climbs, and the fire of FORTISSIMO comes sooner. A miss is still a miss.",
                SignatureCall = "* CLOCKWORK! Four strokes, every stroke on time is PERFECT.",
                Recipe = new int[] { 3, 0, 3 },     // the rhythm section
                Move = SignatureMove.Clockwork,
                Stamina = 10, PushPower = 6, Perk = ConductorPerk.LockedTempo,       // round 10 : 9 -> 7, no cheaper notes. Round 11a : 9,
                                                                                     // round 11b : 10 and push 6. Bigger rests and EASE help
                                                                                     // everyone but him (he cannot EASE). Round 12 : he pays
                                                                                     // for the whole band on every stroke. Round 12.2 : the
                                                                                     // band plays the written mark, see BattleRules.LockedTempoCost
                ThemeColor = Palette.ToneC,
                Unlocked = true
            },

            //Conductor 3 : damage class
            new Conductor
            {
                Name = "THE INFERNO",
                BasedOn = "HECTOR BERLIOZ",
                Role = "BURST",
                Epithet = "Louder than anyone thought was allowed.",
                Story = "He asked for a thousand players and meant it. "
                      + "Where others hear a mistake, he hears the next bar getting bigger.",
                MechanicName = "RUNAWAY FIRE",
                MechanicText = "The hardest push in the game, and the least breath. A missed stroke loses no power, "
                             + "and the beat after it hits 30 percent harder.",
                SignatureMark = "Con fuoco",
                SignatureName = "SET ALIGHT",
                SignatureText = "For the next 4 strokes the band burns: every stroke hits 30 percent harder "
                              + "and may push the line a quarter further than a note usually can.",
                SignatureCall = "* SET ALIGHT! Four strokes, harder and further than ever.",
                Recipe = new int[] { 2, 0, 3 },     // drums above all
                Stamina = 3, PushPower = 8, Perk = ConductorPerk.RunawayFire, Move = SignatureMove.SetAlight,   // round 13 : push 9 -> 8,
                                                                                     // SET ALIGHT pushes past the cap, and he won the most runs
                                                                                     // (77 percent, the others 70 to 74). Stamina 2 : 65 to 68
                ThemeColor = Palette.ToneA,
                Unlocked = true
            },

            //Conductor 4 : scaling class
            new Conductor
            {
                Name = "THE UNHEARING",
                BasedOn = "L. V. BEETHOVEN",
                Role = "SCALING",
                Epithet = "He wrote it deaf, and it was the loudest thing ever written.",
                Story = "Silence took his ears first and he kept writing anyway. "
                      + "He is the only conductor who is not afraid of TACET getting closer.",
                MechanicName = "THE CLOSER THE LOUDER",
                MechanicText = "You start weak. Once the band is down to half its breath it hits harder the lower it goes, "
                             + "up to 30 percent harder on its very last breath.",
                SignatureMark = "Senza paura",
                SignatureName = "DEAF EARS",
                SignatureText = "For the next 4 strokes TACET's blows take no breath at all. "
                              + "Let the band run low to hit hard, then stop listening.",
                SignatureCall = "* DEAF EARS! Four strokes, TACET's blows take no breath.",
                Recipe = new int[] { 3, 3, 0 },     // the strings carry it
                Stamina = 5, PushPower = 3, Perk = ConductorPerk.CloserLouder, Move = SignatureMove.DeafEars,
                                                                                     // round 10 : push 3 -> 4, perk up to 130 percent.
                                                                                     // Round 12.2 : rests breathe deeper when behind too.
                                                                                     // Round 13 : the perk counts lost breath, not the line,
                                                                                     // and push 4 -> 3 (DEAF EARS is worth a lot : 76 percent at 4)
                ThemeColor = Palette.ToneD,
                Unlocked = true
            },

            //Conductor 5 : combo class
            new Conductor
            {
                Name = "THE FOLK LEADER",
                BasedOn = "BELA BARTOK",
                Role = "SUPPORT",
                Epithet = "He walked the villages and wrote down what he heard.",
                Story = "He collected songs nobody had bothered to write down, "
                      + "and found out they all fit together.",
                MechanicName = "EVERY ROAD HOME",
                MechanicText = "Players from different cultures on the same beat hit harder together, "
                             + "7 percent for every culture after the first, but you begin the run with one seat fewer.",
                SignatureMark = "Alla rustica",
                SignatureName = "VILLAGE BAND",
                SignatureText = "For the next 4 strokes, any stroke brings the whole band in, every culture at once, "
                              + "and the band pays only what a middle stroke costs.",
                SignatureCall = "* VILLAGE BAND! Four strokes, the whole band for the price of a middle one.",
                Recipe = new int[] { 2, 3, 2 },     // the pipes of every road
                Move = SignatureMove.VillageBand,
                Stamina = 6, PushPower = 5, Perk = ConductorPerk.EveryRoadHome,      // round 12.2 : push 6 -> 5, he won the most
                                                                                     // runs (77 to 82 percent, the others 67 to 75)
                ThemeColor = Palette.ToneE,
                Unlocked = true
            },
        };

        //Text Prepare : called once from LoadContent after the font exists.
        //Everything the screens need to print is built here so drawing stays free of
        //string work. If you change any text above, it gets picked up on the next launch.
        public static void PrepareText(SpriteFont font, float wrapWidth, float scale)
        {
            for (int i = 0; i < All.Length; i++)
            {
                Conductor c = All[i];
                c.BasedOnLabel = "BASED ON " + c.BasedOn;
                c.StoryWrapped = Gfx.WrapText(font, c.Story, wrapWidth, scale);
                c.MechanicWrapped = Gfx.WrapText(font, c.MechanicText, wrapWidth, scale);
                c.SignatureWrapped = Gfx.WrapText(font, c.SignatureText, wrapWidth, scale);
                c.SkillsLabel = c.MechanicName + "   /   SPACE  " + c.SignatureName;
                c.IndexLabel = "No." + (i + 1).ToString("00");
                c.StaminaLabel = (BattleRules.StaminaBase + c.Stamina * BattleRules.StaminaPerPoint).ToString();
                c.PushLabel = "x" + (BattleRules.PowerBase + c.PushPower * BattleRules.PowerPerPoint).ToString("0.00");

                //Recipe Label : only the families the recipe asks for
                c.RecipeLabel = "NEEDS";
                for (int f = 0; f < c.Recipe.Length; f++)
                    if (c.Recipe[f] > 0) c.RecipeLabel += "   " + c.Recipe[f] + " " + FamilyWords[f];
            }
        }
    }
}
