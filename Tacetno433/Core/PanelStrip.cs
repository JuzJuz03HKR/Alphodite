using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //PanelStrip : a row of tall art panels that fills the whole screen edge to edge,
    //like one long painting cut into strips. The panel being chosen widens and steps
    //forward, the others make room for it (the accordion effect common in gacha menus).
    //
    //Drawing a panel is two calls, so the page can paint its art in between:
    //   Rectangle box = strip.DrawBase(sb, i, chosen);    the empty panel
    //   ... the page draws its art into box ...
    //   strip.DrawCaption(sb, i, ...);                     number, name and caption on top
    //
    //TO PUT THE REAL ART IN : in the page, draw the texture into box between the two calls
    //    sb.Draw(panelTexture, box, Color.White);
    //The texture is stretched to the panel, so paint panel art tall (around 480 x 720).
    public class PanelStrip
    {
        //Caption Limits : captions are wrapped to this width and never run past this many lines
        public const int CaptionWrapWidth = 230;
        public const int CaptionLines = 3;

        //Strip Layout
        private const int Seam = 6;                   // dark gap between two panels
        private const float ChosenWeight = 1.8f;      // how much wider the chosen panel gets
        private const int CaptionH = 150;

        //Ragged Edges : each panel starts and stops at a different height, like the reference.
        //The chosen panel pulls most of the way back to full height.
        private int[] topOffset = { 0, 58, 22, 84 };
        private int[] bottomOffset = { -64, 0, -30, -10 };
        private int[] captionLift = { 0, 90, 10, 60 };

        //Strip Area : the band of the screen the panels live in. Pages leave room above and
        //below for their own header and footer.
        public int AreaTop = 0;
        public int AreaBottom = TacetGame.ScreenH;

        private float[] weights = new float[0];
        private int count;

        //Strip Reset : call when a page opens with a new set of panels
        public void Reset(int panelCount, int chosen)
        {
            count = panelCount;
            weights = new float[count];
            for (int i = 0; i < count; i++)
                weights[i] = (i == chosen) ? ChosenWeight : 1f;
        }

        //Strip Update : ease every panel toward its target width
        public void Update(float dt, int chosen)
        {
            for (int i = 0; i < count; i++)
            {
                float target = (i == chosen) ? ChosenWeight : 1f;
                weights[i] += (target - weights[i]) * dt * 10f;
            }
        }

        //Emphasis : 0 for a resting panel, 1 for the fully chosen one
        public float Emphasis(int i)
        {
            return (weights[i] - 1f) / (ChosenWeight - 1f);
        }

        //Panel Rect : where panel i sits right now. Also used for mouse hit tests.
        public Rectangle PanelRect(int i)
        {
            float total = 0f;
            for (int k = 0; k < count; k++) total += weights[k];

            float usable = TacetGame.ScreenW - Seam * (count - 1);
            float x = 0f;
            for (int k = 0; k < i; k++)
                x += usable * weights[k] / total + Seam;

            int width = (int)(usable * weights[i] / total);

            float pull = 1f - Emphasis(i) * 0.75f;
            int top = AreaTop + (int)(topOffset[i % topOffset.Length] * pull);
            int bottom = AreaBottom + (int)(bottomOffset[i % bottomOffset.Length] * pull);

            return new Rectangle((int)x, top, width, bottom - top);
        }

        //Panel Base : the empty picture. Returns the box for the page to paint into.
        public Rectangle DrawBase(SpriteBatch sb, int i)
        {
            Rectangle box = PanelRect(i);
            float e = Emphasis(i);

            //ARTWORK AREA : an empty slot until the page draws the painting over it
            Gfx.Rect(sb, box, Palette.Stage);
            Rectangle slot = box;
            slot.Inflate(-8, -8);
            ArtSlot.Draw(sb, slot, Palette.Paper, 0.6f + 0.4f * e);
            return box;
        }

        //Panel Caption : everything written on top of the picture
        //number   "01" at the top left
        //tag      a small word like DUEL, empty for none
        //danger   0 to 3 filled diamonds, less than 0 hides them
        public void DrawCaption(SpriteBatch sb, int i, string number, string title, string tag, int danger,
                                string captionWrapped, bool chosen)
        {
            Rectangle box = PanelRect(i);
            float e = Emphasis(i);
            float bright = 0.5f + 0.5f * e;

            //Rest Dim : panels that are not chosen sink back into the dark
            Gfx.Rect(sb, box, Color.Black * (0.55f * (1f - e)));

            //Number : big serif index with a short rule, like a chapter mark
            Gfx.Text(sb, Ui.BigFont, number, box.X + 22, box.Y + 26, Palette.Paper * bright, TextSize.Subtitle);
            Gfx.Rect(sb, box.X + 24, box.Y + 62, 30 + 30 * e, 1, Palette.Paper * bright);

            //Vertical Name : printed down the right edge of every panel, the poster look
            Gfx.TextVertical(sb, Ui.Font, title, box.Right - 12, box.Y + 30, Palette.PaperDim * (0.35f + 0.3f * e), TextSize.Label);

            //Caption Plate : a dark fade rising from the bottom, text on top of it
            int plateY = box.Bottom - CaptionH - (int)(captionLift[i % captionLift.Length] * (1f - e));
            Rectangle plate = new Rectangle(box.X, plateY, box.Width, box.Bottom - plateY);
            Ornament.FadeUp(sb, new Rectangle(box.X, plateY - 60, box.Width, 60), 0.85f);
            Gfx.Rect(sb, plate, Color.Black * 0.85f);

            int tx = plate.X + 24;
            int ty = plate.Y + 4;

            //Tag and Danger
            if (tag.Length > 0) Ui.Tag(sb, tag, tx, ty, chosen, bright);
            if (danger >= 0)
                Ui.Pips(sb, tx + (tag.Length > 0 ? Ui.TagWidth(tag) + 16 : 6), ty + 9, danger, 3, 4, 12, bright);

            //Title
            if (chosen)
                Gfx.Text(sb, Ui.BigFont, title, tx, ty + 24, Palette.Highlight, TextSize.Title);
            else
                Gfx.Text(sb, Ui.BigFont, title, tx, ty + 28, Palette.Paper * bright, TextSize.Subtitle);

            Gfx.Text(sb, Ui.StoryFont, captionWrapped, tx, ty + (chosen ? 70 : 62), Palette.PaperDim * bright, TextSize.StorySmall);

            //Chosen Marks : a bar on the top edge, crop corners and a glow line at the bottom
            if (chosen)
            {
                Gfx.Rect(sb, box.X, box.Y, box.Width, 3, Palette.Accent);
                Rectangle inner = box;
                inner.Inflate(-10, -10);
                Ornament.CornerBrackets(sb, inner, 18, Palette.Paper * (0.8f * e), 2);
                Gfx.DrawGlowBox(sb, new Rectangle(box.X, box.Bottom - 10, box.Width, 20), Palette.Accent * (0.4f * e));
            }
        }
    }
}
