using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Core;

namespace Tacetno433.Battle
{
    //DuelEffects : the sound waves, the clash bursts and the numbers that pop up during a duel.
    //
    //Everything is made ONCE when the duel opens and then reused (an "object pool").
    //Spawning a wave just switches on a slot that is currently off, so a long fight never
    //creates new objects and never feeds the garbage collector.
    //
    //A wave is one big arc of sound rolling toward the line in the middle of the screen.
    //When its front reaches the line it is spent: it reports a hit, the screen shakes the
    //line, and a burst is left behind where it landed.
    //
    //The HOLLOW effects (see Core/Hollow.cs) live here too:
    //   Spark    a white shard thrown out of a clash, falling as it fades
    //   Flare    a streak of light across the hit point
    //   Ripple   a flat ring spreading on the floor under whoever just played, like sound on water
    //   Motes    specks of light and small hollow rings drifting up through TACET's dark
    //
    //And the HIT effects at the hit point, studied from the tap effects of Project Sekai:
    //   HitBurst rings of light opening from the hit point, bigger and brighter for a better grade
    //   Shard    a piece of TACET's hollow note, broken off when the band wins the beat
    public class DuelEffects
    {
        //Wave : one arc of sound travelling toward the line
        private class Wave
        {
            public bool On;
            public bool Spent;
            public float X, Y, Radius, Life, Dir, Power, Speed;
        }

        //Burst : the ring left where a wave hit the line
        private class Burst
        {
            public bool On;
            public float X, Y, Radius, Life, Power;
        }

        //Pop : one short piece of text that floats up and fades
        private class Pop
        {
            public bool On;
            public float X, Y, Life, Scale;
            public string Text = "";
            public Color Color;
        }

        //Spark : one shard of light
        private class Spark
        {
            public bool On;
            public float X, Y, VX, VY, Life;
        }

        //Flare : one streak of light
        private class Flare
        {
            public bool On;
            public float X, Y, Life, Strength;
        }

        //Ripple : one ring on the floor. Dark ripples are drawn in TACET's black, light ones on our stage.
        private class Ripple
        {
            public bool On;
            public bool Dark;
            public float X, Y, Life, Size;
        }

        //Hit Burst : one burst at the hit point, drawn by grade
        private class HitBurst
        {
            public bool On;
            public float X, Y, Life;
            public Grade Grade;
        }

        //Shard : one arc of TACET's broken note. X, Y is the middle of the ring it came from,
        //which flies outward, so the arc drawn around it flies away from the hit point.
        private class Shard
        {
            public bool On;
            public float X, Y, VX, VY, Angle, Spin, Life, Radius;
        }

        private const float SparkLife = 0.55f;
        private const float FlareLife = 0.28f;
        private const float RippleLife = 0.9f;
        private const float HitLife = 0.42f;
        private const float ShardLife = 0.5f;
        private const float ShardArc = 0.8f;       // how much of the ring one shard is, in radians
        private const int MoteCount = 22;

        private Spark[] sparks = new Spark[60];
        private Flare[] flares = new Flare[6];
        private Ripple[] ripples = new Ripple[20];
        private HitBurst[] hits = new HitBurst[8];
        private Shard[] shards = new Shard[24];

        //Motes : fixed specks, each with its own place across the dark (0 to 1), height and speed
        private float[] moteAcross = new float[MoteCount];
        private float[] moteY = new float[MoteCount];
        private float[] moteSpeed = new float[MoteCount];
        private float[] moteSize = new float[MoteCount];

        //Scatter : only decides where sparks fly, never anything in the rules
        private Random scatter = new Random(11);

        private const float WaveLife = 1.4f;
        private const float WaveSpeed = 250f;     // how fast the arc travels sideways
        private const float WaveGrow = 300f;      // how fast the arc opens up
        private const float BurstLife = 0.4f;
        private const float PopLife = 1.2f;

        private Wave[] waves = new Wave[10];
        private Burst[] bursts = new Burst[10];
        private Pop[] pops = new Pop[20];

        //Line X : where the two sides meet. The duel sets this every frame.
        public float LineX = 640f;

        //Line Hit : true for the one frame a wave reaches the line, read by the duel so it
        //can shake the line and shudder the picture
        public bool Hit;
        public float HitY;
        public float HitDir;
        public float HitPower;

        public DuelEffects()
        {
            for (int i = 0; i < waves.Length; i++) waves[i] = new Wave();
            for (int i = 0; i < bursts.Length; i++) bursts[i] = new Burst();
            for (int i = 0; i < pops.Length; i++) pops[i] = new Pop();
            for (int i = 0; i < sparks.Length; i++) sparks[i] = new Spark();
            for (int i = 0; i < flares.Length; i++) flares[i] = new Flare();
            for (int i = 0; i < ripples.Length; i++) ripples[i] = new Ripple();
            for (int i = 0; i < hits.Length; i++) hits[i] = new HitBurst();
            for (int i = 0; i < shards.Length; i++) shards[i] = new Shard();

            for (int i = 0; i < MoteCount; i++)
            {
                moteAcross[i] = (float)scatter.NextDouble();
                moteY[i] = (float)scatter.NextDouble() * TacetGame.ScreenH;
                moteSpeed[i] = 12f + (float)scatter.NextDouble() * 30f;
                moteSize[i] = 2f + (float)scatter.NextDouble() * 9f;
            }
        }

        //Sparks Spawn : count shards from (x, y). lean is -1 to fly mostly left, +1 right, 0 all round.
        public void SpawnSparks(float x, float y, int count, float lean)
        {
            for (int i = 0; i < sparks.Length && count > 0; i++)
            {
                if (sparks[i].On) continue;
                float angle = (float)(scatter.NextDouble() * MathHelper.TwoPi);
                float speed = 260f + (float)scatter.NextDouble() * 520f;
                sparks[i].On = true;
                sparks[i].X = x;
                sparks[i].Y = y;
                sparks[i].VX = (float)Math.Cos(angle) * speed + lean * 380f;
                sparks[i].VY = (float)Math.Sin(angle) * speed - 120f;
                sparks[i].Life = SparkLife * (0.6f + (float)scatter.NextDouble() * 0.4f);
                count--;
            }
        }

        //Flare Spawn : strength 0 to 1 sets how long and how bright the streak is
        public void SpawnFlare(float x, float y, float strength)
        {
            for (int i = 0; i < flares.Length; i++)
            {
                if (flares[i].On) continue;
                flares[i].On = true;
                flares[i].X = x;
                flares[i].Y = y;
                flares[i].Life = FlareLife;
                flares[i].Strength = strength;
                return;
            }
        }

        //Ripple Spawn : size is how wide the ring grows, dark says which side of the line it is on
        public void SpawnRipple(float x, float y, float size, bool dark)
        {
            for (int i = 0; i < ripples.Length; i++)
            {
                if (ripples[i].On) continue;
                ripples[i].On = true;
                ripples[i].Dark = dark;
                ripples[i].X = x;
                ripples[i].Y = y;
                ripples[i].Size = size;
                ripples[i].Life = RippleLife;
                return;
            }
        }

        //Hit Spawn : the burst for a stroke that just landed. A HESITATE has no stroke, so no burst.
        public void SpawnHit(float x, float y, Grade grade)
        {
            if (grade == Grade.None || grade == Grade.Hesitate) return;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].On) continue;
                hits[i].On = true;
                hits[i].X = x;
                hits[i].Y = y;
                hits[i].Life = HitLife;
                hits[i].Grade = grade;
                return;
            }
        }

        //Shatter Spawn : TACET's note, a ring of this radius, breaks into six arcs that fly apart
        public void SpawnShatter(float x, float y, float radius)
        {
            int count = 6;
            for (int i = 0; i < shards.Length && count > 0; i++)
            {
                if (shards[i].On) continue;
                float angle = MathHelper.TwoPi * count / 6f + (float)scatter.NextDouble() * 0.3f;
                float speed = 170f + (float)scatter.NextDouble() * 150f;
                shards[i].On = true;
                shards[i].X = x;
                shards[i].Y = y;
                shards[i].VX = (float)Math.Cos(angle) * speed;
                shards[i].VY = (float)Math.Sin(angle) * speed - 60f;
                shards[i].Angle = angle - ShardArc / 2f;          // the arc sits on the side it flies toward
                shards[i].Spin = ((float)scatter.NextDouble() - 0.5f) * 8f;
                shards[i].Radius = radius;
                shards[i].Life = ShardLife;
                count--;
            }
        }

        //Wave Spawn : dir +1 travels right (our sound), -1 travels left (TACET).
        //power 0..1 decides how big and how loud the arc looks.
        public void SpawnWave(float x, float y, float dir, float power)
        {
            SpawnWave(x, y, dir, power, WaveSpeed);
        }

        //Wave Spawn Fast : the same, with its own speed. The duel's answers use a very fast
        //one, so the band's sound reaches the line on the beat.
        public void SpawnWave(float x, float y, float dir, float power, float speed)
        {
            if (power < 0.25f) power = 0.25f;
            if (power > 1f) power = 1f;

            for (int i = 0; i < waves.Length; i++)
            {
                if (waves[i].On) continue;
                waves[i].On = true;
                waves[i].Spent = false;
                waves[i].X = x;
                waves[i].Y = y;
                waves[i].Radius = 80f + power * 70f;
                waves[i].Life = WaveLife;
                waves[i].Dir = dir;
                waves[i].Power = power;
                waves[i].Speed = speed;
                return;
            }
        }

        //Pop Spawn : text should come from NumberText or a fixed word, never built on the spot
        public void SpawnPop(float x, float y, string text, Color color, float scale)
        {
            for (int i = 0; i < pops.Length; i++)
            {
                if (pops[i].On) continue;
                pops[i].On = true;
                pops[i].X = x;
                pops[i].Y = y;
                pops[i].Life = PopLife;
                pops[i].Text = text;
                pops[i].Color = color;
                pops[i].Scale = scale;
                return;
            }
        }

        private void SpawnBurst(float x, float y, float power)
        {
            for (int i = 0; i < bursts.Length; i++)
            {
                if (bursts[i].On) continue;
                bursts[i].On = true;
                bursts[i].X = x;
                bursts[i].Y = y;
                bursts[i].Radius = 16f;
                bursts[i].Life = BurstLife;
                bursts[i].Power = power;
                return;
            }
        }

        public void Update(float dt)
        {
            Hit = false;

            for (int i = 0; i < waves.Length; i++)
            {
                Wave w = waves[i];
                if (!w.On) continue;

                if (!w.Spent)
                {
                    w.X += w.Dir * w.Speed * dt;
                    w.Radius += WaveGrow * dt;

                    //Line Reached : the leading edge of the arc has arrived at the middle
                    float front = w.X + w.Dir * w.Radius;
                    bool reached = w.Dir > 0f ? front >= LineX : front <= LineX;
                    if (reached)
                    {
                        w.Spent = true;
                        w.Life = Math.Min(w.Life, 0.22f);

                        Hit = true;
                        HitY = w.Y;
                        HitDir = w.Dir;
                        HitPower = w.Power;
                        SpawnBurst(LineX, w.Y, w.Power);
                    }
                }

                w.Life -= dt;
                if (w.Life <= 0f) w.On = false;
            }

            for (int i = 0; i < bursts.Length; i++)
            {
                Burst b = bursts[i];
                if (!b.On) continue;
                b.Radius += (160f + b.Power * 200f) * dt;
                b.Life -= dt;
                if (b.Life <= 0f) b.On = false;
            }

            for (int i = 0; i < pops.Length; i++)
            {
                Pop p = pops[i];
                if (!p.On) continue;
                p.Y -= 38f * dt;
                p.Life -= dt;
                if (p.Life <= 0f) p.On = false;
            }

            //Sparks : fly, slow down in the air, and fall
            for (int i = 0; i < sparks.Length; i++)
            {
                Spark k = sparks[i];
                if (!k.On) continue;
                k.X += k.VX * dt;
                k.Y += k.VY * dt;
                k.VX *= 1f - 2.5f * dt;
                k.VY += 900f * dt;
                k.Life -= dt;
                if (k.Life <= 0f) k.On = false;
            }

            for (int i = 0; i < flares.Length; i++)
            {
                if (!flares[i].On) continue;
                flares[i].Life -= dt;
                if (flares[i].Life <= 0f) flares[i].On = false;
            }

            for (int i = 0; i < ripples.Length; i++)
            {
                if (!ripples[i].On) continue;
                ripples[i].Life -= dt;
                if (ripples[i].Life <= 0f) ripples[i].On = false;
            }

            for (int i = 0; i < hits.Length; i++)
            {
                if (!hits[i].On) continue;
                hits[i].Life -= dt;
                if (hits[i].Life <= 0f) hits[i].On = false;
            }

            //Shards : fly out, slow down, fall a little and spin
            for (int i = 0; i < shards.Length; i++)
            {
                Shard s = shards[i];
                if (!s.On) continue;
                s.X += s.VX * dt;
                s.Y += s.VY * dt;
                s.VX *= 1f - 2f * dt;
                s.VY += 500f * dt;
                s.Angle += s.Spin * dt;
                s.Life -= dt;
                if (s.Life <= 0f) s.On = false;
            }

            //Motes : drift up forever, coming back in at the bottom
            for (int i = 0; i < MoteCount; i++)
            {
                moteY[i] -= moteSpeed[i] * dt;
                if (moteY[i] < -20f) moteY[i] = TacetGame.ScreenH + 20f;
            }
        }

        //Sparks Draw : each shard is a short streak pointing the way it flies
        public void DrawSparks(SpriteBatch sb)
        {
            for (int i = 0; i < sparks.Length; i++)
            {
                Spark k = sparks[i];
                if (!k.On) continue;
                float a = k.Life / SparkLife;
                Vector2 head = new Vector2(k.X, k.Y);
                Vector2 tail = new Vector2(k.X - k.VX * 0.025f, k.Y - k.VY * 0.025f);
                Gfx.Line(sb, tail, head, Palette.Ink * (0.5f * a), 4f);
                Gfx.Line(sb, tail, head, Palette.Highlight * a, 2f);
            }
        }

        //Flares Draw : a streak that snaps open and fades
        public void DrawFlares(SpriteBatch sb)
        {
            for (int i = 0; i < flares.Length; i++)
            {
                Flare f = flares[i];
                if (!f.On) continue;
                float t = f.Life / FlareLife;
                float open = 1f - t * t * 0.4f;
                Hollow.Flare(sb, f.X, f.Y, (220f + f.Strength * 620f) * open, t * (0.4f + 0.6f * f.Strength));
            }
        }

        //Hits Draw : t runs from 0 (just landed) to 1 (gone). "open" rushes out fast and then
        //slows down (ease out), which is what makes a burst feel like a hit and not a slide.
        //   PERFECT  two rings, a four point star and four specks flying out on the diagonals
        //   GOOD     one ring
        //   MISS     a ring broken into four pieces, sinking and grey
        public void DrawHits(SpriteBatch sb)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                HitBurst h = hits[i];
                if (!h.On) continue;
                float t = 1f - h.Life / HitLife;
                float open = 1f - (1f - t) * (1f - t);
                float a = 1f - t;

                if (h.Grade == Grade.Perfect)
                {
                    float r = 30f + open * 72f;
                    Gfx.CircleOutline(sb, h.X, h.Y, r, Palette.Highlight * a, 2f + 4f * a);
                    Gfx.CircleOutline(sb, h.X, h.Y, r * 0.62f, Palette.Paper * (0.7f * a), 1.5f);
                    NoteGlyph.Spark(sb, h.X, h.Y, 16f + open * 26f, Palette.Highlight * a);
                    for (int k = 0; k < 4; k++)
                    {
                        float angle = MathHelper.PiOver4 + k * MathHelper.PiOver2;
                        float reach = r * 1.15f;
                        Gfx.Diamond(sb, h.X + (float)Math.Cos(angle) * reach, h.Y + (float)Math.Sin(angle) * reach, 4f * a, Palette.Highlight * a);
                    }
                }
                else if (h.Grade == Grade.Good)
                {
                    float r = 26f + open * 46f;
                    Gfx.CircleOutline(sb, h.X, h.Y, r, Palette.Paper * (0.85f * a), 1.5f + 3f * a);
                }
                else
                {
                    float r = 36f;
                    float sink = t * 22f;
                    for (int k = 0; k < 4; k++)
                    {
                        float from = k * MathHelper.PiOver2 + 0.25f;
                        Gfx.Arc(sb, h.X, h.Y + sink, r, from, from + 1.0f, Palette.PaperDim * (0.8f * a), 2f);
                    }
                }
            }
        }

        //Shards Draw : each piece is a short arc of the ring it broke from, dark under light
        //so it reads on the bright stage as well as in TACET's dark
        public void DrawShards(SpriteBatch sb)
        {
            for (int i = 0; i < shards.Length; i++)
            {
                Shard s = shards[i];
                if (!s.On) continue;
                float a = s.Life / ShardLife;
                Gfx.Arc(sb, s.X, s.Y, s.Radius, s.Angle, s.Angle + ShardArc, Palette.Ink * (0.6f * a), 5f);
                Gfx.Arc(sb, s.X, s.Y, s.Radius, s.Angle, s.Angle + ShardArc, Palette.Highlight * a, 2.5f);
            }
        }

        //Ripples Draw : dark says which set to draw, so each side can be drawn at the right depth
        public void DrawRipples(SpriteBatch sb, bool dark)
        {
            for (int i = 0; i < ripples.Length; i++)
            {
                Ripple r = ripples[i];
                if (!r.On || r.Dark != dark) continue;
                float t = 1f - r.Life / RippleLife;             // 0 new .. 1 gone
                float rx = r.Size * (0.25f + 0.75f * t);
                Color ink = dark ? Palette.Highlight : Palette.Ink;
                Gfx.EllipseOutline(sb, r.X, r.Y, rx, rx * 0.24f, ink * (0.55f * (1f - t)), 2f);
                Gfx.EllipseOutline(sb, r.X, r.Y, rx * 0.6f, rx * 0.6f * 0.24f, ink * (0.3f * (1f - t)), 1f);
            }
        }

        //Motes Draw : across TACET's dark only, from edgeX to the right side of the screen.
        //danger 0 to 1 makes more of them show up as hollow rings.
        public void DrawMotes(SpriteBatch sb, float edgeX, float danger, float time)
        {
            float width = TacetGame.ScreenW - edgeX;
            if (width < 40f) return;

            for (int i = 0; i < MoteCount; i++)
            {
                float x = edgeX + 20f + moteAcross[i] * (width - 20f);
                float y = moteY[i];
                float flicker = 0.6f + 0.4f * (float)Math.Sin(time * 3f + i * 1.7f);

                //Hollow Rings : a few holes in the dark, more of them as TACET closes in
                if (i % 4 == 0 || (i % 4 == 1 && danger > 0.4f))
                    Hollow.Ring(sb, x, y, moteSize[i] * 1.4f, 1.5f, 0.55f * flicker);
                else
                    Gfx.DrawGlow(sb, x, y, moteSize[i] * 2.2f, Palette.Paper * (0.35f * flicker));
            }
        }

        //Waves Draw : each arc is drawn twice, dark then light, so it reads on the bright
        //stage and on TACET's black alike
        public void DrawWaves(SpriteBatch sb)
        {
            for (int i = 0; i < waves.Length; i++)
            {
                Wave w = waves[i];
                if (!w.On) continue;

                float a = Math.Min(1f, w.Life / WaveLife * 1.6f);
                float centre = w.Dir > 0f ? 0f : MathHelper.Pi;
                float open = 0.7f + w.Power * 0.45f;
                float thick = 5f + w.Power * 9f;

                Gfx.Arc(sb, w.X, w.Y, w.Radius, centre - open, centre + open, Palette.Ink * (0.7f * a), thick);
                Gfx.Arc(sb, w.X, w.Y, w.Radius, centre - open, centre + open, Palette.Highlight * a, thick * 0.4f);

                //Trailing Arcs : two fainter ones behind, so one sound reads as a swell
                Gfx.Arc(sb, w.X, w.Y, w.Radius - 30f, centre - open * 0.75f, centre + open * 0.75f, Palette.Ink * (0.30f * a), 3f);
                Gfx.Arc(sb, w.X, w.Y, w.Radius - 30f, centre - open * 0.75f, centre + open * 0.75f, Palette.Paper * (0.45f * a), 1.5f);
                Gfx.Arc(sb, w.X, w.Y, w.Radius - 58f, centre - open * 0.5f, centre + open * 0.5f, Palette.Paper * (0.25f * a), 1.5f);
            }
        }

        //Bursts Draw : the ring left where a wave hit the line, plus a flash along the line
        public void DrawBursts(SpriteBatch sb)
        {
            for (int i = 0; i < bursts.Length; i++)
            {
                Burst b = bursts[i];
                if (!b.On) continue;

                float a = b.Life / BurstLife;
                Gfx.CircleOutline(sb, b.X, b.Y, b.Radius, Palette.Highlight * (0.9f * a), 3f + b.Power * 3f);
                Gfx.CircleOutline(sb, b.X, b.Y, b.Radius * 0.6f, Palette.Paper * (0.5f * a), 2f);
                Gfx.DrawGlow(sb, b.X, b.Y, 60f + b.Power * 90f, Palette.Paper * (0.35f * a));

                //Spray : short strokes flying back from the line
                float spread = 40f + b.Power * 70f;
                Gfx.Rect(sb, b.X - 2, b.Y - spread * a, 4, spread * 2f * a, Palette.Highlight * (0.5f * a));
            }
        }

        //Pops Draw : white text with a black shadow, readable on any background
        public void DrawPops(SpriteBatch sb, SpriteFont font)
        {
            for (int i = 0; i < pops.Length; i++)
            {
                Pop p = pops[i];
                if (!p.On) continue;

                float a = Math.Min(1f, p.Life * 2.5f);
                Gfx.TextCentered(sb, font, p.Text, p.X + 2, p.Y + 2, Color.Black * (0.8f * a), p.Scale);
                Gfx.TextCentered(sb, font, p.Text, p.X, p.Y, p.Color * a, p.Scale);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < waves.Length; i++) waves[i].On = false;
            for (int i = 0; i < bursts.Length; i++) bursts[i].On = false;
            for (int i = 0; i < pops.Length; i++) pops[i].On = false;
            for (int i = 0; i < sparks.Length; i++) sparks[i].On = false;
            for (int i = 0; i < flares.Length; i++) flares[i].On = false;
            for (int i = 0; i < ripples.Length; i++) ripples[i].On = false;
            for (int i = 0; i < hits.Length; i++) hits[i].On = false;
            for (int i = 0; i < shards.Length; i++) shards[i].On = false;
        }
    }
}
