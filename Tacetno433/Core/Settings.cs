namespace Tacetno433.Core
{
    //Settings : the options the player can change on the settings page.
    //
    //Everything here is a plain number or flag kept in memory. SaveFile writes them to a small
    //text file whenever the settings page closes, and reads them back when the game starts.
    //
    //Volume works like a mixing desk: Master is the main fader and the other two sit under
    //it, so a sound ends up playing at  its own volume * Sfx * Master.
    public static class Settings
    {
        //Volume : 0 to 1
        public static float Master = 0.8f;
        public static float Sfx = 0.8f;
        public static float Music = 0.55f;

        //Language : 0 English, 1 Thai.
        //NOTHING IS TRANSLATED YET. The page only remembers which one was picked, because
        //the fonts in the game hold English letters only. When Thai text arrives, every
        //screen reads its words through this number.
        public static int Language;
        public static string[] LanguageNames = { "ENGLISH", "THAI" };

        //Full Screen : applied by TacetGame.ApplyFullscreen
        public static bool Fullscreen;

        //MAESTRO MODE : the mode the next run starts in, picked on the conductor pages and
        //remembered for next time (29 Sep). false is NORMAL.
        public static bool Maestro;

        //Stroke Timing : seconds taken off every stroke before it is judged in a duel.
        //Every hand, mouse and screen is a little late in its own way. A player whose strokes
        //are always judged late moves this up, and the duel meets them where they are.
        //The calibration test on the settings page measures it for them.
        public const float TimingMost = 0.15f;      // plus or minus 150 milliseconds
        public const float TimingStep = 0.005f;     // moved 5 milliseconds at a time
        public static float TimingOffset;

        //Percent Text : "0%" to "100%" built once, so dragging a slider never makes a string
        private static string[] percents = new string[101];

        //Timing Text : "-150 MS" to "+150 MS" in steps of 5, built once for the same reason
        private static string[] timings = new string[61];

        //Settings Build : called once from LoadContent
        public static void Build()
        {
            for (int i = 0; i <= 100; i++) percents[i] = i + "%";
            for (int i = 0; i < timings.Length; i++)
            {
                int ms = (i - 30) * 5;
                timings[i] = (ms > 0 ? "+" : "") + ms + " MS";
            }
        }

        //Percent : the label for a 0..1 volume, rounded to a whole percent
        public static string Percent(float value)
        {
            int i = (int)(value * 100f + 0.5f);
            if (i < 0) i = 0;
            if (i > 100) i = 100;
            return percents[i];
        }

        //Timing Label : the label for a stroke timing in seconds
        public static string TimingLabel(float seconds)
        {
            int i = (int)System.Math.Round(seconds / TimingStep) + 30;
            if (i < 0) i = 0;
            if (i >= timings.Length) i = timings.Length - 1;
            return timings[i];
        }

        //Timing Set : kept inside the limits and on a 5 millisecond step
        public static void SetTiming(float seconds)
        {
            if (seconds < -TimingMost) seconds = -TimingMost;
            if (seconds > TimingMost) seconds = TimingMost;
            TimingOffset = (float)System.Math.Round(seconds / TimingStep) * TimingStep;
        }
    }
}
