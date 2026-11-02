using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //THAI TEXT (Beta1 2 Nov, block F3, docs/BETA1_DIRECTION.md 52) : drawing Thai with SpriteFont.
    //
    //Each font of the game has a Thai partner (Content/ThaiMainFont, ThaiBigFont, ThaiStoryFont : Chakra Petch and Trirong, the
    //fonts the user picked on 28 Oct). A line that holds any Thai letter is drawn and measured wholly with the partner, which
    //has English letters too. Every text call of the game goes through Gfx, and Gfx asks here, so no page has to know.
    //
    //ADVANCED PART : a SpriteFont is a picture of every letter, laid side by side. It knows nothing of how Thai stacks its
    //vowels and tone marks, which a word processor works out with the font's own rules (OpenType, "shaping"). The marks in
    //these fonts take no room and reach back over the letter before them, but MonoGame's font builder keeps only how wide a
    //mark is, not how far back it reaches, and puts its picture at the pen, after its letter. So :
    //   REACH   every mark (and SARA AM) is moved back by its true reach, read from the font file once by
    //           docs/lang/thai_marks.py (FONT TABLE below, in em) and turned into pixels by how big the built font is
    //           (the 44 consonants' widths added up, against the same sum in the file)
    //Then three cases a font's own rules would fix, measured from the font's own letters when it loads :
    //   STACK   a tone mark on top of an upper vowel (ที่ มั่น) or before SARA AM (น้ำ) would sit inside the vowel : it goes up
    //   TALL    over ป ฝ ฟ ฬ the marks would cut the letter's tall stroke : they move left
    //   DEEP    under ฎ ฏ a lower vowel would hit the letter's tail : it goes down
    //Thai also has no spaces between words, so a long Thai line in the word list (Data/Lang.cs) may carry a zero width space
    //(U+200B) where a line may break. It is never drawn and takes no room.
    public static class ThaiText
    {
        public const char Break = '\u200B';                  // a place a Thai line may break, never drawn

        //Partner : a font of the game and the Thai font that stands in for it
        private class Partner
        {
            public SpriteFont Latin;
            public SpriteFont Thai;
            public float Size = 1f;                          // drawn this much bigger (LogoFont borrows BigFont's partner)
            public float Down;                               // pixels of the Latin font : Thai letters moved onto its baseline
        }

        //Marks : how far each fixed mark moves, in pixels of that Thai font (STACK, TALL, DEEP)
        private class Marks
        {
            public SpriteFont Font;
            public int[] Index;                              // a letter's place in Font.Glyphs, for every letter up to the Thai block
            public float[] Reach = new float[0x80];          // REACH : pixels each letter of the Thai block moves back
            public float Raise;
            public float Shift;
            public float Push;
        }

        private static readonly Partner[] partners = new Partner[4];
        private static int partnerCount;
        private static readonly Marks[] marks = new Marks[4];
        private static int markCount;

        //Has : any Thai letter in the line (the Thai block, U+0E00 to U+0E7F)
        public static bool Has(string s)
        {
            if (s == null) return false;
            for (int i = 0; i < s.Length; i++)
                if (s[i] >= '\u0E00' && s[i] <= '\u0E7F') return true;
            return false;
        }

        //FONT TABLE (REACH) : made by docs/lang/thai_marks.py from Content/Fonts. For each Thai font file : the 44 consonants'
        //advances added up, then the letters that reach back and how far their picture's left edge lies before the pen (em).
        private class FontTable
        {
            public string File;
            public float Consonants;
            public int[] Letters;
            public float[] Left;

            public FontTable(string file, float consonants, int[] letters, float[] left)
            {
                File = file;
                Consonants = consonants;
                Letters = letters;
                Left = left;
            }
        }

        private static readonly FontTable[] tables =
        {
            new FontTable("ChakraPetch-Medium", 29.3080f, new int[] { 0x0E31, 0x0E33, 0x0E34, 0x0E35, 0x0E36, 0x0E37, 0x0E38, 0x0E39, 0x0E3A, 0x0E42, 0x0E43, 0x0E44, 0x0E47, 0x0E48, 0x0E49, 0x0E4A, 0x0E4B, 0x0E4C, 0x0E4D, 0x0E4E }, new float[] { -0.3670f, -0.2410f, -0.5170f, -0.5170f, -0.5170f, -0.5170f, -0.2800f, -0.4370f, -0.1880f, -0.0380f, -0.0380f, -0.0430f, -0.4610f, -0.1690f, -0.3800f, -0.4660f, -0.2620f, -0.3190f, -0.2450f, -0.3590f }),
            new FontTable("Trirong-SemiBold", 29.6640f, new int[] { 0x0E31, 0x0E33, 0x0E34, 0x0E35, 0x0E36, 0x0E37, 0x0E38, 0x0E39, 0x0E3A, 0x0E42, 0x0E43, 0x0E44, 0x0E47, 0x0E48, 0x0E49, 0x0E4A, 0x0E4B, 0x0E4C, 0x0E4D, 0x0E4E }, new float[] { -0.3630f, -0.2610f, -0.5450f, -0.5450f, -0.5450f, -0.5450f, -0.2680f, -0.4320f, -0.1940f, -0.0410f, -0.0440f, -0.0690f, -0.4840f, -0.1730f, -0.3780f, -0.4680f, -0.3150f, -0.3100f, -0.2610f, -0.2990f }),
            new FontTable("Trirong-Medium", 28.8780f, new int[] { 0x0E31, 0x0E33, 0x0E34, 0x0E35, 0x0E36, 0x0E37, 0x0E38, 0x0E39, 0x0E3A, 0x0E42, 0x0E43, 0x0E44, 0x0E47, 0x0E48, 0x0E49, 0x0E4A, 0x0E4B, 0x0E4C, 0x0E4D, 0x0E4E }, new float[] { -0.3470f, -0.2460f, -0.5280f, -0.5280f, -0.5280f, -0.5280f, -0.2570f, -0.4150f, -0.1810f, -0.0400f, -0.0390f, -0.0620f, -0.4690f, -0.1570f, -0.3470f, -0.4550f, -0.2950f, -0.2860f, -0.2460f, -0.2910f }),
        };

        //Pair : at startup, a font of the game and its Thai partner, and the font file it was built from (FONT TABLE).
        //lineShare : the partner's line spacing as a share of the Latin font's (Thai needs a little more room for its marks).
        //size : drawn this much bigger (LogoFont on BigFont's partner).
        public static void Pair(SpriteFont latin, SpriteFont thai, string file, float lineShare, float size)
        {
            if (latin == null || thai == null || partnerCount >= partners.Length) return;
            thai.DefaultCharacter = '?';
            Marks m = MarksOf(thai);
            if (m == null)
            {
                m = Measure(thai);
                Reach(m, thai, file);
                marks[markCount++] = m;
                thai.LineSpacing = (int)Math.Round(latin.LineSpacing * lineShare / size);
            }
            Partner p = new Partner();
            p.Latin = latin;
            p.Thai = thai;
            p.Size = size;
            p.Down = Baseline(latin) - Baseline(thai) * size;    // where 'x' stands in each, the Thai line moved to match
            partners[partnerCount++] = p;
        }

        //Reach : how far back each mark goes in this built font, from the font file's table. The size of an em in pixels is the
        //44 consonants' widths as built, against the same widths in the file.
        private static void Reach(Marks m, SpriteFont font, string file)
        {
            FontTable table = null;
            for (int i = 0; i < tables.Length; i++)
                if (tables[i].File == file) table = tables[i];
            if (table == null) return;
            float built = 0f;
            for (int c = 0x0E01; c <= 0x0E2E; c++)
            {
                SpriteFont.Glyph g = GlyphOf(font, (char)c);
                built += g.LeftSideBearing + g.Width + g.RightSideBearing;
            }
            float em = built / table.Consonants;
            for (int i = 0; i < table.Letters.Length; i++)
                m.Reach[table.Letters[i] - 0x0E00] = table.Left[i] * em;
        }

        //Baseline : how far below the top of a line the letters stand, read from the letter x (sitting on the line)
        private static float Baseline(SpriteFont font)
        {
            int i = font.Characters.IndexOf('x');
            if (i < 0) return font.LineSpacing * 0.75f;
            SpriteFont.Glyph g = font.Glyphs[i];
            return g.Cropping.Y + g.BoundsInTexture.Height;
        }

        //Measure Marks : STACK, TALL and DEEP read from the font's own letters
        private static Marks Measure(SpriteFont font)
        {
            Marks m = new Marks();
            m.Font = font;
            m.Index = new int[0x0E80];
            int fallback = Math.Max(0, font.Characters.IndexOf('?'));
            for (int c = 0; c < m.Index.Length; c++)
            {
                int place = font.Characters.IndexOf((char)c);
                m.Index[c] = place >= 0 ? place : fallback;
            }
            SpriteFont.Glyph ko = GlyphOf(font, '\u0E01');                 // ก, a plain letter
            SpriteFont.Glyph vowel = GlyphOf(font, '\u0E34');              // ิ, an upper vowel
            SpriteFont.Glyph tone = GlyphOf(font, '\u0E48');               // ่, a tone mark
            SpriteFont.Glyph tall = GlyphOf(font, '\u0E1B');               // ป, a tall letter
            SpriteFont.Glyph deep = GlyphOf(font, '\u0E0E');               // ฎ, a letter with a tail
            float letter = Math.Max(1f, ko.BoundsInTexture.Height);
            float gap = letter * 0.1f;

            //STACK : the tone mark's foot goes just above the vowel's top
            float toneFoot = tone.Cropping.Y + tone.BoundsInTexture.Height;
            m.Raise = Math.Max(0f, toneFoot - vowel.Cropping.Y + gap);

            //TALL : about a sixth of the tall letter's width, the room its stroke takes
            m.Shift = (tall.LeftSideBearing + tall.Width + tall.RightSideBearing) * 0.17f;

            //DEEP : the lower vowel goes below the tail
            float line = ko.Cropping.Y + ko.BoundsInTexture.Height;
            m.Push = Math.Max(0f, deep.Cropping.Y + deep.BoundsInTexture.Height - line + gap * 0.5f);
            return m;
        }

        private static SpriteFont.Glyph GlyphOf(SpriteFont font, char c)
        {
            int i = font.Characters.IndexOf(c);
            if (i < 0) i = Math.Max(0, font.Characters.IndexOf('?'));
            return font.Glyphs[i];
        }

        private static Marks MarksOf(SpriteFont font)
        {
            for (int i = 0; i < markCount; i++)
                if (marks[i].Font == font) return marks[i];
            return null;
        }

        private static Partner PartnerOf(SpriteFont font)
        {
            for (int i = 0; i < partnerCount; i++)
                if (partners[i].Latin == font || partners[i].Thai == font) return partners[i];
            return null;
        }

        //Font For : the font a Thai line is drawn with in place of font, how much bigger, and how far down (Latin pixels)
        public static SpriteFont FontFor(SpriteFont font, out float size, out float down)
        {
            Partner p = PartnerOf(font);
            if (p == null)
            {
                size = 1f;
                down = 0f;
                return font;
            }
            size = p.Latin == font ? p.Size : 1f;
            down = p.Latin == font ? p.Down : 0f;
            return p.Thai;
        }

        //Glyph Index : where letter c sits in a Thai font's Glyphs (the '?' for a letter it does not have)
        public static int GlyphIndex(SpriteFont thai, char c)
        {
            Marks m = MarksOf(thai);
            if (m == null) return Math.Max(0, thai.Characters.IndexOf(c));
            return c < m.Index.Length ? m.Index[c] : m.Index['?'];
        }

        //Is Thai Font : one of the partners (SharpText fixes the marks only for these)
        public static bool IsThaiFont(SpriteFont font)
        {
            return MarksOf(font) != null;
        }

        //======================================================================================
        //Where each mark goes (STACK, TALL, DEEP)
        //======================================================================================

        private static bool IsUpper(char c)                   // ั ิ ี ึ ื ็ ํ
        {
            return c == '\u0E31' || (c >= '\u0E34' && c <= '\u0E37') || c == '\u0E47' || c == '\u0E4D';
        }

        private static bool IsTone(char c)                    // ่ ้ ๊ ๋ ์ ๎
        {
            return (c >= '\u0E48' && c <= '\u0E4C') || c == '\u0E4E';
        }

        private static bool IsLower(char c)                   // ุ ู ฺ
        {
            return c >= '\u0E38' && c <= '\u0E3A';
        }

        private static bool IsMark(char c)
        {
            return IsUpper(c) || IsTone(c) || IsLower(c);
        }

        //Takes No Room : a mark (no width of its own), so no letter spacing is added before it either
        public static bool TakesNoRoom(char c)
        {
            return IsMark(c);
        }

        //Base Of : the letter the mark at i sits on, the nearest one before it that is not a mark
        private static char BaseOf(string s, int i)
        {
            for (int k = i - 1; k >= 0; k--)
                if (!IsMark(s[k])) return s[k];
            return ' ';
        }

        //Offset : how far the letter at i moves from where the font puts it, in pixels of the Thai font
        public static Vector2 Offset(SpriteFont font, string s, int i)
        {
            char c = s[i];
            if (c < '\u0E31' || c > '\u0E4E') return Vector2.Zero;
            Marks m = MarksOf(font);
            if (m == null) return Vector2.Zero;
            float back = m.Reach[c - 0x0E00];                                              // REACH

            char under = BaseOf(s, i);
            bool tall = under == '\u0E1B' || under == '\u0E1D' || under == '\u0E1F' || under == '\u0E2C';   // ป ฝ ฟ ฬ
            if (IsTone(c))
            {
                char before = i > 0 ? s[i - 1] : ' ';
                char after = i + 1 < s.Length ? s[i + 1] : ' ';
                float up = IsUpper(before) || after == '\u0E33' ? -m.Raise : 0f;      // STACK (ำ is SARA AM)
                return new Vector2(back + (tall ? -m.Shift : 0f), up);                  // TALL
            }
            if (IsUpper(c)) return new Vector2(back + (tall ? -m.Shift : 0f), 0f);       // TALL
            if (IsLower(c) && (under == '\u0E0E' || under == '\u0E0F')) return new Vector2(back, m.Push);   // DEEP (ฎ ฏ)
            return new Vector2(back, 0f);
        }

        //======================================================================================
        //Measuring a Thai line
        //======================================================================================

        //Size Of : SpriteFont.MeasureString with the partner font, the same sums as MonoGame's own, but a break (U+200B) takes
        //no room and every line is one line spacing tall. In the pixels of font (the Latin one asked for).
        public static Vector2 SizeOf(SpriteFont font, string s)
        {
            float size;
            float down;
            SpriteFont thai = FontFor(font, out size, out down);
            SpriteFont.Glyph[] glyphs = thai.Glyphs;
            float width = 0f;
            float penX = 0f;
            int lines = 1;
            bool first = true;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '\r' || c == Break) continue;
                if (c == '\n')
                {
                    penX = 0f;
                    lines++;
                    first = true;
                    continue;
                }
                SpriteFont.Glyph g = glyphs[GlyphIndex(thai, c)];
                if (first)
                {
                    penX = Math.Max(g.LeftSideBearing, 0f);
                    first = false;
                }
                else penX += (TakesNoRoom(c) ? 0f : thai.Spacing) + g.LeftSideBearing;
                penX += g.Width;
                width = Math.Max(width, penX + Math.Max(g.RightSideBearing, 0f));
                penX += g.RightSideBearing;
            }
            return new Vector2(width, lines * thai.LineSpacing) * size;
        }
    }
}
