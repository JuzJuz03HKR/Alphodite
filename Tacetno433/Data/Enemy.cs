using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Core;

namespace Tacetno433.Data
{
    public enum EnemyKind { Normal, Elite, Boss }

    //EnemyTrait : the one rule a shape of TACET bends. BattleState and the duel check these by
    //name. A trait is always told to the player BEFORE it matters: the first banner of the duel
    //explains it the first time it is met (round 14), its name sits on the enemy's plate, and the
    //story box says it when the fight opens.
    //Round 12 : SILENT MOUTHS and UNFINISHED also play with the band. Round 15 : SILENT MOUTHS
    //silences a section of the orchestra and UNFINISHED swaps the left and right sides.
    public enum EnemyTrait
    {
        None,
        EchoFades,       // HUSH         the f / mf / p marks on its notes fade before they arrive
        FillsGaps,       // DEAD AIR     a note missed or let pass lands 50 percent harder
        Lullaby,         // THE LULL     round three slows down instead of speeding up
        FalseNotes,      // STATIC       one note in every bar shows the wrong loudness
        SilentMouths,    // MUTE CHOIR   from round two it silences one section of your band, and hides some notes
        NoRest,          // WHITE NOISE  silent beats give back only half the stamina
        Unfinished,      // REQUIEM      round three plays its part backwards, faster, and swaps your left and right sides
        Mirror,          // NAMELESS     from round two it plays YOUR last round back at you
        Bargain,         // DEVIL'S STRING  before round two it offers a deal

        //ONE BAR (Alpha 7 step 13) : turn battle only, the old rhythm duel plays these enemies plainly
        HoldsTheRoom,    // ROOM TONE    while it stands, the other enemies take less
        OnCue,           // THE CUE      it strikes the musician called in last
        FeltHand,        // THE DAMPER   while it stands, a CALL costs more
        TwiceABar,       // STUTTER      it cuts in twice a bar, one note each time
        FirstInLine,     // THE ROUND    it strikes the musician who plays first in the bar

        //Bosses of step 14 (option B of docs/ALPHA7_CONTENT_PROPOSALS.md, the Hollow of Bach, Lully and Beethoven)
        ByTouch,         // THE BLIND CANTOR  it strikes the seats in turn, a note you miss comes back in its next attack
        TheCourt,        // THE STAFF         half damage while its court stands, every third attack hits everyone
        Decree           // THE DEAF CROWN    each bar it forbids SKILLS or ATTACKS, and it never takes a COUNTER
    }

    //Enemy : one shape TACET takes. It has no face, only a written part it plays against you.
    public class Enemy
    {
        public string Name = "";
        public string Title = "";               // short line under the name
        public EnemyKind Kind = EnemyKind.Normal;
        public int Era = -1;                    // -1 means it can turn up in any era

        //Enemy Pattern : power on each of the 8 beats, 0 means silent on that beat.
        //These are floor 1 numbers, deeper floors scale them up automatically.
        public int[] Pattern = new int[BattleRules.BeatsPerRound];

        //Rotate : each new round the pattern slides this many beats to the left,
        //so round two is not a copy of round one
        public int RotatePerRound = 3;

        //Hidden : beat numbers (0 to 7) shown as ??? until the clash
        public int[] Hidden = new int[0];

        //SONG (round 15) : a real piece this enemy plays instead of its pattern, or null. Its notes,
        //rests and marks come from the song (see Data/SongChart.cs). The pattern still sets how hard
        //each note hits : the average of its notes (SongNotePower).
        public SongChart Song;

        public int SongNotePower()
        {
            int total = 0;
            int notes = 0;
            for (int b = 0; b < Pattern.Length; b++)
                if (Pattern[b] > 0) { total += Pattern[b]; notes++; }
            return notes > 0 ? (int)System.Math.Round(total / (float)notes) : 1;
        }

        //Temper : above 0 it boosts more often, below 0 it eases more often (percent)
        public int Temper = 0;

        //Trait : see EnemyTrait. TraitFloor is the first floor it switches on, so ordinary
        //enemies play plainly on floor 1 while the player learns, and show their trick later.
        public EnemyTrait Trait = EnemyTrait.None;
        public string TraitName = "";
        public string TraitText = "";
        public int TraitFloor = 1;

        //Turn Battle (Alpha 7, step 8) : life, speed and the PHRASES it attacks with. A phrase is
        //written as notes like "Lp Rmf Df" : the way to swipe back (L R U D) and the size (p mf f).
        //It plays its phrases in turn, so a player can learn them. A deeper floor makes the phrase
        //longer or shorter to fit BattleRules.TurnNotesMin / Max (it repeats from the start).
        //TurnTraitText is what its trait does in the turn battle (the trait is checked in TurnBattle by name).
        public int TurnHp;
        public int TurnSpeed;
        public int TurnPower;                   // how hard each note hits before the floor scale, 0 = the average of Pattern (SongNotePower)
        public string[] Phrases = new string[0];
        public string TurnTraitText = "";
        public string TurnTraitLine = "";     // "HUSH  ECHO FADES : ...", prepared, for the battle page

        //Enemy Look : brightness stand-in until the real figure goes in
        public Color Tone = Palette.ToneC;

        //Prepared Text
        public string KindLabel = "";
        public string TraitWrapped = "";
        public string TraitStory = "";        // the story box line, "* HUSH : ..."
    }

    //EnemyList : THE PLACE TO EDIT ENEMIES.
    public static class EnemyList
    {
        public static Enemy[] All = new Enemy[]
        {
            //Normal : any era. Round 11 : HUSH and THE LULL were given heavier parts, they were
            //far weaker than the other two and never threatened anyone.
            //Round 15 : the roadside enemies play ODE TO JOY for now (the first song). In the month
            //after the 29 Sep hand in, each should get a piece of its own.
            new Enemy { Name = "HUSH",
                        TurnHp = 50, TurnSpeed = 4, Phrases = new[] { "Dp Up", "Lp Rp Dp" }, TurnTraitText = "Each note that gets through adds 1 DISSONANCE.",      Title = "The first thing to go is the echo.",
                        Trait = EnemyTrait.EchoFades, TraitName = "ECHO FADES", TraitFloor = 2,
                        TraitText = "The f, mf and p marks on its notes fade before they reach you. Remember them.",
                        Kind = EnemyKind.Normal, Song = SongList.Twinkle, Pattern = new int[] { 4, 0, 3, 4, 0, 0, 5, 0 }, Tone = Palette.ToneD, Temper = 5 },
            new Enemy { Name = "DEAD AIR",
                        TurnHp = 50, TurnSpeed = 4, Phrases = new[] { "Lmf Rmf", "Dmf Dmf Umf" }, TurnTraitText = "A note you miss hits 50 percent harder.",  Title = "It waits between your notes.",
                        Trait = EnemyTrait.FillsGaps, TraitName = "FILLS THE GAPS", TraitFloor = 2,
                        TraitText = "A note you miss or let pass hits 50 percent harder.",
                        Kind = EnemyKind.Normal, Song = SongList.OdeToJoy, Pattern = new int[] { 0, 4, 0, 4, 0, 4, 0, 5 }, Tone = Palette.ToneE },
            new Enemy { Name = "THE LULL",
                        TurnHp = 60, TurnSpeed = 2, Phrases = new[] { "Dp Dp", "Up Dp Up" }, TurnTraitText = "Every third turn it hums instead of attacking and gets back 12 life.",  Title = "Soft, patient, and never finished.",
                        Trait = EnemyTrait.Lullaby, TraitName = "LULLABY", TraitFloor = 2,
                        TraitText = "Round three slows down instead of speeding up.",
                        Kind = EnemyKind.Normal, Song = SongList.Moonlight, Pattern = new int[] { 3, 3, 0, 0, 7, 0, 3, 3 }, Tone = Palette.ToneC, Temper = -10 },
            new Enemy { Name = "STATIC",
                        TurnHp = 45, TurnSpeed = 5, Phrases = new[] { "Lf Rp", "Umf Df Lp" }, TurnTraitText = "One note of each attack shows the wrong size.",    Title = "It sounds like something. It is not.",
                        Trait = EnemyTrait.FalseNotes, TraitName = "FALSE NOTES", TraitFloor = 2,
                        TraitText = "One note in every bar shows the wrong loudness.",
                        Kind = EnemyKind.Normal, Song = SongList.OdeToJoy, Pattern = new int[] { 4, 0, 3, 0, 4, 0, 3, 6 }, Tone = Palette.ToneB, Temper = 10 },

            //Elite : any era
            new Enemy { Name = "THE MUTE CHOIR",
                        TurnHp = 120, TurnSpeed = 4, Phrases = new[] { "Df Df Uf", "Lmf Rmf Lmf Rmf" }, TurnTraitText = "A note that gets through takes the target's skills away for a turn.", Title = "A hundred mouths, open, making nothing.",
                        Trait = EnemyTrait.SilentMouths, TraitName = "SILENT MOUTHS", TraitFloor = 1,
                        TraitText = "From round two it silences one section of your band each round, strings, winds or percussion: those players cannot play. Some notes stay hidden as ???",
                        Kind = EnemyKind.Elite, Song = SongList.FifthSymphony, Pattern = new int[] { 4, 0, 5, 0, 6, 0, 4, 6 }, Hidden = new int[] { 3, 7 }, Tone = Palette.ToneB },
                        // round 15, was 5 0 5 0 6 0 5 7 : with the whole band on every note, losing a section hurt average players most
                        // (79 percent of them lost to it on floor 3). Round 14, was 5 0 5 0 7 0 5 8
            //WHITE NOISE : round 11, quieter notes (3 -> 2) and NO REST gives back half instead of
            //nothing. It used to end more runs on floor one than any boss.
            //Round 12 : a hiss, mostly p (Temper 15 -> -60) and nothing hidden. A note on every beat
            //means a stroke paid on every beat, so its notes are soft enough to answer small.
            //Round 14 : notes 2 -> 3, Temper -60 -> -45. The winds (p) answer soft notes cheaply now,
            //and it had stopped being a threat (a strong player lost to it 3 percent of the time, was 16).
            //Round 15 : notes 5, Temper 0. Playing is free, so soft notes on every beat were nothing,
            //now it is a reading test : a note on every beat, any mark, and no rest at all.
            new Enemy { Name = "WHITE NOISE",
                        TurnHp = 110, TurnSpeed = 6, Phrases = new[] { "Lp Rp Lp Rp", "Up Dp Up Dp" }, TurnTraitText = "One note more in every attack, and parrying it gives no BREATH.",    Title = "Every beat, all the time, forever.",
                        Trait = EnemyTrait.NoRest, TraitName = "NO REST", TraitFloor = 1,
                        TraitText = "It never stops, so silent beats give back only half the breath.",
                        Kind = EnemyKind.Elite, Song = SongList.PreludeInC, Pattern = new int[] { 5, 5, 5, 5, 5, 5, 5, 5 }, Tone = Palette.ToneA, Temper = 0 },

            //Boss : one per era
            new Enemy { Name = "REQUIEM",
                        TurnHp = 250, TurnSpeed = 5, Phrases = new[] { "Df Lmf Rmf Uf", "Lf Rf Lf Rf" }, TurnTraitText = "Below half life it swaps left and right and plays faster.",            Title = "The piece that was never finished.",
                        Trait = EnemyTrait.Unfinished, TraitName = "UNFINISHED", TraitFloor = 1,
                        TraitText = "In round three it plays backwards and faster, and your band is mirrored: the players on the left and right swap sides.",
                        Kind = EnemyKind.Boss, Era = 0, Song = SongList.RondoAllaTurca, Pattern = new int[] { 6, 0, 4, 0, 8, 0, 4, 9 }, Hidden = new int[] { 2, 6 }, Tone = Palette.ToneA },
            new Enemy { Name = "THE NAMELESS MASTER",
                        TurnHp = 230, TurnSpeed = 4, Phrases = new[] { "Dmf Umf Lmf", "Rf Lf Dp Up" }, TurnTraitText = "It answers with the notes of the last skill your band played.", Title = "He plays a phrase. You must answer better.",
                        Trait = EnemyTrait.Mirror, TraitName = "ANSWER BETTER", TraitFloor = 1,
                        TraitText = "From round two it plays your last round back at you.",
                        Kind = EnemyKind.Boss, Era = 1, Song = SongList.NachtMusik, Pattern = new int[] { 3, 0, 3, 5, 0, 3, 2, 7 }, Hidden = new int[] { 3, 7 }, Tone = Palette.ToneB },
                        // round 12.1, was 3 3 0 5 0 3 0 8 : a strong player lost to it on floor one 27 percent of the time,
                        // twice the hardest elite. Now about 15 percent, still the hardest boss. Round 12 : was 4 4 0 6 4 4 0 9
            new Enemy { Name = "THE DEVIL'S STRING",
                        TurnHp = 260, TurnSpeed = 6, Phrases = new[] { "Rf Rf Lf", "Uf Df Uf Df" }, TurnTraitText = "Each of its attacks steals 1 BREATH.",  Title = "One string, one bow, one bargain.",
                        Trait = EnemyTrait.Bargain, TraitName = "THE BARGAIN", TraitFloor = 1,
                        TraitText = "Before round two it offers a deal: more power now, less breath for ever.",
                        Kind = EnemyKind.Boss, Era = 2, Pattern = new int[] { 7, 0, 7, 0, 0, 9, 0, 9 }, Hidden = new int[] { 4 }, Tone = Palette.ToneA, Temper = 20 },

            //ONE BAR (Alpha 7 step 13, docs/ALPHA7_CONTENT_PROPOSALS.md) : any era, added at the END because the
            //save file and ArtBank keep enemies by their place in this list. Each one is a puzzle for one part of
            //ONE BAR : who to hit first (ROOM TONE), when to CALL (THE CUE, THE DAMPER) and the order of the bar
            //(STUTTER, THE ROUND). TurnPower 0 : each note hits the average of the Pattern, like the others.
            new Enemy { Name = "ROOM TONE",
                        TurnHp = 45, TurnSpeed = 2, Phrases = new[] { "Dp", "Up Dp" }, TurnTraitText = "While it stands, the other enemies take 30 percent less.", Title = "The sound of an empty room, holding the others up.",
                        Trait = EnemyTrait.HoldsTheRoom, TraitName = "HOLDS THE ROOM", TraitFloor = 2,
                        TraitText = "While it stands, the other enemies take 30 percent less.",
                        Kind = EnemyKind.Normal, Pattern = new int[] { 4, 0, 4, 0, 4, 0, 4, 0 }, Tone = Palette.ToneE },
            new Enemy { Name = "THE CUE",
                        TurnHp = 55, TurnSpeed = 5, Phrases = new[] { "Rmf Lmf", "Uf" }, TurnTraitText = "It always strikes the musician who was CALLED in last.", Title = "It waits in the wings for whoever walks on next.",
                        Trait = EnemyTrait.OnCue, TraitName = "ON CUE", TraitFloor = 2,
                        TraitText = "It always strikes the musician who was CALLED in last.",
                        Kind = EnemyKind.Normal, Pattern = new int[] { 4, 0, 4, 0, 4, 0, 4, 0 }, Tone = Palette.ToneC },
            new Enemy { Name = "THE DAMPER",
                        TurnHp = 42, TurnSpeed = 3, Phrases = new[] { "Dmf Dp", "Lmf Rmf" }, TurnTraitText = "While it stands, a CALL costs 1 more BREATH.", Title = "A felt hand resting on every string.",
                        Trait = EnemyTrait.FeltHand, TraitName = "FELT HAND", TraitFloor = 2,
                        TraitText = "While it stands, a CALL costs 1 more BREATH.",
                        Kind = EnemyKind.Normal, Pattern = new int[] { 4, 0, 4, 0, 4, 0, 4, 0 }, Tone = Palette.ToneD },
            new Enemy { Name = "STUTTER",
                        TurnHp = 36, TurnSpeed = 7, Phrases = new[] { "Lp", "Rp", "Up" }, TurnTraitText = "It cuts in twice a bar, after the 1st and the 3rd move, one note each time.", Title = "It cannot finish a word, so it never stops starting.",
                        Trait = EnemyTrait.TwiceABar, TraitName = "TWICE A BAR", TraitFloor = 2,
                        TraitText = "It cuts in twice a bar, after the 1st and the 3rd move, one note each time.",
                        Kind = EnemyKind.Normal, Pattern = new int[] { 4, 4, 4, 4, 4, 4, 4, 4 }, Tone = Palette.ToneB },
            new Enemy { Name = "THE ROUND",
                        TurnHp = 115, TurnSpeed = 4, Phrases = new[] { "Df Dmf Df", "Lmf Rmf Lf Rf" }, TurnTraitText = "It always strikes the musician who plays first in the bar.", Title = "It sings your bar back at you, in your own order.",
                        Trait = EnemyTrait.FirstInLine, TraitName = "FIRST IN LINE", TraitFloor = 1,
                        TraitText = "It always strikes the musician who plays first in the bar.",
                        Kind = EnemyKind.Elite, Pattern = new int[] { 5, 0, 5, 0, 5, 0, 5, 5 }, Tone = Palette.ToneB },

            //Bosses of step 14 (option B : no new era, each era picks one of its two bosses at random). The Hollow
            //of three conductors from the lore (docs/USER_BRIEFS.md) : Bach with no eyes, Lully with no legs and a cane,
            //Beethoven with no ears and a crown of black thorns. The names are stand-ins until the story is written.
            new Enemy { Name = "THE DEAF CROWN",
                        TurnHp = 240, TurnSpeed = 5, TurnPower = 6, Phrases = new[] { "Uf Uf Uf Df", "Lf Rf Df" }, TurnTraitText = "Each bar it forbids SKILLS or ATTACKS, and breaking it hurts. It never takes a COUNTER.", Title = "It cannot hear you. It only gives orders.",
                        Trait = EnemyTrait.Decree, TraitName = "DECREE", TraitFloor = 1,
                        TraitText = "Each bar it forbids SKILLS or ATTACKS, and breaking it hurts. It never takes a COUNTER.",
                        Kind = EnemyKind.Boss, Era = 0, Pattern = new int[] { 6, 0, 6, 0, 6, 0, 6, 9 }, Tone = Palette.ToneA },
            new Enemy { Name = "THE STAFF",
                        TurnHp = 250, TurnSpeed = 3, TurnPower = 7, Phrases = new[] { "Df Dmf Df", "Dmf Lmf Dmf Rmf" }, TurnTraitText = "While its court stands it takes half. Every third attack it strikes the floor and hits everyone.", Title = "It cannot walk. It beats time on the floor instead.",
                        Trait = EnemyTrait.TheCourt, TraitName = "THE COURT", TraitFloor = 1,
                        TraitText = "While its court stands it takes half. Every third attack it strikes the floor and hits everyone.",
                        Kind = EnemyKind.Boss, Era = 1, Pattern = new int[] { 6, 0, 6, 0, 6, 0, 6, 6 }, Tone = Palette.ToneB },
            new Enemy { Name = "THE BLIND CANTOR",
                        TurnHp = 240, TurnSpeed = 4, TurnPower = 6, Phrases = new[] { "Df Umf Df Umf", "Lmf Rmf Lp Rp" }, TurnTraitText = "It strikes the seats in turn, front to back. A note you miss comes back in its next attack.", Title = "It plays by touch, one voice after another.",
                        Trait = EnemyTrait.ByTouch, TraitName = "BY TOUCH", TraitFloor = 1,
                        TraitText = "It strikes the seats in turn, front to back. A note you miss comes back in its next attack.",
                        Kind = EnemyKind.Boss, Era = 2, Pattern = new int[] { 6, 0, 6, 0, 6, 0, 6, 6 }, Tone = Palette.ToneA },
        };

        //Practice : the enemy of the tutorial's practice fight (Alpha 7, step 9, BattleRules PRACTICE).
        //It is not in All, so it never turns up in a run and the save file never sees it. No trait.
        public static Enemy Practice = new Enemy { Name = "PRACTICE",
                        TurnHp = BattleRules.TurnPracticeHp, TurnSpeed = BattleRules.TurnPracticeSpeed, Phrases = new[] { "Dp", "Lmf Rmf" },
                        Kind = EnemyKind.Normal, Pattern = new int[] { BattleRules.TurnPracticePower } };

        private static string[] kindNames = { "ENCOUNTER", "ELITE", "BOSS" };

        //Text Prepare : called once from LoadContent
        public static void PrepareText(SpriteFont storyFont, float wrapWidth)
        {
            for (int i = 0; i < All.Length; i++)
            {
                Enemy e = All[i];
                e.KindLabel = kindNames[(int)e.Kind];
                e.TraitWrapped = Gfx.WrapText(storyFont, e.TraitText, wrapWidth, TextSize.StorySmall);
                e.TraitStory = "* " + e.Name + " : " + e.TraitText;
                e.TurnTraitLine = e.TraitName + " : " + e.TurnTraitText;
            }
        }

        //Pick Other : like Pick, but someone not in the fight yet while there is anyone left (PACKS, Alpha 7 step 12)
        public static Enemy PickOther(EnemyKind kind, int era, List<Enemy> already, Random random)
        {
            for (int tries = 0; tries < 20; tries++)
            {
                Enemy e = Pick(kind, era, random);
                if (!already.Contains(e)) return e;
            }
            return Pick(kind, era, random);
        }

        //Enemy Pick : a random enemy of this kind that fits the era.
        //Bosses prefer their own era, and fall back to any boss if none is written for it.
        public static Enemy Pick(EnemyKind kind, int era, Random random)
        {
            List<Enemy> pool = new List<Enemy>();

            for (int i = 0; i < All.Length; i++)
            {
                Enemy e = All[i];
                if (e.Kind != kind) continue;
                if (e.Era == -1 || e.Era == era) pool.Add(e);
            }

            //Fallback : nothing fits the era, take any enemy of the right kind
            if (pool.Count == 0)
                for (int i = 0; i < All.Length; i++)
                    if (All[i].Kind == kind) pool.Add(All[i]);

            return pool[random.Next(pool.Count)];
        }
    }
}
