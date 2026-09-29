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
    //   LEFT   who they are: name, epithet, role, stats as diamonds, face and story
    //   CENTRE the conductor standing in the scene, with arrows to the next and last profile
    //   RIGHT  how they play: the PASSIVE (always on) and the SIGNATURE (SPACE), and what each does
    //
    //The background is not a picture, it is drawn from maths every frame: parallax
    //pillars for depth, sine ribbons for the music, a backlight behind the figure,
    //a vignette and two letterbox bars to make it read like a shot from a film.
    public class ConductorDetailScreen : GameScreen
    {
        //Can Pause : a menu page outside the run, ESC here means going back
        public override bool CanPause
        {
            get { return false; }
        }

        //Detail Layout : move these to reposition the page
        private const int LetterboxH = 40;
        private const int LeftX = 70;
        private const int RightX = 842;
        private const int RightW = 380;
        private Rectangle figureBox = new Rectangle(480, 70, 320, 530);
        private const int StoryWrap = 250;             // the story sits beside the face, narrower than the dossier
        private Rectangle faceBox = new Rectangle(70, 392, 112, 112);
        private Rectangle chooseButton = new Rectangle(486, 614, 308, 50);
        private Rectangle backButton = new Rectangle(70, 616, 170, 46);
        private Rectangle lastButton = new Rectangle(424, 614, 50, 50);   // the arrows either side of TAKE THE BATON
        private Rectangle nextButton = new Rectangle(806, 614, 50, 50);

        //Detail Data
        private int index;
        private Conductor c;
        private Rectangle sourceRect;      // where the painting sat on the gallery page
        private string profileLabel = "";  // built once in Load, never while drawing
        private string epithetWrapped = "";
        private string storyWrapped = "";
        private string seatsLabel = "";
        private float nameScale;

        //Detail State
        private float enter;               // 0 = just walked in, 1 = fully arrived
        private float swap = 1f;           // the words fade in again after the arrows switch profile
        private float time;

        public ConductorDetailScreen(int index, Rectangle sourceRect)
        {
            this.index = index;
            this.sourceRect = sourceRect;
            this.c = ConductorList.All[index];
        }

        public override void Load()
        {
            Prepare();
        }

        //Prepare : the labels for the conductor on show, built here so Draw never joins strings.
        //Called again when the arrows switch to another conductor.
        private void Prepare()
        {
            profileLabel = "CONDUCTOR PROFILE   /   " + c.IndexLabel;
            epithetWrapped = Gfx.WrapText(Game.StoryFont, c.Epithet, 370, TextSize.Story);
            storyWrapped = Gfx.WrapText(Game.StoryFont, c.Story, StoryWrap, TextSize.Story);
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
            swap = Math.Min(1f, swap + dt * 4f);

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

            //Detail Switch : the arrows (or left and right) turn to the next profile without leaving
            if (Input.KeyPressed(Keys.Right) || Input.KeyPressed(Keys.D) || Input.ClickedOn(nextButton))
            {
                Switch(1);
                return;
            }
            if (Input.KeyPressed(Keys.Left) || Input.KeyPressed(Keys.A) || Input.ClickedOn(lastButton))
            {
                Switch(-1);
                return;
            }

            //Detail Confirm
            if (Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Space) || Input.ClickedOn(chooseButton))
                Confirm();
        }

        //Detail Switch : show the conductor one step along, stopping at the ends like the gallery.
        //The figure stays where it stands and the words fade in again.
        private void Switch(int direction)
        {
            int next = index + direction;
            if (next < 0 || next > ConductorList.All.Length - 1)
            {
                SoundBank.Play(Sfx.UiDenied);
                return;
            }

            index = next;
            c = ConductorList.All[index];
            sourceRect = figureBox;
            swap = 0f;
            Prepare();
            SoundBank.Play(Sfx.UiMove);
        }

        //Detail Confirm : remember the class and move on to the run
        private void Confirm()
        {
            //Run Begin : build a fresh run and hand the player their first choice of era
            Game.CurrentRun = new RunState();
            Game.CurrentRun.Start(c, Game.StoryFont, RouteNodeInfo.CaptionWrapWidth);
            SoundBank.Play(Sfx.UiConfirm);
            Game.Screens.Change(new ChapterScreen());
        }

        public override void Draw(SpriteBatch sb)
        {
            //Enter Easing : slow at both ends so the zoom feels like stepping in
            float e = enter * enter * (3f - 2f * enter);

            //Panels only appear once we are most of the way in
            float alpha = (enter - 0.55f) / 0.45f;
            if (alpha < 0f) alpha = 0f;
            if (alpha > 1f) alpha = 1f;
            alpha *= swap;

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
                DrawArrow(sb, lastButton, false, index > 0, alpha);
                DrawArrow(sb, nextButton, true, index < ConductorList.All.Length - 1, alpha);
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

        //Left Column : the character sheet, then the face and the story beside it
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
            DrawRow(sb, "BREATH", rowY, a);
            Ui.Pips(sb, LeftX + 136, rowY + 6, c.Stamina, 10, 5, 15, a);
            Gfx.TextRight(sb, Game.Font, c.StaminaLabel, LeftX + 380, rowY - 3, Palette.Paper * a, TextSize.Body);

            rowY += 36;
            DrawRow(sb, "PUSH", rowY, a);
            Ui.Pips(sb, LeftX + 136, rowY + 6, c.PushPower, 10, 5, 15, a);
            Gfx.TextRight(sb, Game.Font, c.PushLabel, LeftX + 380, rowY - 3, Palette.Paper * a, TextSize.Body);

            rowY += 36;
            DrawRow(sb, "SEATS", rowY, a);
            Gfx.Text(sb, Game.Font, seatsLabel, LeftX + 136, rowY - 3, Palette.Paper * a, TextSize.Body);

            rowY += 36;
            DrawRow(sb, "BASED ON", rowY, a);
            Gfx.Text(sb, Game.StoryFont, c.BasedOn, LeftX + 130, rowY - 5, Palette.Paper * a, TextSize.Story);

            //Face : the head shot in a viewfinder, and who they were beside it
            PortraitBox.DrawFaceIcon(sb, faceBox, c, a);
            int sx = faceBox.Right + 18;
            Gfx.TextSpaced(sb, Game.Font, "WHO THEY WERE", sx, faceBox.Y, Palette.LineGrey * a, TextSize.Tiny, 3f);
            Gfx.Text(sb, Game.StoryFont, storyWrapped, sx, faceBox.Y + 20, Palette.PaperDim * a, TextSize.Story);
        }

        private void DrawRow(SpriteBatch sb, string label, int y, float a)
        {
            Gfx.Diamond(sb, LeftX + 3, y + 7, 3, Palette.PaperDim * a);
            Gfx.TextSpaced(sb, Game.Font, label, LeftX + 14, y, Palette.PaperDim * a, TextSize.Label, 2f);
            Gfx.Rect(sb, LeftX, y + 24, 380, 1, Palette.LineGrey * (0.35f * a));
        }

        //Right Column : how they play. The PASSIVE is always on, the SIGNATURE is what SPACE lets
        //loose once the recipe is full (round 13 : every conductor has their own).
        private void DrawRightColumn(SpriteBatch sb, float a)
        {
            //Section 1 : Passive
            int y = 64;
            DrawRibbon(sb, "PASSIVE  /  ALWAYS ON", "I", y, a);
            Gfx.Text(sb, Game.BigFont, c.MechanicName, RightX, y + 38, Palette.Highlight * a, TextSize.Subtitle);
            Gfx.Text(sb, Game.StoryFont, c.MechanicWrapped, RightX, y + 76, Palette.Paper * a, TextSize.Story);

            //Section 2 : Signature, with the key that lets it loose and the recipe it needs
            y = 272;
            DrawRibbon(sb, "SIGNATURE", "II", y, a);
            Ui.KeyChipRight(sb, "SPACE", RightX + RightW - 58, y + 14, Palette.Paper * a);
            Gfx.Text(sb, Game.StoryFont, c.SignatureMark, RightX, y + 36, Palette.PaperDim * a, TextSize.Story);
            Gfx.Text(sb, Game.BigFont, c.SignatureName, RightX, y + 58, Palette.Highlight * a, TextSize.Subtitle);
            Gfx.Text(sb, Game.StoryFont, c.SignatureWrapped, RightX, y + 96, Palette.Paper * a, TextSize.Story);

            //Recipe : NEEDS, then a number and the section's name for each family it asks for
            int ny = y + 190;
            Gfx.TextSpaced(sb, Game.Font, "NEEDS", RightX, ny + 5, Palette.LineGrey * a, TextSize.Tiny, 2f);
            float rx = RightX + 70;
            for (int f = 0; f < c.Recipe.Length; f++)
            {
                if (c.Recipe[f] <= 0) continue;
                Gfx.Text(sb, Game.Font, NumberText.Get(c.Recipe[f]), rx, ny, Palette.Paper * a, TextSize.Body);
                string section = StageLayout.Rows[StageLayout.SectionOf((Family)f)].Name;
                Gfx.TextSpaced(sb, Game.Font, section, rx + 22, ny + 5, Palette.Paper * a, TextSize.Tiny, 2f);
                rx += 22 + Gfx.TextWidth(Game.Font, section, TextSize.Tiny) + section.Length * 2f + 26;
            }
            //Where the notes come from : PERFECT beats, and GOOD ones too for BY THE BOOK
            string from = c.Perk == ConductorPerk.ByTheBook ? "NOTES FROM PERFECT OR GOOD BEATS" : "NOTES FROM PERFECT BEATS";
            Gfx.TextSpaced(sb, Game.Font, from, RightX, ny + 32, Palette.LineGrey * a, TextSize.Tiny, 2f);
        }

        //Arrow : a diamond with an arrow inside, for the next and the last profile. Dim at the ends.
        private void DrawArrow(SpriteBatch sb, Rectangle box, bool pointRight, bool enabled, float a)
        {
            Color color = Palette.LineGrey * 0.5f;
            if (enabled) color = Input.MouseOver(box) ? Palette.Highlight : Palette.Paper;
            Gfx.Diamond(sb, box.Center.X, box.Center.Y, 24, Palette.Void * (0.8f * a));
            Gfx.DiamondOutline(sb, box.Center.X, box.Center.Y, 24, color * a, 1.5f);
            Gfx.Arrow(sb, box.Center.X + (pointRight ? 2 : -2), box.Center.Y, 9, pointRight, color * a);
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
    }
}
