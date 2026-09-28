using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //ArtBank : THE PLACE WHERE STILL PICTURES GO.
    //
    //Every picture here is optional. When a file is missing the page draws an empty ArtSlot
    //box in its place, so the game runs today with no artwork and switches each picture on by
    //itself the moment its file exists. The file names are listed in Content/Art/README.txt.
    //
    //   Stage              the duel background, one per era      Art/Stage/stage_siam
    //   Stand              a musician standing in the duel       Art/Musicians/stand_mali
    //   MusicianPortrait   full body, recruit page and cards     Art/Musicians/portrait_mali
    //   MusicianFace       small square face, lists and panels   Art/Musicians/face_mali
    //   ConductorPortrait  the gallery painting, profile, bow    Art/Conductors/portrait_the_inferno
    //   ConductorFace      small square face                     Art/Conductors/face_the_inferno
    //   CutIn              the signature picture                 Art/Conductors/cutin_the_inferno
    //   Enemy              TACET's shape in a duel               Art/Enemies/enemy_hush
    //   EraPanel           the tall panel on the era page        Art/Eras/era_siam
    //   RoutePanel         a kind of place, small and wide       Art/Route/route_shop    (230 x 142 on the route page)
    //   EventPicture       one per event                         Art/Events/event_a_street_musician
    //   Shop, Rest         the shop and the rest room            Art/Places/shop, Art/Places/rest
    //   Baton              the stick the player holds            Art/Baton/baton
    //
    //The conductor's hand and the pixel musicians animate, so they have their own files:
    //HandArt and CharacterArt.
    public static class ArtBank
    {
        public static Texture2D[] Stage = new Texture2D[0];               // lined up with EraList.All
        public static Texture2D[] Stand = new Texture2D[0];               // lined up with MusicianList.All
        public static Texture2D[] MusicianPortrait = new Texture2D[0];
        public static Texture2D[] MusicianFace = new Texture2D[0];
        public static Texture2D[] ConductorPortrait = new Texture2D[0];   // lined up with ConductorList.All
        public static Texture2D[] ConductorFace = new Texture2D[0];
        public static Texture2D[] CutIn = new Texture2D[0];
        public static Texture2D[] Enemy = new Texture2D[0];               // lined up with EnemyList.All
        public static Texture2D[] EraPanel = new Texture2D[0];
        public static Texture2D[] RoutePanel = new Texture2D[0];          // lined up with NodeType
        public static Texture2D[] EventPicture = new Texture2D[0];        // lined up with EventList.All
        public static Texture2D Shop;
        public static Texture2D Rest;
        public static Texture2D Baton;

        public static int Loaded;

        //Route File Names : in NodeType order
        private static string[] routeFile = { "battle", "event", "shop", "elite", "rest", "boss", "crossing" };

        //Art Load : called once from LoadContent
        public static void Load(ContentManager content)
        {
            Loaded = 0;

            Stage = new Texture2D[EraList.All.Length];
            EraPanel = new Texture2D[EraList.All.Length];
            for (int i = 0; i < Stage.Length; i++)
            {
                string key = Key(EraList.All[i].Name);
                Stage[i] = Picture(content, "Art/Stage/stage_" + key);
                EraPanel[i] = Picture(content, "Art/Eras/era_" + key);
            }

            int musicians = MusicianList.All.Length;
            Stand = new Texture2D[musicians];
            MusicianPortrait = new Texture2D[musicians];
            MusicianFace = new Texture2D[musicians];
            for (int i = 0; i < musicians; i++)
            {
                string key = Key(MusicianList.All[i].Name);
                Stand[i] = Picture(content, "Art/Musicians/stand_" + key);
                MusicianPortrait[i] = Picture(content, "Art/Musicians/portrait_" + key);
                MusicianFace[i] = Picture(content, "Art/Musicians/face_" + key);
            }

            int conductors = ConductorList.All.Length;
            CutIn = new Texture2D[conductors];
            ConductorPortrait = new Texture2D[conductors];
            ConductorFace = new Texture2D[conductors];
            for (int i = 0; i < conductors; i++)
            {
                string key = Key(ConductorList.All[i].Name);
                CutIn[i] = Picture(content, "Art/Conductors/cutin_" + key);
                ConductorPortrait[i] = Picture(content, "Art/Conductors/portrait_" + key);
                ConductorFace[i] = Picture(content, "Art/Conductors/face_" + key);
            }

            Enemy = new Texture2D[EnemyList.All.Length];
            for (int i = 0; i < Enemy.Length; i++)
                Enemy[i] = Picture(content, "Art/Enemies/enemy_" + Key(EnemyList.All[i].Name));

            RoutePanel = new Texture2D[routeFile.Length];
            for (int i = 0; i < RoutePanel.Length; i++)
                RoutePanel[i] = Picture(content, "Art/Route/route_" + routeFile[i]);

            EventPicture = new Texture2D[EventList.All.Length];
            for (int i = 0; i < EventPicture.Length; i++)
                EventPicture[i] = Picture(content, "Art/Events/event_" + Key(EventList.All[i].Title));

            Shop = Picture(content, "Art/Places/shop");
            Rest = Picture(content, "Art/Places/rest");
            Baton = Picture(content, "Art/Baton/baton");
        }

        //Picture : one optional file. Null when it is not there yet, or cannot be read.
        public static Texture2D Picture(ContentManager content, string name)
        {
            string path = Path.Combine(AppContext.BaseDirectory, content.RootDirectory, name + ".xnb");
            if (!File.Exists(path)) return null;

            try
            {
                Texture2D picture = content.Load<Texture2D>(name);
                Loaded++;
                return picture;
            }
            catch (Exception)
            {
                return null;
            }
        }

        //File Key : a name turned into a file name, "THE INFERNO" becomes "the_inferno"
        public static string Key(string name)
        {
            char[] letters = name.ToLowerInvariant().ToCharArray();
            for (int i = 0; i < letters.Length; i++)
                if (!char.IsLetterOrDigit(letters[i])) letters[i] = '_';
            return new string(letters);
        }

        //Draw Or Slot : the picture stretched into box, or the empty slot when there is none
        public static void DrawOrSlot(SpriteBatch sb, Texture2D art, Rectangle box, Color ink, float alpha)
        {
            if (art != null) sb.Draw(art, box, Color.White * alpha);
            else ArtSlot.Draw(sb, box, ink, alpha);
        }

        //Lookups : the picture for one thing, or null
        public static Texture2D StandOf(Musician m) { return Find(Stand, Array.IndexOf(MusicianList.All, m)); }
        public static Texture2D PortraitOf(Musician m) { return Find(MusicianPortrait, Array.IndexOf(MusicianList.All, m)); }
        public static Texture2D FaceOf(Musician m) { return Find(MusicianFace, Array.IndexOf(MusicianList.All, m)); }
        public static Texture2D PortraitOf(Conductor c) { return Find(ConductorPortrait, Array.IndexOf(ConductorList.All, c)); }
        public static Texture2D FaceOf(Conductor c) { return Find(ConductorFace, Array.IndexOf(ConductorList.All, c)); }
        public static Texture2D CutInOf(Conductor c) { return Find(CutIn, Array.IndexOf(ConductorList.All, c)); }
        public static Texture2D EnemyOf(Enemy e) { return Find(Enemy, Array.IndexOf(EnemyList.All, e)); }
        public static Texture2D EventOf(GameEvent e) { return Find(EventPicture, Array.IndexOf(EventList.All, e)); }
        public static Texture2D RouteOf(NodeType type) { return Find(RoutePanel, (int)type); }
        public static Texture2D EraOf(int era) { return Find(EraPanel, era); }

        private static Texture2D Find(Texture2D[] list, int i)
        {
            return (i >= 0 && i < list.Length) ? list[i] : null;
        }

        //Stage Draw : the painting for this era, or the bright empty stage the band stands on.
        //The bright side is part of the fight itself (our light against TACET's dark), so it stays.
        public static void DrawStage(SpriteBatch sb, int era, Rectangle box, float floorY)
        {
            Texture2D art = Find(Stage, era);
            if (art != null)
            {
                sb.Draw(art, box, Color.White);
                return;
            }

            Gfx.Rect(sb, box, Palette.StageLight);
            Gfx.DrawGlowBox(sb, new Rectangle(box.X - 200, box.Y - 100, 1100, 700), Color.White * 0.5f);

            //Floor : a band under the band, so they have ground to stand on
            Gfx.Rect(sb, box.X, floorY, box.Width, box.Bottom - floorY, Palette.StageLightDeep * 0.45f);
            Gfx.Rect(sb, box.X, floorY, box.Width, 1, Palette.InkSoft * 0.35f);
        }
    }
}
