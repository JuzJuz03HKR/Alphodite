using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;
using Tacetno433.Core;
using Tacetno433.Data;

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
        RoundStart,     // the round banner (Beta1 : the band's turn begins, the baton taps the stand)
        BeatTick,       // the needle moving onto a new beat
        QteBoost,       // a big baton stroke (BOOST)
        QteNormal,      // a middle stroke (PLAY) (Beta1 : a note swiped GOOD)
        QteEase,        // a small stroke (EASE)
        QtePerfect,     // a stroke right on the ring (Beta1 : the bright ring over a PERFECT)
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
        RunComplete,    // the last boss fell

        //BLOCK E (Beta1 2 Nov, docs/BETA1_DIRECTION.md 47) : the fight's own sounds. A blow is TWO LAYERS : the weapon,
        //then the striker's own instrument, light (the user 13.131). Many moments share one file (the user 13.133 :
        //reuse what can be reused, make apart only what has to be told apart), see SfxFiles.
        WeaponCut,      // a blade or a bow lands (strings : PAGANINI, DUPORT)
        WeaponShot,     // a shot lands (winds : QUANTZ, PUNTO)
        WeaponSmash,    // a hammer lands (drums : PFUNDT), and TACET's blows on the band
        SkillHeal,      // a HEAL lands (TACET's own is the same sound, lower)
        SkillGuard,     // GUARD, SHIELD, TACET's ARMOR UP
        SkillBoost,     // VOLUME, FOCUS, FORTE, a call that lifts the band
        ParryPerfect,   // a note of TACET's met PERFECT
        ParryGood,      // met, not quite (and a blow ARMOR takes)
        EnemySwing,     // TACET's note on its way, loudest right on the ring : the parry cue (Expedition 33, docs/research/E_music.md)
        BandHurt,       // a musician takes a blow
        BandDown,       // a musician falls
        EnemySilenced,  // a shape of TACET breaks into the dark
        EnemyCharge,    // CHARGE : TACET winds up its big move
        EnemyBroken,    // BROKEN : the wind up breaks (and a BIG ATTACK blocked whole)
        Stunned,        // STUNNED
        BurnTick,       // BURN hurts
        FinaleReady,    // the FINALE gauge is full
        FinaleStorm,    // THE UNHEARING's FINALE (storm)
        FinalePrism,    // FANNY AND ???'s (crystal)
        FinaleHoly,     // THE METRONOME's (holy light)
        BossEntrance,   // a boss rises
        BossTell,       // THE TELL : its BIG ATTACK comes this turn
        BossStrike,     // THE STRIKE : its BIG ATTACK begins
        BossTurn,       // THE TURN : half its life, the white shell breaks
        BossDown,       // a boss falls
        EnemyTurn,      // TACET's turn begins
        FightStart,     // a fight begins
        HoldLoop,       // a HOLD note while it is held (it loops)
        RollTap         // one shake of a ROLL
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
        Ending,
        Elite,          // BLOCK E : an ELITE's fight
        BossTurn        // BLOCK E : a boss after THE TURN, its second half (Honkai: Star Rail and Limbus change the music for a boss's last phase)
    }

    //SoundBank : one place that knows about every sound in the game.
    //
    //HOW TO ADD A SOUND (block E, 2 Nov : read straight from the folders, no MGCB)
    //  1. put the file in Content/Audio/Sfx  (wav, 44.1 kHz 16 bit)  or  Content/Audio/Music  (ogg)
    //  2. the name below is the file name without its extension. Nothing to add in the MGCB editor : the project copies
    //     every file of these folders next to the game (Tacetno433.csproj) and the game reads them when it starts.
    //
    //BORROWED (the user 13.125 13.132) : sounds and music borrowed from other games or of unknown origin may never go into the
    //public repo. They live in Content/Audio/Borrowed (git keeps only its README) with the same names. A file there wins over
    //the one in Sfx or Music, so the playtest build gets the borrowed set and anyone else still gets the free one.
    //Every file is credited in docs/CREDITS.md and on the CREDITS page.
    //
    //Anything that is missing is skipped quietly, so the game runs with no audio at all, and each sound switches on by
    //itself the moment its file exists. A file that cannot be read (wrong format) is skipped the same way.
    //
    //PHRASE NOTES (Alpha6, the old duel) : the duel's melody is played ONE NOTE PER BEAT, from built content
    //(Audio/Phrase/answer_1 ... answer_8 and call_1 ... call_8). The Beta1 fight does not use them (the user 13.127 13.128 :
    //the music is a game's plain music, never built from notes played on the hits).
    //
    //INSTRUMENTS (round 15, the old duel) : one recorded note per instrument from built content, played at every pitch of the
    //song (Audio/Instruments). SOUND OFF since 29 Sep. The Beta1 fight's own instrument layer is BAND NOTES below.
    public static class SoundBank
    {
        //Sound Files : file names in Content/Audio/Sfx (or Borrowed), no extension. Several moments may share one file.
        public static string[] SfxFiles =
        {
            "ui_move",
            "ui_confirm",
            "ui_back",
            "ui_denied",
            "path_chosen",
            "seat_pickup",
            "seat_drop",
            "seat_remove",
            "note_on",
            "note_off",
            "round_start",
            "beat_tick",
            "qte_boost",
            "qte_normal",
            "qte_ease",
            "qte_perfect",
            "qte_miss",
            "qte_miss",                 // QteHesitate : the same thud, played softer
            "clash_win",
            "clash_lose",
            "clash_even",
            "enemy_boost",
            "enemy_ease",
            "stamina_empty",
            "rest_recover",
            "victory",
            "defeat",
            "recruit",
            "combo_up",
            "combo_break",
            "motif_get",
            "buy",
            "page_turn",
            "check_pass",
            "check_fail",
            "rehearse",
            "run_complete",

            //BLOCK E
            "weapon_cut",
            "weapon_shot",
            "weapon_smash",
            "skill_heal",
            "skill_guard",
            "skill_boost",
            "parry_perfect",
            "parry_good",
            "enemy_swing",
            "band_hurt",
            "band_down",
            "enemy_silenced",
            "enemy_charge",
            "enemy_broken",
            "stunned",
            "burn_tick",
            "finale_ready",
            "finale_storm",
            "finale_prism",
            "finale_holy",
            "boss_entrance",
            "boss_tell",
            "boss_strike",
            "boss_turn",
            "boss_down",
            "enemy_turn",
            "fight_start",
            "hold_loop",
            "roll_tap"
        };

        //Music Files : file names in Content/Audio/Music (or Borrowed), no extension, ogg only (MediaPlayer reads Ogg Vorbis)
        public static string[] MusicFiles =
        {
            "",                         // None
            "title",
            "gallery",
            "route",
            "battle",
            "boss",
            "victory",
            "defeat",
            "shop",
            "event",
            "rest",
            "ending",
            "elite",
            "boss_turn"
        };

        //Volume : the numbers live in Core/Settings.cs, because the settings page owns them.
        //Everything played here is multiplied by the master fader as well.
        public static float MusicLevel { get { return Settings.Music * Settings.Master; } }

        //Load Report : shown on the F3 overlay so you can see what got picked up
        public static int LoadedSfx;
        public static int LoadedMusic;
        public static int LoadedBorrowed;          // files of the two above that came from Content/Audio/Borrowed
        public static int LoadedBand;              // BAND NOTES found

        private static SoundEffect[] sfx;
        private static float[] lastPlayed;         // REPEAT GAP : when each sound last started (SoundBank clock)
        private static float clock;

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

        //BAND NOTES (block E) : each musician's instrument, one short note, the light second layer of their blows and their
        //sound when they join the band. Content/Audio/Sfx/band_<Sample>.wav, in MusicianList order. Always the same note :
        //never a tune (the user 13.131).
        private static SoundEffect[] bandNotes = new SoundEffect[0];

        //Audio Folder : where the files are, next to the game
        private static string AudioFolder(string sub)
        {
            return Path.Combine(AppContext.BaseDirectory, "Content", "Audio", sub);
        }

        //Sound Load : called once from LoadContent
        public static void Load(ContentManager content)
        {
            Dictionary<string, SoundEffect> read = new Dictionary<string, SoundEffect>();   // one copy of a file several moments share
            sfx = new SoundEffect[SfxFiles.Length];
            lastPlayed = new float[SfxFiles.Length];
            for (int i = 0; i < lastPlayed.Length; i++) lastPlayed[i] = -10f;
            LoadedSfx = 0;
            LoadedBorrowed = 0;

            for (int i = 0; i < SfxFiles.Length; i++)
            {
                sfx[i] = ReadSound(SfxFiles[i], read);
                if (sfx[i] != null) LoadedSfx++;
            }

            Musician[] all = MusicianList.All;
            bandNotes = new SoundEffect[all.Length];
            LoadedBand = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].Sample.Length == 0) continue;
                bandNotes[i] = ReadSound("band_" + all[i].Sample, read);
                if (bandNotes[i] != null) LoadedBand++;
            }

            FindMusic();

            answerCount = LoadPhrase(content, "Audio/Phrase/answer_", answerNotes);
            callCount = LoadPhrase(content, "Audio/Phrase/call_", callNotes);
            LoadedPhrase = answerCount + callCount;
            LoadInstruments(content);

            MediaPlayer.IsRepeating = true;
            MediaPlayer.Volume = 0f;
        }

        //Read Sound : Borrowed first, then Sfx. Null when neither has it or it cannot be read.
        private static SoundEffect ReadSound(string name, Dictionary<string, SoundEffect> read)
        {
            SoundEffect known;
            if (read.TryGetValue(name, out known)) return known;

            SoundEffect effect = null;
            string borrowed = Path.Combine(AudioFolder("Borrowed"), name + ".wav");
            string own = Path.Combine(AudioFolder("Sfx"), name + ".wav");
            if (File.Exists(borrowed)) effect = TryFile(borrowed);
            if (effect != null) LoadedBorrowed++;
            else if (File.Exists(own)) effect = TryFile(own);
            read[name] = effect;
            return effect;
        }

        //Safe Load : a broken file, or no sound card at all, should never stop the game from starting
        private static SoundEffect TryFile(string path)
        {
            try { return SoundEffect.FromFile(path); }
            catch (Exception) { return null; }
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

        //SOUND OFF (29 Sep) : the player turned the band's instruments off for the presentation,
        //the songs did not sound right yet. true loads them again, nothing else needs to change.
        public static bool InstrumentsOn = false;

        //Instruments Load : one file per musician, and TACET's own, each skipped quietly if missing
        private static void LoadInstruments(ContentManager content)
        {
            Musician[] all = MusicianList.All;
            instrumentNotes = new SoundEffect[all.Length];
            LoadedInstruments = 0;
            if (!InstrumentsOn) return;                                           // SOUND OFF
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
            instrumentNotes[musician].Play(Level(volume), PitchFor(sampleNote, midi, detune), 0f);
            return true;
        }

        //Play Tacet : TACET's call on its own voice, when the file exists
        public static bool PlayTacet(int midi, float volume)
        {
            if (tacetNote == null) return false;
            tacetNote.Play(Level(volume), PitchFor(60, midi, 0f), 0f);
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
            answerNotes[beat % answerCount].Play(Level(volume), pitch, 0f);
            return true;
        }

        //Play Call : TACET's note for this beat of the round
        public static bool PlayCall(int beat, float volume, float pitch)
        {
            if (callCount == 0) return false;
            callNotes[beat % callCount].Play(Level(volume), pitch, 0f);
            return true;
        }

        //File Check : built content lives next to the game as .xnb files (the old duel's phrases and instruments)
        private static bool Exists(ContentManager content, string name)
        {
            string path = Path.Combine(AppContext.BaseDirectory, content.RootDirectory, name + ".xnb");
            return File.Exists(path);
        }

        //Level : a sound's volume through the SFX and master faders, never above full
        private static float Level(float volume)
        {
            return MathHelper.Clamp(volume * Settings.Sfx * Settings.Master, 0f, 1f);
        }

        //Has : the file of this sound was found (a caller can fall back to another)
        public static bool Has(Sfx id)
        {
            return sfx != null && sfx[(int)id] != null;
        }

        //Play Sound : fire and forget
        public static void Play(Sfx id)
        {
            Play(id, 1f, 0f);
        }

        //Play Sound Tuned : volume 0..1, pitch -1..1 (use a little pitch to stop repeats sounding flat).
        //REPEAT GAP : the same sound twice in one moment (three enemies hit by one blow) plays once, louder is not better.
        public static void Play(Sfx id, float volume, float pitch)
        {
            if (sfx == null) return;
            SoundEffect effect = sfx[(int)id];
            if (effect == null) return;
            if (clock - lastPlayed[(int)id] < BattleRules.SoundRepeatGap) return;
            lastPlayed[(int)id] = clock;

            //Safe Play : too many sounds at once (or no sound card) is never worth a crash
            try { effect.Play(Level(volume), MathHelper.Clamp(pitch, -1f, 1f), 0f); }
            catch (Exception) { }
        }

        //Play Band : BAND NOTES, this musician's own instrument (block E). False when it has no file.
        public static bool PlayBand(Musician m, float volume)
        {
            if (m == null || bandNotes.Length == 0) return false;
            int index = Array.IndexOf(MusicianList.All, m);
            if (index < 0 || bandNotes[index] == null) return false;
            try { bandNotes[index].Play(Level(volume), 0f, 0f); }
            catch (Exception) { return false; }
            return true;
        }

        //======================================================================================
        //LOOP : one sound that keeps going until it is stopped (a HOLD note while it is held)
        //======================================================================================
        private static SoundEffectInstance loop;
        private static Sfx loopId;

        public static void StartLoop(Sfx id, float volume)
        {
            StopLoop();
            if (sfx == null || sfx[(int)id] == null) return;
            try
            {
                loop = sfx[(int)id].CreateInstance();
                loop.IsLooped = true;
                loop.Volume = Level(volume);
                loop.Play();
                loopId = id;
            }
            catch (Exception) { loop = null; }
        }

        public static void StopLoop()
        {
            if (loop == null) return;
            try
            {
                loop.Stop();
                loop.Dispose();
            }
            catch (Exception) { }
            loop = null;
        }

        //======================================================================================
        //MUSIC : one track at a time (MediaPlayer), faded out and in when the page asks for another, pushed down for a
        //moment while a FINALE or a boss's moment speaks (DUCK). A track's Song is made only when it starts playing, and
        //let go when it stops, so the songs that are not playing hold no file open.
        //======================================================================================
        private static string[] musicPaths = new string[0];
        private static Music wanted = Music.None;      // the track the page asked for
        private static Music playing = Music.None;     // the track in the player now
        private static Song song;
        private static float fade;                     // 0..1, the current track fading in or out
        private static float duck = 1f;                // DUCK : the music pushed down for a moment
        private static float duckTo = 1f;
        private static float duckHold;
        private static float lastVolume = -1f;

        //Music Now : the track playing (or fading in) now
        public static Music MusicNow { get { return playing; } }

        //Find Music : the file of every track, Borrowed first, then Music (ogg only)
        private static void FindMusic()
        {
            musicPaths = new string[MusicFiles.Length];
            LoadedMusic = 0;
            for (int i = 0; i < MusicFiles.Length; i++)
            {
                if (MusicFiles[i].Length == 0) continue;
                string borrowed = Path.Combine(AudioFolder("Borrowed"), MusicFiles[i] + ".ogg");
                string own = Path.Combine(AudioFolder("Music"), MusicFiles[i] + ".ogg");
                if (File.Exists(borrowed))
                {
                    musicPaths[i] = borrowed;
                    LoadedBorrowed++;
                }
                else if (File.Exists(own)) musicPaths[i] = own;
                if (musicPaths[i] != null) LoadedMusic++;
            }
        }

        //Play Music : asking for the track already playing does nothing, so every page can simply ask for its track when
        //it opens. A new track waits for the old one to fade out (Update).
        public static void PlayMusic(Music id)
        {
            wanted = id;
        }

        public static void StopMusic()
        {
            wanted = Music.None;
        }

        //Duck Music : DUCK, the music down to this share for this long (seconds), then back up
        public static void DuckMusic(float level, float seconds)
        {
            duckTo = Math.Min(duckTo, MathHelper.Clamp(level, 0f, 1f));
            duckHold = Math.Max(duckHold, seconds);
        }

        //Update : called every frame from TacetGame, the fades and the duck
        public static void Update(float dt)
        {
            clock += dt;
            if (musicPaths.Length == 0) return;

            if (wanted != playing)
            {
                if (song != null && fade > 0f) fade = Math.Max(0f, fade - dt / BattleRules.SoundMusicFadeOut);
                if (song == null || fade <= 0f) Switch();
            }
            else if (song != null && fade < 1f) fade = Math.Min(1f, fade + dt / BattleRules.SoundMusicFadeIn);

            if (duckHold > 0f) duckHold -= dt;
            else duckTo = 1f;
            float speed = duckTo < duck ? BattleRules.SoundDuckDown : BattleRules.SoundDuckUp;
            duck += (duckTo - duck) * Math.Min(1f, dt * speed);
            ApplyVolume();
        }

        //Switch : the old track is let go and the wanted one starts from silence
        private static void Switch()
        {
            StopSong();
            playing = wanted;
            fade = 0f;
            string path = musicPaths[(int)playing];
            if (path == null) return;
            try
            {
                song = Song.FromUri(MusicFiles[(int)playing], new Uri(path));
                MediaPlayer.IsRepeating = true;
                MediaPlayer.Volume = 0f;
                lastVolume = 0f;
                MediaPlayer.Play(song);
            }
            catch (Exception)
            {
                song = null;
                musicPaths[(int)playing] = null;                                  // a file that cannot play is not tried again
            }
        }

        private static void StopSong()
        {
            if (song == null) return;
            try
            {
                MediaPlayer.Stop();
                song.Dispose();
            }
            catch (Exception) { }
            song = null;
        }

        //Track Gain : a fight's music sits lower, so the swipe and parry sounds stay clear on top (Worldless keeps its
        //plain fights quiet for the same reason, docs/research/E_music.md)
        private static float TrackGain(Music id)
        {
            if (id == Music.Battle || id == Music.Elite) return BattleRules.SoundFightMusic;
            return 1f;
        }

        //Volume Changed : called by the settings page while a volume slider is moving, and by Update
        public static void ApplyVolume()
        {
            if (song == null) return;
            float volume = MathHelper.Clamp(MusicLevel * TrackGain(playing) * fade * duck, 0f, 1f);
            if (Math.Abs(volume - lastVolume) < 0.002f) return;
            lastVolume = volume;
            try { MediaPlayer.Volume = volume; }
            catch (Exception) { }
        }
    }
}
