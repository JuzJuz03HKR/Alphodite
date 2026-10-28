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
        }

        //Draw QTE Light : added on top (additive), then the frame's own blend is put back
        private void DrawQteLight(SpriteBatch sb)
        {
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp);
            qteEffects.DrawHits(sb);
            qteEffects.DrawShards(sb);
            qteEffects.DrawSparks(sb);
            qteEffects.DrawSlashes(sb);
            qteEffects.DrawFlares(sb);
            qteHits.DrawGlow(sb);
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);
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

            //Portrait : taller than the band, so it breaks out of it top and bottom (an empty frame until the art)
            Rectangle portrait = new Rectangle(x + 100, 170, 360, 380);
            Gfx.Rect(sb, portrait, Palette.Void);
            ArtBank.DrawOrSlot(sb, ArtBank.CutInOf(c), portrait, Palette.Paper, 1f);

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
            Ui.Tag(sb, "SIGNATURE", nx, 292, true, dim);
            Gfx.TextSpaced(sb, Game.Font, c.Name, nx + Ui.TagWidth("SIGNATURE") + 16, 297, Palette.PaperDim * dim, TextSize.Tiny, 4f);
            Gfx.Text(sb, Game.BigFont, c.SignatureMark, nx, 318, Palette.Paper * dim, TextSize.Title);
            Gfx.Text(sb, Game.LogoFont, signatureName, nx - 4, 360, Palette.Highlight * dim, TextSize.Banner * 0.85f);

            //Flash : the first instant is pure white (turned down by LESS FLASHING)
            float flash = 1f - t / 0.1f;
            if (flash > 0f) Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Color.White * (0.8f * flash * Flashing));
        }
    }
}
