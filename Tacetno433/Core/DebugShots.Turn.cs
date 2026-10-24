using System;
using System.Collections.Generic;
using System.IO;
using Tacetno433.Battle;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //DebugShots.Turn : DEVELOPER TOOL, checks the rules of the turn battle (Alpha 7).
    //   Tacetno433.exe --shots turncheck --out <folder>      writes turncheck.txt
    //
    //Plays short scripted fights with Battle/TurnBattle.cs, no screen involved, and writes one
    //PASS or FAIL line per rule : life, the bar (ONE BAR, step 12), damage, parry, breath, the BENCH and
    //CALL, CUT IN, knocked out, win, lose, two enemies and an enemy whose target fell. The random numbers
    //use a fixed seed, so every run gives the same report. The expected numbers follow the
    //TURN BATTLE section of Data/BattleRules.cs : change a number there and update them here.
    public static partial class DebugShots
    {
        private static void TurnCheck(TacetGame game)
        {
            string report = "TACET TURN BATTLE CHECK\r\n";
            MusicianList.ResetRehearsals();

            //Traits Off : the checks of steps 1 to 9 are about the plain rules, so the musicians' own
            //traits (step 10) are switched off until the trait checks near the end, then put back
            MusicianTrait[] traits = new MusicianTrait[MusicianList.All.Length];
            for (int i = 0; i < traits.Length; i++)
            {
                traits[i] = MusicianList.All[i].Trait;
                MusicianList.All[i].Trait = MusicianTrait.None;
            }

            //ANNA strings power 5, KLARA winds power 4, OTTO percussion power 6
            //HUSH : its Pattern notes average 4, so each of its notes hits round(4 x 2.2) = 9
            List<Musician> band = new List<Musician> { MusicianList.All[0], MusicianList.All[1], MusicianList.All[2] };
            Enemy hush = EnemyList.All[0];
            Enemy deadAir = EnemyList.All[1];
            Enemy staticEnemy = EnemyList.All[3];
            NoteResult good = new NoteResult(Grade.Good, true, 1f);
            NoteResult perfect = new NoteResult(Grade.Perfect, true, 1f);
            NoteResult miss = new NoteResult(Grade.Miss, true, 1f);

            //Life, and the bar being written first
            TurnBattle b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            report += Same("life", "30/26/36/50", b.Party[0].Hp + "/" + b.Party[1].Hp + "/" + b.Party[2].Hp + "/" + b.Enemies[0].Hp);
            report += Same("plan first", "Plan 1 3 True", b.Step + " " + b.Round + " " + b.Breath + " " + (b.Current == null));
            List<Fighter> preview = new List<Fighter>();
            List<BarMove> previewMoves = new List<BarMove>();
            b.PreviewOrder(preview, previewMoves);
            report += Same("preview", "ANNA KLARA OTTO HUSH", Names(preview));

            //ONE BAR : the bar plays in the order the moves were written, the unwritten ATTACK after them
            Fighter enemy = b.Enemies[0];
            b.PlanAttack(b.Party[2], enemy);
            b.PlanAttack(b.Party[0], enemy);
            b.PlayBar();
            report += Same("written order", "OTTO ANNA KLARA HUSH", OrderNames(b));

            //ATTACK : no swipe, power x 2 (OTTO 12, ANNA 10, KLARA 8), 1 breath each
            int h1 = b.Attack();
            int h2 = b.Attack();
            int h3 = b.Attack();
            report += Same("attack", "12 10 8 20 6", h1 + " " + h2 + " " + h3 + " " + enemy.Hp + " " + b.Breath);

            //Enemy turn : an ordinary enemy plays 1 or 2 notes on floor 1, HUSH's first phrase has 2 of 9
            report += Same("enemy turn", "EnemyTurn HUSH", b.Step + " " + b.Current.Name);
            int notes = enemy.IntentNotes.Length;
            bool intentOk = notes == 2 && enemy.IntentNotes[0] == 9 && enemy.IntentTarget != null && !enemy.IntentTarget.Down;
            report += Same("intent", "True", intentOk.ToString());
            bool marksOk = enemy.IntentWays.Length == notes && enemy.IntentSizes.Length == notes;
            for (int i = 0; i < notes && marksOk; i++)
                marksOk = enemy.IntentWays[i] != Flick.None && enemy.IntentSizes[i] >= 0 && enemy.IntentSizes[i] <= 2;
            report += Same("intent marks", "True", marksOk.ToString());

            //PARRY every note PERFECT : no damage, 1 breath per note, the COUNTER hits HUSH for the target's power x 2,
            //and the next bar is written
            int counter = enemy.IntentTarget.Musician.Power * 2;
            int taken = b.TakeEnemyAttack(AllGrades(notes, Grade.Perfect));
            report += Same("parry perfect", "0 8 " + (20 - counter) + " Plan 2", taken + " " + b.Breath + " " + enemy.Hp + " " + b.Step + " " + b.Round);

            //Swipe table
            report += Same("swipe table", "1.3 1 0.7 0.4 0.4",
                TurnBattle.SwipeScale(Grade.Perfect, true) + " " + TurnBattle.SwipeScale(Grade.Good, true) + " " +
                TurnBattle.SwipeScale(Grade.Perfect, false) + " " + TurnBattle.SwipeScale(Grade.Miss, true) + " " +
                TurnBattle.SwipeScale(Grade.Hesitate, true));

            //Judge : PERFECT within 0.10 s, GOOD within 0.22 s (NORMAL), the wrong way is a MISS
            report += Same("judge", "Perfect Good Miss Miss", b.Judge(0f, true) + " " + b.Judge(0.15f, true) + " " + b.Judge(0.3f, true) + " " + b.Judge(0f, false));
            report += Same("note damage", "0 3 6", TurnBattle.NoteDamage(6, Grade.Perfect) + " " + TurnBattle.NoteDamage(6, Grade.Good) + " " + TurnBattle.NoteDamage(6, Grade.Miss));

            //PARRY GOOD lets half through (9 -> 5 a note), no swipe lets all of it through
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            PlayToEnemy(b);
            notes = b.Current.IntentNotes.Length;
            report += Same("parry good", (5 * notes).ToString(), b.TakeEnemyAttack(AllGrades(notes, Grade.Good)).ToString());
            b.Enemies[0].Hp = 50;                                                      // the band's 30 a bar would end HUSH first
            PlayToEnemy(b);
            notes = b.Current.IntentNotes.Length;
            report += Same("no parry", (9 * notes).ToString(), b.TakeEnemyAttack(null).ToString());

            //KNOCKED OUT : KLARA starts the fight down, has no move in the bar and gets up with 1 after it
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433), new int[] { 30, 0, 36 });
            b.PlayBar();
            report += Same("knocked out", "ANNA OTTO HUSH", OrderNames(b));
            report += Same("revive", "1 30", b.HpAfterBattle(b.Party[1]) + " " + b.HpAfterBattle(b.Party[0]));

            //Win : the last hit on the last enemy ends the fight
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Enemies[0].Hp = 1;
            b.PlayBar();
            b.Attack();
            report += Same("win", "Won True", b.Step + " " + (b.Current == null));

            //Lose : only OTTO is standing with 1 life, his ATTACK does not end HUSH and it hits him
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433), new int[] { 0, 0, 1 });
            b.PlayBar();
            report += Same("lose order", "OTTO HUSH", OrderNames(b));
            b.Attack();
            b.TakeEnemyAttack(null);
            report += Same("lose", "Lost", b.Step.ToString());

            //CUT IN : STATIC (speed 5) plays right after the band's second move, HUSH (speed 4) after the band
            b = new TurnBattle(band, new List<Enemy> { hush, staticEnemy }, 1, new Random(433));
            b.PlayBar();
            report += Same("cut in", "ANNA KLARA STATIC OTTO HUSH", OrderNames(b));

            //A move whose enemy fell goes to the next one standing, and the fallen enemy is skipped
            b.Enemies[0].Hp = 1;
            b.Attack();                                                                // ANNA silences HUSH
            report += Same("retarget move", "PlayerTurn KLARA STATIC", b.Step + " " + b.Current.Name + " " + b.CurrentMove.Target.Name);
            b.Attack();                                                                // KLARA
            report += Same("cut in turn", "EnemyTurn STATIC", b.Step + " " + b.Current.Name);
            b.TakeEnemyAttack(null);
            b.Attack();                                                                // OTTO, then HUSH is down
            report += Same("skip the fallen", "Plan 2", b.Step + " " + b.Round);

            //Target fell : an enemy whose chosen musician is down by its turn picks another one
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Enemies[0].IntentTarget.Hp = 0;
            PlayToEnemy(b);
            bool retarget = b.Step == TurnStep.EnemyTurn && b.Current.IntentTarget != null && !b.Current.IntentTarget.Down;
            report += Same("retarget", "True", retarget.ToString());

            //Breath never goes past the top
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            for (int i = 0; i < 40; i++)
            {
                b.Enemies[0].Hp = 50;
                Step(b, Grade.Perfect);
            }
            report += Same("breath top", BattleRules.TurnBreathMax.ToString(), b.Breath.ToString());

            //SKILLS (step 4) : two for each musician, every cost 2 to 4, every skill has notes
            bool skillsOk = SkillList.All.Length == MusicianList.All.Length * 2;
            for (int i = 0; i < SkillList.All.Length && skillsOk; i++)
                skillsOk = SkillList.All[i].Cost >= 2 && SkillList.All[i].Cost <= 4 && SkillList.All[i].Notes.Length > 0;
            report += Same("skills", "18 True", SkillList.All.Length + " " + skillsOk);

            //Reserved : a skill written into the bar holds its BREATH, so KLARA's SOFT AIR (2) cannot be written with 1 left
            Skill quickBow = SkillList.First(band[0]);
            Skill softAir = SkillList.First(band[1]);
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            bool wrote = b.PlanSkill(b.Party[0], quickBow, b.Enemies[0]);
            report += Same("reserved", "True 2 1 False", wrote + " " + b.Reserved + " " + b.BreathFree + " " + b.CanPlanSkill(b.Party[1], softAir));

            //Rewrite : a musician written again keeps one move, rubbing it out leaves none
            b.PlanAttack(b.Party[0], b.Enemies[0]);
            int moves = b.Bar.Count;
            b.Unplan(b.Party[0]);
            report += Same("rewrite", "1 0 0", moves + " " + b.Bar.Count + " " + b.Reserved);

            //QUICK BOW (ANNA) : PERFECT 5 x 2 x 1.3 x 0.5 = 6.5 -> 6, GOOD 5, MISS nothing, 2 DISSONANCE, 2 breath spent
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.PlanSkill(b.Party[0], quickBow, b.Enemies[0]);
            b.PlayBar();
            bool used = b.UseSkill(new NoteResult[] { perfect, good, miss });
            report += Same("quick bow", "True 39 2 1", used + " " + b.Enemies[0].Hp + " " + b.Enemies[0].Dissonance + " " + b.Breath);

            //DISSONANCE : KLARA 8 and OTTO 12 bring HUSH to 19, at the end of its turn it loses 2, then 1 stack is left
            b.Attack();
            b.Attack();
            b.TakeEnemyAttack(null);
            report += Same("dissonance", "17 1", b.Enemies[0].Hp + " " + b.Enemies[0].Dissonance);

            //OFF-BEAT : ANNA's ATTACK fills the gauge (90 + 10), KLARA then hits x1.5 (4 x 2 x 1.5 = 12),
            //OTTO 18, and HUSH loses its turn, so the next bar is written at once
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Enemies[0].OffBeat = 90;
            b.PlayBar();
            b.Attack();
            bool broke = b.Enemies[0].Staggered;
            b.Attack();
            b.Attack();
            report += Same("off-beat", "True 10 Plan 2 False", broke + " " + b.Enemies[0].Hp + " " + b.Step + " " + b.Round + " " + b.Enemies[0].Staggered);

            //COUNTER : every note parried PERFECT, the target hits back for its attack (power x 2)
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            PlayToEnemy(b);                                                            // HUSH 50 - 30 = 20
            Fighter struck = b.Current.IntentTarget;
            b.TakeEnemyAttack(AllGrades(b.Current.IntentNotes.Length, Grade.Perfect));
            report += Same("counter", (20 - struck.Musician.Power * 2).ToString(), b.Enemies[0].Hp.ToString());

            //STAND FIRM (OTTO) : GUARD and TAUNT, HUSH turns to him and each note does half (9 -> 5)
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.PlanSkill(b.Party[2], SkillList.Second(band[2]), null);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { good });
            bool aimed = b.Enemies[0].IntentTarget == b.Party[2];
            b.Attack();
            b.Attack();
            notes = b.Current.IntentNotes.Length;
            int guarded = b.TakeEnemyAttack(null);
            report += Same("guard taunt", "True " + (5 * notes), aimed + " " + guarded);

            //CRESCENDO : 2 stacks hit 30 percent harder (ANNA 5 x 2 x 1.3 = 13), a skill note MISSed loses them all
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Party[0].Crescendo = 2;
            b.Party[1].Crescendo = 2;
            b.PlanSkill(b.Party[1], softAir, null);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { miss, miss });                               // KLARA misses both notes
            int loud = b.Attack();                                                     // ANNA
            report += Same("crescendo", "0 13 2", b.Party[1].Crescendo + " " + loud + " " + b.Party[0].Crescendo);

            //MUTE : no skill can be written, and it wears off at the end of the musician's move
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Party[1].Mute = 1;
            bool canSkill = b.CanPlanSkill(b.Party[1], softAir);
            b.PlayBar();
            b.Attack();
            b.Attack();
            report += Same("mute", "False 0", canSkill + " " + b.Party[1].Mute);

            //MUTE after writing (a CUT IN can do it) : the skill turns into an ATTACK and costs nothing
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.PlanSkill(b.Party[0], quickBow, b.Enemies[0]);
            b.Party[0].Mute = 1;
            b.PlayBar();
            report += Same("muted move", "True 3", (b.CurrentMove.Skill == null) + " " + b.Breath);

            //SOFT AIR (KLARA) : 3 back per note that lands to the whole band, never past full life
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433), new int[] { 20, 26, 30 });
            b.PlanSkill(b.Party[1], softAir, null);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { perfect, new NoteResult(Grade.Good, false, 1f) });
            report += Same("soft air", "26 26 36 1", b.Party[0].Hp + " " + b.Party[1].Hp + " " + b.Party[2].Hp + " " + b.Breath);

            //THE FLOOR (BORIS) : one held note hits every enemy. PERFECT held to the end : 7 x 2 x 1.3 x 1.4 = 25.48 -> 25
            Musician boris = MusicianList.All[8];
            b = new TurnBattle(new List<Musician> { boris }, new List<Enemy> { hush, deadAir }, 1, new Random(433));
            b.PlanSkill(b.Party[0], SkillList.First(boris), null);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { perfect });
            report += Same("the floor", "25 25 0", b.Enemies[0].Hp + " " + b.Enemies[1].Hp + " " + b.Breath);

            //CADENZA : REQUIEM's third attack has one note more and each note x1.5 (13 -> 19.5 -> 20)
            b = new TurnBattle(band, new List<Enemy> { EnemyList.All[6] }, 1, new Random(433));
            for (int i = 0; i < 60 && !b.Enemies[0].IntentCadenza && b.Step != TurnStep.Lost; i++) Step(b, Grade.Good);
            report += Same("cadenza", "True 20 2", b.Enemies[0].IntentCadenza + " " + b.Enemies[0].IntentNotes[0] + " " + b.Enemies[0].Attacks);

            //ROLL : 5 shakes PERFECT, 3 GOOD, fewer a MISS, none at all a HESITATE (NORMAL)
            report += Same("roll", "Perfect Good Miss Hesitate", b.RollGrade(5) + " " + b.RollGrade(3) + " " + b.RollGrade(1) + " " + b.RollGrade(0));

            //CONDUCTOR (step 5) : a conductor with no kit has no gauge. With one, every PERFECT note
            //(a skill or a parry, an ATTACK has none) fills one diamond
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.PlanSkill(b.Party[0], quickBow, b.Enemies[0]);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { perfect, perfect, perfect });
            report += Same("no kit", "0 False", b.Signature + " " + b.CanSignature);
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Kit = TurnKit.Beethoven;
            b.PlanSkill(b.Party[0], quickBow, b.Enemies[0]);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { perfect, perfect, good });
            b.Attack();
            b.Attack();
            notes = b.Current.IntentNotes.Length;
            b.TakeEnemyAttack(AllGrades(notes, Grade.Perfect));
            report += Same("signature fill", (2 + notes).ToString(), b.Signature.ToString());

            //THE CLOSER THE LOUDER : the band at half its life hits x1.25 (KLARA written first, 4 x 2 x 1.25 = 10)
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433), new int[] { 15, 13, 18 });
            b.Kit = TurnKit.Beethoven;
            b.PlanAttack(b.Party[1], b.Enemies[0]);
            b.PlayBar();
            report += Same("beethoven", "10", b.Attack().ToString());

            //LIGHTNING : played while the bar is written. All four PERFECT, strength 1.3 : 14 x 1.3 = 18.2 -> 18 to HUSH,
            //thrown OFF-BEAT, and the bar is still being written
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Kit = TurnKit.Beethoven;
            b.Signature = BattleRules.TurnSignatureNeed;
            NoteResult[] four = { perfect, perfect, perfect, perfect };
            bool played = b.UseSignature(four);
            report += Same("lightning", "True 32 True Plan 0", played + " " + b.Enemies[0].Hp + " " + b.Enemies[0].Staggered + " " + b.Step + " " + b.Signature);

            //FEATHER : 3 strokes land, so 6 free ATTACKs at the end of the band's part, ANNA KLARA OTTO and round again
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Kit = TurnKit.Mozart;
            b.Signature = BattleRules.TurnSignatureNeed;
            b.UseSignature(new NoteResult[] { perfect, good, miss, perfect });
            b.PlayBar();
            report += Same("feather", "ANNA KLARA OTTO ANNA KLARA OTTO ANNA KLARA OTTO HUSH", OrderNames(b));

            //PRODIGY : three PERFECT notes in a row give 1 breath (QUICK BOW : 3 - 2 + 1 = 2)
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Kit = TurnKit.Mozart;
            b.PlanSkill(b.Party[0], quickBow, b.Enemies[0]);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { perfect, perfect, perfect });
            report += Same("prodigy", "2", b.Breath.ToString());

            //MOTIFS (step 6) : BREATH MARK starts with 5, CRESCENDO with 1 each, CON BRIO needs 4
            List<Motif> carried = new List<Motif> { MotifList.All[(int)MotifId.BreathMark], MotifList.All[(int)MotifId.Crescendo], MotifList.All[(int)MotifId.ConBrio] };
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.GiveMotifs(carried);
            report += Same("motif start", "5 1 4", b.Breath + " " + b.Party[0].Crescendo + " " + b.SignatureNeed);

            //OVERTURE doubles the first hit only (ANNA 10 -> 20, KLARA 8)
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.GiveMotifs(new List<Motif> { MotifList.All[(int)MotifId.Overture] });
            b.PlayBar();
            int first = b.Attack();
            int second = b.Attack();
            report += Same("overture", "20 8", first + " " + second);

            //RUBATO : QUICK BOW's three notes all MISSed still play at 0.6 (5 x 2 x 0.5 x 0.6 = 3 each)
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.GiveMotifs(new List<Motif> { MotifList.All[(int)MotifId.Rubato] });
            b.PlanSkill(b.Party[0], quickBow, b.Enemies[0]);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { miss, miss, miss });
            report += Same("rubato", "41", b.Enemies[0].Hp.ToString());

            //SECOND WIND : KLARA at 1 life is hit by one note and stays up with 1 (only once a fight)
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433), new int[] { 30, 1, 36 });
            b.GiveMotifs(new List<Motif> { MotifList.All[(int)MotifId.SecondWind] });
            PlayToEnemy(b);
            b.Current.IntentTarget = b.Party[1];
            b.Current.IntentNotes = new int[] { 6 };
            b.TakeEnemyAttack(null);
            report += Same("second wind", "1", b.Party[1].Hp.ToString());

            //BAND LIFE (step 6) : the run's breath numbers are the band's life, and a change hits each musician by share
            RunState run = SampleRun(game, "turncheck");
            run.RestoreAllStamina();
            int max = run.MaxStamina;
            run.ChangeStamina(-max / 2);
            bool halved = run.Roster.Count > 0;
            for (int i = 0; i < run.Roster.Count; i++)
                halved = halved && run.LifeOf(run.Roster[i]) == run.MaxLifeOf(run.Roster[i]) / 2;
            run.RestoreAllStamina();
            report += Same("band life", "True " + max, halved + " " + run.Stamina);

            //ENEMIES (step 8) : HUSH plays its phrases in turn (Dp Up first), and floor 2 makes it tougher (50 x 1.3, notes 8.8 x 1.2)
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            report += Same("phrase", "Down Up 0 0", b.Enemies[0].IntentWays[0] + " " + b.Enemies[0].IntentWays[1] + " " + b.Enemies[0].IntentSizes[0] + " " + b.Enemies[0].IntentSizes[1]);
            b = new TurnBattle(band, new List<Enemy> { hush }, 2, new Random(433));
            report += Same("floor 2", "65 2 11", b.Enemies[0].MaxHp + " " + b.Enemies[0].IntentNotes.Length + " " + b.Enemies[0].IntentNotes[0]);

            //ECHO FADES (HUSH, floor 2) : each of its 2 notes that gets through adds 1 DISSONANCE
            PlayToEnemy(b);
            Fighter echoed = b.Current.IntentTarget;
            b.TakeEnemyAttack(null);
            report += Same("echo fades", "2", echoed.Dissonance.ToString());

            //FILLS THE GAPS (DEAD AIR, floor 2) : a missed note hits x1.5, 11 -> 16.5 -> 16, two notes
            b = new TurnBattle(band, new List<Enemy> { deadAir }, 2, new Random(433));
            PlayToEnemy(b);
            report += Same("fills the gaps", "32", b.TakeEnemyAttack(null).ToString());

            //LULLABY (THE LULL, floor 2) : its third turn is a rest that heals 12 (KLARA alone, so it lives that long)
            b = new TurnBattle(Solo(1), new List<Enemy> { EnemyList.All[2] }, 2, new Random(433));
            Sturdy(b);
            for (int i = 0; i < 60 && !b.Enemies[0].IntentRest && b.Step != TurnStep.Lost; i++) Step(b, Grade.Good);
            b.Enemies[0].Hp = 30;
            b.PlayBar();
            b.Attack();                                                                // KLARA 8 -> 22
            int lullBefore = b.Enemies[0].Hp;
            string resting = b.Enemies[0].IntentRest + " " + b.Enemies[0].IntentNotes.Length;   // read before it acts, it plans again after
            b.TakeEnemyAttack(null);
            report += Same("lullaby", "True 0 12", resting + " " + (b.Enemies[0].Hp - lullBefore));

            //NO REST (WHITE NOISE, a CUT IN) : one note more (3), and parrying it gives no breath (ANNA and KLARA make it 5)
            b = new TurnBattle(band, new List<Enemy> { EnemyList.All[5] }, 1, new Random(433));
            PlayToEnemy(b);
            notes = b.Current.IntentNotes.Length;
            b.TakeEnemyAttack(AllGrades(notes, Grade.Perfect));
            report += Same("no rest", "3 5", notes + " " + b.Breath);

            //THE BARGAIN (THE DEVIL'S STRING, a CUT IN) : its attack steals 1 breath (5 -> 4)
            b = new TurnBattle(band, new List<Enemy> { EnemyList.All[8] }, 1, new Random(433));
            PlayToEnemy(b);
            b.TakeEnemyAttack(null);
            report += Same("bargain", "4", b.Breath.ToString());

            //UNFINISHED (REQUIEM) : below half life its next phrase (Lf Rf) comes mirrored and faster
            b = new TurnBattle(band, new List<Enemy> { EnemyList.All[6] }, 1, new Random(433));
            b.Enemies[0].Hp = 100;
            for (int i = 0; i < 20 && b.Round < 2; i++) Step(b, Grade.Hesitate);
            report += Same("unfinished", "True Right Left", b.Enemies[0].IntentFast + " " + b.Enemies[0].IntentWays[0] + " " + b.Enemies[0].IntentWays[1]);

            //FALSE NOTES (STATIC, floor 2) : exactly one note shows a size it is not
            b = new TurnBattle(band, new List<Enemy> { staticEnemy }, 2, new Random(433));
            int lies = 0;
            for (int i = 0; i < b.Enemies[0].IntentNotes.Length; i++)
                if (b.Enemies[0].IntentShown[i] != b.Enemies[0].IntentSizes[i]) lies++;
            report += Same("false notes", "1", lies.ToString());

            //ANSWER BETTER (THE NAMELESS MASTER) : after KLARA's SOFT AIR (Dp Up) it plays those notes back
            b = new TurnBattle(band, new List<Enemy> { EnemyList.All[7] }, 1, new Random(433));
            b.PlanSkill(b.Party[1], softAir, null);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { good, good });
            for (int i = 0; i < 20 && b.Round < 2; i++) Step(b, Grade.Hesitate);
            report += Same("answer better", "Down Up 0 0", b.Enemies[0].IntentWays[0] + " " + b.Enemies[0].IntentWays[1] + " " + b.Enemies[0].IntentSizes[0] + " " + b.Enemies[0].IntentSizes[1]);

            //ARCHIVE (step 7) : a lost run on floor 2 with 4 wins leaves 4 x 3 + 1 x 10 = 22 echoes, STRONGER BAND costs 30
            //and gives every musician 2 more life. FULL STAGE (step 12) costs 120 and starts fights with 3 on the field.
            //The real archive is put back afterwards.
            int keptEchoes = Archive.Echoes;
            int keptLife = Archive.All[Archive.Life].Level;
            int keptStage = Archive.All[Archive.Stage].Level;
            run.Floor = 2;
            run.BattlesWon = 4;
            run.Maestro = false;
            int earned = Archive.EchoesFor(run, false);
            Archive.Echoes = 29;
            bool tooPoor = Archive.Buy(Archive.Life);
            Archive.Echoes = 30;
            Archive.All[Archive.Life].Level = 0;
            bool bought = Archive.Buy(Archive.Life);
            report += Same("archive", "22 False True 0 32", earned + " " + tooPoor + " " + bought + " " + Archive.Echoes + " " + run.MaxLifeOf(MusicianList.All[0]));
            Archive.All[Archive.Stage].Level = 0;
            int before = Archive.StartBand;
            Archive.Echoes = 120;
            Archive.Buy(Archive.Stage);
            report += Same("full stage", "2 3 0", before + " " + Archive.StartBand + " " + Archive.Echoes);
            Archive.Echoes = keptEchoes;
            Archive.All[Archive.Life].Level = keptLife;
            Archive.All[Archive.Stage].Level = keptStage;

            //PRACTICE (step 9) : the tutorial's enemy is in no list (never in a run, never saved), 160 life, notes of 4
            b = new TurnBattle(band, new List<Enemy> { EnemyList.Practice }, 1, new Random(433));
            report += Same("practice enemy", "-1 160 4", Array.IndexOf(EnemyList.All, EnemyList.Practice) + " " + b.Enemies[0].MaxHp + " " + b.Enemies[0].IntentNotes[0]);

            report += TurnCheckBench(band, hush);
            for (int i = 0; i < traits.Length; i++) MusicianList.All[i].Trait = traits[i];
            report += TurnCheckStep10(band, hush, deadAir);
            File.WriteAllText(Path.Combine(outDir, "turncheck.txt"), report);
        }

        //Turn Check Bench : ONE BAR (step 12), the BENCH, CALL, PACKS and MAESTRO's harder notes
        private static string TurnCheckBench(List<Musician> band, Enemy hush)
        {
            string report = "";

            //Start Band : two on the field, OTTO waits on the BENCH with his life
            TurnBattle b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433), new int[] { 30, 26, 20 }, 0, 2);
            report += Same("start band", "2 1 OTTO 20", b.Party.Count + " " + b.Bench.Count + " " + b.Bench[0].Name + " " + b.BenchLife[0]);

            //CALL : 3 BREATH, he walks in with his own life and plays from the next bar, one call per bar
            bool could = b.CanCall(0);
            Fighter otto = b.Call(0);
            report += Same("call", "True 3 0 0 20 True", could + " " + b.Party.Count + " " + b.Bench.Count + " " + b.Breath + " " + otto.Hp + " " + otto.JustCalled);
            b.PlayBar();
            report += Same("called waits", "ANNA KLARA HUSH", OrderNames(b));
            b.Attack();
            b.Attack();
            b.TakeEnemyAttack(null);
            b.PlayBar();
            report += Same("called plays", "ANNA KLARA OTTO HUSH", OrderNames(b));

            //Call Cost : 3, then 4 for every call after the first (MAESTRO 5 and 6). One per bar.
            List<Musician> four = new List<Musician> { MusicianList.All[0], MusicianList.All[1], MusicianList.All[2], MusicianList.All[3] };
            b = new TurnBattle(four, new List<Enemy> { hush }, 1, new Random(433), null, 0, 2);
            b.Breath = 10;
            int firstCost = b.CallCost;
            b.Call(0);
            bool again = b.CanCall(0);
            int secondCost = b.CallCost;
            b.Maestro = true;
            report += Same("call cost", "3 False 4 6", firstCost + " " + again + " " + secondCost + " " + b.CallCost);

            //Field Most : six on the field, the seventh cannot be called
            List<Musician> seven = new List<Musician>();
            for (int i = 0; i < 7; i++) seven.Add(MusicianList.All[i]);
            b = new TurnBattle(seven, new List<Enemy> { hush }, 1, new Random(433), null, 0, 6);
            b.Breath = 10;
            report += Same("field most", "6 1 False", b.Party.Count + " " + b.Bench.Count + " " + b.CanCall(0));

            //MAESTRO : TACET's notes hit MaestroTurnDamage harder, 1 for now (HUSH still 9)
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Maestro = true;
            report += Same("maestro notes", "9", b.Enemies[0].IntentNotes[0].ToString());

            //PACKS : ordinary enemies that join a fight. Floor 1 early 0, floor 1 elite 0, floor 3 late 3, floor 3 boss 2
            RunState run = new RunState();
            run.Floor = 1;
            run.Stage = 2;
            run.StagesThisFloor = 8;
            string packs = run.PackEscorts(EnemyKind.Normal) + " " + run.PackEscorts(EnemyKind.Elite);
            run.Floor = 3;
            run.Stage = 7;
            packs += " " + run.PackEscorts(EnemyKind.Normal) + " " + run.PackEscorts(EnemyKind.Boss);
            report += Same("packs", "0 0 3 2", packs);

            return report;
        }

        //Step : one step of a fight with the plainest habit. The bar is played with everybody ATTACKing,
        //a skill written earlier is played with every note at this grade, the enemy's notes are answered with it.
        private static void Step(TurnBattle b, Grade grade)
        {
            if (b.Step == TurnStep.Plan) b.PlayBar();
            else if (b.Step == TurnStep.PlayerTurn)
            {
                BarMove move = b.CurrentMove;
                if (move.Skill == null) b.Attack();
                else
                {
                    NoteResult[] all = new NoteResult[move.Skill.Notes.Length];
                    for (int i = 0; i < all.Length; i++) all[i] = new NoteResult(grade, true, 1f);
                    b.UseSkill(all);
                }
            }
            else if (b.Step == TurnStep.EnemyTurn) b.TakeEnemyAttack(AllGrades(b.Current.IntentNotes.Length, grade));
        }

        //Play To Enemy : play the bar until it is TACET's turn
        private static void PlayToEnemy(TurnBattle b)
        {
            for (int i = 0; i < 20 && b.Step != TurnStep.EnemyTurn && b.Step != TurnStep.Won && b.Step != TurnStep.Lost; i++)
                Step(b, Grade.Good);
        }

        //Sturdy : a band of one gets lots of life, so the enemy's turns never end a check before it is done
        private static void Sturdy(TurnBattle b)
        {
            for (int i = 0; i < b.Party.Count; i++)
            {
                b.Party[i].MaxHp = 999;
                b.Party[i].Hp = 999;
            }
        }

        private static Grade[] AllGrades(int count, Grade grade)
        {
            Grade[] all = new Grade[count];
            for (int i = 0; i < count; i++) all[i] = grade;
            return all;
        }

        private static string OrderNames(TurnBattle b)
        {
            return Names(b.Order);
        }

        private static string Names(List<Fighter> list)
        {
            string s = "";
            for (int i = 0; i < list.Count; i++) s += (i > 0 ? " " : "") + list[i].Name;
            return s;
        }
    }
}
