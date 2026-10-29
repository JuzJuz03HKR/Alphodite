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
    //
    //Beta1 screens round 3 (29 Oct, docs/SCREENS_LEVEL_UP.md 20) : a small sister of the title page, drawn over the page.
    //The page goes dark, FERMATA (the sign for a held note) stands turned on the tall white bar at the left (menu1), and
    //the three lines sit on a big thin ring round TACET's hole, still and faint at the right (Ui.MenuItem, the ink bar
    //under the pointed one). A wide slanted band of black lies behind it all (Persona 5), so the menu reads the same over
    //any page. The menu has no lens of its own, it is drawn on top of the page's picture, so the hole is drawn with plain
    //shapes (Ornament.StillHole).
    public class PauseMenu
    {
        public bool Open;

        private TacetGame game;
        private ConfirmBox confirm = new ConfirmBox();
        private SettingsScreen settings;      // not null while the settings page is borrowed
        private int selected;
        private float appear;
        private float clock;                  // since the menu opened, the pieces come in one after another
        private float time;
        private Vector2 lastMouse;

        //Menu Lines
        private static string[] items = { "RESUME", "SETTINGS", "RETURN TO TITLE" };
        private static string[] itemIndex = { "01", "02", "03" };
        private const string EnterKey = "ENTER";
        private Rectangle[] boxes = new Rectangle[3];
        private Vector2[] itemAt = new Vector2[3];
        private UiMotion[] motion = new UiMotion[3];
        private const int MenuY = 318;
        private const int MenuStep = 62;

        //The Ring : TACET's hole at the right, and the big thin ring round it that the lines sit on, like the title page
        private static readonly Vector2 Hole = new Vector2(800f, 250f);
        private const float HoleRadius = 34f;
        private const float Orbit = 340f;
        private const int BarX = 300;
        private const float OrbitTop = MathHelper.Pi * 1.5f;                         // the ring is drawn from straight above the hole ...
        private const float OrbitSpan = 2.31f;                                       // ... round the left this far (radians), to just under the last line
        private static readonly Rectangle Band = new Rectangle(200, -10, 820, 740);     // the slanted band of black behind the menu
        private const int BandSlant = 200;

        //Saved Note : where CONTINUE will pick the run up again, made when the menu opens
        private string savedNote = "";
        private float noteX;

        public PauseMenu(TacetGame game)
        {
            this.game = game;
            for (int i = 0; i < items.Length; i++)
            {
                float y = MenuY + i * MenuStep;
                itemAt[i] = new Vector2(OrbitX(y), y);
                boxes[i] = new Rectangle((int)itemAt[i].X - 44, (int)(y - MenuStep / 2f), 470, MenuStep);
            }
            noteX = itemAt[items.Length - 1].X + 28f;
        }

        //Orbit X : where the big ring round the hole crosses the row y, on its left side
        private static float OrbitX(float y)
        {
            float dy = y - Hole.Y;
            return Hole.X - (float)Math.Sqrt(Math.Max(0f, Orbit * Orbit - dy * dy));
        }

        //Item In : how far menu line i has come in, the lines one after another
        private float ItemIn(int i)
        {
            return UiMotion.Between(clock, 0.08f + i * BattleRules.UiEnterStagger, BattleRules.UiEnterTime * 0.8f);
        }

        //Menu Show : stop the page underneath and open
        public void Show()
        {
            Open = true;
            selected = 0;
            appear = 0f;
            clock = 0f;
            settings = null;
            lastMouse = Input.MousePos;
            for (int i = 0; i < motion.Length; i++) motion[i] = new UiMotion();     // fresh, so the last press does not play again

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
            clock += dt;
            appear = Math.Min(1f, appear + dt * 5f);

            //Settings : the borrowed page runs until its own BACK closes it, then the menu comes in again
            if (settings != null)
            {
                settings.Update(dt);
                if (settings.Closed)
                {
                    settings = null;
                    clock = 0f;
                }
                return;
            }

            //POINT : the chosen line's ink bar shoots out once the line is in, the others pull theirs back
            for (int i = 0; i < motion.Length; i++)
                motion[i].Update(dt, i == selected && ItemIn(i) > 0.5f);

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
            motion[selected].Press();

            if (selected == 0)
            {
                Resume();
            }
            else if (selected == 1)
            {
                OpenSettings();
            }
            else
            {
                AskReturn();
            }
        }

        //Open Settings : borrow the settings page (SETTINGS, and the DEVELOPER TOOL's picture of it)
        public void OpenSettings()
        {
            SoundBank.Play(Sfx.UiConfirm);
            settings = new SettingsScreen(true);
            settings.Game = game;
            settings.Load();
        }

        //Ask Return : the box that asks before going back to the title page (RETURN TO TITLE, and the DEVELOPER TOOL)
        public void AskReturn()
        {
            confirm.Show("RETURN TO TITLE?",
                         "The run was saved when you last picked a path. CONTINUE on the title page carries on from there.",
                         "RETURN", "STAY");
        }

        public void Draw(SpriteBatch sb)
        {
            if (settings != null)
            {
                settings.Draw(sb);
                return;
            }

            //Hush : the page goes dark, darker still on the slanted band behind the menu, its edges thin lines of light
            float a = appear;
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Color.Black * (0.62f * a));
            Gfx.SlantBox(sb, Band, BandSlant, Palette.Void * (0.9f * a));
            Color edge = Palette.Paper * (0.16f * a);
            Gfx.Line(sb, Band.X + BandSlant, Band.Y, Band.X, Band.Bottom, edge, 1f);
            Gfx.Line(sb, Band.Right, Band.Y, Band.Right - BandSlant, Band.Bottom, edge, 1f);
            Gfx.DrawGlow(sb, 560f, 380f, 420f, Color.White * (0.03f * a));

            //TACET : still and faint at the right. The big ring round it draws itself down from the top and stops just under
            //the last line, a small ring at its end (menu3).
            float ring = UiMotion.EaseOut(MathHelper.Clamp(clock / 0.45f, 0f, 1f));
            float from = OrbitTop - OrbitSpan * ring;
            Color orbit = Palette.Paper * (0.2f * a);
            Gfx.Arc(sb, Hole.X, Hole.Y, Orbit, from, OrbitTop, orbit, 1f, 90);
            Gfx.CircleOutline(sb, Hole.X + Orbit * (float)Math.Cos(from), Hole.Y + Orbit * (float)Math.Sin(from), 3f, orbit, 1f);
            Ornament.StillHole(sb, Hole.X, Hole.Y, HoleRadius * UiMotion.EaseBack(MathHelper.Clamp(clock / 0.4f, 0f, 1f)), time * 0.02f, a);

            //Title Bar : FERMATA turned on the tall white bar, sliding in from the left
            float bar = UiMotion.Between(clock, 0f, 0.3f);
            int bx = BarX + (int)Math.Round((1f - bar) * -40f);
            Ui.TitleBar(sb, new Rectangle(bx, 170, 96, 380), 24, "FERMATA", bar * a);
            Gfx.TextSpaced(sb, Ui.Font, "PAUSED", bx + 4, 562, Palette.LineGrey * (bar * a), TextSize.Tiny, 4f);

            //Lines : on the big ring, sliding down it into place one after another
            for (int i = 0; i < items.Length; i++)
            {
                float k = ItemIn(i);
                if (k <= 0f) continue;
                float y = itemAt[i].Y - (1f - k) * 24f;
                Ui.MenuItem(sb, new Vector2(OrbitX(y), y), itemIndex[i], items[i], EnterKey, motion[i], k * a);
            }

            float low = UiMotion.Between(clock, 0.25f, 0.3f) * a;
            Gfx.TextSpaced(sb, Ui.Font, savedNote, noteX, 510, Palette.PaperDim * low, TextSize.Tiny, 3f);
            Gfx.TextSpaced(sb, Ui.Font, "ESC  RESUME   /   ENTER  CHOOSE", noteX, 534, Palette.LineGrey * low, TextSize.Tiny, 2f);

            confirm.Draw(sb);
        }
    }
}
