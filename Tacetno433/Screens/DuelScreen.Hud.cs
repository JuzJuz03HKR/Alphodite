using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Battle;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //DuelScreen.Hud : what sits on top of the world. The strip along the top (round, stamina,
    //the tug bar, TACET's name), the ring at the hit point, the clash numbers, the combo,
    //the round banners, the signature and finale cut-in, the count-in and the finale card.
    public partial class DuelScreen
    {
        //Plates : round, beat, tempo and stamina on the left, TACET's name and trait on the right
        private void DrawPlates(SpriteBatch sb)
        {
            RunState run = Game.CurrentRun;

            //Round Plate
            Gfx.SlantBox(sb, new Rectangle(-20, roundPlate.Y, roundPlate.Width + 20, roundPlate.Height), -Ui.Slant, Palette.Void * 0.88f);
            Gfx.Text(sb, Game.BigFont, battle.RoundLabel, 20, roundPlate.Y + 4, Palette.Highlight, TextSize.Small);
            Gfx.TextSpaced(sb, Game.Font, "BEAT", 22, roundPlate.Y + 40, Palette.LineGrey, TextSize.Tiny, 2f);
            Gfx.Text(sb, Game.Font, NumberText.Get(beat + 1), 72, roundPlate.Y + 35, Palette.Paper, TextSize.Body);
            Gfx.Text(sb, Game.Font, beatsLabel, 90, roundPlate.Y + 35, Palette.LineGrey, TextSize.Body);
            Gfx.TextSpacedRight(sb, Game.Font, tempoLabel, 250, roundPlate.Y + 40, Palette.PaperDim, TextSize.Tiny, 2f);

            //Stamina : the bar lights up when it changes, instead of a number popping up
            Gfx.TextSpaced(sb, Game.Font, "STAMINA", 22, roundPlate.Y + 64, Palette.LineGrey, TextSize.Tiny, 2f);
            Gfx.TextRight(sb, Game.Font, run.StaminaValue, 250, roundPlate.Y + 58, Palette.Paper, TextSize.Body);
            Rectangle bar = new Rectangle(22, roundPlate.Y + 80, 228, 10);
            if (staminaFlash > 0f) Gfx.DrawGlowBox(sb, bar, Palette.Highlight * (0.6f * staminaFlash));
            Ui.CapsuleBar(sb, bar, run.Stamina / (float)run.MaxStamina, Palette.Paper, 1f);

            //Enemy Plate : taller when the enemy has a trait on this floor, to name it
            bool trait = battle.TraitShown;
            int plateH = enemyPlate.Height + (trait ? 24 : 0);
            Gfx.SlantBox(sb, new Rectangle(enemyPlate.X, enemyPlate.Y, enemyPlate.Width + 20, plateH), Ui.Slant, Palette.Void * 0.88f);
            Gfx.TextRight(sb, Game.BigFont, battle.Enemy.Name, enemyPlate.Right - 18, enemyPlate.Y + 4, Palette.Highlight, TextSize.Small * 0.9f);
            Gfx.TextSpacedRight(sb, Game.Font, battle.Enemy.KindLabel, enemyPlate.Right - 18, enemyPlate.Y + 40, Palette.LineGrey, TextSize.Tiny, 3f);
            if (trait)
                Gfx.TextSpacedRight(sb, Game.Font, battle.Enemy.TraitName, enemyPlate.Right - 18, enemyPlate.Y + 62, Palette.Paper, TextSize.Tiny, 3f);
        }

        //Tug Bar : who is winning, in the middle of the top strip. White is our ground, black is
        //TACET's. The marker flashes on every push, instead of a number popping up.
        private void DrawTugBar(SpriteBatch sb)
        {
            Rectangle plate = new Rectangle(tugBar.X - 76, tugBar.Y - 16, tugBar.Width + 152, tugBar.Height + 32);
            Gfx.SlantBox(sb, plate, Ui.Slant, Palette.Void * 0.88f);
            Gfx.TextSpacedRight(sb, Game.Font, "YOU", tugBar.X - 14, tugBar.Center.Y - 6, Palette.Paper, TextSize.Tiny, 2f);
            Gfx.TextSpaced(sb, Game.Font, "TACET", tugBar.Right + 14, tugBar.Center.Y - 6, Palette.PaperDim, TextSize.Tiny, 2f);

            float split = TugSplitX(displayLine);
            Rectangle rim = tugBar;
            rim.Inflate(2, 2);
            Gfx.Pill(sb, rim, Palette.LineGrey);
            Gfx.Pill(sb, tugBar, Palette.Void);
            int filled = (int)(split - tugBar.X);
            if (filled >= tugBar.Height)
                Gfx.Pill(sb, new Rectangle(tugBar.X, tugBar.Y, filled, tugBar.Height), Palette.Paper);

            Gfx.Rect(sb, tugBar.Center.X, tugBar.Y - 5, 1, tugBar.Height + 10, Palette.LineGrey);
            if (tugFlash > 0f) Gfx.DrawGlow(sb, split, tugBar.Center.Y, 34f * tugFlash, Palette.Highlight * tugFlash);
            Gfx.Rect(sb, split - 1, tugBar.Y - 6, 3, tugBar.Height + 12, Palette.Highlight);
            Gfx.Diamond(sb, split, tugBar.Center.Y, 6, Palette.Highlight);
        }

        //Answer Ring : the timing circle for the beat being answered. It starts closing one beat
        //before the note is due and meets its mark exactly on the beat, when TACET's note sits
        //right in the middle of it. The arrow outside the ring points the way to swing.
        private void DrawAnswerRing(SpriteBatch sb)
        {
            float t = AnswerProgress();
            if (t < 0f || rolling || holding) return;

            float cx = RingX();
            float cy = RingY;
            float a = MathHelper.Clamp(t * 4f, 0f, 1f);

            //Flick Back : a smaller ring for the second note of a pair
            float mark = onGrace ? RingTarget * 0.7f : RingTarget;
            float start = onGrace ? RingStart * 0.7f : RingStart;

            //Mark : where the ring has to be when the stroke lands, swelling with the beat
            float swell = BeatPulse() * 3f;
            Gfx.CircleOutline(sb, cx, cy, mark + swell, Palette.Ink * a, 5f);
            Gfx.CircleOutline(sb, cx, cy, mark + swell, Palette.Highlight * a, 3f);

            //Closing Ring : shrinks to the mark on the beat, then keeps going a little
            float radius = start - (start - mark) * t;
            if (radius < 8f) radius = 8f;
            Gfx.CircleOutline(sb, cx, cy, radius, Palette.Ink * a, 6f);
            Gfx.CircleOutline(sb, cx, cy, radius, Palette.Highlight * a, 3f);

            //Spark : the second note of a pair takes any way, so the ring holds a spark instead
            if (onGrace)
            {
                NoteGlyph.Spark(sb, cx, cy - mark - 26f, 14f, Palette.Highlight * a);
                return;
            }

            //Arrow : outside the mark, on the side the baton should travel toward
            Flick want = WantedWay();
            Vector2 d = DirVector(want);
            float ax = cx + d.X * (mark + 30f);
            float ay = cy + d.Y * (mark + 30f);
            DrawDirection(sb, want, ax, ay, 36f, 12f, Palette.Ink * a, 9f);
            DrawDirection(sb, want, ax, ay, 32f, 9f, Palette.Highlight * a, 4f);
        }

        //Ready : while TACET plays its first bar, the band counts the player in, 3 2 1, so the
        //first answer never comes as a surprise
        private void DrawReady(SpriteBatch sb)
        {
            if (phase != Phase.Play || pending > 0 || clock < beatLen || clock >= AnswerTime(0)) return;

            int count = 4 - (int)(clock / beatLen);           // 3 on beat two, 2 on beat three, 1 on beat four
            float within = (clock / beatLen) % 1f;
            float a = 1f - within * 0.7f;
            float y = RingY - 118f;
            Gfx.Circle(sb, HitX, y + 2f, 34f, Palette.Void * (0.85f * a));
            Gfx.CircleOutline(sb, HitX, y + 2f, 34f, Palette.Paper * (0.6f * a), 2f);
            Gfx.TextCentered(sb, Game.LogoFont, countWords[count], HitX, y, Palette.Highlight * a, TextSize.Banner * 0.6f);
        }

        //Judge : the word for the last stroke on a small dark plate under the hit point, so it
        //reads on the bright stage and in TACET's dark alike. It jumps in, rises and fades.
        private void DrawJudge(SpriteBatch sb)
        {
            const float Life = 0.6f;
            if (judgeTimer >= Life || judgeWord.Length == 0) return;

            float t = judgeTimer / Life;
            float a = 1f - t * t;
            float pop = 1f + (1f - Math.Min(1f, judgeTimer / 0.08f)) * 0.25f;
            float y = RingY + 92f - t * 16f;

            Rectangle plate = new Rectangle((int)(RingX() - judgeWidth * pop / 2f - 18f), (int)(y - 18f), (int)(judgeWidth * pop + 36f), 36);
            Gfx.SlantBox(sb, plate, 10, Palette.Void * (0.85f * a));
            Gfx.TextCentered(sb, Game.BigFont, judgeWord, RingX(), y - 2f, Palette.Highlight * a, judgeScale * pop);
        }

        //Clash Numbers : our number against TACET's, counting up side by side above the line.
        //When the count is done the bigger one swells and the smaller one drops away.
        private void DrawClashNumbers(SpriteBatch sb)
        {
            if (!clashShown || clashTimer > BattleRules.ClashShowTime) return;

            float count = MathHelper.Clamp(clashTimer / BattleRules.ClashCountTime, 0f, 1f);
            float settle = MathHelper.Clamp((clashTimer - BattleRules.ClashCountTime) / 0.12f, 0f, 1f);
            float a = 1f - MathHelper.Clamp((clashTimer - (BattleRules.ClashShowTime - 0.15f)) / 0.15f, 0f, 1f);

            int ours = (int)Math.Round(clashOurs * count);
            int theirs = (int)Math.Round(clashTheirs * count);
            bool weWin = clashOurs > clashTheirs;
            bool theyWin = clashTheirs > clashOurs;

            float cx = RingX();

            float ourScale = 0.46f + (weWin ? 0.16f : (theyWin ? -0.12f : 0f)) * settle;
            float theirScale = 0.46f + (theyWin ? 0.16f : (weWin ? -0.12f : 0f)) * settle;
            float ourDrop = theyWin ? settle * 18f : 0f;
            float theirDrop = weWin ? settle * 18f : 0f;

            //Plates : ours is paper with ink, TACET's is black with white, so both read anywhere
            Rectangle ourPlate = new Rectangle((int)cx - 124, (int)(ClashY - 27 + ourDrop), 104, 54);
            Rectangle theirPlate = new Rectangle((int)cx + 20, (int)(ClashY - 27 + theirDrop), 104, 54);
            Gfx.SlantBox(sb, ourPlate, 12, Palette.Paper * a);
            Gfx.SlantOutline(sb, ourPlate, 12, Palette.Ink * a, 2f);
            Gfx.SlantBox(sb, theirPlate, 12, Palette.Void * a);
            Gfx.SlantOutline(sb, theirPlate, 12, Palette.Paper * a, 2f);

            float ourFade = theyWin ? 1f - settle * 0.5f : 1f;
            float theirFade = weWin ? 1f - settle * 0.5f : 1f;
            Gfx.TextCentered(sb, Game.LogoFont, NumberText.Get(ours), ourPlate.Center.X, ourPlate.Center.Y - 3, Palette.Ink * (a * ourFade), ourScale);
            Gfx.TextCentered(sb, Game.LogoFont, NumberText.Get(theirs), theirPlate.Center.X, theirPlate.Center.Y - 3, Palette.Highlight * (a * theirFade), theirScale);

            Gfx.Diamond(sb, cx, ClashY, 9, Palette.Ink * a);
            Gfx.Diamond(sb, cx, ClashY, 6, Palette.Highlight * a);
        }

        //Combo : a small plate right under the tug bar, where the eye already is.
        //While the band is on fire a second plate beside it says FORTISSIMO, with a pip for
        //every beat of fire still to come.
        private void DrawCombo(SpriteBatch sb)
        {
            if (battle.Combo >= 2)
            {
                float grow = 1f + comboPulse * 0.25f;
                Rectangle plate = new Rectangle(560, 70, 160, 40);
                Gfx.SlantBox(sb, plate, Ui.Slant, Palette.Void * 0.88f);
                Gfx.Rect(sb, plate.X + 14, plate.Bottom - 3, plate.Width - 28, 2, Palette.Accent);
                Gfx.TextSpaced(sb, Game.Font, "COMBO", plate.X + 22, plate.Y + 8, Palette.PaperDim, TextSize.Tiny, 3f);
                Gfx.TextCentered(sb, Game.LogoFont, NumberText.Get(battle.Combo), plate.X + 100, plate.Center.Y - 2, Palette.Accent, 0.34f * grow);
                Gfx.Text(sb, Game.Font, comboBonusWords[Math.Min(battle.Combo, BattleRules.ComboMax)], plate.X + 116, plate.Y + 10, Palette.Paper, TextSize.Label);

                //Pips : how close the combo is to setting the band on fire
                int toFire = battle.Combo % battle.FortissimoCombo;          // CON BRIO needs fewer
                if (battle.FortissimoLeft > 0) toFire = battle.FortissimoCombo;
                Ui.Pips(sb, plate.X + 24, plate.Y + 28, toFire, battle.FortissimoCombo, 3, 9, 1f);
            }

            if (fireGlow > 0.02f)
            {
                Rectangle fire = new Rectangle(730, 70, 196, 40);
                float a = fireGlow;
                Gfx.DrawGlowBox(sb, fire, Palette.Highlight * (0.35f * a));
                Gfx.SlantBox(sb, fire, Ui.Slant, Palette.Paper * a);
                Gfx.TextCentered(sb, Game.BigFont, "ff", fire.X + 30, fire.Center.Y - 2, Palette.Ink * a, TextSize.Small);
                Gfx.TextSpaced(sb, Game.Font, "FORTISSIMO", fire.X + 52, fire.Y + 8, Palette.Ink * a, TextSize.Tiny, 2f);
                for (int i = 0; i < BattleRules.FortissimoBeats; i++)
                {
                    float px = fire.X + 56 + i * 11;
                    if (i < battle.FortissimoLeft) Gfx.Diamond(sb, px, fire.Y + 28, 3, Palette.Ink * a);
                    else Gfx.DiamondOutline(sb, px, fire.Y + 28, 3, Palette.InkSoft * a, 1f);
                }
            }
        }

        //Banner : a band across the middle of the screen for round starts and endings
        private void DrawBanner(SpriteBatch sb, string text, float t)
        {
            float a = t < 0.2f ? t / 0.2f : (t > 0.8f ? (1f - t) / 0.2f : 1f);
            if (a < 0f) a = 0f;

            Gfx.Rect(sb, 0, 290, TacetGame.ScreenW, 130, Color.Black * (0.82f * a));
            Gfx.Rect(sb, 0, 290, TacetGame.ScreenW, 1, Palette.Paper * a);
            Gfx.Rect(sb, 0, 419, TacetGame.ScreenW, 1, Palette.Paper * a);
            Gfx.TextSpacedCentered(sb, Game.Font, battle.EnemyTitle, 640, 304, Palette.PaperDim * a, TextSize.Tiny, 4f);
            Gfx.TextCentered(sb, Game.LogoFont, text, 640 + (1f - a) * 40f, 360, Palette.Highlight * a, TextSize.Banner * 0.8f);
            Ornament.Divider(sb, 640, 402, 120, Palette.PaperDim * a);
        }

        //Cut In : the signature, staged like an E.G.O in Limbus Company. A white flash, a black
        //band sweeping across with the conductor's picture breaking out of it, a streak of light
        //through the eyes, shards bursting outward, and the move's name stacked on the right:
        //a tag, the conductor, the marking as a score would write it, and the name itself.
        //ARTWORK : Content/Art/Conductors/cutin_<name>.png, see ArtBank.
        private void DrawCutIn(SpriteBatch sb)
        {
            Conductor c = Game.CurrentRun.Conductor;
            float length = cutInFinale ? BattleRules.CutInTime * 1.3f : BattleRules.CutInTime;
            float t = 1f - cutIn / length;                              // 0 at the start, 1 at the end
            float slideIn = MathHelper.Clamp(t / 0.18f, 0f, 1f);
            slideIn = slideIn * slideIn * (3f - 2f * slideIn);
            float slideOut = MathHelper.Clamp((t - 0.82f) / 0.18f, 0f, 1f);
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

            //Portrait : taller than the band, so it breaks out of it top and bottom
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

            //Names : a tag, the conductor, the marking in italian, and the move.
            //The finale writes "Fine" (the end, as a score marks it) and the band's own name.
            int nx = x + 560;
            string tag = cutInFinale ? "FINALE" : "SIGNATURE";
            Ui.Tag(sb, tag, nx, 292, true, dim);
            Gfx.TextSpaced(sb, Game.Font, c.Name, nx + Ui.TagWidth(tag) + 16, 297, Palette.PaperDim * dim, TextSize.Tiny, 4f);
            Gfx.Text(sb, Game.BigFont, cutInFinale ? "Fine" : c.SignatureMark, nx, 318, Palette.Paper * dim, TextSize.Title);
            Gfx.Text(sb, Game.LogoFont, cutInFinale ? Game.CurrentRun.BandName : signatureName, nx - 4, 360, Palette.Highlight * dim, TextSize.Banner * 0.85f);

            //Flash : the first instant is pure white
            float flash = 1f - t / 0.1f;
            if (flash > 0f) Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Color.White * (0.8f * flash));
        }

        //Count In : after a pause, the band counts three beats before carrying on
        private void DrawCountIn(SpriteBatch sb)
        {
            int left = Math.Min(3, Math.Max(1, (int)Math.Ceiling(countIn / beatLen)));
            float within = 1f - (countIn / beatLen - (left - 1));      // 0 when the number appears, 1 at the next
            float a = 1f - within * 0.6f;

            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Color.Black * 0.25f);
            Gfx.TextCentered(sb, Game.LogoFont, countWords[left], HitX, RingY - 150f, Palette.Highlight * a, TextSize.Banner * (1.1f - within * 0.2f));
            Gfx.TextSpacedCentered(sb, Game.Font, "THE BAND COUNTS YOU IN", HitX, RingY - 80f, Palette.Paper * a, TextSize.Label, 4f);
        }

    }
}
