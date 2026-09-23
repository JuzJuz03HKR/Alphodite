using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //EraChoiceScreen : pick which time and place to travel into.
    //
    //It runs in two situations:
    //   FLOOR OPENING : stage one of every floor is locked to this page, every era is on
    //                   offer, and musicians from the era you pick join you afterwards
    //   CROSSING      : a route path mid floor, only the eras you are NOT in are offered,
    //                   and it simply moves you
    public class EraChoiceScreen : GameScreen
    {
        private const int HeaderH = 84;
        private const int FooterY = 640;

        //Era Choice State
        private bool isFloorOpening;
        private int[] offered;             // which eras this page is showing
        private int selected;
        private float time;
        private PanelStrip strip = new PanelStrip();
        private Vector2 lastMouse;

        public EraChoiceScreen(bool floorOpening)
        {
            isFloorOpening = floorOpening;
        }

        public override void Load()
        {
            BuildOffer();
            selected = 0;
            strip.AreaTop = HeaderH;
            strip.AreaBottom = FooterY;
            strip.Reset(offered.Length, selected);
            lastMouse = Input.MousePos;

            SoundBank.PlayMusic(Music.Route);
        }

        //Era Offer : a floor opening shows every era, a crossing shows only the others
        private void BuildOffer()
        {
            int total = EraList.All.Length;

            if (isFloorOpening)
            {
                offered = new int[total];
                for (int i = 0; i < total; i++) offered[i] = i;
                return;
            }

            offered = new int[total - 1];
            int next = 0;
            for (int i = 0; i < total; i++)
                if (i != Game.CurrentRun.Era) { offered[next] = i; next++; }
        }

        public override void Update(float dt)
        {
            time += dt;
            int before = selected;

            //Era Keyboard
            if (Input.KeyPressed(Keys.Right) || Input.KeyPressed(Keys.D))
                selected = (selected + 1) % offered.Length;
            if (Input.KeyPressed(Keys.Left) || Input.KeyPressed(Keys.A))
                selected = (selected - 1 + offered.Length) % offered.Length;

            //Era Mouse : hovering picks a panel, but only once the mouse actually moves
            if (Input.MousePos != lastMouse)
            {
                lastMouse = Input.MousePos;
                for (int i = 0; i < offered.Length; i++)
                    if (Input.MouseOver(strip.PanelRect(i))) selected = i;
            }

            if (selected != before) SoundBank.Play(Sfx.UiMove);
            strip.Update(dt, selected);

            //Era Confirm
            bool confirm = Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Space);
            for (int i = 0; i < offered.Length; i++)
                if (Input.ClickedOn(strip.PanelRect(i))) { selected = i; confirm = true; }

            if (confirm) Choose(selected);
        }

        //Era Choose : step into the era, then either meet somebody or carry on routing
        private void Choose(int index)
        {
            RunState run = Game.CurrentRun;
            run.ChooseEra(offered[index]);
            SoundBank.Play(Sfx.PathChosen);

            if (isFloorOpening)
            {
                //Floor Opening : musicians from this era join before the routing starts
                run.RecruitAtOpening();
                Game.Screens.Change(new RecruitScreen());
            }
            else
            {
                //Crossing : the path is spent, move on to the next stage
                run.Advance();
                Game.Screens.Change(new RouteScreen());
            }
        }

        public override void Draw(SpriteBatch sb)
        {
            RunState run = Game.CurrentRun;
            float cx = TacetGame.ScreenW / 2f;

            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.Void);

            //Panels : the era painting, or the empty panel slot until it exists
            for (int i = 0; i < offered.Length; i++)
            {
                Era era = EraList.All[offered[i]];
                bool chosen = (i == selected);
                float e = strip.Emphasis(i);

                Rectangle box = strip.DrawBase(sb, i);

                //ARTWORK : Content/Art/Eras/era_<name>.png, see ArtBank
                Texture2D art = ArtBank.EraOf(offered[i]);
                if (art != null) sb.Draw(art, box, Color.White);

                strip.DrawCaption(sb, i, RouteNodeInfo.PanelNumbers[i], era.Name, era.Years, -1, era.DescriptionWrapped, chosen);
                Gfx.TextSpaced(sb, Game.Font, era.Subtitle, box.X + 24, box.Y + 72, Palette.PaperDim * (0.4f + 0.6f * e), TextSize.Tiny, 3f);
            }

            //Header
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, HeaderH, Palette.Void);
            string heading = isFloorOpening ? "WHERE DOES THE MUSIC GO NEXT" : "STEP THROUGH THE CROSSING";
            Gfx.TextSpacedCentered(sb, Game.BigFont, heading, cx, 20, Palette.Paper, TextSize.Small, 4f);
            Ornament.Divider(sb, cx, 62, 220, Palette.LineGrey);
            Gfx.TextSpaced(sb, Game.Font, isFloorOpening ? "FLOOR OPENING" : "CROSSING", 36, 36, Palette.LineGrey, TextSize.Tiny, 3f);
            Gfx.TextSpacedRight(sb, Game.Font, run.FloorShort, TacetGame.ScreenW - 36, 36, Palette.LineGrey, TextSize.Tiny, 3f);

            //Footer
            Gfx.Rect(sb, 0, FooterY, TacetGame.ScreenW, TacetGame.ScreenH - FooterY, Palette.Void);
            Gfx.Rect(sb, 0, FooterY, TacetGame.ScreenW, 1, Palette.LineGrey * 0.5f);
            Gfx.TextSpacedCentered(sb, Game.Font, run.FloorLabel, cx, 660, Palette.PaperDim, TextSize.Label, 4f);

            string hint = isFloorOpening
                ? "Musicians of that era will join you."
                : "You will stay in the new era until the next crossing.";
            Gfx.TextCentered(sb, Game.StoryFont, hint, cx, 694, Palette.LineGrey, TextSize.StorySmall);
            Gfx.TextSpacedRight(sb, Game.Font, "ARROWS  /  ENTER", TacetGame.ScreenW - 36, 676, Palette.LineGrey, TextSize.Tiny, 3f);
        }
    }
}
