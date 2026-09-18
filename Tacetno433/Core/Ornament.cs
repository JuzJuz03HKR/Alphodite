using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //Ornament : the small decorations that make a page look designed instead of just laid out.
    //
    //They come from the reference boards: viewfinder corners, diamond dividers (the
    //profile pages), rows of little crosses and barcodes (the editorial posters), rays
    //behind a new character (the gacha reveal), and film grain over everything.
    //All of them are drawn from boxes and lines, no picture files.
    public static class Ornament
    {
        //Barcode Widths : a fixed pattern, so the barcode never flickers
        private static int[] barWidths = { 2, 1, 1, 3, 1, 2, 1, 1, 2, 3, 1, 1, 2, 1, 3, 1, 1, 2, 1, 2 };

        private static float grainTimer;
        private static int grainShiftX;
        private static int grainShiftY;
        private static Random grainRandom = new Random(7);

        //Corner Brackets : four L shapes, the crop marks of a viewfinder
        public static void CornerBrackets(SpriteBatch sb, Rectangle r, int length, Color color, int thickness)
        {
            Gfx.Rect(sb, r.X, r.Y, length, thickness, color);
            Gfx.Rect(sb, r.X, r.Y, thickness, length, color);

            Gfx.Rect(sb, r.Right - length, r.Y, length, thickness, color);
            Gfx.Rect(sb, r.Right - thickness, r.Y, thickness, length, color);

            Gfx.Rect(sb, r.X, r.Bottom - thickness, length, thickness, color);
            Gfx.Rect(sb, r.X, r.Bottom - length, thickness, length, color);

            Gfx.Rect(sb, r.Right - length, r.Bottom - thickness, length, thickness, color);
            Gfx.Rect(sb, r.Right - thickness, r.Bottom - length, thickness, length, color);
        }

        //Divider : a line, a diamond in the middle, a line. The separator on profile pages.
        public static void Divider(SpriteBatch sb, float cx, float y, float halfWidth, Color color)
        {
            Gfx.Rect(sb, cx - halfWidth, y, halfWidth - 14, 1, color);
            Gfx.Rect(sb, cx + 14, y, halfWidth - 14, 1, color);
            Gfx.DiamondOutline(sb, cx, y, 6, color, 1f);
            Gfx.Diamond(sb, cx, y, 2, color);
            Gfx.Diamond(sb, cx - halfWidth - 5, y, 2, color);
            Gfx.Diamond(sb, cx + halfWidth + 5, y, 2, color);
        }

        //Rule With Diamond : a heading underline that ends in a small diamond
        public static void Rule(SpriteBatch sb, float x, float y, float width, Color color)
        {
            Gfx.Rect(sb, x, y, width - 8, 1, color);
            Gfx.Diamond(sb, x + width - 3, y, 3, color);
        }

        //Double Frame : two thin frames with diamond studs on the corners
        public static void DoubleFrame(SpriteBatch sb, Rectangle r, Color color)
        {
            Gfx.RectOutline(sb, r, color, 1);
            Rectangle inner = r;
            inner.Inflate(-5, -5);
            Gfx.RectOutline(sb, inner, color * 0.5f, 1);

            Gfx.Diamond(sb, r.X, r.Y, 4, color);
            Gfx.Diamond(sb, r.Right, r.Y, 4, color);
            Gfx.Diamond(sb, r.X, r.Bottom, 4, color);
            Gfx.Diamond(sb, r.Right, r.Bottom, 4, color);
        }

        //Crosses : a small grid of plus signs, the technical poster decoration
        public static void Crosses(SpriteBatch sb, float x, float y, int columns, int rows, float gap, Color color)
        {
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    float px = x + col * gap;
                    float py = y + row * gap;
                    Gfx.Rect(sb, px - 3, py, 7, 1, color);
                    Gfx.Rect(sb, px, py - 3, 1, 7, color);
                }
            }
        }

        //Barcode : thin upright bars
        public static void Barcode(SpriteBatch sb, float x, float y, float width, float height, Color color)
        {
            float cursor = 0f;
            int i = 0;
            while (cursor < width)
            {
                int w = barWidths[i % barWidths.Length];
                if (i % 2 == 0) Gfx.Rect(sb, x + cursor, y, w, height, color);
                cursor += w + 1;
                i++;
            }
        }

        //Ticks : a ruler, one tall tick every fifth mark
        public static void Ticks(SpriteBatch sb, float x, float y, float width, int count, Color color)
        {
            for (int i = 0; i <= count; i++)
            {
                float px = x + width * i / count;
                int h = (i % 5 == 0) ? 8 : 4;
                Gfx.Rect(sb, px, y, 1, h, color);
            }
        }

        //Stave : five lines, the staff every page is written on
        public static void Stave(SpriteBatch sb, float x, float y, float width, float gap, Color color)
        {
            for (int i = 0; i < 5; i++)
                Gfx.Rect(sb, x, y + i * gap, width, 1, color);
        }

        //Rays : thin lines fanning out from a point, turning slowly.
        //This is the burst behind a character on a gacha reveal.
        public static void Rays(SpriteBatch sb, float cx, float cy, float inner, float outer, int count, float turn, Color color)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = turn + MathHelper.TwoPi * i / count;
                float cos = (float)Math.Cos(angle);
                float sin = (float)Math.Sin(angle);
                float thickness = (i % 3 == 0) ? 3f : 1f;
                Gfx.Line(sb, cx + cos * inner, cy + sin * inner, cx + cos * outer, cy + sin * outer, color, thickness);
            }
        }

        //Vignette : shade the four edges so the eye lands in the middle.
        //depth is how many pixels the shade reaches in, strength how dark the very edge gets.
        public static void Vignette(SpriteBatch sb, int depth, float strength)
        {
            for (int i = 0; i < depth; i += 2)
            {
                float t = 1f - (float)i / depth;
                Color shade = Color.Black * (t * t * strength);
                Gfx.Rect(sb, i, 0, 2, TacetGame.ScreenH, shade);
                Gfx.Rect(sb, TacetGame.ScreenW - 2 - i, 0, 2, TacetGame.ScreenH, shade);
                Gfx.Rect(sb, 0, i, TacetGame.ScreenW, 2, shade);
                Gfx.Rect(sb, 0, TacetGame.ScreenH - 2 - i, TacetGame.ScreenW, 2, shade);
            }
        }

        //Soft Band : a dark band that fades out at the top, for text sitting on top of art
        public static void FadeUp(SpriteBatch sb, Rectangle r, float strength)
        {
            for (int y = 0; y < r.Height; y += 2)
            {
                float t = (float)y / r.Height;
                Gfx.Rect(sb, r.X, r.Y + y, r.Width, 2, Color.Black * (t * strength));
            }
        }

        //Grain Update : the grain jumps to a new spot a few times a second, like old film
        public static void UpdateGrain(float dt)
        {
            grainTimer += dt;
            if (grainTimer < 0.09f) return;
            grainTimer = 0f;
            grainShiftX = grainRandom.Next(256);
            grainShiftY = grainRandom.Next(256);
        }

        //Grain : the noise texture laid over the whole screen in tiles. Light specks show
        //on dark pages and dark specks on bright ones, so both kinds are drawn faintly.
        public static void Grain(SpriteBatch sb)
        {
            int size = Gfx.Noise.Width;

            //Tiles start one tile early so the shifted second layer still covers the edges
            for (int y = -grainShiftY - size; y < TacetGame.ScreenH; y += size)
            {
                for (int x = -grainShiftX - size; x < TacetGame.ScreenW; x += size)
                {
                    sb.Draw(Gfx.Noise, new Rectangle(x, y, size, size), Color.White * 0.05f);
                    sb.Draw(Gfx.Noise, new Rectangle(x + 97, y + 53, size, size), Color.Black * 0.07f);
                }
            }
        }
    }
}
