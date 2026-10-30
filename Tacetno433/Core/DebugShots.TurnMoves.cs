using System;
using System.Collections.Generic;
using Tacetno433.Battle;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //DebugShots.TurnMoves : DEVELOPER TOOL, the checks of ENEMY MOVES (Beta1 30 Oct, docs/BETA1_DIRECTION.md 12.3), part of
    //--shots turncheck. TACET's CHARGE and GUARD, and the band's answers : BREAK (TIMPANI ROLL, a full OFF-BEAT), PIERCE
    //(IL CANNONE) and DISSONANCE through a BLOCK. Like DebugShots.Turn.cs every line is PASS or FAIL with what was expected
    //and what came out. The musicians' traits are on here (TURN OF THE HAND has nothing to cut in a CHARGE).
    public static partial class DebugShots
    {
        private static string TurnCheckMoves()
        {
            string report = "";
            NoteResult good = new NoteResult(Grade.Good, 1f);
            Enemy deadAir = EnemyList.All[1];
            Enemy damper = EnemyList.All[11];
            Skill roll = SkillList.First(MusicianList.All[2]);
            Skill cannone = SkillList.Second(MusicianList.All[0]);
            Skill caprice = SkillList.First(MusicianList.All[0]);

            //CHARGE (DEAD AIR, floor 1, each note 8) : bar 1 its plain attack (2 notes, 16 on PFUNDT), then it winds up and plays
            //nothing, the blow to come is 8 x 3 = 24. Bar 2 it winds up (a CHARGE written down), its next attack is that one note.
            //Bar 3 the note is parried PERFECT, the COUNTER is twice as hard (6 x 2 x 2 = 24), then it attacks plainly again.
            TurnBattle b = new TurnBattle(Solo(2), new List<Enemy> { deadAir }, 1, new Random(433));
            Sturdy(b);
            b.PlayBar();
            b.Attack();
            b.TakeEnemyAttack(null);
            Fighter dead = b.Enemies[0];
            string charge = dead.IntentCharge + " " + dead.IntentNotes.Length + " " + dead.ChargeHit;
            b.PlayBar();
            b.Attack();
            b.TakeEnemyAttack(null);
            charge += " " + CountEvents(b, TurnEventKind.Charge) + " " + dead.IntentHeavy + " " + dead.IntentNotes.Length + " " + dead.IntentNotes[0];
            b.PlayBar();
            b.Attack();
            b.Events.Clear();
            b.TakeEnemyAttack(new Grade[] { Grade.Perfect });
            charge += " " + EventAmount(b, TurnEventKind.Counter) + " " + dead.IntentHeavy + " " + dead.IntentNotes.Length + " " + b.Party[0].Hp;
            report += Same("charge", "True 0 24 1 True 1 24 24 False 2 983", charge);

            //BREAK (TIMPANI ROLL) : played at DEAD AIR while it winds up, the CHARGE is broken (BROKEN written down). Its wind up and
            //the turn of the blow play nothing, then it attacks plainly : PFUNDT only ever loses bar 1's 16.
            b = new TurnBattle(Solo(2), new List<Enemy> { deadAir }, 1, new Random(433));
            Sturdy(b);
            b.PlayBar();
            b.Attack();
            b.TakeEnemyAttack(null);
            b.PlanSkill(b.Party[0], roll, b.Enemies[0]);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { good });
            dead = b.Enemies[0];
            string broken = CountEvents(b, TurnEventKind.Broken) + " " + dead.ChargeBroken;
            b.TakeEnemyAttack(null);
            broken += " " + dead.ChargeBroken + " " + dead.IntentNotes.Length + " " + dead.IntentHeavy;
            b.PlayBar();
            b.Attack();
            b.TakeEnemyAttack(null);
            broken += " " + dead.ChargeBroken + " " + dead.IntentNotes.Length + " " + b.Party[0].Hp;
            report += Same("break roll", "1 True True 0 False False 2 983", broken);

            //BREAK (OFF-BEAT) : thrown off the beat while it winds up, it loses its turn (as ever) and that lost turn is the broken
            //CHARGE's wind up. The turn of the blow plays nothing, then it attacks plainly : again only bar 1's 16.
            b = new TurnBattle(Solo(2), new List<Enemy> { deadAir }, 1, new Random(433));
            Sturdy(b);
            b.PlayBar();
            b.Attack();
            b.TakeEnemyAttack(null);
            dead = b.Enemies[0];
            dead.OffBeat = BattleRules.TurnOffBeatMax - BattleRules.TurnAttackOffBeat;
            b.PlayBar();
            b.Attack();                                                                // fills the gauge : thrown, and the CHARGE breaks
            string thrown = CountEvents(b, TurnEventKind.Broken) + " " + CountEvents(b, TurnEventKind.Skip) + " " + dead.ChargeBroken + " " + dead.Charge;
            b.PlayBar();
            b.Attack();
            b.TakeEnemyAttack(null);
            thrown += " " + dead.ChargeBroken + " " + dead.IntentNotes.Length + " " + b.Party[0].Hp;
            report += Same("break off-beat", "1 1 True 2 False 2 983", thrown);

            //GUARD (THE DAMPER, floor 1, life 42 x 1.6 = 67) : bar 1 it attacks, then it means to GUARD with 14 x 1.6 = 22. Bar 2 its
            //turn raises the BLOCK (written down), bar 3 PAGANINI's ATTACK (5 x 2 x 0.55 = 5.5 -> 6) only takes 6 off the BLOCK, its
            //life stays, and its own turn ends the BLOCK.
            b = new TurnBattle(Solo(0), new List<Enemy> { damper }, 1, new Random(433));
            Sturdy(b);
            b.PlayBar();
            b.Attack();
            b.TakeEnemyAttack(null);
            Fighter felt = b.Enemies[0];
            string guard = felt.IntentGuard + " " + felt.IntentNotes.Length;
            b.PlayBar();
            b.Attack();
            int life = felt.Hp;
            b.TakeEnemyAttack(null);
            guard += " " + EventAmount(b, TurnEventKind.Guard) + " " + felt.Block;
            b.PlayBar();
            b.Events.Clear();
            b.Attack();                                                                // its turn starts right after : the BLOCK ends
            guard += " " + EventAmount(b, TurnEventKind.Blocked) + " " + (life - felt.Hp) + " " + felt.Block;
            report += Same("guard", "22 0 22 22 6 0 0", guard);

            //PIERCE (IL CANNONE) : held GOOD to the end at THE DAMPER behind a BLOCK of 16, 5 x 2 x 1.8 x 1.4 = 25 goes through to its
            //life (67 -> 42), the BLOCK keeps its 16 and a PIERCE is written down. CAPRICE's 4 cuts (4 each) all land in the BLOCK,
            //its life stays. (Read before EndSkill : the enemy's own turn comes right after and ends the BLOCK.)
            b = new TurnBattle(Solo(0), new List<Enemy> { damper }, 1, new Random(433));
            Sturdy(b);
            b.Breath = 10;
            felt = b.Enemies[0];
            felt.Block = 16;
            b.PlanSkill(b.Party[0], cannone, felt);
            b.PlayBar();
            b.BeginSkill();
            b.SkillNote(0, good);
            string pierce = felt.Hp + " " + felt.Block + " " + CountEvents(b, TurnEventKind.Pierce);
            b.EndSkill();
            b = new TurnBattle(Solo(0), new List<Enemy> { damper }, 1, new Random(433));
            Sturdy(b);
            b.Breath = 10;
            felt = b.Enemies[0];
            felt.Block = 16;
            b.PlanSkill(b.Party[0], caprice, felt);
            b.PlayBar();
            b.BeginSkill();
            for (int n = 0; n < 4; n++) b.SkillNote(n, good);
            pierce += " " + felt.Hp + " " + felt.Block + " " + felt.Dissonance;
            b.EndSkill();
            report += Same("pierce", "42 16 1 67 0 4", pierce);

            //DISSONANCE through a GUARD : CAPRICE in the bar THE DAMPER guards (4 DISSONANCE). Its turn raises the BLOCK of 22 and the
            //DISSONANCE still hurts at the end of it (4 off its life), it never strikes the BLOCK.
            b = new TurnBattle(Solo(0), new List<Enemy> { damper }, 1, new Random(433));
            Sturdy(b);
            b.Breath = 10;
            felt = b.Enemies[0];
            b.PlayBar();
            b.Attack();
            b.TakeEnemyAttack(null);                                                   // bar 1 : it attacks, then means to GUARD
            b.PlanSkill(b.Party[0], caprice, felt);
            b.PlayBar();
            b.UseSkill(new NoteResult[] { good, good, good, good });
            life = felt.Hp;
            b.TakeEnemyAttack(null);                                                   // its GUARD turn
            report += Same("dissonance guard", "4 22 3", (life - felt.Hp) + " " + felt.Block + " " + felt.Dissonance);

            return report;
        }

        //Event Amount : the amount of the last event of this kind the battle has written down, 0 for none
        private static int EventAmount(TurnBattle b, TurnEventKind kind)
        {
            for (int i = b.Events.Count - 1; i >= 0; i--)
                if (b.Events[i].Kind == kind) return b.Events[i].Amount;
            return 0;
        }
    }
}
