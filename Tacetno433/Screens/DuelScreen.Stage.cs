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

        //Stand Rect : where a seat's musician stands. Whoever plays the beat being answered is
        //lifted a little, and whoever just played leans toward TACET.
        private Rectangle StandRect(int seat)
        {
            Rectangle r = StageLayout.DuelStandRect(seat);
            r.X += (int)(lit[seat] * 10f);
            if (UpNext(seat)) r.Y -= 4;
            return r;
        }

        //Up Next : this seat plays the beat being answered right now
        private bool UpNext(int seat)
        {
            return AnswerProgress() >= 0f && Game.CurrentRun.Formation.Plays(seat, bar * 4 + pending);
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
            float breathe = 1f + (float)Math.Sin(time * 1.6f) * 0.02f;

            Hollow.Eclipse(sb, cx, 250f, 96f * size * breathe, time * 0.3f, 1f);
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

            Gfx.Rect(sb, left, top, width, LaneHalf * 2f, Color.Black * 0.5f);
            Gfx.Rect(sb, left, top, width, 1, Palette.Paper * 0.25f);
            Gfx.Rect(sb, left, top + LaneHalf * 2f - 1f, width, 1, Palette.Paper * 0.25f);

            Gfx.Rect(sb, HitX - 3, top - 8, 6, LaneHalf * 2f + 16f, Palette.Ink);
            Gfx.Rect(sb, HitX - 1, top - 8, 2, LaneHalf * 2f + 16f, Palette.Highlight);
        }

        //Incoming : TACET's phrase on its way. Every note leaves the right edge on its beat of
        //the call and slides for exactly one bar, so its middle reaches the hit point on the
        //beat where it is answered, right inside the timing ring.
        //Each note is a hollow ring with its loudness written in it, the way music marks it:
        //   f   loud, a thick bright ring with a crown of sparks. TACET will BOOST.
        //   mf  plain.
        //   p   soft, a thin dim ring. TACET will EASE.
        //   ?   a hidden note, nobody can tell.
        //The small arrow over each note is the stroke that answers it. A dash is a beat where
        //nobody plays at all.
        private void DrawIncoming(SpriteBatch sb)
        {
            if (phase != Phase.Phrase) return;

            float fromX = TacetGame.ScreenW - 30f;
            bool echoFades = battle.EnemyHas(EnemyTrait.EchoFades);

            for (int k = 0; k < 4; k++)
            {
                if (k > called || k < pending) continue;          // not played yet, or already answered
                int n = bar * 4 + k;
                float t = MathHelper.Clamp((clock - k * beatLen) / (4f * beatLen), 0f, 1f);
                float x = MathHelper.Lerp(fromX, HitX, t);
                int power = battle.EnemyPower[n];

                if (power <= 0)
                {
                    //Silent Note : a dash, or a hollow mark with an arrow when we still play here
                    if (battle.HasAction(n))
                    {
                        Gfx.DiamondOutline(sb, x, RingY, 12f, Palette.Paper * 0.7f, 1.5f);
                        DrawDirection(sb, pattern[k], x, RingY, 14f, 5f, Palette.Highlight, 2f);
                    }
                    else
                    {
                        Gfx.Rect(sb, x - 8, RingY - 2, 16, 4, Palette.PaperDim * 0.6f);
                    }
                    continue;
                }

                bool hidden = battle.EnemyHidden[n];
                Choice shown = battle.ShownChoice[n];
                float size = hidden ? 0.55f : MathHelper.Clamp(power / 14f, 0.25f, 1f);
                float radius = 19f + size * 12f;
                float thick = 2.5f;
                float alpha = 1f;
                if (!hidden && shown == Choice.Boost) thick = 4.5f;
                if (!hidden && shown == Choice.Ease) { thick = 1.5f; alpha = 0.65f; }

                Hollow.Ring(sb, x, RingY, radius, thick, alpha);

                //Crown : a loud note bristles
                if (!hidden && shown == Choice.Boost)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        float angle = i * MathHelper.PiOver4 + time * 1.5f;
                        Vector2 d = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                        Gfx.Line(sb, new Vector2(x, RingY) + d * (radius + 3f), new Vector2(x, RingY) + d * (radius + 10f), Palette.Highlight, 2f);
                    }
                }

                //Mark : the loudness in the middle. ECHO FADES wipes it out before it arrives.
                float markAlpha = 1f;
                if (echoFades) markAlpha = MathHelper.Clamp(1f - (t - 0.35f) / 0.2f, 0f, 1f);
                string mark = hidden ? "?" : dynamicMark[(int)shown];
                float markSize = TextSize.Small * (hidden ? 0.9f : (shown == Choice.Boost ? 1.15f : (shown == Choice.Normal ? 0.72f : 0.9f)));
                Gfx.TextCentered(sb, Game.BigFont, mark, x, RingY - 2, Palette.Highlight * (alpha * markAlpha), markSize);

                //Arrow : the stroke that answers this note, just above the lane
                DrawDirection(sb, pattern[k], x, RingY - LaneHalf - 12f, 14f, 5f, Palette.Highlight * 0.9f, 2f);
            }
        }
    }
}
