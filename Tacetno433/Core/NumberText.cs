namespace Tacetno433.Core
{
    //NumberText : every number the game shows, turned into text ONCE at startup.
    //
    //Why : writing  "+" + 14  inside a draw or on every hit makes a brand new string each
    //time. That rubbish piles up and the garbage collector eventually pauses the game to
    //clean it. Looking the text up in a ready made array costs nothing.
    public static class NumberText
    {
        public const int Max = 999;

        private static string[] plain = new string[Max + 1];     // "0" .. "999"
        private static string[] plus = new string[Max + 1];      // "+0" .. "+999"
        private static string[] minus = new string[Max + 1];     // "-0" .. "-999"

        //Number Build : called once from LoadContent
        public static void Build()
        {
            for (int i = 0; i <= Max; i++)
            {
                plain[i] = i.ToString();
                plus[i] = "+" + i;
                minus[i] = "-" + i;
            }
        }

        //Number Get : plain number, clamped into range
        public static string Get(int value)
        {
            if (value < 0) return minus[Clamp(-value)];
            return plain[Clamp(value)];
        }

        //Number Signed : always shows a plus or a minus sign
        public static string Signed(int value)
        {
            if (value < 0) return minus[Clamp(-value)];
            return plus[Clamp(value)];
        }

        private static int Clamp(int value)
        {
            if (value > Max) return Max;
            return value;
        }
    }
}
