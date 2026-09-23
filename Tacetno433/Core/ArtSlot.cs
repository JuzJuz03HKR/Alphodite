using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //ArtSlot : an EMPTY box that marks where a picture goes.
    //
    //The artwork is made by the team, so the code draws nothing inside it on purpose: a faint
    //fill, a thin frame and four corner marks, nothing else. When the picture exists, the page
    //draws it into the very same rectangle instead (see ArtBank.DrawOrSlot).
    //
    //ink is the line colour : Palette.Paper on the dark pages, Palette.Ink on the bright stage.
    public static class ArtSlot
    {
        public static void Draw(SpriteBatch sb, Rectangle box, Color ink, float alpha)
        {
            if (box.Width <= 0 || box.Height <= 0) return;

            int corner = Math.Min(16, Math.Min(box.Width, box.Height) / 4);
            Gfx.Rect(sb, box, ink * (0.05f * alpha));
            Gfx.RectOutline(sb, box, ink * (0.22f * alpha), 1);
            Ornament.CornerBrackets(sb, box, corner, ink * (0.6f * alpha), 1);
        }
    }
}
