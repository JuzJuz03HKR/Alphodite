using Tacetno433.Battle;

namespace Tacetno433.Data
{
    //SongChart : a real piece of music, cut into phrases of 8 beats (two bars of 4/4) for TACET to play
    //(round 15, 29 Sep : the player asked for the fights to BE songs, so a duel played well sounds
    //like the piece). TACET's notes follow the song exactly : where a note is, when it is silent,
    //and how loud it is written. Every note the baton lands, the band plays at that pitch, each
    //player on their own instrument (SoundBank.PlayInstrument).
    //
    //A phrase is written as 8 words, one per beat :
    //   "E4p"   the note E in octave 4, soft (p). Middle C is C4.
    //   "G4f"   loud (f).   "F#4mf" a sharp and a plain mark (mf).
    //   "-"     TACET is silent on that beat, a rest for the band.
    //Only songs whose composers died long ago, so the music is free for everyone to use. The
    //notes come from the score, never from a recording (a recording belongs to its players).
    //
    //The phrases go in order through a fight : round 1 plays the first phrase, round 2 the next
    //two (REPEATS), round 3 the next three, and the song starts over when it runs out.
    public class SongChart
    {
        public string Name = "";
        public string Composer = "";
        public string[] Phrases = new string[0];

        //Parsed : made by SongList.Prepare, one row per phrase
        public int[,] Notes;          // MIDI note number on each beat, 0 for silent
        public Choice[,] Marks;       // the loudness written on each beat

        //Prepared Text
        public string Label = "";     // "ODE TO JOY  /  BEETHOVEN", shown in the duel

        public int PhraseCount
        {
            get { return Phrases.Length; }
        }
    }

    //SongList : THE PLACE TO EDIT SONGS. An enemy plays one by pointing at it (Enemy.Song).
    public static class SongList
    {
        //Ode To Joy : L. v. Beethoven, Symphony No. 9 (1824), the main theme in C major, one note
        //per beat. A dotted note is written on its beat, a held one is followed by a rest.
        //The marks rise with the melody to f on its highest notes.
        public static SongChart OdeToJoy = new SongChart
        {
            Name = "ODE TO JOY",
            Composer = "BEETHOVEN",
            Phrases = new string[]
            {
                "E4p  E4p  F4mf G4f   G4f  F4mf E4p  D4p",
                "C4p  C4p  D4mf E4mf  E4f  D4mf D4p  -",
                "E4p  E4p  F4mf G4f   G4f  F4mf E4p  D4p",
                "C4p  C4p  D4mf E4mf  D4mf C4p  C4p  -",
                "D4mf D4mf E4mf C4p   D4mf E4f  E4mf C4p",
                "D4mf E4f  E4mf D4p   C4p  D4mf G3f  -",
                "E4f  E4f  F4f  G4f   G4f  F4mf E4mf D4mf",
                "C4mf C4mf D4mf E4f   D4mf C4p  C4p  -",
            }
        };

        //Twinkle : W. A. Mozart, the theme of his twelve variations on "Ah, vous dirai-je, maman",
        //K. 265 (1781), a French song everyone knows as Twinkle, Twinkle, Little Star. C major, 2/4,
        //one note per beat, a held note followed by a rest (29 Sep, the second song).
        public static SongChart Twinkle = new SongChart
        {
            Name = "AH, VOUS DIRAI-JE, MAMAN",
            Composer = "MOZART",
            Phrases = new string[]
            {
                "C4p  C4p  G4mf G4mf  A4f  A4f  G4mf -",
                "F4mf F4mf E4p  E4p   D4mf D4mf C4p  -",
                "G4mf G4mf F4p  F4p   E4mf E4mf D4p  -",
                "G4f  G4f  F4mf F4mf  E4mf E4mf D4p  -",
                "C4p  C4p  G4mf G4mf  A4f  A4f  G4mf -",
                "F4mf F4mf E4p  E4p   D4mf D4mf C4p  -",
            }
        };

        //Moonlight : "Au clair de la lune", a French song of the 18th century, long said to be
        //by J.-B. Lully (nobody is sure, so the label says TRAD.). C major, 4/4, a half note is a
        //note and a rest, the whole note at the end of a line is a note and three rests.
        public static SongChart Moonlight = new SongChart
        {
            Name = "AU CLAIR DE LA LUNE",
            Composer = "TRAD. / LULLY",
            Phrases = new string[]
            {
                "C4p  C4p  C4p  D4mf  E4mf -    D4mf -",
                "C4p  E4mf D4p  D4p   C4p  -    -    -",
                "C4p  C4p  C4p  D4mf  E4mf -    D4mf -",
                "C4p  E4mf D4p  D4p   C4p  -    -    -",
                "D4f  D4f  D4f  D4f   A3mf -    A3mf -",
                "D4f  C4mf B3mf A3p   G3p  -    -    -",
                "C4p  C4p  C4p  D4mf  E4mf -    D4mf -",
                "C4p  E4mf D4p  D4p   C4p  -    -    -",
            }
        };

        //Fifth Symphony : L. v. Beethoven, Symphony No. 5 (1808), the four-note opening (fate knocking)
        //and its answer a step lower, loud then soft. Eb is written D# (the same key on a piano).
        public static SongChart FifthSymphony = new SongChart
        {
            Name = "SYMPHONY NO. 5",
            Composer = "BEETHOVEN",
            Phrases = new string[]
            {
                "-    G4f  G4f  G4f   D#4f -    -    -",
                "-    F4f  F4f  F4f   D4f  -    -    -",
                "-    G4mf G4mf G4mf  D#4mf -   -    -",
                "-    F4p  F4p  F4p   D4p  -    -    -",
            }
        };

        //Prelude In C : J. S. Bach, The Well-Tempered Clavier I, Prelude in C major BWV 846 (1722),
        //the first five bars, half a bar of broken chord per phrase. A note on every beat, no rest.
        public static SongChart PreludeInC = new SongChart
        {
            Name = "PRELUDE IN C",
            Composer = "BACH",
            Phrases = new string[]
            {
                "C4p  E4p  G4mf C5mf  E5f  G4mf C5mf E5f",
                "C4p  D4p  A4mf D5mf  F5f  A4mf D5mf F5f",
                "B3p  D4p  G4mf D5mf  F5f  G4mf D5mf F5f",
                "C4p  E4p  G4mf C5mf  E5f  G4mf C5mf E5f",
                "A3p  C4p  E4mf A4mf  C5f  E4mf A4mf C5f",
            }
        };

        //Rondo Alla Turca : W. A. Mozart, Piano Sonata No. 11 K. 331 (1783), the last movement's
        //opening, every sixteenth note on its own beat (slowed down so it can be conducted).
        public static SongChart RondoAllaTurca = new SongChart
        {
            Name = "RONDO ALLA TURCA",
            Composer = "MOZART",
            Phrases = new string[]
            {
                "B4p  A4p  G#4p A4p   C5f  -    -    -",
                "D5p  C5p  B4p  C5p   E5f  -    -    -",
                "F5mf E5mf D#5mf E5mf B5f  A5mf G#5mf A5mf",
                "B5mf A5mf G#5mf A5mf C6f  -    -    -",
            }
        };

        //Nacht Musik : W. A. Mozart, Eine kleine Nachtmusik K. 525 (1787), the opening four bars,
        //one note per beat (the quick eighth notes are shortened to the note on the beat).
        public static SongChart NachtMusik = new SongChart
        {
            Name = "EINE KLEINE NACHTMUSIK",
            Composer = "MOZART",
            Phrases = new string[]
            {
                "G4f  D4mf G4f  D4mf  G4mf B4mf D5f  -",
                "C5f  A4mf C5f  A4mf  C5mf A4mf F#4mf D4p",
                "G4f  D4mf G4f  D4mf  G4mf B4mf D5f  -",
                "C5f  A4mf C5f  A4mf  C5mf A4mf F#4mf D4p",
            }
        };

        //Plain Melody : for an enemy without a song yet, one gentle line so the band still plays
        //in tune when the instrument files exist (a C major pentatonic arch, not a real piece)
        public static int[] PlainMelody = { 60, 62, 64, 67, 69, 67, 64, 62 };

        public static SongChart[] All = { OdeToJoy, Twinkle, Moonlight, FifthSymphony, PreludeInC, RondoAllaTurca, NachtMusik };

        //Prepare : read every phrase once, when the game starts
        public static void Prepare()
        {
            for (int i = 0; i < All.Length; i++)
            {
                SongChart song = All[i];
                song.Label = song.Name + "  /  " + song.Composer;
                song.Notes = new int[song.Phrases.Length, BattleRules.BeatsPerRound];
                song.Marks = new Choice[song.Phrases.Length, BattleRules.BeatsPerRound];

                for (int p = 0; p < song.Phrases.Length; p++)
                {
                    string[] words = song.Phrases[p].Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    for (int b = 0; b < BattleRules.BeatsPerRound && b < words.Length; b++)
                    {
                        song.Notes[p, b] = NoteOf(words[b]);
                        song.Marks[p, b] = MarkOf(words[b]);
                    }
                }
            }
        }

        //Note Of : "E4p" -> 64. The letter, an optional # or b, then the octave digit.
        //MIDI numbers count semitones, C4 = 60, and every octave is 12 more.
        private static int NoteOf(string word)
        {
            if (word == "-" || word.Length < 2) return 0;
            int[] fromC = { 9, 11, 0, 2, 4, 5, 7 };          // A B C D E F G, semitones above C
            int note = fromC[word[0] - 'A'];
            int at = 1;
            if (word[at] == '#') { note++; at++; }
            else if (word[at] == 'b') { note--; at++; }
            int octave = word[at] - '0';
            return 12 * (octave + 1) + note;
        }

        //Mark Of : the loudness at the end of a word, f, mf or p
        private static Choice MarkOf(string word)
        {
            if (word.EndsWith("mf")) return Choice.Normal;
            if (word.EndsWith("f")) return Choice.Boost;
            if (word.EndsWith("p")) return Choice.Ease;
            return Choice.Normal;
        }
    }
}
