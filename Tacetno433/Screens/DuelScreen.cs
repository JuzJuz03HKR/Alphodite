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
    //DuelScreen : one round of the fight, played beat by beat.
    //
    //Every beat runs through four steps:
    //   INTENT  the beat lights up, and you see whether TACET is hitting HEAVY or LIGHT
    //   RING    a ring closes on a mark. Press F boost, G play, H ease before it runs out.
    //           On the mark is PERFECT, near it is GOOD, far off is a MISS (weaker, costs more),
    //           and not pressing at all is HESITATE (weaker)
    //   CLASH   both sides are revealed, the line moves, stamina changes
    //   then the next beat. After eight beats the round ends and you go back to the score.
    //
    //Picture, following the storyboard: our stage is the bright side with the conductor's
    //hand in front, TACET is the black side with its own hand reaching in, and the border
    //between them IS the tug of war line. The ring sits on that border, where the clash happens.
    public class DuelScreen : GameScreen
    {
        private enum Phase { Intro, Intent, Ring, Clash, RoundEnd }

        //Duel Layout
        private Rectangle stageBox = new Rectangle(40, 196, 600, 420);
        private const float SeatScale = 0.9f;
        private Rectangle frame = new Rectangle(300, 0, 680, 84);
        private const int CellX = 392;
        private const int CellW = 62;
        private Rectangle tugBar = new Rectangle(430, 98, 420, 10);
        private const float RingY = 372f;
        private const float RingStart = 140f;
        private const float RingTarget = 44f;
        private Rectangle roundPlate = new Rectangle(0, 10, 282, 100);
        private Rectangle enemyPlate = new Rectangle(1000, 10, 280, 64);
        private Rectangle intentTag = new Rectangle(1120, 82, 140, 30);
        private Rectangle speedButton = new Rectangle(1170, 120, 90, 26);
        private Rectangle console = new Rectangle(430, 660, 420, 60);
        private Rectangle[] keys = new Rectangle[3];

        //Choice Words : indexed by the key, F G H
        private static Choice[] keyChoice = { Choice.Boost, Choice.Normal, Choice.Ease };
        private static string[] keyWord = { "BOOST", "PLAY", "EASE" };
        private static string[] keyLetter = { "F", "G", "H" };
        private static string[] gradeWord = { "", "PERFECT", "GOOD", "MISS", "HESITATE" };
        private static string[] enemyChoiceWord = { "PLAYS", "BOOSTS", "EASES" };
        private static string[] comboWords = { "", "", "COMBO 2", "COMBO 3", "COMBO 4", "COMBO 5", "COMBO MAX" };
        private static string[] comboBonusWords = { "+0%", "+6%", "+12%", "+18%", "+24%", "+30%" };

        //Duel State
        private BattleState battle;
        private DuelEffects effects = new DuelEffects();
        private Phase phase;
        private float phaseTimer;
        private float clashLength;
        private int beat;
        private bool pressed;
        private int speed = 1;
        private float time;
        private float displayLine;
        private float batonFlick;
        private float comboPulse;
        private float[] lit = new float[StageLayout.SeatCount];
        private float[] keyFlash = new float[3];
        private string roundEndLabel = "";
        private string bannerText = "";
        private string beatsLabel = "";

        public override void Load()
        {
            battle = Game.CurrentRun.Battle;
            displayLine = battle.Line;
            roundEndLabel = "END OF ROUND " + battle.Round;
            beatsLabel = "/ " + BattleRules.BeatsPerRound;

            for (int i = 0; i < 3; i++)
                keys[i] = new Rectangle(console.X + 30 + i * 124, console.Y - 36, 114, 48);

            phase = Phase.Intro;
            phaseTimer = 0f;
            beat = 0;

            SoundBank.PlayMusic(battle.Enemy.Kind == EnemyKind.Boss ? Music.Boss : Music.Battle);
            SoundBank.Play(Sfx.RoundStart);
        }

        //Edge X : where the bright stage ends and TACET begins
        private float EdgeX()
        {
            float x = 640f + displayLine / BattleRules.LineLimit * 560f;
            if (x < 80f) x = 80f;
            if (x > 1200f) x = 1200f;
            return x;
        }

        //Enemy X : TACET's figure stands in the middle of its own black area
        private float EnemyX()
        {
            float edge = EdgeX();
            return edge + (TacetGame.ScreenW - edge) * 0.58f;
        }

        //Ring X : the ring sits on the border, kept away from the screen edges
        private float RingX()
        {
            return MathHelper.Clamp(EdgeX(), 580f, 860f);
        }

        private Rectangle SeatRect(int seat)
        {
            return StageLayout.DuelSeatRect(seat, stageBox, SeatScale);
        }

        public override void Update(float dt)
        {
            time += dt;
            float step = dt * speed;

            effects.Update(step);
            for (int s = 0; s < lit.Length; s++) lit[s] = Math.Max(0f, lit[s] - step * 1.6f);
            for (int k = 0; k < 3; k++) keyFlash[k] = Math.Max(0f, keyFlash[k] - dt * 3f);
            batonFlick = Math.Max(0f, batonFlick - dt * 4f);
            comboPulse = Math.Max(0f, comboPulse - dt * 3f);
            displayLine += (battle.Line - displayLine) * Math.Min(1f, step * 4f);

            //Speed Toggle : x1 or x2, the ring itself always runs at normal speed
            if (Input.KeyPressed(Keys.X) || Input.ClickedOn(speedButton))
            {
                speed = speed == 1 ? 2 : 1;
                SoundBank.Play(Sfx.UiMove);
            }

            if (phase == Phase.Intro)
            {
                phaseTimer += step;
                if (phaseTimer >= BattleRules.IntroTime) StartBeat(0);
            }
            else if (phase == Phase.Intent)
            {
                phaseTimer += step;
                if (phaseTimer >= BattleRules.IntentTime)
                {
                    //Silent Beat : nobody plays, no ring, just a breath
                    if (battle.HasAction(beat))
                    {
                        phase = Phase.Ring;
                        phaseTimer = 0f;
                        pressed = false;
                    }
                    else
                    {
                        ResolveBeat(Choice.Normal, Grade.None);
                    }
                }
            }
            else if (phase == Phase.Ring)
            {
                phaseTimer += dt;
                ReadKeys();

                //Hesitate : the ring ran out and nothing was pressed
                if (!pressed && phaseTimer >= BattleRules.RingTime + BattleRules.LateTime)
                {
                    SoundBank.Play(Sfx.QteHesitate);
                    ResolveBeat(Choice.Normal, Grade.Hesitate);
                }
            }
            else if (phase == Phase.Clash)
            {
                phaseTimer += step;
                if (phaseTimer >= clashLength) NextBeat();
            }
            else if (phase == Phase.RoundEnd)
            {
                phaseTimer += step;
                if (phaseTimer >= BattleRules.RoundEndTime) LeaveRound();
            }
        }

        private void StartBeat(int b)
        {
            beat = b;
            phase = Phase.Intent;
            phaseTimer = 0f;
            SoundBank.Play(Sfx.BeatTick);
        }

        //Keys Read : F G H on the keyboard, or clicking the three keys on screen
        private void ReadKeys()
        {
            if (pressed) return;

            int chosen = -1;
            if (Input.KeyPressed(Keys.F)) chosen = 0;
            if (Input.KeyPressed(Keys.G)) chosen = 1;
            if (Input.KeyPressed(Keys.H)) chosen = 2;
            for (int k = 0; k < 3; k++)
                if (Input.ClickedOn(keys[k])) chosen = k;

            if (chosen < 0) return;

            //Locked Tempo : THE METRONOME can only play, so every key counts as G
            if (battle.ChoicesLocked) chosen = 1;

            pressed = true;
            keyFlash[chosen] = 1f;
            batonFlick = 1f;

            //Timing Grade : how far the press was from the moment the ring met the mark
            float off = Math.Abs(phaseTimer - BattleRules.RingTime);
            Grade grade = Grade.Miss;
            if (off <= battle.GoodWindow) grade = Grade.Good;
            if (off <= battle.PerfectWindow) grade = Grade.Perfect;

            Choice choice = keyChoice[chosen];
            if (choice == Choice.Boost) SoundBank.Play(Sfx.QteBoost);
            if (choice == Choice.Normal) SoundBank.Play(Sfx.QteNormal);
            if (choice == Choice.Ease) SoundBank.Play(Sfx.QteEase);
            if (grade == Grade.Perfect) SoundBank.Play(Sfx.QtePerfect);
            if (grade == Grade.Miss) SoundBank.Play(Sfx.QteMiss);

            ResolveBeat(choice, grade);
        }

        //Beat Resolve : ask the rules what happened, then show it
        private void ResolveBeat(Choice choice, Grade grade)
        {
            int comboBefore = battle.Combo;
            BeatResult r = battle.Resolve(beat, choice, grade);

            phase = Phase.Clash;
            phaseTimer = 0f;
            clashLength = grade == Grade.None ? BattleRules.RestClashTime : BattleRules.ClashTime;

            float enemyX = EnemyX();
            float ringX = RingX();

            //Our Sound : every seat that played lights up and sends a wave
            Formation f = Game.CurrentRun.Formation;
            for (int s = 0; s < StageLayout.SeatCount; s++)
            {
                if (!f.Plays(s, beat)) continue;
                lit[s] = 1f;
                Rectangle seat = SeatRect(s);
                effects.SpawnWave(seat.Center.X, seat.Center.Y, 1f);
            }

            //TACET's Sound
            if (r.EnemyPower > 0)
            {
                effects.SpawnWave(enemyX - 60, 300f, -1f);
                effects.SpawnWave(enemyX - 60, 420f, -1f);
            }

            //Numbers : sizes are BigFont scales, kept small so several can share the screen
            if (r.OurPower > 0)
                effects.SpawnPop(stageBox.Center.X, stageBox.Y + 4, NumberText.Get(r.OurPower), Palette.Highlight, 0.9f);
            if (r.EnemyPower > 0)
                effects.SpawnPop(enemyX, 180f, NumberText.Get(r.EnemyPower), Palette.Paper, 0.9f);
            if (r.Push != 0)
                effects.SpawnPop(TugSplitX(battle.Line), tugBar.Bottom + 38, NumberText.Signed(r.Push), Palette.Highlight, 0.6f);
            if (r.StaminaChange != 0)
                effects.SpawnPop(roundPlate.Right + 34, roundPlate.Y + 86, NumberText.Signed(r.StaminaChange), Palette.Paper, 0.5f);
            if (grade != Grade.None)
                effects.SpawnPop(ringX, RingY + 78, gradeWord[(int)grade], Palette.Highlight, 0.62f);
            if (battle.EnemyPower[beat] > 0)
                effects.SpawnPop(enemyX + 100, 214f, enemyChoiceWord[(int)r.EnemyChoice], Palette.Highlight, 0.5f);

            //Combo : grows on a PERFECT, breaks on a miss or no press
            if (r.Combo > comboBefore && r.Combo >= 2)
            {
                comboPulse = 1f;
                effects.SpawnPop(110, roundPlate.Bottom + 140, comboWords[Math.Min(r.Combo, comboWords.Length - 1)], Palette.Accent, 0.5f);
                SoundBank.Play(Sfx.ComboUp, 1f, Math.Min(0.5f, r.Combo * 0.08f));
            }
            if (r.ComboBroken)
            {
                effects.SpawnPop(110, roundPlate.Bottom + 40, "COMBO BREAK", Palette.Paper, 0.5f);
                SoundBank.Play(Sfx.ComboBreak);
            }

            //Special Moments
            if (r.OutOfBreath)
            {
                effects.SpawnPop(stageBox.Center.X, stageBox.Center.Y, "OUT OF BREATH", Palette.Highlight, 0.6f);
                SoundBank.Play(Sfx.StaminaEmpty);
            }
            else if (r.OurPower > 0 && battle.EnemyPower[beat] == 0 && !battle.EnemyHidden[beat])
            {
                effects.SpawnPop(stageBox.Center.X, stageBox.Y + 40, "FREE HIT", Palette.Paper, 0.5f);
            }
            if (r.SecondWind)
                effects.SpawnPop(stageBox.Center.X, stageBox.Center.Y + 40, "SECOND WIND", Palette.Accent, 0.6f);
            if (r.Fired)
                effects.SpawnPop(stageBox.Center.X, stageBox.Y + 40, "RUNAWAY FIRE", Palette.Accent, 0.5f);

            //Sounds
            if (grade == Grade.None) SoundBank.Play(Sfx.RestRecover);
            else if (r.Push > 0) SoundBank.Play(Sfx.ClashWin);
            else if (r.Push < 0) SoundBank.Play(Sfx.ClashLose);
            else SoundBank.Play(Sfx.ClashEven);

            if (r.EnemyChoice == Choice.Boost) SoundBank.Play(Sfx.EnemyBoost);
            if (r.EnemyChoice == Choice.Ease) SoundBank.Play(Sfx.EnemyEase);
        }

        private void NextBeat()
        {
            if (!battle.Finished && beat + 1 < BattleRules.BeatsPerRound)
            {
                StartBeat(beat + 1);
                return;
            }

            //Round End : pick the banner without changing the battle yet, so the top strip
            //keeps showing this round while the banner is up
            phase = Phase.RoundEnd;
            phaseTimer = 0f;

            if (battle.Finished)
                bannerText = battle.PlayerWon ? "THE SILENCE BREAKS" : "THE SILENCE WINS";
            else if (battle.Round >= BattleRules.MaxRounds)
                bannerText = battle.Line > 0f ? "YOU HOLD THE STAGE" : "TACET HOLDS THE STAGE";
            else
                bannerText = roundEndLabel;
        }

        private void LeaveRound()
        {
            battle.EndRound();

            if (battle.Finished)
                Game.Screens.Change(new ResultScreen());
            else
                Game.Screens.Change(new ScoreScreen());
        }

        //Tug Split : where the marker sits on the tug bar
        private float TugSplitX(float line)
        {
            float t = (line / BattleRules.LineLimit + 1f) / 2f;
            return tugBar.X + tugBar.Width * t;
        }

        public override void Draw(SpriteBatch sb)
        {
            float edge = EdgeX();

            DrawStageSide(sb);
            DrawMusicians(sb);
            DrawConductorHand(sb);

            //Stage Dark : when the band has no breath left the lights go down
            if (Game.CurrentRun.Stamina == 0)
                Gfx.Rect(sb, 0, 0, edge, TacetGame.ScreenH, Color.Black * 0.35f);

            //TACET : closer to losing means a tighter, angrier edge
            float danger = MathHelper.Clamp(-displayLine / BattleRules.LineLimit, 0f, 1f);
            TacetField.Draw(sb, edge, time, 0.2f + danger * 0.8f);
            DrawEdgeSpikes(sb, edge, danger);
            DrawEnemyFigure(sb);
            DrawRhythmFan(sb);
            DrawEnemyHand(sb);

            effects.DrawWaves(sb);

            DrawSequencer(sb);
            DrawTugBar(sb);
            DrawPlates(sb);
            DrawConsole(sb);

            if (phase == Phase.Intent || phase == Phase.Ring) DrawRing(sb);
            DrawCombo(sb);

            effects.DrawPops(sb, Game.BigFont);

            if (phase == Phase.Intro) DrawBanner(sb, battle.RoundLabel, phaseTimer / BattleRules.IntroTime);
            if (phase == Phase.RoundEnd) DrawBanner(sb, bannerText, phaseTimer / BattleRules.RoundEndTime);
        }

        //Stage Side : the bright half of the picture. The era sketch is the PLACEHOLDER
        //for the era background painting.
        private void DrawStageSide(SpriteBatch sb)
        {
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageLight);
            Gfx.DrawGlowBox(sb, new Rectangle(-200, -100, 1100, 700), Color.White * 0.5f);

            //ARTWORK : sb.Draw(eraBackground[run.Era], new Rectangle(0, 0, 1280, 720), Color.White);
            EraBackdrop.Draw(sb, Game.CurrentRun.Era, new Rectangle(-20, 110, 900, 440), time, Palette.Ink * 0.32f);

            //Floor : a paler band the musicians stand on
            Gfx.Rect(sb, 0, 500, TacetGame.ScreenW, TacetGame.ScreenH - 500, Palette.StageLightDeep * 0.5f);
            Gfx.Rect(sb, 0, 500, TacetGame.ScreenW, 1, Palette.InkSoft * 0.4f);

            //Gesture Arcs : the sweep of the conductor's arm, faint rings around the band
            Gfx.Arc(sb, 170, 560, 210, MathHelper.Pi * 1.05f, MathHelper.Pi * 1.75f, Palette.Ink * 0.35f, 3f);
            Gfx.Arc(sb, 420, 610, 240, MathHelper.Pi * 1.1f, MathHelper.Pi * 1.7f, Palette.Ink * 0.25f, 3f);
        }

        //Musicians : white capsules with dark outlines, glowing while they play.
        //No name tags here, like the storyboard, so the stage stays uncluttered.
        private void DrawMusicians(SpriteBatch sb)
        {
            Formation f = Game.CurrentRun.Formation;

            for (int s = 0; s < StageLayout.SeatCount; s++)
            {
                Musician m = f.Seated[s];
                if (m == null) continue;

                //Up Next : whoever plays this beat turns bright and breathes a little
                bool upNext = (phase == Phase.Intent || phase == Phase.Ring) && f.Plays(s, beat);
                Rectangle r = SeatRect(s);
                float glow = lit[s];
                if (upNext)
                {
                    glow = Math.Max(glow, 0.35f + (float)Math.Sin(time * 8f) * 0.15f);
                    r.Y -= 4;
                }

                MusicianArt.Capsule(sb, r, m, Color.White, Palette.Ink, glow);
                if (upNext) Gfx.Diamond(sb, r.Center.X, r.Y - 12, 4, Palette.Ink);
            }
        }

        //Conductor Hand : PLACEHOLDER for the hand and baton in the bottom left corner.
        //The baton flicks every time a key is pressed.
        private void DrawConductorHand(SpriteBatch sb)
        {
            //Sleeve
            Gfx.Line(sb, -80, 800, 150, 640, Palette.Ink, 104f);
            Gfx.Line(sb, -80, 800, 150, 640, Palette.Paper, 96f);
            Gfx.Line(sb, 100, 700, 150, 640, Palette.InkSoft * 0.5f, 2f);

            //Baton : pivots at the hand, swaying with the beat
            float sway = (float)Math.Sin(time * 3f) * 0.06f - batonFlick * 0.25f;
            float angle = -0.95f + sway;
            float length = 250f;
            Vector2 grip = new Vector2(200, 610);
            Vector2 tip = new Vector2(grip.X + (float)Math.Cos(angle) * length, grip.Y + (float)Math.Sin(angle) * length);
            Gfx.Line(sb, grip, tip, Palette.Ink, 5f);
            Gfx.Line(sb, grip, tip, Palette.Paper, 2f);
            Gfx.Circle(sb, grip.X - 10, grip.Y + 12, 11, Palette.Ink);
            Gfx.Circle(sb, grip.X - 10, grip.Y + 12, 8, Palette.Paper);
            if (batonFlick > 0f) Gfx.DrawGlow(sb, tip.X, tip.Y, 30f * batonFlick, Palette.Highlight * batonFlick);

            //Hand : a palm and four fingers wrapped round the baton
            Gfx.Circle(sb, 180, 628, 42, Palette.Ink);
            Gfx.Circle(sb, 180, 628, 39, Palette.Paper);
            for (int i = 0; i < 4; i++)
            {
                float fx = 196 + i * 9;
                float fy = 596 + i * 12;
                Gfx.Line(sb, fx - 20, fy + 6, fx + 12, fy - 2, Palette.Ink, 17f);
                Gfx.Line(sb, fx - 20, fy + 6, fx + 12, fy - 2, Palette.Paper, 13f);
            }
            Gfx.TextSpaced(sb, Game.Font, "ART  /  CONDUCTOR HAND", 24, 700, Palette.InkSoft, TextSize.Tiny, 2f);
        }

        //Edge Spikes : thin needles shooting out of TACET's edge into the light, like the
        //peaks of a sound wave in the storyboard. More of them when TACET is winning.
        private void DrawEdgeSpikes(SpriteBatch sb, float edge, float danger)
        {
            int count = 5 + (int)(danger * 6f);
            for (int i = 0; i < count; i++)
            {
                float y = 140 + (i * 97) % 520;
                float grow = (float)Math.Sin(time * (1.5f + i * 0.3f) + i) * 0.5f + 0.5f;
                float length = 20f + grow * (40f + danger * 60f);
                Gfx.Line(sb, edge + 4, y, edge - length, y, Palette.Void, 3f);
                Gfx.Line(sb, edge + 4, y, edge - length * 0.8f, y, Palette.Paper, 1f);
            }
        }

        //Enemy Figure : PLACEHOLDER. A shape in the dark with its eyes barred, as the design asks.
        private void DrawEnemyFigure(SpriteBatch sb)
        {
            Enemy e = battle.Enemy;
            float x = EnemyX() + 60;
            float size = e.Kind == EnemyKind.Boss ? 1.3f : (e.Kind == EnemyKind.Elite ? 1.15f : 1f);
            float breathe = 1f + (float)Math.Sin(time * 1.6f) * 0.03f;

            //Halo and Mist
            Gfx.DrawGlow(sb, x, 260f, 180f * size, e.Tone * 0.18f);
            Gfx.CircleOutline(sb, x, 250f, 90f * size, Palette.Paper * 0.10f, 1f);
            Gfx.CircleOutline(sb, x, 250f, 120f * size, Palette.Paper * 0.06f, 1f);

            //Body : head and shoulders, dark grey with a pale rim
            float headR = 34f * size;
            float headY = 230f;
            Color body = Color.Lerp(Palette.Stage, e.Tone, 0.25f);
            Gfx.Circle(sb, x, headY, headR * breathe, body);
            Gfx.Arc(sb, x, headY, headR * breathe, -2.4f, -0.4f, Palette.PaperDim, 2f);
            for (int y = (int)(headY + headR); y < 520; y += 2)
            {
                float t = (y - headY - headR) / (520f - headY - headR);
                float half = (40f + t * 90f) * size;
                Gfx.Rect(sb, x - half, y, half * 2f, 2, body * (1f - t * 0.6f));
            }

            //Eye Bar : the censor stripe from the storyboard
            Gfx.Rect(sb, x - headR * 1.1f, headY - 6, headR * 2.2f, 10, Palette.Paper * 0.8f);
            Gfx.TextSpaced(sb, Game.Font, "ART  /  ENEMY", x - 40, 540, Palette.LineGrey, TextSize.Tiny, 2f);
        }

        //Rhythm Fan : TACET's part for this round as a fan of bars in front of it.
        //Beat 1 is the top bar. Longer bars hit harder, ??? bars are dashed.
        private void DrawRhythmFan(SpriteBatch sb)
        {
            float fx = EnemyX() - 40;
            float fy = 360f;
            float inner = 34f;

            Gfx.Arc(sb, fx, fy, inner - 8, MathHelper.ToRadians(110), MathHelper.ToRadians(250), Palette.PaperDim * 0.7f, 2f);
            Gfx.Diamond(sb, fx, fy, 6, Palette.PaperDim);
            Gfx.TextSpacedCentered(sb, Game.Font, "TACET'S PART", fx - 60, fy + 100, Palette.LineGrey, TextSize.Tiny, 2f);

            for (int b = 0; b < BattleRules.BeatsPerRound; b++)
            {
                float angle = MathHelper.ToRadians(250 - b * 20);
                float cos = (float)Math.Cos(angle);
                float sin = (float)Math.Sin(angle);
                int power = battle.EnemyPower[b];
                bool current = b == beat && phase != Phase.Intro && phase != Phase.RoundEnd;
                bool done = battle.Results[b].Done;

                float length = power == 0 ? 8f : 24f + power * 7f;
                Color color = Palette.PaperDim * 0.6f;
                if (done) color = Palette.LineGrey * 0.6f;
                if (current) color = Palette.Highlight;

                if (battle.EnemyHidden[b] && !done)
                {
                    //Hidden : three dashes and a question mark
                    for (int d = 0; d < 3; d++)
                    {
                        float from = inner + d * 18f;
                        Gfx.Line(sb, fx + cos * from, fy + sin * from, fx + cos * (from + 10f), fy + sin * (from + 10f), color, 6f);
                    }
                    Gfx.TextCentered(sb, Game.Font, "?", fx + cos * (inner + 66f), fy + sin * (inner + 66f), color, TextSize.Body);
                    continue;
                }

                Gfx.Line(sb, fx + cos * inner, fy + sin * inner, fx + cos * (inner + length), fy + sin * (inner + length), color, current ? 9f : 7f);
                if (current) Gfx.DrawGlow(sb, fx + cos * (inner + length), fy + sin * (inner + length), 18f, Palette.Paper * 0.5f);
            }
        }

        //Enemy Hand : PLACEHOLDER for TACET's claw reaching in from the bottom right
        private void DrawEnemyHand(SpriteBatch sb)
        {
            float reach = (float)Math.Sin(time * 0.9f) * 6f;
            Color skin = new Color(34, 34, 38);

            Gfx.Line(sb, 1360, 780, 1160 + reach, 640, Palette.PaperDim, 92f);
            Gfx.Line(sb, 1360, 780, 1160 + reach, 640, skin, 86f);
            Gfx.Circle(sb, 1130 + reach, 628, 40, Palette.PaperDim);
            Gfx.Circle(sb, 1130 + reach, 628, 37, skin);

            //Claws : long thin fingers with pale tips
            for (int i = 0; i < 4; i++)
            {
                float sx = 1110 + reach;
                float sy = 606 + i * 16;
                float tx = 990 + reach + i * 18 + (float)Math.Sin(time * 1.3f + i) * 4f;
                float ty = 560 + i * 34;
                Gfx.Line(sb, sx, sy, tx, ty, Palette.PaperDim, 11f);
                Gfx.Line(sb, sx, sy, tx, ty, skin, 8f);
                Gfx.Line(sb, tx + 10, ty + 2, tx - 6, ty - 2, Palette.Paper, 2f);
            }
            Gfx.TextSpacedRight(sb, Game.Font, "ART  /  TACET HAND", 1256, 700, Palette.LineGrey, TextSize.Tiny, 2f);
        }

        //Sequencer : the white strip at the top, our beats above TACET's, like the storyboard
        private void DrawSequencer(SpriteBatch sb)
        {
            //Frame Body : wide at the top, narrowing toward the bottom
            float inset = 40f;
            for (int y = frame.Y; y < frame.Bottom; y += 2)
            {
                float t = (float)(y - frame.Y) / frame.Height;
                Gfx.Rect(sb, frame.X + inset * t, y, frame.Width - inset * 2f * t, 2, Palette.Paper);
            }
            Gfx.Rect(sb, frame.X + inset, frame.Bottom - 3, frame.Width - inset * 2f, 3, Palette.Ink);
            Gfx.Line(sb, frame.X, frame.Y, frame.X + inset, frame.Bottom, Palette.Ink, 3f);
            Gfx.Line(sb, frame.Right, frame.Y, frame.Right - inset, frame.Bottom, Palette.Ink, 3f);
            Gfx.Rect(sb, CellX - 4, frame.Y + 41, CellW * 8 + 8, 1, Palette.Ink * 0.35f);

            //Icons : conductor on the left, TACET on the right (PLACEHOLDERS)
            DrawIcon(sb, frame.X + 58, 38, Palette.StageLight, Palette.Ink);
            DrawIcon(sb, frame.Right - 58, 38, Palette.Void, battle.Enemy.Tone);

            //Current Beat Column
            if (phase != Phase.Intro && phase != Phase.RoundEnd)
                Gfx.Rect(sb, CellX + beat * CellW, frame.Y + 4, CellW, frame.Height - 12, Palette.Ink * 0.12f);

            //Bar Line : halfway through the round
            Gfx.Rect(sb, CellX + 4 * CellW, frame.Y + 8, 2, frame.Height - 20, Palette.Ink * 0.6f);

            for (int b = 0; b < BattleRules.BeatsPerRound; b++)
            {
                float cx = CellX + b * CellW + CellW / 2f;
                BeatResult r = battle.Results[b];

                //Our Row
                if (r.Done && r.Grade != Grade.None)
                    DrawChoiceGlyph(sb, r.PlayerChoice, cx, frame.Y + 22, Palette.Ink);
                else
                    DrawPowerHead(sb, battle.OurPowerAt(b), cx, frame.Y + 22, Palette.Ink);

                if (r.Done && r.Grade == Grade.Perfect)
                    Gfx.Diamond(sb, cx + 14, frame.Y + 10, 3, Palette.Ink);

                //TACET Row
                if (r.Done && battle.EnemyPower[b] > 0)
                    DrawChoiceGlyph(sb, r.EnemyChoice, cx, frame.Y + 60, Palette.InkSoft);
                else if (battle.EnemyHidden[b] && !r.Done)
                    Gfx.TextCentered(sb, Game.Font, "???", cx, frame.Y + 60, Palette.Ink, TextSize.Body);
                else
                    DrawPowerHead(sb, battle.EnemyPower[b], cx, frame.Y + 60, Palette.InkSoft);
            }
        }

        //Icon : a round badge with a little bust inside
        private void DrawIcon(SpriteBatch sb, float cx, float cy, Color fill, Color mark)
        {
            Gfx.Circle(sb, cx, cy, 26, fill);
            Gfx.CircleOutline(sb, cx, cy, 26, Palette.Ink, 3f);
            Gfx.Circle(sb, cx, cy - 5, 8, mark);
            Gfx.Rect(sb, cx - 11, cy + 5, 22, 10, mark);
        }

        //Power Head : a hollow ring for light notes, a solid dot for heavy ones, a dash for rest
        private void DrawPowerHead(SpriteBatch sb, int power, float cx, float cy, Color color)
        {
            if (power <= 0)
            {
                Gfx.Rect(sb, cx - 8, cy - 2, 16, 4, color * 0.5f);
                return;
            }
            float radius = Math.Min(11f, 4f + power * 0.7f);
            if (power >= 6) Gfx.Circle(sb, cx, cy, radius, color);
            else Gfx.CircleOutline(sb, cx, cy, radius, color, 2.5f);
        }

        //Choice Glyph : up for boost, a dot for play, down for ease
        private void DrawChoiceGlyph(SpriteBatch sb, Choice choice, float cx, float cy, Color color)
        {
            if (choice == Choice.Boost) Gfx.Triangle(sb, cx, cy, 8, true, color);
            else if (choice == Choice.Ease) Gfx.Triangle(sb, cx, cy, 8, false, color);
            else Gfx.Circle(sb, cx, cy, 5, color);
        }

        //Tug Bar : a long capsule, white is our ground, black is TACET's
        private void DrawTugBar(SpriteBatch sb)
        {
            float split = TugSplitX(displayLine);

            Rectangle plate = tugBar;
            plate.Inflate(3, 3);
            Gfx.Pill(sb, plate, Palette.Ink);
            Gfx.Pill(sb, tugBar, Palette.Void);
            int filled = (int)(split - tugBar.X);
            if (filled >= tugBar.Height)
                Gfx.Pill(sb, new Rectangle(tugBar.X, tugBar.Y, filled, tugBar.Height), Palette.Paper);

            Gfx.Rect(sb, tugBar.Center.X, tugBar.Y - 5, 1, tugBar.Height + 10, Palette.InkSoft);
            Gfx.Rect(sb, split - 1, tugBar.Y - 7, 3, tugBar.Height + 14, Palette.Highlight);
            Gfx.DiamondOutline(sb, split, tugBar.Center.Y, 9, Palette.Ink, 1.5f);
        }

        //Plates : round, beat and stamina on the left, TACET and its intent on the right
        private void DrawPlates(SpriteBatch sb)
        {
            RunState run = Game.CurrentRun;

            //Round Plate
            Gfx.SlantBox(sb, new Rectangle(-20, roundPlate.Y, roundPlate.Width + 20, roundPlate.Height), -Ui.Slant, Palette.Void * 0.88f);
            Gfx.Text(sb, Game.BigFont, battle.RoundLabel, 20, roundPlate.Y + 4, Palette.Highlight, TextSize.Small);
            Gfx.TextSpaced(sb, Game.Font, "BEAT", 22, roundPlate.Y + 40, Palette.LineGrey, TextSize.Tiny, 2f);
            Gfx.Text(sb, Game.Font, NumberText.Get(beat + 1), 72, roundPlate.Y + 35, Palette.Paper, TextSize.Body);
            Gfx.Text(sb, Game.Font, beatsLabel, 90, roundPlate.Y + 35, Palette.LineGrey, TextSize.Body);

            //Stamina
            Gfx.TextSpaced(sb, Game.Font, "STAMINA", 22, roundPlate.Y + 64, Palette.LineGrey, TextSize.Tiny, 2f);
            Gfx.TextRight(sb, Game.Font, run.StaminaValue, 250, roundPlate.Y + 58, Palette.Paper, TextSize.Body);
            Ui.CapsuleBar(sb, new Rectangle(22, roundPlate.Y + 80, 228, 10), run.Stamina / (float)run.MaxStamina, Palette.Paper, 1f);

            //Enemy Plate
            Gfx.SlantBox(sb, new Rectangle(enemyPlate.X, enemyPlate.Y, enemyPlate.Width + 20, enemyPlate.Height), Ui.Slant, Palette.Void * 0.88f);
            Gfx.TextRight(sb, Game.BigFont, battle.Enemy.Name, enemyPlate.Right - 18, enemyPlate.Y + 4, Palette.Highlight, TextSize.Small * 0.9f);
            Gfx.TextSpacedRight(sb, Game.Font, battle.Enemy.KindLabel, enemyPlate.Right - 18, enemyPlate.Y + 40, Palette.LineGrey, TextSize.Tiny, 3f);

            //Intent Tag : HEAVY or LIGHT before you choose
            if (phase == Phase.Intent || phase == Phase.Ring)
            {
                string intent = "SILENT";
                if (battle.EnemyHidden[beat]) intent = "???";
                else if (battle.EnemyPower[beat] > 0) intent = battle.IsHeavy(beat) ? "HEAVY" : "LIGHT";

                bool heavy = intent == "HEAVY";
                Gfx.Rect(sb, intentTag, heavy ? Palette.Paper : Palette.Void);
                Gfx.RectOutline(sb, intentTag, Palette.Paper, 1);
                Gfx.TextSpacedCentered(sb, Game.Font, intent, intentTag.Center.X, intentTag.Y + 6,
                                       heavy ? Palette.Ink : Palette.Highlight, TextSize.Body, 3f);
                Gfx.TextSpacedRight(sb, Game.Font, "NEXT", intentTag.X - 8, intentTag.Y + 9, Palette.LineGrey, TextSize.Tiny, 2f);
            }

            //Speed Button
            bool over = Input.MouseOver(speedButton);
            Gfx.Rect(sb, speedButton, over ? Palette.Stage : Palette.Void * 0.85f);
            Gfx.RectOutline(sb, speedButton, Palette.LineGrey, 1);
            Gfx.TextSpacedCentered(sb, Game.Font, speed == 1 ? "SPEED x1" : "SPEED x2", speedButton.Center.X, speedButton.Y + 6, Palette.Paper, TextSize.Tiny, 1.5f);
            Ui.KeyChipRight(sb, "X", speedButton.X - 6, speedButton.Center.Y, Palette.LineGrey);
        }

        //Console : the trapezoid desk at the bottom with the three keys on it, F G H
        private void DrawConsole(SpriteBatch sb)
        {
            //Desk : wider at the bottom, like the storyboard
            for (int y = console.Y; y < console.Bottom; y += 2)
            {
                float t = (float)(y - console.Y) / console.Height;
                float inset = 26f * (1f - t);
                Gfx.Rect(sb, console.X + inset, y, console.Width - inset * 2f, 2, Palette.Paper);
            }
            Gfx.Line(sb, console.X + 26, console.Y, console.X, console.Bottom, Palette.Ink, 3f);
            Gfx.Line(sb, console.Right - 26, console.Y, console.Right, console.Bottom, Palette.Ink, 3f);
            Gfx.Rect(sb, console.X + 26, console.Y, console.Width - 52, 3, Palette.Ink);
            Gfx.Rect(sb, console.X + 40, console.Y + 22, console.Width - 80, 1, Palette.Ink * 0.3f);
            Gfx.Rect(sb, console.X + 30, console.Y + 36, console.Width - 60, 1, Palette.Ink * 0.3f);

            bool active = phase == Phase.Ring && !pressed;
            bool locked = battle.ChoicesLocked;

            for (int k = 0; k < 3; k++)
            {
                Rectangle key = keys[k];
                bool sealedKey = locked && k != 1;
                bool over = active && Input.MouseOver(key);

                Color fill = Color.Lerp(Palette.Paper, Palette.Highlight, keyFlash[k]);
                if (!active && keyFlash[k] <= 0f) fill = Palette.StageLightDeep;
                if (over) fill = Palette.Highlight;
                if (sealedKey) fill = Palette.StageLightDeep * 0.8f;

                Rectangle pushed = key;
                pushed.Y += (int)(keyFlash[k] * 5f);

                //Key Body : rounded by a circle at each corner
                Gfx.Rect(sb, pushed, Palette.Ink);
                Rectangle face = pushed;
                face.Inflate(-2, -2);
                Gfx.Rect(sb, face, fill);
                Gfx.Rect(sb, face.X, face.Bottom - 5, face.Width, 5, Palette.Ink * 0.15f);
                if (active && !sealedKey) Gfx.DrawGlowBox(sb, new Rectangle(key.X - 10, key.Y - 16, key.Width + 20, 30), Palette.Highlight * 0.3f);

                Gfx.TextSpaced(sb, Game.Font, sealedKey ? "SEALED" : keyWord[k], face.X + 6, face.Y + 4, Palette.InkSoft, TextSize.Tiny, 1.5f);
                Gfx.TextRight(sb, Game.BigFont, keyLetter[k], face.Right - 6, face.Y + 12, Palette.Ink, TextSize.Small);

                //Key Icon
                float cx = face.Center.X - 10;
                float cy = face.Center.Y + 6;
                Color ink = sealedKey ? Palette.InkSoft * 0.5f : Palette.Ink;
                if (k == 0)
                {
                    Gfx.Arrow(sb, cx - 7, cy, 8, true, ink);
                    Gfx.Arrow(sb, cx + 7, cy, 8, true, ink);
                }
                else if (k == 1)
                {
                    Gfx.Arrow(sb, cx, cy, 9, true, ink);
                }
                else
                {
                    Gfx.Rect(sb, cx - 11, cy - 5, 22, 3, ink);
                    Gfx.Rect(sb, cx - 11, cy + 2, 22, 3, ink);
                }
            }
        }

        //Ring : the timing circle closing on its mark, sitting on the border
        private void DrawRing(SpriteBatch sb)
        {
            float cx = RingX();
            float cy = RingY;

            Gfx.DrawGlow(sb, cx, cy, 170f, Color.Black * 0.35f);

            //Dial Ticks : twelve small marks around the ring, like a metronome face
            for (int i = 0; i < 12; i++)
            {
                float a = MathHelper.TwoPi * i / 12f;
                float r1 = RingTarget + 22f;
                float r2 = RingTarget + (i % 3 == 0 ? 32f : 27f);
                Gfx.Line(sb, cx + (float)Math.Cos(a) * r1, cy + (float)Math.Sin(a) * r1,
                         cx + (float)Math.Cos(a) * r2, cy + (float)Math.Sin(a) * r2, Palette.Paper * 0.6f, 1.5f);
            }

            //Grade Guides : the mark itself is PERFECT, the two faint rings either side are the
            //edges of GOOD. Anything outside them is a MISS.
            float shrink = (RingStart - RingTarget) / BattleRules.RingTime;
            float good = battle.GoodWindow * shrink;
            Gfx.CircleOutline(sb, cx, cy, RingTarget + good, Palette.Paper * 0.35f, 1f);
            Gfx.CircleOutline(sb, cx, cy, RingTarget - good, Palette.Paper * 0.35f, 1f);
            Gfx.CircleOutline(sb, cx, cy, RingTarget, Palette.Ink, 5f);
            Gfx.CircleOutline(sb, cx, cy, RingTarget, Palette.Highlight, 3f);

            if (phase == Phase.Ring && !pressed)
            {
                //Closing Ring : shrinks to the mark at RingTime, then keeps going a little
                float t = phaseTimer / BattleRules.RingTime;
                float radius = RingStart - (RingStart - RingTarget) * t;
                if (radius < 8f) radius = 8f;

                Gfx.CircleOutline(sb, cx, cy, radius, Palette.Ink, 6f);
                Gfx.CircleOutline(sb, cx, cy, radius, Palette.Highlight, 3f);
            }

            //Centre : a diamond, bright on a beat where somebody plays
            Gfx.Diamond(sb, cx, cy, 10, Palette.Ink);
            Gfx.Diamond(sb, cx, cy, 7, Palette.Highlight);
        }

        //Combo : the counter under the round plate, with pips up to the most it can give
        private void DrawCombo(SpriteBatch sb)
        {
            if (battle.Combo < 2) return;

            float x = 22;
            float y = roundPlate.Bottom + 16;
            float grow = 1f + comboPulse * 0.25f;

            Gfx.SlantBox(sb, new Rectangle(-20, (int)y - 8, 190, 104), -Ui.Slant, Palette.Void * 0.88f);
            Gfx.Rect(sb, 0, y - 8, 3, 104, Palette.Accent);
            Gfx.TextSpaced(sb, Game.Font, "COMBO", x, y, Palette.PaperDim, TextSize.Tiny, 3f);
            Gfx.Text(sb, Game.LogoFont, NumberText.Get(battle.Combo), x - 2, y + 8, Palette.Accent, TextSize.Banner * 0.62f * grow);
            Gfx.Text(sb, Game.Font, comboBonusWords[Math.Min(battle.Combo, BattleRules.ComboMax)], x + 56, y + 38, Palette.Paper, TextSize.Body);
            Ui.Pips(sb, x + 4, y + 82, Math.Min(battle.Combo, BattleRules.ComboMax), BattleRules.ComboMax, 4, 13, 1f);
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
    }
}
