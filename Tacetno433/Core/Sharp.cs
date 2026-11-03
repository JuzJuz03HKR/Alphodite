using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //SHARP SCREEN (Beta1, 28 Oct) : full screen is drawn at the monitor's own size, not stretched.
    //
    //Every position in the code is written for a page 1280 x 720 (GAME PIXELS, TacetGame.ScreenW and ScreenH). Before this,
    //the whole page was drawn into a 1280 x 720 picture and that picture was stretched to fill the screen, so in full
    //screen on a 1920 x 1080 monitor every letter and line was blown up one and a half times and went soft.
    //Now the page is drawn straight at the size it is shown (REAL PIXELS) :
    //   Scale      real pixels per game pixel : 1 in the 1280 x 720 window, 1.5 full screen on 1920 x 1080, 2 on 2560 x 1440,
    //              3 on 3840 x 2160 (4K)
    //   Transform  the matrix that turns game pixels into real pixels. Every SpriteBatch.Begin that draws on the page passes
    //              it (behind a camera when the page has one), so no page has to change a single position
    //   Dot        one real pixel, measured in game pixels (1 / Scale). The shapes built from rows (Gfx.Circle, Diamond ...)
    //              use rows one Dot tall, so their edges stay smooth however big the screen is
    //   Snap, Edge a position put on a whole real pixel, so an edge never falls between two pixels and goes soft
    //The fonts have bigger copies for big screens (Core/SharpText.cs), the pictures of Core/PostChain.cs are made at the real
    //size, and the pixel art stays pixel art : a pixel of the art is 2 game pixels, which is 3 real pixels at 1.5, 4 at 2 and
    //6 at 3, always whole and sharp.
    //ADVANCED PART : at Scale 1 every drawing path is the old one, line for line, so the 1280 x 720 window looks exactly as
    //it did. The new paths only run when the screen is bigger (or smaller) than the page.
    public static class Sharp
    {
        //Max Scale : a screen bigger than 4K gets the 4K picture stretched. It would cost a lot more and look the same.
        public const float MaxScale = 3f;

        public static float Scale = 1f;
        public static float Dot = 1f;
        public static int Width = TacetGame.ScreenW;
        public static int Height = TacetGame.ScreenH;
        public static Matrix Transform = Matrix.Identity;

        //Set : called by TacetGame whenever the window changes size (and by DebugShots for its pictures)
        public static void Set(float scale)
        {
            pageScale = MathHelper.Clamp(scale, 0.25f, MaxScale);
            Apply(pageScale);
        }

        private static void Apply(float scale)
        {
            Scale = scale;
            Dot = 1f / Scale;
            Width = (int)(TacetGame.ScreenW * Scale);
            Height = (int)(TacetGame.ScreenH * Scale);
            Transform = Matrix.CreateScale(Scale, Scale, 1f);
        }

        //======================================================================================
        //WORLD SCALE (3 Nov, the user : "on an 8 GB laptop the game lags, the small window is smooth")
        //======================================================================================
        //The heavy part of a frame is the WORLD : the pictures of Core/PostChain.cs (the backdrop, TACET's dark, the fighters,
        //their glow, the bloom) are full screen pictures run through shaders several times a frame, so their cost grows with
        //every pixel of the screen. Full screen on 1920 x 1080 has 2.25 times the pixels of the 1280 x 720 window, and a
        //laptop's graphics chip that keeps up with the window falls behind there.
        //So the world may be drawn smaller than the screen (WorldShare of it) and stretched onto it smoothly, while the
        //words, numbers and buttons on top stay at the screen's own size, sharp. GRAPHICS on the settings page picks the
        //share (Settings.Graphics), AUTO lowers it by itself when frames go missing (TacetGame.WatchFrames).
        //   UseWorld  Scale, Dot, Width, Height and Transform become the world's : everything drawn into the world's
        //             pictures (a page's PreDraw, PostChain.DrawPage) works in the world's real pixels without knowing it
        //   UseFull   back to the screen's own, for the words and buttons drawn on top
        //ADVANCED PART : nothing in the pages changes. They all draw in game pixels through Sharp.Transform and measure with
        //Sharp.Dot and Sharp.Scale, so switching these numbers between the two halves of the frame is enough.
        public static float WorldShare = 1f;
        private static float pageScale = 1f;

        public static float PageScale
        {
            get { return pageScale; }
        }

        //World Scale : real pixels per game pixel in the world's pictures (never more than the screen's own)
        public static float WorldScale
        {
            get { return MathHelper.Clamp(pageScale * WorldShare, 0.25f, pageScale); }
        }

        public static void UseWorld()
        {
            float world = WorldScale;
            if (world != Scale) Apply(world);
        }

        public static void UseFull()
        {
            if (pageScale != Scale) Apply(pageScale);
        }

        //Use Scale : any other size for a moment (ART PIXELS, Core/PostChain.cs : the battle's backdrop drawn at one real pixel
        //per pixel of art, 0.5). UseWorld or UseFull puts it back.
        public static void UseScale(float scale)
        {
            if (scale != Scale) Apply(scale);
        }

        //Snap : v (game pixels) moved onto the nearest real pixel. Math.Round, so at Scale 1 it is the old rounding exactly.
        public static float Snap(float v)
        {
            return (float)Math.Round(v * Scale) * Dot;
        }

        //Edge : v (game pixels) as a whole number of real pixels, a half always going up. Gfx puts the edges of boxes on
        //these, so a box two game pixels wide is always three real pixels wide at 1.5, wherever it sits.
        public static float Edge(float v)
        {
            return (float)Math.Floor(v * Scale + 0.5f);
        }

        //Begin : the frame's sprite batch begun again as TacetGame begins it, for a page or a cover that stepped out of it
        //to draw something of its own (the cards of Core/Card3D.cs, the cloth of Core/Curtain.cs)
        public static void Begin(SpriteBatch sb)
        {
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, Transform);
        }
    }
}
