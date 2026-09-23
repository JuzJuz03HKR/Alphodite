using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;

namespace Tacetno433.Screens
{
    //SettingsScreen : the options page, opened from the title menu.
    //
    //Five rows: three volume sliders, the language choice, and a full screen tick box.
    //Up and down pick a row, left and right change it, and the mouse can drag the sliders
    //or click the chips. The panel on the right explains whichever row is chosen.
    //
    //The numbers themselves live in Core/Settings.cs. Nothing is written to disk yet, so
    //the options go back to their starting values when the game is closed.
    public class SettingsScreen : GameScreen
    {
        //Setting Rows
        private const int RowMaster = 0;
        private const int RowSfx = 1;
        private const int RowMusic = 2;
        private const int RowLanguage = 3;
        private const int RowFull = 4;
        private const int RowCount = 5;

        //Settings Layout
        private const int PanelX = 80;
        private const int PanelY = 140;
        private const int PanelW = 700;
        private const int PanelH = 440;
        private const int RowX = 96;
        private const int RowY = 168;
        private const int RowStep = 76;
        private const int RowW = 668;
        private const int RowH = 60;
        private const int TrackX = 430;
        private const int TrackW = 260;
        private const int ValueRight = 762;
        private const int HelpWrap = 340;

        private static string[] rowIndex = { "01", "02", "03", "04", "05" };
        private static string[] rowNames =
        {
            "MASTER VOLUME",
            "SOUND EFFECTS",
            "MUSIC",
            "LANGUAGE",
            "FULL SCREEN"
        };

        //Row Help : the sentence shown on the right for whichever row is chosen
        private static string[] rowHelp =
        {
            "The main fader. Every sound in the game is measured against this one.",
            "Button clicks, baton strokes, and every clash in a duel.",
            "The track playing behind the page you are on.",
            "Which language the game is written in. Only English is written so far, so the choice is remembered but the words do not change yet.",
            "Fills the whole monitor without changing its resolution, so other programs are left where they are and alt tab is instant. The picture keeps its shape and black bars fill whatever is left over."
        };
        private static string savedNote =
            "Options last until the game is closed. A save file for them comes later.";

        private string[] helpWrapped = new string[RowCount];
        private string savedWrapped = "";

        //Setting Boxes : worked out once in Load
        private Rectangle[] rowBoxes = new Rectangle[RowCount];
        private Rectangle[] tracks = new Rectangle[3];      // the drawn rail of each slider
        private Rectangle[] grabs = new Rectangle[3];       // the easier area the mouse can grab
        private Rectangle[] langBoxes = new Rectangle[2];
        private Rectangle fullBox;
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

            //Help Text : wrapped once here, never while drawing
            for (int i = 0; i < RowCount; i++)
                helpWrapped[i] = Gfx.WrapText(Game.StoryFont, rowHelp[i], HelpWrap, TextSize.StorySmall);
            savedWrapped = Gfx.WrapText(Game.StoryFont, savedNote, HelpWrap, TextSize.StorySmall);

            lastMouse = Input.MousePos;
            SoundBank.PlayMusic(Music.Title);
        }

        public override void Update(float dt)
        {
            time += dt;
            enter = Math.Min(1f, enter + dt * 2.5f);

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
                Game.Screens.Change(new TitleScreen());
            }
        }

        //Key Hold : holding left or right keeps a slider sliding after a short wait
        private void UpdateHold(float dt)
        {
            int dir = 0;
            if (Input.KeyDown(Keys.Left) || Input.KeyDown(Keys.A)) dir = -1;
            if (Input.KeyDown(Keys.Right) || Input.KeyDown(Keys.D)) dir = 1;

            if (dir == 0 || selected > RowMusic)
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
            SetVolume(selected, VolumeOf(selected) + dir * 0.02f);
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
            }

            if (!Input.MouseDown()) dragging = -1;

            if (dragging >= 0)
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
            else SetFullscreen(dir > 0);
        }

        //Enter Press : the two rows that are simply on or off flip over
        private void Press(int row)
        {
            if (row == RowLanguage) SetLanguage(Settings.Language == 0 ? 1 : 0);
            else if (row == RowFull) SetFullscreen(!Settings.Fullscreen);
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

            DrawHelp(sb, e);

            Ui.Button(sb, backButton, "BACK", "ESC", false, enter >= 1f, e);

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
            else DrawFullscreenRow(sb, on, a);
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

            Ui.Tag(sb, "NOT SAVED YET", helpBox.X, helpBox.Bottom + 30, false, e);
            Gfx.Text(sb, Game.StoryFont, savedWrapped, helpBox.X, helpBox.Bottom + 58, Palette.LineGrey * e, TextSize.StorySmall);
        }
    }
}
