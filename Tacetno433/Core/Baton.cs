using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //Baton : the conductor's stick on screen, and the ruler that measures a stroke.
    //The duel and the tutorial both own one, so the stick looks and swings the same in both.
    //It only draws and swings. Reading strokes is GestureReader's job, judging them the page's.
    //
    //ARTWORK : Content/Art/Baton/baton.png, drawn standing up from its bottom middle and turned
    //to the stick's angle (see ArtBank). Until then it is a plain rectangle.
    public class Baton
    {
        public const float Length = 118f;
        public const float Width = 10f;
        private const float Rest = -1.3f;       // radians : 0 points right, minus a half turn points up
        private const float Lowered = 0.9f;     // hanging down to the right while the button is up

        //Ruler : how long the size ruler is, a little past the big mark. The zones carry the same
        //marks as TACET's notes, so an f note is answered by reaching the f (round 12)
        public const float RulerLength = GestureReader.BigLength + 50f;
        private static string[] sizeWords = { "p", "mf", "f" };

        private float angle = Rest;
        private float spin;                     // how fast the angle is changing
        private Vector2 last;

        //Velocity : how fast the pointer moves, smoothed, in pixels per second.
        //The fermata reads it to tell whether the hand is holding still.
        public Vector2 Velocity;

        //Glow : lights the tip for a moment after a stroke, fades by itself
        public float Glow;

        //Reset : start from where the pointer is now, so the first frame does not jump
        public void Reset(Vector2 at)
        {
            last = at;
            Velocity = Vector2.Zero;
        }

        //Baton Update : the stick behaves like a spring. It wants to point at its rest angle, plus
        //a lean against the way the hand is moving, so it trails behind a swing, overshoots a
        //little and settles, the way a blade swings when you whip it about.
        //ADVANCED PART : a damped spring, in three lines. Pull the spin toward the target angle,
        //take some of the spin away as friction, then turn the stick by the spin.
        public void Update(float dt, bool held)
        {
            if (dt <= 0f) return;
            Glow = Math.Max(0f, Glow - dt * 2.5f);

            Vector2 now = Input.MousePos;
            Vector2 move = (now - last) / dt;
            last = now;
            Velocity = Velocity * 0.7f + move * 0.3f;

            float target = Lowered;
            if (held) target = Rest + MathHelper.Clamp(-Velocity.X * 0.0016f, -1.1f, 1.1f);

            spin += (target - angle) * 90f * dt;           // pull toward the target
            spin *= Math.Max(0f, 1f - 9f * dt);            // friction
            angle += spin * dt;
        }

        //Baton Whip : a stroke gives the stick an extra flick against the way it went, and a glow
        public void Whip(Flick way)
        {
            Glow = 1f;
            if (way == Flick.Right) spin -= 9f;
            else if (way == Flick.Left) spin += 9f;
            else spin += (way == Flick.Up ? -5f : 5f);
        }

        //Baton Draw : the stick is held where the pointer is and swings behind it. Held, it is
        //raised and bright with a tail behind it. Let go, it hangs down, dim, and nothing counts.
        public void Draw(SpriteBatch sb, GestureReader gesture)
        {
            Vector2 grip = Input.MousePos;
            Vector2 along = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            Vector2 tip = grip + along * Length;
            bool held = gesture.Held;

            //Tail : the last moment of movement, only while the baton is raised
            if (held)
            {
                for (int i = 1; i < gesture.TrailCount; i++)
                {
                    Vector2 a = gesture.TrailPoint(i - 1);
                    Vector2 b = gesture.TrailPoint(i);
                    float fresh = 1f - gesture.TrailAge(i);
                    if (fresh <= 0.06f) continue;
                    Gfx.Line(sb, a, b, Palette.Ink * (0.25f * fresh), 4f + fresh * 8f);
                    Gfx.Line(sb, a, b, Palette.Highlight * (0.7f * fresh), 2f + fresh * 5f);
                }
            }

            Texture2D art = ArtBank.Baton;
            if (art != null)
            {
                Vector2 origin = new Vector2(art.Width / 2f, art.Height);
                float scale = Length / art.Height;
                sb.Draw(art, grip, null, Color.White * (held ? 1f : 0.6f), angle + MathHelper.PiOver2,
                        origin, scale, SpriteEffects.None, 0f);
            }
            else
            {
                Gfx.Line(sb, grip, tip, Palette.Ink * (held ? 1f : 0.6f), Width + 4f);
                Gfx.Line(sb, grip, tip, held ? Palette.Paper : Palette.PaperDim, Width);
            }

            if (Glow > 0f) Gfx.DrawGlow(sb, tip.X, tip.Y, 40f * Glow, Palette.Highlight * (0.6f * Glow));
        }

        //Size Of : 0 small (p, the p players), 1 middle (mf, the mf players too), 2 big (f, the whole band)
        public static int SizeOf(float length)
        {
            if (length < GestureReader.MiddleLength) return 0;
            if (length < GestureReader.BigLength) return 1;
            return 2;
        }

        //Ruler : a line coming off anchor the way d points, filled as far as the stroke has come
        //(along). With zones on, marks show where p turns to mf and mf to f, and the mark of the
        //zone reached (zone, -1 for none yet) is bright. Draw it only while held.
        public static void DrawRuler(SpriteBatch sb, Vector2 anchor, Vector2 d, float along, int zone, bool zones)
        {
            if (along < 0f) along = 0f;
            if (along > RulerLength) along = RulerLength;

            Vector2 end = anchor + d * RulerLength;
            Gfx.Line(sb, anchor, end, Palette.Ink * 0.35f, 6f);
            Gfx.Line(sb, anchor, end, Palette.Paper * 0.55f, 2f);
            if (along > 0f) Gfx.Line(sb, anchor, anchor + d * along, Palette.Highlight, 4f);
            if (!zones) return;

            RulerMark(sb, anchor + d * GestureReader.MiddleLength, d, along >= GestureReader.MiddleLength);
            RulerMark(sb, anchor + d * GestureReader.BigLength, d, along >= GestureReader.BigLength);

            //Zone Words : beside the ruler, in the middle of each zone
            Vector2 side = new Vector2(-d.Y, d.X) * 18f;
            RulerWord(sb, sizeWords[0], anchor + d * (GestureReader.MiddleLength * 0.5f) + side, zone == 0);
            RulerWord(sb, sizeWords[1], anchor + d * ((GestureReader.MiddleLength + GestureReader.BigLength) * 0.5f) + side, zone == 1);
            RulerWord(sb, sizeWords[2], anchor + d * (GestureReader.BigLength + 30f) + side, zone == 2);
        }

        private static void RulerMark(SpriteBatch sb, Vector2 at, Vector2 d, bool passed)
        {
            Vector2 side = new Vector2(-d.Y, d.X) * 10f;
            Gfx.Line(sb, at - side, at + side, Palette.Ink * 0.5f, 5f);
            Gfx.Line(sb, at - side, at + side, passed ? Palette.Highlight : Palette.Paper * 0.7f, 2f);
        }

        //Ruler Word : the mark in the same italic letters as the notes, bigger once it is reached
        private static void RulerWord(SpriteBatch sb, string word, Vector2 at, bool reached)
        {
            float a = reached ? 1f : 0.55f;
            float size = TextSize.Small * (reached ? 0.8f : 0.62f);
            Gfx.TextCentered(sb, Ui.BigFont, word, at.X + 1, at.Y + 1, Color.Black * (0.6f * a), size);
            Gfx.TextCentered(sb, Ui.BigFont, word, at.X, at.Y, Palette.Highlight * a, size);
        }
    }
}
