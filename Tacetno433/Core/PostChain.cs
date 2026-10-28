using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //PostChain : the battle picture is drawn in layers, then finished in passes (Beta1).
    //
    //   SCENE      1280 x 720  the world : the era's backdrop, the stage, TACET's dark, the enemies, the musician
    //   LIGHT MAP   640 x 360  the lights : a dim grey everywhere, brighter where a light shines, kept at
    //                          half strength so a flash can go above normal (LIGHT x 2, see Post.fx)
    //   GLOW       1280 x 720  effects that give off their own light (sparks, slashes, flares) : added after the
    //                          lights, so the dark never dims them, and they feed the BLOOM
    //   SOFT        320 x 180  the lit world shrunk and blurred : the glow (BLOOM) and the QTE FOCUS come from it
    //   FINAL      1280 x 720  SCENE x LIGHT + GLOW + BLOOM, then FOCUS, VIGNETTE and grain
    //
    //A page draws SCENE, LIGHT MAP and GLOW in its PreDraw, calls Finish, then draws FINAL in its Draw with the
    //words and buttons on top. Without shaders (Fx.Ready false) the same picture is put together with
    //blend states only : no glow and no blur, the rest looks the same.
    //
    //Optimize : the big pictures are made once and kept. Blur and glow work at a quarter of the screen,
    //which is sixteen times fewer pixels than the full size.
    public static class PostChain
    {
        public const int W = TacetGame.ScreenW;
        public const int H = TacetGame.ScreenH;

        public static RenderTarget2D Scene;
        public static RenderTarget2D Light;
        public static RenderTarget2D Glow;
        public static RenderTarget2D Final;
        private static RenderTarget2D softA;
        private static RenderTarget2D softB;

        //Light Scale : the light map is half the size of the screen, so lights are drawn through this
        public static readonly Matrix LightScale = Matrix.CreateScale(0.5f);

        //Ensure : make the pictures the first time they are needed (or after the card lost them)
        public static void Ensure(GraphicsDevice device)
        {
            if (Scene != null && !Scene.IsDisposed && Scene.GraphicsDevice == device) return;

            Scene = new RenderTarget2D(device, W, H);
            Light = new RenderTarget2D(device, W / 2, H / 2);
            Glow = new RenderTarget2D(device, W, H);
            Final = new RenderTarget2D(device, W, H);
            softA = new RenderTarget2D(device, W / 4, H / 4);
            softB = new RenderTarget2D(device, W / 4, H / 4);
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
            device.SetRenderTarget(Glow);
            device.Clear(Color.Black);
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, null, null, null, camera);
        }

        public static void EndGlow(SpriteBatch sb)
        {
            sb.End();
        }

        //Finish : SCENE and LIGHT MAP into FINAL. focus 0..1 is the QTE FOCUS.
        public static void Finish(GraphicsDevice device, SpriteBatch sb, float time, float focus,
                                  float bloomThreshold, float bloomStrength, float vignette, float grain)
        {
            if (Fx.Ready) FinishWithShaders(device, sb, time, focus, bloomThreshold, bloomStrength, vignette, grain);
            else FinishPlain(device, sb, focus);
            device.SetRenderTarget(null);

            //Unbind : the light map and the soft copy must not stay plugged in as textures, they are drawn into again next frame
            device.Textures[1] = null;
            device.Textures[2] = null;
            device.Textures[3] = null;
        }

        //Finish With Shaders : Down, Blur across, Blur down, Blur across again wider, Blur down, Composite
        private static void FinishWithShaders(GraphicsDevice device, SpriteBatch sb, float time, float focus,
                                              float bloomThreshold, float bloomStrength, float vignette, float grain)
        {
            Effect post = Fx.Post;

            //Down : SCENE x LIGHT at a quarter size
            device.SetRenderTarget(softA);
            post.CurrentTechnique = post.Techniques["Down"];
            post.Parameters["LightTexture"].SetValue(Light);
            post.Parameters["GlowTexture"].SetValue(Glow);
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
            sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, null, null, post);
            device.SamplerStates[1] = SamplerState.LinearClamp;
            device.SamplerStates[2] = SamplerState.LinearClamp;
            device.SamplerStates[3] = SamplerState.LinearClamp;
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
            sb.Draw(Light, new Rectangle(0, 0, W, H), Color.White);
            sb.End();

            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp);
            sb.Draw(Glow, Vector2.Zero, Color.White);
            sb.End();

            if (focus > 0f)
            {
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);
                Gfx.Rect(sb, 0, 0, W, H, Palette.Void * (0.62f * MathHelper.Clamp(focus, 0f, 1f)));
                sb.End();
            }
        }
    }
}
