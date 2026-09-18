using System;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

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
        QteBoost,       // player pressed F
        QteNormal,      // player pressed G
        QteEase,        // player pressed H
        QtePerfect,     // pressed exactly on the ring
        QteMiss,        // pressed badly off the ring
        QteHesitate,    // never pressed at all
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
        Prep,
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
            "Audio/Music/prep",
            "Audio/Music/battle",
            "Audio/Music/boss",
            "Audio/Music/victory",
            "Audio/Music/defeat",
            "Audio/Music/shop",
            "Audio/Music/event",
            "Audio/Music/rest",
            "Audio/Music/ending"
        };

        //Volume : 0 to 1
        public static float SfxVolume = 0.8f;
        public static float MusicVolume = 0.55f;

        //Load Report : shown on the F3 overlay so you can see what got picked up
        public static int LoadedSfx;
        public static int LoadedMusic;

        private static SoundEffect[] sfx;
        private static Song[] songs;
        private static Music playing = Music.None;

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

            MediaPlayer.IsRepeating = true;
            MediaPlayer.Volume = MusicVolume;
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

            effect.Play(volume * SfxVolume, pitch, 0f);
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

            MediaPlayer.Volume = MusicVolume;
            MediaPlayer.Play(song);
        }

        public static void StopMusic()
        {
            playing = Music.None;
            MediaPlayer.Stop();
        }
    }
}
