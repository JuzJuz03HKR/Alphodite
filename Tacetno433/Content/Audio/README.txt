TACET AUDIO FOLDER  (Beta1, updated 2 Nov 2026, block E)
=========================================================

Every sound and every track is read straight from these folders when the game starts
(Audio/SoundBank.cs). Nothing goes into Content.mgcb : the project copies the folders next
to the game (Tacetno433.csproj). A missing file is skipped quietly, so each sound switches on by
itself the moment its file is there. Press F3 in game : the overlay counts SFX, MUSIC,
BORROWED and BAND files found.

FOLDERS
  Sfx/       short sounds, WAV 44.1 kHz 16 bit. Free files only (ours, CC0, CC-BY).
  Music/     looping tracks, OGG Vorbis only. Free files only.
  Borrowed/  the team's free picks (TEAM FREE : found on free sound sites, the sites not written
             down) and anything borrowed for the closed playtest (none yet). NEVER COMMITTED :
             git keeps only its README. Same names as Sfx and Music.
             A file here wins over the free one with the same name.
  Instruments/  the old duel's nine notes (Alpha6, not used by Beta1, kept as the user asked).

THE NAMES
  The names the code plays are in Audio/SoundBank.cs (SfxFiles, MusicFiles) and each band
  member's own note is band_<Sample>.wav (band_violin band_flute band_timpani band_cello
  band_horn). The full list with what plays where is docs/ASSET_LIST.md (S1 to S3).
  Many moments share one file on purpose (the user 13.133 : reuse what can be reused).

HOW A BLOW SOUNDS (the user 13.131)
  Two layers : the weapon (weapon_cut for strings, weapon_shot for winds, weapon_smash for
  drums), then the striker's own instrument, light, the same note every time. Never a tune.

WHERE THE FILES COME FROM
  docs/CREDITS.md names every file, who made it, its licence and its label : OURS, FREE,
  TEAM FREE or BORROWED. The free files are made again with docs/audio/make_sounds.py.
  The CREDITS page of the game (C on the title page) is Data/Credits.cs.
  SoundLibrary/ is VSCO 2 Community Edition by Versilian Studios, CC0 (checked 2 Nov).
