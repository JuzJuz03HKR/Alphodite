using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //Figure Band (Beta1 1 Nov, MATH ART, SYMBOLS round 5) : the five legends. The user (13.86) : simple but symbolic, symbolic
    //but see the origins, some things many, some things few (the Worldless way : a small core of light and a few shapes that
    //say what the creature is). So each legend is :
    //   CORE     a ball of light for a head, ringed in the legend's own colour
    //   BODY     the legend's own instrument, standing where a body would be (the music in the characters, 13.82) :
    //            PAGANINI a violin, its scroll curling round the head, four strings, two f holes / QUANTZ a flute, its keys and
    //            lip plate / PFUNDT a kettle drum, its rim and tuning lugs / DUPORT a tall body standing on the cello's spike /
    //            PUNTO a horn's bell flaring down from its coil
    //   FEET     two drops, HANDS a dot each, and the WEAPON the instrument became (the shape read first, 30.8)
    //   BITS     three small shapes of the instrument's family floating round it
    //   LEVEL 1  nothing joins them, a thin line from the core to the weapon hand hints the arm (as Worldless does)
    //   LEVEL 2  thin curved arms and legs join the same parts into a figure
    //Colours : white (the band is the light against TACET) and the legend's own colour (their SPOT colour for now).
    public partial class Figure
    {
        //Body : the parts every design moves (TACET's designs give them their own meaning)
        private const int Head = 0;
        private const int Chest = 1;          // the band : the middle of the shoulders, the top of the body triangle
        private const int Hip = 2;            //   its point
        private const int HandA = 3;          // the weapon hand
        private const int HandB = 4;          // the other hand
        private const int FootA = 5;
        private const int FootB = 6;
        private const int Extra = 7;          // a design's own loose part

        private readonly Point[] body = new Point[8];
        private readonly Vector2[] want = new Vector2[8];
        private readonly float[] bodyLoose = new float[8];       // 0 held firm, 1 floating loose and bouncy
        private readonly Point[] bits = new Point[3];            // BITS : small shapes floating round the figure (the constructivist way, 13.83)
        private readonly Vector2[] bitWant = new Vector2[3];
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

        //Legend : how one of the five looks
        private struct Legend
        {
            public int Weapon;                // 0 IL CANNONE, 1 FLUTE RAPIER, 2 TWIN HAMMERS, 3 THE SCARRED SHIELD, 4 HORN CHAKRAM
            public float Core;                // the head's radius, shares of the height
            public float Size;                // how tall against the others (the heavy ones a little taller, 13.85)
            public float Limb;                // how thick the arms and legs of LEVEL 2 are
        }
        private Legend legend;

        private const int Cannone = 0;
        private const int Rapier = 1;
        private const int Hammers = 2;
        private const int Shield = 3;
        private const int Chakram = 4;

        //Legend Of : the design of each of the five (by the key of their name, ArtBank.Key)
        private static Legend LegendOf(string key)
        {
            switch (key)
            {
                case "quantz": return new Legend { Weapon = Rapier, Core = 0.048f, Size = 1f, Limb = 0.016f };
                case "pfundt": return new Legend { Weapon = Hammers, Core = 0.052f, Size = 1.07f, Limb = 0.026f };
                case "duport": return new Legend { Weapon = Shield, Core = 0.05f, Size = 1.05f, Limb = 0.024f };
                case "punto": return new Legend { Weapon = Chakram, Core = 0.048f, Size = 1f, Limb = 0.017f };
                default: return new Legend { Weapon = Cannone, Core = 0.048f, Size = 0.99f, Limb = 0.016f };   // paganini
            }
        }

        //H : the height a design is drawn at (a legend's Size, so the heavy ones stand taller)
        private float H
        {
            get { return Height * (Of == Sort.Musician ? legend.Size : 1f); }
        }

        //Set Up Body : how loose each part is (the band has no chains any more : no hair, no clothes, 13.80)
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
        }

        private void AddChain(int count, float link, float weight, int part, Vector2 spot, float width)
        {
            if (chainCount >= MaxChains) return;
            chains[chainCount] = new Chain(count, link * Height);
            chainLink[chainCount] = link;
            chainWeight[chainCount] = weight;
            chainPart[chainCount] = part;
            chainSpot[chainCount] = spot;
            chainWidth[chainCount] = width;
            chainCount++;
        }

        //Chain Root : where a chain hangs from this frame (its body part, moved by the spot)
        private Vector2 ChainRoot(int c)
        {
            Vector2 spot = chainSpot[c];
            return body[chainPart[c]].At + new Vector2(spot.X * Facing, spot.Y) * Height;
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

        //Pose Band : standing, breathing, every floating part bobbing on its own beat, the weapon held ready.
        //ATTACK swings it (Strike), PARRY raises it, KO lets everything fall. Both levels use the same parts and the same
        //poses, so F4 swaps between them smoothly.
        private void PoseBand()
        {
            float breath = (float)Math.Sin(clock * 1.9f + seed) * 0.005f;
            float rest = RestAngle();
            float angle = rest;
            float angle2 = RestAngle2();
            float lean = 0f;
            float duck = 0f;
            bool floating = Level == 1;
            Vector2 handA = new Vector2(0.19f, -0.56f + breath);
            Vector2 handB = new Vector2(-0.16f, -0.55f + breath);
            if (legend.Weapon == Shield) handB = new Vector2(0.15f, -0.53f + breath);
            if (legend.Weapon == Hammers) handB = new Vector2(0.07f, -0.5f + breath);

            if (move == "attack")
            {
                float s = Strike();
                float from, to;
                SwingAngles(out from, out to);
                angle = s < 0f ? MathHelper.Lerp(rest, from, -s) : MathHelper.Lerp(rest, to, s);
                Vector2 shoulder = new Vector2(0.08f, -0.7f);
                handA = shoulder + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 0.24f;
                lean = 0.04f * s;
                if (legend.Weapon == Hammers)
                {
                    float late = MathHelper.Clamp((moveTime - 0.07f) / BattleRules.TurnStrikeTime, 0f, 1f);
                    float s2 = Strike2(late);
                    angle2 = s2 < 0f ? MathHelper.Lerp(angle2, from, -s2) : MathHelper.Lerp(angle2, to, s2);
                    handB = new Vector2(-0.06f, -0.7f) + new Vector2((float)Math.Cos(angle2), (float)Math.Sin(angle2)) * 0.23f;
                }
                if (legend.Weapon == Shield) handB = new Vector2(0.15f + Math.Max(0f, s) * 0.14f, -0.53f - Math.Max(0f, s) * 0.03f);
            }
            else if (move == "parry")
            {
                float t = MathHelper.Clamp(moveTime / 0.08f, 0f, 1f);
                angle = MathHelper.Lerp(rest, -1.35f, t);
                angle2 = MathHelper.Lerp(angle2, -1.2f, t);
                handA = new Vector2(0.22f, -0.66f);
                if (legend.Weapon == Shield) handB = new Vector2(0.22f, -0.6f);
                else if (legend.Weapon == Hammers) handB = new Vector2(0.15f, -0.62f);
                lean = -0.015f;
            }
            else if (move == "hurt") lean = -0.035f * (1f - moveTime / BattleRules.StageShiverTime);
            else if (move == "enter") duck = -0.02f * (float)Math.Sin(MathHelper.Clamp(moveTime / BattleRules.StageSwapTime, 0f, 1f) * MathHelper.Pi);

            //FLOAT : at LEVEL 1 the parts drift each on its own slow beat, at LEVEL 2 they hold together
            float drift = floating ? 1f : 0.3f;
            float bobA = (float)Math.Sin(clock * 1.3f + seed + 1f) * 0.008f * drift;
            float bobB = (float)Math.Sin(clock * 1.1f + seed + 2.3f) * 0.008f * drift;
            float bobF = (float)Math.Sin(clock * 1.5f + seed + 3.1f) * 0.006f * drift;
            float footY = floating ? -0.25f : -0.01f;
            float tall = 0.28f;

            if (down)
            {
                //KO : the parts drop to the floor one after another and lie there (the springs pull them down loose)
                float t = Ease(moveTime / BattleRules.StageFallTime);
                want[Head] = Local(MathHelper.Lerp(0.02f, 0.09f, t), MathHelper.Lerp(-0.8f, -0.24f, t));
                want[Chest] = Local(MathHelper.Lerp(0f, 0.03f, t), MathHelper.Lerp(-0.72f, -0.2f, t));
                want[Hip] = Local(MathHelper.Lerp(-0.01f, -0.14f, t), MathHelper.Lerp(-0.72f + tall, -0.12f, t));
                want[HandA] = Local(MathHelper.Lerp(handA.X, 0.22f, t), MathHelper.Lerp(handA.Y, -0.1f, t));
                want[HandB] = Local(MathHelper.Lerp(handB.X, -0.2f, t), MathHelper.Lerp(handB.Y, -0.08f, t));
                want[FootA] = Local(-0.08f, MathHelper.Lerp(footY, -0.03f, t));
                want[FootB] = Local(0.06f, MathHelper.Lerp(footY, -0.03f, t));
                want[Extra] = want[Head];
                for (int b = 0; b < bits.Length; b++) bitWant[b] = Local(-0.12f + b * 0.13f, -0.02f);      // they fall too
                weaponWant = MathHelper.Lerp(rest, 1.45f, t);
                weapon2Want = MathHelper.Lerp(angle2, 1.5f, t);
                return;
            }

            want[Head] = Local(0.036f + lean * 1.4f, -0.845f + breath * 1.3f + duck);              // STANCE : leaning toward TACET
            want[Chest] = Local(0.012f + lean, -0.75f + breath + duck);
            want[Hip] = Local(-0.012f + lean * 0.3f, -0.75f + tall + breath * 0.6f + duck * 0.6f);
            want[HandA] = Local(handA.X + lean, handA.Y + duck + bobA);
            want[HandB] = Local(handB.X + lean * 0.5f, handB.Y + duck + bobB);
            want[FootA] = Local(-0.09f, footY + bobF);                                            // the back foot takes the weight
            want[FootB] = Local(0.085f, footY - 0.01f - bobF);
            float orbit = clock * 1.2f + seed;
            want[Extra] = want[Head] + new Vector2((float)Math.Cos(orbit) * Facing, (float)Math.Sin(orbit) * 0.35f) * (0.11f * Height);
            Vector2 middle = (want[Chest] + want[Hip]) * 0.5f;
            for (int b = 0; b < bits.Length; b++)
            {
                float a = clock * (0.45f + 0.12f * b) + b * 2.1f + seed;
                float reach = (0.2f + 0.045f * b) * Height * (floating ? 1f : 0.85f);
                bitWant[b] = middle + new Vector2((float)Math.Cos(a) * reach * Facing, (float)Math.Sin(a) * reach * 0.55f);
            }
            weaponWant = angle;
            weapon2Want = angle2;
        }

        //Strike 2 : the second hammer's swing, a little after the first (the same beats as Strike)
        private float Strike2(float p)
        {
            float saved = moveTime;
            moveTime = p * BattleRules.TurnStrikeTime;
            float s = Strike();
            moveTime = saved;
            return s;
        }

        //Rest Angle : how each weapon is held while waiting (0 forward, minus up)
        private float RestAngle()
        {
            switch (legend.Weapon)
            {
                case Rapier: return -0.32f;
                case Hammers: return -1.2f;
                case Shield: return -0.55f;
                case Chakram: return -0.2f;
                default: return -1.0f;
            }
        }

        private float RestAngle2()
        {
            return legend.Weapon == Hammers ? -0.45f : -0.6f;
        }

        //======================================================================================
        //Draw : the band
        //======================================================================================

        private static readonly Vector2[] line = new Vector2[Shapes.MaxPoints];
        private static readonly float[] lineW = new float[Shapes.MaxPoints];

        //LIT : the stage light on the musician in front doubles what it shows (PostChain LIGHT x 2), so the colours are laid
        //down darker (BattleRules.FigureBandShade) and come out bright white and the legend's own colour, not blown out
        private static readonly Color BodyTop = new Color(246, 246, 250);
        private static readonly Color BodyLow = new Color(176, 178, 194);

        //SILHOUETTE : drawn in black only, into the GLOW picture, so the musician hides the light of TACET's seam behind it
        //(BattleScreen.Stage.cs). Every colour of the band goes through C, which knows this.
        private bool silhouette;

        //C : a colour of the band, darkened for the stage light and faded by alpha (black when drawn as a SILHOUETTE).
        //Only red, green and blue are darkened : the part stays solid (Color * a number would also make it see-through).
        private Color C(Color c, float alpha)
        {
            if (silhouette) return Color.Black * (alpha * c.A / 255f);
            float k = BattleRules.FigureBandShade;
            return new Color((byte)(c.R * k), (byte)(c.G * k), (byte)(c.B * k), c.A) * alpha;
        }

        //Draw Silhouette : the band's shape in black (see SILHOUETTE)
        public void DrawSilhouette(float alpha)
        {
            if (Of != Sort.Musician || alpha <= 0.005f) return;
            silhouette = true;
            DrawBand(alpha, 0f);
            silhouette = false;
        }

        private Vector2 windNow;                                  // the wind of this frame (Update keeps it)

        private void DrawBand(float alpha, float flash)
        {
            float h = H;
            bool floating = Level == 1;
            Color white = C(Color.Lerp(BodyTop, Color.White, flash), alpha);
            Color low = C(Color.Lerp(BodyLow, Color.White, flash), alpha);
            Color edge = C(Color.Lerp(Tint, Hot, flash), alpha);
            Vector2 head = body[Head].At;
            Vector2 chest = body[Chest].At;
            Vector2 hip = body[Hip].At;
            Vector2 down1 = hip - chest;
            float tall = Math.Max(0.0001f, down1.Length());
            down1 /= tall;
            Vector2 across = new Vector2(-down1.Y, down1.X) * -Facing;              // toward TACET when standing

            DrawBits(white, edge);
            if (!floating) DrawLimbs(chest, down1, across, white, low);

            //Feet : two drops pointing at the floor
            DrawDrop(body[FootA].At, 0.062f * h, 0.03f * h, low);
            DrawDrop(body[FootB].At, 0.056f * h, 0.027f * h, low);

            //Body : the instrument
            DrawInstrument(head, chest, down1, across, white, low, edge, alpha);

            //LEVEL 1 : a thin line from under the core to the weapon hand, the arm only hinted
            if (floating)
            {
                Vector2 from = head + new Vector2(0f, legend.Core * 1.1f * h);
                Shapes.Bend(from, (from + body[HandA].At) * 0.5f + new Vector2(-0.04f * Facing, 0.04f) * h, body[HandA].At, 12, line);
                for (int i = 0; i < 12; i++) lineW[i] = MathHelper.Lerp(0.006f, 0.0035f, i / 11f) * h;
                Shapes.Stroke(line, lineW, 12, white * 0.8f, white * 0.8f, false);
            }

            //Weapon, hands
            DrawWeapons(alpha, flash);
            Shapes.Circle(body[HandA].At, 0.017f * h, white);
            if (!floating || legend.Weapon == Hammers || legend.Weapon == Shield) Shapes.Circle(body[HandB].At, 0.015f * h, white);
            if (legend.Weapon == Shield) DrawShield(body[HandB].At + new Vector2(0.03f * Facing, 0.02f) * h, weapon2.At * 0.25f, alpha, flash);

            //Core : the head
            DrawCore(head, edge, alpha, flash);
        }

        //Draw Instrument : the body is the legend's instrument (13.82, 13.86 : the origin can be seen). Laid on its EDGE LIGHT
        //first (the same shape in the legend's colour, moved a little toward TACET), so a thin line of colour runs down one side.
        private void DrawInstrument(Vector2 head, Vector2 chest, Vector2 down1, Vector2 across, Color white, Color low, Color edge, float alpha)
        {
            float h = H;
            Vector2 rim = across * (0.007f * h);
            float rot = (float)Math.Atan2(across.Y, across.X);                      // an ellipse's width runs across the body
            Color cut = silhouette ? Color.Black * alpha : new Color(26, 26, 32) * alpha;
            switch (legend.Weapon)
            {
                case Rapier:                                                        // QUANTZ : a flute, its keys and its lip plate
                    {
                        Vector2 top = chest + down1 * (0.02f * h);
                        Vector2 end = chest + down1 * (0.42f * h);
                        Shapes.Taper(top + rim, end + rim, 0.048f * h, 0.048f * h, edge);
                        Shapes.Taper(top, end, 0.046f * h, 0.046f * h, white);
                        Shapes.Circle(top, 0.023f * h, white);
                        Shapes.Circle(end, 0.023f * h, low);
                        Shapes.Ellipse(top + down1 * (0.04f * h), 0.016f * h, 0.01f * h, rot, edge, edge);   // the lip plate
                        for (int k = 0; k < 5; k++) Shapes.Circle(top + down1 * ((0.12f + k * 0.055f) * h), 0.011f * h, edge);
                    }
                    break;

                case Hammers:                                                       // PFUNDT : a kettle drum, its rim and lugs
                    {
                        Vector2 middle = chest + down1 * (0.15f * h);
                        float r = 0.13f * h;
                        Shapes.Circle(middle + rim, r, edge);
                        Shapes.Ellipse(middle, r, r, 0f, white, low);
                        Shapes.Ring(middle, r * 0.93f, 0.012f * h, edge);
                        Shapes.Ring(middle, r * 0.5f, 0.0035f * h, low);
                        for (int k = 0; k < 6; k++)
                        {
                            float a = MathHelper.TwoPi * k / 6f + 0.26f;
                            Vector2 out1 = new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
                            Shapes.Box(middle + out1 * (r * 1.06f), 0.03f * h, 0.014f * h, a, edge);
                        }
                    }
                    break;

                case Shield:                                                        // DUPORT : a tall body standing on the cello's spike
                    {
                        Vector2 top = chest + down1 * (0.01f * h);
                        line[0] = top - across * (0.06f * h);
                        line[1] = top + across * (0.055f * h);
                        line[2] = chest + down1 * (0.25f * h) + across * (0.03f * h);
                        line[3] = chest + down1 * (0.48f * h);
                        line[4] = chest + down1 * (0.25f * h) - across * (0.035f * h);
                        FillLit(line, 5, white, low, top.Y, line[3].Y, edge, rim);
                        Shapes.Kite(chest + down1 * (0.4f * h), chest + down1 * (0.52f * h), 0.016f * h, 0.2f, edge);   // the spike's point
                    }
                    break;

                case Chakram:                                                       // PUNTO : a horn's bell flaring down from its coil
                    {
                        int n = 9;
                        for (int i = 0; i < n; i++)
                        {
                            float t = (float)i / (n - 1);
                            float half = (0.022f + 0.11f * (float)Math.Pow(t, 2.4)) * h;
                            Vector2 at = chest + down1 * ((0.06f + 0.34f * t) * h);
                            line[i] = at - across * half;
                            line[2 * n - 1 - i] = at + across * half;
                        }
                        FillLit(line, 2 * n, white, low, chest.Y, chest.Y + 0.4f * h, edge, rim);
                        Shapes.Ellipse(chest + down1 * (0.4f * h), 0.132f * h, 0.018f * h, rot, edge, edge);     // the bell's mouth
                        Vector2 coil = chest + down1 * (0.05f * h);
                        Shapes.Ring(coil, 0.05f * h, 0.011f * h, edge);
                        Shapes.Arc(coil, 0.028f * h, clock * 0.8f, clock * 0.8f + 4.2f, 0.007f * h, 0.003f * h, edge);
                    }
                    break;

                default:                                                            // PAGANINI : a violin
                    {
                        Vector2 upper = chest + down1 * (0.08f * h);
                        Vector2 lower = chest + down1 * (0.2f * h);
                        Shapes.Taper(head + down1 * (legend.Core * h), upper, 0.016f * h, 0.022f * h, white);   // the neck up to the scroll
                        Shapes.Ellipse(upper + rim, 0.062f * h, 0.056f * h, rot, edge, edge);
                        Shapes.Ellipse(lower + rim, 0.082f * h, 0.074f * h, rot, edge, edge);
                        Shapes.Ellipse(upper, 0.06f * h, 0.055f * h, rot, white, white);
                        Shapes.Ellipse(lower, 0.08f * h, 0.072f * h, rot, white, low);
                        for (int k = 0; k < 4; k++)                                 // four strings
                        {
                            float x = (k - 1.5f) * 0.008f * h;
                            Shapes.Line(head + down1 * (legend.Core * 1.2f * h) + across * x, lower + down1 * (0.04f * h) + across * x, 0.0022f * h, edge);
                        }
                        Shapes.Kite(lower + down1 * (0.03f * h), lower + down1 * (0.075f * h), 0.03f * h, 0.3f, edge);   // the tailpiece
                        for (int side = -1; side <= 1; side += 2)                   // two f holes
                        {
                            for (int k = 0; k < 6; k++)
                            {
                                float t = k / 5f;
                                line[k] = upper + down1 * ((0.035f + 0.1f * t) * h) + across * ((side * 0.042f + (float)Math.Sin(t * MathHelper.TwoPi) * 0.008f * side) * h);
                                lineW[k] = (0.003f + 0.004f * (float)Math.Sin(t * MathHelper.Pi)) * h;
                            }
                            Shapes.Stroke(line, lineW, 6, cut, cut, false);
                            Shapes.Circle(line[0], 0.005f * h, cut);
                            Shapes.Circle(line[5], 0.005f * h, cut);
                        }
                    }
                    break;
            }
        }

        //Draw Core : the head, a ball of light ringed in the legend's colour. PAGANINI's sits in the violin's scroll.
        private void DrawCore(Vector2 head, Color edge, float alpha, float flash)
        {
            float h = H;
            float r = legend.Core * h;
            if (legend.Weapon == Cannone)                                           // the scroll : a spiral round the head
            {
                int n = 24;
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / (n - 1);
                    float a = -MathHelper.PiOver2 * Facing + t * 5.2f * Facing;
                    float rr = MathHelper.Lerp(1.55f, 1.05f, t) * r;
                    line[i] = head + new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * rr;
                    lineW[i] = MathHelper.Lerp(0.004f, 0.012f, t) * h;
                }
                Shapes.Stroke(line, lineW, n, edge, edge, false);
            }
            Shapes.Circle(head, r * 1.32f, silhouette ? Color.Black * alpha : C(Tint, 0.26f * alpha));
            Shapes.Circle(head, r, edge);
            Shapes.Circle(head, r * 0.74f, C(Color.Lerp(Hot, Color.White, 0.65f + 0.35f * flash), alpha));
        }

        //Draw Limbs (LEVEL 2) : thin curved arms from the body's top to the hands and legs from under it to the feet
        private void DrawLimbs(Vector2 chest, Vector2 down1, Vector2 across, Color white, Color low)
        {
            float h = H;
            float w = legend.Limb * h;
            float shoulder = legend.Weapon == Hammers || legend.Weapon == Shield ? 0.09f : 0.06f;
            Vector2 legRoot = chest + down1 * ((legend.Weapon == Hammers ? 0.27f : 0.3f) * h);
            Limb(legRoot - across * (0.03f * h), body[FootA].At, across * (-0.03f * h), w * 1.3f, 0.005f * h, low);
            Limb(legRoot + across * (0.03f * h), body[FootB].At, across * (0.04f * h), w * 1.2f, 0.005f * h, low);
            Limb(chest + across * (shoulder * h), body[HandA].At, down1 * (0.05f * h), w, 0.006f * h, white);
            Limb(chest - across * (shoulder * 1.1f * h), body[HandB].At, down1 * (0.05f * h) - across * (0.03f * h), w, 0.006f * h, white);
        }

        //Limb : one curved tapered stroke, bowed by bend at its middle
        private void Limb(Vector2 from, Vector2 to, Vector2 bend, float w0, float w1, Color color)
        {
            Shapes.Bend(from, (from + to) * 0.5f + bend, to, 10, line);
            for (int i = 0; i < 10; i++) lineW[i] = MathHelper.Lerp(w0, w1, i / 9f);
            Shapes.Stroke(line, lineW, 10, color, color, false);
        }

        //Drop : a foot, round on top and sharp at the floor
        private void DrawDrop(Vector2 tip, float length, float width, Color color)
        {
            Vector2 top = tip - new Vector2(0f, length);
            Shapes.Circle(top + new Vector2(0f, width * 0.45f), width * 0.5f, color);
            Shapes.Kite(top + new Vector2(0f, width * 0.2f), tip, width, 0.15f, color);
        }

        //Draw Bits : the three small shapes floating round the figure, of the instrument's family
        private void DrawBits(Color white, Color edge)
        {
            float h = H;
            for (int b = 0; b < bits.Length; b++)
            {
                Vector2 at = bits[b].At;
                Color c = b == 1 ? edge : white * 0.85f;
                float turn = clock * (0.6f + 0.3f * b) + b;
                switch (legend.Weapon)
                {
                    case Rapier: Shapes.Circle(at, 0.009f * h, c); break;                                        // puffs of air
                    case Hammers: Shapes.Ring(at, 0.015f * h, 0.0045f * h, c); break;                            // rings of a beat
                    case Shield: Shapes.Polygon(at, 0.014f * h, 4, turn, c); break;                              // splinters
                    case Chakram: Shapes.Polygon(at, 0.015f * h, 3, turn, c); break;                             // sparks of the bell
                    default:                                                                                     // strings
                        Vector2 d = new Vector2((float)Math.Cos(turn), (float)Math.Sin(turn)) * (0.028f * h);
                        Shapes.Line(at - d, at + d, 0.005f * h, c);
                        break;
                }
            }
        }

        //Fill Lit : a filled part with its edge light under it
        private static void FillLit(Vector2[] p, int n, Color top, Color low, float fromY, float toY, Color edge, Vector2 rim)
        {
            for (int i = 0; i < n; i++) p[i] += rim;
            Shapes.Fill(p, n, edge);
            for (int i = 0; i < n; i++) p[i] -= rim;
            Shapes.FillShaded(p, n, top, low, fromY, toY);
        }

        //======================================================================================
        //Weapons
        //======================================================================================

        //Draw Weapons : the legend's weapon at its hand (both hands for the hammers, the shield's bow in the weapon hand)
        private void DrawWeapons(float alpha, float flash)
        {
            Vector2 grip = body[HandA].At;
            float angle = weapon.At;
            switch (legend.Weapon)
            {
                case Rapier: DrawRapier(grip, angle, alpha, flash); break;
                case Hammers:
                    DrawHammer(body[HandB].At, weapon2.At, alpha * 0.85f, flash);
                    DrawHammer(grip, angle, alpha, flash);
                    break;
                case Shield: DrawBow(grip, angle, alpha, flash); break;
                case Chakram: DrawChakram(grip, alpha, flash); break;
                default: DrawCannone(grip, angle, alpha, flash); break;
            }
        }

        private Color WeaponColor(float alpha, float flash)
        {
            return C(Color.Lerp(Tint, Hot, 0.25f + 0.5f * flash), alpha);
        }

        private Color ShineColor(float alpha)
        {
            return C(Color.Lerp(Hot, Color.White, 0.55f), alpha);
        }

        //IL CANNONE : the violin as a sword. Its body (two bouts) is the guard at the hand, a round pommel, the neck runs on
        //into a long blade with a bright line down its middle like a string
        private void DrawCannone(Vector2 grip, float angle, float alpha, float flash)
        {
            float h = H;
            Vector2 d = Way(angle);
            Color metal = WeaponColor(alpha, flash);
            float rot = (float)Math.Atan2(d.Y, d.X);
            Shapes.Kite(grip + d * (0.05f * h), grip + d * (0.48f * h), 0.026f * h, 0.12f, metal);
            Shapes.Line(grip + d * (0.08f * h), grip + d * (0.42f * h), 0.004f * h, ShineColor(alpha));
            Shapes.Ellipse(grip - d * (0.022f * h), 0.03f * h, 0.037f * h, rot, metal, metal);              // lower bout
            Shapes.Ellipse(grip + d * (0.03f * h), 0.024f * h, 0.03f * h, rot, metal, metal);               // upper bout
            Shapes.Circle(grip - d * (0.07f * h), 0.014f * h, metal);                                       // the scroll, a pommel
        }

        //FLUTE RAPIER : a straight tube with its keys, a cup over the hand, a point
        private void DrawRapier(Vector2 grip, float angle, float alpha, float flash)
        {
            float h = H;
            Vector2 d = Way(angle);
            Color metal = WeaponColor(alpha, flash);
            Color shine = ShineColor(alpha);
            Shapes.Line(grip - d * (0.05f * h), grip + d * (0.44f * h), 0.014f * h, metal);
            Shapes.Kite(grip + d * (0.43f * h), grip + d * (0.55f * h), 0.014f * h, 0.05f, metal);
            for (int k = 0; k < 5; k++) Shapes.Circle(grip + d * ((0.13f + k * 0.055f) * h), 0.006f * h, shine);
            float rot = (float)Math.Atan2(d.Y, d.X);
            Shapes.Arc(grip + d * (0.02f * h), 0.032f * h, rot - 1.9f, rot + 1.9f, 0.008f * h, 0.008f * h, metal);   // the cup
        }

        //TWIN HAMMERS : one timpani stick, a felt ball at its end with a glint
        private void DrawHammer(Vector2 grip, float angle, float alpha, float flash)
        {
            float h = H;
            Vector2 d = Way(angle);
            Color stick = C(Color.Lerp(BodyTop, Color.White, flash), 0.9f * alpha);
            Color felt = WeaponColor(alpha, flash);
            Shapes.Line(grip - d * (0.03f * h), grip + d * (0.19f * h), 0.011f * h, stick);
            Shapes.Circle(grip + d * (0.22f * h), 0.038f * h, felt);
            Shapes.Circle(grip + d * (0.22f * h) + new Vector2(-0.011f, -0.013f) * h, 0.012f * h, ShineColor(alpha));
        }

        //THE SCARRED SHIELD : the cello's body held up as a shield, and the scar across it
        private void DrawShield(Vector2 middle, float tilt, float alpha, float flash)
        {
            float h = H;
            Color face = WeaponColor(alpha, flash);
            Vector2 up = new Vector2((float)Math.Sin(tilt) * Facing, -(float)Math.Cos(tilt));
            Vector2 side = new Vector2(-up.Y, up.X);
            float rot = (float)Math.Atan2(up.Y, up.X);
            Shapes.Ellipse(middle - up * (0.07f * h), 0.1f * h, 0.086f * h, rot, face, face);               // lower bout
            Shapes.Ellipse(middle + up * (0.075f * h), 0.08f * h, 0.068f * h, rot, face, face);             // upper bout
            line[0] = middle + side * (0.075f * h) + up * (0.1f * h);                                        // THE SCAR
            line[1] = middle + side * (0.03f * h) + up * (0.035f * h);
            line[2] = middle - side * (0.005f * h) + up * (0.01f * h);
            line[3] = middle - side * (0.055f * h) - up * (0.06f * h);
            lineW[0] = 0.003f * h; lineW[1] = 0.011f * h; lineW[2] = 0.01f * h; lineW[3] = 0.003f * h;
            Color scar = Color.Black * (silhouette ? alpha : 0.85f * alpha);
            Shapes.Stroke(line, lineW, 4, scar, scar, false);
        }

        //The short bow DUPORT strikes with from behind the shield
        private void DrawBow(Vector2 grip, float angle, float alpha, float flash)
        {
            float h = H;
            Vector2 d = Way(angle);
            Shapes.Kite(grip - d * (0.02f * h), grip + d * (0.31f * h), 0.016f * h, 0.1f, WeaponColor(alpha, flash));
            Shapes.Line(grip + d * (0.02f * h), grip + d * (0.27f * h), 0.003f * h, ShineColor(alpha));
        }

        //HORN CHAKRAM : the horn's coil as a ring with its bell flaring out, spinning. An ATTACK throws it at TACET and it
        //comes back (THROW).
        private void DrawChakram(Vector2 grip, float alpha, float flash)
        {
            float h = H;
            Color metal = WeaponColor(alpha, flash);
            Vector2 middle = ChakramAt(grip);
            float spin = clock * (move == "attack" ? 22f : 2.2f) * Facing;
            Shapes.Ring(middle, 0.085f * h, 0.018f * h, metal);
            Shapes.Arc(middle, 0.05f * h, spin, spin + 4.2f, 0.01f * h, 0.004f * h, metal);
            Vector2 out1 = new Vector2((float)Math.Cos(spin), (float)Math.Sin(spin));
            Vector2 at = middle + out1 * (0.085f * h);
            Vector2 tangent = new Vector2(-out1.Y, out1.X);
            line[0] = at - tangent * (0.006f * h);
            line[1] = at + tangent * (0.055f * h) + out1 * (0.038f * h);
            line[2] = at + tangent * (0.055f * h) - out1 * (0.022f * h);
            Shapes.Fill(line, 3, metal);                                                                       // the bell
            Shapes.Circle(middle, 0.01f * h, ShineColor(alpha));
        }

        //Chakram At : where the ring is. Held beside the hand, thrown out and back during an ATTACK (THROW)
        private Vector2 ChakramAt(Vector2 grip)
        {
            Vector2 held = grip + new Vector2(0.06f * Facing, -0.02f) * H;
            if (move != "attack") return held;
            float s = Math.Max(0f, Strike());
            Vector2 far = Local(0.85f, -0.6f);
            return Vector2.Lerp(held, far, s * s);
        }

        //======================================================================================
        //Glow : the band
        //======================================================================================

        //Glow Band : the core's light, and the weapon's light along its length (small lights, stronger in the cut)
        private void GlowBand(SpriteBatch sb, float alpha)
        {
            float h = H;
            float power = BattleRules.FigureGlow * alpha;
            float strike = move == "attack" ? Math.Max(0f, Strike()) : 0f;
            float glow = power * (0.45f + strike * 0.7f);
            Vector2 grip = body[HandA].At;
            Vector2 d = Way(weapon.At);
            switch (legend.Weapon)
            {
                case Chakram:
                    Vector2 ring = ChakramAt(grip);
                    Gfx.DrawGlow(sb, ring.X, ring.Y, 0.14f * h, Tint * glow);
                    break;
                case Hammers:
                    Vector2 ballA = grip + d * (0.22f * h);
                    Vector2 ballB = body[HandB].At + Way(weapon2.At) * (0.22f * h);
                    Gfx.DrawGlow(sb, ballA.X, ballA.Y, 0.08f * h, Tint * glow);
                    Gfx.DrawGlow(sb, ballB.X, ballB.Y, 0.07f * h, Tint * (glow * 0.8f));
                    break;
                case Shield:
                    Vector2 face = body[HandB].At + new Vector2(0.03f * Facing, 0.02f) * h;
                    Gfx.DrawGlow(sb, face.X, face.Y, 0.12f * h, Tint * (glow * 0.6f));
                    for (int k = 1; k <= 3; k++)
                    {
                        Vector2 p = grip + d * (0.08f * k * h);
                        Gfx.DrawGlow(sb, p.X, p.Y, 0.045f * h, Tint * glow);
                    }
                    break;
                default:
                    float length = legend.Weapon == Rapier ? 0.5f : 0.45f;
                    for (int k = 1; k <= 5; k++)
                    {
                        Vector2 p = grip + d * (length * k / 5f * h);
                        Gfx.DrawGlow(sb, p.X, p.Y, 0.05f * h, Tint * glow);
                    }
                    break;
            }
            Vector2 head = body[Head].At;                                     // the core is a light
            Gfx.DrawGlow(sb, head.X, head.Y, legend.Core * 3f * h, Tint * (power * 0.8f));
            Gfx.DrawGlow(sb, head.X, head.Y, legend.Core * 1.2f * h, Color.White * (power * 0.6f));
        }
    }
}
