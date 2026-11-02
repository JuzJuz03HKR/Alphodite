using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //LEGEND ART (Beta1 2 Nov, block C items 3 4 6, docs/BETA1_DIRECTION.md 33) : the user 13.106, "the art is only on the
    //Maestro page, everything else is math art". The pictures the team will not draw are drawn by the code on every page.
    //
    //   PORTRAIT  a legend (or a boss) as the same figure the fight draws (Core/Figure.cs), alive in its window : it breathes,
    //             sways, and its hair and cloth blow in the WIND of the pointer (the mouse is the baton). Each window keeps its
    //             own figure, so its springs only feel that window move.
    //                COLUMN  a tall slanted window, the whole legend from head to feet (the band page, the audition, the name
    //                        page, the luthier, the curtain call)
    //                FACE    a square, the head and the shoulders (rehearsal, the friend cards, an event's cards, the band strip)
    //                WHOLE   the whole figure fitted in its box (a boss in THE TELL and THE STRIKE)
    //   SIGIL     a conductor as their mark in a few lines, never a person (the user 13.87 named them) :
    //                STORM   THE UNHEARING : a lightning strike in red through a ring of storm cloud, rain blown sideways
    //                RIFT    THE UNSIGNED : two cuts through the world, Fanny's in rainbow with prism cracks, Felix's a
    //                        purple, blue and black burn behind it
    //                HOLY    THE METRONOME : a warm sky of countless small stars and one brighter star that points the way
    //
    //Pages call Portrait.Update every frame (it only steps the springs) and Portrait.Draw with the window where it is now.
    //Drawing steps out of the page's sprite batch and begins it again as TacetGame does (Sharp.Begin), so a page calls it
    //where its normal batch is running. Numbers : Data/BattleRules.cs, Legend Art.
    //Portrait Set : one page's portraits of the legends, each made the first time it is asked for (one per legend, so a
    //legend that changes place on the page walks there), stepped together
    public class PortraitSet
    {
        private readonly Portrait.Frame shape;
        private readonly Portrait[] byIndex = new Portrait[MusicianList.All.Length];

        public PortraitSet(Portrait.Frame shape)
        {
            this.shape = shape;
        }

        public Portrait Get(Musician m)
        {
            Portrait p = byIndex[m.Index];
            if (p != null) return p;
            p = Portrait.Of(m, shape, m.Index + 1);
            byIndex[m.Index] = p;
            return p;
        }

        //Tell : the run's life and stars on the portraits of its band (made here if they are not yet)
        public void Tell(RunState run)
        {
            if (run == null) return;
            for (int i = 0; i < run.Roster.Count; i++) Get(run.Roster[i]);
            for (int i = 0; i < byIndex.Length; i++)
                if (byIndex[i] != null && run.Roster.Contains(MusicianList.All[i]))
                    byIndex[i].Tell(MusicianList.All[i], run.LifeOf(MusicianList.All[i]) / (float)Math.Max(1, run.MaxLifeOf(MusicianList.All[i])));
        }

        public void Update(float dt)
        {
            for (int i = 0; i < byIndex.Length; i++)
                if (byIndex[i] != null) byIndex[i].Update(dt);
        }
    }

    public class Portrait
    {
        public enum Frame { Column, Face, Whole }

        public readonly Frame Shape;
        public readonly Figure Figure;
        private Rectangle window;
        private bool stepped;

        public Portrait(Figure.Sort sort, string key, Color tint, Color hot, Frame shape, int seed)
        {
            Shape = shape;
            Figure = new Figure(sort, key, seed);
            Figure.Tint = tint;
            Figure.Hot = hot;
            Figure.Cues.Life = 1f;
            Figure.Cues.Button = -1;
        }

        //Of : a legend's portrait, in their own colour (the element of round 6)
        public static Portrait Of(Musician m, Frame shape, int seed)
        {
            return new Portrait(Figure.Sort.Musician, ArtBank.Key(m.Name), Palette.SpotOf(m.Spot), Palette.SpotHotOf(m.Spot), shape, seed);
        }

        //Tell : what the run shows on the legend's body (LEGEND CUES, Core/Figure.Band.cs) : the life left (PAGANINI's LIFE
        //STRING, PFUNDT's heartbeat) and the REHEARSAL stars (three on a button is that button's LEGEND FORM)
        public void Tell(Musician m, float life)
        {
            Figure.Cues.Life = MathHelper.Clamp(life, 0f, 1f);
            Figure.Cues.Stars0 = RehearsalList.StarsOf(m, 0);
            Figure.Cues.Stars1 = RehearsalList.StarsOf(m, 1);
            Figure.Cues.Stars2 = RehearsalList.StarsOf(m, 2);
        }

        //Update : the springs, the chains and the breath, with the pointer's wind where the window last was
        public void Update(float dt)
        {
            if (window.Width <= 0) return;
            Figure.Update(dt, LegendArt.WindNear(window));
            stepped = true;
        }

        //Draw : the figure in its window (where it is this frame), cut by the window's edges, with its light added round it
        public void Draw(SpriteBatch sb, Rectangle box, float alpha)
        {
            Draw(sb, box, alpha, 0f);
        }

        public void Draw(SpriteBatch sb, Rectangle box, float alpha, float flash)
        {
            window = box;
            Place();
            if (!stepped) Update(1f / 60f);                            // the first frame : every part goes where it stands
            if (alpha <= 0.01f) return;
            LegendArt.BeginShapes(sb, box);
            Figure.Draw(alpha, flash, 1f, 0f);
            LegendArt.ShapesToGlow(sb);
            Figure.DrawGlow(sb, alpha * BattleRules.LegendGlow);
            LegendArt.EndClip(sb);
        }

        //Place : where the figure stands in its window, the way its frame shows it
        private void Place()
        {
            if (Shape == Frame.Face)
            {
                Figure.Height = window.Height * BattleRules.LegendFaceZoom;
                Figure.Feet = new Vector2(window.Center.X, window.Center.Y + Figure.Height * BattleRules.LegendFaceAnchor);
            }
            else if (Shape == Frame.Column)
            {
                Figure.Height = window.Height * BattleRules.LegendColumnTall;
                Figure.Feet = new Vector2(window.Center.X - BandColumn.Slant * 0.3f, window.Bottom - window.Height * BattleRules.LegendColumnFoot);
            }
            else
            {
                Figure.Height = window.Height * 0.86f;
                Figure.Feet = new Vector2(window.Center.X, window.Bottom - window.Height * 0.05f);
            }
        }
    }

    public static class LegendArt
    {
        //======================================================================================
        //WIND : the pointer is the baton (the fight blows the figures with the baton's swing, Core/Figure.cs)
        //======================================================================================

        private static Vector2 lastPointer;
        private static bool pointerSeen;
        private static Vector2 wind;                 // page pixels a second squared, before the distance takes its share

        //Update : once a frame from TacetGame, the pointer's speed becomes the wind, calming when it stops
        public static void Update(float dt)
        {
            if (dt <= 0f) return;
            Vector2 at = Input.MousePos;
            if (!pointerSeen)
            {
                lastPointer = at;
                pointerSeen = true;
            }
            Vector2 push = (at - lastPointer) / dt * BattleRules.LegendWind;
            lastPointer = at;
            if (push.Length() > BattleRules.LegendWindMax) push = Vector2.Normalize(push) * BattleRules.LegendWindMax;
            float calm = MathHelper.Clamp(BattleRules.LegendWindCalm * dt, 0f, 1f);
            wind = Vector2.Lerp(wind, push, Math.Max(calm, push.LengthSquared() > wind.LengthSquared() ? 0.5f : 0f));
        }

        //Wind Near : the wind a window feels, all of it under the pointer, none LegendWindReach away
        public static Vector2 WindNear(Rectangle window)
        {
            float far = Vector2.Distance(Input.MousePos, new Vector2(window.Center.X, window.Center.Y)) - window.Height * 0.4f;
            float share = 1f - MathHelper.Clamp(far / BattleRules.LegendWindReach, 0f, 1f);
            return wind * share;
        }

        //======================================================================================
        //CLIP : drawing cut by a window
        //ADVANCED PART : the card can be told to drop every pixel outside one rectangle (the SCISSOR test). The rectangle is in
        //real pixels, so the page's 1280 x 720 window is multiplied by Sharp.Scale (SHARP SCREEN). The shapes pen and the sprite
        //batch both need a rasterizer state with ScissorTestEnable to obey it, and the rectangle the page had before is put back.
        //======================================================================================

        private static readonly RasterizerState clipped = new RasterizerState { CullMode = CullMode.None, ScissorTestEnable = true };
        private static Rectangle before;
        private static bool clipping;

        //Begin Shapes : out of the page's batch, the scissor on the window, the shapes pen begun (Core/Shapes.cs)
        public static void BeginShapes(SpriteBatch sb, Rectangle window)
        {
            sb.End();
            Scissor(sb.GraphicsDevice, window);
            Shapes.Clip = clipping;
            Shapes.Begin(sb.GraphicsDevice, Sharp.Transform, Sharp.Dot);
        }

        //Shapes To Glow : the shapes ended, an adding sprite batch begun in the same window, for the light
        public static void ShapesToGlow(SpriteBatch sb)
        {
            Shapes.End();
            Shapes.Clip = false;
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, null, clipping ? clipped : null, null, Sharp.Transform);
        }

        //Begin Clip : out of the page's batch into one cut by the window (blend AlphaBlend, or Additive for light)
        public static void BeginClip(SpriteBatch sb, Rectangle window, BlendState blend)
        {
            sb.End();
            Scissor(sb.GraphicsDevice, window);
            sb.Begin(SpriteSortMode.Deferred, blend, SamplerState.LinearClamp, null, clipping ? clipped : null, null, Sharp.Transform);
        }

        //Switch Blend : the same window, another blend (the shapes of a sigil, then its light)
        public static void SwitchBlend(SpriteBatch sb, BlendState blend)
        {
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, blend, SamplerState.LinearClamp, null, clipping ? clipped : null, null, Sharp.Transform);
        }

        //End Clip : the old scissor back and the page's batch begun again as TacetGame begins it
        public static void EndClip(SpriteBatch sb)
        {
            sb.End();
            if (clipping) sb.GraphicsDevice.ScissorRectangle = before;
            clipping = false;
            Sharp.Begin(sb);
        }

        private static void Scissor(GraphicsDevice device, Rectangle window)
        {
            clipping = window.Width > 0 && window.Height > 0;
            if (!clipping) return;
            before = device.ScissorRectangle;
            float s = Sharp.Scale;
            Rectangle r = new Rectangle((int)Math.Floor(window.X * s), (int)Math.Floor(window.Y * s), (int)Math.Ceiling(window.Width * s), (int)Math.Ceiling(window.Height * s));
            Rectangle room = new Rectangle(0, 0, device.Viewport.Width, device.Viewport.Height);
            device.ScissorRectangle = Rectangle.Intersect(r, room);
        }

        //======================================================================================
        //SIGIL : a conductor as their mark (the user 13.87), drawn with lines and light in a window
        //======================================================================================

        private const int Stars = 48;                // HOLY : the small stars of the warm sky
        private const int BoltPoints = 9;            // STORM : the bolt's jagged line
        private static readonly Vector2[] bolt = new Vector2[BoltPoints];

        //Sigil : a conductor's mark filling box. time is the page's clock (it moves by itself). alpha 0..1. burst 0..1 lights
        //it as at its strongest (the FINALE's cut in), 0 lets it breathe by itself.
        private static float burstNow;

        public static void Sigil(SpriteBatch sb, Conductor c, Rectangle box, float time, float alpha, float burst = 0f)
        {
            burstNow = MathHelper.Clamp(burst, 0f, 1f);
            if (alpha <= 0.01f || c == null) return;
            BeginClip(sb, box, BlendState.AlphaBlend);
            Vector2 mid = new Vector2(box.Center.X, box.Center.Y - box.Height * 0.04f);
            float r = Math.Min(box.Width, box.Height) * 0.36f;
            if (c.Aura == AuraKind.Holy) HolyLines(sb, box, mid, r, time, alpha);
            else if (c.Aura == AuraKind.Prism) RiftLines(sb, box, mid, r, time, alpha);
            else StormLines(sb, box, mid, r, time, alpha);
            SwitchBlend(sb, BlendState.Additive);
            if (c.Aura == AuraKind.Holy) HolyLight(sb, box, mid, r, time, alpha);
            else if (c.Aura == AuraKind.Prism) RiftLight(sb, mid, r, time, alpha);
            else StormLight(sb, mid, r, time, alpha);
            EndClip(sb);
        }

        //Hash : a steady random 0..1 from two whole numbers (the sine trick of TacetField)
        private static float Hash(int a, int b)
        {
            float h = (float)Math.Sin(a * 12.9898f + b * 78.233f) * 43758.547f;
            return h - (float)Math.Floor(h);
        }

        //STORM : THE UNHEARING. Grey cloud arcs turning round the middle, rain leaning in the wind, and a red bolt that strikes
        //through the ring every SigilBoltEvery seconds and stays as a faint ember between strikes (so a still frame still reads).
        private static Color BoltRed { get { return Palette.StormRed; } }
        private static Color BoltHot { get { return Palette.StormRedHot; } }

        private static void StormLines(SpriteBatch sb, Rectangle box, Vector2 mid, float r, float time, float a)
        {
            float spin = time * BattleRules.SigilSpin;
            for (int i = 0; i < 3; i++)                                                    // the cloud ring
            {
                float from = spin + i * MathHelper.TwoPi / 3f;
                Gfx.Arc(sb, mid.X, mid.Y, r * (1f + 0.08f * i), from, from + 1.5f, Palette.PaperDim * (0.5f * a), 3f + i);
                Gfx.Arc(sb, mid.X, mid.Y, r * (0.86f + 0.05f * i), from + 0.6f, from + 1.6f, Palette.LineGrey * (0.6f * a), 2f);
            }
            for (int i = 0; i < 18; i++)                                                   // the rain, blown left
            {
                float x = box.X + Hash(i, 3) * (box.Width + 60f);
                float y = box.Y + (Hash(i, 7) * box.Height + time * (260f + 90f * Hash(i, 9))) % (box.Height + 30f) - 20f;
                Gfx.Line(sb, x, y, x - 9f, y + 22f, Palette.PaperDim * (0.22f * a), 1f);
            }
            float strike = Strike(time);
            MakeBolt(box, mid, r, (int)(time / BattleRules.SigilBoltEvery));
            Color ink = Color.Lerp(BoltRed * 0.85f, BoltHot, strike) * a;
            Color core = BoltHot * ((0.35f + 0.65f * strike) * a);
            for (int i = 0; i + 1 < BoltPoints; i++)
            {
                Gfx.Line(sb, bolt[i], bolt[i + 1], ink, 2.5f + 3f * strike);
                Gfx.Line(sb, bolt[i], bolt[i + 1], core, 1f);                          // its white hot middle
            }
        }

        private static void StormLight(SpriteBatch sb, Vector2 mid, float r, float time, float a)
        {
            float strike = Strike(time);
            Gfx.DrawGlow(sb, mid.X, mid.Y, r * 1.1f, BoltRed * ((0.3f + 0.6f * strike) * a));
            for (int i = 1; i + 1 < BoltPoints; i += 2) Gfx.DrawGlow(sb, bolt[i].X, bolt[i].Y, r * 0.35f, BoltRed * ((0.08f + 0.5f * strike) * a));
            Gfx.DrawGlow(sb, mid.X, mid.Y, r * 0.22f, BoltHot * ((0.4f + 0.6f * strike) * a));
        }

        //Strike : 1 as the bolt lands, falling to 0 over SigilBoltTime, every SigilBoltEvery seconds (no flash with LESS FLASHING)
        private static float Strike(float time)
        {
            float into = time % BattleRules.SigilBoltEvery;
            float s = Math.Max(burstNow, 1f - MathHelper.Clamp(into / BattleRules.SigilBoltTime, 0f, 1f));
            return Settings.ReduceFlashing ? s * BattleRules.LessFlash : s * s;
        }

        //Make Bolt : the jagged line from the window's top through the middle to the bottom left, a new shape each strike
        private static void MakeBolt(Rectangle box, Vector2 mid, float r, int strike)
        {
            Vector2 top = new Vector2(mid.X + r * 0.55f, box.Y - 4f);
            Vector2 foot = new Vector2(mid.X - r * 0.7f, box.Bottom + 4f);
            for (int i = 0; i < BoltPoints; i++)
            {
                float t = i / (float)(BoltPoints - 1);
                Vector2 p = Vector2.Lerp(top, foot, t);
                if (i > 0 && i < BoltPoints - 1) p.X += (Hash(strike, i) - 0.5f) * r * 0.55f;
                bolt[i] = p;
            }
            bolt[BoltPoints / 2] = Vector2.Lerp(bolt[BoltPoints / 2], mid, 0.7f);               // it passes through the middle
        }

        //RIFT : THE UNSIGNED. Two cuts side by side through the world, one blow of two blades (a slash, never a cross) :
        //Felix's behind, a black band whose edges burn purple and blue, Fanny's in front, a white line split into the rainbow
        //with prism cracks leaving it.
        private static Color FelixPurple { get { return Palette.SpotBrother; } }
        private static Color FelixBlue { get { return Palette.SpotBrotherDeep; } }

        private static void RiftLines(SpriteBatch sb, Rectangle box, Vector2 mid, float r, float time, float a)
        {
            Vector2 felixA = mid + new Vector2(-r * 0.95f, -r * 1.35f), felixB = mid + new Vector2(r * 1.45f, r * 1.05f);
            for (int i = 0; i < 7; i++)                                                    // the burn's flickering edge
            {
                float wave = (float)Math.Sin(time * 5f + i * 1.7f) * 0.5f + 0.5f;
                float w = 10f + 6f * wave;
                Vector2 off = new Vector2(1f, 1f) * ((i - 3) * 1.6f);
                Color edge = Color.Lerp(FelixPurple, FelixBlue, i / 6f) * ((0.25f + 0.2f * wave) * a);
                Gfx.Line(sb, felixA + off, felixB + off, edge, w * 0.25f);
            }
            Gfx.Line(sb, felixA, felixB, Palette.Void * a, 7f);                                     // the black of the cut
            Vector2 fannyA = mid + new Vector2(-r * 1.35f, -r * 0.95f), fannyB = mid + new Vector2(r * 1.05f, r * 1.25f);
            Vector2 side = Vector2.Normalize(new Vector2(-(fannyB - fannyA).Y, (fannyB - fannyA).X));
            for (int i = 0; i < 6; i++)                                                     // the rainbow split
            {
                Vector2 off = side * ((i - 2.5f) * 1.6f);
                Gfx.Line(sb, fannyA + off, fannyB + off, Palette.Prism(i / 6f + time * 0.05f) * (0.55f * a), 1.2f);
            }
            Gfx.Line(sb, fannyA, fannyB, Palette.Highlight * a, 1.5f);
            for (int i = 0; i < 9; i++)                                                     // prism cracks leaving the line
            {
                float t = 0.1f + i * 0.095f;
                Vector2 at = Vector2.Lerp(fannyA, fannyB, t);
                float turn = (Hash(i, 21) - 0.5f) * 1.4f;
                Vector2 way = Vector2.Transform(-side, Matrix.CreateRotationZ(turn));          // away from Felix's cut
                float reach = r * (0.18f + 0.22f * Hash(i, 23));
                Gfx.Line(sb, at, at + way * reach, Palette.Prism(i / 7f + time * 0.08f) * (0.7f * a), 1f);
                Gfx.Diamond(sb, at.X + way.X * reach, at.Y + way.Y * reach, 2.5f, Palette.Prism(i / 7f) * (0.8f * a));
            }
        }

        private static void RiftLight(SpriteBatch sb, Vector2 mid, float r, float time, float a)
        {
            float wave = (float)Math.Sin(time * 3f) * 0.5f + 0.5f;
            Gfx.DrawGlow(sb, mid.X + r * 0.35f, mid.Y - r * 0.2f, r * 0.8f, FelixPurple * ((0.16f + 0.1f * wave) * a));
            Gfx.DrawGlow(sb, mid.X + r * 0.1f, mid.Y + r * 0.35f, r * 0.8f, FelixBlue * ((0.14f + 0.1f * (1f - wave)) * a));
            Gfx.DrawGlow(sb, mid.X, mid.Y, r * 0.9f, Palette.Prism(time * 0.1f) * (0.2f * a));
            Gfx.DrawGlow(sb, mid.X, mid.Y, r * 0.2f, Palette.Highlight * (0.6f * a));
        }

        //HOLY : THE METRONOME. Countless small warm stars twinkling, and over the middle one bigger star with its long ray down,
        //the light that guides and shows the way.
        private static void HolyLines(SpriteBatch sb, Rectangle box, Vector2 mid, float r, float time, float a)
        {
            for (int i = 0; i < Stars; i++)
            {
                float x = box.X + 6f + Hash(i, 31) * (box.Width - 12f);
                float y = box.Y + 6f + Hash(i, 37) * (box.Height - 12f);
                float twinkle = 0.5f + 0.5f * (float)Math.Sin(time * (1.5f + 2.5f * Hash(i, 41)) + i);
                float size = 1f + 1.6f * Hash(i, 43);
                Gfx.Diamond(sb, x, y, size, Color.Lerp(Palette.SpotHoly, Palette.SpotHolyHot, twinkle) * ((0.25f + 0.6f * twinkle) * a));
            }
            Vector2 guide = new Vector2(mid.X, mid.Y - r * 0.35f);
            Gfx.Line(sb, guide, new Vector2(guide.X, box.Bottom), Palette.SpotHoly * (0.35f * a), 2f);   // its ray, the way down
            Gfx.Star(sb, guide.X, guide.Y, r * 0.5f, Palette.SpotHolyHot * a);
        }

        private static void HolyLight(SpriteBatch sb, Rectangle box, Vector2 mid, float r, float time, float a)
        {
            Vector2 guide = new Vector2(mid.X, mid.Y - r * 0.35f);
            float breath = 0.5f + 0.5f * (float)Math.Sin(time * 1.3f);
            Gfx.DrawGlowBox(sb, new Rectangle((int)(guide.X - r * 0.3f), (int)guide.Y, (int)(r * 0.6f), (int)(box.Bottom - guide.Y)), Palette.SpotHoly * (0.12f * a));
            Gfx.DrawGlow(sb, guide.X, guide.Y, r * (0.9f + 0.15f * breath), Palette.SpotHoly * (0.3f * a));
            Gfx.DrawGlow(sb, guide.X, guide.Y, r * 0.25f, Palette.SpotHolyHot * (0.7f * a));
        }
    }
}
