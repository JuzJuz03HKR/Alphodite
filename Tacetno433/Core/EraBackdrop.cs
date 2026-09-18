using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //EraBackdrop : a line sketch of each era, drawn from shapes.
    //
    //PLACEHOLDER. The real era paintings replace this whole file:
    //    sb.Draw(eraTexture[era], area, Color.White);
    //Until then each era gets a recognisable outline, so the pages already read as a place:
    //   0 CLASSICAL  a colonnade under a pediment
    //   1 SIAM       tiered temple roofs and prang towers
    //   2 ROMANTIC   pointed gothic arches and a rose window
    //Every position is written from 0 to 1 inside the area, so it fits any box.
    public static class EraBackdrop
    {
        //Draw : ink is the line colour, pick dark ink on a bright page and pale ink on a dark one
        public static void Draw(SpriteBatch sb, int era, Rectangle area, float time, Color ink)
        {
            if (era == 0) DrawClassical(sb, area, ink);
            else if (era == 1) DrawSiam(sb, area, ink);
            else DrawRomantic(sb, area, ink);

            DrawNotes(sb, area, time, ink);
        }

        //Point Helpers : turn 0 to 1 positions into screen positions
        private static float X(Rectangle a, float t) { return a.X + a.Width * t; }
        private static float Y(Rectangle a, float t) { return a.Y + a.Height * t; }

        //Classical : six columns, an entablature and a pediment
        private static void DrawClassical(SpriteBatch sb, Rectangle a, Color ink)
        {
            float ground = Y(a, 0.80f);
            float top = Y(a, 0.36f);
            Gfx.Rect(sb, a.X, ground, a.Width, 2, ink);
            Gfx.Rect(sb, X(a, 0.10f), ground - 6, a.Width * 0.80f, 6, ink * 0.4f);

            //Columns
            for (int i = 0; i < 6; i++)
            {
                float cx = X(a, 0.17f + i * 0.132f);
                float w = a.Width * 0.034f;
                Gfx.Rect(sb, cx - w / 2f, top, 2, ground - top, ink);
                Gfx.Rect(sb, cx + w / 2f, top, 2, ground - top, ink);
                Gfx.Rect(sb, cx, top + 8, 1, ground - top - 16, ink * 0.45f);
                Gfx.Rect(sb, cx - w * 0.9f, top - 4, w * 1.8f + 2, 4, ink);           // capital
                Gfx.Rect(sb, cx - w * 0.8f, ground - 10, w * 1.6f + 2, 4, ink * 0.8f); // base
            }

            //Entablature and Pediment
            float beam = top - 16;
            Gfx.Rect(sb, X(a, 0.10f), beam, a.Width * 0.80f, 2, ink);
            Gfx.Rect(sb, X(a, 0.10f), beam + 10, a.Width * 0.80f, 2, ink * 0.7f);
            Gfx.Line(sb, X(a, 0.08f), beam, X(a, 0.50f), Y(a, 0.12f), ink, 2f);
            Gfx.Line(sb, X(a, 0.92f), beam, X(a, 0.50f), Y(a, 0.12f), ink, 2f);
            Gfx.Line(sb, X(a, 0.18f), beam - 6, X(a, 0.50f), Y(a, 0.16f), ink * 0.5f, 1f);
            Gfx.Line(sb, X(a, 0.82f), beam - 6, X(a, 0.50f), Y(a, 0.16f), ink * 0.5f, 1f);

            //Clock : the era that counted everything
            float r = a.Height * 0.045f;
            Gfx.CircleOutline(sb, X(a, 0.5f), Y(a, 0.225f), r, ink, 2f);
            Gfx.Line(sb, X(a, 0.5f), Y(a, 0.225f), X(a, 0.5f), Y(a, 0.225f) - r * 0.7f, ink, 1.5f);
            Gfx.Line(sb, X(a, 0.5f), Y(a, 0.225f), X(a, 0.5f) + r * 0.5f, Y(a, 0.225f), ink, 1.5f);
        }

        //Siam : a hall with stacked roofs and hooked finials, prang towers behind it
        private static void DrawSiam(SpriteBatch sb, Rectangle a, Color ink)
        {
            float ground = Y(a, 0.82f);
            Gfx.Rect(sb, a.X, ground, a.Width, 2, ink);

            //Prang Towers : stacked tiers getting narrower, then a thin spire
            DrawPrang(sb, X(a, 0.16f), ground, a.Height * 0.62f, a.Width * 0.11f, ink * 0.55f);
            DrawPrang(sb, X(a, 0.86f), ground, a.Height * 0.52f, a.Width * 0.09f, ink * 0.45f);

            //Hall Pillars
            float eave = Y(a, 0.50f);
            for (int i = 0; i < 5; i++)
            {
                float px = X(a, 0.33f + i * 0.085f);
                Gfx.Rect(sb, px, eave, 3, ground - eave, ink);
                Gfx.Rect(sb, px - 3, eave + 6, 9, 3, ink * 0.6f);
            }

            //Tiered Roofs : three layers, each one smaller and higher
            for (int t = 0; t < 3; t++)
            {
                float halfW = a.Width * (0.26f - t * 0.055f);
                float baseY = eave - t * a.Height * 0.075f;
                float peakY = baseY - a.Height * 0.16f;
                float cx = X(a, 0.50f);

                Gfx.Line(sb, cx - halfW, baseY, cx, peakY, ink, 2f);
                Gfx.Line(sb, cx + halfW, baseY, cx, peakY, ink, 2f);
                Gfx.Line(sb, cx - halfW * 0.9f, baseY - 5, cx, peakY + 6, ink * 0.4f, 1f);
                Gfx.Line(sb, cx + halfW * 0.9f, baseY - 5, cx, peakY + 6, ink * 0.4f, 1f);

                //Finials : the hooked tips at the ends of each roof line
                DrawFinial(sb, cx - halfW, baseY, -1f, ink);
                DrawFinial(sb, cx + halfW, baseY, 1f, ink);
                if (t == 2) Gfx.Line(sb, cx, peakY, cx, peakY - 18, ink, 2f);
            }

            //Gable : the carved triangle in the front roof
            float gx = X(a, 0.50f);
            Gfx.DiamondOutline(sb, gx, eave - a.Height * 0.07f, a.Height * 0.03f, ink * 0.7f, 1.5f);
        }

        private static void DrawPrang(SpriteBatch sb, float cx, float ground, float height, float width, Color ink)
        {
            int tiers = 6;
            float y = ground;
            for (int i = 0; i < tiers; i++)
            {
                float w = width * (1f - i * 0.14f);
                float h = height * 0.13f;
                Gfx.Rect(sb, cx - w / 2f, y - h, 2, h, ink);
                Gfx.Rect(sb, cx + w / 2f, y - h, 2, h, ink);
                Gfx.Rect(sb, cx - w / 2f - 4, y - h, w + 10, 2, ink);
                y -= h;
            }
            Gfx.Line(sb, cx - width * 0.1f, y, cx, y - height * 0.22f, ink, 2f);
            Gfx.Line(sb, cx + width * 0.1f, y, cx, y - height * 0.22f, ink, 2f);
        }

        private static void DrawFinial(SpriteBatch sb, float x, float y, float side, Color ink)
        {
            Gfx.Line(sb, x, y, x + side * 12, y - 14, ink, 2f);
            Gfx.Line(sb, x + side * 12, y - 14, x + side * 6, y - 22, ink, 2f);
        }

        //Romantic : a row of pointed arches and a rose window above the middle one
        private static void DrawRomantic(SpriteBatch sb, Rectangle a, Color ink)
        {
            float ground = Y(a, 0.82f);
            Gfx.Rect(sb, a.X, ground, a.Width, 2, ink);

            for (int i = 0; i < 5; i++)
            {
                float w = a.Width * 0.12f;
                float x = X(a, 0.13f + i * 0.155f);
                float spring = Y(a, 0.46f) + Math.Abs(i - 2) * a.Height * 0.04f;
                DrawPointedArch(sb, x, spring, w, ground, ink * (i == 2 ? 1f : 0.7f));
                Gfx.Rect(sb, x + w / 2f, spring, 1, ground - spring, ink * 0.3f);
            }

            //Rose Window
            float cx = X(a, 0.5f);
            float cy = Y(a, 0.2f);
            float r = a.Height * 0.09f;
            Gfx.CircleOutline(sb, cx, cy, r, ink, 2f);
            Gfx.CircleOutline(sb, cx, cy, r * 0.4f, ink * 0.7f, 1.5f);
            for (int s = 0; s < 8; s++)
            {
                float angle = MathHelper.TwoPi * s / 8f;
                Gfx.Line(sb, cx + (float)Math.Cos(angle) * r * 0.4f, cy + (float)Math.Sin(angle) * r * 0.4f,
                         cx + (float)Math.Cos(angle) * r, cy + (float)Math.Sin(angle) * r, ink * 0.7f, 1f);
            }
        }

        //Pointed Arch : two arcs that meet at a point, then straight sides down to the ground
        private static void DrawPointedArch(SpriteBatch sb, float x, float spring, float w, float ground, Color ink)
        {
            Gfx.Arc(sb, x + w, spring, w, MathHelper.Pi, MathHelper.Pi * 4f / 3f, ink, 2f);
            Gfx.Arc(sb, x, spring, w, MathHelper.Pi * 5f / 3f, MathHelper.TwoPi, ink, 2f);
            Gfx.Rect(sb, x, spring, 2, ground - spring, ink);
            Gfx.Rect(sb, x + w - 1, spring, 2, ground - spring, ink);
        }

        //Drifting Notes : a few note heads floating up, like the storyboard
        private static void DrawNotes(SpriteBatch sb, Rectangle a, float time, Color ink)
        {
            for (int i = 0; i < 4; i++)
            {
                float t = (time * 0.05f + i * 0.25f) % 1f;
                float x = X(a, 0.12f + i * 0.23f) + (float)Math.Sin(time * 0.8f + i) * 10f;
                float y = Y(a, 0.75f - t * 0.6f);
                float fade = t < 0.2f ? t / 0.2f : (1f - t);
                Color c = ink * (0.8f * fade);

                Gfx.Circle(sb, x, y, 4, c);
                Gfx.Rect(sb, x + 3, y - 18, 1.5f, 18, c);
                Gfx.Line(sb, x + 4, y - 18, x + 10, y - 11, c, 1.5f);
            }
        }
    }
}
