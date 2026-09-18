using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //PortraitBox : draws a conductor in the three shapes the game needs.
    //
    //ALL THREE ARE PLACEHOLDERS. Each one ends at DrawSilhouette, and that is the only
    //method that has to be replaced when the real art arrives:
    //    sb.Draw(portraitTexture, box, Color.White * brightness);
    //Frames, name plates, dimming and the zoom animation all work off box and brightness,
    //so none of them need touching later.
    public static class PortraitBox
    {
        public const int FrameThickness = 14;

        //Portrait Framed : the hanging painting used in the gallery.
        //A thin double frame with studded corners, closer to a museum print than a gold frame.
        public static void Draw(SpriteBatch sb, Rectangle frame, Conductor c, float brightness, bool showPlate)
        {
            Color line = Palette.Paper * (0.75f * brightness);

            //Mat : the pale border between frame and canvas
            Gfx.Rect(sb, frame, Palette.Stage * brightness);
            Gfx.RectOutline(sb, frame, line, 1);
            Rectangle inner = frame;
            inner.Inflate(-6, -6);
            Gfx.RectOutline(sb, inner, line * 0.5f, 1);

            //Canvas
            Rectangle canvas = new Rectangle(frame.X + FrameThickness, frame.Y + FrameThickness,
                                             frame.Width - FrameThickness * 2, frame.Height - FrameThickness * 2);
            Gfx.Rect(sb, canvas, Palette.CanvasDark * brightness);
            Gfx.DrawGlowBox(sb, new Rectangle(canvas.X - 40, canvas.Y, canvas.Width + 80, canvas.Height * 2 / 3),
                            Palette.Paper * (0.10f * brightness));
            DrawSilhouette(sb, canvas, c, brightness, 0.16f, 0.32f);

            //Corner Studs and Crest
            Gfx.Diamond(sb, frame.X, frame.Y, 5, line);
            Gfx.Diamond(sb, frame.Right, frame.Y, 5, line);
            Gfx.Diamond(sb, frame.X, frame.Bottom, 5, line);
            Gfx.Diamond(sb, frame.Right, frame.Bottom, 5, line);
            Gfx.Diamond(sb, frame.Center.X, frame.Y, 8, Palette.StageDeep * brightness);
            Gfx.DiamondOutline(sb, frame.Center.X, frame.Y, 8, line, 1f);

            //Index : the number written in the corner of the canvas
            Gfx.Text(sb, Ui.Font, c.IndexLabel, canvas.X + 10, canvas.Y + 8, Palette.PaperDim * brightness, TextSize.Label);
            Gfx.TextSpacedRight(sb, Ui.Font, c.Unlocked ? "ART" : "LOCKED", canvas.Right - 10, canvas.Y + 10,
                                Palette.LineGrey * brightness, TextSize.Tiny, 3f);

            //Name Plate : the name in serif over the bottom of the canvas
            if (showPlate)
            {
                Ornament.FadeUp(sb, new Rectangle(canvas.X, canvas.Bottom - 90, canvas.Width, 90), 0.9f);
                Gfx.TextCentered(sb, Ui.BigFont, c.Unlocked ? c.Name : "? ? ?", canvas.Center.X, canvas.Bottom - 44,
                                 Palette.Highlight * brightness, TextSize.Subtitle);
                Gfx.TextSpacedCentered(sb, Ui.Font, c.Role, canvas.Center.X, canvas.Bottom - 22,
                                       Palette.PaperDim * brightness, TextSize.Tiny, 4f);
            }
        }

        //Portrait Figure : the conductor standing in the scene with no frame around them.
        //Used in the middle of the detail page, so they read as part of the shot.
        public static void DrawFigure(SpriteBatch sb, Rectangle box, Conductor c, float brightness)
        {
            DrawSilhouette(sb, box, c, brightness, 0.15f, 0.16f);
        }

        //Portrait Face : square head shot, used on the detail page
        public static void DrawFaceIcon(SpriteBatch sb, Rectangle box, Conductor c, float brightness)
        {
            Gfx.Rect(sb, box, Palette.CanvasDark * brightness);
            DrawSilhouette(sb, box, c, brightness, 0.30f, 0.42f);
            Gfx.RectOutline(sb, box, Palette.LineGrey * brightness, 1);
            Ornament.CornerBrackets(sb, box, 12, Palette.Paper * brightness, 2);
            Gfx.TextSpacedCentered(sb, Ui.Font, "FACE ART", box.Center.X, box.Bottom + 6, Palette.LineGrey * brightness, TextSize.Tiny, 3f);
        }

        //Silhouette : THIS IS THE PLACEHOLDER TO REPLACE WITH REAL ART.
        //headSize = head radius as a fraction of the box width
        //headY    = how far down the box the head sits, 0 is the top and 1 is the bottom
        private static void DrawSilhouette(SpriteBatch sb, Rectangle box, Conductor c, float brightness, float headSize, float headY)
        {
            Color body = c.Unlocked ? c.ThemeColor * (0.5f * brightness) : Color.Black * brightness;
            Color rim = Palette.Highlight * (0.35f * brightness);

            float cx = box.Center.X;
            float headRadius = box.Width * headSize;
            float headCentreY = box.Y + box.Height * headY;

            //Head
            Gfx.Circle(sb, cx, headCentreY, headRadius, body);
            Gfx.Arc(sb, cx, headCentreY, headRadius, -1.3f, 0.7f, rim, 2f);

            //Shoulders : bars that widen going down, drawn two rows at a time to stay cheap
            int top = (int)(headCentreY + headRadius * 0.85f);
            int bottom = box.Bottom;
            if (bottom <= top) return;

            for (int y = top; y < bottom; y += 2)
            {
                float t = (float)(y - top) / (bottom - top);
                float halfWidth = box.Width * (headSize * 1.15f + t * headSize * 1.5f);
                if (halfWidth > box.Width * 0.5f) halfWidth = box.Width * 0.5f;

                Gfx.Rect(sb, cx - halfWidth, y, halfWidth * 2f, 2, body);
                Gfx.Rect(sb, cx + halfWidth - 2, y, 2, 2, rim);
            }
        }
    }
}
