using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //MARK WORDS (3 Nov, the user 13.150 : "the explanations are still too confusing, colour the words or explain them more simply,
    //the special words have to be easy to understand") : in every sentence the game draws (Gfx.Text with the story or the main
    //font, a line with small letters in it or with Thai), the rule words of Data/Words.cs (Words.Marked) are drawn in the colour
    //of their kind : red harms the enemy, blue protects, green heals, gold makes the band stronger, lavender stops the enemy,
    //warm white is what you gain or spend. The same colours as the skill kinds on the buttons (Palette.Kind).
    //A heading in capitals (a name, a title) is left as it is.
    //ADVANCED PART : the first time a line is drawn it is cut into PIECES once (the plain words between, each rule word on its
    //own, where each starts on its line, which line) and the pieces are kept, so drawing never makes a string (the rule of
    //CLAUDE.md 3). A Thai line is drawn piece by piece with its Thai partner font, the font the whole line would have used, so the
    //letters look exactly as before. A line with no rule word is remembered as such and drawn whole.
    public static class MarkWords
    {
        private class Piece
        {
            public string Text;
            public float X;          // where it starts on its line, in the font's own pixels (times scale when drawn)
            public int Line;         // which line of the text
            public int Kind;         // Words.MarkedKind, or -1 for the line's own colour
        }

        private static readonly Dictionary<string, Piece[]> pieces = new Dictionary<string, Piece[]>();
        private const int MostKept = 6000;              // a page that writes new lines every frame never fills the memory

        public static bool On = true;

        private static readonly Color[] kindColor =
        {
            Palette.KindAttack, Palette.KindDefend, Palette.KindHeal, Palette.KindBoost, Palette.SpotPrism, Palette.SpotHolyHot
        };

        //Draw : the text with its rule words coloured. False when it has none (the caller draws it whole as before).
        public static bool Draw(SpriteBatch sb, SpriteFont font, string s, float x, float y, Color color, float scale)
        {
            if (!On || s == null || s.Length < 6 || color.A == 0) return false;
            if (font != Ui.StoryFont && font != Ui.Font) return false;
            Piece[] cut;
            if (!pieces.TryGetValue(s, out cut))
            {
                if (pieces.Count >= MostKept) return false;
                cut = Cut(font, s);
                pieces[s] = cut;
            }
            if (cut == null) return false;

            //The colour of a rule word : its kind's, as bright and as see-through as the line it sits in
            float alpha = color.A / 255f;
            float bright = Math.Max(color.R, Math.Max(color.G, color.B)) / (255f * Math.Max(alpha, 0.01f));
            float shade = MathHelper.Clamp(bright / (Palette.Paper.R / 255f), 0.35f, 1f) * alpha;

            bool thai = ThaiText.Has(s);
            SpriteFont drawn = font;
            float size = 1f;
            float down = 0f;
            if (thai) drawn = ThaiText.FontFor(font, out size, out down);
            float lineStep = drawn.LineSpacing * size * scale;
            for (int i = 0; i < cut.Length; i++)
            {
                Piece p = cut[i];
                Color c = p.Kind < 0 ? color : kindColor[p.Kind] * shade;
                Vector2 at = new Vector2(x + p.X * scale, y + down * scale + p.Line * lineStep);
                SharpText.Draw(sb, drawn, p.Text, new Vector2(Gfx.Whole(at.X), Gfx.Whole(at.Y)), c, 0f, Vector2.Zero, scale * size);
            }
            return true;
        }

        //Cut : the pieces of one text, or null when it has no rule word (or is a heading)
        private static Piece[] Cut(SpriteFont font, string s)
        {
            if (!Sentence(s) || !HasMark(s)) return null;
            List<Piece> list = new List<Piece>();
            bool thai = ThaiText.Has(s);
            string[] lines = s.Split('\n');
            for (int line = 0; line < lines.Length; line++)
            {
                string l = lines[line];
                int start = 0;
                int i = 0;
                while (i < l.Length)
                {
                    int found = MatchAt(l, i);
                    if (found < 0)
                    {
                        i++;
                        continue;
                    }
                    int end = i + Words.Marked[found].Length;
                    if (i > start) Add(list, font, thai, l, start, i, line, -1);       // the plain words before it
                    Add(list, font, thai, l, i, end, line, Words.MarkedKind[found]);   // the rule word
                    i = end;
                    start = end;
                }
                if (start < l.Length) Add(list, font, thai, l, start, l.Length, line, -1);
            }
            return list.ToArray();
        }

        //Has Mark : a rule word somewhere in s
        private static bool HasMark(string s)
        {
            for (int i = 0; i < s.Length; i++)
                if (MatchAt(s, i) >= 0) return true;
            return false;
        }

        //Sentence : a line of small letters or Thai, not a heading in capitals
        private static bool Sentence(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if ((c >= 'a' && c <= 'z') || (c >= '฀' && c <= '๿')) return true;
            }
            return false;
        }

        //Match At : the longest rule word that starts at i as a whole word, or -1
        private static int MatchAt(string l, int i)
        {
            char c = l[i];
            if (c < 'A' || c > 'Z') return -1;
            if (i > 0 && char.IsLetter(l[i - 1])) return -1;
            for (int u = 0; u < Words.Unmarked.Length; u++)                       // inside a name : not a rule word here
            {
                string name = Words.Unmarked[u];
                int from = Math.Max(0, i - name.Length + 1);
                int at = l.IndexOf(name, from, StringComparison.Ordinal);
                if (at >= 0 && at <= i && i < at + name.Length) return -1;
            }
            int best = -1;
            int bestLength = 0;
            for (int w = 0; w < Words.Marked.Length; w++)
            {
                string word = Words.Marked[w];
                if (word.Length <= bestLength || i + word.Length > l.Length) continue;
                if (string.CompareOrdinal(l, i, word, 0, word.Length) != 0) continue;
                int after = i + word.Length;
                if (after < l.Length && char.IsLetter(l[after])) continue;
                best = w;
                bestLength = word.Length;
            }
            return best;
        }

        private static void Add(List<Piece> list, SpriteFont font, bool thai, string line, int from, int to, int lineIndex, int kind)
        {
            string before = line.Substring(0, from);
            float x = before.Length == 0 ? 0f : (thai ? ThaiText.SizeOf(font, before).X : font.MeasureString(before).X);
            list.Add(new Piece { Text = line.Substring(from, to - from), X = x, Line = lineIndex, Kind = kind });
        }
    }
}
