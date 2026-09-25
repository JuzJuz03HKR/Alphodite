using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Battle;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //DuelScreen.Baton : reading the baton and drawing it.
    //JudgeStroke turns a finished stroke into an order and a grade, the baton swings like a
    //sprung blade, and the stroke guide shows how big the stroke is while it is being made.
    public partial class DuelScreen
    {
        //Stroke Judge : the one place where the baton becomes an order to the band
        private void JudgeStroke(int b, float target)
        {
            int k = b % 4;
            strokeGlow = 1f;

            //Stroke Size : small eases, middle plays, big boosts
            Choice choice = sizeChoice[SizeOf(gesture.Length)];
            if (battle.ChoicesLocked) choice = Choice.Boost;                   // THE METRONOME : every stroke is a BOOST

            //Timing Grade : judged where the baton stopped, against the beat
            float now = StrokeClock();
            float off = Math.Abs(now - target);
            Grade grade = Grade.Miss;
            if (off <= battle.GoodWindow) grade = Grade.Good;
            if (off <= battle.PerfectWindowAt(b)) grade = Grade.Perfect;

            //COUNTS ALOUD : a PERFECT that only her wider window allowed gets her name over it
            if (grade == Grade.Perfect && off > battle.PerfectWindow)
            {
                int anna = TraitSeat(MusicianTrait.KeepsCount, b);
                if (anna >= 0) PopTrait(anna);
            }

            //FASHIONABLY LATE : a late stroke on her beat is forgiven as GOOD
            if (grade == Grade.Miss && now > target && battle.LateForgivenAt(b))
            {
                grade = Grade.Good;
                int iris = TraitSeat(MusicianTrait.Forgiven, b);
                if (iris >= 0) PopTrait(iris);
            }

            //Wrong Way : the stroke has to follow the beat pattern, any other way is a MISS
            bool rightWay = gesture.Direction == pattern[k];
            if (!rightWay) grade = Grade.Miss;

            //Signature : this stroke is a PERFECT BOOST whatever its shape, with the bonus on top
            bool signature = battle.SignatureArmed;
            if (signature)
            {
                battle.SignatureArmed = false;
                choice = Choice.Boost;
                grade = Grade.Perfect;
                battle.SignatureNext = true;
            }

            //Hand And Stick : the hand picture changes pose, the stick gets a flick
            hand.Play(signature ? HandPose.Signature : PoseFor(gesture.Direction));
            WhipBaton(gesture.Direction);

            //Slash : a big stroke the right way leaves a blade of light along its path, brighter
            //for a PERFECT. The band's own blow, the answer to TACET's.
            if (choice == Choice.Boost && (rightWay || signature))
                effects.SpawnSlash(gesture.From, gesture.To, grade == Grade.Perfect ? 1f : 0.55f);

            //Judgement : ONE word at the hit point, how well it landed and what the band was told,
            //and the shape of the order opening out from the hit point
            string word = (!rightWay && !signature) ? "WRONG WAY" : judgeText[(int)grade * 3 + (int)choice];
            ShowJudge(word, 0.55f);
            if (rightWay && !signature) ShowTiming(grade, now, target);

            if (choice == Choice.Boost) SoundBank.Play(Sfx.QteBoost);
            if (choice == Choice.Normal) SoundBank.Play(Sfx.QteNormal);
            if (choice == Choice.Ease) SoundBank.Play(Sfx.QteEase);
            if (grade == Grade.Perfect) SoundBank.Play(Sfx.QtePerfect);
            if (grade == Grade.Miss) SoundBank.Play(Sfx.QteMiss);

            //Fermata : a stroke that lands on TACET's held note opens the hold instead of settling
            //the beat at once. A missed one settles it straight away, there is nothing to hold.
            if (battle.IsFermata(b) && grade != Grade.Miss && !battle.Finished)
            {
                StartHold(choice, grade, signature);
                return;
            }

            ResolveAnswer(b, choice, grade, signature);
        }

        //EARLY / LATE : like the FAST and LATE marks of Project Sekai. A stroke that counted but
        //was not PERFECT says which side of the beat it fell on, so the player knows what to fix
        //(always LATE on a good mouse usually means the STROKE TIMING setting should move).
        private void ShowTiming(Grade grade, float now, float target)
        {
            if (grade == Grade.Good || grade == Grade.Miss)
                judgeTiming = now < target ? -1 : 1;
        }

        //Size Of : 0 small, 1 middle, 2 big
        private int SizeOf(float length)
        {
            if (length < GestureReader.MiddleLength) return 0;
            if (length < GestureReader.BigLength) return 1;
            return 2;
        }

        //Pose For : which hand picture belongs to which stroke
        private HandPose PoseFor(Flick flick)
        {
            if (flick == Flick.Down) return HandPose.Down;
            if (flick == Flick.Left) return HandPose.Left;
            if (flick == Flick.Right) return HandPose.Right;
            if (flick == Flick.Up) return HandPose.Up;
            return HandPose.Ready;
        }

        //Baton Update : the stick behaves like a spring. It wants to point at its rest angle, plus
        //a lean against the way the hand is moving, so it trails behind a swing, overshoots a
        //little and settles, the way a blade swings when you whip it about.
        //ADVANCED PART : a damped spring, in three lines. Pull the spin toward the target angle,
        //take some of the spin away as friction, then turn the stick by the spin.
        private void UpdateBaton(float dt)
        {
            if (dt <= 0f) return;

            Vector2 now = Input.MousePos;
            Vector2 move = (now - batonLast) / dt;
            batonLast = now;
            batonVelocity = batonVelocity * 0.7f + move * 0.3f;

            float target = BatonLowered;
            if (gesture.Held) target = BatonRest + MathHelper.Clamp(-batonVelocity.X * 0.0016f, -1.1f, 1.1f);

            batonSpin += (target - batonAngle) * 90f * dt;         // pull toward the target
            batonSpin *= Math.Max(0f, 1f - 9f * dt);               // friction
            batonAngle += batonSpin * dt;
        }

        //Baton Whip : a stroke gives the stick an extra flick, against the way it went
        private void WhipBaton(Flick way)
        {
            if (way == Flick.Right) batonSpin -= 9f;
            else if (way == Flick.Left) batonSpin += 9f;
            else batonSpin += (way == Flick.Up ? -5f : 5f);
        }

        //Baton : the stick is held where the pointer is and swings behind it. Held, it is raised
        //and bright with a tail behind it. Let go, it hangs down, dim, and nothing it does counts.
        //ARTWORK : Content/Art/Baton/baton.png, drawn standing up from its bottom middle and
        //turned to the stick's angle (see ArtBank). Until then it is a plain rectangle.
        private void DrawBaton(SpriteBatch sb)
        {
            Vector2 grip = Input.MousePos;
            Vector2 along = new Vector2((float)Math.Cos(batonAngle), (float)Math.Sin(batonAngle));
            Vector2 tip = grip + along * BatonLength;
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
                float scale = BatonLength / art.Height;
                sb.Draw(art, grip, null, Color.White * (held ? 1f : 0.6f), batonAngle + MathHelper.PiOver2,
                        origin, scale, SpriteEffects.None, 0f);
            }
            else
            {
                Gfx.Line(sb, grip, tip, Palette.Ink * (held ? 1f : 0.6f), BatonWidth + 4f);
                Gfx.Line(sb, grip, tip, held ? Palette.Paper : Palette.PaperDim, BatonWidth);
            }

            if (strokeGlow > 0f) Gfx.DrawGlow(sb, tip.X, tip.Y, 40f * strokeGlow, Palette.Highlight * (0.6f * strokeGlow));
        }

        //Stroke Guide : a ruler coming off the baton the way this beat goes, with marks where a
        //stroke turns from EASE to PLAY and from PLAY to BOOST. Once a stroke starts the ruler
        //stays where it began and fills up as the baton travels, so the size can be judged on
        //the way instead of guessed.
        private void DrawStrokeGuide(SpriteBatch sb)
        {
            if (AnswerProgress() < 0f || cutIn > 0f || rolling) return;
            if (!gesture.Held) return;                  // only while the baton is raised

            //Fermata : the baton should stay where it is, so the sign sits beside it
            if (holding)
            {
                Vector2 at = Input.MousePos + new Vector2(30f, -30f);
                Gfx.Circle(sb, at.X, at.Y - 4f, 20f, Palette.Void * 0.85f);
                NoteGlyph.FermataSign(sb, at.X, at.Y + 4f, 13f, Palette.Highlight);
                return;
            }

            //Spark : the second note of a pair takes any way, so a spark sits by the baton instead
            if (onGrace)
            {
                Vector2 at = Input.MousePos + new Vector2(26f, -26f);
                Gfx.Circle(sb, at.X, at.Y, 18f, Palette.Void * 0.85f);
                NoteGlyph.Spark(sb, at.X, at.Y, 13f, Palette.Highlight);
                return;
            }

            Vector2 d = DirVector(WantedWay());
            Vector2 anchor = gesture.InStroke ? gesture.StrokeStart : Input.MousePos;
            float full = GestureReader.BigLength + 50f;
            float fade = 1f;

            //Progress : how far the stroke has come the way it should be going
            float along = Vector2.Dot(gesture.LiveVector, d);
            if (along < 0f) along = 0f;
            if (along > full) along = full;

            Vector2 end = anchor + d * full;
            Gfx.Line(sb, anchor, end, Palette.Ink * (0.35f * fade), 6f);
            Gfx.Line(sb, anchor, end, Palette.Paper * (0.55f * fade), 2f);
            if (along > 0f) Gfx.Line(sb, anchor, anchor + d * along, Palette.Highlight, 4f);

            //Size Zones : only where the size gives an order. The finale only needs the right
            //way at the right time.
            if (phase == Phase.Finale) return;

            DrawGuideMark(sb, anchor + d * GestureReader.MiddleLength, d, along >= GestureReader.MiddleLength);
            DrawGuideMark(sb, anchor + d * GestureReader.BigLength, d, along >= GestureReader.BigLength);

            //Zone Words : beside the ruler, in the middle of each zone. The zone the stroke has
            //reached is bright, the others faint.
            int zone = gesture.InStroke ? SizeOf(along) : -1;
            Vector2 side = new Vector2(-d.Y, d.X) * 18f;
            DrawGuideWord(sb, sizeWord[0], anchor + d * (GestureReader.MiddleLength * 0.5f) + side, zone == 0);
            DrawGuideWord(sb, sizeWord[1], anchor + d * ((GestureReader.MiddleLength + GestureReader.BigLength) * 0.5f) + side, zone == 1);
            DrawGuideWord(sb, sizeWord[2], anchor + d * (GestureReader.BigLength + 30f) + side, zone == 2);
        }

        private void DrawGuideMark(SpriteBatch sb, Vector2 at, Vector2 d, bool passed)
        {
            Vector2 side = new Vector2(-d.Y, d.X) * 10f;
            Gfx.Line(sb, at - side, at + side, Palette.Ink * 0.5f, 5f);
            Gfx.Line(sb, at - side, at + side, passed ? Palette.Highlight : Palette.Paper * 0.7f, 2f);
        }

        private void DrawGuideWord(SpriteBatch sb, string word, Vector2 at, bool reached)
        {
            float a = reached ? 1f : 0.55f;
            Gfx.TextSpacedCentered(sb, Game.Font, word, at.X + 1, at.Y - 6, Color.Black * (0.6f * a), TextSize.Tiny, 1.5f);
            Gfx.TextSpacedCentered(sb, Game.Font, word, at.X, at.Y - 7, Palette.Highlight * a, TextSize.Tiny, 1.5f);
        }

        //Direction : a straight arrow of the given length, centred on (cx, cy)
        private void DrawDirection(SpriteBatch sb, Flick dir, float cx, float cy, float length, float head, Color color, float thickness)
        {
            Vector2 d = DirVector(dir);
            Vector2 centre = new Vector2(cx, cy);
            Vector2 from = centre - d * (length / 2f);
            Vector2 tip = centre + d * (length / 2f);
            Gfx.Line(sb, from, tip - d * head, color, thickness);

            //Head : the triangle helpers are drawn around their middle, so step back half a head
            Vector2 c = tip - d * (head / 2f);
            if (dir == Flick.Up) Gfx.Triangle(sb, c.X, c.Y, head, true, color);
            else if (dir == Flick.Down) Gfx.Triangle(sb, c.X, c.Y, head, false, color);
            else Gfx.Arrow(sb, c.X, c.Y, head, dir == Flick.Right, color);
        }

        //Dir Vector : one step in the way a stroke goes
        private static Vector2 DirVector(Flick dir)
        {
            if (dir == Flick.Up) return new Vector2(0f, -1f);
            if (dir == Flick.Down) return new Vector2(0f, 1f);
            if (dir == Flick.Left) return new Vector2(-1f, 0f);
            if (dir == Flick.Right) return new Vector2(1f, 0f);
            return Vector2.Zero;
        }
    }
}
