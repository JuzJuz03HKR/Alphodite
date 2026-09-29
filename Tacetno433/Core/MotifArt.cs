using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //MotifArt : how a motif is drawn, in one file.
    //
    //PLACEHOLDER. Each motif is its two initials inside a diamond (round 15, it was a notation
    //mark). When motif icons are painted, Badge becomes one sb.Draw of the icon into the same square.
    //   Badge -> the small icon (top bar, shop shelf)
    //   Card  -> the tall card on the reward page
    public static class MotifArt
    {
        //Badge : a diamond frame with the initials inside. Rarer motifs get more frame lines.
        public static void Badge(SpriteBatch sb, Motif m, float cx, float cy, float size, float alpha)
        {
            Gfx.Diamond(sb, cx, cy, size, Palette.Void * alpha);
            Gfx.DiamondOutline(sb, cx, cy, size, Palette.Paper * alpha, 1.5f);
            if (m.Rarity >= 2) Gfx.DiamondOutline(sb, cx, cy, size - 4, Palette.PaperDim * alpha, 1f);
            if (m.Rarity >= 3) Gfx.DiamondOutline(sb, cx, cy, size + 5, Palette.Accent * (0.7f * alpha), 1f);

            Mark(sb, m, cx, cy, size / 30f, Palette.Highlight * alpha);
        }

        //Mark : the motif's two initials, plain letters (round 15 : the notation marks drawn here,
        //hairpins, breath commas, "arco", were hard to read for anyone who does not read music)
        public static void Mark(SpriteBatch sb, Motif m, float cx, float cy, float s, Color color)
        {
            Gfx.TextCentered(sb, Ui.BigFont, m.Initials, cx, cy - 2 * s, color, TextSize.Small * s * 1.05f);
        }

        //Card : the tall reward card. hover lifts it and lights the frame.
        public static void Card(SpriteBatch sb, Rectangle r, Motif m, bool hover, float alpha, float time)
        {
            if (hover) r.Y -= 8;

            //Card Body
            Gfx.Rect(sb, new Rectangle(r.X + 6, r.Y + 8, r.Width, r.Height), Color.Black * (0.5f * alpha));
            Gfx.Rect(sb, r, Palette.Panel * alpha);
            Gfx.DrawGlowBox(sb, new Rectangle(r.X - 30, r.Y + 20, r.Width + 60, r.Height / 2), Palette.Paper * ((hover ? 0.12f : 0.05f) * alpha));
            Ornament.DoubleFrame(sb, r, (hover ? Palette.Highlight : Palette.LineGrey) * alpha);

            //Rarity : pips at the top and the word under them
            Ui.Pips(sb, r.Center.X - 16, r.Y + 26, m.Rarity, 3, 5, 16, alpha);
            Gfx.TextSpacedCentered(sb, Ui.Font, m.RarityLabel, r.Center.X, r.Y + 38, Palette.PaperDim * alpha, TextSize.Tiny, 3f);

            //Badge : slowly turning rays behind it when hovered
            float by = r.Y + r.Height * 0.36f;
            if (hover) Ornament.Rays(sb, r.Center.X, by, 50, 90, 18, time * 0.3f, Palette.Paper * (0.18f * alpha));
            Badge(sb, m, r.Center.X, by, 44, alpha);

            //Name and Text
            float ny = r.Y + r.Height * 0.58f;
            Gfx.TextCentered(sb, Ui.BigFont, m.Name, r.Center.X, ny, Palette.Highlight * alpha, TextSize.Subtitle);
            Ornament.Divider(sb, r.Center.X, ny + 26, r.Width * 0.3f, Palette.LineGrey * alpha);
            Gfx.Text(sb, Ui.StoryFont, m.TextWrapped, r.X + 22, ny + 42, Palette.PaperDim * alpha, TextSize.StorySmall);
        }
    }
}
