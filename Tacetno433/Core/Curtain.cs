using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //Curtain : the stage curtain that closes between two pages and opens on the next one (on the way to or from the
    //conductor select page). ScreenManager moves it in Update and draws it over everything.
    //
    //REAL CLOTH (Beta1 screens, 29 Oct, the user's pick : the drapes part in the middle like real cloth). The two drapes
    //still meet in the middle and part again, but now they hang on a rail like cloth. The inner edge is pulled by the
    //rope and leads, and the pleats behind it only move when the cloth between them pulls tight, so they come one after
    //another. Pulled back to the side of the stage, the inner edge pushes into the pleats and they pack tighter and
    //tighter. The hem of every pleat swings like a pendulum when its top is pulled, and calms down. The change takes as
    //long as before (each half 1 / ScreenManager.FadeSpeed seconds) : the drapes travel in the first UiCurtainReach of
    //each half and hang closed for the rest, so they settle before the page changes behind them.
    //
    //ADVANCED PART : POSITION BASED DYNAMICS, the "verlet" way of simulating rope and cloth (Jakobsen 2001, Muller 2007).
    //Every pleat is a point on the rail. Each small step :
    //   1. every free point moves on by the speed it had (inertia), and the rail slows it a little (drag)
    //   2. then the rules between two neighbours are fixed by moving the points : they never come closer than SLACK
    //      (the cloth is packed) and never further apart than the cloth between them (the cloth is tight). Packed is
    //      fixed at once, tight only part way each try (GIVE), so pulled cloth stretches a moment and the pleat behind
    //      snaps after it
    //   3. the point's new speed is how far it really moved in the step
    //The inner edge is not free, the rope moves it (Drive), and the far end is tied in the wings off the screen.
    //HEM : each pleat's hem is a damped spring (a pendulum). When the top of the pleat speeds up, the hem is thrown the
    //other way and swings back past the middle, like a skirt. The cloth curves from the rail down to the hem.
    //The drapes are drawn as one mesh of triangles (DrawUserIndexedPrimitives with a BasicEffect that only uses the
    //colour of each corner). The folds turn toward the light and away from it in turn, deeper where the pleats are
    //packed, and the hem is a darker band. The mesh is drawn in real pixels through Sharp.Transform (SHARP SCREEN), so
    //the cloth is smooth on every screen size.
    public static class Curtain
    {
        private const float Wing = 60f;            // the far end of a drape is tied this far off the side of the screen
        private const float Overlap = 2f;          // closed, the two inner edges cross by this much, so no seam shows
        private const float Step = 1f / 240f;      // the cloth moves in small steps of about this long, at any frame rate
        private const int Tries = 4;               // the rules between pleats are fixed this many times a step
        private const float HemH = 26f;            // the heavier band along the bottom
        private const int Across = 4;              // the mesh has this many columns in each pleat, for the shade of the fold
        private const int Down = 12;               // and this many rows from the rail to the hem, for the curve of the swing

        //Rail : drape 0 is the left one, drape 1 its mirror on the right. r is how far a pleat is from the far end in the
        //wings (game pixels). Point 0 is the inner edge, point count is the far end, which never moves.
        private static int count;
        private static float[][] r;
        private static float[][] speed;
        private static float[][] pull;             // how much a pleat sped up in the last step (pixels a second squared)
        private static float[][] cloth;            // the width of cloth between pleat i and i + 1, pulled tight
        private static float[] before = new float[0];
        private static float packed;               // where the inner edge rests when the drapes are open, packed in the wings
        private static float meet;                 // where it rests when the drapes meet in the middle
        private static float leadWas;              // where the rope had the inner edge last frame

        //HEM : how far each pleat's hem swings out (pixels, + toward the middle of the stage) and how fast
        private static float[][] swing;
        private static float[][] swingSpeed;
        private static float[][] swingRate;        // its own swing speed (radians a second), a little different per pleat

        //Mesh
        private static BasicEffect effect;
        private static VertexPositionColor[] corners = new VertexPositionColor[0];
        private static short[] order = new short[0];
        private static int triangles;
        private static float[] rowY = new float[Down + 3];
        private static float[] rowCurve = new float[Down + 3];
        private static Color[] rowShade = new Color[0];

        //Ensure : the rail, the cloth widths and the mesh order, made once (and again if UiCurtainPleats is changed)
        private static void Ensure()
        {
            int n = Math.Max(4, BattleRules.UiCurtainPleats);
            if (n == count && r != null) return;
            count = n;
            r = new float[2][];
            speed = new float[2][];
            pull = new float[2][];
            cloth = new float[2][];
            swing = new float[2][];
            swingSpeed = new float[2][];
            swingRate = new float[2][];
            before = new float[count + 1];
            meet = Wing + TacetGame.ScreenW / 2f + Overlap;
            packed = count * BattleRules.UiCurtainSlack;
            for (int d = 0; d < 2; d++)
            {
                r[d] = new float[count + 1];
                speed[d] = new float[count + 1];
                pull[d] = new float[count + 1];
                cloth[d] = new float[count];
                swing[d] = new float[count + 1];
                swingSpeed[d] = new float[count + 1];
                swingRate[d] = new float[count + 1];

                //Cloth : the pleats are a little uneven, like real cloth, and the two drapes differ, so they do not
                //move as one mirror. Together they still reach exactly to the middle.
                float sum = 0f;
                for (int i = 0; i < count; i++)
                {
                    cloth[d][i] = 1f + 0.14f * (float)Math.Sin(i * 2.3f + d * 1.7f);
                    sum += cloth[d][i];
                }
                for (int i = 0; i < count; i++) cloth[d][i] *= meet / sum;
                for (int i = 0; i <= count; i++)
                    swingRate[d][i] = MathHelper.TwoPi * BattleRules.UiCurtainSwing * (1f + 0.09f * (float)Math.Sin(i * 1.3f + d));
            }
            BuildMesh();
            Begin(0f);
        }

        //Begin : the cloth at rest for this much closed, called when a change with the curtain starts. Open, every
        //pleat is packed in the wings. Part way, the cloth hangs tight from the inner edge.
        public static void Begin(float closed)
        {
            Ensure();
            float lead = Drive(closed);
            leadWas = lead;
            for (int d = 0; d < 2; d++)
            {
                float taut = lead;
                for (int i = 0; i <= count; i++)
                {
                    r[d][i] = i == 0 ? lead : Math.Max((count - i) * BattleRules.UiCurtainSlack, taut);
                    if (i < count) taut -= cloth[d][i];
                    speed[d][i] = 0f;
                    pull[d][i] = 0f;
                    swing[d][i] = 0f;
                    swingSpeed[d][i] = 0f;
                }
                r[d][count] = 0f;
            }
        }

        //Drive : where the rope has the inner edge for closed 0..1 (ScreenManager's fade). It travels in the first
        //UiCurtainReach and hangs still, closed, after that. The same both ways, so a change turned back half way
        //carries on smoothly.
        private static float Drive(float closed)
        {
            float t = Travel(closed);
            float ease = t * t * t * (t * (t * 6f - 15f) + 10f);        // smootherstep : off gently, fast in the middle, a soft stop
            return MathHelper.Lerp(packed, meet, ease);
        }

        //Travel : 0..1, how far along the rope the inner edge is
        private static float Travel(float closed)
        {
            return MathHelper.Clamp(closed / BattleRules.UiCurtainReach, 0f, 1f);
        }

        //Update : move the cloth on by dt seconds. closed is ScreenManager's fade, 0 open to 1 closed.
        public static void Update(float dt, float closed)
        {
            Ensure();
            float lead = Drive(closed);
            if (dt <= 0f) return;
            dt = Math.Min(dt, 0.1f);

            //Small Steps : the rope's pull is shared out evenly over the frame
            int steps = Math.Max(1, Math.Min(24, (int)Math.Ceiling(dt / Step)));
            float h = dt / steps;
            for (int k = 1; k <= steps; k++)
            {
                float at = MathHelper.Lerp(leadWas, lead, (float)k / steps);
                StepDrape(0, at, h);
                StepDrape(1, at, h);
            }
            leadWas = lead;
        }

        //Step Drape : one small step of one drape (POSITION BASED DYNAMICS, see the top of the file)
        private static void StepDrape(int d, float lead, float h)
        {
            float[] p = r[d];
            float[] v = speed[d];
            float[] a = pull[d];
            float[] w = cloth[d];
            float slack = BattleRules.UiCurtainSlack;

            //The Rope : the inner edge goes where it is pulled
            float leadSpeed = (lead - p[0]) / h;
            a[0] = (leadSpeed - v[0]) / h;
            v[0] = leadSpeed;
            p[0] = lead;

            //1. Inertia and Drag
            float keep = Math.Max(0f, 1f - BattleRules.UiCurtainDrag * h);
            for (int i = 1; i < count; i++)
            {
                before[i] = p[i];
                a[i] = v[i];                           // the speed it came in with, for the hem below
                v[i] *= keep;
                p[i] += v[i] * h;
            }

            //2. Rules : packed is fixed at once, tight part way (GIVE). The inner edge and the far end never move.
            //EVEN : the cloth is springy too. Each try a pleat drifts a little toward its share of the way between its two
            //neighbours, so a push or a pull spreads along the whole drape (the pleats gather one after another, packed
            //tightest by the inner edge) instead of stopping at the next pleat.
            float even = BattleRules.UiCurtainEven;
            for (int t = 0; t < Tries; t++)
            {
                for (int i = 1; i < count; i++)
                {
                    float share = (p[i - 1] * w[i] + p[i + 1] * w[i - 1]) / (w[i - 1] + w[i]);
                    p[i] += (share - p[i]) * even;
                }
                for (int i = 1; i <= count; i++)
                {
                    float gap = p[i - 1] - p[i];
                    float wrong;
                    if (gap < slack) wrong = slack - gap;                               // packed : push apart
                    else if (gap > w[i - 1]) wrong = (w[i - 1] - gap) * BattleRules.UiCurtainGive;   // tight : pull together
                    else continue;

                    bool frontFree = i - 1 > 0;
                    bool backFree = i < count;
                    if (frontFree && backFree)
                    {
                        p[i - 1] += wrong * 0.5f;
                        p[i] -= wrong * 0.5f;
                    }
                    else if (frontFree) p[i - 1] += wrong;
                    else if (backFree) p[i] -= wrong;
                }
            }

            //3. Speed : how far each pleat really moved
            for (int i = 1; i < count; i++)
            {
                v[i] = (p[i] - before[i]) / h;
                a[i] = (v[i] - a[i]) / h;
            }

            //HEM : a damped spring thrown back by the pull on its top (semi-implicit Euler, stable at this step)
            float damp = BattleRules.UiCurtainSwingDamp;
            float most = BattleRules.UiCurtainSwingMost;
            float[] s = swing[d];
            float[] sv = swingSpeed[d];
            for (int i = 0; i < count; i++)
            {
                float k = swingRate[d][i];
                float push = -k * k * (s[i] + a[i] * BattleRules.UiCurtainSwingPull) - 2f * damp * k * sv[i];
                sv[i] += push * h;
                s[i] = MathHelper.Clamp(s[i] + sv[i] * h, -most, most);
            }
        }

        //Draw : closed 0 is fully open (nothing drawn), 1 is both drapes meeting in the middle
        public static void Draw(SpriteBatch sb, float closed)
        {
            if (closed <= 0f) return;
            Ensure();
            float t = Travel(closed);
            float ease = t * t * (3f - 2f * t);

            //House Lights : the room dims a little as the drapes come in
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Color.Black * (0.45f * ease));

            //Backing : while they hang closed the page changes behind them, so nothing may show through where two
            //swinging hems part at the bottom
            if (t >= 1f) Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.CurtainDark);

            //Mesh : out of the sprite batch, the cloth, and back in with the batch as the game began it
            GraphicsDevice device = sb.GraphicsDevice;
            sb.End();
            if (effect == null) effect = new BasicEffect(device) { VertexColorEnabled = true, LightingEnabled = false, TextureEnabled = false };
            Viewport view = device.Viewport;
            effect.World = Sharp.Transform;
            effect.View = Matrix.Identity;
            effect.Projection = Matrix.CreateOrthographicOffCenter(0, view.Width, view.Height, 0, -1f, 1f);

            //The cloth calms as it packs into the wings, so no hem is left swinging on the screen when the curtain is gone
            float calm = MathHelper.Clamp((r[0][0] - packed) / 120f, 0f, 1f);
            int at = 0;
            at = FillDrape(0, at, calm);
            FillDrape(1, at, calm);

            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;    // the right drape is a mirror, so its triangles face the other way
            effect.CurrentTechnique.Passes[0].Apply();
            device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, corners, 0, corners.Length, order, 0, triangles);

            Sharp.Begin(sb);
        }

        //Columns : corners across one drape, the pleats' columns and then the far end
        private static int Columns
        {
            get { return count * Across + 1; }
        }

        //Rows : corners down a column, Down + 1 rows of cloth, then the hem's top again (a hard edge) and its bottom
        private const int Rows = Down + 3;

        //Build Mesh : the order the corners are joined in, and each row's height and curve. Made once.
        private static void BuildMesh()
        {
            int cols = Columns;
            int perDrape = cols * Rows + 2 * Rows;                 // the cloth, then the thin line of light on the inner edge
            corners = new VertexPositionColor[perDrape * 2];
            rowShade = new Color[Rows];

            float h = TacetGame.ScreenH;
            for (int k = 0; k <= Down; k++) rowY[k] = -2f + (h - HemH + 2f) * k / Down;
            rowY[Down + 1] = h - HemH;
            rowY[Down + 2] = h + 6f;                               // a little past the bottom, so a swinging hem never shows a gap
            for (int k = 0; k < Rows; k++) rowCurve[k] = (float)Math.Pow(MathHelper.Clamp(rowY[k] / h, 0f, 1f), 1.6f);

            //Quads : two triangles between each four corners. The row between the last cloth row and the hem's top has no
            //height, it is skipped.
            int quadsPerDrape = (cols - 1) * (Rows - 2) + (Rows - 2);
            order = new short[quadsPerDrape * 6 * 2];
            int o = 0;
            for (int d = 0; d < 2; d++)
            {
                int start = d * perDrape;
                for (int k = 0; k < Rows - 1; k++)
                {
                    if (k == Down) continue;
                    for (int c = 0; c < cols - 1; c++)
                        o = Quad(o, start + k * cols + c, start + k * cols + c + 1, start + (k + 1) * cols + c, start + (k + 1) * cols + c + 1);
                }
                int edge = start + cols * Rows;
                for (int k = 0; k < Rows - 1; k++)
                {
                    if (k == Down) continue;
                    o = Quad(o, edge + k * 2, edge + k * 2 + 1, edge + (k + 1) * 2, edge + (k + 1) * 2 + 1);
                }
            }
            triangles = o / 3;
        }

        private static int Quad(int o, int topLeft, int topRight, int bottomLeft, int bottomRight)
        {
            order[o++] = (short)topLeft;
            order[o++] = (short)topRight;
            order[o++] = (short)bottomLeft;
            order[o++] = (short)topRight;
            order[o++] = (short)bottomRight;
            order[o++] = (short)bottomLeft;
            return o;
        }

        //Fill Drape : the corners of one drape for this frame. Returns where the next drape's corners start.
        private static int FillDrape(int d, int at, float calm)
        {
            float[] p = r[d];
            float[] s = swing[d];
            float slack = BattleRules.UiCurtainSlack;
            int cols = Columns;
            float h = TacetGame.ScreenH;

            for (int c = 0; c < cols; c++)
            {
                //Where the column is : part way along one pleat of cloth, between two points on the rail
                int i = Math.Min(c / Across, count - 1);
                float u = c == cols - 1 ? 1f : (float)(c - i * Across) / Across;
                float top = MathHelper.Lerp(p[i], p[i + 1], u);
                float out_ = MathHelper.Lerp(s[i], s[i + 1], u) * calm;

                //Fold : every other pleat turns toward the light. Packed pleats fold deeper and shade each other.
                float tight = Math.Max(slack + 0.01f, cloth[d][i]);
                float depth = MathHelper.Clamp((tight - (p[i] - p[i + 1])) / (tight - slack), 0f, 1f);
                float fold = BattleRules.UiCurtainFold + (1f - BattleRules.UiCurtainFold) * depth;
                float bow = (float)Math.Sin(u * MathHelper.Pi) * (i % 2 == 0 ? 1f : -1f);
                float light = (0.5f + 0.5f * fold * bow) * (1f - 0.35f * depth);

                for (int k = 0; k < Rows; k++)
                {
                    float y = rowY[k];
                    float x = ScreenX(d, top + out_ * rowCurve[k]);

                    //Stage Light : brightest a little below the middle, dark under the rail, the hem a heavier band
                    float lit = light * (0.72f + 0.28f * (float)Math.Sin(MathHelper.Pi * MathHelper.Clamp(y / h, 0f, 1f) * 0.92f));
                    if (k > Down) lit *= 0.62f;
                    Color color = Color.Lerp(Palette.CurtainDark, Palette.CurtainLight, MathHelper.Clamp(lit, 0f, 1f));
                    corners[at + k * cols + c] = new VertexPositionColor(new Vector3(x, y, 0f), color);
                }
            }

            //Inner Edge : a thin line of light where the drape meets the stage, following the swing of the hem
            int edge = at + cols * Rows;
            Color line = Palette.Paper * 0.2f;
            for (int k = 0; k < Rows; k++)
            {
                float x = ScreenX(d, p[0] + s[0] * calm * rowCurve[k]);
                float inward = d == 0 ? -2f : 2f;                   // the line sits on the cloth side of the edge
                corners[edge + k * 2] = new VertexPositionColor(new Vector3(x + inward, rowY[k], 0f), line);
                corners[edge + k * 2 + 1] = new VertexPositionColor(new Vector3(x, rowY[k], 0f), line);
            }
            return edge + 2 * Rows;
        }

        //Screen X : a point on the rail (and its swing) on the screen. The left drape is tied off the left side, the
        //right drape off the right side, so + toward the middle is right on one and left on the other.
        private static float ScreenX(int d, float onRail)
        {
            return d == 0 ? -Wing + onRail : TacetGame.ScreenW + Wing - onRail;
        }
    }
}
