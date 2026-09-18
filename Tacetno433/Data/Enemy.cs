using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Tacetno433.Core;

namespace Tacetno433.Data
{
    public enum EnemyKind { Normal, Elite, Boss }

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

        //Enemy Look : brightness stand-in until the real figure goes in
        public Color Tone = Palette.ToneC;

        //Prepared Text
        public string KindLabel = "";
    }

    //EnemyList : THE PLACE TO EDIT ENEMIES.
    public static class EnemyList
    {
        public static Enemy[] All = new Enemy[]
        {
            //Normal : any era
            new Enemy { Name = "HUSH",      Title = "The first thing to go is the echo.",
                        Kind = EnemyKind.Normal, Pattern = new int[] { 3, 0, 0, 3, 0, 0, 4, 0 }, Tone = Palette.ToneD },
            new Enemy { Name = "DEAD AIR",  Title = "It waits between your notes.",
                        Kind = EnemyKind.Normal, Pattern = new int[] { 0, 4, 0, 4, 0, 4, 0, 5 }, Tone = Palette.ToneE },
            new Enemy { Name = "THE LULL",  Title = "Soft, patient, and never finished.",
                        Kind = EnemyKind.Normal, Pattern = new int[] { 2, 2, 0, 0, 5, 0, 2, 2 }, Tone = Palette.ToneC, Temper = -10 },
            new Enemy { Name = "STATIC",    Title = "It sounds like something. It is not.",
                        Kind = EnemyKind.Normal, Pattern = new int[] { 4, 0, 3, 0, 4, 0, 3, 6 }, Tone = Palette.ToneB, Temper = 10 },

            //Elite : any era
            new Enemy { Name = "THE MUTE CHOIR", Title = "A hundred mouths, open, making nothing.",
                        Kind = EnemyKind.Elite, Pattern = new int[] { 5, 0, 5, 0, 7, 0, 5, 8 }, Hidden = new int[] { 3, 7 }, Tone = Palette.ToneB },
            new Enemy { Name = "WHITE NOISE",    Title = "Every beat, all the time, forever.",
                        Kind = EnemyKind.Elite, Pattern = new int[] { 3, 3, 3, 3, 3, 3, 3, 3 }, Hidden = new int[] { 5 }, Tone = Palette.ToneA, Temper = 15 },

            //Boss : one per era
            new Enemy { Name = "REQUIEM",            Title = "The piece that was never finished.",
                        Kind = EnemyKind.Boss, Era = 0, Pattern = new int[] { 6, 0, 4, 0, 8, 0, 4, 9 }, Hidden = new int[] { 2, 6 }, Tone = Palette.ToneA },
            new Enemy { Name = "THE NAMELESS MASTER", Title = "He plays a phrase. You must answer better.",
                        Kind = EnemyKind.Boss, Era = 1, Pattern = new int[] { 4, 4, 0, 6, 4, 4, 0, 9 }, Hidden = new int[] { 3, 7 }, Tone = Palette.ToneB },
            new Enemy { Name = "THE DEVIL'S STRING",  Title = "One string, one bow, one bargain.",
                        Kind = EnemyKind.Boss, Era = 2, Pattern = new int[] { 7, 0, 7, 0, 0, 9, 0, 9 }, Hidden = new int[] { 4 }, Tone = Palette.ToneA, Temper = 20 },
        };

        private static string[] kindNames = { "ENCOUNTER", "ELITE", "BOSS" };

        //Text Prepare : called once from LoadContent
        public static void PrepareText()
        {
            for (int i = 0; i < All.Length; i++)
                All[i].KindLabel = kindNames[(int)All[i].Kind];
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
