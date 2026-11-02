using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //HitFx Skills (Beta1 block D, 2 Nov, docs/BETA1_DIRECTION.md 37) : each skill of the five legends looks like its name.
    //The rules of the look (37.2, from the games studied in docs/research/FINALE9_vfx.md) : one white focus where the blow
    //lands, three beats (gather, hit, fade), the SHAPE says what the move does, the legend's element colour on the edge and a
    //white hot core. Nothing flies across the screen : it happens on the enemy.
    //   CAPRICE   PAGANINI's four quick cuts. Each cut splits the stage along its line for a moment (SPACE CUT, Post.fx) and
    //             leaves a burning SEAM on the body (BURN). The last note sets every seam alight at once (Caprice Finish).
    //   CANNON    IL CANNONE. While its note is held the fire is drawn into the f hole (GATHER). Let go, the blast goes in at
    //             the front of the enemy and bursts out of its back (it goes through ARMOR).
    //   LANCE     DUPORT's two thrusts, the way Worldless stabs : a long flat spike of light shoots through the body in two
    //             frames, a ring of pressure stands across it, water sprays out of the back.
    //   CHAKRAM   PUNTO's ring : a spinning saw of light grinds on the enemy it reached, sparks thrown off its edge, a ring cut
    //             left behind.
    //   ROLL      PFUNDT's TIMPANI ROLL : the floor shakes in rings one after another, the blows jolt up from below.
    //Every piece lives in a fixed pool (no garbage in a fight), sizes and times are in Data/BattleRules.cs, Skill Feel.
    public partial class HitFx
    {
        //SPACE CUT : a cut that splits the picture of the stage for a moment. DMC's Judgement Cut and ZZZ's Miyabi cut
        //space itself; TACET's version cuts the silence : what shows between the two halves is the black behind the world.
        //Mode : a skill's cut opens and closes by itself (CutQuick). FANNY's FINALE writes cracks that stand as a hairline
        //(CutWaiting) until THE BREAK opens them all together (CutBroken, Core/HitFx.Finale.cs). Prism 1 = its edges run
        //through the rainbow.
        private const byte CutQuick = 0, CutWaiting = 1, CutBroken = 2;
        private struct SpaceCut
        {
            public bool On;
            public byte Mode;
            public Vector2 At;
            public float Angle, Age, Reach, Slide, Gap, Prism;
            public Color Tint;
        }
        private readonly SpaceCut[] spaceCuts = new SpaceCut[PostChain.MostCuts];

        //SEAM : a burning line CAPRICE leaves on an enemy. It glows white, flickers along its length, cools to the
        //legend's colour and then to a dark ember. Flare relights it (the last note of CAPRICE).
        private struct Seam
        {
            public bool On;
            public Vector2 A, B;
            public float Age, Life, Flare;
            public int Seed;
            public Color Tint, Hot;
        }
        private readonly Seam[] seams = new Seam[12];

        //THRUST : LANCE's spike. It shoots from its tail to its tip, holds, then thins away from the tail.
        private struct Thrust
        {
            public bool On;
            public Vector2 Tail, Tip;
            public float Width, Age;
            public Color Tint, Hot;
        }
        private readonly Thrust[] thrusts = new Thrust[6];

        //SAW : CHAKRAM's spinning ring grinding on an enemy
        private struct Saw
        {
            public bool On;
            public Vector2 At;
            public float Radius, Age, Spin;
            public Color Tint, Hot;
        }
        private readonly Saw[] saws = new Saw[4];

        //PULSE : a ring that waits a moment before it opens (TIMPANI ROLL's rings one after another)
        private struct Pulse
        {
            public bool On;
            public Vector2 At;
            public float Wait, Size, Squash, Width, Length;
            public Color Tint;
        }
        private readonly Pulse[] pulses = new Pulse[16];

        //GATHER : IL CANNONE held. Sparks of the legend's colour are drawn into the f hole and a ball of fire grows there.
        private Vector2 gatherAt;
        private float gatherPower;          // 0..1, how much has gathered
        private float gatherFresh;          // seconds since the hold last fed it (it fades once nobody does)
        private float gatherClock;          // seconds the gather has been drawn (its pull lines run on it)
        private HitColors gatherColors = HitColors.Plain;

        //======================================================================================
        //Skill Hit : the five legends' own skills (called first by Hit)
        //======================================================================================

        private bool SkillHit(HitStyle style, Rectangle box, float power, HitColors c, int tier, Flick swing)
        {
            if (style != HitStyle.Caprice && style != HitStyle.Cannon && style != HitStyle.Lance && style != HitStyle.Chakram && style != HitStyle.Roll) return false;
            Vector2 at = new Vector2(box.Center.X, box.Y + box.Height * 0.45f);
            float size = (0.75f + 0.35f * power) * BattleRules.HitSize * (1f + BattleRules.HitTierSize * tier);
            float lean = (float)(random.NextDouble() - 0.5);

            if (style == HitStyle.Caprice) Caprice(at, box, size, power, c, swing, lean);
            else if (style == HitStyle.Cannon) Cannon(at, box, size, power, c);
            else if (style == HitStyle.Lance) Lance(at, box, size, power, c, swing, lean);
            else if (style == HitStyle.Chakram) Chakram(at, size, power, c);
            else Roll(at, box, size, power, c);

            //WEAPON TIERS : a sharper weapon still shows (the same marks as the plain blows), at three stars the element's
            //flourish (STAR MARKS, HitFx.Stars.cs)
            if (tier >= 2) AddRing(at, size * 1.35f, 1f, 2f, 0.36f, c.Tint);
            if (tier >= 3)
            {
                Star(at, size * 1.25f, c);
                Flourish(at, box, size, c);
            }
            return true;
        }

        //CAPRICE : one of the four cuts, the way the baton swung. The cut tears the stage open along its line (SPACE CUT),
        //a thin cut of fire runs through it, flames rise along it, and a SEAM stays burning on the body.
        private void Caprice(Vector2 at, Rectangle box, float size, float power, HitColors c, Flick swing, float lean)
        {
            float angle = swing != Flick.None ? SwingAngle(swing) + lean * 0.25f : -0.35f + lean * 0.5f;
            Vector2 way = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            float reach = Math.Max(box.Width, box.Height) * BattleRules.SkillCapriceReach;
            at += new Vector2(-way.Y, way.X) * (lean * box.Width * 0.18f);                  // the four cuts do not all cross one point

            AddSpaceCut(at, angle, reach, BattleRules.SkillCutSlide * (0.7f + 0.5f * power), BattleRules.SkillCutGap * (0.7f + 0.5f * power), c.Tint);
            AddCut(at - way * reach * 1.15f, at + way * reach * 1.15f, 3f + 2f * power, 0f, c);
            AddSeam(at - way * reach * 0.6f, at + way * reach * 0.6f, c);
            AddBurst(at, size * 0.55f, angle, 2.4f, 10, 3f, 0.18f, c);
            Throw(at, way, 10 + (int)(8 * power), 560f, Streak, c);
            Throw(at, -way, 6, 360f, Streak, c);
            //Each cut stays clean (one thin line and its seam, the way Miyabi's cuts read at once); the flames wait for the last
            //note, when every seam bursts at once (Caprice Finish). Embers drift up off the seam meanwhile.
            Throw(at, new Vector2(0f, -1f), 4 + (int)(4 * power), 160f, Ember, c);
            if (c.Flair == HitFlair.Sparkle) Sparkle(at, size * 0.6f, power * 0.5f, c);
        }

        //Caprice Finish : the last note of CAPRICE. Every seam still on the stage flares white and burns out in a burst of
        //flame, a ring of fire opens round the body (the BURN it left is seen at once).
        public void CapriceFinish(Rectangle box, HitColors c)
        {
            Vector2 middle = new Vector2(box.Center.X, box.Y + box.Height * 0.45f);
            float size = BattleRules.HitSize;
            for (int i = 0; i < seams.Length; i++)
            {
                if (!seams[i].On) continue;
                seams[i].Flare = 1f;
                seams[i].Age = Math.Min(seams[i].Age, seams[i].Life * 0.3f);
                Vector2 mid = (seams[i].A + seams[i].B) * 0.5f;
                Vector2 along = seams[i].B - seams[i].A;
                float length = along.Length();
                if (length > 0.01f) along /= length;
                if (c.Flair == HitFlair.Flame) Burn(mid, length * 0.25f, 0.7f, along, true, c);
                Throw(mid, new Vector2(0f, -1f), 8, 420f, Ember, c);
            }
            Star(middle, size * 1.6f, c);
            AddRing(middle, size * 1.7f, 1f, 4f, 0.42f, c.Tint);
            AddRing(middle, size * 1.1f, 1f, 2f, 0.3f, c.Hot);
            Focus(middle);
        }

        //CANNON : IL CANNONE let go. The blast goes in at the front of the enemy (the side facing the band) and bursts out of
        //its back : a thick straight beam through it, a ring of pressure standing across the beam where it goes in, spikes
        //along its way, fire sprayed out behind, speed lines. power grows with how long it was held.
        private void Cannon(Vector2 at, Rectangle box, float size, float power, HitColors c)
        {
            gatherPower = 0f;                                                                // the ball of fire is spent
            Vector2 front = new Vector2(box.X + box.Width * 0.2f, at.Y);
            Vector2 back = new Vector2(box.Right + box.Width * 0.55f, at.Y - 4f);
            float thick = BattleRules.SkillCannonWidth * (0.75f + 0.45f * power);
            AddCut(front - new Vector2(BattleRules.SkillCannonLead, 0f), back + new Vector2(BattleRules.SkillCannonLead * 0.6f, 0f), thick, 0f, c);
            AddCut(front - new Vector2(BattleRules.SkillCannonLead * 0.6f, 0f), back, thick * 0.35f, -0.03f, HitColors.Plain);   // the white core a frame later
            AddRing(front, size * 0.85f, 2.4f, 4f, 0.32f, c.Hot);                         // standing across the beam (taller than wide)
            AddRing(front, size * 1.35f, 2.4f, 2f, 0.42f, c.Tint);
            AddBurst(front, size * 1.2f, 0f, 2.6f, 16, 5f, 0.3f, c);
            AddBurst(back, size * 1.0f, 0f, 3.2f, 12, 4f, 0.36f, c);
            Throw(back, new Vector2(1f, -0.05f), 22 + (int)(14 * power), 980f, Streak, c);
            Throw(back, new Vector2(1f, -0.25f), 12, 520f, Spark, c);
            if (c.Flair == HitFlair.Flame) Burn(back, size * 0.9f, power, new Vector2(1f, 0f), false, c);
            else if (c.Flair == HitFlair.Sparkle) Sparkle(back, size, power, c);
            Focus(at);
        }

        //LANCE : one thrust. Mostly level from the band's side, a little up or down with the baton. The spike passes through
        //the body and out of the back, a ring of pressure stands across it, water sprays out where it comes out.
        private void Lance(Vector2 at, Rectangle box, float size, float power, HitColors c, Flick swing, float lean)
        {
            float angle = swing == Flick.Up ? -0.16f : swing == Flick.Down ? 0.16f : lean * 0.12f;
            Vector2 way = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            at += new Vector2(0f, lean * box.Height * 0.12f);
            Vector2 tail = at - way * box.Width * BattleRules.SkillLanceTail;
            Vector2 tip = at + way * box.Width * BattleRules.SkillLanceTip;
            AddThrust(tail, tip, BattleRules.SkillLanceWidth * (0.8f + 0.5f * power), c);
            AddRing(at, size * 0.7f, 2.6f, 3f, 0.26f, c.Hot);                               // the ring of pressure, across the thrust
            AddRing(at, size * 1.15f, 2.6f, 2f, 0.36f, c.Tint);
            AddBurst(at, size * 0.6f, angle, 2.8f, 8, 3f, 0.16f, c);
            Throw(tip, way, 16 + (int)(10 * power), 860f, Streak, c);                       // the spray out of the back
            Throw(tip, way + new Vector2(0f, -0.45f), 12, 420f, Spark, c);                  //   drops that fall
            if (c.Flair == HitFlair.Sparkle) Sparkle(tip, size * 0.5f, power * 0.5f, c);
            else if (c.Flair == HitFlair.Flame) Burn(tip, size * 0.5f, power * 0.5f, way, false, c);
        }

        //CHAKRAM : the ring reached this enemy. A saw of light spins on it, sparks are thrown off its edge, and a thin ring
        //cut stays a moment after.
        private void Chakram(Vector2 at, float size, float power, HitColors c)
        {
            int slot = Free(saws);
            float radius = size * BattleRules.SkillSawSize;
            saws[slot] = new Saw { On = true, At = at, Radius = radius, Age = 0f, Spin = (float)(random.NextDouble() * MathHelper.TwoPi), Tint = c.Tint, Hot = c.Hot };
            AddRing(at, radius * 1.25f, 1f, 2f, BattleRules.SkillSawTime + 0.25f, c.Hot);
            AddBurst(at, size * 0.7f, 0f, 1f, 12, 3f, 0.2f, c);
            Throw(at + new Vector2(0f, radius), new Vector2(-1f, 0.15f), 14 + (int)(8 * power), 620f, Streak, c);   // off the bottom edge, turning back
            if (c.Flair == HitFlair.Sparkle) Sparkle(at, size * 0.7f, power * 0.6f, c);
            else if (c.Flair == HitFlair.Flame) Burn(at, size * 0.6f, power * 0.6f, new Vector2(0f, -1f), false, c);
        }

        //ROLL : TIMPANI ROLL. Rings run out on the floor one after another and the blows jolt up from under the enemy. A small
        //power is one shake of the roll, a big one is the roll landing.
        private void Roll(Vector2 at, Rectangle box, float size, float power, HitColors c)
        {
            Vector2 feet = new Vector2(box.Center.X, box.Bottom - 6f);
            int rings = power > 0.5f ? 4 : 1;
            for (int k = 0; k < rings; k++)                                              // a shake's ring is wide and thick enough to be seen on the floor
                AddPulse(feet, size * (1.7f + 0.5f * k), 0.24f, 5f, 0.36f, k * BattleRules.SkillRollStep, k % 2 == 0 ? c.Tint : c.Hot);
            float x = box.X + box.Width * (0.3f + 0.4f * (float)random.NextDouble());
            Vector2 jolt = new Vector2(x, box.Bottom - box.Height * (0.15f + 0.25f * (float)random.NextDouble()));
            AddBurst(jolt, size * (0.5f + 0.5f * power), -MathHelper.PiOver2, 2.2f, 8 + (int)(8 * power), 4f, 0.2f, c);
            Throw(feet, new Vector2(0f, -1f), 6 + (int)(14 * power), 420f, Shard, c);
            if (power > 0.5f)
            {
                AddBurst(at, size * 1.1f, -MathHelper.PiOver2, 1.6f, 16, 5f, 0.3f, c);
                Throw(feet, new Vector2(0f, -1f), 12, 300f, Spark, c);
                if (c.Flair == HitFlair.Sparkle) Sparkle(at, size, power, c);
                else if (c.Flair == HitFlair.Flame) Burn(at, size, power, new Vector2(0f, -1f), false, c);
            }
        }

        //======================================================================================
        //Gather : IL CANNONE's charge while its note is held
        //======================================================================================

        //Gather : the hold fed the charge (build 0..1 grows as the note is held). Sparks are drawn in from round the point.
        public void Gather(Vector2 at, float build, HitColors c)
        {
            gatherAt = at;
            gatherPower = Math.Max(gatherPower, MathHelper.Clamp(build, 0f, 1f));
            gatherFresh = 0f;
            gatherColors = c;
            int count = 6 + (int)(8 * build);
            for (int i = 0; i < count; i++)
            {
                int slot = Free(bits);
                float angle = (float)(random.NextDouble() * MathHelper.TwoPi);
                float far = BattleRules.SkillGatherReach * (0.6f + 0.6f * (float)random.NextDouble());
                Vector2 from = at + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * far;
                bits[slot] = new Bit
                {
                    On = true,
                    Kind = Pull,
                    At = from,
                    Speed = new Vector2(-(float)Math.Sin(angle), (float)Math.Cos(angle)) * 120f,     // a little round, then in
                    Target = at,
                    Life = 0.6f,
                    Size = 2f,
                    Tint = c.Tint,
                    Hot = c.Hot,
                };
                bits[slot].Length = bits[slot].Life;
            }
        }

        //======================================================================================
        //TACET's moves and the band's calls (block D, 37.3, Screens/BattleScreen.Tells.cs says when)
        //======================================================================================

        //Shatter : TACET's CHARGE broken (BREAK). The power it had gathered bursts out of its heart as white splinters all
        //round, a spiked star and two rings : the crystal burst Acheron's ultimate ends on (docs/art_refs/vfx/
        //hsr_acheron_shatter.jpg), white because that power is nobody's now.
        public void Shatter(Vector2 at, float size)
        {
            HitColors white = HitColors.Plain;
            AddBurst(at, size * 1.4f, 0f, 1f, 16, 3f, 0.32f, white);
            AddRing(at, size * 1.6f, 1f, 3f, 0.42f, Color.White);
            AddRing(at, size * 0.9f, 1f, 2f, 0.3f, Color.White);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * MathHelper.TwoPi / 8f + (float)random.NextDouble() * 0.6f;
                Throw(at, new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)), 3, 560f, Shard, white);
            }
            Star(at, size * 1.3f, white);
            Focus(at);
        }

        //Splinter : a plate of TACET's ARMOR took a blow (Blocked) : pale chips and sparks off it, away from the band
        public void Splinter(Vector2 at, HitColors c)
        {
            Throw(at, new Vector2(0.8f, -0.6f), 6, 380f, Shard, HitColors.Plain);
            Throw(at, new Vector2(1f, -0.2f), 5, 520f, Spark, c);
            AddRing(at, BattleRules.HitSize * 0.35f, 1f, 2f, 0.2f, Color.White);
        }

        //Rumble : a ring on the floor that opens after a wait (THUNDER CALL's call rolling out to every enemy, nearest first)
        public void Rumble(Vector2 at, float size, float wait, Color tint)
        {
            AddPulse(at, size, 0.24f, 4f, 0.42f, wait, tint);
            AddPulse(at, size * 0.6f, 0.24f, 3f, 0.3f, wait + 0.06f, tint);
        }

        //======================================================================================
        //Spawning
        //======================================================================================

        private void AddSpaceCut(Vector2 at, float angle, float reach, float slide, float gap, Color tint)
        {
            int slot = 0;
            float oldest = -1f;
            for (int i = 0; i < spaceCuts.Length; i++)
            {
                if (!spaceCuts[i].On) { slot = i; oldest = -1f; break; }
                if (spaceCuts[i].Age > oldest) { oldest = spaceCuts[i].Age; slot = i; }
            }
            spaceCuts[slot] = new SpaceCut { On = true, Mode = CutQuick, At = at, Angle = angle, Age = 0f, Reach = reach, Slide = slide, Gap = gap, Tint = tint };
        }

        private void AddSeam(Vector2 a, Vector2 b, HitColors c)
        {
            int slot = Free(seams);
            seams[slot] = new Seam { On = true, A = a, B = b, Age = 0f, Life = BattleRules.SkillSeamTime, Flare = 0f, Seed = random.Next(1000), Tint = c.Tint, Hot = c.Hot };
        }

        private void AddThrust(Vector2 tail, Vector2 tip, float width, HitColors c)
        {
            int slot = Free(thrusts);
            thrusts[slot] = new Thrust { On = true, Tail = tail, Tip = tip, Width = width, Age = 0f, Tint = c.Tint, Hot = c.Hot };
        }

        private void AddPulse(Vector2 at, float size, float squash, float width, float length, float wait, Color tint)
        {
            int slot = Free(pulses);
            pulses[slot] = new Pulse { On = true, At = at, Wait = wait, Size = size, Squash = squash, Width = width, Length = length, Tint = tint };
        }

        private static int Free(Seam[] pool) { for (int i = 0; i < pool.Length; i++) if (!pool[i].On) return i; return 0; }
        private static int Free(Thrust[] pool) { for (int i = 0; i < pool.Length; i++) if (!pool[i].On) return i; return 0; }
        private static int Free(Saw[] pool) { for (int i = 0; i < pool.Length; i++) if (!pool[i].On) return i; return 0; }
        private static int Free(Pulse[] pool) { for (int i = 0; i < pool.Length; i++) if (!pool[i].On) return i; return 0; }

        //======================================================================================
        //Update and Clear
        //======================================================================================

        //Update Skills : the cuts and the spikes are the moment itself, they play through the hit stop. The seams cool,
        //the saw spins, and the waiting rings wait, only when the world moves.
        private void UpdateSkills(float dt, bool frozen)
        {
            for (int i = 0; i < spaceCuts.Length; i++)
            {
                if (!spaceCuts[i].On) continue;
                spaceCuts[i].Age += dt;
                if (spaceCuts[i].Age > CutLife(spaceCuts[i].Mode)) spaceCuts[i].On = false;
            }
            for (int i = 0; i < thrusts.Length; i++)
            {
                if (!thrusts[i].On) continue;
                thrusts[i].Age += dt;
                if (thrusts[i].Age > ThrustOut + ThrustHold + ThrustFade) thrusts[i].On = false;
            }
            gatherFresh += dt;
            gatherClock += dt;
            if (gatherFresh > 0.2f) gatherPower = Math.Max(0f, gatherPower - dt * 3f);
            if (frozen) return;

            for (int i = 0; i < seams.Length; i++)
            {
                if (!seams[i].On) continue;
                seams[i].Age += dt;
                seams[i].Flare = Math.Max(0f, seams[i].Flare - dt * 2.2f);
                if (seams[i].Age > seams[i].Life) seams[i].On = false;
            }
            for (int i = 0; i < saws.Length; i++)
            {
                if (!saws[i].On) continue;
                saws[i].Age += dt;
                saws[i].Spin += dt * BattleRules.SkillSawSpin;
                if (saws[i].Age > BattleRules.SkillSawTime) saws[i].On = false;
                else if (random.NextDouble() < dt * 40.0)                                  // grinding : sparks off the bottom edge
                    Throw(saws[i].At + new Vector2(0f, saws[i].Radius), new Vector2(-1f, 0.1f), 2, 520f, Streak, new HitColors(saws[i].Tint, saws[i].Hot, HitFlair.None));
            }
            for (int i = 0; i < pulses.Length; i++)
            {
                if (!pulses[i].On) continue;
                pulses[i].Wait -= dt;
                if (pulses[i].Wait > 0f) continue;
                pulses[i].On = false;
                AddRing(pulses[i].At, pulses[i].Size, pulses[i].Squash, pulses[i].Width, pulses[i].Length, pulses[i].Tint);
            }
        }

        private void ClearSkills()
        {
            for (int i = 0; i < spaceCuts.Length; i++) spaceCuts[i].On = false;
            for (int i = 0; i < seams.Length; i++) seams[i].On = false;
            for (int i = 0; i < thrusts.Length; i++) thrusts[i].On = false;
            for (int i = 0; i < saws.Length; i++) saws[i].On = false;
            for (int i = 0; i < pulses.Length; i++) pulses[i].On = false;
            gatherPower = 0f;
        }

        //======================================================================================
        //SPACE CUT for the post pass
        //======================================================================================

        //The cut's times : snaps open, holds, closes slowly (fast in, slow out)
        private const float CutOpenTime = 0.05f;
        private const float CutHoldTime = 0.09f;
        private const float CutCloseTime = 0.3f;

        //Cut Open : 0..1, how far a cut of this age is open
        private static float CutOpen(float age)
        {
            if (age < 0f) return 0f;
            if (age < CutOpenTime) return Ease(age / CutOpenTime);
            if (age < CutOpenTime + CutHoldTime) return 1f;
            float t = (age - CutOpenTime - CutHoldTime) / CutCloseTime;
            if (t >= 1f) return 0f;
            return 1f - t * t * (3f - 2f * t);
        }

        //Cut Life : how long a cut of this mode stays
        private static float CutLife(byte mode)
        {
            if (mode == CutWaiting) return BattleRules.FinaleCrackWait + CrackFadeTime;
            if (mode == CutBroken) return BreakEnd;
            return CutOpenTime + CutHoldTime + CutCloseTime;
        }

        //Cut Open Of : how far a cut is open now, by its mode
        private static float CutOpenOf(SpaceCut c)
        {
            if (c.Mode == CutWaiting)
            {
                float fade = 1f - MathHelper.Clamp((c.Age - BattleRules.FinaleCrackWait) / CrackFadeTime, 0f, 1f);
                return BattleRules.FinaleCrackHair * Math.Min(1f, c.Age / 0.04f) * fade;
            }
            if (c.Mode == CutBroken) return BreakOpen(c.Age);
            return CutOpen(c.Age);
        }

        //Cut Reach Of : a written crack runs out across the stage from where it was cut (FinaleCrackDraw)
        private static float CutReachOf(SpaceCut c)
        {
            if (c.Mode != CutWaiting) return c.Reach;
            return c.Reach * Ease(Math.Min(1f, c.Age / BattleRules.FinaleCrackDraw));
        }

        //Cuts Now : the SPACE CUTS open this frame, for Post.fx (Core/PostChain.cs SetCuts). They stand on the page in the
        //fighters' layer; shift is how far that layer has moved with the camera. LESS FLASHING opens them half as far.
        //Returns how far THE BREAK has dropped the world's pieces (CutFall, 0 when nothing is broken).
        public float CutsNow(Vector2 shift, Vector4[] where, Vector4[] tint, Vector4[] shape)
        {
            float soft = Settings.ReduceFlashing ? 0.5f : 1f;
            int n = 0;
            for (int i = 0; i < spaceCuts.Length && n < where.Length; i++)
            {
                if (!spaceCuts[i].On) continue;
                float open = CutOpenOf(spaceCuts[i]);
                if (open <= 0.001f) continue;
                Vector2 at = spaceCuts[i].At - shift;
                Color c = spaceCuts[i].Tint;
                float angle = spaceCuts[i].Angle;
                where[n] = new Vector4(at.X / TacetGame.ScreenW, at.Y / TacetGame.ScreenH, (float)Math.Cos(angle), (float)Math.Sin(angle));
                tint[n] = new Vector4(c.R / 255f, c.G / 255f, c.B / 255f, CutReachOf(spaceCuts[i]) / TacetGame.ScreenH);
                shape[n] = new Vector4(spaceCuts[i].Slide / TacetGame.ScreenH, spaceCuts[i].Gap / TacetGame.ScreenH, open * soft, spaceCuts[i].Prism);
                n++;
            }
            for (int i = n; i < where.Length; i++)
            {
                where[i] = Vector4.Zero;
                tint[i] = Vector4.Zero;
                shape[i] = Vector4.Zero;
            }
            return BreakFall() * soft;
        }

        //======================================================================================
        //Draw
        //======================================================================================

        //The spike's times : out in two frames, holds, then thins away from its tail
        private const float ThrustOut = 0.035f;
        private const float ThrustHold = 0.06f;
        private const float ThrustFade = 0.22f;

        private void DrawSkills(SpriteBatch sb)
        {
            for (int i = 0; i < seams.Length; i++) if (seams[i].On) DrawSeam(sb, seams[i]);
            for (int i = 0; i < saws.Length; i++) if (saws[i].On) DrawSaw(sb, saws[i]);
            for (int i = 0; i < thrusts.Length; i++) if (thrusts[i].On) DrawThrust(sb, thrusts[i]);
            if (gatherPower > 0.01f) DrawGather(sb);
        }

        //Draw Seam : white hot when it is cut or flared, cooling to the legend's colour, then a dark ember, flickering in six
        //pieces along its length like a burning line
        private static void DrawSeam(SpriteBatch sb, Seam s)
        {
            float t = s.Age / s.Life;
            float heat = Math.Max(s.Flare, 1f - Math.Min(1f, t * 3f));                      // white for the first third, or flared
            float left = 1f - t;
            const int pieces = 6;
            for (int k = 0; k < pieces; k++)
            {
                Vector2 a = Vector2.Lerp(s.A, s.B, k / (float)pieces);
                Vector2 b = Vector2.Lerp(s.A, s.B, (k + 1) / (float)pieces);
                float flicker = 0.65f + 0.35f * (float)Math.Sin(s.Age * 23f + s.Seed + k * 1.9f);
                float edge = (float)Math.Sin(Math.PI * (k + 0.5f) / pieces);                 // the ends are thinner
                float lit = left * flicker * (0.4f + 0.6f * edge);
                Color core = Color.Lerp(s.Tint, Color.White, heat);
                Gfx.Line(sb, Snap(a), Snap(b), s.Tint * (0.22f * lit), 14f * edge + 4f);
                Gfx.Line(sb, Snap(a), Snap(b), Color.Lerp(s.Tint, s.Hot, 0.5f + 0.5f * heat) * (0.7f * lit), 4f);
                Gfx.Line(sb, Snap(a), Snap(b), core * lit, 2f);
            }
            if (s.Flare > 0f)
            {
                Vector2 mid = (s.A + s.B) * 0.5f;
                Gfx.DrawGlow(sb, mid.X, mid.Y, Vector2.Distance(s.A, s.B) * 0.7f, s.Hot * (0.5f * s.Flare));
            }
        }

        //Draw Thrust : a flat spike, wide at its tail and sharp at its tip, a soft wide layer of colour under a hot one, a
        //white core. It shoots out from the tail, holds, then fades from the tail forward and leaves a thin line.
        private static void DrawThrust(SpriteBatch sb, Thrust s)
        {
            float reach = Ease(Math.Min(1f, s.Age / ThrustOut));
            float fade = MathHelper.Clamp((s.Age - ThrustOut - ThrustHold) / ThrustFade, 0f, 1f);
            Vector2 tip = Vector2.Lerp(s.Tail, s.Tip, reach);
            Vector2 tail = Vector2.Lerp(s.Tail, tip, Ease(fade));
            float width = s.Width * (1f - 0.75f * fade);
            const int steps = 14;
            Vector2 last = tail;
            for (int k = 1; k <= steps; k++)
            {
                float t = k / (float)steps;
                Vector2 p = Vector2.Lerp(tail, tip, t);
                float w = width * (float)Math.Pow(1f - (t - 0.5f / steps), 0.55);          // wide at the tail, sharp at the tip
                //the legend's colour must show on the spike (37.2 : colour on the edge, white only in the core)
                Gfx.Line(sb, Snap(last), Snap(p), s.Tint * (0.6f * (1f - fade)), Math.Max(2f, w * 2.4f));
                Gfx.Line(sb, Snap(last), Snap(p), Color.Lerp(s.Tint, s.Hot, 0.45f) * (1f - 0.5f * fade), Math.Max(2f, w));
                Gfx.Line(sb, Snap(last), Snap(p), Color.White * (1f - fade), Math.Max(1f, w * 0.3f));
                last = p;
            }
            if (fade < 0.5f) Gfx.DrawGlow(sb, tip.X, tip.Y, s.Width * 2.2f, s.Hot * (0.7f * (1f - 2f * fade)));   // the point, brightest
        }

        //Draw Saw : three arcs of light spinning fast round the point, with a soft glow under them, fading at the end
        private static void DrawSaw(SpriteBatch sb, Saw s)
        {
            float t = s.Age / BattleRules.SkillSawTime;
            float grow = Ease(Math.Min(1f, t * 5f));
            float a = 1f - t * t;
            float r = s.Radius * grow;
            Gfx.DrawGlow(sb, s.At.X, s.At.Y, r * 1.6f, s.Tint * (0.35f * a));
            for (int arc = 0; arc < 3; arc++)
            {
                float from = s.Spin + arc * MathHelper.TwoPi / 3f;
                Vector2 last = s.At + new Vector2((float)Math.Cos(from), (float)Math.Sin(from)) * r;
                const int steps = 8;
                for (int k = 1; k <= steps; k++)
                {
                    float angle = from + 1.5f * k / steps;
                    Vector2 p = s.At + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * r;
                    float thin = (float)Math.Sin(Math.PI * k / (steps + 1));
                    Gfx.Line(sb, Snap(last), Snap(p), s.Tint * (0.6f * a), 9f * thin + 3f);
                    Gfx.Line(sb, Snap(last), Snap(p), s.Hot * a, 3f);
                    last = p;
                }
            }
        }

        //Draw Gather : the ball of fire growing at the f hole, a ring tightening round it as the hold goes on. The white core
        //is the one focus while it is held (37.2), so it is the brightest thing on the stage until the blast takes over.
        //Four short lines run in towards it (the pull Miyabi's draw has before the cut), faster as it fills.
        private void DrawGather(SpriteBatch sb)
        {
            float p = gatherPower;
            float breathe = 0.85f + 0.15f * (float)Math.Sin(gatherFresh * 30f + p * 20f);
            Gfx.DrawGlow(sb, gatherAt.X, gatherAt.Y, BattleRules.SkillGatherGlow * (0.5f + 0.9f * p), gatherColors.Tint * (0.8f * p * breathe));
            Gfx.DrawGlow(sb, gatherAt.X, gatherAt.Y, BattleRules.SkillGatherGlow * (0.2f + 0.3f * p), gatherColors.Hot * (1.2f * p));
            Gfx.DrawGlow(sb, gatherAt.X, gatherAt.Y, BattleRules.SkillGatherGlow * (0.08f + 0.12f * p), Color.White * (p * p));
            float ring = BattleRules.SkillGatherReach * (1f - 0.7f * p);
            Gfx.EllipseOutline(sb, gatherAt.X, gatherAt.Y, ring, ring, gatherColors.Hot * (0.8f * p), 3f);
            Gfx.EllipseOutline(sb, gatherAt.X, gatherAt.Y, ring * 1.35f, ring * 1.35f, gatherColors.Tint * (0.35f * p), 2f);
            float run = (gatherClock * (2f + 3f * p)) % 1f;                                   // how far in the pull lines are
            for (int i = 0; i < 4; i++)
            {
                float angle = i * MathHelper.PiOver2 + 0.4f + gatherClock * 0.7f;
                Vector2 way = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                float far = BattleRules.SkillGatherReach * (1.3f - run);
                Vector2 a = gatherAt + way * far;
                Vector2 b = gatherAt + way * Math.Max(6f, far - 22f - 30f * p);
                Gfx.Line(sb, Snap(a), Snap(b), gatherColors.Hot * (0.7f * p * (1f - run * 0.6f)), 2f);
            }
        }
    }
}
