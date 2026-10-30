using System;
using System.Collections.Generic;
using Tacetno433.Data;

namespace Tacetno433.Battle
{
    //Move Preview : what one move would do, worked out without playing it (TurnBattle.Preview). The screen keeps one and has
    //it filled again when the move under the pointer changes, so nothing is made new in a frame.
    public class MovePreview
    {
        public Skill Skill;                                  // the skill, null for ATTACK
        public Fighter Who;                                  // who would play it
        public int PerNote;                                  // one note's damage on the first enemy it reaches (one hit for ATTACK)
        public int Notes;                                    // the notes it asks for, 0 for ATTACK
        public int Damage;                                   // the life all the enemies it reaches would lose together
        public int Heal;                                     // the life one musician would get back from all its notes
        public int Crescendo;                                // CRESCENDO each musician it reaches would get
        public int Dissonance;                               // DISSONANCE the first enemy would get
        public int OffBeat;                                  // OFF-BEAT the first enemy would get
        public int Breath;                                   // BREATH back to the band
        public float Share;                                  // GUARD, SHIELD : the share of a hit still taken. THE LESSON : x the next skill
        public List<Fighter> Targets = new List<Fighter>();  // who it reaches, a CHAKRAM's line in the order of its throws
        public int[] Loss = new int[4];                      // the life each enemy would lose, by its slot (BLOCK taken off first)
        public int[] Gain = new int[3];                      // the life each musician would get back, by their slot
        public int[] BlockLeft = new int[4];                 // GUARD : the BLOCK each enemy would have left
    }

    //PREVIEW (Beta1 30 Oct, block 1 item 4, docs/BETA1_DIRECTION.md 11.7 point 4) : the battle screen shows what a move does
    //before it is played, the skill card's numbers and the life a target would lose or get back on its bar. The move is worked
    //out as if every note lands GOOD and every hold is held to the end. Nothing changes : no event, no stack, no OVERTURE spent.
    //Left out because they hang on what happens before the move in the bar or on the swipe : DOWNBEAT, UNISON, THE HUNTING CALL,
    //OVERTURE, a PERFECT's TurnPerfect, so a move that lands often does a little more than its preview.
    public partial class TurnBattle
    {
        private readonly List<Fighter> previewLine = new List<Fighter>();

        //Targets For : who a skill would reach, the way it is played (a CHAKRAM's line, THE DEVIL'S OWN, every enemy ...)
        public void TargetsFor(Skill s, Fighter target, Fighter who, List<Fighter> into)
        {
            into.Clear();
            if (s == null)
            {
                if (CanTarget(target)) into.Add(target);                                     // ATTACK : the one enemy
                return;
            }
            into.AddRange(TargetsOf(s, target, who));
        }

        //Preview : fill into with what this musician's move would do. s is null for ATTACK. target is the enemy or the
        //musician it is played at, null when the skill picks its own (every enemy, the whole band, themself).
        public void Preview(Fighter who, Skill s, Fighter target, MovePreview into)
        {
            into.Skill = s;
            into.Who = who;
            into.PerNote = into.Notes = into.Damage = into.Heal = into.Crescendo = into.Dissonance = into.OffBeat = into.Breath = 0;
            into.Share = 0f;
            Fit(ref into.Loss, Enemies.Count);
            Fit(ref into.Gain, Party.Count);
            Fit(ref into.BlockLeft, Enemies.Count);
            for (int i = 0; i < Enemies.Count; i++) into.BlockLeft[i] = Enemies[i].Block;
            TargetsFor(s, target, who, into.Targets);
            if (who == null || who.IsEnemy || who.Down || into.Targets.Count == 0) return;

            //A legend's first move of the fight : ENTRANCE (motif) and BONDS (MoveScale, without the bar's DOWNBEAT and UNISON)
            float first = 1f;
            if (Has(MotifId.Entrance) && !who.Entered) first *= BattleRules.TurnEntranceScale;
            if (!who.Entered && BondList.Of(who.Musician, Kit) != null) first *= BattleRules.TurnBondScale;

            if (s == null)
            {
                //ATTACK : one hit at its enemy, OFF-BEAT and BREATH back (BREATH CONTROL : QUANTZ more)
                Fighter enemy = into.Targets[0];
                into.PerNote = HitFor(who, enemy, BattleRules.TurnAttackScale * first, false);
                PreviewStrike(into, enemy, into.PerNote, false);
                into.OffBeat = BattleRules.TurnAttackOffBeat * (Has(MotifId.Marcato) ? BattleRules.TurnMarcatoOffBeat : 1);   // MARCATO
                into.Breath = AttackBreath + (who.Musician.Trait == MusicianTrait.BreathControl ? BattleRules.TurnBreathControl : 0);
                return;
            }

            //The skill's own scale, as BeginSkill works it out : THE LESSON on them, LANCE after a blow, THE PACT
            float scale = first;
            if (who.Lesson) scale *= who.LessonScale > 0f ? who.LessonScale : BattleRules.TurnLessonScale;
            if (s.Revenge > 0f && who.Wounded) scale *= s.Revenge;
            if (who.Musician.Has(PathRule.PaidInBlood)) scale *= BattleRules.TurnBloodScale;
            bool everyone = s.Target == SkillTarget.AllEnemies || s.Target == SkillTarget.AllAllies;
            float tutti = everyone && Has(MotifId.Tutti) ? BattleRules.TurnTuttiScale : 1f;                  // TUTTI
            bool felix = Kit == TurnKit.Fanny && FelixVoice;                                                   // TWO VOICES
            bool lastCut = IsSkill(who, s, 0) && who.Musician.Has(PathRule.LastCut);                           // CAPRICE No. 24
            int offBeat = s.OffBeat;
            if (who.Musician.Family == Family.Percussion && Has(MotifId.SpareSticks)) offBeat = (int)Math.Round(offBeat * BattleRules.TurnSpareSticksOffBeat);   // SPARE STICKS

            into.Notes = s.Notes.Length;
            for (int n = 0; n < s.Notes.Length; n++)
            {
                float hold = s.Notes[n].Kind == NoteKind.Hold ? HoldScale(1f) : 1f;
                float noteScale = SwipeScale(Grade.Good) * hold * scale;
                bool last = n == s.Notes.Length - 1;
                if (lastCut && last) noteScale *= BattleRules.TurnLastCutScale;
                float repeat;
                PreviewNote(s, n, who, into.Targets, out repeat);
                noteScale *= repeat;

                for (int t = 0; t < previewLine.Count; t++)
                {
                    Fighter f = previewLine[t];
                    if (f.IsEnemy)
                    {
                        if (s.Damage > 0f)
                        {
                            int hit = HitFor(who, f, noteScale * s.Damage * tutti, false);
                            if (n == 0 && t == 0) into.PerNote = hit;
                            PreviewStrike(into, f, hit, s.Pierce);                                           // PIERCE
                        }
                        if (f == into.Targets[0])
                        {
                            into.Dissonance += s.Dissonance;
                            into.OffBeat += (int)Math.Round(offBeat * repeat);
                        }
                        continue;
                    }
                    float heal = s.Heal * hold * tutti * repeat;
                    if (who.Musician.Family == Family.Wind && Has(MotifId.ReedCase)) heal *= BattleRules.TurnReedCaseHeal;   // REED CASE
                    if (felix) heal *= BattleRules.TurnFelixHeal;
                    int gain = Math.Min((int)Math.Round(heal), f.MaxHp - f.Hp - into.Gain[f.Slot]);
                    if (gain > 0) into.Gain[f.Slot] += gain;
                    if (f == into.Targets[0])
                    {
                        into.Heal += (int)Math.Round(heal);
                        if (s.Crescendo > 0) into.Crescendo += s.Crescendo + (felix ? BattleRules.TurnFelixCrescendo : 0);
                    }
                }
                into.Breath += s.Breath;
            }

            //What a skill that only stands by someone does : GUARD (THUNDER CALL), SHIELD (STRADIVARIUS : less), THE LESSON
            if (s.Guard) into.Share = BattleRules.TurnGuardKeep;
            if (s.Shield) into.Share = who.Musician.Has(PathRule.ShieldThird) ? BattleRules.TurnShieldThird : BattleRules.TurnShieldKeep;
            if (s.Lesson) into.Share = who.Musician.Has(PathRule.LessonStrong) ? BattleRules.TurnLessonStrong : BattleRules.TurnLessonScale;
        }

        //Preview Note : who note n of a skill reaches into previewLine, the way NoteTargets throws a CHAKRAM, without its
        //list of the enemies already hit (the line has each enemy once, so note n reaches the n-th one, round again after)
        private void PreviewNote(Skill s, int n, Fighter who, List<Fighter> line, out float repeat)
        {
            repeat = 1f;
            previewLine.Clear();
            if (s.Bounce <= 0)
            {
                previewLine.AddRange(line);
                return;
            }
            if (who.Musician.Has(PathRule.ChakramLast) && n == s.Notes.Length - 1)                // WHIRLWIND
            {
                for (int i = 0; i < Enemies.Count; i++)
                    if (!Enemies[i].Down) previewLine.Add(Enemies[i]);
                return;
            }
            if (line.Count == 0) return;
            previewLine.Add(line[n % line.Count]);
            if (n >= line.Count) repeat = who.Musician.Has(PathRule.ChakramFull) ? 1f : BattleRules.TurnChakramRepeat;   // THE CATCH
        }

        //Preview Strike : Strike worked out only. HOLDS THE ROOM and THE COURT take some off, the BLOCK left takes the rest
        //first (a PIERCE goes through), never more than the life left.
        private void PreviewStrike(MovePreview into, Fighter enemy, int amount, bool pierce)
        {
            if (amount <= 0 || enemy.Down) return;
            if (HeldUp(enemy)) amount = Math.Max(1, (int)Math.Round(amount * BattleRules.TurnRoomToneKeep));   // HOLDS THE ROOM
            if (Has(enemy, EnemyTrait.TheCourt) && OthersStanding(enemy))                                     // THE COURT
                amount = Math.Max(1, (int)Math.Round(amount * BattleRules.TurnCourtKeep));
            int slot = enemy.Slot;
            if (!pierce && into.BlockLeft[slot] > 0)
            {
                int taken = Math.Min(into.BlockLeft[slot], amount);
                into.BlockLeft[slot] -= taken;
                amount -= taken;
            }
            amount = Math.Min(amount, enemy.Hp - into.Loss[slot]);
            if (amount <= 0) return;
            into.Loss[slot] += amount;
            into.Damage += amount;
        }

        //Fit : a list of numbers one per fighter, as long as needed (longer only when a bigger pack comes), all 0
        private static void Fit(ref int[] list, int count)
        {
            if (list.Length < count) list = new int[count];
            Array.Clear(list, 0, list.Length);
        }
    }
}
