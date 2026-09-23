using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //FormationScreen : the stage. Step 1 of preparing a fight.
    //
    //Left  : the orchestra, three rows of numbered seats. Front hits harder, back costs less.
    //Right : everyone in the ensemble as portrait cards, on stage or on the bench.
    //Drag a card onto a seat to place them, drag a seated musician back onto the cards
    //(or right click them) to bench them. Dropping on someone swaps the two.
    //
    //Opened two ways:
    //   battle mode : before a fight, leads on to the score page
    //   view mode   : from the route page, just to rearrange, leads back to the route
    public class FormationScreen : GameScreen
    {
        //Formation Layout
        private Rectangle stageBox = new Rectangle(170, 150, 560, 420);
        private Rectangle rosterBox = new Rectangle(770, 150, 476, 420);
        private Rectangle detailBox = new Rectangle(36, 596, 880, 108);
        private Rectangle primaryButton = new Rectangle(950, 628, 290, 56);
        private const float SeatScale = 0.75f;
        private const int CardGap = 10;
        private const int CardColumns = 4;

        //Step Nav : the three steps of getting ready for a fight
        private static string[] steps = { "01  STAGE", "02  SCORE", "03  PERFORM" };
        private const float NavY = 94f;
        private const float NavSpacing = 150f;

        //Riser Shape : the top and bottom of each tier, from 0 to 1 down the stage box
        private static float[] tierTop = { 0.04f, 0.31f, 0.58f };
        private static float[] tierBottom = { 0.34f, 0.62f, 0.93f };
        private static string[] rowShort = { "COSTS LESS", "STEADY", "HITS HARDER" };
        private static string[] seatNumbers = { "01", "02", "03", "04", "05", "06", "07", "08", "09" };

        //Formation State
        private bool battleMode;
        private Musician held;                 // the musician being dragged, or null
        private int heldFromSeat = -1;         // where they were picked up from, -1 is the list
        private int hoverSeat = -1;
        private int hoverCard = -1;
        private float time;
        private float messageTimer;
        private string message = "";

        public FormationScreen(bool forBattle)
        {
            battleMode = forBattle;
        }

        public override void Load()
        {
            SoundBank.PlayMusic(battleMode ? RunFlow.FightMusic(Game.CurrentRun) : Music.Route);
            Game.CurrentRun.RefreshLabels();
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

        public override void Update(float dt)
        {
            time += dt;
            if (messageTimer > 0f) messageTimer -= dt;

            RunState run = Game.CurrentRun;
            Formation f = run.Formation;

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
            bool next = Input.KeyPressed(Keys.Enter) || Input.ClickedOn(primaryButton);
            if (battleMode && Input.KeyPressed(Keys.Tab)) next = true;
            if (battleMode && Input.ClickedOn(Ui.StepRect(steps.Length, 1, 640, NavY, NavSpacing))) next = true;
            if (!battleMode && (Input.KeyPressed(Keys.Escape) || Input.KeyPressed(Keys.Tab))) next = true;

            if (next && held == null) GoNext(run);
        }

        //Hover Find : which seat or card is under the mouse
        private void FindHover(RunState run)
        {
            hoverSeat = -1;
            hoverCard = -1;

            for (int s = 0; s < StageLayout.SeatCount; s++)
            {
                Rectangle r = SeatRect(s);
                r.Inflate(14, 14);                  // a little easier to drop onto
                if (Input.MouseOver(r)) hoverSeat = s;
            }

            for (int i = 0; i < run.Roster.Count; i++)
                if (Input.MouseOver(CardRect(i))) hoverCard = i;
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
            Game.Screens.Change(new ScoreScreen());
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

            DrawStage(sb);
            DrawSeats(sb, run);
            DrawRoster(sb, run);
            DrawDetail(sb, run);

            Ui.Button(sb, primaryButton, battleMode ? "NEXT  /  SCORE" : "BACK TO ROUTE", battleMode ? "TAB" : "ESC", true);

            //Top Bar and Steps
            RunHud.DrawTop(sb, run, battleMode ? run.Battle.EnemyTitle : "THE STAGE");
            if (battleMode) Ui.StepNav(sb, steps, 0, 640, NavY, NavSpacing);
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
        }

        //Stage : a curtain valance, three risers seen from the hall, and the podium
        private void DrawStage(SpriteBatch sb)
        {
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

                //Row Label : name and effect, in the column left of the stage
                StageRow row = StageLayout.Rows[r];
                int labelY = (top + bottom) / 2 - 20;
                Gfx.Rect(sb, 36, labelY + 2, 2, 36, Palette.Paper * (0.4f + r * 0.3f));
                Gfx.TextSpaced(sb, Game.Font, row.Name, 48, labelY, Palette.Paper, TextSize.Body, 3f);
                Gfx.Text(sb, Game.StoryFont, rowShort[r], 48, labelY + 20, Palette.PaperDim, TextSize.StorySmall);
            }

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
                if (!open) Gfx.Text(sb, Game.StoryFont, "Locked. More seats can be bought in a shop.", x, y + 64, Palette.PaperDim, TextSize.Story);
            }
            else
            {
                Gfx.TextSpaced(sb, Game.Font, "STAGE NOTES", x, y + 4, Palette.LineGrey, TextSize.Tiny, 3f);
                Gfx.Text(sb, Game.StoryFont, "Point at a seat or a musician to read about it.", x, y + 24, Palette.Paper, TextSize.Story);
                Gfx.Text(sb, Game.StoryFont, "Front row: power x1.3, costs more.   Back row: power x0.8, costs less.", x, y + 48, Palette.PaperDim, TextSize.StorySmall);
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
