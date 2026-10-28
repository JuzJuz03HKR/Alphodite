using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //PixelDigits (Beta1 round 4) : numbers drawn as pixel art, chunky digits with a dark outline, the way the user's
    //references show damage (big blocky numbers over the enemy). A digit is 5 x 7 cells and a cell is a few pixels
    //(it may be a fraction : each edge is rounded to a whole pixel).
    //Only the digits, + and -, so no string is ever made while drawing : the number is taken apart with sums.
    //ADVANCED PART (small) : each digit's filled cells are kept as RUNS (a row, where the stretch starts, how long it
    //is), worked out once from the little pictures below. A digit is then a handful of rectangles, not thirty five.
    public static class PixelDigits
    {
        public const int Cols = 5;
        public const int Rows = 7;

        //The shapes : 0 to 9, then + and -. A # is a filled cell.
        private static readonly string[][] shapes =
        {
            new[] { ".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###." },
            new[] { "..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###." },
            new[] { ".###.", "#...#", "....#", "..##.", ".#...", "#....", "#####" },
            new[] { "####.", "....#", "....#", ".###.", "....#", "....#", "####." },
            new[] { "...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#." },
            new[] { "#####", "#....", "####.", "....#", "....#", "#...#", ".###." },
            new[] { ".###.", "#....", "#....", "####.", "#...#", "#...#", ".###." },
            new[] { "#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..." },
            new[] { ".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###." },
            new[] { ".###.", "#...#", "#...#", ".####", "....#", "....#", ".###." },
            new[] { ".....", "..#..", "..#..", "#####", "..#..", "..#..", "....." },
            new[] { ".....", ".....", ".....", "#####", ".....", ".....", "....." },
        };
        public const int Plus = 10;
        public const int Minus = 11;

        private struct Run
        {
            public int Row, From, Length;
        }
        private static Run[][] runs;
        private static readonly int[] digits = new int[10];     // the digits of the number being drawn, last first

        //Build : the runs of every shape, once
        private static void Build()
        {
            runs = new Run[shapes.Length][];
            Run[] scratch = new Run[Rows * Cols];
            for (int g = 0; g < shapes.Length; g++)
            {
                int count = 0;
                for (int row = 0; row < Rows; row++)
                {
                    string line = shapes[g][row];
                    int col = 0;
                    while (col < Cols)
                    {
                        if (line[col] != '#') { col++; continue; }
                        int start = col;
                        while (col < Cols && line[col] == '#') col++;
                        scratch[count++] = new Run { Row = row, From = start, Length = col - start };
                    }
                }
                runs[g] = new Run[count];
                Array.Copy(scratch, runs[g], count);
            }
        }

        //Width : how wide a number is, in pixels
        public static float Width(int value, bool sign, float cell)
        {
            int count = 0;
            int v = Math.Abs(value);
            do { count++; v /= 10; } while (v > 0);
            if (sign) count++;
            return count * (Cols + 1) * cell - cell;
        }

        //Draw : the number centred on (cx, cy). sign puts a + (or a - for a negative value) in front.
        //The outline is the same runs drawn a little bigger in the outline colour first.
        public static void Draw(SpriteBatch sb, int value, bool sign, float cx, float cy, float cell, Color color, Color outline)
        {
            if (runs == null) Build();

            int count = 0;
            int v = Math.Abs(value);
            do { digits[count++] = v % 10; v /= 10; } while (v > 0 && count < digits.Length);

            float advance = (Cols + 1) * cell;
            float width = (count + (sign ? 1 : 0)) * advance - cell;
            float left = Sharp.Snap(cx - width / 2f);
            float top = Sharp.Snap(cy - Rows * cell / 2f);
            float grow = Math.Max(2f, (float)Math.Floor(cell * 0.5f));

            for (int pass = 0; pass < 2; pass++)
            {
                Color c = pass == 0 ? outline : color;
                float g = pass == 0 ? grow : 0f;
                float x = left;
                if (sign)
                {
                    DrawGlyph(sb, value < 0 ? Minus : Plus, x, top, cell, c, g);
                    x += advance;
                }
                for (int i = count - 1; i >= 0; i--)
                {
                    DrawGlyph(sb, digits[i], x, top, cell, c, g);
                    x += advance;
                }
            }
        }

        private static void DrawGlyph(SpriteBatch sb, int glyph, float x, float y, float cell, Color color, float grow)
        {
            //Every edge lands on a whole pixel, so a cell of 4.5 is drawn 4 or 5 pixels wide and stays sharp
            //(a whole REAL pixel, SHARP SCREEN : Sharp.Snap is Math.Round in the window)
            Run[] list = runs[glyph];
            for (int k = 0; k < list.Length; k++)
            {
                Run r = list[k];
                float x0 = Sharp.Snap(x + r.From * cell);
                float x1 = Sharp.Snap(x + (r.From + r.Length) * cell);
                float y0 = Sharp.Snap(y + r.Row * cell);
                float y1 = Sharp.Snap(y + (r.Row + 1) * cell);
                Gfx.Rect(sb, x0 - grow, y0 - grow, x1 - x0 + grow * 2f, y1 - y0 + grow * 2f, color);
            }
        }
    }
}
