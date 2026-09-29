using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //RecruitScreen : musicians from the era you just chose walk on and join the ensemble.
    //Shown right after a floor opening, and after an elite fight that brought someone home.
    //The band is named here too, but only on the very first floor of a run.
    public class RecruitScreen : GameScreen
    {
        //Recruit Layout
        private const int ColumnW = 440;
        private const int ColumnGap = 40;
        private Rectangle continueButton = new Rectangle(490, 646, 300, 48);

        //Recruit State
        private bool afterBattle;          // true when a fight or an event brought someone in
        private float time;
        private float enter;

        public RecruitScreen()
        {
            afterBattle = false;
        }

        public RecruitScreen(bool fromBattle)
        {
            afterBattle = fromBattle;
        }

        public override void Load()
        {
            if (Game.CurrentRun.JustJoined.Count > 0)
                SoundBank.Play(Sfx.Recruit);
        }

        public override void Update(float dt)
        {
            time += dt;

            //Enter Animation : let them walk on before the button works
            enter += dt * 1.8f;
            if (enter > 1f) enter = 1f;
            if (enter < 1f) return;

            if (Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Space) || Input.ClickedOn(continueButton))
            {
                SoundBank.Play(Sfx.UiConfirm);
                Continue();
            }
        }

        //Recruit Continue : where the player goes next
        private void Continue()
        {
            RunState run = Game.CurrentRun;

            //After Battle : the fight is already counted, just carry on routing
            if (afterBattle)
            {
                RunFlow.NextStage(Game);
                return;
            }

            //First Floor : name the band before anything else
            if (run.BandName.Length == 0)
            {
                Game.Screens.Change(new BandNameScreen());
                return;
            }

            run.LeaveOpening();
            Game.Screens.Change(new RouteScreen());
        }

        public override void Draw(SpriteBatch sb)
        {
            RunState run = Game.CurrentRun;
            float cx = TacetGame.ScreenW / 2f;
            float ease = enter * enter * (3f - 2f * enter);

            SceneBackdrop.Draw(sb, time);

            int count = run.JustJoined.Count;

            //Nobody Joined : the era ran out of people, or the ensemble is full
            if (count == 0)
            {
                Gfx.TextSpacedCentered(sb, Game.BigFont, "NOBODY STEPS FORWARD", cx, 280, Palette.Paper, TextSize.Subtitle, 4f);
                Ornament.Divider(sb, cx, 330, 160, Palette.LineGrey);

                string why = run.RosterFull
                    ? "Your ensemble and bench are both full."
                    : "This era has nobody left to give.";
                Gfx.TextCentered(sb, Game.StoryFont, why, cx, 360, Palette.PaperDim, TextSize.Story);

                DrawFooter(sb, run, 1f);
                return;
            }

            //Header
            string heading = count == 1 ? "A MUSICIAN JOINS" : "THE ENSEMBLE GATHERS";
            Gfx.TextSpacedCentered(sb, Game.BigFont, heading, cx, 24, Palette.Paper * ease, TextSize.Small, 5f);
            Ornament.Divider(sb, cx, 66, 200, Palette.LineGrey * ease);

            //Columns : one per new musician, centred as a group
            int totalW = count * ColumnW + (count - 1) * ColumnGap;
            int startX = (TacetGame.ScreenW - totalW) / 2;

            for (int i = 0; i < count; i++)
            {
                //Stagger : each one walks on a little after the one before
                float step = ease * 1.3f - i * 0.3f;
                if (step < 0f) step = 0f;
                if (step > 1f) step = 1f;

                Rectangle column = new Rectangle(startX + i * (ColumnW + ColumnGap), 84, ColumnW, 540);
                DrawColumn(sb, column, run.JustJoined[i], step, i);
            }

            DrawFooter(sb, run, ease);

            //Reveal Flash : a white burst the moment the page opens, like a summon
            float flash = 1f - time * 2.2f;
            if (flash > 0f) Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Color.White * (0.7f * flash));
        }

        //Column : light burst, figure, then the details stacked underneath
        private void DrawColumn(SpriteBatch sb, Rectangle column, Musician m, float a, int slot)
        {
            float cx = column.Center.X;
            float figureCy = column.Y + 160;

            //Light Burst : turning rays behind the figure
            Gfx.DrawGlow(sb, cx, figureCy, 230f, Palette.Paper * (0.14f * a));
            Ornament.Rays(sb, cx, figureCy, 70f, 230f, 30, time * 0.15f + slot, Palette.Paper * (0.10f * a));
            Gfx.CircleOutline(sb, cx, figureCy, 150f, Palette.Paper * (0.12f * a), 1f);

            //Figure : rises into place as it appears
            Rectangle figure = new Rectangle((int)cx - 80, column.Y + 40 + (int)((1f - a) * 40f), 160, 236);
            MusicianArt.Figure(sb, figure, m, a);

            //New Tag and Instrument : printed up the side of the figure
            Ui.Tag(sb, "NEW", column.X + 40, column.Y + 40, true, a);

            //Seat : their section and the side of the stage they sit on, under the tag (round 15)
            MusicianArt.SeatBadge(sb, m, column.X + 62, column.Y + 108, 18f, Palette.Ink, Palette.Paper, a);
            Gfx.TextSpacedCentered(sb, Game.Font, "SEAT", column.X + 62, column.Y + 146, Palette.LineGrey * a, TextSize.Tiny, 2f);
            Gfx.TextSpacedCentered(sb, Game.Font, StageLayout.Rows[StageLayout.SectionOf(m.Family)].Name, column.X + 62, column.Y + 164, Palette.PaperDim * a, TextSize.Tiny, 1f);
            Gfx.TextSpacedCentered(sb, Game.Font, StageLayout.SideNames[m.Cue], column.X + 62, column.Y + 180, Palette.PaperDim * a, TextSize.Tiny, 1f);
            Gfx.TextVertical(sb, Game.Font, m.Instrument, column.Right - 40, column.Y + 50, Palette.PaperDim * a, TextSize.Label);
            Gfx.Rect(sb, column.Right - 60, column.Y + 40, 1, 200, Palette.LineGrey * a);

            //Name : the big serif name with its slash
            int y = column.Y + 282;
            Gfx.TextCentered(sb, Game.BigFont, m.NameTag, cx, y + 20, Palette.Highlight * a, TextSize.Hero);
            Gfx.TextSpacedCentered(sb, Game.Font, m.FamilyLabel, cx, y + 54, Palette.PaperDim * a, TextSize.Tiny, 3f);

            //Stats : their power, the same number every page shows (round 15 : there is no cost)
            RunState run = Game.CurrentRun;
            Gfx.TextSpacedRight(sb, Game.Font, "POWER", cx - 6, y + 84, Palette.LineGrey * a, TextSize.Tiny, 2f);
            Gfx.Text(sb, Game.BigFont, NumberText.Get(run.PowerOf(m)), cx + 2, y + 70, Palette.Paper * a, TextSize.Subtitle);

            Ornament.Divider(sb, cx, y + 116, 150, Palette.LineGrey * a);
            Gfx.Text(sb, Game.StoryFont, m.LineWrapped, cx - TacetGame.MusicianWrapWidth / 2f, y + 128, Palette.Paper * a, TextSize.Story);

            //Trait : what they do in a duel, under the line it comes from
            float tx = cx - TacetGame.MusicianWrapWidth / 2f;
            Ui.Tag(sb, m.TraitName, tx, y + 190, true, a);
            Gfx.Text(sb, Game.StoryFont, m.TraitWrapped, tx, y + 216, Palette.PaperDim * a, TextSize.StorySmall);

            //Where They Went : on stage or waiting on the bench, printed up the side
            bool seated = run.Formation.SeatOf(m) >= 0;
            string where = seated ? "ON STAGE" : "ON THE BENCH";
            Gfx.TextSpacedRight(sb, Game.Font, where, column.Right - 40, column.Y + 12, Palette.LineGrey * a, TextSize.Tiny, 3f);
        }

        private void DrawFooter(SpriteBatch sb, RunState run, float a)
        {
            Gfx.TextSpaced(sb, Game.Font, run.RosterLabel, 40, 660, Palette.PaperDim * a, TextSize.Tiny, 3f);
            Gfx.TextSpaced(sb, Game.Font, run.SeatsLabel, 40, 680, Palette.PaperDim * a, TextSize.Tiny, 3f);

            Ui.Button(sb, continueButton, "CONTINUE", "ENTER", true, enter >= 1f, a);
        }
    }
}
