using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //NoteGlyph : the two signs a note can carry besides its letter.
    //   Spark         a four point star, the second note of a pair : one more flick, any way
    //   Fermata Sign  the arch with a dot that music writes over a held note : hold still
    //The pause menu uses the fermata sign as well, both mean "hold".
    public static class NoteGlyph
    {
        //Spark : a thin four point star, built from rows (the tall point) and columns (the wide one)
        public static void Spark(SpriteBatch sb, float cx, float cy, float size, Color color)
        {
            for (int y = -(int)size; y <= (int)size; y++)
            {
                float half = (size - Math.Abs(y)) * 0.28f;
                Gfx.Rect(sb, cx - half, cy + y, half * 2f + 1f, 1, color);
            }
            for (int x = -(int)size; x <= (int)size; x++)
            {
                float half = (size - Math.Abs(x)) * 0.28f;
                Gfx.Rect(sb, cx + x, cy - half, 1, half * 2f + 1f, color);
            }
            Gfx.DrawGlow(sb, cx, cy, size * 0.8f, color);
        }

        //Fermata Sign : an arch with a dot under it, outlined dark so it reads on any ground
        public static void FermataSign(SpriteBatch sb, float cx, float cy, float size, Color color)
        {
            Gfx.Arc(sb, cx, cy, size, MathHelper.Pi, MathHelper.TwoPi, Palette.Void * (color.A / 255f), 6f);
            Gfx.Arc(sb, cx, cy, size, MathHelper.Pi, MathHelper.TwoPi, color, 3f);
            Gfx.Circle(sb, cx, cy - size * 0.28f, size * 0.2f, color);
        }
    }
}
