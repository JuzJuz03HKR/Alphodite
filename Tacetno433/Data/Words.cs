using System;
using System.Globalization;

namespace Tacetno433.Data
{
    //======================================================================================
    //WORDS (Beta1 31 Oct, docs/BETA1_DIRECTION.md 16.14 item 3 and 19) : every rule word the player reads, in one place, like
    //the numbers in BattleRules and the colours in Palette. Change a word here and every skill line, icon, pop and page that
    //uses it changes with it.
    //The user (USER_BRIEFS 13.45) : a player reads a word once and knows what it is, and the word is tied to an action or to
    //the GUI. So every rule word here is
    //   - a word players already know from other games (BURN STUN ARMOR GUARD FOCUS) or a plain one (VOLUME TURN PLAY),
    //     never a word from a music lesson. Music stays in the NAMES : skills, gifts, bosses, the conductors' FINALEs.
    //   - one meaning only, in every place. Until 31 Oct GUARD meant three things and REST two.
    //   - shown with its line (WORD = what it does) where the player looks : the skill card (KeywordsIn), the icon under the
    //     pointer (BattleScreen.Marks), HOW TO PLAY. And the word pops at the moment it works (BattleScreen.ShowEvents).
    //The names in the code and in the save keep the old words (Dissonance, Crescendo, OffBeat, Mute, Block, Lesson,
    //Signature, Bar, Luthier, Echoes), so old saves load and the rules did not move.
    //======================================================================================
    public static class Words
    {
        //Statuses and the STUN bar
        public const string Burn = "BURN";            // DISSONANCE in the code : life lost at the end of each of its turns, then 1 less
        public const string Volume = "VOLUME";        // CRESCENDO in the code : every point hits harder, a MISS loses it all
        public const string Stun = "STUN";            // OFF-BEAT in the code : the bar under an enemy's life
        public const string Stunned = "STUNNED";      //   the bar full : it loses its next turn and takes more damage
        public const string Muted = "MUTED";          // MUTE in the code : no skills for a turn
        public const string Guard = "GUARD";          // a musician takes half until their next turn (THUNDER CALL, THE KING'S FAVOUR)
        public const string Armor = "ARMOR";          // Block in the code (TACET's GUARD until 31 Oct) : damage it takes before its life
        public const string ArmorUp = "ARMOR UP";     // the intent that raises ARMOR (Ico.Raise)
        public const string Focus = "FOCUS";          // Lesson in the code, the mark THE LESSON leaves : the next skill is easier and harder
        public const string Shield = "SHIELD";        // DUPORT stands in front of a friend
        public const string Echo = "ECHO";            // FUGUE : the next move plays again

        //What an enemy will do (its intent)
        public const string Attack = "ATTACK";
        public const string Charge = "CHARGE";
        public const string Charged = "CHARGED";      //   the charged blow itself
        public const string Broken = "BROKEN";
        public const string CutIn = "CUT IN";
        public const string Heal = "HEAL";            // LULLABY (THE LULL) : REST until 31 Oct, the route's REST is a place
        public const string Everyone = "EVERYONE";    // THE COURT (THE STAFF)
        public const string BigAttack = "BIG ATTACK"; // CADENZA in the code : every third attack of a boss

        //The fight
        public const string Finale = "FINALE";        // SIGNATURE in the code : the conductor's big move, PERFECTs fill its diamonds
        public const string FinaleReady = "FINALE READY";
        public const string Turn = "TURN";            // BAR in the code : one round, the band's moves then TACET's
        public const string Turns = "TURNS";
        public const string Play = "PLAY";            // PLAY THE BAR until 31 Oct : the button that plays the turn

        //The run
        public const string Workshop = "WORKSHOP";    // LUTHIER in the code : a place where one ATTACK gets a star, free
        public const string Applause = "APPLAUSE";    // ECHOES in the code : what a run leaves behind, spent in THE ARCHIVE
        public const string Shards = "SHARDS";

        //======================================================================================
        //Lines : what each word does, shown after "WORD = ". Plain words, one short line, the numbers from BattleRules.
        //======================================================================================
        public static readonly string BurnLine = "loses this much life at the end of its turn, then 1 less";
        public static readonly string VolumeLine = "every point hits " + Percent(BattleRules.TurnCrescendoStep) + " percent harder. A MISS loses it all";
        public static readonly string StunLine = "fills the bar under its life. Full, it is " + Stunned;
        public static readonly string StunnedLine = "it loses its next turn and takes " + Percent(BattleRules.TurnOffBeatScale) + " percent more damage";
        public static readonly string MutedLine = "no skills for this many turns. ATTACK still works";
        public static readonly string GuardLine = "takes half of every hit until their next turn";
        public static readonly string ArmorLine = "takes the band's damage before its life. IL CANNONE goes through";
        public static readonly string ArmorUpLine = "this turn it gains this much " + Armor + " and does not attack";
        public static readonly string FocusLine = "their next skill is easier to swipe and hits harder";
        public static readonly string ShieldLine = "DUPORT stands in front and takes their hits";
        public static readonly string EchoLine = "their next move plays again";
        public static readonly string AttackLine = "its notes hit the marked musician. Swipe back to block them";
        public static readonly string ChargeLine = "next turn one big note. " + Stun + " it or hit it with TIMPANI ROLL to break it";
        public static readonly string BrokenLine = "its " + Charge + " was broken, the big note will not come";
        public static readonly string CutInLine = "it moves in the middle of your turn";
        public static readonly string HealLine = "this turn it heals instead of attacking";
        public static readonly string EveryoneLine = "its notes hit every musician standing";
        public static readonly string FinaleLine = "the conductor's big move. PERFECTs fill the diamonds, SPACE plays it";

        //Keywords : the words a skill's line can use, with their lines, for the skill card (KeywordsIn). STUNNED before STUN.
        private static readonly string[] keys = { Burn, Volume, Stunned, Stun, Muted, Guard, Armor, Focus, Shield, Echo, Charge };
        private static readonly string[] keyLines = { BurnLine, VolumeLine, StunnedLine, StunLine, MutedLine, GuardLine, ArmorLine, FocusLine, ShieldLine, EchoLine, ChargeLine };

        //Keyword Lines : "BURN  =  loses this much life ...", made once. The card draws the word bright (KeywordNames) and
        //the rest dim (KeywordRests, "=  loses this much life ..."), KeywordLines is the two together for its width.
        public static readonly string[] KeywordNames = keys;
        public static readonly string[] KeywordRests = MakeKeywordLines("");
        public static readonly string[] KeywordLines = MakeKeywordLines(null);

        private static string[] MakeKeywordLines(string name)
        {
            string[] lines = new string[keys.Length];
            for (int i = 0; i < keys.Length; i++) lines[i] = (name == null ? keys[i] + "  " : "") + "=  " + keyLines[i];
            return lines;
        }

        //Keywords In : the keywords a text uses (whole words only, STUN is not found in STUNNED), at most "most" of them,
        //written into "found" as their places in KeywordLines. Returns how many. Called when the skill card changes, not in Draw.
        public static int KeywordsIn(string text, int[] found, int most)
        {
            int count = 0;
            if (string.IsNullOrEmpty(text)) return 0;
            for (int i = 0; i < keys.Length && count < most && count < found.Length; i++)
                if (HasWord(text, keys[i])) found[count++] = i;
            return count;
        }

        //Has Word : the word stands on its own in the text (no letter just before or after it)
        public static bool HasWord(string text, string word)
        {
            int at = text.IndexOf(word, StringComparison.Ordinal);
            while (at >= 0)
            {
                bool startOk = at == 0 || !char.IsLetter(text[at - 1]);
                int end = at + word.Length;
                bool endOk = end >= text.Length || !char.IsLetter(text[end]);
                if (startOk && endOk) return true;
                at = text.IndexOf(word, at + 1, StringComparison.Ordinal);
            }
            return false;
        }

        //Percent : "15" for 0.15 and "50" for 1.5, so a line says "15 percent harder" whatever the rule is written as
        public static string Percent(float scale)
        {
            float part = scale >= 1f ? scale - 1f : scale;
            return ((int)Math.Round(part * 100f)).ToString(CultureInfo.InvariantCulture);
        }
    }
}
