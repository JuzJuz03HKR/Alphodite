using System;
using Microsoft.Xna.Framework;

namespace Tacetno433.Core
{
    //Flick : the four ways a baton stroke can go
    public enum Flick { None, Up, Down, Left, Right }

    //GestureReader : turns mouse movement into baton strokes.
    //
    //A STROKE is one movement of the baton, from where it starts to where it stops.
    //   it starts   when the mouse begins to move quickly
    //   it ends     when the mouse slows right down, OR turns a sharp corner (the bounce at
    //               the bottom of a conductor's beat), OR has simply gone on too long
    //When a stroke ends we know three things:
    //   which way   the axis that moved more between the start and the end
    //   how big     the straight distance from the start to the end
    //   when        the moment it stopped. A conductor's beat lands where the stick stops
    //               (musicians call that point the ictus), so that is when timing is judged.
    //
    //The baton only counts while the LEFT BUTTON IS HELD. With the button up the mouse can go
    //anywhere and nothing is read. Letting go in the middle of a stroke ends it right there.
    //
    //Nothing here knows about the duel. It only answers "was there a stroke, which way, how big".
    //
    //ADVANCED PART : a small "state machine" with two states, waiting and inside a stroke,
    //switched by how fast the mouse is moving. It is only subtraction, a square root for
    //distance, and a dot product to spot a sharp turn.
    public class GestureReader
    {
        //Stroke Feel : the numbers that decide what counts as a stroke
        public const float StartSpeed = 350f;     // pixels per second before a stroke has started
        public const float StopSpeed = 110f;      // below this the baton has stopped
        public const float MinLength = 34f;       // shorter than this is only a shaking hand
        public const float MaxTime = 0.6f;        // a stroke that goes on longer is cut off here

        //Stroke Size : a stroke shorter than MiddleLength is small, longer than BigLength is big.
        //Round 12.1 : the middle zone is 140 pixels wide instead of 100 (BigLength was 200), so an
        //mf is easier to land at speed. The small zone is unchanged, a big stroke reaches further.
        public const float MiddleLength = 100f;
        public const float BigLength = 240f;

        //Trail : the last few positions, kept for drawing the tail of the baton
        private const int Samples = 20;
        private Vector2[] spot = new Vector2[Samples];
        private int count;
        private int next;

        private float clock;
        private Vector2 last;           // where the mouse was on the previous frame
        private bool hasLast;
        private Vector2 velocity;       // smoothed, in pixels per second

        //Stroke In Progress
        public bool InStroke;
        public Vector2 StrokeStart;
        private float strokeStartTime;

        //Stroke Result : filled in when a stroke finishes, handed out once by Read
        private bool ready;
        public Flick Direction;
        public float Length;
        public Vector2 From;
        public Vector2 To;

        //Gesture Clear : forget everything, so old movement is never read as a new stroke
        public void Clear()
        {
            count = 0;
            next = 0;
            hasLast = false;
            velocity = Vector2.Zero;
            InStroke = false;
            ready = false;
            Direction = Flick.None;
        }

        //Held : true while the button is down, so the baton is raised
        public bool Held;

        //Gesture Update : called once a frame, held says whether the left button is down
        public void Update(float dt, bool held)
        {
            clock += dt;
            Vector2 now = Input.MousePos;

            //Button Up : the baton is lowered. A stroke in progress ends where it is let go.
            if (!held)
            {
                if (InStroke) Finish(StrokeStart, now);
                InStroke = false;
                Held = false;
                count = 0;
                next = 0;
                hasLast = false;
                return;
            }

            //Button Down : start reading from a clean slate
            if (!Held)
            {
                Held = true;
                velocity = Vector2.Zero;
            }

            spot[next] = now;
            next = (next + 1) % Samples;
            if (count < Samples) count++;

            if (!hasLast || dt <= 0f)
            {
                last = now;
                hasLast = true;
                return;
            }

            //Velocity : how fast the mouse moved this frame, smoothed with the frame before
            //so a single jumpy frame cannot start or stop a stroke on its own
            Vector2 frameVelocity = (now - last) / dt;
            velocity = velocity * 0.5f + frameVelocity * 0.5f;
            float speed = velocity.Length();

            if (!InStroke)
            {
                //Waiting : a quick movement starts a stroke from where the mouse just was
                if (speed >= StartSpeed)
                {
                    InStroke = true;
                    StrokeStart = last;
                    strokeStartTime = clock - dt;
                }
            }
            else
            {
                Vector2 soFar = now - StrokeStart;
                float soFarLength = soFar.Length();
                float frameSpeed = frameVelocity.Length();

                //Sharp Turn : the mouse is now heading well away from the way the stroke was
                //going. That is the bounce, so the stroke ended on the previous frame.
                bool turned = false;
                if (soFarLength > MinLength && frameSpeed > StopSpeed)
                {
                    float along = Vector2.Dot(frameVelocity, soFar) / (frameSpeed * soFarLength);
                    turned = along < 0.35f;
                }

                bool stopped = speed < StopSpeed;
                bool tooLong = clock - strokeStartTime > MaxTime;

                if (turned || stopped || tooLong)
                {
                    Finish(StrokeStart, turned ? last : now);
                    InStroke = false;

                    //Bounce : after a turn the next stroke begins right here, because a
                    //conductor's hand never stops between beats
                    if (turned)
                    {
                        InStroke = true;
                        StrokeStart = last;
                        strokeStartTime = clock - dt;
                    }
                }
            }

            last = now;
        }

        //Stroke Finish : work out which way and how big, then keep it until it is read
        private void Finish(Vector2 from, Vector2 to)
        {
            float dx = to.X - from.X;
            float dy = to.Y - from.Y;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length < MinLength) return;

            if (Math.Abs(dy) > Math.Abs(dx)) Direction = dy < 0f ? Flick.Up : Flick.Down;
            else Direction = dx < 0f ? Flick.Left : Flick.Right;

            Length = length;
            From = from;
            To = to;
            ready = true;
        }

        //Stroke Read : true once for every finished stroke
        public bool Read()
        {
            if (!ready) return false;
            ready = false;
            return true;
        }

        //Live Stroke : how far the stroke in progress has gone, for the size guide
        public Vector2 LiveVector
        {
            get { return InStroke ? last - StrokeStart : Vector2.Zero; }
        }

        //Trail : the recent positions, oldest first, so the baton can draw its tail
        public int TrailCount
        {
            get { return count; }
        }

        public Vector2 TrailPoint(int i)
        {
            int s = (next - count + i + Samples * 2) % Samples;
            return spot[s];
        }

        //Trail Age : 0 for the newest point, 1 for the oldest one still kept
        public float TrailAge(int i)
        {
            if (count < 2) return 1f;
            return 1f - i / (float)(count - 1);
        }
    }
}
