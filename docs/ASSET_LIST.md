# TACET 4'33 — รายการไฟล์ภาพและเสียงที่ต้องทำ

อัปเดต : 23 กันยายน 2026 · ตอนนี้ **ยังไม่มีไฟล์ภาพและเสียงเลย** ทุกช่องในเกมเป็นกรอบว่าง / เงียบ
ใส่ไฟล์ตามชื่อด้านล่าง แล้วเพิ่มใน `Content/Content.mgcb` (MGCB Editor → Add Existing Item) เกมจะใช้เองโดยไม่ต้องแก้โค้ด
รายละเอียดท่าทาง ตำแหน่ง และขนาดแบบละเอียด : `Tacetno433/Content/Art/README.txt` และ `Tacetno433/Content/Audio/README.txt`

**กติกาทั่วไป** : ชื่อไฟล์ตัวพิมพ์เล็ก ช่องว่างและ ' กลายเป็น `_` · พื้นหลังโปร่งใส (PNG) ถ้ารูปไม่ใช่สี่เหลี่ยมเต็ม ·
เกมเป็น **ขาวดำ** (โทนเทา) ภาพสีได้แต่จะโดดจาก UI · ธีมภาพอ้างอิง Limbus Company Canto 10 / E.G.O Hollow

---

## ฝ่ายอาร์ต

### สรุปจำนวน

| หมวด | จำนวน | ลำดับความสำคัญ |
|---|---|---|
| นักดนตรี pixel มีแอนิเมชัน (idle / attack / hurt) | 9 คน × 3 ท่า | ⭐⭐⭐ เห็นตลอดการดวล |
| ศัตรู | 9 ภาพ | ⭐⭐⭐ ตอนนี้ศัตรูเป็นกรอบว่างกลางจอ |
| ฉากหลังดวล | 3 ภาพ (ยุคละ 1) | ⭐⭐ |
| คอนดักเตอร์ : ภาพเต็มตัว / หน้า / cut-in | 5 คน × 3 ภาพ = 15 | ⭐⭐ |
| นักดนตรี : ภาพเต็มตัว / หน้า | 9 คน × 2 ภาพ = 18 | ⭐⭐ |
| มือวาทยากร | 6 ท่า (มีแอนิเมชันได้) | ⭐ |
| ไม้บาตอง | 1 | ⭐ |
| แผงยุค (แนวตั้ง) / ภาพเส้นทาง (แนวนอนเล็ก) | 3 + 7 = 10 | ⭐ |
| ร้าน / ห้องพัก / เหตุการณ์ | 1 + 1 + 6 = 8 | ⭐ |
| **รวม** | **ภาพนิ่ง 70 ภาพ + ชุดแอนิเมชัน pixel 27 ชุด** | |

### 1. นักดนตรี pixel บนเวทีดวล ⭐⭐⭐
โฟลเดอร์ `Content/Art/Musicians/` · ชื่อ `<ชื่อ>_<ท่า>_<เลข>.png` เลขเริ่ม 0

| นักดนตรี | ยุค | เครื่องดนตรี | ไฟล์ |
|---|---|---|---|
| ANNA | Classical | Violin | `anna_idle_0.png` … `anna_attack_0.png` … `anna_hurt_0.png` … |
| KLARA | Classical | Flute | `klara_…` |
| OTTO | Classical | Timpani | `otto_…` |
| MALI | Siam | So Duang | `mali_…` |
| CHAI | Siam | Pi Nai | `chai_…` |
| NUAN | Siam | Ranat Ek | `nuan_…` |
| LUKA | Romantic | Cello | `luka_…` |
| IRIS | Romantic | Horn | `iris_…` |
| BORIS | Romantic | Bass Drum | `boris_…` |

- ท่า : `idle` ยืนหายใจ (วน) · `attack` เล่นบีตของตัวเอง (เล่นครั้งเดียว) · `hurt` โดน TACET ชนะบีตนั้น (เล่นครั้งเดียว)
- ท่าละไม่เกิน 12 เฟรม · มีแค่ `idle` ก็ใช้ได้ก่อน
- **หันขวา** (หันหา TACET) · เท้าอยู่แถวล่างสุดของภาพ · ทุกเฟรมของคนเดียวกันใช้ขนาด canvas เท่ากัน
- ขนาด : เกมขยาย ×3 แบบคม canvas ประมาณ **32 × 56 px** เต็มช่อง 100 × 170 พอดี
  (อยากได้รายละเอียดมากกว่านี้ บอกฝ่ายโค้ดให้เปลี่ยน `CharacterArt.PixelScale` เป็น 2 แล้วใช้ canvas ~50 × 85)
- เฟรม idle แรกถูกใช้ในที่นั่งหน้าเวที หน้าตั้งชื่อวง และตอนโค้งคำนับด้วย

### 2. ศัตรู ⭐⭐⭐
โฟลเดอร์ `Content/Art/Enemies/` · ขนาดในเกม 200 × 300 (Elite ×1.15, บอส ×1.3) วาดใหญ่กว่าได้ เช่น 400 × 600 · พื้นหลังโปร่งใส
ยืนอยู่หน้า "ดวงอาทิตย์ดำ" ที่โค้ดวาดให้ (อย่าวาดสุริยุปราคาเอง)

| ไฟล์ | ศัตรู | ชนิด | คำโปรย |
|---|---|---|---|
| `enemy_hush.png` | HUSH | ธรรมดา | The first thing to go is the echo. |
| `enemy_dead_air.png` | DEAD AIR | ธรรมดา | It waits between your notes. |
| `enemy_the_lull.png` | THE LULL | ธรรมดา | Soft, patient, and never finished. |
| `enemy_static.png` | STATIC | ธรรมดา | It sounds like something. It is not. |
| `enemy_the_mute_choir.png` | THE MUTE CHOIR | Elite | A hundred mouths, open, making nothing. |
| `enemy_white_noise.png` | WHITE NOISE | Elite | Every beat, all the time, forever. |
| `enemy_requiem.png` | REQUIEM | บอส Classical | The piece that was never finished. |
| `enemy_the_nameless_master.png` | THE NAMELESS MASTER | บอส Siam | He plays a phrase. You must answer better. |
| `enemy_the_devil_s_string.png` | THE DEVIL'S STRING | บอส Romantic | One string, one bow, one bargain. |

### 3. ฉากหลังดวล ⭐⭐
`Content/Art/Stage/` : `stage_classical.png` · `stage_siam.png` · `stage_romantic.png` — 1280 × 720 เต็มจอ
ครึ่งขวาจะถูกความมืดของ TACET ทับระหว่างสู้ ของสำคัญควรอยู่ฝั่งซ้าย · พื้นเวทีเริ่มประมาณ y 452 (เท้าวงอยู่ y 506–554)

### 4. คอนดักเตอร์ ⭐⭐
`Content/Art/Conductors/` — 5 คน : `the_apprentice` · `the_metronome` · `the_inferno` · `the_unhearing` · `the_folk_leader`

| ไฟล์ | ใช้ที่ | ขนาดแนะนำ |
|---|---|---|
| `portrait_<ชื่อ>.png` | ภาพในแกลเลอรี (302 × 402) · หน้าโปรไฟล์ (320 × 530) · โค้งคำนับตอนจบ | 600 × 900 |
| `face_<ชื่อ>.png` | หน้าโปรไฟล์ · แผง SIGNATURE ในดวล | 160 × 160 |
| `cutin_<ชื่อ>.png` | cut-in ท่า SIGNATURE (ช่อง 360 × 380 ทะลุแถบดำบนล่าง แบบ E.G.O ของ Limbus) | 720 × 760 |

### 5. นักดนตรี ภาพเต็มตัวและหน้า ⭐⭐
`Content/Art/Musicians/` — 9 คนตามตารางข้อ 1
- `portrait_<ชื่อ>.png` ภาพเต็มตัว : หน้ารับเข้าวง (160 × 236) และการ์ดรายชื่อ (~110 × 128) — วาดประมาณ 320 × 472
- `face_<ชื่อ>.png` หน้าสี่เหลี่ยม : แผงวงในดวล แผงรายละเอียด — 160 × 160

### 6. มือวาทยากร ⭐
`Content/Art/Hand/` : `hand_ready_0` · `hand_down_0` · `hand_left_0` · `hand_right_0` · `hand_up_0` · `hand_signature_0` (.png)
ช่อง 190 × 190 มุมซ้ายบนของหน้าดวล · ทำแอนิเมชันได้ด้วยเลข `_1 _2 …` (ท่าละไม่เกิน 8 เฟรม) · ทุกท่าใช้ canvas เท่ากัน

### 7. ไม้บาตอง ⭐
`Content/Art/Baton/baton.png` — วาด **ตั้งขึ้น** ด้ามอยู่ล่าง ปลายอยู่บน พื้นโปร่งใส canvas ประมาณ 24 × 236 (เกมยืดเป็นยาว 118 px และหมุนตามการแกว่ง)

### 8. แผงยุคและภาพเส้นทาง ⭐
- **แผงยุค** แนวตั้ง ยืดตามช่อง (วาดประมาณ 480 × 720) ด้านล่าง 150 px ถูกคำบรรยายทับ ของสำคัญไว้ด้านบน :
  `Content/Art/Eras/` : `era_classical.png` · `era_siam.png` · `era_romantic.png`
- **ภาพเส้นทาง (เปลี่ยน 28 ก.ย.)** หน้าเลือกเส้นทางไม่ใช้แผงสูงแล้ว ภาพไปอยู่ในกล่องรายละเอียด **แนวนอนเล็ก แสดง 230 × 142 (วาด 460 × 284)** :
  `Content/Art/Route/` : `route_battle.png` · `route_elite.png` · `route_boss.png` · `route_event.png` · `route_shop.png` · `route_rest.png` · `route_crossing.png`

### 9. ร้าน ห้องพัก เหตุการณ์ ⭐
- `Content/Art/Places/shop.png` 672 × 470 (มีคนขายของในภาพ) · `Content/Art/Places/rest.png` 960 × 236 (แถบกว้างเหนือตัวเลือก)
- `Content/Art/Events/` 520 × 430 : `event_the_broken_metronome.png` · `event_a_street_musician.png` · `event_the_silent_audience.png` ·
  `event_a_lost_score.png` · `event_tacet_s_echo.png` · `event_the_tea_house.png`

### ยังไม่มีช่องในเกม (ถ้าอยากได้ บอกฝ่ายโค้ดเพิ่มช่องก่อน)
- ไอคอน Motif 21 อัน (ตอนนี้โค้ดวาดเป็นเครื่องหมายโน้ตในกรอบเพชร)
- ภาพพื้นหลังหน้าแรก / หน้าแกลเลอรี / Curtain Call (ตอนนี้เป็นกราฟิกโค้ด)
- ไอคอนเกม (`Icon.ico` / `Icon.bmp` ตอนนี้เป็นของ MonoGame) · โลโก้ · key art สำหรับหน้าร้าน/พรีเซนต์

### ของที่โค้ดวาดเองและจะคงไว้ (ไม่ต้องทำเป็นภาพ)
ความมืดของ TACET และขอบรอยแยกแสง · สุริยุปราคา · วงแหวนโน้ต · เส้นแสง ประกายไฟ ระลอกน้ำตอนปะทะ ·
ไอคอน Motif · สัญลักษณ์ตระกูลเครื่องดนตรี · แผนภาพหน้าสอนเล่น · การ์ด MOVEMENT ตอนเปิดชั้น

---

## ฝ่ายเสียง

### 1. ทำนองดวล ⭐⭐⭐ (สำคัญที่สุด)
ดวลไม่มีเพลงยาว เสียงดนตรีเกิดเมื่อผู้เล่นตวัด **ทีละโน้ต ทีละบีต**
- อัดทำนองสั้นๆ 1 ท่อน (8 โน้ต หรือ 4 โน้ตที่วนซ้ำ) แล้วตัดเป็นไฟล์ละโน้ต แต่ละไฟล์สั้นกว่า 1 วินาที คีย์เดียวกัน
- `Content/Audio/Phrase/answer_1.wav` … `answer_8.wav` = วงของเราตอบ
- `Content/Audio/Phrase/call_1.wav` … `call_8.wav` = TACET เล่นก่อน (ควรฟังดูเป็นอีกฝ่าย ทุ้ม/หลอน)
- เกมเล่นดังขึ้นตอนตวัดใหญ่ (f) เบาลงตอนตวัดเล็ก (p) เพี้ยนนิดๆ ตอน MISS และเงียบตอนลังเล/พัก (รอบ 12 ไม่มีคำ BOOST / EASE ในเกมแล้ว ชื่อไฟล์ `qte_boost` `qte_ease` ใช้ต่อได้)
- (รอบ 6, รอบ 9 เร่งเป็น 96–128 BPM) ดวลตวัดทุกบีตต่อเนื่อง **แต่ละโน้ตควรสั้น ~0.25 วิ** ไม่งั้นจะทับกัน (ที่ 128 BPM บีตห่าง 0.47 วิ ประกายของโน้ตคู่มาหลังครึ่งบีต 0.23 วิ) · โน้ตตัวหลังของโน้ตคู่ใช้ไฟล์เดียวกันแต่เสียงสูงขึ้น

### 2. เสียงประกอบ (SFX) — `Content/Audio/Sfx/<ชื่อ>.wav` รวม 37 เสียง
**ชุดแรก 12 เสียงที่หน้าดวลใช้ (ทำก่อน)** : `beat_tick` · `note_on` · `qte_normal` · `qte_boost` · `qte_ease` · `qte_perfect` ·
`qte_miss` · `qte_hesitate` · `clash_win` · `clash_lose` · `clash_even` · `combo_up`

ที่เหลือ : `ui_move` `ui_confirm` `ui_back` `ui_denied` `path_chosen` `seat_pickup` `seat_drop` `seat_remove` `note_off`
`round_start` `enemy_boost` `enemy_ease` `stamina_empty` `rest_recover` `victory` `defeat` `recruit` `combo_break`
`motif_get` `buy` `page_turn` `check_pass` `check_fail` `rehearse` `run_complete`
(ความหมายแต่ละเสียงอยู่ใน `Content/Audio/README.txt`)

รอบ 6 ใช้เสียงเดิมเพิ่ม ไม่ต้องทำไฟล์ใหม่ : `beat_tick` = นับเข้า + FINALE + **เครื่องวัด STROKE TIMING ในหน้าตั้งค่า** (ต้องสั้นและคม) ·
`note_on` = เสียงรัวของ TREMOLO ฝั่ง TACET · `qte_normal` = ทุกครั้งที่รัวไม้ (เสียงสูงขึ้นเรื่อยๆ) · `clash_win` + `enemy_boost` = COUNTER ·
`combo_up` = FORTISSIMO / FINALE · `page_turn` = กล่องถามยืนยันเปิด

### 3. เพลง (วนซ้ำ) — `Content/Audio/Music/<ชื่อ>.ogg` รวม 11 เพลง
`title` · `gallery` · `route` · `battle` · `boss` · `victory` · `defeat` · `shop` · `event` · `rest` · `ending`
- `battle` และ `boss` เล่นตลอดการต่อสู้ (หน้าเวที และทุกรอบดวล · รอบ 12 ไม่มีหน้าโน้ตแล้ว) ไม่เริ่มใหม่ **ควรเบา เป็นบรรยากาศ ไม่มีจังหวะกลองชัด**
  เพราะทำนองมาจากการตวัด (ข้อ 1)
