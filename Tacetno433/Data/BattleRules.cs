namespace Tacetno433.Data
{
    //BattleRules : EVERY BALANCE NUMBER IN ONE PLACE.
    //Change a value here and the whole combat system follows. Nothing else in the code
    //hides its own tuning numbers.
    public static class BattleRules
    {
        //Battle Shape
        public const int BeatsPerRound = 8;       // sub-turns in one round
        public const int MaxRounds = 3;           // after this, whoever is ahead wins
        public const float LineLimit = 100f;      // push the line this far to win outright

        //REPEATS (round 11) : TACET's phrase is played more than once before the band goes back to
        //the stage page, without a stop. TACET plays the same beats each time, but decides its
        //f / mf / p and its pairs afresh for every time through, so they must be read again.
        //Players found a round over in seconds. Round 12 : kept as the length of a round.
        public static int[] PassesPerRound = { 1, 2, 3 };   // round 1 once, round 2 twice, round 3 three times
        public const int PassesMost = 3;                    // no entry above may be bigger than this

        //Push : how far the line moves for each point of power difference on a beat.
        //Round 9 (25 Sep) : playtesters won by putting the whole band on the first four beats,
        //the fight was over in two to four beats. The push is much lighter now, so a duel
        //takes a normal enemy about a round and a half and a boss two to three rounds.
        public static float PushPerPower = 1.0f;   // was 2.2

        //PUSH CAP : no single note moves the line further than this, either way. No fight is
        //won or lost in one blow, however strong the band has grown (see BattleState.PushFor).
        //So a beat won by a mile pushes no further than a beat won well: spread the band out.
        //For a player who wins every beat, the cap is what decides how long a duel lasts.
        //Round 11 (REPEATS) : 10 -> 8, so a good player needs about two plans for an ordinary
        //enemy (16 beats), two to three for an elite (23) and three for a boss (31).
        public static float PushCap = 8f;

        //HEAVY LINE : elites and bosses are heavier to move. Every note in their duels, ours and
        //theirs, moves the line only this share of the usual push, so those duels last longer.
        public static float EliteLine = 0.7f;      // round 11, was 0.85
        public static float BossLine = 0.6f;       // round 11, was 0.7

        //Harmony : several musicians on the same beat hit harder together.
        //Each extra player adds this much, so three players give x1.30.
        public static float HarmonyPerExtra = 0.15f;

        //DYNAMICS (round 12) : there is no score page. How big a stroke is decides who plays:
        //   small  (p)   the back row
        //   middle (mf)  the back and middle rows
        //   big    (f)   the whole band
        //A row with nobody in it is skipped, so every stroke brings somebody in (BattleState.Joins).
        //Whoever plays pays stamina for their note, so a big stroke is strong and tiring and a small
        //one is cheap. Only a rest gives breath back. The players who come in ARE the power: the
        //size itself multiplies nothing any more (round 8 to 11 : small x0.6, big x1.5).
        //The numbers below stay here as tuning knobs.
        public static float BoostPower = 1.0f;     // a big stroke, on top of the whole band coming in
        public static float EasePower = 1.0f;      // a small stroke, the back row at full strength
        public static float EaseCost = 1.0f;       // a small stroke pays this share of its notes
        public static int EaseRecover = 0;         // breath a small stroke gives back (was 8). At 6, small strokes
                                                   // on every beat won 90 percent of simulated fights (round 12)

        //CUE (round 12) : the baton points at one side of the stage on every beat. Down and up point
        //at the centre, left and right at their own side. Whoever sits on that side and plays hits
        //harder. The sides are the stage's columns (StageLayout.SeatSide).
        public static float CuePower = 1.5f;
        public static int[] CueSide = { 1, 0, 2, 1 };      // beat of the bar -> side : 0 left, 1 centre, 2 right

        //Rest : a beat where TACET is silent and the baton lets it pass gives stamina back.
        //Round 11 : 6 -> 10, the player found rests gave back far too little to be worth planning.
        //Round 12 : 12, a rest is now the only breath there is inside a fight.
        public static int RestRecover = 12;

        //Stamina Cost : every player who comes in costs their written cost times this (0.72 = 28 percent less)
        //Round 9 : duels last two to three times as many beats, so each note costs less.
        //Round 12 : 0.72, so a big stroke on every note runs out of breath and reading the marks pays.
        public static float StaminaCostScale = 0.72f;   // was 1.15, 0.7 in rounds 9 to 11

        //Stamina Pressure (round 8) : stamina is the band's breath. It is not spent on big
        //strokes, rolls or holds any more. It is knocked out of the band instead:
        //   TACET'S BLOW  a beat TACET wins hits the band for what got through
        //   a MISS costs MissExtraCost on top of the note
        //   COLLAPSE      breath at zero ends the fight, the band is silenced (SECOND WIND saves it once)
        public static float BlowPerPower = 0.75f;   // stamina lost per point TACET wins a beat by, was 1.0, 0.7 in round 9
                                                    // round 10 : 0.63 kept the whole game as hard as round 9 after BREATH MARK was cut
                                                    // round 11a : 0.68 made up for the softer WHITE NOISE
                                                    // round 11b : 0.78 with the bigger rests (the player allowed it a little easier)
                                                    // round 12 : 0.75 with the marks wider apart (EnemyBoostPower 2),
                                                    // a misread f hurts, which is what sets careful players apart

        //Timing Grades (HARD, 25 Sep : players found the wide windows far too easy)
        //The stroke settings page can move every judgement earlier or later for one player's
        //hand and screen (Settings.TimingOffset), so the windows can stay narrow.
        public static float PerfectWindow = 0.08f; // seconds either side of the beat, was 0.12
        public static float GoodWindow = 0.18f;    // was 0.30
        public static float EarlyTime = 0.24f;     // a stroke ending earlier than this before its beat is the hand getting ready
        public static float PerfectBonus = 1.3f;   // a PERFECT stroke multiplies the band's power (round 12, was 1.2)
        public static float MissPower = 0.45f;     // PENALTY : a bad stroke still counts, but weaker, was 0.7, 0.55 until round 12
        public static int MissExtraCost = 4;       // PENALTY : and costs extra stamina
        //No stroke on one of TACET's notes (HESITATE) : nobody comes in, so nobody pays, and the
        //note lands whole. Round 12 : the band used to play its planned part at 30 percent.

        //Signature : PERFECT and GOOD beats collect notes for the instrument families that
        //played them. When the conductor's recipe is complete (see Conductor.Recipe), SPACE lets
        //the signature loose: the next stroke is a PERFECT BOOST that hits harder still.
        public static float SignaturePower = 1.5f;      // on top of everything else on that beat

        //Tempo : beats per minute in round 1, 2 and 3. The duel speeds up as the fight goes on.
        //TACET's second bar is called while the first is being answered, so the player
        //conducts on every beat without waiting.
        public static int[] TempoBpm = { 96, 112, 128 };   // round 9 : quicker strokes, was 88 / 100 / 116
        public static float PhraseTail = 0.5f;      // after the last answer, before the round is summed up

        //Teaching Order : the special notes arrive one floor at a time, so floor one teaches the
        //plain game with the roll (the enemy traits also start on floor two). Keep it simple to learn.
        //Round 9 : every round ends on a special note. Whoever does not HOLD it ROLLS it, so on
        //floor one every enemy rolls, and from floor two ordinary enemies hold instead.
        public static int TremoloFromFloor = 1;     // rolls from the start, shaking is easy
        public static int FermataFromFloor = 2;     // ordinary enemies hold their last note from floor two
        public static int PairsFromFloor = 2;       // round 9 : pairs from floor two for quicker flicks, was 3

        //Double Notes : some of TACET's notes come in pairs, a note tied to a spark. The note is
        //answered on the beat, the spark half a beat later with one more flick, any way.
        public static int[] DoubleNotes = { 2, 3, 4 };     // how many pairs in round 1 / 2 / 3, from PairsFromFloor on, was 1 / 2 / 3
        public static int DoubleMost = 4;
        public static float GraceShare = 0.5f;      // the second note is worth this much of its beat, on both sides

        //Tremolo : elites and bosses (and everyone on floor one) end every round with a roll two
        //beats long. Shake the baton as fast as you can: every stroke adds power (the strokes are
        //free since round 8).
        public static float TremoloBeats = 2f;
        public static float TremoloEnemy = 1.5f;    // TACET's roll hits this much harder than its note
        public static float TremoloBase = 0.4f;     // our part with no strokes at all
        public static float TremoloStep = 0.15f;    // each stroke adds this much of our part
        public static int TremoloMost = 10;         // strokes past this add nothing
        public static int TremoloPerfect = 6;       // strokes for a PERFECT roll, was 7 : the roll is shorter at 128 BPM
        public static int TremoloGood = 4;          // strokes for a GOOD roll, fewer than 1 is a HESITATE

        //Fermata : ordinary enemies end every round with a held note two beats long (elites and
        //bosses end theirs with the tremolo instead). Stroke it on the beat, then keep the button
        //held and the baton still. The longer it is held the harder it pushes (holding is free
        //since round 8, the test is keeping the hand still under pressure).
        public static float FermataBeats = 2f;
        public static float FermataEnemy = 1.3f;    // TACET's held note hits this much harder than its note
        public static float FermataBase = 0.6f;     // our part when it is let go at once
        public static float FermataHold = 0.8f;     // added over the whole hold, so holding to the end is x1.4
        public static float FermataStill = 260f;    // pixels per second the baton may drift and still be still

        //IN TUNE (round 12) : a stroke on time (PERFECT or GOOD) whose size matches the mark TACET
        //really plays, p small, mf middle, f big, takes the edge off its note. Reading the marks is
        //the heart of the duel: an over-sized answer only wastes breath, an under-sized one loses.
        //A FALSE NOTE's shown mark does not count, and a hidden ??? note has none to match.
        public static float InTuneKeep = 0.6f;      // TACET keeps only this much of the note

        //Counter : a PERFECT big stroke against TACET's real f note knocks more of it back
        public static float CounterKeep = 0.7f;     // TACET keeps only this much of the note

        //Fortissimo : a combo this long sets the band on fire for the next few beats
        public static int FortissimoCombo = 8;
        public static int FortissimoBeats = 4;
        public static float FortissimoPower = 1.3f;
        //On fire the band does not tire : stamina paid on a burning beat is multiplied by this.
        //25 Sep : friends who played said stamina cuts the best moments short, so the peak of
        //a fight is free. Only a long run of PERFECTs earns it. Set to 1 for the old rule.
        public static float FortissimoCost = 0f;

        //Finale : once the line is this far our way at the end of a round, the band may try to
        //finish the piece. Four strokes of the 4/4 pattern on the beat end the fight at once.
        public static float FinaleLine = 80f;
        public static float FinaleFailPush = 12f;   // TACET claws back this much when the finale falls apart
        public static int FinaleShards = 25;        // extra reward for ending it this way

        //Clash Feel (seconds)
        public static float CutInTime = 1.0f;       // the signature picture across the screen
        public static float HitStopTime = 0.14f;    // the picture freezes at the end of every bar
        public static float ClashCountTime = 0.16f; // the two numbers count up for this long
        public static float ClashShowTime = 0.55f;  // and stay on screen for this long

        //Enemy Traits : the numbers behind each EnemyTrait (see Data/Enemy.cs)
        public static int LullabyBpm = 88;          // THE LULL, round three slows down, was 80 (kept a third under round three)
        public static int UnfinishedBpm = 140;      // REQUIEM, round three rushes, was 128 (kept above round three)
        public static float FillsGapsPower = 1.5f;  // DEAD AIR, answered with a small stroke or none (round 12, was: beats nobody planned)
        public static float NoRestShare = 0.5f;     // WHITE NOISE, silent beats give back only this share (round 11, was nothing)
        public static float MirrorScale = 0.35f;    // THE NAMELESS MASTER, your last round played back. Round 12 : 0.9 -> 0.35,
                                                    // a round holds whole-band strokes now, not a planned few
        public static float BargainPower = 1.5f;    // THE DEVIL'S STRING, the deal : this much power
        public static int BargainStamina = 15;      //     for this much max stamina, for the whole run

        //Musician Traits : the numbers behind each MusicianTrait (see Data/Musician.cs)
        public static float KeepsCountWindow = 0.05f;  // ANNA, added to the PERFECT window on beats cued to her side
        public static int MomentumStep = 1;            // OTTO, per beat in a row
        public static int MomentumMax = 3;
        public static float ByEarPower = 1.5f;         // MALI, on hidden beats
        public static float HeldNoteShare = 0.4f;      // CHAI, how much of his note rings on
        public static float OneStepBetterPower = 1.3f; // NUAN, against a loud note
        public static float FourBarsPower = 2f;        // LUKA, the fourth beat of a full bar
        public static int ThunderKnock = 2;            // BORIS, taken off TACET's next note

        //Timing Lengths (seconds). The beats themselves follow TempoBpm above.
        public static float IntroTime = 1.1f;      // round banner
        public static float LateTime = 0.35f;      // the latest a stroke can land after its beat
        public static float RoundEndTime = 1.4f;

        //Enemy Choices : what TACET does on a beat, depending on how heavy that beat is.
        //Each row is  boost chance, ease chance  in percent. Normal is whatever is left.
        public static int HeavyBoostChance = 60, HeavyEaseChance = 10;
        public static int LightBoostChance = 15, LightEaseChance = 40;
        public static float HeavyThreshold = 0.6f; // a beat at 60 percent of its strongest counts as heavy

        //Round 12 : the marks are wider apart, so they match the band's rows. A small stroke (the back
        //row) can answer p, a big one (the whole band) is needed for f. Was 1.5 and 0.6.
        public static float EnemyBoostPower = 2.0f;
        public static float EnemyEasePower = 0.5f;

        //Enemy Scaling
        public static float FloorScale = 0.25f;    // each floor below the first adds 25 percent
        //Round 8 : BOOST costs nothing extra, so a big stroke on every beat is the normal way to
        //play. TACET hits about 1.5 times harder to match, which kept the simulated win rates
        //of ordinary players where they were (see PROJECT_STATUS section 8).
        //Round 11 : ordinary enemies never won a single fight in the simulation, they only cost
        //breath. They hit harder now, so a band that arrives tired can lose to them.
        //Round 12 : lower again, TACET's f doubles its note now and the band pays for every player.
        public static float NormalScale = 2.2f;   // ordinary encounters, 3.1 in round 11 (2.0 in round 8, 1.35 before)
        public static float EliteScale = 1.4f;    // was 2.0 (1.35 before round 8)
        public static float BossScale = 1.8f;     // was 2.8 (1.85 before round 8)

        //Conductor Stats : the 0 to 10 numbers on the select page become real values here
        public static int StaminaBase = 60;
        public static int StaminaPerPoint = 8;     // stamina 5  ->  60 + 40 = 100
        public static float PowerBase = 0.8f;
        public static float PowerPerPoint = 0.04f; // push 5  ->  0.8 + 0.2 = x1.0

        //Between Battles
        public static float RecoverAfterWin = 0.3f;  // a won battle gives back 30 percent of max stamina

        //Rewards
        public static int ShardsNormal = 20;
        public static int ShardsElite = 40;
        public static int ShardsBoss = 80;
        public static int ShardsPerPerfect = 1;      // was 2 : duels are longer, so there are more PERFECTs (round 9)
        public static int EliteRecruitChance = 50;   // percent

        //Party
        public static int BenchSize = 3;             // musicians you can keep off stage
        public static int FirstFloorRecruits = 2;    // how many join at the very first opening
        public static int LaterFloorRecruits = 1;

        //Run Length : beating the boss of the last floor wins the whole run
        public static int FloorsPerRun = 3;

        //Combo : every PERFECT in a row adds power, a MISS or no stroke breaks it.
        //GOOD keeps the combo but does not add to it.
        public static float ComboStep = 0.06f;       // +6 percent power per step
        public static int ComboMax = 5;              // so the most it gives is +30 percent

        //Motif Rewards
        public static int MotifChoices = 3;          // pick one of this many after a win
        public static int MotifChanceNormal = 60;    // percent, after a normal fight. Elites and bosses always offer.
        public static int SkipMotifShards = 15;      // taking nothing pays this instead
        public static int[] MotifPrice = { 0, 45, 70, 100 };   // shop price by rarity 1, 2, 3

        //Motif Effects : the numbers each motif changes
        public static int FamilyMotifPower = 1;      // ROSIN, SPARE STICKS
        public static int ReedCaseDiscount = 1;      // REED CASE, 2 in round 11, back to 1 (round 12 : +10 to +13 percent alone)
        public static int PianissimoRecover = 3;     // PIANISSIMO, a small stroke gives this back (was 6 on top of EASE's 8)
        public static float PurseBonus = 1.3f;       // PATRON'S PURSE
        public static float BreathMarkRecover = 1.25f; // BREATH MARK : 12 x 1.25 = 15 (round 12, 10 -> 12.5 in round 11). Was 2 (x2), round 10 :
                                                        // one card alone lifted a run from 8 to 46 percent in the simulation
        public static float TuttiPerExtra = 0.10f;   // TUTTI
        public static float SteadyWindowBonus = 0.05f;   // STEADY PULSE, added to both windows
        public static float EncoreRecover = 0.15f;   // ENCORE
        public static float RubatoMissPower = 0.85f; // RUBATO
        public static float SforzandoPower = 1.3f;   // SFORZANDO, a big stroke (round 12 : 1.8 against BOOST's old x1.5, now against x1)
        public static float SecondWindRefill = 0.25f;// SECOND WIND, was 0.33 (round 12 : +34 percent alone, the most of any motif)
        public static float OverturePower = 1.5f;    // OVERTURE
        public static float CounterpointPower = 1.3f;// COUNTERPOINT

        //Motifs For The Special Notes (25 Sep). The card texts in Data/Motif.cs say these numbers.
        //Round 11 : the PUSH CAP (round 9) meant extra power on a beat already won by a mile did
        //nothing, so ACCELERANDO, TENUTO and MARCATO were changed and GRACE NOTE made stronger.
        public static int AccelerandoCount = 2;      // ACCELERANDO, every TREMOLO shake counts this many (was: 14 shakes counted, nobody shakes that fast)
        public static int TenutoRecover = 8;         // TENUTO, stamina back for a FERMATA held to the end (was: the hold hit x1.8)
        public static float GraceNotePower = 3f;     // GRACE NOTE, a spark that lands counts three times, was 2
        public static float MarcatoCap = 16f;        // MARCATO, a COUNTER may push this far, twice the PUSH CAP (was: TACET kept 55 percent)
        public static int ConBrioCombo = 6;          // CON BRIO, FORTISSIMO after this many PERFECTs instead of 8
        public static float CodaLine = 60f;          // CODA, the FINALE is offered from here instead of FinaleLine

        //Conductor Perks
        public static float LockedTempoCost = 1.25f; // THE METRONOME pays this share. Was 0.6 : since BOOST is free (round 8)
                                                     // his lock cost him nothing, and he won 98 percent of simulated runs (round 10).
                                                     // Round 12 : 1.25, the whole band on every stroke without reading any
                                                     // mark won 74 percent at 1.0, the others 50 to 64
        public static float RunawayFireBonus = 1.3f; // THE INFERNO, the beat after a miss
        public static float CloserLouderMax = 1.3f;  // THE UNHEARING, +130 percent at the very edge. Was 0.6 : with the PUSH CAP
                                                     // the line rarely falls far, so he was the weakest conductor (round 10)
        public static float CrossCultureHarmony = 0.07f; // THE FOLK LEADER, per extra culture on a beat. Was 0.15 : he won the most runs (round 11)

        //Shop
        public static int SeatPriceBase = 55;        // the first extra seat
        public static int SeatPriceStep = 25;        // each seat after that costs this much more
        public static int SeatsPerFloor = 2;
        public static int TuningPrice = 30;          // stamina back
        public static float TuningRecover = 0.4f;
        public static int HirePrice = 80;            // a musician from this era

        //Rest
        public static float BreatheRecover = 0.4f;   // free
        public static int DeepRestPrice = 35;        // full stamina
        public static int RehearseMax = 3;           // how many times one musician can train

        //Event Checks
        public static int CheckPerPower = 5;         // each point of power adds this many percent
        public static int CheckFamilyBonus = 25;     // the family the check asks for adds this
    }
}
