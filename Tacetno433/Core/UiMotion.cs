using System;
using Microsoft.Xna.Framework;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //UiMotion (Beta1 screens) : how the buttons and pieces of a page move, one rhythm for the whole game
    //(docs/SCREENS_LEVEL_UP.md 3.4 and 4.4). Every number is in BattleRules, UI Feel.
    //   POINT   the mouse or the keys come onto a button : it eases in (Hover), and its circle sends out one thin ring
    //   PRESS   squashed small, bounced back, and a flash of white (none with LESS FLASHING)
    //   ENTER   a page's pieces come in one after another, sliding a little and slowing to a stop (Between)
    //   COUNT   a number counts up from 0 (Count)
    //A page keeps one UiMotion for each button and ticks it in Update. Core/Ui.cs draws the buttons from it.
    //The easing sums are at the bottom : each takes 0..1 and gives back 0..1, only the way there changes.
    public class UiMotion
    {
        public float Hover;              // 0..1, how far the button is pointed at (moves at 1 / UiHoverTime a second)
        public bool Over;                // pointed at this frame
        private float ringAge = 99f;     // seconds since it was pointed at, for the ring it sends out
        private float pressAge = 99f;    // seconds since it was pressed

        //Update : over is whether the button is pointed at this frame
        public void Update(float dt, bool over)
        {
            if (over && !Over) ringAge = 0f;
            Over = over;
            float step = dt / BattleRules.UiHoverTime;
            Hover = over ? Math.Min(1f, Hover + step) : Math.Max(0f, Hover - step);
            ringAge += dt;
            pressAge += dt;
        }

        //Press : the button was pressed
        public void Press()
        {
            pressAge = 0f;
        }

        //Shown : the hover the drawing uses, fast at first then a soft stop (the ink bar shooting out)
        public float Shown
        {
            get { return EaseOut(Hover); }
        }

        //Scale : PRESS squashes it to UiPressSquash in UiPressDown seconds, then it bounces back in UiPressBack,
        //going a little past full size before it settles (EaseBack)
        public float Scale
        {
            get
            {
                float squash = BattleRules.UiPressSquash;
                if (pressAge < BattleRules.UiPressDown)
                    return MathHelper.Lerp(1f, squash, EaseOut(pressAge / BattleRules.UiPressDown));
                float back = (pressAge - BattleRules.UiPressDown) / BattleRules.UiPressBack;
                if (back < 1f) return MathHelper.Lerp(squash, 1f, EaseBack(back));
                return 1f;
            }
        }

        //Flash : the white of a press, 1 at once and gone after UiPressFlash. LESS FLASHING : none.
        public float Flash
        {
            get
            {
                if (Settings.ReduceFlashing) return 0f;
                return Math.Max(0f, 1f - pressAge / BattleRules.UiPressFlash);
            }
        }

        //Ring : how far the ring sent out has gone, 0..1 (1 = gone)
        public float Ring
        {
            get { return Math.Min(1f, ringAge / BattleRules.UiRingTime); }
        }

        //Snap : straight to pointed at or not, without the ring (a page that opens with a button already chosen)
        public void Snap(bool over)
        {
            Over = over;
            Hover = over ? 1f : 0f;
            ringAge = 99f;
        }

        //======================================================================================
        //Shared timing
        //======================================================================================

        //Between : ENTER, how far a piece has come in, 0..1 eased. It starts start seconds into the page's own timeline and
        //takes length seconds (UiEnterTime for a usual piece, the next one UiEnterStagger later)
        public static float Between(float time, float start, float length)
        {
            return EaseOut(MathHelper.Clamp((time - start) / length, 0f, 1f));
        }

        //Count : COUNT UP, a number on its way from 0 to value, started at start seconds
        public static int Count(int value, float time, float start)
        {
            float t = MathHelper.Clamp((time - start) / BattleRules.UiCountTime, 0f, 1f);
            return (int)Math.Round(value * EaseOut(t));
        }

        //Ease Out : fast at first, slowing to a soft stop (cubic)
        public static float EaseOut(float t)
        {
            float rest = 1f - t;
            return 1f - rest * rest * rest;
        }

        //Ease In Out : slow, fast, slow (smoothstep)
        public static float EaseInOut(float t)
        {
            return t * t * (3f - 2f * t);
        }

        //Ease Back : runs a little past the end and comes back to it, for a slam or a bounce
        //ADVANCED PART (small) : a cubic that starts at 0, ends at 1 and swings over 1 on the way. 1.7 sets how far
        //(about 10% past the end), the classic number from Robert Penner's easing sums.
        public static float EaseBack(float t)
        {
            const float swing = 1.70158f;
            float r = t - 1f;
            return 1f + r * r * ((swing + 1f) * r + swing);
        }
    }
}
