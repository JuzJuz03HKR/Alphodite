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
    //Click the price or press 1 to 5 to buy. ESC goes back to the title.
    public class ArchiveScreen : GameScreen
    {
        private const int RowX = 190;
        private const int RowW = 900;
        private const int RowY = 150;
        private const int RowH = 68;
        private const int RowStep = 76;      // six rows since FULL STAGE (step 12)

        private Rectangle[] buyBox = new Rectangle[Archive.All.Length];
        private Rectangle backButton = new Rectangle(40, 640, 190, 48);
        private static readonly string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8" };

        //Prepared Text : rebuilt after every purchase, never in Draw
        private string[] priceLabel = new string[Archive.All.Length];
        private string echoesLabel = "";
        private string message = "";
        private float messageLife;
        private float enter;

        public override bool CanPause
        {
            get { return false; }
        }

        public override void Load()
        {
            for (int i = 0; i < Archive.All.Length; i++)
                buyBox[i] = new Rectangle(RowX + RowW - 220, RowY + i * RowStep + 12, 200, 50);
            Refresh();
        }

        private void Refresh()
        {
            echoesLabel = NumberText.Get(Archive.Echoes);
            for (int i = 0; i < Archive.All.Length; i++)
            {
                ArchiveUpgrade u = Archive.All[i];
                priceLabel[i] = u.Maxed ? "TOP LEVEL" : NumberText.Get(u.NextPrice) + " ECHOES";
            }
        }

        public override void Update(float dt)
        {
            enter = MathHelper.Min(1f, enter + dt * 3f);
            if (messageLife > 0f) messageLife -= dt;

            for (int i = 0; i < Archive.All.Length; i++)
                if (Input.ClickedOn(buyBox[i]) || Input.KeyPressed(Keys.D1 + i)) Try(i);

            if (Input.ClickedOn(backButton) || Input.KeyPressed(Keys.Escape))
            {
                SoundBank.Play(Sfx.UiConfirm);
                Game.Screens.Change(new TitleScreen());
            }
        }

        //Buy : pay and keep it for good, saved at once
        private void Try(int i)
        {
            ArchiveUpgrade u = Archive.All[i];
            if (Archive.Buy(i))
            {
                SaveFile.SaveArchive();
                SoundBank.Play(Sfx.UiConfirm);
                message = u.Name + "  " + NumberText.Get(u.Level) + " / " + NumberText.Get(u.Prices.Length);
            }
            else
            {
                SoundBank.Play(Sfx.UiDenied);
                message = u.Maxed ? "ALREADY AT THE TOP LEVEL" : "NOT ENOUGH ECHOES";
            }
            messageLife = 2f;
            Refresh();
        }

        public override void Draw(SpriteBatch sb)
        {
            float a = enter;
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageDeep);

            Gfx.TextCentered(sb, Game.BigFont, "THE ARCHIVE", TacetGame.ScreenW / 2f, 62, Palette.Highlight * a, TextSize.Title);
            Gfx.TextCentered(sb, Game.StoryFont, "Every run leaves echoes behind. What you buy here stays for good.", TacetGame.ScreenW / 2f, 108, Palette.PaperDim * a, TextSize.StorySmall);

            //Echoes : what there is to spend
            Gfx.TextSpacedRight(sb, Game.Font, "ECHOES", TacetGame.ScreenW - 60, 40, Palette.LineGrey * a, TextSize.Label, 3f);
            Gfx.TextRight(sb, Game.BigFont, echoesLabel, TacetGame.ScreenW - 60, 62, Palette.Highlight * a, TextSize.Subtitle);

            for (int i = 0; i < Archive.All.Length; i++)
            {
                ArchiveUpgrade u = Archive.All[i];
                Rectangle row = new Rectangle(RowX, RowY + i * RowStep, RowW, RowH);
                Ui.Panel(sb, row, a);

                Gfx.TextSpaced(sb, Game.BigFont, u.Name, row.X + 24, row.Y + 10, Palette.Paper * a, TextSize.Small, 2f);
                Gfx.Text(sb, Game.Font, u.Text, row.X + 24, row.Y + 44, Palette.PaperDim * a, TextSize.Body);
                Ui.Pips(sb, row.X + 470, row.Y + 37, u.Level, u.Prices.Length, 6f, 18f, a);

                bool canBuy = !u.Maxed && Archive.Echoes >= u.NextPrice;
                Ui.Button(sb, buyBox[i], priceLabel[i], keys[i], canBuy, !u.Maxed, a);
            }

            if (messageLife > 0f)
                Gfx.TextSpacedCentered(sb, Game.Font, message, TacetGame.ScreenW / 2f, 622, Palette.Highlight * MathHelper.Min(1f, messageLife), TextSize.Body, 3f);

            Ui.Button(sb, backButton, "BACK", "ESC", false);
        }
    }
}
