"""Turrets: a shared socket plus one simple head per type, all 16x16 (one board cell).

The heads don't rotate as sprites (that would smear the pixels): the game draws the
barrels as pixel lines on top of the socket and under the head, pointing where the turret
aims. Only the laser turns its sprite, in 90 degree steps. Head sheets have 2 frames: the
head and its emissive pixels. *_icon.png is the whole turret for menus (barrel to the right).
Colours come from data/turrets.json, with defaults for the future counter-turrets.
"""
import json
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
    glow.points([(5, 6), (6, 5), (9, 10)], t[3])
    return head, glow


def mortar(t):
    head, glow = Canvas(S, S), Canvas(S, S)
    head.dome(C, C, 4.0, 4.0, t)
    head.outline(OUTLINE)
    head.ellipse(C, C, 2.4, 2.4, t[0])
    head.ellipse(C + 0.4, C + 0.4, 1.6, 1.6, hexc("05070a"))
    glow.points([(7, 5), (8, 5), (5, 7), (5, 8)], t[4])
    glow.points([(10, 9), (9, 10)], t[3])
    return head, glow


def burst(t):
    """A router hub; its four nozzles are drawn by the game (they spin)."""
    head, glow = Canvas(S, S), Canvas(S, S)
    head.dome(C, C, 3.6, 3.6, t)
    head.outline(OUTLINE)
    head.ellipse(C, C, 1.6, 1.6, t[0])
    head.rect(7, 7, 2, 2, t[4])
    glow.rect(7, 7, 2, 2, t[4])
    glow.points([(5, 5), (10, 5), (5, 10), (10, 10)], t[3])
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
    head.rect(6, 6, 1, 4, t[4])
    glow.rect(6, 6, 1, 4, t[3])
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
    glow.points([(7, 5), (8, 5), (7, 10), (8, 10), (5, 7), (5, 8), (10, 7), (10, 8)], t[4])
    return head, glow


def antivirus(t):
    """A scanner lens with a quarter-sweep and a bright detected contact."""
    head, glow = Canvas(S, S), Canvas(S, S)
    head.dome(C, C, 5, 5, t)
    head.outline(OUTLINE)
    head.ellipse(C, C, 3.6, 3.6, t[0])
    rim = [(5, 5), (6, 4), (7, 4), (8, 4), (9, 4), (10, 5), (11, 6), (11, 7)]
    head.points(rim, t[3])
    glow.points(rim, t[2])
    for canvas in (head, glow):
        canvas.line(7, 8, 9, 6, t[3])
        canvas.rect(7, 7, 2, 2, t[4])
        canvas.set(6, 10, t[4])
    return head, glow


def firewall(t):
    """Brick-like shield face: broad shoulders and a tapered lower tip."""
    head, glow = Canvas(S, S), Canvas(S, S)
    head.rect(3, 3, 10, 5, t[2])
    head.rect(4, 8, 8, 2, t[2])
    head.rect(5, 10, 6, 2, t[1])
    head.rect(7, 12, 2, 1, t[1])
    head.outline(OUTLINE)
    head.rect(4, 4, 8, 1, t[4])
    head.rect(4, 7, 8, 1, t[0])
    head.rect(7, 5, 1, 2, t[0])
    head.rect(6, 8, 1, 2, t[0])
    head.rect(9, 8, 1, 2, t[0])
    glow.rect(4, 4, 8, 1, t[3])
    glow.points([(4, 8), (5, 10), (6, 11), (7, 12), (8, 12), (9, 11), (10, 10), (11, 8)], t[3])
    return head, glow


def backup(t):
    """Drive case, platter and a small read/write indicator."""
    head, glow = Canvas(S, S), Canvas(S, S)
    head.rect(3, 3, 10, 10, t[1])
    head.rect(4, 4, 8, 7, t[2])
    head.outline(OUTLINE)
    head.ellipse(7.5, 7.5, 3, 3, t[3])
    head.ellipse(7.5, 7.5, 1.3, 1.3, t[0])
    head.line(8, 9, 11, 6, t[4])
    head.rect(5, 11, 3, 1, t[0])
    head.rect(10, 11, 2, 1, t[4])
    glow.points([(5, 6), (6, 5), (7, 5), (8, 5)], t[2])
    glow.line(8, 9, 11, 6, t[3])
    glow.rect(10, 11, 2, 1, t[4])
    return head, glow


def technician(t):
    """A square maintenance robot with a jointed wrench arm and friendly eyes."""
    head, glow = Canvas(S, S), Canvas(S, S)
    head.rect(3, 6, 7, 6, t[2])
    head.rect(4, 5, 5, 1, t[3])
    head.rect(4, 7, 5, 3, t[0])
    head.line(9, 9, 12, 6, t[3])
    head.rect(11, 4, 3, 3, t[3])
    head.set(12, 4, (0, 0, 0, 0))
    head.outline(OUTLINE)
    head.rect(4, 11, 5, 1, t[1])
    for canvas in (head, glow):
        canvas.points([(5, 8), (7, 8)], t[4])
        canvas.set(11, 7, t[4])
    glow.points([(11, 4), (13, 4), (12, 6)], t[3])
    return head, glow


HEADS = {"blaster": blaster, "mortar": mortar, "burst": burst, "laser": laser, "freezer": freezer,
         "antivirus": antivirus, "firewall": firewall, "backup": backup, "technician": technician}

# Art can be reviewed before simulation/economy adds these definitions to the JSON.
# Real data overlays these defaults by id, including colour and behaviour.
FUTURE_TURRETS = {
    "antivirus": {"id": "antivirus", "behavior": "antivirus", "color": "3dff6a"},
    "firewall": {"id": "firewall", "behavior": "firewall", "color": "ff8a3d"},
    "backup": {"id": "backup", "behavior": "backup", "color": "4da3ff"},
    "technician": {"id": "technician", "behavior": "technician", "color": "ffd9a0"},
}


def definitions():
    with open(os.path.join(ROOT, "data", "turrets.json")) as source:
        configured = json.load(source)
    turrets = {t["id"]: {**FUTURE_TURRETS.get(t["id"], {}), **t} for t in configured}
    for id, defaults in FUTURE_TURRETS.items():
        turrets.setdefault(id, defaults.copy())
    return list(turrets.values())


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
    turrets = definitions()
    os.makedirs(out_dir, exist_ok=True)
    sock = socket()
    sock_shadow = sock.copy()
    sock.image().save(os.path.join(out_dir, "socket.png"))
    images = {"socket": sock.image()}
    for tdef in turrets:
        behavior = tdef["behavior"]
        t = tones(hexc(tdef["color"]))
        design = HEADS.get(tdef["id"], HEADS.get(behavior))
        if design is None:
            raise ValueError(f"No turret art for {tdef['id']} ({behavior})")
        head, glow = design(t)
        glow.mask(head)
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
