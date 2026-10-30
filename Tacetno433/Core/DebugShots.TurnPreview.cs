using System;
using System.Collections.Generic;
using Tacetno433.Battle;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //DebugShots.TurnPreview : DEVELOPER TOOL, the checks of PREVIEW (Beta1 30 Oct, block 1 item 4), part of --shots turncheck.
    //What the battle screen shows before a move is played (TurnBattle.Preview) must be what the move does when every note
    //lands GOOD and every hold is held to the end. Each line takes the preview, then plays the move and compares : the life each
    //enemy lost (or each musician got back), the stacks it gave, the BLOCK left. The expected side is what really happened, so
    //a change to the rules that the preview does not follow shows here as a FAIL.
    public static partial class DebugShots
    {
        private static string TurnCheckPreview()
        {
            string report = "";
            Enemy deadAir = EnemyList.All[1];
            MovePreview p = new MovePreview();
            Musician paganini = MusicianList.All[0];
            Musician quantz = MusicianList.All[1];
            Musician punto = MusicianList.All[7];

            //ATTACK : PAGANINI at DEAD AIR, the hit and the BREATH it gives (from none)
            TurnBattle b = new TurnBattle(Solo(0), new List<Enemy> { deadAir }, 1, new Random(433));
            Sturdy(b);
            b.Breath = 0;
            Fighter e = b.Enemies[0];
            b.Preview(b.Party[0], null, e, p);
            string shown = p.Loss[0] + " " + p.Breath;
            int hp = e.Hp;
            b.PlanAttack(b.Party[0], e);
            b.PlayBar();
            b.Attack();
            report += Same("aim attack", (hp - e.Hp) + " " + b.Breath, shown);

            //CAPRICE : four cuts and their DISSONANCE
            Skill caprice = SkillList.First(paganini);
            b = PreviewFight(Solo(0), new List<Enemy> { deadAir });
            e = b.Enemies[0];
            b.Preview(b.Party[0], caprice, e, p);
            shown = p.Loss[0] + " " + p.Dissonance + " " + p.PerNote * caprice.Notes.Length;
            hp = e.Hp;
            PlaySkill(b, b.Party[0], caprice, e);
            report += Same("aim caprice", (hp - e.Hp) + " " + e.Dissonance + " " + (hp - e.Hp), shown);

            //GUARD : 10 BLOCK on DEAD AIR. CAPRICE's cuts take it off first, IL CANNONE's held shot goes through it (PIERCE).
            //The BLOCK left is read from what it took (Blocked), the enemy's own turn starts right after and clears it.
            b = PreviewFight(Solo(0), new List<Enemy> { deadAir });
            e = b.Enemies[0];
            e.Block = 10;
            b.Preview(b.Party[0], caprice, e, p);
            shown = p.Loss[0] + " " + p.BlockLeft[0];
            hp = e.Hp;
            PlaySkill(b, b.Party[0], caprice, e);
            report += Same("aim block", (hp - e.Hp) + " " + (10 - EventSum(b, TurnEventKind.Blocked)), shown);

            Skill cannone = SkillList.Second(paganini);
            b = PreviewFight(Solo(0), new List<Enemy> { deadAir });
            e = b.Enemies[0];
            e.Block = 10;
            b.Preview(b.Party[0], cannone, e, p);
            shown = p.Loss[0] + " " + p.BlockLeft[0];
            hp = e.Hp;
            PlaySkill(b, b.Party[0], cannone, e);
            report += Same("aim pierce", (hp - e.Hp) + " " + (10 - EventSum(b, TurnEventKind.Blocked)), shown);

            //CHAKRAM : three throws at two DEAD AIRs from the first, the first is hit again at half
            Skill chakram = SkillList.Second(punto);
            b = PreviewFight(Solo(7), new List<Enemy> { deadAir, deadAir });
            Fighter e0 = b.Enemies[0];
            Fighter e1 = b.Enemies[1];
            b.Preview(b.Party[0], chakram, e0, p);
            shown = p.Targets.Count + " " + p.Loss[0] + " " + p.Loss[1];
            int hp0 = e0.Hp;
            int hp1 = e1.Hp;
            PlaySkill(b, b.Party[0], chakram, e0);
            report += Same("aim chakram", "2 " + (hp0 - e0.Hp) + " " + (hp1 - e1.Hp), shown);

            //SOFT AIR : QUANTZ heals the band, the one short of only 4 life gets 4 back
            Skill softAir = SkillList.First(quantz);
            b = PreviewFight(new List<Musician> { quantz, paganini }, new List<Enemy> { deadAir });
            Fighter q = b.Party[0];
            Fighter g = b.Party[1];
            q.Hp = q.MaxHp - 4;
            g.Hp = g.MaxHp - 20;
            b.Preview(q, softAir, null, p);
            shown = p.Gain[0] + " " + p.Gain[1] + " " + p.Heal;
            int hpQ = q.Hp;
            int hpG = g.Hp;
            PlaySkill(b, q, softAir, null);
            report += Same("aim heal", (q.Hp - hpQ) + " " + (g.Hp - hpG) + " " + softAir.Heal * softAir.Notes.Length, shown);

            //FANFARE : PUNTO's CRESCENDO on the whole band
            Skill fanfare = SkillList.First(punto);
            b = PreviewFight(new List<Musician> { punto, paganini }, new List<Enemy> { deadAir });
            b.Preview(b.Party[0], fanfare, null, p);
            shown = p.Targets.Count + " " + p.Crescendo;
            PlaySkill(b, b.Party[0], fanfare, null);
            report += Same("aim fanfare", "2 " + b.Party[1].Crescendo, shown);
            return report;
        }

        //Event Sum : all the Amounts of one kind of event so far
        private static int EventSum(TurnBattle b, TurnEventKind kind)
        {
            int sum = 0;
            for (int i = 0; i < b.Events.Count; i++)
                if (b.Events[i].Kind == kind) sum += b.Events[i].Amount;
            return sum;
        }

        //Preview Fight : a fight on floor 1 with full BREATH, the band sturdy, the enemies' traits on
        private static TurnBattle PreviewFight(List<Musician> band, List<Enemy> enemies)
        {
            TurnBattle b = new TurnBattle(band, enemies, 1, new Random(433));
            b.Breath = BattleRules.TurnBreathMax;
            return b;
        }

        //Play Skill : who plays the skill at target this bar, every note GOOD and held to the end. The rest of the band ATTACKs
        //before or after as the bar's order says, the enemies are not reached before it (the band plays first on floor 1).
        private static void PlaySkill(TurnBattle b, Fighter who, Skill s, Fighter target)
        {
            b.PlanSkill(who, s, target);
            b.PlayBar();
            for (int guard = 0; guard < 12 && b.Step == TurnStep.PlayerTurn; guard++)
            {
                if (b.Current == who)
                {
                    NoteResult[] notes = new NoteResult[s.Notes.Length];
                    for (int n = 0; n < notes.Length; n++) notes[n] = new NoteResult(Grade.Good, 1f);
                    b.UseSkill(notes);
                    return;
                }
                b.Attack();
            }
        }
    }
}
