using System;
using System.Collections.Generic;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Battle
{
    //TurnStep : what the turn battle is waiting for (Alpha 7)
    //   PlayerTurn   a musician is up : the screen asks for a command, a target and the swipes
    //   EnemyTurn    an enemy is up : the screen shows its notes and asks for the parry swipes
    //   Won / Lost   the fight is over
    public enum TurnStep { PlayerTurn, EnemyTurn, Won, Lost }

    //TurnEventKind : something that happened, kept so the screen can show it (numbers, words, sparks).
    //The rules never draw, they only write down what happened (docs/research/ALPHA7_battle_ui.md).
    public enum TurnEventKind { Damage, Heal, Breath, Dissonance, Crescendo, Mute, Break, Skip, Counter, Down, Ready, Extra, Saved }

    public struct TurnEvent
    {
        public TurnEventKind Kind;
        public Fighter Who;
        public int Amount;

        public TurnEvent(TurnEventKind kind, Fighter who, int amount)
        {
            Kind = kind;
            Who = who;
            Amount = amount;
        }
    }

    //NoteResult : how one note of a swipe went, handed in by the screen.
    //Hold is the share of a held note's time the baton stayed still (1 for every other note).
    public struct NoteResult
    {
        public Grade Grade;
        public bool RightSize;
        public float Hold;

        public NoteResult(Grade grade, bool rightSize, float hold)
        {
            Grade = grade;
            RightSize = rightSize;
            Hold = hold;
        }

        public bool Lands
        {
            get { return Grade == Grade.Perfect || Grade == Grade.Good; }
        }
    }

    //Fighter : one body on the field, a musician of the band or a shape of TACET
    public class Fighter
    {
        public string Name = "";
        public bool IsEnemy;
        public Musician Musician;      // set for the band, null for TACET
        public Enemy Enemy;            // set for TACET, null for the band
        public int Slot;               // place in its own side's list, breaks speed ties
        public int MaxHp;
        public int Hp;
        public int Speed;

        //Enemy Intent : what this enemy does on its next turn. It is decided when the round starts,
        //so the screen can show it BEFORE it happens and the player can plan (like Slay the Spire).
        public Fighter IntentTarget;
        public int[] IntentNotes = new int[0];   // how hard each note of the attack hits
        public Flick[] IntentWays = new Flick[0];  // the way to swipe back at each note
        public int[] IntentSizes = new int[0];     // the size of each note : 0 p, 1 mf, 2 f (Baton.SizeOf order)
        public bool IntentCadenza;                 // CADENZA : this attack is a boss's big move
        public int Attacks;                        // attacks made so far, a boss's every third is a CADENZA

        //Statuses (ALPHA7_DESIGN 5.7) and the two short guards of the percussion
        public int Dissonance;         // DISSONANCE : loses this much life at the end of its turn, then one less
        public int Crescendo;          // CRESCENDO : hits TurnCrescendoStep harder per stack, all lost on a MISS
        public int Mute;               // MUTE : turns left without skills
        public bool Guard;             // GUARD : takes TurnGuardKeep of every hit until its next turn
        public bool Taunt;             // TAUNT : every enemy aims here until its next turn

        //OFF-BEAT (enemies, ALPHA7_DESIGN 5.6) : the gauge fills from PERFECT hits, parries and
        //percussion. Full, the enemy is thrown off the beat : it loses its next turn and takes
        //TurnOffBeatScale damage until then.
        public int OffBeat;
        public bool Staggered;

        public bool Down { get { return Hp <= 0; } }
    }

    //TurnBattle : THE RULES OF THE TURN BASED FIGHT (Alpha 7). No drawing code lives here.
    //A screen runs it like this :
    //   PlayerTurn : the player attacks (Attack) or plays a skill (UseSkill) with the notes they swiped
    //   EnemyTurn  : the screen shows Current.IntentNotes flying at Current.IntentTarget, the player
    //                swipes back at each one, the screen calls ParryNote for each note as it lands
    //                and FinishEnemyTurn after the last (TakeEnemyAttack does all of it at once)
    //   After every call the screen reads Events to show what happened, then clears it.
    //   Won / Lost : the fight is over, HpAfterBattle gives each musician's life to keep
    //Every number comes from Data/BattleRules.cs (TURN BATTLE section).
    //Grade is the same enum the rhythm duel uses (Battle/BattleState.cs).
    public class TurnBattle
    {
        public List<Fighter> Party = new List<Fighter>();
        public List<Fighter> Enemies = new List<Fighter>();

        //Turn Order : everybody still standing this round, fastest first
        public List<Fighter> Order = new List<Fighter>();
        public int OrderIndex;
        public int Round;

        public int Breath;             // BREATH, shared by the whole band
        public int Floor;              // 1, 2 or 3
        public bool Maestro;           // MAESTRO MODE : narrower timing windows, more shakes for a roll
        public TurnStep Step;

        //Conductor (step 5) : whose PASSIVE and SIGNATURE this fight plays, and the SIGNATURE gauge
        public TurnKit Kit;
        public int Signature;
        private int perfectRun;        // PERFECTs in a row, for PRODIGY
        public int BestRun;            // the longest run of PERFECTs this fight, for the result page

        //Motifs (step 6) : what the band carries, see GiveMotifs and Data/Motif.cs (TurnText)
        private bool[] motif = new bool[64];
        private bool overtureDone;     // OVERTURE : the band's first hit has landed
        private bool secondWindUsed;   // SECOND WIND : once per fight

        //Events : what happened since the screen last looked
        public List<TurnEvent> Events = new List<TurnEvent>();

        private Random random;
        private int parried;           // notes of the current enemy attack parried PERFECT, for the COUNTER

        //Current : whoever is acting now (null once the fight is over)
        public Fighter Current
        {
            get
            {
                if (Step == TurnStep.Won || Step == TurnStep.Lost) return null;
                return Order[OrderIndex];
            }
        }

        //Battle Start : startHp is each musician's life carried in from the run, or null for full life
        public TurnBattle(List<Musician> band, List<Enemy> enemies, int floor, Random random, int[] startHp = null)
        {
            this.random = random;
            Floor = Math.Max(1, Math.Min(3, floor));
            Breath = BattleRules.TurnBreathStart;

            for (int i = 0; i < band.Count; i++)
            {
                Musician m = band[i];
                Fighter f = new Fighter();
                f.Name = m.Name;
                f.Musician = m;
                f.Slot = i;
                f.MaxHp = BattleRules.TurnHp[(int)m.Family];
                f.Hp = (startHp != null && i < startHp.Length) ? Math.Min(startHp[i], f.MaxHp) : f.MaxHp;
                f.Speed = BattleRules.TurnSpeed[(int)m.Family];
                Party.Add(f);
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy e = enemies[i];
                Fighter f = new Fighter();
                f.Name = e.Name;
                f.IsEnemy = true;
                f.Enemy = e;
                f.Slot = i;
                f.MaxHp = BattleRules.TurnEnemyHp[(int)e.Kind];
                f.Hp = f.MaxHp;
                f.Speed = BattleRules.TurnEnemySpeed[(int)e.Kind];
                Enemies.Add(f);
            }

            Round = 0;
            if (!CheckEnd()) StartRound();
        }

        //======================================================================================
        //Turn Order
        //======================================================================================

        //Round Start : line up everybody still standing, fastest first, and let every enemy decide its move.
        //Ties : the band goes before TACET, then the order they were listed in.
        //A simple insertion sort, so equal speeds never swap places (List.Sort does not promise that).
        private void StartRound()
        {
            Round++;
            Order.Clear();

            List<Fighter> everyone = new List<Fighter>();
            everyone.AddRange(Party);
            everyone.AddRange(Enemies);

            for (int i = 0; i < everyone.Count; i++)
            {
                Fighter f = everyone[i];
                if (f.Down) continue;

                int at = Order.Count;
                while (at > 0 && Order[at - 1].Speed < f.Speed) at--;
                Order.Insert(at, f);
            }

            for (int i = 0; i < Enemies.Count; i++)
                if (!Enemies[i].Down) PlanIntent(Enemies[i]);

            OrderIndex = 0;
            BeginTurn();
        }

        //Enemy Intent : pick a musician to hit and how many notes, from the floor's range.
        //CADENZA : every third attack of a boss is its big move, one note more and each note harder.
        private void PlanIntent(Fighter enemy)
        {
            enemy.IntentTarget = PickTarget();

            int count = random.Next(BattleRules.TurnNotesMin[Floor - 1], BattleRules.TurnNotesMax[Floor - 1] + 1);
            int hit = Math.Max(1, (int)Math.Round(enemy.Enemy.SongNotePower() * BattleRules.TurnNoteDamage));   // SongNotePower = the average of its Pattern notes

            enemy.IntentCadenza = enemy.Enemy.Kind == EnemyKind.Boss && (enemy.Attacks + 1) % BattleRules.TurnCadenzaEvery == 0;
            if (enemy.IntentCadenza)
            {
                count++;
                hit = (int)Math.Round(hit * BattleRules.TurnCadenzaPower);
            }

            enemy.IntentNotes = new int[count];
            enemy.IntentWays = new Flick[count];
            enemy.IntentSizes = new int[count];
            for (int i = 0; i < count; i++)
            {
                enemy.IntentNotes[i] = hit;
                enemy.IntentWays[i] = (Flick)(1 + random.Next(4));     // Up, Down, Left or Right
                enemy.IntentSizes[i] = random.Next(3);
            }
        }

        //Pick Target : a musician with TAUNT up if there is one, else anyone standing
        private Fighter PickTarget()
        {
            for (int i = 0; i < Party.Count; i++)
                if (Party[i].Taunt && !Party[i].Down) return Party[i];

            List<Fighter> standing = new List<Fighter>();
            for (int i = 0; i < Party.Count; i++)
                if (!Party[i].Down) standing.Add(Party[i]);

            if (standing.Count == 0) return null;
            return standing[random.Next(standing.Count)];
        }

        //Turn Start : tell the screen whose turn it is. GUARD and TAUNT end when their owner is up again.
        //OFF-BEAT : an enemy thrown off the beat loses this turn (its statuses still count down).
        //An enemy whose target fell, or who is now taunted, aims again, so the attack it showed still happens.
        private void BeginTurn()
        {
            while (true)
            {
                Fighter f = Order[OrderIndex];
                f.Guard = false;
                f.Taunt = false;

                if (f.IsEnemy && f.Staggered)
                {
                    f.Staggered = false;
                    Events.Add(new TurnEvent(TurnEventKind.Skip, f, 0));
                    EndOfTurn(f);
                    if (CheckEnd()) return;
                    if (!Advance())
                    {
                        StartRound();
                        return;
                    }
                    continue;
                }

                if (f.IsEnemy)
                {
                    Fighter taunt = PickTaunt();
                    if (taunt != null) f.IntentTarget = taunt;
                    else if (f.IntentTarget == null || f.IntentTarget.Down) f.IntentTarget = PickTarget();
                    parried = 0;
                    Step = TurnStep.EnemyTurn;
                }
                else
                {
                    Step = TurnStep.PlayerTurn;
                }
                return;
            }
        }

        private Fighter PickTaunt()
        {
            for (int i = 0; i < Party.Count; i++)
                if (Party[i].Taunt && !Party[i].Down) return Party[i];
            return null;
        }

        //Advance : move to the next one standing this round, false when the round is over
        private bool Advance()
        {
            OrderIndex++;
            while (OrderIndex < Order.Count && Order[OrderIndex].Down) OrderIndex++;
            return OrderIndex < Order.Count;
        }

        //Next Turn : the one who just acted counts down its statuses, then the fight ends or the next one is up.
        //When everybody has had a turn a new round starts.
        private void NextTurn()
        {
            EndOfTurn(Order[OrderIndex]);
            if (CheckEnd()) return;

            if (Advance()) BeginTurn();
            else StartRound();
        }

        //End Of Turn : DISSONANCE hurts, then fades by one. MUTE counts down.
        private void EndOfTurn(Fighter f)
        {
            if (f.Dissonance > 0 && !f.Down)
            {
                Hurt(f, f.Dissonance);
                f.Dissonance--;
            }
            if (f.Mute > 0) f.Mute--;
        }

        //Win / Lose : every enemy down wins, every musician down loses
        private bool CheckEnd()
        {
            if (AllDown(Enemies)) { Step = TurnStep.Won; return true; }
            if (AllDown(Party)) { Step = TurnStep.Lost; return true; }
            return false;
        }

        private static bool AllDown(List<Fighter> side)
        {
            for (int i = 0; i < side.Count; i++)
                if (!side[i].Down) return false;
            return true;
        }

        //======================================================================================
        //Player Turn
        //======================================================================================

        //Can Target : the player may only attack an enemy that is still standing
        public bool CanTarget(Fighter f)
        {
            return f != null && f.IsEnemy && !f.Down;
        }

        //Can Use : enough BREATH, and not under MUTE
        public bool CanUse(Skill s)
        {
            return Step == TurnStep.PlayerTurn && s != null && Breath >= s.Cost && Current.Mute == 0;
        }

        //Player Attack : the musician whose turn it is hits one enemy.
        //grade is how the swipe landed on the ring (a swipe the wrong way counts as Miss), and
        //rightSize is false when it was on time but the wrong size. Returns the damage done.
        public int Attack(Fighter target, Grade grade, bool rightSize)
        {
            if (Step != TurnStep.PlayerTurn || !CanTarget(target)) return 0;

            Fighter attacker = Current;
            float scale = Scale(grade, rightSize);
            if (Has(MotifId.Sforzando) && grade == Grade.Perfect && rightSize && AttackSize(attacker.Musician) == 2) scale *= BattleRules.TurnSforzandoScale;   // SFORZANDO
            int damage = HitDamage(attacker, target, scale);
            Hurt(target, damage);
            AddBreath(BattleRules.TurnBreathPerAttack);

            int offBeat = BattleRules.TurnPerfectOffBeat * (Has(MotifId.Marcato) ? BattleRules.TurnMarcatoOffBeat : 1);   // MARCATO
            if (grade == Grade.Perfect && rightSize) AddOffBeat(target, offBeat);                        // OFF-BEAT
            if (grade != Grade.Perfect && grade != Grade.Good) attacker.Crescendo = 0;                    // CRESCENDO is lost on a MISS
            CountNote(grade == Grade.Perfect && rightSize);

            NextTurn();
            return damage;
        }

        //Player Skill : the musician whose turn it is plays a skill. target is the enemy or the musician
        //it was played at (ignored for skills that pick their own targets). results holds how each note
        //went, in order. EVERY NOTE THAT LANDS DOES THE SKILL ONCE (see Data/Skill.cs).
        //Returns false when the skill could not be played.
        public bool UseSkill(Skill s, Fighter target, NoteResult[] results)
        {
            if (!CanUse(s)) return false;

            Fighter user = Current;
            List<Fighter> targets = TargetsOf(s, target, user);
            if (targets.Count == 0) return false;

            Breath -= s.Cost;
            bool anyLanded = false;

            for (int n = 0; n < s.Notes.Length; n++)
            {
                NoteResult r = (results != null && n < results.Length) ? results[n] : new NoteResult(Grade.Hesitate, true, 1f);
                CountNote(r.Grade == Grade.Perfect && r.RightSize);
                if (!r.Lands)
                {
                    user.Crescendo = 0;                                                     // CRESCENDO is lost on a MISS
                    continue;
                }
                anyLanded = true;

                SkillNote note = s.Notes[n];
                float hold = 1f;
                if (note.Kind == NoteKind.Hold) hold = HoldScale(Has(MotifId.Tenuto) ? 1f : r.Hold);     // HOLD, TENUTO
                float scale = Scale(r.Grade, r.RightSize) * hold;
                if (Has(MotifId.Sforzando) && r.Grade == Grade.Perfect && r.RightSize && note.Size == 2) scale *= BattleRules.TurnSforzandoScale;   // SFORZANDO

                bool everyone = s.Target == SkillTarget.AllEnemies || s.Target == SkillTarget.AllAllies;
                float tutti = everyone && Has(MotifId.Tutti) ? BattleRules.TurnTuttiScale : 1f;               // TUTTI
                float heal = s.Heal * hold * tutti;
                if (user.Musician.Family == Family.Wind && Has(MotifId.ReedCase)) heal *= BattleRules.TurnReedCaseHeal;   // REED CASE
                int offBeat = s.OffBeat;
                if (user.Musician.Family == Family.Percussion && Has(MotifId.SpareSticks)) offBeat = (int)Math.Round(offBeat * BattleRules.TurnSpareSticksOffBeat);   // SPARE STICKS

                //GRACE NOTE : a spark that lands does the skill twice
                int times = note.Kind == NoteKind.Spark && Has(MotifId.GraceNote) ? BattleRules.TurnGraceTimes : 1;
                for (int k = 0; k < times; k++)
                {
                    for (int t = 0; t < targets.Count; t++)
                    {
                        Fighter f = targets[t];
                        if (f.Down) continue;

                        if (f.IsEnemy)
                        {
                            if (s.Damage > 0f) Hurt(f, HitDamage(user, f, scale * s.Damage * tutti));
                            if (s.Dissonance > 0 && !f.Down) AddDissonance(f, s.Dissonance);
                            if (offBeat > 0 && !f.Down) AddOffBeat(f, offBeat);
                        }
                        else
                        {
                            if (heal > 0f) Heal(f, (int)Math.Round(heal));
                            if (s.Crescendo > 0) AddCrescendo(f, s.Crescendo);
                        }
                    }

                    if (s.Breath > 0) AddBreath(s.Breath);
                }
            }

            //GUARD and TAUNT : the percussion stands in front, every enemy turns to them at once
            if (anyLanded && s.Guard) user.Guard = true;
            if (anyLanded && s.Taunt)
            {
                user.Taunt = true;
                for (int i = 0; i < Enemies.Count; i++)
                    if (!Enemies[i].Down) Enemies[i].IntentTarget = user;
            }

            NextTurn();
            return true;
        }

        //Targets Of : who a skill reaches. An enemy or a musician that is down cannot be picked.
        private List<Fighter> TargetsOf(Skill s, Fighter target, Fighter user)
        {
            List<Fighter> list = new List<Fighter>();

            if (s.Target == SkillTarget.Enemy && CanTarget(target)) list.Add(target);
            if (s.Target == SkillTarget.Self) list.Add(user);
            if (s.Target == SkillTarget.Ally && target != null && !target.IsEnemy && !target.Down) list.Add(target);

            if (s.Target == SkillTarget.AllEnemies)
                for (int i = 0; i < Enemies.Count; i++)
                    if (!Enemies[i].Down) list.Add(Enemies[i]);

            if (s.Target == SkillTarget.AllAllies)
                for (int i = 0; i < Party.Count; i++)
                    if (!Party[i].Down) list.Add(Party[i]);

            return list;
        }

        //Swipe Scale : how much of the hit lands, from the table in ALPHA7_DESIGN 5.3
        public static float SwipeScale(Grade grade, bool rightSize)
        {
            if (grade != Grade.Perfect && grade != Grade.Good) return BattleRules.TurnMiss;
            if (!rightSize) return BattleRules.TurnWrongSize;
            return grade == Grade.Perfect ? BattleRules.TurnPerfect : BattleRules.TurnGood;
        }

        //Hold Scale : a held note gives TurnHoldBase let go at once, up to TurnHoldBase + TurnHoldGain held to the end
        public static float HoldScale(float held)
        {
            return BattleRules.TurnHoldBase + BattleRules.TurnHoldGain * MathF.Max(0f, MathF.Min(1f, held));
        }

        //Hit Damage : power x TurnAttackPerPower x scale, harder with CRESCENDO and against an enemy OFF-BEAT
        public int HitDamage(Fighter attacker, Fighter target, float scale)
        {
            float power = attacker.IsEnemy ? attacker.Enemy.SongNotePower() : PowerOf(attacker.Musician);
            float damage = power * BattleRules.TurnAttackPerPower * scale;
            if (target.IsEnemy && target.Hp < target.MaxHp * BattleRules.TurnCodaBelow && Has(MotifId.Coda)) damage *= BattleRules.TurnCodaScale;   // CODA
            if (!attacker.IsEnemy && !overtureDone && Has(MotifId.Overture))                  // OVERTURE
            {
                damage *= BattleRules.TurnOvertureScale;
                overtureDone = true;
            }
            damage *= 1f + attacker.Crescendo * BattleRules.TurnCrescendoStep;               // CRESCENDO
            if (!attacker.IsEnemy) damage *= BeethovenScale();                                 // THE CLOSER THE LOUDER
            if (target.Staggered) damage *= BattleRules.TurnOffBeatScale;                     // OFF-BEAT
            return Math.Max(1, (int)Math.Round(damage));
        }

        //======================================================================================
        //Enemy Turn
        //======================================================================================

        //Enemy Attack : the enemy whose turn it is plays its notes at its target.
        //grades holds the parry swipe for each note in order. A note with no grade (the array is
        //short) was not swiped at all and lands whole. Returns the damage taken.
        public int TakeEnemyAttack(Grade[] grades)
        {
            if (Step != TurnStep.EnemyTurn) return 0;

            int total = 0;
            for (int i = 0; i < Current.IntentNotes.Length; i++)
            {
                Grade g = (grades != null && i < grades.Length) ? grades[i] : Grade.Hesitate;
                total += ParryNote(i, g);
            }

            FinishEnemyTurn();
            return total;
        }

        //Parry Note : one note of the enemy's attack lands, swiped back with this grade.
        //Returns the damage it did to the target.
        public int ParryNote(int note, Grade g)
        {
            if (Step != TurnStep.EnemyTurn) return 0;

            Fighter enemy = Current;
            Fighter target = enemy.IntentTarget;
            if (note < 0 || note >= enemy.IntentNotes.Length || target == null) return 0;

            //PARRY : a PERFECT swipe blocks the note, gives breath back and shakes the enemy (OFF-BEAT)
            CountNote(g == Grade.Perfect);
            if (g == Grade.Perfect)
            {
                parried++;
                AddBreath(BattleRules.TurnBreathPerParry);
                if (Has(MotifId.Pianissimo) && enemy.IntentSizes[note] == 0) AddBreath(BattleRules.TurnPianissimoBreath);   // PIANISSIMO
                AddOffBeat(enemy, BattleRules.TurnParryOffBeat);
                return 0;
            }

            int damage = NoteDamage(enemy.IntentNotes[note], g);
            if (target.Guard) damage = (int)Math.Ceiling(damage * BattleRules.TurnGuardKeep);      // GUARD
            Hurt(target, damage);

            if (g != Grade.Good)
            {
                target.Crescendo = 0;                                                        // CRESCENDO is lost on a MISS

                //SILENT MOUTHS : THE MUTE CHOIR's notes that get through take the target's skills away
                if (enemy.Enemy.Trait == EnemyTrait.SilentMouths && !target.Down)
                {
                    target.Mute = BattleRules.TurnMuteTurns;
                    Events.Add(new TurnEvent(TurnEventKind.Mute, target, target.Mute));
                }
            }
            return damage;
        }

        //Note Damage : what gets through a parry. PERFECT nothing, GOOD a share, anything else all of it.
        public static int NoteDamage(int note, Grade g)
        {
            if (g == Grade.Perfect) return 0;
            if (g == Grade.Good) return (int)Math.Ceiling(note * BattleRules.TurnParryGoodKeep);
            return note;
        }

        //Enemy Turn End : after its last note. COUNTER : when every note was parried PERFECT, the
        //target hits back for free, twice as hard against a CADENZA. Then on to whoever is next.
        public void FinishEnemyTurn()
        {
            if (Step != TurnStep.EnemyTurn) return;

            Fighter enemy = Current;
            Fighter target = enemy.IntentTarget;
            enemy.Attacks++;

            if (parried > 0 && parried == enemy.IntentNotes.Length && target != null && !target.Down && !enemy.Down)
            {
                float scale = BattleRules.TurnCounterScale * (enemy.IntentCadenza ? BattleRules.TurnCadenzaCounter : 1f);
                if (Has(MotifId.Counterpoint)) scale *= BattleRules.TurnCounterpointScale;                     // COUNTERPOINT
                int damage = HitDamage(target, enemy, scale);
                Events.Add(new TurnEvent(TurnEventKind.Counter, target, damage));
                Hurt(enemy, damage);
            }

            enemy.IntentCadenza = false;
            NextTurn();
        }

        //Judge : how a swipe landed. offset is the seconds between the swipe and the moment the
        //ring meets the note (minus is early). The wrong way is always a MISS.
        public Grade Judge(float offset, bool rightWay)
        {
            float steady = Has(MotifId.SteadyPulse) ? BattleRules.SteadyWindowBonus : 0f;           // STEADY PULSE
            float perfect = (Maestro ? BattleRules.MaestroPerfectWindow : BattleRules.PerfectWindow) + steady;   // MAESTRO
            float good = (Maestro ? BattleRules.MaestroGoodWindow : BattleRules.GoodWindow) + steady;
            float gap = Math.Abs(offset);

            if (!rightWay || gap > good) return Grade.Miss;
            return gap <= perfect ? Grade.Perfect : Grade.Good;
        }

        //Roll Grade : how many shakes a roll note got (TREMOLO)
        public Grade RollGrade(int shakes)
        {
            int perfect = Maestro ? BattleRules.MaestroTurnRollPerfect : BattleRules.TurnRollPerfect;   // MAESTRO
            int good = Maestro ? BattleRules.MaestroTurnRollGood : BattleRules.TurnRollGood;
            if (Has(MotifId.Accelerando)) shakes *= BattleRules.TurnAccelerandoShakes;              // ACCELERANDO
            if (shakes >= perfect) return Grade.Perfect;
            if (shakes >= good) return Grade.Good;
            return shakes > 0 ? Grade.Miss : Grade.Hesitate;
        }

        //Late Limit : after this many seconds past the ring a note counts as not swiped at all
        public float LateLimit
        {
            get { return (Maestro ? BattleRules.MaestroGoodWindow : BattleRules.GoodWindow) + (Has(MotifId.SteadyPulse) ? BattleRules.SteadyWindowBonus : 0f); }
        }

        //ATTACK PART : a musician's attack is the PART written on them since round 14, so each
        //one always swings the same way : the size of their family (winds p, strings mf,
        //percussion f) and the way of their arrow (left, down, right).
        public static int AttackSize(Musician m)
        {
            if (m.Family == Family.Wind) return 0;
            if (m.Family == Family.String) return 1;
            return 2;
        }

        public static Flick AttackWay(Musician m)
        {
            if (m.Cue == 0) return Flick.Left;
            if (m.Cue == 2) return Flick.Right;
            return Flick.Down;
        }

        //======================================================================================
        //Conductor (step 5) : PASSIVE and SIGNATURE
        //======================================================================================

        //Count Note : every PERFECT fills the SIGNATURE gauge (a conductor with no kit has none).
        //PRODIGY (THE SHADOWLESS) : TurnMozartRun PERFECTs in a row give 1 breath back.
        private void CountNote(bool perfect)
        {
            if (!perfect)
            {
                perfectRun = 0;
                return;
            }

            if (Kit != TurnKit.None && Signature < SignatureNeed)
            {
                Signature++;
                if (Signature == SignatureNeed) Events.Add(new TurnEvent(TurnEventKind.Ready, null, 0));
            }

            perfectRun++;
            if (perfectRun > BestRun) BestRun = perfectRun;
            if (Kit == TurnKit.Mozart && perfectRun >= BattleRules.TurnMozartRun)
            {
                perfectRun = 0;
                AddBreath(1);
            }
        }

        //THE CLOSER THE LOUDER (THE UNHEARING) : the band hits harder the less life it has left,
        //up to TurnBeethovenMost more with nobody standing. 1 for every other conductor.
        public float BeethovenScale()
        {
            if (Kit != TurnKit.Beethoven) return 1f;

            int hp = 0;
            int max = 0;
            for (int i = 0; i < Party.Count; i++)
            {
                hp += Party[i].Hp;
                max += Party[i].MaxHp;
            }
            if (max <= 0) return 1f;
            return 1f + BattleRules.TurnBeethovenMost * (1f - hp / (float)max);
        }

        //Can Signature : the gauge is full and a musician is up
        public bool CanSignature
        {
            get { return Step == TurnStep.PlayerTurn && Kit != TurnKit.None && Signature >= SignatureNeed; }
        }

        //Signature Need : PERFECTs to fill the gauge (CON BRIO needs fewer)
        public int SignatureNeed
        {
            get { return Has(MotifId.ConBrio) ? BattleRules.TurnConBrioNeed : BattleRules.TurnSignatureNeed; }   // CON BRIO
        }

        //Signature Shape : the 4/4 pattern a conductor beats, down, left, right, up (any size)
        public static readonly Flick[] SignatureShape = { Flick.Down, Flick.Left, Flick.Right, Flick.Up };

        //Signature : the conductor's big move. It is played before the musician's command and costs
        //no turn. strokes holds how the four strokes went. Its strength is their average swipe scale :
        //0.4 with all four missed, 1 all GOOD, 1.3 all PERFECT.
        public bool UseSignature(NoteResult[] strokes)
        {
            if (!CanSignature) return false;
            Signature = 0;

            float strength = 0f;
            int landed = 0;
            for (int i = 0; i < SignatureShape.Length; i++)
            {
                NoteResult r = (strokes != null && i < strokes.Length) ? strokes[i] : new NoteResult(Grade.Hesitate, true, 1f);
                strength += SwipeScale(r.Grade, true);
                if (r.Lands) landed++;
            }
            strength /= SignatureShape.Length;

            //LIGHTNING (THE UNHEARING) : every enemy is struck, and thrown OFF-BEAT at full strength
            if (Kit == TurnKit.Beethoven)
            {
                for (int i = 0; i < Enemies.Count; i++)
                {
                    Fighter e = Enemies[i];
                    if (e.Down) continue;
                    Hurt(e, Math.Max(1, (int)Math.Round(BattleRules.TurnBoltDamage * strength * BeethovenScale())));
                    if (!e.Down) AddOffBeat(e, (int)Math.Round(BattleRules.TurnOffBeatMax * strength));
                }
            }

            //FEATHER (THE SHADOWLESS) : one extra turn per stroke that lands, for the band in order,
            //right after the musician who is up now
            if (Kit == TurnKit.Mozart)
            {
                List<Fighter> extra = new List<Fighter>();
                for (int i = 0; i < Party.Count && extra.Count < landed; i++)
                    if (!Party[i].Down) extra.Add(Party[i]);
                for (int i = extra.Count - 1; i >= 0; i--) Order.Insert(OrderIndex + 1, extra[i]);
                Events.Add(new TurnEvent(TurnEventKind.Extra, null, extra.Count));
            }

            CheckEnd();
            return true;
        }

        //======================================================================================
        //Motifs (step 6)
        //======================================================================================

        //Give Motifs : the run's motifs, handed in once when the fight starts. BREATH MARK and
        //CRESCENDO change the start of the fight, the others are checked where they work (search the name).
        public void GiveMotifs(List<Motif> motifs)
        {
            for (int i = 0; i < motifs.Count; i++) motif[(int)motifs[i].Id] = true;

            if (Has(MotifId.BreathMark)) Breath = Math.Min(BattleRules.TurnBreathMax, Breath + BattleRules.TurnBreathMarkStart);   // BREATH MARK
            if (Has(MotifId.Crescendo))                                                                                             // CRESCENDO
                for (int i = 0; i < Party.Count; i++) Party[i].Crescendo = BattleRules.TurnCrescendoStart;
        }

        public bool Has(MotifId id)
        {
            return motif[(int)id];
        }

        //Power Of : a musician's power with rehearsals, and ROSIN for the strings (the same number every page shows)
        public int PowerOf(Musician m)
        {
            int power = m.Power + m.Rehearsed;
            if (m.Family == Family.String && Has(MotifId.Resin)) power += BattleRules.FamilyMotifPower;   // ROSIN
            return power;
        }

        //Scale : SwipeScale, with RUBATO softening a MISS
        public float Scale(Grade grade, bool rightSize)
        {
            if (grade != Grade.Perfect && grade != Grade.Good && Has(MotifId.Rubato)) return BattleRules.TurnRubatoMiss;   // RUBATO
            return SwipeScale(grade, rightSize);
        }

        //======================================================================================
        //Life, Breath and Statuses
        //======================================================================================

        private void Hurt(Fighter f, int amount)
        {
            if (amount <= 0 || f.Down) return;

            //SECOND WIND : once per fight, a musician who would fall stays up with 1 life
            if (!f.IsEnemy && amount >= f.Hp && Has(MotifId.SecondWind) && !secondWindUsed)
            {
                secondWindUsed = true;
                amount = f.Hp - 1;
                Events.Add(new TurnEvent(TurnEventKind.Saved, f, 0));
                if (amount <= 0) return;
            }

            f.Hp = Math.Max(0, f.Hp - amount);
            Events.Add(new TurnEvent(TurnEventKind.Damage, f, amount));
            if (f.Down)
            {
                f.Staggered = false;
                Events.Add(new TurnEvent(TurnEventKind.Down, f, 0));
            }
        }

        private void Heal(Fighter f, int amount)
        {
            if (amount <= 0 || f.Down) return;
            int before = f.Hp;
            f.Hp = Math.Min(f.MaxHp, f.Hp + amount);
            if (f.Hp > before) Events.Add(new TurnEvent(TurnEventKind.Heal, f, f.Hp - before));
        }

        private void AddBreath(int amount)
        {
            int before = Breath;
            Breath = Math.Max(0, Math.Min(BattleRules.TurnBreathMax, Breath + amount));
            if (Breath > before) Events.Add(new TurnEvent(TurnEventKind.Breath, null, Breath - before));
        }

        private void AddDissonance(Fighter f, int stacks)
        {
            f.Dissonance = Math.Min(BattleRules.TurnDissonanceMost, f.Dissonance + stacks);
            Events.Add(new TurnEvent(TurnEventKind.Dissonance, f, f.Dissonance));
        }

        private void AddCrescendo(Fighter f, int stacks)
        {
            f.Crescendo = Math.Min(BattleRules.TurnCrescendoMost, f.Crescendo + stacks);
            Events.Add(new TurnEvent(TurnEventKind.Crescendo, f, f.Crescendo));
        }

        //OFF-BEAT : fill the gauge, and when it is full the enemy is thrown off the beat
        private void AddOffBeat(Fighter enemy, int amount)
        {
            if (enemy.Staggered || enemy.Down) return;
            enemy.OffBeat += amount;
            if (enemy.OffBeat >= BattleRules.TurnOffBeatMax)
            {
                enemy.OffBeat = 0;
                enemy.Staggered = true;
                Events.Add(new TurnEvent(TurnEventKind.Break, enemy, 0));
            }
        }

        //KNOCKED OUT : life a musician keeps after the fight. Anyone who fell gets up with TurnReviveHp.
        public int HpAfterBattle(Fighter f)
        {
            return f.Down ? BattleRules.TurnReviveHp : f.Hp;
        }
    }
}
