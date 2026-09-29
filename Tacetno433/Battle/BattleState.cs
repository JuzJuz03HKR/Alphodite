using System;
using Microsoft.Xna.Framework;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Battle
{
    //Choice : how loud a note is, written as music marks. TACET's notes carry one, and the
    //size of our stroke is one (round 15 : the note says which size to swing, the whole band plays).
    //   p   Ease    soft.   For us : a small stroke.
    //   mf  Normal  plain.  For us : a middle stroke.
    //   f   Boost   loud.   For us : a big stroke.
    public enum Choice { Normal, Boost, Ease }

    //Grade : how well the stroke landed on the ring. None is a beat let pass on purpose (a rest).
    public enum Grade { None, Perfect, Good, Miss, Hesitate }

    //BeatResult : what happened on one beat, kept so the duel page can show it afterwards
    public class BeatResult
    {
        public bool Done;
        public int BasePower;          // the players who came in, before timing, combo and the rest
        public int OurPower;
        public int EnemyPower;
        public int Push;
        public int StaminaChange;
        public Choice PlayerChoice;
        public Choice EnemyChoice;
        public Grade Grade;
        public int Blow;               // TACET'S BLOW : stamina knocked out of the band on this beat (or its spark)
        public int BlowIgnored;        // DEAF EARS : the blow the signature kept off the band, for the duel to show
        public bool Collapsed;         // COLLAPSE : the band ran out of breath on this beat, the fight is lost
        public bool WrongSize;         // the stroke was not the size the note asked for (half power)

        //Extras : the duel page shows a word for each of these
        public int Combo;              // the combo after this beat
        public bool ComboBroken;       // a combo of 2 or more just ended
        public bool SecondWind;        // the SECOND WIND motif fired
        public bool Fired;             // THE INFERNO's bonus beat
        public bool Signature;         // the conductor's signature landed on this beat
        public bool Played;            // somebody on our side made a sound
        public bool Rested;            // TACET was silent and the baton let the beat pass
        public int HeldSeat = -1;      // HELD NOTE : the seat still ringing on a beat nobody played
        public bool Counter;           // a PERFECT big stroke knocked TACET's f note back
        public bool InTune;            // the stroke's size matched TACET's real mark (IN TUNE)
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

        //Per Seat : who played on this beat, and whose trait changed it. A seat is a musician's
        //home seat, so it always means the same player (StageLayout.HomeSeat).
        public bool[] Joined = new bool[StageLayout.SeatCount];
        public bool[] TraitFired = new bool[StageLayout.SeatCount];
    }

    //BattleState : ALL THE COMBAT RULES, with no drawing in it.
    //
    //How a fight works:
    //   a round is 8 beats of TACET's part, and the rounds follow each other without leaving
    //   the duel (round 14). There is no plan and no seating to arrange.
    //   THE NOTE SAYS  every note of TACET's carries f, mf or p, and the stroke should be that
    //             size (big, middle, small). The right size plays at full strength, the wrong
    //             one at half (WrongSizePower). Round 15 : the size no longer picks who plays.
    //   THE BAND  everybody on stage plays every note the baton lands, like a real orchestra
    //             plays what is written (see Plays). Stroking costs nothing.
    //   CUE       the baton goes one way on every beat, and the players whose arrow points
    //             that way hit harder (see CueSideAt). The arrow is where they sit.
    //   the round is played through once, twice or three times in a row (REPEATS, round 11):
    //   the same beats each time, but TACET decides its f / mf / p again for every pass.
    //   on every beat both sides add up their power and the difference pushes a line. Push it
    //   to +100 and you win on the spot, get pushed to -100 and you lose. After 3 rounds,
    //   whoever is ahead wins.
    //   no single note pushes further than PUSH CAP, and elites and bosses are a HEAVY LINE
    //   (their pushes count less), so a duel is a run of beats, never one big hit (round 9).
    //   BREATH (the STAMINA number) is the band's life, carried across the whole run, like the
    //   life gauge of a rhythm game. Playing never spends it (round 15). A beat TACET wins
    //   knocks breath out of the band (TACET'S BLOW), a MISS costs a little more, and a silent
    //   beat let pass is a rest that gives some back. At zero the band COLLAPSES and the fight is lost.
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
        public int SignatureLeft;                // strokes the conductor's signature still lasts, 0 when none is running

        //Round Data : rebuilt at the start of every round
        public int[] EnemyPower = new int[BattleRules.BeatsPerRound];
        public bool[] EnemyHidden = new bool[BattleRules.BeatsPerRound];
        public Choice[] EnemyChoice = new Choice[BattleRules.BeatsPerRound];
        public Choice[] ShownChoice = new Choice[BattleRules.BeatsPerRound];   // what the note SAYS it is (FALSE NOTES can lie)
        public bool[] EnemyDouble = new bool[BattleRules.BeatsPerRound];       // the note comes in a pair
        public int TremoloBeat = -1;                                           // the beat TACET rolls on, -1 for none
        public int FermataBeat = -1;                                           // the beat TACET holds, -1 for none
        public BeatResult[] Results = new BeatResult[BattleRules.BeatsPerRound];

        //REPEATS : this round's phrase is played Passes times in a row. Pass is the time through
        //being answered now, 0 for the first. EnemyChoice, ShownChoice and EnemyDouble above always
        //hold the marks of that pass. The marks of every pass are decided when the round starts
        //(see PrepareRound), so the duel can already show the next pass coming down the lane.
        public int Passes = 1;
        public int Pass;
        private Choice[,] passChoice = new Choice[BattleRules.PassesMost, BattleRules.BeatsPerRound];
        private Choice[,] passShown = new Choice[BattleRules.PassesMost, BattleRules.BeatsPerRound];
        private bool[,] passDouble = new bool[BattleRules.PassesMost, BattleRules.BeatsPerRound];
        private int[,] passPower = new int[BattleRules.PassesMost, BattleRules.BeatsPerRound];   // each pass's notes before THE WHOLE FLOOR knocks them
        private int[,] passNote = new int[BattleRules.PassesMost, BattleRules.BeatsPerRound];    // SONG : the pitch of each note (MIDI), 0 for none

        //SONG : the phrase of the enemy's song this round starts on. Round 1 plays phrase 0, each
        //round then carries on from where the last one stopped (see EndRound).
        private int songStart;

        //Sheet : every beat of every round, kept for the result page. Sheet[round - 1][beat].
        //When a round is played more than once, it keeps the last time through.
        public BeatResult[][] Sheet = new BeatResult[BattleRules.MaxRounds][];

        //Prepared Text
        public string RoundLabel = "";
        public string TempoLabel = "";
        public string PassesLabel = "";          // "PLAYED x2", empty when the round is played once
        public string EnemyTitle = "";
        public string SongLabel = "";            // "ODE TO JOY  /  BEETHOVEN", empty for an enemy without a song
        private static string[] passesWords = { "", "", "PLAYED x2", "PLAYED x3" };

        private Random random;
        private float scale;                     // floor and kind scaling for the enemy
        private float lineWeight = 1f;           // HEAVY LINE : elites and bosses move the line less
        private bool secondWindUsed;
        private bool fireNext;                   // THE INFERNO : the next played beat is stronger
        private int[] lastRound = new int[BattleRules.BeatsPerRound];  // ANSWER BETTER : what the band played last round
        private bool bargainAnswered;
        private bool bargainTaken;
        private int lastSilenced = -1;           // SILENT MOUTHS : the section silenced last round

        //Seat Memory : what each seat did on the beats before this one, for the traits that
        //count a run of beats. Every round starts them afresh.
        private bool[] playedLast = new bool[StageLayout.SeatCount];   // HELD NOTE : played the beat before
        private int[] streak = new int[StageLayout.SeatCount];         // MOMENTUM : beats in a row
        private int[] barCount = new int[StageLayout.SeatCount];       // FOUR BARS : beats of this bar

        //SILENT MOUTHS : the section (0 strings, 1 winds, 2 percussion) THE MUTE CHOIR silences
        //this round, -1 for none (round 15, it was a letter p / mf / f)
        public int SilencedSection = -1;

        public BattleState(RunState run, Enemy enemy, Random random)
        {
            Run = run;
            Enemy = enemy;
            this.random = random;

            //Enemy Scale : deeper floors hit harder, elites and bosses harder still
            scale = BattleRules.FloorPower[Math.Min(run.Floor, BattleRules.FloorPower.Length) - 1];
            if (enemy.Kind == EnemyKind.Normal) scale *= BattleRules.NormalScale;
            if (enemy.Kind == EnemyKind.Elite) scale *= BattleRules.EliteScale;
            if (enemy.Kind == EnemyKind.Boss) scale *= BattleRules.BossScale;

            //HEAVY LINE : an elite or a boss is heavier to move, so its duel lasts longer
            if (enemy.Kind == EnemyKind.Elite) lineWeight = BattleRules.EliteLine;
            if (enemy.Kind == EnemyKind.Boss) lineWeight = BattleRules.BossLine;

            EnemyTitle = enemy.KindLabel + "  /  " + enemy.Name;
            if (enemy.Song != null) SongLabel = enemy.Song.Label;

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

        //Perfect Window At : COUNTS ALOUD widens it on her beats, the ones the baton goes her way
        public float PerfectWindowAt(int beat)
        {
            float window = PerfectWindow;
            if (CuedSeat(MusicianTrait.KeepsCount, beat) >= 0) window += BattleRules.KeepsCountWindow;
            return window;
        }

        //Late Forgiven : FASHIONABLY LATE, on her beats (the baton goes her way) a late stroke
        //still counts as GOOD
        public bool LateForgivenAt(int beat)
        {
            return CuedSeat(MusicianTrait.Forgiven, beat) >= 0;
        }

        //Cued Seat : a player with this trait whose arrow the baton follows on this beat, or -1
        public int CuedSeat(MusicianTrait trait, int beat)
        {
            for (int s = 0; s < StageLayout.SeatCount; s++)
                if (CanPlay(s) && Run.Formation.Seated[s].Trait == trait && IsCued(s, beat)) return s;
            return -1;
        }

        //Playing Seat : a seat with this trait that plays, or -1
        public int PlayingSeat(MusicianTrait trait)
        {
            for (int s = 0; s < StageLayout.SeatCount; s++)
                if (Plays(s) && Run.Formation.Seated[s].Trait == trait) return s;
            return -1;
        }

        //CUE : which arrow the baton follows on this beat, 0 left, 1 down and up, 2 right.
        //REQUIEM mirrors the band in round three: the left players answer right and back.
        public int CueSideAt(int beat)
        {
            int side = BattleRules.CueSide[beat % 4];
            if (Mirrored) side = 2 - side;                                              // UNFINISHED
            return side;
        }

        //Is Cued : this seat's player answers the way the baton goes on this beat (their arrow)
        public bool IsCued(int seat, int beat)
        {
            return StageLayout.SeatSide(seat) == CueSideAt(beat);
        }

        //Mirrored : UNFINISHED, in round three the left and right arrows swap
        public bool Mirrored
        {
            get { return Round >= 3 && EnemyHas(EnemyTrait.Unfinished); }
        }

        //Can Play : somebody sits here, and not in the section SILENT MOUTHS has silenced. If the
        //whole band is that section the silence does not hold, a band always has a sound.
        public bool CanPlay(int seat)
        {
            Formation f = Run.Formation;
            if (f.Seated[seat] == null) return false;
            if (SilencedSection < 0 || StageLayout.SeatRow[seat] != SilencedSection) return true;

            for (int s = 0; s < StageLayout.SeatCount; s++)
                if (f.Seated[s] != null && StageLayout.SeatRow[s] != SilencedSection) return false;
            return true;
        }

        //Plays : does this seat play when the baton lands a note? Everybody on stage does, the
        //way a real orchestra plays what is written (round 15). Round 12 to 14 : the size of the
        //stroke picked who came in (DYNAMICS), a small stroke only the p players.
        public bool Plays(int seat)
        {
            return CanPlay(seat);
        }

        //Tempo : beats per minute this round. It grows with the floor as well as the round, so the
        //first floor is gentle and the last one as quick as ever (round 15, TempoByFloor).
        //LULLABY and UNFINISHED change the last round.
        public int Tempo
        {
            get
            {
                int[] floorTempo = BattleRules.TempoByFloor[Math.Min(Run.Floor, BattleRules.TempoByFloor.Length) - 1];
                int bpm = floorTempo[Math.Min(Round, floorTempo.Length) - 1];
                if (Round >= 3 && EnemyHas(EnemyTrait.Lullaby)) bpm = floorTempo[0] - BattleRules.LullabySlower;    // LULLABY
                if (Round >= 3 && EnemyHas(EnemyTrait.Unfinished)) bpm += BattleRules.UnfinishedFaster;            // UNFINISHED
                return bpm;
            }
        }

        //Gentle : the first fights of a run, where TACET only plays f and p, never mf, so the
        //first thing to learn is two sizes (round 15, GentleFights)
        public bool Gentle
        {
            get { return Run.Floor == 1 && Run.BattlesWon < BattleRules.GentleFights; }
        }

        //Asks Size : this note asks for a size, the one its mark SAYS (a FALSE NOTE lies). A
        //silent beat, a hidden ??? note, a roll and a held note take any size.
        public bool AsksSize(int beat)
        {
            return !IsSilent(beat) && !EnemyHidden[beat] && beat != TremoloBeat && beat != FermataBeat;
        }

        //Size Free : on this beat any size is right. LOCKED TEMPO, THE METRONOME's band reads the
        //marks for him, so he only keeps time (round 15, round 12.2 : the band played the mark).
        //ABSOLUTE PITCH, THE APPRENTICE's signature, is right at any size too.
        public bool SizeFree(int beat)
        {
            if (!AsksSize(beat)) return true;
            return Run.Conductor.Perk == ConductorPerk.LockedTempo || SignatureIs(SignatureMove.AbsolutePitch);
        }

        //Size Right : the stroke is the size the note asks for
        public bool SizeRight(int beat, Choice drawn)
        {
            return SizeFree(beat) || drawn == ShownChoice[beat];
        }

        //SIGNATURE : SPACE with the recipe full lets the conductor's own move loose, for the next
        //few strokes (BattleRules.SignatureStrokes). Every conductor has a different one (round 13):
        //   ABSOLUTE PITCH  THE APPRENTICE   every stroke on time is the right size and IN TUNE
        //   CLOCKWORK       THE METRONOME    every stroke on time is PERFECT (SignatureGrade)
        //   SET ALIGHT      THE INFERNO      every stroke hits harder and may push past the PUSH CAP
        //   DEAF EARS       THE UNHEARING    TACET's blows take no breath
        //   VILLAGE BAND    THE FOLK LEADER  every stroke on time gives breath back (round 15, it was
        //                                    the whole band paid as a small stroke, and playing is free now)
        //A stroke on a silent beat counts as one of the strokes, a rest does not.
        public bool SignatureOn
        {
            get { return SignatureLeft > 0; }
        }

        public bool SignatureIs(SignatureMove move)
        {
            return SignatureLeft > 0 && Run.Conductor.Move == move;
        }

        public void StartSignature()
        {
            SpendNotes();
            SignatureLeft = BattleRules.SignatureStrokes;
        }

        //Signature Grade : CLOCKWORK, a stroke on time (a GOOD) counts as PERFECT. A MISS, late,
        //early or the wrong way, stays a MISS. The duel asks here with the grade it judged.
        public Grade SignatureGrade(Grade grade)
        {
            if (SignatureIs(SignatureMove.Clockwork) && grade == Grade.Good) return Grade.Perfect;   // CLOCKWORK
            return grade;
        }

        //Roll Counted : how many of these shakes count towards a roll. Past TremoloMost they add
        //nothing. ACCELERANDO counts every shake twice (round 11), so the top is reached sooner.
        public int RollCounted(int strokes)
        {
            if (Run.Has(MotifId.Accelerando)) strokes *= BattleRules.AccelerandoCount;      // ACCELERANDO
            return Math.Min(strokes, BattleRules.TremoloMost);
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

        //Louder Bonus : THE CLOSER THE LOUDER, how much harder THE UNHEARING's band hits right now.
        //0 while the band has more than CloserLouderFrom of its breath, growing to CloserLouderMax
        //with none left. 0 for every other conductor.
        public float LouderBonus
        {
            get
            {
                if (Run.Conductor.Perk != ConductorPerk.CloserLouder || Run.MaxStamina <= 0) return 0f;
                float share = Run.Stamina / (float)Run.MaxStamina;
                float from = BattleRules.CloserLouderFrom;
                if (share >= from) return 0f;
                return BattleRules.CloserLouderMax * MathHelper.Clamp((from - share) / from, 0f, 1f);
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

            //ANSWER BETTER : from round two, what the band played last round comes back at it.
            //It never drops below half of what was written, so a quiet round cannot starve it.
            if (Round >= 2 && EnemyHas(EnemyTrait.Mirror))
                for (int b = 0; b < BattleRules.BeatsPerRound; b++)
                    if (!EnemyHidden[b])
                        EnemyPower[b] = Math.Max((int)Math.Round(lastRound[b] * BattleRules.MirrorScale), EnemyPower[b] / 2);

            //Seat Memory : runs of beats start afresh every round
            for (int s = 0; s < StageLayout.SeatCount; s++)
            {
                playedLast[s] = false;
                streak[s] = 0;
                barCount[s] = 0;
            }

            //SILENT MOUTHS : from round two one section of the band is silenced for the whole round,
            //picked now so the duel can say so on the round's banner
            SilencedSection = -1;
            if (EnemyHas(EnemyTrait.SilentMouths) && Round >= 2) SilencedSection = PickSilencedSection();

            //Last Note : every round ends on a special note two beats long. Ordinary enemies HOLD
            //it (fermata) from FermataFromFloor on, elites and bosses ROLL it (tremolo). Round 15 :
            //ordinary enemies on floor one end on a plain note, the first fights teach the plain
            //game (round 9 to 14 : every enemy rolled on floor one). A special note always has a
            //note, and it is never hidden.
            int last = BattleRules.BeatsPerRound - 1;
            bool hold = Enemy.Kind == EnemyKind.Normal && Run.Floor >= BattleRules.FermataFromFloor;
            bool roll = Enemy.Kind != EnemyKind.Normal && Run.Floor >= BattleRules.TremoloFromFloor;
            TremoloBeat = roll ? last : -1;
            FermataBeat = hold ? last : -1;
            if (roll || hold)
            {
                EnemyHidden[last] = false;
                if (EnemyPower[last] <= 0) EnemyPower[last] = Math.Max(1, Strongest() * 6 / 10);
            }

            int[] roundPower = new int[BattleRules.BeatsPerRound];
            for (int b = 0; b < BattleRules.BeatsPerRound; b++)
                roundPower[b] = EnemyPower[b];

            //REPEATS : the same beats every time through, but the marks and the pairs are
            //decided afresh for each pass, all of them now.
            //SONG : an enemy with a song plays the next phrase of it on every pass instead, its
            //notes, rests and marks exactly as written (SongPhrase).
            Passes = BattleRules.PassesPerRound[Math.Min(Round, BattleRules.PassesPerRound.Length) - 1];
            for (int p = 0; p < Passes; p++)
            {
                for (int b = 0; b < BattleRules.BeatsPerRound; b++)
                {
                    EnemyPower[b] = roundPower[b];
                    passNote[p, b] = SongList.PlainMelody[b];
                    EnemyChoice[b] = b == TremoloBeat ? Choice.Normal : RollEnemyChoice(b);
                    ShownChoice[b] = EnemyChoice[b];
                }
                if (Enemy.Song != null) SongPhrase(p);

                //FALSE NOTES : one note in each bar shows the wrong loudness
                if (EnemyHas(EnemyTrait.FalseNotes))
                    for (int bar = 0; bar < BattleRules.BeatsPerRound / 4; bar++)
                        PlantFalseNote(bar);

                PlantDoubles();

                for (int b = 0; b < BattleRules.BeatsPerRound; b++)
                {
                    passChoice[p, b] = EnemyChoice[b];
                    passShown[p, b] = ShownChoice[b];
                    passDouble[p, b] = EnemyDouble[b];
                    passPower[p, b] = EnemyPower[b];
                }
            }
            BeginPass(0);

            RoundLabel = "ROUND " + Round + " / " + BattleRules.MaxRounds;
            TempoLabel = Tempo + " BPM";
            PassesLabel = passesWords[Passes];
        }

        //Song Phrase : SONG, pass p of this round plays the next phrase of the enemy's song. Each
        //note hits as hard as the enemy's pattern does on average (Enemy.SongNotePower), and keeps
        //the mark the song writes. The round's last note is still a roll or a hold when the enemy
        //has one (a silent last beat then holds on to the note before it).
        //GENTLE fights, the first of a run, play only the notes on beats 1 and 3 of every bar,
        //and an mf there is played p, so the song is learnt with two sizes at half the speed.
        private void SongPhrase(int p)
        {
            SongChart song = Enemy.Song;
            int phrase = (songStart + p) % song.PhraseCount;
            int power = Math.Max(1, (int)Math.Round(Enemy.SongNotePower() * scale));
            int last = BattleRules.BeatsPerRound - 1;
            int held = 0;

            for (int b = 0; b < BattleRules.BeatsPerRound; b++)
            {
                int note = song.Notes[phrase, b];
                Choice mark = song.Marks[phrase, b];
                if (Gentle && b % 2 == 1) note = 0;                                         // GENTLE : beats 1 and 3 only
                if (Gentle && mark == Choice.Normal) mark = Choice.Ease;                    // GENTLE : f and p only
                if (note > 0) held = note;

                //Last Note : a roll or a hold always has a note, the one before it rings on
                bool special = b == last && (TremoloBeat == last || FermataBeat == last);
                if (special && note == 0) { note = held > 0 ? held : SongList.PlainMelody[b]; mark = Choice.Normal; }

                EnemyPower[b] = note > 0 ? power : 0;
                EnemyHidden[b] = EnemyHidden[b] && note > 0;
                passNote[p, b] = note;
                EnemyChoice[b] = b == TremoloBeat ? Choice.Normal : mark;
                ShownChoice[b] = EnemyChoice[b];
            }
        }

        //Note At : SONG, the pitch (MIDI number) the band plays for this beat of this pass. An
        //enemy without a song gives a plain line, so the band always plays in tune.
        public int NoteAt(int pass, int beat)
        {
            return passNote[Math.Min(pass, BattleRules.PassesMost - 1), beat];
        }

        //Pass Begin : the next time through the phrase. Its marks and pairs become the round's,
        //and TACET's notes are whole again (THE WHOLE FLOOR only knocks a note down once).
        public void BeginPass(int pass)
        {
            Pass = pass;
            for (int b = 0; b < BattleRules.BeatsPerRound; b++)
            {
                EnemyChoice[b] = passChoice[pass, b];
                ShownChoice[b] = passShown[pass, b];
                EnemyDouble[b] = passDouble[pass, b];
                EnemyPower[b] = passPower[pass, b];
                Results[b].Done = false;
            }
        }

        //Pass Marks : how a note of any pass is written, so the duel can draw a pass that has
        //not begun yet. The pass being answered reads the live arrays (THE WHOLE FLOOR inside).
        public Choice ShownAt(int pass, int beat)
        {
            return passShown[pass, beat];
        }

        public bool DoubleAt(int pass, int beat)
        {
            return passDouble[pass, beat];
        }

        public int PowerAt(int pass, int beat)
        {
            return pass == Pass ? EnemyPower[beat] : passPower[pass, beat];
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

        //Silenced Section Pick : SILENT MOUTHS takes one section that has somebody in it, fairly
        //at random, and never the same one two rounds running. A band of one section only is
        //left alone, it would have no sound at all.
        private int PickSilencedSection()
        {
            Formation f = Run.Formation;
            int used = 0;
            int picks = 0;
            int chosen = -1;
            for (int section = 0; section < StageLayout.Rows.Length; section++)
            {
                bool has = false;
                for (int s = 0; s < StageLayout.SeatCount; s++)
                    if (f.Seated[s] != null && StageLayout.SeatRow[s] == section) has = true;
                if (!has) continue;
                used++;
                if (section == lastSilenced) continue;
                picks++;
                if (random.Next(picks) == 0) chosen = section;
            }
            if (used < 2) return -1;
            if (chosen < 0) chosen = lastSilenced;
            lastSilenced = chosen;
            return chosen;
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

        //Enemy Choice Roll : heavy beats tend to boost, light beats tend to ease.
        //Gentle fights (the first of a run) play only f on heavy beats and p on light ones.
        private Choice RollEnemyChoice(int beat)
        {
            if (EnemyPower[beat] == 0) return Choice.Normal;
            if (Gentle) return IsHeavy(beat) ? Choice.Boost : Choice.Ease;

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

        //Seat Power : one player's sound on a beat, with the CUE and their own trait. The traits
        //that count a run of beats read the seat memory, which Resolve moves on after every
        //beat. Traits that react to the stroke itself live in Resolve.
        //Round 14 : the power is the musician's own number, the one on their card (the rows used
        //to scale it, x0.8 at the back and x1.3 at the front).
        public float SeatPowerAt(int seat, int beat)
        {
            Musician m = Run.Formation.Seated[seat];
            float power = Run.PowerOf(m);

            //MOMENTUM : every beat in a row he played just before this one adds a little
            if (m.Trait == MusicianTrait.Momentum)
                power += Math.Min(streak[seat], BattleRules.MomentumMax) * BattleRules.MomentumStep;

            //BY EAR : a hidden note is no problem for someone who never read one
            if (m.Trait == MusicianTrait.ByEar && EnemyHidden[beat]) power *= BattleRules.ByEarPower;

            //FOUR BARS STRAIGHT : the last beat of a bar he played from its first beat
            if (m.Trait == MusicianTrait.FourBars && FullBarUpTo(seat, beat)) power *= BattleRules.FourBarsPower;

            //THE QUIET PART : against TACET's real p note, she plays it back twice as hard
            if (m.Trait == MusicianTrait.QuietPart && QuietNote(beat)) power *= BattleRules.QuietPartPower;

            //SPARE STICKS : the percussion hit harder on TACET's real f notes (round 15)
            if (m.Family == Family.Percussion && Run.Has(MotifId.SpareSticks) && LoudNote(beat))
                power *= BattleRules.SpareSticksPower;

            //CUE : the baton goes this player's way
            if (IsCued(seat, beat)) power *= BattleRules.CuePower;

            return power;
        }

        //Quiet Note : TACET really plays this beat soft (p), a note that is there and not hidden
        private bool QuietNote(int beat)
        {
            return EnemyChoice[beat] == Choice.Ease && EnemyPower[beat] > 0 && !EnemyHidden[beat];
        }

        //Loud Note : TACET really plays this beat loud (f), a note that is there and not hidden
        private bool LoudNote(int beat)
        {
            return EnemyChoice[beat] == Choice.Boost && EnemyPower[beat] > 0 && !EnemyHidden[beat];
        }

        //Full Bar : true on the fourth beat of a bar when this seat played the three before it
        private bool FullBarUpTo(int seat, int beat)
        {
            return beat % 4 == 3 && barCount[seat] >= 3;
        }

        //Trait Fires : a trait from SeatPowerAt changes this beat (for the duel's pop ups)
        private bool TraitFires(int seat, int beat)
        {
            Musician m = Run.Formation.Seated[seat];
            if (m.Trait == MusicianTrait.Momentum) return streak[seat] > 0;
            if (m.Trait == MusicianTrait.ByEar) return EnemyHidden[beat];
            if (m.Trait == MusicianTrait.FourBars) return FullBarUpTo(seat, beat);
            if (m.Trait == MusicianTrait.QuietPart) return QuietNote(beat);
            return false;
        }

        //Held Note Seat : HELD NOTE. On a beat nobody plays, a player with this trait who played
        //the beat before rings on. Returns that seat, or -1.
        public int HeldNoteSeat()
        {
            for (int s = 0; s < StageLayout.SeatCount; s++)
                if (playedLast[s] && CanPlay(s) && Run.Formation.Seated[s].Trait == MusicianTrait.HeldNote) return s;
            return -1;
        }

        //Power For : the whole band on this beat, with the CUE, their traits, harmony and the
        //conductor. Before the size, the timing grade and the combo.
        public int PowerFor(int beat)
        {
            Formation f = Run.Formation;
            float total = 0f;
            int players = 0;
            int cultures = 0;

            for (int s = 0; s < StageLayout.SeatCount; s++)
            {
                if (!Plays(s)) continue;
                total += SeatPowerAt(s, beat);
                players++;

                //Culture Count : a culture counts once, the first time it turns up on this beat
                bool seenBefore = false;
                for (int t = 0; t < s; t++)
                    if (Plays(t) && f.Seated[t].Culture == f.Seated[s].Culture) seenBefore = true;
                if (!seenBefore) cultures++;
            }
            if (players == 0) return 0;

            //Harmony : every extra player on the same beat adds a bonus, up to HarmonyMost players
            float perExtra = BattleRules.HarmonyPerExtra;
            if (Run.Has(MotifId.Tutti)) perExtra += BattleRules.TuttiPerExtra;         // TUTTI
            float harmony = 1f + perExtra * (Math.Min(players, BattleRules.HarmonyMost) - 1);

            //EVERY ROAD HOME : mixed cultures on one beat
            if (Run.Conductor.Perk == ConductorPerk.EveryRoadHome)
                harmony += BattleRules.CrossCultureHarmony * (cultures - 1);

            //THE BARGAIN : the devil's deal, for this one round
            if (BargainActive) total *= BattleRules.BargainPower;

            return (int)Math.Round(total * harmony * Run.PowerMultiplier);
        }

        //Enemy Strike : TACET's written power on a beat, before its f / mf / p.
        //A roll and a held note hit harder than a plain note.
        public int EnemyStrikeAt(int beat)
        {
            int power = EnemyPower[beat];
            if (beat == TremoloBeat) power = (int)Math.Round(power * BattleRules.TremoloEnemy);     // TREMOLO
            if (beat == FermataBeat) power = (int)Math.Round(power * BattleRules.FermataEnemy);     // FERMATA
            return power;
        }

        //Is Silent : TACET plays nothing on this beat. The baton may let it pass for a rest, or
        //swing anyway for a free hit. A hidden ??? beat is never taken as silent.
        public bool IsSilent(int beat)
        {
            return EnemyPower[beat] <= 0 && !EnemyHidden[beat];
        }

        //SOFT REST : on a silent beat a small stroke is still a rest, the same as letting the beat
        //pass. Players who keep conducting through the silence breathe too (round 12.1, the habit
        //of stroking every beat used to spend the rest). Only a middle or big stroke there is a
        //free hit.
        public bool RestsOn(int beat, Choice drawn)
        {
            return IsSilent(beat) && drawn == Choice.Ease;
        }

        //Silent Recover : what a rest gives back
        public int SilentRecover
        {
            get
            {
                float recover = BattleRules.RestRecover;
                if (Run.Has(MotifId.ReedCase) && SectionPlays(Family.Wind)) recover += BattleRules.ReedCaseRest;   // REED CASE
                if (Run.Has(MotifId.BreathMark)) recover *= BattleRules.BreathMarkRecover;   // BREATH MARK
                if (EnemyHas(EnemyTrait.NoRest)) recover *= BattleRules.NoRestShare;       // NO REST
                return (int)recover;
            }
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

        //Section Plays : somebody of this family is on stage and able to play
        public bool SectionPlays(Family family)
        {
            for (int s = 0; s < StageLayout.SeatCount; s++)
                if (CanPlay(s) && Run.Formation.Seated[s].Family == family) return true;
            return false;
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

        //Notes Add : a PERFECT beat gives one note to each family that came in on it (round 13, a
        //GOOD one did too, and the signature came round two or three times a fight). A beat
        //played under the signature gives none, and neither does any beat while it runs.
        //BY THE BOOK : THE APPRENTICE's GOOD beats still give notes.
        public void AddNotes(BeatResult r)
        {
            bool counts = r.Grade == Grade.Perfect;
            if (r.Grade == Grade.Good && Run.Conductor.Perk == ConductorPerk.ByTheBook) counts = true;   // BY THE BOOK
            if (!counts || r.Signature || SignatureLeft > 0) return;
            Formation formation = Run.Formation;
            for (int f = 0; f < Notes.Length; f++)
            {
                bool played = false;
                for (int s = 0; s < StageLayout.SeatCount; s++)
                    if (r.Joined[s] && (int)formation.Seated[s].Family == f) played = true;

                if (played && Notes[f] < NeedFor(f)) Notes[f]++;
            }
        }

        //Notes Spend : the signature uses them all up
        public void SpendNotes()
        {
            for (int f = 0; f < Notes.Length; f++) Notes[f] = 0;
        }

        //Joined Share : how much of this beat's sound comes from players with this trait who came in, 0 to 1
        private float JoinedShare(BeatResult r, MusicianTrait trait, int beat)
        {
            Formation f = Run.Formation;
            float all = 0f;
            float part = 0f;
            for (int s = 0; s < StageLayout.SeatCount; s++)
            {
                if (!r.Joined[s]) continue;
                float p = SeatPowerAt(s, beat);
                all += p;
                if (f.Seated[s].Trait == trait) part += p;
            }
            return all > 0f ? part / all : 0f;
        }

        //Joined Has : somebody with this trait came in on the beat
        public bool JoinedHas(BeatResult r, MusicianTrait trait)
        {
            for (int s = 0; s < StageLayout.SeatCount; s++)
                if (r.Joined[s] && Run.Formation.Seated[s].Trait == trait) return true;
            return false;
        }

        //Mark Trait : flag every seat with this trait that came in on the beat
        private void MarkTrait(BeatResult r, MusicianTrait trait)
        {
            for (int s = 0; s < StageLayout.SeatCount; s++)
                if (r.Joined[s] && Run.Formation.Seated[s].Trait == trait) r.TraitFired[s] = true;
        }

        //Beat Resolve : the heart of the fight. Applies the stroke's size against the note's mark,
        //the timing grade, the combo, the traits, breath, and moves the line. Returns what happened
        //so the page can show it. choice is the size the hand drew.
        //Grade None means the baton let the beat pass on purpose (only offered when TACET is silent),
        //Hesitate that one of TACET's notes went by without a stroke. Either way nobody plays.
        public BeatResult Resolve(int beat, Choice choice, Grade grade)
        {
            BeatResult r = Results[beat];
            if (grade != Grade.None && grade != Grade.Hesitate && RestsOn(beat, choice))
                grade = Grade.None;                                                     // SOFT REST
            bool stroked = grade != Grade.None && grade != Grade.Hesitate;

            //SIGNATURE : this stroke is one of the few the conductor's move lasts for
            bool underSignature = stroked && SignatureLeft > 0;
            SignatureMove move = Run.Conductor.Move;

            //Bar Start : FOUR BARS counts the beats of every bar afresh
            if (beat % 4 == 0)
                for (int s = 0; s < StageLayout.SeatCount; s++) barCount[s] = 0;

            //The Band : everybody on stage plays a note the baton lands (see Plays)
            for (int s = 0; s < StageLayout.SeatCount; s++)
            {
                r.Joined[s] = stroked && Plays(s);
                r.TraitFired[s] = r.Joined[s] && TraitFires(s, beat);
            }

            int basePower = stroked ? PowerFor(beat) : 0;
            float recover = 0f;
            float breathLost = 0f;
            Choice enemyChoice = EnemyChoice[beat];

            //THE NOTE SAYS : the stroke should be the size the note's mark asks for. The wrong
            //size still plays, at WrongSizePower (LOCKED TEMPO and ABSOLUTE PITCH are never wrong).
            bool sizeRight = !stroked || SizeRight(beat, choice);
            r.WrongSize = !sizeRight;

            //HELD NOTE : nobody came in, but the note from the beat before is still ringing
            r.HeldSeat = -1;
            if (basePower == 0)
            {
                r.HeldSeat = HeldNoteSeat();
                if (r.HeldSeat >= 0)
                {
                    basePower = (int)Math.Round(SeatPowerAt(r.HeldSeat, beat) * BattleRules.HeldNoteShare * Run.PowerMultiplier);
                    r.TraitFired[r.HeldSeat] = true;
                }
            }
            r.BasePower = basePower;
            float power = basePower;

            //ONE STEP BETTER : her part swells against TACET's loud note
            if (enemyChoice == Choice.Boost)
            {
                float share = JoinedShare(r, MusicianTrait.OneStepBetter, beat);
                if (share > 0f)
                {
                    power *= 1f + (BattleRules.OneStepBetterPower - 1f) * share;
                    MarkTrait(r, MusicianTrait.OneStepBetter);
                }
            }

            //Stroke Size
            r.Rested = false;
            float easeBack = 0f;
            if (!stroked)
            {
                //No Stroke : TACET is silent and the baton let the beat pass, the band rests.
                //On one of TACET's notes nobody answers, and nobody breathes either.
                //A HELD NOTE rings on through a rest for free, the rest still breathes.
                if (IsSilent(beat))
                {
                    recover += SilentRecover;                                          // BREATH MARK, REED CASE, NO REST inside
                    r.Rested = true;
                }
            }
            else if (!sizeRight)
            {
                //Wrong Size : the band plays, but not the way the note is written
                power *= BattleRules.WrongSizePower;
            }
            else if (choice == Choice.Boost && Run.Has(MotifId.Sforzando))
            {
                //SFORZANDO : a big stroke on a note that asks for it hits harder
                power *= BattleRules.SforzandoPower;
            }
            else if (choice == Choice.Ease && Run.Has(MotifId.Pianissimo))
            {
                //PIANISSIMO : a small stroke on a note that asks for it gives breath back (NO REST halves it).
                //Added after the grade, so the card's number is exact.
                easeBack = BattleRules.PianissimoRecover;
                if (EnemyHas(EnemyTrait.NoRest)) easeBack *= BattleRules.NoRestShare;   // NO REST
            }

            //TREMOLO : TACET's roll is answered by many strokes instead of one, by the whole band.
            //Every stroke adds a little, up to a limit. Shaking is free, so shake hard.
            r.Tremolo = beat == TremoloBeat;
            r.RollStrokes = 0;
            if (r.Tremolo)
            {
                int counted = RollCounted(RollStrokes);                                // ACCELERANDO inside
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
                power *= BattleRules.FermataBase + BattleRules.FermataHold * r.Held;
                HoldFraction = 0f;
            }

            //TENUTO : a held note is a breath. Holding it to the end gives stamina back, holding
            //half of it gives half. Added after the timing grade so the card's number is exact.
            float tenutoBack = r.Fermata && Run.Has(MotifId.Tenuto) ? BattleRules.TenutoRecover * r.Held : 0f;

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
                if (basePower > 0 && !Run.Has(MotifId.Rubato))                         // RUBATO skips the extra breath
                    breathLost += BattleRules.MissBreath;
            }
            else if (grade == Grade.Hesitate)
            {
                Combo = 0;                                                             // nobody played, see above
            }
            recover += tenutoBack + easeBack;                                          // TENUTO, PIANISSIMO

            //VILLAGE BAND : under THE FOLK LEADER's signature every stroke on time gives breath back
            if (underSignature && move == SignatureMove.VillageBand && (grade == Grade.Perfect || grade == Grade.Good))
                recover += BattleRules.VillageBandRecover;

            if (Combo > BestCombo) BestCombo = Combo;
            r.Combo = Combo;
            r.ComboBroken = comboBefore >= 2 && Combo == 0;

            //Combo Bonus
            power *= ComboBonus;

            //FORTISSIMO : the band is on fire for a few beats after a long combo, and hits harder
            r.Fortissimo = false;
            if (FortissimoLeft > 0 && basePower > 0)
            {
                power *= BattleRules.FortissimoPower;
                r.Fortissimo = true;
            }
            if (FortissimoLeft > 0 && grade != Grade.None) FortissimoLeft--;
            UpdateFortissimo(r, grade);

            //SET ALIGHT : under THE INFERNO's signature every stroke burns hotter (and pushes further, below)
            if (underSignature && move == SignatureMove.SetAlight) power *= BattleRules.SetAlightPower;

            //RUNAWAY FIRE : the first beat we play after a miss burns hotter
            r.Fired = false;
            if (fireNext && basePower > 0 && !missedNow)
            {
                power *= BattleRules.RunawayFireBonus;
                fireNext = false;
                r.Fired = true;
            }
            if (missedNow && runaway) fireNext = true;

            //OVERTURE : the first beat of each round, the first time through only
            if (beat == 0 && Pass == 0 && Run.Has(MotifId.Overture)) power *= BattleRules.OverturePower;

            //THE CLOSER THE LOUDER : the less breath the band has left, the harder it hits
            power *= 1f + LouderBonus;

            //Enemy Choice : decided at the start of the round, shown in its call, applied now
            float enemyPower = EnemyStrikeAt(beat);
            if (enemyChoice == Choice.Boost) enemyPower *= BattleRules.EnemyBoostPower;
            if (enemyChoice == Choice.Ease) enemyPower *= BattleRules.EnemyEasePower;

            //FILLS THE GAPS : a note let pass or MISSED lets it in harder (round 15, it was a small
            //stroke or none, and a small stroke is what a p note asks for now)
            if (EnemyHas(EnemyTrait.FillsGaps) && (!stroked || grade == Grade.Miss))
                enemyPower *= BattleRules.FillsGapsPower;

            //COUNTERPOINT : answering a boost
            if (enemyChoice == Choice.Boost && Run.Has(MotifId.Counterpoint))
                power *= BattleRules.CounterpointPower;

            //IN TUNE : a stroke on time the same size as the mark TACET really plays takes the edge
            //off its note. A FALSE NOTE's shown mark does not count, a hidden note has none, and a
            //roll is the whole band whatever TACET plays. ABSOLUTE PITCH makes any size on time IN TUNE.
            //LOCKED TEMPO is never the wrong size, but only a stroke that really matches is IN TUNE.
            //COUNTER : a PERFECT big stroke against a real f note knocks more of it back.
            //MARCATO lets a COUNTER push further.
            r.Counter = false;
            r.InTune = false;
            bool onTime = grade == Grade.Perfect || grade == Grade.Good;
            bool matched = choice == enemyChoice && !EnemyHidden[beat];
            if (underSignature && move == SignatureMove.AbsolutePitch) matched = true;     // ABSOLUTE PITCH
            if (onTime && matched && basePower > 0 && enemyPower > 0 && !r.Tremolo)
            {
                r.InTune = true;
                r.Counter = choice == Choice.Boost && enemyChoice == Choice.Boost && !EnemyHidden[beat] && grade == Grade.Perfect;
                enemyPower *= r.Counter ? BattleRules.CounterKeep : BattleRules.InTuneKeep;
            }

            //Clash : the difference pushes the line
            r.OurPower = (int)Math.Round(power);
            r.EnemyPower = (int)Math.Round(enemyPower);
            r.Played = r.OurPower > 0;
            float cap = r.Counter && Run.Has(MotifId.Marcato) ? BattleRules.MarcatoCap : BattleRules.PushCap;   // MARCATO
            if (underSignature && move == SignatureMove.SetAlight) cap = Math.Max(cap, BattleRules.SetAlightCap);  // SET ALIGHT
            float push = PushFor(r.OurPower, r.EnemyPower, cap);                        // PUSH CAP inside

            Line += push;
            if (Line > BattleRules.LineLimit) Line = BattleRules.LineLimit;
            if (Line < -BattleRules.LineLimit) Line = -BattleRules.LineLimit;

            //THE WHOLE FLOOR : his big stroke, on a note that asks for one, knocks TACET's next note down
            if (choice == Choice.Boost && sizeRight && beat + 1 < BattleRules.BeatsPerRound && EnemyPower[beat + 1] > 0
                && JoinedHas(r, MusicianTrait.Thunder))
            {
                EnemyPower[beat + 1] = Math.Max(0, EnemyPower[beat + 1] - BattleRules.ThunderKnock);
                MarkTrait(r, MusicianTrait.Thunder);
            }

            //TACET'S BLOW : a beat TACET wins hits the band for whatever got through.
            //DEAF EARS : under THE UNHEARING's signature it takes no breath at all.
            r.Blow = BlowFor(r.OurPower, r.EnemyPower);
            r.BlowIgnored = 0;
            if (underSignature && move == SignatureMove.DeafEars)
            {
                r.BlowIgnored = r.Blow;
                r.Blow = 0;
            }

            //Breath : rests and motifs give some back, a blow and a MISS take some away. Playing is free.
            r.StaminaChange = (int)Math.Round(recover - breathLost) - r.Blow;
            Run.ChangeStamina(r.StaminaChange);

            //Seat Memory : who played this beat, for the traits that count runs of beats
            for (int s = 0; s < StageLayout.SeatCount; s++)
            {
                playedLast[s] = r.Joined[s];
                if (r.Joined[s]) { streak[s]++; barCount[s]++; }
                else streak[s] = 0;
            }

            r.Push = (int)Math.Round(push);
            r.PlayerChoice = choice;
            r.EnemyChoice = enemyChoice;
            r.Grade = grade;
            r.Double = EnemyDouble[beat];
            r.GraceGrade = Grade.None;
            r.GracePush = 0;

            //SIGNATURE : one of its strokes is used up
            r.Signature = underSignature;
            if (underSignature) SignatureLeft--;

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
            return PushFor(ours, theirs, BattleRules.PushCap);
        }

        //Push For, with its own cap : a MARCATO COUNTER may push past the usual PUSH CAP
        private float PushFor(int ours, int theirs, float cap)
        {
            float push = (ours - theirs) * BattleRules.PushPerPower;
            push = MathHelper.Clamp(push, -cap, cap);                                   // PUSH CAP
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

            //SECOND WIND : the first time the band would collapse, some breath comes back (SecondWindRefill)
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

            //CLOCKWORK : the flick back of a signature stroke, on time, is PERFECT too
            if (r.Signature && Run.Conductor.Move == SignatureMove.Clockwork && grade == Grade.Good)
                grade = Grade.Perfect;

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

            //GRACE NOTE : a spark that lands on time counts three times
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

            //TACET'S BLOW : its second note gets through too (DEAF EARS : not under the signature)
            r.Blow = BlowFor(ourPart, theirPart);
            r.BlowIgnored = 0;
            if (r.Signature && Run.Conductor.Move == SignatureMove.DeafEars)
            {
                r.BlowIgnored = r.Blow;
                r.Blow = 0;
            }
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

            //ANSWER BETTER remembers what the band played, the last time through
            for (int b = 0; b < BattleRules.BeatsPerRound; b++)
                lastRound[b] = Results[b].BasePower;

            songStart += Passes;                                                        // SONG : the next round goes on with the next phrase
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
    }
}
