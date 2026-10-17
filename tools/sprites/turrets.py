"""Turrets: a shared socket plus one simple head per type, all 16x16 (one board cell).

The heads don't rotate as sprites (that would smear the pixels): the game draws the
barrels as pixel lines on top of the socket and under the head, pointing where the turret
aims. Only the laser turns its sprite, in 90 degree steps. Head sheets have 2 frames: the
head and its emissive pixels. *_icon.png is the whole turret for menus (barrel to the right).
Colours come from data/turrets.json.
"""
import json
import math
import os

from pixel import Canvas, hexc

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUTLINE = hexc("0b0d10")
S = 16
C = 8  # centre (between pixels 7 and 8)


def mix(a, b, t):
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(3)) + (255,)


def tones(base):
    """Five tones from a base colour, darkest first."""
    black, white = hexc("05070a"), hexc("ffffff")
    return [mix(black, base, 0.18), mix(black, base, 0.42), mix(black, base, 0.7), base, mix(base, white, 0.55)]


def socket():
    c = Canvas(S, S)
    c.rect(1, 1, 14, 14, hexc("262c34"))
    c.rect(1, 1, 14, 1, hexc("3d4650"))
    c.rect(1, 1, 1, 14, hexc("343c45"))
    c.rect(1, 14, 14, 1, hexc("12161b"))
    c.rect(14, 1, 1, 14, hexc("171b21"))
    for x, y in ((3, 3), (12, 3), (3, 12), (12, 12)):
        c.set(x, y, hexc("8a939e"))
        c.set(x + 1, y + 1, hexc("12161b"))
    c.ellipse(C, C, 5.6, 5.6, hexc("14181d"))
    c.ellipse(C, C, 4.6, 4.6, hexc("1c2128"))
    return c


def blaster(t):
    head, glow = Canvas(S, S), Canvas(S, S)
    head.dome(C, C, 3.6, 3.6, t)
    head.outline(OUTLINE)
    head.rect(7, 7, 2, 2, t[4])
    glow.rect(7, 7, 2, 2, t[4])
    return head, glow


def mortar(t):
    head, glow = Canvas(S, S), Canvas(S, S)
    head.dome(C, C, 4.0, 4.0, t)
    head.outline(OUTLINE)
    head.ellipse(C, C, 2.4, 2.4, t[0])
    head.ellipse(C + 0.4, C + 0.4, 1.6, 1.6, hexc("05070a"))
    glow.points([(7, 6), (6, 7)], t[3])
    return head, glow


def burst(t):
    """A router hub; its four nozzles are drawn by the game (they spin)."""
    head, glow = Canvas(S, S), Canvas(S, S)
    head.dome(C, C, 3.6, 3.6, t)
    head.outline(OUTLINE)
    head.ellipse(C, C, 1.6, 1.6, t[0])
    head.rect(7, 7, 2, 2, t[4])
    glow.rect(7, 7, 2, 2, t[4])
    return head, glow


def laser(t):
    head, glow = Canvas(S, S), Canvas(S, S)
    body = [hexc("1d1a22"), hexc("2f2a36"), hexc("4a4352"), hexc("6a6274"), hexc("9a92a4")]
    head.rect(3, 5, 9, 6, body[2])
    head.rect(3, 5, 9, 1, body[3])
    head.rect(3, 10, 9, 1, body[1])
    head.rect(6, 5, 2, 6, t[2])
    head.rect(6, 5, 2, 1, t[3])
    head.rect(1, 6, 2, 4, body[1])
    head.rect(12, 6, 2, 4, t[3])
    head.outline(OUTLINE)
    head.rect(12, 7, 2, 2, t[4])
    glow.rect(12, 6, 2, 4, t[3])
    glow.rect(12, 7, 2, 2, t[4])
    return head, glow


def freezer(t):
    head, glow = Canvas(S, S), Canvas(S, S)
    # Hexagonal crystal with a snowflake.
    for y in range(S):
        for x in range(S):
            dx, dy = abs(x + 0.5 - C), abs(y + 0.5 - C)
            if dy <= 4.4 and dx + dy * 0.55 <= 5.0:
                head.set(x, y, t[2] if (x + y) % 2 else t[3])
    head.dome(C, C, 3.2, 3.6, t)
    head.outline(OUTLINE)
    flake = hexc("f4fdff")
    head.points([(7, 5), (8, 5), (7, 10), (8, 10), (5, 7), (5, 8), (10, 7), (10, 8)], flake)
    head.rect(7, 7, 2, 2, flake)
    glow.rect(7, 7, 2, 2, flake)
    glow.points([(7, 5), (8, 10), (5, 8), (10, 7)], t[4])
    return head, glow


HEADS = {"blaster": blaster, "mortar": mortar, "burst": burst, "laser": laser, "freezer": freezer}


def barrels(behavior, t, c):
    """The barrels as drawn in game, pointing right (for icons)."""
    steel, steel_dark = hexc("b8c0ca"), hexc("5a626c")
    if behavior == "blaster":
        c.rect(8, 7, 8, 2, steel)
        c.rect(8, 8, 8, 1, steel_dark)
        c.rect(15, 7, 1, 2, t[4])
    elif behavior == "mortar":
        c.rect(8, 6, 7, 4, t[1])
        c.rect(8, 6, 7, 1, t[2])
        c.rect(14, 6, 1, 4, t[0])
    elif behavior == "burst":
        for sx, sy in ((1, 1), (-1, 1), (1, -1), (-1, -1)):
            for d in range(3, 7):
                x = 7 + sx * d if sx > 0 else 8 + sx * d
                y = 7 + sy * d if sy > 0 else 8 + sy * d
                c.rect(min(x, x + sx), min(y, y + sy), 2, 1, steel)
            tip_x = 7 + sx * 6 if sx > 0 else 8 + sx * 6
            tip_y = 7 + sy * 6 if sy > 0 else 8 + sy * 6
            c.set(tip_x, tip_y, t[4])


def build(out_dir):
    turrets = json.load(open(os.path.join(ROOT, "data", "turrets.json")))
    os.makedirs(out_dir, exist_ok=True)
    sock = socket()
    sock_shadow = sock.copy()
    sock.image().save(os.path.join(out_dir, "socket.png"))
    images = {"socket": sock.image()}
    for tdef in turrets:
        behavior = tdef["behavior"]
        t = tones(hexc(tdef["color"]))
        head, glow = HEADS[behavior](t)
        sheet = Canvas(S * 2, S)
        sheet.paste(head, 0, 0)
        sheet.paste(glow, S, 0)
        sheet.image().save(os.path.join(out_dir, f"{tdef['id']}.png"))
        icon = sock_shadow.copy()
        barrels(behavior, t, icon)
        icon.paste(head)
        icon.paste(glow)
        icon.image().save(os.path.join(out_dir, f"{tdef['id']}_icon.png"))
        images[tdef["id"]] = icon.image()
    return images
