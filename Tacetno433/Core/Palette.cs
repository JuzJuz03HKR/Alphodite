using Microsoft.Xna.Framework;

namespace Tacetno433.Core
{
    //Palette : every colour in the game lives here.
    //
    //RIGHT NOW THE WHOLE GAME IS GREYSCALE ON PURPOSE.
    //When colour comes back, only this one file has to change and every screen follows.
    //
    //The art rule behind these values: about 70 percent of the screen stays near black,
    //greys carry the information, and pure white is saved for the one thing the player
    //should look at. That is what makes a silhouette readable from across the room.
    public static class Palette
    {
        //Base Tones : dark to light
        public static Color Void = new Color(4, 4, 5);            // TACET, the silence
        public static Color StageDeep = new Color(13, 13, 15);
        public static Color Panel = new Color(20, 20, 23);        // cards and plates on the dark pages
        public static Color Stage = new Color(30, 30, 33);
        public static Color LineGrey = new Color(92, 92, 96);
        public static Color PaperDim = new Color(152, 152, 154);
        public static Color Paper = new Color(226, 226, 222);

        //Light Stage Tones : the duel page and the victory page flip the scheme.
        //Our side is bright paper, TACET is black, so these are the inks used on the bright side.
        public static Color Ink = new Color(20, 20, 22);
        public static Color InkSoft = new Color(96, 96, 100);
        public static Color StageLight = new Color(216, 215, 210);
        public static Color StageLightDeep = new Color(180, 179, 174);

        //Focus Tone : the brightest thing on screen, use it sparingly
        public static Color Highlight = new Color(255, 255, 255);

        //Accent : THE ONE COLOUR SWITCH.
        //Selected things, the combo counter and warnings use this. It is white while the game
        //is greyscale. Change these two lines (for example to a deep red) and every accent follows.
        public static Color Accent = new Color(255, 255, 255);
        public static Color AccentDim = new Color(160, 160, 160);

        //Meaning Tones : greyscale stand-ins until colour returns
        public static Color Warning = new Color(196, 196, 196);
        public static Color Good = new Color(210, 210, 210);
        public static Color Stamina = new Color(180, 180, 180);

        //Gallery Tones : the room the conductors hang in
        public static Color FrameGold = new Color(120, 120, 122);
        public static Color FrameGoldDark = new Color(62, 62, 66);
        public static Color Curtain = new Color(54, 54, 58);
        public static Color CurtainDark = new Color(22, 22, 25);
        public static Color CanvasDark = new Color(24, 24, 27);

        //Character Tones : each conductor gets its own brightness so they stay
        //telling apart in greyscale. These become real colours later.
        public static Color ToneA = new Color(255, 255, 255);
        public static Color ToneB = new Color(205, 205, 205);
        public static Color ToneC = new Color(168, 168, 168);
        public static Color ToneD = new Color(130, 130, 130);
        public static Color ToneE = new Color(98, 98, 98);

        //Culture Tones : kept by name so the duel page can use them later.
        //Greyscale for now, same as everything else.
        public static Color CultureSiam = new Color(220, 220, 220);
        public static Color CultureWest = new Color(180, 180, 180);
        public static Color CultureIndia = new Color(140, 140, 140);
        public static Color CultureEastAsia = new Color(105, 105, 105);
    }
}
