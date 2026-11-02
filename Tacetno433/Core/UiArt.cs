using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //UiArt (Beta1, 29 Oct) : the pixel art of the pages outside the battle, read straight from PNG files in
    //Content/Art/Ui/<folder>/ (docs/UI_ART_PLAN.md). Like the battle's FrameArt, the csproj copies that folder next to
    //the game, so a new picture needs no Content.mgcb entry : drop the file in, Rebuild, done. Every file is read once
    //when the game starts. A picture that is not there yet is null, and the page draws its empty frame (ArtSlot).
    //   Busts   bust_<name>.png, 160 x 256    a musician's tall picture : the band's columns, the recruit page
    //   Faces   face_<name>.png, 112 x 112    a musician's face
    //   Icons   <name>.png, 24 x 24          the battle's icons (block 1 item 4) : skill_<skill>, status_<status>, intent_<move>,
    //                                         every file in the folder is read, the screen asks for it by name (IconOf)
    public static class UiArt
    {
        public static Texture2D[] Busts = new Texture2D[0];       // lined up with MusicianList.All
        public static Texture2D[] Faces = new Texture2D[0];
        public static Dictionary<string, Texture2D> Icons = new Dictionary<string, Texture2D>();
        public static int Loaded;

        //Load : called once from LoadContent
        public static void Load(GraphicsDevice device)
        {
            Loaded = 0;
            int musicians = MusicianList.All.Length;
            Busts = new Texture2D[musicians];
            Faces = new Texture2D[musicians];
            for (int i = 0; i < musicians; i++)
            {
                string key = ArtBank.Key(MusicianList.All[i].Name);
                Busts[i] = Picture(device, "Busts", "bust_" + key);
                Faces[i] = Picture(device, "Faces", "face_" + key);
            }

            //Icons : whatever the team has put in the folder, by file name
            Icons.Clear();
            string icons = Path.Combine(AppContext.BaseDirectory, "Content", "Art", "Ui", "Icons");
            if (!Directory.Exists(icons)) return;
            foreach (string path in Directory.GetFiles(icons, "*.png"))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                Texture2D icon = Picture(device, "Icons", name);
                if (icon != null) Icons[name] = icon;
            }
        }

        //Picture : Content/Art/Ui/<folder>/<name>.png, or null when it is not there or cannot be read
        public static Texture2D Picture(GraphicsDevice device, string folder, string name)
        {
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, "Content", "Art", "Ui", folder, name + ".png");
                if (!File.Exists(path)) return null;
                Texture2D picture = Texture2D.FromFile(device, path, DefaultColorProcessors.PremultiplyAlpha);
                Loaded++;
                return picture;
            }
            catch (Exception)
            {
                return null;                              // a broken file must never stop the game
            }
        }

        //Lookups : one musician's picture, or null
        public static Texture2D BustOf(Musician m) { return Find(Busts, m.Index); }
        public static Texture2D FaceOf(Musician m) { return Find(Faces, m.Index); }

        //Icon Of : the icon called this (skill_caprice, status_dissonance ...), null until the team has drawn it
        public static Texture2D IconOf(string name)
        {
            Texture2D icon;
            return Icons.TryGetValue(name, out icon) ? icon : null;
        }

        private static Texture2D Find(Texture2D[] list, int i)
        {
            return i >= 0 && i < list.Length ? list[i] : null;
        }
    }
}
