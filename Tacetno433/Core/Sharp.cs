using System;
using Microsoft.Xna.Framework;

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
            Scale = MathHelper.Clamp(scale, 0.25f, MaxScale);
            Dot = 1f / Scale;
            Width = (int)(TacetGame.ScreenW * Scale);
            Height = (int)(TacetGame.ScreenH * Scale);
            Transform = Matrix.CreateScale(Scale, Scale, 1f);
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
    }
}
