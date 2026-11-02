namespace Tacetno433.Data
{
    //Credit : one line of the CREDITS page. Who made it, what of theirs is in the game, and the licence it came under.
    public class Credit
    {
        public string Who;          // the maker, in capitals
        public string What;         // what of theirs the game uses, one or two short sentences
        public string License;      // the small tag on the right : CC0, CC BY 4.0, OFL, MS-PL, FREE, OURS

        public Credit(string who, string what, string license)
        {
            Who = who;
            What = what;
            License = license;
        }
    }

    //Credits (Beta1 2 Nov, block E, docs/BETA1_DIRECTION.md 48) : THE PLACE TO EDIT THE CREDITS PAGE (Screens/CreditsScreen.cs).
    //Four pages : the game, the music, the sounds and the fonts. docs/CREDITS.md keeps the longer list with every source's
    //address, docs/audio/MANIFEST.md every sound file and what it was made from. A new sound, track or font goes in all three.
    //
    //CC BY needs the maker, the work and the licence shown where the players can find them : Joshua Empyre's track below.
    //TEAM PICKS are the free sounds the team found on free sound sites and checked (the user 13.138). They live in
    //Content/Audio/Borrowed, outside the public repo, because the site of each file was not written down.
    public static class Credits
    {
        public static readonly string[] Pages = { "THE GAME", "MUSIC", "SOUND", "FONTS" };
        public static readonly string[] Numbers = { "01", "02", "03", "04" };
        public static readonly string[] Titles = { "TACET 4'33", "The score", "Every hit and step", "The type" };
        public static readonly string[] Notes =
        {
            "A band against silence. Thank you for playing.",
            "Every track here is free to use. Thank you to the people who share their music.",
            "Ready made sounds, cut and levelled for TACET.",
            "All four fonts are free under the SIL Open Font License 1.1.",
        };

        public static readonly Credit[][] Lines =
        {
            //01 THE GAME
            new Credit[]
            {
                new Credit("TEAM ALPHODITE", "The game : its design, its code, its battle drawn in shapes, and the conductors' paintings.", "OURS"),
                new Credit("MONOGAME", "The framework the game runs on. monogame.net", "MS-PL"),
            },

            //02 MUSIC
            new Credit[]
            {
                new Credit("SZEGVARI", "Seven tracks from freesound.org : the title, the gallery, the route, the fights, the victory and the defeat.", "CC0"),
                new Credit("JOSHUA EMPYRE", "\"Epic Orchestra - LOOP\", freesound.org 250856 : a boss's second half. creativecommons.org/licenses/by/4.0", "CC BY 4.0"),
                new Credit("TEAM PICKS", "The title and boss tracks of the playtest, found on free sound sites.", "FREE"),
            },

            //03 SOUND
            new Credit[]
            {
                new Credit("VERSILIAN STUDIOS", "VSCO 2 Community Edition : the musicians' own notes, the orchestra's hits, gongs and bells. Recorded by Sam Gossner and Simon Dalzell, cut by Elan Hickler (Soundemote).", "CC0"),
                new Credit("KENNEY", "Interface Sounds, RPG Audio, Impact Sounds and Sci-fi Sounds. kenney.nl", "CC0"),
                new Credit("UNTAMEDPIXELS", "150 Combat Sound Effects, with sounds by lentikula and CoMiGo.", "CC0"),
                new Credit("RUBBERDUCK", "80 CC0 RPG SFX, 75 breaking, falling and hit SFX, 100 CC0 SFX #2.", "CC0"),
                new Credit("STARNINJAS, ARTISTICDUDE, JAGGEDSTONE", "20 Sword Sound Effects, Swishes Sound Pack, Magic Spell SFX. All from OpenGameArt.org", "CC0"),
                new Credit("TEAM PICKS", "The hits, the menu sounds and the bosses' braams, found on free sound sites.", "FREE"),
            },

            //04 FONTS
            new Credit[]
            {
                new Credit("CORMORANT GARAMOND", "The Cormorant Project Authors (Catharsis Fonts). The titles and the sentences.", "OFL"),
                new Credit("SAIRA SEMI CONDENSED", "The Saira Project Authors (Omnibus-Type). The labels, the buttons and the numbers.", "OFL"),
                new Credit("TRIRONG", "Cadson Demak. The Thai titles and sentences.", "OFL"),
                new Credit("CHAKRA PETCH", "Cadson Demak, The Chakra Petch Project Authors. The Thai labels and buttons.", "OFL"),
            },
        };
    }
}
