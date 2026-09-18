using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //RestScreen : a quiet room. The band sits around a lamp and the player picks ONE thing:
    //   BREATHE    some stamina back, free
    //   REHEARSE   one musician gains a point of power for the rest of the run
    //   DEEP REST  all stamina back, costs shards
    public class RestScreen : GameScreen
    {
        private enum Phase { Choose, Pick, Done }

        //Rest Options
        private const int Breathe = 0;
        private const int Rehearse = 1;
        private const int DeepRest = 2;
        private static string[] optionNames = { "BREATHE", "REHEARSE", "DEEP REST" };
        private static string[] optionNumerals = { "I", "II", "III" };

        //Rest Layout
        private const int CardW = 300;
        private const int CardH = 176;
        private const int CardsY = 440;
        private Rectangle pickPanel = new Rectangle(0, 400, TacetGame.ScreenW, 320);
        private Rectangle moveButton = new Rectangle(930, 636, 310, 54);
        private Rectangle cancelButton = new Rectangle(40, 660, 170, 44);
        private Vector2 lamp = new Vector2(640, 310);

        //Rest State
        private Phase phase;
        private int hoverOption = -1;
        private int hoverCard = -1;
        private string[] optionTexts = new string[3];
        private string[] optionPrices = new string[3];
        private string resultTitle = "";
        private string resultLine = "";
        private string stageLine = "";
        private string pickHint = "";
        private float phaseTimer;
        private float time;

        public override void Load()
        {
            RunState run = Game.CurrentRun;
            stageLine = "REST STOP   /   " + run.StageLabel;
            pickHint = "EACH MUSICIAN CAN TRAIN " + BattleRules.RehearseMax + " TIMES";

            optionTexts[Breathe] = Gfx.WrapText(Game.StoryFont, "Sit down, close your eyes. The band gets back "
                                   + (int)(BattleRules.BreatheRecover * 100) + " percent of its stamina.", CardW - 40, TextSize.StorySmall);
            optionTexts[Rehearse] = Gfx.WrapText(Game.StoryFont, "Practise one part until it is perfect. "
                                   + "That musician gains 1 power for the rest of the run.", CardW - 40, TextSize.StorySmall);
            optionTexts[DeepRest] = Gfx.WrapText(Game.StoryFont, "Pay for a real bed. The band gets back all of its stamina.",
                                   CardW - 40, TextSize.StorySmall);
            optionPrices[Breathe] = "FREE";
            optionPrices[Rehearse] = "FREE";
            optionPrices[DeepRest] = BattleRules.DeepRestPrice + " SHARDS";

            phase = Phase.Choose;
            SoundBank.PlayMusic(Music.Rest);
        }

        private Rectangle OptionRect(int i)
        {
            int total = 3 * CardW + 2 * 30;
            return new Rectangle((TacetGame.ScreenW - total) / 2 + i * (CardW + 30), CardsY, CardW, CardH);
        }

        private Rectangle CardRect(int i)
        {
            int count = Game.CurrentRun.Roster.Count;
            int gap = 10;
            int w = Math.Min(110, (TacetGame.ScreenW - 40 - (count - 1) * gap) / Math.Max(1, count));
            int total = count * w + (count - 1) * gap;
            return new Rectangle((TacetGame.ScreenW - total) / 2 + i * (w + gap), pickPanel.Y + 80, w, 180);
        }

        private bool OptionAllowed(int i)
        {
            RunState run = Game.CurrentRun;
            if (i == DeepRest) return run.Shards >= BattleRules.DeepRestPrice && run.Stamina < run.MaxStamina;
            if (i == Breathe) return run.Stamina < run.MaxStamina;
            return true;
        }

        public override void Update(float dt)
        {
            time += dt;
            phaseTimer += dt;
            RunState run = Game.CurrentRun;

            if (phase == Phase.Choose)
            {
                hoverOption = -1;
                for (int i = 0; i < 3; i++)
                    if (Input.MouseOver(OptionRect(i))) hoverOption = i;

                if (Input.MouseClicked() && hoverOption >= 0) Choose(hoverOption);

                //Skip : leaving without resting is allowed
                if (Input.KeyPressed(Keys.Escape) || Input.ClickedOn(moveButton))
                {
                    SoundBank.Play(Sfx.UiBack);
                    RunFlow.NextStage(Game);
                }
            }
            else if (phase == Phase.Pick)
            {
                hoverCard = -1;
                for (int i = 0; i < run.Roster.Count; i++)
                    if (Input.MouseOver(CardRect(i))) hoverCard = i;

                if (Input.MouseClicked() && hoverCard >= 0)
                {
                    Musician m = run.Roster[hoverCard];
                    if (!run.CanRehearse(m))
                    {
                        SoundBank.Play(Sfx.UiDenied);
                    }
                    else
                    {
                        run.Rehearse(m);
                        SoundBank.Play(Sfx.Rehearse);
                        Done("REHEARSED", m.Name + " plays the part again and again until it is perfect. Power is now " + m.PowerLabel + ".");
                    }
                }

                if (Input.KeyPressed(Keys.Escape) || Input.MouseRightClicked() || Input.ClickedOn(cancelButton))
                {
                    phase = Phase.Choose;
                    phaseTimer = 0f;
                    SoundBank.Play(Sfx.UiBack);
                }
            }
            else
            {
                if (phaseTimer > 0.4f && (Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Escape) || Input.ClickedOn(moveButton)))
                {
                    SoundBank.Play(Sfx.UiConfirm);
                    RunFlow.NextStage(Game);
                }
            }
        }

        //Choose : one option per rest stop
        private void Choose(int option)
        {
            RunState run = Game.CurrentRun;

            if (!OptionAllowed(option))
            {
                SoundBank.Play(Sfx.UiDenied);
                return;
            }

            if (option == Breathe)
            {
                int before = run.Stamina;
                run.ChangeStamina((int)(run.MaxStamina * BattleRules.BreatheRecover));
                SoundBank.Play(Sfx.RestRecover);
                Done("RESTED", "The band breathes slowly together. Stamina " + NumberText.Signed(run.Stamina - before) + ".");
            }
            else if (option == DeepRest)
            {
                run.SpendShards(BattleRules.DeepRestPrice);
                run.RestoreAllStamina();
                SoundBank.Play(Sfx.RestRecover);
                Done("FULLY RESTED", "A real bed, a long night. The band wakes up with all of its stamina.");
            }
            else
            {
                phase = Phase.Pick;
                phaseTimer = 0f;
                SoundBank.Play(Sfx.UiConfirm);
            }
        }

        private void Done(string title, string text)
        {
            resultTitle = title;
            resultLine = Gfx.WrapText(Game.StoryFont, text, 640, TextSize.Story);
            phase = Phase.Done;
            phaseTimer = 0f;
        }

        public override void Draw(SpriteBatch sb)
        {
            RunState run = Game.CurrentRun;
            float cx = TacetGame.ScreenW / 2f;

            DrawRoom(sb, run);

            //Heading
            Gfx.TextSpacedCentered(sb, Game.Font, stageLine, cx, 76, Palette.LineGrey, TextSize.Tiny, 4f);
            Gfx.TextCentered(sb, Game.BigFont, "A quiet room", cx, 122, Palette.Paper, TextSize.Title);
            Gfx.TextCentered(sb, Game.StoryFont, "There is time for one thing before moving on.", cx, 158, Palette.PaperDim, TextSize.Story);

            if (phase == Phase.Choose) DrawOptions(sb);
            if (phase == Phase.Pick) DrawPick(sb, run);
            if (phase == Phase.Done) DrawDone(sb);

            RunHud.DrawTop(sb, run, "REST");
            RunHud.DrawTips(sb, run);
        }

        //Room : PLACEHOLDER for the rest painting. A lamp, and the band sitting around it.
        private void DrawRoom(SpriteBatch sb, RunState run)
        {
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.Void);

            float flicker = 1f + (float)Math.Sin(time * 7f) * 0.03f + (float)Math.Sin(time * 11.3f) * 0.02f;
            Gfx.DrawGlow(sb, lamp.X, lamp.Y + 40, 520f * flicker, Palette.Paper * 0.13f);
            Gfx.DrawGlow(sb, lamp.X, lamp.Y, 160f * flicker, Palette.Paper * 0.20f);

            //Floor Rings : the light falling on the floor
            Gfx.DrawGlowBox(sb, new Rectangle((int)lamp.X - 360, (int)lamp.Y + 50, 720, 120), Palette.Paper * 0.10f);

            //Band : capsules on a half ring behind the lamp, breathing slowly
            int count = run.Roster.Count;
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : (float)i / (count - 1);
                float angle = MathHelper.Pi + t * MathHelper.Pi;
                float x = lamp.X + (float)Math.Cos(angle) * 330f;
                float y = lamp.Y + 70 + (float)Math.Sin(angle) * 60f;
                float breathe = (float)Math.Sin(time * 1.5f + i) * 2f;
                float near = 1f - Math.Abs(t - 0.5f);
                Rectangle cap = new Rectangle((int)x - 18, (int)(y - 80 + breathe), 36, 92);
                MusicianArt.Capsule(sb, cap, run.Roster[i], Palette.Paper * (0.35f + 0.4f * near), Palette.Void, 0f);
                if (run.Roster[i].Rehearsed > 0)
                    Ui.Pips(sb, x - (run.Roster[i].Rehearsed - 1) * 5, cap.Y - 10, run.Roster[i].Rehearsed, run.Roster[i].Rehearsed, 2, 10, 1f);
            }

            //Lamp : a stand and a shade
            Gfx.Rect(sb, lamp.X - 1, lamp.Y + 10, 3, 110, Palette.PaperDim);
            Gfx.Rect(sb, lamp.X - 24, lamp.Y + 118, 50, 4, Palette.PaperDim);
            Gfx.Line(sb, lamp.X - 26, lamp.Y + 6, lamp.X - 14, lamp.Y - 22, Palette.Paper, 3f);
            Gfx.Line(sb, lamp.X + 26, lamp.Y + 6, lamp.X + 14, lamp.Y - 22, Palette.Paper, 3f);
            Gfx.Rect(sb, lamp.X - 26, lamp.Y + 5, 53, 3, Palette.Paper);
            Gfx.Circle(sb, lamp.X, lamp.Y + 12, 6 * flicker, Palette.Highlight);
            Gfx.TextSpacedCentered(sb, Game.Font, "ART  /  REST ROOM", lamp.X, lamp.Y - 90, Palette.LineGrey, TextSize.Tiny, 2f);

            Ornament.Vignette(sb, 80, 0.7f);
        }

        //Options : three tall cards, one choice only
        private void DrawOptions(SpriteBatch sb)
        {
            for (int i = 0; i < 3; i++)
            {
                Rectangle r = OptionRect(i);
                bool allowed = OptionAllowed(i);
                bool over = i == hoverOption && allowed;
                float a = allowed ? 1f : 0.4f;
                if (over) r.Y -= 6;

                Gfx.Rect(sb, new Rectangle(r.X + 5, r.Y + 6, r.Width, r.Height), Color.Black * 0.5f);
                Gfx.Rect(sb, r, (over ? Palette.Stage : Palette.Panel) * a);
                Ornament.DoubleFrame(sb, r, (over ? Palette.Highlight : Palette.LineGrey) * a);

                Gfx.Text(sb, Game.BigFont, optionNumerals[i], r.X + 20, r.Y + 12, Palette.PaperDim * a, TextSize.Small);
                Gfx.TextSpaced(sb, Game.BigFont, optionNames[i], r.X + 60, r.Y + 12, Palette.Highlight * a, TextSize.Small, 2f);
                Gfx.Rect(sb, r.X + 20, r.Y + 50, r.Width - 40, 1, Palette.LineGrey * a);
                Gfx.Text(sb, Game.StoryFont, optionTexts[i], r.X + 20, r.Y + 62, Palette.Paper * a, TextSize.StorySmall);

                Ui.Tag(sb, optionPrices[i], r.X + 20, r.Bottom - 30, i == DeepRest, a);
            }

            Ui.Button(sb, moveButton, "MOVE ON", "ESC", false);
        }

        //Pick : who rehearses. Musicians who cannot improve further are dimmed.
        private void DrawPick(SpriteBatch sb, RunState run)
        {
            float slide = Math.Min(1f, phaseTimer * 5f);
            Rectangle panel = pickPanel;
            panel.Y += (int)((1f - slide) * 60f);

            Gfx.Rect(sb, panel, Palette.Void * 0.95f);
            Gfx.Rect(sb, panel.X, panel.Y, panel.Width, 1, Palette.PaperDim);
            Gfx.TextCentered(sb, Game.BigFont, "Who rehearses?", 640, panel.Y + 30, Palette.Paper, TextSize.Subtitle);
            Gfx.TextSpacedCentered(sb, Game.Font, pickHint, 640, panel.Y + 56, Palette.PaperDim, TextSize.Tiny, 3f);

            for (int i = 0; i < run.Roster.Count; i++)
            {
                Rectangle card = CardRect(i);
                card.Y += (int)((1f - slide) * 60f);
                Musician m = run.Roster[i];
                bool can = run.CanRehearse(m);
                MusicianArt.Card(sb, card, m, run, i == hoverCard && can, !can, can ? "+1 POWER" : "MASTERED");
            }

            Ui.Button(sb, cancelButton, "BACK", "ESC", false);
        }

        //Done : what the rest did
        private void DrawDone(SpriteBatch sb)
        {
            float a = Math.Min(1f, phaseTimer * 3f);
            Rectangle box = new Rectangle(300, 450, 680, 150);
            Gfx.Rect(sb, box, Palette.Void * (0.9f * a));
            Ornament.DoubleFrame(sb, box, Palette.PaperDim * a);
            Ui.Tag(sb, resultTitle, box.X + 24, box.Y - 9, true, a);
            Gfx.Text(sb, Game.StoryFont, resultLine, box.X + 24, box.Y + 30, Palette.Paper * a, TextSize.Story);
            Gfx.TextSpaced(sb, Game.Font, Game.CurrentRun.StaminaLabel, box.X + 24, box.Bottom - 34, Palette.PaperDim * a, TextSize.Label, 2f);

            Ui.Button(sb, moveButton, "MOVE ON", "ENTER", true, phaseTimer > 0.4f, a);
        }
    }
}
