# ผลค้น Alpha 7 : QTE ตีตามเวลาและการปัด (สัญญาณเตือน หน้าต่างเวลา feedback VFX)

> ผลดิบจาก agent ค้นเว็บ 18 ต.ค. (workflow แรก) คัดมาทั้งหมดไม่ได้แก้ · ยังไม่ได้ตรวจซ้ำ · agent เปิดหน้าเว็บเต็มไม่ได้ ข้อมูลมาจากบทสรุปผลค้นหา · ใช้ตอนทำขั้น 2 (หน้าต่อสู้) แล้วสรุปที่ใช้จริงลง `ALPHA7_DESIGN.md` ข้อ 13

# วิจัย QTE ตีตามจังหวะและการปัด: เกมอ้างอิง และข้อเสนอสำหรับวงแหวนหดของ TACET

**สรุปก่อน:** ข้อเสนอ 5 อันดับแรกในข้อ 4 ทำได้เร็วและได้ผลมาก เพราะใช้โค้ดที่มีอยู่แล้วเกือบทั้งหมด ได้แก่ ให้วงแหวนหดด้วยความเร็วคงที่และบอกล่วงหน้าว่าต้องตวัดท่าอะไร ใส่ชุดผลตอบสนองตามเกรด (เก็บตัวเลขใน BattleRules) บอก EARLY/LATE ให้ผู้เล่นปรับมือได้ ทำให้ hit-stop ไม่ไปกินเวลาของโน้ตถัดไป และห้ามเขย่าวงแหวนกับโน้ต

**วิธีค้น:** WebFetch เปิดหน้าเว็บไม่ได้ทุกโดเมน (ขึ้น `getaddrinfo ENOTFOUND`) ข้อมูลเว็บทั้งหมดจึงมาจากบทสรุปของผลค้นหา WebSearch ผมไม่ได้เปิดอ่านหน้าเต็ม ตัวเลข ms ของ Expedition 33 ไม่มีตัวเลขทางการ มีแต่ที่ผู้เล่นวัดเอง ผมไม่ได้แก้ไฟล์ใดใน repo

---

## 1. สิ่งที่ได้จากเกมอ้างอิง

### Clair Obscur: Expedition 33
- **หน้าต่างเวลา:** ไม่มีตัวเลขทางการ ผู้เล่นอ้างว่าปัด (parry) ประมาณ 0.15 วิ หลบ (dodge) ประมาณ 0.22 ถึง 0.25 วิ และเท่ากันทั้ง Normal กับ Expert **(ไม่ยืนยัน)** [Steam](https://steamcommunity.com/app/1903340/discussions/0/592895604607180519) [mein-mmo](https://mein-mmo.de/clair-obscur-expedition-33-ohne-ausweichen-parieren/)
- **หลบง่ายกว่าปัด:** หน้าต่างหลบกว้างและนานกว่า คำแนะนำคือหลบก่อนเพื่อเรียนจังหวะ แล้วค่อยปัด [Push Square](https://pushsquare.com/guides/clair-obscur-expedition-33-how-to-improve-parry-timing-and-make-parrying-easier)
- **ตัวเลือกความยาก:** แพตช์ 1.3.0 ขยายหน้าต่างหลบและปัดของ Story mode อีก 40% และลดดาเมจที่โดนเหลือ 50% [Automaton](https://automaton-media.com/en/news/clair-obscur-expedition-33-makes-parrying-and-dodging-easier-in-story-mode-allowing-for-a-more-traditional-turn-based-rpg-experience/) [Siliconera](https://www.siliconera.com/clair-obscur-expedition-33-patch-adjusts-dodge-and-parry-timing/)
- **Automatic QTE:** ให้เกมกด QTE ตอนโจมตีแทนได้ [Xbox Wire](https://news.xbox.com/en-us/2025/04/23/tips-to-get-started-clair-obscur-expedition-33/) แต่ใช้กับการหลบและปัดไม่ได้ [GIGA](https://www.giga.de/games/clair-obscur-expedition-33-alle-tipps-und-guides-zum-rollenspiel--01JSM9PCT6EW7PS53T4DA4T4XC) [caniplaythat](https://caniplaythat.com/2025/12/04/clair-obscur-expedition-33-accessibility-review/)
- **ตัวช่วยอื่น:** caniplaythat ระบุว่าปิดการขยับกล้องได้ (ลดเมารถ) และลองสู้ใหม่ได้ไม่จำกัด แพตช์ 1.4.0 เพิ่มปุ่ม Battle Retry หลังแพ้ [Push Square](https://www.pushsquare.com/news/2025/07/clair-obscur-expedition-33-patch-1-4-0-brings-battle-retry-better-ui-teases-more-to-come)
- **บอกจังหวะด้วยเสียงเป็นหลัก:** มี session ที่ GDC ชื่อ "Audio Cues for Parry Timing" บอกว่าแต่ละท่ามีเสียงเฉพาะ (ตั้งแต่เสียงวืดจนถึงเสียงไล่โน้ต) ไว้ใช้ตอนภาพอย่างเดียวแคบเกินไป [gamedev.net](https://gamedev.net/news/audio-cues-for-parry-timing-in-expedition-33-r5168/) ผู้เล่นบอกว่า "95% ให้ปัดตามเสียง เพราะท่าทางในภาพเชื่อไม่ได้" [Steam](https://steamcommunity.com/app/1903340/discussions/0/592895445665075343/)
- **ท่าหลอก:** ศัตรูง้างค้างไว้แล้วตีช้ากว่าที่คิด เป็นสาเหตุหลักที่ปัดพลาดเพราะกดเร็วไป [Push Square](https://pushsquare.com/guides/clair-obscur-expedition-33-how-to-improve-parry-timing-and-make-parrying-easier)
- **Gradient Counter:** ก่อนท่าใหญ่ เกมจะช้าลงและจอกลายเป็นเทาเกือบขาวดำ แล้วต้องกดปุ่มคนละปุ่มกับปุ่มปัด ไกด์บอกว่าผู้เล่นสับสนปุ่มกันบ่อย และเข้าใจผิดว่าต้องกดตอนเอฟเฟกต์เริ่ม ทั้งที่ต้องกดตอนท่าตีจริงลงมา [Deltia](https://deltiasgaming.com/?p=211864) [GamerBlurb](https://gamerblurb.com/articles/gradient-counter-guide-clair-obscur-expedition-33)
- **รางวัล:** ปัดได้ AP +1 ปัดครบทุกฮิตในท่อนแล้วได้สวนกลับ หลบเฉยๆ ไม่ได้ AP [Beebom](https://beebom.com/how-to-parry-dodge-jump-in-clair-obscur-expedition-33/amp/)
- **คำวิจารณ์:** การปัดกลบการวางแผน (กดโจมตีธรรมดารัวๆ ก็ผ่านได้) และแต่ละท่าใช้เวลาอย่างน้อยราวครึ่งวินาที [RAWG reviews](https://rawg.io/games/clair-obscur-expedition-33/reviews) **(ไม่ยืนยัน)**
- **QTE ตอนตี:** ตัวชี้วิ่งรอบรูปทรง แล้วกดตอนถึงช่องสีเหลือง [Steam](https://steamcommunity.com/app/1903340/discussions/0/592895445665136015) **(ไม่ยืนยัน)** มีไกด์อ้างว่า PERFECT เพิ่มดาเมจได้ถึง +100% **(ไม่ยืนยัน)**

### Sea of Stars
- **กดตามจังหวะ:** ตีตรงจังหวะได้ฮิตเพิ่มหรือดาเมจเพิ่ม กันตรงจังหวะลดดาเมจราว 30 ถึง 50% (แต่ละแหล่งให้ตัวเลขต่างกัน) ไม่กดก็ยังชนะได้ [caniplaythat](https://caniplaythat.com/2023/11/01/sea-of-stars-accessibility-review/) [fandom](https://seaofstars.fandom.com/wiki/Combat)
- **ของช่วยเล่น (relic):** [PCGamesN](https://www.pcgamesn.com/sea-of-stars/relics) [caniplaythat](https://caniplaythat.com/2023/11/01/sea-of-stars-accessibility-review/)
  - Sequent Flare มีแสงพุ่งออกจากตัวเมื่อกดสำเร็จ เป็นแค่ผลตอบสนอง ไม่ได้บอกล่วงหน้าว่าต้องกดเมื่อไร
  - Adamant Shard ทำให้การโจมตีธรรมดาจับจังหวะเอง แลกกับโบนัสที่ลดลง (แหล่งให้ตัวเลขไม่ตรงกัน)
  - Sixth Sense มีโอกาส 35% ที่จะกันให้เอง
- **ไม่มีภาพบอกจังหวะ:** ก่อนวางขาย มีผู้เล่นอ้างว่าทีมพัฒนาบอกจะไม่ใส่ภาพบอกจังหวะ **(ไม่ยืนยัน)** [Steam](https://steamcommunity.com/app/1244090/discussions/0/3805028677406464110)
- **คำวิจารณ์:** ท่าทางในภาพดูช้ากว่าจังหวะจริงจนกันไม่ติด และสกิลหลายฮิตจับจังหวะยาก [GamingTrend](https://gamingtrend.com/reviews/sea-of-stars-review-a-solstice-celebration/)
- **Locks:** ท่าชาร์จของศัตรูมีไอคอนนับถอยหลัง ตีให้ตรงชนิดเพื่อทำลายแล้วท่านั้นถูกยกเลิก เป็นการบอกท่าศัตรูล่วงหน้าที่ดี [fandom](https://seaofstars.fandom.com/wiki/Combat)

### Paper Mario TTYD / Super Mario RPG remake / Mario & Luigi
- **TTYD Guard:** กดภายใน 8 เฟรม (ประมาณ 133 ms) ก่อนโดน ลดดาเมจ 1 [MarioWiki Guard](https://www.mariowiki.com/Guard)
- **TTYD Superguard:** กดภายใน 3 เฟรม (ประมาณ 50 ms) ไม่เสียดาเมจ สะท้อนกลับ 1 และกันสถานะ ศัตรูบางตัวตีจังหวะเหลื่อมเพื่อหลอก [MarioWiki](https://mariowiki.com/Superguard) [setsideb](https://setsideb.com/?p=7214)
- **TTYD badge:**
  - Simplifier ทำให้กดง่ายขึ้นแต่ได้ Star Power น้อยลง Unsimplifier กลับกัน [Simplifier](https://www.mariowiki.com:443/Simplifier) [Unsimplifier](https://mariowiki.com/Unsimplifier)
  - Timing Tutor แสดงจังหวะของท่า Stylish [Nintendo Life](https://www.nintendolife.com/guides/paper-mario-the-thousand-year-door-all-badges-where-to-find-all-abilities)
- **TTYD คำชมไล่ระดับ:** Nice → Good → Great → Wonderful → Excellent เมื่อทำ Stylish ต่อกัน คนดูยิ่งเชียร์และเติม Star Power ให้ ผลตอบสนองจึงผูกกับทรัพยากรที่ได้ [MarioWiki Stylish](https://www.mariowiki.com/Stylish_move)
- **Super Mario RPG remake:**
  - เพิ่มเครื่องหมาย "!" บอกจังหวะ ทำให้เรียนง่ายขึ้น แต่ถ้ากดเร็วไป "!" จะไม่ขึ้นเลย ผู้เล่นจึงไม่รู้ว่าพลาดทางไหน [Stevivor](https://stevivor.com/reviews/super-mario-rpg-review-small-plumber-big-quest/) [Sportskeeda](https://sportskeeda.com/esports/super-mario-rpg-remake-review-a-remarkable-adaptation-one-snes-best-role-playing-games)
  - Breezy mode ผ่อนจังหวะตอนตี และสลับโหมดได้ทุกเมื่อ [MarioWiki](https://www.mariowiki.com/Super_Mario_RPG_(Nintendo_Switch))
  - กดสำเร็จแล้วเติมเกจ Triple Move ซึ่งเหมือนเกจ SIGNATURE ของเรา [RPG Site](https://www.rpgsite.net/news/14817-super-mario-rpg-remake-introduces-new-timed-aoe-attacks-and-triple-move-mechanic)
- **Mario & Luigi Brothership:** มือใหม่พลาดเพราะกดตั้งแต่ศัตรูเริ่มง้าง ศัตรูทุกตัวมีท่าบอกล่วงหน้าเฉพาะตัวให้จำ [Gamenguide](https://www.gamenguide.com/articles/108802/20260817/mario-luigi-brothership-early-game-tips-avoid-common-beginner-mistakes.htm) [Gfinity](https://www.gfinityesports.com/article/mario-and-luigi-brothership-beginners-tips) และมีที่ฝึกท่าอยู่ในเมนู settings (Snoutlet School)

### เกมที่ใกล้กับวงแหวนของเรามากที่สุด
- **Lost Odyssey (Aim Ring):** วงนอกหดเข้ามาทับวงใน กดตอนทับกัน ได้ผล Bad / Good / Perfect ถ้าได้ Good แหวนเสริมพลังทำงานแค่บางส่วน (มีอ้างว่าราว 10% **ไม่ยืนยัน**) คนวิจารณ์ว่าง่ายกว่า Legend of Dragoon [RPGamer](https://archive.rpgamer.com/games/other/xbox2/losto/reviews/lostostrev3.html) [WorthPlaying](https://www.worthplaying.com/article/2008/1/22/previews/48184-xbox-360-preview-lost-odyssey/)
- **Legend of Dragoon (Additions):** กรอบสี่เหลี่ยมหดเข้า กดเร็วไปตรงกลางเป็นสีเทา กดช้าไปเป็นสีน้ำเงิน ผู้เล่นจึงรู้ว่าพลาดทางไหน ท่าที่ศัตรูสวนกลางคอมโบ (ต้องเปลี่ยนปุ่ม) ถูกวิจารณ์ว่าทำให้เล่นสะดุดและยากเกิน [fandom](https://legendofdragoon.fandom.com/wiki/Additions) [GameInformer](https://gameinformer.com/games/members/b/natea64_blog/archive/2018/03/19/review-the-legend-of-dragoon)
- **osu! (approach circle):**
  - เวลาที่วงแหวนเริ่มหดก่อนถึงจุด: AR5 = 1200 ms (fade in 800 ms) AR0 = 1800 ms AR10 = 450 ms [osu! wiki AR](https://osu.ppy.sh/wiki/en/Beatmap/Approach_rate)
  - หน้าต่างเวลา: 300 = 80−6×OD, 100 = 140−8×OD, 50 = 200−10×OD ms ที่ OD5 ได้ ±50 / ±100 / ±150 ms [osu! wiki OD](https://osu.ppy.sh/wiki/en/Beatmap/Overall_difficulty)

### Hi-Fi Rush (เอาแค่เรื่องผลตอบสนอง)
- **ปัดแบบถาม-ตอบ:** ศัตรูโชว์ลำดับท่าด้วยภาพและเสียงก่อน แล้วผู้เล่นปัดตามลำดับ Perfect ไม่เสียเลือด Good เสียบางส่วน พลาดเสียเต็ม ซึ่งตรงกับข้อ 5.4 ของเราพอดี [CGM](https://www.cgmagonline.com/review/game/hi-fi-rush-xbox-series-x-review)
- **ตีพลาดจังหวะยังตีออก:** แค่แรงน้อยลง ไม่ลงโทษหนัก [Tacoma Ledger](https://thetacomaledger.com/2023/02/27/the-ingenious-accessibility-of-hi-fi-rush/)
- **ตัวช่วยเรื่องภาพ:** แมว 808 กะพริบตามจังหวะ (เลือกได้ 3 แบบ) และมีแถบ Rhythm Assist ด้านล่างจอ [Kotaku](https://kotaku.com/hi-fi-rush-game-pass-rhythm-game-visual-options-1850053933) [Dot Esports](https://dotesports.com/indies/news/how-to-turn-on-rhythm-assist-in-hi-fi-rush)
- **โหมดช่วยกด:** Auto-Action Mode และ Single Button [Krafton](https://support.krafton.com/hc/en-us/articles/52371575035801-Hi-Fi-RUSH-Accessibility-Settings)

### Hit-stop และการเขย่าจอ
- **Sakurai:** คลิป "Stop for Big Moments!" บอกว่าการหยุดภาพตอนกระทบช่วยให้ผู้เล่นรู้สึกถึงแรงตี เรียกการหยุดตอนล้มบอสว่า "boss stop" และแนะให้เขย่าตัวที่โดนตีระหว่างหยุดภาพ [Nintendo Wire](https://nintendowire.com/news/2022/12/12/this-week-in-sakurai-12-5-12-11-fine-tuning-hit-stop-and-cheating-the-system/) [Source Gaming](https://sourcegaming.info/2015/11/11/thoughts-on-hitstop-sakurais-famitsu-column-vol-490-1/)
- **SF6:** ท่าหนักหยุดภาพนานกว่า [SRK wiki](https://srk.shib.live/w/Street_Fighter_6/Game_Data)
- **Smash Ultimate:** หยุดภาพ ⌊d×0.65+6⌋ เฟรม ท่า 15% ได้ 15 เฟรม (ประมาณ 250 ms) [SmashWiki](https://ssbwiki.com/Hitlag)
- **Vlambeer:** ยิงหนึ่งนัดมีหลายอย่างซ้อนกัน คือเสียง ปลอกกระสุน กล้องดีด และจอเขย่าที่หายเร็ว [infovore](https://infovore.org/?p=5275) [Kenney](https://kenney.nl/learn/must-see-videos-for-indie-developers)

### Accessibility
- **Game Accessibility Guidelines (GAG):** อย่าให้การจับจังหวะแม่นๆ เป็นเงื่อนไขบังคับ ให้มีทางเลือกหรือข้ามได้ [GAG](https://gameaccessibilityguidelines.com/do-not-make-precise-timing-essential-to-gameplay-offer-alternatives-actions-that-can-be-carried-out-while-paused-or-a-skip-mechanism/) และควรเลี่ยงการกดรัว
- **XAG 116 (แนวทางของ Xbox):** ให้ขยายเวลาได้ถึง 10 เท่า [Xbox](https://devdocs.xbox.com/build/game-principles/accessibility/xag-deep-dives/xag-116-time-limits)
- **WCAG 2.3.1:** กะพริบไม่เกิน 3 ครั้งต่อวินาที และสีแดงอิ่มเสี่ยงกว่าสีอื่น [WCAG](https://www.disabilityworld.org/toolkit/standards/wcag/2-3-1-three-flashes-or-below-threshold/)
- **Playdate:** มีตัวเลือก Reduce Flashing ระดับระบบ [Playdate](https://help.play.date/accessibility/reduce-flashing/)

---

## 2. บทเรียนที่ใช้กับเกมเรา

1. **ให้รู้ว่าต้องตวัดท่าอะไรก่อนรู้ว่าตวัดเมื่อไร:** ทั้ง Hi-Fi Rush, Locks ของ Sea of Stars และท่าบอกล่วงหน้าของ Mario & Luigi ให้ผู้เล่นรู้ว่าศัตรูจะทำอะไรก่อนถึงจังหวะ
2. **ภาพบอกจังหวะต้องตรงกับจังหวะจริง 100%:** ความผิดหวังหลักของ Expedition 33 และ Sea of Stars คือท่าทางในภาพไม่ตรงกับหน้าต่างเวลา เกมเรายังปิดเสียง ภาพต้องรับหน้าที่บอกจังหวะคนเดียว
3. **บอกให้ได้ว่าพลาดทางไหน:** Legend of Dragoon บอกด้วยสีว่าเร็วหรือช้า ส่วน Super Mario RPG remake ไม่บอกอะไรเลยและโดนติ
4. **ท่าพิเศษอย่าเปลี่ยนวิธีกด:** Gradient Counter ใช้ปุ่มอื่นแล้วผู้เล่นสับสน และการสวนกลางคอมโบของ Legend of Dragoon ทำให้เล่นสะดุด
5. **ขยายหน้าต่างเป็นทางช่วยหลัก:** Expedition 33 ใช้ +40% Super Mario RPG ใช้ Breezy และให้ autoplay ได้เฉพาะตอนตี ส่วนการป้องกันยังต้องเล่นเอง (แบบ Expedition 33)
6. **หน้าต่างของเราไม่แคบเกินไป:** NORMAL PERFECT ±100 ms / GOOD ±220 ms (`Data/BattleRules.cs:88-89`) กว้างกว่า osu! OD5 (±50 / ±150) และกว้างกว่า Superguard (50 ms) มาก เหมาะกับการตวัดเมาส์ที่แกว่งกว่าการคลิก ส่วน MAESTRO ±80 / ±180 (`:251-252`) ยังพอเล่นได้ ไม่ควรแคบกว่านี้

---

## 3. โค้ดตอนนี้ที่เกี่ยวข้อง

| เรื่อง | ไฟล์และบรรทัด | ข้อสังเกต |
|---|---|---|
| จับเวลาตอนไม้หยุด (ictus) | `Core/GestureReader.cs:18-19`, `StopSpeed` 110 อยู่ที่ `:33` | ตรวจว่าไม้หยุดหลังมันหยุดจริงอยู่เล็กน้อย เพราะความเร็วถูกเฉลี่ยกับเฟรมก่อน (`:117-119`) จึงต้องพึ่งการตั้ง STROKE TIMING |
| ตั้งเวลาเอง ±150 ms + หน้าทดสอบ | `Core/Settings.cs:35-37`, `Screens/SettingsScreen.cs:292` | มีแล้ว ใช้ต่อได้ |
| วงแหวนหดแบบเส้นตรง | `Core/NoteGlyph.cs:50-59` (สูตรรัศมีที่ `:55`) | ถูกต้อง สิ่งที่บอกจังหวะต้องหดด้วยความเร็วคงที่ ห้ามใส่ easing |
| จอเขย่า | `Screens/DuelScreen.cs:636` ลดลง 3.5 ต่อวินาที, `:1392` = shake²×10 px | ตามแบบ trauma ของ skill game-feel อยู่แล้ว |
| ไม่เขย่าเลน โน้ต และวงแหวน | `Screens/DuelScreen.cs:1384-1386`, `:1427` | กฎดีมาก ต้องเก็บไว้ในหน้าจอใหม่ |
| hit-stop | `Data/BattleRules.cs:180` 0.14 วิตอนจบท่อน, `Screens/DuelScreen.cs:669-673` | ระหว่างหยุด `Update` ออกก่อนถึงโค้ดตัดสิน ถ้าเอาไปใช้กลางท่อนหลายโน้ตจะทำให้จังหวะโน้ตถัดไปเพี้ยน (ตัวอ่านไม้ยังทำงานต่อที่ `:632`) |
| เอฟเฟกต์ที่ใช้ซ้ำได้ | `Battle/DuelEffects.cs:201` Flare, `:232` SpawnHit(grade), `:248` Shatter, `:287` Slash, `:332` Pop | พอสำหรับชุดผลตอบสนองตามเกรด |
| สีเน้น | `Core/Palette.cs:39` ตอนนี้เป็นสีขาว | ถ้าเปลี่ยนเป็นแดง ห้ามกะพริบแดงเต็มจอ (WCAG) |
| ตั้งค่าที่มี | `Screens/SettingsScreen.cs:57-62` | ยังไม่มีตัวเลือกเขย่าจอหรือแสงกะพริบ |

---

## 4. ข้อเสนอสำหรับ QTE ของเรา (เรียงตามความคุ้ม: ผลมาก ใช้แรงน้อยขึ้นก่อน)

ตัวเลขทุกตัวเป็นค่าตั้งต้นที่ยังไม่ได้ทดสอบ ให้เก็บใน `Data/BattleRules.cs`

| # | ข้อเสนอ | ผล | แรงที่ใช้ |
|---|---|---|---|
| 1 | **ระบบบอกจังหวะ** ตามรายละเอียดด้านล่าง | มาก | น้อย |
| 2 | **ชุดผลตอบสนองตามเกรด** ฟังก์ชันเดียวที่รับเกรดกับชนิด (ตี/ปัด) ตามตารางด้านล่าง | มาก | น้อยถึงกลาง |
| 3 | **บอก EARLY / LATE เมื่อได้ GOOD หรือ MISS** ขีดเล็กบนวงแหวนฝั่งที่พลาด + คำ EARLY/LATE เตรียมข้อความไว้ตอนโหลด | มาก (ช่วยเรียน) | น้อย |
| 4 | **กติกา hit-stop ในท่อนหลายโน้ต** ตามรายละเอียดด้านล่าง | มาก | น้อย |
| 5 | **ห้ามเขย่าวงแหวน โน้ต และไม้** (กฎเดิมที่ `DuelScreen.cs:1384`) เขย่าเฉพาะตัวละคร ศัตรู และฉาก ถ้าเขย่าสิ่งที่บอกจังหวะ ผู้เล่นจะจับเวลายากขึ้น | มาก | น้อย |
| 6 | **กันเบื่อ** ตามรายการด้านล่าง | มาก | กลาง |
| 7 | **ตั้งค่า Accessibility** ตามรายการด้านล่าง | กลาง | น้อย |
| 8 | **CADENZA (ท่าใหญ่บอส)** ใช้วิธีตวัดเดิม ห้ามเปลี่ยนปุ่ม | กลาง | กลาง |
| 9 | **เสียงบอกจังหวะ** (Expedition 33 พึ่งเสียงเป็นหลัก) ทำตอนถึงเฟสเสียง | มาก | รอ |

### ข้อ 1 ระบบบอกจังหวะ
- **วงแหวนหดเวลาคงที่ต่อโหมด:** NORMAL 1.1 วิ MAESTRO 0.8 วิ (เทียบ osu! AR5 1200 ms) ค่อยๆ ปรากฏในช่วง 1/3 แรก และไม่สุ่มความเร็ว
- **ก่อนเริ่มท่อน:** แสดงโน้ตทั้งท่อน (ลูกศร + p/mf/f) เรียงเป็นแถวเล็กเหนือศัตรูหรือเหนือจุดตีราว 0.6 วิ แล้วค่อยเริ่ม ผู้เล่นจะรู้ว่าต้องตวัดท่าอะไรก่อนรู้ว่าตวัดเมื่อไร
- **ระยะห่างโน้ตในท่อน:** ไม่ต่ำกว่า 0.45 ถึง 0.5 วิใน NORMAL พอให้เด้งไม้ต่อได้ (`GestureReader.cs:156-157`) ใช้รูปแบบตายตัวต่อสกิลหรือต่อท่าศัตรู ไม่สุ่ม จะได้จำได้
- **ตอนปัด บอกจังหวะสองทาง:** โน้ตบินตามเลนมาถึงจุดตี และวงแหวนหดที่จุดตีไปพร้อมกัน
- **ศัตรูธรรมดาห้ามมีท่าหลอกหรือตีช้า:** ถ้า Elite หรือบอสจะมี ต้องมีสัญลักษณ์บอกชัด

### ข้อ 2 ชุดผลตอบสนองตามเกรด
ค่าเขย่าคิดจากสูตรเดิม shake²×10 px

| เกรด | ตี | ปัด |
|---|---|---|
| **PERFECT** | หยุดภาพ 70 ถึง 80 ms ใส่ trauma 0.55 (ประมาณ 3 px) แล้วกล้องดีดไปทางที่ตวัด 4 ถึง 6 px แล้วค่อยกลับ (ease-out 0.15 วิ) วงแหวนแตกเป็นวงบางขยายออก (Shatter) มีเส้น Slash สีเน้นตามทางไม้ ป้ายคำเด้งด้วย BACK ease 1.3 → 1.0 ใน 0.18 วิ กรอบตัวละครกับศัตรูกะพริบขาว 1 ถึง 2 เฟรม ศัตรูถอย 8 ถึง 12 px แล้วกลับ | โน้ตถูกตีกลับไปหาศัตรู ลม +1 เด้งขึ้นแล้วบินเข้าแถบลม (แบบดาวของคนดูใน TTYD) ทำให้เห็นว่าลมมาจากไหน |
| **GOOD** | ไม่หยุดภาพหรือหยุดแค่ 30 ms trauma 0.25 (แทบไม่ขยับ) มีประกายเล็ก คำเป็นสีเทา | เหมือนกัน |
| **MISS** | ไม่เขย่า ไม่หยุดภาพ (ไม่ให้รางวัลกับการพลาด) เศษวงแหวนสีเทาร่วงลง | ตรงนี้ถึงใช้การเขย่า: trauma 0.6 ถึง 0.7 (4 ถึง 5 px) กรอบนักดนตรีกลับสีขาวดำ 1 เฟรม แถบเลือดมีส่วนจางที่ค่อยๆ หดตามหลัง |

- **ท่อนที่ปัด PERFECT ต่อกัน:** ผลแต่ละโน้ตใหญ่ขึ้นทีละขั้น แสดงเป็นจุดนับเช่น ●●○ ไม่ใช้คำ (เลียน Nice→Excellent ของ TTYD แต่สั้นกว่า) ครบท่อนแล้วขึ้น COUNTER พร้อม trauma 0.8 และหยุดภาพ 0.12 วิ
- **ศัตรูเต็มเกจ OFF-BEAT หรือ CADENZA:** trauma 1.0 หยุดภาพ 0.14 วิ (ค่าเดิม) จอขาวแวบ 1 ครั้ง

### ข้อ 4 กติกา hit-stop ในท่อนหลายโน้ต
- ระหว่างโน้ต หยุดเฉพาะภาพตัวละครกับศัตรู (หยุดเฉพาะจุดตามที่ skill game-feel แนะนำ) ห้ามหยุดนาฬิกาของ QTE และห้ามหยุดการอ่านไม้
- หยุดทั้งจอได้หลังโน้ตสุดท้ายเท่านั้น
- อย่าเอา `return` ออกจาก `Update` แบบที่ `DuelScreen.cs:669-673` ไปใช้กลางท่อน

### ข้อ 6 กันเบื่อ
- โจมตีธรรมดา 1 โน้ตเสมอ (ตรงกับข้อ 5.2 ของเอกสารออกแบบ)
- ศัตรูแต่ละตัวมีท่าตายตัว 2 ถึง 3 ท่อน ไม่สุ่มโน้ตใหม่ทุกตา
- ท่อนศัตรูชั้น 1 ยาวไม่เกิน 2 โน้ต (ตรงกับข้อ 8 ของเอกสารออกแบบ)
- โน้ตรัวไม่เกิน 1.5 วิ (GAG ให้เลี่ยงการกดรัว)
- ตัวเลือก **BATTLE SPEED ×1 / ×1.5 / ×2** เร่งเฉพาะท่าทางกับป้ายต่างๆ ไม่เร่งวงแหวน (Expedition 33 โดนติว่าแต่ละท่าอย่างน้อยครึ่งวินาที)
- คลิกขวาเพื่อข้ามท่าทางหรือป้าย แต่ข้าม QTE ไม่ได้

### ข้อ 7 ตั้งค่า Accessibility
- **SCREEN SHAKE** 0 / 50 / 100% คูณผลของ trauma
- **FLASHES** ON / REDUCED ถ้า REDUCED แทนการแวบขาวด้วยการค่อยๆ สว่าง และไม่กะพริบเกิน 3 ครั้งต่อวินาที
- ใช้ STROKE TIMING ที่มีอยู่แล้ว (`Settings.cs:35-37`)
- เกรดดูได้จากรูปทรงกับคำ ไม่ใช้สีอย่างเดียว (เกมเป็นขาวดำอยู่แล้ว)
- **ต้องให้ผู้ใช้ตัดสิน เพราะลดความยาก (CLAUDE.md ห้ามลดความยากโดยไม่ถาม):**
  - AUTO STROKE ให้การตีธรรมดาได้ GOOD เองโดยไม่ได้โบนัส PERFECT (แบบ Adamant Shard หรือ Automatic QTE) ส่วนการปัดยังต้องทำเอง
  - TIMING ASSIST หน้าต่างกว้างขึ้น 40% แบบ Story mode

### ข้อ 8 CADENZA
- เกมเราขาวดำอยู่แล้ว จึงใช้ "กลับสีจอเป็นภาพเนกาทีฟ 1 เฟรม" + ภาพช้าลง 0.6 เท่าเป็นเวลา 0.3 วิ + โน้ตเรืองสีเน้น แทน "จอเป็นขาวดำ" ของ Expedition 33
- ห้ามเปลี่ยนปุ่มหรือวิธีตวัด จะได้ไม่สับสนแบบ RT/RB ของ Expedition 33 หรือการสวนกลางคอมโบของ Legend of Dragoon
- จังหวะที่ต้องตวัดคือตอนวงแหวนแตะโน้ต ไม่ใช่ตอนเอฟเฟกต์เริ่ม

### ข้อ 9 เสียงบอกจังหวะ
ทำตอนถึงเฟสเสียง เพราะตอนนี้เสียงยังปิดตามคำสั่งผู้ใช้และห้ามใส่เสียงชั่วคราว ตอนนั้นให้มีเสียงเตือนสั้นๆ ก่อนโน้ตจำนวนเวลาตายตัว และเสียงอาวุธต่อเกรด

---

## 5. เรื่องที่ชนกฎ หรือต้องถามผู้ใช้
- AUTO STROKE และ TIMING ASSIST ลดความยาก ต้องถามก่อน
- คำ EARLY/LATE และ BATTLE SPEED เพิ่มข้อความบนจอ ทั้งสองอย่างสั้นและไม่ใช่หน้าจอใหม่ แต่ควรให้ผู้ใช้เห็นก่อน
- ถ้า `Palette.Accent` จะเปลี่ยนเป็นแดง ห้ามใช้แสงกะพริบสีแดงเต็มจอ
- ตัวเลข ms ของ Expedition 33 กับข้อมูลภาพ QTE ตอนตีของมัน **ไม่ยืนยัน** ไม่ควรเอาไปอ้างในเอกสารว่าเป็นค่าจริง
