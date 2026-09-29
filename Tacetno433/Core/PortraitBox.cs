using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //PortraitBox : draws a conductor in the three shapes the game needs.
    //
    //The picture itself comes from ArtBank (Art/Conductors/portrait_<name> and face_<name>).
    //Until it exists an empty ArtSlot box marks the place. Frames, name plates, dimming and
    //the zoom animation all work off the box and the brightness, so nothing else changes later.
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

            //Canvas : the painting, or the empty slot for it
            Rectangle canvas = new Rectangle(frame.X + FrameThickness, frame.Y + FrameThickness,
                                             frame.Width - FrameThickness * 2, frame.Height - FrameThickness * 2);
            Gfx.Rect(sb, canvas, Palette.CanvasDark * brightness);
            ArtBank.DrawOrSlot(sb, ArtBank.GalleryOf(c), canvas, Palette.Paper, brightness);

            //Corner Studs and Crest
            Gfx.Diamond(sb, frame.X, frame.Y, 5, line);
            Gfx.Diamond(sb, frame.Right, frame.Y, 5, line);
            Gfx.Diamond(sb, frame.X, frame.Bottom, 5, line);
            Gfx.Diamond(sb, frame.Right, frame.Bottom, 5, line);
            Gfx.Diamond(sb, frame.Center.X, frame.Y, 8, Palette.StageDeep * brightness);
            Gfx.DiamondOutline(sb, frame.Center.X, frame.Y, 8, line, 1f);

            //Index : the number written in the corner of the canvas
            Gfx.Text(sb, Ui.Font, c.IndexLabel, canvas.X + 10, canvas.Y + 8, Palette.PaperDim * brightness, TextSize.Label);
            if (!c.Unlocked)
                Gfx.TextSpacedRight(sb, Ui.Font, "LOCKED", canvas.Right - 10, canvas.Y + 10, Palette.PaperDim * brightness, TextSize.Tiny, 3f);

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
        //Used in the middle of the profile page, and for the bow at the curtain call.
        public static void DrawFigure(SpriteBatch sb, Rectangle box, Conductor c, float brightness)
        {
            ArtBank.DrawOrSlot(sb, ArtBank.PortraitOf(c), box, Palette.Paper, brightness);
        }

        //Portrait Face : square head shot, used on the profile page and in the duel
        public static void DrawFaceIcon(SpriteBatch sb, Rectangle box, Conductor c, float brightness)
        {
            Gfx.Rect(sb, box, Palette.CanvasDark * brightness);
            ArtBank.DrawOrSlot(sb, ArtBank.FaceOf(c), box, Palette.Paper, brightness);
        }
    }
}
