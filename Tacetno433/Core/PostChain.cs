using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //PostChain : the battle picture is drawn in layers, then finished in passes (Beta1).
    //
    //   BACK       the screen   the era's backdrop and the stage floor alone, so TACET's dark can bend them (Void.fx)
    //   SCENE      the screen   the world : BACK through TACET's dark, the enemies, the musician
    //   LIGHT MAP   640 x 360   the lights : a dim grey everywhere, brighter where a light shines, kept at
    //                           half strength so a flash can go above normal (LIGHT x 2, see Post.fx)
    //   GLOW       the screen   effects that give off their own light (sparks, slashes, flares) : added after the
    //                           lights, so the dark never dims them, and they feed the BLOOM
    //   SOFT        320 x 180   the lit world shrunk and blurred : the glow (BLOOM) and the QTE FOCUS come from it
    //   FINAL      the screen   SCENE x LIGHT + GLOW + BLOOM, then FOCUS, ENGULF, VIGNETTE and grain
    //
    //"The screen" is the real size the page is shown at (SHARP SCREEN, Core/Sharp.cs) : 1280 x 720 in the window,
    //1920 x 1080 full screen on such a monitor. Everything is drawn into it in game pixels through Sharp.Transform.
    //The light map and the soft pictures stay the same size on every screen : they are soft anyway, and keeping them
    //small keeps the glow exactly as wide as it was tuned, and cheap.
    //
    //The battle draws SCENE, LIGHT MAP and GLOW in its PreDraw, calls Finish, then draws FINAL in its Draw with the
    //words and buttons on top. Every other page goes through DrawPage (Beta1 screens, ScreenManager does it) :
    //the same layers, so every page gets a stage light, bloom and vignette (THE LENS). Without shaders (Fx.Ready false)
    //the same picture is put together with blend states only : no glow and no blur, the rest looks the same.
    //
    //Optimize : the big pictures are made once and kept (made again only when the screen changes size). Blur and glow
    //work at 320 x 180, sixteen times fewer pixels than the window and up to a hundred and forty times fewer than 4K.
    public static class PostChain
    {
        public const int W = TacetGame.ScreenW;
        public const int H = TacetGame.ScreenH;

        public static RenderTarget2D Back;
        public static RenderTarget2D Scene;
        public static RenderTarget2D Light;
        public static RenderTarget2D Glow;
        public static RenderTarget2D Final;
        private static RenderTarget2D softA;
        private static RenderTarget2D softB;

        //Light Scale : the light map is half the size of the page, so lights are drawn through this
        public static readonly Matrix LightScale = Matrix.CreateScale(0.5f);

        //Ensure : make the pictures the first time they are needed, after the card lost them, or when the screen
        //changed size (full screen on or off)
        public static void Ensure(GraphicsDevice device)
        {
            if (Scene != null && !Scene.IsDisposed && Scene.GraphicsDevice == device
                && Scene.Width == Sharp.Width && Scene.Height == Sharp.Height) return;

            for (int i = 0; i < 5; i++) device.Textures[i] = null;      // nothing still plugged in when the old ones go
            Release();
            int w = Sharp.Width;
            int h = Sharp.Height;

            //SHARP SCREEN : on a screen bigger than the page, SCENE and GLOW keep smaller copies of themselves (MIPMAPS, the
            //card makes them each frame), so the Down pass can read them at the page's own 1280 x 720 (see Down)
            bool shrinks = Sharp.Scale > 1f;
            Back = new RenderTarget2D(device, w, h);
            Scene = new RenderTarget2D(device, w, h, shrinks, SurfaceFormat.Color, DepthFormat.None);
            Light = new RenderTarget2D(device, W / 2, H / 2);
            Glow = new RenderTarget2D(device, w, h, shrinks, SurfaceFormat.Color, DepthFormat.None);
            Final = new RenderTarget2D(device, w, h);
            softA = new RenderTarget2D(device, W / 4, H / 4);
            softB = new RenderTarget2D(device, W / 4, H / 4);
        }

        //Release : give the pictures back to the card (before making them at a new size)
        private static void Release()
        {
            if (Back != null) Back.Dispose();
            if (Scene != null) Scene.Dispose();
            if (Light != null) Light.Dispose();
            if (Glow != null) Glow.Dispose();
            if (Final != null) Final.Dispose();
            if (softA != null) softA.Dispose();
            if (softB != null) softB.Dispose();
        }

        //Begin Back : the backdrop and the floor, before TACET's dark is laid over them
        public static void BeginBack(GraphicsDevice device)
        {
            device.SetRenderTarget(Back);
            device.Clear(Palette.Void);
        }

        //Begin Scene : everything drawn until the page sets another target lands in SCENE
        public static void BeginScene(GraphicsDevice device)
        {
            device.SetRenderTarget(Scene);
            device.Clear(Palette.Void);
        }

        //Begin Lights : the light map starts at ambient (0..1, 0.5 = lit normally), lights are then added on top
        public static void BeginLights(GraphicsDevice device, SpriteBatch sb, float ambient)
        {
            device.SetRenderTarget(Light);
            float half = MathHelper.Clamp(ambient, 0f, 1f);
            device.Clear(new Color(half, half, half, 1f));
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, null, null, null, LightScale);
        }

        public static void EndLights(SpriteBatch sb)
        {
            sb.End();
        }

        //Begin Glow : black, then effects added on top (premultiplied colours add up like light)
        public static void BeginGlow(GraphicsDevice device, SpriteBatch sb, Matrix camera)
        {
            BeginGlowTarget(device);
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, null, null, null, camera * Sharp.Transform);
        }

        //Begin Glow Target : only the target, for a page that draws its glow in several batches
        public static void BeginGlowTarget(GraphicsDevice device)
        {
            device.SetRenderTarget(Glow);
            device.Clear(Color.Black);
        }

        public static void EndGlow(SpriteBatch sb)
        {
            sb.End();
        }

        //Impact Look : the IMPACT FRAME's steps for Post.fx (Core/HitFx.cs works them out)
        public struct ImpactLook
        {
            public float Turn;        // 1 = the picture turned over
            public float Flash;       // the white, 0..1
            public float Blur;        // the smear towards the blow, 0..1
            public Vector2 Center;    // where the blow landed, 0..1 of the screen
        }

        //======================================================================================
        //THE LENS (Beta1 screens) : every page but the battle
        //======================================================================================

        //Page Look : how one page's picture is finished. Every page has its own (GameScreen.Lens) and may change it
        //while it runs. The numbers start from BattleRules, UI Feel.
        public class PageLook
        {
            public float Ambient = BattleRules.UiAmbient;               // the LIGHT MAP everywhere (0.5 = as drawn)
            public Vector2 Spot = new Vector2(W / 2f, H / 2f);          // a soft stage light on what matters
            public float SpotRadius = BattleRules.UiSpotRadius;
            public float SpotPower = BattleRules.UiSpot;
            public float BloomThreshold = BattleRules.UiBloomThreshold;
            public float BloomStrength = BattleRules.UiBloomStrength;
            public float Vignette = BattleRules.UiVignette;
            public float Focus;                                         // the world dark, grey and soft (like the QTE FOCUS)
            public float Engulf;                                        // TACET's dark crept in from the edges (ENGULF)
            public ImpactLook Impact;                                   // the IMPACT FRAME look (a won fight's result opens on it)
            public float Zoom;                                          // the camera punched in this much (0.06 = 6%) ...
            public Vector2 ZoomAt = new Vector2(W / 2f, H / 2f);        // ... around this point
        }

        //Draw Page : the page's picture into FINAL, before the frame. The same layers as the battle :
        //   BACK    page.DrawBack, only when the page has TACET's dark (page.Dark), which bends it
        //   SCENE   the dark, then page.DrawWorld (a page with a world) or the whole page.Draw (every other page)
        //   LIGHT   page.DrawLights (a soft spot unless the page lights itself), then the dark's own light
        //   GLOW    the dark's light, then page.DrawGlow
        public static void DrawPage(GraphicsDevice device, SpriteBatch sb, GameScreen page, float time)
        {
            Ensure(device);
            PageLook look = page.Lens;
            TacetVoid dark = page.Dark;

            if (dark != null)
            {
                BeginBack(device);
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, Sharp.Transform);
                page.DrawBack(sb);
                sb.End();
            }

            BeginScene(device);
            if (dark != null) dark.DrawScene(device, sb, Back);
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, Sharp.Transform);
            if (page.HasWorld) page.DrawWorld(sb);
            else page.Draw(sb);
            sb.End();

            BeginLights(device, sb, look.Ambient);
            page.DrawLights(sb);
            if (dark != null) dark.DrawLights(sb);
            EndLights(sb);

            BeginGlowTarget(device);
            if (dark != null) dark.DrawGlow(device, sb, Back);
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, null, null, null, Sharp.Transform);
            page.DrawGlow(sb);
            sb.End();

            Finish(device, sb, time, look.Focus, look.Engulf, look.Impact, look.BloomThreshold, look.BloomStrength, look.Vignette, 0f);
        }

        //Spot Light : the plain stage light of a page, into the LIGHT MAP (GameScreen.DrawLights calls it).
        //ADVANCED PART (small) : the light map is drawn with BlendState.Additive, which multiplies a colour by its own alpha
        //before adding it. The glow picture is white with alpha (premultiplied), so a light of power p adds about p x p at
        //its middle : 0.3 adds 0.09, 0.6 adds 0.36. The battle's lights were tuned by eye the same way.
        public static void SpotLight(SpriteBatch sb, PageLook look)
        {
            if (look.SpotPower <= 0f) return;
            Gfx.DrawGlow(sb, look.Spot.X, look.Spot.Y, look.SpotRadius, Color.White * look.SpotPower);
        }

        //Show Page : FINAL onto the frame. up 0..1 is how far the page has come up from black (ENTER), and the camera
        //punch (PageLook.Zoom) pushes in round its point.
        public static void ShowPage(SpriteBatch sb, PageLook look, float up)
        {
            if (Final == null) return;
            Color tint = new Color(up, up, up, 1f);
            if (look.Zoom > 0f) ShowFinal(sb, look.ZoomAt, 1f + look.Zoom, tint);
            else ShowFinal(sb, Vector2.Zero, 1f, tint);
        }

        //Show Final : FINAL onto the frame, pushed in by zoom round the point at (game pixels, 1 = no push).
        //SHARP SCREEN : FINAL is as big as the real screen and the frame's batch turns game pixels into real ones, so it
        //is drawn at one Dot per pixel of FINAL : its own pixels land one on one, nothing is stretched.
        public static void ShowFinal(SpriteBatch sb, Vector2 at, float zoom, Color tint)
        {
            ShowFinal(sb, at, zoom, 0f, Vector2.Zero, tint);
        }

        //Show Final Turned : the same, also turned round at by turn (radians) and slid by shift (game pixels).
        //The battle's camera that moves with the baton (Core/StageCamera.cs, Beta1 29 Oct) frames the world through this.
        public static void ShowFinal(SpriteBatch sb, Vector2 at, float zoom, float turn, Vector2 shift, Color tint)
        {
            sb.Draw(Final, at + shift, null, tint, turn, at * Sharp.Scale, zoom * Sharp.Dot, SpriteEffects.None, 0f);
        }

        //Finish : SCENE and LIGHT MAP into FINAL. focus 0..1 is the QTE FOCUS, engulf 0..1 how far TACET's dark
        //has crept in from the screen's edges (ENGULF), impact the IMPACT FRAME.
        public static void Finish(GraphicsDevice device, SpriteBatch sb, float time, float focus, float engulf, ImpactLook impact,
                                  float bloomThreshold, float bloomStrength, float vignette, float grain)
        {
            if (Fx.Ready) FinishWithShaders(device, sb, time, focus, engulf, impact, bloomThreshold, bloomStrength, vignette, grain);
            else FinishPlain(device, sb, focus);
            device.SetRenderTarget(null);

            //Unbind : the light map and the soft copy must not stay plugged in as textures, they are drawn into again next frame
            device.Textures[1] = null;
            device.Textures[2] = null;
            device.Textures[3] = null;
            device.Textures[4] = null;
        }

        //Finish With Shaders : Down, Blur across, Blur down, Blur across again wider, Blur down, Composite
        private static void FinishWithShaders(GraphicsDevice device, SpriteBatch sb, float time, float focus, float engulf, ImpactLook impact,
                                              float bloomThreshold, float bloomStrength, float vignette, float grain)
        {
            Effect post = Fx.Post;

            //Down : SCENE x LIGHT at a quarter of the page.
            //SHARP SCREEN : on a bigger screen SCENE and GLOW are read from their copy the page's size (Lod, the MIPMAP
            //level : log2 of Sharp.Scale, 0.58 at 1.5, 1 at 2), so the glow is made from the same picture as in the window
            //and looks the same, instead of from a few scattered pixels of a much bigger one (which would flicker).
            device.SetRenderTarget(softA);
            post.CurrentTechnique = post.Techniques["Down"];
            post.Parameters["LightTexture"].SetValue(Light);
            post.Parameters["GlowTexture"].SetValue(Glow);
            EffectParameter lod = post.Parameters["Lod"];
            if (lod != null) lod.SetValue(Sharp.Scale > 1f ? (float)System.Math.Log(Sharp.Scale, 2.0) : 0f);
            sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, null, null, post);
            device.SamplerStates[1] = SamplerState.LinearClamp;
            device.SamplerStates[3] = SamplerState.LinearClamp;
            sb.Draw(Scene, new Rectangle(0, 0, W / 4, H / 4), Color.White);
            sb.End();

            //Blur : across then down, twice, the second time with wider steps for a softer glow
            Blur(device, sb, softA, softB, new Vector2(1f / (W / 4), 0f));
            Blur(device, sb, softB, softA, new Vector2(0f, 1f / (H / 4)));
            Blur(device, sb, softA, softB, new Vector2(2f / (W / 4), 0f));
            Blur(device, sb, softB, softA, new Vector2(0f, 2f / (H / 4)));

            //Composite : the finished picture
            device.SetRenderTarget(Final);
            post.CurrentTechnique = post.Techniques["Composite"];
            post.Parameters["LightTexture"].SetValue(Light);
            post.Parameters["SoftTexture"].SetValue(softA);
            post.Parameters["GlowTexture"].SetValue(Glow);
            post.Parameters["BloomThreshold"].SetValue(bloomThreshold);
            post.Parameters["BloomStrength"].SetValue(bloomStrength);
            post.Parameters["Focus"].SetValue(MathHelper.Clamp(focus, 0f, 1f));
            post.Parameters["Vignette"].SetValue(vignette);
            post.Parameters["Grain"].SetValue(grain);
            post.Parameters["Time"].SetValue(time);
            post.Parameters["Engulf"].SetValue(MathHelper.Clamp(engulf, 0f, 1f));
            post.Parameters["Impact"].SetValue(MathHelper.Clamp(impact.Turn, 0f, 1f));
            post.Parameters["Flash"].SetValue(MathHelper.Clamp(impact.Flash, 0f, 1f));
            post.Parameters["Blur"].SetValue(MathHelper.Clamp(impact.Blur, 0f, 1f));
            post.Parameters["Center"].SetValue(impact.Center);
            post.Parameters["NoiseTexture"].SetValue(Fx.Noise);
            sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, null, null, post);
            device.SamplerStates[1] = SamplerState.LinearClamp;
            device.SamplerStates[2] = SamplerState.LinearClamp;
            device.SamplerStates[3] = SamplerState.LinearClamp;
            device.SamplerStates[4] = SamplerState.LinearWrap;
            sb.Draw(Scene, Vector2.Zero, Color.White);
            sb.End();
        }

        private static void Blur(GraphicsDevice device, SpriteBatch sb, RenderTarget2D from, RenderTarget2D to, Vector2 step)
        {
            Effect post = Fx.Post;
            device.SetRenderTarget(to);
            post.CurrentTechnique = post.Techniques["Blur"];
            post.Parameters["Step"].SetValue(step);
            sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, null, null, post);
            sb.Draw(from, Vector2.Zero, Color.White);
            sb.End();
        }

        //Finish Plain : no shaders. SCENE, then the light map laid over it with MULTIPLY 2X, then the FOCUS as a dark veil.
        private static void FinishPlain(GraphicsDevice device, SpriteBatch sb, float focus)
        {
            device.SetRenderTarget(Final);
            device.Clear(Palette.Void);

            sb.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearClamp);
            sb.Draw(Scene, Vector2.Zero, Color.White);
            sb.End();

            sb.Begin(SpriteSortMode.Deferred, Fx.Multiply2X, SamplerState.LinearClamp);
            sb.Draw(Light, new Rectangle(0, 0, Final.Width, Final.Height), Color.White);
            sb.End();

            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp);
            sb.Draw(Glow, Vector2.Zero, Color.White);
            sb.End();

            if (focus > 0f)
            {
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, Sharp.Transform);
                Gfx.Rect(sb, 0, 0, W, H, Palette.Void * (0.62f * MathHelper.Clamp(focus, 0f, 1f)));
                sb.End();
            }
        }
    }
}
