using System;
using Microsoft.Xna.Framework;
using Tacetno433.Audio;
using Tacetno433.Battle;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //======================================================================================
    //BattleScreen.Sound : BLOCK E (Beta1 2 Nov, docs/BETA1_DIRECTION.md 47). What the fight sounds like. No rule lives here :
    //every sound answers something the rules wrote down (battle.Events) or a moment of the screen (a note judged, a boss's
    //moment, the FINALE). The files and how loud each layer is : Audio/SoundBank.cs and BattleRules Sound Feel.
    //   A BLOW      TWO LAYERS (the user 13.131) : the weapon (strings CUT, winds SHOT, drums SMASH), then the striker's own
    //               instrument, light, the same note every time (never a tune, 13.127 13.128)
    //   A NOTE      PERFECT rings bright, GOOD ticks, a MISS thuds
    //   A PARRY     each of TACET's notes swings in (EnemySwing, loudest right on the ring : the cue Expedition 33 gives every
    //               attack, docs/research/E_music.md), met PERFECT it clangs bright, met GOOD it clanks
    //   SHARED      many moments share a file (the user 13.133) : TACET's HEAL is the band's, lower / its ARMOR UP is GUARD,
    //               lower / a COUNTER is a SMASH with the PERFECT ring
    //   THE MUSIC   a FINALE or a boss's moment pushes it down while it speaks (DUCK), THE TURN changes it (Music.BossTurn)
    //======================================================================================
    public partial class BattleScreen
    {
        private int cueNext;                    // the next of TACET's notes whose swing has not sounded yet (this swipe)
        private bool enemyTurnSounded;          // TACET's turn has been announced (once a turn, not once an enemy)
        private bool planSounded;               // the first bar of the fight is written without the baton's tap (FightStart rang)
        private static readonly Random soundRng = new Random();   // the sounds' own dice : the run's dice (run.Rng) are the rules' alone

        //Sound Hit : a blow lands on an enemy. An ATTACK is the weapon of the striker's family, a skill's note their own
        //element (ELEMENTS, the colour its VFX already has), then their instrument, light, under either.
        private void SoundHit(Fighter striker)
        {
            if (striker == null || striker.IsEnemy) return;
            float pitch = ((float)soundRng.NextDouble() * 2f - 1f) * BattleRules.SoundWeaponPitch;
            if (PlayingSkill != null) SoundBank.Play(ElementOf(striker.Musician), BattleRules.SoundWeapon, pitch);
            else SoundBank.Play(WeaponOf(StyleOf(striker)), BattleRules.SoundWeapon, pitch);
            SoundBank.PlayBand(striker.Musician, BattleRules.SoundBandLayer);
        }

        private static Sfx ElementOf(Musician m)
        {
            if (m == null) return Sfx.WeaponShot;
            if (m.Spot == SpotLook.Fire || m.Spot == SpotLook.Red) return Sfx.SkillFire;
            if (m.Spot == SpotLook.Wind) return Sfx.SkillWind;
            if (m.Spot == SpotLook.Earth) return Sfx.SkillEarth;
            if (m.Spot == SpotLook.Water) return Sfx.SkillWater;
            return Sfx.SkillLight;
        }

        private static Sfx WeaponOf(HitStyle style)
        {
            if (style == HitStyle.Smash) return Sfx.WeaponSmash;
            if (style == HitStyle.Slash) return Sfx.WeaponCut;
            return Sfx.WeaponShot;
        }

        //Sound Events : what the rules just wrote down, heard. Called first thing in ShowEvents, before the list is cleared.
        //REPEAT GAP (SoundBank) keeps one blow on three enemies from playing three times over.
        private void SoundEvents()
        {
            for (int i = 0; i < battle.Events.Count; i++)
            {
                TurnEvent e = battle.Events[i];
                switch (e.Kind)
                {
                    case TurnEventKind.Damage:
                        if (e.Who == null) break;
                        if (!e.Who.IsEnemy)
                        {
                            SoundBank.Play(Sfx.BandHurt, 0.9f, 0f);                       // TACET's blow on the band
                            break;
                        }
                        if (hitSignature) SoundBank.Play(Sfx.WeaponSmash, 1f, -0.25f);    // the FINALE's strokes land as one great blow
                        else SoundHit(hitBy);
                        break;
                    case TurnEventKind.DissonanceTick: SoundBank.Play(Sfx.BurnTick, 0.45f, 0.15f); break;      // BURN : the fire, softer
                    case TurnEventKind.Heal:
                    case TurnEventKind.Saved:
                        bool theirs = e.Who != null && e.Who.IsEnemy;
                        SoundBank.Play(Sfx.SkillHeal, theirs ? 0.6f : 0.85f, theirs ? -0.35f : 0f);
                        break;
                    case TurnEventKind.Down:
                        if (e.Who == null) break;
                        if (!e.Who.IsEnemy) SoundBank.Play(Sfx.BandDown, 1f, 0f);
                        else if (e.Who == boss) SoundBank.Play(Sfx.BossDown, 1f, 0f);
                        else SoundBank.Play(Sfx.EnemySilenced, 1f, 0f);
                        break;
                    case TurnEventKind.Crescendo:
                    case TurnEventKind.Lesson:
                    case TurnEventKind.Cue:
                        SoundBank.Play(Sfx.SkillBoost, 0.8f, 0f);                                  // VOLUME FOCUS HUNTING CALL
                        break;
                    case TurnEventKind.Forte: SoundBank.Play(Sfx.SkillBoost, 1f, 0.2f); break;      // FORTE, after the FINALE
                    case TurnEventKind.CrescendoLost:
                    case TurnEventKind.Steal:
                    case TurnEventKind.Mute:
                        SoundBank.Play(Sfx.QteMiss, 0.55f, -0.4f);                                 // something taken away
                        break;
                    case TurnEventKind.GuardUp:
                    case TurnEventKind.Shield:
                        SoundBank.Play(Sfx.SkillGuard, 0.85f, 0f);
                        break;
                    case TurnEventKind.Guard: SoundBank.Play(Sfx.SkillGuard, 0.75f, -0.3f); break;  // TACET's ARMOR UP : GUARD, lower
                    case TurnEventKind.Blocked: SoundBank.Play(Sfx.ParryGood, 0.6f, -0.25f); break; // ARMOR takes the blow
                    case TurnEventKind.Skip:
                    case TurnEventKind.Break:
                        SoundBank.Play(Sfx.Stunned, e.Kind == TurnEventKind.Break ? 1f : 0.75f, 0f);  // STUNNED
                        break;
                    case TurnEventKind.Charge: SoundBank.Play(Sfx.EnemyCharge, 0.9f, 0f); break;    // CHARGE
                    case TurnEventKind.Broken:
                    case TurnEventKind.BigParried:
                        SoundBank.Play(Sfx.EnemyBroken, 1f, 0f);                                    // BROKEN, a BIG ATTACK blocked whole
                        break;
                    case TurnEventKind.Counter:
                        SoundBank.Play(Sfx.WeaponSmash, 1f, 0.1f);                                  // COUNTER : a SMASH with the PERFECT ring
                        SoundBank.Play(Sfx.QtePerfect, BattleRules.SoundSpark, 0.2f);
                        if (e.Who != null) SoundBank.PlayBand(e.Who.Musician, BattleRules.SoundBandLayer);
                        break;
                    case TurnEventKind.OwnFoot: SoundBank.Play(Sfx.WeaponSmash, 0.9f, -0.3f); break; // TE DEUM's own foot
                    case TurnEventKind.Ready: SoundBank.Play(Sfx.FinaleReady, 0.9f, 0f); break;      // the FINALE gauge is full
                    case TurnEventKind.Solo:
                        if (e.Amount == 0 && e.Who != null)                                         // SOLO earned : their own note, loud
                        {
                            SoundBank.PlayBand(e.Who.Musician, 0.9f);
                            SoundBank.Play(Sfx.QtePerfect, BattleRules.SoundSpark, 0.3f);
                        }
                        break;
                    case TurnEventKind.Echo: SoundBank.Play(Sfx.QtePerfect, 0.4f, 0.5f); break;     // FUGUE : the gold voice
                    case TurnEventKind.Gift: SoundBank.Play(Sfx.MotifGet, 0.45f, 0f); break;        // GIFT AT WORK
                }
            }
        }

        //Sound Land : one note judged (Land). A note of a skill or the FINALE rings, ticks or thuds before its blow
        //(the blow's own sound comes from its Damage event). A parried note clangs, a MISS is heard as the band's hurt.
        private void SoundLand(Grade grade)
        {
            bool perfect = grade == Grade.Perfect;
            bool landed = perfect || grade == Grade.Good;
            if (swipeFor == SwipeFor.Parry)
            {
                if (perfect) SoundBank.Play(Sfx.ParryPerfect, 1f, 0f);
                else if (landed) SoundBank.Play(Sfx.ParryGood, 0.85f, 0f);
                return;
            }
            if (perfect) SoundBank.Play(Sfx.QtePerfect, BattleRules.SoundSpark, 0f);
            else if (landed) SoundBank.Play(Sfx.QteNormal, BattleRules.SoundGood, 0f);
            else if (grade == Grade.Hesitate) SoundBank.Play(Sfx.QteHesitate, BattleRules.SoundMiss * 0.6f, 0f);
            else SoundBank.Play(Sfx.QteMiss, BattleRules.SoundMiss, 0f);
        }

        //Sound Cues : PARRY CUE, each of TACET's notes swings in so its loudest moment is right on the ring. A ROLL has no
        //single moment and gets none. Called every frame of a parry (UpdateSwipe), on the same clock as the rings.
        private void SoundCues(float now)
        {
            if (swipeFor != SwipeFor.Parry) return;
            while (cueNext < notes.Count && now >= notes[cueNext].Due - BattleRules.SoundSwingLead)
            {
                if (notes[cueNext].Kind != NoteKind.Roll && cueNext >= next) SoundBank.Play(Sfx.EnemySwing, BattleRules.SoundSwing, 0f);
                cueNext++;
            }
        }

        //Sound Turn : the band's turn (the baton taps the stand) or TACET's (once, at its first attack)
        private void SoundBandTurn()
        {
            enemyTurnSounded = false;
            if (!planSounded)
            {
                planSounded = true;                                                       // the fight's first bar : FightStart already rang
                return;
            }
            SoundBank.Play(Sfx.RoundStart, 0.8f, 0f);
        }

        private void SoundEnemyTurn()
        {
            if (enemyTurnSounded) return;
            enemyTurnSounded = true;
            SoundBank.Play(Sfx.EnemyTurn, 0.8f, 0f);
        }

        //Sound Finale : the conductor's own FINALE, the music pushed down while it speaks
        private void SoundFinale()
        {
            Sfx own = battle.Kit == TurnKit.Fanny ? Sfx.FinalePrism : (battle.Kit == TurnKit.Bach ? Sfx.FinaleHoly : Sfx.FinaleStorm);
            SoundBank.Play(own, 1f, 0f);
            SoundBank.DuckMusic(BattleRules.SoundDuckFinale, BattleRules.SoundDuckFinaleTime);
        }

        //Sound Boss : a boss's moment speaks over its music
        private void SoundBoss(Sfx moment)
        {
            SoundBank.Play(moment, 1f, 0f);
            SoundBank.DuckMusic(BattleRules.SoundDuckBoss, BattleRules.SoundDuckBossTime);
        }
    }
}
