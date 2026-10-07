# Draws labelled pictures of the Catacombs sprite sheets, so each sprite number can be matched to what it shows.
# Usage (from this folder, needs Python with Pillow):
#   python tiles.py overview mainlevbuild.png 1 16 overview.png      whole sheet with a 16px grid
#   python tiles.py region mainlevbuild.png X Y W H SCALE out.png     a zoomed area (pixels from top-left) with sprite numbers
#   python tiles.py count mainlevbuild.png                            how many sprites the .meta defines
# Sprite rectangles come from the .meta file next to each PNG (Unity's slicing). Pictures go to Obsidian/Images/Catacombs.
import re, sys, json, os
from PIL import Image, ImageDraw, ImageFont

# Paths are relative to this folder: <project>/Tools/catacombs-catalog
HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.normpath(os.path.join(HERE, "..", ".."))
ROOT = os.path.join(PROJECT, "Assets", "Art", "RF_Catacombs_v1.0")
OUT = os.path.join(PROJECT, "Obsidian", "Images", "Catacombs")
os.makedirs(OUT, exist_ok=True)

def sprites(png):
    meta = open(os.path.join(ROOT, png + ".meta"), encoding="utf-8").read()
    out = []
    # Each sprite block: name, rect x/y/width/height
    for m in re.finditer(r"- serializedVersion: 2\s+name: (\S+)\s+rect:\s+serializedVersion: 2\s+x: (\S+)\s+y: (\S+)\s+width: (\S+)\s+height: (\S+)", meta):
        name, x, y, w, h = m.groups()
        out.append((name, float(x), float(y), float(w), float(h)))
    return out

def overview(png, scale, step):
    img = Image.open(os.path.join(ROOT, png)).convert("RGBA")
    W, H = img.size
    bg = Image.new("RGBA", img.size, (60, 60, 70, 255))
    bg.alpha_composite(img)
    big = bg.resize((W * scale, H * scale), Image.NEAREST)
    d = ImageDraw.Draw(big)
    for x in range(0, W, step):
        d.line([(x * scale, 0), (x * scale, H * scale)], fill=(255, 0, 255, 90))
    for y in range(0, H, step):
        d.line([(0, y * scale), (W * scale, y * scale)], fill=(255, 0, 255, 90))
    return big

def region(png, x0, y0, w, h, scale, name):
    # x0,y0 in pixels from the TOP-left of the image
    img = Image.open(os.path.join(ROOT, png)).convert("RGBA")
    W, H = img.size
    bg = Image.new("RGBA", img.size, (60, 60, 70, 255))
    bg.alpha_composite(img)
    crop = bg.crop((x0, y0, x0 + w, y0 + h)).resize((w * scale, h * scale), Image.NEAREST)
    d = ImageDraw.Draw(crop)
    try:
        font = ImageFont.truetype("arial.ttf", 12)
    except Exception:
        font = ImageFont.load_default()
    for (n, x, y, sw, sh) in sprites(png):
        top = H - y - sh  # unity rect y is from the bottom
        if x + sw <= x0 or x >= x0 + w or top + sh <= y0 or top >= y0 + h:
            continue
        px, py = (x - x0) * scale, (top - y0) * scale
        d.rectangle([px, py, px + sw * scale - 1, py + sh * scale - 1], outline=(255, 0, 255, 160))
        label = n.split("_")[-1]
        d.text((px + 2, py + 1), label, fill=(255, 255, 0, 255), font=font, stroke_width=2, stroke_fill=(0, 0, 0, 255))
    crop.save(os.path.join(OUT, name))

if __name__ == "__main__":
    cmd = sys.argv[1]
    if cmd == "overview":
        overview(sys.argv[2], int(sys.argv[3]), int(sys.argv[4])).save(os.path.join(OUT, sys.argv[5]))
    elif cmd == "region":
        png, x0, y0, w, h, scale, name = sys.argv[2:9]
        region(png, int(x0), int(y0), int(w), int(h), int(scale), name)
    elif cmd == "count":
        s = sprites(sys.argv[2])
        print(len(s), s[:3], s[-3:])
