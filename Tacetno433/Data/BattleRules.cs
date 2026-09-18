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

        //Push : how far the line moves for each point of power difference on a beat
        public static float PushPerPower = 2.2f;

        //Harmony : several musicians on the same beat hit harder together.
        //Each extra player adds this much, so three players give x1.30.
        public static float HarmonyPerExtra = 0.15f;

        //Player Choices
        public static float BoostPower = 1.5f;     // F : hit harder
        public static float BoostCost = 1.6f;      //     and pay more stamina
        public static float EasePower = 0.6f;      // H : hit softer
        public static float EaseCost = 0.5f;       //     pay less
        public static int EaseRecover = 8;         //     and breathe a little

        //Rest : a beat where we play nothing gives stamina back
        public static int RestRecover = 6;

        //Stamina Cost : every note the band plays costs this much more (1.2 = 20 percent more)
        public static float StaminaCostScale = 1.15f;   // was 1.0

        //Timing Grades
        public static float PerfectWindow = 0.08f; // seconds either side of the ring closing
        public static float GoodWindow = 0.22f;
        public static float PerfectBonus = 1.2f;   // perfect press multiplies the choice
        public static float MissPower = 0.7f;      // PENALTY : a bad press still counts, but weaker
        public static int MissExtraCost = 4;       // PENALTY : and costs extra stamina
        public static float HesitatePower = 0.75f; // PENALTY : never pressing plays normal, weaker

        //Timing Lengths (seconds)
        public static float IntroTime = 1.1f;      // round banner
        public static float IntentTime = 0.55f;    // beat highlighted, enemy weight shown
        public static float RingTime = 1.25f;      // ring shrinking to the mark
        public static float LateTime = 0.35f;      // the ring keeps going a little past the mark
        public static float ClashTime = 1.0f;      // result shown
        public static float RestClashTime = 0.45f; // quieter beats move faster
        public static float RoundEndTime = 1.4f;

        //Enemy Choices : what TACET does on a beat, depending on how heavy that beat is.
        //Each row is  boost chance, ease chance  in percent. Normal is whatever is left.
        public static int HeavyBoostChance = 60, HeavyEaseChance = 10;
        public static int LightBoostChance = 15, LightEaseChance = 40;
        public static float HeavyThreshold = 0.6f; // a beat at 60 percent of its strongest counts as heavy

        public static float EnemyBoostPower = 1.5f;
        public static float EnemyEasePower = 0.6f;

        //Enemy Scaling
        public static float FloorScale = 0.25f;    // each floor below the first adds 25 percent
        public static float NormalScale = 1.15f;  // ordinary encounters, was 1.0
        public static float EliteScale = 1.25f;   // was 1.35, eased because stamina costs more now
        public static float BossScale = 1.75f;    // was 1.7

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
        public static int ShardsPerPerfect = 2;
        public static int EliteRecruitChance = 50;   // percent

        //Party
        public static int BenchSize = 3;             // musicians you can keep off stage
        public static int FirstFloorRecruits = 2;    // how many join at the very first opening
        public static int LaterFloorRecruits = 1;

        //Run Length : beating the boss of the last floor wins the whole run
        public static int FloorsPerRun = 3;

        //Combo : every PERFECT in a row adds power, a MISS or no press breaks it.
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
        public static int ReedCaseDiscount = 1;      // REED CASE
        public static int PianissimoRecover = 6;     // PIANISSIMO
        public static float PurseBonus = 1.3f;       // PATRON'S PURSE
        public static float FermataRecover = 2f;     // FERMATA
        public static float TuttiPerExtra = 0.10f;   // TUTTI
        public static float SteadyWindowBonus = 0.05f;   // STEADY PULSE, added to both windows
        public static float EncoreRecover = 0.15f;   // ENCORE
        public static float RubatoMissPower = 0.85f; // RUBATO
        public static float SforzandoPower = 1.8f;   // SFORZANDO
        public static float SecondWindRefill = 0.33f;// SECOND WIND
        public static float OverturePower = 1.5f;    // OVERTURE
        public static float CounterpointPower = 1.3f;// COUNTERPOINT

        //Conductor Perks
        public static float LockedTempoCost = 0.6f;  // THE METRONOME pays 60 percent
        public static float RunawayFireBonus = 1.3f; // THE INFERNO, the beat after a miss
        public static float CloserLouderMax = 0.6f;  // THE UNHEARING, +60 percent at the very edge
        public static float CrossCultureHarmony = 0.15f; // THE FOLK LEADER, per extra culture on a beat

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
