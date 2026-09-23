using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //TacetField : the enemy of the game drawn as a picture.
    //A flat black area creeping in from the right. Its border is a RIFT : a torn seam of white
    //light, like a crack in the world with something blinding behind it. The seam never stops
    //moving, frays into streaks of light on the dark side, and throws off shards.
    //Used on the title page, the duel, the result pages and the settings page.
    //
    //Everything is drawn two pixel rows at a time from plain rectangles, so it stays cheap.
    public static class TacetField
    {
        //TACET Draw : edgeX = where the darkness starts, intensity 0..1 = how close we are to losing
        public static void Draw(SpriteBatch sb, float edgeX, float time, float intensity)
        {
            Draw(sb, edgeX, time, intensity, 0f, 0f);
        }

        //TACET Draw Rippled : the same rift, but shuddering around rippleY.
        //The duel uses this when a wave of sound lands on the border.
        public static void Draw(SpriteBatch sb, float edgeX, float time, float intensity, float ripple, float rippleY)
        {
            //Flicker Step : the torn edge redraws itself about fourteen times a second
            int step = (int)(time * 14f);
            float blend = time * 14f - step;

            for (int y = 0; y < TacetGame.ScreenH; y += 2)
            {
                float x = EdgeAt(y, edgeX, time, intensity, ripple, rippleY);
                int row = y / 2;

                //Tear : how far the white seam reaches into the dark on this row. Chunks of rows
                //share a value so the edge tears in slabs, and it drifts between two random steps.
                float tearA = Hash(row / 3, step) * 0.6f + Hash(row, step) * 0.4f;
                float tearB = Hash(row / 3, step + 1) * 0.6f + Hash(row, step + 1) * 0.4f;
                float tear = MathHelper.Lerp(tearA, tearB, blend);
                float seam = 4f + tear * (8f + intensity * 14f);

                //Silence : the black itself, starting where the seam ends
                Gfx.Rect(sb, x + seam, y, TacetGame.ScreenW - x - seam, 2, Palette.Void);

                //Light Leak : a feathered glow bleeding out of the seam into the dark
                float leak = 8f + tear * 18f + intensity * 8f;
                Gfx.Rect(sb, x + seam, y, leak, 2, Palette.Highlight * 0.42f);
                Gfx.Rect(sb, x + seam + leak, y, leak * 1.2f, 2, Palette.Paper * 0.14f);

                //Streak : now and then one row shoots a thin line of light far into the dark
                float streak = Hash(row, step / 3 + 7);
                if (streak > 0.86f)
                {
                    float reach = (streak - 0.86f) / 0.14f * (40f + intensity * 70f);
                    Gfx.Rect(sb, x + seam, y, reach, 1, Palette.Highlight * 0.6f);
                }

                //Seam : the crack itself, white hot, with a soft bloom on the light side
                Gfx.Rect(sb, x - 10f, y, 10f, 2, Palette.Highlight * 0.3f);
                Gfx.Rect(sb, x - 2f, y, seam + 2f, 2, Palette.Highlight);
            }

            //Flares : a few bright bursts along the seam that swell and fade in turn
            for (int i = 0; i < 5; i++)
            {
                float y = 70f + i * 150f + (float)Math.Sin(time * 0.7f + i * 2.1f) * 40f;
                float pulse = (float)Math.Sin(time * (1.3f + i * 0.4f) + i * 1.7f) * 0.5f + 0.5f;
                float x = EdgeAt(y, edgeX, time, intensity, ripple, rippleY);
                Gfx.DrawGlow(sb, x + 6f, y, 40f + pulse * (40f + intensity * 60f), Palette.Highlight * (0.25f + 0.35f * pulse));
            }

            //Shards : slivers of light breaking off the seam and drifting away, the same every time
            int shards = 6 + (int)(intensity * 10f);
            for (int i = 0; i < shards; i++)
            {
                float life = (time * (0.45f + (i % 4) * 0.12f) + i * 0.37f) % 1f;
                float y0 = Hash(i, 999) * TacetGame.ScreenH;
                float x0 = EdgeAt(y0, edgeX, time, intensity, ripple, rippleY);
                float side = i % 3 == 0 ? -1f : 1f;                  // most drift into the dark
                float x = x0 + side * life * (50f + (i % 5) * 18f);
                float y = y0 - life * 30f;
                float length = 6f + (i % 4) * 5f;
                Color c = Palette.Highlight * (1f - life);
                Gfx.Line(sb, x - length * 0.6f, y + length * 0.3f, x + length * 0.6f, y - length * 0.3f, c, 2f);
            }
        }

        //Edge At : where the seam sits on row y. Two slow waves, plus the ripple of a hit.
        private static float EdgeAt(float y, float edgeX, float time, float intensity, float ripple, float rippleY)
        {
            //The closer to losing, the tighter and faster the wave gets
            float amplitude = 8f + intensity * 20f;
            float speed = 1.2f + intensity * 3f;
            float tightness = 0.030f + intensity * 0.02f;

            float wave1 = (float)Math.Sin(y * tightness + time * speed) * amplitude;
            float wave2 = (float)Math.Sin(y * 0.011f - time * speed * 0.7f) * amplitude * 0.8f;
            float x = edgeX + wave1 + wave2;

            //Ripple : a shudder running away from the point the sound hit, dying out
            //the further up and down the line it gets
            if (ripple > 0f)
            {
                float distance = Math.Abs(y - rippleY);
                float fade = 1f / (1f + distance * 0.012f);
                x += (float)Math.Sin(distance * 0.06f - time * 26f) * ripple * 46f * fade;
            }
            return x;
        }

        //Hash : a steady "random" number from 0 to 1 for a pair of whole numbers.
        //The same pair always gives the same number, so nothing has to be stored.
        //ADVANCED PART (small) : the classic sine hash used in shaders. A big multiplier makes
        //the sine's tiny digits jump about, and keeping only the part after the point gives 0..1.
        private static float Hash(int a, int b)
        {
            float h = (float)Math.Sin(a * 12.9898f + b * 78.233f) * 43758.547f;
            return h - (float)Math.Floor(h);
        }
    }
}
