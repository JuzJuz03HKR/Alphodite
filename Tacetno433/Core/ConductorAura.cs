using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //ConductorAura (Beta1, 29 Oct, docs/CHARACTER_DESIGN.md 4) : a conductor's theme made visible round their painting, when
    //the gallery points at it and on their profile page. The user gave the three themes, the character design's pictures
    //showed the pieces.
    //   STORM   THE UNHEARING : rain blown sideways under dark clouds, and now and then lightning in his blue striking down
    //           behind the painting, the room flashing with it (no flash with LESS FLASHING)
    //   PRISM   FANNY MENDELSSOHN AND ??? : big shards of prism glass floating round her, every one turned to throw its light
    //           at her brother (BROTHER, a spot in the painting), the rainbow breaking along their edges, and a spotlight that
    //           always finds him
    //   HOLY    THE METRONOME : the room goes dark round him (he cannot see it) and a shaft of holy gold falls on him alone,
    //           dust turning in it
    //Nothing here draws a person : the brother is in the painting, the light only finds him (the art rule, CLAUDE.md 3).
    //Every piece lives in a pool made once, nothing is made while the page runs.
    //The page calls Update with the painting's rectangle and how far it is pointed at (0..1), then draws each layer where it
    //belongs : DrawBack behind every painting, DrawMiddle over the room and the other paintings but under this one, DrawFront
    //over it, DrawLights into the light map and DrawGlow into the glow. Numbers : Data/BattleRules.cs, UI Feel (AURA).
    public class ConductorAura
    {
        public readonly AuraKind Kind;
        private Rectangle frame;                      // the painting this frame
        private float power;                          // 0 = not there, 1 = pointed at
        private float time;
        private readonly Random dice;

        public ConductorAura(AuraKind kind, int seed)
        {
            Kind = kind;
            dice = new Random(seed);
            if (kind == AuraKind.Storm) MakeStorm();
            if (kind == AuraKind.Prism) MakePrism();
            if (kind == AuraKind.Holy) MakeHoly();
        }

        //Update : the painting's rectangle this frame and how far it is pointed at. Nothing moves while it is not there.
        public void Update(float dt, Rectangle frame, float power)
        {
            this.frame = frame;
            bool waking = this.power <= 0f && power > 0f;
            this.power = MathHelper.Clamp(power, 0f, 1f);
            if (this.power <= 0f) return;
            time += dt;
            if (Kind == AuraKind.Storm) UpdateStorm(dt, waking);
            if (Kind == AuraKind.Holy) UpdateHoly(dt);
        }

        //Draw Back : behind every painting (the storm's clouds and far rain, the shards behind her)
        public void DrawBack(SpriteBatch sb)
        {
            if (power <= 0f) return;
            if (Kind == AuraKind.Storm) DrawStormBack(sb);
            if (Kind == AuraKind.Prism) DrawShards(sb, false);
        }

        //Draw Middle : over the room and the other paintings, under this one (the lightning, the dark round THE METRONOME)
        public void DrawMiddle(SpriteBatch sb)
        {
            if (power <= 0f) return;
            if (Kind == AuraKind.Storm) DrawBolt(sb);
            if (Kind == AuraKind.Holy) Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Color.Black * (BattleRules.UiAuraHolyDark * power));
        }

        //Draw Front : over the painting (the nearest rain, the shards in front of her, the dust in the gold)
        public void DrawFront(SpriteBatch sb)
        {
            if (power <= 0f) return;
            if (Kind == AuraKind.Storm) DrawRain(sb, true);
            if (Kind == AuraKind.Prism) DrawShards(sb, true);
            if (Kind == AuraKind.Holy) DrawMotes(sb);
        }

        //Draw Lights : into the light map (the lightning's flash, the spotlight on the brother, the holy shaft)
        public void DrawLights(SpriteBatch sb)
        {
            if (power <= 0f) return;
            if (Kind == AuraKind.Storm) DrawFlash(sb);
            if (Kind == AuraKind.Prism) DrawSpotlight(sb);
            if (Kind == AuraKind.Holy) DrawShaft(sb);
        }

        //Draw Glow : added light on top (the bolt's glow, the light thrown at the brother, the gold rays)
        public void DrawGlow(SpriteBatch sb)
        {
            if (power <= 0f) return;
            if (Kind == AuraKind.Storm) DrawBoltGlow(sb);
            if (Kind == AuraKind.Prism) DrawThrownLight(sb);
            if (Kind == AuraKind.Holy) DrawRays(sb);
        }

        //======================================================================================
        //STORM : THE UNHEARING
        //======================================================================================

        private const float Wind = 0.3f;              // the rain leans this far sideways for every pixel it falls
        private const int BoltPoints = 17;            // the bolt's jagged line, 16 pieces (Jag halves it four times)
        private const int ForkPoints = 9;             // and each of its two forks, 8 pieces
        private const float BoltLife = 0.34f;         // a strike lasts this long (seconds)
        private const int Clouds = 5;

        private float[] dropX = new float[0];
        private float[] dropY = new float[0];
        private float[] dropSpeed = new float[0];
        private float[] dropLength = new float[0];
        private readonly Vector2[] bolt = new Vector2[BoltPoints];
        private readonly Vector2[] fork = new Vector2[ForkPoints];
        private readonly Vector2[] fork2 = new Vector2[ForkPoints];
        private float boltAge = 99f;                  // seconds since the last strike
        private float nextBolt;                       // seconds to the next one

        private void MakeStorm()
        {
            int n = Math.Max(0, BattleRules.UiAuraRain);
            dropX = new float[n];
            dropY = new float[n];
            dropSpeed = new float[n];
            dropLength = new float[n];
            for (int i = 0; i < n; i++)
            {
                dropX[i] = (float)dice.NextDouble() * (TacetGame.ScreenW + 400f);
                dropY[i] = (float)dice.NextDouble() * TacetGame.ScreenH;
                dropSpeed[i] = 900f + 500f * (float)dice.NextDouble();
                dropLength[i] = 16f + 22f * (float)dice.NextDouble();
            }
        }

        private void UpdateStorm(float dt, bool waking)
        {
            //Rain : falls and drifts left with the wind, and starts again over the top once it has left the screen
            for (int i = 0; i < dropX.Length; i++)
            {
                dropY[i] += dropSpeed[i] * dt;
                dropX[i] -= dropSpeed[i] * Wind * dt;
                if (dropY[i] > TacetGame.ScreenH + 40f)
                {
                    dropY[i] = -40f - 60f * (float)dice.NextDouble();
                    dropX[i] = (float)dice.NextDouble() * (TacetGame.ScreenW + 400f);
                }
            }

            //Lightning : the first strike comes soon after the painting is pointed at, the rest every UiAuraBoltEvery or so
            boltAge += dt;
            if (waking) nextBolt = 0.25f;
            nextBolt -= dt;
            if (nextBolt <= 0f && power > 0.5f) Strike();
        }

        //Strike : a new bolt from the top of the screen down to the floor just beside the painting, and two forks off it
        private void Strike()
        {
            float side = dice.NextDouble() < 0.5 ? -1f : 1f;
            float x = frame.Center.X + side * frame.Width * (0.56f + 0.3f * (float)dice.NextDouble());
            bolt[0] = new Vector2(x - side * 90f * (float)dice.NextDouble(), -20f);
            bolt[BoltPoints - 1] = new Vector2(x, frame.Bottom - frame.Height * 0.2f * (float)dice.NextDouble());
            Jag(bolt, 0, BoltPoints - 1, 100f);
            Fork(fork, 3 + dice.Next(4), side);
            Fork(fork2, 8 + dice.Next(4), -side);

            boltAge = 0f;
            nextBolt = BattleRules.UiAuraBoltEvery * (0.5f + (float)dice.NextDouble());
        }

        //Fork : a smaller jagged line off the bolt at point from, turned away from it toward side
        private void Fork(Vector2[] line, int from, float side)
        {
            line[0] = bolt[from];
            Vector2 rest = (bolt[BoltPoints - 1] - bolt[from]) * 0.4f;
            float turn = side * (0.45f + 0.35f * (float)dice.NextDouble());
            float cos = (float)Math.Cos(turn);
            float sin = (float)Math.Sin(turn);
            line[ForkPoints - 1] = line[0] + new Vector2(rest.X * cos - rest.Y * sin, rest.X * sin + rest.Y * cos);
            Jag(line, 0, ForkPoints - 1, 34f);
        }

        //Jag : ADVANCED PART (MIDPOINT DISPLACEMENT). The point halfway between two points is pushed sideways by a random
        //amount, then each half gets the same with half the push (a little more than half, 0.55), and so on : big kinks and
        //small ones on top of them, the way lightning looks. The pieces between a and b must be a power of two.
        private void Jag(Vector2[] line, int a, int b, float push)
        {
            if (b - a < 2) return;
            int mid = (a + b) / 2;
            Vector2 along = line[b] - line[a];
            Vector2 across = new Vector2(-along.Y, along.X);
            if (across != Vector2.Zero) across.Normalize();
            line[mid] = (line[a] + line[b]) * 0.5f + across * (push * ((float)dice.NextDouble() * 2f - 1f));
            Jag(line, a, mid, push * 0.55f);
            Jag(line, mid, b, push * 0.55f);
        }

        //Bolt Seen : how bright the bolt is now. It strikes, goes out for a blink, strikes again, then dies away, like a real
        //one. LESS FLASHING : one dimmer strike that only fades.
        private float BoltSeen()
        {
            if (boltAge >= BoltLife) return 0f;
            if (Settings.ReduceFlashing) return 0.6f * (1f - boltAge / BoltLife);
            if (boltAge < 0.06f) return 1f;
            if (boltAge < 0.1f) return 0.15f;
            return 1f - (boltAge - 0.1f) / (BoltLife - 0.1f);
        }

        //Flash Now : how hard the room flashes, only on the two strikes. LESS FLASHING : never.
        private float FlashNow()
        {
            if (Settings.ReduceFlashing) return 0f;
            if (boltAge < 0.06f) return 1f;
            if (boltAge >= 0.1f && boltAge < 0.16f) return 0.55f;
            return 0f;
        }

        //Storm Back : dark clouds drifting over the top, and the far rain
        private void DrawStormBack(SpriteBatch sb)
        {
            for (int k = 0; k < Clouds; k++)
            {
                float x = (k * 337f + time * (10f + 4f * k)) % (TacetGame.ScreenW + 700f) - 350f;
                float y = -40f + 60f * Hash(k, 1);
                Gfx.DrawGlow(sb, x, y, 280f + 140f * Hash(k, 2), Color.Black * (0.75f * power));
            }
            DrawRain(sb, false);
        }

        //Bolt : the strike itself, a blue edge round a white hot core, over the other paintings but behind his
        private void DrawBolt(SpriteBatch sb)
        {
            float seen = BoltSeen() * power;
            if (seen <= 0f) return;
            Polyline(sb, bolt, Palette.LightningBlue * (0.6f * seen), 7f);
            Polyline(sb, bolt, Palette.LightningBlueHot * seen, 3f);
            Polyline(sb, fork, Palette.LightningBlue * (0.5f * seen), 4f);
            Polyline(sb, fork, Palette.LightningBlueHot * (0.8f * seen), 1.5f);
            Polyline(sb, fork2, Palette.LightningBlue * (0.4f * seen), 3f);
            Polyline(sb, fork2, Palette.LightningBlueHot * (0.7f * seen), 1f);
        }

        //Rain : thin streaks leaning with the wind. One drop in five is near the camera, longer and brighter, and falls in
        //front of the painting (near = true), the rest behind it.
        private void DrawRain(SpriteBatch sb, bool near)
        {
            for (int i = 0; i < dropX.Length; i++)
            {
                bool close = i % 5 == 0;
                if (close != near) continue;
                float length = close ? dropLength[i] * 1.6f : dropLength[i];
                Color color = Palette.Paper * ((close ? 0.3f : 0.17f) * power);
                Gfx.Line(sb, dropX[i], dropY[i], dropX[i] + Wind * length, dropY[i] - length, color, 1f);
            }
        }

        //Bolt Glow : the bolt glowing blue along its length, and a burst where it strikes
        private void DrawBoltGlow(SpriteBatch sb)
        {
            float seen = BoltSeen() * power;
            if (seen <= 0f) return;
            for (int k = 0; k < BoltPoints; k += 2)
                Gfx.DrawGlow(sb, bolt[k].X, bolt[k].Y, 46f, Palette.LightningBlue * (0.4f * seen));
            Vector2 end = bolt[BoltPoints - 1];
            Gfx.DrawGlow(sb, end.X, end.Y, 120f, Palette.LightningBlueHot * (0.55f * seen));
        }

        //Flash : the whole room lit blue for a blink with each strike, brightest in the clouds it came from
        private void DrawFlash(SpriteBatch sb)
        {
            float flash = BattleRules.UiAuraBoltFlash * FlashNow() * power;
            if (flash <= 0f) return;
            Gfx.DrawGlow(sb, bolt[0].X, 0f, 620f, Palette.LightningBlueHot * flash);
            Gfx.DrawGlow(sb, frame.Center.X, frame.Center.Y, 520f, Color.White * (0.6f * flash));
        }

        //======================================================================================
        //PRISM : FANNY MENDELSSOHN AND ???
        //======================================================================================

        //Brother : where her brother stands in the painting (a share of its width and height). Every shard turns to throw
        //its light there and the spotlight finds him there. On her right until the painting exists, move it to him then.
        private const float BrotherX = 0.74f;
        private const float BrotherY = 0.42f;

        //Shard Places : where each shard floats, as a share of the painting's width (X) and height (Y), and its length in
        //pixels (Z). The last two cross the painting's edge and float in front of it.
        private static readonly Vector3[] ShardPlaces =
        {
            new Vector3(-0.06f, 0.2f, 116f),
            new Vector3(-0.03f, 0.74f, 94f),
            new Vector3(1.06f, 0.3f, 88f),
            new Vector3(1.04f, 0.82f, 112f),
            new Vector3(0.6f, -0.1f, 104f),
            new Vector3(0.12f, -0.02f, 72f),
            new Vector3(0.4f, 0.97f, 84f)
        };
        private const int FrontShards = 2;

        private Vector2[][] shardShape = new Vector2[0][];    // each shard's four corners round its middle, made once
        private float[] shardHue = new float[0];               // where in the rainbow its edge starts
        private readonly Vector2[] corners = new Vector2[4];   // one shard's corners on the screen, reused for each

        private void MakePrism()
        {
            int n = Math.Min(Math.Max(0, BattleRules.UiAuraShards), ShardPlaces.Length);
            shardShape = new Vector2[n][];
            shardHue = new float[n];
            for (int i = 0; i < n; i++)
            {
                //A long sliver with four corners, a little different each time but always bulging outward (FillQuad)
                float length = ShardPlaces[i].Z;
                float wide = length * (0.5f + 0.2f * (float)dice.NextDouble());
                shardShape[i] = new Vector2[]
                {
                    new Vector2(-length * 0.5f, wide * 0.18f * (float)dice.NextDouble()),
                    new Vector2(-length * (0.05f + 0.15f * (float)dice.NextDouble()), -wide * 0.5f),
                    new Vector2(length * 0.5f, -wide * 0.15f * (float)dice.NextDouble()),
                    new Vector2(length * (0.05f + 0.2f * (float)dice.NextDouble()), wide * 0.5f)
                };
                shardHue[i] = (float)dice.NextDouble();
            }
        }

        //Brother At : his spot on the screen this frame
        private Vector2 BrotherAt()
        {
            return new Vector2(frame.X + frame.Width * BrotherX, frame.Y + frame.Height * BrotherY);
        }

        //Shard At : where shard i floats now, bobbing slowly
        private Vector2 ShardAt(int i)
        {
            Vector3 place = ShardPlaces[i];
            return new Vector2(frame.X + frame.Width * place.X, frame.Y + frame.Height * place.Y + 6f * (float)Math.Sin(time * 0.7f + i * 1.3f));
        }

        //Shards : glass you can see through, white edges, one edge breaking into the rainbow, and a glint now and then.
        //Each lies across the line to the brother, like a mirror turned to him.
        private void DrawShards(SpriteBatch sb, bool front)
        {
            Vector2 brother = BrotherAt();
            for (int i = 0; i < shardShape.Length; i++)
            {
                bool before = i >= shardShape.Length - FrontShards;
                if (before != front) continue;
                Vector2 at = ShardAt(i);
                Vector2 to = brother - at;
                float face = (float)Math.Atan2(to.Y, to.X) + MathHelper.PiOver2 + 0.2f * (float)Math.Sin(time * 0.45f + i);
                float cos = (float)Math.Cos(face);
                float sin = (float)Math.Sin(face);
                for (int k = 0; k < 4; k++)
                {
                    Vector2 p = shardShape[i][k];
                    corners[k] = at + new Vector2(p.X * cos - p.Y * sin, p.X * sin + p.Y * cos);
                }

                float a = power * (front ? 0.75f : 1f);
                float hue = shardHue[i] + time * 0.12f;
                FillQuad(sb, corners, Color.Lerp(Palette.Paper, Palette.Prism(hue), 0.35f) * (0.14f * a));
                for (int k = 0; k < 4; k++)
                    Gfx.Line(sb, corners[k], corners[(k + 1) % 4], Palette.Paper * (0.55f * a), 1f);
                Gfx.Line(sb, corners[0], corners[2], Palette.Paper * (0.22f * a), 1f);         // the ridge down the crystal

                //Rainbow Edge : light breaking into its colours along one edge, three thin lines side by side
                Vector2 edge = corners[2] - corners[1];
                Vector2 step = new Vector2(-edge.Y, edge.X);
                if (step != Vector2.Zero) step.Normalize();
                for (int c = 0; c < 3; c++)
                {
                    Vector2 off = step * (1.5f * (c + 1));
                    Gfx.Line(sb, corners[1] + off, corners[2] + off, Palette.Prism(hue + c / 3f) * (0.75f * a), 1f);
                }

                //Glint : a cross of light that flares now and then on its sharpest corner (pixels, like the hits' sparks)
                float glint = (float)Math.Pow(Math.Max(0.0, Math.Sin(time * 1.3f + i * 2.1f)), 12.0) * a;
                if (glint > 0.02f)
                {
                    Vector2 g = new Vector2((float)Math.Floor(corners[2].X * 0.5f) * 2f, (float)Math.Floor(corners[2].Y * 0.5f) * 2f);
                    Gfx.Rect(sb, g.X - 6f, g.Y, 14f, 2f, Palette.Highlight * glint);
                    Gfx.Rect(sb, g.X, g.Y - 6f, 2f, 14f, Palette.Highlight * glint);
                }
            }
        }

        //Thrown Light : every shard throws a thin beam at the brother, split into the rainbow, shimmering
        private void DrawThrownLight(SpriteBatch sb)
        {
            Vector2 brother = BrotherAt();
            for (int i = 0; i < shardShape.Length; i++)
            {
                Vector2 at = ShardAt(i);
                float shimmer = 0.7f + 0.3f * (float)Math.Sin(time * 2.2f + i * 1.7f);
                float hue = shardHue[i] + time * 0.12f;
                for (int c = 0; c < 3; c++)
                {
                    Vector2 spread = new Vector2(0f, (c - 1) * 5f);
                    Gfx.Line(sb, at, brother + spread, Palette.Prism(hue + c / 3f) * (0.18f * shimmer * power), 1f);
                }
            }
            Gfx.DrawGlow(sb, brother.X, brother.Y, 70f, Color.Lerp(Color.White, Palette.Prism(time * 0.2f), 0.5f) * (0.35f * power));
        }

        //Spotlight : a cone of white from above that always finds the brother, and its pool under him
        private void DrawSpotlight(SpriteBatch sb)
        {
            Vector2 brother = BrotherAt();
            float foot = frame.Bottom + 10f;
            Cone(sb, brother.X + 80f, -30f, 26f, brother.X, foot, frame.Width * 0.24f, Color.White * (0.48f * power));
            Gfx.DrawGlow(sb, brother.X, brother.Y, 160f, Color.White * (0.36f * power));
        }

        //======================================================================================
        //HOLY : THE METRONOME
        //======================================================================================

        private float[] moteX = new float[0];         // across the shaft, -1 at its left edge, 1 at its right
        private float[] moteY = new float[0];         // down the shaft, 0 at the top, 1 at the floor
        private float[] moteSpeed = new float[0];

        private void MakeHoly()
        {
            int n = Math.Max(0, BattleRules.UiAuraMotes);
            moteX = new float[n];
            moteY = new float[n];
            moteSpeed = new float[n];
            for (int i = 0; i < n; i++)
            {
                moteX[i] = (float)dice.NextDouble() * 2f - 1f;
                moteY[i] = (float)dice.NextDouble();
                moteSpeed[i] = 0.02f + 0.04f * (float)dice.NextDouble();
            }
        }

        //Holy : the dust sinks slowly through the light and starts again at the top
        private void UpdateHoly(float dt)
        {
            for (int i = 0; i < moteY.Length; i++)
            {
                moteY[i] += moteSpeed[i] * dt;
                if (moteY[i] > 1f) { moteY[i] -= 1f; moteX[i] = (float)dice.NextDouble() * 2f - 1f; }
            }
        }

        //Shaft Half : how wide the shaft is (half) at a height 0..1 from its top to the floor
        private float ShaftHalf(float down)
        {
            return MathHelper.Lerp(frame.Width * 0.26f, frame.Width * 0.64f, down);
        }

        //Shaft : holy gold falling straight down on him alone, a brighter core, and the pool it leaves at his feet
        private void DrawShaft(SpriteBatch sb)
        {
            float cx = frame.Center.X;
            float foot = frame.Bottom + 30f;
            Cone(sb, cx, -30f, ShaftHalf(0f), cx, foot, ShaftHalf(1f), Palette.SpotHoly * (0.55f * power));
            Cone(sb, cx, -30f, ShaftHalf(0f) * 0.5f, cx, foot, ShaftHalf(1f) * 0.55f, Palette.SpotHolyHot * (0.3f * power));
            Gfx.DrawGlowBox(sb, new Rectangle((int)(cx - frame.Width * 0.9f), (int)(foot - 40f), (int)(frame.Width * 1.8f), 120), Palette.SpotHoly * (0.6f * power));
        }

        //Motes : specks of dust turning in the light, two pixels square, each catching it now and then
        private void DrawMotes(SpriteBatch sb)
        {
            float top = -30f;
            float foot = frame.Bottom + 30f;
            for (int i = 0; i < moteY.Length; i++)
            {
                float down = moteY[i];
                float sway = 0.12f * (float)Math.Sin(time * 0.5f + i * 2.3f);
                float x = frame.Center.X + (moteX[i] + sway) * ShaftHalf(down) * 0.9f;
                float y = MathHelper.Lerp(top, foot, down);
                float catchLight = 0.25f + 0.75f * (float)Math.Pow(0.5 + 0.5 * Math.Sin(time * (1.1f + moteSpeed[i] * 20f) + i), 4.0);
                float edge = Math.Min(1f, Math.Min(down, 1f - down) * 8f);
                Gfx.Rect(sb, (float)Math.Floor(x * 0.5f) * 2f, (float)Math.Floor(y * 0.5f) * 2f, 2f, 2f, Palette.SpotHolyHot * (catchLight * edge * power));
            }
        }

        //Rays : thin rays of gold fanning down from high above him, swaying slowly
        private void DrawRays(SpriteBatch sb)
        {
            const int count = 7;
            Vector2 from = new Vector2(frame.Center.X, frame.Y - frame.Height * 1.1f);
            for (int k = 0; k < count; k++)
            {
                float spread = (k - (count - 1) / 2f) / ((count - 1) / 2f);
                float angle = spread * 0.32f + 0.03f * (float)Math.Sin(time * 0.4f + k);
                float length = frame.Bottom - from.Y + 20f;
                Vector2 to = from + new Vector2((float)Math.Sin(angle), (float)Math.Cos(angle)) * length;
                float beat = 0.6f + 0.4f * (float)Math.Sin(time * 0.6f + k * 1.9f);
                Gfx.Line(sb, from, to, Palette.SpotHoly * (0.16f * beat * power), 2f + (k % 3));
            }
            Gfx.DrawGlow(sb, frame.Center.X, frame.Y + frame.Height * 0.3f, frame.Width * 0.8f, Palette.SpotHoly * (0.12f * power));
        }

        //======================================================================================
        //Shapes
        //======================================================================================

        //Polyline : straight pieces through a row of points
        private static void Polyline(SpriteBatch sb, Vector2[] points, Color color, float thickness)
        {
            for (int k = 1; k < points.Length; k++) Gfx.Line(sb, points[k - 1], points[k], color, thickness);
        }

        //Fill Quad : a four cornered shape filled in rows two pixels tall (PIXEL x2). It must bulge outward everywhere,
        //so every row crosses it in one piece : from the leftmost edge crossing to the rightmost.
        private static void FillQuad(SpriteBatch sb, Vector2[] p, Color color)
        {
            float top = Math.Min(Math.Min(p[0].Y, p[1].Y), Math.Min(p[2].Y, p[3].Y));
            float bottom = Math.Max(Math.Max(p[0].Y, p[1].Y), Math.Max(p[2].Y, p[3].Y));
            for (float y = (float)Math.Floor(top * 0.5f) * 2f; y < bottom; y += 2f)
            {
                float row = y + 1f;
                float left = float.MaxValue;
                float right = float.MinValue;
                for (int k = 0; k < 4; k++)
                {
                    Vector2 a = p[k];
                    Vector2 b = p[(k + 1) % 4];
                    if ((row < a.Y) == (row < b.Y)) continue;             // this edge does not cross the row
                    float x = a.X + (row - a.Y) / (b.Y - a.Y) * (b.X - a.X);
                    left = Math.Min(left, x);
                    right = Math.Max(right, x);
                }
                if (right > left) Gfx.Rect(sb, left, y, right - left, 2f, color);
            }
        }

        //Cone : a beam of light from a narrow top to a wide foot, in rows four pixels tall (the light map is soft anyway)
        private static void Cone(SpriteBatch sb, float topX, float topY, float topHalf, float footX, float footY, float footHalf, Color color)
        {
            for (float y = topY; y < footY; y += 4f)
            {
                float t = (y - topY) / (footY - topY);
                float x = MathHelper.Lerp(topX, footX, t);
                float half = MathHelper.Lerp(topHalf, footHalf, t);
                Gfx.Rect(sb, x - half, y, half * 2f, 4f, color);
            }
        }

        //Hash : a steady random 0..1 from two whole numbers (the same trick as TacetVoid)
        private static float Hash(int a, int b)
        {
            float h = (float)Math.Sin(a * 12.9898f + b * 78.233f) * 43758.547f;
            return h - (float)Math.Floor(h);
        }
    }
}
