using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //TacetVoid (Beta1 round 4) : TACET's dark on the battle page. A tear in space behind the enemies (a black hole)
    //pours its dark over the stage floor towards the camera, so the silence swallows the stage from the enemies'
    //side (docs/BATTLE_SCENE_2_3D_CHANGE.md 12.4).
    //   SHARE    0 = TACET beaten, 1 = the band beaten. How big the hole is and how far the pool reaches.
    //            It follows the balance of life on a spring : a hit throws it and it swings back.
    //   HOLE     black, a white rim, rays, a tilted ring of light (DISC), broken rings drifting out (ARCS)  (Void.fx)
    //   POOL     the dark on the floor, its front edge a waveform with a white seam                         (Void.fx)
    //   CRACKS   white lightning breaking out of the rim and the seam, drawn here as lines
    //   DEBRIS   bits of the stage turning around the hole, slowly drawn in
    //   ENGULF   past a point the dark creeps in from the edges of the screen                               (Post.fx)
    //Without shaders the old leaning rift (TacetField.DrawSlanted) stands in, the rest of the battle is the same.
    //Every number is in Data/BattleRules.cs, Stage Void.
    public class TacetVoid
    {
        //SHARE : where the dark stands, on a spring
        public float Share = -1f;          // -1 = not placed yet
        private float speed;

        //Shapes this frame, in screen pixels with the camera counted (Place)
        public Vector2 Hole;
        public float Radius;
        public Vector2 PoolBase;
        public Vector2 PoolSize;
        private float spike;
        private float horizon;
        private float time;
        private float pulse;               // a hit makes the hole and the waveform jump, then it dies away

        //SHOCK WAVE : the ring of the last hit
        private Vector2 waveAt;
        private float waveAge = 99f;
        private float wavePower;

        //CRACKS : lightning out of the rim or the seam, kept in a fixed pool (no garbage while fighting)
        private const int CrackPoints = 9;
        private struct Crack
        {
            public bool OnRim;             // out of the hole's rim, or out of the seam on the floor
            public float Anchor;           // where on it : an angle on the rim, a column on the seam
            public Vector2[] Points;       // the jagged line, as steps away from the anchor
            public float Life;
            public float Length;
        }
        private readonly Crack[] cracks = new Crack[16];
        private readonly Random random = new Random(433);
        private float crackClock;

        //Shader Settings : the shader's numbers, found once by name
        private EffectParameter pNoise, pScreen, pHole, pRadius, pPoolBase, pPoolSize, pSpike, pSwirl, pDanger, pTime, pWave;

        public TacetVoid()
        {
            for (int i = 0; i < cracks.Length; i++) cracks[i].Points = new Vector2[CrackPoints];
        }

        //Danger : how close the band is to losing, 0..1 (the dark gets wilder)
        public float Danger
        {
            get { return MathHelper.Clamp(Share * 1.4f - 0.6f, 0f, 1f); }
        }

        //ENGULF : 0..1, how far the dark has crept in from the screen's edges
        public float Engulf
        {
            get
            {
                float from = BattleRules.StageVoidEngulfFrom;
                return MathHelper.Clamp((Share - from) / (1f - from), 0f, 1f);
            }
        }

        //Reset : a new fight, the dark is placed again on the first update
        public void Reset()
        {
            Share = -1f;
            speed = 0f;
            pulse = 0f;
            waveAge = 99f;
            for (int i = 0; i < cracks.Length; i++) cracks[i].Life = 0f;
        }

        //======================================================================================
        //Update
        //======================================================================================

        //Update : target is where the balance of life puts the SHARE
        public void Update(float dt, float target)
        {
            time += dt;
            if (Share < 0f) Share = target;
            float pull = (target - Share) * 16f - speed * 6f;
            speed += pull * dt;
            Share += speed * dt;

            pulse = Math.Max(0f, pulse - dt * 2.2f);
            waveAge += dt;

            for (int i = 0; i < cracks.Length; i++)
                if (cracks[i].Life > 0f) cracks[i].Life -= dt;

            //CRACKS : a few now and then, more when the band is close to losing
            crackClock -= dt * BattleRules.StageVoidCracks * (0.5f + Danger * 1.5f);
            if (crackClock <= 0f)
            {
                crackClock += 0.5f + (float)random.NextDouble();
                if (random.Next(3) == 0) SpawnCrack(false, PoolBase.X + ((float)random.NextDouble() * 2f - 1f) * PoolSize.X * 0.8f, 1f);
                else SpawnCrack(true, (float)(random.NextDouble() * MathHelper.TwoPi), 1f);
            }
        }

        //Hit : a blow lands at a point on the screen. The band's blows throw the dark back, TACET's throw it forward.
        public void Hit(Vector2 at, bool bandHits, float power)
        {
            speed += (bandHits ? -1f : 1f) * BattleRules.StageVoidKick * power;
            pulse = Math.Min(1f, pulse + 0.6f * power);
            waveAt = at;
            waveAge = 0f;
            wavePower = MathHelper.Clamp(power, 0f, 1.2f);

            //The band's blow cracks the rim on the side it came from, TACET's cracks the seam towards the band
            int count = 2 + (int)(power * 3f);
            float toward = (float)Math.Atan2(at.Y - Hole.Y, at.X - Hole.X);
            for (int i = 0; i < count; i++)
            {
                float jitter = (float)random.NextDouble() * 2f - 1f;
                if (bandHits) SpawnCrack(true, toward + jitter * 1.2f, 1f + power * 0.5f);
                else SpawnCrack(false, at.X + jitter * 220f, 1f + power * 0.5f);
            }
        }

        //Spawn Crack : a new jagged line, in the slot of the one nearest its end
        private void SpawnCrack(bool onRim, float anchor, float size)
        {
            int slot = 0;
            for (int i = 1; i < cracks.Length; i++)
                if (cracks[i].Life < cracks[slot].Life) slot = i;

            //Heading : straight out of the rim, or out of the seam (mostly towards the camera)
            float heading;
            if (onRim) heading = anchor;
            else
            {
                float dx = (anchor - PoolBase.X) / Math.Max(1f, PoolSize.X);
                heading = MathHelper.PiOver2 - MathHelper.Clamp(dx, -1f, 1f) * 1.1f;
            }

            //A random walk : each step turns a little and goes a little further
            Vector2 at = Vector2.Zero;
            Vector2[] points = cracks[slot].Points;
            for (int i = 0; i < CrackPoints; i++)
            {
                heading += ((float)random.NextDouble() - 0.5f) * 1.3f;
                float step = (5f + (float)random.NextDouble() * 9f) * size;
                at += new Vector2((float)Math.Cos(heading), (float)Math.Sin(heading)) * step;
                points[i] = at;
            }

            cracks[slot].OnRim = onRim;
            cracks[slot].Anchor = anchor;
            cracks[slot].Length = 0.25f + (float)random.NextDouble() * 0.35f;
            cracks[slot].Life = cracks[slot].Length;
        }

        //======================================================================================
        //Shapes
        //======================================================================================

        //Place : the shapes for this frame. cam is the battle camera, horizonY the line where the floor starts.
        //The hole is far behind the enemies and moves with the camera less than the floor does (PARALLAX).
        public void Place(Vector2 cam, float horizonY)
        {
            float s = MathHelper.Clamp(Share, -0.1f, 1.1f);
            float floorFollow = BattleRules.StageParallax[2];
            horizon = horizonY - cam.Y * floorFollow;

            Hole = new Vector2(BattleRules.StageVoidX, BattleRules.StageVoidY) - cam * BattleRules.StageVoidParallax;
            Radius = MathHelper.Lerp(BattleRules.StageVoidRadiusMin, BattleRules.StageVoidRadiusMax, s) * (1f + 0.12f * pulse);
            PoolBase = new Vector2(BattleRules.StageVoidX - cam.X * floorFollow, horizon);
            PoolSize = new Vector2(MathHelper.Lerp(BattleRules.StagePoolWideMin, BattleRules.StagePoolWideMax, s),
                                   MathHelper.Lerp(BattleRules.StagePoolDeepMin, BattleRules.StagePoolDeepMax, s));
            spike = BattleRules.StageVoidSpike + BattleRules.StageVoidSpikeDanger * Danger + 26f * pulse;
        }

        //Pool Edge At : the row where the pool ends at column x, without its waveform (the same ellipse as Void.fx)
        public float PoolEdgeAt(float x)
        {
            float dx = (x - PoolBase.X) / Math.Max(1f, PoolSize.X);
            float k = 1f - dx * dx;
            return PoolBase.Y + (k > 0f ? PoolSize.Y * (float)Math.Sqrt(k) : 0f);
        }

        //In Dark : 0..1, how deep a point on the floor stands in the pool (for shading the enemies)
        public float InDark(Vector2 feet)
        {
            return MathHelper.Clamp((PoolEdgeAt(feet.X) - feet.Y) / 70f, 0f, 1f);
        }

        //======================================================================================
        //Draw
        //======================================================================================

        //Draw Scene : BACK (the backdrop and the floor) into the SCENE through the bend, then the dark.
        //Without shaders : BACK as it is, then the old leaning rift.
        public void DrawScene(GraphicsDevice device, SpriteBatch sb, Texture2D back)
        {
            if (Fx.VoidReady)
            {
                Effect e = Fx.Void;
                Setup(e);
                e.CurrentTechnique = e.Techniques["Void"];
                sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp, null, null, e);
                device.SamplerStates[1] = SamplerState.LinearWrap;
                sb.Draw(back, Vector2.Zero, Color.White);
                sb.End();
                device.Textures[1] = null;
                return;
            }

            sb.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp);
            sb.Draw(back, Vector2.Zero, Color.White);
            sb.End();

            float edgeX = MathHelper.Lerp(BattleRules.StageRiftBeaten, BattleRules.StageRiftWinning, MathHelper.Clamp(Share, -0.1f, 1.1f));
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);
            TacetField.DrawSlanted(sb, edgeX, BattleRules.StageRiftSlant, horizon, time, Danger, WaveStrength(), waveAt.Y);
            sb.End();
        }

        //Draw Glow : the light of the dark into the GLOW (the target is set and cleared already), then the cracks
        public void DrawGlow(GraphicsDevice device, SpriteBatch sb, Texture2D back)
        {
            if (Fx.VoidReady)
            {
                Effect e = Fx.Void;
                Setup(e);
                e.CurrentTechnique = e.Techniques["VoidGlow"];
                sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp, null, null, e);
                device.SamplerStates[1] = SamplerState.LinearWrap;
                sb.Draw(back, Vector2.Zero, Color.White);
                sb.End();
                device.Textures[1] = null;
            }
            else return;                                   // the old rift has its own light, and no hole to crack

            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp);
            DrawCracks(sb);
            DrawDebris(sb, true);
            sb.End();
        }

        //Setup : hand the shapes to the shader
        private void Setup(Effect e)
        {
            if (pHole == null)
            {
                pNoise = e.Parameters["NoiseTexture"];
                pScreen = e.Parameters["Screen"];
                pHole = e.Parameters["Hole"];
                pRadius = e.Parameters["Radius"];
                pPoolBase = e.Parameters["PoolBase"];
                pPoolSize = e.Parameters["PoolSize"];
                pSpike = e.Parameters["Spike"];
                pSwirl = e.Parameters["Swirl"];
                pDanger = e.Parameters["Danger"];
                pTime = e.Parameters["Time"];
                pWave = e.Parameters["Wave"];
            }
            pNoise.SetValue(Fx.Noise);
            pScreen.SetValue(new Vector2(TacetGame.ScreenW, TacetGame.ScreenH));
            pHole.SetValue(Hole);
            pRadius.SetValue(Radius);
            pPoolBase.SetValue(PoolBase);
            pPoolSize.SetValue(PoolSize);
            pSpike.SetValue(spike);
            pSwirl.SetValue(BattleRules.StageVoidSwirl * (1f + 0.5f * Danger + 0.6f * pulse));
            pDanger.SetValue(Danger);
            pTime.SetValue(time);
            pWave.SetValue(new Vector4(waveAt.X, waveAt.Y, waveAge * BattleRules.StageVoidWaveSpeed, WaveStrength()));
        }

        //Wave Strength : the shock wave fades as it runs out
        private float WaveStrength()
        {
            float left = 1f - waveAge / BattleRules.StageVoidWaveTime;
            return left > 0f ? wavePower * left * left : 0f;
        }

        //Draw Lights : into the LIGHT MAP. The hole lights what stands in front of it, the seam lights the floor.
        public void DrawLights(SpriteBatch sb)
        {
            Gfx.DrawGlow(sb, Hole.X, Hole.Y, Radius * 2.8f, Color.White * (BattleRules.StageVoidLight * (1f + 0.6f * pulse)));
            float from = Math.Max(-40f, PoolBase.X - PoolSize.X);
            float to = Math.Min(TacetGame.ScreenW + 40f, PoolBase.X + PoolSize.X);
            for (float x = from; x <= to; x += 90f)
            {
                float y = PoolEdgeAt(x);
                if (y > TacetGame.ScreenH + 60f) continue;
                Gfx.DrawGlow(sb, x, y, 90f, Color.White * BattleRules.StageVoidSeamLight);
            }
        }

        //Draw Cracks : each one grows out fast, flickers, and is gone
        private void DrawCracks(SpriteBatch sb)
        {
            for (int i = 0; i < cracks.Length; i++)
            {
                Crack c = cracks[i];
                if (c.Life <= 0f) continue;
                float age = 1f - c.Life / c.Length;
                float grown = Math.Min(1f, age / 0.25f) * CrackPoints;
                float flicker = Hash(i, (int)(time * 30f)) > 0.3f ? 1f : 0.45f;
                float a = flicker * Math.Min(1f, c.Life / (c.Length * 0.5f));

                Vector2 root;
                if (c.OnRim) root = Hole + new Vector2((float)Math.Cos(c.Anchor), (float)Math.Sin(c.Anchor)) * Radius;
                else root = new Vector2(c.Anchor, PoolEdgeAt(c.Anchor));

                Vector2 from = Snap(root);
                for (int k = 0; k < CrackPoints && k < grown; k++)
                {
                    Vector2 next = Snap(root + c.Points[k]);
                    Gfx.Line(sb, from, next, Color.White * a, k < CrackPoints / 2 ? 2f : 1f);
                    from = next;
                }
            }
        }

        //Draw Debris : bits turning around the hole on the plane of its DISC, slowly drawn in, then gone.
        //bright = the few that shine (into the GLOW), otherwise the dark chunks (into the SCENE).
        public void DrawDebris(SpriteBatch sb, bool bright)
        {
            if (!Fx.VoidReady) return;                     // no hole without the shader
            int count = BattleRules.StageVoidDebris;
            for (int i = 0; i < count; i++)
            {
                bool shines = i % 4 == 0;
                if (shines != bright) continue;

                float a = Hash(i, 1);
                float b = Hash(i, 2);
                float c = Hash(i, 3);
                float cycle = (time * (0.025f + 0.035f * c) * (1f + Danger) + a) % 1f;    // 0 far out, 1 at the rim
                float orbit = Radius * (1.25f + 1.7f * (1f - cycle));
                float angle = a * MathHelper.TwoPi + time * (0.18f + 0.3f * b);
                float sin = (float)Math.Sin(angle);
                Vector2 local = new Vector2((float)Math.Cos(angle) * orbit, sin * orbit * 0.22f);
                if (sin < 0f && local.Length() < Radius) continue;                       // behind the hole

                float fade = Math.Min(1f, cycle * 6f) * Math.Min(1f, (1f - cycle) * 5f);
                Vector2 at = Snap(Hole + local);
                if (bright) Gfx.Rect(sb, at.X, at.Y, 2f, 2f, Color.White * (0.7f * fade));
                else
                {
                    float size = 2f + 2f * (int)(c * 3f);
                    Gfx.Rect(sb, at.X, at.Y, size, size * (b > 0.5f ? 1f : 0.5f), new Color(84, 84, 90) * fade);
                }
            }
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
