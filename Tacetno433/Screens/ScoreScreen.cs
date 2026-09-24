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
    //decide which beats they play. The small arrow beside each beat number is the way the
    //baton goes on that beat in the duel (the 4/4 pattern: down, left, right, up).
    //
    //The FORECAST row names every beat the way it looks on paper, if it is simply played:
    //   DOMINATING  FAVORED  EVEN  STRUGGLING  HOPELESS    our power against TACET's
    //   FREE HIT    you play where TACET is silent
    //   UNGUARDED   TACET attacks and nobody answers (framed, it is the thing to fix)
    //   REST        both silent, stamina comes back
    //   UNKNOWN     a ??? beat, nobody can tell
    //The hollow diamond on the gauge at the top is where the line would end the round.
    //PREVIEW plays the plan through once, so it can be seen and heard before the fight.
    //
    //If this enemy has a trait on this floor, its name sits on TACET's row in place of the
    //kind tag. Point at it to read what it does. The first time a trait is met, it opens by
    //itself for a few seconds, so nobody meets a new rule without being told.
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
        private Rectangle previewButton = new Rectangle(428, 630, 154, 50);
        private Rectangle autoButton = new Rectangle(592, 630, 140, 50);
        private Rectangle clearButton = new Rectangle(744, 630, 140, 50);
        private Rectangle beginButton = new Rectangle(934, 626, 306, 58);
        private Rectangle enemyInfo = new Rectangle(36, 180, 290, 80);        // TACET's name and trait
        private Rectangle bargainBox = new Rectangle(330, 230, 620, 250);
        private Rectangle acceptButton = new Rectangle(380, 404, 250, 50);
        private Rectangle refuseButton = new Rectangle(650, 404, 250, 50);

        //Row Power Tags : what the row does to power, back to front
        private static string[] rowPowerTag = { "x0.8", "", "x1.3" };

        //Trait Seen : traits met at least once since the game started, so each one is
        //explained on its own the very first time
        private static bool[] traitSeen = new bool[16];
        private float traitIntro;

        //Step Nav
        private static string[] steps = { "01  STAGE", "02  SCORE", "03  PERFORM" };
        private const float NavY = 94f;
        private const float NavSpacing = 150f;

        //Forecast Words : indexed by Forecast
        private static string[] forecastWords =
        {
            "REST", "FREE HIT", "UNGUARDED", "UNKNOWN", "DOMINATING", "FAVORED", "EVEN", "STRUGGLING", "HOPELESS"
        };

        //Beat Pattern : the way the baton goes on each beat of the bar in the duel
        private static int[] patternWay = { 0, 1, 2, 3 };   // 0 down, 1 left, 2 right, 3 up

        //Score State
        private int[] seatOrder = new int[0];    // seated seats, front row first
        private int hoverSeat = -1;              // the seat whose row is under the mouse
        private int hoverBeat = -1;
        private bool painting;
        private bool paintValue;
        private float time;
        private string projectionLabel = "";
        private string perkLabel = "";
        private string forecastLabel = "";
        private float forecastLine;

        //Preview : the plan played through once. -1 when it is not playing.
        private float preview = -1f;
        private int previewBeat = -1;

        public override void Load()
        {
            BuildSeatOrder();
            RefreshProjection();
            perkLabel = "SIGNATURE  /  " + Game.CurrentRun.Conductor.MechanicName;

            //First Meeting : a trait nobody has seen yet explains itself
            BattleState battle = Game.CurrentRun.Battle;
            int trait = (int)battle.Enemy.Trait;
            if (battle.TraitShown && !traitSeen[trait])
            {
                traitSeen[trait] = true;
                traitIntro = 5f;
            }
            SoundBank.PlayMusic(RunFlow.FightMusic(Game.CurrentRun));
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

            //Forecast : where the line ends if every beat is simply played as written
            forecastLine = battle.ForecastLine();
            forecastLabel = "AS WRITTEN  " + NumberText.Signed((int)Math.Round(forecastLine - battle.Line));
        }

        public override void Update(float dt)
        {
            time += dt;
            BattleState battle = Game.CurrentRun.Battle;
            Formation f = Game.CurrentRun.Formation;
            if (traitIntro > 0f) traitIntro -= dt;

            //THE BARGAIN : nothing else on the page works until the deal is answered
            if (battle.BargainOffered)
            {
                bool accept = Input.ClickedOn(acceptButton) || Input.KeyPressed(Keys.Y);
                bool refuse = Input.ClickedOn(refuseButton) || Input.KeyPressed(Keys.N);
                if (accept || refuse)
                {
                    battle.AnswerBargain(accept);
                    SoundBank.Play(accept ? Sfx.MotifGet : Sfx.UiBack);
                    RefreshProjection();
                }
                return;
            }

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

            //Preview : play the plan through once, a beat at a time
            if (Input.ClickedOn(previewButton) || Input.KeyPressed(Keys.P))
            {
                preview = 0f;
                previewBeat = -1;
                SoundBank.Play(Sfx.UiConfirm);
            }
            if (preview >= 0f)
            {
                preview += dt / BattleRules.PreviewBeatTime;
                int b = (int)preview;
                if (b != previewBeat && b < BattleRules.BeatsPerRound)
                {
                    previewBeat = b;
                    if (battle.OurPowerAt(b) > 0) SoundBank.Play(Sfx.NoteOn);
                    else if (battle.EnemyPower[b] > 0) SoundBank.Play(Sfx.BeatTick);
                }
                if (preview >= BattleRules.BeatsPerRound)
                {
                    preview = -1f;
                    previewBeat = -1;
                }
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
            DrawForecast(sb, battle);
            DrawFooter(sb, run, battle);

            RunHud.DrawTop(sb, run, battle.EnemyTitle);
            Ui.StepNav(sb, steps, 1, 640, NavY, NavSpacing);
            RunHud.DrawTips(sb, run);

            //Trait Tooltip : while pointing at TACET's name, or on the first meeting
            if (battle.TraitShown && (Input.MouseOver(enemyInfo) || traitIntro > 0f))
            {
                float tx = Input.MouseOver(enemyInfo) ? Input.MousePos.X : enemyInfo.X + 60;
                float ty = Input.MouseOver(enemyInfo) ? Input.MousePos.Y : enemyInfo.Bottom - 10;
                Ui.Tooltip(sb, battle.Enemy.TraitName, battle.Enemy.TraitWrapped, tx, ty);
            }

            if (battle.BargainOffered) DrawBargain(sb, run);
        }

        //Bargain : the devil's offer, before round two
        private void DrawBargain(SpriteBatch sb, RunState run)
        {
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Color.Black * 0.75f);
            Hollow.Eclipse(sb, bargainBox.Center.X, bargainBox.Y - 10, 60f, time * 0.4f, 1f);
            Gfx.Rect(sb, bargainBox, Palette.Void);
            Ornament.DoubleFrame(sb, bargainBox, Palette.Paper);

            Gfx.TextSpacedCentered(sb, Game.Font, "THE DEVIL'S STRING  /  THE BARGAIN", bargainBox.Center.X, bargainBox.Y + 30, Palette.PaperDim, TextSize.Tiny, 4f);
            Gfx.TextCentered(sb, Game.BigFont, "One string, one bow, one bargain.", bargainBox.Center.X, bargainBox.Y + 74, Palette.Highlight, TextSize.Subtitle);
            Gfx.TextCentered(sb, Game.StoryFont, "Your band hits 50 percent harder for this round.", bargainBox.Center.X, bargainBox.Y + 120, Palette.Paper, TextSize.Story);
            Gfx.TextCentered(sb, Game.StoryFont, "Your stamina limit drops by 15 for the rest of the run.", bargainBox.Center.X, bargainBox.Y + 148, Palette.Paper, TextSize.Story);

            Ui.Button(sb, acceptButton, "ACCEPT", "Y", true);
            Ui.Button(sb, refuseButton, "REFUSE", "N", false);
        }

        //Gauge : the round, and where the tug of war stands before it
        private void DrawGauge(SpriteBatch sb, BattleState battle)
        {
            Gfx.Text(sb, Game.BigFont, battle.RoundLabel, 36, GaugeY - 18, Palette.Paper, TextSize.Small);
            float roundW = Gfx.TextWidth(Game.BigFont, battle.RoundLabel, TextSize.Small);
            Gfx.TextSpaced(sb, Game.Font, battle.TempoLabel, 36 + roundW + 14, GaugeY - 10, Palette.PaperDim, TextSize.Tiny, 2f);
            Gfx.TextSpaced(sb, Game.Font, perkLabel, 36, GaugeY + 12, Palette.LineGrey, TextSize.Tiny, 2f);
            Gfx.TextSpaced(sb, Game.Font, Game.CurrentRun.Conductor.RecipeLabel, 36, GaugeY + 28, Palette.PaperDim, TextSize.Tiny, 1.5f);

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

            //Forecast Mark : a hollow diamond where the line would end if the plan is played
            float ft = (forecastLine / BattleRules.LineLimit + 1f) / 2f;
            Gfx.DiamondOutline(sb, x + w * ft, bar.Center.Y, 10, Palette.Highlight, 1.5f);
            float labelX = MathHelper.Clamp(x + w * ft, x + 60, x + w - 60);
            Gfx.TextSpacedCentered(sb, Game.Font, forecastLabel, labelX, bar.Bottom + 5, Palette.PaperDim, TextSize.Tiny, 1.5f);
        }

        //Beat Header : the numbers 1 to 8 and the bar lines
        private void DrawBeatHeader(SpriteBatch sb)
        {
            for (int b = 0; b < BattleRules.BeatsPerRound; b++)
            {
                float cx = GridX + b * ColW + ColW / 2f;
                Color c = (b == hoverBeat || b == previewBeat) ? Palette.Highlight : Palette.LineGrey;
                Gfx.TextCentered(sb, Game.Font, NumberText.Get(b + 1), cx - 8, BeatHeaderY, c, TextSize.Label);
                DrawWay(sb, patternWay[b % 4], cx + 10, BeatHeaderY, c);

                //Bar Line : every fourth beat, the way written music is divided
                if (b > 0 && b % 4 == 0)
                    Gfx.Rect(sb, GridX + b * ColW, EnemyRowY, 1, RowsBottom - EnemyRowY, Palette.LineGrey * 0.6f);
            }

            //Hover Column : a faint light down the beat under the mouse
            if (hoverBeat >= 0)
                Gfx.Rect(sb, GridX + hoverBeat * ColW, EnemyRowY, ColW, RowsBottom - EnemyRowY, Palette.Paper * 0.03f);

            //Preview Playhead : a bright needle sweeping across the plan
            if (preview >= 0f)
            {
                float px = GridX + preview * ColW;
                Gfx.Rect(sb, GridX + previewBeat * ColW, EnemyRowY, ColW, RowsBottom - EnemyRowY, Palette.Paper * 0.06f);
                Gfx.Rect(sb, px, EnemyRowY - 6, 2, RowsBottom - EnemyRowY + 12, Palette.Highlight * 0.8f);
                Gfx.DrawGlow(sb, px, (EnemyRowY + RowsBottom) / 2f, 60f, Palette.Paper * 0.08f);
            }
        }

        //Way : a small arrow for the baton pattern, 0 down, 1 left, 2 right, 3 up
        private void DrawWay(SpriteBatch sb, int way, float cx, float cy, Color color)
        {
            if (way == 0) Gfx.Triangle(sb, cx, cy, 5, false, color);
            else if (way == 1) Gfx.Arrow(sb, cx, cy, 5, false, color);
            else if (way == 2) Gfx.Arrow(sb, cx, cy, 5, true, color);
            else Gfx.Triangle(sb, cx, cy, 5, true, color);
        }

        //Enemy Row : TACET's written part for this round
        private void DrawEnemyRow(SpriteBatch sb, BattleState battle)
        {
            Enemy e = battle.Enemy;
            Rectangle band = new Rectangle(36, EnemyRowY, GridX + ColW * 8 - 36, EnemyRowH);
            Gfx.Rect(sb, band, Palette.Void);
            Ornament.Stave(sb, GridX, EnemyRowY + 22, ColW * 8, 9, Palette.Paper * 0.06f);
            Gfx.RectOutline(sb, band, Palette.LineGrey * 0.5f, 1);

            //Enemy Tile : the enemy's picture shrunk to a square, or an empty slot
            Rectangle tile = new Rectangle(44, EnemyRowY + 8, EnemyRowH - 16, EnemyRowH - 16);
            Gfx.Rect(sb, tile, Palette.Stage);
            ArtBank.DrawOrSlot(sb, ArtBank.EnemyOf(e), tile, Palette.Paper, 1f);

            //Name and Tag : the trait when it has one on this floor (point at it to read it),
            //otherwise what kind of fight this is
            Gfx.Text(sb, Game.BigFont, e.Name, tile.Right + 12, EnemyRowY + 8, Palette.Highlight, TextSize.Small * 0.9f);
            if (battle.TraitShown)
            {
                Ui.Tag(sb, e.TraitName, tile.Right + 12, EnemyRowY + 44, true, 1f);
                float tagEnd = tile.Right + 12 + Ui.TagWidth(e.TraitName);
                if (traitIntro > 0f || Input.MouseOver(enemyInfo)) Gfx.Diamond(sb, tagEnd + 10, EnemyRowY + 53, 4, Palette.Highlight);
                else Gfx.TextSpaced(sb, Game.Font, "?", tagEnd + 8, EnemyRowY + 47, Palette.PaperDim, TextSize.Tiny, 1f);
            }
            else
            {
                Ui.Tag(sb, e.KindLabel, tile.Right + 12, EnemyRowY + 44, e.Kind != EnemyKind.Normal, 1f);
            }

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
                bool pair = battle.EnemyDouble[b];
                bool roll = battle.IsTremolo(b);
                bool held = battle.IsFermata(b);
                float noteX = pair ? cx - 7f : cx;
                Color ink = heavy || roll || held ? Palette.Highlight : Palette.PaperDim;
                Gfx.Circle(sb, noteX, cy - 2, radius, ink);
                Gfx.Rect(sb, noteX + radius - 2, cy - 2 - radius * 2.2f, 2, radius * 2.2f, ink);
                if (heavy && !pair) Gfx.CircleOutline(sb, noteX, cy - 2, radius + 5, Palette.Paper * 0.5f, 1f);

                //Pair : a ribbon from the note to a spark, the same as the duel shows it
                if (pair)
                {
                    float gx = cx + 16f;
                    Gfx.Rect(sb, noteX, cy - 6, gx - noteX, 8, ink * 0.4f);
                    NoteGlyph.Spark(sb, gx, cy - 2, 10f, Palette.Highlight);
                }

                //Roll : the zigzag bar of TACET's roll under the note
                if (roll)
                    for (int z = 0; z < 4; z++)
                        Gfx.Line(sb, cx - 14 + z * 7, cy + 13 + (z % 2) * 4, cx - 7 + z * 7, cy + 17 - (z % 2) * 4, Palette.Highlight, 1.5f);

                Gfx.TextCentered(sb, Game.Font, NumberText.Get(battle.EnemyStrikeAt(b)), cx, EnemyRowY + 70, Palette.Paper, TextSize.Label);
                //Fermata : the held note carries its sign beside the head
                if (held) NoteGlyph.FermataSign(sb, cx + 20f, cy - 16f, 9f, Palette.Highlight);

                string word = roll ? "TREMOLO" : (held ? "FERMATA" : (pair ? "PAIR" : (heavy ? "HEAVY" : "LIGHT")));
                Gfx.TextSpacedCentered(sb, Game.Font, word, cx, EnemyRowY + 6,
                                       heavy || roll || pair || held ? Palette.Highlight : Palette.LineGrey, TextSize.Tiny, 2f);
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

                //Row and Trait : where they sit, and what they do that nobody else does
                string rowName = StageLayout.RowOf(seat).Name;
                Gfx.TextSpaced(sb, Game.Font, rowName, 56 + tile, y + rowH - 22, Palette.LineGrey, TextSize.Tiny, 2f);
                float rowW = Gfx.SpacedWidth(Game.Font, rowName, TextSize.Tiny, 2f);
                Gfx.TextSpaced(sb, Game.Font, m.TraitName, 56 + tile + rowW + 12, y + rowH - 22, Palette.PaperDim, TextSize.Tiny, 1.5f);

                //Numbers : power with the row's effect beside it, then cost, from the right edge
                DrawRowNumbers(sb, run, m, seat, GridX - 18, y + 8);

                //Staff : faint lines behind the cells, so the plan reads as written music
                Ornament.Stave(sb, GridX, y + rowH / 2f - 12, ColW * 8, 6, Palette.Paper * 0.04f);

                //Cells
                for (int b = 0; b < BattleRules.BeatsPerRound; b++)
                {
                    Rectangle cell = CellRect(i, b);
                    bool on = f.Plan[seat, b];
                    bool hover = (seat == hoverSeat && b == hoverBeat);

                    bool playing = on && b == previewBeat;
                    Gfx.Rect(sb, cell, playing ? Palette.Paper * 0.45f : (on ? Palette.Paper * 0.14f : Palette.Void * 0.5f));
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
                    else if (Game.CurrentRun.Battle.HeldNoteSeat(b) == seat)
                    {
                        //HELD NOTE : a hollow note tied to the one before, it rings on for free
                        float r = Math.Min(cell.Height * 0.26f, 11f);
                        float nx = cell.Center.X - 3;
                        float ny = cell.Center.Y + 5;
                        Gfx.CircleOutline(sb, nx, ny, r, Palette.Paper * 0.8f, 1.5f);
                        Gfx.Arc(sb, cell.X - 4, ny + r + 2, ColW * 0.5f, 0.15f, MathHelper.Pi - 0.15f, Palette.Paper * 0.6f, 1.5f);
                    }
                    else
                    {
                        Gfx.Diamond(sb, cell.Center.X, cell.Center.Y, 2, Palette.LineGrey * 0.6f);
                    }
                }
            }
        }

        //Forecast : one word per beat saying how it looks on paper, the two numbers under it,
        //and a small bar of how much of the beat is ours
        private void DrawForecast(SpriteBatch sb, BattleState battle)
        {
            Gfx.TextSpaced(sb, Game.Font, "FORECAST", 36, SummaryY + 4, Palette.Paper, TextSize.Label, 3f);
            Gfx.TextSpaced(sb, Game.Font, "IF PLAYED AS WRITTEN", 36, SummaryY + 26, Palette.LineGrey, TextSize.Tiny, 2f);

            for (int b = 0; b < BattleRules.BeatsPerRound; b++)
            {
                float cx = GridX + b * ColW + ColW / 2f;
                Forecast forecast = battle.ForecastFor(b);
                int ours = battle.OurPowerAt(b);
                int theirs = battle.EnemyStrikeAt(b);
                bool hidden = battle.EnemyHidden[b];

                //Word : brighter the better it looks, the two that need fixing are framed
                Color color = Palette.PaperDim;
                if (forecast == Forecast.Dominating || forecast == Forecast.Favored || forecast == Forecast.FreeHit) color = Palette.Highlight;
                if (forecast == Forecast.Even) color = Palette.Paper;
                if (forecast == Forecast.Rest) color = Palette.LineGrey;
                bool fixThis = forecast == Forecast.Hopeless || forecast == Forecast.Unguarded;
                if (fixThis) color = Palette.Accent;
                if (b == previewBeat) color = Palette.Highlight;

                Gfx.TextSpacedCentered(sb, Game.Font, forecastWords[(int)forecast], cx, SummaryY + 2, color, TextSize.Label, 1.5f);
                if (fixThis)
                    Gfx.RectOutline(sb, new Rectangle((int)cx - 52, SummaryY - 3, 104, 22), Palette.Accent, 1);

                //Numbers : ours : theirs
                Gfx.TextRight(sb, Game.Font, NumberText.Get(ours), cx - 6, SummaryY + 23, Palette.Paper, TextSize.Tiny);
                Gfx.TextCentered(sb, Game.Font, ":", cx, SummaryY + 29, Palette.LineGrey, TextSize.Tiny);
                Gfx.Text(sb, Game.Font, hidden ? "?" : NumberText.Get(theirs), cx + 6, SummaryY + 23, Palette.PaperDim, TextSize.Tiny);

                //Share Bar : how much of the beat is ours
                Rectangle bar = new Rectangle((int)cx - 28, SummaryY + 44, 56, 5);
                Gfx.Rect(sb, bar, Palette.Void);
                float total = ours + (hidden ? 5 : theirs);
                float share = total <= 0f ? 0.5f : ours / total;
                Gfx.Rect(sb, bar.X, bar.Y, bar.Width * share, bar.Height, Palette.Paper);
                Gfx.RectOutline(sb, bar, Palette.LineGrey * 0.6f, 1);
            }
        }

        //Row Numbers : "PWR 7 x1.3   COST 6", drawn right to left from rightX, no string built
        private void DrawRowNumbers(SpriteBatch sb, RunState run, Musician m, int seat, float rightX, float y)
        {
            float x = rightX;
            string cost = NumberText.Get(run.CostOf(m));
            Gfx.TextRight(sb, Game.Font, cost, x, y, Palette.Paper, TextSize.Label);
            x -= Gfx.TextWidth(Game.Font, cost, TextSize.Label) + 4;
            Gfx.TextRight(sb, Game.Font, "COST", x, y + 2, Palette.LineGrey, TextSize.Tiny);
            x -= Gfx.TextWidth(Game.Font, "COST", TextSize.Tiny) + 12;

            string tag = rowPowerTag[StageLayout.SeatRow[seat]];
            if (tag.Length > 0)
            {
                Gfx.TextRight(sb, Game.Font, tag, x, y + 2, Palette.PaperDim, TextSize.Tiny);
                x -= Gfx.TextWidth(Game.Font, tag, TextSize.Tiny) + 4;
            }
            string power = NumberText.Get(run.PowerOf(m));
            Gfx.TextRight(sb, Game.Font, power, x, y, Palette.Paper, TextSize.Label);
            x -= Gfx.TextWidth(Game.Font, power, TextSize.Label) + 4;
            Gfx.TextRight(sb, Game.Font, "PWR", x, y + 2, Palette.LineGrey, TextSize.Tiny);
        }

        //Footer : stamina forecast and the buttons
        private void DrawFooter(SpriteBatch sb, RunState run, BattleState battle)
        {
            Gfx.Rect(sb, 0, FooterY, TacetGame.ScreenW, TacetGame.ScreenH - FooterY, Palette.Void * 0.8f);
            Gfx.Rect(sb, 0, FooterY, TacetGame.ScreenW, 1, Palette.LineGrey * 0.5f);

            //Stamina Forecast : the dim bar is now, the bright bar is after this round
            Gfx.TextSpaced(sb, Game.Font, "STAMINA AFTER THIS ROUND", 36, FooterY + 16, Palette.PaperDim, TextSize.Tiny, 2f);
            Rectangle bar = new Rectangle(36, FooterY + 36, 380, 14);
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

            Ui.Button(sb, previewButton, preview >= 0f ? "PLAYING" : "PREVIEW", "P", false);
            Ui.Button(sb, autoButton, "AUTO", "A", false);
            Ui.Button(sb, clearButton, "CLEAR", "C", false);
            Ui.Button(sb, beginButton, "BEGIN ROUND", "ENTER", true);
        }
    }
}
