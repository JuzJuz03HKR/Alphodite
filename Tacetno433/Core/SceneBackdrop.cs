using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //SceneBackdrop : the quiet notation space the menu pages sit in.
    //Kept in one place so the recruit, reward, rest and event pages all match.
    //
    //Layers, far to near: dark ground, a haze of light, a huge faint staff, bar lines
    //drifting sideways, slow mist, and a vignette.
    public static class SceneBackdrop
    {
        public static void Draw(SpriteBatch sb, float time)
        {
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageDeep);

            //Haze : one wide soft light across the upper middle
            Gfx.DrawGlowBox(sb, new Rectangle(-200, -260, TacetGame.ScreenW + 400, 820), Palette.Paper * 0.07f);

            //Staff : five wide lines running behind everything
            Ornament.Stave(sb, 0, 300, TacetGame.ScreenW, 22, Palette.Paper * 0.045f);

            //Bar Lines : drifting slowly sideways so the page is never completely still
            float drift = (time * 12f) % 260f;
            for (int i = -1; i < 6; i++)
                Gfx.Rect(sb, i * 260f + drift, 300, 1, 88, Palette.Paper * 0.07f);

            //Mist : three large glows wandering slowly
            for (int i = 0; i < 3; i++)
            {
                float x = 640f + (float)Math.Sin(time * 0.07f + i * 2.1f) * 520f;
                float y = 360f + (float)Math.Cos(time * 0.05f + i * 1.7f) * 200f;
                Gfx.DrawGlow(sb, x, y, 300f, Palette.Paper * 0.035f);
            }

            //Frame Lines : thin rules near the edges, like a printed page
            Gfx.Rect(sb, 24, 24, 1, TacetGame.ScreenH - 48, Palette.Paper * 0.08f);
            Gfx.Rect(sb, TacetGame.ScreenW - 25, 24, 1, TacetGame.ScreenH - 48, Palette.Paper * 0.08f);

            Ornament.Vignette(sb, 70, 0.6f);
        }
    }
}
