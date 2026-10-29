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
    //Beta1 screens round 2 (29 Oct, docs/SCREENS_LEVEL_UP.md 16), left to right :
    //   LEFT     who they are : the name, their line, the face close up (face_<name>.png, 224 x 224, drawn at its size),
    //            their role and the real composer, and who they were
    //   CENTRE   the painting itself. The gallery hands it over where its panel stood (SHARED PICTURE, ScreenManager.Cut)
    //            and it is carried to the middle of the shot at the size it was painted, the room coming up around it :
    //            a backlight in the conductor's colour (Conductor.Spot), sound ribbons, a stave for a horizon
    //   RIGHT    how they play : the PASSIVE (always on) and the SIGNATURE (SPACE)
    //   BOTTOM   BACK, the arrows to the next and the last profile either side of TAKE THE BATON, and the mode
    //The arrows slide one painting out and the next in. BACK carries the painting back to its panel in the gallery.
    //Two black bars top and bottom make it read like a shot from a film.
    //AURA (29 Oct) : the conductor's theme lives round the painting as the room comes up (Core/ConductorAura.cs), the same
    //weather the gallery shows when it is pointed at. A name too long for the column takes two lines (NAME FIT).
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
        private const int LeftW = 380;
        private const int RightX = 842;
        private const int RightW = 380;
        private const int FaceSize = 224;              // the face as it was drawn (docs/UI_ART_PLAN.md 2.3)
        private static readonly Rectangle paintBox = new Rectangle(TacetGame.ScreenW / 2 - ConductorSelectScreen.PaintW / 2, 96,
                                                                   ConductorSelectScreen.PaintW, ConductorSelectScreen.PaintH);
        private Rectangle faceBox = new Rectangle(LeftX, 174, FaceSize, FaceSize);
        private Rectangle chooseButton = new Rectangle(486, 614, 308, 50);
        private Rectangle backButton = new Rectangle(70, 616, 170, 46);
        private Rectangle lastButton = new Rectangle(400, 614, 50, 50);    // the arrows either side of TAKE THE BATON
        private Rectangle nextButton = new Rectangle(830, 614, 50, 50);
        private Rectangle modeBox = new Rectangle(976, 612, 234, 38);      // NORMAL or MAESTRO, MAESTRO MODE

        //Detail Data
        private int index;
        private Conductor c;
        private string profileLabel = "";  // built in Prepare, never while drawing
        private string epithetWrapped = "";
        private string storyWrapped = "";
        private string basedOnWrapped = "";
        private float nameScale;
        private string nameTop = "";       // NAME FIT : the name, or its first line when it is too long for one
        private string nameLow = "";       //   and the rest on a second line ("" for a name that fits)
        private float nameDrop;            //   how far the column under the name steps down for that second line
        private ConductorAura[] auras = new ConductorAura[0];   // AURA : one for each conductor the arrows can show

        //Detail State
        private enum Phase { Arrive, Look, Leave }
        private Phase phase = Phase.Arrive;
        private float time;                // seconds on this page
        private float leaveTime;
        private float swap = 99f;          // seconds since the arrows changed the profile
        private int swapFrom = -1;         // the conductor whose painting slides out
        private int swapWay;               // +1 the next one came in from the right, -1 from the left
        private UiMotion chooseMotion = new UiMotion();
        private UiMotion backMotion = new UiMotion();
        private UiMotion lastMotion = new UiMotion();
        private UiMotion nextMotion = new UiMotion();

        public ConductorDetailScreen(int index)
        {
            this.index = Math.Max(0, Math.Min(ConductorList.Shown.Length - 1, index));
            this.c = ConductorList.Shown[this.index];
        }

        public override void Load()
        {
            Prepare();
            auras = new ConductorAura[ConductorList.Shown.Length];
            for (int i = 0; i < auras.Length; i++) auras[i] = new ConductorAura(ConductorList.Shown[i].Aura, 433 + i);

            //The Lens : the painting is lit as drawn (the gallery hands it over in the same light), the corners like a stage
            Lens.Ambient = 0.5f;
            Lens.SpotPower = 0f;
            Lens.Vignette = 0.5f;
        }

        //Prepare : the labels for the conductor on show, built here so Draw never joins strings.
        //Called again when the arrows switch to another conductor.
        private void Prepare()
        {
            profileLabel = "CONDUCTOR PROFILE   /   " + c.IndexLabel;
            epithetWrapped = Gfx.WrapText(Game.StoryFont, c.Epithet, LeftW, TextSize.Story);
            storyWrapped = Gfx.WrapText(Game.StoryFont, c.Story, LeftW, TextSize.Story);
            basedOnWrapped = Gfx.WrapText(Game.StoryFont, c.BasedOn, LeftW - FaceSize - 22, TextSize.Story);

            //Name Fit : long names shrink so they never run into the painting. NAME FIT : a name that would shrink to less
            //than 80% takes two lines instead, broken at the space that keeps both lines shortest (FANNY MENDELSSOHN / AND ???)
            nameTop = c.Name;
            nameLow = "";
            nameDrop = 0f;
            nameScale = TextSize.Hero;
            float width = Gfx.TextWidth(Game.BigFont, c.Name, nameScale);
            if (width > LeftW) nameScale *= LeftW / width;
            if (nameScale < TextSize.Hero * 0.8f)
            {
                float best = float.MaxValue;
                for (int k = c.Name.IndexOf(' '); k > 0; k = c.Name.IndexOf(' ', k + 1))
                {
                    float wider = Math.Max(Gfx.TextWidth(Game.BigFont, c.Name.Substring(0, k), TextSize.Hero),
                                           Gfx.TextWidth(Game.BigFont, c.Name.Substring(k + 1), TextSize.Hero));
                    if (wider >= best) continue;
                    best = wider;
                    nameTop = c.Name.Substring(0, k);
                    nameLow = c.Name.Substring(k + 1);
                }
                nameScale = TextSize.Hero * Math.Min(1f, LeftW / best);
                nameDrop = Game.BigFont.LineSpacing * nameScale * 0.86f;
            }
        }

        public override void Update(float dt)
        {
            time += dt;
            swap += dt;

            //Aura : the one on show comes up with the room, the one sliding out under the arrows fades with it
            for (int i = 0; i < auras.Length; i++)
            {
                float power = 0f;
                Rectangle frame = PaintingAt();
                if (i == index) power = Room() * (swapFrom >= 0 ? Math.Min(1f, swap / BattleRules.UiProfileSwap) : 1f);
                else if (i == swapFrom && swap < BattleRules.UiProfileSwap)
                {
                    float t = UiMotion.EaseOut(swap / BattleRules.UiProfileSwap);
                    frame = paintBox;
                    frame.X -= (int)Math.Round(t * 140f * swapWay);
                    power = Room() * (1f - t);
                }
                auras[i].Update(dt, frame, power);
            }

            //Motion : TAKE THE BATON is the page's main action, so it stands pointed at (white) unless another button is
            bool look = phase == Phase.Look;
            bool elsewhere = Input.MouseOver(backButton) || Input.MouseOver(lastButton) || Input.MouseOver(nextButton) || Input.MouseOver(modeBox);
            chooseMotion.Update(dt, look && !elsewhere);
            backMotion.Update(dt, look && Input.MouseOver(backButton));
            lastMotion.Update(dt, look && index > 0 && Input.MouseOver(lastButton));
            nextMotion.Update(dt, look && index < ConductorList.Shown.Length - 1 && Input.MouseOver(nextButton));

            //Arrive : the painting is carried in, no input until it lands
            if (phase == Phase.Arrive)
            {
                if (time >= BattleRules.UiProfileCarry) phase = Phase.Look;
                return;
            }

            //Leave : the painting is carried back to its panel, then the gallery takes it (SHARED PICTURE)
            if (phase == Phase.Leave)
            {
                leaveTime += dt;
                if (leaveTime >= BattleRules.UiProfileCarry) Game.Screens.Cut(new ConductorSelectScreen(index, true));
                return;
            }

            //Detail Back : step out of the painting
            if (Input.KeyPressed(Keys.Escape) || Input.ClickedOn(backButton))
            {
                backMotion.Press();
                SoundBank.Play(Sfx.UiBack);
                phase = Phase.Leave;
                leaveTime = 0f;
                return;
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

            //Detail Mode : NORMAL or MAESTRO, the same switch as the gallery
            if (Input.ClickedOn(modeBox) || Input.KeyPressed(Keys.M)) ConductorSelectScreen.FlipMode();

            //Detail Confirm
            if (Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Space) || Input.ClickedOn(chooseButton))
                Confirm();
        }

        //Detail Switch : show the conductor one step along, stopping at the ends like the gallery.
        //The painting slides out and the next one slides in, the words fade in again.
        private void Switch(int direction)
        {
            int next = index + direction;
            if (next < 0 || next > ConductorList.Shown.Length - 1)
            {
                SoundBank.Play(Sfx.UiDenied);
                return;
            }

            (direction > 0 ? nextMotion : lastMotion).Press();
            swapFrom = index;
            swapWay = direction;
            swap = 0f;
            index = next;
            c = ConductorList.Shown[index];
            Prepare();
            SoundBank.Play(Sfx.UiMove);
        }

        //Detail Confirm : remember the class and move on to the run
        private void Confirm()
        {
            chooseMotion.Press();

            //Run Begin : build a fresh run and hand the player their first choice of era
            Game.CurrentRun = new RunState();
            Game.CurrentRun.Start(c, Game.StoryFont, RouteNodeInfo.CaptionWrapWidth);
            Game.CurrentRun.Maestro = Settings.Maestro;                                  // MAESTRO MODE
            SoundBank.Play(Sfx.UiConfirm);
            Game.Screens.SweepNext(chooseButton.Center.ToVector2(), RingSweep.Kind.Light);     // RING SWEEP : the run begins
            Game.Screens.Change(new ChapterScreen());
        }

        //======================================================================================
        //Timing
        //======================================================================================

        //Room : 0..1, how far the room around the painting is there. It comes up as the painting is carried in, and goes
        //back to black as it is carried out (at 0 the gallery shows the very same picture).
        private float Room()
        {
            float up = UiMotion.EaseOut(MathHelper.Clamp((time - 0.05f) / BattleRules.UiProfileCarry, 0f, 1f));
            if (phase == Phase.Leave) up *= 1f - UiMotion.EaseInOut(Left());
            return up;
        }

        //Left : 0..1 through the carry back to the gallery
        private float Left()
        {
            return MathHelper.Clamp(leaveTime / BattleRules.UiProfileCarry, 0f, 1f);
        }

        //Words : the page's words and buttons, one piece after another once the painting is on its way. They leave first,
        //and fade in again after the arrows (Arrived alone is how far they have slid in).
        private float Words(float start)
        {
            return Arrived(start) * Math.Min(1f, 0.25f + swap * 4f);
        }

        private float Arrived(float start)
        {
            float a = UiMotion.Between(time, 0.22f + start, BattleRules.UiEnterTime);
            if (phase == Phase.Leave) a *= 1f - MathHelper.Clamp(leaveTime / (BattleRules.UiProfileCarry * 0.5f), 0f, 1f);
            return a;
        }

        //Painting At : where the painting of the conductor on show is this frame. Carried in from its gallery panel, carried
        //back to it, or slid in by the arrows. The size never changes, only the place.
        private Rectangle PaintingAt()
        {
            Rectangle home = ConductorSelectScreen.PaintingRect(index);
            if (phase == Phase.Leave) return Lerp(paintBox, home, UiMotion.EaseInOut(Left()));
            Rectangle r = Lerp(home, paintBox, UiMotion.EaseInOut(MathHelper.Clamp(time / BattleRules.UiProfileCarry, 0f, 1f)));
            if (swapFrom >= 0 && swap < BattleRules.UiProfileSwap)
                r.X += (int)Math.Round((1f - UiMotion.EaseOut(swap / BattleRules.UiProfileSwap)) * 140f * swapWay);
            return r;
        }

        //Rect Lerp : blend two rectangles of the same size, kept on whole pixels so the pixel art stays sharp
        private static Rectangle Lerp(Rectangle a, Rectangle b, float t)
        {
            return new Rectangle((int)Math.Round(MathHelper.Lerp(a.X, b.X, t)), (int)Math.Round(MathHelper.Lerp(a.Y, b.Y, t)), a.Width, a.Height);
        }

        //======================================================================================
        //The World (THE LENS) : the room behind the painting, and the painting
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
            float room = Room();
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.Void);
            if (room > 0f) DrawScene(sb, room);
            for (int i = 0; i < auras.Length; i++) auras[i].DrawBack(sb);
            for (int i = 0; i < auras.Length; i++) auras[i].DrawMiddle(sb);

            //Painting : the one sliding out first, then the one on show
            if (swapFrom >= 0 && swap < BattleRules.UiProfileSwap)
            {
                float t = UiMotion.EaseOut(swap / BattleRules.UiProfileSwap);
                Rectangle away = paintBox;
                away.X -= (int)Math.Round(t * 140f * swapWay);
                DrawPainting(sb, ConductorList.Shown[swapFrom], away, 1f - t, room);
            }
            float come = swapFrom >= 0 ? Math.Min(1f, swap / BattleRules.UiProfileSwap) : 1f;
            DrawPainting(sb, c, PaintingAt(), come, room);
            for (int i = 0; i < auras.Length; i++) auras[i].DrawFront(sb);
        }

        //Painting : the conductor's painting at the size it was painted, or the empty slot that waits for it
        private static void DrawPainting(SpriteBatch sb, Conductor who, Rectangle r, float alpha, float room)
        {
            Texture2D art = ArtBank.GalleryOf(who);
            if (art != null) sb.Draw(art, new Vector2(r.X + (r.Width - art.Width) / 2, r.Y), Color.White * alpha);
            else ArtSlot.Draw(sb, r, Palette.Paper, alpha * room);
        }

        //Scene : the space behind the painting, built in layers from far to near, all of it fading with the room
        private void DrawScene(SpriteBatch sb, float e)
        {
            //Layer 1 : the dark ground
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageDeep * e);

            //Layer 2 : parallax pillars. Three sets drifting at different speeds is enough to read as depth.
            for (int layer = 0; layer < 3; layer++)
            {
                float spacing = 300f - layer * 90f;
                float drift = (time * (5f + layer * 9f)) % spacing;
                float bright = (0.022f + layer * 0.015f) * e;
                for (float x = -spacing + drift; x < TacetGame.ScreenW; x += spacing)
                    Gfx.Rect(sb, x, 0, 4f - layer, TacetGame.ScreenH, Palette.Paper * bright);
            }

            //Layer 3 : the stave the painting stands on, notation instead of a plain floor line
            Ornament.Stave(sb, 0, 548, TacetGame.ScreenW, 9, Palette.Paper * (0.11f * e));

            //Layer 4 : sound ribbons, sine waves crossing the shot, music made visible
            for (int r = 0; r < 3; r++)
            {
                float baseY = 210f + r * 130f;
                float amplitude = 34f + r * 22f;
                float frequency = 0.0055f + r * 0.0018f;
                float speed = 0.55f + r * 0.22f;
                float alpha = (0.09f - r * 0.022f) * e;
                Vector2 previous = new Vector2(0f, baseY + (float)Math.Sin(time * speed) * amplitude);
                for (int x = 12; x <= TacetGame.ScreenW; x += 12)
                {
                    Vector2 now = new Vector2(x, baseY + (float)Math.Sin(x * frequency + time * speed) * amplitude);
                    Gfx.Line(sb, previous, now, Palette.Paper * alpha, 2f);
                    previous = now;
                }
            }

            //Layer 5 : the backlight, a pale pool behind the painting with the conductor's colour in it, so the painting
            //stands out from the dark
            Gfx.DrawGlow(sb, 640, 300, 360, Palette.Paper * (0.13f * e));
            Gfx.DrawGlow(sb, 640, 330, 240, c.SpotAt(time) * (0.16f * e));
            Ornament.Rays(sb, 640, 250, 190, 430, 24, time * 0.02f, Palette.Paper * (0.03f * e));

            //Layer 6 : shade behind the two text columns so the words stay readable
            Gfx.DrawGlowBox(sb, new Rectangle(-260, 0, 800, TacetGame.ScreenH), Color.Black * (0.55f * e));
            Gfx.DrawGlowBox(sb, new Rectangle(760, 0, 800, TacetGame.ScreenH), Color.Black * (0.55f * e));
        }

        //Lights : the room's light (Lens.Ambient), so the painting looks as it was drawn, and the AURA's own light on it
        public override void DrawLights(SpriteBatch sb)
        {
            for (int i = 0; i < auras.Length; i++) auras[i].DrawLights(sb);
        }

        //Glow : the conductor's colour glowing at the foot of the painting
        public override void DrawGlow(SpriteBatch sb)
        {
            float e = Room() * Math.Min(1f, 0.25f + swap * 4f);
            if (e <= 0f) return;                      // no room yet, so no aura either (it rises with the room)
            Rectangle r = PaintingAt();
            Gfx.DrawGlowBox(sb, new Rectangle(r.X - 70, r.Bottom - 70, r.Width + 140, 130), c.SpotAt(time) * (0.45f * e));
            for (int i = 0; i < auras.Length; i++) auras[i].DrawGlow(sb);
        }

        //======================================================================================
        //The Words (crisp on top)
        //======================================================================================

        public override void Draw(SpriteBatch sb)
        {
            DrawLeftColumn(sb);
            DrawRightColumn(sb);

            //Under the painting : the signature's marking, as it would be written in a score
            float mark = Words(0.2f);
            Gfx.TextCentered(sb, Game.StoryFont, c.SignatureMark, TacetGame.ScreenW / 2f, 576, Palette.PaperDim * mark, TextSize.Story);

            //Buttons
            float low = Words(0.3f);
            Ui.Command(sb, chooseButton, "TAKE THE BATON", "ENTER", chooseMotion, low, false);
            Ui.Command(sb, backButton, "BACK", "ESC", backMotion, low, false);
            DrawArrow(sb, lastButton, false, index > 0, lastMotion, low);
            DrawArrow(sb, nextButton, true, index < ConductorList.Shown.Length - 1, nextMotion, low);
            Ui.ModeSwitch(sb, modeBox, Settings.Maestro, Input.MouseOver(modeBox), low);

            DrawLetterbox(sb);
        }

        //Letterbox : two black bars, the cheapest way to make a screen feel like a film shot
        private void DrawLetterbox(SpriteBatch sb)
        {
            float a = Words(0f);
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, LetterboxH, Palette.Void);
            Gfx.Rect(sb, 0, TacetGame.ScreenH - LetterboxH, TacetGame.ScreenW, LetterboxH, Palette.Void);
            Gfx.Rect(sb, 0, LetterboxH, TacetGame.ScreenW, 1, Palette.LineGrey * (0.45f * a));
            Gfx.Rect(sb, 0, TacetGame.ScreenH - LetterboxH - 1, TacetGame.ScreenW, 1, Palette.LineGrey * (0.45f * a));

            Gfx.TextSpaced(sb, Game.Font, profileLabel, LeftX, 14, Palette.LineGrey * a, TextSize.Tiny, 3f);
            Gfx.TextSpacedRight(sb, Game.Font, "TACET  4'33", TacetGame.ScreenW - LeftX, 14, Palette.LineGrey * a, TextSize.Tiny, 3f);
            Ornament.Crosses(sb, TacetGame.ScreenW - 180, TacetGame.ScreenH - 20, 5, 1, 14, Palette.LineGrey * a);
        }

        //Left Column : the name and line, the face close up with the record beside it, and who they were
        private void DrawLeftColumn(SpriteBatch sb)
        {
            float a = Words(0f);
            float b = Words(0.06f);
            float d = Words(0.12f);
            float f = Words(0.18f);
            float slide = BattleRules.UiEnterSlide;

            //Name and Line (NAME FIT : a second line pushes the rest of the column down by nameDrop)
            float drop = nameDrop;
            Gfx.Text(sb, Game.BigFont, nameTop, LeftX - (1f - Arrived(0f)) * slide, 54, Palette.Highlight * a, nameScale);
            if (nameLow.Length > 0)
                Gfx.Text(sb, Game.BigFont, nameLow, LeftX - (1f - Arrived(0.03f)) * slide, 54 + drop, Palette.Highlight * a, nameScale);
            Gfx.Text(sb, Game.StoryFont, epithetWrapped, LeftX + 2 - (1f - Arrived(0.06f)) * slide, 108 + drop, Palette.PaperDim * b, TextSize.Story);
            Gfx.Rect(sb, LeftX, 160 + drop, LeftW * Arrived(0.06f), 1, Palette.LineGrey * b);
            Gfx.Circle(sb, LeftX, 160 + drop, 3f, c.SpotAt(time) * b);

            //Face : the close up, as it was drawn, or the empty slot that waits for it
            Rectangle face = faceBox;
            face.Y += (int)Math.Round(drop);
            face.X -= (int)Math.Round((1f - Arrived(0.12f)) * slide);
            Gfx.Rect(sb, face, Palette.CanvasDark * d);
            ArtBank.DrawOrSlot(sb, ArtBank.FaceOf(c), face, Palette.Paper, d);
            Ornament.CornerBrackets(sb, new Rectangle(face.X - 4, face.Y - 4, face.Width + 8, face.Height + 8), 12, Palette.PaperDim * d, 1);

            //Record : beside the face, a label over each value like a record card
            float rx = faceBox.Right + 22;
            float rw = LeftX + LeftW - rx;
            float ry = faceBox.Y + drop;
            DrawRecord(sb, "ROLE", rx, ry + 4, rw, d);
            Gfx.TextSpaced(sb, Game.Font, c.Role, rx, ry + 24, Palette.Paper * d, TextSize.Body, 2f);
            DrawRecord(sb, "BASED ON", rx, ry + 74, rw, d);
            Gfx.Text(sb, Game.StoryFont, basedOnWrapped, rx, ry + 92, Palette.Paper * d, TextSize.Story);

            //Who They Were
            float wy = faceBox.Bottom + drop;
            Gfx.TextSpaced(sb, Game.Font, "WHO THEY WERE", LeftX, wy + 18, Palette.LineGrey * f, TextSize.Tiny, 3f);
            Gfx.Text(sb, Game.StoryFont, storyWrapped, LeftX, wy + 38, Palette.PaperDim * f, TextSize.Story);
        }

        //Record : a small spaced label with a thin line under it
        private static void DrawRecord(SpriteBatch sb, string label, float x, float y, float width, float a)
        {
            Gfx.TextSpaced(sb, Ui.Font, label, x, y, Palette.LineGrey * a, TextSize.Tiny, 2f);
            Gfx.Rect(sb, x, y + 16, width, 1, Palette.LineGrey * (0.35f * a));
        }

        //Right Column : how they play. The PASSIVE is always on, the SIGNATURE is what SPACE lets loose once the gauge is full.
        private void DrawRightColumn(SpriteBatch sb)
        {
            float a = Words(0.04f);
            float b = Words(0.1f);
            float d = Words(0.16f);
            float slide = BattleRules.UiEnterSlide;

            //Section 1 : Passive
            int y = 62;
            float sx = RightX + (1f - Arrived(0.04f)) * slide;
            DrawSection(sb, "I", "PASSIVE  /  ALWAYS ON", sx, y, a);
            Gfx.Text(sb, Game.BigFont, c.MechanicName, sx, y + 40, Palette.Highlight * a, TextSize.Subtitle);
            Gfx.Text(sb, Game.StoryFont, c.MechanicWrapped, sx, y + 76, Palette.Paper * a, TextSize.Story);

            //Section 2 : Signature, with the key that lets it loose. Their own move, so their own colour.
            y = 262;
            sx = RightX + (1f - Arrived(0.1f)) * slide;
            DrawSection(sb, "II", "SIGNATURE", sx, y, b);
            Ui.KeyBox(sb, "SPACE", sx + RightW - 70, y + 13, Palette.Paper, Palette.Ink, b);
            Gfx.Text(sb, Game.BigFont, c.SignatureName, sx, y + 40, Color.Lerp(Palette.Highlight, c.SpotAt(time), 0.75f) * b, TextSize.Subtitle);
            Gfx.Text(sb, Game.StoryFont, c.SignatureWrapped, sx, y + 76, Palette.Paper * b, TextSize.Story);

            //Turn Battle : the gauge fills with PERFECTs, the same for every conductor (Alpha 7)
            int ny = y + 178;
            sx = RightX + (1f - Arrived(0.16f)) * slide;
            if (BattleRules.TurnBattleOn)
            {
                Gfx.TextSpaced(sb, Game.Font, "NEEDS", sx, ny + 5, Palette.LineGrey * d, TextSize.Tiny, 2f);
                int need = BattleRules.PlaytestSignatureNeed > 0 ? BattleRules.PlaytestSignatureNeed : BattleRules.TurnSignatureNeed;   // PLAYTEST
                Gfx.Text(sb, Game.Font, NumberText.Get(need), sx + 70, ny, Palette.Paper * d, TextSize.Body);
                Gfx.TextSpaced(sb, Game.Font, "PERFECT", sx + 92, ny + 5, Palette.Paper * d, TextSize.Tiny, 2f);
                Gfx.TextSpaced(sb, Game.Font, "PERFECT HITS AND BLOCKS FILL IT", sx, ny + 32, Palette.LineGrey * d, TextSize.Tiny, 2f);
                return;
            }

            //Recipe : NEEDS, then a number and the section's name for each family it asks for (the old duel)
            Gfx.TextSpaced(sb, Game.Font, "NEEDS", sx, ny + 5, Palette.LineGrey * d, TextSize.Tiny, 2f);
            float rx = sx + 70;
            for (int f = 0; f < c.Recipe.Length; f++)
            {
                if (c.Recipe[f] <= 0) continue;
                Gfx.Text(sb, Game.Font, NumberText.Get(c.Recipe[f]), rx, ny, Palette.Paper * d, TextSize.Body);
                string section = StageLayout.Rows[StageLayout.SectionOf((Family)f)].Name;
                Gfx.TextSpaced(sb, Game.Font, section, rx + 22, ny + 5, Palette.Paper * d, TextSize.Tiny, 2f);
                rx += 22 + Gfx.TextWidth(Game.Font, section, TextSize.Tiny) + section.Length * 2f + 26;
            }
            //Where the notes come from : PERFECT beats, and GOOD ones too for BY THE BOOK
            string from = c.Perk == ConductorPerk.ByTheBook ? "NOTES FROM PERFECT OR GOOD BEATS" : "NOTES FROM PERFECT BEATS";
            Gfx.TextSpaced(sb, Game.Font, from, sx, ny + 32, Palette.LineGrey * d, TextSize.Tiny, 2f);
        }

        //Section : a roman numeral, the section's name in spaced capitals, and a thin rule with a circle on it (menu3)
        private void DrawSection(SpriteBatch sb, string numeral, string title, float x, int y, float a)
        {
            Gfx.Text(sb, Game.BigFont, numeral, x, y, Palette.PaperDim * a, TextSize.Small);
            Gfx.TextSpaced(sb, Game.Font, title, x + 40, y + 7, Palette.Highlight * a, TextSize.Label, 4f);
            Gfx.Rect(sb, x, y + 30, RightW - 10, 1, Palette.LineGrey * (0.6f * a));
            Gfx.CircleOutline(sb, x + RightW - 10, y + 30, 3f, Palette.LineGrey * a, 1f);
            Gfx.Circle(sb, x, y + 30, 2.5f, c.SpotAt(time) * a);
        }

        //Arrow : a ring with an arrow inside, for the next and the last profile. Pointed, it fills and sends out a ring.
        //Dim at the ends.
        private void DrawArrow(SpriteBatch sb, Rectangle box, bool pointRight, bool enabled, UiMotion m, float a)
        {
            float e = enabled ? m.Shown : 0f;
            float s = m.Scale;
            Color line = enabled ? Color.Lerp(Palette.Paper, Palette.Highlight, e) : Palette.LineGrey * 0.5f;
            float cx = box.Center.X + (pointRight ? 3f : -3f) * e;
            float cy = box.Center.Y;
            float radius = 22f * s;
            Gfx.Circle(sb, cx, cy, radius, Color.Lerp(Palette.Void * 0.8f, Palette.Paper, e) * a);
            Gfx.CircleOutline(sb, cx, cy, radius, line * a, 1.5f);
            if (enabled && m.Ring < 1f) Gfx.CircleOutline(sb, cx, cy, radius + 18f * m.Ring, Palette.Paper * ((1f - m.Ring) * 0.8f * a), 1.5f);
            Gfx.Arrow(sb, cx + (pointRight ? 2f : -2f), cy, 8f * s, pointRight, Color.Lerp(line, Palette.Ink, e) * a);
        }
    }
}
