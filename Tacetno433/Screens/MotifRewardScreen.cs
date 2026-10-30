using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //MotifRewardScreen : after a win, pick one motif out of three, or take shards instead.
    //The motif stays with the band for the rest of the run.
    //Beta1 screens round 1 (29 Oct, docs/SCREENS_LEVEL_UP.md 14) : THE SPOILS. Three cards lie face down under three
    //stage lights, flip up one after another and lean toward the mouse like the cards of Balatro (Core/Card3D.cs). The
    //rarer the motif, the stronger its light. A picked card jumps and flashes, the others turn back over.
    //THE BAND OF THREE (29 Oct, BETA1_DIRECTION 8.3 and 8.4) : the same cards for three things
    //   FIGHT     after an ELITE, three motifs, the common ones left out (normal fights pay shards only now)
    //   BOSS      a great upgrade, 1 of 3 : REFORGE (every weapon of the band a tier up, MotifArt.ReforgeFace) and two
    //             treasured motifs. REFORGE sits in the middle while a weapon can still go up.
    //   TREASURE  a chest on the map : one card, one motif, free (from the place's own dice, RunState.PlaceSeed)
    //PATH (Beta1 30 Oct, Data/Path.cs, BETA1_DIRECTION 12.4) : the same cards for the band's steps after a fight won, one page
    //each, before the motif of an ELITE or a BOSS (RunState.StepsWaiting)
    //   PATH      INSPIRED : the two next steps of a musician INSPIRED in the fight who did not pick at once, then the one card
    //             every win gives : up to three of the band's open steps. A card shows the musician, the step's numeral and path.
    public class MotifRewardScreen : GameScreen
    {
        public enum Kind { Fight, Treasure, Path }

        //Reward Layout
        private const int CardW = 240;
        private const int CardH = 366;
        private const int CardGap = 40;
        private const int CardsY = 166;
        private const int Pad = 30;                // a card's picture reaches this far past the card, for the glow on its face
        private Rectangle skipButton = new Rectangle(470, 624, 340, 50);

        //Reward State
        private Kind kind = Kind.Fight;
        private bool boss;                 // the boss's great upgrade (BOSS above)
        private int reforgeCard = -1;      // the card that is REFORGE, -1 for none. Its place in offer is null.
        private string reforgeText = "";
        private Motif[] offer = new Motif[0];

        //PATH : the step each card offers (kept as it was when the page opened, so the face stays once it is taken), whose it
        //is, on which path, and the page's words (made in Load)
        private PathStep[] stepOf = new PathStep[0];
        private Musician[] stepWho = new Musician[0];
        private int[] stepPath = new int[0];
        private int[] stepLevel = new int[0];
        private string[] stepPathName = new string[0];
        private string stepOver = "";
        private string stepTitle = "";
        private Rectangle[] cards = new Rectangle[0];
        private int selected;
        private bool mouseOnCard;          // the mouse is over the selected card, so it leans toward it
        private int picked = -1;           // -1 until a choice is made, -2 for skipping
        private float leaveTimer;
        private bool gone;                 // the next page has been asked for
        private float time;
        private string skipLabel = "";
        private Vector2 lastMouse;
        private UiMotion skipMotion = new UiMotion();

        //Cards In 3D : a picture of each face and one of the back, drawn in PreDraw, and each card's lean (radians),
        //how far it is lifted (0..1, the pointed card) and how lit its face is
        private RenderTarget2D[] faces = new RenderTarget2D[0];
        private RenderTarget2D back;
        private float[] leanX = new float[0];
        private float[] leanY = new float[0];
        private float[] lift = new float[0];

        public MotifRewardScreen()
        {
        }

        public MotifRewardScreen(Kind kind)
        {
            this.kind = kind;
        }

        //Wipe Word : the page's name in the wipe on the way here (WIPE WORD), short for SPOILS OF THE FIGHT
        public override string WipeWord
        {
            get { return kind == Kind.Treasure ? "THE TREASURE" : (kind == Kind.Path ? "A NEW STEP" : "THE SPOILS"); }
        }

        public override void Load()
        {
            RunState run = Game.CurrentRun;

            if (kind == Kind.Path) LoadSteps(run);
            else if (kind == Kind.Treasure)
            {
                //TREASURE : one motif of any rarity, from the place's dice so CONTINUE finds the same one
                offer = MotifList.Roll(run.Motifs, 1, 1, run.Floor, new Random(run.PlaceSeed));
            }
            else
            {
                //Offer : bosses and elites skip the common motifs. A boss offers REFORGE in the middle while a weapon can go up.
                int minRarity = 1;
                if (run.Battle != null && run.Battle.Enemy.Kind == EnemyKind.Elite) minRarity = 2;
                if (run.Battle != null && run.Battle.Enemy.Kind == EnemyKind.Boss) minRarity = 3;
                boss = minRarity == 3;
                bool reforge = boss && run.AnyToTrain;
                Motif[] motifs = MotifList.Roll(run.Motifs, BattleRules.MotifChoices - (reforge ? 1 : 0), minRarity, run.Floor, run.Rng);
                if (reforge)
                {
                    offer = new Motif[motifs.Length + 1];
                    reforgeCard = offer.Length / 2;
                    for (int i = 0, k = 0; i < offer.Length; i++)
                        if (i != reforgeCard) offer[i] = motifs[k++];
                    reforgeText = Gfx.WrapText(Game.StoryFont, "Every weapon of the band goes up one tier.", CardW - 44, TextSize.StorySmall, 4);
                }
                else offer = motifs;
            }

            cards = new Rectangle[offer.Length];
            int totalW = offer.Length * CardW + (offer.Length - 1) * CardGap;
            int startX = (TacetGame.ScreenW - totalW) / 2;
            for (int i = 0; i < offer.Length; i++)
                cards[i] = new Rectangle(startX + i * (CardW + CardGap), CardsY, CardW, CardH);

            faces = new RenderTarget2D[offer.Length];
            leanX = new float[offer.Length];
            leanY = new float[offer.Length];
            lift = new float[offer.Length];

            skipLabel = "TAKE " + BattleRules.SkipMotifShards + " SHARDS";
            lastMouse = Input.MousePos;
            SoundBank.PlayMusic(Music.Victory);

            //The Lens : a darker room, so the three stage lights carry the page
            Lens.Ambient = 0.36f;
            Lens.SpotPower = 0f;
            Lens.Vignette = 0.55f;
        }

        //Load Steps : PATH, an INSPIRED musician's two next steps first (one page each), else the win's card, up to three of the
        //band's open steps in a random order (the run's dice). The cards are motif cards with no motif (offer holds nulls).
        private void LoadSteps(RunState run)
        {
            List<Musician> who = new List<Musician>();
            List<int> path = new List<int>();
            Musician inspired = null;
            while (inspired == null && run.InspiredPending.Count > 0)
            {
                Musician m = run.InspiredPending[0];
                run.InspiredPending.RemoveAt(0);
                if (PathList.AnyOpen(m, run.Floor)) inspired = m;
            }
            if (inspired != null)
            {
                for (int p = 0; p < 2; p++)
                    if (PathList.StepOpen(inspired, p, run.Floor)) { who.Add(inspired); path.Add(p); }
                stepOver = "INSPIRED";
                stepTitle = inspired.Name + " : choose a step";
            }
            else
            {
                run.WinStepPending = false;
                for (int i = 0; i < run.Roster.Count; i++)
                    for (int p = 0; p < 2; p++)
                        if (PathList.StepOpen(run.Roster[i], p, run.Floor)) { who.Add(run.Roster[i]); path.Add(p); }
                for (int i = who.Count - 1; i > 0; i--)                                     // a shuffle with the run's dice
                {
                    int k = run.Rng.Next(i + 1);
                    Musician w = who[i]; who[i] = who[k]; who[k] = w;
                    int q = path[i]; path[i] = path[k]; path[k] = q;
                }
                while (who.Count > BattleRules.MotifChoices)
                {
                    who.RemoveAt(who.Count - 1);
                    path.RemoveAt(path.Count - 1);
                }
                stepOver = "THE BAND GROWS";
                stepTitle = "Choose a step";
            }

            int n = who.Count;
            offer = new Motif[n];
            stepOf = new PathStep[n];
            stepWho = who.ToArray();
            stepPath = path.ToArray();
            stepLevel = new int[n];
            stepPathName = new string[n];
            for (int i = 0; i < n; i++)
            {
                stepLevel[i] = stepWho[i].PathLevel[stepPath[i]];
                stepOf[i] = PathList.Next(stepWho[i], stepPath[i]);
                stepPathName[i] = PathList.Of(stepWho[i])[stepPath[i]].Name;
            }
        }

        //Leave : the card pictures are let go with the page
        public override void Leave()
        {
            for (int i = 0; i < faces.Length; i++)
                if (faces[i] != null) faces[i].Dispose();
            if (back != null) back.Dispose();
        }

        public override void Update(float dt)
        {
            time += dt;
            UpdateCards(dt);
            skipMotion.Update(dt, picked == -1 && Input.MouseOver(skipButton));

            //Leaving : the chosen card has a moment on its own before the page changes
            if (picked != -1)
            {
                leaveTimer += dt;
                if (leaveTimer > 0.8f && !gone)
                {
                    gone = true;
                    GoOn();
                }
                return;
            }

            int before = selected;
            if (Input.KeyPressed(Keys.Right) || Input.KeyPressed(Keys.D)) selected = Math.Min(offer.Length - 1, selected + 1);
            if (Input.KeyPressed(Keys.Left) || Input.KeyPressed(Keys.A)) selected = Math.Max(0, selected - 1);

            mouseOnCard = false;
            for (int i = 0; i < cards.Length; i++)
                if (Input.MouseOver(cards[i]))
                {
                    if (Input.MousePos != lastMouse) selected = i;
                    mouseOnCard = i == selected;
                }
            lastMouse = Input.MousePos;
            if (selected != before) SoundBank.Play(Sfx.UiMove);

            //Pick : only once the cards are face up
            bool ready = time > FlipStart(offer.Length - 1) + BattleRules.UiCardFlipTime * 0.6f;
            bool confirm = ready && offer.Length > 0 && (Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Space));
            for (int i = 0; i < cards.Length; i++)
                if (ready && Input.ClickedOn(cards[i])) { selected = i; confirm = true; }

            if (confirm)
            {
                picked = selected;
                RunState run = Game.CurrentRun;
                if (kind == Kind.Path)
                {
                    PathList.Take(stepWho[picked], stepPath[picked], run.Floor);              // PATH : the step is theirs
                    SoundBank.Play(Sfx.MotifGet);
                }
                else if (picked == reforgeCard)
                {
                    for (int i = 0; i < run.Roster.Count; i++) run.Rehearse(run.Roster[i]);    // REFORGE : every weapon a tier up
                    SoundBank.Play(Sfx.Rehearse);
                }
                else
                {
                    run.AddMotif(offer[picked]);
                    SoundBank.Play(Sfx.MotifGet);
                }
            }

            //Skip
            bool skip = Input.ClickedOn(skipButton);
            if (offer.Length == 0 && Input.KeyPressed(Keys.Enter)) skip = true;
            if (skip && picked == -1)
            {
                picked = -2;
                skipMotion.Press();
                Game.CurrentRun.AddShards(BattleRules.SkipMotifShards);
                SoundBank.Play(Sfx.UiBack);
            }
        }

        //Go On : the next stage. PATH : another step page while one waits, then the motif of an ELITE or a BOSS.
        private void GoOn()
        {
            RunState run = Game.CurrentRun;
            if (kind == Kind.Path && run.StepsWaiting)
            {
                Game.Screens.Change(new MotifRewardScreen(Kind.Path));
                return;
            }
            if (kind == Kind.Path && run.MotifAfterSteps)
            {
                run.MotifAfterSteps = false;
                Game.Screens.Change(new MotifRewardScreen());
                return;
            }
            RunFlow.NextStage(Game);
        }

        //======================================================================================
        //The Cards (Core/Card3D.cs)
        //======================================================================================

        //Flip Start : when card i starts to turn face up
        private float FlipStart(int i)
        {
            return BattleRules.UiCardFlipAt + i * BattleRules.UiCardFlipGap;
        }

        //Update Cards : each card's lean follows the mouse while it is pointed at, and sways a little on its own
        //otherwise. Picked or not, the lift eases in and out.
        private void UpdateCards(float dt)
        {
            float follow = 1f - (float)Math.Exp(-BattleRules.UiCardFollow * dt);
            float most = MathHelper.ToRadians(BattleRules.UiCardTilt);
            for (int i = 0; i < cards.Length; i++)
            {
                bool pointed = i == selected && picked == -1;
                float toX = 0.03f * (float)Math.Cos(time * 0.7f + i * 1.9f);
                float toY = 0.04f * (float)Math.Sin(time * 0.9f + i * 2.3f);
                if (pointed && mouseOnCard)
                {
                    //Lean : the side under the mouse is pressed away, like a card held between the fingers
                    Vector2 from = Input.MousePos - cards[i].Center.ToVector2();
                    toY = MathHelper.Clamp(from.X / (CardW / 2f), -1f, 1f) * most;
                    toX = MathHelper.Clamp(from.Y / (CardH / 2f), -1f, 1f) * most;
                }
                else if (pointed) toX = -0.08f;                     // chosen with the keys : tipped a little toward the player
                leanX[i] += (toX - leanX[i]) * follow;
                leanY[i] += (toY - leanY[i]) * follow;
                lift[i] = MathHelper.Clamp(lift[i] + (pointed || i == picked ? dt : -dt) / BattleRules.UiHoverTime, 0f, 1f);
            }
        }

        //Pose : how card i stands this frame. turn is its turn round the upright (pi = face down), drop how far it sits
        //below its place, alpha how much of it shows, grow its size.
        private void Pose(int i, out float turn, out float drop, out float alpha, out float grow)
        {
            //Flip Up : it rises into its light face down, then turns over, going a little past and back (EaseBack)
            float start = FlipStart(i);
            float rise = UiMotion.EaseOut(MathHelper.Clamp((time - start + 0.15f) / 0.3f, 0f, 1f));
            float flip = MathHelper.Clamp((time - start) / BattleRules.UiCardFlipTime, 0f, 1f);
            turn = MathHelper.Pi * (1f - UiMotion.EaseBack(flip));
            drop = (1f - rise) * 50f;
            alpha = rise;
            grow = 1f + (BattleRules.UiCardLift - 1f) * UiMotion.EaseOut(lift[i]);

            //Leaving : the picked card jumps and settles a little bigger, the rest turn back over and sink away
            if (picked == -1) return;
            float k = MathHelper.Clamp(leaveTimer / 0.35f, 0f, 1f);
            if (i == picked)
            {
                grow *= 1f + 0.1f * UiMotion.EaseBack(k);
                drop -= 26f * UiMotion.EaseOut(k);
                return;
            }
            turn += MathHelper.Pi * UiMotion.EaseInOut(k);
            drop += 40f * k;
            alpha *= 1f - MathHelper.Clamp((leaveTimer - 0.2f) / 0.4f, 0f, 1f);
        }

        //Showing Face : true while the card's front looks at the player (turned less than a quarter round either way)
        private static bool ShowingFace(float turn)
        {
            float t = Math.Abs(turn) % MathHelper.TwoPi;
            return t < MathHelper.PiOver2 || t > MathHelper.Pi * 1.5f;
        }

        //Pre Draw : the faces and the back into their pictures (Core/Card3D.cs). The face of a leaning card has a band of
        //light that slides as it leans (Balatro's shine), kept inside the card by a scissor box.
        private static readonly RasterizerState clip = new RasterizerState { ScissorTestEnable = true, CullMode = CullMode.None };

        public override void PreDraw(SpriteBatch sb)
        {
            if (offer.Length == 0) return;
            GraphicsDevice device = sb.GraphicsDevice;
            int w = CardW + Pad * 2;
            int h = CardH + Pad * 2;
            Rectangle card = new Rectangle(Pad, Pad, CardW, CardH);

            back = Card3D.Picture(device, back, w, h);
            Card3D.BeginPicture(device, sb, back);
            MotifArt.CardBack(sb, card, 1f, time);
            sb.End();

            float s = Sharp.Scale;
            for (int i = 0; i < offer.Length; i++)
            {
                faces[i] = Card3D.Picture(device, faces[i], w, h);
                Card3D.BeginPicture(device, sb, faces[i]);
                if (kind == Kind.Path) MotifArt.PathFace(sb, card, stepWho[i], stepPathName[i], stepOf[i], stepLevel[i], lift[i] > 0.5f, 1f, time);   // PATH
                else if (i == reforgeCard) MotifArt.ReforgeFace(sb, card, Game.CurrentRun, reforgeText, lift[i] > 0.5f, 1f, time);   // REFORGE
                else MotifArt.CardFace(sb, card, offer[i], lift[i] > 0.5f, 1f, time);
                sb.End();

                //Shine : across the card from corner to corner, further along the more it leans, brighter when rarer
                float shine = BattleRules.UiCardShine * (0.4f + 0.6f * lift[i]) * (0.6f + 0.2f * RarityOf(i));
                if (shine <= 0.01f) continue;
                float along = 0.5f + 1.1f * (leanY[i] - leanX[i]) / MathHelper.ToRadians(Math.Max(1f, BattleRules.UiCardTilt));
                Vector2 at = new Vector2(card.X + card.Width * along, card.Y + card.Height * (1f - along));
                device.ScissorRectangle = new Rectangle((int)(card.X * s), (int)(card.Y * s), (int)(card.Width * s), (int)(card.Height * s));
                Card3D.Batch(sb, Card3D.AddLight, clip);
                Vector2 middle = new Vector2(Gfx.Glow.Width / 2f, Gfx.Glow.Height / 2f);
                Vector2 size = new Vector2(card.Height * 1.4f / Gfx.Glow.Width, 70f / Gfx.Glow.Height);
                sb.Draw(Gfx.Glow, at, null, Color.White * shine, -0.85f, middle, size, SpriteEffects.None, 0f);
                sb.Draw(Gfx.Glow, at + new Vector2(60f, 60f), null, Color.White * (shine * 0.4f), -0.85f, middle, size * 0.6f, SpriteEffects.None, 0f);
                sb.End();
            }
        }

        //======================================================================================
        //The World (THE LENS) : the stage the cards stand on, their lights and their glow
        //======================================================================================

        public override bool HasWorld
        {
            get { return true; }
        }

        public override bool ShowsGrain
        {
            get { return false; }
        }

        //Light In : 0..1, how far card i's light has come up, once it is face up
        private float LightIn(int i)
        {
            float k = MathHelper.Clamp((time - FlipStart(i) - BattleRules.UiCardFlipTime * 0.4f) / 0.4f, 0f, 1f);
            float fade = picked == -1 || i == picked ? 1f : 1f - MathHelper.Clamp(leaveTimer / 0.4f, 0f, 1f);
            if (picked == -2) fade = 1f - MathHelper.Clamp(leaveTimer / 0.4f, 0f, 1f);
            return UiMotion.EaseOut(k) * fade;
        }

        public override void DrawWorld(SpriteBatch sb)
        {
            SceneBackdrop.Draw(sb, time);

            //Floor : a pool of light under each card, and its shadow there, the shadow wider the higher it is lifted
            for (int i = 0; i < cards.Length; i++)
            {
                Rectangle r = cards[i];
                float light = LightIn(i);
                Gfx.DrawGlowBox(sb, new Rectangle(r.X - 60, r.Bottom - 10, r.Width + 120, 70), Palette.Paper * (0.08f * light));
                float up = UiMotion.EaseOut(lift[i]);
                Gfx.DrawGlowBox(sb, new Rectangle(r.X - (int)(10 + 14 * up), r.Bottom + 4, r.Width + (int)(20 + 28 * up), 26), Color.Black * (0.55f * (1f - 0.3f * up)));
            }
        }

        //Lights : a stage light from above on each card, stronger the rarer the motif
        public override void DrawLights(SpriteBatch sb)
        {
            Gfx.DrawGlowBox(sb, new Rectangle(-200, 40, TacetGame.ScreenW + 400, 300), Color.White * 0.12f);
            for (int i = 0; i < cards.Length; i++)
            {
                Rectangle r = cards[i];
                float power = RarityLight(i) * LightIn(i) * (1f + 0.25f * lift[i]);
                Gfx.DrawGlow(sb, r.Center.X, r.Center.Y - 20f, 300f, Color.White * power);
                Gfx.DrawGlowBox(sb, new Rectangle(r.X - 20, -60, r.Width + 40, r.Bottom + 40), Color.White * (power * 0.6f));
            }
        }

        //Glow : rare and treasured motifs give off light round the card, a treasured one turns slow rays as well.
        //A picked card flashes (not with LESS FLASHING). The glow layer is added at alpha x alpha (PostChain.SpotLight).
        public override void DrawGlow(SpriteBatch sb)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                Rectangle r = cards[i];
                Vector2 c = r.Center.ToVector2();
                float light = LightIn(i);
                int rarity = RarityOf(i);
                if (rarity >= 2)
                {
                    float halo = (rarity >= 3 ? 0.42f : 0.28f) * light;
                    Gfx.DrawGlowBox(sb, new Rectangle(r.X - 46, r.Y - 40, r.Width + 92, r.Height + 80), Palette.Paper * halo);
                }
                if (rarity >= 3) Ornament.Rays(sb, c.X, c.Y, 160f, 300f, 24, time * 0.05f, Palette.Paper * (0.24f * light));
            }
            if (picked >= 0 && !Settings.ReduceFlashing)
            {
                float flash = 1f - MathHelper.Clamp(leaveTimer / 0.3f, 0f, 1f);
                Vector2 c = cards[picked].Center.ToVector2();
                Gfx.DrawGlow(sb, c.X, c.Y, 360f, Color.White * (0.75f * flash * flash));
            }
        }

        private float RarityLight(int i)
        {
            float[] lights = BattleRules.UiRarityLight;
            return lights[MathHelper.Clamp(RarityOf(i), 1, lights.Length) - 1];
        }

        //Rarity Of : card i's rarity for its light, REFORGE is as bright as a treasured motif, a PATH step as its numeral (I II III)
        private int RarityOf(int i)
        {
            if (kind == Kind.Path) return Math.Min(3, stepLevel[i] + 1);
            return i == reforgeCard || offer[i] == null ? 3 : offer[i].Rarity;
        }

        //======================================================================================
        //The Words and the Cards (crisp on top)
        //======================================================================================

        public override void Draw(SpriteBatch sb)
        {
            RunState run = Game.CurrentRun;
            float cx = TacetGame.ScreenW / 2f;

            //Heading : comes in first
            float head = UiMotion.Between(time, 0.05f, BattleRules.UiEnterTime);
            float rise = (1f - head) * BattleRules.UiEnterSlide;
            string over = kind == Kind.Treasure ? "A CHEST IN THE WINGS" : (boss ? "SPOILS OF THE MOVEMENT" : (BattleRules.TurnBattleOn ? "SPOILS OF THE FIGHT" : "SPOILS OF THE DUEL"));
            string title = kind == Kind.Treasure ? "Take what is inside" : (boss ? "Choose a great upgrade" : "Choose a motif");
            if (kind == Kind.Path)                                                         // PATH : made in Load
            {
                over = stepOver;
                title = stepTitle;
            }
            Gfx.TextSpacedCentered(sb, Game.Font, over, cx, 76 - rise, Palette.LineGrey * head, TextSize.Tiny, 5f);
            Gfx.TextCentered(sb, Game.BigFont, title, cx, 122 - rise, Palette.Paper * head, TextSize.Title);

            if (offer.Length == 0)
                Gfx.TextCentered(sb, Game.StoryFont, kind == Kind.Path ? "No step is open on this floor." : "The band already carries every motif there is.", cx, 340, Palette.PaperDim * head, TextSize.Story);

            DrawCards(sb);

            //Numbers : under each card, with it
            for (int i = 0; i < offer.Length; i++)
            {
                float turn, drop, alpha, grow;
                Pose(i, out turn, out drop, out alpha, out grow);
                Rectangle r = cards[i];
                Gfx.TextCentered(sb, Game.Font, RouteNodeInfo.PanelNumbers[i], r.Center.X, r.Bottom + 22 + drop * 0.3f, Palette.LineGrey * alpha, TextSize.Label);
            }

            //Pick Ring : a thin ring goes out from the picked card
            if (picked >= 0)
            {
                float k = MathHelper.Clamp(leaveTimer / 0.5f, 0f, 1f);
                Vector2 c = cards[picked].Center.ToVector2();
                if (k < 1f) Gfx.Ring(sb, c.X, c.Y - 26f, 150f + 260f * UiMotion.EaseOut(k), Palette.Paper * (0.7f * (1f - k)), 2f);
            }

            float foot = UiMotion.Between(time, FlipStart(offer.Length) + 0.2f, BattleRules.UiEnterTime);
            Gfx.TextCentered(sb, Game.StoryFont, "It stays with the band until the run ends.", cx, 594, Palette.PaperDim * foot, TextSize.StorySmall);
            float leaving = picked == -1 ? 1f : 1f - MathHelper.Clamp(leaveTimer / 0.3f, 0f, 1f);
            Ui.Command(sb, skipButton, skipLabel, "", skipMotion, foot * leaving, false);

            RunHud.DrawTop(sb, run, kind == Kind.Treasure ? "TREASURE" : "REWARD");
            RunHud.DrawTips(sb, run);
        }

        //Draw Cards : out of the sprite batch, each card in 3D, then the batch again (Core/Card3D.cs)
        private void DrawCards(SpriteBatch sb)
        {
            if (offer.Length == 0 || back == null) return;
            GraphicsDevice device = sb.GraphicsDevice;
            sb.End();
            float w = CardW + Pad * 2;
            float h = CardH + Pad * 2;
            for (int i = 0; i < offer.Length; i++)
            {
                if (faces[i] == null) continue;
                float turn, drop, alpha, grow;
                Pose(i, out turn, out drop, out alpha, out grow);
                float spin = turn + leanY[i];
                bool face = ShowingFace(spin);
                Vector2 at = cards[i].Center.ToVector2() + new Vector2(0f, drop - 10f * UiMotion.EaseOut(lift[i]));
                Card3D.Draw(device, face ? faces[i] : back, at, w, h, leanX[i], spin, grow, alpha, !face);
            }
            Sharp.Begin(sb);
        }
    }
}
