using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //SettingsScreen : the options page, opened from the title menu or from the pause menu.
    //
    //Six rows: three volume sliders, the language choice, a full screen tick box, and the
    //stroke timing. Up and down pick a row, left and right change it, and the mouse can drag
    //the sliders or click the chips. The panel on the right explains whichever row is chosen.
    //
    //STROKE TIMING has a test: a flash (and a tick) every beat, the player strokes on each one,
    //and the page works out how early or late their strokes land. See Settings.TimingOffset.
    //
    //The numbers themselves live in Core/Settings.cs. They are saved to a file (SaveFile)
    //as soon as the page closes.
    public class SettingsScreen : GameScreen
    {
        //Can Pause : a menu page outside the run, ESC here means going back
        public override bool CanPause
        {
            get { return false; }
        }

        //Setting Rows
        private const int RowMaster = 0;
        private const int RowSfx = 1;
        private const int RowMusic = 2;
        private const int RowLanguage = 3;
        private const int RowFull = 4;
        private const int RowTiming = 5;
        private const int RowCount = 6;

        //Settings Layout
        private const int PanelX = 80;
        private const int PanelY = 140;
        private const int PanelW = 700;
        private const int PanelH = 440;
        private const int RowX = 96;
        private const int RowY = 164;
        private const int RowStep = 68;
        private const int RowW = 668;
        private const int RowH = 56;
        private const int TrackX = 430;
        private const int TrackW = 260;
        private const int ValueRight = 762;
        private const int HelpWrap = 340;

        private static string[] rowIndex = { "01", "02", "03", "04", "05", "06" };
        private static string[] rowNames =
        {
            "MASTER VOLUME",
            "SOUND EFFECTS",
            "MUSIC",
            "LANGUAGE",
            "FULL SCREEN",
            "STROKE TIMING"
        };

        //Row Help : the sentence shown on the right for whichever row is chosen
        private static string[] rowHelp =
        {
            "The main fader. Every sound in the game is measured against this one.",
            "Button clicks, baton strokes, and every clash in a duel.",
            "The track playing behind the page you are on.",
            "Which language the game is written in. Only English is written so far, so the choice is remembered but the words do not change yet.",
            "Fills the whole monitor without changing its resolution, so other programs are left where they are and alt tab is instant. The picture keeps its shape and black bars fill whatever is left over.",
            "If your strokes feel right on the beat but the duel calls them late, move this to the right. Early, to the left. Press ENTER or TEST and the game measures it for you."
        };
        private static string savedNote =
            "Options are saved the moment you leave this page, and come back the next time the game starts.";
        private static string testNote =
            "Hold the left button and stroke the baton, any way, on every flash. The first two strokes are practice.";

        private string[] helpWrapped = new string[RowCount];
        private string savedWrapped = "";
        private string testWrapped = "";

        //Setting Boxes : worked out once in Load
        private Rectangle[] rowBoxes = new Rectangle[RowCount];
        private Rectangle[] tracks = new Rectangle[3];      // the drawn rail of each slider
        private Rectangle[] grabs = new Rectangle[3];       // the easier area the mouse can grab
        private Rectangle[] langBoxes = new Rectangle[2];
        private Rectangle fullBox;
        private Rectangle timingTrack;
        private Rectangle timingGrab;
        private Rectangle testChip;
        private Rectangle helpBox = new Rectangle(820, 140, 380, 240);
        private Rectangle backButton = new Rectangle(80, 596, 260, 50);

        private int selected;
        private int dragging = -1;      // which slider the mouse is holding, -1 for none
        private int lastStep = -1;      // used to click only when a volume moves a whole step
        private float hold;             // how long an arrow key has been held
        private float repeat;
        private float time;
        private float enter;
        private Vector2 lastMouse;

        //From Pause : the pause menu borrows this page. BACK then only closes it (Closed turns
        //true and the pause menu takes over again) instead of going to the title page.
        private bool fromPause;
        public bool Closed;

        //Timing Test : a metronome at 100 beats a minute. Every stroke is matched to the
        //nearest tick, and the middle of the measured strokes becomes the setting.
        private const float TestBeat = 0.6f;
        private const float TestLeadIn = 1.2f;      // two quiet beats before the first tick
        private const int TestPractice = 2;         // strokes not counted, the hand finds the beat
        private const int TestCount = 8;            // strokes that are counted
        private GestureReader testGesture = new GestureReader();
        private bool testing;
        private float testClock;
        private int testTicks;
        private int testStrokes;
        private int testRecorded;
        private float testFlash;
        private float[] testOffsets = new float[TestCount];
        private string testResult = "";             // made once, when a test ends

        public SettingsScreen()
        {
        }

        public SettingsScreen(bool fromPause)
        {
            this.fromPause = fromPause;
        }

        public override void Load()
        {
            for (int i = 0; i < RowCount; i++)
                rowBoxes[i] = new Rectangle(RowX, RowY + i * RowStep, RowW, RowH);

            for (int i = 0; i < 3; i++)
            {
                int cy = rowBoxes[i].Center.Y;
                tracks[i] = new Rectangle(TrackX, cy - 8, TrackW, 16);
                grabs[i] = new Rectangle(TrackX - 12, cy - 18, TrackW + 24, 36);
            }

            int lang = rowBoxes[RowLanguage].Center.Y;
            langBoxes[0] = new Rectangle(TrackX, lang - 17, 122, 34);
            langBoxes[1] = new Rectangle(TrackX + 134, lang - 17, 122, 34);
            fullBox = new Rectangle(TrackX, rowBoxes[RowFull].Center.Y - 13, 26, 26);

            int timing = rowBoxes[RowTiming].Center.Y;
            timingTrack = new Rectangle(TrackX, timing - 8, 160, 16);
            timingGrab = new Rectangle(TrackX - 12, timing - 18, 184, 36);
            testChip = new Rectangle(ValueRight - 66, timing - 17, 66, 34);

            //Help Text : wrapped once here, never while drawing
            for (int i = 0; i < RowCount; i++)
                helpWrapped[i] = Gfx.WrapText(Game.StoryFont, rowHelp[i], HelpWrap, TextSize.StorySmall);
            savedWrapped = Gfx.WrapText(Game.StoryFont, savedNote, HelpWrap, TextSize.StorySmall);
            testWrapped = Gfx.WrapText(Game.StoryFont, testNote, HelpWrap, TextSize.StorySmall);

            lastMouse = Input.MousePos;
            if (!fromPause) SoundBank.PlayMusic(Music.Title);
        }

        public override void Update(float dt)
        {
            time += dt;
            enter = Math.Min(1f, enter + dt * 2.5f);

            //Timing Test : takes over the page until it ends or is stopped
            if (testing)
            {
                UpdateTest(dt);
                return;
            }

            int before = selected;

            //Row Choice : keyboard
            if (Input.KeyPressed(Keys.Down) || Input.KeyPressed(Keys.S)) selected = (selected + 1) % RowCount;
            if (Input.KeyPressed(Keys.Up) || Input.KeyPressed(Keys.W)) selected = (selected - 1 + RowCount) % RowCount;

            //Row Choice : mouse, only when it actually moves, so a resting cursor does not
            //fight the arrow keys
            if (Input.MousePos != lastMouse)
            {
                lastMouse = Input.MousePos;
                for (int i = 0; i < RowCount; i++)
                    if (Input.MouseOver(rowBoxes[i])) selected = i;
            }

            if (selected != before)
            {
                SoundBank.Play(Sfx.UiMove);
                lastStep = -1;
            }

            //Value Change : one step per press, then a repeat while the key is held
            if (Input.KeyPressed(Keys.Left) || Input.KeyPressed(Keys.A)) Nudge(selected, -1);
            if (Input.KeyPressed(Keys.Right) || Input.KeyPressed(Keys.D)) Nudge(selected, 1);
            if (Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Space)) Press(selected);
            UpdateHold(dt);

            UpdateMouse();

            if (Input.KeyPressed(Keys.Escape) || Input.KeyPressed(Keys.Back) || Input.ClickedOn(backButton))
            {
                SoundBank.Play(Sfx.UiBack);
                SaveFile.SaveSettings();
                if (fromPause) Closed = true;
                else Game.Screens.Change(new TitleScreen());
            }
        }

        //Begin Test : DEVELOPER TOOL hook, so the capture tool can picture the test running
        public void BeginTest()
        {
            StartTest();
        }

        //Test Start : two quiet beats, then the flashes begin
        private void StartTest()
        {
            testing = true;
            testClock = -TestLeadIn;
            testTicks = 0;
            testStrokes = 0;
            testRecorded = 0;
            testFlash = 0f;
            testResult = "";
            testGesture.Clear();
            selected = RowTiming;
            SoundBank.Play(Sfx.UiConfirm);
        }

        //Test Update : tick on every beat, and match every stroke to its nearest tick
        private void UpdateTest(float dt)
        {
            testClock += dt;
            testFlash = Math.Max(0f, testFlash - dt * 4f);
            testGesture.Update(dt, Input.MouseDown());

            //Tick : a flash the eye can follow even with the sound off
            if (testClock >= 0f)
            {
                int due = (int)(testClock / TestBeat);
                while (testTicks <= due)
                {
                    testTicks++;
                    testFlash = 1f;
                    SoundBank.Play(Sfx.BeatTick, 1f, 0f);
                }
            }

            //Stroke : how far it landed from the nearest tick, in seconds, late is positive
            if (testGesture.Read() && testClock > -TestBeat * 0.5f)
            {
                float nearest = (float)Math.Round(testClock / TestBeat) * TestBeat;
                testStrokes++;
                if (testStrokes > TestPractice && testRecorded < TestCount)
                {
                    testOffsets[testRecorded] = testClock - nearest;
                    testRecorded++;
                    SoundBank.Play(Sfx.UiMove);
                }
                if (testRecorded >= TestCount) FinishTest();
            }

            //Stop : the setting stays as it was
            if (Input.KeyPressed(Keys.Escape) || Input.MouseRightClicked())
            {
                testing = false;
                SoundBank.Play(Sfx.UiBack);
            }
        }

        //Test Finish : sort the strokes, drop the earliest and the latest (a slip of the hand
        //should not count), and use the average of the rest
        private void FinishTest()
        {
            float[] sorted = (float[])testOffsets.Clone();
            Array.Sort(sorted);
            float total = 0f;
            for (int i = 1; i < sorted.Length - 1; i++) total += sorted[i];
            float middle = total / (sorted.Length - 2);

            Settings.SetTiming(middle);
            SaveFile.SaveSettings();
            testing = false;
            testResult = "MEASURED  " + Settings.TimingLabel(middle) + "   /   SET TO  " + Settings.TimingLabel(Settings.TimingOffset);
            SoundBank.Play(Sfx.ComboUp);
        }

        //Key Hold : holding left or right keeps a slider sliding after a short wait
        private void UpdateHold(float dt)
        {
            int dir = 0;
            if (Input.KeyDown(Keys.Left) || Input.KeyDown(Keys.A)) dir = -1;
            if (Input.KeyDown(Keys.Right) || Input.KeyDown(Keys.D)) dir = 1;

            if (dir == 0 || (selected > RowMusic && selected != RowTiming))
            {
                hold = 0f;
                repeat = 0f;
                return;
            }

            hold += dt;
            if (hold < 0.4f) return;

            repeat -= dt;
            if (repeat > 0f) return;
            repeat = 0.05f;
            if (selected == RowTiming) Settings.SetTiming(Settings.TimingOffset + dir * Settings.TimingStep);
            else SetVolume(selected, VolumeOf(selected) + dir * 0.02f);
        }

        //Mouse : grab a slider and hold it, or click one of the chips
        private void UpdateMouse()
        {
            if (Input.MouseClicked())
            {
                for (int i = 0; i < 3; i++)
                    if (Input.MouseOver(grabs[i])) { dragging = i; selected = i; lastStep = -1; }

                if (Input.MouseOver(langBoxes[0])) { selected = RowLanguage; SetLanguage(0); }
                if (Input.MouseOver(langBoxes[1])) { selected = RowLanguage; SetLanguage(1); }
                if (Input.MouseOver(fullBox)) { selected = RowFull; SetFullscreen(!Settings.Fullscreen); }
                if (Input.MouseOver(timingGrab)) { dragging = RowTiming; selected = RowTiming; }
                if (Input.MouseOver(testChip)) { StartTest(); return; }
            }

            if (!Input.MouseDown()) dragging = -1;

            if (dragging == RowTiming)
            {
                float t = (Input.MousePos.X - timingTrack.X) / timingTrack.Width;
                Settings.SetTiming((t * 2f - 1f) * Settings.TimingMost);
            }
            else if (dragging >= 0)
            {
                Rectangle t = tracks[dragging];
                SetVolume(dragging, (Input.MousePos.X - t.X) / t.Width);
            }
        }

        //Arrow Press : what left and right mean on each row
        private void Nudge(int row, int dir)
        {
            if (row <= RowMusic) SetVolume(row, VolumeOf(row) + dir * 0.05f);
            else if (row == RowLanguage) SetLanguage(dir < 0 ? 0 : 1);
            else if (row == RowTiming) Settings.SetTiming(Settings.TimingOffset + dir * Settings.TimingStep);
            else SetFullscreen(dir > 0);
        }

        //Enter Press : the two rows that are simply on or off flip over
        private void Press(int row)
        {
            if (row == RowLanguage) SetLanguage(Settings.Language == 0 ? 1 : 0);
            else if (row == RowFull) SetFullscreen(!Settings.Fullscreen);
            else if (row == RowTiming) StartTest();
        }

        private float VolumeOf(int row)
        {
            if (row == RowMaster) return Settings.Master;
            if (row == RowSfx) return Settings.Sfx;
            return Settings.Music;
        }

        //Volume Set : clamp, store, and let the music hear the change straight away
        private void SetVolume(int row, float value)
        {
            if (value < 0f) value = 0f;
            if (value > 1f) value = 1f;

            if (row == RowMaster) Settings.Master = value;
            else if (row == RowSfx) Settings.Sfx = value;
            else Settings.Music = value;

            SoundBank.ApplyVolume();

            //Level Check : click once every five percent so the player can hear the level.
            //The music row is left alone, its own track is the check.
            int step = (int)(value * 20f);
            if (step != lastStep)
            {
                lastStep = step;
                if (row != RowMusic) SoundBank.Play(Sfx.UiMove);
            }
        }

        private void SetLanguage(int id)
        {
            if (Settings.Language == id) return;
            Settings.Language = id;
            SoundBank.Play(Sfx.UiConfirm);
        }

        private void SetFullscreen(bool on)
        {
            if (Settings.Fullscreen == on) return;
            Settings.Fullscreen = on;
            Game.ApplyFullscreen();
            SoundBank.Play(Sfx.UiConfirm);
        }

        public override void Draw(SpriteBatch sb)
        {
            float e = enter * enter * (3f - 2f * enter);

            DrawBackground(sb);

            //Page Head
            Gfx.TextSpaced(sb, Game.Font, "TACET 4'33   /   OPTIONS", 82, 38, Palette.LineGrey, TextSize.Tiny, 4f);
            Gfx.Text(sb, Game.LogoFont, "SETTINGS", 78, 52, Palette.Paper * e, TextSize.Banner);
            Ornament.Rule(sb, 80, 124, 700, Palette.LineGrey * e);

            //Rows
            Ui.Panel(sb, new Rectangle(PanelX, PanelY, PanelW, PanelH), e);
            for (int i = 0; i < RowCount; i++) DrawRow(sb, i, e);

            if (testing) DrawTest(sb, e);
            else DrawHelp(sb, e);

            Ui.Button(sb, backButton, fromPause ? "BACK TO PAUSE" : "BACK", "ESC", false, enter >= 1f, e);

            Gfx.TextSpaced(sb, Game.Font, "UP DOWN TO CHOOSE  /  LEFT RIGHT TO CHANGE  /  MOUSE CAN DRAG THE SLIDERS", 82, 664, Palette.LineGrey, TextSize.Tiny, 2f);
        }

        //Background : the same dark stage as the title page, so the two feel like one place
        private void DrawBackground(SpriteBatch sb)
        {
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageDeep);
            Gfx.DrawGlowBox(sb, new Rectangle(-300, -200, 1300, 900), Palette.Paper * 0.07f);
            Gfx.Rect(sb, 60, 30, 1, TacetGame.ScreenH - 60, Palette.Paper * 0.08f);
            Gfx.Rect(sb, 60, 30, 1140, 1, Palette.Paper * 0.08f);
            Ornament.Crosses(sb, 1180, 44, 1, 1, 16, Palette.LineGrey);

            //TACET : a thin band of silence still waiting at the right edge
            TacetField.Draw(sb, 1224f + (float)Math.Sin(time * 0.5f) * 8f, time, 0.1f);

            Ornament.Vignette(sb, 60, 0.55f);
        }

        //Row : index, name, and whatever control that row uses
        private void DrawRow(SpriteBatch sb, int i, float e)
        {
            Rectangle box = rowBoxes[i];
            bool on = (i == selected);
            float a = MathHelper.Clamp(e * 2f - i * 0.12f, 0f, 1f);

            //Chosen Row : a slanted plate behind it, a bar down the left, a diamond in the margin
            if (on)
            {
                Gfx.SlantBox(sb, new Rectangle(box.X - 10, box.Y, box.Width + 16, box.Height), Ui.Slant, Palette.Stage * (0.92f * a));
                Gfx.Rect(sb, box.X - 10, box.Y, 3, box.Height, Palette.Accent * a);
                float bob = (float)Math.Sin(time * 6f) * 2f;
                Gfx.Diamond(sb, box.X - 26 + bob, box.Center.Y, 5, Palette.Accent * a);
            }
            else
            {
                Gfx.Rect(sb, box.X, box.Bottom - 1, box.Width, 1, Palette.LineGrey * (0.25f * a));
            }

            Gfx.Text(sb, Game.Font, rowIndex[i], box.X + 14, box.Y + 23, (on ? Palette.Accent : Palette.LineGrey) * a, TextSize.Label);
            Gfx.TextSpaced(sb, Game.Font, rowNames[i], box.X + 48, box.Y + 19, (on ? Palette.Paper : Palette.PaperDim) * a, TextSize.Heading, 2f);

            if (i <= RowMusic) DrawVolumeRow(sb, i, on, a);
            else if (i == RowLanguage) DrawLanguageRow(sb, on, a);
            else if (i == RowFull) DrawFullscreenRow(sb, on, a);
            else DrawTimingRow(sb, on, a);
        }

        //Timing Row : a slider with the middle marked, the value, and the TEST chip
        private void DrawTimingRow(SpriteBatch sb, bool on, float a)
        {
            bool hot = on || dragging == RowTiming || Input.MouseOver(timingGrab);
            float value = (Settings.TimingOffset / Settings.TimingMost + 1f) / 2f;
            Ui.Slider(sb, timingTrack, value, hot, a);
            Gfx.Rect(sb, timingTrack.Center.X, timingTrack.Y - 5, 1, timingTrack.Height + 10, Palette.PaperDim * a);

            Gfx.TextRight(sb, Game.Font, Settings.TimingLabel(Settings.TimingOffset), testChip.X - 12, timingTrack.Y - 2, (on ? Palette.Highlight : Palette.Paper) * a, TextSize.Body);
            DrawChip(sb, testChip, "TEST", false, on, a);
        }

        //Test Panel : in place of the help while the test runs. A ring flashes on every beat,
        //the marks along the ruler are the strokes so far, early to the left, late to the right.
        private void DrawTest(SpriteBatch sb, float e)
        {
            Ui.Panel(sb, helpBox, e);
            Ui.Header(sb, "", "Stroke timing test", helpBox.X + 20, helpBox.Y + 26, helpBox.Width - 40, e);

            float cx = helpBox.Center.X;
            float cy = helpBox.Y + 112;
            Gfx.DrawGlow(sb, cx, cy, 40f + testFlash * 50f, Palette.Highlight * (0.15f + testFlash * 0.6f));
            Hollow.Ring(sb, cx, cy, 24f + testFlash * 8f, 2f + testFlash * 3f, 0.5f + testFlash * 0.5f);
            Gfx.TextSpacedCentered(sb, Game.Font, testStrokes < TestPractice ? "PRACTICE" : "COUNTING", cx, cy + 42, Palette.PaperDim * e, TextSize.Tiny, 3f);
            Ui.Pips(sb, cx - (TestCount - 1) * 7f, cy + 66, testRecorded, TestCount, 4, 14, e);

            //Ruler : -150 to +150 milliseconds, the middle is right on the beat
            float left = helpBox.X + 40;
            float width = helpBox.Width - 80;
            float ry = helpBox.Bottom - 30;
            Gfx.Rect(sb, left, ry, width, 1, Palette.LineGrey * e);
            Gfx.Rect(sb, left + width / 2f, ry - 8, 1, 16, Palette.Paper * e);
            Gfx.TextSpaced(sb, Game.Font, "EARLY", left, ry + 8, Palette.LineGrey * e, TextSize.Tiny, 2f);
            Gfx.TextSpacedRight(sb, Game.Font, "LATE", left + width, ry + 8, Palette.LineGrey * e, TextSize.Tiny, 2f);
            for (int i = 0; i < testRecorded; i++)
            {
                float t = MathHelper.Clamp(testOffsets[i] / Settings.TimingMost, -1f, 1f);
                Gfx.Rect(sb, left + width / 2f + t * width / 2f - 1f, ry - 10, 3, 20, Palette.Highlight * e);
            }

            Gfx.Text(sb, Game.StoryFont, testWrapped, helpBox.X, helpBox.Bottom + 24, Palette.PaperDim * e, TextSize.StorySmall);
            Gfx.TextSpaced(sb, Game.Font, "ESC OR RIGHT CLICK TO STOP", helpBox.X, helpBox.Bottom + 96, Palette.LineGrey * e, TextSize.Tiny, 2f);
        }

        //Volume Row : the slider and the number beside it
        private void DrawVolumeRow(SpriteBatch sb, int i, bool on, float a)
        {
            float value = VolumeOf(i);
            bool hot = on || dragging == i || Input.MouseOver(grabs[i]);

            Ui.Slider(sb, tracks[i], value, hot, a);
            Gfx.TextRight(sb, Game.BigFont, Settings.Percent(value), ValueRight, tracks[i].Center.Y - 15, (on ? Palette.Highlight : Palette.Paper) * a, TextSize.Subtitle);
        }

        //Language Row : two chips, the chosen one filled in
        private void DrawLanguageRow(SpriteBatch sb, bool on, float a)
        {
            for (int i = 0; i < 2; i++)
                DrawChip(sb, langBoxes[i], Settings.LanguageNames[i], Settings.Language == i, on, a);
        }

        //Full Screen Row : a tick box and the word beside it
        private void DrawFullscreenRow(SpriteBatch sb, bool on, float a)
        {
            bool hot = on || Input.MouseOver(fullBox);
            Ui.CheckBox(sb, fullBox, Settings.Fullscreen, hot, a);
            Gfx.TextSpaced(sb, Game.Font, Settings.Fullscreen ? "ON" : "OFF", fullBox.Right + 16, fullBox.Y + 4, (on ? Palette.Paper : Palette.PaperDim) * a, TextSize.Body, 2f);
            Gfx.TextSpacedRight(sb, Game.Font, "BORDERLESS  WINDOW", ValueRight, fullBox.Y + 7, Palette.LineGrey * a, TextSize.Tiny, 2f);
        }

        //Chip : a small slanted blade, used for the language choice
        private void DrawChip(SpriteBatch sb, Rectangle box, string text, bool chosen, bool rowOn, float a)
        {
            bool over = Input.MouseOver(box);

            if (chosen)
            {
                Gfx.SlantBox(sb, box, 8, (rowOn ? Palette.Paper : Palette.PaperDim) * a);
                Gfx.TextSpacedCentered(sb, Game.Font, text, box.Center.X, box.Y + 10, Palette.Ink * a, TextSize.Label, 2f);
            }
            else
            {
                Gfx.SlantOutline(sb, box, 8, (over ? Palette.Highlight : Palette.LineGrey) * a, 1f);
                Gfx.TextSpacedCentered(sb, Game.Font, text, box.Center.X, box.Y + 10, (over ? Palette.Paper : Palette.LineGrey) * a, TextSize.Label, 2f);
            }
        }

        //Help : what the chosen row does, plus the warning that nothing is saved
        private void DrawHelp(SpriteBatch sb, float e)
        {
            Ui.Panel(sb, helpBox, e);
            Ui.Header(sb, "", "What this does", helpBox.X + 20, helpBox.Y + 26, helpBox.Width - 40, e);

            Gfx.TextSpaced(sb, Game.Font, rowNames[selected], helpBox.X + 20, helpBox.Y + 74, Palette.Accent * e, TextSize.Label, 3f);
            Gfx.Text(sb, Game.StoryFont, helpWrapped[selected], helpBox.X + 20, helpBox.Y + 100, Palette.PaperDim * e, TextSize.StorySmall);

            Ui.Tag(sb, "SAVED", helpBox.X, helpBox.Bottom + 30, false, e);
            Gfx.Text(sb, Game.StoryFont, savedWrapped, helpBox.X, helpBox.Bottom + 58, Palette.LineGrey * e, TextSize.StorySmall);

            //Test Result : what the last timing test measured
            if (testResult.Length > 0)
                Gfx.TextSpaced(sb, Game.Font, testResult, helpBox.X, helpBox.Bottom + 128, Palette.Highlight * e, TextSize.Tiny, 2f);
        }
    }
}
