using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //Card3D (Beta1 screens, round 1, 29 Oct) : a flat card shown turning in 3D, like the cards of Balatro. The reward page
    //flips its motif cards up with it and tilts them toward the mouse.
    //
    //A page uses it in two steps :
    //   PreDraw  draw the card's face into its own picture (Picture, BeginPicture, then the usual Gfx calls, sb.End)
    //   Draw     show that picture turned in 3D (Draw), between sb.End() and Sharp.Begin(sb)
    //
    //ADVANCED PART : a card is a picture on a flat square (two triangles) in a real 3D space.
    //   World       the card's own turn : moved so its middle is at (0, 0), sized, turned round the X axis (top and
    //               bottom lean back) and the Y axis (left and right lean back), then moved to its place
    //   View        an eye EyeDistance game pixels in front of the middle of the screen, looking straight at it
    //   Projection  perspective : far things look smaller. The view's width is chosen so that anything lying flat on
    //               the screen (z = 0) lands exactly on its game pixel, so a card that is not turned looks the same as
    //               one drawn with the SpriteBatch
    //The picture is drawn at the screen's real size (SHARP SCREEN), so the card's words stay sharp while it turns.
    //When a card is turned more than a quarter round, its back faces the eye : the page passes the back's picture
    //and mirror, so the back reads the right way round.
    public static class Card3D
    {
        private const float EyeDistance = 1500f;   // nearer = stronger perspective

        private static BasicEffect effect;
        private static readonly VertexPositionColorTexture[] corners = new VertexPositionColorTexture[4];

        //Add Light : light added onto a picture as it is (BlendState.Additive would multiply it by its own alpha a second
        //time, see PostChain.SpotLight), leaving how see-through the picture is alone
        public static readonly BlendState AddLight = new BlendState
        {
            ColorSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.One,
            AlphaSourceBlend = Blend.Zero,
            AlphaDestinationBlend = Blend.One
        };

        //Picture : rt, or a new one when it is missing or the wrong size, w x h game pixels at the screen's real size
        public static RenderTarget2D Picture(GraphicsDevice device, RenderTarget2D rt, int w, int h)
        {
            int realW = Math.Max(1, (int)Math.Ceiling(w * Sharp.Scale));
            int realH = Math.Max(1, (int)Math.Ceiling(h * Sharp.Scale));
            if (rt != null && rt.Width == realW && rt.Height == realH && !rt.IsDisposed) return rt;
            if (rt != null) rt.Dispose();
            return new RenderTarget2D(device, realW, realH, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        }

        //Begin Picture : draw into rt, cleared, its top left corner at (0, 0) in game pixels. End it with sb.End().
        public static void BeginPicture(GraphicsDevice device, SpriteBatch sb, RenderTarget2D rt)
        {
            device.SetRenderTarget(rt);
            device.Clear(Color.Transparent);
            Batch(sb, BlendState.AlphaBlend, null);
        }

        //Batch : one more batch into the picture BeginPicture set, drawn another way (added light, or kept inside a box by
        //a scissor, which is in the picture's real pixels)
        public static void Batch(SpriteBatch sb, BlendState blend, RasterizerState raster)
        {
            sb.Begin(SpriteSortMode.Deferred, blend, SamplerState.LinearClamp, null, raster, null, Matrix.CreateScale(Sharp.Scale, Sharp.Scale, 1f));
        }

        //Draw : picture as a card w x h game pixels, its middle at centre, grown by scale, turned by turnX and turnY
        //(radians, + leans the bottom or the right side away) and see-through by alpha. mirror flips it left to right.
        public static void Draw(GraphicsDevice device, Texture2D picture, Vector2 centre, float w, float h, float turnX, float turnY,
                                float scale, float alpha, bool mirror)
        {
            if (picture == null || alpha <= 0f) return;
            if (effect == null) effect = new BasicEffect(device) { TextureEnabled = true, VertexColorEnabled = true, LightingEnabled = false };

            float sw = TacetGame.ScreenW;
            float sh = TacetGame.ScreenH;
            effect.World = Matrix.CreateTranslation(-w / 2f, -h / 2f, 0f)
                         * Matrix.CreateScale(scale)
                         * Matrix.CreateRotationX(turnX)
                         * Matrix.CreateRotationY(-turnY)
                         * Matrix.CreateTranslation(centre.X, centre.Y, 0f);
            effect.View = Matrix.CreateLookAt(new Vector3(sw / 2f, sh / 2f, -EyeDistance), new Vector3(sw / 2f, sh / 2f, 0f), new Vector3(0f, -1f, 0f));
            effect.Projection = Matrix.CreatePerspectiveFieldOfView(2f * (float)Math.Atan(sh / 2f / EyeDistance), sw / sh, 10f, EyeDistance * 4f);
            effect.Texture = picture;

            //Shade : a card leaning away from the stage light gets a little darker
            float lit = 1f - 0.22f * Math.Min(1f, Math.Abs((float)Math.Sin(turnY)) + Math.Abs((float)Math.Sin(turnX)));
            Color tint = new Color(lit * alpha, lit * alpha, lit * alpha, alpha);
            float u0 = mirror ? 1f : 0f;
            float u1 = mirror ? 0f : 1f;
            corners[0] = new VertexPositionColorTexture(new Vector3(0f, 0f, 0f), tint, new Vector2(u0, 0f));
            corners[1] = new VertexPositionColorTexture(new Vector3(w, 0f, 0f), tint, new Vector2(u1, 0f));
            corners[2] = new VertexPositionColorTexture(new Vector3(0f, h, 0f), tint, new Vector2(u0, 1f));
            corners[3] = new VertexPositionColorTexture(new Vector3(w, h, 0f), tint, new Vector2(u1, 1f));

            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            device.SamplerStates[0] = SamplerState.LinearClamp;
            effect.CurrentTechnique.Passes[0].Apply();
            device.DrawUserPrimitives(PrimitiveType.TriangleStrip, corners, 0, 2);
        }
    }
}
