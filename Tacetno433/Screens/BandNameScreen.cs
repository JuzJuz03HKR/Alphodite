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
    //
    //Beta1 screens round 2 (29 Oct, docs/SCREENS_LEVEL_UP.md 19) : the name sits on an orbit rule like a signature, and the
    //band that is being named stands under it as the same tall columns as the band page (Core/BandColumn.cs), each in its
    //own soft light. Every letter typed lands with a small drop.
    public class BandNameScreen : GameScreen
    {
        private const int MaxLength = 20;
        private const string DefaultName = "THE NAMELESS";

        private Rectangle field = new Rectangle(340, 236, 600, 70);
        private Rectangle confirmButton = new Rectangle(490, 640, 300, 48);
        private const int LineupTop = 368;
        private const int ColumnW = 96;
        private const int ColumnH = 168;

        private string name = "";
        private float time;
        private float typed = 99f;                     // seconds since the last letter, for its little drop
        private UiMotion confirmMotion = new UiMotion();

        //Wipe Word : the page's name in the wipe on the way here (WIPE WORD)
        public override string WipeWord
        {
            get { return "NAME YOUR ENSEMBLE"; }
        }

        //Text Input Hook : MonoGame reports every typed character through this event.
        //It is the proper way to read typing, because it already understands the
        //keyboard layout, shift, and key repeat. Reading raw key states instead would
        //mean guessing at all three.
        public override void Load()
        {
            Game.Window.TextInput += HandleTyping;
            Game.CurrentRun.SyncOrder();
            Lens.Ambient = 0.42f;
            Lens.SpotPower = 0f;
            Lens.Vignette = 0.55f;
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
            if (char.IsLetterOrDigit(c))
            {
                name += char.ToUpper(c);
                typed = 0f;
            }
        }

        public override void Update(float dt)
        {
            time += dt;
            typed += dt;
            confirmMotion.Update(dt, Input.MouseOver(confirmButton));

            //Name Confirm : enter, or the button. An empty name gets the default.
            if (Input.KeyPressed(Keys.Enter) || Input.ClickedOn(confirmButton))
            {
                confirmMotion.Press();
                Game.CurrentRun.BandName = name.Length == 0 ? DefaultName : name;
                SoundBank.Play(Sfx.UiConfirm);

                //Opening Done : the floor opening is finished, routing starts at stage two
                Game.CurrentRun.LeaveOpening();
                Game.Screens.Change(new RouteScreen());
            }
        }

        //Column Rect : member i of the band under the name, risen into place
        private Rectangle ColumnRect(int i, int count)
        {
            int gap = 18;
            int total = count * ColumnW + (count - 1) * gap;
            int x = (TacetGame.ScreenW - total) / 2 + i * (ColumnW + gap);
            float rise = UiMotion.Between(time, 0.35f + i * 0.07f, 0.45f);
            float bob = (float)Math.Sin(time * 1.6f + i) * 2f;
            return new Rectangle(x, (int)(LineupTop + (1f - rise) * 30f + bob), ColumnW, ColumnH);
        }

        //======================================================================================
        //The World (THE LENS) : the band, each in a soft light
        //======================================================================================

        public override bool HasWorld
        {
            get { return true; }
        }

        public override bool ShowsGrain
        {
            get { return false; }
        }

        public override void DrawWorld(SpriteBatch sb)
        {
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageDeep);
            Gfx.DrawGlowBox(sb, new Rectangle(240, 330, 800, 300), Palette.Paper * 0.06f);

            List<Musician> band = Game.CurrentRun.Order;
            for (int i = 0; i < band.Count; i++)
            {
                float a = UiMotion.Between(time, 0.35f + i * 0.07f, 0.45f);
                if (a <= 0f) continue;
                BandColumn.Picture(sb, ColumnRect(i, band.Count), UiArt.BustOf(band[i]), BandColumn.SpotOf(band[i]), 0.35f, a);
            }
            Gfx.Rect(sb, 300, LineupTop + ColumnH + 8, 680, 1, Palette.LineGrey * 0.6f);
        }

        //Lights : a soft light on the band and one on the name
        public override void DrawLights(SpriteBatch sb)
        {
            float up = UiMotion.Between(time, 0.1f, 0.6f);
            Gfx.DrawGlow(sb, 640f, LineupTop + ColumnH / 2f, 420f, Color.White * (0.42f * up));
            Gfx.DrawGlow(sb, 640f, field.Center.Y, 380f, Color.White * (0.35f * up));
        }

        //======================================================================================
        //The Words (crisp on top)
        //======================================================================================

        public override void Draw(SpriteBatch sb)
        {
            float cx = TacetGame.ScreenW / 2f;
            float a = UiMotion.Between(time, 0.1f, BattleRules.UiEnterTime);
            float b = UiMotion.Between(time, 0.1f + BattleRules.UiEnterStagger, BattleRules.UiEnterTime);
            float c = UiMotion.Between(time, 0.1f + BattleRules.UiEnterStagger * 2f, BattleRules.UiEnterTime);
            float slide = BattleRules.UiEnterSlide;

            //Heading
            Gfx.TextSpacedCentered(sb, Game.Font, "BEFORE THE FIRST STAGE", cx, 96 - (1f - a) * slide, Palette.LineGrey * a, TextSize.Tiny, 5f);
            Gfx.TextCentered(sb, Game.BigFont, "Name your ensemble", cx, 142 - (1f - b) * slide, Palette.Paper * b, TextSize.Title);
            Gfx.TextCentered(sb, Game.StoryFont, "This is what the world will call you.", cx, 184 - (1f - c) * slide, Palette.PaperDim * c, TextSize.Story);

            //Input Line : no box, just the name sitting on a rule, like a signature. A new letter drops into place.
            string shown = name.Length == 0 ? DefaultName : name;
            Color shownColor = name.Length == 0 ? Palette.LineGrey : Palette.Highlight;
            float drop = name.Length > 0 ? (1f - UiMotion.EaseOut(Math.Min(1f, typed / 0.12f))) * -6f : 0f;
            Gfx.TextCentered(sb, Game.BigFont, shown, field.Center.X, field.Center.Y + drop, shownColor * c, TextSize.Hero);
            Ornament.OrbitRule(sb, field.Center.X, field.Bottom + 6, field.Width / 2f, Palette.PaperDim * c);

            //Caret : blinks so it is obvious the field is waiting for typing
            if ((int)(time * 2f) % 2 == 0)
            {
                float width = Gfx.TextWidth(Game.BigFont, shown, TextSize.Hero);
                Gfx.Rect(sb, field.Center.X + width / 2f + 8, field.Center.Y - 22, 2, 44, Palette.Highlight * c);
            }

            //Names Under The Band
            RunState run = Game.CurrentRun;
            for (int i = 0; i < run.Order.Count; i++)
            {
                float k = UiMotion.Between(time, 0.35f + i * 0.07f, 0.45f);
                if (k <= 0f) continue;
                Rectangle r = ColumnRect(i, run.Order.Count);
                Gfx.TextSpacedCentered(sb, Game.Font, run.Order[i].NameTag, r.Center.X, LineupTop + ColumnH + 18, Palette.PaperDim * k, TextSize.Tiny, 2f);
            }

            float low = UiMotion.Between(time, 0.5f, BattleRules.UiEnterTime);
            Ui.Command(sb, confirmButton, "TAKE THE STAGE", "ENTER", confirmMotion, low, false);

            //Hint
            Gfx.TextSpacedCentered(sb, Game.Font, "TYPE A NAME   /   BACKSPACE TO DELETE   /   ENTER TO CONFIRM", cx, 700, Palette.LineGrey * low, TextSize.Tiny, 2f);
        }
    }
}
