using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //Curtain : the stage curtain that closes between two pages and opens on the next one,
    //instead of a plain fade to black. ScreenManager draws it over everything.
    //
    //Math art : each drape is thin vertical stripes. A stripe's shade follows a sine wave of its
    //distance from the drape's leading edge, which reads as folds of cloth. Because the distance
    //is measured from the moving edge, the folds travel with the drape as it slides in.
    //Same look as the curtains of the conductor select and curtain call pages.
    public static class Curtain
    {
        private const int Stripe = 4;           // pixels per stripe, wide enough to stay cheap
        private const float FoldSize = 0.09f;   // bigger = narrower folds

        //Draw : closed 0 is fully open (nothing drawn), 1 is both drapes meeting in the middle
        public static void Draw(SpriteBatch sb, float closed)
        {
            if (closed <= 0f) return;
            if (closed > 1f) closed = 1f;

            //Smoothstep : starts slowly, speeds up, and settles, like a curtain on a rope
            float ease = closed * closed * (3f - 2f * closed);
            int half = TacetGame.ScreenW / 2;
            int reach = (int)(half * ease) + 2;     // two pixels of overlap, so no seam shows when closed

            //House Lights : the room dims a little as the drapes come in
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Color.Black * (0.45f * ease));

            DrawDrape(sb, 0, reach, false);
            DrawDrape(sb, TacetGame.ScreenW - reach, reach, true);
        }

        //Drape : one side. fromRight says which way the drape is pulled in.
        private static void DrawDrape(SpriteBatch sb, int x, int width, bool fromRight)
        {
            for (int i = 0; i < width; i += Stripe)
            {
                float distance = fromRight ? i : width - i;         // from the leading edge
                float fold = (float)Math.Sin(distance * FoldSize) * 0.5f + 0.5f;
                Color c = Color.Lerp(Palette.CurtainDark, Palette.Curtain, fold);
                Gfx.Rect(sb, x + i, 0, Stripe, TacetGame.ScreenH, c);
            }

            //Leading Edge : a thin line of light where the drape meets the stage
            int edgeX = fromRight ? x : x + width - 2;
            Gfx.Rect(sb, edgeX, 0, 2, TacetGame.ScreenH, Palette.Paper * 0.2f);

            //Hem : a heavier band along the bottom
            Gfx.Rect(sb, x, TacetGame.ScreenH - 26, width, 26, Palette.CurtainDark * 0.8f);
            Gfx.Rect(sb, x, TacetGame.ScreenH - 26, width, 1, Palette.Paper * 0.12f);
        }
    }
}
