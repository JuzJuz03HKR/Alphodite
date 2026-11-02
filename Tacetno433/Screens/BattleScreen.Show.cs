using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Battle;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //BattleScreen Show (Beta1 round 4) : the moments drawn over everything else.
    //   QTE LIGHT   the ring's own effects (a note landing, a parry's clash) are drawn over the world after the
    //               QTE FOCUS has dimmed it, so they always shine
    //   CUT-IN      the SIGNATURE starts with the conductor's picture sliding across on a slanted band, the same
    //               as the old duel (the user asked for it to stay), then the strokes begin
    //   NUMBERS     damage as chunky pixel numbers with a dark outline (Core/PixelDigits.cs), in the striker's colour.
    //               They pop in too big, settle, shudder for a moment, rise and fade. A big hit is bigger.
    public partial class BattleScreen
    {
        //Number : one damage or heal number over a fighter
        private struct Number
        {
            public bool On;
            public int Value;
            public bool Plus;          // a heal : + in front
            public bool Big;
            public Vector2 At;
            public float Age;
            public Color Tint;
        }
        private readonly Number[] numbers = new Number[24];

        //Spawn Number : at a point over a fighter (in the same place the words go)
        private void SpawnNumber(Vector2 at, int value, bool plus, bool big, Color tint)
        {
            int slot = 0;
            for (int i = 0; i < numbers.Length; i++)
                if (!numbers[i].On) { slot = i; break; }
            numbers[slot] = new Number { On = true, Value = value, Plus = plus, Big = big, At = at, Age = 0f, Tint = tint };
        }

        //Draw Numbers : POP 0 to 0.08 seconds (too big), SETTLE to 0.2, a SHUDDER while it lands, then it rises and fades
        private void DrawNumbers(SpriteBatch sb)
        {
            for (int i = 0; i < numbers.Length; i++)
            {
                Number n = numbers[i];
                if (!n.On) continue;
                float t = n.Age;
                float life = BattleRules.TurnNumberTime;
                float scale = t < 0.08f ? MathHelper.Lerp(0.5f, 1.35f, t / 0.08f)
                            : t < 0.2f ? MathHelper.Lerp(1.35f, 1f, (t - 0.08f) / 0.12f) : 1f;
                float rise = 34f * (1f - (1f - t / life) * (1f - t / life));
                float shudder = t < 0.14f ? (float)Math.Sin(t * 130f) * 3f * (1f - t / 0.14f) : 0f;
                float a = t > life - 0.25f ? (life - t) / 0.25f : 1f;
                float cell = (n.Big ? BattleRules.TurnNumberBigCell : BattleRules.TurnNumberCell) * scale;
                PixelDigits.Draw(sb, n.Value, n.Plus, n.At.X + shudder, n.At.Y - rise, cell, n.Tint * a, Color.Black * (0.9f * a));
            }
        }

        //STAR MARKS (block D item 3, 2 Nov) : a move's stars over its blow, the same row as on its button (Ui.Stars) : the ones
        //it has in the legend's colour, lighting one after another, the rest dim, on a dark pill so they read over any light
        private struct Mark
        {
            public bool On;
            public Vector2 At;
            public int Count;
            public float Age;
            public Color Tint;
        }
        private readonly Mark[] starMarks = new Mark[6];
        private const float MarkLife = 0.95f;
        private const float MarkStep = 0.07f;                          // a star lights this long after the one before

        //Spawn Stars : count stars (1 to 3) of the move that struck, over its blow on box
        private void SpawnStars(Rectangle box, int count, Color tint)
        {
            int slot = 0;
            for (int i = 0; i < starMarks.Length; i++)
                if (!starMarks[i].On) { slot = i; break; }
            Vector2 at = new Vector2(box.Center.X, box.Y + box.Height * 0.45f - BattleRules.StarMarkLift);
            starMarks[slot] = new Mark { On = true, At = at, Count = Math.Min(count, BattleRules.RehearseMax), Age = 0f, Tint = tint };
        }

        //Draw Star Marks : through the battle's camera with the numbers. Each pops in big and settles, the row rises a little
        //and fades at the end.
        private void DrawStarMarks(SpriteBatch sb)
        {
            float gap = BattleRules.StarMarkGap;
            int total = BattleRules.RehearseMax;
            for (int i = 0; i < starMarks.Length; i++)
            {
                if (!starMarks[i].On) continue;
                Mark m = starMarks[i];
                float rise = 12f * Math.Min(1f, m.Age / MarkLife);
                float fade = Math.Min(1f, Math.Min(m.Age / 0.08f, (MarkLife - m.Age) / 0.25f));
                float left = m.At.X - gap * (total - 1) / 2f;
                float y = m.At.Y - rise;
                Gfx.Pill(sb, new Rectangle((int)(left - 14f), (int)(y - 12f), (int)(gap * (total - 1) + 28f), 24), Palette.Void * (0.6f * fade));
                for (int k = 0; k < total; k++)
                {
                    float x = left + k * gap;
                    if (k >= m.Count)
                    {
                        Gfx.Star(sb, x, y, BattleRules.StarMarkSize * 0.55f, Palette.LineGrey * (0.6f * fade));
                        continue;
                    }
                    float lit = m.Age - k * MarkStep;
                    if (lit < 0f) continue;
                    float pop = lit < 0.1f ? 1f + 0.9f * (1f - lit / 0.1f) : 1f;
                    Gfx.DrawGlow(sb, x, y, BattleRules.StarMarkSize * 3f * pop, m.Tint * (0.5f * fade));
                    Gfx.Star(sb, x, y, BattleRules.StarMarkSize * pop, Color.Lerp(m.Tint, Color.White, 0.35f) * fade);
                }
            }
        }

        //GIFT AT WORK (block D item 4, 2 Nov) : when a gift works, its badge (the diamond with its initials, framed in the colour of
        //the legend it is for, MotifArt.Badge) pops up big over who it worked on, a ring of that colour opening from it, then it
        //rises and fades. The gift's name still pops beside it. So the player sees which gift did it, the same badge as in the
        //top bar and the shop.
        private struct GiftPop
        {
            public bool On;
            public Motif Gift;
            public Vector2 At;
            public float Age;
            public bool OnScreen;      // over the ring (SPARE STRINGS), not on a fighter : drawn without the battle's camera
        }
        private readonly GiftPop[] giftPops = new GiftPop[6];
        private const float GiftPopLife = 1.1f;

        //Spawn Gift : a gift worked at at (on a fighter, or over the ring when onScreen)
        private void SpawnGift(Motif gift, Vector2 at, bool onScreen)
        {
            int slot = 0;
            for (int i = 0; i < giftPops.Length; i++)
                if (!giftPops[i].On) { slot = i; break; }
            giftPops[slot] = new GiftPop { On = true, Gift = gift, At = at, Age = 0f, OnScreen = onScreen };
        }

        //Draw Gift Pops : the ones on fighters (onScreen false) through the battle's camera, the others after. A soft glow of its
        //colour, the ring opening, the badge popping in too big and settling, rising and fading at the end.
        private void DrawGiftPops(SpriteBatch sb, bool onScreen)
        {
            for (int i = 0; i < giftPops.Length; i++)
            {
                if (!giftPops[i].On || giftPops[i].OnScreen != onScreen) continue;
                GiftPop g = giftPops[i];
                float t = g.Age / GiftPopLife;
                float pop = g.Age < 0.12f ? 1f + 0.7f * (1f - g.Age / 0.12f) : 1f;
                float fade = Math.Min(1f, Math.Min(g.Age / 0.06f, (GiftPopLife - g.Age) / 0.3f));
                Vector2 at = g.At + new Vector2(0f, -18f * Smooth(t));
                Color key = MotifArt.KeyColor(g.Gift.Key);
                Gfx.DrawGlow(sb, at.X, at.Y, 46f * pop, key * (0.35f * fade));
                float ring = 18f + 50f * Smooth(Math.Min(1f, t * 2.5f));
                Gfx.CircleOutline(sb, at.X, at.Y, ring, key * (0.8f * fade * (1f - Math.Min(1f, t * 2.5f))), 2f);
                MotifArt.Badge(sb, g.Gift, at.X, at.Y, BattleRules.GiftPopSize * pop, fade);
            }
        }

        private readonly DuelEffects qteEffects = new DuelEffects();   // QTE LIGHT : rings, sparks and slashes at the ring
        private readonly HitFx qteHits = new HitFx();                  //             a parry's clash
        private float cutIn;                                           // CUT-IN : counts down while the picture is up

        //Update Show : the moments' clocks. They play on through the hit stop, they are the feedback of it.
        private void UpdateShow(float dt)
        {
            qteEffects.Update(dt);
            qteHits.Update(dt, false);
            for (int i = 0; i < numbers.Length; i++)
            {
                if (!numbers[i].On) continue;
                numbers[i].Age += dt;
                if (numbers[i].Age > BattleRules.TurnNumberTime) numbers[i].On = false;
            }
            for (int i = 0; i < giftPops.Length; i++)                                     // GIFT AT WORK
            {
                if (!giftPops[i].On) continue;
                giftPops[i].Age += dt;
                if (giftPops[i].Age > GiftPopLife) giftPops[i].On = false;
            }
            for (int i = 0; i < starMarks.Length; i++)                                    // STAR MARKS
            {
                if (!starMarks[i].On) continue;
                starMarks[i].Age += dt;
                if (starMarks[i].Age > MarkLife) starMarks[i].On = false;
            }
        }

        //Draw QTE Light : added on top (additive), then the frame's own blend is put back
        private void DrawQteLight(SpriteBatch sb)
        {
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, null, null, null, Sharp.Transform);
            qteEffects.DrawHits(sb);
            qteEffects.DrawShards(sb);
            qteEffects.DrawSparks(sb);
            qteEffects.DrawSlashes(sb);
            qteEffects.DrawFlares(sb);
            qteHits.DrawGlow(sb);
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, Sharp.Transform);
        }

        //Cut-In Conductor : whose picture the CUT-IN shows (PRACTICE has no run, it borrows THE UNHEARING's)
        private Conductor CutInConductor()
        {
            if (run != null && run.Conductor != null) return run.Conductor;
            for (int i = 0; i < ConductorList.All.Length; i++)
                if (ConductorList.All[i].Turn == battle.Kit) return ConductorList.All[i];
            return ConductorList.All[0];
        }

        //CUT-IN : a slanted black band with two white rules slides in, the conductor's picture breaks out of it top
        //and bottom, speed lines rush across, the move's name, then it all slides away (DuelScreen.Hud.cs DrawCutIn).
        private void DrawCutIn(SpriteBatch sb)
        {
            if (bossCut)                                                                 // BOSS SHOW : THE STRIKE or PRE (BattleScreen.Boss.cs)
            {
                DrawBossCut(sb);
                return;
            }
            Conductor c = CutInConductor();
            float t = 1f - cutIn / BattleRules.TurnCutInTime;              // 0 at the start, 1 at the end
            float slideIn = Smooth(t / 0.2f);
            float slideOut = MathHelper.Clamp((t - 0.8f) / 0.2f, 0f, 1f);
            slideOut = slideOut * slideOut;
            float offset = (1f - slideIn) * -1500f + slideOut * 1500f;
            float dim = slideIn * (1f - slideOut);

            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Color.Black * (0.7f * dim));

            //Band : a slanted black strip with two white rules
            int x = (int)offset;
            Rectangle band = new Rectangle(x - 80, 262, TacetGame.ScreenW + 160, 196);
            Gfx.SlantBox(sb, new Rectangle(band.X + 10, band.Y + 10, band.Width, band.Height), 70, Color.Black * 0.6f);
            Gfx.SlantBox(sb, band, 70, Palette.Void);
            Gfx.Rect(sb, band.X, band.Y + 6, band.Width, 2, Palette.Paper * 0.8f);
            Gfx.Rect(sb, band.X, band.Bottom - 8, band.Width, 2, Palette.Paper * 0.8f);

            //Speed Lines : streaks rushing across the band
            for (int i = 0; i < 14; i++)
            {
                float ly = band.Y + 20 + (i * 53) % (band.Height - 40);
                float lx = (time * 2600f + i * 211f) % (TacetGame.ScreenW + 400) - 200 + offset;
                Gfx.Rect(sb, lx, ly, 90 + (i % 3) * 70, 2, Palette.Paper * 0.18f);
            }

            //Portrait : taller than the band, so it breaks out of it top and bottom. The conductor's big painting (the user 13.109 :
            //their art number 2 is the FINALE's picture, Content/Art/Conductors/cutin_<name>.png) at the size it was painted, standing
            //on the band's middle line. Until it comes, SIGIL (block C) : the conductor's mark drawn by the code, its light filling the window
            Texture2D art = ArtBank.CutInOf(c);
            Rectangle portrait = art != null ? new Rectangle(x + 120, band.Center.Y - art.Height / 2, art.Width, art.Height)
                                             : new Rectangle(x + 100, 170, 360, 380);
            Gfx.Rect(sb, portrait, Palette.Void);
            if (art != null) sb.Draw(art, new Vector2(portrait.X, portrait.Y), Color.White);
            else LegendArt.Sigil(sb, c, portrait, time, 1f, 1f - t * 1.6f);               // it strikes as the cut in lands
            Gfx.RectOutline(sb, portrait, Palette.Paper * (0.7f * dim), 1);

            //Shards : sixteen streaks thrown out of the portrait, the same every time
            float cx = portrait.Center.X;
            float cy = portrait.Center.Y - 20;
            for (int i = 0; i < 16; i++)
            {
                float angle = i * 2.4f;
                float reach = 90f + t * (380f + (i % 5) * 90f);
                Vector2 d = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle) * 0.7f);
                Vector2 head = new Vector2(cx, cy) + d * reach;
                Gfx.Line(sb, head - d * 26f, head, Palette.Highlight * (dim * (1f - t * 0.6f)), 2f);
            }

            //Eye Line : the streak of light through the picture, like the key art
            Hollow.Flare(sb, cx + 180f, 300f, 1300f, dim * 0.8f);

            //Names : the tag, the conductor, the marking in italian, and the move
            int nx = x + 560;
            Ui.Tag(sb, Words.Finale, nx, 292, true, dim);
            Gfx.TextSpaced(sb, Game.Font, c.Name, nx + Ui.TagWidth(Words.Finale) + 16, 297, Palette.PaperDim * dim, TextSize.Tiny, 4f);
            Gfx.Text(sb, Game.BigFont, c.SignatureMark, nx, 318, Palette.Paper * dim, TextSize.Title);
            Gfx.Text(sb, Game.LogoFont, signatureName, nx - 4, 360, Palette.Highlight * dim, TextSize.Banner * 0.85f);

            //Flash : the first instant is pure white (turned down by LESS FLASHING)
            float flash = 1f - t / 0.1f;
            if (flash > 0f) Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Color.White * (0.8f * flash * Flashing));
        }
    }
}
