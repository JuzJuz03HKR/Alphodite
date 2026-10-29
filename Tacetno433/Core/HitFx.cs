using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //Hit Style : how a blow looks where it lands (HitFx)
    public enum HitStyle { Slash, Shot, Smash, Cross, Ink, Lightning }

    //Hit Flair : what a musician's own colour adds to every blow (round 4, the user asked) :
    //SPARKLE = golden twinkles and glitter, FLAME = tongues of fire and embers
    public enum HitFlair { None, Sparkle, Flame }

    //Hit Colors : a striker's colours. Tint for the glow, Hot for the bright core, and the flair.
    public struct HitColors
    {
        public Color Tint;
        public Color Hot;
        public HitFlair Flair;

        public HitColors(Color tint, Color hot, HitFlair flair)
        {
            Tint = tint;
            Hot = hot;
            Flair = flair;
        }

        public static readonly HitColors Plain = new HitColors(Color.White, Color.White, HitFlair.None);
    }

    //HitFx (Beta1 round 4) : what a blow looks like where it lands. Nothing flies across the screen : the musician
    //strikes and the light happens on the enemy (the user's references, docs/BATTLE_SCENE_2_3D_CHANGE.md 12.5).
    //   SLASH   strings, the bow as a blade : crescents of light sweep across the enemy, a long thin cut, sparks along it
    //   SHOT    winds : sharp spikes burst out of one point, a round glow, a ring, sparks sprayed the way the blow went
    //   SMASH   percussion : a heavy blow from above, a ring on the floor, shards of glass thrown up and falling
    //   CROSS   the SIGNATURE : two cuts across the whole screen crossing on the enemy, a four pointed star where they cross
    //   LIGHTNING  THE UNHEARING's SIGNATURE (round 6) : a bolt from the sky onto every enemy, one after another, striking
    //           again along the same channel, small arcs crawling where it struck
    //   INK     TACET's blow on a musician : white cracks and a ring, dark drops falling
    //   PULL    an enemy SILENCED : specks of it are drawn off into TACET's hole
    //   STAR    a four pointed twinkle, for a perfect note or a big moment
    //   FOCUS   lines rushing in to the point of a big blow, like a comic
    //   IMPACT  a big blow's moment (IMPACT FRAME, Post.fx) : the world turned over black and white, a frame of white,
    //           turned over again, then the white fades while the camera punches in and the world is smeared towards
    //           the blow (round 6 : the user asked for more impact and a little longer)
    //Every blow is in the striker's colours (Hit Colors) : the glow in its colour, the core hot, and its FLAIR on top,
    //golden SPARKLES or FLAMES. Everything is lines, squares and the soft Glow texture, drawn into the GLOW layer so it
    //shines and blooms (the INK drops go into the SCENE). Every piece sits on the art's 2 x 2 grid (PIXEL x2) and the
    //pieces live in fixed pools, so a long fight makes no garbage. Sizes and times are in Data/BattleRules.cs, Hit Feel.
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
            public Color Tint, Hot;
        }

        //Burst : sharp spikes out of one point, longer along the way the blow went (Stretch)
        private struct Burst
        {
            public bool On;
            public Vector2 At;
            public float Size, Angle, Stretch, Width, Age, Length;
            public int Spikes, Seed;
            public Color Tint, Hot;
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
        //turns and glints, INK a dark drop (drawn into the SCENE), PULL a speck drawn towards Target,
        //GLITTER a gold speck drifting down and twinkling (SPARKLE), EMBER a spark of fire rising (FLAME)
        private const byte Spark = 0, Streak = 1, Shard = 2, Ink = 3, Pull = 4, Glitter = 5, Ember = 6;
        private struct Bit
        {
            public bool On;
            public byte Kind;
            public Vector2 At, Speed, Target;
            public float Life, Length, Size, Spin, Turn;
            public Color Tint, Hot;
        }

        //Twinkle : a small four pointed star that pops up and shrinks away (SPARKLE)
        private struct Twinkle
        {
            public bool On;
            public Vector2 At;
            public float Size, Age, Life;
            public Color Tint;
        }

        //Flame : a tongue of fire rising and swaying, white hot when it is born, then the hot colour, then the
        //striker's colour, then dark (FLAME)
        private struct Flame
        {
            public bool On;
            public Vector2 At, Speed;
            public float Size, Age, Life, Sway;
            public Color Tint, Hot;
        }

        //Bolt : one stroke of LIGHTNING. The channel is jagged (MIDPOINT DISPLACEMENT, see Make Channel) with a few
        //branches. It strikes, goes out, strikes again along the same channel (RE-STRIKES, real lightning does this),
        //then fades as an afterglow.
        private const int BoltPoints = 33;          // 2^5 + 1 : the channel is halved five times
        private const int BranchPoints = 9;         // 2^3 + 1
        private const int Branches = 3;
        private const float BoltLife = 0.6f;
        private struct Bolt
        {
            public bool On;
            public Vector2[] Main;
            public Vector2[][] Branch;
            public Vector2 At;         // where it struck
            public float Age, Size;
            public int Seed;
            public Color Tint, Hot;
        }
        private readonly Bolt[] bolts = new Bolt[8];

        //Pending : a blow waiting a moment before it lands (the LIGHTNING strikes one enemy after another)
        private struct Pending
        {
            public bool On;
            public HitStyle Style;
            public Rectangle Box;
            public float Power, Wait;
            public HitColors Colors;
        }
        private readonly Pending[] pending = new Pending[8];

        private readonly Stroke[] strokes = new Stroke[32];
        private readonly Burst[] bursts = new Burst[16];
        private readonly Ring[] rings = new Ring[16];
        private readonly Bit[] bits = new Bit[384];
        private readonly Twinkle[] twinkles = new Twinkle[48];
        private readonly Flame[] flames = new Flame[96];
        private readonly Random random = new Random(433);

        //FOCUS : one set of lines at a time
        private Vector2 focusAt;
        private float focusAge = 99f;

        //IMPACT FRAME : its clock and where the blow landed (in the fighters' layer, like every blow here)
        private float impactAge = 99f;
        private Vector2 impactAt;

        //The steps, from BattleRules : TURN, FLASH, TURN again, then SETTLE
        private static float TurnEnd { get { return BattleRules.HitImpactTurn; } }
        private static float FlashEnd { get { return TurnEnd + BattleRules.HitImpactFlash; } }
        private static float SecondEnd { get { return FlashEnd + BattleRules.HitImpactTurn2; } }
        private static float ImpactEnd { get { return SecondEnd + BattleRules.HitImpactSettle; } }

        //Impact : 1 while the world is turned over (none with LESS FLASHING)
        public float Impact
        {
            get
            {
                if (Settings.ReduceFlashing) return 0f;
                float t = impactAge;
                return t < TurnEnd || (t >= FlashEnd && t < SecondEnd) ? 1f : 0f;
            }
        }

        //Impact Flash : the frame of white between the two turns, then a white glare fading while it settles
        public float ImpactFlash
        {
            get
            {
                if (Settings.ReduceFlashing) return 0f;
                float t = impactAge;
                if (t >= TurnEnd && t < FlashEnd) return 1f;
                if (t >= SecondEnd && t < ImpactEnd)
                {
                    float k = 1f - (t - SecondEnd) / BattleRules.HitImpactSettle;
                    return 0.6f * k * k;
                }
                return 0f;
            }
        }

        //Impact Blur : the world smeared towards the blow, strongest at once and easing away (half with LESS FLASHING)
        public float ImpactBlur
        {
            get
            {
                if (impactAge >= ImpactEnd) return 0f;
                float k = 1f - impactAge / ImpactEnd;
                return k * k * (Settings.ReduceFlashing ? 0.5f : 1f);
            }
        }

        //Impact Zoom : the camera punches in at once and eases back out (0..1, BattleRules.HitImpactZoom says how far)
        public float ImpactZoom
        {
            get
            {
                if (impactAge >= ImpactEnd) return 0f;
                float k = 1f - impactAge / ImpactEnd;
                return k * k * k * (Settings.ReduceFlashing ? 0.5f : 1f);
            }
        }

        //Impact At : where the blow landed, in the fighters' layer
        public Vector2 ImpactAt
        {
            get { return impactAt; }
        }

        public HitFx()
        {
            for (int i = 0; i < bolts.Length; i++)
            {
                bolts[i].Main = new Vector2[BoltPoints];
                bolts[i].Branch = new Vector2[Branches][];
                for (int k = 0; k < Branches; k++) bolts[i].Branch[k] = new Vector2[BranchPoints];
            }
        }

        //Stroke Times : draw on, hold, wipe off (seconds)
        private const float DrawOn = 0.08f;
        private const float Hold = 0.07f;
        private const float WipeOff = 0.22f;

        //======================================================================================
        //Hits
        //======================================================================================

        //Hit : a blow of this style lands on a fighter standing in box. power 0..1.5 scales everything,
        //c is the striker's colours (Plain = white, for the SIGNATURE and TACET).
        //tier : WEAPON TIERS (29 Oct), the striker's weapon tier 0 to 3. Every tier makes the blow bigger with more sparks
        //(BattleRules.HitTierSize), tier II adds a ring of their colour, tier III a star where it lands, so an upgrade is seen.
        //swing : ACTION (Beta1, 29 Oct), the way the baton swung for this blow. A SLASH is cut that way and a SHOT flies that
        //way, so the mark on the enemy follows the player's hand (Flick.None : the blow's own slant, as before).
        public void Hit(HitStyle style, Rectangle box, float power, HitColors c, int tier = 0, Flick swing = Flick.None)
        {
            Vector2 at = new Vector2(box.Center.X, box.Y + box.Height * 0.45f);
            float size = (0.75f + 0.35f * power) * BattleRules.HitSize * (1f + BattleRules.HitTierSize * tier);
            float lean = (float)(random.NextDouble() - 0.5) * 0.6f;
            Vector2 way = new Vector2(1f, 0f);

            if (style == HitStyle.Slash)
            {
                //SLASH : two crescents crossing a little, the second a moment later, and a long thin cut through both
                float turn = swing != Flick.None ? SwingAngle(swing) + lean * 0.3f : -0.55f + lean;
                AddCrescent(at, size * 1.25f, 0.42f, turn, 2.5f, 3.1f, 20f * (0.7f + 0.5f * power), 0f, c);
                AddCrescent(at + new Vector2(14f, -10f), size * 1.0f, 0.38f, turn + 0.5f, 5.8f, -2.6f, 13f * (0.7f + 0.5f * power), -0.07f, c);
                way = new Vector2((float)Math.Cos(turn), (float)Math.Sin(turn));
                AddCut(at - way * size * 2.2f, at + way * size * 2.2f, 3f, -0.02f, c);
                AddBurst(at, size * 0.45f, (float)Math.Atan2(way.Y, way.X), 1.8f, 8, 3f, 0.16f, c);
                Throw(at, way, 14 + (int)(10 * power), 520f, Streak, c);
                Throw(at, -way, 6, 300f, Spark, c);
            }
            else if (style == HitStyle.Shot)
            {
                //SHOT : the spikes stretched the way the shot went (from the band, left to right), a round glow, a ring
                float angle = swing != Flick.None ? SwingAngle(swing) : -0.12f + lean * 0.3f;
                AddBurst(at, size * 1.25f, angle, 1.9f, 18, 5f, 0.3f, c);
                AddRing(at, size * 0.9f, 1f, 3f, 0.3f, c.Tint);
                way = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                Throw(at, way, 18 + (int)(10 * power), 640f, Streak, c);
                Throw(at, way, 10, 360f, Spark, c);
            }
            else if (style == HitStyle.Smash)
            {
                //SMASH : spikes stretched up and down (a blow from above), a ring opening on the floor, glass thrown up
                Vector2 feet = new Vector2(box.Center.X, box.Bottom - 6f);
                AddBurst(at, size * 1.15f, -MathHelper.PiOver2, 1.7f, 14, 5f, 0.28f, c);
                AddRing(feet, size * 1.8f, 0.24f, 4f, 0.42f, c.Tint);
                AddRing(feet, size * 1.1f, 0.24f, 2f, 0.3f, c.Hot);
                Throw(at, new Vector2(0f, -1f), 16 + (int)(8 * power), 460f, Shard, c);
                Throw(feet, new Vector2(0f, -1f), 10, 260f, Spark, c);
                way = new Vector2(0f, -1f);
            }
            else if (style == HitStyle.Cross)
            {
                //CROSS : two cuts across the whole screen meeting on the enemy, a star where they cross
                float a = 0.42f + lean * 0.2f;
                float b = -0.38f + lean * 0.2f;
                float reach = BattleRules.HitCrossLength;
                Vector2 wayA = new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
                Vector2 wayB = new Vector2((float)Math.Cos(b), (float)Math.Sin(b));
                AddCut(at - wayA * reach, at + wayA * reach, 5f, 0f, c);
                AddCut(at + wayB * reach, at - wayB * reach, 5f, -0.08f, c);
                Star(at, size * 2.2f, c);
                AddBurst(at, size * 1.3f, 0f, 1f, 22, 4f, 0.32f, c);
                AddRing(at, size * 1.6f, 1f, 3f, 0.4f, c.Tint);
                Throw(at, wayA, 16, 700f, Streak, c);
                Throw(at, -wayB, 16, 700f, Streak, c);
                Throw(at, new Vector2(0f, -1f), 14, 420f, Shard, c);
                Focus(at);
                ImpactFrame(at);
            }
            else if (style == HitStyle.Lightning)
            {
                //LIGHTNING : a bolt from the sky onto the enemy, the ground lit and ringing where it lands
                Vector2 feet = new Vector2(box.Center.X, box.Bottom - 6f);
                Vector2 struck = new Vector2(box.Center.X, box.Y + box.Height * 0.3f);
                Strike(struck, box.Width * 0.5f, c);
                AddBurst(struck, size * 1.4f, -MathHelper.PiOver2, 1.6f, 20, 5f, 0.36f, c);
                Star(struck, size * 1.9f, c);
                AddRing(feet, size * 2.3f, 0.24f, 4f, 0.5f, c.Tint);
                AddRing(feet, size * 1.3f, 0.24f, 2f, 0.36f, c.Hot);
                Throw(struck, new Vector2(0f, 1f), 18, 560f, Streak, c);
                Throw(feet, new Vector2(0f, -1f), 18, 360f, Spark, c);
                Throw(struck, new Vector2(0f, -1f), 10, 420f, Shard, c);
                ImpactFrame(struck);
                return;
            }
            else
            {
                //INK : TACET's note bursts on a musician, a ring of white, white cracks, dark drops falling
                AddRing(at, size * 0.9f, 1f, 3f, 0.3f, Color.White);
                AddBurst(at, size * 0.7f, 0f, 1f, 10, 2f, 0.2f, HitColors.Plain);
                Throw(at, new Vector2(0f, -1f), 16, 380f, Ink, HitColors.Plain);
                return;
            }

            //WEAPON TIERS : a sharper weapon's blow shows it (SLASH, SHOT and SMASH, the musicians' own blows)
            if (tier >= 1) Throw(at, way, 6 * tier, 480f, Streak, c);
            if (tier >= 2) AddRing(at, size * 1.35f, 1f, 2f, 0.36f, c.Tint);
            if (tier >= 3)
            {
                Star(at, size * 1.25f, c);
                AddRing(at, size * 1.9f, 1f, 1f, 0.46f, c.Hot);
            }

            //FLAIR : the striker's own touch on top of the blow
            if (c.Flair == HitFlair.Sparkle) Sparkle(at, size, power, c);
            else if (c.Flair == HitFlair.Flame) Burn(at, size, power, way, style == HitStyle.Slash, c);
        }

        //Swing Angle : the way of a baton stroke as an angle, leaned a little off straight so a cut always reads as a cut
        private static float SwingAngle(Flick way)
        {
            if (way == Flick.Left) return MathHelper.Pi - 0.35f;
            if (way == Flick.Up) return -MathHelper.PiOver2 - 0.35f;
            if (way == Flick.Down) return MathHelper.PiOver2 - 0.35f;
            return -0.35f;
        }

        //Clash : a parry meets TACET's note at the ring. perfect = the blow is turned away whole (a star, spikes, a ring
        //and sparks all round), otherwise a smaller burst. In the colours of the musician who parried.
        public void Clash(Vector2 at, bool perfect, HitColors c)
        {
            float size = BattleRules.HitSize;
            if (perfect)
            {
                Star(at, size * 1.5f, c);
                AddBurst(at, size * 0.9f, 0f, 1f, 16, 4f, 0.24f, c);
                AddRing(at, size * 1.2f, 1f, 3f, 0.32f, c.Tint);
                for (int k = 0; k < 4; k++) Throw(at, new Vector2((float)Math.Cos(k * MathHelper.PiOver2), (float)Math.Sin(k * MathHelper.PiOver2)), 5, 520f, Streak, c);
                if (c.Flair == HitFlair.Sparkle) Sparkle(at, size * 0.8f, 0.6f, c);
                else if (c.Flair == HitFlair.Flame) Burn(at, size * 0.7f, 0.6f, new Vector2(0f, -1f), false, c);
            }
            else
            {
                AddBurst(at, size * 0.5f, 0f, 1f, 10, 3f, 0.18f, c);
                AddRing(at, size * 0.7f, 1f, 2f, 0.24f, c.Tint);
            }
        }

        //Silence : an enemy SILENCED. Specks of it lift off all over its box and are drawn into the hole at target,
        //faster as they go, with a ring and a star where it stood.
        public void Silence(Rectangle box, Vector2 target)
        {
            Vector2 middle = new Vector2(box.Center.X, box.Center.Y);
            Star(middle, BattleRules.HitSize * 1.6f, HitColors.Plain);
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
                    Hot = Color.White,
                };
                bits[slot].Length = bits[slot].Life;
            }
        }

        //Star : a four pointed twinkle, the long points up and down
        public void Star(Vector2 at, float size, HitColors c)
        {
            int slot = Free(bursts);
            bursts[slot] = new Burst { On = true, At = at, Size = size, Angle = -MathHelper.PiOver2, Stretch = 1f, Width = 6f, Age = 0f, Length = 0.34f, Spikes = 4, Seed = -1, Tint = c.Tint, Hot = c.Hot };
        }

        //Focus : lines rush in to a point for a moment
        public void Focus(Vector2 at)
        {
            focusAt = at;
            focusAge = 0f;
        }

        //Impact Frame : the IMPACT FRAME starts, round the blow at (with LESS FLASHING only the softer punch and smear)
        public void ImpactFrame(Vector2 at)
        {
            if (impactAge < SecondEnd) return;            // one is already turning : the next bolt of the same SIGNATURE joins it
            impactAge = 0f;
            impactAt = at;
        }

        //Hit Later : the same as Hit, wait seconds from now (the LIGHTNING strikes the enemies one after another)
        public void HitLater(HitStyle style, Rectangle box, float power, HitColors c, float wait)
        {
            if (wait <= 0f)
            {
                Hit(style, box, power, c);
                return;
            }
            int slot = 0;
            for (int i = 0; i < pending.Length; i++)
                if (!pending[i].On) { slot = i; break; }
            pending[slot] = new Pending { On = true, Style = style, Box = box, Power = power, Wait = wait, Colors = c };
        }

        //Strike : a new bolt from above the screen down to at. size is how wide the arcs crawl round it.
        private void Strike(Vector2 at, float size, HitColors c)
        {
            int slot = 0;
            for (int i = 0; i < bolts.Length; i++)
                if (!bolts[i].On) { slot = i; break; }

            //The channel : from high above, a little to one side, down to the enemy
            Vector2 top = new Vector2(at.X + ((float)random.NextDouble() - 0.5f) * 260f, -80f);
            MakeChannel(bolts[slot].Main, BoltPoints, top, at, 0.55f);

            //Branches : off the upper two thirds of the channel, angled down and away, shorter and fainter
            for (int k = 0; k < Branches; k++)
            {
                int from = 6 + random.Next(16);
                Vector2 along = bolts[slot].Main[from + 1] - bolts[slot].Main[from - 1];
                float angle = (float)Math.Atan2(along.Y, along.X) + (random.Next(2) == 0 ? -1f : 1f) * (0.45f + 0.45f * (float)random.NextDouble());
                float reach = 50f + 110f * (float)random.NextDouble();
                Vector2 end = bolts[slot].Main[from] + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * reach;
                MakeChannel(bolts[slot].Branch[k], BranchPoints, bolts[slot].Main[from], end, 0.6f);
            }

            bolts[slot].On = true;
            bolts[slot].At = at;
            bolts[slot].Age = 0f;
            bolts[slot].Size = size;
            bolts[slot].Seed = random.Next(1000);
            bolts[slot].Tint = c.Tint;
            bolts[slot].Hot = c.Hot;
        }

        //Make Channel : a jagged line from from to to, count = 2^n + 1 points.
        //ADVANCED PART : MIDPOINT DISPLACEMENT. Start with the two ends. Put a point half way between every pair and push
        //it sideways by a random amount that is a share (rough) of that pair's distance. Do it again on the halves, and
        //again... Each round the pushes get smaller with the pieces, so the line is jagged at every size, like lightning.
        private void MakeChannel(Vector2[] points, int count, Vector2 from, Vector2 to, float rough)
        {
            int last = count - 1;
            points[0] = from;
            points[last] = to;
            for (int size = last; size >= 2; size /= 2)
            {
                for (int i = 0; i + size <= last; i += size)
                {
                    Vector2 a = points[i];
                    Vector2 b = points[i + size];
                    Vector2 d = b - a;
                    float length = d.Length();
                    Vector2 side = length > 0.01f ? new Vector2(-d.Y, d.X) / length : Vector2.Zero;
                    points[i + size / 2] = (a + b) * 0.5f + side * ((float)random.NextDouble() - 0.5f) * length * rough;
                }
            }
        }

        //======================================================================================
        //Flair
        //======================================================================================

        //SPARKLE : golden twinkles popping one after another round the blow, and glitter drifting down
        private void Sparkle(Vector2 at, float size, float power, HitColors c)
        {
            int count = 9 + (int)(6 * power);
            for (int i = 0; i < count; i++)
            {
                int slot = Free(twinkles);
                float angle = (float)(random.NextDouble() * MathHelper.TwoPi);
                float far = size * (0.35f + 1.05f * (float)random.NextDouble());
                twinkles[slot] = new Twinkle
                {
                    On = true,
                    At = at + new Vector2((float)Math.Cos(angle) * far, (float)Math.Sin(angle) * far * 0.8f),
                    Size = 8f + 14f * (float)random.NextDouble(),
                    Age = -0.25f * (float)random.NextDouble(),             // they come one after another
                    Life = 0.32f + 0.12f * (float)random.NextDouble(),
                    Tint = c.Hot,
                };
            }
            Throw(at, new Vector2(0f, -1f), 16 + (int)(8 * power), 260f, Glitter, c);
        }

        //FLAME : tongues of fire rising off the blow (along the cut for a SLASH), and embers flying up
        private void Burn(Vector2 at, float size, float power, Vector2 way, bool along, HitColors c)
        {
            int count = 26 + (int)(14 * power);
            for (int i = 0; i < count; i++)
            {
                int slot = Free(flames);
                Vector2 from;
                if (along) from = at + way * size * 1.8f * ((float)random.NextDouble() * 2f - 1f);
                else from = at + new Vector2(((float)random.NextDouble() * 2f - 1f) * size * 0.7f, ((float)random.NextDouble() - 0.3f) * size * 0.5f);
                flames[slot] = new Flame
                {
                    On = true,
                    At = from,
                    Speed = new Vector2(((float)random.NextDouble() - 0.5f) * 60f, -(50f + 110f * (float)random.NextDouble())),
                    Size = (11f + 15f * (float)random.NextDouble()) * (0.8f + 0.3f * power),
                    Age = -0.08f * (float)random.NextDouble(),
                    Life = 0.45f + 0.4f * (float)random.NextDouble(),
                    Sway = (float)(random.NextDouble() * MathHelper.TwoPi),
                    Tint = c.Tint,
                    Hot = c.Hot,
                };
            }
            Throw(at, new Vector2(0f, -1f), 12 + (int)(8 * power), 340f, Ember, c);
        }

        //======================================================================================
        //Spawning
        //======================================================================================

        //Add Crescent : a curved cut. from and sweep are angles on its ellipse, delay below 0 waits a moment.
        private void AddCrescent(Vector2 center, float radius, float squash, float turn, float from, float sweep, float width, float delay, HitColors c)
        {
            int slot = Free(strokes);
            strokes[slot] = new Stroke { On = true, Straight = false, Center = center, Radius = radius, Squash = squash, Turn = turn, From = from, Sweep = sweep, Width = width, Age = delay, Tint = c.Tint, Hot = c.Hot };
        }

        //Add Cut : a straight cut from a to b
        private void AddCut(Vector2 a, Vector2 b, float width, float delay, HitColors c)
        {
            int slot = Free(strokes);
            strokes[slot] = new Stroke { On = true, Straight = true, A = a, B = b, Width = width, Age = delay, Tint = c.Tint, Hot = c.Hot };
        }

        private void AddBurst(Vector2 at, float size, float angle, float stretch, int spikes, float width, float length, HitColors c)
        {
            int slot = Free(bursts);
            bursts[slot] = new Burst { On = true, At = at, Size = size, Angle = angle, Stretch = stretch, Width = width, Age = 0f, Length = length, Spikes = spikes, Seed = random.Next(1000), Tint = c.Tint, Hot = c.Hot };
        }

        private void AddRing(Vector2 at, float size, float squash, float width, float length, Color tint)
        {
            int slot = Free(rings);
            rings[slot] = new Ring { On = true, At = at, Size = size, Squash = squash, Width = width, Age = 0f, Length = length, Tint = tint };
        }

        //Throw : count bits out of a point, mostly along way (spread about 70 degrees)
        private void Throw(Vector2 at, Vector2 way, int count, float speed, byte kind, HitColors c)
        {
            float baseAngle = (float)Math.Atan2(way.Y, way.X);
            float spread = kind == Glitter || kind == Ember ? 2.6f : 1.25f;
            for (int i = 0; i < count; i++)
            {
                int slot = Free(bits);
                float angle = baseAngle + (float)(random.NextDouble() - 0.5) * spread;
                float fast = speed * (0.35f + 0.65f * (float)random.NextDouble());
                bits[slot] = new Bit
                {
                    On = true,
                    Kind = kind,
                    At = at,
                    Speed = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * fast,
                    Life = 0.25f + 0.35f * (float)random.NextDouble() + (kind == Shard || kind == Ink ? 0.3f : 0f) + (kind == Glitter || kind == Ember ? 0.45f : 0f),
                    Size = kind == Shard ? 6f + 8f * (float)random.NextDouble() : 2f,
                    Spin = (float)(random.NextDouble() * MathHelper.TwoPi),
                    Turn = (float)(random.NextDouble() - 0.5) * 18f,
                    Tint = c.Tint,
                    Hot = c.Hot,
                };
                bits[slot].Length = bits[slot].Life;
            }
        }

        //Free : a slot that is off, or else the first one (the oldest is usually near its end)
        private static int Free(Stroke[] pool) { for (int i = 0; i < pool.Length; i++) if (!pool[i].On) return i; return 0; }
        private static int Free(Burst[] pool) { for (int i = 0; i < pool.Length; i++) if (!pool[i].On) return i; return 0; }
        private static int Free(Ring[] pool) { for (int i = 0; i < pool.Length; i++) if (!pool[i].On) return i; return 0; }
        private static int Free(Bit[] pool) { for (int i = 0; i < pool.Length; i++) if (!pool[i].On) return i; return 0; }
        private static int Free(Twinkle[] pool) { for (int i = 0; i < pool.Length; i++) if (!pool[i].On) return i; return 0; }
        private static int Free(Flame[] pool) { for (int i = 0; i < pool.Length; i++) if (!pool[i].On) return i; return 0; }

        //======================================================================================
        //Update
        //======================================================================================

        //Update : frozen = the hit stop. The cuts, bursts and twinkles still play (they ARE the moment), the bits and
        //the fire hold still.
        public void Update(float dt, bool frozen)
        {
            impactAge += dt;
            focusAge += dt;

            //Pending blows land when their wait is over (the clock runs through the hit stop, they are part of it)
            for (int i = 0; i < pending.Length; i++)
            {
                if (!pending[i].On) continue;
                pending[i].Wait -= dt;
                if (pending[i].Wait > 0f) continue;
                pending[i].On = false;
                Hit(pending[i].Style, pending[i].Box, pending[i].Power, pending[i].Colors);
            }
            for (int i = 0; i < bolts.Length; i++)
            {
                if (!bolts[i].On) continue;
                bolts[i].Age += dt;
                if (bolts[i].Age > BoltLife) bolts[i].On = false;
            }

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
            for (int i = 0; i < twinkles.Length; i++)
            {
                if (!twinkles[i].On) continue;
                twinkles[i].Age += dt;
                if (twinkles[i].Age > twinkles[i].Life) twinkles[i].On = false;
            }

            if (frozen) return;

            //Flames : rise faster as they go (hot air), sway, slow sideways
            float sideDrag = (float)Math.Pow(0.2, dt);
            for (int i = 0; i < flames.Length; i++)
            {
                if (!flames[i].On) continue;
                flames[i].Age += dt;
                if (flames[i].Age > flames[i].Life)
                {
                    flames[i].On = false;
                    continue;
                }
                if (flames[i].Age < 0f) continue;
                flames[i].Speed.Y -= 90f * dt;
                flames[i].Speed.X *= sideDrag;
                flames[i].At += flames[i].Speed * dt;
            }

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
                byte kind = bits[i].Kind;
                if (kind == Pull)
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
                bits[i].Speed *= kind == Streak ? drag * 0.97f : drag;
                if (kind == Glitter) bits[i].Speed.Y += 160f * dt;                 // drifts down slowly
                else if (kind == Ember) bits[i].Speed.Y -= 260f * dt;              // rises
                else if (kind != Streak) bits[i].Speed.Y += (kind == Ink ? 900f : 700f) * dt;    // falls
                bits[i].Spin += bits[i].Turn * dt;
            }
        }

        public void Clear()
        {
            for (int i = 0; i < strokes.Length; i++) strokes[i].On = false;
            for (int i = 0; i < bursts.Length; i++) bursts[i].On = false;
            for (int i = 0; i < rings.Length; i++) rings[i].On = false;
            for (int i = 0; i < bits.Length; i++) bits[i].On = false;
            for (int i = 0; i < twinkles.Length; i++) twinkles[i].On = false;
            for (int i = 0; i < flames.Length; i++) flames[i].On = false;
            for (int i = 0; i < bolts.Length; i++) bolts[i].On = false;
            for (int i = 0; i < pending.Length; i++) pending[i].On = false;
            focusAge = 99f;
            impactAge = 99f;
        }

        //======================================================================================
        //Draw
        //======================================================================================

        //Draw Glow : into the GLOW layer (additive), with the fighters' camera. The fire is under the cuts.
        public void DrawGlow(SpriteBatch sb)
        {
            DrawFocus(sb);
            for (int i = 0; i < flames.Length; i++) if (flames[i].On && flames[i].Age >= 0f) DrawFlame(sb, flames[i]);
            for (int i = 0; i < rings.Length; i++) if (rings[i].On) DrawRing(sb, rings[i]);
            for (int i = 0; i < bursts.Length; i++) if (bursts[i].On) DrawBurst(sb, bursts[i]);
            for (int i = 0; i < strokes.Length; i++) if (strokes[i].On && strokes[i].Age > 0f) DrawStroke(sb, strokes[i]);
            for (int i = 0; i < bolts.Length; i++) if (bolts[i].On) DrawBolt(sb, bolts[i]);
            for (int i = 0; i < twinkles.Length; i++) if (twinkles[i].On && twinkles[i].Age >= 0f) DrawTwinkle(sb, twinkles[i]);
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
        //ends (a crescent), a wide soft layer in the striker's colour under the hot core, and thin threads trailing
        //inside a crescent.
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
                Gfx.Line(sb, Snap(last), Snap(p), s.Tint * 0.45f, Math.Max(2f, w * 2.6f));
                Gfx.Line(sb, Snap(last), Snap(p), s.Hot, Math.Max(1f, w));
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
                        Gfx.Line(sb, Snap(from), Snap(p), s.Tint * (0.7f / thread), 1f);
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
            Gfx.DrawGlow(sb, b.At.X, b.At.Y, b.Size * 0.8f * grow, b.Tint * (0.6f * fade));
            Gfx.DrawGlow(sb, b.At.X, b.At.Y, b.Size * 0.28f, b.Hot * fade);

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
                DrawSpike(sb, b.At, way, length * grow, b.Width * (0.5f + 0.5f * fade), (i % 2 == 0 ? b.Hot : b.Tint) * fade);
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

        //Draw Bit : sparks are squares, streaks lines along their way, shards slivers that glint as they turn,
        //glitter twinkles as it drifts, embers flicker from hot to the striker's colour
        private static void DrawBit(SpriteBatch sb, Bit b)
        {
            float a = Math.Min(1f, b.Life / (b.Length * 0.5f));
            Vector2 p = Snap(b.At);
            if (b.Kind == Spark) Gfx.Rect(sb, p.X, p.Y, 2f, 2f, b.Hot * a);
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
            else if (b.Kind == Glitter)
            {
                float twinkle = 0.35f + 0.65f * (float)Math.Abs(Math.Sin(b.Spin * 1.7f));
                Gfx.Rect(sb, p.X, p.Y, 2f, 2f, b.Hot * (a * twinkle));
            }
            else if (b.Kind == Ember)
            {
                float age = 1f - b.Life / b.Length;
                float flicker = 0.6f + 0.4f * (float)Math.Abs(Math.Sin(b.Spin * 2.3f));
                Gfx.Rect(sb, p.X, p.Y, 2f, 2f, Color.Lerp(b.Hot, b.Tint, age) * (a * flicker));
            }
            else
            {
                Vector2 way = new Vector2((float)Math.Cos(b.Spin), (float)Math.Sin(b.Spin)) * b.Size * 0.5f;
                float glint = 0.35f + 0.65f * (float)Math.Abs(Math.Cos(b.Spin * 2f));
                Gfx.Line(sb, Snap(b.At - way), Snap(b.At + way), b.Hot * (a * glint), 2f);
            }
        }

        //Draw Twinkle : a small four pointed star, popping up then shrinking away
        private static void DrawTwinkle(SpriteBatch sb, Twinkle w)
        {
            float t = w.Age / w.Life;
            float pop = t < 0.25f ? t / 0.25f : 1f - (t - 0.25f) / 0.75f;
            float s = w.Size * pop;
            if (s < 2f) return;
            Vector2 at = Snap(w.At);
            Gfx.DrawGlow(sb, at.X, at.Y, s * 0.9f, w.Tint * 0.35f);
            DrawSpike(sb, at, new Vector2(0f, -1f), s, 2f, w.Tint);
            DrawSpike(sb, at, new Vector2(0f, 1f), s, 2f, w.Tint);
            DrawSpike(sb, at, new Vector2(-1f, 0f), s * 0.6f, 2f, w.Tint);
            DrawSpike(sb, at, new Vector2(1f, 0f), s * 0.6f, 2f, w.Tint);
            Gfx.Rect(sb, at.X - 1f, at.Y - 1f, 2f, 2f, Color.White);
        }

        //Draw Flame : a pixel tongue of fire, four slabs stacked, each narrower, cooler and swaying further than the
        //one below (a teardrop pointing up), over a soft glow of its light
        private static void DrawFlame(SpriteBatch sb, Flame f)
        {
            float t = f.Age / f.Life;
            float grow = Math.Min(1f, f.Age / 0.06f);
            float s = f.Size * grow * (1f - 0.6f * t);
            if (s < 2f) return;
            float sway = (float)Math.Sin(f.Age * 14f + f.Sway) * s * 0.22f;

            Gfx.DrawGlow(sb, f.At.X, f.At.Y - s * 0.8f, s * 2f, FireColor(t, f.Tint, f.Hot) * 0.4f);

            //Slabs : from the base up, width and height as shares of the flame's size
            float y = f.At.Y;
            for (int k = 0; k < 4; k++)
            {
                float w = Even(s * (0.7f - 0.16f * k));
                float h = Even(s * (0.55f - 0.08f * k));
                Color c = FireColor(t + 0.14f * k, f.Tint, f.Hot);
                Vector2 p = Snap(new Vector2(f.At.X + sway * k * 0.6f - w / 2f, y - h));
                Gfx.Rect(sb, p.X, p.Y, w, h, c);
                y -= h;
            }
        }

        //Fire Colour : white yellow when born, then the hot colour, then the striker's colour, then dark (adding
        //nothing to the GLOW)
        private static Color FireColor(float t, Color tint, Color hot)
        {
            if (t < 0.2f) return Color.Lerp(new Color(255, 248, 220), hot, t / 0.2f);
            if (t < 0.55f) return Color.Lerp(hot, tint, (t - 0.2f) / 0.35f);
            return Color.Lerp(tint, Color.Black, MathHelper.Clamp((t - 0.55f) / 0.45f, 0f, 1f));
        }

        //Draw Bolt : the channel and its branches, each as a wide dim glow, a hot middle and a white core, lit by
        //Bolt Light. Small arcs crawl round where it struck, jumping twenty times a second.
        private static void DrawBolt(SpriteBatch sb, Bolt b)
        {
            float lit = BoltLight(b.Age);
            if (lit > 0.01f)
            {
                DrawChannel(sb, b.Main, BoltPoints, b.Tint, b.Hot, lit, 1f);
                for (int k = 0; k < Branches; k++) DrawChannel(sb, b.Branch[k], BranchPoints, b.Tint, b.Hot, lit * 0.75f, 0.5f);
            }

            //Arcs : stateless, so nothing has to be stored : the hash of the bolt and the tick gives each zigzag
            if (b.Age < 0.05f) return;
            float fade = 1f - b.Age / BoltLife;
            int tick = (int)(b.Age * 20f);
            for (int k = 0; k < 4; k++)
            {
                Vector2 p = b.At + new Vector2((Hash(b.Seed + k, tick) - 0.5f) * b.Size * 1.8f, (Hash(b.Seed + k * 7, tick + 3) - 0.2f) * b.Size * 2.4f);
                for (int s = 0; s < 4; s++)
                {
                    float a = Hash(b.Seed + k * 13 + s, tick) * MathHelper.TwoPi;
                    Vector2 q = p + new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * 12f;
                    Gfx.Line(sb, Snap(p), Snap(q), b.Hot * (0.9f * fade), 2f);
                    p = q;
                }
            }
        }

        //Bolt Light : strike, out, strike again, out, a last strike, then the afterglow fading
        private static float BoltLight(float t)
        {
            if (t < 0.06f) return 1f;
            if (t < 0.09f) return 0.12f;
            if (t < 0.15f) return 1f;
            if (t < 0.18f) return 0.18f;
            if (t < 0.24f) return 0.9f;
            float k = 1f - (t - 0.24f) / 0.3f;
            return k > 0f ? 0.65f * k * k : 0f;
        }

        //Draw Channel : one jagged line of lightning, a wide faint halo under a glow under a hot middle under a white core
        //(round 6 : the halo makes the blue glow, the user asked for glowing blue lightning)
        private static void DrawChannel(SpriteBatch sb, Vector2[] points, int count, Color tint, Color hot, float lit, float width)
        {
            for (int i = 0; i < count - 1; i++)
            {
                Vector2 a = Snap(points[i]);
                Vector2 b = Snap(points[i + 1]);
                Gfx.Line(sb, a, b, tint * (0.08f * lit), 36f * width);
                Gfx.Line(sb, a, b, tint * (0.28f * lit), 16f * width);
                Gfx.Line(sb, a, b, hot * (0.75f * lit), 6f * width);
                Gfx.Line(sb, a, b, Color.White * lit, Math.Max(2f, 2f * width));
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

        //Even : a size in whole pixels of the art (2, 4, 6 ...)
        private static float Even(float v)
        {
            return Math.Max(2f, (float)Math.Floor(v * 0.5f) * 2f);
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
