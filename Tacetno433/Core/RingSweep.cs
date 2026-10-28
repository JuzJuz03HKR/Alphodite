using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //RingSweep (Beta1 screens, 28 Oct) : the page change for the moments that matter (docs/SCREENS_LEVEL_UP.md 3.4.4,
    //the star map of menu4). ScreenManager.SweepNext asks for it just before a Change.
    //   LIGHT   a ring opens from a point and floods the screen with light, its front a bright line : a run begins,
    //           a fight is won
    //   DARK    the same ring, black inside, white hot at its edge like TACET's hole : into a fight
    //   CLOSE   TACET's dark closes in from the edges onto a point : a fight is lost
    //Covering : the ring grows fast and slows to a stop (ease out), most of the way out in about 0.3 seconds, the rest of
    //the half fills it in. Showing : the light (or the dark) fades off the new page, quickly at first. Each half takes
    //as long as every other page change (ScreenManager.FadeSpeed), the user found faster changes too quick.
    //Drawn in rows two pixels tall (PIXEL x2) : one or two boxes per row, so a ring as big as the screen is still only a
    //few hundred boxes, and its edge steps like the pixel art. LESS FLASHING : the flash at the start and the bright
    //edge are softer (BattleRules.LessFlash).
    public static class RingSweep
    {
        public enum Kind { None, Light, Dark, Close }

        private const int Strip = 2;
        private const int W = TacetGame.ScreenW;
        private const int H = TacetGame.ScreenH;

        //Draw : covered 0 is nothing, 1 the whole screen. going is true while the page is being covered.
        public static void Draw(SpriteBatch sb, float covered, bool going, Vector2 from, Kind kind, float time)
        {
            if (covered <= 0f || kind == Kind.None) return;
            covered = Math.Min(1f, covered);
            float far = Farthest(from) + 24f;
            float power = Settings.ReduceFlashing ? BattleRules.LessFlash : 1f;
            Color fill = kind == Kind.Light ? Palette.StageLight : Palette.Void;

            //Showing : the light or the dark fades off the new page
            if (!going)
            {
                Gfx.Rect(sb, 0, 0, W, H, fill * (covered * covered));
                return;
            }

            //CLOSE : the dark outside a ring that shrinks onto the point, a thin seam of light on its edge
            if (kind == Kind.Close)
            {
                float r = far * (1f - UiMotion.EaseInOut(covered));
                Outside(sb, from, r, fill);
                Band(sb, from, r - 14f, r, Color.White * (0.1f * power));
                Band(sb, from, r - 2f, r, Palette.Paper * (0.8f * power));
                return;
            }

            //LIGHT, DARK : the ring opens, filled, with its bright edge
            float radius = far * UiMotion.EaseOut(covered);
            Inside(sb, from, radius, fill);
            Band(sb, from, radius, radius + 10f, Color.White * ((kind == Kind.Light ? 0.18f : 0.08f) * power));
            Band(sb, from, radius - BattleRules.SweepGlowEdge, radius, Color.White * (0.22f * power));
            Band(sb, from, radius - 3f, radius, Color.White * (0.85f * power));

            //Mark : the point flashes as the ring sets off (menu4 : the emblem lights up)
            float mark = 1f - covered / 0.3f;
            if (mark > 0f)
            {
                Gfx.DrawGlow(sb, from.X, from.Y, 70f + 160f * covered, Color.White * (mark * 0.8f * power));
                Gfx.CircleOutline(sb, from.X, from.Y, 10f + 60f * covered, Palette.Paper * (mark * power), 2f);
            }
        }

        //Farthest : how far the furthest corner of the screen is from a point
        private static float Farthest(Vector2 at)
        {
            float dx = Math.Max(at.X, W - at.X);
            float dy = Math.Max(at.Y, H - at.Y);
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        //Inside : the disc of radius r round at, one box per row
        private static void Inside(SpriteBatch sb, Vector2 at, float r, Color color)
        {
            if (r <= 0f) return;
            int top = Math.Max(0, (int)(at.Y - r) / Strip * Strip);
            int bottom = Math.Min(H, (int)(at.Y + r) + Strip);
            for (int y = top; y < bottom; y += Strip)
            {
                float half = Chord(r, y + 1f - at.Y);
                if (half <= 0f) continue;
                Row(sb, at.X - half, at.X + half, y, color);
            }
        }

        //Outside : everything but the disc
        private static void Outside(SpriteBatch sb, Vector2 at, float r, Color color)
        {
            for (int y = 0; y < H; y += Strip)
            {
                float half = r > 0f ? Chord(r, y + 1f - at.Y) : 0f;
                if (half <= 0f)
                {
                    Gfx.Rect(sb, 0, y, W, Strip, color);
                    continue;
                }
                Row(sb, 0f, at.X - half, y, color);
                Row(sb, at.X + half, W, y, color);
            }
        }

        //Band : the ring between two radii, two boxes per row (one where the ring's top or bottom lies flat)
        private static void Band(SpriteBatch sb, Vector2 at, float inner, float outer, Color color)
        {
            if (outer <= 0f) return;
            int top = Math.Max(0, (int)(at.Y - outer) / Strip * Strip);
            int bottom = Math.Min(H, (int)(at.Y + outer) + Strip);
            for (int y = top; y < bottom; y += Strip)
            {
                float dy = y + 1f - at.Y;
                float o = Chord(outer, dy);
                if (o <= 0f) continue;
                float i = inner > 0f ? Chord(inner, dy) : 0f;
                if (i <= 0f)
                {
                    Row(sb, at.X - o, at.X + o, y, color);
                    continue;
                }
                Row(sb, at.X - o, at.X - i, y, color);
                Row(sb, at.X + i, at.X + o, y, color);
            }
        }

        //Chord : half the width of a circle of radius r at dy from its middle (0 = the row misses it)
        private static float Chord(float r, float dy)
        {
            float k = r * r - dy * dy;
            return k > 0f ? (float)Math.Sqrt(k) : 0f;
        }

        //Row : one box from x0 to x1 on row y, kept on the screen and on the grid of the art's pixels (PIXEL x2)
        private static void Row(SpriteBatch sb, float x0, float x1, int y, Color color)
        {
            float a = (float)Math.Floor(Math.Max(0f, x0) * 0.5f) * 2f;
            float b = (float)Math.Floor(Math.Min(W, x1) * 0.5f) * 2f;
            if (b > a) Gfx.Rect(sb, a, y, b - a, Strip, color);
        }
    }
}
