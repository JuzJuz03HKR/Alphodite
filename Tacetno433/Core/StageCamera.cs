using System;
using Microsoft.Xna.Framework;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //StageCamera (Beta1, 29 Oct) : the camera that moves WITH THE BATON. The user : "zoom in, zoom out, the screen
    //shakes with what we do, linked to the character", like Muse Dash when it asks for fast taps and Chaos Zero
    //Nightmare when a character attacks (docs/BETA1_DIRECTION.md 10).
    //It only moves the finished picture of the world (PostChain.ShowFinal draws FINAL through it). The ring, the notes
    //and the words on top never move, so the timing can always be read.
    //   ZOOM    steps in with every note of a phrase that lands, punches in on a hit, eases back after the phrase
    //   KICK    a swipe slides the world the way the baton went and leans it a little that way (TILT), then it springs back
    //   SHAKE   a short tremble, strongest right after a hit and fading fast (TRAUMA : the shake is trauma squared,
    //           so small hits barely move it and big ones really do)
    //The numbers live in BattleRules (Camera Feel). The battle page owns one (Screens/BattleScreen.Stage.cs).
    public class StageCamera
    {
        //Now : where the camera is this frame
        public Vector2 Focus;              // the point it pushes in toward (game pixels)
        private float zoom = 1f;           // the steady zoom (1 = none)
        private float zoomSpeed;
        private float punch;               // extra zoom from a hit, dying away fast
        private Vector2 pan;               // the slide of a KICK (game pixels)
        private Vector2 panSpeed;
        private float tilt;                // the lean of a KICK (radians)
        private float tiltSpeed;
        private float trauma;              // SHAKE 0..1
        private float clock;
        private float letGo;               // HOLD FOR : seconds until it lets go by itself (0 = held until Release)

        //Wanted : where it is heading
        private float zoomWant = 1f;
        private Vector2 focusWant;

        //Screen : the picture it frames, the page's game pixels
        private static readonly Vector2 middle = new Vector2(TacetGame.ScreenW / 2f, TacetGame.ScreenH / 2f);

        public StageCamera()
        {
            Focus = middle;
            focusWant = middle;
        }

        //Reset : straight back to no move at all (a new fight)
        public void Reset()
        {
            Focus = middle;
            focusWant = middle;
            zoom = 1f;
            zoomWant = 1f;
            zoomSpeed = 0f;
            punch = 0f;
            letGo = 0f;
            pan = Vector2.Zero;
            panSpeed = Vector2.Zero;
            tilt = 0f;
            tiltSpeed = 0f;
            trauma = 0f;
        }

        //Hold : while a phrase is played the camera leans in toward at, zoomed by amount (0 = none). The point is pulled
        //part of the way back to the middle (CamFocusShare), so the action stays in frame and the push stays gentle.
        public void Hold(Vector2 at, float amount)
        {
            focusWant = Vector2.Lerp(middle, at, BattleRules.CamFocusShare);
            zoomWant = 1f + MathHelper.Clamp(amount, 0f, BattleRules.CamZoomMost);
        }

        //Hold For : the same, for a moment only (a COUNTER, the SIGNATURE's strike), then it lets go by itself
        public void HoldFor(Vector2 at, float amount, float seconds)
        {
            Hold(at, amount);
            letGo = seconds;
        }

        //Release : the phrase is over, everything eases back
        public void Release()
        {
            zoomWant = 1f;
            focusWant = middle;
            letGo = 0f;
        }

        //Punch : a hit pushes in for a moment
        public void Punch(float amount)
        {
            punch = Math.Max(punch, amount);
        }

        //Kick : the baton swung this way (a unit step, Vector2.Zero for none). The world slides with it and leans,
        //power 0..1.5 scales both.
        //ADVANCED PART : the kick is a push on the spring's SPEED, not on where it is. The spring then carries it out
        //and back by itself, so it never jumps. A critically damped spring pushed with speed v goes furthest
        //(v / stiffness / e) and that is CamKick here, which is why the push is CamKick x stiffness x e.
        public void Kick(Vector2 way, float power)
        {
            float push = BattleRules.CamSpring * (float)Math.E * power;
            panSpeed += way * (BattleRules.CamKick * push);
            tiltSpeed += way.X * BattleRules.CamTilt * push;             // a swing to the right leans the world clockwise
        }

        //Shake : add trauma (it is capped at 1)
        public void Shake(float amount)
        {
            trauma = Math.Min(1f, trauma + amount);
        }

        //Camera Update : real time, it moves on through a hit stop (the punch is the feedback of the freeze)
        public void Update(float dt)
        {
            if (dt <= 0f) return;
            dt = Math.Min(dt, 1f / 30f);                                  // a slow frame must not throw the springs
            clock += dt;
            if (letGo > 0f)
            {
                letGo -= dt;
                if (letGo <= 0f) Release();
            }

            float k = BattleRules.CamSpring;
            Spring(ref zoom, ref zoomSpeed, zoomWant, k * 0.6f, dt);     // the zoom follows a softer spring than the kick
            Spring(ref pan.X, ref panSpeed.X, 0f, k, dt);
            Spring(ref pan.Y, ref panSpeed.Y, 0f, k, dt);
            Spring(ref tilt, ref tiltSpeed, 0f, k, dt);
            Focus = Vector2.Lerp(Focus, focusWant, 1f - (float)Math.Exp(-dt * k * 0.5f));

            punch *= (float)Math.Exp(-dt * BattleRules.CamPunchFade);
            trauma = Math.Max(0f, trauma - dt * BattleRules.CamShakeFade);
        }

        //Spring : value follows target without bouncing past it (critically damped).
        //ADVANCED PART : the pull grows with the distance (stiffness squared) and the friction with the speed (twice the
        //stiffness). With exactly that much friction it is the fastest way to arrive without overshooting.
        private static void Spring(ref float value, ref float speed, float target, float stiffness, float dt)
        {
            speed += ((target - value) * stiffness * stiffness - 2f * stiffness * speed) * dt;
            value += speed * dt;
        }

        //Shown : what PostChain.ShowFinal is given this frame. calm 0..1 turns the shake down (LESS FLASHING).
        //extraZoom multiplies the zoom (the IMPACT FRAME's punch).
        public void Shown(float calm, float extraZoom, out Vector2 at, out float shownZoom, out float turn, out Vector2 shift)
        {
            float shake = trauma * trauma * calm;
            Vector2 tremble = new Vector2(Noise(clock * 31f, 0.3f), Noise(clock * 29f, 1.7f)) * (BattleRules.CamShakeMost * shake);
            at = Focus;
            turn = tilt + Noise(clock * 23f, 4.1f) * BattleRules.CamShakeTurn * shake;
            shift = pan + tremble;
            shownZoom = Math.Max(zoom + punch, 1f) * extraZoom;

            //At Rest : what is left of a spring is dropped, so a still camera draws the picture pixel for pixel (sharp)
            if (Math.Abs(turn) < 0.0002f) turn = 0f;
            if (shift.LengthSquared() < 0.0025f) shift = Vector2.Zero;
            if (shownZoom < 1.0005f) shownZoom = 1f;
            if (turn != 0f || shift != Vector2.Zero) shownZoom = Math.Max(shownZoom, Cover(at, turn, shift));
        }

        //To Screen : where a point of the world (game pixels) is seen through the camera, for the words that stand
        //on a fighter (their numbers, their name) so they move with them
        public static Vector2 ToScreen(Vector2 world, Vector2 at, float shownZoom, float turn, Vector2 shift)
        {
            Vector2 d = (world - at) * shownZoom;
            float c = (float)Math.Cos(turn);
            float s = (float)Math.Sin(turn);
            return at + shift + new Vector2(d.X * c - d.Y * s, d.X * s + d.Y * c);
        }

        //Cover : the least zoom that keeps the picture over the whole screen with this turn and slide, so no black
        //edge ever shows.
        //ADVANCED PART : for each corner of the screen, find which point of the picture lands there (undo the slide,
        //the turn and the zoom), then ask how much zoom keeps that point inside the picture. The biggest answer wins.
        private static float Cover(Vector2 at, float turn, Vector2 shift)
        {
            float need = 1f;
            float c = (float)Math.Cos(-turn);
            float s = (float)Math.Sin(-turn);
            float w = TacetGame.ScreenW;
            float h = TacetGame.ScreenH;
            for (int i = 0; i < 4; i++)
            {
                Vector2 corner = new Vector2((i & 1) == 0 ? 0f : w, (i & 2) == 0 ? 0f : h);
                Vector2 d = corner - at - shift;
                Vector2 v = new Vector2(d.X * c - d.Y * s, d.X * s + d.Y * c);     // the corner seen from at, unturned
                if (v.X > 0f) need = Math.Max(need, v.X / Math.Max(1f, w - at.X));
                if (v.X < 0f) need = Math.Max(need, -v.X / Math.Max(1f, at.X));
                if (v.Y > 0f) need = Math.Max(need, v.Y / Math.Max(1f, h - at.Y));
                if (v.Y < 0f) need = Math.Max(need, -v.Y / Math.Max(1f, at.Y));
            }
            return need;
        }

        //Noise : a smooth wobble between -1 and 1 made of three waves that never line up, so the shake never repeats
        //in a pattern the eye can catch
        private static float Noise(float t, float seed)
        {
            return (float)(Math.Sin(t + seed) * 0.5 + Math.Sin(t * 1.73 + seed * 2.1) * 0.3 + Math.Sin(t * 2.91 + seed * 3.7) * 0.2);
        }
    }
}
