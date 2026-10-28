using System;
using System.IO;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //Fx : the shaders (Beta1). They are loaded once at startup. When one is missing or will not load,
    //Ready stays false and every page that uses them falls back to plain SpriteBatch drawing, so the
    //game always runs, it only looks flatter.
    //The shader files are Content/Effects/*.fx, built by the content pipeline like the fonts.
    public static class Fx
    {
        public static Effect Post;      // Post.fx   : Down, Blur, Composite (see Core/PostChain.cs)
        public static Effect Sprite;    // Sprite.fx : Flash, Silhouette, Dissolve
        public static bool Ready;

        //MULTIPLY 2X : picture x what is under it x 2. A light map is kept at half strength so a light
        //can go brighter than "normal" (see LIGHT x 2 in Post.fx), and this blend undoes the half.
        //ADVANCED PART : the card adds two products, picture x under + under x picture, which is 2 x picture x under.
        public static readonly BlendState Multiply2X = new BlendState
        {
            ColorSourceBlend = Blend.DestinationColor,
            ColorDestinationBlend = Blend.SourceColor,
            AlphaSourceBlend = Blend.Zero,
            AlphaDestinationBlend = Blend.One,
        };

        //Fx Load : called once from LoadContent
        public static void Load(ContentManager content)
        {
            Post = Load(content, "Effects/Post");
            Sprite = Load(content, "Effects/Sprite");
            Ready = Post != null && Sprite != null;
        }

        //Load One : null when the file is not there, or the card cannot run it
        private static Effect Load(ContentManager content, string name)
        {
            string path = Path.Combine(AppContext.BaseDirectory, content.RootDirectory, name + ".xnb");
            if (!File.Exists(path)) return null;

            try
            {
                return content.Load<Effect>(name);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
