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
    //DuelScreen.Stage : the world. The stage painting, the band standing side on, TACET's black
    //sun and its edge, the lane at the middle of the screen and the notes sliding along it.
    public partial class DuelScreen
    {
        //Stage Side : the painting for this era, or the plain bright stage until it exists.
        //ARTWORK : Content/Art/Stage/stage_<era>.png, see ArtBank.
        private void DrawStageSide(SpriteBatch sb)
        {
            ArtBank.DrawStage(sb, Game.CurrentRun.Era, new Rectangle(0, 0, TacetGame.ScreenW, TacetGame.ScreenH), StageLayout.DuelFloorY);
        }

        //Staff : five lines of music across the upper stage, from the hand's picture to TACET's
        //edge. Won beats set them ringing, see Core/StageStaff.cs. A little darker while shaking.
        private void DrawStaff(SpriteBatch sb, float edge)
        {
            float energy = staffEnergy + BeatPulse() * 0.12f;
            float ink = 0.14f + Math.Min(1f, staffEnergy) * 0.16f;
            StageStaff.Draw(sb, 232f, edge, 150f, energy, time, ink);
        }

        //Stand Rect : where a seat's musician stands. Whoever plays the beat being answered is
        //lifted a little, and whoever just played leans toward TACET.
        private Rectangle StandRect(int seat)
        {
            Rectangle r = StageLayout.DuelStandRect(seat);
            r.X += (int)(lit[seat] * 10f);
            if (UpNext(seat)) r.Y -= 4;
            return r;
        }

        //Up Next : this seat plays the beat being answered right now. In the finale, everyone does.
        private bool UpNext(int seat)
        {
            if (AnswerProgress() < 0f) return false;
            if (phase == Phase.Finale) return Game.CurrentRun.Formation.Seated[seat] != null;
            return Game.CurrentRun.Formation.Plays(seat, pending);
        }

        //Musician Glow : the light behind whoever is playing or about to. Drawn before the band
        //switches to pixel sampling, because a soft glow must stay smooth.
        private void DrawMusicianGlow(SpriteBatch sb)
        {
            Formation f = Game.CurrentRun.Formation;
            for (int s = 0; s < StageLayout.SeatCount; s++)
            {
                if (f.Seated[s] == null) continue;
                float glow = lit[s];
                if (UpNext(s)) glow = Math.Max(glow, 0.35f + (float)Math.Sin(time * 8f) * 0.15f);
                if (glow <= 0f) continue;

                Rectangle r = StandRect(s);
                Gfx.DrawGlow(sb, r.Center.X, r.Center.Y, r.Height * 0.62f, Palette.Highlight * (0.6f * glow));
            }
        }

        //Musicians : the band standing side on, facing TACET. Drawn far to near, so the nearer
        //players overlap the ones behind them.
        //ARTWORK : Content/Art/Musicians/<name>_idle_0.png and friends, see CharacterArt.
        //Pixel art is drawn at one whole number scale for everybody; players further back are
        //a shade darker instead of smaller, so no pixel is ever squashed.
        private void DrawMusicians(SpriteBatch sb)
        {
            Formation f = Game.CurrentRun.Formation;

            for (int i = 0; i < StageLayout.SeatCount; i++)
            {
                int s = StageLayout.DuelDrawOrder[i];
                Musician m = f.Seated[s];
                if (m == null) continue;

                int depth = s % 3;
                float shade = 0.82f + depth * 0.09f;
                Color tint = new Color(shade, shade, shade);
                CharacterArt.Draw(sb, m, actors[s].Anim, actors[s].Frame, StandRect(s), CharacterArt.PixelScale,
                                  tint, Palette.Ink, 0.7f + depth * 0.15f);
            }
        }

        //Musician Marks : a small diamond over whoever plays the beat being answered
        private void DrawMusicianMarks(SpriteBatch sb)
        {
            Formation f = Game.CurrentRun.Formation;
            for (int s = 0; s < StageLayout.SeatCount; s++)
            {
                if (f.Seated[s] == null || !UpNext(s)) continue;
                Rectangle r = StandRect(s);
                Gfx.Diamond(sb, r.Center.X, r.Y - 14, 5, Palette.Ink);
            }
        }

        //Tacet Side : everything in the dark. TACET's black sun with its white rim, the holes
        //of silence drifting up, the ripples of its notes on the floor, and its shape in front.
        //ARTWORK : Content/Art/Enemies/enemy_<name>.png, see ArtBank. The eclipse is an effect
        //and stays behind the picture once it arrives.
        private void DrawTacetSide(SpriteBatch sb, float edge, float danger)
        {
            Enemy e = battle.Enemy;
            float size = e.Kind == EnemyKind.Boss ? 1.3f : (e.Kind == EnemyKind.Elite ? 1.15f : 1f);
            float cx = EnemyX();
            float breathe = 1f + (float)Math.Sin(time * 1.6f) * 0.02f + BeatPulse() * 0.025f;

            float sun = 96f * size * breathe;
            Hollow.Eclipse(sb, cx, 250f, sun, time * 0.3f, 1f);

            //Call Flare : rays shoot out of the sun's rim whenever TACET plays a note, longer for f
            if (callPulse > 0f)
                Ornament.Rays(sb, cx, 250f, sun + 10f, sun + 30f + callPulse * 70f, 24, time * 0.2f, Palette.Highlight * (0.45f * callPulse));
            effects.DrawMotes(sb, edge, danger, time);
            effects.DrawRipples(sb, true);

            int w = (int)(200 * size);
            int h = (int)(300 * size);
            Rectangle shape = new Rectangle((int)cx - w / 2, (int)EnemyFeetY - h, w, h);
            ArtBank.DrawOrSlot(sb, ArtBank.EnemyOf(e), shape, Palette.Paper, 1f);
        }

        //Lane : a dark strip from the hit point to the right edge. TACET's notes always slide
        //over the same dark ground, wherever the line between the two sides happens to be,
        //and the bright bar is the hit point every note has to be answered on.
        private void DrawLane(SpriteBatch sb)
        {
            float left = HitX;
            float top = RingY - LaneHalf;
            float width = TacetGame.ScreenW - left;

            float pulse = BeatPulse();

            //Lane Edges : brighten a little on every beat, so the tempo can be seen along the lane
            Gfx.Rect(sb, left, top, width, LaneHalf * 2f, Color.Black * 0.5f);
            Gfx.Rect(sb, left, top, width, 1, Palette.Paper * (0.25f + pulse * 0.3f));
            Gfx.Rect(sb, left, top + LaneHalf * 2f - 1f, width, 1, Palette.Paper * (0.25f + pulse * 0.3f));

            //LANE FLASH : a good hit lights the lane from the hit point outward and fades, grown out of
            //the tap effects of Project Sekai. Eight steps, each a little fainter.
            if (laneFlash > 0f)
            {
                for (int i = 0; i < 8; i++)
                {
                    float fade = 1f - i / 8f;
                    Gfx.Rect(sb, left + i * 70f, top + 1, 70f, LaneHalf * 2f - 2f, Palette.Highlight * (0.22f * laneFlash * fade));
                }
            }

            //Beat Lines : a line for every beat slides down the lane with the notes, a stronger
            //one for every bar, the way Taiko no Tatsujin shows its bars. The eye can count along.
            if (phase == Phase.Play)
            {
                for (int j = 0; j < BattleRules.BeatsPerRound + 6; j++)
                {
                    float sent = j * beatLen;
                    if (clock < sent || clock > sent + 4f * beatLen) continue;
                    float lx = LaneX(sent);
                    bool barLine = j % 4 == 0;
                    Gfx.Rect(sb, lx - (barLine ? 1f : 0.5f), top + 2, barLine ? 2f : 1f, LaneHalf * 2f - 4f,
                             Palette.Paper * (barLine ? 0.4f : 0.16f));
                }
            }

            //Hit Point : the dark well and the bright bar, swelling a little on every beat.
            //TACET's torn edge flickers all the time and often passes right behind the hit point,
            //so the well gives the one spot the eye has to watch a steady ground (Core/NoteGlyph.cs).
            NoteGlyph.HitPoint(sb, HitX, RingY, LaneHalf, pulse);
        }

        //Lane Spot : where a note sent down the lane at this moment is now. Every note takes one
        //bar to go from TACET's edge to the hit point, and stops there.
        private float LaneX(float sentAt)
        {
            float t = MathHelper.Clamp((clock - sentAt) / (4f * beatLen), 0f, 1f);
            return MathHelper.Lerp(TacetGame.ScreenW - 30f, HitX, t);
        }

        //Incoming : TACET's part on its way. Every note leaves the right edge on its beat of
        //the call and slides for exactly one bar, so its middle reaches the hit point on the
        //beat where it is answered, right inside the timing ring.
        //Each note is a hollow ring with its loudness written in it, the way music marks it:
        //   f   loud, a thick bright ring with a crown of sparks. TACET will BOOST.
        //   mf  plain.
        //   p   soft, a thin dim ring. TACET will EASE.
        //   ?   a hidden note, nobody can tell.
        //   tr  TACET's roll, with a zigzag bar as long as the roll lasts.
        //The pointer on the ring's edge is the way to swing, the same as the answer ring shows.
        //The note to answer next is bright, the next one a little dimmer, the rest faint.
        //A pair (from floor two) is a note tied to a spark: one more flick, any way, on the spark.
        //A note under an arch (from floor two) is held still after its stroke.
        //A dash is a beat where nobody plays at all.
        private void DrawIncoming(SpriteBatch sb)
        {
            if (phase != Phase.Play) return;

            bool echoFades = battle.EnemyHas(EnemyTrait.EchoFades);

            for (int n = 0; n < BattleRules.BeatsPerRound; n++)
            {
                if (n > called || n < pending) continue;          // not played yet, or already answered
                float x = LaneX(n * beatLen);
                float t = MathHelper.Clamp((clock - n * beatLen) / (4f * beatLen), 0f, 1f);
                int power = battle.EnemyPower[n];
                bool firstDone = n == pending && (onGrace || rolling);

                //Pair : a ribbon from the note back to its spark, half a beat behind
                if (battle.EnemyDouble[n] && n <= graceCalled)
                {
                    float gx = LaneX((n + 0.5f) * beatLen);
                    if (gx > x + 2f)
                    {
                        Gfx.Rect(sb, x, RingY - 9f, gx - x, 18f, Palette.Paper * 0.3f);
                        Gfx.Rect(sb, x, RingY - 9f, gx - x, 2f, Palette.Paper * 0.8f);
                        Gfx.Rect(sb, x, RingY + 7f, gx - x, 2f, Palette.Paper * 0.8f);
                    }
                    NoteGlyph.Spark(sb, gx, RingY, 14f, Palette.Highlight * Focus(n));
                }

                //Roll : the tail runs back up the lane for as long as the roll lasts
                if (battle.IsTremolo(n))
                {
                    float tailEnd = LaneX((n + BattleRules.TremoloBeats) * beatLen);
                    NoteGlyph.RollBar(sb, x, tailEnd, RingY);
                }

                //Fermata : a wide ribbon for as long as the note is held, and the fermata sign on top
                if (battle.IsFermata(n))
                {
                    float holdX = LaneX((n + BattleRules.FermataBeats) * beatLen);
                    NoteGlyph.HoldRibbon(sb, x, holdX, RingY);
                    if (!holding) NoteGlyph.FermataSign(sb, x, RingY - 30f, 14f, Palette.Highlight);
                }

                if (holding && n == pending) continue;             // the hold draws itself at the hit point
                if (firstDone && !rolling) continue;               // the first of the pair is answered

                float focus = Focus(n);

                if (power <= 0)
                {
                    //Silent Note : a dash, or a hollow diamond with its pointer when we still play here
                    if (battle.HasAction(n))
                    {
                        Gfx.DiamondOutline(sb, x, RingY, 14f, Palette.Paper * (0.8f * focus), 2f);
                        NoteGlyph.Pointer(sb, pattern[n % 4], x, RingY, 14f, focus);
                    }
                    else
                    {
                        Gfx.Rect(sb, x - 8, RingY - 2, 16, 4, Palette.PaperDim * 0.6f);
                    }
                    continue;
                }

                bool hidden = battle.EnemyHidden[n];
                bool roll = battle.IsTremolo(n);
                Choice shown = battle.ShownChoice[n];
                bool plain = !hidden && !roll;
                float size = plain ? MathHelper.Clamp(power / 14f, 0.25f, 1f) : 0.6f;
                float radius = (19f + size * 12f) * (0.85f + 0.25f * t);     // a note grows as it comes closer
                float thick = 2.5f;
                float alpha = focus;
                if (plain && shown == Choice.Boost) thick = 4.5f;
                if (plain && shown == Choice.Ease) { thick = 1.5f; alpha *= 0.75f; }

                Hollow.Ring(sb, x, RingY, radius, thick, alpha);

                //Crown : a loud note bristles
                if (plain && shown == Choice.Boost) NoteGlyph.Crown(sb, x, RingY, radius, time, alpha);

                //Letter : how loud TACET plays it. ECHO FADES wipes it out before it arrives.
                float markAlpha = 1f;
                if (echoFades && !roll) markAlpha = MathHelper.Clamp(1f - (t - 0.35f) / 0.2f, 0f, 1f);
                string mark = roll ? "tr" : (hidden ? "?" : dynamicMark[(int)shown]);
                float markSize = TextSize.Small * (plain ? (shown == Choice.Boost ? 1.15f : (shown == Choice.Normal ? 0.72f : 0.9f)) : 0.9f);
                Gfx.TextCentered(sb, Game.BigFont, mark, x, RingY - 2, Palette.Highlight * (alpha * markAlpha), markSize);

                //Pointer : the way to swing. A roll takes any way, so it has none.
                if (!roll) NoteGlyph.Pointer(sb, pattern[n % 4], x, RingY, radius, focus);
            }
        }

        //Focus : the note to answer next is bright, the one after it a little dimmer, the rest faint
        private float Focus(int n)
        {
            if (n <= pending) return 1f;
            if (n == pending + 1) return 0.75f;
            return 0.5f;
        }

    }
}
