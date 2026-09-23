namespace Tacetno433.Core
{
    //Settings : the options the player can change on the settings page.
    //
    //Everything here is a plain number or flag kept in memory. The game has no save file
    //yet, so these go back to the values written below every time the game starts.
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

        //Percent Text : "0%" to "100%" built once, so dragging a slider never makes a string
        private static string[] percents = new string[101];

        //Settings Build : called once from LoadContent
        public static void Build()
        {
            for (int i = 0; i <= 100; i++) percents[i] = i + "%";
        }

        //Percent : the label for a 0..1 volume, rounded to a whole percent
        public static string Percent(float value)
        {
            int i = (int)(value * 100f + 0.5f);
            if (i < 0) i = 0;
            if (i > 100) i = 100;
            return percents[i];
        }
    }
}
