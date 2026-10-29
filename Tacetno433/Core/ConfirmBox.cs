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
    //
    //Beta1 screens round 3 (29 Oct, docs/SCREENS_LEVEL_UP.md 20) : a dark slanted plate with a thin bright edge, the question
    //big, the line under it, and the two answers as Ui.Command plates. The chosen answer is drawn as the pointed one
    //(white, torn like ink), so the keys and the mouse always show the same thing.
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
        private UiMotion yesMotion = new UiMotion();
        private UiMotion noMotion = new UiMotion();

        //Box Layout
        private Rectangle box = new Rectangle(350, 240, 580, 236);
        private Rectangle yesButton = new Rectangle(404, 404, 220, 46);
        private Rectangle noButton = new Rectangle(654, 404, 220, 46);
        private const int BoxSlant = 30;
        private const float LineWrap = 480f;

        //Box Show : the text is wrapped here, once, never while drawing
        public void Show(string title, string line, string yes, string no)
        {
            this.title = title;
            lineWrapped = Gfx.WrapText(Ui.StoryFont, line, LineWrap, TextSize.Story);
            yesText = yes;
            noText = no;
            yesChosen = false;
            yesMotion = new UiMotion();
            noMotion = new UiMotion();
            noMotion.Snap(true);
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
            yesMotion.Update(dt, yesChosen);
            noMotion.Update(dt, !yesChosen);

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

        //Box Draw : the page goes dark, the plate drops in a little, the two answers under the line
        public void Draw(SpriteBatch sb)
        {
            if (!Open) return;
            float a = appear;

            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Color.Black * (0.6f * a));
            int drop = (int)((1f - UiMotion.EaseOut(a)) * 16f);
            Rectangle r = box;
            r.Y += drop;

            //Plate : a shadow, the dark slanted plate, its thin bright edge
            Gfx.SlantBox(sb, new Rectangle(r.X + 8, r.Y + 8, r.Width, r.Height), BoxSlant, Color.Black * (0.5f * a));
            Gfx.SlantBox(sb, r, BoxSlant, Palette.Void * (0.97f * a));
            Gfx.SlantOutline(sb, r, BoxSlant, Palette.Paper * (0.55f * a), 1f);

            //Question : big, then a thin line with a small ring at its start (menu3), then the line
            Gfx.Text(sb, Ui.BigFont, title, r.X + 60, r.Y + 26, Palette.Highlight * a, TextSize.Subtitle);
            Gfx.CircleOutline(sb, r.X + 58, r.Y + 80, 3f, Palette.LineGrey * a, 1f);
            Gfx.Rect(sb, r.X + 64, r.Y + 80, r.Width - BoxSlant - 100, 1, Palette.LineGrey * (0.6f * a));
            Gfx.Text(sb, Ui.StoryFont, lineWrapped, r.X + 54, r.Y + 94, Palette.PaperDim * a, TextSize.Story);

            Rectangle yes = yesButton;
            Rectangle no = noButton;
            yes.Y += drop;
            no.Y += drop;
            Ui.Command(sb, yes, yesText, "Y", yesMotion, a, false);
            Ui.Command(sb, no, noText, "N", noMotion, a, false);
        }
    }
}
