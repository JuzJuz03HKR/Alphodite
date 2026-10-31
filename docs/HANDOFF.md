# TACET 4'33 — เอกสารส่งต่อให้ฝ่ายโค้ด

> **29 ต.ค. : Beta1** (branch `Beta1`) อ่านข้อ 00 ก่อน เป็นภาพรวมของเกมตอนนี้ แผนที่โค้ดอยู่ข้อ 0.0 (Beta1) กับข้อ 0 (Alpha 7)
> ข้อ 1 ถึง 6 เป็นประวัติของเกม rhythm (`Alpha6` ถึง 6 ต.ค.) เก็บไว้อ้างอิง (เดิม 18 ต.ค. : Alpha 7 เปลี่ยนเป็น turn-based roguelike โครงโค้ดอยู่ข้อ 9 ของ [`ALPHA7_DESIGN.md`](ALPHA7_DESIGN.md))

## 00. ภาพรวมตอนนี้ (Beta1, อัปเดต 31 ต.ค.)

### 00.1 เกมตอนนี้
- roguelike แบบผลัดตา เลือกวาทยากร 1 ใน 3 (THE UNHEARING, FANNY MENDELSSOHN AND ???, THE METRONOME) แล้วเลือกนักดนตรี 3 จาก 5 คนในตำนาน (PAGANINI QUANTZ PFUNDT DUPORT PUNTO) เป็นทีมตายตัวทั้งรัน
- รันมี 3 ชั้น (PRELUDE NOCTURNE CODA) แผนที่ทั้งชั้นแบบมองไกล ห้อง FIGHT ELITE BOSS ??? SHOP REST LUTHIER TREASURE ของถาวรหลังรันซื้อที่ THE ARCHIVE
- การต่อสู้ ONE BAR : ทุกห้องให้แต่ละคนเลือก ATTACK หรือสกิล เรียงลำดับเอง แล้วกด ENTER สกิลกับ SIGNATURE ตวัดไม้ตามลูกศรตอนวงแหวนปิด (ONE RULE) ศัตรูตีแล้วปัดสวนได้ (parry, COUNTER)
- ชุดสกิลใหม่ของ 5 คน (THE FIVE KITS 29 ต.ค. ดึก) ทุกสกิลมีชนิดและสี โจมตีแดง ป้องกันฟ้า ฮีลเขียว เสริมทอง
- 30 ต.ค. : ศัตรูมีท่าชาร์จกับตั้งโล่ (CHARGE GUARD) / นักดนตรีโตตามเส้นทางของตัวเอง PATH คนละ 2 เส้น 3 ขั้น (30 ใบ) ได้จาก INSPIRED (PERFECT 4 ครั้งในไฟต์ เลือกทันที) กับชนะไฟต์ได้เลือก 1 ใบ / SIGNATURE โตทุกบอส / BONDS 5 สายจากประวัติจริง / หน้าต่อสู้แบบ GUI มาตรฐาน วงเล็ง เครื่องหมายบนเพื่อน การ์ดสกิลเป็นตัวเลข ไอคอนสถานะกับเจตนา ไม่มีเส้นบอกเป้าแล้ว (`BETA1_DIRECTION.md` 12.3 13.1 ถึง 13.5)
- ฉากต่อสู้ Beta1 : มุมวาทยากร ความมืด TACET (shader) VFX ตอนตี impact frame กล้องขยับตามไม้ เต็มจอวาดที่ความละเอียดจอจริง
- หน้าจอทุกหน้ายกระดับแล้ว (ฟอนต์ OFL ปุ่มแบบผสม แผ่นเฉียงบอกชื่อหน้า ม่านผ้าจริง การ์ด 3D)
- เนื้อหา : MOTIF 27 ใบ ศัตรู 17 ตัว (บอส 6) เหตุการณ์ 10 สกิล 10 สกิลของ 5 คน ขั้นเส้นทาง 30 ใบ
- ยังไม่มีอาร์ตกับเสียงจริง ทุกช่องเป็นกรอบว่าง เกมเงียบ (โค้ดรองรับครบ)

### 00.2 ความคืบหน้า (ประมาณการ 30 ต.ค. ไม่นับงานอาร์ต)

| ส่วน | น้ำหนัก | เสร็จ | ที่เหลือ |
|---|---|---|---|
| ระบบเกมพื้นฐานกับเครื่องมือ (หน้าจอ เปลี่ยนหน้า shader เต็มจอคม เซฟ ตั้งค่า ตัวทดสอบ) | 15% | 90% | ตั้งค่ากราฟิก ทำ build สำหรับขาย |
| กติกาการต่อสู้ | 15% | 90% | ท่าใหม่ของบอส เก็บรายละเอียด |
| หน้าตาการต่อสู้ (ฉาก VFX GUI) | 15% | 75% | VFX ของสกิลใหม่กับท่าใหม่ SIGNATURE ของ Fanny กับ Bach แถบการ์ดของวง (ผู้ใช้มีไอเดีย) ไอคอนจริง |
| ระบบรันและการโตของทีม | 15% | 65% | ก้อน 3 ถึง 5 : MOTIF ที่เห็นผล อาวุธวิวัฒน์ บอสตามวาทยากร ENCORE ปลดของใหม่ |
| หน้าจออื่นๆ | 10% | 88% | หน้าใหม่ที่ก้อน 3 ถึง 5 ต้องใช้ |
| เนื้อหา (สกิล ศัตรู MOTIF เหตุการณ์) | 10% | 60% | ปรับ MOTIF ท่าใหม่ของบอส เหตุการณ์แบบทางเลือกเสี่ยง |
| Tutorial | 5% | 35% | ทำใหม่ให้ตรงระบบ Beta1 (HOW TO PLAY อัปเดตแล้ว) |
| เนื้อเรื่อง | 5% | 10% | มีแค่ lore ตัวละคร ยังไม่มีฉากเล่าเรื่อง |
| บาลานซ์ เพลย์เทส QA | 10% | 30% | มีแค่ตัวจำลอง ยังไม่มีคนจริงลองหลายคน |

รวมประมาณ **68%** (ช่วง 63 ถึง 72%, 29 ต.ค. ประเมินไว้ 60%) เป็นการประเมินจากรายการงานในเอกสาร ไม่ใช่ค่าที่วัดได้ 100% หมายถึงแผนตอนนี้ (ก้อน 1 ถึง 6 ของ `BETA1_DIRECTION.md` 11.7 กับ Tutorial เนื้อเรื่อง การทดสอบ) ถ้าแผนโตขึ้น % จะลดลง
ไม่นับไฟล์เสียง (เป็นงานแบบเดียวกับอาร์ต) และภาษาไทย ของเก่าตอนเป็นเกม rhythm (6 ต.ค.) อยู่ข้อ 2

### 00.3 งานที่เหลือ เรียงตามที่ตกลงกับผู้ใช้
0. **แชทถัดไป (31 ต.ค. ผู้ใช้ลองรันแล้ว)** : `BETA1_DIRECTION.md` 14.7 ตัดสิน INSPIRED กับจังหวะการโต / เป้าของสกิลที่ใช้กับเพื่อนให้เห็นตรงที่มอง / ท่าบอสถ้าผู้ใช้เลือก / ทบทวนรอบแรกของผู้เล่นใหม่ (เป้าหมาย : เล่นครั้งแรกเข้าใจทันที)
1. ก้อน 1 (`BETA1_DIRECTION.md` 11.11 ข้อ 12 13) : ✅ ข้อ 1 ชนิดสกิล ✅ ข้อ 2 ชุดสกิลใหม่ 5 คน ✅ ข้อ 3 ศัตรู CHARGE GUARD ✅ ข้อ 4 หน้าต่อสู้แบบ HSR/CZN (13.4) ✅ ข้อ 5 ข้อความ ✅ ข้อ 6 ทดสอบ / เหลือข้อ 7 VFX สกิลใหม่ (ถามตัวอย่างผู้ใช้ก่อน)
2. ก้อน 2 : ✅ PATH / ✅ INSPIRED กับรางวัลไฟต์แบบ ก / ✅ SIGNATURE โตที่บอส / ✅ BONDS (30 ต.ค. ผู้ใช้ยังไม่ได้ลองรัน ที่รอตัดสินอยู่ 13.6)
3. ก้อน 3 : MOTIF ทำงานให้เห็น / อาวุธขั้น III วิวัฒน์ / ดอกเบี้ย
4. ก้อน 4 : เห็นบอสตั้งแต่ต้นชั้น / บอสท้ายรันเป็นร่าง Hollow ของวาทยากรที่เลือก
5. ก้อน 5 : ENCORE 1 ถึง 10 / ARCHIVE ปลดของใหม่ / MEMORY
6. ก้อน 6 : COMBO
7. งานอื่นที่ค้าง : SIGNATURE ของ Fanny กับ Bach ยังเป็นแสงกากบาทธรรมดา (`BattleScreen.Stage.cs` `SignatureStyle`) / VFX อาวุธขั้นสูงยังไม่ชัด (ผู้ใช้บอก 29 ต.ค.) / ตั้งค่ากราฟิก / ผูกดาเมจกับเฟรมภาพ (รออาร์ต) / Tutorial ใหม่ / เนื้อเรื่อง (ทำท้ายสุด) / เพลย์เทสคนจริง / ภาษาไทยถ้าจะทำ / build สำหรับขาย (ห่อ `DebugShots` ด้วย `#if DEBUG` และลบแพ็กเกจที่ไม่ใช้ ข้อ 4 ข้อ 14 เดิม)

### 00.4 build และทดสอบ
- Visual Studio 2026 (.NET 9) เปิด `Tacetno433.sln` แล้ว Rebuild ต้องได้ 0 error 0 warning เครื่องใหม่ (เช่นโน้ตบุ๊คอีกเครื่อง) clone หรือ pull branch `Beta1` ล่าสุดก่อนเสมอ
- แชท cloud : `/build` (เครื่องใหม่ลง dotnet-sdk-8.0 กับรัน `.claude/skills/build/shader_setup.sh` ก่อน ดู `.claude/skills/build/SKILL.md`)
- `--shots turncheck` 145/145 / `--shots savecheck` 33/33 / `--shots turnsim` ใช้ 12 หรือ 24 seed (`TACET_SIM_SEED` 4330 กับ 1 ถึง 23 รันพร้อมกันได้)
- **ผู้ใช้ 31 ต.ค. : คนจริงเล่นแล้วเกมง่ายมาก ห้ามใช้ตัวเลขตัวจำลองตัดสินหรือรายงานว่ายากง่าย** (`BETA1_DIRECTION.md` 14.6) ใช้เทียบก่อนหลังเท่านั้น / ค่าล่าสุด (30 ต.ค. หลังก้อน 2) : NORMAL STRONG ราว 63% MAESTRO EXPERT ราว 83% (24 seed) กับตัวจำลองนิสัยใหม่ที่เลือกสกิลตามค่าและเดินเส้นทาง ส่วน 54% ของ 29 ต.ค. เป็นของนิสัยก่อนหน้า วัดเทียบต้องใช้ตัวจำลองตัวเดียวกัน (`TACET_SIM_NOPATH=1` ปิดเส้นทาง)
- `TACET_SIM_SET` ลองตัวเลขโดยไม่ build ใหม่ ตาราง MUSICIANS ในรายงานบอกว่าวงที่มีแต่ละคนชนะเท่าไร
- ชีต `docs/TACET_Balance.xlsx` : หลังแก้ตัวเลข `python3 docs/turn_balance_tabs.py <turnsim.txt>` (แท็บ Turn ทั้งหมดกับ Turn Skills)
- ภาพทดสอบหน้าต่อสู้ `battle*` (ใหม่ 30 ต.ค. `battlemoves` `battleinspired` `battleaim` `battlepick` `battleheal` `battletip` `battlechakram` 31 ต.ค. `battlefriend` ต่อท้าย clear ได้) ภาพจาก cloud ห้ามแทน `docs/screenshots/`

### 00.5 ที่ต้องรู้และที่ค้างอยู่
- PLAYTEST : SIGNATURE ใช้ 1 PERFECT (`BattleRules.PlaytestSignatureNeed` = 1) ผู้ใช้สั่งคงไว้ ห้ามแก้จนผู้ใช้บอก ค่าจริง 6
- ผู้ใช้ลองรันแล้ว 31 ต.ค. ทุกอย่างโอเค ยกเว้น INSPIRED เข้าใจยาก กับเป้าของสกิลที่ใช้กับเพื่อนไม่ชัด (`BETA1_DIRECTION.md` 14.4 14.5) ยังไม่ได้ส่งตัวอย่าง VFX ของ SHIELD CHAKRAM THE LESSON TURN OF THE HAND INSPIRED CHARGE GUARD
- ตัวจำลองเลือกสกิลตามค่าแล้ว (30 ต.ค.) CAPRICE ถูกเล่นและวัดได้ วงที่มี PAGANINI ยังต่ำกว่าเพื่อนราว 4.8%
- ราคาเลือดของ THE DEVIL'S BARGAIN กับ THE PACT ขึ้นบนการ์ดสกิลแล้ว (PAYS 12 LIFE) แต่ใต้ปุ่มยังเป็นเพชรลม
- โค้ดดวลเดิม (เกม rhythm `Battle/BattleState.cs` `Screens/DuelScreen*.cs`) ยังอยู่ ปิดด้วย `BattleRules.TurnBattleOn` ความสามารถเดิมของ 5 คนยังอยู่ใน `enum MusicianTrait` ให้ดวลเดิม
- เอกสารหลัก : `CLAUDE.md` ข้อ 00 / `docs/RESUME_PROMPT.md` สถานะล่าสุด / `docs/BETA1_DIRECTION.md` (11 รีวิวเกมเพลย์ 12 ก้อน 1) / `docs/SCREENS_LEVEL_UP.md` / `docs/CHARACTER_DESIGN.md` / `docs/BATTLE_SCENE_2_3D_CHANGE.md` / แผนอาร์ต `docs/BATTLE_ART_PLAN.md` `docs/UI_ART_PLAN.md` / คำต่อคำ `docs/USER_BRIEFS.md` หัวข้อ 13


## 0.0 แผนที่โค้ด Beta1 (ฉากต่อสู้ใหม่ + VFX, 28 ต.ค. รอบ 1–6)

ภาพของหน้าต่อสู้แยกจากกติกาทั้งหมด (`Battle/TurnBattle.cs` ไม่มีโค้ดวาด) · เอกสาร `BATTLE_SCENE_2_3D_CHANGE.md` ข้อ 9–14 · ตัวเลขใน `BattleRules` หัวข้อ Stage Feel, Stage Void, Hit Feel, Turn Feel

| อยากแก้อะไร | ไปไฟล์ไหน |
|---|---|
| ชั้นของภาพ (BACK SCENE LIGHT GLOW FINAL) bloom, QTE FOCUS, ENGULF, IMPACT FRAME (กลับสี แวบขาว ซูม เบลอ), vignette, grain | `Core/PostChain.cs` + `Content/Effects/Post.fx` |
| โหลด shader, noise ที่ทำบน CPU | `Core/Fx.cs` (Post, Sprite, Void, `Fx.Noise`) |
| ความมืดของ TACET : หลุมดำหลังศัตรู ความมืดบนพื้น ขอบคลื่นเสียง เงาสะท้อน รอยแตก เศษ คลื่นกระแทก | `Core/TacetVoid.cs` + `Content/Effects/Void.fx` (ไม่มี shader ใช้คลื่นเอียงเดิม `TacetField.DrawSlanted`) |
| VFX ตอนตี : SLASH SHOT SMASH CROSS INK LIGHTNING, สีของตัวละคร (ทอง ประกาย / แดง ไฟ), ปัด CLASH, ศัตรูตาย PULL, focus lines | `Core/HitFx.cs` (สไตล์ต่อกลุ่มเครื่องดนตรี `BattleScreen.StyleOf`, สีต่อคน `Musician.Spot` + `Palette.Spot*`, SIGNATURE `SignatureStyle` / `SignatureColors`) |
| ตัวเลขดาเมจแบบ pixel | `Core/PixelDigits.cs` · ป๊อปอัป `Screens/BattleScreen.Show.cs` (`SpawnNumber`) |
| ฉาก กล้อง parallax ไฟ นักดนตรีตัวหน้า สลับคน ศัตรูยืน ลำแสง | `Screens/BattleScreen.Stage.cs` |
| cut-in SIGNATURE, แสงของวง QTE (วาดทับหลังโลกถูกหรี่) | `Screens/BattleScreen.Show.cs` |
| อ่านภาพ animation ของทีมอาร์ต (PNG / Aseprite) | `Core/FrameArt.cs` · โฟลเดอร์ `Content/Art/Battle/` · แผนอาร์ต `BATTLE_ART_PLAN.md` |
| LESS FLASHING | `Core/Settings.cs` `ReduceFlashing` · หน้า `Screens/SettingsScreen.cs` แถว 07 · `BattleRules.LessFlash` |
| CLEAR FIGHT (หน้าต่อสู้อ่านง่าย 29 ต.ค.) | `Core/Settings.cs` `ClearFight` เซฟ `clearfight=` · หน้า `Screens/SettingsScreen.cs` แถว 08 · วาดใน `Screens/BattleScreen.cs` (ค้น `CLEAR FIGHT` : `DrawClearPlate` `DrawAimedAt` `DrawIntent` `DrawBand` `DrawBottom`) · ตัวเลข `BattleRules.ClearFocusDim` `ClearFocusPool` `ClearPlate` |
| PLAYTEST (SIGNATURE 1 แต้ม) | `BattleRules.PlaytestSignatureNeed` (0 = ค่าจริง) |
| ระบบตวัดไม้ใหม่ ONE RULE (29 ต.ค.) : โน้ต = ลูกศรใหญ่ในวง + วงแหวน ไม่มีขนาด f mf p แล้ว, คำ HOLD SHAKE AGAIN ใต้โน้ตพิเศษ, โน้ตค้างแค่กดค้าง | หน้าตา `Core/NoteGlyph.cs` (`Note` `BigArrow` `RollNote`) · ตัดสินการตวัด `Screens/BattleScreen.cs` (`UpdateSwipe` `UpdateHold` `UpdateRoll` `Land`) · กติกา `Battle/TurnBattle.cs` (`NoteResult` `SwipeScale`) · `BETA1_DIRECTION.md` ข้อ 10 |
| ตวัดแล้วตีทันที (NOTE BY NOTE) | `TurnBattle.BeginSkill` `SkillNote` `EndSkill` `SkillTargetsDown` (`UseSkill` เรียกทั้งสามต่อกัน ใช้ในตัวจำลองกับ turncheck) · หน้าจอ `BattleScreen.StartSkill` `Land` `FinishSwipe` |
| กล้องที่ขยับตามไม้ (ACTION : ซูมทีละขั้น เตะ เอียง สั่น ท่าปิด ตีรัวตอนโน้ตรัวกับโน้ตค้าง ปัด COUNTER นักดนตรีพุ่ง JAB) | `Core/StageCamera.cs` · หัวข้อ ACTION ใน `Screens/BattleScreen.Stage.cs` (`SwipeAction` `ParryAction` `RapidBlow` `ActionPoint` `WorldFocus`) · โลกวาดผ่านกล้องด้วย `PostChain.ShowFinal` แบบหมุนได้ · ป้ายชื่อ ท่าถัดไป ตัวเลขบนตัวละครตามกล้องด้วย `BattleScreen.BeginCameraBatch` · ตัวเลข `BattleRules` หัวข้อ Camera Feel |
| ภาพทดสอบกลางสกิล | `--shots battlecombo battlefinish battleroll battlehold` |
| ภาพทดสอบ | `--shots battle battlewin battlelose battleslash battleshot battlesmash battlecross battleink battlegold battlefire battlecutin battlekill battlebolt battleboltsoft battleimpact` (`BattleScreen.ShowForPicture`) |

**ยกระดับทุกหน้าจอ (28 ต.ค. รอบตัวอย่าง, 29 ต.ค. คำในแผ่นเฉียง ม่านผ้าจริง รอบ 1)** · เอกสาร `SCREENS_LEVEL_UP.md` ข้อ 10 13 14 · ตัวเลขใน `BattleRules` หัวข้อ UI Feel

| อยากแก้อะไร | ไปไฟล์ไหน |
|---|---|
| เลนส์ของทุกหน้า (แสงสปอต bloom vignette สว่างจากดำตอนเข้า) และหน้าไหนวาดเอง (`OwnLens` `HasWorld` `DrawWorld` `DrawLights` `DrawGlow` `Lens` `Dark`) | `Core/ScreenManager.cs` + `Core/PostChain.cs` (`DrawPage` `PageLook` `ShowPage`) |
| หลุม TACET บนหน้าอื่น | `TacetVoid.PlaceOnPage` (shader `Void.fx` ค่า `Fine`) |
| ปุ่มแบบผสม (รายการเมนู ปุ่มคำสั่ง ตัวเลข แถบหมึก กล่องคีย์) | `Core/Ui.cs` (`MenuItem` `Command` `Number` `InkBar` `KeyBox`) + จังหวะ `Core/UiMotion.cs` |
| แถบชื่อหน้าแนวตั้ง แถบเลื่อน ตัวเลือกคำ วงกลมบอกหน้า (รอบ 3) | `Core/Ui.cs` (`TitleBar` `Fader` `Choice` `Steps`) ใช้ที่ Archive Settings HOW TO PLAY Pause |
| เส้นคั่นวงกลม วงแหวนมีขีด วงแหวนบนพื้น หลุมนิ่งแบบรูปทรง | `Core/Ornament.cs` (`OrbitRule` `TickRing` `FloorRing` `StillHole`) · `StillHole` ใช้เฉพาะสิ่งที่วาดทับหน้า (เมนูหยุด) หน้าปกติใช้ `TacetVoid` |
| เปลี่ยนหน้าปกติ (แผ่นเฉียงหลายชั้น คำในแผ่นดำ = ชื่อหน้าที่กำลังไป) / ช่วงสำคัญ (วงแหวนกวาด LIGHT DARK CLOSE) / ม่าน (เข้าออกหน้าเลือกวาทยากร) | `Core/SlantWipe.cs` (`SetWord`) + คำของแต่ละหน้า `GameScreen.WipeWord` (WIPE WORD) / `Core/RingSweep.cs` เรียกด้วย `ScreenManager.SweepNext` หรือ `EntersWithSweep` ของหน้า / `Core/Curtain.cs` (`UsesCurtain`) |
| ม่านผ้าจริง (REAL CLOTH, 29 ต.ค.) : จีบบนราว ADVANCED PART position based dynamics, ชายผ้าแกว่ง (HEM), ตาข่ายสามเหลี่ยม | `Core/Curtain.cs` (`Begin` `Update` `Draw`) เรียกจาก `ScreenManager.Change/Update` · ตัวเลข `BattleRules.UiCurtain*` · สี `Palette.CurtainLight` |
| การ์ด 3D แบบ Balatro (พลิก เอียงตามเมาส์ แถบแสง) : วาดหน้าการ์ดลงภาพใน `PreDraw` แล้วโชว์เป็นสี่เหลี่ยมใน 3D ใน `Draw` | `Core/Card3D.cs` (`Picture` `BeginPicture` `Batch` `Draw` `AddLight`) · ใช้ที่ `Screens/MotifRewardScreen.cs` · หน้าการ์ด `MotifArt.CardFace` `CardBack` · ตัวเลข `BattleRules.UiCard*` `UiRarityLight` |
| หน้ารอบ 1 (29 ต.ค.) : Reward (THE SPOILS) / Shop (THE ARCHIVIST, ของที่ซื้อบินขึ้นแถบบน FLY UP, ข้อความคนขายเขียนทีละบรรทัด) / Event (แสงเย็นวูบ) / Rest (โคมหายใจ) | `Screens/MotifRewardScreen.cs` / `ShopScreen.cs` (ปลายทาง `RunHud.BadgeCentre` `LifeAt` `ShardsAt` `BandAt`, ซ่อนตราที่ยังบินอยู่ `RunHud.Arriving`) / `EventScreen.cs` (`Falter`) / `RestScreen.cs` (`Breath`) |
| แผนอาร์ตของหน้าจอ | `docs/UI_ART_PLAN.md` |
| ฟอนต์ (OFL) | `Content/Fonts/` + `.spritefont` 4 ไฟล์ และชุดใหญ่ของแต่ละตัว `*_15x` `*_2x` `*_3x` (12 ไฟล์ แก้ขนาดฟอนต์ต้องแก้ทั้งชุด) |
| เต็มจอภาพคม (SHARP SCREEN) : วาดที่ความละเอียดจอจริง ทุกหน้ายังใช้ตำแหน่ง 1280×720 ผ่าน `Sharp.Transform` (**ทุก `SpriteBatch.Begin` ที่วาดลงหน้าต้องใส่**) ตัวหนังสือจากฟอนต์ชุดใหญ่ เส้นและรูปทรงลงพิกเซลจริง | `Core/Sharp.cs` (`Scale` `Dot` `Snap` `Edge` `MaxScale`) + `Core/SharpText.cs` + `Core/Gfx.cs` + `TacetGame.UpdateView/SetSharp` · `SCREENS_LEVEL_UP.md` ข้อ 11 |
| หน้า Title / Result และ Defeat | `Screens/TitleScreen.cs` / `Screens/ResultScreen.cs` |
| หน้าเลือกวาทยากร (แผง 3 แผ่นเอียงตัดจากภาพ ชี้แล้วตั้งตรง สีไล่ขึ้น) / โปรไฟล์ (ภาพกลางจอ หน้าใกล้ 224) : 29 ต.ค. รอบ 2 | `Screens/ConductorSelectScreen.cs` (`CutRows` `PaintingRect`) / `Screens/ConductorDetailScreen.cs` · สีของวาทยากร `Conductor.Spot` + `Palette.LightningBlue` `SpotPrism` `SpotHoly` (29 ต.ค. แทน SpotCrimson Violet Teal ที่ลบแล้ว) · วาทยากรที่โชว์ `ConductorList.Shown` · ตัวเลข `BattleRules.UiGallery*` `UiProfile*` |
| เปลี่ยนหน้าแบบส่งภาพต่อกัน (SHARED PICTURE : ไม่มีอะไรปิด ไม่สว่างจากดำ) | `ScreenManager.Cut` (หน้าเก่าเล่นทางออกเอง หน้าใหม่เริ่มจากภาพที่ตำแหน่งเดิม) |
| แผนที่ทั้งชั้น (DAG แบบ STAIRCASE + BFS, 29 ต.ค.) : สร้างตอนเปิดชั้น ห้องที่เลือกได้ ตำแหน่ง ทางที่เดินมา เซฟ | `Data/FloorMap.cs` · `RunState.BuildMap` `OfferPlaces` `RestoreMap` `ReplaceChosen` `Map` `MapAt` `MapPath` · เซฟ `map=` `mappath=` ใน `Data/SaveFile.cs` · ตรวจด้วย `--shots savecheck` (map reach) |
| ธีมวาทยากร AURA รอบภาพ (พายุ ฟ้าผ่า / เศษปริซึม สปอตไลต์ / แสงทองในความมืด, 29 ต.ค. `SCREENS_LEVEL_UP.md` ข้อ 23) · สีวาทยากร · ชื่อยาวสองบรรทัด | `Core/ConductorAura.cs` · `Conductor.Aura` `SpotAt` `ShortName` ใน `Data/Conductor.cs` · สี `Palette.SpotPrism` `SpotHoly` `Prism()` · ตัวเลข `BattleRules.UiAura*` · ใช้ใน `Screens/ConductorSelectScreen.cs` `ConductorDetailScreen.cs` · แคป `gallery@28` `gallery1` `gallery2` `detail` `detail1` `detail2` |
| หน้าแผนที่แบบมองไกล (perspective ถนนเข้าหาหลุม กล้องเดินทีละแถว บอส วงไหลเข้าหลุม การ์ดด้านข้าง กล้องพุ่ง, 29 ต.ค. `SCREENS_LEVEL_UP.md` ข้อ 22) | `Screens/RouteScreen.cs` (ค่าคงที่ `Road Layout` · `ToScreen` `OnFloor` `Project` `DrawBoss` `DrawBack`) · ตัวเลข `BattleRules.UiRoute*` · แคป `routestart` `route` `routeboss` `routedive@52` `routewalk@20` |
| นักดนตรีในตำนาน 5 คน (PAGANINI QUANTZ PFUNDT DUPORT PUNTO, 29 ต.ค. `CHARACTER_DESIGN.md` ข้อ 5) : ช่อง 0 1 2 6 7 ของ `All` ฉายา อาวุธ | `Data/Musician.cs` (`MusicianList.Shown` · ฟิลด์ `Epithet` `Weapon` `WeaponAt` `WeaponNow` `TierNames`) · ตัวจำลองใช้ `simRoster` ใน `Core/DebugShots.TurnSim.cs` |
| ไม่มียุคแล้ว ชั้น = ท่อนเพลง PRELUDE NOCTURNE CODA (29 ต.ค. `BETA1_DIRECTION.md` ข้อ 6) : บอสตามท่อน ฉากต่อสู้ตามท่อน เซฟเก่า | `Data/Movement.cs` (`MovementList.Names` `Of`) · `RunState.Movement` · `Enemy.Movement` `EnemyList.Pick` · ฉาก `<ท่อน>_far.png` ใน `Screens/BattleScreen.Stage.cs` · `ArtBank.Stage` · เซฟเก่า : `era=` ไม่อ่านแล้ว ห้อง CROSSING กลายเป็น ??? (`RunState.RestoreMap`) · ลบแล้ว `Data/Era.cs` `Screens/EraChoiceScreen.cs` `Core/PanelStrip.cs` |
| เสาภาพนักดนตรี (หน้าต่างเอียงตัดภาพ ชื่อ/ การ์ดสกิล สีไล่ขึ้น) กล่องอธิบายคนที่ชี้ เพชรขั้นอาวุธ / ภาพ pixel ของหน้าจอจาก `Content/Art/Ui/` / ตัดภาพด้วยหน้าต่างเอียง | `Core/BandColumn.cs` (`Picture` `Words` `Detail` `SkillText` `TierPips`) / `Core/UiArt.cs` (`BustOf` `FaceOf`) / `Ui.SlantCut` `ColourRise` `SlantEdge` `SlantSlot` |
| **ทีม 3 คน (THE BAND OF THREE, 29 ต.ค. ค่ำ `BETA1_DIRECTION.md` ข้อ 9)** : หน้าเลือก 3 จาก 5 ต้นรัน (THE AUDITION) / หน้าวง 3 เสาลากสลับ / ตั้งวง เซฟเก่าที่มีม้านั่ง / ลำดับต้นรัน วาทยากร แล้วทีม แล้วชื่อ แล้วการ์ดท่อน | `Screens/TeamPickScreen.cs` / `Screens/BandScreen.cs` / `RunState.PickBand` `BandReady` `KeepBand` `Lineup` (เซฟ `order=`) / `ConductorDetailScreen.Confirm` `BandNameScreen` `ChapterScreen` · ไม่มี CALL BENCH ที่นั่ง HIRE แล้ว (`Screens/RecruitScreen.cs` ลบแล้ว) |
| ของที่เคยผูกกับ CALL (ทำอย่างอื่นในช่องเดิม) | ENTRANCE `TurnBattle.MoveScale` (`Fighter.Entered`) · GREEN ROOM `TurnBattle.GreenRoom` · THE CUE `TurnBattle.OnCue` `LastSoloist` · THE DAMPER `TurnBattle.AttackBreath` · FIRST CHAIR `Archive.FirstChair` (`RunState.PickBand`) · เหตุการณ์ `Data/GameEvent.cs` |
| ขั้นอาวุธ I II III (WEAPON TIERS) : พลังต่อขั้น ขึ้นขั้นจากที่พัก ห้อง LUTHIER ร้าน เหตุการณ์ บอส REFORGE / VFX ตอนตีใหญ่ขึ้นตามขั้น | `Musician.Rehearsed` (เซฟ `rehearsed=`) `RunState.Rehearse` `CanRehearse` `LeastTrained` `AnyToTrain` `PowerOf` · `BattleRules.WeaponTierPower` `RehearseMax` / `HitFx.Hit(... tier)` + `BattleRules.HitTierSize` (เรียกจาก `BattleScreen` ค้น `WEAPON TIERS`) **ผู้ใช้ 29 ต.ค. : ขั้นสูงยังไม่ชัด เห็นความต่างนิดเดียว ต้องแก้ทีหลัง** |
| ห้องใหม่บนแผนที่ LUTHIER (อาวุธฟรี 1 ขั้น) TREASURE (MOTIF ฟรี 1 ใบ) : น้ำหนักห้อง / หน้าจอ | `NodeType.Luthier` `Treasure` + `RouteNodeInfo` ใน `Data/RouteNode.cs` · `RunState.PickType` · `RunFlow.Enter` / `Screens/LuthierScreen.cs` · TREASURE ใช้ `MotifRewardScreen(Kind.Treasure)` · ภาพ `Art/Places/luthier.png` `Art/Route/route_luthier.png` `route_treasure.png` |
| รางวัลบอส : อัปเกรดใหญ่ 1 จาก 3 (REFORGE อาวุธทุกคนขึ้นขั้น + MOTIF TREASURED 2 ใบ) | `Screens/MotifRewardScreen.cs` (`reforgeCard` `RarityOf`) · หน้าการ์ด `MotifArt.ReforgeFace` |
| ร้านใหม่ : MOTIF 2 ช่อง WEAPON TUNING หรือ SECTIONAL + REROLL แพงขึ้นทีละครั้ง / ลูกเต๋าของห้องที่เซฟไว้ | `Screens/ShopScreen.cs` (`Stock` `Ware` `Reroll`) · ตัวเลข `BattleRules` หัวข้อ Shop / `RunState.PlaceSeed` ตั้งใน `RunFlow.Enter` เซฟ `placeseed=` |
| เงินตามอันดับ (RANK = ครึ่ง PERFECT % ครึ่งเลือดที่เหลือ) หน้า Result 6 แถว | `BattleState.RankScore` `RankIndex` `RankScale` `ShardsBeforeRank` `ShardsEarned` (`NotesPlayed` `LifeKept` จาก `BattleScreen.Finish`) · `BattleRules.RankAt` `RankShards` · `Screens/ResultScreen.cs` (`ApplyWin` `DrawRank`) |
| **ชุดสกิลใหม่ 5 คน THE FIVE KITS (29 ต.ค. ดึก `BETA1_DIRECTION.md` 12.2)** : สกิล / ความสามารถติดตัว / กติกา SHIELD THE LESSON CHAKRAM LANCE TURN OF THE HAND THE HUNTING CALL THE DEVIL'S BARGAIN BREATH CONTROL THE SCAR / คำเด้งกับป้ายบนจอ / ทดสอบ | `Data/Skill.cs` (`Shield` `Lesson` `Bounce` `Revenge` `OthersOnly`) / `Data/Musician.cs` (ท้าย `enum MusicianTrait`) / `Battle/TurnBattle.cs` (`CanAim` `PaysInLife` `HitTarget` `CoverOf` `StepAside` `NoteTargets` `TurnOfTheHand` `CueScale` `Taught` ค้นชื่อตัวพิมพ์ใหญ่) · ตัวเลข `BattleRules` หัวข้อ Musician Traits / `BattleScreen.ShowEvents` `DrawBand` / `Core/DebugShots.TurnLegends.cs` · ภาพ `battlekit` · ตัวจำลองนิสัยใหม่ + ตาราง MUSICIANS + `TACET_SIM_SET` ใน `Core/DebugShots.TurnSim.cs` · แท็บ Turn Skills ของ `TACET_Balance.xlsx` |
| **ท่าใหม่ของศัตรู CHARGE GUARD (30 ต.ค. `BETA1_DIRECTION.md` 12.3)** : ลิสต์ท่า / ทุบท่าชาร์จ / ทะลุโล่ | `Data/Enemy.cs` (`EnemyMove` `Moves` `GuardBlock`) / `TurnBattle` ค้นคำ CHARGE GUARD BREAK PIERCE (`PlanIntent` `NextMove` `BreakCharge` `Strike` `Aims`) / ตัวเลข `BattleRules` หัวข้อ ENEMY MOVES / ทดสอบ `Core/DebugShots.TurnMoves.cs` |
| **PATH INSPIRED ชนะได้เลือก SIGNATURE โต BONDS (30 ต.ค. 13.1 ถึง 13.3)** | การ์ด 30 ใบ `Data/Path.cs` (`PathList` แก้ที่นี่) / สายสัมพันธ์ `Data/Bond.cs` / กติกาใน `TurnBattle` ค้นคำ PATH INSPIRED SIGNATURE GROWS BONDS / หน้าเลือก `Screens/MotifRewardScreen.cs` (`Kind.Path`) กับในไฟต์ `BattleScreen` (`OpenStepPick`) / เซฟ `paths=` / ทดสอบ `Core/DebugShots.TurnPaths.cs` |
| **หน้าต่อสู้ใหม่ MARKS AND AIM (30 ต.ค. 13.4)** : วงเล็ง เครื่องหมายบนเพื่อน เครื่องหมายเป้า เส้นโค้งเล็ง การ์ดสกิล ตัวอย่างเลือด ไอคอนกับคำอธิบาย | `Screens/BattleScreen.Marks.cs` (ทั้งหมดของหน้าจอ) / ตัวเลขจากกติกา `Battle/TurnBattle.Preview.cs` (`Preview` `MovePreview`) / ไอคอน `Ui.Icon` กับ `UiArt.IconOf` (โฟลเดอร์ `Content/Art/Ui/Icons/` รายชื่อ `UI_ART_PLAN.md` 2.5) / ตัวเลข `BattleRules` หัวข้อ Aim Feel (`AimArc` ปิดเส้นโค้ง) / ทดสอบ `Core/DebugShots.TurnPreview.cs` |
| **FRIEND PICK (31 ต.ค. 15.1)** : สกิลที่ใช้กับเพื่อนคนเดียว (SHIELD THE LESSON) ขึ้นการ์ดเพื่อนใบใหญ่เหนือปุ่มคำสั่ง คลิกหรือกดเลข | `Screens/BattleScreen.Friends.cs` (ทั้งหมดของแถว) / ต่อเข้า `BattleScreen.UpdateTarget` กับ `DrawBottom` (ซ่อนเลขบนปุ่มระหว่างเลือก) / การ์ดสกิลเลื่อนขึ้นเหนือแถว (`cardShown` ใน `BattleScreen.Marks.cs`) / ภาพ `battlefriend` |
| ความยากหลังเอา CALL ออก (เลือดวง x1.5 อาวุธขั้นละ 2 MAESTRO 1.3) | `BattleRules.TurnHp` `WeaponTierPower` `MaestroTurnDamage` · ผลตัวจำลอง `BETA1_DIRECTION.md` 9.6 · ตัวจำลองนับ RANK ด้วย (`--shots turnsim` หัวข้อ RANKS) |
| รอบ 2 ที่เหลือ (29 ต.ค.) : การ์ดเปิดท่อน (หลุมจริง ชื่อโผล่จากขอบหลุม) / จบรัน (ม่านผ้าจริง วงยืนโค้งเป็นเสา ตัวเลขนับขึ้น) / ตั้งชื่อวง / Archive (หัวข้อตั้ง menu1 ECHOES เลขใหญ่) | `Screens/ChapterScreen.cs` / `CurtainCallScreen.cs` / `BandNameScreen.cs` / `ArchiveScreen.cs` · `SCREENS_LEVEL_UP.md` ข้อ 19 |
| ภาพทดสอบ | `--shots titlereveal resultquiet wipe sweep sweepdark sweepclose` และ `result@3,result@30` (ชื่อซ้ำได้ ภาพชื่อ `ชื่อ_เฟรม`) · 29 ต.ค. : `wipeshop wiperest wipeevent wipereward wipechapter wipeband wipesettings` (แผ่นเฉียงไปหน้านั้น) `wipegallery` (ม่าน) `rewardpick rewardtilt shopbuy` · 29 ต.ค. ค่ำ : `teampick teampickfull luthier luthierdone@60 treasure rewardboss battletier@4` |
| ภาพทดสอบเต็มจอ / เทียบก่อนหลัง | `--scale 1.5` (1920×1080, 2 = 2560×1440, 3 = 4K) · `--steady` (1/60 วินาทีต่อภาพ ล็อกการสุ่มของรัน รันซ้ำได้ภาพเดิมทุกพิกเซล) |

## 0. แผนที่โค้ด Alpha 7 (การต่อสู้แบบผลัดตา, อัปเดต 24 ต.ค. ขั้น 1–15)

ตัวสลับ : `Data/BattleRules.cs` `TurnBattleOn` (true = ผลัดตา, false = กลับไปดวลเดิมทั้งเกม)

| อยากแก้อะไร | ไปไฟล์ไหน |
|---|---|
| กติกาการต่อสู้ทั้งหมด (ห้อง ONE BAR ตี ปัด สกิล สถานะ เสียศูนย์ COUNTER CADENZA ท่าวาทยากร ความสามารถติดตัว) ไม่มีโค้ดวาด | `Battle/TurnBattle.cs` (ค้นชื่อกติกาตัวพิมพ์ใหญ่ เช่น `CAPRICE` `MOMENTUM`) |
| ONE BAR (ขั้น 12) : เขียนห้อง `PlanAttack` `PlanSkill` `Unplan` `PlayBar` ม้านั่ง `Bench` `Call` `CallCost` แทรกกลางห้อง `CUT IN` (`BuildOrder`) | `Battle/TurnBattle.cs` หัวข้อ ONE BAR · ตัวเลข `BattleRules` หัวข้อ ONE BAR และ PACKS |
| ขั้น 15 : ที่นั่งเริ่ม `RunState.StartSeats` ใครนั่งบนเวทีเริ่มไฟต์ (`BattleScreen.LoadContent` onStage) · ม้านั่ง `TurnBenchSize` · ร้าน EXTRA SEAT · เส้นเลือดบนการ์ด `MusicianArt.Card` · DAMAGE SHARE ในตัวจำลอง | `Data/RunState.cs` `Screens/BattleScreen.cs` `Screens/ShopScreen.cs` `Core/MusicianArt.cs` · ตัวเลข `BattleRules` (TurnAttackScale TurnFloorLife TurnNoteDamage) |
| ขั้น 14 บอส : DECREE (`Decree` `BreaksDecree`) · THE COURT (`IntentAll` `NoteHits`, ลดดาเมจใน `Strike`) · BY TOUCH (`MissedWays` ใน `ParryNote`/`PlanIntent`, ที่นั่งไล่ใน `PickTarget`) · การตวัดที่จะได้ MISS รอก่อน (MISS WAIT ใน `BattleScreen.UpdateSwipe`) · เมาส์กลางจอตอนเริ่มตวัด (`Input.MoveMouseTo`) | `Battle/TurnBattle.cs` · `Screens/BattleScreen.cs` · ตัวเลข `BattleRules` หัวข้อ Bosses of step 14 · ทดสอบ `Core/DebugShots.TurnContent.cs` (`TurnCheckBosses`) |
| ขั้น 13 : ดาเมจของวงเข้าศัตรู `Strike` (HOLDS THE ROOM, SEGUE) · ความแรงของท่า `MoveScale` (DOWNBEAT ENTRANCE UNISON) · `LastWord` `GreenRoom` · เป้าของศัตรู `PickTarget(enemy)` (ON CUE, FIRST IN LINE) · แทรกสองครั้ง TWICE A BAR ใน `BuildOrder` | `Battle/TurnBattle.cs` · ตัวเลข `BattleRules` หัวข้อ ONE BAR motifs / ONE BAR enemies · ศัตรูใหม่ `Data/Enemy.cs` ท้ายรายการ · ทดสอบ `Core/DebugShots.TurnContent.cs` |
| ศัตรูหลายตัวในรัน (PACKS) และเซฟ | `RunState.BeginBattle` `PackEscorts` · `BattleState.Pack` · `SaveFile` (`pack=`) · `EnemyList.PickOther` |
| หน้าต่อสู้ (เขียนห้อง ม้านั่ง ATTACK พุ่งตี game feel วงแหวน QTE แถบห้อง ป้ายสถานะ ไฟต์ฝึก PRACTICE) | `Screens/BattleScreen.cs` |
| ตัวเลขบาลานซ์ทุกตัว | `Data/BattleRules.cs` หัวข้อ TURN BATTLE (บนสุด) และ Turn Feel |
| สกิลนักดนตรี 18 สกิล (2 ต่อคน ตามลำดับ `MusicianList`) · ชนิดสกิล SKILL KIND (โจมตี ป้องกัน ฮีล เสริม Beta1) | `Data/Skill.cs` (`SkillKind` `Skill.Kind`) · สีของชนิด `Core/Palette.cs` (`Palette.Kind`) |
| ความสามารถติดตัวนักดนตรีในโหมดผลัดตา | ข้อความ `Data/Musician.cs` `TurnTraitText` · ผล `TurnBattle.cs` (ค้นชื่อความสามารถ) |
| วาทยากร 6 คน (PASSIVE / SIGNATURE) และคนที่โชว์ในแกลเลอรี | `Data/Conductor.cs` (`TurnKit`, `Turn*` text, `ConductorList.Shown`) · ผลอยู่ `TurnBattle.cs` (`UseSignature`, `Judge`, `CanUse`) |
| ศัตรู 9 ตัว (เลือด ความเร็ว ท่อนโจมตี กลไก) + ศัตรูฝึก | `Data/Enemy.cs` (`TurnHp` `TurnSpeed` `Phrases` `TurnTraitText` `TurnPower` · `EnemyList.Practice`) |
| Motif 21 ใบในโหมดผลัดตา | `Data/Motif.cs` `TurnText` · ผลอยู่ `TurnBattle.cs` (ค้นชื่อ Motif) |
| เลือดนักดนตรีข้ามการต่อสู้ / เหตุการณ์ ร้าน พัก | `Data/RunState.cs` (`ChangeStamina` `LifeOf` `SetLife`) · เซฟ `Data/SaveFile.cs` บรรทัด `life=` |
| ของถาวรหลังรัน THE ARCHIVE | `Data/Archive.cs` · `Screens/ArchiveScreen.cs` · ได้ ECHOES ที่ `Screens/CurtainCallScreen.cs` · เซฟ `archive.txt` |
| Tutorial / HOW TO PLAY / ข้อความหน้าแรก | `Screens/TutorialScreen.cs` (`For.Turn`) · `Screens/GuideScreen.cs` · `Screens/TitleScreen.cs` `turnTips` |
| ทดสอบ | `--shots turncheck` (`Core/DebugShots.Turn.cs` + `DebugShots.TurnKits.cs` + `TurnContent.cs` + `TurnLegends.cs`) · `--shots turnsim` (`Core/DebugShots.TurnSim.cs`, `TACET_SIM_SEED` `TACET_SIM_KIT` `TACET_SIM_SET`) · `--shots savecheck` · ภาพ `battle*` `practice*` `archive` |


อัปเดต : 6 ตุลาคม 2026 (รอบ 15 · ส่งงาน 29 ก.ย. · นำเสนอ 30 ก.ย.) · repo `github.com/JuzJuz03HKR/Alphodite` branch **`Alpha6`** (รอบ 15 · `Release` = zip ตัวที่ส่งอย่างเดียว · `Alpha5` = รอบ 14 · `Alpha4` = รอบ 12–13 · `Alpha3` = รอบ 9–11b · รอบ 6–8 อยู่ `Alpha2`) · กติกาสำหรับ Claude อยู่ `CLAUDE.md` · อ่านคู่กับ [`PROJECT_STATUS.md`](PROJECT_STATUS.md) (รายละเอียดทุกระบบ)
และ [`ASSET_LIST.md`](ASSET_LIST.md) (ไฟล์ภาพ/เสียงที่ต้องใส่) · สิ่งที่ผู้ใช้สั่งคำต่อคำ [`USER_BRIEFS.md`](USER_BRIEFS.md) · prompt หลัง compact [`RESUME_PROMPT.md`](RESUME_PROMPT.md) · เนื้อหาสไลด์ [`TACET_Slides_29Sep.md`](TACET_Slides_29Sep.md) · แผนภาพลูป `diagrams/core_game_loop.png` · `gameplay_loop.png` (+ `*_detail.png`)

---

## 1. สรุปใน 1 นาที (ประวัติ : เกม rhythm `Alpha6` ถึง 6 ต.ค. ของตอนนี้อยู่ข้อ 00)

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

## 2. ความคืบหน้า (ประวัติ : `Alpha6` 6 ต.ค. ของตอนนี้อยู่ข้อ 00.2)

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

## 3. โครงสร้างโค้ด (ที่ต้องรู้ก่อนแก้ เขียนตอน `Alpha6` แผนที่โค้ดตอนนี้อยู่ข้อ 0.0 กับ 0)

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
| เมนูหยุด / หน้าไหนกด Esc แล้วเปิดเมนู | `Screens/PauseMenu.cs` (Beta1 รอบ 3 : แถบดำเฉียง FERMATA วงแหวนรอบหลุม · `OpenSettings` `AskReturn`) · กล่องถาม `Core/ConfirmBox.cs` · `GameScreen.CanPause` / `UsesEscape` / `Paused` / `Resumed` ใน `Core/ScreenManager.cs` |
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

## 4. งานที่เหลือ (ประวัติ : `Alpha6` ของตอนนี้อยู่ข้อ 00.3)

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
10.1 **ช่องโหลดภาพ UI skin (8 ต.ค., ยังไม่ได้ทำ)** : ทีมอาร์ตจะวาดตาม `docs/TACET_UI_Asset_List.xlsx` (126 ไฟล์ `Content/Art/UI/<code id>.png`) · ต้องเพิ่มการโหลดแบบ ArtBank + วาดแบบ 3-slice / 9-slice ใน `Core/Ui.cs` (Button Panel Tag CapsuleBar ModeSwitch และชิ้น Beta1 Command MenuItem Fader Choice) และจุดที่วาดเองในหน้าจอ · ไม่มีไฟล์ = ใช้แบบโค้ดเดิม

### ทำทีหลัง (P3)
11. ภาษาไทยจริง (ต้อง build ฟอนต์ที่มีตัวไทย + ตารางข้อความ 2 ภาษา ตอนนี้หน้าตั้งค่าเลือกได้แต่ยังไม่แปล)
12. ปลดล็อกถาวร (คอนดักเตอร์ 2–5 ตอนนี้เปิดหมดตั้งแต่แรก)
13. โปรเจกต์ทดสอบของ `BattleState` · README หน้า repo · tag checkpoint
14. `DebugShots` ห่อด้วย `#if DEBUG` หรือเอาออกตอนส่ง · แพ็กเกจ `MonoGame.Extended` ใน `.csproj` ไม่ได้ใช้ ลบได้

## 5. ปัญหาที่รู้อยู่ (ประวัติ : `Alpha6` ของตอนนี้อยู่ข้อ 00.5)

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

## 6. สิ่งที่ผู้ใช้ตัดสินใจแล้ว (ประวัติ : `Alpha6` การตัดสินใจตอนนี้อยู่ `CLAUDE.md` ข้อ 00)

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
