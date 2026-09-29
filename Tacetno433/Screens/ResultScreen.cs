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
    //
    //Beside the numbers sits THE PERFORMANCE : every beat of the fight in a small grid, one row
    //per round. A filled box is a beat the band won, an empty one a beat TACET took. The arrow is
    //the stroke, bigger for a BOOST and smaller for an EASE, and a small diamond marks a PERFECT.
    //It is not there to be studied, just to look back at what was played.
    public class ResultScreen : GameScreen
    {
        private Rectangle continueButton = new Rectangle(490, 630, 300, 52);
        private const int LedgerX = 110;
        private const int LedgerW = 440;
        private const int SheetX = 660;
        private const int SheetY = 262;
        private const int CellW = 50;
        private const int CellH = 42;

        private static string[] roundNumerals = { "I", "II", "III" };
        private static Flick[] pattern = { Flick.Down, Flick.Left, Flick.Right, Flick.Up };

        //Key : what the marks in the grid mean, two columns under it, left of the rank
        private static string[] keyWords = { "WON", "PERFECT", "MISS", "NO STROKE", "COUNTER", "SPARK", "FERMATA", "ROLL" };
        private const int KeyColumn = 108;
        private const int KeyRow = 18;

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

            statNames[0] = "PERFECT BEATS";    statValues[0] = NumberText.Get(battle.PerfectCount);
            statNames[1] = "BEST COMBO";       statValues[1] = NumberText.Get(battle.BestCombo);
            statNames[2] = "FINAL LINE";       statValues[2] = NumberText.Signed((int)battle.Line);
            statNames[3] = "BREATH BACK";     statValues[3] = NumberText.Signed(recovered);
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

            footnote = battle.Collapsed ? "OUT OF BREATH  -  THE RUN ENDS HERE" : "THE RUN ENDS HERE";
            run.MarkLastStopLost();
            SaveFile.DeleteRun();          // a lost fight ends the run, there is nothing to continue

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

        //Sheet : the whole fight at a glance, one row of eight beats per round
        private void DrawSheet(SpriteBatch sb, BattleState battle, Color ink, Color soft, bool bright, float e)
        {
            if (battle == null) return;

            Gfx.TextSpaced(sb, Game.Font, "THE PERFORMANCE", SheetX, SheetY, soft * e, TextSize.Label, 4f);
            int rounds = Math.Min(battle.Round, BattleRules.MaxRounds);
            Color back = bright ? Palette.StageLight : Palette.Void;

            for (int r = 0; r < rounds; r++)
            {
                float rowE = MathHelper.Clamp(e * 2f - 0.3f - r * 0.25f, 0f, 1f);
                int y = SheetY + 30 + r * (CellH + 12);
                Gfx.TextRight(sb, Game.BigFont, roundNumerals[r], SheetX + 22, y + 8, soft * rowE, TextSize.Small);

                for (int b = 0; b < BattleRules.BeatsPerRound; b++)
                {
                    int x = SheetX + 34 + b * (CellW + 4) + (b >= 4 ? 10 : 0);    // a gap for the bar line
                    Rectangle cell = new Rectangle(x, y, CellW, CellH);
                    BeatResult mark = battle.Sheet[r][b];
                    DrawSheetCell(sb, cell, mark, b, ink, back, rowE);
                }
            }

            DrawKey(sb, ink, soft, e);
        }

        //Key : a tiny cell for each mark with its word beside it, so the grid can be read
        //without the guide. Placed under the third row, whether or not it was played.
        private void DrawKey(SpriteBatch sb, Color ink, Color soft, float e)
        {
            int top = SheetY + 30 + BattleRules.MaxRounds * (CellH + 12) + 2;
            for (int i = 0; i < keyWords.Length; i++)
            {
                int x = SheetX + 34 + (i % 2) * KeyColumn;
                int y = top + (i / 2) * KeyRow;
                Rectangle cell = new Rectangle(x, y, 18, 14);
                Color mark = ink * (0.8f * e);

                if (i == 0) Gfx.Rect(sb, cell, ink * (0.9f * e));                                         // WON
                else Gfx.RectOutline(sb, cell, ink * (0.45f * e), 1);
                if (i == 1) Gfx.Diamond(sb, cell.Right - 4, cell.Y + 4, 2, mark);                           // PERFECT
                if (i == 2) Gfx.Line(sb, cell.X + 3, cell.Bottom - 3, cell.Right - 3, cell.Y + 3, mark, 1.5f);   // MISS
                if (i == 3) Gfx.CircleOutline(sb, cell.Center.X, cell.Center.Y, 4, mark, 1f);              // NO STROKE
                if (i == 4) Gfx.CircleOutline(sb, cell.X + 5, cell.Y + 5, 3, mark, 1f);                    // COUNTER
                if (i == 5) Gfx.Diamond(sb, cell.X + 4, cell.Bottom - 4, 2, mark);                          // SPARK
                if (i == 6) NoteGlyph.FermataSign(sb, cell.Right - 6, cell.Bottom - 2, 4f, mark);           // FERMATA
                if (i == 7)                                                                                 // ROLL
                    for (int z = 0; z < 3; z++)
                        Gfx.Line(sb, cell.Right - 13 + z * 3, cell.Bottom - 3 - (z % 2) * 3, cell.Right - 10 + z * 3, cell.Bottom - 6 + (z % 2) * 3, mark, 1f);

                Gfx.TextSpaced(sb, Game.Font, keyWords[i], x + 24, y + 1, soft * e, TextSize.Tiny, 1.5f);
            }
        }

        //Sheet Cell : filled when the band won the beat, outlined when TACET did
        private void DrawSheetCell(SpriteBatch sb, Rectangle cell, BeatResult mark, int b, Color ink, Color back, float a)
        {
            if (!mark.Done)
            {
                Gfx.RectOutline(sb, cell, ink * (0.15f * a), 1);          // never played, the fight ended first
                return;
            }
            if (mark.Grade == Grade.None)
            {
                Gfx.Rect(sb, cell.Center.X - 8, cell.Center.Y, 16, 2, ink * (0.4f * a));   // a rest
                return;
            }

            bool won = mark.Push > 0;
            if (won) Gfx.Rect(sb, cell, ink * (0.9f * a));
            Gfx.RectOutline(sb, cell, ink * ((won ? 0.9f : 0.45f) * a), 1);
            Color stroke = won ? back * a : ink * (0.8f * a);

            if (mark.Grade == Grade.Hesitate)
            {
                Gfx.CircleOutline(sb, cell.Center.X, cell.Center.Y, 6, stroke, 1.5f);   // no stroke at all
            }
            else
            {
                float length = mark.PlayerChoice == Choice.Boost ? 26f : (mark.PlayerChoice == Choice.Ease ? 12f : 19f);
                DrawWay(sb, pattern[b % 4], cell.Center.X, cell.Center.Y, length, stroke);
                if (mark.Grade == Grade.Miss) Gfx.Line(sb, cell.X + 6, cell.Bottom - 6, cell.Right - 6, cell.Y + 6, stroke * 0.7f, 1.5f);
            }

            if (mark.Grade == Grade.Perfect) Gfx.Diamond(sb, cell.Right - 7, cell.Y + 7, 3, stroke);
            if (mark.Signature) Gfx.DiamondOutline(sb, cell.Center.X, cell.Center.Y, CellH * 0.62f, stroke, 1.5f);

            //Extras : a ring in the corner for a COUNTER, a small mark low down for the spark of a
            //pair (filled when it landed), a short zigzag for TACET's roll, an arch for its fermata
            if (mark.Counter) Gfx.CircleOutline(sb, cell.X + 7, cell.Y + 7, 4, stroke, 1.5f);
            if (mark.Double)
            {
                bool flicked = mark.GraceGrade == Grade.Perfect || mark.GraceGrade == Grade.Good;
                if (flicked) Gfx.Diamond(sb, cell.X + 7, cell.Bottom - 7, 3, stroke);
                else Gfx.DiamondOutline(sb, cell.X + 7, cell.Bottom - 7, 3, stroke, 1f);
            }
            if (mark.Fermata) NoteGlyph.FermataSign(sb, cell.Right - 12, cell.Bottom - 5, 5f, stroke);
            if (mark.Tremolo)
                for (int z = 0; z < 3; z++)
                    Gfx.Line(sb, cell.Right - 18 + z * 4, cell.Bottom - 5 - (z % 2) * 4, cell.Right - 14 + z * 4, cell.Bottom - 9 + (z % 2) * 4, stroke, 1f);
        }

        //Way : a straight arrow of the given length through (cx, cy)
        private static void DrawWay(SpriteBatch sb, Flick way, float cx, float cy, float length, Color color)
        {
            Vector2 d = Vector2.Zero;
            if (way == Flick.Up) d = new Vector2(0f, -1f);
            if (way == Flick.Down) d = new Vector2(0f, 1f);
            if (way == Flick.Left) d = new Vector2(-1f, 0f);
            if (way == Flick.Right) d = new Vector2(1f, 0f);

            Vector2 centre = new Vector2(cx, cy);
            Vector2 tip = centre + d * (length / 2f);
            Gfx.Line(sb, centre - d * (length / 2f), tip - d * 4f, color, 2f);
            Vector2 c = tip - d * 3f;
            if (way == Flick.Up) Gfx.Triangle(sb, c.X, c.Y, 7, true, color);
            else if (way == Flick.Down) Gfx.Triangle(sb, c.X, c.Y, 7, false, color);
            else Gfx.Arrow(sb, c.X, c.Y, 7, way == Flick.Right, color);
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

            DrawSheet(sb, Game.CurrentRun.Battle, ink, soft, won, e);

            //Rank : a letter in a diamond, under the performance
            if (won)
            {
                float rx = 1050;
                float ry = 548;
                float spin = (1f - e) * 40f;
                Gfx.Diamond(sb, rx, ry, 66 + spin, Palette.Ink * e);
                Gfx.DiamondOutline(sb, rx, ry, 76 + spin, Palette.Ink * (0.5f * e), 1f);
                Gfx.TextCentered(sb, Game.LogoFont, rank, rx, ry - 4, Palette.Paper * e, TextSize.Banner * 0.9f);
                Gfx.TextSpacedCentered(sb, Game.Font, "RANK", rx - 110, ry - 6, soft * e, TextSize.Tiny, 4f);
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
                    //Lost : centred under the ledger, clear of THE PERFORMANCE legend on the right
                    Ui.Tag(sb, footnote, LedgerX + (LedgerW - Ui.TagWidth(footnote)) / 2f, fy, true, e);
                }
            }

            //Continue Button : dark on the bright page, light on the dark one
            Ui.Button(sb, continueButton, won ? "CONTINUE" : "CURTAIN CALL", "ENTER", !won, enter >= 1f, e);
        }
    }
}
