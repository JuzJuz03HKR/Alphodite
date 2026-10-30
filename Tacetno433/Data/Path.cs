using System;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Core;

namespace Tacetno433.Data
{
    //PathRule : a rule a PATH step adds to a musician, checked in Battle/TurnBattle.cs by name (search the name). The steps
    //that only change a number of a skill need no rule, they rewrite the skill (PathStep below). New rules go at the end.
    public enum PathRule
    {
        None,
        PaidInBlood,     // THE PACT          (PAGANINI) every skill of theirs costs TurnPactLife life and plays TurnBloodScale times as hard
        MarkedSouls,     // THE DEVIL'S OWN   (PAGANINI) IL CANNONE also hits every other enemy with DISSONANCE
        LastCut,         // CAPRICE No. 24    (PAGANINI) the last cut of CAPRICE hits TurnLastCutScale times as hard
        LessonStrong,    // THE METHOD        (QUANTZ)   a LESSON plays TurnLessonStrong instead of TurnLessonScale
        LessonEcho,      // EVERY EVENING     (QUANTZ)   a friend given a LESSON plays their next move again (an ECHO)
        Sanssouci,       // SANSSOUCI         (QUANTZ)   SOFT AIR also clears DISSONANCE and MUTE off the band
        HandToOne,       // ONE TURN          (PFUNDT)   TURN OF THE HAND leaves 1 note at least (not TurnHandLeast)
        CallRetunes,     // RETUNED           (PFUNDT)   THUNDER CALL takes a note off every enemy's next attack too (one, DIE PAUKEN or not)
        HandTwo,         // DIE PAUKEN        (PFUNDT)   TURN OF THE HAND takes 2 notes (not TurnHandCut)
        RollThrows,      // THUNDERCLAP       (PFUNDT)   a PERFECT TIMPANI ROLL throws the first enemy it reaches OFF-BEAT at once
        ShieldThird,     // STRADIVARIUS      (DUPORT)   SHIELD : DUPORT takes TurnShieldThird of a blow (not TurnShieldKeep)
        DentStrikesBack, // NAPOLEON'S DENT   (DUPORT)   a blow DUPORT takes for a friend strikes its enemy back as hard
        ScarTwo,         // OLD WOUNDS        (DUPORT)   THE SCAR gives twice the CRESCENDO
        CallStrong,      // TALLY-HO          (PUNTO)    THE HUNTING CALL plays TurnCallStrong (not TurnHuntingCallScale)
        CallAll,         // THE WHOLE HUNT    (PUNTO)    THE HUNTING CALL reaches everyone after PUNTO in the bar
        ChakramFull,     // THE CATCH         (PUNTO)    an enemy the same CHAKRAM hits again takes it whole
        ChakramLast      // WHIRLWIND         (PUNTO)    the last throw of CHAKRAM hits every enemy
    }

    //PathStep : one card of a PATH (Beta1 30 Oct, docs/BETA1_DIRECTION.md 12.4). It rewrites one of the musician's two
    //skills (Skill 0 or 1 : the first or the second of SkillList), and or adds a rule (PathRule). A number below 0 means
    //"keep it as it is". The rewritten skills are the musician's own for the run (Musician.Kit, PathList.Rebuild).
    public class PathStep
    {
        public string Name = "";
        public string Text = "";               // one short line, what changes, on the card
        public int Skill = -1;                 // the skill it rewrites, 0 the first, 1 the second, -1 none
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
        public PathRule Rule = PathRule.None;

        //Prepared Text
        public string TextWrapped = "";
    }

    //LegendPath : one of the two ways a legend grows in a run, three steps in order (I, II, III)
    public class LegendPath
    {
        public string Name = "";               // "THE VIRTUOSO"
        public string Story = "";              // where it comes from, one line
        public PathStep[] Steps = new PathStep[0];
    }

    //PathList : THE PLACE TO EDIT PATHS (Beta1 30 Oct, docs/BETA1_DIRECTION.md 11.7 point 5 and 12.4). Each of the five
    //legends grows along two paths from their own legend, three steps each. A step changes what a skill does where the
    //player sees it (more cuts, every enemy, a rule of its own), never only a hidden number (11.5 principle 3).
    //   Step I opens on floor 1, II on floor 2, III on floor 3 (StepOpen) : plain early, wild late (the user, 13.13).
    //   A step is taken after a fight won (1 of up to 3 cards, MotifRewardScreen Kind.Path) or when a musician is INSPIRED
    //   in a fight (1 of their 2 next steps, at once, TurnBattle.Inspire).
    //Places follow MusicianList.All : 0 PAGANINI, 1 QUANTZ, 2 PFUNDT, 6 DUPORT, 7 PUNTO (the hidden ones have none).
    //The facts behind the names were checked (30 Oct) : the 24 Caprices are Paganini's Op. 1, Quantz wrote the flute's
    //method (1752) and about 300 concertos for Frederick's evening concerts at Sanssouci, Pfundt came to the Gewandhaus in
    //1835 at Mendelssohn's call, is credited with crank timpani and wrote "Die Pauken" (1849), the Duport Stradivarius
    //(1711) and the dent Napoleon left in it (1812, a legend, not proven), Punto the hand horn player.
    public static class PathList
    {
        private const Flick L = Flick.Left;
        private const Flick R = Flick.Right;
        private const Flick U = Flick.Up;
        private const Flick D = Flick.Down;

        private static SkillNote S(Flick way) { return new SkillNote(NoteKind.Swipe, way); }

        public static readonly string[] Numerals = { "I", "II", "III" };

        private static readonly LegendPath[] none = new LegendPath[0];

        public static LegendPath[][] All = new LegendPath[][]
        {
            //0 PAGANINI : CAPRICE (skill 0), IL CANNONE (skill 1)
            new LegendPath[]
            {
                new LegendPath { Name = "THE DEVIL", Story = "They say the devil taught those hands. Pay, and play harder.", Steps = new PathStep[]
                {
                    new PathStep { Name = "THE PACT", Text = "Every skill : x1.3, for 4 life.", Rule = PathRule.PaidInBlood },
                    new PathStep { Name = "DIABOLUS IN MUSICA", Text = "CAPRICE : 2 DISSONANCE a cut.", Skill = 0, Dissonance = 2 },
                    new PathStep { Name = "THE DEVIL'S OWN", Text = "IL CANNONE also hits every enemy with DISSONANCE.", Rule = PathRule.MarkedSouls },
                } },
                new LegendPath { Name = "THE VIRTUOSO", Story = "The 24 Caprices, faster than any hand should play.", Steps = new PathStep[]
                {
                    new PathStep { Name = "OP. 1", Text = "CAPRICE : 5 cuts.", Skill = 0, Notes = new[] { S(L), S(R), S(L), S(R), S(L) } },
                    new PathStep { Name = "ON ONE STRING", Text = "CAPRICE : every cut the same way, 0.5 each.", Skill = 0, Notes = new[] { S(L), S(L), S(L), S(L), S(L) }, Damage = 0.5f },
                    new PathStep { Name = "CAPRICE No. 24", Text = "CAPRICE : 6 cuts, the last one x3.", Skill = 0, Notes = new[] { S(L), S(L), S(L), S(L), S(L), S(L) }, Rule = PathRule.LastCut },
                } },
            },

            //1 QUANTZ : SOFT AIR (skill 0), THE LESSON (skill 1)
            new LegendPath[]
            {
                new LegendPath { Name = "THE TEACHER", Story = "Taught a king the flute, and wrote the book on it.", Steps = new PathStep[]
                {
                    new PathStep { Name = "THE METHOD", Text = "THE LESSON : x1.6 instead of x1.3.", Rule = PathRule.LessonStrong },
                    new PathStep { Name = "THE CLASS", Text = "THE LESSON : every friend at once, 3 BREATH.", Skill = 1, Everyone = true, Cost = 3 },
                    new PathStep { Name = "EVERY EVENING", Text = "A friend taught plays their next move twice.", Rule = PathRule.LessonEcho },
                } },
                new LegendPath { Name = "THE COURT", Story = "About 300 concertos for the king's evening concerts.", Steps = new PathStep[]
                {
                    new PathStep { Name = "EVENING CONCERT", Text = "SOFT AIR : heals 4 a note.", Skill = 0, Heal = 4 },
                    new PathStep { Name = "300 CONCERTOS", Text = "SOFT AIR : 3 notes, 3 BREATH.", Skill = 0, Notes = new[] { S(D), S(U), S(D) }, Cost = 3 },
                    new PathStep { Name = "SANSSOUCI", Text = "SOFT AIR : clears DISSONANCE and MUTE.", Rule = PathRule.Sanssouci },
                } },
            },

            //2 PFUNDT : TIMPANI ROLL (skill 0), THUNDER CALL (skill 1)
            new LegendPath[]
            {
                new LegendPath { Name = "THE CRANK", Story = "The drums that change key with one turn of the hand.", Steps = new PathStep[]
                {
                    new PathStep { Name = "ONE TURN", Text = "TURN OF THE HAND : down to 1 note.", Rule = PathRule.HandToOne },
                    new PathStep { Name = "RETUNED", Text = "THUNDER CALL : -1 note on every enemy.", Rule = PathRule.CallRetunes },
                    new PathStep { Name = "DIE PAUKEN", Text = "TURN OF THE HAND : 2 notes off.", Rule = PathRule.HandTwo },
                } },
                new LegendPath { Name = "THE THUNDER", Story = "The drum is the thunder that throws everything off the beat.", Steps = new PathStep[]
                {
                    new PathStep { Name = "FORTISSIMO", Text = "TIMPANI ROLL : OFF-BEAT 40.", Skill = 0, OffBeat = 40 },
                    new PathStep { Name = "THE STORM", Text = "TIMPANI ROLL : every enemy, 3 BREATH.", Skill = 0, Everyone = true, Cost = 3 },
                    new PathStep { Name = "THUNDERCLAP", Text = "PERFECT ROLL : one enemy OFF-BEAT at once.", Rule = PathRule.RollThrows },
                } },
            },

            none, none, none,          //3 to 5 : MALI, CHAI, NUAN (hidden)

            //6 DUPORT : SHIELD (skill 0), LANCE (skill 1)
            new LegendPath[]
            {
                new LegendPath { Name = "THE SHIELD", Story = "The Duport Stradivarius, dented and still singing.", Steps = new PathStep[]
                {
                    new PathStep { Name = "STRADIVARIUS", Text = "SHIELD : DUPORT takes a third.", Rule = PathRule.ShieldThird },
                    new PathStep { Name = "THE WALL", Text = "SHIELD : every friend at once.", Skill = 0, Everyone = true },
                    new PathStep { Name = "NAPOLEON'S DENT", Text = "A blow taken for a friend hits back.", Rule = PathRule.DentStrikesBack },
                } },
                new LegendPath { Name = "THE SCAR", Story = "It plays louder for it.", Steps = new PathStep[]
                {
                    new PathStep { Name = "OLD WOUNDS", Text = "THE SCAR : 2 CRESCENDO.", Rule = PathRule.ScarTwo },
                    new PathStep { Name = "RIPOSTE", Text = "LANCE : x3 after a hit.", Skill = 1, Revenge = 3f },
                    new PathStep { Name = "THE LAST LANCE", Text = "LANCE : 3 thrusts.", Skill = 1, Notes = new[] { S(R), S(R), S(R) } },
                } },
            },

            //7 PUNTO : FANFARE (skill 0), CHAKRAM (skill 1)
            new LegendPath[]
            {
                new LegendPath { Name = "THE HUNT", Story = "The hunting horn sends everyone in at once.", Steps = new PathStep[]
                {
                    new PathStep { Name = "TALLY-HO", Text = "THE HUNTING CALL : x1.5.", Rule = PathRule.CallStrong },
                    new PathStep { Name = "THE WHOLE HUNT", Text = "THE HUNTING CALL : everyone after PUNTO.", Rule = PathRule.CallAll },
                    new PathStep { Name = "HAND HORN", Text = "FANFARE : 2 CRESCENDO a note.", Skill = 0, Crescendo = 2 },
                } },
                new LegendPath { Name = "THE CHAKRAM", Story = "The coiled horn, a ring of blades thrown and caught.", Steps = new PathStep[]
                {
                    new PathStep { Name = "WIDER ARC", Text = "CHAKRAM : 4 throws.", Skill = 1, Notes = new[] { S(L), S(R), S(L), S(R) }, Bounce = 4 },
                    new PathStep { Name = "THE CATCH", Text = "CHAKRAM : a second hit is not halved.", Rule = PathRule.ChakramFull },
                    new PathStep { Name = "WHIRLWIND", Text = "CHAKRAM : the last throw hits every enemy.", Rule = PathRule.ChakramLast },
                } },
            },

            none,                      //8 : BORIS (hidden)
        };

        //Of : the two paths of a musician (none for the hidden ones)
        public static LegendPath[] Of(Musician m)
        {
            int i = Array.IndexOf(MusicianList.All, m);
            return i >= 0 && i < All.Length ? All[i] : none;
        }

        //Next : the next step of path p for this musician, null when the path is walked to its end
        public static PathStep Next(Musician m, int p)
        {
            LegendPath[] two = Of(m);
            if (p < 0 || p >= two.Length) return null;
            int level = m.PathLevel[p];
            return level < two[p].Steps.Length ? two[p].Steps[level] : null;
        }

        //Step Open : the next step of path p can be taken on this floor (step I from floor 1, II from 2, III from 3)
        public static bool StepOpen(Musician m, int p, int floor)
        {
            return Next(m, p) != null && m.PathLevel[p] + 1 <= floor;
        }

        //Any Open : one of the musician's two paths has a step open on this floor
        public static bool AnyOpen(Musician m, int floor)
        {
            return StepOpen(m, 0, floor) || StepOpen(m, 1, floor);
        }

        //Take : the musician walks one step on path p, their skills and rules are written again (Rebuild)
        public static bool Take(Musician m, int p, int floor)
        {
            if (!StepOpen(m, p, floor)) return false;
            m.PathLevel[p]++;
            Rebuild(m);
            return true;
        }

        //Rebuild : the musician's two skills for this run, the plain ones of SkillList rewritten by every step taken, path 0
        //first then path 1 (the two paths of one musician never rewrite the same number), and the rules they gave.
        //No step taken : Kit is null and SkillList gives the plain skills.
        public static void Rebuild(Musician m)
        {
            Array.Clear(m.Rules, 0, m.Rules.Length);
            LegendPath[] two = Of(m);
            if (m.PathLevel[0] == 0 && m.PathLevel[1] == 0)
            {
                m.Kit = null;
                return;
            }
            Skill[] kit = { SkillList.Plain(m, 0).Copy(), SkillList.Plain(m, 1).Copy() };
            for (int p = 0; p < two.Length; p++)
                for (int s = 0; s < m.PathLevel[p] && s < two[p].Steps.Length; s++)
                    Apply(two[p].Steps[s], kit, m);
            m.Kit = kit;
        }

        private static void Apply(PathStep step, Skill[] kit, Musician m)
        {
            if (step.Rule != PathRule.None) m.Rules[(int)step.Rule] = true;
            if (step.Skill < 0 || step.Skill > 1) return;
            Skill s = kit[step.Skill];
            if (step.Notes != null) s.Notes = step.Notes;
            if (step.Damage >= 0f) s.Damage = step.Damage;
            if (step.Dissonance >= 0) s.Dissonance = step.Dissonance;
            if (step.Heal >= 0) s.Heal = step.Heal;
            if (step.OffBeat >= 0) s.OffBeat = step.OffBeat;
            if (step.Crescendo >= 0) s.Crescendo = step.Crescendo;
            if (step.Bounce >= 0) s.Bounce = step.Bounce;
            if (step.Revenge >= 0f) s.Revenge = step.Revenge;
            if (step.Cost >= 0) s.Cost = step.Cost;
            if (step.Everyone && s.Target == SkillTarget.Enemy) s.Target = SkillTarget.AllEnemies;
            if (step.Everyone && s.Target == SkillTarget.Ally) s.Target = SkillTarget.AllAllies;
        }

        //Reset : every musician back to no step (a new run, RunState.Start and MusicianList.ResetRehearsals)
        public static void Reset(Musician m)
        {
            m.PathLevel[0] = 0;
            m.PathLevel[1] = 0;
            Rebuild(m);
        }

        //Rule Words : the name of the step that gives each rule, the word the battle page pops when the rule works
        public static string[] RuleWords = new string[32];

        //Text Prepare : called once from LoadContent, the card lines wrapped to the card
        public static void PrepareText(SpriteFont storyFont, float wrapWidth)
        {
            for (int r = 0; r < RuleWords.Length; r++) RuleWords[r] = "";
            for (int i = 0; i < All.Length; i++)
                for (int p = 0; p < All[i].Length; p++)
                    for (int s = 0; s < All[i][p].Steps.Length; s++)
                    {
                        PathStep step = All[i][p].Steps[s];
                        step.TextWrapped = Gfx.WrapText(storyFont, step.Text, wrapWidth, TextSize.StorySmall);
                        if (step.Rule != PathRule.None) RuleWords[(int)step.Rule] = step.Name;
                    }
        }
    }
}
