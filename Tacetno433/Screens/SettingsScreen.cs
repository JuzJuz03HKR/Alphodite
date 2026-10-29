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
    //Eight rows: three volume sliders, the language choice, full screen, the stroke timing,
    //LESS FLASHING and CLEAR FIGHT. Up and down pick a row, left and right change it, and the mouse can drag
    //the sliders or click the words. The column on the right explains whichever row is chosen.
    //
    //STROKE TIMING has a test: a flash (and a tick) every beat, the player strokes on each one,
    //and the page works out how early or late their strokes land. See Settings.TimingOffset.
    //
    //The numbers themselves live in Core/Settings.cs. They are saved to a file (SaveFile)
    //as soon as the page closes.
    //
    //Beta1 screens round 3 (29 Oct, docs/SCREENS_LEVEL_UP.md 20) : the same dark, still room as THE ARCHIVE. The page's
    //name stands turned on the tall white bar at the left (menu1), the rows are menu lines hanging on one thin line
    //(Ui.MenuItem, the ink bar under the chosen one), the sliders are thin lines with a circle (Ui.Fader) and the
    //either-or rows are words on plates (Ui.Choice). TACET's hole hangs still above the help. Borrowed by the pause
    //menu the page has no lens of its own, so it draws the same room flat and the hole with plain shapes.
    public class SettingsScreen : GameScreen
    {
        //Can Pause : a menu page outside the run, ESC here means going back
        public override bool CanPause
        {
            get { return false; }
        }

        //Wipe Word : the page's name in the wipe on the way here (WIPE WORD)
        public override string WipeWord
        {
            get { return "SETTINGS"; }
        }

        //Setting Rows
        private const int RowMaster = 0;
        private const int RowSfx = 1;
        private const int RowMusic = 2;
        private const int RowLanguage = 3;
        private const int RowFull = 4;
        private const int RowTiming = 5;
        private const int RowFlash = 6;                     // LESS FLASHING (Beta1 round 4)
        private const int RowClear = 7;                     // CLEAR FIGHT (Beta1 step 6, BETA1_DIRECTION 3.6)
        private const int RowCount = 8;

        //Settings Layout
        private const int SpineX = 262;                     // the thin line the rows hang on, each row's circle sits on it
        private const int RowY = 92;                        // the top of the first row
        private const int RowStep = 60;
        private const int RowH = 56;
        private const int RowLeft = 200;                    // the row's box for the mouse, from here ...
        private const int RowRight = 900;                   // ... to here
        private const int TrackX = 560;
        private const int TrackW = 240;
        private const int ValueRight = 890;
        private const int HelpX = 950;                      // the column that explains the chosen row
        private const int HelpW = 270;
        private static readonly Vector2 Hole = new Vector2(1085f, 96f);
        private const float HoleRadius = 30f;

        private static string[] rowIndex = { "01", "02", "03", "04", "05", "06", "07", "08" };
        private static string[] rowNames =
        {
            "MASTER VOLUME",
            "SOUND EFFECTS",
            "MUSIC",
            "LANGUAGE",
            "FULL SCREEN",
            "STROKE TIMING",
            "LESS FLASHING",
            "CLEAR FIGHT"
        };
        private static string[] offOn = { "OFF", "ON" };

        //Row Help : the sentence shown on the right for whichever row is chosen
        private static string[] rowHelp =
        {
            "The main fader. Every sound in the game is measured against this one.",
            BattleRules.TurnBattleOn ? "Button clicks, baton strokes, and every hit in a fight." : "Button clicks, baton strokes, and every clash in a duel.",
            "The track playing behind the page you are on.",
            "Which language the game is written in. Only English is written so far, so the choice is remembered but the words do not change yet.",
            "Fills the whole monitor without changing its resolution, so other programs are left where they are and alt tab is instant. The picture keeps its shape and black bars fill whatever is left over.",
            BattleRules.TurnBattleOn
                ? "If your strokes feel on time but the game calls them late, move this to the right. Early, to the left. Press ENTER or TEST and the game measures it for you."
                : "If your strokes feel right on the beat but the duel calls them late, move this to the right. Early, to the left. Press ENTER or TEST and the game measures it for you.",
            "Turns down the white flashes and the shaking of big hits, and leaves out the frame where the picture turns black and white. For anyone who finds flashing light hard to look at.",
            "A fight that is easier to read : bigger numbers, a solid plate behind every name and attack, each attack says who it is for, and the rest goes darker while you swing. The rules stay the same. Turn it on and off to compare."
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
        private Rectangle[] tracks = new Rectangle[3];      // the drawn line of each slider
        private Rectangle[] grabs = new Rectangle[3];       // the easier area the mouse can grab
        private Rectangle[] langBoxes = new Rectangle[2];
        private Rectangle[] fullBoxes = new Rectangle[2];   // FULL SCREEN : OFF, ON
        private Rectangle[] flashBoxes = new Rectangle[2];  // LESS FLASHING : OFF, ON
        private Rectangle[] clearBoxes = new Rectangle[2];  // CLEAR FIGHT : OFF, ON
        private Rectangle timingTrack;
        private Rectangle timingGrab;
        private Rectangle testChip;
        private Rectangle backButton;

        private int selected;
        private int dragging = -1;      // which slider the mouse is holding, -1 for none
        private int lastStep = -1;      // used to click only when a volume moves a whole step
        private float hold;             // how long an arrow key has been held
        private float repeat;
        private float time;
        private float helpClock;        // since the chosen row changed, so the help can slide in again
        private Vector2 lastMouse;
        private UiMotion[] rowMotion = new UiMotion[RowCount];
        private UiMotion backMotion = new UiMotion();

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

        //Row Center : the middle of row i, where its circle sits on the line
        private static float RowCenter(int i)
        {
            return RowY + i * RowStep + RowH / 2f;
        }

        public override void Load()
        {
            for (int i = 0; i < RowCount; i++)
            {
                rowBoxes[i] = new Rectangle(RowLeft, RowY + i * RowStep, RowRight - RowLeft, RowH);
                rowMotion[i] = new UiMotion();
            }

            for (int i = 0; i < 3; i++)
            {
                int cy = rowBoxes[i].Center.Y;
                tracks[i] = new Rectangle(TrackX, cy - 8, TrackW, 16);
                grabs[i] = new Rectangle(TrackX - 12, cy - 18, TrackW + 24, 36);
            }

            int lang = rowBoxes[RowLanguage].Center.Y;
            langBoxes[0] = new Rectangle(TrackX, lang - 16, 120, 32);
            langBoxes[1] = new Rectangle(TrackX + 132, lang - 16, 120, 32);
            for (int k = 0; k < 2; k++)
            {
                fullBoxes[k] = new Rectangle(TrackX + k * 88, rowBoxes[RowFull].Center.Y - 16, 76, 32);
                flashBoxes[k] = new Rectangle(TrackX + k * 88, rowBoxes[RowFlash].Center.Y - 16, 76, 32);
                clearBoxes[k] = new Rectangle(TrackX + k * 88, rowBoxes[RowClear].Center.Y - 16, 76, 32);
            }

            int timing = rowBoxes[RowTiming].Center.Y;
            timingTrack = new Rectangle(TrackX, timing - 8, 150, 16);
            timingGrab = new Rectangle(TrackX - 12, timing - 18, 174, 36);
            testChip = new Rectangle(ValueRight - 72, timing - 16, 72, 32);
            backButton = new Rectangle(40, 646, fromPause ? 250 : 190, 46);

            //Help Text : wrapped once here, never while drawing
            for (int i = 0; i < RowCount; i++)
                helpWrapped[i] = Gfx.WrapText(Game.StoryFont, rowHelp[i], HelpW, TextSize.StorySmall);
            savedWrapped = Gfx.WrapText(Game.StoryFont, savedNote, HelpW, TextSize.StorySmall);
            testWrapped = Gfx.WrapText(Game.StoryFont, testNote, HelpW, TextSize.StorySmall);

            //TACET and the page's light : the dark, still room of THE ARCHIVE (borrowed by the pause menu, none of this shows)
            if (!fromPause) Dark = new TacetVoid();
            Lens.Ambient = 0.42f;
            Lens.SpotPower = 0f;
            Lens.Vignette = 0.6f;

            lastMouse = Input.MousePos;
            if (!fromPause) SoundBank.PlayMusic(Music.Title);
        }

        //Hole Size : opens a little past its size and settles, then breathes very slowly
        private float HoleSize()
        {
            return HoleRadius * UiMotion.EaseBack(MathHelper.Clamp(time / 0.6f, 0f, 1f)) * (1f + 0.03f * (float)Math.Sin(time * 0.6f));
        }

        public override void Update(float dt)
        {
            time += dt;
            helpClock += dt;

            //TACET : above the help, still
            if (Dark != null)
            {
                Dark.Update(dt, 0.3f);
                Dark.PlaceOnPage(Hole, HoleSize(), 0f, 0f, 0f);
            }

            //POINT : the chosen row's ink bar shoots out, the others pull theirs back
            for (int i = 0; i < RowCount; i++)
                rowMotion[i].Update(dt, i == selected && RowIn(i) > 0.5f);
            backMotion.Update(dt, !testing && Input.MouseOver(backButton));

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
                helpClock = 0f;
            }

            //Value Change : one step per press, then a repeat while the key is held
            if (Input.KeyPressed(Keys.Left) || Input.KeyPressed(Keys.A)) Nudge(selected, -1);
            if (Input.KeyPressed(Keys.Right) || Input.KeyPressed(Keys.D)) Nudge(selected, 1);
            if (Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Space)) Press(selected);
            UpdateHold(dt);

            UpdateMouse();

            if (Input.KeyPressed(Keys.Escape) || Input.KeyPressed(Keys.Back) || Input.ClickedOn(backButton))
            {
                backMotion.Press();
                SoundBank.Play(Sfx.UiBack);
                SaveFile.SaveSettings();
                if (fromPause) Closed = true;
                else Game.Screens.Change(new TitleScreen());
            }
        }

        //Row In : how far row i has come in (ENTER), the rows one after another
        private float RowIn(int i)
        {
            return UiMotion.Between(time, 0.2f + i * BattleRules.UiEnterStagger, BattleRules.UiEnterTime);
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
            rowMotion[RowTiming].Press();
            helpClock = 0f;
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
                helpClock = 0f;
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
            helpClock = 0f;
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

        //Mouse : grab a slider and hold it, or click one of the words
        private void UpdateMouse()
        {
            if (Input.MouseClicked())
            {
                for (int i = 0; i < 3; i++)
                    if (Input.MouseOver(grabs[i])) { dragging = i; selected = i; lastStep = -1; }

                for (int k = 0; k < 2; k++)
                {
                    if (Input.MouseOver(langBoxes[k])) { selected = RowLanguage; SetLanguage(k); }
                    if (Input.MouseOver(fullBoxes[k])) { selected = RowFull; SetFullscreen(k == 1); }
                    if (Input.MouseOver(flashBoxes[k])) { selected = RowFlash; SetFlashing(k == 1); }
                    if (Input.MouseOver(clearBoxes[k])) { selected = RowClear; SetClear(k == 1); }
                }
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
            else if (row == RowFlash) SetFlashing(dir > 0);
            else if (row == RowClear) SetClear(dir > 0);
            else SetFullscreen(dir > 0);
        }

        //Enter Press : the rows that are simply one or the other flip over
        private void Press(int row)
        {
            if (row == RowLanguage) SetLanguage(Settings.Language == 0 ? 1 : 0);
            else if (row == RowFull) SetFullscreen(!Settings.Fullscreen);
            else if (row == RowFlash) SetFlashing(!Settings.ReduceFlashing);
            else if (row == RowClear) SetClear(!Settings.ClearFight);
            else if (row == RowTiming) StartTest();
            if (row >= RowLanguage) rowMotion[row].Press();
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

        //Set Flashing : LESS FLASHING on or off (the battle reads Settings.ReduceFlashing)
        private void SetFlashing(bool on)
        {
            if (Settings.ReduceFlashing == on) return;
            Settings.ReduceFlashing = on;
            SoundBank.Play(Sfx.UiConfirm);
        }

        //Set Clear : CLEAR FIGHT on or off (the battle reads Settings.ClearFight)
        private void SetClear(bool on)
        {
            if (Settings.ClearFight == on) return;
            Settings.ClearFight = on;
            SoundBank.Play(Sfx.UiConfirm);
        }

        private void SetFullscreen(bool on)
        {
            if (Settings.Fullscreen == on) return;
            Settings.Fullscreen = on;
            Game.ApplyFullscreen();
            SoundBank.Play(Sfx.UiConfirm);
        }

        //======================================================================================
        //The World (THE LENS) : the dark, still room of THE ARCHIVE, TACET's hole above the help
        //======================================================================================

        public override bool HasWorld
        {
            get { return true; }
        }

        public override bool ShowsGrain
        {
            get { return false; }
        }

        //Back : what the hole bends, the dark room and the rings lying on its floor (menu7)
        public override void DrawBack(SpriteBatch sb)
        {
            DrawRoom(sb);
        }

        //World : the bits the hole pulls in, and the line the rows hang on
        public override void DrawWorld(SpriteBatch sb)
        {
            Dark.DrawDebris(sb, false);
            DrawSpine(sb);
        }

        //Lights : a soft light on the chosen row, and one on the help
        public override void DrawLights(SpriteBatch sb)
        {
            Gfx.DrawGlow(sb, 520f, RowCenter(selected), 380f, Color.White * (0.32f * rowMotion[selected].Shown));
            Gfx.DrawGlow(sb, HelpX + HelpW / 2f, 300f, 300f, Color.White * (0.16f * UiMotion.Between(time, 0.2f, 0.6f)));
        }

        //Room : the dark room and the rings on its floor, the same as THE ARCHIVE's
        private void DrawRoom(SpriteBatch sb)
        {
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageDeep);
            Gfx.DrawGlowBox(sb, new Rectangle(200, 60, 1100, 640), Palette.Paper * 0.05f);
            float a = UiMotion.Between(time, 0.1f, 0.8f);
            for (int k = 0; k < 4; k++)
            {
                float rx = 240f + k * 150f;
                Ornament.FloorRing(sb, 700f, 640f, rx, rx * 0.16f, Palette.Paper * ((0.1f - k * 0.02f) * a), a);
            }
        }

        //Spine : the thin line the rows hang on, drawing itself down, with a gap round each row's circle
        private void DrawSpine(SpriteBatch sb)
        {
            float top = RowCenter(0) - 34f;
            float end = RowCenter(RowCount - 1) + 34f;
            float bottom = top + (end - top) * UiMotion.Between(time, 0.15f, 0.6f);
            Color line = Palette.LineGrey * 0.6f;
            float from = top;
            for (int i = 0; i <= RowCount; i++)
            {
                float to = Math.Min(i < RowCount ? RowCenter(i) - 9f : end, bottom);
                if (to > from) Gfx.Rect(sb, SpineX, from, 1, to - from, line);
                if (i < RowCount) from = RowCenter(i) + 9f;
            }
        }

        //======================================================================================
        //The Words (crisp on top)
        //======================================================================================

        public override void Draw(SpriteBatch sb)
        {
            //Borrowed : the pause menu draws only this, so the room, the line and the hole come along flat
            if (fromPause)
            {
                DrawRoom(sb);
                DrawSpine(sb);
                Ornament.StillHole(sb, Hole.X, Hole.Y, HoleSize(), time * 0.02f, 1f);
            }

            DrawTitleBar(sb);
            for (int i = 0; i < RowCount; i++) DrawRow(sb, i);

            if (testing) DrawTest(sb);
            else DrawHelp(sb);

            float low = UiMotion.Between(time, 0.5f, BattleRules.UiEnterTime);
            Ui.Command(sb, backButton, fromPause ? "BACK TO PAUSE" : "BACK", "ESC", backMotion, low, false);
            Gfx.TextSpacedRight(sb, Game.Font, "UP DOWN  CHOOSE   /   LEFT RIGHT  CHANGE   /   DRAG THE SLIDERS", 1240, 692, Palette.LineGrey * low, TextSize.Tiny, 2f);
        }

        //Title Bar : the page's name turned on the tall white bar at the left (menu1)
        private void DrawTitleBar(SpriteBatch sb)
        {
            float a = UiMotion.Between(time, 0f, 0.45f);
            int x = 40 + (int)Math.Round((1f - a) * -40f);
            Ui.TitleBar(sb, new Rectangle(x, 80, 96, 540), 24, "SETTINGS", a);
        }

        //Row : the menu line (number, circle on the line, name, the ink bar when chosen), then the row's control
        private void DrawRow(SpriteBatch sb, int i)
        {
            float a = RowIn(i);
            if (a <= 0f) return;
            float slide = (1f - a) * BattleRules.UiEnterSlide;
            bool on = i == selected;
            float cy = RowCenter(i);

            Ui.MenuItem(sb, new Vector2(SpineX + slide, cy), rowIndex[i], rowNames[i], "", rowMotion[i], a);
            Gfx.Rect(sb, TrackX - 24 + slide, cy + RowH / 2f + 3f, ValueRight - TrackX + 24, 1, Palette.LineGrey * (0.18f * a));

            if (i <= RowMusic) DrawVolumeRow(sb, i, on, a);
            else if (i == RowLanguage) DrawLanguageRow(sb, on, a);
            else if (i == RowFull) DrawSwitchRow(sb, fullBoxes, Settings.Fullscreen, "BORDERLESS  WINDOW", 21, on, a);
            else if (i == RowFlash) DrawSwitchRow(sb, flashBoxes, Settings.ReduceFlashing, "SOFTER  HITS", 33, on, a);
            else if (i == RowClear) DrawSwitchRow(sb, clearBoxes, Settings.ClearFight, "BIGGER  NUMBERS", 57, on, a);
            else DrawTimingRow(sb, on, a);
        }

        //Volume Row : the fader and the number beside it
        private void DrawVolumeRow(SpriteBatch sb, int i, bool on, float a)
        {
            float value = VolumeOf(i);
            bool hot = on || dragging == i || Input.MouseOver(grabs[i]);
            Ui.Fader(sb, tracks[i], value, hot, false, a);
            Gfx.TextRight(sb, Game.Font, Settings.Percent(value), ValueRight, tracks[i].Center.Y - Game.Font.LineSpacing * TextSize.Heading / 2f, (on ? Palette.Highlight : Palette.PaperDim) * a, TextSize.Heading);
        }

        //Language Row : the two languages, the chosen one on its plate
        private void DrawLanguageRow(SpriteBatch sb, bool on, float a)
        {
            for (int k = 0; k < 2; k++)
                Ui.Choice(sb, langBoxes[k], Settings.LanguageNames[k], Settings.Language == k, on, Input.MouseOver(langBoxes[k]), 7 + k * 4, a);
        }

        //Switch Row : OFF and ON, the one in use on its plate, and what it means at the right
        private void DrawSwitchRow(SpriteBatch sb, Rectangle[] boxes, bool value, string note, int seed, bool on, float a)
        {
            for (int k = 0; k < 2; k++)
                Ui.Choice(sb, boxes[k], offOn[k], value == (k == 1), on, Input.MouseOver(boxes[k]), seed + k * 4, a);
            Gfx.TextSpacedRight(sb, Game.Font, note, ValueRight, boxes[0].Center.Y - 7, Palette.LineGrey * a, TextSize.Tiny, 2f);
        }

        //Timing Row : a fader lit from its middle, the value, and TEST (white when the row is chosen, ENTER runs it)
        private void DrawTimingRow(SpriteBatch sb, bool on, float a)
        {
            bool hot = on || dragging == RowTiming || Input.MouseOver(timingGrab);
            float value = (Settings.TimingOffset / Settings.TimingMost + 1f) / 2f;
            Ui.Fader(sb, timingTrack, value, hot, true, a);
            Gfx.TextRight(sb, Game.Font, Settings.TimingLabel(Settings.TimingOffset), testChip.X - 14, timingTrack.Center.Y - Game.Font.LineSpacing * TextSize.Body / 2f, (on ? Palette.Highlight : Palette.PaperDim) * a, TextSize.Body);
            Ui.Choice(sb, testChip, "TEST", on, on, Input.MouseOver(testChip), 45, a);
        }

        //Help Column : what the chosen row does, sliding in again whenever the row changes, and the note that nothing
        //is lost (options are saved on the way out)
        private void DrawHelp(SpriteBatch sb)
        {
            float a = UiMotion.Between(time, 0.3f, BattleRules.UiEnterTime);
            float turn = UiMotion.EaseOut(MathHelper.Clamp(helpClock / 0.2f, 0f, 1f));
            float x = HelpX + (1f - turn) * 12f;

            Gfx.TextSpaced(sb, Game.Font, rowNames[selected], x, 168, Palette.Paper * (a * turn), TextSize.Label, 3f);
            DrawHelpRule(sb, 196, a);
            Gfx.Text(sb, Game.StoryFont, helpWrapped[selected], x, 210, Palette.PaperDim * (a * turn), TextSize.StorySmall);

            Gfx.Circle(sb, HelpX + 3, 452, 3f, Palette.LineGrey * a);
            Gfx.TextSpaced(sb, Game.Font, "SAVED", HelpX + 14, 445, Palette.LineGrey * a, TextSize.Tiny, 3f);
            Gfx.Text(sb, Game.StoryFont, savedWrapped, HelpX, 466, Palette.LineGrey * a, TextSize.StorySmall);

            //Test Result : what the last timing test measured
            if (testResult.Length > 0)
                Gfx.TextSpaced(sb, Game.Font, testResult, HelpX, 540, Palette.Highlight * a, TextSize.Tiny, 2f);
        }

        //Help Rule : a thin line with a small ring at its start (menu3)
        private void DrawHelpRule(SpriteBatch sb, float y, float a)
        {
            Gfx.CircleOutline(sb, HelpX + 3, y, 3f, Palette.LineGrey * a, 1f);
            Gfx.Rect(sb, HelpX + 9, y, HelpW - 9, 1, Palette.LineGrey * (0.6f * a));
        }

        //Test Column : in place of the help while the test runs. A ring flashes on every beat, the circles under it
        //are the strokes counted so far, and the marks along the ruler are where they landed, early to the left.
        private void DrawTest(SpriteBatch sb)
        {
            float a = UiMotion.Between(helpClock, 0f, 0.2f);
            Gfx.TextSpaced(sb, Game.Font, "STROKE TIMING TEST", HelpX, 168, Palette.Paper * a, TextSize.Label, 3f);
            DrawHelpRule(sb, 196, a);

            //Beat : the ring flashes and swells a little on every tick
            float cx = HelpX + HelpW / 2f;
            float cy = 270f;
            Gfx.DrawGlow(sb, cx, cy, 40f + testFlash * 50f, Palette.Highlight * ((0.12f + testFlash * 0.5f) * a));
            Gfx.CircleOutline(sb, cx, cy, 24f + testFlash * 8f, Palette.Paper * ((0.5f + testFlash * 0.5f) * a), 1.5f + testFlash * 2f);
            Gfx.Circle(sb, cx, cy, 3f, Palette.Paper * a);
            Gfx.TextSpacedCentered(sb, Game.Font, testStrokes < TestPractice ? "PRACTICE" : "COUNTING", cx, cy + 40, Palette.PaperDim * a, TextSize.Tiny, 3f);

            //Counted : one circle per stroke that counts, filled once it is in
            for (int i = 0; i < TestCount; i++)
            {
                float px = cx - (TestCount - 1) * 7f + i * 14f;
                if (i < testRecorded) Gfx.Circle(sb, px, cy + 70, 4f, Palette.Paper * a);
                else Gfx.CircleOutline(sb, px, cy + 70, 4f, Palette.LineGrey * a, 1f);
            }

            //Ruler : -150 to +150 milliseconds, the middle is right on the beat
            float left = HelpX + 10;
            float width = HelpW - 20;
            float ry = 392f;
            Gfx.Rect(sb, left, ry, width, 1, Palette.LineGrey * a);
            Gfx.Rect(sb, left + width / 2f, ry - 8, 1, 16, Palette.Paper * a);
            Gfx.TextSpaced(sb, Game.Font, "EARLY", left, ry + 10, Palette.LineGrey * a, TextSize.Tiny, 2f);
            Gfx.TextSpacedRight(sb, Game.Font, "LATE", left + width, ry + 10, Palette.LineGrey * a, TextSize.Tiny, 2f);
            for (int i = 0; i < testRecorded; i++)
            {
                float t = MathHelper.Clamp(testOffsets[i] / Settings.TimingMost, -1f, 1f);
                Gfx.Rect(sb, left + width / 2f + t * width / 2f - 1f, ry - 10, 3, 20, Palette.Highlight * a);
            }

            Gfx.Text(sb, Game.StoryFont, testWrapped, HelpX, 446, Palette.PaperDim * a, TextSize.StorySmall);
            Gfx.TextSpaced(sb, Game.Font, "ESC OR RIGHT CLICK TO STOP", HelpX, 540, Palette.LineGrey * a, TextSize.Tiny, 2f);
        }
    }
}
