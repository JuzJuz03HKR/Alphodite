using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Core;

namespace Tacetno433.Data
{
    //Era : one of the times and places the run can travel to.
    //The era decides which musicians you meet and, later, the palette and background.
    public class Era
    {
        public string Name = "";
        public string Subtitle = "";
        public string Years = "";          // a small date line, like the editorial posters
        public string Description = "";

        //Prepared Text : wrapped once at startup, never while drawing
        public string DescriptionWrapped = "";
    }

    //EraList : THE PLACE TO EDIT ERAS.
    //Add one here and it turns up on the era choice page by itself. Musicians below
    //point at eras by their number in this list, so adding in the middle shifts them.
    //The pictures for each era (panel and duel stage) are listed in Core/ArtBank.cs.
    public static class EraList
    {
        public static Era[] All = new Era[]
        {
            //Era 0
            new Era
            {
                Name = "CLASSICAL",
                Subtitle = "ORDER AND SYMMETRY",
                Years = "1750 - 1820",
                Description = "Everything in its place, counted by a clock that never stops."
            },

            //Era 1
            new Era
            {
                Name = "SIAM",
                Subtitle = "GOLD AND HEAT",
                Years = "RATTANAKOSIN",
                Description = "Courtyard duels. The percussion here hits hardest."
            },

            //Era 2
            new Era
            {
                Name = "ROMANTIC",
                Subtitle = "EVERYTHING FELT",
                Years = "1820 - 1900",
                Description = "Bigger and louder. The breath bill comes later."
            },
        };

        //Text Prepare : called once from LoadContent after the font exists
        public static void PrepareText(SpriteFont storyFont, float wrapWidth)
        {
            for (int i = 0; i < All.Length; i++)
                All[i].DescriptionWrapped = Gfx.WrapText(storyFont, All[i].Description, wrapWidth, TextSize.StorySmall, PanelStrip.CaptionLines);
        }
    }
}
