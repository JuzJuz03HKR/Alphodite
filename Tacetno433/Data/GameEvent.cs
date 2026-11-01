using System;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Core;

namespace Tacetno433.Data
{
    //Reward : what a choice in an event can do to the run
    public enum Reward
    {
        Nothing,
        Shards,       // Amount can be negative
        Stamina,      // Amount can be negative
        Motif,        // a random motif, Amount is the lowest rarity
        Rehearse,     // a weapon goes up one tier : the musician who took the check, or without a check the band's least trained
        Fight,        // an elite fight starts
        //THE BAND OF THREE (29 Oct) : RECRUIT and RECRUIT PART (someone joins the band) are gone, the band is picked at the start
        //RISKY WAYS (Beta1 1 Nov, docs/BETA1_DIRECTION.md 28) : three more, made of things the band already knows
        Stars,        // Amount stars, the player gives each one to a move on the rehearsal page (RehearsalScreen.From.Event)
        Sectional,    // every ATTACK of the band gets a star, like the shop's SECTIONAL
        FullLife      // every musician's life back to full
    }

    //EventChoice : one button on an event page.
    //
    //Without a check the Win result simply happens.
    //With a check the player picks a musician first, the game shows the chance, rolls,
    //and either Win or Lose happens. The chance is
    //    CheckBase + that musician's power x CheckPerPower
    //    + CheckFamilyBonus if they belong to CheckFamily
    //
    //RISKY WAYS (Beta1 1 Nov, docs/BETA1_DIRECTION.md 28, the user picked them) : most events have one way that costs more and
    //gives more, paid in life (LifeCost) or in an ELITE's fight (Reward.Fight). ShardCost and LifeCost are for the ways
    //without a check.
    public class EventChoice
    {
        public string Text = "";             // the button
        public int ShardCost = 0;            // paid when the choice is taken, 0 for free
        public float LifeCost = 0f;          // RISKY WAYS : paid when the choice is taken, a share of every musician's full life

        public bool Check = false;
        public int CheckFamily = -1;         // -1 any family helps equally, else (int)Family
        public int CheckBase = 30;

        public Reward Win = Reward.Nothing;
        public int WinAmount = 0;
        public string WinText = "";

        public Reward Lose = Reward.Nothing;
        public int LoseAmount = 0;
        public string LoseText = "";

        //Prepared Text
        public string HintLabel = "";        // "CHECK  /  PERCUSSION"  or  "COSTS 30 SHARDS" (a price in life : EventScreen.Load)
        public string RewardTag = "";        // REWARD TAGS : what the way gives, "RARE GIFT"  or  "+25 SHARDS" (a check : its success)
        public string WinWrapped = "";
        public string LoseWrapped = "";
    }

    //GameEvent : a place with a short story and two or three choices
    public class GameEvent
    {
        public string Title = "";
        public string Place = "";            // a short line over the illustration
        public string Text = "";
        public EventChoice[] Choices = new EventChoice[0];

        public string TextWrapped = "";
    }

    //EventList : THE PLACE TO EDIT EVENTS.
    public static class EventList
    {
        public static GameEvent[] All = new GameEvent[]
        {
            new GameEvent
            {
                Title = "THE BROKEN METRONOME",
                Place = "AN EMPTY REHEARSAL HALL",
                Text = "On a music stand in an empty hall, a metronome ticks out of time. Every few beats it skips, "
                     + "and the silence in the skip is a little too deep.",
                Choices = new EventChoice[]
                {
                    new EventChoice { Text = "Try to repair it", Check = true, CheckFamily = (int)Family.Percussion, CheckBase = 25,
                                      Win = Reward.Motif, WinAmount = 2, WinText = "It clicks back into time, and something small and bright falls out of its case.",
                                      Lose = Reward.Stamina, LoseAmount = -12, LoseText = BattleRules.TurnBattleOn ? "It snaps at the fingers. The band gets hurt trying to hold the tempo." : "It snaps at the fingers. The band loses its breath trying to hold the tempo." },
                    new EventChoice { Text = "Smash it and sell the brass",
                                      Win = Reward.Shards, WinAmount = 25, WinText = "The brass is worth something. The silence it leaves behind is not." },
                    //RISKY WAYS (1 Nov) : forced by hand instead of mended, a sure price for the better gift (was : Leave it ticking)
                    new EventChoice { Text = "Wind it by hand until it holds", LifeCost = BattleRules.EventLifeCost,
                                      Win = Reward.Motif, WinAmount = 3, WinText = "It bites every hand that holds it. Then it ticks true, and something bright drops out of its case." },
                }
            },

            new GameEvent
            {
                Title = "A STREET MUSICIAN",
                Place = "A CORNER NOBODY WALKS PAST",
                Text = "Someone plays alone for an audience that never comes. They look up when your band arrives, "
                     + "and for the first time tonight they smile.",
                Choices = new EventChoice[]
                {
                    new EventChoice { Text = "Pay for a lesson", ShardCost = 30,
                                      Win = Reward.Rehearse, WinText = "One old trick, played slowly. Your band will not forget it." },
                    new EventChoice { Text = "Play along with them", Check = true, CheckFamily = (int)Family.String, CheckBase = 30,
                                      Win = Reward.Rehearse, WinText = "The duet teaches your player something new.",
                                      Lose = Reward.Stamina, LoseAmount = -10, LoseText = "You fall out of step. It is tiring to be out of step." },
                    //RISKY WAYS (1 Nov) : stars all three ways, paid in shards, in luck, or in a whole night (was : Drop a coin and move on)
                    new EventChoice { Text = "Play with them until dawn", LifeCost = BattleRules.EventLifeCost,
                                      Win = Reward.Stars, WinAmount = BattleRules.EventStars, WinText = "By morning nobody can stand, and nobody has ever played better." },
                }
            },

            new GameEvent
            {
                Title = "THE SILENT AUDIENCE",
                Place = "A HALL FULL OF LISTENERS",
                Text = "Every seat is taken. Nobody moves, nobody coughs, nobody has a face. "
                     + "They are waiting for someone to play.",
                Choices = new EventChoice[]
                {
                    new EventChoice { Text = "Perform for them", Check = true, CheckFamily = (int)Family.Wind, CheckBase = 30,
                                      Win = Reward.Shards, WinAmount = 45, WinText = "The hall does not clap. It leaves shards on the seats instead.",
                                      Lose = Reward.Stamina, LoseAmount = -15, LoseText = "The silence swallows every note. The band walks off drained." },
                    new EventChoice { Text = "Bow, and leave quietly",
                                      Win = Reward.Stamina, WinAmount = 10, WinText = "A small rest in the dark does the band good." },
                    //RISKY WAYS (1 Nov) : a faceless hall that waits in silence is TACET's, and a sound loud enough wakes it
                    new EventChoice { Text = "Play loud enough to wake them",
                                      Win = Reward.Fight, WinText = "Every faceless head turns at once. This was never an audience." },
                }
            },

            new GameEvent
            {
                Title = "A LOST SCORE",
                Place = "PAGES ON THE FLOOR",
                Text = "Sheets of music are scattered down a corridor, still wet with ink. "
                     + "Some bars have been scratched out so hard the paper tore.",
                Choices = new EventChoice[]
                {
                    new EventChoice { Text = "Gather every page",
                                      Win = Reward.Motif, WinAmount = 1, WinText = "Among the pages is a mark you can keep." },
                    new EventChoice { Text = "Read the torn bars aloud", Check = true, CheckBase = 35,
                                      Win = Reward.Rehearse, WinText = "The torn bars were the best part. Your player remembers them.",
                                      Lose = Reward.Stamina, LoseAmount = -8, LoseText = "The notes make no sense, and trying costs something." },
                }
            },

            new GameEvent
            {
                Title = "TACET'S ECHO",
                Place = "A SHADOW THAT ANSWERS BACK",
                Text = "The dark ahead repeats your footsteps half a beat late. It is waiting for you to play first. "
                     + "It will not wait forever.",
                Choices = new EventChoice[]
                {
                    new EventChoice { Text = "Face it",
                                      Win = Reward.Fight, WinText = "The echo takes a shape. It is stronger than it sounded." },
                    new EventChoice { Text = "Slip past it",
                                      Win = Reward.Stamina, WinAmount = -6, WinText = "You hold your breath all the way past it." },
                }
            },

            new GameEvent
            {
                Title = "THE TEA HOUSE",
                Place = "A WARM LIGHT IN A WINDOW",
                Text = "An old tea house, still open. The owner says musicians drink for free, "
                     + "as long as somebody plays something first.",
                Choices = new EventChoice[]
                {
                    new EventChoice { Text = "Rest and drink",
                                      Win = Reward.Stamina, WinAmount = 25, WinText = "Warm tea, a quiet room. The band feels like itself again." },
                    //RISKY WAYS (1 Nov) : the sure small rest, or a long chance at a whole night's sleep (was : Play for the room, 40 shards)
                    new EventChoice { Text = "Play the owner's favourite", Check = true, CheckBase = 10,
                                      Win = Reward.FullLife, WinText = "The owner hums along, then locks the door and lets you sleep. You wake up whole.",
                                      Lose = Reward.Shards, LoseAmount = -10, LoseText = "Nobody listens. You pay for the tea after all." },
                }
            },

            //THE EMPTY CHAIR (round 14) : a band can be missing a whole section of the orchestra
            //(round 15, it was a letter). Until the band of three (29 Oct) this place offered a player for it, now the one
            //who plays it leaves a rare motif behind.
            new GameEvent
            {
                Title = "THE EMPTY CHAIR",
                Place = "AN ORCHESTRA PIT, ONE STAND LIT",
                Text = "In the dark pit one chair still has its stand, and on the stand one part, a single line of music "
                     + "for an instrument your band does not have. The ink is wet. Someone was meant to play it tonight.",
                Choices = new EventChoice[]
                {
                    new EventChoice { Text = "Find the one who plays it", ShardCost = 35,
                                      Win = Reward.Motif, WinAmount = 2, WinText = "A player steps out of the dark, plays the part once, and leaves something behind." },
                    new EventChoice { Text = "Play it yourselves", Check = true, CheckBase = 35,
                                      Win = Reward.Rehearse, WinText = "The line is strange and beautiful. Your player keeps it.",
                                      Lose = Reward.Stamina, LoseAmount = -8, LoseText = "The part is written for someone else. It fights every note." },
                    //RISKY WAYS (1 Nov) : the gift is bought, or fought for. Whoever was meant to play it is gone (was : Leave the chair empty)
                    new EventChoice { Text = "Wait in the dark for its player",
                                      Win = Reward.Fight, WinText = "Something sits down in the chair. It is not who you were waiting for." },
                }
            },

            //Alpha 7 step 13 (docs/ALPHA7_CONTENT_PROPOSALS.md 4.1) : three more, made of the rewards that already exist
            new GameEvent
            {
                Title = "A RIVAL BAND",
                Place = "A STAGE ACROSS THE SQUARE",
                Text = "Another band is playing your road, louder than you. Their leader points a bow at you "
                     + "and waits to see what you do.",
                Choices = new EventChoice[]
                {
                    new EventChoice { Text = "Take the challenge",
                                      Win = Reward.Fight, WinText = "They stop playing. Something under their music was never a band at all." },
                    new EventChoice { Text = "Pay them to play for you", ShardCost = 20,
                                      Win = Reward.Stamina, WinAmount = 12, WinText = "For once someone else does the work. The band sits and listens." },
                    new EventChoice { Text = "Walk the other way",
                                      Win = Reward.Nothing, WinText = "Their laughter follows you a little way down the road." },
                }
            },

            new GameEvent
            {
                Title = "A TUNING FORK",
                Place = "STILL HUMMING ON THE FLOOR",
                Text = "A tuning fork lies on the stones, still ringing, though nobody is near. "
                     + "The note it holds is almost the right one.",
                Choices = new EventChoice[]
                {
                    new EventChoice { Text = "Strike it and listen", Check = true, CheckBase = 40,
                                      Win = Reward.Motif, WinAmount = 2, WinText = "The note settles in your ear and stays there.",
                                      Lose = Reward.Stamina, LoseAmount = -8, LoseText = "It rings far too loud. Everyone flinches." },
                    new EventChoice { Text = "Pocket it to sell",
                                      Win = Reward.Shards, WinAmount = 20, WinText = "Good steel. Somebody will pay for it." },
                    //RISKY WAYS (1 Nov) : the note that is almost right goes through every instrument and every player
                    new EventChoice { Text = "Tune the whole band to it", LifeCost = BattleRules.EventLifeCostBig,
                                      Win = Reward.Sectional, WinText = "The note runs through every string and every player. It hurts, and then the whole band rings true." },
                }
            },

            new GameEvent
            {
                Title = "A CRACKED BOW",
                Place = "A CASE LEFT IN THE RAIN",
                Text = "An open case, and in it a bow with a long crack down the stick. "
                     + "It might hold for one more piece.",
                Choices = new EventChoice[]
                {
                    new EventChoice { Text = "Play it as it is", Check = true, CheckFamily = (int)Family.String, CheckBase = 35,
                                      Win = Reward.Rehearse, WinText = "It holds. Playing it that lightly teaches something new.",
                                      Lose = Reward.Stamina, LoseAmount = -10, LoseText = "It snaps mid phrase and the whip of it stings." },
                    new EventChoice { Text = "Sell the horsehair",
                                      Win = Reward.Shards, WinAmount = 15, WinText = "Old horsehair, but good. A few shards for it." },
                    new EventChoice { Text = "Close the case",
                                      Win = Reward.Nothing, WinText = "You leave it for whoever owned it." },
                }
            },
        };

        private static string[] familyNames = { "STRING", "WIND", "PERCUSSION" };

        //Text Prepare : called once from LoadContent
        public static void PrepareText(SpriteFont storyFont, float storyWidth, float outcomeWidth)
        {
            for (int e = 0; e < All.Length; e++)
            {
                GameEvent ev = All[e];
                ev.TextWrapped = Gfx.WrapText(storyFont, ev.Text, storyWidth, TextSize.Story);

                for (int c = 0; c < ev.Choices.Length; c++)
                {
                    EventChoice ch = ev.Choices[c];
                    ch.WinWrapped = Gfx.WrapText(storyFont, ch.WinText, outcomeWidth, TextSize.Story);
                    ch.LoseWrapped = Gfx.WrapText(storyFont, ch.LoseText, outcomeWidth, TextSize.Story);

                    if (ch.Check)
                        ch.HintLabel = ch.CheckFamily < 0 ? "CHECK  /  ANY PLAYER" : "CHECK  /  " + familyNames[ch.CheckFamily] + " HELPS";
                    else if (ch.ShardCost > 0)
                        ch.HintLabel = "COSTS " + ch.ShardCost + " SHARDS";
                    else
                        ch.HintLabel = "";                // RISKY WAYS : a price in life is the band's own number, EventScreen.Load makes it
                    ch.RewardTag = TagOf(ch.Win, ch.WinAmount);
                }
            }
        }

        //Reward Tag : REWARD TAGS (Beta1 1 Nov, docs/BETA1_DIRECTION.md 28, the user picked it), what a way gives in the words of
        //the map (A2), so a player sees it before taking it. A check shows what its success gives, a way that gives nothing no tag.
        private static string TagOf(Reward reward, int amount)
        {
            string signed = amount > 0 ? "+" + amount : (amount < 0 ? "-" + (-amount) : "0");
            if (reward == Reward.Shards) return signed + " " + Words.Shards;
            if (reward == Reward.Stamina) return signed + " " + BattleRules.LifeWord;
            if (reward == Reward.Motif) return amount >= 3 ? Words.RareGift : Words.Gift;
            if (reward == Reward.Rehearse) return Words.Star;
            if (reward == Reward.Stars) return amount + " " + Words.Stars;
            if (reward == Reward.Sectional) return "+1 " + Words.Star + " EACH";
            if (reward == Reward.FullLife) return "FULL " + BattleRules.LifeWord;
            if (reward == Reward.Fight) return Words.EliteFight;
            return "";
        }

        //Event Pick : any event, but never the same one twice in a row
        private static int last = -1;
        public static GameEvent Pick(Random random)
        {
            int pick = random.Next(All.Length);
            if (pick == last) pick = (pick + 1) % All.Length;
            last = pick;
            return All[pick];
        }

        //Check Chance : percent chance a musician passes a check, kept between 5 and 95
        public static int ChanceFor(EventChoice choice, RunState run, Musician m)
        {
            int chance = choice.CheckBase + run.PowerOf(m) * BattleRules.CheckPerPower;
            if (choice.CheckFamily == (int)m.Family) chance += BattleRules.CheckFamilyBonus;
            if (chance < 5) chance = 5;
            if (chance > 95) chance = 95;
            return chance;
        }
    }
}
