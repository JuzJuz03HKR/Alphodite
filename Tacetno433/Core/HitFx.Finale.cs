using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //HitFx Finale (Beta1 block D item 2, 2 Nov, docs/BETA1_DIRECTION.md 39) : FANNY's FINALE in its own look, PRISM, and the
    //GREAT BOLT of THE UNHEARING's grown FINALE. The user named her theme (13.87) : the rainbow, prism cracks through the
    //dimension, her brother's cut purple, blue and black. FINALE GROWS (the user 13.119) : each conductor's FINALE looks
    //bigger after each boss, the way its strength grows (SIGNATURE GROWS, Battle/TurnBattle.cs) :
    //   TWO VOICES  the cracks open and the world is cut along them (it slides apart, black in the cuts), a few splinters
    //   DAS JAHR    the cut world breaks like glass : its pieces fall, glass is thrown off, the twelve months light up
    //   DAS JAHR II the pieces fall further and wider apart, twice the glass, every month throws its light at the middle
    //   STORM II    THE UNHEARING's lightning, red since STORM, strikes as one huge bolt that splits over every enemy
    //   CRACKS     each stroke of the 4/4 writes a crack right across the stage (a SPACE CUT, Post.fx) : a hairline of black
    //              with the rainbow running along its edges. The first three are FANNY's, the fourth is her brother's voice
    //              answering (TWO VOICES), its edges burning purple and blue. They wait, written, for the strike.
    //   THE BREAK  at the strike every crack opens at once and the world's picture comes apart along them : the pieces slide
    //              (and from DAS JAHR drop, Post.fx CutFall), black between them, every piece's edge catching the rainbow.
    //              Pieces of prism glass are thrown off the cracks and fall turning. Then the world closes again.
    //   PRISM      on each enemy struck : a white star, spikes in every colour of the rainbow, glass splinters, a ring
    //   MONTHS     DAS JAHR (FANNY's twelve pieces for the twelve months) : a ring of twelve lights round the enemies, lit
    //              one month after another. DAS JAHR II : every month throws its light at the middle, the break goes further.
    //From DMC's Judgement Cut End (the screen breaking like glass, docs/art_refs/vfx/dmc_jce_*.jpg). TACET's own : the glass
    //is the world's picture and what shows between its pieces is the silence behind it, the rainbow is Fanny's crystal, and
    //the 4/4 the player beats writes the cracks.
    //THE METRONOME's FINALE (HOLY, the stars) is drawn over the world by the screen (Screens/BattleScreen.Finale.cs).
    //Every piece lives in a fixed pool, nothing is made in a fight. Numbers : Data/BattleRules.cs, Finale Feel.
    public partial class HitFx
    {
        //THE BREAK : its clock (99 = nothing broken) and how far its pieces drop this time (DAS JAHR II further)
        private float breakAge = 99f;
        private float breakFall;
        private const float BreakOpenTime = 0.06f;                 // it snaps open in this long
        private const float CrackFadeTime = 0.3f;                  // a crack nobody broke closes in this long
        private static float BreakEnd { get { return BreakOpenTime + BattleRules.FinaleBreakHold + BattleRules.FinaleBreakClose; } }

        //Crack Reach : a crack runs right across the stage and past both edges, so it is WHOLE in Post.fx (longer than the
        //screen) and the world breaks along it
        private const float CrackReach = 1700f;

        //Glass : a piece of prism glass thrown off a crack at THE BREAK. Four corners round its middle (its own shape from
        //Seed), it turns as it falls and flips (Flip squeezes it across, so it seems to turn in depth), its face nearly clear,
        //its edges white with the rainbow along one, a glint when it faces the light. Hue below 0 = her brother's glass.
        private struct Glass
        {
            public bool On;
            public Vector2 At, Speed;
            public float Size, Spin, Turn, Flip, FlipSpeed, Age, Life, Hue;
            public int Seed;
        }
        private readonly Glass[] glass = new Glass[72];
        private readonly Vector2[] glassCorners = new Vector2[4];     // one piece's corners while it is drawn (no garbage)

        //Great Bolt : STORM II. One channel from the sky down to over the enemies, where it splits into a branch for each
        //enemy struck, and two forks off its side. It strikes, goes out, strikes again and glows away like a bolt (Bolt Light),
        //only longer.
        private const int GreatPoints = 33;
        private const int GreatBranchPoints = 17;
        private const int GreatBranches = 8;
        private const float GreatLife = 1.4f;
        private readonly Vector2[] greatMain = new Vector2[GreatPoints];
        private readonly Vector2[][] greatBranch = MakeGreatBranches();
        private int greatBranches;
        private float greatAge = 99f;
        private Color greatTint, greatHot;

        private static Vector2[][] MakeGreatBranches()
        {
            Vector2[][] b = new Vector2[GreatBranches][];
            for (int i = 0; i < GreatBranches; i++) b[i] = new Vector2[GreatBranchPoints];
            return b;
        }

        //Months : DAS JAHR, the ring of the twelve months round the enemies, lit one after another at THE BREAK
        private Vector2 monthsAt;
        private float monthsAge = 99f;
        private int monthsLevel;
        private const float MonthStep = 0.05f;                     // a month lights this long after the one before
        private const float MonthsLife = 1.8f;

        //======================================================================================
        //The cracks and THE BREAK
        //======================================================================================

        //Prism Crack : a stroke of FANNY's FINALE writes a crack across the stage through at, going the way of angle.
        //brother : the fourth stroke, her brother's voice (purple and blue, not the rainbow). It runs across the stage in
        //FinaleCrackDraw and stands as a hairline until THE BREAK opens it (Prism Break), or closes after FinaleCrackWait.
        public void PrismCrack(Vector2 at, float angle, bool brother)
        {
            int slot = CutSlot();
            spaceCuts[slot] = new SpaceCut
            {
                On = true,
                Mode = CutWaiting,
                At = at,
                Angle = angle,
                Age = 0f,
                Reach = CrackReach,
                Slide = BattleRules.FinaleBreakSlide,
                Gap = BattleRules.FinaleBreakGap,
                Prism = brother ? 0f : 1f,
                Tint = brother ? Palette.SpotBrother : Color.White,
            };

            //The stroke itself : a white cut across the stage for a moment, light where the baton cut it, sparks along it
            HitColors c = brother ? new HitColors(Palette.SpotBrother, Palette.SpotBrotherDeep, HitFlair.None)
                                  : new HitColors(Palette.SpotPrism, Color.White, HitFlair.None);
            Vector2 way = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            AddCut(at - way * 760f, at + way * 760f, 3f, 0f, c);
            AddBurst(at, BattleRules.HitSize * 0.6f, angle, 2.6f, 8, 3f, 0.2f, c);
            Throw(at, way, 8, 640f, Streak, c);
            Throw(at, -way, 8, 640f, Streak, c);
        }

        //Prism Break : FANNY's FINALE strikes. Every crack written opens at once and the world falls apart along them, glass is
        //thrown off them, a star and two rings where the enemies stand (center), and at DAS JAHR the ring of the months.
        //level : the FINALE's level (0, DAS JAHR 1, DAS JAHR II 2).
        public void PrismBreak(Vector2 center, int level)
        {
            int cracks = 0;
            for (int i = 0; i < spaceCuts.Length; i++)
                if (spaceCuts[i].On && spaceCuts[i].Mode == CutWaiting) cracks++;
            if (cracks == 0)                                                            // every stroke missed : it still breaks once
            {
                PrismCrack(center, -0.45f, false);
                cracks = 1;
            }

            //FINALE GROWS : TWO VOICES cuts the world, DAS JAHR breaks it like glass, DAS JAHR II further and with twice the glass
            breakAge = 0f;
            breakFall = level <= 0 ? 0f : BattleRules.FinaleBreakFall * (level >= 2 ? 1.5f : 1f);
            int pieces = level <= 0 ? BattleRules.FinaleGlass / 3 : BattleRules.FinaleGlass * (level >= 2 ? 2 : 1);
            if (Settings.ReduceFlashing) pieces /= 2;
            for (int i = 0; i < spaceCuts.Length; i++)
            {
                if (!spaceCuts[i].On || spaceCuts[i].Mode != CutWaiting) continue;
                spaceCuts[i].Mode = CutBroken;
                spaceCuts[i].Age = 0f;
                if (level >= 2) spaceCuts[i].Gap *= 1.3f;
                ThrowGlass(spaceCuts[i].At, spaceCuts[i].Angle, pieces / cracks, spaceCuts[i].Prism < 0.5f ? BrotherGlass : -0.5f);
            }

            HitColors white = new HitColors(Palette.SpotPrism, Color.White, HitFlair.None);
            Star(center, BattleRules.HitSize * 2.6f, white);
            AddRing(center, BattleRules.HitSize * 2.2f, 1f, 4f, 0.5f, Color.White);
            AddRing(center, BattleRules.HitSize * 3.4f, 1f, 2f, 0.65f, Palette.SpotPrism);
            Focus(center);
            ImpactFrame(center);
            if (level >= 1)
            {
                monthsAt = center;
                monthsAge = 0f;
                monthsLevel = level;
            }
        }

        //Throw Glass : count pieces of glass off the part of a crack that is on the stage, each flung to one side and up.
        //hue : BrotherGlass, WhiteGlass, or below 0 otherwise for a random colour of the rainbow each
        private void ThrowGlass(Vector2 at, float angle, int count, float hue)
        {
            Vector2 way = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            Vector2 across = new Vector2(-way.Y, way.X);
            for (int i = 0; i < count; i++)
            {
                int slot = Free(glass);
                Vector2 p = at + way * ((float)random.NextDouble() * 2f - 1f) * 640f;
                if (p.X < 0f || p.X > TacetGame.ScreenW || p.Y < 0f || p.Y > TacetGame.ScreenH)
                    p = at + way * ((float)random.NextDouble() - 0.5f) * 320f;               // keep it where it can be seen
                float side = random.Next(2) == 0 ? -1f : 1f;
                glass[slot] = new Glass
                {
                    On = true,
                    At = p,
                    Speed = across * side * (90f + 230f * (float)random.NextDouble()) + new Vector2(0f, -(80f + 260f * (float)random.NextDouble())),
                    Size = 7f + 16f * (float)random.NextDouble(),
                    Spin = (float)(random.NextDouble() * MathHelper.TwoPi),
                    Turn = ((float)random.NextDouble() - 0.5f) * 8f,
                    Flip = (float)(random.NextDouble() * MathHelper.TwoPi),
                    FlipSpeed = 3f + 9f * (float)random.NextDouble(),
                    Age = -0.1f * (float)random.NextDouble(),
                    Life = 0.9f + 0.8f * (float)random.NextDouble(),
                    Hue = hue == BrotherGlass || hue == WhiteGlass ? hue : (float)random.NextDouble(),
                    Seed = random.Next(1000),
                };
            }
        }

        //Great Bolt : STORM II strikes (the screen hands it every enemy struck, count of them in boxes). The channel comes down
        //from high over their middle and splits a little above them into a branch for each, two forks off its side; each enemy
        //gets the light of a bolt landing (spikes, a star, rings on the floor, sparks) without a bolt of its own.
        public void GreatBolt(Rectangle[] boxes, int count, HitColors c)
        {
            if (count <= 0) return;
            Vector2 mid = Vector2.Zero;
            float high = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                mid += new Vector2(boxes[i].Center.X, boxes[i].Y + boxes[i].Height * 0.3f);
                high = Math.Min(high, boxes[i].Y);
            }
            mid /= count;
            Vector2 split = new Vector2(mid.X, Math.Max(high + 10f, mid.Y - 40f));            // low, near their heads : the one channel is long
            Vector2 top = new Vector2(mid.X + ((float)random.NextDouble() - 0.5f) * 260f, -140f);
            MakeChannel(greatMain, GreatPoints, top, split, 0.35f);

            greatBranches = 0;
            for (int i = 0; i < count && greatBranches < GreatBranches - 2; i++)
            {
                Vector2 struck = new Vector2(boxes[i].Center.X, boxes[i].Y + boxes[i].Height * 0.3f);
                MakeChannel(greatBranch[greatBranches++], GreatBranchPoints, split, struck, 0.5f);
                GreatLanding(boxes[i], c);
            }
            for (int k = 0; k < 2; k++)                                                        // two forks off the side of the channel
            {
                int from = 10 + random.Next(14);
                float side = k == 0 ? -1f : 1f;
                Vector2 along = greatMain[from + 1] - greatMain[from - 1];
                float angle = (float)Math.Atan2(along.Y, along.X) + side * (0.5f + 0.4f * (float)random.NextDouble());
                Vector2 end = greatMain[from] + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * (140f + 120f * (float)random.NextDouble());
                MakeChannel(greatBranch[greatBranches++], GreatBranchPoints, greatMain[from], end, 0.6f);
            }
            greatAge = 0f;
            greatTint = c.Tint;
            greatHot = c.Hot;
            Star(split, BattleRules.HitSize * 2.4f, c);
            AddRing(split, BattleRules.HitSize * 2.6f, 1f, 4f, 0.5f, c.Tint);
            Focus(split);
            ImpactFrame(split);
        }

        //Great Landing : where a branch of the great bolt lands, the light of a strike, bigger than one bolt's
        private void GreatLanding(Rectangle box, HitColors c)
        {
            float size = BattleRules.HitSize * 1.25f;
            Vector2 feet = new Vector2(box.Center.X, box.Bottom - 6f);
            Vector2 struck = new Vector2(box.Center.X, box.Y + box.Height * 0.3f);
            AddBurst(struck, size * 1.6f, -MathHelper.PiOver2, 1.6f, 22, 6f, 0.42f, c);
            Star(struck, size * 2.1f, c);
            AddRing(feet, size * 2.8f, 0.24f, 5f, 0.6f, c.Tint);
            AddRing(feet, size * 1.6f, 0.24f, 3f, 0.42f, c.Hot);
            Throw(struck, new Vector2(0f, 1f), 18, 600f, Streak, c);
            Throw(feet, new Vector2(0f, -1f), 22, 400f, Spark, c);
            Throw(struck, new Vector2(0f, -1f), 12, 460f, Shard, c);
        }

        //Great Light : how bright the great bolt is at age t. It strikes and flickers through the IMPACT FRAME, then strikes
        //twice more after it (so its red is seen once the world has turned back), then glows away.
        private static float GreatLight(float t)
        {
            if (t < 0.07f) return 1f;
            if (t < 0.1f) return 0.15f;
            if (t < 0.17f) return 1f;
            if (t < 0.36f) return 0.35f;
            if (t < 0.46f) return 1f;
            if (t < 0.5f) return 0.25f;
            if (t < 0.62f) return 1f;
            float k = 1f - (t - 0.62f) / (GreatLife - 0.62f);
            return k > 0f ? 0.8f * k * k : 0f;
        }

        //======================================================================================
        //THE SILENCE BREAKS (block D item 5, the bosses) : TACET's own way of breaking the world, white and grey, no rainbow
        //(the world is TACET's colours, the rainbow is the band's). Used by Screens/BattleScreen.Boss.cs.
        //======================================================================================

        //Silence Break : count cracks through at, spread round it, opening at once (THE BREAK's look), the pieces dropping by
        //fall (screen heights, 0 = they only slide apart), white glass thrown off them. THE TURN and a boss SILENCED.
        public void SilenceBreak(Vector2 at, int count, float fall, int glassCount)
        {
            count = Math.Max(1, Math.Min(count, spaceCuts.Length));
            for (int i = 0; i < count; i++)
            {
                float angle = -1.25f + i * (2.5f / count) + ((float)random.NextDouble() - 0.5f) * 0.25f;
                Vector2 off = new Vector2(((float)random.NextDouble() - 0.5f) * 60f, ((float)random.NextDouble() - 0.5f) * 40f);
                int slot = CutSlot();
                spaceCuts[slot] = new SpaceCut
                {
                    On = true,
                    Mode = CutBroken,
                    At = at + off,
                    Angle = angle,
                    Age = 0f,
                    Reach = CrackReach,
                    Slide = BattleRules.FinaleBreakSlide * 0.8f,
                    Gap = BattleRules.FinaleBreakGap,
                    Prism = 0f,
                    Tint = Color.White,
                };
                ThrowGlass(at + off, angle, glassCount / count, WhiteGlass);                // TACET's glass is white
            }
            breakAge = 0f;
            breakFall = fall;
        }

        //Silence Cut : one long white cut torn across the stage through at (a boss's BIG ATTACK coming), held open as THE
        //BREAK is, without falling
        public void SilenceCut(Vector2 at, float angle)
        {
            int slot = CutSlot();
            spaceCuts[slot] = new SpaceCut
            {
                On = true,
                Mode = CutBroken,
                At = at,
                Angle = angle,
                Age = 0f,
                Reach = CrackReach,
                Slide = BattleRules.FinaleBreakSlide,
                Gap = BattleRules.FinaleBreakGap * 1.4f,
                Prism = 0f,
                Tint = Color.White,
            };
            breakAge = 0f;
            breakFall = 0f;
            Vector2 way = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            AddCut(at - way * 800f, at + way * 800f, 4f, 0f, HitColors.Plain);
            Throw(at, way, 14, 700f, Streak, HitColors.Plain);
            Throw(at, -way, 14, 700f, Streak, HitColors.Plain);
        }

        //Boss Rise : ENTRANCE, the monument rising out of the hole : a white pillar of light up from where it stands, rings on
        //the floor opening one after another, shards thrown up, lines rushing in
        public void BossRise(Rectangle box)
        {
            Vector2 feet = new Vector2(box.Center.X, box.Bottom - 6f);
            Vector2 middle = new Vector2(box.Center.X, box.Center.Y);
            float size = BattleRules.HitSize;
            AddCut(feet + new Vector2(0f, 20f), new Vector2(feet.X, -60f), 30f, 0f, HitColors.Plain);
            AddCut(feet + new Vector2(0f, 20f), new Vector2(feet.X, -60f), 10f, -0.04f, HitColors.Plain);
            for (int k = 0; k < 3; k++) AddPulse(feet, size * (2.4f + 1.4f * k), 0.24f, 5f - k, 0.6f, k * 0.12f, Color.White);
            Throw(feet, new Vector2(0f, -1f), 30, 620f, Shard, HitColors.Plain);
            Throw(middle, new Vector2(0f, -1f), 16, 420f, Spark, HitColors.Plain);
            Focus(middle);
        }

        private const float BrotherGlass = -1f;                    // Glass.Hue for her brother's purple glass
        private const float WhiteGlass = -2f;                      //   and for TACET's white glass

        //Finale Hit : a FINALE's blow on one enemy (called by Hit). PRISM : a white star, spikes in every colour of the
        //rainbow round it, splinters of glass in those colours, a ring, and the IMPACT FRAME.
        private bool FinaleHit(HitStyle style, Rectangle box, float power)
        {
            if (style != HitStyle.Prism) return false;
            Vector2 at = new Vector2(box.Center.X, box.Y + box.Height * 0.45f);
            float size = (0.75f + 0.35f * power) * BattleRules.HitSize;
            Star(at, size * 2f, new HitColors(Palette.SpotPrism, Color.White, HitFlair.None));
            for (int k = 0; k < 6; k++)
            {
                Color hue = Palette.Prism(k / 6f);
                HitColors c = new HitColors(hue, Color.Lerp(hue, Color.White, 0.6f), HitFlair.None);
                float angle = k * MathHelper.TwoPi / 6f + 0.3f;
                AddBurst(at, size * (1.05f + 0.07f * k), angle, 1.5f, 3, 3f, 0.36f, c);
                Throw(at, new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)), 3, 540f, Shard, c);
            }
            AddRing(at, size * 1.6f, 1f, 3f, 0.42f, Palette.Prism((float)random.NextDouble()));
            AddRing(at, size * 1.0f, 1f, 2f, 0.3f, Color.White);
            ImpactFrame(at);
            return true;
        }

        //Cut Slot : the cut to write over, a free one or else the oldest
        private int CutSlot()
        {
            int slot = 0;
            float oldest = -1f;
            for (int i = 0; i < spaceCuts.Length; i++)
            {
                if (!spaceCuts[i].On) return i;
                if (spaceCuts[i].Age > oldest)
                {
                    oldest = spaceCuts[i].Age;
                    slot = i;
                }
            }
            return slot;
        }

        //Break Open : 0..1, how far THE BREAK is open at age t : snaps open, stays, closes slowly
        private static float BreakOpen(float t)
        {
            if (t < 0f) return 0f;
            if (t < BreakOpenTime) return Ease(t / BreakOpenTime);
            float hold = BattleRules.FinaleBreakHold;
            if (t < BreakOpenTime + hold) return 1f;
            float k = (t - BreakOpenTime - hold) / BattleRules.FinaleBreakClose;
            if (k >= 1f) return 0f;
            return 1f - k * k * (3f - 2f * k);
        }

        //Break Fall : how far the pieces have dropped. They fall faster and faster while it stays broken (like a weight,
        //t squared) and rise back into place as it closes : the world is whole again.
        private float BreakFall()
        {
            if (breakAge >= BreakEnd) return 0f;
            float t = MathHelper.Clamp(breakAge / (BreakOpenTime + BattleRules.FinaleBreakHold), 0f, 1f);
            return breakFall * t * t * BreakOpen(breakAge);
        }

        //======================================================================================
        //Update, Clear, Draw
        //======================================================================================

        //Update Finale : the clocks run through the hit stop (they are the moment), the glass holds still in it like the bits
        private void UpdateFinale(float dt, bool frozen)
        {
            breakAge += dt;
            monthsAge += dt;
            greatAge += dt;
            if (frozen) return;
            float drag = (float)Math.Pow(0.5, dt);
            for (int i = 0; i < glass.Length; i++)
            {
                if (!glass[i].On) continue;
                glass[i].Age += dt;
                if (glass[i].Age > glass[i].Life)
                {
                    glass[i].On = false;
                    continue;
                }
                if (glass[i].Age < 0f) continue;
                glass[i].Speed.Y += 640f * dt;                                             // it falls
                glass[i].Speed.X *= drag;
                glass[i].At += glass[i].Speed * dt;
                glass[i].Spin += glass[i].Turn * dt;
                glass[i].Flip += glass[i].FlipSpeed * dt;
            }
        }

        private void ClearFinale()
        {
            for (int i = 0; i < glass.Length; i++) glass[i].On = false;
            breakAge = 99f;
            monthsAge = 99f;
            greatAge = 99f;
        }

        private static int Free(Glass[] pool) { for (int i = 0; i < pool.Length; i++) if (!pool[i].On) return i; return 0; }

        //Draw Finale : into the GLOW, under the cuts and bursts : the written cracks (part of the world, they open with it) and
        //the great bolt (wide and thick : the main channel 2.6 times a bolt, each branch 1.5)
        private void DrawFinale(SpriteBatch sb)
        {
            for (int i = 0; i < spaceCuts.Length; i++)
                if (spaceCuts[i].On && spaceCuts[i].Mode == CutWaiting) DrawCrack(sb, spaceCuts[i]);
            if (greatAge < GreatLife)
            {
                float lit = GreatLight(greatAge);
                if (lit > 0.01f)
                {
                    for (int i = 2; i < GreatPoints; i += 4)                                          // a red glow all along the channel
                        Gfx.DrawGlow(sb, greatMain[i].X, greatMain[i].Y, 90f, greatTint * (0.3f * lit));
                    DrawChannel(sb, greatMain, GreatPoints, greatTint, greatHot, lit, 3.4f);
                    for (int k = 0; k < greatBranches; k++)
                        DrawChannel(sb, greatBranch[k], GreatBranchPoints, greatTint, greatHot, lit * (k < greatBranches - 2 ? 0.95f : 0.6f), k < greatBranches - 2 ? 1.8f : 0.8f);
                }
            }
        }

        //Has Over : glass or the months are showing. They fly in front of the broken world, so the screen draws them over the
        //finished picture (DrawOver) : drawn into the GLOW they would be cut and dropped with the world's pieces.
        public bool HasOver
        {
            get
            {
                if (monthsAge < MonthsLife) return true;
                for (int i = 0; i < glass.Length; i++) if (glass[i].On) return true;
                return false;
            }
        }

        //Draw Over : the glass and the months, light added, in the fighters' layer (the screen sets its camera)
        public void DrawOver(SpriteBatch sb)
        {
            for (int i = 0; i < glass.Length; i++)
                if (glass[i].On && glass[i].Age >= 0f) DrawGlass(sb, glass[i]);
            if (monthsAge < MonthsLife) DrawMonths(sb);
        }

        //Draw Crack : a written crack waiting for the break. A thin white line (her brother's purple) as long as it has run,
        //the rainbow split beside it, and a few glints winking along it.
        private void DrawCrack(SpriteBatch sb, SpaceCut c)
        {
            float a = CutOpenOf(c) / BattleRules.FinaleCrackHair;                          // 1 while it waits, fading if nobody breaks it
            if (a <= 0.01f) return;
            Vector2 way = new Vector2((float)Math.Cos(c.Angle), (float)Math.Sin(c.Angle));
            Vector2 side = new Vector2(-way.Y, way.X);
            float reach = CutReachOf(c);
            Vector2 from = c.At - way * reach;
            Vector2 to = c.At + way * reach;
            bool brother = c.Prism < 0.5f;
            Gfx.Line(sb, from, to, (brother ? Palette.SpotBrother : Palette.SpotPrism) * (0.35f * a), 5f);
            for (int k = 0; k < 3; k++)
            {
                Vector2 off = side * ((k - 1) * 2.5f);
                Color hue = brother ? Color.Lerp(Palette.SpotBrother, Palette.SpotBrotherDeep, k / 2f) : Palette.Prism(k / 3f + c.Age * 0.4f);
                Gfx.Line(sb, from + off, to + off, hue * (0.5f * a), 1f);
            }
            Gfx.Line(sb, from, to, Color.White * (0.8f * a), 1.5f);
            for (int k = 0; k < 6; k++)
            {
                float along = (Hash(k, (int)(c.At.X + c.At.Y)) - 0.5f) * Math.Min(reach, 640f) * 2f;
                float wink = (float)Math.Sin(c.Age * (6f + 3f * k) + k * 2.1f);
                if (wink < 0.6f) continue;
                Vector2 p = c.At + way * along;
                float s = 3f + 9f * (wink - 0.6f);
                Gfx.DrawGlow(sb, p.X, p.Y, s * 3f, (brother ? Palette.SpotBrother : Palette.Prism(k / 6f)) * (0.6f * a));
                Gfx.Diamond(sb, p.X, p.Y, s * 0.5f, Color.White * a);
            }
        }

        //Draw Glass : one piece, turning and flipping as it falls, fading at the end of its life
        private void DrawGlass(SpriteBatch sb, Glass g)
        {
            float a = Math.Min(1f, (g.Life - g.Age) / 0.35f) * Math.Min(1f, g.Age / 0.05f + 0.2f);
            float facing = (float)Math.Cos(g.Flip);
            float squeeze = Math.Max(0.15f, Math.Abs(facing));
            float cs = (float)Math.Cos(g.Spin);
            float sn = (float)Math.Sin(g.Spin);
            for (int k = 0; k < 4; k++)
            {
                float angle = k * MathHelper.PiOver2 + (Hash(g.Seed, k) - 0.5f) * 0.9f;
                float r = g.Size * (0.55f + 0.45f * Hash(g.Seed + 5, k));
                Vector2 local = new Vector2((float)Math.Cos(angle) * r * squeeze, (float)Math.Sin(angle) * r);
                glassCorners[k] = g.At + new Vector2(local.X * cs - local.Y * sn, local.X * sn + local.Y * cs);
            }
            bool white = g.Hue <= WhiteGlass + 0.5f;                                          // TACET's glass : white and grey, no rainbow
            bool brother = !white && g.Hue < 0f;
            Color face = white ? Palette.PaperDim : brother ? Palette.SpotBrother : Color.Lerp(Color.White, Palette.Prism(g.Hue), 0.45f);
            Gfx.Quad(sb, glassCorners, face * (0.12f * a));
            for (int k = 0; k < 4; k++)
                Gfx.Line(sb, glassCorners[k], glassCorners[(k + 1) % 4], (brother ? Palette.SpotBrother : Color.White) * (0.6f * a), 1.5f);
            if (white)
            {
                if (facing > 0.9f) Gfx.DrawGlow(sb, g.At.X, g.At.Y, g.Size * 1.6f, Color.White * ((facing - 0.9f) * 7f * a));
                return;
            }

            //The rainbow along one edge : light breaking into its colours, three thin lines side by side
            Vector2 e0 = glassCorners[1];
            Vector2 e1 = glassCorners[2];
            Vector2 along = e1 - e0;
            float length = along.Length();
            if (length > 1f)
            {
                Vector2 off = new Vector2(-along.Y, along.X) / length * 1.5f;
                for (int s = 0; s < 3; s++)
                {
                    Color hue = brother ? Color.Lerp(Palette.SpotBrother, Palette.SpotBrotherDeep, s / 2f) : Palette.Prism(g.Hue + s / 3f);
                    Gfx.Line(sb, e0 + off * (s - 1), e1 + off * (s - 1), hue * (0.85f * a), 1f);
                }
            }
            if (facing > 0.9f) Gfx.DrawGlow(sb, g.At.X, g.At.Y, g.Size * 1.6f, Color.White * ((facing - 0.9f) * 7f * a));   // it faces the light
        }

        //Draw Months : DAS JAHR, the twelve months lit one after another round the ring, a thread joining each to the one before,
        //each in its colour of the rainbow (the year going round). DAS JAHR II : every month throws its light at the middle.
        private void DrawMonths(SpriteBatch sb)
        {
            float rx = BattleRules.FinaleMonthRing;
            float ry = rx * 0.4f;
            float fade = 1f - MathHelper.Clamp((monthsAge - (MonthsLife - 0.6f)) / 0.6f, 0f, 1f);
            Vector2 last = Vector2.Zero;
            for (int k = 0; k < 12; k++)
            {
                float lit = monthsAge - k * MonthStep;
                if (lit < 0f) break;
                float angle = -MathHelper.PiOver2 + k * MathHelper.TwoPi / 12f;
                Vector2 p = monthsAt + new Vector2((float)Math.Cos(angle) * rx, (float)Math.Sin(angle) * ry);
                float pop = lit < 0.12f ? 1f + 0.8f * (1f - lit / 0.12f) : 1f;
                Color hue = Palette.Prism(k / 12f);
                if (k > 0) Gfx.Line(sb, last, p, hue * (0.6f * fade), 2f);
                if (monthsLevel >= 2 && lit > 0.1f) Gfx.Line(sb, p, monthsAt, hue * (0.3f * fade), 1.5f);
                Gfx.DrawGlow(sb, p.X, p.Y, 44f * pop, hue * (0.6f * fade));
                Gfx.Diamond(sb, p.X, p.Y, 8f * pop, Color.Lerp(hue, Color.White, 0.55f) * fade);
                last = p;
            }
        }
    }
}
