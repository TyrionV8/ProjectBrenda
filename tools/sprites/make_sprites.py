#!/usr/bin/env python3
"""Generates the pixel-art sprite sheets in assets/sprites from the designs in this folder.

    python3 tools/sprites/make_sprites.py            # writes assets/sprites/...
    python3 tools/sprites/make_sprites.py --preview out.png   # also a zoomed contact sheet

All art uses the same pixel grid: 1 art pixel = 3 screen pixels in game (a board cell is
16 art pixels). The PNGs can also be edited by hand; rerunning this script overwrites them.
"""
import os
import sys

from PIL import Image

sys.path.insert(0, os.path.dirname(__file__))

from bugs import BUGS, FRAMES  # noqa: E402
from pixel import sheet  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))


def build_bugs():
    out = {}
    for bug in BUGS:
        frames = [bug.frame(f) for f in range(FRAMES)]
        img = sheet([[f[0] for f in frames], [f[1] for f in frames]])
        path = os.path.join(ROOT, "assets", "sprites", "bugs", f"{bug.id}.png")
        os.makedirs(os.path.dirname(path), exist_ok=True)
        img.save(path)
        out[bug.id] = img
    return out


def preview(images, path, zoom=10):
    """Big frames (walk 0, walk 2, chew 0 with the glow added) on board and copper colours,
    then the same sprites at game scale (x3)."""
    pad = 3
    cells = []
    for bug in BUGS:
        img = images[bug.id]
        fw, fh = bug.w, bug.h
        for col, bgc in ((0, (16, 44, 74, 255)), (2, (16, 44, 74, 255)), (4, (196, 140, 60, 255))):
            tile = Image.new("RGBA", (fw + 2, fh + 2), bgc)
            frame = img.crop((col * fw, 0, col * fw + fw, fh))
            glow = img.crop((col * fw, fh, col * fw + fw, fh * 2))
            tile.alpha_composite(frame, (1, 1))
            tile.alpha_composite(glow, (1, 1))
            cells.append((bug, col, tile))
    big_w = max(t.width for _, _, t in cells) * 3 * zoom + pad * 4 * zoom
    rows = [cells[i:i + 3] for i in range(0, len(cells), 3)]
    big_h = sum(max(t.height for _, _, t in r) * zoom + pad * zoom for r in rows)
    small = Image.new("RGBA", (sum(b.w * 3 + 6 for b in BUGS) + 6, 100), (16, 44, 74, 255))
    x = 6
    for bug in BUGS:
        f = images[bug.id].crop((0, 0, bug.w, bug.h))
        small.alpha_composite(f.resize((bug.w * 3, bug.h * 3), Image.NEAREST), (x, 8))
        x += bug.w * 3 + 6
    out = Image.new("RGBA", (max(big_w, small.width * 3), big_h + small.height * 3), (8, 12, 18, 255))
    y = 0
    for r in rows:
        x = pad * zoom
        for _, _, t in r:
            out.paste(t.resize((t.width * zoom, t.height * zoom), Image.NEAREST), (x, y))
            x += t.width * zoom + pad * zoom
        y += max(t.height for _, _, t in r) * zoom + pad * zoom
    out.paste(small.resize((small.width * 3, small.height * 3), Image.NEAREST), (0, y))
    out.save(path)


if __name__ == "__main__":
    images = build_bugs()
    if "--preview" in sys.argv:
        preview(images, sys.argv[sys.argv.index("--preview") + 1])
    print(f"wrote {len(images)} bug sheets")
