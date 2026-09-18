using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Battle;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //ScoreScreen : the score. Step 2 of preparing a fight.
    //
    //The top row is TACET's part for this round, already written and shown to you.
    //Every row under it is one seated musician. Click (or click and drag) the boxes to
    //decide which beats they play. The bottom shows what the round will cost in stamina.
    //
    //Reading the hints under each beat:
    //   COVERED  you answer an attack          GAP   an attack with nobody answering
    //   ACCENT   several of you at once         FREE  you play where TACET is silent
    //   REST     both silent, stamina comes back
    public class ScoreScreen : GameScreen
    {
        //Score Layout
        private const int GridX = 330;
        private const int ColW = 114;
        private const int GaugeY = 134;
        private const int BeatHeaderY = 166;
        private const int EnemyRowY = 180;
        private const int EnemyRowH = 80;
        private const int RowsTop = 272;
        private const int RowsBottom = 530;
        private const int SummaryY = 538;
        private const int FooterY = 598;
        private Rectangle autoButton = new Rectangle(592, 630, 140, 50);
        private Rectangle clearButton = new Rectangle(744, 630, 140, 50);
        private Rectangle beginButton = new Rectangle(934, 626, 306, 58);

        //Step Nav
        private static string[] steps = { "01  STAGE", "02  SCORE", "03  PERFORM" };
        private const float NavY = 94f;
        private const float NavSpacing = 150f;

        //Beat Tag Words : indexed by BeatTag
        private static string[] tagWords = { "REST", "COVERED", "ACCENT", "GAP", "FREE" };

        //Score State
        private int[] seatOrder = new int[0];    // seated seats, front row first
        private int hoverSeat = -1;              // the seat whose row is under the mouse
        private int hoverBeat = -1;
        private bool painting;
        private bool paintValue;
        private float time;
        private string projectionLabel = "";
        private string perkLabel = "";

        public override void Load()
        {
            BuildSeatOrder();
            RefreshProjection();
            perkLabel = "SIGNATURE  /  " + Game.CurrentRun.Conductor.MechanicName;
            SoundBank.PlayMusic(Music.Prep);
        }

        //Seat Order : front row first, then middle, then back, left to right inside each
        private void BuildSeatOrder()
        {
            Formation f = Game.CurrentRun.Formation;
            List<int> order = new List<int>();

            for (int row = StageLayout.Rows.Length - 1; row >= 0; row--)
                for (int s = 0; s < StageLayout.SeatCount; s++)
                    if (StageLayout.SeatRow[s] == row && f.Seated[s] != null)
                        order.Add(s);

            seatOrder = order.ToArray();
        }

        //Row Height : rows share the space, capped so a small band does not look stretched
        private int RowH()
        {
            if (seatOrder.Length == 0) return 64;
            return Math.Min(64, (RowsBottom - RowsTop) / seatOrder.Length);
        }

        private Rectangle CellRect(int rowIndex, int beat)
        {
            int rowH = RowH();
            return new Rectangle(GridX + beat * ColW + 4, RowsTop + rowIndex * rowH + 3, ColW - 8, rowH - 6);
        }

        //Projection Refresh : rebuilt only when the plan changes, never every frame
        private void RefreshProjection()
        {
            BattleState battle = Game.CurrentRun.Battle;
            projectionLabel = "COST " + battle.PlannedCost()
                            + "     BACK " + battle.PlannedRecover()
                            + "     ENDS AT " + battle.ProjectedStamina() + " / " + Game.CurrentRun.MaxStamina;
        }

        public override void Update(float dt)
        {
            time += dt;
            BattleState battle = Game.CurrentRun.Battle;
            Formation f = Game.CurrentRun.Formation;

            //Hover Cell
            hoverSeat = -1;
            hoverBeat = -1;
            for (int i = 0; i < seatOrder.Length; i++)
                for (int b = 0; b < BattleRules.BeatsPerRound; b++)
                    if (Input.MouseOver(CellRect(i, b))) { hoverSeat = seatOrder[i]; hoverBeat = b; }

            //Paint Start : the first box clicked decides whether the drag fills or clears
            if (Input.MouseClicked() && hoverSeat >= 0)
            {
                painting = true;
                paintValue = !f.Plan[hoverSeat, hoverBeat];
            }
            if (!Input.MouseDown()) painting = false;

            //Paint : every box the drag passes over takes the same value
            if (painting && hoverSeat >= 0 && f.Plan[hoverSeat, hoverBeat] != paintValue)
            {
                f.Plan[hoverSeat, hoverBeat] = paintValue;
                SoundBank.Play(paintValue ? Sfx.NoteOn : Sfx.NoteOff);
                RefreshProjection();
            }

            //Plan Helpers
            if (Input.ClickedOn(autoButton) || Input.KeyPressed(Keys.A))
            {
                battle.AutoPlan();
                SoundBank.Play(Sfx.UiConfirm);
                RefreshProjection();
            }
            if (Input.ClickedOn(clearButton) || Input.KeyPressed(Keys.C))
            {
                f.ClearAll();
                SoundBank.Play(Sfx.UiBack);
                RefreshProjection();
            }

            //Back To Stage : the seats can be rearranged before any round
            bool stageStep = Input.ClickedOn(Ui.StepRect(steps.Length, 0, 640, NavY, NavSpacing));
            if (stageStep || Input.KeyPressed(Keys.Tab))
            {
                SoundBank.Play(Sfx.UiBack);
                Game.Screens.Change(new FormationScreen(true));
                return;
            }

            //Begin Round
            bool performStep = Input.ClickedOn(Ui.StepRect(steps.Length, 2, 640, NavY, NavSpacing));
            if (Input.ClickedOn(beginButton) || Input.KeyPressed(Keys.Enter) || performStep)
            {
                SoundBank.Play(Sfx.UiConfirm);
                Game.Screens.Change(new DuelScreen());
            }
        }

        public override void Draw(SpriteBatch sb)
        {
            RunState run = Game.CurrentRun;
            BattleState battle = run.Battle;

            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageDeep);
            Gfx.DrawGlowBox(sb, new Rectangle(0, 120, TacetGame.ScreenW, 520), Palette.Paper * 0.04f);

            DrawGauge(sb, battle);
            DrawBeatHeader(sb);
            DrawEnemyRow(sb, battle);
            DrawOurRows(sb, run);
            DrawSummary(sb, battle);
            DrawFooter(sb, run, battle);

            RunHud.DrawTop(sb, run, battle.EnemyTitle);
            Ui.StepNav(sb, steps, 1, 640, NavY, NavSpacing);
            RunHud.DrawTips(sb, run);
        }

        //Gauge : the round, and where the tug of war stands before it
        private void DrawGauge(SpriteBatch sb, BattleState battle)
        {
            Gfx.Text(sb, Game.BigFont, battle.RoundLabel, 36, GaugeY - 18, Palette.Paper, TextSize.Small);
            Gfx.TextSpaced(sb, Game.Font, perkLabel, 36, GaugeY + 12, Palette.LineGrey, TextSize.Tiny, 2f);

            int x = GridX + 40;
            int w = ColW * BattleRules.BeatsPerRound - 110;
            Gfx.TextSpacedRight(sb, Game.Font, "YOU", x - 10, GaugeY - 4, Palette.Paper, TextSize.Tiny, 2f);
            Gfx.TextSpaced(sb, Game.Font, "TACET", x + w + 10, GaugeY - 4, Palette.PaperDim, TextSize.Tiny, 2f);

            float t = (battle.Line / BattleRules.LineLimit + 1f) / 2f;   // 0 lost .. 1 won
            Rectangle bar = new Rectangle(x, GaugeY - 4, w, 10);
            Gfx.Pill(sb, bar, Palette.Void);
            Gfx.Pill(sb, new Rectangle(x, bar.Y, Math.Max(10, (int)(w * t)), 10), Palette.Paper);
            Gfx.Rect(sb, x + w / 2, bar.Y - 4, 1, 18, Palette.LineGrey);
            Gfx.Diamond(sb, x + w * t, bar.Center.Y, 7, Palette.Highlight);
            Gfx.DiamondOutline(sb, x + w * t, bar.Center.Y, 7, Palette.Void, 1f);
        }

        //Beat Header : the numbers 1 to 8 and the bar lines
        private void DrawBeatHeader(SpriteBatch sb)
        {
            for (int b = 0; b < BattleRules.BeatsPerRound; b++)
            {
                float cx = GridX + b * ColW + ColW / 2f;
                Color c = (b == hoverBeat) ? Palette.Highlight : Palette.LineGrey;
                Gfx.TextCentered(sb, Game.Font, NumberText.Get(b + 1), cx, BeatHeaderY, c, TextSize.Label);

                //Bar Line : every fourth beat, the way written music is divided
                if (b > 0 && b % 4 == 0)
                    Gfx.Rect(sb, GridX + b * ColW, EnemyRowY, 1, RowsBottom - EnemyRowY, Palette.LineGrey * 0.6f);
            }

            //Hover Column : a faint light down the beat under the mouse
            if (hoverBeat >= 0)
                Gfx.Rect(sb, GridX + hoverBeat * ColW, EnemyRowY, ColW, RowsBottom - EnemyRowY, Palette.Paper * 0.03f);
        }

        //Enemy Row : TACET's written part for this round
        private void DrawEnemyRow(SpriteBatch sb, BattleState battle)
        {
            Enemy e = battle.Enemy;
            Rectangle band = new Rectangle(36, EnemyRowY, GridX + ColW * 8 - 36, EnemyRowH);
            Gfx.Rect(sb, band, Palette.Void);
            Ornament.Stave(sb, GridX, EnemyRowY + 22, ColW * 8, 9, Palette.Paper * 0.06f);
            Gfx.RectOutline(sb, band, Palette.LineGrey * 0.5f, 1);

            //Enemy Tile : PLACEHOLDER for the enemy portrait
            Rectangle tile = new Rectangle(44, EnemyRowY + 8, EnemyRowH - 16, EnemyRowH - 16);
            Gfx.Rect(sb, tile, Palette.Stage);
            Gfx.DrawGlow(sb, tile.Center.X, tile.Center.Y, tile.Width * 0.5f, e.Tone * 0.6f);
            Ornament.CornerBrackets(sb, tile, 8, Palette.PaperDim, 1);

            Gfx.Text(sb, Game.BigFont, e.Name, tile.Right + 12, EnemyRowY + 8, Palette.Highlight, TextSize.Small * 0.9f);
            Ui.Tag(sb, e.KindLabel, tile.Right + 12, EnemyRowY + 44, e.Kind != EnemyKind.Normal, 1f);

            for (int b = 0; b < BattleRules.BeatsPerRound; b++)
            {
                float cx = GridX + b * ColW + ColW / 2f;
                float cy = EnemyRowY + 42;
                int power = battle.EnemyPower[b];

                if (battle.EnemyHidden[b])
                {
                    Gfx.TextCentered(sb, Game.BigFont, "???", cx, cy - 2, Palette.Paper, TextSize.Subtitle);
                    Gfx.TextSpacedCentered(sb, Game.Font, "HIDDEN", cx, EnemyRowY + 6, Palette.PaperDim, TextSize.Tiny, 2f);
                    continue;
                }

                if (power == 0)
                {
                    //Rest Mark : TACET is silent here
                    Gfx.Rect(sb, cx - 12, cy - 4, 24, 7, Palette.LineGrey * 0.6f);
                    continue;
                }

                //Attack : the note grows with its power, heavy ones get a ring
                float radius = Math.Min(16f, 4f + power * 1.1f);
                bool heavy = battle.IsHeavy(b);
                Gfx.Circle(sb, cx, cy - 2, radius, heavy ? Palette.Highlight : Palette.PaperDim);
                Gfx.Rect(sb, cx + radius - 2, cy - 2 - radius * 2.2f, 2, radius * 2.2f, heavy ? Palette.Highlight : Palette.PaperDim);
                if (heavy) Gfx.CircleOutline(sb, cx, cy - 2, radius + 5, Palette.Paper * 0.5f, 1f);

                Gfx.TextCentered(sb, Game.Font, NumberText.Get(power), cx, EnemyRowY + 70, Palette.Paper, TextSize.Label);
                Gfx.TextSpacedCentered(sb, Game.Font, heavy ? "HEAVY" : "LIGHT", cx, EnemyRowY + 6,
                                       heavy ? Palette.Highlight : Palette.LineGrey, TextSize.Tiny, 2f);
            }
        }

        //Our Rows : one per seated musician, the boxes are the plan
        private void DrawOurRows(SpriteBatch sb, RunState run)
        {
            Formation f = run.Formation;
            int rowH = RowH();

            if (seatOrder.Length == 0)
            {
                Gfx.TextCentered(sb, Game.StoryFont, "Nobody is on stage. Go back to 01 STAGE.", 640, 400, Palette.PaperDim, TextSize.Story);
                return;
            }

            for (int i = 0; i < seatOrder.Length; i++)
            {
                int seat = seatOrder[i];
                Musician m = f.Seated[seat];
                int y = RowsTop + i * rowH;

                //Row Header
                bool rowHover = (seat == hoverSeat);
                Rectangle head = new Rectangle(36, y + 3, GridX - 44, rowH - 6);
                Gfx.Rect(sb, head, rowHover ? Palette.Stage : Palette.Panel);
                Gfx.Rect(sb, head.X, head.Y, 2, head.Height, Palette.Paper * (0.3f + StageLayout.SeatRow[seat] * 0.35f));
                int tile = rowH - 14;
                MusicianArt.Tile(sb, new Rectangle(44, y + 7, tile, tile), m, 1f);
                Gfx.Text(sb, Game.BigFont, m.NameTag, 54 + tile, y + 4, Palette.Paper, TextSize.Small * 0.9f);
                Gfx.TextSpaced(sb, Game.Font, StageLayout.RowOf(seat).Name, 56 + tile, y + rowH - 22, Palette.LineGrey, TextSize.Tiny, 2f);
                Gfx.TextRight(sb, Game.Font, m.StatLabel, GridX - 18, y + rowH - 24, Palette.PaperDim, TextSize.Label);

                //Staff : faint lines behind the cells, so the plan reads as written music
                Ornament.Stave(sb, GridX, y + rowH / 2f - 12, ColW * 8, 6, Palette.Paper * 0.04f);

                //Cells
                for (int b = 0; b < BattleRules.BeatsPerRound; b++)
                {
                    Rectangle cell = CellRect(i, b);
                    bool on = f.Plan[seat, b];
                    bool hover = (seat == hoverSeat && b == hoverBeat);

                    Gfx.Rect(sb, cell, on ? Palette.Paper * 0.14f : Palette.Void * 0.5f);
                    Gfx.RectOutline(sb, cell, hover ? Palette.Highlight : Palette.LineGrey * 0.4f, 1);

                    if (on)
                    {
                        //Planned Note : a head with a stem and a flag
                        float r = Math.Min(cell.Height * 0.26f, 11f);
                        float nx = cell.Center.X - 3;
                        float ny = cell.Center.Y + 5;
                        Gfx.Circle(sb, nx, ny, r, Palette.Highlight);
                        Gfx.Rect(sb, nx + r - 2, ny - r * 2.6f, 2, r * 2.6f, Palette.Highlight);
                        Gfx.Line(sb, nx + r - 1, ny - r * 2.6f, nx + r + 6, ny - r * 1.6f, Palette.Highlight, 2f);
                    }
                    else
                    {
                        Gfx.Diamond(sb, cell.Center.X, cell.Center.Y, 2, Palette.LineGrey * 0.6f);
                    }
                }
            }
        }

        //Summary : for every beat, a small tug bar of our power against theirs, and the hint word
        private void DrawSummary(SpriteBatch sb, BattleState battle)
        {
            Gfx.TextSpaced(sb, Game.Font, "YOU  /  TACET", 36, SummaryY + 4, Palette.LineGrey, TextSize.Tiny, 2f);
            Gfx.TextSpaced(sb, Game.Font, "PLAN HINT", 36, SummaryY + 34, Palette.LineGrey, TextSize.Tiny, 2f);

            for (int b = 0; b < BattleRules.BeatsPerRound; b++)
            {
                float cx = GridX + b * ColW + ColW / 2f;
                BeatTag tag = battle.TagFor(b);
                int ours = battle.OurPowerAt(b);
                int theirs = battle.EnemyPower[b];
                bool hidden = battle.EnemyHidden[b];

                Gfx.TextRight(sb, Game.Font, NumberText.Get(ours), cx - 30, SummaryY - 2, Palette.Paper, TextSize.Label);
                Gfx.Text(sb, Game.Font, hidden ? "?" : NumberText.Get(theirs), cx + 30, SummaryY - 2, Palette.PaperDim, TextSize.Label);

                //Mini Tug : how much of the bar is ours
                Rectangle bar = new Rectangle((int)cx - 24, SummaryY + 4, 48, 6);
                Gfx.Rect(sb, bar, Palette.Void);
                float total = ours + (hidden ? 5 : theirs);
                float share = total <= 0f ? 0.5f : ours / total;
                Gfx.Rect(sb, bar.X, bar.Y, bar.Width * share, bar.Height, Palette.Paper);
                Gfx.RectOutline(sb, bar, Palette.LineGrey * 0.6f, 1);

                //Tag Word : GAP is framed so it jumps out as the thing to fix
                Color tagColor = Palette.LineGrey;
                if (tag == BeatTag.Free || tag == BeatTag.Accent) tagColor = Palette.Highlight;
                if (tag == BeatTag.Covered) tagColor = Palette.Paper;

                Gfx.TextSpacedCentered(sb, Game.Font, tagWords[(int)tag], cx, SummaryY + 34, tagColor, TextSize.Tiny, 2f);
                if (tag == BeatTag.Gap)
                    Gfx.RectOutline(sb, new Rectangle((int)cx - 28, SummaryY + 29, 56, 18), Palette.Accent, 1);
            }
        }

        //Footer : stamina forecast and the buttons
        private void DrawFooter(SpriteBatch sb, RunState run, BattleState battle)
        {
            Gfx.Rect(sb, 0, FooterY, TacetGame.ScreenW, TacetGame.ScreenH - FooterY, Palette.Void * 0.8f);
            Gfx.Rect(sb, 0, FooterY, TacetGame.ScreenW, 1, Palette.LineGrey * 0.5f);

            //Stamina Forecast : the dim bar is now, the bright bar is after this round
            Gfx.TextSpaced(sb, Game.Font, "STAMINA AFTER THIS ROUND", 36, FooterY + 16, Palette.PaperDim, TextSize.Tiny, 2f);
            Rectangle bar = new Rectangle(36, FooterY + 36, 520, 14);
            int projected = battle.ProjectedStamina();

            Ui.CapsuleBar(sb, bar, run.Stamina / (float)run.MaxStamina, Palette.LineGrey, 1f);
            if (projected > 0)
            {
                Rectangle after = new Rectangle(bar.X + 2, bar.Y + 4, (int)((bar.Width - 4) * projected / (float)run.MaxStamina), bar.Height - 8);
                Gfx.Rect(sb, after, Palette.Highlight);
            }

            Gfx.TextSpaced(sb, Game.Font, projectionLabel, 36, FooterY + 60, Palette.Paper, TextSize.Label, 1.5f);

            //Breath Warning : blinks when the plan costs more than the band has
            if (projected <= 0 && (int)(time * 3f) % 2 == 0)
                Gfx.TextSpaced(sb, Game.Font, "THE BAND WILL RUN OUT OF BREATH", 36, FooterY + 82, Palette.Accent, TextSize.Tiny, 2f);

            Ui.Button(sb, autoButton, "AUTO", "A", false);
            Ui.Button(sb, clearButton, "CLEAR", "C", false);
            Ui.Button(sb, beginButton, "BEGIN ROUND", "ENTER", true);
        }
    }
}
