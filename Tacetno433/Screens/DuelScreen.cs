using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Battle;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //DuelScreen : one round of the fight, conducted beat after beat without a break.
    //
    //A ROUND is eight beats, written as two bars of four.
    //   CALL    TACET plays its eight notes, one on each beat. Every note leaves the right edge
    //           and slides along the lane for exactly one bar, so it reaches the hit point in the
    //           middle of the screen on the very beat where it has to be answered.
    //           Each note is written with how loud TACET plays it: f (loud), mf, or p (soft), and
    //           the small pointer on its edge is the way to swing. The note to answer next is bright,
    //           the ones behind it are dimmed, so there is always one clear thing to do.
    //   ANSWER  starts one bar after the call, so from then on TACET's second bar is still
    //           arriving while the player answers its first. The player conducts on every beat,
    //           at the round's tempo. The tempo rises each round (see BattleState.Tempo).
    //   REPEATS round two plays the phrase twice and round three three times, without a stop
    //           (BattleRules.PassesPerRound). The next time through comes down the lane while the
    //           last one is still being answered, with fresh f / mf / p marks. Note g of the
    //           round is beat g % 8 of pass g / 8 (see BeatOf, PassOf, CallAt).
    //
    //THE MOUSE IS THE BATON, and it only counts while the LEFT BUTTON IS HELD. The strokes follow
    //a real conductor's 4/4 pattern: beat 1 DOWN, beat 2 LEFT, beat 3 RIGHT, beat 4 UP. The beat
    //lands where the baton stops or turns. There is no long song: every answered beat plays the
    //next note of the band's melody, so the music only happens when the player conducts.
    //
    //WHO PLAYS is decided by the stroke itself (round 12, there is no score page any more):
    //   DYNAMICS  how BIG the stroke is picks the rows that come in: small (p) the back row,
    //             middle (mf) the middle row too, big (f) the whole band. The ruler on the baton
    //             says p, mf and f, the same marks TACET's notes carry: answer f with f.
    //             Everyone who comes in pays stamina, so a big stroke tires the band.
    //   CUE       the way the baton goes points at one side of the stage (down and up the centre,
    //             left and right their own side). Whoever sits there hits harder. The players the
    //             baton points at wear a small mark over their heads.
    //   REST      where TACET is silent, letting the beat pass or a small stroke is a rest and
    //             gives breath back (SOFT REST). A middle or big stroke there is a free hit
    //             instead. Either is fine, neither is a miss.
    //
    //Extra notes and moments:
    //   PAIR       a note tied to a spark (from floor two). The spark is answered half a beat
    //              later with one more flick, any way at all (the conductor's rebound).
    //   FERMATA    from floor two, ordinary enemies end each round with a held note two beats
    //              long. Stroke it, then keep the button held and the baton still. Holding pushes.
    //   TREMOLO    everyone else ends each round with a roll two beats long instead (every enemy
    //              on floor one, elites and bosses after). Shake the baton: every stroke adds power.
    //   Holds and rolls cost nothing on top of the players who play them. A roll is the whole band.
    //   IN TUNE    a stroke on time the same size as TACET's real mark (p small, mf middle, f big)
    //              takes the edge off its note, a small tag over the judgement says so.
    //   COUNTER    a PERFECT big stroke against TACET's real f note knocks more of it back.
    //   FORTISSIMO a long combo sets the band on fire for a few beats.
    //   FINALE     far enough ahead at the end of a round, four strokes of the pattern on the
    //              beat end the fight at once.
    //
    //Good beats give notes to the instrument families that played them. When the conductor's
    //recipe is complete, SPACE lets the signature loose: a cut-in, then the next stroke is a
    //PERFECT big stroke, harder still. ESC opens the pause menu (see PauseMenu); coming back, the
    //band counts three beats in.
    //
    //THIS CLASS IS SPLIT OVER SEVEN FILES, all called DuelScreen (the "partial" keyword lets one
    //class be written in several files; the compiler joins them back into one):
    //   DuelScreen.cs          the state, Load, Update, and the order of a round
    //   DuelScreen.Baton.cs    judging a stroke and the stroke guide (the stick is Core/Baton.cs)
    //   DuelScreen.Notes.cs    pairs, the fermata, TACET's roll, the counter and FORTISSIMO
    //   DuelScreen.Finale.cs   the FINALE that ends a fight early
    //   DuelScreen.Stage.cs    the stage, the band, TACET's eclipse, the lane and its notes
    //   DuelScreen.Hud.cs      the top strip, the ring, the clash numbers, banners, cut-in
    //   DuelScreen.Panels.cs   the three panels along the bottom
    public partial class DuelScreen : GameScreen
    {
        private enum Phase { Intro, Play, Finale, RoundEnd }

        //Duel Layout
        private Rectangle stageBox = new Rectangle(40, 196, 600, 420);   // the band area, for pop ups
        private Rectangle tugBar = new Rectangle(440, 34, 400, 12);
        private const float HitX = 640f;                     // the hit point never moves
        private const float RingY = 372f;
        private const float CueBeamStart = 72f;              // the CUE beam leaves the ring this far left of the hit point
        private const float LaneHalf = 34f;                  // half the height of the lane
        private const float RingStart = 120f;
        private const float RingTarget = 44f;
        private const float BandX = 330f;                    // our sound starts here
        private const float AnswerWaveSpeed = 1600f;         // fast, so it reaches the hit point on the beat
        private const float ClashY = 236f;                   // the clash numbers sit above the lane
        private const float EnemyFeetY = 520f;               // TACET's shape stands on this line
        private const float FinaleTitleTime = 1.4f;          // the FINALE card before its bar starts
        private Rectangle roundPlate = new Rectangle(0, 10, 282, 100);
        private Rectangle enemyPlate = new Rectangle(1000, 10, 280, 64);
        private Rectangle handBox = new Rectangle(20, 122, 190, 190);
        private Rectangle signPanel = new Rectangle(12, 612, 184, 104);
        private Rectangle bandPanel = new Rectangle(206, 612, 662, 104);
        private Rectangle textBox = new Rectangle(880, 612, 388, 104);

        //Beat Pattern : the shape a conductor draws in 4/4, one way per beat of the bar
        private static Flick[] pattern = { Flick.Down, Flick.Left, Flick.Right, Flick.Up };

        //Stroke Sizes : small, middle and big, and the order each one gives the band
        private static Choice[] sizeChoice = { Choice.Ease, Choice.Normal, Choice.Boost };

        private static string[] choiceWord = { "mf", "f", "p" };            // in Choice order, the stroke's mark
        private static int[] tierChoice = { 2, 0, 1 };                      // row tier 0, 1, 2 -> Choice p, mf, f
        private const string silencedWord = "SILENCED";
        private static string[] dynamicMark = { "mf", "f", "p" };           // in Choice order, as music writes loudness
        private static string[] gradeWord = { "", "PERFECT", "GOOD", "MISS", "HESITATE" };
        private static string[] comboBonusWords = { "+0%", "+6%", "+12%", "+18%", "+24%", "+30%" };
        private static string[] countWords = { "", "1", "2", "3" };
        private static string[] timeWords = { "1ST TIME", "2ND TIME", "3RD TIME" };   // REPEATS, by pass

        //Judgement Words : "PERFECT  /  f" and so on, one per grade and choice, made in Load.
        //The grace and the roll get their own short words, also made once.
        private string[] judgeText = new string[15];
        private string[] graceText = new string[5];
        private string[] rollText = new string[5];
        private static string[] holdText = { "LET GO", "FERMATA" };

        //Band Panel Order : the same way round as the stage above it, back row (p) on the left to
        //front row (f) on the right, left side to right side inside each row
        private static int[] panelOrder = { 0, 1, 2, 3, 4, 5, 6, 7, 8 };

        //Front First : front row first, to find who stands nearest TACET
        private static int[] frontFirst = { 6, 7, 8, 3, 4, 5, 0, 1, 2 };

        //Story Lines : what the box at the bottom right says. The signature line and the enemy's
        //trait are written in Load, because they need names. Every line is wrapped once, in Load.
        private const int SayCall = 0;
        private const int SayLoud = 1;
        private const int SayHidden = 2;
        private const int SayQuiet = 3;
        private const int SayAnswer = 4;
        private const int SayReady = 5;
        private const int SaySignature = 6;
        private const int SayWin = 7;
        private const int SayAhead = 8;
        private const int SayEven = 9;
        private const int SayBehind = 10;
        private const int SayBreath = 11;
        private const int SaySoft = 12;
        private const int SayTrait = 13;
        private const int SayDouble = 14;
        private const int SayTremolo = 15;
        private const int SayFire = 16;
        private const int SayCounter = 17;
        private const int SayFinale = 18;
        private const int SayFinaleFail = 19;
        private const int SayFinaleWin = 20;
        private const int SayFermata = 21;
        private const int SayCollapse = 22;
        private const int SayAgain = 23;
        private const int SayRest = 24;
        private static string[] sayText =
        {
            "* It hums a phrase. Listen...",
            "* It swells. Loud f notes are coming. Answer big!",
            "* Something is hidden in its phrase.",
            "* It holds its breath.",
            "* Your turn. Answer every note, let the silent beats pass!",
            "* The band looks to you. Press SPACE.",
            "",
            "* Your band drowns it out!",
            "* The silence gives a little ground.",
            "* Neither side gives way.",
            "* The silence presses closer...",
            "* The band is gasping for air. One more blow could finish it!",
            "* It plays softly. A small answer saves your breath.",
            "",
            "* A note tied to a spark. Flick once more, any way, on the half beat!",
            "* It winds up a long trill. Shake the baton!",
            "* FORTISSIMO! The band is on fire!",
            "* You threw its loudest note right back at it!",
            "* The silence staggers. Finish the piece!",
            "* The ending falls apart. It claws its way back.",
            "* The last chord rings out. The silence breaks.",
            "* It draws out a long note. Stroke it, then hold still!",
            "* The band runs out of breath. The music stops.",
            "* It plays the phrase again. Read the marks, they change!",
            "* Silence. Let it pass or stroke small to breathe. A bigger swing is a free hit."
        };
        private const float SayWrap = 350f;
        private const float SaySpeed = 520f;     // pixels of text uncovered per second
        private string[] sayLineA = new string[sayText.Length];
        private string[] sayLineB = new string[sayText.Length];
        private float[] sayWidthA = new float[sayText.Length];
        private float[] sayWidthB = new float[sayText.Length];
        private int saying = -1;
        private float sayTimer;

        //Duel State
        private BattleState battle;
        private DuelEffects effects = new DuelEffects();
        private Phase phase;
        private float phaseTimer;
        private int beat;               // the beat on show: the one TACET is playing, or the one being answered
        private float time;
        private float displayLine;
        private float comboPulse;
        private float staminaFlash;     // the stamina bar lights up when it changes
        private float staminaJolt;      // the stamina plate shakes when TACET'S BLOW lands
        private bool breathWarned;      // the story box has warned about low breath this fight
        private float tugFlash;         // the tug marker lights up when the line is pushed
        private float fireGlow;         // how much of FORTISSIMO's light is on the stage
        private float fireFlash;        // the instant the band catches fire
        private float counterFlash;     // the instant of a COUNTER
        private float laneFlash;        // the lane lights up after a good hit, see DrawLane
        private float callPulse;        // TACET's sun flares when it plays a note, see DrawTacetSide
        private float staffEnergy;      // how hard the staff lines behind the band shake, see StageStaff
        private float[] lit = new float[StageLayout.SeatCount];
        private CharacterAnimator[] actors = new CharacterAnimator[StageLayout.SeatCount];
        private string roundEndLabel = "";
        private string bannerText = "";
        private string beatsLabel = "";
        private string tempoLabel = "";
        private string signatureName = "";

        //Round State : the whole round is one run of beats. TACET's note n sounds at n beats,
        //and is answered at n + 4 beats, so the answers start one bar after the call.
        //With REPEATS the notes are counted across the whole round: note g is beat g % 8 of
        //pass g / 8, and each pass starts passBeats after the one before.
        private float clock;            // seconds since TACET's first note of the round
        private float beatLen;          // seconds per beat at this round's tempo
        private int total;              // notes in the whole round, eight for every pass
        private int passBeats;          // beats one pass takes, ten when its last note lasts two
        private int called = -1;        // the last of TACET's notes that has sounded, -1 for none
        private int graceCalled = -1;   // the last note whose second note time has passed
        private float trillStart = -1f; // when TACET's latest roll began to sound, -1 for none yet
        private int trillTicks;         // how many ticks of that roll have sounded
        private int ticked = -1;        // the last beat the metronome clicked on

        //Fermata State : the stroke that opened the hold is kept until the hold ends, then the
        //beat is settled with it. heldTime only grows while the baton is still.
        private bool holding;
        private float holdEnd;          // when the held note ends, on the stroke clock
        private float holdClock;        // the clock at the last check
        private float holdSteady;       // the first moment after the stroke is never counted as moving
        private float heldTime;
        private Choice holdChoice;
        private Grade holdGrade;
        private bool holdSignature;

        //Trait Pops : each musician's trait name rises at most once in this many seconds, so a
        //trait that works every beat does not bury the stage in words
        private const float TraitPopGap = 1.6f;
        private float[] traitPopAt = new float[StageLayout.SeatCount];

        //Metronome : a soft click on every beat once the band is answering, so the pulse can be
        //heard all the way through the round
        private const float MetronomeVolume = 0.25f;

        //Low Breath : under this share of stamina the screen closes in with the beat, a warning
        private const float LowBreath = 0.25f;
        private int pending;            // the note waiting for its stroke, 0 to total - 1, total when all are in
        private bool onGrace;           // that beat's first note is answered, its pair is still due
        private bool rolling;           // TACET's roll is being answered
        private int rollStrokes;
        private float rollEnd;          // when the roll closes, on the stroke clock
        private float rollPulse;        // the roll counter jumps on every stroke
        private float doneAt = -1f;     // when the last answer went in
        private int barPush;            // how far the line moved over this bar
        private bool doubleTold;        // the story box has explained pairs once this fight
        private bool restTold;          // the story box has explained rests once this fight

        //Finale State : four strokes of the pattern, one bar after a one bar count
        private float finaleClock;      // below zero while the FINALE card is up
        private int finaleCalled;
        private int finaleStep;         // strokes landed so far, 0 to 4
        private bool finaleDone;
        private float finaleEnd;

        //Pause State : ESC or the window going to the back opens the pause menu, which stops
        //this page. Coming back, the band counts in for three beats.
        private bool paused;
        private float countIn;
        private int countShown;

        //Baton State
        private GestureReader gesture = new GestureReader();
        private HandAnim hand = new HandAnim();
        private float cutIn;            // counts down while the signature picture is on screen
        private bool cutInFinale;       // the picture is the finale's, not the signature's

        //Baton : the stick is held at the pointer and swings behind it like a blade (Core/Baton.cs)
        private Baton baton = new Baton();

        //Judgement : ONE word under the hit point at a time. A new judgement replaces the last,
        //so quick beats never pile words on top of each other. Its width is measured once.
        private string judgeWord = "";
        private float judgeTimer = 9f;
        private float judgeScale;
        private float judgeWidth;
        private int judgeTiming;        // EARLY / LATE : -1 early, +1 late, 0 nothing to say
        private bool judgeTune;         // IN TUNE : the stroke matched TACET's real mark
        private const string InTuneWord = "IN TUNE";
        private static string[] timingWord = { "EARLY", "", "LATE" };
        private const string restWord = "REST";

        //Clash State
        private bool clashShown;
        private float clashTimer;
        private int clashOurs;
        private int clashTheirs;
        private float hitStop;          // the whole duel freezes for a blink at the end of a round
        private float shake;            // how hard the world is shaking, fades by itself
        private float ripple;           // how hard the middle line is shuddering
        private float rippleY;

        public override void Load()
        {
            battle = Game.CurrentRun.Battle;
            displayLine = battle.Line;
            roundEndLabel = "END OF ROUND " + battle.Round;
            beatsLabel = "/ " + BattleRules.BeatsPerRound;
            signatureName = Game.CurrentRun.Conductor.MechanicName;

            //Tempo : this round's speed, which an enemy's trait may have changed
            beatLen = 60f / battle.Tempo;
            tempoLabel = battle.TempoLabel;

            //Judgement Words : made once here, so a beat never builds a string
            for (int g = 0; g < gradeWord.Length; g++)
            {
                for (int c = 0; c < choiceWord.Length; c++)
                    judgeText[g * 3 + c] = gradeWord[g].Length == 0 ? "" : gradeWord[g] + "  /  " + choiceWord[c];
                graceText[g] = gradeWord[g].Length == 0 ? "" : gradeWord[g] + "  /  FLICK";
                rollText[g] = gradeWord[g].Length == 0 ? "" : gradeWord[g] + "  /  ROLL";
            }

            for (int s = 0; s < actors.Length; s++) actors[s] = new CharacterAnimator();

            PrepareStory();

            //Baton Cursor : from here on the pointer IS the baton, so the ordinary mouse arrow
            //is put away until the duel is over
            Game.IsMouseVisible = false;
            gesture.Clear();
            baton.Reset(Input.MousePos);

            phase = Phase.Intro;
            phaseTimer = 0f;
            beat = 0;

            SoundBank.PlayMusic(RunFlow.FightMusic(Game.CurrentRun));
            SoundBank.Play(Sfx.RoundStart);
        }

        //Screen Leave : give the ordinary mouse pointer back to the rest of the game
        public override void Leave()
        {
            Game.IsMouseVisible = true;
        }

        //Paused : the pause menu is open. The pointer comes back so the menu can be clicked.
        public override void Paused()
        {
            paused = true;
            Game.IsMouseVisible = true;
        }

        //Resumed : the menu closed. The band counts three beats in if a bar was under way.
        public override void Resumed()
        {
            paused = false;
            Game.IsMouseVisible = false;
            gesture.Clear();

            //Fermata : a hold cannot survive a pause, it ends with what was held so far
            if (holding) FinishHold(pending);

            if (phase == Phase.Play || (phase == Phase.Finale && finaleClock >= 0f && !finaleDone))
            {
                countIn = 3f * beatLen;
                countShown = 0;
            }
        }

        //Story Prepare : wrap every line into at most two lines and measure them, once
        private void PrepareStory()
        {
            Conductor c = Game.CurrentRun.Conductor;

            for (int i = 0; i < sayText.Length; i++)
            {
                string text = sayText[i];
                if (i == SaySignature) text = "* " + c.Name + " : " + c.MechanicName + "!";
                if (i == SayTrait) text = battle.Enemy.TraitStory;

                string wrapped = Gfx.WrapText(Game.StoryFont, text, SayWrap, TextSize.Story);
                int cut = wrapped.IndexOf('\n');
                sayLineA[i] = cut < 0 ? wrapped : wrapped.Substring(0, cut);
                sayLineB[i] = cut < 0 ? "" : wrapped.Substring(cut + 1).Replace('\n', ' ');
                sayWidthA[i] = Game.StoryFont.MeasureString(sayLineA[i]).X * TextSize.Story;
                sayWidthB[i] = Game.StoryFont.MeasureString(sayLineB[i]).X * TextSize.Story;
            }
        }

        //Say : show a line in the story box, typed out from the start
        private void Say(int line)
        {
            saying = line;
            sayTimer = 0f;
        }

        //Judge Show : the word for the stroke that just landed, in place of the last one
        private void ShowJudge(string word, float scale)
        {
            if (word.Length == 0) return;
            judgeWord = word;
            judgeScale = scale;
            judgeTimer = 0f;
            judgeTiming = 0;
            judgeTune = false;
            judgeWidth = Game.BigFont.MeasureString(word).X * scale;
        }

        //Edge X : where the bright stage ends and TACET begins
        private float EdgeX()
        {
            float x = 640f + displayLine / BattleRules.LineLimit * 560f;
            if (x < 80f) x = 80f;
            if (x > 1200f) x = 1200f;
            return x;
        }

        //Enemy X : TACET's shape stands in the middle of its own black area
        private float EnemyX()
        {
            float edge = EdgeX();
            return edge + (TacetGame.ScreenW - edge) * 0.58f + 30f;
        }

        //Ring X : the hit point. It stays in the middle of the screen whatever the line does,
        //so the notes always travel the same distance and never bunch up.
        private float RingX()
        {
            return HitX;
        }

        //Beat Pulse : 1 right on a beat, falling quickly to 0 before the next one. The hit point,
        //the ring and TACET's eclipse swell with it, so the tempo can be seen as well as heard.
        private float BeatPulse()
        {
            float c = phase == Phase.Finale ? finaleClock : clock;
            if ((phase != Phase.Play && phase != Phase.Finale) || c < 0f) return 0f;
            float along = (c / beatLen) % 1f;
            float left = 1f - along;
            return left * left * left;
        }

        //Beat Of, Pass Of : note g of the round is this beat of TACET's phrase, in this time through
        private static int BeatOf(int g)
        {
            return g % BattleRules.BeatsPerRound;
        }

        private static int PassOf(int g)
        {
            return g / BattleRules.BeatsPerRound;
        }

        //Call At : when TACET plays note g of the round. Each pass starts passBeats after the last.
        private float CallAt(int g)
        {
            return (PassOf(g) * passBeats + BeatOf(g)) * beatLen;
        }

        //Answer At : when note g has to be answered, one bar after TACET played it
        private float AnswerAt(int g)
        {
            return CallAt(g) + 4f * beatLen;
        }

        //Stroke Clock : the clock a stroke is judged by. The player's own timing setting is taken
        //off here, and only here, so the whole duel follows it.
        private float StrokeClock()
        {
            return clock - Settings.TimingOffset;
        }

        //Early Limit : a stroke ending further before its beat than this is the hand getting
        //ready, and is not an answer at all
        private float EarlyLimit()
        {
            return Math.Min(BattleRules.EarlyTime, beatLen * 0.45f);
        }

        //Late Limit : after this the beat has gone by without a stroke
        private float LateLimit()
        {
            return Math.Min(BattleRules.LateTime, beatLen * 0.45f);
        }

        //Grace Late : the flick back has less room, the next beat is only half a beat away
        private float GraceLate()
        {
            return Math.Min(battle.GoodWindow, beatLen * 0.3f);
        }

        public override void Update(float dt)
        {
            time += dt;
            sayTimer += dt;

            //Baton : read every frame, whatever else is happening. It only counts while held.
            gesture.Update(dt, Input.MouseDown());
            baton.Update(dt, gesture.Held);
            hand.Update(dt);
            ripple = Math.Max(0f, ripple - dt * 1.8f);
            shake = Math.Max(0f, shake - dt * 3.5f);
            staminaFlash = Math.Max(0f, staminaFlash - dt * 2.5f);
            staminaJolt = Math.Max(0f, staminaJolt - dt * 3f);
            tugFlash = Math.Max(0f, tugFlash - dt * 2.5f);
            fireFlash = Math.Max(0f, fireFlash - dt * 3f);
            counterFlash = Math.Max(0f, counterFlash - dt * 4f);
            laneFlash = Math.Max(0f, laneFlash - dt * 4f);
            callPulse = Math.Max(0f, callPulse - dt * 3f);
            staffEnergy = Math.Max(0f, staffEnergy - dt * 1.5f);
            rollPulse = Math.Max(0f, rollPulse - dt * 6f);
            judgeTimer += dt;
            if (clashShown) clashTimer += dt;

            //Fire Light : fades in while the band is on fire, out when it is not.
            //On fire, the staff behind the band never stops singing.
            float fireWanted = battle.FortissimoLeft > 0 ? 1f : 0f;
            fireGlow += (fireWanted - fireGlow) * Math.Min(1f, dt * 5f);
            if (fireGlow > 0.5f) staffEnergy = Math.Max(staffEnergy, 1f);

            //Band Animation : every seated musician keeps breathing, whatever the phase
            Formation f = Game.CurrentRun.Formation;
            for (int s = 0; s < actors.Length; s++)
                if (f.Seated[s] != null) actors[s].Update(dt, f.Seated[s]);

            //Freeze : the signature picture stops the fight, and so does the accent at the end
            //of a round. The baton keeps following the mouse, and the sparks keep flying.
            if (cutIn > 0f)
            {
                cutIn -= dt;
                effects.Update(dt);
                if (cutIn <= 0f) gesture.Clear();      // movement during the picture never counts
                return;
            }
            if (hitStop > 0f)
            {
                hitStop -= dt;
                return;
            }

            //Count In : three ticks after a pause, then the round carries on from where it stopped
            if (countIn > 0f)
            {
                countIn -= dt;
                int left = (int)Math.Ceiling(countIn / beatLen);
                if (left != countShown && left > 0)
                {
                    countShown = left;
                    SoundBank.Play(Sfx.BeatTick, 0.8f, 0f);
                }
                if (countIn <= 0f) gesture.Clear();
                return;
            }

            //Waves : our sound rushes to the hit point and breaks there
            effects.LineX = HitX;
            effects.Update(dt);
            if (effects.Hit)
            {
                ripple = Math.Min(1.4f, ripple + 0.6f + effects.HitPower * 0.5f);
                rippleY = effects.HitY;
            }

            for (int s = 0; s < lit.Length; s++) lit[s] = Math.Max(0f, lit[s] - dt * 1.6f);
            comboPulse = Math.Max(0f, comboPulse - dt * 3f);
            displayLine += (battle.Line - displayLine) * Math.Min(1f, dt * 6f);

            //Left Hand : SPACE lets the signature loose once the recipe is complete
            if (Input.KeyPressed(Keys.Space)) ArmSignature();

            if (phase == Phase.Intro)
            {
                phaseTimer += dt;
                if (phaseTimer >= BattleRules.IntroTime) StartRound();
            }
            else if (phase == Phase.Play)
            {
                UpdatePlay(dt);
            }
            else if (phase == Phase.Finale)
            {
                UpdateFinale(dt);
            }
            else if (phase == Phase.RoundEnd)
            {
                phaseTimer += dt;
                if (phaseTimer >= BattleRules.RoundEndTime) LeaveRound();
            }
        }

        //Round Start : TACET is about to play. The story box warns what kind of bar comes first.
        //The very first bar of a fight tells the enemy's trait instead, so the player hears
        //the cause before they feel the effect.
        private void StartRound()
        {
            phase = Phase.Play;
            clock = 0f;
            called = -1;
            graceCalled = -1;
            trillStart = -1f;
            trillTicks = 0;
            pending = 0;

            //REPEATS : eight notes for every time through. When the last note is rolled or held
            //for two beats, the hand is busy right up to its end, so the next pass waits one more
            //beat after it: the same one beat to swing in as between any two notes.
            int lastBeats = 1;
            if (battle.TremoloBeat >= 0) lastBeats = (int)BattleRules.TremoloBeats;
            if (battle.FermataBeat >= 0) lastBeats = (int)BattleRules.FermataBeats;
            passBeats = BattleRules.BeatsPerRound + (lastBeats > 1 ? lastBeats : 0);
            total = BattleRules.BeatsPerRound * battle.Passes;
            onGrace = false;
            rolling = false;
            holding = false;
            ticked = -1;
            doneAt = -1f;
            barPush = 0;
            beat = 0;
            gesture.Clear();

            bool loud = false;
            bool hidden = false;
            int notes = 0;
            int soft = 0;
            for (int n = 0; n < 4; n++)
            {
                if (battle.EnemyHidden[n]) { hidden = true; continue; }
                if (battle.EnemyPower[n] <= 0) continue;
                notes++;
                if (battle.ShownChoice[n] == Choice.Boost) loud = true;
                if (battle.ShownChoice[n] == Choice.Ease) soft++;
            }

            if (battle.Round == 1 && battle.TraitShown) Say(SayTrait);
            else if (hidden) Say(SayHidden);
            else if (notes == 0) Say(SayQuiet);
            else if (loud) Say(SayLoud);
            else if (soft == notes) Say(SaySoft);
            else Say(SayCall);
        }

        //Play Update : TACET's part plays on its own, the answers wait for the baton
        private void UpdatePlay(float dt)
        {
            clock += dt;

            //Call : one of TACET's notes on each beat, all of the round's in a row
            while (called < total - 1 && clock >= CallAt(called + 1))
            {
                called++;
                CallNote(called);
            }

            //Grace Call : the second note of a pair sounds half a beat after the first
            while (graceCalled < called && clock >= CallAt(graceCalled + 1) + beatLen * 0.5f)
            {
                graceCalled++;
                if (battle.DoubleAt(PassOf(graceCalled), BeatOf(graceCalled))) CallGrace(BeatOf(graceCalled));
            }

            UpdateTrill();

            //Metronome : one click on every beat from the first answer on
            int beatNow = (int)(clock / beatLen);
            if (beatNow != ticked)
            {
                ticked = beatNow;
                if (beatNow >= 4) SoundBank.Play(Sfx.BeatTick, MetronomeVolume, 0f);
            }

            if (pending < total) UpdateAnswer();

            //Beat On Show : the note being played during the first bar, the one being answered after
            if (pending < total && clock >= AnswerAt(0) - beatLen * 0.5f) beat = BeatOf(pending);
            else beat = BeatOf(Math.Max(called, 0));

            //Round Over : every answer is in and the last clash has had its moment,
            //or the line reached an edge and the fight is decided
            if (pending >= total && doneAt < 0f) doneAt = clock;
            if (battle.Finished || (doneAt >= 0f && clock >= doneAt + BattleRules.PhraseTail))
                EndPlay();
        }

        //Call Note : TACET plays one note, as loud as its mark says. Its note leaves now and
        //reaches the hit point exactly one bar later, on the beat where it is answered.
        //FALSE NOTES lie in the sound as well as on the page. g counts across the whole round.
        private void CallNote(int g)
        {
            int n = BeatOf(g);
            int pass = PassOf(g);
            if (battle.PowerAt(pass, n) > 0)
            {
                Choice shown = battle.EnemyHidden[n] ? Choice.Normal : battle.ShownAt(pass, n);
                float volume = shown == Choice.Boost ? 1f : (shown == Choice.Ease ? 0.45f : 0.75f);
                if (!SoundBank.PlayCall(n, volume, 0f))
                    SoundBank.Play(Sfx.NoteOn, volume, shown == Choice.Boost ? -0.4f : 0.2f);

                effects.SpawnRipple(EnemyX(), EnemyFeetY, shown == Choice.Boost ? 170f : 110f, true);
                callPulse = shown == Choice.Boost ? 1f : 0.6f;       // its sun flares with the note
            }
            else
            {
                SoundBank.Play(Sfx.BeatTick, 0.5f, 0f);
            }

            if (battle.IsTremolo(n))
            {
                Say(SayTremolo);
                trillStart = CallAt(g);                          // its ticks sound from now, see UpdateTrill
                trillTicks = 0;
            }
            else if (battle.IsFermata(n)) Say(SayFermata);

            //Last Note Of The First Bar : the answers start on the next beat
            else if (g == 3) Say(SayAnswer);

            //REPEATS : the phrase starts over, and its marks may have changed
            else if (n == 0 && pass > 0) Say(SayAgain);
        }

        //Answer Update : the beat being answered, its window, and what happens if it is missed.
        //Each beat goes through at most three steps: the roll, or the stroke, then the flick back.
        private void UpdateAnswer()
        {
            int b = BeatOf(pending);
            float target = AnswerAt(pending);
            float now = StrokeClock();
            bool stroked = gesture.Read();

            if (holding) { UpdateHold(b, now); return; }
            if (rolling) { UpdateRoll(b, stroked, now); return; }
            if (onGrace) { UpdateGrace(b, stroked, now); return; }

            //Tremolo : the roll opens a moment before its beat and lasts two beats
            if (battle.IsTremolo(b))
            {
                if (now >= target - battle.GoodWindow)
                {
                    rolling = true;
                    rollStrokes = 0;
                    rollEnd = target + BattleRules.TremoloBeats * beatLen;
                    if (stroked) RollStroke();
                }
                return;
            }

            //Stroke : one made well before the beat is the hand getting ready, not an answer
            if (stroked && now >= target - EarlyLimit())
            {
                JudgeStroke(b, target);
                AfterStroke(b);
                return;
            }

            //No Stroke : the beat went by. Where TACET is silent that is a REST, the band breathes
            //and the combo holds. On one of TACET's notes it is a HESITATE, nobody answers.
            if (now > target + LateLimit())
            {
                if (battle.IsSilent(b))
                {
                    ResolveAnswer(b, Choice.Normal, Grade.None, false);
                    AdvanceBeat();
                    return;
                }
                SoundBank.Play(Sfx.QteHesitate);
                ResolveAnswer(b, Choice.Normal, Grade.Hesitate, false);
                AfterStroke(b);
            }
        }

        //After Stroke : a pair waits for its flick back, anything else moves to the next beat
        private void AfterStroke(int b)
        {
            if (holding) return;                         // the fermata settles the beat itself
            if (battle.EnemyDouble[b] && !battle.Finished) onGrace = true;
            else AdvanceBeat();
        }

        //Beat Advance : move on, and sum up a bar whenever one is finished
        private void AdvanceBeat()
        {
            pending++;
            onGrace = false;
            int b = BeatOf(pending);
            if (b == 4 || b == 0) EndBar();

            //REPEATS : the first answer of the next time through, its marks take over
            if (b == 0 && pending < total) battle.BeginPass(PassOf(pending));
        }

        //Answer Resolve : ask the rules what happened on this beat, then show it at once.
        //TACET's note reaches the hit point on the beat, so the clash happens right here.
        private void ResolveAnswer(int b, Choice choice, Grade grade, bool signature)
        {
            int comboBefore = battle.Combo;
            bool wasReady = battle.SignatureReady;
            bool falseNote = battle.EnemyPower[b] > 0 && !battle.EnemyHidden[b] && battle.ShownChoice[b] != battle.EnemyChoice[b];

            BeatResult r = battle.Resolve(b, choice, grade);
            barPush += r.Push;
            if (r.InTune && !r.Counter) judgeTune = true;             // the COUNTER has a word of its own
            if (r.StaminaChange != 0) staminaFlash = 1f;

            //Our Sound : whoever came in lights up, plays (or flinches, if TACET won the beat),
            //and their sound spreads on the floor under their feet
            Formation f = Game.CurrentRun.Formation;
            for (int s = 0; s < StageLayout.SeatCount; s++)
            {
                if (f.Seated[s] == null) continue;
                bool played = r.Joined[s] || (s == r.HeldSeat && r.Played);
                if (played)
                {
                    lit[s] = 1f;
                    actors[s].Play(r.Push < 0 && grade != Grade.None ? CharacterAnim.Hurt : CharacterAnim.Attack);
                    Rectangle stand = StandRect(s);
                    effects.SpawnRipple(stand.Center.X, stand.Bottom, 80f, false);
                }
                if (r.TraitFired[s]) PopTrait(s);
            }

            //Rest : the baton let a silent beat pass. The story box says what a rest is, once.
            if (grade == Grade.None)
            {
                SoundBank.Play(Sfx.RestRecover);
                if (r.Rested) ShowJudge(restWord, 0.45f);
                if (!restTold)
                {
                    restTold = true;
                    Say(SayRest);
                }
                return;
            }

            //Melody : the band's note for this beat, only when somebody really plays it.
            //Loud for BOOST, soft for EASE, a little sour for a MISS, and nothing at all when
            //the conductor hesitates. The silence is the punishment. On fire, it rings brighter.
            if (r.Played && grade != Grade.Hesitate && !r.Fermata)          // a fermata sang when it was struck
            {
                float volume = choice == Choice.Boost ? 1f : (choice == Choice.Ease ? 0.5f : 0.8f);
                float pitch = grade == Grade.Miss ? -0.12f : (r.Fortissimo ? 0.08f : 0f);
                if (r.Fortissimo) volume = 1f;
                SoundBank.PlayAnswer(b, volume, pitch);
            }

            if (grade == Grade.Hesitate) ShowJudge(gradeWord[(int)Grade.Hesitate], 0.55f);
            if (falseNote)
                effects.SpawnPop(RingX() + 90f, RingY - 60f, "FALSE NOTE", Palette.Highlight, 0.45f);

            if (r.OurPower > 0) effects.SpawnWave(BandX, RingY, 1f, r.OurPower / 14f, AnswerWaveSpeed);
            StartClash(r, grade);
            if (r.Counter) ShowCounter();

            //Notes : a good beat feeds the signature, but the signature itself does not
            if (!signature && (grade == Grade.Perfect || grade == Grade.Good)) battle.AddNotes(r);
            if (!wasReady && battle.SignatureReady && !battle.SignatureArmed)
            {
                Say(SayReady);
                effects.SpawnPop(bandPanel.Center.X, bandPanel.Y - 24, "SIGNATURE READY  -  SPACE", Palette.Accent, 0.5f);
                SoundBank.Play(Sfx.ComboUp, 1f, 0.5f);
            }

            ShowCombo(r, comboBefore);
            ShowFire(r);

            //Special Moments : rare, so they still get a word of their own
            ShowBreath(r);
            if (r.Fired)
                effects.SpawnPop(stageBox.Center.X, stageBox.Y + 60, "RUNAWAY FIRE", Palette.Accent, 0.5f);
        }

        //Combo Show : grows on a PERFECT, breaks on a miss or no stroke
        private void ShowCombo(BeatResult r, int comboBefore)
        {
            if (r.Combo > comboBefore && r.Combo >= 2)
            {
                comboPulse = 1f;
                SoundBank.Play(Sfx.ComboUp, 1f, Math.Min(0.5f, r.Combo * 0.08f));
            }
            if (r.ComboBroken) SoundBank.Play(Sfx.ComboBreak);
        }

        //Breath Show : what the beat did to the band's breath.
        //   TACET'S BLOW  its note flies on into the band, the stamina plate jolts, the loss pops up
        //   SECOND WIND   the motif caught the band this once
        //   COLLAPSE      out of breath, the fight is over
        //   low breath    the first time it drops under a quarter, the story box warns
        private void ShowBreath(BeatResult r)
        {
            RunState run = Game.CurrentRun;
            if (r.Blow > 0)
            {
                Vector2 target = BlowTarget(r);
                effects.SpawnBlow(HitX, RingY, target.X, target.Y, r.Blow / 16f);
                staminaJolt = 1f;
                staminaFlash = 1f;
                shake = Math.Max(shake, Math.Min(1f, 0.35f + r.Blow / 24f));
                effects.SpawnPop(310f, 100f, NumberText.Signed(-r.Blow), Palette.Highlight, 0.5f);
            }

            if (r.SecondWind)
                effects.SpawnPop(stageBox.Center.X, stageBox.Y + 60, "SECOND WIND", Palette.Accent, 0.6f);

            if (r.Collapsed)
            {
                effects.SpawnPop(stageBox.Center.X, stageBox.Y + 20, "OUT OF BREATH", Palette.Highlight, 0.8f);
                SoundBank.Play(Sfx.StaminaEmpty);
                Say(SayCollapse);
            }
            else if (!breathWarned && run.Stamina <= run.MaxStamina * LowBreath)
            {
                breathWarned = true;
                Say(SayBreath);
            }
        }

        //Blow Target : the front most player who came in takes TACET's blow, or the middle of
        //the band when nobody answered it
        private Vector2 BlowTarget(BeatResult r)
        {
            Formation f = Game.CurrentRun.Formation;
            for (int i = 0; i < frontFirst.Length; i++)
            {
                int s = frontFirst[i];
                if (f.Seated[s] != null && r.Joined[s])
                {
                    Rectangle stand = StandRect(s);
                    return new Vector2(stand.Center.X, stand.Center.Y);
                }
            }
            return new Vector2(stageBox.Center.X - 20, stageBox.Center.Y + 40);
        }

        //Trait Pop : the musician's trait name rises over their head when it changes a beat
        private void PopTrait(int seat)
        {
            Musician m = Game.CurrentRun.Formation.Seated[seat];
            if (m == null || m.TraitName.Length == 0) return;
            if (time - traitPopAt[seat] < TraitPopGap && traitPopAt[seat] > 0f) return;
            traitPopAt[seat] = time;
            Rectangle stand = StandRect(seat);
            effects.SpawnPop(stand.Center.X, stand.Y - 20, m.TraitName, Palette.Highlight, 0.36f);
        }

        //Clash Start : our sound and TACET's meet at the hit point. The world shakes, the line
        //shudders, a streak of light cuts across, shards fly toward the side that lost, and the
        //two numbers count up above the lane.
        private void StartClash(BeatResult r, Grade grade)
        {
            clashShown = true;
            clashTimer = 0f;
            clashOurs = r.OurPower;
            clashTheirs = r.EnemyPower;

            float gap = Math.Abs(r.OurPower - r.EnemyPower) / 14f;
            shake = Math.Min(1f, 0.3f + gap * 0.8f);
            ripple = Math.Min(1.4f, ripple + 0.5f + gap * 0.5f);
            rippleY = RingY;
            if (r.Push != 0) tugFlash = 1f;

            //Hollow Hit : a streak for every clash, brightest for a PERFECT or the signature
            float strength = grade == Grade.Perfect ? 1f : (grade == Grade.Good ? 0.6f : 0.3f);
            if (r.Signature) strength = 1.4f;
            effects.SpawnFlare(HitX, RingY, strength);
            float lean = r.Push > 0 ? 1f : (r.Push < 0 ? -1f : 0f);
            effects.SpawnSparks(HitX, RingY, Math.Min(24, 6 + (int)(gap * 10f) + (grade == Grade.Perfect ? 6 : 0)), lean);

            //Hit Burst : rings of light as good as the grade, the lane lights up after a good
            //stroke, and when the band wins the beat TACET's note breaks apart and the staff
            //behind the band rings like a plucked string
            effects.SpawnHit(HitX, RingY, grade);
            if (grade == Grade.Perfect) laneFlash = 1f;
            else if (grade == Grade.Good) laneFlash = 0.6f;
            if (r.Push > 0 && r.EnemyPower > 0) effects.SpawnShatter(HitX, RingY, 30f);
            if (r.Push > 0) staffEnergy = Math.Min(1.5f, staffEnergy + 0.5f + gap * 0.5f);

            if (r.Push > 0) SoundBank.Play(Sfx.ClashWin);
            else if (r.Push < 0) SoundBank.Play(Sfx.ClashLose);
            else SoundBank.Play(Sfx.ClashEven);

            if (r.EnemyChoice == Choice.Boost) SoundBank.Play(Sfx.EnemyBoost);
            if (r.EnemyChoice == Choice.Ease) SoundBank.Play(Sfx.EnemyEase);
        }

        //Bar End : a bar is over. A shake marks it, and the story box says how it went.
        //The picture does not freeze in the middle of a round, the beat has to keep going.
        private void EndBar()
        {
            shake = Math.Max(shake, Math.Min(1f, 0.4f + Math.Abs(barPush) / 60f));

            if (barPush >= 25) Say(SayWin);
            else if (barPush > 4) Say(SayAhead);
            else if (barPush < -4) Say(SayBehind);
            else Say(SayEven);
            barPush = 0;
        }

        //Play End : the round's beats are done. A short freeze, then either the finale (when the
        //line is far enough our way) or the end of the round.
        private void EndPlay()
        {
            hitStop = BattleRules.HitStopTime;
            rolling = false;
            onGrace = false;

            if (battle.FinaleOffered) StartFinale();
            else EndRound();
        }

        //Round End : pick the banner without changing the battle yet
        private void EndRound()
        {
            phase = Phase.RoundEnd;
            phaseTimer = 0f;

            if (battle.Finished && battle.Collapsed)
                bannerText = "OUT OF BREATH";
            else if (battle.Finished)
                bannerText = battle.PlayerWon ? "THE SILENCE BREAKS" : "THE SILENCE WINS";
            else if (battle.Round >= BattleRules.MaxRounds)
                bannerText = battle.Line > 0f ? "YOU HOLD THE STAGE" : "TACET HOLDS THE STAGE";
            else
                bannerText = roundEndLabel;
        }

        //Signature Arm : SPACE with the recipe complete. The conductor's picture sweeps across
        //the screen, and the next stroke becomes a PERFECT big stroke with the signature bonus on top.
        private void ArmSignature()
        {
            if (battle.SignatureArmed) return;
            if (!battle.SignatureReady)
            {
                SoundBank.Play(Sfx.UiDenied, 0.4f, 0f);
                return;
            }
            if (phase != Phase.Play || pending >= total || rolling || holding) return;

            battle.SpendNotes();
            battle.SignatureArmed = true;
            cutIn = BattleRules.CutInTime;
            cutInFinale = false;
            Say(SaySignature);
            SoundBank.Play(Sfx.ComboUp, 1f, 0.6f);
        }

        private void LeaveRound()
        {
            battle.EndRound();

            //Next Round : back to the stage page, to see TACET's next part and move the band
            if (battle.Finished)
                Game.Screens.Change(new ResultScreen());
            else
                Game.Screens.Change(new FormationScreen(true));
        }

        //Tug Split : where the marker sits on the tug bar
        private float TugSplitX(float line)
        {
            float t = (line / BattleRules.LineLimit + 1f) / 2f;
            return tugBar.X + tugBar.Width * t;
        }

        //Answer Timing : how far the ring of the stroke being waited for has closed.
        //Below 0 it has not started, 1 is the beat itself. Returns -1 when nothing is being answered.
        //The flick back of a pair closes over half a beat, the finale's strokes like any other.
        private float AnswerProgress()
        {
            if (phase == Phase.Finale)
            {
                if (finaleClock < 0f || finaleDone) return -1f;
                float due = (4 + finaleStep) * beatLen;
                return (finaleClock - (due - beatLen)) / beatLen;
            }

            if (phase != Phase.Play || pending >= total) return -1f;
            if (rolling || holding) return 1f;

            if (onGrace)
            {
                float grace = AnswerAt(pending) + beatLen * 0.5f;
                return (clock - (grace - beatLen * 0.5f)) / (beatLen * 0.5f);
            }

            float target = AnswerAt(pending);
            return (clock - (target - beatLen)) / beatLen;
        }

        //Wanted Way : the stroke the ring is asking for right now
        private Flick WantedWay()
        {
            if (phase == Phase.Finale) return pattern[Math.Min(finaleStep, 3)];
            if (onGrace) return Flick.None;              // the spark takes any way
            return pattern[BeatOf(pending) % 4];
        }

        //Live Size : how big the stroke being drawn is so far, 0 small, 1 middle, 2 big, so the
        //band can light up row by row while the baton travels. -1 when no stroke is under way.
        private int LiveSize()
        {
            if (phase != Phase.Play || !gesture.InStroke || rolling || holding || onGrace) return -1;
            if (AnswerProgress() < 0f) return -1;
            float along = Vector2.Dot(gesture.LiveVector, NoteGlyph.Way(WantedWay()));
            return Baton.SizeOf(along);
        }

        public override void Draw(SpriteBatch sb)
        {
            float edge = EdgeX();
            float danger = MathHelper.Clamp(-displayLine / BattleRules.LineLimit, 0f, 1f);

            //World Shake : the stage, TACET and the waves are drawn through a small offset that
            //jumps about while shake is above zero. Everything drawn afterwards does not shake,
            //so the lane, the numbers and the baton stay steady and readable.
            //ADVANCED PART : the SpriteBatch is ended and begun again with a transform matrix
            //that moves everything drawn inside it. The other settings are the same ones
            //TacetGame uses, so the picture looks exactly the same apart from the offset.
            Matrix world = Matrix.Identity;
            if (shake > 0f)
            {
                float amount = shake * shake * 10f;
                world = Matrix.CreateTranslation((float)Math.Sin(time * 91f) * amount, (float)Math.Cos(time * 73f) * amount, 0f);
            }
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, world);

            //World : see DuelScreen.Stage.cs
            DrawStageSide(sb);
            DrawFireLight(sb, edge);
            DrawStaff(sb, edge);
            effects.DrawRipples(sb, false);
            DrawMusicianGlow(sb);

            //Band : pixel characters need "point" sampling to stay sharp, see CharacterArt
            CharacterArt.BeginPixels(sb, world);
            DrawMusicians(sb);
            CharacterArt.EndPixels(sb, world);
            DrawMusicianMarks(sb);

            if (Game.CurrentRun.Stamina == 0)
                Gfx.Rect(sb, 0, 0, edge, TacetGame.ScreenH, Color.Black * 0.35f);    // out of breath, lights down
            TacetField.Draw(sb, edge, time, 0.2f + danger * 0.8f - fireGlow * 0.15f + callPulse * 0.15f, ripple, rippleY);
            DrawTacetSide(sb, edge, danger);
            effects.DrawWaves(sb);
            effects.DrawBursts(sb);
            effects.DrawSparks(sb);
            HandArt.Draw(sb, hand.Pose, hand.Frame, handBox, Palette.Ink, 1f);    // THE ART SLOT

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);

            //Low Breath : the edges close in with the beat, see DuelScreen.Hud.cs
            DrawLowBreath(sb);

            //Play Area : the lane, the notes and the ring never shake. The CUE beam runs under them.
            DrawCueBeam(sb);
            DrawLane(sb);
            DrawIncoming(sb);
            DrawFinaleNotes(sb);
            effects.DrawShards(sb);
            effects.DrawHits(sb);
            effects.DrawBlows(sb);
            effects.DrawSlashes(sb);
            DrawAnswerRing(sb);
            DrawReady(sb);
            DrawRoll(sb);
            DrawHold(sb);
            DrawJudge(sb);
            effects.DrawFlares(sb);
            DrawCounterFlash(sb);
            DrawClashNumbers(sb);

            //Top Strip : see DuelScreen.Hud.cs
            DrawPlates(sb);
            DrawTugBar(sb);
            DrawCombo(sb);

            //Bottom Panels : see DuelScreen.Panels.cs
            DrawSignaturePanel(sb);
            DrawBandPanel(sb);
            DrawStory(sb);

            effects.DrawPops(sb, Game.BigFont);

            if (phase == Phase.Intro) DrawBanner(sb, battle.RoundLabel, phaseTimer / BattleRules.IntroTime);
            if (phase == Phase.RoundEnd) DrawBanner(sb, bannerText, phaseTimer / BattleRules.RoundEndTime);
            if (phase == Phase.Finale && finaleClock < 0f) DrawFinaleCard(sb);
            if (cutIn > 0f) DrawCutIn(sb);
            if (countIn > 0f && !paused) DrawCountIn(sb);
            if (fireFlash > 0f) Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Color.White * (0.25f * fireFlash));

            //Baton : the size guide and the stick itself, over everything else. See DuelScreen.Baton.cs
            //While the pause menu is open the ordinary pointer is back, so the stick is hidden.
            if (!paused)
            {
                DrawStrokeGuide(sb);
                baton.Draw(sb, gesture);
            }
        }
    }
}
