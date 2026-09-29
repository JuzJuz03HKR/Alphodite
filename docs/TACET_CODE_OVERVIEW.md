# TACET 4'33 — ภาพรวมโค้ด (สำหรับฝ่ายโค้ด / อธิบายอาจารย์)

MonoGame DesktopGL 3.8 · C# .NET 9 · 77 ไฟล์ .cs · ~19,900 บรรทัด · เปิด `Tacetno433.sln` แล้วกด F5 · อัปเดต 29 ก.ย. 2026 (`Alpha6`)
แผนที่โค้ดแบบละเอียด ("อยากแก้อะไร ไปไฟล์ไหน") อยู่ `docs/HANDOFF.md` หัวข้อ 3

---

## 1. โครงสร้างโปรเจกต์

```
Tacetno433/
├─ Program.cs / TacetGame.cs   จุดเริ่มเกม : โหลดฟอนต์ ภาพ เสียง · วาดทั้งเกมลงภาพ 1280x720 แล้วยืดใส่จอ
├─ Core/     (4,500 บรรทัด)    เครื่องมือกลาง : วาด (Gfx, Ui, Palette) · อ่านเมาส์ (Input, GestureReader) ·
│                              ไม้บาตอง (Baton) · สัญลักษณ์โน้ต (NoteGlyph) · เปลี่ยนหน้า (ScreenManager, SlantWipe,
│                              SilenceWave, Curtain) · เอฟเฟกต์ math art (Hollow, TacetField, StageStaff) ·
│                              ช่องภาพ (ArtBank, ArtSlot, CharacterArt) · เครื่องมือทดสอบ (DebugShots)
├─ Data/     (2,800 บรรทัด)    ข้อมูลล้วน : BattleRules (ตัวเลขบาลานซ์ทุกตัว) · Conductor · Musician · Enemy ·
│                              Motif · GameEvent · SongChart (เพลง) · RunState (สถานะรัน) · SaveFile (เซฟ)
├─ Battle/   (2,300 บรรทัด)    BattleState = กติกาการต่อสู้ทั้งหมด (ไม่มีโค้ดวาด) · DuelEffects = เอฟเฟกต์ในดวล
├─ Screens/  (9,600 บรรทัด)    หน้าจอ 19 หน้า + PauseMenu · DuelScreen แยก 7 ไฟล์ (partial class)
└─ Audio/SoundBank.cs          เสียงทั้งหมด (ตอนนี้ปิดเครื่องดนตรีไว้ : InstrumentsOn = false)
```

**หลักการแบ่งโค้ด (พูดกับอาจารย์ได้)**
- **ข้อมูล / กติกา / การวาด แยกกัน** : ตัวเลขอยู่ `BattleRules` ที่เดียว · กติกาอยู่ `BattleState` (ทดสอบได้โดยไม่ต้องเปิดจอ) · หน้าจอแค่อ่านสถานะแล้ววาด
- **หน้าจอทุกหน้าสืบทอด `GameScreen`** มีแค่ `Load / Update / Draw` · `ScreenManager` สลับหน้าพร้อม transition
- **ห้ามสร้าง string ใน `Draw`** : ข้อความเตรียมตอนโหลด ตัวเลขใช้ `NumberText.Get()` → ไม่มีขยะให้ Garbage Collector เก็บทุกเฟรม
- **ขาวดำล้วน** สีทั้งเกมอยู่ `Core/Palette.cs` · สีเน้นเปลี่ยนที่ `Palette.Accent` จุดเดียว
- **ภาพที่ยังไม่มี = กรอบว่าง** (`ArtSlot`) · ใส่ไฟล์ชื่อตรงแล้วขึ้นเองโดยไม่ต้องแก้โค้ด (`ArtBank`)
- **กฎพิเศษทุกอันมีชื่อตัวพิมพ์ใหญ่ในคอมเมนต์** (`// FERMATA`, `// MAESTRO`, `// CUE`) ค้นแล้วเจอทุกจุด

---

## 2. การเดินของเกม (Game Loop ของ MonoGame)

1. `TacetGame.Update(dt)` → `Input` อ่านเมาส์/คีย์ → `ScreenManager.Update` → หน้าปัจจุบัน `Update(dt)`
2. `TacetGame.Draw` → วาดหน้าลง **RenderTarget 1280×720** (ภาพในหน่วยความจำ) → ยืดภาพนั้นใส่หน้าต่างจริงแบบรักษาสัดส่วน
   → เล่นได้ทุกความละเอียดจอ โดยโค้ดทุกหน้าคิดพิกัดแค่ 1280×720
3. ลำดับหน้าในรัน : `Screens/RunFlow.cs` (`NextStage` / `Enter` / `Continue`)

---

## 3. ส่วนที่ advance (มีคอมเมนต์ `ADVANCED PART` ในโค้ด)

| เรื่อง | ไฟล์ | อธิบายสั้นๆ |
|---|---|---|
| **อ่านการตวัดไม้** | `Core/GestureReader.cs` | **State machine** 2 สถานะ (รอ / อยู่ในการตวัด) · เริ่มตวัดเมื่อเมาส์เร็วขึ้น · จบเมื่อช้าลง หรือหักมุม (จุดเด้งของวาทยากร) หรือนานเกิน · ได้ 3 อย่าง : **ทิศ** (แกนที่ขยับมากกว่า) · **ขนาด** (ระยะเริ่ม-จบ) · **เวลา** (ตอนไม้หยุด = ictus) · นับเฉพาะตอนกดคลิกซ้ายค้าง |
| **ไม้บาตองแกว่ง** | `Core/Baton.cs` | **Damped spring 3 บรรทัด** : ดึงความเร็วหมุนเข้าหามุมเป้าหมาย → หักแรงเสียดทาน → หมุนไม้ตามความเร็ว · ไม้เลยลากตามมือ เลยเป้านิดนึงแล้วนิ่ง · ตวัดเสร็จมี "whip" สะบัดกลับ + ปลายไม้เรืองแสง |
| **จอสั่น (shockwave)** | `Screens/DuelScreen.cs` | ตอนเสียงสองฝั่งปะทะ (`StartClash`) ตัวแปร `shake` เพิ่มตามแรง แล้วลดเองทุกเฟรม · วาดโลกด้วย **SpriteBatch End/Begin + transform matrix** เลื่อนภาพทั้งฉาก · มี hit-stop (ภาพหยุดสั้นๆ ตอนจบห้อง) |
| **ภาพ pixel คม** | `Core/CharacterArt.cs` | End/Begin SpriteBatch ด้วย `SamplerState.PointClamp` ให้ภาพ pixel ขยายแล้วไม่เบลอ · ย่อตัวละครให้ไม่สูงเกินช่องยืน |
| **เปลี่ยน pitch เสียง** | `Audio/SoundBank.cs` | ไฟล์เดียวต่อเครื่องดนตรี เล่นโน้ตอื่นด้วย `pitch = ครึ่งเสียง / 12` (MonoGame รับ −1..+1 = ±1 อ็อกเทฟ) ไกลกว่านั้นเลื่อนทีละอ็อกเทฟ |
| **ขอบความเงียบสั่นไหว** | `Core/TacetField.cs` | **sine hash** (สูตรสุ่มแบบ shader) สร้างรอยฉีกของขอบดำให้ขยับเหมือนสุ่ม แต่คำนวณได้ทันทีไม่ต้องเก็บค่า |
| **วาดลงภาพนอกจอ** | `TacetGame.cs` / `Core/DebugShots.cs` | **RenderTarget2D** : วาดเกมลงภาพ 1280×720 แล้วยืด · เครื่องมือทดสอบใช้ภาพเดียวกันเซฟเป็น PNG |

---

## 4. VFX / ความสวยงาม (วาดด้วยโค้ดทั้งหมด = math art)

| เอฟเฟกต์ | ไฟล์ | ทำยังไง |
|---|---|---|
| **ดวงอาทิตย์ดำของ TACET** (Eclipse) | `Core/Hollow.cs` | แผ่นดำ + corona เป็นวงแสงหลายชั้นจางออก + ขอบขาวบาง + เสี้ยวหนาที่ค่อยๆ หมุน · วงแหวนเรือง (Ring) = โน้ตของ TACET |
| **รอยแยกความเงียบ** | `Core/TacetField.cs` | วาดทีละ 2 แถว pixel ด้วยสี่เหลี่ยมธรรมดา · ขอบฉีกเปลี่ยน ~14 ครั้ง/วินาที · มีแสงรั่ว เส้นแสงพุ่ง เศษแสงปลิว · ใช้ทั้งหน้าแรก ดวล ผลดวล |
| **คลื่นเสียง / ระเบิดตอนปะทะ** | `Battle/DuelEffects.cs` | **Object pool** : สร้างทุกอย่างครั้งเดียวตอนเปิดดวล แล้วเปิด/ปิดช่องเดิมซ้ำ → ดวลยาวแค่ไหนก็ไม่สร้าง object ใหม่ · มี Spark (เศษแสง) Flare (เส้นแสงตัดจุดตี) Ripple (วงน้ำใต้นักดนตรี) HitBurst (วงแสงตามเกรด แบบ Project Sekai) โน้ตแตกเป็นชิ้น |
| **บรรทัดห้าเส้นสั่น** | `Core/StageStaff.cs` | เส้นละ 40 ท่อน · ความสูงแต่ละท่อน = sin(ตำแหน่ง − เวลา) × sin ช้าๆ ที่เป็น 0 ที่ปลาย → คลื่นวิ่งแต่ปลายตรึงเหมือนสายดีด · ชนะบีตเส้นสั่น ช่วง FORTISSIMO สั่นตลอด |
| **เปลี่ยนหน้า 3 แบบ** | `Core/ScreenManager.cs` | แถบดำเฉียง (`SlantWipe`, ease-out) หน้าปกติ · คลื่นความเงียบกลืนจอ (`SilenceWave`) ตอนเข้าดวล/จบรัน · ม่านเวที (`Curtain`) หน้าเลือกคอนดักเตอร์ |
| **ลมใกล้หมด** | `Screens/DuelScreen.Hud.cs` | ขอบจอบีบตามจังหวะ + แผงลมสั่นตอนโดน TACET ซัด |
| **โน้ต / จุดตี / วงแหวนจับเวลา** | `Core/NoteGlyph.cs` | ใช้ร่วมกันทั้งดวลและ Tutorial (แก้ที่เดียว) · โน้ตถัดไปสว่าง ที่เหลือจาง |

---

## 5. ระบบหลักกับไฟล์

| ระบบ | ที่อยู่ |
|---|---|
| กติกาดวลทั้งหมด (ขนาดตวัด ทั้งวงเล่น CUE SECTION IN TUNE ลม เส้นดัน รัว ค้าง โน้ตคู่ FORTISSIMO FINALE SIGNATURE) | `Battle/BattleState.cs` (ค้นชื่อกฎตัวพิมพ์ใหญ่) |
| ตัวเลขบาลานซ์ทุกตัว + โหมด NORMAL / MAESTRO | `Data/BattleRules.cs` (ค้น `MAESTRO`) |
| หน้าดวล (7 ไฟล์) | `DuelScreen.cs` (จังหวะ/รอบ/OUTRO) · `.Baton` (ตัดสินการตวัด) · `.Notes` (รัว ค้าง คู่) · `.Stage` (เวที เลน นักดนตรี) · `.Hud` (แถบบน ป้าย) · `.Panels` (กล่องล่าง) · `.Finale` |
| เพลง | `Data/SongChart.cs` : ท่อนละ 8 คำ เช่น `"E4p G4f -"` (โน้ต+อ็อกเทฟ+ความดัง, `-` = พัก) · ศัตรูชี้เพลงด้วย `Enemy.Song` |
| คอนดักเตอร์ / นักดนตรี / ศัตรู / Motif / เหตุการณ์ | `Data/Conductor.cs` · `Musician.cs` · `Enemy.cs` · `Motif.cs` · `GameEvent.cs` |
| สถานะรัน + เซฟ | `Data/RunState.cs` · `Data/SaveFile.cs` (ไฟล์ข้อความ `key=value` ใน `%AppData%\TACET433`) · **ห้ามสลับลำดับรายการ** ใน `MusicianList` ฯลฯ เพราะเซฟเก็บเป็นลำดับ |
| Tutorial | `Screens/TutorialScreen.cs` (รายการบท แก้ข้อความที่นี่) + `.Practice.cs` |
| ภาพ | `Core/ArtBank.cs` (ภาพนิ่ง) · `CharacterArt.cs` (ตัวละคร pixel, `<ชื่อ>_idle_0.png`) · รายการไฟล์ที่ต้องทำ `docs/ASSET_LIST.md` |

---

## 6. เครื่องมือทดสอบ (DebugShots) — จุดเด่นที่เล่าให้อาจารย์ฟังได้

- `Tacetno433.exe --shots duel@150,band --out <โฟลเดอร์>` : แคปหน้าจอนอกจอ ไม่ต้องเล่นเอง
- `--shots simulate` : **ตัวจำลองบาลานซ์** เล่นดวลและรันเต็มหลายพันครั้งด้วยผู้เล่น 6 แบบ (ไร้ที่ติ เก่ง ทั่วไป มือใหม่ ฟาดใหญ่ ตวัดกลาง) ทั้ง NORMAL และ MAESTRO → `simulate.txt`
- `--shots audit` : แยกอัตราชนะตามคอนดักเตอร์ / Motif / ศัตรู / ยุค → หาของที่แรงหรืออ่อนเกิน
- `--shots savecheck` : ทดสอบเซฟ/โหลด 20 จุดอัตโนมัติ (ผ่าน 20/20)
- ในเกมกด **F3** ดู FPS และจำนวนไฟล์ที่โหลดได้

## 7. การ build

- ปกติ : Visual Studio → Rebuild → F5 (ต้อง 0 error 0 warning)
- ทำ .exe : `dotnet publish Tacetno433\Tacetno433.csproj -c Release -r win-x64 --self-contained true` → zip โฟลเดอร์ `publish`
- ไฟล์ที่ส่งแล้ว : branch `Release` → `TACET4'33.zip` (build จาก Linux ด้วย .NET 8 + ฟอนต์แทน)
- คลังเสียงต้นฉบับของทีม : `SoundLibrary/` (ไม่ได้อยู่ใน build) · เปิดเสียงคืน : `SoundBank.InstrumentsOn = true`
