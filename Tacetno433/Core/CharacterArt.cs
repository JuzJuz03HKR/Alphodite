using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //CharacterAnim : what a musician is doing on the duel stage
    //   Idle    standing and breathing, loops for ever
    //   Attack  playing their beat, plays once and goes back to Idle
    //   Hurt    TACET won the clash on their beat, plays once and goes back to Idle
    public enum CharacterAnim { Idle, Attack, Hurt }

    //CharacterArt : THE PLACE WHERE THE PIXEL MUSICIANS GO.
    //
    //HOW TO ADD A MUSICIAN
    //  1. put the frames in  Content/Art/Musicians  named like this (the name in lower case)
    //         mali_idle_0.png    mali_idle_1.png    ...
    //         mali_attack_0.png  mali_attack_1.png  ...
    //         mali_hurt_0.png    ...
    //     facing RIGHT (toward TACET), feet on the bottom row of the picture, every frame of one
    //     musician on the same canvas size. Idle is enough to start with; a missing Attack or
    //     Hurt simply keeps showing Idle.
    //  2. add them in the MGCB editor so they get built
    //
    //Until the frames exist, an empty ArtSlot box marks where the musician stands.
    //
    //PIXEL ART RULE : pixel art only stays sharp when it is drawn at a WHOLE number scale
    //(x2, x3 ...) with "point" sampling. So musicians are never shrunk for depth like the
    //placeholder boxes are; players further back are drawn a little darker instead.
    public static class CharacterArt
    {
        public const int AnimCount = 3;
        public const int MaxFrames = 12;
        private static string[] animFile = { "idle", "attack", "hurt" };

        //Frames[musician][anim] : the pictures, lined up with MusicianList.All. Null when missing.
        public static Texture2D[][][] Frames = new Texture2D[0][][];

        //Timing : seconds per frame for idle, attack and hurt
        public static float[] FrameTime = { 0.14f, 0.06f, 0.08f };

        //Pixel Scale : how many screen pixels one art pixel becomes on the duel stage
        public static int PixelScale = 3;

        public static int Loaded;

        //Load : called once from LoadContent. Missing files are skipped quietly.
        public static void Load(ContentManager content)
        {
            Loaded = 0;
            Frames = new Texture2D[MusicianList.All.Length][][];

            for (int m = 0; m < Frames.Length; m++)
            {
                Frames[m] = new Texture2D[AnimCount][];
                string key = ArtBank.Key(MusicianList.All[m].Name);

                for (int a = 0; a < AnimCount; a++)
                {
                    Texture2D[] found = new Texture2D[MaxFrames];
                    int have = 0;
                    for (int f = 0; f < MaxFrames; f++)
                    {
                        string name = "Art/Musicians/" + key + "_" + animFile[a] + "_" + f;
                        string path = Path.Combine(AppContext.BaseDirectory, content.RootDirectory, name + ".xnb");
                        if (!File.Exists(path)) break;
                        try { found[f] = content.Load<Texture2D>(name); have++; }
                        catch (Exception) { break; }
                    }

                    if (have == 0) continue;
                    Frames[m][a] = new Texture2D[have];
                    for (int f = 0; f < have; f++) Frames[m][a][f] = found[f];
                    Loaded += have;
                }
            }
        }

        //Frame Count : 1 when an animation has no pictures, so the animator never divides by 0
        public static int FrameCount(Musician m, CharacterAnim anim)
        {
            Texture2D[] list = List(m, anim);
            return list == null ? 1 : list.Length;
        }

        //Frame Of : the picture to show. A missing animation falls back to Idle, a missing Idle
        //falls back to the single standing picture in ArtBank. Null means nothing at all yet.
        public static Texture2D FrameOf(Musician m, CharacterAnim anim, int frame)
        {
            Texture2D[] list = List(m, anim);
            if (list == null) list = List(m, CharacterAnim.Idle);
            if (list == null) return ArtBank.StandOf(m);

            if (frame < 0) frame = 0;
            if (frame >= list.Length) frame = list.Length - 1;
            return list[frame];
        }

        private static Texture2D[] List(Musician m, CharacterAnim anim)
        {
            int i = Array.IndexOf(MusicianList.All, m);
            if (i < 0 || i >= Frames.Length) return null;
            return Frames[i][(int)anim];
        }

        //Draw On Stage : the picture stands with its feet on the bottom middle of slot, at a whole
        //number scale. With no picture yet, the slot itself is drawn as an empty box.
        public static void Draw(SpriteBatch sb, Musician m, CharacterAnim anim, int frame, Rectangle slot,
                                int scale, Color tint, Color slotInk, float slotAlpha)
        {
            Texture2D art = FrameOf(m, anim, frame);
            if (art == null)
            {
                ArtSlot.Draw(sb, slot, slotInk, slotAlpha);
                return;
            }

            //Fit : never taller than the slot, so the side badge over the slot stays over the head
            //(29 Sep : a 112 pixel picture at x3 stood far above its spot and hid the badge)
            float size = Math.Min(scale, slot.Height / (float)art.Height);
            Vector2 feet = new Vector2(slot.Center.X, slot.Bottom);
            Vector2 origin = new Vector2(art.Width / 2f, art.Height);
            sb.Draw(art, feet, null, tint, 0f, origin, size, SpriteEffects.None, 0f);
        }

        //Draw Still : the first idle frame fitted into a box (formation seats, the bow, the rest
        //room). The biggest whole number scale that fits is used, so it stays sharp.
        public static void DrawStill(SpriteBatch sb, Musician m, Rectangle box, Color slotInk, float alpha)
        {
            Texture2D art = FrameOf(m, CharacterAnim.Idle, 0);
            if (art == null)
            {
                ArtSlot.Draw(sb, box, slotInk, alpha);
                return;
            }

            int scale = Math.Max(1, Math.Min(box.Width / art.Width, box.Height / art.Height));
            Vector2 feet = new Vector2(box.Center.X, box.Bottom);
            sb.Draw(art, feet, null, Color.White * alpha, 0f, new Vector2(art.Width / 2f, art.Height),
                    scale, SpriteEffects.None, 0f);
        }

        //Pixel Mode : switch the SpriteBatch to "point" sampling so pixel art is not blurred,
        //and back again afterwards. transform is the world shake of the duel (or Identity).
        //ADVANCED PART : the same End / Begin trick the duel uses for its screen shake. Point
        //sampling copies the nearest texel instead of blending four, which keeps hard pixel edges.
        public static void BeginPixels(SpriteBatch sb, Matrix transform)
        {
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                     null, null, null, transform);
        }

        public static void EndPixels(SpriteBatch sb, Matrix transform)
        {
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                     null, null, null, transform);
        }
    }

    //CharacterAnimator : which animation one musician is showing, and which frame of it.
    //Attack and Hurt play once and fall back to Idle by themselves.
    public class CharacterAnimator
    {
        public CharacterAnim Anim = CharacterAnim.Idle;
        public int Frame;
        private float timer;

        public void Play(CharacterAnim anim)
        {
            Anim = anim;
            Frame = 0;
            timer = 0f;
        }

        public void Update(float dt, Musician m)
        {
            timer += dt;
            int frames = CharacterArt.FrameCount(m, Anim);
            float step = CharacterArt.FrameTime[(int)Anim];
            int f = (int)(timer / step);

            if (Anim == CharacterAnim.Idle)
            {
                Frame = f % frames;                    // idle loops
                return;
            }

            //One Shot : attack and hurt end on their last frame, then idle again. With no
            //pictures at all a short pause stands in, so the timing is the same either way.
            float length = frames > 1 ? frames * step : 0.3f;
            Frame = Math.Min(f, frames - 1);
            if (timer >= length) Play(CharacterAnim.Idle);
        }
    }
}
