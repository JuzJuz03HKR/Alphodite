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
    //DuelScreen.Baton : judging the baton. JudgeStroke turns a finished stroke into who plays (its
    //size, DYNAMICS) and a grade, and the stroke guide shows how big the stroke is while it is being
    //made: the ruler marks p, mf and f, and the band lights up row by row as the stroke grows.
    //The stick itself and its ruler are drawn by Core/Baton.cs, the arrows by Core/NoteGlyph.cs.
    public partial class DuelScreen
    {
        //Stroke Judge : the one place where the baton becomes an order to the band
        private void JudgeStroke(int b, float target)
        {
            int k = b % 4;

            //Stroke Size : small brings the back row, middle the middle row too, big the whole band
            Choice choice = sizeChoice[Baton.SizeOf(gesture.Length)];

            //SOFT REST : a small stroke on a silent beat is a rest, whatever its timing or way.
            //The caller moves on to the next beat (AfterStroke), as for any other stroke.
            if (battle.RestsOn(b, choice) && !battle.SignatureArmed)
            {
                baton.Whip(gesture.Direction);
                ResolveAnswer(b, Choice.Ease, Grade.None, false);
                return;
            }
            if (battle.ChoicesLocked && !battle.IsSilent(b)) choice = battle.MarkedChoice(b);   // LOCKED TEMPO : the mark decides

            //Timing Grade : judged where the baton stopped, against the beat
            float now = StrokeClock();
            float off = Math.Abs(now - target);
            Grade grade = Grade.Miss;
            if (off <= battle.GoodWindow) grade = Grade.Good;
            if (off <= battle.PerfectWindowAt(b)) grade = Grade.Perfect;

            //COUNTS ALOUD : a PERFECT that only her wider window allowed gets her name over it
            if (grade == Grade.Perfect && off > battle.PerfectWindow)
            {
                int anna = battle.CuedSeat(MusicianTrait.KeepsCount, b);
                if (anna >= 0) PopTrait(anna);
            }

            //FASHIONABLY LATE : a late stroke that brings her in is forgiven as GOOD
            if (grade == Grade.Miss && now > target && battle.LateForgivenAt(choice))
            {
                grade = Grade.Good;
                int iris = battle.JoinedSeat(MusicianTrait.Forgiven, choice);
                if (iris >= 0) PopTrait(iris);
            }

            //Wrong Way : the stroke has to follow the beat pattern, any other way is a MISS
            bool rightWay = gesture.Direction == pattern[k];
            if (!rightWay) grade = Grade.Miss;

            //Signature : this stroke is a PERFECT big stroke whatever its shape, with the bonus on top
            bool signature = battle.SignatureArmed;
            if (signature)
            {
                battle.SignatureArmed = false;
                choice = Choice.Boost;
                grade = Grade.Perfect;
                battle.SignatureNext = true;
            }

            //Hand And Stick : the hand picture changes pose, the stick gets a flick
            hand.Play(signature ? HandPose.Signature : PoseFor(gesture.Direction));
            baton.Whip(gesture.Direction);

            //Slash : a big stroke the right way leaves a blade of light along its path, brighter
            //for a PERFECT. The whole band's blow, the answer to TACET's.
            if (choice == Choice.Boost && (rightWay || signature))
                effects.SpawnSlash(gesture.From, gesture.To, grade == Grade.Perfect ? 1f : 0.55f);

            //Judgement : ONE word at the hit point, how well it landed and what the band was told,
            //and the shape of the order opening out from the hit point
            string word = (!rightWay && !signature) ? "WRONG WAY" : judgeText[(int)grade * 3 + (int)choice];
            ShowJudge(word, 0.55f);
            if (rightWay && !signature) ShowTiming(grade, now, target);

            if (choice == Choice.Boost) SoundBank.Play(Sfx.QteBoost);
            if (choice == Choice.Normal) SoundBank.Play(Sfx.QteNormal);
            if (choice == Choice.Ease) SoundBank.Play(Sfx.QteEase);
            if (grade == Grade.Perfect) SoundBank.Play(Sfx.QtePerfect);
            if (grade == Grade.Miss) SoundBank.Play(Sfx.QteMiss);

            //Fermata : a stroke that lands on TACET's held note opens the hold instead of settling
            //the beat at once. A missed one settles it straight away, there is nothing to hold.
            if (battle.IsFermata(b) && grade != Grade.Miss && !battle.Finished)
            {
                StartHold(choice, grade, signature);
                return;
            }

            ResolveAnswer(b, choice, grade, signature);
        }

        //EARLY / LATE : like the FAST and LATE marks of Project Sekai. A stroke that counted but
        //was not PERFECT says which side of the beat it fell on, so the player knows what to fix
        //(always LATE on a good mouse usually means the STROKE TIMING setting should move).
        private void ShowTiming(Grade grade, float now, float target)
        {
            if (grade == Grade.Good || grade == Grade.Miss)
                judgeTiming = now < target ? -1 : 1;
        }

        //Pose For : which hand picture belongs to which stroke
        private HandPose PoseFor(Flick flick)
        {
            if (flick == Flick.Down) return HandPose.Down;
            if (flick == Flick.Left) return HandPose.Left;
            if (flick == Flick.Right) return HandPose.Right;
            if (flick == Flick.Up) return HandPose.Up;
            return HandPose.Ready;
        }

        //Stroke Guide : a ruler coming off the baton the way this beat goes, with marks where a
        //stroke turns from p to mf and from mf to f. Once a stroke starts the ruler stays where it
        //began and fills up as the baton travels, so the size can be judged on the way instead of
        //guessed. The band lights up with it, row by row (see DrawMusicianGlow).
        private void DrawStrokeGuide(SpriteBatch sb)
        {
            if (AnswerProgress() < 0f || cutIn > 0f || rolling) return;
            if (!gesture.Held) return;                  // only while the baton is raised

            //Fermata : the baton should stay where it is, so the sign sits beside it
            if (holding)
            {
                Vector2 at = Input.MousePos + new Vector2(30f, -30f);
                Gfx.Circle(sb, at.X, at.Y - 4f, 20f, Palette.Void * 0.85f);
                NoteGlyph.FermataSign(sb, at.X, at.Y + 4f, 13f, Palette.Highlight);
                return;
            }

            //Spark : the second note of a pair takes any way, so a spark sits by the baton instead
            if (onGrace)
            {
                Vector2 at = Input.MousePos + new Vector2(26f, -26f);
                Gfx.Circle(sb, at.X, at.Y, 18f, Palette.Void * 0.85f);
                NoteGlyph.Spark(sb, at.X, at.Y, 13f, Palette.Highlight);
                return;
            }

            //Ruler : from where the stroke began, the way this beat goes, filled as far as the
            //stroke has come. The finale only needs the right way, so it has no size zones, and
            //neither has a note under LOCKED TEMPO, where the mark decides the size.
            Vector2 d = NoteGlyph.Way(WantedWay());
            Vector2 anchor = gesture.InStroke ? gesture.StrokeStart : Input.MousePos;
            float along = Vector2.Dot(gesture.LiveVector, d);
            int zone = gesture.InStroke ? Baton.SizeOf(along) : -1;
            bool markDecides = battle.ChoicesLocked && !battle.IsSilent(BeatOf(pending));
            Baton.DrawRuler(sb, anchor, d, along, zone, phase != Phase.Finale && !markDecides);
        }

    }
}
