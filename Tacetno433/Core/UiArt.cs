using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
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
        public static Texture2D Logo;                             // TITLE (block C item 2) : Title/logo.png, the team's logo, any size
        public static Texture2D Poster;                           //   and Title/poster.png, the team's poster, shown as the game opens
        public static int Loaded;

        //Load : called once from LoadContent
        public static void Load(GraphicsDevice device)
        {
            Loaded = 0;
            int musicians = MusicianList.All.Length;
            Busts = new Texture2D[musicians];
            Faces = new Texture2D[musicians];
            Logo = Picture(device, "Title", "logo");
            Poster = Picture(device, "Title", "poster");
            Clean(Logo, BattleRules.TitleArtFloor, BattleRules.TitleLogoFeather);       // TITLE ART : no pale box round them
            Clean(Poster, BattleRules.TitleArtFloor, 0f);
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

        //Clean (TITLE ART, block C item 2) : the team's logo and poster are drawn by hand on black with a faint grey grain in it.
        //Drawn as added light (so their black is the page's black) that grain would show as a pale box. As each loads, floor
        //(of 255) is taken off every pixel and the rest stretched back up to white, and edge (a share of the size) fades the
        //picture to nothing toward its four edges. The files themselves are never changed.
        //ADVANCED PART : GetData copies the picture's pixels into an array, the loop changes them, SetData puts them back on the
        //card. It runs once at load (a 1500 x 1500 logo is 2.25 million pixels, a few hundredths of a second), never per frame.
        //The pixels are premultiplied (alpha 255 for these pictures), so scaling the colour scales the light it adds.
        private static void Clean(Texture2D picture, float floor, float edge)
        {
            if (picture == null) return;
            int w = picture.Width, h = picture.Height;
            Color[] pixels = new Color[w * h];
            picture.GetData(pixels);
            float stretch = 255f / Math.Max(1f, 255f - floor);
            for (int y = 0; y < h; y++)
            {
                float fadeY = edge > 0f ? Math.Min(1f, Math.Min(y, h - 1 - y) / (h * edge)) : 1f;
                for (int x = 0; x < w; x++)
                {
                    float fade = fadeY;
                    if (edge > 0f) fade = Math.Min(fade, Math.Min(1f, Math.Min(x, w - 1 - x) / (w * edge)));
                    fade = fade * fade * (3f - 2f * fade);                                       // smooth, no line where it starts
                    Color c = pixels[y * w + x];
                    float k = stretch * fade;
                    pixels[y * w + x] = new Color((int)(Math.Max(0f, c.R - floor) * k), (int)(Math.Max(0f, c.G - floor) * k),
                                                  (int)(Math.Max(0f, c.B - floor) * k), (int)(c.A * fade));
                }
            }
            picture.SetData(pixels);
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
