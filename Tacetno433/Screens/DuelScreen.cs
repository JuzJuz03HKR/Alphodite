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
    //DuelScreen : one round of the fight, played as two phrases of call and answer.
    //
    //A ROUND is eight beats, played as two bars of four. Each bar is a PHRASE:
    //   CALL    TACET plays its four notes first, one on each beat. Every note leaves the right
    //           edge and slides along the lane for exactly one bar, so it reaches the hit point
    //           in the middle of the screen on the very beat where it has to be answered.
    //           Each note is written with how loud TACET plays it: f (loud), mf, or p (soft).
    //   ANSWER  the player conducts four beats in a row, at the round's tempo. The tempo rises
    //           each round (see BattleState.Tempo), so the fight speeds up as it goes on.
    //
    //THE MOUSE IS THE BATON, and it only counts while the LEFT BUTTON IS HELD. The strokes follow
    //a real conductor's 4/4 pattern: beat 1 DOWN, beat 2 LEFT, beat 3 RIGHT, beat 4 UP. How BIG a
    //stroke is gives the order: small EASE, middle PLAY, big BOOST. The beat lands where the
    //baton stops or turns. There is no long song: every answered beat plays the next note of
    //the band's melody, so the music only happens when the player conducts.
    //
    //Good beats give notes to the instrument families that played them. When the conductor's
    //recipe is complete, SPACE lets the signature loose: a cut-in, then the next stroke is a
    //PERFECT BOOST, harder still. ESC pauses; coming back, the band counts three beats in.
    //
    //THIS CLASS IS SPLIT OVER FIVE FILES, all called DuelScreen (the "partial" keyword lets one
    //class be written in several files; the compiler joins them back into one):
    //   DuelScreen.cs          the state, Load, Update, and the order of a phrase
    //   DuelScreen.Baton.cs    reading and judging a stroke, drawing the baton
    //   DuelScreen.Stage.cs    the stage, the band, TACET's eclipse, the lane and its notes
    //   DuelScreen.Hud.cs      the top strip, the ring, the clash numbers, banners, cut-in, pause
    //   DuelScreen.Panels.cs   the three panels along the bottom
    public partial class DuelScreen : GameScreen
    {
        private enum Phase { Intro, Phrase, BarEnd, RoundEnd }

        //Duel Layout
        private Rectangle stageBox = new Rectangle(40, 196, 600, 420);   // the band area, for pop ups
        private Rectangle tugBar = new Rectangle(440, 34, 400, 12);
        private const float HitX = 640f;                     // the hit point never moves
        private const float RingY = 372f;
        private const float LaneHalf = 34f;                  // half the height of the lane
        private const float RingStart = 120f;
        private const float RingTarget = 44f;
        private const float BandX = 330f;                    // our sound starts here
        private const float AnswerWaveSpeed = 1600f;         // fast, so it reaches the hit point on the beat
        private const float ClashY = 236f;                   // the clash numbers sit above the lane
        private const float EnemyFeetY = 520f;               // TACET's shape stands on this line
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
        private static string[] sizeWord = { "EASE", "PLAY", "BOOST" };

        private static string[] choiceWord = { "PLAY", "BOOST", "EASE" };   // in Choice order
        private static string[] dynamicMark = { "mf", "f", "p" };           // in Choice order, as music writes loudness
        private static string[] gradeWord = { "", "PERFECT", "GOOD", "MISS", "HESITATE" };
        private static string[] comboBonusWords = { "+0%", "+6%", "+12%", "+18%", "+24%", "+30%" };
        private static string[] countWords = { "", "1", "2", "3" };

        //Judgement Words : "PERFECT  /  BOOST" and so on, one per grade and choice, made in Load
        private string[] judgeText = new string[15];

        //Band Panel Order : front row first, the same order as the score page
        private static int[] panelOrder = { 6, 7, 8, 3, 4, 5, 0, 1, 2 };

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
        private static string[] sayText =
        {
            "* It hums a phrase. Listen...",
            "* It swells. Loud f notes are coming. Answer big!",
            "* Something is hidden in its phrase.",
            "* It holds its breath.",
            "* Your turn. Hold the mouse and conduct!",
            "* The band looks to you. Press SPACE.",
            "",
            "* Your band drowns it out!",
            "* The silence gives a little ground.",
            "* Neither side gives way.",
            "* The silence presses closer...",
            "* The band is gasping for air...",
            "* It plays softly. A small answer saves your breath.",
            ""
        };
        private const float SayWrap = 350f;
        private const float SaySpeed = 520f;     // pixels of text uncovered per second
        private string[] sayLineA = new string[14];
        private string[] sayLineB = new string[14];
        private float[] sayWidthA = new float[14];
        private float[] sayWidthB = new float[14];
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
        private float tugFlash;         // the tug marker lights up when the line is pushed
        private float[] lit = new float[StageLayout.SeatCount];
        private CharacterAnimator[] actors = new CharacterAnimator[StageLayout.SeatCount];
        private string roundEndLabel = "";
        private string bannerText = "";
        private string beatsLabel = "";
        private string tempoLabel = "";
        private string signatureName = "";

        //Phrase State : one phrase is TACET's bar (the call) followed by ours (the answer)
        private int bar;                // 0 or 1, which half of the round
        private float clock;            // seconds since the phrase began
        private float beatLen;          // seconds per beat at this round's tempo
        private int called = -1;        // the last note of the call that has sounded, -1 for none
        private int pending;            // the answer waiting for a stroke, 0 to 3, 4 when all are in
        private int barPush;            // how far the line moved over this bar

        //Pause State : ESC or the window going to the back stops the duel. Coming back, the band
        //counts in for three beats, so nobody has to find the beat again from nothing.
        private bool paused;
        private float countIn;
        private int countShown;

        //Baton State
        private GestureReader gesture = new GestureReader();
        private HandAnim hand = new HandAnim();
        private float strokeGlow;       // fades away right after a stroke is read
        private float cutIn;            // counts down while the signature picture is on screen

        //Baton Swing : the stick is held at the pointer and swings behind it like a blade
        private const float BatonLength = 118f;
        private const float BatonWidth = 10f;
        private const float BatonRest = -1.3f;      // radians : 0 points right, minus a half turn points up
        private const float BatonLowered = 0.9f;    // hanging down to the right while the button is up
        private float batonAngle = BatonRest;
        private float batonSpin;                    // how fast the angle is changing
        private Vector2 batonLast;
        private Vector2 batonVelocity;

        //Clash State
        private bool clashShown;
        private float clashTimer;
        private int clashOurs;
        private int clashTheirs;
        private float hitStop;          // the whole duel freezes for a blink at the end of a bar
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
                for (int c = 0; c < choiceWord.Length; c++)
                    judgeText[g * 3 + c] = gradeWord[g].Length == 0 ? "" : gradeWord[g] + "  /  " + choiceWord[c];

            for (int s = 0; s < actors.Length; s++) actors[s] = new CharacterAnimator();

            PrepareStory();

            //Baton Cursor : from here on the pointer IS the baton, so the ordinary mouse arrow
            //is put away until the duel is over
            Game.IsMouseVisible = false;
            gesture.Clear();
            batonLast = Input.MousePos;

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

        //Lost Focus : the window went to the back, so the duel waits for the player
        public override void LostFocus()
        {
            Pause();
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

        //Pause and Resume
        private void Pause()
        {
            if (paused) return;
            paused = true;
            Game.IsMouseVisible = true;
        }

        private void Resume()
        {
            paused = false;
            Game.IsMouseVisible = false;
            gesture.Clear();

            //Count In : only needed while the band is in the middle of a phrase
            if (phase == Phase.Phrase)
            {
                countIn = 3f * beatLen;
                countShown = 0;
            }
        }

        public override void Update(float dt)
        {
            //Pause Key : not during the signature picture, which is over in a second anyway
            if (Input.KeyPressed(Keys.Escape) && cutIn <= 0f)
            {
                if (paused) Resume();
                else Pause();
                return;
            }
            if (paused)
            {
                if (Input.MouseClicked()) Resume();
                return;
            }

            time += dt;
            sayTimer += dt;

            //Baton : read every frame, whatever else is happening. It only counts while held.
            gesture.Update(dt, Input.MouseDown());
            UpdateBaton(dt);
            hand.Update(dt);
            strokeGlow = Math.Max(0f, strokeGlow - dt * 2.5f);
            ripple = Math.Max(0f, ripple - dt * 1.8f);
            shake = Math.Max(0f, shake - dt * 3.5f);
            staminaFlash = Math.Max(0f, staminaFlash - dt * 2.5f);
            tugFlash = Math.Max(0f, tugFlash - dt * 2.5f);
            if (clashShown) clashTimer += dt;

            //Band Animation : every seated musician keeps breathing, whatever the phase
            Formation f = Game.CurrentRun.Formation;
            for (int s = 0; s < actors.Length; s++)
                if (f.Seated[s] != null) actors[s].Update(dt, f.Seated[s]);

            //Freeze : the signature picture stops the fight, and so does the accent at the end
            //of a bar. The baton keeps following the mouse, and the sparks keep flying.
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

            //Count In : three ticks after a pause, then the phrase carries on from where it stopped
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
                if (phaseTimer >= BattleRules.IntroTime) StartPhrase(0);
            }
            else if (phase == Phase.Phrase)
            {
                UpdatePhrase(dt);
            }
            else if (phase == Phase.BarEnd)
            {
                phaseTimer += dt;
                if (phaseTimer >= BattleRules.BarEndTime)
                {
                    if (battle.Finished || bar >= 1) EndRound();
                    else StartPhrase(1);
                }
            }
            else if (phase == Phase.RoundEnd)
            {
                phaseTimer += dt;
                if (phaseTimer >= BattleRules.RoundEndTime) LeaveRound();
            }
        }

        //Phrase Start : TACET is about to play one bar. The story box warns what kind.
        //The very first phrase of a fight tells the enemy's trait instead, so the player hears
        //the cause before they feel the effect.
        private void StartPhrase(int which)
        {
            bar = which;
            phase = Phase.Phrase;
            clock = 0f;
            called = -1;
            pending = 0;
            barPush = 0;
            beat = bar * 4;
            gesture.Clear();

            bool loud = false;
            bool hidden = false;
            int notes = 0;
            int soft = 0;
            for (int k = 0; k < 4; k++)
            {
                int n = bar * 4 + k;
                if (battle.EnemyHidden[n]) { hidden = true; continue; }
                if (battle.EnemyPower[n] <= 0) continue;
                notes++;
                if (battle.ShownChoice[n] == Choice.Boost) loud = true;
                if (battle.ShownChoice[n] == Choice.Ease) soft++;
            }

            if (which == 0 && battle.Round == 1 && battle.TraitShown) Say(SayTrait);
            else if (hidden) Say(SayHidden);
            else if (notes == 0) Say(SayQuiet);
            else if (loud) Say(SayLoud);
            else if (soft == notes) Say(SaySoft);
            else Say(SayCall);
        }

        //Phrase Update : the call plays on its own, the answer waits for the baton
        private void UpdatePhrase(float dt)
        {
            clock += dt;

            //Call : one note of TACET's bar on each beat
            while (called < 3 && clock >= (called + 1) * beatLen)
            {
                called++;
                CallNote(bar * 4 + called);
            }

            if (pending < 4) UpdateAnswer();

            //Beat On Show : the note being played during the call, the one being answered after
            if (clock < 4f * beatLen) beat = bar * 4 + Math.Max(called, 0);
            else beat = bar * 4 + Math.Min(pending, 3);

            //Phrase Over : every answer is in and the last clash has had its moment,
            //or the line reached an edge and the fight is decided
            if (battle.Finished || (pending >= 4 && clock >= 8f * beatLen + BattleRules.PhraseTail))
                EndBar();
        }

        //Call Note : TACET plays one note, as loud as its mark says. Its note leaves now and
        //reaches the hit point exactly one bar later, on the beat where it is answered.
        //FALSE NOTES lie in the sound as well as on the page.
        private void CallNote(int n)
        {
            if (battle.EnemyPower[n] > 0)
            {
                Choice shown = battle.EnemyHidden[n] ? Choice.Normal : battle.ShownChoice[n];
                float volume = shown == Choice.Boost ? 1f : (shown == Choice.Ease ? 0.45f : 0.75f);
                if (!SoundBank.PlayCall(n, volume, 0f))
                    SoundBank.Play(Sfx.NoteOn, volume, shown == Choice.Boost ? -0.4f : 0.2f);

                effects.SpawnRipple(EnemyX(), EnemyFeetY, shown == Choice.Boost ? 170f : 110f, true);
            }
            else
            {
                SoundBank.Play(Sfx.BeatTick, 0.5f, 0f);
            }

            //Last Note Of The Call : the answer is next
            if (n % 4 == 3) Say(SayAnswer);
        }

        //Answer Update : the beat being answered, its window, and what happens if it is missed
        private void UpdateAnswer()
        {
            int k = pending;
            int b = bar * 4 + k;
            float target = (4 + k) * beatLen;
            float late = Math.Min(BattleRules.LateTime, beatLen * 0.45f);

            bool stroked = gesture.Read();

            //Silent Beat : nobody on either side, so nothing to conduct. The band breathes.
            if (!battle.HasAction(b))
            {
                if (clock >= target) ResolveAnswer(b, Choice.Normal, Grade.None, false);
                return;
            }

            //Stroke : one made more than half a beat early is the hand getting ready, not an answer
            if (stroked && clock >= target - beatLen * 0.5f)
            {
                JudgeStroke(b, target);
                return;
            }

            //Hesitate : the beat went by without a stroke
            if (clock > target + late)
            {
                SoundBank.Play(Sfx.QteHesitate);
                ResolveAnswer(b, Choice.Normal, Grade.Hesitate, false);
            }
        }

        //Answer Resolve : ask the rules what happened on this beat, then show it at once.
        //TACET's note reaches the hit point on the beat, so the clash happens right here.
        private void ResolveAnswer(int b, Choice choice, Grade grade, bool signature)
        {
            int comboBefore = battle.Combo;
            bool wasReady = battle.SignatureReady;
            bool falseNote = battle.EnemyPower[b] > 0 && !battle.EnemyHidden[b] && battle.ShownChoice[b] != battle.EnemyChoice[b];
            int held = battle.HeldNoteSeat(b);

            BeatResult r = battle.Resolve(b, choice, grade);
            pending++;
            barPush += r.Push;
            if (r.StaminaChange != 0) staminaFlash = 1f;

            //Our Sound : whoever played lights up, plays (or flinches, if TACET won the beat),
            //and their sound spreads on the floor under their feet
            Formation f = Game.CurrentRun.Formation;
            for (int s = 0; s < StageLayout.SeatCount; s++)
            {
                if (f.Seated[s] == null) continue;
                bool played = f.Plays(s, b) || (s == held && r.Played);
                if (played)
                {
                    lit[s] = 1f;
                    actors[s].Play(r.Push < 0 && grade != Grade.None ? CharacterAnim.Hurt : CharacterAnim.Attack);
                    Rectangle stand = StandRect(s);
                    effects.SpawnRipple(stand.Center.X, stand.Bottom, 80f, false);
                }
                if (r.TraitFired[s]) PopTrait(s);
            }

            if (grade == Grade.None)
            {
                SoundBank.Play(Sfx.RestRecover);
                return;
            }

            //Melody : the band's note for this beat, only when somebody really plays it.
            //Loud for BOOST, soft for EASE, a little sour for a MISS, and nothing at all when
            //the conductor hesitates. The silence is the punishment.
            if (r.Played && grade != Grade.Hesitate)
            {
                float volume = choice == Choice.Boost ? 1f : (choice == Choice.Ease ? 0.5f : 0.8f);
                SoundBank.PlayAnswer(b, volume, grade == Grade.Miss ? -0.12f : 0f);
            }

            if (grade == Grade.Hesitate)
                effects.SpawnPop(RingX(), RingY + 80f, gradeWord[(int)Grade.Hesitate], Palette.Highlight, 0.55f);
            if (falseNote)
                effects.SpawnPop(RingX() + 90f, RingY - 60f, "FALSE NOTE", Palette.Highlight, 0.45f);

            if (r.OurPower > 0) effects.SpawnWave(BandX, RingY, 1f, r.OurPower / 14f, AnswerWaveSpeed);
            StartClash(r, grade);

            //Notes : a good beat feeds the signature, but the signature itself does not
            if (!signature && (grade == Grade.Perfect || grade == Grade.Good)) battle.AddNotes(b);
            if (!wasReady && battle.SignatureReady && !battle.SignatureArmed)
            {
                Say(SayReady);
                effects.SpawnPop(bandPanel.Center.X, bandPanel.Y - 24, "SIGNATURE READY  -  SPACE", Palette.Accent, 0.5f);
                SoundBank.Play(Sfx.ComboUp, 1f, 0.5f);
            }

            //Combo : grows on a PERFECT, breaks on a miss or no stroke
            if (r.Combo > comboBefore && r.Combo >= 2)
            {
                comboPulse = 1f;
                SoundBank.Play(Sfx.ComboUp, 1f, Math.Min(0.5f, r.Combo * 0.08f));
            }
            if (r.ComboBroken) SoundBank.Play(Sfx.ComboBreak);

            //Special Moments : rare, so they still get a word of their own
            if (r.OutOfBreath)
            {
                effects.SpawnPop(stageBox.Center.X, stageBox.Y + 20, "OUT OF BREATH", Palette.Highlight, 0.6f);
                SoundBank.Play(Sfx.StaminaEmpty);
                Say(SayBreath);
            }
            if (r.SecondWind)
                effects.SpawnPop(stageBox.Center.X, stageBox.Y + 60, "SECOND WIND", Palette.Accent, 0.6f);
            if (r.Fired)
                effects.SpawnPop(stageBox.Center.X, stageBox.Y + 60, "RUNAWAY FIRE", Palette.Accent, 0.5f);
        }

        //Trait Pop : the musician's trait name rises over their head when it changes a beat
        private void PopTrait(int seat)
        {
            Musician m = Game.CurrentRun.Formation.Seated[seat];
            if (m == null || m.TraitName.Length == 0) return;
            Rectangle stand = StandRect(seat);
            effects.SpawnPop(stand.Center.X, stand.Y - 20, m.TraitName, Palette.Highlight, 0.36f);
        }

        //Trait Seat : the first seat whose player has this trait and plays this beat, or -1
        private int TraitSeat(MusicianTrait trait, int b)
        {
            Formation f = Game.CurrentRun.Formation;
            for (int s = 0; s < StageLayout.SeatCount; s++)
                if (f.Plays(s, b) && f.Seated[s].Trait == trait) return s;
            return -1;
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

            if (r.Push > 0) SoundBank.Play(Sfx.ClashWin);
            else if (r.Push < 0) SoundBank.Play(Sfx.ClashLose);
            else SoundBank.Play(Sfx.ClashEven);

            if (r.EnemyChoice == Choice.Boost) SoundBank.Play(Sfx.EnemyBoost);
            if (r.EnemyChoice == Choice.Ease) SoundBank.Play(Sfx.EnemyEase);
        }

        //Bar End : the phrase is over. A short freeze and a shake mark the end of the bar,
        //and the story box says how it went.
        private void EndBar()
        {
            phase = Phase.BarEnd;
            phaseTimer = 0f;
            hitStop = BattleRules.HitStopTime;
            shake = Math.Min(1f, 0.5f + Math.Abs(barPush) / 60f);

            if (barPush >= 25) Say(SayWin);
            else if (barPush > 4) Say(SayAhead);
            else if (barPush < -4) Say(SayBehind);
            else Say(SayEven);
        }

        //Round End : pick the banner without changing the battle yet
        private void EndRound()
        {
            phase = Phase.RoundEnd;
            phaseTimer = 0f;

            if (battle.Finished)
                bannerText = battle.PlayerWon ? "THE SILENCE BREAKS" : "THE SILENCE WINS";
            else if (battle.Round >= BattleRules.MaxRounds)
                bannerText = battle.Line > 0f ? "YOU HOLD THE STAGE" : "TACET HOLDS THE STAGE";
            else
                bannerText = roundEndLabel;
        }

        //Signature Arm : SPACE with the recipe complete. The conductor's picture sweeps across
        //the screen, and the next stroke becomes a PERFECT BOOST with the signature bonus on top.
        private void ArmSignature()
        {
            if (battle.SignatureArmed) return;
            if (!battle.SignatureReady)
            {
                SoundBank.Play(Sfx.UiDenied, 0.4f, 0f);
                return;
            }
            if (phase != Phase.Phrase || pending >= 4) return;

            battle.SpendNotes();
            battle.SignatureArmed = true;
            cutIn = BattleRules.CutInTime;
            Say(SaySignature);
            SoundBank.Play(Sfx.ComboUp, 1f, 0.6f);
        }

        private void LeaveRound()
        {
            battle.EndRound();

            if (battle.Finished)
                Game.Screens.Change(new ResultScreen());
            else
                Game.Screens.Change(new ScoreScreen());
        }

        //Tug Split : where the marker sits on the tug bar
        private float TugSplitX(float line)
        {
            float t = (line / BattleRules.LineLimit + 1f) / 2f;
            return tugBar.X + tugBar.Width * t;
        }

        //Answer Timing : how far the ring of the beat being answered has closed.
        //Below 0 it has not started, 1 is the beat itself. Returns -1 when nothing is being answered.
        private float AnswerProgress()
        {
            if (phase != Phase.Phrase || pending >= 4) return -1f;
            if (!battle.HasAction(bar * 4 + pending)) return -1f;

            float target = (4 + pending) * beatLen;
            return (clock - (target - beatLen)) / beatLen;
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
            effects.DrawRipples(sb, false);
            DrawMusicianGlow(sb);

            //Band : pixel characters need "point" sampling to stay sharp, see CharacterArt
            CharacterArt.BeginPixels(sb, world);
            DrawMusicians(sb);
            CharacterArt.EndPixels(sb, world);
            DrawMusicianMarks(sb);

            if (Game.CurrentRun.Stamina == 0)
                Gfx.Rect(sb, 0, 0, edge, TacetGame.ScreenH, Color.Black * 0.35f);    // out of breath, lights down
            TacetField.Draw(sb, edge, time, 0.2f + danger * 0.8f, ripple, rippleY);
            DrawTacetSide(sb, edge, danger);
            effects.DrawWaves(sb);
            effects.DrawBursts(sb);
            effects.DrawSparks(sb);
            HandArt.Draw(sb, hand.Pose, hand.Frame, handBox, Palette.Ink, 1f);    // THE ART SLOT

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);

            //Play Area : the lane, the notes and the ring never shake
            DrawLane(sb);
            DrawIncoming(sb);
            DrawAnswerRing(sb);
            effects.DrawFlares(sb);
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
            if (cutIn > 0f) DrawCutIn(sb);
            if (countIn > 0f && !paused) DrawCountIn(sb);

            //Baton : the size guide and the stick itself, over everything else. See DuelScreen.Baton.cs
            if (!paused)
            {
                DrawStrokeGuide(sb);
                DrawBaton(sb);
            }
            else
            {
                DrawPause(sb);
            }
        }
    }
}
