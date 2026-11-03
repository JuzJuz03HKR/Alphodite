using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //HitFx Floor (Beta1 3 Nov, docs/BETA1_DIRECTION.md 55, the user : "take the examples from Worldless") : in Worldless a
    //blow lights the ground : a long flat line of light runs along the floor under the one who was hit, much wider than the
    //blow itself, hot in the middle and in the striker's colour at its ends. TACET's stage is a dark glossy floor (MIRROR),
    //so the same line reads as the blow's light thrown across the floor : the player sees where it landed even with the
    //eye on the ring. Ours grows out from the feet both ways and fades from its ends, and a stronger blow or a higher
    //weapon tier throws it wider.
    //   FLOOR GLARE  three layers, all in the glow layer (bloom makes them soft) : a wide low haze in the striker's colour,
    //                a narrow hot band, and a thin line of the hottest colour
    public partial class HitFx
    {
        private struct Glare
        {
            public bool On;
            public Vector2 At;
            public float Width, Age, Length;
            public Color Tint, Hot;
        }
        private readonly Glare[] glares = new Glare[12];

        //Add Glare : at the feet of the one hit (box = their body)
        private void AddGlare(Rectangle box, float power, int tier, HitColors c)
        {
            int slot = 0;
            for (int i = 0; i < glares.Length; i++) if (!glares[i].On) { slot = i; break; }
            float width = BattleRules.FloorGlareWidth * (0.7f + 0.3f * power) * (1f + BattleRules.HitTierSize * tier);
            glares[slot] = new Glare
            {
                On = true, At = new Vector2(box.Center.X, box.Bottom - 4f), Width = width,
                Age = 0f, Length = BattleRules.FloorGlareTime, Tint = c.Tint, Hot = c.Hot,
            };
        }

        //Update Glares : they play through the hit stop (they are the moment the blow lands)
        private void UpdateGlares(float dt)
        {
            for (int i = 0; i < glares.Length; i++)
            {
                if (!glares[i].On) continue;
                glares[i].Age += dt;
                if (glares[i].Age > glares[i].Length) glares[i].On = false;
            }
        }

        private void ClearGlares()
        {
            for (int i = 0; i < glares.Length; i++) glares[i].On = false;
        }

        //Draw Glares : under every other light of the blow
        private void DrawGlares(SpriteBatch sb)
        {
            for (int i = 0; i < glares.Length; i++)
            {
                if (!glares[i].On) continue;
                Glare g = glares[i];
                float t = g.Age / g.Length;
                float open = Ease(Math.Min(1f, t * 4f));             // out from the feet in the first quarter
                float a = 1f - t * t;                                  // holds bright, then goes at the end
                float w = g.Width * (0.25f + 0.75f * open);
                float h = BattleRules.FloorGlareHeight;

                Gfx.DrawGlowBox(sb, GlareBox(g.At, w * 1.3f, h * 6f), g.Tint * (0.6f * a));
                Gfx.DrawGlowBox(sb, GlareBox(g.At, w, h * 3f), g.Tint * a);
                Gfx.DrawGlowBox(sb, GlareBox(g.At, w * 0.7f, h * 1.6f), g.Hot * a);
                Gfx.Rect(sb, g.At.X - w * 0.3f, g.At.Y - 1.5f, w * 0.6f, 3f, Color.White * a);
            }
        }

        private static Rectangle GlareBox(Vector2 centre, float w, float h)
        {
            return new Rectangle((int)(centre.X - w * 0.5f), (int)(centre.Y - h * 0.5f), (int)w, (int)Math.Max(2f, h));
        }
    }
}
