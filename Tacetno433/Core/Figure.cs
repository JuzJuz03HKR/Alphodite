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
    //   LOOK      the band in smooth lines and its own colour (BAND LOOK), TACET as smooth black shells with a pale rim and
    //             a white light held inside (TACET LOOK, round 6, the user 13.87 : smooth, brighter than the stage, breakable)
    //
    //The figure only shows, the battle rules never read it (Battle/TurnBattle.cs). Screens/BattleScreen.Stage.cs tells it
    //where it stands, what it plays and how hard it was hit. Every number is in BattleRules, Figure Feel.
    //The files : Figure.cs (this one, the machinery), Figure.Band.cs (the five legends), Figure.Tacet.cs (TACET and bosses).
    //Round 6 (2 Nov, docs/BETA1_DIRECTION.md 32) : the legends are drawn from the sketch docs/art_refs/concept_v6.html, LEVEL 1
    //by default, and their bodies show the fight (Figure.Band.cs LEGEND CUES).
    //Round 6 part 5 (2 Nov, 32.21) : ONE NOTE ONE BLOW, every note is one blow of KEY POSES landing when the game judges it
    //(see the ONE NOTE ONE BLOW part below, numbers in BattleRules Attack Feel).
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

        //Move : what it is doing besides its blows (idle, hurt, enter, ko, death). The blows are the PHASES below.
        private string move = "idle";
        private float moveTime;              // seconds into the move
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
            public int Hold;               // ROOTED : this many links leave the root along RootWay, held (hair leaves the head one way, then falls)
            public Vector2 RootWay;

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
                    for (int h = 1; h <= Hold && h < Count; h++) At[h] = root + RootWay * (Link * h);
                    for (int i = 0; i < Count - 1; i++)
                    {
                        if (i + 1 <= Hold) continue;                           // ROOTED : both ends of this link are held
                        Vector2 d = At[i + 1] - At[i];
                        float length = d.Length();
                        if (length < 0.0001f) continue;
                        float off = (length - Link) / length;
                        if (i <= Hold) At[i + 1] -= d * off;                   // the root (and its held links) stay, only the next one moves
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
        //An ATTACK (and a skill, kept for a screen that still asks) is one blow on its own clock, landing at TurnStrikeImpact.
        //The notes of a skill and of TACET's attacks come one by one instead (Ready, Blow, Guard below).
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
                if (Of != Sort.Musician) Chip(BattleRules.FigureChips);         // CHIPS : bits of TACET's shell fly off
                if (move == "idle") { move = "hurt"; moveTime = 0f; }
                return;
            }
            if (name == "ko" || name == "death")
            {
                down = true;
                move = name;
                moveTime = 0f;
                phase = Phase.None;                                             // a blow ends where it falls
                windDelay = -1f;
                if (name == "death") Shatter();
                return;
            }
            if (down) return;
            if (name == "attack" || name == "skill")
            {
                StartAttack(swing);
                return;
            }
            if (name == "parry")
            {
                Guard(true);
                return;
            }
            move = name;
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
            windNow = wind;

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

            if (move == "hurt" && moveTime > BattleRules.StageShiverTime) move = "idle";
            if (move == "enter" && moveTime > BattleRules.StageSwapTime) move = "idle";

            TickBlow(dt);                                                 // ONE NOTE ONE BLOW : the blow's phases move on
            Pose();
            if (float.IsNaN(want[Chest].X) || float.IsNaN(body[Chest].At.X))   // a broken number must never lose the figure for good
            {
                phase = Phase.None;
                pose = default(KeyPose);
                Pose();
                PlaceAll();
            }
            TickBand(dt);                                                 // LEGEND CUES : the band's own timers (Figure.Band.cs)
            float scale = Height / 512f;
            float stiff = BattleRules.FigureSpring * (phase != Phase.None ? BattleRules.AttackBodyStiff : 1f);   // the body carries the blow
            bool pinned = Pinned;
            for (int i = 0; i < body.Length; i++)
            {
                if (pinned && PinPart(i))                                 // PINNED : the blow's hands are where its pose says
                {
                    body[i].At = want[i];
                    body[i].Speed = Vector2.Zero;
                    continue;
                }
                float loose = bodyLoose[i];
                body[i].Follow(want[i], stiff * MathHelper.Lerp(1f, BattleRules.FigureLooseShare, loose),
                               MathHelper.Lerp(BattleRules.FigureDamping, BattleRules.FigureBounce, loose), dt);
            }
            if (pinned)
            {
                weapon.At = weaponWant;
                weapon.Speed = 0f;
                weapon2.At = weapon2Want;
                weapon2.Speed = 0f;
            }
            else
            {
                weapon.At = weaponWant + MathHelper.WrapAngle(weapon.At - weaponWant);      // a free weapon turns home the short way
                weapon2.At = weapon2Want + MathHelper.WrapAngle(weapon2.At - weapon2Want);
                weapon.Follow(weaponWant, BattleRules.FigureSpring * 1.25f, BattleRules.FigureBounce, dt);
                weapon2.Follow(weapon2Want, BattleRules.FigureSpring * 1.1f, BattleRules.FigureBounce, dt);
            }
            for (int b = 0; b < bits.Length; b++) bits[b].Follow(bitWant[b], BattleRules.FigureSpring * 0.45f, BattleRules.FigureBounce * 0.8f, dt);
            KeepSmear();

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
                chains[c].Step(ChainRoot(c), pull * chainWeight[c] + ChainDrift(c) * scale, dt, BattleRules.FigureDrag);   // DRIFT : a ribbon streams back
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
            return Feet + new Vector2(x * Facing, y) * H;                  // H : a legend's own size (Figure.Band.cs)
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

        //======================================================================================
        //ONE NOTE ONE BLOW (round 6 part 5, 2 Nov, docs/BETA1_DIRECTION.md 32.21, numbers in BattleRules Attack Feel)
        //======================================================================================

        //Every note is one blow, on both sides, and it lands the moment the game judges it : the band's when the note is
        //swiped (Blow), TACET's on the note's beat, the one in front blocking it (Guard). A blow is a few KEY POSES, still
        //poses given as numbers by each design (Figure.Band.cs BandKey, Figure.Tacet.cs TacetKey), joined in time. That is
        //the Worldless way, where the poses are drawn by hand and joined in between :
        //   READY   the wind up while a note's ring closes, as far as the time left says (Ready). A legend keeps a little of
        //           it between notes (STANCE). The plain ATTACK winds up on its own clock (WIND), its cut ending at
        //           TurnStrikeImpact, when the ATTACK's hit lands
        //   CUT     the blow, from wherever the wind up got to, fast, ending on the hit
        //   HOLD    the pose kept a moment on the hit (a PERFECT's hit stop holds it longer, Freeze)
        //   BACK    home, eased, the springs taking the hands again
        //   GUARD   on TACET's turn the one in front blocks each note, and starts raising it a moment before (Brace)
        //   CHARGE  a HOLD note : the charged pose, trembling, let go as its blow when the hold ends
        //PINNED : from READY to HOLD (and in GUARD and CHARGE) the weapon and the hands are put exactly where the pose says,
        //not on their springs, so the blow lands on time (the springs took 0.3 to 0.4 seconds to arrive, longer than a cut).
        //The body's springs pull harder meanwhile, so the whole body carries the weapon, and hair, cloth and the floating bits
        //still swing loose as always.
        private enum Phase { None, Ready, Wind, Cut, Hold, Back, Guard, Charge }
        private enum KeyOf { Wound, Struck, Guard, Charge }

        //Key Pose : one still pose. Points are the design's own (the band in the sketch's units, TACET in shares of its height, x
        //toward the other side), angles 0 forward and a quarter turn down, Lean the tip of the whole body (plus toward the
        //other side). The band's Head, Chest and Hip are before the lean, its hands and feet where they end up.
        private struct KeyPose
        {
            public Vector2 Head, Chest, Hip, HandA, HandB, FootA, FootB;
            public float Angle, Angle2;        // the two weapons
            public int Turn, Turn2;            // which way each weapon turns on its way to this pose : 1 forward then down, -1 up then back, 0 the short way
            public float Lean;
            public float Light;                // the blow's light : 0 at rest, 1 on the hit
            public float Squash;               // a body pressed by its blow (PFUNDT's drum, THE WEIGHT's block), below 0 stretched
            public float Throw;                // a thrown weapon on its way : 0 in the hand, 1 at the target (PUNTO)
            public float Aim;                  // a design's own part, 0 to 1 (PAGANINI aiming, THE MUTE's finger, the choir's fists, REQUIEM's quill)
            public float Show;                 // parts only shown in a blow (THE EMPTY CHOIR's floating hands)
            public float Arc;                  // ARCS : how much the hands swing round their shoulders on the way to this pose (0 straight, a thrust)
        }

        private Phase phase = Phase.None;
        private float phaseTime;              // seconds into the phase
        private float phraseTime;             // seconds since this phrase of blows began (QUANTZ's KEY RUN)
        private KeyPose pose;                     // the pose shown this frame (the draws read its Light, Squash, Throw, Aim and Show)
        private KeyPose from;                     // where the phase began
        private Flick blowWay = Flick.Down;   // the way of this blow (ACTION : the baton's way, a note's way)
        private int blowSide;                 // CHAINS : 0 the weapon hand, 1 the other (they take turns), 2 both (PFUNDT's FINISHER)
        private int nextSide;                 //   the side the next blow takes
        private float blowSize = 1f;          // FINISHER 1.25, MISS 0.5, a ROLL's shake 0.45
        private bool blowLast;                // FINISHER : the last note of a skill
        private bool secondBeat;              // TWO BEATS : PFUNDT's other hand is still to come down
        private bool readyGuard;              // this READY raises a guard (Brace), it is not a wind up
        private bool readySeen;               // Ready or Brace was called since the last Update (when the calls stop, it goes home)
        private bool stance;                  // STANCE : this READY began a phrase from rest
        private float readyDue;               // seconds until the note being readied
        private float readyStart;             //   how many there were when READY began
        private float windDelay = -1f;        // the plain ATTACK : seconds before its wind up begins (-1 : none waiting)
        private float windLength = 0.14f, cutLength = 0.06f, holdLength = 0.08f, backLength = 0.3f;   // set by each blow (never 0 : a phase's share divides by them)
        private bool guardClean;              // GUARD : a PERFECT block (else GOOD, pushed back)
        private float hitAge = -1f;           // seconds since the last blow landed (-1 : none yet), for what follows a blow (THE BLIND CANTOR's voice)
        private bool preCut;                  // PRE CUT : this cut began before its note was judged (Blow finishes it)
        private bool awaiting;                //   a legend's : it waits on the hit for the swipe to say how it went
        private KeyPose cutFrom;              // where the last cut began (FOLLOW THROUGH goes on the way it went)
        private KeyPose after;                // FOLLOW THROUGH (or REBOUND) : where a blow goes on to before it comes home

        //SMEAR : where the weapons were last frame and this one (the band draws two faint copies between them when fast)
        private Vector2 smearGrip, smearGrip2, lastGrip, lastGrip2;
        private float smearAngle, smearAngle2, lastAngle, lastAngle2;

        //Weight : 0 light, 1 medium, 2 heavy (Attack Feel), by design
        private int Weight
        {
            get
            {
                if (Of == Sort.Musician) return legend.Weapon == Hammers ? 2 : (legend.Weapon == Cannone || legend.Weapon == Rapier ? 0 : 1);
                if (Of == Sort.Boss) return 2;
                return design == Static ? 0 : (design == Damper ? 2 : 1);
            }
        }

        //Hold Of : how long a blow stays on its hit (the FINISHER longer). REBOUND : PFUNDT's sticks bounce off the skin at once.
        private float HoldOf(bool last)
        {
            float hold = BattleRules.AttackHold[Weight] * (last ? BattleRules.AttackFinisherHold : 1f);
            if (Of == Sort.Musician && legend.Weapon == Hammers) hold *= BattleRules.AttackDrumHold;
            return hold;
        }

        //Wind Time : how long its wind up takes before a note (THE WEIGHT and THE STAFF longest, every boss long)
        private float WindTime
        {
            get
            {
                if (Of != Sort.Musician && (design == Damper || design == Staff)) return BattleRules.AttackWindHeaviest;
                if (Of == Sort.Boss) return BattleRules.AttackWindBoss;
                return BattleRules.AttackWind[Weight];
            }
        }

        //Pinned : the hands and the weapon are put where the pose says this frame (see PINNED)
        private bool Pinned
        {
            get { return phase != Phase.None && phase != Phase.Back; }
        }

        //Pin Part : the parts a blow pins. The band : the hands and the feet (a step is a step). TACET : its whole body, it
        //is its own weapon (but THE NAMELESS MASTER's shadow and the legs of LEVEL 2 stay loose).
        private bool PinPart(int part)
        {
            if (Of == Sort.Musician) return part == HandA || part == HandB || part == FootA || part == FootB;
            return part != Extra && part != FootA && part != FootB;
        }

        //Blowing : in a blow (or on its way home)
        private bool Blowing
        {
            get { return phase != Phase.None || windDelay >= 0f; }
        }

        //--------------------------------------------------------------------------------------
        //What the stage asks for
        //--------------------------------------------------------------------------------------

        //Ready : a note of this fighter's phrase is coming, its way, how long until it is due (seconds of the screen's clock)
        //and whether it is the last. Called every frame while its ring closes. A legend takes its STANCE from the start of the
        //phrase, TACET only winds up near the note. cut : a note answered by one stroke, so its blow may start a moment before
        //it is due (PRE CUT) and land on the beat (a HOLD charges and a ROLL taps instead).
        public void Ready(Flick way, float dueIn, bool last, bool cut)
        {
            if (down) return;
            readySeen = true;
            readyDue = dueIn;
            if (phase == Phase.Cut || phase == Phase.Hold || phase == Phase.Guard || phase == Phase.Charge || windDelay >= 0f) return;   // the blow before ends first
            if (phase == Phase.Ready && !readyGuard && blowWay == way)
            {
                //PRE CUT : a cut has in-betweens to show its way, so it starts this much before the note and lands on its beat
                int w = Weight;
                if (cut && dueIn <= BattleRules.AttackCut[w] * BattleRules.AttackPreCut)
                {
                    int side = nextSide;
                    nextSide = side == 0 ? 1 : 0;
                    StartCut(way, side, blowSize, blowLast);
                    cutLength = BattleRules.AttackCut[w];
                    holdLength = HoldOf(blowLast);
                    backLength = BattleRules.AttackBack[w];
                    preCut = true;
                    awaiting = Of == Sort.Musician;                       // the band waits on the hit for the swipe, TACET always lands
                }
                return;
            }
            if (Of != Sort.Musician && dueIn > WindTime) return;
            bool fresh = phase == Phase.None;
            Snap();
            if (fresh)
            {
                phraseTime = 0f;
                nextSide = 0;
            }
            phase = Phase.Ready;
            phaseTime = 0f;
            readyGuard = false;
            stance = fresh && Of == Sort.Musician;
            blowWay = way;
            blowSide = nextSide;
            blowSize = last ? BattleRules.AttackFinisher : 1f;
            blowLast = last;
            readyStart = Math.Max(0.03f, dueIn);
        }

        //Brace : on TACET's turn the one in front raises the guard a little as the note comes (dueIn : seconds until it)
        public void Brace(float dueIn)
        {
            if (down) return;
            readySeen = true;
            readyDue = dueIn;
            if (dueIn > BattleRules.AttackBraceTime) return;
            if (phase != Phase.None && phase != Phase.Back) return;
            Snap();
            phase = Phase.Ready;
            phaseTime = 0f;
            readyGuard = true;
            stance = false;
            readyStart = Math.Max(0.03f, dueIn);
        }

        //Blow : the note was judged, the blow lands now. A MISS cuts the air, the last note of a skill is the FINISHER.
        //A PRE CUT on its way is finished at once instead (or, already on its hit, held from now).
        public void Blow(Flick way, bool landed, bool last)
        {
            if (down) return;
            if (preCut && (phase == Phase.Cut || phase == Phase.Hold))
            {
                preCut = false;
                awaiting = false;
                bool finisher = last && landed;
                if (finisher && !blowLast)                                    // the last note grows into the FINISHER
                {
                    blowSize = BattleRules.AttackFinisher;
                    if (Of == Sort.Musician && legend.Weapon == Hammers) blowSide = 2;
                }
                blowLast = finisher;
                holdLength = HoldOf(blowLast);
                if (phase == Phase.Cut) cutLength = Math.Min(cutLength, phaseTime + BattleRules.AttackCatchUp);
                else phaseTime = 0f;                                          // the hold counts from the hit
                return;
            }
            if (way == Flick.None) way = blowWay;
            int side = nextSide;
            nextSide = side == 0 ? 1 : 0;                                     // CHAINS : the next blow takes the other hand
            if (last && landed && Of == Sort.Musician && legend.Weapon == Hammers) side = 2;   // PFUNDT's FINISHER : both hands
            float size = !landed ? BattleRules.AttackMiss : (last ? BattleRules.AttackFinisher : 1f);
            StartCut(way, side, size, last && landed);
            int w = Weight;
            cutLength = BattleRules.AttackCut[w];
            holdLength = HoldOf(blowLast);
            backLength = BattleRules.AttackBack[w];
        }

        //Tap : one shake of a ROLL, a small quick blow with the other hand each time (build : how far the roll has gone, 0 to 1)
        public void Tap(Flick way, float build)
        {
            if (down) return;
            if (way == Flick.None) way = blowWay;
            int side = nextSide;
            nextSide = side == 0 ? 1 : 0;
            StartCut(way, side, BattleRules.AttackTap * (0.8f + 0.4f * build), false);
            cutLength = BattleRules.AttackTapTime;
            holdLength = BattleRules.AttackTapTime;
            backLength = BattleRules.AttackTapTime * 4f;
        }

        //Charge : a HOLD note was caught, its charged pose is held (and trembles harder) until the hold ends and its Blow lets go
        public void Charge(Flick way)
        {
            if (down) return;
            if (way != Flick.None) blowWay = way;
            if (phase == Phase.Charge) return;
            Snap();
            phase = Phase.Charge;
            phaseTime = 0f;
        }

        //Guard : TACET's blow meets the one in front. clean : a PERFECT, a sharp block. Else GOOD : the block holds but it is
        //pushed back.
        public void Guard(bool clean)
        {
            if (down) return;
            Snap();
            phase = Phase.Guard;
            phaseTime = 0f;
            guardClean = clean;
            Knock(BattleRules.FigureKnock * (clean ? 0.15f : BattleRules.AttackGuardPush));
        }

        //Freeze : the hit stop holds the world still, but a cut just begun still finishes, so the stop holds the weapon ON the
        //hit and not before it (the screen calls this instead of Update while it is frozen)
        public void Freeze(float dt)
        {
            if (phase != Phase.Cut || dt <= 0f)
            {
                KeepSmear();
                return;
            }
            dt = Math.Min(dt, 1f / 30f);
            phaseTime += dt;
            if (phaseTime >= cutLength) EndCut();
            Pose();
            for (int i = 0; i < body.Length; i++)
            {
                if (!PinPart(i)) continue;
                body[i].At = want[i];
                body[i].Speed = Vector2.Zero;
            }
            weapon.At = weaponWant;
            weapon2.At = weapon2Want;
            KeepSmear();
        }

        //RIBBON (Worldless, the user 3 Nov) : the last places of the main weapon's tip, newest first. The band draws a tapered
        //trail of light along them (white at the tip, the legend's colour fading to nothing at its tail) when the tip moves fast.
        private const int RibbonPoints = 9;
        private readonly Vector2[] ribbon = new Vector2[RibbonPoints];
        private readonly Vector2[] ribbonSmooth = new Vector2[RibbonPoints * 4];      // CHAIKIN : the trail rounded off
        private readonly Vector2[] ribbonStep = new Vector2[RibbonPoints * 4];
        private readonly float[] ribbonWide = new float[RibbonPoints * 4];
        private int ribbonCount;
        private float ribbonReach;                // how far the tip is from the hand (the band sets it when it draws the weapon)

        //Keep Smear : remember where the weapons were last frame (SMEAR, drawn by the band), and the tip's trail (RIBBON)
        private void KeepSmear()
        {
            if (ribbonReach > 0f)
            {
                for (int i = RibbonPoints - 1; i > 0; i--) ribbon[i] = ribbon[i - 1];
                ribbon[0] = body[HandA].At + Way(weapon.At) * ribbonReach;
                ribbonCount = Math.Min(RibbonPoints, ribbonCount + 1);
            }
            smearGrip = lastGrip;
            smearAngle = lastAngle;
            smearGrip2 = lastGrip2;
            smearAngle2 = lastAngle2;
            lastGrip = body[HandA].At;
            lastAngle = weapon.At;
            lastGrip2 = body[HandB].At;
            lastAngle2 = weapon2.At;
        }

        //Busy : still in a blow, a guard, a hurt or walking in. The stage waits for it before the next musician comes in (the
        //user 13.104 : let a blow, or taking a hit, finish first). The last part of the way home does not count.
        public bool Busy
        {
            get
            {
                if (down) return false;
                if (move == "hurt" || move == "enter" || windDelay >= 0f) return true;
                if (phase == Phase.None || (phase == Phase.Ready && readyGuard)) return false;
                if (phase == Phase.Back) return phaseTime < backLength * BattleRules.AttackBusyBack;
                return true;
            }
        }

        //--------------------------------------------------------------------------------------
        //The phases
        //--------------------------------------------------------------------------------------

        //Start Attack : the plain ATTACK on its own clock. The cut must end at TurnStrikeImpact, when the hit lands, so the
        //wind up starts that long before it (a heavy wind up that does not fit is made shorter, its weight shows in how far
        //it draws back and how long it holds). PFUNDT's ATTACK is TWO BEATS, the other hand follows.
        private void StartAttack(Flick swing)
        {
            int w = Weight;
            cutLength = BattleRules.AttackCut[w];
            windLength = Math.Min(BattleRules.AttackWind[w], BattleRules.TurnStrikeImpact - cutLength);
            holdLength = HoldOf(false);
            backLength = BattleRules.AttackBack[w];
            blowWay = swing == Flick.None ? Flick.Down : swing;
            blowSide = 0;
            nextSide = 1;
            blowSize = 1f;
            blowLast = false;
            phraseTime = 0f;
            secondBeat = Of == Sort.Musician && legend.Weapon == Hammers;
            if (secondBeat) holdLength = BattleRules.AttackSecondBeat;
            windDelay = Math.Max(0f, BattleRules.TurnStrikeImpact - cutLength - windLength);
            if (windDelay <= 0f) StartWind();
        }

        private void StartWind()
        {
            windDelay = -1f;
            Snap();
            phase = Phase.Wind;
            phaseTime = 0f;
        }

        private void StartCut(Flick way, int side, float size, bool last)
        {
            Snap();
            cutFrom = from;
            phase = Phase.Cut;
            phaseTime = 0f;
            blowWay = way;
            blowSide = side;
            blowSize = size;
            blowLast = last;
            windDelay = -1f;
            preCut = false;
            awaiting = false;
        }

        private void EndCut()
        {
            phase = Phase.Hold;
            phaseTime = 0f;
            hitAge = 0f;
        }

        //Start Back : home again. After a blow's hit it first goes on a little the way it went (FOLLOW THROUGH), a drum stick
        //bounces back up instead (REBOUND), then it settles home.
        private void StartBack()
        {
            bool afterHit = phase == Phase.Hold;
            Snap();
            phase = Phase.Back;
            phaseTime = 0f;
            if (backLength < 0.05f) backLength = BattleRules.AttackBack[Weight];
            after = from;
            if (!afterHit) return;
            if (Of == Sort.Musician && legend.Weapon == Hammers) after = Blend(from, cutFrom, BattleRules.AttackRebound);   // REBOUND
            else after = FollowKey(cutFrom, from, Of == Sort.Musician ? BattleRules.AttackFollow : BattleRules.AttackFollow * 0.5f);
        }

        //Snap : a phase starts from the pose shown now. Parts that were on their springs start from where the springs have
        //them, so nothing jumps when they are pinned.
        private void Snap()
        {
            from = pose;
            if (Pinned) return;
            if (Of == Sort.Musician)
            {
                from.HandA = SketchOf(body[HandA].At);
                from.HandB = SketchOf(body[HandB].At);
                from.FootA = SketchOf(body[FootA].At);
                from.FootB = SketchOf(body[FootB].At);
            }
            else
            {
                from.Head = ShareOf(body[Head].At);
                from.Chest = ShareOf(body[Chest].At);
                from.Hip = ShareOf(body[Hip].At);
                from.HandA = ShareOf(body[HandA].At);
                from.HandB = ShareOf(body[HandB].At);
            }
            from.Angle = weapon.At;
            from.Angle2 = weapon2.At;
        }

        //Share Of : a point of the page as a point of the design in shares of the height (the other way from Local)
        private Vector2 ShareOf(Vector2 page)
        {
            return new Vector2((page.X - Feet.X) / (H * Facing), (page.Y - Feet.Y) / H);
        }

        //Tick Blow : the phases move on (Update calls it once a frame, before the pose)
        private void TickBlow(float dt)
        {
            if (hitAge >= 0f) hitAge += dt;
            if (noiseBlink > 0f) noiseBlink -= dt;
            if (phase != Phase.None) phraseTime += dt;
            if (windDelay >= 0f)
            {
                windDelay -= dt;
                if (windDelay <= 0f) StartWind();
                return;
            }
            phaseTime += dt;
            switch (phase)
            {
                case Phase.Ready:
                    if (!readySeen) StartBack();                               // the notes stopped coming (the phrase is over)
                    break;
                case Phase.Wind:
                    if (phaseTime >= windLength)
                    {
                        StartCut(blowWay, blowSide, blowSize, blowLast);
                        phaseTime = 0f;
                    }
                    break;
                case Phase.Cut:
                    if (phaseTime >= cutLength) EndCut();
                    break;
                case Phase.Hold:
                    if (awaiting && readySeen) break;                         // PRE CUT : on its hit until the swipe says how it went
                    if (awaiting)                                              //   (the notes stopped coming : nothing will)
                    {
                        awaiting = false;
                        preCut = false;
                        phaseTime = 0f;
                    }
                    if (phaseTime < holdLength) break;
                    if (secondBeat)                                            // TWO BEATS : the other hand comes down
                    {
                        secondBeat = false;
                        StartCut(blowWay, 1, 1f, false);
                        holdLength = HoldOf(false);
                        backLength = BattleRules.AttackBack[Weight] * 0.75f;
                    }
                    else StartBack();
                    break;
                case Phase.Back:
                    if (phaseTime >= backLength) phase = Phase.None;
                    break;
                case Phase.Guard:
                    if (phaseTime >= BattleRules.AttackGuardTime + BattleRules.AttackGuardHold)
                    {
                        backLength = BattleRules.AttackBack[Weight];
                        StartBack();
                    }
                    break;
            }
            readySeen = false;
        }

        //Pose Now : the pose of this frame, the design's keys joined by the phase. rest is the design's standing pose now.
        private KeyPose PoseNow(KeyPose rest)
        {
            switch (phase)
            {
                case Phase.Ready:
                    {
                        KeyPose target;
                        if (readyGuard)
                        {
                            float near = 1f - MathHelper.Clamp(readyDue / BattleRules.AttackBraceTime, 0f, 1f);
                            target = Blend(rest, KeyFor(KeyOf.Guard, rest), BattleRules.AttackBrace * Ease(near));
                        }
                        else
                        {
                            //A phrase's first wind up starts from rest (a STANCE kept), the next from the blow before as it
                            //drifts home, so a chain of blows flows from one into the next
                            float near = 1f - MathHelper.Clamp(readyDue / WindTime, 0f, 1f);
                            float amount = Steps(WindEase(near));
                            KeyPose start = rest;
                            if (stance) amount = Math.Max(BattleRules.AttackStance, amount);    // STANCE
                            else start = Join(from, rest, Steps(phaseTime / BattleRules.AttackBack[Weight]));
                            target = Join(start, KeyFor(KeyOf.Wound, rest), amount);
                        }
                        float settle = phaseTime / Math.Min(BattleRules.AttackSettle, readyStart);
                        return Blend(from, target, Ease(settle));
                    }
                case Phase.Wind:
                    return Join(from, KeyFor(KeyOf.Wound, rest), Steps(WindEase(phaseTime / windLength)));
                case Phase.Cut:
                    return Join(from, KeyFor(KeyOf.Struck, rest), phaseTime / cutLength);
                case Phase.Hold:
                    return KeyFor(KeyOf.Struck, rest);
                case Phase.Back:
                    {
                        //FOLLOW THROUGH (or REBOUND) first, quick then slowing, then home, slow at both ends
                        float p = phaseTime / backLength;
                        float f = BattleRules.AttackFollowTime;
                        if (p < f) return Join(from, after, Steps(1f - (1f - p / f) * (1f - p / f)));
                        return Join(after, rest, Steps((p - f) / (1f - f)));
                    }
                case Phase.Guard:
                    return Join(from, KeyFor(KeyOf.Guard, rest), phaseTime / BattleRules.AttackGuardTime);
                case Phase.Charge:
                    {
                        KeyPose charged = Blend(from, KeyFor(KeyOf.Charge, rest), Ease(phaseTime / BattleRules.AttackChargeTime));
                        float grow = MathHelper.Clamp(phaseTime / BattleRules.TurnHoldTime, 0f, 1f);
                        float shake = BattleRules.AttackChargeShake * (0.3f + 0.7f * grow);      // it trembles harder as it builds
                        Vector2 tremble = new Vector2((float)Math.Sin(clock * 53f), (float)Math.Sin(clock * 41f + 1.3f)) * shake;
                        if (Of != Sort.Musician) tremble /= SketchHeight;                         // TACET's points are shares of its height
                        charged.HandA += tremble;
                        charged.Angle += (float)Math.Sin(clock * 47f) * 0.025f * (0.3f + 0.7f * grow);
                        charged.Light = Math.Max(charged.Light, 0.35f + 0.5f * grow);
                        return charged;
                    }
                default:
                    return rest;
            }
        }

        //Steps : THE NOISE moves like a broken signal, in three jumps and not smoothly, blinking out at each jump (noiseBlink,
        //Figure.Tacet.cs DrawLook). Every other design : as it is.
        private int noiseStep;
        private float noiseBlink;

        private float Steps(float t)
        {
            if (Of == Sort.Musician || design != Static) return t;
            int step = (int)Math.Floor(MathHelper.Clamp(t, 0f, 1f) * 3f + 0.35f);
            if (step != noiseStep)
            {
                noiseStep = step;
                noiseBlink = 0.06f;
            }
            return step / 3f;
        }

        //Wind Ease : a wind up starts quickly and slows into its top, where it hangs a moment before the blow (ANTICIPATION)
        private static float WindEase(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            return 1f - (1f - t) * (1f - t);
        }

        //JOIN : the way from one key to the next the way a body moves, not every part in a straight line at once (the user
        //13.104 : like real blows; these are the animator's ARCS and OVERLAPPING ACTION) :
        //   OVERLAP  the body starts the move and gets there first (AttackLead), the hand follows
        //   DRAG     the weapon starts late and whips through at the end (AttackDrag), so the blade is fastest on the hit
        //   FEET     land after the hand (AttackFootLate) : a fencer's arm goes before her foot, a step comes under the blow
        //   ARCS     a hand swings round its shoulder (its angle and its distance are joined, not its place), as much as the
        //            key's Arc says : a slash or a throw goes round, a thrust goes straight
        //p is the share of the move done, 0 to 1. Every part is at a at 0 and at b at 1, so nothing jumps between phases.
        private KeyPose Join(KeyPose a, KeyPose b, float p)
        {
            p = MathHelper.Clamp(p, 0f, 1f);
            float body = Ease(Math.Min(1f, p * BattleRules.AttackLead));
            float hand = Ease(p);
            float tip = Ease((p - BattleRules.AttackDrag) / (1f - BattleRules.AttackDrag));
            float feet = Ease((p - BattleRules.AttackFootLate) / (1f - BattleRules.AttackFootLate));
            KeyPose k = Blend(a, b, hand);
            k.Head = Vector2.Lerp(a.Head, b.Head, body);
            k.Chest = Vector2.Lerp(a.Chest, b.Chest, body);
            k.Hip = Vector2.Lerp(a.Hip, b.Hip, body);
            k.Lean = MathHelper.Lerp(a.Lean, b.Lean, body);
            k.FootA = Vector2.Lerp(a.FootA, b.FootA, feet);
            k.FootB = Vector2.Lerp(a.FootB, b.FootB, feet);
            k.Angle = a.Angle + Toward(a.Angle, b.Angle, b.Turn) * tip;
            k.Angle2 = a.Angle2 + Toward(a.Angle2, b.Angle2, b.Turn2) * tip;
            if (b.Arc > 0f)
            {
                k.HandA = Vector2.Lerp(k.HandA, ArcHand(a.HandA, b.HandA, ShoulderOf(a, false), ShoulderOf(b, false), body, hand), b.Arc);
                if (Of == Sort.Musician)
                    k.HandB = Vector2.Lerp(k.HandB, ArcHand(a.HandB, b.HandB, ShoulderOf(a, true), ShoulderOf(b, true), body, hand), b.Arc);
            }
            return k;
        }

        //Arc Hand : a hand on its way from a to b round its shoulder (sa to sb, the shoulder moves with the body). Its angle
        //round the shoulder turns the short way (an arm lifts in front to go behind the head, while the blade it holds swings
        //down and back on its own way, Toward), its distance from the shoulder changes evenly.
        //ADVANCED PART : POLAR JOINING. A straight line between two places of a hand cuts through the body when the hand
        //goes from behind the head to the front. Joined as an angle and a length round the shoulder instead, the hand draws
        //the circle an arm really draws.
        private static Vector2 ArcHand(Vector2 a, Vector2 b, Vector2 sa, Vector2 sb, float body, float t)
        {
            Vector2 da = a - sa, db = b - sb;
            float la = da.Length(), lb = db.Length();
            if (la < 0.001f || lb < 0.001f) return Vector2.Lerp(a, b, t);
            float aa = (float)Math.Atan2(da.Y, da.X), ab = (float)Math.Atan2(db.Y, db.X);
            float angle = aa + MathHelper.WrapAngle(ab - aa) * t;
            float length = MathHelper.Lerp(la, lb, t);
            return Vector2.Lerp(sa, sb, body) + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * length;
        }

        //Shoulder Of : where a key's shoulder is (off : the other one), in the key's own units. The band's turns with its
        //lean, TACET's hands go round its middle.
        private Vector2 ShoulderOf(KeyPose k, bool off)
        {
            if (Of != Sort.Musician) return k.Chest;
            Vector2 s = legend.Shoulder;
            if (off) s.X = legend.Chest.X * 2f - s.X;
            return LeanRound(s + (k.Chest - legend.Chest), new Vector2(SketchMiddle, SketchFloor), k.Lean * (off ? 0.6f : 1f));
        }

        //Follow Key : FOLLOW THROUGH, a blow going on a little past its hit the way it went (from start through hit), its
        //feet staying where they landed
        private KeyPose FollowKey(KeyPose start, KeyPose hit, float amount)
        {
            KeyPose k = Blend(start, hit, 1f + amount);
            k.Angle = hit.Angle + Toward(start.Angle, hit.Angle, hit.Turn) * amount;
            k.Angle2 = hit.Angle2 + Toward(start.Angle2, hit.Angle2, hit.Turn2) * amount;
            if (hit.Arc > 0f)
                k.HandA = Vector2.Lerp(k.HandA, ArcHand(start.HandA, hit.HandA, ShoulderOf(start, false), ShoulderOf(hit, false), 1f, 1f + amount), hit.Arc);
            k.FootA = hit.FootA;
            k.FootB = hit.FootB;
            k.Light = hit.Light;
            k.Squash = hit.Squash;
            k.Throw = hit.Throw;
            k.Aim = hit.Aim;
            k.Show = hit.Show;
            return k;
        }

        //Key For : the design's key of this kind for the blow being played, made bigger or smaller by its size (FINISHER,
        //MISS, a ROLL's shake)
        private KeyPose KeyFor(KeyOf kind, KeyPose rest)
        {
            KeyPose k = Of == Sort.Musician ? BandKey(kind, rest) : TacetKey(kind, rest);
            if ((kind == KeyOf.Wound || kind == KeyOf.Struck) && blowSize != 1f) k = Scale(rest, k, blowSize);
            return k;
        }

        //Blend : the pose t of the way from a to b, the weapons turning the way b asks (Toward)
        private static KeyPose Blend(KeyPose a, KeyPose b, float t)
        {
            KeyPose k;
            k.Head = Vector2.Lerp(a.Head, b.Head, t);
            k.Chest = Vector2.Lerp(a.Chest, b.Chest, t);
            k.Hip = Vector2.Lerp(a.Hip, b.Hip, t);
            k.HandA = Vector2.Lerp(a.HandA, b.HandA, t);
            k.HandB = Vector2.Lerp(a.HandB, b.HandB, t);
            k.FootA = Vector2.Lerp(a.FootA, b.FootA, t);
            k.FootB = Vector2.Lerp(a.FootB, b.FootB, t);
            k.Angle = a.Angle + Toward(a.Angle, b.Angle, b.Turn) * t;
            k.Angle2 = a.Angle2 + Toward(a.Angle2, b.Angle2, b.Turn2) * t;
            k.Turn = b.Turn;
            k.Turn2 = b.Turn2;
            k.Lean = MathHelper.Lerp(a.Lean, b.Lean, t);
            k.Light = MathHelper.Lerp(a.Light, b.Light, t);
            k.Squash = MathHelper.Lerp(a.Squash, b.Squash, t);
            k.Throw = MathHelper.Lerp(a.Throw, b.Throw, t);
            k.Aim = MathHelper.Lerp(a.Aim, b.Aim, t);
            k.Show = MathHelper.Lerp(a.Show, b.Show, t);
            k.Arc = b.Arc;
            return k;
        }

        //Scale : a key made bigger (or smaller) by size, measured from rest. Its weapons turn the short way from rest.
        private static KeyPose Scale(KeyPose rest, KeyPose k, float size)
        {
            KeyPose s = Blend(rest, k, size);
            s.Angle = rest.Angle + MathHelper.WrapAngle(k.Angle - rest.Angle) * size;
            s.Angle2 = rest.Angle2 + MathHelper.WrapAngle(k.Angle2 - rest.Angle2) * size;
            s.Turn = k.Turn;
            s.Turn2 = k.Turn2;
            return s;
        }

        //Toward : how far an angle turns to reach another. A big turn (more than about 70 degrees) goes the way asked
        //(1 forward then down, -1 up then back), so a wind up never sweeps through TACET and a cut goes over the top. A small
        //one always goes the short way, so a weapon never spins round to reach a pose it is nearly at.
        private static float Toward(float from, float to, int turn)
        {
            float d = MathHelper.WrapAngle(to - from);
            if (turn != 0 && Math.Abs(d) > 1.2f && Math.Sign(d) != turn) d += turn * MathHelper.TwoPi;
            return d;
        }

        //======================================================================================
        //Draw
        //======================================================================================

        //Draw : the figure into the shapes being drawn (Core/Shapes.cs begun by the stage). alpha 0..1, flash 0..1 a hit's
        //white, light 0..1 how much light falls on it (TACET further back is darker), mark 0..1 the STAGGERED edge.
        //HOLE LIGHT (D7) : Behind says how dark it is behind it; the stage sets it before drawing a TACET figure.
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

        //Draw Reflection (round 6 part 4) : the figure upside down in the glossy floor under its feet, faint, fading out as it
        //goes down (MIRROR in Core/Shapes.cs, as in the sketch). Drawn before the figure, so it stands on its reflection.
        //No hit flash and no STAGGERED edge show in the floor.
        public void DrawReflection(float alpha, float light)
        {
            if (alpha <= 0.005f) return;
            Shapes.MirrorAt(Feet.Y + Shift.Y, Height * BattleRules.FigureMirrorDepth);
            Draw(alpha * BattleRules.FigureMirrorAlpha, 0f, light, 0f);
            Shapes.MirrorOff();
        }

        //Glow Floor (round 6 part 4) : the light a figure gives the floor it stands on, a flat soft pool under its feet, into
        //the GLOW picture. wide is a share of its height.
        private void GlowFloor(SpriteBatch sb, float wide, Color light)
        {
            Vector2 at = Feet + Shift;
            float w = wide * Height, tall = 0.04f * Height;
            Gfx.DrawGlowBox(sb, new Rectangle((int)(at.X - w * 0.5f), (int)(at.Y - tall * 0.5f), (int)w, (int)tall), light);
        }
    }
}
