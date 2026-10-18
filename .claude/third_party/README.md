# Skill จากภายนอกที่อยู่ใน `.claude/skills/`

คัดลอกมาเมื่อ 18 ต.ค. 2026 ตามที่ผู้ใช้อนุญาต (บันทึกใน `docs/USER_BRIEFS.md` ข้อ 12)
ไฟล์ใบอนุญาตของแต่ละแหล่งอยู่ในโฟลเดอร์ย่อยของโฟลเดอร์นี้ ห้ามลบ

| แหล่ง | commit | ใบอนุญาต | skill ที่คัดลอก |
|---|---|---|---|
| [obra/superpowers](https://github.com/obra/superpowers) v7.0.0 | `bb92a77` | MIT | brainstorming, writing-plans, executing-plans, subagent-driven-development, dispatching-parallel-agents, systematic-debugging, test-driven-development, verification-before-completion, requesting-code-review, receiving-code-review, using-git-worktrees, finishing-a-development-branch |
| [gamedev-skills/awesome-gamedev-agent-skills](https://github.com/gamedev-skills/awesome-gamedev-agent-skills) | `0a70cfc` | Apache-2.0 (มี NOTICE) | game-feel, game-ui-ux |
| [alca259/monogame-skills](https://github.com/alca259/monogame-skills) | `ddbe387` | MIT | monogame-game-loop, monogame-scenes, monogame-input, monogame-2d, monogame-math, monogame-audio, monogame-content-pipeline, monogame-effects, monogame-shaders |
| [JuliusBrussee/caveman](https://github.com/JuliusBrussee/caveman) v4.0.1 | `bad48a0` | Apache-2.0 (มี NOTICE) | caveman |

## ข้อควรรู้

- **Superpowers** : ในไฟล์ของมันเรียกกันว่า `superpowers:ชื่อ` (เช่น `superpowers:test-driven-development`) ที่นี่ไม่มีคำนำหน้า ให้ใช้ skill ชื่อเดียวกันแทน
  ไม่ได้เอา hook ตอนเปิดเซสชันกับ skill using-superpowers มา เพราะมันบังคับให้เรียก skill ทุกงาน ถ้าอยากเปิดต้องคุยกับผู้ใช้ก่อน
  ไม่ได้เอา writing-skills กับ diagnosing-superpowers (ไม่เกี่ยวกับงานเกม)
- **game-feel / game-ui-ux** : ตัวอย่างโค้ดในนั้นเป็น Godot / Unity ใช้แนวคิดแล้วเขียนเป็น MonoGame เอง
- **monogame-skills** : ผู้เขียน archive repo แล้ว (22 ก.ย. 2026) ไม่มีอัปเดตอีก · ไม่ได้เอาชุด ui-* (เกมมี `Core/Ui.cs` ของตัวเอง), 3d, camera-modes, extended-* (ลบ MonoGame.Extended ไปแล้ว) และโฟลเดอร์ instructions (บังคับใช้ `docs/specs/` ที่โปรเจกต์นี้ไม่มี)
- **caveman** : เอาแค่ skill หลัก ไม่ได้เอา hook, ultracave, megacave และเครื่องมืออื่นในชุด · hook ของ plugin ไม่ทำงานในแชท cloud จึงสั่งเปิดผ่าน CLAUDE.md แทน
- อัปเดต : clone แหล่งเดิม แล้วคัดลอกโฟลเดอร์ skill ทับ จากนั้นแก้ commit ในตารางนี้
