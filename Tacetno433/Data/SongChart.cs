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

        //Plain Melody : for an enemy without a song yet, one gentle line so the band still plays
        //in tune when the instrument files exist (a C major pentatonic arch, not a real piece)
        public static int[] PlainMelody = { 60, 62, 64, 67, 69, 67, 64, 62 };

        public static SongChart[] All = { OdeToJoy };

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
