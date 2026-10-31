using System.Collections.Generic;
using System.Globalization;

namespace Tacetno433.Core
{
    //NumberText : every number the game shows, turned into text ONCE.
    //
    //Why : writing  "+" + 14  inside a draw or on every hit makes a brand new string each
    //time. That rubbish piles up and the garbage collector eventually pauses the game to
    //clean it. Looking the text up in a ready made array costs nothing.
    //
    //NO LIMIT (Beta1 31 Oct, docs/BETA1_DIRECTION.md 20, the user : "the screen does not show the number that really
    //happened"). Until then the table stopped at 999 and every bigger number was shown as 999 : a boss with 1560 life,
    //a TURN TOTAL past 999. Now every number shows as it is, with no limit and no separators (1560, 12345) :
    //   0 to Ready      made at startup like before. Every number a fight shows today is far under it.
    //   past Ready      made the first time it is asked for, then kept (Kept Text) so it is never made again.
    public static class NumberText
    {
        public const int Ready = 9999;                            // 0 .. 9999 made at startup, about 1 MB in all

        private static string[] plain = new string[Ready + 1];   // "0" .. "9999"
        private static string[] plus = new string[Ready + 1];    // "+0" .. "+9999"
        private static string[] minus = new string[Ready + 1];   // "-0" .. "-9999"

        //Number Build : called once from LoadContent
        public static void Build()
        {
            for (int i = 0; i <= Ready; i++)
            {
                plain[i] = i.ToString(CultureInfo.InvariantCulture);
                plus[i] = "+" + plain[i];
                minus[i] = "-" + plain[i];
            }
        }

        //Number Get : plain number, "-" in front when it is below zero
        public static string Get(int value)
        {
            if (value >= 0 && value <= Ready) return plain[value];
            if (value < 0 && value >= -Ready) return minus[-value];
            return Kept(value, false);
        }

        //Number Signed : always shows a plus or a minus sign
        public static string Signed(int value)
        {
            if (value >= 0 && value <= Ready) return plus[value];
            if (value < 0 && value >= -Ready) return minus[-value];
            return Kept(value, true);
        }

        //======================================================================================
        //ADVANCED PART : Kept Text, a cache for the numbers past the table.
        //A Dictionary remembers each big number's text the first time it is shown, so a boss with 12345 life drawn every
        //frame makes its "12345" once and then only looks it up. Plain and signed texts are kept apart (12345 and +12345).
        //The table is rebuilt from nothing if it ever holds KeptMost numbers (something counting up through thousands of
        //values), so it can never grow without end. The game draws on one thread, so one Dictionary is safe.
        //======================================================================================
        private const int KeptMost = 4096;
        private static readonly Dictionary<int, string> keptPlain = new Dictionary<int, string>();
        private static readonly Dictionary<int, string> keptSigned = new Dictionary<int, string>();

        private static string Kept(int value, bool signed)
        {
            Dictionary<int, string> kept = signed ? keptSigned : keptPlain;
            string text;
            if (kept.TryGetValue(value, out text)) return text;

            if (kept.Count >= KeptMost) kept.Clear();
            text = value.ToString(CultureInfo.InvariantCulture);        // "-12345" for a number below zero, int.MinValue too
            if (signed && value > 0) text = "+" + text;
            kept[value] = text;
            return text;
        }
    }
}
