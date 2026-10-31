using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //RunHud : the bar across the top of every page during a run, and the floor track.
    //
    //Left   band name and conductor
    //Centre the page title
    //Right  life, shards and gift count (the seats went with the band of three, 29 Oct. GIFTS 31 Oct : a motif is a GIFT on screen)
    //Under the bar on the right, one badge per gift, framed in the colour of the legend it is for. Point at a badge to read it.
    //
    //Pages call DrawTop early and DrawTips last, so the tooltip sits above everything.
    public static class RunHud
    {
        public const int TopH = 64;
        private const float BadgeSize = 16f;     // round 15 : 11 was too small to read the initials
        private const int BadgeGap = 40;

        private static int hoverMotif = -1;

        //Arriving (Beta1 round 1) : this many of the newest motif badges are not drawn yet, they are still flying up from
        //the shop shelf (ShopScreen). The page that sets it puts it back to 0 when it leaves.
        public static int Arriving;

        //Chip Places : where the top bar shows LIFE, SHARDS and the band's name, for something flying into them
        public static readonly Vector2 LifeAt = new Vector2(925f, 39f);
        public static readonly Vector2 ShardsAt = new Vector2(1060f, 40f);
        public static readonly Vector2 BandAt = new Vector2(110f, 22f);

        //Top Bar
        public static void DrawTop(SpriteBatch sb, RunState run, string title)
        {
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TopH, Palette.Void * 0.94f);
            Gfx.Rect(sb, 0, TopH - 1, TacetGame.ScreenW, 1, Palette.LineGrey * 0.5f);
            Gfx.Diamond(sb, TacetGame.ScreenW / 2f, TopH - 1, 3, Palette.PaperDim);

            //Band
            Gfx.Text(sb, Ui.BigFont, run.BandName.Length > 0 ? run.BandName : "UNNAMED ENSEMBLE", 32, 8, Palette.Paper, TextSize.Small);
            Gfx.TextSpaced(sb, Ui.Font, run.Conductor.Name, 34, 40, Palette.LineGrey, TextSize.Tiny, 2.5f);

            //Mode : NORMAL or MAESTRO after the conductor's name (MAESTRO MODE)
            float modeX = 34 + Gfx.SpacedWidth(Ui.Font, run.Conductor.Name, TextSize.Tiny, 2.5f) + 16;
            Gfx.TextSpaced(sb, Ui.Font, run.Maestro ? "MAESTRO" : "NORMAL", modeX, 40, run.Maestro ? Palette.Highlight : Palette.PaperDim, TextSize.Tiny, 2.5f);

            //Title
            if (title.Length > 0)
                Gfx.TextSpacedCentered(sb, Ui.Font, title, TacetGame.ScreenW / 2f, 24, Palette.PaperDim, TextSize.Label, 5f);

            //Stamina Chip : label, number and a bar
            int sx = 850;
            Gfx.TextSpaced(sb, Ui.Font, BattleRules.LifeWord, sx, 12, Palette.LineGrey, TextSize.Tiny, 2f);
            Gfx.TextRight(sb, Ui.Font, run.StaminaValue, sx + 150, 8, Palette.Paper, TextSize.Body);
            Ui.CapsuleBar(sb, new Rectangle(sx, 34, 150, 10), run.Stamina / (float)run.MaxStamina, Palette.Paper, 1f);

            //Shards Chip : wider than the others since the shop became the band's place to grow (29 Oct)
            int shardX = 1050;
            Gfx.TextSpaced(sb, Ui.Font, "SHARDS", shardX, 12, Palette.LineGrey, TextSize.Tiny, 2f);
            Gfx.Diamond(sb, shardX + 5, 40, 5, Palette.Paper);
            Gfx.Text(sb, Ui.Font, NumberText.Get(run.Shards), shardX + 16, 28, Palette.Highlight, TextSize.Body);

            //Motif Chip
            int motifX = 1210;
            Gfx.TextSpaced(sb, Ui.Font, "GIFTS", motifX, 12, Palette.LineGrey, TextSize.Tiny, 2f);
            Gfx.Text(sb, Ui.Font, NumberText.Get(run.Motifs.Count), motifX, 28, Palette.Paper, TextSize.Body);

            DrawBadges(sb, run);
        }

        //Badge Centre : motif i, counted from the right edge
        public static Vector2 BadgeCentre(int i)
        {
            return new Vector2(TacetGame.ScreenW - 34 - i * BadgeGap, TopH + 24);
        }

        //Badges : one diamond per motif, the newest on the left
        private static void DrawBadges(SpriteBatch sb, RunState run)
        {
            hoverMotif = -1;
            for (int i = 0; i < run.Motifs.Count - Arriving; i++)
            {
                Vector2 c = BadgeCentre(i);
                Rectangle hit = new Rectangle((int)(c.X - BadgeSize), (int)(c.Y - BadgeSize), (int)(BadgeSize * 2), (int)(BadgeSize * 2));
                bool over = Input.MouseOver(hit);
                if (over) hoverMotif = i;

                if (over) Gfx.DrawGlow(sb, c.X, c.Y, 30f, Palette.Paper * 0.3f);
                MotifArt.Badge(sb, run.Motifs[i], c.X, c.Y, BadgeSize, over ? 1f : 0.8f);
            }
        }

        //Tips : the motif tooltip, drawn last by the page
        public static void DrawTips(SpriteBatch sb, RunState run)
        {
            if (hoverMotif < 0 || hoverMotif >= run.Motifs.Count) return;
            Motif m = run.Motifs[hoverMotif];
            Ui.Tooltip(sb, m.Name, m.TipWrapped, Input.MousePos.X, Input.MousePos.Y);
        }

        //Track : the stages of this floor as diamonds on a line. Behind you, you, ahead,
        //and the boss at the far end drawn larger with a ring.
        public static void DrawTrack(SpriteBatch sb, RunState run, float cx, float y, float width)
        {
            int count = run.StagesThisFloor;
            float step = width / (count - 1);
            float left = cx - width / 2f;

            Gfx.Rect(sb, left, y, width, 1, Palette.LineGrey * 0.7f);
            float doneWidth = step * (run.Stage - 1);
            if (doneWidth > 0f) Gfx.Rect(sb, left, y - 1, doneWidth, 3, Palette.PaperDim);

            for (int s = 1; s <= count; s++)
            {
                float x = left + (s - 1) * step;
                bool boss = s == count;

                if (s < run.Stage)
                {
                    Gfx.Diamond(sb, x, y, 4, Palette.PaperDim);
                }
                else if (s == run.Stage)
                {
                    Gfx.DrawGlow(sb, x, y, 20f, Palette.Accent * 0.4f);
                    Gfx.Diamond(sb, x, y, 8, Palette.Void);
                    Gfx.DiamondOutline(sb, x, y, 8, Palette.Accent, 2f);
                    Gfx.Diamond(sb, x, y, 3, Palette.Accent);
                }
                else
                {
                    Gfx.Diamond(sb, x, y, boss ? 7 : 4, Palette.Void);
                    Gfx.DiamondOutline(sb, x, y, boss ? 7 : 4, Palette.LineGrey, 1f);
                }

                if (boss)
                {
                    Gfx.CircleOutline(sb, x, y, 13, Palette.PaperDim, 1f);
                    Gfx.TextSpacedCentered(sb, Ui.Font, "BOSS", x, y + 16, Palette.PaperDim, TextSize.Tiny, 2f);
                }
            }
        }
    }
}
