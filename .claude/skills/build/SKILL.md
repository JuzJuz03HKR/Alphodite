---
name: build
description: คอมไพล์เกมในแชท cloud (Linux) เพื่อเช็ก error และถ้าสั่ง ก็แคปหน้าจอ จำลองบาลานซ์ ตรวจสมดุล หรือทดสอบเซฟ
argument-hint: "[shots <หน้า,หน้า> | simulate | audit | savecheck]"
disable-model-invocation: true
---

# /build : คอมไพล์และทดสอบในแชท cloud

สิ่งที่สั่งเพิ่ม (อาจว่าง = คอมไพล์อย่างเดียว): $ARGUMENTS

เกมเขียนสำหรับ Windows (.NET 9, ฟอนต์ Windows) แชท cloud เป็น Linux จึงต้อง build จาก**สำเนา**ที่ปรับให้เข้ากับ Linux
**ห้ามแก้ไฟล์ใน repo เพื่อให้ build ผ่าน**

## ขั้นตอน

1. **เตรียมเครื่อง** (ข้ามข้อที่มีอยู่แล้ว)
   - `dotnet --list-sdks` ต้องมี 8.0 ถ้าไม่มี : `apt-get install -y dotnet-sdk-8.0` (ตัวติดตั้งของ Microsoft ถูกบล็อก · เครื่องใหม่ขึ้น `Couldn't find any package` ให้ `apt-get update` ก่อน)
   - ฟอนต์ `/usr/share/fonts/truetype/liberation/LiberationSerif-Regular.ttf` และ `/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf` ถ้าไม่มี : `apt-get install -y fonts-liberation fonts-dejavu-core`
   - ถ้าจะรันเกม (shots, simulate, audit, savecheck) ต้องมี `xvfb-run` ถ้าไม่มี : `apt-get install -y xvfb`
   - **shader (.fx, Beta1 28 ต.ค.)** : ถ้า `Content.mgcb` มีไฟล์ .fx ต้องตั้ง Wine ก่อน : `bash .claude/skills/build/shader_setup.sh <scratchpad>/shader` แล้ว `export MGFXC_WINE_PATH=<scratchpad>/shader/winemonogame` ก่อน `dotnet build`
     (เว็บ Microsoft กับ Mozilla ถูกบล็อก สคริปต์นี้เอา `d3dcompiler_47.dll` ตัวจริงจากแพ็กเกจ `cef.redist.x64` บน NuGet และใช้ `fxcshim.c` แทน `fxccs.dll` ของ MonoGame · ผลคอมไพล์เหมือนบน Windows เพราะใช้ DLL ตัวเดียวกัน)
2. **ทำสำเนา** ไปที่ `<scratchpad>/build/` (ลบสำเนาเก่าก่อน) คัดลอก `Tacetno433.sln` และโฟลเดอร์ `Tacetno433/` โดยไม่เอา `bin/` กับ `obj/` (เครื่อง cloud ไม่มี rsync ใช้ `tar --exclude='Tacetno433/bin' --exclude='Tacetno433/obj' -cf - Tacetno433 | tar -xf - -C <scratchpad>/build`)
3. **ปรับสำเนา**
   - `Tacetno433/Tacetno433.csproj` : `net9.0` เป็น `net8.0`
   - `Tacetno433/Content/*.spritefont` : `C:/Windows/Fonts/pala.ttf` เป็นไฟล์ Liberation Serif และ `Bahnschrift` เป็นไฟล์ DejaVu Sans (ใช้ path เต็มข้างบน)
4. **คอมไพล์** : `dotnet build` ในโฟลเดอร์สำเนา
   - เป้าคือ **0 error 0 warning** รายงาน error ขึ้นต้น `CS` เป็น path ของ repo พร้อมเลขบรรทัด เช่น `Tacetno433/Battle/TurnBattle.cs:42`
   - ถ้าพังแค่ขั้น Content (ฟอนต์หรือ asset) แต่ C# ผ่าน ให้บอกแยกกันชัดๆ
5. **รันเครื่องมือทดสอบ** (เฉพาะเมื่อสั่งมา) ในโฟลเดอร์ `bin/Debug/net8.0` ของสำเนา
   ```
   xvfb-run -a -s "-screen 0 1280x720x24" env LIBGL_ALWAYS_SOFTWARE=1 SDL_AUDIODRIVER=dummy dotnet Tacetno433.dll --shots <สิ่งที่สั่ง> --out <โฟลเดอร์ผล>
   ```
   - `shots` : ชื่อหน้าคั่นด้วยจุลภาค เช่น `duel@150,band` (รายชื่อหน้าอยู่ `docs/PROJECT_STATUS.md` หัวข้อ 11 และ `docs/ALPHA6_HISTORY.md` ข้อ 2) แล้วเปิดดูภาพที่ได้
   - `simulate` ผลอยู่ `simulate.txt` · `audit` ผลอยู่ `audit.txt` (**ลบไฟล์ผลเก่าก่อนรันทุกครั้ง**) · `savecheck` ทดสอบเซฟ/โหลด
   - ผลจำลองแกว่ง ±5 ถึง 8% ถ้าจะเทียบก่อนกับหลัง ให้รันหลายรอบแล้วเฉลี่ย
   - ห้ามใช้ `pkill -f` กับคำที่อยู่ในคำสั่งของตัวเอง (จะฆ่า shell ตัวเอง)
6. **รายงาน** : error/warning กี่ตัว (ถ้ามี บอกไฟล์และบรรทัด) และผลที่สั่งรัน
   - ภาพที่แคปใช้ฟอนต์ต่างจากของจริง **ห้ามเอาไปแทนภาพใน `docs/screenshots/`**
   - ปิดท้ายด้วยการบอกผู้ใช้ให้ **Rebuild ใน Visual Studio** และเล่นทดสอบบน Windows ทุกครั้ง
