using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //Figure (Beta1 1 Nov, MATH ART, docs/BETA1_DIRECTION.md 31) : a fighter drawn by the code from shapes, in place of a
    //picture the team will not draw. The user : the art as math art / geometry art in a few colours, things that react to
    //force (USER_BRIEFS 13.78 13.79, the reference is Worldless : a glowing core, a weapon, floating parts).
    //
    //   LEVEL 1   the Worldless way : no body, a glowing core for a head, the weapon, a few floating parts
    //   LEVEL 2   a simple body (head, chest, arms, legs, hair, a coat) holding the same weapon. The band is seen from
    //             behind like Chaos Zero Nightmare. F4 in a fight swaps the two levels (Figure.Level) so both can be seen.
    //   SPRINGS   every part follows where the pose wants it on a spring, so it lags, passes and settles by itself
    //             (FOLLOW THROUGH). A hit is a push on the springs' speed (KNOCK).
    //   CHAINS    hair, coat tails, ribbons and tails are ropes of points (the verlet rope of Core/Curtain.cs) that swing
    //             with the body, fall with gravity and blow in the WIND of the baton
    //   SHARDS    a beaten enemy breaks into pieces that fall, bounce on the floor and are pulled into TACET's hole
    //   LOOK      the band in smooth lines and its own colour (BAND LOOK), TACET black with white edges in blocks of 2 game
    //             pixels (PIXEL LOOK, drawn small then blown up by Screens/BattleScreen.Stage.cs), the user's pick (13.79)
    //
    //The figure only shows, the battle rules never read it (Battle/TurnBattle.cs). Screens/BattleScreen.Stage.cs tells it
    //where it stands, what it plays and how hard it was hit. Every number is in BattleRules, Figure Feel.
    //The files : Figure.cs (this one, the machinery), Figure.Band.cs (the five legends), Figure.Tacet.cs (TACET and bosses).
    public partial class Figure
    {
        //FIGURE LEVEL : 1 = shapes only, 2 = a simple body (F4 in a fight, BattleScreen.cs)
        public static int Level = BattleRules.FigureLevelStart;

        public enum Sort { Musician, Enemy, Boss }

        public readonly Sort Of;
        public readonly string Key;          // the design : a legend ("paganini"), a look ("hush") or a boss ("requiem")
        public Color Tint = Color.White;     // the band's own colour (BattleScreen SpotOf), white for TACET
        public Color Hot = Color.White;      //   and its bright core

        //Where : the stage sets these every frame before Update and Draw
        public Vector2 Feet;                 // the middle of the feet on the floor, page pixels
        public float Height = 512f;          // how tall its box is (the designs are drawn in shares of this)
        public float Facing = 1f;            // 1 faces right (the band), -1 faces left (TACET)
        public Vector2 Hole;                 // TACET's hole, where a beaten enemy's shards are pulled (page pixels)

        //Move : what it is playing now
        private string move = "idle";
        private float moveTime;              // seconds into the move
        private Flick way = Flick.None;      // ACTION : a skill's blow cuts the way the baton swung
        private bool down;                   // KO or SILENCED : it stays down
        private float clock;                 // its own clock (breath, sway)
        private readonly float seed;         // each figure moves a little out of step with the others
        private Vector2 lastFeet;
        private bool placed;                 // its parts have been put where it stands at least once

        public Figure(Sort of, string key, int seedNumber)
        {
            Of = of;
            Key = key;
            seed = (seedNumber * 0.618034f) % 1f * 10f;
            Facing = of == Sort.Musician ? 1f : -1f;
            SetUpBody();
        }

        //======================================================================================
        //Physics parts
        //======================================================================================

        //Point : a part that follows where the pose wants it, on a spring.
        //ADVANCED PART : a damped spring. The pull grows with the distance (stiffness squared), the friction with the speed
        //(twice the stiffness times "damping"). Damping 1 arrives without passing (critical), below 1 it passes and comes
        //back (BOUNCE). It moves in small steps (at most 1/120 of a second) so a stiff spring never blows up on a slow frame.
        private struct Point
        {
            public Vector2 At;
            public Vector2 Speed;

            public void Follow(Vector2 want, float stiffness, float damping, float dt)
            {
                int steps = Math.Max(1, (int)Math.Ceiling(dt * 120f));
                float h = dt / steps;
                for (int i = 0; i < steps; i++)
                {
                    Vector2 pull = (want - At) * (stiffness * stiffness) - Speed * (2f * stiffness * damping);
                    Speed += pull * h;
                    At += Speed * h;
                }
            }
        }

        //Turn : an angle on a spring (a weapon's angle), the same spring as Point for one number
        private struct Turn
        {
            public float At;
            public float Speed;

            public void Follow(float want, float stiffness, float damping, float dt)
            {
                int steps = Math.Max(1, (int)Math.Ceiling(dt * 120f));
                float h = dt / steps;
                for (int i = 0; i < steps; i++)
                {
                    float pull = (want - At) * (stiffness * stiffness) - Speed * (2f * stiffness * damping);
                    Speed += pull * h;
                    At += Speed * h;
                }
            }
        }

        //Chain : hair, a coat's tail, a ribbon, a tail. Points joined by sticks of one length, the first held by the body.
        //ADVANCED PART : VERLET, the way Core/Curtain.cs hangs its cloth. A point remembers where it was, and where it is
        //minus where it was is its speed, nothing else is kept. Each step every free point goes on by that speed (less a
        //little DRAG), plus gravity and the wind. Then each stick is put back to its length, three times over (the
        //CONSTRAINTS) : that is what makes it a rope and not loose dust. The old speed is scaled by the new step's length
        //over the last one, so when the world slows down (QTE FOCUS) the hair slows down with it.
        private class Chain
        {
            public readonly Vector2[] At;
            private readonly Vector2[] was;
            public readonly int Count;
            public float Link;
            private float lastDt = 1f / 60f;

            public Chain(int count, float link)
            {
                Count = Math.Max(2, count);
                Link = link;
                At = new Vector2[Count];
                was = new Vector2[Count];
            }

            //Place : laid straight from the root the given way, still
            public void Place(Vector2 root, Vector2 way)
            {
                for (int i = 0; i < Count; i++)
                {
                    At[i] = root + way * (Link * i);
                    was[i] = At[i];
                }
            }

            //Carry : moved as a whole (the figure jumped to a new place), its shape and speed kept
            public void Carry(Vector2 by)
            {
                for (int i = 0; i < Count; i++)
                {
                    At[i] += by;
                    was[i] += by;
                }
            }

            public void Step(Vector2 root, Vector2 pull, float dt, float drag)
            {
                if (dt <= 0f) return;
                float k = MathHelper.Clamp(dt / lastDt, 0f, 2f);
                lastDt = dt;
                At[0] = root;
                was[0] = root;
                for (int i = 1; i < Count; i++)
                {
                    Vector2 now = At[i];
                    At[i] += (At[i] - was[i]) * (k * (1f - drag)) + pull * (dt * dt);
                    was[i] = now;
                }
                for (int pass = 0; pass < 3; pass++)
                {
                    At[0] = root;
                    for (int i = 0; i < Count - 1; i++)
                    {
                        Vector2 d = At[i + 1] - At[i];
                        float length = d.Length();
                        if (length < 0.0001f) continue;
                        float off = (length - Link) / length;
                        if (i == 0) At[1] -= d * off;                          // the root is held, only the next one moves
                        else
                        {
                            At[i] += d * (0.5f * off);
                            At[i + 1] -= d * (0.5f * off);
                        }
                    }
                }
            }

            //Push : a hit's push on every free point (more at the loose end)
            public void Push(Vector2 speed, float dt)
            {
                for (int i = 1; i < Count; i++) was[i] -= speed * (dt * i / (Count - 1));
            }
        }

        //Shard : a piece of a beaten enemy (or a loose bit of a figure)
        private struct Shard
        {
            public Vector2 At;
            public Vector2 Speed;
            public float Angle;
            public float Spin;
            public float Size;
            public int Shape;              // 0 a diamond, 1 a box, 2 a sliver
        }

        //======================================================================================
        //Moves
        //======================================================================================

        //Play : start a move. The same names as the team's animations (Screens/BattleScreen.Stage.cs PlayArt) :
        //idle attack skill parry hurt ko enter death. A move it has no pose for keeps the one it has.
        public void Play(string name, Flick swing)
        {
            if (name == "idle")
            {
                if (!down) move = "idle";
                return;
            }
            if (name == "hurt")
            {
                Knock(BattleRules.FigureKnock);
                if (move == "idle") { move = "hurt"; moveTime = 0f; }
                return;
            }
            if (name == "ko" || name == "death")
            {
                down = true;
                move = name;
                moveTime = 0f;
                if (name == "death") Shatter();
                return;
            }
            if (down) return;
            move = name == "skill" ? "attack" : name;            // a skill's blow is an attack's, the way the baton went
            way = swing;
            moveTime = 0f;
            if (name == "enter") Knock(BattleRules.FigureKnock * 0.35f);
        }

        //Up : back on its feet (a heal from zero, a new fight)
        public void Up()
        {
            if (!down) return;
            down = false;
            move = "idle";
            shardCount = 0;
        }

        public bool IsDown
        {
            get { return down; }
        }

        //Knock : a push on every part, away from whoever hit it (TACET hits the band from the right, the band hits TACET from
        //the left), a little up, never quite the same twice
        private void Knock(float power)
        {
            float scale = Height / 512f;
            for (int i = 0; i < body.Length; i++)
            {
                float jitter = 0.7f + 0.6f * Hash(i, (int)(clock * 10f));
                body[i].Speed += new Vector2(-Facing * power * jitter, -power * 0.25f * jitter) * scale;
            }
            weapon.Speed += -Facing * 9f;
            for (int b = 0; b < bits.Length; b++)                            // the floating bits fly furthest
            {
                float a = Hash(b + 11, (int)(clock * 10f)) * MathHelper.TwoPi;
                bits[b].Speed += new Vector2(-Facing * power * 1.4f + (float)Math.Cos(a) * power * 0.8f, (float)Math.Sin(a) * power * 0.8f) * scale;
            }
            for (int c = 0; c < chainCount; c++) chains[c].Push(new Vector2(-Facing * power * 0.6f, -power * 0.2f) * scale, 1f / 60f);
        }

        //======================================================================================
        //Update
        //======================================================================================

        //Update : the move's clock, the pose it wants, the springs, the chains, the shards.
        //wind is the push of the air in page pixels a second squared (the baton's swing, Screens/BattleScreen.Stage.cs).
        public void Update(float dt, Vector2 wind)
        {
            if (dt <= 0f) return;
            dt = Math.Min(dt, 1f / 30f);                                  // a slow frame must not throw the springs
            clock += dt;
            moveTime += dt;

            //CARRY : a big jump in one frame (a new place, the first frame) takes every part with it
            if (!placed)
            {
                PlaceAll();
                placed = true;
            }
            else
            {
                Vector2 jump = Feet - lastFeet;
                if (jump.LengthSquared() > BattleRules.FigureCarry * BattleRules.FigureCarry) CarryAll(jump);
            }
            lastFeet = Feet;

            if (move == "attack" && moveTime > BattleRules.TurnStrikeTime) move = "idle";
            if (move == "parry" && moveTime > BattleRules.FigureParryTime) move = "idle";
            if (move == "hurt" && moveTime > BattleRules.StageShiverTime) move = "idle";
            if (move == "enter" && moveTime > BattleRules.StageSwapTime) move = "idle";

            Pose();
            float scale = Height / 512f;
            float stiff = BattleRules.FigureSpring;
            for (int i = 0; i < body.Length; i++)
            {
                float loose = bodyLoose[i];
                body[i].Follow(want[i], stiff * MathHelper.Lerp(1f, BattleRules.FigureLooseShare, loose),
                               MathHelper.Lerp(BattleRules.FigureDamping, BattleRules.FigureBounce, loose), dt);
            }
            weapon.Follow(weaponWant, stiff * 1.25f, BattleRules.FigureBounce, dt);
            weapon2.Follow(weapon2Want, stiff * 1.1f, BattleRules.FigureBounce, dt);
            for (int b = 0; b < bits.Length; b++) bits[b].Follow(bitWant[b], stiff * 0.45f, BattleRules.FigureBounce * 0.8f, dt);

            //AIR : gravity, the baton's wind, and a slow breeze so nothing ever hangs dead still. The wind also pushes the loose
            //parts a little (they float on springs, so they lean with the swing and come back)
            Vector2 breeze = new Vector2((float)Math.Sin(clock * 0.9f + seed) * 0.6f + (float)Math.Sin(clock * 2.3f + seed * 2f) * 0.25f, 0f)
                             * BattleRules.FigureBreeze;
            for (int i = 0; i < body.Length; i++) body[i].Speed += wind * (bodyLoose[i] * BattleRules.FigureWindBody * scale * dt);
            for (int b = 0; b < bits.Length; b++) bits[b].Speed += wind * (BattleRules.FigureWindBody * 1.6f * scale * dt);
            Vector2 pull = new Vector2(0f, BattleRules.FigureGravity) * scale + (wind + breeze) * scale;
            for (int c = 0; c < chainCount; c++)
            {
                chains[c].Link = chainLink[c] * Height;
                chains[c].Step(ChainRoot(c), pull * chainWeight[c], dt, BattleRules.FigureDrag);
            }
            UpdateShards(dt);
        }

        //Place All : every part where the pose wants it, still (the first frame)
        private void PlaceAll()
        {
            Pose();
            for (int i = 0; i < body.Length; i++)
            {
                body[i].At = want[i];
                body[i].Speed = Vector2.Zero;
            }
            weapon.At = weaponWant;
            weapon2.At = weapon2Want;
            for (int b = 0; b < bits.Length; b++)
            {
                bits[b].At = bitWant[b];
                bits[b].Speed = Vector2.Zero;
            }
            for (int c = 0; c < chainCount; c++)
            {
                chains[c].Link = chainLink[c] * Height;
                chains[c].Place(ChainRoot(c), new Vector2(-0.25f * Facing, 1f));
            }
        }

        private void CarryAll(Vector2 by)
        {
            for (int i = 0; i < body.Length; i++) body[i].At += by;
            for (int b = 0; b < bits.Length; b++) bits[b].At += by;
            for (int c = 0; c < chainCount; c++) chains[c].Carry(by);
            for (int s = 0; s < shardCount; s++) shards[s].At += by;
        }

        //======================================================================================
        //Helpers for the designs
        //======================================================================================

        //Local : a point of the design (shares of the height, x forward, y down from the feet) on the page
        private Vector2 Local(float x, float y)
        {
            return Feet + new Vector2(x * Facing, y) * Height;
        }

        //Local Way : a way of the design (an angle, 0 = forward, a quarter turn = down) on the page
        private Vector2 Way(float angle)
        {
            return new Vector2((float)Math.Cos(angle) * Facing, (float)Math.Sin(angle));
        }

        //Reach : the elbow (or knee) of a limb from its root to its end, the bend on the given side.
        //ADVANCED PART : two bone INVERSE KINEMATICS. The two bones and the line from root to end make a triangle whose three
        //sides are known, so the law of cosines gives the angle at the root. Turning the line by that angle and walking one
        //bone along it lands on the elbow. Too far to reach : the arm is straight, pointing at the end.
        private static Vector2 Reach(Vector2 root, Vector2 end, float upper, float lower, float side)
        {
            Vector2 d = end - root;
            float length = d.Length();
            if (length < 0.0001f) return root + new Vector2(0f, upper);
            float reach = MathHelper.Clamp(length, Math.Abs(upper - lower) + 0.01f, upper + lower - 0.01f);
            float cos = (upper * upper + reach * reach - lower * lower) / (2f * upper * reach);
            float angle = (float)Math.Acos(MathHelper.Clamp(cos, -1f, 1f)) * side;
            float baseAngle = (float)Math.Atan2(d.Y, d.X);
            return root + new Vector2((float)Math.Cos(baseAngle + angle), (float)Math.Sin(baseAngle + angle)) * upper;
        }

        //Hash : a steady random 0..1 from two whole numbers (the trick of BattleScreen.Stage.cs Hash01)
        private static float Hash(int a, int b)
        {
            float h = (float)Math.Sin(a * 12.9898f + b * 78.233f) * 43758.547f;
            return h - (float)Math.Floor(h);
        }

        //Ease : 0..1 eased at both ends
        private static float Ease(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        //Strike : an ATTACK's swing through its time (BattleRules.TurnStrikeTime, the hit at TurnStrikeImpact), the same
        //beats as the lunge of the box (BattleScreen.cs Lunge) : a pull back (ANTICIPATION), a fast cut that arrives as the
        //hit lands, a moment held, an eased way back. -1 = drawn back, 1 = the cut finished, 0 = rest.
        private float Strike()
        {
            float p = moveTime / BattleRules.TurnStrikeTime;
            float hit = BattleRules.TurnStrikeImpact / BattleRules.TurnStrikeTime;
            float wind = hit * 0.5f;
            if (p < wind) return -Ease(p / wind);
            if (p < hit)
            {
                float t = (p - wind) / (hit - wind);
                return MathHelper.Lerp(-1f, 1f, 1f - (1f - t) * (1f - t) * (1f - t));
            }
            float hold = hit + 0.16f;
            if (p < hold) return 1f;
            return 1f - Ease((p - hold) / (1f - hold));
        }

        //Swing Angles : where a blade starts and ends for the baton's way (ACTION). Angles of the design : 0 forward,
        //minus up. A plain ATTACK chops from high behind to low in front.
        private void SwingAngles(out float from, out float to)
        {
            switch (way)
            {
                case Flick.Up: from = 0.9f; to = -1.55f; break;            // from low, up past the head
                case Flick.Left: from = -0.3f; to = 2.6f; break;           // a back cut, round to behind
                case Flick.Right: from = -2.6f; to = 0.05f; break;         // a sweep round to straight ahead
                default: from = -2.5f; to = 0.75f; break;                  // Down and ATTACK : the overhead chop
            }
        }

        //======================================================================================
        //Draw
        //======================================================================================

        //Draw : the figure into the shapes being drawn (Core/Shapes.cs begun by the stage). alpha 0..1, flash 0..1 a hit's
        //white, light 0..1 how much light falls on it (TACET deeper in the dark is darker), mark 0..1 the STAGGERED edge.
        public void Draw(float alpha, float flash, float light, float mark)
        {
            if (alpha <= 0.005f) return;
            if (Of == Sort.Musician) DrawBand(alpha, flash);
            else DrawTacet(alpha, flash, light, mark);
        }

        //Draw Glow : the lights it gives off, into the GLOW picture (an additive sprite batch with the fighters' camera)
        public void DrawGlow(SpriteBatch sb, float alpha)
        {
            if (alpha <= 0.005f) return;
            if (Of == Sort.Musician) GlowBand(sb, alpha);
            else GlowTacet(sb, alpha);
        }
    }
}
