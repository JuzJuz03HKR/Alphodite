using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;

namespace Tacetno433.Core
{
    //ConfirmBox : "are you sure?" before something that cannot be taken back.
    //
    //A page keeps one of these. Show opens it, then the page calls Update every frame while
    //it is Open and does nothing else, and draws it last so it sits over everything.
    //   Update returns  1  for YES,  -1  for NO,  0  while the player is still deciding.
    //NO is chosen at the start, so pressing ENTER by accident is always safe.
    //Keys : LEFT and RIGHT choose, ENTER presses, Y is yes, N or ESC is no. The mouse clicks.
    public class ConfirmBox
    {
        public bool Open;

        private string title = "";
        private string lineWrapped = "";
        private string yesText = "";
        private string noText = "";
        private bool yesChosen;
        private float appear;
        private Vector2 lastMouse;

        //Box Layout
        private Rectangle box = new Rectangle(390, 250, 500, 220);
        private Rectangle yesButton = new Rectangle(420, 396, 210, 48);
        private Rectangle noButton = new Rectangle(650, 396, 210, 48);
        private const float LineWrap = 440f;

        //Box Show : the text is wrapped here, once, never while drawing
        public void Show(string title, string line, string yes, string no)
        {
            this.title = title;
            lineWrapped = Gfx.WrapText(Ui.StoryFont, line, LineWrap, TextSize.Story);
            yesText = yes;
            noText = no;
            yesChosen = false;
            appear = 0f;
            lastMouse = Input.MousePos;
            Open = true;
            SoundBank.Play(Sfx.PageTurn, 0.6f, 0f);
        }

        //Box Update : 1 yes, -1 no, 0 still deciding
        public int Update(float dt)
        {
            if (!Open) return 0;
            appear = System.Math.Min(1f, appear + dt * 6f);

            if (Input.KeyPressed(Keys.Left) || Input.KeyPressed(Keys.A)) yesChosen = true;
            if (Input.KeyPressed(Keys.Right) || Input.KeyPressed(Keys.D)) yesChosen = false;

            //Mouse : only a mouse that moves changes the choice, so a pointer resting over YES
            //when the box opens cannot turn a stray ENTER into a yes
            if (Input.MousePos != lastMouse)
            {
                lastMouse = Input.MousePos;
                if (Input.MouseOver(yesButton)) yesChosen = true;
                if (Input.MouseOver(noButton)) yesChosen = false;
            }

            bool yes = Input.KeyPressed(Keys.Y) || Input.ClickedOn(yesButton);
            bool no = Input.KeyPressed(Keys.N) || Input.KeyPressed(Keys.Escape) || Input.ClickedOn(noButton);
            if (Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Space))
            {
                if (yesChosen) yes = true;
                else no = true;
            }

            if (yes)
            {
                Open = false;
                SoundBank.Play(Sfx.UiConfirm);
                return 1;
            }
            if (no)
            {
                Open = false;
                SoundBank.Play(Sfx.UiBack);
                return -1;
            }
            return 0;
        }

        //Box Draw : the page goes dark, the box sits in the middle with its two buttons
        public void Draw(SpriteBatch sb)
        {
            if (!Open) return;
            float a = appear;

            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Color.Black * (0.6f * a));
            Rectangle r = box;
            r.Y += (int)((1f - a) * 16f);

            Gfx.Rect(sb, r, Palette.Void * a);
            Ornament.DoubleFrame(sb, r, Palette.Paper * a);
            Hollow.Streak(sb, r.Center.X, r.Y, r.Width * 0.8f, 0.5f * a);

            Gfx.TextCentered(sb, Ui.BigFont, title, r.Center.X, r.Y + 46, Palette.Highlight * a, TextSize.Subtitle);
            Gfx.Text(sb, Ui.StoryFont, lineWrapped, r.X + 30, r.Y + 80, Palette.PaperDim * a, TextSize.Story);

            Ui.Button(sb, yesButton, yesText, "Y", yesChosen, true, a);
            Ui.Button(sb, noButton, noText, "N", !yesChosen, true, a);
        }
    }
}
