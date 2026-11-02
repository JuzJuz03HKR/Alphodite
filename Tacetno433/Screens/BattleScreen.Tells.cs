using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Battle;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //BattleScreen Tells (Beta1 block D, 2 Nov, docs/BETA1_DIRECTION.md 37.3 and 38) : what the moves look like on the bodies
    //and on the band's cards, so a player reads them without the words that pop. Only the look : the rules are untouched.
    //   ARMOR      black plates with a pale rim float round an enemy with ARMOR (Fighter.Block), one plate for each share
    //              of what it put up. A blow on the armor chips one off (Blocked); when it runs out the last ones fall;
    //              IL CANNONE's PIERCE blows them out of the back.
    //   CHARGE     an enemy wound up for its charged blow (Fighter.Charge 2) draws black shards into its white heart, the
    //              heart flickers faster and a ring tightens round it. Kept apart from the look of a hit (37.2 : the
    //              charge gathers in, a hit bursts out).
    //   BREAK      its CHARGE broken : the power bursts out as white splinters (HitFx.Shatter)
    //   STUNNED    three small stars turn round its head while it is STUNNED (Fighter.Staggered)
    //   THE TURN OF THE HAND   a winding key turns over its head and a note drops off (the note it lost)
    //   CARDS      the band's skills on the cards of the ones they reach : soft round motes rising for a heal (with a
    //              ribbon of wind for SOFT AIR), a lens closing round the face for THE LESSON (kept while it waits), a
    //              curved shell of glass for SHIELD, gold arrows rising for VOLUME, a drum skin for THUNDER CALL's GUARD
    //              (its call rolls out to every enemy on the floor), rings of a horn for THE HUNTING CALL, a flame
    //              licking the card for life paid (THE DEVIL'S BARGAIN, THE PACT).
    //Colours (37.2) : TACET white on black, the band's calls in the colour of the skill's kind (the stripe on the button),
    //the one who gave it in their own colour where it helps. Sizes and times : Data/BattleRules.cs, Tell Feel.
    public partial class BattleScreen
    {
        //Tell : one enemy's tells, beside its Look
        private class Tell
        {
            public int Plates;            // ARMOR plates standing now (0..MostPlates)
            public float Full;            // the ARMOR it had when the plates went up : each plate stands for a share of it
            public float Rise;            // 0..1 the plates coming in after ARMOR UP
            public float Chip;            // 1 = a plate just took a blow, fades
            public float Drop;            // 0 = nothing falling, 0..1 while plates fall or fly off
            public int Dropping;          // how many fall
            public int DropFrom;          // the first slot of the ones falling
            public bool Blown;            // PIERCE : they fly out of the back instead of falling
            public Color BlownTint;       //   in the colour of the blast
            public float Charge;          // 0..1 how wound up it looks (eases to its CHARGE)
            public float Stun;            // 0..1 the stars (eases in and out with STUNNED)
            public float Turn;            // THE TURN OF THE HAND : 1 = the key just turned, fades
        }
        private readonly Dictionary<Fighter, Tell> tells = new Dictionary<Fighter, Tell>();
        private const int MostPlates = 6;

        //Card Fx : a skill seen on a band card for a moment
        private enum CardLook { Heal, Breeze, Lesson, Shield, Volume, Guard, Call, Price }
        private struct CardFx
        {
            public bool On;
            public CardLook Look;
            public Fighter Who;
            public float Age;
            public Color Tint;            // the one who gave it (the ribbon of wind, the horn's rings, the flame)
        }
        private readonly CardFx[] cardFx = new CardFx[12];

        private Tell TellOf(Fighter f)
        {
            Tell t;
            if (!tells.TryGetValue(f, out t))
            {
                t = new Tell();
                tells[f] = t;
            }
            return t;
        }

        //Plates For : how many plates the ARMOR it has now stands for (at least one while it has any)
        private static int PlatesFor(Fighter f, Tell t)
        {
            if (f.Down || f.Block <= 0) return 0;
            float share = f.Block / Math.Max(1f, t.Full);
            return (int)MathHelper.Clamp((float)Math.Ceiling(share * MostPlates), 1f, MostPlates);
        }

        //======================================================================================
        //Events : ShowEvents hands each of these moments here
        //======================================================================================

        //Tell Event : the look of one event on its fighter. Returns nothing, the words still pop as before.
        private void TellEvent(TurnEvent e)
        {
            if (e.Who == null) return;
            if (e.Who.IsEnemy)
            {
                Tell t = TellOf(e.Who);
                if (e.Kind == TurnEventKind.Guard)                                          // ARMOR UP : the plates come in
                {
                    t.Full = Math.Max(1f, e.Who.Block);
                    t.Plates = PlatesFor(e.Who, t);
                    t.Rise = 0.0001f;
                }
                else if (e.Kind == TurnEventKind.Blocked)                                   // a blow on the armor : a plate chips
                {
                    t.Chip = 1f;
                    if (t.Plates > 0) hits.Splinter(PlatePoint(e.Who, t, t.Plates - 1), SpotOf(hitBy));
                }
                else if (e.Kind == TurnEventKind.Pierce)                                    // PIERCE : blown out of the back
                {
                    t.Blown = true;
                    t.BlownTint = SpotOf(hitBy).Tint;
                    StartDrop(t, 0, t.Plates);
                    t.Plates = 0;
                }
                else if (e.Kind == TurnEventKind.Broken)                                    // BREAK : the power bursts out
                {
                    t.Charge = 0f;
                    Figure figure = ArtOf(e.Who).Figure;
                    Rectangle box = DrawBox(e.Who);
                    Vector2 heart = figure != null ? figure.Heart : new Vector2(box.Center.X, box.Center.Y);
                    hits.Shatter(heart, BattleRules.HitSize * BattleRules.TellBreakSize);
                }
                else if (e.Kind == TurnEventKind.Cut) t.Turn = 1f;                          // THE TURN OF THE HAND
                return;
            }

            //The band's cards
            Color by = hitBy != null && !hitBy.IsEnemy ? SpotOf(hitBy).Tint : Palette.Paper;
            Skill playing = PlayingSkill;
            bool breeze = playing != null && hitBy != null && SkillStyleName(hitBy, playing) == "SOFT AIR";
            if (e.Kind == TurnEventKind.Heal) AddCardFx(breeze ? CardLook.Breeze : CardLook.Heal, e.Who, by);
            else if (e.Kind == TurnEventKind.Lesson) AddCardFx(CardLook.Lesson, e.Who, by);
            else if (e.Kind == TurnEventKind.Shield) AddCardFx(CardLook.Shield, e.Who, by);
            else if (e.Kind == TurnEventKind.Crescendo) AddCardFx(CardLook.Volume, e.Who, by);
            else if (e.Kind == TurnEventKind.Cue) AddCardFx(CardLook.Call, e.Who, by);
            else if (e.Kind == TurnEventKind.GuardUp)
            {
                AddCardFx(CardLook.Guard, e.Who, SpotOf(e.Who).Tint);
                //THUNDER CALL : the call rolls out over the floor to every enemy, the nearest first
                for (int i = 0; i < battle.Enemies.Count; i++)
                {
                    Fighter foe = battle.Enemies[i];
                    if (foe.Down) continue;
                    Rectangle box = DrawBox(foe);
                    hits.Rumble(new Vector2(box.Center.X, box.Bottom - 6f), BattleRules.HitSize * 1.6f, 0.06f + 0.09f * i, SpotOf(e.Who).Tint);
                }
            }
            else if (e.Kind == TurnEventKind.Damage && e.Who == hitBy)                     // life paid for a skill (it hurt itself)
                AddCardFx(CardLook.Price, e.Who, SpotOf(e.Who).Tint);
        }

        //Skill Style Name : the plain name under a skill's button (a legend form renames it, SkillStyle)
        private static string SkillStyleName(Fighter f, Skill s)
        {
            if (f == null || f.IsEnemy || f.Musician == null || s == null) return "";
            int k = s == SkillList.First(f.Musician) ? 0 : (s == SkillList.Second(f.Musician) ? 1 : -1);
            return k < 0 ? "" : SkillList.Plain(f.Musician, k).Name;
        }

        private void AddCardFx(CardLook look, Fighter who, Color tint)
        {
            int slot = 0;
            float oldest = -1f;
            for (int i = 0; i < cardFx.Length; i++)
            {
                if (!cardFx[i].On) { slot = i; break; }
                if (cardFx[i].Age > oldest) { oldest = cardFx[i].Age; slot = i; }
            }
            cardFx[slot] = new CardFx { On = true, Look = look, Who = who, Age = 0f, Tint = tint };
        }

        private static void StartDrop(Tell t, int from, int count)
        {
            if (count <= 0) return;
            t.DropFrom = from;
            t.Dropping = count;
            t.Drop = 0.0001f;
        }

        //======================================================================================
        //Update
        //======================================================================================

        private void UpdateTells(float dt)
        {
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                Fighter f = battle.Enemies[i];
                Tell t = TellOf(f);
                int want = PlatesFor(f, t);
                if (want < t.Plates)                                                         // ARMOR shrank : the plates past it break and fall
                {
                    for (int k = want; k < t.Plates; k++) hits.Splinter(PlatePoint(f, t, k), HitColors.Plain);
                    t.Blown = false;
                    StartDrop(t, want, t.Plates - want);
                    t.Plates = want;
                }
                else if (want > t.Plates)
                {
                    if (t.Full < f.Block) t.Full = f.Block;
                    t.Plates = PlatesFor(f, t);
                    if (t.Rise <= 0f) t.Rise = 0.0001f;
                }
                if (t.Rise > 0f) t.Rise = Math.Min(1f, t.Rise + dt / BattleRules.TellPlateRise);
                if (t.Rise >= 1f) t.Rise = 0f;                                               // 0 = settled
                if (t.Drop > 0f)
                {
                    t.Drop += dt / BattleRules.TellPlateFall;
                    if (t.Drop >= 1f) { t.Drop = 0f; t.Dropping = 0; }
                }
                t.Chip = Math.Max(0f, t.Chip - dt * 4f);
                t.Turn = Math.Max(0f, t.Turn - dt / BattleRules.TellTurnTime);
                float charged = !f.Down && f.Charge >= 2 ? 1f : 0f;
                t.Charge = MathHelper.Clamp(t.Charge + Math.Sign(charged - t.Charge) * dt * 3f, 0f, 1f);
                float stunned = !f.Down && f.Staggered ? 1f : 0f;
                t.Stun = MathHelper.Clamp(t.Stun + Math.Sign(stunned - t.Stun) * dt * 4f, 0f, 1f);
            }
            for (int i = 0; i < cardFx.Length; i++)
            {
                if (!cardFx[i].On) continue;
                cardFx[i].Age += dt;
                if (cardFx[i].Age > BattleRules.TellCardTime) cardFx[i].On = false;
            }
        }

        //======================================================================================
        //ARMOR plates (the fighters' pass, Shapes, after TACET's figures)
        //======================================================================================

        //Plate Ring : the middle and size of the ring the plates float on
        private void PlateRing(Fighter f, out Vector2 middle, out float radius)
        {
            Rectangle box = DrawBox(f);
            Figure figure = ArtOf(f).Figure;
            middle = figure != null ? Vector2.Lerp(new Vector2(box.Center.X, box.Center.Y), figure.Heart, 0.5f) : new Vector2(box.Center.X, box.Center.Y);
            radius = 0.5f * Math.Max(box.Width, box.Height) * BattleRules.TellPlateReach;
        }

        //Plate Angle : the middle of plate k on its ring. They float round slowly and spread all the way round.
        private float PlateAngle(int k)
        {
            return MathHelper.Pi + (k + 0.5f) * MathHelper.TwoPi / MostPlates + worldTime * BattleRules.TellPlateSpin;
        }

        private Vector2 PlatePoint(Fighter f, Tell t, int k)
        {
            Vector2 middle;
            float radius;
            PlateRing(f, out middle, out radius);
            float a = PlateAngle(k);
            return middle + new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * radius;
        }

        //Draw Plates : every enemy's standing plates, then the ones falling or flying off
        private void DrawTellPlates()
        {
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                Fighter f = battle.Enemies[i];
                Tell t;
                if (!tells.TryGetValue(f, out t) || (t.Plates == 0 && t.Dropping == 0)) continue;
                float alpha, light, flash, mark;
                FigureLookOf(f, out alpha, out light, out flash, out mark);
                if (alpha <= 0f) continue;
                Vector2 middle;
                float radius;
                PlateRing(f, out middle, out radius);
                float half = MathHelper.Pi / MostPlates * BattleRules.TellPlateWidth;
                float thick = BattleRules.TellPlateThick;
                float rise = t.Rise > 0f ? Smooth(t.Rise) : 1f;

                for (int k = 0; k < t.Plates; k++)
                {
                    float a = PlateAngle(k);
                    float r = radius * (1.6f - 0.6f * rise) + (float)Math.Sin(worldTime * 2.1f + k * 1.7f) * 2f;   // coming in, then a slow bob
                    bool chipped = k == t.Plates - 1 && t.Chip > 0f;
                    DrawPlate(middle, r, a, half, thick, alpha * rise, chipped ? t.Chip : 0f, Palette.Paper);
                }
                if (t.Dropping > 0 && t.Drop > 0f)
                {
                    float d = t.Drop;
                    for (int k = t.DropFrom; k < t.DropFrom + t.Dropping && k < MostPlates; k++)
                    {
                        float a = PlateAngle(k);
                        Vector2 at = middle;
                        float turn;
                        if (t.Blown)                                                         // PIERCE : out of the back, fast
                        {
                            at += new Vector2(BattleRules.TellPlateBlow * d * (0.8f + 0.1f * k), -40f * d + 90f * d * d);
                            turn = d * (2f + k);
                        }
                        else                                                                 // fall to the floor, tumbling
                        {
                            at += new Vector2(14f * d * (k % 2 == 0 ? 1f : -1f), 260f * d * d);
                            turn = d * (1.2f + 0.4f * k) * (k % 2 == 0 ? 1f : -1f);
                        }
                        DrawPlate(at, radius, a + turn, half, thick, alpha * (1f - d * d), 1f - d, t.Blown ? t.BlownTint : Palette.Paper);   // the rim flares as it breaks
                    }
                }
            }
        }

        //Draw Plate : one plate, a piece of a black shell bent round the body, a pale rim on its outer edge and a thin dark one
        //inside. lit (0..1) brightens the rim in tint (a chip, a blast).
        private static void DrawPlate(Vector2 middle, float radius, float angle, float half, float thick, float alpha, float lit, Color tint)
        {
            if (alpha <= 0.01f) return;
            Color body = new Color(30, 30, 36) * (0.96f * alpha);                           // a dark grey, so a plate reads on the hole's black
            Color rim = Color.Lerp(new Color(226, 226, 232), Color.Lerp(tint, Color.White, 0.4f), lit) * (alpha * (0.85f + 0.15f * lit));
            Shapes.Arc(middle, radius, angle - half, angle + half, thick, thick, body);
            Shapes.Arc(middle, radius + thick * 0.5f, angle - half * 0.96f, angle + half * 0.96f, 2.4f + 1.6f * lit, 2.4f + 1.6f * lit, rim);   // reads on the hole's black (HOLE LIGHT)
            Shapes.Arc(middle, radius - thick * 0.5f, angle - half * 0.9f, angle + half * 0.9f, 1f, 1f, new Color(120, 120, 128) * (0.5f * alpha));
        }

        //======================================================================================
        //CHARGE shards (the fighters' pass) and the glows (the GLOW pass)
        //======================================================================================

        //Draw Charge Shards : black shards drawn in from round the body into its heart, faster and nearer as it winds up
        private void DrawTellShards()
        {
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                Fighter f = battle.Enemies[i];
                Tell t;
                if (!tells.TryGetValue(f, out t) || t.Charge <= 0.01f) continue;
                Figure figure = ArtOf(f).Figure;
                if (figure == null) continue;
                float alpha, light, flash, mark;
                FigureLookOf(f, out alpha, out light, out flash, out mark);
                Rectangle box = DrawBox(f);
                Vector2 heart = figure.Heart;
                float reach = 0.5f * Math.Max(box.Width, box.Height) * BattleRules.TellChargeReach;
                for (int k = 0; k < 12; k++)
                {
                    float phase = (worldTime * BattleRules.TellChargePull + k / 12f) % 1f;  // 0 far out .. 1 in the heart
                    float a = k * 2.399f + phase * 1.6f;                                     // a turn as it is drawn in
                    float r = reach * (1f - phase) + 4f;
                    Vector2 at = heart + new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * r;
                    float size = 4f + 5f * (1f - phase);
                    float fade = Smooth(Math.Min(1f, phase * 4f)) * (1f - phase * phase) * t.Charge * alpha;
                    Shapes.Polygon(at, size, 3, a + phase * 3f, new Color(8, 8, 10) * fade);
                    Shapes.Arc(at, size * 0.9f, a, a + 1.4f, 1.4f, 1.4f, new Color(210, 210, 216) * (0.85f * fade));
                }
            }
        }

        //Draw Tell Glows : the CHARGE's heart and ring, the STUNNED stars, THE TURN OF THE HAND's key (additive, the fighters' layer)
        private void DrawTellGlows(SpriteBatch sb)
        {
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                Fighter f = battle.Enemies[i];
                Tell t;
                if (!tells.TryGetValue(f, out t)) continue;
                float alpha, light, flash, mark;
                FigureLookOf(f, out alpha, out light, out flash, out mark);
                if (alpha <= 0f) continue;
                Rectangle box = DrawBox(f);
                Figure figure = ArtOf(f).Figure;

                if (t.Plates > 0)                                                            // ARMOR : a faint light along each plate's rim
                {
                    Vector2 middle;
                    float radius;
                    PlateRing(f, out middle, out radius);
                    for (int k = 0; k < t.Plates; k++)
                    {
                        float a = PlateAngle(k);
                        Vector2 at = middle + new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * (radius + BattleRules.TellPlateThick * 0.5f);
                        Gfx.DrawGlow(sb, at.X, at.Y, 16f, Color.White * (0.16f * alpha * (k == t.Plates - 1 ? 1f + 2f * t.Chip : 1f)));
                    }
                }

                if (t.Charge > 0.01f)                                                        // CHARGE : the heart flickers, a ring tightens
                {
                    Vector2 heart = figure != null ? figure.Heart : new Vector2(box.Center.X, box.Center.Y);
                    float reach = 0.5f * Math.Max(box.Width, box.Height) * BattleRules.TellChargeReach;
                    float beat = 0.5f + 0.5f * (float)Math.Sin(worldTime * BattleRules.TellChargeBeat);
                    Gfx.DrawGlow(sb, heart.X, heart.Y, 30f + 14f * beat, Color.White * (0.85f * t.Charge * alpha * (0.45f + 0.55f * beat) * Flashing));
                    for (int k = 0; k < 2; k++)
                    {
                        float p = (worldTime * 1.3f + k * 0.5f) % 1f;                       // each ring closes in, again and again
                        float r = reach * (1.15f - 0.95f * p);
                        float a = (float)Math.Sin(p * MathHelper.Pi) * 0.8f * t.Charge * alpha;
                        Gfx.EllipseOutline(sb, heart.X, heart.Y, r, r * 0.92f, Color.White * a, 2.5f);
                    }
                }

                if (t.Stun > 0.01f)                                                          // STUNNED : stars round the head
                {
                    Vector2 head = figure != null ? figure.Heart - new Vector2(0f, figure.Height * 0.32f) : new Vector2(box.Center.X, box.Y + 30f);
                    float rx = Math.Max(22f, box.Width * 0.32f);
                    for (int k = 0; k < 3; k++)
                    {
                        float a = worldTime * BattleRules.TellStunSpin + k * MathHelper.TwoPi / 3f;
                        float depth = 0.6f + 0.4f * (float)Math.Sin(a);                     // nearer stars bigger and brighter
                        Vector2 at = head + new Vector2((float)Math.Cos(a) * rx, (float)Math.Sin(a) * 9f);
                        float s = BattleRules.TellStunSize * depth;
                        Color star = new Color(255, 232, 170) * (t.Stun * alpha * depth);
                        Gfx.Line(sb, at - new Vector2(s, 0f), at + new Vector2(s, 0f), star, 2.5f);
                        Gfx.Line(sb, at - new Vector2(0f, s), at + new Vector2(0f, s), star, 2.5f);
                        Gfx.DrawGlow(sb, at.X, at.Y, s * 2.2f, star * 0.5f);
                    }
                }

                if (t.Turn > 0.01f)                                                          // THE TURN OF THE HAND : a key turns, a note drops
                {
                    float done = 1f - t.Turn;
                    Vector2 at = new Vector2(box.Center.X, box.Y - 30f);
                    float a = Smooth(Math.Min(1f, done * 2f)) * MathHelper.Pi;              // half a turn
                    Vector2 way = new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * 12f;
                    Color key = Color.Lerp(Palette.KindAttack, Color.White, 0.3f) * (t.Turn * alpha);
                    Gfx.EllipseOutline(sb, at.X, at.Y, 14f, 14f, key, 2f);
                    Gfx.Line(sb, at - way, at + way, key, 3f);
                    Vector2 note = at + new Vector2(18f, 4f + 70f * done * done);           // the lost note falls away
                    Color fallen = Color.White * (t.Turn * alpha);
                    Gfx.DrawGlow(sb, note.X, note.Y, 9f, fallen * 0.6f);
                    Gfx.Line(sb, note + new Vector2(4f, 0f), note + new Vector2(4f, -14f), fallen, 2f);
                }
            }
        }

        //======================================================================================
        //CARDS (the screen's pass, after DrawBand)
        //======================================================================================

        private void DrawCardTells(SpriteBatch sb)
        {
            //THE LESSON kept : a thin gold lens round the face until the skill it helps is played
            for (int i = 0; i < battle.Party.Count; i++)
            {
                Fighter f = battle.Party[i];
                if (!f.Lesson || f.Down) continue;
                Rectangle face = CardFace(i);
                float r = face.Width * 0.56f;
                Gfx.EllipseOutline(sb, face.Center.X, face.Center.Y, r, r, Palette.KindBoost * 0.75f, 1.5f);
                for (int k = 0; k < 4; k++)
                {
                    float a = time * 0.8f + k * MathHelper.PiOver2;
                    Vector2 w = new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
                    Gfx.Line(sb, new Vector2(face.Center.X, face.Center.Y) + w * (r - 3f), new Vector2(face.Center.X, face.Center.Y) + w * (r + 4f), Palette.KindBoost, 2f);
                }
            }

            for (int n = 0; n < cardFx.Length; n++)
            {
                if (!cardFx[n].On) continue;
                int slot = battle.Party.IndexOf(cardFx[n].Who);
                if (slot < 0) continue;
                Rectangle card = ChipBox(slot);
                Rectangle face = CardFace(slot);
                float t = cardFx[n].Age / BattleRules.TellCardTime;                         // 0..1
                float fade = 1f - t * t;
                Vector2 c = new Vector2(face.Center.X, face.Center.Y);
                switch (cardFx[n].Look)
                {
                    case CardLook.Breeze:                                                    // SOFT AIR : a ribbon of wind across the card
                        {
                            float head = card.X - 20f + (card.Width + 60f) * Smooth(t);
                            Vector2 last = Vector2.Zero;
                            for (int k = 0; k <= 16; k++)
                            {
                                float x = head - k * 7f;
                                if (x < card.X || x > card.Right) { last = Vector2.Zero; continue; }
                                Vector2 p = new Vector2(x, card.Center.Y + (float)Math.Sin(x * 0.07f + t * 9f) * 9f);
                                if (last != Vector2.Zero) Gfx.Line(sb, last, p, cardFx[n].Tint * (fade * (1f - k / 17f)), 2f);
                                last = p;
                            }
                            goto case CardLook.Heal;
                        }
                    case CardLook.Heal:                                                      // soft round motes rising, no corners
                        Gfx.DrawGlow(sb, c.X, c.Y, face.Width * 0.8f, Palette.KindHeal * (0.35f * fade));
                        for (int k = 0; k < 7; k++)
                        {
                            float p = (t * 1.3f + k * 0.15f) % 1f;
                            float x = face.X + 6f + (k * 13 % (face.Width - 8));
                            float y = face.Bottom - p * (face.Height + 16f);
                            Gfx.DrawGlow(sb, x, y, 5f - 2f * p, Palette.KindHeal * (fade * (1f - p)));
                        }
                        break;

                    case CardLook.Lesson:                                                    // THE LESSON : a lens closes round the face
                        {
                            float r = MathHelper.Lerp(face.Width * 0.95f, face.Width * 0.56f, Smooth(Math.Min(1f, t * 2f)));
                            Gfx.EllipseOutline(sb, c.X, c.Y, r, r, Palette.KindBoost * fade, 2.5f);
                            Gfx.DrawGlow(sb, c.X, c.Y, face.Width * 0.7f, Palette.KindBoost * (0.3f * fade));
                        }
                        break;

                    case CardLook.Shield:                                                    // SHIELD : a curved shell of glass over the face
                        {
                            float grow = Smooth(Math.Min(1f, t * 3f));
                            Gfx.EllipseOutline(sb, c.X, c.Y, face.Width * 0.62f * grow, face.Height * 0.7f * grow, Palette.KindDefend * fade, 2.5f);
                            Gfx.EllipseOutline(sb, c.X, c.Y, face.Width * 0.5f * grow, face.Height * 0.58f * grow, Palette.KindDefend * (0.45f * fade), 1.5f);
                            Gfx.Line(sb, c + new Vector2(-face.Width * 0.3f, -face.Height * 0.38f) * grow, c + new Vector2(-face.Width * 0.08f, -face.Height * 0.52f) * grow, Color.White * fade, 2f);
                            Gfx.DrawGlow(sb, c.X, c.Y, face.Width * 0.75f, Palette.KindDefend * (0.25f * fade));
                        }
                        break;

                    case CardLook.Volume:                                                    // VOLUME : gold arrows rising, rays
                        for (int k = 0; k < 3; k++)
                        {
                            float p = (t * 1.4f + k * 0.3f) % 1f;
                            float x = face.X + face.Width * (0.25f + 0.25f * k);
                            float y = face.Bottom - p * (face.Height + 14f);
                            Color gold = Palette.KindBoost * (fade * (1f - p));
                            Gfx.Line(sb, new Vector2(x - 6f, y + 6f), new Vector2(x, y), gold, 2.5f);
                            Gfx.Line(sb, new Vector2(x + 6f, y + 6f), new Vector2(x, y), gold, 2.5f);
                        }
                        for (int k = 0; k < 6; k++)
                        {
                            float a = -MathHelper.PiOver2 + (k - 2.5f) * 0.32f;
                            Vector2 w = new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
                            Gfx.Line(sb, new Vector2(c.X, face.Y) + w * 8f, new Vector2(c.X, face.Y) + w * (14f + 14f * t), Palette.KindBoost * (0.7f * fade), 1.5f);
                        }
                        break;

                    case CardLook.Guard:                                                     // THUNDER CALL's GUARD : a drum skin, beating twice
                        {
                            float beat = 1f + 0.12f * (float)Math.Abs(Math.Sin(t * MathHelper.TwoPi * 2f));
                            Gfx.EllipseOutline(sb, c.X, face.Bottom - 6f, face.Width * 0.62f * beat, face.Height * 0.2f * beat, Palette.KindDefend * fade, 2.5f);
                            Gfx.EllipseOutline(sb, c.X, face.Bottom - 6f, face.Width * 0.4f * beat, face.Height * 0.12f * beat, cardFx[n].Tint * fade, 1.5f);
                            Gfx.DrawGlow(sb, c.X, face.Bottom - 6f, face.Width * 0.7f, Palette.KindDefend * (0.3f * fade));
                        }
                        break;

                    case CardLook.Call:                                                      // THE HUNTING CALL : a horn's rings from the face
                        for (int k = 0; k < 3; k++)
                        {
                            float p = (t * 1.5f + k / 3f) % 1f;
                            float r = face.Width * (0.3f + 0.7f * p);
                            Gfx.EllipseOutline(sb, c.X, c.Y, r, r, cardFx[n].Tint * (fade * (1f - p)), 2f);
                        }
                        break;

                    case CardLook.Price:                                                     // life paid : a flame licks along the card's foot
                        for (int k = 0; k < 9; k++)
                        {
                            float x = card.X + 6f + k * (card.Width - 12f) / 8f;
                            float h = (8f + 6f * (float)Math.Sin(time * 19f + k * 1.3f)) * fade;
                            Color fire = Color.Lerp(cardFx[n].Tint, Color.White, 0.25f) * fade;
                            Gfx.Line(sb, new Vector2(x, card.Bottom - 2f), new Vector2(x + 2f, card.Bottom - 2f - h), fire, 3f);
                            Gfx.Line(sb, new Vector2(x, card.Bottom - 2f), new Vector2(x + 1f, card.Bottom - 2f - h * 0.55f), Color.White * (0.7f * fade), 1.5f);
                        }
                        Gfx.DrawGlow(sb, card.Center.X, card.Bottom - 4f, card.Width * 0.5f, cardFx[n].Tint * (0.3f * fade));
                        break;
                }
            }
        }

        //Card Face : the face's window on band card slot (the same place DrawBand draws it)
        private static Rectangle CardFace(int slot)
        {
            Rectangle r = ChipBox(slot);
            return new Rectangle(r.X + 7, r.Y + 6, ChipFace, ChipFace);
        }
    }
}
