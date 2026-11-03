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
    //   LAYERS    the movement's backdrop (far, middle), the stage the band stands on, a dark front layer. Each layer
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
        private const int ChipW = 168;                      // BAND CARDS (block C item 6) : 150 until 2 Nov, now with a face (ends at 540, clear of a boss's bar)
        private const int ChipH = 68;                       // 50 until 31 Oct : the move written in the plan now has a line of its own
        private const int ChipMoveY = 48;                   //   this far down, under the life (a long one ran into "/ 54", 19.7)
        private const int ChipStep = 174;

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
        private readonly StageCamera actionCam = new StageCamera();   // ACTION : the camera that moves with the baton (Core/StageCamera.cs)
        private Vector2 camAt;             //   this frame's push : the point, the zoom, the turn and the slide (Draw works them out once)
        private float camZoom = 1f;
        private float camTurn;
        private Vector2 camShift;
        private int movement;              // which movement's backdrop (the floor, Data/Movement.cs, NO MORE ERAS 29 Oct)
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
            public bool Shared;        // SHARED LOOKS : an enemy wearing another's picture (Data/Enemy.cs Looks), its mark is drawn
            public Figure Figure;      // MATH ART (1 Nov) : no picture, the code draws it from shapes (Core/Figure.cs)
        }
        private Dictionary<Fighter, Art> arts = new Dictionary<Fighter, Art>();
        private Texture2D[] layerArt = new Texture2D[4];   // LAYERS : far, middle, stage floor, front (null = empty frame)
        private static readonly string[] layerFile = { "_far", "_mid", "_stage", "_front" };
        private static readonly string[] musicianAnims = { "idle", "attack", "enter", "skill", "parry", "hurt", "ko", "exit", "victory" };
        private static readonly string[] enemyAnims = { "idle", "attack", "hurt", "death" };

        //Stage Load : called from Arrange, once the battle exists
        private void LoadStage()
        {
            movement = run != null ? run.Movement : 0;
            string key = MovementList.Names[movement];
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
            actionCam.Reset();
            jab = 0f;

            //LAYERS : the movement's pictures, if the team has made them (Content/Art/Battle/Stages/<movement>_far.png ...)
            string movementKey = ArtBank.Key(key);
            for (int i = 0; i < layerArt.Length; i++) layerArt[i] = FrameArt.Picture(Game.GraphicsDevice, "Stages", movementKey + layerFile[i]);

            arts.Clear();
            for (int i = 0; i < battle.Party.Count; i++) ArtOf(battle.Party[i]);
            for (int i = 0; i < battle.Enemies.Count; i++) ArtOf(battle.Enemies[i]);
        }

        //Art Of : a fighter's pictures, found the first time they are needed (a musician CALLED in later too).
        //SHARED LOOKS (1 Nov) : an enemy with no picture of its own wears the first look of its list that the team has drawn
        //(Data/Enemy.cs Looks : THE LULL wears hush). Each wearer keeps its own clock, the pictures are read once (FrameArt).
        private Art ArtOf(Fighter f)
        {
            Art art;
            if (arts.TryGetValue(f, out art)) return art;
            art = new Art();
            if (f.IsEnemy)
            {
                art.Set = FrameArt.Load(Game.GraphicsDevice, "Enemies", ArtBank.Key(f.Name), enemyAnims);
                for (int i = 0; art.Set == null && f.Enemy != null && i < f.Enemy.Looks.Length; i++)
                {
                    art.Set = FrameArt.Load(Game.GraphicsDevice, "Enemies", f.Enemy.Looks[i], enemyAnims);
                    art.Shared = art.Set != null;
                }
            }
            else art.Set = FrameArt.Load(Game.GraphicsDevice, "Musicians", ArtBank.Key(f.Name), musicianAnims);
            if (art.Set != null) art.Player.Play(art.Set.Find("idle"), true);
            else MakeFigure(f, art);
            arts[f] = art;
            return art;
        }

        //Make Figure (MATH ART, Beta1 1 Nov, docs/BETA1_DIRECTION.md 31) : a fighter with no picture is drawn by the code.
        //A legend by its name, a boss by its name, an enemy by the look it shares (SHARED LOOKS : THE LULL wears hush, and its
        //mark and brightness still tell it apart, Data/Enemy.cs Looks). The team only draws the conductors now (13.79).
        private void MakeFigure(Fighter f, Art art)
        {
            string own = ArtBank.Key(f.Name);
            if (!f.IsEnemy)
            {
                HitColors colors = SpotOf(f);
                art.Figure = new Figure(Figure.Sort.Musician, own, f.Slot + 1);
                art.Figure.Tint = colors.Tint;
                art.Figure.Hot = colors.Hot;
                return;
            }
            bool isBoss = f.Enemy != null && f.Enemy.Kind == EnemyKind.Boss;
            string key = own;
            if (!isBoss && !IsLook(own)) key = f.Enemy != null && f.Enemy.Looks.Length > 0 ? f.Enemy.Looks[0] : "hush";
            if (!isBoss && !IsLook(key)) key = f.Enemy != null && f.Enemy.Looks.Length > 1 ? f.Enemy.Looks[1] : "hush";
            art.Figure = new Figure(isBoss ? Figure.Sort.Boss : Figure.Sort.Enemy, key, f.Slot + 7);
            art.Shared = !isBoss && key != own;
        }

        //Is Look : one of the four looks every enemy shares (docs/ASSET_LIST.md 1.2)
        private static bool IsLook(string key)
        {
            return key == "hush" || key == "static" || key == "the_damper" || key == "the_mute_choir";
        }

        //Play Art : start one of a fighter's animations.
        //STAND IN MOVES (1 Nov, docs/BETA1_DIRECTION.md 24) : the team draws idle and attack first (docs/ASSET_LIST.md), so a
        //move with no picture of its own borrows a near one, and with nothing near the code moves the picture (StandInMove) :
        //   skill, parry   the attack's picture, both land a blow
        //   hurt           the picture shivers as it flashes and is knocked back
        //   ko             the picture holds still, sinks and fades
        //   death          it holds still while it dissolves into the hole (the code did that already)
        //   attack         a musician lunges at the enemy (the code did that already), an enemy rushes at the band and back
        //   enter          the musician slides in (SWAP did that already)
        //   idle           a set with no idle stands on the first frame of its attack (FrameArt StandStill)
        private void PlayArt(Fighter f, string name)
        {
            if (f == null) return;
            Art art = ArtOf(f);
            if (art.Figure != null)                                     // MATH ART : the figure has its own poses (the box still moves)
            {
                bool swung = (name == "attack" || name == "skill") && view == View.Swipe;
                art.Figure.Play(name, swung ? lastWay : Flick.None);
            }
            FrameAnim anim = art.Set != null ? art.Set.Find(name) : null;
            if (anim == null && art.Set != null && (name == "skill" || name == "parry")) anim = art.Set.Find("attack");
            if (anim == null)
            {
                StandInMove(f, name);
                return;
            }
            art.Player.Play(anim, true);
            art.Busy = !anim.Loop;
        }

        //Stand In Move : the code's own move for a picture not drawn yet. Look keeps its clock, StandInPose moves the picture.
        private void StandInMove(Fighter f, string name)
        {
            Look look;
            if (!looks.TryGetValue(f, out look)) return;
            if (name == "hurt") look.Shiver = 1f;
            else if (name == "ko") look.Fall = 0.0001f;
            else if (name == "attack" && f.IsEnemy && f != boss) look.Rush = 0.0001f;          // a boss rears up instead (BossPose)
        }

        //Update Arts : every clock moves on. When a once-only animation ends, idle comes back (KO and death hold).
        //STAND IN MOVES : one down with no KO or death picture holds the frame it fell on.
        private void UpdateArts(float dt)
        {
            foreach (KeyValuePair<Fighter, Art> pair in arts)
            {
                Art art = pair.Value;
                if (art.Set == null) continue;
                if (pair.Key.Down && art.Player.Anim != null && art.Player.Anim.Loop) continue;
                art.Player.Update(dt);
                if (art.Busy && art.Player.Done && !pair.Key.Down) art.Busy = false;
                if (!art.Busy) art.Player.Play(art.Set.Find("idle"), false);
            }
        }

        //Update Figures (MATH ART) : each figure is told where it stands this frame, then its springs, chains and shards move.
        //WIND : while the baton swings, its speed blows through the hair and the cloth (BattleRules.FigureWind), TACET feels half.
        private void UpdateFigures(float dt)
        {
            figureFrame++;
            Vector2 wind = Vector2.Zero;
            if (view == View.Swipe && Input.MouseDown())
            {
                wind = baton.Velocity * BattleRules.FigureWind;
                if (wind.LengthSquared() > BattleRules.FigureWindMax * BattleRules.FigureWindMax) wind = Vector2.Normalize(wind) * BattleRules.FigureWindMax;
            }
            Vector2 hole = dark.Hole + stageCam * BattleRules.StageParallax[3];
            if (fore != null) UpdateFigure(fore, dt, wind, hole);
            for (int i = 0; i < battle.Enemies.Count; i++) UpdateFigure(battle.Enemies[i], dt, wind * 0.5f, hole);
        }

        private void UpdateFigure(Fighter f, float dt, Vector2 wind, Vector2 hole)
        {
            Figure figure = ArtOf(f).Figure;
            if (figure == null) return;
            Rectangle r = DrawBox(f);
            figure.Feet = new Vector2(r.Center.X, r.Bottom);
            figure.Height = r.Height;
            if (!f.IsEnemy)                                               // WHOLE LEGEND (round 6) : the box's feet are under the commands,
            {                                                             //   the legend stands over them, a little smaller, feet and all
                figure.Feet.Y -= r.Height * BattleRules.FigureBandLift;
                figure.Height = r.Height * BattleRules.FigureBandScale;
            }
            figure.Hole = hole;
            if (f.IsEnemy) figure.Life = f.MaxHp > 0 ? MathHelper.Clamp((float)f.Hp / f.MaxHp, 0f, 1f) : 0f;   // CRACKS
            figure.TurnPending = bossTurnWaiting;                                     // THE MONUMENT breaks at THE TURN
            if (!f.Down && figure.IsDown) figure.Up();                    // back up (a heal from zero)
            if (!f.IsEnemy && f.Musician != null) TellLegend(f, figure);  // LEGEND CUES
            figure.Update(dt, wind);
        }

        //Tell Legend (LEGEND CUES, MATH ART round 6, 2 Nov, docs/BETA1_DIRECTION.md 32.12) : what the fight shows on a legend's
        //body this frame (Core/Figure.Band.cs) : their life, the button they play, VOLUME, THE LESSON, SHIELD, THE HUNTING CALL
        //and the stars of each button (three = its LEGEND FORM). The rules are only read, never changed.
        private int figureFrame;           // LEGEND CUES : counts the frames the figures were moved

        private void TellLegend(Fighter f, Figure figure)
        {
            Figure.LegendCues cues = figure.Cues;
            Musician m = f.Musician;
            float life = f.MaxHp > 0 ? MathHelper.Clamp((float)f.Hp / f.MaxHp, 0f, 1f) : 0f;
            BarMove move = battle.Step == TurnStep.PlayerTurn && battle.Current == f ? battle.CurrentMove : null;
            bool seen = cues.Frame == figureFrame - 1;                    // on stage last frame too
            if (seen && life < cues.Life - 0.001f && move != null && move.Skill != null && battle.PaysInLife(f)) figure.Bled();   // THE DEVIL'S BARGAIN
            cues.Frame = figureFrame;
            cues.Life = life;
            cues.Button = move == null ? -1 : ButtonOf(m, move.Skill);
            cues.Volume = f.Crescendo;
            cues.Taught = f.Lesson;
            cues.LessonOut = false;
            for (int i = 0; i < battle.Party.Count; i++)
                if (battle.Party[i] != f && battle.Party[i].Lesson) cues.LessonOut = true;
            cues.Covered = f.CoveredBy != null;
            Fighter next = m.Trait == MusicianTrait.HuntingCall ? NextAfter(f) : null;
            cues.Calling = next != null;
            cues.CallTint = next != null ? SpotOf(next).Tint : Color.White;
            cues.Stars0 = RehearsalList.StarsOf(m, 0);
            cues.Stars1 = RehearsalList.StarsOf(m, 1);
            cues.Stars2 = RehearsalList.StarsOf(m, 2);
            Fighter aim = AimOf(f);                                       // ONE NOTE ONE BLOW : where the blow is going (PUNTO's ring)
            cues.Aimed = aim != null;
            if (aim != null) cues.TargetAt = CenterOf(DrawBox(aim));
            figure.Cues = cues;
        }

        //Button Of : which button a move is (0 the ATTACK, 1 and 2 the skills)
        private static int ButtonOf(Musician m, Skill s)
        {
            if (s == null) return 0;
            Skill first = SkillList.First(m);
            return s == first || s.Name == first.Name ? 1 : 2;
        }

        //Next After : THE HUNTING CALL, the musician who plays right after this one in the turn (the plan's order while it is
        //written, the turn's order while it plays, TACET's turns in between do not count), null when nobody does or it has passed
        private Fighter NextAfter(Fighter f)
        {
            if (battle.Step == TurnStep.Won || battle.Step == TurnStep.Lost) return null;
            bool planning = battle.Step == TurnStep.Plan;
            List<Fighter> order = planning ? previewOrder : battle.Order;
            int at = order.IndexOf(f);
            if (at < 0) return null;
            for (int i = at + 1; i < order.Count; i++)
            {
                Fighter o = order[i];
                if (o.IsEnemy || o == f || o.Down) continue;
                if (!planning && battle.OrderIndex > i) return null;      // that turn has been played
                return o;
            }
            return null;
        }

        //Figure Look Of : how an enemy's figure is drawn this frame, worked out the way DrawEnemy works out a picture :
        //fainter as it dissolves (SILENCED) or blinks (FLICKER), darker further back, the hit's white, the STAGGERED edge
        //beating. HOLE LIGHT (D7) : a figure is not darkened deeper in the dark like a picture is. Its shell is black
        //already, so the dark would swallow it whole; the hole's light catches its rim there instead (Figure.Behind).
        private void FigureLookOf(Fighter f, out float alpha, out float light, out float flash, out float mark)
        {
            float depth = 1f - BattleRules.StageDepthDim * f.Slot;
            light = MathHelper.Clamp(depth, 0.2f, 1f);
            if (ArtOf(f).Shared && f.Enemy != null) light *= f.Enemy.LookShade;
            Figure figure = ArtOf(f).Figure;
            if (figure != null) figure.Behind = BehindOf(f);
            alpha = (1f - GoneOf(f)) * FlickerOf(f);
            Look look;
            flash = looks.TryGetValue(f, out look) ? look.Flash * Flashing : 0f;
            mark = f.Staggered ? 0.5f + 0.5f * (float)Math.Sin(time * 8f) : 0f;
        }

        //Behind Of : HOLE LIGHT (D7, 2 Nov). How dark it is behind an enemy, 0..1 : the middle of its body over the black of
        //TACET's hole (full inside most of the hole, none past its rim), or its feet deep in the pool on the floor.
        //Both are measured on the screen, the way InDark always was (the fighters' layer moves with the camera).
        private float BehindOf(Fighter f)
        {
            Rectangle r = DrawBox(f);
            Figure figure = ArtOf(f).Figure;
            float tall = figure != null ? figure.Height : r.Height;
            Vector2 onScreen = stageCam * BattleRules.StageParallax[3];
            Vector2 middle = new Vector2(r.Center.X, r.Bottom - tall * 0.5f) - onScreen;
            float past = (Vector2.Distance(middle, dark.Hole) - dark.Radius * 0.8f) / (dark.Radius * 0.6f + 1f);
            float overHole = 1f - MathHelper.Clamp(past, 0f, 1f);
            float deep = dark.InDark(new Vector2(r.Center.X, r.Bottom) - onScreen);
            return Math.Max(overHole, deep);
        }

        //======================================================================================
        //Stage Update
        //======================================================================================

        //Update Stage : frozen = the hit stop is on (the world holds still, the FOCUS still moves)
        private void UpdateStage(float dt, bool frozen)
        {
            float want = view == View.Swipe ? 1f : 0f;
            focus = MoveTowards(focus, want, dt * BattleRules.StageFocusSpeed);
            actionCam.Update(dt);                                      // ACTION : the camera moves on through a hit stop
            if (frozen)
            {
                FreezeFigures(dt);                                     // ONE NOTE ONE BLOW : the stop holds a blow on its hit
                return;
            }

            float worldDt = dt * MathHelper.Lerp(1f, BattleRules.StageFocusSlow, WorldFocus);
            worldTime += worldDt;
            UpdateArts(worldDt);

            //SWAP : the musician pointed at walks in. A change in the middle of a swap just changes who comes in.
            Fighter pointed = Pointed();
            Figure leaving = FigureOf(fore);
            bool hold = view != View.Plan && view != View.Target && leaving != null && leaving.Busy;   // STAGE FIRST : a blow or a hurt ends first
            if (fore == null) fore = pointed;
            else if (pointed != null && pointed != fore && swap <= 0f && !hold)
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
            dark.Update(worldDt, MathHelper.Clamp(DarkShare() + BossDark() - ComboLight(), 0f, 1f));   // BOSS SHOW : the dark grows in its moments, THE MUSIC SWELLS pushes it back

            //MATH ART : the code's figures move on (where they stand, the baton's wind)
            UpdateFigures(worldDt);

            //Camera : the nudge of a hit dies away quickly
            kick *= (float)Math.Pow(0.0015, worldDt);

            for (int i = 0; i < stageLights.Length; i++)
                if (stageLights[i].Life > 0f) stageLights[i].Life -= worldDt;
        }

        //Focus Eased : the QTE FOCUS eased in and out, so the ring comes up and goes softly
        private float FocusEased
        {
            get { return focus * focus * (3f - 2f * focus); }
        }

        //World Focus : how far the world itself goes dark, grey and slow behind the ring. ACTION (Beta1, 29 Oct) : only a
        //little while the band swipes, so its blows can be seen (StageFocusSkill), more while TACET attacks.
        //CLEAR FIGHT keeps the whole dim of round 4 (nothing competes with the ring).
        private float WorldFocus
        {
            get
            {
                if (Settings.ClearFight) return FocusEased;
                return FocusEased * (swipeFor == SwipeFor.Parry ? BattleRules.StageFocusParry : BattleRules.StageFocusSkill);
            }
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
                Fighter aimed = battle.HitTarget(now);                                 // SHIELD : the one standing in front
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

        //Signature Colours : the conductor's SIGNATURE in its own colours. THE UNHEARING's LIGHTNING glows blue (round 6), red once
        //it has grown into STORM (the user : "blue first, then red", 13.90 and 13.119),
        //FANNY's PRISM is the white of her crystal, THE METRONOME's HOLY his gold (block D item 2), the hidden conductors white.
        private HitColors SignatureColors()
        {
            if (battle.Kit == TurnKit.Beethoven && battle.SignatureLevel >= 1) return new HitColors(Palette.StormRed, Palette.StormRedHot, HitFlair.None);   // STORM : red (13.119)
            if (battle.Kit == TurnKit.Beethoven) return new HitColors(Palette.LightningBlue, Palette.LightningBlueHot, HitFlair.None);
            if (battle.Kit == TurnKit.Fanny) return new HitColors(Palette.SpotPrism, Color.White, HitFlair.None);
            if (battle.Kit == TurnKit.Bach) return HolyColors;
            return HitColors.Plain;
        }

        //Signature Style : how the conductor's SIGNATURE lands. LIGHTNING strikes from the sky (THE UNHEARING, round 6), PRISM
        //breaks on each enemy (FANNY, block D item 2, the world's break is BattleScreen.Finale.cs), the others cut a CROSS.
        //THE METRONOME's FUGUE strikes nobody : its stars go to the band (BattleScreen.Finale.cs).
        private HitStyle SignatureStyle()
        {
            if (battle.Kit == TurnKit.Beethoven) return HitStyle.Lightning;
            if (battle.Kit == TurnKit.Fanny) return HitStyle.Prism;
            return HitStyle.Cross;
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

        //Skill Style (block D, 2 Nov, docs/BETA1_DIRECTION.md 37.3) : how a legend's own skill lands, so each looks like its
        //name (Core/HitFx.Skills.cs). The skill is found by its button, not by the name on it : a legend form renames the
        //button (CAPRICE No. 24) and stars rewrite it (Musician.Kit), but underneath it is still CAPRICE. A skill with no look
        //of its own yet lands like the musician's ATTACK (StyleOf).
        private static HitStyle SkillStyle(Fighter f, Skill s)
        {
            if (f == null || f.IsEnemy || f.Musician == null || s == null) return StyleOf(f);
            int k = s == SkillList.First(f.Musician) ? 0 : (s == SkillList.Second(f.Musician) ? 1 : -1);
            if (k < 0) return StyleOf(f);
            string plain = SkillList.Plain(f.Musician, k).Name;
            if (plain == "CAPRICE") return HitStyle.Caprice;
            if (plain == "IL CANNONE") return HitStyle.Cannon;
            if (plain == "LANCE") return HitStyle.Lance;
            if (plain == "CHAKRAM") return HitStyle.Chakram;
            if (plain == "TIMPANI ROLL") return HitStyle.Roll;
            return StyleOf(f);
        }

        //Playing Skill : the skill whose notes are being swiped now, null when none is
        private Skill PlayingSkill
        {
            get
            {
                if (view != View.Swipe || swipeFor != SwipeFor.Skill) return null;
                BarMove move = battle.CurrentMove;
                return move != null ? move.Skill : null;
            }
        }

        //Flashing : how strong flashes and shaking are, turned down by LESS FLASHING on the settings page
        private static float Flashing
        {
            get { return Settings.ReduceFlashing ? BattleRules.LessFlash : 1f; }
        }

        //Spot Of : a musician's colours for their blows (Palette spot colours, SpotLook in Data/Musician.cs).
        //The conductor's SIGNATURE and TACET are white for now.
        //ELEMENTS (round 6, 2 Nov) : each legend's own colour. FIRE and the old RED burn (flames), the others sparkle in their
        //colour until the elements' own VFX (docs/HANDOFF.md 00.3 part D).
        private static HitColors SpotOf(Fighter f)
        {
            if (f == null || f.IsEnemy || f.Musician == null) return HitColors.Plain;
            SpotLook look = f.Musician.Spot;
            HitFlair flair = look == SpotLook.Red || look == SpotLook.Fire ? HitFlair.Flame : HitFlair.Sparkle;
            return new HitColors(Palette.SpotOf(look), Palette.SpotHotOf(look), flair, look);
        }

        //Move Stars : STAR MARKS (block D item 3), the stars of the move a musician is striking with : the ATTACK's are the
        //weapon's tier, a skill's are its button's (REHEARSAL, Data/Rehearsal.cs). A skill is found by its button, the way
        //SkillStyle finds it (a legend form renames it).
        private int MoveStars(Fighter f)
        {
            if (f == null || f.IsEnemy || f.Musician == null) return 0;
            Skill s = PlayingSkill;
            if (s == null) return f.Musician.Rehearsed;
            int button = s == SkillList.First(f.Musician) ? 1 : (s == SkillList.Second(f.Musician) ? 2 : 0);
            return button > 0 ? RehearsalList.StarsOf(f.Musician, button) : 0;
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
        //ACTION (Beta1, 29 Oct, docs/BETA1_DIRECTION.md 10) : the baton moves the world. The user : "like we gave the order
        //and it really does it", the camera zooming and the screen shaking with what the player does (Muse Dash, Chaos
        //Zero Nightmare). Every swipe that lands is the musician's blow at once (NOTE BY NOTE in Battle/TurnBattle.cs) and :
        //   ZOOM STEPS  each note landed in a row pushes the camera in a step toward where the phrase lands, a MISS lets
        //               it fall back, the phrase over it eases out (Core/StageCamera.cs)
        //   KICK        the world slides and leans the way the baton swung
        //   SHAKE       every blow shakes it, a PERFECT more, the FINISHER (a skill's last note) most
        //   JAB         the musician in front jumps at the enemy with every blow
        //   RAPID       a ROLL is a blow for each shake, a HOLD keeps striking while it is held, the camera creeping in
        //The ring and the words on top never move with it, so the timing can always be read.
        //======================================================================================

        //Action Point : where the phrase being swiped lands. A skill at an enemy looks at it, a skill for the band at the
        //musician in front, the SIGNATURE (and a skill at every enemy) at TACET, a parry between the enemy and its target.
        private Vector2 ActionPoint()
        {
            if (swipeFor == SwipeFor.Parry)
            {
                Fighter enemy = battle.Current;
                if (enemy == null || !enemy.IsEnemy) return EnemiesCenter();
                Fighter aimed = battle.HitTarget(enemy);
                Vector2 to = aimed != null ? AimPoint(aimed) : CenterOf(foreBox);
                return (CenterOf(BoxOf(enemy)) + to) / 2f;
            }
            if (swipeFor == SwipeFor.Skill)
            {
                BarMove move = battle.CurrentMove;
                if (move != null && move.Skill != null && !move.Skill.HitsEnemy) return new Vector2(foreBox.Center.X + 60f, foreBox.Y + 170f);
                if (move != null && move.Skill != null && move.Skill.Target == SkillTarget.Enemy && move.Target != null && !move.Target.Down)
                    return CenterOf(BoxOf(move.Target));
            }
            return EnemiesCenter();
        }

        //Enemies Center : the middle of every enemy standing (the middle of the screen when none is)
        private Vector2 EnemiesCenter()
        {
            Vector2 sum = Vector2.Zero;
            int count = 0;
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                if (battle.Enemies[i].Down) continue;
                sum += CenterOf(BoxOf(battle.Enemies[i]));
                count++;
            }
            if (count == 0) return new Vector2(TacetGame.ScreenW / 2f, TacetGame.ScreenH / 2f);
            return sum / count;
        }

        private static Vector2 CenterOf(Rectangle r)
        {
            return new Vector2(r.Center.X, r.Center.Y);
        }

        //Skill Aims : the skill being swiped is aimed at this enemy (every enemy for a skill at all of them)
        private bool SkillAims(Fighter f)
        {
            BarMove move = battle.CurrentMove;
            if (view != View.Swipe || move == null || move.Skill == null || !move.Skill.HitsEnemy || f.Down) return false;
            return move.Skill.Target == SkillTarget.AllEnemies || move.Target == f;
        }

        //Streak Zoom : how far in the ZOOM STEPS are
        private float StreakZoom()
        {
            return Math.Min(BattleRules.CamZoomMost, streak * BattleRules.CamNoteZoom);
        }

        //Swipe Action : a note of a skill or of the SIGNATURE was answered. One that lands steps the camera in, kicks it the
        //way the baton went and shakes it, the last note of a skill punches hardest (FINISHER). A MISS lets it fall back.
        private void SwipeAction(Grade grade, bool last)
        {
            bool landed = grade == Grade.Perfect || grade == Grade.Good;
            if (!landed)
            {
                streak = 0;
                actionCam.Hold(ActionPoint(), 0f);
                return;
            }

            streak++;
            float strength = grade == Grade.Perfect ? BattleRules.CamPerfect : 1f;
            bool finisher = last && swipeFor == SwipeFor.Skill;
            actionCam.Hold(ActionPoint(), StreakZoom());
            actionCam.Kick(NoteGlyph.Way(lastWay), grade == Grade.Perfect ? 1f : 0.7f);
            actionCam.Punch(finisher ? BattleRules.CamFinishPunch : BattleRules.CamPunch * strength);
            actionCam.Shake(BattleRules.CamShake * strength * (finisher ? 2f : 1f));

            if (swipeFor == SwipeFor.Skill)
            {
                jab = 1f;
                if (FigureOf(battle.Current) == null) PlayArt(battle.Current, "attack");   // a figure struck already (NoteBlow)
            }
            else
            {
                //SIGNATURE : each stroke of the 4/4 is a beat of the conductor's light on TACET, the strike comes after the fourth.
                //FANNY's strokes write cracks, THE METRONOME's light stars (block D item 2, BattleScreen.Finale.cs).
                if (FinaleStroke(next)) return;
                HitColors c = SignatureColors();
                AddStageLight(EnemiesCenter(), 520f, 0.25f * strength * streak, 0.3f, Color.Lerp(Color.White, c.Tint, 0.6f));
                hits.Star(EnemiesCenter() - new Vector2(0f, 60f), BattleRules.HitSize * (0.5f + 0.25f * streak), c);
            }
        }

        //Parry Action : a note of TACET's attack was answered. A block throws the camera back toward the enemy (the blow is
        //turned away) and a block in a row steps it in, a blow that gets through throws it toward the musician and shakes more.
        private void ParryAction(Grade grade)
        {
            Fighter enemy = battle.Current;
            Vector2 toward = Vector2.Zero;
            if (enemy != null && enemy.IsEnemy && enemy.IntentTarget != null)
            {
                toward = AimPoint(battle.HitTarget(enemy)) - CenterOf(BoxOf(enemy));
                if (toward.LengthSquared() > 1f) toward.Normalize();
            }

            if (grade == Grade.Perfect)
            {
                streak++;
                actionCam.Hold(ActionPoint(), StreakZoom());
                actionCam.Kick(-toward, 0.8f);
                actionCam.Punch(BattleRules.CamPunch * BattleRules.CamPerfect);
                actionCam.Shake(BattleRules.CamShake);
                return;
            }

            streak = 0;
            actionCam.Hold(ActionPoint(), 0f);
            actionCam.Kick(toward, grade == Grade.Good ? 0.6f : 1.2f);
            actionCam.Shake(BattleRules.CamShake * (grade == Grade.Good ? 1.2f : 2f));
        }

        //Rapid Blow : one quick blow of a ROLL's shake or a HOLD being held. It is only light and feel : the note's
        //damage lands when the roll or the hold ends. build 0..1 grows the blows as the note goes on.
        private void RapidBlow(float build)
        {
            if (swipeFor != SwipeFor.Skill) return;
            BarMove move = battle.CurrentMove;
            Fighter user = battle.Current;
            if (move == null || move.Skill == null || user == null || user.IsEnemy) return;

            HitColors colors = SpotOf(user);
            Figure figure = FigureOf(user);
            if (holding)                                                             // HOLD : the charge stays planted and trembles (Figure.Charge)
            {
                if (figure == null) jab = Math.Max(jab, 0.6f);
            }
            else
            {
                jab = Math.Max(jab, 0.6f);
                if (figure != null) figure.Tap(lastWay, build);                      // ROLL : a small blow with the other hand each shake
            }
            actionCam.Shake(BattleRules.CamShake * 0.35f);
            if (!move.Skill.HitsEnemy)
            {
                //For the band (a heal) : the light gathers on the musician in front
                Vector2 at = ActionPoint();
                hits.Star(at + new Vector2(0f, -40f), BattleRules.HitSize * (0.35f + 0.3f * build), colors);
                AddStageLight(at, 200f, 0.3f + 0.3f * build, 0.2f, Color.Lerp(Color.White, colors.Tint, 0.5f));
                return;
            }

            //IL CANNONE (block D, 37.3) : the hold gathers fire into the violin's f hole (GATHER), nothing lands on the enemy
            //until it is let go. TIMPANI ROLL : each shake is one small ring on the floor under the enemy.
            HitStyle style = SkillStyle(user, move.Skill);
            if (style == HitStyle.Cannon)
            {
                Vector2 hole = figure != null ? figure.SoundPoint : ActionPoint();
                hits.Gather(hole, build, colors);
                AddStageLight(hole, 160f + 140f * build, 0.25f + 0.4f * build, 0.15f, colors.Tint);
                return;
            }
            if (style != HitStyle.Roll) style = StyleOf(user);

            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                Fighter e = battle.Enemies[i];
                if (e.Down) continue;
                if (move.Skill.Target == SkillTarget.Enemy && e != move.Target) continue;
                Rectangle box = DrawBox(e);
                hits.Hit(style, box, 0.15f + 0.3f * build, colors, 0, lastWay);
                StageHit(CenterOf(box), true, 0.2f + 0.25f * build, colors.Tint);
                Look look;
                if (looks.TryGetValue(e, out look)) look.Knock = Math.Max(look.Knock, 0.5f);
            }
        }

        //======================================================================================
        //ONE NOTE ONE BLOW (MATH ART round 6 part 5, 2 Nov, docs/BETA1_DIRECTION.md 32.21) : the figures' blows, note by note.
        //Core/Figure.cs does the poses, these only say when. The rules are not touched : each note lands when it always did.
        //======================================================================================

        //Figure Of : the code's figure of a fighter (MATH ART), null for one shown by a picture
        private Figure FigureOf(Fighter f)
        {
            return f != null ? ArtOf(f).Figure : null;
        }

        //Tell Blows : every frame of a swipe, before its note is answered. The one who plays a skill winds up toward the
        //note's way (Ready). On TACET's turn the enemy winds up, the one in front raises the guard (Brace), and the enemy's
        //blow lands ON the note's beat (EnemyBlow), or sooner if the parry comes early (Land).
        private void TellBlows(QteNote n, float now)
        {
            float dueIn = n.Due - now;
            bool last = next == notes.Count - 1;
            if (swipeFor == SwipeFor.Skill)
            {
                Figure f = FigureOf(battle.Current);
                if (f != null && !rolling) f.Ready(n.Way, dueIn, last, n.Kind == NoteKind.Swipe || n.Kind == NoteKind.Spark);   // PRE CUT for one stroke notes
                return;
            }
            if (swipeFor != SwipeFor.Parry || battle.Current == null || !battle.Current.IsEnemy) return;
            Figure guard = FigureOf(battle.HitTarget(battle.Current));
            if (guard != null) guard.Brace(dueIn);
            Figure enemy = FigureOf(battle.Current);
            if (enemy == null || struckNote == next) return;
            if (dueIn > 0f) enemy.Ready(n.Way, dueIn, last, true);                    // its blow starts before the beat (PRE CUT)
            else EnemyBlow(n);                                                         //   and lands on it
        }

        //Enemy Blow : TACET's blow for the note being answered, once
        private void EnemyBlow(QteNote n)
        {
            if (struckNote == next) return;
            struckNote = next;
            Figure enemy = FigureOf(battle.Current);
            if (enemy != null) enemy.Blow(n.Way, true, next == notes.Count - 1);
        }

        //Note Blow : a note was answered (Land). A skill's note is the musician's blow at once (a MISS cuts the air, the last
        //is the FINISHER), a parried note is TACET's blow on the one in front's guard (a MISS shows as the hurt of the hit)
        private void NoteBlow(Grade grade, bool last)
        {
            bool landed = grade == Grade.Perfect || grade == Grade.Good;
            if (swipeFor == SwipeFor.Skill)
            {
                Figure f = FigureOf(battle.Current);
                if (f == null) return;
                Fighter reached = HitInEvents();                                     // CHAKRAM : the throw goes where this note hit
                if (reached != null) throwAt = reached;
                f.Blow(lastWay != Flick.None ? lastWay : notes[next].Way, landed, last);
                return;
            }
            if (swipeFor != SwipeFor.Parry || battle.Current == null || !battle.Current.IsEnemy) return;
            EnemyBlow(notes[next]);
            Figure guard = FigureOf(battle.HitTarget(battle.Current));
            if (guard != null && landed) guard.Guard(grade == Grade.Perfect);
        }

        //Hold Blow : a HOLD note was caught. A skill's : the musician holds the charge (let go as the blow when it ends). TACET's :
        //its blow lands and the guard stays up.
        private void HoldBlow(Grade grade)
        {
            if (swipeFor == SwipeFor.Skill)
            {
                Figure f = FigureOf(battle.Current);
                if (f != null) f.Charge(lastWay);
                return;
            }
            if (swipeFor != SwipeFor.Parry || battle.Current == null || !battle.Current.IsEnemy) return;
            EnemyBlow(notes[next]);
            Figure guard = FigureOf(battle.HitTarget(battle.Current));
            if (guard != null) guard.Guard(grade == Grade.Perfect);
        }

        //Hit In Events : the first enemy the rules just hurt (read before ShowEvents clears them)
        private Fighter HitInEvents()
        {
            for (int i = 0; i < battle.Events.Count; i++)
            {
                TurnEvent e = battle.Events[i];
                if (e.Kind == TurnEventKind.Damage && e.Who != null && e.Who.IsEnemy) return e.Who;
            }
            return null;
        }

        //Wait For Stage : STAGE FIRST (the user 13.104 : let a blow, or taking a hit, finish before the next musician comes in).
        //True while the one in front or an enemy is still in a blow, a guard or a hurt, at most AttackWaitMost seconds.
        private bool WaitForStage(float dt)
        {
            if (!StageBusy() || stageWait >= BattleRules.AttackWaitMost)
            {
                stageWait = 0f;
                return false;
            }
            stageWait += dt;
            return true;
        }

        private bool StageBusy()
        {
            Figure f = FigureOf(fore);
            if (f != null && f.Busy) return true;
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                f = FigureOf(battle.Enemies[i]);
                if (f != null && f.Busy) return true;
            }
            return false;
        }

        //Freeze Figures : while the hit stop holds the world, a cut just begun still reaches its hit (Figure.Freeze)
        private void FreezeFigures(float dt)
        {
            Figure f = FigureOf(fore);
            if (f != null) f.Freeze(dt);
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                f = FigureOf(battle.Enemies[i]);
                if (f != null) f.Freeze(dt);
            }
        }

        //Aim Of : who the one in front's blow is at this frame, for PUNTO's ring (an ATTACK's target, the enemy a skill's
        //last note reached, else the skill's target), null when none stands
        private Fighter AimOf(Fighter f)
        {
            if (view == View.Strike && striker == f) return strikeTarget;
            if (view != View.Swipe || battle.Current != f || swipeFor != SwipeFor.Skill) return null;
            if (throwAt != null && !throwAt.Down) return throwAt;
            BarMove move = battle.CurrentMove;
            if (move != null && move.Target != null && move.Target.IsEnemy && !move.Target.Down) return move.Target;
            return FirstStanding();
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

            //BACK : the movement's backdrop and the floor, alone
            PostChain.BeginBack(device);
            DrawBackdrop(sb, cam);

            //SCENE : BACK seen through TACET's dark, then the fighters in front of it
            PostChain.BeginScene(device);
            dark.DrawScene(device, sb, PostChain.Back);
            DrawFighters(sb, cam);

            PostChain.BeginLights(device, sb, MoodAmbient(BattleRules.StageAmbient));      // THE STAGE TURNS : darker under a STORM or a night
            DrawStageLights(sb, cam);
            PostChain.EndLights(sb);

            //GLOW : the light of the dark, then the effects of the blows
            PostChain.BeginGlowTarget(device);
            dark.DrawGlow(device, sb, PostChain.Back);
            DrawMoodBack(sb, cam);                                // THE STAGE TURNS : the conductor's sky after a FINALE (BattleScreen.Finale.cs)
            DrawFrontSilhouette(device, cam);                     // MATH ART : the musician in front hides the seam's light behind it
            DrawTacetSilhouettes(device, cam);                    //   and TACET's figures hide the hole's light behind them
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, null, null, null, Layer(cam, 3));
            effects.DrawBlows(sb);
            effects.DrawSparks(sb);
            effects.DrawShards(sb);
            effects.DrawHits(sb);
            effects.DrawSlashes(sb);
            effects.DrawFlares(sb);
            hits.DrawGlow(sb);
            DrawFigureGlows(sb);
            DrawTellGlows(sb);                                    // TELLS : the CHARGE's heart and ring, STUNNED stars, the winding key
            DrawMood(sb);
            sb.End();

            PostChain.SetCuts(hits, cam * BattleRules.StageParallax[3]);      // SPACE CUT : the skills' cuts split the world's picture (Post.fx)
            PostChain.ImpactLook impact;
            impact.Turn = hits.Impact;
            impact.Flash = hits.ImpactFlash;
            impact.Blur = hits.ImpactBlur;
            impact.Center = ImpactOnScreen() / new Vector2(TacetGame.ScreenW, TacetGame.ScreenH);
            PostChain.Finish(device, sb, time, WorldFocus, dark.Engulf, impact, BattleRules.StageBloomThreshold, BattleRules.StageBloomStrength,
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
                float amount = shake * shake * 10f * Flashing * (Settings.ScreenShake ? 1f : 0f);   // SCREEN SHAKE (settings)
                cam += new Vector2((float)Math.Sin(time * 91f) * amount, (float)Math.Cos(time * 73f) * amount);
            }
            return cam;
        }

        //Layer : the camera for one layer. PARALLAX : 0 far, 1 middle, 2 stage floor, 3 fighters, 4 front
        //ADVANCED PART : SpriteBatch takes a matrix that moves everything drawn in that batch. Moving a far layer
        //less than a near one is all it takes for the eye to see depth.
        //SHARP SCREEN : the camera moves in game pixels, then Sharp.Transform makes them real ones. On a bigger screen the
        //move is put on a whole real pixel, so the words and frames on the layer stay crisp while it drifts.
        private static Matrix Layer(Vector2 cam, int depth)
        {
            float f = BattleRules.StageParallax[depth];
            if (Sharp.Scale == 1f) return Matrix.CreateTranslation(-cam.X * f, -cam.Y * f, 0f);
            return Matrix.CreateTranslation(Sharp.Snap(-cam.X * f), Sharp.Snap(-cam.Y * f), 0f) * Sharp.Transform;
        }

        //Draw Backdrop : into BACK, the far and middle layers and the stage floor
        private void DrawBackdrop(SpriteBatch sb, Vector2 cam)
        {
            int left = (TacetGame.ScreenW - LayerW) / 2;

            //Far : the movement's backdrop above the horizon, then the middle layer. No picture is coming (the team draws the
            //conductors only, 13.106) : the plain stage colour under TACET's dark, no empty frame or label (block C item 1)
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Layer(cam, 0));
            if (layerArt[0] != null) DrawLayerArt(sb, layerArt[0], left);
            else
            {
                Gfx.Rect(sb, left, 0, LayerW, Horizon, Palette.Stage);
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
                DrawFloorGuide(sb, left);                                              // the floor's lines stay, its label is gone (block C item 1)
                sb.End();
            }

        }

        //Draw Fighters : into the SCENE after the dark. The bits turning around the hole, the enemies back to front,
        //the musician in front, then the front layer.
        private void DrawFighters(SpriteBatch sb, Vector2 cam)
        {
            int left = (TacetGame.ScreenW - LayerW) / 2;

            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Sharp.Transform);
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

            //TACET LOOK (round 6) : TACET's figures drawn by the code straight onto the scene, smooth, over their BACKLIGHT
            if (DrawsTacet())
            {
                sb.End();
                Shapes.Begin(sb.GraphicsDevice, Layer(cam, 3), Sharp.Dot);
                DrawTacetReflections();
                DrawTacetFigures();
                DrawTellPlates();                                     // TELLS : ARMOR plates round them, CHARGE drawing shards in
                DrawTellShards();
                Shapes.End();
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Layer(cam, 3));
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

            Art art = ArtOf(f);
            float lift = LiftOf(f);                                                    // FLOAT : the shadow stays on the floor
            Gfx.DrawGlowBox(sb, new Rectangle(r.Center.X - r.Width / 2, r.Bottom + (int)lift - 22, r.Width, 44), Color.Black * ((lift > 0f ? 0.4f : 0.6f) * a));
            if (art.Figure != null)                                                    // MATH ART : the figure is drawn after every enemy (TACET LOOK),
            {                                                                          //   only its BACKLIGHT, HUM and HALO go behind it here
                float tall = art.Figure.Height;                                        // BACKLIGHT : round and soft, brightest behind the middle
                float lit = MathHelper.Clamp(depth, 0.2f, 1f);                         // HOLE LIGHT (D7) : not darker in the dark (FigureLookOf)
                float behind = BehindOf(f);
                Gfx.DrawGlow(sb, r.Center.X, r.Bottom - tall * 0.5f, tall * 0.62f, Color.White * (BattleRules.FigureBacklight * lit * a));
                if (behind > 0f)                                                       //   HALO : over the black a wide soft haze, the black body
                    Gfx.DrawGlow(sb, r.Center.X, r.Bottom - tall * 0.5f, tall * BattleRules.FigureHoleHaloSize,   //   stands out of it
                                 Color.White * (BattleRules.FigureHoleHalo * behind * lit * a));
                LookMark glowMark = MarkOf(f);
                if (gone <= 0f && (glowMark == LookMark.Hum || glowMark == LookMark.Halo)) DrawMark(sb, f, art, r, lit * f.Enemy.LookShade, a * FlickerOf(f));
                return;
            }
            if (art.Set != null)
            {
                if (art.Shared)
                {
                    shade *= f.Enemy.LookShade;                                          // SHARED LOOKS : its own brightness and mark
                    a *= FlickerOf(f);
                    DrawMark(sb, f, art, r, shade, a);
                }
                art.Player.Draw(sb, art.Set, new Vector2(r.Center.X, r.Bottom), 2, new Color(shade, shade, shade) * a);
            }
            else ArtSlot.Draw(sb, r, Palette.Paper * shade, a);
            if (f.Staggered) Gfx.RectOutline(sb, r, Palette.Highlight * (0.5f + 0.5f * (float)Math.Sin(time * 8f)), 3);

            Look look;
            if (looks.TryGetValue(f, out look) && look.Flash > 0f) Gfx.Rect(sb, r, Palette.Paper * (0.55f * look.Flash * Flashing));
        }

        //======================================================================================
        //STAND IN MOVES and SHARED LOOKS (Beta1 1 Nov, docs/BETA1_DIRECTION.md 24, numbers in BattleRules Look Feel)
        //======================================================================================

        //Stand In Pose : where the code's own moves put a fighter's picture this frame (called from DrawBox, so the aim
        //marks and the blows' light follow it). Only the enemies and the musician in front move, the band's cards stay still.
        private Rectangle StandInPose(Fighter f, Look look, Rectangle r)
        {
            if (!f.IsEnemy && f != fore) return r;
            if (look.Shiver > 0f) r.X += (int)(Math.Sin(time * BattleRules.StageShiverSpeed) * BattleRules.StageShiverPixels * look.Shiver * Flashing);
            if (look.Rush > 0f)
            {
                float lunge = Lunge(look.Rush);                                          // the same weighty move as a musician's ATTACK
                r.X -= (int)(lunge * BattleRules.StageRushX);
                r.Y += (int)(Math.Max(0f, lunge) * BattleRules.StageRushY);
            }
            if (look.Fall > 0f) r.Y += (int)(BattleRules.StageFallDrop * Smooth(look.Fall));
            if (f.IsEnemy)
            {
                r.Y -= (int)LiftOf(f);
                r.X += (int)JitterOf(f);
            }
            return r;
        }

        //Mark Of : the mark an enemy wears on a borrowed picture, None on its own picture or an empty frame
        private LookMark MarkOf(Fighter f)
        {
            if (!f.IsEnemy || f.Enemy == null || !ArtOf(f).Shared) return LookMark.None;
            return f.Enemy.Mark;
        }

        //Lift Of : FLOAT, how high it hangs above its shadow (slots bob out of step)
        private float LiftOf(Fighter f)
        {
            if (MarkOf(f) != LookMark.Float) return 0f;
            return BattleRules.LookFloatLift + (float)Math.Sin(worldTime * BattleRules.LookFloatSpeed + f.Slot * 1.7f) * BattleRules.LookFloatBob;
        }

        //Jitter Of : JITTER, the step sideways it snaps to now and then, left and right in turn
        private float JitterOf(Fighter f)
        {
            if (MarkOf(f) != LookMark.Jitter) return 0f;
            float t = worldTime + f.Slot * 0.37f;
            if (t % BattleRules.LookJitterEvery >= BattleRules.LookJitterTime) return 0f;
            return ((int)(t / BattleRules.LookJitterEvery) % 2 == 0 ? 1f : -1f) * BattleRules.LookJitterStep;
        }

        //Flicker Of : FLICKER, how strong it is this moment, now and then it blinks out.
        //ADVANCED PART : the blinks come from a hash of the step number (multiplying by a big odd number mixes its bits),
        //so they look random but need no dice and come back the same, frame after frame, with nothing stored.
        private float FlickerOf(Fighter f)
        {
            if (MarkOf(f) != LookMark.Flicker) return 1f;
            uint step = (uint)((worldTime + f.Slot * 0.53f) / BattleRules.LookFlickerStep);
            uint mix = unchecked(step * 2654435761u) >> 16;
            return mix % (uint)BattleRules.LookFlickerOdds == 0 ? BattleRules.LookFlickerDim : 1f;
        }

        //Draw Mark : HUM, HALO and ECHO, drawn behind the picture (FLOAT, JITTER and FLICKER move or fade the picture itself)
        private void DrawMark(SpriteBatch sb, Fighter f, Art art, Rectangle r, float shade, float a)
        {
            LookMark mark = MarkOf(f);
            if (mark == LookMark.Hum || mark == LookMark.Halo)
            {
                bool hum = mark == LookMark.Hum;
                float size = hum ? BattleRules.LookHumSize : BattleRules.LookHaloSize;
                float beat = 0.5f + 0.5f * (float)Math.Sin(worldTime * (hum ? BattleRules.LookHumSpeed : BattleRules.LookHaloSpeed));
                float power = hum ? BattleRules.LookHumPower * beat : BattleRules.LookHaloPower * beat * Flashing;
                Rectangle glow = r;
                glow.Inflate((int)(r.Width * size), (int)(r.Height * size * 0.6f));
                Gfx.DrawGlowBox(sb, glow, (hum ? Palette.Paper : Color.White) * (power * a));
            }
            else if (mark == LookMark.Echo)
            {
                for (int k = 2; k >= 1; k--)
                {
                    float strength = BattleRules.LookEchoAlpha / k;
                    Vector2 feet = new Vector2(r.Center.X + BattleRules.LookEchoStep * k, r.Bottom - BattleRules.LookEchoStep * 0.4f * k);
                    art.Player.Draw(sb, art.Set, feet, 2, new Color(shade, shade, shade) * (strength * a));
                }
            }
        }

        //Front : the musician the conductor points at, big, from behind (an empty frame until the art comes)
        private void DrawFront(SpriteBatch sb)
        {
            if (fore == null) return;
            Rectangle r = DrawBox(fore);
            Look down;
            float sink = looks.TryGetValue(fore, out down) && down.Fall > 0f ? Smooth(down.Fall) : 1f;   // STAND IN MOVES : it fades as it sinks
            float a = SwapAlpha() * (fore.Down ? MathHelper.Lerp(1f, BattleRules.StageDownFade, sink) : 1f);

            Art art = ArtOf(fore);
            if (art.Figure != null)                                                    // MATH ART : the legend drawn by the code, smooth (BAND LOOK)
            {
                Figure figure = art.Figure;                                            // its shadow under its feet (WHOLE LEGEND : they stand over the box's bottom)
                float wide = figure.Height * 0.6f, tall = figure.Height * 0.09f;
                Gfx.DrawGlowBox(sb, new Rectangle((int)(figure.Feet.X - wide * 0.5f), (int)(figure.Feet.Y - tall * 0.5f), (int)wide, (int)tall), Color.Black * (0.7f * a));
                Look lit;
                float flash = looks.TryGetValue(fore, out lit) ? lit.Flash * Flashing : 0f;
                sb.End();
                Shapes.Begin(sb.GraphicsDevice, Layer(stageCam, 3), Sharp.Dot);
                figure.DrawReflection(a, 1f);                                          // MIRROR : the legend in the glossy floor
                figure.Draw(a, flash, 1f, 0f);
                Shapes.End();
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Layer(stageCam, 3));
                return;
            }
            Gfx.DrawGlowBox(sb, new Rectangle(r.X - 20, r.Bottom - 30, r.Width + 40, 60), Color.Black * (0.7f * a));
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

        //======================================================================================
        //MATH ART (Beta1 1 Nov, docs/BETA1_DIRECTION.md 31) : the fighters the code draws (Core/Figure.cs)
        //======================================================================================

        //Draws Tacet : at least one enemy on the stage is drawn by the code
        private bool DrawsTacet()
        {
            for (int i = 0; i < battle.Enemies.Count; i++)
                if (ArtOf(battle.Enemies[i]).Figure != null) return true;
            return false;
        }

        //Draw Tacet Figures (TACET LOOK, round 6 : the user 13.87 asked for smooth edges, the old PIXEL LOOK and SCANLINES
        //are gone) : every enemy the code draws, back to front, black shells with a pale rim and the light inside
        private void DrawTacetFigures()
        {
            for (int i = battle.Enemies.Count - 1; i >= 0; i--)
            {
                Fighter e = battle.Enemies[i];
                Figure figure = ArtOf(e).Figure;
                if (figure == null) continue;
                float alpha, light, flash, mark;
                FigureLookOf(e, out alpha, out light, out flash, out mark);
                if (alpha <= 0f) continue;
                if (MarkOf(e) == LookMark.Echo)                                          // ECHO : two faint copies behind it
                {
                    for (int k = 2; k >= 1; k--)
                    {
                        figure.Shift = new Vector2(BattleRules.LookEchoStep * k, -BattleRules.LookEchoStep * 0.4f * k);
                        figure.Draw(alpha * BattleRules.LookEchoAlpha / k, 0f, light, 0f);
                    }
                }
                figure.Shift = Vector2.Zero;
                figure.Draw(alpha, flash, light, mark);
            }
        }

        //Draw Tacet Reflections (round 6 part 4, MIRROR) : every enemy the code draws, upside down and faint in the glossy floor
        //under it, back to front, before the figures themselves (Figure.DrawReflection)
        private void DrawTacetReflections()
        {
            for (int i = battle.Enemies.Count - 1; i >= 0; i--)
            {
                Fighter e = battle.Enemies[i];
                Figure figure = ArtOf(e).Figure;
                if (figure == null) continue;
                float alpha, light, flash, mark;
                FigureLookOf(e, out alpha, out light, out flash, out mark);
                figure.Shift = Vector2.Zero;
                figure.DrawReflection(alpha, light);
            }
        }

        //Tacet Silhouettes (round 6 part 4) : every enemy the code draws, in black into the GLOW picture over the light of
        //TACET's hole, so the rim, its rays and its grains pass behind the body instead of across it (it stands in front of the
        //hole, the user 13.90 : less sparkle where the fighters are). Their own light is added after (DrawFigureGlows).
        private void DrawTacetSilhouettes(GraphicsDevice device, Vector2 cam)
        {
            if (!DrawsTacet()) return;
            Shapes.Begin(device, Layer(cam, 3), Sharp.Dot);
            for (int i = battle.Enemies.Count - 1; i >= 0; i--)
            {
                Fighter e = battle.Enemies[i];
                Figure figure = ArtOf(e).Figure;
                if (figure == null) continue;
                float alpha, light, flash, mark;
                FigureLookOf(e, out alpha, out light, out flash, out mark);
                figure.Shift = Vector2.Zero;
                figure.DrawRim(alpha);                                                // BOSS RIM : the ring of light first (Figure.Tacet.cs)
                figure.DrawSilhouette(alpha);
            }
            Shapes.End();
        }

        //Fore Feet Y : where the musician in front stands on the floor. MATH ART : the legend stands over its box's bottom
        //(WHOLE LEGEND), so its pool of light goes under its feet, not under the commands
        private float ForeFeetY()
        {
            Figure figure = fore != null ? ArtOf(fore).Figure : null;
            return figure != null ? figure.Feet.Y : foreBox.Bottom;
        }

        //Front Silhouette : the musician in front drawn in black into the GLOW picture, over the light of TACET's seam, so the
        //seam's glow passes behind the figure instead of across it (the figure stands in front of it)
        private void DrawFrontSilhouette(GraphicsDevice device, Vector2 cam)
        {
            if (fore == null) return;
            Figure figure = ArtOf(fore).Figure;
            if (figure == null) return;
            Look down;
            float sink = looks.TryGetValue(fore, out down) && down.Fall > 0f ? Smooth(down.Fall) : 1f;
            float a = SwapAlpha() * (fore.Down ? MathHelper.Lerp(1f, BattleRules.StageDownFade, sink) : 1f);
            Shapes.Begin(device, Layer(cam, 3), Sharp.Dot);
            figure.DrawSilhouette(a);
            Shapes.End();
        }

        //Figure Glows : the band's weapon and core, TACET's white light inside, into the GLOW picture (they feed the bloom)
        private void DrawFigureGlows(SpriteBatch sb)
        {
            if (fore != null)
            {
                Figure figure = ArtOf(fore).Figure;
                if (figure != null) figure.DrawGlow(sb, SwapAlpha() * (fore.Down ? BattleRules.StageDownFade : 1f));
            }
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                Fighter e = battle.Enemies[i];
                Figure figure = ArtOf(e).Figure;
                if (figure == null) continue;
                float alpha, light, flash, mark;
                FigureLookOf(e, out alpha, out light, out flash, out mark);
                figure.Shift = Vector2.Zero;
                figure.DrawGlow(sb, alpha * light);
            }
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
            a *= 1f + BattleRules.StageSoloShaft * soloGlow;                                       // SOLO (BLOCK G) : the spotlight, in their colour
            Color light = fore != null ? Color.Lerp(Color.White, SpotOf(fore).Tint, 0.6f * soloGlow) : Color.White;

            //Shaft : a wide dim light stretched tall, and a narrow brighter core inside it
            Gfx.DrawGlowBox(sb, new Rectangle(f.X - 10, -260, f.Width + 20, f.Y + 520), light * (a * 0.6f));
            Gfx.DrawGlowBox(sb, new Rectangle(f.X + 70, -200, f.Width - 140, f.Y + 420), light * a);

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
            spot *= 1f + BattleRules.StageSoloSpot * soloGlow;                                     // SOLO (BLOCK G) : the spotlight
            Color lamp = fore != null ? Color.Lerp(Color.White, SpotOf(fore).Tint, 0.5f * soloGlow) : Color.White;
            Gfx.DrawGlowBox(sb, new Rectangle(f.X - 120 - (int)near.X, f.Y - 120 - (int)near.Y, f.Width + 240, f.Height + 160), lamp * spot);
            Gfx.DrawGlowBox(sb, new Rectangle(f.X - 160 - (int)floor.X, (int)ForeFeetY() - 70 - (int)floor.Y, f.Width + 320, 140), lamp * (spot * 0.8f));

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
