using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //BandScreen : THE BAND, opened from the route page (round 14, it replaces the STAGE page).
    //
    //There is nothing to arrange before a fight any more. Every musician plays a PART that is
    //written on their card like a note in the lane (StageLayout, MusicianArt.PartBadge):
    //   the LETTER  p, mf or f, set by the instrument: which strokes bring them in
    //   the ARROW   left, down and up, or right: which way of the baton makes them hit harder
    //The only choice here is WHO PLAYS. The stage has room for RunState.Seats players, the rest
    //wait on the bench. Click a musician to move them on or off the stage. With the stage full,
    //clicking someone on the bench picks them, and the next click on a player swaps the two.
    //
    //Left  : THE ORCHESTRA, a chart of the nine parts. Everyone sits in their own chair, so the
    //        chart fills itself: the rows are the letters, from p near the conductor to f at the
    //        back (the bigger the stroke, the further it reaches), the columns are the arrows.
    //        A chair nobody plays yet shows its part faintly, a part to look for.
    //Right : everyone in the ensemble as portrait cards, on stage or on the bench.
    public class BandScreen : GameScreen
    {
        //Band Layout
        private Rectangle rosterBox = new Rectangle(770, 176, 476, 406);
        private Rectangle detailBox = new Rectangle(36, 604, 880, 100);
        private Rectangle primaryButton = new Rectangle(950, 628, 290, 56);
        private const int CardGap = 10;
        private const int CardColumns = 4;

        //Orchestra Chart : one column per arrow, one row per letter
        private static float[] columnX = { 380f, 510f, 640f };        // left, down and up, right
        private static float[] partY = { 492f, 374f, 256f };          // p, mf, f : p nearest the conductor
        private const float ChartTop = 130f;
        private const float LabelX = 48f;
        private const int ChairW = 54;
        private const int ChairH = 104;
        private const float PodiumY = 584f;

        //Chart Words
        private static string[] rowMarks = { "p", "mf", "f" };
        private static string[] missingWords = { "NO p PLAYER : A SMALL STROKE BRINGS YOUR QUIETEST PART",
                                                 "NO mf PLAYER : A MIDDLE STROKE BRINGS ONLY THE p PLAYERS",
                                                 "NO f PLAYER : A BIG STROKE ADDS NOBODY" };
        private static string[] missingShort = { "NOBODY PLAYS p YET", "NOBODY PLAYS mf YET", "NOBODY PLAYS f YET" };

        //Notes : what the detail box says when the mouse is over nothing
        private const string NoteTitle = "EVERY PLAYER'S PART IS WRITTEN ON THEM, LIKE A NOTE";
        private const string NoteLetter = "LETTER : a small stroke brings the p players, a middle one the mf players too, a big one everybody.";
        private const string NoteArrow = "ARROW : when the baton goes their way, they hit 50 percent harder (CUE).";
        private const string NoteClick = "Click a musician to move them on or off the stage.";

        //Band State
        private Musician picked;               // a bench player waiting to swap in, or null
        private int hoverSeat = -1;            // a chair under the mouse, -1 for none
        private int hoverCard = -1;            // a card under the mouse, -1 for none
        private float time;
        private float messageTimer;
        private string message = "";

        //Prepared Text : made in Load and whenever the band changes, never in Draw
        private string[] sideBeats = { "", "", "" };   // "BEATS 2  6" for each arrow, CUE
        private bool[] partMissing = new bool[3];
        private string gapLine = "";                  // the first missing part, said in full

        public override void Load()
        {
            RunState run = Game.CurrentRun;
            SoundBank.PlayMusic(Music.Route);
            run.Formation.Tidy(run.Seats);
            run.RefreshLabels();

            //Side Beats : which beats of a round the baton goes each way (CUE)
            for (int side = 0; side < 3; side++)
            {
                string beats = "BEATS";
                for (int b = 0; b < BattleRules.BeatsPerRound; b++)
                    if (BattleRules.CueSide[b % 4] == side) beats += "  " + (b + 1);
                sideBeats[side] = beats;
            }

            Rebuild(run);
        }

        //Rebuild : the labels that follow the band, after every change
        private void Rebuild(RunState run)
        {
            run.RefreshLabels();
            gapLine = "";
            for (int part = 0; part < 3; part++)
            {
                partMissing[part] = true;
                for (int side = 0; side < 3; side++)
                    if (run.Formation.Seated[part * 3 + side] != null) partMissing[part] = false;
                if (partMissing[part] && gapLine.Length == 0 && run.Formation.SeatedCount > 0) gapLine = missingWords[part];
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

        //Chair Rect : where a part's chair stands on the chart
        private static Rectangle ChairRect(int seat)
        {
            float cx = columnX[StageLayout.SeatSide(seat)];
            float cy = partY[StageLayout.SeatRow[seat]];
            return new Rectangle((int)(cx - ChairW / 2f), (int)(cy - ChairH / 2f), ChairW, ChairH);
        }

        //Chair Owner : the musician of the ensemble whose home is this chair, on stage or not
        private Musician ChairOwner(RunState run, int seat)
        {
            for (int i = 0; i < run.Roster.Count; i++)
                if (StageLayout.HomeSeat(run.Roster[i]) == seat) return run.Roster[i];
            return null;
        }

        public override void Update(float dt)
        {
            time += dt;
            if (messageTimer > 0f) messageTimer -= dt;

            RunState run = Game.CurrentRun;
            FindHover(run);

            Musician under = null;
            if (hoverCard >= 0) under = run.Roster[hoverCard];
            else if (hoverSeat >= 0) under = ChairOwner(run, hoverSeat);

            if (Input.MouseClicked() && under != null) Choose(run, under);

            //Cancel : a right click puts a picked player back down
            if (Input.MouseRightClicked() && picked != null)
            {
                picked = null;
                SoundBank.Play(Sfx.UiBack);
            }

            bool back = Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Tab) || Input.ClickedOn(primaryButton);
            if (back)
            {
                SoundBank.Play(Sfx.UiBack);
                Game.Screens.Change(new RouteScreen());
            }
        }

        //Hover Find : which chair or card is under the mouse
        private void FindHover(RunState run)
        {
            hoverSeat = -1;
            hoverCard = -1;

            for (int s = 0; s < StageLayout.SeatCount; s++)
            {
                Rectangle r = ChairRect(s);
                r.Inflate(10, 10);
                if (Input.MouseOver(r)) hoverSeat = s;
            }

            for (int i = 0; i < run.Roster.Count; i++)
                if (Input.MouseOver(CardRect(i))) hoverCard = i;
        }

        //Choose : a click on a musician. On stage they go to the bench, from the bench they step
        //on while there is room. With the stage full, a bench player is picked and the next click
        //on someone playing swaps the two.
        private void Choose(RunState run, Musician m)
        {
            Formation f = run.Formation;
            bool playing = f.OnStage(m);

            if (picked != null && playing)
            {
                f.Swap(picked, m);
                picked = null;
                SoundBank.Play(Sfx.SeatDrop);
                Rebuild(run);
                return;
            }

            if (playing)
            {
                if (f.SeatedCount <= 1)
                {
                    Say("SOMEBODY HAS TO PLAY");
                    SoundBank.Play(Sfx.UiDenied);
                    return;
                }
                f.Bench(m);
                SoundBank.Play(Sfx.SeatRemove);
                Rebuild(run);
                return;
            }

            //From The Bench
            if (picked == m)
            {
                picked = null;
                SoundBank.Play(Sfx.UiBack);
                return;
            }
            if (f.AutoSeat(m, run.Seats))
            {
                picked = null;
                SoundBank.Play(Sfx.SeatDrop);
                Rebuild(run);
                return;
            }
            picked = m;
            Say("THE STAGE IS FULL : CLICK WHO SITS OUT");
            SoundBank.Play(Sfx.SeatPickUp);
        }

        private void Say(string text)
        {
            message = text;
            messageTimer = 2.2f;
        }

        public override void Draw(SpriteBatch sb)
        {
            RunState run = Game.CurrentRun;

            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageDeep);
            Gfx.DrawGlow(sb, columnX[1], partY[1], 420f, Palette.Paper * 0.07f);

            DrawChart(sb, run);
            DrawRoster(sb, run);
            DrawDetail(sb, run);

            Ui.Button(sb, primaryButton, "BACK TO ROUTE", "TAB", true);

            RunHud.DrawTop(sb, run, "THE BAND");
            Gfx.TextSpacedCentered(sb, Game.Font, "WHO PLAYS, AND WHO WAITS ON THE BENCH", 640, 90, Palette.LineGrey, TextSize.Tiny, 3f);
            RunHud.DrawTips(sb, run);
        }

        //Chart : the orchestra, its letters down the side and its arrows across the top
        private void DrawChart(SpriteBatch sb, RunState run)
        {
            //Arrows : one column each, with the beats of a round the baton goes that way
            for (int side = 0; side < 3; side++)
            {
                float x = columnX[side];
                DrawArrow(sb, side, x, ChartTop + 14);
                Gfx.TextSpacedCentered(sb, Game.Font, StageLayout.SideNames[side], x, ChartTop + 38, Palette.Paper, TextSize.Tiny, 2f);
                Gfx.TextSpacedCentered(sb, Game.Font, sideBeats[side], x, ChartTop + 54, Palette.LineGrey, TextSize.Tiny, 1.5f);
                Gfx.Rect(sb, x, ChartTop + 72, 1, PodiumY - ChartTop - 96, Palette.LineGrey * 0.18f);
            }

            //Letters : one row each, p nearest the conductor, f at the back
            for (int part = 0; part < 3; part++)
            {
                StageRow row = StageLayout.Rows[part];
                float y = partY[part];
                Gfx.Rect(sb, 250, y + ChairH / 2f + 8, 470, 1, Palette.LineGrey * 0.3f);
                Gfx.Text(sb, Game.BigFont, rowMarks[part], LabelX, y - 30, Palette.Highlight, TextSize.Subtitle);
                Gfx.TextSpaced(sb, Game.Font, row.Name, LabelX + 66, y - 22, Palette.Paper, TextSize.Label, 2f);
                Gfx.TextSpaced(sb, Game.Font, row.Strokes, LabelX + 66, y - 2, Palette.PaperDim, TextSize.Tiny, 1.5f);
                MusicianArt.FamilyGlyph(sb, row.Family, LabelX + 20, y + 30, 0.55f, Palette.LineGrey);
                if (partMissing[part])
                    Gfx.TextSpaced(sb, Game.Font, missingShort[part], LabelX + 66, y + 18, Palette.Accent, TextSize.Tiny, 1.5f);
            }

            //Chairs
            for (int s = 0; s < StageLayout.SeatCount; s++)
                DrawChair(sb, run, s);

            //Conductor : the podium every stroke starts from
            float px = columnX[1];
            Gfx.Rect(sb, px - 50, PodiumY - 4, 100, 8, Palette.PaperDim * 0.6f);
            Gfx.Diamond(sb, px, PodiumY - 14, 5, Palette.Paper);
            Gfx.TextSpacedCentered(sb, Game.Font, "CONDUCTOR", px, PodiumY + 8, Palette.LineGrey, TextSize.Tiny, 3f);
        }

        //Arrow : the way a column answers, a head for left and right, two for down and up
        private static void DrawArrow(SpriteBatch sb, int side, float x, float y)
        {
            if (side == 0) NoteGlyph.Arrow(sb, Flick.Left, x, y, 34f, 12f, Palette.Paper, 2f);
            else if (side == 2) NoteGlyph.Arrow(sb, Flick.Right, x, y, 34f, 12f, Palette.Paper, 2f);
            else
            {
                NoteGlyph.Arrow(sb, Flick.Down, x - 9, y, 24f, 10f, Palette.Paper, 2f);
                NoteGlyph.Arrow(sb, Flick.Up, x + 9, y, 24f, 10f, Palette.Paper, 2f);
            }
        }

        //Chair : a player on stage, a player on the bench (dim), or a part nobody plays yet
        private void DrawChair(SpriteBatch sb, RunState run, int s)
        {
            Rectangle r = ChairRect(s);
            Musician owner = ChairOwner(run, s);
            bool hover = s == hoverSeat && owner != null;

            if (owner == null)
            {
                //Empty Part : the chair's own badge, faint, a part still to find
                Gfx.CapsuleOutline(sb, r, Palette.LineGrey * 0.3f, 1f);
                MusicianArt.PartBadge(sb, StageLayout.SeatRow[s], StageLayout.SeatSide(s), r.Center.X, r.Center.Y, 12f,
                                      Palette.Ink, Palette.PaperDim, 0.35f);
                return;
            }

            bool playing = run.Formation.OnStage(owner);
            bool swapTarget = picked != null && playing;

            if (swapTarget) Gfx.DrawGlow(sb, r.Center.X, r.Center.Y, r.Height * 0.8f, Palette.Highlight * (0.18f + 0.1f * (float)Math.Sin(time * 6f)));
            MusicianArt.Token(sb, r, owner, playing ? 1f : 0.3f, hover ? 0.5f : 0f);
            if (hover || owner == picked) Gfx.RectOutline(sb, r, Palette.Highlight, 2);

            //Name Band : across the foot of the chair, BENCH over the top of it for a player sitting out
            float nameW = Gfx.TextWidth(Game.BigFont, owner.NameTag, TextSize.Small * 0.75f) + 14f;
            Rectangle band = new Rectangle((int)(r.Center.X - nameW / 2f), r.Bottom - 12, (int)nameW, 20);
            Gfx.Rect(sb, band, Palette.Void * 0.9f);
            Gfx.Rect(sb, band.X, band.Y, band.Width, 1, playing ? Palette.Paper : Palette.LineGrey);
            Gfx.TextCentered(sb, Game.BigFont, owner.NameTag, band.Center.X, band.Center.Y, playing ? Palette.Highlight : Palette.PaperDim, TextSize.Small * 0.75f);
            if (!playing)
            {
                Gfx.Rect(sb, r.X, r.Y + 6, r.Width, 16, Palette.Void * 0.85f);
                Gfx.TextSpacedCentered(sb, Game.Font, "BENCH", r.Center.X, r.Y + 8, Palette.PaperDim, TextSize.Tiny, 2f);
            }
        }

        //Roster : every musician owned as a card, then empty slots with a plus
        private void DrawRoster(SpriteBatch sb, RunState run)
        {
            Ui.Header(sb, "", run.RosterLabel, rosterBox.X, rosterBox.Y - 4, rosterBox.Width, 1f);
            Gfx.TextSpacedRight(sb, Game.Font, run.SeatsLabel, rosterBox.Right, rosterBox.Y + 4, Palette.PaperDim, TextSize.Tiny, 2f);

            for (int i = 0; i < run.Roster.Count; i++)
            {
                Musician m = run.Roster[i];
                bool playing = run.Formation.OnStage(m);
                Rectangle card = CardRect(i);
                if (picked != null && playing) Ornament.CornerBrackets(sb, new Rectangle(card.X - 4, card.Y - 4, card.Width + 8, card.Height + 8), 12, Palette.Highlight, 1);
                MusicianArt.Card(sb, card, m, run, i == hoverCard, false, playing ? "ON STAGE" : "BENCH");
                if (m == picked) Gfx.RectOutline(sb, card, Palette.Highlight, 3);
            }

            //Empty Slots : the plus cards from the reference
            for (int i = run.Roster.Count; i < run.RosterCapacity; i++)
            {
                Rectangle slot = CardRect(i);
                Gfx.RectOutline(sb, slot, Palette.LineGrey * 0.35f, 1);
                Gfx.Rect(sb, slot.Center.X - 10, slot.Center.Y, 21, 1, Palette.LineGrey * 0.6f);
                Gfx.Rect(sb, slot.Center.X, slot.Center.Y - 10, 1, 21, Palette.LineGrey * 0.6f);
            }
        }

        //Detail : whatever the mouse is over, explained
        private void DrawDetail(SpriteBatch sb, RunState run)
        {
            Ui.Panel(sb, detailBox, 1f);

            Musician m = null;
            if (hoverCard >= 0) m = run.Roster[hoverCard];
            else if (hoverSeat >= 0) m = ChairOwner(run, hoverSeat);

            int x = detailBox.X + 20;
            int y = detailBox.Y + 10;

            if (m != null)
            {
                MusicianArt.Tile(sb, new Rectangle(x, y, 80, 80), m, 1f);
                Gfx.Text(sb, Game.BigFont, m.NameTag, x + 96, y - 4, Palette.Highlight, TextSize.Subtitle);
                Gfx.TextSpaced(sb, Game.Font, m.Instrument, x + 98, y + 34, Palette.Paper, TextSize.Label, 2f);
                Gfx.TextSpaced(sb, Game.Font, m.FamilyLabel, x + 98, y + 56, Palette.LineGrey, TextSize.Tiny, 1.5f);

                //Part : the badge, and what its letter and arrow mean
                int part = StageLayout.PartOf(m.Family);
                MusicianArt.PartBadge(sb, m, x + 300, y + 30, 18f, Palette.Ink, Palette.Paper, 1f);
                Gfx.TextSpaced(sb, Game.Font, StageLayout.Rows[part].Strokes, x + 336, y + 10, Palette.Paper, TextSize.Tiny, 1.5f);
                Gfx.TextSpaced(sb, Game.Font, StageLayout.SideNames[m.Cue], x + 336, y + 28, Palette.Paper, TextSize.Tiny, 1.5f);
                Gfx.TextSpaced(sb, Game.Font, sideBeats[m.Cue], x + 336, y + 46, Palette.LineGrey, TextSize.Tiny, 1.5f);

                //Numbers : the same power and cost as the cards, rehearsals and motifs included
                Gfx.TextSpaced(sb, Game.Font, "POWER", x + 300, y + 66, Palette.LineGrey, TextSize.Tiny, 2f);
                Gfx.Text(sb, Game.Font, NumberText.Get(run.PowerOf(m)), x + 360, y + 62, Palette.Paper, TextSize.Body);
                Gfx.TextSpaced(sb, Game.Font, "COST", x + 392, y + 66, Palette.LineGrey, TextSize.Tiny, 2f);
                Gfx.Text(sb, Game.Font, NumberText.Get(run.CostOf(m)), x + 440, y + 62, Palette.Paper, TextSize.Body);

                //Trait : what this musician does that nobody else does
                Gfx.Rect(sb, x + 520, y, 1, 80, Palette.LineGrey * 0.5f);
                Ui.Tag(sb, m.TraitName, x + 534, y + 2, true, 1f);
                Gfx.Text(sb, Game.StoryFont, m.TraitWrapped, x + 534, y + 28, Palette.Paper, TextSize.StorySmall);
            }
            else
            {
                Gfx.TextSpaced(sb, Game.Font, NoteTitle, x, y + 2, Palette.LineGrey, TextSize.Tiny, 2f);
                Gfx.Text(sb, Game.StoryFont, NoteLetter, x, y + 20, Palette.Paper, TextSize.StorySmall);
                Gfx.Text(sb, Game.StoryFont, NoteArrow, x, y + 42, Palette.Paper, TextSize.StorySmall);
                if (gapLine.Length > 0) Gfx.TextSpaced(sb, Game.Font, gapLine, x, y + 68, Palette.Accent, TextSize.Tiny, 1.5f);
                else Gfx.Text(sb, Game.StoryFont, NoteClick, x, y + 64, Palette.PaperDim, TextSize.StorySmall);
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
