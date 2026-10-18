# ผลค้น Alpha 7 : หน้าต่อสู้ของเกม turn-based ชั้นนำ (ลำดับตา intent เลือกเป้า เลือดและสถานะ)

> ผลดิบจาก agent ค้นเว็บ 18 ต.ค. (workflow แรก) คัดมาทั้งหมดไม่ได้แก้ · ยังไม่ได้ตรวจซ้ำ · agent เปิดหน้าเว็บเต็มไม่ได้ ข้อมูลมาจากบทสรุปผลค้นหา · ใช้ตอนทำขั้น 2 (หน้าต่อสู้) แล้วสรุปที่ใช้จริงลง `ALPHA7_DESIGN.md` ข้อ 13

**สรุป:** เกมอ้างอิงที่ผู้เล่นชมเรื่องความชัดมีจุดร่วม 4 อย่าง

1. แถบลำดับตาที่เห็นตลอด
2. ศัตรูบอกล่วงหน้าว่าจะทำอะไร โดยใช้ตัวเลขแทนประโยค
3. เกจแตกอยู่ใต้หลอดเลือด
4. ช่วงที่ต้องกดตามจังหวะต้องเห็นชัดที่จุดเดียว

ส่วนที่ผู้เล่นบ่นมากที่สุดคือ ตัวหนังสือเล็ก ศัตรูหน้าเหมือนกันแยกไม่ออก ข้อมูลถูกซ่อนไว้ในเมนู และ UI ที่เน้นสวยจนอ่านยาก ข้อเสนอสำหรับหน้าต่อสู้ของเราอยู่ส่วน D และ E

**หมายเหตุวิธีค้น:** WebFetch ใช้ไม่ได้ทุกเว็บ (`getaddrinfo ENOTFOUND` และ proxy ตอบ `CONNECT tunnel failed, response 403`) ข้อมูลเว็บทั้งหมดจึงมาจากสรุปผลค้นของ WebSearch ไม่ได้เปิดอ่านหน้าเอง ข้อมูลจากกระทู้ Steam เป็นความเห็นผู้เล่น ข้อไหนแหล่งข้อมูลบาง หรือผลค้นไม่ระบุว่าประโยคมาจากลิงก์ไหน ใส่ "ไม่ยืนยัน" ไว้ ไม่ได้แก้ไฟล์ใดใน repo

---

## A. แต่ละเกม

**Clair Obscur: Expedition 33**
- **ลำดับตา:** เห็นตลอดการต่อสู้ ไว้วางแผน ([X ทางการ](https://x.com/expedition33/status/1858852599383953600), [Xbox Wire](https://news.xbox.com/en-us/2024/08/28/clair-obscur-expedition-33-combat-breakdown-preview/)) แหล่งต่างๆ บอกว่าอยู่ซ้ายบนหรือด้านซ้าย เรียงตาม Speed ([Maxroll](https://maxroll.gg/clair-obscur-expedition-33/guides/combat-guide)) การตีศัตรูบางแบบเปลี่ยนลำดับได้ ([Seeking Tech](https://seekingtech.com/how-to-change-the-turn-order-in-clair-obscur-expedition-33/))
- **เมนูคำสั่ง:** เป็น "Battle Wheel" มี Attack / Skills / Items / Free Aim ทีหลังเพิ่ม Gradient ([Fextralife Combat](https://expedition33.wiki.fextralife.com/Combat)) คำสั่งผูกกับปุ่มหน้าจอยตรงๆ เช่น Skills = Square, Items = Triangle, Parry = R1 ([Magic Game World](https://www.magicgameworld.com/clair-obscur-expedition-33-controls/))
- **AP:** เป็นแถบสีฟ้าอยู่เหนือด้านขวาของ Battle Wheel หรือใต้หลอดเลือดที่ไอคอนตัวละคร (ไม่ยืนยันว่าเป็นของแหล่งไหนในผลค้น)
- **Break:** เป็นแถบเหลืองใต้หลอดเลือดศัตรู เต็มแล้วมีไอคอนเพชรขึ้นข้างแถบ ([Seeking Tech break](https://seekingtech.com/how-to-instantly-break-and-stun-enemies-in-clair-obscur-expedition-33/), [xboxplay](https://xboxplay.games/clair-obscur-expedition-33/how-to-break-enemies-in-clair-obscur-expedition-33-63708), ไม่ยืนยันว่ารายละเอียดมาจากลิงก์ไหน)
- **บอกท่าศัตรู:** ท่า Gradient ใช้การหรี่จอเป็นสัญญาณ ([ppe.pl](https://www.ppe.pl/poradniki/367326/clair-obscur-expedition-33-akt-2-verso-zapomniane-pobojowisko-kontra-gradientowa-dualista-pochowek.html), แหล่งเดียว)
- **การสอน:** มีบทสอน parry คู่มือแนะนำให้ฝึก dodge ก่อนเพราะช่วงกว้างกว่า ([Automaton](https://automaton-media.com/en/tips/clair-obscur-expedition-33-how-to-get-the-hang-of-dodges-parries-jumps-and-gradient-counters/), [Push Square](https://www.pushsquare.com/guides/clair-obscur-expedition-33-how-to-improve-parry-timing-and-make-parrying-easier)) มี Story mode กับ Automatic QTEs ([Can I Play That](https://caniplaythat.com/2025/12/04/clair-obscur-expedition-33-accessibility-review/))
- **ที่โดนบ่น:**
  - UI สับสนเป็นบางครั้ง ([PCGamesN review](https://www.pcgamesn.com/clair-obscur-expedition-33/review))
  - ตัวหนังสือสกิลตอนต่อสู้เล็กเกิน ตั้ง text size ใหญ่แล้วไม่เปลี่ยน ([Steam 1](https://steamcommunity.com/app/1903340/discussions/0/592895604607181816), [Steam 2](https://steamcommunity.com/app/1903340/discussions/0/592895445665104229))
  - ศัตรูหน้าเหมือนกัน ดูไม่ออกว่าตัวไหนเล่นก่อน ([Steam](https://steamcommunity.com/app/1903340/discussions/0/592895905593608178))
  - ช่วงท้ายเกมภาพรกจนจับจังหวะ parry ยาก ผู้เล่นต้องพึ่งเสียง ([Steam](https://steamcommunity.com/app/1903340/discussions/0/592895604607223362), ไม่ยืนยันว่ากระทู้ไหนพูดประโยคไหน)

**Honkai: Star Rail**
- **ลำดับตา:** ส่วนใหญ่บอกว่าอยู่ซ้ายหรือซ้ายบน มีทั้งฝ่ายเราและศัตรู ตั้งค่าให้โชว์ Action Value ได้ ([AFK Gaming](https://afkgaming.com/gaming/honkai-star-rail/turn-based-combat-in-honkai-star-rail-how-does-it-work), [Icy Veins](https://www.icy-veins.com/honkai-star-rail/combat-basics)) ตาไหนมาก่อนใช้ค่า AV ([KQM](https://hsr.keqingmains.com/misc/speed-guide/))
- **เกจแตก:** ศัตรูมี 2 แถบ คือเลือดสีแดงกับ Toughness สีขาว มีไอคอนจุดอ่อน 2–3 ตัวอยู่เหนือแถบ ([Icy Veins](https://www.icy-veins.com/honkai-star-rail/combat-basics), [Fandom Toughness](https://honkai-star-rail.fandom.com/wiki/Toughness))
- **ความโปร่งใส:** ฉบับ 2.3 เพิ่มตัวเลข Toughness ของศัตรูและค่าลด Toughness ของแต่ละท่า ([SI](https://videogames.si.com/news/honkai-star-rail-update-2-3-features)) แปลว่าผู้เล่นอยากเห็นตัวเลขจริง
- **การเลือกเป้า:** ภาพหน้าศัตรูที่จะโดนสกิลขึ้นไฮไลต์สีขาว (ไม่ยืนยันว่าเป็นของแหล่งไหน)
- **บน PC:** Q = โจมตี, E = สกิล, Space = ยืนยัน, A/D = เลือกเป้า ([Magic Game World](https://www.magicgameworld.com/?p=97707))
- **ปุ่มมุมจอ:** มีปุ่มเร่งความเร็ว / ออโต้ / หยุด ([Prima](https://primagames.com/?p=120676))

**Persona 5**
- **ลำดับตา:** เรียงตาม agility ([Game Developer](https://www.gamedeveloper.com/design/learn-how-to-make-turn-based-battles-from-persona-5))
- **เมนูคำสั่ง:** คำสั่งผูกกับปุ่มหน้าจอยตรงๆ ไม่ต้องไล่เมนูหลายชั้น วางรอบตัวละครเหมือนกรอบคำพูดในการ์ตูน ([Siliconera](https://www.siliconera.com/?p=1007130)) ตัวที่เลือกอยู่จะขยายใหญ่และมีสีต่าง ([Mechanics of Magic](https://mechanicsofmagic.com/2022/04/23/visual-design-of-games-persona-5/))
- **จุดอ่อนศัตรู:** มีป้าย "Weak" ตอนเลือกตัว ตีโดนจุดอ่อนได้ 1 More ([SuperCheats](https://www.supercheats.com/persona-5/walkthrough/exploiting-weaknesses), [Baton Pass](https://megatenwiki.com/wiki/Baton_Pass)) ที่ว่าธาตุที่ยังไม่รู้ขึ้นเป็น "?" มาจากความรู้ของโมเดล ผลค้นไม่ยืนยัน
- **บทเรียน:** Inverse วิจารณ์ว่าตัวหนังสือที่เอียงและเส้นฐานไม่นิ่ง เอาสไตล์ไว้ก่อนข้อมูล ([Inverse](https://inverse.com/gaming/persona-5-tactica-user-interface)) ผู้กำกับเองยอมรับว่าตอนแรกเมนูเหลี่ยมๆ อ่านไม่ออก ([Push Square](https://www.pushsquare.com/news/2024/10/making-metaphor-refantazio-personas-immaculate-menus-is-really-annoying-says-director)) ข้อนี้สำคัญกับธีม Limbus "Hollow" ของเรา

**Darkest Dungeon 1 และ 2**
- **ลำดับตา:** ไม่บอก DD2 สุ่มความเร็วทุกรอบแต่ไม่โชว์ ([wiki.gg DD2](https://darkestdungeon.wiki.gg/wiki/Combat_Mechanics_(Darkest_Dungeon_II))) ผู้เล่นขอให้มีแถบลำดับตา และใช้เส้นขาวดูว่าใครเล่นไปแล้ว ([Steam DD](https://steamcommunity.com/app/262060/discussions/0/458607699611256529))
- **ระยะสกิล:** สกิลบอกระยะด้วยจุด 4+4 จุด เหลือง = ตำแหน่งที่ใช้ได้ แดง = เป้า ถ้าโดนหลายตัวมีเส้นเชื่อม ชี้ศัตรูแล้วเห็นโอกาสโดน ดาเมจ และ crit ([Steam](https://steamcommunity.com/app/262060/discussions/0/412447613581265645), [QT3](https://forum.quartertothree.com/t/darkest-dungeon/73511?page=32))
- **ที่โดนบ่น:** DD2 ช่วง early access มีคนบ่นว่า UI งง บทสอนเก่า ข้อมูลซ่อนเยอะ ([Steam DD2](https://steamcommunity.com/app/1940340/discussions/0/4138312860558748711))

**Slay the Spire**
- **บอกท่าศัตรู (intent):** ไอคอนเหนือหัวศัตรูบอกท่าถัดไป เป็นข้อมูลครบ ทำให้แต่ละตาเป็นปริศนาเล็กๆ ([roytang](https://roytang.net/2019/02/slay-the-spire/))
  - ท่าโจมตีบอกดาเมจกับจำนวนครั้ง เช่น "6 x 4" ไอคอนใหญ่ขึ้นตามดาเมจ ท่าพิเศษใช้ "?" ([wiki.gg Awakened One](https://slaythespire.wiki.gg/wiki/Awakened_One))
  - ผู้เล่นบางคนอ่าน "9x4" สับสน และขอให้เว้นวรรคหรือใช้สีต่าง ([Steam](https://steamcommunity.com/app/646570/discussions/0/1692659769948918101), ไม่ยืนยันว่ากระทู้ไหน)
  - STS2 วางท่าหลายอย่างเรียงข้างกันแทนซ้อนกัน ([untapped STS2](https://sts2.untapped.gg/guides/how-to-read-enemy-intent))
- **การสอน:** ใช้การชี้คำสำคัญเพื่อดูคำอธิบาย ([itch](https://itch.io/post/12318189), ไม่ยืนยัน) ดวลแรกๆ เสียน้อยจึงเป็นที่ฝึกไปในตัว ([Steam](https://steamcommunity.com/app/646570/discussions/0/1778262124951666595))
- **เมาส์:** ปี 2018 ทีมพัฒนาตอบว่าต้องคลิกเลือกเป้า และคีย์บอร์ดยังอยู่ในรายการที่จะทำ ([Steam](https://steamcommunity.com/app/646570/discussions/0/3288067088123507740))

**Limbus Company**
- **การเลือกเป้า:** ลากเคอร์เซอร์ผ่านสกิล 2 แถวเพื่อต่อโซ่ ตอนสู้บอสลากสกิลไปใส่ศัตรูหรือส่วนของร่าง ([Wikipedia](https://en.wikipedia.org/wiki/Limbus_Company)) แถบบนบอกผล clash ล่วงหน้า
- **ที่โดนบ่น:**
  - ขอให้โชว์โอกาสชนะ clash และพบว่าผลจริงเกินช่วงที่แถบบอก ([Steam](https://steamcommunity.com/app/1973530/discussions/0/3789254716329960375))
  - ไม่เตรียมผู้เล่นใหม่ จุดอ่อนซ่อนในเมนู สีของธาตุแยกยาก ([Steam](https://steamcommunity.com/app/1973530/discussions/0/6547745983578867238), ไม่ยืนยันว่ากระทู้ไหน)
  - สรุปรีวิวมี "confusing combat, poor tutorial" ([Vaporlens](https://vaporlens.app/app/1973530/limbus_company))
  - อัปเดต UI การเลือกเป้าแล้วผู้เล่นไม่รู้ว่ามี ([Steam](https://steamcommunity.com/app/1973530/discussions/0/3812911294783415005))

**Sea of Stars**
- **บอกท่าศัตรู:** ศัตรูมีตัวนับถอยหลังว่าอีกกี่ตาจะเล่น ท่าพิเศษมี "Lock" เป็นไอคอนชนิดดาเมจ ตีให้ตรงชนิดแล้วท่าอ่อนลง ทำลายครบแล้วยกเลิกท่า ([Fandom Combat](https://seaofstars.fandom.com/wiki/Combat)) รีวิวชมว่าโปร่งใสวางแผนได้ ([Checkpoint](https://checkpointgaming.net/reviews/2023/08/sea-of-stars-review-over-the-moon/), [RPGFan](https://www.rpgfan.com/review/sea-of-stars/))
- **ตัวช่วย:** relic "Sequent Flare" ขึ้นประกายเมื่อ timed hit หรือ block สำเร็จ "Adamant Shard" ทำให้ตีโดนจังหวะเองแต่โบนัสลดครึ่ง "Sixth Sense" มีโอกาสกันเอง ([GamerGuides relics](https://www.gamerguides.com/sea-of-stars/guide/getting-started/tips-and-tricks/all-relics-locations-in-sea-of-stars)) เกมไม่มีเมนู accessibility แยก ([Can I Play That](https://caniplaythat.com/2023/11/01/sea-of-stars-accessibility-review/))

**Octopath Traveler**
- **ลำดับตา:** แถบบนจอโชว์ทั้งรอบนี้และรอบถัดไป ([GamerGuides](https://www.gamerguides.com/octopath-traveler/guide/the-basics/game-mechanics/battle-system)) ชี้ศัตรูแล้วช่องของมันในแถบสว่างด้วย ([Vulkk](https://vulkk.com/2023/02/24/how-combat-works-in-octopath-traveler-2-beginners-guide/))
- **Break:** โล่เป็นตัวเลขอยู่ข้างช่องจุดอ่อน จุดอ่อนที่ยังไม่รู้เป็น "?" ([Twinfinite](https://twinfinite.net/guides/break-and-boost-system-octopath-traveler-2-explained/)) ศัตรูที่แตกจะหลุดจากลำดับและเสียตาถัดไป ([iMore](https://www.imore.com/how-master-octopath-traveler-battle-system))
- **Boost:** เป็นจุดเหนือหลอดเลือด ([GamerGuides](https://gamerguides.com/octopath-traveler/guide/walkthrough/chapter-1/subterranean-study))

---

## B. บทเรียนรวม

1. **แถบลำดับตาต้องเห็นตลอด** และต้องผูกกับตัวบนจอ (ชี้ศัตรูแล้วช่องในแถบสว่าง) DD ที่ซ่อนลำดับตาโดนขอให้เพิ่ม
2. **ท่าศัตรูบอกด้วยตัวเลขกับไอคอน ไม่ใช่ประโยค** เช่นดาเมจต่อครั้งคูณจำนวนครั้ง แต่ต้องอ่านไม่สับสน และพรีวิวต้องตรงกับผลจริง
3. **เกจแตกเป็นแถบเล็กใต้เลือด** เต็มแล้วต้องเห็นชัด และลำดับตาต้องเปลี่ยนให้เห็น
4. **ช่วงกดตามจังหวะต้องชัด** ช่วงเวลาเท่ากันแต่สัญญาณไม่ชัดก็ยากได้ เกมนั้นผู้เล่นพึ่งเสียง ส่วน **เกมเรายังปิดเสียง ภาพจึงต้องบอกจังหวะได้ 100%**
5. **ตัวหนังสือเล็กและข้อมูลซ่อนในเมนู** เป็นเรื่องที่โดนบ่นมากที่สุด
6. **ไม่แยกประเภทด้วยเฉดสีอย่างเดียว** (สีธาตุ Limbus แยกยาก) เราขาวดำ จึงต้องใช้ ทึบ/กรอบ ขนาด ตำแหน่ง และการเคลื่อนไหวแทน
7. **สไตล์ต้องไม่กินความอ่านง่าย** (P5)
8. **สอนทีละเรื่อง** ในดวลจริงที่เสียน้อย และมีตัวช่วยเป็นทางเลือก (SoS, E33)

---

## C. ข้อจำกัดจากโค้ดเรา (ตรวจแล้ว)

**ขนาดจอและเมาส์**
- จอคงที่ 1280×720 (`Tacetno433/TacetGame.cs:17-18`) วาดลงภาพเดียวแล้วขยายใส่หน้าต่าง คงสัดส่วน มีแถบดำข้าง (`TacetGame.cs:178-193`) และเมาส์ถูกแปลงกลับเป็นพิกัด 1280×720 (`Core/Input.cs:32-35`) เพราะฉะนั้นวางตำแหน่งตายตัวได้ ไม่ต้องทำ anchor

**การตวัดชนกับการคลิก**
- การตวัดนับเฉพาะตอน **กดเมาส์ซ้ายค้าง** (`Core/GestureReader.cs:21-22`) ลากสั้นกว่า 34 px ไม่นับเป็นตวัด (`:34`) ขนาด p < 100, mf 100–240, f > 240 px (`:40-41`)
- ผล 1: คลิกปุ่มธรรมดาไม่กลายเป็นตวัด
- ผล 2: **การลากไปใส่เป้าแบบ Limbus จะชนกับการอ่านตวัด** ไม่ควรใช้

**ของที่มีอยู่แล้วใช้ต่อได้**
- จุดตีเดิม `NoteGlyph.WellRadius` 70 (`Core/NoteGlyph.cs:36`) ดวลเดิมวางที่ (640, 372) (`Screens/DuelScreen.cs:90-91`)
- ช่องตัวละคร 100×170 (`Data/StageLayout.cs:88-89`)
- แถบบนของหน้ารันสูง 64 (`Core/RunHud.cs:17`) หน้าดวลเดิมไม่ได้ใช้ `RunHud.DrawTop`
- UI สำเร็จรูป:
  - `Ui.Button` มีป้ายปุ่มลัด (`Core/Ui.cs:33`)
  - `Ui.Tag` แบบทึบ/กรอบ สูง 18 (`:146`)
  - `Ui.Pips` เพชรทึบ/กรอบ (`:191`)
  - `Ui.Tooltip` (`:245`)
  - `Ui.CapsuleBar` มีขีดทุก 10% (`:302`, `:316`)
- เคยสลับเคอร์เซอร์ตามช่วงเกมแล้ว คือไม้บาตองกับลูกศรธรรมดา (`DuelScreen.cs:491`)
- สอนตอนเจอครั้งแรกมีแล้ว `TeachNote` (`DuelScreen.cs:451`)

**สีกับตัวหนังสือ**
- `Palette.Accent` ตอนนี้เป็นสีขาว (`Core/Palette.cs:36-40`) แปลว่า "สีเน้น" ยังแยกจากกันด้วยความสว่างเท่านั้น
- ฟอนต์หลัก Bahnschrift 18 (`Content/MainFont.spritefont:5`) Tiny 0.52 และ Label 0.6 (`Core/TextSize.cs:31-32`) เล็กมาก (ราว 9–11 px ถ้า 18 = 18 px ไม่ยืนยันหน่วยของตัวแปลงฟอนต์) ตัวเลขสำคัญในการต่อสู้ควรใช้ Body 0.72 ขึ้นไป

**เอกสารออกแบบ**
- `docs/ALPHA7_DESIGN.md` อ้างถึง "ข้อ 13" (บรรทัด 45, 291, 320) แต่ในไฟล์ยังไม่มีข้อ 13 (จบที่ข้อ 12)
- ข้อ 5.1 กำหนดแถบลำดับตาด้านบนและบอกท่าล่วงหน้า 1 ตา (บรรทัด 109-110)
- ข้อ 5.4 โน้ตวิ่งจากขวาเข้าหาเป้า (139)
- ข้อ 5.5 ลมสูงสุด 10 เริ่ม 3 (150)
- ข้อ 5.6 แถบเสียศูนย์ใต้เลือด (155-157)
- ข้อ 8 SIGNATURE = PERFECT 6 ครั้ง (261)

---

## D. ข้อเสนอหน้าต่อสู้ 1280×720

```
y   0 ┌──────────────────────────────────────────────────────────────────────────────┐
      │ ╔═══════╗ ┌───┐┌───┐┌───┐┌───┐ ┃ ┌───┐┌───┐┌───┐┌───┐          ROUND 2  [ESC] │ 8-56 ลำดับตา
      │ ║ KLARA ║ │ A ││OTT││ B ││IRI│ ┃ │KLA││ A ││OTT││ B │  (เรา = ทึบ ศัตรู = กรอบ)│
      │ ╚═══════╝ └───┘└───┘└───┘└───┘ ┃ └───┘└───┘└───┘└───┘                         │
   64 │                                KLARA                                         │ 66-90 บรรทัดเดียว
      │                                                      ┌─────┐   ┌──────┐      │
      │                                                      │9 ●●●│   │ MUTE │      │ 118-146 ท่าถัดไป
      │   [A]                                                └─────┘   └──────┘      │ ป้ายบนหัว = ศัตรูตัวไหนเล็งเรา
      │  ┌─────┐  ┌─────┐  ┌─────┐                          ┌───────┐  ┌───────┐     │
      │  │ Art │  │ Art │  │ Art │          ╭─────╮         │  Art  │  │  Art  │     │ 160-370 ArtSlot
      │  │Slot │  │Slot │  │Slot │          │  ◯  │ จุดตวัด   │ Slot  │  │ Slot  │     │
      │  └─────┘  └─────┘  └─────┘          ╰─────╯ 640,290 └───────┘  └───────┘     │
      │  KLARA    OTTO     IRIS        (ว่าง x 480-800)       A MUTE      B MUTE      │
      │  ▓▓▓▓░ 24 ▓▓▓▓▓ 36 ▓▓░░░ 12                         ▓▓▓▓▓▓░ 48  ▓▓▓░░░ 30   │ 384-396 เลือด + เลข
      │  [DIS 2]  [CRE 3]                                   ▔▔▔▔░░ เสียศูนย์ 4px        │ 400-404
      │                                                     [DIS 1]     [OFF-BEAT]   │ 410-428 สถานะ
  556 ├──────────────────────────────────────────────────────────────────────────────┤
      │ KLARA          BREATH ◆◆◆◇◇ ◇◇◇◇◇ 3            MOZART  SIGNATURE ◆◆◆◇◇◇      │ 566-590
      │ STRINGS  ┌──────────┐┌──────────┐┌──────────┐┌────────┐          ┌────────┐  │
      │          │1 ATTACK  ││2 TRILL ◆◆││3 BOW ◆◆◆ ││4 SWAP  │          │ SPACE  │  │ 600-664 ปุ่ม
      │          └──────────┘└──────────┘└──────────┘└────────┘          └────────┘  │
      │          ชี้สกิล = คำอธิบายบรรทัดเดียว เช่น "3 notes = DIS"                        │ 676-700
  720 └──────────────────────────────────────────────────────────────────────────────┘
```

**แถบลำดับตา (บน)**
- ช่องของตัวที่กำลังเล่นใหญ่ 96×48 ช่องอื่น 64×40
- ฝ่ายเรา = กล่องทึบสี Paper ตัวอักษรเข้ม ศัตรู = กรอบเส้น ตัวอักษรอ่อน
- ใช้ชื่อย่อ 3 ตัว ไม่ใช้หน้าตัวละคร เพราะอาร์ตเป็นของทีมอาร์ต
- ศัตรูชนิดเดียวกันใส่ตัวอักษร A/B/C ทั้งในแถบและที่ชื่อ (แก้ปัญหาของ E33)
- มีเส้นคั่นรอบนี้กับรอบถัดไปแบบ Octopath ชี้ตัวไหนบนจอ ช่องของมันในแถบสว่าง และกลับกัน
- ศัตรูเสียศูนย์ ช่องถัดไปของมันถูกขีดฆ่า

**ลม (BREATH)**
- ใช้ `Ui.Pips` 10 เม็ด แบ่ง 5+5 พร้อมตัวเลข วางเหนือปุ่มคำสั่ง (เหมือน E33 ที่ AP อยู่ติดวงล้อคำสั่ง)
- ปุ่มสกิลมีเพชรบอกราคา ชี้สกิลแล้วเม็ดที่จะถูกใช้กะพริบ ลมไม่พอ = ปุ่มจาง
- ได้ลมแล้วมี "+1" ลอยเข้าแถบ

**เสียศูนย์ (OFF-BEAT)**
- แถบบาง 4 px ใต้เลือดศัตรู มีขีดทุก 25%
- ตอนเลือกเป้า โชว์ส่วนที่จะเติมเป็นเงาจาง ถ้าตวัด PERFECT
- เต็มแล้ว กรอบศัตรูกลับสี (ทึบ) ขึ้นป้าย `OFF-BEAT` และ `×1.5` และช่องในแถบลำดับตาถูกขีด (Octopath / HSR / E33)

**ท่าถัดไปของศัตรู (intent)**
- ท่าโจมตี = ตัวเลขดาเมจต่อโน้ต + จุดเท่าจำนวนโน้ต เช่น `9 ●●●` แทน "9x3" ที่อ่านสับสนใน STS
- ท่าอื่นใช้คำเดียว เช่น `MUTE`
- ท่าใหญ่ของบอส (CADENZA) กล่องกลับสีเป็นทึบ
- นักดนตรีที่ถูกเล็งมีป้ายตัวอักษรศัตรู `[A]` บนหัว ชี้ศัตรูแล้วมีเส้นบางๆ ไปหาเป้า แบบนี้ไม่ต้องวาดเส้นตลอดเวลาให้รก
- ไม่ควรโชว์ทิศของโน้ตล่วงหน้า (เก็บไว้เป็นความสามารถของ Bach ตามข้อ 5.9)

**สถานะ**
- ใช้ `Ui.Tag` เป็นตัวอักษร ไม่วาดไอคอน (ทีมเคยเจอว่าไอคอนดนตรีที่วาดเองดูแปลก และ `Motif.Initials` ก็มาจากเหตุผลเดียวกัน `Data/Motif.cs:46`) : `DIS 2` `CRE 3` `MUTE`
- กติกาเดียวทั้งเกม: **ทึบ = ไม่ดีกับตัวที่ติด, กรอบ = ดีกับตัวที่ติด** ไม่ว่าฝ่ายไหน
- ชี้แล้วขึ้น tooltip บรรทัดเดียวแบบ `DIS = lose HP each turn end`

**ลำดับการเล่นด้วยเมาส์**
1. เริ่มตา: ช่องของตัวนั้นในแถบสว่าง ตัวละครขยับออกมาข้างหน้า แถบล่างโชว์คำสั่งของคนนั้น
2. **คลิกศัตรูตรงๆ = โจมตีธรรมดาตัวนั้น** (1 คลิก) หรือคลิกปุ่มสกิลแล้วคลิกเป้า ถ้ามีศัตรูตัวเดียวหรือสกิลตีทั้งหมด ข้ามขั้นเลือกเป้า คลิกขวา = ย้อนกลับ
3. ปุ่มลัดคีย์บอร์ด: 1–4 คำสั่ง, Tab/A/D เลือกเป้า, Space = SIGNATURE, ESC หยุดเกม ไม่ต้องทำจอย (ผู้เล่นใช้เมาส์เป็นไม้ และเกมไม่รองรับทัชแพด)
4. ปล่อยเมาส์ → พักสั้นๆ (ราว 0.3–0.4 วิ ไม่ยืนยัน ต้องลองเล่น) → แถบล่างเลื่อนลงซ่อน ฉากหรี่ เคอร์เซอร์เปลี่ยนเป็นไม้บาตอง (แบบ `DuelScreen.cs:491`) โน้ตขึ้นที่ **จุดตวัดกลางจุดเดียว** (640, ~290) วงแหวนหดเข้า
   - การตวัดไม่ขึ้นกับว่าเมาส์อยู่ตรงไหน จุดกลางจึงช่วยให้สายตาอยู่ที่เดิม และมีที่ว่างพอสำหรับวาดหางไม้ตอนตวัด f ยาวเกิน 240 px
5. ตาศัตรู: กล่องท่าถัดไปขยายเป็นชื่อท่า เป้าถูกไฮไลต์ โน้ตวิ่งจากศัตรูทางขวาเข้าจุดกลาง แล้วมีเส้นต่อไปหาเป้า ระหว่างนั้นฉากอื่นหรี่และกล้องไม่ขยับ
   - ปัดสำเร็จขึ้นประกายแบบ Sequent Flare เพราะเรายังไม่มีเสียง ภาพต้องแทนเสียงทั้งหมด

**ตัวหนังสือให้น้อย**
- คำบนจอมีแค่ชื่อคำสั่ง ชื่อสกิล (≤ 12 ตัวอักษร) BREATH, SIGNATURE, รหัสสถานะ และ PERFECT / GOOD / MISS
- ที่เหลือใช้ตัวเลขกับเพชร คำอธิบายขึ้นเฉพาะตอนชี้ แบบ `X = Y`
- ตัวเลขใช้ `NumberText.Get` ข้อความท่าศัตรูเตรียมตอนศัตรูเลือกท่า (ใน Update) ไม่ใช่ใน Draw
- ให้ `TurnBattle` ส่งรายการเหตุการณ์ (โดนดาเมจ ลมเปลี่ยน ติดสถานะ เสียศูนย์) ออกมาให้หน้าจอเล่นแอนิเมชันและเตรียมข้อความตอนนั้น ตรงกับแนวของ skill game-ui-ux (HUD อัปเดตเมื่อมีเหตุการณ์ ไม่อ่านทุกเฟรม) และกติกาไม่มีโค้ดวาดใน Battle/

**ขาวดำ**
- แยกของด้วย ทึบ/กรอบ ความสว่าง ขนาด และการกะพริบ ใช้ Accent เฉพาะ "สิ่งที่ต้องทำตอนนี้": ช่องตาปัจจุบัน วงแหวน QTE และเส้นไปหาเป้า
- ห้ามใช้เฉดเทาแยกของ 3 ประเภทขึ้นไป (บทเรียนจาก Limbus)
- ตัวเลขสำคัญไม่เอียง ไม่หมุน (บทเรียนจาก P5)

---

## E. เรียงตามความคุ้ม (ผลมาก / แรงน้อย ก่อน)

| # | ข้อเสนอ | ผล | แรงที่ใช้ | อ้างอิง |
|---|---|---|---|---|
| 1 | จุดตวัดกลางจุดเดียว + ซ่อนแถบคำสั่ง + หรี่ฉาก + เปลี่ยนเคอร์เซอร์ตอนตวัด | สูง | ต่ำ (`NoteGlyph` เดิม) | E33 parry, DuelScreen.cs:491 |
| 2 | แถบลำดับตาด้านบน เรา = ทึบ ศัตรู = กรอบ ตัวปัจจุบันใหญ่ ศัตรูมี A/B ชี้แล้วสว่างทั้งสองที่ | สูง | ต่ำ–กลาง | HSR, Octopath, Steam E33, DD |
| 3 | แถบคำสั่งล่าง 4 ปุ่มใหญ่ + ปุ่มลัด + ลมติดปุ่ม + ราคาเป็นเพชร + ปุ่มจางเมื่อลมไม่พอ | สูง | ต่ำ (`Ui.Button` `Ui.Pips`) | E33 AP, P5 |
| 4 | คลิกศัตรู = โจมตีเลย, ข้ามเลือกเป้าเมื่อมีตัวเดียว, คลิกขวา = ย้อน | สูง | ต่ำ | STS |
| 5 | กล่องท่าศัตรู `9 ●●●` + ป้าย `[A]` บนหัวเป้า + เส้นตอนชี้ | สูง | กลาง | STS, SoS |
| 6 | ประกายเมื่อ PERFECT หรือปัดได้ (ภาพแทนเสียง) | กลาง–สูง | ต่ำ | SoS Sequent Flare, E33 |
| 7 | เสียศูนย์: แถบบาง + กลับสีเมื่อเต็ม + ขีดช่องในแถบลำดับตา | กลาง | ต่ำ | E33, HSR, Octopath |
| 8 | สถานะเป็นตัวอักษร ทึบ = ร้าย กรอบ = ดี + tooltip บรรทัดเดียว + การ์ดสอนครั้งแรก (`TeachNote`) | กลาง | ต่ำ | STS, DD2/Limbus |
| 9 | เงาพรีวิวดาเมจ/เสียศูนย์บนแถบตอนชี้เป้า (ค่า PERFECT ต้องตรงกับผลจริง) | กลาง–สูง | กลาง | HSR 2.3, Limbus |
| 10 | ดวลแรกเป็นบทสอน สอนทีละเรื่องต่อตา (โจมตี → ปัด → สกิลใช้ลม) แพ้แล้วลองใหม่ได้ | สูง | กลาง–สูง (ขั้น 9 ของแผน) | E33, STS |
| 11 | ปุ่มเร่งแอนิเมชัน x2 | ต่ำ–กลาง | ต่ำ | HSR |
| 12 | ตัวช่วยเป็นทางเลือก (ช่วงกว้างขึ้น หรือปัดอัตโนมัติแต่ได้ผลลดลง) | กลาง | กลาง | SoS Adamant Shard, E33 Auto QTE |

**ไม่แนะนำ**
- ลากไปใส่เป้า (ชนกับการตวัด `GestureReader.cs:21-22`)
- เลือกคำสั่งด้วยการตวัดเป็นวงล้อ (เสี่ยงอ่านผิด ขัดกับเป้า "SIMPLE")
- ตัวหนังสือเอียงแบบ P5 กับตัวเลขสำคัญ
- ใช้ Tiny/Label กับตัวเลขในการต่อสู้

**ต้องถามผู้ใช้ก่อน (เสนอเป็นข้อ 12 ขึ้นไปของ ALPHA7_DESIGN)**
1. จุดตวัดกลางจุดเดียว หรือวงแหวนที่ตัวเป้า (ข้อ 5.4 เขียนว่าโน้ตวิ่งเข้าหานักดนตรี)
2. ตัวช่วยข้อ 12 ถือเป็นการลดความยาก (CLAUDE.md : ห้ามลดความยากโดยไม่ถาม)
3. ใช้ `DIS` / `CRE` หรือคำเต็ม
4. ท่าศัตรูโชว์ทิศโน้ตล่วงหน้าไหม
5. มีปุ่ม x2 ไหม

---

**แหล่งอ้างอิง** (ลิงก์ทั้งหมดอยู่ในตัวข้อความด้านบน ส่วนนี้เพิ่มเฉพาะลิงก์ที่ยังไม่ได้ใส่)
- E33: [Maxroll beginners](https://maxroll.gg/clair-obscur-expedition-33/guides/getting-started-in-clair-obscur-expedition-33-beginners-guide), [Fextralife Controls](https://expedition33.wiki.fextralife.com/Controls)
- HSR: [Prydwen](https://www.prydwen.gg/star-rail/guides/combat-and-exploration)
- STS: [wiki.gg Exploder](https://slaythespire.wiki.gg/wiki/Exploder)
- Limbus: [Prydwen combat](https://prydwen.gg/limbus-company/guides/combat-mechanics)
- SoS: [PlayStation Blog](https://blog.playstation.com/?p=366730)
- Octopath: [TheGamer](https://www.thegamer.com/octopath-traveler-tips-to-know-before-you-start/)

ไฟล์ที่เกี่ยวข้อง:
- /home/user/Alphodite/docs/ALPHA7_DESIGN.md
- /home/user/Alphodite/Tacetno433/TacetGame.cs
- /home/user/Alphodite/Tacetno433/Core/GestureReader.cs
- /home/user/Alphodite/Tacetno433/Core/Ui.cs
- /home/user/Alphodite/Tacetno433/Core/NoteGlyph.cs
- /home/user/Alphodite/Tacetno433/Core/Palette.cs
- /home/user/Alphodite/Tacetno433/Core/TextSize.cs
- /home/user/Alphodite/Tacetno433/Screens/DuelScreen.cs
