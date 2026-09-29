TACET AUDIO FOLDER
==================

The game already calls every sound below. Until a file exists it simply stays silent,
so you can add sounds one at a time and each one switches on by itself.

HOW TO ADD ONE
  1. Put the file in the right folder with EXACTLY the name listed (lower case)
       Sfx   ->  Content/Audio/Sfx/<name>.wav
       Music ->  Content/Audio/Music/<name>.ogg   (mp3 also works)
  2. Open Content.mgcb in the MGCB Editor, Add > Existing Item, pick the file.
     Sound effects use the "Sound Effect" processor, music uses "Song".
  3. Build. Press F3 in game: the overlay shows how many SFX and MUSIC files loaded.

The list of names lives in  Audio/SoundBank.cs  (SfxFiles and MusicFiles).
Change a name there if you would rather name your file differently.

INSTRUMENTS (round 15, 29 Sep)  :  the band plays the real song TACET's notes come from
  Every musician has an instrument, and ONE recorded note of it is enough : the game plays that
  note higher or lower for every note of the song (up to an octave either way, further notes move
  by whole octaves). Everybody on stage plays each note you hit, together.
       Content/Audio/Instruments/violin.wav     ANNA      recorded on C4 (middle C)
       Content/Audio/Instruments/flute.wav      KLARA     C4
       Content/Audio/Instruments/timpani.wav    OTTO      C3
       Content/Audio/Instruments/soduang.wav    MALI      C4
       Content/Audio/Instruments/pinai.wav      CHAI      C4
       Content/Audio/Instruments/ranatek.wav    NUAN      C4
       Content/Audio/Instruments/cello.wav      LUKA      C3
       Content/Audio/Instruments/horn.wav       IRIS      C4
       Content/Audio/Instruments/bassdrum.wav   BORIS     one hit, no pitch
       Content/Audio/Instruments/tacet.wav      (optional) TACET's own voice for its call, C4
  Keep each note 1 to 2 seconds, dry (no reverb), the start of the note right at the start of the
  file, mono is fine. Recorded on another note? Change that musician's SampleNote in
  Data/Musician.cs (a MIDI number : C4 = 60, C3 = 48, A4 = 69).
  Add each file in the MGCB Editor with the "Sound Effect" processor. F3 shows INSTR = how many loaded.
  When instrument files exist they are used instead of the phrase notes below.
  The songs themselves are written in Data/SongChart.cs (notes from the score, never from a recording).

THE DUEL HAS NO LONG SONG
  The melody of a fight is played ONE NOTE PER BEAT, and only when the player conducts.
  Record one short phrase, cut it into notes, and name the notes in order:
       Content/Audio/Phrase/answer_1.wav ... answer_8.wav    the band's answer
       Content/Audio/Phrase/call_1.wav   ... call_8.wav      TACET's call
  Note 1 plays on beat 1 of a round, note 2 on beat 2, and so on. With only 4 notes the
  melody repeats every bar. Keep each note short (under a second) and in the same key.
  The game plays a note louder for a BOOST, softer for an EASE, a little out of tune for a
  MISS, and not at all when the player hesitates. TACET's call is loud for an f and soft for
  a p. Until the notes exist the duel falls back to note_on and beat_tick below.
  F3 in game shows PHRASE = how many notes were found.

SOUND EFFECTS  (Content/Audio/Sfx)
  ui_move         moving between menu items, cards, paintings
  ui_confirm      pressing a button
  ui_back         backing out of a page
  ui_denied       trying something that is not allowed
  path_chosen     picking a route panel
  seat_pickup     lifting a musician on the stage page
  seat_drop       placing a musician into a seat
  seat_remove     sending a musician to the bench
  note_on         ticking a beat on the score page
  note_off        clearing a beat on the score page
  round_start     the ROUND banner in a duel
  beat_tick       the needle moving onto a new beat
  qte_boost       a big baton stroke (BOOST)
  qte_normal      a middle stroke (PLAY)
  qte_ease        a small stroke (EASE)
  qte_perfect     a stroke right on the ring
  qte_miss        a stroke badly off the ring, or the wrong way
  qte_hesitate    the beat went by with no stroke
  clash_win       our sound beat theirs on a beat
  clash_lose      theirs beat ours
  clash_even      nobody moved, or both silent
  enemy_boost     TACET revealed a boost
  enemy_ease      TACET revealed an ease
  stamina_empty   the band ran out of breath
  rest_recover    a silent beat or a rest room gave stamina back
  victory         a fight was won
  defeat          a fight was lost
  recruit         a musician joins
  combo_up        the combo counter grew
  combo_break     a combo of two or more ended
  motif_get       a motif was taken (reward page, shop, event)
  buy             something bought in the shop
  page_turn       flipping a guide page, opening an event
  check_pass      an event check succeeded
  check_fail      an event check failed
  rehearse        a musician got stronger at a rest stop
  run_complete    the last boss of the run fell

MUSIC  (Content/Audio/Music, loops)
  title           title page
  gallery         conductor gallery
  route           route and era pages
  battle          a whole normal or elite fight: the stage page, the score page and every duel
                  round. It never restarts between rounds. Keep it quiet, ambient, with no
                  strong beat of its own, because the melody comes from the baton (above).
  boss            the same for a boss fight
  victory         win result page
  defeat          loss result page
  shop            shop page
  event           event page
  rest            rest page
  ending          curtain call page at the end of a run
