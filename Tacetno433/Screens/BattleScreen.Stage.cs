using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Battle;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //BattleScreen Stage (Beta1, 28 Oct) : how the fight looks. The player is the conductor, standing behind the
    //band and looking past them at TACET (first person, like Chaos Zero Nightmare).
    //   FRONT     the musician the conductor points at stands big on the left, seen from behind, one at a time :
    //             the one selected while the bar is written, the one playing while it plays, the one TACET aims
    //             at when it attacks. A new one slides in (SWAP).
    //   THE BAND  everyone on the field as a row of cards under the bar strip (until the user's face GUI)
    //   TACET     the enemies further off on the right, standing on the floor, the front one nearest
    //   THE DARK  a black hole behind the enemies pours TACET's dark over the floor towards the camera, as far as the
    //             balance of life lets it (Core/TacetVoid.cs). A hit throws it and it springs back to its place.
    //   LAYERS    the era's backdrop (far, middle), the stage the band stands on, a dark front layer. Each layer
    //             follows the camera less the further away it is (PARALLAX), so a small camera move reads as depth.
    //   LIGHT     a dark stage lit by lights : a stage light on the front musician, the rift's glow, a flash at each hit
    //   FOCUS     while a ring is up the world goes dark, grey and soft behind it and moves slowly (QTE FOCUS)
    //Every picture is an empty frame until the team's art exists (docs/BATTLE_ART_PLAN.md : pixel art drawn x2).
    //The rules never read anything here, this file only looks at the battle (Battle/TurnBattle.cs).
    public partial class BattleScreen
    {
        //Stage Layout : 1 pixel of the art is 2 pixels here (PIXEL x2)
        private const int Horizon = 300;                    // where the floor meets the far wall
        private const int LayerW = 1408;                    // a backdrop layer, 704 x 360 at x2 : 64 to spare on each side for the camera
        private static readonly Rectangle foreBox = new Rectangle(40, 188, 320, 512);   // the musician in front, 160 x 256 at x2, feet at 700
        private const int ChipX = 24;                       // THE BAND : the row of cards under the bar strip
        private const int ChipY = 66;
        private const int ChipW = 150;
        private const int ChipH = 50;
        private const int ChipStep = 156;

        //Enemy Feet : where each enemy stands for 1 to 4 enemies, the front one first. The floor goes away up to the right.
        private static readonly Vector2[][] enemyFeet =
        {
            new Vector2[] { new Vector2(900f, 528f) },
            new Vector2[] { new Vector2(820f, 528f), new Vector2(1070f, 470f) },
            new Vector2[] { new Vector2(795f, 532f), new Vector2(995f, 482f), new Vector2(1160f, 442f) },
            new Vector2[] { new Vector2(770f, 536f), new Vector2(915f, 496f), new Vector2(1050f, 462f), new Vector2(1180f, 432f) },
        };

        //Stage State
        private Fighter fore;              // FRONT : the musician standing in front
        private Fighter foreNext;          // SWAP : who comes in at the middle of the swap
        private float swap;                // SWAP : 0 = settled, 0..1 while one slides out and the next slides in
        private float worldTime;           // the world's own clock, slower in the QTE FOCUS
        private float focus;               // QTE FOCUS 0..1
        private readonly TacetVoid dark = new TacetVoid();   // THE DARK
        private readonly HitFx hits = new HitFx();           // what a blow looks like where it lands (Core/HitFx.cs)
        private Fighter hitBy;             // who threw the blows being shown (their style), null = the conductor
        private bool hitSignature;         // the blows being shown are the SIGNATURE's (CROSS)
        private Vector2 stageCam;          // the camera of the frame being drawn
        private Vector2 kick;              // the camera's nudge after a hit
        private int era;                   // which era's backdrop
        private string[] layerName = new string[0];   // LAYERS : the words in the empty frames, made once

        //Stage Lights : short flashes of light where something lands, kept in a fixed pool (no garbage)
        private struct StageLight
        {
            public Vector2 At;
            public float Radius;
            public float Power;
            public float Life;
            public float Length;
            public Color Tint;         // round 4 : a blow lights the enemy in the striker's colour
        }
        private StageLight[] stageLights = new StageLight[16];

        //Art : each fighter's pictures and animation clock (Core/FrameArt.cs). No files yet = an empty frame.
        private class Art
        {
            public FrameSet Set;
            public FramePlayer Player;
            public bool Busy;          // a once-only animation (attack, hurt, enter ...) is on, idle waits for it to end
        }
        private Dictionary<Fighter, Art> arts = new Dictionary<Fighter, Art>();
        private Texture2D[] layerArt = new Texture2D[4];   // LAYERS : far, middle, stage floor, front (null = empty frame)
        private static readonly string[] layerFile = { "_far", "_mid", "_stage", "_front" };
        private static readonly string[] musicianAnims = { "idle", "attack", "enter", "skill", "parry", "hurt", "ko", "exit", "victory" };
        private static readonly string[] enemyAnims = { "idle", "attack", "hurt", "death" };

        //Stage Load : called from Arrange, once the battle exists
        private void LoadStage()
        {
            era = run != null ? run.Era : 0;
            string key = EraList.All[Math.Max(0, Math.Min(EraList.All.Length - 1, era))].Name;
            layerName = new string[]
            {
                "FAR  " + key + "  704 x 360",
                "STAGE FLOOR  704 x 360",
                "FRONT MUSICIAN  BACK VIEW  160 x 256",
            };
            fore = null;
            foreNext = null;
            swap = 0f;
            dark.Reset();
            focus = 0f;

            //LAYERS : the era's pictures, if the team has made them (Content/Art/Battle/Stages/<era>_far.png ...)
            string eraKey = ArtBank.Key(key);
            for (int i = 0; i < layerArt.Length; i++) layerArt[i] = FrameArt.Picture(Game.GraphicsDevice, "Stages", eraKey + layerFile[i]);

            arts.Clear();
            for (int i = 0; i < battle.Party.Count; i++) ArtOf(battle.Party[i]);
            for (int i = 0; i < battle.Enemies.Count; i++) ArtOf(battle.Enemies[i]);
        }

        //Art Of : a fighter's pictures, found the first time they are needed (a musician CALLED in later too)
        private Art ArtOf(Fighter f)
        {
            Art art;
            if (arts.TryGetValue(f, out art)) return art;
            art = new Art();
            art.Set = f.IsEnemy ? FrameArt.Load(Game.GraphicsDevice, "Enemies", ArtBank.Key(f.Name), enemyAnims)
                                : FrameArt.Load(Game.GraphicsDevice, "Musicians", ArtBank.Key(f.Name), musicianAnims);
            if (art.Set != null) art.Player.Play(art.Set.Find("idle"), true);
            arts[f] = art;
            return art;
        }

        //Play Art : start one of a fighter's animations (nothing happens when it has no such animation yet)
        private void PlayArt(Fighter f, string name)
        {
            if (f == null) return;
            Art art = ArtOf(f);
            if (art.Set == null) return;
            FrameAnim anim = art.Set.Find(name);
            if (anim == null) return;
            art.Player.Play(anim, true);
            art.Busy = !anim.Loop;
        }

        //Update Arts : every clock moves on. When a once-only animation ends, idle comes back (KO and death hold)
        private void UpdateArts(float dt)
        {
            foreach (KeyValuePair<Fighter, Art> pair in arts)
            {
                Art art = pair.Value;
                if (art.Set == null) continue;
                art.Player.Update(dt);
                if (art.Busy && art.Player.Done && !pair.Key.Down) art.Busy = false;
                if (!art.Busy) art.Player.Play(art.Set.Find("idle"), false);
            }
        }

        //======================================================================================
        //Stage Update
        //======================================================================================

        //Update Stage : frozen = the hit stop is on (the world holds still, the FOCUS still moves)
        private void UpdateStage(float dt, bool frozen)
        {
            float want = view == View.Swipe ? 1f : 0f;
            focus = MoveTowards(focus, want, dt * BattleRules.StageFocusSpeed);
            if (frozen) return;

            float worldDt = dt * MathHelper.Lerp(1f, BattleRules.StageFocusSlow, FocusEased);
            worldTime += worldDt;
            UpdateArts(worldDt);

            //SWAP : the musician pointed at walks in. A change in the middle of a swap just changes who comes in.
            Fighter pointed = Pointed();
            if (fore == null) fore = pointed;
            else if (pointed != null && pointed != fore && swap <= 0f)
            {
                foreNext = pointed;
                swap = 0.0001f;
            }
            else if (swap > 0f && pointed != null) foreNext = pointed;

            if (swap > 0f)
            {
                float before = swap;
                swap += dt / BattleRules.StageSwapTime;
                if (before < 0.5f && swap >= 0.5f && foreNext != null)
                {
                    fore = foreNext;
                    PlayArt(fore, "enter");
                }
                if (swap >= 1f) swap = 0f;
            }

            //THE DARK : a spring towards where the balance of life puts it
            dark.Update(worldDt, DarkShare());

            //Camera : the nudge of a hit dies away quickly
            kick *= (float)Math.Pow(0.0015, worldDt);

            for (int i = 0; i < stageLights.Length; i++)
                if (stageLights[i].Life > 0f) stageLights[i].Life -= worldDt;
        }

        //Focus Eased : the QTE FOCUS eased in and out, so the world dims and comes back softly
        private float FocusEased
        {
            get { return focus * focus * (3f - 2f * focus); }
        }

        private static float MoveTowards(float from, float to, float step)
        {
            if (from < to) return Math.Min(to, from + step);
            return Math.Max(to, from - step);
        }

        //Pointed : who the conductor is pointing at right now
        private Fighter Pointed()
        {
            if ((view == View.Plan || view == View.Target) && selected != null && !selected.IsEnemy) return selected;
            if (view == View.Strike && striker != null && !striker.IsEnemy) return striker;

            Fighter now = battle.Current;
            if (now != null)
            {
                if (!now.IsEnemy) return now;
                Fighter aimed = now.IntentTarget;
                if (aimed != null && !aimed.IsEnemy && !aimed.Down) return aimed;     // TACET's turn : the one it aims at
            }
            if (fore != null && !fore.Down) return fore;
            for (int i = 0; i < battle.Party.Count; i++)
                if (!battle.Party[i].Down) return battle.Party[i];
            return battle.Party.Count > 0 ? battle.Party[0] : null;
        }

        //Dark Share : 0 = TACET beaten, 1 = the band beaten, taken from the life each side has left
        private float DarkShare()
        {
            float band = 0f, bandMax = 0f, foe = 0f, foeMax = 0f;
            for (int i = 0; i < battle.Party.Count; i++)
            {
                band += Math.Max(0, battle.Party[i].Hp);
                bandMax += battle.Party[i].MaxHp;
            }
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                foe += Math.Max(0, battle.Enemies[i].Hp);
                foeMax += battle.Enemies[i].MaxHp;
            }
            float b = bandMax > 0f ? band / bandMax : 0f;
            float e = foeMax > 0f ? foe / foeMax : 0f;
            return (e + 0.001f) / (b + e + 0.002f);
        }

        //Impact On Screen : where the IMPACT FRAME's blow landed, on the screen (blows live in the fighters' layer)
        private Vector2 ImpactOnScreen()
        {
            return hits.ImpactAt - stageCam * BattleRules.StageParallax[3];
        }

        //Signature Style : how the conductor's SIGNATURE lands. LIGHTNING strikes from the sky (THE UNHEARING, round 6),
        //the others cut a CROSS until they get their own.
        private HitStyle SignatureStyle()
        {
            return battle.Kit == TurnKit.Beethoven ? HitStyle.Lightning : HitStyle.Cross;
        }

        //Style Of : how a musician's blows look, by their instrument's family. Strings cut (SLASH), winds blast (SHOT),
        //percussion strikes from above (SMASH). The enemies' blows on the band are TACET's INK.
        private static HitStyle StyleOf(Fighter f)
        {
            if (f == null || f.IsEnemy || f.Musician == null) return HitStyle.Shot;
            if (f.Musician.Family == Family.String) return HitStyle.Slash;
            if (f.Musician.Family == Family.Percussion) return HitStyle.Smash;
            return HitStyle.Shot;
        }

        //Flashing : how strong flashes and shaking are, turned down by LESS FLASHING on the settings page
        private static float Flashing
        {
            get { return Settings.ReduceFlashing ? BattleRules.LessFlash : 1f; }
        }

        //Spot Of : a musician's colours for their blows (Palette spot colours, SpotLook in Data/Musician.cs).
        //The conductor's SIGNATURE and TACET are white for now.
        private static HitColors SpotOf(Fighter f)
        {
            if (f == null || f.IsEnemy || f.Musician == null) return HitColors.Plain;
            if (f.Musician.Spot == SpotLook.Red) return new HitColors(Palette.SpotRed, Palette.SpotRedHot, HitFlair.Flame);
            return new HitColors(Palette.SpotGold, Palette.SpotGoldHot, HitFlair.Sparkle);
        }

        //Stage Hit : a hit lands at a point (where the fighters stand). The dark is thrown (back when the band hits),
        //a shock wave runs out, the camera nudges towards it and a light flashes there.
        private void StageHit(Vector2 at, bool bandHits, float power)
        {
            StageHit(at, bandHits, power, Color.White);
        }

        //Stage Hit In Colour : the flash of light takes the striker's colour (half way to white, so it still reads as light)
        private void StageHit(Vector2 at, bool bandHits, float power, Color light)
        {
            dark.Hit(at - Camera() * BattleRules.StageParallax[3], bandHits, power);

            Vector2 middle = new Vector2(TacetGame.ScreenW / 2f, TacetGame.ScreenH / 2f);
            Vector2 toward = at - middle;
            if (toward.LengthSquared() > 1f) toward.Normalize();
            kick += toward * BattleRules.StageKick * power;

            AddStageLight(at, 150f + 90f * power, BattleRules.StageHitLight * power, BattleRules.StageHitLightTime, Color.Lerp(Color.White, light, 0.5f));
        }

        private void AddStageLight(Vector2 at, float radius, float power, float length, Color tint)
        {
            int slot = 0;
            for (int i = 1; i < stageLights.Length; i++)
                if (stageLights[i].Life < stageLights[slot].Life) slot = i;           // the pool is full : reuse the one nearest its end
            stageLights[slot].At = at;
            stageLights[slot].Radius = radius;
            stageLights[slot].Power = power * Flashing;
            stageLights[slot].Life = length;
            stageLights[slot].Length = length;
            stageLights[slot].Tint = tint;
        }

        //======================================================================================
        //Boxes
        //======================================================================================

        //Chip Box : a card in THE BAND row
        private static Rectangle ChipBox(int slot)
        {
            return new Rectangle(ChipX + slot * ChipStep, ChipY, ChipW, ChipH);
        }

        //Enemy Feet : where one enemy stands
        private Vector2 EnemyFeetOf(int slot)
        {
            int count = Math.Max(1, battle.Enemies.Count);
            Vector2[] row = enemyFeet[Math.Min(count, enemyFeet.Length) - 1];
            if (slot < row.Length) return row[slot];
            return row[row.Length - 1] + new Vector2(80f * (slot - row.Length + 1), -24f * (slot - row.Length + 1));   // more than four
        }

        //Enemy Size : 112 x 128, 144 x 160 or 224 x 224 pixels of art (BATTLE_ART_PLAN), x2 on screen
        private static Point EnemySize(Fighter f)
        {
            if (f.Enemy.Kind == EnemyKind.Boss) return new Point(448, 448);
            if (f.Enemy.Kind == EnemyKind.Elite) return new Point(288, 320);
            return new Point(224, 256);
        }

        //Enemy Box : where an enemy stands, feet on the floor
        private Rectangle EnemyBox(int slot)
        {
            Fighter f = battle.Enemies[slot];
            Vector2 feet = EnemyFeetOf(slot);
            Point size = EnemySize(f);
            return new Rectangle((int)(feet.X - size.X / 2f), (int)(feet.Y - size.Y), size.X, size.Y);
        }

        //======================================================================================
        //Stage Draw : into the pictures of Core/PostChain.cs, before the frame
        //======================================================================================

        public override void PreDraw(SpriteBatch sb)
        {
            if (battle == null) return;
            GraphicsDevice device = Game.GraphicsDevice;
            PostChain.Ensure(device);

            Vector2 cam = Camera();
            stageCam = cam;
            dark.Place(cam, Horizon);

            //BACK : the era's backdrop and the floor, alone
            PostChain.BeginBack(device);
            DrawBackdrop(sb, cam);

            //SCENE : BACK seen through TACET's dark, then the fighters in front of it
            PostChain.BeginScene(device);
            dark.DrawScene(device, sb, PostChain.Back);
            DrawFighters(sb, cam);

            PostChain.BeginLights(device, sb, BattleRules.StageAmbient);
            DrawStageLights(sb, cam);
            PostChain.EndLights(sb);

            //GLOW : the light of the dark, then the effects of the blows
            PostChain.BeginGlowTarget(device);
            dark.DrawGlow(device, sb, PostChain.Back);
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, null, null, null, Layer(cam, 3));
            effects.DrawBlows(sb);
            effects.DrawSparks(sb);
            effects.DrawShards(sb);
            effects.DrawHits(sb);
            effects.DrawSlashes(sb);
            effects.DrawFlares(sb);
            hits.DrawGlow(sb);
            DrawMood(sb);
            sb.End();

            PostChain.ImpactLook impact;
            impact.Turn = hits.Impact;
            impact.Flash = hits.ImpactFlash;
            impact.Blur = hits.ImpactBlur;
            impact.Center = ImpactOnScreen() / new Vector2(TacetGame.ScreenW, TacetGame.ScreenH);
            PostChain.Finish(device, sb, time, FocusEased, dark.Engulf, impact, BattleRules.StageBloomThreshold, BattleRules.StageBloomStrength,
                             BattleRules.StageVignette, BattleRules.StageGrain);
        }

        //Camera : a slow breath, the nudge of the last hit, and the shake
        private Vector2 Camera()
        {
            float drift = BattleRules.StageDriftAmount;
            Vector2 cam = new Vector2((float)Math.Sin(worldTime * 0.37f) * drift, (float)Math.Sin(worldTime * 0.53f + 1.3f) * drift * 0.5f);
            cam += kick;
            if (shake > 0f)
            {
                float amount = shake * shake * 10f * Flashing;
                cam += new Vector2((float)Math.Sin(time * 91f) * amount, (float)Math.Cos(time * 73f) * amount);
            }
            return cam;
        }

        //Layer : the camera for one layer. PARALLAX : 0 far, 1 middle, 2 stage floor, 3 fighters, 4 front
        //ADVANCED PART : SpriteBatch takes a matrix that moves everything drawn in that batch. Moving a far layer
        //less than a near one is all it takes for the eye to see depth.
        private static Matrix Layer(Vector2 cam, int depth)
        {
            float f = BattleRules.StageParallax[depth];
            return Matrix.CreateTranslation(-cam.X * f, -cam.Y * f, 0f);
        }

        //Draw Backdrop : into BACK, the far and middle layers and the stage floor
        private void DrawBackdrop(SpriteBatch sb, Vector2 cam)
        {
            int left = (TacetGame.ScreenW - LayerW) / 2;

            //Far : the era's backdrop above the horizon (an empty frame until the art comes), then the middle layer
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Layer(cam, 0));
            if (layerArt[0] != null) DrawLayerArt(sb, layerArt[0], left);
            else
            {
                Gfx.Rect(sb, left, 0, LayerW, Horizon, Palette.Stage);
                ArtSlot.Draw(sb, new Rectangle(left + 64, 40, LayerW - 128, Horizon - 60), Palette.Paper, 0.6f);
                Gfx.TextSpaced(sb, Game.Font, layerName[0], left + 470, Horizon - 46, Palette.LineGrey * 0.7f, TextSize.Label, 2f);
            }
            sb.End();
            if (layerArt[1] != null)
            {
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Layer(cam, 1));
                DrawLayerArt(sb, layerArt[1], left);
                sb.End();
            }

            //Stage Floor : the boards the band stands on, running away to the horizon. Until the art comes, a few
            //faint lines show the floor's slope so the depth can be judged.
            if (layerArt[2] != null)
            {
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Layer(cam, 2));
                DrawLayerArt(sb, layerArt[2], left);
                sb.End();
            }
            else
            {
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, Layer(cam, 2));
                Gfx.Rect(sb, left, Horizon, LayerW, TacetGame.ScreenH - Horizon, Palette.Stage);
                DrawFloorGuide(sb, left);
                Gfx.TextSpaced(sb, Game.Font, layerName[1], left + 470, Horizon + 10, Palette.LineGrey * 0.6f, TextSize.Label, 2f);
                sb.End();
            }

        }

        //Draw Fighters : into the SCENE after the dark. The bits turning around the hole, the enemies back to front,
        //the musician in front, then the front layer.
        private void DrawFighters(SpriteBatch sb, Vector2 cam)
        {
            int left = (TacetGame.ScreenW - LayerW) / 2;

            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            dark.DrawDebris(sb, false);
            sb.End();

            //Fighters : enemies back to front, then the musician in front. One SILENCED with its art is dissolved by
            //the shader in a batch of its own.
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Layer(cam, 3));
            for (int i = battle.Enemies.Count - 1; i >= 0; i--)
            {
                Fighter e = battle.Enemies[i];
                if (DissolvesWithShader(e))
                {
                    sb.End();
                    DrawDissolve(sb, e, cam);
                    sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Layer(cam, 3));
                }
                else DrawEnemy(sb, e);
            }
            DrawFront(sb);
            hits.DrawScene(sb);
            sb.End();

            //Front : dark shapes nearest the camera (a curtain's edge, a light rig), only when the art exists
            if (layerArt[3] != null)
            {
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Layer(cam, 4));
                DrawLayerArt(sb, layerArt[3], left);
                sb.End();
            }
        }

        //Layer Art : one backdrop picture at PIXEL x2, its left edge at left (64 pixels off screen for the camera)
        private static void DrawLayerArt(SpriteBatch sb, Texture2D art, int left)
        {
            sb.Draw(art, new Rectangle(left, 0, art.Width * 2, art.Height * 2), Color.White);
        }

        //Floor Guide : lines from the horizon, the way boards of a stage run, and a few across. Faint, a guide only.
        private void DrawFloorGuide(SpriteBatch sb, int left)
        {
            Vector2 vanish = new Vector2(760f, Horizon - 140f);
            Color line = Palette.LineGrey * 0.22f;
            for (int i = -8; i <= 16; i++)
            {
                float x = 640f + i * 150f;
                Vector2 bottom = new Vector2(x, TacetGame.ScreenH);
                float t = (Horizon - vanish.Y) / (bottom.Y - vanish.Y);
                Vector2 top = Vector2.Lerp(vanish, bottom, t);
                Gfx.Line(sb, top, bottom, line, 1f);
            }
            float y = Horizon;
            float gap = 10f;
            while (y < TacetGame.ScreenH)
            {
                Gfx.Line(sb, left, y, left + LayerW, y, line, 1f);
                y += gap;
                gap *= 1.45f;
            }
        }

        //Gone Of : how far a SILENCED enemy has dissolved (0 standing, 1 gone)
        private float GoneOf(Fighter f)
        {
            Look look;
            if (!f.Down) return 0f;
            if (!looks.TryGetValue(f, out look) || look.Gone <= 0f) return 1f;      // down before this page saw it
            return look.Gone;
        }

        private bool DissolvesWithShader(Fighter f)
        {
            float gone = GoneOf(f);
            return Fx.Ready && gone > 0f && gone < 1f && ArtOf(f).Set != null;
        }

        //Dissolve : SILENCED with its art. Sprite.fx eats the picture away from noise with a white rim.
        private void DrawDissolve(SpriteBatch sb, Fighter f, Vector2 cam)
        {
            Art art = ArtOf(f);
            Rectangle r = DrawBox(f);
            Effect e = Fx.Sprite;
            e.CurrentTechnique = e.Techniques["Dissolve"];
            e.Parameters["Amount"].SetValue(GoneOf(f));
            e.Parameters["RimColor"].SetValue(Color.White.ToVector4());
            e.Parameters["NoiseScale"].SetValue(18f);
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, e, Layer(cam, 3));
            art.Player.Draw(sb, art.Set, new Vector2(r.Center.X, r.Bottom), 2, Color.White);
            sb.End();
        }

        //Enemy : its shadow, its picture (darker the further back and the deeper in the dark), its flash.
        //SILENCED (no art yet) : the frame is drawn towards the hole, shrinking and fading, while its specks fly off.
        private void DrawEnemy(SpriteBatch sb, Fighter f)
        {
            float gone = GoneOf(f);
            if (gone >= 1f) return;
            Rectangle r = DrawBox(f);
            float depth = 1f - BattleRules.StageDepthDim * f.Slot;
            float deep = dark.InDark(new Vector2(r.Center.X, r.Bottom) - stageCam * BattleRules.StageParallax[3]);
            float shade = MathHelper.Clamp(depth * MathHelper.Lerp(1f, 0.55f, deep), 0.2f, 1f);
            float a = 1f;
            if (gone > 0f)
            {
                Vector2 hole = dark.Hole + stageCam * BattleRules.StageParallax[3];
                float pull = gone * gone;
                Vector2 middle = Vector2.Lerp(new Vector2(r.Center.X, r.Center.Y), hole, pull * 0.35f);
                float size = 1f - 0.45f * pull;
                r = new Rectangle((int)(middle.X - r.Width * size / 2f), (int)(middle.Y - r.Height * size / 2f), (int)(r.Width * size), (int)(r.Height * size));
                a = 1f - gone;
            }

            Gfx.DrawGlowBox(sb, new Rectangle(r.Center.X - r.Width / 2, r.Bottom - 22, r.Width, 44), Color.Black * (0.6f * a));
            Art art = ArtOf(f);
            if (art.Set != null) art.Player.Draw(sb, art.Set, new Vector2(r.Center.X, r.Bottom), 2, new Color(shade, shade, shade) * a);
            else ArtSlot.Draw(sb, r, Palette.Paper * shade, a);
            if (f.Staggered) Gfx.RectOutline(sb, r, Palette.Highlight * (0.5f + 0.5f * (float)Math.Sin(time * 8f)), 3);

            Look look;
            if (looks.TryGetValue(f, out look) && look.Flash > 0f) Gfx.Rect(sb, r, Palette.Paper * (0.55f * look.Flash * Flashing));
        }

        //Front : the musician the conductor points at, big, from behind (an empty frame until the art comes)
        private void DrawFront(SpriteBatch sb)
        {
            if (fore == null) return;
            Rectangle r = DrawBox(fore);
            float a = SwapAlpha() * (fore.Down ? 0.35f : 1f);

            Gfx.DrawGlowBox(sb, new Rectangle(r.X - 20, r.Bottom - 30, r.Width + 40, 60), Color.Black * (0.7f * a));
            Art art = ArtOf(fore);
            if (art.Set != null) art.Player.Draw(sb, art.Set, new Vector2(r.Center.X, r.Bottom), 2, Color.White * a);
            else
            {
                ArtSlot.Draw(sb, r, Palette.Paper, a);
                Gfx.TextSpaced(sb, Game.Font, layerName[2], r.X + 14, r.Y + 12, Palette.LineGrey * (0.8f * a), TextSize.Tiny, 1f);
                Gfx.TextSpaced(sb, Game.BigFont, fore.Name, r.X + 14, r.Y + 34, Palette.Paper * (0.5f * a), TextSize.Small, 3f);
            }

            Look look;
            if (looks.TryGetValue(fore, out look) && look.Flash > 0f) Gfx.Rect(sb, r, Palette.Paper * (0.5f * look.Flash * a * Flashing));
        }

        //Swap Offset : the musician in front slides out to the left, the next slides in from the left.
        //Eased at both ends (Beta1 round 4 : the user found the old one too quick and hard).
        private float SwapOffset()
        {
            if (swap <= 0f) return 0f;
            float slide = BattleRules.StageSwapSlide;
            if (swap < 0.5f) return -slide * Smooth(swap / 0.5f);            // out
            return -slide * (1f - Smooth((swap - 0.5f) / 0.5f));             // in
        }

        private float SwapAlpha()
        {
            if (swap <= 0f) return 1f;
            return swap < 0.5f ? 1f - Smooth(swap / 0.5f) : Smooth((swap - 0.5f) / 0.5f);
        }

        //Mood (Beta1 round 4) : a shaft of light from above onto the musician in front, with dust turning in it.
        //Into the GLOW, with the fighters' camera.
        private void DrawMood(SpriteBatch sb)
        {
            Rectangle f = foreBox;
            float a = BattleRules.StageShaft * (fore != null ? SwapAlpha() * 0.5f + 0.5f : 0.5f);

            //Shaft : a wide dim light stretched tall, and a narrow brighter core inside it
            Gfx.DrawGlowBox(sb, new Rectangle(f.X - 10, -260, f.Width + 20, f.Y + 520), Color.White * (a * 0.6f));
            Gfx.DrawGlowBox(sb, new Rectangle(f.X + 70, -200, f.Width - 140, f.Y + 420), Color.White * a);

            //Dust : specks drifting slowly down and sideways through the shaft, twinkling
            float span = f.Y + 300f;
            for (int i = 0; i < BattleRules.StageDust; i++)
            {
                float h1 = Hash01(i, 21);
                float h2 = Hash01(i, 22);
                float h3 = Hash01(i, 23);
                float x = f.X + 30f + h1 * (f.Width - 60f) + (float)Math.Sin(worldTime * (0.3f + h2) + i) * 12f;
                float y = (h2 * span + worldTime * (6f + 10f * h3)) % span - 40f;
                float twinkle = 0.5f + 0.5f * (float)Math.Sin(worldTime * (1.5f + 2f * h3) + i * 1.7f);
                Gfx.Rect(sb, (float)Math.Floor(x * 0.5f) * 2f, (float)Math.Floor(y * 0.5f) * 2f, 2f, 2f, Color.White * (0.35f * twinkle));
            }
        }

        //Hash 01 : a steady random 0..1 from two whole numbers (the same trick as TacetField)
        private static float Hash01(int a, int b)
        {
            float h = (float)Math.Sin(a * 12.9898f + b * 78.233f) * 43758.547f;
            return h - (float)Math.Floor(h);
        }

        //Stage Lights : into the light map (at half size, see PostChain.LightScale)
        private void DrawStageLights(SpriteBatch sb, Vector2 cam)
        {
            Vector2 near = cam * BattleRules.StageParallax[3];
            Vector2 floor = cam * BattleRules.StageParallax[2];

            //Stage Light : from above onto the musician in front, and its pool on the boards
            Rectangle f = foreBox;
            float spot = BattleRules.StageSpot * (fore != null ? SwapAlpha() * 0.5f + 0.5f : 0.5f);
            Gfx.DrawGlowBox(sb, new Rectangle(f.X - 120 - (int)near.X, f.Y - 120 - (int)near.Y, f.Width + 240, f.Height + 160), Color.White * spot);
            Gfx.DrawGlowBox(sb, new Rectangle(f.X - 160 - (int)floor.X, f.Bottom - 70 - (int)floor.Y, f.Width + 320, 140), Color.White * (spot * 0.8f));

            //House Lights : the floor near the band is lit, so TACET's dark can be seen eating the light as it comes
            Gfx.DrawGlowBox(sb, new Rectangle(-300 - (int)floor.X, Horizon + 60 - (int)floor.Y, TacetGame.ScreenW + 600, 760), Color.White * BattleRules.StageFloorLight);

            //The Dark : the hole lights what stands in front of it, the seam lights the floor
            dark.DrawLights(sb);

            //Foot Lights : each enemy stands in a pool of light, so it can be seen coming out of the dark
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                Fighter e = battle.Enemies[i];
                if (e.Down) continue;
                Rectangle r = EnemyBox(i);
                float power = BattleRules.StageFootLight * (1f - BattleRules.StageDepthDim * i);
                Gfx.DrawGlowBox(sb, new Rectangle(r.X - 30 - (int)near.X, r.Y + r.Height / 3 - (int)near.Y, r.Width + 60, r.Height), Color.White * power);
            }

            //Hits : a flash of light where something landed
            for (int i = 0; i < stageLights.Length; i++)
            {
                StageLight l = stageLights[i];
                if (l.Life <= 0f) continue;
                float left = l.Life / l.Length;
                Gfx.DrawGlow(sb, l.At.X - near.X, l.At.Y - near.Y, l.Radius, l.Tint * (l.Power * left * left));
            }
        }
    }
}
