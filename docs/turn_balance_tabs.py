# Turn Tabs : rebuilds the Turn tabs of docs/TACET_Balance.xlsx from the code (Alpha 7 step 12, ONE BAR).
#   python3 docs/turn_balance_tabs.py <folder of --shots turnsim>/turnsim.txt
# Turn Battle     : every number of the TURN BATTLE section of Data/BattleRules.cs (section, code, value, note)
# Turn Enemies    : the turn data of Data/Enemy.cs (its MOVES too, Beta1 30 Oct)
# Turn Skills     : every skill of Data/Skill.cs with its musician, and each musician's trait (Beta1 29 Oct, THE FIVE KITS)
# Turn Conductors : each conductor's SIGNATURE text from Data/Conductor.cs, matched by kit
# Turn Sim        : turnsim.txt as it is
# Game States     : BandSize
# Needs openpyxl (pip install openpyxl). Run it after changing a number in BattleRules, then commit the sheet.
import os, re, sys, copy, openpyxl
repo = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sim = sys.argv[1]
wb = openpyxl.load_workbook(f'{repo}/docs/TACET_Balance.xlsx')

def header_style(ws):
    return [copy.copy(ws.cell(row=1, column=c)._style) for c in range(1, ws.max_column + 1)]

def clear(ws, keep_header=True):
    styles = header_style(ws)
    ws.delete_rows(2 if keep_header else 1, ws.max_row)
    return styles

# --- Turn Battle
src = open(f'{repo}/Tacetno433/Data/BattleRules.cs', encoding='utf-8-sig').read().split('\n')
start = next(i for i, l in enumerate(src) if l.strip().startswith('//TURN BATTLE'))
end = next(i for i, l in enumerate(src) if 'TurnRollTime' in l)
rows = []
section = ''
prev_code = True
for l in src[start:end + 1]:
    t = l.strip()
    if t.startswith('//') and len(l) - len(l.lstrip()) > 8:
        continue                                   # the second line of a comment that sits after a number
    if t.startswith('//'):
        if prev_code:
            text = t[2:].strip()
            cut = len(text)
            for sep in [' : ', ',', ' (']:
                k = text.find(sep)
                if k > 0: cut = min(cut, k)
            section = text[:cut].strip()
        prev_code = False
        continue
    prev_code = True
    m = re.match(r'public static (?:readonly )?[\w\[\]]+ (\w+) = (.+?);\s*(?://\s*(.*))?$', t)
    if m:
        value = m.group(2).strip().rstrip('f').replace('f, ', ', ').replace('f }', ' }')
        rows.append((section, m.group(1), value, (m.group(3) or None)))
ws = wb['Turn Battle']
clear(ws)
for r in rows: ws.append(list(r))
values = {r[1]: r[2] for r in rows}

# CS Text (WORDS, Beta1 31 Oct) : a line of the code written as "..." + Words.X + "..." + BattleRules.Y read back as the
# player sees it, the rule words from Data/Words.cs and the numbers from BattleRules above
words = dict(re.findall(r'public const string (\w+) = "([^"]*)";', open(f'{repo}/Tacetno433/Data/Words.cs', encoding='utf-8-sig').read()))
CS_EXPR = r'((?:"[^"]*"|\s*\+\s*|BattleRules\.\w+|Words\.\w+)+)'
def cs_text(expr):
    text = ''
    for lit, rule, word in re.findall(r'"([^"]*)"|BattleRules\.(\w+)|Words\.(\w+)', expr):
        text += values.get(rule, rule) if rule else (words.get(word, word) if word else lit)
    return text

# --- Turn Enemies
es = open(f'{repo}/Tacetno433/Data/Enemy.cs', encoding='utf-8-sig').read()
ws = wb['Turn Enemies']
clear(ws)
if ws.cell(row=1, column=9).value is None:
    ws.cell(row=1, column=9).value = 'CUT IN (speed 5 or more)'
    ws.cell(row=1, column=9)._style = copy.copy(ws.cell(row=1, column=8)._style)
if ws.cell(row=1, column=10).value is None:      # ENEMY MOVES (Beta1 30 Oct) : its Moves list, GUARD with its GuardBlock
    ws.cell(row=1, column=10).value = 'MOVES in turn (GUARD block on floor 1 before x1.6)'
    ws.cell(row=1, column=10)._style = copy.copy(ws.cell(row=1, column=8)._style)
for block in re.findall(r'new Enemy \{ Name = "([^"]+)",(.*?)\},\s*(?=//|new Enemy|\};)', es, re.S):
    name, body = block
    def get(key, default=''):
        m = re.search(key + r'\s*=\s*([^,\n]+?)(?:,|\n)', body)
        return m.group(1).strip() if m else default
    hp = get('TurnHp'); speed = get('TurnSpeed'); kind = get('Kind').replace('EnemyKind.', '')
    pattern = re.search(r'Pattern = new int\[\] \{([^}]*)\}', body)
    nums = [int(x) for x in pattern.group(1).split(',')] if pattern else []
    nz = [x for x in nums if x > 0]
    power = round(sum(nz) / len(nz)) if nz else 1
    phrases = ', '.join(re.findall(r'"([^"]+)"', re.search(r'Phrases = new\[\] \{([^}]*)\}', body).group(1)))
    trait = re.search(r'TraitName = "([^"]+)"', body).group(1)
    floor = get('TraitFloor', '1')
    text = cs_text(re.search(r'TurnTraitText = ' + CS_EXPR, body).group(1))
    moves = re.search(r'Moves = new\[\] \{([^}]*)\}', body)
    moves = ', '.join(m.strip().upper().replace('HIT', 'ATTACK') for m in moves.group(1).split(',')) if moves else 'ATTACK'
    if 'GUARD' in moves: moves += f' (GUARD {get("GuardBlock", "0")})'
    ws.append([name, kind, hp, speed, power, phrases, f'{trait} ({floor})', text, 'yes' if int(speed) >= 5 else '', moves])

# --- Turn Skills (Beta1 29 Oct, THE FIVE KITS) : every skill of Data/Skill.cs with its musician (skills 2k and 2k + 1 belong
#     to musician k of Data/Musician.cs), and each musician's trait on their first row. The legends of MusicianList.Shown first.
ms = open(f'{repo}/Tacetno433/Data/Musician.cs', encoding='utf-8-sig').read()
musicians = []
for block in re.findall(r'new Musician \{ Name = "([^"]+)",(.*?)\},\s*(?=//|new Musician|\};)', ms, re.S):
    name, body = block
    trait = re.search(r'TraitName = "([^"]+)"', body)
    turn = re.search(r'TurnTraitText = ' + CS_EXPR + ',', body)
    text = cs_text(turn.group(1)) if turn else ''   # "..." + BattleRules.X + Words.Y : as the player reads it
    family = re.search(r'Family = Family\.(\w+)', body).group(1)
    power = re.search(r'Power = (\d+)', body).group(1)
    musicians.append((name, family, power, trait.group(1) if trait else '', text))
shown = re.findall(r'All\[(\d+)\]', re.search(r'public static Musician\[\] Shown = \{([^}]*)\}', ms).group(1))
ss = open(f'{repo}/Tacetno433/Data/Skill.cs', encoding='utf-8-sig').read()
skills = re.findall(r'new Skill \{(.*?)\},\s*\n', ss, re.S)
if 'Turn Skills' not in wb.sheetnames:
    ws = wb.create_sheet('Turn Skills', wb.sheetnames.index('Turn Enemies') + 1)
    head = ['Musician', 'Skill', 'Kind', 'BREATH', 'Target', 'Notes', 'Damage per note', 'DISSONANCE', 'Heal', 'CRESCENDO',
            'BREATH back', 'OFF-BEAT', 'Special', 'Text', 'Trait (turn battle)']
    styles = header_style(wb['Turn Enemies'])
    for c, h in enumerate(head, 1):
        ws.cell(row=1, column=c).value = h
        ws.cell(row=1, column=c)._style = copy.copy(styles[min(c, len(styles)) - 1])
ws = wb['Turn Skills']
clear(ws)
order = [int(i) for i in shown] + [i for i in range(len(musicians)) if str(i) not in shown]
for k in order:
    name, family, power, trait, trait_text = musicians[k]
    for j in (0, 1):
        body = skills[2 * k + j]
        def get(key, default=''):
            m = re.search(r'\b' + key + r' = ([^,]+?)(?:,|$)', body)
            v = m.group(1).strip() if m else default
            return v[:-1] if re.match(r'^-?\d+(\.\d+)?f$', v) else v      # 0.4f -> 0.4, but SkillTarget.Self keeps its f
        notes = ' '.join(n.replace('()', '').replace('S(', '').replace('H(', 'HOLD ').replace(')', '')
                         .replace('K', 'SPARK').replace('T', 'ROLL') for n in re.findall(r'[SHKT]\([LRUD]?\)', body))
        special = ' '.join(w for w, key in (('GUARD', 'Guard'), ('TAUNT', 'Taunt'), ('SHIELD', 'Shield'), ('LESSON', 'Lesson'))
                           if get(key) == 'true')
        if get('Bounce'): special += f' BOUNCE {get("Bounce")}'
        if get('Revenge'): special += f' x{get("Revenge")} WHEN HIT'
        def num(v):                                # a number from the code as a number in the sheet (None when it is not there)
            try:
                f = float(v)
                return int(f) if f.is_integer() else f
            except ValueError:
                return v or None
        label = f'{name} ({family}, power {power})' + ('' if str(k) in shown else ' hidden')
        ws.append([label if j == 0 else None, re.search(r'Name = "([^"]+)"', body).group(1), get('Kind').replace('SkillKind.', ''),
                   int(get('Cost', '2')), get('Target').replace('SkillTarget.', ''), notes, num(get('Damage')),
                   num(get('Dissonance')), num(get('Heal')), num(get('Crescendo')), num(get('Breath')),
                   num(get('OffBeat')), special.strip() or None, cs_text(re.search(r'\bText = ' + CS_EXPR, body).group(1)),
                   f'{trait} : {trait_text}' if j == 0 and trait else None])

# --- Turn Conductors : the signature texts from the code, matched by kit
cs = open(f'{repo}/Tacetno433/Data/Conductor.cs', encoding='utf-8-sig').read()
ws = wb['Turn Conductors']
for row in ws.iter_rows(min_row=2):
    kit = row[2].value
    if not kit: continue
    m = re.search(r'Turn = TurnKit\.' + kit + r'\b(.*?)TurnSignatureText = ' + CS_EXPR, cs, re.S)
    if not m:
        m = re.search(r'TurnSignatureText = ' + CS_EXPR + r'(?:(?!new Conductor).)*?Turn = TurnKit\.' + kit + r'\b', cs, re.S)
        text = cs_text(m.group(1)) if m else None
    else:
        text = cs_text(m.group(2))
    if text: row[6].value = text

# --- Turn Sim
ws = wb['Turn Sim']
title = ws.cell(row=1, column=1).value
ws.delete_rows(1, ws.max_row)
ws.append([title])
for line in open(sim, encoding='utf-8').read().replace('\r', '').split('\n'):
    ws.append([line if line else None])

# --- Game States : BandSize (29 Oct, the band of three : FirstFloorRecruits is gone)
ws = wb['Game States']
for row in ws.iter_rows():
    if len(row) > 2 and row[2].value == 'BandSize':
        row[1].value = int(re.search(r'BandSize = (\d+)', '\n'.join(src)).group(1))

wb.save(f'{repo}/docs/TACET_Balance.xlsx')
print('rows', len(rows))
