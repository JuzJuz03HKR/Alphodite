using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Tacetno433.Core;

namespace Tacetno433.Data
{
    //LANG (Beta1 2 Nov, block F3, docs/BETA1_DIRECTION.md 52) : the game in Thai.
    //
    //THE WORD LIST : Content/Text/thai.txt, one line each, the English exactly as the game has it, a TAB, then the Thai.
    //A line the list does not have stays English, so names (PAGANINI, CAPRICE No. 24) and the rule words (BURN, FINALE,
    //BREATH ...) are left out on purpose : the user, "special words only as a name or a heading" (13.39). "\n" in the list
    //is a new line, a zero width space (U+200B) a place a Thai line may break (Core/ThaiText.cs).
    //
    //HOW IT REACHES THE SCREEN :
    //   Apply   once at start, after the settings : every string field of the game's data (musicians, skills, gifts, events,
    //           enemies, conductors, the legends' words ...) and every static list of strings (the title's tips, HOW TO PLAY's
    //           pages ...) found in the list is swapped for its Thai, so the pages build their lines from Thai.
    //   Of      every line drawn or measured goes through Gfx, and Gfx asks Of first : a line found in the list is drawn in
    //           Thai (the words written straight into a page, the constant ones).
    //Changing the language needs the game to start again (SETTINGS asks), since the pages make their lines once.
    public static class Lang
    {
        private static readonly Dictionary<string, string> table = new Dictionary<string, string>();
        public static int Count { get { return table.Count; } }
        public static int Swapped;                       // how many strings Apply swapped (F3 shows it)

        //Active : the language the game started in (0 English, 1 Thai), read once by Load.
        //ONE LANGUAGE A START (3 Nov, the user 13.150 : "Thai and English are buggy, sometimes translated, sometimes not") :
        //Of used to read Settings.Language every time it was asked, while the game's data had been swapped once at start
        //(Apply). Changing the language in the pause menu (which waits for the next start) turned every line written into
        //the pages over at once and left every line of the data as it was, so the two languages mixed on the same page.
        //Now the whole game keeps the language it started in until it starts again.
        public static int Active;

        //Thai : the game started in Thai and the word list loaded
        public static bool Thai
        {
            get { return Active == 1 && table.Count > 0; }
        }

        //Of : the line in the game's language, the same line when the list does not have it
        public static string Of(string s)
        {
            if (s == null || Active != 1 || table.Count == 0) return s;
            string t;
            if (table.TryGetValue(s, out t)) return t;
            return OfNumbers(s);
        }

        //======================================================================================
        //ANY NUMBERS (3 Nov, the user 13.150 : "sometimes translated, sometimes not") : a line with a number in it was only found
        //when its number was the one in the list. Every balance change (a skill costing 4 life instead of 6, a passive up to 80
        //percent instead of 50) turned its line back to English, and a line made with a number of the moment (LIFE +21) never
        //had one. So every line of the list that holds numbers is also kept as a PATTERN, its numbers taken out (#), and a line
        //the list does not have is looked for by its pattern : found, its own numbers go into the Thai one after another.
        //ADVANCED PART : working the pattern out makes strings, so each line asked for is answered once and remembered
        //(answered), and the pages never pay for it twice. The memory is capped for pages that write a new number every frame.
        //======================================================================================
        private static readonly Dictionary<string, string> patterns = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> answered = new Dictionary<string, string>();
        private const int MostAnswered = 8000;
        private static readonly System.Text.RegularExpressions.Regex number = new System.Text.RegularExpressions.Regex(@"\d+(\.\d+)?");

        //Pattern : s with every number turned into #, and how many there were
        private static string Pattern(string s, out int count)
        {
            int found = 0;
            string p = number.Replace(s, m => { found++; return "#"; });
            count = found;
            return p;
        }

        private static bool HasDigit(string s)
        {
            for (int i = 0; i < s.Length; i++)
                if (s[i] >= '0' && s[i] <= '9') return true;
            return false;
        }

        //Learn Pattern : one pair of the list, when both sides hold the same count of numbers
        private static void LearnPattern(string en, string th)
        {
            if (!HasDigit(en)) return;
            int enCount;
            int thCount;
            string key = Pattern(en, out enCount);
            string value = Pattern(th, out thCount);
            if (enCount == thCount && !patterns.ContainsKey(key)) patterns[key] = value;
        }

        //Of Numbers : the line found by its pattern, its own numbers put in (or the line itself)
        private static string OfNumbers(string s)
        {
            if (patterns.Count == 0 || !HasDigit(s)) return s;
            string done;
            if (answered.TryGetValue(s, out done)) return done;
            int count;
            string thai;
            done = s;
            if (patterns.TryGetValue(Pattern(s, out count), out thai))
            {
                System.Text.RegularExpressions.MatchCollection numbers = number.Matches(s);
                System.Text.StringBuilder b = new System.Text.StringBuilder(thai.Length + 8);
                int next = 0;
                for (int i = 0; i < thai.Length; i++)
                {
                    if (thai[i] == '#' && next < numbers.Count) b.Append(numbers[next++].Value);
                    else b.Append(thai[i]);
                }
                done = b.ToString();
            }
            if (answered.Count < MostAnswered) answered[s] = done;
            return done;
        }

        //Load : read the word list (a missing file leaves the game English)
        public static void Load(string path)
        {
            Active = Settings.Language;
            table.Clear();
            patterns.Clear();
            answered.Clear();
            if (!File.Exists(path)) return;
            foreach (string raw in File.ReadAllLines(path))
            {
                if (raw.Length == 0 || raw[0] == '#') continue;
                int tab = raw.IndexOf('\t');
                if (tab <= 0 || tab >= raw.Length - 1) continue;
                string en = raw.Substring(0, tab).Replace("\\n", "\n");
                string th = raw.Substring(tab + 1).Replace("\\n", "\n");
                table[en] = th;
                LearnPattern(en, th);                                          // ANY NUMBERS
            }
        }

        //Apply : swap every string of the game's data the list has (ADVANCED PART : reflection, the program reading its own
        //fields by name). It walks the static fields of every class of the game (a list of strings has its items swapped, a
        //lone string swapped when it can be), and every object found in them that belongs to the game (a Skill, an Enemy, a
        //GameEvent's choices ...), each object once. Run once at start, before any page makes its lines.
        public static void Apply()
        {
            Swapped = 0;
            if (!Thai) return;
            HashSet<object> seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
            foreach (Type type in typeof(Lang).Assembly.GetTypes())
            {
                if (type.Namespace == null || !type.Namespace.StartsWith("Tacetno433")) continue;
                if (type.ContainsGenericParameters || type.Name.StartsWith("DebugShots")) continue;
                foreach (FieldInfo f in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (f.IsLiteral) continue;                                         // a const is swapped where it is drawn (Of)
                    object value;
                    try { value = f.GetValue(null); }
                    catch (Exception) { continue; }
                    if (value is string s)
                    {
                        if (f.IsInitOnly) continue;                                    // readonly : swapped where it is drawn (Of)
                        string t = Of(s);
                        if (!ReferenceEquals(t, s)) { f.SetValue(null, t); Swapped++; }
                    }
                    else Walk(value, seen, 0);
                }
            }
        }

#if DEBUG
        //Dump (TEXT DUMP, --shots textdump) : every string Apply would look at, and the constants too, each once, with the field
        //it was found in. For making the word list (docs/lang/make_keys.py), never used by the game.
        public static string Dump()
        {
            Dictionary<string, string> found = new Dictionary<string, string>();
            HashSet<object> seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
            foreach (Type type in typeof(Lang).Assembly.GetTypes())
            {
                if (type.Namespace == null || !type.Namespace.StartsWith("Tacetno433")) continue;
                if (type.ContainsGenericParameters || type.Name.StartsWith("DebugShots")) continue;
                foreach (FieldInfo f in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    object value;
                    try { value = f.IsLiteral ? f.GetRawConstantValue() : f.GetValue(null); }
                    catch (Exception) { continue; }
                    Collect(value, type.Name + "." + f.Name, found, seen, 0);
                }
            }
            System.Text.StringBuilder r = new System.Text.StringBuilder();
            foreach (KeyValuePair<string, string> pair in found)
                r.Append(pair.Value).Append('\t').Append(pair.Key.Replace("\r", "").Replace("\n", "\\n")).Append('\n');
            return r.ToString();
        }

        private static void Collect(object value, string where, Dictionary<string, string> found, HashSet<object> seen, int depth)
        {
            if (value == null || depth > 4) return;
            if (value is string s)
            {
                if (s.Length > 0 && !found.ContainsKey(s)) found[s] = where;
                return;
            }
            Type type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || value is Delegate) return;
            if (value is Array grid && grid.Rank > 1)
            {
                foreach (object item in grid) Collect(item, where + "[,]", found, seen, depth + 1);   // a table (rows and columns)
                return;
            }
            if (value is IList list)
            {
                if (value is Array && type.GetElementType().IsPrimitive) return;
                if (!seen.Add(value)) return;
                for (int i = 0; i < list.Count; i++) Collect(list[i], where + "[" + i + "]", found, seen, depth + 1);
                return;
            }
            if (type.IsValueType || type.Namespace == null || !type.Namespace.StartsWith("Tacetno433.Data")) return;
            if (!seen.Add(value)) return;
            foreach (FieldInfo f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                Collect(f.GetValue(value), where + "." + f.Name, found, seen, depth + 1);
        }
#endif

        //Walk : the strings inside one value (a list of strings, an object of the game, a list of those), a few levels deep
        private static void Walk(object value, HashSet<object> seen, int depth)
        {
            if (value == null || depth > 4) return;
            if (value is string[] words)
            {
                for (int i = 0; i < words.Length; i++)
                {
                    string t = Of(words[i]);
                    if (!ReferenceEquals(t, words[i])) { words[i] = t; Swapped++; }
                }
                return;
            }
            Type type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || value is Delegate) return;
            if (value is Array grid && grid.Rank > 1) return;                     // a table (rows and columns) : none holds words
            if (value is IList list && !(value is Array && type.GetElementType().IsPrimitive))
            {
                if (!seen.Add(value)) return;
                for (int i = 0; i < list.Count; i++)
                {
                    object item = list[i];
                    if (item is string s)
                    {
                        string t = Of(s);
                        if (!ReferenceEquals(t, s) && !list.IsReadOnly) { list[i] = t; Swapped++; }
                    }
                    else Walk(item, seen, depth + 1);
                }
                return;
            }
            if (type.IsValueType || type.Namespace == null || !type.Namespace.StartsWith("Tacetno433.Data")) return;
            if (!seen.Add(value)) return;
            foreach (FieldInfo f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                object inner = f.GetValue(value);
                if (inner is string s)
                {
                    string t = Of(s);
                    if (!ReferenceEquals(t, s)) { f.SetValue(value, t); Swapped++; }
                }
                else if (inner != null && !f.FieldType.IsPrimitive && !f.FieldType.IsEnum) Walk(inner, seen, depth + 1);
            }
        }
    }
}
