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

        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

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

        public TacetGame()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = ScreenW;
            graphics.PreferredBackBufferHeight = ScreenH;
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
            Gfx.Init(GraphicsDevice);
            NumberText.Build();

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

            //Text Prepare : wrap all the long text ONCE, never while drawing
            ConductorList.PrepareText(StoryFont, TextWrapWidth, TextSize.Story);
            EraList.PrepareText(StoryFont, PanelStrip.CaptionWrapWidth);
            MusicianList.PrepareText(StoryFont, MusicianWrapWidth);
            EnemyList.PrepareText();
            MotifList.PrepareText(StoryFont, MotifCardWidth, MotifTipWidth);
            EventList.PrepareText(StoryFont, EventTextWidth, EventTextWidth);

            //First Screen : the title page, or the capture tool when it was asked for
            if (DebugShots.Active)
                DebugShots.Begin(this);
            else
                Screens.ChangeNow(new TitleScreen());
        }

        protected override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            Input.Update();
            Screens.Update(dt);
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
            //Developer Capture : draw into a picture instead of the window
            if (DebugShots.Active) GraphicsDevice.SetRenderTarget(DebugShots.Target);

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
                            + "   SFX " + SoundBank.LoadedSfx + "   MUSIC " + SoundBank.LoadedMusic;
                Gfx.Rect(spriteBatch, 0, 0, 440, 24, Color.Black * 0.8f);
                Gfx.Text(spriteBatch, Font, line, 8, 3, Palette.Highlight, TextSize.Body);
            }

            spriteBatch.End();

            if (DebugShots.Active)
            {
                GraphicsDevice.SetRenderTarget(null);
                DebugShots.AfterDraw(this);
            }

            base.Draw(gameTime);
        }
    }
}
