#!/usr/bin/env python3
"""make_keys.py : the list of English lines to put in Thai (THAI TEXT, block F3, 2 Nov 2026, docs/BETA1_DIRECTION.md 52).

The game is asked twice (in the cloud build or Visual Studio's Debug build, see docs/lang/README.md) :
    --shots <every page>@90 --seen --out <dir>   -> seen.txt     every line a page drew or wrapped, as the player sees it
    --shots textdump --out <dir>                 -> textdump.txt every string the game's data holds, with its field
This joins them, leaves out what stays English (names, single words, keys, codes), and writes docs/lang/keys.txt : one English
line each, the lines the word list (Tacetno433/Content/Text/thai.txt) has already marked with a +, the rest still to do.
    python3 docs/lang/make_keys.py <seen.txt> <textdump.txt>
"""
import os, re, sys

HERE = os.path.dirname(os.path.abspath(__file__))
WORDS = os.path.join(HERE, '..', '..', 'Tacetno433', 'Content', 'Text', 'thai.txt')
OUT = os.path.join(HERE, 'keys.txt')

# the conductors and legends Beta1 shows (ConductorList.Shown, MusicianList's five)
SHOWN_CONDUCTORS = {1, 3, 8}
SHOWN_MUSICIANS = {0, 1, 2, 6, 7}
# data fields that are only the old duel's, made from others (wrapped, labels), or not words at all
SKIP_FIELD = re.compile(r'(Wrapped|Label|FileKey|Sample|Looks|Phrases|Song\.|Mechanic|SignatureText|SignatureCall|SignatureMark|'
                        r'Recipe|\.Name$|TraitName|BigName|Instrument|Weapon|ShortName|Mark$|Enemy$|Place$|Title$|Composer|'
                        r'MotifList\.All\[\d+\]\.Text$|\.TraitText$|\.TraitStory$|EnemyList\.All\[\d+\]\.Title|Credits\.Lines)')
SKIP_TYPE = re.compile(r'^(NumberText|SongChart|BattleState|Duel|TutorialScreen|HandArt|CharacterArt|MusicianArt|PortraitBox|'
                       r'StageStaff|Formation|StageLayout|SongList|DuelEffects|Phrase|ArtBank|UiArt|FrameArt|SoundBank|PixelDigits|'
                       r'Input|Sharp|Fx|PostChain|Settings|TestRoomScreen|Lang)')


def sentence(s):
    """worth putting in Thai : a sentence, or an instruction of four words or more"""
    if not re.search('[A-Za-z]{2}', s) or len(s) < 6:
        return False
    if re.fullmatch(r'[a-z0-9_/\.\-]+', s):
        return False
    words = re.findall(r"[A-Za-z']+", s)
    if re.search('[a-z]', s):
        return len(words) >= 2
    return len(words) >= 4


def from_dump(path):
    keep = []
    for line in open(path, encoding='utf-8'):
        if '\t' not in line:
            continue
        where, s = line.rstrip('\n').split('\t', 1)
        s = s.replace('\\n', '\n')
        kind = where.split('.')[0].split('[')[0]
        if SKIP_TYPE.match(kind) or SKIP_FIELD.search(where) or 'duel' in where.lower():   # the old duel's words stay English
            continue
        m = re.match(r'ConductorList\.All\[(\d+)\]', where)
        if m and int(m.group(1)) not in SHOWN_CONDUCTORS:
            continue
        m = re.match(r'MusicianList\.All\[(\d+)\]', where)
        if m and int(m.group(1)) not in SHOWN_MUSICIANS:
            continue
        if sentence(s):
            keep.append(s)
    return keep


def main():
    seen = [l.rstrip('\n').replace('\\n', '\n') for l in open(sys.argv[1], encoding='utf-8')] if len(sys.argv) > 1 else []
    dump = from_dump(sys.argv[2]) if len(sys.argv) > 2 else []
    done = set()
    if os.path.exists(WORDS):
        for line in open(WORDS, encoding='utf-8'):
            if '\t' in line and not line.startswith('#'):
                done.add(line.split('\t', 1)[0].replace('\\n', '\n'))
    keys = []
    for s in seen + dump:
        if '\n' in s or not sentence(s) or s in keys:
            continue
        keys.append(s)
    with open(OUT, 'w', encoding='utf-8', newline='\n') as f:
        for s in keys:
            f.write(('+ ' if s in done else '  ') + s + '\n')
    print('keys %d, done %d, to do %d' % (len(keys), sum(1 for s in keys if s in done), sum(1 for s in keys if s not in done)))


main()
