using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //SharpText (Beta1, SHARP SCREEN) : sharp letters at any screen size.
    //
    //A SpriteFont is a picture of every letter at one size. Drawn bigger than it was made the letters go soft, and in full
    //screen every letter is drawn 1.5 to 3 times bigger (Sharp.Scale). So every font has bigger copies, built by the content
    //pipeline from the same font file (TWINS : MainFont_15x, MainFont_2x, MainFont_3x and the same for the other three).
    //The game loads the smallest twin that is at least as big as the screen needs, so letters are only ever drawn the size
    //they were made or smaller (sharp), never bigger. In the 1280 x 720 window there is no twin and nothing changes.
    //
    //ADVANCED PART : the twin draws the letters, but the ORIGINAL font still decides where every letter goes. Every
    //MeasureString and every wrapped line in the game was worked out with the original (TextSize, PrepareText), and a
    //bigger copy rounds its letter widths differently, so a line drawn wholly with the twin would come out a pixel or two
    //longer or shorter than measured and stop lining up. Here the pen moves exactly as SpriteBatch.DrawString moves it
    //with the original font (the same sums as MonoGame's own code), and at each step the twin's letter is put where its own
    //outline starts (its bearing divided by the twin's size), on a whole real pixel so it is never smeared between two.
    public static class SharpText
    {
        //Twin : one font of the game and its bigger copy
        private class Twin
        {
            public SpriteFont Font;                  // the original, the one every page holds and measures with
            public string Name;                      // its content name (MainFont ...)
            public SpriteFont Big;                   // the bigger copy, null when the screen does not need one
            public float Size;                       // how much bigger the copy was made (1.5, 2 or 3)
            public int[] Index = new int[128];       // a letter's place in Font.Glyphs, the same place in Big.Glyphs
            public int Fallback;                     // the place of DefaultCharacter ('?'), for any letter the font lacks
        }

        private static readonly Twin[] twins = new Twin[4];
        private static int count;
        private static ContentManager content;       // the twins' own loader, emptied when the screen size changes
        private static float loadedSize;

        //Twin Sizes : the copies that exist (Content/*_15x, *_2x, *_3x.spritefont) and the ending of their names
        private static readonly float[] sizes = { 1.5f, 2f, 3f };
        private static readonly string[] endings = { "_15x", "_2x", "_3x" };

        //Add : a font of the game, once at startup (after its DefaultCharacter is set)
        public static void Add(SpriteFont font, string name)
        {
            if (count >= twins.Length) return;
            Twin t = new Twin();
            t.Font = font;
            t.Name = name;
            t.Fallback = Math.Max(0, PlaceOf(font, font.DefaultCharacter ?? '?'));
            for (int c = 0; c < t.Index.Length; c++)
            {
                int place = PlaceOf(font, (char)c);
                t.Index[c] = place >= 0 ? place : t.Fallback;
            }
            twins[count++] = t;
        }

        //Choose : load the twins this screen size needs (called whenever Sharp.Scale changes). Twins that will not load
        //(missing files) leave that font as it is : soft in full screen, but the game runs.
        public static void Choose(IServiceProvider services, string root, float scale)
        {
            if (count == 0) return;                  // the fonts are not loaded yet, LoadContent calls again after them
            float size = 0f;
            for (int i = 0; i < sizes.Length && scale > 1.001f; i++)
            {
                size = sizes[i];
                if (scale <= sizes[i] + 0.001f) break;
            }
            if (size == loadedSize) return;

            //Unload : the copies for the old size are given back before the new ones come in
            for (int i = 0; i < count; i++) twins[i].Big = null;
            if (content != null) content.Unload();
            loadedSize = size;
            if (size <= 0f) return;

            if (content == null) content = new ContentManager(services, root);
            string ending = endings[Array.IndexOf(sizes, size)];
            for (int i = 0; i < count; i++)
            {
                Twin t = twins[i];
                t.Size = size;
                try
                {
                    SpriteFont big = content.Load<SpriteFont>(t.Name + ending);
                    if (big.Glyphs.Length == t.Font.Glyphs.Length) t.Big = big;        // the same letters, or no twin
                }
                catch (ContentLoadException)
                {
                    t.Big = null;
                }
            }
        }

        //Draw : SpriteBatch.DrawString, sharp at any screen size. The same arguments, no SpriteEffects.
        public static void Draw(SpriteBatch sb, SpriteFont font, string text, Vector2 position, Color color, float rotation, Vector2 origin, float scale)
        {
            Twin twin = Find(font);
            if (twin == null || twin.Big == null)
            {
                sb.DrawString(font, text, position, color, rotation, origin, scale, SpriteEffects.None, 0f);
                return;
            }

            SpriteFont.Glyph[] small = font.Glyphs;
            SpriteFont.Glyph[] big = twin.Big.Glyphs;
            Texture2D page = twin.Big.Texture;
            float shrink = 1f / twin.Size;
            float cos = 1f, sin = 0f;
            if (rotation != 0f)
            {
                cos = (float)Math.Cos(rotation);
                sin = (float)Math.Sin(rotation);
            }

            //The Pen : MonoGame's own sums with the original font (SpriteBatch.DrawString, SpriteFont.MeasureString)
            float penX = 0f;
            float penY = 0f;
            bool first = true;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\r') continue;
                if (c == '\n')
                {
                    penX = 0f;
                    penY += font.LineSpacing;
                    first = true;
                    continue;
                }

                int g = c < 128 ? twin.Index[c] : twin.Fallback;
                ref SpriteFont.Glyph s = ref small[g];
                ref SpriteFont.Glyph b = ref big[g];

                //Where the outline starts : the original's pen, plus the twin's own bearing shrunk to the original's size
                float inkX;
                if (first)
                {
                    penX = Math.Max(s.LeftSideBearing, 0f);
                    inkX = Math.Max(b.LeftSideBearing, 0f) * shrink;
                    first = false;
                }
                else
                {
                    float lead = penX + font.Spacing;
                    penX = lead + s.LeftSideBearing;
                    inkX = lead + b.LeftSideBearing * shrink;
                }
                float lx = (inkX + b.Cropping.X * shrink - origin.X) * scale;
                float ly = (penY + b.Cropping.Y * shrink - origin.Y) * scale;
                penX += s.Width + s.RightSideBearing;

                Vector2 at;
                if (rotation == 0f)
                    at = new Vector2(Sharp.Snap(position.X + lx), Sharp.Snap(position.Y + ly));
                else
                    at = new Vector2(position.X + lx * cos - ly * sin, position.Y + lx * sin + ly * cos);
                sb.Draw(page, at, b.BoundsInTexture, color, rotation, Vector2.Zero, scale * shrink, SpriteEffects.None, 0f);
            }
        }

        private static Twin Find(SpriteFont font)
        {
            for (int i = 0; i < count; i++)
                if (twins[i].Font == font) return twins[i];
            return null;
        }

        //Place Of : where a letter sits in the font's list (-1 when it is not there)
        private static int PlaceOf(SpriteFont font, char c)
        {
            for (int i = 0; i < font.Characters.Count; i++)
                if (font.Characters[i] == c) return i;
            return -1;
        }
    }
}
