using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //CurtainCallScreen : the end of a run, won or lost.
    //The band takes a bow, the run record is printed like a concert programme, and THE JOURNEY
    //under the bow shows every place the run went through, floor by floor, to look back on.
    //After this the run is thrown away and the game returns to the title.
    public class CurtainCallScreen : GameScreen
    {
        private Rectangle titleButton = new Rectangle(930, 636, 310, 54);
        private const int LedgerX = 760;
        private const int LedgerW = 460;

        //Journey Marks : one letter per kind of place, the same as the route's road (RouteNodeInfo.Marks)
        private static string[] journeyKey = { "E ENCOUNTER", "! ELITE", "B BOSS", "? UNKNOWN", "$ SHOP", "R REST", "X CROSSING" };

        private bool won;
        private RunState run;            // kept here, because Game.CurrentRun is cleared when we leave
        private string[] names = new string[7];
        private string[] values = new string[7];
        private string headline = "";
        private string subline = "";
        private float time;
        private float enter;

        public CurtainCallScreen(bool runWon)
        {
            won = runWon;
        }

        public override void Load()
        {
            run = Game.CurrentRun;
            SaveFile.DeleteRun();          // the run is over either way

            headline = won ? "BRAVO" : "SILENCE";
            subline = won ? "The last silence is broken. The music goes on." : "The music stopped here. It can always start again.";

            names[0] = "ENSEMBLE";        values[0] = run.BandName;
            names[1] = "CONDUCTOR";       values[1] = run.Conductor.Name;
            names[2] = "REACHED";         values[2] = "FLOOR " + run.Floor + "  /  " + run.StageLabel;
            names[3] = "DUELS WON";       values[3] = run.BattlesWon.ToString();
            names[4] = "PERFECT BEATS";   values[4] = run.PerfectsTotal.ToString();
            names[5] = "BEST COMBO";      values[5] = run.BestCombo.ToString();
            names[6] = "SHARDS LEFT";     values[6] = run.Shards.ToString();

            SoundBank.Play(won ? Sfx.RunComplete : Sfx.Defeat);
            SoundBank.PlayMusic(Music.Ending);
        }

        public override void Update(float dt)
        {
            time += dt;
            enter = Math.Min(1f, enter + dt * 1.2f);
            if (enter < 1f) return;

            if (Input.KeyPressed(Keys.Enter) || Input.ClickedOn(titleButton))
            {
                SoundBank.Play(Sfx.UiConfirm);
                SoundBank.StopMusic();
                Game.Screens.Change(new TitleScreen());
            }
        }

        //Screen Leave : the run is thrown away here, once the fade has finished and this page
        //is really gone. Clearing it inside Update would leave Draw with nothing to draw for
        //the few frames the fade is still running.
        public override void Leave()
        {
            //only throw away the run this page was showing, in case a new one has already started
            if (Game.CurrentRun == run) Game.CurrentRun = null;
        }

        public override void Draw(SpriteBatch sb)
        {
            float e = enter * enter * (3f - 2f * enter);

            //Background : a bright stage for a win, TACET's dark for a loss
            if (won)
            {
                Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageDeep);
                Gfx.DrawGlow(sb, 360, 300, 520f, Palette.Paper * 0.16f);
                Ornament.Rays(sb, 360, 200, 60f, 700f, 40, time * 0.02f, Palette.Paper * 0.05f);
            }
            else
            {
                Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageDeep);
                Gfx.DrawGlow(sb, 360, 300, 420f, Palette.Paper * 0.06f);
                TacetField.Draw(sb, 700f - e * 60f, time, 0.6f);
            }

            //Curtains : two drapes pulled to the sides
            DrawCurtain(sb, 0, e);
            DrawCurtain(sb, 1, e);

            //Headline
            Gfx.TextSpaced(sb, Game.Font, "CURTAIN CALL", 90, 44, Palette.PaperDim * e, TextSize.Label, 6f);
            Gfx.Text(sb, Game.LogoFont, headline, 84, 72, Palette.Highlight * e, TextSize.Logo * 0.85f);
            Gfx.Text(sb, Game.StoryFont, subline, 92, 170, Palette.Paper * e, TextSize.Story);

            DrawBow(sb, run, e);
            DrawJourney(sb, run, e);
            DrawLedger(sb, run, e);

            Ui.Button(sb, titleButton, "RETURN TO TITLE", "ENTER", true, enter >= 1f, e);
        }

        //Curtain : red velvet in the final game, grey folds for now
        private void DrawCurtain(SpriteBatch sb, int side, float e)
        {
            int w = (int)(70 - e * 30);
            for (int i = 0; i < w; i += 2)
            {
                float fold = (float)Math.Sin(i * 0.5f) * 0.5f + 0.5f;
                Color c = Color.Lerp(Palette.CurtainDark, Palette.Curtain, fold);
                int x = side == 0 ? i : TacetGame.ScreenW - 2 - i;
                Gfx.Rect(sb, x, 0, 2, TacetGame.ScreenH, c);
            }
        }

        //Journey : one small box per place, floors split by a gap. The place the run ended on
        //is crossed out. The key underneath says what each letter means.
        private void DrawJourney(SpriteBatch sb, RunState run, float e)
        {
            float x = 92;
            float y = 606;
            Gfx.TextSpaced(sb, Game.Font, "THE JOURNEY", x, y - 22, Palette.PaperDim * e, TextSize.Tiny, 4f);

            int floor = 1;
            for (int i = 0; i < run.Journey.Count; i++)
            {
                JourneyStop stop = run.Journey[i];
                if (stop.Floor != floor)
                {
                    floor = stop.Floor;
                    Gfx.Rect(sb, x + 3, y - 2, 1, 24, Palette.PaperDim * e);    // a floor line
                    x += 10;
                }

                Rectangle box = new Rectangle((int)x, (int)y, 18, 20);
                bool fight = stop.Type == NodeType.Battle || stop.Type == NodeType.Elite || stop.Type == NodeType.Boss;
                if (fight) Gfx.Rect(sb, box, Palette.Paper * (0.85f * e));
                Gfx.RectOutline(sb, box, Palette.Paper * (0.7f * e), 1);
                Gfx.TextCentered(sb, Game.Font, RouteNodeInfo.Marks[(int)stop.Type], box.Center.X, box.Center.Y,
                                 (fight ? Palette.Ink : Palette.Paper) * e, TextSize.Tiny);
                if (stop.Lost) Gfx.Line(sb, box.X - 3, box.Bottom + 3, box.Right + 3, box.Y - 3, Palette.Highlight * e, 2f);
                x += 22;
            }

            //Key : what the letters stand for
            float kx = 92;
            for (int i = 0; i < journeyKey.Length; i++)
            {
                Gfx.TextSpaced(sb, Game.Font, journeyKey[i], kx, y + 32, Palette.LineGrey * e, TextSize.Tiny, 1.5f);
                kx += Gfx.SpacedWidth(Game.Font, journeyKey[i], TextSize.Tiny, 1.5f) + 18;
            }
        }

        //Bow : the band standing in a row, bowing in turn, the conductor at the head of the line
        private void DrawBow(SpriteBatch sb, RunState run, float e)
        {
            int count = run.Roster.Count;
            float cx = 360;
            float floor = 520;
            int gap = count > 8 ? 42 : 56;
            float left = cx + 50 - (count - 1) * gap / 2f;

            Gfx.DrawGlowBox(sb, new Rectangle((int)cx - 320, (int)floor - 40, 640, 90), Palette.Paper * 0.10f);
            Gfx.Rect(sb, cx - 300, floor, 600, 1, Palette.PaperDim * e);

            for (int i = 0; i < count; i++)
            {
                //Bow Wave : each musician dips a little after the one before
                float dip = Math.Max(0f, (float)Math.Sin(time * 1.6f - i * 0.5f)) * 10f;
                float x = left + i * gap;
                Rectangle cap = new Rectangle((int)x - 22, (int)(floor - 120 + dip), 44, 110);
                MusicianArt.Token(sb, cap, run.Roster[i], e, 0f);
                Gfx.TextSpacedCentered(sb, Game.Font, run.Roster[i].NameTag, x, floor + 10, Palette.PaperDim * e, TextSize.Tiny, 1.5f);
            }

            //Conductor : at the head of the line, bowing with the band
            float bow = Math.Max(0f, (float)Math.Sin(time * 1.6f + 0.5f)) * 8f;
            Rectangle figure = new Rectangle((int)left - 110, (int)(floor - 170 + bow), 76, 170 - (int)bow);
            PortraitBox.DrawFigure(sb, figure, run.Conductor, e);
            Gfx.TextSpacedCentered(sb, Game.Font, run.Conductor.Name, figure.Center.X, floor + 10, Palette.Paper * e, TextSize.Tiny, 1.5f);
        }

        //Ledger : the run record, like the back page of a programme
        private void DrawLedger(SpriteBatch sb, RunState run, float e)
        {
            Rectangle box = new Rectangle(LedgerX - 30, 80, LedgerW + 60, 530);
            Gfx.Rect(sb, box, Palette.Void * (0.85f * e));
            Ornament.DoubleFrame(sb, box, Palette.PaperDim * e);
            Ui.Header(sb, "", "Programme", LedgerX, 104, LedgerW, e);

            for (int i = 0; i < names.Length; i++)
            {
                float rowE = MathHelper.Clamp(e * 2f - i * 0.2f, 0f, 1f);
                int y = 158 + i * 48;
                Gfx.TextSpaced(sb, Game.Font, names[i], LedgerX, y + 6, Palette.PaperDim * rowE, TextSize.Label, 3f);
                Gfx.TextRight(sb, Game.BigFont, values[i], LedgerX + LedgerW, y - 4, Palette.Highlight * rowE, TextSize.Small);
                Gfx.Rect(sb, LedgerX, y + 34, LedgerW, 1, Palette.LineGrey * (0.4f * rowE));
            }

            //Motifs : every badge the band carried
            int my = 158 + names.Length * 48 + 6;
            Gfx.TextSpaced(sb, Game.Font, "MOTIFS", LedgerX, my, Palette.PaperDim * e, TextSize.Label, 3f);
            for (int i = 0; i < run.Motifs.Count; i++)
                MotifArt.Badge(sb, run.Motifs[i], LedgerX + 110 + i * 26, my + 8, 11, e);
            if (run.Motifs.Count == 0)
                Gfx.Text(sb, Game.StoryFont, "none", LedgerX + 110, my - 2, Palette.LineGrey * e, TextSize.Story);
        }
    }
}
