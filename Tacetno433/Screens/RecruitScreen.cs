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
    //
    //Beta1 (29 Oct, docs/BETA1_DIRECTION.md 3.2) : the same tall slanted columns as the band page (Core/BandColumn.cs).
    //Each new face rises into its own beam, one after another, their colour flooding up the column as they arrive and
    //settling, then their trait and where they went (on stage or the bench) under the column.
    public class RecruitScreen : GameScreen
    {
        //Recruit Layout
        private const int ColumnW = 176;
        private const int ColumnGap = 120;
        private const int ColumnsTop = 120;
        private Rectangle continueButton = new Rectangle(490, 648, 300, 46);

        //Recruit State
        private bool afterBattle;          // true when a fight or an event brought someone in
        private float time;
        private UiMotion continueMotion = new UiMotion();
        private string heading = "";
        private string[] traitText = new string[0];

        public RecruitScreen()
        {
            afterBattle = false;
        }

        public RecruitScreen(bool fromBattle)
        {
            afterBattle = fromBattle;
        }

        //Wipe Word : the page's heading in the wipe on the way here, none when nobody joins (WIPE WORD)
        public override string WipeWord
        {
            get
            {
                int count = Game.CurrentRun == null ? 0 : Game.CurrentRun.JustJoined.Count;
                if (count == 0) return null;
                return count == 1 ? "A MUSICIAN JOINS" : "THE ENSEMBLE GATHERS";
            }
        }

        public override void Load()
        {
            RunState run = Game.CurrentRun;
            if (run.JustJoined.Count > 0) SoundBank.Play(Sfx.Recruit);
            heading = run.JustJoined.Count == 1 ? "A MUSICIAN JOINS" : "THE ENSEMBLE GATHERS";
            traitText = new string[run.JustJoined.Count];
            for (int i = 0; i < traitText.Length; i++)
                traitText[i] = Gfx.WrapText(Game.StoryFont, run.JustJoined[i].TraitText, ColumnW + 60, TextSize.StorySmall, 4);

            Lens.Ambient = 0.4f;
            Lens.SpotPower = 0f;
            Lens.Vignette = 0.55f;
        }

        //Ready : the button works once everybody has walked on
        private bool Ready
        {
            get { return time >= 0.5f + Game.CurrentRun.JustJoined.Count * 0.25f; }
        }

        public override void Update(float dt)
        {
            time += dt;
            continueMotion.Update(dt, Ready && Input.MouseOver(continueButton));
            if (!Ready) return;

            if (Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Space) || Input.ClickedOn(continueButton))
            {
                continueMotion.Press();
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

        //Column Rect : column i of the new faces, centred as a group
        private Rectangle ColumnRect(int i)
        {
            int count = Game.CurrentRun.JustJoined.Count;
            int total = count * ColumnW + (count - 1) * ColumnGap;
            return new Rectangle((TacetGame.ScreenW - total) / 2 + i * (ColumnW + ColumnGap), ColumnsTop, ColumnW, BandColumn.PictureH);
        }

        //Arrive : 0..1, how far new face i has walked on, one after another
        private float Arrive(int i)
        {
            return UiMotion.Between(time, 0.25f + i * 0.25f, 0.5f);
        }

        //Flood : their colour fills the column the moment they arrive, then settles to a glow at its foot
        private float Flood(int i)
        {
            float t = time - (0.25f + i * 0.25f);
            if (t < 0f) return 0f;
            return 0.55f + 0.45f * Math.Max(0f, 1f - t * 1.6f);
        }

        //Shown : column i this frame, rising into place
        private Rectangle Shown(int i)
        {
            Rectangle r = ColumnRect(i);
            r.Y += (int)Math.Round((1f - Arrive(i)) * 60f);
            return r;
        }

        //======================================================================================
        //The World (THE LENS) : a beam for every new face
        //======================================================================================

        public override bool HasWorld
        {
            get { return true; }
        }

        public override bool ShowsGrain
        {
            get { return false; }
        }

        public override void DrawWorld(SpriteBatch sb)
        {
            RunState run = Game.CurrentRun;
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageDeep);
            for (int i = 0; i < run.JustJoined.Count; i++)
            {
                float a = Arrive(i);
                if (a <= 0f) continue;
                Musician m = run.JustJoined[i];
                Rectangle r = Shown(i);
                Gfx.DrawGlowBox(sb, new Rectangle(r.X - 60, r.Bottom - 40, r.Width + 120, 90), BandColumn.SpotOf(m) * (0.18f * a));
                BandColumn.Picture(sb, r, UiArt.BustOf(m), BandColumn.SpotOf(m), Flood(i), a);
            }
        }

        //Lights : a beam from above on every new face
        public override void DrawLights(SpriteBatch sb)
        {
            RunState run = Game.CurrentRun;
            for (int i = 0; i < run.JustJoined.Count; i++)
            {
                float a = Arrive(i);
                if (a <= 0f) continue;
                Rectangle r = ColumnRect(i);
                for (float y = 0f; y < r.Bottom + 40; y += 4f)
                {
                    float half = 50f + (y / (r.Bottom + 40)) * 90f;
                    Gfx.Rect(sb, r.Center.X - half, y, half * 2f, 4f, Color.White * (0.32f * a));
                }
            }
        }

        //Glow : their colour at the foot of the column as they arrive (none with LESS FLASHING)
        public override void DrawGlow(SpriteBatch sb)
        {
            if (Settings.ReduceFlashing) return;
            RunState run = Game.CurrentRun;
            for (int i = 0; i < run.JustJoined.Count; i++)
            {
                float t = time - (0.25f + i * 0.25f);
                if (t < 0f || t > 0.7f) continue;
                Rectangle r = Shown(i);
                Gfx.DrawGlow(sb, r.Center.X, r.Bottom - 20, 160f, BandColumn.SpotOf(run.JustJoined[i]) * (0.6f * (1f - t / 0.7f)));
            }
        }

        //======================================================================================
        //The Words (crisp on top)
        //======================================================================================

        public override void Draw(SpriteBatch sb)
        {
            RunState run = Game.CurrentRun;
            float cx = TacetGame.ScreenW / 2f;
            int count = run.JustJoined.Count;

            //Nobody Joined : the era ran out of people, or the ensemble is full
            if (count == 0)
            {
                Gfx.TextSpacedCentered(sb, Game.BigFont, "NOBODY STEPS FORWARD", cx, 280, Palette.Paper, TextSize.Subtitle, 4f);
                Ornament.OrbitRule(sb, cx, 330, 160, Palette.LineGrey);
                string why = run.RosterFull ? "Your ensemble and bench are both full." : "This era has nobody left to give.";
                Gfx.TextCentered(sb, Game.StoryFont, why, cx, 360, Palette.PaperDim, TextSize.Story);
                DrawFooter(sb, run, 1f);
                return;
            }

            //Header
            float head = UiMotion.Between(time, 0.1f, BattleRules.UiEnterTime);
            Gfx.TextSpacedCentered(sb, Game.BigFont, heading, cx, 30, Palette.Paper * head, TextSize.Small, 5f);
            Ornament.OrbitRule(sb, cx, 74, 200, Palette.LineGrey * head);

            for (int i = 0; i < count; i++)
            {
                float a = Arrive(i);
                if (a <= 0f) continue;
                Musician m = run.JustJoined[i];
                Rectangle r = Shown(i);
                BandColumn.Words(sb, r, m.NameTag, m.Instrument, SkillList.First(m).Name, SkillList.First(m).Cost, SkillList.Second(m).Name, SkillList.Second(m).Cost, BandColumn.SpotOf(m), 1f, a);

                //New, and where they went : on stage or waiting on the bench
                Ui.Tag(sb, "NEW", r.X + BandColumn.Slant + 6, r.Y + 8, true, a);
                bool seated = run.Formation.OnStage(m);
                Gfx.TextSpacedRight(sb, Game.Font, seated ? "ON STAGE" : "ON THE BENCH", r.Right - 4, r.Y - 18, Palette.LineGrey * a, TextSize.Tiny, 3f);

                //Trait : what they do that nobody else does
                float ty = r.Bottom + BandColumn.WordsH + 6;
                float tx = r.X - 30;
                Ui.Tag(sb, m.TraitName, tx, ty, false, a);
                Gfx.Text(sb, Game.StoryFont, traitText[i], tx, ty + 24, Palette.PaperDim * a, TextSize.StorySmall);
            }

            DrawFooter(sb, run, head);
        }

        private void DrawFooter(SpriteBatch sb, RunState run, float a)
        {
            Gfx.TextSpaced(sb, Game.Font, run.RosterLabel, 40, 660, Palette.PaperDim * a, TextSize.Tiny, 3f);
            Gfx.TextSpaced(sb, Game.Font, run.SeatsLabel, 40, 680, Palette.PaperDim * a, TextSize.Tiny, 3f);
            Ui.Command(sb, continueButton, "CONTINUE", "ENTER", continueMotion, Ready ? a : a * 0.4f, false);
        }
    }
}
