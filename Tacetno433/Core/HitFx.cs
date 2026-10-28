using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //Hit Style : how a blow looks where it lands (HitFx)
    public enum HitStyle { Slash, Shot, Smash, Cross, Ink }

    //HitFx (Beta1 round 4) : what a blow looks like where it lands. Nothing flies across the screen : the musician
    //strikes and the light happens on the enemy (the user's references, docs/BATTLE_SCENE_2_3D_CHANGE.md 12.5).
    //   SLASH   strings, the bow as a blade : crescents of light sweep across the enemy, a long thin cut, sparks along it
    //   SHOT    winds : sharp spikes burst out of one point, a round glow, a ring, sparks sprayed the way the blow went
    //   SMASH   percussion : a heavy blow from above, a ring on the floor, shards of glass thrown up and falling
    //   CROSS   the SIGNATURE : two cuts across the whole screen crossing on the enemy, a four pointed star where they cross
    //   INK     TACET's blow on a musician : white cracks and a ring, dark drops falling
    //   PULL    an enemy SILENCED : specks of it are drawn off into TACET's hole
    //   STAR    a four pointed twinkle, for a perfect note or a big moment
    //   FOCUS   lines rushing in to the point of a big blow, like a comic
    //   IMPACT  a frame or two of the world turned over, black and white (IMPACT FRAME, Post.fx)
    //Everything is lines, squares and the soft Glow texture, drawn into the GLOW layer so it shines and blooms (the
    //INK drops go into the SCENE). Every piece sits on the art's 2 x 2 grid (PIXEL x2) and the pieces live in fixed
    //pools, so a long fight makes no garbage. Sizes and times are in Data/BattleRules.cs, Hit Feel.
    public class HitFx
    {
        //Stroke : one cut of light, curved (a crescent) or straight. It draws on fast from its head, holds, then is
        //wiped off from its tail, the way a blade's trail looks.
        private struct Stroke
        {
            public bool On;
            public bool Straight;
            public Vector2 Center;     // curved : the middle of its ellipse
            public float Radius;
            public float Squash;       //   how flat the ellipse is (1 = a circle)
            public float Turn;         //   how the ellipse is turned
            public float From;         //   where on it the cut starts, and how far round it goes (minus = the other way)
            public float Sweep;
            public Vector2 A, B;       // straight : from A to B
            public float Width;
            public float Age;          // below 0 = still waiting (a second cut comes a moment after the first)
            public Color Tint;
        }

        //Burst : sharp spikes out of one point, longer along the way the blow went (Stretch)
        private struct Burst
        {
            public bool On;
            public Vector2 At;
            public float Size, Angle, Stretch, Width, Age, Length;
            public int Spikes, Seed;
            public Color Tint;
        }

        //Ring : a ring that opens and thins (Squash below 1 = lying on the floor)
        private struct Ring
        {
            public bool On;
            public Vector2 At;
            public float Size, Squash, Width, Age, Length;
            public Color Tint;
        }

        //Bit : one small piece. SPARK a square that falls, STREAK a line along its way, SHARD a sliver of glass that
        //turns and glints, INK a dark drop (drawn into the SCENE), PULL a speck drawn towards Target
        private const byte Spark = 0, Streak = 1, Shard = 2, Ink = 3, Pull = 4;
        private struct Bit
        {
            public bool On;
            public byte Kind;
            public Vector2 At, Speed, Target;
            public float Life, Length, Size, Spin, Turn;
            public Color Tint;
        }

        private readonly Stroke[] strokes = new Stroke[32];
        private readonly Burst[] bursts = new Burst[16];
        private readonly Ring[] rings = new Ring[16];
        private readonly Bit[] bits = new Bit[320];
        private readonly Random random = new Random(433);

        //FOCUS : one set of lines at a time
        private Vector2 focusAt;
        private float focusAge = 99f;

        //IMPACT FRAME : how long the world stays turned over
        private float impactLeft;
        public float Impact
        {
            get { return impactLeft > 0f ? 1f : 0f; }
        }

        //Stroke Times : draw on, hold, wipe off (seconds)
        private const float DrawOn = 0.08f;
        private const float Hold = 0.07f;
        private const float WipeOff = 0.22f;

        //======================================================================================
        //Hits
        //======================================================================================

        //Hit : a blow of this style lands on a fighter standing in box. power 0..1.5 scales everything,
        //tint is the striker's own colour (white until the art and its colours come).
        public void Hit(HitStyle style, Rectangle box, float power, Color tint)
        {
            Vector2 at = new Vector2(box.Center.X, box.Y + box.Height * 0.45f);
            float size = (0.75f + 0.35f * power) * BattleRules.HitSize;
            float lean = (float)(random.NextDouble() - 0.5) * 0.6f;

            if (style == HitStyle.Slash)
            {
                //SLASH : two crescents crossing a little, the second a moment later, and a long thin cut through both
                float turn = -0.55f + lean;
                AddCrescent(at, size * 1.25f, 0.42f, turn, 2.5f, 3.1f, 20f * (0.7f + 0.5f * power), 0f, tint);
                AddCrescent(at + new Vector2(14f, -10f), size * 1.0f, 0.38f, turn + 0.5f, 5.8f, -2.6f, 13f * (0.7f + 0.5f * power), -0.07f, tint);
                Vector2 way = new Vector2((float)Math.Cos(turn), (float)Math.Sin(turn));
                AddCut(at - way * size * 2.2f, at + way * size * 2.2f, 3f, -0.02f, tint);
                AddBurst(at, size * 0.45f, (float)Math.Atan2(way.Y, way.X), 1.8f, 8, 3f, 0.16f, tint);
                Throw(at, way, 14 + (int)(10 * power), 520f, Streak, tint);
                Throw(at, -way, 6, 300f, Spark, tint);
            }
            else if (style == HitStyle.Shot)
            {
                //SHOT : the spikes stretched the way the shot went (from the band, left to right), a round glow, a ring
                float angle = -0.12f + lean * 0.3f;
                AddBurst(at, size * 1.25f, angle, 1.9f, 18, 5f, 0.3f, tint);
                AddRing(at, size * 0.9f, 1f, 3f, 0.3f, tint);
                Vector2 way = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                Throw(at, way, 18 + (int)(10 * power), 640f, Streak, tint);
                Throw(at, way, 10, 360f, Spark, tint);
            }
            else if (style == HitStyle.Smash)
            {
                //SMASH : spikes stretched up and down (a blow from above), a ring opening on the floor, glass thrown up
                Vector2 feet = new Vector2(box.Center.X, box.Bottom - 6f);
                AddBurst(at, size * 1.15f, -MathHelper.PiOver2, 1.7f, 14, 5f, 0.28f, tint);
                AddRing(feet, size * 1.8f, 0.24f, 4f, 0.42f, tint);
                AddRing(feet, size * 1.1f, 0.24f, 2f, 0.3f, tint);
                Throw(at, new Vector2(0f, -1f), 16 + (int)(8 * power), 460f, Shard, tint);
                Throw(feet, new Vector2(0f, -1f), 10, 260f, Spark, tint);
            }
            else if (style == HitStyle.Cross)
            {
                //CROSS : two cuts across the whole screen meeting on the enemy, a star where they cross
                float a = 0.42f + lean * 0.2f;
                float b = -0.38f + lean * 0.2f;
                float reach = BattleRules.HitCrossLength;
                Vector2 wayA = new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
                Vector2 wayB = new Vector2((float)Math.Cos(b), (float)Math.Sin(b));
                AddCut(at - wayA * reach, at + wayA * reach, 5f, 0f, tint);
                AddCut(at + wayB * reach, at - wayB * reach, 5f, -0.08f, tint);
                Star(at, size * 2.2f, tint);
                AddBurst(at, size * 1.3f, 0f, 1f, 22, 4f, 0.32f, tint);
                AddRing(at, size * 1.6f, 1f, 3f, 0.4f, tint);
                Throw(at, wayA, 16, 700f, Streak, tint);
                Throw(at, -wayB, 16, 700f, Streak, tint);
                Throw(at, new Vector2(0f, -1f), 14, 420f, Shard, tint);
                Focus(at);
                ImpactFrame();
            }
            else
            {
                //INK : TACET's note bursts on a musician, a ring of white, white cracks, dark drops falling
                AddRing(at, size * 0.9f, 1f, 3f, 0.3f, Color.White);
                AddBurst(at, size * 0.7f, 0f, 1f, 10, 2f, 0.2f, Color.White);
                Throw(at, new Vector2(0f, -1f), 16, 380f, Ink, Color.White);
            }
        }

        //Clash : a parry meets TACET's note at the ring. perfect = the blow is turned away whole (a star, spikes, a ring
        //and sparks all round), otherwise a smaller burst.
        public void Clash(Vector2 at, bool perfect, Color tint)
        {
            float size = BattleRules.HitSize;
            if (perfect)
            {
                Star(at, size * 1.5f, tint);
                AddBurst(at, size * 0.9f, 0f, 1f, 16, 4f, 0.24f, tint);
                AddRing(at, size * 1.2f, 1f, 3f, 0.32f, tint);
                for (int k = 0; k < 4; k++) Throw(at, new Vector2((float)Math.Cos(k * MathHelper.PiOver2), (float)Math.Sin(k * MathHelper.PiOver2)), 5, 520f, Streak, tint);
            }
            else
            {
                AddBurst(at, size * 0.5f, 0f, 1f, 10, 3f, 0.18f, tint);
                AddRing(at, size * 0.7f, 1f, 2f, 0.24f, tint);
            }
        }

        //Silence : an enemy SILENCED. Specks of it lift off all over its box and are drawn into the hole at target,
        //faster as they go, with a ring and a star where it stood.
        public void Silence(Rectangle box, Vector2 target)
        {
            Vector2 middle = new Vector2(box.Center.X, box.Center.Y);
            Star(middle, BattleRules.HitSize * 1.6f, Color.White);
            AddRing(middle, BattleRules.HitSize * 1.4f, 1f, 3f, 0.45f, Color.White);
            for (int i = 0; i < 44; i++)
            {
                int slot = Free(bits);
                Vector2 at = new Vector2(box.X + (float)random.NextDouble() * box.Width, box.Y + (float)random.NextDouble() * box.Height);
                Vector2 away = at - middle;
                bits[slot] = new Bit
                {
                    On = true,
                    Kind = Pull,
                    At = at,
                    Speed = away * (1.5f + 2f * (float)random.NextDouble()) + new Vector2(0f, -60f),
                    Target = target,
                    Life = 1.1f + 0.7f * (float)random.NextDouble(),
                    Size = 2f,
                    Tint = Color.White,
                };
                bits[slot].Length = bits[slot].Life;
            }
        }

        //Star : a four pointed twinkle, the long points up and down
        public void Star(Vector2 at, float size, Color tint)
        {
            int slot = Free(bursts);
            bursts[slot] = new Burst { On = true, At = at, Size = size, Angle = -MathHelper.PiOver2, Stretch = 1f, Width = 6f, Age = 0f, Length = 0.34f, Spikes = 4, Seed = -1, Tint = tint };
        }

        //Focus : lines rush in to a point for a moment
        public void Focus(Vector2 at)
        {
            focusAt = at;
            focusAge = 0f;
        }

        //Impact Frame : the world turns over for a frame or two (none when the player asked for less flashing)
        public void ImpactFrame()
        {
            if (Settings.ReduceFlashing) return;
            impactLeft = BattleRules.HitImpactTime;
        }

        //======================================================================================
        //Spawning
        //======================================================================================

        //Add Crescent : a curved cut. from and sweep are angles on its ellipse, delay below 0 waits a moment.
        private void AddCrescent(Vector2 center, float radius, float squash, float turn, float from, float sweep, float width, float delay, Color tint)
        {
            int slot = Free(strokes);
            strokes[slot] = new Stroke { On = true, Straight = false, Center = center, Radius = radius, Squash = squash, Turn = turn, From = from, Sweep = sweep, Width = width, Age = delay, Tint = tint };
        }

        //Add Cut : a straight cut from a to b
        private void AddCut(Vector2 a, Vector2 b, float width, float delay, Color tint)
        {
            int slot = Free(strokes);
            strokes[slot] = new Stroke { On = true, Straight = true, A = a, B = b, Width = width, Age = delay, Tint = tint };
        }

        private void AddBurst(Vector2 at, float size, float angle, float stretch, int spikes, float width, float length, Color tint)
        {
            int slot = Free(bursts);
            bursts[slot] = new Burst { On = true, At = at, Size = size, Angle = angle, Stretch = stretch, Width = width, Age = 0f, Length = length, Spikes = spikes, Seed = random.Next(1000), Tint = tint };
        }

        private void AddRing(Vector2 at, float size, float squash, float width, float length, Color tint)
        {
            int slot = Free(rings);
            rings[slot] = new Ring { On = true, At = at, Size = size, Squash = squash, Width = width, Age = 0f, Length = length, Tint = tint };
        }

        //Throw : count bits out of a point, mostly along way (spread about 70 degrees)
        private void Throw(Vector2 at, Vector2 way, int count, float speed, byte kind, Color tint)
        {
            float baseAngle = (float)Math.Atan2(way.Y, way.X);
            for (int i = 0; i < count; i++)
            {
                int slot = Free(bits);
                float angle = baseAngle + (float)(random.NextDouble() - 0.5) * 1.25f;
                float fast = speed * (0.35f + 0.65f * (float)random.NextDouble());
                bits[slot] = new Bit
                {
                    On = true,
                    Kind = kind,
                    At = at,
                    Speed = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * fast,
                    Life = 0.25f + 0.35f * (float)random.NextDouble() + (kind == Shard || kind == Ink ? 0.3f : 0f),
                    Size = kind == Shard ? 6f + 8f * (float)random.NextDouble() : 2f,
                    Spin = (float)(random.NextDouble() * MathHelper.TwoPi),
                    Turn = (float)(random.NextDouble() - 0.5) * 18f,
                    Tint = tint,
                };
                bits[slot].Length = bits[slot].Life;
            }
        }

        //Free : a slot that is off, or else the first one (the oldest is usually near its end)
        private static int Free(Stroke[] pool) { for (int i = 0; i < pool.Length; i++) if (!pool[i].On) return i; return 0; }
        private static int Free(Burst[] pool) { for (int i = 0; i < pool.Length; i++) if (!pool[i].On) return i; return 0; }
        private static int Free(Ring[] pool) { for (int i = 0; i < pool.Length; i++) if (!pool[i].On) return i; return 0; }
        private static int Free(Bit[] pool) { for (int i = 0; i < pool.Length; i++) if (!pool[i].On) return i; return 0; }

        //======================================================================================
        //Update
        //======================================================================================

        //Update : frozen = the hit stop. The cuts and bursts still play (they ARE the moment), the bits hold still.
        public void Update(float dt, bool frozen)
        {
            if (impactLeft > 0f) impactLeft -= dt;
            focusAge += dt;

            for (int i = 0; i < strokes.Length; i++)
            {
                if (!strokes[i].On) continue;
                strokes[i].Age += dt;
                if (strokes[i].Age > DrawOn + Hold + WipeOff) strokes[i].On = false;
            }
            for (int i = 0; i < bursts.Length; i++)
            {
                if (!bursts[i].On) continue;
                bursts[i].Age += dt;
                if (bursts[i].Age > bursts[i].Length) bursts[i].On = false;
            }
            for (int i = 0; i < rings.Length; i++)
            {
                if (!rings[i].On) continue;
                rings[i].Age += dt;
                if (rings[i].Age > rings[i].Length) rings[i].On = false;
            }

            if (frozen) return;
            float drag = (float)Math.Pow(0.04, dt);
            for (int i = 0; i < bits.Length; i++)
            {
                if (!bits[i].On) continue;
                bits[i].Life -= dt;
                if (bits[i].Life <= 0f)
                {
                    bits[i].On = false;
                    continue;
                }
                if (bits[i].Kind == Pull)
                {
                    //PULL : drawn towards the hole harder and harder, gone when it gets there
                    Vector2 to = bits[i].Target - bits[i].At;
                    float far = to.Length();
                    if (far < 16f)
                    {
                        bits[i].On = false;
                        continue;
                    }
                    bits[i].Speed += to / far * (900f + 2400f * (1f - bits[i].Life / bits[i].Length)) * dt;
                    bits[i].Speed *= (float)Math.Pow(0.3, dt);
                    bits[i].At += bits[i].Speed * dt;
                    continue;
                }
                bits[i].At += bits[i].Speed * dt;
                bits[i].Speed *= bits[i].Kind == Streak ? drag * 0.97f : drag;
                if (bits[i].Kind != Streak) bits[i].Speed.Y += (bits[i].Kind == Ink ? 900f : 700f) * dt;    // falls
                bits[i].Spin += bits[i].Turn * dt;
            }
        }

        public void Clear()
        {
            for (int i = 0; i < strokes.Length; i++) strokes[i].On = false;
            for (int i = 0; i < bursts.Length; i++) bursts[i].On = false;
            for (int i = 0; i < rings.Length; i++) rings[i].On = false;
            for (int i = 0; i < bits.Length; i++) bits[i].On = false;
            focusAge = 99f;
            impactLeft = 0f;
        }

        //======================================================================================
        //Draw
        //======================================================================================

        //Draw Glow : into the GLOW layer (additive), with the fighters' camera
        public void DrawGlow(SpriteBatch sb)
        {
            DrawFocus(sb);
            for (int i = 0; i < rings.Length; i++) if (rings[i].On) DrawRing(sb, rings[i]);
            for (int i = 0; i < bursts.Length; i++) if (bursts[i].On) DrawBurst(sb, bursts[i]);
            for (int i = 0; i < strokes.Length; i++) if (strokes[i].On && strokes[i].Age > 0f) DrawStroke(sb, strokes[i]);
            for (int i = 0; i < bits.Length; i++) if (bits[i].On && bits[i].Kind != Ink) DrawBit(sb, bits[i]);
        }

        //Draw Scene : the dark drops, into the SCENE after the fighters
        public void DrawScene(SpriteBatch sb)
        {
            for (int i = 0; i < bits.Length; i++)
            {
                if (!bits[i].On || bits[i].Kind != Ink) continue;
                Vector2 p = Snap(bits[i].At);
                float a = Math.Min(1f, bits[i].Life / (bits[i].Length * 0.4f));
                Gfx.Rect(sb, p.X, p.Y, 4f, 4f, Color.Black * a);
            }
        }

        //Draw Stroke : the visible part runs from the tail to the head. Thick in the middle of the cut, sharp at both
        //ends (a crescent), with a wide soft layer under the hard core, and thin threads trailing inside a crescent.
        private void DrawStroke(SpriteBatch sb, Stroke s)
        {
            float head = Ease(Math.Min(1f, s.Age / DrawOn));
            float tail = Ease(MathHelper.Clamp((s.Age - DrawOn - Hold) / WipeOff, 0f, 1f));
            if (head - tail < 0.01f) return;

            const int steps = 22;
            Vector2 last = Point(s, tail, 1f);
            for (int k = 1; k <= steps; k++)
            {
                float t = MathHelper.Lerp(tail, head, k / (float)steps);
                Vector2 p = Point(s, t, 1f);
                float mid = MathHelper.Lerp(tail, head, (k - 0.5f) / steps);
                float w = s.Width * (float)Math.Pow(Math.Sin(Math.PI * mid), 0.7);
                Gfx.Line(sb, Snap(last), Snap(p), s.Tint * 0.22f, Math.Max(2f, w * 2.6f));
                Gfx.Line(sb, Snap(last), Snap(p), s.Tint, Math.Max(1f, w));
                last = p;
            }

            //Threads : two thin cuts just inside the crescent, a little shorter
            if (!s.Straight)
            {
                for (int thread = 1; thread <= 2; thread++)
                {
                    float inside = 1f - 0.08f * thread;
                    Vector2 from = Point(s, tail, inside);
                    for (int k = 1; k <= 12; k++)
                    {
                        float t = MathHelper.Lerp(tail, head * (1f - 0.1f * thread), k / 12f);
                        Vector2 p = Point(s, t, inside);
                        Gfx.Line(sb, Snap(from), Snap(p), s.Tint * (0.55f / thread), 1f);
                        from = p;
                    }
                }
            }
        }

        //Point : where the cut is at t (0..1 along it). scale moves a crescent's point in towards its middle.
        private static Vector2 Point(Stroke s, float t, float scale)
        {
            if (s.Straight) return Vector2.Lerp(s.A, s.B, t);
            float angle = s.From + s.Sweep * t;
            Vector2 local = new Vector2((float)Math.Cos(angle) * s.Radius * scale, (float)Math.Sin(angle) * s.Radius * s.Squash * scale);
            float c = (float)Math.Cos(s.Turn);
            float n = (float)Math.Sin(s.Turn);
            return s.Center + new Vector2(local.X * c - local.Y * n, local.X * n + local.Y * c);
        }

        //Draw Burst : spikes snap out, then fade. The ones along the blow's way are longer (Stretch).
        private void DrawBurst(SpriteBatch sb, Burst b)
        {
            float grow = Ease(Math.Min(1f, b.Age / 0.06f));
            float left = 1f - b.Age / b.Length;
            float fade = left * left;
            Gfx.DrawGlow(sb, b.At.X, b.At.Y, b.Size * 0.8f * grow, b.Tint * (0.55f * fade));
            Gfx.DrawGlow(sb, b.At.X, b.At.Y, b.Size * 0.28f, b.Tint * fade);

            for (int i = 0; i < b.Spikes; i++)
            {
                float angle, length;
                if (b.Seed < 0)
                {
                    //STAR : four points, up and down twice as long as the sides
                    angle = b.Angle + i * MathHelper.PiOver2;
                    length = b.Size * (i % 2 == 0 ? 1f : 0.5f);
                }
                else
                {
                    angle = b.Angle + i * MathHelper.TwoPi / b.Spikes + (Hash(b.Seed, i) - 0.5f) * 0.35f;
                    float along = (float)Math.Abs(Math.Cos(angle - b.Angle));
                    length = b.Size * (0.4f + 0.8f * Hash(b.Seed + 7, i)) * (1f + (b.Stretch - 1f) * along * along);
                }
                Vector2 way = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                DrawSpike(sb, b.At, way, length * grow, b.Width * (0.5f + 0.5f * fade), b.Tint * fade);
            }
        }

        //Draw Spike : a sharp point drawn in three steps, each thinner (the art's stepped taper)
        private static void DrawSpike(SpriteBatch sb, Vector2 from, Vector2 way, float length, float width, Color color)
        {
            Vector2 a = Snap(from);
            Vector2 b = Snap(from + way * length * 0.45f);
            Vector2 c = Snap(from + way * length * 0.75f);
            Vector2 d = Snap(from + way * length);
            Gfx.Line(sb, a, b, color, Math.Max(2f, width));
            Gfx.Line(sb, b, c, color, Math.Max(1f, width * 0.6f));
            Gfx.Line(sb, c, d, color * 0.8f, 1f);
        }

        //Draw Ring : opens fast, then thins away
        private static void DrawRing(SpriteBatch sb, Ring r)
        {
            float t = r.Age / r.Length;
            float radius = r.Size * Ease(Math.Min(1f, t * 1.4f));
            float a = (1f - t);
            Gfx.EllipseOutline(sb, r.At.X, r.At.Y, radius, radius * r.Squash, r.Tint * (a * a), Math.Max(1f, r.Width * a));
        }

        //Draw Bit : sparks are squares, streaks lines along their way, shards slivers that glint as they turn
        private static void DrawBit(SpriteBatch sb, Bit b)
        {
            float a = Math.Min(1f, b.Life / (b.Length * 0.5f));
            Vector2 p = Snap(b.At);
            if (b.Kind == Spark) Gfx.Rect(sb, p.X, p.Y, 2f, 2f, b.Tint * a);
            else if (b.Kind == Pull)
            {
                Vector2 back = b.At - b.Speed * 0.02f;
                Gfx.Line(sb, Snap(back), p, b.Tint * (0.5f * a), 1f);
                Gfx.Rect(sb, p.X, p.Y, 2f, 2f, b.Tint * a);
            }
            else if (b.Kind == Streak)
            {
                Vector2 back = b.At - b.Speed * 0.035f;
                Gfx.Line(sb, Snap(back), p, b.Tint * a, 2f);
            }
            else
            {
                Vector2 way = new Vector2((float)Math.Cos(b.Spin), (float)Math.Sin(b.Spin)) * b.Size * 0.5f;
                float glint = 0.35f + 0.65f * (float)Math.Abs(Math.Cos(b.Spin * 2f));
                Gfx.Line(sb, Snap(b.At - way), Snap(b.At + way), b.Tint * (a * glint), 2f);
            }
        }

        //Draw Focus : thin lines from far out rushing in towards the point, gone in a moment
        private void DrawFocus(SpriteBatch sb)
        {
            const float length = 0.22f;
            if (focusAge >= length) return;
            float t = focusAge / length;
            for (int i = 0; i < 36; i++)
            {
                float angle = Hash(i, 11) * MathHelper.TwoPi;
                float outer = 520f + 380f * Hash(i, 12);
                float inner = 150f + 140f * Hash(i, 13) - 60f * t;
                Vector2 way = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                Gfx.Line(sb, Snap(focusAt + way * outer), Snap(focusAt + way * inner), Color.White * (0.5f * (1f - t)), i % 3 == 0 ? 2f : 1f);
            }
        }

        //Ease : fast, then settling (ease out)
        private static float Ease(float t)
        {
            float u = 1f - t;
            return 1f - u * u * u;
        }

        //Snap : onto the grid of the art's pixels (PIXEL x2)
        private static Vector2 Snap(Vector2 v)
        {
            return new Vector2((float)Math.Floor(v.X * 0.5f) * 2f, (float)Math.Floor(v.Y * 0.5f) * 2f);
        }

        //Hash : a steady random 0..1 from two whole numbers (the same trick as TacetField)
        private static float Hash(int a, int b)
        {
            float h = (float)Math.Sin(a * 12.9898f + b * 78.233f) * 43758.547f;
            return h - (float)Math.Floor(h);
        }
    }
}
