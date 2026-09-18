using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //BandNameScreen : name your ensemble. Shown once, right after the first path of a run
    //is chosen, and the game calls the band by that name for the rest of the run.
    public class BandNameScreen : GameScreen
    {
        private const int MaxLength = 20;
        private const string DefaultName = "THE NAMELESS";

        private Rectangle field = new Rectangle(340, 250, 600, 70);
        private Rectangle confirmButton = new Rectangle(490, 604, 300, 50);

        private string name = "";
        private float time;

        //Text Input Hook : MonoGame reports every typed character through this event.
        //It is the proper way to read typing, because it already understands the
        //keyboard layout, shift, and key repeat. Reading raw key states instead would
        //mean guessing at all three.
        public override void Load()
        {
            Game.Window.TextInput += HandleTyping;
        }

        //Screen Leave : always unhook, or this page keeps receiving keys after it is gone
        public override void Leave()
        {
            Game.Window.TextInput -= HandleTyping;
        }

        //Name Typing : one character at a time, straight from the keyboard
        private void HandleTyping(object sender, TextInputEventArgs e)
        {
            char c = e.Character;

            //Backspace : character code 8
            if (c == (char)8)
            {
                if (name.Length > 0) name = name.Substring(0, name.Length - 1);
                return;
            }

            //Control Keys : enter, tab and the rest are not text, drop them.
            //Anything below the space character is a control code.
            if (c < ' ') return;

            if (name.Length >= MaxLength) return;

            //Space : never at the start, never two in a row
            if (c == ' ')
            {
                if (name.Length > 0 && name[name.Length - 1] != ' ') name += ' ';
                return;
            }

            //Letters and numbers only, always shown in capitals
            if (char.IsLetterOrDigit(c)) name += char.ToUpper(c);
        }

        public override void Update(float dt)
        {
            time += dt;

            //Name Confirm : enter, or the button. An empty name gets the default.
            if (Input.KeyPressed(Keys.Enter) || Input.ClickedOn(confirmButton))
            {
                Game.CurrentRun.BandName = name.Length == 0 ? DefaultName : name;
                SoundBank.Play(Sfx.UiConfirm);

                //Opening Done : the floor opening is finished, routing starts at stage two
                Game.CurrentRun.LeaveOpening();
                Game.Screens.Change(new RouteScreen());
            }
        }

        public override void Draw(SpriteBatch sb)
        {
            SceneBackdrop.Draw(sb, time);

            float cx = TacetGame.ScreenW / 2f;

            //Heading
            Gfx.TextSpacedCentered(sb, Game.Font, "BEFORE THE FIRST STAGE", cx, 118, Palette.LineGrey, TextSize.Tiny, 5f);
            Gfx.TextCentered(sb, Game.BigFont, "Name your ensemble", cx, 164, Palette.Paper, TextSize.Title);
            Gfx.TextCentered(sb, Game.StoryFont, "This is what the world will call you.", cx, 206, Palette.PaperDim, TextSize.Story);

            //Input Line : no box, just the name sitting on a rule, like a signature
            string shown = name.Length == 0 ? DefaultName : name;
            Color shownColor = name.Length == 0 ? Palette.LineGrey : Palette.Highlight;
            Gfx.TextCentered(sb, Game.BigFont, shown, field.Center.X, field.Center.Y, shownColor, TextSize.Hero);
            Ornament.Divider(sb, field.Center.X, field.Bottom + 6, field.Width / 2f, Palette.PaperDim);

            //Caret : blinks so it is obvious the field is waiting for typing
            if ((int)(time * 2f) % 2 == 0)
            {
                float width = Gfx.TextWidth(Game.BigFont, shown, TextSize.Hero);
                Gfx.Rect(sb, field.Center.X + width / 2f + 8, field.Center.Y - 22, 2, 44, Palette.Highlight);
            }

            DrawLineup(sb, cx);

            Ui.Button(sb, confirmButton, "TAKE THE STAGE", "ENTER", true);

            //Hint
            Gfx.TextSpacedCentered(sb, Game.Font, "TYPE A NAME   /   BACKSPACE TO DELETE   /   ENTER TO CONFIRM",
                                   cx, 690, Palette.LineGrey, TextSize.Tiny, 2f);
        }

        //Lineup : the band that is being named, standing in a row under the name
        private void DrawLineup(SpriteBatch sb, float cx)
        {
            List<Musician> roster = Game.CurrentRun.Roster;
            int count = roster.Count;
            int gap = 90;
            float left = cx - (count - 1) * gap / 2f;

            Gfx.DrawGlowBox(sb, new Rectangle((int)cx - 260, 380, 520, 200), Palette.Paper * 0.08f);
            Gfx.Rect(sb, cx - 220, 540, 440, 1, Palette.LineGrey);

            for (int i = 0; i < count; i++)
            {
                float x = left + i * gap;
                float bob = (float)Math.Sin(time * 2f + i) * 2f;
                Rectangle cap = new Rectangle((int)x - 22, (int)(420 + bob), 44, 116);
                MusicianArt.Capsule(sb, cap, roster[i], Palette.Paper, Palette.Void, 0f);
                Gfx.TextSpacedCentered(sb, Game.Font, roster[i].NameTag, x, 552, Palette.PaperDim, TextSize.Tiny, 2f);
            }
        }
    }
}
