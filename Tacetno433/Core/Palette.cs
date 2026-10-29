using System;
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
        //Readable Greys : small labels are drawn in these two, so they sit high enough above
        //the black to be read on a projector, and still well under white. Was 92 and 152.
        public static Color LineGrey = new Color(118, 118, 122);
        public static Color PaperDim = new Color(174, 174, 176);
        public static Color Paper = new Color(226, 226, 222);

        //Light Stage Tones : the duel page and the victory page flip the scheme.
        //Our side is bright paper, TACET is black, so these are the inks used on the bright side.
        public static Color Ink = new Color(20, 20, 22);
        public static Color InkSoft = new Color(96, 96, 100);
        public static Color StageLight = new Color(216, 215, 210);
        public static Color StageLightDeep = new Color(180, 179, 174);

        //Focus Tone : the brightest thing on screen, use it sparingly
        public static Color Highlight = new Color(255, 255, 255);

        //Spot Colours (Beta1 round 4) : a musician's own colour, for their VFX and their damage numbers (SpotLook in
        //Data/Musician.cs). The user picks each character's colour when the art is done. For now the two looks the
        //user asked to try : GOLD with golden sparkles and RED with flames. Hot is the bright core of a blow.
        public static Color SpotGold = new Color(255, 186, 52);
        public static Color SpotGoldHot = new Color(255, 236, 160);
        public static Color SpotRed = new Color(255, 48, 30);
        public static Color SpotRedHot = new Color(255, 160, 60);

        //Lightning Blue (round 6, the user asked) : THE UNHEARING's LIGHTNING glows blue. The glow, then its hot core.
        public static Color LightningBlue = new Color(40, 170, 255);
        public static Color LightningBlueHot = new Color(170, 230, 255);

        //Conductor Colours (Beta1, 29 Oct, the user gave each conductor a theme, docs/CHARACTER_DESIGN.md 4) : each of the
        //three conductors' own colour, for the parts of the pages that are theirs (Conductor.Spot : the gallery, the profile,
        //the band page) and for their AURA round the painting (Core/ConductorAura.cs).
        //   STORM   THE UNHEARING : the blue of lightning, the same blue as his LIGHTNING (LightningBlue above)
        //   PRISM   FANNY MENDELSSOHN AND ??? : the white of a crystal, which breaks into the rainbow (Prism below) as it moves
        //   HOLY    THE METRONOME : the gold of a holy light, paler than a musician's GOLD so the two never mix up. Hot is its core.
        public static Color SpotPrism = new Color(214, 206, 255);
        public static Color SpotHoly = new Color(255, 204, 104);
        public static Color SpotHolyHot = new Color(255, 242, 200);

        //Prism : the rainbow a crystal throws, going once round all the colours for every 1 that t grows. Mix it with white
        //for the pale shimmer of the crystal itself (Conductor.SpotAt).
        //ADVANCED PART (small) : a colour wheel without HSV. Three waves a third of a turn apart give the red, the green
        //and the blue, so while one rises another falls and the colour walks round the rainbow.
        public static Color Prism(float t)
        {
            float turn = t * MathHelper.TwoPi;
            float r = 0.5f + 0.5f * (float)Math.Cos(turn);
            float g = 0.5f + 0.5f * (float)Math.Cos(turn - 2.0944f);
            float b = 0.5f + 0.5f * (float)Math.Cos(turn + 2.0944f);
            return new Color(r, g, b);
        }

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
        public static Color CurtainLight = new Color(94, 94, 100);      // the fold of the stage curtain that turns to the light (Core/Curtain.cs)
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
