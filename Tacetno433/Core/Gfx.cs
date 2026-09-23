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
        public static void Rect(SpriteBatch sb, float x, float y, float w, float h, Color color)
        {
            sb.Draw(Pixel, new Rectangle((int)x, (int)y, (int)w, (int)h), color);
        }

        public static void Rect(SpriteBatch sb, Rectangle r, Color color)
        {
            sb.Draw(Pixel, r, color);
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
        public static void Line(SpriteBatch sb, Vector2 a, Vector2 b, Color color, float thickness)
        {
            Vector2 diff = b - a;
            float length = diff.Length();
            if (length < 0.5f) return;
            float angle = (float)Math.Atan2(diff.Y, diff.X);
            sb.Draw(Pixel, a, null, color, angle, new Vector2(0f, 0.5f),
                    new Vector2(length, thickness), SpriteEffects.None, 0f);
        }

        //Draw Line (numbers) : same as above without building the two points first
        public static void Line(SpriteBatch sb, float x1, float y1, float x2, float y2, Color color, float thickness)
        {
            Line(sb, new Vector2(x1, y1), new Vector2(x2, y2), color, thickness);
        }

        //Draw Circle : filled circle built from horizontal lines
        public static void Circle(SpriteBatch sb, float cx, float cy, float radius, Color color)
        {
            for (int y = -(int)radius; y <= (int)radius; y++)
            {
                float inside = radius * radius - y * y;
                if (inside < 0f) continue;
                int half = (int)Math.Sqrt(inside);
                Rect(sb, cx - half, cy + y, half * 2 + 1, 1, color);
            }
        }

        //Draw Circle Outline : a ring, built from short straight segments
        public static void CircleOutline(SpriteBatch sb, float cx, float cy, float radius, Color color, float thickness)
        {
            int segments = 36;
            Vector2 previous = new Vector2(cx + radius, cy);

            for (int i = 1; i <= segments; i++)
            {
                float angle = MathHelper.TwoPi * i / segments;
                Vector2 now = new Vector2(cx + (float)Math.Cos(angle) * radius, cy + (float)Math.Sin(angle) * radius);
                Line(sb, previous, now, color, thickness);
                previous = now;
            }
        }

        //Draw Ellipse Outline : a ring squashed flat, like a ripple seen on the floor.
        //rx is half the width, ry half the height.
        public static void EllipseOutline(SpriteBatch sb, float cx, float cy, float rx, float ry, Color color, float thickness)
        {
            int segments = 32;
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
            int segments = 8;
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
            int caps = (int)radius;

            for (int y = 0; y < caps; y++)
            {
                float dy = caps - y;
                int half = (int)Math.Sqrt(Math.Max(0f, radius * radius - dy * dy));
                Rect(sb, cx - half, r.Y + y, half * 2 + 1, 1, color);                 // top cap row
                Rect(sb, cx - half, r.Bottom - 1 - y, half * 2 + 1, 1, color);        // bottom cap row
            }
            Rect(sb, cx - radius, r.Y + caps, radius * 2f + 1f, r.Height - caps * 2, color);
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

            for (int y = 0; y < r.Height; y++)
            {
                float dy = y + 0.5f - radius;
                float half = (float)Math.Sqrt(Math.Max(0f, radius * radius - dy * dy));
                Rect(sb, r.X + radius - half, r.Y + y, straight + half * 2f, 1, color);
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
            sb.Draw(Glow, new Rectangle((int)(cx - radius), (int)(cy - radius), (int)(radius * 2f), (int)(radius * 2f)), color);
        }

        //Draw Glow Box : the same soft light squashed into a rectangle, for wide halos
        public static void DrawGlowBox(SpriteBatch sb, Rectangle box, Color color)
        {
            sb.Draw(Glow, box, color);
        }

        //Draw Arrow : solid triangle, used for the left/right gallery buttons
        public static void Arrow(SpriteBatch sb, float cx, float cy, float size, bool pointRight, Color color)
        {
            for (int i = 0; i <= (int)size; i++)
            {
                float half = size - i;
                float x = pointRight ? cx - size / 2f + i : cx + size / 2f - i;
                Rect(sb, x, cy - half, 1, half * 2 + 1, color);
            }
        }

        //Draw Triangle : solid triangle pointing up or down, used for boost and ease marks
        public static void Triangle(SpriteBatch sb, float cx, float cy, float size, bool pointUp, Color color)
        {
            for (int i = 0; i <= (int)size; i++)
            {
                float half = size - i;
                float y = pointUp ? cy + size / 2f - i : cy - size / 2f + i;
                Rect(sb, cx - half, y, half * 2 + 1, 1, color);
            }
        }

        //Draw Diamond : a square turned on its corner, the game's main ornament
        public static void Diamond(SpriteBatch sb, float cx, float cy, float size, Color color)
        {
            for (int y = -(int)size; y <= (int)size; y++)
            {
                float half = size - Math.Abs(y);
                Rect(sb, cx - half, cy + y, half * 2f + 1f, 1, color);
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
            for (int y = 0; y < r.Height; y++)
            {
                float shift = slant * (1f - (float)y / r.Height);
                Rect(sb, r.X + shift, r.Y + y, r.Width - slant, 1, color);
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

        //Draw Text : normal top-left text
        public static void Text(SpriteBatch sb, SpriteFont font, string s, float x, float y, Color color, float scale)
        {
            sb.DrawString(font, s, new Vector2((int)x, (int)y), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        //Draw Text Centered : text centred on a point
        public static void TextCentered(SpriteBatch sb, SpriteFont font, string s, float cx, float cy, Color color, float scale)
        {
            Vector2 size = font.MeasureString(s) * scale;
            sb.DrawString(font, s, new Vector2((int)(cx - size.X / 2f), (int)(cy - size.Y / 2f)),
                          color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        //Draw Text Right : text ending at a point
        public static void TextRight(SpriteBatch sb, SpriteFont font, string s, float rightX, float y, Color color, float scale)
        {
            Vector2 size = font.MeasureString(s) * scale;
            sb.DrawString(font, s, new Vector2((int)(rightX - size.X), (int)y), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
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

        //Draw Vertical Text : turned a quarter turn so it reads from top to bottom.
        //x is the RIGHT edge of the column, because the letters hang to the left of it.
        public static void TextVertical(SpriteBatch sb, SpriteFont font, string s, float x, float y, Color color, float scale)
        {
            sb.DrawString(font, s, new Vector2((int)x, (int)y), color, MathHelper.PiOver2, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        private static string LetterOf(char c)
        {
            if (c < 32 || c > 126) return "?";
            return letters[c];
        }
    }
}
