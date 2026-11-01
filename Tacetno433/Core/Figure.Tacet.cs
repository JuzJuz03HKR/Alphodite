using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //Figure Tacet (Beta1 1 Nov, MATH ART) : TACET's fighters, the four looks every enemy wears (Data/Enemy.cs Looks,
    //docs/ASSET_LIST.md 1.2) and the six bosses. TACET is music's word for "be silent", so every one of them is built from
    //the signs of silence in a score (the user 13.82 : the music in the enemies too) and from its own words :
    //   hush             "The first thing to go is the echo."  its body is the quarter rest, a finger on its lips, its echoes
    //                    drift away as hollow squares
    //   static           "It sounds like something. It is not."  note heads with no stems that never sit still, broken staff
    //                    lines. LEVEL 2 : a note that walks, its stem for a body, its flag for a cape
    //   the_damper       "A felt hand resting on every string."  the whole rest hanging from its line, a great felt pad
    //                    pressed on four strings
    //   the_mute_choir   "A hundred mouths, open, making nothing."  hollow note heads for heads, open mouths, a robe of spikes
    //   requiem          the piece never finished : a broken halo, staff lines that stop, a quill for a blade, tears (LACRIMOSA)
    //   the_nameless_master  a blank mask, an empty frame where the name was, a shadow that copies it a moment late (ITALIEN)
    //   the_devil_s_string   one string through its heart, a violin's scroll for a head with horns, the bow as a scythe
    //   the_deaf_crown   a crown over an empty face (it cannot hear), two great hands that give orders, lightning (THE NINTH)
    //   the_staff        the conductor's staff it beat time with on its own foot : it slams the floor, its body is a banner of
    //                    the five staff lines (TE DEUM)
    //   the_blind_cantor a blindfold, organ pipes for wings, voices going out one after another in rings (IN THE DARK)
    //The look (the user's references 13.79 13.81) : sharp black shapes with a white edge, white cut through them where the eyes
    //and signs are, drawn in blocks of 2 game pixels with scanlines (PIXEL LOOK, BattleScreen.Stage.cs). LEVEL 2 gives each one
    //thin limbs under the same shapes.
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

        //Set Up Tacet : how loose each part is (SYMBOLS 13.80 : no hems or tails, shapes only)
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
            chainCount = 0;
        }

        //======================================================================================
        //Pose : TACET
        //======================================================================================

        private void PoseTacet()
        {
            float bob = (float)Math.Sin(clock * 1.6f + seed) * 0.014f;
            float s = move == "attack" ? Strike() : 0f;
            float lunge = Math.Max(0f, s) * 0.08f - Math.Max(0f, -s) * 0.03f;      // forward is toward the band
            float raise = Math.Max(0f, -s);                                        // drawn back before the blow
            bool body2 = Level == 2;
            float orbit = clock * 0.9f + seed;
            want[Extra] = Local((float)Math.Cos(orbit) * 0.3f, -0.6f + (float)Math.Sin(orbit) * 0.12f);

            switch (design)
            {
                case Static:
                    Set(Head, 0f + lunge, -0.84f + bob);
                    Set(Chest, 0f + lunge * 0.7f, body2 ? -0.62f : -0.56f + bob);
                    Set(Hip, 0f, -0.38f + bob * 0.5f);
                    Set(HandA, 0.17f + lunge * 1.6f, -0.52f + bob - raise * 0.1f);
                    Set(HandB, -0.15f, -0.5f + bob);
                    Set(FootA, -0.08f, 0f);
                    Set(FootB, 0.08f, 0f);
                    break;
                case Damper:
                    Set(Head, 0.06f + lunge, -0.66f + bob * 0.5f);
                    Set(Chest, 0f + lunge * 0.5f, -0.74f + bob * 0.5f);
                    Set(Hip, -0.02f, -0.36f);
                    Set(HandA, 0.25f + lunge * 1.8f, -0.42f + bob - raise * 0.14f);
                    Set(HandB, -0.21f, -0.38f + bob * 0.5f);
                    Set(FootA, -0.12f, 0f);
                    Set(FootB, 0.1f, 0f);
                    break;
                case Choir:
                    Set(Head, 0f + lunge * 0.5f, -0.93f + bob);
                    Set(Chest, 0f + lunge * 0.4f, -0.66f + bob);
                    Set(Hip, 0f, -0.3f + bob * 0.5f);
                    Set(HandA, 0.15f + lunge * 0.5f, -0.86f + bob * 1.2f);
                    Set(HandB, -0.15f, -0.85f + bob * 0.8f);
                    Set(FootA, 0.33f + lunge * 1.5f, -0.64f + bob - raise * 0.1f);
                    Set(FootB, -0.33f, -0.66f + bob);
                    break;
                case Requiem:
                    Set(Head, 0f + lunge * 0.4f, -0.83f + bob);
                    Set(Chest, 0f + lunge * 0.4f, -0.72f + bob);
                    Set(Hip, 0f, -0.42f + bob);
                    Set(HandA, 0.27f + lunge * 1.6f, -0.52f + bob * 1.4f - raise * 0.18f);
                    Set(HandB, -0.22f, -0.5f + bob * 1.2f);
                    Set(FootA, -0.08f, -0.08f);
                    Set(FootB, 0.08f, -0.08f);
                    break;
                case Nameless:
                    Set(Head, 0f + lunge, (body2 ? -0.86f : -0.76f) + bob);
                    Set(Chest, 0f + lunge * 0.6f, -0.7f + bob);
                    Set(Hip, 0f, -0.45f + bob * 0.5f);
                    Set(HandA, 0.22f + lunge * 1.6f, -0.56f + bob - raise * 0.12f);
                    Set(HandB, -0.18f, -0.5f + bob);
                    Set(FootA, -0.07f, 0f);
                    Set(FootB, 0.07f, 0f);
                    want[Extra] = body[Head].At + new Vector2(-0.11f * Facing, 0.03f) * Height;   // the shadow, late
                    break;
                case Devil:
                    Set(Head, 0.03f + lunge * 0.6f, -0.82f + bob);
                    Set(Chest, 0.03f + lunge * 0.4f, -0.66f + bob);
                    Set(Hip, 0.03f, -0.44f + bob * 0.6f);
                    Set(HandA, 0.22f + lunge * 1.5f, -0.6f + bob - raise * 0.15f);
                    Set(HandB, -0.16f, -0.5f + bob);
                    Set(FootA, -0.06f, 0f);
                    Set(FootB, 0.08f, 0f);
                    break;
                case Crown:
                    Set(Head, 0f + lunge * 0.4f, -0.86f + bob);
                    Set(Chest, 0f + lunge * 0.3f, -0.7f + bob * 0.6f);
                    Set(Hip, 0f, -0.4f + bob * 0.3f);
                    Set(HandA, 0.33f + lunge * 2f, -0.56f + bob * 1.5f - raise * 0.2f);
                    Set(HandB, -0.3f, -0.62f + bob * 1.3f);
                    Set(FootA, -0.12f, 0f);
                    Set(FootB, 0.12f, 0f);
                    break;
                case Staff:
                    float beat = StaffBeat();
                    Set(Head, 0.01f, -0.88f + bob - beat * 0.04f);
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
                default:                                                                 // hush
                    Set(Head, 0.03f + lunge, -0.8f + bob);
                    Set(Chest, 0f + lunge * 0.5f, -0.64f + bob);
                    Set(Hip, -0.02f, -0.4f + bob);
                    Set(HandA, 0.1f + lunge * 0.6f, -0.76f + bob);
                    Set(HandB, -0.15f + lunge * 1.2f, -0.38f + bob - raise * 0.08f);
                    Set(FootA, -0.06f, -0.12f);
                    Set(FootB, 0.06f, -0.12f);
                    break;
            }
            for (int b = 0; b < bits.Length; b++)
            {
                float a = clock * (0.5f + 0.1f * b) + b * 2.1f + seed;
                bitWant[b] = want[Chest] + new Vector2((float)Math.Cos(a) * 0.3f * Facing, (float)Math.Sin(a) * 0.16f) * Height;
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
        private static readonly Color BodyInk = new Color(20, 20, 24);       // a little above the hole's black, so a body reads in front of it

        //Draw Tacet : three passes. The edge (every part a little bigger, in white), the body (every part in black over it, so
        //only the outline of the whole stays white), then the white cut through it (eyes, signs).
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
            DrawTacetLights(alpha * (0.6f + 0.4f * light));
        }

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
                    Vector2 d = new Vector2((float)Math.Cos(s.Angle), (float)Math.Sin(s.Angle));
                    if (s.Shape == 1) Shapes.Box(at, size, size * 0.7f, s.Angle, c);
                    else if (s.Shape == 2) Shapes.Kite(at - d * size, at + d * size, size * 0.45f, 0.4f, c);
                    else Shapes.Polygon(at, size * 0.7f, 4, s.Angle, c);
                }
            }
        }

        //======================================================================================
        //TACET helpers : every part through these, so the edge pass makes it bigger by tGrow
        //======================================================================================

        //P : a point near a part, x toward the band, y down, in shares of the height
        private Vector2 P(Vector2 from, float x, float y)
        {
            return from + new Vector2(x * Facing, y) * Height;
        }

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

        //T Hand : a hand of TACET, a diamond (THE DEAF CROWN's great hands that give orders, THE BLIND CANTOR's that feel the way)
        private void THand(Vector2 at, float size)
        {
            Shapes.Polygon(at + Shift, size + tGrow * 1.4f, 4, 0f, tColor);
        }

        //T Claws : three thin spikes from a hand
        private void TClaws(Vector2 at, float length)
        {
            for (int k = 0; k < 3; k++)
            {
                float a = 1.1f + k * 0.35f;
                Vector2 d = new Vector2((float)Math.Cos(a) * Facing, (float)Math.Sin(a));
                TLine(at, at + d * length, 0.022f * Height, 0.004f * Height);
            }
        }

        //Note Head : a tilted oval, the sign of a note (STATIC's false notes, the choir's hollow heads)
        private float NoteTilt
        {
            get { return -0.42f * Facing; }
        }

        //======================================================================================
        //The designs : black bodies (drawn twice, see DrawTacet)
        //======================================================================================

        private void DrawTacetParts()
        {
            switch (design)
            {
                case Static: PartsStatic(); break;
                case Damper: PartsDamper(); break;
                case Choir: PartsChoir(); break;
                case Requiem: PartsRequiem(); break;
                case Nameless: PartsNameless(); break;
                case Devil: PartsDevil(); break;
                case Crown: PartsCrown(); break;
                case Staff: PartsStaff(); break;
                case Cantor: PartsCantor(); break;
                default: PartsHush(); break;
            }
        }

        //HUSH : the quarter rest, a slanted hood on top, two blades slanting opposite ways, a hook at the bottom
        private void PartsHush()
        {
            float h = Height;
            Vector2 head = body[Head].At, chest = body[Chest].At, hip = body[Hip].At;
            if (Level == 2)
            {
                TLine(P(chest, 0.07f, 0f), body[HandA].At, 0.035f * h, 0.018f * h);
                TLine(P(chest, -0.09f, 0.03f), body[HandB].At, 0.035f * h, 0.018f * h);
                TClaws(body[HandB].At, 0.07f * h);
            }
            line[0] = P(head, -0.07f, -0.09f); line[1] = P(head, 0.07f, -0.05f); line[2] = P(head, 0.065f, 0.075f); line[3] = P(head, -0.075f, 0.065f);
            TPoly(4);
            line[0] = P(chest, -0.05f, -0.05f); line[1] = P(chest, 0.11f, 0.03f); line[2] = P(chest, 0.03f, 0.17f); line[3] = P(chest, -0.12f, 0.08f);
            TPoly(4);
            line[0] = P(hip, 0.09f, -0.14f); line[1] = P(hip, -0.09f, -0.04f); line[2] = P(hip, -0.03f, 0.09f); line[3] = P(hip, 0.14f, 0f);
            TPoly(4);
            TCrescent(P(hip, 0.02f, 0.15f), 0.07f * h, 0.3f, 2.8f, 0.05f * h);
        }

        //STATIC : false notes. LEVEL 1 a crowd of note heads with no stems, LEVEL 2 one note walking on its stem
        private void PartsStatic()
        {
            float h = Height;
            Vector2 head = body[Head].At, chest = body[Chest].At, hip = body[Hip].At;
            if (Level == 2)
            {
                Vector2 stemTop = P(head, -0.08f, 0.02f);
                Vector2 stemFoot = P(hip, -0.06f, 0.08f);
                TLine(stemFoot, body[FootA].At, 0.03f * h, 0.01f * h);
                TLine(stemFoot, body[FootB].At, 0.03f * h, 0.01f * h);
                TLine(stemTop, stemFoot, 0.032f * h, 0.026f * h);                      // the stem is its body
                line[0] = P(head, -0.075f, 0.06f); line[1] = P(head, 0.09f, 0.17f); line[2] = P(head, 0.03f, 0.25f); line[3] = P(head, -0.07f, 0.14f);
                TPoly(4);                                                              // the flag is its cape
                TLine(P(head, -0.06f, 0.13f), body[HandA].At, 0.026f * h, 0.015f * h);
                TEllipse(Glitch(body[HandA].At, 5), 0.036f * h, 0.026f * h, NoteTilt);
                TEllipse(Glitch(head, 0), 0.095f * h, 0.068f * h, NoteTilt);
                return;
            }
            for (int k = 0; k < 7; k++)
            {
                float rx = (0.065f + 0.03f * Hash(k, 41)) * h;
                Vector2 at = chest + new Vector2((Hash(k, 45) - 0.5f) * 0.42f * Facing, (Hash(k, 47) - 0.5f) * 0.52f) * h;
                TEllipse(Glitch(at, k), rx, rx * 0.72f, NoteTilt);
            }
        }

        //THE DAMPER : the whole rest, a heavy block hanging from its line, and the felt pad it presses down
        private void PartsDamper()
        {
            float h = Height;
            Vector2 chest = body[Chest].At, hip = body[Hip].At;
            if (Level == 2)
            {
                TLine(P(hip, -0.08f, 0f), body[FootA].At, 0.09f * h, 0.07f * h);
                TLine(P(hip, 0.07f, 0f), body[FootB].At, 0.09f * h, 0.07f * h);
                TLine(P(chest, -0.18f, 0.03f), body[HandB].At, 0.07f * h, 0.05f * h);
                TBox(P(chest, 0.05f, -0.08f), 0.12f * h, 0.08f * h, 0f);
            }
            TBox(chest, 0.46f * h, 0.05f * h, 0f);                                     // the line it hangs from
            TBox(P(chest, 0f, 0.16f), 0.32f * h, 0.25f * h, 0f);                       // the whole rest
            TLine(P(chest, 0.12f, 0.07f), body[HandA].At, 0.05f * h, 0.04f * h);
            TEllipse(body[HandA].At, 0.075f * h, 0.15f * h, 0f);                        // the felt pad
        }

        //THE MUTE CHOIR : a pyramid of hollow note heads on a robe that ends in spikes
        private void PartsChoir()
        {
            float h = Height;
            Vector2 chest = body[Chest].At, hip = body[Hip].At;
            if (Level == 2)
            {
                TLine(P(chest, 0.06f, 0.04f), body[FootA].At, 0.05f * h, 0.022f * h);
                TLine(P(chest, -0.06f, 0.04f), body[FootB].At, 0.05f * h, 0.022f * h);
                TClaws(body[FootA].At, 0.06f * h);
                TClaws(body[FootB].At, 0.06f * h);
            }
            int k = 0;
            line[k++] = P(chest, -0.08f, 0.02f);
            line[k++] = P(chest, 0.08f, 0.02f);
            for (int i = 0; i <= 8; i++)                                               // the hem : spikes
            {
                float t = i / 8f;
                float x = MathHelper.Lerp(0.22f, -0.24f, t);
                line[k++] = P(hip, x, i % 2 == 0 ? 0.3f : 0.2f);
            }
            TPoly(k);
            for (int i = 0; i < 6; i++) TEllipse(ChoirHead(i), 0.068f * h, 0.055f * h, NoteTilt);
        }

        //REQUIEM : a tall robe with points at the shoulders, a hood, a quill for a blade, staff lines that stop below it
        private void PartsRequiem()
        {
            float h = Height;
            Vector2 head = body[Head].At, chest = body[Chest].At, hip = body[Hip].At;
            if (Level == 2)
            {
                TLine(P(chest, 0.1f, 0.02f), body[HandA].At, 0.045f * h, 0.025f * h);
                TLine(P(chest, -0.1f, 0.02f), body[HandB].At, 0.045f * h, 0.025f * h);
                TClaws(body[HandB].At, 0.08f * h);
            }
            TKite(body[HandA].At, P(body[HandA].At, 0.05f, -0.32f), 0.06f * h, 0.15f);    // the quill
            line[0] = P(chest, -0.12f, 0f); line[1] = P(chest, 0.11f, 0f); line[2] = P(hip, 0.17f, 0.28f); line[3] = P(hip, -0.19f, 0.28f);
            TPoly(4);
            TKite(P(chest, -0.11f, 0f), P(chest, -0.2f, -0.09f), 0.05f * h, 0.3f);
            TKite(P(chest, 0.1f, 0f), P(chest, 0.18f, -0.08f), 0.05f * h, 0.3f);
            line[0] = P(head, -0.07f, -0.08f); line[1] = P(head, 0f, -0.13f); line[2] = P(head, 0.07f, -0.08f); line[3] = P(head, 0.06f, 0.07f); line[4] = P(head, -0.06f, 0.07f);
            TPoly(5);
            for (int k = 0; k < 5; k++)                                                // the piece never finished : lines that stop
            {
                float y = 0.31f + k * 0.045f;
                float gap = (float)Math.Sin(clock * 0.7f + k * 1.3f) * 0.04f;
                float wide = 0.3f - k * 0.04f;
                TBox(P(hip, -wide * 0.25f + gap, y), wide * 0.42f * h, 0.02f * h, 0f);
                TBox(P(hip, wide * 0.32f - gap, y), wide * 0.26f * h, 0.02f * h, 0f);
            }
        }

        //THE NAMELESS MASTER : a tailcoat, a blank diamond mask, and its shadow a moment behind (drawn first)
        private void PartsNameless()
        {
            float h = Height;
            Vector2 head = body[Head].At, chest = body[Chest].At, hip = body[Hip].At;
            for (int twin = 0; twin < 2; twin++)
            {
                Vector2 m = twin == 0 ? body[Extra].At - head : Vector2.Zero;
                if (Level == 2)
                {
                    TLine(P(hip, -0.04f, 0f) + m, body[FootA].At + m, 0.04f * h, 0.012f * h);
                    TLine(P(hip, 0.04f, 0f) + m, body[FootB].At + m, 0.04f * h, 0.012f * h);
                    TLine(P(chest, 0.1f, 0.01f) + m, body[HandA].At + m, 0.035f * h, 0.018f * h);
                    TLine(P(chest, -0.1f, 0.01f) + m, body[HandB].At + m, 0.035f * h, 0.018f * h);
                    TLine(body[HandA].At + m, P(body[HandA].At, 0.14f, -0.2f) + m, 0.014f * h, 0.006f * h);   // its pen, its baton
                }
                TKite(P(hip, -0.03f, 0f) + m, P(hip, -0.16f, 0.36f) + m, 0.08f * h, 0.15f);
                TKite(P(hip, 0.03f, 0f) + m, P(hip, -0.04f, 0.34f) + m, 0.06f * h, 0.15f);
                line[0] = P(chest, -0.12f, 0f) + m; line[1] = P(chest, 0.11f, 0f) + m; line[2] = P(hip, 0.05f, 0f) + m; line[3] = P(hip, -0.06f, 0f) + m;
                TPoly(4);
                line[0] = P(head, 0f, -0.12f) + m; line[1] = P(head, 0.08f, 0f) + m; line[2] = P(head, 0f, 0.14f) + m; line[3] = P(head, -0.08f, 0f) + m;
                TPoly(4);
            }
        }

        //THE DEVIL'S STRING : a thin twisted body, a violin's scroll for a head with two horns, the bow as a scythe
        private void PartsDevil()
        {
            float h = Height;
            Vector2 head = body[Head].At, chest = body[Chest].At, hip = body[Hip].At;
            if (Level == 2)
            {
                TLine(hip, body[FootA].At, 0.04f * h, 0.01f * h);
                TLine(hip, body[FootB].At, 0.04f * h, 0.01f * h);
                TLine(P(chest, -0.06f, 0.01f), body[HandB].At, 0.03f * h, 0.015f * h);
                TClaws(body[HandB].At, 0.07f * h);
                TLine(P(chest, 0.06f, 0.01f), body[HandA].At, 0.03f * h, 0.015f * h);
            }
            line[0] = P(chest, -0.07f, 0f); line[1] = P(chest, 0.08f, 0.02f); line[2] = P(hip, 0.02f, 0.05f); line[3] = P(hip, -0.05f, 0.04f);
            TPoly(4);
            TKite(P(hip, -0.01f, 0.02f), P(hip, -0.06f, 0.38f), 0.1f * h, 0.12f);
            TCircle(head, 0.065f * h);
            TCrescent(P(head, -0.04f, -0.03f), 0.09f * h, -2.6f, -1.5f, 0.03f * h);   // horns
            TCrescent(P(head, 0.04f, -0.03f), 0.09f * h, -1.6f, -0.5f, 0.03f * h);
            Vector2 top = P(body[HandA].At, 0.02f, -0.38f);
            TLine(P(body[HandA].At, 0f, 0.05f), top, 0.018f * h, 0.012f * h);          // the bow's stick
            TCrescent(P(top, -0.06f, 0f), 0.16f * h, Facing > 0f ? -0.2f : MathHelper.Pi - 1.2f, Facing > 0f ? 1.2f : MathHelper.Pi + 0.2f, 0.045f * h);
        }

        //THE DEAF CROWN : a broad robe with spikes at the shoulders, a crown over a face that is not there, two great hands
        private void PartsCrown()
        {
            float h = Height;
            Vector2 head = body[Head].At, chest = body[Chest].At, hip = body[Hip].At;
            if (Level == 2)
            {
                TLine(P(chest, 0.2f, 0f), body[HandA].At, 0.08f * h, 0.045f * h);
                TLine(P(chest, -0.2f, 0f), body[HandB].At, 0.08f * h, 0.045f * h);
            }
            line[0] = P(chest, -0.24f, -0.02f); line[1] = P(chest, 0.24f, -0.02f); line[2] = P(hip, 0.26f, 0.36f); line[3] = P(hip, -0.28f, 0.36f);
            TPoly(4);
            TKite(P(chest, -0.2f, -0.02f), P(chest, -0.3f, -0.14f), 0.07f * h, 0.3f);
            TKite(P(chest, 0.2f, -0.02f), P(chest, 0.3f, -0.14f), 0.07f * h, 0.3f);
            TCircle(P(head, 0f, 0.09f), 0.085f * h);
            for (int k = 0; k < 5; k++)                                                // the crown's points
            {
                float x = -0.1f + k * 0.05f;
                float tall = (k == 2 ? 0.13f : (k == 1 || k == 3 ? 0.1f : 0.075f)) * h;
                TKite(P(head, x, 0.03f), P(head, x, 0.03f) + new Vector2(0f, -tall), 0.045f * h, 0.15f);
            }
            TBox(P(head, 0f, 0.035f), 0.26f * h, 0.035f * h, 0f);
            THand(body[HandA].At, 0.075f * h);
            THand(body[HandB].At, 0.075f * h);
        }

        //THE STAFF : the staff it beat time with, slamming the floor, its body a banner hanging from the top
        private void PartsStaff()
        {
            float h = Height;
            Vector2 head = body[Head].At, chest = body[Chest].At, hip = body[Hip].At;
            float beat = StaffBeat();
            Vector2 foot = Local(0.13f, 0f) + new Vector2(0f, -beat * 0.12f * h);
            TLine(foot, foot + new Vector2(0f, -0.96f * h), 0.03f * h, 0.025f * h);
            TBox(foot + new Vector2(0f, -0.98f * h), 0.06f * h, 0.06f * h, MathHelper.PiOver4);
            TCircle(foot + new Vector2(0f, -0.9f * h), 0.03f * h);
            if (Level == 2)
            {
                TLine(P(chest, 0.08f, 0f), new Vector2(foot.X, body[HandA].At.Y), 0.05f * h, 0.035f * h);
                TLine(P(chest, -0.1f, 0f), body[HandB].At, 0.05f * h, 0.035f * h);
                TLine(P(hip, 0.04f, 0f), body[FootB].At, 0.06f * h, 0.03f * h);
                TLine(P(hip, -0.05f, 0f), P(hip, -0.12f, 0.3f), 0.05f * h, 0.03f * h);   // the leg it hurt, hanging
            }
            line[0] = P(chest, -0.17f, 0f); line[1] = P(chest, 0.1f, 0f); line[2] = P(hip, 0.09f, 0.22f); line[3] = P(hip, -0.02f, 0.14f); line[4] = P(hip, -0.15f, 0.24f);
            TPoly(5);                                                                  // the banner, a swallowtail
            TCircle(head, 0.06f * h);
            TBox(P(head, 0f, -0.05f), 0.15f * h, 0.03f * h, 0f);
        }

        //THE BLIND CANTOR : a robe, organ pipes for wings, two hands feeling the way
        private void PartsCantor()
        {
            float h = Height;
            Vector2 head = body[Head].At, chest = body[Chest].At, hip = body[Hip].At;
            for (int k = 0; k < 6; k++)                                                // organ pipes for wings
            {
                float tall = (0.3f + 0.22f * (float)Math.Sin(k * 0.9f + 0.4f)) * h;
                float x = 0.16f + k * 0.055f;
                TBox(P(chest, x, 0f) + new Vector2(0f, -tall * 0.35f), 0.04f * h, tall, 0f);
                TBox(P(chest, -x, 0f) + new Vector2(0f, -tall * 0.35f), 0.04f * h, tall, 0f);
            }
            if (Level == 2)
            {
                TLine(P(chest, 0.1f, 0f), body[HandA].At, 0.05f * h, 0.03f * h);
                TLine(P(chest, 0.06f, 0.04f), body[HandB].At, 0.045f * h, 0.03f * h);
            }
            line[0] = P(chest, -0.14f, -0.04f); line[1] = P(chest, 0.14f, -0.04f); line[2] = P(hip, 0.17f, 0.3f); line[3] = P(hip, -0.19f, 0.3f);
            TPoly(4);
            TCircle(head, 0.085f * h);
            THand(body[HandA].At, 0.05f * h);
            THand(body[HandB].At, 0.05f * h);
        }

        //Choir Head : where THE MUTE CHOIR's heads sit, a pyramid of one, two and three over its body, each bobbing on its own
        private Vector2 ChoirHead(int k)
        {
            int row = k == 0 ? 0 : (k < 3 ? 1 : 2);
            int col = k == 0 ? 0 : (k < 3 ? k - 1 : k - 3);
            float x = (col - row * 0.5f) * 0.14f;
            float y = -0.28f + row * 0.12f;
            float bob = (float)Math.Sin(clock * 2f + k * 1.3f) * 0.006f;
            return body[Chest].At + new Vector2(x * Facing, y + bob) * Height;
        }

        //Glitch : STATIC's shapes jump to a new place every so often, each on its own beat
        private Vector2 Glitch(Vector2 at, int k)
        {
            if (design != Static) return at;
            int step = (int)((clock + k * 0.37f) * 7f);
            if (Hash(step, k) > 0.7f) return at + new Vector2((Hash(step, k + 19) - 0.5f) * 0.12f * Height, 0f);
            return at;
        }

        //======================================================================================
        //The designs : the white cut through them (eyes and the signs of silence), never the band's colours
        //======================================================================================

        private void DrawTacetLights(float alpha)
        {
            float h = Height;
            Color white = Color.White * alpha;
            Color grey = Palette.PaperDim * alpha;
            Vector2 head = body[Head].At + Shift, chest = body[Chest].At + Shift, hip = body[Hip].At + Shift;
            float slant = -0.25f * Facing;
            switch (design)
            {
                case Static:
                    {
                        for (int k = 0; k < 5; k++)                                    // broken staff lines behind the false notes
                        {
                            int step = (int)(clock * 5f) + k * 7;
                            float x = (Hash(step, k) - 0.5f) * 0.2f;
                            Vector2 at = P(chest, x, -0.12f + k * 0.06f);
                            Shapes.Box(at, (0.08f + 0.12f * Hash(step, k + 3)) * h, BattleRules.FigurePixel, 0f, grey);
                        }
                        if (Hash((int)(clock * 9f), 3) > 0.2f)                         // its one eye, flickering
                        {
                            Vector2 eye = Level == 2 ? Glitch(body[Head].At, 0) + Shift : chest;
                            Shapes.Box(eye, 0.05f * h, 0.01f * h, NoteTilt, white);
                        }
                    }
                    break;
                case Damper:
                    Shapes.Box(P(chest, 0.05f, 0.11f), 0.13f * h, 0.012f * h, 0f, white);      // the slit
                    for (int k = 0; k < 4; k++)                                                // the strings under the pad
                        Shapes.Box(P(body[HandA].At + Shift, -0.02f, 0.17f + k * 0.022f), 0.24f * h, BattleRules.FigurePixel, 0f, grey);
                    break;
                case Choir:
                    for (int k = 0; k < 6; k++)
                    {
                        float open = 0.35f + 0.65f * (0.5f + 0.5f * (float)Math.Sin(clock * 2.4f + k * 1.9f));
                        Vector2 at = ChoirHead(k) + Shift;
                        Shapes.Ellipse(at, 0.04f * h, 0.03f * h, NoteTilt, white, white);              // hollow : a whole note
                        Shapes.Ellipse(at, 0.022f * h, 0.022f * h * open, 0f, Color.Black * alpha, Color.Black * alpha);   // open on nothing
                    }
                    break;
                case Requiem:
                    Shapes.Arc(P(head, 0f, -0.02f), 0.2f * h, -1.2f, 3.6f, 0.014f * h, 0.006f * h, white);   // the broken halo
                    Shapes.Box(head, 0.06f * h, 0.01f * h, 0f, white);                                      // the face, a slit
                    for (int k = 0; k < 5; k++)                                                             // a staff that stops
                    {
                        float wide = (0.13f - k * 0.02f) * h;
                        Vector2 start = P(chest, -0.07f, 0.09f + k * 0.026f);
                        Shapes.Box(start + new Vector2(wide * 0.5f * Facing, 0f), wide, BattleRules.FigurePixel, 0f, white);
                    }
                    for (int k = 0; k < 4; k++)                                                             // LACRIMOSA : the tears
                    {
                        float t = (clock * 0.45f + k * 0.25f) % 1f;
                        Vector2 at = head + new Vector2((Hash(k, 61) - 0.5f) * 0.05f * h, (0.03f + t * 0.5f) * h);
                        Shapes.Circle(at, 0.009f * h, white * (1f - t));
                        Shapes.Kite(at, at - new Vector2(0f, 0.03f * h), 0.018f * h, 0.1f, white * (1f - t));
                    }
                    break;
                case Nameless:
                    {
                        Vector2 frame = P(chest, 0f, 0.11f);                                       // the frame where its name was, empty
                        float w = 0.13f * h, hh = 0.06f * h, t = BattleRules.FigurePixel;
                        Shapes.Box(frame - new Vector2(0f, hh * 0.5f), w, t, 0f, white);
                        Shapes.Box(frame + new Vector2(0f, hh * 0.5f), w, t, 0f, white);
                        Shapes.Box(frame - new Vector2(w * 0.5f, 0f), t, hh, 0f, white);
                        Shapes.Box(frame + new Vector2(w * 0.5f, 0f), t, hh, 0f, white);
                        Shapes.Box(head + new Vector2(0f, 0.01f * h), t, 0.18f * h, 0f, grey);     // a crack down the blank mask
                    }
                    break;
                case Devil:
                    {
                        float top = -0.2f * h;                                                     // ON ONE STRING : it shivers
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
                        Shapes.Box(P(head, 0.03f, 0.02f), 0.04f * h, 0.01f * h, slant, white);                       // an eye
                    }
                    break;
                case Crown:
                    {
                        Vector2 face = P(head, 0f, 0.09f) + new Vector2(0f, 0f);                   // it cannot hear : the face is empty
                        Shapes.Ring(face, 0.055f * h, 0.012f * h, white);
                        if (Hash((int)(clock * 11f), 2) > 0.45f)                                   // lightning between the hands
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
                    }
                    break;
                case Staff:
                    {
                        for (int k = 0; k < 5; k++)                                                // its banner is a staff : five lines
                        {
                            Vector2 a = P(chest, -0.15f, 0.05f + k * 0.035f);
                            Vector2 b = P(chest, 0.08f, 0.05f + k * 0.035f);
                            Shapes.Line(a, b, BattleRules.FigurePixel, white);
                        }
                        Shapes.Box(head, 0.05f * h, 0.01f * h, 0f, white);
                        float t = ((clock + seed) % 1.7f - 0.45f) / 0.8f;                         // TE DEUM : the beat on the floor
                        if (t > 0f && t < 1f)
                        {
                            Vector2 foot = Local(0.13f, 0f) + Shift;
                            Shapes.Arc(foot, 0.1f * h + t * 0.4f * h, MathHelper.Pi, MathHelper.TwoPi, 0.01f * h * (1f - t), 0.01f * h * (1f - t), white * (1f - t));
                        }
                    }
                    break;
                case Cantor:
                    Shapes.Box(P(head, 0f, -0.005f), 0.19f * h, 0.035f * h, 0f, white);           // the blindfold
                    for (int k = 0; k < 6; k++)                                                    // each pipe's mouth
                    {
                        float tall = (0.3f + 0.22f * (float)Math.Sin(k * 0.9f + 0.4f)) * h;
                        float x = 0.16f + k * 0.055f;
                        Shapes.Box(P(chest, x, 0f) + new Vector2(0f, tall * 0.05f), 0.022f * h, 0.012f * h, 0f, grey);
                        Shapes.Box(P(chest, -x, 0f) + new Vector2(0f, tall * 0.05f), 0.022f * h, 0.012f * h, 0f, grey);
                    }
                    for (int k = 0; k < 3; k++)                                                    // voices, one after another
                    {
                        float t = (clock * 0.5f + k / 3f) % 1f;
                        Shapes.Ring(head, (0.1f + t * 0.4f) * h, 0.006f * h * (1f - t), white * (0.8f * (1f - t)));
                    }
                    break;
                default:                                                                           // hush
                    {
                        Shapes.Box(P(head, -0.005f, -0.012f), 0.032f * h, 0.009f * h, slant, white);   // two slits for eyes
                        Shapes.Box(P(head, 0.04f, -0.024f), 0.028f * h, 0.009f * h, slant, white);
                        Vector2 finger = Level == 2 ? body[HandA].At + Shift + new Vector2(0f, -0.03f * h) : P(head, 0.025f, 0.035f);
                        Shapes.Box(finger, 0.013f * h, 0.075f * h, 0f, white);                        // a finger on its lips
                        Shapes.Circle(finger + new Vector2(0f, -0.037f * h), 0.009f * h, white);
                        for (int k = 0; k < 5; k++)                                                    // the echo going away
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
