using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //MotifRewardScreen : after a win, pick one motif out of three, or take shards instead.
    //The motif stays with the band for the rest of the run.
    public class MotifRewardScreen : GameScreen
    {
        //Reward Layout
        private const int CardW = 240;
        private const int CardH = 366;
        private const int CardGap = 40;
        private const int CardsY = 166;
        private Rectangle skipButton = new Rectangle(470, 624, 340, 50);

        //Reward State
        private bool thenRecruit;          // someone also joined, show them after this page
        private Motif[] offer = new Motif[0];
        private Rectangle[] cards = new Rectangle[0];
        private int selected;
        private int picked = -1;           // -1 until a choice is made, -2 for skipping
        private float leaveTimer;
        private bool gone;                 // the next page has been asked for
        private float time;
        private string skipLabel = "";
        private Vector2 lastMouse;

        public MotifRewardScreen(bool recruitAfter)
        {
            thenRecruit = recruitAfter;
        }

        public override void Load()
        {
            RunState run = Game.CurrentRun;

            //Offer : bosses and elites skip the common motifs
            int minRarity = 1;
            if (run.Battle != null && run.Battle.Enemy.Kind == EnemyKind.Elite) minRarity = 2;
            if (run.Battle != null && run.Battle.Enemy.Kind == EnemyKind.Boss) minRarity = 3;
            offer = MotifList.Roll(run.Motifs, BattleRules.MotifChoices, minRarity, run.Floor, run.Rng);

            cards = new Rectangle[offer.Length];
            int totalW = offer.Length * CardW + (offer.Length - 1) * CardGap;
            int startX = (TacetGame.ScreenW - totalW) / 2;
            for (int i = 0; i < offer.Length; i++)
                cards[i] = new Rectangle(startX + i * (CardW + CardGap), CardsY, CardW, CardH);

            skipLabel = "TAKE " + BattleRules.SkipMotifShards + " SHARDS";
            lastMouse = Input.MousePos;
            SoundBank.PlayMusic(Music.Victory);
        }

        public override void Update(float dt)
        {
            time += dt;

            //Leaving : the chosen card has a moment on its own before the page changes
            if (picked != -1)
            {
                leaveTimer += dt;
                if (leaveTimer > 0.8f && !gone)
                {
                    gone = true;
                    GoOn();
                }
                return;
            }

            int before = selected;
            if (Input.KeyPressed(Keys.Right) || Input.KeyPressed(Keys.D)) selected = Math.Min(offer.Length - 1, selected + 1);
            if (Input.KeyPressed(Keys.Left) || Input.KeyPressed(Keys.A)) selected = Math.Max(0, selected - 1);

            if (Input.MousePos != lastMouse)
            {
                lastMouse = Input.MousePos;
                for (int i = 0; i < cards.Length; i++)
                    if (Input.MouseOver(cards[i])) selected = i;
            }
            if (selected != before) SoundBank.Play(Sfx.UiMove);

            //Pick
            bool confirm = offer.Length > 0 && (Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Space));
            for (int i = 0; i < cards.Length; i++)
                if (Input.ClickedOn(cards[i])) { selected = i; confirm = true; }

            if (confirm)
            {
                picked = selected;
                Game.CurrentRun.AddMotif(offer[picked]);
                SoundBank.Play(Sfx.MotifGet);
            }

            //Skip
            bool skip = Input.ClickedOn(skipButton);
            if (offer.Length == 0 && Input.KeyPressed(Keys.Enter)) skip = true;
            if (skip && picked == -1)
            {
                picked = -2;
                Game.CurrentRun.AddShards(BattleRules.SkipMotifShards);
                SoundBank.Play(Sfx.UiBack);
            }
        }

        //Go On : on to whoever joined, or straight to the next stage
        private void GoOn()
        {
            if (thenRecruit)
                Game.Screens.Change(new RecruitScreen(true));
            else
                RunFlow.NextStage(Game);
        }

        public override void Draw(SpriteBatch sb)
        {
            RunState run = Game.CurrentRun;
            float cx = TacetGame.ScreenW / 2f;

            SceneBackdrop.Draw(sb, time);

            Gfx.TextSpacedCentered(sb, Game.Font, "SPOILS OF THE DUEL", cx, 76, Palette.LineGrey, TextSize.Tiny, 5f);
            Gfx.TextCentered(sb, Game.BigFont, "Choose a motif", cx, 122, Palette.Paper, TextSize.Title);

            if (offer.Length == 0)
                Gfx.TextCentered(sb, Game.StoryFont, "The band already carries every motif there is.", cx, 340, Palette.PaperDim, TextSize.Story);

            for (int i = 0; i < offer.Length; i++)
            {
                float alpha = 1f;
                bool hover = i == selected && picked == -1;
                Rectangle r = cards[i];

                if (picked >= 0)
                {
                    if (i == picked) { hover = true; r.Y -= (int)(leaveTimer * 30f); }
                    else alpha = Math.Max(0f, 1f - leaveTimer * 3f);
                }
                if (picked == -2) alpha = Math.Max(0f, 1f - leaveTimer * 3f);

                MotifArt.Card(sb, r, offer[i], hover, alpha, time);
                Gfx.TextCentered(sb, Game.Font, RouteNodeInfo.PanelNumbers[i], r.Center.X, r.Bottom + 18, Palette.LineGrey * alpha, TextSize.Label);
            }

            Gfx.TextCentered(sb, Game.StoryFont, "It stays with the band until the run ends.", cx, 594, Palette.PaperDim, TextSize.StorySmall);
            Ui.Button(sb, skipButton, skipLabel, "", false, picked == -1, 1f);

            RunHud.DrawTop(sb, run, "REWARD");
            RunHud.DrawTips(sb, run);
        }
    }
}
