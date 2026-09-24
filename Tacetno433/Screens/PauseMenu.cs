using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //PauseMenu : the menu ESC opens on every page of a run.
    //
    //It is not a page of its own. It sits on top of whatever page is on show: TacetGame stops
    //updating that page while the menu is open, but still draws it underneath, so the player
    //comes back to exactly where they were. Alt tab opens it too.
    //   RESUME           close the menu (ESC does the same). The duel counts the band back in.
    //   SETTINGS         the settings page, borrowed and drawn over everything until it closes
    //   RETURN TO TITLE  asks first. The run was saved when the last path was picked, so
    //                    CONTINUE on the title page carries on from there.
    public class PauseMenu
    {
        public bool Open;

        private TacetGame game;
        private ConfirmBox confirm = new ConfirmBox();
        private SettingsScreen settings;      // not null while the settings page is borrowed
        private int selected;
        private float appear;
        private float time;
        private Vector2 lastMouse;

        //Menu Lines
        private static string[] items = { "RESUME", "SETTINGS", "RETURN TO TITLE" };
        private static string[] itemIndex = { "01", "02", "03" };
        private Rectangle[] boxes = new Rectangle[3];
        private const int MenuX = 470;
        private const int MenuY = 330;
        private const int MenuStep = 62;
        private const int MenuW = 380;
        private const int MenuH = 48;

        //Saved Note : where CONTINUE will pick the run up again, made when the menu opens
        private string savedNote = "";

        public PauseMenu(TacetGame game)
        {
            this.game = game;
            for (int i = 0; i < items.Length; i++)
                boxes[i] = new Rectangle(MenuX, MenuY + i * MenuStep, MenuW, MenuH);
        }

        //Menu Show : stop the page underneath and open
        public void Show()
        {
            Open = true;
            selected = 0;
            appear = 0f;
            settings = null;
            lastMouse = Input.MousePos;

            RunState run = game.CurrentRun;
            savedNote = run != null && SaveFile.HasRun ? "THE RUN IS SAVED  /  " + run.FloorShort + "  /  " + run.StageLabel : "";

            game.Screens.Current.Paused();
            SoundBank.Play(Sfx.UiBack);
        }

        //Menu Close : hand the frame back to the page
        private void Resume()
        {
            Open = false;
            SoundBank.Play(Sfx.UiConfirm);
            game.Screens.Current.Resumed();
        }

        public void Update(float dt)
        {
            time += dt;
            appear = Math.Min(1f, appear + dt * 5f);

            //Settings : the borrowed page runs until its own BACK closes it
            if (settings != null)
            {
                settings.Update(dt);
                if (settings.Closed) settings = null;
                return;
            }

            //Return To Title : only once the player has said yes
            if (confirm.Open)
            {
                int answer = confirm.Update(dt);
                if (answer == 1)
                {
                    Open = false;
                    SoundBank.StopMusic();
                    game.Screens.Change(new TitleScreen());
                }
                return;
            }

            int before = selected;
            if (Input.KeyPressed(Keys.Down) || Input.KeyPressed(Keys.S)) selected = (selected + 1) % items.Length;
            if (Input.KeyPressed(Keys.Up) || Input.KeyPressed(Keys.W)) selected = (selected - 1 + items.Length) % items.Length;
            if (Input.MousePos != lastMouse)
            {
                lastMouse = Input.MousePos;
                for (int i = 0; i < boxes.Length; i++)
                    if (Input.MouseOver(boxes[i])) selected = i;
            }
            if (selected != before) SoundBank.Play(Sfx.UiMove);

            if (Input.KeyPressed(Keys.Escape))
            {
                Resume();
                return;
            }

            bool press = Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Space);
            for (int i = 0; i < boxes.Length; i++)
                if (Input.ClickedOn(boxes[i])) { selected = i; press = true; }
            if (!press) return;

            if (selected == 0)
            {
                Resume();
            }
            else if (selected == 1)
            {
                SoundBank.Play(Sfx.UiConfirm);
                settings = new SettingsScreen(true);
                settings.Game = game;
                settings.Load();
            }
            else
            {
                confirm.Show("RETURN TO TITLE?",
                             "The run was saved when you last picked a path. CONTINUE on the title page carries on from there.",
                             "RETURN", "STAY");
            }
        }

        public void Draw(SpriteBatch sb)
        {
            if (settings != null)
            {
                settings.Draw(sb);
                return;
            }

            float a = appear;
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Color.Black * (0.7f * a));

            //Card : a dark card behind the menu, so it reads over any page
            Rectangle card = new Rectangle(380, 140, 520, 446);
            Gfx.Rect(sb, card, Palette.Void * (0.94f * a));
            Ornament.DoubleFrame(sb, card, Palette.Paper * a);

            //Fermata : the sign written over a note that is held as long as the conductor likes.
            //An arch with a dot under it, drawn in the middle of the title.
            float cx = 640f;
            NoteGlyph.FermataSign(sb, cx, 190f, 30f, Palette.Highlight * a);
            Hollow.Streak(sb, cx, 204f, 440f * a, 0.5f * a);

            Gfx.TextCentered(sb, Ui.LogoFont, "FERMATA", cx, 250f, Palette.Highlight * a, TextSize.Banner * 0.8f);
            Gfx.TextSpacedCentered(sb, Ui.Font, "PAUSED", cx, 288f, Palette.PaperDim * a, TextSize.Label, 6f);

            //Blades : the chosen one turns solid white, the same as the title menu
            for (int i = 0; i < items.Length; i++)
            {
                Rectangle box = boxes[i];
                bool on = i == selected;
                if (on)
                {
                    Gfx.SlantBox(sb, new Rectangle(box.X + 5, box.Y + 5, box.Width, box.Height), Ui.Slant, Color.Black * (0.5f * a));
                    Gfx.SlantBox(sb, box, Ui.Slant, Palette.Paper * a);
                    float bob = (float)Math.Sin(time * 6f) * 2f;
                    Gfx.Diamond(sb, box.X - 22 + bob, box.Center.Y, 5, Palette.Accent * a);
                }
                else
                {
                    Gfx.SlantOutline(sb, box, Ui.Slant, Palette.LineGrey * a, 1f);
                }

                Color text = (on ? Palette.Ink : Palette.PaperDim) * a;
                Gfx.Text(sb, Ui.Font, itemIndex[i], box.X + 22, box.Y + 17, (on ? Palette.InkSoft : Palette.LineGrey) * a, TextSize.Label);
                Gfx.TextSpaced(sb, Ui.BigFont, items[i], box.X + 60, box.Y + 7, text, TextSize.Small, 3f);
            }

            Gfx.TextSpacedCentered(sb, Ui.Font, savedNote, cx, 524f, Palette.PaperDim * a, TextSize.Tiny, 3f);
            Gfx.TextSpacedCentered(sb, Ui.Font, "ESC TO RESUME  /  ENTER TO CHOOSE", cx, 552f, Palette.LineGrey * a, TextSize.Tiny, 2f);

            confirm.Draw(sb);
        }
    }
}
