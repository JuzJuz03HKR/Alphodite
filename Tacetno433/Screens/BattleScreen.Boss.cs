using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Battle;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //======================================================================================
    //BattleScreen.Boss : BOSS SHOW (Beta1 31 Oct, docs/BETA1_DIRECTION.md 21.9). The user : a boss must look grand, frightening
    //and like a real boss, with cut ins before its attacks and the band swiping them back (USER_BRIEFS 13.50).
    //Every moment is drawn with code (light, lines, words, the camera, the dark) round the boss's own pictures, which are
    //empty frames until the art comes (Content/Art/Battle/Bosses/<key>_cutin.png, ASSET_LIST). No rule lives here, the rules
    //are in Battle/TurnBattle.cs (PlanBig, TurnOver).
    //   ENTRANCE    the fight's start : the dark swallows the stage, the boss rises out of the hole, its name is written on five
    //               lines like the title of a piece, its line and its rule under it, then its life bar slides in
    //   THE TELL    the turn its BIG ATTACK shows : a band comes in from the right with the attack's name and what it does, then
    //               a ring round the boss closes slowly while the turn is written (the game's timing ring, as a warning)
    //   THE STRIKE  its BIG ATTACK : a cut in from the right, the boss rushing at the band, then its notes to swipe back
    //   PRE         before each plain attack : the boss rises and leans in, the dark flares, the camera turns to it
    //   THE TURN    its life falls to half : the frame turns negative, the half mark breaks, its second line, PHASE 2
    //   BOSS BAR    its life, long, at the top right, with the half mark, instead of the small plate over it
    //What each is made from (inspiration only, never copied, 21.2) : the band answering every hit of an attack it can read
    //(Expedition 33), a sign before a special attack (Sekiro, Zenless Zone Zero : here the ring and the name, not a colour,
    //the world is black and white), a stop between the two halves (Honkai: Star Rail), the boss known early (Slay the Spire).
    //======================================================================================
    public partial class BattleScreen
    {
        private enum BossMoment { None, Entrance, Tell, Turn }

        private const string WordOwnFoot = "ITS OWN FOOT";       // TE DEUM : the staff came down on its own foot

        private Fighter boss;                  // the boss of this fight, null when there is none
        private BossMoment bossMoment;
        private float bossClock;               // seconds into the moment
        private float bossLength;              // how long it lasts
        private bool bossThenStep;             // it came in the middle of NextStep, which goes on when it ends
        private bool bossTurnWaiting;          // THE TURN : the rules said half life, it plays at the next step
        private int bossToldAt = -1;           // THE TELL : the count of its attacks when its BIG ATTACK was last told
        private bool bossCut;                  // THE STRIKE or PRE : the CUT-IN running now (cutIn) is the boss's
        private bool bossCutBig;               //   THE STRIKE (true) or PRE (false)
        private float bossCutLength;
        private float bossBarIn;               // BOSS BAR : 0 hidden .. 1 in place
        private float bossHalfBreak;           // THE TURN : the half mark breaking, 1 = just now
        private Texture2D bossPicture;         // the cut in's picture, null for an empty frame

        //Boss Plate : where its marks go, on the screen under the BOSS BAR. Its next move is the row just above it (IntentRow :
        //22 over its top), its STUN, BURN and CUT IN icons the row under it, from the bar's left.
        private static readonly Rectangle BossPlate = new Rectangle(BossBarLeft, 150, BossBarWidth, 0);
        private const int BossBarLeft = 560;
        private const int BossBarWidth = 540;
        private static readonly string DecreeSkills = "DECREE   NO SKILLS";
        private static readonly string DecreeAttacks = "DECREE   NO ATTACKS";

        //Words : made in LoadBoss, never in Draw
        private string bossName = "";
        private string bossTitle = "";
        private string bossRule = "";
        private string bossBigLine = "";
        private string bossPhaseLine = "";
        private string bossMovement = "";

        //Load Boss : the boss of the fight, its words and picture, and the ENTRANCE (not in PRACTICE)
        private void LoadBoss()
        {
            boss = null;
            for (int i = 0; i < battle.Enemies.Count; i++)
                if (battle.Enemies[i].Enemy.Kind == EnemyKind.Boss) boss = battle.Enemies[i];
            if (boss == null) return;

            Enemy e = boss.Enemy;
            bossName = e.Name;
            bossTitle = e.Title;
            bossRule = e.TraitName.Length > 0 ? e.TraitName + "   " + e.TurnTraitText : "";
            bossBigLine = e.BigName.Length > 0 ? Words.BigAttack + "   " + e.BigName : "";
            bossPhaseLine = e.PhaseLine;
            bossMovement = run != null ? MovementList.Names[Math.Max(0, Math.Min(MovementList.Names.Length - 1, run.Movement))] : "";
            bossPicture = FrameArt.Picture(Game.GraphicsDevice, "Bosses", ArtBank.Key(e.Name) + "_cutin");
            bossBarIn = 0f;
            StartBoss(BossMoment.Entrance, BattleRules.BossEntranceTime, false);
            actionCam.HoldFor(BossAt(), BattleRules.CamZoomMost * 0.5f, BattleRules.BossEntranceTime * 0.75f);
        }

        //Start Boss : a moment begins. then : NextStep was stopped for it and goes on afterwards.
        private void StartBoss(BossMoment moment, float length, bool then)
        {
            bossMoment = moment;
            bossClock = 0f;
            bossLength = length;
            bossThenStep = then;
        }

        //Update Boss : the moments' clocks. True while a moment holds the fight (no input, no next step).
        private bool UpdateBoss(float dt)
        {
            if (boss == null) return false;
            if (bossHalfBreak > 0f) bossHalfBreak = Math.Max(0f, bossHalfBreak - dt * 0.8f);
            bool entering = bossMoment == BossMoment.Entrance && bossClock < bossLength - 0.7f;
            if (!entering) bossBarIn = Math.Min(1f, bossBarIn + dt * 2.5f);
            if (bossMoment == BossMoment.None) return false;

            bossClock += dt;
            if (bossMoment == BossMoment.Entrance && bossClock > BattleRules.BossEntranceSkip && Input.MouseClicked())
                bossClock = Math.Max(bossClock, bossLength - 0.5f);                    // a click : on to its end
            if (bossClock < bossLength) return true;

            bool then = bossThenStep;
            bossMoment = BossMoment.None;
            bossThenStep = false;
            if (then) NextStep();
            return false;
        }

        //Boss Tell : THE TELL, when the turn being written is the one its BIG ATTACK comes in (once for each BIG ATTACK)
        private void BossTell()
        {
            if (boss == null || boss.Down || !boss.IntentBig || bossToldAt == boss.Attacks) return;
            bossToldAt = boss.Attacks;
            StartBoss(BossMoment.Tell, BattleRules.BossTellTime, false);
            actionCam.HoldFor(BossAt(), BattleRules.CamZoomMost * 0.35f, BattleRules.BossTellTime);
        }

        //Boss Turn First : THE TURN plays before the next step when the boss's life has just fallen to half
        private bool BossTurnFirst()
        {
            if (!bossTurnWaiting || boss == null) return false;
            bossTurnWaiting = false;
            if (boss.Down || battle.Step == TurnStep.Won || battle.Step == TurnStep.Lost) return false;
            StartBoss(BossMoment.Turn, BattleRules.BossTurnTime, true);
            hits.ImpactFrame(BossAt());                                                  // the frame turns negative
            actionCam.HoldFor(BossAt(), BattleRules.CamZoomMost * 0.7f, BattleRules.BossTurnTime * 0.6f);
            actionCam.Shake(BattleRules.CamShake * 3f);
            dark.CrackAll(6, 1f);
            return true;
        }

        //Boss Cut : THE STRIKE before a BIG ATTACK's notes, PRE before a plain attack's (the ring waits, CUT-IN)
        private void BossCut(Fighter enemy)
        {
            bossCut = false;
            if (enemy == null || enemy != boss) return;
            bossCut = true;
            bossCutBig = enemy.IntentBig;
            bossCutLength = bossCutBig ? BattleRules.BossStrikeTime : BattleRules.BossPreTime;
            cutIn = bossCutLength;
            actionCam.HoldFor(BossAt(), BattleRules.CamZoomMost * (bossCutBig ? 0.6f : 0.3f), bossCutLength);
            if (bossCutBig)
            {
                hits.ImpactFrame(BossAt());
                actionCam.Shake(BattleRules.CamShake * 2f);
            }
        }

        //Boss Blocked : every note of its BIG ATTACK blocked, its words over it (the rules threw it off the beat)
        private void BossBlocked(float x, Rectangle box)
        {
            if (boss == null) return;
            effects.SpawnPop(x, box.Y - 110, boss.Enemy.BigWin, Palette.Highlight, 1.3f);
            hits.ImpactFrame(new Vector2(box.Center.X, box.Center.Y));
            actionCam.Punch(BattleRules.CamFinishPunch * 1.5f);
        }

        //Boss At : the middle of the boss on the screen
        private Vector2 BossAt()
        {
            if (boss == null) return new Vector2(HitX, HitY);
            Rectangle r = BoxOf(boss);
            return new Vector2(r.Center.X, r.Center.Y);
        }

        //Boss Dark : how much more of the stage the dark takes during a moment (DarkShare adds it)
        private float BossDark()
        {
            if (boss == null) return 0f;
            float k = 0f;
            if (bossMoment == BossMoment.Entrance) k = Bell(bossClock / bossLength, 0.15f, 0.8f);
            if (bossMoment == BossMoment.Turn) k = Bell(bossClock / bossLength, 0.05f, 0.85f);
            if (bossMoment == BossMoment.Tell) k = 0.5f * Bell(bossClock / bossLength, 0.1f, 0.8f);
            if (bossCut && cutIn > 0f) k = (bossCutBig ? 1f : 0.5f) * Bell(1f - cutIn / bossCutLength, 0.1f, 0.7f);
            return k * BattleRules.BossDarkRise;
        }

        //Bell : 0 at the start, up to 1 by "up" (a share of the moment), held, back down to 0 from "down" to the end
        private static float Bell(float t, float up, float down)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            if (t < up) return Smooth(t / up);
            if (t > down) return 1f - Smooth((t - down) / (1f - down));
            return 1f;
        }

        //Boss Pose : where the boss is drawn during a moment. ENTRANCE : it rises out of the hole and grows to its size.
        //PRE and THE STRIKE : it rears up and leans in. THE TURN : it shudders.
        private Rectangle BossPose(Rectangle r)
        {
            float rise = 0f, grow = 1f, shiver = 0f;
            if (bossMoment == BossMoment.Entrance)
            {
                float t = MathHelper.Clamp((bossClock - 0.45f) / 1.0f, 0f, 1f);
                float e = 1f - (1f - t) * (1f - t) * (1f - t);                         // ease out : fast, then it settles
                rise = (1f - e) * BattleRules.BossRise;
                grow = 0.82f + 0.18f * e;
            }
            if (bossCut && cutIn > 0f)
            {
                float p = 1f - cutIn / bossCutLength;
                float rear = (float)Math.Sin(MathHelper.Clamp(p * 1.2f, 0f, 1f) * MathHelper.Pi);
                grow *= 1f + (bossCutBig ? 0.1f : 0.06f) * rear;
                rise -= (bossCutBig ? 26f : 14f) * rear;
            }
            if (bossMoment == BossMoment.Turn && bossClock < 1.2f) shiver = (float)Math.Sin(time * 70f) * 6f * (1f - bossClock / 1.2f);
            if (rise == 0f && grow == 1f && shiver == 0f) return r;
            int w = (int)(r.Width * grow);
            int h = (int)(r.Height * grow);
            return new Rectangle(r.Center.X - w / 2 + (int)shiver, r.Bottom - h + (int)rise, w, h);
        }

        //======================================================================================
        //Drawing
        //======================================================================================

        //Draw Boss Show : the moments on top of everything (the CUT-IN of THE STRIKE is drawn by DrawCutIn)
        private void DrawBossShow(SpriteBatch sb)
        {
            if (boss == null) return;
            if (bossMoment == BossMoment.Entrance) DrawEntrance(sb);
            else if (bossMoment == BossMoment.Tell) DrawTell(sb);
            else if (bossMoment == BossMoment.Turn) DrawTurn(sb);
        }

        //ENTRANCE : five lines drawn across the screen like the staff of a piece, the boss's name written on them, its line
        //and its rule under them. The screen goes dark round them first, and everything slides away at the end.
        private void DrawEntrance(SpriteBatch sb)
        {
            float t = bossClock;
            float dim = Bell(t / bossLength, 0.12f, 0.82f);
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Color.Black * (0.55f * dim));

            //The flash as it rises (LESS FLASHING turns it down)
            float flash = 1f - Math.Abs(t - 0.95f) / 0.12f;
            if (flash > 0f) Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Color.White * (0.45f * flash * Flashing));

            float write = MathHelper.Clamp((t - 1.0f) / 0.7f, 0f, 1f);
            float leave = MathHelper.Clamp((t - (bossLength - 0.7f)) / 0.6f, 0f, 1f);
            if (write <= 0f) return;
            float a = Smooth(write) * (1f - leave);
            float slide = Smooth(leave) * -TacetGame.ScreenW * 0.6f;

            //Five Lines : from the left, one after another
            float cx = TacetGame.ScreenW / 2f;
            const float top = 286f;
            for (int i = 0; i < 5; i++)
            {
                float grow = Smooth(MathHelper.Clamp((t - 1.0f - i * 0.06f) / 0.6f, 0f, 1f));
                float width = 980f * grow;
                Gfx.Rect(sb, cx - 490f + slide, top + i * 14f, width, 2f, Palette.Paper * (0.55f * a));
            }

            //Name : on the lines, its letters spaced wide
            Gfx.Rect(sb, cx - 470f + slide, top - 6f, 940f, 70f, Color.Black * (0.35f * a));
            Gfx.TextSpacedCentered(sb, Game.LogoFont, bossName, cx + slide, top - 6f, Palette.Highlight * a, TextSize.Banner * 0.8f, 10f);
            if (bossMovement.Length > 0)
                Gfx.TextSpacedCentered(sb, Game.Font, bossMovement, cx + slide, top - 40f, Palette.PaperDim * a, TextSize.Label, 8f);

            float under = Smooth(MathHelper.Clamp((t - 1.6f) / 0.5f, 0f, 1f)) * (1f - leave);
            Gfx.TextCentered(sb, Game.StoryFont, bossTitle, cx + slide, top + 92f, Palette.Paper * under, TextSize.Story);
            if (bossRule.Length > 0) Gfx.TextSpacedCentered(sb, Game.Font, bossRule, cx + slide, top + 128f, Palette.PaperDim * under, TextSize.Label, 1f);
            if (bossBigLine.Length > 0) Gfx.TextSpacedCentered(sb, Game.Font, bossBigLine, cx + slide, top + 152f, Palette.Highlight * under, TextSize.Label, 3f);
            if (t > BattleRules.BossEntranceSkip && t < bossLength - 0.6f)
                Gfx.TextSpacedCentered(sb, Game.Font, "CLICK", cx, 680f, Palette.LineGrey * (0.6f * under), TextSize.Tiny, 4f);
        }

        //THE TELL : a slanted band from the right with the boss's picture, the tag BIG ATTACK, the name and its line
        private void DrawTell(SpriteBatch sb)
        {
            float p = bossClock / bossLength;
            float slideIn = Smooth(MathHelper.Clamp(p / 0.15f, 0f, 1f));
            float slideOut = MathHelper.Clamp((p - 0.82f) / 0.18f, 0f, 1f);
            slideOut *= slideOut;
            float offset = (1f - slideIn) * 1400f - slideOut * 1400f;
            float a = slideIn * (1f - slideOut);

            Rectangle band = new Rectangle((int)(offset - 60), 214, TacetGame.ScreenW + 120, 132);
            Gfx.SlantBox(sb, new Rectangle(band.X + 8, band.Y + 8, band.Width, band.Height), 50, Color.Black * (0.5f * a));
            Gfx.SlantBox(sb, band, 50, Palette.Void * (0.92f * a));
            Gfx.Rect(sb, band.X, band.Y + 5, band.Width, 2, Palette.Paper * (0.7f * a));
            Gfx.Rect(sb, band.X, band.Bottom - 7, band.Width, 2, Palette.Paper * (0.7f * a));

            //Its picture on the right, taller than the band (an empty frame until the art)
            Rectangle portrait = new Rectangle((int)(offset + 930), 170, 230, 220);
            Gfx.Rect(sb, portrait, Palette.Void * a);
            if (bossPicture != null) sb.Draw(bossPicture, portrait, Color.White * a);
            else ArtSlot.Draw(sb, portrait, Palette.Paper, 0.8f * a);

            int x = (int)offset + 150;
            Ui.Tag(sb, Words.BigAttack, x, 234, true, a);
            Gfx.TextSpaced(sb, Game.Font, bossName, x + Ui.TagWidth(Words.BigAttack) + 16, 239, Palette.PaperDim * a, TextSize.Tiny, 4f);
            Gfx.Text(sb, Game.LogoFont, boss.Enemy.BigName, x - 4, 256, Palette.Highlight * a, TextSize.Banner * 0.62f);
            Gfx.Text(sb, Game.StoryFont, boss.Enemy.BigText, x, 312, Palette.Paper * a, TextSize.StorySmall);
        }

        //THE TURN : PHASE 2 and its second line on a dark band across the middle
        private void DrawTurn(SpriteBatch sb)
        {
            float p = bossClock / bossLength;
            float a = Bell(p, 0.18f, 0.82f);
            float flash = 1f - bossClock / 0.15f;
            if (flash > 0f) Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Color.White * (0.5f * flash * Flashing));
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Color.Black * (0.35f * a));
            Gfx.Rect(sb, 0, 288, TacetGame.ScreenW, 104, Palette.Void * (0.85f * a));
            Gfx.Rect(sb, 0, 290, TacetGame.ScreenW * a, 1, Palette.Paper * (0.6f * a));
            Gfx.Rect(sb, TacetGame.ScreenW * (1f - a), 389, TacetGame.ScreenW * a, 1, Palette.Paper * (0.6f * a));
            float cx = TacetGame.ScreenW / 2f;
            Ui.Tag(sb, Words.PhaseTwo, (int)(cx - Ui.TagWidth(Words.PhaseTwo) / 2f), 300, true, a);
            Gfx.TextCentered(sb, Game.BigFont, bossPhaseLine, cx, 352, Palette.Highlight * a, TextSize.Title);
        }

        //THE STRIKE / PRE (drawn in place of the SIGNATURE's CUT-IN while bossCut) : THE STRIKE is the SIGNATURE's cut in turned
        //round, coming from the right because the boss rushes at the band, black and grey with its picture breaking out of
        //the band. PRE is only the dark flaring and a thin streak across, the boss itself rears up (BossPose).
        private void DrawBossCut(SpriteBatch sb)
        {
            float t = 1f - cutIn / bossCutLength;
            if (!bossCutBig)
            {
                float s = (float)Math.Sin(MathHelper.Clamp(t, 0f, 1f) * MathHelper.Pi);
                Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Color.Black * (0.3f * s));
                Vector2 at = BossAt();
                Gfx.Rect(sb, 0, at.Y - 1f, TacetGame.ScreenW * s, 2f, Palette.Paper * (0.35f * s));
                return;
            }

            float slideIn = Smooth(t / 0.2f);
            float slideOut = MathHelper.Clamp((t - 0.82f) / 0.18f, 0f, 1f);
            slideOut *= slideOut;
            float offset = (1f - slideIn) * 1500f - slideOut * 1500f;
            float dim = slideIn * (1f - slideOut);
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Color.Black * (0.72f * dim));

            int x = (int)offset;
            Rectangle band = new Rectangle(x - 80, 262, TacetGame.ScreenW + 160, 196);
            Gfx.SlantBox(sb, new Rectangle(band.X - 10, band.Y + 10, band.Width, band.Height), -70, Color.Black * 0.6f);
            Gfx.SlantBox(sb, band, -70, Palette.Void);
            Gfx.Rect(sb, band.X, band.Y + 6, band.Width, 2, Palette.Paper * 0.8f);
            Gfx.Rect(sb, band.X, band.Bottom - 8, band.Width, 2, Palette.Paper * 0.8f);

            //Speed Lines : rushing to the left, at the band
            for (int i = 0; i < 16; i++)
            {
                float ly = band.Y + 20 + (i * 47) % (band.Height - 40);
                float lx = TacetGame.ScreenW + 200 - (time * 2800f + i * 197f) % (TacetGame.ScreenW + 400) + offset;
                Gfx.Rect(sb, lx, ly, 90 + (i % 3) * 80, 2, Palette.Paper * 0.2f);
            }

            //Portrait : on the right, breaking out of the band top and bottom
            Rectangle portrait = new Rectangle(x + 820, 160, 360, 400);
            Gfx.Rect(sb, portrait, Palette.Void);
            if (bossPicture != null) sb.Draw(bossPicture, portrait, Color.White * dim);
            else ArtSlot.Draw(sb, portrait, Palette.Paper, dim);

            //Shards : streaks thrown out of the portrait
            float cx = portrait.Center.X;
            float cy = portrait.Center.Y - 20;
            for (int i = 0; i < 16; i++)
            {
                float angle = i * 2.4f;
                float reach = 90f + t * (380f + (i % 5) * 90f);
                Vector2 d = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle) * 0.7f);
                Vector2 head = new Vector2(cx, cy) + d * reach;
                Gfx.Line(sb, head - d * 26f, head, Palette.Paper * (dim * (1f - t * 0.6f)), 2f);
            }

            //Names : BIG ATTACK, the boss, the attack's name big
            int nx = x + 90;
            Ui.Tag(sb, Words.BigAttack, nx, 292, true, dim);
            Gfx.TextSpaced(sb, Game.Font, bossName, nx + Ui.TagWidth(Words.BigAttack) + 16, 297, Palette.PaperDim * dim, TextSize.Tiny, 4f);
            Gfx.Text(sb, Game.LogoFont, boss.Enemy.BigName, nx - 4, 330, Palette.Highlight * dim, TextSize.Banner * 0.85f);
            Gfx.Text(sb, Game.StoryFont, boss.Enemy.BigText, nx, 404, Palette.Paper * dim, TextSize.StorySmall);

            //Flash : the first instant is pure white (turned down by LESS FLASHING)
            float flash = 1f - t / 0.1f;
            if (flash > 0f) Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Color.White * (0.8f * flash * Flashing));
        }

        //Boss Ring (camera batch) : while the turn its BIG ATTACK comes in is written, a ring round the boss closes slowly,
        //again and again, like the timing ring of a note : it is coming this turn
        private void DrawBossRing(SpriteBatch sb, bool planning)
        {
            if (boss == null || boss.Down || !boss.IntentBig || !planning || bossMoment == BossMoment.Tell) return;
            Rectangle r = BoxOf(boss);
            float cx = r.Center.X;
            float cy = r.Center.Y;
            float size = Math.Min(r.Width, r.Height) * 0.62f;
            float cycle = (time * 0.5f) % 1f;
            float close = 1f - cycle;
            Gfx.CircleOutline(sb, cx, cy, size * (1f + 0.8f * close), Palette.Highlight * (0.5f * cycle), 2f);
            Gfx.CircleOutline(sb, cx, cy, size, Palette.Paper * (0.35f + 0.15f * (float)Math.Sin(time * 4f)), 1f);
        }

        //BOSS BAR : the boss's life, long, at the top right, its name and life over it, the half mark (broken after THE TURN)
        //and the STUN bar under it. In place of its small plate (DrawEnemyPlates leaves it out).
        private void DrawBossBar(SpriteBatch sb)
        {
            if (boss == null || bossBarIn <= 0f) return;
            float a = Smooth(bossBarIn);
            const int left = BossBarLeft;
            const int width = BossBarWidth;
            int y = 62 - (int)((1f - a) * 20f);
            float right = left + width;

            Gfx.TextSpaced(sb, Game.BigFont, bossName, left, y, Palette.Highlight * a, TextSize.Small, 3f);
            float after = left + Gfx.SpacedWidth(Game.BigFont, bossName, TextSize.Small, 3f) + 14f;
            if (boss.SecondHalf)
            {
                Ui.Tag(sb, Words.PhaseTwo, (int)after, y + 2, true, a);
                after += Ui.TagWidth(Words.PhaseTwo) + 14f;
            }
            if (battle.Decree != DecreeKind.None)                                        // DECREE (THE DEAF CROWN) : what this turn forbids,
                Gfx.TextSpaced(sb, Game.Font, battle.Decree == DecreeKind.NoSkills ? DecreeSkills : DecreeAttacks, left + 104f, BossPlate.Y + 5,   // on the
                               Palette.Accent * (a * (0.75f + 0.25f * (float)Math.Sin(time * 5f))), TextSize.Label, 2f);                            // icons' row
            float maxW = Gfx.TextWidth(Game.Font, maxLabel[boss], TextSize.Tiny);
            Gfx.TextRight(sb, Game.Font, maxLabel[boss], right, y + 6, Palette.LineGrey * a, TextSize.Tiny);
            Gfx.TextRight(sb, Game.Font, NumberText.Get(boss.Hp), right - maxW - 6f, y + 2, Palette.Highlight * a, TextSize.Body);

            Rectangle bar = new Rectangle(left, y + 28, width, 8);
            Gfx.Rect(sb, new Rectangle(bar.X - 1, bar.Y - 1, bar.Width + 2, bar.Height + 2), Palette.Void * (0.8f * a));
            Gfx.Bar(sb, bar, boss.Hp / (float)Math.Max(1, boss.MaxHp), Palette.Stage * a, Palette.Paper * a);
            if (!boss.Down) DrawLifePreview(sb, bar, boss, LossOf(boss), 0, a);

            //Half Mark : a diamond at half life (three quarters on ENCORE 5), broken into two halves flying apart after THE TURN
            float half = bar.X + bar.Width * battle.BigPhase;
            if (!boss.SecondHalf) Gfx.Diamond(sb, half, bar.Center.Y, 6, Palette.Highlight * a);
            else if (bossHalfBreak > 0f)
            {
                float apart = (1f - bossHalfBreak) * 18f;
                Gfx.Diamond(sb, half - apart, bar.Center.Y - apart * 0.5f, 5, Palette.Highlight * (a * bossHalfBreak));
                Gfx.Diamond(sb, half + apart, bar.Center.Y + apart * 0.5f, 5, Palette.Highlight * (a * bossHalfBreak));
            }
            else Gfx.Rect(sb, half - 1f, bar.Y - 3, 2, bar.Height + 6, Palette.LineGrey * a);

            //STUN : the thin bar under it
            Gfx.Bar(sb, new Rectangle(left, bar.Bottom + 4, width, 3), boss.OffBeat / (float)BattleRules.TurnOffBeatMax, Palette.Stage * a, Palette.PaperDim * a);

            //Its next move under the bar (DrawIntentOf)
            if (view != View.Over) DrawIntentOf(sb, boss, (1f - FocusEased) * a);
        }
    }
}
