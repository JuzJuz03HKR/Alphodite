using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //Figure Tacet (Beta1 1 Nov, MATH ART) : TACET's fighters, the four looks every enemy wears (Data/Enemy.cs Looks,
    //docs/ASSET_LIST.md 1.2) and the six bosses.
    //TACET LOOK (round 6 part 2, 2 Nov, docs/BETA1_DIRECTION.md 32.5) : the four looks are the things that make music go quiet,
    //drawn from the sketch docs/art_refs/concept_v6.html with its numbers as they are (see LOOK FRAME) :
    //   hush             THE MUTE : the cone mute pushed into a horn, and a "shh". A tall leaning black cone, a slit of light
    //                    with a black bar across it like a finger on the lips, light leaking from under its hem
    //   static           THE NOISE : a radio between stations. Black bits of shell in a twitching cloud, thin white lines
    //                    between them, a blinking core with a ghost beside it. It cracks by losing its bits
    //   the_damper       THE WEIGHT : the felt that stops a piano's strings. A heavy block hung from a beam presses four
    //                    strings until they sag, its light squeezed out at the sides
    //   the_mute_choir   THE EMPTY CHOIR : six black heads in rows, mouths open on nothing, a robe of long blades down to the
    //                    floor. On its blow every mouth lights
    //   Every one : a smooth black shell with a pale rim and a soft white BACKLIGHT behind it (Screens/BattleScreen.Stage.cs),
    //   so it reads as a shape anywhere on the dark stage, and a white CORE, the brightest thing on the stage (the user 13.87 :
    //   smooth, brighter than the stage, breakable, light first). CRACKS at 75 50 25 of its life, CHIPS off its shell on every
    //   hit, and when it is beaten the shell breaks (SHARDS) and the light it held rises as a ring (RELEASE).
    //The bosses, still the round 5 shapes (round 6 part 3 draws them again) : black with a white edge, now smooth, built from
    //the signs of silence and their own stories :
    //   requiem          the piece never finished : a broken halo, staff lines that stop, a quill for a blade, tears (LACRIMOSA)
    //   the_nameless_master  a blank mask, an empty frame where the name was, a shadow that copies it a moment late (ITALIEN)
    //   the_devil_s_string   one string through its heart, a violin's scroll for a head with horns, the bow as a scythe
    //   the_deaf_crown   a crown over an empty face (it cannot hear), two great hands that give orders, lightning (THE NINTH)
    //   the_staff        the conductor's staff it beat time with on its own foot : it slams the floor, its body is a banner of
    //                    the five staff lines (TE DEUM)
    //   the_blind_cantor a blindfold, organ pipes for wings, voices going out one after another in rings (IN THE DARK)
    //LEVEL 2 gives each one thin limbs under the same shapes.
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

        //Life : its share of life left, 0 to 1 (the stage sets it every frame, CRACKS)
        public float Life = 1f;
        private int crackSeen;            // CRACKS : how many cracks it shows (one more at 75 50 25 of its life)
        private float crackClock;         //   seconds since the newest one began to run
        private Vector2 releaseAt;        // RELEASE : where its light was when it broke
        private const float SignLine = 2f;   // the bosses' thin white lines, page pixels

        //THE MONUMENT (round 6 part 3, docs/BETA1_DIRECTION.md 32.6) : a boss stands first as the white monument the world
        //remembers of that legend. At half life (THE TURN, Screens/BattleScreen.Boss.cs) the white shell breaks off in a
        //flash and the black form inside is left, the same shape but dark, its core brighter.
        private bool shellOn = true;
        private float breakFlash;            // 1 just broken, fades to 0
        private float shellDue;              // seconds below half life with no THE TURN waiting (a fight begun below half)
        public bool TurnPending;             // the stage : THE TURN is waiting to play, do not break quietly
        private static readonly Color MonumentWhite = new Color(230, 229, 234);
        private static readonly Color MonumentShade = new Color(164, 163, 172);
        private static readonly Color MonumentInk = new Color(10, 10, 12);

        //Shell On : a boss still in its white shell
        public bool ShellOn
        {
            get { return Of == Sort.Boss && shellOn; }
        }

        //Break Shell : THE TURN breaks the white shell (loud : white pieces fall and the black form flashes)
        public void BreakShell(bool loud)
        {
            if (!ShellOn) return;
            shellOn = false;
            if (!loud) return;
            breakFlash = 1f;
            int count = 18;
            for (int i = 0; i < count && chipCount < chips.Length; i++)
            {
                int k = chipSerial++;
                Vector2 at = body[i % 8].At + new Vector2(Hash(k, 95) - 0.5f, Hash(k, 97) - 0.5f) * (Height * 0.3f);
                ChipAt(at, 1);
                ChipBit c = chips[chipCount - 1];
                c.White = true;
                c.Size *= 2.2f;
                c.Speed *= 1.6f;
                chips[chipCount - 1] = c;
            }
        }

        //Dim : a colour darker, still solid (a colour times a number would also make it see through)
        private static Color Dim(Color c, float k)
        {
            return new Color((byte)(c.R * k), (byte)(c.G * k), (byte)(c.B * k), c.A);
        }

        //Is Look : one of the four looks every enemy wears (not a boss)
        private bool IsLook
        {
            get { return design <= Choir; }
        }

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
                case Static:                                                             // THE NOISE : a nervous twitch
                    {
                        int step = (int)((clock + seed) * 6f);
                        LookRest(lunge + (Hash(step, 3) - 0.5f) * 0.014f, bob + (Hash(step, 5) - 0.5f) * 0.012f, 0.4f);
                    }
                    break;
                case Damper:                                                             // THE WEIGHT : slow and heavy, its blow presses down
                    LookRest(lunge * 0.4f, bob * 0.5f + Math.Max(0f, s) * 0.05f, 0.6f);
                    break;
                case Choir:                                                              // THE EMPTY CHOIR : LEVEL 2's hands up, leading the silence
                    LookRest(lunge * 0.6f, bob, 0.4f);
                    Set(HandA, 0.2f + lunge, -0.86f + bob * 1.2f);
                    Set(HandB, -0.2f, -0.85f + bob * 0.8f);
                    break;
                case Requiem:                                                            // REQUIEM : tall and still, the quill swings on its blow
                    LookRest(lunge * 0.6f, bob, 0.4f);
                    Set(HandA, -0.124f + Math.Max(0f, s) * 0.45f, -0.44f + bob - raise * 0.1f - Math.Max(0f, s) * 0.08f);
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
                case Crown:                                                              // THE DEAF CROWN : the bust stands still, the hand gives orders
                    LookRest(lunge * 0.3f, bob * 0.5f, 0.6f);
                    Set(HandA, -0.36f + Math.Max(0f, s) * 0.55f, -0.69f + bob * 1.5f - raise * 0.1f + Math.Max(0f, s) * 0.12f);
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
                default:                                                                 // THE MUTE : it leans into its blow
                    LookRest(lunge, bob, 0.2f);
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

        //Look Rest : the four looks stand the same way (the sketch rides on the Chest spring, see LOOK FRAME). x is the push
        //toward the band, y the bob, follow how much of the push the Hip takes (less = it leans more into the blow)
        private void LookRest(float x, float y, float follow)
        {
            Set(Head, x * 1.2f, -0.8f + y);
            Set(Chest, x, -0.5f + y);
            Set(Hip, x * follow, -0.2f + y * 0.5f);
            Set(HandA, 0.22f + x * 1.6f, -0.5f + y);
            Set(HandB, -0.2f, -0.45f + y);
            Set(FootA, -0.08f, 0f);
            Set(FootB, 0.08f, 0f);
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
            if (IsLook)
            {
                SetLook();
                releaseAt = LookCore();
            }
            else releaseAt = CorePoint();
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
            UpdateChips(dt);
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

        //Draw Tacet : a look draws itself shape by shape (DrawLook). A boss in three passes : the edge (every part a little
        //bigger, in white), the body (every part in black over it, so only the outline of the whole stays white), then the white
        //cut through it (eyes, signs). A beaten one is only its shards and the light it lets go.
        private void DrawTacet(float alpha, float flash, float light, float mark)
        {
            Color fill = Color.Lerp(BodyInk, Color.White, flash) * alpha;
            Color rim = Color.Lerp(Palette.Paper * (BattleRules.FigureTacetRim * light), Color.White, mark) * alpha;
            Color edge = Color.Lerp(Palette.Paper * light, Color.White, mark) * alpha;
            if (shardCount > 0)
            {
                DrawRelease(alpha);
                DrawShards(BattleRules.FigureTacetEdge * 2f, IsLook ? rim : edge, fill);
                return;
            }
            if (down) return;
            if (IsLook)
            {
                lookEdge = BattleRules.FigureTacetEdge * (1f + 1.5f * mark);
                lookRim = rim;
                lookFill = fill;
                DrawLook(alpha, light);
                DrawChips(alpha);
                return;
            }
            if (design == Crown || design == Requiem)                                // drawn from the round 6 sketch
            {
                SetBossInks(alpha, light, flash, mark);
                if (design == Crown) DrawCrown(alpha);
                else DrawRequiem(alpha);
                DrawChips(alpha);
                return;
            }
            if (ShellOn)                                                             // THE MONUMENT : white, a grey rim, signs cut in black
            {
                edge = Color.Lerp(Dim(MonumentShade, 0.5f + 0.5f * light), Color.White, mark) * alpha;
                fill = Dim(MonumentWhite, 0.6f + 0.4f * light) * alpha;
            }
            else if (breakFlash > 0f) fill = Color.Lerp(fill, Color.White, breakFlash);    // just broken : the black form flashes
            tGrow = SignLine * (1f + mark);
            tColor = edge;
            DrawTacetParts();
            tGrow = 0f;
            tColor = fill;
            DrawTacetParts();
            DrawTacetLights(alpha * (0.6f + 0.4f * light));
            lookEdge = BattleRules.FigureTacetEdge;
            lookRim = rim;
            lookFill = fill;
            DrawChips(alpha);
        }

        private void DrawShards(float grow, Color edge, Color fill)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < shardCount; i++)
                {
                    Shard s = shards[i];
                    float size = s.Size + (pass == 0 ? grow : 0f);
                    Color c = pass == 0 ? edge : fill;
                    Vector2 at = s.At + Shift;
                    Vector2 d = new Vector2((float)Math.Cos(s.Angle), (float)Math.Sin(s.Angle));
                    if (s.Shape == 1) Shapes.Box(at, size, size * 0.7f, s.Angle, c);
                    else if (s.Shape == 2) Shapes.Kite(at - d * size, at + d * size, size * 0.45f, 0.4f, c);
                    else Shapes.Polygon(at, size * 0.7f, 4, s.Angle, c);
                }
            }
        }

        //Draw Release (RELEASE) : the light the shell held, freed, rises as a ring and fades, a flash where it was
        private void DrawRelease(float alpha)
        {
            float t = moveTime / BattleRules.FigureReleaseTime;
            if (t >= 1f) return;
            float e = Ease(t);
            float fade = (1f - t) * (1f - t);
            Vector2 at = releaseAt + Shift - new Vector2(0f, 0.35f * e * Height);
            Shapes.Ring(at, (0.03f + 0.32f * e) * Height, (0.004f + 0.018f * (1f - t)) * Height, Color.White * (alpha * fade));
            if (t < 0.3f) Shapes.Polygon(releaseAt + Shift, CoreSize * (1f + 3f * t), 4, 0f, Color.White * (alpha * (1f - t / 0.3f)));
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
                case Nameless: PartsNameless(); break;
                case Devil: PartsDevil(); break;
                case Staff: PartsStaff(); break;
                case Cantor: PartsCantor(); break;
                default: break;                                                          // the looks draw themselves (DrawLook)
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

        //Core Point : TACET's heart, a small white diamond of light (the Worldless way : every creature has one small glowing
        //core, 13.86). Where it sits says something about the enemy : on HUSH's lips, in THE DAMPER's block, on THE DEVIL'S
        //STRING's string, in the frame where THE NAMELESS MASTER's name was, at the head of THE STAFF.
        private Vector2 CorePoint()
        {
            Vector2 head = body[Head].At, chest = body[Chest].At;
            switch (design)
            {
                case Requiem:
                    SetLook();
                    return S(32, 160) - Shift;                                           // where the unfinished lines stop
                case Nameless: return P(chest, 0f, 0.11f);
                case Devil: return chest;
                case Crown:
                    SetLook();
                    return S(176, 109) - Shift;                                          // the tip of the stick in its teeth
                case Staff: return Local(0.13f, 0f) + new Vector2(0f, (-0.98f - StaffBeat() * 0.12f) * Height);
                case Cantor: return P(chest, 0f, 0.08f);
                default: return chest;                                                   // the looks have their own (LookCore)
            }
        }

        //Core Size : the bosses' hearts are bigger
        private float CoreSize
        {
            get { return (Of == Sort.Boss ? (shellOn ? 0.024f : 0.032f) : 0.034f) * Height; }   // the black form's core is brighter
        }

        //======================================================================================
        //The designs : the white cut through them (eyes and the signs of silence), never the band's colours
        //======================================================================================

        private void DrawTacetLights(float alpha)
        {
            DrawTacetSigns(alpha);
            Vector2 core = CorePoint() + Shift;                                      // the heart, over everything
            Shapes.Polygon(core, CoreSize, 4, 0f, Color.White * alpha);
            Shapes.Polygon(core, CoreSize * 0.45f, 4, 0f, new Color(40, 40, 46) * alpha);
            Shapes.Polygon(core, CoreSize * 0.22f, 4, 0f, Color.White * alpha);
        }

        private void DrawTacetSigns(float alpha)
        {
            float h = Height;
            Color white = (ShellOn ? MonumentInk : Color.White) * alpha;          // THE MONUMENT : the signs cut in black
            Color grey = (ShellOn ? MonumentShade : Palette.PaperDim) * alpha;
            Vector2 head = body[Head].At + Shift, chest = body[Chest].At + Shift, hip = body[Hip].At + Shift;
            float slant = -0.25f * Facing;
            switch (design)
            {
                case Nameless:
                    {
                        Vector2 frame = P(chest, 0f, 0.11f);                                       // the frame where its name was, empty
                        float w = 0.13f * h, hh = 0.06f * h, t = SignLine;
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
                case Staff:
                    {
                        for (int k = 0; k < 5; k++)                                                // its banner is a staff : five lines
                        {
                            Vector2 a = P(chest, -0.15f, 0.05f + k * 0.035f);
                            Vector2 b = P(chest, 0.08f, 0.05f + k * 0.035f);
                            Shapes.Line(a, b, SignLine, white);
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
            }
        }

        //======================================================================================
        //TACET LOOK (round 6 part 2, 2 Nov, docs/BETA1_DIRECTION.md 32.5) : the four looks, from the sketch
        //======================================================================================

        //LOOK FRAME : the sketch is a card 240 x 300, the floor at 262, the tallest look 222 high, facing left (the band).
        //Its middle point (120, 151) rides on the Chest spring and the whole look leans the way the line from the Chest spring
        //to the Hip spring leans, so the bob, the lunge and every blow move every shape. Points on the floor stay put (SF).
        private const float LookMiddle = 120f;
        private const float LookFloor = 262f;
        private const float LookTall = 222f;
        private const float LookChest = 151f;

        private Vector2 lookAt;
        private float lookK;                  // page pixels for one unit of the sketch
        private float lookLean;
        private float lookCos = 1f;
        private float lookSin;
        private float lookStrike;             // 0 to 1 at the height of its own blow
        private float lookEdge;               // the pale rim's width, page pixels
        private Color lookRim;
        private Color lookFill;
        private static readonly Vector2[] grown = new Vector2[Shapes.MaxPoints];
        private static readonly Vector2[] bent = new Vector2[16];

        //Set Look : the frame for this frame (see LOOK FRAME)
        private void SetLook()
        {
            lookK = Height / LookTall;
            lookAt = body[Chest].At;
            Vector2 now = body[Hip].At - body[Chest].At;                         // at rest straight down
            lookLean = 0f;
            if (now.LengthSquared() > 0.01f) lookLean = MathHelper.WrapAngle((float)Math.Atan2(now.Y, now.X) - MathHelper.PiOver2);
            lookLean = MathHelper.Clamp(lookLean, -0.4f, 0.4f);
            lookCos = (float)Math.Cos(lookLean);
            lookSin = (float)Math.Sin(lookLean);
            lookStrike = move == "attack" ? Math.Max(0f, Strike()) : 0f;
        }

        //S : a point of the sketch on the body (it moves with it)
        private Vector2 S(float x, float y)
        {
            float dx = (x - LookMiddle) * lookK * -Facing;
            float dy = (y - LookChest) * lookK;
            return lookAt + new Vector2(dx * lookCos - dy * lookSin, dx * lookSin + dy * lookCos) + Shift;
        }

        //SF : a point of the sketch that stays put on the stage (the floor, THE WEIGHT's beam and the ends of its strings)
        private Vector2 SF(float x, float y)
        {
            return Feet + new Vector2((x - LookMiddle) * lookK * -Facing, (y - LookFloor) * lookK) + Shift;
        }

        //Shell : the points in line drawn as a piece of TACET's black shell, its pale rim first (the points pushed out from
        //their middle by the rim's width), then the black over it. fade lets a falling bit go.
        private void Shell(int n, float fade)
        {
            Vector2 middle = Vector2.Zero;
            for (int i = 0; i < n; i++) middle += line[i];
            middle /= n;
            for (int i = 0; i < n; i++)
            {
                Vector2 out1 = line[i] - middle;
                if (out1.LengthSquared() > 0.0001f) out1.Normalize();
                grown[i] = line[i] + out1 * lookEdge;
            }
            Shapes.Fill(grown, n, lookRim * fade);
            Shapes.Fill(line, n, lookFill * fade);
        }

        private void ShellQuad(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            line[0] = a;
            line[1] = b;
            line[2] = c;
            line[3] = d;
            Shell(4, 1f);
        }

        private void ShellOval(Vector2 at, float rx, float ry)
        {
            Shapes.Ellipse(at, rx * lookK + lookEdge, ry * lookK + lookEdge, lookLean, lookRim, lookRim);
            Shapes.Ellipse(at, rx * lookK, ry * lookK, lookLean, lookFill, lookFill);
        }

        //Crumb : a small three cornered bit of shell (floating round it, falling off it)
        private void Crumb(Vector2 at, float size, float turn, float fade)
        {
            for (int i = 0; i < 3; i++)
            {
                float a = turn + i * 2.1f + (i == 1 ? 0.45f : 0f);
                line[i] = at + new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * size;
            }
            Shell(3, fade);
        }

        //Limb : LEVEL 2's thin legs and arms, rim then black
        private void Limb(Vector2 a, Vector2 b, float w0, float w1)
        {
            Shapes.Taper(a, b, w0 * Height + lookEdge * 2f, w1 * Height + lookEdge * 2f, lookRim);
            Shapes.Taper(a, b, w0 * Height, w1 * Height, lookFill);
        }

        private Vector2 PartAt(int part)
        {
            return body[part].At + Shift;
        }

        //Light Line : a thin white line through n points of line (a slit, a crack, a string)
        private void LightLine(int n, float width, Color c)
        {
            for (int i = 0; i < n; i++) lineW[i] = width;
            Shapes.Stroke(line, lineW, n, c, c, false);
        }

        //Diamond : the white core's shape, half as wide and half as tall as given in sketch units
        private void Diamond(Vector2 at, float w, float h, Color c)
        {
            line[0] = at + new Vector2(0f, -h * lookK);
            line[1] = at + new Vector2(w * lookK, 0f);
            line[2] = at + new Vector2(0f, h * lookK);
            line[3] = at + new Vector2(-w * lookK, 0f);
            Shapes.Fill(line, 4, c);
        }

        //Oval Line : a thin ring drawn as a line (a halo, an open mouth)
        private void OvalLine(Vector2 at, float rx, float ry, float width, Color c)
        {
            const int count = 18;
            for (int i = 0; i < count; i++)
            {
                float t = i * MathHelper.TwoPi / count;
                float x = (float)Math.Cos(t) * rx * lookK;
                float y = (float)Math.Sin(t) * ry * lookK;
                line[i] = at + new Vector2(x * lookCos - y * lookSin, x * lookSin + y * lookCos);
                lineW[i] = width * lookK;
            }
            Shapes.Stroke(line, lineW, count, c, c, true);
        }

        //Look Core : where its white core is (the brightest thing on the stage)
        private Vector2 LookCore()
        {
            switch (design)
            {
                case Static: return S(120f + NoiseShiver(9).X, 127f);
                case Damper: return S(120, 150);
                case Choir: return S(120, 70f + ChoirBob(0));
                default: return S(108, 150);
            }
        }

        private void DrawLook(float alpha, float light)
        {
            SetLook();
            float glow = alpha * (0.6f + 0.4f * light);
            switch (design)
            {
                case Static: LookNoise(alpha, glow); break;
                case Damper: LookWeight(alpha, glow); break;
                case Choir: LookChoir(alpha, glow); break;
                default: LookMute(alpha, glow); break;
            }
        }

        //THE MUTE (hush) : the cone it is, the slit of light with the core behind it, the black bar across the slit like a
        //finger on its lips (on its blow the finger lifts : the "shh" lets go), the light leaking from under its hem
        private void LookMute(float alpha, float glow)
        {
            Color white = Color.White * glow;
            line[0] = S(78, 232);                                                  // the light under its hem, spilling on the floor
            line[1] = S(164, 232);
            line[2] = SF(186, 262);
            line[3] = SF(56, 262);
            Shapes.Fill(line, 4, Color.White * (0.08f * glow));
            if (Level == 2)
            {
                Limb(S(108, 226), PartAt(FootA), 0.026f, 0.012f);
                Limb(S(134, 226), PartAt(FootB), 0.026f, 0.012f);
            }
            for (int b = 0; b < 3; b++) Crumb(bits[b].At + Shift, (5f + b * 1.5f) * lookK, clock * (0.5f + 0.2f * b) + b * 2f, 1f);
            int n = Cubic(S(128, 40), S(134, 70), S(152, 150), S(166, 230), 12, line, 0);
            n = CubicMore(S(166, 230), S(142, 238), S(100, 238), S(76, 230), 8, line, n);
            n = CubicMore(S(76, 230), S(92, 160), S(110, 90), S(128, 40), 12, line, n);
            Shell(n - 1, 1f);                                                      // the last point is the first again
            n = Cubic(S(108, 118), S(105, 140), S(105, 160), S(109, 182), 8, line, 0);
            LightLine(n, 2.4f * lookK, white);                                     // the slit
            Diamond(S(108, 150), 5f, 7f, Color.White * alpha);                     // the core
            Shapes.Box(S(108, 137.8f - lookStrike * 12f), 24f * lookK, 3.6f * lookK, lookLean, lookFill);   // the finger
            Shapes.Ellipse(S(121, 232), 46f * lookK, 3.6f * lookK, lookLean, white * 0.85f, white * 0.85f);   // the hem's light
            DrawCracks(MuteCracks, white);
        }

        private static readonly float[] MuteCracks = { 124, 60, 131, 74, 127, 80, 146, 136, 139, 150, 144, 158, 96, 196, 104, 206, 99, 216 };

        //THE NOISE (static) : eight bits of shell (their corners from the sketch), white lines of a dead radio between them,
        //each bit jumping on its own beat, the core blinking with a ghost beside it, crumbs falling off it all the time.
        //Its CRACKS are bits it loses, in this order.
        private static readonly float[] NoiseBits =
        {
            96, 96, 118, 90, 112, 112, 94, 110,
            124, 86, 148, 94, 140, 112, 122, 104,
            84, 118, 104, 116, 100, 138, 80, 134,
            130, 116, 154, 120, 150, 140, 126, 136,
            104, 140, 124, 142, 120, 164, 100, 160,
            134, 146, 152, 150, 146, 168, 130, 162,
            62, 94, 78, 96, 76, 106, 60, 104,
            160, 100, 174, 104, 170, 116, 158, 112,
        };
        private static readonly int[] NoiseLoses = { 6, 7, 5 };
        private static readonly float[] NoiseLines = { 70, 98, 101, 116, 168, 99, 88, 126, 114, 78, 160, 128, 96, 132, 139, 110, 156, 152, 92, 122, 166 };

        //Noise Shiver : how far bit k has jumped this moment, in sketch units (now and then a big jump sideways)
        private Vector2 NoiseShiver(int k)
        {
            if (design != Static) return Vector2.Zero;
            int step = (int)((clock + k * 0.37f) * 7f);
            float x = (Hash(step, k + 40) - 0.5f) * 1.6f;
            if (Hash(step, k) > 0.74f) x += (Hash(step, k + 19) - 0.5f) * 14f;
            return new Vector2(x, (Hash(step, k + 60) - 0.5f) * 1.2f);
        }

        private bool NoiseGone(int k)
        {
            for (int i = 0; i < crackSeen && i < NoiseLoses.Length; i++)
                if (NoiseLoses[i] == k) return true;
            return false;
        }

        private Vector2 NoiseMiddle(int k)
        {
            int o = k * 8;
            return new Vector2(NoiseBits[o] + NoiseBits[o + 2] + NoiseBits[o + 4] + NoiseBits[o + 6], NoiseBits[o + 1] + NoiseBits[o + 3] + NoiseBits[o + 5] + NoiseBits[o + 7]) * 0.25f;
        }

        private void LookNoise(float alpha, float glow)
        {
            Color white = Color.White * glow;
            if (Level == 2)
            {
                Limb(S(110, 164), PartAt(FootA), 0.02f, 0.01f);
                Limb(S(134, 166), PartAt(FootB), 0.02f, 0.01f);
            }
            for (int k = 0; k < 7; k++)                                              // the dead radio's lines, blinking
            {
                if (Hash((int)(clock * 12f) + k * 5, k) < 0.25f) continue;
                float jump = (Hash((int)(clock * 7f), k + 30) - 0.5f) * 6f;
                float y = NoiseLines[k * 3 + 2];
                Shapes.Line(S(NoiseLines[k * 3] + jump, y), S(NoiseLines[k * 3 + 1] + jump, y), 0.8f * lookK, white * 0.6f);
            }
            for (int k = 0; k < 8; k++)
            {
                if (NoiseGone(k)) continue;
                Vector2 spread = (NoiseMiddle(k) - new Vector2(120f, 127f)) * (lookStrike * 0.35f) + NoiseShiver(k);   // its blow : the cloud bursts out
                int o = k * 8;
                ShellQuad(S(NoiseBits[o] + spread.X, NoiseBits[o + 1] + spread.Y), S(NoiseBits[o + 2] + spread.X, NoiseBits[o + 3] + spread.Y),
                          S(NoiseBits[o + 4] + spread.X, NoiseBits[o + 5] + spread.Y), S(NoiseBits[o + 6] + spread.X, NoiseBits[o + 7] + spread.Y));
            }
            Vector2 core = LookCore();
            if (Hash((int)(clock * 9f), 3) > 0.15f) Diamond(core, 6f, 7f, Color.White * alpha);
            Diamond(core + S(131, 127) - S(120, 127), 6f, 7f, Color.White * (0.3f * alpha));   // the ghost of a bad signal
            for (int k = 0; k < 3; k++)                                              // crumbs falling off it, over and over
            {
                float t = (clock * 0.45f + k * 0.33f + seed) % 1f;
                float x = 104f + k * 16f + (Hash(k, 71) - 0.5f) * 10f;
                Vector2 at = Vector2.Lerp(S(x, 176), SF(x - 6f, 258), t * t);
                Crumb(at, (3f + k) * lookK, t * 5f + k, 1f - t);
            }
        }

        //THE WEIGHT (the_damper) : the beam and its two cords, the light squeezed out at the sides, the heavy block, the light
        //under it with the core, and the four strings it presses down until they sag (harder on its blow)
        private void LookWeight(float alpha, float glow)
        {
            Color white = Color.White * glow;
            Shapes.Line(SF(56, 40), SF(184, 40), 3f * lookK, new Color(44, 44, 51) * alpha);              // the beam stays put
            Shapes.Line(SF(100, 40), S(100, 88), 1.4f * lookK, new Color(74, 74, 83) * alpha);           //   the cords hold the block
            Shapes.Line(SF(140, 40), S(140, 88), 1.4f * lookK, new Color(74, 74, 83) * alpha);
            line[0] = S(84, 150);                                                  // the light squeezed out at both sides
            line[1] = SF(30, 142);
            line[2] = SF(30, 156);
            Shapes.Fill(line, 3, Color.White * (0.14f * glow));
            line[0] = S(156, 150);
            line[1] = SF(210, 156);
            line[2] = SF(210, 142);
            Shapes.Fill(line, 3, Color.White * (0.14f * glow));
            if (Level == 2)
            {
                Limb(S(96, 140), PartAt(FootA), 0.04f, 0.026f);
                Limb(S(144, 140), PartAt(FootB), 0.04f, 0.026f);
            }
            int n = RoundBox(120, 116, 76, 60, 10);                                // the block
            Shell(n, 1f);
            Shapes.Ellipse(S(120, 150), 34f * lookK, 3f * lookK, lookLean, white * 0.9f, white * 0.9f);
            Diamond(S(120, 150), 4f, 4f, Color.White * alpha);
            DrawCracks(WeightCracks, white);
            for (int k = 0; k < 4; k++)                                              // the strings
            {
                float y = 156f + k * 6f;
                Shapes.Bend(S(84, y), S(120, 178f + k * 2f + lookStrike * 10f), S(156, y), 9, bent);
                line[0] = SF(34, y);
                for (int i = 0; i < 9; i++) line[1 + i] = bent[i];
                line[10] = SF(206, y);
                LightLine(11, 1f * lookK, white * 0.8f);
            }
        }

        private static readonly float[] WeightCracks = { 96, 98, 104, 108, 100, 114, 140, 92, 134, 104, 141, 112, 118, 120, 124, 131, 119, 140 };

        //Round Box : a box with round corners round (cx, cy) of the sketch into line, returns how many points
        private int RoundBox(float cx, float cy, float w, float h, float r)
        {
            int n = 0;
            for (int c = 0; c < 4; c++)                                            // top left, top right, bottom right, bottom left
            {
                float mx = c == 1 || c == 2 ? cx + w * 0.5f - r : cx - w * 0.5f + r;
                float my = c < 2 ? cy - h * 0.5f + r : cy + h * 0.5f - r;
                float from = MathHelper.Pi + c * MathHelper.PiOver2;
                for (int i = 0; i < 5; i++)
                {
                    float a = from + i * MathHelper.PiOver2 / 4f;
                    line[n++] = S(mx + (float)Math.Cos(a) * r, my + (float)Math.Sin(a) * r);
                }
            }
            return n;
        }

        //THE EMPTY CHOIR (the_mute_choir) : the robe's blades from its body down to the floor, six heads in rows, each open
        //mouth a hollow ring (all of them light on its blow), the core in the mouth of the top one under a thin halo
        private static readonly float[] ChoirHeads = { 120, 66, 102, 96, 138, 96, 84, 126, 120, 126, 156, 126 };
        private static readonly float[] ChoirBlades = { 86, 96, 70, 54, 100, 110, 98, 84, 115, 125, 127, 113, 130, 140, 156, 142, 144, 154, 186, 170 };

        private float ChoirBob(int k)
        {
            return (float)Math.Sin(clock * 2f + k * 1.3f) * 1.4f;
        }

        private void LookChoir(float alpha, float glow)
        {
            Color white = Color.White * glow;
            if (Level == 2)
            {
                Limb(S(96, 140), PartAt(HandA), 0.018f, 0.008f);
                Limb(S(144, 140), PartAt(HandB), 0.018f, 0.008f);
            }
            for (int k = 0; k < 5; k++)                                              // the blades : on the body at the top, on the floor at the foot
            {
                int o = k * 4;
                ShellQuad(S(ChoirBlades[o], 142), S(ChoirBlades[o + 1], 142), SF(ChoirBlades[o + 2], 252), SF(ChoirBlades[o + 3], 252));
            }
            Crumb(bits[0].At + Shift, 5f * lookK, clock * 0.6f, 1f);
            for (int k = 0; k < 6; k++) ShellOval(S(ChoirHeads[k * 2], ChoirHeads[k * 2 + 1] + ChoirBob(k)), 13f, 16f);
            float sing = 0.45f + 0.55f * lookStrike;
            for (int k = 1; k < 6; k++) OvalLine(S(ChoirHeads[k * 2], ChoirHeads[k * 2 + 1] + 4f + ChoirBob(k)), 4f, 5f, 1.4f, white * sing);
            OvalLine(S(120, 44f + ChoirBob(0)), 14f, 4f, 0.9f, white * 0.6f);         // the halo
            OvalLine(LookCore(), 4f, 5f, 1.6f, white);
            Shapes.Circle(LookCore(), 1.8f * lookK + 0.6f, Color.White * alpha);
            DrawCracks(ChoirCracks, white);
        }

        private static readonly float[] ChoirCracks = { 76, 116, 82, 124, 78, 130, 146, 88, 140, 96, 144, 104, 124, 56, 118, 63, 122, 70 };

        //======================================================================================
        //CRACKS and CHIPS : the shell breaks a little at a time
        //======================================================================================

        //Draw Cracks : one more white crack at 75 50 25 of its life (three points each), the newest running across
        private void DrawCracks(float[] cracks, Color white)
        {
            for (int k = 0; k < crackSeen && k < 3; k++)
            {
                float run = k == crackSeen - 1 ? MathHelper.Clamp(crackClock / BattleRules.FigureCrackTime, 0f, 1f) : 1f;
                int o = k * 6;
                Vector2 a = S(cracks[o], cracks[o + 1]), b = S(cracks[o + 2], cracks[o + 3]), c = S(cracks[o + 4], cracks[o + 5]);
                line[0] = a;
                line[1] = Vector2.Lerp(a, b, Math.Min(1f, run * 2f));
                int n = 2;
                if (run > 0.5f) line[n++] = Vector2.Lerp(b, c, (run - 0.5f) * 2f);
                LightLine(n, 1.1f * lookK + 0.4f, white);
            }
        }

        //Crack Spot : where crack k is (THE NOISE : the bit it lost), for the chips and the glow
        private Vector2 CrackSpot(int k)
        {
            float[] cracks;
            switch (design)
            {
                case Static:
                    Vector2 m = NoiseMiddle(NoiseLoses[Math.Min(k, NoiseLoses.Length - 1)]);
                    return S(m.X, m.Y);
                case Damper: cracks = WeightCracks; break;
                case Choir: cracks = ChoirCracks; break;
                default: cracks = MuteCracks; break;
            }
            int o = Math.Min(k, 2) * 6;
            return S(cracks[o + 2], cracks[o + 3]);
        }

        //CHIPS : small bits of shell a hit knocks off, falling and bouncing, then gone
        private struct ChipBit
        {
            public Vector2 At;
            public Vector2 Speed;
            public float Turn;
            public float Spin;
            public float Size;
            public float Age;
            public bool White;               // a piece of a boss's white shell (THE MONUMENT)
        }
        private readonly ChipBit[] chips = new ChipBit[24];
        private int chipCount;
        private int chipSerial;

        //Chip : n chips from round its core (THE NOISE breaks most easily, twice as many)
        private void Chip(int n)
        {
            Vector2 at = IsLook ? LookCoreNow() : body[Chest].At;
            if (design == Static) n *= 2;
            for (int i = 0; i < n; i++)
            {
                int k = chipSerial++;
                ChipAt(at + new Vector2(Hash(k, 81) - 0.5f, Hash(k, 83) - 0.5f) * (Height * 0.22f), 1);
            }
        }

        private Vector2 LookCoreNow()
        {
            SetLook();
            return LookCore() - Shift;
        }

        private void ChipAt(Vector2 at, int n)
        {
            float scale = Height / 256f;
            for (int i = 0; i < n; i++)
            {
                if (chipCount >= chips.Length) return;
                int k = chipSerial++;
                ChipBit c;
                c.At = at;
                c.Speed = new Vector2((-Facing * (50f + 130f * Hash(k, 85))) * scale, -(70f + 150f * Hash(k, 87)) * scale);   // away from the blow, up
                c.Turn = Hash(k, 89) * MathHelper.TwoPi;
                c.Spin = (Hash(k, 91) - 0.5f) * 16f;
                c.Size = (0.012f + 0.014f * Hash(k, 93)) * Height;
                c.Age = 0f;
                c.White = false;
                chips[chipCount++] = c;
            }
        }

        //Update Chips : gravity, the floor, age. Also CRACKS : one more crack when its life passes 75 50 25, a few chips from it.
        private void UpdateChips(float dt)
        {
            if (Of == Sort.Musician) return;
            int stage = Life < 0.25f ? 3 : (Life < 0.5f ? 2 : (Life < 0.75f ? 1 : 0));
            if (down) stage = crackSeen;
            if (stage > crackSeen)
            {
                crackSeen = stage;
                crackClock = 0f;
                if (IsLook && clock > 0.3f)
                {
                    SetLook();
                    ChipAt(CrackSpot(stage - 1) - Shift, 3);
                }
            }
            else if (stage < crackSeen) crackSeen = stage;                       // healed
            crackClock += dt;
            if (breakFlash > 0f) breakFlash = Math.Max(0f, breakFlash - dt * 1.4f);
            if (ShellOn && Life < 0.5f && !TurnPending && !down)                  // a fight begun below half : the shell goes quietly
            {
                shellDue += dt;
                if (shellDue > 1f) BreakShell(false);
            }
            else shellDue = 0f;

            float scale = Height / 512f;
            int keep = 0;
            for (int i = 0; i < chipCount; i++)
            {
                ChipBit c = chips[i];
                c.Age += dt;
                if (c.Age >= BattleRules.FigureChipLife) continue;
                c.Speed.Y += BattleRules.FigureGravity * scale * dt;
                c.At += c.Speed * dt;
                c.Turn += c.Spin * dt;
                if (c.At.Y > Feet.Y && c.Speed.Y > 0f)                           // BOUNCE
                {
                    c.At.Y = Feet.Y;
                    c.Speed.Y *= -BattleRules.FigureShardBounce;
                    c.Speed.X *= 0.6f;
                }
                chips[keep++] = c;
            }
            chipCount = keep;
        }

        private void DrawChips(float alpha)
        {
            for (int i = 0; i < chipCount; i++)
            {
                ChipBit c = chips[i];
                float fade = 1f - MathHelper.Clamp((c.Age / BattleRules.FigureChipLife - 0.6f) / 0.4f, 0f, 1f);
                if (c.White)
                {
                    Color rim = lookRim, fill = lookFill;
                    lookRim = MonumentShade * alpha;
                    lookFill = MonumentWhite * alpha;
                    Crumb(c.At + Shift, c.Size, c.Turn, fade);
                    lookRim = rim;
                    lookFill = fill;
                }
                else Crumb(c.At + Shift, c.Size, c.Turn, fade * alpha);
            }
        }

        //Glow Look : the four looks' lights into the GLOW picture (they feed the bloom) : the core strongest (and stronger on
        //its blow), the slit, the light under THE WEIGHT, the choir's mouths when it sings, the light on the floor, the cracks
        private void GlowLook(SpriteBatch sb, float power)
        {
            SetLook();
            float h = Height;
            Vector2 core = LookCore();
            float flare = 1f + lookStrike * 1.5f;
            Gfx.DrawGlow(sb, core.X, core.Y, CoreSize * 5f, Color.White * (power * 1.6f * flare));
            Gfx.DrawGlow(sb, core.X, core.Y, CoreSize * 2f, Color.White * (power * 1.4f));
            Vector2 at;
            switch (design)
            {
                case Damper:
                    at = S(120, 150);
                    Gfx.DrawGlow(sb, at.X, at.Y, 0.16f * h, Color.White * (power * 0.7f * flare));
                    break;
                case Choir:
                    for (int k = 1; k < 6 && lookStrike > 0.05f; k++)
                    {
                        at = S(ChoirHeads[k * 2], ChoirHeads[k * 2 + 1] + 4f);
                        Gfx.DrawGlow(sb, at.X, at.Y, 0.06f * h, Color.White * (power * 1.2f * lookStrike));
                    }
                    break;
                case Hush:
                    at = S(108, 150);
                    Gfx.DrawGlow(sb, at.X, at.Y, 0.1f * h, Color.White * (power * 0.6f));
                    at = S(121, 232);
                    Gfx.DrawGlow(sb, at.X, at.Y, 0.14f * h, Color.White * (power * 0.5f));
                    break;
            }
            at = SF(120, 262);                                                     // the light it gives the floor
            Gfx.DrawGlowBox(sb, new Rectangle((int)(at.X - 0.28f * h), (int)(at.Y - 0.02f * h), (int)(0.56f * h), (int)(0.04f * h)), Color.White * (power * 0.45f));
            for (int k = 0; k < crackSeen && k < 3; k++)
            {
                at = CrackSpot(k);
                Gfx.DrawGlow(sb, at.X, at.Y, 0.05f * h, Color.White * (power * 0.8f));
            }
        }

        //======================================================================================
        //THE DEAF CROWN (round 6 part 3, from the sketch docs/art_refs/concept_v6.html, docs/BETA1_DIRECTION.md 32.6)
        //======================================================================================

        //Draw Crown : the bust everyone knows of Beethoven. A V of a frown, a crown of black thorns over both ears and none in
        //front (it cannot hear), a stick held in its teeth (he heard through his jaw, a stick on the piano) whose tip holds the
        //core, a great hand raised to give orders. THE MONUMENT : white, lit from the band's side, cracking as it is hit. Broken
        //at THE TURN : the same shape in black, cracks of lightning across it (THE NINTH), the brows and the stick in white.
        //Set Boss Inks : the colours of a boss drawn from the sketch. THE MONUMENT : lit white, its far side in grey shade,
        //signs cut in black, a dark grey rim. The black form : black, darker on its far side, signs in pale white, the pale rim
        //of TACET, and the white flash when it has just broken.
        private Color bossLit;
        private Color bossDark;
        private Color bossInk;

        private void SetBossInks(float alpha, float light, float flash, float mark)
        {
            SetLook();
            bool white = ShellOn;
            float lift = Math.Max(flash, breakFlash);
            bossLit = white ? Dim(MonumentWhite, 0.6f + 0.4f * light) : Color.Lerp(BodyInk, Color.White, lift);
            bossDark = white ? Dim(MonumentShade, 0.6f + 0.4f * light) : Color.Lerp(new Color(10, 10, 13), Color.White, lift);
            bossInk = white ? MonumentInk : Dim(Palette.Paper, 0.85f);
            lookEdge = BattleRules.FigureTacetEdge * (1f + 1.5f * mark);
            lookRim = (white ? Dim(MonumentShade, 0.45f) : Color.Lerp(Palette.Paper * (BattleRules.FigureTacetRim * light), Color.White, mark)) * alpha;
        }

        private void DrawCrown(float alpha)
        {
            bool white = ShellOn;
            Color lit = bossLit, dark = bossDark, ink = bossInk;
            Vector2 hand = body[HandA].At - Local(-0.36f, -0.69f) - (body[Chest].At - Local(0f, -0.5f));   // the hand's own swing

            lookFill = lit * alpha;                                                // the robe of the bust, its far side in shade
            line[0] = S(64, 140); line[1] = S(176, 140); line[2] = SF(206, 262); line[3] = SF(34, 262);
            Shell(4, 1f);
            line[0] = S(120, 140); line[1] = S(176, 140); line[2] = SF(206, 262); line[3] = SF(120, 262);
            Shapes.Fill(line, 4, dark * alpha);
            int n = Cubic(S(89, 138), S(104, 154), S(136, 154), S(151, 138), 10, line, 0);   // the collar
            for (int i = 0; i < n; i++) lineW[i] = 15f * lookK;
            Shapes.Stroke(line, lineW, n, Color.Lerp(lit, dark, 0.35f) * alpha, Color.Lerp(lit, dark, 0.35f) * alpha, false);

            lookFill = ink * alpha;                                                // the thorns over the ears, none in front
            CrownThorn(100, 86, 70, 64, 96, 95); CrownThorn(97, 99, 58, 95, 97, 107); CrownThorn(100, 112, 70, 128, 101, 116);
            CrownThorn(140, 86, 170, 64, 144, 95); CrownThorn(143, 99, 182, 95, 143, 107); CrownThorn(140, 112, 170, 128, 139, 116);
            lookFill = lit * alpha;                                                // the head, its far half in shade
            ShellOval(S(120, 98), 23f, 29f);
            for (int i = 0; i <= 12; i++)
            {
                float t = -MathHelper.PiOver2 + i * MathHelper.Pi / 12f;
                line[i] = S(120f + (float)Math.Cos(t) * 23f, 98f + (float)Math.Sin(t) * 29f);
            }
            Shapes.Fill(line, 13, dark * alpha);
            line[0] = S(104, 92); line[1] = S(119, 99); line[2] = S(136, 90);      // the frown
            LightLine(3, 3.6f * lookK, ink * alpha);
            Shapes.Box(S(138.9f, 111.6f), 70f * lookK, 4f * lookK, -0.07f + lookLean, ink * alpha);   // the stick in its teeth

            lookFill = lit * alpha;                                                // the raised hand
            line[0] = S(182, 120) + hand; line[1] = S(214, 112) + hand; line[2] = S(218, 140) + hand; line[3] = S(190, 148) + hand;
            Shell(4, 1f);
            CrownFinger(hand, 184, 120, 180, 64, 190, 118); CrownFinger(hand, 192, 118, 195, 54, 200, 116);
            CrownFinger(hand, 201, 116, 211, 60, 208, 116); CrownFinger(hand, 209, 114, 225, 72, 215, 119);
            CrownFinger(hand, 186, 133, 162, 110, 190, 127);

            Color core = Color.White * alpha;                                      // the core at the stick's tip
            Diamond(S(176, 109), white ? 6f : 7.5f, white ? 7f : 9f, core);

            if (white)                                                             // CRACKS in the white shell, light leaking out
            {
                if (crackSeen >= 1) CrownCrack(0, MonumentInk * alpha, 1.6f);
                if (crackSeen >= 2) CrownCrack(2, MonumentInk * alpha, 1.6f);
                if (crackSeen >= 1)
                {
                    line[0] = S(97, 222); line[1] = S(91, 197);
                    LightLine(2, 1.1f * lookK, Color.White * alpha);
                }
            }
            else                                                                   // THE NINTH : the black form cracked by lightning
            {
                Color bolt = Color.White * alpha;
                CrownCrack(0, bolt, 1.8f);
                CrownCrack(1, bolt, 1.8f);
                if (crackSeen >= 3) CrownCrack(2, bolt, 1.6f);
            }
        }

        private void CrownThorn(float ax, float ay, float bx, float by, float cx, float cy)
        {
            line[0] = S(ax, ay); line[1] = S(bx, by); line[2] = S(cx, cy);
            Shell(3, 1f);
        }

        private void CrownFinger(Vector2 hand, float ax, float ay, float bx, float by, float cx, float cy)
        {
            line[0] = S(ax, ay) + hand; line[1] = S(bx, by) + hand; line[2] = S(cx, cy) + hand;
            Shell(3, 1f);
        }

        //Crown Crack : the cracks of the sketch (down its left side, down its right, over its brow)
        private static readonly float[] CrownCracks =
        {
            84, 168, 96, 186, 90, 196, 102, 222, 96, 240,
            156, 162, 146, 182, 154, 194, 142, 224, 142, 224,
            108, 72, 114, 84, 110, 92, 110, 92, 110, 92,
        };

        private void CrownCrack(int k, Color c, float width)
        {
            for (int i = 0; i < 5; i++) line[i] = S(CrownCracks[k * 10 + i * 2], CrownCracks[k * 10 + i * 2 + 1]);
            LightLine(5, width * lookK, c);
        }

        //======================================================================================
        //REQUIEM (round 6 part 3, from the sketch) : the piece Mozart never finished
        //======================================================================================

        //Draw Requiem : a tall slim robe, a small head under a broken halo, a quill held as a blade, tears running down
        //(LACRIMOSA), and from its other hand the five lines of a score that stop half way across : the core is where they
        //stop. THE MONUMENT white, then the black form with the same lines and tears in white.
        private void DrawRequiem(float alpha)
        {
            bool white = ShellOn;
            Color lit = bossLit, dark = bossDark, ink = bossInk;
            Color light = Dim(MonumentWhite, 1f) * (0.7f * alpha);
            Vector2 hand = body[HandA].At - Local(-0.124f, -0.44f) - (body[Chest].At - Local(0f, -0.5f));   // the quill's own swing

            for (int k = 0; k < 5; k++)                                            // the unfinished lines, their ends never still
            {
                float y = 150f + k * 5f;
                float end = RequiemEnds[k] - (float)Math.Sin(clock * 1.3f + k * 1.7f) * 3f;
                Shapes.Line(S(end, y), S(88, y), 0.9f * lookK, light);
            }
            lookFill = lit * alpha;                                                // the robe, its far side in shade
            int n = Cubic(S(120, 98), S(138, 140), S(152, 208), SF(166, 262), 10, line, 0);
            line[n++] = SF(74, 262);
            n = CubicMore(SF(74, 262), S(88, 208), S(102, 140), S(120, 98), 10, line, n);
            Shell(n - 1, 1f);
            n = Cubic(S(120, 98), S(138, 140), S(152, 208), SF(166, 262), 10, line, 0);
            line[n++] = SF(120, 262);
            Shapes.Fill(line, n, dark * alpha);
            ShellOval(S(120, 80), 13f, 16f);                                       // the head, its far half in shade
            for (int i = 0; i <= 10; i++)
            {
                float t = -MathHelper.PiOver2 + i * MathHelper.Pi / 10f;
                line[i] = S(120f + (float)Math.Cos(t) * 13f, 80f + (float)Math.Sin(t) * 16f);
            }
            Shapes.Fill(line, 11, dark * alpha);
            for (int i = 0; i < 16; i++)                                           // the broken halo, open at the front
            {
                float t = MathHelper.ToRadians(190f + i * 12f);
                line[i] = S(118f + (float)Math.Cos(t) * 24f, 60f + (float)Math.Sin(t) * 7f);
            }
            LightLine(16, 2.4f * lookK, (white ? MonumentWhite : Dim(Palette.Paper, 0.9f)) * alpha);
            lookFill = lit * alpha;
            line[0] = S(140, 72); line[1] = S(146, 75); line[2] = S(141, 78);      //   a piece of it, fallen
            Shell(3, 1f);

            n = Cubic(S(146, 162) + hand, S(172, 128) + hand, S(196, 96) + hand, S(218, 60) + hand, 10, line, 0);   // the quill, a blade
            n = CubicMore(S(218, 60) + hand, S(206, 100) + hand, S(186, 134) + hand, S(152, 168) + hand, 10, line, n);
            Shell(n, 1f);
            Shapes.Line(S(150, 166) + hand, S(216, 62) + hand, 1f * lookK, dark * alpha);
            Shapes.Polygon(S(147.5f, 164.5f) + hand, 6.4f * lookK, 4, lookLean, lit * alpha);   // the hand on it
            Shapes.Polygon(S(90, 159), 5.7f * lookK, 4, lookLean, lit * alpha);                 // the hand the lines leave

            for (int k = 0; k < 3; k++)                                            // LACRIMOSA : tears running down
            {
                float t = (clock * 0.3f + k / 3f) % 1f;
                Vector2 at = S(114f - k * 1.5f, 98f + t * 52f);
                float size = (3.6f - k * 0.5f) * lookK;
                Color tear = Color.White * (alpha * (1f - t * t));
                Shapes.Circle(at, size * 0.6f, tear);
                Shapes.Kite(at, at - new Vector2(0f, size * 1.8f), size * 1.1f, 0.15f, tear);
            }
            Diamond(S(32, 160), white ? 6f : 7.5f, white ? 7f : 9f, Color.White * alpha);   // the core where the lines stop

            if (white)                                                             // CRACKS in the white shell
            {
                if (crackSeen >= 1) RequiemCrack(0, ink * alpha, 1.4f);
                if (crackSeen >= 2) RequiemCrack(1, ink * alpha, 1.4f);
            }
            else
            {
                RequiemCrack(0, Color.White * alpha, 1.6f);
                RequiemCrack(1, Color.White * alpha, 1.6f);
            }
        }

        private static readonly float[] RequiemEnds = { 34, 30, 38, 33, 40 };
        private static readonly float[] RequiemCracks = { 104, 190, 110, 206, 106, 214, 114, 236, 140, 200, 134, 214, 139, 224, 139, 224 };

        private void RequiemCrack(int k, Color c, float width)
        {
            for (int i = 0; i < 4; i++) line[i] = S(RequiemCracks[k * 8 + i * 2], RequiemCracks[k * 8 + i * 2 + 1]);
            LightLine(4, width * lookK, c);
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
            if (shardCount > 0)
            {
                for (int i = 0; i < shardCount; i += 4) Gfx.DrawGlow(sb, shards[i].At.X, shards[i].At.Y, shards[i].Size * 2f, Color.White * (power * 0.5f));
                float t = moveTime / BattleRules.FigureReleaseTime;                  // RELEASE : the freed light glows as it rises
                if (t < 1f)
                {
                    float e = Ease(t);
                    Vector2 ring = releaseAt + Shift - new Vector2(0f, 0.35f * e * Height);
                    Gfx.DrawGlow(sb, ring.X, ring.Y, (0.1f + 0.35f * e) * h, Color.White * (power * 2.2f * (1f - t)));
                }
                return;
            }
            if (IsLook)
            {
                GlowLook(sb, power);
                return;
            }
            Gfx.DrawGlow(sb, at.X, at.Y, 0.18f * h, Color.White * (power * (ShellOn ? 0.9f : 0.6f)));
            Vector2 core = CorePoint() + Shift;                                      // the heart glows (it feeds the bloom)
            float bright = ShellOn ? 1f : 1.5f;                                      // brighter once the shell is gone
            Gfx.DrawGlow(sb, core.X, core.Y, CoreSize * 5f, Color.White * (power * 1.6f * bright));
            Gfx.DrawGlow(sb, core.X, core.Y, CoreSize * 2f, Color.White * (power * 1.4f * bright));
            if (breakFlash > 0f)                                                     // THE MONUMENT breaking : a white blast
            {
                Vector2 middle = body[Chest].At + Shift;
                Gfx.DrawGlow(sb, middle.X, middle.Y, 0.75f * h, Color.White * (power * 4f * breakFlash));
            }
            if (design == Crown && !ShellOn)                                         // THE NINTH : the lightning cracks glow
            {
                SetLook();
                Vector2 c1 = S(96, 200), c2 = S(150, 190);
                Gfx.DrawGlow(sb, c1.X, c1.Y, 0.12f * h, Color.White * (power * 0.9f));
                Gfx.DrawGlow(sb, c2.X, c2.Y, 0.1f * h, Color.White * (power * 0.9f));
            }
        }
    }
}
