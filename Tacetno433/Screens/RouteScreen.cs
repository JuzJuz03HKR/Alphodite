using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //RouteScreen : choosing where to go next (round 12.2 : the road and its fork).
    //
    //Not a map. Nothing ahead is shown, only how far the floor goes. Across the top runs the
    //ROAD of this floor : the stops already passed carry the mark of what was there, the
    //glowing one is where the band stands, the circle at the end is the boss. From that stop
    //the FORK drops to one to four small TILES, the places on offer now. The one under the
    //pointer shows its picture and its whole line in the DETAIL box below. Forward only.
    //
    //TAB (or the button) opens the stage so the ensemble can be rearranged between fights.
    //ESC opens the pause menu, like every page of a run. Arriving here saves the run.
    //(The era page keeps the tall panels of PanelStrip.)
    public class RouteScreen : GameScreen
    {
        //Route Layout
        private const int FooterY = 612;
        private Rectangle stageButton = new Rectangle(36, 642, 220, 48);

        //Road : the whole floor, one stop per stage, left to right
        private const float RoadY = 176f;
        private const float RoadLeft = 120f;
        private const float RoadRight = 1160f;
        private const int PastBox = 22;               // the square of a stop already passed

        //Tiles : the places on offer, hanging off the stop the band stands on
        private const int TileW = 196;
        private const int TileH = 112;
        private const int TileGap = 22;
        private const int TileTop = 262;
        private const int TileMargin = 40;             // tiles never come closer to the screen edge

        //Detail : the place under the pointer, with its picture (ARTWORK, see ArtBank.RouteOf)
        private static readonly Rectangle Detail = new Rectangle(250, 418, 780, 170);
        private const int DetailArtW = 230;

        //Route State
        private int selected;
        private float time;
        private float[] lift = new float[4];          // how far each tile has risen, 0 to 1
        private Vector2 lastMouse;

        public override void Load()
        {
            if (Game.CurrentRun == null) return;

            selected = 0;
            lift = new float[Game.CurrentRun.Options.Length];
            lastMouse = Input.MousePos;

            //Save : every arrival on the route is a point CONTINUE can come back to
            SaveFile.SaveRun(Game.CurrentRun);

            SoundBank.PlayMusic(Music.Route);
        }

        public override void Update(float dt)
        {
            time += dt;
            if (Game.CurrentRun == null) { Game.Screens.Change(new TitleScreen()); return; }

            RouteNode[] options = Game.CurrentRun.Options;
            int before = selected;

            //Route Keyboard
            if (Input.KeyPressed(Keys.Right) || Input.KeyPressed(Keys.D))
                selected = (selected + 1) % options.Length;
            if (Input.KeyPressed(Keys.Left) || Input.KeyPressed(Keys.A))
                selected = (selected - 1 + options.Length) % options.Length;

            //Route Mouse : hovering picks a tile, but only once the mouse actually moves
            if (Input.MousePos != lastMouse)
            {
                lastMouse = Input.MousePos;
                for (int i = 0; i < options.Length; i++)
                    if (Input.MouseOver(TileRect(i))) selected = i;
            }

            if (selected != before) SoundBank.Play(Sfx.UiMove);

            //Tile Lift : the chosen tile rises a little, the others settle back
            for (int i = 0; i < lift.Length; i++)
                lift[i] += ((i == selected ? 1f : 0f) - lift[i]) * Math.Min(1f, dt * 12f);

            //Stage View : look at the ensemble between fights
            if (Input.KeyPressed(Keys.Tab) || Input.ClickedOn(stageButton))
            {
                SoundBank.Play(Sfx.UiConfirm);
                Game.Screens.Change(new FormationScreen(false));
                return;
            }

            //Route Confirm
            bool confirm = Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Space);
            for (int i = 0; i < options.Length; i++)
                if (Input.ClickedOn(TileRect(i))) { selected = i; confirm = true; }
            if (Input.ClickedOn(Detail)) confirm = true;                 // the detail box is the chosen place too

            if (confirm) Choose(selected);
        }

        //Route Choose : send the player to the page that matches the place
        private void Choose(int index)
        {
            RunState run = Game.CurrentRun;
            RouteNode node = run.Options[index];
            run.Chosen = node;
            run.RecordStop(node.Type);
            SoundBank.Play(Sfx.PathChosen);
            RunFlow.Enter(Game, node, null, null);      // this also saves the run
        }

        public override void Draw(SpriteBatch sb)
        {
            if (Game.CurrentRun == null) return;
            RunState run = Game.CurrentRun;

            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.Void);

            DrawRoad(sb, run);
            DrawFork(sb, run);
            DrawTiles(sb, run);
            DrawDetail(sb, run);

            //Top Bar
            bool warn = run.BossIsNext && run.Stage < run.StagesThisFloor;
            RunHud.DrawTop(sb, run, warn ? "THE BOSS IS THE NEXT ROOM" : "CHOOSE A PATH");
            Gfx.TextSpaced(sb, Game.Font, run.FloorLabel, 36, RunHud.TopH + 12, Palette.LineGrey, TextSize.Tiny, 3f);

            DrawFooter(sb, run);
            RunHud.DrawTips(sb, run);
        }

        //Stop X : where stage s of this floor sits on the road
        private float StopX(RunState run, int stage)
        {
            int count = Math.Max(2, run.StagesThisFloor);
            return RoadLeft + (RoadRight - RoadLeft) * (stage - 1) / (count - 1);
        }

        //Tile Rect : the tiles sit in a row under the stop the band stands on, pulled back in
        //when that stop is near an end of the road. The chosen one rises a little.
        private Rectangle TileRect(int i)
        {
            RunState run = Game.CurrentRun;
            int count = run.Options.Length;
            int total = count * TileW + (count - 1) * TileGap;
            float centre = MathHelper.Clamp(StopX(run, run.Stage), TileMargin + total / 2f, TacetGame.ScreenW - TileMargin - total / 2f);
            int x = (int)(centre - total / 2f) + i * (TileW + TileGap);
            float up = i < lift.Length ? lift[i] : 0f;
            return new Rectangle(x, TileTop - (int)(up * 6f), TileW, TileH);
        }

        //Road : this floor from its first stage to the boss. The stops behind carry the mark of
        //the place picked there (the run's Journey), so the way already walked can be read.
        private void DrawRoad(SpriteBatch sb, RunState run)
        {
            float here = StopX(run, run.Stage);
            Gfx.Rect(sb, RoadLeft, RoadY, RoadRight - RoadLeft, 1, Palette.LineGrey * 0.7f);
            Gfx.Rect(sb, RoadLeft, RoadY - 1, here - RoadLeft, 3, Palette.PaperDim);

            int j = 0;                                       // walks the journey, skipping other floors
            for (int s = 1; s <= run.StagesThisFloor; s++)
            {
                float x = StopX(run, s);
                bool boss = s == run.StagesThisFloor;

                if (s < run.Stage)
                {
                    //Past Stop : the mark of what was there
                    while (j < run.Journey.Count && run.Journey[j].Floor != run.Floor) j++;
                    Rectangle box = new Rectangle((int)x - PastBox / 2, (int)RoadY - PastBox / 2, PastBox, PastBox);
                    Gfx.Rect(sb, box, Palette.Stage);
                    Gfx.RectOutline(sb, box, Palette.PaperDim, 1);
                    if (j < run.Journey.Count)
                    {
                        Gfx.TextCentered(sb, Game.Font, RouteNodeInfo.Marks[(int)run.Journey[j].Type], x, RoadY - 1, Palette.Paper, TextSize.Tiny);
                        j++;
                    }
                }
                else if (s == run.Stage)
                {
                    //Here : where the band stands, the fork starts from it
                    float pulse = 0.4f + 0.1f * (float)Math.Sin(time * 3f);
                    Gfx.DrawGlow(sb, x, RoadY, 28f, Palette.Accent * pulse);
                    Gfx.Diamond(sb, x, RoadY, 11, Palette.Void);
                    Gfx.DiamondOutline(sb, x, RoadY, 11, Palette.Accent, 2f);
                    Gfx.TextSpacedCentered(sb, Game.Font, "YOU ARE HERE", x, RoadY - 38, Palette.Paper, TextSize.Tiny, 2f);
                }
                else
                {
                    Gfx.Diamond(sb, x, RoadY, boss ? 9 : 5, Palette.Void);
                    Gfx.DiamondOutline(sb, x, RoadY, boss ? 9 : 5, Palette.LineGrey, 1f);
                }

                if (boss)
                {
                    Gfx.CircleOutline(sb, x, RoadY, 17, Palette.PaperDim, 1f);
                    Gfx.TextSpacedCentered(sb, Game.Font, "BOSS", x, RoadY + 24, Palette.PaperDim, TextSize.Tiny, 2f);
                }
            }
        }

        //Fork : a line from the stop the band stands on down to every place on offer
        private void DrawFork(SpriteBatch sb, RunState run)
        {
            float x = StopX(run, run.Stage);
            for (int i = 0; i < run.Options.Length; i++)
            {
                Rectangle tile = TileRect(i);
                bool chosen = i == selected;
                Gfx.Line(sb, x, RoadY + 12f, tile.Center.X, tile.Y, chosen ? Palette.Paper : Palette.LineGrey * 0.45f, chosen ? 2f : 1f);
            }
        }

        //Tiles : one small plate per place, its mark, its kind, how dangerous, its name.
        //The chosen one turns bright with dark writing.
        private void DrawTiles(SpriteBatch sb, RunState run)
        {
            for (int i = 0; i < run.Options.Length; i++)
            {
                RouteNode node = run.Options[i];
                int type = (int)node.Type;
                bool chosen = i == selected;
                Rectangle tile = TileRect(i);
                Color ink = chosen ? Palette.Ink : Palette.Paper;
                Color soft = chosen ? Palette.InkSoft : Palette.LineGrey;

                Gfx.Rect(sb, new Rectangle(tile.X + 4, tile.Y + 6, tile.Width, tile.Height), Color.Black * 0.4f);
                Gfx.Rect(sb, tile, chosen ? Palette.Paper : Palette.Panel);
                Gfx.RectOutline(sb, tile, chosen ? Palette.Highlight : Palette.LineGrey * 0.6f, 1);

                //Mark : the same letter the road and the curtain call use
                Rectangle mark = new Rectangle(tile.X + 12, tile.Y + 12, 40, 40);
                Gfx.RectOutline(sb, mark, chosen ? Palette.Ink : Palette.PaperDim, 1);
                Gfx.TextCentered(sb, Game.BigFont, RouteNodeInfo.Marks[type], mark.Center.X, mark.Center.Y - 2, ink, TextSize.Small);

                //Kind And Danger : dark pips on the bright tile, light ones on the dark
                Gfx.TextSpaced(sb, Game.Font, RouteNodeInfo.Tags[type], tile.X + 64, tile.Y + 14, soft, TextSize.Tiny, 2f);
                for (int p = 0; p < 3; p++)
                {
                    float px = tile.X + 70 + p * 13;
                    if (p < RouteNodeInfo.Danger[type]) Gfx.Diamond(sb, px, tile.Y + 42, 4, ink);
                    else Gfx.DiamondOutline(sb, px, tile.Y + 42, 4, soft, 1f);
                }

                Gfx.Text(sb, Game.BigFont, node.Title, tile.X + 12, tile.Y + 66, ink, TextSize.Small * 1.25f);
            }
        }

        //Detail : the place under the pointer, its picture and its whole line. Clicking it goes there.
        //ARTWORK : Content/Art/Route/route_<kind>.png drawn at DetailArtW x 142, see ArtBank
        private void DrawDetail(SpriteBatch sb, RunState run)
        {
            if (selected >= run.Options.Length) return;
            RouteNode node = run.Options[selected];
            int type = (int)node.Type;

            Gfx.Rect(sb, Detail, Palette.Panel);
            Ornament.DoubleFrame(sb, Detail, Palette.PaperDim);

            Rectangle art = new Rectangle(Detail.X + 14, Detail.Y + 14, DetailArtW, Detail.Height - 28);
            Texture2D picture = ArtBank.RouteOf(node.Type);
            if (picture != null) sb.Draw(picture, art, Color.White);
            else
            {
                Gfx.Rect(sb, art, Palette.Stage);
                ArtSlot.Draw(sb, art, Palette.Paper, 0.8f);
            }

            int x = art.Right + 24;
            Ui.Tag(sb, RouteNodeInfo.Tags[type], x, Detail.Y + 18, true, 1f);
            Gfx.Text(sb, Game.BigFont, node.Title, x, Detail.Y + 44, Palette.Highlight, TextSize.Title);
            Gfx.Text(sb, Game.StoryFont, node.CaptionWrapped, x, Detail.Y + 98, Palette.PaperDim, TextSize.Story);
        }

        //Footer : the stage number, the stage button and the keys
        private void DrawFooter(SpriteBatch sb, RunState run)
        {
            Gfx.Rect(sb, 0, FooterY, TacetGame.ScreenW, TacetGame.ScreenH - FooterY, Palette.Void);
            Gfx.Rect(sb, 0, FooterY, TacetGame.ScreenW, 1, Palette.LineGrey * 0.5f);

            //Stage Number : spaced serif (the floor track is the road at the top now)
            float cx = TacetGame.ScreenW / 2f;
            Gfx.TextSpacedCentered(sb, Game.BigFont, run.StageLabel, cx, 640, Palette.Paper, TextSize.Subtitle, 6f);

            Ui.Button(sb, stageButton, "VIEW STAGE", "TAB", false);

            Gfx.TextSpacedRight(sb, Game.Font, run.FloorShort, 1244, 640, Palette.PaperDim, TextSize.Label, 3f);
            Gfx.TextSpacedRight(sb, Game.Font, "ARROWS  CHOOSE   /   ENTER  GO", 1244, 670, Palette.LineGrey, TextSize.Tiny, 2f);
        }
    }
}
