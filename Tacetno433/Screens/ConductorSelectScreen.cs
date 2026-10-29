using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //ConductorSelectScreen : THE GALLERY, where a run begins. The stage curtain opens on it.
    //
    //Beta1 screens round 2 (29 Oct, docs/SCREENS_LEVEL_UP.md 16) : the three conductors the user kept (ConductorList.Shown)
    //stand side by side as tall slanted panels, each cut from its painting (menu6). The paintings are pixel art drawn at the
    //size they are shown (Content/Art/Conductors/gallery_<name>.png, 302 x 402), so they are never stretched.
    //   POINT        the panel straightens into the whole painting and steps up into its own light, its numeral turns the
    //                conductor's colour (Conductor.Spot) and that colour rises from the bottom of the panel. The other two
    //                sink back into the dark. The mouse points only when it moves, so a resting mouse never fights the keys.
    //   STEP INSIDE  a click or ENTER : the other panels fall away into the dark, then the profile page takes the painting
    //                from where it stands (SHARED PICTURE, ScreenManager.Cut) and carries it to the middle of its shot.
    //   RETURN       back from the profile, the painting is set down in its panel and the gallery comes back around it.
    public class ConductorSelectScreen : GameScreen
    {
        //Can Pause : a menu page outside the run, ESC here means going back
        public override bool CanPause
        {
            get { return false; }
        }

        //Uses Curtain : the gallery is on the stage behind the curtain, so the stage curtain opens on it
        public override bool UsesCurtain
        {
            get { return true; }
        }

        //Gallery Layout : move these numbers to reposition everything
        public const int PaintW = 302;                // the painting's size, as it was drawn (docs/UI_ART_PLAN.md 2.3)
        public const int PaintH = 402;
        private const int PanelsY = 132;              // the top of the panels
        private const int PanelGap = 36;
        private const int SideRoom = 110;             // the least room left at each side of the row
        private const int Lift = 8;                   // a pointed panel steps up this far (pixels)
        private const int FloorY = 604;               // where the wall meets the floor
        private const int CenterX = TacetGame.ScreenW / 2;
        private static readonly string[] numerals = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX" };

        //Gallery Buttons
        private Rectangle backButton = new Rectangle(36, 650, 170, 42);
        private static Rectangle modeBox = new Rectangle(1010, 634, 234, 38);   // NORMAL or MAESTRO, MAESTRO MODE

        //Gallery State
        private enum Phase { Look, Walk }
        private Phase phase = Phase.Look;
        private int index;                            // the conductor pointed at
        private float[] lit = new float[0];           // 0..1 for each panel, how far it is pointed at (straight, lit, coloured)
        private bool returning;                       // back from the profile page, the painting already in its panel
        private float time;
        private float walkTime;
        private float sincePoint = 99f;               // seconds since the pointed panel changed, the line under the row fades in again
        private Vector2 mouseWas;
        private UiMotion backMotion = new UiMotion();

        public ConductorSelectScreen() : this(0, false)
        {
        }

        //Gallery Return : stand at this conductor
        public ConductorSelectScreen(int startIndex) : this(startIndex, false)
        {
        }

        //returning : the profile page has set the painting down in its panel (SHARED PICTURE), the gallery comes back round it
        public ConductorSelectScreen(int startIndex, bool returning)
        {
            index = startIndex;
            this.returning = returning;
        }

        public override void Load()
        {
            int n = ConductorList.Shown.Length;
            index = Math.Max(0, Math.Min(n - 1, index));
            lit = new float[n];
            if (returning) lit[index] = 1f;               // the painting comes back straight, as the profile page held it
            mouseWas = Input.MousePos;
            SoundBank.PlayMusic(Music.Gallery);

            //The Lens : a dim room, every panel lit by its own beam (DrawLights), corners dark like a stage
            Lens.Ambient = returning ? HandAmbient : RoomAmbient;
            Lens.SpotPower = 0f;
            Lens.Vignette = 0.5f;
        }

        //Ambient : the room's light, and the light the painting has while it passes between this page and the profile
        //(as drawn, the same on both pages, so the picture never changes brightness when the pages swap)
        private const float RoomAmbient = 0.34f;
        private const float HandAmbient = 0.5f;

        //Panel Rect : where panel i stands (not pointed, not moving). Three fit at the painting's own width, more get narrower
        //and show the middle of their painting (the old duel's gallery of nine).
        public static Rectangle PanelRect(int i)
        {
            int n = ConductorList.Shown.Length;
            int w = Math.Min(PaintW, (TacetGame.ScreenW - SideRoom * 2 - (n - 1) * PanelGap) / Math.Max(1, n));
            int total = n * w + (n - 1) * PanelGap;
            return new Rectangle((TacetGame.ScreenW - total) / 2 + i * (w + PanelGap), PanelsY, w, PaintH);
        }

        //Painting Rect : where the whole painting of conductor i stands when its panel is pointed at, straight and stepped
        //up. The profile page takes it from here and sets it back here (SHARED PICTURE).
        public static Rectangle PaintingRect(int i)
        {
            Rectangle panel = PanelRect(i);
            return new Rectangle(panel.Center.X - PaintW / 2, PanelsY - Lift, PaintW, PaintH);
        }

        public override void Update(float dt)
        {
            time += dt;
            sincePoint += dt;
            int n = ConductorList.Shown.Length;

            //Point : each panel eases toward pointed or not
            float step = dt / BattleRules.UiGalleryPoint;
            for (int i = 0; i < n; i++)
                lit[i] = i == index ? Math.Min(1f, lit[i] + step) : Math.Max(0f, lit[i] - step);

            //Lens : the room light goes up to the hand over's light while the painting passes between the pages
            Lens.Ambient = MathHelper.Lerp(RoomAmbient, HandAmbient, HandOver());

            //Step Inside : once the others have gone, the profile page takes the painting
            if (phase == Phase.Walk)
            {
                walkTime += dt;
                if (walkTime >= BattleRules.UiGalleryWalk) Game.Screens.Cut(new ConductorDetailScreen(index));
                return;
            }

            backMotion.Update(dt, Input.MouseOver(backButton));

            //Point With The Mouse : only when it moves. A pointer that has not entered the window yet reads (0, 0), so its
            //first report is not a move.
            bool moved = mouseWas != Vector2.Zero && Vector2.DistanceSquared(Input.MousePos, mouseWas) > 1f;
            mouseWas = Input.MousePos;
            for (int i = 0; i < n; i++)
            {
                if (!Input.MouseOver(PanelRect(i))) continue;
                if (moved && i != index) Point(i);
                if (Input.MouseClicked())
                {
                    if (i != index) Point(i);
                    StepInside();
                    return;
                }
            }

            //Point With The Keys
            if (Input.KeyPressed(Keys.Right) || Input.KeyPressed(Keys.D)) Step(1);
            if (Input.KeyPressed(Keys.Left) || Input.KeyPressed(Keys.A)) Step(-1);
            if (Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Space))
            {
                StepInside();
                return;
            }

            //Gallery Mode : NORMAL or MAESTRO for the run about to start
            if (Input.ClickedOn(modeBox) || Input.KeyPressed(Keys.M)) FlipMode();

            //Gallery Back
            if (Input.KeyPressed(Keys.Escape) || Input.ClickedOn(backButton))
            {
                backMotion.Press();
                SoundBank.Play(Sfx.UiBack);
                Game.Screens.Change(new TitleScreen());
            }
        }

        //Point : another panel is pointed at
        private void Point(int i)
        {
            index = i;
            sincePoint = 0f;
            SoundBank.Play(Sfx.UiMove);
        }

        //Step : the keys point one panel along, stopping at the ends
        private void Step(int direction)
        {
            int next = index + direction;
            if (next < 0 || next > ConductorList.Shown.Length - 1)
            {
                SoundBank.Play(Sfx.UiDenied);
                return;
            }
            Point(next);
        }

        //Step Inside : the others fall away, then the profile page takes the painting (Update)
        private void StepInside()
        {
            SoundBank.Play(Sfx.UiConfirm);
            phase = Phase.Walk;
            walkTime = 0f;
        }

        //Flip Mode : MAESTRO MODE on or off, remembered in the settings file for the next run.
        //The profile page uses it too, the switch sits on both conductor pages.
        public static void FlipMode()
        {
            Settings.Maestro = !Settings.Maestro;
            SaveFile.SaveSettings();
            SoundBank.Play(Sfx.UiMove);
        }

        //======================================================================================
        //Timing : how far everything has come in, or gone
        //======================================================================================

        //Walked : 0..1 through the step inside
        private float Walked()
        {
            return MathHelper.Clamp(walkTime / BattleRules.UiGalleryWalk, 0f, 1f);
        }

        //Hand Over : 0 in the gallery, 1 at the moment the painting passes between this page and the profile page. Then the
        //room is black around it and it is lit as drawn, exactly as the profile page shows it on its first frame.
        private float HandOver()
        {
            if (phase == Phase.Walk) return UiMotion.EaseInOut(Walked());
            if (returning) return 1f - UiMotion.EaseInOut(MathHelper.Clamp(time / BattleRules.UiProfileCarry, 0f, 1f));
            return 0f;
        }

        //Panel Alpha : how far panel i is there. Fresh from the curtain they rise in one after another, coming back from the
        //profile the others gather round the painting, stepping inside they fall away.
        private float PanelAlpha(int i)
        {
            float a = Arrived(i);
            if (phase == Phase.Walk) a = i == index ? Math.Max(a, Walked()) : a * (1f - UiMotion.EaseInOut(Walked()));
            return a;
        }

        //Panel Drop : how far below its place panel i is, rising in or falling away (pixels). The painting being handed over
        //is always in its place by the time the profile page takes it.
        private float PanelDrop(int i)
        {
            float drop = (1f - Arrived(i)) * 34f;
            if (phase == Phase.Walk) drop = i == index ? drop * (1f - Walked()) : drop + Walked() * Walked() * 46f;
            return drop;
        }

        //Arrived : 0..1, how far panel i has come in. Fresh from the curtain they rise one after another, back from the
        //profile the others gather round the painting that is already there.
        private float Arrived(int i)
        {
            if (returning) return i == index ? 1f : UiMotion.Between(time, 0.12f + 0.08f * Math.Abs(i - index), 0.45f);
            return UiMotion.Between(time, 0.18f + 0.09f * i, 0.5f);
        }

        //Pointed : how far panel i is pointed at, eased. The painting being handed over is always fully pointed.
        private float Pointed(int i)
        {
            float e = UiMotion.EaseOut(lit[i]);
            if (i == index) e = Math.Max(e, HandOver());
            return e;
        }

        //Words : the page's words and buttons. They come in after the panels, and leave first when stepping inside.
        private float Words(float start)
        {
            float a = returning ? UiMotion.Between(time, 0.1f + start * 0.5f, BattleRules.UiEnterTime)
                                : UiMotion.Between(time, 0.3f + start, BattleRules.UiEnterTime);
            if (phase == Phase.Walk) a *= 1f - MathHelper.Clamp(walkTime / (BattleRules.UiGalleryWalk * 0.6f), 0f, 1f);
            return a;
        }

        //Shown Rect : panel i as it stands this frame, risen in or fallen, stepped up when pointed
        private Rectangle ShownRect(int i)
        {
            Rectangle r = PanelRect(i);
            r.Y += (int)Math.Round(PanelDrop(i) - Lift * Pointed(i));
            return r;
        }

        //======================================================================================
        //The World (THE LENS) : a dark stage, a beam on every panel
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
            float room = 1f - HandOver();

            //Room : a dark back wall and the floor, with thin boards running toward us
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.Void);
            if (room > 0f)
            {
                Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, FloorY, Palette.StageDeep * room);
                Gfx.Rect(sb, 0, FloorY, TacetGame.ScreenW, 1, Palette.LineGrey * (0.3f * room));
                for (int k = -8; k <= 8; k++)
                    Gfx.Line(sb, CenterX + k * 46, FloorY, CenterX + k * 170, TacetGame.ScreenH, Palette.Paper * (0.03f * room), 1f);
            }

            //Panels : the pointed one last, so it stands over the others' shadows
            for (int i = 0; i < ConductorList.Shown.Length; i++)
                if (i != index) DrawPanel(sb, i);
            DrawPanel(sb, index);
        }

        //Panel : the shadow, the plate, the painting cut by the slanted window, the colour rising and the edge
        private void DrawPanel(SpriteBatch sb, int i)
        {
            float a = PanelAlpha(i);
            if (a <= 0f) return;
            Conductor c = ConductorList.Shown[i];
            float e = Pointed(i);
            float hand = i == index ? HandOver() : 0f;
            Rectangle r = ShownRect(i);
            float slant = BattleRules.UiGallerySlant * (1f - e);

            //Shadow : soft dark behind and under, so the panel stands off the wall
            Gfx.DrawGlowBox(sb, new Rectangle(r.X - 30, r.Y + 24, r.Width + 60, r.Height + 50), Color.Black * (0.6f * a * (1f - hand)));

            //Plate and Painting : the plate shows where the painting is see through. Handed over, the plate is the black
            //of the profile page, the painting lit as drawn.
            CutRows(sb, null, r, slant, Color.Lerp(Palette.Panel, Palette.Void, hand) * a);
            Texture2D art = ArtBank.GalleryOf(c);
            if (art != null) CutRows(sb, art, r, slant, Color.White * (MathHelper.Lerp(0.72f, 1f, e) * a));
            else EmptySlot(sb, r, slant, a * (1f - hand));

            //Colour Rise : the conductor's own colour, up from the bottom (menu6)
            float rise = e * (1f - hand);
            if (rise > 0f) ColourRise(sb, r, slant, c.Spot, rise, a);

            //Edge : a thin line round the window, bright when pointed
            SlantEdge(sb, r, slant, Color.Lerp(Palette.LineGrey * 0.5f, Palette.Paper, e) * (a * (1f - hand)));
        }

        //Cut Rows : a picture (or a plain plate when art is null) cut by a panel's slanted window, the picture in the middle
        //of the panel. ADVANCED PART (small) : a sprite can only be cut by a straight rectangle (its source rectangle), so the
        //slanted cut is made of rows : each row of the picture is drawn on its own, from where the slanted window crosses it
        //to where it leaves it. A row is one pixel of the picture, so the slanted edge steps like pixel art and the painting
        //stays exactly the size it was painted. The window leans like Gfx.SlantBox : its top is slant pixels right of its foot.
        public static void CutRows(SpriteBatch sb, Texture2D art, Rectangle r, float slant, Color color)
        {
            int artW = art != null ? art.Width : r.Width;
            int artX = r.Center.X - artW / 2;
            int rows = art != null ? Math.Min(r.Height, art.Height) : r.Height;
            for (int j = 0; j < rows; j++)
            {
                float lean = slant * (1f - (j + 0.5f) / r.Height);
                int from = Math.Max(artX, (int)Math.Round(r.X + lean));
                int to = Math.Min(artX + artW, (int)Math.Round(r.X + lean + r.Width - slant));
                if (to <= from) continue;
                if (art != null) sb.Draw(art, new Vector2(from, r.Y + j), new Rectangle(from - artX, j, to - from, 1), color);
                else sb.Draw(Gfx.Pixel, new Vector2(from, r.Y + j), null, color, 0f, Vector2.Zero, new Vector2(to - from, 1f), SpriteEffects.None, 0f);
            }
        }

        //Colour Rise : a conductor's colour rising from the bottom of the panel (menu6), strongest at the foot. Rows two pixels
        //tall like the ink bars (PIXEL x2). rise 0..1 is how far it has come up.
        private static void ColourRise(SpriteBatch sb, Rectangle r, float slant, Color color, float rise, float alpha)
        {
            int tall = (int)(r.Height * BattleRules.UiGalleryRise * rise);
            for (int k = 0; k < tall; k += 2)
            {
                int j = r.Height - 2 - k;
                float t = 1f - (float)k / Math.Max(1, tall);
                float lean = slant * (1f - (j + 1f) / r.Height);
                sb.Draw(Gfx.Pixel, new Vector2(r.X + lean, r.Y + j), null, color * (0.42f * t * t * alpha), 0f, Vector2.Zero,
                        new Vector2(r.Width - slant, 2f), SpriteEffects.None, 0f);
            }
        }

        //Slant Edge : the four sides of a slanted window
        private static void SlantEdge(SpriteBatch sb, Rectangle r, float slant, Color color)
        {
            float w = r.Width - slant;
            Gfx.Line(sb, r.X + slant, r.Y, r.X + slant + w, r.Y, color, 1f);
            Gfx.Line(sb, r.X, r.Bottom, r.X + w, r.Bottom, color, 1f);
            Gfx.Line(sb, r.X + slant, r.Y, r.X, r.Bottom, color, 1f);
            Gfx.Line(sb, r.X + slant + w, r.Y, r.X + w, r.Bottom, color, 1f);
        }

        //Empty Slot : the panel of a conductor whose painting is not drawn yet. Like ArtSlot it draws nothing inside on
        //purpose : a faint fill and a mark at every corner of the slanted window.
        private static void EmptySlot(SpriteBatch sb, Rectangle r, float slant, float a)
        {
            CutRows(sb, null, r, slant, Palette.Paper * (0.04f * a));
            float w = r.Width - slant;
            Vector2 topLeft = new Vector2(r.X + slant, r.Y);
            Vector2 topRight = new Vector2(r.X + slant + w, r.Y);
            Vector2 footLeft = new Vector2(r.X, r.Bottom);
            Vector2 footRight = new Vector2(r.X + w, r.Bottom);
            Color mark = Palette.Paper * (0.6f * a);
            Corner(sb, topLeft, topRight, footLeft, mark);
            Corner(sb, topRight, topLeft, footRight, mark);
            Corner(sb, footLeft, footRight, topLeft, mark);
            Corner(sb, footRight, footLeft, topRight, mark);
        }

        //Corner : two short strokes from a corner toward its two neighbours
        private static void Corner(SpriteBatch sb, Vector2 at, Vector2 toA, Vector2 toB, Color color)
        {
            Vector2 a = toA - at;
            Vector2 b = toB - at;
            a.Normalize();
            b.Normalize();
            Gfx.Line(sb, at, at + a * 16f, color, 1f);
            Gfx.Line(sb, at, at + b * 16f, color, 1f);
        }

        //Lights : a beam from above on every panel, the pointed one strong, the others faint. Handed over, the beams are gone
        //and only the room's light (HandAmbient) is left on the painting, as on the profile page.
        public override void DrawLights(SpriteBatch sb)
        {
            float hand = HandOver();
            for (int i = 0; i < ConductorList.Shown.Length; i++)
            {
                float a = PanelAlpha(i) * (1f - hand);
                if (a <= 0f) continue;
                Rectangle r = ShownRect(i);
                Beam(sb, r.Center.X, r.Bottom + 12f, r.Width * 0.66f, (0.24f + 0.36f * UiMotion.EaseOut(lit[i])) * a);
            }
        }

        //Beam : a cone of light from the top of the stage, two widths stacked so its middle is brighter, and the pool it
        //leaves on the floor
        private static void Beam(SpriteBatch sb, float cx, float foot, float halfWide, float power)
        {
            for (float y = 0f; y < foot; y += 4f)
            {
                float half = 46f + (y / foot) * (halfWide - 46f);
                Gfx.Rect(sb, cx - half, y, half * 2f, 4f, Color.White * (power * 0.55f));
                Gfx.Rect(sb, cx - half * 0.6f, y, half * 1.2f, 4f, Color.White * (power * 0.5f));
            }
            Gfx.DrawGlowBox(sb, new Rectangle((int)(cx - halfWide * 1.4f), (int)(foot + 20f), (int)(halfWide * 2.8f), 130), Color.White * (power * 0.9f));
        }

        //Glow : the pointed conductor's colour glowing at the foot of their panel
        public override void DrawGlow(SpriteBatch sb)
        {
            float hand = HandOver();
            for (int i = 0; i < ConductorList.Shown.Length; i++)
            {
                float e = UiMotion.EaseOut(lit[i]) * PanelAlpha(i) * (1f - hand);
                if (e <= 0f) continue;
                Rectangle r = ShownRect(i);
                Gfx.DrawGlowBox(sb, new Rectangle(r.X - 50, r.Bottom - 80, r.Width + 100, 150), ConductorList.Shown[i].Spot * (0.5f * e));
            }
        }

        //======================================================================================
        //The Words (crisp on top)
        //======================================================================================

        public override void Draw(SpriteBatch sb)
        {
            Conductor pointed = ConductorList.Shown[index];

            //Header : the spaced serif title over a thin orbit rule, a caption in each corner
            float head = Words(0f);
            float slide = (1f - head) * BattleRules.UiEnterSlide * 0.5f;
            Gfx.TextSpacedCentered(sb, Game.BigFont, "CHOOSE YOUR CONDUCTOR", CenterX, 18 - slide, Palette.Paper * head, TextSize.Small, 5f);
            Ornament.OrbitRule(sb, CenterX, 56, 180, Palette.LineGrey * head);
            Gfx.TextSpaced(sb, Game.Font, "THE GALLERY", 36, 30, Palette.LineGrey * head, TextSize.Tiny, 3f);
            Gfx.TextSpacedRight(sb, Game.Font, "SELECT ONE TO BEGIN", TacetGame.ScreenW - 36, 30, Palette.LineGrey * head, TextSize.Tiny, 3f);

            //Every Panel : a big thin numeral over its top corner, the name and the real composer under it
            for (int i = 0; i < ConductorList.Shown.Length; i++)
            {
                float a = PanelAlpha(i) * Words(0.05f + 0.04f * i);
                if (a <= 0f) continue;
                Conductor c = ConductorList.Shown[i];
                float e = UiMotion.EaseOut(lit[i]);
                Rectangle r = ShownRect(i);

                //Numeral : in the conductor's colour when pointed (menu6)
                Gfx.Text(sb, Game.LogoFont, numerals[Math.Min(i, numerals.Length - 1)], r.X - 16, r.Y - 66, Color.Lerp(Palette.LineGrey, c.Spot, e) * (0.9f * a), 0.8f);

                //Name : a stroke of the conductor's colour under it when pointed, and the key that steps inside
                float nameY = r.Bottom + 14;
                Gfx.Text(sb, Game.BigFont, c.Name, r.X + 2, nameY, Color.Lerp(Palette.PaperDim, Palette.Highlight, e) * a, TextSize.Subtitle);
                Gfx.TextSpaced(sb, Game.Font, c.BasedOn, r.X + 2, nameY + 32, Palette.LineGrey * a, TextSize.Tiny, 3f);
                if (e > 0f)
                {
                    float nameW = Gfx.TextWidth(Game.BigFont, c.Name, TextSize.Subtitle);
                    Ui.InkBar(sb, r.X, nameY + 25, (nameW + 6f) * e, 4f, 3f, c.Spot * a, 13 * i + 5, 0.6f);
                    Ui.KeyBox(sb, "ENTER", r.Right - 52, nameY + 12, Palette.Paper, Palette.Ink, a * e);
                }
            }

            //Pointed : their two skills and their line, under the row, fading in again when the pointed panel changes
            float line = Words(0.2f) * Math.Min(1f, sincePoint * 5f);
            Gfx.TextSpacedCentered(sb, Game.Font, pointed.SkillsLabel, CenterX, 612, Palette.Highlight * line, TextSize.Tiny, 3f);
            Gfx.TextCentered(sb, Game.StoryFont, pointed.Epithet, CenterX, 644, Palette.Paper * line, TextSize.Story);

            //Buttons and Hint
            float low = Words(0.25f);
            Ui.Command(sb, backButton, "BACK", "ESC", backMotion, low, false);
            Ui.ModeSwitch(sb, modeBox, Settings.Maestro, Input.MouseOver(modeBox), low);
            Gfx.TextSpacedCentered(sb, Game.Font, "CLICK A PAINTING TO STEP INSIDE", CenterX, 694, Palette.LineGrey * low, TextSize.Tiny, 3f);
        }
    }
}
