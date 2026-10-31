using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //ArchiveScreen : THE ARCHIVE, the after-run upgrades (Alpha 7, step 7). Opened from the title page.
    //Every run leaves ECHOES (Data/Archive.cs), and they are spent here, between runs, on upgrades
    //that stay for good. One row per upgrade : what it does, the levels bought, and the price.
    //Click a row or press 1 to 6 to buy, or point with the arrows and press ENTER. ESC goes back to the title.
    //
    //Beta1 screens round 2 (29 Oct, docs/SCREENS_LEVEL_UP.md 19) : a dark, still room (menu7). The page's name stands
    //turned on a tall bar at the left (menu1). The upgrades hang on one thin line like the branches of a tree, a circle for
    //each, and the pointed one gets the ink bar under its name. The ECHOES to spend are the big number at the bottom, with
    //its thin line (menu7). TACET's hole hangs far above, still.
    public class ArchiveScreen : GameScreen
    {
        private const int RowX = 300;
        private const int RowW = 880;
        private const int RowY = 96;
        private const int RowH = 70;
        private const int RowStep = 76;
        private const int SpineX = 270;                // the thin line the rows hang on

        private Rectangle backButton = new Rectangle(40, 646, 190, 46);
        private static readonly string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8" };
        private static readonly string[] numbers = { "01", "02", "03", "04", "05", "06", "07", "08" };
        private static readonly Vector2 Hole = new Vector2(760f, 42f);

        //Prepared Text : rebuilt after every purchase, never in Draw
        private string[] priceLabel = new string[Archive.All.Length];
        private string message = "";
        private float messageLife;
        private float time;
        private int selected;
        private int shownEchoes;                       // the echoes number counts down to what is left after a purchase
        private float echoClock = 99f;
        private int echoFrom;
        private Vector2 lastMouse;
        private UiMotion[] rowMotion = new UiMotion[Archive.All.Length];
        private UiMotion backMotion = new UiMotion();

        public override bool CanPause
        {
            get { return false; }
        }

        //Wipe Word : the page's name in the wipe on the way here (WIPE WORD)
        public override string WipeWord
        {
            get { return "THE ARCHIVE"; }
        }

        public override void Load()
        {
            for (int i = 0; i < rowMotion.Length; i++) rowMotion[i] = new UiMotion();
            shownEchoes = Archive.Echoes;
            echoFrom = Archive.Echoes;
            lastMouse = Input.MousePos;
            Refresh();

            Dark = new TacetVoid();
            Lens.Ambient = 0.42f;
            Lens.SpotPower = 0f;
            Lens.Vignette = 0.6f;
        }

        private void Refresh()
        {
            for (int i = 0; i < Archive.All.Length; i++)
            {
                ArchiveUpgrade u = Archive.All[i];
                priceLabel[i] = u.Maxed ? "TOP LEVEL" : NumberText.Get(u.NextPrice) + " " + Words.Applause;
            }
        }

        private Rectangle RowRect(int i)
        {
            return new Rectangle(RowX, RowY + i * RowStep, RowW, RowH);
        }

        public override void Update(float dt)
        {
            time += dt;
            echoClock += dt;
            if (messageLife > 0f) messageLife -= dt;

            //TACET : far above, still, breathing very slowly
            Dark.Update(dt, 0.3f);
            Dark.PlaceOnPage(Hole, 30f * UiMotion.EaseBack(MathHelper.Clamp(time / 0.6f, 0f, 1f)) * (1f + 0.03f * (float)Math.Sin(time * 0.6f)), 0f, 0f, 0f);

            //Echoes : the number counts down to what is left after a purchase
            float t = MathHelper.Clamp(echoClock / BattleRules.UiCountTime, 0f, 1f);
            shownEchoes = (int)Math.Round(MathHelper.Lerp(echoFrom, Archive.Echoes, UiMotion.EaseOut(t)));

            //Point : the arrows, or the mouse when it moves
            int before = selected;
            if (Input.KeyPressed(Keys.Down) || Input.KeyPressed(Keys.S)) selected = (selected + 1) % Archive.All.Length;
            if (Input.KeyPressed(Keys.Up) || Input.KeyPressed(Keys.W)) selected = (selected - 1 + Archive.All.Length) % Archive.All.Length;
            if (Input.MousePos != lastMouse)
            {
                lastMouse = Input.MousePos;
                for (int i = 0; i < Archive.All.Length; i++)
                    if (Input.MouseOver(RowRect(i))) selected = i;
            }
            if (selected != before) SoundBank.Play(Sfx.UiMove);
            for (int i = 0; i < rowMotion.Length; i++) rowMotion[i].Update(dt, i == selected);
            backMotion.Update(dt, Input.MouseOver(backButton));

            //Buy
            for (int i = 0; i < Archive.All.Length; i++)
                if (Input.ClickedOn(RowRect(i)) || Input.KeyPressed(Keys.D1 + i)) { selected = i; Try(i); }
            if (Input.KeyPressed(Keys.Enter)) Try(selected);

            if (Input.ClickedOn(backButton) || Input.KeyPressed(Keys.Escape))
            {
                backMotion.Press();
                SoundBank.Play(Sfx.UiConfirm);
                Game.Screens.Change(new TitleScreen());
            }
        }

        //Buy : pay and keep it for good, saved at once
        private void Try(int i)
        {
            ArchiveUpgrade u = Archive.All[i];
            int had = Archive.Echoes;
            if (Archive.Buy(i))
            {
                SaveFile.SaveArchive();
                SoundBank.Play(Sfx.UiConfirm);
                rowMotion[i].Press();
                message = u.Name + "  " + NumberText.Get(u.Level) + " / " + NumberText.Get(u.Prices.Length);
                echoFrom = had;
                echoClock = 0f;
            }
            else
            {
                SoundBank.Play(Sfx.UiDenied);
                message = u.Maxed ? "ALREADY AT THE TOP LEVEL" : "NOT ENOUGH " + Words.Applause;
            }
            messageLife = 2f;
            Refresh();
        }

        //======================================================================================
        //The World (THE LENS) : a still, dark room, rings on its floor, TACET far above
        //======================================================================================

        public override bool HasWorld
        {
            get { return true; }
        }

        public override bool ShowsGrain
        {
            get { return false; }
        }

        //Back : what the hole bends, the dark room and the rings lying on its floor (menu7)
        public override void DrawBack(SpriteBatch sb)
        {
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageDeep);
            Gfx.DrawGlowBox(sb, new Rectangle(200, 60, 1100, 640), Palette.Paper * 0.05f);
            float a = UiMotion.Between(time, 0.1f, 0.8f);
            for (int k = 0; k < 4; k++)
            {
                float rx = 240f + k * 150f;
                Ornament.FloorRing(sb, 700f, 640f, rx, rx * 0.16f, Palette.Paper * ((0.1f - k * 0.02f) * a), a);
            }
        }

        //World : the bits the hole pulls in, the spine the rows hang on, a circle for each row
        public override void DrawWorld(SpriteBatch sb)
        {
            Dark.DrawDebris(sb, false);
            float spine = UiMotion.Between(time, 0.15f, 0.6f);
            Gfx.Rect(sb, SpineX, RowY - 10, 1, (Archive.All.Length * RowStep) * spine, Palette.LineGrey * 0.6f);
            for (int i = 0; i < Archive.All.Length; i++)
            {
                float a = UiMotion.Between(time, 0.25f + i * BattleRules.UiEnterStagger, BattleRules.UiEnterTime);
                Rectangle r = RowRect(i);
                float cy = r.Center.Y;
                float e = rowMotion[i].Shown;
                Gfx.Rect(sb, SpineX, cy, (RowX - SpineX - 10) * a, 1, Palette.LineGrey * (0.6f * a));
                Gfx.Circle(sb, SpineX, cy, 5f, Palette.StageDeep * a);
                Gfx.CircleOutline(sb, SpineX, cy, 5f, Color.Lerp(Palette.LineGrey, Palette.Paper, e) * a, 1.5f);
                if (Archive.All[i].Level > 0) Gfx.Circle(sb, SpineX, cy, 3f, Palette.Paper * a);
                if (rowMotion[i].Ring < 1f) Gfx.CircleOutline(sb, SpineX, cy, 5f + 22f * rowMotion[i].Ring, Palette.Paper * ((1f - rowMotion[i].Ring) * 0.8f * a), 1.5f);
            }
        }

        //Lights : a soft light on the pointed row
        public override void DrawLights(SpriteBatch sb)
        {
            Rectangle r = RowRect(selected);
            Gfx.DrawGlow(sb, r.X + 300f, r.Center.Y, 360f, Color.White * (0.4f * rowMotion[selected].Shown));
        }

        //======================================================================================
        //The Words (crisp on top)
        //======================================================================================

        public override void Draw(SpriteBatch sb)
        {
            DrawTitleBar(sb);

            for (int i = 0; i < Archive.All.Length; i++)
            {
                float a = UiMotion.Between(time, 0.25f + i * BattleRules.UiEnterStagger, BattleRules.UiEnterTime);
                if (a <= 0f) continue;
                ArchiveUpgrade u = Archive.All[i];
                Rectangle r = RowRect(i);
                r.X += (int)Math.Round((1f - a) * BattleRules.UiEnterSlide);
                UiMotion m = rowMotion[i];
                float e = m.Shown;
                bool canBuy = !u.Maxed && Archive.Echoes >= u.NextPrice;

                //Name : the ink bar shoots out under it when pointed, the name turns black (like the menus)
                float scale = TextSize.Small * m.Scale;
                float nameW = Gfx.SpacedWidth(Game.BigFont, u.Name, scale, 2f);
                if (e > 0f)
                {
                    Ui.InkBar(sb, r.X - 6 + BattleRules.UiShadow, r.Y + 4 + BattleRules.UiShadow, (nameW + 40f) * e, 30f, 10f, Palette.LineGrey * (0.5f * a), i * 11 + 3, 1f);
                    Ui.InkBar(sb, r.X - 6, r.Y + 4, (nameW + 40f) * e, 30f, 10f, Palette.Paper * a, i * 11, 1f);
                }
                Gfx.Text(sb, Game.Font, numbers[i], r.X - 0, r.Y - 12, Palette.LineGrey * a, TextSize.Tiny);
                Gfx.TextSpaced(sb, Game.BigFont, u.Name, r.X + 6, r.Y + 6, Color.Lerp(Palette.Paper, Palette.Ink, e) * a, scale, 2f);
                Gfx.Text(sb, Game.StoryFont, u.Text, r.X + 8, r.Y + 42, Palette.PaperDim * a, TextSize.StorySmall);

                //Levels : one circle per level, filled when bought
                for (int k = 0; k < u.Prices.Length; k++)
                {
                    float px = r.X + 520 + k * 20;
                    if (k < u.Level) Gfx.Circle(sb, px, r.Y + 22, 5f, Palette.Paper * a);
                    else Gfx.CircleOutline(sb, px, r.Y + 22, 5f, Palette.LineGrey * a, 1f);
                }

                //Price : the key that buys it, bright when there are enough echoes
                Color ink = canBuy ? Palette.Highlight : (u.Maxed ? Palette.LineGrey : Palette.PaperDim * 0.7f);
                Gfx.TextSpacedRight(sb, Game.Font, priceLabel[i], r.Right - 44, r.Y + 13, ink * a, TextSize.Label, 2f);
                if (!u.Maxed) Ui.KeyBox(sb, keys[i], r.Right - 34, r.Y + 22, canBuy ? Palette.Paper : Palette.LineGrey * 0.6f, Palette.Ink, a);
                Gfx.Rect(sb, r.X, r.Bottom - 2, r.Width, 1, Palette.LineGrey * (0.25f * a));
                if (m.Flash > 0f) Gfx.DrawGlow(sb, r.X + 160, r.Y + 22, 220f, Color.White * (0.5f * m.Flash));
            }

            //Echoes : the big number of what there is to spend, its thin line full (menu7)
            float low = UiMotion.Between(time, 0.6f, BattleRules.UiEnterTime);
            Ui.Number(sb, Words.Applause + " TO SPEND", shownEchoes, false, 860, 566, 320, 1f, Palette.Highlight, Palette.PaperDim, low);

            if (messageLife > 0f)
                Gfx.TextSpacedCentered(sb, Game.Font, message, 640, 620, Palette.Highlight * Math.Min(1f, messageLife), TextSize.Body, 3f);

            Ui.Command(sb, backButton, "BACK", "ESC", backMotion, low, false);
            Gfx.TextSpacedRight(sb, Game.Font, "ARROWS  CHOOSE   /   ENTER OR 1 TO 6  BUY", 1240, 690, Palette.LineGrey * low, TextSize.Tiny, 2f);
        }

        //Title Bar : the page's name, big and turned, on a tall bar at the left (menu1)
        private void DrawTitleBar(SpriteBatch sb)
        {
            float a = UiMotion.Between(time, 0f, 0.45f);
            int x = 40 + (int)Math.Round((1f - a) * -40f);
            Ui.TitleBar(sb, new Rectangle(x, 80, 96, 540), 24, "THE ARCHIVE", a);
            Gfx.TextSpaced(sb, Game.Font, "EVERY RUN EARNS " + Words.Applause, x + 4, 630, Palette.LineGrey * a, TextSize.Tiny, 2f);
        }
    }
}
