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

import icons  # noqa: E402
import turrets  # noqa: E402
from bosses import BOSSES  # noqa: E402
from bugs import BUGS, FRAMES, WALK_FRAMES, IDLE_FRAME  # noqa: E402
from pixel import sheet  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))


def build_bugs():
    out = {}
    for bug in BUGS + BOSSES:
        frames = [bug.frame(f) for f in range(FRAMES)]
        img = sheet([[f[0] for f in frames], [f[1] for f in frames]])
        path = os.path.join(ROOT, "assets", "sprites", "bugs", f"{bug.id}.png")
        os.makedirs(os.path.dirname(path), exist_ok=True)
        img.save(path)
        out[bug.id] = img
    return out


def preview(images, path, zoom=10, designs=BUGS):
    """Walk, lifted stride, open bite and idle, plus x3 silhouettes on a dark board."""
    pad = 3
    cells = []
    for bug in designs:
        img = images[bug.id]
        fw, fh = bug.w, bug.h
        for col, bgc in ((0, (16, 44, 74, 255)), (3, (16, 44, 74, 255)), (WALK_FRAMES + 2, (196, 140, 60, 255)), (IDLE_FRAME, (7, 9, 14, 255))):
            tile = Image.new("RGBA", (fw + 2, fh + 2), bgc)
            frame = img.crop((col * fw, 0, col * fw + fw, fh))
            glow = img.crop((col * fw, fh, col * fw + fw, fh * 2))
            tile.alpha_composite(frame, (1, 1))
            tile.alpha_composite(glow, (1, 1))
            cells.append((bug, col, tile))
    big_w = max(t.width for _, _, t in cells) * 4 * zoom + pad * 5 * zoom
    rows = [cells[i:i + 4] for i in range(0, len(cells), 4)]
    big_h = sum(max(t.height for _, _, t in r) * zoom + pad * zoom for r in rows)
    small = Image.new("RGBA", (sum(b.w * 3 + 6 for b in designs) + 6, 100), (16, 44, 74, 255))
    x = 6
    for bug in designs:
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


def preview_animation(images, path, designs=BUGS):
    """Nearest-neighbour animation: walking then chewing, lit and almost unlit rows.

    The dark row approximates 12% ambient light plus additive emission; no smooth
    scaling or interpolation is used. This is a review artifact, not a game asset.
    """
    from PIL import ImageChops, ImageDraw
    cell_w = max(b.w for b in designs) + 8
    cell_h = max(b.h for b in designs) + 12
    frames = []
    for column in list(range(WALK_FRAMES)) * 2 + list(range(WALK_FRAMES, IDLE_FRAME)) * 2:
        canvas = Image.new("RGBA", (cell_w * len(designs), cell_h * 2), (9, 13, 21, 255))
        for i, bug in enumerate(designs):
            img = images[bug.id]
            body = img.crop((column * bug.w, 0, (column + 1) * bug.w, bug.h))
            glow = img.crop((column * bug.w, bug.h, (column + 1) * bug.w, bug.h * 2))
            for row in range(2):
                tile = Image.new("RGBA", body.size, (16, 35, 49, 255) if row == 0 else (3, 4, 7, 255))
                visible = body.copy()
                if row:
                    visible.putdata([(int(r * .12), int(g * .12), int(b * .12), a) for r, g, b, a in body.getdata()])
                tile.alpha_composite(visible)
                emission = Image.new("RGBA", body.size, (0, 0, 0, 255))
                emission.alpha_composite(glow)
                tile = ImageChops.add(tile.convert("RGB"), emission.convert("RGB")).convert("RGBA")
                x = i * cell_w + (cell_w - bug.w) // 2
                y = row * cell_h + 10 + (cell_h - 12 - bug.h) // 2
                canvas.alpha_composite(tile, (x, y))
                ImageDraw.Draw(canvas).text((i * cell_w + 1, row * cell_h), bug.id, fill=(220, 222, 219), font_size=7)
        frames.append(canvas.resize((canvas.width * 3, canvas.height * 3), Image.Resampling.NEAREST))
    frames[0].save(path, save_all=True, append_images=frames[1:], duration=125, loop=0, disposal=2)


def preview_row(images, path, zoom=6, bg=(16, 44, 74, 255)):
    """Images side by side, zoomed."""
    w = sum(i.width + 2 for i in images) + 2
    h = max(i.height for i in images) + 4
    out = Image.new("RGBA", (w, h), bg)
    x = 2
    for img in images:
        out.alpha_composite(img, (x, 2))
        x += img.width + 2
    out.resize((w * zoom, h * zoom), Image.NEAREST).save(path)


def preview_turrets(images, path):
    """Nine menu icons plus local emission on a board with 12% ambient light."""
    from PIL import ImageChops, ImageDraw
    definitions = turrets.definitions()
    label_width = max(ImageDraw.Draw(Image.new("RGBA", (1, 1))).textbbox((0, 0), d["id"], font_size=7)[2] for d in definitions)
    cell_w, cell_h = max(25, label_width + 4), 30
    out = Image.new("RGBA", (cell_w * len(definitions), cell_h * 3), (8, 12, 18, 255))
    draw = ImageDraw.Draw(out)
    for column, definition in enumerate(definitions):
        id = definition["id"]
        x = column * cell_w
        draw.text((x + 1, 1), id, fill=(220, 222, 219), font_size=7)
        out.alpha_composite(images[id], (x + 4, 11))
        with Image.open(os.path.join(ROOT, "assets", "sprites", "turrets", id + ".png")) as sheet:
            emission = sheet.convert("RGBA").crop((16, 0, 32, 16))
        for row, factor in ((1, 1), (2, .25)):
            body = images[id].copy()
            body.putdata([(int(r * .12), int(g * .12), int(b * .12), a) for r, g, b, a in body.getdata()])
            tile = Image.new("RGBA", (16, 16), (3, 4, 7, 255))
            tile.alpha_composite(body)
            glow = Image.new("RGBA", (16, 16), (0, 0, 0, 255))
            glow.alpha_composite(emission)
            glow = glow.point(lambda value: int(value * factor))
            tile = ImageChops.add(tile.convert("RGB"), glow.convert("RGB")).convert("RGBA")
            out.alpha_composite(tile, (x + 4, row * cell_h + 11))
            draw.text((x + 1, row * cell_h + 1), "dark" if row == 1 else "dim", fill=(150, 170, 190), font_size=7)
    out.resize((out.width * 8, out.height * 8), Image.Resampling.NEAREST).save(path)


if __name__ == "__main__":
    images = build_bugs()
    turret_images = turrets.build(os.path.join(ROOT, "assets", "sprites", "turrets"))
    icon_images = icons.build(os.path.join(ROOT, "assets", "sprites", "icons"))
    if "--preview" in sys.argv:
        out = sys.argv[sys.argv.index("--preview") + 1]
        # Kernel Panic lives in bugs.py for historical reasons; include it in
        # the boss review too so all eight level bosses can be checked together.
        boss_designs = [b for b in BUGS if b.id == "boss"] + BOSSES
        preview(images, out)
        preview(images, out.replace(".png", "_bosses.png"), designs=boss_designs)
        preview_animation(images, out.replace(".png", "_animation.gif"))
        preview_animation(images, out.replace(".png", "_bosses_animation.gif"), designs=boss_designs)
        preview_turrets(turret_images, out.replace(".png", "_turrets.png"))
        preview_row(list(icon_images.values()), out.replace(".png", "_icons.png"), 8, (40, 40, 48, 255))
    print(f"wrote {len(images)} bug sheets, {len(turret_images)} turret images, {len(icon_images)} icons")
