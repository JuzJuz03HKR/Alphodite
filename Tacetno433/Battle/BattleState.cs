using System;
using Microsoft.Xna.Framework;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Battle
{
    //Choice : what the band (or TACET) does on one beat. The player picks it by how big
    //the baton stroke is: small eases, middle plays, big boosts.
    //Written on TACET's notes as music marks: p (ease), mf (normal), f (boost).
    public enum Choice { Normal, Boost, Ease }

    //Grade : how well the stroke landed on the ring
    public enum Grade { None, Perfect, Good, Miss, Hesitate }

    //Forecast : how one beat looks on paper, shown on the score page before the fight
    public enum Forecast { Rest, FreeHit, Unguarded, Unknown, Dominating, Favored, Even, Struggling, Hopeless }

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
        public int Blow;               // TACET'S BLOW : stamina knocked out of the band on this beat (or its spark)
        public bool Collapsed;         // COLLAPSE : the band ran out of breath on this beat, the fight is lost

        //Extras : the duel page shows a word for each of these
        public int Combo;              // the combo after this beat
        public bool ComboBroken;       // a combo of 2 or more just ended
        public bool SecondWind;        // the SECOND WIND motif fired
        public bool Fired;             // THE INFERNO's bonus beat
        public bool Signature;         // the conductor's signature landed on this beat
        public bool Played;            // somebody on our side made a sound
        public bool Counter;           // a PERFECT BOOST knocked TACET's f note back
        public bool Fortissimo;        // the band was on fire for this beat
        public bool FortissimoStarted; // the combo just set the band on fire
        public bool FortissimoLost;    // a slip put the fire out
        public bool Tremolo;           // this beat was TACET's roll
        public int RollStrokes;        //     and how many strokes answered it
        public bool Fermata;           // this beat was TACET's held note
        public float Held;             //     and how much of it was held, 0 to 1

        //Double : the second note of a pair, answered half a beat after the first
        public bool Double;
        public Grade GraceGrade;
        public int GracePush;

        //Trait Fired : one flag per seat, true when that musician's trait changed this beat
        public bool[] TraitFired = new bool[StageLayout.SeatCount];
    }

    //BattleState : ALL THE COMBAT RULES, with no drawing in it.
    //
    //How a fight works:
    //   a round is 8 beats. Before each round the player decides which seat plays on which beat.
    //   on every beat both sides add up their power, both pick Normal / Boost / Ease,
    //   and the difference pushes a line. Push it to +100 and you win on the spot, get pushed
    //   to -100 and you lose. After 3 rounds, whoever is ahead wins.
    //   no single note pushes further than PUSH CAP, and elites and bosses are a HEAVY LINE
    //   (their pushes count less), so a duel is a run of beats, never one big hit (round 9).
    //   STAMINA is the band's breath, carried across the whole run. Planned notes cost some,
    //   silent beats and EASE give some back. A beat TACET wins knocks breath out of the band
    //   (TACET'S BLOW), and so does a MISS. At zero the band COLLAPSES and the fight is lost.
    //   Big strokes, rolls and holds cost nothing extra: the flashy moves are free.
    //   PERFECT strokes in a row build a COMBO that makes every beat stronger, and a long
    //   combo sets the band on fire (FORTISSIMO) for a few beats.
    //   some of TACET's notes come in pairs (the second one is a grace, see ResolveGrace).
    //   every round ends on a special note: from floor two ordinary enemies HOLD it (FERMATA,
    //   keep the baton still), everyone else ROLLS it (TREMOLO, shake the baton).
    //   a PERFECT BOOST against TACET's real f note is a COUNTER that knocks part of it back.
    //   the special notes arrive one floor at a time (BattleRules teaching order).
    //   far enough ahead at the end of a round, the band may try the FINALE and end it at once.
    //
    //Motifs, the conductor's perk, each musician's trait and the enemy's trait bend these rules.
    //Each place that checks one is marked with its name, so searching for it finds every effect.
    public class BattleState
    {
        public RunState Run;
        public Enemy Enemy;

        //Battle Progress
        public int Round = 1;
        public float Line = 0f;                  // -100 we lose  ..  +100 we win
        public bool Finished;
        public bool PlayerWon;
        public bool Collapsed;                   // lost by running out of breath, not by the line
        public int PerfectCount;

        //Combo
        public int Combo;                        // PERFECT strokes in a row
        public int BestCombo;
        public int FortissimoLeft;               // beats the band is still on fire for

        //Finale : the band tried to end the piece this round, and whether it worked
        public bool FinaleTried;
        public bool FinaleWon;

        //Roll Strokes : how many strokes the duel counted during TACET's tremolo, read by Resolve
        public int RollStrokes;

        //Hold Fraction : how much of TACET's fermata the baton was held still for, 0 to 1, read by Resolve
        public float HoldFraction;

        //Signature : the notes live here so they carry over from one round to the next
        public int[] Notes = new int[3];         // gathered per family : strings, winds, percussion
        public bool SignatureNext;               // the next beat resolved gets the signature bonus
        public bool SignatureArmed;              // SPACE was pressed, the next stroke is the signature

        //Round Data : rebuilt at the start of every round
        public int[] EnemyPower = new int[BattleRules.BeatsPerRound];
        public bool[] EnemyHidden = new bool[BattleRules.BeatsPerRound];
        public Choice[] EnemyChoice = new Choice[BattleRules.BeatsPerRound];
        public Choice[] ShownChoice = new Choice[BattleRules.BeatsPerRound];   // what the note SAYS it is (FALSE NOTES can lie)
        public bool[] EnemyDouble = new bool[BattleRules.BeatsPerRound];       // the note comes in a pair
        public int TremoloBeat = -1;                                           // the beat TACET rolls on, -1 for none
        public int FermataBeat = -1;                                           // the beat TACET holds, -1 for none
        public BeatResult[] Results = new BeatResult[BattleRules.BeatsPerRound];

        //Sheet : every beat of every round, kept for the result page. Sheet[round - 1][beat]
        public BeatResult[][] Sheet = new BeatResult[BattleRules.MaxRounds][];

        //Prepared Text
        public string RoundLabel = "";
        public string TempoLabel = "";
        public string EnemyTitle = "";

        private Random random;
        private float scale;                     // floor and kind scaling for the enemy
        private float lineWeight = 1f;           // HEAVY LINE : elites and bosses move the line less
        private bool secondWindUsed;
        private bool fireNext;                   // THE INFERNO : the next played beat is stronger
        private int[] lastPlan = new int[BattleRules.BeatsPerRound];   // ANSWER BETTER : last round's plan
        private bool bargainAnswered;
        private bool bargainTaken;

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

            //HEAVY LINE : an elite or a boss is heavier to move, so its duel lasts longer
            if (enemy.Kind == EnemyKind.Elite) lineWeight = BattleRules.EliteLine;
            if (enemy.Kind == EnemyKind.Boss) lineWeight = BattleRules.BossLine;

            EnemyTitle = enemy.KindLabel + "  /  " + enemy.Name;

            for (int b = 0; b < Results.Length; b++)
                Results[b] = new BeatResult();

            for (int round = 0; round < Sheet.Length; round++)
            {
                Sheet[round] = new BeatResult[BattleRules.BeatsPerRound];
                for (int b = 0; b < BattleRules.BeatsPerRound; b++)
                    Sheet[round][b] = new BeatResult();
            }

            PrepareRound();
        }

        //Enemy Has : true when this enemy's trait is switched on. Ordinary enemies keep their
        //trick for the later floors (Enemy.TraitFloor), so floor one teaches the plain game first.
        public bool EnemyHas(EnemyTrait trait)
        {
            return Enemy.Trait == trait && Run.Floor >= Enemy.TraitFloor;
        }

        //Trait Shown : the enemy has a trait on this floor, so the pages should explain it
        public bool TraitShown
        {
            get { return Enemy.Trait != EnemyTrait.None && Run.Floor >= Enemy.TraitFloor; }
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

        //Perfect Window At : COUNTS ALOUD widens it on the beats she plays
        public float PerfectWindowAt(int beat)
        {
            float window = PerfectWindow;
            if (TraitPlays(MusicianTrait.KeepsCount, beat)) window += BattleRules.KeepsCountWindow;
            return window;
        }

        //Late Forgiven : FASHIONABLY LATE, a late stroke on her beats still counts as GOOD
        public bool LateForgivenAt(int beat)
        {
            return TraitPlays(MusicianTrait.Forgiven, beat);
        }

        //Tempo : beats per minute this round. LULLABY and UNFINISHED change the last round.
        public int Tempo
        {
            get
            {
                int bpm = BattleRules.TempoBpm[Math.Min(Round, BattleRules.TempoBpm.Length) - 1];
                if (Round >= 3 && EnemyHas(EnemyTrait.Lullaby)) bpm = BattleRules.LullabyBpm;       // LULLABY
                if (Round >= 3 && EnemyHas(EnemyTrait.Unfinished)) bpm = BattleRules.UnfinishedBpm; // UNFINISHED
                return bpm;
            }
        }

        //Choices Locked : THE METRONOME's every stroke is a BOOST, whatever its size, and he can never EASE
        public bool ChoicesLocked
        {
            get { return Run.Conductor.Perk == ConductorPerk.LockedTempo; }
        }

        //Tremolo Most : strokes past this add nothing to a roll. ACCELERANDO counts more.
        public int TremoloMost
        {
            get { return BattleRules.TremoloMost + (Run.Has(MotifId.Accelerando) ? BattleRules.AccelerandoStrokes : 0); }
        }

        //Fortissimo Combo : PERFECTs in a row that set the band on fire. CON BRIO needs fewer.
        public int FortissimoCombo
        {
            get { return Run.Has(MotifId.ConBrio) ? BattleRules.ConBrioCombo : BattleRules.FortissimoCombo; }
        }

        //Finale Line : how far our way the line must be for the finale. CODA offers it sooner.
        public float FinaleLine
        {
            get { return Run.Has(MotifId.Coda) ? BattleRules.CodaLine : BattleRules.FinaleLine; }
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
        //TACET decides at the START of the round, so it never reacts to the player's stroke.
        public void PrepareRound()
        {
            int shift = (Round - 1) * Enemy.RotatePerRound;
            bool backwards = Round >= 3 && EnemyHas(EnemyTrait.Unfinished);                  // UNFINISHED

            for (int b = 0; b < BattleRules.BeatsPerRound; b++)
            {
                int written = backwards ? BattleRules.BeatsPerRound - 1 - b : b;
                int source = (written + shift) % BattleRules.BeatsPerRound;
                EnemyPower[b] = (int)Math.Round(Enemy.Pattern[source] * scale);

                EnemyHidden[b] = false;
                for (int h = 0; h < Enemy.Hidden.Length; h++)
                    if (Enemy.Hidden[h] == source) EnemyHidden[b] = true;

                Results[b].Done = false;
            }

            //ANSWER BETTER : from round two, the last plan the player wrote comes back at them.
            //It never drops below half of what was written, so a thin plan cannot starve it.
            if (Round >= 2 && EnemyHas(EnemyTrait.Mirror))
                for (int b = 0; b < BattleRules.BeatsPerRound; b++)
                    if (!EnemyHidden[b])
                        EnemyPower[b] = Math.Max((int)Math.Round(lastPlan[b] * BattleRules.MirrorScale), EnemyPower[b] / 2);

            //Last Note : every round ends on a special note two beats long. Ordinary enemies HOLD
            //it (fermata) from FermataFromFloor on, everyone else ROLLS it (tremolo), so on floor
            //one every enemy rolls. It always has a note, and it is never hidden, so the player can
            //plan for it on the score page.
            int last = BattleRules.BeatsPerRound - 1;
            bool hold = Enemy.Kind == EnemyKind.Normal && Run.Floor >= BattleRules.FermataFromFloor;
            bool roll = !hold && Run.Floor >= BattleRules.TremoloFromFloor;     // round 9 : whoever does not hold, rolls
            TremoloBeat = roll ? last : -1;
            FermataBeat = hold ? last : -1;
            if (roll || hold)
            {
                EnemyHidden[last] = false;
                if (EnemyPower[last] <= 0) EnemyPower[last] = Math.Max(1, Strongest() * 6 / 10);
            }

            for (int b = 0; b < BattleRules.BeatsPerRound; b++)
            {
                EnemyChoice[b] = b == TremoloBeat ? Choice.Normal : RollEnemyChoice(b);
                ShownChoice[b] = EnemyChoice[b];
            }

            //FALSE NOTES : one note in each bar shows the wrong loudness
            if (EnemyHas(EnemyTrait.FalseNotes))
                for (int bar = 0; bar < BattleRules.BeatsPerRound / 4; bar++)
                    PlantFalseNote(bar);

            PlantDoubles();

            RoundLabel = "ROUND " + Round + " / " + BattleRules.MaxRounds;
            TempoLabel = Tempo + " BPM";
        }

        //False Note : pick one plain, visible note in this bar and give it the wrong mark
        private void PlantFalseNote(int bar)
        {
            int picks = 0;
            int chosen = -1;
            for (int k = 0; k < 4; k++)
            {
                int b = bar * 4 + k;
                if (EnemyPower[b] <= 0 || EnemyHidden[b] || b == TremoloBeat) continue;
                picks++;
                if (random.Next(picks) == 0) chosen = b;     // every candidate gets a fair chance
            }
            if (chosen < 0) return;

            Choice truth = EnemyChoice[chosen];
            if (truth == Choice.Boost) ShownChoice[chosen] = Choice.Ease;
            else if (truth == Choice.Ease) ShownChoice[chosen] = Choice.Boost;
            else ShownChoice[chosen] = random.Next(2) == 0 ? Choice.Boost : Choice.Ease;
        }

        //Doubles Plant : pick which notes come in pairs, from PairsFromFloor on, a few more each
        //round. Hidden notes and the last note of the round never double.
        private void PlantDoubles()
        {
            for (int b = 0; b < BattleRules.BeatsPerRound; b++) EnemyDouble[b] = false;
            if (Run.Floor < BattleRules.PairsFromFloor) return;

            int want = BattleRules.DoubleNotes[Math.Min(Round, BattleRules.DoubleNotes.Length) - 1];
            if (want > BattleRules.DoubleMost) want = BattleRules.DoubleMost;

            for (int i = 0; i < want; i++)
            {
                //Fair Pick : every note still free gets the same chance
                int picks = 0;
                int chosen = -1;
                for (int b = 0; b < BattleRules.BeatsPerRound; b++)
                {
                    if (EnemyPower[b] <= 0 || EnemyHidden[b] || EnemyDouble[b] || b == TremoloBeat || b == FermataBeat) continue;
                    picks++;
                    if (random.Next(picks) == 0) chosen = b;
                }
                if (chosen < 0) return;
                EnemyDouble[chosen] = true;
            }
        }

        //Strongest : the biggest note TACET plays this round
        private int Strongest()
        {
            int strongest = 1;
            for (int b = 0; b < BattleRules.BeatsPerRound; b++)
                if (EnemyPower[b] > strongest) strongest = EnemyPower[b];
            return strongest;
        }

        //Heavy Beat : a beat at or above 60 percent of this round's strongest
        public bool IsHeavy(int beat)
        {
            return EnemyPower[beat] >= Strongest() * BattleRules.HeavyThreshold;
        }

        //Is Tremolo : this beat is TACET's roll
        public bool IsTremolo(int beat)
        {
            return beat == TremoloBeat;
        }

        //Is Fermata : this beat is TACET's held note
        public bool IsFermata(int beat)
        {
            return beat == FermataBeat;
        }

        //Roll Grade : how a roll of this many strokes is graded
        public static Grade RollGrade(int strokes)
        {
            if (strokes >= BattleRules.TremoloPerfect) return Grade.Perfect;
            if (strokes >= BattleRules.TremoloGood) return Grade.Good;
            if (strokes >= 1) return Grade.Miss;
            return Grade.Hesitate;
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

        //Seat Power : one seated player's part on a beat, with their row and their own trait.
        //Only traits that can be read off the plan are here, so the score page forecast
        //already shows them. Traits that react to the fight itself live in Resolve.
        public float SeatPowerAt(int seat, int beat)
        {
            Formation f = Run.Formation;
            Musician m = f.Seated[seat];
            float power = Run.PowerOf(m);

            //MOMENTUM : every beat in a row he played just before this one adds a little
            if (m.Trait == MusicianTrait.Momentum)
                power += Math.Min(StreakBefore(seat, beat), BattleRules.MomentumMax) * BattleRules.MomentumStep;

            power *= StageLayout.RowOf(seat).PowerScale;

            //BY EAR : a hidden note is no problem for someone who never read one
            if (m.Trait == MusicianTrait.ByEar && EnemyHidden[beat]) power *= BattleRules.ByEarPower;

            //FOUR BARS STRAIGHT : the last beat of a bar he played from its first beat
            if (m.Trait == MusicianTrait.FourBars && FullBarUpTo(seat, beat)) power *= BattleRules.FourBarsPower;

            return power;
        }

        //Streak Before : how many beats in a row this seat played right before this one
        private int StreakBefore(int seat, int beat)
        {
            int streak = 0;
            for (int b = beat - 1; b >= 0 && Run.Formation.Plays(seat, b); b--) streak++;
            return streak;
        }

        //Full Bar : true on the fourth beat of a bar when this seat played all four
        private bool FullBarUpTo(int seat, int beat)
        {
            if (beat % 4 != 3) return false;
            Formation f = Run.Formation;
            return f.Plays(seat, beat - 1) && f.Plays(seat, beat - 2) && f.Plays(seat, beat - 3);
        }

        //Plan Trait Fires : a trait from SeatPowerAt changes this beat (for the duel's pop ups)
        private bool PlanTraitFires(int seat, int beat)
        {
            Musician m = Run.Formation.Seated[seat];
            if (m.Trait == MusicianTrait.Momentum) return StreakBefore(seat, beat) > 0;
            if (m.Trait == MusicianTrait.ByEar) return EnemyHidden[beat];
            if (m.Trait == MusicianTrait.FourBars) return FullBarUpTo(seat, beat);
            return false;
        }

        //Held Note Seat : HELD NOTE. On a beat nobody plays, a player with this trait who played
        //the beat before rings on. Returns that seat, or -1.
        public int HeldNoteSeat(int beat)
        {
            if (beat <= 0) return -1;
            Formation f = Run.Formation;
            if (f.PlayersOnBeat(beat) > 0) return -1;

            for (int s = 0; s < StageLayout.SeatCount; s++)
                if (f.Plays(s, beat - 1) && f.Seated[s].Trait == MusicianTrait.HeldNote) return s;
            return -1;
        }

        //Trait Plays : somebody with this trait plays on this beat
        public bool TraitPlays(MusicianTrait trait, int beat)
        {
            Formation f = Run.Formation;
            for (int s = 0; s < StageLayout.SeatCount; s++)
                if (f.Plays(s, beat) && f.Seated[s].Trait == trait) return true;
            return false;
        }

        //Trait Share : how much of this beat's sound comes from players with this trait, 0 to 1
        private float TraitShare(MusicianTrait trait, int beat)
        {
            Formation f = Run.Formation;
            float all = 0f;
            float part = 0f;
            for (int s = 0; s < StageLayout.SeatCount; s++)
            {
                if (!f.Plays(s, beat)) continue;
                float p = SeatPowerAt(s, beat);
                all += p;
                if (f.Seated[s].Trait == trait) part += p;
            }
            return all > 0f ? part / all : 0f;
        }

        //Mark Trait : flag every seat with this trait that plays this beat
        private void MarkTrait(BeatResult r, MusicianTrait trait, int beat)
        {
            Formation f = Run.Formation;
            for (int s = 0; s < StageLayout.SeatCount; s++)
                if (f.Plays(s, beat) && f.Seated[s].Trait == trait) r.TraitFired[s] = true;
        }

        //Our Power : everyone planned on this beat, with seat rows, traits, harmony and the conductor
        public int OurPowerAt(int beat)
        {
            Formation f = Run.Formation;
            float total = 0f;
            int players = 0;
            int cultures = 0;

            for (int s = 0; s < StageLayout.SeatCount; s++)
            {
                if (!f.Plays(s, beat)) continue;
                total += SeatPowerAt(s, beat);
                players++;

                //Culture Count : a culture counts once, the first time it turns up on this beat
                bool seenBefore = false;
                for (int t = 0; t < s; t++)
                    if (f.Plays(t, beat) && f.Seated[t].Culture == f.Seated[s].Culture) seenBefore = true;
                if (!seenBefore) cultures++;
            }

            if (players == 0)
            {
                //HELD NOTE : nobody plays, but the note from the beat before is still ringing
                int held = HeldNoteSeat(beat);
                if (held < 0) return 0;
                return (int)Math.Round(SeatPowerAt(held, beat - 1) * BattleRules.HeldNoteShare * Run.PowerMultiplier);
            }

            //Harmony : every extra player on the same beat adds a bonus
            float perExtra = BattleRules.HarmonyPerExtra;
            if (Run.Has(MotifId.Tutti)) perExtra += BattleRules.TuttiPerExtra;         // TUTTI
            float harmony = 1f + perExtra * (players - 1);

            //EVERY ROAD HOME : mixed cultures on one beat
            if (Run.Conductor.Perk == ConductorPerk.EveryRoadHome)
                harmony += BattleRules.CrossCultureHarmony * (cultures - 1);

            //THE BARGAIN : the devil's deal, for this one round
            if (BargainActive) total *= BattleRules.BargainPower;

            return (int)Math.Round(total * harmony * Run.PowerMultiplier);
        }

        //Expected Power : what a beat should bring when it is answered the usual way, with a big
        //stroke. BOOST costs nothing extra, so the forecast and the auto plan count on it.
        public int ExpectedPowerAt(int beat)
        {
            return (int)Math.Round(OurPowerAt(beat) * BattleRules.BoostPower);
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

        //Enemy Strike : TACET's written power on a beat, as it will really land.
        //FILLS THE GAPS hits harder where nobody on our side plays, and a roll hits harder still.
        public int EnemyStrikeAt(int beat)
        {
            int power = EnemyPower[beat];
            if (power > 0 && EnemyHas(EnemyTrait.FillsGaps) && OurPowerAt(beat) == 0)
                power = (int)Math.Round(power * BattleRules.FillsGapsPower);
            if (beat == TremoloBeat) power = (int)Math.Round(power * BattleRules.TremoloEnemy);     // TREMOLO
            if (beat == FermataBeat) power = (int)Math.Round(power * BattleRules.FermataEnemy);     // FERMATA
            return power;
        }

        //Silent Recover : what a beat with nobody playing gives back
        public int SilentRecover
        {
            get
            {
                if (EnemyHas(EnemyTrait.NoRest)) return 0;                              // NO REST
                float recover = BattleRules.RestRecover;
                if (Run.Has(MotifId.BreathMark)) recover *= BattleRules.BreathMarkRecover;   // BREATH MARK
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

        //Signature Need : how many notes of a family the signature still asks for.
        //A family nobody on stage plays is skipped, so a recipe can never be impossible.
        //If the band has none of the families the recipe names, every family on stage needs 2.
        public int NeedFor(int family)
        {
            if (!BandHas(family)) return 0;

            int[] recipe = Run.Conductor.Recipe;
            int total = 0;
            for (int f = 0; f < recipe.Length; f++)
                if (BandHas(f)) total += recipe[f];

            if (total == 0) return 2;
            return recipe[family];
        }

        //Band Has : somebody seated plays this family
        public bool BandHas(int family)
        {
            Formation f = Run.Formation;
            for (int s = 0; s < StageLayout.SeatCount; s++)
                if (f.Seated[s] != null && (int)f.Seated[s].Family == family) return true;
            return false;
        }

        //Signature Ready : every family has the notes the recipe asks for
        public bool SignatureReady
        {
            get
            {
                bool asksForAny = false;
                for (int f = 0; f < Notes.Length; f++)
                {
                    int need = NeedFor(f);
                    if (need > 0) asksForAny = true;
                    if (Notes[f] < need) return false;
                }
                return asksForAny;
            }
        }

        //Notes Add : a good beat gives one note to each family that played on it
        public void AddNotes(int beat)
        {
            Formation formation = Run.Formation;
            for (int f = 0; f < Notes.Length; f++)
            {
                bool played = false;
                for (int s = 0; s < StageLayout.SeatCount; s++)
                    if (formation.Plays(s, beat) && (int)formation.Seated[s].Family == f) played = true;

                if (played && Notes[f] < NeedFor(f)) Notes[f]++;
            }
        }

        //Notes Spend : the signature uses them all up
        public void SpendNotes()
        {
            for (int f = 0; f < Notes.Length; f++) Notes[f] = 0;
        }

        //Forecast For : how a beat looks on paper, our planned power answered with a big stroke
        //(ExpectedPowerAt) against TACET's written power. TACET's own choice stays secret until its call, and
        //a hidden beat is never guessed, so the forecast gives nothing away.
        public Forecast ForecastFor(int beat)
        {
            int ours = ExpectedPowerAt(beat);
            int theirs = EnemyStrikeAt(beat);

            if (EnemyHidden[beat]) return Forecast.Unknown;
            if (ours == 0 && theirs == 0) return Forecast.Rest;
            if (ours == 0) return Forecast.Unguarded;
            if (theirs == 0) return Forecast.FreeHit;

            float ratio = ours / (float)theirs;
            if (ratio >= BattleRules.ForecastDominating) return Forecast.Dominating;
            if (ratio >= BattleRules.ForecastFavored) return Forecast.Favored;
            if (ratio >= BattleRules.ForecastEven) return Forecast.Even;
            if (ratio >= BattleRules.ForecastStruggling) return Forecast.Struggling;
            return Forecast.Hopeless;
        }

        //Forecast Line : where the line would end the round if every beat were answered big.
        //Hidden beats are left out, because nobody knows yet what they hold.
        public float ForecastLine()
        {
            float line = Line;
            for (int b = 0; b < BattleRules.BeatsPerRound; b++)
            {
                if (EnemyHidden[b]) continue;
                line += PushFor(ExpectedPowerAt(b), EnemyStrikeAt(b));
            }

            if (line > BattleRules.LineLimit) line = BattleRules.LineLimit;
            if (line < -BattleRules.LineLimit) line = -BattleRules.LineLimit;
            return line;
        }

        //Beat Active : false when nobody plays at all, the duel skips the stroke on those
        public bool HasAction(int beat)
        {
            return OurPowerAt(beat) > 0 || EnemyPower[beat] > 0;
        }

        //Beat Resolve : the heart of the fight. Applies both choices, the timing grade,
        //the combo, the traits, stamina, and moves the line. Returns what happened so the page can show it.
        public BeatResult Resolve(int beat, Choice choice, Grade grade)
        {
            BeatResult r = Results[beat];
            if (ChoicesLocked) choice = Choice.Boost;                                   // LOCKED TEMPO
            for (int s = 0; s < r.TraitFired.Length; s++) r.TraitFired[s] = false;

            int basePower = OurPowerAt(beat);
            float power = basePower;
            float cost = OurCostAt(beat);
            float recover = 0f;
            Choice enemyChoice = EnemyChoice[beat];

            //Plan Traits : marked here so the duel can show which musician did something
            Formation f = Run.Formation;
            for (int s = 0; s < StageLayout.SeatCount; s++)
                if (f.Plays(s, beat) && PlanTraitFires(s, beat)) r.TraitFired[s] = true;
            int held = basePower > 0 ? HeldNoteSeat(beat) : -1;
            if (held >= 0) r.TraitFired[held] = true;                                   // HELD NOTE

            //ONE STEP BETTER : her part swells against TACET's loud note
            if (enemyChoice == Choice.Boost)
            {
                float share = TraitShare(MusicianTrait.OneStepBetter, beat);
                if (share > 0f)
                {
                    power *= 1f + (BattleRules.OneStepBetterPower - 1f) * share;
                    MarkTrait(r, MusicianTrait.OneStepBetter, beat);
                }
            }

            float easeRecover = BattleRules.EaseRecover;
            if (Run.Has(MotifId.Pianissimo)) easeRecover += BattleRules.PianissimoRecover;   // PIANISSIMO

            //Player Choice
            if (basePower == 0)
            {
                //Silent Beat : the band breathes, and easing breathes deeper
                recover += SilentRecover;
                if (choice == Choice.Ease && !EnemyHas(EnemyTrait.NoRest)) recover += easeRecover;
            }
            else if (choice == Choice.Boost)
            {
                //BOOST : hits harder and costs nothing extra, a big stroke is always worth making
                bool sforzando = Run.Has(MotifId.Sforzando);                          // SFORZANDO
                power *= sforzando ? BattleRules.SforzandoPower : BattleRules.BoostPower;
            }
            else if (choice == Choice.Ease)
            {
                //THE QUIET PART : easing never softens her share of the beat
                float quiet = TraitShare(MusicianTrait.QuietPart, beat);
                power *= BattleRules.EasePower + (1f - BattleRules.EasePower) * quiet;
                if (quiet > 0f) MarkTrait(r, MusicianTrait.QuietPart, beat);
                cost *= BattleRules.EaseCost;
                recover += easeRecover;
            }

            //TREMOLO : TACET's roll is answered by many strokes instead of one. Every stroke adds
            //a little, up to a limit. Shaking is free, so shake hard.
            r.Tremolo = beat == TremoloBeat;
            r.RollStrokes = 0;
            if (r.Tremolo)
            {
                int counted = Math.Min(RollStrokes, TremoloMost);                     // ACCELERANDO inside
                power *= BattleRules.TremoloBase + BattleRules.TremoloStep * counted;
                r.RollStrokes = RollStrokes;
                RollStrokes = 0;
            }

            //FERMATA : TACET's held note. Our part grows with how long the baton was held still
            //after the stroke. Holding is free, the test is keeping the hand still.
            r.Fermata = beat == FermataBeat;
            r.Held = 0f;
            if (r.Fermata)
            {
                r.Held = MathHelper.Clamp(HoldFraction, 0f, 1f);
                float hold = Run.Has(MotifId.Tenuto) ? BattleRules.TenutoHold : BattleRules.FermataHold;   // TENUTO
                power *= BattleRules.FermataBase + hold * r.Held;
                HoldFraction = 0f;
            }

            //Timing Grade : perfect helps and builds the combo, a bad stroke or no stroke hurts
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

            //FORTISSIMO : the band is on fire for a few beats after a long combo. It hits harder,
            //and it does not tire (BattleRules.FortissimoCost), so the peak is never cut short.
            r.Fortissimo = false;
            if (FortissimoLeft > 0 && basePower > 0)
            {
                power *= BattleRules.FortissimoPower;
                cost *= BattleRules.FortissimoCost;
                r.Fortissimo = true;
            }
            if (FortissimoLeft > 0 && grade != Grade.None) FortissimoLeft--;
            UpdateFortissimo(r, grade);

            //SIGNATURE : the conductor's own move lands on this beat
            r.Signature = SignatureNext;
            if (SignatureNext)
            {
                power *= BattleRules.SignaturePower;
                SignatureNext = false;
            }

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

            //Enemy Choice : decided at the start of the round, shown in its call, applied now
            float enemyPower = EnemyStrikeAt(beat);                                    // FILLS THE GAPS inside
            if (enemyChoice == Choice.Boost) enemyPower *= BattleRules.EnemyBoostPower;
            if (enemyChoice == Choice.Ease) enemyPower *= BattleRules.EnemyEasePower;

            //COUNTERPOINT : answering a boost
            if (enemyChoice == Choice.Boost && Run.Has(MotifId.Counterpoint))
                power *= BattleRules.CounterpointPower;

            //COUNTER : a PERFECT BOOST against a real f note knocks most of it back.
            //A FALSE NOTE that only looked loud cannot be countered. MARCATO knocks back more.
            r.Counter = false;
            if (enemyChoice == Choice.Boost && choice == Choice.Boost && grade == Grade.Perfect && basePower > 0 && enemyPower > 0)
            {
                enemyPower *= Run.Has(MotifId.Marcato) ? BattleRules.MarcatoKeep : BattleRules.CounterKeep;   // MARCATO
                r.Counter = true;
            }

            //Short Of Breath : the band can only play as much as it can still pay for, and paying
            //the very last of it means the band COLLAPSES (see CheckBreath)
            int costPaid = (int)Math.Round(cost);
            if (costPaid > Run.Stamina)
            {
                if (costPaid > 0) power *= Run.Stamina / (float)costPaid;
                costPaid = Run.Stamina;
            }

            //Clash : the difference pushes the line
            r.OurPower = (int)Math.Round(power);
            r.EnemyPower = (int)Math.Round(enemyPower);
            r.Played = r.OurPower > 0;
            float push = PushFor(r.OurPower, r.EnemyPower);                             // PUSH CAP inside

            Line += push;
            if (Line > BattleRules.LineLimit) Line = BattleRules.LineLimit;
            if (Line < -BattleRules.LineLimit) Line = -BattleRules.LineLimit;

            //THE WHOLE FLOOR : his BOOST knocks TACET's next note down
            if (choice == Choice.Boost && beat + 1 < BattleRules.BeatsPerRound && EnemyPower[beat + 1] > 0
                && TraitPlays(MusicianTrait.Thunder, beat))
            {
                EnemyPower[beat + 1] = Math.Max(0, EnemyPower[beat + 1] - BattleRules.ThunderKnock);
                MarkTrait(r, MusicianTrait.Thunder, beat);
            }

            //TACET'S BLOW : a beat TACET wins hits the band for whatever got through
            r.Blow = BlowFor(r.OurPower, r.EnemyPower);

            //Stamina
            r.StaminaChange = (int)Math.Round(recover) - costPaid - r.Blow;
            Run.ChangeStamina(r.StaminaChange);

            r.Push = (int)Math.Round(push);
            r.PlayerChoice = choice;
            r.EnemyChoice = enemyChoice;
            r.Grade = grade;
            r.Double = EnemyDouble[beat];
            r.GraceGrade = Grade.None;
            r.GracePush = 0;
            r.Done = true;
            WriteSheet(beat, r);

            //Instant Result : the line reached an edge, or the band ran out of breath
            if (Line >= BattleRules.LineLimit) { Finished = true; PlayerWon = true; }
            if (Line <= -BattleRules.LineLimit) { Finished = true; PlayerWon = false; }
            CheckBreath(r);

            return r;
        }

        //Push For : how far one note moves the line. The power difference times PushPerPower,
        //but never further than PUSH CAP either way, so no single note decides the fight.
        //Against an elite or a boss the whole push is lighter still (HEAVY LINE).
        private float PushFor(int ours, int theirs)
        {
            float push = (ours - theirs) * BattleRules.PushPerPower;
            push = MathHelper.Clamp(push, -BattleRules.PushCap, BattleRules.PushCap);   // PUSH CAP
            return push * lineWeight;                                                   // HEAVY LINE
        }

        //Blow For : TACET'S BLOW, the stamina a beat costs the band when TACET wins it
        private static int BlowFor(int ours, int theirs)
        {
            if (theirs <= ours) return 0;
            return (int)Math.Round((theirs - ours) * BattleRules.BlowPerPower);
        }

        //Breath Check : breath at zero is a COLLAPSE, the band stops and the fight is lost.
        //SECOND WIND catches the band once. A fight the line already won is not lost this way.
        private void CheckBreath(BeatResult r)
        {
            r.SecondWind = false;
            r.Collapsed = false;
            if (Run.Stamina > 0 || Finished) return;

            //SECOND WIND : the first time the band would collapse, a third of its breath comes back
            if (!secondWindUsed && Run.Has(MotifId.SecondWind))
            {
                Run.ChangeStamina((int)(Run.MaxStamina * BattleRules.SecondWindRefill));
                secondWindUsed = true;
                r.SecondWind = true;
                return;
            }

            //COLLAPSE
            r.Collapsed = true;
            Collapsed = true;
            Finished = true;
            PlayerWon = false;
        }

        //Sheet Write : copy the beat into the record the result page reads
        private void WriteSheet(int beat, BeatResult r)
        {
            if (Round < 1 || Round > Sheet.Length) return;
            BeatResult mark = Sheet[Round - 1][beat];
            mark.Done = true;
            mark.OurPower = r.OurPower;
            mark.EnemyPower = r.EnemyPower;
            mark.Push = r.Push;
            mark.PlayerChoice = r.PlayerChoice;
            mark.EnemyChoice = r.EnemyChoice;
            mark.Grade = r.Grade;
            mark.Signature = r.Signature;
            mark.Played = r.Played;
            mark.Counter = r.Counter;
            mark.Tremolo = r.Tremolo;
            mark.Fermata = r.Fermata;
            mark.Double = r.Double;
            mark.GraceGrade = r.GraceGrade;
        }

        //Fortissimo Update : a miss or no stroke puts the fire out, and every FortissimoCombo
        //PERFECTs in a row light it again (CON BRIO needs fewer). The beat that lights it is not part of it yet.
        private void UpdateFortissimo(BeatResult r, Grade grade)
        {
            r.FortissimoStarted = false;
            r.FortissimoLost = false;

            if (grade == Grade.Miss || grade == Grade.Hesitate)
            {
                if (FortissimoLeft > 0) r.FortissimoLost = true;
                FortissimoLeft = 0;
            }

            if (grade == Grade.Perfect && Combo > 0 && Combo % FortissimoCombo == 0 && FortissimoLeft == 0)
            {
                FortissimoLeft = BattleRules.FortissimoBeats;
                r.FortissimoStarted = true;
            }
        }

        //Grace Resolve : the second note of a pair, half a beat after the first. It is worth part
        //of the beat on both sides: ours as the first note landed, TACET's the same. Missing the
        //flick back lets TACET's second note land with nothing against it.
        //The same BeatResult is handed back with the grace filled in, so the duel can show it.
        public BeatResult ResolveGrace(int beat, Grade grade)
        {
            BeatResult r = Results[beat];
            float ours = r.OurPower * BattleRules.GraceShare;
            float theirs = r.EnemyPower * BattleRules.GraceShare;
            int comboBefore = Combo;

            if (grade == Grade.Perfect)
            {
                ours *= BattleRules.PerfectBonus;
                PerfectCount++;
                Combo++;
            }
            else if (grade == Grade.Miss)
            {
                ours *= BattleRules.MissPower;
                Combo = 0;
            }

            //GRACE NOTE : a spark that lands on time counts twice
            if ((grade == Grade.Perfect || grade == Grade.Good) && Run.Has(MotifId.GraceNote))
                ours *= BattleRules.GraceNotePower;
            else if (grade == Grade.Hesitate)
            {
                ours = 0f;
                Combo = 0;
            }

            if (Combo > BestCombo) BestCombo = Combo;
            r.Combo = Combo;
            r.ComboBroken = comboBefore >= 2 && Combo == 0;
            UpdateFortissimo(r, grade);

            int ourPart = (int)Math.Round(ours);
            int theirPart = (int)Math.Round(theirs);
            float push = PushFor(ourPart, theirPart);                                   // PUSH CAP inside
            Line += push;
            if (Line > BattleRules.LineLimit) Line = BattleRules.LineLimit;
            if (Line < -BattleRules.LineLimit) Line = -BattleRules.LineLimit;

            //TACET'S BLOW : its second note gets through too
            r.Blow = BlowFor(ourPart, theirPart);
            Run.ChangeStamina(-r.Blow);

            r.GraceGrade = grade;
            r.GracePush = (int)Math.Round(push);
            if (Round >= 1 && Round <= Sheet.Length) Sheet[Round - 1][beat].GraceGrade = grade;

            if (Line >= BattleRules.LineLimit) { Finished = true; PlayerWon = true; }
            if (Line <= -BattleRules.LineLimit) { Finished = true; PlayerWon = false; }
            CheckBreath(r);
            return r;
        }

        //Finale : offered at the end of a round while the line is far enough our way.
        //One try per round. Winning it ends the fight on the spot.
        public bool FinaleOffered
        {
            get { return !Finished && !FinaleTried && Line >= FinaleLine; }       // CODA inside FinaleLine
        }

        public void WinFinale()
        {
            FinaleTried = true;
            FinaleWon = true;
            Line = BattleRules.LineLimit;
            Finished = true;
            PlayerWon = true;
        }

        public void FailFinale()
        {
            FinaleTried = true;
            Line = Math.Max(-BattleRules.LineLimit, Line - BattleRules.FinaleFailPush);
        }

        //Round End : move to the next round, or decide the fight on points after the last one
        public void EndRound()
        {
            if (Finished) return;

            //ANSWER BETTER remembers the plan that was just played
            for (int b = 0; b < BattleRules.BeatsPerRound; b++)
                lastPlan[b] = OurPowerAt(b);

            Round++;
            FinaleTried = false;
            if (Round > BattleRules.MaxRounds)
            {
                Finished = true;
                PlayerWon = Line > 0f;
                return;
            }

            PrepareRound();
        }

        //THE BARGAIN : before round two the devil offers a deal, once
        public bool BargainOffered
        {
            get { return Round == 2 && !bargainAnswered && EnemyHas(EnemyTrait.Bargain); }
        }

        public bool BargainActive
        {
            get { return bargainTaken && Round == 2; }
        }

        public void AnswerBargain(bool accept)
        {
            bargainAnswered = true;
            if (!accept) return;

            bargainTaken = true;
            Run.MaxStamina = Math.Max(20, Run.MaxStamina - BattleRules.BargainStamina);
            Run.ChangeStamina(0);     // keeps stamina under the new maximum and rebuilds the labels
        }

        //Shards : the reward for a won fight
        public int ShardsEarned()
        {
            int reward = BattleRules.ShardsNormal;
            if (Enemy.Kind == EnemyKind.Elite) reward = BattleRules.ShardsElite;
            if (Enemy.Kind == EnemyKind.Boss) reward = BattleRules.ShardsBoss;

            reward += PerfectCount * BattleRules.ShardsPerPerfect;
            if (Line > 0f) reward += (int)(Line / 10f);
            if (FinaleWon) reward += BattleRules.FinaleShards;                           // FINALE

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
                int need = EnemyHidden[b] ? (int)(5 * scale) : EnemyStrikeAt(b);
                if (need == 0) continue;

                bool[] used = new bool[StageLayout.SeatCount];
                while (ExpectedPowerAt(b) <= need)
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
