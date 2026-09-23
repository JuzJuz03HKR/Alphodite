namespace Tacetno433.Core
{
    //TextSize : one type scale for the whole game.
    //
    //Every screen picks a size from this list instead of writing its own number, so the
    //hierarchy stays the same everywhere: the player always knows a Heading is a heading.
    //
    //The game has four fonts, all built from fonts that ship with Windows:
    //   Game.LogoFont   Palatino, very large   the logo and the big result words
    //   Game.BigFont    Palatino               names and page titles
    //   Game.StoryFont  Palatino, small        sentences: stories, captions, dialogue
    //   Game.Font       Bahnschrift            numbers, labels, buttons
    //
    //Each font is built LARGER than it is shown and drawn scaled down, which keeps the
    //letters sharp. Scaling a font up past 1 makes it blurry, so none of these go above 1.
    public static class TextSize
    {
        //LogoFont Sizes
        public const float Logo = 1.0f;       // the game name on the title page
        public const float Banner = 0.62f;    // VICTORY, ROUND 1, big moments

        //BigFont Sizes
        public const float Hero = 1.0f;       // a character name on a profile page
        public const float Title = 0.72f;     // page headings
        public const float Subtitle = 0.54f;  // names on cards, the line under a title
        public const float Small = 0.42f;     // serif labels inside small cards

        //Font Sizes
        public const float Heading = 0.92f;   // button words, section headings
        public const float Body = 0.72f;      // values and short lines
        public const float Label = 0.6f;      // captions, hints, small tags
        public const float Tiny = 0.52f;      // widely spaced small capitals, was 0.5

        //StoryFont Sizes
        public const float Story = 0.92f;     // sentences. MUST match the scale used when the
                                              // text is prepared, or wrapped text overflows
        public const float StorySmall = 0.8f; // captions on panels and cards
    }
}
