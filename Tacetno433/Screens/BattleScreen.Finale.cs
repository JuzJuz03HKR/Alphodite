using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Battle;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //BattleScreen Finale (Beta1 block D item 2, 2 Nov, docs/BETA1_DIRECTION.md 39) : each conductor's FINALE looks like their
    //theme (the user 13.87), from the four strokes of the 4/4 to the strike.
    //   LIGHTNING  THE UNHEARING : blue bolts from the sky (Core/HitFx.cs). STORM (after a boss) : the bolts turn red and rain
    //              is driven across the stage. STORM II : one great red bolt splitting over every enemy (Core/HitFx.Finale.cs).
    //   PRISM      FANNY MENDELSSOHN AND ??? : the strokes write cracks across the stage and the strike breaks the world like
    //              glass (Core/HitFx.Finale.cs). Her brother's voice heals the band : his cut opens on every card, purple and
    //              blue, and the life rises out of it (BattleScreen.Tells.cs, RIFT).
    //   HOLY       THE METRONOME : each stroke lights a star where its beat of the 4/4 falls (down, left, right, up : the shape
    //              a conductor's baton draws), a thread of gold joins it to the last, the stage goes dark round him and
    //              countless small warm stars come out (the user : "a million warm stars"). At the strike the top star, the
    //              one that guides, sends a star down to every musician : the ECHO they now carry, kept as a small star on
    //              their card. When an echo plays, the move is answered by a second voice in gold a moment later (FUGUE :
    //              the same theme again in another voice).
    //FINALE GROWS (the user 13.119 : "light and stars over the whole field") : FUGUE lights a patch of sky round its 4/4,
    //THE ART OF FUGUE fills the whole field with stars and the room goes darker, THE ART OF FUGUE II adds shafts of holy
    //light falling across the stage and more stars still.
    //The sky hangs over the enemies, so its 4/4 is drawn through the battle's camera; the far stars stand still over the
    //world, under the band's cards; the falling stars land on the cards, so they are drawn with them. Nothing here is made
    //while the fight runs. Numbers : Data/BattleRules.cs, Finale Feel.
    public partial class BattleScreen
    {
        //HOLY : each point's clock since it was lit (-1 = not lit), how far the sky has come out, the strike's clock
        private readonly float[] holyLit = { -1f, -1f, -1f, -1f };     // the points : 0 top (where the 4/4 starts and ends), 1 down, 2 left, 3 right
        private readonly float[] holyDrawn = { -1f, -1f, -1f, -1f };   // each beat's thread : 0 top to down, 1 down to left, 2 left to right, 3 right to top
        private float holySky;
        private float holyStruck = -1f;
        private bool holyOn;
        private Vector2 holyMiddle;                                    // where the 4/4 hangs, kept from its first beat

        //Holy Colors : THE METRONOME's gold, the ECHO's second voice
        private static readonly HitColors HolyColors = new HitColors(Palette.SpotHoly, Palette.SpotHolyHot, HitFlair.Sparkle);

        //Comet : a guiding star on its way down to a musician (the ECHO they get), waiting its turn first
        private struct Comet
        {
            public bool On;
            public Fighter To;
            public float Age, Wait;
        }
        private readonly Comet[] comets = new Comet[12];

        //ECHO VOICE : the damage that follows an ECHO in ShowEvents is the echo's (FUGUE), answered in gold a moment after
        private bool echoVoice;

        //STORM : THE UNHEARING's FINALE after a boss, rain driven across the stage while the bolts strike (-1 = none)
        private float stormAge = -1f;
        private const float StormTime = 1.8f;
        private const int StormDrops = 110;

        //STORM II : the enemies struck by the great bolt (no list is made in a fight)
        private readonly Rectangle[] greatBoxes = new Rectangle[8];

        //PRISM : where each of FANNY's strokes cuts, by its beat. The cracks cross near the enemies at four angles, so the
        //world breaks into pieces of many sizes (the small ones round the enemies).
        private static readonly float[] CrackAngles = { 1.15f, -0.38f, 0.3f, -1.0f };
        private static readonly Vector2[] CrackShifts = { new Vector2(-70f, -10f), new Vector2(30f, -70f), new Vector2(-10f, 60f), new Vector2(80f, 0f) };

        //======================================================================================
        //The strokes and the strike
        //======================================================================================

        //Finale Begins : the FINALE's swipe starts (StartSignature). HOLY starts from an empty sky.
        private void FinaleBegins()
        {
            holyOn = battle.Kit == TurnKit.Bach;
            holySky = 0f;
            holyStruck = -1f;
            for (int i = 0; i < holyLit.Length; i++)
            {
                holyLit[i] = -1f;
                holyDrawn[i] = -1f;
            }
            holyMiddle = EnemiesCenter() - new Vector2(0f, BattleRules.FinaleHolyLift);
        }

        //Finale Stroke : a stroke of the 4/4 landed (beat 0 to 3). Returns true when the conductor has a look of their own
        //for it, false lets the plain beat of light play.
        private bool FinaleStroke(int beat)
        {
            if (beat < 0 || beat > 3) return false;
            if (battle.Kit == TurnKit.Fanny)
            {
                //PRISM : the stroke writes a crack across the stage. The fourth is her brother's (TWO VOICES).
                bool brother = beat == 3;
                Vector2 at = EnemiesCenter() + CrackShifts[beat];
                float lean = lastWay == Flick.Left || lastWay == Flick.Up ? -0.06f : 0.06f;      // the baton's way leans it a little
                hits.PrismCrack(at, CrackAngles[beat] + lean, brother);
                AddStageLight(at, 420f, 0.3f, 0.3f, brother ? Palette.SpotBrother : Palette.SpotPrism);
                return true;
            }
            if (battle.Kit == TurnKit.Bach)
            {
                //HOLY : the beat's star lights and the thread to it from the last point runs on. The first beat starts from the top.
                if (holyLit[0] < 0f) holyLit[0] = 0f;
                int point = beat < 3 ? beat + 1 : 0;
                holyLit[point] = 0f;
                holyDrawn[beat] = 0f;
                return true;
            }
            return false;
        }

        //Finale Strike : the FINALE lands (FinishSwipe, after the rules have played it, before its events are shown)
        private void FinaleStrike()
        {
            int level = battle.SignatureLevel;
            if (battle.Kit == TurnKit.Fanny) hits.PrismBreak(EnemiesCenter(), level);
            else if (battle.Kit == TurnKit.Bach)
            {
                holyStruck = 0f;
                holyLit[0] = 0f;                                                          // the guiding star flares, lit or not
                AddStageLight(holyMiddle, 560f, 0.35f, 0.5f, Palette.SpotHoly);
            }
            else if (battle.Kit == TurnKit.Beethoven && level >= 1) stormAge = 0f;      // STORM
        }

        //Great Bolt Now : STORM II, one great bolt for every enemy this FINALE struck (the ones it silenced too, they were there)
        private void GreatBoltNow(HitColors colors)
        {
            int count = 0;
            for (int i = 0; i < battle.Events.Count && count < greatBoxes.Length; i++)
            {
                TurnEvent e = battle.Events[i];
                if (e.Kind != TurnEventKind.Damage || e.Who == null || !e.Who.IsEnemy) continue;
                Rectangle box = DrawBox(e.Who);
                bool seen = false;
                for (int k = 0; k < count; k++) if (greatBoxes[k] == box) seen = true;
                if (!seen) greatBoxes[count++] = box;
            }
            hits.GreatBolt(greatBoxes, count, colors);
            AddStageLight(new Vector2(TacetGame.ScreenW / 2f, 260f), 1400f, BattleRules.HitBoltFlash * 1.4f, 0.6f, Color.Lerp(Color.White, colors.Tint, 0.6f));
        }

        //Holy Comets : the strike sends a guiding star down to a musician for each move their ECHO will play (THE ART OF
        //FUGUE : more than one), one after another
        private void HolyComets(Fighter to)
        {
            int count = Math.Max(1, to.EchoLeft);
            int order = 0;
            for (int i = 0; i < comets.Length; i++)
                if (comets[i].On) order++;
            for (int k = 0; k < count; k++)
            {
                int slot = -1;
                for (int i = 0; i < comets.Length; i++)
                    if (!comets[i].On) { slot = i; break; }
                if (slot < 0) return;
                comets[slot] = new Comet { On = true, To = to, Age = 0f, Wait = 0.12f + 0.09f * order + 0.14f * k };
                order++;
            }
        }

        //======================================================================================
        //Update
        //======================================================================================

        private void UpdateFinale(float dt)
        {
            if (stormAge >= 0f)
            {
                stormAge += dt;
                if (stormAge > StormTime) stormAge = -1f;
            }

            for (int i = 0; i < comets.Length; i++)
            {
                if (!comets[i].On) continue;
                if (comets[i].Wait > 0f)
                {
                    comets[i].Wait -= dt;
                    continue;
                }
                comets[i].Age += dt;
                if (comets[i].Age < BattleRules.FinaleHolyFall) continue;
                comets[i].On = false;                                                      // it lands : the card takes the ECHO
                if (battle.Party.Contains(comets[i].To)) AddCardFx(CardLook.Echo, comets[i].To, Palette.SpotHoly);
            }

            if (!holyOn) return;
            for (int i = 0; i < holyLit.Length; i++)
            {
                if (holyLit[i] >= 0f) holyLit[i] += dt;
                if (holyDrawn[i] >= 0f) holyDrawn[i] += dt;
            }
            int beats = 0;
            for (int i = 0; i < holyDrawn.Length; i++)
                if (holyDrawn[i] >= 0f) beats++;
            float sky = beats / 4f;
            if (holyStruck >= 0f)
            {
                holyStruck += dt;
                sky = 1f - MathHelper.Clamp((holyStruck - 0.7f) / BattleRules.FinaleHolyFade, 0f, 1f);
                if (holyStruck > 0.7f + BattleRules.FinaleHolyFade) holyOn = false;
            }
            else if (view != View.Swipe) holyOn = false;                                  // a FINALE that never struck (the fight ended)
            holySky += (sky - holySky) * Math.Min(1f, dt * 5f);                          // the sky eases out, never jumps
        }

        //======================================================================================
        //Draw
        //======================================================================================

        //Draw Finale Sky : over the world, under the panel and the cards. HOLY : the stage darkens, the far stars come out,
        //the 4/4 of stars hangs over the enemies. STORM : the rain.
        private void DrawFinaleSky(SpriteBatch sb)
        {
            if (hits.HasOver)                                                            // PRISM : the glass and the months over the broken world
            {
                float f = BattleRules.StageParallax[3];
                sb.End();
                sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, null, null, null,
                         Matrix.CreateTranslation(-stageCam.X * f, -stageCam.Y * f, 0f) * CameraPush() * Sharp.Transform);
                hits.DrawOver(sb);
                sb.End();
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, Sharp.Transform);
            }
            if (stormAge >= 0f) DrawStorm(sb);
            if (!holyOn || holySky <= 0.005f) return;

            int level = battle.SignatureLevel;
            float dark = BattleRules.FinaleHolyDark * (level >= 1 ? 1f : 0.6f);              // FINALE GROWS : the room darker from THE ART OF FUGUE
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Color.Black * (dark * holySky));

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, null, null, null, Sharp.Transform);
            if (level >= 2) DrawHolyShafts(sb);
            DrawHolyStars(sb, level);
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, Sharp.Transform);
        }

        //Draw Finale Stars : HOLY's 4/4 of stars, over the enemies' name plates (a star under a plate would not be seen),
        //through the battle's camera so it hangs over them
        private void DrawFinaleStars(SpriteBatch sb)
        {
            if (!holyOn || holySky <= 0.005f) return;
            BeginCameraBatch(sb, BlendState.Additive);
            DrawHolyFour(sb);
            EndCameraBatch(sb);
        }

        //Draw Holy Stars : the countless small stars, each where its number puts it (the same sky every time), twinkling at
        //its own pace, coming out one after another as the sky fills, warm gold to white. FUGUE lights only the sky round its
        //4/4 (a patch fading at its edge), THE ART OF FUGUE the whole field, THE ART OF FUGUE II more of them.
        private void DrawHolyStars(SpriteBatch sb, int level)
        {
            int count = level >= 2 ? BattleRules.FinaleHolyStars * 8 / 5 : BattleRules.FinaleHolyStars;
            float top = PanelTop - 40f;
            Vector2 patch = OnScreen(holyMiddle);
            float reach = BattleRules.FinaleHolyWide * 2.2f;
            for (int i = 0; i < count; i++)
            {
                float showAt = Hash(i, 5) * 0.85f;                                        // when in the filling this star comes out
                float seen = MathHelper.Clamp((holySky - showAt) / 0.15f, 0f, 1f);
                if (seen <= 0f) continue;
                float x = Hash(i, 1) * TacetGame.ScreenW;
                float y = Hash(i, 2) * Hash(i, 3) * top;                                  // more of them high up, fewer near the floor
                if (level <= 0)                                                           // FUGUE : only the patch of sky round the 4/4
                {
                    float far = Vector2.Distance(new Vector2(x, y), patch);
                    seen *= MathHelper.Clamp((reach - far) / 90f, 0f, 1f);
                    if (seen <= 0f) continue;
                }
                float twinkle = 0.45f + 0.55f * (float)Math.Sin(time * (1.2f + 3f * Hash(i, 4)) + i * 1.7f);
                float a = seen * twinkle;
                Color c = Color.Lerp(Palette.SpotHoly, Palette.SpotHolyHot, Hash(i, 6));
                float size = Hash(i, 7) > 0.9f ? 3f : 2f;
                Gfx.Rect(sb, (float)Math.Floor(x * 0.5f) * 2f, (float)Math.Floor(y * 0.5f) * 2f, size, size, c * a);
                if (size > 2f) Gfx.DrawGlow(sb, x, y, 9f, c * (0.3f * a));                  // the brighter few glow
            }
        }

        //Draw Holy Four : the 4/4 of stars over the enemies. Each lit point is a star with its glow, each beat's thread of
        //gold runs from the last point to its own. The top star, the one that guides, flares at the strike.
        private void DrawHolyFour(SpriteBatch sb)
        {
            float struck = holyStruck >= 0f ? Math.Max(0f, 1f - holyStruck / 0.9f) : 0f;
            for (int beat = 0; beat < 4; beat++)
            {
                if (holyDrawn[beat] < 0f) continue;
                Vector2 a = HolyPoint(beat);
                Vector2 b = HolyPoint(beat < 3 ? beat + 1 : 0);
                float run = Math.Min(1f, holyDrawn[beat] / 0.14f);
                Vector2 head = Vector2.Lerp(a, b, run);
                Gfx.Line(sb, a, head, Palette.SpotHoly * (0.3f * holySky), 8f);
                Gfx.Line(sb, a, head, Palette.SpotHolyHot * (0.9f * holySky), 2f);
                if (run < 1f) Gfx.DrawGlow(sb, head.X, head.Y, 16f, Palette.SpotHolyHot * holySky);
            }
            for (int p = 0; p < 4; p++)
            {
                if (holyLit[p] < 0f) continue;
                Vector2 at = HolyPoint(p);
                float pop = holyLit[p] < 0.15f ? 1f + 1.2f * (1f - holyLit[p] / 0.15f) : 1f;
                float breathe = 0.85f + 0.15f * (float)Math.Sin(time * 2.4f + p);
                float size = (p == 0 ? 13f : 9f) * pop * breathe * (1f + 1.6f * struck * (p == 0 ? 1f : 0.4f));
                Gfx.DrawGlow(sb, at.X, at.Y, size * 4.5f, Palette.SpotHoly * (0.45f * holySky));
                Gfx.Star(sb, at.X, at.Y, size, Palette.SpotHolyHot * holySky);
                Gfx.Rect(sb, at.X - 1f, at.Y - 1f, 2f, 2f, Color.White * holySky);
            }
            if (struck > 0f)
            {
                Vector2 guide = HolyPoint(0);
                Gfx.DrawGlow(sb, guide.X, guide.Y, 260f * struck, Palette.SpotHoly * (0.5f * struck * Flashing));
            }
        }

        //Draw Holy Shafts : THE ART OF FUGUE II, five wide shafts of holy gold falling from the top across the whole stage (the
        //band's side too), swaying slowly, soft at their edges, a narrower bright core in each (the shaft of THE METRONOME's
        //AURA, Core/ConductorAura.cs, now over the whole field)
        private void DrawHolyShafts(SpriteBatch sb)
        {
            for (int k = 0; k < 5; k++)
            {
                float x = 140f + k * 250f + (float)Math.Sin(time * 0.35f + k * 1.7f) * 40f;
                float lean = 60f + 30f * (float)Math.Sin(time * 0.2f + k);
                float strength = holySky * (0.7f + 0.3f * (float)Math.Sin(time * 0.9f + k * 2.3f));
                Shaft(sb, x, x - lean, 34f, 120f, Palette.SpotHoly * (0.14f * strength));
                Shaft(sb, x, x - lean, 12f, 46f, Palette.SpotHolyHot * (0.16f * strength));
            }
        }

        //Shaft : a beam of light from the top of the screen (narrow, at topX) to the panel (wide, at footX), in rows 4 pixels tall
        private static void Shaft(SpriteBatch sb, float topX, float footX, float topHalf, float footHalf, Color color)
        {
            for (float y = 0f; y < PanelTop; y += 4f)
            {
                float t = y / PanelTop;
                float x = MathHelper.Lerp(topX, footX, t);
                float half = MathHelper.Lerp(topHalf, footHalf, t);
                Gfx.Rect(sb, x - half, y, half * 2f, 4f, color);
                Gfx.Rect(sb, x - half * 0.55f, y, half * 1.1f, 4f, color);              // brighter towards the middle : softer edges
            }
        }

        //Holy Point : where a point of the 4/4 hangs (0 top, 1 down, 2 left, 3 right), the shape a conductor's baton draws
        private Vector2 HolyPoint(int p)
        {
            float wide = BattleRules.FinaleHolyWide;
            float tall = BattleRules.FinaleHolyTall;
            if (p == 1) return holyMiddle + new Vector2(0f, tall);
            if (p == 2) return holyMiddle + new Vector2(-wide, tall * 0.2f);
            if (p == 3) return holyMiddle + new Vector2(wide, tall * 0.1f);
            return holyMiddle + new Vector2(0f, -tall);
        }

        //Draw Comets : the guiding stars coming down from the top star to the band's cards, each with its tail, landing on
        //the face (the card's ECHO look takes over there). Over the cards, additive.
        private void DrawComets(SpriteBatch sb)
        {
            bool any = false;
            for (int i = 0; i < comets.Length; i++)
                if (comets[i].On && comets[i].Wait <= 0f) any = true;
            if (!any) return;

            Vector2 from = OnScreen(HolyPoint(0));
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, null, null, null, Sharp.Transform);
            for (int i = 0; i < comets.Length; i++)
            {
                if (!comets[i].On || comets[i].Wait > 0f) continue;
                int slot = battle.Party.IndexOf(comets[i].To);
                if (slot < 0) continue;
                Rectangle face = CardFace(slot);
                Vector2 to = new Vector2(face.Center.X, face.Center.Y);
                float t = comets[i].Age / BattleRules.FinaleHolyFall;
                Vector2 head = CometAt(from, to, Smooth(t));
                Vector2 last = head;
                for (int k = 1; k <= 10; k++)                                                   // the tail, thinning and fading behind the head
                {
                    Vector2 p = CometAt(from, to, Smooth(Math.Max(0f, t - k * 0.035f)));
                    float fade = 1f - k / 11f;
                    Gfx.Line(sb, last, p, Palette.SpotHoly * (0.35f * fade), 2f + 6f * fade);
                    Gfx.Line(sb, last, p, Palette.SpotHolyHot * (0.85f * fade), 1f + 2f * fade);
                    last = p;
                }
                Gfx.DrawGlow(sb, head.X, head.Y, 34f, Palette.SpotHoly * 0.7f);
                Gfx.Star(sb, head.X, head.Y, 9f, Palette.SpotHolyHot);
                Gfx.Rect(sb, head.X - 1f, head.Y - 1f, 2f, 2f, Color.White);
            }
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, Sharp.Transform);
        }

        //Comet At : a guiding star's way to a card, a curve that swoops down over the stage and comes up onto the card
        private static Vector2 CometAt(Vector2 from, Vector2 to, float t)
        {
            Vector2 bend = (from + to) * 0.5f + new Vector2(0f, 110f);
            float u = 1f - t;
            return from * (u * u) + bend * (2f * u * t) + to * (t * t);
        }

        //On Screen : a point of the fighters' layer where the battle's camera shows it now (the same push as BeginCameraBatch)
        private Vector2 OnScreen(Vector2 p)
        {
            return Vector2.Transform(p, CameraPush());
        }

        //Draw Storm : STORM, rain driven across the stage from the right, the drops falling at their own speeds, thinning out
        //as the storm passes. Each drop is where its number and the clock put it (nothing is stored).
        private void DrawStorm(SpriteBatch sb)
        {
            float k = stormAge / StormTime;
            float a = Math.Min(1f, stormAge / 0.15f) * (1f - k * k);
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, PanelTop, Palette.LightningBlue * (0.05f * a));
            for (int i = 0; i < StormDrops; i++)
            {
                float speed = 900f + 600f * Hash(i, 11);
                float y = (Hash(i, 12) * (PanelTop + 80f) + stormAge * speed) % (PanelTop + 80f) - 40f;
                float x = (Hash(i, 13) * (TacetGame.ScreenW + 300f) - stormAge * speed * 0.45f) % (TacetGame.ScreenW + 300f);
                if (x < 0f) x += TacetGame.ScreenW + 300f;
                Gfx.Line(sb, new Vector2(x, y), new Vector2(x + 16f, y - 36f), Color.Lerp(Palette.LightningBlueHot, Color.White, 0.5f) * (0.5f * a), 1.5f);
            }
        }

        //Hash : a steady random 0..1 from two whole numbers (the same trick as TacetField)
        private static float Hash(int a, int b)
        {
            float h = (float)Math.Sin(a * 12.9898f + b * 78.233f) * 43758.547f;
            return h - (float)Math.Floor(h);
        }
    }
}
