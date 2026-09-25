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
    //DuelScreen.Finale : ending the piece. When the line is far enough our way at the end of a
    //round (BattleState.FinaleOffered), a FINALE card comes up, TACET counts one bar, and four
    //bright notes arrive, one for each way of the 4/4 pattern. All four on the beat and the
    //right way, and the fight ends at once with a picture of its own. One slip, and it falls
    //apart and TACET claws some ground back.
    public partial class DuelScreen
    {
        //Finale Start : the FINALE card, then one bar of TACET's count, then the player's bar
        private void StartFinale()
        {
            phase = Phase.Finale;
            finaleClock = -FinaleTitleTime;
            finaleCalled = -1;
            finaleStep = 0;
            finaleDone = false;
            finaleEnd = 0f;
            gesture.Clear();
            Say(SayFinale);
            SoundBank.Play(Sfx.RoundStart, 1f, 0.3f);
        }

        //Finale Update : four notes arrive like any other bar, one for each way of the pattern.
        //All four on the beat and the right way ends the fight. One slip and it falls apart.
        private void UpdateFinale(float dt)
        {
            finaleClock += dt;

            if (finaleDone)
            {
                finaleEnd += dt;
                if (finaleEnd >= 0.7f) EndRound();
                return;
            }
            if (finaleClock < 0f)
            {
                gesture.Clear();          // nothing counts while the card is up
                return;
            }

            //Count : four ticks, each one sends a note down the lane
            while (finaleCalled < 3 && finaleClock >= (finaleCalled + 1) * beatLen)
            {
                finaleCalled++;
                SoundBank.Play(Sfx.BeatTick, 0.9f, 0.3f);
                effects.SpawnRipple(EnemyX(), EnemyFeetY, 150f, true);
            }

            float target = (4 + finaleStep) * beatLen;
            float now = finaleClock - Settings.TimingOffset;
            bool stroked = gesture.Read();

            if (stroked && now >= target - EarlyLimit()) JudgeFinale(target, now);
            else if (now > target + LateLimit()) FinaleSlip(gradeWord[(int)Grade.Hesitate]);
        }

        //Finale Judge : one stroke of the last bar. Anything short of GOOD, or the wrong way,
        //and the ending falls apart.
        private void JudgeFinale(float target, float now)
        {
            float off = Math.Abs(now - target);
            Grade grade = Grade.Miss;
            if (off <= battle.GoodWindow) grade = Grade.Good;
            if (off <= battle.PerfectWindow) grade = Grade.Perfect;
            bool rightWay = gesture.Direction == pattern[finaleStep];

            hand.Play(PoseFor(gesture.Direction));
            baton.Whip(gesture.Direction);

            if (grade == Grade.Miss || !rightWay)
            {
                SoundBank.Play(Sfx.QteMiss);
                FinaleSlip(rightWay ? gradeWord[(int)Grade.Miss] : "WRONG WAY");
                return;
            }

            //Landed : the whole band plays, and the line creeps toward the edge
            ShowJudge(gradeWord[(int)grade], 0.55f);
            effects.SpawnFlare(HitX, RingY, 1f + finaleStep * 0.3f);
            effects.SpawnSparks(HitX, RingY, 12 + finaleStep * 4, 1f);
            effects.SpawnWave(BandX, RingY, 1f, 1f, AnswerWaveSpeed);
            shake = Math.Max(shake, 0.4f + finaleStep * 0.15f);
            if (!SoundBank.PlayAnswer(finaleStep * 2, 1f, 0f)) SoundBank.Play(Sfx.QteBoost);
            if (grade == Grade.Perfect) SoundBank.Play(Sfx.QtePerfect);

            Formation f = Game.CurrentRun.Formation;
            for (int s = 0; s < StageLayout.SeatCount; s++)
            {
                if (f.Seated[s] == null) continue;
                lit[s] = 1f;
                actors[s].Play(CharacterAnim.Attack);
            }

            finaleStep++;
            if (finaleStep >= 4) WinFinale();
        }

        //Finale Slip : the ending falls apart. TACET claws some ground back.
        private void FinaleSlip(string word)
        {
            finaleDone = true;
            battle.FailFinale();
            ShowJudge(word, 0.55f);
            effects.SpawnPop(RingX(), RingY - 150f, "THE ENDING FALLS APART", Palette.PaperDim, 0.6f);
            shake = 0.8f;
            SoundBank.Play(Sfx.ClashLose);
            Say(SayFinaleFail);
        }

        //Finale Win : the last chord. The fight ends, with a picture of its own.
        private void WinFinale()
        {
            finaleDone = true;
            battle.WinFinale();
            cutIn = BattleRules.CutInTime * 1.3f;
            cutInFinale = true;
            effects.SpawnFlare(HitX, RingY, 2.2f);
            effects.SpawnSparks(HitX, RingY, 40, 1f);
            SoundBank.Play(Sfx.ComboUp, 1f, 0.9f);
            SoundBank.Play(Sfx.ClashWin, 1f, 0.5f);
            Say(SayFinaleWin);
        }

        //Finale Notes : four bright diamonds, one for each way of the pattern, sent down the lane
        //one per beat of the count
        private void DrawFinaleNotes(SpriteBatch sb)
        {
            if (phase != Phase.Finale || finaleClock < 0f || finaleDone) return;

            for (int i = finaleStep; i <= finaleCalled && i < 4; i++)
            {
                float t = MathHelper.Clamp((finaleClock - i * beatLen) / (4f * beatLen), 0f, 1f);
                float x = MathHelper.Lerp(TacetGame.ScreenW - 30f, HitX, t);
                Gfx.DrawGlow(sb, x, RingY, 40f, Palette.Highlight * 0.5f);
                Gfx.Diamond(sb, x, RingY, 20f, Palette.Paper);
                Gfx.DiamondOutline(sb, x, RingY, 24f, Palette.Highlight, 2f);
                NoteGlyph.Arrow(sb, pattern[i], x, RingY, 18f, 7f, Palette.Ink, 3f);
            }

            //Progress : four marks over the lane, filled as the strokes land
            for (int i = 0; i < 4; i++)
            {
                float mx = HitX + 60f + i * 26f;
                if (i < finaleStep) Gfx.Diamond(sb, mx, RingY - LaneHalf - 20f, 7f, Palette.Highlight);
                else Gfx.DiamondOutline(sb, mx, RingY - LaneHalf - 20f, 7f, Palette.Paper * 0.7f, 1.5f);
            }
        }

        //Finale Card : the band gathers itself. The way the four strokes go is written under it.
        private void DrawFinaleCard(SpriteBatch sb)
        {
            float t = 1f + finaleClock / FinaleTitleTime;              // 0 when the card appears, 1 when the bar starts
            float a = t < 0.2f ? t / 0.2f : (t > 0.85f ? (1f - t) / 0.15f : 1f);
            if (a < 0f) a = 0f;

            Gfx.Rect(sb, 0, 256, TacetGame.ScreenW, 196, Color.Black * (0.85f * a));
            Gfx.Rect(sb, 0, 256, TacetGame.ScreenW, 1, Palette.Paper * a);
            Gfx.Rect(sb, 0, 451, TacetGame.ScreenW, 1, Palette.Paper * a);
            Hollow.Streak(sb, 640f, 262f, 900f * a, a * 0.8f);
            Gfx.TextSpacedCentered(sb, Game.Font, "THE SILENCE STAGGERS", 640, 272, Palette.PaperDim * a, TextSize.Tiny, 4f);
            Gfx.TextCentered(sb, Game.LogoFont, "FINALE", 640 + (1f - a) * 40f, 340, Palette.Highlight * a, TextSize.Banner);
            Gfx.TextSpacedCentered(sb, Game.Font, "CONDUCT THE WHOLE BAR ON THE BEAT", 640, 386, Palette.Paper * a, TextSize.Label, 3f);

            for (int i = 0; i < 4; i++)
                NoteGlyph.Arrow(sb, pattern[i], 568f + i * 48f, 426f, 22f, 8f, Palette.Highlight * a, 3f);
        }

        //Finale For Picture : DEVELOPER TOOL hook, so the capture tool can picture the finale
        //without playing a whole round first
        public void BeginFinaleForPicture()
        {
            beatLen = 60f / battle.Tempo;
            StartFinale();
        }
    }
}
