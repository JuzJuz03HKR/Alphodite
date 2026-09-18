using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Battle;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //ResultScreen : how the fight ended.
    //
    //WIN  : rewards are handed out once when the page opens (shards, some stamina back,
    //       after an elite a chance that a musician follows you home), and the run record
    //       is updated. Then a motif choice may follow.
    //LOSS : the run is over. The curtain call page sums it up.
    public class ResultScreen : GameScreen
    {
        private Rectangle continueButton = new Rectangle(490, 630, 300, 52);
        private const int LedgerX = 390;
        private const int LedgerW = 500;

        //Result Data : worked out once in Load
        private bool won;
        private bool recruited;
        private bool offerMotif;
        private string enemyLine = "";
        private string rank = "";
        private string[] statNames = new string[5];
        private string[] statValues = new string[5];
        private int statCount;
        private string footnote = "";

        private float time;
        private float enter;

        public override void Load()
        {
            RunState run = Game.CurrentRun;
            BattleState battle = run.Battle;

            won = battle.PlayerWon;
            enemyLine = battle.EnemyTitle;
            rank = battle.Rank();

            if (won) ApplyWin(run, battle);
            else PrepareLoss(run, battle);
        }

        //Win Rewards : handed out exactly once, here
        private void ApplyWin(RunState run, BattleState battle)
        {
            int shards = battle.ShardsEarned();
            run.AddShards(shards);

            float recover = BattleRules.RecoverAfterWin;
            if (run.Has(MotifId.Encore)) recover += BattleRules.EncoreRecover;         // ENCORE
            int before = run.Stamina;
            run.ChangeStamina((int)(run.MaxStamina * recover));
            int recovered = run.Stamina - before;

            //Run Record
            run.BattlesWon++;
            run.PerfectsTotal += battle.PerfectCount;
            if (battle.BestCombo > run.BestCombo) run.BestCombo = battle.BestCombo;

            //Elite Bonus : a chance that someone from this era joins
            if (battle.Enemy.Kind == EnemyKind.Elite && run.RollPercent() < BattleRules.EliteRecruitChance)
            {
                run.JustJoined.Clear();
                recruited = run.RecruitOne() != null;
            }

            //Motif Offer : elites and bosses always, normal fights sometimes
            offerMotif = battle.Enemy.Kind != EnemyKind.Normal || run.RollPercent() < BattleRules.MotifChanceNormal;
            if (run.Motifs.Count >= MotifList.All.Length) offerMotif = false;

            //Final Boss : the run ends straight after, so there is nothing left to hand out
            bool lastBoss = battle.Enemy.Kind == EnemyKind.Boss && run.Floor >= BattleRules.FloorsPerRun;
            if (lastBoss)
            {
                offerMotif = false;
                recruited = false;
            }

            statNames[0] = "PERFECT PRESSES";  statValues[0] = NumberText.Get(battle.PerfectCount);
            statNames[1] = "BEST COMBO";       statValues[1] = NumberText.Get(battle.BestCombo);
            statNames[2] = "FINAL LINE";       statValues[2] = NumberText.Signed((int)battle.Line);
            statNames[3] = "STAMINA BACK";     statValues[3] = NumberText.Signed(recovered);
            statNames[4] = "SHARDS";           statValues[4] = NumberText.Signed(shards);
            statCount = 5;

            footnote = "";
            if (offerMotif) footnote = "A MOTIF IS LEFT BEHIND";
            if (recruited) footnote = "SOMEONE FOLLOWS YOU OUT OF THE FIGHT";
            if (battle.Enemy.Kind == EnemyKind.Boss)
                footnote = run.Floor >= BattleRules.FloorsPerRun ? "THE LAST SILENCE IS BROKEN" : "FLOOR CLEARED  -  A NEW ERA IS WAITING";

            SoundBank.Play(Sfx.Victory);
            SoundBank.PlayMusic(Music.Victory);
        }

        //Loss Summary : how far the run got
        private void PrepareLoss(RunState run, BattleState battle)
        {
            statNames[0] = "ENSEMBLE";      statValues[0] = run.BandName;
            statNames[1] = "CONDUCTOR";     statValues[1] = run.Conductor.Name;
            statNames[2] = "REACHED";       statValues[2] = run.StageLabel + "  /  FLOOR " + run.Floor;
            statNames[3] = "FINAL LINE";    statValues[3] = NumberText.Signed((int)battle.Line);
            statCount = 4;

            footnote = "THE RUN ENDS HERE";

            SoundBank.Play(Sfx.Defeat);
            SoundBank.PlayMusic(Music.Defeat);
        }

        public override void Update(float dt)
        {
            time += dt;
            enter += dt * 1.5f;
            if (enter > 1f) enter = 1f;
            if (enter < 1f) return;

            if (Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Space) || Input.ClickedOn(continueButton))
            {
                SoundBank.Play(Sfx.UiConfirm);
                Continue();
            }
        }

        //Continue : reward choice first, then whoever joined, then the next stage
        private void Continue()
        {
            if (!won)
            {
                Game.Screens.Change(new CurtainCallScreen(false));
                return;
            }

            if (offerMotif)
                Game.Screens.Change(new MotifRewardScreen(recruited));
            else if (recruited)
                Game.Screens.Change(new RecruitScreen(true));
            else
                RunFlow.NextStage(Game);
        }

        public override void Draw(SpriteBatch sb)
        {
            float e = enter * enter * (3f - 2f * enter);
            float cx = TacetGame.ScreenW / 2f;

            //Background : a win floods the stage with light, a loss lets TACET take it all
            Color ink;
            Color soft;
            if (won)
            {
                Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageLight);
                Gfx.DrawGlow(sb, cx, 200f, 560f, Color.White * 0.8f);
                Ornament.Rays(sb, cx, 170f, 120f, 700f, 36, time * 0.03f, Palette.Ink * 0.05f);
                TacetField.Draw(sb, 1180f + (1f - e) * -300f, time, 0.1f);
                ink = Palette.Ink;
                soft = Palette.InkSoft;
            }
            else
            {
                Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageLight);
                TacetField.Draw(sb, 360f - e * 300f, time, 1f);
                ink = Palette.Paper;
                soft = Palette.PaperDim;
            }

            //Title
            Gfx.TextSpacedCentered(sb, Game.Font, "BATTLE RESULT", cx, 34, soft * e, TextSize.Label, 6f);
            string title = won ? "VICTORY" : "SILENCED";
            Gfx.TextCentered(sb, Game.LogoFont, title, cx, 130 - (1f - e) * 30f, ink * e, TextSize.Logo * 0.9f);
            Gfx.TextSpacedCentered(sb, Game.Font, enemyLine, cx, 200, soft * e, TextSize.Label, 4f);
            Ornament.Divider(sb, cx, 230, 200, soft * e);

            //Rank : a letter in a diamond, beside the ledger
            if (won)
            {
                float rx = LedgerX + LedgerW + 110;
                float ry = 360;
                float spin = (1f - e) * 40f;
                Gfx.Diamond(sb, rx, ry, 66 + spin, Palette.Ink * e);
                Gfx.DiamondOutline(sb, rx, ry, 76 + spin, Palette.Ink * (0.5f * e), 1f);
                Gfx.TextCentered(sb, Game.LogoFont, rank, rx, ry - 4, Palette.Paper * e, TextSize.Banner * 0.9f);
                Gfx.TextSpacedCentered(sb, Game.Font, "RANK", rx, ry + 88, soft * e, TextSize.Tiny, 4f);
            }

            //Ledger : rows with dotted leaders, sliding in one after the other
            for (int i = 0; i < statCount; i++)
            {
                float rowE = MathHelper.Clamp(e * 2f - i * 0.25f, 0f, 1f);
                int y = 262 + i * 52;
                Gfx.Diamond(sb, LedgerX + 4, y + 12, 3, soft * rowE);
                Gfx.TextSpaced(sb, Game.Font, statNames[i], LedgerX + 18 - (1f - rowE) * 20f, y + 4, soft * rowE, TextSize.Label, 3f);
                float valueW = Gfx.TextWidth(Game.BigFont, statValues[i], TextSize.Subtitle);
                for (int d = LedgerX + 200; d < LedgerX + LedgerW - valueW - 14; d += 8)
                    Gfx.Rect(sb, d, y + 16, 2, 1, soft * (0.6f * rowE));
                Gfx.TextRight(sb, Game.BigFont, statValues[i], LedgerX + LedgerW, y - 6, ink * rowE, TextSize.Subtitle);
            }

            //Footnote : a tag on the dark page, plain spaced words between diamonds on the bright one
            if (footnote.Length > 0)
            {
                float fy = 262 + statCount * 52 + 16;
                if (won)
                {
                    float w = Gfx.SpacedWidth(Game.Font, footnote, TextSize.Label, 3f);
                    Gfx.TextSpacedCentered(sb, Game.Font, footnote, cx, fy, ink * e, TextSize.Label, 3f);
                    Gfx.Diamond(sb, cx - w / 2f - 16, fy + 8, 4, ink * e);
                    Gfx.Diamond(sb, cx + w / 2f + 16, fy + 8, 4, ink * e);
                }
                else
                {
                    Ui.Tag(sb, footnote, cx - Ui.TagWidth(footnote) / 2f, fy, true, e);
                }
            }

            //Continue Button : dark on the bright page, light on the dark one
            Ui.Button(sb, continueButton, won ? "CONTINUE" : "CURTAIN CALL", "ENTER", !won, enter >= 1f, e);
        }
    }
}
