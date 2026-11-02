using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //DIGITS (Beta1 2 Nov, the user 13.141 : "the same number font in the whole game, the pixel numbers out") : the big numbers
    //of the game, the damage that flies off a hit, the COMBO count and the numbers that count up on the result pages, in the
    //game's one number font : Saira, the font of every other number (Gfx NUMBERS), here its biggest copy (MainFont_3x) so a big
    //number stays sharp. A dark rim round it, so it reads over the stage. Until 2 Nov these were blocky pixel digits
    //(PixelDigits), the only numbers in the game that did not look like the others.
    //The same calls as the old pixel digits : cell is the size the old digits were made of, a digit stands 7 cells tall.
    public static class Digits
    {
        public static SpriteFont Font;                       // MainFont_3x, set by TacetGame at start
        private static float tall;                           // how tall a digit stands in Font, in its own pixels
        private static float top;                            // how far below the line's top a digit starts

        private const float CellsTall = 7f;                  // the old pixel digits : 7 cells tall
        private const float Rim = 0.55f;                     // the dark rim, this many cells wide

        //Measure : the digit '0' of the font, once
        private static void Measure()
        {
            int i = Font.Characters.IndexOf('0');
            SpriteFont.Glyph g = Font.Glyphs[Math.Max(0, i)];
            tall = Math.Max(1f, g.BoundsInTexture.Height);
            top = g.Cropping.Y;
        }

        private static string TextOf(int value, bool sign)
        {
            return sign ? NumberText.Signed(value) : NumberText.Get(Math.Abs(value));     // as the pixel digits : no sign, no minus
        }

        //Width : how wide a number is, in pixels
        public static float Width(int value, bool sign, float cell)
        {
            if (Font == null) return 0f;
            if (tall <= 0f) Measure();
            float scale = CellsTall * cell / tall;
            return Font.MeasureString(TextOf(value, sign)).X * scale;
        }

        //Draw : the number centred on (cx, cy). sign puts a + (or a - for a negative value) in front. The rim is the same number
        //drawn eight times a little off centre in the outline colour first (Color.Transparent : no rim).
        public static void Draw(SpriteBatch sb, int value, bool sign, float cx, float cy, float cell, Color color, Color outline)
        {
            if (Font == null) return;
            if (tall <= 0f) Measure();
            string s = TextOf(value, sign);
            float scale = CellsTall * cell / tall;
            float w = Font.MeasureString(s).X * scale;
            Vector2 at = new Vector2(cx - w / 2f, cy - (top + tall / 2f) * scale);
            if (outline.A > 0)
            {
                float r = Math.Max(1f, Rim * cell);
                for (int k = 0; k < 8; k++)
                {
                    float angle = k * MathHelper.PiOver4;
                    Vector2 off = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * r;
                    SharpText.Draw(sb, Font, s, at + off, outline, 0f, Vector2.Zero, scale);
                }
            }
            SharpText.Draw(sb, Font, s, at, color, 0f, Vector2.Zero, scale);
        }
    }
}
