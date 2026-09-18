using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //ConductorSelectScreen : a gallery of portraits. You walk left and right along it,
    //a spotlight follows whoever is in front of you, and clicking the painting steps inside it.
    public class ConductorSelectScreen : GameScreen
    {
        //Gallery Layout : move these numbers to reposition everything
        private const int FrameW = 330;
        private const int FrameH = 430;
        private const int FrameY = 148;
        private const int Spacing = 440;              // gap between one painting and the next
        private const int CenterX = TacetGame.ScreenW / 2;
        private const int CurtainW = 150;
        private const int FloorY = 640;

        //Gallery Buttons
        private Rectangle leftButton = new Rectangle(250, 318, 70, 70);
        private Rectangle rightButton = new Rectangle(960, 318, 70, 70);
        private Rectangle backButton = new Rectangle(36, 648, 150, 42);

        //Gallery State
        private int index;                            // which painting we are standing at
        private float scroll;                         // smoothly slides toward index
        private float time;

        public ConductorSelectScreen()
        {
            index = 0;
        }

        //Gallery Return : used when coming back out of a painting, so we stand at the same one
        public ConductorSelectScreen(int startIndex)
        {
            index = startIndex;
        }

        public override void Load()
        {
            scroll = index;
            SoundBank.PlayMusic(Music.Gallery);
        }

        public override void Update(float dt)
        {
            time += dt;

            //Gallery Scroll : ease the camera toward the selected painting
            scroll += (index - scroll) * dt * 9f;
            if (Math.Abs(index - scroll) < 0.002f) scroll = index;

            //Gallery Navigate
            if (Input.KeyPressed(Keys.Right) || Input.KeyPressed(Keys.D) || Input.ClickedOn(rightButton))
                Move(1);
            if (Input.KeyPressed(Keys.Left) || Input.KeyPressed(Keys.A) || Input.ClickedOn(leftButton))
                Move(-1);

            //Gallery Enter : click the painting or press confirm to step inside it
            Rectangle centerFrame = FrameRect(index);
            bool stepInside = Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Space) || Input.ClickedOn(centerFrame);

            if (stepInside)
            {
                SoundBank.Play(Sfx.UiConfirm);
                Game.Screens.Change(new ConductorDetailScreen(index, centerFrame));
            }

            //Gallery Back
            if (Input.KeyPressed(Keys.Escape) || Input.ClickedOn(backButton))
            {
                SoundBank.Play(Sfx.UiBack);
                Game.Screens.Change(new TitleScreen());
            }
        }

        //Gallery Move : step one painting left or right, stopping at the ends
        private void Move(int direction)
        {
            int before = index;
            index += direction;
            if (index < 0) index = 0;
            if (index > ConductorList.All.Length - 1) index = ConductorList.All.Length - 1;

            SoundBank.Play(index != before ? Sfx.UiMove : Sfx.UiDenied);
        }

        //Gallery Frame Position : where painting number i sits on screen right now
        private Rectangle FrameRect(int i)
        {
            float x = CenterX + (i - scroll) * Spacing - FrameW / 2f;
            return new Rectangle((int)x, FrameY, FrameW, FrameH);
        }

        public override void Draw(SpriteBatch sb)
        {
            DrawRoom(sb);

            //Room Dim : everything that is not under the light goes dark
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Color.Black * 0.42f);

            DrawSpotlight(sb);
            DrawPaintings(sb);
            DrawCurtains(sb);
            DrawVignette(sb);
            DrawInterface(sb);        // drawn last so no text ends up under the shading
        }

        //Room : back wall and floor of the gallery
        private void DrawRoom(SpriteBatch sb)
        {
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageDeep);

            //Wall : slightly lighter block behind the paintings
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, FloorY, Palette.Stage);

            //Wall Panels : tall faint frames along the wall. A flat wall reads as empty space,
            //a few panel lines make it read as a real room.
            for (int i = 0; i < 9; i++)
            {
                Rectangle panel = new Rectangle(20 + i * 142, 96, 120, 500);
                Gfx.RectOutline(sb, panel, Palette.Paper * 0.03f, 1);
            }
            Gfx.Rect(sb, 0, 88, TacetGame.ScreenW, 1, Palette.Paper * 0.05f);

            //Floor : with long boards running toward the viewer
            Gfx.Rect(sb, 0, FloorY, TacetGame.ScreenW, TacetGame.ScreenH - FloorY, Palette.StageDeep);
            Gfx.Rect(sb, 0, FloorY, TacetGame.ScreenW, 1, Palette.LineGrey);
            for (int i = -8; i <= 8; i++)
                Gfx.Line(sb, CenterX + i * 40, FloorY, CenterX + i * 150, TacetGame.ScreenH, Palette.Paper * 0.03f, 1f);
        }

        //Spotlight : a cone of light dropping from the ceiling onto whoever is selected.
        //Drawn three pixel rows at a time so the sloped edge stays smooth but stays cheap.
        private void DrawSpotlight(SpriteBatch sb)
        {
            for (int y = 0; y < FloorY; y += 3)
            {
                float t = (float)y / FloorY;
                float halfWidth = 45f + t * 285f;

                //Three widths stacked on each row make the middle of the beam brighter
                Gfx.Rect(sb, CenterX - halfWidth, y, halfWidth * 2f, 3, Color.White * 0.030f);
                Gfx.Rect(sb, CenterX - halfWidth * 0.66f, y, halfWidth * 1.32f, 3, Color.White * 0.030f);
                Gfx.Rect(sb, CenterX - halfWidth * 0.33f, y, halfWidth * 0.66f, 3, Color.White * 0.035f);
            }

            //Beam Dust : specks caught in the light. This is the small detail that makes a
            //beam read as real light instead of a grey triangle.
            for (int i = 0; i < 16; i++)
            {
                float t = (time * 0.06f + i * 0.0625f) % 1f;       // 0 at the ceiling, 1 at the floor
                float spread = (45f + t * 285f) * 0.85f;
                float x = CenterX + (float)Math.Sin(i * 53.1f + time * 0.3f) * spread;
                float a = 0.05f + 0.25f * (1f - t);

                Gfx.DrawGlow(sb, x, t * FloorY, 3f, Palette.Paper * a);
            }

            //Light Pool : where the beam lands. One soft glow, squashed flat.
            Gfx.DrawGlowBox(sb, new Rectangle(CenterX - 340, FloorY - 60, 680, 150), Color.White * 0.16f);
        }

        //Paintings : draw every frame, the further from the light the darker it gets
        private void DrawPaintings(SpriteBatch sb)
        {
            for (int i = 0; i < ConductorList.All.Length; i++)
            {
                Rectangle frame = FrameRect(i);

                //Skip anything fully off screen
                if (frame.Right < -40 || frame.X > TacetGame.ScreenW + 40) continue;

                float distance = Math.Abs(i - scroll);
                if (distance > 1f) distance = 1f;
                float brightness = 1f - distance * 0.72f;

                //Hanging Shadow : soft dark behind and below, so the frame sits on the wall
                Gfx.DrawGlowBox(sb, new Rectangle(frame.X - 26, frame.Y + 24, frame.Width + 52, frame.Height + 52),
                                Color.Black * 0.55f);

                PortraitBox.Draw(sb, frame, ConductorList.All[i], brightness, true);

                //Chapter Mark : the big number above each painting, like an exhibition label
                Conductor c = ConductorList.All[i];
                Gfx.TextCentered(sb, Game.BigFont, NumberText.Get(i + 1), frame.Center.X, frame.Y - 44, Palette.Paper * brightness, TextSize.Subtitle);
                Gfx.Rect(sb, frame.Center.X - 16, frame.Y - 26, 32, 1, Palette.Paper * brightness);
                Gfx.TextSpacedCentered(sb, Game.Font, c.Role, frame.Center.X, frame.Y - 20, Palette.PaperDim * brightness, TextSize.Tiny, 3f);
            }
        }

        //Curtains : drawn after the paintings so the side ones slide away behind them
        private void DrawCurtains(SpriteBatch sb)
        {
            DrawOneCurtain(sb, 0, false);
            DrawOneCurtain(sb, TacetGame.ScreenW - CurtainW, true);
        }

        //Curtain : vertical folds made of thin bars
        private void DrawOneCurtain(SpriteBatch sb, int x, bool mirrored)
        {
            for (int i = 0; i < CurtainW; i += 2)
            {
                float t = (float)i / CurtainW;
                if (mirrored) t = 1f - t;

                float fold = (float)Math.Sin(t * 24f);
                Color c = Color.Lerp(Palette.CurtainDark, Palette.Curtain, 0.5f + fold * 0.5f);
                c = Color.Lerp(c, Palette.Void, t * 0.6f);           // darker toward the stage

                Gfx.Rect(sb, x + i, 0, 2, TacetGame.ScreenH, c);
            }
        }

        //Vignette : shade the top and bottom. Left and right are already covered by curtains.
        private void DrawVignette(SpriteBatch sb)
        {
            for (int i = 0; i < 40; i++)
            {
                float t = 1f - i / 40f;
                float a = t * t * 0.5f;
                Gfx.Rect(sb, 0, i, TacetGame.ScreenW, 1, Color.Black * a);
                Gfx.Rect(sb, 0, TacetGame.ScreenH - 1 - i, TacetGame.ScreenW, 1, Color.Black * a);
            }
        }

        //Interface : heading, arrows, back button and the lines under the painting
        private void DrawInterface(SpriteBatch sb)
        {
            Conductor c = ConductorList.All[index];

            //Header : spaced serif title with a thin rule and a small caption
            Gfx.TextSpacedCentered(sb, Game.BigFont, "CHOOSE YOUR CONDUCTOR", CenterX, 16, Palette.Paper, TextSize.Small, 5f);
            Ornament.Divider(sb, CenterX, 52, 170, Palette.LineGrey);
            Gfx.TextSpaced(sb, Game.Font, "THE GALLERY OF CONDUCTORS", 36, 30, Palette.LineGrey, TextSize.Tiny, 3f);
            Gfx.TextSpacedRight(sb, Game.Font, "SELECT ONE TO BEGIN", TacetGame.ScreenW - 36, 30, Palette.LineGrey, TextSize.Tiny, 3f);

            //Epithet : the line under the painting, only once the camera has settled
            float settled = 1f - Math.Min(1f, Math.Abs(index - scroll) * 3f);
            if (settled > 0f)
            {
                Gfx.TextCentered(sb, Game.StoryFont, c.Epithet, CenterX, 612, Palette.Paper * settled, TextSize.Story);
                Gfx.TextSpacedCentered(sb, Game.Font, c.BasedOnLabel, CenterX, 632, Palette.LineGrey * settled, TextSize.Tiny, 3f);
            }

            //Arrow Buttons : greyed out at the ends of the gallery
            DrawArrowButton(sb, leftButton, false, index > 0);
            DrawArrowButton(sb, rightButton, true, index < ConductorList.All.Length - 1);

            //Position Diamonds : which painting out of how many
            int count = ConductorList.All.Length;
            for (int i = 0; i < count; i++)
            {
                float dotX = CenterX - (count - 1) * 10f + i * 20f;
                if (i == index) Gfx.Diamond(sb, dotX, 672, 5, Palette.Highlight);
                else Gfx.DiamondOutline(sb, dotX, 672, 4, Palette.LineGrey, 1f);
            }

            Ui.Button(sb, backButton, "BACK", "ESC", false);

            //Hint
            Gfx.TextSpacedCentered(sb, Game.Font, "CLICK THE PAINTING TO STEP INSIDE", CenterX, 694, Palette.LineGrey, TextSize.Tiny, 3f);
        }

        //Arrow Button : a diamond frame with an arrow inside, dimmed when you cannot go that way
        private void DrawArrowButton(SpriteBatch sb, Rectangle box, bool pointRight, bool enabled)
        {
            Color color = Palette.LineGrey * 0.5f;
            if (enabled) color = Input.MouseOver(box) ? Palette.Highlight : Palette.Paper;

            //Arrow Pulse : nudges outward so the player notices it can be pressed
            float pulse = enabled ? (float)Math.Sin(time * 3f) * 3f : 0f;
            float cx = box.Center.X + (pointRight ? pulse : -pulse);

            Gfx.Diamond(sb, cx, box.Center.Y, 30, Palette.Void * 0.7f);
            Gfx.DiamondOutline(sb, cx, box.Center.Y, 30, color, 1.5f);
            Gfx.DiamondOutline(sb, cx, box.Center.Y, 24, color * 0.4f, 1f);
            Gfx.Arrow(sb, cx + (pointRight ? 2 : -2), box.Center.Y, 10, pointRight, color);
        }
    }
}
