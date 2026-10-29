using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //TeamPickScreen : THE AUDITION, where a run picks its band (THE BAND OF THREE, Beta1 29 Oct, docs/BETA1_DIRECTION.md 8.8).
    //The five legends stand in a row of tall slanted columns (Core/BandColumn.cs), all of them open from the first run.
    //   POINT   the column steps up and the musician's own colour rises from its foot, the box below explains them
    //   CLICK   picks them or puts them back. A picked column stays up in its own light with its number on it : the first one
    //           picked stands in front in a fight (the band page can change the order later)
    //   BEGIN   with three picked, the band is set (RunState.PickBand) and named next (BandNameScreen)
    //Keys : LEFT and RIGHT point, SPACE picks, ENTER begins. ESC opens the pause menu, like every page of a run.
    //Until 29 Oct new faces were rolled at every floor and the BENCH held the rest (RecruitScreen, CALL).
    public class TeamPickScreen : GameScreen
    {
        //Audition Layout : five columns centred, the box under them, the button at the bottom
        private const int ColumnW = 176;
        private const int ColumnGap = 34;
        private const int ColumnsTop = 132;
        private static readonly Rectangle detailBox = new Rectangle(40, 512, 1200, 112);
        private Rectangle beginButton = new Rectangle(930, 642, 310, 50);

        //Audition State
        private Musician[] legends = new Musician[0];        // MusicianList.Shown, in its order
        private Rectangle[] columns = new Rectangle[0];
        private float[] lit = new float[0];                  // the pointed column's light, 0..1
        private float[] chosen = new float[0];               // a picked column's light, 0..1
        private float[] pickedAt = new float[0];             // when each was last picked, for its flash
        private List<Musician> picked = new List<Musician>();
        private int pointed = -1;
        private Vector2 lastMouse;
        private float time;
        private float leaving = -1f;                         // seconds since BEGIN, -1 before
        private float messageTimer;
        private string message = "";
        private string[] skillText = new string[0];
        private string[] numbers = { "1", "2", "3", "4", "5" };
        private string countLabel = "";
        private UiMotion beginMotion = new UiMotion();

        //Wipe Word : the page's name in the wipe on the way here (WIPE WORD)
        public override string WipeWord
        {
            get { return "THE AUDITION"; }
        }

        public override void Load()
        {
            RunState run = Game.CurrentRun;
            legends = MusicianList.Shown;
            columns = new Rectangle[legends.Length];
            lit = new float[legends.Length];
            chosen = new float[legends.Length];
            pickedAt = new float[legends.Length];
            int total = legends.Length * ColumnW + (legends.Length - 1) * ColumnGap;
            for (int i = 0; i < legends.Length; i++)
                columns[i] = new Rectangle((TacetGame.ScreenW - total) / 2 + i * (ColumnW + ColumnGap), ColumnsTop, ColumnW, BandColumn.PictureH);

            //A run that already has some of its band (an old save) starts with them picked
            for (int i = 0; i < run.Roster.Count && picked.Count < BattleRules.BandSize; i++)
                if (Array.IndexOf(legends, run.Roster[i]) >= 0) picked.Add(run.Roster[i]);
            RefreshCount();

            skillText = BandColumn.SkillText(Game.StoryFont);
            SoundBank.PlayMusic(Music.Route);

            //The Lens : a dark stage, every column in its own soft light
            Lens.Ambient = 0.4f;
            Lens.SpotPower = 0f;
            Lens.Vignette = 0.55f;
        }

        private void RefreshCount()
        {
            countLabel = picked.Count + " / " + BattleRules.BandSize;
        }

        private bool Ready
        {
            get { return picked.Count >= BattleRules.BandSize; }
        }

        public override void Update(float dt)
        {
            time += dt;
            if (messageTimer > 0f) messageTimer -= dt;

            //Leaving : the band steps forward a moment, then the name page
            if (leaving >= 0f)
            {
                leaving += dt;
                UpdateLights(dt);
                if (leaving >= 0.6f && leaving - dt < 0.6f) Game.Screens.Change(new BandNameScreen());
                return;
            }

            //Point : the mouse when it moves, or the arrow keys
            int before = pointed;
            bool moved = Input.MousePos != lastMouse;
            lastMouse = Input.MousePos;
            for (int i = 0; i < columns.Length; i++)
                if (moved && Input.MouseOver(Tall(columns[i]))) pointed = i;
            if (Input.KeyPressed(Keys.Right) || Input.KeyPressed(Keys.D)) pointed = Math.Min(columns.Length - 1, pointed + 1);
            if (Input.KeyPressed(Keys.Left) || Input.KeyPressed(Keys.A)) pointed = pointed < 0 ? 0 : Math.Max(0, pointed - 1);
            if (pointed != before && pointed >= 0) SoundBank.Play(Sfx.UiMove);

            //Pick : a click on a column, or SPACE on the pointed one
            for (int i = 0; i < columns.Length; i++)
                if (Input.ClickedOn(Tall(columns[i]))) Toggle(i);
            if (Input.KeyPressed(Keys.Space) && pointed >= 0) Toggle(pointed);

            UpdateLights(dt);
            beginMotion.Update(dt, Ready && Input.MouseOver(beginButton));

            //Begin : with the whole band picked
            if (Input.KeyPressed(Keys.Enter) || Input.ClickedOn(beginButton))
            {
                if (!Ready)
                {
                    Say("PICK " + BattleRules.BandSize + " MUSICIANS");
                    SoundBank.Play(Sfx.UiDenied);
                    return;
                }
                beginMotion.Press();
                Game.CurrentRun.PickBand(picked);
                SoundBank.Play(Sfx.UiConfirm);
                leaving = 0f;
            }
        }

        //Toggle : pick the legend, or put them back. A fourth is refused, one has to be put back first.
        private void Toggle(int i)
        {
            Musician m = legends[i];
            if (picked.Contains(m))
            {
                picked.Remove(m);
                SoundBank.Play(Sfx.SeatRemove);
            }
            else if (picked.Count >= BattleRules.BandSize)
            {
                Say("THE BAND IS FULL : CLICK ONE TO PUT THEM BACK");
                SoundBank.Play(Sfx.UiDenied);
                return;
            }
            else
            {
                picked.Add(m);
                pickedAt[i] = time;
                SoundBank.Play(Sfx.Recruit);
            }
            RefreshCount();
        }

        //Lights : the pointed column and the picked ones come up, the others go down (BattleRules.UiGalleryPoint)
        private void UpdateLights(float dt)
        {
            float step = dt / BattleRules.UiGalleryPoint;
            for (int i = 0; i < legends.Length; i++)
            {
                bool on = i == pointed && leaving < 0f;
                lit[i] = on ? Math.Min(1f, lit[i] + step) : Math.Max(0f, lit[i] - step);
                bool pick = picked.Contains(legends[i]);
                chosen[i] = pick ? Math.Min(1f, chosen[i] + step) : Math.Max(0f, chosen[i] - step);
            }
        }

        private void Say(string text)
        {
            message = text;
            messageTimer = 2.2f;
        }

        //Tall : a column with the words under it, the whole thing the mouse can take
        private static Rectangle Tall(Rectangle r)
        {
            return new Rectangle(r.X, r.Y, r.Width, r.Height + BandColumn.WordsH);
        }

        //Column In : ENTER, the columns come in one after another from the left
        private float ColumnIn(int i)
        {
            return UiMotion.Between(time, 0.1f + i * 0.06f, 0.4f);
        }

        //Shown : column i this frame, risen in, stepped up when pointed or picked, forward when the band leaves
        private Rectangle Shown(int i)
        {
            Rectangle r = columns[i];
            float up = Math.Max(UiMotion.EaseOut(lit[i]), UiMotion.EaseOut(chosen[i]));
            r.Y += (int)Math.Round((1f - ColumnIn(i)) * 30f - BandColumn.Lift * up);
            if (leaving >= 0f && picked.Contains(legends[i])) r.Y -= (int)Math.Round(14f * UiMotion.EaseOut(Math.Min(1f, leaving / 0.35f)));
            return r;
        }

        //Bright : how much of a column shows. With the band full the ones left out dim, and fade as the band leaves.
        private float Bright(int i)
        {
            bool pick = picked.Contains(legends[i]);
            float a = ColumnIn(i);
            if (!pick && Ready) a *= 0.55f + 0.45f * lit[i];
            if (!pick && leaving >= 0f) a *= 1f - Math.Min(1f, leaving / 0.4f);
            return a;
        }

        //======================================================================================
        //The World (THE LENS) : the columns' pictures on a dark stage
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
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageDeep);
            Gfx.DrawGlowBox(sb, new Rectangle(-100, 60, 1480, 520), Palette.Paper * 0.04f);
            for (int i = 0; i < legends.Length; i++)
            {
                Musician m = legends[i];
                Rectangle r = Shown(i);
                float glow = UiMotion.EaseOut(chosen[i]);
                if (glow > 0f) Gfx.DrawGlowBox(sb, new Rectangle(r.X - 60, r.Bottom - 40, r.Width + 120, 90), BandColumn.SpotOf(m) * (0.18f * glow));
                BandColumn.Picture(sb, r, UiArt.BustOf(m), BandColumn.SpotOf(m), Math.Max(UiMotion.EaseOut(lit[i]), 0.6f * glow), Bright(i));
            }
        }

        //Lights : a beam from above on every picked column, a softer light on the pointed one
        public override void DrawLights(SpriteBatch sb)
        {
            for (int i = 0; i < legends.Length; i++)
            {
                Rectangle r = Shown(i);
                float beam = UiMotion.EaseOut(chosen[i]);
                if (beam > 0f)
                    for (float y = 0f; y < r.Bottom + 40; y += 4f)
                    {
                        float half = 50f + (y / (r.Bottom + 40)) * 80f;
                        Gfx.Rect(sb, r.Center.X - half, y, half * 2f, 4f, Color.White * (0.3f * beam));
                    }
                float e = UiMotion.EaseOut(lit[i]);
                if (e > 0f) Gfx.DrawGlow(sb, r.Center.X, r.Center.Y, 220f, Color.White * (0.4f * e));
            }
        }

        //Glow : their colour at the foot of a column the moment it is picked (none with LESS FLASHING)
        public override void DrawGlow(SpriteBatch sb)
        {
            if (Settings.ReduceFlashing) return;
            for (int i = 0; i < legends.Length; i++)
            {
                if (!picked.Contains(legends[i])) continue;
                float t = time - pickedAt[i];
                if (t < 0f || t > 0.7f) continue;
                Rectangle r = Shown(i);
                Gfx.DrawGlow(sb, r.Center.X, r.Bottom - 20, 160f, BandColumn.SpotOf(legends[i]) * (0.6f * (1f - t / 0.7f)));
            }
        }

        //======================================================================================
        //The Words (crisp on top)
        //======================================================================================

        public override void Draw(SpriteBatch sb)
        {
            RunState run = Game.CurrentRun;
            float cx = TacetGame.ScreenW / 2f;

            //Heading : who leads the audition, and what to do
            float head = UiMotion.Between(time, 0.05f, BattleRules.UiEnterTime);
            float rise = (1f - head) * BattleRules.UiEnterSlide;
            Gfx.TextSpacedCentered(sb, Game.Font, run.Conductor.Name, cx, 22 - rise, Palette.LineGrey * head, TextSize.Tiny, 5f);
            Gfx.TextCentered(sb, Game.BigFont, "Choose your band of three", cx, 64 - rise, Palette.Paper * head, TextSize.Title);

            //Columns : the words under each, and the number of a picked one
            for (int i = 0; i < legends.Length; i++)
            {
                Musician m = legends[i];
                Rectangle r = Shown(i);
                float a = Bright(i);
                BandColumn.Words(sb, r, m.NameTag, m.Instrument, SkillList.First(m).Name, SkillList.First(m).Cost, SkillList.Second(m).Name, SkillList.Second(m).Cost,
                                 BandColumn.SpotOf(m), Math.Max(UiMotion.EaseOut(lit[i]), UiMotion.EaseOut(chosen[i])), a);
                int place = picked.IndexOf(m);
                if (place >= 0)
                {
                    Ui.Tag(sb, numbers[place], r.X + BandColumn.Slant + 6, r.Y + 8, true, a);
                    if (place == 0) Gfx.TextSpacedRight(sb, Game.Font, "IN FRONT", r.Right - 4, r.Y - 18, Palette.Highlight * a, TextSize.Tiny, 3f);
                }
            }

            //Detail : the pointed legend explained, or what the page is for
            float d = UiMotion.Between(time, 0.4f, BattleRules.UiEnterTime);
            Gfx.Rect(sb, detailBox, Palette.Panel * (0.9f * d));
            Gfx.Rect(sb, detailBox.X, detailBox.Y, detailBox.Width, 1, Palette.LineGrey * (0.6f * d));
            if (pointed >= 0) BandColumn.Detail(sb, detailBox, legends[pointed], run, skillText[legends[pointed].Index], d);
            else
            {
                Gfx.TextSpaced(sb, Game.Font, "THE AUDITION", detailBox.X + 18, detailBox.Y + 12, Palette.LineGrey * d, TextSize.Tiny, 3f);
                Gfx.Text(sb, Game.StoryFont, "Three of them play with you for the whole run. Every one of them is in every fight.", detailBox.X + 18, detailBox.Y + 34, Palette.Paper * d, TextSize.StorySmall);
                Gfx.Text(sb, Game.StoryFont, "Point at a column to read it. The first one you pick stands in front.", detailBox.X + 18, detailBox.Y + 58, Palette.PaperDim * d, TextSize.StorySmall);
            }

            //Footer : how many, how to, and the button
            float foot = UiMotion.Between(time, 0.5f, BattleRules.UiEnterTime);
            Gfx.TextSpaced(sb, Game.Font, "THE BAND", 40, 646, Palette.LineGrey * foot, TextSize.Tiny, 3f);
            Gfx.Text(sb, Game.BigFont, countLabel, 40, 660, (Ready ? Palette.Highlight : Palette.Paper) * foot, TextSize.Small);
            Gfx.TextSpaced(sb, Game.Font, "CLICK = PICK OR PUT BACK", 180, 674, Palette.LineGrey * foot, TextSize.Tiny, 2f);
            Ui.Command(sb, beginButton, "BEGIN", "ENTER", beginMotion, Ready ? foot : foot * 0.4f, false);
            if (messageTimer > 0f)
                Gfx.TextSpacedRight(sb, Game.Font, message, beginButton.X - 30, beginButton.Y + 18, Palette.Accent * Math.Min(1f, messageTimer), TextSize.Label, 2f);
        }
    }
}
