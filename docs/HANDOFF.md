# TACET 4'33 — เอกสารส่งต่อให้ฝ่ายโค้ด

อัปเดต : 6 ตุลาคม 2026 (รอบ 15 · ส่งงาน 29 ก.ย. · นำเสนอ 30 ก.ย.) · repo `github.com/JuzJuz03HKR/Alphodite` branch **`Alpha6`** (รอบ 15 · `Release` = zip ตัวที่ส่งอย่างเดียว · `Alpha5` = รอบ 14 · `Alpha4` = รอบ 12–13 · `Alpha3` = รอบ 9–11b · รอบ 6–8 อยู่ `Alpha2`) · กติกาสำหรับ Claude อยู่ `CLAUDE.md` · อ่านคู่กับ [`PROJECT_STATUS.md`](PROJECT_STATUS.md) (รายละเอียดทุกระบบ)
และ [`ASSET_LIST.md`](ASSET_LIST.md) (ไฟล์ภาพ/เสียงที่ต้องใส่) · สิ่งที่ผู้ใช้สั่งคำต่อคำ [`USER_BRIEFS.md`](USER_BRIEFS.md) · prompt หลัง compact [`RESUME_PROMPT.md`](RESUME_PROMPT.md) · เนื้อหาสไลด์ [`TACET_Slides_29Sep.md`](TACET_Slides_29Sep.md) · แผนภาพลูป `diagrams/core_game_loop.png` · `gameplay_loop.png` (+ `*_detail.png`)

---

## 1. สรุปใน 1 นาที

- MonoGame DesktopGL 3.8 · .NET 9 · เปิด `Tacetno433.sln` แล้วกด F5 · build ต้องได้ **0 error 0 warning**
- เล่นได้ครบลูป : เมนู → เลือกคอนดักเตอร์ → รัน 3 ชั้น (เส้นทาง / ดวล / ผลลัพธ์ / ร้าน / เหตุการณ์ / พัก) → Curtain Call (รอบ 12 ไม่มีหน้าโน้ต · **รอบ 14 ไม่มีหน้าเวทีก่อนดวล**)
- ดวล = ใช้เมาส์เป็นไม้บาตอง กดค้างแล้วตวัดตามท่า 4/4 **ทุกบีตต่อเนื่อง** · **รอบ 15 : ตัวอักษรบนโน้ตบอกขนาดตวัด (p เล็ก · mf กลาง · f ใหญ่ ผิดขนาดพลังลด) · ทั้งวงบนเวทีเล่นทุกโน้ต · ลม = เลือด** ·
  นักดนตรีมีลูกศร CUE ตามฝั่งที่นั่งจริงในวง (ไม่มีตัวอักษร PART / COST แล้ว) · 3 รอบเล่นต่อกันในหน้าดวล (รอบ 14) · โหมด NORMAL / MAESTRO ·
  โน้ตเขียน f / mf / p มีหัวลูกศรที่ขอบบอกทิศ โน้ตถัดไปสว่าง · โน้ตพิเศษมาทีละชั้น : TREMOLO (ชั้น 1 ทุกศัตรู · Elite/บอสทุกชั้น) · FERMATA (ชั้น 2) · โน้ตคู่ (ชั้น 2 ตั้งแต่รอบ 9) ·
  COUNTER · FORTISSIMO · FINALE (รอบ 6–6.3, 25 ก.ย.)
- Esc = เมนูหยุดทุกหน้าในรัน · มีเซฟตั้งค่า + เซฟรัน + CONTINUE · STROKE TIMING (calibration) ในหน้าตั้งค่า
- **รอบ 7** : ภาพและความรู้สึก (หลุมจุดตี · EARLY/LATE · แสงแตกตามเกรด · เลนสว่าง · โน้ต TACET แตก · บรรทัดห้าเส้นสั่น · ม่านเวทีแทนเฟดดำ) ·
  Motif ใหม่ 6 ชิ้นที่ดัดโน้ตพิเศษ · Motif FERMATA → BREATH MARK · FORTISSIMO ไม่เสียสตามิน่า
- **รอบ 8** : **สตามิน่าเป็นตัวกดดัน** (BOOST/รัว/ค้างฟรี · บีตที่ TACET ชนะ = TACET ซัดสตามิน่า · หมด = แพ้) · ศัตรู ×1.5 ชดเชย ·
  โน้ตบินเข้าใส่วง + แสงดาบ BOOST + ขอบจอบีบตอนลมน้อย · ม่านเฉพาะหน้าเลือกคอนดักเตอร์ คลื่นความเงียบหน้าอื่น · **โหมด TUTORIAL 12 บท**
- **รอบ 9** (หลังเพลย์เทส 5 คน) : **ดาเมจเบาลง** (`PushPerPower` 1.0 · PUSH CAP 10 ต่อโน้ต · HEAVY LINE Elite/บอส) ดวลยาวขึ้น ~3 เท่า เล่นโถมบีตแรกๆ ไม่ชนะง่ายอีก ·
  **ตวัดถี่ขึ้น** BPM 96/112/128 · โน้ตคู่ชั้น 2 · ชั้น 1 ทุกศัตรูรัว · ตัวจำลองนับ Motif (`Core/DebugShots.cs` `TakeMotif` `FrontLoad`)
- **รอบ 10** : ตรวจสมดุลด้วย **`--shots audit`** (`Core/DebugShots.Audit.cs`) · METRONOME ไม่มีส่วนลดสตามิน่า + สตามิน่า 7 · UNHEARING +130% + Push 4 ·
  BREATH MARK ×1.25 · `BlowPerPower` 0.63 · คอนดักเตอร์ทุกคน 50–64% (เก่ง) ความยากรวมเท่าเดิม
- **รอบ 11a** : WHITE NOISE โน้ต 2 + NO REST คืนครึ่ง · ศัตรูธรรมดาแรงขึ้น (`NormalScale` 3.1, HUSH/THE LULL โน้ตหนักขึ้น) ชนะผู้เล่นได้บ้างแล้ว ·
  `BlowPerPower` 0.68 · METRONOME สตามิน่า 9 · FOLK LEADER +7% · Motif : REED CASE 2 · GRACE NOTE ×3 · **ACCELERANDO รัวนับ 2 · TENUTO ค้างครบคืน 8 · MARCATO COUNTER ดันถึง 20** ·
  ทั่วไป ~8% เก่ง ~63%
- **รอบ 11b (REPEATS)** : แผน 1 ชุด **เล่นวน 1 / 2 / 3 ครั้งในรอบ 1 / 2 / 3 ต่อเนื่องไม่หยุด** (TACET เล่นบีตเดิม สุ่ม f/mf/p ใหม่ทุกรอบวน) ·
  บีตพักคืน 6 → 10 · PUSH CAP 8 · HEAVY LINE 0.7/0.6 · `BlowPerPower` 0.78 · METRONOME 10/6 · ดวลไร้ที่ติ ธรรมดา 16 บีต Elite 23 บอส 31.5 ·
  ทั่วไป ~9% เก่ง ~67% · คอนดักเตอร์ 64–68% (เก่ง)
- **รอบ 12 (27 ก.ย., `Alpha4`) — ลบหน้า SCORE** : ไม่มีการวางแผนจังหวะแล้ว · **DYNAMICS** ขนาดตวัดเลือกแถวที่เล่น คนที่เล่นจ่ายสตามิน่า ·
  **CUE** ไม้ชี้ฝั่งไหน (ลง/ขึ้น กลาง · ซ้าย · ขวา) คนฝั่งนั้น ×1.5 · **IN TUNE** ขนาดตรงเครื่องหมายจริงของ TACET = โน้ตมันเหลือ 60% · บีตเงียบไม่ตวัด = พัก +12 ·
  หน้า STAGE มีแถบท่อน TACET + ฝั่งที่ไม้ชี้ + p/mf/f ของแต่ละแถว · ที่นั่งเริ่มแนวทแยง · MUTE CHOIR ปิดเสียงฝั่ง · REQUIEM กลับเวที ·
  ทั่วไป ~7–8% เก่ง ~68–70% · ตวัดใหญ่ทุกโน้ต 46–51% (อ่านโน้ตดีกว่า)
- **รอบ 12.1 (28 ก.ย.)** : **ตวัดเล็กบนบีตเงียบ = พัก** (`BattleState.RestsOn`) · **เส้นแสง CUE** จากวงตอบไปหาคนที่ถูกชี้ + ชื่อฝั่งเหนือหัว (`DuelScreen.Stage.DrawCueBeam`) ·
  **Tutorial บท 5 WHERE THEY SIT** (`TutorialScreen.Seats.cs`, รวม 13 บท) · ตวัดกลาง 100–240 px (`GestureReader.BigLength` 240) · NAMELESS ท่อนใหม่ · SECOND WIND 0.15 ·
  ทั่วไป ~9–10% เก่ง ~71–74% (ง่ายลงเล็กน้อยจาก NAMELESS)
- **รอบ 12.2 (28 ก.ย.)** : **หน้าเลือกเส้นทาง = ถนนของชั้น + ทางแยก + กล่องรายละเอียด** (`RouteScreen.cs` ไม่ใช้ `PanelStrip` แล้ว หน้าเลือกยุคยังใช้) ·
  **METRONOME** วงเล่นตามตัวอักษรบนโน้ต (`BattleState.MarkedChoice`, จ่าย ×1.33) · **UNHEARING** พักได้ลมเพิ่มเมื่อโดนดันถอย (`CloserRestMax`) · FOLK LEADER Push 5 ·
  หน้าโปรไฟล์บอก SIGNATURE + สูตร · คอนดักเตอร์ทุกคน (เก่ง) 68–79%
- **รอบ 13 (28 ก.ย.)** : **คอนดักเตอร์ทุกคนมี PASSIVE + SIGNATURE ของตัวเอง** (`ConductorPerk` + `SignatureMove`) · SIGNATURE อยู่ 4 ตวัด · โน้ตสูตรจาก PERFECT ·
  APPRENTICE BY THE BOOK / ABSOLUTE PITCH · METRONOME LOCKED TEMPO / CLOCKWORK · INFERNO RUNAWAY FIRE / SET ALIGHT · UNHEARING THE CLOSER THE LOUDER / DEAF EARS · FOLK LEADER EVERY ROAD HOME / VILLAGE BAND ·
  ตัวจำลองใช้ SIGNATURE แล้ว · `BlowPerPower` 0.95 ชดเชย · คอนดักเตอร์ทุกคน (เก่ง) 70–75% · ไม่กด Space เลย เก่ง ~64–68% ·
  หน้าโปรไฟล์ = PASSIVE / SIGNATURE / สูตร + ลูกศรเปลี่ยนคน · แก้บั๊กลูกศรแกลเลอรี (คลิกแล้วเปิดโปรไฟล์คนถัดไป)
- **รอบ 14 (28 ก.ย., `Alpha5`)** : **ลบหน้า STAGE (`FormationScreen`)** ผู้ใช้บอกว่าไม่จำเป็น ไม่ดึงดูด อธิบายยาก · นักดนตรีทุกคนมี **PART** = ตัวอักษร + ลูกศร วาดแบบโน้ต
  (`MusicianArt.PartBadge`) · ตัวอักษรตามตระกูล (`StageLayout.PartOf`) · ลูกศร `Musician.Cue` · ที่นั่งประจำ `StageLayout.HomeSeat` (9 คน = 9 PART) · กฎสำรอง : ไม่มีคน p → ตวัดเล็กเรียกส่วนที่เบาสุด ·
  ดวลเล่น 3 รอบในหน้าเดียว (`DuelScreen.SetUpRound` · ป้ายบอกสิ่งที่เปลี่ยน · การ์ดกลไกศัตรูครั้งแรก · ดีล DEVIL ในดวล) · หน้า THE BAND (`BandScreen`) เลือกใครขึ้นเวที ·
  ค่าตัวคูณแถวย้ายเข้าการ์ด · MUTE CHOIR ปิดตัวอักษร · REQUIEM สลับลูกศร · KLARA ×2 ใส่โน้ต p · ANNA / IRIS บนบีตลูกศรตัวเอง · ร้านบอกคนจ้าง · event THE EMPTY CHAIR ·
  SPARE STICKS ค่า −1 · จูน FOLK LEADER / UNHEARING / WHITE NOISE · เก่ง ~71–74% ทั่วไป ~8–9% (เท่ารอบ 13)
- **รอบ 15 (29 ก.ย., `Alpha6`)** : **โน้ตบอกขนาดตวัด** (ผิดขนาดเหลือ 30%) · **ทั้งวงเล่นทุกโน้ต** (`BattleState.Plays`) · **ตวัดไม่เสียสตามิน่า** ลม = เลือด (เสียเมื่อ TACET ชนะบีต/MISS) ·
  เส้นดันคงเดิม · ผังเวทีแบบวงจริง (สายหน้า เป่ากลาง ตีหลัง, ลูกศร = ฝั่งจริง) · ไม่มี COST · ไล่ระดับ BPM ตามชั้น + 2 ดวลแรกมีแค่ f/p + แรงศัตรูตามชั้น 0.85/1.45/2.45 ·
  ทั่วไป ~21–24% เก่ง ~76–79% · Tutorial/HOW TO PLAY เขียนใหม่ในก้อน 2
- **ยังไม่มีไฟล์ภาพและเสียงเลย** ทุกช่องเป็นกรอบว่าง / เงียบ ใส่ไฟล์ตามชื่อแล้วขึ้นเองโดยไม่ต้องแก้โค้ด
- เพื่อนต่างกลุ่มเล่นระบบรอบ 5 แล้ว (ง่ายเกิน) · **เพลย์เทส 25 ก.ย.** 5 คน เล่น build รอบ 8 : ชอบการตวัด · เข้าใจยาก · ง่ายไป (แก้แล้วรอบ 9) ·
  **รอบ 9–14 ยังไม่มีคนเล่นจริง** ตัวเลขได้จากการจำลอง
- **ข้อเสนอที่รอผู้ใช้เลือก** (ให้เล่นสนุกขึ้น · รีวิวทั้งเกม · สอนเล่น ก–ซ · ต่อยอดรอบ 14) อยู่ `PROJECT_STATUS.md` หัวข้อ 0 · ข้อเสนอหน้า STAGE ทำแบบ 2 แล้วรอบ 14

## 2. ความคืบหน้า (ประมาณการ)

| ส่วน | % | หมายเหตุ |
|---|---|---|
| ระบบหลักของรัน (เส้นทาง วง ดวล ผลลัพธ์ Motif ร้าน เหตุการณ์ พัก จบรัน) | 90% | ครบวง ขาดการจูนหลังเล่นจริง · รอบ 12–14 เปลี่ยนแกนดวลและวง ต้องเล่นจริงก่อน |
| ระบบดวล (ตวัดต่อเนื่อง โน้ตบอกขนาดตวัด ทั้งวงเล่น ลม = เลือด CUE IN TUNE โน้ตคู่ TREMOLO COUNTER FORTISSIMO FINALE ความสามารถ กลไก SIGNATURE) | 85% | สมดุลด้วยตัวจำลองแล้ว (รอบ 15 ก้อน 8 · NORMAL / MAESTRO) · มีแค่รีวิวสั้นจากเพื่อน ยังไม่มีเพลย์เทสแบบมีชุดคำถาม |
| หน้าจอ UI 19 หน้า (รวม TUTORIAL) + เมนูหยุด + กล่องถามยืนยัน | 90% | Tutorial 7 บท (6 บทพื้นฐาน + READY) + EXTRAS โน้ตพิเศษ (TAB) · HOW TO PLAY 3 หน้า · สอนตอนเจอครั้งแรกในดวล · สวิตช์ NORMAL / MAESTRO · รอบ 14 : STAGE → THE BAND |
| ระบบรอบนอก (ตั้งค่า เซฟ CONTINUE calibration ปลดล็อก) | 75% | ครบยกเว้นปลดล็อกถาวร · เซฟยังไม่จำจังหวะหน้าผลลัพธ์/Motif |
| ช่องใส่ภาพ/เสียง (hooks) | 100% | ครบทุกหน้า รายการใน `ASSET_LIST.md` |
| เนื้อหา (ตัวละคร ศัตรู เหตุการณ์ Motif) | 40% | พอเล่นได้ ยังน้อยสำหรับเกมเต็ม |
| ภาษาไทย | 0% | ฟอนต์ที่ build ไว้มีแต่ตัวอังกฤษ |
| การทดสอบ / บาลานซ์ | 55% | simulate + audit (นับ Motif, นิสัยขนาดตวัด) · เพลย์เทส 5 คน (build รอบ 8) · รอบ 15 มีแค่รีวิวสั้นจากเพื่อน (29 ก.ย.) · ความยากยืนยันด้วยตัวจำลองเท่านั้น |
| **รวมเฉพาะระบบเกม (โค้ด)** | **~85%** | |
| **รวมทั้งโปรเจกต์ (รวมอาร์ต เสียง เนื้อหา)** | **~53%** | อาร์ตและเสียงยัง 0% |

## 2.5 กติกาออกแบบ (ตกลงกับผู้ใช้ 25 ก.ย. — อ่านก่อนเพิ่มอะไรในดวล)

0. **ผู้ใช้ต้องการให้เกม SIMPLE เข้าใจง่ายก่อน** (25 ก.ย.) ตวัดดีแล้ว อย่าเพิ่มของที่ทำให้การอ่านโน้ตยากขึ้น
1. ชนิดโน้ตล็อกไว้ 4 แบบ : ตวัด (วงกลม + f/mf/p + หัวลูกศรที่ขอบ) · ประกาย ✦ · FERMATA · TREMOLO — **ห้ามเพิ่มชนิดใหม่โดยไม่คุยกับผู้ใช้** (รอบ 12 ไม่ได้เพิ่ม)
2. โน้ตพิเศษมาทีละชั้น : TREMOLO ชั้น 1 (ชั้น 1 ทุกศัตรู · Elite/บอสทุกชั้น) · FERMATA ชั้น 2 · โน้ตคู่ชั้น 2 (รอบ 9) (`BattleRules` Teaching Order)
3. ความหลากหลายใหม่ให้มาจาก Motif / นักดนตรี / กลไกศัตรู ที่ดัดโน้ต 4 แบบนี้ (ไม่ต้องมีหน้าจอหรือกติกาใหม่)
4. มีสิ่งที่ต้องทำชัดๆ ทีละอย่าง : โน้ตถัดไปสว่าง ที่เหลือจาง · คำเด้งเก็บไว้เฉพาะช่วงสำคัญ
5. ก่อน commit : build 0 warning · `--shots simulate` · `--shots savecheck` ผ่านครบ

## 3. โครงสร้างโค้ด (ที่ต้องรู้ก่อนแก้)

```
Tacetno433/
├─ TacetGame.cs        เริ่มเกม โหลดฟอนต์ เสียง ภาพ เตรียมข้อความ · วาดทั้งเกมลงภาพ 1280x720 แล้วยืดใส่จอ
├─ Core/               เครื่องมือวาด ชุด UI อ่านเมาส์/คีย์ ช่องภาพ (ArtBank ArtSlot CharacterArt HandArt) เอฟเฟกต์ (Hollow TacetField StageStaff)
│                      กล่องถาม (ConfirmBox) · ไม้บาตอง (Baton) · สัญลักษณ์โน้ต (NoteGlyph) · เปลี่ยนหน้า (Curtain SilenceWave SlantWipe) · เครื่องมือทดสอบ (DebugShots .Audit)
├─ Data/               ข้อมูลทั้งหมด — แก้ตัวเลข/เนื้อหาที่นี่ (BattleRules = ตัวเลขบาลานซ์ทุกตัว) · SaveFile (เซฟ 2 ไฟล์)
├─ Battle/             กติกาการต่อสู้ BattleState (ไม่มีโค้ดวาด) + DuelEffects (เอฟเฟกต์แบบ object pool)
├─ Screens/            หน้าจอ 19 หน้า + PauseMenu (DuelScreen แยก 7 ไฟล์ · TutorialScreen แยก 3 ไฟล์ แบบ partial) · รอบ 12 ลบ ScoreScreen
└─ Audio/SoundBank.cs  เสียงทั้งหมด
```

| อยากแก้อะไร | ไปที่ |
|---|---|
| ตัวเลขบาลานซ์ทุกตัว | `Data/BattleRules.cs` |
| **โน้ตบอกขนาด / ทั้งวงเล่น (รอบ 15)** | ใครเล่น `BattleState.Plays` (ทุกคนบนเวที) · ขนาดที่โน้ตขอ `AsksSize` / `SizeFree` / `SizeRight` · ผิดขนาด `BattleRules.WrongSizePower` · พลัง `PowerFor` · ค้น `THE NOTE SAYS` · ป้าย TOO BIG / TOO SMALL `DuelScreen.Baton.JudgeStroke` |
| **ที่นั่งแบบวงจริง (รอบ 15)** | กลุ่มเครื่อง = แถว `StageLayout.Rows` / `SectionOf` (สายหน้า เป่ากลาง ตีหลัง) · ฝั่ง `Musician.Cue` · ที่นั่งประจำ `StageLayout.HomeSeat` · ป้ายลูกศรในวงกลม `MusicianArt.SideBadge` (การ์ด · หน้ารับคน · ร้าน · หน้า THE BAND · เหนือหัว · กล่องวง) · ชื่อกลุ่มเป็นคำ `StageRow.Name` / `Short` |
| **ผังยืนในดวล (มองจากข้างเวทีเยื้องบน)** | ตำแหน่ง/ขนาด `StageLayout.DuelColumnX` `DuelDepthY` `DuelDepthX` `DuelDepthScale` → `DuelStandRect` · วงรีบนพื้น + ชื่อกลุ่ม `DuelScreen.Stage.cs` `DrawFloorSpots` |
| ป้าย Motif (ตัวอักษรย่อ) | `Motif.Initials` (สร้างใน `MotifList.PrepareText` ตั้งเองได้ถ้าซ้ำ เช่น COUNTERPOINT = CP) · วาด `Core/MotifArt.cs` `Mark` |
| **ไล่ระดับความยาก (รอบ 15)** | BPM `BattleRules.TempoByFloor` · ดวลแรกๆ แค่ f/p `GentleFights` / `BattleState.Gentle` · แรงศัตรูตามชั้น `FloorPower` · โน้ตสุดท้ายของรอบ `BattleState.PrepareRound` (Last Note) |
| **ลูกศรที่ไม้ตวัดไป (CUE)** | `BattleRules.CuePower` `CueSide` · `BattleState.CueSideAt` / `IsCued` · ลูกศรของที่นั่ง `StageLayout.SeatSide` · ค้น `CUE` |
| **IN TUNE / COUNTER** | `BattleState.Resolve` (ค้น `IN TUNE`) · `BattleRules.InTuneKeep` `CounterKeep` · ป้ายในดวล `DuelScreen.Hud.cs` `DrawJudge` |
| **พัก (บีตเงียบไม่ตวัด)** | `DuelScreen.UpdateAnswer` (No Stroke) · `BattleState.IsSilent` / `SilentRecover` |
| **หน้า THE BAND (รอบ 14, แทนหน้าเวที)** : ใครขึ้นเวที/นั่งพัก · แผนผัง PART 3×3 | `Screens/BandScreen.cs` · ใครอยู่บนเวที `Data/Formation.cs` (`AutoSeat` `Bench` `Swap` `Tidy`) · เปิดจาก `RouteScreen` (TAB) |
| **ระหว่างรอบในดวล (รอบ 14)** : ป้ายบอกสิ่งที่เปลี่ยน · การ์ดกลไกศัตรูครั้งแรก · ดีล DEVIL | `DuelScreen.SetUpRound` / `LeaveRound` / `UpdateBargain` · วาด `DuelScreen.Hud.cs` `DrawRoundNote` `DrawBargain` |
| คนสว่างตามขนาดตอนตวัด · ป้าย PART เหนือหัว (คนที่ถูกชี้ใหญ่ขึ้น) · กากบาท SILENCED | `DuelScreen.Stage.cs` (`DrawMusicianGlow` `DrawMusicianMarks`) · `DuelScreen.LiveSize` |
| กลุ่มเครื่องที่ถูกปิดเสียง (MUTE CHOIR) · ฝั่งสลับ (REQUIEM) | `BattleState.SilencedSection` / `CanPlay` / `PickSilencedSection` · `Mirrored` (ค้น `SILENT MOUTHS` `UNFINISHED`) |
| ร้านบอกคนที่จะจ้าง · event THE EMPTY CHAIR | `ShopScreen` (`hire`) · `RunState.Recruit` / `MissingSectionPlayer` · `Reward.RecruitPart` |
| นักดนตรี / ความสามารถ | `Data/Musician.cs` (+ ผลของความสามารถใน `Battle/BattleState.cs` ค้นชื่อความสามารถ เช่น `MOMENTUM`) |
| ศัตรู / กลไก / ชั้นที่เริ่มใช้กลไก | `Data/Enemy.cs` (+ ค้นชื่อกลไกใน `BattleState.cs`, `DuelScreen*.cs`) |
| คอนดักเตอร์ / สูตร SIGNATURE | `Data/Conductor.cs` |
| Motif / เหตุการณ์ | `Data/Motif.cs` / `Data/GameEvent.cs` |
| ความรู้สึกการตวัด (เริ่ม/หยุด/ขนาด) | ค่าคงที่บนสุดของ `Core/GestureReader.cs` |
| ลำดับหน้าหลังจบแต่ละด่าน · เดินเข้าด่าน · CONTINUE | `Screens/RunFlow.cs` (`NextStage` / `Enter` / `Continue`) |
| เซฟ (ไฟล์อยู่ `%AppData%\TACET433\`) | `Data/SaveFile.cs` — **ห้ามสลับลำดับรายการใน `MusicianList` `MotifList` `EnemyList` `EventList` `ConductorList`** เซฟเก็บเป็นลำดับ |
| เมนูหยุด / หน้าไหนกด Esc แล้วเปิดเมนู | `Screens/PauseMenu.cs` · `GameScreen.CanPause` / `UsesEscape` / `Paused` / `Resumed` ใน `Core/ScreenManager.cs` |
| FERMATA (โน้ตค้าง) | กติกา `BattleState` (ค้น `FERMATA`, `HoldFraction`) · หน้าจอ `DuelScreen.Notes.cs` (`StartHold` / `UpdateHold` / `FinishHold`) |
| หน้าตาโน้ตในเลน | `DuelScreen.Stage.cs` (`DrawIncoming` · `Focus` โน้ตไหนสว่าง) · หัวลูกศร ✦ เฟอร์มาตา แถบรัว อยู่ `Core/NoteGlyph.cs` (`Pointer` `Spark` `FermataSign` `RollBar`) |
| ชั้นที่โน้ตพิเศษเริ่มมา | `BattleRules.TremoloFromFloor / FermataFromFloor / PairsFromFloor` · ใครรัว/ใครค้าง `BattleState.PrepareRound` |
| ดาเมจ / เพดานต่อโน้ต / Elite-บอสดันยาก | `BattleRules.PushPerPower` `PushCap` `EliteLine` `BossLine` · ใช้ที่ `BattleState.PushFor` จุดเดียว (ค้น `PUSH CAP`, `HEAVY LINE`) |
| ความสามารถคอนดักเตอร์ (ตัวเลข) | `BattleRules` หัวข้อ Conductor Perks · ค่าพลัง/สตามิน่าใน `Data/Conductor.cs` |
| โน้ตคู่ TREMOLO COUNTER FORTISSIMO | กติกา `Battle/BattleState.cs` (ค้น `TREMOLO` `COUNTER` `FORTISSIMO` `ResolveGrace`) · หน้าจอ `Screens/DuelScreen.Notes.cs` |
| FINALE | `Screens/DuelScreen.Finale.cs` + `BattleState.FinaleOffered / WinFinale / FailFinale` |
| calibration | `Settings.TimingOffset` ใช้ใน `DuelScreen.StrokeClock()` จุดเดียว · หน้า TEST ใน `Screens/SettingsScreen.cs` |
| เอฟเฟกต์ที่จุดตี (แสงแตก โน้ตแตก) | `Battle/DuelEffects.cs` (`SpawnHit` / `SpawnShatter` + `DrawHits` / `DrawShards`) · เรียกจาก `DuelScreen.StartClash` |
| หลุมจุดตี · เลนสว่าง · ขอบเลนเต้น | `DuelScreen.Stage.cs` `DrawLane` (ค้น `Hit Well`, `LANE FLASH`) |
| EARLY / LATE | `DuelScreen.Baton.cs` `ShowTiming` · วาดใน `DuelScreen.Hud.cs` `DrawJudge` |
| บรรทัดห้าเส้นหลังวง | `Core/StageStaff.cs` · ความแรงคือ `staffEnergy` ใน `DuelScreen.cs` |
| ตัวเปลี่ยนหน้า | `Core/ScreenManager.cs` เลือก : ม่าน `Core/Curtain.cs` เมื่อหน้านั้น `UsesCurtain` (หน้าเลือกคอนดักเตอร์) · คลื่น `Core/SilenceWave.cs` เมื่อ `EntersWithWave` (ดวล · Curtain Call) · อื่นๆ แถบดำเฉียง `Core/SlantWipe.cs` (รอบ 15) · ความเร็ว `FadeSpeed` (2.2 = ครึ่งละ ~0.45 วิ) |
| ไม้บาตอง / ไม้บรรทัดวัดขนาด (เขียน p / mf / f) | `Core/Baton.cs` (ใช้ทั้งดวลและ Tutorial) |
| สัญลักษณ์โน้ต ลูกศร จุดตี วงแหวนจับเวลา | `Core/NoteGlyph.cs` (ใช้ทั้งดวลและ Tutorial) |
| สตามิน่า : TACET ซัด / หมดลมแพ้ | `BattleState` (ค้น `TACET'S BLOW`, `COLLAPSE`, `CheckBreath`) · ตัวเลข `BattleRules.BlowPerPower` · ภาพ `DuelScreen.ShowBreath` · ขอบจอ `DuelScreen.Hud.cs` `DrawLowBreath` |
| โหมด TUTORIAL | `Screens/TutorialScreen.cs` (รายการบท `lessons` แก้ข้อความ/ลำดับที่นี่ · บทพื้นฐานจบที่ READY บทหลังจากนั้นคือ EXTRAS, รอบ 15) · `TutorialScreen.Practice.cs` (จังหวะ โน้ต การตัดสิน) |
| HOW TO PLAY · ไอคอนปุ่ม | `Screens/GuideScreen.cs` (3 หน้า ข้อความอยู่ `bodies`) · `Ui.KeyCap` / `Ui.MouseIcon` |
| สอนตอนเจอครั้งแรกในดวล (TEACH NOTES) | `DuelScreen.TeachNote` / `SetUpRound` · เวลา `BattleRules.TeachNoteTime` |
| SECTION (กลุ่มเครื่องครบ แรงขึ้น) | `BattleState.SectionBonus` / `SectionSize` · ตัวเลข `BattleRules.SectionTwo` `SectionThree` · ป้ายหน้า THE BAND `BandScreen.Rebuild` |
| **เพลง (รอบ 15)** : ตัวโน้ตเพลง · ศัตรูเล่นตามเพลง · เสียงเครื่องดนตรี | เพลง `Data/SongChart.cs` (`SongList`) · ศัตรูชี้เพลง `Enemy.Song` · ท่อนต่อ pass `BattleState.SongPhrase` / `NoteAt` · วงเล่นโน้ต `DuelScreen.PlayBandNote` · ไฟล์เสียง/pitch `SoundBank.PlayInstrument` / `PitchFor` (ADVANCED) · ไฟล์ของใคร `Musician.Sample` / `SampleNote` |
| เส้นแสง CUE ในดวล | `Screens/DuelScreen.Stage.cs` `DrawCueBeam` |
| **โหมด NORMAL / MAESTRO (29 ก.ย. เย็น)** | ตัวเลข NORMAL = ค่าเดิมใน `BattleRules` · MAESTRO = `BattleRules.Maestro*` · ใช้ที่ `BattleState` (ค้น `MAESTRO`) · โหมดของรัน `RunState.Maestro` · จำไว้ `Settings.Maestro` · เซฟ `maestro=` ทั้งสองไฟล์ · สวิตช์ `Ui.ModeSwitch` + `ConductorSelectScreen.FlipMode` (หน้าแกลเลอรี + โปรไฟล์ กด M) · แถบรัน `RunHud.DrawTop` |
| การ์ดนักดนตรีตอนวงใหญ่ (ตัวอักษรไม่ซ้อน) | `MusicianArt.Card` (การ์ดเตี้ย = ลูกศรท้ายชื่อ) · กล่องวงในดวล `DuelScreen.Panels` (แคบ = STR/WND/PRC) · ชื่อตอนโค้ง `CurtainCallScreen.DrawBow` |
| เปิด/ปิดเสียงเครื่องดนตรี (29 ก.ย. ปิดอยู่) | `Audio/SoundBank.cs` `InstrumentsOn` (ค้น `SOUND OFF`) · คลังเสียงต้นฉบับ `SoundLibrary/` (README บอกไฟล์ที่ใช้) |
| **OUTRO (รอบ 15)** : ชนะกลางท่อน วงเล่นท่อนนั้นต่อจนจบ | `Screens/DuelScreen.cs` `StartOutro` / `UpdateOutro` / `OutroNote` (ค้น `OUTRO`) · เรียกจาก `UpdatePlay` เมื่อ `battle.Finished && battle.PlayerWon` |
| คำ BREATH บนจอ (รอบ 15) | ข้อความเท่านั้น · ในโค้ดยังชื่อ `Stamina` · คีย์เซฟ `stamina=` |
| หน้าเลือกเส้นทาง (ถนน · ทางแยก · ป้าย · กล่องรายละเอียด) | `Screens/RouteScreen.cs` `DrawRoad` `DrawFork` `DrawTiles` `DrawDetail` · ตัวอักษรชนิดที่ `RouteNodeInfo.Marks` |
| ความสามารถคอนดักเตอร์ (PASSIVE) | ตัวเลข `Data/Conductor.cs` (สตามิน่า Push สูตร ข้อความ) · ผล `BattleState` ค้น `BY THE BOOK` `LOCKED TEMPO` `RUNAWAY FIRE` `THE CLOSER THE LOUDER` (`LouderBonus`) `EVERY ROAD HOME` |
| **SIGNATURE ต่อคอนดักเตอร์ (รอบ 13)** | ชนิดท่า `Data/Conductor.cs` `SignatureMove` + ข้อความ `SignatureName` / `SignatureText` / `SignatureCall` · กติกา `BattleState.StartSignature` `SignatureIs` `SignatureGrade` `SizeDecided` · ค้น `ABSOLUTE PITCH` `CLOCKWORK` `SET ALIGHT` `DEAF EARS` `VILLAGE BAND` · อยู่กี่ครั้ง `BattleRules.SignatureStrokes` · เก็บโน้ต `BattleState.AddNotes` (PERFECT เท่านั้น) · กด Space `DuelScreen.ArmSignature` · กล่องนับถอยหลัง `DuelScreen.Panels.DrawSignaturePanel` |
| หน้าเลือกคอนดักเตอร์ | แกลเลอรี `Screens/ConductorSelectScreen.cs` (ลูกศร · ชื่อสกิลใต้ภาพ) · โปรไฟล์ `Screens/ConductorDetailScreen.cs` (PASSIVE / SIGNATURE · ลูกศรเปลี่ยนคน `Switch`) |
| พักบีตเงียบ (ไม่ตวัด หรือตวัดเล็ก = SOFT REST) | `Battle/BattleState.cs` `RestsOn` · `IsSilent` |
| Motif ของโน้ตพิเศษ / ชั้นที่เริ่มสุ่มให้ | `Data/Motif.cs` (`FromFloor`) · ผลใน `BattleState` (ค้น `ACCELERANDO` `TENUTO` `GRACE NOTE` `MARCATO` `CON BRIO` `CODA`) |
| ช่องภาพ | `Core/ArtBank.cs` (ภาพนิ่ง) · `Core/CharacterArt.cs` (นักดนตรี pixel) · `Core/HandArt.cs` (มือ) |
| ชื่อไฟล์เสียง | `Audio/SoundBank.cs` |

**กติกาเขียนโค้ดที่ตกลงกันไว้** (อาจารย์ตรวจโค้ด)
- ใช้ MonoGame แบบพื้นฐาน ท่ายากต้องมีคอมเมนต์ `ADVANCED PART` อธิบาย ตอนนี้มี : GestureReader (state machine),
  สปริงของไม้, SpriteBatch End/Begin (จอสั่น + point sampling ของภาพ pixel), RenderTarget (ยืดภาพใส่จอ), sine hash ใน TacetField
- คอมเมนต์หัวข้อสั้นค้นง่าย เช่น `//Player QTE` · กฎพิเศษทุกอันมีชื่อเป็นตัวพิมพ์ใหญ่ในคอมเมนต์ (`// MOMENTUM`) ค้นเจอทุกจุด
- **ห้ามสร้าง string ใน `Draw`** ข้อความเตรียมตอนโหลด ตัวเลขใช้ `NumberText.Get()`
- ขาวดำล้วน สีทั้งเกมอยู่ `Core/Palette.cs` สีเน้นเปลี่ยนที่ `Palette.Accent` จุดเดียว
- **ห้ามเจนภาพ** ภาพที่ยังไม่มี = กรอบว่าง `ArtSlot` เท่านั้น
- ไฟล์ .cs บางไฟล์มี BOM บางไฟล์ไม่มี แก้แล้วให้คงแบบเดิม · บรรทัดใน git เก็บเป็น LF ทุกไฟล์ (`.gitattributes` `text=auto`) Windows แปลงเป็น CRLF ตอน checkout เอง

**เครื่องมือ** `Core/DebugShots.cs` (ลบได้ ไม่ใช่ส่วนของเกม)
- `Tacetno433.exe --shots title,duel@150,result --out C:\pics` แคปหน้าจอนอกจอ ไม่ยุ่งกับเมาส์/คีย์บอร์ด
- `Tacetno433.exe --shots simulate --out C:\temp` จำลองการต่อสู้หลายพันครั้ง + ทั้งรัน เขียนผลลง `simulate.txt` (รอบ 12 : นิสัยผู้เล่น = ขนาดตวัด `Habit` · ความยาวดวลดูบรรทัด "READS THE MARKS, PERFECT")
- `Tacetno433.exe --shots audit --out C:\temp` ตรวจสมดุล (รอบ 10) แยกศัตรูต่อชั้น / ยุค / คอนดักเตอร์ / Motif ทีละใบ → `audit.txt` (~1 นาที)
- `Tacetno433.exe --shots savecheck --out C:\temp` เซฟ/โหลดในโฟลเดอร์ทดสอบ (ไม่แตะเซฟจริง) แล้วเขียน PASS/FAIL ลง `savecheck.txt`
- ในเกมกด **F3** ดู FPS และจำนวนไฟล์ที่โหลดได้ (SFX / MUSIC / PHRASE / HAND / ART / PIXEL)

## 4. งานที่เหลือ (เรียงตามความสำคัญ)

### ต้องทำก่อน (P1)
000000. **เล่นทดสอบรอบ 15** (`Alpha6`) : ดวลแรกช้า/อ่านง่ายพอไหม · ตวัดตามขนาดโน้ต · ตวัดไม่เสียลม · ความยากชั้น 2–3 · หน้า THE BAND แบบวงจริง · แล้วก้อน 2–4 ของวันนี้ (ดู `PROJECT_STATUS.md` หัวข้อ 0)
00000. **เล่นทดสอบรอบ 14** : เข้าดวลตรง · ป้าย PART เหนือหัวอ่านทันไหม · ป้ายคั่นรอบ / การ์ดกลไก / ดีล DEVIL ขัดจังหวะไหม · หน้า THE BAND · ร้าน HIRE · THE EMPTY CHAIR · แคปภาพ `docs/screenshots/` ใหม่บน Windows
0000. **เล่นทดสอบรอบ 13** : คอนดักเตอร์ครบ 5 คน กด Space ทุกครั้งที่สูตรเต็ม · แต่ละคนเล่นต่างกันจริงไหม · หน้าโปรไฟล์ / ลูกศร · TACET ซัดแรงขึ้น 0.95 (ถ้ายากไป ถามผู้ใช้ก่อนลด)
000. **เล่นทดสอบรอบ 12.2** : หน้าเลือกเส้นทางใหม่ · คอนดักเตอร์ครบ 5 คน (METRONOME แบบใหม่)
00. **เล่นทดสอบรอบ 12.1** : บท Tutorial ที่ 5 · เส้นแสง CUE · ตวัดกลาง/ใหญ่ที่ยาวขึ้น · ตวัดเล็กบนบีตเงียบได้พัก · NAMELESS MASTER
0. **เล่นทดสอบรอบ 12 ด้วยเมาส์จริง** (branch `Alpha4`) : ตวัด 3 ขนาดแม่นไหม · อ่าน f/mf/p ทันไหม · ตวัดบีตเงียบโดยไม่ตั้งใจบ่อยไหม · เห็น CUE ไหม · หน้า STAGE ใหม่อ่านรู้เรื่องไหม ·
   ข้อเสนอรีวิวรอบ 12 ข้อ 4 ทำแล้วรอบ 12.1 · ที่ยังรออยู่ : ข้อ 5 และ 6 ใน `PROJECT_STATUS.md` หัวข้อ 0
1. **เล่นทดสอบรอบ 9–10 ด้วยเมาส์จริง แล้วจูน** (branch `Alpha3`) · ลองคอนดักเตอร์ทุกคน (METRONOME / UNHEARING เพิ่งปรับ) · ดวลยาวพอดีไหม (`PushCap` `PushPerPower` `EliteLine` `BossLine`) · ยากขึ้นมาก ถ้าเกินให้ถามผู้ใช้แล้วลด `BlowPerPower` ก่อน ·
   เอฟเฟกต์รกไหม (ทำ STROKE TIMING test ก่อน) : 128 BPM ตวัดทันไหม · โน้ตคู่ชั้น 2 อ่านออกไหม ·
   TREMOLO รัวได้กี่ครั้ง (ตั้ง `TremoloPerfect` ตามจริง, รอบ 9 = 6) · COUNTER / FORTISSIMO / FINALE แรงไปไหม ·
   ค่าใน `GestureReader`, `PerfectWindow` / `GoodWindow` / `EarlyTime` · **ผู้ใช้ต้องการให้ยาก ห้ามลดความยากโดยไม่ถาม**
2. ~~เมนูหยุดเต็ม~~ ✅ รอบ 6 · ~~ถามยืนยัน~~ ✅ · ~~เซฟ + CONTINUE~~ ✅ (ไฟล์ข้อความ ไม่ใช่ JSON) · ~~Calibration~~ ✅
3. **เซฟหลังชนะดวล** : ตอนนี้ถ้าออกเกมที่หน้าผลลัพธ์/เลือก Motif แล้ว CONTINUE จะได้ดวลนั้นซ้ำ ·
   ถ้าจะแก้ ให้เซฟหลัง `ResultScreen.ApplyWin` พร้อมจำว่าค้างอยู่ที่หน้า Motif
4. ใส่ไฟล์เสียงชุดแรก (ทำนอง `Phrase` + `BeatTick` + `QtePerfect`) — ตอนนี้เกมเงียบ (ผู้ใช้สั่งลบเสียงชั่วคราวจากโค้ดออก 25 ก.ย.)

5. **Pull Request** ยังไม่ได้สร้างเลย (ผู้ใช้ยังไม่สั่ง) · `master` ยังเป็นงาน 23 ก.ย. · งานล่าสุดอยู่ `Alpha6` · ตัวที่ส่งอาจารย์อยู่ `Release` (zip อย่างเดียว) · ถ้าผู้ใช้สั่ง : `Alpha6` → `master` ให้ `master` เป็นเวอร์ชันล่าสุด

### ควรทำ (P2)
6. ~~โหมด Tutorial ที่หน้าแรก~~ ✅ รอบ 8 (12 บท, รอบ 12.1 เพิ่มบทจัดที่นั่งเป็น 13) · ยังเหลือ : รีวิว 25 ก.ย. เสนอ ก–ซ ไว้ (`DESIGN_RESEARCH.md` 6.8) รอผู้ใช้เลือก · แนะนำ ก+ข+ค+ง ก่อน
7. **ปรับใหญ่ (ต้องคุยกับผู้ใช้ก่อน)** : ห้องซ้อม · การเลือกเส้นทาง (ให้ง่ายขึ้น/เหมือนเกมตลาด เช่นแผนที่ที่เห็นข้างหน้า) ·
   การจัดทีม · ~~หน้าจัดจังหวะ~~ (รอบ 12 ลบแล้ว)
8. ~~เปลี่ยนหน้าแบบม่านเวที~~ ✅ รอบ 7 · ~~คำอธิบายตาราง THE PERFORMANCE~~ ✅ รอบ 7 · ~~เลน/จุดตีเต้นตาม BPM~~ ✅ รอบ 7 ·
   ยังเหลือ : ตัวเลขปะทะนับพร้อมเสียงไต่ระดับ (รอไฟล์เสียง) · แผงวงด้านล่างเต้นตาม BPM
9. เนื้อหาเพิ่ม : เหตุการณ์ (6 → 12+) · บอสยุคละ 2 · นักดนตรี · Motif
10. ต่อไฟล์ภาพ/เสียงเมื่อทีมส่งมา (เพิ่มใน `Content.mgcb` เท่านั้น ไม่ต้องแก้โค้ด) และเช็กขนาดในเกม
10.1 **ช่องโหลดภาพ UI skin (8 ต.ค., ยังไม่ได้ทำ)** : ทีมอาร์ตจะวาดตาม `docs/TACET_UI_Asset_List.xlsx` (126 ไฟล์ `Content/Art/UI/<code id>.png`) · ต้องเพิ่มการโหลดแบบ ArtBank + วาดแบบ 3-slice / 9-slice ใน `Core/Ui.cs` (Button Panel Tag Slider CheckBox CapsuleBar ModeSwitch) และจุดที่วาดเองในหน้าจอ · ไม่มีไฟล์ = ใช้แบบโค้ดเดิม

### ทำทีหลัง (P3)
11. ภาษาไทยจริง (ต้อง build ฟอนต์ที่มีตัวไทย + ตารางข้อความ 2 ภาษา ตอนนี้หน้าตั้งค่าเลือกได้แต่ยังไม่แปล)
12. ปลดล็อกถาวร (คอนดักเตอร์ 2–5 ตอนนี้เปิดหมดตั้งแต่แรก)
13. โปรเจกต์ทดสอบของ `BattleState` · README หน้า repo · tag checkpoint
14. `DebugShots` ห่อด้วย `#if DEBUG` หรือเอาออกตอนส่ง · แพ็กเกจ `MonoGame.Extended` ใน `.csproj` ไม่ได้ใช้ ลบได้

## 5. ปัญหาที่รู้อยู่

- เพลย์เทส 25 ก.ย. เล่น build รอบ 8 · รอบ 9–12 ยังไม่มีคนเล่นจริง ภาพทดสอบใช้เมาส์ปลอม
- รอบ 14 : ตัวจำลองไม่เลือกคนขึ้นเวทีเอง (นั่งตามลำดับที่เข้าวง) · ไม่ได้วัดว่าคนอ่านป้าย PART เหนือหัวทันไหม (รัศมี 12–15 px) · ดวลยาวขึ้น ~10% สำหรับผู้เล่นเก่ง (24.4 บีต เดิม 22.3) ·
  FOLK LEADER ผู้เล่นทั่วไปชนะน้อยสุด 5–6% (สูตร 3/3/3) · UNHEARING เก่งสูงสุด ~76% · WHITE NOISE ง่ายกว่ารอบ 13 (เก่งแพ้ชั้น 1 7–8% เดิม 16%) DEVIL ยากขึ้นสำหรับผู้เล่นทั่วไป (20% เดิม 11–13%) ·
  เซฟเก่าย้ายคนเข้าที่นั่งประจำเอง (`Formation.Tidy`, savecheck ผ่าน) ยังไม่ได้ลองกับไฟล์จริง
- รอบ 12 : ตัวจำลองไม่ย้ายคนระหว่างรอบ ไม่จัดฝั่งตาม CUE ไม่หลบ SILENT MOUTHS (นั่งตามลำดับปลดล็อก) · ไม่ได้วัดว่าคนตวัด 3 ขนาดแม่นแค่ไหน ·
  (รอบ 12.1 : NAMELESS ชั้น 1 เก่งแพ้ ~17% ยังยากสุดแต่ไม่เป็นกำแพง · SECOND WIND +14–16 · ใบแรงสุดตอนนี้ CON BRIO +19–21) · เซฟเก่า (ก่อนรอบ 12) ย้ายคนออกจากที่ล็อกเอง ยังไม่ได้ลองกับไฟล์จริง ·
  ภาพ `docs/screenshots/score*.png` `bargain.png` `stage.png` เป็นหน้าก่อนรอบ 12
- การจำลองบาลานซ์รวม SIGNATURE แล้ว (รอบ 13 : กด Space ทันทีที่สูตรเต็ม บนบีตที่มีโน้ต · คนจริงอาจกดช้ากว่าหรือเลือกจังหวะได้ดีกว่า) ·
  รวมการอ่าน f/mf/p (นิสัย "อ่านโน้ต") · โน้ตคู่ TREMOLO COUNTER FORTISSIMO FINALE แล้ว · ไม่กด Space เลย เสีย ~5–8% ของผู้เล่นเก่ง
- สตามิน่า (รอบ 8) กดดันภายในดวลแล้ว (แพ้เพราะหมดลมเป็นส่วนใหญ่) แต่ตอนถึงบอสยังเหลือ ~85% → การบริหารข้ามด่านยังเบา ผูกกับการปรับใหญ่ห้องซ้อม/เส้นทาง (ต้องคุยก่อน)
- เซฟมีช่องเดียว · CONTINUE เข้าร้านเดิมจะสุ่มของในร้านใหม่
- ฟอนต์อ้าง `C:/Windows/Fonts/pala.ttf` แบบ path เต็ม (ใช้ได้ทุกเครื่อง Windows ที่ติดตั้งที่ไดรฟ์ C)
- ตอน cut-in ของ SIGNATURE เกมหยุดนาฬิกาจังหวะ 1 วินาที (ตั้งใจ) ถ้ารู้สึกหลุดจังหวะให้ย้ายไปเล่นตอนจบห้อง
- โน้ตทำนอง (`Audio/Phrase`) เล่นตามลำดับบีต 1–8 ไม่สนว่าใครเล่น ถ้าอยากให้แต่ละเครื่องดนตรีมีเสียงของตัวเองต้องต่อยอด
- การจำลองรวม Motif แล้ว (รอบ 9, สุ่มเลือก 1 จากข้อเสนอ) แต่ยังไม่รวม Event / การซื้อ Motif ในร้าน / HIRE / SIGNATURE · ผลจำลองแกว่ง ±5–8% ระหว่างรัน ต้องรันหลายครั้ง
- รอบ 11a แก้แล้ว : WHITE NOISE · ศัตรูธรรมดา · Motif อ่อน 5 ใบ · FOLK LEADER · ที่ยังเหลือ : INFERNO สูงกว่าเพื่อน ~5% (ผู้เล่นทั่วไป ~2 เท่า) ·
  OVERTURE (หายากขั้น 3) อ่อน +3 · **PUSH CAP ทำให้การ์ดที่เพิ่มพลังบนบีตเดียวอ่อน** ออกแบบการ์ดใหม่ให้ระวัง (ของที่ขาดจริงคือสตามิน่า)
- รอบ 8–9 : การจำลองถือว่าผู้เล่นเก่ง BOOST ได้ทุกบีต ไม่ได้วัดว่ามือคนตวัดยาวทุกบีตที่ 128 BPM ไหวไหม · ผู้เล่นไร้ที่ติชนะ ~97%
- เพลย์เทส 25 ก.ย. : **เข้าใจยาก** (หน้าเตรียมตัวสู้ ข้อความเยอะ ไม่รู้ว่าดาเมจมาจากไหน) · รอบ 12 ลบหน้า SCORE แล้ว ยังไม่ได้ทดสอบกับคน
- ข้อความบนการ์ด Motif เขียนตัวเลขไว้ตรงๆ ถ้าแก้ตัวเลข Motif ใน `BattleRules` ต้องแก้ข้อความใน `Data/Motif.cs` ด้วย
- เอฟเฟกต์รอบ 7 ตรวจแค่ด้วยภาพนิ่ง (เมาส์ปลอม) ยังไม่มีคนเล่นจริง

## 6. สิ่งที่ผู้ใช้ตัดสินใจแล้ว (ห้ามเปลี่ยนเองโดยไม่ถาม)

- ไม่มีโหมดเลือกความยาก (roguelike, ผู้ใช้อยากให้ยาก) · ไม่รองรับทัชแพด
- ไม่มีเพลงยาวในดวล เสียงทำนองขึ้นทีละโน้ตเมื่อตวัด · ตัดไอเดีย "ยุค = จังหวะ"
- ภาพชั่วคราว = กรอบว่างเท่านั้น · ธีมภาพตาม Limbus Company Canto 10 / E.G.O Hollow แต่ยังขาวดำ
- ความสามารถนักดนตรีและกลไกศัตรูเป็นฐานแรก ("ลองทำไปก่อน") เปลี่ยนได้หลังเล่นทดสอบ
- รอบ 6 : ผู้ใช้เลือกครบ ตวัดต่อเนื่อง · BPM 88/100/116 (รอบ 9 → 96/112/128) · โน้ตคู่ · TREMOLO · COUNTER · FORTISSIMO · FINALE · ความยาก "หนัก"
- รอบ 8 : สตามิน่า = ตัวกดดัน หมดลม = แพ้ · ท่าเท่ๆ ฟรี · ม่านเฉพาะหน้าเลือกคอนดักเตอร์ · Tutorial
- รอบ 9 : ดาเมจเบาลง + PUSH CAP + HEAVY LINE · เป้าดวลผู้เล่นเก่ง ธรรมดา ~1.5 รอบ Elite ~2 บอส 2–3 · ตวัดถี่ขึ้น (BPM โน้ตคู่ชั้น 2 ชั้น 1 ทุกศัตรูรัว)
- รอบ 10 : ปรับ METRONOME / UNHEARING / BREATH MARK · ความยากรวมเท่าเดิม
- รอบ 11 : ทำสมดุลที่เหลือ (ศัตรู / Motif อ่อน / FOLK LEADER) ก่อน · ACCELERANDO รัวนับ 2 · TENUTO ค้างครบคืน 8 · MARCATO ดันถึง 20 ·
  รอบ 11b แผนเล่นวน 1→2→3 ครั้ง (ต่อเนื่อง ไม่มีช่วงนั่งรอ) · TACET เล่นบีตเดิมสุ่ม f/mf/p ใหม่ · ดวลเก่ง ธรรมดา ~2 แผน Elite 2–3 บอส 3 · ความยากง่ายลงได้นิดหน่อย
- รอบ 12 : โล๊ะการจัดจังหวะก่อนสู้ · แบบ 1 DYNAMICS (ขนาดตวัด = ใครเล่น) + เสริมจากแบบ 2 CUE (ทิศตวัด) · คนที่เล่นกินสตามิน่า · เก็บ REPEATS ·
  CUE + ศัตรูเล่นกับฝั่ง (ใช้ศัตรูเดิม) · "เล่นได้ไม่ยากมาก แต่ยังมีความลึก" (จูนให้อัตราชนะเท่ารอบ 11b)
- รอบ 12.2 : หน้าเลือกเส้นทางแบบ ข (ถนน + ทางแยก) · หน้าเลือกยุคคงเดิม · รีวิว/ปรับคอนดักเตอร์ทุกคน
- รอบ 13 : คอนดักเตอร์ทุกคนมี PASSIVE + SIGNATURE ของตัวเอง
- รอบ 14 : ลบหน้า STAGE · PART ติดตัวนักดนตรี (แบบที่ 2 ที่ Claude เสนอ) · ปรับบอส สกิล event ให้เข้ากับรูปแบบใหม่ · ความยากรวมเท่ารอบ 13
- รอบ 12.1 : ผู้ใช้สั่งข้อเสนอรีวิวรอบ 12 ก–จ ทั้งหมด (ตวัดเล็กบนบีตเงียบ = พัก · CUE เห็นชัด · บท Tutorial จัดที่นั่ง · ตวัดกลางกว้างขึ้น · ปรับ NAMELESS / SECOND WIND)
- Esc = เมนูหยุดทุกหน้าในรัน · สิ่งที่ย้อนไม่ได้ต้องถามก่อน ค่าเริ่มเลือก "ไม่" · แพ้ดวล = ลบเซฟ (roguelike)
