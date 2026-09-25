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

        //Screen Uses Curtain : true on the page where the stage curtain opens (conductor select).
        //Every other page changes with the plain silence wave.
        public virtual bool UsesCurtain
        {
            get { return false; }
        }

        //Screen Draw
        public abstract void Draw(SpriteBatch sb);
    }

    //ScreenManager : holds the screen we are on, and covers the screen while swapping to another.
    //The cover is TACET's silence washing over the page and pulling back (Core/SilenceWave.cs),
    //or, going to or from the conductor select page, the stage curtain (Core/Curtain.cs).
    public class ScreenManager
    {
        private TacetGame game;
        private GameScreen current;
        private GameScreen waiting;      // the screen we will swap to once the screen is covered

        private float fade;              // 0 = nothing covers the page, 1 = fully covered
        private bool fadingOut;
        private bool curtain;            // this change uses the stage curtain instead of the wave
        private float time;              // keeps the wave's torn edge moving
        private const float FadeSpeed = 3f;

        public ScreenManager(TacetGame game)
        {
            this.game = game;
        }

        public GameScreen Current { get { return current; } }

        //Busy : a fade is under way, so the page on show is about to change or has just changed
        public bool Busy { get { return fadingOut || fade > 0f; } }

        //Screen Change : cover the page, swap, uncover the new one
        public void Change(GameScreen screen)
        {
            waiting = screen;
            fadingOut = true;
            curtain = screen.UsesCurtain || (current != null && current.UsesCurtain);
        }

        //Screen Change Now : swap with no fade (used for the very first screen)
        public void ChangeNow(GameScreen screen)
        {
            if (current != null) current.Leave();

            current = screen;
            current.Game = game;
            current.Load();
        }

        //Screen Update : run the cover, then update the active screen
        public void Update(float dt)
        {
            time += dt;
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

        //Screen Draw : the active screen, then the cover on top while it is moving
        public void Draw(SpriteBatch sb)
        {
            if (current != null) current.Draw(sb);
            if (fade <= 0f) return;

            if (curtain) Curtain.Draw(sb, fade);
            else SilenceWave.Draw(sb, fade, time);
        }
    }
}
