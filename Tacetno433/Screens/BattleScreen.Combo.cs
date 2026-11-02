using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Battle;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //BattleScreen Combo (Beta1 block D item 6, 2 Nov, docs/BETA1_DIRECTION.md 43) : the fun moments 3 and 6 of 16.4, shown.
    //BLOCK G (2 Nov, 46) : the COMBO is a rule now (TurnBattle.ComboScale, a little harder from TurnComboEvery PERFECTs), its
    //x shows under the number, and FORTE (after a FINALE) shows over it with the turns it has left.
    //   COMBO         the PERFECTs in a row (TurnBattle.PerfectRun), big on the right over the panel from 2, popping at each
    //                 one. A miss breaks it : the number cracks and falls away grey.
    //   THE MUSIC     the longer the COMBO, the louder the band : the conductor's weather grows (THE STAGE TURNS) and TACET's dark
    //   SWELLS        is pushed back from the edges (ENGULF), a miss lets the silence creep back (16.4 moment 3)
    //   LIVE RANK     the fight's RANK as it stands (half the PERFECT share, half the life kept, BattleState.RankScoreOf), under
    //                 the TURN in the top right, popping when it changes (16.4 moment 6 : the grade seen while playing)
    public partial class BattleScreen
    {
        private int comboShown;            // the COMBO the screen shows
        private float comboPop;            // 1 when it just grew, fades
        private int comboBroken;           // the COMBO that just broke, falling away
        private float comboBreak;          // 1 when it just broke, fades
        private float comboGlow;           // 0..1 eased : how loud the band is (THE MUSIC SWELLS)
        private float soloGlow;            // 0..1 eased : SOLO (BLOCK G), the musician in front has the spotlight
        private int rankShown = -1;        // LIVE RANK : the letter shown (0 S .. 3 C)
        private float rankPop;
        private static readonly string[] RankWords = { "S", "A", "B", "C" };
        private const string WordCombo = Words.Combo;
        private const string WordRank = "RANK";
        //BLOCK G : the COMBO's x by its step ("x1.05" ...) and FORTE's, made once from BattleRules
        private static readonly string[] ComboScaleWords = MakeComboWords();
        private static readonly string ForteScaleWord = "x" + BattleRules.TurnForteScale.ToString("0.00", CultureInfo.InvariantCulture);

        private static string[] MakeComboWords()
        {
            string[] words = new string[BattleRules.TurnComboSteps + 1];
            for (int k = 0; k < words.Length; k++)
                words[k] = "x" + (1f + k * BattleRules.TurnComboStep).ToString("0.00", CultureInfo.InvariantCulture);
            return words;
        }

        //Update Combo : follow the rules' run of PERFECTs, the swell and the rank
        private void UpdateCombo(float dt)
        {
            int run = battle.PerfectRun;
            if (run > comboShown) comboPop = 1f;
            if (run < comboShown && comboShown >= BattleRules.ComboShowFrom)
            {
                comboBroken = comboShown;
                comboBreak = 1f;
            }
            comboShown = run;
            comboPop = Math.Max(0f, comboPop - dt * 4f);
            comboBreak = Math.Max(0f, comboBreak - dt * 1.4f);
            float swell = Math.Min(1f, run / (float)BattleRules.ComboFull);
            comboGlow += (swell - comboGlow) * Math.Min(1f, dt * (swell > comboGlow ? 3f : 1.2f));   // up quickly, back slowly
            float soloWant = fore != null && !fore.IsEnemy && fore.Solo && !fore.Down ? 1f : 0f;      // SOLO : the spotlight finds them
            soloGlow += (soloWant - soloGlow) * Math.Min(1f, dt * 3f);

            int rank = BattleState.RankIndexOf(BattleState.RankScoreOf(perfects, notesPlayed, LifeShareNow()));
            if (rank != rankShown)
            {
                if (rankShown >= 0) rankPop = 1f;
                rankShown = rank;
            }
            rankPop = Math.Max(0f, rankPop - dt * 3f);
        }

        //Life Share Now : the band's life now over its life when it walked in (the RANK's other half)
        private float LifeShareNow()
        {
            if (lifeAtStart <= 0) return 1f;
            int now = 0;
            for (int i = 0; i < battle.Party.Count; i++) now += Math.Max(0, battle.Party[i].Hp);
            return now / (float)lifeAtStart;
        }

        //Combo Light : how far THE MUSIC SWELLS pushes TACET's dark back (DarkShare takes it away)
        private float ComboLight()
        {
            return comboGlow * BattleRules.ComboDarkBack;
        }

        //Draw Combo : the COMBO over the panel on the right, and a broken one falling away
        private void DrawCombo(SpriteBatch sb)
        {
            const float cx = 1176f;
            const float cy = 536f;
            if (comboShown >= BattleRules.ComboShowFrom)
            {
                float big = 1f + 0.35f * comboPop * comboPop;
                float a = Math.Min(1f, 0.6f + 0.1f * comboShown);
                Color ink = comboGlow >= 1f ? Palette.SpotHolyHot : Palette.Highlight;      // at its fullest the number turns gold
                Gfx.DrawGlow(sb, cx, cy, 70f * big, ink * (0.25f * a * (0.4f + 0.6f * comboGlow)));
                PixelDigits.Draw(sb, comboShown, false, cx, cy, BattleRules.ComboCell * big, ink * a, Color.Black * (0.85f * a));
                Gfx.TextSpacedCentered(sb, Game.Font, WordCombo, cx, cy + 22f, Palette.PaperDim * a, TextSize.Label, 4f);
                int steps = Math.Min(BattleRules.TurnComboSteps, comboShown / Math.Max(1, BattleRules.TurnComboEvery));   // COMBO (BLOCK G) : its x
                if (steps > 0) Gfx.TextSpacedCentered(sb, Game.Font, ComboScaleWords[steps], cx, cy + 40f, ink * a, TextSize.Label, 3f);
            }
            if (battle.Forte > 0)                                                                // FORTE (BLOCK G) : after a FINALE
            {
                float beat = 0.75f + 0.25f * (float)Math.Sin(worldTime * 5f);
                float fy = cy - 58f;
                Gfx.DrawGlow(sb, cx, fy, 60f, Palette.Highlight * (0.18f * beat));
                Gfx.TextSpacedCentered(sb, Game.BigFont, Words.Forte, cx, fy - 12f, Palette.Highlight * beat, TextSize.Small, 4f);
                Gfx.TextSpacedCentered(sb, Game.Font, ForteScaleWord, cx - 22f, fy + 14f, Palette.Highlight, TextSize.Label, 2f);
                Gfx.TextSpacedCentered(sb, Game.Font, NumberText.Get(battle.Forte), cx + 18f, fy + 14f, Palette.PaperDim, TextSize.Label, 2f);
                Gfx.TextSpacedCentered(sb, Game.Font, Words.Turn, cx + 44f, fy + 14f, Palette.PaperDim, TextSize.Label, 2f);
            }
            if (comboBreak > 0f)
            {
                float fall = (1f - comboBreak) * 40f;
                float a = comboBreak * comboBreak;
                PixelDigits.Draw(sb, comboBroken, false, cx + 6f * (1f - comboBreak), cy + fall, BattleRules.ComboCell, Palette.LineGrey * a, Color.Black * (0.6f * a));
                Gfx.Line(sb, new Vector2(cx - 30f, cy - 14f + fall), new Vector2(cx + 26f, cy + 12f + fall), Palette.Void * a, 3f);   // the crack
            }
        }

        //Draw Live Rank : RANK and its letter under the TURN, S in gold, popping when it changes
        private void DrawLiveRank(SpriteBatch sb)
        {
            if (rankShown < 0 || practice) return;
            float y = 38f;
            Gfx.TextSpacedRight(sb, Game.Font, WordRank, TacetGame.ScreenW - 64, y + 10f, Palette.LineGrey, TextSize.Label, 3f);
            Color ink = rankShown == 0 ? Palette.SpotHolyHot : rankShown == 1 ? Palette.Highlight : Palette.PaperDim;
            float big = 1f + 0.5f * rankPop;
            if (rankPop > 0f) Gfx.DrawGlow(sb, TacetGame.ScreenW - 44f, y + 16f, 30f * big, ink * (0.4f * rankPop));
            Gfx.Text(sb, Game.BigFont, RankWords[Math.Min(rankShown, RankWords.Length - 1)], TacetGame.ScreenW - 52, y, ink, TextSize.Small * big);
        }
    }
}
