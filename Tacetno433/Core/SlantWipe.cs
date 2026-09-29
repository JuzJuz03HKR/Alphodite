using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //SlantWipe : the plain page change (round 15, 29 Sep : the player found TACET's wave on every
    //page too much, it is kept for the moments that matter, see ScreenManager).
    //A black band with a slanted edge sweeps in from the left and covers the page, the page
    //changes under it, and the band sweeps on to the right to show the next one. A bright line
    //rides on its edge with a thinner one just ahead, like the quick geometric wipes between the
    //menus of rhythm games such as Project Sekai. The eye always travels one way, left to right.
    public static class SlantWipe
    {
        private const float Lean = 240f;          // how far the top of the edge runs ahead of the bottom
        private const int Strip = 2;              // the band is drawn in strips this many pixels tall

        //Draw : covered 0 is nothing, 1 the whole screen. going is true while the page is being
        //covered, false while the next page is being shown.
        public static void Draw(SpriteBatch sb, float covered, bool going)
        {
            if (covered <= 0f) return;
            if (covered > 1f) covered = 1f;

            //Ease Out : sets off at once and slows down all the way to a soft stop (round 15 : the
            //smoothstep rush in the middle flickered. Project Sekai's animators let every motion
            //settle slowly for the same reason, see DESIGN_RESEARCH 6.13)
            float t = going ? covered : 1f - covered;
            float rest = 1f - t;
            float ease = 1f - rest * rest * rest;
            int w = TacetGame.ScreenW;
            int h = TacetGame.ScreenH;
            float edge = -Lean - 40f + ease * (w + Lean + 80f);     // the bottom of the edge, from off the left to off the right

            //Band : going, everything left of the edge is dark. Showing, everything right of it.
            for (int y = 0; y < h; y += Strip)
            {
                float x = edge + Lean * (1f - y / (float)h);
                if (going) Gfx.Rect(sb, 0, y, x, Strip, Palette.Void);
                else Gfx.Rect(sb, x, y, w - x, Strip, Palette.Void);
            }

            //Edge Lines : a bright one on the edge, a thinner one running just ahead of it
            Vector2 top = new Vector2(edge + Lean, 0f);
            Vector2 bottom = new Vector2(edge, h);
            Vector2 ahead = new Vector2(going ? 22f : -22f, 0f);
            Gfx.Line(sb, top, bottom, Palette.Paper, 3f);
            Gfx.Line(sb, top + ahead, bottom + ahead, Palette.Paper * 0.45f, 1f);
        }
    }
}
