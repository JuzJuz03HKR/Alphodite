# FINALE9 : ค้น VFX จากเกมอื่น (2 พ.ย. แชท FINALE9 ผู้ใช้สั่ง `USER_BRIEFS.md` 13.114)

วิธีค้น : WebSearch 14 ครั้ง (ผลเป็นบทสรุปของหน้าค้น) / **WebFetch เปิดหน้าเว็บไม่ได้** (realtimevfx.com กับ notion.site ขึ้น ENOTFOUND เหมือนแชทก่อนๆ) จึงไม่ได้เห็นภาพหรือคลิปจริง ข้อที่มาจากความรู้ของไอริสเองติดป้าย (ความรู้เดิม ไม่ได้ยืนยันจากเว็บรอบนี้)

## Judgement Cut / Judgement Cut End (Devil May Cry, Vergil) : ต้นแบบของ "ฟันผ่ามิติ"
- หน้าจอแตกเหมือนกระจก เส้นพลังงานสีฟ้าลากตามรอยฟันที่มองไม่เห็นนับไม่ถ้วน โลกหยุดนิ่งชั่วขณะ (สรุปจากหลายแหล่ง แหล่งอ่อน)
- กระทู้ realtimevfx ที่พยายามทำตาม : มีมากกว่า 20 ชิ้นเอฟเฟกต์ เช่น รอยฟันดำเร็ว รอยฟันฟ้าบาง รอยฟันบางที่บิดภาพ แสงเรืองมาจากทรงกลมที่บิดภาพซ้อนบนรอยฟัน และมีสีเหลื่อม (chromatic aberration) ที่รอยฟัน
- งานทำใหม่ใน Unity HDRP : รอยฟัน + ชิ้นกระจกที่หักเหแสง + post process (lens distortion, chromatic aberration, ความสว่างทั้งจอกระพริบเป็นจังหวะ)
- แหล่ง : https://realtimevfx.com/t/recreating-a-specific-vfx/16147 / https://iamam_ayem.artstation.com/projects/DLKN8A / https://huaxu.dev/eu/wiki/constructs/vergil/skills

## Hoshimi Miyabi (Zenless Zone Zero)
- ท่าไม้ตาย Lingering Snow : ฟันแรงหลายครั้งทั่วพื้นที่ข้างหน้า แล้ว "หยุด" ชั่วครู่ ก่อนท่าปิด (Finishing Move) ดาเมจ Frost ก้อนใหญ่ / ธาตุ Frost เป็นของเธอคนเดียว ไม่ใช่ Ice
- Shimotsuki : เก็บดาบเข้าฝักแล้วชาร์จ (อยู่ในท่านี้ไม่โดนตี) ปล่อยแล้วชักดาบฟันตามระดับชาร์จ / ชาร์จแต่ละขั้นมีรอยฟันออกมา
- หน้าตาของ VFX ไม่มีแหล่งที่เป็นข้อความบรรยาย (ต้องดูคลิป ซึ่งดูไม่ได้)
- (ความรู้เดิม ไม่ได้ยืนยัน) ท่าใหญ่ของเธอใช้ความนิ่งก่อนฟัน จอขาวหรือมืดลง เส้นฟันสีฟ้าอ่อนพาดทั้งจอ แล้วแตกเป็นเกล็ดหิมะ
- แหล่ง : https://www.prydwen.gg/zenless/characters/miyabi / https://esports.gg/news/zenless-zone-zero-zzz/zenless-zone-zero-zzz-hoshimi-miyabi / https://www.icy-veins.com/zenless-zone-zero/hoshimi-miyabi-profile-skills-mindscapes

## Worldless
- ตัวละครทำจากรูปทรงแบนทึบ เคลื่อนไหวลื่นมาก ("buttery smooth") / รีวิวติว่านามธรรมเกินไปจนอ่านยากบางที่ / การต่อสู้ชมว่าลึก ตอบสนองดี ให้รางวัลการอ่านจังหวะศัตรู
- แหล่ง : https://entertainium.co/2023/12/02/worldless-review/ / https://game8.co/articles/reviews/worldless-review / https://godisageek.com/reviews/worldless-review/
- ภาพจริงที่ผู้ใช้เคยส่ง 7 ภาพอยู่ `docs/art_refs/` (ดูได้ ห้ามใส่ในเกม)

## หลักทั่วไปของ VFX ในเกม
- GDC 2017 "Artistic Principles of VFX" (Jason Keyser Riot / Hadidjah Chamberlain Blizzard) : รูปทรงกับพื้นที่ผล / ความต่างกับจุดโฟกัส / สีกับตัวตน / จังหวะกับระดับอันตราย / กฎหลัก "ต้องเห็นว่าเกิดอะไรขึ้น"
- League of Legends VFX style guide : ความสำคัญของเอฟเฟกต์คุมด้วยขนาด รูปทรง จังหวะ ค่าความสว่าง ความอิ่มสี ความทึบ / ลดความรก / มีจุดหลักหนึ่งจุดกับส่วนรอง
- รูปทรงเป็นสิ่งที่ตาจับได้เร็วที่สุด (Halo Infinite แยกอาวุธด้วยรูปแสงที่ปากกระบอก)
- จังหวะ : ทุก VFX เป็นแบบกระแทก (impulse) หรือแบบไหล (rhythm) / ง้าง กระแทก จางหาย / hit stop ราว 2 ถึง 5 เฟรม
- "ทุกอย่างด่วนหมด = ไม่มีจุดให้ตามอง"
- รอยฟัน : แกนเป็นวงเสี้ยวมีมาสก์ เนื้อในเลื่อนด้วย noise สีจากแถบไล่สีกับ bloom / ให้หางสั้นลงแทนการเร็วขึ้น / ชั้นที่สองช้ากว่า มืดกว่า กลับสี เป็นรอยที่ค้างหลังฟัน / ใช้การบิดภาพมากกว่าสี (แนว NieR)
- ท่าชาร์จของศัตรู : ความเข้มต้องโตขึ้นจนถึงจังหวะปล่อย / แยก "เลน" ของสัญญาณอันตรายออกจากเอฟเฟกต์ตอนโดน / เปลี่ยนสี ท่า หรือมีรอยบนพื้นก่อนดาเมจ
- ฮีลกับโล่ : ต้องไม่เหมือนดาเมจหรือบัฟ / ใช้สีที่คนคุ้น / เพิ่มรูปทรงแนวตั้งให้อ่านออกบนพื้นรก / โล่ทรงกลมมีลายเลื่อน มีการสลายและบอกเวลาที่เหลือ
- แหล่ง : https://nexus.leagueoflegends.com/wp-content/uploads/2017/10/VFX_Styleguide_final_public_hidpjqwx7lqyx0pjj3ss.pdf / https://www.vfxapprentice.com/blog/10-league-of-legends-vfx-design-tips / https://www.vfxapprentice.com/blog/the-soul-of-effects-what-is-timing-in-vfx / https://gamedeveloper.com/design/vfx-as-game-design-tools-the-ludology-of-vfx-in-god-of-war-ragnarok-and-halo-infinite / https://realtimevfx.com/t/venom-slash-breakdown-of-the-effect-included/18903 / https://realtimevfx.com/t/wip-sword-slash-attack/1272 / https://medium.com/mighty-bear-games/how-ux-design-can-help-with-vfx-a0925f80e0f0 / https://www.gamedeveloper.com/art/vfx-breakdown-combining-2d-and-3d-in-blightbound / https://gamedeveloper.com/design/enemy-attacks-and-telegraphing

## เกมอื่นที่เกี่ยว
- Zenless Zone Zero : ทีมเอาความหนักและการตอบสนองของทุกครั้งที่ตีมาจาก Street Fighter 6 / มี impact frame / ท่าตอบโต้แบบ bullet time ทำด้วยการปรับความเร็วกล้อง / กล้องสั่นตอนท่าใหญ่ (ปิดไม่ได้ ถูกขอให้มีตัวเลือก) / เอฟเฟกต์จ้าแต่ทำให้ตีแล้วรู้สึกดี (https://pcgamer.com/games/action/zenless-zone-zeros-producer-has-spent-an-ungodly-amount-of-time-playing-street-fighter-6-which-makes-the-whisperings-of-a-collaboration-even-more-understandable / https://dotgg.gg/zenless-zone-zero/?p=1540)
- Honkai: Star Rail : แถบ Toughness ทำตัวเหมือนโล่ ตีจนหมด = Weakness Break ผลต่างกันตามธาตุ / ท่าไม้ตายของ Acheron เปลี่ยนรูปลักษณ์และเวลาช้าลง เทคนิคของเธอทำจอขาวดำทั้งจอเหลือสีแดงที่ตัวเธอ (ข่าวหลุด) (https://honkai-star-rail.fandom.com/wiki/Toughness / https://sportskeeda.com/esports/news-honkai-star-rail-acheron-leaks-gameplay-idle-animations-instant-kill)
- Hades : เอฟเฟกต์เน้นบอกว่าโดนเมื่อไหร่และเปลี่ยนพื้นที่ระหว่างเรากับศัตรูยังไง / ท่าบอสบอกด้วยท่าทางมากกว่าเอฟเฟกต์ (แหล่งอ่อน https://80.lv/articles/a-behind-the-scenes-look-at-the-effects-in-hades/)
- Chaos Zero Nightmare : รีวิวชมว่าการต่อสู้ดูดีมาก particle เป็นส่วนของความอลังการ ไม่มีข้อมูลละเอียด (https://finalboss.io/chaos-zero-nightmare-looks-incredible-in-battle-but)
