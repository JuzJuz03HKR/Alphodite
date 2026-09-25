using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //StageStaff : the five lines of a music staff stretched across the bright stage, behind the
    //band. They lie still while nothing happens. When the band wins a beat a wave runs along
    //them toward TACET and dies away, like a plucked string. While the band is on fire they
    //never stop singing.
    //
    //Math art : every line is 40 short straight pieces. The height of each piece is a sine wave
    //(it travels because time is taken away inside the sine) multiplied by a second, slow sine
    //that is zero at both ends, so the lines stay pinned at the edges like real strings.
    //It is an effect, not artwork, so it stays when the stage painting arrives.
    public static class StageStaff
    {
        private const int Lines = 5;
        private const float Gap = 12f;          // pixels from one line to the next
        private const int Pieces = 40;          // straight pieces per line
        private const float Height = 7f;        // how far a line moves at energy 1

        //Draw : the staff from left to right, its top line at top.
        //energy 0 (still) to about 1.5 (shaking hard), alpha is how dark the ink is.
        public static void Draw(SpriteBatch sb, float left, float right, float top, float energy, float time, float alpha)
        {
            float width = right - left;
            if (width < 40f || alpha <= 0f) return;

            for (int line = 0; line < Lines; line++)
            {
                float y0 = top + line * Gap;
                Vector2 previous = new Vector2(left, y0);

                for (int i = 1; i <= Pieces; i++)
                {
                    float along = i / (float)Pieces;                                   // 0 at the left end, 1 at the right
                    float wave = (float)Math.Sin(along * 18f - time * 9f - line * 0.6f);  // runs to the right
                    float pinned = (float)Math.Sin(along * MathHelper.Pi);              // 0 at both ends
                    Vector2 now = new Vector2(left + width * along, y0 + wave * pinned * energy * Height);

                    Gfx.Line(sb, previous, now, Palette.Ink * alpha, 1.5f);
                    previous = now;
                }
            }
        }
    }
}
