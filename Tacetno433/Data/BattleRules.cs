namespace Tacetno433.Data
{
    //BattleRules : EVERY BALANCE NUMBER IN ONE PLACE.
    //Change a value here and the whole combat system follows. Nothing else in the code
    //hides its own tuning numbers.
    public static class BattleRules
    {
        //TURN BATTLE (Alpha 7, 18 Oct) : the turn based fight that replaces the rhythm duel
        //(docs/ALPHA7_DESIGN.md, sections 5 and 8). Battle/TurnBattle.cs reads every number here.
        //They are FIRST GUESSES that nobody has played yet : tune them with "--shots turncheck"
        //and real play. The rhythm duel numbers further down stay until the old duel is removed.

        //Turn Battle On : fights open the turn battle page. False plays the old rhythm duel again.
        public static bool TurnBattleOn = true;

        //Life Word : the run's old BREATH (stamina) is the band's life in the turn battle, and BREATH
        //is the skill points of one fight. Pages outside the fight say this word for it.
        public static readonly string LifeWord = TurnBattleOn ? "LIFE" : "BREATH";
        public static readonly string LifeWordLower = TurnBattleOn ? "life" : "breath";

        //Life and Speed of the band, by family in the order of the Family enum : strings, winds, percussion.
        //Each round everybody acts once, the fastest first (winds, then strings, then percussion).
        public static int[] TurnHp = { 30, 26, 36 };
        public static int[] TurnSpeed = { 5, 7, 3 };

        //Life and Speed of TACET, by EnemyKind : normal, elite, boss.
        //Speed 4 puts an enemy after the strings and before the percussion.
        public static int[] TurnEnemyHp = { 50, 120, 250 };
        public static int[] TurnEnemySpeed = { 4, 4, 4 };

        //Attack : damage = the musician's power x TurnAttackPerPower, then x how the swipe went
        public static float TurnAttackPerPower = 2f;
        public static float TurnPerfect = 1.3f;     // right way, right size, on time
        public static float TurnGood = 1f;          // right way, right size, a little off time
        public static float TurnWrongSize = 0.7f;   // on time but the wrong size
        public static float TurnMiss = 0.4f;        // the wrong way, too early, too late or no swipe

        //ONE BAR (step 12, 24 Oct, the user played it and chose this) : each round is a BAR. The player gives
        //every musician on the field one move (ATTACK or a skill) in the order they want, then the bar plays
        //as one phrase and TACET answers. ATTACK needs no swipe any more, only skills and parries do.
        //The fight starts with a few musicians, the rest wait on the BENCH and can be CALLED in during it
        //(like Wildfrost, where a new card on the board costs your turn).
        public static float TurnAttackScale = 0.55f;  // ATTACK : no swipe, it always hits at this (step 15 : was 1, the user found ATTACK far too strong and fights too short.
                                                      // turnsim : ATTACK did 68 percent of the band's damage, now about 36, the skills carry the rest)
        public static int TurnAttackOffBeat = 10;     // ATTACK : the OFF-BEAT it adds
        public static int TurnStartBand = 2;          // seats on stage a run starts with (FULL STAGE in the ARCHIVE adds 1, EXTRA SEAT in the shop 1 each).
                                                      // Step 15 : everybody seated on the BAND page starts the fight on the field
        public static int TurnFieldMost = 6;          // musicians on the field at most, the called ones too
        public static int TurnCallCost = 3;           // CALL : BREATH to bring one in from the BENCH, one per bar (NORMAL)
        public static int MaestroTurnCallCost = 5;    //        MAESTRO
        public static int TurnCallCostStep = 1;       //        the second call of a fight and every one after costs this much more
        public static int TurnCutInSpeed = 5;         // CUT IN : an enemy this fast plays in the middle of the bar
        public static int TurnCutInAfter = 2;         //          right after this many of the band's moves

        //PACKS (step 12) : how many enemies a fight has, on floor 1 / 2 / 3. The user asked for more of them the
        //further the band goes, so the second half of a floor's stages has more than the first.
        public static int[] TurnPackEarly = { 1, 2, 3 };      // an ENCOUNTER in the first half of the floor
        public static int[] TurnPackLate = { 2, 3, 4 };       //              in the second half
        public static int[] TurnEliteEscort = { 0, 1, 2 };    // ordinary enemies that come with an ELITE
        public static int[] TurnBossEscort = { 0, 1, 2 };     // and with the BOSS
        public static float MaestroTurnDamage = 1.15f;           // MAESTRO : each of TACET's notes hits this much harder (1 = as NORMAL :
                                                              // its narrower windows and dearer CALL already make it hard, 1.25 left EXPERT at 0 percent)
        public static int TurnShardsPerEscort = 6;            // shards for each enemy beyond the first

        //Floor Scale (step 8) : enemies are tougher deeper down, life and each note's damage, floor 1 / 2 / 3
        public static float[] TurnFloorLife = { 1.6f, 2.1f, 2.7f };   // step 15 : was 1 / 1.3 / 1.6, fights about 1.5 times longer (the user asked for longer fights)
        public static float[] TurnFloorDamage = { 1f, 1.2f, 1.4f };

        //Enemy Traits in the turn battle (step 8, the TurnTraitText of each enemy in Data/Enemy.cs)
        public static float TurnFillsGapsScale = 1.5f;   // DEAD AIR : a missed note
        public static int TurnLullHeal = 12;             // THE LULL : life back on its humming turn
        public static int TurnLullEvery = 3;             //            every this many turns
        public static float TurnFastGap = 0.7f;          // REQUIEM : the gap between notes below half life
        public static int TurnBargainSteal = 1;          // THE DEVIL'S STRING : breath taken by each attack
        //ONE BAR enemies (step 13)
        public static float TurnRoomToneKeep = 0.7f;     // ROOM TONE : the share of the band's damage the OTHER enemies take while it stands
        public static int TurnFeltHandCall = 1;          // THE DAMPER : a CALL costs this much more while it stands (2 left the sim's STRONG player at 23 percent against it)
        public static int TurnTwiceFirst = 1;            // STUTTER : cuts in after this many of the band's moves
        public static int TurnTwiceSecond = 3;           //           and again after this many
        //Bosses of step 14
        public static int TurnByTouchMost = 2;           // THE BLIND CANTOR : missed notes that come back in its next attack, at most
        public static float TurnCourtKeep = 0.5f;        // THE STAFF : the share of the band's damage it takes while another enemy stands
        public static int TurnStaffEvery = 3;            //            every this many attacks it strikes the floor (instead of a CADENZA)
        public static float TurnStaffPower = 1.5f;       //            that one note hits everyone this much harder
        public static int TurnThorns = 5;                // THE DEAF CROWN : life lost by a musician who breaks the DECREE (x the floor's note scale)

        //PRACTICE (step 9) : the soft enemy of the tutorial's practice fight (EnemyList.Practice).
        //Lots of life so the four lessons fit before it falls, and notes that barely hurt.
        public static int TurnPracticeHp = 60;           // x TurnFloorLife on floor 1 (1.6) = 96, step 15 (was 160 with the floor at 1)
        public static int TurnPracticeSpeed = 4;
        public static int TurnPracticePower = 2;         // each note hits this x TurnNoteDamage (4 life)

        //Enemy Attack : how many notes one attack has on floor 1, 2 and 3 (its phrase is cut or repeated to
        //fit between min and max), and how hard each note hits : the enemy's average Pattern power x TurnNoteDamage.
        //ONE BAR : ordinary enemies come in packs, so they play shorter attacks (fewer parries per bar).
        public static int[] TurnNotesMin = { 1, 2, 3 };        // ELITE and BOSS
        public static int[] TurnNotesMax = { 2, 3, 4 };
        public static int[] TurnSmallNotesMin = { 1, 1, 2 };   // ordinary enemies
        public static int[] TurnSmallNotesMax = { 2, 2, 2 };
        public static float TurnNoteDamage = 1.85f;          // step 12 : was 1.5, the user found NORMAL far too easy (turnsim STRONG 81 -> 57 percent).
                                                             // Step 15 : was 2.2, longer fights give TACET more turns, so each note is softer and the run is as hard as before.
                                                             // Notes are rounded to whole numbers, so a small change can jump (1.9 made power 4 notes 8 instead of 7)

        //PARRY : swiping back at a note. PERFECT blocks all of it and gives breath back,
        //GOOD lets this share through, a MISS or no swipe lets all of it through.
        public static float TurnParryGoodKeep = 0.5f;

        //BREATH : one pool for the whole band (the user chose shared, 18 Oct). Skills spend it.
        public static int TurnBreathMax = 10;
        public static int TurnBreathStart = 3;
        public static int TurnBreathPerAttack = 2;   // step 15 : was 1. A weaker ATTACK pays for the skills instead
        public static int TurnBreathPerParry = 1;   // for each note parried PERFECT

        //KNOCKED OUT : a musician at 0 HP sits out the rest of the fight and gets up with this much after it
        public static int TurnReviveHp = 1;

        //OFF-BEAT (ALPHA7_DESIGN 5.6) : an enemy's gauge. Full, it loses its next turn and takes more damage until then.
        //An ATTACK adds TurnAttackOffBeat (ONE BAR above), a skill its own OffBeat per note.
        public static int TurnOffBeatMax = 100;
        public static int TurnParryOffBeat = 10;      // each note parried PERFECT
        public static float TurnOffBeatScale = 1.5f;  // damage against an enemy off the beat

        //Statuses (ALPHA7_DESIGN 5.7)
        public static int TurnDissonanceMost = 9;     // DISSONANCE stacks no higher
        public static float TurnCrescendoStep = 0.15f;// CRESCENDO : each stack hits this much harder
        public static int TurnCrescendoMost = 5;
        public static int TurnMuteTurns = 1;          // MUTE : turns without skills
        public static float TurnGuardKeep = 0.5f;     // GUARD : the share of a hit that gets through

        //COUNTER and CADENZA (ALPHA7_DESIGN 5.4) : parry every note PERFECT and the target hits back for free.
        //Every third attack of a boss is its CADENZA : one note more, each note harder, and a counter twice as hard.
        public static float TurnCounterScale = 1f;    // x the target's attack damage
        public static float TurnCadenzaCounter = 2f;
        public static int TurnCadenzaEvery = 3;
        public static float TurnCadenzaPower = 1.5f;

        //Skill Notes : a HOLD note gives this much let go at once, up to Base + Gain held to the end.
        //A ROLL note is PERFECT with this many shakes, GOOD with this many (MAESTRO asks for more).
        public static float TurnHoldBase = 0.6f;
        public static float TurnHoldGain = 0.8f;
        public static int TurnRollPerfect = 5;
        public static int TurnRollGood = 3;
        public static int MaestroTurnRollPerfect = 6;
        public static int MaestroTurnRollGood = 4;

        //Conductors (ALPHA7_DESIGN 5.9, step 5). The SIGNATURE gauge fills one per PERFECT (an attack,
        //a skill note or a parry). Full, SPACE plays the 4/4 shape : down, left, right, up, any size.
        //How well the four strokes land sets how strong it is (the average of their swipe scale).
        public static int TurnSignatureNeed = 6;
        //PLAYTEST (Beta1, the user asked 28 Oct, to try the SIGNATURE and its CUT-IN often) : above 0, the SIGNATURE needs
        //only this many PERFECTs, CON BRIO or not. Set it back to 0 for the real cost. turncheck and turnsim always use the real one.
        public static int PlaytestSignatureNeed = 1;
        public static float TurnBeethovenMost = 0.5f;  // THE UNHEARING's PASSIVE : up to this much harder as the band's life runs out
        public static int TurnBoltDamage = 14;         // LIGHTNING : damage to every enemy at full strength (x1)
        public static int TurnMozartRun = 3;           // THE SHADOWLESS's PASSIVE : this many PERFECTs in a row give 1 breath
        public static int TurnFeatherPerStroke = 2;    // FEATHER : free ATTACKs per stroke that lands (step 12, was 1 extra turn : 25 percent of STRONG runs won)

        //The other four of the lore (step 10, ALPHA7_DESIGN 5.9). Signatures are x their strength (0.4 to 1.3).
        //THE METRONOME (Bach) : LOCKED TEMPO needs no number (any size is right), FUGUE echoes the next move at the strength.
        public static float TurnFugueScale = 1.8f;     // FUGUE : the echo plays at the SIGNATURE's strength x this (step 13 : was 1, THE METRONOME was the weakest at 44 percent)
        public static int TurnPaganiniLife = 6;        // THE HEARTLESS's PASSIVE : life paid for each BREATH a skill is short of (step 15 : was 3, skills carry the fight now)
        public static float TurnCapricePrice = 0.2f;   // CAPRICE : when it ends, every musician pays this share of their life (never below 1, step 13 : was 0.1)
        public static int TurnCapricePerStroke = 2;    // CAPRICE : turns per stroke that lands (step 12, a bar has more turns now)
        public static float TurnLullyWindow = 0.01f;   // THE KING'S BATON's PASSIVE : seconds added to both parry windows (0.05 won 99 percent of STRONG runs, the others 83 to 90;
                                                       // step 12 : 0.03 won 92 percent and 0.02 84 when parrying became most of the fight)
        public static int TurnSunHeal = 6;             // THE SUN : life back for every musician at full strength (step 13 : was 12, THE KING'S BATON was the strongest at 71 percent. Step 15 : 8 -> 6)
        public static float TurnFannyScale = 0.25f;    // THE UNSIGNED's PASSIVE : FANNY's voice hits this much harder
        public static float TurnFelixHeal = 1.5f;      //                          FELIX's voice heals this much more
        public static int TurnFelixCrescendo = 1;      //                          and gives this much more CRESCENDO
        public static int TurnDuetDamage = 12;         // TWO VOICES : damage to every enemy at full strength (step 13 : was 9 and 6, THE UNSIGNED 46 percent)
        public static int TurnDuetHeal = 8;             //              and life back for every musician

        //Musician Traits in the turn battle (step 10, the TurnTraitText of each musician in Data/Musician.cs)
        public static float TurnCountsWindow = 0.05f;  // COUNTS ALOUD (ANNA) : seconds added to her PERFECT window
        public static float TurnQuietScale = 1.5f;     // THE QUIET PART (KLARA) : against an enemy showing a p note
        public static int TurnMomentumMost = 3;        // MOMENTUM (OTTO) : power gained by hits in a row, at most
        public static float TurnByEarScale = 1.5f;     // BY EAR (MALI) : against an enemy that is OFF-BEAT
        public static float TurnHeldNoteShare = 0.4f;  // HELD NOTE (CHAI) : his ATTACK hits again for this share
        public static float TurnStepBetterScale = 1.3f;   // ONE STEP BETTER (NUAN) : against an enemy showing an f note
        public static int TurnFourBarsEvery = 4;       // FOUR BARS STRAIGHT (LUKA) : every this many ATTACKs
        public static float TurnFourBarsScale = 2f;    //                             hits this much harder
        public static float TurnLateBonus = 0.12f;     // FASHIONABLY LATE (IRIS) : seconds after the GOOD window a late stroke still counts
        public static int TurnThunderSoften = 2;       // THE WHOLE FLOOR (BORIS) : taken off each note of the enemy's next attack

        //ARCHIVE (step 7, Data/Archive.cs) : ECHOES a finished run leaves, and what each upgrade level gives
        public static int EchoesPerWin = 3;            // each fight won
        public static int EchoesPerFloor = 10;         // each floor cleared
        public static int EchoesRunWon = 25;           // the whole run won
        public static float EchoesMaestro = 1.5f;      // MAESTRO pays more
        public static int ArchiveLifePerLevel = 2;     // STRONGER BAND
        public static int ArchiveBreathPerLevel = 1;   // DEEP BREATH
        public static int ArchiveSignaturePerLevel = 2;// WARM UP
        public static int ArchiveShardsPerLevel = 20;  // SAVINGS
        public static int ArchiveStagePerLevel = 1;    // FULL STAGE : musicians more on the field at the start (step 12)

        //Turn Motifs (step 6) : what the 21 motifs do in the turn battle (their TurnText in Data/Motif.cs).
        //ROSIN keeps FamilyMotifPower, STEADY PULSE keeps SteadyWindowBonus, ENCORE and PATRON'S PURSE work on the result page.
        public static float TurnReedCaseHeal = 1.5f;      // REED CASE : wind players' healing
        public static float TurnSpareSticksOffBeat = 1.5f;// SPARE STICKS : percussion skills' OFF-BEAT
        public static int TurnPianissimoBreath = 1;       // PIANISSIMO : more breath for a p note parried PERFECT
        public static int TurnBreathMarkStart = 2;        // BREATH MARK : more breath at the start of a fight
        public static float TurnTuttiScale = 1.25f;       // TUTTI : skills that reach everyone
        public static float TurnRubatoMiss = 0.6f;        // RUBATO : a skill note you MISS still plays at this (step 12)
        public static int TurnCrescendoStart = 1;         // CRESCENDO : stacks on every musician at the start
        public static float TurnSforzandoScale = 1.3f;    // SFORZANDO : a PERFECT f note
        public static float TurnOvertureScale = 2f;       // OVERTURE : the band's first hit of a fight
        public static float TurnCounterpointScale = 2f;   // COUNTERPOINT : the COUNTER
        public static int TurnAccelerandoShakes = 2;      // ACCELERANDO : each shake of a roll counts this many
        public static int TurnGraceTimes = 2;             // GRACE NOTE : a spark that lands does its skill this many times
        public static int TurnMarcatoOffBeat = 2;         // MARCATO : a PERFECT attack's OFF-BEAT, times this
        public static int TurnConBrioNeed = 4;            // CON BRIO : PERFECTs the SIGNATURE needs instead
        public static float TurnCodaBelow = 0.25f;        // CODA : an enemy under this share of its life
        public static float TurnCodaScale = 1.5f;         //        takes this much more
        //ONE BAR motifs (step 13). SEGUE and LAST WORD need no number.
        public static float TurnDownbeatScale = 1.5f;     // DOWNBEAT : the first move of a bar
        public static float TurnEntranceScale = 2f;       // ENTRANCE : a called musician's moves in their first bar
        public static int TurnGreenRoomHeal = 2;          // GREEN ROOM : life back for each musician on the BENCH at the end of a bar
        public static float TurnUnisonScale = 2f;         // UNISON : the last ATTACK of a bar that all goes at one enemy

        //Turn Feel (seconds) : how the battle page paces a swipe. The ring always closes at the same
        //speed, never eased, so the eye can count it (docs/research/ALPHA7_qte_parry.md).
        public static float TurnRingTime = 1.1f;          // from a note showing to the moment to swipe
        public static float MaestroTurnRingTime = 0.8f;   // MAESTRO
        public static float TurnNoteGap = 0.55f;          // between the notes of one enemy attack
        public static float TurnPause = 0.8f;             // after a turn, before the next one starts
        public static float TurnHitStop = 0.075f;         // the band and TACET freeze for a moment on a PERFECT
        public static float TurnStrikeTime = 0.62f;       // ONE BAR : how long an ATTACK's lunge takes, there and back (Beta1 : slower, smoother)
        public static float TurnStrikeImpact = 0.24f;     //           when in it the hit lands
        public static float TurnStrikeStop = 0.08f;       //           the freeze when it lands
        public static float TurnNumberTime = 1.0f;        // a damage number stays this long (Beta1 round 4, pixel numbers)
        public static int TurnBigHit = 18;                //   from this much damage it is drawn bigger
        public static float TurnNumberCell = 4.5f;        //   one cell of a digit (5 x 7 cells) in pixels (round 4 : 25% smaller, was 6)
        public static float TurnNumberBigCell = 6f;       //   the same for a big hit (was 8)
        public static float TurnDissolveTime = 1.0f;      // an enemy SILENCED dissolves into TACET's hole in this long
        public static float TurnCutInTime = 1.3f;         // the SIGNATURE's CUT-IN, before its strokes (Beta1 round 4, the old duel's picture)
        public static float TurnKillStop = 0.18f;         //           the freeze when an enemy falls silent
        public static float TurnEndTime = 2.2f;           // the win or lose banner stays this long
        public static float TurnSparkGap = 0.3f;          // a spark comes this soon after the note before it
        public static float TurnHoldTime = 0.8f;          // how long a HOLD note wants holding
        public static float TurnHoldStill = 260f;         // pixels per second the baton may drift and still count as still
        public static float TurnRollTime = 1.2f;          // how long a ROLL note lasts

        //Stage Feel (Beta1) : how the battle page looks and moves, the conductor's view from behind the band
        //(docs/BATTLE_SCENE_2_3D_CHANGE.md). Only Screens/BattleScreen.Stage.cs reads these, the rules never do.
        public static float StageSwapTime = 0.5f;         // the musician pointed at slides out and the next slides in (seconds, round 4 : slower)
        public static float StageSwapSlide = 240f;        //   how far they slide (pixels)
        public static float StageAmbient = 0.34f;         // LIGHT MAP everywhere (0.5 = lit normally) : the stage is dark, lights do the rest
        public static float StageSpot = 0.42f;            //   the stage light on the musician in front
        public static float StageFootLight = 0.2f;        //   the pool of light each enemy stands in (like Chaos Zero Nightmare)
        public static float StageShaft = 0.1f;            //   the shaft of light from above on the musician in front (GLOW, round 4)
        public static int StageDust = 0;                  //   specks of dust turning in it (round 4 : none, the user found white specks cluttered)
        public static float StageFloorLight = 0.16f;      //   the house lights on the floor near the band, so TACET's dark can be seen eating it
        public static float StageHitLight = 0.55f;        //   a hit lights up where it lands, for a moment
        public static float StageHitLightTime = 0.35f;    //   how long that lasts (seconds)
        public static float StageBloomThreshold = 0.58f;  // BLOOM : how bright before it glows
        public static float StageBloomStrength = 0.75f;   //         how strong the glow is
        public static float StageVignette = 0.85f;        // VIGNETTE : dark corners
        public static float StageGrain = 0.02f;           // film grain (round 4 : softer, the user found specks cluttered)
        public static float StageFocusSpeed = 3.4f;       // QTE FOCUS : how fast the world dims when a ring comes up (round 4 : slower, eased)
        public static float StageFocusSlow = 0.3f;        //   the world behind moves this fast meanwhile (1 = normal)
        public static float StageRiftSlant = 0.42f;       // NO SHADERS : TACET's old rift leans with the floor, pixels right for each pixel down
        public static float StageRiftWinning = 520f;      //   where it stands at the horizon when TACET has all its life and the band none
        public static float StageRiftBeaten = 1250f;      //   and the other way round
        public static float StageDriftAmount = 5f;        // the camera breathes this many pixels
        public static float StageKick = 9f;               // a hit nudges the camera this many pixels toward it
        public static float[] StageParallax = { 0.18f, 0.4f, 0.75f, 1f, 1.25f };   // far, mid, stage floor, fighters, front : how much each layer follows the camera
        public static float StageDepthDim = 0.16f;        // each enemy further back is this much darker

        //Hit Feel (Beta1 round 4) : what a blow looks like where it lands (Core/HitFx.cs). Nothing flies : the musician
        //strikes and the light happens on the enemy. Strings SLASH, winds SHOT, percussion SMASH, the SIGNATURE CROSS.
        public static float HitSize = 105f;               // the size of a blow's light (pixels), more for a stronger blow
        public static float HitCrossLength = 1500f;       // CROSS : how far the SIGNATURE's two cuts reach each way
        public static float HitImpactTime = 0.06f;        // IMPACT FRAME : how long the world stays turned over (seconds)
        public static float LessFlash = 0.45f;            // LESS FLASHING (settings) : flashes and shaking are this strong

        //Stage Void (Beta1 round 4) : TACET's dark, a black hole behind the enemies pouring over the floor towards the camera
        //(Core/TacetVoid.cs, Content/Effects/Void.fx). SHARE 0 = TACET beaten, 1 = the band beaten.
        public static float StageVoidX = 990f;            // HOLE : its middle on the screen, behind the enemies
        public static float StageVoidY = 240f;
        public static float StageVoidParallax = 0.5f;     //   it follows the camera this much (between the middle layer and the floor)
        public static float StageVoidRadiusMin = 60f;     //   its size at SHARE 0 ...
        public static float StageVoidRadiusMax = 170f;    //   ... and at SHARE 1
        public static float StagePoolWideMin = 300f;      // POOL : the dark on the floor, how far it reaches across at SHARE 0 ...
        public static float StagePoolWideMax = 1300f;     //   ... and at SHARE 1
        public static float StagePoolDeepMin = 70f;       //   how far it comes towards the camera at SHARE 0 ...
        public static float StagePoolDeepMax = 440f;      //   ... and at SHARE 1 (past the bottom of the screen)
        public static float StageVoidSpike = 14f;         //   the waveform on its edge (pixels)
        public static float StageVoidSpikeDanger = 30f;   //   this much taller when the band is close to losing
        public static float StageVoidSwirl = 0.9f;        // LENSING : how far the world turns at the rim (radians)
        public static float StageVoidKick = 0.36f;        // a hit throws the SHARE this fast, then it springs back
        public static float StageVoidLight = 0.32f;       // LIGHT MAP : the hole lights what stands in front of it
        public static float StageVoidSeamLight = 0.16f;   //   and the seam lights the floor
        public static float StageVoidWaveSpeed = 950f;    // SHOCK WAVE : how fast the ring of a hit runs out (pixels a second)
        public static float StageVoidWaveTime = 0.55f;    //   how long it lasts (seconds)
        public static float StageVoidCracks = 2.5f;       // CRACKS a second when calm (up to twice as many in danger)
        public static int StageVoidDebris = 34;           // DEBRIS : bits of the stage turning around the hole
        public static float StageVoidEngulfFrom = 0.62f;  // ENGULF : past this SHARE the dark creeps in from the screen's edges

        //Battle Shape
        public const int BeatsPerRound = 8;       // sub-turns in one round
        public const int MaxRounds = 3;           // after this, whoever is ahead wins
        public const float LineLimit = 100f;      // push the line this far to win outright

        //REPEATS (round 11) : TACET's phrase is played more than once in a round, without a stop. TACET plays the same beats each time, but decides its
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
        //Each extra player adds this much, so three players give x1.30. Players past HarmonyMost
        //add their power but no more harmony (round 15 : the whole band plays every note now).
        public static float HarmonyPerExtra = 0.15f;
        public static int HarmonyMost = 5;

        //THE NOTE SAYS (round 15, 29 Sep) : the player found the stamina rules made the game about
        //playing small to save breath. Now every note of TACET's says how big to swing, p small,
        //mf middle, f big, and the whole band plays every note the baton lands. Playing is free.
        //The wrong size still plays, at this share. (Round 12 to 14 : DYNAMICS, the size picked
        //who came in, a small stroke only the p players, and everybody who played paid stamina.)
        //NORMAL keeps half (29 Sep, was 0.3 for everyone), MAESTRO keeps 0.3 (see MAESTRO MODE below).
        public static float WrongSizePower = 0.5f;

        //CUE (round 12) : every beat the baton goes one way. Round 14 : every musician has an arrow
        //of their own (Musician.Cue), left, down and up, or right, and the players whose arrow
        //matches the way hit harder. Down and up share one arrow. Round 15 : the arrow is where
        //their instrument sits in a real orchestra (violins left, cellos right).
        public static float CuePower = 1.5f;

        //SECTION (round 15) : players of one family on stage together (a real orchestra's string section),
        //each hits this much harder with two of them, and with all three
        public static float SectionTwo = 0.10f;
        public static float SectionThree = 0.25f;
        public static int[] CueSide = { 1, 0, 2, 1 };      // beat of the bar -> arrow : 0 left, 1 down and up, 2 right

        //Rest : a beat where TACET is silent and the baton lets it pass gives stamina back.
        //Round 11 : 6 -> 10, the player found rests gave back far too little to be worth planning.
        //Round 12 : 12, a rest is now the only breath there is inside a fight.
        public static int RestRecover = 12;

        //Breath (the STAMINA number) is the band's life, like the life gauge of a rhythm game.
        //Round 15 : playing never spends it. It is knocked out of the band instead:
        //   TACET'S BLOW  a beat TACET wins hits the band for what got through
        //   a MISS costs MissBreath more
        //   COLLAPSE      breath at zero ends the fight, the band is silenced (SECOND WIND saves it once)
        public static float BlowPerPower = 0.95f;   // stamina lost per point TACET wins a beat by, was 1.0, 0.7 in round 9
                                                    // round 10 : 0.63 kept the whole game as hard as round 9 after BREATH MARK was cut
                                                    // round 11a : 0.68 made up for the softer WHITE NOISE
                                                    // round 11b : 0.78 with the bigger rests (the player allowed it a little easier)
                                                    // round 12 : 0.75 with the marks wider apart (EnemyBoostPower 2),
                                                    // a misread f hurts, which is what sets careful players apart
                                                    // round 13 : 0.95, every conductor's own passive and signature
                                                    // is real power now, so the whole game stays as hard as round 12.2

        //Timing Grades (HARD, 25 Sep : players found the wide windows far too easy)
        //The stroke settings page can move every judgement earlier or later for one player's
        //hand and screen (Settings.TimingOffset), so the windows can stay narrow.
        //NORMAL (29 Sep) : a little wider again, friends could not keep up on the first floor.
        //MAESTRO keeps the narrow 0.08 / 0.18.
        public static float PerfectWindow = 0.10f; // seconds either side of the beat, was 0.12, 0.08 until 29 Sep
        public static float GoodWindow = 0.22f;    // was 0.30, 0.18 until 29 Sep
        public static float EarlyTime = 0.24f;     // a stroke ending earlier than this before its beat is the hand getting ready
        public static float PerfectBonus = 1.3f;   // a PERFECT stroke multiplies the band's power (round 12, was 1.2)
        public static float MissPower = 0.45f;     // PENALTY : a bad stroke still counts, but weaker, was 0.7, 0.55 until round 12
        public static int MissBreath = 4;          // PENALTY : and costs this much breath
        //No stroke on one of TACET's notes (HESITATE) : nobody plays, and the note lands whole.

        //Signature : PERFECT beats collect notes for the instrument families that played them
        //(GOOD ones did too until round 13). When the conductor's recipe is complete (see Conductor.Recipe), SPACE lets
        //the signature loose. Round 13 : every conductor has a move of their own (Conductor.Move),
        //and it lasts for the next few strokes. Was : the next stroke was a PERFECT f, x1.5 for all.
        //No notes are collected while a signature is running, so one cannot chain into the next.
        public static int SignatureStrokes = 4;         // how many strokes a signature lasts

        //Tempo : beats per minute in round 1, 2 and 3, one row per floor. The duel speeds up as the
        //fight goes on, and the floors speed up too. TACET's second bar is called while the first
        //is being answered, so the player conducts on every beat without waiting.
        //Round 15 : the player found the very first fight far too quick (every floor was 96 / 112 / 128,
        //round 9). The last floor keeps that, the first is gentle, so the game climbs.
        //NORMAL (29 Sep) : slower again, friends still could not keep up on the first floor
        //(was 80 / 88 / 96, 88 / 100 / 112, 96 / 112 / 128). MAESTRO plays 96 / 112 / 128 on every floor.
        public static int[][] TempoByFloor =
        {
            new int[] { 70, 76, 84 },       // floor 1
            new int[] { 80, 90, 100 },      // floor 2
            new int[] { 88, 100, 112 },     // floor 3
        };
        public static float PhraseTail = 0.5f;      // after the last answer, before the round is summed up

        //Gentle Fights : the first fights of a run, where TACET only plays f and p (never mf), so
        //the first thing to learn is two sizes (round 15, see BattleState.Gentle). NORMAL 3
        //(29 Sep, was 2), MAESTRO none.
        public static int GentleFights = 3;

        //Teaching Order : the special notes arrive one floor at a time, so floor one teaches the
        //plain game with the roll (the enemy traits also start on floor two). Keep it simple to learn.
        //Round 9 : every round ends on a special note. Round 15 : elites and bosses ROLL it, ordinary
        //enemies end on a plain note on floor one and HOLD it from floor two.
        public static int TremoloFromFloor = 1;     // elites and bosses roll from the start, shaking is easy
        public static int FermataFromFloor = 2;     // ordinary enemies hold their last note from floor two
        public static int PairsFromFloor = 2;       // round 9 : pairs from floor two for quicker flicks, was 3

        //Double Notes : some of TACET's notes come in pairs, a note tied to a spark. The note is
        //answered on the beat, the spark half a beat later with one more flick, any way.
        public static int[] DoubleNotes = { 2, 3, 4 };     // how many pairs in round 1 / 2 / 3, from PairsFromFloor on, was 1 / 2 / 3
        public static int DoubleMost = 4;
        public static float GraceShare = 0.5f;      // the second note is worth this much of its beat, on both sides

        //Tremolo : elites and bosses end every round with a roll two
        //beats long. Shake the baton as fast as you can: every stroke adds power (the strokes are
        //free since round 8).
        public static float TremoloBeats = 2f;
        public static float TremoloEnemy = 1.5f;    // TACET's roll hits this much harder than its note
        public static float TremoloBase = 0.4f;     // our part with no strokes at all
        public static float TremoloStep = 0.15f;    // each stroke adds this much of our part
        public static int TremoloMost = 10;         // strokes past this add nothing
        public static int TremoloPerfect = 5;       // strokes for a PERFECT roll, was 7, 6 until 29 Sep (NORMAL is slower, MAESTRO keeps 6)
        public static int TremoloGood = 3;          // strokes for a GOOD roll, 4 until 29 Sep, fewer than 1 is a HESITATE

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
        public static int LullabySlower = 8;        // THE LULL, round three slows to this much under round one of its floor (round 15, was 88 BPM)
        public static int UnfinishedFaster = 12;    // REQUIEM, round three rushes this much over round three (round 15, was 140 BPM)
        public static float FillsGapsPower = 1.5f;  // DEAD AIR, a note let pass or missed (round 15, was : a small stroke or none)
        public static float NoRestShare = 0.5f;     // WHITE NOISE, silent beats give back only this share (round 11, was nothing)
        public static float MirrorScale = 0.8f;     // THE NAMELESS MASTER, your last round played back. Round 15 : 0.35 -> 0.8,
                                                    // the whole band plays every note, at 0.35 nobody ever lost to him. Round 12 : 0.9 -> 0.35,
                                                    // a round holds whole-band strokes now, not a planned few
        public static float BargainPower = 1.5f;    // THE DEVIL'S STRING, the deal : this much power
        public static int BargainStamina = 15;      //     for this much max stamina, for the whole run

        //Musician Traits : the numbers behind each MusicianTrait (see Data/Musician.cs)
        public static float KeepsCountWindow = 0.05f;  // ANNA, added to the PERFECT window on her beats (her arrow)
        public static int MomentumStep = 1;            // OTTO, per beat in a row
        public static int MomentumMax = 3;
        public static float ByEarPower = 1.5f;         // MALI, on hidden beats
        public static float HeldNoteShare = 0.4f;      // CHAI, how much of his note rings on
        public static float OneStepBetterPower = 1.3f; // NUAN, against a loud note
        public static float FourBarsPower = 2f;        // LUKA, the fourth beat of a full bar
        public static int ThunderKnock = 2;            // BORIS, taken off TACET's next note after his big stroke on an f
        public static float QuietPartPower = 2f;       // KLARA, against TACET's p notes (round 14, she used to play on every stroke)

        //Timing Lengths (seconds). The beats themselves follow TempoBpm above.
        public static float IntroTime = 1.1f;      // round banner
        public static float IntroNoteTime = 2.2f;  // a banner that says what changes this round (round 14)
        public static float TraitIntroTime = 3.6f; // the first banner against a trait never met before (round 14)
        public static float TeachNoteTime = 3.0f;  // a banner that teaches something new, the first time it turns up (round 15)
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
        //Floor Power : how hard TACET hits on each floor. Round 15 : playing is free and the whole
        //band plays every note, so a band grows much stronger over a run than before. The floors
        //climb steeply to match, and the first floor is softer, so it can be learned (the player
        //asked for a curve, easy first, hard later). Round 9 to 14 : 1.0, 1.25, 1.5.
        //NORMAL (29 Sep) : softer, most of all on floors two and three where a newcomer used to
        //stop (was 0.85, 1.45, 2.45). With the slower tempo an average player wins about 65 percent
        //of runs in the simulation (24 before), a newcomer clears floor one about 88 percent of the time.
        public static float[] FloorPower = { 0.8f, 1.25f, 2.1f };

        //MAESTRO MODE (29 Sep) : the player picks NORMAL or MAESTRO on the conductor pages
        //(RunState.Maestro, Settings.Maestro). A friend found even the first floor too quick, so
        //NORMAL is every number above, made gentler. MAESTRO is hard from the first fight:
        //full speed on every floor, no gentle fights, every special note from floor one (the
        //roll, the held note and the pairs), a wrong size keeps only 30 percent, narrow windows,
        //more shakes for a PERFECT roll, and TACET at full strength. Search MAESTRO to find
        //every place the mode changes something (BattleState).
        public static int[][] MaestroTempo =
        {
            new int[] { 96, 112, 128 },     // every floor, as round 9 to 14
            new int[] { 96, 112, 128 },
            new int[] { 96, 112, 128 },
        };
        public static int MaestroGentleFights = 0;
        public static int MaestroSpecialFromFloor = 1;      // rolls, held notes and pairs all from floor one
        public static float MaestroWrongSizePower = 0.3f;
        public static float MaestroPerfectWindow = 0.08f;
        public static float MaestroGoodWindow = 0.18f;
        public static int MaestroTremoloPerfect = 6;
        public static int MaestroTremoloGood = 4;
        public static float[] MaestroFloorPower = { 1.0f, 1.65f, 2.4f };   // a strong player wins about 64 percent, an average one 14 (with the songs, 29 Sep)
        //Round 8 : BOOST costs nothing extra, so a big stroke on every beat is the normal way to
        //play. TACET hits about 1.5 times harder to match, which kept the simulated win rates
        //of ordinary players where they were (see PROJECT_STATUS section 8).
        //Round 11 : ordinary enemies never won a single fight in the simulation, they only cost
        //breath. They hit harder now, so a band that arrives tired can lose to them.
        //Round 12 : lower again, TACET's f doubles its note now and the band pays for every player.
        //Round 15 : higher again, the whole band plays every note and playing costs nothing.
        public static float NormalScale = 3.4f;   // ordinary encounters. Round 15 : 2.6, then 3.4 once they play ODE TO JOY (a note
                                                  // on almost every beat, many of them p, made them far weaker). 2.2 in round 12 to 14
        public static float EliteScale = 2.1f;    // 29 Sep 1.8 -> 2.1 : elites play songs now (SYMPHONY NO. 5, PRELUDE IN C), a song has rests and softer notes. 1.4 in round 12 to 14, was 2.0 (1.35 before round 8)
        public static float BossScale = 2.7f;     // 29 Sep 2.3 -> 2.7 : bosses play songs now (RONDO ALLA TURCA, NACHTMUSIK). 1.8 in round 12 to 14, was 2.8 (1.85 before round 8)

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
        public static int TurnBenchSize = 4;         // TURN BATTLE (step 15) : the stage starts with 2 seats, so the bench holds one more
        public static int FirstFloorRecruits = 3;    // how many join at the very first opening (step 12 : was 2, so the first fight already has one on the BENCH)
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
        public static int FamilyMotifPower = 1;      // ROSIN
        public static int ReedCaseRest = 4;          // REED CASE (round 15) : a rest gives this much more with a wind player on stage.
                                                     // Was : the winds cost 1 less stamina, and playing is free now
        public static float SpareSticksPower = 1.3f; // SPARE STICKS (round 15) : the percussion on TACET's f notes. Was : they cost 1 less
        public static int PianissimoRecover = 3;     // PIANISSIMO, a small stroke on a p note gives this back
        public static float PurseBonus = 1.3f;       // PATRON'S PURSE
        public static float BreathMarkRecover = 1.25f; // BREATH MARK : 12 x 1.25 = 15 (round 12, 10 -> 12.5 in round 11). Was 2 (x2), round 10 :
                                                        // one card alone lifted a run from 8 to 46 percent in the simulation
        public static float TuttiPerExtra = 0.06f;   // TUTTI, 0.10 until round 14 (the whole band plays every note now)
        public static float SteadyWindowBonus = 0.05f;   // STEADY PULSE, added to both windows
        public static float EncoreRecover = 0.15f;   // ENCORE
        public static float RubatoMissPower = 0.85f; // RUBATO
        public static float SforzandoPower = 1.2f;   // SFORZANDO, a big stroke on an f note (round 15 : 1.3 -> 1.2, +20 alone)
        public static float SecondWindRefill = 0.15f;// SECOND WIND, was 0.25 (round 12.1 : still +26 alone, the most of any motif;
                                                    // 0.15 is about one rest, +14 to +18 like the other rarity 3 motifs). Round 12 : 0.33 -> 0.25
        public static float OverturePower = 1.5f;    // OVERTURE
        public static float CounterpointPower = 1.2f;// COUNTERPOINT (round 15 : 1.3 -> 1.2, +20 alone)

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
        //THE METRONOME's LOCKED TEMPO has no number (round 15) : his band reads the marks, so his
        //stroke is never the wrong size, but only a real match is IN TUNE. Rounds 10 to 14 he paid
        //more stamina for it (LockedTempoCost 1.33), and playing costs nothing now.
        //THE APPRENTICE's BY THE BOOK has no number : his GOOD beats fill the recipe too (round 13, he had no perk).
        //Tried first : 1 or 2 breath back for every stroke IN TUNE won 85 to 88 percent of simulated runs alone.
        public static float RunawayFireBonus = 1.3f; // THE INFERNO, the beat after a miss
        public static float SetAlightPower = 1.3f;   // THE INFERNO's SET ALIGHT, every stroke under it
        public static float SetAlightCap = 10f;      //     and it may push this far, a quarter past the PUSH CAP (16 : 80 percent, 12 : 77). Tried first :
                                                     //     the band caught fire (FORTISSIMO, free), 91 to 96 percent, a fire that costs
                                                     //     no breath is worth far more than any power
        public static float CloserLouderMax = 0.25f; // THE UNHEARING, how much harder the band hits with no breath left (+25 percent).
                                                     // Round 14 : 0.3 -> 0.25, the percussion hit harder now and he led the rest.
                                                     // Round 13 : it counts the breath the band has lost. It used to count how far
                                                     // TACET had pushed the line (+130 percent), which a band rarely lets happen, and
                                                     // a band loses on breath since round 12. Round 12.2 made rests deeper there too.
                                                     // 1.0 won 84 percent of simulated runs alone, the others about 70,
                                                     // and 0.4 from half breath 76 with the others at 70 to 72
        public static float CloserLouderFrom = 0.5f; // THE UNHEARING, the share of breath under which the band starts hitting harder
        public static float CrossCultureHarmony = 0.05f; // THE FOLK LEADER, per extra culture on a beat. Was 0.15 : he won the most runs (round 11).
                                                         // Round 14 : 0.07 -> 0.05, with stamina 6 -> 5 (he was 5 to 9 points above the rest)
        public static int VillageBandRecover = 3;    // THE FOLK LEADER's VILLAGE BAND, breath back for every stroke on time under it (round 15)

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
