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
    //FormationScreen : the stage. The only step of getting ready for a fight (round 12 : the
    //score page with its beat plan is gone, every stroke in the duel decides who plays).
    //
    //Left  : the orchestra, three rows of numbered seats. Where a musician sits is how they play:
    //        the ROW  decides which strokes bring them in: small (p) the back row, middle (mf)
    //                 the middle row too, big (f) the whole band. An empty row is skipped.
    //        the SIDE (left, centre, right) decides on which beats the baton points at them (CUE):
    //                 they hit harder on those beats.
    //Right : everyone in the ensemble as portrait cards, on stage or on the bench.
    //Drag a card onto a seat to place them, drag a seated musician back onto the cards
    //(or right click them) to bench them. Dropping on someone swaps the two.
    //
    //Opened two ways:
    //   battle mode : before a fight and between its rounds. The strip at the top is TACET's
    //                 part for the coming round, each beat with the side the baton will point at,
    //                 so the strongest players can be seated where TACET hits hardest.
    //                 If the enemy has a trait on this floor, its name sits on the strip. The first
    //                 time a trait is met it opens by itself for a few seconds.
    //   view mode   : from the route page, just to rearrange, leads back to the route
    public class FormationScreen : GameScreen
    {
        //Formation Layout
        private Rectangle stageBox = new Rectangle(170, 250, 560, 332);
        private Rectangle rosterBox = new Rectangle(770, 206, 476, 376);
        private Rectangle detailBox = new Rectangle(36, 596, 880, 108);
        private Rectangle primaryButton = new Rectangle(950, 628, 290, 56);
        private const float SeatScale = 0.75f;
        private const int CardGap = 10;
        private const int CardColumns = 4;

        //Call Strip : TACET's part for the coming round, battle mode only
        private Rectangle callBox = new Rectangle(36, 128, 1210, 72);
        private Rectangle enemyInfo = new Rectangle(36, 128, 284, 72);
        private const int CallX = 330;
        private const int CallW = 114;

        //Bargain : THE DEVIL'S STRING's offer before round two
        private Rectangle bargainBox = new Rectangle(330, 230, 620, 250);
        private Rectangle acceptButton = new Rectangle(380, 404, 250, 50);
        private Rectangle refuseButton = new Rectangle(650, 404, 250, 50);

        //Step Nav : the stage, then the duel itself
        private static string[] steps = { "01  STAGE", "02  PERFORM" };
        private const float NavY = 94f;
        private const float NavSpacing = 150f;

        //Riser Shape : the top and bottom of each tier, from 0 to 1 down the stage box
        private static float[] tierTop = { 0.04f, 0.31f, 0.58f };
        private static float[] tierBottom = { 0.34f, 0.62f, 0.93f };
        private static string[] rowShort = { "EVERY STROKE", "MIDDLE AND BIG", "BIG STROKES" };
        private static string[] seatNumbers = { "01", "02", "03", "04", "05", "06", "07", "08", "09" };

        //Stroke Words : which strokes bring a row in, by its place from the back (DYNAMICS)
        private static string[] tierWords = { "EVERY STROKE", "MIDDLE AND BIG", "BIG ONLY" };
        private static string[] tierMarks = { "p", "mf", "f" };
        private const string EmptyRow = "EMPTY, SKIPPED";
        private const string AllStrokes = "EVERY STROKE";

        //Call Words
        private static string[] kindWords = { "", "TREMOLO", "FERMATA", "HIDDEN" };
        private static Flick[] pattern = { Flick.Down, Flick.Left, Flick.Right, Flick.Up };
        private static string[] sideArrows = { "<  LEFT", "CENTRE", "RIGHT  >" };
        private static float[] sideX = { 0.18f, 0.5f, 0.82f };           // where each side's label sits across the stage

        //Stage Notes : what the detail box says when the mouse is over nothing
        private const string NoteTitle = "WHERE THEY SIT IS HOW THEY PLAY";
        private const string NoteRow = "ROW : a small stroke (p) brings the back row, middle (mf) the middle too, big (f) everyone.";
        private const string NoteSide = "SIDE : when the baton points at their side, they hit 50 percent harder (CUE).";
        private const string NoteView = "Front row: power x1.3, costs more.   Back row: power x0.8, costs less.";

        //Trait Seen : traits met at least once since the game started, so each one is
        //explained on its own the very first time
        private static bool[] traitSeen = new bool[16];
        private float traitIntro;

        //Formation State
        private bool battleMode;
        private Musician held;                 // the musician being dragged, or null
        private int heldFromSeat = -1;         // where they were picked up from, -1 is the list
        private int hoverSeat = -1;
        private int hoverCard = -1;
        private int hoverBeat = -1;            // a beat of the call strip under the mouse
        private float time;
        private float messageTimer;
        private string message = "";

        //Prepared Text : made in Load, never in Draw
        private string[] sideBeats = { "", "", "" };   // "BEATS 2  6" for each side, CUE
        private string roundLine = "";

        public FormationScreen(bool forBattle)
        {
            battleMode = forBattle;
        }

        public override void Load()
        {
            RunState run = Game.CurrentRun;
            SoundBank.PlayMusic(battleMode ? RunFlow.FightMusic(run) : Music.Route);
            run.Formation.Tidy(run.Seats);
            run.RefreshLabels();

            //Side Beats : which beats of the round the baton points at each side (CUE).
            //Outside a fight the usual ones, without anybody's trick.
            for (int side = 0; side < 3; side++)
            {
                string beats = "BEATS";
                for (int b = 0; b < BattleRules.BeatsPerRound; b++)
                {
                    int cued = battleMode ? run.Battle.CueSideAt(b) : BattleRules.CueSide[b % 4];
                    if (cued == side) beats += "  " + (b + 1);
                }
                sideBeats[side] = beats;
            }
            if (!battleMode) return;

            BattleState battle = run.Battle;
            roundLine = battle.RoundLabel + "     " + battle.TempoLabel + (battle.Passes > 1 ? "     " + battle.PassesLabel : "");

            //First Meeting : a trait nobody has seen yet explains itself
            int trait = (int)battle.Enemy.Trait;
            if (battle.TraitShown && !traitSeen[trait])
            {
                traitSeen[trait] = true;
                traitIntro = 5f;
            }
        }

        //Card Rect : roster cards fill the right box in rows of four.
        //More slots make the cards shorter, so a full bench still fits.
        private Rectangle CardRect(int i)
        {
            int slots = Game.CurrentRun.RosterCapacity;
            int rows = (slots + CardColumns - 1) / CardColumns;
            int top = rosterBox.Y + 48;
            int height = rosterBox.Bottom - top;
            int w = (rosterBox.Width - CardGap * (CardColumns - 1)) / CardColumns;
            int h = Math.Min(190, (height - CardGap * (rows - 1)) / rows);

            int col = i % CardColumns;
            int row = i / CardColumns;
            return new Rectangle(rosterBox.X + col * (w + CardGap), top + row * (h + CardGap), w, h);
        }

        private Rectangle SeatRect(int seat)
        {
            return StageLayout.SeatRect(seat, stageBox, SeatScale);
        }

        private Rectangle CallCell(int beat)
        {
            return new Rectangle(CallX + beat * CallW, callBox.Y, CallW, callBox.Height);
        }

        public override void Update(float dt)
        {
            time += dt;
            if (messageTimer > 0f) messageTimer -= dt;
            if (traitIntro > 0f) traitIntro -= dt;

            RunState run = Game.CurrentRun;
            Formation f = run.Formation;

            //THE BARGAIN : nothing else on the page works until the deal is answered
            if (battleMode && run.Battle.BargainOffered)
            {
                bool accept = Input.ClickedOn(acceptButton) || Input.KeyPressed(Keys.Y);
                bool refuse = Input.ClickedOn(refuseButton) || Input.KeyPressed(Keys.N);
                if (accept || refuse)
                {
                    run.Battle.AnswerBargain(accept);
                    SoundBank.Play(accept ? Sfx.MotifGet : Sfx.UiBack);
                }
                return;
            }

            FindHover(run);

            //Drag Start : press on a seated musician or on a card
            if (Input.MouseClicked() && held == null)
            {
                if (hoverSeat >= 0 && f.Seated[hoverSeat] != null)
                {
                    held = f.Seated[hoverSeat];
                    heldFromSeat = hoverSeat;
                    SoundBank.Play(Sfx.SeatPickUp);
                }
                else if (hoverCard >= 0)
                {
                    held = run.Roster[hoverCard];
                    heldFromSeat = f.SeatOf(held);
                    SoundBank.Play(Sfx.SeatPickUp);
                }
            }

            //Drag End : drop onto a seat, or onto the cards to bench
            if (Input.MouseReleased() && held != null)
            {
                Drop(run);
                held = null;
                heldFromSeat = -1;
                run.RefreshLabels();
            }

            //Quick Bench : right click a seated musician
            if (Input.MouseRightClicked() && held == null && hoverSeat >= 0 && f.Seated[hoverSeat] != null)
            {
                f.Remove(hoverSeat);
                SoundBank.Play(Sfx.SeatRemove);
                run.RefreshLabels();
            }

            //Primary Action
            bool next = Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Tab) || Input.ClickedOn(primaryButton);
            if (battleMode && Input.ClickedOn(Ui.StepRect(steps.Length, 1, 640, NavY, NavSpacing))) next = true;

            if (next && held == null) GoNext(run);
        }

        //Hover Find : which seat, card or beat of the call is under the mouse
        private void FindHover(RunState run)
        {
            hoverSeat = -1;
            hoverCard = -1;
            hoverBeat = -1;

            for (int s = 0; s < StageLayout.SeatCount; s++)
            {
                Rectangle r = SeatRect(s);
                r.Inflate(14, 14);                  // a little easier to drop onto
                if (Input.MouseOver(r)) hoverSeat = s;
            }

            for (int i = 0; i < run.Roster.Count; i++)
                if (Input.MouseOver(CardRect(i))) hoverCard = i;

            if (battleMode)
                for (int b = 0; b < BattleRules.BeatsPerRound; b++)
                    if (Input.MouseOver(CallCell(b))) hoverBeat = b;
        }

        //Drop : work out what the drag meant
        private void Drop(RunState run)
        {
            Formation f = run.Formation;

            if (hoverSeat >= 0)
            {
                if (!f.IsUnlocked(hoverSeat, run.Seats))
                {
                    Say("THAT SEAT IS STILL LOCKED");
                    SoundBank.Play(Sfx.UiDenied);
                    return;
                }

                f.Place(held, hoverSeat);
                SoundBank.Play(Sfx.SeatDrop);
                return;
            }

            //Dropped On The Cards : a seated musician goes back to the bench
            if (Input.MouseOver(rosterBox) && heldFromSeat >= 0)
            {
                f.Remove(heldFromSeat);
                SoundBank.Play(Sfx.SeatRemove);
            }
        }

        private void GoNext(RunState run)
        {
            if (!battleMode)
            {
                SoundBank.Play(Sfx.UiBack);
                Game.Screens.Change(new RouteScreen());
                return;
            }

            //Fight Guard : somebody has to be on stage
            if (run.Formation.SeatedCount == 0)
            {
                Say("SEAT AT LEAST ONE MUSICIAN");
                SoundBank.Play(Sfx.UiDenied);
                return;
            }

            SoundBank.Play(Sfx.UiConfirm);
            Game.Screens.Change(new DuelScreen());
        }

        private void Say(string text)
        {
            message = text;
            messageTimer = 1.8f;
        }

        public override void Draw(SpriteBatch sb)
        {
            RunState run = Game.CurrentRun;

            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageDeep);
            Gfx.DrawGlow(sb, stageBox.Center.X, stageBox.Y + 60, 480f, Palette.Paper * 0.08f);

            if (battleMode) DrawCall(sb, run.Battle);
            DrawStage(sb, run);
            DrawSeats(sb, run);
            DrawRoster(sb, run);
            DrawDetail(sb, run);

            Ui.Button(sb, primaryButton, battleMode ? "BEGIN ROUND" : "BACK TO ROUTE", battleMode ? "ENTER" : "TAB", true);

            //Top Bar and Steps
            RunHud.DrawTop(sb, run, battleMode ? run.Battle.EnemyTitle : "THE STAGE");
            if (battleMode)
            {
                Ui.StepNav(sb, steps, 0, 640, NavY, NavSpacing);
                Gfx.TextSpaced(sb, Game.Font, roundLine, 36, NavY - 8, Palette.PaperDim, TextSize.Tiny, 2f);
            }
            else Gfx.TextSpacedCentered(sb, Game.Font, "REARRANGE THE ENSEMBLE BETWEEN FIGHTS", 640, NavY - 4, Palette.LineGrey, TextSize.Tiny, 3f);

            //Held Musician : follows the mouse while dragging
            if (held != null)
            {
                Rectangle ghost = new Rectangle((int)Input.MousePos.X - 22, (int)Input.MousePos.Y - 56, 44, 112);
                Gfx.Rect(sb, ghost, Palette.Void * 0.6f);
                MusicianArt.Token(sb, ghost, held, 1f, 0.6f);
                Gfx.TextCentered(sb, Game.BigFont, held.NameTag, ghost.Center.X, ghost.Bottom + 14, Palette.Highlight, TextSize.Small);
            }

            RunHud.DrawTips(sb, run);

            if (battleMode)
            {
                //Trait Tooltip : while pointing at TACET's name, or on the first meeting
                BattleState battle = run.Battle;
                if (battle.TraitShown && (Input.MouseOver(enemyInfo) || traitIntro > 0f))
                {
                    float tx = Input.MouseOver(enemyInfo) ? Input.MousePos.X : enemyInfo.X + 60;
                    float ty = Input.MouseOver(enemyInfo) ? Input.MousePos.Y : enemyInfo.Bottom - 10;
                    Ui.Tooltip(sb, battle.Enemy.TraitName, battle.Enemy.TraitWrapped, tx, ty);
                }

                if (battle.BargainOffered) DrawBargain(sb);
            }
        }

        //Call : TACET's part for the coming round. Each beat shows the way the baton goes, how
        //hard TACET plays it and which side of the stage the baton points at (CUE). Its f, mf
        //and p are decided afresh every time through, so they are only shown in the duel.
        private void DrawCall(SpriteBatch sb, BattleState battle)
        {
            Enemy e = battle.Enemy;
            Gfx.Rect(sb, callBox, Palette.Void);
            Ornament.Stave(sb, CallX, callBox.Y + 20, CallW * 8, 7, Palette.Paper * 0.06f);
            Gfx.RectOutline(sb, callBox, Palette.LineGrey * 0.5f, 1);

            //Enemy Tile : the enemy's picture shrunk to a square, or an empty slot
            Rectangle tile = new Rectangle(callBox.X + 8, callBox.Y + 8, callBox.Height - 16, callBox.Height - 16);
            Gfx.Rect(sb, tile, Palette.Stage);
            ArtBank.DrawOrSlot(sb, ArtBank.EnemyOf(e), tile, Palette.Paper, 1f);

            //Name and Tag : the trait when it has one on this floor (point at it to read it),
            //otherwise what kind of fight this is
            Gfx.Text(sb, Game.BigFont, e.Name, tile.Right + 12, callBox.Y + 6, Palette.Highlight, TextSize.Small * 0.85f);
            if (battle.TraitShown)
            {
                Ui.Tag(sb, e.TraitName, tile.Right + 12, callBox.Y + 40, true, 1f);
                float tagEnd = tile.Right + 12 + Ui.TagWidth(e.TraitName);
                if (traitIntro > 0f || Input.MouseOver(enemyInfo)) Gfx.Diamond(sb, tagEnd + 10, callBox.Y + 49, 4, Palette.Highlight);
                else Gfx.TextSpaced(sb, Game.Font, "?", tagEnd + 8, callBox.Y + 43, Palette.PaperDim, TextSize.Tiny, 1f);
            }
            else
            {
                Ui.Tag(sb, e.KindLabel, tile.Right + 12, callBox.Y + 40, e.Kind != EnemyKind.Normal, 1f);
            }

            for (int b = 0; b < BattleRules.BeatsPerRound; b++)
            {
                Rectangle cell = CallCell(b);
                float cx = cell.Center.X;
                bool hover = b == hoverBeat;
                if (hover) Gfx.Rect(sb, cell, Palette.Paper * 0.06f);

                //Bar Line : every fourth beat, the way written music is divided
                if (b % 4 == 0) Gfx.Rect(sb, cell.X, cell.Y + 4, 1, cell.Height - 8, Palette.LineGrey * (b == 0 ? 0.3f : 0.7f));

                //Beat Number and Way : the 4/4 pattern, down, left, right, up
                Color head = hover ? Palette.Highlight : Palette.LineGrey;
                Gfx.TextCentered(sb, Game.Font, NumberText.Get(b + 1), cx - 8, cell.Y + 10, head, TextSize.Label);
                DrawWay(sb, pattern[b % 4], cx + 10, cell.Y + 10, head);

                //Note : TACET's written strength, a rest mark where it is silent
                float ny = cell.Y + 34;
                if (battle.EnemyHidden[b])
                {
                    Gfx.TextCentered(sb, Game.BigFont, "???", cx, ny - 2, Palette.Paper, TextSize.Small);
                }
                else if (battle.EnemyPower[b] <= 0)
                {
                    Gfx.Rect(sb, cx - 12, ny - 3, 24, 6, Palette.LineGrey * 0.6f);
                    Gfx.TextSpacedCentered(sb, Game.Font, "REST", cx, ny + 6, Palette.LineGrey, TextSize.Tiny, 2f);
                }
                else
                {
                    int power = battle.EnemyStrikeAt(b);
                    bool heavy = battle.IsHeavy(b) || battle.IsTremolo(b) || battle.IsFermata(b);
                    float radius = Math.Min(11f, 4f + power * 0.6f);
                    float nx = cx - 18f;
                    Color ink = heavy ? Palette.Highlight : Palette.PaperDim;
                    Gfx.Circle(sb, nx, ny, radius, ink);
                    Gfx.Rect(sb, nx + radius - 2, ny - radius * 2f, 2, radius * 2f, ink);
                    if (battle.IsTremolo(b))
                        for (int z = 0; z < 3; z++)
                            Gfx.Line(sb, nx - 10 + z * 7, ny + 12 + (z % 2) * 3, nx - 3 + z * 7, ny + 15 - (z % 2) * 3, Palette.Highlight, 1.5f);
                    if (battle.IsFermata(b)) NoteGlyph.FermataSign(sb, nx, ny - radius - 10, 7f, Palette.Highlight);
                    Gfx.Text(sb, Game.BigFont, NumberText.Get(power), cx + 2, ny - 13, heavy ? Palette.Highlight : Palette.Paper, TextSize.Small * 0.85f);
                }

                //Cue : which side of the stage the baton points at on this beat
                int side = battle.CueSideAt(b);
                Gfx.TextSpacedCentered(sb, Game.Font, sideArrows[side], cx, cell.Bottom - 16, hover ? Palette.Highlight : Palette.PaperDim, TextSize.Tiny, 1.5f);
            }
        }

        //Way : a small arrow for the baton pattern
        private void DrawWay(SpriteBatch sb, Flick way, float cx, float cy, Color color)
        {
            if (way == Flick.Down) Gfx.Triangle(sb, cx, cy, 5, false, color);
            else if (way == Flick.Left) Gfx.Arrow(sb, cx, cy, 5, false, color);
            else if (way == Flick.Right) Gfx.Arrow(sb, cx, cy, 5, true, color);
            else Gfx.Triangle(sb, cx, cy, 5, true, color);
        }

        //Bargain : the devil's offer, before round two
        private void DrawBargain(SpriteBatch sb)
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

        //Stage : a curtain valance, three risers seen from the hall, and the podium.
        //In battle mode each row says which strokes bring it in, each side which beats the baton
        //points at it, and a side SILENT MOUTHS has silenced this round is shaded over.
        private void DrawStage(SpriteBatch sb, RunState run)
        {
            BattleState battle = battleMode ? run.Battle : null;

            //Valance : a thin curtain rail with small scallops hanging from it
            int valY = stageBox.Y - 10;
            Gfx.Rect(sb, stageBox.X - 20, valY, stageBox.Width + 40, 2, Palette.PaperDim * 0.5f);
            for (int i = 0; i < 20; i++)
                Gfx.Arc(sb, stageBox.X - 20 + 15 + i * 30f, valY + 1, 15, 0f, MathHelper.Pi, Palette.PaperDim * 0.25f, 1.5f);

            for (int r = 0; r < StageLayout.Rows.Length; r++)
            {
                int top = stageBox.Y + (int)(tierTop[r] * stageBox.Height);
                int bottom = stageBox.Y + (int)(tierBottom[r] * stageBox.Height);
                Color tone = Color.Lerp(Palette.StageDeep, Palette.Stage, 0.45f + r * 0.28f);

                //Tier : a trapezoid, drawn two rows at a time
                for (int y = top; y < bottom; y += 2)
                {
                    float t = (float)(y - top) / (bottom - top);
                    float inset = stageBox.Width * (0.10f - r * 0.035f) * (1f - t * 0.3f);
                    Gfx.Rect(sb, stageBox.X + inset, y, stageBox.Width - inset * 2f, 2, tone);
                }

                //Riser Lip : the edge of the step, lit
                Gfx.Rect(sb, stageBox.X + 10, bottom - 2, stageBox.Width - 20, 2, Palette.PaperDim * 0.5f);

                //Row Label : name, and the strokes that bring it in (DYNAMICS)
                StageRow row = StageLayout.Rows[r];
                int labelY = (top + bottom) / 2 - 20;
                Gfx.Rect(sb, 36, labelY + 2, 2, 36, Palette.Paper * (0.4f + r * 0.3f));
                Gfx.TextSpaced(sb, Game.Font, row.Name, 48, labelY, Palette.Paper, TextSize.Body, 3f);
                if (battle == null)
                {
                    Gfx.Text(sb, Game.StoryFont, rowShort[r], 48, labelY + 20, Palette.PaperDim, TextSize.StorySmall);
                    continue;
                }

                int tier = battle.RowTier(r);
                if (tier < 0) Gfx.TextSpaced(sb, Game.Font, EmptyRow, 48, labelY + 24, Palette.LineGrey, TextSize.Tiny, 1.5f);
                else
                {
                    Gfx.Text(sb, Game.BigFont, tierMarks[battle.ChoicesLocked ? 0 : tier], 48, labelY + 14, Palette.Highlight, TextSize.Small);
                    Gfx.TextSpaced(sb, Game.Font, battle.ChoicesLocked ? AllStrokes : tierWords[tier], 84, labelY + 24, Palette.PaperDim, TextSize.Tiny, 1.5f);
                }
            }

            //Sides : left, centre and right, and the beats the baton points at each (CUE).
            //While the mouse is on a beat of the call strip, its side lights up.
            for (int side = 0; side < 3; side++)
            {
                float x = stageBox.X + stageBox.Width * sideX[side];
                bool lit = battle != null && hoverBeat >= 0 && battle.CueSideAt(hoverBeat) == side;
                bool silenced = battle != null && battle.SilencedSide == side;
                Color ink = lit ? Palette.Highlight : Palette.PaperDim;
                Gfx.TextSpacedCentered(sb, Game.Font, StageLayout.SideNames[side], x, stageBox.Y - 42, ink, TextSize.Tiny, 3f);
                Gfx.TextSpacedCentered(sb, Game.Font, silenced ? "SILENCED" : sideBeats[side], x, stageBox.Y - 28, silenced ? Palette.Accent : Palette.LineGrey, TextSize.Tiny, 1.5f);
                if (lit) Gfx.DrawGlow(sb, x, stageBox.Center.Y, 140f, Palette.Paper * 0.08f);
            }
            if (battle != null && battle.Mirrored)
                Gfx.TextSpacedRight(sb, Game.Font, "MIRRORED", stageBox.Right + 20, stageBox.Y - 42, Palette.Accent, TextSize.Tiny, 2f);

            //Podium : where the conductor stands, placeholder only
            float px = stageBox.Center.X;
            float py = stageBox.Bottom - 4;
            Gfx.Rect(sb, px - 50, py - 8, 100, 10, Palette.PaperDim * 0.6f);
            Gfx.Diamond(sb, px, py - 16, 5, Palette.Paper);
            Gfx.TextSpacedCentered(sb, Game.Font, "CONDUCTOR", px, py + 6, Palette.LineGrey, TextSize.Tiny, 3f);
        }

        //Seats : locked, empty or filled, back row first so the front row overlaps it.
        //A filled seat shows the musician's pixel sprite, or an empty slot until it exists.
        private void DrawSeats(SpriteBatch sb, RunState run)
        {
            Formation f = run.Formation;
            BattleState battle = battleMode ? run.Battle : null;

            for (int s = 0; s < StageLayout.SeatCount; s++)
            {
                Rectangle r = SeatRect(s);
                bool unlocked = f.IsUnlocked(s, run.Seats);
                bool hover = (s == hoverSeat);
                Musician m = f.Seated[s];

                //Seat Number : above every seat, like numbered slots on a poster
                Color numberColor = unlocked ? Palette.PaperDim : Palette.LineGrey * 0.4f;
                Gfx.TextRight(sb, Game.Font, seatNumbers[s], r.X - 6, r.Y + 4, numberColor, TextSize.Tiny);

                if (!unlocked)
                {
                    //Locked Seat
                    Gfx.RectOutline(sb, r, Palette.LineGrey * 0.3f, 1);
                    DrawLock(sb, r.Center.X, r.Center.Y, Palette.LineGrey * 0.45f);
                    continue;
                }

                //Cue Light : the seats the hovered beat of the call points at
                if (battle != null && hoverBeat >= 0 && StageLayout.SeatSide(s) == battle.CueSideAt(hoverBeat))
                    Gfx.DrawGlow(sb, r.Center.X, r.Center.Y, r.Height * 0.8f, Palette.Highlight * 0.25f);

                if (m == null || (m == held && heldFromSeat == s))
                {
                    //Empty Seat : a hollow slot with a plus, brighter while something hovers over it
                    Color line = hover && held != null ? Palette.Highlight : Palette.LineGrey;
                    if (hover && held != null) Gfx.DrawGlow(sb, r.Center.X, r.Center.Y, r.Height * 0.7f, Palette.Paper * 0.2f);
                    Gfx.RectOutline(sb, r, line, 1);
                    Gfx.Rect(sb, r.Center.X - 8, r.Center.Y - 1, 16, 2, line);
                    Gfx.Rect(sb, r.Center.X - 1, r.Center.Y - 8, 2, 16, line);
                    continue;
                }

                //Filled Seat : the musician standing in their seat
                float lit = hover ? 0.5f : 0f;
                MusicianArt.Token(sb, r, m, 1f, lit);
                if (hover) Gfx.RectOutline(sb, r, Palette.Highlight, 2);

                //SILENT MOUTHS : a silenced player is shaded over for the round
                if (battle != null && !battle.CanPlay(s)) Gfx.Rect(sb, r, Color.Black * 0.6f);

                //Name Band : across the foot of the capsule, so it never reaches the row below
                float nameW = Gfx.TextWidth(Game.BigFont, m.NameTag, TextSize.Small * 0.8f) + 14f;
                Rectangle band = new Rectangle((int)(r.Center.X - nameW / 2f), r.Bottom - 14, (int)nameW, 20);
                Gfx.Rect(sb, band, Palette.Void * 0.9f);
                Gfx.Rect(sb, band.X, band.Y, band.Width, 1, Palette.PaperDim);
                Gfx.TextCentered(sb, Game.BigFont, m.NameTag, band.Center.X, band.Center.Y, Palette.Highlight, TextSize.Small * 0.8f);
            }
        }

        //Lock Glyph : a padlock from a ring and a box
        private void DrawLock(SpriteBatch sb, float cx, float cy, Color color)
        {
            Gfx.Arc(sb, cx, cy - 6, 7, MathHelper.Pi, MathHelper.TwoPi, color, 2f);
            Gfx.Rect(sb, cx - 9, cy - 6, 18, 14, color);
        }

        //Roster : every musician owned as a card, then empty slots with a plus
        private void DrawRoster(SpriteBatch sb, RunState run)
        {
            Ui.Header(sb, "", run.RosterLabel, rosterBox.X, rosterBox.Y - 4, rosterBox.Width, 1f);
            Gfx.TextSpacedRight(sb, Game.Font, "DRAG TO A SEAT  /  RIGHT CLICK TO BENCH", rosterBox.Right, rosterBox.Y + 4, Palette.LineGrey, TextSize.Tiny, 1.5f);

            for (int i = 0; i < run.Roster.Count; i++)
            {
                Musician m = run.Roster[i];
                int seat = run.Formation.SeatOf(m);
                string where = seat >= 0 ? StageLayout.RowOf(seat).Name : "BENCH";
                bool hover = (i == hoverCard) && held == null;
                MusicianArt.Card(sb, CardRect(i), m, run, hover, m == held, where);
            }

            //Empty Slots : the plus cards from the reference
            for (int i = run.Roster.Count; i < run.RosterCapacity; i++)
            {
                Rectangle slot = CardRect(i);
                Gfx.RectOutline(sb, slot, Palette.LineGrey * 0.35f, 1);
                Gfx.Rect(sb, slot.Center.X - 10, slot.Center.Y, 21, 1, Palette.LineGrey * 0.6f);
                Gfx.Rect(sb, slot.Center.X, slot.Center.Y - 10, 1, 21, Palette.LineGrey * 0.6f);
            }

            //Bench Drop Hint : while dragging a seated musician
            if (held != null && heldFromSeat >= 0 && Input.MouseOver(rosterBox))
            {
                Rectangle hint = rosterBox;
                hint.Inflate(8, 8);
                Ornament.CornerBrackets(sb, hint, 20, Palette.Highlight, 2);
                Gfx.TextSpacedCentered(sb, Game.Font, "RELEASE TO BENCH", rosterBox.Center.X, rosterBox.Bottom + 4, Palette.Highlight, TextSize.Tiny, 3f);
            }
        }

        //Detail : whatever the mouse is over, explained
        private void DrawDetail(SpriteBatch sb, RunState run)
        {
            Ui.Panel(sb, detailBox, 1f);

            Musician m = held;
            if (m == null && hoverCard >= 0) m = run.Roster[hoverCard];
            if (m == null && hoverSeat >= 0) m = run.Formation.Seated[hoverSeat];

            int x = detailBox.X + 24;
            int y = detailBox.Y + 14;

            if (m != null)
            {
                MusicianArt.Tile(sb, new Rectangle(x, y, 80, 80), m, 1f);
                Gfx.Text(sb, Game.BigFont, m.NameTag, x + 100, y - 4, Palette.Highlight, TextSize.Subtitle);
                Gfx.TextSpaced(sb, Game.Font, m.Instrument, x + 102, y + 34, Palette.Paper, TextSize.Label, 2f);
                Gfx.TextSpaced(sb, Game.Font, m.FamilyLabel, x + 102, y + 54, Palette.LineGrey, TextSize.Tiny, 2f);

                //Numbers : the same power and cost as the cards, rehearsals and motifs included
                Gfx.TextSpaced(sb, Game.Font, "POWER", x + 300, y + 4, Palette.LineGrey, TextSize.Tiny, 2f);
                Gfx.Text(sb, Game.BigFont, NumberText.Get(run.PowerOf(m)), x + 300, y + 16, Palette.Paper, TextSize.Subtitle);
                Gfx.TextSpaced(sb, Game.Font, "COST", x + 370, y + 4, Palette.LineGrey, TextSize.Tiny, 2f);
                Gfx.Text(sb, Game.BigFont, NumberText.Get(run.CostOf(m)), x + 370, y + 16, Palette.Paper, TextSize.Subtitle);
                if (m.Rehearsed > 0) Ui.Pips(sb, x + 302, y + 66, m.Rehearsed, BattleRules.RehearseMax, 3, 10, 1f);

                //Trait : what this musician does that nobody else does
                Gfx.Rect(sb, x + 430, y, 1, 80, Palette.LineGrey * 0.5f);
                Ui.Tag(sb, m.TraitName, x + 446, y + 2, true, 1f);
                Gfx.Text(sb, Game.StoryFont, m.TraitWrapped, x + 446, y + 30, Palette.Paper, TextSize.StorySmall);
            }
            else if (hoverSeat >= 0)
            {
                StageRow row = StageLayout.RowOf(hoverSeat);
                bool open = run.Formation.IsUnlocked(hoverSeat, run.Seats);
                Gfx.Text(sb, Game.BigFont, row.Title, x, y, Palette.Highlight, TextSize.Subtitle);
                Gfx.TextSpaced(sb, Game.Font, row.Effect, x, y + 44, Palette.Paper, TextSize.Label, 2f);
                Gfx.TextSpaced(sb, Game.Font, StageLayout.SideNames[StageLayout.SeatSide(hoverSeat)], x, y + 66, Palette.PaperDim, TextSize.Tiny, 2f);
                if (battleMode) Gfx.TextSpaced(sb, Game.Font, sideBeats[StageLayout.SeatSide(hoverSeat)], x + 110, y + 66, Palette.LineGrey, TextSize.Tiny, 2f);
                if (!open) Gfx.Text(sb, Game.StoryFont, "Locked. More seats can be bought in a shop.", x + 330, y + 60, Palette.PaperDim, TextSize.Story);
            }
            else
            {
                Gfx.TextSpaced(sb, Game.Font, battleMode ? NoteTitle : "STAGE NOTES", x, y + 4, Palette.LineGrey, TextSize.Tiny, 3f);
                if (battleMode)
                {
                    Gfx.Text(sb, Game.StoryFont, NoteRow, x, y + 26, Palette.Paper, TextSize.StorySmall);
                    Gfx.Text(sb, Game.StoryFont, NoteSide, x, y + 50, Palette.Paper, TextSize.StorySmall);
                }
                else
                {
                    Gfx.Text(sb, Game.StoryFont, "Point at a seat or a musician to read about it.", x, y + 24, Palette.Paper, TextSize.Story);
                    Gfx.Text(sb, Game.StoryFont, NoteView, x, y + 48, Palette.PaperDim, TextSize.StorySmall);
                }
            }

            //Message : short warnings, fading out
            if (messageTimer > 0f)
            {
                float a = Math.Min(1f, messageTimer);
                Gfx.TextSpacedRight(sb, Game.Font, message, primaryButton.Right, primaryButton.Y - 22, Palette.Accent * a, TextSize.Label, 2f);
            }
        }
    }
}
