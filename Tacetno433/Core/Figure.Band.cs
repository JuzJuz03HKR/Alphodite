using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //Figure Band (Beta1 1 Nov, MATH ART, SYMBOLS) : the five legends drawn as symbols, not as people. The user (13.80) :
    //no hair, no clothes, nothing busy, squares, triangles and circles standing for the character. One shape language :
    //   CORE     a circle for the head, white, ringed in the legend's own colour, it glows
    //   BODY     the shape of the legend's own instrument (the user, 13.82 : the music in the characters) :
    //            PAGANINI a sharp triangle with the violin's two f holes cut in it / QUANTZ a slim tube with the flute's keys
    //            down the front / PFUNDT a round drum head with its rim and the lugs round it / DUPORT a broad block standing on
    //            the cello's spike / PUNTO the horn's bell flaring down from a coil at the chest
    //   BITS     three small shapes of the same family floating round it (strings, puffs of air, rings, blocks, sparks)
    //   HANDS    two small diamonds, one holds the weapon
    //   FEET     two small triangles, point down
    //   WEAPON   the instrument turned weapon, the shape the eye reads first (BETA1_DIRECTION 30.8) :
    //            PAGANINI IL CANNONE (a violin for a guard, its neck run on into a blade) / QUANTZ FLUTE RAPIER (a straight
    //            flute with its keys and a point) / PFUNDT TWIN HAMMERS (two sticks, a felt ball each) / DUPORT THE SCARRED
    //            SHIELD (the cello's body held up, the scar of Napoleon's spur across it) / PUNTO HORN CHAKRAM (the horn's coil
    //            as a throwing ring, its bell flaring out)
    //   LEVEL 1  every part floats on its own spring with a gap round it (the Worldless way) : a hit throws them apart and
    //            they come back
    //   LEVEL 2  the same parts joined into a figure by straight bars (a neck, arms, legs), still no clothes
    //Each body is a little different too (PAGANINI tall and thin, PFUNDT and DUPORT broad), so the five read apart.
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
            public float Shoulder;            // half the shoulders, toward TACET (the near one is a little wider), shares of the height
            public float Core;                // the head's radius
            public Plane[] Coat;              // the planes of the body (STYLE, round 4)
            public float Size;                // how tall against the others (the heavy ones a little taller)
            public float Limb;                // how thick the arms and legs of LEVEL 2 are
        }
        private Legend legend;

        //Plane : one flat piece of a legend's body (STYLE, round 4, the user 13.84 : like ZZZ, CZN, Expedition 33, Worldless).
        //Numbers in pairs (across, down) in shares of the height from the middle of the shoulders : across toward TACET, down
        //along the body. LIT is the whole piece in light, SHADE the part turned from it, laid on top (TWO TONES, like cel
        //shading). The lower a corner is, the more it trails behind when the body moves (HEM SWING).
        private struct Plane
        {
            public float[] Lit;
            public float[] Shade;
            public Plane(float[] lit, float[] shade)
            {
                Lit = lit;
                Shade = shade;
            }
        }

        //PROPORTIONS (the user 13.85 : like real characters, slim and cool, a heavy one big, tall and solid) : the shoulders are
        //wide and the waist narrow (a V), the coat flows down behind, the legs are long. The slim three (PAGANINI QUANTZ PUNTO)
        //are drawn at their size, the heavy two (PFUNDT DUPORT) a little taller with thick shoulders (Legend.Size).

        //PAGANINI : slim and sharp, two points at the shoulders, two long tails like a bow's blades (the devil's violinist)
        private static readonly Plane[] coatPaganini =
        {
            new Plane(new[] { 0f, 0.21f, 0.04f, 0.21f, -0.06f, 0.6f }, new[] { 0f, 0.21f, 0.04f, 0.21f, -0.06f, 0.6f }),
            new Plane(new[] { -0.045f, 0.2f, 0f, 0.22f, -0.165f, 0.66f }, null),
            new Plane(new[] { -0.096f, 0.01f, 0.086f, -0.006f, 0.034f, 0.21f, -0.045f, 0.215f }, new[] { 0.01f, 0.004f, 0.086f, -0.006f, 0.034f, 0.21f, 0f, 0.212f }),
            new Plane(new[] { -0.098f, 0.012f, -0.058f, 0.004f, -0.115f, -0.06f }, null),
            new Plane(new[] { 0.05f, -0.002f, 0.086f, -0.006f, 0.098f, -0.056f }, new[] { 0.05f, -0.002f, 0.086f, -0.006f, 0.098f, -0.056f }),
        };

        //QUANTZ : slim and upright, a high collar, a long narrow coat behind like the flute itself (the king's flute master)
        private static readonly Plane[] coatQuantz =
        {
            new Plane(new[] { -0.048f, 0.2f, 0.034f, 0.2f, 0.03f, 0.6f, -0.046f, 0.6f }, new[] { 0f, 0.2f, 0.034f, 0.2f, 0.03f, 0.6f, 0f, 0.6f }),
            new Plane(new[] { -0.082f, 0.01f, 0.072f, 0f, 0.032f, 0.21f, -0.042f, 0.21f }, new[] { 0.004f, 0.005f, 0.072f, 0f, 0.032f, 0.21f, 0f, 0.21f }),
            new Plane(new[] { -0.058f, 0.014f, 0.05f, 0.012f, -0.02f, -0.075f }, new[] { 0f, 0.013f, 0.05f, 0.012f, -0.02f, -0.075f }),
        };

        //PFUNDT : heavy, broad shoulders and a thick waist, a short skirt, a drum on each shoulder (who tunes the thunder)
        private static readonly Plane[] coatPfundt =
        {
            new Plane(new[] { -0.092f, 0.24f, 0.082f, 0.24f, 0.104f, 0.37f, -0.114f, 0.37f }, new[] { 0f, 0.24f, 0.082f, 0.24f, 0.104f, 0.37f, 0f, 0.37f }),
            new Plane(new[] { -0.15f, 0.02f, 0.14f, 0.008f, 0.078f, 0.25f, -0.088f, 0.25f }, new[] { 0.01f, 0.014f, 0.14f, 0.008f, 0.078f, 0.25f, 0f, 0.25f }),
        };

        //DUPORT : heavy and steady, square shoulders, a long coat, the cello's shape on the back and its spike below
        private static readonly Plane[] coatDuport =
        {
            new Plane(new[] { -0.092f, 0.23f, 0.082f, 0.23f, 0.102f, 0.52f, -0.114f, 0.52f }, new[] { 0f, 0.23f, 0.082f, 0.23f, 0.102f, 0.52f, 0f, 0.52f }),
            new Plane(new[] { -0.14f, 0f, 0.13f, -0.006f, 0.08f, 0.24f, -0.092f, 0.24f }, new[] { 0.008f, -0.003f, 0.13f, -0.006f, 0.08f, 0.24f, 0f, 0.24f }),
            new Plane(new[] { -0.152f, -0.014f, -0.086f, -0.014f, -0.086f, 0.046f, -0.152f, 0.046f }, null),
            new Plane(new[] { 0.078f, -0.02f, 0.14f, -0.02f, 0.14f, 0.04f, 0.078f, 0.04f }, new[] { 0.078f, -0.02f, 0.14f, -0.02f, 0.14f, 0.04f, 0.078f, 0.04f }),
        };

        //PUNTO : slim above, the coat flaring into the horn's bell below the waist (louder than Beethoven)
        private static readonly Plane[] coatPunto =
        {
            new Plane(new[] { -0.045f, 0.2f, -0.056f, 0.29f, -0.08f, 0.37f, -0.118f, 0.44f, -0.17f, 0.5f, 0.15f, 0.5f, 0.106f, 0.44f, 0.072f, 0.37f, 0.05f, 0.29f, 0.038f, 0.2f },
                      new[] { 0f, 0.2f, 0.038f, 0.2f, 0.05f, 0.29f, 0.072f, 0.37f, 0.106f, 0.44f, 0.15f, 0.5f, 0f, 0.5f }),
            new Plane(new[] { -0.086f, 0.004f, 0.076f, -0.004f, 0.036f, 0.2f, -0.045f, 0.2f }, new[] { 0.006f, 0f, 0.076f, -0.004f, 0.036f, 0.2f, 0f, 0.2f }),
        };

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
                case "quantz": return new Legend { Weapon = Rapier, Shoulder = 0.07f, Core = 0.041f, Coat = coatQuantz, Size = 1f, Limb = 0.024f };
                case "pfundt": return new Legend { Weapon = Hammers, Shoulder = 0.14f, Core = 0.043f, Coat = coatPfundt, Size = 1.07f, Limb = 0.042f };
                case "duport": return new Legend { Weapon = Shield, Shoulder = 0.13f, Core = 0.042f, Coat = coatDuport, Size = 1.05f, Limb = 0.038f };
                case "punto": return new Legend { Weapon = Chakram, Shoulder = 0.075f, Core = 0.041f, Coat = coatPunto, Size = 1f, Limb = 0.026f };
                default: return new Legend { Weapon = Cannone, Shoulder = 0.085f, Core = 0.04f, Coat = coatPaganini, Size = 0.99f, Limb = 0.024f };   // paganini
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
            float footY = floating ? -0.13f : -0.01f;
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

        private Vector2 windNow;                                  // the wind of this frame (Update keeps it), for HEM SWING
        private static readonly Color BodyShade = new Color(198, 200, 216);

        //Draw Band (STYLE, round 4) : a tall figure leaning toward TACET, its body a few flat planes in two tones with an edge
        //of the legend's colour, the instrument's mark on the coat, a ring of the legend's colour tilted behind the head.
        //LEVEL 1 : the planes float apart with small gaps (a body of pieces, the parts on springs). LEVEL 2 : joined, with legs
        //and arms as long tapered bars.
        private void DrawBand(float alpha, float flash)
        {
            float h = H;
            bool floating = Level == 1;
            Color white = C(Color.Lerp(BodyTop, Color.White, flash), alpha);
            Color shade = C(Color.Lerp(BodyShade, Color.White, flash * 0.8f), alpha);
            Color low = C(Color.Lerp(BodyLow, Color.White, flash), alpha);
            Color edge = C(Color.Lerp(Tint, Hot, flash), alpha);
            Vector2 head = body[Head].At;
            Vector2 chest = body[Chest].At;
            Vector2 hip = body[Hip].At;
            Vector2 down1 = hip - chest;
            float tall = Math.Max(0.0001f, down1.Length());
            down1 /= tall;
            Vector2 across = new Vector2(-down1.Y, down1.X) * -Facing;              // toward TACET when standing
            across = Vector2.Normalize(across + new Vector2(0f, -0.09f));          // STANCE : the far shoulder a little higher

            //HEM SWING : the lower the corner, the more it trails behind the body's move, and the wind and a breeze move it
            Vector2 trail = (body[Hip].At - want[Hip]) * 1.6f + new Vector2(windNow.X * 0.012f + (float)Math.Sin(clock * 1.1f + seed) * 0.006f * h, 0f);

            DrawBits(white, edge);

            //Legs and the far arm, behind the coat (LEVEL 2 bars, LEVEL 1 small floating feet)
            if (!floating)
            {
                float limb = legend.Limb * h;
                Shapes.Taper(hip + across * (0.035f * h), body[FootB].At, limb * 1.25f, 0.004f * h, low);
                Shapes.Taper(hip - across * (0.04f * h), body[FootA].At, limb * 1.4f, 0.004f * h, shade);
                Shapes.Taper(Shoulder(true, chest, down1, across), body[HandA].At, limb, limb * 0.5f, low);
            }
            else
            {
                DrawFoot(body[FootA].At, 0.04f * h, low);
                DrawFoot(body[FootB].At, 0.036f * h, low);
            }

            //The coat : the edge light first (every plane in the legend's colour, moved toward TACET), then the planes
            Plane[] coat = legend.Coat;
            Vector2 rim = across * (0.007f * h);
            for (int i = 0; i < coat.Length; i++)
            {
                int n = PlanePoints(coat[i].Lit, i, chest, down1, across, trail, rim, floating);
                Shapes.Fill(line, n, edge);
            }
            for (int i = 0; i < coat.Length; i++)
            {
                int n = PlanePoints(coat[i].Lit, i, chest, down1, across, trail, Vector2.Zero, floating);
                Shapes.FillShaded(line, n, white, low, chest.Y - 0.05f * h, chest.Y + 0.6f * h);
                if (coat[i].Shade != null)
                {
                    n = PlanePoints(coat[i].Shade, i, chest, down1, across, trail, Vector2.Zero, floating);
                    Shapes.Fill(line, n, shade);
                }
            }
            DrawCoatMarks(chest, down1, across, trail, white, shade, edge, alpha, floating);

            //LEVEL 2 : the neck and the near arm over the coat
            if (!floating)
            {
                Shapes.Taper(chest, head + new Vector2(0f, legend.Core * 0.8f * h), 0.024f * h, 0.018f * h, shade);
                Shapes.Taper(Shoulder(false, chest, down1, across), body[HandB].At, legend.Limb * 1.08f * h, legend.Limb * 0.55f * h, white);
            }

            //Weapon and hands
            DrawWeapons(alpha, flash);
            DrawHand(body[HandA].At, 0.024f * h, white);
            if (!floating || legend.Weapon == Hammers || legend.Weapon == Shield) DrawHand(body[HandB].At, 0.02f * h, white);
            if (legend.Weapon == Shield) DrawShield(body[HandB].At + new Vector2(0.03f * Facing, 0.02f) * h, weapon2.At * 0.25f, alpha, flash);

            //Head : the ring of the legend's colour tilted behind it (PUNTO's is the horn's coil), then the head in two tones
            DrawHalo(head, edge);
            float r = legend.Core * h;
            Shapes.Circle(head, r, white);
            int k = 0;
            for (int i = 0; i <= 10; i++)                                           // the half turned from the light
            {
                float a = -MathHelper.PiOver2 + MathHelper.Pi * i / 10f;
                line[k++] = head + (across * (float)Math.Cos(a) + new Vector2(0f, (float)Math.Sin(a))) * r;
            }
            line[k++] = head + new Vector2(0f, r * 0.9f);
            Shapes.Fill(line, k, shade);
        }

        //Plane Points : one plane's corners on the page into line (its count back). LEVEL 1 moves each plane a little away from
        //the middle of the body and lets it bob on its own (a body of floating pieces).
        private int PlanePoints(float[] pts, int index, Vector2 chest, Vector2 down1, Vector2 across, Vector2 trail, Vector2 shift, bool floating)
        {
            float h = H;
            int n = pts.Length / 2;
            Vector2 apart = Vector2.Zero;
            if (floating)
            {
                float ca = 0f, cd = 0f;
                for (int i = 0; i < n; i++)
                {
                    ca += pts[i * 2];
                    cd += pts[i * 2 + 1];
                }
                Vector2 out1 = new Vector2(ca / n, cd / n - 0.22f);
                if (out1.LengthSquared() > 0.00001f) out1.Normalize();
                apart = (across * out1.X + down1 * out1.Y) * (0.016f * h) + new Vector2(0f, (float)Math.Sin(clock * 1.3f + index * 1.7f + seed) * 0.006f * h);
            }
            for (int i = 0; i < n; i++)
            {
                float a = pts[i * 2];
                float d = pts[i * 2 + 1];
                float swing = MathHelper.Clamp((d - 0.15f) / 0.45f, 0f, 1f);
                line[i] = chest + across * (a * h) + down1 * (d * h) + trail * (swing * swing) + apart + shift;
            }
            return n;
        }

        //Coat Point : one point on the coat, the same way PlanePoints places corners (for the marks)
        private Vector2 CoatPoint(float a, float d, Vector2 chest, Vector2 down1, Vector2 across, Vector2 trail)
        {
            float swing = MathHelper.Clamp((d - 0.15f) / 0.45f, 0f, 1f);
            return chest + across * (a * H) + down1 * (d * H) + trail * (swing * swing);
        }

        //Shoulder : where an arm leaves the body (the far one toward TACET)
        private Vector2 Shoulder(bool far, Vector2 chest, Vector2 down1, Vector2 across)
        {
            float half = far ? legend.Shoulder : -legend.Shoulder * 1.1f;
            return chest + across * (half * H) + down1 * (0.02f * H);
        }

        //Coat Marks : the instrument on the coat (13.82, the music in the characters)
        private void DrawCoatMarks(Vector2 chest, Vector2 down1, Vector2 across, Vector2 trail, Color white, Color shade, Color edge, float alpha, bool floating)
        {
            float h = H;
            Color cut = silhouette ? Color.Black * alpha : new Color(30, 30, 36) * alpha;
            switch (legend.Weapon)
            {
                case Rapier:                                                        // QUANTZ : the flute's keys down the back, a slit below
                    for (int k = 0; k < 6; k++) Shapes.Circle(CoatPoint(-0.004f, 0.07f + k * 0.07f, chest, down1, across, trail), 0.011f * h, edge);
                    line[0] = CoatPoint(-0.007f, 0.56f, chest, down1, across, trail);
                    line[1] = CoatPoint(0.007f, 0.56f, chest, down1, across, trail);
                    line[2] = CoatPoint(0f, 0.4f, chest, down1, across, trail);
                    Shapes.Fill(line, 3, cut);
                    break;

                case Hammers:                                                       // PFUNDT : a drum on each shoulder, one on the back
                    {
                        Vector2 near = CoatPoint(-0.145f, 0.035f, chest, down1, across, trail);
                        Vector2 far = CoatPoint(0.135f, 0.035f, chest, down1, across, trail);
                        Shapes.Circle(far, 0.056f * h, shade);
                        Shapes.Ring(far, 0.05f * h, 0.01f * h, edge);
                        Shapes.Circle(near, 0.066f * h, white);
                        Shapes.Ring(near, 0.059f * h, 0.012f * h, edge);
                        Shapes.Ring(CoatPoint(0f, 0.15f, chest, down1, across, trail), 0.05f * h, 0.006f * h, edge);
                    }
                    break;

                case Shield:                                                        // DUPORT : the cello's shape on the back, its spike below
                    Shapes.Ring(CoatPoint(0f, 0.12f, chest, down1, across, trail), 0.045f * h, 0.006f * h, edge);
                    Shapes.Ring(CoatPoint(0f, 0.27f, chest, down1, across, trail), 0.062f * h, 0.006f * h, edge);
                    Shapes.Kite(CoatPoint(0f, 0.42f, chest, down1, across, trail), CoatPoint(0f, 0.57f, chest, down1, across, trail), 0.022f * h, 0.15f, edge);
                    break;

                case Chakram:                                                       // PUNTO : the bell's rim at the hem
                    {
                        float rot = (float)Math.Atan2(across.Y, across.X);
                        Shapes.Ellipse(CoatPoint(-0.01f, 0.52f, chest, down1, across, trail), 0.2f * h, 0.018f * h, rot, edge, edge);
                    }
                    break;

                default:                                                            // PAGANINI : the violin's two f holes cut in the back
                    for (int side = -1; side <= 1; side += 2)
                    {
                        for (int k = 0; k < 6; k++)
                        {
                            float t = k / 5f;
                            float a = side * 0.03f + (float)Math.Sin(t * MathHelper.TwoPi) * 0.01f * side;
                            line[k] = CoatPoint(a, 0.06f + 0.11f * t, chest, down1, across, trail);
                            lineW[k] = (0.004f + 0.004f * (float)Math.Sin(t * MathHelper.Pi)) * h;
                        }
                        Shapes.Stroke(line, lineW, 6, cut, cut, false);
                        Shapes.Circle(line[0], 0.0055f * h, cut);
                        Shapes.Circle(line[5], 0.0055f * h, cut);
                    }
                    break;
            }
        }

        //Halo : the ring of the legend's colour tilted behind the head. PUNTO's is the horn's coil, a spiral.
        private void DrawHalo(Vector2 head, Color edge)
        {
            float h = H;
            Vector2 middle = head + new Vector2(-0.012f * Facing, -0.016f) * h;
            float tilt = -0.32f * Facing;
            float cos = (float)Math.Cos(tilt);
            float sin = (float)Math.Sin(tilt);
            int n = 28;
            for (int i = 0; i < n; i++)
            {
                float a = MathHelper.TwoPi * i / n;
                float rx = 0.068f * h;
                float ry = 0.021f * h;
                if (legend.Weapon == Chakram)
                {
                    float t = (float)i / (n - 1);
                    a = MathHelper.TwoPi * 1.6f * t + clock * 0.8f;
                    rx = MathHelper.Lerp(0.072f, 0.026f, t) * h;
                    ry = rx * 0.32f;
                }
                float x = (float)Math.Cos(a) * rx;
                float y = (float)Math.Sin(a) * ry;
                line[i] = middle + new Vector2(x * cos - y * sin, x * sin + y * cos);
                lineW[i] = 0.0055f * h;
            }
            Shapes.Stroke(line, lineW, n, edge, edge, legend.Weapon != Chakram);
        }

        //Draw Bits : the three small shapes floating round the figure, of the instrument's family
        private void DrawBits(Color white, Color edge)
        {
            float h = H;
            for (int b = 0; b < bits.Length; b++)
            {
                Vector2 at = bits[b].At;
                Color c = b == 1 ? edge : white;
                float turn = clock * (0.6f + 0.3f * b) + b;
                switch (legend.Weapon)
                {
                    case Rapier: Shapes.Circle(at, 0.01f * h, c); break;                                         // puffs of air
                    case Hammers: Shapes.Ring(at, 0.016f * h, 0.005f * h, c); break;                             // rings of a beat
                    case Shield: Shapes.Box(at, 0.022f * h, 0.022f * h, turn, c); break;                         // blocks
                    case Chakram: Shapes.Polygon(at, 0.017f * h, 3, turn, c); break;                             // sparks of the bell
                    default:                                                                                     // strings
                        Vector2 d = new Vector2((float)Math.Cos(turn), (float)Math.Sin(turn)) * (0.03f * h);
                        Shapes.Line(at - d, at + d, 0.006f * h, c);
                        break;
                }
            }
        }

        //Foot : a small triangle pointing at the floor
        private void DrawFoot(Vector2 tip, float size, Color color)
        {
            line[0] = tip + new Vector2(-size * 0.5f, -size);
            line[1] = tip + new Vector2(size * 0.5f, -size);
            line[2] = tip;
            Shapes.Fill(line, 3, color);
        }

        //Hand : a small diamond
        private void DrawHand(Vector2 at, float size, Color color)
        {
            Shapes.Polygon(at, size, 4, 0f, color);
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
            Vector2 head = body[Head].At;                                     // the ring behind the head glows a little
            Gfx.DrawGlow(sb, head.X, head.Y - 0.016f * h, legend.Core * 2.4f * h, Tint * (power * 0.45f));
        }
    }
}
