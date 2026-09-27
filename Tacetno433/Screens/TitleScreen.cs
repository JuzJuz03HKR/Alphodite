using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //TitleScreen : the first page of the game.
    //
    //The left side is a written score with a playhead running across it. When the
    //playhead reaches a note the note flashes, which is exactly what the sequencer does
    //during a duel, so the title page teaches the core idea before the player presses
    //anything. The staff runs off to the right and gets swallowed by TACET.
    //
    //Under the logo sit the slanted menu blades, and a small notice strip at the
    //bottom cycles through play tips, the way a launcher shows news.
    //When a run is saved, CONTINUE sits on top and says where the run was left.
    //Quitting, and starting over a saved run, both ask first.
    public class TitleScreen : GameScreen
    {
        //Can Pause : a menu page outside the run, ESC here means going back
        public override bool CanPause
        {
            get { return false; }
        }

        //Title Menu Data : CONTINUE is only there when a run is saved
        private const int ItemContinue = 0;
        private const int ItemNewRun = 1;
        private const int ItemTutorial = 2;
        private const int ItemGuide = 3;
        private const int ItemSettings = 4;
        private const int ItemQuit = 5;
        private static string[] allItems = { "CONTINUE", "NEW RUN", "TUTORIAL", "HOW TO PLAY", "SETTINGS", "QUIT" };
        private static string[] allIndex = { "01", "02", "03", "04", "05", "06" };
        private int[] menu;                 // which items are on the menu, top to bottom
        private Rectangle[] menuBoxes;
        private int selected;
        private string continueNote = "";   // where the saved run was left
        private ConfirmBox askBox = new ConfirmBox();
        private int confirmFor = -1;        // the item waiting for a yes

        //Title Tips : the notice strip at the bottom, one every few seconds
        private static string[] tips =
        {
            "A beat TACET wins knocks breath out of the band. At zero, it collapses.",
            "A small stroke brings the back row, a big one the whole band. Everyone who plays pays stamina.",
            "PERFECT strokes in a row build a combo. A miss breaks it.",
            "The baton points at one side of the stage on every beat. Whoever sits there hits harder.",
            "TACET's notes say f, mf or p. Answer to match: the ruler on the baton says the same.",
            "A ??? beat hides its strength until the clash.",
            "Motifs last the whole run. Pick the ones that suit your conductor.",
            "A PERFECT f against a real f note is a COUNTER. It throws part of the note back.",
            "From floor two, a note tied to a spark: answer it, then flick once more, any way, on the half beat.",
            "Rounds end with a roll on floor one, and always for elites and bosses. Shake as fast as you can.",
            "From floor two, a note under an arch is a fermata. Stroke it, then hold still to the end.",
            "Eight PERFECTs in a row set the band on fire. FORTISSIMO hits harder for four beats.",
            "Far enough ahead at the end of a round, conduct the FINALE and end the duel at once.",
            "Where TACET is silent, let the beat pass to rest, or swing for a free hit.",
            "Strokes judged early or late? Settings has a STROKE TIMING test.",
        };
        private const float TipTime = 6f;

        //Title Layout : up to six blades between the hook line and the tip strip
        private const int MenuX = 96;
        private const int MenuY = 350;
        private const int MenuStep = 44;
        private const int MenuW = 400;
        private const int MenuH = 40;
        private const int NoticeY = 616;

        //Staff Layout : five lines, sixteen pixels apart, so half a step is eight pixels
        private const int StaffTop = 70;
        private const int StaffGap = 12;
        private const int StaffLeft = 80;
        private const int NoteFirstX = 110;
        private const int NoteStepW = 52;

        //Title Notation : the phrase written on the staff.
        //step is the slot along the staff, pitch is how far down the note head sits.
        private int[] noteStep = { 0, 1, 3, 4, 6, 7, 8, 10, 11, 13, 14, 16, 17, 19, 20 };
        private int[] notePitch = { 6, 4, 5, 3, 4, 2, 5, 3, 1, 4, 2, 5, 3, 4, 2 };

        //Title Animation
        private float time;
        private float playhead = StaffLeft;
        private float selectSlide;          // eases the white blade toward the chosen row
        private Vector2 lastMouse;

        public override void Load()
        {
            //No Run : the title page is outside any run
            Game.CurrentRun = null;

            //Menu Lines : CONTINUE first when there is a saved run, and it starts chosen
            bool saved = SaveFile.HasRun;
            if (saved) menu = new int[] { ItemContinue, ItemNewRun, ItemTutorial, ItemGuide, ItemSettings, ItemQuit };
            else menu = new int[] { ItemNewRun, ItemTutorial, ItemGuide, ItemSettings, ItemQuit };
            continueNote = saved ? SaveFile.RunSummary() : "";
            selected = 0;

            //Menu Boxes : one clickable box per line, built once
            menuBoxes = new Rectangle[menu.Length];
            for (int i = 0; i < menu.Length; i++)
                menuBoxes[i] = new Rectangle(MenuX, MenuY + i * MenuStep, MenuW, MenuH);

            selectSlide = selected;
            lastMouse = Input.MousePos;
            SoundBank.PlayMusic(Music.Title);
        }

        public override void Update(float dt)
        {
            time += dt;
            int before = selected;

            //Playhead Sweep : runs left to right, then vanishes into TACET and starts over
            playhead += dt * 170f;
            if (playhead > 1180f) playhead = StaffLeft;

            //Asking : while the box is open nothing else on the page moves
            if (askBox.Open)
            {
                int answer = askBox.Update(dt);
                if (answer == 1) Act(confirmFor);
                return;
            }

            //Menu Keyboard
            if (Input.KeyPressed(Keys.Down) || Input.KeyPressed(Keys.S))
                selected = (selected + 1) % menu.Length;
            if (Input.KeyPressed(Keys.Up) || Input.KeyPressed(Keys.W))
                selected = (selected - 1 + menu.Length) % menu.Length;

            //Menu Mouse : only take over the selection when the mouse actually moves,
            //otherwise a resting cursor would fight the arrow keys
            if (Input.MousePos != lastMouse)
            {
                lastMouse = Input.MousePos;
                for (int i = 0; i < menuBoxes.Length; i++)
                    if (Input.MouseOver(menuBoxes[i])) selected = i;
            }

            if (selected != before) SoundBank.Play(Sfx.UiMove);
            selectSlide += (selected - selectSlide) * Math.Min(1f, dt * 14f);

            //Menu Confirm
            bool confirm = Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Space);
            for (int i = 0; i < menuBoxes.Length; i++)
                if (Input.ClickedOn(menuBoxes[i])) { selected = i; confirm = true; }

            if (confirm) Choose(menu[selected]);

            if (Input.KeyPressed(Keys.Escape)) Choose(ItemQuit);
        }

        //Menu Choose : the two lines that cannot be undone ask first
        private void Choose(int item)
        {
            if (item == ItemQuit)
            {
                Ask(item, "QUIT THE GAME?", "Your settings are kept. A run in progress is saved at its last path.", "QUIT", "STAY");
                return;
            }
            if (item == ItemNewRun && SaveFile.HasRun)
            {
                Ask(item, "START A NEW RUN?", "The saved run will be thrown away. There is only room for one.", "NEW RUN", "KEEP IT");
                return;
            }
            Act(item);
        }

        private void Ask(int item, string title, string line, string yes, string no)
        {
            confirmFor = item;
            askBox.Show(title, line, yes, no);
        }

        //Menu Action : what each line does
        private void Act(int item)
        {
            SoundBank.Play(Sfx.UiConfirm);

            if (item == ItemContinue)
            {
                if (!RunFlow.Continue(Game))
                {
                    //Broken Save : a file from another version, or damaged. Start clean.
                    SaveFile.DeleteRun();
                    SoundBank.Play(Sfx.UiDenied);
                    Game.Screens.Change(new TitleScreen());
                }
            }
            else if (item == ItemNewRun)
            {
                SaveFile.DeleteRun();
                Game.Screens.Change(new ConductorSelectScreen());
            }
            else if (item == ItemTutorial)
                Game.Screens.Change(new TutorialScreen());
            else if (item == ItemGuide)
                Game.Screens.Change(new GuideScreen());
            else if (item == ItemSettings)
                Game.Screens.Change(new SettingsScreen());
            else
                Game.Exit();
        }

        public override void Draw(SpriteBatch sb)
        {
            DrawBackground(sb);
            DrawScore(sb);

            //TACET : the silence sits at the right, breathing slowly, eating the end of the score
            float edge = 860f + (float)Math.Sin(time * 0.5f) * 25f;
            TacetField.Draw(sb, edge, time, 0.15f);
            DrawDarkSide(sb, edge);

            DrawLogo(sb);
            DrawMenu(sb);
            DrawNotice(sb);
            DrawFooter(sb);
            askBox.Draw(sb);
        }

        //Background : dark stage with a soft pool of light, and darker edges
        private void DrawBackground(SpriteBatch sb)
        {
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageDeep);
            Gfx.DrawGlowBox(sb, new Rectangle(-300, -200, 1300, 900), Palette.Paper * 0.08f);
            Gfx.DrawGlow(sb, 300, 250, 260, Palette.Paper * 0.05f);

            //Page Rules : thin lines like the margins of a printed programme
            Gfx.Rect(sb, 60, 30, 1, TacetGame.ScreenH - 60, Palette.Paper * 0.08f);
            Gfx.Rect(sb, 60, 30, 760, 1, Palette.Paper * 0.08f);
            Gfx.TextSpaced(sb, Game.Font, "ALPHODITE", 80, 38, Palette.LineGrey, TextSize.Tiny, 4f);
            Gfx.TextSpaced(sb, Game.Font, "A DUEL AGAINST SILENCE", 470, 38, Palette.LineGrey, TextSize.Tiny, 4f);
            Ornament.Crosses(sb, 740, 44, 4, 1, 16, Palette.LineGrey);

            Ornament.Vignette(sb, 60, 0.55f);
        }

        //Score : the staff, the written notes, and the playhead running across them
        private void DrawScore(SpriteBatch sb)
        {
            //Staff Lines : run all the way to the right edge so TACET can eat the end of them
            Ornament.Stave(sb, StaffLeft, StaffTop, TacetGame.ScreenW - StaffLeft, StaffGap, Palette.LineGrey * 0.7f);

            //Bar Lines : a stroke every four slots, so it reads as real notation
            for (int step = 0; step <= 20; step += 4)
                Gfx.Rect(sb, NoteFirstX + step * NoteStepW - 26, StaffTop, 1, StaffGap * 4, Palette.LineGrey * 0.55f);

            //Treble Opening : a double bar at the very start of the staff
            Gfx.Rect(sb, StaffLeft, StaffTop, 3, StaffGap * 4, Palette.PaperDim);
            Gfx.Rect(sb, StaffLeft + 6, StaffTop, 1, StaffGap * 4, Palette.PaperDim);

            //Playhead : the needle. This is the same needle the duel page uses.
            Gfx.Rect(sb, playhead, StaffTop - 14, 2, StaffGap * 4 + 28, Palette.Paper * 0.55f);
            Gfx.DrawGlow(sb, playhead, StaffTop + StaffGap * 2, 46f, Palette.Paper * 0.10f);

            //Note Heads : flash as the playhead passes over them
            for (int i = 0; i < noteStep.Length; i++)
            {
                float x = NoteFirstX + noteStep[i] * NoteStepW;
                float y = StaffTop + notePitch[i] * (StaffGap / 2f);

                //Pulse : one when the playhead is exactly on the note, zero when far away
                float distance = Math.Abs(playhead - x);
                float pulse = 0f;
                if (distance < 40f) pulse = 1f - distance / 40f;

                Color noteColor = Color.Lerp(Palette.PaperDim, Palette.Highlight, pulse);

                Gfx.Rect(sb, x + 4, y - 22, 1, 22, noteColor * 0.8f);
                Gfx.Circle(sb, x, y, 4.5f + pulse * 2f, noteColor);

                if (pulse > 0.1f)
                    Gfx.DrawGlow(sb, x, y, 12f + pulse * 26f, Palette.Paper * (pulse * 0.30f));
            }
        }

        //Dark Side : writing inside TACET's black, the poster side of the page
        private void DrawDarkSide(SpriteBatch sb, float edge)
        {
            //Black Sun : TACET's eclipse hanging in its own dark, with holes of silence drifting up
            Hollow.Eclipse(sb, 1050f, 330f, 108f, time * 0.2f, 1f);
            for (int i = 0; i < 10; i++)
            {
                float x = 930f + (i * 67) % 260;
                float y = 720f - ((i * 131) % 700 + time * (14f + i % 4 * 6f)) % 720f;
                if (i % 3 == 0) Hollow.Ring(sb, x, y, 5f + i % 3 * 3f, 1.5f, 0.45f);
                else Gfx.DrawGlow(sb, x, y, 7f, Palette.Paper * 0.25f);
            }

            Gfx.TextVertical(sb, Game.Font, "SILENCE  IS  ALSO  A  SOUND", 1236, 90, Palette.PaperDim * 0.8f, TextSize.Label);
            Gfx.Rect(sb, 1222, 76, 1, 480, Palette.LineGrey * 0.4f);

            Gfx.TextRight(sb, Game.BigFont, "No.433", 1190, 600, Palette.Paper * 0.8f, TextSize.Subtitle);
            Ornament.Barcode(sb, 1060, 646, 130, 22, Palette.PaperDim * 0.6f);
            Gfx.TextSpacedRight(sb, Game.Font, "FOUR MINUTES  THIRTY THREE SECONDS", 1190, 676, Palette.LineGrey, TextSize.Tiny, 2f);

        }

        //Logo : game name, subtitle and the hook line
        private void DrawLogo(SpriteBatch sb)
        {
            Gfx.Text(sb, Game.LogoFont, "TACET", 82, 128, Palette.Void, TextSize.Logo);      // drop shadow
            Gfx.Text(sb, Game.LogoFont, "TACET", 78, 124, Palette.Paper, TextSize.Logo);

            float y = 248;
            Gfx.Rect(sb, 84, y + 16, 60, 1, Palette.PaperDim);
            Gfx.TextSpaced(sb, Game.BigFont, "4'33", 156, y, Palette.Paper, TextSize.Subtitle, 8f);
            Gfx.Rect(sb, 262, y + 16, 180, 1, Palette.PaperDim);
            Gfx.Diamond(sb, 446, y + 16, 3, Palette.PaperDim);

            Gfx.Text(sb, Game.StoryFont, "You do not play the notes.", 86, 296, Palette.Highlight, TextSize.Story);
            Gfx.Text(sb, Game.StoryFont, "You decide who does.", 86, 318, Palette.PaperDim, TextSize.Story);
        }

        //Menu : slanted blades, the chosen one turns solid white
        private void DrawMenu(SpriteBatch sb)
        {
            //Chosen Blade : slides between rows instead of jumping
            int bladeY = (int)(MenuY + selectSlide * MenuStep);
            Rectangle blade = new Rectangle(MenuX - 16, bladeY, MenuW, MenuH);
            Gfx.SlantBox(sb, new Rectangle(blade.X + 6, blade.Y + 6, blade.Width, blade.Height), Ui.Slant, Color.Black * 0.5f);
            Gfx.SlantBox(sb, blade, Ui.Slant, Palette.Paper);
            Gfx.Rect(sb, blade.Right, blade.Center.Y, 300, 1, Palette.Paper * 0.35f);

            for (int i = 0; i < menu.Length; i++)
            {
                Rectangle box = menuBoxes[i];
                float closeness = 1f - Math.Min(1f, Math.Abs(selectSlide - i));
                Color textColor = Color.Lerp(Palette.PaperDim, Palette.Ink, closeness);

                Gfx.Text(sb, Game.Font, allIndex[i], box.X + 8, box.Y + 13, Color.Lerp(Palette.LineGrey, Palette.InkSoft, closeness), TextSize.Label);
                Gfx.TextSpaced(sb, Game.BigFont, allItems[menu[i]], box.X + 48 + closeness * 8f, box.Y + 3, textColor, TextSize.Small, 3f);

                //Continue Note : who, which floor, which stage, beside the blade
                if (menu[i] == ItemContinue)
                    Gfx.TextSpaced(sb, Game.Font, continueNote, box.Right + 20, box.Y + 4, Palette.PaperDim, TextSize.Tiny, 2f);

                if (i == selected)
                {
                    float bob = (float)Math.Sin(time * 6f) * 2f;
                    Gfx.Diamond(sb, box.X - 32 + bob, box.Center.Y, 5, Palette.Accent);
                }
            }
        }

        //Notice : the news strip, one tip at a time, fading between them
        private void DrawNotice(SpriteBatch sb)
        {
            int index = (int)(time / TipTime) % tips.Length;
            float phase = (time % TipTime) / TipTime;
            float a = phase < 0.1f ? phase / 0.1f : (phase > 0.9f ? (1f - phase) / 0.1f : 1f);

            Rectangle strip = new Rectangle(80, NoticeY, 720, 38);
            Gfx.Rect(sb, strip, Palette.Void * 0.6f);
            Gfx.Rect(sb, strip.X, strip.Y, 3, strip.Height, Palette.Paper);
            Ui.Tag(sb, "NOTE", strip.X + 16, strip.Y + 10, true, 1f);
            Gfx.Text(sb, Game.StoryFont, tips[index], strip.X + 78, strip.Y + 9, Palette.Paper * a, TextSize.StorySmall);

            //Tip Dots : which tip out of how many
            for (int i = 0; i < tips.Length; i++)
                Gfx.Diamond(sb, strip.Right - 16 - (tips.Length - 1 - i) * 12, strip.Center.Y, i == index ? 3 : 2,
                            i == index ? Palette.Paper : Palette.LineGrey);
        }

        //Footer : controls and build tag
        private void DrawFooter(SpriteBatch sb)
        {
            Gfx.TextSpaced(sb, Game.Font, "ARROWS OR MOUSE  /  ENTER TO CONFIRM  /  ESC TO QUIT", 82, 668, Palette.LineGrey, TextSize.Tiny, 2f);
            Gfx.TextSpaced(sb, Game.Font, "PROTOTYPE BUILD", 82, 688, Palette.LineGrey * 0.7f, TextSize.Tiny, 2f);
        }
    }
}
