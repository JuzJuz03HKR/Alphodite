TACET AUDIO FOLDER  (Beta1, updated 1 Nov 2026)
===============================================

The full list of every sound the game needs is docs/ASSET_LIST.md (the sound part, S0 to S5),
with a sheet to tick in docs/TACET_Asset_List.xlsx (tab Sound). It says which sounds the game
already plays, which ones the battle still needs (they are not hooked in the code yet), and which
old ones are not used any more.

Until a file exists the game simply stays silent, so sounds can be added one at a time and each
one switches on by itself. There are no stand-in sounds.

HOW TO ADD ONE
  1. Put the file in the right folder with EXACTLY the name in the list (lower case)
       Sfx   ->  Content/Audio/Sfx/<name>.wav
       Music ->  Content/Audio/Music/<name>.ogg   (mp3 also works)
  2. Open Content.mgcb in the MGCB Editor, Add > Existing Item, pick the file.
     Sound effects use the "Sound Effect" processor, music uses "Song".
  3. Build. Press F3 in game : the overlay shows how many SFX and MUSIC files loaded.

The names the code plays today live in Audio/SoundBank.cs (SfxFiles and MusicFiles).

FILES
  Sound effects : WAV, 44.1 kHz, 16 bit, mono is fine. The sound starts right at the start of the
  file. Hits and notes stay short, because the notes of a skill come close together.
  Music : the game loops the whole file, so the end must run straight back into the start
  (no fade out). It does not need to follow the notes : the fights are no longer tied to a song.

THE BATTLE
  The Beta1 battle plays almost no sound yet : the buttons and the fight music (combo_up only
  in the practice fight, when a lesson is done). The hits, the notes, the parries, the enemies
  and the bosses are listed in ASSET_LIST.md S3 and get hooked in during the sound work.
  The user's direction : every weapon is an instrument, so a hit sounds like the instrument
  mixed with the weapon.

NOT USED BY BETA1
  Instruments/ (nine notes from SoundLibrary/, the song system of Alpha6, SoundBank.InstrumentsOn
  is false) and Phrase/ (answer_1 to answer_8, call_1 to call_8) belong to the old duel.
  So do note_off, qte_boost, qte_ease, clash_win, clash_lose, clash_even, enemy_boost,
  enemy_ease, stamina_empty and combo_break.

BEFORE THE GAME IS SOLD
  Every sound and every piece of music needs a licence for commercial use. Where the samples in
  SoundLibrary/ came from, and under which licence, is not written down anywhere in the repo :
  ask the team before shipping them.
