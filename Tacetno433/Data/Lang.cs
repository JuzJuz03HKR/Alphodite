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

        //Thai : the player picked Thai and the word list loaded
        public static bool Thai
        {
            get { return Settings.Language == 1 && table.Count > 0; }
        }

        //Of : the line in the player's language, the same line when the list does not have it
        public static string Of(string s)
        {
            if (s == null || Settings.Language != 1 || table.Count == 0) return s;
            string t;
            return table.TryGetValue(s, out t) ? t : s;
        }

        //Load : read the word list (a missing file leaves the game English)
        public static void Load(string path)
        {
            table.Clear();
            if (!File.Exists(path)) return;
            foreach (string raw in File.ReadAllLines(path))
            {
                if (raw.Length == 0 || raw[0] == '#') continue;
                int tab = raw.IndexOf('\t');
                if (tab <= 0 || tab >= raw.Length - 1) continue;
                string en = raw.Substring(0, tab).Replace("\\n", "\n");
                string th = raw.Substring(tab + 1).Replace("\\n", "\n");
                table[en] = th;
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
