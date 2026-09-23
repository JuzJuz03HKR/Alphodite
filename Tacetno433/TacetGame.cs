using System;
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

        //Game Picture : the whole game is drawn into this 1280 x 720 picture, then the
        //picture is put on the window at whatever size the window happens to be.
        private RenderTarget2D screen;
        private Rectangle view;

        //Shared Fonts : see TextSize for what each one is for
        public SpriteFont Font;
        public SpriteFont BigFont;
        public SpriteFont StoryFont;
        public SpriteFont LogoFont;

        //Screen Manager
        public ScreenManager Screens;

        //Run Data : everything about the run in progress. Null until a run starts.
        public RunState CurrentRun;

        //Debug Overlay : press F3 while playing to watch frame rate, memory and audio
        private bool showDebug;
        private int frameCount;
        private float frameTimer;
        private int shownFps;
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

            base.Initialize();   // this calls LoadContent
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            screen = new RenderTarget2D(GraphicsDevice, ScreenW, ScreenH);
            Window.ClientSizeChanged += delegate { UpdateView(); };
            UpdateView();
            Gfx.Init(GraphicsDevice);
            NumberText.Build();
            Settings.Build();

            Font = Content.Load<SpriteFont>("MainFont");
            BigFont = Content.Load<SpriteFont>("BigFont");
            StoryFont = Content.Load<SpriteFont>("StoryFont");
            LogoFont = Content.Load<SpriteFont>("LogoFont");

            //Font Safety : the fonts only contain plain English characters. Without this,
            //printing anything else (a stray symbol, Thai text) throws and kills the game.
            //With it, an unknown character just draws as a question mark.
            Font.DefaultCharacter = '?';
            BigFont.DefaultCharacter = '?';
            StoryFont.DefaultCharacter = '?';
            LogoFont.DefaultCharacter = '?';

            //Shared UI Pieces : hand the fonts over once
            Ui.Font = Font;
            Ui.BigFont = BigFont;
            Ui.StoryFont = StoryFont;
            Ui.LogoFont = LogoFont;

            //Audio : picks up whatever sound files exist, silently skips the rest
            SoundBank.Load(Content);

            //Hand Art : the same idea for the conductor hand pictures in the duel
            HandArt.Load(Content);

            //Still Art : stage backgrounds, portraits, faces, places, signature pictures
            ArtBank.Load(Content);

            //Pixel Musicians : the animated band on the duel stage
            CharacterArt.Load(Content);

            //Text Prepare : wrap all the long text ONCE, never while drawing
            ConductorList.PrepareText(StoryFont, TextWrapWidth, TextSize.Story);
            EraList.PrepareText(StoryFont, PanelStrip.CaptionWrapWidth);
            MusicianList.PrepareText(StoryFont, MusicianWrapWidth);
            EnemyList.PrepareText(StoryFont, TraitWrapWidth);
            MotifList.PrepareText(StoryFont, MotifCardWidth, MotifTipWidth);
            EventList.PrepareText(StoryFont, EventTextWidth, EventTextWidth);

            //First Screen : the title page, or the capture tool when it was asked for
            if (DebugShots.Active)
                DebugShots.Begin(this);
            else
                Screens.ChangeNow(new TitleScreen());
        }

        //Full Screen : switched on and off by the settings page.
        //Full screen here means a borderless window the size of the desktop. The monitor
        //keeps its own resolution, so other programs are left alone and alt tab is instant.
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

        //View : where the 1280 x 720 picture sits inside the real window. It keeps its shape
        //and is centred, so a wider screen gets black bars at the sides instead of stretching.
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
        }

        protected override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            Input.Update();
            if (DebugShots.Active) DebugShots.FakeMouse(dt);

            //Window Focus : when the player alt tabs away the game stops where it is. The page
            //on screen is told once (the duel pauses itself), then nothing moves until they return.
            bool active = IsActive || DebugShots.Active;
            if (!active && wasActive && Screens.Current != null) Screens.Current.LostFocus();
            wasActive = active;

            if (active) Screens.Update(dt);
            Ornament.UpdateGrain(dt);
            UpdateDebug(dt);

            base.Update(gameTime);
        }

        //Debug Update : count frames and read how much memory is in use
        private void UpdateDebug(float dt)
        {
            if (Input.KeyPressed(Keys.F3)) showDebug = !showDebug;
            if (!showDebug) return;

            frameCount++;
            frameTimer += dt;

            if (frameTimer >= 1f)
            {
                shownFps = frameCount;
                //false means "do not force a collection", we only want to read the number
                shownMemory = GC.GetTotalMemory(false) / 1048576f;
                frameCount = 0;
                frameTimer = 0f;
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            //Developer Capture : the tool has its own picture to draw into
            GraphicsDevice.SetRenderTarget(DebugShots.Active ? DebugShots.Target : screen);

            GraphicsDevice.Clear(Palette.Void);

            //LinearClamp smooths the fonts when they are drawn smaller than they were built
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);

            Screens.Draw(spriteBatch);

            //Grain : one layer of film grain over every page, so flat greys feel like print
            Ornament.Grain(spriteBatch);

            if (showDebug)
            {
                //Debug numbers are built here on purpose. They only exist while F3 is on,
                //so the string work never happens during normal play.
                string line = "FPS " + shownFps + "   MEM " + shownMemory.ToString("0.0") + " MB"
                            + "   SFX " + SoundBank.LoadedSfx + "   MUSIC " + SoundBank.LoadedMusic
                            + "   PHRASE " + SoundBank.LoadedPhrase
                            + "   HAND " + HandArt.LoadedPoses + "   ART " + ArtBank.Loaded + "   PIXEL " + CharacterArt.Loaded;
                Gfx.Rect(spriteBatch, 0, 0, 860, 24, Color.Black * 0.8f);
                Gfx.Text(spriteBatch, Font, line, 8, 3, Palette.Highlight, TextSize.Body);
            }

            spriteBatch.End();
            GraphicsDevice.SetRenderTarget(null);

            if (DebugShots.Active)
            {
                DebugShots.AfterDraw(this);
            }
            else
            {
                //Window Fit : black around the picture, then the picture itself
                GraphicsDevice.Clear(Color.Black);
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearClamp);
                spriteBatch.Draw(screen, view, Color.White);
                spriteBatch.End();
            }

            base.Draw(gameTime);
        }
    }
}
