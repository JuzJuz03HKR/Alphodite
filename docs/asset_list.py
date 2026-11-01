#!/usr/bin/env python3
"""TACET 4'33 : the art and sound list, kept in ONE place (Beta1, 1 Nov 2026).

    python3 docs/asset_list.py

1. Fills the tables of docs/ASSET_LIST.md between <!-- AUTO:<name> --> and <!-- /AUTO:<name> -->
   (the words around the tables are written by hand in the .md).
2. Writes docs/TACET_Asset_List.xlsx (tabs : อ่านก่อน, Art, Animations, Sound) for the team to tick.
   What the team typed (Status, Start - Deadline, Link, Comment, hit frames) is read back from the old
   file first and put back on the same file name, so making the file again never loses their work.
3. "มีแล้ว" is read from the repo itself : the file exists, it is in Content.mgcb when the game needs
   that, and a picture's real size is checked against the canvas in the list.

Every size and file name below was read from the code (Beta1) :
  battle art     Core/FrameArt.cs, Screens/BattleScreen.Stage.cs (EnemySize, foreBox, LayerW), BattleScreen.Boss.cs
  pages art      Core/UiArt.cs (Busts, Faces, Icons), BattleScreen.Marks.cs (IcoFile), Core/MotifArt.cs (keyFile)
  Content.mgcb   Core/ArtBank.cs (Conductors, Places, Events, Route, Baton)
  sound          Audio/SoundBank.cs (SfxFiles, MusicFiles) and every SoundBank.Play in the Beta1 pages
"""
import hashlib
import json
import os
import re
import sys

from openpyxl import Workbook, load_workbook
from openpyxl.formatting.rule import CellIsRule, FormulaRule
from openpyxl.styles import Alignment, Border, Font, PatternFill, Side
from openpyxl.worksheet.datavalidation import DataValidation

try:
    from PIL import Image
except ImportError:          # the size check is skipped without Pillow
    Image = None

DOCS = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(DOCS)
GAME = os.path.join(REPO, "Tacetno433")
MD = os.path.join(DOCS, "ASSET_LIST.md")
XLSX = os.path.join(DOCS, "TACET_Asset_List.xlsx")
MGCB = os.path.join(GAME, "Content", "Content.mgcb")
UPDATED = "1 พ.ย. 2026"

# MonoGame's own icon (checked by eye 1 Nov) : the game icon counts as done once these change
DEFAULT_ICON_MD5 = {"Icon.ico": "37df66d725ffec0cad491732c2b57ebf", "Icon.bmp": "5243057d8d80845cbe8b5b02137d0807"}

STATUS = ["NonStart", "In Progress", "Done", "Fix", "None"]   # the art team's own words (TACET_UI_Asset_List.xlsx)
STARS = {3: "⭐⭐⭐", 2: "⭐⭐", 1: "⭐"}

# ---------------------------------------------------------------------------------------------
# ART : one row per file, or per animated character set (kind "set")
# ---------------------------------------------------------------------------------------------
ART = []


def art(sec, path, name, canvas, screen, style, where, prio, stable=True, note="", kind="png",
        mgcb=False, optional=False, check=True, size_text=None):
    """path : inside Tacetno433/ without the extension (a set : the character's key, both ways of handing over)."""
    ART.append(dict(sec=sec, path=path, name=name, canvas=canvas, screen=screen, style=style, where=where,
                    prio=prio, stable=stable, note=note, kind=kind, mgcb=mgcb, optional=optional, check=check,
                    size_text=size_text))


PIX2 = "pixel ขยาย 2 เท่า"
PIX1 = "pixel ขนาดจริง"

# 1.1 the five legends on the battle page (MusicianList.Shown 0 1 2 6 7)
LEGENDS = [("paganini", "PAGANINI", "ไวโอลิน"), ("quantz", "QUANTZ", "ฟลุต"), ("pfundt", "PFUNDT", "ทิมปานี"),
           ("duport", "DUPORT", "เชลโล"), ("punto", "PUNTO", "ฮอร์น")]
for key, name, inst in LEGENDS:
    art("musicians", "Content/Art/Battle/Musicians/" + key, name + " (" + inst + ")", (160, 256), "320×512", PIX2,
        "หน้าต่อสู้ : คนที่วาทยากรชี้ยืนตัวใหญ่ซ้ายหน้า มองจากด้านหลัง", 3, kind="set",
        note="ท่าในแท็บ Animations บอกเฟรมที่โดนของ attack skill parry")

# 1.2 enemies (EnemyList.All, BATTLE_ART_PLAN 3, BattleScreen.Stage EnemySize)
NORMAL, ELITE, BOSS = ((112, 128), "224×256"), ((144, 160), "288×320"), ((224, 224), "448×448")
ENEMIES = [
    ("hush", "HUSH", "ธรรมดา", NORMAL, "The first thing to go is the echo."),
    ("dead_air", "DEAD AIR", "ธรรมดา", NORMAL, "It waits between your notes."),
    ("the_lull", "THE LULL", "ธรรมดา", NORMAL, "Soft, patient, and never finished."),
    ("static", "STATIC", "ธรรมดา", NORMAL, "It sounds like something. It is not."),
    ("room_tone", "ROOM TONE", "ธรรมดา", NORMAL, "The sound of an empty room, holding the others up."),
    ("the_cue", "THE CUE", "ธรรมดา", NORMAL, "It waits in the wings for whoever takes the solo."),
    ("the_damper", "THE DAMPER", "ธรรมดา", NORMAL, "A felt hand resting on every string."),
    ("stutter", "STUTTER", "ธรรมดา", NORMAL, "It cannot finish a word, so it never stops starting."),
    ("the_mute_choir", "THE MUTE CHOIR", "elite", ELITE, "A hundred mouths, open, making nothing."),
    ("white_noise", "WHITE NOISE", "elite", ELITE, "Every beat, all the time, forever."),
    ("the_round", "THE ROUND", "elite", ELITE, "It sings your turn back at you, in your own order."),
    ("requiem", "REQUIEM", "บอส ร่างปีศาจของ Mozart", BOSS, "The piece that was never finished. ท่าไม้ตาย LACRIMOSA"),
    ("the_nameless_master", "THE NAMELESS MASTER", "บอส ร่างปีศาจของ Fanny", BOSS, "He plays a phrase. You must answer better. ท่าไม้ตาย ITALIEN"),
    ("the_devil_s_string", "THE DEVIL'S STRING", "บอส ร่างปีศาจของ Paganini", BOSS, "One string, one bow, one bargain. ท่าไม้ตาย ON ONE STRING"),
    ("the_deaf_crown", "THE DEAF CROWN", "บอส ร่างปีศาจของ Beethoven", BOSS, "It cannot hear you. It only gives orders. ท่าไม้ตาย THE NINTH"),
    ("the_staff", "THE STAFF", "บอส ร่างปีศาจของ Lully", BOSS, "It cannot walk. It beats time on the floor instead. ท่าไม้ตาย TE DEUM"),
    ("the_blind_cantor", "THE BLIND CANTOR", "บอส ร่างปีศาจของ Bach", BOSS, "It plays by touch, one voice after another. ท่าไม้ตาย IN THE DARK"),
    ("practice", "PRACTICE", "หุ่นซ้อมของไฟต์ฝึก", NORMAL, "ไฟต์ฝึกจาก Tutorial ซึ่งจะทำใหม่ทีหลัง"),
]
# SHARED LOOKS (the user, 1 Nov : 3 or 4 enemy pictures, every enemy kept, the pictures shared). A look is named after
# its first enemy. An enemy with no file of its own wears its look's file, told apart by its brightness and a small mark
# drawn in code (Data/Enemy.cs Looks LookShade Mark, BETA1_DIRECTION 24). Not by size : pixel art drawn at a size that
# is not a whole number of pixels gets uneven pixels. The table below (LOOK_OF) is read from that code.
DESIGNS = [
    ("hush", "แบบที่ 1 เงียบนุ่ม", NORMAL, "HUSH, THE LULL, ROOM TONE, PRACTICE (หุ่นซ้อม)",
     "ตัวเงียบที่ค่อยๆ กลืนเสียง ฮีลตัวเองและคุ้มกันพวก"),
    ("static", "แบบที่ 2 สัญญาณรบกวน", NORMAL, "STATIC, STUTTER, THE CUE", "ตัวกวน ซ่อนลูกศร แทรกตา เล็งคนที่เพิ่งใช้สกิล"),
    ("the_damper", "แบบที่ 3 ของหนักกดเสียง", NORMAL, "THE DAMPER, DEAD AIR",
     "ตัวหนัก ตั้งโล่และชาร์จตีแรง / ถ้าทีมทำได้แค่ 3 แบบ THE DAMPER ใช้แบบที่ 1 DEAD AIR ใช้แบบที่ 2"),
    ("the_mute_choir", "แบบ elite คณะร้องไร้เสียง", ELITE, "THE MUTE CHOIR, WHITE NOISE, THE ROUND", "elite ทั้ง 3 ตัว ตัวใหญ่กว่า"),
]
for key, name, size, users, line in DESIGNS:
    art("enemies", "Content/Art/Battle/Enemies/" + key, name, size[0], size[1], PIX2, "หน้าต่อสู้ ฝั่งขวา", 3,
        kind="set", note="ใช้กับ " + users + " / " + line)
for key, name, kind, size, line in ENEMIES:
    if kind.startswith("บอส"):
        art("enemies", "Content/Art/Battle/Enemies/" + key, name + " (" + kind + ")", size[0], size[1], PIX2,
            "หน้าต่อสู้บอส", 2, kind="set", note=line)

# 1.3 backdrops, one set per movement (MovementList.Names), four layers (BattleScreen.Stage layerFile)
MOVEMENTS = [("prelude", "PRELUDE ชั้น 1"), ("nocturne", "NOCTURNE ชั้น 2"), ("coda", "CODA ชั้น 3")]
LAYERS = [("far", "ไกลสุด ทึบทั้งภาพ ท้องฟ้า ผนัง วิว", False), ("mid", "กลาง โปร่งใส สิ่งปลูกสร้าง เสา เงาคน", False),
          ("stage", "พื้นเวทีที่วงยืน โปร่งใส ครึ่งล่างของภาพ", False), ("front", "หน้าสุด โปร่งใส ของใกล้กล้องสีดำ เช่นขอบม่าน", True)]
for mkey, mname in MOVEMENTS:
    for lkey, lline, optional in LAYERS:
        if optional:
            prio = 1
        elif mkey == "prelude":
            prio = 2 if lkey == "mid" else 3
        else:
            prio = 2
        art("stages", "Content/Art/Battle/Stages/" + mkey + "_" + lkey, mname + " ชั้นภาพ " + lkey, (704, 360), "1408×720", PIX2,
            "หน้าต่อสู้ ฉากหลังของท่อนนี้", prio, note=lline, optional=optional)

# 1.4 the bosses' cut in (BattleScreen.Boss : THE TELL 230 x 220, THE STRIKE 360 x 400, the picture stretched)
for key, name, kind, size, line in ENEMIES:
    if not kind.startswith("บอส"):
        continue
    big = line.split("ท่าไม้ตาย ")[1]
    art("bosscut", "Content/Art/Battle/Bosses/" + key + "_cutin", name + " ท่าไม้ตาย " + big, (180, 200),
        "THE STRIKE 360×400 / THE TELL 230×220", PIX2, "ไฟต์บอส ตอนเตือนและตอนปล่อยท่าไม้ตาย", 2, stable=False,
        note="หน้าหรือครึ่งตัวของร่างปีศาจ ท่าทางกำลังจะปล่อยท่า")

# 2 the three conductors (ConductorList.Shown 3 8 1, ArtBank.ConductorKey : a fixed file name, Conductor.FileKey, 1 Nov)
CONDUCTORS = [("the_unhearing", "THE UNHEARING (Beethoven)", "ฟ้าสายฟ้ากับพายุ"),
              ("the_unsigned", "FANNY MENDELSSOHN AND ??? (Fanny กับเงาของ Felix)", "ผลึกคริสตัลสีรุ้ง Felix เป็นเงาอยู่ขวาของภาพ"),
              ("the_metronome", "THE METRONOME (J. S. Bach)", "แสงศักดิ์สิทธิ์สีทอง ผ้าพันตา รอบตัวมืด")]
for key, name, theme in CONDUCTORS:
    warn = ""
    art("conductors", "Content/Art/Conductors/gallery_" + key, name + " ภาพเต็ม", (302, 402), "302×402", PIX1,
        "หน้าเลือกวาทยากร หน้าโปรไฟล์ เสาแรกของหน้าวง CURTAIN CALL", 2, mgcb=True, note=theme + warn)
    art("conductors", "Content/Art/Conductors/face_" + key, name + " หน้า", (224, 224), "224×224", PIX1,
        "หน้าโปรไฟล์วาทยากร", 1, mgcb=True, note=warn.strip(" /"))
    art("conductors", "Content/Art/Conductors/cutin_" + key, name + " ภาพ FINALE", (180, 190), "360×380", PIX2,
        "หน้าต่อสู้ ตอนใช้ FINALE ทะลุแถบดำบนล่าง", 2, stable=False, mgcb=True,
        note="โค้ดจะเปลี่ยนให้วาดแบบ pixel คม (ข้อ 9)" + warn)

# 3 the legends on the other pages (UiArt : Busts, Faces)
for key, name, inst in LEGENDS:
    art("uimusicians", "Content/Art/Ui/Busts/bust_" + key, name + " เสาภาพ", (160, 256), "160×256", PIX1,
        "THE AUDITION (เลือก 3 จาก 5) หน้าวง ตั้งชื่อวง WORKSHOP CURTAIN CALL", 2)
for key, name, inst in LEGENDS:
    art("uimusicians", "Content/Art/Ui/Faces/face_" + key, name + " หน้า", (112, 112), "112×112", PIX1,
        "การ์ดเลือกเพื่อนในไฟต์ หน้า REHEARSAL", 2, note="REHEARSAL ตอนนี้ย่อเหลือ 96×96 ไอริสจะแก้ (ข้อ 9)")

# 4 icons (BattleScreen.Marks IcoFile and command icons, MotifArt keyFile), drawn at their own size
ICONS = [
    ("skill_attack", "ATTACK", (24, 24), "ปุ่มคำสั่ง ATTACK ของทุกคน (โจมตี แดง)"),
    ("skill_caprice", "CAPRICE", (24, 24), "สกิล PAGANINI (โจมตี แดง)"),
    ("skill_il_cannone", "IL CANNONE", (24, 24), "สกิล PAGANINI (โจมตี แดง)"),
    ("skill_soft_air", "SOFT AIR", (24, 24), "สกิล QUANTZ (ฮีล เขียว)"),
    ("skill_the_lesson", "THE LESSON", (24, 24), "สกิล QUANTZ (เสริม ทอง)"),
    ("skill_timpani_roll", "TIMPANI ROLL", (24, 24), "สกิล PFUNDT (โจมตี แดง)"),
    ("skill_thunder_call", "THUNDER CALL", (24, 24), "สกิล PFUNDT (ป้องกัน ฟ้ากระจก)"),
    ("skill_shield", "SHIELD", (24, 24), "สกิล DUPORT (ป้องกัน ฟ้ากระจก)"),
    ("skill_lance", "LANCE", (24, 24), "สกิล DUPORT (โจมตี แดง)"),
    ("skill_fanfare", "FANFARE", (24, 24), "สกิล PUNTO (เสริม ทอง)"),
    ("skill_chakram", "CHAKRAM", (24, 24), "สกิล PUNTO (โจมตี แดง)"),
    ("status_dissonance", "BURN", (22, 22), "ไฟ เสียเลือดทุกตาแล้วมอดลง / เครื่องหมายของ PAGANINI"),
    ("status_crescendo", "VOLUME", (22, 22), "ดังขึ้นคือแรงขึ้น สูงสุด 5 ขั้น พลาดแล้วหายหมด / เครื่องหมายของ PUNTO"),
    ("status_mute", "MUTED", (22, 22), "ใช้สกิลไม่ได้หนึ่งตา"),
    ("status_guard", "GUARD", (22, 22), "นักดนตรีรับดาเมจครึ่งเดียวจนถึงตาของตัวเอง"),
    ("status_echo", "ECHO", (22, 22), "ท่าถัดไปเล่นซ้ำอีกครั้ง (FINALE ของ THE METRONOME)"),
    ("status_shield", "SHIELD", (22, 22), "DUPORT ยืนบังเพื่อน / เครื่องหมายของ DUPORT"),
    ("status_lesson", "FOCUS", (22, 22), "สกิลถัดไปง่ายขึ้นและแรงขึ้น (THE LESSON)"),
    ("status_off_beat", "STUNNED", (22, 22), "แถบ STUN เต็ม เสียตาและโดนแรงขึ้น / เครื่องหมายของ PFUNDT"),
    ("status_cut_in", "CUT IN", (22, 22), "ศัตรูแทรกเข้ามาระหว่างตา (STUTTER)"),
    ("status_block", "ARMOR", (26, 26), "โล่ตัวเลขของศัตรู รับดาเมจก่อนเลือด เกมเขียนเลขทับกลาง ให้กลางภาพเรียบและเข้ม"),
    ("intent_attack", "ATTACK", (24, 24), "เจตนาศัตรู : จะตี"),
    ("intent_charge", "CHARGE", (24, 24), "เจตนาศัตรู : ตั้งท่าชาร์จ ตาหน้าตีโน้ตใหญ่แรง 3 เท่า"),
    ("intent_guard", "ARMOR UP", (24, 24), "เจตนาศัตรู : ตั้งโล่ ARMOR"),
    ("intent_rest", "HEAL", (24, 24), "เจตนาศัตรู : ฮีลแทนการตี"),
    ("intent_broken", "BROKEN", (24, 24), "เจตนาศัตรู : ท่าชาร์จพังแล้ว"),
    ("intent_everyone", "EVERYONE", (24, 24), "เจตนาศัตรู : ตีทั้งวง (THE STAFF)"),
    ("keyword_heal", "HEAL", (22, 22), "เครื่องหมายของ QUANTZ บนของ GIFT และที่ THE AUDITION"),
    ("keyword_band", "ALL", (22, 22), "เครื่องหมายของ GIFT ที่ใช้ได้ทั้งวง"),
]
for key, word, size, line in ICONS:
    group = "ไอคอนสกิล" if key.startswith("skill") else ("เจตนาศัตรู" if key.startswith("intent") else "ไอคอนสถานะ")
    where = {"ไอคอนสกิล": "ปุ่มคำสั่งในไฟต์ ซ้ายของชื่อ", "เจตนาศัตรู": "เหนือหัวศัตรู หน้าตัวเลขกับโน้ต",
             "ไอคอนสถานะ": "ใต้การ์ดวง ใต้ป้ายศัตรู ข้างเครื่องหมายบนเพื่อน"}[group]
    if key.startswith("keyword"):
        where = "การ์ดของ GIFT ร้าน หีบ THE AUDITION"
    if key == "status_block":
        where = "ซ้ายของป้ายชื่อศัตรูที่มี ARMOR"
    art("icons", "Content/Art/Ui/Icons/" + key, word + " (" + group + ")", size, "%d×%d" % size, PIX1, where, 3, note=line)

# legend forms : the third star renames the skill (Data/Rehearsal.cs), icon "skill_" + the new name, else the old icon
LEGEND_FORMS = [("CAPRICE No. 24", "CAPRICE"), ("DEVIL'S CANNON", "IL CANNONE"), ("SANSSOUCI", "SOFT AIR"),
                ("KING'S LESSON", "THE LESSON"), ("THUNDERCLAP", "TIMPANI ROLL"), ("DIE PAUKEN", "THUNDER CALL"),
                ("NAPOLEON'S DENT", "SHIELD"), ("THE LAST LANCE", "LANCE"), ("THE WHOLE HUNT", "FANFARE"),
                ("WHIRLWIND", "CHAKRAM")]


def key_of(name):
    """ArtBank.Key : lower case, every letter or digit kept, everything else becomes _"""
    return "".join(c if c.isalnum() else "_" for c in name.lower())


for legend, skill in LEGEND_FORMS:
    art("legendicons", "Content/Art/Ui/Icons/skill_" + key_of(legend), legend + " (ร่างในตำนานของ " + skill + ")", (24, 24),
        "24×24", PIX1, "ปุ่มคำสั่งหลังได้ดาวที่ 3", 1, optional=True, note="ไม่มีก็ใช้ไอคอนของ " + skill)

# 5 places, events, route cards (ArtBank, Content.mgcb)
art("places", "Content/Art/Places/shop", "ร้าน SHOP", (672, 470), "672×470", PIX1, "หน้าร้าน", 1, mgcb=True,
    note="ห้องหนังสือเก่า คนขาย THE ARCHIVIST ยืนกลางกรอบหลังเคาน์เตอร์ ใต้ลำแสงที่โค้ดใส่")
art("places", "Content/Art/Places/rest", "ห้องพัก REST", (960, 236), "960×236", PIX1, "หน้าห้องพัก และ THE WINGS ห้องก่อนบอส", 1,
    mgcb=True, note="แถบกว้างเหนือตัวเลือก วงนั่งพักรอบโคมดวงเดียว หันหลัง")
art("places", "Content/Art/Places/luthier", "โรงช่าง WORKSHOP", (520, 470), "520×470", PIX1,
    "หน้า WORKSHOP (ในโค้ดชื่อ LUTHIER) ซ้ายของหน้า ข้างเสา 3 ต้น", 1, mgcb=True, note="โรงช่างทำเครื่องดนตรี มีช่างในภาพได้")
EVENTS = ["THE BROKEN METRONOME", "A STREET MUSICIAN", "THE SILENT AUDIENCE", "A LOST SCORE", "TACET'S ECHO",
          "THE TEA HOUSE", "THE EMPTY CHAIR", "A RIVAL BAND", "A TUNING FORK", "A CRACKED BOW"]
for title in EVENTS:
    art("places", "Content/Art/Events/event_" + key_of(title), "เหตุการณ์ " + title, (520, 430), "520×430", PIX1,
        "หน้าเหตุการณ์", 1, mgcb=True, note="ล่าง 80 พิกเซลโดนเงาไล่ มีชื่อสถานที่เขียนทับ")
ROUTES = [("battle", "ไฟต์"), ("elite", "ไฟต์ ELITE"), ("boss", "บอส"), ("event", "เหตุการณ์"), ("shop", "ร้าน"),
          ("rest", "ห้องพัก และ THE WINGS"), ("luthier", "WORKSHOP"), ("treasure", "หีบ TREASURE")]
for key, word in ROUTES:
    art("places", "Content/Art/Route/route_" + key, "การ์ดห้อง " + word, (230, 142), "230×142", PIX1,
        "การ์ดข้างแผนที่ตอนชี้ห้อง", 1, stable=False, mgcb=True, note="งานแผนที่ในงานระบบอาจเปลี่ยนการ์ดนี้")

# 6 others
art("other", "Content/Art/Baton/baton", "ไม้บาตอง", None, "ยาว 118 หมุนตามการตวัด", PIX1,
    "เคอร์เซอร์ตอนตวัดไม้ในวงแหวน QTE", 2, mgcb=True,
    note="วาดตั้งขึ้น ด้ามล่าง ปลายบน แคนวาสสูง 118 กว้างตามรูป (เกมยืดความสูงภาพเป็น 118)")
art("other", "Content/Art/Ui/logo", "โลโก้เกม", None, "หน้า Title", PIX1,
    "หน้า Title แทนชื่อเกมที่เป็นตัวอักษรตอนนี้ และใช้ทำไอคอนเกม", 3, stable=False, check=False,
    size_text="แนะนำไม่เกิน 480×200", note="ทีมส่งพร้อมงานอาร์ต / ยังไม่มีช่องในโค้ด ไอริสทำช่องตอนงาน GUI แล้วบอกขนาดแน่นอน พื้นหลังโปร่งใส")
art("other", "Icon.ico", "ไอคอนเกม .ico", None, "ไฟล์ .exe และทาสก์บาร์", "ไอคอน",
    "ไอคอนของไฟล์เกม", 2, kind="gameicon", note="ไอริสทำจากโลโก้ (ไฟล์เดียวใส่ 16 32 48 256) / ตอนนี้ยังเป็นโลโก้ MonoGame")
art("other", "Icon.bmp", "ไอคอนเกม .bmp", (256, 256), "หน้าต่างเกม", "ไอคอน",
    "ไอคอนของหน้าต่างเกม", 2, kind="gameicon", note="ไอริสทำจากโลโก้ 256×256 / ตอนนี้ยังเป็นโลโก้ MonoGame")

# Animations : the names FrameArt looks for, and what the battle page plays (BattleScreen.Stage, BattleScreen)
MUST, TIME, NO, IRIS, FREE = "ต้องมี", "ถ้ามีเวลา", "ไม่วาด", "ไอริสทำ", "ไม่บังคับ"
MUSICIAN_ANIMS = [("idle", "4–8", "วนซ้ำ", "ทุกช่วงที่ไม่ได้ทำอะไร (ไม่มี ยืนนิ่งที่เฟรมแรกของ attack)", MUST),
                  ("attack", "6–10", "ครั้งเดียว", "ATTACK (บอกเฟรมที่โดน)", MUST),
                  ("skill", "6–12", "ครั้งเดียว", "สกิล (บอกเฟรมที่โดน) ไม่มี โค้ดใช้ attack แทน", TIME),
                  ("parry", "3–6", "ครั้งเดียว", "ปัดตอนศัตรูตี (บอกเฟรมที่โดน) ไม่มี โค้ดใช้ attack แทน", TIME),
                  ("hurt", "2–4", "ครั้งเดียว", "เสียเลือด ไม่มี โค้ดทำตัวสั่นกับแวบแทน", TIME),
                  ("ko", "4–8", "ค้างเฟรมสุดท้าย", "เลือดหมด ไม่มี โค้ดให้ค้างนิ่ง จมลงแล้วจางแทน", TIME),
                  ("enter", "4–8", "ครั้งเดียว", "สลับคนขึ้นหน้าจอ ไม่มี โค้ดเลื่อนเข้าแทน", TIME),
                  ("exit", "3–6", "ครั้งเดียว", "โค้ดยังไม่เรียก", FREE),
                  ("victory", "4–8", "วนซ้ำ", "โค้ดยังไม่เรียก", FREE)]
ENEMY_ANIMS = [("idle", "4–8", "วนซ้ำ", "ทุกช่วงที่ไม่ได้ทำอะไร (ไม่มี ยืนนิ่งที่เฟรมแรกของ attack)", MUST),
               ("attack", "6–10", "ครั้งเดียว", "ตี (บอกเฟรมที่โดน) ไม่มี โค้ดให้พุ่งเข้าหาวงแล้วกลับแทน", MUST),
               ("hurt", "2–4", "ครั้งเดียว", "โดนตี ไม่มี โค้ดทำตัวสั่นกับแวบแทน", TIME),
               ("death", "4–8", "ครั้งเดียว", "เกมสลายตัวเข้าหลุมให้อยู่แล้ว (ไม่มี ค้างนิ่งระหว่างสลาย)", FREE)]
ANIM_COLUMNS = ["idle", "attack", "skill", "parry", "hurt", "ko", "enter", "death", "exit", "victory"]

# PLAN (the user, 1 Nov, USER_BRIEFS 13.57 13.58) : the team draws the 3 conductors, the 5 legends, every boss, 3 or 4
# enemy looks and the logo, in two tiers. Everything else is not drawn : the pages are made to need no picture.
def plan_of(r):
    name = os.path.basename(r["path"])
    if r["kind"] == "gameicon":
        return IRIS                                   # made from the team's logo
    if r["sec"] == "musicians":
        return MUST
    if r["sec"] == "enemies":
        return TIME if name == "the_damper" else MUST
    if r["sec"] == "bosscut":
        return TIME
    if r["sec"] == "conductors":
        return MUST if name.startswith("gallery_") else TIME
    if r["sec"] == "uimusicians":
        return MUST if name.startswith("bust_") else TIME
    if name == "logo":
        return MUST
    return NO                                         # stages, icons, places, events, route cards, the baton


for r in ART:
    r["plan"] = plan_of(r)

# ---------------------------------------------------------------------------------------------
# SOUND
# ---------------------------------------------------------------------------------------------
SOUND = []


def sound(sec, file, kind, what, where, hooked, length, prio, note="", optional=False):
    SOUND.append(dict(sec=sec, file=file, kind=kind, what=what, where=where, hooked=hooked, length=length,
                      prio=prio, note=note, optional=optional))


LOOP = "วนทั้งไฟล์"
for file, what, where, prio in [
        ("title", "เพลงหน้าแรก", "Title, Settings, HOW TO PLAY, TEST ROOM", 3),
        ("gallery", "เพลงเลือกวาทยากร", "หน้าเลือกวาทยากรและหน้าโปรไฟล์", 2),
        ("route", "เพลงแผนที่", "แผนที่, THE AUDITION, หน้าวง", 3),
        ("battle", "เพลงไฟต์", "ไฟต์ธรรมดาและ ELITE", 3),
        ("boss", "เพลงไฟต์บอส", "ไฟต์บอสทั้ง 6 ตัว", 3),
        ("victory", "เพลงชนะ", "หน้าผลชนะไฟต์, หน้าเลือกรางวัล", 2),
        ("defeat", "เพลงแพ้", "หน้าผลแพ้", 2),
        ("shop", "เพลงร้าน", "ร้าน", 1),
        ("event", "เพลงเหตุการณ์", "หน้าเหตุการณ์", 1),
        ("rest", "เพลงพัก", "ห้องพัก, THE WINGS, WORKSHOP, REHEARSAL", 1),
        ("ending", "เพลงจบรัน", "CURTAIN CALL", 2)]:
    sound("music", "Content/Audio/Music/" + file, "Music", what, where, True, LOOP, prio)

SHORT = "สั้น"
for file, what, where, prio, note in [
        ("ui_move", "เลื่อนเมนูหรือการ์ด", "ทุกหน้า", 2, ""),
        ("ui_confirm", "กดปุ่ม", "ทุกหน้า", 2, ""),
        ("ui_back", "ถอยกลับ", "ทุกหน้า", 2, ""),
        ("ui_denied", "กดสิ่งที่ทำไม่ได้ เช่น BREATH ไม่พอ", "ทุกหน้า", 2, ""),
        ("path_chosen", "เลือกห้องบนแผนที่", "แผนที่", 2, ""),
        ("recruit", "เลือกคนเข้าวง", "THE AUDITION", 2, ""),
        ("seat_remove", "เอาคนออกจากวง", "THE AUDITION", 1, ""),
        ("seat_pickup", "หยิบเสานักดนตรีเพื่อสลับลำดับ", "หน้าวง", 1, ""),
        ("seat_drop", "วางเสานักดนตรีลงที่ใหม่", "หน้าวง", 1, ""),
        ("buy", "ซื้อของ", "ร้าน", 2, ""),
        ("motif_get", "ได้ของ GIFT", "ร้าน หีบ เหตุการณ์ รางวัลบอส", 2, "ชื่อไฟล์ยังเป็นคำเดิม motif"),
        ("rehearse", "ได้ดาวซ้อม", "REHEARSAL ร้าน WORKSHOP เหตุการณ์ รางวัล", 2, ""),
        ("rest_recover", "ฟื้นเลือด", "ห้องพัก", 2, ""),
        ("page_turn", "เปิดหน้า กล่องถาม เหตุการณ์ เปิดชั้น", "หลายหน้า", 1, ""),
        ("check_pass", "เสี่ยงสำเร็จ", "เหตุการณ์", 1, ""),
        ("check_fail", "เสี่ยงไม่สำเร็จ", "เหตุการณ์", 1, ""),
        ("victory", "ชนะไฟต์ (เสียงสั้นตอนขึ้นผล)", "หน้าผลชนะ", 2, ""),
        ("defeat", "แพ้", "หน้าผลแพ้ CURTAIN CALL", 2, ""),
        ("run_complete", "ชนะบอสสุดท้าย จบรัน", "CURTAIN CALL", 2, ""),
        ("combo_up", "ผ่านบทเรียน หรือจบการทดสอบจังหวะ", "ไฟต์ฝึก หน้าฝึก Settings", 1, "ชื่อไฟล์เป็นคำเดิมของหน้าดวล"),
        ("beat_tick", "เสียงจับจังหวะตอนทดสอบจังหวะตวัด", "Settings หน้าฝึก", 1, "ต้องสั้นและคม"),
        ("qte_perfect", "ตวัดโน้ตตรงวงแหวนพอดี PERFECT", "หน้าฝึกตวัดไม้ตอนนี้ ไอริสเสนอใช้ในไฟต์ด้วย", 3, ""),
        ("qte_normal", "ตวัดโน้ตโดน (ไม่พอดีวง)", "หน้าฝึกตวัดไม้ตอนนี้ ไอริสเสนอใช้ในไฟต์ด้วย", 3, ""),
        ("qte_miss", "พลาดหรือตวัดผิดทาง", "หน้าฝึกตวัดไม้ตอนนี้ ไอริสเสนอใช้ในไฟต์ด้วย", 3, ""),
        ("qte_hesitate", "ปล่อยให้เวลาของโน้ตหมด", "หน้าฝึกตวัดไม้ตอนนี้ ไอริสเสนอใช้ในไฟต์ด้วย", 2, ""),
        ("note_on", "โน้ตโผล่", "หน้าฝึกตวัดไม้", 1, "")]:
    sound("sfx_now", "Content/Audio/Sfx/" + file, "SFX", what, where, True, SHORT, prio, note)

NEW = "ยังไม่มี"
for file, what, prio in [("hit_" + k, "ATTACK ของ " + n + " : เสียง" + i + "ผสมเสียงอาวุธ", 3) for k, n, i in LEGENDS] + [
        ("skill_caprice", "CAPRICE กรีดเร็ว 4 ครั้ง (ทีละโน้ต)", 3), ("skill_il_cannone", "IL CANNONE ยิงนัดเดียวแรง", 3),
        ("skill_soft_air", "SOFT AIR ฮีลทั้งวง", 3), ("skill_the_lesson", "THE LESSON ส่ง FOCUS ให้เพื่อน", 2),
        ("skill_timpani_roll", "TIMPANI ROLL รัวกลอง", 3), ("skill_thunder_call", "THUNDER CALL เรียกศัตรูให้หันมา", 2),
        ("skill_shield", "SHIELD ยืนบังเพื่อน", 2), ("skill_lance", "LANCE แทงสองครั้ง", 3),
        ("skill_fanfare", "FANFARE เป่าปลุกทั้งวง", 2), ("skill_chakram", "CHAKRAM ขว้างวงใบมีด", 3)]:
    sound("sfx_new", "Content/Audio/Sfx/" + file, "SFX", what, "ไฟต์", False, "สั้น ไม่เกินราว 0.5 วิ", prio,
          note="ตีทีละโน้ต เสียงเดียวเล่นซ้ำได้หลายครั้งติดกัน")
for file, what, where, prio, length, optional in [
        ("qte_hold", "ระหว่างกดค้างโน้ต HOLD", "ไฟต์ วงแหวน QTE", 2, "วนได้", False),
        ("qte_shake", "รัวไม้ทีละครั้งตอนโน้ต SHAKE", "ไฟต์ วงแหวน QTE", 2, SHORT, False),
        ("qte_appear", "โน้ตโผล่ในวงแหวน", "ไฟต์ วงแหวน QTE", 1, SHORT, True),
        ("parry_perfect", "ปัดโน้ตของศัตรูได้พอดี", "ไฟต์ ตาของศัตรู", 3, SHORT, False),
        ("parry_good", "ปัดโดนแต่ไม่พอดี", "ไฟต์ ตาของศัตรู", 2, SHORT, False),
        ("band_hurt", "นักดนตรีโดนตี", "ไฟต์", 3, SHORT, False),
        ("band_ko", "นักดนตรีล้ม", "ไฟต์", 2, SHORT, False),
        ("enemy_hit", "โน้ตของศัตรูพุ่งเข้า", "ไฟต์ ตาของศัตรู", 3, SHORT, False),
        ("enemy_charge", "ศัตรูตั้งท่าชาร์จ CHARGE", "ไฟต์", 2, SHORT, False),
        ("enemy_broken", "ท่าชาร์จพัง BROKEN", "ไฟต์", 2, SHORT, False),
        ("enemy_armor_up", "ศัตรูตั้งโล่ ARMOR UP", "ไฟต์", 2, SHORT, False),
        ("enemy_armor_break", "โล่ ARMOR แตก", "ไฟต์", 2, SHORT, False),
        ("enemy_heal", "ศัตรูฮีล HEAL", "ไฟต์", 1, SHORT, False),
        ("enemy_stunned", "ศัตรู STUNNED", "ไฟต์", 2, SHORT, False),
        ("enemy_die", "ศัตรูสลายเข้าความมืด", "ไฟต์", 3, SHORT, False),
        ("burn_tick", "BURN ลดเลือด", "ไฟต์", 2, SHORT, False),
        ("volume_up", "ได้ VOLUME", "ไฟต์", 2, SHORT, False),
        ("guard_up", "GUARD หรือ SHIELD ขึ้น", "ไฟต์", 2, SHORT, False),
        ("muted", "โดน MUTED", "ไฟต์", 1, SHORT, False),
        ("finale_ready", "เกจ FINALE เต็ม", "ไฟต์", 2, SHORT, False),
        ("finale_the_unhearing", "FINALE ของ THE UNHEARING (พายุ ฟ้าผ่า)", "ไฟต์ ตอนภาพ FINALE", 2, "1–2 วิ", False),
        ("finale_fanny", "FINALE ของ FANNY AND ??? (ผลึก สองเสียง)", "ไฟต์ ตอนภาพ FINALE", 2, "1–2 วิ", False),
        ("finale_the_metronome", "FINALE ของ THE METRONOME (แสงศักดิ์สิทธิ์)", "ไฟต์ ตอนภาพ FINALE", 2, "1–2 วิ", False),
        ("boss_entrance", "บอสเปิดตัว", "ไฟต์บอส ENTRANCE", 2, "1–3 วิ", False),
        ("boss_tell", "บอสเตือนท่าไม้ตาย", "ไฟต์บอส THE TELL", 2, "1–2 วิ", False),
        ("boss_strike", "บอสปล่อยท่าไม้ตาย", "ไฟต์บอส THE STRIKE", 2, "1–2 วิ", False),
        ("boss_turn", "บอสเหลือครึ่งเลือด THE TURN", "ไฟต์บอส", 2, "1–2 วิ", False),
        ("boss_down", "บอสล้ม", "ไฟต์บอส", 2, "1–3 วิ", False),
        ("round_start", "เริ่มตาของวง", "ไฟต์", 2, SHORT, False),
        ("turn_enemy", "เริ่มตาของศัตรู", "ไฟต์", 2, SHORT, False),
        ("void_ambience", "เสียงหึ่งของความมืด TACET เล่นใต้เพลง", "ไฟต์", 1, "วนได้", True),
        ("shop_reroll", "REROLL ของในร้าน", "ร้าน", 1, SHORT, False),
        ("coins", "ได้เงิน", "หน้าผลชนะ", 1, SHORT, False),
        ("rank_reveal", "ขึ้นอันดับ S A B", "หน้าผลชนะ", 1, SHORT, False)]:
    note = "ชื่อไฟล์มีอยู่แล้วในโค้ด (เคยเป็นป้ายรอบของหน้าดวล)" if file == "round_start" else ""
    sound("sfx_new", "Content/Audio/Sfx/" + file, "SFX", what, where, False, length, prio, note=note, optional=optional)

# ---------------------------------------------------------------------------------------------
# what is in the repo
# ---------------------------------------------------------------------------------------------
MGCB_TEXT = open(MGCB, encoding="utf-8-sig").read() if os.path.exists(MGCB) else ""


def in_mgcb(content_path):
    """content_path : 'Content/Art/...png' -> is '#begin Art/...png' in Content.mgcb"""
    rel = content_path.split("Content/", 1)[1]
    return ("#begin " + rel) in MGCB_TEXT


def image_size(path):
    if Image is None:
        return None
    try:
        with Image.open(path) as im:
            return im.size
    except Exception:
        return None


def set_anims(path):
    """the animations found for a battle character, either way of handing over (FrameArt)"""
    full = os.path.join(GAME, path)
    found = []
    sheet = full + ".json"
    if os.path.exists(sheet) and os.path.exists(full + ".png"):
        try:
            meta = json.load(open(sheet, encoding="utf-8")).get("meta", {})
            tags = [t.get("name", "").strip().lower() for t in meta.get("frameTags", [])]
            found += tags or ["idle"]
        except Exception:
            pass
    folder, key = os.path.dirname(full), os.path.basename(full)
    if os.path.isdir(folder):
        for f in os.listdir(folder):
            m = re.match(re.escape(key) + r"_([a-z]+)_0\.png$", f)
            if m and m.group(1) not in found:
                found.append(m.group(1))
    return found


def have(row):
    """the words for the "มีแล้ว" column, and True when it counts as done"""
    if row["kind"] == "gameicon":
        full = os.path.join(GAME, row["path"])
        if not os.path.exists(full):
            return "ยังไม่มี", False
        md5 = hashlib.md5(open(full, "rb").read()).hexdigest()
        return ("ยังไม่มี (ยังเป็นโลโก้ MonoGame)", False) if md5 == DEFAULT_ICON_MD5.get(row["path"]) else ("มีแล้ว", True)
    if row["kind"] == "set":
        anims = set_anims(row["path"])
        return ("มีแล้ว (" + " ".join(anims) + ")", True) if anims else ("ยังไม่มี", False)
    full = os.path.join(GAME, row["path"] + ".png")
    if not os.path.exists(full):
        return "ยังไม่มี", False
    words = "มีแล้ว"
    size = image_size(full)
    if row["check"] and row["canvas"] and size and tuple(size) != tuple(row["canvas"]):
        words += " (ขนาดจริง %d×%d ไม่ตรง)" % size
    if row["mgcb"] and not in_mgcb(row["path"] + ".png"):
        words += " (ยังไม่อยู่ใน Content.mgcb)"
    return words, True


def have_sound(row):
    for ext in (".wav", ".ogg", ".mp3"):
        full = os.path.join(GAME, row["file"] + ext)
        if os.path.exists(full):
            return ("มีแล้ว" if in_mgcb(row["file"] + ext) else "มีแล้ว (ยังไม่อยู่ใน Content.mgcb)"), True
    return "ยังไม่มี", False


for r in ART:
    r["have"], r["done"] = have(r)
for r in SOUND:
    r["have"], r["done"] = have_sound(r)


def canvas_text(r):
    c = r["canvas"]
    if r.get("size_text"):
        return r["size_text"]
    if r["kind"] == "gameicon" and r["path"].endswith(".ico"):
        return "16 32 48 256"
    return "%d×%d" % tuple(c) if c else "สูง 118"


def file_text(r):
    """what the team names the file"""
    name = os.path.basename(r["path"])
    if r["kind"] == "set":
        return name + ".png + .json หรือ " + name + "_<ท่า>_<เลข>.png"
    if r["kind"] == "gameicon":
        return name
    return name + ".png"


def folder_text(r):
    if r["kind"] == "gameicon":
        return "Tacetno433/"
    return "Tacetno433/" + os.path.dirname(r["path"]) + "/"


def label(r):
    tags = []
    tags.append("นิ่ง" if r["stable"] else "อาจเปลี่ยน")
    if r["optional"]:
        tags.append("ไม่บังคับ")
    return " ".join(tags)


# ---------------------------------------------------------------------------------------------
# ASSET_LIST.md tables
# ---------------------------------------------------------------------------------------------
def md_escape(s):
    return str(s).replace("|", "/")


def md_table(head, rows):
    out = ["| " + " | ".join(head) + " |", "|" + "---|" * len(head)]
    out += ["| " + " | ".join(md_escape(c) for c in row) + " |" for row in rows]
    return "\n".join(out)


SECTIONS = [("musicians", "1.1 นักดนตรีในฉากต่อสู้"), ("enemies", "1.2 ศัตรูและบอสในฉากต่อสู้"),
            ("stages", "1.3 ฉากหลัง"), ("bosscut", "1.4 ภาพ cut in ของบอส"), ("conductors", "2 วาทยากร"),
            ("uimusicians", "3 เสาภาพและหน้านักดนตรี"), ("icons", "4 ไอคอน"), ("legendicons", "4 ไอคอนร่างในตำนาน"),
            ("places", "5 สถานที่ เหตุการณ์ การ์ดห้อง"), ("other", "6 โลโก้ ไม้บาตอง ไอคอนเกม")]


def summary_md():
    rows = []
    for sec, title in SECTIONS:
        items = [r for r in ART if r["sec"] == sec]
        rows.append([title] + [sum(1 for r in items if r["plan"] == p) for p in (MUST, TIME, NO, IRIS)]
                    + [sum(1 for r in items if r["done"])])
    rows.append(["**รวมภาพ**"] + ["**%d**" % sum(1 for r in ART if r["plan"] == p) for p in (MUST, TIME, NO, IRIS)]
                + ["**%d**" % sum(1 for r in ART if r["done"])])
    text = md_table(["หมวดภาพ", MUST, TIME, NO + " (เกมไม่ต้องมีภาพ)", IRIS, "มีแล้ว"], rows)
    rows = []
    for sec, title in [("music", "S1 เพลง"), ("sfx_now", "S2 เสียงที่โค้ดเล่นแล้ว"), ("sfx_new", "S3 เสียงที่ต้องมีเพิ่ม (ข้อเสนอ)")]:
        items = [r for r in SOUND if r["sec"] == sec]
        need = [r for r in items if not r["optional"]]
        rows.append([title, len(need), len(items) - len(need), sum(1 for r in items if r["done"])])
    text += "\n\n" + md_table(["หมวดเสียง", "ต้องทำ", "ไม่บังคับ", "มีแล้ว"], rows)
    sets = [r for r in ART if r["kind"] == "set"]
    text += ("\n\nตัวละครในฉากต่อสู้ %d ชุด (นักดนตรี 5 ศัตรู 4 แบบที่ใช้ร่วมกัน บอส 6) ท่า idle กับ attack ต้องมี ท่าอื่นถ้ามีเวลา รายท่าอยู่แท็บ Animations"
             % len(sets))
    text += "\nตัวเลขในตารางนี้สร้างตอนรันสคริปต์ล่าสุด (" + UPDATED + ")"
    return text


def art_md(sec):
    rows = []
    for r in ART:
        if r["sec"] != sec:
            continue
        rows.append(["`" + file_text(r) + "`", r["name"], canvas_text(r), r["screen"], r["where"],
                     r["plan"], label(r), r["have"], r["note"]])
    head = ["ไฟล์", "คืออะไร", "วาด (px)", "บนจอ", "ใช้ที่", "แผน", "ป้าย", "มีแล้ว", "หมายเหตุ"]
    folders = sorted(set(folder_text(r) for r in ART if r["sec"] == sec))
    items = [r for r in ART if r["sec"] == sec]
    mg = [r for r in items if r["mgcb"]]
    lead = "โฟลเดอร์ : " + " ".join("`" + f + "`" for f in folders)
    if not mg:
        lead += " (ไม่ต้องแก้ `Content.mgcb`)"
    elif len(mg) == len(items):
        lead += " (ต้องเพิ่มใน `Content.mgcb`)"
    else:
        lead += " (ต้องเพิ่มใน `Content.mgcb` เฉพาะ " + " ".join("`" + file_text(r) + "`" for r in mg) + ")"
    if any(r["kind"] == "gameicon" for r in items):
        lead += "\nไอคอนเกมวางทับไฟล์เดิมใน `Tacetno433/` ชื่อเดิม (โปรเจกต์ฝังไฟล์ไว้ในเกมเอง ไม่ต้องแก้ `Content.mgcb`)"
    return lead + "\n\n" + md_table(head, rows)


def anims_md(anims):
    return md_table(["ท่า", "เฟรมที่แนะนำ", "เล่นแบบ", "เกมเล่นตอน", "แผน"], [[a, f, p, w, plan] for a, f, p, w, plan in anims])


def sound_md(sec):
    rows = []
    for r in SOUND:
        if r["sec"] != sec:
            continue
        name = os.path.basename(r["file"]) + (".ogg" if r["kind"] == "Music" else ".wav")
        rows.append(["`" + name + "`", r["what"], r["where"], r["length"], STARS[r["prio"]],
                     "ไม่บังคับ" if r["optional"] else "", r["have"], r["note"]])
    return md_table(["ไฟล์", "เสียงอะไร", "เล่นที่ไหน", "ความยาว", "ลำดับ", "ป้าย", "มีแล้ว", "หมายเหตุ"], rows)


# LOOK OF : which shared picture each enemy wears, read from the game's own table (Data/Enemy.cs, SHARED LOOKS 1 Nov) so
# the list and the game never disagree. LOOKS keeps the whole entry : (the looks in order, brightness, mark).
MARK_WORDS = {"None": "", "Float": "ลอยเหนือเงา", "Hum": "แสงนวลรอบตัวเต้นช้า", "Jitter": "กระตุกไปข้างเป็นพักๆ",
              "Flicker": "วูบหายเป็นพักๆ", "Echo": "ภาพซ้อนจางสองชั้นข้างหลัง", "Halo": "แสงขาวข้างหลังเต้นเร็ว"}


def read_looks():
    code = open(os.path.join(GAME, "Data", "Enemy.cs"), encoding="utf-8-sig").read()
    looks = {}
    for m in re.finditer(r'new Enemy \{ Name = "([^"]+)",(.*?)(?=new Enemy \{|\};)', code, re.S):
        block = m.group(2)
        key = re.sub(r"[^a-z0-9]", "_", m.group(1).lower())
        names = re.search(r'Looks = new\[\] \{([^}]*)\}', block)
        shade = re.search(r"LookShade = ([0-9.]+)f", block)
        mark = re.search(r"Mark = LookMark\.(\w+)", block)
        looks[key] = (re.findall(r'"([^"]+)"', names.group(1)) if names else [],
                      float(shade.group(1)) if shade else 1.0, mark.group(1) if mark else "None")
    return looks


LOOKS = read_looks()
DESIGN_KEYS = [k for k, _, _, _, _ in DESIGNS]
LOOK_OF = {}
for _key, _name, _kind, _size, _line in ENEMIES:
    if _kind.startswith("บอส"):
        continue
    if _key not in LOOKS:
        sys.exit("Data/Enemy.cs has no enemy called " + _name + " (asset_list.py ENEMIES)")
    _wears = [_key] if _key in DESIGN_KEYS else [k for k in LOOKS[_key][0] if k in DESIGN_KEYS]
    if not _wears:
        sys.exit(_name + " wears none of the drawn looks in Data/Enemy.cs (Looks)")
    LOOK_OF[_key] = _wears[0]


def told_apart(key):
    names, shade, mark = LOOKS[key]
    words = []
    if shade < 0.995:
        words.append("เข้มขึ้น %d%%" % round((1 - shade) * 100))
    if MARK_WORDS.get(mark):
        words.append(MARK_WORDS[mark])
    marks = " + ".join(words) if words else "ไม่แยก (สู้คนเดียวในไฟต์ฝึก)"
    worn = [k for k in names if k in DESIGN_KEYS]
    if key in DESIGN_KEYS:
        text = "ภาพของตัวเอง (แบบนี้ตั้งชื่อตามตัวนี้)"
        if worn:
            text += " / ถ้าไม่มีแบบนี้ ใช้ `" + worn[0] + "` " + marks
        return text
    if len(worn) > 1:
        marks += " / ถ้าไม่มีแบบนี้ ใช้ `" + worn[1] + "`"
    return marks


def enemy_map_md():
    looks = {k: n for k, n, _, _, _ in DESIGNS}
    rows = []
    for key, name, kind, size, line in ENEMIES:
        if kind.startswith("บอส"):
            continue
        rows.append([name, kind, looks[LOOK_OF[key]] + " `" + LOOK_OF[key] + "`", told_apart(key), line])
    return md_table(["ศัตรู", "ชนิด", "ใช้ภาพ", "โค้ดแยกตัวด้วย", "คำโปรย (ใช้เป็นแนววาดของแบบนั้น)"], rows)


def fill_md():
    text = open(MD, encoding="utf-8").read()
    blocks = {"summary": summary_md(), "musician_anims": anims_md(MUSICIAN_ANIMS), "enemy_anims": anims_md(ENEMY_ANIMS),
              "enemy_map": enemy_map_md(),
              "music": sound_md("music"), "sfx_now": sound_md("sfx_now"), "sfx_new": sound_md("sfx_new")}
    for sec, _ in SECTIONS:
        blocks[sec] = art_md(sec)
    for name, body in blocks.items():
        pattern = re.compile(r"(<!-- AUTO:%s -->\n).*?(<!-- /AUTO:%s -->)" % (name, name), re.S)
        if not pattern.search(text):
            sys.exit("ASSET_LIST.md has no AUTO block called " + name)
        text = pattern.sub(lambda m: m.group(1) + body + "\n" + m.group(2), text)
    open(MD, "w", encoding="utf-8").write(text)


# ---------------------------------------------------------------------------------------------
# TACET_Asset_List.xlsx
# ---------------------------------------------------------------------------------------------
FONT = "Arial"
HEAD_FILL = PatternFill("solid", fgColor="F4CCCC")      # the team sheet's header colour
SUB_FILL = PatternFill("solid", fgColor="EFEFEF")
TEAM_FILL = PatternFill("solid", fgColor="FFF2CC")      # yellow : the team types here
NA_FILL = PatternFill("solid", fgColor="D9D9D9")
THIN = Side(style="thin", color="BFBFBF")
BOX = Border(left=THIN, right=THIN, top=THIN, bottom=THIN)
WRAP = Alignment(vertical="top", wrap_text=True)
CENTER = Alignment(horizontal="center", vertical="top", wrap_text=True)

ART_COLS = [  # (Thai header, English header, width, team column?)
    ("#", "#", 5, False), ("ชื่อไฟล์", "Code ID", 30, False), ("หมวด", "Section", 22, False),
    ("ชื่อตัวละคร/ของ", "Name", 34, False), ("ขนาดวาด (px)", "Scale", 12, False), ("ขนาดบนจอ", "In-game size", 18, False),
    ("รูปแบบภาพ", "Type", 16, False), ("โฟลเดอร์ในเกม", "Game folder", 34, False), ("ต้องเพิ่มใน Content.mgcb", "mgcb", 10, False),
    ("ใช้ที่หน้า", "Where", 36, False), ("แผน", "Plan", 11, False), ("ป้าย", "Stable", 14, False),
    ("มีในเกมแล้ว", "In game", 22, False), ("สถานะการดำเนินงาน", "Status", 14, True),
    ("กำหนดส่ง", "Start - Deadline", 20, True), ("ชนิดไฟล์ที่ส่ง", ".File", 16, False),
    ("ที่อยู่ไฟล์ส่งงาน", "Link", 26, True), ("หมายเหตุจากไอริส", "Note", 46, False), ("หมายเหตุของทีม", "Comment", 30, True)]
SOUND_COLS = [
    ("#", "#", 5, False), ("ชื่อไฟล์", "File", 24, False), ("ประเภท", "Type", 8, False), ("หมวด", "Section", 20, False),
    ("เสียงอะไร", "What", 40, False), ("เล่นที่ไหน", "Where", 32, False), ("โค้ดเล่นแล้ว", "Hooked", 12, False),
    ("ความยาว", "Length", 16, False), ("ลำดับ", "Priority", 9, False), ("ไม่บังคับ", "Optional", 10, False),
    ("มีในเกมแล้ว", "In game", 18, False), ("สถานะการดำเนินงาน", "Status", 14, True), ("กำหนดส่ง", "Start - Deadline", 20, True),
    ("ที่อยู่ไฟล์ส่งงาน", "Link", 26, True), ("หมายเหตุจากไอริส", "Note", 36, False), ("หมายเหตุของทีม", "Comment", 30, True)]
SOUND_SEC = {"music": "S1 เพลง", "sfx_now": "S2 โค้ดเล่นแล้ว", "sfx_new": "S3 ต้องมีเพิ่ม (ข้อเสนอ)"}
TEAM_HEADS = {"Status", "Start - Deadline", "Link", "Comment", "Hit frames"} | set(ANIM_COLUMNS)   # the yellow columns


def old_team_values():
    """what the team typed in the last file : {(sheet, key, column header): value}"""
    kept = {}
    if not os.path.exists(XLSX):
        return kept
    wb = load_workbook(XLSX)
    for sheet, key_col in (("Art", 2), ("Sound", 2), ("Animations", 1)):
        if sheet not in wb.sheetnames:
            continue
        ws = wb[sheet]
        heads = [c.value for c in ws[2]]
        for row in ws.iter_rows(min_row=3):
            key = row[key_col - 1].value
            if not key:
                continue
            for c, head in zip(row, heads):
                if head in TEAM_HEADS and c.value is not None and c.value != "NonStart":   # NonStart is where every row starts
                    kept[(sheet, key, head)] = c.value
    return kept


def style_head(ws, cols):
    for i, (th, en, width, team) in enumerate(cols, start=1):
        for r, text in ((1, th), (2, en)):
            c = ws.cell(row=r, column=i, value=text)
            c.font = Font(name=FONT, size=9, bold=True)
            c.fill = TEAM_FILL if team else (HEAD_FILL if r == 1 else SUB_FILL)
            c.alignment = CENTER
            c.border = BOX
        ws.column_dimensions[ws.cell(row=1, column=i).column_letter].width = width
    ws.freeze_panes = "C3"


def put(ws, row, col, value, team=False, center=False):
    c = ws.cell(row=row, column=col, value=value)
    c.font = Font(name=FONT, size=9)
    c.alignment = CENTER if center else WRAP
    c.border = BOX
    if team:
        c.fill = TEAM_FILL
    return c


def status_rules(ws, ref):
    dv = DataValidation(type="list", formula1='"' + ",".join(STATUS) + '"', allow_blank=True)
    dv.error, dv.errorTitle = "เลือกจากรายการ : " + ", ".join(STATUS), "สถานะ"
    ws.add_data_validation(dv)
    dv.add(ref)
    first = ref.split(":")[0]
    for word, colour in (("Done", "B7E1CD"), ("In Progress", "FFE599"), ("Fix", "F4CCCC")):
        ws.conditional_formatting.add(ref, CellIsRule(operator="equal", formula=['"%s"' % word],
                                                      fill=PatternFill("solid", fgColor=colour)))


def have_rule(ws, col_letter, last):
    ref = "%s3:%s%d" % (col_letter, col_letter, last)
    ws.conditional_formatting.add(ref, FormulaRule(formula=['LEFT(%s3,6)="มีแล้ว"' % col_letter],
                                                   font=Font(name=FONT, size=9, color="38761D", bold=True)))


def start_status(r):
    """the team's word a row starts with : None when it is not drawn, Done when the file is already in the game"""
    if r["plan"] in (NO, IRIS):
        return "None"
    return "Done" if r["done"] else "NonStart"


def write_art(wb, kept):
    ws = wb.create_sheet("Art")
    style_head(ws, ART_COLS)
    heads = [en for _, en, _, _ in ART_COLS]
    titles = dict(SECTIONS)
    for n, r in enumerate(ART, start=1):
        row = n + 2
        key = file_text(r)
        values = [n, key, titles[r["sec"]], r["name"], canvas_text(r), r["screen"], r["style"], folder_text(r),
                  "ต้อง" if r["mgcb"] else "ไม่ต้อง", r["where"], r["plan"], label(r), r["have"],
                  kept.get(("Art", key, "Status"), start_status(r)), kept.get(("Art", key, "Start - Deadline")),
                  "PNG + JSON (Aseprite)" if r["kind"] == "set" else ("ICO / BMP" if r["kind"] == "gameicon" else "PNG"),
                  kept.get(("Art", key, "Link")), r["note"], kept.get(("Art", key, "Comment"))]
        for col, (value, (_, en, _, team)) in enumerate(zip(values, ART_COLS), start=1):
            c = put(ws, row, col, value, team=team, center=en in ("#", "Scale", "mgcb", "Plan", "Status"))
            if r["plan"] == NO and not team:
                c.font = Font(name=FONT, size=9, color="7F7F7F")      # grey : not drawn
    last = len(ART) + 2
    ws.auto_filter.ref = "A2:%s%d" % (ws.cell(row=2, column=len(ART_COLS)).column_letter, last)
    status_rules(ws, "N3:N%d" % last)
    have_rule(ws, "M", last)
    return last


def write_animations(wb, kept):
    ws = wb.create_sheet("Animations")
    cols = [("ตัวละคร", "Key", 22, False), ("ชนิด", "Kind", 26, False), ("แคนวาส", "Canvas", 10, False)]
    cols += [(a, a, 11, True) for a in ANIM_COLUMNS]
    cols += [("เฟรมที่โดน (attack skill parry)", "Hit frames", 24, True), ("หมายเหตุของทีม", "Comment", 30, True)]
    style_head(ws, cols)
    ws.freeze_panes = "B3"
    musician = {a: plan for a, _, _, _, plan in MUSICIAN_ANIMS}
    enemy = {a: plan for a, _, _, _, plan in ENEMY_ANIMS}
    sets = [r for r in ART if r["kind"] == "set"]
    for n, r in enumerate(sets, start=1):
        row = n + 2
        key = os.path.basename(r["path"])
        is_musician = r["sec"] == "musicians"
        put(ws, row, 1, key)
        put(ws, row, 2, r["name"])
        put(ws, row, 3, canvas_text(r), center=True)
        for i, a in enumerate(ANIM_COLUMNS):
            col = 4 + i
            plan = (musician if is_musician else enemy).get(a)
            if plan is None:
                c = put(ws, row, col, "-", center=True)
                c.fill = NA_FILL
                continue
            c = put(ws, row, col, kept.get(("Animations", key, a), "None" if plan == FREE else "NonStart"), team=True, center=True)
            if plan != MUST:
                c.font = Font(name=FONT, size=9, italic=True, color="7F7F7F")
        put(ws, row, 4 + len(ANIM_COLUMNS), kept.get(("Animations", key, "Hit frames")), team=True)
        put(ws, row, 5 + len(ANIM_COLUMNS), kept.get(("Animations", key, "Comment")), team=True)
    last = len(sets) + 2
    status_rules(ws, "D3:%s%d" % (ws.cell(row=3, column=3 + len(ANIM_COLUMNS)).column_letter, last))
    note = last + 2
    put(ws, note, 1, "ตัวปกติ = ต้องมี (idle attack) / ตัวเอียงสีเทา = ถ้ามีเวลา หรือไม่บังคับ / ช่อง - = ตัวละครชนิดนี้ไม่มีท่านี้ / เฟรมที่โดนเขียนแบบ attack 5, skill 7, parry 2 (นับจาก 0)")
    ws.merge_cells(start_row=note, start_column=1, end_row=note, end_column=8)
    return last


def write_sound(wb, kept):
    ws = wb.create_sheet("Sound")
    style_head(ws, SOUND_COLS)
    for n, r in enumerate(SOUND, start=1):
        row = n + 2
        key = os.path.basename(r["file"]) + (".ogg" if r["kind"] == "Music" else ".wav")
        values = [n, key, r["kind"], SOUND_SEC[r["sec"]], r["what"], r["where"], "เล่นแล้ว" if r["hooked"] else "ยังไม่มีจุดเล่น",
                  r["length"], STARS[r["prio"]], "ไม่บังคับ" if r["optional"] else "", r["have"],
                  kept.get(("Sound", key, "Status"), "NonStart"), kept.get(("Sound", key, "Start - Deadline")),
                  kept.get(("Sound", key, "Link")), r["note"], kept.get(("Sound", key, "Comment"))]
        for col, (value, (_, en, _, team)) in enumerate(zip(values, SOUND_COLS), start=1):
            put(ws, row, col, value, team=team, center=en in ("#", "Type", "Priority", "Status"))
    last = len(SOUND) + 2
    ws.auto_filter.ref = "A2:%s%d" % (ws.cell(row=2, column=len(SOUND_COLS)).column_letter, last)
    status_rules(ws, "L3:L%d" % last)
    have_rule(ws, "K", last)
    return last


def write_readme(ws, art_last, anim_last, sound_last):
    ws.title = "อ่านก่อน"
    ws.column_dimensions["A"].width = 3
    ws.column_dimensions["B"].width = 36
    ws.column_dimensions["C"].width = 110
    lines = [
        ("TACET 4'33 : รายการงานอาร์ตและเสียง (Beta1)", None),
        ("ทำเมื่อ", UPDATED + " จากโค้ด branch Beta1 ขนาดและชื่อไฟล์ทุกชิ้นอ่านจากโค้ดจริง รายการหลักและคำอธิบายเต็มอยู่ docs/ASSET_LIST.md"),
        ("ไฟล์นี้สร้างด้วยสคริปต์", "python3 docs/asset_list.py สร้างไฟล์นี้ใหม่ได้ทุกเมื่อ ช่องสีเหลืองที่ทีมกรอกจะถูกเก็บไว้ตามชื่อไฟล์ ช่องอื่นถูกเขียนใหม่ แก้ช่องอื่นที่นี่จะหาย"),
        ("ช่องที่ทีมกรอก (สีเหลือง)", "สถานะ (Status) / กำหนดส่ง (Start - Deadline) / ที่อยู่ไฟล์ส่งงาน (Link) / หมายเหตุของทีม (Comment) / แท็บ Animations : สถานะทีละท่า และเฟรมที่โดน"),
        ("สถานะ", "NonStart = ยังไม่เริ่ม / In Progress = กำลังทำ / Done = เสร็จ / Fix = ต้องแก้ / None = ไม่ทำ"),
        ("มีในเกมแล้ว", "สคริปต์ดูจากไฟล์ใน repo เอง และเช็กขนาดภาพกับแคนวาสในรายการ ถ้าไม่ตรงจะบอกขนาดจริง"),
        ("แท็บ", "Art = ภาพทุกชิ้น / Animations = ท่าของตัวละครในฉากต่อสู้ทีละท่า / Sound = เพลงและเสียง"),
        ("pixel art", "ฉากต่อสู้ขยาย 2 เท่า (วาดเหมือนจอ 640×360) / หน้าจออื่นขนาดจริง 1 พิกเซลของภาพ = 1 พิกเซลของจอ 1280×720 / ขนาดวาดในตาราง = แคนวาสที่ export"),
        ("สี", "โลกกับศัตรูขาว ดำ เทา แบบ dramatic / ฝั่งผู้เล่นสีสดได้ / ตัวละครแต่ละตัวมีสีเดี่ยวของตัวเอง บอก hex ด้วย"),
        ("ห้ามมีตัวหนังสือในภาพ", "ชื่อ ป้าย ตัวเลข เกมเขียนเองทั้งหมด"),
        ("ชื่อไฟล์", "ตัวพิมพ์เล็ก เว้นวรรคและเครื่องหมายเป็น _ ใช้ตามคอลัมน์ชื่อไฟล์ตรงตัว"),
        ("ส่งงาน", "Content/Art/Battle และ Content/Art/Ui วางไฟล์แล้ว Rebuild ได้เลย / ชิ้นที่คอลัมน์ mgcb เขียนว่า ต้อง ให้เพิ่มใน Content.mgcb ด้วย / ส่งให้ผู้ใช้หรือไอริสวางให้ก็ได้"),
        ("ป้าย", "นิ่ง = ชื่อกับขนาดไม่เปลี่ยนแล้ว / อาจเปลี่ยน = รองานจัดหน้าจอรอบสุดท้าย / ไม่บังคับ = ไม่มีเกมก็ทำงานครบ"),
        ("ขอบเขตที่ทีมวาด (1 พ.ย.)", "วาทยากร 3 คน นักดนตรี 5 คน บอสทั้ง 6 ตัว ศัตรู 3 ถึง 4 แบบที่ศัตรูทุกตัวใช้ร่วมกัน และโลโก้เกม ที่เหลือเกมออกแบบให้ไม่ต้องมีภาพ"),
        ("แผน", "ต้องมี = ทีมวาดแน่ / ถ้ามีเวลา = ไม่มีไอริสตัดจากภาพหลักของทีมหรือให้โค้ดทำท่าแทน / ไม่วาด = เกมไม่ต้องมีภาพ สถานะเริ่มที่ None / ไอริสทำ = ไอคอนเกมทำจากโลโก้"),
        ("ไฟล์เก่า", "TACET_UI_Asset_List.xlsx (8 ต.ค. UI skin 126 ไฟล์) เป็นของหน้าจอ Alpha6 เกม Beta1 ไม่โหลดภาพพวกนั้น ใช้ไฟล์นี้แทน"),
    ]
    row = 2
    for label_text, text in lines:
        b = ws.cell(row=row, column=2, value=label_text)
        b.font = Font(name=FONT, size=12 if text is None else 9, bold=True)
        if text is not None:
            c = ws.cell(row=row, column=3, value=text)
            c.font = Font(name=FONT, size=9)
            c.alignment = WRAP
        row += 1 if text is not None else 2
    # one example of how the yellow cells are filled
    row += 1
    ws.cell(row=row, column=2, value="ตัวอย่างการกรอก (ช่องสีเหลือง)").font = Font(name=FONT, size=9, bold=True)
    row += 1
    example = [("Status", "In Progress"), ("Start - Deadline", "3.Nov.69 - 10.Nov.69"),
               ("Link", "ลิงก์โฟลเดอร์งานของทีม"), ("Comment", "idle เสร็จแล้ว เหลือ attack")]
    for head, value in example:
        ws.cell(row=row, column=2, value=head).font = Font(name=FONT, size=9)
        c = ws.cell(row=row, column=3, value=value)
        c.font = Font(name=FONT, size=9)
        c.fill = TEAM_FILL
        row += 1
    # counts : formulas, so they follow the team's ticks
    row += 1
    ws.cell(row=row, column=2, value="นับจากแท็บอื่น (สูตร)").font = Font(name=FONT, size=9, bold=True)
    row += 1
    counts = [
        ("ภาพทั้งหมด (แถว)", "=COUNTA(Art!B3:B%d)" % art_last),
        ("ภาพที่ไม่บังคับ", '=COUNTIF(Art!L3:L%d,"*ไม่บังคับ*")' % art_last),
        ("ภาพที่มีในเกมแล้ว", '=COUNTIF(Art!M3:M%d,"มีแล้ว*")' % art_last),
        ("ภาพที่ทีมต้องวาด (ต้องมี)", '=COUNTIF(Art!K3:K%d,"ต้องมี")' % art_last),
        ("ภาพถ้ามีเวลา", '=COUNTIF(Art!K3:K%d,"ถ้ามีเวลา")' % art_last),
        ("ภาพสถานะ Done", '=COUNTIF(Art!N3:N%d,"Done")' % art_last),
        ("ภาพสถานะ In Progress", '=COUNTIF(Art!N3:N%d,"In Progress")' % art_last),
        ("ท่าของตัวละคร (ทุกช่อง รวมท่าไม่บังคับ)", '=COUNTA(Animations!D3:M%d)-COUNTIF(Animations!D3:M%d,"-")' % (anim_last, anim_last)),
        ("ท่าสถานะ Done", '=COUNTIF(Animations!D3:M%d,"Done")' % anim_last),
        ("เสียงทั้งหมด (แถว)", "=COUNTA(Sound!B3:B%d)" % sound_last),
        ("เสียงที่มีในเกมแล้ว", '=COUNTIF(Sound!K3:K%d,"มีแล้ว*")' % sound_last),
        ("เสียงสถานะ Done", '=COUNTIF(Sound!L3:L%d,"Done")' % sound_last),
    ]
    for head, formula in counts:
        ws.cell(row=row, column=2, value=head).font = Font(name=FONT, size=9)
        c = ws.cell(row=row, column=3, value=formula)
        c.font = Font(name=FONT, size=9)
        c.alignment = Alignment(horizontal="left")
        row += 1


def write_xlsx():
    kept = old_team_values()
    wb = Workbook()
    readme = wb.active
    art_last = write_art(wb, kept)
    anim_last = write_animations(wb, kept)
    sound_last = write_sound(wb, kept)
    write_readme(readme, art_last, anim_last, sound_last)
    wb.calculation.fullCalcOnLoad = True          # Excel and Sheets work the counts out when the file opens
    wb.save(XLSX)
    return sum(1 for v in kept.values() if v not in ("NonStart", "-"))   # what the team has really typed


if __name__ == "__main__":
    fill_md()
    kept = write_xlsx()
    print("art rows %d (must %d, if time %d, not drawn %d, made by Iris %d, in the game %d) / sound rows %d / team cells typed and kept %d"
          % (len(ART), sum(1 for r in ART if r["plan"] == MUST), sum(1 for r in ART if r["plan"] == TIME),
             sum(1 for r in ART if r["plan"] == NO), sum(1 for r in ART if r["plan"] == IRIS),
             sum(1 for r in ART if r["done"]), len(SOUND), kept))
    print("wrote", os.path.relpath(MD, REPO), "and", os.path.relpath(XLSX, REPO))
