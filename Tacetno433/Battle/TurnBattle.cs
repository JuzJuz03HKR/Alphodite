using System;
using System.Collections.Generic;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Battle
{
    //TurnStep : what the turn battle is waiting for (Alpha 7)
    //   Plan         ONE BAR (step 12) : the player writes the bar, one move for each musician on the field
    //                in the order they want, may CALL one musician from the BENCH and play the SIGNATURE,
    //                then PlayBar starts it
    //   PlayerTurn   a musician plays their move : an ATTACK hits at once, a skill asks for its swipes
    //   EnemyTurn    an enemy is up : the screen shows its notes and asks for the parry swipes
    //   Won / Lost   the fight is over
    public enum TurnStep { Plan, PlayerTurn, EnemyTurn, Won, Lost }

    //TurnEventKind : something that happened, kept so the screen can show it (numbers, words, sparks).
    //The rules never draw, they only write down what happened (docs/research/ALPHA7_battle_ui.md).
    public enum TurnEventKind { Damage, Heal, Breath, Dissonance, Crescendo, Mute, Break, Skip, Counter, Down, Ready, Extra, Saved, Steal, Echo, Quieter, Called, Thorns }

    //DecreeKind : DECREE (THE DEAF CROWN, step 14), what the band may not do this bar
    public enum DecreeKind { None, NoSkills, NoAttacks }

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

    //BarMove : ONE BAR (step 12), one move the player wrote into the bar
    public class BarMove
    {
        public Fighter Who;
        public Skill Skill;            // null for ATTACK
        public Fighter Target;         // the enemy (ATTACK, or a skill at one enemy) or the musician it is for
        public bool Free;              // FEATHER : a free ATTACK, not the musician's turn (GUARD, TAUNT and statuses wait)
        public bool Unison;            // UNISON (step 13) : the last ATTACK of a bar that all went at one enemy
        public bool Forced;            // a skill that could not be played any more and became an ATTACK (DECREE lets it pass)
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
        public bool IntentAll;                     // THE COURT (THE STAFF, step 14) : this attack hits every musician standing
        public List<Flick> MissedWays = new List<Flick>();   // BY TOUCH (THE BLIND CANTOR, step 14) : notes that got through,
        public List<int> MissedSizes = new List<int>();      //          played again in its next attack
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
        public bool JustCalled;        // CALL (step 12) : walked in from the BENCH this bar, plays from the next one
        public int CalledBar;          // ENTRANCE (step 13) : the bar they were called in, 0 for the ones who started on the field
        public float Echo;             // FUGUE : the next skill plays again at this strength, 0 for none
        public int Momentum;           // MOMENTUM (OTTO) : hits in a row, each one 1 power more
        public int Swings;             // FOUR BARS STRAIGHT (LUKA) : ATTACKs made this fight

        public bool Down { get { return Hp <= 0; } }
    }

    //TurnBattle : THE RULES OF THE TURN BASED FIGHT (Alpha 7). No drawing code lives here.
    //ONE BAR (step 12, 24 Oct) : every round is a bar. A screen runs it like this :
    //   Plan       : PlanAttack / PlanSkill give a musician their move (the bar plays in the order they
    //                were given), Call brings one in from the BENCH, UseSignature plays the conductor's
    //                big move, PlayBar starts the bar. A musician with no move ATTACKs Focus.
    //   PlayerTurn : CurrentMove is the move. An ATTACK is Attack (no swipe), a skill is UseSkill with
    //                the notes the player swiped
    //   EnemyTurn  : the screen shows Current.IntentNotes flying at Current.IntentTarget, the player
    //                swipes back at each one, the screen calls ParryNote for each note as it lands
    //                and FinishEnemyTurn after the last (TakeEnemyAttack does all of it at once)
    //   After every call the screen reads Events to show what happened, then clears it.
    //   Won / Lost : the fight is over, HpAfterBattle gives each musician's life to keep
    //Every number comes from Data/BattleRules.cs (TURN BATTLE and ONE BAR).
    //Grade is the same enum the rhythm duel uses (Battle/BattleState.cs).
    public class TurnBattle
    {
        public List<Fighter> Party = new List<Fighter>();      // the musicians on the field
        public List<Fighter> Enemies = new List<Fighter>();

        //BENCH (step 12) : the musicians who did not start the fight, and their life. Call brings them in.
        public List<Musician> Bench = new List<Musician>();
        public List<int> BenchLife = new List<int>();
        public int Calls;              // musicians called this fight
        public bool CalledThisBar;     // one call per bar
        public Fighter LastCalled;     // ON CUE (THE CUE, step 13) : the musician called in last, null before the first call
        private int lifeBonus;         // the ARCHIVE's STRONGER BAND, for the ones called later

        //The Bar : the moves the player wrote, in the order they play (Plan)
        public List<BarMove> Bar = new List<BarMove>();
        public Fighter Focus;          // the enemy a musician with no move written ATTACKs

        //Turn Order : who plays in this bar, in order. OrderMoves holds the musicians' moves, null for TACET.
        public List<Fighter> Order = new List<Fighter>();
        public List<BarMove> OrderMoves = new List<BarMove>();
        public int OrderIndex;
        public int Round;              // the bar
        private int bandMovesDone;     // the band's own moves played so far in this bar (DOWNBEAT), FEATHER's free ATTACKs not counted

        public int Breath;             // BREATH, shared by the whole band
        public DecreeKind Decree;      // DECREE (THE DEAF CROWN) : what this bar forbids, None without it
        public int Floor;              // 1, 2 or 3
        public TurnStep Step;

        //Conductor (step 5) : whose PASSIVE and SIGNATURE this fight plays, and the SIGNATURE gauge
        public TurnKit Kit;
        public int Signature;
        private int perfectRun;        // PERFECTs in a row, for the result page's BEST STREAK
        private int prodigyRun;        // PERFECTs in a row toward the next PRODIGY breath
        public int BestRun;            // the longest run of PERFECTs this fight, for the result page
        public bool FelixVoice;        // TWO VOICES (FANNY MENDELSSOHN AND ???) : false while FANNY's voice leads, true for FELIX's
        public int CapriceTurns;       // CAPRICE (THE HEARTLESS) : turns left in which no swipe is a MISS
        public int Feather;            // FEATHER (THE SHADOWLESS) : free ATTACKs at the end of the band's part of this bar

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

        //Current : whoever is acting now (null while the bar is being written and once the fight is over)
        public Fighter Current
        {
            get
            {
                if (Step != TurnStep.PlayerTurn && Step != TurnStep.EnemyTurn) return null;
                return Order[OrderIndex];
            }
        }

        //Current Move : the move of the musician who is playing now (null on TACET's turns)
        public BarMove CurrentMove
        {
            get { return Step == TurnStep.PlayerTurn ? OrderMoves[OrderIndex] : null; }
        }

        //MAESTRO MODE : narrower timing windows, more shakes for a roll, a dearer CALL and harder notes.
        //Set after the fight is made, so the attacks already shown are planned again with the harder notes.
        private bool maestro;
        public bool Maestro
        {
            get { return maestro; }
            set
            {
                maestro = value;
                for (int i = 0; i < Enemies.Count; i++)
                    if (!Enemies[i].Down && Enemies[i].Planned) PlanIntent(Enemies[i]);
            }
        }

        //Battle Start : startHp is each musician's life carried in from the run, or null for full life.
        //onField : the first this many start on the field (ONE BAR, Archive.StartBand), the rest wait on the BENCH.
        public TurnBattle(List<Musician> band, List<Enemy> enemies, int floor, Random random, int[] startHp = null, int lifeBonus = 0, int onField = 99)
        {
            this.random = random;
            this.lifeBonus = lifeBonus;
            Floor = Math.Max(1, Math.Min(3, floor));
            Breath = BattleRules.TurnBreathStart;

            for (int i = 0; i < band.Count; i++)
            {
                int hp = (startHp != null && i < startHp.Length) ? startHp[i] : -1;
                if (Party.Count < Math.Min(onField, BattleRules.TurnFieldMost)) Party.Add(MakeMusician(band[i], hp));
                else
                {
                    Bench.Add(band[i]);
                    BenchLife.Add(hp);
                }
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

        //Make Musician : one musician walks onto the field with this much life (-1 or less is full)
        private Fighter MakeMusician(Musician m, int hp)
        {
            Fighter f = new Fighter();
            f.Name = m.Name;
            f.Musician = m;
            f.Slot = Party.Count;
            f.MaxHp = BattleRules.TurnHp[(int)m.Family] + lifeBonus;              // lifeBonus : the ARCHIVE's STRONGER BAND
            f.Hp = hp >= 0 ? Math.Min(hp, f.MaxHp) : f.MaxHp;
            f.Speed = BattleRules.TurnSpeed[(int)m.Family];
            if (Has(MotifId.Crescendo)) f.Crescendo = BattleRules.TurnCrescendoStart;   // CRESCENDO (the motif) : the ones called later too
            return f;
        }

        //======================================================================================
        //Turn Order
        //======================================================================================

        //Round Start : a new bar. The plan is empty, the ones called last bar may play now, and every enemy
        //decides its move so the player can write the bar against it.
        private void StartRound()
        {
            if (Round > 0) GreenRoom();
            Round++;
            Order.Clear();
            OrderMoves.Clear();
            OrderIndex = 0;
            Bar.Clear();
            CalledThisBar = false;
            for (int i = 0; i < Party.Count; i++) Party[i].JustCalled = false;

            //Intent : decided once at the start, then again right after each of its attacks (FinishEnemyTurn),
            //so what shows over its head is always the attack still to come (step 10, like Slay the Spire).
            //A musician it aimed at who fell later in the bar is swapped now, so the plan never shows a fallen target (step 13).
            for (int i = 0; i < Enemies.Count; i++)
            {
                Fighter e = Enemies[i];
                if (e.Down) continue;
                if (!e.Planned) PlanIntent(e);
                else if (e.IntentTarget != null && e.IntentTarget.Down) e.IntentTarget = PickTarget(e);
            }

            //DECREE (THE DEAF CROWN) : SKILLS and ATTACKS are forbidden in turn, one bar each
            Decree = DecreeKind.None;
            if (AnyStanding(EnemyTrait.Decree)) Decree = Round % 2 == 1 ? DecreeKind.NoSkills : DecreeKind.NoAttacks;

            FocusTarget();
            Step = TurnStep.Plan;
            AimFirstInLine();
        }

        //GREEN ROOM (motif, step 13) : at the end of every bar the musicians on the BENCH get some life back
        private void GreenRoom()
        {
            if (!Has(MotifId.GreenRoom)) return;
            for (int i = 0; i < Bench.Count; i++)
            {
                if (BenchLife[i] < 0) continue;                                              // -1 : already at full life
                int max = BattleRules.TurnHp[(int)Bench[i].Family] + lifeBonus;
                BenchLife[i] = Math.Min(max, BenchLife[i] + BattleRules.TurnGreenRoomHeal);
            }
        }

        //======================================================================================
        //ONE BAR : writing the bar (step 12)
        //======================================================================================

        //Can Plan : a musician on the field who is standing and did not walk in this bar
        public bool CanPlan(Fighter who)
        {
            return Step == TurnStep.Plan && who != null && !who.IsEnemy && !who.Down && !who.JustCalled;
        }

        //Move Of : the move written for this musician, null when there is none yet
        public BarMove MoveOf(Fighter who)
        {
            for (int i = 0; i < Bar.Count; i++)
                if (Bar[i].Who == who) return Bar[i];
            return null;
        }

        //Reserved : the BREATH the skills already written will spend. Breath Free is what is left to write more.
        public int Reserved
        {
            get
            {
                int cost = 0;
                for (int i = 0; i < Bar.Count; i++)
                    if (Bar[i].Skill != null) cost += Bar[i].Skill.Cost;
                return cost;
            }
        }

        public int BreathFree
        {
            get { return Breath - Reserved; }
        }

        //Can Plan Skill : enough free BREATH and not under MUTE. A skill already written for this musician gives its BREATH back first.
        //BORROWED BREATH (THE HEARTLESS) : short of BREATH, it can still be written if the life it would cost is there.
        public bool CanPlanSkill(Fighter who, Skill s)
        {
            if (!CanPlan(who) || s == null || who.Mute > 0) return false;
            BarMove old = MoveOf(who);
            int free = BreathFree + (old != null && old.Skill != null ? old.Skill.Cost : 0);
            if (free >= s.Cost) return true;
            return Kit == TurnKit.Paganini && (s.Cost - Math.Max(0, free)) * BattleRules.TurnPaganiniLife < who.Hp;
        }

        //Plan Attack : this musician ATTACKs that enemy. A musician written again keeps their place in the bar.
        public bool PlanAttack(Fighter who, Fighter target)
        {
            if (!CanPlan(who) || !CanTarget(target)) return false;
            Write(who, null, target);
            Focus = target;
            return true;
        }

        //Plan Skill : this musician plays the skill at the target (ignored for skills that pick their own)
        public bool PlanSkill(Fighter who, Skill s, Fighter target)
        {
            if (!CanPlanSkill(who, s)) return false;
            if (s.Target == SkillTarget.Enemy && !CanTarget(target)) return false;
            if (s.Target == SkillTarget.Ally && (target == null || target.IsEnemy || target.Down)) return false;
            if (s.Target == SkillTarget.Self) target = who;
            if (s.Target == SkillTarget.AllEnemies || s.Target == SkillTarget.AllAllies) target = null;
            Write(who, s, target);
            if (s.Target == SkillTarget.Enemy) Focus = target;
            return true;
        }

        private void Write(Fighter who, Skill s, Fighter target)
        {
            BarMove move = MoveOf(who);
            if (move == null)
            {
                move = new BarMove();
                move.Who = who;
                Bar.Add(move);
            }
            move.Skill = s;
            move.Target = target;
            AimFirstInLine();
        }

        //Unplan : the musician's move is rubbed out (they ATTACK Focus if nothing else is written)
        public void Unplan(Fighter who)
        {
            BarMove move = MoveOf(who);
            if (move != null && Step == TurnStep.Plan) Bar.Remove(move);
            AimFirstInLine();
        }

        //Focus Target : the enemy unwritten moves go to, the first one standing when the last one fell
        public Fighter FocusTarget()
        {
            if (Focus != null && !Focus.Down) return Focus;
            Focus = null;
            for (int i = 0; i < Enemies.Count && Focus == null; i++)
                if (!Enemies[i].Down) Focus = Enemies[i];
            return Focus;
        }

        //CALL : one musician from the BENCH walks onto the field. One per bar, for BREATH, and they play from the next bar.
        //The first call of a fight costs TurnCallCost (MAESTRO more), every one after it TurnCallCostStep more.
        public int CallCost
        {
            get
            {
                int cost = (Maestro ? BattleRules.MaestroTurnCallCost : BattleRules.TurnCallCost) + (Calls > 0 ? BattleRules.TurnCallCostStep : 0);
                if (AnyStanding(EnemyTrait.FeltHand)) cost += BattleRules.TurnFeltHandCall;   // FELT HAND (THE DAMPER)
                return cost;
            }
        }

        public bool CanCall(int bench)
        {
            return Step == TurnStep.Plan && !CalledThisBar && bench >= 0 && bench < Bench.Count
                && Party.Count < BattleRules.TurnFieldMost && BreathFree >= CallCost;
        }

        public Fighter Call(int bench)
        {
            if (!CanCall(bench)) return null;
            Breath -= CallCost;
            Calls++;
            CalledThisBar = true;

            Fighter f = MakeMusician(Bench[bench], BenchLife[bench]);
            f.JustCalled = true;
            f.CalledBar = Round;                                                             // ENTRANCE
            Party.Add(f);
            Bench.RemoveAt(bench);
            BenchLife.RemoveAt(bench);
            Events.Add(new TurnEvent(TurnEventKind.Called, f, 0));

            //ON CUE (THE CUE) : it turns to the one who just walked in, so the screen shows it at once
            LastCalled = f;
            if (PickTaunt() == null)
                for (int i = 0; i < Enemies.Count; i++)
                    if (Cued(Enemies[i]) && !Enemies[i].IntentRest) Enemies[i].IntentTarget = f;
            return f;
        }

        //Play Bar : the bar is written. Every musician still without a move ATTACKs Focus, in field order,
        //then the bar plays : the band in the order written, TACET after it.
        public void PlayBar()
        {
            if (Step != TurnStep.Plan) return;
            for (int i = 0; i < Party.Count; i++)
                if (CanPlan(Party[i]) && MoveOf(Party[i]) == null) Write(Party[i], null, FocusTarget());

            MarkUnison();
            BuildOrder(Bar, Order, OrderMoves);
            Feather = 0;
            OrderIndex = 0;
            bandMovesDone = 0;
            BeginTurn();
        }

        //UNISON (motif, step 13) : when every move of the bar is an ATTACK at the same enemy, the last one hits harder
        private void MarkUnison()
        {
            if (!Has(MotifId.Unison) || Bar.Count < 2) return;
            for (int i = 0; i < Bar.Count; i++)
                if (Bar[i].Skill != null || Bar[i].Target != Bar[0].Target) return;
            Bar[Bar.Count - 1].Unison = true;
        }

        //Preview Order : who will play in what order if the bar were played now, for the screen's bar strip.
        //Musicians without a move are shown ATTACKing Focus, nothing is changed.
        public void PreviewOrder(List<Fighter> order, List<BarMove> moves)
        {
            List<BarMove> bar = new List<BarMove>(Bar);
            for (int i = 0; i < Party.Count; i++)
            {
                if (!CanPlan(Party[i]) || MoveOf(Party[i]) != null) continue;
                BarMove attack = new BarMove();
                attack.Who = Party[i];
                attack.Target = FocusTarget();
                bar.Add(attack);
            }
            BuildOrder(bar, order, moves);
        }

        //Build Order : the band's moves in the order written (FEATHER adds free ATTACKs after them), then TACET.
        //CUT IN : an enemy of TurnCutInSpeed or faster plays right after the band's TurnCutInAfter-th move,
        //the others after the band, fastest first. Equal speeds keep their order (an insertion sort,
        //List.Sort does not promise that).
        private void BuildOrder(List<BarMove> bar, List<Fighter> order, List<BarMove> moves)
        {
            order.Clear();
            moves.Clear();

            List<Fighter> fast = new List<Fighter>();
            List<Fighter> slow = new List<Fighter>();
            List<Fighter> twice = new List<Fighter>();        // TWICE A BAR (STUTTER)
            for (int i = 0; i < Enemies.Count; i++)
            {
                Fighter e = Enemies[i];
                if (e.Down) continue;
                if (Has(e, EnemyTrait.TwiceABar))
                {
                    twice.Add(e);
                    continue;
                }
                List<Fighter> list = e.Speed >= BattleRules.TurnCutInSpeed ? fast : slow;
                int at = list.Count;
                while (at > 0 && list[at - 1].Speed < e.Speed) at--;
                list.Insert(at, e);
            }

            //FEATHER : free ATTACKs by the band in field order, round again when there are more than musicians
            List<BarMove> band = new List<BarMove>(bar);
            List<Fighter> players = new List<Fighter>();
            for (int i = 0; i < Party.Count; i++)
                if (!Party[i].Down && !Party[i].JustCalled) players.Add(Party[i]);
            for (int k = 0; k < Feather && players.Count > 0; k++)
            {
                Fighter f = players[k % players.Count];
                BarMove extra = new BarMove();
                extra.Who = f;
                extra.Free = true;
                BarMove own = MoveOf(f);
                extra.Target = own != null && own.Skill == null ? own.Target : FocusTarget();
                band.Add(extra);
            }

            for (int i = 0; i < band.Count; i++)
            {
                order.Add(band[i].Who);
                moves.Add(band[i]);
                if (i + 1 == BattleRules.TurnTwiceFirst) AddEnemies(twice, order, moves);       // TWICE A BAR, the first time
                if (i + 1 == BattleRules.TurnCutInAfter) AddEnemies(fast, order, moves);       // CUT IN
                if (i + 1 == BattleRules.TurnTwiceSecond) AddEnemies(twice, order, moves);     // TWICE A BAR, again
            }
            if (band.Count < BattleRules.TurnTwiceFirst) AddEnemies(twice, order, moves);
            if (band.Count < BattleRules.TurnCutInAfter) AddEnemies(fast, order, moves);
            if (band.Count < BattleRules.TurnTwiceSecond) AddEnemies(twice, order, moves);
            AddEnemies(slow, order, moves);
        }

        private static void AddEnemies(List<Fighter> enemies, List<Fighter> order, List<BarMove> moves)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                order.Add(enemies[i]);
                moves.Add(null);
            }
        }

        //Cuts In : this enemy plays in the middle of the bar (CUT IN)
        public static bool CutsIn(Fighter enemy)
        {
            return enemy.IsEnemy && enemy.Speed >= BattleRules.TurnCutInSpeed;
        }

        //Enemy Intent (step 8) : the next of its PHRASES, fitted to the floor's length, at a musician.
        //CADENZA : every third attack of a boss is its big move, one note more and each note harder.
        //Its trait may change the plan, see the trait names below (from TraitFloor on).
        private void PlanIntent(Fighter enemy)
        {
            Enemy e = enemy.Enemy;
            bool trait = TraitOn(enemy);
            enemy.Planned = true;
            enemy.IntentTarget = PickTarget(enemy);
            enemy.IntentRest = false;
            enemy.IntentFast = false;
            enemy.IntentCadenza = false;
            enemy.IntentAll = false;

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

            //ONE BAR : ordinary enemies come in packs, so their attacks are shorter
            bool small = e.Kind == EnemyKind.Normal;
            int fewest = small ? BattleRules.TurnSmallNotesMin[Floor - 1] : BattleRules.TurnNotesMin[Floor - 1];
            int most = small ? BattleRules.TurnSmallNotesMax[Floor - 1] : BattleRules.TurnNotesMax[Floor - 1];
            int count = Math.Max(fewest, Math.Min(most, ways.Count));
            if (trait && e.Trait == EnemyTrait.NoRest) count++;              // NO REST (WHITE NOISE)
            if (trait && e.Trait == EnemyTrait.TwiceABar) count = 1;         // TWICE A BAR (STUTTER) : one note each time
            int power = e.TurnPower > 0 ? e.TurnPower : e.SongNotePower();                  // SongNotePower = the average of its Pattern notes
            float maestroHit = Maestro ? BattleRules.MaestroTurnDamage : 1f;                 // MAESTRO
            int hit = Math.Max(1, (int)Math.Round(power * BattleRules.TurnNoteDamage * BattleRules.TurnFloorDamage[Floor - 1] * maestroHit));

            //THE COURT (THE STAFF) : its big move is one heavy note down at the floor that hits everybody
            if (trait && e.Trait == EnemyTrait.TheCourt && (enemy.Attacks + 1) % BattleRules.TurnStaffEvery == 0)
            {
                enemy.IntentAll = true;
                count = 1;
                ways = new List<Flick> { Flick.Down };
                sizes = new List<int> { 2 };
                hit = (int)Math.Round(hit * BattleRules.TurnStaffPower);
            }

            enemy.IntentCadenza = e.Kind == EnemyKind.Boss && !enemy.IntentAll && (enemy.Attacks + 1) % BattleRules.TurnCadenzaEvery == 0;
            if (enemy.IntentCadenza)
            {
                count++;
                hit = (int)Math.Round(hit * BattleRules.TurnCadenzaPower);
            }

            //BY TOUCH (THE BLIND CANTOR) : the notes that got through last time come back after its own
            int back = trait && e.Trait == EnemyTrait.ByTouch ? Math.Min(BattleRules.TurnByTouchMost, enemy.MissedWays.Count) : 0;

            enemy.IntentNotes = new int[count + back];
            enemy.IntentWays = new Flick[count + back];
            enemy.IntentSizes = new int[count + back];
            enemy.IntentShown = new int[count + back];
            for (int i = 0; i < count + back; i++)
            {
                enemy.IntentNotes[i] = hit;
                enemy.IntentWays[i] = i < count ? ways[i % ways.Count] : enemy.MissedWays[i - count];
                enemy.IntentSizes[i] = i < count ? sizes[i % sizes.Count] : enemy.MissedSizes[i - count];
                enemy.IntentShown[i] = enemy.IntentSizes[i];
            }
            enemy.MissedWays.Clear();
            enemy.MissedSizes.Clear();
            count += back;

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

        //Pick Target : a musician with TAUNT up if there is one, else anyone standing.
        //ON CUE and FIRST IN LINE (step 13) pick their own musician when there is one to pick.
        private Fighter PickTarget(Fighter enemy)
        {
            for (int i = 0; i < Party.Count; i++)
                if (Party[i].Taunt && !Party[i].Down) return Party[i];

            if (Cued(enemy) && LastCalled != null && !LastCalled.Down) return LastCalled;          // ON CUE
            if (enemy != null && Has(enemy, EnemyTrait.ByTouch))                                   // BY TOUCH : the seats in turn, front to back
            {
                List<Fighter> seats = new List<Fighter>();
                for (int i = 0; i < Party.Count; i++)
                    if (!Party[i].Down) seats.Add(Party[i]);
                if (seats.Count > 0) return seats[enemy.Attacks % seats.Count];
            }
            if (enemy != null && Has(enemy, EnemyTrait.FirstInLine))                               // FIRST IN LINE
            {
                Fighter first = FirstInLine();
                if (first != null) return first;
            }

            List<Fighter> standing = new List<Fighter>();
            for (int i = 0; i < Party.Count; i++)
                if (!Party[i].Down) standing.Add(Party[i]);

            if (standing.Count == 0) return null;
            return standing[random.Next(standing.Count)];
        }

        //Turn Start : tell the screen whose turn it is. GUARD and TAUNT end when their owner is up again.
        //OFF-BEAT : an enemy thrown off the beat loses this turn (its statuses still count down).
        //An enemy whose target fell, or who is now taunted, aims again, so the attack it showed still happens.
        //A musician whose skill cannot be played any more (MUTE from a CUT IN, BREATH taken) ATTACKs instead,
        //and a move whose target fell goes to another one.
        private void BeginTurn()
        {
            while (true)
            {
                if (OrderIndex >= Order.Count || Order[OrderIndex].Down)
                {
                    if (!Advance())
                    {
                        StartRound();
                        return;
                    }
                    continue;
                }
                Fighter f = Order[OrderIndex];
                bool free = OrderMoves[OrderIndex] != null && OrderMoves[OrderIndex].Free;   // FEATHER : not their turn
                if (!free) f.Guard = false;
                if (f.Taunt && !free)
                {
                    //TAUNT ends : an enemy that planned its next attack while taunted aims again
                    f.Taunt = false;
                    for (int i = 0; i < Enemies.Count; i++)
                        if (!Enemies[i].Down && Enemies[i].IntentTarget == f) Enemies[i].IntentTarget = PickTarget(Enemies[i]);
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
                    else if (f.IntentTarget == null || f.IntentTarget.Down) f.IntentTarget = PickTarget(f);
                    parried = 0;
                    Step = TurnStep.EnemyTurn;
                }
                else
                {
                    Step = TurnStep.PlayerTurn;
                    BarMove move = OrderMoves[OrderIndex];
                    if (move.Skill != null && !CanUse(move.Skill))
                    {
                        move.Skill = null;
                        move.Forced = true;                                                  // not the player's choice, DECREE lets it pass
                    }
                    Retarget(move);
                }
                return;
            }
        }

        //Retarget : a move whose target fell goes to Focus (an enemy) or to the musician with the least life
        private void Retarget(BarMove move)
        {
            bool atEnemy = move.Skill == null || move.Skill.Target == SkillTarget.Enemy;
            if (atEnemy && !CanTarget(move.Target)) move.Target = FocusTarget();
            if (move.Skill != null && move.Skill.Target == SkillTarget.Ally && (move.Target == null || move.Target.Down))
            {
                move.Target = null;
                for (int i = 0; i < Party.Count; i++)
                    if (!Party[i].Down && (move.Target == null || Party[i].Hp < move.Target.Hp)) move.Target = Party[i];
            }
        }

        private Fighter PickTaunt()
        {
            for (int i = 0; i < Party.Count; i++)
                if (Party[i].Taunt && !Party[i].Down) return Party[i];
            return null;
        }

        //Has : the enemy's trait is this one and it is switched on (TraitOn)
        private bool Has(Fighter enemy, EnemyTrait trait)
        {
            return enemy != null && TraitOn(enemy) && enemy.Enemy.Trait == trait;
        }

        //Any Standing : an enemy still standing has this trait switched on
        private bool AnyStanding(EnemyTrait trait)
        {
            for (int i = 0; i < Enemies.Count; i++)
                if (!Enemies[i].Down && Has(Enemies[i], trait)) return true;
            return false;
        }

        private bool Cued(Fighter enemy)
        {
            return enemy != null && !enemy.Down && Has(enemy, EnemyTrait.OnCue);
        }

        //FIRST IN LINE (THE ROUND, step 13) : the musician who plays first in this bar. While the bar is written
        //that is the first move written, or the first one on the field who will ATTACK without a move.
        private Fighter FirstInLine()
        {
            for (int i = 0; i < Bar.Count; i++)
                if (!Bar[i].Who.Down) return Bar[i].Who;
            for (int i = 0; i < Party.Count; i++)
                if (!Party[i].Down && !Party[i].JustCalled) return Party[i];
            return null;
        }

        //Aim First In Line : the bar changed while it is written, so FIRST IN LINE turns to its new first musician
        private void AimFirstInLine()
        {
            if (Step != TurnStep.Plan || PickTaunt() != null) return;
            Fighter first = FirstInLine();
            if (first == null) return;
            for (int i = 0; i < Enemies.Count; i++)
                if (!Enemies[i].Down && !Enemies[i].IntentRest && Has(Enemies[i], EnemyTrait.FirstInLine)) Enemies[i].IntentTarget = first;
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
            bool free = OrderMoves[OrderIndex] != null && OrderMoves[OrderIndex].Free;      // FEATHER : statuses wait for the real turn
            if (!free) EndOfTurn(done);
            if (!done.IsEnemy && !free) bandMovesDone++;                                     // DOWNBEAT
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

        //Player Attack : ONE BAR (step 12), the musician whose turn it is hits one enemy. No swipe : it always
        //lands at TurnAttackScale and adds TurnAttackOffBeat. A target that fell is swapped for Focus.
        //Returns the damage done.
        public int Attack(Fighter target)
        {
            if (Step != TurnStep.PlayerTurn) return 0;
            if (!CanTarget(target)) target = FocusTarget();
            if (target == null) return 0;

            Fighter attacker = Current;
            MusicianTrait trait = attacker.Musician.Trait;
            float scale = BattleRules.TurnAttackScale;

            //FOUR BARS STRAIGHT (LUKA) : every fourth ATTACK hits twice as hard
            attacker.Swings++;
            if (trait == MusicianTrait.FourBars && attacker.Swings % BattleRules.TurnFourBarsEvery == 0) scale *= BattleRules.TurnFourBarsScale;

            scale *= MoveScale(attacker, CurrentMove);                                              // DOWNBEAT, ENTRANCE, UNISON
            int damage = HitDamage(attacker, target, scale);
            Strike(target, damage);
            AddBreath(BattleRules.TurnBreathPerAttack);

            //HELD NOTE (CHAI) : his ATTACK rings on, a second hit for a share of the first
            if (trait == MusicianTrait.HeldNote && !target.Down)
                Strike(target, Math.Max(1, (int)Math.Round(damage * BattleRules.TurnHeldNoteShare)));

            //FUGUE (THE METRONOME) : an echo waiting on this musician plays the ATTACK once more at its strength (step 12)
            if (attacker.Echo > 0f && !target.Down)
            {
                float echo = attacker.Echo;
                attacker.Echo = 0f;
                Events.Add(new TurnEvent(TurnEventKind.Echo, attacker, 0));
                Strike(target, Math.Max(1, (int)Math.Round(damage * echo)));
            }

            CountHit(attacker, true);                                                                // MOMENTUM
            AddOffBeat(target, BattleRules.TurnAttackOffBeat * (Has(MotifId.Marcato) ? BattleRules.TurnMarcatoOffBeat : 1));   // OFF-BEAT, MARCATO
            LastWord(attacker);                                                                      // LAST WORD
            BreaksDecree(attacker, DecreeKind.NoAttacks);                                            // DECREE

            NextTurn();
            return damage;
        }

        //Attack : the move written for the musician who is up
        public int Attack()
        {
            BarMove move = CurrentMove;
            return move == null ? 0 : Attack(move.Target);
        }

        //Use Skill : the move written for the musician who is up, with the notes swiped
        public bool UseSkill(NoteResult[] results)
        {
            BarMove move = CurrentMove;
            return move != null && move.Skill != null && UseSkill(move.Skill, move.Target, results);
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
            float moveScale = MoveScale(user, CurrentMove);                                        // DOWNBEAT, ENTRANCE

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
                    //RUBATO : a missed note still plays, at TurnRubatoMiss of a GOOD one and with no BREATH
                    if (Has(MotifId.Rubato)) PlayNote(s, s.Notes[n], new NoteResult(Grade.Good, true, r.Hold), targets, user, BattleRules.TurnRubatoMiss * moveScale, false);
                    continue;
                }
                anyLanded = true;
                PlayNote(s, s.Notes[n], r, targets, user, moveScale, true);
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
            LastWord(user);                                                                 // LAST WORD
            BreaksDecree(user, DecreeKind.NoSkills);                                        // DECREE

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
                        if (s.Damage > 0f) Strike(f, HitDamage(user, f, scale * s.Damage * tutti));
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

            //BY TOUCH (THE BLIND CANTOR) : a note that gets through whole comes back in its next attack
            if (Has(enemy, EnemyTrait.ByTouch) && g != Grade.Good && enemy.MissedWays.Count < BattleRules.TurnByTouchMost)
            {
                enemy.MissedWays.Add(enemy.IntentWays[note]);
                enemy.MissedSizes.Add(enemy.IntentSizes[note]);
            }

            //THE COURT (THE STAFF) : the floor is struck, every musician standing takes the note
            if (!enemy.IntentAll) return NoteHits(enemy, target, note, g);
            int total = 0;
            for (int i = 0; i < Party.Count; i++)
                if (!Party[i].Down) total += NoteHits(enemy, Party[i], note, g);
            return total;
        }

        //Note Hits : one note that was not parried PERFECT lands on one musician
        private int NoteHits(Fighter enemy, Fighter target, int note, Grade g)
        {
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

            bool deaf = Has(enemy, EnemyTrait.Decree);                                                       // DECREE : it never takes a COUNTER
            if (parried > 0 && parried == enemy.IntentNotes.Length && target != null && !target.Down && !enemy.Down && !deaf)
            {
                float scale = BattleRules.TurnCounterScale * (enemy.IntentCadenza ? BattleRules.TurnCadenzaCounter : 1f);
                if (Has(MotifId.Counterpoint)) scale *= BattleRules.TurnCounterpointScale;                     // COUNTERPOINT
                int damage = HitDamage(target, enemy, scale);
                Events.Add(new TurnEvent(TurnEventKind.Counter, target, damage));
                Strike(enemy, damage);
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

        //Can Signature : the gauge is full and the bar is being written (ONE BAR : it is played before the bar)
        public bool CanSignature
        {
            get { return Step == TurnStep.Plan && Kit != TurnKit.None && Signature >= SignatureNeed; }
        }

        //Signature Need : PERFECTs to fill the gauge (CON BRIO needs fewer)
        public int SignatureNeed
        {
            get
            {
                if (BattleRules.PlaytestSignatureNeed > 0) return BattleRules.PlaytestSignatureNeed;                // PLAYTEST
                return Has(MotifId.ConBrio) ? BattleRules.TurnConBrioNeed : BattleRules.TurnSignatureNeed;          // CON BRIO
            }
        }

        //Signature Shape : the 4/4 pattern a conductor beats, down, left, right, up (any size)
        public static readonly Flick[] SignatureShape = { Flick.Down, Flick.Left, Flick.Right, Flick.Up };

        //Signature : the conductor's big move. It is played while the bar is written and costs
        //no move. strokes holds how the four strokes went. Its strength is their average swipe scale :
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
                    Strike(e, Math.Max(1, (int)Math.Round(BattleRules.TurnBoltDamage * strength * BeethovenScale())));
                    if (!e.Down) AddOffBeat(e, (int)Math.Round(BattleRules.TurnOffBeatMax * strength));
                }
            }

            //FEATHER (THE SHADOWLESS) : TurnFeatherPerStroke free ATTACKs per stroke that lands, by the band in field
            //order and round again, at the end of the band's part of this bar (BuildOrder)
            if (Kit == TurnKit.Mozart)
            {
                Feather = landed * BattleRules.TurnFeatherPerStroke;
                Events.Add(new TurnEvent(TurnEventKind.Extra, null, Feather));
            }

            //FUGUE (THE METRONOME) : every musician standing gets an echo, their next move (an ATTACK or a skill) plays again at this strength
            if (Kit == TurnKit.Bach)
            {
                for (int i = 0; i < Party.Count; i++)
                {
                    if (Party[i].Down) continue;
                    Party[i].Echo = strength * BattleRules.TurnFugueScale;
                    Events.Add(new TurnEvent(TurnEventKind.Echo, Party[i], 0));
                }
            }

            //CAPRICE (THE HEARTLESS) : for TurnCapricePerStroke turns per stroke that lands every swipe goes one better (MISS
            //to GOOD, GOOD to PERFECT), the enemies' notes included. When it runs out every musician pays (PayCaprice).
            if (Kit == TurnKit.Paganini && landed > 0) CapriceTurns = landed * BattleRules.TurnCapricePerStroke;

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

            //TWO VOICES (FANNY MENDELSSOHN AND ???) : FANNY strikes every enemy while FELIX heals every musician
            if (Kit == TurnKit.Fanny)
            {
                for (int i = 0; i < Enemies.Count; i++)
                    if (!Enemies[i].Down) Strike(Enemies[i], Math.Max(1, (int)Math.Round(BattleRules.TurnDuetDamage * strength)));
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

        //Scale : SwipeScale (RUBATO is played in UseSkill since step 12, ATTACK has no swipe to miss)
        public float Scale(Grade grade, bool rightSize)
        {
            return SwipeScale(grade, rightSize);
        }

        //======================================================================================
        //Life, Breath and Statuses
        //======================================================================================

        //Strike : the band's damage to an enemy (an ATTACK, a skill, a COUNTER, a SIGNATURE).
        //HOLDS THE ROOM (ROOM TONE) takes some off while another enemy has it, SEGUE (motif) carries what is
        //left over a SILENCE on to the next enemy standing. DISSONANCE is not a strike, it uses Hurt.
        private void Strike(Fighter enemy, int amount)
        {
            if (amount <= 0 || enemy.Down) return;
            if (HeldUp(enemy)) amount = Math.Max(1, (int)Math.Round(amount * BattleRules.TurnRoomToneKeep));   // HOLDS THE ROOM
            if (Has(enemy, EnemyTrait.TheCourt) && OthersStanding(enemy))                                     // THE COURT
                amount = Math.Max(1, (int)Math.Round(amount * BattleRules.TurnCourtKeep));
            int over = amount - enemy.Hp;
            Hurt(enemy, amount);
            if (over <= 0 || !Has(MotifId.Segue)) return;

            //SEGUE : the next enemy standing after this one in the line, round to the start
            for (int k = 1; k < Enemies.Count; k++)
            {
                Fighter next = Enemies[(enemy.Slot + k) % Enemies.Count];
                if (next.Down) continue;
                Strike(next, over);
                return;
            }
        }

        //Others Standing : another enemy is still up (THE COURT)
        private bool OthersStanding(Fighter enemy)
        {
            for (int i = 0; i < Enemies.Count; i++)
                if (Enemies[i] != enemy && !Enemies[i].Down) return true;
            return false;
        }

        //Breaks Decree : DECREE (THE DEAF CROWN), a move of a kind this bar forbids hurts the musician who played it.
        //FEATHER's free ATTACKs and a skill turned into an ATTACK by the rules do not count.
        private void BreaksDecree(Fighter who, DecreeKind kind)
        {
            BarMove move = CurrentMove;
            if (Decree != kind || move == null || move.Free || move.Forced || who.Down) return;
            if (!AnyStanding(EnemyTrait.Decree)) return;
            int thorns = Math.Max(1, (int)Math.Round(BattleRules.TurnThorns * BattleRules.TurnFloorDamage[Floor - 1]));
            Events.Add(new TurnEvent(TurnEventKind.Thorns, who, thorns));
            Hurt(who, thorns);
        }

        //Held Up : HOLDS THE ROOM, another enemy standing has it (two ROOM TONEs hold each other up)
        private bool HeldUp(Fighter enemy)
        {
            for (int i = 0; i < Enemies.Count; i++)
                if (Enemies[i] != enemy && !Enemies[i].Down && Has(Enemies[i], EnemyTrait.HoldsTheRoom)) return true;
            return false;
        }

        //Move Scale : how much stronger this musician's move is (1 for most). DOWNBEAT : the first move of the bar.
        //ENTRANCE : a musician called in last bar. UNISON : the last ATTACK of a bar that all went at one enemy.
        private float MoveScale(Fighter who, BarMove move)
        {
            float scale = 1f;
            bool free = move != null && move.Free;
            if (Has(MotifId.Downbeat) && !free && bandMovesDone == 0) scale *= BattleRules.TurnDownbeatScale;
            if (Has(MotifId.Entrance) && who.CalledBar > 0 && Round == who.CalledBar + 1) scale *= BattleRules.TurnEntranceScale;
            if (move != null && move.Unison) scale *= BattleRules.TurnUnisonScale;
            return scale;
        }

        //LAST WORD (motif, step 13) : the musician who plays the band's last move of the bar stands GUARD
        //until their next turn (FEATHER's free ATTACKs after it do not count)
        private void LastWord(Fighter who)
        {
            if (!Has(MotifId.LastWord) || CurrentMove == null || CurrentMove.Free) return;
            for (int j = OrderIndex + 1; j < Order.Count; j++)
                if (OrderMoves[j] != null && !OrderMoves[j].Free && !Order[j].Down) return;
            who.Guard = true;
        }

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
