# Turn Tabs : rebuilds the Turn tabs of docs/TACET_Balance.xlsx from the code (Alpha 7 step 12, ONE BAR).
#   python3 docs/turn_balance_tabs.py <folder of --shots turnsim>/turnsim.txt
# Turn Battle     : every number of the TURN BATTLE section of Data/BattleRules.cs (section, code, value, note)
# Turn Enemies    : the turn data of Data/Enemy.cs
# Turn Conductors : each conductor's SIGNATURE text from Data/Conductor.cs, matched by kit
# Turn Sim        : turnsim.txt as it is
# Game States     : FirstFloorRecruits
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

# --- Turn Enemies
es = open(f'{repo}/Tacetno433/Data/Enemy.cs', encoding='utf-8-sig').read()
ws = wb['Turn Enemies']
clear(ws)
if ws.cell(row=1, column=9).value is None:
    ws.cell(row=1, column=9).value = 'CUT IN (speed 5 or more)'
    ws.cell(row=1, column=9)._style = copy.copy(ws.cell(row=1, column=8)._style)
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
    text = re.search(r'TurnTraitText = "([^"]+)"', body).group(1)
    ws.append([name, kind, hp, speed, power, phrases, f'{trait} ({floor})', text, 'yes' if int(speed) >= 5 else ''])

# --- Turn Conductors : the signature texts from the code, matched by kit
cs = open(f'{repo}/Tacetno433/Data/Conductor.cs', encoding='utf-8-sig').read()
ws = wb['Turn Conductors']
for row in ws.iter_rows(min_row=2):
    kit = row[2].value
    if not kit: continue
    m = re.search(r'Turn = TurnKit\.' + kit + r'\b(.*?)TurnSignatureText = "([^"]+)"', cs, re.S)
    if not m:
        m = re.search(r'TurnSignatureText = "([^"]+)"(?:(?!new Conductor).)*?Turn = TurnKit\.' + kit + r'\b', cs, re.S)
        text = m.group(1) if m else None
    else:
        text = m.group(2)
    if text: row[6].value = text

# --- Turn Sim
ws = wb['Turn Sim']
title = ws.cell(row=1, column=1).value
ws.delete_rows(1, ws.max_row)
ws.append([title])
for line in open(sim, encoding='utf-8').read().replace('\r', '').split('\n'):
    ws.append([line if line else None])

# --- Game States : FirstFloorRecruits
ws = wb['Game States']
for row in ws.iter_rows():
    if len(row) > 2 and row[2].value == 'FirstFloorRecruits':
        row[1].value = int(re.search(r'FirstFloorRecruits = (\d+)', '\n'.join(src)).group(1))

wb.save(f'{repo}/docs/TACET_Balance.xlsx')
print('rows', len(rows))
