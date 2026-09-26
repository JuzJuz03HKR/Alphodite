using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Core;

namespace Tacetno433.Data
{
    //ConductorPerk : the one rule each conductor bends. BattleState checks these by name.
    public enum ConductorPerk
    {
        None,            // THE APPRENTICE
        LockedTempo,     // THE METRONOME   every stroke a boost, whatever its size, never ease
        RunawayFire,     // THE INFERNO     a miss is not weaker and powers up the next beat
        CloserLouder,    // THE UNHEARING   stronger the further TACET has pushed
        EveryRoadHome    // THE FOLK LEADER mixed cultures on a beat hit harder, one seat fewer
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
        public string MechanicName = "";   // the gimmick heading
        public string SignatureMark = "";  // the same move as a marking written in a score, for the cut-in
        public string MechanicText = "";   // the gimmick body

        //Conductor Stats : 0 to 10, turned into real numbers by BattleRules
        public int Stamina;
        public int PushPower;
        public ConductorPerk Perk = ConductorPerk.None;

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
        public string IndexLabel = "";     // "No.03"
        public string StaminaLabel = "";   // the real number the stat turns into
        public string PushLabel = "";
        public string RecipeLabel = "";    // "NEEDS  1 STRING   3 PERCUSSION"
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
                MechanicName = "EVEN HAND",
                SignatureMark = "Semplice",
                Recipe = new int[] { 1, 1, 1 },     // a little of everything
                MechanicText = "Every system sits at its standard value. No bonus, no penalty. "
                             + "The class to learn the game on.",
                Stamina = 5, PushPower = 5,
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
                SignatureMark = "Tempo giusto",
                Recipe = new int[] { 2, 0, 2 },     // the rhythm section
                MechanicText = "Every stroke is a BOOST, whatever its size, so a small flick hits as hard "
                             + "as a big swing. Ease is sealed.",
                Stamina = 10, PushPower = 6, Perk = ConductorPerk.LockedTempo,       // round 10 : 9 -> 7, no cheaper notes. Round 11a : 9,
                                                                                     // round 11b : 10 and push 6. Bigger rests and EASE help
                                                                                     // everyone but him (he cannot EASE)
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
                SignatureMark = "Con fuoco",
                Recipe = new int[] { 1, 0, 3 },     // drums above all
                MechanicText = "The hardest push in the game. A missed stroke loses no power, "
                             + "and the beat after it hits 30 percent harder.",
                Stamina = 3, PushPower = 9, Perk = ConductorPerk.RunawayFire,
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
                SignatureMark = "Sempre crescendo",
                Recipe = new int[] { 3, 1, 0 },     // the strings carry it
                MechanicText = "You start weak. The nearer TACET creeps to your edge, "
                             + "the harder your ensemble hits, up to 130 percent.",
                Stamina = 5, PushPower = 4, Perk = ConductorPerk.CloserLouder,       // round 10 : push 3 -> 4, perk up to 130 percent
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
                SignatureMark = "Alla rustica",
                Recipe = new int[] { 1, 2, 1 },     // the pipes of every road
                MechanicText = "Players from different cultures on the same beat hit harder together, "
                             + "but you begin the run with one seat fewer.",
                Stamina = 6, PushPower = 6, Perk = ConductorPerk.EveryRoadHome,
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
