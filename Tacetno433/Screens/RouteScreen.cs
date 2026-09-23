using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //RouteScreen : choosing where to go next.
    //
    //Not a map. Nothing is shown in advance. When the player arrives at a stage the game
    //lays out two to four places as tall panels across the whole screen, and they pick one.
    //Forward only, there is no going back.
    //
    //TAB (or the button) opens the stage so the ensemble can be rearranged between fights.
    public class RouteScreen : GameScreen
    {
        //Route Layout
        private const int PanelTop = 100;           // leaves a band under the top bar for motif badges
        private const int FooterY = 612;
        private Rectangle stageButton = new Rectangle(36, 642, 220, 48);

        //Route State
        private PanelStrip strip = new PanelStrip();
        private int selected;
        private float time;
        private Vector2 lastMouse;

        public override void Load()
        {
            if (Game.CurrentRun == null) return;

            selected = 0;
            strip.AreaTop = PanelTop;
            strip.AreaBottom = FooterY;
            strip.Reset(Game.CurrentRun.Options.Length, selected);
            lastMouse = Input.MousePos;

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

            //Route Mouse : hovering picks a panel, but only once the mouse actually moves
            if (Input.MousePos != lastMouse)
            {
                lastMouse = Input.MousePos;
                for (int i = 0; i < options.Length; i++)
                    if (Input.MouseOver(strip.PanelRect(i))) selected = i;
            }

            if (selected != before) SoundBank.Play(Sfx.UiMove);
            strip.Update(dt, selected);

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
                if (Input.ClickedOn(strip.PanelRect(i))) { selected = i; confirm = true; }

            if (confirm) Choose(selected);

            //Route Abandon : prototype shortcut back to the menu, remove this later
            if (Input.KeyPressed(Keys.Escape))
            {
                SoundBank.Play(Sfx.UiBack);
                Game.Screens.Change(new TitleScreen());
            }
        }

        //Route Choose : send the player to the page that matches the place
        private void Choose(int index)
        {
            RunState run = Game.CurrentRun;
            RouteNode node = run.Options[index];
            run.Chosen = node;
            run.RecordStop(node.Type);
            SoundBank.Play(Sfx.PathChosen);

            if (node.Type == NodeType.Battle || node.Type == NodeType.Elite || node.Type == NodeType.Boss)
            {
                //Fight : set up the battle, then open the stage to prepare
                run.BeginBattle();
                Game.Screens.Change(new FormationScreen(true));
            }
            else if (node.Type == NodeType.EraShift)
            {
                Game.Screens.Change(new EraChoiceScreen(false));
            }
            else if (node.Type == NodeType.Shop)
            {
                Game.Screens.Change(new ShopScreen());
            }
            else if (node.Type == NodeType.Rest)
            {
                Game.Screens.Change(new RestScreen());
            }
            else
            {
                run.CurrentEvent = EventList.Pick(run.Rng);
                Game.Screens.Change(new EventScreen());
            }
        }

        public override void Draw(SpriteBatch sb)
        {
            if (Game.CurrentRun == null) return;
            RunState run = Game.CurrentRun;

            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.Void);

            //Panels
            for (int i = 0; i < run.Options.Length; i++)
            {
                RouteNode node = run.Options[i];
                bool chosen = (i == selected);
                float e = strip.Emphasis(i);

                Rectangle box = strip.DrawBase(sb, i);

                //ARTWORK : Content/Art/Route/route_<kind>.png, see ArtBank
                Texture2D art = ArtBank.RouteOf(node.Type);
                if (art != null) sb.Draw(art, box, Color.White);

                int type = (int)node.Type;
                strip.DrawCaption(sb, i, RouteNodeInfo.PanelNumbers[i], node.Title, RouteNodeInfo.Tags[type],
                                  RouteNodeInfo.Danger[type], node.CaptionWrapped, chosen);
            }

            //Top Bar
            bool warn = run.BossIsNext && run.Stage < run.StagesThisFloor;
            Gfx.Rect(sb, 0, RunHud.TopH, TacetGame.ScreenW, PanelTop - RunHud.TopH, Palette.Void);
            RunHud.DrawTop(sb, run, warn ? "THE BOSS IS THE NEXT ROOM" : "CHOOSE A PATH");
            Gfx.TextSpaced(sb, Game.Font, run.FloorLabel, 36, RunHud.TopH + 12, Palette.LineGrey, TextSize.Tiny, 3f);

            DrawFooter(sb, run);
            RunHud.DrawTips(sb, run);
        }

        //Footer : where we are on the floor, and the stage button
        private void DrawFooter(SpriteBatch sb, RunState run)
        {
            Gfx.Rect(sb, 0, FooterY, TacetGame.ScreenW, TacetGame.ScreenH - FooterY, Palette.Void);
            Gfx.Rect(sb, 0, FooterY, TacetGame.ScreenW, 1, Palette.LineGrey * 0.5f);

            //Stage Number : spaced serif, the floor track right under it
            float cx = TacetGame.ScreenW / 2f;
            Gfx.TextSpacedCentered(sb, Game.BigFont, run.StageLabel, cx, 622, Palette.Paper, TextSize.Subtitle, 6f);
            RunHud.DrawTrack(sb, run, cx, 672, 440);

            Ui.Button(sb, stageButton, "VIEW STAGE", "TAB", false);

            Gfx.TextSpacedRight(sb, Game.Font, run.FloorShort, 1244, 640, Palette.PaperDim, TextSize.Label, 3f);
            Gfx.TextSpacedRight(sb, Game.Font, "ARROWS  CHOOSE   /   ENTER  GO", 1244, 670, Palette.LineGrey, TextSize.Tiny, 2f);
        }
    }
}
