using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //Baton : the conductor's stick on screen, and the ruler that measures a stroke.
    //The duel and the tutorial both own one, so the stick looks and swings the same in both.
    //It only draws and swings. Reading strokes is GestureReader's job, judging them the page's.
    //
    //ARTWORK : Content/Art/Baton/baton.png, drawn standing up from its bottom middle and turned
    //to the stick's angle (see ArtBank). Until then it is a plain rectangle.
    //CONDUCTOR'S BATON (block D, 2 Nov, the user 13.121 : "a lightning baton, a crystal baton and a gold baton sparkling with
    //stars") : the page tells the stick whose it is (Look, the conductor's weather) and it is drawn as theirs :
    //   STORM  THE UNHEARING : a bolt of lightning, jagged, flickering, small arcs crawling off its tip (blue, red once his
    //          FINALE has grown into STORM : Level)
    //   PRISM  FANNY : a long crystal, clear with white edges, the rainbow along one edge, a glint running up it
    //   HOLY   THE METRONOME : a stick of gold with a star at its tip, twinkling, gold dust falling off its swing
    //The swing's tail takes the same colour. None keeps the plain stick (the old duel, the tutorial).
    public class Baton
    {
        public AuraKind Look = AuraKind.None;
        public int Level;                       // the FINALE's level (STORM turns the lightning red)
        private float clock;                    // the looks' flicker and glints run on it
        private static readonly Vector2[] crystal = new Vector2[4];
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
            clock += dt;
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
                    Gfx.Line(sb, a, b, TailColor(i) * (0.7f * fresh), 2f + fresh * 5f);
                    if (Look == AuraKind.Holy && fresh > 0.3f && Hash(i, (int)(clock * 12f)) > 0.6f)   // gold dust off the swing
                        Gfx.Diamond(sb, b.X + (Hash(i, 3) - 0.5f) * 14f, b.Y + (Hash(i, 5) - 0.5f) * 14f + (1f - fresh) * 10f, 2f, Palette.SpotHolyHot * fresh);
                }
            }

            if (Look != AuraKind.None && ArtBank.Baton == null)
            {
                DrawOwn(sb, grip, along, held);
                return;
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

        //Tail Color : the swing's tail in the conductor's colour (the rainbow along it for the crystal)
        private Color TailColor(int i)
        {
            if (Look == AuraKind.Storm) return Level >= 1 ? Palette.StormRedHot : Palette.LightningBlueHot;
            if (Look == AuraKind.Prism) return Color.Lerp(Color.White, Palette.Prism(i * 0.07f + clock * 0.3f), 0.7f);
            if (Look == AuraKind.Holy) return Palette.SpotHolyHot;
            return Palette.Highlight;
        }

        //Draw Own : the conductor's own baton (CONDUCTOR'S BATON), held at the grip and pointing along. Let go it is dimmer.
        private void DrawOwn(SpriteBatch sb, Vector2 grip, Vector2 along, bool held)
        {
            Vector2 side = new Vector2(-along.Y, along.X);
            Vector2 tip = grip + along * Length;
            float on = held ? 1f : 0.6f;

            if (Look == AuraKind.Storm)
            {
                //A bolt : the stick's line broken into a zigzag that jumps to a new shape a few times a second
                Color tint = Level >= 1 ? Palette.StormRed : Palette.LightningBlue;
                Color hot = Level >= 1 ? Palette.StormRedHot : Palette.LightningBlueHot;
                int shape = (int)(clock * 9f);
                Vector2 last = grip;
                for (int k = 1; k <= 6; k++)
                {
                    float t = k / 6f;
                    float off = k < 6 ? (Hash(k, shape) - 0.5f) * 9f + (k % 2 == 0 ? 3f : -3f) : 0f;
                    Vector2 p = grip + along * (Length * t) + side * off;
                    Gfx.Line(sb, last, p, tint * (0.35f * on), 9f);
                    Gfx.Line(sb, last, p, hot * on, 3f);
                    Gfx.Line(sb, last, p, Color.White * on, 1f);
                    last = p;
                }
                if (held)                                                                    // small arcs crawling off the tip
                    for (int a = 0; a < 2; a++)
                    {
                        Vector2 p = tip;
                        for (int k = 0; k < 3; k++)
                        {
                            float angle = Hash(a * 7 + k, (int)(clock * 20f)) * MathHelper.TwoPi;
                            Vector2 q = p + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 8f;
                            Gfx.Line(sb, p, q, hot * 0.8f, 1.5f);
                            p = q;
                        }
                    }
                Gfx.DrawGlow(sb, tip.X, tip.Y, 26f + 30f * Glow, tint * ((0.35f + 0.5f * Glow) * on));
                return;
            }

            if (Look == AuraKind.Prism)
            {
                //A crystal : a long narrow diamond, widest a third of the way up, clear with white edges, the rainbow along one
                Vector2 wide = grip + along * (Length * 0.3f);
                crystal[0] = grip;
                crystal[1] = wide + side * 6f;
                crystal[2] = tip;
                crystal[3] = wide - side * 6f;
                Gfx.Quad(sb, crystal, Color.Lerp(Palette.SpotPrism, Palette.Prism(clock * 0.2f), 0.25f) * (0.35f * on));
                for (int k = 0; k < 4; k++) Gfx.Line(sb, crystal[k], crystal[(k + 1) % 4], Color.White * (0.85f * on), 1.5f);
                for (int c = 0; c < 3; c++)                                                  // the rainbow along the upper edge
                {
                    Vector2 off = side * (1.5f + c * 1.5f);
                    Gfx.Line(sb, crystal[1] + off, crystal[2] + off * 0.3f, Palette.Prism(c / 3f + clock * 0.25f) * (0.8f * on), 1f);
                }
                Gfx.Line(sb, grip, tip, Color.White * (0.5f * on), 1f);                   // the crystal's ridge
                float run = (clock * 0.8f) % 1.4f;                                           // a glint running up it now and then
                if (run < 1f)
                {
                    Vector2 g = grip + along * (Length * run);
                    Gfx.DrawGlow(sb, g.X, g.Y, 14f, Color.White * (0.6f * on));
                    Gfx.Diamond(sb, g.X, g.Y, 2.5f, Color.White * on);
                }
                Gfx.DrawGlow(sb, tip.X, tip.Y, 22f + 30f * Glow, Palette.Prism(clock * 0.3f) * ((0.3f + 0.5f * Glow) * on));
                return;
            }

            //Gold : a stick of gold with an ink edge, and a star at its tip that twinkles (and flares with a stroke)
            Gfx.Line(sb, grip, tip, Palette.Ink * on, Width + 2f);
            Gfx.Line(sb, grip, tip, Palette.SpotHoly * on, Width - 3f);
            Gfx.Line(sb, grip + side * 1.5f, tip + side * 1.5f, Palette.SpotHolyHot * (0.8f * on), 1.5f);
            float twinkle = 0.75f + 0.25f * (float)Math.Sin(clock * 6f);
            float star = (7f + 5f * Glow) * twinkle;
            Gfx.DrawGlow(sb, tip.X, tip.Y, 26f + 30f * Glow, Palette.SpotHoly * ((0.4f + 0.5f * Glow) * on));
            Gfx.Star(sb, tip.X, tip.Y, star, Palette.SpotHolyHot * on);
            Gfx.Rect(sb, tip.X - 1f, tip.Y - 1f, 2f, 2f, Color.White * on);
        }

        //Hash : a steady random 0..1 from two whole numbers (the same trick as TacetField)
        private static float Hash(int a, int b)
        {
            float h = (float)Math.Sin(a * 12.9898f + b * 78.233f) * 43758.547f;
            return h - (float)Math.Floor(h);
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
