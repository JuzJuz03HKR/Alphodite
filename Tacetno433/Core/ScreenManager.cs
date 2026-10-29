using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

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
        public virtual bool UsesCurtain
        {
            get { return false; }
        }

        //Screen Enters With Wave : true on the pages TACET's silence swallows the stage into, the
        //duel and the end of a run. Every other page changes with the quick slant wipe (round 15,
        //29 Sep : the player found the wave on every page too much).
        public virtual bool EntersWithWave
        {
            get { return false; }
        }

        //Screen Enters With Sweep (Beta1 screens) : the RING SWEEP this page is always entered with (the fight : TACET's
        //dark opening from where its hole will hang), None for the covers above. It wins over a SweepNext, which can ask
        //for a ring on any other change.
        public virtual RingSweep.Kind EntersWithSweep
        {
            get { return RingSweep.Kind.None; }
        }

        //Sweep From : where that ring opens from (screen pixels)
        public virtual Vector2 SweepFrom
        {
            get { return new Vector2(TacetGame.ScreenW / 2f, TacetGame.ScreenH / 2f); }
        }

        //Wipe Word (Beta1 screens) : the page's name, run big and dim through the black of the SLANT WIPE on the way to
        //this page (the user's pick, 28 Oct : the name of the page you are going to, TACET 4'33 only on the way back to
        //the title). A name of two words or more is split at its last space into two lines that cross, like TACET and
        //4'33. null = no words. It is read once, when the change starts (ScreenManager.Change), and the page's Game is
        //already set by then, so a page may pick its name from the run.
        public virtual string WipeWord
        {
            get { return null; }
        }

        //Screen Pre Draw (Beta1) : called every frame before anything is drawn on the screen, for a page that
        //draws into its own pictures first (render targets : the battle's layers, see Core/PostChain.cs and OwnLens, or the
        //reward page's cards, Core/Card3D.cs). Draw then shows them.
        public virtual void PreDraw(SpriteBatch sb) { }

        //======================================================================================
        //THE LENS (Beta1 screens) : how ScreenManager draws a page, see PostChain.DrawPage
        //======================================================================================

        //Own Lens : true for the page that builds and finishes its own picture in PreDraw (the battle)
        public virtual bool OwnLens
        {
            get { return false; }
        }

        //Lens : how this page's picture is finished (its light, bloom, vignette ...). A page may change it while it runs.
        public PostChain.PageLook Lens = new PostChain.PageLook();

        //Dark : TACET's dark on this page (Core/TacetVoid.cs), or null. A page that has one places it in its Update.
        public TacetVoid Dark;

        //Has World : true for a page split in two. DrawWorld is the stage : it is lit, it glows, the vignette darkens
        //its corners. Draw is then only the words and buttons, laid crisp on top. A page that says false goes through
        //the lens whole (a soft stage light, bloom and vignette).
        public virtual bool HasWorld
        {
            get { return false; }
        }

        //Draw Back : what TACET's dark bends, only used when the page has Dark
        public virtual void DrawBack(SpriteBatch sb) { }

        //Draw World : the stage of a page with a world
        public virtual void DrawWorld(SpriteBatch sb) { }

        //Draw Lights : into the LIGHT MAP. It is half the size of the screen but drawn through PostChain.LightScale,
        //so positions are screen pixels as everywhere else. The plain light is one soft spot (Lens.Spot).
        public virtual void DrawLights(SpriteBatch sb)
        {
            PostChain.SpotLight(sb, Lens);
        }

        //Draw Glow : effects that give off their own light. They are added, so they never darken, and they bloom.
        public virtual void DrawGlow(SpriteBatch sb) { }

        //Shows Grain : the film grain laid over every page. A page with its own grain (the battle, Post.fx) turns it off.
        public virtual bool ShowsGrain
        {
            get { return true; }
        }

        //Screen Draw
        public abstract void Draw(SpriteBatch sb);
    }

    //ScreenManager : holds the screen we are on, and covers the screen while swapping to another.
    //Four covers (round 15, Beta1) :
    //   SWEEP    the moments that matter (a run begins, into and out of a fight, a win) : a ring opens from a point
    //            and floods the screen, or TACET's dark closes in (Core/RingSweep.cs). Asked for with SweepNext.
    //   CURTAIN  going to or from the conductor select page, the stage curtain (Core/Curtain.cs)
    //   WAVE     the old duel and the curtain call, TACET's silence washing over the page and pulling back
    //            (Core/SilenceWave.cs)
    //   WIPE     every other page, slanted bands of white, grey and black, the name of the next page running through
    //            the black (Core/SlantWipe.cs, GameScreen.WipeWord)
    //Every page but the battle is drawn through THE LENS (Core/PostChain.cs DrawPage) : a stage light, bloom and
    //vignette, and it comes up from black as it opens (ENTER).
    public class ScreenManager
    {
        private TacetGame game;
        private GameScreen current;
        private GameScreen waiting;      // the screen we will swap to once the screen is covered

        private float fade;              // 0 = nothing covers the page, 1 = fully covered
        private bool fadingOut;
        private bool curtain;            // this change uses the stage curtain
        private bool wave;               // this change uses TACET's wave, otherwise the slant wipe
        private float time;              // keeps the wave's torn edge moving
        private const float FadeSpeed = 1.7f;   // each half of a change takes 1 / FadeSpeed seconds (round 15 : 3 felt too fast, it flickered ;
                                                // Beta1 round 4 : 2.2 still felt fast, the user asked for slower and smoother)

        //RING SWEEP (Beta1 screens) : asked for by SweepNext, used by the next Change
        private RingSweep.Kind sweep;          // this change's ring, None = one of the other covers
        private Vector2 sweepFrom;
        private RingSweep.Kind sweepNext;
        private Vector2 sweepNextFrom;

        //ENTER (Beta1 screens) : a new page comes up from black (menu4). Not under a RING SWEEP, which fades off it by itself.
        private float pageTime;          // seconds since the page on show was loaded
        private bool upFromBlack = true;

        public ScreenManager(TacetGame game)
        {
            this.game = game;
        }

        public GameScreen Current { get { return current; } }

        //Busy : a fade is under way, so the page on show is about to change or has just changed
        public bool Busy { get { return fadingOut || fade > 0f; } }

        //Sweep Next : the next Change uses the RING SWEEP from this point (screen pixels)
        public void SweepNext(Vector2 from, RingSweep.Kind kind)
        {
            sweepNextFrom = from;
            sweepNext = kind;
        }

        //Screen Change : cover the page, swap, uncover the new one
        public void Change(GameScreen screen)
        {
            waiting = screen;
            waiting.Game = game;                // WIPE WORD : the name may come from the run
            SlantWipe.SetWord(waiting.WipeWord);
            fadingOut = true;
            sweep = sweepNext;
            sweepFrom = sweepNextFrom;
            sweepNext = RingSweep.Kind.None;
            if (screen.EntersWithSweep != RingSweep.Kind.None)          // a fight is always entered through TACET's dark
            {
                sweep = screen.EntersWithSweep;
                sweepFrom = screen.SweepFrom;
            }
            bool plain = sweep == RingSweep.Kind.None;
            bool curtainShowing = curtain && fade > 0f;
            curtain = plain && (screen.UsesCurtain || (current != null && current.UsesCurtain));
            wave = plain && !curtain && screen.EntersWithWave;
            if (curtain && !curtainShowing) Curtain.Begin(fade);      // REAL CLOTH : a curtain already moving carries on as it is
        }

        //Screen Change Now : swap with no fade (used for the very first screen). A cover still on its way is dropped.
        public void ChangeNow(GameScreen screen)
        {
            fade = 0f;
            fadingOut = false;
            waiting = null;
            Swap(screen);
        }

        //Screen Cut (Beta1 screens) : swap at once, with no cover and without coming up from black. For two pages that hand
        //one picture across (SHARED PICTURE : the gallery's painting walks into the profile page, and back). The old page
        //plays its own way out first, the new page starts with the picture exactly where the old one left it, so the eye
        //follows the picture and never sees the change.
        public void Cut(GameScreen screen)
        {
            fade = 0f;
            fadingOut = false;
            waiting = null;
            sweep = RingSweep.Kind.None;
            curtain = false;
            Swap(screen);
            upFromBlack = false;
        }

        //Swap : the page changes, under a cover or straight away
        private void Swap(GameScreen screen)
        {
            if (current != null) current.Leave();

            current = screen;
            current.Game = game;
            pageTime = 0f;
            upFromBlack = sweep == RingSweep.Kind.None || fade <= 0f;     // a ring fades off the new page by itself
            current.Load();
        }

        //Lights Up : 0..1, how far the page on show has come up from black (ENTER), eased to a soft stop
        private float LightsUp
        {
            get
            {
                if (!upFromBlack) return 1f;
                float t = MathHelper.Clamp(pageTime / BattleRules.UiLightsUp, 0f, 1f);
                return UiMotion.EaseOut(t);
            }
        }

        //Screen Update : run the cover, then update the active screen
        public void Update(float dt)
        {
            time += dt;
            pageTime += dt;
            if (fadingOut)
            {
                fade += dt * FadeSpeed;
                if (fade >= 1f)
                {
                    fade = 1f;
                    Swap(waiting);
                    waiting = null;
                    fadingOut = false;
                }
            }
            else if (fade > 0f)
            {
                fade -= dt * FadeSpeed;
                if (fade < 0f) fade = 0f;
            }
            if (curtain && (fadingOut || fade > 0f)) Curtain.Update(dt, fade);     // REAL CLOTH : the drapes move in Update

            //Input Lock : while the screen is fading out the old page must stop updating.
            //Otherwise a second key press during the fade would run its action twice, for
            //example advancing the run two stages from one button.
            if (fadingOut) return;

            if (current != null) current.Update(dt);
        }

        //Shows Grain : whether the page on screen wants the film grain over it
        public bool ShowsGrain
        {
            get { return current == null || current.ShowsGrain; }
        }

        //Screen Pre Draw : the page's picture, before the frame starts. The battle builds its own,
        //every other page goes through THE LENS.
        public void PreDraw(SpriteBatch sb)
        {
            if (current == null) return;
            current.PreDraw(sb);
            if (!current.OwnLens) PostChain.DrawPage(game.GraphicsDevice, sb, current, time);
        }

        //Screen Draw : the active screen, then the cover on top while it is moving
        public void Draw(SpriteBatch sb)
        {
            if (current != null)
            {
                if (current.OwnLens) current.Draw(sb);
                else
                {
                    PostChain.ShowPage(sb, current.Lens, LightsUp);
                    if (current.HasWorld) current.Draw(sb);       // the words and buttons, crisp on top
                }
            }
            if (fade <= 0f) return;

            if (sweep != RingSweep.Kind.None) RingSweep.Draw(sb, fade, fadingOut, sweepFrom, sweep, time);
            else if (curtain) Curtain.Draw(sb, fade);
            else if (wave) SilenceWave.Draw(sb, fade, time);
            else SlantWipe.Draw(sb, fade, fadingOut);
        }
    }
}
