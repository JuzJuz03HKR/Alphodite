# TACET 4'33 : CREDITS (sound and music)

ทุกไฟล์มีป้ายหนึ่งในสี่ : **FREE** (ของคนอื่นที่ใช้ฟรี CC0 หรือ CC-BY อยู่ใน repo) / **TEAM FREE** (เสียงฟรีที่ทีมหามาจากเว็บเสียงฟรีและตรวจแล้วว่าใช้ได้ ไม่อยู่ใน repo) / **BORROWED** (เพลงหรือเสียงของเกมอื่น ใช้แค่ playtest ในงานปิด ไม่อยู่ใน repo ตอนนี้ไม่มี) / **OURS** (ทำเอง ตอนนี้ไม่มี)
รายการทีละไฟล์ (ไฟล์ไหนมาจากเสียงอะไร) : `docs/audio/MANIFEST.md` สร้างจาก `docs/audio/make_sounds.py`
**หน้า CREDITS ในเกม** (หน้า Title มุมซ้ายล่าง หรือกด C) อ่านจาก `Tacetno433/Data/Credits.cs` : เพิ่มเสียง เพลง หรือฟอนต์ใหม่ แก้สามที่ให้ตรงกัน (ไฟล์นี้ `MANIFEST.md` และ `Data/Credits.cs`)

## FREE : music (freesound.org)
- szegvari, CC0 : "Dark Sad Mood Orchestra Strings Piano Choir..." (610532), "Long Time Waiting - Strings..." (610996), "Dark Sad Mood Thriller Drama Orchestra Piano Keys..." (609755), "Old ritual - Fantasy Background Soundscape..." (595853), "Force Dance - Orchestra Strings Brass..." (611098), "Desert fly Orchestra Atmo" (577391), "Sad Alone Orchestra Strings Vocal Choir..." (609647)
- **Joshua Empyre, "Epic Orchestra - LOOP" (freesound 250856), CC-BY 4.0** https://creativecommons.org/licenses/by/4.0/ (levelled, used for boss_turn)

## FREE : sound effects
- **VSCO 2 Community Edition** by Versilian Studios (recorded by Sam Gossner and Simon Dalzell, sample cutting by Elan Hickler / Soundemote; organ by Simon Dalzell of Ivy Audio), CC0 : the musicians' own notes, orchestral hits, gongs, glockenspiel, bells. Also the team's `SoundLibrary/`.
- OpenGameArt.org packs, all CC0 :
  - Kenney : Interface Sounds / 50 RPG sound effects
  - StarNinjas : 20 Sword Sound Effects (Attacks and Clashes)
  - untamedpixels : 150 Combat Sound Effects, made of : lentikula (Basic Spell Impacts, Druid Spell Impacts, Healing Spell Impacts), Kenney (Impact Sounds, RPG Audio, Sci-fi Sounds), Cosmo Myzrail Gorynych / CoMiGo (Bleeps and Bloops)
  - rubberduck : 80 CC0 RPG SFX / 75 CC0 breaking, falling and hit SFX / 100 CC0 SFX #2
  - artisticdude : Swishes Sound Pack
  - JaggedStone : Magic Spell SFX

## TEAM FREE (the team's own picks, never in the public repo)
- From the team's Google Drive folder soundgame : **free sounds the team found on free sound websites and checked free to use** (the user 2 Nov, `USER_BRIEFS.md` 13.138 : "ได้มาจากการไปหาเสียงฟรีบนเว็บต่างๆ ซึ่งเสียงทั้งหมดฟรีตรวจสอบมาแล้ว ใช้งานได้"). The site of each file was not written down, so they stay in `Tacetno433/Content/Audio/Borrowed/` (git never keeps it) and ship only inside the game's build. Shown in game as TEAM PICKS.
  - `Hit_1.wav` `HIt_2.wav` `Hit_3.wav` (the weapons) / `some GUI.wav` `Enter the Fight.wav` `enter_somthing.wav` `Dice event.wav` (the menus) / `Boss_music_1.wav` `title_music.wav` (the title and boss tracks) / "Designed, Braam ..." SND58427 SND58575 SND56455 SND58610 SND56476 SND56543 (the bosses)
  - If the team later finds which site a file came from, write it here : a file whose licence allows sharing can then move into the repo as FREE.

## BORROWED (closed playtest only, never in the public repo)
- None now.
- **Worldless** (the user asked 13.135 and 13.138) : the soundtrack by Berlinist and beGun is sold, not free : Bandcamp (berlinistmusic.bandcamp.com, "Worldless (Original Game Soundtrack)", 18 tracks, 5 EUR or more, all rights reserved) and a Steam DLC. Iris found no free legal source and does not download from unofficial sites. If the team buys it and wants it in the closed playtest : put the .ogg files in `Borrowed/` named after the slots (`title gallery route battle elite boss boss_turn victory defeat shop event rest ending`) and write here which track sits in which slot, with "Worldless (Original Game Soundtrack), Berlinist and beGun".

## Not used by Beta1 (kept)
- freesound : ManDaKi "Ballad E.Piano" (876172, CC0) / Victek "FX Bassdrop" (541744, CC-BY 4.0) / Streetpoptunez "Poach Dulcimier" (578226, CC-BY 4.0) : downloaded in the Drive, not in the game
