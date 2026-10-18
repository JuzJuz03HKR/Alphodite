using System;
using System.Collections.Generic;
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

        public bool Down { get { return Hp <= 0; } }
    }

    //TurnBattle : THE RULES OF THE TURN BASED FIGHT (Alpha 7). No drawing code lives here.
    //A screen runs it like this :
    //   PlayerTurn : the player picks an enemy and swipes, the screen calls Attack(target, grade, rightSize)
    //   EnemyTurn  : the screen shows Current.IntentNotes flying at Current.IntentTarget, the player
    //                swipes back at each one, and the screen calls TakeEnemyAttack(grades)
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
            for (int i = 0; i < count; i++) enemy.IntentNotes[i] = hit;
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

            Fighter enemy = Current;
            Fighter target = enemy.IntentTarget;
            int total = 0;

            for (int i = 0; i < enemy.IntentNotes.Length; i++)
            {
                Grade g = (grades != null && i < grades.Length) ? grades[i] : Grade.Hesitate;
                int note = enemy.IntentNotes[i];

                //PARRY
                if (g == Grade.Perfect)
                {
                    AddBreath(BattleRules.TurnBreathPerParry);
                    continue;
                }
                if (g == Grade.Good) note = (int)Math.Ceiling(note * BattleRules.TurnParryGoodKeep);
                total += note;
            }

            if (target != null) target.Hp = Math.Max(0, target.Hp - total);

            NextTurn();
            return total;
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
