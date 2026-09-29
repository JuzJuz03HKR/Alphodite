using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;
using Tacetno433.Core;

namespace Tacetno433.Audio
{
    //Sfx : every short sound the game can make.
    //KEEP THIS ORDER IN STEP WITH SoundBank.SfxFiles below.
    public enum Sfx
    {
        UiMove,         // moving between menu items or cards
        UiConfirm,      // pressing a button
        UiBack,         // backing out of a page
        UiDenied,       // trying something that is not allowed
        PathChosen,     // picking a route card
        SeatPickUp,     // lifting a musician off the stage or roster
        SeatDrop,       // placing a musician into a seat
        SeatRemove,     // sending a musician back to the bench
        NoteOn,         // ticking a beat on the score
        NoteOff,        // clearing a beat on the score
        RoundStart,     // the round banner
        BeatTick,       // the needle moving onto a new beat
        QteBoost,       // a big baton stroke (BOOST)
        QteNormal,      // a middle stroke (PLAY)
        QteEase,        // a small stroke (EASE)
        QtePerfect,     // a stroke right on the ring
        QteMiss,        // a stroke badly off the ring, or the wrong way
        QteHesitate,    // the beat went by with no stroke
        ClashWin,       // our sound beat theirs on this beat
        ClashLose,      // theirs beat ours
        ClashEven,      // nobody moved
        EnemyBoost,     // TACET revealed a boost
        EnemyEase,      // TACET revealed an ease
        StaminaEmpty,   // the band ran out of breath
        RestRecover,    // a silent beat gave stamina back
        Victory,        // battle won
        Defeat,         // battle lost
        Recruit,        // a musician joins
        ComboUp,        // the combo grew
        ComboBreak,     // a combo of two or more ended
        MotifGet,       // a motif was taken
        Buy,            // something bought in the shop
        PageTurn,       // flipping a page in the guide or opening an event
        CheckPass,      // an event check succeeded
        CheckFail,      // an event check failed
        Rehearse,       // a musician got stronger
        RunComplete     // the last boss fell
    }

    //Music : every looping track. None means silence.
    //KEEP THIS ORDER IN STEP WITH SoundBank.MusicFiles below.
    public enum Music
    {
        None,
        Title,
        Gallery,
        Route,
        Battle,
        Boss,
        Victory,
        Defeat,
        Shop,
        Event,
        Rest,
        Ending
    }

    //SoundBank : one place that knows about every sound in the game.
    //
    //HOW TO ADD A SOUND
    //  1. put the file in Content/Audio/Sfx  (wav)  or  Content/Audio/Music  (ogg or mp3)
    //  2. add it in the MGCB editor so it gets built
    //  3. make sure the name below matches the file name, without the extension
    //
    //Anything that is missing is skipped quietly, so the game already runs today with no
    //audio at all, and each sound switches on by itself the moment its file exists.
    //
    //PHRASE NOTES : the duel has no long song. Its melody is played ONE NOTE PER BEAT, so the
    //music only happens when the player conducts (and stops when they miss one):
    //   Audio/Phrase/answer_1 ... answer_8   the band's answer, note 1 on beat 1 and so on
    //   Audio/Phrase/call_1   ... call_8     TACET's call
    //Fewer files are fine: with 4 notes the melody simply repeats every bar.
    //
    //INSTRUMENTS (round 15) : the band plays the real song TACET's notes come from (Data/SongChart.cs),
    //each musician on their own instrument. One recorded note per instrument is enough, the game
    //plays it at every pitch of the song :
    //   Audio/Instruments/violin  flute  timpani  soduang  pinai  ranatek  cello  horn  bassdrum
    //   (the names are Musician.Sample). Record each on C : middle C (C4) for most, C3 for the
    //   cello and the timpani (Musician.SampleNote). The bass drum is one hit, it has no pitch.
    //   Audio/Instruments/tacet  (optional) TACET's own voice for its call, also on C4.
    //When an instrument file is there, it plays instead of the phrase files above.
    public static class SoundBank
    {
        //Sound Files : paths inside the Content folder, no extension
        public static string[] SfxFiles =
        {
            "Audio/Sfx/ui_move",
            "Audio/Sfx/ui_confirm",
            "Audio/Sfx/ui_back",
            "Audio/Sfx/ui_denied",
            "Audio/Sfx/path_chosen",
            "Audio/Sfx/seat_pickup",
            "Audio/Sfx/seat_drop",
            "Audio/Sfx/seat_remove",
            "Audio/Sfx/note_on",
            "Audio/Sfx/note_off",
            "Audio/Sfx/round_start",
            "Audio/Sfx/beat_tick",
            "Audio/Sfx/qte_boost",
            "Audio/Sfx/qte_normal",
            "Audio/Sfx/qte_ease",
            "Audio/Sfx/qte_perfect",
            "Audio/Sfx/qte_miss",
            "Audio/Sfx/qte_hesitate",
            "Audio/Sfx/clash_win",
            "Audio/Sfx/clash_lose",
            "Audio/Sfx/clash_even",
            "Audio/Sfx/enemy_boost",
            "Audio/Sfx/enemy_ease",
            "Audio/Sfx/stamina_empty",
            "Audio/Sfx/rest_recover",
            "Audio/Sfx/victory",
            "Audio/Sfx/defeat",
            "Audio/Sfx/recruit",
            "Audio/Sfx/combo_up",
            "Audio/Sfx/combo_break",
            "Audio/Sfx/motif_get",
            "Audio/Sfx/buy",
            "Audio/Sfx/page_turn",
            "Audio/Sfx/check_pass",
            "Audio/Sfx/check_fail",
            "Audio/Sfx/rehearse",
            "Audio/Sfx/run_complete"
        };

        public static string[] MusicFiles =
        {
            "",                         // None
            "Audio/Music/title",
            "Audio/Music/gallery",
            "Audio/Music/route",
            "Audio/Music/battle",
            "Audio/Music/boss",
            "Audio/Music/victory",
            "Audio/Music/defeat",
            "Audio/Music/shop",
            "Audio/Music/event",
            "Audio/Music/rest",
            "Audio/Music/ending"
        };

        //Volume : the numbers live in Core/Settings.cs, because the settings page owns them.
        //Everything played here is multiplied by the master fader as well.
        public static float MusicLevel { get { return Settings.Music * Settings.Master; } }

        //Load Report : shown on the F3 overlay so you can see what got picked up
        public static int LoadedSfx;
        public static int LoadedMusic;

        private static SoundEffect[] sfx;
        private static Song[] songs;
        private static Music playing = Music.None;

        //Phrase Notes : see the note at the top
        public const int PhraseLength = 8;
        private static SoundEffect[] answerNotes = new SoundEffect[PhraseLength];
        private static SoundEffect[] callNotes = new SoundEffect[PhraseLength];
        private static int answerCount;
        private static int callCount;
        public static int LoadedPhrase;

        //Instrument Notes : see the note at the top, one per musician in MusicianList order
        private static SoundEffect[] instrumentNotes = new SoundEffect[0];
        private static SoundEffect tacetNote;
        public static int LoadedInstruments;

        //Sound Load : called once from LoadContent
        public static void Load(ContentManager content)
        {
            sfx = new SoundEffect[SfxFiles.Length];
            songs = new Song[MusicFiles.Length];
            LoadedSfx = 0;
            LoadedMusic = 0;

            for (int i = 0; i < SfxFiles.Length; i++)
            {
                if (!Exists(content, SfxFiles[i])) continue;

                //Safe Load : a broken file should never stop the game from starting
                try
                {
                    sfx[i] = content.Load<SoundEffect>(SfxFiles[i]);
                    LoadedSfx++;
                }
                catch (Exception) { sfx[i] = null; }
            }

            for (int i = 0; i < MusicFiles.Length; i++)
            {
                if (MusicFiles[i].Length == 0 || !Exists(content, MusicFiles[i])) continue;

                try
                {
                    songs[i] = content.Load<Song>(MusicFiles[i]);
                    LoadedMusic++;
                }
                catch (Exception) { songs[i] = null; }
            }

            answerCount = LoadPhrase(content, "Audio/Phrase/answer_", answerNotes);
            callCount = LoadPhrase(content, "Audio/Phrase/call_", callNotes);
            LoadedPhrase = answerCount + callCount;
            LoadInstruments(content);

            MediaPlayer.IsRepeating = true;
            MediaPlayer.Volume = MusicLevel;
        }

        //Phrase Load : note files numbered from 1, stopping at the first one that is missing
        private static int LoadPhrase(ContentManager content, string prefix, SoundEffect[] into)
        {
            int count = 0;
            for (int n = 0; n < into.Length; n++)
            {
                string name = prefix + (n + 1);
                if (!Exists(content, name)) break;
                try
                {
                    into[n] = content.Load<SoundEffect>(name);
                    count++;
                }
                catch (Exception) { break; }
            }
            return count;
        }

        //Instruments Load : one file per musician, and TACET's own, each skipped quietly if missing
        private static void LoadInstruments(ContentManager content)
        {
            Tacetno433.Data.Musician[] all = Tacetno433.Data.MusicianList.All;
            instrumentNotes = new SoundEffect[all.Length];
            LoadedInstruments = 0;
            for (int i = 0; i < all.Length; i++)
            {
                instrumentNotes[i] = TryLoad(content, "Audio/Instruments/" + all[i].Sample);
                if (instrumentNotes[i] != null) LoadedInstruments++;
            }
            tacetNote = TryLoad(content, "Audio/Instruments/tacet");
        }

        //Try Load : a sound effect if its file exists and reads, otherwise null
        private static SoundEffect TryLoad(ContentManager content, string name)
        {
            if (!Exists(content, name)) return null;
            try { return content.Load<SoundEffect>(name); }
            catch (Exception) { return null; }
        }

        //Play Instrument : one musician's instrument playing this note of the song (a MIDI number,
        //60 is middle C). detune bends it a little, the duel uses it to make a MISS sound wrong.
        //Returns false when the file is missing, so the caller can fall back.
        public static bool PlayInstrument(int musician, int sampleNote, int midi, float volume, float detune)
        {
            if (musician < 0 || musician >= instrumentNotes.Length || instrumentNotes[musician] == null) return false;
            instrumentNotes[musician].Play(volume * Settings.Sfx * Settings.Master, PitchFor(sampleNote, midi, detune), 0f);
            return true;
        }

        //Play Tacet : TACET's call on its own voice, when the file exists
        public static bool PlayTacet(int midi, float volume)
        {
            if (tacetNote == null) return false;
            tacetNote.Play(volume * Settings.Sfx * Settings.Master, PitchFor(60, midi, 0f), 0f);
            return true;
        }

        //ADVANCED PART : one recorded note played at another pitch. SoundEffect.Play takes a pitch
        //from -1 (one octave down) to +1 (one octave up), so a note n semitones above the recording
        //is pitch n / 12. A note further away than an octave is moved by whole octaves (12 semitones)
        //until it fits, so the cello plays the tune an octave lower instead of not at all.
        //A drum (sampleNote 0) always plays as recorded.
        private static float PitchFor(int sampleNote, int midi, float detune)
        {
            if (sampleNote <= 0 || midi <= 0) return MathHelper.Clamp(detune, -1f, 1f);
            int shift = midi - sampleNote;
            while (shift > 12) shift -= 12;
            while (shift < -12) shift += 12;
            return MathHelper.Clamp(shift / 12f + detune, -1f, 1f);
        }

        //Play Answer : the band's note for this beat of the round. Returns false when there are
        //no phrase files yet, so the caller can fall back to a plain sound effect.
        //pitch 0 is in tune. The duel detunes a MISS a little, so a mistake sounds wrong.
        public static bool PlayAnswer(int beat, float volume, float pitch)
        {
            if (answerCount == 0) return false;
            answerNotes[beat % answerCount].Play(volume * Settings.Sfx * Settings.Master, pitch, 0f);
            return true;
        }

        //Play Call : TACET's note for this beat of the round
        public static bool PlayCall(int beat, float volume, float pitch)
        {
            if (callCount == 0) return false;
            callNotes[beat % callCount].Play(volume * Settings.Sfx * Settings.Master, pitch, 0f);
            return true;
        }

        //File Check : built content lives next to the game as .xnb files
        private static bool Exists(ContentManager content, string name)
        {
            string path = Path.Combine(AppContext.BaseDirectory, content.RootDirectory, name + ".xnb");
            return File.Exists(path);
        }

        //Play Sound : fire and forget
        public static void Play(Sfx id)
        {
            Play(id, 1f, 0f);
        }

        //Play Sound Tuned : volume 0..1, pitch -1..1 (use a little pitch to stop repeats sounding flat)
        public static void Play(Sfx id, float volume, float pitch)
        {
            if (sfx == null) return;
            SoundEffect effect = sfx[(int)id];
            if (effect == null) return;

            effect.Play(volume * Settings.Sfx * Settings.Master, pitch, 0f);
        }

        //Play Music : switching to the track already playing does nothing,
        //so every page can simply ask for its track when it opens
        public static void PlayMusic(Music id)
        {
            if (songs == null) return;
            if (id == playing) return;

            playing = id;
            Song song = songs[(int)id];

            if (song == null)
            {
                MediaPlayer.Stop();
                return;
            }

            MediaPlayer.Volume = MusicLevel;
            MediaPlayer.Play(song);
        }

        //Volume Changed : called by the settings page while a volume slider is moving
        public static void ApplyVolume()
        {
            MediaPlayer.Volume = MusicLevel;
        }

        public static void StopMusic()
        {
            playing = Music.None;
            MediaPlayer.Stop();
        }
    }
}
