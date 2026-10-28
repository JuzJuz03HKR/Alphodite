using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //SlantWipe : the plain page change (round 15, 29 Sep : the player found TACET's wave on every
    //page too much, it is kept for the moments that matter, see ScreenManager).
    //Beta1 screens (28 Oct) : slanted bands sweep in from the left and cover the page, a thin white band in front, a grey
    //one, then the black body (flat panels of black and white cut on a slant, like the menus of Persona 5 and ZZZ). The
    //page changes under the black, and the bands sweep on to the right in the same order, the grey and the white now
    //trailing behind. While the black passes, the words TACET and 4'33 run through it, big and dim.
    //The eye always travels one way, left to right. It takes as long as before (ScreenManager.FadeSpeed) : the user
    //found faster changes flickered, so the punch comes from the layers, not from the speed (SCREENS_LEVEL_UP.md 3.5).
    public static class SlantWipe
    {
        private const float Lean = 240f;          // how far the top of the edge runs ahead of the bottom
        private const int Strip = 2;              // the bands are drawn in strips this many pixels tall
        private const float WhiteBand = 18f;      // the white band (pixels)
        private const float GreyBand = 64f;       // the grey band next to the black
        private const float Spread = 46f;         // the white pulls away from the grey this far in the middle of the sweep

        //Words : drawn only inside the black, cut off by a scissor box (the part of the screen the black covers on every row)
        private static readonly RasterizerState clip = new RasterizerState { ScissorTestEnable = true };

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
            float edge = -Lean - 40f + ease * (w + Lean + 80f);     // the bottom of the black's edge, from off the left to off the right
            float apart = Spread * (float)Math.Sin(t * Math.PI);    // the white band opens away mid sweep and closes again

            //Bands : going, the black is left of the edge with the grey and the white in front of it.
            //Showing, the black is right of the edge with the grey and the white trailing behind it.
            for (int y = 0; y < h; y += Strip)
            {
                float x = edge + Lean * (1f - y / (float)h);
                if (going)
                {
                    Gfx.Rect(sb, 0, y, x, Strip, Palette.Void);
                    Gfx.Rect(sb, x, y, GreyBand, Strip, Palette.LineGrey);
                    Gfx.Rect(sb, x + GreyBand + apart, y, WhiteBand, Strip, Palette.Paper);
                }
                else
                {
                    Gfx.Rect(sb, x, y, w - x, Strip, Palette.Void);
                    Gfx.Rect(sb, x - GreyBand, y, GreyBand, Strip, Palette.LineGrey);
                    Gfx.Rect(sb, x - GreyBand - apart - WhiteBand, y, WhiteBand, Strip, Palette.Paper);
                }
            }

            //Edge Line : a bright line where the black meets the grey
            Gfx.Line(sb, new Vector2(edge + Lean, 0f), new Vector2(edge, h), Palette.Paper, 2f);

            //Words : one sweep in and one out is one trip (0..1), so the words keep moving across the swap
            if (Ui.LogoFont == null) return;
            float trip = going ? t * 0.5f : 0.5f + t * 0.5f;
            Word(sb, "TACET", w * 0.62f - trip * w * 0.42f, 168f, edge, going);
            Word(sb, "4'33", w * 0.08f + trip * w * 0.3f, 390f, edge, going);
        }

        //Word : one big dim word inside the black. The scissor box is the part of the word's rows the black covers on
        //every one of them : the slanted edge is furthest left at the word's bottom row and furthest right at its top.
        private static void Word(SpriteBatch sb, string word, float x, float y, float edge, bool going)
        {
            int w = TacetGame.ScreenW;
            int h = TacetGame.ScreenH;
            float tall = Ui.LogoFont.LineSpacing * TextSize.Logo;
            int left = going ? 0 : (int)(edge + Lean * (1f - y / h));
            int right = going ? (int)(edge + Lean * (1f - (y + tall) / h)) : w;
            left = Math.Max(0, Math.Min(w, left));
            right = Math.Max(0, Math.Min(w, right));
            if (right - left < 4) return;

            GraphicsDevice device = sb.GraphicsDevice;
            Rectangle before = device.ScissorRectangle;
            sb.End();
            device.ScissorRectangle = new Rectangle(left, (int)y, right - left, (int)tall);
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, clip);
            Gfx.TextSpaced(sb, Ui.LogoFont, word, x, y, Palette.Stage, TextSize.Logo, 26f);
            sb.End();
            device.ScissorRectangle = before;
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);
        }
    }
}
