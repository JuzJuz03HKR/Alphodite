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
    public enum TurnEventKind { Damage, Heal, Breath, Dissonance, Crescendo, Mute, Break, Skip, Counter, Down, Ready, Extra, Saved, Steal, Echo, Quieter }

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
        public int[] IntentShown = new int[0];     // the size each note SHOWS (FALSE NOTES can lie), the screen draws these
        public bool IntentRest;                    // LULLABY : this turn it hums and heals instead
        public bool IntentFast;                    // UNFINISHED : the notes come faster
        public bool IntentCadenza;                 // CADENZA : this attack is a boss's big move
        public int Attacks;                        // attacks made so far, a boss's every third is a CADENZA
        public bool Planned;                       // the intent is decided (step 10 : right after its last attack)

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

        //Band only (step 10)
        public float Echo;             // FUGUE : the next skill plays again at this strength, 0 for none
        public int Momentum;           // MOMENTUM (OTTO) : hits in a row, each one 1 power more
        public int Swings;             // FOUR BARS STRAIGHT (LUKA) : ATTACKs made this fight

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
        private int perfectRun;        // PERFECTs in a row, for the result page's BEST STREAK
        private int prodigyRun;        // PERFECTs in a row toward the next PRODIGY breath
        public int BestRun;            // the longest run of PERFECTs this fight, for the result page
        public bool FelixVoice;        // TWO VOICES (THE UNSIGNED) : false while FANNY's voice leads, true for FELIX's
        public int CapriceTurns;       // CAPRICE (THE HEARTLESS) : turns left in which no swipe is a MISS

        //Motifs (step 6) : what the band carries, see GiveMotifs and Data/Motif.cs (TurnText)
        private bool[] motif = new bool[64];
        private bool overtureDone;     // OVERTURE : the band's first hit is done (a MISS still hits, so it counts too)
        private bool secondWindUsed;   // SECOND WIND : once per fight

        //Last Skill : the swipe notes of the band's last skill, for ANSWER BETTER (THE NAMELESS MASTER)
        private List<Flick> lastWays = new List<Flick>();
        private List<int> lastSizes = new List<int>();

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
        public TurnBattle(List<Musician> band, List<Enemy> enemies, int floor, Random random, int[] startHp = null, int lifeBonus = 0)
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
                f.MaxHp = BattleRules.TurnHp[(int)m.Family] + lifeBonus;          // lifeBonus : the ARCHIVE's STRONGER BAND
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
                int life = e.TurnHp > 0 ? e.TurnHp : BattleRules.TurnEnemyHp[(int)e.Kind];
                f.MaxHp = (int)Math.Round(life * BattleRules.TurnFloorLife[Floor - 1]);       // deeper floors, tougher enemies
                f.Hp = f.MaxHp;
                f.Speed = e.TurnSpeed > 0 ? e.TurnSpeed : BattleRules.TurnEnemySpeed[(int)e.Kind];
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

            //Intent : decided once at the start, then again right after each of its attacks (FinishEnemyTurn),
            //so what shows over its head is always the attack still to come (step 10, like Slay the Spire)
            for (int i = 0; i < Enemies.Count; i++)
                if (!Enemies[i].Down && !Enemies[i].Planned) PlanIntent(Enemies[i]);

            OrderIndex = 0;
            BeginTurn();
        }

        //Enemy Intent (step 8) : the next of its PHRASES, fitted to the floor's length, at a musician.
        //CADENZA : every third attack of a boss is its big move, one note more and each note harder.
        //Its trait may change the plan, see the trait names below (from TraitFloor on).
        private void PlanIntent(Fighter enemy)
        {
            Enemy e = enemy.Enemy;
            bool trait = TraitOn(enemy);
            enemy.Planned = true;
            enemy.IntentTarget = PickTarget();
            enemy.IntentRest = false;
            enemy.IntentFast = false;
            enemy.IntentCadenza = false;

            //LULLABY (THE LULL) : every third turn it hums instead of attacking, and heals
            if (trait && e.Trait == EnemyTrait.Lullaby && (enemy.Attacks + 1) % BattleRules.TurnLullEvery == 0)
            {
                enemy.IntentRest = true;
                enemy.IntentNotes = new int[0];
                enemy.IntentWays = new Flick[0];
                enemy.IntentSizes = new int[0];
                enemy.IntentShown = new int[0];
                return;
            }

            //Phrase : its phrases are played in turn, so they can be learned
            List<Flick> ways = new List<Flick>();
            List<int> sizes = new List<int>();
            if (e.Phrases.Length > 0) ReadPhrase(e.Phrases[enemy.Attacks % e.Phrases.Length], ways, sizes);

            //ANSWER BETTER (THE NAMELESS MASTER) : it plays back the band's last skill
            if (trait && e.Trait == EnemyTrait.Mirror && lastWays.Count > 0)
            {
                ways = new List<Flick>(lastWays);
                sizes = new List<int>(lastSizes);
            }

            if (ways.Count == 0)                                             // no phrase written : one random note
            {
                ways.Add((Flick)(1 + random.Next(4)));
                sizes.Add(random.Next(3));
            }

            int count = Math.Max(BattleRules.TurnNotesMin[Floor - 1], Math.Min(BattleRules.TurnNotesMax[Floor - 1], ways.Count));
            if (trait && e.Trait == EnemyTrait.NoRest) count++;              // NO REST (WHITE NOISE)
            int power = e.TurnPower > 0 ? e.TurnPower : e.SongNotePower();                  // SongNotePower = the average of its Pattern notes
            int hit = Math.Max(1, (int)Math.Round(power * BattleRules.TurnNoteDamage * BattleRules.TurnFloorDamage[Floor - 1]));

            enemy.IntentCadenza = e.Kind == EnemyKind.Boss && (enemy.Attacks + 1) % BattleRules.TurnCadenzaEvery == 0;
            if (enemy.IntentCadenza)
            {
                count++;
                hit = (int)Math.Round(hit * BattleRules.TurnCadenzaPower);
            }

            enemy.IntentNotes = new int[count];
            enemy.IntentWays = new Flick[count];
            enemy.IntentSizes = new int[count];
            enemy.IntentShown = new int[count];
            for (int i = 0; i < count; i++)
            {
                enemy.IntentNotes[i] = hit;
                enemy.IntentWays[i] = ways[i % ways.Count];
                enemy.IntentSizes[i] = sizes[i % sizes.Count];
                enemy.IntentShown[i] = enemy.IntentSizes[i];
            }

            Unfinished(enemy);

            //FALSE NOTES (STATIC) : one note shows a size it is not
            if (trait && e.Trait == EnemyTrait.FalseNotes)
            {
                int k = random.Next(count);
                enemy.IntentShown[k] = (enemy.IntentSizes[k] + 1 + random.Next(2)) % 3;
            }
        }

        //UNFINISHED (REQUIEM) : below half life, left and right swap and the notes come faster.
        //Checked when it plans and again when its turn starts, so it changes as soon as it is hurt enough.
        private void Unfinished(Fighter enemy)
        {
            if (!TraitOn(enemy) || enemy.Enemy.Trait != EnemyTrait.Unfinished) return;
            if (enemy.IntentFast || enemy.Hp * 2 >= enemy.MaxHp) return;
            enemy.IntentFast = true;
            for (int i = 0; i < enemy.IntentWays.Length; i++)
            {
                if (enemy.IntentWays[i] == Flick.Left) enemy.IntentWays[i] = Flick.Right;
                else if (enemy.IntentWays[i] == Flick.Right) enemy.IntentWays[i] = Flick.Left;
            }
        }

        //Mirror Refresh : ANSWER BETTER (THE NAMELESS MASTER) answers the band's newest skill. Its next
        //attack keeps its length and takes the ways and sizes of the skill just played.
        private void RefreshMirror()
        {
            if (lastWays.Count == 0) return;
            for (int i = 0; i < Enemies.Count; i++)
            {
                Fighter f = Enemies[i];
                if (f.Down || f.IntentRest || !TraitOn(f) || f.Enemy.Trait != EnemyTrait.Mirror) continue;
                for (int n = 0; n < f.IntentWays.Length; n++)
                {
                    f.IntentWays[n] = lastWays[n % lastWays.Count];
                    f.IntentSizes[n] = lastSizes[n % lastSizes.Count];
                    f.IntentShown[n] = f.IntentSizes[n];
                }
            }
        }

        //Read Phrase : "Lp Rmf Df" into ways and sizes
        private static void ReadPhrase(string phrase, List<Flick> ways, List<int> sizes)
        {
            string[] notes = phrase.Split(' ');
            for (int i = 0; i < notes.Length; i++)
            {
                string n = notes[i];
                if (n.Length < 2) continue;

                Flick way = Flick.Down;
                if (n[0] == 'L') way = Flick.Left;
                if (n[0] == 'R') way = Flick.Right;
                if (n[0] == 'U') way = Flick.Up;

                string mark = n.Substring(1);
                int size = mark == "p" ? 0 : (mark == "f" ? 2 : 1);
                ways.Add(way);
                sizes.Add(size);
            }
        }

        //Trait On : enemy traits switch on from their TraitFloor (ordinary enemies from floor 2)
        public bool TraitOn(Fighter enemy)
        {
            return enemy.IsEnemy && enemy.Enemy.Trait != EnemyTrait.None && Floor >= enemy.Enemy.TraitFloor;
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
                if (f.Taunt)
                {
                    //TAUNT ends : an enemy that planned its next attack while taunted aims again
                    f.Taunt = false;
                    for (int i = 0; i < Enemies.Count; i++)
                        if (!Enemies[i].Down && Enemies[i].IntentTarget == f) Enemies[i].IntentTarget = PickTarget();
                }

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
                    Unfinished(f);                                                           // UNFINISHED
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
            Fighter done = Order[OrderIndex];
            EndOfTurn(done);
            if (!done.IsEnemy && Kit == TurnKit.Fanny) FelixVoice = !FelixVoice;              // TWO VOICES : the other voice leads next
            if (CapriceTurns > 0)
            {
                CapriceTurns--;
                if (CapriceTurns == 0) PayCaprice();                                         // CAPRICE : the price comes due
            }
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

        //Can Use : enough BREATH, and not under MUTE.
        //BORROWED BREATH (THE HEARTLESS) : a skill short of BREATH can be paid in life instead (LifeCost)
        public bool CanUse(Skill s)
        {
            if (Step != TurnStep.PlayerTurn || s == null || Current.Mute > 0) return false;
            if (Breath >= s.Cost) return true;
            return Kit == TurnKit.Paganini && LifeCost(s) < Current.Hp;
        }

        //Life Cost : BORROWED BREATH, the life a skill takes for the BREATH it is short of (0 when there is enough).
        //It is never all of the musician's life, CanUse says no first.
        public int LifeCost(Skill s)
        {
            if (Kit != TurnKit.Paganini || s == null || Breath >= s.Cost) return 0;
            return (s.Cost - Breath) * BattleRules.TurnPaganiniLife;
        }

        //Any Size : LOCKED TEMPO (THE METRONOME), every size the baton swings is the right size
        public bool AnySize
        {
            get { return Kit == TurnKit.Bach; }
        }

        //Player Attack : the musician whose turn it is hits one enemy.
        //grade is how the swipe landed on the ring (a swipe the wrong way counts as Miss), and
        //rightSize is false when it was on time but the wrong size. Returns the damage done.
        public int Attack(Fighter target, Grade grade, bool rightSize)
        {
            if (Step != TurnStep.PlayerTurn || !CanTarget(target)) return 0;

            Fighter attacker = Current;
            MusicianTrait trait = attacker.Musician.Trait;
            bool lands = grade == Grade.Perfect || grade == Grade.Good;
            bool perfect = grade == Grade.Perfect && rightSize;
            float scale = Scale(grade, rightSize);
            if (Has(MotifId.Sforzando) && perfect && AttackSize(attacker.Musician) == 2) scale *= BattleRules.TurnSforzandoScale;   // SFORZANDO

            //FOUR BARS STRAIGHT (LUKA) : every fourth ATTACK hits twice as hard
            attacker.Swings++;
            if (trait == MusicianTrait.FourBars && attacker.Swings % BattleRules.TurnFourBarsEvery == 0) scale *= BattleRules.TurnFourBarsScale;

            int damage = HitDamage(attacker, target, scale);
            Hurt(target, damage);
            AddBreath(BattleRules.TurnBreathPerAttack);

            //HELD NOTE (CHAI) : his ATTACK rings on, a second hit for a share of the first
            if (trait == MusicianTrait.HeldNote && lands && !target.Down)
                Hurt(target, Math.Max(1, (int)Math.Round(damage * BattleRules.TurnHeldNoteShare)));

            //THE WHOLE FLOOR (BORIS) : a PERFECT f swing takes some off each note of the enemy's next attack
            if (trait == MusicianTrait.Thunder && perfect && AttackSize(attacker.Musician) == 2) Quieten(target);

            CountHit(attacker, lands);                                                               // MOMENTUM

            int offBeat = BattleRules.TurnPerfectOffBeat * (Has(MotifId.Marcato) ? BattleRules.TurnMarcatoOffBeat : 1);   // MARCATO
            if (perfect) AddOffBeat(target, offBeat);                                                    // OFF-BEAT
            if (!lands) attacker.Crescendo = 0;                                                          // CRESCENDO is lost on a MISS
            CountNote(perfect);

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

            int lifeCost = LifeCost(s);                                                            // BORROWED BREATH
            Breath = Math.Max(0, Breath - s.Cost);
            if (lifeCost > 0) Hurt(user, lifeCost);
            bool anyLanded = false;

            //Last Skill : remembered for ANSWER BETTER
            lastWays.Clear();
            lastSizes.Clear();
            for (int n = 0; n < s.Notes.Length; n++)
            {
                if (s.Notes[n].Kind != NoteKind.Swipe && s.Notes[n].Kind != NoteKind.Hold) continue;
                lastWays.Add(s.Notes[n].Way);
                lastSizes.Add(s.Notes[n].Size);
            }
            RefreshMirror();

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
                PlayNote(s, s.Notes[n], r, targets, user, 1f, true);
            }
            CountHit(user, anyLanded);                                                      // MOMENTUM

            //FUGUE (THE METRONOME) : an echo waiting on this musician plays the notes that landed once more
            if (user.Echo > 0f && anyLanded && Echoes(s))
            {
                float echo = user.Echo;
                user.Echo = 0f;
                Events.Add(new TurnEvent(TurnEventKind.Echo, user, 0));
                for (int n = 0; n < s.Notes.Length; n++)
                {
                    NoteResult r = (results != null && n < results.Length) ? results[n] : new NoteResult(Grade.Hesitate, true, 1f);
                    if (r.Lands) PlayNote(s, s.Notes[n], r, targets, user, echo, false);
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

        //Play Note : one note of a skill that landed does the skill once on every target.
        //extra scales all of it (1, or the FUGUE echo's strength). withBreath is false for the echo,
        //which repeats what the notes do to the fighters but gives no BREATH (BREATH-saving gets strong fast).
        private void PlayNote(Skill s, SkillNote note, NoteResult r, List<Fighter> targets, Fighter user, float extra, bool withBreath)
        {
            float hold = 1f;
            if (note.Kind == NoteKind.Hold) hold = HoldScale(Has(MotifId.Tenuto) ? 1f : r.Hold);     // HOLD, TENUTO
            float scale = Scale(r.Grade, r.RightSize) * hold * extra;
            bool perfect = r.Grade == Grade.Perfect && r.RightSize;
            if (Has(MotifId.Sforzando) && perfect && note.Size == 2) scale *= BattleRules.TurnSforzandoScale;   // SFORZANDO

            bool everyone = s.Target == SkillTarget.AllEnemies || s.Target == SkillTarget.AllAllies;
            float tutti = everyone && Has(MotifId.Tutti) ? BattleRules.TurnTuttiScale : 1f;               // TUTTI
            float heal = s.Heal * hold * tutti * extra;
            if (user.Musician.Family == Family.Wind && Has(MotifId.ReedCase)) heal *= BattleRules.TurnReedCaseHeal;   // REED CASE
            int crescendo = s.Crescendo;
            if (Kit == TurnKit.Fanny && FelixVoice)                                                       // TWO VOICES : FELIX helps
            {
                heal *= BattleRules.TurnFelixHeal;
                if (crescendo > 0) crescendo += BattleRules.TurnFelixCrescendo;
            }
            int offBeat = (int)Math.Round(s.OffBeat * extra);
            if (user.Musician.Family == Family.Percussion && Has(MotifId.SpareSticks)) offBeat = (int)Math.Round(offBeat * BattleRules.TurnSpareSticksOffBeat);   // SPARE STICKS
            bool thunder = user.Musician.Trait == MusicianTrait.Thunder && perfect && note.Size == 2;     // THE WHOLE FLOOR

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
                        if (thunder && !f.Down) Quieten(f);
                    }
                    else
                    {
                        if (heal > 0f) Heal(f, (int)Math.Round(heal));
                        if (crescendo > 0) AddCrescendo(f, crescendo);
                    }
                }

                if (withBreath && s.Breath > 0) AddBreath(s.Breath);
            }
        }

        //Echoes : FUGUE only repeats what a skill does to the fighters, so a skill that only gives BREATH or
        //stands guard (LONG BREATH, STAND FIRM) leaves the echo waiting for the next one
        private static bool Echoes(Skill s)
        {
            return s.Damage > 0f || s.Heal > 0f || s.Dissonance > 0 || s.Crescendo > 0 || s.OffBeat > 0;
        }

        //Count Hit : MOMENTUM (OTTO), a turn that lands adds one, a turn that misses starts again
        private void CountHit(Fighter f, bool landed)
        {
            if (f.Musician.Trait != MusicianTrait.Momentum) return;
            f.Momentum = landed ? Math.Min(BattleRules.TurnMomentumMost, f.Momentum + 1) : 0;
        }

        //Quieten : THE WHOLE FLOOR (BORIS), every note of the enemy's next attack hits a little softer (at least 1)
        private void Quieten(Fighter enemy)
        {
            if (enemy.IntentNotes.Length == 0) return;
            for (int i = 0; i < enemy.IntentNotes.Length; i++)
                enemy.IntentNotes[i] = Math.Max(1, enemy.IntentNotes[i] - BattleRules.TurnThunderSoften);
            Events.Add(new TurnEvent(TurnEventKind.Quieter, enemy, BattleRules.TurnThunderSoften));
        }

        //Shows Size : the enemy's next attack, as shown over its head, has a note of this size (0 p, 1 mf, 2 f)
        private static bool ShowsSize(Fighter enemy, int size)
        {
            if (!enemy.IsEnemy || enemy.IntentRest) return false;
            for (int i = 0; i < enemy.IntentShown.Length; i++)
                if (enemy.IntentShown[i] == size) return true;
            return false;
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
            float power = attacker.IsEnemy ? attacker.Enemy.SongNotePower() : PowerOf(attacker.Musician) + attacker.Momentum;   // MOMENTUM
            float damage = power * BattleRules.TurnAttackPerPower * scale;
            if (target.IsEnemy && target.Hp < target.MaxHp * BattleRules.TurnCodaBelow && Has(MotifId.Coda)) damage *= BattleRules.TurnCodaScale;   // CODA
            if (!attacker.IsEnemy && !overtureDone && Has(MotifId.Overture))                  // OVERTURE
            {
                damage *= BattleRules.TurnOvertureScale;
                overtureDone = true;
            }
            damage *= 1f + attacker.Crescendo * BattleRules.TurnCrescendoStep;               // CRESCENDO
            if (!attacker.IsEnemy) damage *= BeethovenScale();                                 // THE CLOSER THE LOUDER
            if (!attacker.IsEnemy) damage *= TraitScale(attacker, target);                     // the musician's own trait
            if (!attacker.IsEnemy && Kit == TurnKit.Fanny && !FelixVoice) damage *= 1f + BattleRules.TurnFannyScale;   // TWO VOICES : FANNY hits
            if (target.Staggered) damage *= BattleRules.TurnOffBeatScale;                     // OFF-BEAT
            return Math.Max(1, (int)Math.Round(damage));
        }

        //Trait Scale : the traits that make a musician hit harder against some enemies
        private static float TraitScale(Fighter attacker, Fighter target)
        {
            MusicianTrait trait = attacker.Musician.Trait;
            if (trait == MusicianTrait.QuietPart && ShowsSize(target, 0)) return BattleRules.TurnQuietScale;          // THE QUIET PART (KLARA)
            if (trait == MusicianTrait.OneStepBetter && ShowsSize(target, 2)) return BattleRules.TurnStepBetterScale; // ONE STEP BETTER (NUAN)
            if (trait == MusicianTrait.ByEar && target.Staggered) return BattleRules.TurnByEarScale;                  // BY EAR (MALI)
            return 1f;
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
                bool noRest = TraitOn(enemy) && enemy.Enemy.Trait == EnemyTrait.NoRest;              // NO REST : no breath from it
                if (!noRest) AddBreath(BattleRules.TurnBreathPerParry);
                if (!noRest && Has(MotifId.Pianissimo) && enemy.IntentSizes[note] == 0) AddBreath(BattleRules.TurnPianissimoBreath);   // PIANISSIMO
                AddOffBeat(enemy, BattleRules.TurnParryOffBeat);
                return 0;
            }

            int damage = NoteDamage(enemy.IntentNotes[note], g);
            bool trait = TraitOn(enemy);
            if (trait && enemy.Enemy.Trait == EnemyTrait.FillsGaps && g != Grade.Good)             // FILLS THE GAPS (DEAD AIR)
                damage = (int)Math.Round(damage * BattleRules.TurnFillsGapsScale);
            if (target.Guard) damage = (int)Math.Ceiling(damage * BattleRules.TurnGuardKeep);      // GUARD
            Hurt(target, damage);
            if (trait && enemy.Enemy.Trait == EnemyTrait.EchoFades && damage > 0 && !target.Down)  // ECHO FADES (HUSH)
                AddDissonance(target, 1);

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

            //LULLABY : the humming turn heals it
            if (enemy.IntentRest) Heal(enemy, BattleRules.TurnLullHeal);

            //THE BARGAIN (THE DEVIL'S STRING) : every attack steals breath
            if (TraitOn(enemy) && enemy.Enemy.Trait == EnemyTrait.Bargain && enemy.IntentNotes.Length > 0 && Breath > 0)
            {
                int stolen = Math.Min(Breath, BattleRules.TurnBargainSteal);
                Breath -= stolen;
                Events.Add(new TurnEvent(TurnEventKind.Steal, enemy, stolen));
            }

            if (parried > 0 && parried == enemy.IntentNotes.Length && target != null && !target.Down && !enemy.Down)
            {
                float scale = BattleRules.TurnCounterScale * (enemy.IntentCadenza ? BattleRules.TurnCadenzaCounter : 1f);
                if (Has(MotifId.Counterpoint)) scale *= BattleRules.TurnCounterpointScale;                     // COUNTERPOINT
                int damage = HitDamage(target, enemy, scale);
                Events.Add(new TurnEvent(TurnEventKind.Counter, target, damage));
                Hurt(enemy, damage);
            }

            //The next attack is decided now, so its intent shows until it comes (a fallen enemy has none)
            enemy.IntentCadenza = false;
            if (!enemy.Down) PlanIntent(enemy);
            NextTurn();
        }

        //Judge : how a swipe landed. offset is the seconds between the swipe and the moment the
        //ring meets the note (minus is early). The wrong way is always a MISS.
        //A parry happens on the enemy's turn, every other swipe on a musician's (the one who is up).
        public Grade Judge(float offset, bool rightWay)
        {
            float steady = Has(MotifId.SteadyPulse) ? BattleRules.SteadyWindowBonus : 0f;           // STEADY PULSE
            float perfect = (Maestro ? BattleRules.MaestroPerfectWindow : BattleRules.PerfectWindow) + steady;   // MAESTRO
            float good = (Maestro ? BattleRules.MaestroGoodWindow : BattleRules.GoodWindow) + steady;
            bool parry = Step == TurnStep.EnemyTurn;
            MusicianTrait trait = !parry && Current != null && !Current.IsEnemy ? Current.Musician.Trait : MusicianTrait.None;

            if (parry && Kit == TurnKit.Lully)                                                      // STEADY HAND (THE KING'S BATON)
            {
                perfect += BattleRules.TurnLullyWindow;
                good += BattleRules.TurnLullyWindow;
            }
            if (trait == MusicianTrait.KeepsCount) perfect = Math.Min(good, perfect + BattleRules.TurnCountsWindow);   // COUNTS ALOUD (ANNA)

            float gap = Math.Abs(offset);
            Grade grade = Grade.Miss;
            if (rightWay && gap <= perfect) grade = Grade.Perfect;
            else if (rightWay && gap <= good) grade = Grade.Good;
            else if (rightWay && trait == MusicianTrait.Forgiven && offset > 0f && offset <= good + BattleRules.TurnLateBonus)
                grade = Grade.Good;                                                                  // FASHIONABLY LATE (IRIS)

            //CAPRICE : every swipe goes one better, a MISS is GOOD and a GOOD is PERFECT
            if (CapriceTurns > 0 && grade == Grade.Good) grade = Grade.Perfect;
            else if (CapriceTurns > 0 && grade == Grade.Miss) grade = Grade.Good;
            return grade;
        }

        //Roll Grade : how many shakes a roll note got (TREMOLO)
        public Grade RollGrade(int shakes)
        {
            int perfect = Maestro ? BattleRules.MaestroTurnRollPerfect : BattleRules.TurnRollPerfect;   // MAESTRO
            int good = Maestro ? BattleRules.MaestroTurnRollGood : BattleRules.TurnRollGood;
            if (Has(MotifId.Accelerando)) shakes *= BattleRules.TurnAccelerandoShakes;              // ACCELERANDO
            if (shakes >= perfect || (shakes >= good && CapriceTurns > 0)) return Grade.Perfect;   // CAPRICE : one better
            if (shakes >= good || (shakes > 0 && CapriceTurns > 0)) return Grade.Good;
            return shakes > 0 ? Grade.Miss : Grade.Hesitate;
        }

        //Late Limit : after this many seconds past the ring a note counts as not swiped at all
        //(STEADY HAND and FASHIONABLY LATE wait longer, see Judge)
        public float LateLimit
        {
            get
            {
                float late = (Maestro ? BattleRules.MaestroGoodWindow : BattleRules.GoodWindow) + (Has(MotifId.SteadyPulse) ? BattleRules.SteadyWindowBonus : 0f);
                if (Step == TurnStep.EnemyTurn && Kit == TurnKit.Lully) late += BattleRules.TurnLullyWindow;                       // STEADY HAND
                if (Step == TurnStep.PlayerTurn && Current != null && Current.Musician.Trait == MusicianTrait.Forgiven) late += BattleRules.TurnLateBonus;   // FASHIONABLY LATE
                return late;
            }
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
                prodigyRun = 0;
                return;
            }

            if (Kit != TurnKit.None && Signature < SignatureNeed)
            {
                Signature++;
                if (Signature == SignatureNeed) Events.Add(new TurnEvent(TurnEventKind.Ready, null, 0));
            }

            perfectRun++;
            prodigyRun++;
            if (perfectRun > BestRun) BestRun = perfectRun;
            if (Kit == TurnKit.Mozart && prodigyRun >= BattleRules.TurnMozartRun)
            {
                prodigyRun = 0;
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

            //FUGUE (THE METRONOME) : every musician standing gets an echo, their next skill plays again at this strength
            if (Kit == TurnKit.Bach)
            {
                for (int i = 0; i < Party.Count; i++)
                {
                    if (Party[i].Down) continue;
                    Party[i].Echo = strength;
                    Events.Add(new TurnEvent(TurnEventKind.Echo, Party[i], 0));
                }
            }

            //CAPRICE (THE HEARTLESS) : for one turn per stroke that lands every swipe goes one better (MISS
            //to GOOD, GOOD to PERFECT), the enemies' notes included. When it runs out every musician pays (PayCaprice).
            if (Kit == TurnKit.Paganini && landed > 0) CapriceTurns = landed;

            //THE SUN (THE KING'S BATON) : life back for every musician, and DISSONANCE and MUTE are gone
            if (Kit == TurnKit.Lully)
            {
                for (int i = 0; i < Party.Count; i++)
                {
                    Fighter f = Party[i];
                    if (f.Down) continue;
                    Heal(f, Math.Max(1, (int)Math.Round(BattleRules.TurnSunHeal * strength)));
                    f.Dissonance = 0;
                    f.Mute = 0;
                }
            }

            //TWO VOICES (THE UNSIGNED) : FANNY strikes every enemy while FELIX heals every musician
            if (Kit == TurnKit.Fanny)
            {
                for (int i = 0; i < Enemies.Count; i++)
                    if (!Enemies[i].Down) Hurt(Enemies[i], Math.Max(1, (int)Math.Round(BattleRules.TurnDuetDamage * strength)));
                for (int i = 0; i < Party.Count; i++)
                    if (!Party[i].Down) Heal(Party[i], Math.Max(1, (int)Math.Round(BattleRules.TurnDuetHeal * strength)));
            }

            CheckEnd();
            return true;
        }

        //Pay Caprice : CAPRICE has run out, every musician standing pays a share of their life, never below 1
        private void PayCaprice()
        {
            for (int i = 0; i < Party.Count; i++)
            {
                Fighter f = Party[i];
                if (f.Down) continue;
                int price = Math.Min(f.Hp - 1, Math.Max(1, (int)Math.Round(f.MaxHp * BattleRules.TurnCapricePrice)));
                if (price > 0) Hurt(f, price);
            }
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
