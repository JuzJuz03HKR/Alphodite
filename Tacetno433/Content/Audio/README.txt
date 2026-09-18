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
  qte_boost       pressing F
  qte_normal      pressing G
  qte_ease        pressing H
  qte_perfect     pressing right on the mark
  qte_miss        pressing badly off the mark
  qte_hesitate    the ring ran out with no press
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
  prep            stage and score pages
  battle          normal and elite duels
  boss            boss duels
  victory         win result page
  defeat          loss result page
  shop            shop page
  event           event page
  rest            rest page
  ending          curtain call page at the end of a run
