using System;
using System.Globalization;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Core;

namespace Tacetno433.Data
{
    //StarRule : a rule a star of REHEARSAL gives a musician, checked in Battle/TurnBattle.cs by name (search the name). A star
    //that only changes a number of a skill needs no rule, it rewrites the skill (Star below). New rules go at the end.
    //Until 31 Oct these were the rules of the PATH cards. Four of them no star gives now (marked "no star"). GIFTS (31 Oct) : THE PACT
    //and THE CRANK (ONE TURN with HandTwo) are gifts now (Data/Motif.cs, TurnBattle checks the gift), OLD WOUNDS is not used.
    //The four stay in the list so the places after them keep their numbers.
    public enum StarRule
    {
        None,
        PaidInBlood,     // (no star, not used : the gift THE PACT since 31 Oct)
        MarkedSouls,     // DEVIL'S CANNON  IL CANNONE 3 : IL CANNONE also hits every other enemy with DISSONANCE
        LastCut,         // CAPRICE No. 24      CAPRICE 3 : the last cut of CAPRICE hits TurnLastCutScale times as hard
        LessonStrong,    //                     THE LESSON 1 : a LESSON plays TurnLessonStrong instead of TurnLessonScale
        LessonEcho,      // KING'S LESSON   THE LESSON 3 : a friend given a LESSON plays their next move again (an ECHO)
        Sanssouci,       // SANSSOUCI           SOFT AIR 3 : SOFT AIR also clears DISSONANCE and MUTE off the band
        HandToOne,       // (no star, not used : part of the gift THE CRANK since 31 Oct)
        CallRetunes,     //                     THUNDER CALL 1 : THUNDER CALL takes notes off every enemy's next attack too
        HandTwo,         // (no star, not used : part of the gift THE CRANK since 31 Oct)
        RollThrows,      // THUNDERCLAP         TIMPANI ROLL 3 : a PERFECT TIMPANI ROLL throws the first enemy it reaches OFF-BEAT at once
        ShieldThird,     //                     SHIELD 1 : DUPORT takes TurnShieldThird of a blow (not TurnShieldKeep)
        DentStrikesBack, // NAPOLEON'S DENT     SHIELD 3 : a blow DUPORT takes for a friend strikes its enemy back as hard
        ScarTwo,         // (no star, not used : OLD WOUNDS, THE SCAR twice, until 31 Oct)
        CallStrong,      //                     FANFARE 2 : THE HUNTING CALL plays TurnCallStrong (not TurnHuntingCallScale), after any move of PUNTO
        CallAll,         // THE WHOLE HUNT      FANFARE 3 : after FANFARE, everyone who plays after PUNTO in the bar gets THE HUNTING CALL
        ChakramFull,     //                     CHAKRAM 2 : an enemy the same CHAKRAM hits again takes it whole
        ChakramLast,     // WHIRLWIND           CHAKRAM 3 : the last throw of CHAKRAM hits every enemy
        //REHEARSAL (31 Oct)
        GuardThird,      //                     THUNDER CALL 2 : PFUNDT's GUARD lets TurnShieldThird of a blow through (not TurnGuardKeep)
        CallTwo,         // DIE PAUKEN          THUNDER CALL 3 : THUNDER CALL takes TurnHandPaukenCut notes off (not TurnHandCut)
        LanceAlways,     // THE LAST LANCE      LANCE 3 : LANCE always hits as if DUPORT was just hit
        AttackDissonance,// DIABOLUS IN MUSICA  PAGANINI's ATTACK 3 : their ATTACK adds TurnAttackDissonance DISSONANCE
        AttackHeals,     // EVENING CONCERT     QUANTZ's ATTACK 3 : their ATTACK heals the most hurt musician TurnAttackHeal
        AttackTwice,     // TWIN THUNDER        PFUNDT's ATTACK 3 : their ATTACK hits twice
        AttackGuards,    // STRADIVARIUS        DUPORT's ATTACK 3 : their ATTACK puts DUPORT on GUARD until their next move
        AttackBounces    // WIDER ARC           PUNTO's ATTACK 3 : their ATTACK hits the next enemy in line too
    }

    //Star : one star of one button (REHEARSAL, Beta1 31 Oct, docs/BETA1_DIRECTION.md 17). The first two stars of a skill change
    //its numbers, the third is the skill's legend : its new name and a rule (StarRule). A number below 0 keeps what the skill
    //has. The rewritten skills are the musician's own for the run (Musician.Kit, RehearsalList.Rebuild).
    public class Star
    {
        public string From = "";               // what the page shows before the arrow : "4 CUTS"
        public string To = "";                 // and after it : "5 CUTS"
        public bool Hit;                       // From and To are the skill's hit, worked out from the musician's power (PlainHit)
        public string Name = "";               // the third star : the legend's name, the skill's name from then on ("CAPRICE No. 24")
        public string Line = "";               // the third star : what the legend does, one short line (the ATTACK's too)
        public string SkillText = "";          // the skill's line from then on (Skill.Text), "" keeps it
        public SkillNote[] Notes;              // its new notes, null keeps them
        public float Damage = -1f;
        public int Dissonance = -1;
        public int Heal = -1;
        public int OffBeat = -1;
        public int Crescendo = -1;
        public int Bounce = -1;
        public float Revenge = -1f;
        public int Cost = -1;                  // its new BREATH
        public bool Everyone;                  // the skill reaches every enemy (a skill at one enemy) or the whole band (at one friend)
        public StarRule Rule = StarRule.None;

        //Prepared Text
        public string LineWrapped = "";
    }

    //RehearsalList : THE PLACE TO EDIT REHEARSAL (Beta1 31 Oct, the user picked it, docs/BETA1_DIRECTION.md 16.14 point 1 and 17).
    //A won fight ends with the band rehearsing : one of the nine buttons the band presses in a fight (each legend's ATTACK and
    //two skills) gets a star. Three stars a button, so 27 for the band, and a run gives about 15 to 20 : the player chooses
    //which legend leans on which move (CZN), or makes a favourite as strong as it gets (Limbus).
    //   ATTACK    each star is a tier of the weapon (Musician.Rehearsed, WEAPON TIERS) : WeaponTierPower more power, so every
    //             move of theirs hits harder. The third adds the weapon's legend (a rule on the ATTACK).
    //   a skill   the first two stars change numbers the player can see (more cuts, more heal), the third is its legend.
    //             (FANFARE's second makes THE HUNTING CALL stronger : the CRESCENDO it gives is near its top, TurnCrescendoMost, already.)
    //Where stars come from : every fight won (RehearsalScreen), the rest stop (rest or rehearse), the shop (a star for sale),
    //and only for the ATTACK the luthier, the boss's REFORGE, SECTIONAL in the shop, events and THE ARCHIVE.
    //It replaced the PATH cards, INSPIRED and the separate weapon tiers (the user, 31 Oct : the cards were too deep for a
    //game you play once, growth must be one word in one place). Most PATH rules live on as the stars below.
    //Places follow MusicianList.All : 0 PAGANINI, 1 QUANTZ, 2 PFUNDT, 6 DUPORT, 7 PUNTO (the hidden ones have none).
    //The facts behind the names were checked on 30 Oct (Path) : the 24 Caprices are Paganini's Op. 1, "diabolus in musica" the
    //old name of the devil's interval, Quantz taught Frederick the Great the flute and wrote about 300 concertos for his
    //evening concerts at Sanssouci, Pfundt wrote "Die Pauken" (1849), the Duport Stradivarius (1711) and the dent Napoleon left
    //in it (1812, a legend, not proven), Punto the hunting horn player. DEVIL'S CANNON, TWIN THUNDER, THE LAST LANCE, THE
    //WHOLE HUNT, WIDER ARC, THUNDERCLAP and WHIRLWIND are names only.
    public static class RehearsalList
    {
        public const int Buttons = 3;          // 0 ATTACK, 1 the first skill, 2 the second
        public const int Most = 3;             // the stars of one button

        private const Flick L = Flick.Left;
        private const Flick R = Flick.Right;
        private const Flick U = Flick.Up;
        private const Flick D = Flick.Down;

        private static SkillNote S(Flick way) { return new SkillNote(NoteKind.Swipe, way); }

        //X : a scale as the page shows it, "x1.5" (the same on every computer, whatever its language)
        private static string X(float scale) { return "x" + scale.ToString("0.##", CultureInfo.InvariantCulture); }

        private static readonly Star[][] none = new Star[0][];

        public static Star[][][] All = new Star[][][]
        {
            //0 PAGANINI : ATTACK (IL CANNONE, the weapon), CAPRICE, IL CANNONE (the skill)
            new Star[][]
            {
                new Star[] { new Star(), new Star(),
                    new Star { Name = "DIABOLUS IN MUSICA", Line = "ATTACK adds " + BattleRules.TurnAttackDissonance + " DISSONANCE.", Rule = StarRule.AttackDissonance } },
                new Star[]
                {
                    new Star { From = "4 CUTS", To = "5 CUTS", Notes = new[] { S(L), S(R), S(L), S(R), S(L) }, SkillText = "5 quick cuts. Each adds 1 DISSONANCE." },
                    new Star { From = "1 DISSONANCE", To = "2 DISSONANCE", Dissonance = 2, SkillText = "5 quick cuts. Each adds 2 DISSONANCE." },
                    new Star { Name = "CAPRICE No. 24", Line = "6 cuts, all one way. The last one x3.", Notes = new[] { S(L), S(L), S(L), S(L), S(L), S(L) },
                               Rule = StarRule.LastCut, SkillText = "6 cuts all one way, 2 DISSONANCE each. The last one x3." },
                },
                new Star[]
                {
                    new Star { Hit = true, Damage = 2.2f },
                    new Star { Hit = true, Damage = 2.6f },
                    new Star { Name = "DEVIL'S CANNON", Line = "Also hits every enemy that has DISSONANCE.", Rule = StarRule.MarkedSouls,
                               SkillText = "One big held shot. Goes through GUARD. Also hits every enemy with DISSONANCE." },
                },
            },

            //1 QUANTZ : ATTACK (FLUTE RAPIER), SOFT AIR, THE LESSON
            new Star[][]
            {
                new Star[] { new Star(), new Star(),
                    new Star { Name = "EVENING CONCERT", Line = "ATTACK heals the most hurt of the band " + BattleRules.TurnAttackHeal + ".", Rule = StarRule.AttackHeals } },
                new Star[]
                {
                    new Star { From = "HEAL 3", To = "HEAL 4", Heal = 4, SkillText = "Heals the whole band 4 per note." },
                    new Star { From = "2 NOTES", To = "3 NOTES, 3 BREATH", Notes = new[] { S(D), S(U), S(D) }, Cost = 3, SkillText = "3 notes. Heals the whole band 4 per note." },
                    new Star { Name = "SANSSOUCI", Line = "Also clears DISSONANCE and MUTE.", Rule = StarRule.Sanssouci,
                               SkillText = "Heals the whole band 4 per note. Clears DISSONANCE and MUTE." },
                },
                new Star[]
                {
                    new Star { From = "FRIEND " + X(BattleRules.TurnLessonScale), To = "FRIEND " + X(BattleRules.TurnLessonStrong), Rule = StarRule.LessonStrong,
                               SkillText = "A friend's next skill : wider PERFECT, " + X(BattleRules.TurnLessonStrong) + "." },
                    new Star { From = "1 FRIEND", To = "EVERY FRIEND, 3 BREATH", Everyone = true, Cost = 3, SkillText = "Every friend's next skill : wider PERFECT, " + X(BattleRules.TurnLessonStrong) + "." },
                    new Star { Name = "KING'S LESSON", Line = "A friend taught plays their next move again, at half.", Rule = StarRule.LessonEcho,
                               SkillText = "Every friend's next skill : wider PERFECT, " + X(BattleRules.TurnLessonStrong) + ", played again at half." },
                },
            },

            //2 PFUNDT : ATTACK (TWIN HAMMERS), TIMPANI ROLL, THUNDER CALL
            new Star[][]
            {
                new Star[] { new Star(), new Star(),
                    new Star { Name = "TWIN THUNDER", Line = "ATTACK hits twice.", Rule = StarRule.AttackTwice } },
                new Star[]
                {
                    new Star { From = "OFF-BEAT 30", To = "OFF-BEAT 40", OffBeat = 40, SkillText = "Shake fast. OFF-BEAT 40. Breaks a CHARGE." },
                    new Star { From = "1 ENEMY", To = "EVERY ENEMY, 3 BREATH", Everyone = true, Cost = 3, SkillText = "Shake fast. OFF-BEAT 40 on every enemy. Breaks a CHARGE." },
                    new Star { Name = "THUNDERCLAP", Line = "PERFECT : the first enemy is OFF-BEAT at once.", Rule = StarRule.RollThrows,
                               SkillText = "Every enemy, OFF-BEAT 40. PERFECT : the first one is OFF-BEAT at once." },
                },
                new Star[]
                {
                    new Star { From = "ENEMY NOTES -0", To = "ENEMY NOTES -" + BattleRules.TurnHandCut, Rule = StarRule.CallRetunes,
                               SkillText = "Enemies aim at PFUNDT, who takes half. Their next attacks lose " + BattleRules.TurnHandCut + " note." },
                    new Star { From = "TAKES 1/2", To = "TAKES 1/3", Rule = StarRule.GuardThird,
                               SkillText = "Enemies aim at PFUNDT, who takes a third. Their next attacks lose " + BattleRules.TurnHandCut + " note." },
                    new Star { Name = "DIE PAUKEN", Line = "Every enemy's next attack loses " + BattleRules.TurnHandPaukenCut + " notes.", Rule = StarRule.CallTwo,
                               SkillText = "Enemies aim at PFUNDT, who takes a third. Their next attacks lose " + BattleRules.TurnHandPaukenCut + " notes." },
                },
            },

            none, none, none,          //3 to 5 : MALI, CHAI, NUAN (hidden)

            //6 DUPORT : ATTACK (THE SCARRED SHIELD), SHIELD, LANCE
            new Star[][]
            {
                new Star[] { new Star(), new Star(),
                    new Star { Name = "STRADIVARIUS", Line = "ATTACK puts DUPORT on GUARD : half damage.", Rule = StarRule.AttackGuards } },
                new Star[]
                {
                    new Star { From = "TAKES 1/2", To = "TAKES 1/3", Rule = StarRule.ShieldThird, SkillText = "Guard a friend : their hits land on DUPORT, a third." },
                    new Star { From = "1 FRIEND", To = "EVERY FRIEND", Everyone = true, SkillText = "Guard every friend : their hits land on DUPORT, a third." },
                    new Star { Name = "NAPOLEON'S DENT", Line = "A blow taken for a friend hits back as hard.", Rule = StarRule.DentStrikesBack,
                               SkillText = "Guard every friend, take a third. Every blow taken hits back." },
                },
                new Star[]
                {
                    new Star { From = "x2 AFTER A HIT", To = "x3 AFTER A HIT", Revenge = 3f, SkillText = "2 thrusts. x3 if DUPORT was just hit." },
                    new Star { From = "2 THRUSTS", To = "3 THRUSTS", Notes = new[] { S(R), S(R), S(R) }, SkillText = "3 thrusts. x3 if DUPORT was just hit." },
                    new Star { Name = "THE LAST LANCE", Line = "Always x3, hit or not.", Rule = StarRule.LanceAlways, SkillText = "3 thrusts. Always x3." },
                },
            },

            //7 PUNTO : ATTACK (HORN CHAKRAM), FANFARE, CHAKRAM
            new Star[][]
            {
                new Star[] { new Star(), new Star(),
                    new Star { Name = "WIDER ARC", Line = "ATTACK hits the next enemy too.", Rule = StarRule.AttackBounces } },
                new Star[]
                {
                    new Star { From = "1 CRESCENDO", To = "2 CRESCENDO", Crescendo = 2, SkillText = "The band hits harder : 2 CRESCENDO per note." },
                    new Star { From = "AFTER PUNTO " + X(BattleRules.TurnHuntingCallScale), To = "AFTER PUNTO " + X(BattleRules.TurnCallStrong), Rule = StarRule.CallStrong },
                    new Star { Name = "THE WHOLE HUNT", Line = "After FANFARE, everyone after PUNTO in the bar hits " + X(BattleRules.TurnCallStrong) + ".", Rule = StarRule.CallAll,
                               SkillText = "The band hits harder : 2 CRESCENDO per note. Everyone after PUNTO hits " + X(BattleRules.TurnCallStrong) + "." },
                },
                new Star[]
                {
                    new Star { From = "3 THROWS", To = "4 THROWS", Notes = new[] { S(L), S(R), S(L), S(R) }, Bounce = 4, SkillText = "4 throws, a new enemy each. Hit again = half." },
                    new Star { From = "HIT AGAIN : HALF", To = "HIT AGAIN : WHOLE", Rule = StarRule.ChakramFull, SkillText = "4 throws, a new enemy each. Hit again = whole." },
                    new Star { Name = "WHIRLWIND", Line = "The last throw hits every enemy.", Rule = StarRule.ChakramLast,
                               SkillText = "4 throws, a new enemy each. The last one hits every enemy." },
                },
            },

            none,                      //8 : BORIS (hidden)
        };

        //Of : the three buttons of a musician, each with its three stars (none for the hidden ones)
        public static Star[][] Of(Musician m)
        {
            int i = Array.IndexOf(MusicianList.All, m);
            return i >= 0 && i < All.Length ? All[i] : none;
        }

        //Stars Of : the stars button b has (0 the ATTACK, the weapon's tier, 1 and 2 the skills)
        public static int StarsOf(Musician m, int button)
        {
            if (button == 0) return m.Rehearsed;
            return button == 1 || button == 2 ? m.SkillStars[button - 1] : 0;
        }

        //Next : the next star of button b, null when it has all three
        public static Star Next(Musician m, int button)
        {
            Star[][] buttons = Of(m);
            if (button < 0 || button >= buttons.Length) return null;
            int stars = StarsOf(m, button);
            return stars < buttons[button].Length ? buttons[button][stars] : null;
        }

        //Can Raise : button b can still get a star
        public static bool CanRaise(Musician m, int button)
        {
            return m != null && Next(m, button) != null;
        }

        //Any Left : one of the musician's buttons can still get a star
        public static bool AnyLeft(Musician m)
        {
            for (int b = 0; b < Buttons; b++)
                if (CanRaise(m, b)) return true;
            return false;
        }

        //Raise : button b gets its next star, the skills and rules are written again (Rebuild). The ATTACK's star is a weapon tier.
        public static bool Raise(Musician m, int button)
        {
            if (!CanRaise(m, button)) return false;
            if (button == 0) m.Rehearsed++;
            else m.SkillStars[button - 1]++;
            Rebuild(m);
            return true;
        }

        //Weapon Legend : the rule the ATTACK's third star gives (Musician.Has reads it from the weapon's tier, so a tier set
        //anywhere counts at once)
        public static StarRule WeaponLegend(Musician m)
        {
            Star[][] buttons = Of(m);
            return buttons.Length > 0 && buttons[0].Length >= Most ? buttons[0][Most - 1].Rule : StarRule.None;
        }

        //Rebuild : the musician's two skills for this run, the plain ones of SkillList rewritten by every star they have, and the
        //rules those stars gave. No star on a skill : Kit is null and SkillList gives the plain skills.
        public static void Rebuild(Musician m)
        {
            Array.Clear(m.Rules, 0, m.Rules.Length);
            Star[][] buttons = Of(m);
            if (buttons.Length < Buttons || (m.SkillStars[0] == 0 && m.SkillStars[1] == 0))
            {
                m.Kit = null;
                return;
            }
            Skill[] kit = { SkillList.Plain(m, 0).Copy(), SkillList.Plain(m, 1).Copy() };
            for (int k = 0; k < 2; k++)
                for (int s = 0; s < m.SkillStars[k] && s < buttons[k + 1].Length; s++)
                    Apply(buttons[k + 1][s], kit[k], m);
            m.Kit = kit;
        }

        //Apply : one star on a skill (m null : a copy worked out for the page, no rule is given)
        private static void Apply(Star star, Skill s, Musician m)
        {
            if (star.Rule != StarRule.None && m != null) m.Rules[(int)star.Rule] = true;
            if (s == null) return;
            if (star.Name.Length > 0) s.Name = star.Name;
            if (star.SkillText.Length > 0) s.Text = star.SkillText;
            if (star.Notes != null) s.Notes = star.Notes;
            if (star.Damage >= 0f) s.Damage = star.Damage;
            if (star.Dissonance >= 0) s.Dissonance = star.Dissonance;
            if (star.Heal >= 0) s.Heal = star.Heal;
            if (star.OffBeat >= 0) s.OffBeat = star.OffBeat;
            if (star.Crescendo >= 0) s.Crescendo = star.Crescendo;
            if (star.Bounce >= 0) s.Bounce = star.Bounce;
            if (star.Revenge >= 0f) s.Revenge = star.Revenge;
            if (star.Cost >= 0) s.Cost = star.Cost;
            if (star.Everyone && s.Target == SkillTarget.Enemy) s.Target = SkillTarget.AllEnemies;
            if (star.Everyone && s.Target == SkillTarget.Ally) s.Target = SkillTarget.AllAllies;
        }

        //Reset : no star on either skill (a new run, RunState.Start and MusicianList.ResetRehearsals, which also sets the weapon back)
        public static void Reset(Musician m)
        {
            m.SkillStars[0] = 0;
            m.SkillStars[1] = 0;
            Rebuild(m);
        }

        //======================================================================================
        //The Words of a star (made when a page opens, never in Draw)
        //======================================================================================

        //Skill Of : the skill button b plays now (null for the ATTACK)
        public static Skill SkillOf(Musician m, int button)
        {
            if (button == 1) return SkillList.First(m);
            if (button == 2) return SkillList.Second(m);
            return null;
        }

        //Button Name : the button's name as the battle shows it now
        public static string ButtonName(Musician m, int button)
        {
            Skill s = SkillOf(m, button);
            return s != null ? s.Name : "ATTACK";
        }

        //From and To : what the next star of button b changes, before and after the arrow. power is the musician's power now
        //(RunState.PowerOf). The ATTACK shows the power, a skill's hit (Hit) is worked out like the battle's skill card at the start
        //of a fight, a legend shows the skill's name becoming the legend's.
        public static string FromText(Musician m, int button, int power)
        {
            Star next = Next(m, button);
            if (next == null) return "";
            if (button == 0) return "POWER " + power;
            Skill now = SkillOf(m, button);
            if (next.Name.Length > 0) return now.Name;
            if (next.Hit) return "HIT " + PlainHit(power, now);
            return next.From;
        }

        public static string ToText(Musician m, int button, int power)
        {
            Star next = Next(m, button);
            if (next == null) return "";
            if (button == 0) return "POWER " + (power + BattleRules.WeaponTierPower);
            if (next.Name.Length > 0) return next.Name;
            if (next.Hit)
            {
                Skill after = SkillOf(m, button).Copy();
                Apply(next, after, null);
                return "HIT " + PlainHit(power, after);
            }
            return next.To;
        }

        //Plain Hit : one note of a skill landing GOOD (a hold held to the end) with this much power, the number the battle's skill
        //card shows when nothing else adds to it (TurnBattle.HitFor without the fight's own scales)
        public static int PlainHit(int power, Skill s)
        {
            float hold = s.Notes.Length > 0 && s.Notes[0].Kind == NoteKind.Hold ? BattleRules.TurnHoldBase + BattleRules.TurnHoldGain : 1f;
            return Math.Max(1, (int)Math.Round(power * BattleRules.TurnAttackPerPower * BattleRules.TurnGood * hold * s.Damage));
        }

        //Rule Words : the name of the star that gives each rule (a legend's), the word the battle page pops when the rule works
        public static string[] RuleWords = new string[32];

        //Text Prepare : called once from LoadContent, the legends' lines wrapped for the rehearsal page (the ATTACK's legend has
        //its name in front, its button keeps the word ATTACK)
        public static void PrepareText(SpriteFont storyFont, float wrapWidth)
        {
            for (int r = 0; r < RuleWords.Length; r++) RuleWords[r] = "";
            for (int i = 0; i < All.Length; i++)
                for (int b = 0; b < All[i].Length; b++)
                    for (int s = 0; s < All[i][b].Length; s++)
                    {
                        Star star = All[i][b][s];
                        string line = b == 0 && star.Name.Length > 0 ? star.Name + " : " + star.Line : star.Line;
                        star.LineWrapped = Gfx.WrapText(storyFont, line, wrapWidth, TextSize.StorySmall);
                        if (star.Rule != StarRule.None && star.Name.Length > 0) RuleWords[(int)star.Rule] = star.Name;
                    }
        }
    }
}
