using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //NoteGlyph : the signs drawn on and around a note, in one place, so the duel and the
    //tutorial draw them exactly the same.
    //   Spark         a four point star, the second note of a pair : one more flick, any way
    //   Fermata Sign  the arch with a dot that music writes over a held note : hold still
    //   Pointer       the solid arrow head on a note's edge : the way to swing
    //   Arrow         a straight arrow, for the answer ring and the finale
    //   Roll Bar      the zigzag band behind a TREMOLO note : shake
    //   Hold Ribbon   the wide band behind a FERMATA note : hold
    //   Crown         the spikes turning round a loud f note
    //   Hit Point     the dark well and bright bar every note is answered on
    //   Timing Ring   the ring that closes on its mark exactly on the beat
    //The pause menu uses the fermata sign as well, both mean "hold".
    public static class NoteGlyph
    {
        //Crown : eight short spikes turning round a loud note
        public static void Crown(SpriteBatch sb, float cx, float cy, float radius, float time, float alpha)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * MathHelper.PiOver4 + time * 1.5f;
                Vector2 d = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                Vector2 c = new Vector2(cx, cy);
                Gfx.Line(sb, c + d * (radius + 3f), c + d * (radius + 9f), Palette.Highlight * alpha, 2f);
            }
        }

        //Hit Point : a calm dark disc at the head of the lane, like the drum target of Taiko no
        //Tatsujin, so the spot the eye has to watch has a steady ground of its own. A bright bar
        //stands in it, as tall as the lane. pulse (0 to 1) swells both on the beat.
        public const float WellRadius = 70f;
        public static void HitPoint(SpriteBatch sb, float cx, float cy, float laneHalf, float pulse)
        {
            Gfx.Circle(sb, cx, cy, WellRadius, Palette.Void * 0.82f);
            Gfx.CircleOutline(sb, cx, cy, WellRadius, Palette.Paper * (0.2f + pulse * 0.4f), 2f);

            float top = cy - laneHalf;
            if (pulse > 0f) Gfx.DrawGlow(sb, cx, cy, 40f + pulse * 30f, Palette.Highlight * (0.35f * pulse));
            Gfx.Rect(sb, cx - 3 - pulse * 2f, top - 8, 6 + pulse * 4f, laneHalf * 2f + 16f, Palette.Ink);
            Gfx.Rect(sb, cx - 1 - pulse, top - 8, 2 + pulse * 2f, laneHalf * 2f + 16f, Palette.Highlight);
        }

        //Timing Ring : the mark where the ring must be when the stroke lands, and the ring closing
        //on it. progress is 0 one beat before and 1 on the beat, after that it keeps closing a little.
        public static void TimingRing(SpriteBatch sb, float cx, float cy, float progress, float mark, float start, float swell, float alpha)
        {
            Gfx.CircleOutline(sb, cx, cy, mark + swell, Palette.Ink * alpha, 5f);
            Gfx.CircleOutline(sb, cx, cy, mark + swell, Palette.Highlight * alpha, 3f);

            float radius = start - (start - mark) * progress;
            if (radius < 8f) radius = 8f;
            Gfx.CircleOutline(sb, cx, cy, radius, Palette.Ink * alpha, 6f);
            Gfx.CircleOutline(sb, cx, cy, radius, Palette.Highlight * alpha, 3f);
        }

        //Spark : a thin four point star, built from rows (the tall point) and columns (the wide one)
        public static void Spark(SpriteBatch sb, float cx, float cy, float size, Color color)
        {
            for (int y = -(int)size; y <= (int)size; y++)
            {
                float half = (size - Math.Abs(y)) * 0.28f;
                Gfx.Rect(sb, cx - half, cy + y, half * 2f + 1f, 1, color);
            }
            for (int x = -(int)size; x <= (int)size; x++)
            {
                float half = (size - Math.Abs(x)) * 0.28f;
                Gfx.Rect(sb, cx + x, cy - half, 1, half * 2f + 1f, color);
            }
            Gfx.DrawGlow(sb, cx, cy, size * 0.8f, color);
        }

        //Fermata Sign : an arch with a dot under it, outlined dark so it reads on any ground
        public static void FermataSign(SpriteBatch sb, float cx, float cy, float size, Color color)
        {
            Gfx.Arc(sb, cx, cy, size, MathHelper.Pi, MathHelper.TwoPi, Palette.Void * (color.A / 255f), 6f);
            Gfx.Arc(sb, cx, cy, size, MathHelper.Pi, MathHelper.TwoPi, color, 3f);
            Gfx.Circle(sb, cx, cy - size * 0.28f, size * 0.2f, color);
        }

        //Note : a whole swipe note for the turn battle (Alpha 7). The ring is sized by its mark,
        //the mark sits in the middle and the pointer shows the way to swing. size is 0 p, 1 mf,
        //2 f, the order of Baton.SizeOf. grow scales the whole note (1 is the duel's size).
        //The duel and the tutorial still draw their own lane notes the same way.
        private static readonly string[] noteMark = { "p", "mf", "f" };
        private static readonly float[] noteRadius = { 23f, 27f, 31f };
        private static readonly float[] noteThick = { 1.5f, 2.5f, 4.5f };
        private static readonly float[] noteMarkScale = { 0.9f, 0.72f, 1.15f };

        public static void Note(SpriteBatch sb, SpriteFont font, float cx, float cy, int size, Flick way, float grow, float time, float alpha)
        {
            float radius = noteRadius[size] * grow;
            Hollow.Ring(sb, cx, cy, radius, noteThick[size] * grow, alpha);
            if (size == 2) Crown(sb, cx, cy, radius, time, alpha);
            Gfx.TextCentered(sb, font, noteMark[size], cx, cy - 2f * grow, Palette.Highlight * alpha, TextSize.Small * noteMarkScale[size] * grow);
            Pointer(sb, way, cx, cy, radius, alpha);
        }

        //Roll Note : the roll of a skill (TREMOLO), a ring with "tr" in it, as music writes a trill
        public static void RollNote(SpriteBatch sb, SpriteFont font, float cx, float cy, float grow, float alpha)
        {
            float radius = noteRadius[1] * grow;
            Hollow.Ring(sb, cx, cy, radius, noteThick[1] * grow, alpha);
            Gfx.TextCentered(sb, font, "tr", cx, cy - 2f * grow, Palette.Highlight * alpha, TextSize.Small * 0.8f * grow);
        }

        //Shape Note : a stroke of the conductor's 4/4 SIGNATURE, a ring with an arrow and no mark, any size will do
        public static void ShapeNote(SpriteBatch sb, float cx, float cy, Flick way, float grow, float alpha)
        {
            Hollow.Ring(sb, cx, cy, noteRadius[1] * grow, noteThick[2] * grow, alpha);
            Arrow(sb, way, cx, cy, 26f * grow, 12f * grow, Palette.Highlight * alpha, 3f * grow);
        }

        //Way : one step in the way a stroke goes, (0, 0) for none
        public static Vector2 Way(Flick dir)
        {
            if (dir == Flick.Up) return new Vector2(0f, -1f);
            if (dir == Flick.Down) return new Vector2(0f, 1f);
            if (dir == Flick.Left) return new Vector2(-1f, 0f);
            if (dir == Flick.Right) return new Vector2(1f, 0f);
            return Vector2.Zero;
        }

        //Pointer : a small solid arrow head just outside a note's ring, on the side the baton has
        //to travel toward. Outlined dark, so it reads over the bright stage as well.
        public static void Pointer(SpriteBatch sb, Flick dir, float cx, float cy, float radius, float alpha)
        {
            Vector2 c = new Vector2(cx, cy) + Way(dir) * (radius + 14f);
            Head(sb, dir, c, 15f, Palette.Ink * alpha);
            Head(sb, dir, c, 10f, Palette.Highlight * alpha);
        }

        //Arrow : a straight arrow of the given length, centred on (cx, cy)
        public static void Arrow(SpriteBatch sb, Flick dir, float cx, float cy, float length, float head, Color color, float thickness)
        {
            Vector2 d = Way(dir);
            Vector2 centre = new Vector2(cx, cy);
            Vector2 from = centre - d * (length / 2f);
            Vector2 tip = centre + d * (length / 2f);
            Gfx.Line(sb, from, tip - d * head, color, thickness);

            //Head : the triangle helpers are drawn around their middle, so step back half a head
            Head(sb, dir, tip - d * (head / 2f), head, color);
        }

        private static void Head(SpriteBatch sb, Flick dir, Vector2 c, float size, Color color)
        {
            if (dir == Flick.Up) Gfx.Triangle(sb, c.X, c.Y, size, true, color);
            else if (dir == Flick.Down) Gfx.Triangle(sb, c.X, c.Y, size, false, color);
            else Gfx.Arrow(sb, c.X, c.Y, size, dir == Flick.Right, color);
        }

        //Roll Bar : the body of TACET's roll, like the drum roll bar of Taiko no Tatsujin. A pale
        //band from the head of the roll back up the lane to where it ends, with a zigzag in it.
        public static void RollBar(SpriteBatch sb, float fromX, float toX, float y)
        {
            if (toX <= fromX + 4f) return;
            Gfx.Rect(sb, fromX, y - 13f, toX - fromX, 26f, Palette.Paper * 0.22f);
            Gfx.Rect(sb, fromX, y - 13f, toX - fromX, 2f, Palette.Paper * 0.7f);
            Gfx.Rect(sb, fromX, y + 11f, toX - fromX, 2f, Palette.Paper * 0.7f);

            float step = 10f;
            float x = fromX + 26f;
            bool up = true;
            while (x + step < toX)
            {
                Gfx.Line(sb, x, y + (up ? -6f : 6f), x + step, y + (up ? 6f : -6f), Palette.Highlight * 0.9f, 2f);
                x += step;
                up = !up;
            }
            Gfx.Rect(sb, toX - 2f, y - 16f, 4f, 32f, Palette.Highlight);
        }

        //Hold Ribbon : the body of a fermata in the lane, a wide pale band like the hold notes of
        //Project Sekai, from the note back up the lane to where the hold ends
        public static void HoldRibbon(SpriteBatch sb, float fromX, float toX, float y)
        {
            if (toX <= fromX + 4f) return;
            Gfx.Rect(sb, fromX, y - 15f, toX - fromX, 30f, Palette.Paper * 0.28f);
            Gfx.Rect(sb, fromX, y - 15f, toX - fromX, 2f, Palette.Paper * 0.8f);
            Gfx.Rect(sb, fromX, y + 13f, toX - fromX, 2f, Palette.Paper * 0.8f);
            Gfx.Rect(sb, toX - 3f, y - 18f, 6f, 36f, Palette.Highlight);
        }
    }
}
