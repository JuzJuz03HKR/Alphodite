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
    //   REHEARSE   one move of the band gets a star, on the rehearsal page (REHEARSAL, 31 Oct, Screens/RehearsalScreen.cs).
    //              Until then one musician's weapon went up a tier here (WEAPON TIERS, 29 Oct)
    //   DEEP REST  all stamina back, costs shards
    //Beta1 screens round 1 (29 Oct, docs/SCREENS_LEVEL_UP.md 14) : A QUIET ROOM, the softest light of the game. One lamp
    //glows low in the middle of the room and breathes slowly, nothing flickers, the corners are only a little dark. The
    //three choices rise in one after another, and pointing at one lifts it into the lamp's light.
    public class RestScreen : GameScreen
    {
        private enum Phase { Choose, Done }

        //Rest Options
        private const int Breathe = 0;
        private const int Rehearse = 1;
        private const int DeepRest = 2;
        private static string[] optionNames = { BattleRules.TurnBattleOn ? "RECOVER" : "BREATHE", "REHEARSE", "DEEP REST" };   // step 15 : BREATH is the fight's skill points, rest gives LIFE
        private static string[] optionNumerals = { "I", "II", "III" };

        //Rest Layout
        private const int CardW = 300;
        private const int CardH = 176;
        private const int CardsY = 440;
        private Rectangle moveButton = new Rectangle(930, 636, 310, 54);

        //Rest State
        private Phase phase;

        //Wipe Word : the page's name in the wipe on the way here (WIPE WORD, the user's pick)
        public override string WipeWord
        {
            get { return "A QUIET ROOM"; }
        }

        private int hoverOption = -1;
        private string[] optionTexts = new string[3];
        private string[] optionPrices = new string[3];
        private string resultTitle = "";
        private string resultLine = "";
        private string stageLine = "";
        private float phaseTimer;
        private float time;

        //Motion (Beta1 round 1) : the cards and buttons point like the menus (Core/UiMotion.cs)
        private UiMotion[] optionMotion = { new UiMotion(), new UiMotion(), new UiMotion() };
        private UiMotion moveMotion = new UiMotion();

        //LAMP : the room's one light, low in the middle, breathing this many times a second
        private static readonly Vector2 Lamp = new Vector2(640f, 330f);
        private const float LampBreath = 0.22f;

        public override void Load()
        {
            RunState run = Game.CurrentRun;
            stageLine = "REST   /   " + run.StageLabel;

            optionTexts[Breathe] = Gfx.WrapText(Game.StoryFont, "Sit down, close your eyes. The band gets back "
                                   + (int)(BattleRules.BreatheRecover * 100) + " percent of its " + BattleRules.LifeWordLower + ".", CardW - 40, TextSize.StorySmall);
            optionTexts[Rehearse] = Gfx.WrapText(Game.StoryFont, "Practise until it sings. "
                                   + "One move of the band gets a star, as after a fight won.", CardW - 40, TextSize.StorySmall);
            optionTexts[DeepRest] = Gfx.WrapText(Game.StoryFont, "Pay for a real bed. The band gets back all of its " + BattleRules.LifeWordLower + ".",
                                   CardW - 40, TextSize.StorySmall);
            optionPrices[Breathe] = "FREE";
            optionPrices[Rehearse] = "FREE";
            optionPrices[DeepRest] = BattleRules.DeepRestPrice + " SHARDS";

            phase = Phase.Choose;
            SoundBank.PlayMusic(Music.Rest);

            //The Lens : the softest light, bloom a little wider, the corners only a little dark
            Lens.Ambient = 0.42f;
            Lens.SpotPower = 0f;
            Lens.Vignette = 0.42f;
            Lens.BloomThreshold = 0.7f;
            Lens.BloomStrength = 0.4f;
        }

        private Rectangle OptionRect(int i)
        {
            int total = 3 * CardW + 2 * 30;
            return new Rectangle((TacetGame.ScreenW - total) / 2 + i * (CardW + 30), CardsY, CardW, CardH);
        }

        private bool OptionAllowed(int i)
        {
            RunState run = Game.CurrentRun;
            if (i == DeepRest) return run.Shards >= BattleRules.DeepRestPrice && run.Stamina < run.MaxStamina;
            if (i == Breathe) return run.Stamina < run.MaxStamina;
            return run.AnyStarLeft;                                                  // REHEARSE : a move can still get a star
        }

        public override void Update(float dt)
        {
            time += dt;
            phaseTimer += dt;
            RunState run = Game.CurrentRun;
            for (int i = 0; i < 3; i++) optionMotion[i].Update(dt, phase == Phase.Choose && Input.MouseOver(OptionRect(i)) && OptionAllowed(i));
            moveMotion.Update(dt, Input.MouseOver(moveButton));

            if (phase == Phase.Choose)
            {
                hoverOption = -1;
                for (int i = 0; i < 3; i++)
                    if (Input.MouseOver(OptionRect(i))) hoverOption = i;

                if (Input.MouseClicked() && hoverOption >= 0) Choose(hoverOption);

                //Skip : leaving without resting is allowed
                if (Input.ClickedOn(moveButton))
                {
                    SoundBank.Play(Sfx.UiBack);
                    RunFlow.NextStage(Game);
                }
            }
            else
            {
                if (phaseTimer > 0.4f && (Input.KeyPressed(Keys.Enter) || Input.ClickedOn(moveButton)))
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
            optionMotion[option].Press();

            if (option == Breathe)
            {
                int before = run.Stamina;
                run.ChangeStamina((int)(run.MaxStamina * BattleRules.BreatheRecover));
                SoundBank.Play(Sfx.RestRecover);
                Done("RESTED", "The band breathes slowly together. " + BattleRules.LifeWord + " " + NumberText.Signed(run.Stamina - before) + ".");
            }
            else if (option == DeepRest)
            {
                run.SpendShards(BattleRules.DeepRestPrice);
                run.RestoreAllStamina();
                SoundBank.Play(Sfx.RestRecover);
                Done("FULLY RESTED", "A real bed, a long night. The band wakes up with all of its " + BattleRules.LifeWordLower + ".");
            }
            else
            {
                SoundBank.Play(Sfx.UiConfirm);                                           // REHEARSE : the rehearsal page, then the map
                Game.Screens.Change(new RehearsalScreen(RehearsalScreen.From.Rest));
            }
        }

        private void Done(string title, string text)
        {
            resultTitle = title;
            resultLine = Gfx.WrapText(Game.StoryFont, text, 640, TextSize.Story);
            phase = Phase.Done;
            phaseTimer = 0f;
        }

        //======================================================================================
        //The World (THE LENS) : a dark, quiet room round one lamp
        //======================================================================================

        public override bool HasWorld
        {
            get { return true; }
        }

        public override bool ShowsGrain
        {
            get { return false; }
        }

        //Breath : the lamp's light, slowly up and down
        private float Breath()
        {
            return 0.9f + 0.1f * (float)Math.Sin(time * MathHelper.TwoPi * LampBreath);
        }

        //World : the rest room painting, or an empty slot where it goes.
        //ARTWORK : Content/Art/Places/rest.png (the band resting is part of the picture), see ArtBank.
        public override void DrawWorld(SpriteBatch sb)
        {
            float e = UiMotion.Between(time, 0f, 0.8f);
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.Void);
            Gfx.DrawGlowBox(sb, new Rectangle(140, 200, 1000, 560), Palette.Paper * 0.05f);
            ArtBank.DrawOrSlot(sb, ArtBank.Rest, new Rectangle(160, 186, 960, 236), Palette.Paper, e);
        }

        //Lights : LAMP, one wide soft light low in the middle that breathes, and the pointed card lifted into it
        public override void DrawLights(SpriteBatch sb)
        {
            float up = UiMotion.EaseInOut(MathHelper.Clamp(time / 1.2f, 0f, 1f));
            float b = Breath();
            Gfx.DrawGlow(sb, Lamp.X, Lamp.Y, 640f, Color.White * (0.6f * up * b));
            Gfx.DrawGlow(sb, Lamp.X, Lamp.Y + 40f, 300f, Color.White * (0.42f * up * b));
            for (int i = 0; i < 3; i++)
            {
                float e = optionMotion[i].Shown;
                if (e <= 0f) continue;
                Rectangle r = OptionRect(i);
                Gfx.DrawGlow(sb, r.Center.X, r.Center.Y, 260f, Color.White * (0.3f * e));
            }
        }

        //Glow : the lamp's own warm halo, never sharp
        public override void DrawGlow(SpriteBatch sb)
        {
            float up = UiMotion.EaseInOut(MathHelper.Clamp(time / 1.2f, 0f, 1f));
            Gfx.DrawGlow(sb, Lamp.X, Lamp.Y + 30f, 240f, Palette.Paper * (0.32f * up * Breath()));
        }

        //======================================================================================
        //The Words (crisp on top)
        //======================================================================================

        public override void Draw(SpriteBatch sb)
        {
            RunState run = Game.CurrentRun;
            float cx = TacetGame.ScreenW / 2f;

            //Heading : one piece after another
            float a = UiMotion.Between(time, 0.15f, BattleRules.UiEnterTime);
            float b = UiMotion.Between(time, 0.15f + BattleRules.UiEnterStagger, BattleRules.UiEnterTime);
            float c = UiMotion.Between(time, 0.15f + BattleRules.UiEnterStagger * 2f, BattleRules.UiEnterTime);
            float slide = BattleRules.UiEnterSlide;
            Gfx.TextSpacedCentered(sb, Game.Font, stageLine, cx, 76 - (1f - a) * slide, Palette.LineGrey * a, TextSize.Tiny, 4f);
            Gfx.TextCentered(sb, Game.BigFont, "A quiet room", cx, 122 - (1f - b) * slide, Palette.Paper * b, TextSize.Title);
            Gfx.TextCentered(sb, Game.StoryFont, "There is time for one thing before moving on.", cx, 158 - (1f - c) * slide, Palette.PaperDim * c, TextSize.Story);

            if (phase == Phase.Choose) DrawOptions(sb);
            if (phase == Phase.Done) DrawDone(sb);

            RunHud.DrawTop(sb, run, "REST");
            RunHud.DrawTips(sb, run);
        }

        //Options : three tall cards, one choice only. They rise in one after another. Pointed at, one lifts into the
        //lamp's light, its frame brightens and an ink bar shoots out under its name (like the menus).
        private void DrawOptions(SpriteBatch sb)
        {
            for (int i = 0; i < 3; i++)
            {
                float enter = UiMotion.Between(time, 0.4f + i * BattleRules.UiEnterStagger * 1.5f, BattleRules.UiEnterTime);
                if (enter <= 0f) continue;
                Rectangle r = OptionRect(i);
                bool allowed = OptionAllowed(i);
                float a = (allowed ? 1f : 0.4f) * enter;
                float e = allowed ? optionMotion[i].Shown : 0f;
                r.Y += (int)((1f - enter) * 40f - 8f * e);

                Gfx.DrawGlowBox(sb, new Rectangle(r.X - 10, r.Bottom - 6, r.Width + 20, 30), Color.Black * (0.5f * a));
                Gfx.Rect(sb, r, Color.Lerp(Palette.Panel, Palette.Stage, e) * a);
                Ornament.DoubleFrame(sb, r, Color.Lerp(Palette.LineGrey, Palette.Highlight, e) * a);

                //Name : on an ink bar when pointed at
                float nameW = Gfx.SpacedWidth(Game.BigFont, optionNames[i], TextSize.Small, 2f);
                if (e > 0f)
                {
                    Ui.InkBar(sb, r.X + 12 + BattleRules.UiShadow, r.Y + 10 + BattleRules.UiShadow, (nameW + 70f) * e, 32f, 10f, Palette.LineGrey * (0.5f * a), i * 9 + 4, 1f);
                    Ui.InkBar(sb, r.X + 12, r.Y + 10, (nameW + 70f) * e, 32f, 10f, Palette.Paper * a, i * 9, 1f);
                }
                Gfx.Text(sb, Game.BigFont, optionNumerals[i], r.X + 20, r.Y + 12, Color.Lerp(Palette.PaperDim, Palette.InkSoft, e) * a, TextSize.Small);
                Gfx.TextSpaced(sb, Game.BigFont, optionNames[i], r.X + 60, r.Y + 12, Color.Lerp(Palette.Highlight, Palette.Ink, e) * a, TextSize.Small, 2f);
                Gfx.Rect(sb, r.X + 20, r.Y + 50, r.Width - 40, 1, Palette.LineGrey * a);
                Gfx.Text(sb, Game.StoryFont, optionTexts[i], r.X + 20, r.Y + 62, Palette.Paper * a, TextSize.StorySmall);

                Ui.Tag(sb, optionPrices[i], r.X + 20, r.Bottom - 30, i == DeepRest, a);
                if (optionMotion[i].Flash > 0f) Gfx.DrawGlow(sb, r.Center.X, r.Center.Y, r.Width * 0.7f, Color.White * (0.5f * optionMotion[i].Flash));
            }

            float button = UiMotion.Between(time, 0.9f, BattleRules.UiEnterTime);
            Ui.Command(sb, moveButton, "MOVE ON", "", moveMotion, button, false);
        }

        //Done : what the rest did
        private void DrawDone(SpriteBatch sb)
        {
            float a = UiMotion.EaseOut(Math.Min(1f, phaseTimer * 3f));
            Rectangle box = new Rectangle(300, 450, 680, 150);
            box.Y += (int)((1f - a) * 16f);
            Gfx.Rect(sb, box, Palette.Void * (0.9f * a));
            Ornament.DoubleFrame(sb, box, Palette.PaperDim * a);
            Ui.Tag(sb, resultTitle, box.X + 24, box.Y - 9, true, a);
            Gfx.Text(sb, Game.StoryFont, resultLine, box.X + 24, box.Y + 30, Palette.Paper * a, TextSize.Story);
            Gfx.TextSpaced(sb, Game.Font, Game.CurrentRun.StaminaLabel, box.X + 24, box.Bottom - 34, Palette.PaperDim * a, TextSize.Label, 2f);

            float button = UiMotion.Between(phaseTimer, 0.3f, BattleRules.UiEnterTime);
            Ui.Command(sb, moveButton, "MOVE ON", "ENTER", moveMotion, button, false);
        }
    }
}
