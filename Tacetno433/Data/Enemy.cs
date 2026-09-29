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
    //Round 12 : SILENT MOUTHS and UNFINISHED also play with the band. Round 14 : with the parts
    //written on the players, SILENT MOUTHS silences a letter (p, mf or f) and UNFINISHED swaps
    //the left and right arrows.
    public enum EnemyTrait
    {
        None,
        EchoFades,       // HUSH         the f / mf / p marks on its notes fade before they arrive
        FillsGaps,       // DEAD AIR     a small stroke or none lets its note in 50 percent harder
        Lullaby,         // THE LULL     round three slows down instead of speeding up
        FalseNotes,      // STATIC       one note in every bar shows the wrong loudness
        SilentMouths,    // MUTE CHOIR   from round two it silences one part of your band (p, mf or f), and hides some notes
        NoRest,          // WHITE NOISE  silent beats give back only half the stamina
        Unfinished,      // REQUIEM      round three plays its part backwards, faster, and swaps your left and right arrows
        Mirror,          // NAMELESS     from round two it plays YOUR last round back at you
        Bargain          // DEVIL'S STRING  before round two it offers a deal
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

        //Temper : above 0 it boosts more often, below 0 it eases more often (percent)
        public int Temper = 0;

        //Trait : see EnemyTrait. TraitFloor is the first floor it switches on, so ordinary
        //enemies play plainly on floor 1 while the player learns, and show their trick later.
        public EnemyTrait Trait = EnemyTrait.None;
        public string TraitName = "";
        public string TraitText = "";
        public int TraitFloor = 1;

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
            new Enemy { Name = "HUSH",      Title = "The first thing to go is the echo.",
                        Trait = EnemyTrait.EchoFades, TraitName = "ECHO FADES", TraitFloor = 2,
                        TraitText = "The f, mf and p marks on its notes fade before they reach you. Remember them.",
                        Kind = EnemyKind.Normal, Pattern = new int[] { 4, 0, 3, 4, 0, 0, 5, 0 }, Tone = Palette.ToneD, Temper = 5 },
            new Enemy { Name = "DEAD AIR",  Title = "It waits between your notes.",
                        Trait = EnemyTrait.FillsGaps, TraitName = "FILLS THE GAPS", TraitFloor = 2,
                        TraitText = "A note you miss or let pass hits 50 percent harder.",
                        Kind = EnemyKind.Normal, Pattern = new int[] { 0, 4, 0, 4, 0, 4, 0, 5 }, Tone = Palette.ToneE },
            new Enemy { Name = "THE LULL",  Title = "Soft, patient, and never finished.",
                        Trait = EnemyTrait.Lullaby, TraitName = "LULLABY", TraitFloor = 2,
                        TraitText = "Round three slows down instead of speeding up.",
                        Kind = EnemyKind.Normal, Pattern = new int[] { 3, 3, 0, 0, 7, 0, 3, 3 }, Tone = Palette.ToneC, Temper = -10 },
            new Enemy { Name = "STATIC",    Title = "It sounds like something. It is not.",
                        Trait = EnemyTrait.FalseNotes, TraitName = "FALSE NOTES", TraitFloor = 2,
                        TraitText = "One note in every bar shows the wrong loudness.",
                        Kind = EnemyKind.Normal, Pattern = new int[] { 4, 0, 3, 0, 4, 0, 3, 6 }, Tone = Palette.ToneB, Temper = 10 },

            //Elite : any era
            new Enemy { Name = "THE MUTE CHOIR", Title = "A hundred mouths, open, making nothing.",
                        Trait = EnemyTrait.SilentMouths, TraitName = "SILENT MOUTHS", TraitFloor = 1,
                        TraitText = "From round two it silences one section of your band each round, strings, winds or percussion: those players cannot play. Some notes stay hidden as ???",
                        Kind = EnemyKind.Elite, Pattern = new int[] { 4, 0, 5, 0, 6, 0, 4, 6 }, Hidden = new int[] { 3, 7 }, Tone = Palette.ToneB },
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
            new Enemy { Name = "WHITE NOISE",    Title = "Every beat, all the time, forever.",
                        Trait = EnemyTrait.NoRest, TraitName = "NO REST", TraitFloor = 1,
                        TraitText = "It never stops, so silent beats give back only half the stamina.",
                        Kind = EnemyKind.Elite, Pattern = new int[] { 5, 5, 5, 5, 5, 5, 5, 5 }, Tone = Palette.ToneA, Temper = 0 },

            //Boss : one per era
            new Enemy { Name = "REQUIEM",            Title = "The piece that was never finished.",
                        Trait = EnemyTrait.Unfinished, TraitName = "UNFINISHED", TraitFloor = 1,
                        TraitText = "In round three it plays backwards and faster, and your band is mirrored: the players on the left and right swap sides.",
                        Kind = EnemyKind.Boss, Era = 0, Pattern = new int[] { 6, 0, 4, 0, 8, 0, 4, 9 }, Hidden = new int[] { 2, 6 }, Tone = Palette.ToneA },
            new Enemy { Name = "THE NAMELESS MASTER", Title = "He plays a phrase. You must answer better.",
                        Trait = EnemyTrait.Mirror, TraitName = "ANSWER BETTER", TraitFloor = 1,
                        TraitText = "From round two it plays your last round back at you.",
                        Kind = EnemyKind.Boss, Era = 1, Pattern = new int[] { 3, 0, 3, 5, 0, 3, 2, 7 }, Hidden = new int[] { 3, 7 }, Tone = Palette.ToneB },
                        // round 12.1, was 3 3 0 5 0 3 0 8 : a strong player lost to it on floor one 27 percent of the time,
                        // twice the hardest elite. Now about 15 percent, still the hardest boss. Round 12 : was 4 4 0 6 4 4 0 9
            new Enemy { Name = "THE DEVIL'S STRING",  Title = "One string, one bow, one bargain.",
                        Trait = EnemyTrait.Bargain, TraitName = "THE BARGAIN", TraitFloor = 1,
                        TraitText = "Before round two it offers a deal: more power now, less stamina for ever.",
                        Kind = EnemyKind.Boss, Era = 2, Pattern = new int[] { 7, 0, 7, 0, 0, 9, 0, 9 }, Hidden = new int[] { 4 }, Tone = Palette.ToneA, Temper = 20 },
        };

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
            }
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
