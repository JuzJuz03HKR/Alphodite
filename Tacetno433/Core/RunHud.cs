using System;
using System.Collections.Generic;
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
    //
    //BAND LIFE (Beta1 1 Nov, docs/BETA1_DIRECTION.md 29 item 2) : under LIFE, one bar per musician in their colour, in the
    //band's order, so who is hurt shows on every page of the run. Point at LIFE to read each one's numbers. A page that
    //points at something that changes life (a REST, an event's way) hands DrawTop the lives it would leave, and the part
    //that goes or comes back blinks on the bars, the way the fight shows a move's damage (MARKS AND AIM).
    public static class RunHud
    {
        public const int TopH = 64;
        private const float BadgeSize = 16f;     // round 15 : 11 was too small to read the initials
        private const int BadgeGap = 40;

        private static int hoverMotif = -1;
        private static bool hoverLife;           // BAND LIFE : the pointer is on the LIFE chip
        private static readonly Rectangle lifeChip = new Rectangle(846, 6, 158, 52);
        private const int LifeBarsW = 150;
        private const int LifeBarGap = 4;

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
            DrawTop(sb, run, title, null, 0f);
        }

        //Top Bar With A Preview : BAND LIFE, "after" is each musician's life once the pointed thing is chosen (Roster order,
        //from RunState.LivesNow and its sums), or null for none. time is the page's clock, for the blink.
        public static void DrawTop(SpriteBatch sb, RunState run, string title, int[] after, float time)
        {
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TopH, Palette.Void * 0.94f);
            Gfx.Rect(sb, 0, TopH - 1, TacetGame.ScreenW, 1, Palette.LineGrey * 0.5f);
            Gfx.Diamond(sb, TacetGame.ScreenW / 2f, TopH - 1, 3, Palette.PaperDim);

            //Band
            Gfx.Text(sb, Ui.BigFont, run.BandName.Length > 0 ? run.BandName : "UNNAMED ENSEMBLE", 32, 8, Palette.Paper, TextSize.Small);
            Gfx.TextSpaced(sb, Ui.Font, run.Conductor.Name, 34, 40, Palette.LineGrey, TextSize.Tiny, 2.5f);

            //ENCORE : the run's level after the conductor's name, nothing on a first concert (NORMAL or MAESTRO until 1 Nov)
            if (run.Encore > 0)
            {
                float modeX = 34 + Gfx.SpacedWidth(Ui.Font, run.Conductor.Name, TextSize.Tiny, 2.5f) + 16;
                Gfx.TextSpaced(sb, Ui.Font, Encore.Labels[run.Encore], modeX, 40, Palette.Highlight, TextSize.Tiny, 2.5f);
            }

            //Title
            if (title.Length > 0)
                Gfx.TextSpacedCentered(sb, Ui.Font, title, TacetGame.ScreenW / 2f, 24, Palette.PaperDim, TextSize.Label, 5f);

            //Stamina Chip : label, number and a bar
            int sx = 850;
            Gfx.TextSpaced(sb, Ui.Font, BattleRules.LifeWord, sx, 12, Palette.LineGrey, TextSize.Tiny, 2f);
            Gfx.TextRight(sb, Ui.Font, run.StaminaValue, sx + 150, 8, Palette.Paper, TextSize.Body);
            if (run.Roster.Count == 0) Ui.CapsuleBar(sb, new Rectangle(sx, 34, 150, 10), run.Stamina / (float)run.MaxStamina, Palette.Paper, 1f);
            else DrawLifeBars(sb, run, sx, after, time);
            hoverLife = run.Roster.Count > 0 && Input.MouseOver(lifeChip);

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

        //Life Bars : BAND LIFE, one capsule per musician in the band's order and colour. With a preview, the part that would
        //go blinks red and the part that would come back blinks green (the kinds' colours, Palette.Kind), and the change is
        //written after LIFE.
        private static void DrawLifeBars(SpriteBatch sb, RunState run, int sx, int[] after, float time)
        {
            List<Musician> order = run.Order.Count == run.Roster.Count ? run.Order : run.Roster;
            int n = order.Count;
            int w = (LifeBarsW - LifeBarGap * (n - 1)) / n;
            float pulse = 0.55f + 0.45f * (float)Math.Sin(time * 7f);
            int change = 0;
            for (int k = 0; k < n; k++)
            {
                Musician m = order[k];
                int max = Math.Max(1, run.MaxLifeOf(m));
                int life = run.LifeOf(m);
                Rectangle bar = new Rectangle(sx + k * (w + LifeBarGap), 34, w, 10);
                Ui.CapsuleBar(sb, bar, life / (float)max, BandColumn.SpotOf(m), 1f);

                int at = run.Roster.IndexOf(m);
                if (after == null || at < 0 || at >= after.Length || after[at] == life) continue;
                change += after[at] - life;
                Rectangle inner = bar;
                inner.Inflate(-2, -2);
                float now = life / (float)max;
                float then = after[at] / (float)max;
                float from = Math.Min(now, then);
                Color kind = then < now ? Palette.KindAttack : Palette.KindHeal;
                Gfx.Rect(sb, inner.X + inner.Width * from, inner.Y, Math.Max(1f, inner.Width * Math.Abs(then - now)), inner.Height, kind * (0.45f + 0.55f * pulse));
            }
            if (change != 0)
                Gfx.Text(sb, Ui.Font, NumberText.Signed(change), sx + 52, 10, change < 0 ? Palette.KindAttack : Palette.KindHeal, TextSize.Tiny);
        }

        //Life Tip : BAND LIFE, pointing at LIFE lists each musician with their life, in the band's order and colour
        private const string Slash = "/";

        private static void DrawLifeTip(SpriteBatch sb, RunState run)
        {
            List<Musician> order = run.Order.Count == run.Roster.Count ? run.Order : run.Roster;
            Rectangle box = new Rectangle(lifeChip.X - 40, TopH + 6, 236, 18 + order.Count * 24);
            Gfx.Rect(sb, new Rectangle(box.X + 4, box.Y + 4, box.Width, box.Height), Color.Black * 0.5f);
            Gfx.Rect(sb, box, Palette.Void * 0.96f);
            Ornament.DoubleFrame(sb, box, Palette.PaperDim);
            for (int k = 0; k < order.Count; k++)
            {
                Musician m = order[k];
                float y = box.Y + 10 + k * 24;
                Gfx.TextSpaced(sb, Ui.Font, m.Name, box.X + 14, y + 3, BandColumn.SpotOf(m), TextSize.Tiny, 2f);
                string most = NumberText.Get(run.MaxLifeOf(m));
                float mostX = box.Right - 14 - Gfx.TextWidth(Ui.Font, most, TextSize.Tiny);
                Gfx.Text(sb, Ui.Font, most, mostX, y + 3, Palette.LineGrey, TextSize.Tiny);
                Gfx.Text(sb, Ui.Font, Slash, mostX - 12, y + 3, Palette.LineGrey, TextSize.Tiny);
                Gfx.TextRight(sb, Ui.Font, NumberText.Get(run.LifeOf(m)), mostX - 18, y, Palette.Paper, TextSize.Body);
            }
        }

        //Tips : the motif tooltip (or BAND LIFE's), drawn last by the page
        public static void DrawTips(SpriteBatch sb, RunState run)
        {
            if (hoverLife)
            {
                DrawLifeTip(sb, run);
                return;
            }
            if (hoverMotif < 0 || hoverMotif >= run.Motifs.Count) return;
            Motif m = run.Motifs[hoverMotif];
            Ui.Tooltip(sb, m.Name, m.TipWrapped, Input.MousePos.X, Input.MousePos.Y, m.RuleWords);   // WORDS EVERYWHERE : its rule words under it
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
