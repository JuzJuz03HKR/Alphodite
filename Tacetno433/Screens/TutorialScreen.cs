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
    //TutorialScreen : learning the baton one small step at a time, opened from the title menu.
    //
    //Every LESSON teaches one thing and waits until the player has done it. The card on the
    //left says what to do, the stage on the right is where it is done, and the GOAL pips on the
    //card fill up with every success. A finished lesson shows LESSON CLEAR and the next one
    //comes in. TAB skips a lesson, ESC goes back to the title. Nothing here can be lost.
    //
    //Round 15 (29 Sep) : playtesters found 13 lessons far too much at once, some gave up before
    //playing. Now there are six short BASICS, one idea each, and READY. Everything else (rolls,
    //held notes, sparks, the arrows over the players, the signature) is explained in the run
    //the first time it comes (DuelScreen.SetUpRound). Rolls, held notes and sparks can still be
    //practised here : READY offers them as EXTRA PRACTICE (TAB).
    //Lessons 1 to 3 have no beat : hold the button, draw the 4/4 shape, swing small, middle and big.
    //From lesson 4 a slow beat runs, started by holding the button, with the same ring, lane
    //and notes as the duel (drawn by the same Core helpers).
    //
    //THIS CLASS IS SPLIT OVER TWO FILES (partial, like DuelScreen) :
    //   TutorialScreen.cs           the lessons, their order, and the page around the stage
    //   TutorialScreen.Practice.cs  the beat, the notes, judging a stroke, and drawing the stage
    public partial class TutorialScreen : GameScreen
    {
        //Can Pause : a menu page outside the run, ESC here means going back
        public override bool CanPause
        {
            get { return false; }
        }

        //Lesson Kind : what the player has to do in a lesson
        private enum Kind { Hold, Pattern, Size, Timing, Loudness, Breath, Roll, Fermata, Spark, Finish }

        //Lesson : one step of the tutorial
        private class Lesson
        {
            public Kind Kind;
            public string Title = "";
            public string Body = "";
            public string Goal = "";
            public int Need;                // successes that finish it
            public int Bpm;                 // the beat, 0 for a lesson without one

            //Prepared Text
            public string BodyWrapped = "";
            public string GoalWrapped = "";
            public float BodyHeight;        // how tall the wrapped body is, so the card fits it
        }

        //Lessons : THE PLACE TO EDIT THE TUTORIAL. Keep each lesson to one idea and two short
        //sentences. The BASICS end at the first Finish lesson, the EXTRA PRACTICE comes after it.
        private static Lesson[] lessons =
        {
            new Lesson { Kind = Kind.Hold, Title = "RAISE THE BATON", Need = 1,
                Body = "The mouse is your baton. Hold the LEFT BUTTON down to raise it.",
                Goal = "Hold the left button down." },

            new Lesson { Kind = Kind.Pattern, Title = "THE 4/4 SHAPE", Need = 4,
                Body = "Keep holding, swing, then stop. Every bar goes DOWN, LEFT, RIGHT, UP, like a real conductor.",
                Goal = "Follow the arrow once round." },

            new Lesson { Kind = Kind.Size, Title = "SMALL, MIDDLE, BIG", Need = 3,
                Body = "How far you swing is how loud the band plays : short p, middle mf, long f. "
                     + "The ruler shows how far you have come.",
                Goal = "Swing down : short, then middle, then long." },

            new Lesson { Kind = Kind.Timing, Title = "ON THE BEAT", Need = 4, Bpm = 70,
                Body = "Now the band keeps time. Stop each stroke just as the ring closes.",
                Goal = "Four strokes GOOD or better." },

            new Lesson { Kind = Kind.Loudness, Title = "READ THE NOTE", Need = 6, Bpm = 76,
                Body = "TACET's notes slide in. Swing where the arrow points, as far as the letter says : "
                     + "f BIG, mf MIDDLE, p SMALL. Your whole band plays every note you hit.",
                Goal = "Six notes on time, the right size." },

            new Lesson { Kind = Kind.Breath, Title = "BREATH", Need = 8, Bpm = BattleRules.TempoByFloor[0][0],
                Body = "The bar at the top is the band's breath, its life. A note TACET wins takes some away. "
                     + "At zero the fight is lost. Where TACET is silent, let the beat pass to breathe.",
                Goal = "Play eight notes. Watch the bar." },

            new Lesson { Kind = Kind.Finish, Title = "READY", Need = 0,
                Body = "That is the whole baton. Everything else is explained in the run, the first time "
                     + "you meet it. TAB for extra practice : rolls, held notes and sparks.",
                Goal = "Start a run whenever you are ready." },

            //Extra Practice : after READY, only for players who ask for it (TAB)
            new Lesson { Kind = Kind.Roll, Title = "TREMOLO", Need = 1, Bpm = 76,
                Body = "A zigzag bar is a roll : elites and bosses end their rounds with one. Shake the baton as fast as you can.",
                Goal = "Six shakes in one roll." },

            new Lesson { Kind = Kind.Fermata, Title = "FERMATA", Need = 1, Bpm = 76,
                Body = "A note under an arch is held, from the second floor. Stroke it, then keep the baton STILL.",
                Goal = "Hold one fermata to the end." },

            new Lesson { Kind = Kind.Spark, Title = "THE SPARK", Need = 2, Bpm = 70,
                Body = "From the second floor a note can come tied to a spark. Flick once more, any way, half a beat later.",
                Goal = "Land two sparks." },

            new Lesson { Kind = Kind.Finish, Title = "ALL DONE", Need = 0,
                Body = "That was every note there is. The silence is waiting.",
                Goal = "Start a run whenever you are ready." },
        };

        //Page Layout : the card is as tall as its lesson needs, centred beside the stage
        private const int CardX = 40;
        private const int CardW = 420;
        private const int CardMiddle = 354;            // halfway between the header and the footer
        private Rectangle backButton = new Rectangle(40, 640, 210, 50);
        private Rectangle skipButton = new Rectangle(1030, 640, 210, 50);
        private Rectangle startButton = new Rectangle(800, 640, 210, 50);
        private const int FooterY = 618;
        private const int CardText = 68;               // left edge of the writing inside the card
        private const float CardWrap = 360f;
        private const float ClearTime = 1.4f;          // how long LESSON CLEAR stays up

        //Prepared Text : made once, so Draw never builds a string
        private static string[] lessonLabels;          // "LESSON 03 / 07", or "EXTRA 1 / 3"
        private static int basicsEnd;                  // the READY lesson, see BasicsEnd
        private static string[] lessonTags;            // "LESSON 03", or "EXTRA 1"
        private static Flick[] pattern = { Flick.Down, Flick.Left, Flick.Right, Flick.Up };
        private static string[] wayHints = { "", "SWING UP", "SWING DOWN", "SWING LEFT", "SWING RIGHT" };   // in Flick order
        private static string[] sizeHints =
        {
            "A SHORTER ONE : STOP BEFORE THE FIRST MARK",
            "IN BETWEEN : PAST THE FIRST MARK, NOT THE SECOND",
            "A LONGER ONE : ALL THE WAY PAST THE SECOND MARK"
        };
        private static string[] sizeDone = { "p, SOFT. NOW A MIDDLE ONE.", "mf. NOW A LONG ONE.", "f, LOUD." };
        private const string HoldHint = "KEEP HOLDING...";
        private const string NiceHint = "GOOD. AGAIN.";

        //Tutorial State
        private int index;                  // the lesson on show
        private int done;                   // successes so far in this lesson
        private float time;
        private float cardIn;               // 0 when a lesson has just arrived, 1 once it has slid in
        private float clearTimer = -1f;     // counting up after LESSON CLEAR, -1 while the lesson runs
        private float holdTime;             // lesson 1 : how long the button has been held
        private string hint = "";           // one line of feedback on the card, always a prepared string
        private float hintTimer;

        private GestureReader gesture = new GestureReader();
        private Baton baton = new Baton();
        private DuelEffects effects = new DuelEffects();

        public override void Load()
        {
            //Text Prepare : once for the whole game, the first time the tutorial opens
            if (lessonLabels == null)
            {
                lessonLabels = new string[lessons.Length];
                lessonTags = new string[lessons.Length];
                basicsEnd = BasicsEnd();
                int basics = basicsEnd + 1;
                int extras = lessons.Length - basics - 1;
                for (int i = 0; i < lessons.Length; i++)
                {
                    string number = (i + 1).ToString("00");
                    lessonLabels[i] = "LESSON " + number + " / " + basics.ToString("00");
                    lessonTags[i] = "LESSON " + number;
                    if (i >= basics)
                    {
                        int extra = Math.Min(i - basics + 1, extras);
                        lessonLabels[i] = "EXTRA " + extra + " / " + extras;
                        lessonTags[i] = "EXTRA " + extra;
                    }
                    lessons[i].BodyWrapped = Gfx.WrapText(Game.StoryFont, lessons[i].Body, CardWrap, TextSize.Story);
                    lessons[i].GoalWrapped = Gfx.WrapText(Game.StoryFont, lessons[i].Goal, CardWrap - 70f, TextSize.StorySmall);
                    lessons[i].BodyHeight = Game.StoryFont.MeasureString(lessons[i].BodyWrapped).Y * TextSize.Story;
                }
            }
            PreparePractice();

            //Baton Cursor : like the duel, the pointer is the baton while the tutorial is open
            Game.IsMouseVisible = false;
            baton.Reset(Input.MousePos);
            StartLesson(0);
            SoundBank.PlayMusic(Music.Title);
        }

        public override void Leave()
        {
            Game.IsMouseVisible = true;
        }

        //Lesson Start : a clean slate for lesson i
        private void StartLesson(int i)
        {
            index = i;
            done = 0;
            cardIn = 0f;
            clearTimer = -1f;
            holdTime = 0f;
            hint = "";
            gesture.Clear();
            effects.Clear();
            ResetPractice();
        }

        //Basics End : the READY lesson, the first Finish. Lessons after it are extra practice.
        private static int BasicsEnd()
        {
            for (int i = 0; i < lessons.Length; i++)
                if (lessons[i].Kind == Kind.Finish) return i;
            return lessons.Length - 1;
        }

        public override void Update(float dt)
        {
            time += dt;
            cardIn = Math.Min(1f, cardIn + dt * 3f);
            hintTimer -= dt;

            gesture.Update(dt, Input.MouseDown());
            baton.Update(dt, gesture.Held);
            effects.Update(dt);

            //Leave : ESC or the button, from any lesson
            if (Input.KeyPressed(Keys.Escape) || Input.ClickedOn(backButton))
            {
                SoundBank.Play(Sfx.UiBack);
                Game.Screens.Change(new TitleScreen());
                return;
            }

            Lesson l = lessons[index];
            if (l.Kind == Kind.Finish)
            {
                gesture.Read();
                if (Input.KeyPressed(Keys.Enter) || Input.ClickedOn(startButton)) StartRun();
                else if (index + 1 < lessons.Length && (Input.KeyPressed(Keys.Tab) || Input.ClickedOn(skipButton)))
                {
                    SoundBank.Play(Sfx.PageTurn);
                    StartLesson(index + 1);                    // EXTRA PRACTICE
                }
                return;
            }

            if (Input.KeyPressed(Keys.Tab) || Input.ClickedOn(skipButton))
            {
                SoundBank.Play(Sfx.PageTurn);
                StartLesson(index + 1);
                return;
            }

            //Lesson Clear : a moment to enjoy it, then the next lesson (ENTER to hurry)
            if (clearTimer >= 0f)
            {
                clearTimer += dt;
                gesture.Read();                        // strokes during the stamp never count
                if (clearTimer >= ClearTime || Input.KeyPressed(Keys.Enter)) StartLesson(index + 1);
                return;
            }

            if (l.Bpm > 0) UpdateBeat(dt, l);
            else UpdateFree(dt, l);

            if (done >= l.Need) Clear();
        }

        //Lesson Clear : the goal is reached
        private void Clear()
        {
            clearTimer = 0f;
            StopBeat();
            effects.SpawnFlare(HitX, RingY, 1.2f);
            effects.SpawnSparks(HitX, RingY, 24, 0f);
            SoundBank.Play(Sfx.ComboUp, 1f, 0.5f);
        }

        //Start Run : the last lesson hands over to a real run (the title asks first if one is saved)
        private void StartRun()
        {
            SoundBank.Play(Sfx.UiConfirm);
            if (SaveFile.HasRun) Game.Screens.Change(new TitleScreen());
            else Game.Screens.Change(new ConductorSelectScreen());
        }

        //Hint : one line of feedback on the card for a couple of seconds
        private void Hint(string line)
        {
            hint = line;
            hintTimer = 2.2f;
        }

        //Free Lessons : the lessons without a beat, just strokes
        private void UpdateFree(float dt, Lesson l)
        {
            //Hold : the ring round the baton fills while the button stays down
            if (l.Kind == Kind.Hold)
            {
                gesture.Read();
                if (Input.MouseDown())
                {
                    holdTime += dt;
                    if (hintTimer <= 0f) Hint(HoldHint);
                }
                else holdTime = Math.Max(0f, holdTime - dt * 2f);
                if (holdTime >= HoldGoal) done = 1;
                return;
            }

            if (!gesture.Read()) return;
            baton.Whip(gesture.Direction);
            Flick want = FreeWay(l);
            int size = Baton.SizeOf(gesture.Length);

            if (gesture.Direction != want)
            {
                Hint(wayHints[(int)want]);
                effects.SpawnHit(HitX, RingY, Grade.Miss);
                SoundBank.Play(Sfx.QteMiss);
                return;
            }

            //Size : the three sizes in order, small first
            if (l.Kind == Kind.Size && size != done)
            {
                Hint(sizeHints[done]);
                effects.SpawnHit(HitX, RingY, Grade.Miss);
                SoundBank.Play(Sfx.QteMiss);
                return;
            }

            if (l.Kind == Kind.Size) Hint(sizeDone[done]);
            else Hint(NiceHint);
            done++;
            effects.SpawnHit(HitX, RingY, Grade.Perfect);
            if (size == 2) effects.SpawnSlash(gesture.From, gesture.To, 1f);
            SoundBank.Play(Sfx.QtePerfect);
        }

        //Free Way : the stroke a free lesson is asking for right now
        private Flick FreeWay(Lesson l)
        {
            if (l.Kind == Kind.Pattern) return pattern[done % 4];
            return Flick.Down;
        }

        private const float HoldGoal = 0.8f;

        public override void Draw(SpriteBatch sb)
        {
            Lesson l = lessons[index];

            //Stage : the bright side, TACET's dark on the right, like the duel
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageLight);
            Gfx.DrawGlow(sb, 640, 360, 520f, Color.White * 0.5f);
            TacetField.Draw(sb, 1010f + (float)Math.Sin(time * 0.6f) * 12f, time, 0.2f);

            DrawStage(sb, l);                   // see TutorialScreen.Practice.cs
            DrawHeader(sb, l);
            DrawCard(sb, l);
            DrawFooter(sb, l);
            if (clearTimer >= 0f) DrawClear(sb);

            DrawRuler(sb, l);
            baton.Draw(sb, gesture);
        }

        //Header : the lesson's name on the left plate, the progress on the right one
        private void DrawHeader(SpriteBatch sb, Lesson l)
        {
            Gfx.SlantBox(sb, new Rectangle(-20, 10, 560, 80), -Ui.Slant, Palette.Void * 0.9f);
            Gfx.TextSpaced(sb, Game.Font, "TUTORIAL", 24, 22, Palette.LineGrey, TextSize.Tiny, 4f);
            Gfx.TextSpaced(sb, Game.Font, lessonLabels[index], 150, 22, Palette.PaperDim, TextSize.Tiny, 2f);
            Gfx.Text(sb, Game.BigFont, l.Title, 22, 42, Palette.Highlight, TextSize.Subtitle);

            Gfx.SlantBox(sb, new Rectangle(860, 10, 440, 80), Ui.Slant, Palette.Void * 0.9f);
            Gfx.TextSpaced(sb, Game.Font, "PROGRESS", 900, 22, Palette.LineGrey, TextSize.Tiny, 4f);
            //Pips : the basics up to READY, or only the extras once past it, so a newcomer sees
            //the seven steps the label counts, not the extras too (round 15). ALL DONE, the very
            //last lesson, is not counted, the same as in the label.
            int first = index <= basicsEnd ? 0 : basicsEnd + 1;
            int last = index <= basicsEnd ? basicsEnd : Math.Max(first, lessons.Length - 2);
            float gap = 330f / Math.Max(1, last - first);
            Gfx.Rect(sb, 904, 58, 330, 1, Palette.LineGrey);
            for (int i = first; i <= last; i++)
            {
                float x = 904 + (i - first) * gap;
                if (i < index) Gfx.Diamond(sb, x, 58, 5, Palette.Paper);
                else if (i == index) { Gfx.Diamond(sb, x, 58, 8, Palette.Highlight); Gfx.DiamondOutline(sb, x, 58, 12, Palette.Paper, 1f); }
                else Gfx.DiamondOutline(sb, x, 58, 5, Palette.LineGrey, 1f);
            }
        }

        //Card : what to do, sliding in from the left when the lesson starts. From the top : the
        //lesson tag, the title, the body, then the goal with its pips and the last hint.
        private void DrawCard(SpriteBatch sb, Lesson l)
        {
            float e = cardIn * cardIn * (3f - 2f * cardIn);
            int shift = (int)((1f - e) * -60f);
            int height = (int)l.BodyHeight + (l.Need > 0 ? 282 : 222);     // no pips, a shorter card
            Rectangle r = new Rectangle(CardX + shift, Math.Max(108, CardMiddle - height / 2), CardW, height);
            int x = CardText + shift;

            Gfx.Rect(sb, new Rectangle(r.X + 6, r.Y + 8, r.Width, r.Height), Color.Black * (0.35f * e));
            Gfx.Rect(sb, r, Palette.Void * (0.93f * e));
            Ornament.DoubleFrame(sb, r, Palette.PaperDim * e);

            Ui.Tag(sb, lessonTags[index], x, r.Y + 26, true, e);
            Gfx.Text(sb, Game.BigFont, l.Title, x, r.Y + 54, Palette.Highlight * e, TextSize.Title);
            Ornament.Rule(sb, x, r.Y + 100, CardWrap, Palette.LineGrey * e);
            Gfx.Text(sb, Game.StoryFont, l.BodyWrapped, x, r.Y + 118, Palette.Paper * e, TextSize.Story);

            //Goal : the tag, the sentence, and a pip for every success still to make
            int goalY = r.Y + 118 + (int)l.BodyHeight + 44;
            Ornament.Rule(sb, x, goalY - 16, CardWrap, Palette.LineGrey * (0.6f * e));
            Ui.Tag(sb, "GOAL", x, goalY, false, e);
            Gfx.Text(sb, Game.StoryFont, l.GoalWrapped, x + 62, goalY - 1, Palette.Highlight * e, TextSize.StorySmall);
            if (l.Need > 0)
            {
                float pipGap = Math.Min(30f, CardWrap / l.Need);
                Ui.Pips(sb, x + 8, goalY + 52, Math.Min(done, l.Need), l.Need, 7, pipGap, e);
            }

            //Hint : feedback on the last try, fading out
            if (hintTimer > 0f && hint.Length > 0)
            {
                float a = Math.Min(1f, hintTimer * 2f) * e;
                Gfx.TextSpaced(sb, Game.Font, hint, x, goalY + 78, Palette.Paper * a, TextSize.Tiny, 1.5f);
            }
        }

        //Footer : the way back on the left, what to do next in the middle, skip on the right
        private void DrawFooter(SpriteBatch sb, Lesson l)
        {
            Gfx.Rect(sb, 0, FooterY, TacetGame.ScreenW, TacetGame.ScreenH - FooterY, Palette.Void * 0.92f);
            Gfx.Rect(sb, 0, FooterY, TacetGame.ScreenW, 1, Palette.LineGrey * 0.5f);
            Ui.Button(sb, backButton, "TITLE", "ESC", false);

            if (l.Kind == Kind.Finish)
            {
                Ui.Button(sb, startButton, SaveFile.HasRun ? "TO THE TITLE" : "NEW RUN", "ENTER", true);
                if (index + 1 < lessons.Length) Ui.Button(sb, skipButton, "EXTRAS", "TAB", false);
                return;
            }

            Ui.Button(sb, skipButton, "SKIP", "TAB", false);

            //Prompt : the one thing to do right now
            string prompt = "HOLD THE LEFT BUTTON  /  SWING  /  STOP";
            bool waiting = l.Bpm > 0 && !running && clearTimer < 0f;
            if (waiting) prompt = "HOLD THE LEFT BUTTON TO START THE BEAT";
            float blink = waiting ? 0.6f + 0.4f * (float)Math.Sin(time * 5f) : 1f;
            Gfx.TextSpacedCentered(sb, Game.Font, prompt, 640, FooterY + 34, Palette.Paper * blink, TextSize.Label, 3f);
        }

        //Clear Stamp : a band across the stage, the way the duel names a round
        private void DrawClear(SpriteBatch sb)
        {
            float t = clearTimer / ClearTime;
            float a = t < 0.15f ? t / 0.15f : (t > 0.85f ? (1f - t) / 0.15f : 1f);
            Gfx.Rect(sb, 480, 300, TacetGame.ScreenW - 480, 144, Color.Black * (0.82f * a));
            Gfx.Rect(sb, 480, 300, TacetGame.ScreenW - 480, 1, Palette.Paper * a);
            Gfx.Rect(sb, 480, 443, TacetGame.ScreenW - 480, 1, Palette.Paper * a);
            Gfx.TextSpacedCentered(sb, Game.Font, lessonTags[index], 880, 318, Palette.PaperDim * a, TextSize.Tiny, 4f);
            Gfx.TextCentered(sb, Game.LogoFont, "LESSON CLEAR", 880 + (1f - a) * 30f, 372, Palette.Highlight * a, TextSize.Banner * 0.7f);
            Ornament.Divider(sb, 880, 420, 120, Palette.PaperDim * a);
        }
    }
}
