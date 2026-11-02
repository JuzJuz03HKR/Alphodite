using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //Shapes (Beta1 1 Nov, MATH ART, docs/BETA1_DIRECTION.md 31) : the pen the fighters are drawn with (Core/Figure.cs).
    //Every shape is cut into triangles, each corner with its own colour. The triangles wait in one list and go to the card
    //in one go when the list is full or the drawing ends (a BATCH : SpriteBatch does the same for pictures).
    //
    //   SOFT EDGE   every shape gets a thin band of corners round it that fade to nothing (FEATHER, about one real pixel
    //               wide), so its edge looks smooth at every screen size : anti aliasing without a shader
    //   HARD EDGE   FEATHER 0, no band (the edges are as hard as the triangles)
    //
    //Use : Begin, the shapes, End. Nothing else may draw in between (the sprite batch is ended first, see Figure.cs).
    //Colours are premultiplied like everything else in MonoGame : Color.White * 0.5f is half seen white.
    //Optimize : the lists are made once (no garbage). A fighter is a few hundred triangles, all of them one draw call.
    public static class Shapes
    {
        private const int MaxCorners = 30000;                        // a short number reaches 32767
        private static readonly VertexPositionColor[] corners = new VertexPositionColor[MaxCorners];
        private static readonly short[] order = new short[MaxCorners * 3];
        private static int cornerCount;
        private static int orderCount;
        private static BasicEffect effect;
        private static GraphicsDevice device;

        //Feather : how wide the soft edge is, in the units being drawn (0 = a hard edge)
        public static float Feather;

        //Blending : how the triangles mix with what is under them (the usual see-through mix unless a caller asks otherwise).
        //Set it back to BlendState.AlphaBlend when done.
        public static BlendState Blending = BlendState.AlphaBlend;

        //Work : the points of one shape while it is cut up (no shape has more than MaxPoints)
        public const int MaxPoints = 256;
        private static readonly Vector2[] work = new Vector2[MaxPoints];
        private static readonly float[] widths = new float[MaxPoints];
        private static readonly Vector2[] normals = new Vector2[MaxPoints];
        private static readonly short[] inner = new short[MaxPoints];
        private static readonly short[] outer = new short[MaxPoints];
        private static readonly short[] left = new short[MaxPoints];
        private static readonly short[] right = new short[MaxPoints];

        //Begin : world moves the shapes onto the target (the camera times Sharp.Transform, as a sprite batch would take it)
        public static void Begin(GraphicsDevice d, Matrix world, float feather)
        {
            device = d;
            if (effect == null) effect = new BasicEffect(d) { VertexColorEnabled = true, LightingEnabled = false, TextureEnabled = false };
            Viewport view = d.Viewport;
            effect.World = world;
            effect.View = Matrix.Identity;
            effect.Projection = Matrix.CreateOrthographicOffCenter(0, view.Width, view.Height, 0, -1f, 1f);
            Feather = feather;
            cornerCount = 0;
            orderCount = 0;
        }

        public static void End()
        {
            Flush();
        }

        //Flush : the triangles waiting go to the card
        private static void Flush()
        {
            if (orderCount == 0 || device == null) return;
            device.BlendState = Blending;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;             // shapes come in both turning directions
            effect.CurrentTechnique.Passes[0].Apply();
            device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, corners, 0, cornerCount, order, 0, orderCount / 3);
            cornerCount = 0;
            orderCount = 0;
        }

        //Room : flush first if a shape of this many corners would not fit
        private static void Room(int cornersNeeded)
        {
            if (cornerCount + cornersNeeded >= MaxCorners || orderCount + cornersNeeded * 6 >= order.Length) Flush();
        }

        private static short Corner(Vector2 at, Color color)
        {
            corners[cornerCount].Position = new Vector3(at, 0f);
            corners[cornerCount].Color = color;
            return (short)cornerCount++;
        }

        private static void Tri(short a, short b, short c)
        {
            order[orderCount++] = a;
            order[orderCount++] = b;
            order[orderCount++] = c;
        }

        private static void Quad(short a, short b, short c, short d)
        {
            Tri(a, b, c);
            Tri(a, c, d);
        }

        //======================================================================================
        //Filled shapes
        //======================================================================================

        //Fill : a closed shape, its points in order round the edge, one colour
        public static void Fill(Vector2[] p, int n, Color color)
        {
            FillShaded(p, n, color, color, 0f, 1f);
        }

        //Fill Shaded : the colour runs from top (at y = fromY) to bottom (at y = toY), the way light falls from above.
        //The shape is filled as a fan from its middle, so it must be convex, or at least see all its edges from the middle
        //(a crescent is a Stroke instead).
        //ADVANCED PART : the SOFT EDGE. Each edge's outward direction is the edge turned a quarter (its normal), flipped if it
        //points at the middle. A corner of the soft band sits on the corner of the shape pushed out along the mean of its two
        //edges' normals (a MITER), made longer at a sharp corner so the band keeps its width, but never more than 2.5 times.
        public static void FillShaded(Vector2[] p, int n, Color top, Color bottom, float fromY, float toY)
        {
            if (n < 3) return;
            if (n > MaxPoints) n = MaxPoints;
            Room(n * 2 + 1);
            bool soft = Feather > 0f;

            Vector2 middle = Vector2.Zero;
            for (int i = 0; i < n; i++) middle += p[i];
            middle /= n;
            short centre = Corner(middle, Shade(middle.Y, top, bottom, fromY, toY));
            for (int i = 0; i < n; i++) inner[i] = Corner(p[i], Shade(p[i].Y, top, bottom, fromY, toY));
            for (int i = 0; i < n; i++) Tri(centre, inner[i], inner[(i + 1) % n]);
            if (!soft) return;

            //edge normals pointing out
            for (int i = 0; i < n; i++)
            {
                Vector2 a = p[i];
                Vector2 b = p[(i + 1) % n];
                Vector2 edge = b - a;
                Vector2 normal = edge.LengthSquared() > 0.0001f ? Vector2.Normalize(new Vector2(edge.Y, -edge.X)) : Vector2.Zero;
                if (Vector2.Dot(normal, (a + b) * 0.5f - middle) < 0f) normal = -normal;
                normals[i] = normal;
            }
            for (int i = 0; i < n; i++)
            {
                Vector2 before = normals[(i + n - 1) % n];
                Vector2 after = normals[i];
                outer[i] = Corner(p[i] + Miter(before, after) * Feather, Color.Transparent);
            }
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                Quad(inner[i], inner[j], outer[j], outer[i]);
            }
        }

        //Miter : the way to push a corner out so a band along both its edges keeps its width
        private static Vector2 Miter(Vector2 before, Vector2 after)
        {
            Vector2 mean = before + after;
            if (mean.LengthSquared() < 0.0001f) return after;
            mean.Normalize();
            float cos = Vector2.Dot(mean, after);
            return mean / Math.Max(0.4f, cos);
        }

        private static Color Shade(float y, Color top, Color bottom, float fromY, float toY)
        {
            if (top == bottom) return top;
            float t = MathHelper.Clamp((y - fromY) / Math.Max(0.001f, toY - fromY), 0f, 1f);
            return Color.Lerp(top, bottom, t);
        }

        //Circle : a round shape. More corners on a bigger screen (SHARP SCREEN), so no corner ever shows.
        public static void Circle(Vector2 at, float radius, Color color)
        {
            Ellipse(at, radius, radius, 0f, color, color);
        }

        //Ellipse : a squashed circle, turned by angle, shaded from top to bottom
        public static void Ellipse(Vector2 at, float rx, float ry, float angle, Color top, Color bottom)
        {
            if (rx <= 0.05f || ry <= 0.05f) return;
            int n = Segments(Math.Max(rx, ry));
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);
            for (int i = 0; i < n; i++)
            {
                float a = MathHelper.TwoPi * i / n;
                float x = (float)Math.Cos(a) * rx;
                float y = (float)Math.Sin(a) * ry;
                work[i] = at + new Vector2(x * cos - y * sin, x * sin + y * cos);
            }
            FillShaded(work, n, top, bottom, at.Y - ry, at.Y + ry);
        }

        //Segments : corners round a circle of this radius (about 4 real pixels a piece, 12 to 128)
        public static int Segments(float radius)
        {
            float real = radius * Sharp.Scale * (Feather > 0f ? 1f : 0.5f);
            return (int)MathHelper.Clamp(real * MathHelper.TwoPi / 4f, 12f, 128f);
        }

        //Kite : a blade or a shard. From the root to the tip, widest (width) at the share widestAt of the way
        public static void Kite(Vector2 root, Vector2 tip, float width, float widestAt, Color color)
        {
            Vector2 along = tip - root;
            if (along.LengthSquared() < 0.01f) return;
            Vector2 across = Vector2.Normalize(new Vector2(-along.Y, along.X)) * (width * 0.5f);
            Vector2 wide = root + along * widestAt;
            work[0] = root;
            work[1] = wide + across;
            work[2] = tip;
            work[3] = wide - across;
            Fill(work, 4, color);
        }

        //Box : a rectangle turned by angle round its middle
        public static void Box(Vector2 middle, float w, float h, float angle, Color color)
        {
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);
            Vector2 x = new Vector2(cos, sin) * (w * 0.5f);
            Vector2 y = new Vector2(-sin, cos) * (h * 0.5f);
            work[0] = middle - x - y;
            work[1] = middle + x - y;
            work[2] = middle + x + y;
            work[3] = middle - x + y;
            Fill(work, 4, color);
        }

        //Polygon : a shape with corners all the same distance from the middle (3 a triangle, 4 a diamond ...)
        public static void Polygon(Vector2 middle, float radius, int sides, float angle, Color color)
        {
            if (sides < 3) return;
            if (sides > MaxPoints) sides = MaxPoints;
            for (int i = 0; i < sides; i++)
            {
                float a = angle + MathHelper.TwoPi * i / sides;
                work[i] = middle + new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * radius;
            }
            Fill(work, sides, color);
        }

        //Ring : a circle's outline with a width, soft on both sides
        public static void Ring(Vector2 at, float radius, float thickness, Color color)
        {
            Arc(at, radius, 0f, MathHelper.TwoPi, thickness, thickness, color, true);
        }

        //Arc : part of a ring, from one angle to another, its width going from w0 to w1
        public static void Arc(Vector2 at, float radius, float from, float to, float w0, float w1, Color color, bool closed = false)
        {
            float span = Math.Abs(to - from);
            int n = Math.Max(3, (int)(Segments(radius) * span / MathHelper.TwoPi) + 2);
            if (closed) n = Segments(radius);
            if (n > MaxPoints - 1) n = MaxPoints - 1;
            int count = closed ? n : n + 1;
            for (int i = 0; i < count; i++)
            {
                float t = closed ? (float)i / n : (float)i / n;
                float a = from + (to - from) * t;
                work[i] = at + new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * radius;
                widths[i] = MathHelper.Lerp(w0, w1, t);
            }
            Stroke(work, widths, count, color, color, closed);
        }

        //Crescent : a blade bent round a circle, sharp at both ends and widest in the middle (the moon's sickle)
        public static void Crescent(Vector2 at, float radius, float from, float to, float thickness, Color color)
        {
            float span = Math.Abs(to - from);
            int n = Math.Max(6, (int)(Segments(radius) * span / MathHelper.TwoPi) + 4);
            if (n > MaxPoints - 1) n = MaxPoints - 1;
            for (int i = 0; i <= n; i++)
            {
                float t = (float)i / n;
                float a = from + (to - from) * t;
                work[i] = at + new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * radius;
                widths[i] = thickness * (float)Math.Pow(Math.Sin(t * MathHelper.Pi), 0.75);
            }
            Stroke(work, widths, n + 1, color, color, false);
        }

        //======================================================================================
        //Strokes
        //======================================================================================

        //Line : a straight stroke of one width
        public static void Line(Vector2 a, Vector2 b, float width, Color color)
        {
            work[0] = a;
            work[1] = b;
            widths[0] = width;
            widths[1] = width;
            Stroke(work, widths, 2, color, color, false);
        }

        //Taper : a straight stroke thick at a, thin at b
        public static void Taper(Vector2 a, Vector2 b, float wa, float wb, Color color)
        {
            work[0] = a;
            work[1] = b;
            widths[0] = wa;
            widths[1] = wb;
            Stroke(work, widths, 2, color, color, false);
        }

        //Stroke : a line through n points, as wide at each point as w says, its colour going from c0 to c1.
        //Strands of hair, tails and ribbons are strokes along the points of a chain (Figure.cs).
        //ADVANCED PART : at each point the line's way is the way from the point before to the point after (a TANGENT),
        //turned a quarter it gives the sides (a NORMAL). The stroke's two edges are the points pushed out to both sides by
        //half the width, and each step between two points is two triangles. The soft band is the same edges pushed out
        //again by FEATHER, plus a short band across both open ends.
        public static void Stroke(Vector2[] p, float[] w, int n, Color c0, Color c1, bool closed)
        {
            if (n < 2) return;
            if (n > MaxPoints) n = MaxPoints;
            bool soft = Feather > 0f;
            Room(n * 4 + 4);

            for (int i = 0; i < n; i++)
            {
                Vector2 before, after;
                if (closed)
                {
                    before = p[(i + n - 1) % n];
                    after = p[(i + 1) % n];
                }
                else
                {
                    before = p[Math.Max(0, i - 1)];
                    after = p[Math.Min(n - 1, i + 1)];
                }
                Vector2 way = after - before;
                if (way.LengthSquared() < 0.0001f) way = new Vector2(1f, 0f);
                way.Normalize();
                normals[i] = new Vector2(-way.Y, way.X);
            }

            for (int i = 0; i < n; i++)
            {
                float t = closed ? 0f : (float)i / (n - 1);
                Color c = c0 == c1 ? c0 : Color.Lerp(c0, c1, t);
                float half = Math.Max(0f, w[i]) * 0.5f;
                left[i] = Corner(p[i] + normals[i] * half, c);
                right[i] = Corner(p[i] - normals[i] * half, c);
            }
            int steps = closed ? n : n - 1;
            for (int i = 0; i < steps; i++)
            {
                int j = (i + 1) % n;
                Quad(left[i], left[j], right[j], right[i]);
            }
            if (!soft) return;

            //the soft band along both edges
            for (int i = 0; i < n; i++)
            {
                float half = Math.Max(0f, w[i]) * 0.5f;
                inner[i] = Corner(p[i] + normals[i] * (half + Feather), Color.Transparent);
                outer[i] = Corner(p[i] - normals[i] * (half + Feather), Color.Transparent);
            }
            for (int i = 0; i < steps; i++)
            {
                int j = (i + 1) % n;
                Quad(left[i], left[j], inner[j], inner[i]);
                Quad(right[i], right[j], outer[j], outer[i]);
            }
            if (closed) return;

            //and across the two ends
            EndBand(p[0] - p[1], 0, p, w);
            EndBand(p[n - 1] - p[n - 2], n - 1, p, w);
        }

        //End Band : the soft band across an open end of a stroke, pushed out the way the stroke was going, and the two
        //small wedges that join it to the bands along the sides
        private static void EndBand(Vector2 outWay, int i, Vector2[] p, float[] w)
        {
            if (outWay.LengthSquared() < 0.0001f) return;
            outWay.Normalize();
            float half = Math.Max(0f, w[i]) * 0.5f + Feather;
            Vector2 push = outWay * Feather;
            short a = Corner(p[i] + normals[i] * half + push, Color.Transparent);
            short b = Corner(p[i] - normals[i] * half + push, Color.Transparent);
            Quad(left[i], right[i], b, a);
            Tri(left[i], a, inner[i]);
            Tri(right[i], b, outer[i]);
        }

        //======================================================================================
        //Curves
        //======================================================================================

        //Bend : n points along a curve from a to c pulled towards b (a quadratic BEZIER), into the array given
        public static void Bend(Vector2 a, Vector2 b, Vector2 c, int n, Vector2[] into)
        {
            for (int i = 0; i < n; i++)
            {
                float t = n == 1 ? 0f : (float)i / (n - 1);
                float u = 1f - t;
                into[i] = a * (u * u) + b * (2f * u * t) + c * (t * t);
            }
        }
    }
}
