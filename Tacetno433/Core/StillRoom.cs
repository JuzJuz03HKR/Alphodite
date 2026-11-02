using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //STILL ROOM (Beta1 2 Nov, the user 13.139 : "what is used or drawn again and again is one piece, change it and everything
    //changes with it") : the dark, still room of the menu pages, THE ARCHIVE, SETTINGS, HOW TO PLAY and CREDITS (and the pause
    //menu when it borrows SETTINGS or HOW TO PLAY). Until 2 Nov every one of them drew its own copy of the same room.
    //Change the room here and all of them change together.
    //
    //   Light      the page's lens : a dark room, no spot, the edges darkened
    //   Back       the floor : the dark, a soft glow over it, four rings lying on it far away, coming in at the start
    //   Hole       TACET in a corner of the page, opening with a small overshoot, then breathing slowly
    //   Frame      a thin frame with a ring on each corner (menu3), round a picture or a list
    public static class StillRoom
    {
        //Light : the lens every still room uses (THE LENS, Core/PostChain.cs)
        public static void Light(PostChain.PageLook lens)
        {
            lens.Ambient = 0.42f;
            lens.SpotPower = 0f;
            lens.Vignette = 0.6f;
        }

        //Back : the dark room and the rings on its floor. time is the page's own clock (the rings come in over its first moments).
        public static void Back(SpriteBatch sb, float time)
        {
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageDeep);
            Gfx.DrawGlowBox(sb, new Rectangle(200, 60, 1100, 640), Palette.Paper * 0.05f);
            float a = UiMotion.Between(time, 0.1f, 0.8f);
            for (int k = 0; k < 4; k++)
            {
                float rx = 240f + k * 150f;
                Ornament.FloorRing(sb, 700f, 640f, rx, rx * 0.16f, Palette.Paper * ((0.1f - k * 0.02f) * a), a);
            }
        }

        //Hole Size : how big TACET is time seconds into the page, size once it has opened : a quick opening that overshoots a
        //little (EaseBack), then a slow breath
        public static float HoleSize(float size, float time)
        {
            return size * UiMotion.EaseBack(MathHelper.Clamp(time / 0.6f, 0f, 1f)) * (1f + 0.03f * (float)Math.Sin(time * 0.6f));
        }

        //Hole : TACET in its corner this frame (call from Update, after Dark.Update)
        public static void Hole(TacetVoid dark, Vector2 at, float size, float time)
        {
            dark.PlaceOnPage(at, HoleSize(size, time), 0f, 0f, 0f);
        }

        //Frame : the thin frame round a picture or a list, dark inside, a ring on each corner, fading in with the page
        public static void Frame(SpriteBatch sb, Rectangle box, float time)
        {
            float a = UiMotion.Between(time, 0.15f, 0.5f);
            Gfx.Rect(sb, box, Palette.Void * (0.45f * a));
            Gfx.RectOutline(sb, box, Palette.LineGrey * (0.4f * a), 1);
            Corner(sb, box.X, box.Y, a);
            Corner(sb, box.Right, box.Y, a);
            Corner(sb, box.X, box.Bottom, a);
            Corner(sb, box.Right, box.Bottom, a);
        }

        //Corner : a small ring the frame's corner sits in
        public static void Corner(SpriteBatch sb, float x, float y, float a)
        {
            Gfx.Circle(sb, x, y, 4f, Palette.StageDeep * a);
            Gfx.CircleOutline(sb, x, y, 4f, Palette.PaperDim * a, 1f);
        }
    }
}
