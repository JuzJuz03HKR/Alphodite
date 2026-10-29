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
    //The conductor's column is first, cut from their painting. Then the seats ON STAGE, then the BENCH, a column for every
    //place the ensemble has, a plus where nobody stands yet. A white slanted sheet closes the row on the right (menu8).
    //   POINT   the column steps up and the musician's own colour rises from its foot, the box below explains them
    //   CLICK   on stage to the bench, from the bench onto the stage while there is room. With the stage full, a bench
    //           player is picked and the next click on a player swaps the two (as before)
    //   DRAG    a column to another place : the two trade places. The ones on stage start a fight in this order, the
    //           first one in front (RunState.Order, Lineup). Dragging across the line between the stage and the bench
    //           moves them on or off the stage.
    //TAB, ENTER or the button goes back to the route. ESC opens the pause menu, like every page of a run.
    public class BandScreen : GameScreen
    {
        //Band Layout
        private const int ColumnsTop = 132;
        private const int SideLeft = 40;
        private const int SideRight = 1146;            // the white sheet starts after this
        private const int Gap = 14;
        private const int GroupGap = 34;
        private static readonly Rectangle detailBox = new Rectangle(40, 512, 1106, 112);
        private Rectangle backButton = new Rectangle(900, 646, 246, 46);

        //Band State
        private int columnW = 140;
        private Rectangle conductorRect;
        private Rectangle[] slotRects = new Rectangle[0];   // every place of the ensemble : the stage seats, then the bench
        private Musician[] owner = new Musician[0];         // who stands in each place, null for nobody
        private float[] lit = new float[0];
        private float conductorLit;
        private int hover = -2;                             // the place under the mouse : -1 the conductor, -2 nothing
        private int pressed = -1;                           // the place the mouse went down on
        private Vector2 pressedAt;
        private bool dragging;
        private Musician picked;                            // a bench player waiting to swap in, or null
        private float time;
        private float messageTimer;
        private string message = "";
        private UiMotion backMotion = new UiMotion();

        //Prepared Text : made when the band changes, never in Draw
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
            run.Formation.Tidy(run.Seats);

            skillText = new string[MusicianList.All.Length];
            for (int i = 0; i < MusicianList.All.Length; i++)
            {
                Musician m = MusicianList.All[i];
                Skill first = SkillList.First(m);
                Skill second = SkillList.Second(m);
                skillText[i] = Gfx.WrapText(Game.StoryFont, first.Name + " : " + first.Text, 470, TextSize.StorySmall, 2) + "\n"
                             + Gfx.WrapText(Game.StoryFont, second.Name + " : " + second.Text, 470, TextSize.StorySmall, 2);
            }
            Conductor c = run.Conductor;
            conductorText = Gfx.WrapText(Game.StoryFont, c.MechanicName + " : " + c.MechanicText, 470, TextSize.StorySmall, 2) + "\n"
                          + Gfx.WrapText(Game.StoryFont, "SPACE  " + c.SignatureName + " : " + c.SignatureText, 470, TextSize.StorySmall, 2);

            Lens.Ambient = 0.46f;
            Lens.SpotPower = 0f;
            Lens.Vignette = 0.5f;
            Rebuild(run);
        }

        //Rebuild : the places and who stands in them, after every change. The stage seats first, then the bench, each in
        //the player's order.
        private void Rebuild(RunState run)
        {
            run.RefreshLabels();
            run.SyncOrder();
            int places = run.RosterCapacity;
            int count = 1 + places;
            columnW = Math.Min(160, (SideRight - SideLeft - GroupGap * 2 - Gap * Math.Max(0, count - 3)) / count);
            int windowH = BandColumn.PictureH;
            conductorRect = new Rectangle(SideLeft, ColumnsTop, columnW, windowH);

            if (slotRects.Length != places)
            {
                slotRects = new Rectangle[places];
                owner = new Musician[places];
                lit = new float[places];
            }
            int stageX = SideLeft + columnW + GroupGap;
            int benchX = stageX + run.Seats * (columnW + Gap) - Gap + GroupGap;
            for (int p = 0; p < places; p++)
            {
                int x = p < run.Seats ? stageX + p * (columnW + Gap) : benchX + (p - run.Seats) * (columnW + Gap);
                slotRects[p] = new Rectangle(x, ColumnsTop, columnW, windowH);
                owner[p] = null;
            }

            int stage = 0;
            int bench = run.Seats;
            for (int i = 0; i < run.Order.Count; i++)
            {
                Musician m = run.Order[i];
                if (run.Formation.OnStage(m)) { if (stage < run.Seats) owner[stage++] = m; }
                else if (bench < places) owner[bench++] = m;
            }
        }

        public override void Update(float dt)
        {
            time += dt;
            if (messageTimer > 0f) messageTimer -= dt;
            RunState run = Game.CurrentRun;

            //Hover : the place under the mouse, its column and the words under it
            hover = -2;
            if (Input.MouseOver(Tall(conductorRect))) hover = -1;
            for (int p = 0; p < slotRects.Length; p++)
                if (Input.MouseOver(Tall(slotRects[p]))) hover = p;

            float step = dt / BattleRules.UiGalleryPoint;
            for (int p = 0; p < lit.Length; p++)
            {
                bool on = owner[p] != null && (dragging ? p == pressed : p == hover);
                lit[p] = on ? Math.Min(1f, lit[p] + step) : Math.Max(0f, lit[p] - step);
            }
            conductorLit = hover == -1 ? Math.Min(1f, conductorLit + step) : Math.Max(0f, conductorLit - step);
            backMotion.Update(dt, Input.MouseOver(backButton));

            //Press, Drag, Release : a press that moves is a drag, one that does not is a click
            if (Input.MouseClicked() && hover >= 0 && owner[hover] != null)
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
                else if (owner[pressed] != null) Choose(run, owner[pressed]);
                pressed = -1;
                dragging = false;
            }

            //Cancel : a right click puts a picked player back down
            if (Input.MouseRightClicked() && picked != null)
            {
                picked = null;
                SoundBank.Play(Sfx.UiBack);
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

        //Drop : a dragged column lands on another place. The two trade places in the order, and when the line between the
        //stage and the bench is crossed, the one coming up goes on stage and the other one sits out.
        private void Drop(RunState run, int from, int to)
        {
            Musician m = owner[from];
            Musician there = owner[to];
            if (m == null || to == from) return;
            bool fromStage = run.Formation.OnStage(m);
            bool toStage = to < run.Seats;

            if (fromStage && !toStage)
            {
                if (there != null) run.Formation.Swap(there, m);
                else if (run.Formation.SeatedCount <= 1)
                {
                    Say("SOMEBODY HAS TO PLAY");
                    SoundBank.Play(Sfx.UiDenied);
                    return;
                }
                else run.Formation.Bench(m);
            }
            else if (!fromStage && toStage)
            {
                if (there != null) run.Formation.Swap(m, there);
                else run.Formation.AutoSeat(m, run.Seats);
            }

            //Order : the two trade places, or the one moved goes to the end
            int a = run.Order.IndexOf(m);
            if (there != null)
            {
                int b = run.Order.IndexOf(there);
                run.Order[a] = there;
                run.Order[b] = m;
            }
            else
            {
                run.Order.RemoveAt(a);
                run.Order.Add(m);
            }
            picked = null;
            SoundBank.Play(Sfx.SeatDrop);
            Rebuild(run);
        }

        //Choose : a click on a musician. On stage they go to the bench, from the bench they step on while there is room.
        //With the stage full, a bench player is picked and the next click on someone playing swaps the two.
        private void Choose(RunState run, Musician m)
        {
            Formation f = run.Formation;
            bool playing = f.OnStage(m);

            if (picked != null && playing)
            {
                f.Swap(picked, m);
                int a = run.Order.IndexOf(picked);
                int b = run.Order.IndexOf(m);
                if (a >= 0 && b >= 0) { run.Order[a] = m; run.Order[b] = picked; }
                picked = null;
                SoundBank.Play(Sfx.SeatDrop);
                Rebuild(run);
                return;
            }

            if (playing)
            {
                if (f.SeatedCount <= 1)
                {
                    Say("SOMEBODY HAS TO PLAY");
                    SoundBank.Play(Sfx.UiDenied);
                    return;
                }
                f.Bench(m);
                SoundBank.Play(Sfx.SeatRemove);
                Rebuild(run);
                return;
            }

            //From The Bench
            if (picked == m)
            {
                picked = null;
                SoundBank.Play(Sfx.UiBack);
                return;
            }
            if (f.AutoSeat(m, run.Seats))
            {
                picked = null;
                SoundBank.Play(Sfx.SeatDrop);
                Rebuild(run);
                return;
            }
            picked = m;
            Say("THE STAGE IS FULL : CLICK WHO SITS OUT");
            SoundBank.Play(Sfx.SeatPickUp);
        }

        private void Say(string text)
        {
            message = text;
            messageTimer = 2.2f;
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

            //The Line : between the stage and the bench, a thin rule with a circle on it
            if (slotRects.Length > run.Seats && run.Seats > 0)
            {
                float lineX = (slotRects[run.Seats - 1].Right + slotRects[run.Seats].X) / 2f;
                Gfx.Rect(sb, lineX, ColumnsTop - 6, 1, BandColumn.PictureH + 12, Palette.LineGrey * 0.4f);
                Gfx.CircleOutline(sb, lineX, ColumnsTop + BandColumn.PictureH / 2f, 4f, Palette.LineGrey * 0.7f, 1f);
            }

            //Conductor : the leader's column, cut from their painting
            float ca = ColumnIn(0);
            Rectangle cr = conductorRect;
            cr.Y += (int)Math.Round((1f - ca) * 30f - BandColumn.Lift * UiMotion.EaseOut(conductorLit));
            BandColumn.Picture(sb, cr, ArtBank.GalleryOf(run.Conductor), run.Conductor.Spot, UiMotion.EaseOut(conductorLit), ca);

            //Places : a plus where nobody stands, the dragged one last so it goes over the others
            for (int p = 0; p < slotRects.Length; p++)
            {
                if (dragging && p == pressed) { BandColumn.Empty(sb, slotRects[p], ColumnIn(p + 1)); continue; }
                Rectangle r = Shown(p);
                if (owner[p] == null) BandColumn.Empty(sb, r, ColumnIn(p + 1));
                else BandColumn.Picture(sb, r, UiArt.BustOf(owner[p]), BandColumn.SpotOf(owner[p]), UiMotion.EaseOut(lit[p]), ColumnIn(p + 1) * (p < run.Seats ? 1f : 0.6f));
            }
            if (dragging && pressed >= 0 && owner[pressed] != null)
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

            //Groups : what each part of the row is
            float head = UiMotion.Between(time, 0.15f, BattleRules.UiEnterTime);
            Gfx.TextSpaced(sb, Game.Font, "CONDUCTOR", conductorRect.X, ColumnsTop - 24, Palette.LineGrey * head, TextSize.Tiny, 3f);
            if (run.Seats > 0) Gfx.TextSpaced(sb, Game.Font, run.SeatsLabel, slotRects[0].X, ColumnsTop - 24, Palette.Highlight * head, TextSize.Tiny, 3f);
            if (slotRects.Length > run.Seats) Gfx.TextSpaced(sb, Game.Font, "BENCH", slotRects[run.Seats].X, ColumnsTop - 24, Palette.LineGrey * head, TextSize.Tiny, 3f);

            //Words Under The Columns
            Conductor c = run.Conductor;
            Rectangle cr = conductorRect;
            float ca = ColumnIn(0);
            cr.Y += (int)Math.Round((1f - ca) * 30f);
            BandColumn.Words(sb, cr, c.Name, "CONDUCTOR", c.MechanicName, 0, c.SignatureName, 0, c.Spot, UiMotion.EaseOut(conductorLit), ca);
            for (int p = 0; p < slotRects.Length; p++)
            {
                if (owner[p] == null) continue;
                Rectangle r = dragging && p == pressed ? Shown(p) : slotRects[p];
                if (!(dragging && p == pressed)) r.Y += (int)Math.Round((1f - ColumnIn(p + 1)) * 30f);
                Musician m = owner[p];
                float a = ColumnIn(p + 1) * (p < run.Seats ? 1f : 0.6f);
                BandColumn.Words(sb, r, m.NameTag, m.Instrument, SkillList.First(m).Name, SkillList.First(m).Cost, SkillList.Second(m).Name, SkillList.Second(m).Cost, BandColumn.SpotOf(m), UiMotion.EaseOut(lit[p]), a);
                if (m == picked) Ornament.CornerBrackets(sb, new Rectangle(r.X - 6, r.Y - 6, r.Width + 12, r.Height + 12), 14, Palette.Highlight, 1);
                if (picked != null && run.Formation.OnStage(m)) Gfx.CircleOutline(sb, r.Center.X, r.Y - 12, 4f + 2f * (float)Math.Sin(time * 6f), Palette.Highlight, 1f);
            }

            DrawDetail(sb, run);

            //Footer
            Gfx.TextSpaced(sb, Game.Font, run.RosterLabel, 40, 664, Palette.PaperDim, TextSize.Tiny, 3f);
            Gfx.TextSpaced(sb, Game.Font, "CLICK = STAGE OR BENCH   /   DRAG = CHANGE THE ORDER", 40, 684, Palette.LineGrey, TextSize.Tiny, 2f);
            Ui.Command(sb, backButton, "BACK TO ROUTE", "TAB", backMotion, 1f, false);
            if (messageTimer > 0f)
                Gfx.TextSpacedRight(sb, Game.Font, message, backButton.X - 30, backButton.Y + 16, Palette.Accent * Math.Min(1f, messageTimer), TextSize.Label, 2f);

            RunHud.DrawTop(sb, run, "THE BAND");
            Gfx.TextSpaced(sb, Game.Font, "WHO PLAYS, IN WHAT ORDER, AND WHO WAITS ON THE BENCH", 40, RunHud.TopH + 14, Palette.LineGrey, TextSize.Tiny, 3f);
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

        //Detail : whoever the mouse is over, explained. Their trait and both skills.
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
                Gfx.Text(sb, Game.StoryFont, "Everyone on stage starts the fight, the first one in front. The bench can be CALLED in.", x, y + 22, Palette.Paper * a, TextSize.StorySmall);
                Gfx.Text(sb, Game.StoryFont, "Point at a column to read it. Drag one to change the order.", x, y + 46, Palette.PaperDim * a, TextSize.StorySmall);
                return;
            }

            //Who : the name, the instrument, life and power
            Gfx.Text(sb, Game.BigFont, m.NameTag, x, y - 2, BandColumn.SpotOf(m) * a, TextSize.Subtitle);
            Gfx.TextSpaced(sb, Game.Font, m.FamilyLabel, x, y + 30, Palette.PaperDim * a, TextSize.Tiny, 2f);
            Gfx.TextSpaced(sb, Game.Font, "LIFE", x, y + 56, Palette.LineGrey * a, TextSize.Tiny, 2f);
            Gfx.Text(sb, Game.Font, NumberText.Get(run.LifeOf(m)), x + 44, y + 52, Palette.Paper * a, TextSize.Body);
            Gfx.Text(sb, Game.Font, NumberText.Get(run.MaxLifeOf(m)), x + 84, y + 56, Palette.LineGrey * a, TextSize.Tiny);
            Gfx.TextSpaced(sb, Game.Font, "POWER", x + 130, y + 56, Palette.LineGrey * a, TextSize.Tiny, 2f);
            Gfx.Text(sb, Game.Font, NumberText.Get(run.PowerOf(m)), x + 186, y + 52, Palette.Paper * a, TextSize.Body);

            //Trait and Skills
            Gfx.Rect(sb, x + 250, y, 1, 86, Palette.LineGrey * (0.5f * a));
            Ui.Tag(sb, m.TraitName, x + 266, y, true, a);
            Gfx.Text(sb, Game.StoryFont, m.TraitWrapped, x + 266, y + 26, Palette.Paper * a, TextSize.StorySmall);
            Gfx.Text(sb, Game.StoryFont, skillText[m.Index], x + 600, y, Palette.Paper * a, TextSize.StorySmall);
        }
    }
}
