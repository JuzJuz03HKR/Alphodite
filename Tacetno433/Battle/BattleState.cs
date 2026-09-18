using System;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Battle
{
    //Choice : what the band (or TACET) does on one beat. F / G / H on the keyboard.
    public enum Choice { Normal, Boost, Ease }

    //Grade : how well the QTE ring was hit
    public enum Grade { None, Perfect, Good, Miss, Hesitate }

    //BeatTag : the hint shown on the score page for each beat, before the fight
    public enum BeatTag { Rest, Covered, Accent, Gap, Free }

    //BeatResult : what happened on one beat, kept so the duel page can show it afterwards
    public class BeatResult
    {
        public bool Done;
        public int OurPower;
        public int EnemyPower;
        public int Push;
        public int StaminaChange;
        public Choice PlayerChoice;
        public Choice EnemyChoice;
        public Grade Grade;
        public bool OutOfBreath;

        //Extras : the duel page shows a word for each of these
        public int Combo;              // the combo after this beat
        public bool ComboBroken;       // a combo of 2 or more just ended
        public bool SecondWind;        // the SECOND WIND motif fired
        public bool Fired;             // THE INFERNO's bonus beat
    }

    //BattleState : ALL THE COMBAT RULES, with no drawing in it.
    //
    //How a fight works:
    //   a round is 8 beats. Before each round the player decides which seat plays on which beat.
    //   on every beat both sides add up their power, both pick Normal / Boost / Ease,
    //   and the difference pushes a line. Push it to +100 and you win on the spot, get pushed
    //   to -100 and you lose. After 3 rounds, whoever is ahead wins.
    //   every note costs stamina. Silent beats and Ease give some back. At zero stamina the
    //   band can only play as much as it can still pay for.
    //   PERFECT presses in a row build a COMBO that makes every beat stronger.
    //
    //Motifs and the conductor's perk bend these rules. Each place that checks one is
    //marked with the motif name, so searching for it finds every effect.
    public class BattleState
    {
        public RunState Run;
        public Enemy Enemy;

        //Battle Progress
        public int Round = 1;
        public float Line = 0f;                  // -100 we lose  ..  +100 we win
        public bool Finished;
        public bool PlayerWon;
        public int PerfectCount;

        //Combo
        public int Combo;                        // PERFECT presses in a row
        public int BestCombo;

        //Round Data : rebuilt at the start of every round
        public int[] EnemyPower = new int[BattleRules.BeatsPerRound];
        public bool[] EnemyHidden = new bool[BattleRules.BeatsPerRound];
        public Choice[] EnemyChoice = new Choice[BattleRules.BeatsPerRound];
        public BeatResult[] Results = new BeatResult[BattleRules.BeatsPerRound];

        //Prepared Text
        public string RoundLabel = "";
        public string EnemyTitle = "";

        private Random random;
        private float scale;                     // floor and kind scaling for the enemy
        private bool secondWindUsed;
        private bool fireNext;                   // THE INFERNO : the next played beat is stronger

        public BattleState(RunState run, Enemy enemy, Random random)
        {
            Run = run;
            Enemy = enemy;
            this.random = random;

            //Enemy Scale : deeper floors hit harder, elites and bosses harder still
            scale = 1f + BattleRules.FloorScale * (run.Floor - 1);
            if (enemy.Kind == EnemyKind.Normal) scale *= BattleRules.NormalScale;
            if (enemy.Kind == EnemyKind.Elite) scale *= BattleRules.EliteScale;
            if (enemy.Kind == EnemyKind.Boss) scale *= BattleRules.BossScale;

            EnemyTitle = enemy.KindLabel + "  /  " + enemy.Name;

            for (int b = 0; b < Results.Length; b++)
                Results[b] = new BeatResult();

            PrepareRound();
        }

        //Timing Windows : STEADY PULSE widens both
        public float PerfectWindow
        {
            get { return BattleRules.PerfectWindow + (Run.Has(MotifId.SteadyPulse) ? BattleRules.SteadyWindowBonus : 0f); }
        }

        public float GoodWindow
        {
            get { return BattleRules.GoodWindow + (Run.Has(MotifId.SteadyPulse) ? BattleRules.SteadyWindowBonus : 0f); }
        }

        //Choices Locked : THE METRONOME can only play what is written
        public bool ChoicesLocked
        {
            get { return Run.Conductor.Perk == ConductorPerk.LockedTempo; }
        }

        //Combo Bonus : the power multiplier the current combo gives
        public float ComboBonus
        {
            get
            {
                float step = BattleRules.ComboStep;
                if (Run.Has(MotifId.Crescendo)) step *= 2f;                 // CRESCENDO
                return 1f + step * Math.Min(Combo, BattleRules.ComboMax);
            }
        }

        //Round Prepare : slide the enemy pattern, scale it, and decide what TACET will do.
        //TACET decides at the START of the round, so it never reacts to the player's press.
        public void PrepareRound()
        {
            int shift = (Round - 1) * Enemy.RotatePerRound;

            for (int b = 0; b < BattleRules.BeatsPerRound; b++)
            {
                int source = (b + shift) % BattleRules.BeatsPerRound;
                EnemyPower[b] = (int)Math.Round(Enemy.Pattern[source] * scale);

                EnemyHidden[b] = false;
                for (int h = 0; h < Enemy.Hidden.Length; h++)
                    if (Enemy.Hidden[h] == source) EnemyHidden[b] = true;

                Results[b].Done = false;
            }

            for (int b = 0; b < BattleRules.BeatsPerRound; b++)
                EnemyChoice[b] = RollEnemyChoice(b);

            RoundLabel = "ROUND " + Round + " / " + BattleRules.MaxRounds;
        }

        //Heavy Beat : a beat at or above 60 percent of this round's strongest
        public bool IsHeavy(int beat)
        {
            int strongest = 1;
            for (int b = 0; b < BattleRules.BeatsPerRound; b++)
                if (EnemyPower[b] > strongest) strongest = EnemyPower[b];

            return EnemyPower[beat] >= strongest * BattleRules.HeavyThreshold;
        }

        //Enemy Choice Roll : heavy beats tend to boost, light beats tend to ease
        private Choice RollEnemyChoice(int beat)
        {
            if (EnemyPower[beat] == 0) return Choice.Normal;

            int boost = IsHeavy(beat) ? BattleRules.HeavyBoostChance : BattleRules.LightBoostChance;
            int ease = IsHeavy(beat) ? BattleRules.HeavyEaseChance : BattleRules.LightEaseChance;

            //Temper : a hot tempered enemy trades ease chances for boost chances
            boost += Enemy.Temper;
            ease -= Enemy.Temper;
            if (boost < 0) boost = 0;
            if (ease < 0) ease = 0;

            int roll = random.Next(100);
            if (roll < boost) return Choice.Boost;
            if (roll < boost + ease) return Choice.Ease;
            return Choice.Normal;
        }

        //Our Power : everyone planned on this beat, with seat rows, harmony and the conductor
        public int OurPowerAt(int beat)
        {
            Formation f = Run.Formation;
            float total = 0f;
            int players = 0;
            int cultures = 0;

            for (int s = 0; s < StageLayout.SeatCount; s++)
            {
                if (!f.Plays(s, beat)) continue;
                total += Run.PowerOf(f.Seated[s]) * StageLayout.RowOf(s).PowerScale;
                players++;

                //Culture Count : a culture counts once, the first time it turns up on this beat
                bool seenBefore = false;
                for (int t = 0; t < s; t++)
                    if (f.Plays(t, beat) && f.Seated[t].Culture == f.Seated[s].Culture) seenBefore = true;
                if (!seenBefore) cultures++;
            }

            if (players == 0) return 0;

            //Harmony : every extra player on the same beat adds a bonus
            float perExtra = BattleRules.HarmonyPerExtra;
            if (Run.Has(MotifId.Tutti)) perExtra += BattleRules.TuttiPerExtra;         // TUTTI
            float harmony = 1f + perExtra * (players - 1);

            //EVERY ROAD HOME : mixed cultures on one beat
            if (Run.Conductor.Perk == ConductorPerk.EveryRoadHome)
                harmony += BattleRules.CrossCultureHarmony * (cultures - 1);

            return (int)Math.Round(total * harmony * Run.PowerMultiplier);
        }

        //Our Cost : stamina this beat will take, before any choice
        public int OurCostAt(int beat)
        {
            Formation f = Run.Formation;
            float total = 0f;

            for (int s = 0; s < StageLayout.SeatCount; s++)
                if (f.Plays(s, beat))
                    total += Run.CostOf(f.Seated[s]) * StageLayout.RowOf(s).CostScale;

            total *= BattleRules.StaminaCostScale;
            if (ChoicesLocked) total *= BattleRules.LockedTempoCost;                    // LOCKED TEMPO

            return (int)Math.Round(total);
        }

        //Silent Recover : what a beat with nobody playing gives back
        public int SilentRecover
        {
            get
            {
                float recover = BattleRules.RestRecover;
                if (Run.Has(MotifId.Fermata)) recover *= BattleRules.FermataRecover;    // FERMATA
                return (int)recover;
            }
        }

        //Plan Totals : used by the score page to preview the round
        public int PlannedCost()
        {
            int total = 0;
            for (int b = 0; b < BattleRules.BeatsPerRound; b++)
                total += OurCostAt(b);
            return total;
        }

        public int PlannedRecover()
        {
            int total = 0;
            for (int b = 0; b < BattleRules.BeatsPerRound; b++)
                if (OurPowerAt(b) == 0) total += SilentRecover;
            return total;
        }

        public int ProjectedStamina()
        {
            int value = Run.Stamina - PlannedCost() + PlannedRecover();
            if (value > Run.MaxStamina) value = Run.MaxStamina;
            return value;
        }

        //Beat Tag : the planning hint for one beat. Hidden beats always count as attacks,
        //so the hint never gives away what the ??? is hiding.
        public BeatTag TagFor(int beat)
        {
            bool enemyActs = EnemyPower[beat] > 0 || EnemyHidden[beat];
            int players = Run.Formation.PlayersOnBeat(beat);

            if (players == 0 && !enemyActs) return BeatTag.Rest;
            if (players == 0) return BeatTag.Gap;
            if (!enemyActs) return BeatTag.Free;
            if (players >= 2) return BeatTag.Accent;
            return BeatTag.Covered;
        }

        //Beat Active : false when nobody plays at all, the duel skips the QTE on those
        public bool HasAction(int beat)
        {
            return OurPowerAt(beat) > 0 || EnemyPower[beat] > 0;
        }

        //Beat Resolve : the heart of the fight. Applies both choices, the timing grade,
        //the combo, stamina, and moves the line. Returns what happened so the page can show it.
        public BeatResult Resolve(int beat, Choice choice, Grade grade)
        {
            BeatResult r = Results[beat];
            if (ChoicesLocked) choice = Choice.Normal;                                  // LOCKED TEMPO

            int basePower = OurPowerAt(beat);
            float power = basePower;
            float cost = OurCostAt(beat);
            float recover = 0f;

            float easeRecover = BattleRules.EaseRecover;
            if (Run.Has(MotifId.Pianissimo)) easeRecover += BattleRules.PianissimoRecover;   // PIANISSIMO

            //Player Choice
            if (basePower == 0)
            {
                //Silent Beat : the band breathes, and easing breathes deeper
                recover += SilentRecover;
                if (choice == Choice.Ease) recover += easeRecover;
            }
            else if (choice == Choice.Boost)
            {
                bool sforzando = Run.Has(MotifId.Sforzando);                          // SFORZANDO
                power *= sforzando ? BattleRules.SforzandoPower : BattleRules.BoostPower;
                cost *= BattleRules.BoostCost;
            }
            else if (choice == Choice.Ease)
            {
                power *= BattleRules.EasePower;
                cost *= BattleRules.EaseCost;
                recover += easeRecover;
            }

            //Timing Grade : perfect helps and builds the combo, a bad press or no press hurts
            int comboBefore = Combo;
            bool runaway = Run.Conductor.Perk == ConductorPerk.RunawayFire;
            bool missedNow = false;

            if (grade == Grade.Perfect)
            {
                power *= BattleRules.PerfectBonus;
                recover *= BattleRules.PerfectBonus;
                PerfectCount++;
                Combo++;
            }
            else if (grade == Grade.Miss)
            {
                Combo = 0;
                missedNow = true;

                if (!runaway)                                                          // RUNAWAY FIRE ignores the loss
                    power *= Run.Has(MotifId.Rubato) ? BattleRules.RubatoMissPower : BattleRules.MissPower;
                if (basePower > 0 && !Run.Has(MotifId.Rubato))                         // RUBATO skips the extra cost
                    cost += BattleRules.MissExtraCost;
            }
            else if (grade == Grade.Hesitate)
            {
                Combo = 0;
                power *= BattleRules.HesitatePower;
            }

            if (Combo > BestCombo) BestCombo = Combo;
            r.Combo = Combo;
            r.ComboBroken = comboBefore >= 2 && Combo == 0;

            //Combo Bonus
            power *= ComboBonus;

            //RUNAWAY FIRE : the first beat we play after a miss burns hotter
            r.Fired = false;
            if (fireNext && basePower > 0 && !missedNow)
            {
                power *= BattleRules.RunawayFireBonus;
                fireNext = false;
                r.Fired = true;
            }
            if (missedNow && runaway) fireNext = true;

            //OVERTURE : the first beat of each round
            if (beat == 0 && Run.Has(MotifId.Overture)) power *= BattleRules.OverturePower;

            //THE CLOSER THE LOUDER : the further behind, the harder we hit
            if (Run.Conductor.Perk == ConductorPerk.CloserLouder && Line < 0f)
                power *= 1f + BattleRules.CloserLouderMax * (-Line / BattleRules.LineLimit);

            //Enemy Choice : decided at the start of the round, revealed now
            Choice enemyChoice = EnemyChoice[beat];
            float enemyPower = EnemyPower[beat];
            if (enemyChoice == Choice.Boost) enemyPower *= BattleRules.EnemyBoostPower;
            if (enemyChoice == Choice.Ease) enemyPower *= BattleRules.EnemyEasePower;

            //COUNTERPOINT : answering a boost
            if (enemyChoice == Choice.Boost && Run.Has(MotifId.Counterpoint))
                power *= BattleRules.CounterpointPower;

            //Out Of Breath : the band can only play as much as it can still pay for
            int costPaid = (int)Math.Round(cost);
            r.OutOfBreath = false;
            if (costPaid > Run.Stamina)
            {
                if (costPaid > 0) power *= Run.Stamina / (float)costPaid;
                costPaid = Run.Stamina;
                r.OutOfBreath = true;
            }

            //Clash : the difference pushes the line
            r.OurPower = (int)Math.Round(power);
            r.EnemyPower = (int)Math.Round(enemyPower);
            float push = (r.OurPower - r.EnemyPower) * BattleRules.PushPerPower;

            Line += push;
            if (Line > BattleRules.LineLimit) Line = BattleRules.LineLimit;
            if (Line < -BattleRules.LineLimit) Line = -BattleRules.LineLimit;

            //Stamina
            r.StaminaChange = (int)Math.Round(recover) - costPaid;
            Run.ChangeStamina(r.StaminaChange);

            //SECOND WIND : the first time the band runs dry, it gets a third back
            r.SecondWind = false;
            if (Run.Stamina == 0 && !secondWindUsed && Run.Has(MotifId.SecondWind))
            {
                Run.ChangeStamina((int)(Run.MaxStamina * BattleRules.SecondWindRefill));
                secondWindUsed = true;
                r.SecondWind = true;
            }

            r.Push = (int)Math.Round(push);
            r.PlayerChoice = choice;
            r.EnemyChoice = enemyChoice;
            r.Grade = grade;
            r.Done = true;

            //Instant Result : the line reached an edge
            if (Line >= BattleRules.LineLimit) { Finished = true; PlayerWon = true; }
            if (Line <= -BattleRules.LineLimit) { Finished = true; PlayerWon = false; }

            return r;
        }

        //Round End : move to the next round, or decide the fight on points after the last one
        public void EndRound()
        {
            if (Finished) return;

            Round++;
            if (Round > BattleRules.MaxRounds)
            {
                Finished = true;
                PlayerWon = Line > 0f;
                return;
            }

            PrepareRound();
        }

        //Shards : the reward for a won fight
        public int ShardsEarned()
        {
            int reward = BattleRules.ShardsNormal;
            if (Enemy.Kind == EnemyKind.Elite) reward = BattleRules.ShardsElite;
            if (Enemy.Kind == EnemyKind.Boss) reward = BattleRules.ShardsBoss;

            reward += PerfectCount * BattleRules.ShardsPerPerfect;
            if (Line > 0f) reward += (int)(Line / 10f);

            if (Run.Has(MotifId.PatronsPurse)) reward = (int)(reward * BattleRules.PurseBonus);   // PATRON'S PURSE
            return reward;
        }

        //Rank : a letter for how well the fight went, shown on the result page
        public string Rank()
        {
            if (!PlayerWon) return "-";
            if (Line >= BattleRules.LineLimit && PerfectCount >= 6) return "S";
            if (Line >= 60f || PerfectCount >= 6) return "A";
            if (Line >= 25f) return "B";
            return "C";
        }

        //Auto Plan : a sensible starting plan for players who do not want to tick every box.
        //Pass 1 answers every attack with just enough power.
        //Pass 2 spends leftover stamina on free hits where TACET is silent.
        public void AutoPlan()
        {
            Formation f = Run.Formation;
            f.ClearAll();

            int reserve = Run.MaxStamina / 4;    // always keep a quarter of the tank

            //Pass 1 : cover attacks. Hidden beats are guessed at a middling strength.
            for (int b = 0; b < BattleRules.BeatsPerRound; b++)
            {
                int need = EnemyHidden[b] ? (int)(5 * scale) : EnemyPower[b];
                if (need == 0) continue;

                bool[] used = new bool[StageLayout.SeatCount];
                while (OurPowerAt(b) <= need)
                {
                    int pick = StrongestFree(used);
                    if (pick < 0) break;
                    used[pick] = true;

                    f.Plan[pick, b] = true;
                    if (ProjectedStamina() < reserve)
                    {
                        f.Plan[pick, b] = false;   // too expensive, leave it
                        break;
                    }
                }
            }

            //Pass 2 : free hits with the single strongest player, while stamina allows
            int best = StrongestFree(new bool[StageLayout.SeatCount]);
            if (best < 0) return;

            for (int b = 0; b < BattleRules.BeatsPerRound; b++)
            {
                if (EnemyPower[b] > 0 || EnemyHidden[b]) continue;
                if (f.PlayersOnBeat(b) > 0) continue;

                f.Plan[best, b] = true;
                if (ProjectedStamina() < reserve * 2)
                    f.Plan[best, b] = false;
            }
        }

        //Strongest Free : the seated musician with the most power that is not in used yet
        private int StrongestFree(bool[] used)
        {
            int best = -1;
            for (int s = 0; s < StageLayout.SeatCount; s++)
            {
                Musician m = Run.Formation.Seated[s];
                if (m == null || used[s]) continue;
                if (best < 0 || Run.PowerOf(m) > Run.PowerOf(Run.Formation.Seated[best])) best = s;
            }
            return best;
        }
    }
}
