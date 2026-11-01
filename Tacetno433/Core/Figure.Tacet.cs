using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //Figure Tacet (Beta1 1 Nov, MATH ART) : TACET's fighters, the four looks every enemy wears (Data/Enemy.cs Looks,
    //docs/ASSET_LIST.md 1.2) and the six bosses. Black with a white edge, a white light inside, drawn in blocks of 2 game
    //pixels (PIXEL LOOK) : TACET is the silence breaking the picture, the band is the light (the user let Iris mix the two
    //looks, 13.79). Each shape comes from the enemy's own words :
    //   hush             "The first thing to go is the echo."  a veil with a finger on its lips, its echoes drift away
    //   static           "It sounds like something. It is not."  blocks that never sit still
    //   the_damper       "A felt hand resting on every string."  a heavy body behind a great felt pad
    //   the_mute_choir   "A hundred mouths, open, making nothing."  rows of open mouths under a pale halo
    //   requiem          the piece never finished : a broken halo, a body that ends in loose staff lines, falling tears (LACRIMOSA)
    //   the_nameless_master  a blank mask and a shadow that copies it a moment late (the name that was taken, ITALIEN)
    //   the_devil_s_string   one string through its heart, a scroll for a head with horns, the bow as a scythe
    //   the_deaf_crown   a crown and two great hands that give orders, lightning between them, no ears (THE NINTH)
    //   the_staff        the staff that beat time on its own foot : it slams the floor, its body hangs from it (TE DEUM)
    //   the_blind_cantor a blindfold, organ pipes for wings, voices going out one after another in rings (IN THE DARK)
    //LEVEL 2 gives each one a body under the same parts (arms, a hood, legs).
    public partial class Figure
    {
        private const int Hush = 0;
        private const int Static = 1;
        private const int Damper = 2;
        private const int Choir = 3;
        private const int Requiem = 4;
        private const int Nameless = 5;
        private const int Devil = 6;
        private const int Crown = 7;
        private const int Staff = 8;
        private const int Cantor = 9;
        private int design;

        //Shift : where an extra copy is drawn (ECHO, the stage draws it more than once), page pixels
        public Vector2 Shift;

        private readonly Shard[] shards = new Shard[40];
        private int shardCount;

        private static int DesignOf(string key)
        {
            switch (key)
            {
                case "static": return Static;
                case "the_damper": return Damper;
                case "the_mute_choir": return Choir;
                case "requiem": return Requiem;
                case "the_nameless_master": return Nameless;
                case "the_devil_s_string": return Devil;
                case "the_deaf_crown": return Crown;
                case "the_staff": return Staff;
                case "the_blind_cantor": return Cantor;
                default: return Hush;
            }
        }

        //Set Up Tacet : how loose each part is, and the chains (hems, tails, banners)
        private void SetUpTacet()
        {
            design = DesignOf(Key);
            bodyLoose[Head] = 0.45f;
            bodyLoose[Chest] = 0.2f;
            bodyLoose[HandA] = 0.7f;
            bodyLoose[HandB] = 0.7f;
            bodyLoose[FootA] = 0.3f;
            bodyLoose[FootB] = 0.3f;
            bodyLoose[Extra] = 1f;
            chainCount = 0;                                                  // SYMBOLS (13.80) : no hems or tails, shapes only
        }

        //======================================================================================
        //Pose : TACET
        //======================================================================================

        private void PoseTacet()
        {
            float bob = (float)Math.Sin(clock * 1.6f + seed) * 0.014f;
            float s = move == "attack" ? Strike() : 0f;
            float lunge = Math.Max(0f, s) * 0.08f - Math.Max(0f, -s) * 0.03f;      // forward is toward the band
            bool body2 = Level == 2;
            float orbit = clock * 0.9f + seed;
            want[Extra] = Local((float)Math.Cos(orbit) * 0.3f, -0.6f + (float)Math.Sin(orbit) * 0.12f);

            switch (design)
            {
                case Static:
                    Set(Head, 0f + lunge, -0.86f + bob);
                    Set(Chest, 0f + lunge * 0.7f, body2 ? -0.62f : -0.56f + bob);
                    Set(Hip, 0f, -0.4f + bob * 0.5f);
                    Set(HandA, 0.17f + lunge * 1.6f, -0.52f + bob);
                    Set(HandB, -0.17f, -0.5f + bob);
                    Set(FootA, -0.08f, 0f);
                    Set(FootB, 0.08f, 0f);
                    break;
                case Damper:
                    Set(Head, 0.1f + lunge, -0.66f + bob * 0.5f);
                    Set(Chest, 0f + lunge * 0.5f, -0.64f + bob * 0.5f);
                    Set(Hip, -0.02f, -0.34f);
                    Set(HandA, 0.25f + lunge * 1.8f, -0.42f + bob - Math.Max(0f, -s) * 0.12f);
                    Set(HandB, -0.19f, -0.38f + bob * 0.5f);
                    Set(FootA, -0.12f, 0f);
                    Set(FootB, 0.1f, 0f);
                    break;
                case Choir:
                    Set(Head, 0f + lunge * 0.5f, -0.93f + bob);
                    Set(Chest, 0f + lunge * 0.4f, -0.66f + bob);
                    Set(Hip, 0f, -0.3f + bob * 0.5f);
                    Set(HandA, 0.15f + lunge * 0.5f, -0.86f + bob * 1.2f);
                    Set(HandB, -0.15f, -0.85f + bob * 0.8f);
                    Set(FootA, 0.33f + lunge * 1.5f, -0.6f + bob);
                    Set(FootB, -0.33f, -0.62f + bob);
                    break;
                case Requiem:
                    Set(Head, 0f, -0.8f + bob);
                    Set(Chest, 0f + lunge * 0.5f, -0.84f + bob);
                    Set(Hip, 0f, -0.48f + bob);
                    Set(HandA, 0.24f + lunge * 1.6f, -0.55f + bob * 1.4f);
                    Set(HandB, -0.21f, -0.5f + bob * 1.2f);
                    Set(FootA, -0.08f, -0.18f);
                    Set(FootB, 0.08f, -0.18f);
                    break;
                case Nameless:
                    Set(Head, 0f + lunge, (body2 ? -0.86f : -0.72f) + bob);
                    Set(Chest, 0f + lunge * 0.6f, -0.7f + bob);
                    Set(Hip, 0f, -0.45f + bob * 0.5f);
                    Set(HandA, 0.2f + lunge * 1.6f, -0.55f + bob);
                    Set(HandB, -0.18f, -0.5f + bob);
                    Set(FootA, -0.07f, 0f);
                    Set(FootB, 0.07f, 0f);
                    want[Extra] = body[Head].At + new Vector2(-0.11f * Facing, 0.03f) * Height;   // the shadow, late
                    break;
                case Devil:
                    Set(Head, 0.02f + lunge * 0.6f, -0.8f + bob);
                    Set(Chest, 0.02f + lunge * 0.4f, -0.62f + bob);
                    Set(Hip, 0.02f, -0.42f + bob * 0.6f);
                    Set(HandA, 0.2f + lunge * 1.5f, -0.58f + bob - Math.Max(0f, -s) * 0.15f);
                    Set(HandB, -0.16f, -0.52f + bob);
                    Set(FootA, -0.06f, 0f);
                    Set(FootB, 0.08f, 0f);
                    break;
                case Crown:
                    Set(Head, 0f + lunge * 0.4f, -0.88f + bob);
                    Set(Chest, 0f + lunge * 0.3f, -0.7f + bob * 0.6f);
                    Set(Hip, 0f, -0.4f + bob * 0.3f);
                    Set(HandA, 0.32f + lunge * 2f, -0.56f + bob * 1.5f - Math.Max(0f, -s) * 0.2f);
                    Set(HandB, -0.3f, -0.62f + bob * 1.3f);
                    Set(FootA, -0.12f, 0f);
                    Set(FootB, 0.12f, 0f);
                    break;
                case Staff:
                    float beat = StaffBeat();
                    Set(Head, 0.01f, -0.86f + bob - beat * 0.04f);
                    Set(Chest, 0f, -0.8f + bob - beat * 0.04f);
                    Set(Hip, -0.01f, -0.5f + bob * 0.6f - beat * 0.03f);
                    Set(HandA, 0.12f + lunge, -0.62f - beat * 0.05f);
                    Set(HandB, -0.16f, -0.52f + bob);
                    Set(FootA, -0.06f, -0.03f);
                    Set(FootB, 0.05f, 0f);
                    break;
                case Cantor:
                    Set(Head, 0f + lunge * 0.5f, -0.82f + bob);
                    Set(Chest, 0f + lunge * 0.4f, -0.66f + bob);
                    Set(Hip, 0f, -0.36f + bob * 0.4f);
                    Set(HandA, 0.3f + lunge * 1.6f, -0.55f + bob * 1.3f + (float)Math.Sin(clock * 2.1f) * 0.02f);
                    Set(HandB, 0.22f + lunge, -0.44f + bob + (float)Math.Sin(clock * 1.7f + 1f) * 0.02f);
                    Set(FootA, -0.08f, 0f);
                    Set(FootB, 0.08f, 0f);
                    break;
                default:
                    Set(Head, 0.02f + lunge, -0.66f + bob);
                    Set(Chest, 0f + lunge * 0.5f, -0.93f + bob * 1.3f);
                    Set(Hip, -0.01f, -0.42f + bob);
                    Set(HandA, 0.08f + lunge * 0.8f, -0.75f + bob);
                    Set(HandB, -0.17f, -0.42f + bob);
                    Set(FootA, -0.2f, -0.06f);
                    Set(FootB, 0.18f, -0.06f);
                    break;
            }
            weaponWant = 0f;
            weapon2Want = 0f;
        }

        private void Set(int part, float x, float y)
        {
            want[part] = Local(x, y);
        }

        //Staff Beat : THE STAFF slams the floor every few seconds, 1 at the slam (it has no feet left to walk on)
        private float StaffBeat()
        {
            float t = (clock + seed) % 1.7f;
            if (t < 0.35f) return Ease(t / 0.35f);
            if (t < 0.45f) return 1f - (t - 0.35f) / 0.1f;
            return 0f;
        }

        //======================================================================================
        //Shards : a beaten enemy breaks
        //======================================================================================

        //Shatter : the parts become pieces thrown out and up, then pulled into the hole (SILENCED)
        private void Shatter()
        {
            shardCount = 0;
            int count = Of == Sort.Boss ? 36 : 20;
            float scale = Height / 256f;
            for (int i = 0; i < count && shardCount < shards.Length; i++)
            {
                int part = i % 8;
                float a = Hash(i, 7) * MathHelper.TwoPi;
                float power = (140f + 260f * Hash(i, 11)) * scale;
                Shard s;
                s.At = body[part].At + new Vector2(Hash(i, 3) - 0.5f, Hash(i, 5) - 0.5f) * (Height * 0.25f);
                s.Speed = new Vector2((float)Math.Cos(a) * power + 120f * scale * -Facing, (float)Math.Sin(a) * power - 220f * scale);   // thrown back, away from the band
                s.Angle = a;
                s.Spin = (Hash(i, 13) - 0.5f) * 14f;
                s.Size = (0.03f + 0.05f * Hash(i, 17)) * Height;
                s.Shape = i % 3;
                shards[shardCount++] = s;
            }
        }

        //Update Shards : gravity, the floor, a little drag, and after a moment the hole pulls them in
        private void UpdateShards(float dt)
        {
            if (shardCount == 0) return;
            float floor = Feet.Y;
            float pullIn = MathHelper.Clamp((moveTime - BattleRules.FigureShardHang) / 0.6f, 0f, 1f);
            for (int i = 0; i < shardCount; i++)
            {
                Shard s = shards[i];
                s.Speed.Y += BattleRules.FigureGravity * (1f - pullIn) * dt;
                s.Speed += (Hole - s.At) * (BattleRules.FigureShardPull * pullIn * pullIn * dt);
                s.Speed *= 1f - Math.Min(0.9f, dt * (1f + pullIn * 3f));
                s.At += s.Speed * dt;
                s.Angle += s.Spin * dt;
                if (pullIn < 0.5f && s.At.Y > floor && s.Speed.Y > 0f)            // BOUNCE on the floor, losing most of the speed
                {
                    s.At.Y = floor;
                    s.Speed.Y *= -BattleRules.FigureShardBounce;
                    s.Speed.X *= 0.6f;
                    s.Spin *= 0.5f;
                }
                shards[i] = s;
            }
        }

        //======================================================================================
        //Draw : TACET
        //======================================================================================

        private float tGrow;          // the part being drawn is this much bigger all round (the white edge pass)
        private Color tColor;

        //Draw Tacet : three passes. The edge (every part a little bigger, in white), the body (every part in black over it, so
        //only the outline of the whole stays white), then the white lights inside.
        private void DrawTacet(float alpha, float flash, float light, float mark)
        {
            float block = BattleRules.FigurePixel;
            Color edge = Color.Lerp(Palette.Paper * light, Color.White, mark) * alpha;
            Color fill = Color.Lerp(BodyInk, Color.White, flash) * alpha;
            if (shardCount > 0)
            {
                DrawShards(block, edge, fill);
                return;
            }
            if (down) return;
            tGrow = block * (1f + mark);
            tColor = edge;
            DrawTacetParts();
            tGrow = 0f;
            tColor = fill;
            DrawTacetParts();
            DrawTacetLights(alpha * (0.6f + 0.4f * light), flash);
        }

        private static readonly Color BodyInk = new Color(20, 20, 24);       // a little above the hole's black, so a body reads in front of it

        private void DrawShards(float block, Color edge, Color fill)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < shardCount; i++)
                {
                    Shard s = shards[i];
                    float size = s.Size + (pass == 0 ? block * 2f : 0f);
                    Color c = pass == 0 ? edge : fill;
                    Vector2 at = s.At + Shift;
                    if (s.Shape == 1) Shapes.Box(at, size, size * 0.7f, s.Angle, c);
                    else if (s.Shape == 2) Shapes.Kite(at - new Vector2((float)Math.Cos(s.Angle), (float)Math.Sin(s.Angle)) * size, at + new Vector2((float)Math.Cos(s.Angle), (float)Math.Sin(s.Angle)) * size, size * 0.45f, 0.4f, c);
                    else Shapes.Polygon(at, size * 0.7f, 4, s.Angle, c);
                }
            }
        }

        //TACET helpers : every part through these, so the edge pass makes it bigger by tGrow
        private void TCircle(Vector2 at, float r)
        {
            Shapes.Circle(at + Shift, r + tGrow, tColor);
        }

        private void TEllipse(Vector2 at, float rx, float ry, float rot)
        {
            Shapes.Ellipse(at + Shift, rx + tGrow, ry + tGrow, rot, tColor, tColor);
        }

        private void TBox(Vector2 at, float w, float h, float rot)
        {
            Shapes.Box(at + Shift, w + tGrow * 2f, h + tGrow * 2f, rot, tColor);
        }

        private void TKite(Vector2 root, Vector2 tip, float width, float widest)
        {
            Vector2 d = tip - root;
            if (d.LengthSquared() < 0.01f) return;
            d.Normalize();
            Shapes.Kite(root - d * tGrow + Shift, tip + d * tGrow + Shift, width + tGrow * 2f, widest, tColor);
        }

        private void TLine(Vector2 a, Vector2 b, float w0, float w1)
        {
            Shapes.Taper(a + Shift, b + Shift, w0 + tGrow * 2f, w1 + tGrow * 2f, tColor);
        }

        //T Poly : the points in line, made bigger round their middle
        private void TPoly(int n)
        {
            Vector2 middle = Vector2.Zero;
            for (int i = 0; i < n; i++) middle += line[i];
            middle /= n;
            for (int i = 0; i < n; i++)
            {
                Vector2 out1 = line[i] - middle;
                if (out1.LengthSquared() > 0.01f) out1.Normalize();
                line[i] += out1 * tGrow + Shift;
            }
            Shapes.Fill(line, n, tColor);
        }

        private void TCrescent(Vector2 at, float r, float from, float to, float thick)
        {
            Shapes.Crescent(at + Shift, r, from, to, thick + tGrow * 2f, tColor);
        }

        private void TArc(Vector2 at, float r, float from, float to, float thick)
        {
            Shapes.Arc(at + Shift, r, from, to, thick + tGrow * 2f, thick + tGrow * 2f, tColor);
        }

        //T Limb : an arm or a leg (LEVEL 2), one straight tapered bar like the band's (SYMBOLS, no elbows or knees)
        private void TLimb(Vector2 root, Vector2 end, float length, float wide, float thin, float side)
        {
            TLine(root, end, wide, thin);
        }

        //Teardrop : round at the bottom, sharp at the top (a veil, a tear)
        private void TDrop(Vector2 tip, Vector2 bulb, float r)
        {
            Vector2 d = bulb - tip;
            float length = d.Length();
            if (length < 0.01f) return;
            d /= length;
            Vector2 n = new Vector2(-d.Y, d.X);
            int k = 0;
            line[k++] = tip;
            for (int i = 0; i <= 12; i++)
            {
                float a = -MathHelper.PiOver2 + MathHelper.Pi * i / 12f;
                line[k++] = bulb + n * ((float)Math.Sin(-a) * r) + d * ((float)Math.Cos(a) * r);
            }
            TPoly(k);
        }

        //Draw Tacet Parts : the black body of each design (drawn twice, see DrawTacet)
        private void DrawTacetParts()
        {
            float h = Height;
            bool body2 = Level == 2;
            Vector2 head = body[Head].At, chest = body[Chest].At, hip = body[Hip].At;
            Vector2 handA = body[HandA].At, handB = body[HandB].At;
            switch (design)
            {
                case Static:
                    if (body2)
                    {
                        TBox(Glitch(head, 0), 0.17f * h, 0.15f * h, 0f);
                        TBox(Glitch(chest, 1), 0.3f * h, 0.26f * h, 0f);
                        TBox(Glitch(hip, 2), 0.24f * h, 0.12f * h, 0f);
                        TLine(Glitch(chest + new Vector2(0.15f * Facing, -0.08f) * h, 3), handA, 0.07f * h, 0.06f * h);
                        TLine(Glitch(chest + new Vector2(-0.15f * Facing, -0.08f) * h, 4), handB, 0.07f * h, 0.06f * h);
                        TLine(hip + new Vector2(-0.07f * Facing, 0f) * h, body[FootA].At, 0.08f * h, 0.07f * h);
                        TLine(hip + new Vector2(0.07f * Facing, 0f) * h, body[FootB].At, 0.08f * h, 0.07f * h);
                    }
                    else
                    {
                        for (int k = 0; k < 8; k++)
                        {
                            float w = (0.1f + 0.12f * Hash(k, 41)) * h;
                            float hh = (0.06f + 0.1f * Hash(k, 43)) * h;
                            Vector2 at = chest + new Vector2((Hash(k, 45) - 0.5f) * 0.42f * Facing, (Hash(k, 47) - 0.5f) * 0.5f) * h;
                            TBox(Glitch(at, k), w, hh, 0f);
                        }
                    }
                    break;

                case Damper:
                    if (body2)
                    {
                        TLimb(hip + new Vector2(-0.08f * Facing, 0f) * h, body[FootA].At, 0.34f * h, 0.12f * h, 0.09f * h, Facing);
                        TLimb(hip + new Vector2(0.08f * Facing, 0f) * h, body[FootB].At, 0.34f * h, 0.12f * h, 0.09f * h, Facing);
                        TLimb(chest + new Vector2(-0.14f * Facing, 0.02f) * h, handB, 0.34f * h, 0.1f * h, 0.07f * h, Facing);
                        line[0] = chest + new Vector2(-0.2f * Facing, -0.06f) * h;
                        line[1] = chest + new Vector2(0.18f * Facing, -0.1f) * h;
                        line[2] = hip + new Vector2(0.16f * Facing, 0f) * h;
                        line[3] = hip + new Vector2(-0.18f * Facing, 0.02f) * h;
                        TPoly(4);
                        TCircle(head, 0.08f * h);
                        TLimb(chest + new Vector2(0.14f * Facing, -0.04f) * h, handA, 0.36f * h, 0.12f * h, 0.09f * h, -Facing);
                    }
                    else
                    {
                        line[0] = chest + new Vector2(-0.16f * Facing, 0f) * h;
                        line[1] = chest + new Vector2(0.14f * Facing, 0f) * h;
                        line[2] = Local(0.22f, -0.02f);
                        line[3] = Local(-0.25f, -0.02f);
                        TPoly(4);
                    }
                    TEllipse(handA, 0.085f * h, 0.17f * h, 0f);                                          // the felt pad
                    break;

                case Choir:
                    //A HUNDRED MOUTHS : a pyramid of round heads (one, two, three) on a tall body, each a mouth open on nothing
                    line[0] = chest + new Vector2(-0.08f * Facing, 0.02f) * h;
                    line[1] = chest + new Vector2(0.08f * Facing, 0.02f) * h;
                    line[2] = hip + new Vector2(0.2f * Facing, 0.26f) * h;
                    line[3] = hip + new Vector2(-0.22f * Facing, 0.26f) * h;
                    TPoly(4);
                    if (body2)
                    {
                        TLimb(chest + new Vector2(0.06f * Facing, 0.04f) * h, body[FootA].At, 0f, 0.05f * h, 0.03f * h, 0f);
                        TLimb(chest + new Vector2(-0.06f * Facing, 0.04f) * h, body[FootB].At, 0f, 0.05f * h, 0.03f * h, 0f);
                        THand(body[FootA].At, 0.035f * h);
                        THand(body[FootB].At, 0.035f * h);
                    }
                    for (int k = 0; k < 6; k++) TCircle(ChoirHead(k), 0.06f * h);
                    break;

                case Requiem:
                    if (body2)
                    {
                        TLimb(chest + new Vector2(0.1f * Facing, 0.05f) * h, handA, 0.4f * h, 0.06f * h, 0.03f * h, -Facing);
                        TLimb(chest + new Vector2(-0.1f * Facing, 0.05f) * h, handB, 0.4f * h, 0.06f * h, 0.03f * h, Facing);
                    }
                    TDrop(chest + new Vector2(0f, -0.08f) * h, hip, 0.16f * h);
                    for (int k = 0; k < 5; k++)                                                          // the staff lines it ends in
                    {
                        float y = hip.Y + (0.14f + k * 0.065f) * h;
                        float gap = (float)Math.Sin(clock * 0.7f + k * 1.3f) * 0.05f * h;
                        float wide = (0.3f - k * 0.035f) * h;
                        TBox(new Vector2(hip.X - wide * 0.3f + gap, y), wide * 0.45f, 0.022f * h, 0f);
                        TBox(new Vector2(hip.X + wide * 0.35f - gap, y), wide * 0.3f, 0.022f * h, 0f);
                    }
                    break;

                case Nameless:
                    for (int twin = 0; twin < 2; twin++)
                    {
                        Vector2 move1 = twin == 0 ? body[Extra].At - head : Vector2.Zero;               // the shadow first, behind
                        if (body2)
                        {
                            line[0] = chest + move1 + new Vector2(-0.13f * Facing, 0f) * h;
                            line[1] = chest + move1 + new Vector2(0.12f * Facing, 0f) * h;
                            line[2] = hip + move1 + new Vector2(0.09f * Facing, 0.05f) * h;
                            line[3] = hip + move1 + new Vector2(-0.1f * Facing, 0.05f) * h;
                            TPoly(4);
                            TLimb(hip + move1 + new Vector2(-0.05f * Facing, 0f) * h, body[FootA].At + move1, 0.46f * h, 0.06f * h, 0.035f * h, Facing);
                            TLimb(hip + move1 + new Vector2(0.05f * Facing, 0f) * h, body[FootB].At + move1, 0.46f * h, 0.06f * h, 0.035f * h, Facing);
                            TLimb(chest + move1 + new Vector2(0.1f * Facing, 0f) * h, handA + move1, 0.32f * h, 0.05f * h, 0.03f * h, -Facing);
                            TLimb(chest + move1 + new Vector2(-0.1f * Facing, 0f) * h, handB + move1, 0.32f * h, 0.05f * h, 0.03f * h, Facing);
                        }
                        line[0] = head + move1 + new Vector2(0f, -0.13f) * h;                            // the mask : a blank diamond
                        line[1] = head + move1 + new Vector2(0.085f * Facing, 0f) * h;
                        line[2] = head + move1 + new Vector2(0f, 0.15f) * h;
                        line[3] = head + move1 + new Vector2(-0.085f * Facing, 0f) * h;
                        TPoly(4);
                    }
                    break;

                case Devil:
                    if (body2)
                    {
                        TLine(chest, hip, 0.07f * h, 0.05f * h);
                        TLimb(hip, body[FootA].At, 0.46f * h, 0.045f * h, 0.025f * h, Facing);
                        TLimb(hip, body[FootB].At, 0.46f * h, 0.045f * h, 0.025f * h, Facing);
                        TLimb(chest + new Vector2(-0.05f * Facing, 0f) * h, handB, 0.34f * h, 0.035f * h, 0.02f * h, Facing);
                        TLimb(chest + new Vector2(0.05f * Facing, 0f) * h, handA, 0.34f * h, 0.035f * h, 0.02f * h, -Facing);
                    }
                    else TDrop(chest + new Vector2(0f, -0.04f) * h, hip, 0.09f * h);
                    TCircle(head, 0.07f * h);                                                          // the scroll
                    TCrescent(head + new Vector2(-0.04f * Facing, -0.03f) * h, 0.09f * h, -2.6f, -1.5f, 0.03f * h);   // horns
                    TCrescent(head + new Vector2(0.04f * Facing, -0.03f) * h, 0.09f * h, -1.6f, -0.5f, 0.03f * h);
                    TLine(handA - new Vector2(0f, -0.05f) * h, handA + new Vector2(0.02f * Facing, -0.36f) * h, 0.018f * h, 0.012f * h);   // the bow's stick
                    TCrescent(handA + new Vector2(-0.06f * Facing, -0.36f) * h, 0.16f * h, Facing > 0f ? -0.2f : MathHelper.Pi - 1.2f,
                              Facing > 0f ? 1.2f : MathHelper.Pi + 0.2f, 0.045f * h);                    // its blade, a scythe
                    break;

                case Crown:
                    if (body2)
                    {
                        TLimb(chest + new Vector2(0.17f * Facing, 0f) * h, handA, 0.42f * h, 0.09f * h, 0.05f * h, -Facing);
                        TLimb(chest + new Vector2(-0.17f * Facing, 0f) * h, handB, 0.42f * h, 0.09f * h, 0.05f * h, Facing);
                    }
                    line[0] = chest + new Vector2(-0.22f * Facing, -0.02f) * h;
                    line[1] = chest + new Vector2(0.22f * Facing, -0.02f) * h;
                    line[2] = hip + new Vector2(0.24f * Facing, 0.22f) * h;
                    line[3] = hip + new Vector2(-0.26f * Facing, 0.22f) * h;
                    TPoly(4);
                    TCircle(head + new Vector2(0f, 0.09f) * h, 0.085f * h);
                    for (int k = 0; k < 5; k++)                                                          // the crown's points
                    {
                        float x = (-0.1f + k * 0.05f) * Facing;
                        float tall = (k == 2 ? 0.13f : (k == 1 || k == 3 ? 0.1f : 0.075f)) * h;
                        TKite(head + new Vector2(x * h, 0.03f * h), head + new Vector2(x * h, 0.03f * h - tall), 0.045f * h, 0.15f);
                    }
                    TBox(head + new Vector2(0f, 0.035f) * h, 0.26f * h, 0.035f * h, 0f);
                    THand(handA, 0.075f * h);
                    THand(handB, 0.075f * h);
                    break;

                case Staff:
                    float beat = StaffBeat();
                    Vector2 foot = Local(0.13f, 0f) + new Vector2(0f, -beat * 0.12f * h);
                    TLine(foot, foot + new Vector2(0f, -0.96f * h), 0.03f * h, 0.025f * h);                // the staff
                    TBox(foot + new Vector2(0f, -0.98f * h), 0.06f * h, 0.06f * h, MathHelper.PiOver4);
                    TCircle(foot + new Vector2(0f, -0.9f * h), 0.03f * h);
                    line[0] = chest + new Vector2(-0.16f * Facing, 0f) * h;
                    line[1] = chest + new Vector2(0.1f * Facing, 0f) * h;
                    line[2] = hip + new Vector2(0.08f * Facing, 0.1f) * h;
                    line[3] = hip + new Vector2(-0.14f * Facing, 0.1f) * h;
                    TPoly(4);
                    TCircle(head, 0.065f * h);
                    if (body2)
                    {
                        TLimb(chest + new Vector2(0.08f * Facing, 0f) * h, new Vector2(foot.X, handA.Y), 0.3f * h, 0.05f * h, 0.035f * h, -Facing);
                        TLimb(chest + new Vector2(-0.1f * Facing, 0f) * h, handB, 0.3f * h, 0.05f * h, 0.035f * h, Facing);
                        TLimb(hip + new Vector2(0.04f * Facing, 0f) * h, body[FootB].At, 0.5f * h, 0.06f * h, 0.035f * h, Facing);
                    }
                    break;

                case Cantor:
                    for (int k = 0; k < 6; k++)                                                          // organ pipes for wings
                    {
                        float tall = (0.3f + 0.22f * (float)Math.Sin(k * 0.9f + 0.4f)) * h;
                        float x = (0.16f + k * 0.055f) * h;
                        TBox(chest + new Vector2(x * Facing, -tall * 0.35f), 0.04f * h, tall, 0f);
                        TBox(chest + new Vector2(-x * Facing, -tall * 0.35f), 0.04f * h, tall, 0f);
                    }
                    line[0] = chest + new Vector2(-0.14f * Facing, -0.04f) * h;
                    line[1] = chest + new Vector2(0.14f * Facing, -0.04f) * h;
                    line[2] = hip + new Vector2(0.17f * Facing, 0.3f) * h;
                    line[3] = hip + new Vector2(-0.19f * Facing, 0.3f) * h;
                    TPoly(4);
                    TCircle(head, 0.085f * h);
                    if (body2)
                    {
                        TLimb(chest + new Vector2(0.1f * Facing, 0f) * h, handA, 0.4f * h, 0.05f * h, 0.03f * h, -Facing);
                        TLimb(chest + new Vector2(0.06f * Facing, 0.04f) * h, handB, 0.36f * h, 0.045f * h, 0.03f * h, -Facing);
                    }
                    THand(handA, 0.075f * h);
                    THand(handB, 0.05f * h);
                    break;

                default:                                                                                 // hush
                    if (body2)
                    {
                        line[0] = chest + new Vector2(0f, -0.02f) * h;                                   // the hood and cloak
                        line[1] = Local(0.2f, -0.06f) + new Vector2(0f, hip.Y - Local(0f, -0.42f).Y);
                        line[2] = Local(-0.02f, 0.01f) + new Vector2(0f, hip.Y - Local(0f, -0.42f).Y);
                        line[3] = Local(-0.24f, -0.06f) + new Vector2(0f, hip.Y - Local(0f, -0.42f).Y);
                        TPoly(4);
                        TLimb(chest + new Vector2(-0.06f * Facing, 0.24f) * h, handB, 0.36f * h, 0.06f * h, 0.035f * h, Facing);
                        TLimb(chest + new Vector2(0.09f * Facing, 0.25f) * h, handA, 0.24f * h, 0.055f * h, 0.035f * h, Facing);
                    }
                    else TDrop(chest, hip, 0.21f * h);
                    break;
            }
        }

        //Choir Head : where THE MUTE CHOIR's heads sit, a pyramid of one, two and three over its body, each bobbing on its own
        private Vector2 ChoirHead(int k)
        {
            int row = k == 0 ? 0 : (k < 3 ? 1 : 2);
            int col = k == 0 ? 0 : (k < 3 ? k - 1 : k - 3);
            float x = (col - row * 0.5f) * 0.13f;
            float y = -0.27f + row * 0.12f;
            float bob = (float)Math.Sin(clock * 2f + k * 1.3f) * 0.006f;
            return body[Chest].At + new Vector2(x * Facing, y + bob) * Height;
        }

        //Glitch : STATIC's blocks jump to a new place every so often, each on its own beat
        private Vector2 Glitch(Vector2 at, int k)
        {
            int step = (int)((clock + k * 0.37f) * 7f);
            if (Hash(step, k) > 0.7f) return at + new Vector2((Hash(step, k + 19) - 0.5f) * 0.12f * Height, 0f);
            return at;
        }

        //T Hand : a hand of TACET, a diamond (THE DEAF CROWN's great hands that give orders, THE BLIND CANTOR's that feel the way)
        private void THand(Vector2 at, float size)
        {
            Shapes.Polygon(at + Shift, size + tGrow * 1.4f, 4, 0f, tColor);
        }

        //Draw Tacet Lights : the white inside, where TACET's silence shows (never the band's colours, CLAUDE.md colours)
        private void DrawTacetLights(float alpha, float flash)
        {
            float h = Height;
            Color white = Color.White * alpha;
            Color grey = Palette.PaperDim * alpha;
            Vector2 head = body[Head].At + Shift, chest = body[Chest].At + Shift, hip = body[Hip].At + Shift;
            switch (design)
            {
                case Static:
                    {
                        bool on = Hash((int)(clock * 9f), 3) > 0.25f;
                        if (on) Shapes.Box(Glitch(chest, 9) + new Vector2(0f, -0.02f * h), 0.32f * h, 0.012f * h, 0f, white);
                        for (int k = 0; k < 5; k++)
                        {
                            int step = (int)(clock * 6f) + k * 13;
                            Vector2 at = chest + new Vector2((Hash(step, 5) - 0.5f) * 0.5f, (Hash(step, 9) - 0.5f) * 0.6f) * h;
                            Shapes.Box(at, BattleRules.FigurePixel, BattleRules.FigurePixel, 0f, grey);
                        }
                    }
                    break;
                case Damper:
                    Shapes.Box(head + new Vector2(0.02f * Facing, 0f) * h, 0.11f * h, 0.012f * h, 0f, white);   // the slit
                    Shapes.Ring(body[HandA].At + Shift, 0.06f * h, 0.008f * h, grey);                     // the felt's seam
                    break;
                case Choir:
                    for (int k = 0; k < 6; k++)
                    {
                        float open = 0.35f + 0.65f * (0.5f + 0.5f * (float)Math.Sin(clock * 2.4f + k * 1.9f));
                        Vector2 at = ChoirHead(k) + Shift + new Vector2(0f, 0.012f * h);
                        Shapes.Ellipse(at, 0.026f * h, 0.034f * h * open + BattleRules.FigurePixel, 0f, white, white);
                        Shapes.Ellipse(at, 0.016f * h, 0.026f * h * open, 0f, Color.Black * alpha, Color.Black * alpha);
                    }
                    break;
                case Requiem:
                    Shapes.Arc(head, 0.2f * h, -1.2f, 3.6f, 0.014f * h, 0.006f * h, white);              // the broken halo
                    Shapes.Circle(chest + new Vector2(0f, 0.12f) * h, 0.022f * h, white);
                    for (int k = 0; k < 5; k++)                                                          // LACRIMOSA : the tears
                    {
                        float t = (clock * 0.45f + k * 0.21f) % 1f;
                        Vector2 at = head + new Vector2((Hash(k, 61) - 0.5f) * 0.32f * h, (0.1f + t * 0.75f) * h);
                        Shapes.Circle(at, 0.009f * h, white * (1f - t));
                        Shapes.Kite(at, at - new Vector2(0f, 0.03f * h), 0.018f * h, 0.1f, white * (1f - t));
                    }
                    break;
                case Nameless:
                    Shapes.Box(head + new Vector2(0f, 0.01f) * h, 0.11f * h, 0.006f * h, 0f, grey);      // where its face was
                    Shapes.Circle(chest + new Vector2(0f, 0.06f) * h, 0.014f * h, white);
                    break;
                case Devil:
                    {
                        float top = -0.2f * h;                                                           // ON ONE STRING : it shivers
                        for (int i = 0; i < 16; i++)
                        {
                            float t = i / 15f;
                            float y = MathHelper.Lerp(top, Feet.Y + Shift.Y, t);
                            float wobble = (float)Math.Sin(t * MathHelper.Pi) * (float)Math.Sin(clock * 31f) * 0.012f * h * (move == "attack" ? 3f : 1f);
                            line[i] = new Vector2(chest.X + wobble, y);
                            lineW[i] = 0.006f * h;
                        }
                        Shapes.Stroke(line, lineW, 16, white, white * 0.5f, false);
                        Shapes.Arc(head, 0.04f * h, clock * 2f, clock * 2f + 4.5f, 0.012f * h, 0.004f * h, white);   // the scroll's curl
                    }
                    break;
                case Crown:
                    if (Hash((int)(clock * 11f), 2) > 0.45f)                                             // lightning between the hands
                    {
                        Vector2 a = body[HandA].At + Shift, b = body[HandB].At + Shift;
                        int seedStep = (int)(clock * 11f);
                        for (int i = 0; i < 9; i++)
                        {
                            float t = i / 8f;
                            Vector2 p = Vector2.Lerp(a, b, t) + new Vector2(0f, -(float)Math.Sin(t * MathHelper.Pi) * 0.2f * h);
                            if (i > 0 && i < 8) p += new Vector2((Hash(seedStep, i) - 0.5f) * 0.06f * h, (Hash(seedStep, i + 9) - 0.5f) * 0.06f * h);
                            line[i] = p;
                            lineW[i] = 0.007f * h;
                        }
                        Shapes.Stroke(line, lineW, 9, white, white, false);
                    }
                    Shapes.Circle(head + new Vector2(0f, 0.035f) * h, 0.012f * h, white);
                    break;
                case Staff:
                    {
                        float t = ((clock + seed) % 1.7f - 0.45f) / 0.8f;                                // TE DEUM : the beat on the floor
                        if (t > 0f && t < 1f)
                        {
                            Vector2 foot = Local(0.13f, 0f) + Shift;
                            Shapes.Arc(foot, 0.1f * h + t * 0.4f * h, MathHelper.Pi, MathHelper.TwoPi, 0.01f * h * (1f - t), 0.01f * h * (1f - t), white * (1f - t));
                        }
                        Shapes.Circle(head, 0.012f * h, white);
                    }
                    break;
                case Cantor:
                    Shapes.Box(head + new Vector2(0f, -0.005f) * h, 0.19f * h, 0.035f * h, 0f, white);   // the blindfold
                    for (int k = 0; k < 3; k++)                                                          // voices, one after another
                    {
                        float t = (clock * 0.5f + k / 3f) % 1f;
                        Shapes.Ring(head, (0.1f + t * 0.4f) * h, 0.006f * h * (1f - t), white * (0.8f * (1f - t)));
                    }
                    break;
                default:                                                                                 // hush
                    {
                        Vector2 finger = Level == 2 ? body[HandA].At + Shift + new Vector2(-0.005f * Facing, -0.06f) * h : head;
                        Shapes.Box(finger, 0.024f * h, 0.13f * h, 0f, white);                             // a finger on its lips
                        Shapes.Circle(finger + new Vector2(0f, -0.065f * h), 0.012f * h, white);
                        for (int k = 0; k < 5; k++)                                                      // the echo going away
                        {
                            float t = (clock * 0.22f + k * 0.2f) % 1f;
                            float a = -2.4f + t * 2.6f;
                            Vector2 at = chest + new Vector2(0f, 0.3f * h) + new Vector2((float)Math.Cos(a) * Facing, (float)Math.Sin(a)) * ((0.32f + t * 0.12f) * h);
                            float size = (0.05f - t * 0.03f) * h;
                            Shapes.Box(at, size, size, 0f, white * (0.9f * (1f - t)));
                            Shapes.Box(at, size * 0.55f, size * 0.55f, 0f, Color.Black * (1f - t));
                        }
                    }
                    break;
            }
        }

        //======================================================================================
        //Glow : TACET
        //======================================================================================

        private void GlowTacet(SpriteBatch sb, float alpha)
        {
            if (down && shardCount == 0) return;
            float h = Height;
            float power = BattleRules.FigureGlowTacet * alpha;
            Vector2 at = body[Head].At + Shift;
            if (design == Static || design == Damper || design == Choir) at = body[Chest].At + Shift;
            if (shardCount > 0)
            {
                for (int i = 0; i < shardCount; i += 4) Gfx.DrawGlow(sb, shards[i].At.X, shards[i].At.Y, shards[i].Size * 2f, Color.White * (power * 0.5f));
                return;
            }
            Gfx.DrawGlow(sb, at.X, at.Y, 0.18f * h, Color.White * power);
            if (design == Crown)
            {
                Gfx.DrawGlow(sb, body[HandA].At.X, body[HandA].At.Y, 0.12f * h, Color.White * (power * 0.6f));
                Gfx.DrawGlow(sb, body[HandB].At.X, body[HandB].At.Y, 0.12f * h, Color.White * (power * 0.6f));
            }
        }
    }
}
