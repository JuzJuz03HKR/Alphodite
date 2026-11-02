# Game icon from the team's logo (block E item 3, the user 2 Nov "ทำไอคอนเกมจากโลโก้เลย" USER_BRIEFS 13.117)
#
#   python3 docs/team_art/make_icon.py docs/team_art/logo.png Tacetno433
#
# Writes Tacetno433/Icon.ico (the .exe and the taskbar) and Tacetno433/Icon.bmp (the game window, MonoGame
# DesktopGL hands it to SDL). The project embeds both files itself, Content.mgcb does not need them.
#
# What it does with the logo
# - a square around the hole's ring (the logo is 1500x1500, the ring's centre is near 752, 727)
# - 48 px and up keep the name TACET 4'33 (still readable at 48)
# - 32 px and down keep only the ring of the hole : the letters would be a grey smear that small,
#   the ring with its band still reads as the game's mark. The letters are taken out, the black boxes
#   the artist put behind them stay
# - the thin scratchy lines are thickened a little before shrinking and the middle tones lifted,
#   so the ring survives at 16 px
# - corners rounded (a dark tile reads on both a light and a dark taskbar)
# - Icon.bmp is the ring version at 256 : Windows shrinks the window's icon to 16 to 32 itself

import struct
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

CX, CY, HALF = 752, 727, 640          # the ring's centre in the logo and half the square around it
NAME_BOXES = [(340, 600, 1180, 775),  # TACET
              (600, 825, 885, 935)]   # 4'33
BIG = [256, 128, 96, 64, 48]
SMALL = [32, 24, 16]
CORNER = 0.18                          # corner radius as a share of the icon's size


def square(logo):
    return logo.crop((CX - HALF, CY - HALF, CX + HALF, CY + HALF))


def no_name(img):
    # bright pixels inside the two name boxes, grown a little, painted black
    a = np.asarray(img).copy()
    lum = a.mean(axis=2)
    mask = np.zeros(lum.shape, bool)
    for (x0, y0, x1, y1) in NAME_BOXES:
        x0 -= CX - HALF; x1 -= CX - HALF; y0 -= CY - HALF; y1 -= CY - HALF
        mask[y0:y1, x0:x1] = lum[y0:y1, x0:x1] > 40
    grown = Image.fromarray((mask * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(9))
    a[np.asarray(grown) > 0] = 0
    return Image.fromarray(a)


def lift(img, gamma, grow):
    # grow : thicken the thin lines (odd number of pixels, 0 = no) / gamma under 1 lifts the middle tones
    if grow:
        img = img.filter(ImageFilter.MaxFilter(grow))
    a = np.asarray(img).astype(np.float32) / 255.0
    return Image.fromarray((np.clip(a, 0, 1) ** gamma * 255 + 0.5).astype(np.uint8))


def tile(img, size):
    small = img.resize((size, size), Image.LANCZOS)
    if size <= 48:
        small = small.filter(ImageFilter.UnsharpMask(radius=0.6, percent=60, threshold=0))
    # the rounded corner is drawn 4 times bigger then shrunk, so its edge is smooth
    mask = Image.new("L", (size * 4, size * 4), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, size * 4 - 1, size * 4 - 1),
                                           radius=int(CORNER * size * 4), fill=255)
    rgba = small.convert("RGBA")
    rgba.putalpha(mask.resize((size, size), Image.LANCZOS))
    return rgba


def write_bmp(path, rgba):
    # 32 bit BMP with a BITMAPV5HEADER and bit masks (the same header MonoGame's default Icon.bmp has),
    # rows bottom up, each pixel R G B A packed as one little endian number 0xRRGGBBAA
    w, h = rgba.size
    px = np.asarray(rgba.convert("RGBA"))
    packed = np.zeros((h, w, 4), np.uint8)
    packed[:, :, 3] = px[:, :, 0]   # R mask 0xff000000 = the highest byte
    packed[:, :, 2] = px[:, :, 1]   # G mask 0x00ff0000
    packed[:, :, 1] = px[:, :, 2]   # B mask 0x0000ff00
    packed[:, :, 0] = px[:, :, 3]   # A mask 0x000000ff = the lowest byte
    data = packed[::-1].tobytes()
    v5 = struct.pack("<IiiHHIIiiII", 124, w, h, 1, 32, 3, len(data), 2835, 2835, 0, 0)
    v5 += struct.pack("<IIII", 0xFF000000, 0x00FF0000, 0x0000FF00, 0x000000FF)
    v5 += struct.pack("<I", 0x73524742)            # 'sRGB'
    v5 += bytes(36)                                 # colour space end points, unused for sRGB
    v5 += struct.pack("<IIIIIII", 0, 0, 0, 2, 0, 0, 0)
    head = struct.pack("<2sIHHI", b"BM", 14 + len(v5) + len(data), 0, 0, 14 + len(v5))
    with open(path, "wb") as f:
        f.write(head + v5 + data)


def main():
    logo = Image.open(sys.argv[1]).convert("RGB")
    out = sys.argv[2].rstrip("/")
    named = lift(square(logo), 0.85, 0)
    ring = lift(no_name(square(logo)), 0.6, 5)
    frames = [tile(named, s) for s in BIG] + [tile(ring, s) for s in SMALL]
    # every size uses its own frame (Pillow picks append_images by size)
    frames[0].save(out + "/Icon.ico", format="ICO", sizes=[(s, s) for s in BIG + SMALL],
                   append_images=frames[1:])
    write_bmp(out + "/Icon.bmp", tile(lift(no_name(square(logo)), 0.75, 3), 256))
    print("wrote", out + "/Icon.ico", out + "/Icon.bmp")


if __name__ == "__main__":
    main()
