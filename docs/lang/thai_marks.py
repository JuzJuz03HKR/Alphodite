#!/usr/bin/env python3
"""thai_marks.py : the numbers Core/ThaiText.cs needs from the Thai fonts (THAI TEXT, block F3, 2 Nov 2026).

MonoGame's font builder keeps every letter's picture and its width, but not how far a Thai mark (a vowel or tone mark that
sits over or under the letter before it) reaches back to the LEFT of where it starts. It puts every mark's picture at the
pen, after the letter it belongs to. This reads the true reach from the font files and writes it as C# for ThaiText.cs :
    for each font file : the advance of all 44 consonants added up (in em, to measure how big a built font is)
                         and, for each letter that reaches back (a mark, SARA AM), its picture's left edge (in em)
Run after changing a Thai font file :  python3 docs/lang/thai_marks.py
(needs fontTools : pip install fonttools) and paste the printed table into Core/ThaiText.cs (FONT TABLE).
"""
import os
from fontTools.ttLib import TTFont
from fontTools.pens.boundsPen import BoundsPen

FONTS = os.path.join(os.path.dirname(__file__), '..', '..', 'Tacetno433', 'Content', 'Fonts')
FILES = ['ChakraPetch-Medium', 'Trirong-SemiBold', 'Trirong-Medium']
CONSONANTS = [chr(c) for c in range(0x0E01, 0x0E2F)]

for name in FILES:
    font = TTFont(os.path.join(FONTS, name + '.ttf'))
    upm = font['head'].unitsPerEm
    cmap = font.getBestCmap()
    hmtx = font['hmtx']
    glyphs = font.getGlyphSet()
    total = sum(hmtx[cmap[ord(c)]][0] for c in CONSONANTS if ord(c) in cmap) / upm
    reach = []
    for code in range(0x0E01, 0x0E5C):
        if code not in cmap:
            continue
        g = cmap[code]
        advance, lsb = hmtx[g]
        if lsb >= 0 and advance > 0:
            continue
        pen = BoundsPen(glyphs)
        glyphs[g].draw(pen)
        if pen.bounds is None:
            continue
        reach.append((code, pen.bounds[0] / upm))
    print('            new FontTable("%s", %.4ff, new int[] { %s }, new float[] { %s }),' % (
        name, total, ', '.join('0x%04X' % c for c, _ in reach), ', '.join('%.4ff' % x for _, x in reach)))
