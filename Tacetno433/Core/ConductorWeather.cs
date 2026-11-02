using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //ConductorWeather (Beta1 block D, 2 Nov, the user 13.120 and 13.122) : the run's conductor's theme as weather, on the
    //battle's stage from the first turn and on the pages of the run, stronger once their FINALE has struck. The user : the
    //stage turns to the character's atmosphere, "the storm's rain, FANNY's rainbow dimension against dark shadows of blue
    //and purple, Bach's stars twinkling in the sky", and then "make it their theme the whole time you play and fight".
    //   STORM  THE UNHEARING : rain driven across from the right, the colour gone cold, and now and then lightning far off,
    //          a flicker in the clouds and a thin bolt down to the horizon (blue, red once the FINALE has grown : STORM)
    //   PRISM  FANNY MENDELSSOHN AND ??? : bands of rainbow light lying across the sky and pieces of prism glass drifting
    //          and turning in it, against shadows of her brother's purple and blue creeping in from the sides and two of his
    //          cuts across the upper corners, black, burning at their edges
    //   HOLY   THE METRONOME : a sky full of small warm stars, each twinkling at its own pace, the brighter few glowing, and
    //          now and then one falling across it
    //The same weather as round their painting on the gallery (Core/ConductorAura.cs), now over the world. Nothing is stored :
    //every drop, star and piece of glass is where its number and the clock put it, so the weather costs no memory and is the
    //same every time. strength 0..1 (how much of it shows), level 0..2 (the FINALE's level : more of it), horizon (the sky
    //ends there), bottom (the page's floor ends there). LESS FLASHING keeps the lightning soft.
    //Light added with BlendState.Additive counts its colour times its own alpha, so a light at a adds about a x a (the same
    //as the battle's lights, Core/PostChain.cs SpotLight) : the numbers below are tuned by eye for that.
    public static class ConductorWeather
    {
        private static readonly Vector2[] corners = new Vector2[4];     // one drifting piece of glass while it is drawn
        private static readonly Vector2[] bolt = new Vector2[9];        // STORM : the far bolt's jagged line

        //Sky : the weather's far parts, light added (into a GLOW). rain : STORM's rain too (a page cannot draw over its own
        //finished picture, so its rain falls here as light)
        public static void Sky(SpriteBatch sb, AuraKind kind, float strength, int level, float time, float horizon, float bottom, bool rain)
        {
            if (strength <= 0.005f) return;
            if (kind == AuraKind.Storm)
            {
                FarLightning(sb, strength, level, time, horizon);
                if (rain) Rain(sb, strength, level, time, bottom, true);
            }
            else if (kind == AuraKind.Prism) PrismSky(sb, strength, level, time, horizon);
            else if (kind == AuraKind.Holy) NightSky(sb, strength, level, time, horizon);
        }

        //Front : over a finished world (alpha blended) : STORM's rain and cold, PRISM's shadows of her brother
        public static void Front(SpriteBatch sb, AuraKind kind, float strength, int level, float time, float bottom)
        {
            if (strength <= 0.005f) return;
            if (kind == AuraKind.Storm)
            {
                Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, bottom, new Color(14, 22, 40) * (0.18f * strength));
                Rain(sb, strength, level, time, bottom, false);
            }
            else if (kind == AuraKind.Prism) BrotherShadows(sb, strength, time, bottom);
        }

        //======================================================================================
        //STORM
        //======================================================================================

        //Far Lightning : every few seconds lightning far off, at a different moment and place each round : the clouds lit
        //from inside at the top of the sky, a thin bolt flickering down to the horizon
        private static void FarLightning(SpriteBatch sb, float s, int level, float time, float horizon)
        {
            const float every = 3.6f;
            int round = (int)(time / every);
            float into = time - round * every - Hash(round, 41) * 1.6f;
            if (into < 0f || into > 0.5f) return;
            float flicker = into < 0.06f ? 1f : into < 0.1f ? 0.2f : into < 0.16f ? 0.9f : Math.Max(0f, 1f - (into - 0.16f) / 0.34f) * 0.6f;
            flicker *= s * (Settings.ReduceFlashing ? BattleRules.LessFlash : 1f);
            Color tint = level >= 1 ? Palette.StormRed : Palette.LightningBlue;
            Color hot = level >= 1 ? Palette.StormRedHot : Palette.LightningBlueHot;
            float x = 80f + Hash(round, 43) * (TacetGame.ScreenW - 160f);
            Gfx.DrawGlow(sb, x, 30f, 520f, tint * (0.5f * flicker));
            Gfx.DrawGlow(sb, x, 60f, 220f, hot * (0.55f * flicker));
            for (int i = 0; i < bolt.Length; i++)
            {
                float t = i / (float)(bolt.Length - 1);
                bolt[i] = new Vector2(x + (i > 0 ? (Hash(round * 9 + i, 47) - 0.5f) * 70f : 0f) + t * 60f, -10f + t * (horizon - 20f));
            }
            for (int i = 1; i < bolt.Length; i++)
            {
                Gfx.Line(sb, bolt[i - 1], bolt[i], tint * (0.7f * flicker), 5f);
                Gfx.Line(sb, bolt[i - 1], bolt[i], hot * flicker, 1.5f);
            }
        }

        //Rain : drops driven across from the right, each falling at its own speed. light : drawn as added light (a page's
        //glow), which counts its alpha twice, so it is given more
        private static void Rain(SpriteBatch sb, float s, int level, float time, float bottom, bool light)
        {
            float a = light ? Math.Min(1f, 0.8f * s) : 0.5f * s;
            int drops = 60 + 25 * level;
            for (int i = 0; i < drops; i++)
            {
                float speed = 700f + 500f * Hash(i, 21);
                float y = (Hash(i, 22) * (bottom + 80f) + time * speed) % (bottom + 80f) - 40f;
                float x = (Hash(i, 23) * (TacetGame.ScreenW + 300f) - time * speed * 0.45f) % (TacetGame.ScreenW + 300f);
                if (x < 0f) x += TacetGame.ScreenW + 300f;
                Gfx.Line(sb, new Vector2(x, y), new Vector2(x + 12f, y - 28f), Color.Lerp(Palette.LightningBlueHot, Color.White, 0.5f) * a, 1.5f);
            }
        }

        //======================================================================================
        //PRISM
        //======================================================================================

        //Prism Sky (2 Nov, the user 13.143 : the rainbow bands looked odd, glass from the start) : no bands of rainbow any more.
        //Shards of clear glass float across the sky from the first turn, turning as they go. Mostly white glass, one edge
        //catching a thin prism colour, and a white glint across the face when a shard turns flat to us. More of them with
        //the FINALE's level.
        private static void PrismSky(SpriteBatch sb, float s, int level, float time, float horizon)
        {
            int pieces = 16 + 6 * level;
            for (int i = 0; i < pieces; i++)
            {
                float x = (Hash(i, 51) * (TacetGame.ScreenW + 200f) + time * (6f + 10f * Hash(i, 52))) % (TacetGame.ScreenW + 200f) - 100f;
                float y = 30f + Hash(i, 53) * (horizon + 80f) + (float)Math.Sin(time * 0.7f + i) * 10f;
                float size = 12f + 22f * Hash(i, 54);
                float spin = time * (0.3f + 0.5f * Hash(i, 55)) + i;
                float squeeze = 0.25f + 0.75f * Math.Abs((float)Math.Sin(time * (0.5f + Hash(i, 56)) + i * 2f));
                float cs = (float)Math.Cos(spin);
                float sn = (float)Math.Sin(spin);
                for (int k = 0; k < 4; k++)
                {
                    float a = k * MathHelper.PiOver2 + (Hash(i * 4 + k, 57) - 0.5f) * 1.1f;            // uneven corners : a broken piece
                    float r = size * (0.35f + 0.65f * Hash(i * 4 + k, 58));
                    Vector2 local = new Vector2((float)Math.Cos(a) * r * squeeze, (float)Math.Sin(a) * r);
                    corners[k] = new Vector2(x, y) + new Vector2(local.X * cs - local.Y * sn, local.X * sn + local.Y * cs);
                }
                Color edge = Color.Lerp(Color.White, Palette.Prism(Hash(i, 59) + time * 0.03f), 0.55f);
                Gfx.Quad(sb, corners, Color.White * (0.1f * s));
                for (int k = 0; k < 4; k++)
                    Gfx.Line(sb, corners[k], corners[(k + 1) % 4], (k == 1 ? edge : Color.White) * (0.7f * s), 1.5f);
                float glint = (squeeze - 0.85f) / 0.15f;                                               // GLINT : flat to us
                if (glint > 0f) Gfx.Line(sb, corners[0], corners[2], Color.White * (0.8f * s * glint), 2f);
            }
        }

        //Brother Shadows : PRISM's other voice. Shadows of her brother's purple and blue creep in from the left and the right
        //edges (rows darker towards the edge), and two of his cuts lie across the upper corners. Against the rainbow.
        private static void BrotherShadows(SpriteBatch sb, float s, float time, float bottom)
        {
            Color shadow = new Color(20, 8, 44);
            const int steps = 12;
            for (int k = 0; k < steps; k++)
            {
                float w = 18f;
                float edge = 1f - k / (float)steps;
                float a = 0.55f * s * edge * edge;                                                 // deep at the edge, soft inside
                Gfx.Rect(sb, k * w, 0, w, bottom, shadow * a);
                Gfx.Rect(sb, TacetGame.ScreenW - (k + 1) * w, 0, w, bottom, shadow * a);
            }
            BrotherCut(sb, new Vector2(-20f, 150f), new Vector2(260f, -10f), s, time, 0f);
            BrotherCut(sb, new Vector2(1010f, -10f), new Vector2(1300f, 210f), s, time, 2.1f);
        }

        //Brother Cut : one of his cuts, a black band with flickering purple and blue edges
        private static void BrotherCut(SpriteBatch sb, Vector2 a, Vector2 b, float s, float time, float phase)
        {
            Vector2 along = b - a;
            Vector2 side = Vector2.Normalize(new Vector2(-along.Y, along.X));
            for (int k = 0; k < 5; k++)
            {
                float wave = 0.5f + 0.5f * (float)Math.Sin(time * 7f + k * 1.7f + phase);
                Vector2 off = side * ((k - 2f) * 3f);
                Color edge = Color.Lerp(Palette.SpotBrother, Palette.SpotBrotherDeep, k / 4f) * ((0.25f + 0.25f * wave) * s);
                Gfx.Line(sb, a + off, b + off, edge, 2f);
            }
            Gfx.Line(sb, a, b, Palette.Void * (0.9f * s), 5f);
        }

        //======================================================================================
        //HOLY
        //======================================================================================

        //Night Sky : small warm stars over the sky (more of them high up), each twinkling at its own pace, the brighter few
        //glowing, and every few seconds a star falling across it with its tail
        private static void NightSky(SpriteBatch sb, float s, int level, float time, float horizon)
        {
            int count = BattleRules.FinaleHolyStars * (3 + level) / 4;
            for (int i = 0; i < count; i++)
            {
                float x = Hash(i, 61) * (TacetGame.ScreenW + 140f) - 70f;
                float y = Hash(i, 62) * Hash(i, 63) * (horizon + 40f) - 10f;
                float twinkle = 0.55f + 0.45f * Math.Abs((float)Math.Sin(time * (0.8f + 2.4f * Hash(i, 64)) + i * 1.3f));
                Color c = Color.Lerp(Palette.SpotHoly, Palette.SpotHolyHot, Hash(i, 65));
                bool bright = Hash(i, 66) > 0.9f;
                Gfx.Rect(sb, (float)Math.Floor(x * 0.5f) * 2f, (float)Math.Floor(y * 0.5f) * 2f, bright ? 4f : 2f, bright ? 4f : 2f, c * Math.Min(1f, twinkle * s * 1.25f));
                if (bright) Gfx.DrawGlow(sb, x + 2f, y + 2f, 16f, c * (0.6f * twinkle * s));
            }

            const float every = 4.2f;
            int round = (int)(time / every);
            float into = time - round * every;
            if (into < 0.7f)
            {
                float t = into / 0.7f;
                Vector2 from = new Vector2(200f + Hash(round, 67) * 880f, 20f + Hash(round, 68) * 80f);
                Vector2 way = new Vector2(-1f, 0.45f);
                Vector2 head = from + way * (420f * t);
                float fade = (1f - t) * s;
                Gfx.Line(sb, head - way * 90f, head, Palette.SpotHoly * (0.35f * fade), 3f);
                Gfx.Line(sb, head - way * 50f, head, Palette.SpotHolyHot * (0.8f * fade), 1.5f);
                Gfx.DrawGlow(sb, head.X, head.Y, 14f, Palette.SpotHolyHot * (0.7f * fade));
            }
        }

        //Hash : a steady random 0..1 from two whole numbers (the same trick as TacetField)
        private static float Hash(int a, int b)
        {
            float h = (float)Math.Sin(a * 12.9898f + b * 78.233f) * 43758.547f;
            return h - (float)Math.Floor(h);
        }
    }
}
