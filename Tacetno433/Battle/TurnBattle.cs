using System;
using System.Collections.Generic;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Battle
{
    //TurnStep : what the turn battle is waiting for (Alpha 7)
    //   PlayerTurn   a musician is up : the screen asks for a target and the attack swipe
    //   EnemyTurn    an enemy is up : the screen shows its notes and asks for the parry swipes
    //   Won / Lost   the fight is over
    public enum TurnStep { PlayerTurn, EnemyTurn, Won, Lost }

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

        public bool Down { get { return Hp <= 0; } }
    }

    //TurnBattle : THE RULES OF THE TURN BASED FIGHT (Alpha 7). No drawing code lives here.
    //A screen runs it like this :
    //   PlayerTurn : the player picks an enemy and swipes, the screen calls Attack(target, grade, rightSize)
    //   EnemyTurn  : the screen shows Current.IntentNotes flying at Current.IntentTarget, the player
    //                swipes back at each one, the screen calls ParryNote for each note as it lands
    //                and FinishEnemyTurn after the last (TakeEnemyAttack does all of it at once)
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
        public bool Maestro;           // MAESTRO MODE : narrower timing windows
        public TurnStep Step;

        private Random random;

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

        //Enemy Intent : pick a musician to hit and how many notes, from the floor's range
        private void PlanIntent(Fighter enemy)
        {
            enemy.IntentTarget = RandomStanding();

            int count = random.Next(BattleRules.TurnNotesMin[Floor - 1], BattleRules.TurnNotesMax[Floor - 1] + 1);
            int hit = Math.Max(1, (int)Math.Round(enemy.Enemy.SongNotePower() * BattleRules.TurnNoteDamage));   // SongNotePower = the average of its Pattern notes

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

        private Fighter RandomStanding()
        {
            List<Fighter> standing = new List<Fighter>();
            for (int i = 0; i < Party.Count; i++)
                if (!Party[i].Down) standing.Add(Party[i]);

            if (standing.Count == 0) return null;
            return standing[random.Next(standing.Count)];
        }

        //Turn Start : tell the screen whose turn it is. An enemy whose target fell before its
        //turn came picks another musician, so the attack it showed still happens.
        private void BeginTurn()
        {
            Fighter f = Order[OrderIndex];
            if (f.IsEnemy)
            {
                if (f.IntentTarget == null || f.IntentTarget.Down) f.IntentTarget = RandomStanding();
                Step = TurnStep.EnemyTurn;
            }
            else
            {
                Step = TurnStep.PlayerTurn;
            }
        }

        //Can Target : the player may only attack an enemy that is still standing
        public bool CanTarget(Fighter f)
        {
            return f != null && f.IsEnemy && !f.Down;
        }

        //Player Attack : the musician whose turn it is hits one enemy.
        //grade is how the swipe landed on the ring (a swipe the wrong way counts as Miss), and
        //rightSize is false when it was on time but the wrong size. Returns the damage done.
        public int Attack(Fighter target, Grade grade, bool rightSize)
        {
            if (Step != TurnStep.PlayerTurn || !CanTarget(target)) return 0;

            Fighter attacker = Current;
            int power = attacker.Musician.Power + attacker.Musician.Rehearsed;
            int damage = Math.Max(1, (int)Math.Round(power * BattleRules.TurnAttackPerPower * SwipeScale(grade, rightSize)));

            target.Hp = Math.Max(0, target.Hp - damage);
            AddBreath(BattleRules.TurnBreathPerAttack);

            NextTurn();
            return damage;
        }

        //Swipe Scale : how much of the hit lands, from the table in ALPHA7_DESIGN 5.3
        public static float SwipeScale(Grade grade, bool rightSize)
        {
            if (grade != Grade.Perfect && grade != Grade.Good) return BattleRules.TurnMiss;
            if (!rightSize) return BattleRules.TurnWrongSize;
            return grade == Grade.Perfect ? BattleRules.TurnPerfect : BattleRules.TurnGood;
        }

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
            if (note < 0 || note >= enemy.IntentNotes.Length) return 0;

            //PARRY : a PERFECT swipe blocks the note and gives breath back
            if (g == Grade.Perfect) AddBreath(BattleRules.TurnBreathPerParry);

            int damage = NoteDamage(enemy.IntentNotes[note], g);
            Fighter target = enemy.IntentTarget;
            if (target != null) target.Hp = Math.Max(0, target.Hp - damage);
            return damage;
        }

        //Note Damage : what gets through a parry. PERFECT nothing, GOOD a share, anything else all of it.
        public static int NoteDamage(int note, Grade g)
        {
            if (g == Grade.Perfect) return 0;
            if (g == Grade.Good) return (int)Math.Ceiling(note * BattleRules.TurnParryGoodKeep);
            return note;
        }

        //Enemy Turn End : after its last note, on to whoever is next
        public void FinishEnemyTurn()
        {
            if (Step != TurnStep.EnemyTurn) return;
            NextTurn();
        }

        //Judge : how a swipe landed. offset is the seconds between the swipe and the moment the
        //ring meets the note (minus is early). The wrong way is always a MISS.
        public Grade Judge(float offset, bool rightWay)
        {
            float perfect = Maestro ? BattleRules.MaestroPerfectWindow : BattleRules.PerfectWindow;   // MAESTRO
            float good = Maestro ? BattleRules.MaestroGoodWindow : BattleRules.GoodWindow;
            float gap = Math.Abs(offset);

            if (!rightWay || gap > good) return Grade.Miss;
            return gap <= perfect ? Grade.Perfect : Grade.Good;
        }

        //Late Limit : after this many seconds past the ring a note counts as not swiped at all
        public float LateLimit
        {
            get { return Maestro ? BattleRules.MaestroGoodWindow : BattleRules.GoodWindow; }
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

        private void AddBreath(int amount)
        {
            Breath = Math.Max(0, Math.Min(BattleRules.TurnBreathMax, Breath + amount));
        }

        //Next Turn : end the fight if one side is down, else move to the next one standing.
        //When everybody has had a turn a new round starts.
        private void NextTurn()
        {
            if (CheckEnd()) return;

            OrderIndex++;
            while (OrderIndex < Order.Count && Order[OrderIndex].Down) OrderIndex++;

            if (OrderIndex >= Order.Count) StartRound();
            else BeginTurn();
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

        //KNOCKED OUT : life a musician keeps after the fight. Anyone who fell gets up with TurnReviveHp.
        public int HpAfterBattle(Fighter f)
        {
            return f.Down ? BattleRules.TurnReviveHp : f.Hp;
        }
    }
}
