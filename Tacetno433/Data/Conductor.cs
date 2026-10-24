using System;
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
        EveryRoadHome,   // THE FOLK LEADER mixed cultures on a beat hit harder
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

    //TurnKit : whose PASSIVE and SIGNATURE the turn battle plays (Alpha 7, step 5). None plays without.
    //Step 10 adds the other four of the lore. KEEP THE ORDER, append only.
    public enum TurnKit { None, Beethoven, Mozart, Bach, Paganini, Lully, Fanny }

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

        //Turn Battle (Alpha 7) : the kit this conductor plays in the turn battle, and its words.
        //While BattleRules.TurnBattleOn, PrepareText shows these instead of the duel's PASSIVE and SIGNATURE.
        public TurnKit Turn = TurnKit.None;
        public string TurnPassiveName = "";
        public string TurnPassiveText = "";
        public string TurnSignatureName = "";
        public string TurnSignatureText = "";
        public string TurnRole = "";          // the role in the turn battle when it differs from Role

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
                MechanicText = "GOOD beats fill the signature too, so it comes sooner. The class to learn on.",
                SignatureMark = "Semplice",
                SignatureName = "ABSOLUTE PITCH",
                SignatureText = "4 strokes : any size on time is right and IN TUNE. Just keep the beat.",
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
                MechanicText = "Any size you swing is the right size. You only keep time.",
                SignatureMark = "Tempo giusto",
                SignatureName = "CLOCKWORK",
                SignatureText = "4 strokes : every stroke on time is PERFECT, even a GOOD one.",
                SignatureCall = "* CLOCKWORK! Four strokes, every stroke on time is PERFECT.",
                Recipe = new int[] { 3, 0, 3 },     // the rhythm section
                Move = SignatureMove.Clockwork,
                Turn = TurnKit.Bach,                // step 10
                TurnRole = "PRECISION",
                TurnPassiveName = "LOCKED TEMPO",
                TurnPassiveText = "Any size you swing is the right size. You only keep time.",
                TurnSignatureName = "FUGUE",
                TurnSignatureText = "The 4/4 shape : every musician's next move plays twice. Better strokes, louder echo.",
                Stamina = 5, PushPower = 6, Perk = ConductorPerk.LockedTempo,        // round 15 : 10 -> 5, his size is never wrong and playing
                                                                                     // is free, at 10 he won 91 to 94 percent (the others 80 to 89).
                                                                                     // Round 10 : 9 -> 7, no cheaper notes. Round 11a : 9,
                                                                                     // round 11b : 10 and push 6. Bigger rests and EASE help
                                                                                     // everyone but him (he cannot EASE). Round 12 : he pays
                                                                                     // for the whole band on every stroke. Round 12.2 : the
                                                                                     // band plays the written mark, and paid 1.33 for it.
                                                                                     // Round 15 : playing is free, his size is never wrong
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
                MechanicText = "Hardest push, least breath. A miss loses no power, and the next beat hits 30 percent harder.",
                SignatureMark = "Con fuoco",
                SignatureName = "SET ALIGHT",
                SignatureText = "4 strokes : 30 percent harder, and a quarter past the usual push limit.",
                SignatureCall = "* SET ALIGHT! Four strokes, harder and further than ever.",
                Recipe = new int[] { 2, 0, 3 },     // drums above all
                Stamina = 3, PushPower = 7, Perk = ConductorPerk.RunawayFire, Move = SignatureMove.SetAlight,   // round 15 : push 8 -> 7 (88 to 90 percent).
                                                                                     // Round 13 : push 9 -> 8,
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
                MechanicText = "Below half breath, the lower it goes the harder the band hits, up to 25 percent.",
                SignatureMark = "Senza paura",
                SignatureName = "DEAF EARS",
                SignatureText = "4 strokes : TACET's blows take no breath.",
                SignatureCall = "* DEAF EARS! Four strokes, TACET's blows take no breath.",
                Recipe = new int[] { 3, 3, 0 },     // the strings carry it
                Turn = TurnKit.Beethoven,
                TurnPassiveName = "THE CLOSER THE LOUDER",
                TurnPassiveText = "The less life the band has left, the harder it hits, up to 50 percent.",
                TurnSignatureName = "LIGHTNING",
                TurnSignatureText = "The 4/4 shape : strikes every enemy and throws it OFF-BEAT. Better strokes, bigger strike.",
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
                MechanicText = "Players from different cultures hit harder together, 5 percent per extra culture.",
                SignatureMark = "Alla rustica",
                SignatureName = "VILLAGE BAND",
                SignatureText = "4 strokes : every stroke on time gives 3 breath back.",
                SignatureCall = "* VILLAGE BAND! Four strokes, every stroke on time gives breath back.",
                Recipe = new int[] { 3, 3, 3 },     // the pipes of every road. Round 14 : 2 3 2 -> 3 3 3, VILLAGE BAND brings the
                                                    // heavy percussion in at a middle stroke's price and came too often
                Move = SignatureMove.VillageBand,
                Stamina = 5, PushPower = 5, Perk = ConductorPerk.EveryRoadHome,      // round 12.2 : push 6 -> 5, he won the most
                                                                                     // runs (77 to 82 percent, the others 67 to 75)
                                                                                     // round 14 : stamina 6 -> 5, VILLAGE BAND brings the
                                                                                     // percussion (f) in for a middle stroke's price
                ThemeColor = Palette.ToneE,
                Unlocked = true
            },

            //Conductor 6 : Alpha 7, the second of the six from the lore (W. A. Mozart). His Hollow has no
            //right arm and no shadow, his baton is a feather (USER_BRIEFS 3). The name and the lines are
            //placeholders until the story is written. No art yet : the painting is an empty frame.
            new Conductor
            {
                Name = "THE SHADOWLESS",
                BasedOn = "W. A. MOZART",
                Role = "TEMPO",
                Epithet = "He wrote as fast as other people breathe.",
                Story = "A prodigy who never stopped writing. Something took his right hand and his shadow. "
                      + "He conducts with a feather now, and the band moves twice.",
                MechanicName = "PRODIGY",
                MechanicText = "3 PERFECTs in a row give 1 breath back.",
                SignatureMark = "Presto",
                SignatureName = "FEATHER",
                SignatureText = "The 4/4 shape : two free ATTACKs at the end of the bar per stroke that lands.",
                SignatureCall = "* FEATHER! Everybody moves again.",
                Recipe = new int[] { 2, 2, 2 },
                Stamina = 5, PushPower = 5, Perk = ConductorPerk.None, Move = SignatureMove.AbsolutePitch,   // the old duel plays him plain
                Turn = TurnKit.Mozart,
                TurnRole = "SPEED",
                TurnPassiveName = "PRODIGY",
                TurnPassiveText = "3 PERFECTs in a row give 1 BREATH back.",
                TurnSignatureName = "FEATHER",
                TurnSignatureText = "The 4/4 shape : two free ATTACKs at the end of the bar per stroke that lands.",
                ThemeColor = Palette.ToneB,
                Unlocked = true
            },

            //Conductors 7 to 9 : Alpha 7 step 10, the rest of the six from the lore (USER_BRIEFS 3). Like THE
            //SHADOWLESS the names and lines are placeholders until the story is written, and there is no art yet.
            //The old duel plays them plain (no perk, ABSOLUTE PITCH).

            //Conductor 7 : Niccolo Paganini. His Hollow has no chest, only a hole, and leans toward a devil.
            new Conductor
            {
                Name = "THE HEARTLESS",
                BasedOn = "NICCOLO PAGANINI",
                Role = "GAMBLE",
                Epithet = "People swore he had made a deal with the devil.",
                Story = "A violinist so fast that people said the devil played for him. "
                      + "Something took his chest and left a hole where the heart was. He still plays like he has nothing to lose.",
                MechanicName = "BORROWED BREATH",
                MechanicText = "A skill short of BREATH can still be played. The rest is paid in life.",
                SignatureMark = "Diabolico",
                SignatureName = "CAPRICE",
                SignatureText = "The 4/4 shape : every swipe goes one better for a while. Then the band pays for it.",
                SignatureCall = "* CAPRICE! Everything goes one better, for now.",
                Recipe = new int[] { 2, 2, 2 },
                Stamina = 5, PushPower = 5, Perk = ConductorPerk.None, Move = SignatureMove.AbsolutePitch,   // the old duel plays him plain
                Turn = TurnKit.Paganini,
                TurnPassiveName = "BORROWED BREATH",
                TurnPassiveText = "A skill short of BREATH can still be played. Each missing BREATH costs " + BattleRules.TurnPaganiniLife + " life.",
                TurnSignatureName = "CAPRICE",
                TurnSignatureText = "The 4/4 shape : for two turns per stroke that lands, a MISS is GOOD and a GOOD is PERFECT. Then every musician pays " + (int)System.Math.Round(BattleRules.TurnCapricePrice * 100) + " percent of their life.",
                ThemeColor = Palette.ToneA,
                Unlocked = true
            },

            //Conductor 8 : Jean-Baptiste Lully. His Hollow has no legs and leans on a staff, his baton is a sun.
            new Conductor
            {
                Name = "THE KING'S BATON",
                BasedOn = "J. B. LULLY",
                Role = "DEFENCE",
                Epithet = "He kept time for a king, and the staff killed him.",
                Story = "Music master to the Sun King. He beat time with a heavy staff, struck his own foot with it, "
                      + "and the wound killed him. His Hollow has no legs and leans on that staff.",
                MechanicName = "STEADY HAND",
                MechanicText = "Blocking TACET's notes is easier : both windows are wider.",
                SignatureMark = "Maestoso",
                SignatureName = "THE SUN",
                SignatureText = "The 4/4 shape : life back for every musician, and DISSONANCE and MUTE are gone.",
                SignatureCall = "* THE SUN! The band stands up again.",
                Recipe = new int[] { 2, 2, 2 },
                Stamina = 5, PushPower = 5, Perk = ConductorPerk.None, Move = SignatureMove.AbsolutePitch,   // the old duel plays him plain
                Turn = TurnKit.Lully,
                TurnPassiveName = "STEADY HAND",
                TurnPassiveText = "Blocking TACET's notes is easier : both windows are wider.",
                TurnSignatureName = "THE SUN",
                TurnSignatureText = "The 4/4 shape : life back for every musician, and DISSONANCE and MUTE are gone. Better strokes, more life.",
                ThemeColor = Palette.ToneD,
                Unlocked = true
            },

            //Conductor 9 : Fanny Mendelssohn with the shadow of Felix. Her Hollow has no name, two people in one.
            new Conductor
            {
                Name = "THE UNSIGNED",
                BasedOn = "FANNY MENDELSSOHN",
                Role = "TWO VOICES",
                Epithet = "Some of her songs were printed under her brother's name.",
                Story = "She wrote hundreds of pieces and was told to keep them at home. "
                      + "Her Hollow has no name of its own, and her brother's shadow plays beside her.",
                MechanicName = "TWO VOICES",
                MechanicText = "The voices take turns : FANNY hits harder, FELIX heals more.",
                SignatureMark = "A due",
                SignatureName = "DUET",
                SignatureText = "The 4/4 shape : FANNY strikes every enemy while FELIX heals every musician.",
                SignatureCall = "* DUET! Two voices at once.",
                Recipe = new int[] { 2, 2, 2 },
                Stamina = 5, PushPower = 5, Perk = ConductorPerk.None, Move = SignatureMove.AbsolutePitch,   // the old duel plays her plain
                Turn = TurnKit.Fanny,
                TurnPassiveName = "TWO VOICES",
                TurnPassiveText = "The voices take turns, one musician each : FANNY hits 25 percent harder, FELIX heals 50 percent more.",
                TurnSignatureName = "DUET",
                TurnSignatureText = "The 4/4 shape : FANNY strikes every enemy while FELIX heals every musician.",
                ThemeColor = Palette.ToneC,
                Unlocked = true
            },
        };

        //Shown : the conductors the gallery offers. The old duel shows all of them. The turn battle shows
        //only the six of the lore, the ones with a PASSIVE and SIGNATURE (the user chose this, 19 Oct),
        //Beethoven first. The others stay in All so the save file still finds them.
        public static Conductor[] Shown = BattleRules.TurnBattleOn
            ? new Conductor[] { All[3], All[5], All[1], All[6], All[7], All[8] }
            : All;

        //Text Prepare : called once from LoadContent after the font exists.
        //Everything the screens need to print is built here so drawing stays free of
        //string work. If you change any text above, it gets picked up on the next launch.
        public static void PrepareText(SpriteFont font, float wrapWidth, float scale)
        {
            for (int i = 0; i < All.Length; i++)
            {
                Conductor c = All[i];

                //Turn Battle : the kit's words replace the duel's on every page
                if (BattleRules.TurnBattleOn && c.Turn != TurnKit.None)
                {
                    c.MechanicName = c.TurnPassiveName;
                    c.MechanicText = c.TurnPassiveText;
                    c.SignatureName = c.TurnSignatureName;
                    c.SignatureText = c.TurnSignatureText;
                    if (c.TurnRole != "") c.Role = c.TurnRole;
                }

                c.BasedOnLabel = "BASED ON " + c.BasedOn;
                c.StoryWrapped = Gfx.WrapText(font, c.Story, wrapWidth, scale);
                c.MechanicWrapped = Gfx.WrapText(font, c.MechanicText, wrapWidth, scale);
                c.SignatureWrapped = Gfx.WrapText(font, c.SignatureText, wrapWidth, scale);
                c.SkillsLabel = c.MechanicName + "   /   SPACE  " + c.SignatureName;
                int shownAt = Array.IndexOf(Shown, c);                              // the number the gallery shows
                c.IndexLabel = "No." + ((shownAt >= 0 ? shownAt : i) + 1).ToString("00");
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
