using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //Gfx : simple drawing helpers. Everything is stretched from one white pixel,
    //so the whole prototype can be drawn without any art files.
    public static class Gfx
    {
        public static Texture2D Pixel;
        public static Texture2D Glow;
        public static Texture2D Noise;

        //Single Letters : every printable character as its own one letter string, made once.
        //Spaced out text draws one letter at a time, and looking the letter up here means
        //it never has to make a new string while drawing.
        private static string[] letters = new string[128];

        //Gfx Init : build the textures the whole game draws with, once at startup.
        //Pixel is a single white dot we stretch into boxes and lines.
        //Glow is a soft round light. Drawing Glow stretched is ONE draw call, where
        //faking the same soft light out of stacked boxes would cost hundreds every frame.
        //Noise is random specks, laid over every page like film grain or paper texture.
        public static void Init(GraphicsDevice device)
        {
            Pixel = new Texture2D(device, 1, 1);
            Pixel.SetData(new Color[] { Color.White });

            //Glow Texture : bright in the middle, fading to nothing at the edge
            int size = 256;                                     // big enough to stay smooth when stretched
            Glow = new Texture2D(device, size, size);
            Color[] dots = new Color[size * size];
            float half = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - half) / half;
                    float dy = (y + 0.5f - half) / half;
                    float distance = (float)Math.Sqrt(dx * dx + dy * dy);

                    float a = 1f - distance;
                    if (a < 0f) a = 0f;
                    a = a * a;                                  // softer falloff

                    //MonoGame blends with premultiplied alpha, so colour and alpha match
                    byte v = (byte)(a * 255f);
                    dots[y * size + x] = new Color(v, v, v, v);
                }
            }
            Glow.SetData(dots);

            //Noise Texture : every dot gets a random strength. A fixed seed keeps the grain
            //the same every time the game starts.
            Noise = new Texture2D(device, size, size);
            Random random = new Random(433);
            for (int i = 0; i < dots.Length; i++)
            {
                int roll = random.Next(100);
                byte v = 0;
                if (roll < 18) v = (byte)random.Next(60, 256);  // only some dots are specks
                dots[i] = new Color(v, v, v, v);
            }
            Noise.SetData(dots);

            //Letters Build
            for (int c = 32; c < 127; c++)
                letters[c] = ((char)c).ToString();
        }

        //Draw Rect : filled box
        //SHARP SCREEN (Beta1) : on a screen bigger than the page (Sharp.Scale above 1) the edges land on whole real pixels,
        //and a hairline (thinner than a pixel and a half) is always the same number of real pixels thick wherever it sits,
        //so the two sides of a frame match. In the 1280 x 720 window it is the plain box it always was.
        public static void Rect(SpriteBatch sb, float x, float y, float w, float h, Color color)
        {
            if (Sharp.Scale == 1f)
            {
                sb.Draw(Pixel, new Rectangle((int)x, (int)y, (int)w, (int)h), color);
                return;
            }
            if (w <= 0f || h <= 0f) return;
            float x0 = Sharp.Edge(x);
            float y0 = Sharp.Edge(y);
            float x1 = w < 1.5f ? x0 + Sharp.Edge(w) : Sharp.Edge(x + w);
            float y1 = h < 1.5f ? y0 + Sharp.Edge(h) : Sharp.Edge(y + h);
            if (x1 <= x0 || y1 <= y0) return;
            float dot = Sharp.Dot;
            sb.Draw(Pixel, new Vector2(x0 * dot, y0 * dot), null, color, 0f, Vector2.Zero,
                    new Vector2((x1 - x0) * dot, (y1 - y0) * dot), SpriteEffects.None, 0f);
        }

        public static void Rect(SpriteBatch sb, Rectangle r, Color color)
        {
            if (Sharp.Scale == 1f) sb.Draw(Pixel, r, color);
            else Rect(sb, r.X, r.Y, r.Width, r.Height, color);
        }

        //Draw Rect Outline : hollow box made of 4 thin boxes
        public static void RectOutline(SpriteBatch sb, Rectangle r, Color color, int thickness)
        {
            Rect(sb, r.X, r.Y, r.Width, thickness, color);                              // top
            Rect(sb, r.X, r.Bottom - thickness, r.Width, thickness, color);             // bottom
            Rect(sb, r.X, r.Y, thickness, r.Height, color);                             // left
            Rect(sb, r.Right - thickness, r.Y, thickness, r.Height, color);             // right
        }

        //Draw Line : a stretched and rotated pixel between two points
        //SHARP SCREEN : on a bigger screen the thickness is a whole number of real pixels, so a straight line is the same
        //thickness wherever it sits (a line one game pixel thick is two real pixels at 1.5, three at 3). A line thinner
        //than one real pixel is drawn one pixel thick and fainter by as much, so it weighs what it did.
        public static void Line(SpriteBatch sb, Vector2 a, Vector2 b, Color color, float thickness)
        {
            Vector2 diff = b - a;
            float length = diff.Length();
            if (length < 0.5f * Sharp.Dot || thickness <= 0f) return;
            float angle = (float)Math.Atan2(diff.Y, diff.X);
            if (Sharp.Scale != 1f)
            {
                float real = thickness * Sharp.Scale;
                if (real < 1f) color *= real;
                thickness = Math.Max(1f, Sharp.Edge(thickness)) * Sharp.Dot;
            }
            sb.Draw(Pixel, a, null, color, angle, new Vector2(0f, 0.5f),
                    new Vector2(length, thickness), SpriteEffects.None, 0f);
        }

        //Draw Line (numbers) : same as above without building the two points first
        public static void Line(SpriteBatch sb, float x1, float y1, float x2, float y2, Color color, float thickness)
        {
            Line(sb, new Vector2(x1, y1), new Vector2(x2, y2), color, thickness);
        }

        //Draw Circle : filled circle built from horizontal lines
        //SHARP SCREEN : this shape and the others built from rows (Capsule, Pill, Arrow, Triangle, Diamond, SlantBox) use
        //rows one real pixel tall (Sharp.Dot), so their edges are as smooth as the screen allows. In the window Dot is 1,
        //the rows are the game pixels they always were.
        public static void Circle(SpriteBatch sb, float cx, float cy, float radius, Color color)
        {
            float dot = Sharp.Dot;
            int rows = (int)(radius * Sharp.Scale);
            for (int k = -rows; k <= rows; k++)
            {
                float y = k * dot;
                float inside = radius * radius - y * y;
                if (inside < 0f) continue;
                float half = (int)(Math.Sqrt(inside) * Sharp.Scale) * dot;
                Rect(sb, cx - half, cy + y, half * 2f + dot, dot, color);
            }
        }

        //Pieces (SHARP SCREEN) : how many straight pieces a curve of this length needs. In the window it is the number the
        //shape always used, on a bigger screen more, so no piece is longer than about 6 real pixels and no corner shows.
        private static int Pieces(int inWindow, float length)
        {
            if (Sharp.Scale == 1f) return inWindow;
            return (int)MathHelper.Clamp(length * Sharp.Scale / 6f, inWindow, 1024f);
        }

        //Draw Circle Outline : a ring, built from short straight segments
        public static void CircleOutline(SpriteBatch sb, float cx, float cy, float radius, Color color, float thickness)
        {
            int segments = Pieces(36, radius * MathHelper.TwoPi);
            Vector2 previous = new Vector2(cx + radius, cy);

            for (int i = 1; i <= segments; i++)
            {
                float angle = MathHelper.TwoPi * i / segments;
                Vector2 now = new Vector2(cx + (float)Math.Cos(angle) * radius, cy + (float)Math.Sin(angle) * radius);
                Line(sb, previous, now, color, thickness);
                previous = now;
            }
        }

        //Draw Ring (Beta1 screens) : a circle outline for big circles. CircleOutline's 36 straight pieces show as corners
        //on a ring as big as the screen, so here the pieces are about 6 pixels long whatever the size (24 to 256 of them).
        public static void Ring(SpriteBatch sb, float cx, float cy, float radius, Color color, float thickness)
        {
            int segments = (int)MathHelper.Clamp(radius * MathHelper.TwoPi / 6f, 24f, 256f);
            Arc(sb, cx, cy, radius, 0f, MathHelper.TwoPi, color, thickness, segments);
        }

        //Draw Ellipse Outline : a ring squashed flat, like a ripple seen on the floor.
        //rx is half the width, ry half the height.
        public static void EllipseOutline(SpriteBatch sb, float cx, float cy, float rx, float ry, Color color, float thickness)
        {
            int segments = Pieces(32, Math.Max(rx, ry) * MathHelper.TwoPi);
            Vector2 previous = new Vector2(cx + rx, cy);

            for (int i = 1; i <= segments; i++)
            {
                float angle = MathHelper.TwoPi * i / segments;
                Vector2 now = new Vector2(cx + (float)Math.Cos(angle) * rx, cy + (float)Math.Sin(angle) * ry);
                Line(sb, previous, now, color, thickness);
                previous = now;
            }
        }

        //Draw Arc : part of a ring, used for sound waves travelling across the duel.
        //Angles are in radians, 0 points right. Eight segments is plenty for a short arc.
        public static void Arc(SpriteBatch sb, float cx, float cy, float radius, float fromAngle, float toAngle, Color color, float thickness)
        {
            Arc(sb, cx, cy, radius, fromAngle, toAngle, color, thickness, 8);
        }

        //Draw Arc (pieces) : the same with as many straight pieces as asked, for long arcs (Beta1 screens)
        public static void Arc(SpriteBatch sb, float cx, float cy, float radius, float fromAngle, float toAngle, Color color, float thickness, int segments)
        {
            segments = Pieces(segments, radius * Math.Abs(toAngle - fromAngle));
            Vector2 previous = new Vector2(cx + (float)Math.Cos(fromAngle) * radius, cy + (float)Math.Sin(fromAngle) * radius);

            for (int i = 1; i <= segments; i++)
            {
                float angle = fromAngle + (toAngle - fromAngle) * i / segments;
                Vector2 now = new Vector2(cx + (float)Math.Cos(angle) * radius, cy + (float)Math.Sin(angle) * radius);
                Line(sb, previous, now, color, thickness);
                previous = now;
            }
        }

        //Draw Capsule : a tall rounded slot, the shape a musician takes on stage.
        //Built as a top cap, a box, and a bottom cap that never overlap each other.
        //Overlapping pieces would show as lighter bands whenever the colour is see-through.
        public static void Capsule(SpriteBatch sb, Rectangle r, Color color)
        {
            //Shared Centre : fill and outline both measure from the same centre line,
            //otherwise the two drift half a pixel apart and a sliver shows
            float cx = r.X + r.Width / 2f;
            float radius = r.Width / 2f;
            float dot = Sharp.Dot;
            int caps = (int)(radius * Sharp.Scale);                                   // rows in each cap (real pixels)

            for (int y = 0; y < caps; y++)
            {
                float dy = (caps - y) * dot;
                float half = (int)(Math.Sqrt(Math.Max(0f, radius * radius - dy * dy)) * Sharp.Scale) * dot;
                Rect(sb, cx - half, r.Y + y * dot, half * 2f + dot, dot, color);                 // top cap row
                Rect(sb, cx - half, r.Bottom - dot - y * dot, half * 2f + dot, dot, color);      // bottom cap row
            }
            Rect(sb, cx - radius, r.Y + caps * dot, radius * 2f + dot, r.Height - caps * dot * 2f, color);
        }

        //Draw Capsule Outline : same shape, only the edge. The side lines sit centred on the
        //edge, the same way the arcs do.
        public static void CapsuleOutline(SpriteBatch sb, Rectangle r, Color color, float thickness)
        {
            float cx = r.X + r.Width / 2f;
            float radius = r.Width / 2f;
            float half = thickness / 2f;
            Arc(sb, cx, r.Y + radius, radius, MathHelper.Pi, MathHelper.TwoPi, color, thickness);
            Arc(sb, cx, r.Bottom - radius, radius, 0f, MathHelper.Pi, color, thickness);
            Rect(sb, cx - radius - half, r.Y + radius, thickness, r.Height - radius * 2f, color);
            Rect(sb, cx + radius - half, r.Y + radius, thickness, r.Height - radius * 2f, color);
        }

        //Draw Pill : a capsule lying on its side, used for bars with round ends.
        //Each row is ONE box from the left curve to the right curve, so nothing overlaps.
        public static void Pill(SpriteBatch sb, Rectangle r, Color color)
        {
            float radius = r.Height / 2f;
            float straight = r.Width - radius * 2f;
            if (straight < 0f) straight = 0f;
            float dot = Sharp.Dot;
            int rows = (int)(r.Height * Sharp.Scale);

            for (int k = 0; k < rows; k++)
            {
                float y = k * dot;
                float dy = y + 0.5f * dot - radius;
                float half = (float)Math.Sqrt(Math.Max(0f, radius * radius - dy * dy));
                Rect(sb, r.X + radius - half, r.Y + y, straight + half * 2f, dot, color);
            }
        }

        //Draw Bar : background box with a fill on top, used for stamina and the push line
        public static void Bar(SpriteBatch sb, Rectangle r, float amount, Color back, Color fill)
        {
            if (amount < 0f) amount = 0f;
            if (amount > 1f) amount = 1f;
            Rect(sb, r, back);
            Rect(sb, r.X, r.Y, r.Width * amount, r.Height, fill);
        }

        //Draw Glow : one soft round light. Use this instead of stacking rectangles.
        public static void DrawGlow(SpriteBatch sb, float cx, float cy, float radius, Color color)
        {
            if (Sharp.Scale == 1f)
            {
                sb.Draw(Glow, new Rectangle((int)(cx - radius), (int)(cy - radius), (int)(radius * 2f), (int)(radius * 2f)), color);
                return;
            }
            //SHARP SCREEN : placed to the real pixel (not the game pixel), so a slow glow glides instead of stepping
            if (radius <= 0f) return;
            sb.Draw(Glow, new Vector2(cx - radius, cy - radius), null, color, 0f, Vector2.Zero, radius * 2f / Glow.Width, SpriteEffects.None, 0f);
        }

        //Draw Glow Box : the same soft light squashed into a rectangle, for wide halos
        public static void DrawGlowBox(SpriteBatch sb, Rectangle box, Color color)
        {
            sb.Draw(Glow, box, color);
        }

        //Draw Arrow : solid triangle, used for the left/right gallery buttons
        public static void Arrow(SpriteBatch sb, float cx, float cy, float size, bool pointRight, Color color)
        {
            float dot = Sharp.Dot;
            int columns = (int)(size * Sharp.Scale);
            for (int k = 0; k <= columns; k++)
            {
                float i = k * dot;
                float half = size - i;
                float x = pointRight ? cx - size / 2f + i : cx + size / 2f - i;
                Rect(sb, x, cy - half, dot, half * 2f + dot, color);
            }
        }

        //Draw Triangle : solid triangle pointing up or down, used for boost and ease marks
        public static void Triangle(SpriteBatch sb, float cx, float cy, float size, bool pointUp, Color color)
        {
            float dot = Sharp.Dot;
            int rows = (int)(size * Sharp.Scale);
            for (int k = 0; k <= rows; k++)
            {
                float i = k * dot;
                float half = size - i;
                float y = pointUp ? cy + size / 2f - i : cy - size / 2f + i;
                Rect(sb, cx - half, y, half * 2f + dot, dot, color);
            }
        }

        //Draw Diamond : a square turned on its corner, the game's main ornament
        public static void Diamond(SpriteBatch sb, float cx, float cy, float size, Color color)
        {
            float dot = Sharp.Dot;
            int rows = (int)(size * Sharp.Scale);
            for (int k = -rows; k <= rows; k++)
            {
                float y = k * dot;
                float half = size - Math.Abs(y);
                Rect(sb, cx - half, cy + y, half * 2f + dot, dot, color);
            }
        }

        //Draw Diamond Outline : the same shape, only the edge
        public static void DiamondOutline(SpriteBatch sb, float cx, float cy, float size, Color color, float thickness)
        {
            Line(sb, cx, cy - size, cx + size, cy, color, thickness);
            Line(sb, cx + size, cy, cx, cy + size, color, thickness);
            Line(sb, cx, cy + size, cx - size, cy, color, thickness);
            Line(sb, cx - size, cy, cx, cy - size, color, thickness);
        }

        //Draw Slanted Box : a box whose left and right sides lean, like a blade.
        //slant is how many pixels the top edge is pushed right compared to the bottom.
        public static void SlantBox(SpriteBatch sb, Rectangle r, int slant, Color color)
        {
            float dot = Sharp.Dot;
            int rows = (int)(r.Height * Sharp.Scale);
            for (int k = 0; k < rows; k++)
            {
                float y = k * dot;
                float shift = slant * (1f - y / r.Height);
                Rect(sb, r.X + shift, r.Y + y, r.Width - slant, dot, color);
            }
        }

        //Draw Slanted Outline : the edge of the same shape
        public static void SlantOutline(SpriteBatch sb, Rectangle r, int slant, Color color, float thickness)
        {
            float w = r.Width - slant;
            Line(sb, r.X + slant, r.Y, r.X + slant + w, r.Y, color, thickness);
            Line(sb, r.X, r.Bottom, r.X + w, r.Bottom, color, thickness);
            Line(sb, r.X + slant, r.Y, r.X, r.Bottom, color, thickness);
            Line(sb, r.X + slant + w, r.Y, r.X + w, r.Bottom, color, thickness);
        }

        //Text Wrap : cut a long sentence into lines that fit inside maxWidth.
        //DrawString already understands "\n" so we just insert them.
        public static string WrapText(SpriteFont font, string text, float maxWidth, float scale)
        {
            string[] words = text.Split(' ');
            string result = "";
            string line = "";

            for (int i = 0; i < words.Length; i++)
            {
                string test = line.Length == 0 ? words[i] : line + " " + words[i];

                if (font.MeasureString(test).X * scale > maxWidth && line.Length > 0)
                {
                    result += line + "\n";
                    line = words[i];
                }
                else
                {
                    line = test;
                }
            }
            return result + line;
        }

        //Text Wrap Limited : same as WrapText but never returns more than maxLines.
        //Whatever does not fit ends in three dots, so a caption can never spill out of
        //the card it was written for, no matter how long somebody types it.
        public static string WrapText(SpriteFont font, string text, float maxWidth, float scale, int maxLines)
        {
            string wrapped = WrapText(font, text, maxWidth, scale);
            string[] lines = wrapped.Split('\n');

            if (lines.Length <= maxLines) return wrapped;

            string result = "";
            for (int i = 0; i < maxLines; i++)
            {
                if (i > 0) result += "\n";
                result += lines[i];
            }
            return result + "...";
        }

        //Whole : where a line of text starts, on a whole pixel so the letters are not smeared. In the window that is a
        //whole game pixel, as it always was. On a bigger screen it is a whole REAL pixel (SHARP SCREEN), which is finer,
        //so spaced out letters keep even gaps.
        private static float Whole(float v)
        {
            return Sharp.Scale == 1f ? (int)v : Sharp.Snap(v);
        }

        //Draw Text : normal top-left text
        public static void Text(SpriteBatch sb, SpriteFont font, string s, float x, float y, Color color, float scale)
        {
            SharpText.Draw(sb, font, s, new Vector2(Whole(x), Whole(y)), color, 0f, Vector2.Zero, scale);
        }

        //Draw Text Centered : text centred on a point
        public static void TextCentered(SpriteBatch sb, SpriteFont font, string s, float cx, float cy, Color color, float scale)
        {
            Vector2 size = font.MeasureString(s) * scale;
            SharpText.Draw(sb, font, s, new Vector2(Whole(cx - size.X / 2f), Whole(cy - size.Y / 2f)),
                           color, 0f, Vector2.Zero, scale);
        }

        //Draw Text Right : text ending at a point
        public static void TextRight(SpriteBatch sb, SpriteFont font, string s, float rightX, float y, Color color, float scale)
        {
            Vector2 size = font.MeasureString(s) * scale;
            SharpText.Draw(sb, font, s, new Vector2(Whole(rightX - size.X), Whole(y)), color, 0f, Vector2.Zero, scale);
        }

        //Text Width : how wide a line will be
        public static float TextWidth(SpriteFont font, string s, float scale)
        {
            return font.MeasureString(s).X * scale;
        }

        //Spaced Width : how wide a line will be when drawn with TextSpaced
        public static float SpacedWidth(SpriteFont font, string s, float scale, float tracking)
        {
            float width = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                string letter = LetterOf(s[i]);
                width += font.MeasureString(letter).X * scale;
                if (i < s.Length - 1) width += tracking;
            }
            return width;
        }

        //Draw Spaced Text : letters pulled apart, the look of "W O R L D   V I E W".
        //tracking is the extra gap in pixels after every letter.
        public static void TextSpaced(SpriteBatch sb, SpriteFont font, string s, float x, float y, Color color, float scale, float tracking)
        {
            for (int i = 0; i < s.Length; i++)
            {
                string letter = LetterOf(s[i]);
                Text(sb, font, letter, x, y, color, scale);
                x += font.MeasureString(letter).X * scale + tracking;
            }
        }

        public static void TextSpacedCentered(SpriteBatch sb, SpriteFont font, string s, float cx, float y, Color color, float scale, float tracking)
        {
            TextSpaced(sb, font, s, cx - SpacedWidth(font, s, scale, tracking) / 2f, y, color, scale, tracking);
        }

        public static void TextSpacedRight(SpriteBatch sb, SpriteFont font, string s, float rightX, float y, Color color, float scale, float tracking)
        {
            TextSpaced(sb, font, s, rightX - SpacedWidth(font, s, scale, tracking), y, color, scale, tracking);
        }

        //Draw Spaced Text Turned (Beta1 screens) : spaced text leaning by angle (radians), the letters following the
        //turned line. (x, cy) is the left end of the line at the middle of the letters, which stays put as it turns.
        public static void TextSpacedTurned(SpriteBatch sb, SpriteFont font, string s, float x, float cy, Color color, float scale, float tracking, float angle)
        {
            Vector2 along = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            Vector2 origin = new Vector2(0f, font.LineSpacing / 2f);
            float run = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                string letter = LetterOf(s[i]);
                Vector2 at = new Vector2(x, cy) + along * run;
                SharpText.Draw(sb, font, letter, at, color, angle, origin, scale);
                run += font.MeasureString(letter).X * scale + tracking;
            }
        }

        //Draw Vertical Text : turned a quarter turn so it reads from top to bottom.
        //x is the RIGHT edge of the column, because the letters hang to the left of it.
        public static void TextVertical(SpriteBatch sb, SpriteFont font, string s, float x, float y, Color color, float scale)
        {
            SharpText.Draw(sb, font, s, new Vector2(Whole(x), Whole(y)), color, MathHelper.PiOver2, Vector2.Zero, scale);
        }

        private static string LetterOf(char c)
        {
            if (c < 32 || c > 126) return "?";
            return letters[c];
        }
    }
}
