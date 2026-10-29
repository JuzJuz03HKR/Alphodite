using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //LuthierScreen : THE LUTHIER, a place on the map (THE BAND OF THREE, 29 Oct, docs/BETA1_DIRECTION.md 8.3). A maker of
    //instruments, and so of weapons : one musician's weapon goes up a tier, free (WEAPON TIERS, RunState.Rehearse).
    //   Left   the workshop's picture, or the empty slot where it goes (Content/Art/Places/luthier.png, 520 x 470)
    //   Right  the band as tall slanted columns (Core/BandColumn.cs). Under each : the weapon now, the next tier, and power
    //          now and after. Pointed at, the column steps up into the lamp's light. A click sharpens that weapon.
    //   The weapon that went up : its colour floods the column, the tier's diamonds fill one after another, the name changes.
    //One weapon per visit. With every weapon at its top tier, the luthier pays shards for the visit instead.
    //A lamp hangs over the bench, the room around it dark (the user : white, black and grey, colour only on the characters).
    public class LuthierScreen : GameScreen
    {
        //Workshop Layout
        private const int ColumnW = 176;
        private const int ColumnGap = 22;
        private const int ColumnsTop = 160;
        private static readonly Rectangle artBox = new Rectangle(36, 92, 520, 470);
        private Rectangle moveButton = new Rectangle(930, 636, 310, 54);
        private static readonly Vector2 Lamp = new Vector2(880f, 70f);

        //Workshop State
        private Rectangle[] columns = new Rectangle[0];
        private float[] lit = new float[0];
        private int pointed = -1;
        private Musician sharpened;                // the musician whose weapon went up, null until then
        private int sharpenedAt = -1;
        private float sharpenTime;                 // seconds since it went up
        private bool nothingToDo;                  // every weapon is at its top tier
        private float time;
        private string[] nowLabel = new string[0]; // "POWER 7" now and "TO 9" after, made when the page opens and after the upgrade
        private string[] nextLabel = new string[0];
        private string resultLine = "";
        private UiMotion moveMotion = new UiMotion();

        //Wipe Word : the page's name in the wipe on the way here (WIPE WORD)
        public override string WipeWord
        {
            get { return "THE LUTHIER"; }
        }

        public override void Load()
        {
            RunState run = Game.CurrentRun;
            int count = run.Roster.Count;
            columns = new Rectangle[count];
            lit = new float[count];
            int total = count * ColumnW + Math.Max(0, count - 1) * ColumnGap;
            int left = artBox.Right + (TacetGame.ScreenW - 36 - artBox.Right - total) / 2;
            for (int i = 0; i < count; i++)
                columns[i] = new Rectangle(left + i * (ColumnW + ColumnGap), ColumnsTop, ColumnW, BandColumn.PictureH);
            Labels(run);

            //Nothing To Sharpen : every weapon is at its top tier, the luthier pays for the visit
            if (!run.AnyToTrain)
            {
                nothingToDo = true;
                run.AddShards(BattleRules.SkipMotifShards);
                resultLine = "Every weapon is already at its best. The luthier pays you for a look at them : +" + BattleRules.SkipMotifShards + " SHARDS.";
            }

            SoundBank.PlayMusic(Music.Rest);

            //The Lens : a dark workshop, one lamp over the bench
            Lens.Ambient = 0.36f;
            Lens.SpotPower = 0f;
            Lens.Vignette = 0.58f;
            Lens.BloomThreshold = 0.72f;
            Lens.BloomStrength = 0.4f;
        }

        //Labels : power now and after the next tier, for every column (made here, never in Draw)
        private void Labels(RunState run)
        {
            nowLabel = new string[run.Roster.Count];
            nextLabel = new string[run.Roster.Count];
            for (int i = 0; i < run.Roster.Count; i++)
            {
                Musician m = run.Roster[i];
                nowLabel[i] = "POWER " + run.PowerOf(m);
                nextLabel[i] = run.CanRehearse(m) ? "TO " + (run.PowerOf(m) + BattleRules.WeaponTierPower) : "TOP TIER";
            }
        }

        private bool Done
        {
            get { return sharpened != null || nothingToDo; }
        }

        public override void Update(float dt)
        {
            time += dt;
            if (sharpened != null) sharpenTime += dt;
            RunState run = Game.CurrentRun;

            pointed = -1;
            if (!Done)
                for (int i = 0; i < columns.Length; i++)
                    if (Input.MouseOver(Tall(columns[i]))) pointed = i;
            float step = dt / BattleRules.UiGalleryPoint;
            for (int i = 0; i < lit.Length; i++)
            {
                bool on = i == pointed || i == sharpenedAt;
                lit[i] = on ? Math.Min(1f, lit[i] + step) : Math.Max(0f, lit[i] - step);
            }
            moveMotion.Update(dt, Done && Input.MouseOver(moveButton));

            //Sharpen : a click on a musician whose weapon can still go up
            if (!Done && pointed >= 0 && Input.MouseClicked())
            {
                Musician m = run.Roster[pointed];
                if (!run.CanRehearse(m))
                {
                    SoundBank.Play(Sfx.UiDenied);
                    return;
                }
                run.Rehearse(m);
                sharpened = m;
                sharpenedAt = pointed;
                sharpenTime = 0f;
                Labels(run);
                resultLine = m.Name + "'s " + m.WeaponNow + ". It hits harder from the next fight.";
                SoundBank.Play(Sfx.Rehearse);
                return;
            }

            //Move On : once the work is done (ENTER too)
            if (Done && time > 0.4f && (Input.KeyPressed(Keys.Enter) || Input.ClickedOn(moveButton)))
            {
                moveMotion.Press();
                SoundBank.Play(Sfx.UiConfirm);
                RunFlow.NextStage(Game);
            }
        }

        //Tall : a column with the words under it, the whole thing the mouse can take
        private static Rectangle Tall(Rectangle r)
        {
            return new Rectangle(r.X, r.Y, r.Width, r.Height + BandColumn.WordsH + 40);
        }

        //Column In : ENTER, the columns come in one after another
        private float ColumnIn(int i)
        {
            return UiMotion.Between(time, 0.2f + i * 0.08f, 0.4f);
        }

        //Shown : column i this frame, risen in and stepped up when pointed
        private Rectangle Shown(int i)
        {
            Rectangle r = columns[i];
            r.Y += (int)Math.Round((1f - ColumnIn(i)) * 30f - BandColumn.Lift * UiMotion.EaseOut(lit[i]));
            return r;
        }

        //Flood : the sharpened weapon's colour fills its column, then settles to a glow at its foot
        private float Flood(int i)
        {
            if (i != sharpenedAt) return UiMotion.EaseOut(lit[i]);
            return 0.6f + 0.4f * Math.Max(0f, 1f - sharpenTime * 1.4f);
        }

        //======================================================================================
        //The World (THE LENS) : the workshop picture and the band, under the lamp
        //======================================================================================

        public override bool HasWorld
        {
            get { return true; }
        }

        public override bool ShowsGrain
        {
            get { return false; }
        }

        //World : the workshop picture or its empty slot, and the band's columns
        //ARTWORK : Content/Art/Places/luthier.png (the luthier at the bench is part of the picture), see ArtBank.
        public override void DrawWorld(SpriteBatch sb)
        {
            RunState run = Game.CurrentRun;
            float e = UiMotion.Between(time, 0f, 0.6f);
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageDeep);
            Gfx.DrawGlowBox(sb, new Rectangle(-120, 520, 1520, 260), Palette.Paper * 0.05f);
            ArtBank.DrawOrSlot(sb, ArtBank.Luthier, artBox, Palette.Paper, e);

            for (int i = 0; i < columns.Length; i++)
            {
                Musician m = run.Roster[i];
                Rectangle r = Shown(i);
                float a = ColumnIn(i) * (Done && i != sharpenedAt ? 0.55f : 1f);
                if (i == sharpenedAt) Gfx.DrawGlowBox(sb, new Rectangle(r.X - 60, r.Bottom - 40, r.Width + 120, 90), BandColumn.SpotOf(m) * 0.2f);
                BandColumn.Picture(sb, r, UiArt.BustOf(m), BandColumn.SpotOf(m), Flood(i), a);
            }
        }

        //Lights : LAMP, a hanging lamp over the bench, its cone on the band, and the pointed or sharpened column lit
        public override void DrawLights(SpriteBatch sb)
        {
            float up = UiMotion.EaseInOut(MathHelper.Clamp(time / 0.8f, 0f, 1f));
            float sway = 1f + 0.03f * (float)Math.Sin(time * 1.1f);
            Gfx.DrawGlow(sb, Lamp.X, Lamp.Y + 120f, 520f, Color.White * (0.55f * up * sway));
            Gfx.DrawGlowBox(sb, new Rectangle((int)Lamp.X - 330, 60, 660, 520), Color.White * (0.22f * up));
            Gfx.DrawGlow(sb, artBox.Center.X, artBox.Center.Y, 420f, Color.White * (0.3f * up));
            for (int i = 0; i < columns.Length; i++)
            {
                float k = UiMotion.EaseOut(lit[i]);
                if (k <= 0f) continue;
                Rectangle r = Shown(i);
                Gfx.DrawGlow(sb, r.Center.X, r.Center.Y, 240f, Color.White * (0.4f * k));
            }
        }

        //Glow : the sharpened weapon flashes in its colour, and a ring goes out from it (none with LESS FLASHING)
        public override void DrawGlow(SpriteBatch sb)
        {
            if (sharpened == null || Settings.ReduceFlashing || sharpenTime > 0.8f) return;
            Rectangle r = Shown(sharpenedAt);
            float k = sharpenTime / 0.8f;
            Gfx.DrawGlow(sb, r.Center.X, r.Center.Y, 260f + 120f * k, BandColumn.SpotOf(sharpened) * (0.7f * (1f - k)));
        }

        //======================================================================================
        //The Words (crisp on top)
        //======================================================================================

        public override void Draw(SpriteBatch sb)
        {
            RunState run = Game.CurrentRun;
            float rightX = columns.Length > 0 ? (columns[0].X + columns[columns.Length - 1].Right) / 2f : 900f;

            //Heading : over the band
            float head = UiMotion.Between(time, 0.1f, BattleRules.UiEnterTime);
            float rise = (1f - head) * BattleRules.UiEnterSlide;
            Gfx.TextSpacedCentered(sb, Game.Font, "A LUTHIER'S WORKSHOP", rightX, 78 - rise, Palette.LineGrey * head, TextSize.Tiny, 5f);
            Gfx.TextCentered(sb, Game.BigFont, Done ? "Sharper than before" : "One weapon, made sharper", rightX, 116 - rise, Palette.Paper * head, TextSize.Subtitle);

            //Columns : the name, the weapon now, power now and after, and the tier's diamonds over the column
            for (int i = 0; i < columns.Length; i++)
            {
                Musician m = run.Roster[i];
                Rectangle r = Shown(i);
                float a = ColumnIn(i) * (Done && i != sharpenedAt ? 0.55f : 1f);
                float l = UiMotion.EaseOut(lit[i]);
                BandColumn.Words(sb, r, m.NameTag, m.WeaponNow, SkillList.First(m).Name, SkillList.First(m).Cost, SkillList.Second(m).Name, SkillList.Second(m).Cost, BandColumn.SpotOf(m), l, a);
                DrawTiers(sb, r, m, i, a);
                float py = r.Bottom + BandColumn.WordsH + 6;
                Gfx.TextSpaced(sb, Game.Font, nowLabel[i], r.X + 2, py, Palette.PaperDim * a, TextSize.Tiny, 2f);
                bool can = run.CanRehearse(m);
                Gfx.TextSpacedRight(sb, Game.Font, nextLabel[i], r.Right - 2, py, (can ? Palette.Highlight : Palette.LineGrey) * a, TextSize.Tiny, 2f);
            }

            //Footer : what to do, or what was done
            float foot = UiMotion.Between(time, 0.5f, BattleRules.UiEnterTime);
            if (Done) Gfx.Text(sb, Game.StoryFont, resultLine, artBox.Right + 24, 600, Palette.Paper * foot, TextSize.Story);
            else Gfx.TextSpaced(sb, Game.Font, "CLICK A MUSICIAN   /   FREE, ONE WEAPON PER VISIT", artBox.Right + 24, 610, Palette.LineGrey * foot, TextSize.Tiny, 2f);
            Ui.Command(sb, moveButton, "MOVE ON", "ENTER", moveMotion, Done ? foot : foot * 0.35f, false);

            RunHud.DrawTop(sb, run, "LUTHIER");
            RunHud.DrawTips(sb, run);
        }

        //Tiers : the weapon's diamonds over its column. The one just earned pops in, bigger first, in their colour.
        private void DrawTiers(SpriteBatch sb, Rectangle r, Musician m, int i, float a)
        {
            float x = r.Right - 34;
            float y = r.Y - 12;
            Color spot = BandColumn.SpotOf(m);
            for (int t = 0; t < BattleRules.RehearseMax; t++)
            {
                float px = x + t * 12f;
                bool filled = t < m.Rehearsed;
                bool fresh = i == sharpenedAt && t == m.Rehearsed - 1;
                if (fresh)
                {
                    float k = MathHelper.Clamp(sharpenTime / 0.35f, 0f, 1f);
                    float size = 4f + 6f * (1f - UiMotion.EaseOut(k));
                    Gfx.Diamond(sb, px, y, size, spot * a);
                    if (k < 1f) Gfx.CircleOutline(sb, px, y, 6f + 18f * k, spot * (a * (1f - k)), 1.5f);
                }
                else if (filled) Gfx.Diamond(sb, px, y, 4f, spot * a);
                else Gfx.DiamondOutline(sb, px, y, 4f, Palette.LineGrey * (0.8f * a), 1f);
            }
        }
    }
}
