#!/usr/bin/env python3
"""merge.py <frozen key list> <batch.tsv> : put a batch of Thai lines into the word list (Content/Text/thai.txt).
A batch line is "<number in the frozen list>\t<Thai>", so the English is copied from the game's own text and never retyped."""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
WORDS = os.path.join(HERE, '..', '..', '..', 'Tacetno433', 'Content', 'Text', 'thai.txt')
keys = [l.rstrip('\n') for l in open(sys.argv[1], encoding='utf-8')]
have = {}
lines = open(WORDS, encoding='utf-8').read().split('\n')
for l in lines:
    if '\t' in l and not l.startswith('#'):
        have[l.split('\t', 1)[0]] = True
added = 0
with open(WORDS, 'a', encoding='utf-8', newline='\n') as out:
    for row in open(sys.argv[2], encoding='utf-8'):
        row = row.rstrip('\n')
        if not row or row.startswith('#') or '\t' not in row:
            continue
        n, th = row.split('\t', 1)
        en = keys[int(n) - 1]
        if en in have or not th.strip():
            continue
        out.write(en + '\t' + th.strip() + '\n')
        have[en] = True
        added += 1
print('added', added)
