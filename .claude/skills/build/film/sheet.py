# sheet.py <dir> <out.png> <cols> <width> [from] [to] : frames of one page side by side, labelled with their frame
import sys, glob, re, os
from PIL import Image, ImageDraw
d, out, cols, w = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4])
lo = int(sys.argv[5]) if len(sys.argv) > 5 else -1; hi = int(sys.argv[6]) if len(sys.argv) > 6 else 10**9
fs = []
for f in glob.glob(d + '/*.png'):
    m = re.search(r'_(\d+)\.png$', f)
    if m and lo <= int(m.group(1)) <= hi: fs.append((int(m.group(1)), f))
fs.sort()
h = int(w * 9 / 16); rows = (len(fs) + cols - 1) // cols
s = Image.new('RGB', (cols * (w + 4) + 4, rows * (h + 4) + 4), (60, 60, 60)); dr = ImageDraw.Draw(s)
for i, (n, f) in enumerate(fs):
    x = 4 + (i % cols) * (w + 4); y = 4 + (i // cols) * (h + 4)
    s.paste(Image.open(f).convert('RGB').resize((w, h), Image.LANCZOS), (x, y)); dr.text((x + 3, y + 2), str(n), fill=(255, 255, 0))
s.save(out); print(len(fs), 'frames')
