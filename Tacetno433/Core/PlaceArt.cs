using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //PLACE ART (Beta1 2 Nov, block C item 1, docs/BETA1_DIRECTION.md 33) : the places the team will not draw, each as a small
    //scene of lines and light in the window that waited for a picture (the user 13.106 : math art everywhere but the Maestro
    //page). The reference is the creature cards of Worldless (docs/REFERENCE_GAMES.md) : one field of dark, one glowing core,
    //a few floating shapes that tell where it comes from, and room round them. Changed for TACET : the places of TACET are
    //black shapes holding white light (the TACET LOOK of the fight), the places of the band are warm, and no picture of a
    //thing is drawn, only shapes that hint at it.
    //   FIGHT     black shards turning round a white core
    //   ELITE     more and bigger shards, a broken crown of arcs over the core
    //   BOSS      the hole : a black disc with a bright rim and its rays
    //   CHANCE    an event or a crossing : layers of fog and a small light behind them, motes rising
    //   SHOP      three shelves, small relics glowing on them in many colours, a lamp above
    //   REST      a warm low light, embers rising, a calm ring on the floor
    //   WORKSHOP  a ring of teeth turning round a copper core, sparks when the hammer falls
    //   TREASURE  a box whose lid is lifted, gold light leaking out
    //Every shape is worked out from the window and the clock, nothing is made while a page draws.
    public enum PlaceKind { Fight, Elite, Boss, Chance, Shop, Rest, Workshop, Treasure }

    public static class PlaceArt
    {
        private static readonly Color Warm = new Color(255, 178, 92);
        private static readonly Color WarmHot = new Color(255, 236, 200);
        private static readonly Color Copper = new Color(255, 138, 61);
        private static readonly Color Gold = new Color(255, 204, 104);

        //Of : the place a kind of room on the map is
        public static PlaceKind Of(NodeType type)
        {
            switch (type)
            {
                case NodeType.Elite: return PlaceKind.Elite;
                case NodeType.Boss: return PlaceKind.Boss;
                case NodeType.Event:
                case NodeType.EraShift: return PlaceKind.Chance;
                case NodeType.Shop: return PlaceKind.Shop;
                case NodeType.Rest: return PlaceKind.Rest;
                case NodeType.Luthier: return PlaceKind.Workshop;
                case NodeType.Treasure: return PlaceKind.Treasure;
                default: return PlaceKind.Fight;
            }
        }

        //Draw : the place in box, its dark field first (field : a card's picture, false on a page where it lies on the stage),
        //then its shapes, then its light added on top
        public static void Draw(SpriteBatch sb, Rectangle box, PlaceKind kind, float time, float alpha, bool field = true)
        {
            if (alpha <= 0.01f) return;
            LegendArt.BeginClip(sb, box, BlendState.AlphaBlend);
            Vector2 mid = new Vector2(box.Center.X, box.Center.Y);
            float r = Math.Min(box.Width, box.Height) * 0.32f;
            if (field) Gfx.Rect(sb, box, Palette.Void * (0.55f * alpha));
            switch (kind)
            {
                case PlaceKind.Elite: Shards(sb, mid, r * 1.15f, time, alpha, 9, true); break;
                case PlaceKind.Boss: Hole(sb, mid, r, time, alpha); break;
                case PlaceKind.Chance: Fog(sb, box, mid, r, time, alpha); break;
                case PlaceKind.Shop: Shelves(sb, box, mid, r, time, alpha); break;
                case PlaceKind.Rest: Embers(sb, box, mid, r, time, alpha); break;
                case PlaceKind.Workshop: Teeth(sb, mid, r, time, alpha); break;
                case PlaceKind.Treasure: Chest(sb, mid, r, time, alpha); break;
                default: Shards(sb, mid, r, time, alpha, 6, false); break;
            }
            LegendArt.SwitchBlend(sb, BlendState.Additive);
            Light(sb, box, mid, r, kind, time, alpha);
            LegendArt.EndClip(sb);
        }

        //Hash : a steady random 0..1 from two whole numbers (the sine trick of TacetField)
        private static float Hash(int a, int b)
        {
            float h = (float)Math.Sin(a * 12.9898f + b * 78.233f) * 43758.547f;
            return h - (float)Math.Floor(h);
        }

        //FIGHT and ELITE : black shards with a pale rim turning round the white core, an ELITE crowned with broken arcs
        private static void Shards(SpriteBatch sb, Vector2 mid, float r, float time, float a, int count, bool crown)
        {
            for (int i = 0; i < count; i++)
            {
                float turn = time * (0.18f + 0.1f * Hash(i, 1)) + i * MathHelper.TwoPi / count;
                float reach = r * (0.55f + 0.45f * Hash(i, 2));
                Vector2 at = mid + new Vector2((float)Math.Cos(turn), (float)Math.Sin(turn) * 0.62f) * reach;
                float size = r * (0.1f + 0.12f * Hash(i, 3)) * (crown ? 1.25f : 1f);
                Gfx.Diamond(sb, at.X, at.Y, size + 1.5f, Palette.PaperDim * (0.55f * a));            // the pale rim
                Gfx.Diamond(sb, at.X, at.Y, size, Palette.Void * a);
            }
            if (crown)
                for (int i = 0; i < 5; i++)
                {
                    float from = -MathHelper.PiOver2 - 0.9f + i * 0.38f;
                    Gfx.Arc(sb, mid.X, mid.Y, r * 0.42f, from, from + 0.24f, Palette.Paper * (0.7f * a), 2f);
                }
            Gfx.Circle(sb, mid.X, mid.Y, r * 0.09f, Palette.Highlight * a);
        }

        //BOSS : the hole, a black disc, a bright rim and rays leaving it
        private static void Hole(SpriteBatch sb, Vector2 mid, float r, float time, float a)
        {
            for (int i = 0; i < 12; i++)
            {
                float turn = i * MathHelper.TwoPi / 12f + time * 0.05f;
                float reach = r * (1.2f + 0.5f * Hash(i, 5));
                Vector2 way = new Vector2((float)Math.Cos(turn), (float)Math.Sin(turn));
                Gfx.Line(sb, mid + way * r * 0.82f, mid + way * reach, Palette.Paper * (0.25f * a), 1.5f);
            }
            Gfx.Circle(sb, mid.X, mid.Y, r * 0.8f, Palette.Void * a);
            Gfx.Ring(sb, mid.X, mid.Y, r * 0.8f, Palette.Highlight * (0.9f * a), 2f);
        }

        //CHANCE : three layers of fog drifting at their own speeds, motes rising through them
        private static void Fog(SpriteBatch sb, Rectangle box, Vector2 mid, float r, float time, float a)
        {
            for (int i = 0; i < 26; i++)
            {
                float x = box.X + ((Hash(i, 7) * box.Width + time * (6f + 10f * Hash(i, 8))) % box.Width);
                float y = box.Bottom - ((Hash(i, 9) * box.Height + time * (14f + 16f * Hash(i, 10))) % box.Height);
                Gfx.Diamond(sb, x, y, 1f + 1.2f * Hash(i, 11), Palette.Paper * ((0.2f + 0.3f * Hash(i, 12)) * a));
            }
            Gfx.Circle(sb, mid.X, mid.Y, r * 0.07f, Palette.Paper * (0.8f * a));
        }

        //SHOP : three shelves, relics in many colours resting on them (each the colour of something one could buy)
        private static void Shelves(SpriteBatch sb, Rectangle box, Vector2 mid, float r, float time, float a)
        {
            Shelves(sb, box, mid, r, time, a, false);
        }

        //Shelves, glow : the same relics again as soft light, each in its own colour (drawn in the adding pass)
        private static void Shelves(SpriteBatch sb, Rectangle box, Vector2 mid, float r, float time, float a, bool glow)
        {
            float left = box.X + box.Width * 0.14f, right = box.Right - box.Width * 0.14f;
            for (int s = 0; s < 3; s++)
            {
                float y = mid.Y - r * 0.55f + s * r * 0.75f;
                if (!glow) Gfx.Line(sb, left, y, right, y, Palette.LineGrey * (0.7f * a), 1.5f);
                int items = 4 + s;
                for (int i = 0; i < items; i++)
                {
                    float x = MathHelper.Lerp(left + 20f, right - 20f, (i + 0.5f) / items) + (Hash(s * 9 + i, 13) - 0.5f) * 14f;
                    float size = r * (0.06f + 0.05f * Hash(s * 9 + i, 14));
                    float bob = (float)Math.Sin(time * 1.4f + i + s) * 2f;
                    Color tone = Relic(s * 9 + i);
                    if (glow)
                    {
                        Gfx.DrawGlow(sb, x, y - size - 3f + bob, size * 3.2f, tone * (0.3f * a));
                        continue;
                    }
                    int shape = (int)(Hash(s * 9 + i, 15) * 3f);
                    if (shape == 0) Gfx.Diamond(sb, x, y - size - 3f + bob, size, tone * a);
                    else if (shape == 1) Gfx.Circle(sb, x, y - size - 3f + bob, size * 0.8f, tone * a);
                    else Gfx.Triangle(sb, x, y - size - 3f + bob, size * 1.3f, true, tone * a);
                }
            }
        }

        private static Color Relic(int i)
        {
            switch (i % 5)
            {
                case 0: return Palette.SpotFire;
                case 1: return Palette.SpotWind;
                case 2: return Palette.SpotEarth;
                case 3: return Palette.SpotWater;
                default: return Palette.SpotLight;
            }
        }

        //REST : a low warm light on the floor, embers rising slowly, a calm ring round it
        private static void Embers(SpriteBatch sb, Rectangle box, Vector2 mid, float r, float time, float a)
        {
            Vector2 floor = new Vector2(mid.X, mid.Y + r * 0.6f);
            Gfx.EllipseOutline(sb, floor.X, floor.Y, r * 1.3f, r * 0.24f, Warm * (0.35f * a), 1.5f);
            for (int i = 0; i < 22; i++)
            {
                float rise = (Hash(i, 16) + time * (0.08f + 0.06f * Hash(i, 17))) % 1f;
                float x = floor.X + (Hash(i, 18) - 0.5f) * r * 1.2f + (float)Math.Sin(time * 1.3f + i) * 6f * rise;
                float y = floor.Y - rise * r * 2.2f;
                Gfx.Diamond(sb, x, y, 1.6f * (1f - rise) + 0.6f, Color.Lerp(WarmHot, Warm, rise) * ((1f - rise) * 0.9f * a));
            }
            Gfx.Circle(sb, floor.X, floor.Y - r * 0.1f, r * 0.1f, WarmHot * a);
        }

        //WORKSHOP : a ring of teeth turning round a copper core, sparks flying each time the hammer falls (every 1.6 seconds)
        private static void Teeth(SpriteBatch sb, Vector2 mid, float r, float time, float a)
        {
            float turn = time * 0.4f;
            Gfx.Ring(sb, mid.X, mid.Y, r * 0.7f, Copper * (0.6f * a), 2f);
            for (int i = 0; i < 14; i++)
            {
                float t = turn + i * MathHelper.TwoPi / 14f;
                Vector2 way = new Vector2((float)Math.Cos(t), (float)Math.Sin(t));
                Gfx.Line(sb, mid + way * r * 0.7f, mid + way * r * 0.86f, Copper * (0.8f * a), 4f);
            }
            float hit = (time % 1.6f) / 1.6f;
            if (hit < 0.25f)
            {
                float k = hit / 0.25f;
                for (int i = 0; i < 9; i++)
                {
                    float t = -MathHelper.PiOver2 + (Hash(i, 19) - 0.5f) * 2.2f;
                    Vector2 way = new Vector2((float)Math.Cos(t), (float)Math.Sin(t));
                    Vector2 at = mid + way * r * (0.15f + 0.9f * k);
                    Gfx.Line(sb, at, at + way * 7f, Gold * ((1f - k) * a), 1.5f);
                }
            }
            Gfx.Circle(sb, mid.X, mid.Y, r * 0.16f, Copper * a);
        }

        //TREASURE : a box seen from the front, its lid lifted, gold light leaking from the gap
        private static void Chest(SpriteBatch sb, Vector2 mid, float r, float time, float a)
        {
            float w = r * 1.3f, h = r * 0.8f;
            Rectangle body = new Rectangle((int)(mid.X - w / 2f), (int)(mid.Y - h * 0.2f), (int)w, (int)h);
            Gfx.Rect(sb, body, Palette.Void * a);
            Gfx.RectOutline(sb, body, Gold * (0.75f * a), 2);
            float lift = 0.32f + 0.05f * (float)Math.Sin(time * 1.8f);
            Vector2 hinge = new Vector2(body.X, body.Y);
            Vector2 tip = hinge + new Vector2((float)Math.Cos(-lift), (float)Math.Sin(-lift)) * w;
            Gfx.Line(sb, hinge, tip, Gold * (0.85f * a), 3f);
            for (int i = 0; i < 7; i++)
            {
                float x = body.X + w * (0.15f + i * 0.12f);
                float tall = r * (0.5f + 0.5f * Hash(i, 20)) * (0.8f + 0.2f * (float)Math.Sin(time * 2f + i));
                Gfx.Line(sb, x, body.Y - 2f, x + (x - mid.X) * 0.15f, body.Y - 2f - tall, Gold * (0.3f * a), 2f);
            }
        }

        //Light : each place's own glow, added on top
        private static void Light(SpriteBatch sb, Rectangle box, Vector2 mid, float r, PlaceKind kind, float time, float a)
        {
            float breath = 0.5f + 0.5f * (float)Math.Sin(time * 1.2f);
            switch (kind)
            {
                case PlaceKind.Fight:
                case PlaceKind.Elite:
                    Gfx.DrawGlow(sb, mid.X, mid.Y, r * 1.4f, Palette.Paper * ((0.12f + 0.05f * breath) * a));
                    Gfx.DrawGlow(sb, mid.X, mid.Y, r * 0.35f, Palette.Highlight * (0.55f * a));
                    break;
                case PlaceKind.Boss:
                    Gfx.DrawGlow(sb, mid.X, mid.Y, r * 1.6f, Palette.Paper * ((0.2f + 0.08f * breath) * a));
                    break;
                case PlaceKind.Chance:
                    for (int i = 0; i < 3; i++)
                    {
                        float drift = (float)Math.Sin(time * (0.2f + 0.1f * i) + i) * box.Width * 0.12f;
                        Gfx.DrawGlowBox(sb, new Rectangle((int)(box.X - 40 + drift), (int)(mid.Y - r * 0.3f + i * r * 0.5f), box.Width + 80, (int)(r * 0.5f)), Palette.Paper * (0.06f * a));
                    }
                    Gfx.DrawGlow(sb, mid.X, mid.Y, r * 0.5f, Palette.Paper * ((0.2f + 0.15f * breath) * a));
                    break;
                case PlaceKind.Shop:
                    Gfx.DrawGlow(sb, mid.X, mid.Y - r * 1.05f, r * 0.9f, Warm * (0.25f * a));                 // the lamp above
                    Gfx.DrawGlow(sb, mid.X, mid.Y - r * 1.05f, r * 0.15f, WarmHot * (0.7f * a));
                    Shelves(sb, box, mid, r, time, a, true);
                    break;
                case PlaceKind.Rest:
                    Gfx.DrawGlow(sb, mid.X, mid.Y + r * 0.5f, r * (1.5f + 0.15f * breath), Warm * (0.3f * a));
                    Gfx.DrawGlow(sb, mid.X, mid.Y + r * 0.5f, r * 0.3f, WarmHot * (0.6f * a));
                    break;
                case PlaceKind.Workshop:
                    float hit = (time % 1.6f) / 1.6f;
                    Gfx.DrawGlow(sb, mid.X, mid.Y, r * 1.1f, Copper * ((0.18f + 0.4f * Math.Max(0f, 1f - hit * 4f)) * a));
                    break;
                case PlaceKind.Treasure:
                    Gfx.DrawGlow(sb, mid.X, mid.Y - r * 0.2f, r * (1.2f + 0.1f * breath), Gold * (0.3f * a));
                    break;
            }
        }
    }
}
