using System;
using System.Collections.Generic;
using System.Globalization;
using Tacetno433.Battle;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //DebugShots.TurnWords : DEVELOPER TOOL, the checks of the WORDS (Beta1 31 Oct, docs/BETA1_DIRECTION.md 19), part of
    //--shots turncheck. Every line the player reads uses the new rule words (Data/Words.cs) and none of the old ones, the skill
    //card finds the words a skill's line uses, and the moments the words work are written down for the screen to pop them
    //(BURN as it hurts, VOLUME as a MISS takes it, GUARD as THUNDER CALL lands). Every line is PASS or FAIL like DebugShots.Turn.cs.
    //Also NO LIMIT (31 Oct) : the numbers the screen shows past 999 (Turn Check Numbers).
    public static partial class DebugShots
    {
        //Old Words : rule words the player must not read any more (the code and the save keep them as names)
        private static readonly string[] oldWords = { "DISSONANCE", "CRESCENDO", "OFF-BEAT", "SIGNATURE", "LUTHIER", "ECHOES", "CADENZA" };

        private static string TurnCheckWords()
        {
            string report = "";

            //Old words : every line of the five legends' skills (and every star's), their traits, the gifts a run can find, the
            //enemies' traits, the three conductors' passives and FINALEs, the ARCHIVE and the route's places
            List<string> lines = new List<string>();
            for (int i = 0; i < SkillList.All.Length; i++) lines.Add(SkillList.All[i].Text);
            for (int m = 0; m < RehearsalList.All.Length; m++)
                for (int k = 0; k < RehearsalList.All[m].Length; k++)
                    for (int s = 0; s < RehearsalList.All[m][k].Length; s++)
                    {
                        Star star = RehearsalList.All[m][k][s];
                        lines.Add(star.From); lines.Add(star.To); lines.Add(star.Line); lines.Add(star.SkillText);
                    }
            for (int i = 0; i < MusicianList.All.Length; i++) lines.Add(MusicianList.All[i].TurnTraitText);
            for (int i = 0; i < MotifList.Pool.Length; i++) lines.Add(MotifList.Pool[i].TurnText.Length > 0 ? MotifList.Pool[i].TurnText : MotifList.Pool[i].Text);
            for (int i = 0; i < EnemyList.All.Length; i++) { lines.Add(EnemyList.All[i].TurnTraitText); lines.Add(EnemyList.All[i].TraitName); }
            for (int i = 0; i < ConductorList.Shown.Length; i++)
            {
                Conductor c = ConductorList.Shown[i];
                lines.Add(c.TurnPassiveText); lines.Add(c.TurnSignatureText);
            }
            for (int i = 0; i < Archive.All.Length; i++) lines.Add(Archive.All[i].Text);
            lines.AddRange(RouteNodeInfo.Titles); lines.AddRange(RouteNodeInfo.Tags); lines.AddRange(RouteNodeInfo.Captions);
            int old = 0;
            string first = "-";
            for (int i = 0; i < lines.Count; i++)
                for (int w = 0; w < oldWords.Length; w++)
                    if (lines[i] != null && Words.HasWord(lines[i], oldWords[w]))
                    {
                        if (old == 0) first = oldWords[w];
                        old++;
                    }
            report += Same("old words", "0 -", old + " " + first);

            //Keywords : CAPRICE's line finds BURN, TIMPANI ROLL's STUN then CHARGE, IL CANNONE's ARMOR, STUNNED is not read as STUN,
            //and the card's line says what the word does
            int[] found = new int[2];
            string keys = Words.KeywordsIn(SkillList.Plain(MusicianList.All[0], 0).Text, found, 2) + " " + KeyName(found[0]);
            keys += " " + Words.KeywordsIn(SkillList.Plain(MusicianList.All[2], 0).Text, found, 2) + " " + KeyName(found[0]) + " " + KeyName(found[1]);
            keys += " " + Words.KeywordsIn(SkillList.Plain(MusicianList.All[0], 1).Text, found, 2) + " " + KeyName(found[0]);
            keys += " " + Words.KeywordsIn("It is STUNNED.", found, 2) + " " + KeyName(found[0]);
            keys += " " + Words.KeywordsIn("Heals the whole band 3 per note.", found, 2);
            report += Same("keywords", "1 BURN 2 STUN CHARGE 1 ARMOR 1 STUNNED 0", keys);

            //Burn Pops : an enemy with 3 BURN loses 3 at the end of its turn, the moment written down with the 3, then it has 2
            Musician paganini = MusicianList.All[0];
            Musician quantz = MusicianList.All[1];
            Musician pfundt = MusicianList.All[2];
            MusicianList.ResetRehearsals();
            List<Musician> band = new List<Musician> { paganini, quantz, pfundt };
            TurnBattle b = new TurnBattle(band, new List<Enemy> { EnemyList.All[1] }, 1, new Random(433));   // DEAD AIR, no BURN of its own
            Tough(b);
            Fighter enemy = b.Enemies[0];
            enemy.Dissonance = 3;
            b.Events.Clear();
            for (int i = 0; i < 40 && b.Round < 2 && b.Step != TurnStep.Won && b.Step != TurnStep.Lost; i++) Step(b, Grade.Good);
            report += Same("burn pops", "1 3 2", CountEvents(b, TurnEventKind.DissonanceTick) + " " + EventAmount(b, TurnEventKind.DissonanceTick) + " " + enemy.Dissonance);

            //Volume Pops : a musician with 3 VOLUME who MISSes a skill note loses it all, written down with what is left (0)
            b = new TurnBattle(band, new List<Enemy> { EnemyList.All[1] }, 1, new Random(433));
            Tough(b);
            b.Breath = 10;
            Fighter pag = b.Party[0];
            pag.Crescendo = 3;
            b.PlanSkill(pag, SkillList.First(paganini), b.Enemies[0]);
            b.Events.Clear();
            for (int i = 0; i < 20 && !(b.Step == TurnStep.PlayerTurn && b.Current == pag); i++) Step(b, Grade.Good);
            Step(b, Grade.Miss);
            report += Same("volume pops", "1 0 0", CountEvents(b, TurnEventKind.CrescendoLost) + " " + EventAmount(b, TurnEventKind.CrescendoLost) + " " + pag.Crescendo);

            //Guard Pops : THUNDER CALL that lands gives PFUNDT GUARD and writes it down once
            b = new TurnBattle(band, new List<Enemy> { EnemyList.All[1] }, 1, new Random(433));
            Tough(b);
            b.Breath = 10;
            Fighter pf = b.Party[2];
            b.PlanSkill(pf, SkillList.Second(pfundt), pf);
            b.Events.Clear();
            for (int i = 0; i < 20 && !(b.Step == TurnStep.PlayerTurn && b.Current == pf); i++) Step(b, Grade.Good);
            Step(b, Grade.Good);
            report += Same("guard pops", "1 True", CountEvents(b, TurnEventKind.GuardUp) + " " + pf.Guard);

            MusicianList.ResetRehearsals();
            return report;
        }

        //Turn Check Numbers : NO LIMIT (Beta1 31 Oct, docs/BETA1_DIRECTION.md 20). The screen shows every number as it really is,
        //past the table made at startup (9999) and below zero. A number past the table is made once and kept, so asking again
        //gives the very same string (no new rubbish every frame). The biggest life an enemy really has on floor 3 (a boss after
        //the floor's scale, 1560 today) shows as it is : it showed 999 until then.
        private static string TurnCheckNumbers()
        {
            string report = "";
            report += Same("numbers", "0 7 999 1000 1560 9999 10000 12345 2147483647",
                NumberText.Get(0) + " " + NumberText.Get(7) + " " + NumberText.Get(999) + " " + NumberText.Get(1000) + " " + NumberText.Get(1560) + " "
                + NumberText.Get(9999) + " " + NumberText.Get(10000) + " " + NumberText.Get(12345) + " " + NumberText.Get(int.MaxValue));
            report += Same("signed numbers", "+0 +7 -7 +1000 -1000 +12345 -12345 -2147483648",
                NumberText.Signed(0) + " " + NumberText.Signed(7) + " " + NumberText.Signed(-7) + " " + NumberText.Signed(1000) + " "
                + NumberText.Signed(-1000) + " " + NumberText.Signed(12345) + " " + NumberText.Signed(-12345) + " " + NumberText.Signed(int.MinValue));
            report += Same("kept once", "True True True",
                ReferenceEquals(NumberText.Get(12345), NumberText.Get(12345)) + " " + ReferenceEquals(NumberText.Signed(-54321), NumberText.Signed(-54321))
                + " " + ReferenceEquals(NumberText.Get(1560), NumberText.Get(1560)));

            int most = 0;
            for (int i = 0; i < EnemyList.All.Length; i++)
            {
                TurnBattle b = new TurnBattle(Solo(0), new List<Enemy> { EnemyList.All[i] }, 3, new Random(433));
                most = Math.Max(most, b.Enemies[0].MaxHp);
            }
            report += Same("floor 3 life", most.ToString(CultureInfo.InvariantCulture), NumberText.Get(most));
            return report;
        }

        //Key Name : the rule word at a place of Words.KeywordLines ("BURN" from "BURN  =  loses ...")
        private static string KeyName(int place)
        {
            string line = Words.KeywordLines[place];
            return line.Substring(0, line.IndexOf("  =  ", StringComparison.Ordinal));
        }
    }
}
