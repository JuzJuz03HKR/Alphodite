using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //Hollow : the look of TACET, drawn in code. Studied from the black and white key art of
    //Limbus Company Canto 10 and its E.G.O "Hollow" : a black sun with a burning white rim,
    //glowing rings punched into the dark like holes, and hard streaks of light across a hit.
    //
    //   Eclipse   TACET's sun. A black disc that hides everything behind it, a white corona.
    //   Ring      one hole of silence, a thin glowing ring. TACET's notes are drawn as these.
    //   Streak    a sharp horizontal line of light, brightest in the middle
    //   Flare     a streak with a short cross and a hot spot, for a hit
    //
    //These are effects, not artwork: they stay when the pictures arrive.
    public static class Hollow
    {
        //Eclipse : turn is a slow angle so the bright crescent on the rim creeps round
        public static void Eclipse(SpriteBatch sb, float cx, float cy, float radius, float turn, float alpha)
        {
            //Haze : one wide soft light, most of it hidden behind the disc
            Gfx.DrawGlow(sb, cx, cy, radius * 2.8f, Palette.Paper * (0.45f * alpha));

            //Corona : rings of light fading outward from the rim, so the edge seems to burn
            int rings = 9;
            for (int i = rings; i >= 1; i--)
            {
                float k = i / (float)rings;
                float fade = (1f - k) * (1f - k);
                Gfx.CircleOutline(sb, cx, cy, radius + i * radius * 0.04f, Palette.Highlight * (0.35f * fade * alpha), radius * 0.05f);
            }

            //Disc : the silence itself, blacker than the dark around it
            Gfx.Circle(sb, cx, cy, radius, Color.Black * alpha);

            //Rim : a thin white line all the way round, and a thick crescent on one side
            Gfx.CircleOutline(sb, cx, cy, radius, Palette.Highlight * (0.95f * alpha), 2f);
            Gfx.Arc(sb, cx, cy, radius + 1f, turn - 1.1f, turn + 1.1f, Palette.Highlight * (0.8f * alpha), 3f);
            Gfx.Arc(sb, cx, cy, radius + 2f, turn - 0.5f, turn + 0.5f, Palette.Highlight * alpha, 5f);
        }

        //Ring : a hole in the air. Bright rim, a faint glow round it, darkness inside.
        public static void Ring(SpriteBatch sb, float cx, float cy, float radius, float thickness, float alpha)
        {
            Gfx.DrawGlow(sb, cx, cy, radius * 2.2f, Palette.Paper * (0.18f * alpha));
            Gfx.Circle(sb, cx, cy, radius, Color.Black * (0.55f * alpha));
            Gfx.CircleOutline(sb, cx, cy, radius, Palette.Highlight * alpha, thickness);
        }

        //Streak : a line of light through (cx, cy), length is the whole line
        public static void Streak(SpriteBatch sb, float cx, float cy, float length, float alpha)
        {
            float half = length / 2f;
            Gfx.DrawGlowBox(sb, new Rectangle((int)(cx - half), (int)cy - 10, (int)length, 20), Palette.Paper * (0.35f * alpha));
            Gfx.Rect(sb, cx - half, cy - 1, length, 2, Palette.Highlight * (0.9f * alpha));
            Gfx.Rect(sb, cx - half * 0.4f, cy - 2, length * 0.4f, 4, Palette.Highlight * alpha);
        }

        //Flare : a streak with a much shorter vertical line and a hot spot where the two meet
        public static void Flare(SpriteBatch sb, float cx, float cy, float length, float alpha)
        {
            Streak(sb, cx, cy, length, alpha);
            float up = length * 0.12f;
            Gfx.Rect(sb, cx - 1, cy - up, 2, up * 2f, Palette.Highlight * (0.8f * alpha));
            Gfx.DrawGlow(sb, cx, cy, 26f + length * 0.04f, Palette.Highlight * (0.8f * alpha));
        }
    }
}
