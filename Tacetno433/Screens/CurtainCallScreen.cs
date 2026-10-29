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
    //
    //Beta1 screens round 2 (29 Oct, docs/SCREENS_LEVEL_UP.md 19) : the real stage curtain (Core/Curtain.cs) falls on a lost
    //run and opens on the bow, and falls again on the way back to the title (a won run still opens with the ring of light).
    //The band stands in a curve as the same tall columns as the band page (Core/BandColumn.cs), the conductor in the middle,
    //and they bow one after another. The programme's numbers count up (Ui.Number). A win is a bright stage, a loss is
    //TACET's hole growing behind the band while its dark creeps in from the edges.
    public class CurtainCallScreen : GameScreen
    {
        //Uses Curtain : the stage curtain falls on the run and rises on the bow
        public override bool UsesCurtain
        {
            get { return true; }
        }

        private Rectangle titleButton = new Rectangle(930, 642, 310, 50);
        private const int LedgerX = 782;
        private const int LedgerW = 440;
        private const int BowW = 92;                   // a column in the bow
        private const int BowH = 150;
        private static readonly Vector2 BowCentre = new Vector2(372f, 390f);
        private static readonly Vector2 LostHole = new Vector2(372f, 286f);   // behind the band, under the headline

        //Journey Marks : one letter per kind of place, the same as the route's map (RouteNodeInfo.Marks)
        private static string[] journeyKey = { "E ENCOUNTER", "! ELITE", "B BOSS", "? UNKNOWN", "$ SHOP", "R REST", "X CROSSING" };

        private bool won;
        private RunState run;            // kept here, because Game.CurrentRun is cleared when we leave
        private string[] names = new string[3];
        private string[] values = new string[3];
        private string[] countNames = new string[4];
        private int[] counts = new int[4];
        private string headline = "";
        private string subline = "";
        private float time;
        private UiMotion titleMotion = new UiMotion();

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
            countNames[0] = BattleRules.TurnBattleOn ? "FIGHTS WON" : "DUELS WON";      counts[0] = run.BattlesWon;
            countNames[1] = BattleRules.TurnBattleOn ? "PERFECTS" : "PERFECT BEATS";   counts[1] = run.PerfectsTotal;
            countNames[2] = BattleRules.TurnBattleOn ? "BEST STREAK" : "BEST COMBO";   counts[2] = run.BestCombo;
            countNames[3] = "SHARDS LEFT";                                              counts[3] = run.Shards;

            //ARCHIVE : the run leaves its ECHOES behind, saved at once (step 7)
            if (BattleRules.TurnBattleOn)
            {
                counts[3] = Archive.Pay(run, won);                 // given once, maybe already when the last fight was lost
                countNames[3] = "ECHOES";
            }

            run.SyncOrder();
            if (!won) Dark = new TacetVoid();
            Lens.Ambient = won ? 0.52f : 0.36f;
            Lens.SpotPower = 0f;
            Lens.Vignette = won ? 0.4f : 0.7f;
            Lens.BloomThreshold = won ? 0.72f : 0.7f;
            Lens.BloomStrength = won ? 0.5f : 0.4f;

            SoundBank.Play(won ? Sfx.RunComplete : Sfx.Defeat);
            SoundBank.PlayMusic(Music.Ending);
        }

        public override void Update(float dt)
        {
            time += dt;

            //A Lost Run : TACET's hole grows behind the band, its dark creeping in from the edges
            if (Dark != null)
            {
                float grow = UiMotion.EaseOut(MathHelper.Clamp(time / 2f, 0f, 1f));
                Dark.Update(dt, 0.55f);
                Dark.PlaceOnPage(LostHole, 48f + 40f * grow, 0f, 0f, 0f);
                Lens.Engulf = 0.32f * grow;
            }

            titleMotion.Update(dt, Ready && Input.MouseOver(titleButton));
            if (!Ready) return;

            if (Input.KeyPressed(Keys.Enter) || Input.ClickedOn(titleButton))
            {
                titleMotion.Press();
                SoundBank.Play(Sfx.UiConfirm);
                SoundBank.StopMusic();
                Game.Screens.Change(new TitleScreen());
            }
        }

        //Ready : the button works once the programme has come in
        private bool Ready
        {
            get { return time >= 0.9f; }
        }

        //Screen Leave : the run is thrown away here, once the fade has finished and this page
        //is really gone. Clearing it inside Update would leave Draw with nothing to draw for
        //the few frames the fade is still running.
        public override void Leave()
        {
            //only throw away the run this page was showing, in case a new one has already started
            if (Game.CurrentRun == run) Game.CurrentRun = null;
        }

        //Bow Rect : the column of the one standing at place i of the curve (place 0 is the middle, the conductor),
        //dipping as the bow passes along the line
        private Rectangle BowRect(int i, int count)
        {
            int side = i == 0 ? 0 : (i % 2 == 1 ? -((i + 1) / 2) : i / 2);    // 0, -1, 1, -2, 2 ... out from the middle
            float x = BowCentre.X + side * (BowW + 14);
            float y = BowCentre.Y - BowH + Math.Abs(side) * Math.Abs(side) * 9f;   // the curve : the ends a little nearer
            float order = side + count;                                            // the bow runs left to right along the line
            float dip = Math.Max(0f, (float)Math.Sin(time * 1.6f - order * 0.5f)) * 10f;
            float rise = UiMotion.Between(time, 0.3f + Math.Abs(side) * 0.08f, 0.5f);
            return new Rectangle((int)(x - BowW / 2f), (int)(y + dip + (1f - rise) * 40f), BowW, BowH);
        }

        //Feet : where the curve the band stands on passes at x (the middle further back, the ends a little nearer)
        private static float FeetY(float x)
        {
            float side = (x - BowCentre.X) / (BowW + 14);
            return BowCentre.Y + side * side * 9f;
        }

        private float BowIn(int i)
        {
            int side = i == 0 ? 0 : (i + 1) / 2;
            return UiMotion.Between(time, 0.3f + side * 0.08f, 0.5f);
        }

        //======================================================================================
        //The World (THE LENS) : the stage, the bow
        //======================================================================================

        public override bool HasWorld
        {
            get { return true; }
        }

        public override bool ShowsGrain
        {
            get { return false; }
        }

        //Back : what TACET's hole bends on a lost run, the stage behind the band
        public override void DrawBack(SpriteBatch sb)
        {
            DrawStage(sb);
        }

        public override void DrawWorld(SpriteBatch sb)
        {
            if (Dark == null) DrawStage(sb);
            else Dark.DrawDebris(sb, false);

            //The Bow : the band in a curve, the conductor in the middle
            int count = run.Order.Count + 1;
            for (int i = count - 1; i >= 0; i--)
            {
                float a = BowIn(i);
                if (a <= 0f) continue;
                Rectangle r = BowRect(i, count);
                if (i == 0) BandColumn.Picture(sb, r, ArtBank.GalleryOf(run.Conductor), run.Conductor.Spot, won ? 0.6f : 0.2f, a);
                else
                {
                    Musician m = run.Order[i - 1];
                    BandColumn.Picture(sb, r, UiArt.BustOf(m), BandColumn.SpotOf(m), won ? 0.4f : 0f, a * (won ? 1f : 0.8f));
                }
            }
        }

        //Stage : a bright stage for a win, a dark one for a loss, and the floor the band bows on
        private void DrawStage(SpriteBatch sb)
        {
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageDeep);
            if (won)
            {
                Gfx.DrawGlow(sb, BowCentre.X, BowCentre.Y - 80f, 560f, Palette.Paper * 0.18f);
                Ornament.Rays(sb, BowCentre.X, 120f, 60f, 760f, 40, time * 0.02f, Palette.Paper * 0.05f);
            }
            else Gfx.DrawGlow(sb, BowCentre.X, BowCentre.Y, 380f, Palette.Paper * 0.05f);
            Gfx.DrawGlowBox(sb, new Rectangle((int)BowCentre.X - 340, (int)BowCentre.Y - 30, 680, 110), Palette.Paper * 0.08f);

            //Floor : the curve the band stands on
            float rise = UiMotion.Between(time, 0.2f, 0.6f);
            for (float x = BowCentre.X - 330f * rise; x < BowCentre.X + 330f * rise; x += 12f)
                Gfx.Line(sb, x, FeetY(x) + 6f, x + 12f, FeetY(x + 12f) + 6f, Palette.PaperDim * 0.6f, 1f);
        }

        //Lights : a warm light on the bow for a win, a thin one for a loss
        public override void DrawLights(SpriteBatch sb)
        {
            float up = UiMotion.Between(time, 0.2f, 0.8f);
            Gfx.DrawGlow(sb, BowCentre.X, BowCentre.Y - 70f, 420f, Color.White * ((won ? 0.6f : 0.35f) * up));
            Gfx.DrawGlow(sb, LedgerX + LedgerW / 2f, 330f, 420f, Color.White * (0.3f * up));
        }

        //======================================================================================
        //The Words (crisp on top)
        //======================================================================================

        public override void Draw(SpriteBatch sb)
        {
            float a = UiMotion.Between(time, 0.15f, BattleRules.UiEnterTime);
            float b = UiMotion.Between(time, 0.25f, BattleRules.UiEnterTime);
            float slide = BattleRules.UiEnterSlide;

            //Headline
            Gfx.TextSpaced(sb, Game.Font, "CURTAIN CALL", 90, 44, Palette.PaperDim * a, TextSize.Label, 6f);
            Gfx.Text(sb, Game.LogoFont, headline, 84 - (1f - a) * slide, 64, Palette.Highlight * a, TextSize.Logo * 0.85f);
            Gfx.Text(sb, Game.StoryFont, subline, 92, 160, Palette.Paper * b, TextSize.Story);

            //Names Under The Bow
            int count = run.Order.Count + 1;
            for (int i = 0; i < count; i++)
            {
                float k = BowIn(i);
                if (k <= 0f) continue;
                Rectangle r = BowRect(i, count);
                string name = i == 0 ? run.Conductor.NameShort : run.Order[i - 1].NameTag;
                float scale = TextSize.Tiny;
                float wide = Gfx.SpacedWidth(Game.Font, name, scale, 1.5f);
                if (wide > BowW + 6) scale *= (BowW + 6) / wide;
                Gfx.TextSpacedCentered(sb, Game.Font, name, r.Center.X, FeetY(r.Center.X) + 16 + (i % 2) * 14, (i == 0 ? Palette.Highlight : Palette.PaperDim) * k, scale, 1.5f);
            }

            DrawJourney(sb);
            DrawLedger(sb);
            Ui.Command(sb, titleButton, "RETURN TO TITLE", "ENTER", titleMotion, Ready ? 1f : 0.4f * a, false);
        }

        //Journey : one small circle per place, floors split by a gap, joined like the map's ways. The place the run ended on
        //is crossed out. The key underneath says what each letter means.
        private void DrawJourney(SpriteBatch sb)
        {
            float e = UiMotion.Between(time, 0.6f, 0.5f);
            float x = 100;
            float y = 596;
            Gfx.TextSpaced(sb, Game.Font, "THE JOURNEY", 92, y - 30, Palette.PaperDim * e, TextSize.Tiny, 4f);

            int floor = 1;
            float lastX = -1f;
            for (int i = 0; i < run.Journey.Count; i++)
            {
                JourneyStop stop = run.Journey[i];
                if (stop.Floor != floor)
                {
                    floor = stop.Floor;
                    Gfx.Rect(sb, x - 4, y - 12, 1, 24, Palette.PaperDim * e);    // a floor line
                    x += 12;
                    lastX = -1f;
                }
                if (lastX >= 0f) Gfx.Rect(sb, lastX + 9, y, x - lastX - 18, 1, Palette.LineGrey * (0.7f * e));

                bool fight = stop.Type == NodeType.Battle || stop.Type == NodeType.Elite || stop.Type == NodeType.Boss;
                if (fight) Gfx.Circle(sb, x, y, 9f, Palette.PaperDim * e);
                Gfx.CircleOutline(sb, x, y, 9f, Palette.Paper * (0.7f * e), 1f);
                Gfx.TextCentered(sb, Game.Font, RouteNodeInfo.Marks[(int)stop.Type], x, y - 1, (fight ? Palette.Ink : Palette.Paper) * e, TextSize.Tiny);
                if (stop.Lost) Gfx.Line(sb, x - 11, y + 11, x + 11, y - 11, Palette.Highlight * e, 2f);
                lastX = x;
                x += 26;
            }

            //Key : what the letters stand for
            float kx = 92;
            for (int i = 0; i < journeyKey.Length; i++)
            {
                Gfx.TextSpaced(sb, Game.Font, journeyKey[i], kx, y + 26, Palette.LineGrey * e, TextSize.Tiny, 1.5f);
                kx += Gfx.SpacedWidth(Game.Font, journeyKey[i], TextSize.Tiny, 1.5f) + 18;
            }
        }

        //Ledger : the run record, like the back page of a programme. The words first, then the numbers counting up.
        private void DrawLedger(SpriteBatch sb)
        {
            float e = UiMotion.Between(time, 0.3f, BattleRules.UiEnterTime);
            Rectangle box = new Rectangle(LedgerX - 30, 60, LedgerW + 60, 560);
            Gfx.Rect(sb, box, Palette.Void * (0.82f * e));
            Ornament.DoubleFrame(sb, box, Palette.PaperDim * e);
            Gfx.Text(sb, Game.BigFont, "Programme", LedgerX, 80, Palette.Paper * e, TextSize.Small);
            Ornament.OrbitRule(sb, LedgerX + LedgerW / 2f, 118, LedgerW / 2f - 8f, Palette.LineGrey * e);

            for (int i = 0; i < names.Length; i++)
            {
                float rowE = UiMotion.Between(time, 0.4f + i * BattleRules.UiEnterStagger, BattleRules.UiEnterTime);
                int y = 138 + i * 50;
                Gfx.TextSpaced(sb, Game.Font, names[i], LedgerX, y + 8, Palette.PaperDim * rowE, TextSize.Label, 3f);
                Gfx.TextRight(sb, Game.BigFont, values[i], LedgerX + LedgerW, y - 2, Palette.Highlight * rowE, TextSize.Small);
                Gfx.Rect(sb, LedgerX, y + 36, LedgerW, 1, Palette.LineGrey * (0.4f * rowE));
            }

            //Numbers : big and blocky, counting up over a thin line (menu7)
            for (int i = 0; i < counts.Length; i++)
            {
                float start = 0.7f + i * 0.2f;
                float rowE = UiMotion.Between(time, start, BattleRules.UiEnterTime);
                if (rowE <= 0f) continue;
                int shown = UiMotion.Count(counts[i], time, start);
                float fill = counts[i] > 0 ? (float)shown / counts[i] : 1f;
                Ui.Number(sb, countNames[i], shown, i == 3 && BattleRules.TurnBattleOn, LedgerX, 300 + i * 56, LedgerW, fill,
                          i == 3 ? Palette.Highlight : Palette.Paper, Palette.PaperDim, rowE);
            }

            //Motifs : every badge the band carried
            float me = UiMotion.Between(time, 1.5f, BattleRules.UiEnterTime);
            int my = 532;
            Gfx.TextSpaced(sb, Game.Font, "MOTIFS", LedgerX, my, Palette.PaperDim * me, TextSize.Label, 3f);
            for (int i = 0; i < run.Motifs.Count; i++)
                MotifArt.Badge(sb, run.Motifs[i], LedgerX + 110 + i * 26, my + 8, 11, me);
            if (run.Motifs.Count == 0)
                Gfx.Text(sb, Game.StoryFont, "none", LedgerX + 110, my - 2, Palette.LineGrey * me, TextSize.Story);
        }
    }
}
