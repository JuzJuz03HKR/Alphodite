using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //GameScreen : base class for every page of the game (title, map, preparation, duel, ...)
    //Each page only has to fill in Load / Update / Draw.
    public abstract class GameScreen
    {
        public TacetGame Game;

        //Screen Load : called once when this screen becomes active
        public virtual void Load() { }

        //Screen Leave : called once when we swap away from this screen.
        //Use it to undo anything Load set up outside the screen itself.
        public virtual void Leave() { }

        //Screen Update : dt is seconds since the last frame
        public virtual void Update(float dt) { }

        //Screen Lost Focus : called once when the window goes to the back (alt tab).
        //On pages that can pause, the pause menu opens by itself as well.
        public virtual void LostFocus() { }

        //Screen Can Pause : true on every page of a run, where ESC opens the pause menu.
        //Menu pages outside a run (title, settings, the guide, conductor select) say false and
        //keep ESC for going back.
        public virtual bool CanPause
        {
            get { return true; }
        }

        //Screen Uses Escape : true while the page has something of its own open that ESC
        //should close first, for example picking a musician on an event page
        public virtual bool UsesEscape
        {
            get { return false; }
        }

        //Screen Paused / Resumed : the pause menu opened over this page, or closed again.
        //While it is open this page gets no Update, but it is still drawn underneath.
        public virtual void Paused() { }
        public virtual void Resumed() { }

        //Screen Draw
        public abstract void Draw(SpriteBatch sb);
    }

    //ScreenManager : holds the screen we are on, and closes the stage curtain when swapping to
    //another one (see Core/Curtain.cs). The old page goes behind the curtain, the new one is
    //revealed when it opens again.
    public class ScreenManager
    {
        private TacetGame game;
        private GameScreen current;
        private GameScreen waiting;      // the screen we will swap to once the curtain is closed

        private float fade;              // 0 = curtain open, 1 = curtain fully closed
        private bool fadingOut;
        private const float FadeSpeed = 3f;

        public ScreenManager(TacetGame game)
        {
            this.game = game;
        }

        public GameScreen Current { get { return current; } }

        //Busy : a fade is under way, so the page on show is about to change or has just changed
        public bool Busy { get { return fadingOut || fade > 0f; } }

        //Screen Change : close the curtain, swap, open it again
        public void Change(GameScreen screen)
        {
            waiting = screen;
            fadingOut = true;
        }

        //Screen Change Now : swap with no fade (used for the very first screen)
        public void ChangeNow(GameScreen screen)
        {
            if (current != null) current.Leave();

            current = screen;
            current.Game = game;
            current.Load();
        }

        //Screen Update : run the curtain, then update the active screen
        public void Update(float dt)
        {
            if (fadingOut)
            {
                fade += dt * FadeSpeed;
                if (fade >= 1f)
                {
                    fade = 1f;
                    ChangeNow(waiting);
                    waiting = null;
                    fadingOut = false;
                }
            }
            else if (fade > 0f)
            {
                fade -= dt * FadeSpeed;
                if (fade < 0f) fade = 0f;
            }

            //Input Lock : while the screen is fading out the old page must stop updating.
            //Otherwise a second key press during the fade would run its action twice, for
            //example advancing the run two stages from one button.
            if (fadingOut) return;

            if (current != null) current.Update(dt);
        }

        //Screen Draw : the active screen, then the curtain on top while it is moving
        public void Draw(SpriteBatch sb)
        {
            if (current != null) current.Draw(sb);

            if (fade > 0f) Curtain.Draw(sb, fade);
        }
    }
}
