using System;
using System.Collections.Generic;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //DebugShots.TurnEvents : DEVELOPER TOOL, the checks of the RISKY WAYS of the events (Beta1 1 Nov, docs/BETA1_DIRECTION.md
    //28, the user picked six events), part of --shots turncheck. A way paid in life takes the same share of every musician's
    //full life, only from a band that has more life than the price, and nobody goes under 1 (RunState.PayLife). The REWARD TAGS
    //say what a way gives before it is taken. Every line is PASS or FAIL like DebugShots.Turn.cs.
    public static partial class DebugShots
    {
        private static string TurnCheckEvents()
        {
            string report = "";
            int keptLife = Archive.All[Archive.Life].Level;               // STRONGER BAND would change every full life
            Archive.All[Archive.Life].Level = 0;
            MusicianList.ResetRehearsals();
            RunState run = new RunState();
            run.Roster.Add(MusicianList.All[0]);                           // PAGANINI, full life 45
            run.Roster.Add(MusicianList.All[1]);                           // QUANTZ, 39
            run.Roster.Add(MusicianList.All[2]);                           // PFUNDT, 54

            //Price : 35 percent is 16 + 14 + 19, 50 percent is 22 + 20 + 27 (a half rounds to the even number, like ChangeStamina)
            report += Same("life price", "49 69", run.LifeCostOf(BattleRules.EventLifeCost) + " " + run.LifeCostOf(BattleRules.EventLifeCostBig));

            //Pay : a band at full life pays its share each, 45 39 54 -> 29 25 35
            int paid = run.PayLife(BattleRules.EventLifeCost);
            report += Same("life pay", "49 29 25 35", paid + " " + EventLives(run));

            //The Rest : PAGANINI at 5 and QUANTZ at 1 cannot pay their share (they stop at 1), so PFUNDT pays the rest, 54 -> 9
            run.SetLife(run.Roster[0], 5);
            run.SetLife(run.Roster[1], 1);
            run.SetLife(run.Roster[2], 54);
            string rest = run.CanPayLife(BattleRules.EventLifeCost) + " ";
            paid = run.PayLife(BattleRules.EventLifeCost);
            report += Same("life pay rest", "True 49 1 1 9", rest + paid + " " + EventLives(run));

            //Not Enough : a band of 40 life cannot pay a price of 49, a band of 49 cannot either (it needs more), 50 can
            run.SetLife(run.Roster[0], 10);
            run.SetLife(run.Roster[1], 10);
            run.SetLife(run.Roster[2], 20);
            string poor = run.CanPayLife(BattleRules.EventLifeCost) + " ";
            run.SetLife(run.Roster[2], 29);
            poor += run.CanPayLife(BattleRules.EventLifeCost) + " ";
            run.SetLife(run.Roster[2], 30);
            poor += run.CanPayLife(BattleRules.EventLifeCost).ToString();
            report += Same("life not enough", "False False True", poor);

            //BAND LIFE (1 Nov, BETA1_DIRECTION 29 item 2) : the top bar's preview runs the same sums on a copy of the lives, so
            //it changes nothing and comes out the same as the real price then the real heal
            run.SetLife(run.Roster[0], 20);
            run.SetLife(run.Roster[1], 7);
            run.SetLife(run.Roster[2], 40);
            string before = EventLives(run);
            int[] preview = run.LivesNow();
            run.PayLives(preview, BattleRules.EventLifeCost);
            run.ChangeLives(preview, 12);
            string untouched = EventLives(run);
            run.PayLife(BattleRules.EventLifeCost);
            run.ChangeStamina(12);
            report += Same("life preview", before + " " + EventLives(run), untouched + " " + preview[0] + " " + preview[1] + " " + preview[2]);

            //Sectional : every ATTACK of the band gets a star, a weapon at its top tier is skipped (QUANTZ at 3)
            run.Roster[1].Rehearsed = BattleRules.RehearseMax;
            run.Roster[2].Rehearsed = 1;
            int raised = run.RehearseAll();
            report += Same("sectional", "2 1 3 2", raised + " " + run.Roster[0].Rehearsed + " " + run.Roster[1].Rehearsed + " " + run.Roster[2].Rehearsed);

            //Risky Ways : how many ways of each event cost more and give more (paid in life, an ELITE's fight, or the long chance at
            //a full life), in EventList.All's order. A LOST SCORE and A CRACKED BOW keep their old ways (the user, 1 Nov)
            string ways = "";
            for (int e = 0; e < EventList.All.Length; e++)
            {
                int risky = 0;
                EventChoice[] choices = EventList.All[e].Choices;
                for (int c = 0; c < choices.Length; c++)
                    if (choices[c].LifeCost > 0f || choices[c].Win == Reward.Fight || choices[c].Win == Reward.FullLife) risky++;
                ways += (e > 0 ? " " : "") + risky;
            }
            report += Same("risky ways", "1 1 1 0 1 1 1 1 1 0", ways);

            //Reward Tags : the six new ways, and two old ones (shards, and a way that costs a little life)
            string tags = EventTag(0, 2) + " / " + EventTag(1, 2) + " / " + EventTag(2, 2) + " / " + EventTag(5, 1) + " / "
                        + EventTag(6, 2) + " / " + EventTag(8, 2) + " / " + EventTag(0, 1) + " / " + EventTag(4, 1);
            report += Same("reward tags", "RARE GIFT / 2 STARS / ELITE FIGHT / FULL LIFE / ELITE FIGHT / +1 STAR EACH / +25 SHARDS / -6 LIFE", tags);

            MusicianList.ResetRehearsals();
            Archive.All[Archive.Life].Level = keptLife;
            return report;
        }

        //Event Lives : the band's life, musician by musician
        private static string EventLives(RunState run)
        {
            string s = "";
            for (int i = 0; i < run.Roster.Count; i++) s += (i > 0 ? " " : "") + run.LifeOf(run.Roster[i]);
            return s;
        }

        //Event Tag : the reward tag of way c of event e
        private static string EventTag(int e, int c)
        {
            return EventList.All[e].Choices[c].RewardTag;
        }
    }
}
