using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;
using Tacetno433.Screens;

namespace Tacetno433
{
    //TacetGame : the main game class. It owns the window, the fonts and the screen manager,
    //then hands every frame over to whichever screen is currently active.
    public class TacetGame : Game
    {
        //Screen Size : fixed window. Every position in the game is written for this size.
        public const int ScreenW = 1280;
        public const int ScreenH = 720;

        //Text Wrap Widths : how wide text columns are, used when preparing text once
        public const int TextWrapWidth = 400;         // conductor dossier column
        public const int MusicianWrapWidth = 330;     // musician personality lines
        public const int MotifCardWidth = 196;        // text on a motif reward card
        public const int MotifTipWidth = 260;         // text in a motif tooltip
        public const int EventTextWidth = 520;        // the story on an event page
        public const int TraitWrapWidth = 300;        // an enemy's trait in its tooltip

        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        //Game Picture : the whole game is drawn into this picture, then the picture is put on the window.
        //SHARP SCREEN (Beta1, Core/Sharp.cs) : the picture is the size it is shown at (1280 x 720 in the window, the
        //monitor's own size in full screen), so it is never stretched. Every page still draws in 1280 x 720 game pixels.
        private RenderTarget2D screen;
        private Rectangle view;

        //Shared Fonts : see TextSize for what each one is for
        public SpriteFont Font;
        public SpriteFont BigFont;
        public SpriteFont StoryFont;
        public SpriteFont LogoFont;

        //Screen Manager
        public ScreenManager Screens;

        //Pause Menu : opened by ESC on every page of a run, drawn over the page, see PauseMenu
        public PauseMenu Pause;

        //Run Data : everything about the run in progress. Null until a run starts.
        public RunState CurrentRun;

        //Debug Overlay : press F3 while playing to watch frame rate, memory and audio
        private bool showDebug;
        private int frameCount;
        private float frameTimer;
        private int shownFps;
        private int shownUps;

        //WATCH FRAMES (3 Nov, the user 13.150 : "the game lags but FPS says 60 all the time") : FPS used to count Updates.
        //MonoGame keeps the game's clock at 60 Updates a second, and when drawing falls behind it runs two Updates for one
        //picture and skips the picture between (IsFixedTimeStep), so the count said 60 while the screen showed 40.
        //Now FPS counts the pictures really drawn, on a real clock, and AUTO graphics watches the same count.
        private readonly System.Diagnostics.Stopwatch frameClock = System.Diagnostics.Stopwatch.StartNew();
        private int drawnThisSecond;
        private double watchStart;
        private int slowSeconds;                 // seconds in a row under BattleRules.AutoGraphicsFps
        private double calmUntil = 4.0;          // nothing is judged before this (the clock's seconds) : the start, a step down

        //Frame Cost (F3, Beta1) : measured while the debug line is on, shown once a second
        private System.Diagnostics.Stopwatch updateWatch = new System.Diagnostics.Stopwatch();
        private System.Diagnostics.Stopwatch drawWatch = new System.Diagnostics.Stopwatch();
        private double updateMsSum;
        private double drawMsSum;
        private float shownUpdateMs;
        private float shownDrawMs;
        private long shownCalls;
        private long shownSprites;
        private long shownTargets;
        private float shownMemory;

        //Focus : whether the window was in front last frame, to notice alt tab
        private bool wasActive = true;

        public TacetGame()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = ScreenW;
            graphics.PreferredBackBufferHeight = ScreenH;

            //Borderless Full Screen : never change the monitor's own resolution. A real mode
            //switch resizes every other program on the desktop and makes alt tab flicker.
            graphics.HardwareModeSwitch = false;

            //Keep Running : without this the game crawls while another window is in front,
            //so coming back from alt tab takes a moment to catch up.
            InactiveSleepTime = TimeSpan.Zero;

            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            Window.Title = "TACET 4'33";
            Screens = new ScreenManager(this);

            //Developer Capture : keep the window out of the way while pictures are taken
            if (DebugShots.Active) Window.Position = new Point(-3000, -3000);
            if (DebugShots.Active && DebugShots.Steady) IsFixedTimeStep = false;     // one update per picture, see Steady

            base.Initialize();   // this calls LoadContent
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            Window.ClientSizeChanged += delegate { UpdateView(); };
            UpdateView();
            Gfx.Init(GraphicsDevice);
            NumberText.Build();
            Settings.Build();

            //Saved Settings : read back what the player chose last time. The capture tool leaves
            //the real save files alone, so its pictures always use the plain defaults.
            SaveFile.Enabled = !DebugShots.Active;
            SaveFile.LoadSettings();
            SaveFile.LoadArchive();                     // Alpha 7 : the ARCHIVE's echoes and upgrades
            if (DebugShots.Thai) Settings.Language = 1; // THAI TEXT : --thai, the capture tool's pictures in Thai

            Font = Content.Load<SpriteFont>("MainFont");
            BigFont = Content.Load<SpriteFont>("BigFont");
            StoryFont = Content.Load<SpriteFont>("StoryFont");
            LogoFont = Content.Load<SpriteFont>("LogoFont");

            //Shaders (Beta1) : Content/Effects. Missing or unreadable ones leave Fx.Ready false and pages draw plainly.
            Fx.Load(Content, GraphicsDevice);

            //Font Safety : the fonts only contain plain English characters. Without this,
            //printing anything else (a stray symbol, Thai text) throws and kills the game.
            //With it, an unknown character just draws as a question mark.
            Font.DefaultCharacter = '?';
            BigFont.DefaultCharacter = '?';
            StoryFont.DefaultCharacter = '?';
            LogoFont.DefaultCharacter = '?';

            //Sharp Letters (SHARP SCREEN) : each font's bigger copies for big screens, see Core/SharpText.cs
            SharpText.Add(Font, "MainFont");
            SharpText.Add(BigFont, "BigFont");
            SharpText.Add(StoryFont, "StoryFont");
            SharpText.Add(LogoFont, "LogoFont");

            //THAI TEXT (block F3) : each font's Thai partner (Core/ThaiText.cs), with its own sharp copies, and the word list put
            //in place before any page makes its lines (Data/Lang.cs). The line spacings are a share of the English font's.
            SpriteFont thaiMain = Content.Load<SpriteFont>("ThaiMainFont");
            SpriteFont thaiBig = Content.Load<SpriteFont>("ThaiBigFont");
            SpriteFont thaiStory = Content.Load<SpriteFont>("ThaiStoryFont");
            ThaiText.Pair(Font, thaiMain, "ChakraPetch-Medium", BattleRules.ThaiLineMain, 1f);
            ThaiText.Pair(BigFont, thaiBig, "Trirong-SemiBold", BattleRules.ThaiLineBig, 1f);
            ThaiText.Pair(StoryFont, thaiStory, "Trirong-Medium", BattleRules.ThaiLineStory, 1f);
            ThaiText.Pair(LogoFont, thaiBig, "Trirong-SemiBold", 1f, LogoFont.LineSpacing / (float)BigFont.LineSpacing);
            SharpText.Add(thaiMain, "ThaiMainFont");
            SharpText.Add(thaiBig, "ThaiBigFont");
            SharpText.Add(thaiStory, "ThaiStoryFont");
            SharpText.Choose(Services, Content.RootDirectory, Sharp.Scale);
            Lang.Load(Path.Combine(AppContext.BaseDirectory, Content.RootDirectory, "Text", "thai.txt"));
            Lang.Apply();

            //DIGITS (the user 13.141) : the one number font's biggest copy, for big numbers (Core/Digits.cs, Gfx NUMBERS)
            Digits.Font = Content.Load<SpriteFont>("MainFont_3x");
            Digits.Font.DefaultCharacter = '?';

            //Shared UI Pieces : hand the fonts over once
            Ui.Font = Font;
            Ui.BigFont = BigFont;
            Ui.StoryFont = StoryFont;
            Ui.LogoFont = LogoFont;

            //Audio : picks up whatever sound files exist, silently skips the rest
            SoundBank.Load(Content);
            SoundBank.ApplyVolume();

            //Hand Art : the same idea for the conductor hand pictures in the duel
            HandArt.Load(Content);

            //Still Art : stage backgrounds, portraits, faces, places, signature pictures
            ArtBank.Load(Content, GraphicsDevice);         // block C : the conductors' paintings straight from Content/Art/Conductors
            UiArt.Load(GraphicsDevice);                     // Beta1 screens : the pages' pixel art, straight from Content/Art/Ui
            MotifArt.Load();                                // GIFTS : the marks of the legends, from the icons UiArt found

            //Pixel Musicians : the animated band on the duel stage
            CharacterArt.Load(Content);

            Pause = new PauseMenu(this);
            if (Settings.Fullscreen) ApplyFullscreen();

            //Text Prepare : wrap all the long text ONCE, never while drawing
            ConductorList.PrepareText(StoryFont, TextWrapWidth, TextSize.Story);
            MusicianList.PrepareText(StoryFont, MusicianWrapWidth);
            EnemyList.PrepareText(StoryFont, TraitWrapWidth);
            MotifList.PrepareText(StoryFont, MotifCardWidth, MotifTipWidth);
            RehearsalList.PrepareText(StoryFont, RehearsalScreen.LineWidth); // REHEARSAL : the legends' lines on the rehearsal page
            EventList.PrepareText(StoryFont, EventTextWidth, EventTextWidth);
            SongList.Prepare();

            //First Screen : the title page, or the capture tool when it was asked for
            if (DebugShots.Active)
                DebugShots.Begin(this);
            else
                Screens.ChangeNow(new TitleScreen());
        }

        //Restart (THAI TEXT, block F3) : the game starts again, here. The pages make their lines once, so a new language needs a
        //fresh start (Data/Lang.cs). The same program is started again and this one closes (a run is kept at its last path).
        public void Restart()
        {
            try
            {
                string me = Environment.ProcessPath ?? "";
                System.Diagnostics.ProcessStartInfo start = new System.Diagnostics.ProcessStartInfo(me);
                start.UseShellExecute = false;
                start.WorkingDirectory = AppContext.BaseDirectory;
                if (Path.GetFileNameWithoutExtension(me).ToLowerInvariant() == "dotnet")
                    start.ArgumentList.Add(System.Reflection.Assembly.GetEntryAssembly().Location);   // started as "dotnet Tacetno433.dll"
                System.Diagnostics.Process.Start(start);
            }
            catch (Exception)
            {
                //No new start (a locked machine) : the new language comes the next time the player opens the game
            }
            Exit();
        }

        //Full Screen : switched on and off by the settings page.
        //Full screen here means a borderless window the size of the desktop. The monitor
        //keeps its own resolution, so other programs are left alone and alt tab is instant.
        //The game is then drawn at that resolution too (SHARP SCREEN, UpdateView sets it).
        public void ApplyFullscreen()
        {
            if (Settings.Fullscreen)
            {
                DisplayMode desktop = GraphicsDevice.Adapter.CurrentDisplayMode;
                graphics.PreferredBackBufferWidth = desktop.Width;
                graphics.PreferredBackBufferHeight = desktop.Height;
            }
            else
            {
                graphics.PreferredBackBufferWidth = ScreenW;
                graphics.PreferredBackBufferHeight = ScreenH;
            }

            graphics.IsFullScreen = Settings.Fullscreen;
            graphics.ApplyChanges();
            UpdateView();
        }

        //View : where the picture sits inside the real window. It keeps its shape and is centred,
        //so a wider screen gets black bars at the sides instead of stretching.
        private void UpdateView()
        {
            int w = GraphicsDevice.PresentationParameters.BackBufferWidth;
            int h = GraphicsDevice.PresentationParameters.BackBufferHeight;
            if (w <= 0 || h <= 0) return;

            float scale = Math.Min(w / (float)ScreenW, h / (float)ScreenH);
            int pw = (int)(ScreenW * scale);
            int ph = (int)(ScreenH * scale);
            view = new Rectangle((w - pw) / 2, (h - ph) / 2, pw, ph);

            //The mouse arrives in window pixels, so Input has to know where the picture is
            //in THOSE pixels. On most machines the window and the picture are the same size,
            //but Windows display scaling can make them differ, so the rectangle is converted.
            Rectangle client = Window.ClientBounds;
            float mx = client.Width > 0 ? client.Width / (float)w : 1f;
            float my = client.Height > 0 ? client.Height / (float)h : 1f;
            Input.SetView(new Rectangle((int)(view.X * mx), (int)(view.Y * my),
                                        (int)(view.Width * mx), (int)(view.Height * my)));

            //SHARP SCREEN : the picture is drawn at the size it is shown. The capture tool keeps its own size (--scale).
            SetSharp(DebugShots.Active ? DebugShots.Scale : scale);
        }

        //Set Sharp : the page's real size changed (or is set for the first time). The game picture, the fonts' bigger
        //copies and the battle's pictures (PostChain.Ensure, on their next use) follow it.
        private void SetSharp(float scale)
        {
            Sharp.Set(scale);
            if (screen == null || screen.Width != Sharp.Width || screen.Height != Sharp.Height)
            {
                GraphicsDevice.Textures[0] = null;
                if (screen != null) screen.Dispose();
                screen = new RenderTarget2D(GraphicsDevice, Sharp.Width, Sharp.Height);
            }
            SharpText.Choose(Services, Content.RootDirectory, Sharp.Scale);
        }

        protected override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (DebugShots.Active && DebugShots.Steady) dt = 1f / 60f;
            if (showDebug) updateWatch.Restart();

            Input.Update();
            if (DebugShots.Active) DebugShots.FakeMouse(dt);
            LegendArt.Update(dt);                        // LEGEND ART : the pointer's wind through the portraits (block C)

            //Window Focus : when the player alt tabs away the game stops where it is. The page
            //on screen is told once, the pause menu opens on pages that can pause, and then
            //nothing moves until they return.
            bool active = IsActive || DebugShots.Active;
            if (!active && wasActive && Screens.Current != null)
            {
                Screens.Current.LostFocus();
                if (CanOpenPause()) Pause.Show();
            }
            wasActive = active;

            //Pause Menu : while it is open the page underneath gets no Update at all.
            //ESC opens it, unless the page is using ESC to close something of its own.
            if (active)
            {
                //TEST ROOM (Beta1 31 Oct) : while a test plays, F2 drops it and goes back to the test page from anywhere
                if (TestRoomScreen.Running && Input.KeyPressed(Keys.F2) && !Screens.Busy && !(Screens.Current is TestRoomScreen))
                {
                    Pause.Open = false;
                    Screens.Change(new TestRoomScreen());
                }
                else if (Pause.Open) Pause.Update(dt);
                else if (Input.KeyPressed(Keys.Escape) && CanOpenPause() && !Screens.Current.UsesEscape) Pause.Show();
                else Screens.Update(dt);
            }
            SoundBank.Update(dt);                        // BLOCK E : the music's fades and DUCK
            Ornament.UpdateGrain(dt);
            WatchFrames(active);
            if (DebugShots.Active) Input.PretendPress = Keys.None;     // a pretend key lasts one update, even when a slow frame runs several
            if (showDebug) updateMsSum += updateWatch.Elapsed.TotalMilliseconds;
            UpdateDebug(dt);

            base.Update(gameTime);
        }

        //Can Open Pause : a page of a run is on show and is not in the middle of changing
        private bool CanOpenPause()
        {
            return Screens.Current != null && Screens.Current.CanPause && !Screens.Busy && CurrentRun != null;
        }

        //Watch Frames : once a second, how many pictures were drawn (FPS), and AUTO GRAPHICS : when the pictures stay under
        //BattleRules.AutoGraphicsFps for AutoGraphicsSeconds seconds in a row while the game is in front and no page is
        //changing (loading a page can stall a frame), the world steps down one level (HIGH, MEDIUM, LOW, Core/Sharp.cs
        //WORLD SCALE). The level is saved, so the next start on this machine begins there. Choosing AUTO again on the
        //settings page starts it from HIGH.
        private void WatchFrames(bool active)
        {
            Sharp.WorldShare = Settings.GraphicsShares[Settings.GraphicsLevel];
            double now = frameClock.Elapsed.TotalSeconds;
            double span = now - watchStart;
            if (span < 1.0) return;
            shownFps = (int)Math.Round(drawnThisSecond / span);
            drawnThisSecond = 0;
            watchStart = now;

            bool judge = Settings.Graphics == 0 && Settings.AutoGraphics < 2 && active && !DebugShots.Active
                         && !Screens.Busy && !Pause.Open && now >= calmUntil;
            if (!judge)
            {
                slowSeconds = 0;
                if (!active || Screens.Busy) calmUntil = Math.Max(calmUntil, now + 1.0);   // a second to settle after
                return;
            }
            slowSeconds = shownFps < BattleRules.AutoGraphicsFps ? slowSeconds + 1 : 0;
            if (slowSeconds < BattleRules.AutoGraphicsSeconds) return;

            Settings.AutoGraphics++;
            SaveFile.SaveSettings();
            slowSeconds = 0;
            calmUntil = now + 3.0;
        }

        //Debug Update : count updates and read how much memory is in use
        private void UpdateDebug(float dt)
        {
            if (Input.KeyPressed(Keys.F3)) showDebug = !showDebug;
            if (!showDebug) return;

            frameCount++;
            frameTimer += dt;

            if (frameTimer >= 1f)
            {
                shownUps = frameCount;
                //false means "do not force a collection", we only want to read the number
                shownMemory = GC.GetTotalMemory(false) / 1048576f;
                shownUpdateMs = (float)(updateMsSum / System.Math.Max(1, frameCount));
                shownDrawMs = (float)(drawMsSum / System.Math.Max(1, frameCount));
                updateMsSum = 0;
                drawMsSum = 0;
                frameCount = 0;
                frameTimer = 0f;
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            //Frame Timer (F3) : how long drawing takes, read in the debug line
            if (showDebug) drawWatch.Restart();
            drawnThisSecond++;                           // WATCH FRAMES : a picture really drawn

            //Pre Draw (Beta1) : a page that builds its picture in layers does it first (see Core/PostChain.cs)
            Screens.PreDraw(spriteBatch);

            //Developer Capture : the tool has its own picture to draw into.
            //STRAIGHT ON (3 Nov, the user 13.150) : when the picture fills the whole window (the 1280 x 720 window, full screen
            //on a 16 : 9 monitor) the page is drawn straight onto the window, saving a full screen copy every frame. A window
            //of another shape still goes through the screen picture, which is then centred between black bars.
            int backW = GraphicsDevice.PresentationParameters.BackBufferWidth;
            int backH = GraphicsDevice.PresentationParameters.BackBufferHeight;
            bool straight = !DebugShots.Active && view.X == 0 && view.Y == 0 && view.Width == backW && view.Height == backH
                            && screen.Width == backW && screen.Height == backH;
            GraphicsDevice.SetRenderTarget(DebugShots.Active ? DebugShots.Target : (straight ? null : screen));

            GraphicsDevice.Clear(Palette.Void);

            //LinearClamp smooths the fonts when they are drawn smaller than they were built.
            //Sharp.Transform turns the pages' game pixels into the picture's real pixels (SHARP SCREEN).
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, Sharp.Transform);

            Screens.Draw(spriteBatch);
            if (Pause.Open) Pause.Draw(spriteBatch);

            //Grain : one layer of film grain over every page, so flat greys feel like print
            if (Screens.ShowsGrain) Ornament.Grain(spriteBatch);

            if (showDebug)
            {
                //Debug numbers are built here on purpose. They only exist while F3 is on,
                //so the string work never happens during normal play.
                string line = "FPS " + shownFps + " (UPDATES " + shownUps + ")   MEM " + shownMemory.ToString("0.0") + " MB"
                            + "   SFX " + SoundBank.LoadedSfx + "   MUSIC " + SoundBank.LoadedMusic + "   BORROWED " + SoundBank.LoadedBorrowed + "   BAND " + SoundBank.LoadedBand
                            + "   PHRASE " + SoundBank.LoadedPhrase + "   INSTR " + SoundBank.LoadedInstruments
                            + "   HAND " + HandArt.LoadedPoses + "   ART " + ArtBank.Loaded + "   UI " + UiArt.Loaded + "   BATTLE " + FrameArt.Loaded   // block C item 5 : the art the game found, by folder
                            + "   PIXEL " + CharacterArt.Loaded
                            + "   VIEW " + Sharp.Width + " x " + Sharp.Height            // SHARP SCREEN : the real size the page is drawn at
                            + "   WORLD " + (int)(ScreenW * Sharp.WorldScale) + " x " + (int)(ScreenH * Sharp.WorldScale)   // WORLD SCALE (3 Nov)
                            + " " + Settings.GraphicsNames[Settings.Graphics] + (Settings.Graphics == 0 ? " " + Settings.GraphicsNames[1 + Settings.GraphicsLevel] : "");
                //Optimize (Beta1) : the cost of a frame. DRAW = draw calls last frame, GC = collections so far (gen 0 / 1 / 2),
                //FX = whether the shaders loaded. Watch these before and after adding an effect.
                string cost = "UPDATE " + shownUpdateMs.ToString("0.00") + " ms   DRAW " + shownDrawMs.ToString("0.00") + " ms   CALLS " + shownCalls
                            + "   SPRITES " + shownSprites + "   TARGETS " + shownTargets
                            + "   GC " + GC.CollectionCount(0) + " / " + GC.CollectionCount(1) + " / " + GC.CollectionCount(2)
                            + "   FX " + (Fx.Ready ? "ON" : "OFF");
                Gfx.Rect(spriteBatch, 0, 0, 1180, 46, Color.Black * 0.8f);
                Gfx.Text(spriteBatch, Font, line, 8, 3, Palette.Highlight, TextSize.Body);
                Gfx.Text(spriteBatch, Font, cost, 8, 25, Palette.Highlight, TextSize.Body);
            }

            spriteBatch.End();
            GraphicsDevice.SetRenderTarget(null);

            if (DebugShots.Active)
            {
                DebugShots.AfterDraw(this);
            }
            else if (!straight)
            {
                //Window Fit : black around the picture, then the picture itself. It is already the size of the view, so
                //its pixels land one on one (PointClamp copies them as they are). Only a screen past Sharp.MaxScale
                //stretches it, smoothly.
                GraphicsDevice.Clear(Color.Black);
                bool fits = screen.Width == view.Width && screen.Height == view.Height;
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, fits ? SamplerState.PointClamp : SamplerState.LinearClamp);
                spriteBatch.Draw(screen, view, Color.White);
                spriteBatch.End();
            }

            base.Draw(gameTime);

            //Frame Cost : what the card was asked to do this frame (MonoGame counts it for us)
            if (showDebug)
            {
                drawMsSum += drawWatch.Elapsed.TotalMilliseconds;
                GraphicsMetrics metrics = GraphicsDevice.Metrics;
                shownCalls = metrics.DrawCount;
                shownSprites = metrics.SpriteCount;
                shownTargets = metrics.TargetCount;
            }
        }
    }
}
