using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Tacetno433.Audio;
using Tacetno433.Battle;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //THE GUIDE (3 Nov, the user 13.151 : "the tutorial has to teach well" and "a NEWBIE mode, help, explanations and advice all the
    //time on the first two floors, the third played alone"). One helper for the practice fight (TUTORIAL) and a NEWBIE run :
    //   WHY        after every note that is not PERFECT, why : TOO EARLY, TOO LATE, WRONG WAY, NO SWIPE, HOLD LONGER, SHAKE FASTER
    //              (under the PERFECT / GOOD / MISS word), so a miss teaches something
    //   POINT      a pulsing frame round what to press next (PLAY, the skill buttons, the FINALE), the coach card's line says why
    //   ADVICE     NEWBIE only : the card under THE BAND says what would help this turn, read from the fight (a CHARGE coming, a
    //              musician low on LIFE, BREATH for a skill, the FINALE full), and what each kind of note asks while it plays
    //   FIRST LOOK PRACTICE only : before the first lesson, three cards show where things are (THE BAND, TACET, the moves). A click
    //              or ENTER goes on
    //Only how the fight is explained changes. The rules stay the same (NEWBIE's softer numbers are TurnBattle.Ease).
    public partial class BattleScreen
    {
        private bool guided;                     // NEWBIE MODE on floors 1 and 2 (RunState.Guided)
        private string why = "";                 // WHY : the last note's reason, "" for a PERFECT
        private string whyKept = "";             //   the reason of a MISS kept while a better stroke may still come (MISS WAIT)
        private Rectangle pointAt;               // POINT : what to press next, empty for nothing
        private float pointClock;
        private int introStep;                   // FIRST LOOK : the card shown, IntroLines.Length once they are all read

        //Why : the reasons, one or two words, the same in every fight
        private const string WhyEarly = "TOO EARLY : wait for the ring";
        private const string WhyLate = "TOO LATE : swipe a little sooner";
        private const string WhyWrong = "WRONG WAY : follow the arrow";
        private const string WhyNone = "NO SWIPE : swipe as the ring closes";
        private const string WhyHold = "HOLD LONGER : keep the button down";
        private const string WhyShake = "SHAKE FASTER";

        //First Look : PRACTICE's three cards, and what each one points at
        private static readonly string[] IntroLines =
        {
            "TOP LEFT : your band of three. The bar on each card is their LIFE.",
            "RIGHT : TACET, the enemy. The number is how hard it hits next, the arrows are the notes you will block.",
            "BOTTOM : the moves of the musician picked. PLAY starts the turn."
        };
        private static readonly Rectangle[] IntroPoints =
        {
            new Rectangle(22, 60, 524, 78),
            new Rectangle(720, 120, 470, 400),
            new Rectangle(384, 596, 878, 122)
        };
        private const string IntroMore = "CLICK OR ENTER : NEXT";

        //Advice : NEWBIE's lines, the first that fits the fight is shown
        private const string GuideTag = "GUIDE";
        private const string AdviceFinale = "Your " + Words.Finale + " is full. Press SPACE, then swipe DOWN, LEFT, RIGHT, UP.";
        private const string AdviceCharge = "An enemy is charging a big hit. Block it PERFECT, or fill its " + Words.Stun + " bar first.";
        private const string AdviceArmor = "An enemy has " + Words.Armor + " : hits break the ARMOR before its life.";
        private const string AdviceHurt = "A musician is low on LIFE. A green skill heals.";
        private const string AdviceSkill = "You have BREATH : click a musician, then 2 or 3 for a skill.";
        private const string AdvicePlay = "Press ENTER (" + Words.Play + ") : the band plays its moves.";
        private const string AdviceBlock = "Block : swipe the arrow's way as the ring closes. PERFECT = no damage.";
        private const string AdviceSkillNotes = "Swipe each arrow as its ring closes. PERFECT hits harder.";
        private const string AdviceHoldNote = "HOLD : swipe it, then keep the button down until it ends.";
        private const string AdviceRollNote = "ROLL : shake the baton fast until the bar is full.";
        private const string AdviceSparkNote = "SPARK : one more quick swipe right after, any way.";
        private const string AdviceRushNote = Words.Rush + " : the next arrow comes right away. Be quick.";
        private const string AdviceFinaleNotes = "4 swipes : DOWN, LEFT, RIGHT, UP.";
        private string adviceLine = AdvicePlay;

        //Wrapped : every line of the guide cut to the card's width once, at load (never while drawing)
        private static readonly Dictionary<string, string> wrapped = new Dictionary<string, string>();
        private const float GuideTextW = 510f;
        private const float IntroTextW = 650f;
        private static readonly Rectangle introBox = new Rectangle(240, 140, 800, 78);
        private static readonly Rectangle guideBox = new Rectangle(240, 140, 640, 56);   // NEWBIE : narrower, clear of the enemies' plates

        private string Wrapped(string line)
        {
            string w;
            return wrapped.TryGetValue(line, out w) ? w : line;
        }

        private void WrapGuide(string line, float width)
        {
            if (!wrapped.ContainsKey(line)) wrapped[line] = Gfx.WrapText(Game.StoryFont, line, width, TextSize.StorySmall);
        }

        //Guide Load : whether this fight explains itself
        private void LoadGuide()
        {
            guided = !practice && run != null && run.Guided;
            introStep = practice ? 0 : IntroLines.Length;
            for (int i = 0; i < IntroLines.Length; i++) WrapGuide(IntroLines[i], IntroTextW);
            string[] lines = { AdviceFinale, AdviceCharge, AdviceArmor, AdviceHurt, AdviceSkill, AdvicePlay, AdviceBlock, AdviceSkillNotes,
                               AdviceHoldNote, AdviceRollNote, AdviceSparkNote, AdviceRushNote, AdviceFinaleNotes };
            for (int i = 0; i < lines.Length; i++) WrapGuide(lines[i], GuideTextW);
        }

        //Intro Open : FIRST LOOK still has a card to show (the plan waits until it is read)
        private bool IntroOpen
        {
            get { return practice && introStep < IntroLines.Length && view == View.Plan; }
        }

        //Update Intro : a click or ENTER shows the next card. True while one is open, so the plan takes no input.
        private bool UpdateIntro()
        {
            if (!IntroOpen) return false;
            if (Input.MouseClicked() || Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Space))
            {
                introStep++;
                SoundBank.Play(Sfx.UiMove);
            }
            return true;
        }

        //Update Guide : the advice and the point for this frame (NEWBIE), the point for PRACTICE's lessons
        private void UpdateGuide(float dt)
        {
            pointClock += dt;
            pointAt = Rectangle.Empty;
            if (!guided && !practice) return;

            if (IntroOpen)
            {
                pointAt = IntroPoints[introStep];
                return;
            }

            if (view == View.Plan || view == View.Target)
            {
                if (practice)
                {
                    if (!learned[(int)Lesson.Play]) pointAt = playBox;
                    else if (!learned[(int)Lesson.Skill]) pointAt = selected != null ? commandBox[1] : BoxOf(battle.Party[0]);
                    else if (learned[(int)Lesson.Hold] && !learned[(int)Lesson.Signature]) pointAt = signatureBox;
                    return;
                }
                adviceLine = Advice();
                if (adviceLine == AdviceFinale) pointAt = signatureBox;
                else if (adviceLine == AdviceSkill && selected != null && view == View.Plan) pointAt = commandBox[1];
                else if (adviceLine == AdvicePlay) pointAt = playBox;
                return;
            }

            if (view == View.Swipe && guided)
            {
                NoteKind kind = next < notes.Count ? notes[next].Kind : NoteKind.Swipe;
                if (swipeFor == SwipeFor.Signature) adviceLine = AdviceFinaleNotes;
                else if (kind == NoteKind.Hold) adviceLine = AdviceHoldNote;
                else if (kind == NoteKind.Roll) adviceLine = AdviceRollNote;
                else if (kind == NoteKind.Spark) adviceLine = AdviceSparkNote;
                else if (kind == NoteKind.Rush) adviceLine = AdviceRushNote;
                else adviceLine = swipeFor == SwipeFor.Parry ? AdviceBlock : AdviceSkillNotes;
            }
        }

        //Advice : the first thing that would help this turn
        private string Advice()
        {
            if (battle.CanSignature) return AdviceFinale;
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                Fighter e = battle.Enemies[i];
                if (e.Hp > 0 && (e.IntentCharge || e.IntentHeavy)) return AdviceCharge;
            }
            for (int i = 0; i < battle.Enemies.Count; i++)
                if (battle.Enemies[i].Hp > 0 && battle.Enemies[i].Block > 0) return AdviceArmor;
            if (HurtWithHealer()) return AdviceHurt;
            if (battle.Breath >= 2 && battle.Bar.Count == 0) return AdviceSkill;
            return AdvicePlay;
        }

        //Hurt With Healer : a musician under a third of their life, and someone in the band has a heal
        private bool HurtWithHealer()
        {
            bool hurt = false;
            bool healer = false;
            for (int i = 0; i < battle.Party.Count; i++)
            {
                Fighter f = battle.Party[i];
                if (f.Hp <= 0) continue;
                if (f.Hp * 3 < f.MaxHp) hurt = true;
                Skill a = SkillList.First(f.Musician);
                Skill b = SkillList.Second(f.Musician);
                if ((a != null && a.Kind == SkillKind.Heal) || (b != null && b.Kind == SkillKind.Heal)) healer = true;
            }
            return hurt && healer;
        }

        //Why Of : the reason for a stroke that landed GOOD or MISS (offset : seconds after the ring, rightWay : the arrow's way)
        private static string WhyOf(Grade grade, float offset, bool rightWay)
        {
            if (!rightWay) return WhyWrong;
            if (grade == Grade.Perfect) return "";
            return offset < 0f ? WhyEarly : WhyLate;
        }

        //Draw Guide : the point, the advice card (NEWBIE) or FIRST LOOK's card (PRACTICE), and WHY under the judge's word
        private void DrawGuide(SpriteBatch sb)
        {
            if (!guided && !practice) return;

            if (pointAt != Rectangle.Empty)
            {
                float pulse = 0.5f + 0.5f * (float)System.Math.Sin(pointClock * 6f);
                Rectangle r = pointAt;
                r.Inflate(4 + (int)(pulse * 4f), 4 + (int)(pulse * 4f));
                Gfx.RectOutline(sb, r, Palette.Accent * (0.55f + 0.45f * pulse), 2);
            }

            if (judgeLife > 0f && why.Length > 0)
                Gfx.TextCentered(sb, Game.Font, why, HitX, HitY + 150f, Palette.Paper * System.Math.Min(1f, judgeLife * 2f), TextSize.Label);

            if (IntroOpen)
            {
                Ui.Panel(sb, introBox, 1f);
                Ui.Tag(sb, "PRACTICE", introBox.X + 18, introBox.Y + 9, true, 1f);
                Ui.Pips(sb, introBox.X + 26, introBox.Y + 40, introStep + 1, IntroLines.Length, 5f, 15f, 1f);
                Gfx.Text(sb, Game.StoryFont, Wrapped(IntroLines[introStep]), introBox.X + 130, introBox.Y + 10, Palette.Paper, TextSize.StorySmall);
                Gfx.TextRight(sb, Game.Font, IntroMore, introBox.Right - 14, introBox.Bottom - 18, Palette.Accent, TextSize.Tiny);
                return;
            }
            if (practice) DrawCoach(sb);
            else if (view == View.Plan || view == View.Target || view == View.Swipe)
            {
                string line = Wrapped(adviceLine);
                bool two = line.IndexOf('\n') >= 0;
                Ui.Panel(sb, guideBox, 0.92f);
                Ui.Tag(sb, GuideTag, guideBox.X + 18, guideBox.Y + 9, true, 1f);
                float y = two ? guideBox.Y + 4f : guideBox.Y + guideBox.Height / 2f - Game.StoryFont.LineSpacing * TextSize.StorySmall / 2f;
                Gfx.Text(sb, Game.StoryFont, line, guideBox.X + 110, y, Palette.Paper, TextSize.StorySmall);
            }
        }
    }
}
