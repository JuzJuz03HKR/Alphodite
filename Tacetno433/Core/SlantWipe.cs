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
    //trailing behind. While the black passes, the name of the page it goes to runs through it, big and dim (WIPE WORD,
    //29 Oct, the user's pick : THE ROAD, THE ARCHIVIST, A QUIET ROOM ... and TACET 4'33 only on the way back to the title).
    //The eye always travels one way, left to right. It takes as long as before (ScreenManager.FadeSpeed) : the user
    //found faster changes flickered, so the punch comes from the layers, not from the speed (SCREENS_LEVEL_UP.md 3.5).
    public static class SlantWipe
    {
        private const float Lean = 240f;          // how far the top of the edge runs ahead of the bottom
        private const int Strip = 2;              // the bands are drawn in strips this many pixels tall (real pixels, SHARP SCREEN)
        private const float WhiteBand = 18f;      // the white band (pixels)
        private const float GreyBand = 64f;       // the grey band next to the black
        private const float Spread = 46f;         // the white pulls away from the grey this far in the middle of the sweep

        //Word Layout : two lines cross like the title's TACET and 4'33, a name of one word sits in the middle
        private const float Track = 26f;          // the gap after every letter, at full size
        private const float TopY = 168f;
        private const float LowY = 390f;
        private const float MidY = 279f;
        private const float Margin = 40f;         // a line too long for the screen shrinks to stay this far inside it

        //Words : drawn only inside the black, cut off by a scissor box (the part of the screen the black covers on every row)
        private static readonly RasterizerState clip = new RasterizerState { ScissorTestEnable = true };

        //WIPE WORD : the two lines of the name, made once when a change starts (SetWord) so Draw makes no strings
        private static string top;                // null = no words
        private static string low;                // null = the name is one word, shown in the middle
        private static float topScale = TextSize.Logo;
        private static float lowScale = TextSize.Logo;
        private static float topWidth;

        //Set Word : the name of the page the wipe goes to (GameScreen.WipeWord), split at its last space. A line that
        //would run off the right of the screen at the swap is made smaller to fit, letters and gaps alike.
        public static void SetWord(string word)
        {
            top = null;
            low = null;
            if (string.IsNullOrEmpty(word) || Ui.LogoFont == null) return;

            int space = word.LastIndexOf(' ');
            top = space > 0 ? word.Substring(0, space) : word;
            low = space > 0 ? word.Substring(space + 1) : null;

            //Room : where each line starts when the black covers everything (trip 0.5, see Draw)
            int w = TacetGame.ScreenW;
            float topRoom = low != null ? w - Margin - w * 0.41f : w - 2f * Margin;
            topScale = Fit(top, topRoom);
            topWidth = Gfx.SpacedWidth(Ui.LogoFont, top, topScale, Track * topScale / TextSize.Logo);
            lowScale = low != null ? Fit(low, w - Margin - w * 0.23f) : TextSize.Logo;
        }

        //Fit : the size that keeps a line inside room, never bigger than the logo size
        private static float Fit(string line, float room)
        {
            float full = Gfx.SpacedWidth(Ui.LogoFont, line, TextSize.Logo, Track);
            return full <= room ? TextSize.Logo : TextSize.Logo * room / full;
        }

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
            //SHARP SCREEN : the strips are 2 real pixels tall on any screen, so the slanted edge stays a clean line
            float strip = Strip * Sharp.Dot;
            for (float y = 0; y < h; y += strip)
            {
                float x = edge + Lean * (1f - y / h);
                if (going)
                {
                    Gfx.Rect(sb, 0, y, x, strip, Palette.Void);
                    Gfx.Rect(sb, x, y, GreyBand, strip, Palette.LineGrey);
                    Gfx.Rect(sb, x + GreyBand + apart, y, WhiteBand, strip, Palette.Paper);
                }
                else
                {
                    Gfx.Rect(sb, x, y, w - x, strip, Palette.Void);
                    Gfx.Rect(sb, x - GreyBand, y, GreyBand, strip, Palette.LineGrey);
                    Gfx.Rect(sb, x - GreyBand - apart - WhiteBand, y, WhiteBand, strip, Palette.Paper);
                }
            }

            //Edge Line : a bright line where the black meets the grey
            Gfx.Line(sb, new Vector2(edge + Lean, 0f), new Vector2(edge, h), Palette.Paper, 2f);

            //Words : one sweep in and one out is one trip (0..1), so the words keep moving across the swap.
            //The top line drifts left and the low line right, so they cross. A name of one word drifts left, centred
            //on the screen at the swap.
            if (Ui.LogoFont == null || top == null) return;
            float trip = going ? t * 0.5f : 0.5f + t * 0.5f;
            if (low != null)
            {
                Word(sb, top, w * 0.62f - trip * w * 0.42f, TopY, topScale, edge, going);
                Word(sb, low, w * 0.08f + trip * w * 0.3f, LowY, lowScale, edge, going);
            }
            else Word(sb, top, (w - topWidth) / 2f + (0.5f - trip) * w * 0.42f, MidY, topScale, edge, going);
        }

        //Word : one big dim word inside the black. The scissor box is the part of the word's rows the black covers on
        //every one of them : the slanted edge is furthest left at the word's bottom row and furthest right at its top.
        private static void Word(SpriteBatch sb, string word, float x, float y, float scale, float edge, bool going)
        {
            int w = TacetGame.ScreenW;
            int h = TacetGame.ScreenH;
            float tall = Ui.LogoFont.LineSpacing * scale;
            int left = going ? 0 : (int)(edge + Lean * (1f - y / h));
            int right = going ? (int)(edge + Lean * (1f - (y + tall) / h)) : w;
            left = Math.Max(0, Math.Min(w, left));
            right = Math.Max(0, Math.Min(w, right));
            if (right - left < 4) return;

            //The scissor box is in the picture's real pixels (SHARP SCREEN), the words in game pixels as always
            GraphicsDevice device = sb.GraphicsDevice;
            Rectangle before = device.ScissorRectangle;
            sb.End();
            float s = Sharp.Scale;
            device.ScissorRectangle = new Rectangle((int)(left * s), (int)(y * s), (int)((right - left) * s), (int)(tall * s));
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, clip, null, Sharp.Transform);
            Gfx.TextSpaced(sb, Ui.LogoFont, word, x, y, Palette.Stage, scale, Track * scale / TextSize.Logo);
            sb.End();
            device.ScissorRectangle = before;
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, Sharp.Transform);
        }
    }
}
