using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Audio;
using Tacetno433.Battle;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //TutorialScreen.Practice : the stage of the tutorial. A small, safe copy of the duel's
    //timing : the same hit point, ring, lane, notes and timing windows, but no TACET to lose to.
    //
    //Notes are never stored. Note n is worked out from its number when it is needed :
    //it leaves TACET's side on beat n and is answered one bar later, on beat n + 4, so the
    //first bar is always the count in (3 2 1), exactly like a round of the duel.
    public partial class TutorialScreen
    {
        //Note Kind : what note n is in the lesson on show
        private enum NoteKind { None, Plain, Roll, Hold, Pair }

        //Stage Layout : the same places as the duel, so the hands learn the real positions
        private const float HitX = 640f;
        private const float RingY = 372f;
        private const float LaneHalf = 34f;
        private const float RingStart = 120f;
        private const float RingTarget = 44f;

        //Loudness Order : the marks the LOUD AND SOFT lesson plays, round and round
        private static Choice[] loudOrder = { Choice.Boost, Choice.Ease, Choice.Normal, Choice.Boost, Choice.Ease, Choice.Ease, Choice.Normal, Choice.Boost };
        private static string[] dynamicMark = { "mf", "f", "p" };          // in Choice order
        private static string[] markHints =                                 // in Choice order
        {
            "mf : A MIDDLE STROKE, PAST THE FIRST MARK",
            "f IS LOUD : BIG, THE WHOLE BAND",
            "p IS SOFT : SMALL, THE BACK ROW"
        };
        private static string[] gradeWords = { "", "PERFECT", "GOOD", "MISS", "HESITATE" };
        private static string[] timingWords = { "EARLY", "", "LATE" };
        private static string[] countWords = { "", "1", "2", "3" };
        private static string[] holdWords = { "LET GO", "FERMATA" };
        private string[] sparkWords = new string[5];
        private string[] rollWords = new string[5];

        //Beat State
        private bool running;           // the beat has started, the first hold of the button starts it
        private float clock;            // seconds since the first beat of the count in
        private float beatLen;
        private int pending;            // the note waiting for its answer
        private int sent = -1;          // the last note that has left TACET's side
        private int ticked = -1;        // the last beat the metronome clicked on

        //Special Notes In Progress
        private bool rolling;
        private int rollStrokes;
        private float rollEnd;
        private bool holding;
        private float holdEnd, holdClock, holdSteady, heldTime;
        private bool onSpark;

        //Breath : the KEEP BREATHING lesson's pretend stamina, 0 to 1. It never runs out here.
        private float breath = 1f;
        private float breathJolt;
        private const float BlowShare = 0.14f;         // breath one lost note knocks out
        private const float BreathFloor = 0.12f;       // the tutorial never lets it reach zero

        //Judgement : one word under the hit point, like the duel
        private string judgeWord = "";
        private float judgeTimer = 9f;
        private int judgeTiming;        // -1 early, +1 late, 0 nothing to say
        private float laneFlash;

        //Practice Prepare : the words that need a grade in front, made once
        private void PreparePractice()
        {
            for (int g = 0; g < gradeWords.Length; g++)
            {
                sparkWords[g] = gradeWords[g].Length == 0 ? "" : gradeWords[g] + "  /  FLICK";
                rollWords[g] = gradeWords[g].Length == 0 ? "" : gradeWords[g] + "  /  ROLL";
            }
        }

        //Practice Reset : a new lesson starts with the beat stopped
        private void ResetPractice()
        {
            running = false;
            clock = 0f;
            pending = 0;
            sent = -1;
            ticked = -1;
            rolling = false;
            holding = false;
            onSpark = false;
            breath = 1f;
            judgeTimer = 9f;
            beatLen = lessons[index].Bpm > 0 ? 60f / lessons[index].Bpm : 1f;
        }

        private void StopBeat()
        {
            running = false;
            rolling = false;
            holding = false;
            onSpark = false;
        }

        //Note Kind Of : the lesson's script, worked out from the note's number.
        //The roll and the hold lessons go round six beats : two notes, the special note for two
        //beats, one beat of rest, one more note. The rest keeps the next note clear of the special.
        private NoteKind KindOf(int n)
        {
            Kind k = lessons[index].Kind;
            if (k == Kind.Roll || k == Kind.Fermata)
            {
                int step = n % 6;
                if (step == 2) return k == Kind.Roll ? NoteKind.Roll : NoteKind.Hold;
                if (step == 3 || step == 4) return NoteKind.None;
                return NoteKind.Plain;
            }
            if (k == Kind.Spark) return n % 2 == 0 ? NoteKind.Pair : NoteKind.Plain;
            return NoteKind.Plain;
        }

        private Choice MarkOf(int n)
        {
            if (lessons[index].Kind == Kind.Loudness) return loudOrder[n % loudOrder.Length];
            return Choice.Normal;
        }

        private Flick WayOf(int n)
        {
            return pattern[n % 4];
        }

        //Times : note n leaves on beat n and is answered on beat n + 4
        private float SentTime(int n) { return n * beatLen; }
        private float AnswerTime(int n) { return (n + 4) * beatLen; }
        private float EarlyLimit() { return Math.Min(BattleRules.EarlyTime, beatLen * 0.45f); }
        private float LateLimit() { return Math.Min(BattleRules.LateTime, beatLen * 0.45f); }

        //Beat Pulse : 1 right on a beat, falling quickly to 0 before the next one
        private float BeatPulse()
        {
            if (!running) return 0f;
            float left = 1f - (clock / beatLen) % 1f;
            return left * left * left;
        }

        //Beat Lessons : 5 to 11. Holding the button the first time starts the music.
        private void UpdateBeat(float dt, Lesson l)
        {
            laneFlash = Math.Max(0f, laneFlash - dt * 4f);
            breathJolt = Math.Max(0f, breathJolt - dt * 3f);
            judgeTimer += dt;

            if (!running)
            {
                gesture.Read();
                if (Input.MouseDown())
                {
                    running = true;
                    clock = 0f;
                }
                return;
            }

            clock += dt;

            //Metronome And Call : a click on every beat, TACET's note on the beats it sends one
            int beatNow = (int)(clock / beatLen);
            if (beatNow != ticked)
            {
                ticked = beatNow;
                SoundBank.Play(Sfx.BeatTick, 0.25f, 0f);
            }
            while (clock >= SentTime(sent + 1))
            {
                sent++;
                if (KindOf(sent) != NoteKind.None && HasLane(l)) SoundBank.Play(Sfx.NoteOn, 0.6f, 0f);
            }

            UpdateAnswer(l);
        }

        //Has Lane : the lessons where TACET's notes slide in. The first beat lesson only has the ring.
        private bool HasLane(Lesson l)
        {
            return l.Kind != Kind.Timing;
        }

        //Answer Update : the note being answered, its window, and what happens if it is missed
        private void UpdateAnswer(Lesson l)
        {
            int n = pending;
            NoteKind k = KindOf(n);
            float target = AnswerTime(n);
            float now = clock - Settings.TimingOffset;          // the player's own STROKE TIMING
            bool stroked = gesture.Read();

            if (holding) { UpdateHold(now); return; }
            if (rolling) { UpdateRoll(stroked, now); return; }
            if (onSpark) { UpdateSpark(stroked, now); return; }

            //Empty Beat : the second half of a roll or a hold, nothing to answer
            if (k == NoteKind.None)
            {
                if (clock >= target) pending++;
                return;
            }

            //Roll : opens a moment before its beat and lasts two beats
            if (k == NoteKind.Roll)
            {
                if (now >= target - BattleRules.GoodWindow)
                {
                    rolling = true;
                    rollStrokes = 0;
                    rollEnd = target + BattleRules.TremoloBeats * beatLen;
                    if (stroked) RollStroke();
                }
                return;
            }

            if (stroked && now >= target - EarlyLimit())
            {
                JudgeStroke(l, n, k, target, now);
                return;
            }

            //Hesitate : the beat went by without a stroke
            if (now > target + LateLimit())
            {
                ShowJudge(gradeWords[(int)Grade.Hesitate], 0);
                SoundBank.Play(Sfx.QteHesitate);
                LoseNote(l);
                pending++;
            }
        }

        //Stroke Judge : the same windows as the duel. A wrong way is always a MISS.
        private void JudgeStroke(Lesson l, int n, NoteKind k, float target, float now)
        {
            float off = Math.Abs(now - target);
            Grade grade = Grade.Miss;
            if (off <= BattleRules.GoodWindow) grade = Grade.Good;
            if (off <= BattleRules.PerfectWindow) grade = Grade.Perfect;
            bool rightWay = gesture.Direction == WayOf(n);
            if (!rightWay) grade = Grade.Miss;
            int size = Baton.SizeOf(gesture.Length);
            bool good = grade == Grade.Perfect || grade == Grade.Good;

            baton.Whip(gesture.Direction);
            ShowJudge(rightWay ? gradeWords[(int)grade] : "WRONG WAY", rightWay && grade != Grade.Perfect ? (now < target ? -1 : 1) : 0);
            effects.SpawnHit(HitX, RingY, grade);
            if (size == 2 && rightWay) effects.SpawnSlash(gesture.From, gesture.To, grade == Grade.Perfect ? 1f : 0.55f);
            if (grade == Grade.Perfect) SoundBank.Play(Sfx.QtePerfect);
            if (grade == Grade.Miss) SoundBank.Play(Sfx.QteMiss);

            //Fermata : a landed stroke opens the hold, the beat is settled when it ends
            if (k == NoteKind.Hold && good)
            {
                StartHold();
                return;
            }

            //Right Size : only the LOUD AND SOFT lesson asks for it
            bool counts = good;
            if (l.Kind == Kind.Loudness && good && !SizeFits(MarkOf(n), size))
            {
                counts = false;
                Hint(markHints[(int)MarkOf(n)]);
            }

            if (counts) WinNote(l);
            else LoseNote(l);

            if (k == NoteKind.Pair) onSpark = true;          // the spark is still to come
            else pending++;
        }

        //Size Fits : f wants a big stroke, p a small one, mf anything but small
        private static bool SizeFits(Choice mark, int size)
        {
            if (mark == Choice.Boost) return size == 2;
            if (mark == Choice.Ease) return size == 0;
            return size == 1;                                // round 12 : mf is a middle stroke, the size is who plays
        }

        //Note Won : the note breaks, the lane lights. The lessons with a beat count it here.
        private void WinNote(Lesson l)
        {
            if (HasLane(l)) effects.SpawnShatter(HitX, RingY, 30f);
            laneFlash = 1f;
            if (l.Kind == Kind.Timing || l.Kind == Kind.Lane || l.Kind == Kind.Loudness || l.Kind == Kind.Breath) done++;
        }

        //Note Lost : in the KEEP BREATHING lesson TACET's note flies on into the band
        private void LoseNote(Lesson l)
        {
            if (l.Kind == Kind.Breath)
            {
                effects.SpawnBlow(HitX, RingY, BreathPlate.Center.X, BreathPlate.Center.Y, 0.7f);    // into the breath bar
                breath = Math.Max(BreathFloor, breath - BlowShare);
                breathJolt = 1f;
                done++;                                          // this lesson counts every note played
            }
        }

        //Spark : any stroke in its window, half a beat after the note
        private void UpdateSpark(bool stroked, float now)
        {
            float target = AnswerTime(pending) + beatLen * 0.5f;
            if (stroked && now >= target - BattleRules.GoodWindow - 0.02f)
            {
                float off = Math.Abs(now - target);
                Grade grade = Grade.Miss;
                if (off <= BattleRules.GoodWindow) grade = Grade.Good;
                if (off <= BattleRules.PerfectWindow) grade = Grade.Perfect;
                baton.Whip(gesture.Direction);
                ShowJudge(sparkWords[(int)grade], grade == Grade.Good ? (now < target ? -1 : 1) : 0);
                effects.SpawnHit(HitX, RingY, grade);
                if (grade != Grade.Miss)
                {
                    done++;
                    laneFlash = 0.7f;
                }
                onSpark = false;
                pending++;
                return;
            }

            if (now > target + Math.Min(BattleRules.GoodWindow, beatLen * 0.3f))
            {
                ShowJudge(sparkWords[(int)Grade.Miss], 0);
                onSpark = false;
                pending++;
            }
        }

        //Roll : count every stroke until the bar runs out
        private void UpdateRoll(bool stroked, float now)
        {
            if (stroked) RollStroke();
            if (now < rollEnd) return;

            rolling = false;
            Grade grade = BattleState.RollGrade(rollStrokes);
            ShowJudge(rollWords[(int)grade], 0);
            if (rollStrokes >= BattleRules.TremoloPerfect)
            {
                done++;
                effects.SpawnFlare(HitX, RingY, 1f);
            }
            else Hint("FASTER : SHAKE IT BACK AND FORTH");
            pending++;
        }

        private void RollStroke()
        {
            rollStrokes++;
            baton.Whip(gesture.Direction);
            effects.SpawnSparks(HitX, RingY, 4, 1f);
            SoundBank.Play(Sfx.QteNormal, 0.7f, Math.Min(0.6f, rollStrokes * 0.05f));
        }

        //Hold : the same rule as the duel's fermata. Moving or letting go ends it.
        private void StartHold()
        {
            holding = true;
            heldTime = 0f;
            holdClock = clock;
            holdSteady = clock + 0.3f;                           // the hand is still slowing down
            holdEnd = AnswerTime(pending) + BattleRules.FermataBeats * beatLen;
        }

        private void UpdateHold(float now)
        {
            bool down = Input.MouseDown();
            bool moving = baton.Velocity.Length() >= BattleRules.FermataStill;
            if (down && !moving) heldTime += clock - holdClock;
            holdClock = clock;
            bool still = !moving || clock < holdSteady;
            if (down && still && now < holdEnd) return;

            holding = false;
            float held = heldTime / (BattleRules.FermataBeats * beatLen);
            bool whole = held >= 0.9f;
            ShowJudge(holdWords[whole ? 1 : 0], 0);
            if (whole)
            {
                done++;
                effects.SpawnFlare(HitX, RingY, 1.2f);
            }
            else Hint(down ? "KEEP THE BATON STILL" : "KEEP THE BUTTON DOWN");
            pending++;
        }

        private void ShowJudge(string word, int timing)
        {
            if (word.Length == 0) return;
            judgeWord = word;
            judgeTimer = 0f;
            judgeTiming = timing;
        }

        //Lane Spot : where a note sent at this moment is now, from the right edge to the hit point
        private float LaneX(float sentAt)
        {
            float t = MathHelper.Clamp((clock - sentAt) / (4f * beatLen), 0f, 1f);
            return MathHelper.Lerp(TacetGame.ScreenW - 30f, HitX, t);
        }

        //Stage Draw : the practice area, drawn under the header, the card and the footer
        private void DrawStage(SpriteBatch sb, Lesson l)
        {
            if (l.Kind == Kind.Finish)
            {
                DrawReadyStage(sb);
                return;
            }

            float pulse = BeatPulse();
            bool lane = l.Bpm > 0 && HasLane(l);
            if (lane) DrawLane(sb, pulse);
            NoteGlyph.HitPoint(sb, HitX, RingY, LaneHalf, pulse);
            if (lane && running) DrawNotes(sb);

            effects.DrawShards(sb);
            effects.DrawHits(sb);

            if (l.Bpm > 0) DrawBeatGuide(sb, l);
            else DrawFreeGuide(sb, l);

            DrawJudge(sb);
            if (l.Kind == Kind.Breath) DrawBreath(sb);
            effects.DrawBlows(sb);
            effects.DrawSlashes(sb);
            effects.DrawFlares(sb);
            effects.DrawSparks(sb);
        }

        //Lane : the dark strip, its beat lines, and the flash after a good hit
        private void DrawLane(SpriteBatch sb, float pulse)
        {
            float top = RingY - LaneHalf;
            float width = TacetGame.ScreenW - HitX;
            Gfx.Rect(sb, HitX, top, width, LaneHalf * 2f, Color.Black * 0.5f);
            Gfx.Rect(sb, HitX, top, width, 1, Palette.Paper * (0.25f + pulse * 0.3f));
            Gfx.Rect(sb, HitX, top + LaneHalf * 2f - 1f, width, 1, Palette.Paper * (0.25f + pulse * 0.3f));

            if (laneFlash > 0f)
                for (int i = 0; i < 8; i++)
                    Gfx.Rect(sb, HitX + i * 70f, top + 1, 70f, LaneHalf * 2f - 2f, Palette.Highlight * (0.22f * laneFlash * (1f - i / 8f)));

            if (!running) return;
            for (int j = ticked - 4; j <= ticked + 1; j++)
            {
                if (j < 0) continue;
                float lx = LaneX(j * beatLen);
                if (lx <= HitX + 1f) continue;
                Gfx.Rect(sb, lx - (j % 4 == 0 ? 1f : 0.5f), top + 2, j % 4 == 0 ? 2f : 1f, LaneHalf * 2f - 4f, Palette.Paper * (j % 4 == 0 ? 0.4f : 0.16f));
            }
        }

        //Notes : TACET's notes on their way, the next one bright, the others dimmer
        private void DrawNotes(SpriteBatch sb)
        {
            for (int n = pending; n <= sent; n++)
            {
                NoteKind k = KindOf(n);
                if (k == NoteKind.None) continue;
                float x = LaneX(SentTime(n));
                float t = MathHelper.Clamp((clock - SentTime(n)) / (4f * beatLen), 0f, 1f);
                float focus = n == pending ? 1f : (n == pending + 1 ? 0.75f : 0.5f);

                if (k == NoteKind.Pair) DrawSpark(sb, n, x, focus);
                if (n == pending && onSpark) continue;                 // the note itself is answered
                if (k == NoteKind.Roll) NoteGlyph.RollBar(sb, x, LaneX(SentTime(n) + BattleRules.TremoloBeats * beatLen), RingY);
                if (k == NoteKind.Hold)
                {
                    NoteGlyph.HoldRibbon(sb, x, LaneX(SentTime(n) + BattleRules.FermataBeats * beatLen), RingY);
                    if (holding && n == pending) continue;             // the hold draws itself at the hit point
                    NoteGlyph.FermataSign(sb, x, RingY - 30f, 14f, Palette.Highlight);
                }
                if (k == NoteKind.Roll && rolling && n == pending) continue;

                Choice mark = MarkOf(n);
                float radius = (mark == Choice.Boost ? 31f : (mark == Choice.Ease ? 23f : 27f)) * (0.85f + 0.25f * t);
                float thick = mark == Choice.Boost ? 4.5f : (mark == Choice.Ease ? 1.5f : 2.5f);
                Hollow.Ring(sb, x, RingY, radius, thick, focus);
                if (mark == Choice.Boost) NoteGlyph.Crown(sb, x, RingY, radius, time, focus);

                string word = k == NoteKind.Roll ? "tr" : dynamicMark[(int)mark];
                float scale = TextSize.Small * (mark == Choice.Boost ? 1.15f : (mark == Choice.Normal ? 0.72f : 0.9f));
                Gfx.TextCentered(sb, Game.BigFont, word, x, RingY - 2, Palette.Highlight * focus, scale);
                if (k != NoteKind.Roll) NoteGlyph.Pointer(sb, WayOf(n), x, RingY, radius, focus);
            }
        }

        //Spark : the second note of a pair, half a beat behind its note, tied by a ribbon
        private void DrawSpark(SpriteBatch sb, int n, float x, float focus)
        {
            float gx = LaneX(SentTime(n) + beatLen * 0.5f);
            if (gx > x + 2f && !(n == pending && onSpark))
            {
                Gfx.Rect(sb, x, RingY - 9f, gx - x, 18f, Palette.Paper * 0.3f);
                Gfx.Rect(sb, x, RingY - 9f, gx - x, 2f, Palette.Paper * 0.8f);
                Gfx.Rect(sb, x, RingY + 7f, gx - x, 2f, Palette.Paper * 0.8f);
            }
            NoteGlyph.Spark(sb, gx, RingY, 14f, Palette.Highlight * focus);
        }

        //Beat Guide : the count in, the closing ring with its arrow, the roll and hold counters,
        //and the ruler while the baton is raised
        private void DrawBeatGuide(SpriteBatch sb, Lesson l)
        {
            if (!running)
            {
                if (clearTimer >= 0f) return;                     // the lesson is done, no new start
                float bob = (float)Math.Sin(time * 4f) * 4f;
                NoteGlyph.Arrow(sb, Flick.Down, HitX, RingY - 150f + bob, 40f, 14f, Palette.Ink, 8f);
                Gfx.TextSpacedCentered(sb, Game.Font, "HOLD TO BEGIN", HitX, RingY - 206f, Palette.Ink, TextSize.Label, 3f);
                return;
            }

            //Count In : 3 2 1 over the hit point while TACET plays its first bar
            if (clock >= beatLen && clock < AnswerTime(0))
            {
                int count = 4 - (int)(clock / beatLen);
                float a = 1f - (clock / beatLen) % 1f * 0.7f;
                Gfx.Circle(sb, HitX, RingY - 116f, 34f, Palette.Void * (0.85f * a));
                Gfx.TextCentered(sb, Game.LogoFont, countWords[count], HitX, RingY - 118f, Palette.Highlight * a, TextSize.Banner * 0.6f);
            }

            if (rolling) { DrawRollCounter(sb); return; }
            if (holding) { DrawHoldCounter(sb); return; }

            //Ring : one beat before the answer it starts closing, it meets its mark on the beat
            float target = onSpark ? AnswerTime(pending) + beatLen * 0.5f : AnswerTime(pending);
            float span = onSpark ? beatLen * 0.5f : beatLen;
            float t = (clock - (target - span)) / span;
            if (t < 0f || KindOf(pending) == NoteKind.None) return;
            float alpha = MathHelper.Clamp(t * 4f, 0f, 1f);
            float mark = onSpark ? RingTarget * 0.7f : RingTarget;
            float start = onSpark ? RingStart * 0.7f : RingStart;
            NoteGlyph.TimingRing(sb, HitX, RingY, t, mark, start, BeatPulse() * 3f, alpha);

            if (onSpark)
            {
                NoteGlyph.Spark(sb, HitX, RingY - mark - 26f, 14f, Palette.Highlight * alpha);
                return;
            }
            if (KindOf(pending) == NoteKind.Roll) return;

            Flick want = WayOf(pending);
            Vector2 d = NoteGlyph.Way(want);
            NoteGlyph.Arrow(sb, want, HitX + d.X * (mark + 30f), RingY + d.Y * (mark + 30f), 36f, 12f, Palette.Ink * alpha, 9f);
            NoteGlyph.Arrow(sb, want, HitX + d.X * (mark + 30f), RingY + d.Y * (mark + 30f), 32f, 9f, Palette.Highlight * alpha, 4f);
        }

        private void DrawRollCounter(SpriteBatch sb)
        {
            float left = MathHelper.Clamp((rollEnd - (clock - Settings.TimingOffset)) / (BattleRules.TremoloBeats * beatLen), 0f, 1f);
            Gfx.Circle(sb, HitX, RingY, RingTarget + 8f, Palette.Void * 0.9f);
            Gfx.Arc(sb, HitX, RingY, RingTarget + 14f, -MathHelper.PiOver2, -MathHelper.PiOver2 + MathHelper.TwoPi * left, Palette.Highlight, 5f);
            Gfx.TextCentered(sb, Game.LogoFont, NumberText.Get(Math.Min(rollStrokes, 99)), HitX, RingY - 6f, Palette.Highlight, 0.42f);
            Ui.Pips(sb, HitX - BattleRules.TremoloMost * 7f, RingY + RingTarget + 34f, Math.Min(rollStrokes, BattleRules.TremoloMost), BattleRules.TremoloMost, 4, 14, 1f);
        }

        private void DrawHoldCounter(SpriteBatch sb)
        {
            float held = MathHelper.Clamp(heldTime / (BattleRules.FermataBeats * beatLen), 0f, 1f);
            Gfx.DrawGlow(sb, HitX, RingY, 70f + held * 30f, Palette.Highlight * (0.2f + held * 0.3f));
            Gfx.CircleOutline(sb, HitX, RingY, RingTarget + 12f, Palette.Paper * 0.35f, 3f);
            if (held > 0f) Gfx.Arc(sb, HitX, RingY, RingTarget + 12f, -MathHelper.PiOver2, -MathHelper.PiOver2 + MathHelper.TwoPi * held, Palette.Highlight, 6f);
            NoteGlyph.FermataSign(sb, HitX, RingY - RingTarget - 34f, 20f, Palette.Highlight);
        }

        //Free Guide : lessons 1 to 4. A big arrow for the way to swing, the hold ring, the ruler.
        private void DrawFreeGuide(SpriteBatch sb, Lesson l)
        {
            if (l.Kind == Kind.Hold)
            {
                //Hold Ring : fills round the pointer while the button is down
                Vector2 at = Input.MousePos;
                float fill = MathHelper.Clamp(holdTime / HoldGoal, 0f, 1f);
                Gfx.CircleOutline(sb, at.X, at.Y, 34f, Palette.Ink * 0.5f, 4f);
                if (fill > 0f) Gfx.Arc(sb, at.X, at.Y, 34f, -MathHelper.PiOver2, -MathHelper.PiOver2 + MathHelper.TwoPi * fill, Palette.Ink, 5f);
                Gfx.TextSpacedCentered(sb, Game.Font, "HOLD", HitX, RingY - 8f, Palette.Highlight, TextSize.Label, 3f);
                return;
            }

            //Way : the stroke asked for, big in the well
            Flick want = FreeWay(l);
            float bob = (float)Math.Sin(time * 5f) * 5f;
            Vector2 d = NoteGlyph.Way(want);
            NoteGlyph.Arrow(sb, want, HitX + d.X * bob, RingY + d.Y * bob, 70f, 22f, Palette.Highlight, 10f);

            //Pattern : the four ways under the well, the one to do now lit
            if (l.Kind == Kind.Pattern)
                for (int i = 0; i < 4; i++)
                {
                    bool now = i == done % 4;
                    float cx = HitX - 90f + i * 60f;
                    if (now) Gfx.Circle(sb, cx, RingY + 118f, 22f, Palette.Void);
                    NoteGlyph.Arrow(sb, pattern[i], cx, RingY + 118f, 26f, 10f, now ? Palette.Highlight : Palette.InkSoft, 4f);
                }

            //Size : the order to give next
            if (l.Kind == Kind.Size && done < 3)
                Gfx.TextCentered(sb, Game.BigFont, sizeWordsBig[done], HitX, RingY + 104f, Palette.Ink, TextSize.Title);
        }

        //Ruler : while the baton is raised, a ruler the way the stroke should go. The size zones
        //show in the two lessons where the size is the point. Drawn over the card and footer.
        private void DrawRuler(SpriteBatch sb, Lesson l)
        {
            if (!gesture.Held || l.Kind == Kind.Hold || l.Kind == Kind.Finish || clearTimer >= 0f) return;
            if (rolling || holding || onSpark) return;
            if (l.Bpm > 0 && (!running || KindOf(pending) == NoteKind.None)) return;

            Vector2 d = NoteGlyph.Way(l.Bpm > 0 ? WayOf(pending) : FreeWay(l));
            Vector2 anchor = gesture.InStroke ? gesture.StrokeStart : Input.MousePos;
            float along = Vector2.Dot(gesture.LiveVector, d);
            bool zones = l.Kind == Kind.Size || l.Kind == Kind.Loudness;
            Baton.DrawRuler(sb, anchor, d, along, gesture.InStroke ? Baton.SizeOf(along) : -1, zones);
        }

        private static string[] sizeWordsBig = { "p", "mf", "f" };

        //Judge : the word for the last stroke on a dark plate under the hit point
        private void DrawJudge(SpriteBatch sb)
        {
            const float Life = 0.6f;
            if (judgeTimer >= Life || judgeWord.Length == 0) return;
            float t = judgeTimer / Life;
            float a = 1f - t * t;
            float y = RingY + 108f - t * 12f;
            float width = Gfx.TextWidth(Game.BigFont, judgeWord, 0.55f);
            Rectangle plate = new Rectangle((int)(HitX - width / 2f - 18f), (int)(y - 18f), (int)(width + 36f), 36);
            Gfx.SlantBox(sb, plate, 10, Palette.Void * (0.85f * a));
            Gfx.TextCentered(sb, Game.BigFont, judgeWord, HitX, y - 2f, Palette.Highlight * a, 0.55f);
            if (judgeTiming != 0)
                Gfx.TextSpacedCentered(sb, Game.Font, timingWords[judgeTiming + 1], HitX + judgeTiming * width * 0.5f, plate.Bottom + 3, Palette.Ink * a, TextSize.Tiny, 2f);
        }

        //Breath Bar : the KEEP BREATHING lesson's stamina, on a plate over the stage
        private static readonly Rectangle BreathPlate = new Rectangle(500, 112, 280, 56);
        private void DrawBreath(SpriteBatch sb)
        {
            int jolt = (int)((float)Math.Sin(time * 70f) * 6f * breathJolt);
            Rectangle plate = new Rectangle(BreathPlate.X + jolt, BreathPlate.Y, BreathPlate.Width, BreathPlate.Height);
            Gfx.SlantBox(sb, plate, Ui.Slant, Palette.Void * 0.9f);
            Gfx.TextSpaced(sb, Game.Font, "STAMINA", plate.X + 26, plate.Y + 10, breath <= 0.25f ? Palette.Highlight : Palette.LineGrey, TextSize.Tiny, 2f);
            Rectangle bar = new Rectangle(plate.X + 26, plate.Y + 32, 220, 10);
            if (breathJolt > 0f) Gfx.DrawGlowBox(sb, bar, Palette.Highlight * (0.6f * breathJolt));
            Ui.CapsuleBar(sb, bar, breath, Palette.Paper, 1f);
            if (breath <= 0.25f) Ornament.Vignette(sb, 140, 0.3f + BeatPulse() * 0.25f);
        }

        //Ready Stage : the last lesson, nothing to practise, TACET waiting
        private void DrawReadyStage(SpriteBatch sb)
        {
            Hollow.Eclipse(sb, 1110f, 330f, 110f, time * 0.2f, 1f);
            Gfx.TextCentered(sb, Game.LogoFont, "READY", 760f, 330f, Palette.Ink, TextSize.Banner);
            Ornament.Divider(sb, 760f, 392f, 140f, Palette.InkSoft);
            Gfx.TextSpacedCentered(sb, Game.Font, "THE SILENCE IS WAITING", 760f, 410f, Palette.InkSoft, TextSize.Label, 4f);
        }

        //Jump For Picture : DEVELOPER TOOL hook, lesson i already going, so a picture can show it
        public void JumpForPicture(int lesson, float beats)
        {
            StartLesson(lesson);
            cardIn = 1f;
            if (lessons[lesson].Bpm <= 0) return;
            running = true;
            clock = beats * beatLen;
            sent = (int)beats;
            ticked = (int)beats;
            pending = Math.Max(0, (int)beats - 4);
            if (lessons[lesson].Kind == Kind.Breath) breath = 0.2f;
        }
    }
}
