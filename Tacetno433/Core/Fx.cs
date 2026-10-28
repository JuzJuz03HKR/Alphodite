using System;
using System.IO;
using Microsoft.Xna.Framework;
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
        public static Effect Void;      // Void.fx   : TACET's dark on the battle page (see Core/TacetVoid.cs)
        public static bool Ready;       // Post and Sprite loaded
        public static bool VoidReady;   // and Void too
        public static Texture2D Noise;  // a soft tiling noise the shaders read (made here, not a file)

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
        public static void Load(ContentManager content, GraphicsDevice device)
        {
            Post = Load(content, "Effects/Post");
            Sprite = Load(content, "Effects/Sprite");
            Void = Load(content, "Effects/Void");
            Ready = Post != null && Sprite != null;
            VoidReady = Ready && Void != null;
            Noise = MakeNoise(device);
        }

        //Make Noise : 256 x 256, made once. r and g are soft clouds of two sizes, b a grain for every dot.
        //It tiles (the right edge fits the left, the top fits the bottom), so a shader can read it anywhere.
        //ADVANCED PART : VALUE NOISE. A grid of random numbers is blended smoothly between its points, and a few
        //grids, each twice as fine and half as strong, are added up (FRACTAL NOISE). Wrapping the grid makes it tile.
        private static Texture2D MakeNoise(GraphicsDevice device)
        {
            const int size = 256;
            Random random = new Random(433);
            float[] clouds = Fractal(size, 8, random);
            float[] fine = Fractal(size, 16, random);
            Color[] dots = new Color[size * size];
            for (int i = 0; i < dots.Length; i++)
                dots[i] = new Color((byte)(clouds[i] * 255f), (byte)(fine[i] * 255f), (byte)random.Next(256), (byte)255);
            Texture2D texture = new Texture2D(device, size, size);
            texture.SetData(dots);
            return texture;
        }

        //Fractal : four grids of random numbers, the first with cells x cells points, stretched to 0..1
        private static float[] Fractal(int size, int cells, Random random)
        {
            float[] sum = new float[size * size];
            float strength = 1f;
            for (int octave = 0; octave < 4; octave++, cells *= 2, strength *= 0.5f)
            {
                float[] grid = new float[cells * cells];
                for (int i = 0; i < grid.Length; i++) grid[i] = (float)random.NextDouble();

                for (int y = 0; y < size; y++)
                {
                    float gy = (float)y * cells / size;
                    int y0 = (int)gy;
                    float fy = Smooth(gy - y0);
                    int y1 = (y0 + 1) % cells;
                    for (int x = 0; x < size; x++)
                    {
                        float gx = (float)x * cells / size;
                        int x0 = (int)gx;
                        float fx = Smooth(gx - x0);
                        int x1 = (x0 + 1) % cells;
                        float top = grid[y0 * cells + x0] + (grid[y0 * cells + x1] - grid[y0 * cells + x0]) * fx;
                        float bottom = grid[y1 * cells + x0] + (grid[y1 * cells + x1] - grid[y1 * cells + x0]) * fx;
                        sum[y * size + x] += (top + (bottom - top) * fy) * strength;
                    }
                }
            }

            //Stretch : the lowest becomes 0, the highest 1
            float low = float.MaxValue, high = float.MinValue;
            for (int i = 0; i < sum.Length; i++)
            {
                if (sum[i] < low) low = sum[i];
                if (sum[i] > high) high = sum[i];
            }
            for (int i = 0; i < sum.Length; i++) sum[i] = (sum[i] - low) / (high - low);
            return sum;
        }

        //Smooth : 0..1 eased at both ends, so the grid's lines never show
        private static float Smooth(float t)
        {
            return t * t * (3f - 2f * t);
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
