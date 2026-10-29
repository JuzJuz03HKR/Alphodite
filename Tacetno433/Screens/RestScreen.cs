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
    //   REHEARSE   one musician's weapon goes up a tier for the rest of the run (WEAPON TIERS, 29 Oct : it was a point of power)
    //   DEEP REST  all stamina back, costs shards
    //Beta1 screens round 1 (29 Oct, docs/SCREENS_LEVEL_UP.md 14) : A QUIET ROOM, the softest light of the game. One lamp
    //glows low in the middle of the room and breathes slowly, nothing flickers, the corners are only a little dark. The
    //three choices rise in one after another, and pointing at one lifts it into the lamp's light.
    public class RestScreen : GameScreen
    {
        private enum Phase { Choose, Pick, Done }

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
        private Rectangle pickPanel = new Rectangle(0, 400, TacetGame.ScreenW, 320);
        private Rectangle moveButton = new Rectangle(930, 636, 310, 54);
        private Rectangle cancelButton = new Rectangle(40, 660, 170, 44);

        //Rest State
        private Phase phase;

        //Uses Escape : while picking who rehearses, ESC goes back to the three choices
        //instead of opening the pause menu
        public override bool UsesEscape
        {
            get { return phase == Phase.Pick; }
        }

        //Wipe Word : the page's name in the wipe on the way here (WIPE WORD, the user's pick)
        public override string WipeWord
        {
            get { return "A QUIET ROOM"; }
        }

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

        //Motion (Beta1 round 1) : the cards and buttons point like the menus (Core/UiMotion.cs)
        private UiMotion[] optionMotion = { new UiMotion(), new UiMotion(), new UiMotion() };
        private UiMotion moveMotion = new UiMotion();
        private UiMotion cancelMotion = new UiMotion();

        //LAMP : the room's one light, low in the middle, breathing this many times a second
        private static readonly Vector2 Lamp = new Vector2(640f, 330f);
        private const float LampBreath = 0.22f;

        public override void Load()
        {
            RunState run = Game.CurrentRun;
            stageLine = "REST STOP   /   " + run.StageLabel;
            pickHint = "EACH WEAPON GOES UP TO TIER " + MusicianList.TierNames[Math.Min(BattleRules.RehearseMax, MusicianList.TierNames.Length - 1)];

            optionTexts[Breathe] = Gfx.WrapText(Game.StoryFont, "Sit down, close your eyes. The band gets back "
                                   + (int)(BattleRules.BreatheRecover * 100) + " percent of its " + BattleRules.LifeWordLower + ".", CardW - 40, TextSize.StorySmall);
            optionTexts[Rehearse] = Gfx.WrapText(Game.StoryFont, "Practise until the weapon sings. "
                                   + "One musician's weapon goes up a tier for the rest of the run.", CardW - 40, TextSize.StorySmall);
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
            for (int i = 0; i < 3; i++) optionMotion[i].Update(dt, phase == Phase.Choose && Input.MouseOver(OptionRect(i)) && OptionAllowed(i));
            moveMotion.Update(dt, phase != Phase.Pick && Input.MouseOver(moveButton));
            cancelMotion.Update(dt, phase == Phase.Pick && Input.MouseOver(cancelButton));

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
                        Done("REHEARSED", m.Name + " plays it again and again until it is perfect. " + m.WeaponNow + ", power " + run.PowerOf(m) + ".");
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
            if (phase == Phase.Pick) DrawPick(sb, run);
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

        //Pick : who rehearses. Musicians who cannot improve further are dimmed.
        private void DrawPick(SpriteBatch sb, RunState run)
        {
            float slide = UiMotion.EaseOut(Math.Min(1f, phaseTimer * 4f));
            Rectangle panel = pickPanel;
            panel.Y += (int)((1f - slide) * 60f);

            Gfx.Rect(sb, panel, Palette.Void * (0.95f * slide));
            Gfx.Rect(sb, panel.X, panel.Y, panel.Width, 1, Palette.PaperDim * slide);
            Gfx.TextCentered(sb, Game.BigFont, "Who rehearses?", 640, panel.Y + 30, Palette.Paper * slide, TextSize.Subtitle);
            Gfx.TextSpacedCentered(sb, Game.Font, pickHint, 640, panel.Y + 56, Palette.PaperDim * slide, TextSize.Tiny, 3f);

            for (int i = 0; i < run.Roster.Count; i++)
            {
                Rectangle card = CardRect(i);
                float k = UiMotion.Between(phaseTimer, i * 0.03f, 0.25f);
                card.Y += (int)((1f - k) * 50f);
                Musician m = run.Roster[i];
                bool can = run.CanRehearse(m);
                MusicianArt.Card(sb, card, m, run, i == hoverCard && can, !can, can ? "+1 TIER" : "TOP TIER");
            }

            Ui.Command(sb, cancelButton, "BACK", "ESC", cancelMotion, slide, false);
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
