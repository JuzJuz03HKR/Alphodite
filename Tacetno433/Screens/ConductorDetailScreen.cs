using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //ConductorDetailScreen : we stepped inside the painting. A character profile page.
    //
    //Layout, left to right:
    //   LEFT   who they are: name, epithet, role, stats as diamonds, face and perk
    //   CENTRE the conductor standing in the scene
    //   RIGHT  the dossier: story, how they play, and their numbers
    //
    //The background is not a picture, it is drawn from maths every frame: parallax
    //pillars for depth, sine ribbons for the music, a backlight behind the figure,
    //a vignette and two letterbox bars to make it read like a shot from a film.
    public class ConductorDetailScreen : GameScreen
    {
        //Detail Layout : move these to reposition the page
        private const int LetterboxH = 40;
        private const int LeftX = 70;
        private const int RightX = 842;
        private const int RightW = 380;
        private Rectangle figureBox = new Rectangle(480, 70, 320, 530);
        private Rectangle faceBox = new Rectangle(70, 392, 112, 112);
        private Rectangle perkBox = new Rectangle(200, 392, 250, 112);
        private Rectangle chooseButton = new Rectangle(486, 614, 308, 50);
        private Rectangle backButton = new Rectangle(70, 616, 170, 46);

        //Detail Data
        private int index;
        private Conductor c;
        private Rectangle sourceRect;      // where the painting sat on the gallery page
        private string profileLabel = "";  // built once in Load, never while drawing
        private string epithetWrapped = "";
        private string seatsLabel = "";
        private float nameScale;

        //Detail State
        private float enter;               // 0 = just walked in, 1 = fully arrived
        private float time;

        public ConductorDetailScreen(int index, Rectangle sourceRect)
        {
            this.index = index;
            this.sourceRect = sourceRect;
            this.c = ConductorList.All[index];
        }

        public override void Load()
        {
            //Label Build : done here so Draw never has to join strings
            profileLabel = "CONDUCTOR PROFILE   /   " + c.IndexLabel;
            epithetWrapped = Gfx.WrapText(Game.StoryFont, c.Epithet, 370, TextSize.Story);
            int seats = c.Perk == ConductorPerk.EveryRoadHome ? 2 : 3;
            seatsLabel = seats.ToString();

            //Name Fit : long names shrink so they never run into the figure
            nameScale = TextSize.Hero;
            float width = Gfx.TextWidth(Game.BigFont, c.Name, nameScale);
            if (width > 380f) nameScale *= 380f / width;
        }

        public override void Update(float dt)
        {
            time += dt;

            //Enter Animation : fly into the painting, no input until we land
            enter += dt * 2.0f;
            if (enter > 1f) enter = 1f;
            if (enter < 1f) return;

            //Detail Back : leave the painting and stand in front of it again
            if (Input.KeyPressed(Keys.Escape) || Input.ClickedOn(backButton))
            {
                SoundBank.Play(Sfx.UiBack);
                Game.Screens.Change(new ConductorSelectScreen(index));
            }

            //Detail Confirm
            if (Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Space) || Input.ClickedOn(chooseButton))
                Confirm();
        }

        //Detail Confirm : remember the class and move on to the run
        private void Confirm()
        {
            //Run Begin : build a fresh run and hand the player their first choice of era
            Game.CurrentRun = new RunState();
            Game.CurrentRun.Start(c, Game.StoryFont, PanelStrip.CaptionWrapWidth);
            SoundBank.Play(Sfx.UiConfirm);
            Game.Screens.Change(new EraChoiceScreen(true));
        }

        public override void Draw(SpriteBatch sb)
        {
            //Enter Easing : slow at both ends so the zoom feels like stepping in
            float e = enter * enter * (3f - 2f * enter);

            //Panels only appear once we are most of the way in
            float alpha = (enter - 0.55f) / 0.45f;
            if (alpha < 0f) alpha = 0f;
            if (alpha > 1f) alpha = 1f;

            DrawScene(sb, e);

            //Figure : slides and grows from where the painting was into the middle of the shot
            Rectangle figure = LerpRect(sourceRect, figureBox, e);
            PortraitBox.DrawFigure(sb, figure, c, 1f);

            if (alpha > 0f)
            {
                DrawLeftColumn(sb, alpha);
                DrawRightColumn(sb, alpha);
                Ui.Button(sb, chooseButton, "TAKE THE BATON", "ENTER", true, true, alpha);
                Ui.Button(sb, backButton, "BACK", "ESC", false, true, alpha);
            }

            DrawLetterbox(sb);
        }

        //Rect Lerp : blend two rectangles, used by the enter animation
        private Rectangle LerpRect(Rectangle a, Rectangle b, float t)
        {
            return new Rectangle(
                (int)MathHelper.Lerp(a.X, b.X, t),
                (int)MathHelper.Lerp(a.Y, b.Y, t),
                (int)MathHelper.Lerp(a.Width, b.Width, t),
                (int)MathHelper.Lerp(a.Height, b.Height, t));
        }

        //Scene : the space behind the character, built in layers from far to near
        private void DrawScene(SpriteBatch sb, float e)
        {
            //Layer 1 : flat dark ground, about seventy percent of the shot stays this dark
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageDeep * e);

            //Layer 2 : parallax pillars. Three sets drifting at different speeds is enough
            //to read as depth, because the near ones move faster than the far ones.
            for (int layer = 0; layer < 3; layer++)
            {
                float spacing = 300f - layer * 90f;
                float speed = 5f + layer * 9f;
                float drift = (time * speed) % spacing;
                float width = 4f - layer;
                float bright = (0.025f + layer * 0.018f) * e;

                for (float x = -spacing + drift; x < TacetGame.ScreenW; x += spacing)
                    Gfx.Rect(sb, x, 0, width, TacetGame.ScreenH, Palette.Paper * bright);
            }

            //Layer 3 : staff horizon. Five lines the figure stands on, so the UI keeps
            //being made out of notation instead of generic boxes.
            Ornament.Stave(sb, 0, 560, TacetGame.ScreenW, 9, Palette.Paper * (0.12f * e));

            //Layer 4 : sound ribbons. Sine waves crossing the frame, music made visible.
            for (int r = 0; r < 3; r++)
            {
                float baseY = 210f + r * 130f;
                float amplitude = 34f + r * 22f;
                float frequency = 0.0055f + r * 0.0018f;
                float speed = 0.55f + r * 0.22f;
                float alpha = (0.10f - r * 0.025f) * e;

                Vector2 previous = new Vector2(0f, baseY);
                for (int x = 12; x <= TacetGame.ScreenW; x += 12)
                {
                    float y = baseY + (float)Math.Sin(x * frequency + time * speed) * amplitude;
                    Vector2 now = new Vector2(x, y);
                    Gfx.Line(sb, previous, now, Palette.Paper * alpha, 2f);
                    previous = now;
                }
            }

            //Layer 5 : backlight. One bright pool right behind the figure so its
            //silhouette reads instantly, which is the whole point of the dark base.
            Gfx.DrawGlow(sb, 640, 330, 340, Palette.Paper * (0.16f * e));
            Gfx.DrawGlow(sb, 640, 280, 170, Palette.Paper * (0.10f * e));
            Ornament.Rays(sb, 640, 250, 180, 420, 24, time * 0.02f, Palette.Paper * (0.035f * e));

            //Layer 6 : dust drifting upward, one glow each
            for (int i = 0; i < 20; i++)
            {
                float seed = i * 41.3f;
                float x = (float)((Math.Sin(seed) * 0.5 + 0.5) * TacetGame.ScreenW);
                float y = TacetGame.ScreenH - ((time * (10f + (i % 4) * 5f) + seed * 11f) % (TacetGame.ScreenH + 100f));
                float sway = (float)Math.Sin(time * 0.7f + seed) * 12f;
                Gfx.DrawGlow(sb, x + sway, y, 4f, Palette.Paper * (0.30f * e));
            }

            //Layer 7 : shade behind the two text columns so the words stay readable
            Gfx.DrawGlowBox(sb, new Rectangle(-260, 0, 800, TacetGame.ScreenH), Color.Black * (0.55f * e));
            Gfx.DrawGlowBox(sb, new Rectangle(760, 0, 800, TacetGame.ScreenH), Color.Black * (0.55f * e));
            Ornament.Vignette(sb, 60, 0.6f * e);
        }

        //Letterbox : two black bars, the cheapest way to make a screen feel like a film shot
        private void DrawLetterbox(SpriteBatch sb)
        {
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, LetterboxH, Palette.Void);
            Gfx.Rect(sb, 0, TacetGame.ScreenH - LetterboxH, TacetGame.ScreenW, LetterboxH, Palette.Void);
            Gfx.Rect(sb, 0, LetterboxH, TacetGame.ScreenW, 1, Palette.LineGrey * 0.45f);
            Gfx.Rect(sb, 0, TacetGame.ScreenH - LetterboxH - 1, TacetGame.ScreenW, 1, Palette.LineGrey * 0.45f);

            Gfx.TextSpaced(sb, Game.Font, profileLabel, LeftX, 14, Palette.LineGrey, TextSize.Tiny, 3f);
            Gfx.TextSpacedRight(sb, Game.Font, "TACET  4'33", TacetGame.ScreenW - LeftX, 14, Palette.LineGrey, TextSize.Tiny, 3f);
            Ornament.Crosses(sb, TacetGame.ScreenW - 180, TacetGame.ScreenH - 20, 5, 1, 14, Palette.LineGrey);
        }

        //Left Column : the character sheet
        private void DrawLeftColumn(SpriteBatch sb, float a)
        {
            int y = 62;

            //Name and Epithet
            Gfx.Text(sb, Game.BigFont, c.Name, LeftX, y, Palette.Highlight * a, nameScale);
            Gfx.Text(sb, Game.StoryFont, epithetWrapped, LeftX + 2, y + 58, Palette.PaperDim * a, TextSize.Story);
            Ornament.Rule(sb, LeftX, y + 112, 380, Palette.LineGrey * a);

            //Stat Rows : label on the left, value on the right, like a record card
            int rowY = y + 130;
            DrawRow(sb, "ROLE", rowY, a);
            Gfx.TextSpaced(sb, Game.Font, c.Role, LeftX + 130, rowY - 3, Palette.Paper * a, TextSize.Body, 2f);

            rowY += 36;
            DrawRow(sb, "STAMINA", rowY, a);
            Ui.Pips(sb, LeftX + 136, rowY + 6, c.Stamina, 10, 5, 15, a);
            Gfx.TextRight(sb, Game.Font, c.StaminaLabel, LeftX + 380, rowY - 3, Palette.Paper * a, TextSize.Body);

            rowY += 36;
            DrawRow(sb, "PUSH", rowY, a);
            Ui.Pips(sb, LeftX + 136, rowY + 6, c.PushPower, 10, 5, 15, a);
            Gfx.TextRight(sb, Game.Font, c.PushLabel, LeftX + 380, rowY - 3, Palette.Paper * a, TextSize.Body);

            rowY += 36;
            DrawRow(sb, "BASED ON", rowY, a);
            Gfx.Text(sb, Game.StoryFont, c.BasedOn, LeftX + 130, rowY - 5, Palette.Paper * a, TextSize.Story);

            //Face : the head shot in a viewfinder
            PortraitBox.DrawFaceIcon(sb, faceBox, c, a);

            //Perk Box : double frame, like the era box on a profile card
            Gfx.Rect(sb, perkBox, Palette.Void * (0.7f * a));
            Ornament.DoubleFrame(sb, perkBox, Palette.PaperDim * a);
            Gfx.TextSpaced(sb, Game.Font, "SIGNATURE", perkBox.X + 16, perkBox.Y + 14, Palette.LineGrey * a, TextSize.Tiny, 3f);
            Gfx.Text(sb, Game.BigFont, c.MechanicName, perkBox.X + 16, perkBox.Y + 30, Palette.Highlight * a, TextSize.Small);
            Gfx.TextSpaced(sb, Game.Font, "SEATS", perkBox.X + 16, perkBox.Y + 76, Palette.LineGrey * a, TextSize.Tiny, 3f);
            Gfx.Text(sb, Game.Font, seatsLabel, perkBox.X + 70, perkBox.Y + 71, Palette.Paper * a, TextSize.Body);
            Gfx.TextSpaced(sb, Game.Font, "BENCH", perkBox.X + 110, perkBox.Y + 76, Palette.LineGrey * a, TextSize.Tiny, 3f);
            Gfx.Text(sb, Game.Font, NumberText.Get(BattleRules.BenchSize), perkBox.X + 166, perkBox.Y + 71, Palette.Paper * a, TextSize.Body);
        }

        private void DrawRow(SpriteBatch sb, string label, int y, float a)
        {
            Gfx.Diamond(sb, LeftX + 3, y + 7, 3, Palette.PaperDim * a);
            Gfx.TextSpaced(sb, Game.Font, label, LeftX + 14, y, Palette.PaperDim * a, TextSize.Label, 2f);
            Gfx.Rect(sb, LeftX, y + 24, 380, 1, Palette.LineGrey * (0.35f * a));
        }

        //Right Column : the dossier. Story, then how they play, then the numbers.
        private void DrawRightColumn(SpriteBatch sb, float a)
        {
            int y = 64;

            //Section 1 : Story
            DrawRibbon(sb, "WHO THEY WERE", "I", y, a);
            Gfx.Text(sb, Game.StoryFont, c.StoryWrapped, RightX, y + 42, Palette.Paper * a, TextSize.Story);

            //Section 2 : Mechanic
            y += 190;
            DrawRibbon(sb, "HOW THEY PLAY", "II", y, a);
            Gfx.Text(sb, Game.BigFont, c.MechanicName, RightX, y + 38, Palette.Highlight * a, TextSize.Subtitle);
            Gfx.Text(sb, Game.StoryFont, c.MechanicWrapped, RightX, y + 76, Palette.Paper * a, TextSize.Story);

            //Section 3 : Numbers, printed large like a poster
            y += 200;
            DrawRibbon(sb, "IN NUMBERS", "III", y, a);
            DrawBigNumber(sb, "STAMINA", c.StaminaLabel, RightX, y + 40, a);
            DrawBigNumber(sb, "PUSH", c.PushLabel, RightX + 130, y + 40, a);
            DrawBigNumber(sb, "SEATS", seatsLabel, RightX + 260, y + 40, a);
        }

        //Ribbon : a section heading. A dark band ending in an arrow tip, a roman numeral,
        //and the title in spaced capitals.
        private void DrawRibbon(SpriteBatch sb, string title, string numeral, int y, float a)
        {
            Gfx.Rect(sb, RightX - 12, y, RightW - 30, 28, Palette.Paper * (0.10f * a));
            Gfx.Arrow(sb, RightX - 12 + RightW - 30 + 7, y + 14, 14, true, Palette.Paper * (0.10f * a));
            Gfx.Rect(sb, RightX - 12, y, 3, 28, Palette.Paper * a);
            Gfx.Text(sb, Game.BigFont, numeral, RightX, y + 1, Palette.PaperDim * a, TextSize.Small);
            Gfx.TextSpaced(sb, Game.Font, title, RightX + 40, y + 7, Palette.Highlight * a, TextSize.Label, 4f);
        }

        private void DrawBigNumber(SpriteBatch sb, string label, string value, int x, int y, float a)
        {
            Gfx.TextSpaced(sb, Game.Font, label, x, y, Palette.LineGrey * a, TextSize.Tiny, 3f);
            Gfx.Text(sb, Game.BigFont, value, x, y + 12, Palette.Paper * a, TextSize.Title);
            Gfx.Rect(sb, x, y + 62, 100, 1, Palette.LineGrey * a);
        }
    }
}
