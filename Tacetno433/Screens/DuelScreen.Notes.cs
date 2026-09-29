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
    //DuelScreen.Notes : the special notes and moments of a round.
    //   PAIRS       a note tied to a spark by a ribbon. The note is answered as usual, the spark
    //               half a beat later with one more flick, ANY way (like the stars of Project DIVA).
    //   FERMATA     TACET's held note on the last beat of an ordinary enemy's round. Stroke it,
    //               then hold the button with the baton still, see BattleRules.Fermata...
    //   TREMOLO     TACET's roll on the last beat of an elite's or a boss's round. Every shake of
    //               the baton for two beats counts, see BattleRules.Tremolo...
    //   COUNTER     a PERFECT big stroke (f) against a real f note, the rules live in BattleState.Resolve
    //   FORTISSIMO  the band on fire after a long combo, also in BattleState
    public partial class DuelScreen
    {
        //Grace Call : the second note of a pair, a quick little echo of the first
        private void CallGrace(int n)
        {
            if (!SoundBank.PlayCall(n, 0.5f, 0.3f))
                SoundBank.Play(Sfx.NoteOn, 0.5f, 0.4f);
            effects.SpawnRipple(EnemyX(), EnemyFeetY, 70f, true);

            if (!doubleTold)
            {
                doubleTold = true;
                Say(SayDouble);
            }
        }

        //Grace Update : waiting for the spark, half a beat after the first note. Any stroke in
        //its window answers it. The spark's window closes before the next beat's opens, so the
        //next beat's stroke is never taken by mistake.
        private void UpdateGrace(int b, bool stroked, float now)
        {
            float target = AnswerAt(pending) + beatLen * 0.5f;

            //Early Wobble : the hand's own bounce right after the first stroke is not the flick
            if (stroked && now >= target - battle.GoodWindow - 0.02f)
            {
                JudgeGrace(b, target);
                return;
            }

            if (now > target + GraceLate()) MissGrace(b);
        }

        //Grace Judge : the spark of a pair. Only its timing matters, any way at all. The size of
        //the first stroke already gave the order for the whole beat.
        private void JudgeGrace(int b, float target)
        {
            float now = StrokeClock();
            float off = Math.Abs(now - target);

            Grade grade = Grade.Miss;
            if (off <= battle.GoodWindow) grade = Grade.Good;
            if (off <= battle.PerfectWindowAt(b)) grade = Grade.Perfect;
            if (grade == Grade.Miss && now > target && battle.JoinedHas(battle.Results[b], MusicianTrait.Forgiven)) grade = Grade.Good;    // FASHIONABLY LATE

            hand.Play(PoseFor(gesture.Direction));
            baton.Whip(gesture.Direction);

            if (grade == Grade.Perfect) SoundBank.Play(Sfx.QtePerfect, 0.7f, 0.3f);
            if (grade == Grade.Miss) SoundBank.Play(Sfx.QteMiss, 0.7f, 0.3f);

            ResolveGrace(b, grade);
        }

        //Grace Missed : the flick back never came
        private void MissGrace(int b)
        {
            SoundBank.Play(Sfx.QteHesitate, 0.6f, 0.3f);
            ResolveGrace(b, Grade.Hesitate);
        }

        //Grace Resolve : the second note of the pair meets ours, a small clash of its own
        private void ResolveGrace(int b, Grade grade)
        {
            BeatResult r = battle.ResolveGrace(b, grade);
            barPush += r.GracePush;
            if (r.GracePush != 0) tugFlash = 1f;

            if (grade == Grade.Perfect || grade == Grade.Good)
            {
                if (r.OurPower > 0 && !PlayBandNote(b, 0.6f, 0f))
                    SoundBank.Play(Sfx.QteEase, 0.7f, 0.3f);
                effects.SpawnFlare(HitX, RingY, grade == Grade.Perfect ? 0.6f : 0.35f);
            }
            effects.SpawnSparks(HitX, RingY, 5, r.GracePush > 0 ? 1f : -1f);
            effects.SpawnHit(HitX, RingY, grade);
            if (grade == Grade.Perfect) laneFlash = Math.Max(laneFlash, 0.7f);
            if (grade != Grade.None) ShowJudge(graceText[(int)grade], 0.45f);
            if (grade != Grade.Hesitate) ShowTiming(grade, StrokeClock(), AnswerAt(pending) + beatLen * 0.5f);

            ShowCombo(r, r.Combo);
            ShowFire(r);
            ShowBreath(r);
            AdvanceBeat();
        }

        //Trill : TACET's roll sounds as quick ticks for its whole length when it is called.
        //CallNote sets trillStart each time the roll comes round (REPEATS).
        private void UpdateTrill()
        {
            if (trillStart < 0f || clock < trillStart) return;
            int due = (int)((clock - trillStart) / (beatLen * 0.25f));
            int most = (int)(BattleRules.TremoloBeats * 4f);
            while (trillTicks <= due && trillTicks < most)
            {
                SoundBank.Play(Sfx.NoteOn, 0.35f, trillTicks % 2 == 0 ? 0.1f : 0.3f);
                trillTicks++;
            }
        }

        //Hold Start : the stroke that opens TACET's fermata landed. The band starts its note now,
        //and the beat is settled when the hold ends.
        private void StartHold(Choice choice, Grade grade)
        {
            holding = true;
            holdChoice = choice;
            holdGrade = grade;
            heldTime = 0f;
            holdClock = clock;
            holdSteady = clock + 0.3f;
            holdEnd = AnswerAt(pending) + BattleRules.FermataBeats * beatLen;

            int b = BeatOf(pending);
            float volume = choice == Choice.Boost ? 1f : (choice == Choice.Ease ? 0.5f : 0.8f);
            if (!PlayBandNote(b, volume, 0f)) SoundBank.Play(Sfx.NoteOn, volume, 0.3f);
            for (int s = 0; s < StageLayout.SeatCount; s++)
                if (battle.Plays(s)) lit[s] = 1f;
        }

        //Hold Update : the hold lasts while the button stays down and the baton stays still.
        //Moving, or letting go, ends it early with whatever was held so far.
        private void UpdateHold(int b, float now)
        {
            //Settling : right after the stroke the hand is still slowing down, so moving does not
            //end the hold yet, but only truly still moments count as held
            bool down = Input.MouseDown();
            bool moving = baton.Velocity.Length() >= BattleRules.FermataStill;
            if (down && !moving) heldTime += clock - holdClock;
            holdClock = clock;
            bool still = !moving || clock < holdSteady;

            //Glow : the players holding the note stay lit
            for (int s = 0; s < StageLayout.SeatCount; s++)
                if (battle.Plays(s)) lit[s] = Math.Max(lit[s], 0.7f);

            if (!down || !still || now >= holdEnd) FinishHold(b);
        }

        //Hold Finish : settle the beat with the stroke that opened it and how long it was held
        private void FinishHold(int b)
        {
            if (!holding) return;
            holding = false;
            float full = BattleRules.FermataBeats * beatLen;
            battle.HoldFraction = MathHelper.Clamp(heldTime / full, 0f, 1f);
            bool whole = battle.HoldFraction >= 0.95f;

            ShowJudge(whole ? holdText[1] : holdText[0], 0.55f);
            if (whole) effects.SpawnFlare(HitX, RingY, 1.2f);
            ResolveAnswer(b, holdChoice, holdGrade);
            AdvanceBeat();
        }

        //Hold : while the fermata is held its sign sits over the hit point and a ring fills up
        //around it with the time held. The ribbon in the lane shrinks toward the hit point.
        private void DrawHold(SpriteBatch sb)
        {
            if (!holding) return;

            float cx = RingX();
            float cy = RingY;
            float full = BattleRules.FermataBeats * beatLen;
            float held = MathHelper.Clamp(heldTime / full, 0f, 1f);

            Gfx.DrawGlow(sb, cx, cy, 70f + held * 30f, Palette.Highlight * (0.2f + held * 0.3f));
            Gfx.CircleOutline(sb, cx, cy, RingTarget + 12f, Palette.Paper * 0.35f, 3f);
            if (held > 0f)
                Gfx.Arc(sb, cx, cy, RingTarget + 12f, -MathHelper.PiOver2, -MathHelper.PiOver2 + MathHelper.TwoPi * held, Palette.Highlight, 6f);
            NoteGlyph.FermataSign(sb, cx, cy - RingTarget - 34f, 20f, Palette.Highlight);
        }

        //Hold For Picture : DEVELOPER TOOL hook, the round skipped to TACET's fermata with the
        //hold already going, so a picture can show it
        public void HoldForPicture()
        {
            JumpForPicture(BattleRules.BeatsPerRound - 1);
            clock = AnswerAt(pending);
            called = BattleRules.BeatsPerRound - 1;
            StartHold(Choice.Normal, Grade.Perfect);
        }

        //Roll Update : count every stroke until the roll closes, then settle the beat
        private void UpdateRoll(int b, bool stroked, float now)
        {
            if (stroked) RollStroke();

            if (now >= rollEnd)
            {
                rolling = false;
                battle.RollStrokes = rollStrokes;
                Grade grade = BattleState.RollGrade(rollStrokes, battle.Run.Maestro);
                if (grade != Grade.Hesitate) ShowJudge(rollText[(int)grade], 0.55f);
                if (grade == Grade.Perfect) SoundBank.Play(Sfx.QtePerfect);
                ResolveAnswer(b, Choice.Boost, grade);                // TREMOLO : the whole band rolls
                AdvanceBeat();
            }
        }

        //Roll Stroke : one shake of the baton during TACET's roll
        private void RollStroke()
        {
            rollStrokes++;
            rollPulse = 1f;
            baton.Whip(gesture.Direction);
            hand.Play(PoseFor(gesture.Direction));

            bool counts = battle.RollCounted(rollStrokes - 1) < BattleRules.TremoloMost;   // ACCELERANDO counts twice
            effects.SpawnSparks(HitX, RingY, counts ? 4 : 1, 1f);
            SoundBank.Play(Sfx.QteNormal, counts ? 0.7f : 0.3f, Math.Min(0.6f, rollStrokes * 0.05f));
        }

        //Roll : while TACET's roll is answered, the ring becomes a counter. The number jumps with
        //every stroke, the pips fill up to the most that still counts, and the arc runs out
        //with the time left.
        private void DrawRoll(SpriteBatch sb)
        {
            if (!rolling) return;

            float cx = RingX();
            float cy = RingY;
            float left = MathHelper.Clamp((rollEnd - StrokeClock()) / (BattleRules.TremoloBeats * beatLen), 0f, 1f);
            float shakeX = (float)Math.Sin(time * 60f) * 3f * rollPulse;

            Gfx.DrawGlow(sb, cx, cy, 64f + rollPulse * 20f, Palette.Highlight * (0.2f + rollPulse * 0.2f));
            Gfx.Circle(sb, cx, cy, RingTarget + 8f, Palette.Void * 0.9f);
            Gfx.Arc(sb, cx, cy, RingTarget + 14f, -MathHelper.PiOver2, -MathHelper.PiOver2 + MathHelper.TwoPi * left, Palette.Highlight, 5f);
            Gfx.CircleOutline(sb, cx, cy, RingTarget + 8f, Palette.Paper * 0.6f, 2f);

            int shown = Math.Min(rollStrokes, 99);
            Gfx.TextCentered(sb, Game.LogoFont, NumberText.Get(shown), cx + shakeX, cy - 6f, Palette.Highlight, 0.42f + rollPulse * 0.1f);
            //Shake Sign : a two way arrow that shakes with every stroke, instead of a word
            float sx = cx + (float)Math.Sin(time * 40f) * 4f * (0.3f + rollPulse);
            Gfx.Rect(sb, sx - 12f, cy + 24f, 24f, 3f, Palette.Paper);
            Gfx.Arrow(sb, sx - 14f, cy + 25f, 5f, false, Palette.Paper);
            Gfx.Arrow(sb, sx + 14f, cy + 25f, 5f, true, Palette.Paper);

            Ui.Pips(sb, cx - BattleRules.TremoloMost * 7f, cy + RingTarget + 34f, battle.RollCounted(rollStrokes), BattleRules.TremoloMost, 4, 14, 1f);
        }

        //Counter Show : TACET's loudest note thrown back. A white flash, a streak across the whole
        //screen, the rift shudders, and the word itself.
        private void ShowCounter()
        {
            counterFlash = 1f;
            shake = 1f;
            ripple = 1.4f;
            rippleY = RingY;
            effects.SpawnFlare(HitX, RingY, 2f);
            effects.SpawnSparks(HitX, RingY, 30, 1f);
            effects.SpawnPop(RingX(), RingY - 150f, "COUNTER", Palette.Highlight, 0.9f);
            SoundBank.Play(Sfx.ClashWin, 1f, 0.3f);
            SoundBank.Play(Sfx.EnemyBoost, 1f, -0.5f);
            Say(SayCounter);
        }

        //Counter Flash : a white instant, and a streak through the whole screen at the hit point
        private void DrawCounterFlash(SpriteBatch sb)
        {
            if (counterFlash <= 0f) return;
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Color.White * (0.3f * counterFlash));
            Hollow.Flare(sb, HitX, RingY, 1400f * (1.2f - counterFlash * 0.2f), counterFlash);
        }

        //Fire Show : FORTISSIMO lighting up, or going out
        private void ShowFire(BeatResult r)
        {
            if (r.FortissimoStarted)
            {
                fireFlash = 1f;
                shake = Math.Max(shake, 0.6f);
                effects.SpawnPop(stageBox.Center.X, stageBox.Y + 40, "FORTISSIMO", Palette.Highlight, 0.8f);
                effects.SpawnSparks(BandX, RingY, 20, 1f);
                SoundBank.Play(Sfx.ComboUp, 1f, 0.8f);
                Say(SayFire);
            }
            if (r.FortissimoLost) SoundBank.Play(Sfx.ComboBreak, 1f, -0.3f);     // the plate going dark says the rest
        }

        //Fire Light : FORTISSIMO floods the floor under the band with white, and dark strokes rise
        //behind the players the way a comic draws a burning spirit. Drawn before the band, so
        //the strokes stay behind them. The stroke places come from their number, nothing is stored.
        private void DrawFireLight(SpriteBatch sb, float edge)
        {
            if (fireGlow <= 0.01f) return;
            float flicker = 0.85f + (float)Math.Sin(time * 17f) * 0.08f + (float)Math.Sin(time * 29f) * 0.07f;
            float a = fireGlow * flicker;
            int right = (int)Math.Min(edge, 640f);
            Gfx.DrawGlowBox(sb, new Rectangle(40, 400, right - 40, 190), Palette.Highlight * (0.7f * a));

            for (int i = 0; i < 24; i++)
            {
                float x = 120f + (i * 53) % 420;
                if (x > right - 20f) continue;
                float rise = (time * (240f + (i % 4) * 45f) + i * 71f) % 300f;
                float y = StageLayout.DuelFloorY + 96f - rise;
                float length = 18f + (i % 3) * 14f;
                float fade = 1f - rise / 300f;
                Gfx.Rect(sb, x, y - length, 2 + i % 2, length, Palette.Ink * (0.4f * fireGlow * fade));
            }
        }

        //Jump For Picture : DEVELOPER TOOL hook, the round skipped ahead to just before beat n is
        //answered, so a picture can show the roll or a pair without playing up to it
        public void JumpForPicture(int n)
        {
            StartRound();
            clock = AnswerAt(n) - beatLen * 0.4f;
            called = n;
            while (called + 1 < total && CallAt(called + 1) <= clock) called++;    // REPEATS : the next pass may be on its way
            graceCalled = called;
            trillTicks = 99;
            pending = n;
        }
    }
}
