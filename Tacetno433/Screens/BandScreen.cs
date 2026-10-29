using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //BandScreen : THE BAND, opened from the route page.
    //
    //Beta1 (29 Oct, docs/BETA1_DIRECTION.md 3.2) : the band stands in a row of tall slanted columns (menu8, Core/BandColumn.cs).
    //The conductor's column is first, cut from their painting, then the band. A white slanted sheet closes the row on the right.
    //   POINT   the column steps up and the musician's own colour rises from its foot, the box below explains them
    //   DRAG    a column to another place : the two trade places. A fight takes the band in this order, the first one in
    //           front (RunState.Order, Lineup)
    //THE BAND OF THREE (29 Oct) : everybody plays every fight, so the stage seats, the BENCH and clicking someone on or off
    //are gone. Under each column : the weapon at its tier (WEAPON TIERS) and the tier's diamonds.
    //TAB, ENTER or the button goes back to the route. ESC opens the pause menu, like every page of a run.
    public class BandScreen : GameScreen
    {
        //Band Layout : the conductor and the band, centred left of the sheet
        private const int ColumnsTop = 132;
        private const int SideLeft = 40;
        private const int SideRight = 1146;            // the white sheet starts after this
        private const int ColumnW = 160;
        private const int Gap = 14;
        private const int GroupGap = 34;
        private static readonly Rectangle detailBox = new Rectangle(40, 512, 1106, 112);
        private Rectangle backButton = new Rectangle(900, 646, 246, 46);

        //Band State
        private Rectangle conductorRect;
        private Rectangle[] slotRects = new Rectangle[0];   // one place per member of the band, in the order
        private Musician[] owner = new Musician[0];         // who stands in each place
        private float[] lit = new float[0];
        private float conductorLit;
        private int hover = -2;                             // the place under the mouse : -1 the conductor, -2 nothing
        private int pressed = -1;                           // the place the mouse went down on
        private Vector2 pressedAt;
        private bool dragging;
        private float time;
        private UiMotion backMotion = new UiMotion();

        //Prepared Text : made when the page opens, never in Draw
        private string[] skillText = new string[0];         // each musician's two skills explained, by MusicianList index
        private string conductorText = "";

        //Wipe Word : the page's name in the wipe on the way here (WIPE WORD)
        public override string WipeWord
        {
            get { return "THE BAND"; }
        }

        public override void Load()
        {
            RunState run = Game.CurrentRun;
            SoundBank.PlayMusic(Music.Route);

            skillText = BandColumn.SkillText(Game.StoryFont);
            Conductor c = run.Conductor;
            conductorText = Gfx.WrapText(Game.StoryFont, c.MechanicName + " : " + c.MechanicText, 470, TextSize.StorySmall, 2) + "\n"
                          + Gfx.WrapText(Game.StoryFont, "SPACE  " + c.SignatureName + " : " + c.SignatureText, 470, TextSize.StorySmall, 2);

            Lens.Ambient = 0.46f;
            Lens.SpotPower = 0f;
            Lens.Vignette = 0.5f;
            Rebuild(run);
        }

        //Rebuild : the places and who stands in them, in the player's order, after every change
        private void Rebuild(RunState run)
        {
            run.RefreshLabels();
            run.SyncOrder();
            int places = run.Order.Count;
            int total = ColumnW + GroupGap + places * ColumnW + Math.Max(0, places - 1) * Gap;
            int left = SideLeft + Math.Max(0, (SideRight - SideLeft - total) / 2);
            conductorRect = new Rectangle(left, ColumnsTop, ColumnW, BandColumn.PictureH);

            if (slotRects.Length != places)
            {
                slotRects = new Rectangle[places];
                owner = new Musician[places];
                lit = new float[places];
            }
            int bandX = left + ColumnW + GroupGap;
            for (int p = 0; p < places; p++)
            {
                slotRects[p] = new Rectangle(bandX + p * (ColumnW + Gap), ColumnsTop, ColumnW, BandColumn.PictureH);
                owner[p] = run.Order[p];
            }
        }

        public override void Update(float dt)
        {
            time += dt;
            RunState run = Game.CurrentRun;

            //Hover : the place under the mouse, its column and the words under it
            hover = -2;
            if (Input.MouseOver(Tall(conductorRect))) hover = -1;
            for (int p = 0; p < slotRects.Length; p++)
                if (Input.MouseOver(Tall(slotRects[p]))) hover = p;

            float step = dt / BattleRules.UiGalleryPoint;
            for (int p = 0; p < lit.Length; p++)
            {
                bool on = dragging ? p == pressed : p == hover;
                lit[p] = on ? Math.Min(1f, lit[p] + step) : Math.Max(0f, lit[p] - step);
            }
            conductorLit = hover == -1 ? Math.Min(1f, conductorLit + step) : Math.Max(0f, conductorLit - step);
            backMotion.Update(dt, Input.MouseOver(backButton));

            //Press, Drag, Release : a press that moves is a drag, a drop on another place trades the two
            if (Input.MouseClicked() && hover >= 0)
            {
                pressed = hover;
                pressedAt = Input.MousePos;
                dragging = false;
            }
            if (pressed >= 0 && Input.MouseDown() && !dragging && Vector2.DistanceSquared(Input.MousePos, pressedAt) > 64f)
            {
                dragging = true;
                SoundBank.Play(Sfx.SeatPickUp);
            }
            if (pressed >= 0 && !Input.MouseDown())
            {
                if (dragging) Drop(run, pressed, PlaceAt(Input.MousePos.X));
                pressed = -1;
                dragging = false;
            }

            if (Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Tab) || Input.ClickedOn(backButton))
            {
                backMotion.Press();
                SoundBank.Play(Sfx.UiBack);
                Game.Screens.Change(new RouteScreen());
            }
        }

        //Tall : a column with the words under it, the whole thing the mouse can take
        private static Rectangle Tall(Rectangle r)
        {
            return new Rectangle(r.X, r.Y, r.Width, r.Height + BandColumn.WordsH);
        }

        //Place At : the place a dragged column would land on, the nearest along the row
        private int PlaceAt(float x)
        {
            int best = 0;
            float bestD = float.MaxValue;
            for (int p = 0; p < slotRects.Length; p++)
            {
                float d = Math.Abs(slotRects[p].Center.X - x);
                if (d < bestD) { bestD = d; best = p; }
            }
            return best;
        }

        //Drop : a dragged column lands on another place, the two trade places in the order
        private void Drop(RunState run, int from, int to)
        {
            if (to == from || from >= run.Order.Count || to >= run.Order.Count) return;
            Musician m = run.Order[from];
            run.Order[from] = run.Order[to];
            run.Order[to] = m;
            SoundBank.Play(Sfx.SeatDrop);
            Rebuild(run);
        }

        //Column In : ENTER, the columns come in one after another from the left
        private float ColumnIn(int i)
        {
            return UiMotion.Between(time, 0.1f + i * 0.05f, 0.4f);
        }

        //Shown : a place's window this frame, risen in, stepped up when pointed, or under the mouse while dragged
        private Rectangle Shown(int p)
        {
            Rectangle r = slotRects[p];
            float a = ColumnIn(p + 1);
            r.Y += (int)Math.Round((1f - a) * 30f - BandColumn.Lift * UiMotion.EaseOut(lit[p]));
            if (dragging && p == pressed) r.X += (int)Math.Round(Input.MousePos.X - pressedAt.X);
            return r;
        }

        //======================================================================================
        //The World (THE LENS) : the columns' pictures
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
            RunState run = Game.CurrentRun;
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageDeep);
            Gfx.DrawGlowBox(sb, new Rectangle(-100, 40, 1400, 560), Palette.Paper * 0.05f);

            //Conductor : the leader's column, cut from their painting
            float ca = ColumnIn(0);
            Rectangle cr = conductorRect;
            cr.Y += (int)Math.Round((1f - ca) * 30f - BandColumn.Lift * UiMotion.EaseOut(conductorLit));
            BandColumn.Picture(sb, cr, ArtBank.GalleryOf(run.Conductor), run.Conductor.Spot, UiMotion.EaseOut(conductorLit), ca);

            //The Band : the dragged one last so it goes over the others, an empty window where it was
            for (int p = 0; p < slotRects.Length; p++)
            {
                if (dragging && p == pressed) { BandColumn.Empty(sb, slotRects[p], ColumnIn(p + 1)); continue; }
                BandColumn.Picture(sb, Shown(p), UiArt.BustOf(owner[p]), BandColumn.SpotOf(owner[p]), UiMotion.EaseOut(lit[p]), ColumnIn(p + 1));
            }
            if (dragging && pressed >= 0)
                BandColumn.Picture(sb, Shown(pressed), UiArt.BustOf(owner[pressed]), BandColumn.SpotOf(owner[pressed]), 1f, 1f);
        }

        //Lights : a soft light on the column pointed at
        public override void DrawLights(SpriteBatch sb)
        {
            for (int p = 0; p < slotRects.Length; p++)
            {
                float e = UiMotion.EaseOut(lit[p]);
                if (e <= 0f) continue;
                Rectangle r = Shown(p);
                Gfx.DrawGlow(sb, r.Center.X, r.Center.Y, 220f, Color.White * (0.45f * e));
            }
            if (conductorLit > 0f) Gfx.DrawGlow(sb, conductorRect.Center.X, conductorRect.Center.Y, 220f, Color.White * (0.45f * UiMotion.EaseOut(conductorLit)));
        }

        //======================================================================================
        //The Words (crisp on top)
        //======================================================================================

        public override void Draw(SpriteBatch sb)
        {
            RunState run = Game.CurrentRun;

            DrawSheet(sb);

            //Groups : what each part of the row is, IN FRONT over the first of the band
            float head = UiMotion.Between(time, 0.15f, BattleRules.UiEnterTime);
            Gfx.TextSpaced(sb, Game.Font, "CONDUCTOR", conductorRect.X, ColumnsTop - 24, Palette.LineGrey * head, TextSize.Tiny, 3f);
            if (slotRects.Length > 0) Gfx.TextSpaced(sb, Game.Font, "IN FRONT", slotRects[0].X, ColumnsTop - 24, Palette.Highlight * head, TextSize.Tiny, 3f);

            //Words Under The Columns : the band's show the weapon at its tier, and the tier's diamonds over the column
            Conductor c = run.Conductor;
            Rectangle cr = conductorRect;
            float ca = ColumnIn(0);
            cr.Y += (int)Math.Round((1f - ca) * 30f);
            BandColumn.Words(sb, cr, c.NameShort, "CONDUCTOR", c.MechanicName, 0, c.SignatureName, 0, c.Spot, UiMotion.EaseOut(conductorLit), ca);
            for (int p = 0; p < slotRects.Length; p++)
            {
                Rectangle r = dragging && p == pressed ? Shown(p) : slotRects[p];
                if (!(dragging && p == pressed)) r.Y += (int)Math.Round((1f - ColumnIn(p + 1)) * 30f);
                Musician m = owner[p];
                float a = ColumnIn(p + 1);
                BandColumn.Words(sb, r, m.NameTag, m.WeaponNow, SkillList.First(m).Name, SkillList.First(m).Cost, SkillList.Second(m).Name, SkillList.Second(m).Cost, BandColumn.SpotOf(m), UiMotion.EaseOut(lit[p]), a);
                BandColumn.TierPips(sb, r.Right - 34, r.Y - 12, m, BandColumn.SpotOf(m), a);
            }

            DrawDetail(sb, run);

            //Footer
            Gfx.TextSpaced(sb, Game.Font, "DRAG = CHANGE THE ORDER   /   THE FIRST ONE STANDS IN FRONT", 40, 664, Palette.LineGrey, TextSize.Tiny, 2f);
            Ui.Command(sb, backButton, "BACK TO ROUTE", "TAB", backMotion, 1f, false);

            RunHud.DrawTop(sb, run, "THE BAND");
            Gfx.TextSpaced(sb, Game.Font, "WHO PLAYS IN WHAT ORDER, AND HOW SHARP THEIR WEAPONS ARE", 40, RunHud.TopH + 14, Palette.LineGrey, TextSize.Tiny, 3f);
            RunHud.DrawTips(sb, run);
        }

        //Sheet : the white slanted sheet that closes the row on the right (menu8), the page's name down it
        private void DrawSheet(SpriteBatch sb)
        {
            float a = UiMotion.Between(time, 0f, 0.5f);
            int x = SideRight + 22 + (int)Math.Round((1f - a) * 80f);
            Gfx.SlantBox(sb, new Rectangle(x, 116, 200, 604), 40, Palette.Paper * (0.92f * a));
            Gfx.SlantBox(sb, new Rectangle(x - 10, 116, 12, 604), 40, Palette.LineGrey * (0.5f * a));
            Gfx.TextVertical(sb, Game.BigFont, "THE BAND", x + 92, 150, Palette.Ink * a, TextSize.Title);
        }

        //Detail : whoever the mouse is over, explained (Core/BandColumn.cs). The conductor's PASSIVE and SIGNATURE.
        private void DrawDetail(SpriteBatch sb, RunState run)
        {
            float a = UiMotion.Between(time, 0.4f, BattleRules.UiEnterTime);
            Gfx.Rect(sb, detailBox, Palette.Panel * (0.9f * a));
            Gfx.Rect(sb, detailBox.X, detailBox.Y, detailBox.Width, 1, Palette.LineGrey * (0.6f * a));

            int x = detailBox.X + 18;
            int y = detailBox.Y + 12;
            Musician m = hover >= 0 && hover < owner.Length ? owner[hover] : null;
            if (dragging && pressed >= 0) m = owner[pressed];

            if (hover == -1)
            {
                Conductor c = run.Conductor;
                Gfx.Text(sb, Game.BigFont, c.Name, x, y - 2, c.Spot * a, TextSize.Subtitle);
                Gfx.TextSpaced(sb, Game.Font, c.Role, x, y + 30, Palette.PaperDim * a, TextSize.Tiny, 2f);
                Gfx.Text(sb, Game.StoryFont, conductorText, x + 560, y, Palette.Paper * a, TextSize.StorySmall);
                return;
            }
            if (m == null)
            {
                Gfx.TextSpaced(sb, Game.Font, "THE ENSEMBLE", x, y, Palette.LineGrey * a, TextSize.Tiny, 3f);
                Gfx.Text(sb, Game.StoryFont, "Everyone plays every fight, the first one in front.", x, y + 22, Palette.Paper * a, TextSize.StorySmall);
                Gfx.Text(sb, Game.StoryFont, "Point at a column to read it. Drag one to change the order.", x, y + 46, Palette.PaperDim * a, TextSize.StorySmall);
                return;
            }
            BandColumn.Detail(sb, detailBox, m, run, skillText[m.Index], a);
        }
    }
}
