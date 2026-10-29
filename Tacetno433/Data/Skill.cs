using System;
using Tacetno433.Core;

namespace Tacetno433.Data
{
    //SkillTarget : who a skill is played at
    public enum SkillTarget { Enemy, AllEnemies, Self, Ally, AllAllies }

    //NoteKind : the four locked note types, as notes of a skill (Alpha 7). ONE RULE (Beta1, 29 Oct) : no sizes,
    //a note asks for the way of its arrow and the moment its ring closes, nothing else.
    //   Swipe  a plain note : the right way, on time
    //   Spark  a second quick swipe right after the note before it, any way
    //   Hold   a swipe, then keep the button down while the ring fills (the baton may move)
    //   Roll   shake the baton as many times as you can while it lasts
    public enum NoteKind { Swipe, Spark, Hold, Roll }

    //SkillNote : one note a skill asks for, its kind and the way of its arrow
    public struct SkillNote
    {
        public NoteKind Kind;
        public Flick Way;

        public SkillNote(NoteKind kind, Flick way)
        {
            Kind = kind;
            Way = way;
        }
    }

    //Skill : one move a musician can play for BREATH (Alpha 7, docs/ALPHA7_DESIGN.md 5.2 and 5.8).
    //EVERY NOTE THAT LANDS DOES THE SKILL ONCE : each note that is not a MISS deals the damage,
    //adds the stacks, heals and gives the breath below. A PERFECT note hits harder (TurnPerfect),
    //a held note to the end gives up to TurnHoldBase + TurnHoldGain of its damage and healing.
    public class Skill
    {
        public string Name = "";
        public string Text = "";             // one short line, shown when the pointer is on the skill
        public int Cost = 2;                 // BREATH
        public SkillTarget Target = SkillTarget.Enemy;
        public SkillNote[] Notes = new SkillNote[0];

        public float Damage;                 // x the musician's attack damage, per note
        public int Dissonance;               // DISSONANCE stacks on the enemy, per note
        public int Crescendo;                // CRESCENDO stacks on the target musicians, per note
        public int Heal;                     // life back to the target musicians, per note
        public int Breath;                   // breath back to the band, per note
        public int OffBeat;                  // OFF-BEAT gauge on the enemy, per note
        public bool Guard;                   // GUARD : the player takes TurnGuardKeep of every hit until their next turn
        public bool Taunt;                   // TAUNT : every enemy aims at the player until their next turn

        public bool HitsEnemy
        {
            get { return Target == SkillTarget.Enemy || Target == SkillTarget.AllEnemies; }
        }
    }

    //SkillList : THE PLACE TO EDIT SKILLS. Two per musician, in the order of MusicianList.All
    //(skill 2k and 2k + 1 belong to musician k). Roles by family (ALPHA7_DESIGN 5.8) :
    //   strings     damage over time, several notes, DISSONANCE
    //   winds       help : healing, breath, CRESCENDO for the band
    //   percussion  OFF-BEAT, GUARD and TAUNT
    //Every skill starts on the musician's own arrow, so a player learns one shape per musician.
    //First draft (18 Oct), not played yet : the numbers are guesses to tune.
    public static class SkillList
    {
        private const Flick L = Flick.Left;
        private const Flick R = Flick.Right;
        private const Flick U = Flick.Up;
        private const Flick D = Flick.Down;

        private static SkillNote S(Flick way) { return new SkillNote(NoteKind.Swipe, way); }
        private static SkillNote H(Flick way) { return new SkillNote(NoteKind.Hold, way); }
        private static SkillNote K() { return new SkillNote(NoteKind.Spark, Flick.None); }
        private static SkillNote T() { return new SkillNote(NoteKind.Roll, Flick.None); }

        public static Skill[] All = new Skill[]
        {
            //PAGANINI, violin (strings, left)
            new Skill { Name = "QUICK BOW", Text = "3 quick hits. Each adds 1 DISSONANCE.", Cost = 2, Target = SkillTarget.Enemy,
                        Notes = new[] { S(L), S(R), S(L) }, Damage = 0.5f, Dissonance = 1 },
            new Skill { Name = "LONG TONE", Text = "One held note. Hold it = harder. 2 DISSONANCE.", Cost = 3, Target = SkillTarget.Enemy,
                        Notes = new[] { H(L) }, Damage = 1.2f, Dissonance = 2 },

            //QUANTZ, flute (winds, centre)
            new Skill { Name = "SOFT AIR", Text = "Heals the whole band 3 per note.", Cost = 2, Target = SkillTarget.AllAllies,
                        Notes = new[] { S(D), S(U) }, Heal = 3 },
            new Skill { Name = "LONG BREATH", Text = "4 soft notes. Each gives 1 BREATH back.", Cost = 2, Target = SkillTarget.Self,
                        Notes = new[] { S(D), S(D), S(U), S(U) }, Breath = 1 },

            //PFUNDT, timpani (percussion, right)
            new Skill { Name = "TIMPANI ROLL", Text = "Shake fast. Big OFF-BEAT.", Cost = 2, Target = SkillTarget.Enemy,
                        Notes = new[] { T() }, Damage = 1f, OffBeat = 30 },
            new Skill { Name = "STAND FIRM", Text = "Enemies aim at PFUNDT, who takes half damage.", Cost = 2, Target = SkillTarget.Self,
                        Notes = new[] { S(R) }, Guard = true, Taunt = true },

            //MALI, so duang (strings, centre)
            new Skill { Name = "BY EAR", Text = "A hit and a spark. Each adds 1 DISSONANCE.", Cost = 2, Target = SkillTarget.Enemy,
                        Notes = new[] { S(D), K() }, Damage = 0.8f, Dissonance = 1 },
            new Skill { Name = "SLIDE", Text = "4 notes up and down. Each adds 1 DISSONANCE.", Cost = 3, Target = SkillTarget.Enemy,
                        Notes = new[] { S(U), S(D), S(U), S(D) }, Damage = 0.4f, Dissonance = 1 },

            //CHAI, pi nai (winds, right)
            new Skill { Name = "HELD NOTE", Text = "Heals one musician. Hold it = more.", Cost = 3, Target = SkillTarget.Ally,
                        Notes = new[] { H(R) }, Heal = 9 },
            new Skill { Name = "CIRCULAR", Text = "Shake fast. Heals everyone 3, 3 BREATH back.", Cost = 3, Target = SkillTarget.AllAllies,
                        Notes = new[] { T() }, Heal = 3, Breath = 3 },

            //NUAN, ranat ek (percussion, left)
            new Skill { Name = "RUN", Text = "3 big hits. OFF-BEAT each.", Cost = 3, Target = SkillTarget.Enemy,
                        Notes = new[] { S(L), S(R), S(L) }, Damage = 0.5f, OffBeat = 12 },
            new Skill { Name = "ONE STEP", Text = "A big hit and a spark. OFF-BEAT each.", Cost = 2, Target = SkillTarget.Enemy,
                        Notes = new[] { S(L), K() }, Damage = 0.7f, OffBeat = 12 },

            //DUPORT, cello (strings, right)
            new Skill { Name = "DEEP BOW", Text = "2 hits. Each adds 1 DISSONANCE.", Cost = 2, Target = SkillTarget.Enemy,
                        Notes = new[] { S(R), S(R) }, Damage = 0.8f, Dissonance = 1 },
            new Skill { Name = "FOUR BARS", Text = "The 4/4 shape. Each note adds 1 DISSONANCE.", Cost = 4, Target = SkillTarget.Enemy,
                        Notes = new[] { S(D), S(L), S(R), S(U) }, Damage = 0.6f, Dissonance = 1 },

            //PUNTO, horn (winds, left)
            new Skill { Name = "FANFARE", Text = "The band hits harder : 1 CRESCENDO per note.", Cost = 2, Target = SkillTarget.AllAllies,
                        Notes = new[] { S(L), S(L) }, Crescendo = 1 },
            new Skill { Name = "LATE ENTRY", Text = "Heals one musician 5 per note.", Cost = 3, Target = SkillTarget.Ally,
                        Notes = new[] { S(L), K() }, Heal = 5 },

            //BORIS, bass drum (percussion, centre)
            new Skill { Name = "THE FLOOR", Text = "Hits every enemy. Hold it = harder.", Cost = 3, Target = SkillTarget.AllEnemies,
                        Notes = new[] { H(D) }, Damage = 1f, OffBeat = 20 },
            new Skill { Name = "THUNDER", Text = "2 big hits and a roll. Big OFF-BEAT.", Cost = 4, Target = SkillTarget.Enemy,
                        Notes = new[] { S(D), S(D), T() }, Damage = 0.8f, OffBeat = 15 },
        };

        //Skills Of : the two skills of one musician
        public static Skill First(Musician m) { return All[Array.IndexOf(MusicianList.All, m) * 2]; }
        public static Skill Second(Musician m) { return All[Array.IndexOf(MusicianList.All, m) * 2 + 1]; }
    }
}
