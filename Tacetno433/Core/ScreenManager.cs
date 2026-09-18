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

        //Screen Draw
        public abstract void Draw(SpriteBatch sb);
    }

    //ScreenManager : holds the screen we are on, and fades to black when swapping to another one
    public class ScreenManager
    {
        private TacetGame game;
        private GameScreen current;
        private GameScreen waiting;      // the screen we will swap to once the fade finishes

        private float fade;              // 0 = normal picture, 1 = fully black
        private bool fadingOut;
        private const float FadeSpeed = 3f;

        public ScreenManager(TacetGame game)
        {
            this.game = game;
        }

        public GameScreen Current { get { return current; } }

        //Screen Change : fade out, swap, fade back in
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

        //Screen Update : run the fade, then update the active screen
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

        //Screen Draw : the active screen, then the black fade on top
        public void Draw(SpriteBatch sb)
        {
            if (current != null) current.Draw(sb);

            if (fade > 0f)
                Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Color.Black * fade);
        }
    }
}
