using System;
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //TacetField : the enemy of the game drawn as a picture.
    //A flat black area creeping in from the right, its border is a white waveform that never stops moving.
    //Used on the title page and later on the duel page.
    public static class TacetField
    {
        //TACET Draw : edgeX = where the darkness starts, intensity 0..1 = how close we are to losing
        public static void Draw(SpriteBatch sb, float edgeX, float time, float intensity)
        {
            //The closer to losing, the tighter and faster the wave gets
            float amplitude = 8f + intensity * 20f;
            float speed = 1.2f + intensity * 3f;
            float tightness = 0.030f + intensity * 0.02f;

            //Draw the field two pixel rows at a time so the wave stays cheap to draw
            for (int y = 0; y < TacetGame.ScreenH; y += 2)
            {
                float wave1 = (float)Math.Sin(y * tightness + time * speed) * amplitude;
                float wave2 = (float)Math.Sin(y * 0.011f - time * speed * 0.7f) * amplitude * 0.8f;
                float x = edgeX + wave1 + wave2;

                Gfx.Rect(sb, x, y, TacetGame.ScreenW - x, 2, Palette.Void);   // the silence itself
                Gfx.Rect(sb, x - 3, y, 3, 2, Palette.Paper);                  // white edge line
            }
        }
    }
}
