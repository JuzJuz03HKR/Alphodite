using System;
using Tacetno433.Core;

namespace Tacetno433.Data
{
    //SkillTarget : who a skill is played at
    public enum SkillTarget { Enemy, AllEnemies, Self, Ally, AllAllies }

    //SKILL KIND (Beta1, 29 Oct) : what a skill is for, seen at once. The user : a skill must show straight away if it
    //attacks, defends or heals, by its colour or its icon (docs/BETA1_DIRECTION.md 11.7 point 3). Every skill has
    //exactly one kind. Its colour is Palette.Kind, on the skill's button (later its icon, its aim ring, the mark on a friend).
    //   Attack  hurts the enemies
    //   Defend  keeps the band from harm (GUARD, TAUNT)
    //   Heal    gives life back
    //   Boost   makes the band stronger (CRESCENDO, BREATH)
    public enum SkillKind { Attack, Defend, Heal, Boost }

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
        public SkillKind Kind = SkillKind.Attack;   // SKILL KIND : what it is for, at a glance
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

        //The five legends' own moves (Beta1 29 Oct, docs/BETA1_DIRECTION.md 11.6), see TurnBattle (search the names)
        public bool Shield;                  // SHIELD : the player stands in front of the friend it is played at until their next turn
        public bool Lesson;                  // THE LESSON : the friend's next skill is easier to swipe and plays harder
        public int Bounce;                   // CHAKRAM : each note hits the next enemy in line, this many enemies at most (0 for a plain skill)
        public float Revenge;                // LANCE : x this when the player has been hit since their last move (0 for none)

        //ENEMY MOVES (Beta1 30 Oct) : the answers to TACET's GUARD and CHARGE (Data/Enemy.cs EnemyMove)
        public bool Pierce;                  // PIERCE : its damage goes through an enemy's BLOCK (IL CANNONE)
        public bool Breaks;                  // BREAK : a note that lands on an enemy breaks its CHARGE (TIMPANI ROLL)

        public bool HitsEnemy
        {
            get { return Target == SkillTarget.Enemy || Target == SkillTarget.AllEnemies; }
        }

        //Others Only : a skill about standing by someone else (SHIELD, THE LESSON) is never played at the one who plays it
        public bool OthersOnly
        {
            get { return Shield || Lesson; }
        }

        //Copy : a skill of its own to rewrite (REHEARSAL, Data/Rehearsal.cs). The notes are shared until a star gives new ones.
        public Skill Copy()
        {
            return (Skill)MemberwiseClone();
        }
    }

    //SkillList : THE PLACE TO EDIT SKILLS. Two per musician, in the order of MusicianList.All
    //(skill 2k and 2k + 1 belong to musician k), so the five legends' kits sit in the places 0 1, 2 3, 4 5, 12 13 and 14 15.
    //THE FIVE KITS (Beta1 29 Oct, docs/BETA1_DIRECTION.md 11.6, the user : "use these for now, adjust later") : every skill
    //comes from who the musician was and answers one problem in a fight, so the player thinks about whom to bring and when.
    //   PAGANINI  the damage dealer            QUANTZ  the teacher and the only healer
    //   PFUNDT    breaks TACET's beat          DUPORT  the guardian
    //   PUNTO     rallies the band, reaches a pack
    //The hidden musicians (MALI, CHAI, NUAN, BORIS) keep their Alpha 7 skills for old saves.
    //Every skill starts on the musician's own arrow, so a player learns one shape per musician.
    //Numbers (29 Oct, turnsim 12 seeds) : IL CANNONE 1.8 and CHAKRAM 0.8 were the first guesses, 1.5 and 1 put the runs won
    //with each legend in the band between 52 and 56 percent (47 to 57 with the old kits). 30 Oct : IL CANNONE 1.8 again, turnsim
    //plays CHAKRAM at one enemy too now (2 for 3 BREATH, IL CANNONE 1.5 held was 2.1), PAGANINI's bands won 9 percent less.
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
            //PAGANINI, violin (strings, left) : rumour said they sold their soul to the devil and played faster than any hand
            //could (the 24 Caprices), on the violin they called their cannon. CAPRICE answers the enemy that must fall first,
            //every cut leaves DISSONANCE (the devil's interval). IL CANNONE is one big shot, a cannon goes through any GUARD.
            new Skill { Name = "CAPRICE", Kind = SkillKind.Attack, Text = "4 quick cuts. Each adds 1 " + Words.Burn + ".", Cost = 2, Target = SkillTarget.Enemy,
                        Notes = new[] { S(L), S(R), S(L), S(R) }, Damage = 0.4f, Dissonance = 1 },
            new Skill { Name = "IL CANNONE", Kind = SkillKind.Attack, Text = "One big held shot. Hold it = harder. Goes through " + Words.Armor + ".", Cost = 3, Target = SkillTarget.Enemy,
                        Notes = new[] { H(L) }, Damage = 1.8f, Pierce = true },

            //QUANTZ, flute (winds, centre) : taught Frederick the Great the flute and wrote the book on playing it (1752).
            //SOFT AIR when the band is hurt (the only healer of the five), THE LESSON so a friend's big skill lands.
            new Skill { Name = "SOFT AIR", Kind = SkillKind.Heal, Text = "Heals the whole band 3 per note.", Cost = 2, Target = SkillTarget.AllAllies,
                        Notes = new[] { S(D), S(U) }, Heal = 3 },
            new Skill { Name = "THE LESSON", Kind = SkillKind.Boost, Text = "A friend gets " + Words.Focus + " for their next skill.", Cost = 2, Target = SkillTarget.Ally,
                        Notes = new[] { S(U), S(D) }, Lesson = true },

            //PFUNDT, timpani (percussion, right) : the crank timpani that change key with one turn of the hand. The drum is the
            //thunder that throws everything off the beat. TIMPANI ROLL against a big move (it breaks a CHARGE), THUNDER CALL to save a friend
            //(the loudest sound on the stage, every enemy has to turn and look).
            new Skill { Name = "TIMPANI ROLL", Kind = SkillKind.Attack, Text = "Shake fast. Big " + Words.Stun + ". Breaks a " + Words.Charge + ".", Cost = 2, Target = SkillTarget.Enemy,
                        Notes = new[] { T() }, Damage = 1f, OffBeat = 30, Breaks = true },
            new Skill { Name = "THUNDER CALL", Kind = SkillKind.Defend, Text = "Every enemy aims at PFUNDT, who gets " + Words.Guard + ".", Cost = 2, Target = SkillTarget.Self,
                        Notes = new[] { S(R) }, Guard = true, Taunt = true },

            //MALI, so duang (strings, centre)
            new Skill { Name = "BY EAR", Kind = SkillKind.Attack, Text = "A hit and a spark. Each adds 1 " + Words.Burn + ".", Cost = 2, Target = SkillTarget.Enemy,
                        Notes = new[] { S(D), K() }, Damage = 0.8f, Dissonance = 1 },
            new Skill { Name = "SLIDE", Kind = SkillKind.Attack, Text = "4 notes up and down. Each adds 1 " + Words.Burn + ".", Cost = 3, Target = SkillTarget.Enemy,
                        Notes = new[] { S(U), S(D), S(U), S(D) }, Damage = 0.4f, Dissonance = 1 },

            //CHAI, pi nai (winds, right)
            new Skill { Name = "HELD NOTE", Kind = SkillKind.Heal, Text = "Heals one musician. Hold it = more.", Cost = 3, Target = SkillTarget.Ally,
                        Notes = new[] { H(R) }, Heal = 9 },
            new Skill { Name = "CIRCULAR", Kind = SkillKind.Heal, Text = "Shake fast. Heals everyone 3, 3 BREATH back.", Cost = 3, Target = SkillTarget.AllAllies,
                        Notes = new[] { T() }, Heal = 3, Breath = 3 },

            //NUAN, ranat ek (percussion, left)
            new Skill { Name = "RUN", Kind = SkillKind.Attack, Text = "3 big hits. Each adds " + Words.Stun + ".", Cost = 3, Target = SkillTarget.Enemy,
                        Notes = new[] { S(L), S(R), S(L) }, Damage = 0.5f, OffBeat = 12 },
            new Skill { Name = "ONE STEP", Kind = SkillKind.Attack, Text = "A big hit and a spark. Each adds " + Words.Stun + ".", Cost = 2, Target = SkillTarget.Enemy,
                        Notes = new[] { S(L), K() }, Damage = 0.7f, OffBeat = 12 },

            //DUPORT, cello (strings, right) : the Duport Stradivarius that Napoleon left a dent in. The cello is a great shield, the
            //bow a lance ("It plays louder for it"). SHIELD for a friend a big move is aimed at (holding the note = holding the
            //shield up), LANCE to pay back the blow that just landed.
            new Skill { Name = "SHIELD", Kind = SkillKind.Defend, Text = "Protect a friend : their hits land on DUPORT, halved.", Cost = 2, Target = SkillTarget.Ally,
                        Notes = new[] { H(R) }, Shield = true },
            new Skill { Name = "LANCE", Kind = SkillKind.Attack, Text = "2 thrusts. Twice as hard if DUPORT was just hit.", Cost = 2, Target = SkillTarget.Enemy,
                        Notes = new[] { S(R), S(R) }, Damage = 0.7f, Revenge = 2f },

            //PUNTO, horn (winds, left) : the horn is the hunting horn that sends everyone in at once, and Punto was once more
            //famous than Beethoven. The coiled horn is a ring of blades, thrown and caught again. FANFARE to finish a fight
            //against one big enemy, CHAKRAM against a pack.
            new Skill { Name = "FANFARE", Kind = SkillKind.Boost, Text = "Every musician gets 1 " + Words.Volume + " per note.", Cost = 2, Target = SkillTarget.AllAllies,
                        Notes = new[] { S(L), S(L) }, Crescendo = 1 },
            new Skill { Name = "CHAKRAM", Kind = SkillKind.Attack, Text = "3 throws, a new enemy each. Hit again = half.", Cost = 3, Target = SkillTarget.Enemy,
                        Notes = new[] { S(L), S(R), S(L) }, Damage = 1f, Bounce = 3 },

            //BORIS, bass drum (percussion, centre)
            new Skill { Name = "THE FLOOR", Kind = SkillKind.Attack, Text = "Hits every enemy. Hold it = harder.", Cost = 3, Target = SkillTarget.AllEnemies,
                        Notes = new[] { H(D) }, Damage = 1f, OffBeat = 20 },
            new Skill { Name = "THUNDER", Kind = SkillKind.Attack, Text = "2 big hits and a roll. Big " + Words.Stun + ".", Cost = 4, Target = SkillTarget.Enemy,
                        Notes = new[] { S(D), S(D), T() }, Damage = 0.8f, OffBeat = 15 },
        };

        //Skills Of : the two skills of one musician in this run, as their stars rewrote them (Musician.Kit, Data/Rehearsal.cs)
        public static Skill First(Musician m) { return m.Kit != null ? m.Kit[0] : Plain(m, 0); }
        public static Skill Second(Musician m) { return m.Kit != null ? m.Kit[1] : Plain(m, 1); }

        //Plain : skill k (0 or 1) of the musician before any star
        public static Skill Plain(Musician m, int k) { return All[Array.IndexOf(MusicianList.All, m) * 2 + k]; }
    }
}
