using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //Figure Band (Beta1 2 Nov, MATH ART round 6, docs/BETA1_DIRECTION.md 32) : the five legends. The user (13.86) : simple but
    //symbolic, symbolic but see the origins. Round 5 made each body the whole instrument and it read as an instrument with a
    //light bulb for a head (13.87). Round 6 gives each one a self, the way Worldless does it : few shapes, each a word.
    //   LIGHT     the light comes out where the sound comes out : PAGANINI's f hole, QUANTZ's blow hole, the skin of PFUNDT's
    //             drum, DUPORT's scar (struck harder, it shines brighter), the bell of PUNTO's horn
    //   ELEMENT   each has one colour, of the band's five elements (Palette spot colours, SpotLook) : FIRE, WIND, EARTH,
    //             WATER, LIGHT
    //   SHAPE     the head and the line of the body tell who they are. The user read two women (PAGANINI, QUANTZ), two men
    //             (PFUNDT, DUPORT) and one unsaid (PUNTO) from the shapes (13.90 13.91)
    //   GIMMICK   a part of the body shows the fight (LEGEND CUES) : PAGANINI's LIFE STRING, the key QUANTZ gives away, PFUNDT's
    //             winding key and beating drum, the cracks of DUPORT's scar, the light of PUNTO's bell on whoever plays next
    //   LEGEND    a button with all three stars (REHEARSAL) changes its part of the body (LEGEND FORM, 32.12) : the ATTACK the
    //             weapon, each skill its gimmick. The more it grows, the cooler it looks (Haste, 13.39)
    //   LEVEL 2   thin curved arms and legs join the same parts into a figure (F4 in a fight, 13.90 : kept)
    //SKETCH : the five were drawn first in docs/art_refs/concept_v6.html. The numbers below are that drawing's, so the two can
    //be laid side by side : a box 240 wide, the floor at y 262, the figure's middle at x 112, 237 units to the figure's height.
    public partial class Figure
    {
        //Body : the parts every design moves (TACET's designs give them their own meaning)
        private const int Head = 0;
        private const int Chest = 1;          // the band : the top of the body
        private const int Hip = 2;            //   its bottom
        private const int HandA = 3;          // the weapon hand
        private const int HandB = 4;          // the other hand
        private const int FootA = 5;          // the back foot (DUPORT : the point of the spike)
        private const int FootB = 6;          // the front foot
        private const int Extra = 7;          // a design's own loose part

        private readonly Point[] body = new Point[8];
        private readonly Vector2[] want = new Vector2[8];
        private readonly float[] bodyLoose = new float[8];       // 0 held firm, 1 floating loose and bouncy
        private readonly Point[] bits = new Point[5];            // BITS : small shapes floating round the figure (embers, leaves, dust, drops, sparks)
        private readonly Vector2[] bitWant = new Vector2[5];
        private Turn weapon;                                     // the weapon's angle (and the second weapon's)
        private Turn weapon2;
        private float weaponWant;
        private float weapon2Want;

        private const int MaxChains = 10;
        private readonly Chain[] chains = new Chain[MaxChains];
        private int chainCount;
        private readonly float[] chainLink = new float[MaxChains];
        private readonly float[] chainWeight = new float[MaxChains];
        private readonly int[] chainPart = new int[MaxChains];          // the body part a chain hangs from
        private readonly Vector2[] chainSpot = new Vector2[MaxChains];  //   and where on it (shares of the height)
        private readonly float[] chainWidth = new float[MaxChains];
        private readonly Vector2[] chainDrift = new Vector2[MaxChains]; // a steady push of its own (pixels a second squared on a figure 512 tall, x forward)

        //======================================================================================
        //The five designs
        //======================================================================================

        private const int Cannone = 0;        // PAGANINI : IL CANNONE
        private const int Rapier = 1;         // QUANTZ : FLUTE RAPIER
        private const int Hammers = 2;        // PFUNDT : TWIN HAMMERS
        private const int Shield = 3;         // DUPORT : THE SCARRED SHIELD (the body is the shield, the bow a lance)
        private const int Chakram = 4;        // PUNTO : HORN CHAKRAM

        //SKETCH : see the top of the file
        private const float SketchMiddle = 112f;
        private const float SketchFloor = 262f;
        private const float SketchHeight = 237f;

        //Legend : how one of the five stands, in the sketch's units
        private struct Legend
        {
            public int Weapon;
            public Vector2 Head, Chest, Hip;          // the rest pose (Chest and Hip are the top and bottom of the body)
            public Vector2 HandA, HandB, FootA, FootB;
            public Vector2 Shoulder;                  // the weapon arm turns round this in a swing
            public float Rest, Rest2;                 // the weapon angles at rest (0 forward, minus up)
            public float Lean;                        // the whole figure leans this much (radians, plus toward TACET)
            public float Nod;                         // the head tips this much more (minus : the chin up)
            public float Limb;                        // LEVEL 2 : how thick the arms and legs are
            public float Core;                        // how far the head's light reaches
        }
        private Legend legend;

        //Legend Of : the design of each of the five (by the key of their name, ArtBank.Key), from the sketch
        private static Legend LegendOf(string key)
        {
            switch (key)
            {
                case "quantz":            // WIND : calm and straight, heels together, a fencer's lean (32.12 : less stiff)
                    return new Legend
                    {
                        Weapon = Rapier, Head = new Vector2(112, 60), Chest = new Vector2(112, 90), Hip = new Vector2(112, 198),
                        HandA = new Vector2(142, 118), HandB = new Vector2(98, 152), FootA = new Vector2(105, 262), FootB = new Vector2(120, 262),
                        Shoulder = new Vector2(118, 96), Rest = -0.29f, Rest2 = 0f, Lean = 0.05f, Nod = 0.02f, Limb = 2.6f, Core = 9f,
                    };
                case "pfundt":            // EARTH : big and wide, the head brought down close to the body (32.12)
                    return new Legend
                    {
                        Weapon = Hammers, Head = new Vector2(112, 78), Chest = new Vector2(112, 112), Hip = new Vector2(112, 190),
                        HandA = new Vector2(158, 142), HandB = new Vector2(68, 146), FootA = new Vector2(95, 262), FootB = new Vector2(131, 262),
                        Shoulder = new Vector2(142, 104), Rest = -0.88f, Rest2 = 2.16f, Lean = 0f, Nod = 0f, Limb = 5f, Core = 10f,
                    };
                case "duport":            // WATER : the tallest, a slab on one spike, still as a monument, a little tilted (32.12)
                    return new Legend
                    {
                        Weapon = Shield, Head = new Vector2(112, 44), Chest = new Vector2(112, 72), Hip = new Vector2(112, 206),
                        HandA = new Vector2(151, 137), HandB = new Vector2(72, 148), FootA = new Vector2(112, 262), FootB = new Vector2(112, 262),
                        Shoulder = new Vector2(134, 96), Rest = -0.165f, Rest2 = 0f, Lean = -0.045f, Nod = 0f, Limb = 4.5f, Core = 9f,
                    };
                case "punto":             // LIGHT : walking, leaning forward like a traveller, the bell's halo behind the head
                    return new Legend
                    {
                        Weapon = Chakram, Head = new Vector2(124, 64), Chest = new Vector2(120, 98), Hip = new Vector2(116, 140),
                        HandA = new Vector2(156, 104), HandB = new Vector2(76, 126), FootA = new Vector2(92, 250), FootB = new Vector2(138, 262),
                        Shoulder = new Vector2(128, 98), Rest = -0.2f, Rest2 = 0f, Lean = 0f, Nod = 0f, Limb = 2.8f, Core = 8.5f,
                    };
                default:                  // paganini, FIRE : on tiptoe, leaning back, the chin up
                    return new Legend
                    {
                        Weapon = Cannone, Head = new Vector2(112, 60), Chest = new Vector2(112, 104), Hip = new Vector2(112, 156),
                        HandA = new Vector2(168, 140), HandB = new Vector2(72, 116), FootA = new Vector2(102, 259), FootB = new Vector2(124, 262),
                        Shoulder = new Vector2(120, 104), Rest = 0.9f, Rest2 = 0f, Lean = -0.02f, Nod = -0.16f, Limb = 3f, Core = 10f,
                    };
            }
        }

        //H : the height a design is drawn at
        private float H
        {
            get { return Height; }
        }

        //======================================================================================
        //LEGEND CUES : the fight on the body
        //======================================================================================

        //Legend Cues (Beta1 2 Nov, MATH ART round 6, docs/BETA1_DIRECTION.md 32.12) : what the fight shows on a legend's body, so
        //the player reads the state without a label. Screens/BattleScreen.Stage.cs (UpdateFigure) fills these every frame.
        public struct LegendCues
        {
            public float Life;          // the life left, 0..1 : PAGANINI's LIFE STRING glows with it, PFUNDT's drum beats faster as it falls
            public int Button;          // the button being played now : 0 ATTACK, 1 and 2 the skills, -1 nothing
            public int Volume;          // VOLUME stacks : the cracks of THE SCAR on DUPORT's body
            public bool LessonOut;      // THE LESSON : a friend holds QUANTZ's lesson, one key of the flute is gone
            public bool Taught;         // THE LESSON : this musician holds it, the key floats over the head (FOCUS)
            public bool Covered;        // SHIELD : DUPORT stands in front of this musician
            public bool Calling;        // THE HUNTING CALL : someone plays right after PUNTO in this turn
            public Color CallTint;      //   their colour, the bell's light shines back at them
            public int Stars0;          // REHEARSAL : the stars of the ATTACK (3 = the weapon's LEGEND FORM)
            public int Stars1;          //   of the first skill
            public int Stars2;          //   of the second skill
            public int Frame;           // the stage's frame when these were last told (a life lost while off stage is not a payment)
            public Vector2 TargetAt;    // ONE NOTE ONE BLOW : the enemy this blow is at, on the page (PUNTO's ring flies there)
            public bool Aimed;          //   there is one
        }
        public LegendCues Cues;

        //Timers of the cues (TickBand moves them)
        private float bleed;                 // THE DEVIL'S BARGAIN : 1 just after PAGANINI paid in life, then down to 0
        private float stringLeft = 1f;       // IL CANNONE : how much of the LIFE STRING is on the head (it flies as the shot, then grows back)
        private float keyTurn;               // TURN OF THE HAND : the angle of PFUNDT's winding key
        private float keySpeed;              //   and how fast it turns
        private float beat;                  // HEARTBEAT : where PFUNDT's drum is in its beat (0..1)

        //Bled : PAGANINI's skill just took life (THE DEVIL'S BARGAIN), the string flickers and the f hole smokes
        public void Bled()
        {
            bleed = 1f;
        }

        //Legend Form : the button has all three stars
        private bool Legendary(int stars)
        {
            return stars >= RehearsalList.Most;
        }

        //======================================================================================
        //Set up
        //======================================================================================

        //Set Up Body : how loose each part is, and the chains : PAGANINI's four strings, QUANTZ's two ribbons of wind
        private void SetUpBody()
        {
            if (Of != Sort.Musician)
            {
                SetUpTacet();
                return;
            }
            legend = LegendOf(Key);
            bodyLoose[Head] = 0.35f;
            bodyLoose[Chest] = 0.1f;
            bodyLoose[Hip] = 0.25f;
            bodyLoose[HandA] = 0.45f;
            bodyLoose[HandB] = 0.6f;
            bodyLoose[FootA] = 0.7f;
            bodyLoose[FootB] = 0.7f;
            bodyLoose[Extra] = 1f;
            chainCount = 0;
            Cues.Life = 1f;
            Cues.Button = -1;

            Vector2 head = legend.Head;
            if (legend.Weapon == Cannone)
            {
                //STRINGS : four strings for hair, hanging from the back of the flame. The first is the LIFE STRING (long, glowing),
                //the other three were snapped short (the legend : she played on with one string) and are drawn short (SNAPPED)
                AddChain(12, 0.066f, 0.42f, Head, SketchShare(106f - head.X, 64f - head.Y), 0.0072f, new Vector2(-720f, 0f));
                AddChain(12, 0.064f, 0.5f, Head, SketchShare(106f - head.X, 67f - head.Y), 0.0047f, new Vector2(-640f, 0f));
                AddChain(12, 0.062f, 0.55f, Head, SketchShare(107f - head.X, 70f - head.Y), 0.0047f, new Vector2(-580f, 0f));
                AddChain(12, 0.06f, 0.6f, Head, SketchShare(108f - head.X, 73f - head.Y), 0.0047f, new Vector2(-520f, 0f));
            }
            else if (legend.Weapon == Rapier)
            {
                //RIBBON : a long ribbon of wind from behind the head, the part of her that moves most (32.12). SANSSOUCI splits it
                //in two like wings, so the second is always made and only drawn then.
                AddChain(15, 0.066f, 0.3f, Head, SketchShare(105f - head.X, 63f - head.Y), 0.022f, new Vector2(-760f, -260f));
                AddChain(13, 0.062f, 0.26f, Head, SketchShare(107f - head.X, 58f - head.Y), 0.016f, new Vector2(-640f, -520f));
            }
        }

        //Sketch Share : a length of the sketch as a share of the figure's height
        private static Vector2 SketchShare(float x, float y)
        {
            return new Vector2(x / SketchHeight, y / SketchHeight);
        }

        private void AddChain(int count, float link, float weight, int part, Vector2 spot, float width)
        {
            AddChain(count, link, weight, part, spot, width, Vector2.Zero);
        }

        private void AddChain(int count, float link, float weight, int part, Vector2 spot, float width, Vector2 drift)
        {
            if (chainCount >= MaxChains) return;
            chains[chainCount] = new Chain(count, link * Height);
            chainLink[chainCount] = link;
            chainWeight[chainCount] = weight;
            chainPart[chainCount] = part;
            chainSpot[chainCount] = spot;
            chainWidth[chainCount] = width;
            chainDrift[chainCount] = drift;
            chainCount++;
        }

        //Chain Root : where a chain hangs from this frame (its body part, moved by the spot)
        private Vector2 ChainRoot(int c)
        {
            Vector2 spot = chainSpot[c];
            return body[chainPart[c]].At + new Vector2(spot.X * Facing, spot.Y) * Height;
        }

        //Chain Drift : a chain's own steady push this frame (forward is the way the figure faces)
        private Vector2 ChainDrift(int c)
        {
            return new Vector2(chainDrift[c].X * Facing, chainDrift[c].Y);
        }

        //======================================================================================
        //Pose
        //======================================================================================

        //Pose : where each part wants to be this frame
        private void Pose()
        {
            if (Of == Sort.Musician) PoseBand();
            else PoseTacet();
        }

        //Sk : a point of the sketch on the page, where the figure stands now
        private Vector2 Sk(Vector2 p)
        {
            return Local((p.X - SketchMiddle) / SketchHeight, (p.Y - SketchFloor) / SketchHeight);
        }

        //Lean Round : a point of the sketch tipped by an angle round a pivot (plus : the top goes toward TACET)
        private static Vector2 LeanRound(Vector2 p, Vector2 pivot, float angle)
        {
            if (angle == 0f) return p;
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);
            Vector2 d = p - pivot;
            return pivot + new Vector2(d.X * cos - d.Y * sin, d.X * sin + d.Y * cos);
        }

        //Pose Band : standing as the sketch has them, breathing, every floating part bobbing on its own beat, the weapon ready
        //(BandRest). A blow joins the legend's KEY POSES (BandKey, ONE NOTE ONE BLOW in Figure.cs), KO lets everything fall.
        //Both levels use the same parts and poses, so F4 swaps between them smoothly.
        private void PoseBand()
        {
            KeyPose rest = BandRest();
            if (down)
            {
                //KO : the parts drop to the floor one after another and lie there (the springs pull them down loose). The body
                //lies along the floor, so the shapes drawn on it lie down too (the body's frame turns with Chest and Hip).
                float t = Ease(moveTime / BattleRules.StageFallTime);
                want[Head] = Sk(Vector2.Lerp(rest.Head, new Vector2(150f, 240f), t));
                want[Chest] = Sk(Vector2.Lerp(rest.Chest, new Vector2(132f, 246f), t));
                want[Hip] = Sk(Vector2.Lerp(rest.Hip, new Vector2(132f - (legend.Hip.Y - legend.Chest.Y), 250f), t));
                want[HandA] = Sk(Vector2.Lerp(rest.HandA, new Vector2(176f, 252f), t));
                want[HandB] = Sk(Vector2.Lerp(rest.HandB, new Vector2(60f, 254f), t));
                want[FootA] = Sk(Vector2.Lerp(rest.FootA, new Vector2(84f, 260f), t));
                want[FootB] = Sk(Vector2.Lerp(rest.FootB, new Vector2(100f, 260f), t));
                want[Extra] = want[Head];
                for (int b = 0; b < bits.Length; b++) bitWant[b] = Sk(new Vector2(70f + b * 26f, 258f));     // they fall too
                weaponWant = MathHelper.Lerp(legend.Rest, 1.45f, t);
                weapon2Want = MathHelper.Lerp(legend.Rest2, 1.5f, t);
                pose = rest;
                return;
            }

            pose = PoseNow(rest);
            KeyPose k = pose;

            //LEAN : the whole figure tips round its feet (the body's frame follows, so the shapes tip with it)
            Vector2 pivot = new Vector2(SketchMiddle, SketchFloor);
            want[Head] = Sk(LeanRound(k.Head, pivot, k.Lean * 1.15f + legend.Nod * 0.2f));
            want[Chest] = Sk(LeanRound(k.Chest, pivot, k.Lean));
            want[Hip] = Sk(LeanRound(k.Hip, pivot, k.Lean * 0.8f));
            want[HandA] = Sk(k.HandA);
            want[HandB] = Sk(k.HandB);
            want[FootA] = Sk(k.FootA);
            want[FootB] = Sk(k.FootB);
            float orbit = clock * 1.2f + seed;
            want[Extra] = want[Head] + new Vector2((float)Math.Cos(orbit) * Facing, (float)Math.Sin(orbit) * 0.35f) * (0.11f * Height);

            //BITS : each design's small floating shapes, at the places the sketch has them, bobbing and turning slowly
            bool floating = Level == 1;
            for (int b = 0; b < bits.Length; b++)
            {
                Vector2 spot = BitSpot(b);
                float a = clock * (0.5f + 0.13f * b) + b * 2.1f + seed;
                float swing = floating ? 1f : 0.7f;
                bitWant[b] = Sk(spot + new Vector2((float)Math.Cos(a) * 5f, (float)Math.Sin(a * 1.3f) * 4f) * swing);
            }
            weaponWant = k.Angle;
            weapon2Want = k.Angle2;
        }

        //Band Rest : the legend standing as the sketch has them this frame, as a key (Head, Chest and Hip before the lean,
        //hands and feet where they are). The lean of a hurt and the duck of a legend walking in are here too.
        private KeyPose BandRest()
        {
            float breath = (float)Math.Sin(clock * 1.9f + seed) * 1.2f;               // sketch units
            float lean = legend.Lean;
            float duck = 0f;
            bool floating = Level == 1;
            if (move == "hurt") lean -= 0.07f * (1f - moveTime / BattleRules.StageShiverTime);
            else if (move == "enter") duck = -5f * (float)Math.Sin(MathHelper.Clamp(moveTime / BattleRules.StageSwapTime, 0f, 1f) * MathHelper.Pi);

            //FLOAT : at LEVEL 1 the parts drift each on its own slow beat, at LEVEL 2 they hold together
            float drift = floating ? 1f : 0.3f;
            float bobA = (float)Math.Sin(clock * 1.3f + seed + 1f) * 2f * drift;
            float bobB = (float)Math.Sin(clock * 1.1f + seed + 2.3f) * 2f * drift;
            float bobF = (float)Math.Sin(clock * 1.5f + seed + 3.1f) * 1.4f * drift;
            float lift = floating ? 4f : 0f;                                           // LEVEL 1 : the feet hover a little over the floor

            Vector2 pivot = new Vector2(SketchMiddle, SketchFloor);
            KeyPose k = new KeyPose();
            k.Head = legend.Head + new Vector2(0f, breath * 1.3f + duck);
            k.Chest = legend.Chest + new Vector2(0f, breath + duck);
            k.Hip = legend.Hip + new Vector2(0f, breath * 0.6f + duck * 0.6f);
            k.HandA = LeanRound(legend.HandA + new Vector2(0f, breath + duck + bobA), pivot, lean);
            k.HandB = LeanRound(legend.HandB + new Vector2(0f, breath + duck + bobB), pivot, lean * 0.6f);
            k.FootA = legend.FootA + new Vector2(0f, -lift + bobF);
            k.FootB = legend.FootB + new Vector2(0f, -lift - bobF * 0.7f);
            k.Angle = legend.Rest;
            k.Angle2 = legend.Rest2;
            k.Lean = lean;
            return k;
        }

        //Sketch Of : a point of the page as a point of the sketch, where the figure stands now (the other way from Sk)
        private Vector2 SketchOf(Vector2 page)
        {
            return new Vector2(SketchMiddle + (page.X - Feet.X) / (H * Facing) * SketchHeight, SketchFloor + (page.Y - Feet.Y) / H * SketchHeight);
        }

        //======================================================================================
        //KEY POSES : the five legends' blows (ONE NOTE ONE BLOW, docs/BETA1_DIRECTION.md 32.21.4)
        //======================================================================================

        //Each legend fights the way their instrument is played, and each note's way changes the blow (ACTION : the baton's
        //way). The sketch's units, x toward TACET : the Head, Chest and Hip before the lean, the hands and feet where they go.
        //The numbers that every design shares (how far a body leans and steps) come from BattleRules Attack Feel.
        //   PAGANINI  DOWN-BOW : the bow drawn hard down like a violin played too hard, the way is the way the bow goes. IL CANNONE
        //             : the violin raised and aimed like a cannon, the string snaps, the body jolts back (ZZZ, Ellen's three cuts)
        //   QUANTZ    LUNGE : still with a hand behind the back like a teacher, then a long low step and a straight thrust,
        //             heels together again. Her skills are the teacher's : the rapier conducts, and points at a friend (Miyabi)
        //   PFUNDT    TWO BEATS : both fists high, the drum stretched, then one hand down and the other, the drum squashed.
        //             TIMPANI ROLL shakes hand after hand, the last note both together (Koleda's hammer)
        //   DUPORT    SHIELD BASH : the spike set, the whole slab leaning in, a step. LANCE's last thrust is the spike itself
        //             shooting out from under the slab. To guard he plants the spike and stands as a wall (Caesar's shield)
        //   PUNTO     SPIN THROW : the arm drawn far back, the body uncoiling, the ring thrown in an arc that reaches the enemy
        //             as the hit lands (Cues.TargetAt), then caught again. FANFARE : the bell raised and blown
        private KeyPose BandKey(KeyOf kind, KeyPose rest)
        {
            switch (legend.Weapon)
            {
                case Rapier: return QuantzKey(kind, rest);
                case Hammers: return PfundtKey(kind, rest);
                case Shield: return DuportKey(kind, rest);
                case Chakram: return PuntoKey(kind, rest);
                default: return PaganiniKey(kind, rest);
            }
        }

        //Body : a key from the rest pose, the whole body leaned by lean (a share of AttackLean) and shifted (sketch units),
        //the light of the blow set
        private static KeyPose Body(KeyPose rest, float lean, float dx, float dy, float light)
        {
            KeyPose k = rest;
            k.Lean = rest.Lean + lean * BattleRules.AttackLean;
            k.Head += new Vector2(dx * 1.1f, dy);
            k.Chest += new Vector2(dx, dy);
            k.Hip += new Vector2(dx * 0.6f, dy * 0.6f);
            k.Light = light;
            k.Arc = 1f;                                                      // ARCS : a swing goes round the shoulder (a thrust sets 0)
            return k;
        }

        //Weapon : the key's first weapon, at this angle turning this way, held in this hand
        private static void Weapon(ref KeyPose k, float x, float y, float angle, int turn)
        {
            k.HandA = new Vector2(x, y);
            k.Angle = angle;
            k.Turn = turn;
        }

        //Other : the key's second hand (and its weapon, PFUNDT's back stick)
        private static void Other(ref KeyPose k, float x, float y, float angle, int turn)
        {
            k.HandB = new Vector2(x, y);
            k.Angle2 = angle;
            k.Turn2 = turn;
        }

        //Paganini Key : DOWN-BOW, the bow's way is the baton's way (down, up, along to TACET, back) and CAPRICE's four cuts
        //turn from one to the next. IL CANNONE (the second skill) aims, then fires and jolts back.
        private KeyPose PaganiniKey(KeyOf kind, KeyPose rest)
        {
            KeyPose k;
            if (kind == KeyOf.Guard)
            {
                k = Body(rest, guardClean ? -0.3f : -0.7f, -2f, 0f, 0.2f);
                Weapon(ref k, 152f, 98f, -1.45f, -1);                                // the blade up in front
                k.HandB = new Vector2(128f, 116f);
                return k;
            }
            bool cannon = Cues.Button == 2;
            if (kind == KeyOf.Charge || (cannon && kind == KeyOf.Wound))
            {
                k = Body(rest, -0.35f, -4f, 0f, 0.4f);                               // IL CANNONE : the violin raised and aimed
                Weapon(ref k, 156f, 94f, -0.1f, 0);
                k.HandB = new Vector2(138f, 104f);                                   //   held in both hands
                k.Aim = 1f;
                return k;
            }
            if (cannon)
            {
                k = Body(rest, -0.8f, -10f, -2f, 1f);                                //   the shot : the body jolts back, the muzzle kicks up
                Weapon(ref k, 140f, 82f, -0.6f, -1);
                k.HandB = new Vector2(120f, 100f);
                k.Aim = 1f;
                return k;
            }
            bool wound = kind == KeyOf.Wound;
            switch (blowWay)
            {
                case Flick.Up:                                                        // UP-BOW : from low behind, up through the front
                    k = wound ? Body(rest, -0.35f, -4f, 2f, 0f) : Body(rest, 0.6f, 6f, -4f, 1f);
                    if (wound) { Weapon(ref k, 100f, 196f, 2.2f, 1); k.HandB = new Vector2(70f, 104f); }
                    else { Weapon(ref k, 178f, 70f, -1.0f, -1); k.HandB = new Vector2(60f, 140f); }
                    break;
                case Flick.Right:                                                     // along to TACET : round over the top to straight ahead
                    k = wound ? Body(rest, -0.45f, -6f, 0f, 0f) : Body(rest, 0.8f, 10f, 2f, 1f);
                    if (wound) { Weapon(ref k, 86f, 104f, -2.75f, 1); k.HandB = new Vector2(140f, 136f); }
                    else { Weapon(ref k, 200f, 116f, 0.2f, 1); k.HandB = new Vector2(60f, 112f); k.FootB = rest.FootB + new Vector2(10f, 0f); }
                    k.Arc = 0.3f;
                    break;
                case Flick.Left:                                                      // back : reached out, then drawn back down past her
                    k = wound ? Body(rest, 0.25f, 4f, 0f, 0f) : Body(rest, -0.55f, -6f, 2f, 1f);
                    if (wound) { Weapon(ref k, 184f, 92f, -0.6f, -1); k.HandB = new Vector2(66f, 120f); }
                    else { Weapon(ref k, 94f, 150f, 2.4f, 1); k.HandB = new Vector2(150f, 110f); }
                    k.Arc = 0.6f;
                    break;
                default:                                                              // DOWN-BOW : high behind the head, then hard down in front
                    k = wound ? Body(rest, -0.7f, -6f, -2f, 0f) : Body(rest, 0.9f, 8f, 6f, 1f);
                    if (wound) { Weapon(ref k, 100f, 34f, -2.6f, 1); k.HandB = new Vector2(64f, 140f); k.FootB = rest.FootB + new Vector2(2f, -4f); }
                    else
                    {
                        Weapon(ref k, 196f, 170f, 1.05f, 1);
                        k.HandB = new Vector2(54f, 98f);
                        k.FootA = rest.FootA + new Vector2(-4f, -3f);                // on her toes
                        k.FootB = rest.FootB + new Vector2(10f, 0f);
                    }
                    break;
            }
            if (Cues.Button == 1 && !blowLast) k = Scale(rest, k, BattleRules.AttackCaprice);   // CAPRICE : quick cuts from the wrist
            return k;
        }

        //Quantz Key : LUNGE, the height of the thrust is the baton's way (up a rising thrust, down a low one, back a flick of
        //the tip). SOFT AIR conducts with the rapier, THE LESSON raises it, then points it back at the friend who learns.
        private KeyPose QuantzKey(KeyOf kind, KeyPose rest)
        {
            KeyPose k;
            Vector2 pivot = new Vector2(SketchMiddle, SketchFloor);
            if (kind == KeyOf.Guard)
            {
                k = Body(rest, guardClean ? -0.2f : -0.6f, -2f, 0f, 0.2f);
                Weapon(ref k, 138f, 92f, -1.15f, -1);                                // a high parry
                return k;
            }
            bool wound = kind == KeyOf.Wound || kind == KeyOf.Charge;
            if (Cues.Button == 1)                                                     // SOFT AIR : the rapier as a baton
            {
                bool low = blowWay == Flick.Down;
                k = wound ? Body(rest, low ? -0.1f : 0.1f, 0f, -2f, 0f) : Body(rest, low ? 0.2f : -0.15f, 2f, low ? 2f : -3f, 1f);
                if (wound) Weapon(ref k, low ? 140f : 144f, low ? 84f : 128f, low ? -1.2f : 0.5f, low ? -1 : 1);
                else Weapon(ref k, low ? 150f : 146f, low ? 132f : 80f, low ? 0.55f : -1.3f, low ? 1 : -1);
                return k;
            }
            if (Cues.Button == 2)                                                     // THE LESSON : raised, then pointed at the friend
            {
                bool point = blowWay != Flick.Up;
                k = wound ? Body(rest, 0f, 0f, 0f, 0f) : Body(rest, point ? -0.2f : -0.1f, point ? -4f : 0f, point ? 0f : -4f, 1f);
                if (wound) Weapon(ref k, point ? 136f : 140f, point ? 70f : 110f, point ? -1.4f : -0.4f, 0);
                else if (point) Weapon(ref k, 88f, 116f, 2.35f, 0);                  //   back and down, at the band
                else Weapon(ref k, 132f, 66f, -1.45f, -1);
                return k;
            }
            switch (blowWay)
            {
                case Flick.Left:                                                      // a flick of the tip, back over her head
                    k = wound ? Body(rest, 0.1f, 2f, 0f, 0f) : Body(rest, -0.2f, -4f, 0f, 1f);
                    if (wound) Weapon(ref k, 172f, 108f, 0.15f, 1);
                    else Weapon(ref k, 134f, 96f, -1.95f, -1);
                    return k;
                case Flick.Up:
                case Flick.Down:
                default:
                    {
                        if (wound)
                        {
                            k = Body(rest, -0.15f, -4f, 0f, 0f);                     // drawn back, the front foot lifting to step
                            Weapon(ref k, 124f, 110f, -0.14f, 0);
                            k.FootB = rest.FootB + new Vector2(4f, -5f);
                            k.Arc = 0f;                                              //   a thrust goes straight
                            return k;
                        }
                        float step = BattleRules.AttackStep * 1.3f;                  // the LUNGE : a long low step and a straight thrust
                        float low = blowWay == Flick.Down ? 22f : (blowWay == Flick.Up ? 6f : 12f);
                        k = Body(rest, 0.55f, step, low, 1f);
                        float y = blowWay == Flick.Down ? 150f : (blowWay == Flick.Up ? 84f : 108f);
                        float a = blowWay == Flick.Down ? 0.42f : (blowWay == Flick.Up ? -0.55f : -0.05f);
                        Weapon(ref k, 216f, y, a, 0);
                        k.FootB = new Vector2(legend.FootB.X + step * 2.3f, SketchFloor);   // the front foot far forward
                        k.FootA = new Vector2(legend.FootA.X - 5f, SketchFloor);
                        k.HandB = LeanRound(legend.HandB + new Vector2(step * 0.8f, low), pivot, k.Lean * 0.6f);   // the hand stays behind her back
                        k.Arc = 0f;                                                  //   the arm goes out first, the foot lands after (FEET)
                        return k;
                    }
            }
        }

        //Pfundt Key : TWO BEATS, the hand that strikes is the blow's side (0 the front, 1 the back, 2 both). Down pounds the
        //floor, up is an upper cut, along to TACET a hook, back a swing back. THUNDER CALL beats both sticks on his own drum.
        private KeyPose PfundtKey(KeyOf kind, KeyPose rest)
        {
            KeyPose k;
            if (kind == KeyOf.Guard)
            {
                k = Body(rest, guardClean ? -0.2f : -0.6f, -2f, 0f, 0.2f);          // the two sticks crossed in front
                Weapon(ref k, 146f, 102f, -2.1f, -1);
                Other(ref k, 128f, 98f, -1.0f, 0);
                return k;
            }
            bool wound = kind == KeyOf.Wound || kind == KeyOf.Charge;
            if (wound)
            {
                k = Body(rest, -0.2f, 0f, -10f, 0f);                                 // both fists high, the drum stretched
                k.Squash = -0.25f;
                if (blowWay == Flick.Up && blowSide != 2)
                {
                    Weapon(ref k, 128f, 196f, 1.9f, 1);                               //   (an upper cut starts low)
                    Other(ref k, 96f, 60f, -2.1f, 0);
                    if (blowSide == 1) { Weapon(ref k, 150f, 52f, -2.3f, -1); Other(ref k, 110f, 198f, 1.9f, 1); }
                    return k;
                }
                Weapon(ref k, 150f, 50f, -2.3f, -1);
                Other(ref k, 124f, 46f, -2.1f, 0);
                return k;
            }
            k = Body(rest, 0.45f, 4f, 10f, 1f);                                      // the drum squashed by the blow
            k.Squash = blowSide == 2 ? 1.3f : 1f;
            if (Cues.Button == 2)                                                     // THUNDER CALL : both sticks on the skin of his drum
            {
                k = Body(rest, 0.1f, 0f, 8f, 1f);
                k.Squash = 1.3f;
                Weapon(ref k, 150f, 84f, 2.32f, 1);
                Other(ref k, 76f, 84f, 0.67f, 0);
                return k;
            }
            float x = 170f, y = 206f, a = 0.75f;                                      // DOWN : the floor pounded
            if (blowWay == Flick.Up) { x = 172f; y = 72f; a = -1.2f; }               // UP : an upper cut
            else if (blowWay == Flick.Right) { x = 186f; y = 124f; a = 0.1f; }       // a hook at TACET
            else if (blowWay == Flick.Left) { x = 126f; y = 152f; a = 2.2f; }        // a swing back
            int turn = blowWay == Flick.Up ? -1 : 1;
            if (blowSide == 1)                                                        // the back hand strikes, the front one bounces up
            {
                Weapon(ref k, 164f, 112f, -0.6f, 0);
                Other(ref k, x - 28f, y + 4f, a + 0.15f, turn);
            }
            else if (blowSide == 2)                                                   // FINISHER : both together
            {
                Weapon(ref k, x, y, a, turn);
                Other(ref k, x - 30f, y + 4f, a + 0.15f, turn);
            }
            else
            {
                Weapon(ref k, x, y, a, turn);
                Other(ref k, 124f, 50f, -2.1f, 0);                                   //   the other still up, the second beat to come
            }
            return k;
        }

        //Duport Key : SHIELD BASH, the slab leans in at the baton's angle (up a rising bash, down a slam). SHIELD stands as a
        //wall (the spike set, the lance planted), LANCE's last thrust shoots the spike out from under the slab.
        private KeyPose DuportKey(KeyOf kind, KeyPose rest)
        {
            KeyPose k;
            if (kind == KeyOf.Guard || kind == KeyOf.Charge || (Cues.Button == 1 && kind == KeyOf.Wound))
            {
                float lean = kind == KeyOf.Charge ? 0.45f : (kind == KeyOf.Guard && !guardClean ? -0.25f : 0.3f);
                k = Body(rest, lean, 4f, 0f, 0.3f);                                  // THE WALL : the slab forward, the lance planted
                Weapon(ref k, 162f, 134f, 1.45f, 1);
                k.HandB = new Vector2(84f, 146f);
                k.FootA = new Vector2(legend.FootA.X, SketchFloor);
                return k;
            }
            bool wound = kind == KeyOf.Wound;
            if (Cues.Button == 1)                                                     // SHIELD let go : a push forward
            {
                k = Body(rest, 0.9f, 14f, 2f, 1f);
                Weapon(ref k, 176f, 132f, 1.3f, 1);
                k.FootA = new Vector2(legend.FootA.X + 10f, SketchFloor);
                return k;
            }
            if (Cues.Button == 2 && blowLast)                                         // LANCE's last : the spike shoots out
            {
                k = wound ? Body(rest, -0.5f, -6f, -4f, 0f) : Body(rest, -0.3f, -4f, -6f, 1f);
                if (wound) { Weapon(ref k, 140f, 124f, -0.25f, 0); k.FootA = new Vector2(100f, 256f); }
                else { Weapon(ref k, 200f, 118f, -0.08f, 0); k.FootA = new Vector2(214f, 236f); }
                k.Arc = 0f;
                return k;
            }
            switch (blowWay)
            {
                case Flick.Up:                                                        // a rising bash from a crouch
                    k = wound ? Body(rest, -0.4f, -6f, 6f, 0f) : Body(rest, 0.8f, 18f, -10f, 1f);
                    if (wound) Weapon(ref k, 138f, 140f, 0.2f, 0);
                    else Weapon(ref k, 178f, 96f, -0.6f, -1);
                    break;
                case Flick.Down:                                                      // risen, then slammed down
                    k = wound ? Body(rest, -0.4f, -4f, -8f, 0f) : Body(rest, 1.35f, 20f, 14f, 1f);
                    if (wound) Weapon(ref k, 140f, 110f, -0.7f, 0);
                    else Weapon(ref k, 180f, 150f, 0.35f, 1);
                    break;
                case Flick.Left:                                                      // the lance swept back
                    k = wound ? Body(rest, 0.3f, 6f, 0f, 0f) : Body(rest, -0.6f, -10f, 2f, 1f);
                    if (wound) Weapon(ref k, 176f, 118f, -0.2f, 0);
                    else Weapon(ref k, 110f, 150f, 2.5f, 1);
                    break;
                default:                                                              // SHIELD BASH : rocked back, then the whole slab in, a step
                    k = wound ? Body(rest, -0.65f, -8f, 0f, 0f) : Body(rest, 1.2f, 22f, 4f, 1f);
                    if (wound) Weapon(ref k, 136f, 128f, -0.3f, 0);
                    else
                    {
                        Weapon(ref k, 186f, 126f, -0.1f, 0);
                        k.HandB = new Vector2(98f, 142f);
                        k.FootA = new Vector2(legend.FootA.X + 12f, SketchFloor);
                    }
                    break;
            }
            k.FootA = wound ? new Vector2(legend.FootA.X - 2f, SketchFloor) : k.FootA;   // the spike stays set in the floor
            if (blowWay != Flick.Left) k.Arc = 0.25f;                          // a push, nearly straight (the back sweep goes round)
            return k;
        }

        //Punto Key : SPIN THROW, the arm drawn far back and the ring thrown (the ATTACK throws the ring in the hand, CHAKRAM the
        //coil off the waist). The baton's way is the ring's arc (Throw Lift). FANFARE raises the bell and both arms.
        private KeyPose PuntoKey(KeyOf kind, KeyPose rest)
        {
            KeyPose k;
            if (kind == KeyOf.Guard)
            {
                k = Body(rest, guardClean ? -0.2f : -0.6f, -2f, 0f, 0.2f);
                Weapon(ref k, 150f, 78f, rest.Angle, 0);                              // the ring held up before the head
                k.HandB = new Vector2(90f, 120f);
                return k;
            }
            bool wound = kind == KeyOf.Wound || kind == KeyOf.Charge;
            if (Cues.Button == 1)                                                     // FANFARE : a breath in, then the bell raised and blown
            {
                k = wound ? Body(rest, -0.2f, -2f, 3f, 0.2f) : Body(rest, -0.7f, -2f, -8f, 1f);
                if (wound) { Weapon(ref k, 150f, 100f, rest.Angle, 0); k.HandB = new Vector2(88f, 116f); }
                else { Weapon(ref k, 178f, 62f, rest.Angle, 0); k.HandB = new Vector2(70f, 74f); }
                return k;
            }
            if (wound)
            {
                k = Body(rest, -0.55f, -8f, 2f, 0f);                                 // coiled : the arm far back, the other forward
                Weapon(ref k, 70f, 112f, rest.Angle, 0);
                k.HandB = new Vector2(150f, 112f);
                k.FootB = rest.FootB + new Vector2(4f, -4f);
                return k;
            }
            k = Body(rest, 0.9f, 12f, 0f, 1f);                                       // uncoiled : the arm out, the ring away
            Weapon(ref k, 192f, 98f, rest.Angle, 0);
            k.HandB = new Vector2(80f, 132f);
            k.FootA = rest.FootA + new Vector2(-2f, -4f);
            k.Throw = 1f;
            return k;
        }

        //Throw Lift : how high the ring's arc rises, a share of the way (up a lob over, down skimming the floor, back a wide
        //curve round)
        private float ThrowLift
        {
            get
            {
                switch (blowWay)
                {
                    case Flick.Up: return 0.42f;
                    case Flick.Down: return -0.1f;
                    case Flick.Left: return 0.26f;
                    default: return 0.16f;
                }
            }
        }

        //Thrown At : where a thrown ring is, from where it leaves (home) to the target and back. Out it rises on its arc, on
        //its way back (BACK) it comes in lower.
        private Vector2 ThrownAt(Vector2 home)
        {
            float t = MathHelper.Clamp(pose.Throw, 0f, 1f);
            if (t <= 0f) return home;
            Vector2 to = Cues.Aimed ? Cues.TargetAt : Local(0.85f, -0.5f);
            Vector2 d = to - home;
            float lift = phase == Phase.Back ? -0.12f : ThrowLift;
            Vector2 mid = (home + to) * 0.5f + new Vector2(0f, -Math.Abs(d.X) * lift);
            float u = 1f - t;
            return home * (u * u) + mid * (2f * u * t) + to * (t * t);
        }

        //Bit Spot : where each design's floating shapes sit in the sketch (embers, leaves, dust by the feet, drops, sparks)
        private Vector2 BitSpot(int b)
        {
            switch (legend.Weapon)
            {
                case Rapier:
                    switch (b) { case 0: return new Vector2(182, 186); case 1: return new Vector2(155, 210); case 2: return new Vector2(64, 150); case 3: return new Vector2(194, 228); default: return new Vector2(56, 200); }
                case Hammers:
                    switch (b) { case 0: return new Vector2(75, 241); case 1: return new Vector2(66, 229); case 2: return new Vector2(151, 239); case 3: return new Vector2(161, 226); default: return new Vector2(58, 247); }
                case Shield:
                    switch (b) { case 0: return new Vector2(152, 95); case 1: return new Vector2(161, 179); case 2: return new Vector2(70, 189); case 3: return new Vector2(141, 212); default: return new Vector2(62, 100); }
                case Chakram:
                    switch (b) { case 0: return new Vector2(152, 50); case 1: return new Vector2(204, 70); case 2: return new Vector2(58, 154); case 3: return new Vector2(196, 155); default: return new Vector2(70, 40); }
                default:
                    switch (b) { case 0: return new Vector2(152, 97); case 1: return new Vector2(163, 81); case 2: return new Vector2(141, 71); case 3: return new Vector2(185, 113); default: return new Vector2(87, 165); }
            }
        }

        //Tick Band : the timers of the LEGEND CUES (Figure.cs Update calls it once a frame)
        private void TickBand(float dt)
        {
            if (Of != Sort.Musician) return;
            bleed = Math.Max(0f, bleed - dt / BattleRules.FigureBleedTime);

            //IL CANNONE : the LIFE STRING leaves the head as the shot is let go (its blow's light), then grows back
            bool cannon = legend.Weapon == Cannone && Cues.Button == 2 && (phase == Phase.Cut || phase == Phase.Hold);
            if (cannon) stringLeft = Math.Min(stringLeft, 1f - pose.Light);
            else stringLeft = Math.Min(1f, stringLeft + dt * BattleRules.FigureStringGrow);

            //TURN OF THE HAND : PFUNDT's key turns slowly, fast while a skill plays, fastest in TIMPANI ROLL's roll
            float spin = BattleRules.FigureKeyIdle;
            if (Blowing && Cues.Button > 0) spin = Cues.Button == 1 ? BattleRules.FigureKeyRoll : BattleRules.FigureKeySkill;
            keySpeed = MathHelper.Lerp(keySpeed, spin, Math.Min(1f, dt * 4f));
            keyTurn += keySpeed * dt;

            //HEARTBEAT : PFUNDT's drum beats faster as life runs out
            float rate = MathHelper.Lerp(BattleRules.FigureBeatSlow, BattleRules.FigureBeatFast, 1f - MathHelper.Clamp(Cues.Life, 0f, 1f));
            beat = (beat + dt * rate) % 1f;

            //ROOTED : the strings and the ribbon leave the head the way the sketch draws them (back, then they fall), turned with it
            for (int c = 0; c < chainCount; c++)
            {
                float a = legend.Weapon == Rapier ? 2.4f + c * 0.35f : MathHelper.Pi - 0.2f + c * 0.22f;
                a = (Facing > 0f ? a : MathHelper.Pi - a) + headTurn;
                chains[c].RootWay = new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
                chains[c].Hold = 1;
            }
        }

        //======================================================================================
        //The body's frame
        //======================================================================================

        private float k = 1f;                 // page pixels for one unit of the sketch, this frame
        private Vector2 frameChest;           // the body's frame : the chest where its spring has it
        private float frameTurn;              //   and how far the body has turned from the sketch (the springs lean it, KO lays it down)
        private float frameCos = 1f;
        private float frameSin;
        private float headTurn;               // the head's own turn (most of the body's, and its NOD)
        private float headCos = 1f;
        private float headSin;

        //Set Frame : the body's frame for this frame. The line from Chest to Hip in the sketch against the line between the two
        //springs now gives how far the body has turned, so every shape drawn on it leans, sways and falls with it.
        private void SetFrame()
        {
            k = H / SketchHeight;
            frameChest = body[Chest].At;
            Vector2 rest = new Vector2((legend.Hip.X - legend.Chest.X) * Facing, legend.Hip.Y - legend.Chest.Y);
            Vector2 now = body[Hip].At - body[Chest].At;
            frameTurn = 0f;
            if (now.LengthSquared() > 0.01f && rest.LengthSquared() > 0.01f)
                frameTurn = MathHelper.WrapAngle((float)(Math.Atan2(now.Y, now.X) - Math.Atan2(rest.Y, rest.X)));
            frameCos = (float)Math.Cos(frameTurn);
            frameSin = (float)Math.Sin(frameTurn);
            headTurn = frameTurn * 0.7f + legend.Nod * Facing;
            headCos = (float)Math.Cos(headTurn);
            headSin = (float)Math.Sin(headTurn);
        }

        //T : a point of the sketch on the body (its frame). SQUASH : a blow presses the body, shorter and wider (PFUNDT's drum).
        private Vector2 T(float x, float y)
        {
            float squash = pose.Squash;
            float dx = (x - legend.Chest.X) * k * Facing * (1f + 0.06f * squash);
            float dy = (y - legend.Chest.Y) * k * (1f - 0.12f * squash);
            return frameChest + new Vector2(dx * frameCos - dy * frameSin, dx * frameSin + dy * frameCos);
        }

        //Hd : a point of the sketch on the head (its frame)
        private Vector2 Hd(float x, float y)
        {
            float dx = (x - legend.Head.X) * k * Facing;
            float dy = (y - legend.Head.Y) * k;
            return body[Head].At + new Vector2(dx * headCos - dy * headSin, dx * headSin + dy * headCos);
        }

        //Sound Point (block D, 2 Nov, docs/BETA1_DIRECTION.md 37.4) : where this legend's sound comes out, the same points GlowBand
        //lights : the violin's f hole (PAGANINI), the flute's blow hole (QUANTZ), the drum's skin (PFUNDT), the cello's f holes
        //(DUPORT), the horn's bell (PUNTO). A skill's light gathers or starts here (IL CANNONE's GATHER, Core/HitFx.Skills.cs).
        //Page pixels in the fighters' layer, like Feet. A figure that is not a legend gives its chest.
        public Vector2 SoundPoint
        {
            get
            {
                if (Of != Sort.Musician) return body[Chest].At;
                SetFrame();
                switch (legend.Weapon)
                {
                    case Rapier: return T(112, 99);
                    case Hammers: return T(112, 112);
                    case Shield: return T(112, 125);
                    case Chakram: return Hd(100, 64);
                    default: return T(112, 131);
                }
            }
        }

        //TA : an angle of the sketch (0 forward, a quarter turn down) turned with the body. TW : its way.
        private float TA(float angle)
        {
            return (Facing > 0f ? angle : MathHelper.Pi - angle) + frameTurn;
        }

        private Vector2 TW(float angle)
        {
            float a = TA(angle);
            return new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
        }

        //HA : an angle of the sketch turned with the head
        private float HA(float angle)
        {
            return (Facing > 0f ? angle : MathHelper.Pi - angle) + headTurn;
        }

        //======================================================================================
        //Draw : the band
        //======================================================================================

        private static readonly Vector2[] line = new Vector2[Shapes.MaxPoints];
        private static readonly float[] lineW = new float[Shapes.MaxPoints];

        //LIT : the stage light on the musician in front doubles what it shows (PostChain LIGHT x 2), so the colours are laid
        //down darker (BattleRules.FigureBandShade) and come out bright white and the legend's own colour, not blown out.
        //The sketch's greys : the lit side, the far side, the side in shadow of the big bodies, a dark opening.
        private static readonly Color BodyTop = new Color(246, 246, 250);
        private static readonly Color BodyLow = new Color(176, 178, 194);
        private static readonly Color BodyDeep = new Color(146, 146, 156);
        private static readonly Color HoleDark = new Color(15, 15, 19);
        private static readonly Color Smoke = new Color(112, 78, 88);                 // THE DEVIL'S BARGAIN : a dull red smoke, lighter than the dark stage so it reads
        private static readonly Color SeamGold = new Color(255, 212, 120);           // NAPOLEON'S DENT and STRADIVARIUS : gold

        //SILHOUETTE : drawn in black only, into the GLOW picture, so the musician hides the light of TACET's seam behind it
        //(BattleScreen.Stage.cs). Every colour of the band goes through C, which knows this.
        private bool silhouette;

        //C : a colour of the band, darkened for the stage light and faded by alpha (black when drawn as a SILHOUETTE).
        //Only red, green and blue are darkened : the part stays solid (Color * a number would also make it see-through).
        private Color C(Color c, float alpha)
        {
            if (silhouette) return Color.Black * (alpha * c.A / 255f);
            float shade = BattleRules.FigureBandShade;
            return new Color((byte)(c.R * shade), (byte)(c.G * shade), (byte)(c.B * shade), c.A) * alpha;
        }

        //CL : a colour that gives light (where the sound comes out, a head of light) : darkened much less than the body, so the
        //light stands out on the figure before the bloom even adds its glow (LIGHT, BattleRules.FigureLightShade)
        private Color CL(Color c, float alpha)
        {
            if (silhouette) return Color.Black * (alpha * c.A / 255f);
            float shade = BattleRules.FigureLightShade;
            return new Color((byte)(c.R * shade), (byte)(c.G * shade), (byte)(c.B * shade), c.A) * alpha;
        }

        //Draw Silhouette : the figure's shape in black (see SILHOUETTE). TACET's figures (round 6 part 4) go black through
        //Shapes.Silhouette, so the light of the hole behind them stops at their edge.
        public void DrawSilhouette(float alpha)
        {
            if (alpha <= 0.005f) return;
            if (Of != Sort.Musician)
            {
                Shapes.Silhouette = true;
                DrawTacet(alpha, 0f, 1f, 0f);
                Shapes.Silhouette = false;
                return;
            }
            silhouette = true;
            DrawBand(alpha, 0f);
            silhouette = false;
        }

        private Vector2 windNow;                                  // the wind of this frame (Update keeps it)

        //The colours of this drawing (DrawBand sets them, every design uses them)
        private float drawAlpha;
        private float drawFlash;
        private Color white;                  // the lit side
        private Color grey;                   // the far side
        private Color deep;                   // the side in shadow
        private Color edge;                   // the legend's colour
        private Color pale;                   // the legend's pale light (where the sound comes out)

        private void DrawBand(float alpha, float flash)
        {
            SetFrame();
            drawAlpha = alpha;
            drawFlash = flash;
            white = C(Color.Lerp(BodyTop, Color.White, flash), alpha);
            grey = C(Color.Lerp(BodyLow, Color.White, flash), alpha);
            deep = C(Color.Lerp(BodyDeep, Color.White, flash), alpha);
            edge = C(Color.Lerp(Tint, Hot, flash), alpha);
            pale = CL(Color.Lerp(Hot, Color.White, 0.25f + 0.75f * flash), alpha);

            switch (legend.Weapon)
            {
                case Rapier: DrawQuantz(); break;
                case Hammers: DrawPfundt(); break;
                case Shield: DrawDuport(); break;
                case Chakram: DrawPunto(); break;
                default: DrawPaganini(); break;
            }
            DrawCues();
        }

        //======================================================================================
        //PAGANINI : FIRE
        //======================================================================================

        //PAGANINI (she, 13.91) : a flame for a head with two small horns (the violin's scroll), four strings for hair (three
        //snapped short, one long and glowing : the LIFE STRING), the violin's waist as two crescents (thick in the middle, sharp at
        //the ends, 32.12), the f hole glowing where the sound comes out, needles for hands, on tiptoe, leaning back, chin up.
        //   LIFE STRING          glows with her life, flickers when it is low
        //   THE DEVIL'S BARGAIN  paying in life dims it, black smoke comes out of the f hole (Bled)
        //   IL CANNONE           the string leaves the head as the shot and grows back
        //   LEGEND FORMS         ATTACK : the blade burns (DIABOLUS IN MUSICA) / CAPRICE : long horns (CAPRICE No. 24) /
        //                        IL CANNONE : the snapped strings are whole again, all four glow (DEVIL'S CANNON)
        private void DrawPaganini()
        {
            bool floating = Level == 1;
            DrawBits();
            DrawStrings(Legendary(Cues.Stars2));

            //Back hand : a long needle pointing back and up
            Vector2 back = body[HandB].At;
            Shapes.Kite(back + TW(0.76f) * (9f * k), back + TW(-2.38f) * (20f * k), 5f * k, 0.3f, grey);

            if (!floating)
            {
                Limb(T(104, 156), body[FootA].At, TW(MathHelper.Pi) * (6f * k), legend.Limb * k * 1.2f, 1.2f * k, grey);
                Limb(T(120, 156), body[FootB].At, TW(0f) * (5f * k), legend.Limb * k * 1.2f, 1.2f * k, grey);
                Limb(T(100, 106), body[HandB].At, TW(1.57f) * (8f * k), legend.Limb * k, 1.2f * k, grey);
            }

            //Feet : two drops on tiptoe, the back one tipped
            DropShape(body[FootA].At, TA(MathHelper.PiOver2 - 0.28f), 11.5f * k, 12f * k, 0f, grey, grey);
            DropShape(body[FootB].At, TA(MathHelper.PiOver2), 11.5f * k, 13f * k, 0f, white, white);

            //WAIST : the violin's two C bouts
            Sickle(T(92, 104), T(102, 130), T(92, 156), 7.5f * k, white);
            Sickle(T(132, 104), T(122, 130), T(132, 156), 7.5f * k, grey);

            //F HOLE : the light. Paying in life dims it, the smoke rises out of it
            Color hole = Color.Lerp(pale, C(new Color(120, 30, 46), drawAlpha), 0.75f * bleed);
            int n = Cubic(T(115, 115), T(107, 124), T(119, 137), T(109, 147), 12, line, 0);
            for (int i = 0; i < n; i++) lineW[i] = (1.6f + 1.4f * (float)Math.Sin(i / (float)(n - 1) * MathHelper.Pi)) * k;
            Shapes.Stroke(line, lineW, n, hole, hole, false);
            Shapes.Circle(T(115, 115), 2.2f * k, hole);
            Shapes.Circle(T(109, 147), 2.2f * k, hole);
            if (bleed > 0f && !silhouette) DrawSmoke(T(112, 131));

            if (!floating) Limb(T(124, 106), body[HandA].At, TW(1.57f) * (8f * k), legend.Limb * k, 1.4f * k, white);

            //Head : the horns behind the flame, then the flame
            DrawHorns(Legendary(Cues.Stars1));
            float sway = (float)Math.Sin(clock * 3.1f + seed) * 0.16f + (float)Math.Sin(clock * 7.3f + seed * 2f) * 0.05f;
            if (bleed > 0f) sway += (float)Math.Sin(clock * 31f) * 0.12f * bleed;
            DropShape(Hd(112, 46), HA(-MathHelper.PiOver2), 14f * k, 12.5f * k, sway * Facing, pale, Color.Lerp(pale, edge, 0.35f));

            //Front hand : a needle pointing forward, holding IL CANNONE at its tip. It turns as the arm does (from the
            //shoulder to the hand), so in a big swing it stays an arm and does not spin with the blade.
            Vector2 grip = body[HandA].At;
            Vector2 arm = grip - T(legend.Shoulder.X, legend.Shoulder.Y);
            Vector2 armRest = new Vector2((legend.HandA.X - legend.Shoulder.X) * Facing, legend.HandA.Y - legend.Shoulder.Y);
            float turn = arm.LengthSquared() > 1f ? MathHelper.WrapAngle((float)(Math.Atan2(arm.Y, arm.X) - Math.Atan2(armRest.Y, armRest.X))) : 0f;
            Vector2 needle = Way(turn);
            Shapes.Kite(grip - needle * (27f * k), grip, 8f * k, 0.38f, white);
            DrawSmear(smearGrip, smearAngle, grip, weapon.At, 96f, 4.6f);
            DrawCannone(grip, weapon.At);
        }

        //Draw Strings : PAGANINI's four strings (chains 0 to 3). Whole (DEVIL'S CANNON) : all four long and glowing.
        private void DrawStrings(bool whole)
        {
            float life = LifeGlow();
            for (int c = 0; c < chainCount && c < 4; c++)
            {
                bool lit = c == 0 || whole;
                float share = c == 0 ? stringLeft : (whole ? 1f : 0.32f - c * 0.05f);    // SNAPPED : the short three
                int n = Math.Max(2, (int)Math.Round(chains[c].Count * share));
                if (share <= 0.02f) continue;
                for (int i = 0; i < n; i++) lineW[i] = MathHelper.Lerp(chainWidth[c] * 1.15f, chainWidth[c] * 0.55f, i / (float)Math.Max(1, chains[c].Count - 1)) * Height;
                Color from = lit ? C(Color.Lerp(BodyLow, new Color(255, 179, 194), life), drawAlpha) : C(BodyTop, drawAlpha * (0.78f - c * 0.12f));
                Color to = lit ? C(Color.Lerp(BodyLow, new Color(255, 120, 150), life), drawAlpha * 0.9f) : from;
                Shapes.Stroke(chains[c].At, lineW, n, from, to, false);
            }
        }

        //Life Glow : how bright the LIFE STRING is (its life, a flicker when low, dimmed by THE DEVIL'S BARGAIN)
        private float LifeGlow()
        {
            float life = MathHelper.Clamp(Cues.Life, 0f, 1f);
            float glow = 0.3f + 0.7f * life;
            if (life < 0.3f) glow *= 0.75f + 0.25f * (float)Math.Sin(clock * 21f + seed);
            glow *= 1f - 0.7f * bleed * (0.6f + 0.4f * (float)Math.Sin(clock * 37f));
            return MathHelper.Clamp(glow, 0f, 1f);
        }

        //Draw Horns : the violin's scroll as two small curls at the top of the flame. Long : CAPRICE No. 24.
        private void DrawHorns(bool tall)
        {
            float grow = tall ? 1.75f : 1f;
            for (int side = -1; side <= 1; side += 2)
            {
                int n = Cubic(HornAt(side, grow, 107, 52), HornAt(side, grow, 101, 47), HornAt(side, grow, 99, 40), HornAt(side, grow, 104, 37), 8, line, 0);
                n = CubicMore(HornAt(side, grow, 104, 37), HornAt(side, grow, 107, 35.5f), HornAt(side, grow, 109, 38.5f), HornAt(side, grow, 107, 40.5f), 5, line, n);
                for (int i = 0; i < n; i++) lineW[i] = MathHelper.Lerp(2.6f, 1.4f, i / (float)(n - 1)) * k * (tall ? 1.15f : 1f);
                Shapes.Stroke(line, lineW, n, edge, edge, false);
            }
        }

        //Horn At : a point of the left horn in the sketch (side -1), mirrored round the middle for the right one, grown out
        //from its root, on the head
        private Vector2 HornAt(int side, float grow, float x, float y)
        {
            Vector2 root = new Vector2(107f, 52f);
            Vector2 p = root + (new Vector2(x, y) - root) * grow;
            if (side > 0) p.X = SketchMiddle * 2f - p.X;
            return Hd(p.X, p.Y);
        }

        //IL CANNONE : the violin as a sword. The bow is its long blade with a string glowing down it, the violin's waist (its
        //two C bouts, thin crescents with sharp ends, as on her body) is the guard across it, the violin's scroll curls at the
        //end of the grip. (13.104 : the guard was two round bouts and read as something rude, nothing round is left there.)
        //LEGEND FORM (DIABOLUS IN MUSICA) : small flames run along the blade.
        private void DrawCannone(Vector2 grip, float angle)
        {
            Vector2 d = Way(angle);
            Vector2 side = new Vector2(-d.Y, d.X);
            float rot = (float)Math.Atan2(d.Y, d.X);
            Color line1 = WeaponLine();
            Shapes.Taper(grip + d * (4f * k), grip + d * (88f * k), 5.2f * k, 4f * k, white);
            Shapes.Kite(grip + d * (87f * k), grip + d * (98f * k), 4f * k, 0.02f, white);
            Shapes.Line(grip + d * (8f * k), grip + d * (86f * k), 1.4f * k, line1);
            Shapes.Line(grip - d * (8f * k), grip + d * (4f * k), 2.2f * k, white);                    // the grip
            for (int s = -1; s <= 1; s += 2)                                                            // the guard : the two C bouts
            {
                Vector2 o = side * s;
                Sickle(grip + o * (1.5f * k) + d * (3f * k), grip + o * (8f * k) + d * (1f * k), grip + o * (10.5f * k) + d * (8f * k), 2.6f * k, white);
            }
            Vector2 scroll = grip - d * (10f * k);                                                     // the scroll : a curl
            Shapes.Arc(scroll, 3.6f * k, rot + 0.4f, rot + 4.2f, 1.7f * k, 0.9f * k, white);
            Shapes.Arc(scroll + side * (0.8f * k), 1.6f * k, rot + 4.2f, rot + 7.4f, 0.9f * k, 0.4f * k, white);
            if (Legendary(Cues.Stars0) && !silhouette)
            {
                for (int f = 0; f < 5; f++)
                {
                    float along = 16f + f * 16f;
                    float lick = 0.65f + 0.35f * (float)Math.Sin(clock * 9f + f * 1.7f);
                    Vector2 root = grip + d * (along * k);
                    DropShape(root - new Vector2(0f, 13f * k * lick), -MathHelper.PiOver2 + (float)Math.Sin(clock * 5f + f) * 0.25f,
                              7f * k * lick, 7f * k, (float)Math.Sin(clock * 6f + f * 2f) * 0.3f, CL(Hot, drawAlpha), C(Tint, drawAlpha));
                }
            }
        }

        //Draw Smoke : THE DEVIL'S BARGAIN, black smoke rising out of the f hole while bleed lasts
        private void DrawSmoke(Vector2 from)
        {
            for (int i = 0; i < 5; i++)
            {
                float age = (clock * 0.8f + i / 5f) % 1f;
                Vector2 at = from + new Vector2((float)Math.Sin(clock * 2f + i * 1.9f) * 7f * k - age * 10f * k * Facing, -age * 70f * k);
                Shapes.Circle(at, (3f + age * 10f) * k, Smoke * (bleed * (1f - age) * 0.6f * drawAlpha));
            }
        }

        //======================================================================================
        //QUANTZ : WIND
        //======================================================================================

        //QUANTZ (she, 13.91) : calm and straight, heels together, leaning a little like a fencer. The flute is the line of her
        //body, its keys buttons down a court coat, a small collar, a ribbon of wind for hair, one hand behind the back like a
        //teacher, the rapier in the other.
        //   THE LESSON       a friend holding her lesson : one key is gone from the flute (it floats over their head, DrawCues)
        //   SOFT AIR         a skill runs the keys down one after another, a scale (KEY RUN)
        //   LEGEND FORMS     ATTACK : a ribbon of wind round the blade (EVENING CONCERT) / SOFT AIR : the ribbon splits in two
        //                    like wings (SANSSOUCI) / THE LESSON : the ring over her head turns (KING'S LESSON)
        private void DrawQuantz()
        {
            bool floating = Level == 1;
            DrawBits();
            if (!silhouette) DrawWindLines();
            DrawRibbons(Legendary(Cues.Stars1));

            //Back hand : behind her back
            Shapes.Circle(body[HandB].At, 3f * k, grey);
            if (!floating)
            {
                Limb(T(108, 196), body[FootA].At, TW(MathHelper.Pi) * (3f * k), legend.Limb * k * 1.3f, 1.2f * k, grey);
                Limb(T(116, 196), body[FootB].At, TW(0f) * (3f * k), legend.Limb * k * 1.3f, 1.2f * k, grey);
                Limb(T(106, 94), body[HandB].At, TW(MathHelper.Pi) * (10f * k), legend.Limb * k, 1.2f * k, grey);
            }
            DropShape(body[FootA].At, TA(MathHelper.PiOver2), 10.5f * k, 11f * k, 0f, white, white);
            DropShape(body[FootB].At, TA(MathHelper.PiOver2), 10.5f * k, 11f * k, 0f, grey, grey);

            //Coat : two long lines down her sides
            Bow3(T(104, 96), T(98, 141), T(106, 186), 1.5f * k, grey);
            Bow3(T(120, 96), T(126, 141), T(118, 186), 1.5f * k, deep);

            //The flute : the line of her body, an arch at its top, the collar over it
            Shapes.Line(T(112, 90), T(112, 198), 3.2f * k, white);
            Bow3(T(106, 95), T(112, 86), T(118, 95), 1.6f * k, white);
            line[0] = T(105, 77); line[1] = T(119, 77); line[2] = T(112, 88);
            Shapes.Fill(line, 3, white);

            //BLOW HOLE : the light
            Shapes.Ellipse(T(112, 99), 4f * k, 3f * k, TA(0f), pale, pale);
            Shapes.Ellipse(T(112, 99), 2.4f * k, 1.5f * k, TA(0f), C(HoleDark, drawAlpha), C(HoleDark, drawAlpha));

            //KEYS : six down the flute
            for (int i = 0; i < 6; i++)
            {
                Vector2 at = T(112, 113 + i * 14);
                if (Cues.LessonOut && i == 1)                                         // THE LESSON : this key is with a friend
                {
                    Shapes.Ring(at, 2.6f * k, 0.8f * k, grey);
                    continue;
                }
                float lit = KeyLight(i);
                if (lit > 0.05f)
                {
                    Shapes.Circle(at, 2.9f * k, Color.Lerp(C(BodyLow, drawAlpha), Color.Lerp(edge, pale, lit * 0.6f), lit));
                }
                else
                {
                    Shapes.Circle(at, 2.7f * k, C(HoleDark, drawAlpha));
                    Shapes.Ring(at, 2.7f * k, 1.1f * k, white);
                }
            }

            if (!floating) Limb(T(118, 94), body[HandA].At, TW(1.2f) * (6f * k), legend.Limb * k, 1.2f * k, white);

            //Head : the ring's back half, the ball of light, the ring's front half (it goes round the head)
            DrawHeadRing(true, Legendary(Cues.Stars2));
            Shapes.Circle(Hd(112, 60), 9.5f * k, pale);
            DrawHeadRing(false, Legendary(Cues.Stars2));

            //Front hand : a ring, the cup and the rapier
            Vector2 grip = body[HandA].At;
            DrawSmear(smearGrip, smearAngle, grip, weapon.At, 90f, 4f);
            DrawRapier(grip, weapon.At);
            Shapes.Ring(grip, 3.8f * k, 1.7f * k, white);
        }

        //Key Light : how lit key i is (0 the top). At rest the top keys glow and shimmer. SOFT AIR and THE LESSON run a light down
        //the six one after another (KEY RUN), a scale played down.
        private float KeyLight(int i)
        {
            if (Blowing && Cues.Button > 0)
            {
                float run = phraseTime / BattleRules.FigureKeyRunTime * 6f - i;
                if (run > 0f && run < 2.5f) return 1f - run / 2.5f;
                return run >= 2.5f ? 0.15f : 0f;
            }
            if (i >= 3) return 0f;
            return (1f - i * 0.28f) * (0.82f + 0.18f * (float)Math.Sin(clock * 2.1f - i * 0.9f + seed));
        }

        //Draw Head Ring : QUANTZ's ring, tilted, half behind the head (back) and half in front. KING'S LESSON : it turns slowly.
        private void DrawHeadRing(bool backHalf, bool turning)
        {
            float rx = 16.5f * (turning ? 0.55f + 0.45f * (float)Math.Cos(clock * 0.9f) : 1f);
            float tilt = HA(-0.24f);
            float from = backHalf ? MathHelper.Pi : 0f;
            Color ring = turning ? Color.Lerp(edge, pale, 0.35f) : edge;
            EllipseLine(Hd(112, 56), rx * k, 4.6f * k, tilt, from, from + MathHelper.Pi, 1.6f * k, backHalf ? C(Tint, drawAlpha * 0.7f) : ring);
        }

        //Draw Ribbons : the ribbon of wind (chain 0) with a pale line down it, and its twin (SANSSOUCI)
        private void DrawRibbons(bool wings)
        {
            for (int c = 0; c < chainCount && c < 2; c++)
            {
                if (c == 1 && !wings) break;
                Chain ch = chains[c];
                for (int i = 0; i < ch.Count; i++)
                {
                    float t = i / (float)(ch.Count - 1);
                    lineW[i] = chainWidth[c] * Height * (0.35f + 0.65f * (float)Math.Sin(Math.Min(1f, t * 1.6f + 0.15f) * MathHelper.Pi * 0.5f + 0.0f)) * (1f - t * 0.7f);
                }
                Shapes.Stroke(ch.At, lineW, ch.Count, C(Tint, drawAlpha * 0.85f), C(Tint, drawAlpha * 0.25f), false);
                for (int i = 0; i < ch.Count; i++) lineW[i] *= 0.28f;
                Shapes.Stroke(ch.At, lineW, ch.Count, C(Hot, drawAlpha * 0.6f), C(Hot, 0f), false);
            }
        }

        //Draw Wind Lines : three faint strokes of wind round her, drifting back
        private void DrawWindLines()
        {
            for (int i = 0; i < 3; i++)
            {
                float t = (clock * 0.12f + i * 0.33f + seed * 0.1f) % 1f;
                float fade = (float)Math.Sin(t * MathHelper.Pi);
                float x = MathHelper.Lerp(196f, 40f, t) + i * 8f;
                float y = 76f + i * 60f;
                Bow3(Sk(new Vector2(x - 16f, y + 6f)), Sk(new Vector2(x, y - 6f)), Sk(new Vector2(x + 18f, y + 4f)), 1.1f * k, C(Tint, drawAlpha * 0.35f * fade));
            }
        }

        //FLUTE RAPIER : a long needle of a blade, the cup over the hand, three small lights along it.
        //LEGEND FORM (EVENING CONCERT) : a thin ribbon of wind winds round the blade.
        private void DrawRapier(Vector2 grip, float angle)
        {
            Vector2 d = Way(angle);
            Vector2 side = new Vector2(-d.Y, d.X);
            Shapes.Kite(grip + d * (3f * k), grip + d * (92f * k), 5f * k, 0.03f, white);
            Bow3(grip + side * (7.5f * k) - d * (2f * k), grip + d * (7f * k), grip - side * (7.5f * k) - d * (2f * k), 1.7f * k, white);
            for (int i = 0; i < 3; i++) Shapes.Circle(grip + d * ((26f + i * 18f) * k), 1.3f * k, WeaponLine());
            if (Legendary(Cues.Stars0) && !silhouette)
            {
                int n = 16;
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)(n - 1);
                    line[i] = grip + d * ((8f + 84f * t) * k) + side * ((float)Math.Sin(t * 9f - clock * 6f) * 4f * k * (1f - t * 0.5f));
                    lineW[i] = 1.1f * k * (1f - t * 0.6f);
                }
                Shapes.Stroke(line, lineW, n, C(Tint, drawAlpha * 0.9f), C(Hot, drawAlpha * 0.4f), false);
            }
        }

        //======================================================================================
        //PFUNDT : EARTH
        //======================================================================================

        //PFUNDT (he, 13.91) : big and wide, round blocks for shoulders, the body a kettle drum, its skin the chest, an oval head
        //close over it (32.12), fists that are the sticks' heads, square feet, the crank key he invented on his back.
        //   TURN OF THE HAND   the winding key turns, fast while a skill plays and fastest in TIMPANI ROLL (TickBand)
        //   HEARTBEAT          the skin of the drum beats like a heart, faster as his life runs out, and the floor rings with it
        //   LEGEND FORMS       ATTACK : the sticks' heads glow copper (TWIN THUNDER) / TIMPANI ROLL : the key shines bright copper
        //                      (THUNDERCLAP) / THUNDER CALL : rings of sound go round the drum (DIE PAUKEN)
        private void DrawPfundt()
        {
            bool floating = Level == 1;
            float pulse = Pulse();
            if (!silhouette) DrawFloorRings(Sk(new Vector2(112, 262)), pulse);
            DrawBits();

            //THE KEY : behind him, on the back of the drum
            DrawWindingKey(Legendary(Cues.Stars1));

            //Back fist and its stick
            DrawSmear(smearGrip2, smearAngle2, body[HandB].At, weapon2.At, 40f, 3.4f, false);
            DrawStick(body[HandB].At, weapon2.At, 36f, 7f, grey, false);
            Shapes.Circle(body[HandB].At, 9f * k, grey);

            if (!floating)
            {
                Limb(T(100, 182), body[FootA].At, TW(MathHelper.Pi) * (6f * k), legend.Limb * k * 1.2f, 2.5f * k, grey);
                Limb(T(124, 182), body[FootB].At, TW(0f) * (6f * k), legend.Limb * k * 1.2f, 2.5f * k, grey);
                Limb(T(80, 104), body[HandB].At, TW(MathHelper.Pi) * (8f * k), legend.Limb * k, 2.4f * k, grey);
            }

            //Feet : square, flat on the floor
            Vector2 up6 = new Vector2(0f, -6f * k);
            Vector2 side6 = new Vector2(6f * k, 0f);
            Capsule(body[FootA].At + up6 - side6, body[FootA].At + up6 + side6, 12f * k, white);
            Capsule(body[FootB].At + up6 - side6, body[FootB].At + up6 + side6, 12f * k, grey);

            //Shoulders : two round blocks
            Capsule(T(77, 102), T(87, 102), 19f * k, white);
            Capsule(T(137, 102), T(147, 102), 19f * k, grey);

            //The drum : two halves of a bowl, lit at the back, the copper rim along its curve
            int n = BowlHalf(-1, line);
            Shapes.FillShaded(line, n, white, C(Color.Lerp(BodyLow, Color.White, drawFlash), drawAlpha), T(0, 112).Y, T(0, 190).Y);
            n = BowlHalf(1, line);
            Shapes.FillShaded(line, n, deep, C(new Color(120, 120, 130), drawAlpha), T(0, 112).Y, T(0, 190).Y);
            n = Cubic(T(78, 112), T(78, 160), T(94, 190), T(112, 190), 12, line, 0);
            n = CubicMore(T(112, 190), T(130, 190), T(146, 160), T(146, 112), 12, line, n);
            for (int i = 0; i < n; i++) lineW[i] = 1.8f * k;
            Shapes.Stroke(line, lineW, n, edge, edge, false);
            if (Legendary(Cues.Stars2) && !silhouette) DrawDrumWaves();

            //THE SKIN : the light, beating
            Shapes.Ellipse(T(112, 112), 34f * k, 7f * k, TA(0f), C(HoleDark, drawAlpha), C(HoleDark, drawAlpha));
            EllipseLine(T(112, 112), 34f * k, 7f * k, TA(0f), 0f, MathHelper.TwoPi, 1.7f * k, white);
            float grow = 1f + 0.12f * pulse;
            EllipseLine(T(112, 112), 24f * k * grow, 5f * k * grow, TA(0f), 0f, MathHelper.TwoPi, 0.9f * k, C(Tint, drawAlpha * (0.5f + 0.3f * pulse)));
            EllipseLine(T(112, 112), 15f * k * grow, 3.4f * k * grow, TA(0f), 0f, MathHelper.TwoPi, 0.9f * k, C(Tint, drawAlpha * (0.7f + 0.3f * pulse)));
            Shapes.Ellipse(T(112, 112), 7f * k * (0.85f + 0.3f * pulse), 2.4f * k * (0.85f + 0.3f * pulse), TA(0f), pale, pale);

            if (!floating) Limb(T(144, 104), body[HandA].At, TW(0.6f) * (8f * k), legend.Limb * k, 2.4f * k, white);

            //Head : an oval of light in a copper ring
            Shapes.Ellipse(Hd(112, 78), 14f * k, 9f * k, TA(0f), pale, pale);
            EllipseLine(Hd(112, 78), 18f * k, 11.5f * k, TA(0f), 0f, MathHelper.TwoPi, 1.7f * k, edge);

            //Front fist and its stick
            DrawSmear(smearGrip, smearAngle, body[HandA].At, weapon.At, 50f, 3.6f);
            DrawStick(body[HandA].At, weapon.At, 45f, 7.5f, white, true);
            Shapes.Circle(body[HandA].At, 9.5f * k, white);
        }

        //Pulse : the HEARTBEAT as a light, 1 at the beat and quickly down, a second softer beat after it (lub dub)
        private float Pulse()
        {
            float first = (float)Math.Exp(-beat * 14f);
            float second = beat > 0.22f ? (float)Math.Exp(-(beat - 0.22f) * 16f) * 0.55f : 0f;
            return MathHelper.Clamp(first + second, 0f, 1f);
        }

        //Bowl Half : one half of the drum's bowl (-1 the back, 1 the front) as a closed shape, into the array. Returns its size.
        private int BowlHalf(int side, Vector2[] into)
        {
            float s = side;
            into[0] = T(112, 112);
            int n = Cubic(T(112 + s * 34f, 112), T(112 + s * 34f, 160), T(112 + s * 18f, 190), T(112, 190), 12, into, 1);
            return n;
        }

        //Draw Winding Key : the crank key on his back. It turns round its own shaft, so its loops narrow and widen as it turns.
        private void DrawWindingKey(bool bright)
        {
            Vector2 root = T(82, 120);
            Vector2 way = TW(2.72f);
            Vector2 across = new Vector2(-way.Y, way.X);
            float face = (float)Math.Cos(keyTurn);
            float squash = Math.Max(0.12f, Math.Abs(face));
            Color key = bright ? Color.Lerp(edge, pale, 0.45f) : edge;
            Shapes.Line(root, root + way * (20f * k), 3f * k, key);
            for (int i = 0; i < 2; i++)
            {
                Vector2 loop = root + way * ((26f + i * 12f) * k);
                float rot = (float)Math.Atan2(way.Y, way.X);
                EllipseLine(loop, 6f * k, 8.5f * k * squash, rot, 0f, MathHelper.TwoPi, 2.4f * k, face >= 0f ? key : C(Tint, drawAlpha * 0.7f));
            }
            if (keySpeed > BattleRules.FigureKeySkill * 0.5f && !silhouette)                 // a fast turn leaves a faint arc
            {
                Vector2 middle = root + way * (32f * k);
                float a0 = keyTurn * 0.15f;
                Shapes.Arc(middle, 16f * k, a0, a0 + 2.2f, 1.2f * k, 0.2f * k, C(Tint, drawAlpha * 0.45f));
            }
        }

        //Draw Stick : a timpani stick from the fist along its angle, its ball at the end. The front ball has a glint.
        //LEGEND FORM (TWIN THUNDER) : both balls glow copper.
        private void DrawStick(Vector2 fist, float angle, float length, float ball, Color color, bool front)
        {
            Vector2 d = Way(angle);
            Shapes.Line(fist + d * (3f * k), fist + d * (length * k), 2.8f * k, color);
            Vector2 end = fist + d * ((length + ball * 0.7f) * k);
            bool glow = Legendary(Cues.Stars0);
            Shapes.Circle(end, ball * k, glow ? Color.Lerp(edge, pale, 0.3f) : color);
            if (front || glow) Shapes.Circle(end + new Vector2(2f * Facing, -2f) * k, 2f * k, pale);
        }

        //Draw Floor Rings : the floor answers each beat, two copper rings and two cracks round his feet
        private void DrawFloorRings(Vector2 at, float pulse)
        {
            float a = drawAlpha;
            EllipseLine(at, 44f * k * (1f + 0.06f * pulse), 4f * k, 0f, 0f, MathHelper.TwoPi, 1.3f * k, C(Tint, a * (0.35f + 0.35f * pulse)));
            EllipseLine(at, 72f * k * (1f + 0.04f * pulse), 6.5f * k, 0f, 0f, MathHelper.TwoPi, 1.1f * k, C(Tint, a * (0.16f + 0.18f * pulse)));
            Color crack = C(Tint, a * 0.45f);
            line[0] = Sk(new Vector2(62, 262)); line[1] = Sk(new Vector2(50, 257)); line[2] = Sk(new Vector2(42, 261)); line[3] = Sk(new Vector2(30, 255));
            for (int i = 0; i < 4; i++) lineW[i] = (1.1f - i * 0.2f) * k;
            Shapes.Stroke(line, lineW, 4, crack, crack, false);
            line[0] = Sk(new Vector2(162, 262)); line[1] = Sk(new Vector2(176, 256)); line[2] = Sk(new Vector2(186, 261)); line[3] = Sk(new Vector2(200, 254));
            Shapes.Stroke(line, lineW, 4, crack, crack, false);
        }

        //Draw Drum Waves : DIE PAUKEN, two thin rings of sound going round the drum
        private void DrawDrumWaves()
        {
            for (int i = 0; i < 2; i++)
            {
                float turn = clock * (0.9f + i * 0.4f) + i * 2f;
                float rx = 46f + i * 8f;
                EllipseLine(T(112, 150), rx * k, 10f * k, TA(0.18f * (float)Math.Sin(turn)), 0f, MathHelper.TwoPi, 1f * k, C(Tint, drawAlpha * (0.4f + 0.2f * (float)Math.Sin(turn * 2f))));
            }
        }

        //======================================================================================
        //DUPORT : WATER
        //======================================================================================

        //DUPORT (he, 13.91) : the tallest, a slab that is both his body and his shield, cut in at the waist like a cello (32.12 :
        //more, so it is not a door), a diamond head like a knight's mask with a slit of light, one leg that is the cello's spike,
        //square hands, still as a monument, a little tilted.
        //   THE SCAR      the emperor's scar is the light. Each VOLUME it gives him cracks a new branch off it, and it shines brighter
        //   SHIELD        on the friend he covers, his ghost stands in front of them (DrawCues)
        //   LEGEND FORMS  ATTACK : the lance gilded (STRADIVARIUS) / SHIELD : the scar a seam of gold, the wound that made him
        //                 stronger (NAPOLEON'S DENT) / LANCE : the spike glows and the water rings stay (THE LAST LANCE)
        private void DrawDuport()
        {
            bool floating = Level == 1;
            bool spike = Legendary(Cues.Stars2);
            if (!silhouette) DrawRipples(body[FootA].At, spike);
            DrawBits();

            //Back hand
            Shapes.Box(body[HandB].At, 9f * k, 9f * k, TA(0f), grey);
            if (!floating) Limb(T(90, 80), body[HandB].At, TW(MathHelper.Pi) * (10f * k), legend.Limb * k, 2.4f * k, grey);

            //The spike : from the bottom of the slab to the floor, where its spring has it
            line[0] = T(108, 204);
            line[1] = T(116, 204);
            line[2] = body[FootA].At;
            Shapes.Fill(line, 3, spike ? Color.Lerp(white, edge, 0.5f) : white);

            //The slab : two halves, the waist cut in
            int n = SlabHalf(-1, line);
            Shapes.FillShaded(line, n, white, C(Color.Lerp(new Color(214, 214, 222), Color.White, drawFlash), drawAlpha), T(0, 72).Y, T(0, 206).Y);
            n = SlabHalf(1, line);
            Shapes.FillShaded(line, n, deep, C(new Color(118, 118, 128), drawAlpha), T(0, 72).Y, T(0, 206).Y);
            n = SlabHalf(1, line);
            for (int i = 0; i < n - 2; i++) line[i] = line[i + 1];
            for (int i = 0; i < n - 2; i++) lineW[i] = 1.1f * k;
            Shapes.Stroke(line, lineW, n - 2, C(Tint, drawAlpha * 0.55f), C(Tint, drawAlpha * 0.55f), false);   // the edge toward TACET

            DrawScar(Legendary(Cues.Stars1));

            if (!floating) Limb(T(134, 80), body[HandA].At, TW(1.2f) * (6f * k), legend.Limb * k, 2.4f * k, white);

            //Head : a diamond, its far half in shadow, a slit of light down its middle
            line[0] = Hd(112, 24); line[1] = Hd(112, 64); line[2] = Hd(100, 44);
            Shapes.Fill(line, 3, white);
            line[0] = Hd(112, 24); line[1] = Hd(124, 44); line[2] = Hd(112, 64);
            Shapes.Fill(line, 3, deep);
            Shapes.Line(Hd(112, 33), Hd(112, 55), 2.6f * k, pale);

            //Front hand and the lance (the bow)
            Vector2 grip = body[HandA].At;
            DrawSmear(smearGrip, smearAngle, grip, weapon.At, 80f, 4f);
            DrawLance(grip, weapon.At);
            Shapes.Box(grip, 10f * k, 10f * k, TA(0.14f), white);
        }

        //Slab Half : one half of DUPORT's slab (-1 the back, 1 the front), the cello's waist cut in, into the array
        private int SlabHalf(int side, Vector2[] into)
        {
            float s = side;
            into[0] = T(112, 72);
            into[1] = T(112 + s * 24f, 72);
            into[2] = T(112 + s * 29f, 110);
            int n = Cubic(T(112 + s * 29f, 110), T(112 + s * 16f, 118), T(112 + s * 16f, 132), T(112 + s * 29f, 140), 9, into, 2);
            into[n++] = T(112 + s * 24f, 206);
            into[n++] = T(112, 206);
            return n;
        }

        //THE SCAR : the light. A crack down the slab, a branch for each VOLUME (up to six), brighter with each.
        //NAPOLEON'S DENT : a seam of gold.
        private static readonly Vector2[] scar =
        {
            new Vector2(94, 94), new Vector2(103, 110), new Vector2(100, 117), new Vector2(112, 138),
            new Vector2(109, 146), new Vector2(123, 166), new Vector2(120, 173), new Vector2(131, 188),
        };
        private static readonly int[] branchAt = { 3, 5, 1, 4, 6, 2 };

        private void DrawScar(bool gold)
        {
            int volume = Math.Min(Cues.Volume, branchAt.Length);
            float shine = 0.55f + 0.45f * volume / branchAt.Length;
            Color glow = gold ? C(SeamGold, drawAlpha) : C(Tint, drawAlpha);
            Color core = Color.Lerp(glow, pale, 0.4f + 0.5f * shine);
            for (int i = 0; i < scar.Length; i++) line[i] = T(scar[i].X, scar[i].Y);
            for (int i = 0; i < scar.Length; i++) lineW[i] = 4.2f * k;
            Shapes.Stroke(line, lineW, scar.Length, glow, glow, false);
            for (int i = 0; i < scar.Length; i++) lineW[i] = 2f * k;
            Shapes.Stroke(line, lineW, scar.Length, core, core, false);
            for (int b = 0; b < volume; b++)                                               // THE SCAR : a branch for each VOLUME
            {
                Vector2 from = scar[branchAt[b]];
                float side = b % 2 == 0 ? 1f : -1f;
                Vector2 mid = from + new Vector2(side * 6f, 4f + b);
                Vector2 end = mid + new Vector2(side * 5f, 6f);
                line[0] = T(from.X, from.Y); line[1] = T(mid.X, mid.Y); line[2] = T(end.X, end.Y);
                lineW[0] = 2.4f * k; lineW[1] = 1.4f * k; lineW[2] = 0.4f * k;
                Shapes.Stroke(line, lineW, 3, core, glow, false);
            }
        }

        //Draw Ripples : rings of water spreading from the spike's point. THE LAST LANCE : stronger.
        private void DrawRipples(Vector2 at, bool strong)
        {
            for (int i = 0; i < 3; i++)
            {
                float t = (clock * 0.35f + i / 3f) % 1f;
                float fade = (1f - t) * (strong ? 0.9f : 0.6f);
                EllipseLine(at, (8f + t * 44f) * k, (1.6f + t * 4.6f) * k, 0f, 0f, MathHelper.TwoPi, 1.2f * k, C(Tint, drawAlpha * fade));
            }
        }

        //THE LANCE : the bow as a long thin lance, a line of water down it. LEGEND FORM (STRADIVARIUS) : the line is gold.
        private void DrawLance(Vector2 grip, float angle)
        {
            Vector2 d = Way(angle);
            Shapes.Taper(grip + d * (2f * k), grip + d * (80f * k), 5f * k, 2.2f * k, white);
            Shapes.Line(grip + d * (4f * k), grip + d * (73f * k), 1.2f * k, Legendary(Cues.Stars0) ? C(SeamGold, drawAlpha) : WeaponLine());
        }

        //======================================================================================
        //PUNTO : LIGHT
        //======================================================================================

        //PUNTO (unsaid, 13.91 : no curve or edge added, a plain way of standing) : mid stride, leaning forward like a traveller,
        //the horn's bell a halo of light behind the head, a curl of tube for hair, the coil round the waist, a thin pipe from the
        //head to it (32.12 : the body was too empty), the chakram held out.
        //   THE HUNTING CALL   a real horn's bell faces back : its light shines back at whoever plays right after them, in that
        //                      musician's colour (GlowBand), and is dim when nobody does
        //   CHAKRAM            the skill throws the coil off the waist, the waist is bare until it comes back round
        //   LEGEND FORMS       ATTACK : a second ring on the chakram (WIDER ARC) / FANFARE : two rims and long rays, a sunrise
        //                      (THE WHOLE HUNT) / CHAKRAM : two coils round the waist (WHIRLWIND)
        private void DrawPunto()
        {
            bool floating = Level == 1;
            bool sunrise = Legendary(Cues.Stars1);
            DrawBits();

            //THE BELL : behind the head
            Vector2 bell = Hd(100, 64);
            Color brass = edge;

            //THE HUNTING CALL : the bell's light falls back at whoever plays right after them, in their colour (a soft cone,
            //brighter in the GLOW), faint when nobody does
            if (!silhouette)
            {
                float a = HA(MathHelper.Pi - 0.1f);
                Vector2 back = new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
                line[0] = bell;
                line[1] = bell + back * (140f * k);
                lineW[0] = 14f * k;
                lineW[1] = 110f * k;
                Color call = CL(Cues.Calling ? Cues.CallTint : Tint, drawAlpha * (Cues.Calling ? BattleRules.FigureCallLight : BattleRules.FigureCallLight * 0.2f));
                Shapes.Stroke(line, lineW, 2, call, Color.Transparent, false);
            }
            int rays = 14;
            for (int i = 0; i < rays; i++)
            {
                float a = i / (float)rays * MathHelper.TwoPi + clock * 0.08f;
                float far = (i % 2 == 1 ? 36f : 42f) * (sunrise ? 1.4f : 1f);
                Vector2 w = new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
                Shapes.Taper(bell + w * (30f * k), bell + w * (far * k), 1.6f * k, 0.5f * k, C(Tint, drawAlpha * 0.8f));
            }
            Shapes.Ring(bell, 26f * k, 2.3f * k, brass);
            if (sunrise) Shapes.Ring(bell, 32f * k, 1.4f * k, C(Tint, drawAlpha * 0.85f));
            Shapes.Ring(bell, 18f * k, 0.9f * k, C(Tint, drawAlpha * 0.55f));
            Shapes.Circle(bell, 5f * k, pale);

            //Back hand, back foot (the heel up, mid stride)
            Shapes.Circle(body[HandB].At, 3.2f * k, grey);
            if (!floating)
            {
                Limb(T(110, 138), body[FootA].At, TW(MathHelper.Pi) * (6f * k), legend.Limb * k * 1.3f, 1.2f * k, grey);
                Limb(T(116, 96), body[HandB].At, TW(MathHelper.Pi) * (8f * k), legend.Limb * k, 1.2f * k, grey);
            }
            DropShape(body[FootA].At, TA(MathHelper.PiOver2 - 0.84f), 10.5f * k, 11f * k, 0f, grey, grey);
            if (!floating) Limb(T(124, 138), body[FootB].At, TW(0f) * (4f * k), legend.Limb * k * 1.3f, 1.2f * k, white);
            DropShape(body[FootB].At, TA(MathHelper.PiOver2), 10.5f * k, 11f * k, 0f, white, white);

            //The pipe from the head down to the coil
            Bow3(Hd(122, 73), T(128, 100), T(121, 118), 1.3f * k, C(Tint, drawAlpha * 0.8f));

            //THE COIL : round the waist (CHAKRAM throws it at the enemy and catches it again, WHIRLWIND doubles it)
            float thrown = Cues.Button == 2 ? pose.Throw : 0f;
            float stay = 1f - MathHelper.Clamp(thrown * 3f, 0f, 1f);
            if (stay > 0.02f)
            {
                DrawCoil(0f, C(Tint, drawAlpha * stay));
                if (Legendary(Cues.Stars2)) DrawCoil(7f, C(Tint, drawAlpha * stay * 0.75f));
            }
            if (thrown > 0.01f)                                                             // the coil flying as a ring
                DrawRing(ThrownAt(T(118, 132)), 13f * k, clock * 22f * Facing, false);

            if (!floating) Limb(T(126, 96), body[HandA].At, TW(0.4f) * (6f * k), legend.Limb * k, 1.2f * k, white);

            //Head : the curl of tube, the ball of light
            int n = Cubic(Hd(119, 57), Hd(110, 50), Hd(113, 40), Hd(121, 43), 8, line, 0);
            n = CubicMore(Hd(121, 43), Hd(127, 46), Hd(124, 54), Hd(118, 52), 7, line, n);
            for (int i = 0; i < n; i++) lineW[i] = 2.1f * k;
            Shapes.Stroke(line, lineW, n, brass, brass, false);
            Shapes.Circle(Hd(124, 64), 8.8f * k, pale);

            //Front hand and the chakram
            Vector2 grip = body[HandA].At;
            bool spinning = Cues.Button <= 0 && pose.Throw > 0.01f;
            DrawRing(ChakramAt(grip), 15f * k, clock * (spinning ? 22f : 2.2f) * Facing, Legendary(Cues.Stars0));
            Shapes.Circle(grip, 3.8f * k, white);
        }

        //Draw Coil : the tube coiled round the waist (the sketch's spiral), pushed up by lift for the second coil
        private void DrawCoil(float lift, Color color)
        {
            int n = Cubic(T(98, 126 - lift), T(110, 114 - lift), T(140, 118 - lift), T(136, 132 - lift), 10, line, 0);
            n = CubicMore(T(136, 132 - lift), T(132, 146 - lift), T(102, 146 - lift), T(104, 134 - lift), 10, line, n);
            n = CubicMore(T(104, 134 - lift), T(106, 124 - lift), T(124, 125 - lift), T(124, 133 - lift), 8, line, n);
            for (int i = 0; i < n; i++) lineW[i] = 2.4f * k;
            Shapes.Stroke(line, lineW, n, color, color, false);
        }

        //Draw Ring : the HORN CHAKRAM, a white ring with four blades and a thin brass line inside. WIDER ARC : a second ring.
        private void DrawRing(Vector2 middle, float r, float spin, bool wide)
        {
            Shapes.Ring(middle, r, 3f * k, white);
            for (int i = 0; i < 4; i++)
            {
                float a = spin + i * MathHelper.PiOver2;
                Vector2 o = new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
                Vector2 t = new Vector2(-o.Y, o.X);
                line[0] = middle + o * (r + 4f * k);
                line[1] = middle + o * (r - 1f * k) + t * (4f * k);
                line[2] = middle + o * (r - 1f * k) - t * (4f * k);
                Shapes.Fill(line, 3, white);
            }
            Shapes.Ring(middle, r, 1f * k, WeaponLine());
            if (wide) Shapes.Ring(middle, r + 7f * k, 1.2f * k, C(Tint, drawAlpha * 0.8f));
        }

        //Chakram At : where the held ring is. Beside the hand, and in an ATTACK (SPIN THROW) thrown at the enemy, reaching it
        //as the hit lands, then back into the hand (ThrownAt)
        private Vector2 ChakramAt(Vector2 grip)
        {
            Vector2 held = grip + new Vector2(22f * Facing, -8f) * k;
            if (Cues.Button > 0) return held;
            return ThrownAt(held);
        }

        //======================================================================================
        //LEGEND CUES on the one in front
        //======================================================================================

        //Draw Cues : what other legends have put on this one. THE LESSON : QUANTZ's key floats over the head in a small ring (FOCUS).
        //SHIELD : DUPORT's ghost stands in front, its spike in the floor.
        private void DrawCues()
        {
            if (silhouette) return;
            if (Cues.Taught)
            {
                Vector2 over = body[Head].At + new Vector2(0f, -(30f + (float)Math.Sin(clock * 2.4f) * 2f) * k);
                Color wind = C(Palette.SpotWind, drawAlpha);
                EllipseLine(over, 10f * k, 2.8f * k, TA(-0.1f), 0f, MathHelper.TwoPi, 1.4f * k, wind);
                Shapes.Circle(over + new Vector2(0f, -1f * k), 2.9f * k, C(Palette.SpotWindHot, drawAlpha));
            }
            if (Cues.Covered)
            {
                //SHIELD : the slab of DUPORT's body, see through, a step in front (its own design's numbers, by Sk)
                Color water = C(Palette.SpotWater, drawAlpha * 0.2f);
                Color rim = C(Palette.SpotWater, drawAlpha * 0.75f);
                float x = 70f;
                line[0] = Sk(new Vector2(88 + x, 82)); line[1] = Sk(new Vector2(136 + x, 82)); line[2] = Sk(new Vector2(141 + x, 118));
                line[3] = Sk(new Vector2(129 + x, 128)); line[4] = Sk(new Vector2(141 + x, 140)); line[5] = Sk(new Vector2(136 + x, 214));
                line[6] = Sk(new Vector2(88 + x, 214)); line[7] = Sk(new Vector2(83 + x, 140)); line[8] = Sk(new Vector2(95 + x, 128));
                line[9] = Sk(new Vector2(83 + x, 118));
                Shapes.Fill(line, 10, water);
                for (int i = 0; i < 10; i++) lineW[i] = 1.4f * k;
                Shapes.Stroke(line, lineW, 10, rim, rim, true);
                line[0] = Sk(new Vector2(108 + x, 214)); line[1] = Sk(new Vector2(116 + x, 214)); line[2] = Sk(new Vector2(112 + x, 262));
                Shapes.Fill(line, 3, rim);
                float t = (clock * 0.5f) % 1f;
                EllipseLine(Sk(new Vector2(112 + x, 262)), (10f + t * 30f) * k, (2f + t * 3f) * k, 0f, 0f, MathHelper.TwoPi, 1f * k, C(Palette.SpotWater, drawAlpha * (1f - t) * 0.6f));
            }
        }

        //======================================================================================
        //Shapes the designs share
        //======================================================================================

        //SMEAR (13.104 : smoother) : a weapon that turned fast this frame leaves two faint copies of its blade between where it
        //was last frame and where it is now, the animator's smear frames (the blade's own grey, not a light : the VFX trails
        //of the elements are a later step)
        private void DrawSmear(Vector2 grip0, float angle0, Vector2 grip1, float angle1, float length, float width, bool main = true)
        {
            if (main) { ribbonReach = length * k; DrawRibbon(width); }
            if (silhouette || BattleRules.AttackSmear <= 0f) return;
            float turn = MathHelper.WrapAngle(angle1 - angle0);
            if (Math.Abs(turn) < BattleRules.AttackSmearTurn && Vector2.DistanceSquared(grip0, grip1) < 144f * k * k) return;
            for (int g = 1; g <= 2; g++)
            {
                float t = g / 3f;
                Vector2 grip = Vector2.Lerp(grip0, grip1, t);
                Vector2 d = Way(angle0 + turn * t);
                Shapes.Taper(grip + d * (4f * k), grip + d * (length * k), width * k, width * 0.35f * k, C(BodyTop, drawAlpha * BattleRules.AttackSmear * (0.5f + 0.5f * t)));
            }
        }

        //Draw Ribbon (RIBBON, Worldless) : a tapered trail of light along the tip's last places, only while the tip runs fast.
        //Two layers : a wide soft one in the legend's colour, and a thin hot core, white at the tip, gone at the tail.
        private void DrawRibbon(float width)
        {
            if (silhouette || BattleRules.AttackRibbon <= 0f || ribbonCount < 3) return;
            float run = 0f;
            for (int i = 1; i < ribbonCount; i++) run += Vector2.Distance(ribbon[i - 1], ribbon[i]);
            float a = MathHelper.Clamp((run - BattleRules.AttackRibbonFrom * k) / ((BattleRules.AttackRibbonFull - BattleRules.AttackRibbonFrom) * k), 0f, 1f) * BattleRules.AttackRibbon;
            if (a <= 0.01f) return;
            int n = SmoothRibbon();
            for (int i = 0; i < n; i++) ribbonWide[i] = width * k * 2.6f * (float)Math.Pow(1f - i / (float)(n - 1), 1.2f);
            Shapes.Stroke(ribbonSmooth, ribbonWide, n, edge * (0.45f * a), edge * 0f, false);
            for (int i = 0; i < n; i++) ribbonWide[i] *= 0.35f;
            Shapes.Stroke(ribbonSmooth, ribbonWide, n, pale * a, edge * 0f, false);
        }

        //Smooth Ribbon (CHAIKIN) : the tip jumps far between frames in a fast cut, so the trail is rounded off twice : each
        //step between two points becomes two points a quarter and three quarters along it. Returns how many points there are.
        //ADVANCED PART : corner cutting. Cutting every corner this way twice gives a curve close to a smooth spline through
        //the same points, with the two ends kept where they were.
        private int SmoothRibbon()
        {
            int n = ribbonCount;
            for (int i = 0; i < n; i++) ribbonSmooth[i] = ribbon[i];
            for (int pass = 0; pass < 2; pass++)
            {
                int m = 0;
                ribbonStep[m++] = ribbonSmooth[0];
                for (int i = 0; i + 1 < n; i++)
                {
                    ribbonStep[m++] = Vector2.Lerp(ribbonSmooth[i], ribbonSmooth[i + 1], 0.25f);
                    ribbonStep[m++] = Vector2.Lerp(ribbonSmooth[i], ribbonSmooth[i + 1], 0.75f);
                }
                ribbonStep[m++] = ribbonSmooth[n - 1];
                for (int i = 0; i < m; i++) ribbonSmooth[i] = ribbonStep[i];
                n = m;
            }
            return n;
        }

        //Weapon Line : the line of the legend's colour down a weapon, brighter with each star of the ATTACK (WEAPON TIERS)
        private Color WeaponLine()
        {
            float tier = MathHelper.Clamp(Cues.Stars0 / (float)RehearsalList.Most, 0f, 1f);
            return Color.Lerp(edge, pale, 0.15f + 0.45f * tier);
        }

        //Draw Bits : the five small shapes floating round the figure, of the design's element
        private void DrawBits()
        {
            if (silhouette) return;
            for (int b = 0; b < bits.Length; b++)
            {
                Vector2 at = bits[b].At;
                float turn = clock * (0.6f + 0.3f * b) + b;
                float a = drawAlpha * (0.55f + 0.25f * (float)Math.Sin(clock * 1.7f + b * 1.3f));
                switch (legend.Weapon)
                {
                    case Rapier:                                                            // leaves of light
                        Vector2 w = new Vector2((float)Math.Cos(turn), (float)Math.Sin(turn)) * (5f * k);
                        Shapes.Kite(at - w, at + w, 4.5f * k, 0.5f, C(Tint, a));
                        break;
                    case Hammers: Shapes.Box(at, 2.4f * k, 2.4f * k, 0f, C(Tint, a)); break;                 // dust
                    case Shield: DropShape(at, -MathHelper.PiOver2, 3.6f * k, 3.4f * k, 0f, C(Tint, a), C(Tint, a)); break;   // drops of water
                    case Chakram:                                                           // four pointed sparks
                        float r = (4f + (float)Math.Sin(clock * 3f + b) * 1f) * k;
                        Shapes.Kite(at - new Vector2(r, 0f), at + new Vector2(r, 0f), 1.8f * k, 0.5f, C(Hot, a));
                        Shapes.Kite(at - new Vector2(0f, r), at + new Vector2(0f, r), 1.8f * k, 0.5f, C(Hot, a));
                        break;
                    default: Shapes.Polygon(at, 2f * k, 4, turn * 0.5f, C(Tint, a)); break;                    // embers
                }
            }
        }

        //Draw Limbs (LEVEL 2) are drawn by each design between its parts, with Limb
        //Limb : one curved tapered stroke from a root to a hand or a foot, bowed by bend at its middle
        private void Limb(Vector2 from, Vector2 to, Vector2 bend, float w0, float w1, Color color)
        {
            Shapes.Bend(from, (from + to) * 0.5f + bend, to, 10, line);
            for (int i = 0; i < 10; i++) lineW[i] = MathHelper.Lerp(w0, w1, i / 9f);
            Shapes.Stroke(line, lineW, 10, color, color, false);
        }

        //Drop Shape : a drop or a flame. Its sharp end points the way given (an angle on the page), the round end opposite.
        //at is the sharp end's point, length the half length, width the widest. sway bends the sharp end sideways (a flame's
        //flicker). The colour runs from top to low down the page.
        private void DropShape(Vector2 tip, float angle, float length, float width, float sway, Color top, Color low)
        {
            Vector2 up = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            Vector2 side = new Vector2(-up.Y, up.X);
            Vector2 middle = tip - up * length;
            int n = 22;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            for (int i = 0; i < n; i++)
            {
                float t = MathHelper.TwoPi * i / n;
                float along = (float)Math.Cos(t);                                 // 1 at the sharp end, -1 at the round end
                float across = (float)Math.Sin(t) * (1f - along) * 0.5f;          // pinched toward the sharp end
                float bend = sway * (along + 1f) * (along + 1f) * 0.25f;          // the sharp end leans most
                line[i] = middle + up * (along * length) + side * ((across + bend) * width);
                minY = Math.Min(minY, line[i].Y);
                maxY = Math.Max(maxY, line[i].Y);
            }
            Shapes.FillShaded(line, n, top, low, minY, maxY);
        }

        //Sickle : a crescent along a curve from a to c bowed toward b, thick in its middle and sharp at both ends
        private static void Sickle(Vector2 a, Vector2 b, Vector2 c, float width, Color color)
        {
            int n = 16;
            Shapes.Bend(a, b * 2f - (a + c) * 0.5f, c, n, line);
            for (int i = 0; i < n; i++) lineW[i] = width * (float)Math.Pow(Math.Sin(i / (float)(n - 1) * MathHelper.Pi), 0.8);
            Shapes.Stroke(line, lineW, n, color, color, false);
        }

        //Bow 3 : a thin curve of one width from a to c through b
        private static void Bow3(Vector2 a, Vector2 b, Vector2 c, float width, Color color)
        {
            int n = 12;
            Shapes.Bend(a, b * 2f - (a + c) * 0.5f, c, n, line);
            for (int i = 0; i < n; i++) lineW[i] = width;
            Shapes.Stroke(line, lineW, n, color, color, false);
        }

        //Capsule : a bar with round ends (PFUNDT's shoulders and feet)
        private static void Capsule(Vector2 a, Vector2 b, float width, Color color)
        {
            Shapes.Line(a, b, width, color);
            Shapes.Circle(a, width * 0.5f, color);
            Shapes.Circle(b, width * 0.5f, color);
        }

        //Ellipse Line : part of an ellipse's outline (from and to are angles round it), turned by angle
        private static void EllipseLine(Vector2 at, float rx, float ry, float angle, float from, float to, float width, Color color)
        {
            if (rx <= 0.05f || ry <= 0.05f) return;
            bool closed = Math.Abs(to - from) >= MathHelper.TwoPi - 0.001f;
            int n = Math.Max(8, (int)(Shapes.Segments(Math.Max(rx, ry)) * Math.Abs(to - from) / MathHelper.TwoPi) + 2);
            if (n > Shapes.MaxPoints - 1) n = Shapes.MaxPoints - 1;
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);
            int count = closed ? n : n + 1;
            for (int i = 0; i < count; i++)
            {
                float t = from + (to - from) * i / n;
                float x = (float)Math.Cos(t) * rx;
                float y = (float)Math.Sin(t) * ry;
                line[i] = at + new Vector2(x * cos - y * sin, x * sin + y * cos);
                lineW[i] = width;
            }
            Shapes.Stroke(line, lineW, count, color, color, closed);
        }

        //Cubic : n points along a cubic BEZIER from a to d pulled by b and c, into the array from start. Returns the next free
        //place. CubicMore goes on from a curve already there (its first point is the last one's end, so it is not repeated).
        private static int Cubic(Vector2 a, Vector2 b, Vector2 c, Vector2 d, int n, Vector2[] into, int start)
        {
            for (int i = 0; i < n; i++)
            {
                float t = n == 1 ? 0f : (float)i / (n - 1);
                float u = 1f - t;
                into[start + i] = a * (u * u * u) + b * (3f * u * u * t) + c * (3f * u * t * t) + d * (t * t * t);
            }
            return start + n;
        }

        private static int CubicMore(Vector2 a, Vector2 b, Vector2 c, Vector2 d, int n, Vector2[] into, int start)
        {
            for (int i = 1; i < n; i++)
            {
                float t = (float)i / (n - 1);
                float u = 1f - t;
                into[start + i - 1] = a * (u * u * u) + b * (3f * u * u * t) + c * (3f * u * t * t) + d * (t * t * t);
            }
            return start + n - 1;
        }

        //======================================================================================
        //Glow : the band
        //======================================================================================

        //Glow Band : the lights each legend gives off, into the GLOW picture (they feed the bloom) : where the sound comes out,
        //the head, the weapon (stronger in the cut), and the cues (the LIFE STRING, THE HUNTING CALL, THE LESSON, SHIELD)
        private void GlowBand(SpriteBatch sb, float alpha)
        {
            SetFrame();
            GlowFloor(sb, 0.5f, Tint * (BattleRules.FigureFloorLight * alpha));          // FLOOR LIGHT : its own colour on the floor under it
            float power = BattleRules.FigureGlow * alpha;
            float strike = MathHelper.Clamp(pose.Light, 0f, 1f);                          // the blow's light, strongest on the hit
            float tier = 1f + 0.25f * Math.Min(Cues.Stars0, RehearsalList.Most);
            float blade = power * (0.4f + strike * 0.7f) * tier;
            Vector2 grip = body[HandA].At;
            Vector2 d = Way(weapon.At);
            Vector2 head = body[Head].At;

            switch (legend.Weapon)
            {
                case Rapier:
                    Glow(sb, T(112, 99), 26f, Tint, power * 1.4f);                                 // the blow hole
                    for (int i = 0; i < 6; i++)
                    {
                        float lit = Cues.LessonOut && i == 1 ? 0f : KeyLight(i);
                        if (lit > 0.05f) Glow(sb, T(112, 113 + i * 14), 9f, Tint, power * 0.6f * lit);
                    }
                    for (int i = 1; i <= 4; i++) Glow(sb, grip + d * (i * 22f * k), 12f, Tint, blade * 0.8f);
                    Glow(sb, Hd(112, 60), 34f, Tint, power * 0.75f);
                    break;

                case Hammers:
                    float pulse = Pulse();
                    Glow(sb, T(112, 112), 44f, Tint, power * (0.8f + 0.9f * pulse));                // the skin, beating
                    Glow(sb, Sk(new Vector2(112, 262)), 70f, Tint, power * 0.25f * pulse);
                    Glow(sb, grip + d * (52f * k), 20f, Tint, blade);
                    Glow(sb, body[HandB].At + Way(weapon2.At) * (42f * k), 16f, Tint, blade * 0.7f);
                    if (Legendary(Cues.Stars1)) Glow(sb, T(82, 120) + TW(2.72f) * (30f * k), 24f, Tint, power * 0.8f);
                    Glow(sb, Hd(112, 78), 34f, Tint, power * 0.7f);
                    break;

                case Shield:
                    float shine = 0.55f + 0.45f * Math.Min(Cues.Volume, branchAt.Length) / (float)branchAt.Length;
                    Color seam = Legendary(Cues.Stars1) ? SeamGold : Tint;
                    for (int i = 0; i < scar.Length; i += 2) Glow(sb, T(scar[i].X, scar[i].Y), 24f, seam, power * shine * 1.5f);
                    for (int i = 1; i <= 3; i++) Glow(sb, grip + d * (i * 22f * k), 11f, Legendary(Cues.Stars0) ? SeamGold : Tint, blade * 0.8f);
                    if (Legendary(Cues.Stars2)) Glow(sb, body[FootA].At, 34f, Tint, power * 0.7f);
                    Glow(sb, Hd(112, 44), 30f, Tint, power * 0.7f);
                    break;

                case Chakram:
                    Vector2 bell = Hd(100, 64);
                    Glow(sb, bell, 48f, Tint, power * 1.1f);
                    Glow(sb, bell, 14f, Hot, power * 0.9f);
                    //THE HUNTING CALL : the light shines back at whoever plays next, in their colour, dim when nobody does
                    Color call = Cues.Calling ? Cues.CallTint : Tint;
                    float callPower = Cues.Calling ? 0.75f : 0.18f;
                    Vector2 back = TW(MathHelper.Pi + 0.12f);
                    for (int i = 1; i <= 5; i++)
                        Glow(sb, bell + back * (i * 20f * k), 10f + i * 7f, Color.Lerp(Tint, call, i / 5f), power * callPower * (1f - i * 0.12f));
                    Glow(sb, ChakramAt(grip), 26f, Tint, blade);
                    Glow(sb, Hd(124, 64), 22f, Tint, power * 0.6f);
                    break;

                default:
                    Glow(sb, T(112, 131), 30f, Tint, power * (1.5f - 1.1f * bleed));                // the f hole
                    float life = LifeGlow();                                                        // the LIFE STRING
                    if (chainCount > 0 && stringLeft > 0.05f)
                    {
                        int n = (int)(chains[0].Count * stringLeft);
                        for (int i = 1; i < n; i += 2) Glow(sb, chains[0].At[i], 9f, Tint, power * 0.55f * life);
                    }
                    for (int i = 1; i <= 5; i++) Glow(sb, grip + d * (i * 17f * k), 12f, Tint, blade * 0.85f);
                    Glow(sb, Hd(112, 60), 36f, Tint, power * 0.8f);
                    break;
            }
            Glow(sb, head, 12f, Color.White, power * 0.5f);

            if (Cues.Taught)                                                                         // THE LESSON
                Glow(sb, head + new Vector2(0f, -30f * k), 18f, Palette.SpotWind, power * 0.7f);
            if (Cues.Covered)                                                                        // SHIELD
                Glow(sb, Sk(new Vector2(182, 150)), 70f, Palette.SpotWater, power * 0.35f);
        }

        //Glow : one soft light, its size in the sketch's units
        private void Glow(SpriteBatch sb, Vector2 at, float radius, Color color, float power)
        {
            if (power <= 0.003f) return;
            Gfx.DrawGlow(sb, at.X, at.Y, radius * k, color * power);
        }
    }
}
