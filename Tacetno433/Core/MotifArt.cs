using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //MotifArt : how a motif is drawn, in one file.
    //
    //PLACEHOLDER. Each motif is its two initials inside a diamond (round 15, it was a notation
    //mark). When motif icons are painted, Badge becomes one sb.Draw of the icon into the same square.
    //   Badge     -> the small icon (top bar, shop shelf)
    //   CardFace  -> the tall card on the reward page, drawn into a picture that turns in 3D (Core/Card3D.cs)
    //   CardBack  -> the back every motif card shares
    //   ReforgeFace -> REFORGE, the boss's great upgrade that is not a motif (29 Oct), on the same kind of card
    //   PathFace  -> a step of a musician's PATH (30 Oct), on the same kind of card
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

        //Card Back (Beta1 screens) : the back every motif card shares, seen while a card flips (Core/Card3D.cs). A ring
        //with ticks like the QTE ring and TACET's hole, nothing that says which motif it is.
        public static void CardBack(SpriteBatch sb, Rectangle r, float alpha, float time)
        {
            Gfx.Rect(sb, r, Palette.Panel * alpha);
            Gfx.DrawGlowBox(sb, new Rectangle(r.X - 30, r.Y + r.Height / 4, r.Width + 60, r.Height / 2), Palette.Paper * (0.05f * alpha));
            Ornament.DoubleFrame(sb, r, Palette.LineGrey * alpha);
            Rectangle inner = r;
            inner.Inflate(-18, -18);
            Ornament.CornerBrackets(sb, inner, 14, Palette.PaperDim * alpha, 1);

            float cx = r.Center.X;
            float cy = r.Center.Y;
            Ornament.TickRing(sb, cx, cy, 54f, 40, time * 0.2f, 1f, Palette.PaperDim * alpha);
            Gfx.CircleOutline(sb, cx, cy, 30f, Palette.LineGrey * alpha, 1.5f);
            Gfx.Diamond(sb, cx, cy, 6f, Palette.Paper * alpha);
            Gfx.TextSpacedCentered(sb, Ui.Font, "MOTIF", cx, r.Bottom - 46, Palette.LineGrey * alpha, TextSize.Tiny, 4f);
        }

        //Card Face : the tall reward card, without a shadow (the page puts it on the floor). hover lights the frame.
        public static void CardFace(SpriteBatch sb, Rectangle r, Motif m, bool hover, float alpha, float time)
        {
            //Card Body
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

        //Path Face : a step of a musician's PATH (Beta1 30 Oct, Data/Path.cs), drawn like a motif card. At the top the musician's
        //name in their own colour and the step's pips (I II III), in the middle the step's numeral in a diamond of their colour
        //with the path's name under it, then the step's name and what it does. level is the step's place on its path (0 for I).
        //Everything is prepared text (the page keeps the step it offers, so the face stays the same once it is taken).
        public static void PathFace(SpriteBatch sb, Rectangle r, Musician m, string pathName, PathStep step, int level, bool hover, float alpha, float time)
        {
            Color spot = BandColumn.SpotOf(m);
            Gfx.Rect(sb, r, Palette.Panel * alpha);
            Gfx.DrawGlowBox(sb, new Rectangle(r.X - 30, r.Y + 20, r.Width + 60, r.Height / 2), spot * ((hover ? 0.16f : 0.07f) * alpha));
            Ornament.DoubleFrame(sb, r, (hover ? Palette.Highlight : Palette.LineGrey) * alpha);
            Ui.Pips(sb, r.Center.X - 16, r.Y + 26, level + 1, 3, 5, 16, alpha);
            Gfx.TextSpacedCentered(sb, Ui.Font, m.Name, r.Center.X, r.Y + 38, spot * alpha, TextSize.Tiny, 3f);

            //The Step : its numeral in a diamond of the musician's colour, the path it belongs to under it
            float by = r.Y + r.Height * 0.33f;
            if (hover) Ornament.Rays(sb, r.Center.X, by, 50, 90, 18, time * 0.3f, spot * (0.22f * alpha));
            Gfx.Diamond(sb, r.Center.X, by, 40f, Palette.Void * alpha);
            Gfx.DiamondOutline(sb, r.Center.X, by, 40f, spot * alpha, 2f);
            Gfx.TextCentered(sb, Ui.BigFont, PathList.Numerals[System.Math.Min(level, PathList.Numerals.Length - 1)], r.Center.X, by - 3f, Palette.Highlight * alpha, TextSize.Subtitle);
            Gfx.TextSpacedCentered(sb, Ui.Font, pathName, r.Center.X, by + 52f, Palette.PaperDim * alpha, TextSize.Tiny, 3f);

            //Name and Text
            float ny = r.Y + r.Height * 0.6f;
            Gfx.TextCentered(sb, Ui.BigFont, step.Name, r.Center.X, ny, Palette.Highlight * alpha, TextSize.Subtitle);
            Ornament.Divider(sb, r.Center.X, ny + 26, r.Width * 0.3f, Palette.LineGrey * alpha);
            Gfx.Text(sb, Ui.StoryFont, step.TextWrapped, r.X + 22, ny + 42, Palette.PaperDim * alpha, TextSize.StorySmall);
        }

        //Reforge Face : REFORGE (THE BAND OF THREE, 29 Oct, BETA1_DIRECTION 8.4), one of the boss's three great upgrades. Every
        //weapon of the band goes up a tier. Drawn like a treasured motif card, three full pips, and in the middle the band's
        //weapons as three diamonds with their tiers, the next tier lit. text is the wrapped line, made when the page loads.
        public static void ReforgeFace(SpriteBatch sb, Rectangle r, RunState run, string text, bool hover, float alpha, float time)
        {
            Gfx.Rect(sb, r, Palette.Panel * alpha);
            Gfx.DrawGlowBox(sb, new Rectangle(r.X - 30, r.Y + 20, r.Width + 60, r.Height / 2), Palette.Paper * ((hover ? 0.12f : 0.05f) * alpha));
            Ornament.DoubleFrame(sb, r, (hover ? Palette.Highlight : Palette.LineGrey) * alpha);
            Ui.Pips(sb, r.Center.X - 16, r.Y + 26, 3, 3, 5, 16, alpha);
            Gfx.TextSpacedCentered(sb, Ui.Font, "FOR THE BAND", r.Center.X, r.Y + 38, Palette.PaperDim * alpha, TextSize.Tiny, 3f);

            //The Band's Weapons : one diamond each in their own colour, the tier they reach next filled
            float by = r.Y + r.Height * 0.36f;
            if (hover) Ornament.Rays(sb, r.Center.X, by, 50, 90, 18, time * 0.3f, Palette.Paper * (0.18f * alpha));
            int count = run.Roster.Count;
            for (int i = 0; i < count; i++)
            {
                Musician m = run.Roster[i];
                float x = r.Center.X + (i - (count - 1) / 2f) * 58f;
                Color spot = BandColumn.SpotOf(m);
                Gfx.Diamond(sb, x, by, 22f, Palette.Void * alpha);
                Gfx.DiamondOutline(sb, x, by, 22f, spot * alpha, 2f);
                int next = System.Math.Min(BattleRules.RehearseMax, m.Rehearsed + 1);
                Gfx.TextCentered(sb, Ui.BigFont, MusicianList.TierNames[next], x, by - 2f, Palette.Highlight * alpha, TextSize.Small * 0.8f);
            }

            //Name and Text
            float ny = r.Y + r.Height * 0.58f;
            Gfx.TextCentered(sb, Ui.BigFont, "REFORGE", r.Center.X, ny, Palette.Highlight * alpha, TextSize.Subtitle);
            Ornament.Divider(sb, r.Center.X, ny + 26, r.Width * 0.3f, Palette.LineGrey * alpha);
            Gfx.Text(sb, Ui.StoryFont, text, r.X + 22, ny + 42, Palette.PaperDim * alpha, TextSize.StorySmall);
        }
    }
}
