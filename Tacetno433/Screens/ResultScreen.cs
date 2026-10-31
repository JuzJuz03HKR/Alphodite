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
    //ResultScreen : how the fight ended.
    //
    //WIN  : rewards are handed out once when the page opens (shards, some stamina back), and the run
    //       record is updated. After an elite or a boss a motif choice follows (the turn battle's
    //       normal fights pay shards only, 29 Oct : fighting is fighting).
    //LOSS : the run is over. The curtain call page sums it up.
    //
    //Beta1 screens (28 Oct, docs/SCREENS_LEVEL_UP.md 4.3 and table 5) :
    //   WIN   the page opens on the last blow's IMPACT FRAME (white paper, everything bright inked black), the look the
    //         fight ended on. TACET's hole cracks all round, and at ResultBurstAt it bursts : a frame of white, the camera
    //         punches in, and the page is bright paper with ink thrown out from where the hole was (a burst of light, drawn
    //         the way a manga draws one). Then the numbers count up one after another and the rank slams down.
    //   LOSS  TACET's dark floods the stage floor and creeps in from the edges of the screen (ENGULF) while the band's
    //         light goes out. The words stay crisp on top of it.
    //LESS FLASHING : a win opens straight on the bright page, no turned frame, no white, no punch.
    //A key or a click while the page is still coming in jumps to the end of it, the next one carries on.
    //
    //Beside the numbers of the old rhythm duel sits THE PERFORMANCE : every beat of the fight in a small grid, one row
    //per round. A filled box is a beat the band won, an empty one a beat TACET took. The arrow is
    //the stroke, bigger for a BOOST and smaller for an EASE, and a small diamond marks a PERFECT.
    //It is not there to be studied, just to look back at what was played.
    public class ResultScreen : GameScreen
    {
        private Rectangle continueButton = new Rectangle(490, 630, 300, 52);
        private UiMotion continueMotion = new UiMotion();
        private const int LedgerX = 110;
        private const int LedgerW = 440;
        private const int RowsY = 262;
        private const int SheetX = 660;
        private const int SheetY = 262;
        private const int CellW = 50;
        private const int CellH = 42;

        private static string[] roundNumerals = { "I", "II", "III" };
        private static Flick[] pattern = { Flick.Down, Flick.Left, Flick.Right, Flick.Up };

        //Key : what the marks in the grid mean, two columns under it, left of the rank
        private static string[] keyWords = { "WON", "PERFECT", "MISS", "NO STROKE", "COUNTER", "SPARK", "FERMATA", "ROLL" };
        private const int KeyColumn = 108;
        private const int KeyRow = 18;

        //Result Data : worked out once in Load
        private bool won;
        private bool offerMotif;
        private string enemyLine = "";
        private string rank = "";
        private string rankScaleLabel = "";              // RANK : "SHARDS x2" under the rank, made in Load
        private string[] statNames = new string[6];
        private string[] statValues = new string[6];     // LOSS : the ledger's words
        private int[] statNumbers = new int[6];          // WIN : the numbers that count up
        private bool[] statSigned = new bool[6];
        private int statCount;
        private string footnote = "";

        //Page Timing (seconds since the page opened). A win waits for the burst, a loss starts at once.
        private float time;
        private float start;              // when the words begin to come in
        private float ready;              // when everything is in and the button answers
        private const float RowGap = 0.09f;

        //WIN : the burst of TACET's hole (BattleRules.ResultBurstAt)
        private bool burst;               // it has burst : the page is bright paper from here on
        private bool cracked;
        private bool quiet;               // LESS FLASHING
        private Vector2 holeAt;
        private static readonly Vector2[] shardWay = new Vector2[14];
        private static readonly float[] shardFar = new float[14];
        private const float BurstTime = 0.7f;      // the ink thrown out lasts this long
        private const float RankSlam = 0.32f;      // the rank's slam takes this long

        //LOSS : TACET's hole a little right of where it hung in the fight, so SILENCED stays clear of its rim,
        //and the light on the band that goes out
        private const float DefeatHoleX = 1060f;
        private const float BandLightX = 250f;
        private const float BandLightY = 520f;

        public override void Load()
        {
            RunState run = Game.CurrentRun;
            BattleState battle = run.Battle;

            won = battle.PlayerWon;
            enemyLine = battle.EnemyTitle;
            rank = battle.Rank();
            quiet = Settings.ReduceFlashing;
            holeAt = new Vector2(BattleRules.StageVoidX, BattleRules.StageVoidY);

            if (won) ApplyWin(run, battle);
            else PrepareLoss(run, battle);

            //Burst Pieces : where each bit of ink flies, the same every time (the sine hash, like TacetField)
            for (int i = 0; i < shardWay.Length; i++)
            {
                float angle = MathHelper.TwoPi * (i + Hash(i, 3) * 0.7f) / shardWay.Length;
                shardWay[i] = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                shardFar[i] = 160f + 300f * Hash(i, 7);
            }

            //Look : a win starts on the battle's dark stage seen through the IMPACT FRAME, a loss on the dark stage itself
            Dark = new TacetVoid();
            if (won)
            {
                start = quiet ? 0.1f : BattleRules.ResultBurstAt + 0.1f;
                Lens.Ambient = 0.36f;
                Lens.Vignette = 0.8f;
                Lens.Impact.Turn = quiet ? 0f : 1f;
                Lens.Impact.Center = holeAt / new Vector2(TacetGame.ScreenW, TacetGame.ScreenH);
                if (quiet) Burst();
                ready = start + 0.35f + statCount * RowGap + 0.7f;
            }
            else
            {
                start = 0.2f;
                Lens.Ambient = 0.34f;
                Lens.BloomThreshold = BattleRules.StageBloomThreshold;
                Lens.BloomStrength = BattleRules.StageBloomStrength;
                Lens.Vignette = BattleRules.StageVignette;
                ready = start + 0.25f + statCount * RowGap + 0.45f;
            }
        }

        //Win Rewards : handed out exactly once, here
        private void ApplyWin(RunState run, BattleState battle)
        {
            int shards = battle.ShardsEarned();
            int beforeRank = battle.ShardsBeforeRank();
            run.AddShards(shards);
            rankScaleLabel = "SHARDS  x" + battle.RankScale().ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

            float recover = BattleRules.RecoverAfterWin;
            if (run.Has(MotifId.Encore)) recover += BattleRules.EncoreRecover;         // ENCORE
            int before = run.Stamina;
            run.ChangeStamina((int)(run.MaxStamina * recover));
            int recovered = run.Stamina - before;

            //Run Record
            run.BattlesWon++;
            run.PerfectsTotal += battle.PerfectCount;
            if (battle.BestCombo > run.BestCombo) run.BestCombo = battle.BestCombo;

            //Motif Offer : elites and bosses always. The turn battle's normal fights pay shards only (29 Oct, the user : fighting
            //is fighting, BETA1_DIRECTION 8.2), the old duel's sometimes offer one.
            offerMotif = battle.Enemy.Kind != EnemyKind.Normal || (!BattleRules.TurnBattleOn && run.RollPercent() < BattleRules.MotifChanceNormal);
            if (!MotifList.AnyLeft(run.Motifs, run.Roster) && battle.Enemy.Kind != EnemyKind.Boss) offerMotif = false;   // GIFTS : none left for this band

            //Final Boss : the run ends straight after, so there is nothing left to hand out
            bool lastBoss = battle.Enemy.Kind == EnemyKind.Boss && run.Floor >= BattleRules.FloorsPerRun;
            if (lastBoss) offerMotif = false;
            if (lastBoss) run.RehearsalPending = false;                                   // REHEARSAL : no star after the last fight

            //Numbers : TURN BATTLE has no beats or combo. RANK (29 Oct) : the two things the rank is made of come first (the
            //PERFECT share and the life kept, in percent), and the shards are split into the fight's own and what the rank adds,
            //so a player sees why they earned what they earned.
            if (BattleRules.TurnBattleOn)
            {
                int perfectShare = battle.NotesPlayed > 0 ? (int)Math.Round(100f * battle.PerfectCount / battle.NotesPlayed) : 100;
                SetNumber(0, "PERFECT %", perfectShare, false);
                SetNumber(1, BattleRules.LifeWord + " KEPT %", (int)Math.Round(100f * battle.LifeKept), false);
                SetNumber(2, Words.Turns, run.LastRounds, false);
                SetNumber(3, BattleRules.LifeWord + " BACK", recovered, true);
                SetNumber(4, "SHARDS", beforeRank, true);
                SetNumber(5, "RANK BONUS", shards - beforeRank, true);
                statCount = 6;
            }
            else
            {
                SetNumber(0, "PERFECT BEATS", battle.PerfectCount, false);
                SetNumber(1, "BEST COMBO", battle.BestCombo, false);
                SetNumber(2, "FINAL LINE", (int)battle.Line, true);
                SetNumber(3, BattleRules.LifeWord + " BACK", recovered, true);
                SetNumber(4, "SHARDS", shards, true);
                statCount = 5;
            }

            footnote = "";
            if (offerMotif) footnote = "A GIFT IS LEFT BEHIND";
            if (battle.Enemy.Kind == EnemyKind.Boss)                                       // SIGNATURE GROWS : what it becomes on the next floor
                footnote = run.Floor >= BattleRules.FloorsPerRun ? "THE LAST SILENCE IS BROKEN"
                         : (run.Conductor.TurnSignatureGrown.Length > 0 ? "FLOOR CLEARED  -  THE " + Words.Finale + " GROWS : " + run.Conductor.SignatureNameAt(run.Floor)
                                                                        : "FLOOR CLEARED  -  THE NEXT MOVEMENT IS WAITING");

            SoundBank.Play(Sfx.Victory);
            SoundBank.PlayMusic(Music.Victory);
        }

        private void SetNumber(int i, string name, int value, bool signed)
        {
            statNames[i] = name;
            statNumbers[i] = value;
            statSigned[i] = signed;
        }

        //Loss Summary : how far the run got
        private void PrepareLoss(RunState run, BattleState battle)
        {
            statNames[0] = "ENSEMBLE";      statValues[0] = run.BandName;
            statNames[1] = "CONDUCTOR";     statValues[1] = run.Conductor.Name;
            statNames[2] = "REACHED";       statValues[2] = run.StageLabel + "  /  FLOOR " + run.Floor;
            statNames[3] = "FINAL LINE";    statValues[3] = NumberText.Signed((int)battle.Line);
            if (BattleRules.TurnBattleOn) { statNames[3] = Words.Turns; statValues[3] = NumberText.Get(run.LastRounds); }   // TURN BATTLE
            statCount = 4;

            //Run Record : the fight that ended the run counts too (step 15, the curtain call left its PERFECTs out)
            run.PerfectsTotal += battle.PerfectCount;
            if (battle.BestCombo > run.BestCombo) run.BestCombo = battle.BestCombo;

            footnote = battle.Collapsed ? "OUT OF BREATH  -  THE RUN ENDS HERE" : "THE RUN ENDS HERE";
            if (BattleRules.TurnBattleOn) footnote = "THE BAND FELL  -  THE RUN ENDS HERE";        // TURN BATTLE : life, not breath
            run.MarkLastStopLost();
            SaveFile.DeleteRun();          // a lost fight ends the run, there is nothing to continue
            if (BattleRules.TurnBattleOn) Archive.Pay(run, false);   // ARCHIVE : the ECHOES are kept even if the player leaves from here

            SoundBank.Play(Sfx.Defeat);
            SoundBank.PlayMusic(Music.Defeat);
        }

        //======================================================================================
        //Update
        //======================================================================================

        public override void Update(float dt)
        {
            time += dt;
            if (won) UpdateWin(dt);
            else UpdateLoss(dt);
            continueMotion.Update(dt, time >= ready && Input.MouseOver(continueButton));

            //Hurry : a key or a click while the page comes in jumps to the end of it
            bool press = Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Space) || Input.MouseClicked();
            if (time < ready)
            {
                if (press) time = ready;
                return;
            }

            if (Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Space) || Input.ClickedOn(continueButton))
            {
                continueMotion.Press();
                SoundBank.Play(Sfx.UiConfirm);
                Continue();
            }
        }

        //Update Win : the turned frame, the cracks, the burst, then the camera settles
        private void UpdateWin(float dt)
        {
            if (!burst)
            {
                Dark.Update(dt, 0.05f);
                Dark.PlaceOnPage(holeAt, BattleRules.StageVoidRadiusMin, BattleRules.StageVoidY + 60f, BattleRules.StagePoolWideMin, BattleRules.StagePoolDeepMin);
                if (!cracked && time > 0.08f)
                {
                    cracked = true;
                    Dark.CrackAll(12, 1.8f);
                }
                if (time >= BattleRules.ResultBurstAt) Burst();
                return;
            }

            //After The Burst : the white fades, the camera eases back, the smear clears (like the battle's IMPACT FRAME)
            float since = time - BattleRules.ResultBurstAt;
            float settle = MathHelper.Clamp(since / BattleRules.HitImpactSettle, 0f, 1f);
            if (quiet) settle = 1f;
            Lens.Impact.Flash = (1f - settle) * (1f - settle);
            Lens.Impact.Blur = (1f - settle) * 0.6f;
            Lens.Zoom = BattleRules.HitImpactZoom * (1f - UiMotion.EaseOut(settle));

            //RANK : the slam punches the camera once more, a little
            float slam = (time - RankAt) / RankSlam;
            if (!quiet && slam > 0f && slam < 1f)
            {
                Lens.Zoom += 0.025f * (1f - slam) * (1f - slam);
                Lens.ZoomAt = RankCenter;
            }
        }

        //Burst : TACET's hole is gone, the page turns to bright paper
        private void Burst()
        {
            burst = true;
            Dark = null;
            Lens.Impact.Turn = 0f;
            Lens.Ambient = 0.5f;
            Lens.SpotPower = 0f;
            Lens.BloomThreshold = 0.97f;
            Lens.BloomStrength = 0.2f;
            Lens.Vignette = 0.45f;
            Lens.ZoomAt = holeAt;
        }

        //Update Loss : the dark floods the floor and creeps in from the edges
        private void UpdateLoss(float dt)
        {
            float grow = UiMotion.EaseInOut(MathHelper.Clamp(time / BattleRules.ResultDarkTime, 0f, 1f));
            float share = MathHelper.Lerp(0.78f, 1.02f, grow);
            Dark.Update(dt, share);
            float s = MathHelper.Clamp(Dark.Share, 0f, 1.1f);
            Dark.PlaceOnPage(new Vector2(DefeatHoleX, BattleRules.StageVoidY - 10f),
                             MathHelper.Lerp(BattleRules.StageVoidRadiusMin, BattleRules.StageVoidRadiusMax, s),
                             BattleRules.StageVoidY + 60f,
                             MathHelper.Lerp(BattleRules.StagePoolWideMin, BattleRules.StagePoolWideMax, s),
                             MathHelper.Lerp(BattleRules.StagePoolDeepMin, BattleRules.StagePoolDeepMax, s));
            float creep = UiMotion.EaseInOut(MathHelper.Clamp((time - 0.4f) / BattleRules.ResultDarkTime, 0f, 1f));
            Lens.Engulf = BattleRules.ResultEngulf * creep;
        }

        //Continue : the reward choice first, then the next stage
        private void Continue()
        {
            if (!won)
            {
                Game.Screens.Change(new CurtainCallScreen(false));
                return;
            }

            //REHEARSAL (Beta1 31 Oct) : the band rehearses first (a star for one button, RehearsalScreen), the motif after it
            RunState run = Game.CurrentRun;
            run.MotifAfterRehearsal = offerMotif;
            if (run.RehearsalWaiting)
                Game.Screens.Change(new RehearsalScreen(RehearsalScreen.From.Fight));
            else if (offerMotif)
                Game.Screens.Change(new MotifRewardScreen());
            else
                RunFlow.NextStage(Game);
        }

        //Timeline : when each piece of the page comes in (seconds)
        private float RowAt(int i)
        {
            return start + 0.35f + i * RowGap;
        }

        private float RankAt
        {
            get { return start + 0.35f + statCount * RowGap + 0.15f; }
        }

        private static readonly Vector2 RankCenter = new Vector2(1050f, 548f);

        //======================================================================================
        //The World (THE LENS)
        //======================================================================================

        public override bool HasWorld
        {
            get { return true; }
        }

        public override bool ShowsGrain
        {
            get { return false; }
        }

        //Back : the dark stage TACET's dark lies on. Before a win's burst, and all through a loss.
        public override void DrawBack(SpriteBatch sb)
        {
            int w = TacetGame.ScreenW;
            int h = TacetGame.ScreenH;
            float floor = BattleRules.StageVoidY + 60f;
            Gfx.Rect(sb, 0, 0, w, h, Palette.StageDeep);
            Gfx.DrawGlowBox(sb, new Rectangle(-200, -260, w + 400, (int)floor + 320), Palette.Paper * 0.05f);

            //Floor : boards getting lighter towards the band, and lines running away to the far wall
            for (int i = 0; i < 12; i++)
            {
                float y = floor + i * (h - floor) / 12f;
                Gfx.Rect(sb, 0, y, w, (h - floor) / 12f + 1f, Palette.Stage * (0.25f + i * 0.05f));
            }
            Gfx.Rect(sb, 0, floor, w, 1, Palette.Paper * 0.12f);
            for (int i = -6; i <= 6; i++)
                Gfx.Line(sb, w / 2f + i * 70f, floor, w / 2f + i * 260f, h, Palette.Paper * 0.05f, 1f);
        }

        //World : a win's page after the burst (paper, rays, the thrown ink), or the bits TACET pulls in
        public override void DrawWorld(SpriteBatch sb)
        {
            if (!won || !burst)
            {
                if (Dark != null) Dark.DrawDebris(sb, false);
                return;
            }

            int w = TacetGame.ScreenW;
            int h = TacetGame.ScreenH;
            Gfx.Rect(sb, 0, 0, w, h, Palette.StageLight);
            Gfx.DrawGlow(sb, w / 2f, 200f, 560f, Color.White * 0.8f);
            Ornament.Rays(sb, holeAt.X, holeAt.Y, 120f, 1500f, 36, time * 0.03f, Palette.Ink * 0.05f);
            DrawBurst(sb);
        }

        //Burst : where the hole was, a ring of ink racing out, lines of speed, and bits of ink flung away
        private void DrawBurst(SpriteBatch sb)
        {
            float k = (time - BattleRules.ResultBurstAt) / BurstTime;
            if (quiet) k = (time - start) / BurstTime;
            if (k < 0f || k >= 1f) return;
            float out1 = UiMotion.EaseOut(k);
            float fade = 1f - k;

            Gfx.Ring(sb, holeAt.X, holeAt.Y, 40f + 900f * out1, Palette.Ink * (0.8f * fade), 1f + 3f * fade);

            for (int i = 0; i < 40; i++)
            {
                float angle = MathHelper.TwoPi * (i + Hash(i, 11) * 0.8f) / 40f;
                Vector2 way = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                float inner = 70f + 520f * out1 * (0.6f + 0.4f * Hash(i, 5));
                float length = (60f + 220f * Hash(i, 9)) * fade;
                Gfx.Line(sb, holeAt + way * inner, holeAt + way * (inner + length), Palette.Ink * (0.55f * fade), 1f + (i % 3 == 0 ? 1f : 0f));
            }

            for (int i = 0; i < shardWay.Length; i++)
            {
                Vector2 at = holeAt + shardWay[i] * shardFar[i] * out1;
                Gfx.Diamond(sb, (float)Math.Floor(at.X * 0.5f) * 2f, (float)Math.Floor(at.Y * 0.5f) * 2f, 2f + 5f * fade, Palette.Ink * fade);
            }
        }

        //Lights : a loss lets the band's light flicker and go out. A win's paper is lit as it is (Burst).
        public override void DrawLights(SpriteBatch sb)
        {
            if (won)
            {
                if (!burst) Gfx.DrawGlowBox(sb, new Rectangle(-200, 200, TacetGame.ScreenW + 400, 700), Color.White * 0.3f);
                return;
            }
            float out1 = MathHelper.Clamp(time / 1.6f, 0f, 1f);
            float flicker = Hash((int)(time * 14f), 1) > 0.75f ? 0.55f : 1f;
            float power = MathHelper.Lerp(0.6f, 0.15f, out1) * (out1 < 1f ? flicker : 1f);
            Gfx.DrawGlow(sb, BandLightX, BandLightY, 420f, Color.White * power);
            Gfx.DrawGlowBox(sb, new Rectangle(-200, 260, TacetGame.ScreenW + 400, 600), Color.White * (0.3f * (1f - out1 * 0.5f)));
        }

        //======================================================================================
        //The Words (crisp on top)
        //======================================================================================

        public override void Draw(SpriteBatch sb)
        {
            //Before the burst the page is only the IMPACT FRAME, no words yet
            if (won && !burst) return;

            float cx = TacetGame.ScreenW / 2f;
            Color ink = won ? Palette.Ink : Palette.Paper;
            Color soft = won ? Palette.InkSoft : Palette.PaperDim;

            //Title
            float head = UiMotion.Between(time, start, 0.35f);
            Gfx.TextSpacedCentered(sb, Game.Font, "BATTLE RESULT", cx, 34, soft * head, TextSize.Label, 6f);
            string title = won ? "VICTORY" : "SILENCED";
            float grow = 1f + 0.12f * (1f - head);
            Gfx.TextCentered(sb, Game.LogoFont, title, cx, 130 - (1f - head) * (won ? 0f : 24f), ink * head, TextSize.Logo * 0.9f * Math.Min(grow, 1f / 0.9f));
            float line = UiMotion.Between(time, start + 0.1f, 0.35f);
            Gfx.TextSpacedCentered(sb, Game.Font, enemyLine, cx, 200, soft * line, TextSize.Label, 4f);
            Ornament.OrbitRule(sb, cx, 230, 200f * line, soft * line);

            if (!BattleRules.TurnBattleOn) DrawSheet(sb, Game.CurrentRun.Battle, ink, soft, won, line);   // the turn battle has no beats to show

            if (won) DrawNumbers(sb, ink, soft);
            else DrawLedger(sb, ink, soft);

            //Rank : a letter in a diamond, slammed down under the performance
            if (won) DrawRank(sb, soft);

            //Footnote : plain spaced words between two rings on the bright page, a tag on the dark one
            float noteA = UiMotion.Between(time, RowAt(statCount), 0.35f);
            if (footnote.Length > 0 && noteA > 0f)
            {
                float fy = RowsY + statCount * (won ? 58 : 52) + (won ? 8 : 16);
                if (won)
                {
                    float w = Gfx.SpacedWidth(Game.Font, footnote, TextSize.Label, 3f);
                    Gfx.TextSpacedCentered(sb, Game.Font, footnote, LedgerX + LedgerW / 2f, fy, ink * noteA, TextSize.Label, 3f);
                    Gfx.CircleOutline(sb, LedgerX + LedgerW / 2f - w / 2f - 16, fy + 8, 3f, ink * noteA, 1f);
                    Gfx.CircleOutline(sb, LedgerX + LedgerW / 2f + w / 2f + 16, fy + 8, 3f, ink * noteA, 1f);
                }
                else
                {
                    //Lost : centred under the ledger, clear of THE PERFORMANCE legend on the right
                    Ui.Tag(sb, footnote, LedgerX + (LedgerW - Ui.TagWidth(footnote)) / 2f, fy, true, noteA);
                }
            }

            //Continue Button : black on the bright page, the dark plate with its bright edge on the dark one
            float button = UiMotion.Between(time, ready - 0.3f, 0.3f);
            Ui.Command(sb, continueButton, won ? "CONTINUE" : "CURTAIN CALL", "ENTER", continueMotion, button, won);
        }

        //Numbers : a win's rows, each counting up over its thin line (menu7), one after another
        private void DrawNumbers(SpriteBatch sb, Color ink, Color soft)
        {
            for (int i = 0; i < statCount; i++)
            {
                float a = UiMotion.Between(time, RowAt(i), BattleRules.UiEnterTime);
                if (a <= 0f) continue;
                int shown = UiMotion.Count(statNumbers[i], time, RowAt(i));
                float fill = MathHelper.Clamp((time - RowAt(i)) / BattleRules.UiCountTime, 0f, 1f);
                float x = LedgerX - (1f - a) * BattleRules.UiEnterSlide;
                Ui.Number(sb, statNames[i], shown, statSigned[i], x, RowsY + i * 58, LedgerW, UiMotion.EaseOut(fill), ink, soft, a);
            }
        }

        //Ledger : a loss's rows with dotted leaders, sliding in one after the other
        private void DrawLedger(SpriteBatch sb, Color ink, Color soft)
        {
            for (int i = 0; i < statCount; i++)
            {
                float rowE = UiMotion.Between(time, RowAt(i), BattleRules.UiEnterTime);
                if (rowE <= 0f) continue;
                int y = RowsY + i * 52;
                Gfx.CircleOutline(sb, LedgerX + 4, y + 12, 3f, soft * rowE, 1f);
                Gfx.TextSpaced(sb, Game.Font, statNames[i], LedgerX + 18 - (1f - rowE) * BattleRules.UiEnterSlide, y + 4, soft * rowE, TextSize.Label, 3f);
                float valueW = Gfx.TextWidth(Game.BigFont, statValues[i], TextSize.Subtitle);
                for (int d = LedgerX + 200; d < LedgerX + LedgerW - valueW - 14; d += 8)
                    Gfx.Rect(sb, d, y + 16, 2, 1, soft * (0.6f * rowE));
                Gfx.TextRight(sb, Game.BigFont, statValues[i], LedgerX + LedgerW, y - 6, ink * rowE, TextSize.Subtitle);
            }
        }

        //Rank : big and turned at first, slammed down into place (EaseBack), a ring of ink knocked out of it
        private void DrawRank(SpriteBatch sb, Color soft)
        {
            float k = MathHelper.Clamp((time - RankAt) / RankSlam, 0f, 1f);
            if (time < RankAt) return;
            float land = UiMotion.EaseBack(k);
            float size = 66f * (2.2f - 1.2f * land);
            float rx = RankCenter.X;
            float ry = RankCenter.Y;
            float a = Math.Min(1f, k * 3f);
            Gfx.Diamond(sb, rx, ry, size, Palette.Ink * a);
            Gfx.DiamondOutline(sb, rx, ry, size + 10f, Palette.Ink * (0.5f * a), 1f);
            Gfx.TextCentered(sb, Game.LogoFont, rank, rx, ry - 4, Palette.Paper * a, TextSize.Banner * 0.9f * Math.Min(1f, size / 66f));
            Gfx.TextSpacedCentered(sb, Game.Font, "RANK", rx - 110, ry - 6, soft * a, TextSize.Tiny, 4f);
            if (rankScaleLabel.Length > 0)                                                            // RANK : what it did to the shards
                Gfx.TextSpacedCentered(sb, Game.Font, rankScaleLabel, rx + 130, ry - 6, Palette.Ink * (a * Math.Min(1f, k * 2f)), TextSize.Label, 3f);

            //Knock : the ring of ink as it lands
            float knock = (time - RankAt - RankSlam * 0.6f) / 0.4f;
            if (knock > 0f && knock < 1f)
                Gfx.Ring(sb, rx, ry, 70f + 140f * UiMotion.EaseOut(knock), Palette.Ink * (0.6f * (1f - knock)), 2f);
        }

        //Sheet : the whole fight at a glance, one row of eight beats per round
        private void DrawSheet(SpriteBatch sb, BattleState battle, Color ink, Color soft, bool bright, float e)
        {
            if (battle == null) return;

            Gfx.TextSpaced(sb, Game.Font, "THE PERFORMANCE", SheetX, SheetY, soft * e, TextSize.Label, 4f);
            int rounds = Math.Min(battle.Round, BattleRules.MaxRounds);
            Color back = bright ? Palette.StageLight : Palette.Void;

            for (int r = 0; r < rounds; r++)
            {
                float rowE = MathHelper.Clamp(e * 2f - 0.3f - r * 0.25f, 0f, 1f);
                int y = SheetY + 30 + r * (CellH + 12);
                Gfx.TextRight(sb, Game.BigFont, roundNumerals[r], SheetX + 22, y + 8, soft * rowE, TextSize.Small);

                for (int b = 0; b < BattleRules.BeatsPerRound; b++)
                {
                    int x = SheetX + 34 + b * (CellW + 4) + (b >= 4 ? 10 : 0);    // a gap for the bar line
                    Rectangle cell = new Rectangle(x, y, CellW, CellH);
                    BeatResult mark = battle.Sheet[r][b];
                    DrawSheetCell(sb, cell, mark, b, ink, back, rowE);
                }
            }

            DrawKey(sb, ink, soft, e);
        }

        //Key : a tiny cell for each mark with its word beside it, so the grid can be read
        //without the guide. Placed under the third row, whether or not it was played.
        private void DrawKey(SpriteBatch sb, Color ink, Color soft, float e)
        {
            int top = SheetY + 30 + BattleRules.MaxRounds * (CellH + 12) + 2;
            for (int i = 0; i < keyWords.Length; i++)
            {
                int x = SheetX + 34 + (i % 2) * KeyColumn;
                int y = top + (i / 2) * KeyRow;
                Rectangle cell = new Rectangle(x, y, 18, 14);
                Color mark = ink * (0.8f * e);

                if (i == 0) Gfx.Rect(sb, cell, ink * (0.9f * e));                                         // WON
                else Gfx.RectOutline(sb, cell, ink * (0.45f * e), 1);
                if (i == 1) Gfx.Diamond(sb, cell.Right - 4, cell.Y + 4, 2, mark);                           // PERFECT
                if (i == 2) Gfx.Line(sb, cell.X + 3, cell.Bottom - 3, cell.Right - 3, cell.Y + 3, mark, 1.5f);   // MISS
                if (i == 3) Gfx.CircleOutline(sb, cell.Center.X, cell.Center.Y, 4, mark, 1f);              // NO STROKE
                if (i == 4) Gfx.CircleOutline(sb, cell.X + 5, cell.Y + 5, 3, mark, 1f);                    // COUNTER
                if (i == 5) Gfx.Diamond(sb, cell.X + 4, cell.Bottom - 4, 2, mark);                          // SPARK
                if (i == 6) NoteGlyph.FermataSign(sb, cell.Right - 6, cell.Bottom - 2, 4f, mark);           // FERMATA
                if (i == 7)                                                                                 // ROLL
                    for (int z = 0; z < 3; z++)
                        Gfx.Line(sb, cell.Right - 13 + z * 3, cell.Bottom - 3 - (z % 2) * 3, cell.Right - 10 + z * 3, cell.Bottom - 6 + (z % 2) * 3, mark, 1f);

                Gfx.TextSpaced(sb, Game.Font, keyWords[i], x + 24, y + 1, soft * e, TextSize.Tiny, 1.5f);
            }
        }

        //Sheet Cell : filled when the band won the beat, outlined when TACET did
        private void DrawSheetCell(SpriteBatch sb, Rectangle cell, BeatResult mark, int b, Color ink, Color back, float a)
        {
            if (!mark.Done)
            {
                Gfx.RectOutline(sb, cell, ink * (0.15f * a), 1);          // never played, the fight ended first
                return;
            }
            if (mark.Grade == Grade.None)
            {
                Gfx.Rect(sb, cell.Center.X - 8, cell.Center.Y, 16, 2, ink * (0.4f * a));   // a rest
                return;
            }

            bool beatWon = mark.Push > 0;
            if (beatWon) Gfx.Rect(sb, cell, ink * (0.9f * a));
            Gfx.RectOutline(sb, cell, ink * ((beatWon ? 0.9f : 0.45f) * a), 1);
            Color stroke = beatWon ? back * a : ink * (0.8f * a);

            if (mark.Grade == Grade.Hesitate)
            {
                Gfx.CircleOutline(sb, cell.Center.X, cell.Center.Y, 6, stroke, 1.5f);   // no stroke at all
            }
            else
            {
                float length = mark.PlayerChoice == Choice.Boost ? 26f : (mark.PlayerChoice == Choice.Ease ? 12f : 19f);
                DrawWay(sb, pattern[b % 4], cell.Center.X, cell.Center.Y, length, stroke);
                if (mark.Grade == Grade.Miss) Gfx.Line(sb, cell.X + 6, cell.Bottom - 6, cell.Right - 6, cell.Y + 6, stroke * 0.7f, 1.5f);
            }

            if (mark.Grade == Grade.Perfect) Gfx.Diamond(sb, cell.Right - 7, cell.Y + 7, 3, stroke);
            if (mark.Signature) Gfx.DiamondOutline(sb, cell.Center.X, cell.Center.Y, CellH * 0.62f, stroke, 1.5f);

            //Extras : a ring in the corner for a COUNTER, a small mark low down for the spark of a
            //pair (filled when it landed), a short zigzag for TACET's roll, an arch for its fermata
            if (mark.Counter) Gfx.CircleOutline(sb, cell.X + 7, cell.Y + 7, 4, stroke, 1.5f);
            if (mark.Double)
            {
                bool flicked = mark.GraceGrade == Grade.Perfect || mark.GraceGrade == Grade.Good;
                if (flicked) Gfx.Diamond(sb, cell.X + 7, cell.Bottom - 7, 3, stroke);
                else Gfx.DiamondOutline(sb, cell.X + 7, cell.Bottom - 7, 3, stroke, 1f);
            }
            if (mark.Fermata) NoteGlyph.FermataSign(sb, cell.Right - 12, cell.Bottom - 5, 5f, stroke);
            if (mark.Tremolo)
                for (int z = 0; z < 3; z++)
                    Gfx.Line(sb, cell.Right - 18 + z * 4, cell.Bottom - 5 - (z % 2) * 4, cell.Right - 14 + z * 4, cell.Bottom - 9 + (z % 2) * 4, stroke, 1f);
        }

        //Way : a straight arrow of the given length through (cx, cy)
        private static void DrawWay(SpriteBatch sb, Flick way, float cx, float cy, float length, Color color)
        {
            Vector2 d = Vector2.Zero;
            if (way == Flick.Up) d = new Vector2(0f, -1f);
            if (way == Flick.Down) d = new Vector2(0f, 1f);
            if (way == Flick.Left) d = new Vector2(-1f, 0f);
            if (way == Flick.Right) d = new Vector2(1f, 0f);

            Vector2 centre = new Vector2(cx, cy);
            Vector2 tip = centre + d * (length / 2f);
            Gfx.Line(sb, centre - d * (length / 2f), tip - d * 4f, color, 2f);
            Vector2 c = tip - d * 3f;
            if (way == Flick.Up) Gfx.Triangle(sb, c.X, c.Y, 7, true, color);
            else if (way == Flick.Down) Gfx.Triangle(sb, c.X, c.Y, 7, false, color);
            else Gfx.Arrow(sb, c.X, c.Y, 7, way == Flick.Right, color);
        }

        //Hash : a steady random 0..1 from two whole numbers (the sine trick of TacetField)
        private static float Hash(int a, int b)
        {
            float h = (float)Math.Sin(a * 12.9898f + b * 78.233f) * 43758.547f;
            return h - (float)Math.Floor(h);
        }
    }
}
