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
    //PASS or FAIL line per rule : life, turn order, damage for each swipe, parry, breath,
    //knocked out, win, lose, two enemies and an enemy whose target fell. The random numbers
    //use a fixed seed, so every run gives the same report. The expected numbers follow the
    //TURN BATTLE section of Data/BattleRules.cs : change a number there and update them here.
    public static partial class DebugShots
    {
        private static void TurnCheck(TacetGame game)
        {
            string report = "TACET TURN BATTLE CHECK\r\n";
            MusicianList.ResetRehearsals();

            //ANNA strings power 5, KLARA winds power 4, OTTO percussion power 6
            //HUSH : its Pattern notes average 4, so each of its notes hits round(4 x 1.5) = 6
            List<Musician> band = new List<Musician> { MusicianList.All[0], MusicianList.All[1], MusicianList.All[2] };
            Enemy hush = EnemyList.All[0];
            Enemy deadAir = EnemyList.All[1];

            //Life and turn order
            TurnBattle b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            report += Same("life", "30/26/36/50", b.Party[0].Hp + "/" + b.Party[1].Hp + "/" + b.Party[2].Hp + "/" + b.Enemies[0].Hp);
            report += Same("order", "KLARA ANNA HUSH OTTO", OrderNames(b));
            report += Same("first turn", "PlayerTurn KLARA", b.Step + " " + b.Current.Name);
            report += Same("breath start", "3", b.Breath.ToString());

            //Swipes : PERFECT 4 x 2 x 1.3 = 10.4, GOOD 5 x 2 = 10
            Fighter enemy = b.Enemies[0];
            int hit = b.Attack(enemy, Grade.Perfect, true);
            report += Same("perfect hit", "10 40 4", hit + " " + enemy.Hp + " " + b.Breath);
            hit = b.Attack(enemy, Grade.Good, true);
            report += Same("good hit", "10 30 5", hit + " " + enemy.Hp + " " + b.Breath);

            //Enemy turn : 1 or 2 notes of 6 on floor 1, at a musician who is standing
            report += Same("enemy turn", "EnemyTurn HUSH", b.Step + " " + b.Current.Name);
            int notes = enemy.IntentNotes.Length;
            bool intentOk = notes >= 1 && notes <= 2 && enemy.IntentNotes[0] == 6 && enemy.IntentTarget != null && !enemy.IntentTarget.Down;
            report += Same("intent", "True", intentOk.ToString());
            bool marksOk = enemy.IntentWays.Length == notes && enemy.IntentSizes.Length == notes;
            for (int i = 0; i < notes && marksOk; i++)
                marksOk = enemy.IntentWays[i] != Flick.None && enemy.IntentSizes[i] >= 0 && enemy.IntentSizes[i] <= 2;
            report += Same("intent marks", "True", marksOk.ToString());

            //PARRY every note PERFECT : no damage, 1 breath per note, and the COUNTER hits HUSH for the target's power x 2
            Grade[] allPerfect = new Grade[notes];
            for (int i = 0; i < notes; i++) allPerfect[i] = Grade.Perfect;
            int counter = enemy.IntentTarget.Musician.Power * 2;
            int taken = b.TakeEnemyAttack(allPerfect);
            report += Same("parry perfect", "0 " + (5 + notes) + " " + (30 - counter), taken + " " + b.Breath + " " + enemy.Hp);

            //Wrong size : 6 x 2 x 0.7 = 8.4, then round two starts with KLARA again
            hit = b.Attack(enemy, Grade.Perfect, false);
            report += Same("wrong size", "8 " + (22 - counter), hit + " " + enemy.Hp);
            report += Same("round two", "2 KLARA", b.Round + " " + b.Current.Name);

            //Miss : 4 x 2 x 0.4 = 3.2
            hit = b.Attack(enemy, Grade.Miss, true);
            report += Same("miss", "3 " + (19 - counter), hit + " " + enemy.Hp);

            //Swipe table
            report += Same("swipe table", "1.3 1 0.7 0.4 0.4",
                TurnBattle.SwipeScale(Grade.Perfect, true) + " " + TurnBattle.SwipeScale(Grade.Good, true) + " " +
                TurnBattle.SwipeScale(Grade.Perfect, false) + " " + TurnBattle.SwipeScale(Grade.Miss, true) + " " +
                TurnBattle.SwipeScale(Grade.Hesitate, true));

            //Judge : PERFECT within 0.10 s, GOOD within 0.22 s (NORMAL), the wrong way is a MISS
            report += Same("judge", "Perfect Good Miss Miss", b.Judge(0f, true) + " " + b.Judge(0.15f, true) + " " + b.Judge(0.3f, true) + " " + b.Judge(0f, false));
            report += Same("note damage", "0 3 6", TurnBattle.NoteDamage(6, Grade.Perfect) + " " + TurnBattle.NoteDamage(6, Grade.Good) + " " + TurnBattle.NoteDamage(6, Grade.Miss));

            //ATTACK PART : ANNA strings left, KLARA winds centre, OTTO percussion right
            report += Same("attack part", "1 Left 0 Down 2 Right",
                TurnBattle.AttackSize(band[0]) + " " + TurnBattle.AttackWay(band[0]) + " " + TurnBattle.AttackSize(band[1]) + " " +
                TurnBattle.AttackWay(band[1]) + " " + TurnBattle.AttackSize(band[2]) + " " + TurnBattle.AttackWay(band[2]));

            //PARRY GOOD lets half through (6 -> 3 a note), no swipe lets all of it through
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Attack(b.Enemies[0], Grade.Good, true);
            b.Attack(b.Enemies[0], Grade.Good, true);
            notes = b.Current.IntentNotes.Length;
            Grade[] allGood = new Grade[notes];
            for (int i = 0; i < notes; i++) allGood[i] = Grade.Good;
            report += Same("parry good", (3 * notes).ToString(), b.TakeEnemyAttack(allGood).ToString());
            b.Attack(b.Enemies[0], Grade.Good, true);
            b.Attack(b.Enemies[0], Grade.Good, true);
            b.Attack(b.Enemies[0], Grade.Good, true);
            notes = b.Current.IntentNotes.Length;
            report += Same("no parry", (6 * notes).ToString(), b.TakeEnemyAttack(null).ToString());

            //KNOCKED OUT : KLARA starts the fight down, sits out the turn order and gets up with 1 after it
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433), new int[] { 30, 0, 36 });
            report += Same("knocked out", "ANNA HUSH OTTO", OrderNames(b));
            report += Same("revive", "1 30", b.HpAfterBattle(b.Party[1]) + " " + b.HpAfterBattle(b.Party[0]));

            //Win : the last hit on the last enemy ends the fight
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Enemies[0].Hp = 1;
            b.Attack(b.Enemies[0], Grade.Miss, true);
            report += Same("win", "Won True", b.Step + " " + (b.Current == null));

            //Lose : only OTTO is standing with 1 life, HUSH is faster and hits him
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433), new int[] { 0, 0, 1 });
            report += Same("lose order", "HUSH OTTO", OrderNames(b));
            b.TakeEnemyAttack(null);
            report += Same("lose", "Lost", b.Step.ToString());

            //Two enemies : the first one down leaves the fight, the other carries on
            b = new TurnBattle(band, new List<Enemy> { hush, deadAir }, 1, new Random(433));
            report += Same("two order", "KLARA ANNA HUSH DEAD AIR OTTO", OrderNames(b));
            b.Enemies[0].Hp = 1;
            b.Attack(b.Enemies[0], Grade.Good, true);
            report += Same("one down", "PlayerTurn ANNA", b.Step + " " + b.Current.Name);
            b.Attack(b.Enemies[1], Grade.Good, true);
            report += Same("skip the fallen", "EnemyTurn DEAD AIR", b.Step + " " + b.Current.Name);
            b.TakeEnemyAttack(null);
            b.Attack(b.Enemies[1], Grade.Good, true);
            report += Same("next round", "KLARA ANNA DEAD AIR OTTO", OrderNames(b));

            //Target fell : an enemy whose chosen musician is down by its turn picks another one
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Enemies[0].IntentTarget.Hp = 0;
            while (b.Step == TurnStep.PlayerTurn) b.Attack(b.Enemies[0], Grade.Good, true);
            bool retarget = b.Step == TurnStep.EnemyTurn && b.Current.IntentTarget != null && !b.Current.IntentTarget.Down;
            report += Same("retarget", "True", retarget.ToString());

            //Breath never goes past the top
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            for (int i = 0; i < 40; i++)
            {
                b.Enemies[0].Hp = 50;
                if (b.Step == TurnStep.PlayerTurn) b.Attack(b.Enemies[0], Grade.Good, true);
                else if (b.Step == TurnStep.EnemyTurn) b.TakeEnemyAttack(new Grade[] { Grade.Perfect, Grade.Perfect, Grade.Perfect, Grade.Perfect });
            }
            report += Same("breath top", BattleRules.TurnBreathMax.ToString(), b.Breath.ToString());

            //SKILLS (step 4) : two for each musician, every cost 2 to 4, every skill has notes
            bool skillsOk = SkillList.All.Length == MusicianList.All.Length * 2;
            for (int i = 0; i < SkillList.All.Length && skillsOk; i++)
                skillsOk = SkillList.All[i].Cost >= 2 && SkillList.All[i].Cost <= 4 && SkillList.All[i].Notes.Length > 0;
            report += Same("skills", "18 True", SkillList.All.Length + " " + skillsOk);

            //QUICK BOW (ANNA) : PERFECT 5 x 2 x 1.3 x 0.5 = 6.5 -> 6, GOOD 5, MISS nothing, 2 DISSONANCE, 2 breath spent
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Attack(b.Enemies[0], Grade.Good, true);                                  // KLARA 8, breath 4
            bool used = b.UseSkill(SkillList.First(band[0]), b.Enemies[0], new NoteResult[] {
                new NoteResult(Grade.Perfect, true, 1f), new NoteResult(Grade.Good, true, 1f), new NoteResult(Grade.Miss, true, 1f) });
            report += Same("quick bow", "True 31 2 2", used + " " + b.Enemies[0].Hp + " " + b.Enemies[0].Dissonance + " " + b.Breath);

            //DISSONANCE : at the end of HUSH's turn it loses 2, then 1 stack is left
            b.TakeEnemyAttack(null);
            report += Same("dissonance", "29 1", b.Enemies[0].Hp + " " + b.Enemies[0].Dissonance);

            //OFF-BEAT : a full gauge throws HUSH off the beat, the next hit is x1.5 (5 x 2 x 1.5 = 15) and it loses its turn
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Enemies[0].OffBeat = 90;
            b.Attack(b.Enemies[0], Grade.Perfect, true);                               // KLARA 10, the gauge fills
            bool broke = b.Enemies[0].Staggered;
            b.Attack(b.Enemies[0], Grade.Good, true);                                  // ANNA 15
            report += Same("off-beat", "True 25 PlayerTurn OTTO False", broke + " " + b.Enemies[0].Hp + " " + b.Step + " " + b.Current.Name + " " + b.Enemies[0].Staggered);

            //COUNTER : every note parried PERFECT, the target hits back for its attack (power x 2)
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Attack(b.Enemies[0], Grade.Good, true);                                  // KLARA 8
            b.Attack(b.Enemies[0], Grade.Good, true);                                  // ANNA 10, HUSH 32
            Fighter struck = b.Current.IntentTarget;
            notes = b.Current.IntentNotes.Length;
            allPerfect = new Grade[notes];
            for (int i = 0; i < notes; i++) allPerfect[i] = Grade.Perfect;
            b.TakeEnemyAttack(allPerfect);
            report += Same("counter", (32 - struck.Musician.Power * 2).ToString(), b.Enemies[0].Hp.ToString());

            //STAND FIRM (OTTO) : GUARD and TAUNT, HUSH turns to him and each note does half (6 -> 3)
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Attack(b.Enemies[0], Grade.Good, true);
            b.Attack(b.Enemies[0], Grade.Good, true);
            b.TakeEnemyAttack(null);
            b.UseSkill(SkillList.Second(band[2]), null, new NoteResult[] { new NoteResult(Grade.Good, true, 1f) });
            bool aimed = b.Enemies[0].IntentTarget == b.Party[2];
            b.Attack(b.Enemies[0], Grade.Good, true);
            b.Attack(b.Enemies[0], Grade.Good, true);
            notes = b.Current.IntentNotes.Length;
            int guarded = b.TakeEnemyAttack(null);
            report += Same("guard taunt", "True " + (3 * notes), aimed + " " + guarded);

            //CRESCENDO : 2 stacks hit 30 percent harder (5 x 2 x 1.3 = 13), a MISS loses them all
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Party[1].Crescendo = 2;
            b.Attack(b.Enemies[0], Grade.Miss, true);                                  // KLARA misses
            b.Party[0].Crescendo = 2;
            int loud = b.Attack(b.Enemies[0], Grade.Good, true);                       // ANNA
            report += Same("crescendo", "0 13 2", b.Party[1].Crescendo + " " + loud + " " + b.Party[0].Crescendo);

            //MUTE : no skills on that turn, and it wears off at the end of it
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Party[1].Mute = 1;
            bool canSkill = b.CanUse(SkillList.First(band[1]));
            b.Attack(b.Enemies[0], Grade.Good, true);
            report += Same("mute", "False 0", canSkill + " " + b.Party[1].Mute);

            //SOFT AIR (KLARA) : 3 back per note that lands to the whole band, never past full life
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433), new int[] { 20, 26, 30 });
            b.UseSkill(SkillList.First(band[1]), null, new NoteResult[] { new NoteResult(Grade.Perfect, true, 1f), new NoteResult(Grade.Good, false, 1f) });
            report += Same("soft air", "26 26 36 1", b.Party[0].Hp + " " + b.Party[1].Hp + " " + b.Party[2].Hp + " " + b.Breath);

            //THE FLOOR (BORIS) : one held note hits every enemy. PERFECT held to the end : 7 x 2 x 1.3 x 1.4 = 25.48 -> 25
            Musician boris = MusicianList.All[8];
            b = new TurnBattle(new List<Musician> { boris }, new List<Enemy> { hush, deadAir }, 1, new Random(433));
            while (b.Step == TurnStep.EnemyTurn) b.TakeEnemyAttack(null);
            b.UseSkill(SkillList.First(boris), null, new NoteResult[] { new NoteResult(Grade.Perfect, true, 1f) });
            report += Same("the floor", "25 25 0", b.Enemies[0].Hp + " " + b.Enemies[1].Hp + " " + b.Breath);

            //CADENZA : REQUIEM's third attack has one note more and each note x1.5 (9 -> 13.5 -> 14)
            b = new TurnBattle(band, new List<Enemy> { EnemyList.All[6] }, 1, new Random(433));
            for (int i = 0; i < 40 && !b.Enemies[0].IntentCadenza && b.Step != TurnStep.Lost; i++)
            {
                if (b.Step == TurnStep.PlayerTurn) b.Attack(b.Enemies[0], Grade.Miss, true);
                else b.TakeEnemyAttack(new Grade[] { Grade.Good, Grade.Good, Grade.Good, Grade.Good });
            }
            report += Same("cadenza", "True 14 2", b.Enemies[0].IntentCadenza + " " + b.Enemies[0].IntentNotes[0] + " " + b.Enemies[0].Attacks);

            //ROLL : 5 shakes PERFECT, 3 GOOD, fewer a MISS, none at all a HESITATE (NORMAL)
            report += Same("roll", "Perfect Good Miss Hesitate", b.RollGrade(5) + " " + b.RollGrade(3) + " " + b.RollGrade(1) + " " + b.RollGrade(0));

            //CONDUCTOR (step 5) : a conductor with no kit has no gauge, a PERFECT attack fills one diamond
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Attack(b.Enemies[0], Grade.Perfect, true);
            report += Same("no kit", "0 False", b.Signature + " " + b.CanSignature);
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Kit = TurnKit.Beethoven;
            b.Attack(b.Enemies[0], Grade.Perfect, true);
            b.Attack(b.Enemies[0], Grade.Good, true);
            notes = b.Current.IntentNotes.Length;
            allPerfect = new Grade[notes];
            for (int i = 0; i < notes; i++) allPerfect[i] = Grade.Perfect;
            b.TakeEnemyAttack(allPerfect);
            report += Same("signature fill", (1 + notes).ToString(), b.Signature.ToString());

            //THE CLOSER THE LOUDER : the band at half its life hits x1.25 (KLARA GOOD 4 x 2 x 1.25 = 10)
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433), new int[] { 15, 13, 18 });
            b.Kit = TurnKit.Beethoven;
            report += Same("beethoven", "10", b.Attack(b.Enemies[0], Grade.Good, true).ToString());

            //LIGHTNING : all four PERFECT, strength 1.3 : 14 x 1.3 = 18.2 -> 18 to HUSH, thrown OFF-BEAT, KLARA still up
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Kit = TurnKit.Beethoven;
            b.Signature = BattleRules.TurnSignatureNeed;
            NoteResult[] four = { new NoteResult(Grade.Perfect, true, 1f), new NoteResult(Grade.Perfect, true, 1f),
                                  new NoteResult(Grade.Perfect, true, 1f), new NoteResult(Grade.Perfect, true, 1f) };
            bool played = b.UseSignature(four);
            report += Same("lightning", "True 32 True PlayerTurn KLARA 0", played + " " + b.Enemies[0].Hp + " " + b.Enemies[0].Staggered + " " + b.Step + " " + b.Current.Name + " " + b.Signature);

            //FEATHER : 3 strokes land, so ANNA, KLARA and OTTO each get an extra turn right after KLARA
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Kit = TurnKit.Mozart;
            b.Signature = BattleRules.TurnSignatureNeed;
            b.UseSignature(new NoteResult[] { new NoteResult(Grade.Perfect, true, 1f), new NoteResult(Grade.Good, true, 1f),
                                              new NoteResult(Grade.Miss, true, 1f), new NoteResult(Grade.Perfect, true, 1f) });
            report += Same("feather", "KLARA ANNA KLARA OTTO ANNA HUSH OTTO", OrderNames(b));

            //PRODIGY : the third PERFECT in a row gives 1 breath (KLARA, ANNA, then the first parry)
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.Kit = TurnKit.Mozart;
            b.Attack(b.Enemies[0], Grade.Perfect, true);
            b.Attack(b.Enemies[0], Grade.Perfect, true);
            notes = b.Current.IntentNotes.Length;
            allPerfect = new Grade[notes];
            for (int i = 0; i < notes; i++) allPerfect[i] = Grade.Perfect;
            b.TakeEnemyAttack(allPerfect);
            report += Same("prodigy", Math.Min(BattleRules.TurnBreathMax, 6 + notes).ToString(), b.Breath.ToString());

            //MOTIFS (step 6) : BREATH MARK starts with 5, CRESCENDO with 1 each, CON BRIO needs 4
            List<Motif> carried = new List<Motif> { MotifList.All[(int)MotifId.BreathMark], MotifList.All[(int)MotifId.Crescendo], MotifList.All[(int)MotifId.ConBrio] };
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.GiveMotifs(carried);
            report += Same("motif start", "5 1 4", b.Breath + " " + b.Party[0].Crescendo + " " + b.SignatureNeed);

            //OVERTURE doubles the first hit only (KLARA GOOD 8 -> 16, ANNA 10), RUBATO : a MISS at 0.6 (OTTO 6 x 2 x 0.6 = 7.2 -> 7)
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            b.GiveMotifs(new List<Motif> { MotifList.All[(int)MotifId.Overture], MotifList.All[(int)MotifId.Rubato] });
            int first = b.Attack(b.Enemies[0], Grade.Good, true);
            int second = b.Attack(b.Enemies[0], Grade.Good, true);
            b.TakeEnemyAttack(null);
            int soft = b.Attack(b.Enemies[0], Grade.Miss, true);
            report += Same("overture rubato", "16 10 7", first + " " + second + " " + soft);

            //SECOND WIND : KLARA at 1 life is hit by one note and stays up with 1 (only once a fight)
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433), new int[] { 30, 1, 36 });
            b.GiveMotifs(new List<Motif> { MotifList.All[(int)MotifId.SecondWind] });
            b.Attack(b.Enemies[0], Grade.Good, true);
            b.Attack(b.Enemies[0], Grade.Good, true);
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

            //ENEMIES (step 8) : HUSH plays its phrases in turn (Dp Up first), and floor 2 makes it tougher (50 x 1.3)
            b = new TurnBattle(band, new List<Enemy> { hush }, 1, new Random(433));
            report += Same("phrase", "Down Up 0 0", b.Enemies[0].IntentWays[0] + " " + b.Enemies[0].IntentWays[1] + " " + b.Enemies[0].IntentSizes[0] + " " + b.Enemies[0].IntentSizes[1]);
            b = new TurnBattle(band, new List<Enemy> { hush }, 2, new Random(433));
            report += Same("floor 2", "65 2 7", b.Enemies[0].MaxHp + " " + b.Enemies[0].IntentNotes.Length + " " + b.Enemies[0].IntentNotes[0]);

            //ECHO FADES (HUSH, floor 2) : each of its 2 notes that gets through adds 1 DISSONANCE
            b.Attack(b.Enemies[0], Grade.Good, true);
            b.Attack(b.Enemies[0], Grade.Good, true);
            Fighter echoed = b.Current.IntentTarget;
            b.TakeEnemyAttack(null);
            report += Same("echo fades", "2", echoed.Dissonance.ToString());

            //FILLS THE GAPS (DEAD AIR, floor 2) : a missed note hits x1.5, 7 -> 10.5 -> 10, two notes
            b = new TurnBattle(band, new List<Enemy> { deadAir }, 2, new Random(433));
            b.Attack(b.Enemies[0], Grade.Good, true);
            b.Attack(b.Enemies[0], Grade.Good, true);
            report += Same("fills the gaps", "20", b.TakeEnemyAttack(null).ToString());

            //LULLABY (THE LULL, floor 2) : its third turn is a rest that heals 12
            b = new TurnBattle(band, new List<Enemy> { EnemyList.All[2] }, 2, new Random(433));
            for (int i = 0; i < 60 && !b.Enemies[0].IntentRest; i++)
            {
                if (b.Step == TurnStep.PlayerTurn) b.Attack(b.Enemies[0], Grade.Miss, true);
                else b.TakeEnemyAttack(new Grade[] { Grade.Good, Grade.Good, Grade.Good, Grade.Good });
            }
            b.Enemies[0].Hp = 30;
            for (int i = 0; i < 10 && b.Step == TurnStep.PlayerTurn; i++) b.Attack(b.Enemies[0], Grade.Miss, true);
            int lullBefore = b.Enemies[0].Hp;
            string resting = b.Enemies[0].IntentRest + " " + b.Enemies[0].IntentNotes.Length;   // read before it acts, a new round plans again
            b.TakeEnemyAttack(null);
            report += Same("lullaby", "True 0 12", resting + " " + (b.Enemies[0].Hp - lullBefore));

            //NO REST (WHITE NOISE) : one note more, and parrying it gives no breath (KLARA's attack makes it 4)
            b = new TurnBattle(band, new List<Enemy> { EnemyList.All[5] }, 1, new Random(433));
            b.Attack(b.Enemies[0], Grade.Good, true);
            notes = b.Current.IntentNotes.Length;
            allPerfect = new Grade[notes];
            for (int i = 0; i < notes; i++) allPerfect[i] = Grade.Perfect;
            b.TakeEnemyAttack(allPerfect);
            report += Same("no rest", "3 4", notes + " " + b.Breath);

            //THE BARGAIN (THE DEVIL'S STRING) : its attack steals 1 breath
            b = new TurnBattle(band, new List<Enemy> { EnemyList.All[8] }, 1, new Random(433));
            b.Attack(b.Enemies[0], Grade.Good, true);
            b.TakeEnemyAttack(null);
            report += Same("bargain", "3", b.Breath.ToString());

            //UNFINISHED (REQUIEM) : below half life its next phrase (Lf Rf) comes mirrored and faster
            b = new TurnBattle(band, new List<Enemy> { EnemyList.All[6] }, 1, new Random(433));
            b.Enemies[0].Hp = 100;
            for (int i = 0; i < 10 && b.Round < 2; i++)
            {
                if (b.Step == TurnStep.PlayerTurn) b.Attack(b.Enemies[0], Grade.Miss, true);
                else b.TakeEnemyAttack(null);
            }
            report += Same("unfinished", "True Right Left", b.Enemies[0].IntentFast + " " + b.Enemies[0].IntentWays[0] + " " + b.Enemies[0].IntentWays[1]);

            //FALSE NOTES (STATIC, floor 2) : exactly one note shows a size it is not
            b = new TurnBattle(band, new List<Enemy> { EnemyList.All[3] }, 2, new Random(433));
            int lies = 0;
            for (int i = 0; i < b.Enemies[0].IntentNotes.Length; i++)
                if (b.Enemies[0].IntentShown[i] != b.Enemies[0].IntentSizes[i]) lies++;
            report += Same("false notes", "1", lies.ToString());

            //ANSWER BETTER (THE NAMELESS MASTER) : after KLARA's SOFT AIR (Dp Up) it plays those notes back
            b = new TurnBattle(band, new List<Enemy> { EnemyList.All[7] }, 1, new Random(433));
            b.UseSkill(SkillList.First(band[1]), null, new NoteResult[] { new NoteResult(Grade.Good, true, 1f), new NoteResult(Grade.Good, true, 1f) });
            for (int i = 0; i < 10 && b.Round < 2; i++)
            {
                if (b.Step == TurnStep.PlayerTurn) b.Attack(b.Enemies[0], Grade.Miss, true);
                else b.TakeEnemyAttack(null);
            }
            report += Same("answer better", "Down Up 0 0", b.Enemies[0].IntentWays[0] + " " + b.Enemies[0].IntentWays[1] + " " + b.Enemies[0].IntentSizes[0] + " " + b.Enemies[0].IntentSizes[1]);

            //ARCHIVE (step 7) : a lost run on floor 2 with 4 wins leaves 4 x 3 + 1 x 10 = 22 echoes, STRONGER BAND costs 30
            //and gives every musician 2 more life. The real archive is put back afterwards.
            int keptEchoes = Archive.Echoes;
            int keptLife = Archive.All[Archive.Life].Level;
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
            Archive.Echoes = keptEchoes;
            Archive.All[Archive.Life].Level = keptLife;

            File.WriteAllText(Path.Combine(outDir, "turncheck.txt"), report);
        }

        private static string OrderNames(TurnBattle b)
        {
            string s = "";
            for (int i = 0; i < b.Order.Count; i++) s += (i > 0 ? " " : "") + b.Order[i].Name;
            return s;
        }
    }
}
