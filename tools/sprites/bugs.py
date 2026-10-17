"""The bugs. Every bug faces right (+x) and is drawn on an odd-sized canvas so it can turn
around its centre pixel. A sprite sheet has 6 frames per row (4 walking, 2 chewing):
row 0 is the bug, row 1 holds its emissive pixels (eyes, glowing marks), which the game
draws additively so they stay visible on a dark board.
"""
from pixel import Canvas, hexc, ramp

WALK = [1, 0, -1, 0]
FRAMES = 6  # walk 0-3, chew 0-1


def swing(frame, group):
    if frame >= 4:
        return 0
    return WALK[frame] * (1 if group == 0 else -1)


def mirror_y(h, y):
    return h - 1 - y


class Bug:
    id = ""
    w = h = 13
    outline = hexc("0b0d10")

    def legs(self, c, frame):
        pass

    def body(self, c, frame):
        pass

    def details(self, c, frame):
        """Drawn on top of the outlined body."""
        pass

    def glow(self, c, frame):
        pass

    def chewing(self, frame):
        return frame >= 4

    def jaw(self, frame):
        """0 = closed, 1 = open (chewing frames snap the jaws)."""
        return 1 if frame == 4 else 0

    # Legs are described for the top side; the bottom side mirrors them.
    def pair_legs(self, c, legs, color, frame, knee_swing=False):
        for i, (hip, knee, foot) in enumerate(legs):
            for side in (0, 1):
                s = swing(frame, (i + side) % 2)
                k = (knee[0] + (s if knee_swing else 0), knee[1])
                f = (foot[0] + s, foot[1])
                pts = [hip, k, f]
                if side:
                    pts = [(x, mirror_y(self.h, y)) for x, y in pts]
                c.polyline(pts, color)

    def frame(self, frame):
        legs = Canvas(self.w, self.h)
        self.legs(legs, frame)
        body = Canvas(self.w, self.h)
        self.body(body, frame)
        body.outline(self.outline)
        out = legs
        out.paste(body)
        self.details(out, frame)
        glow = Canvas(self.w, self.h)
        self.glow(glow, frame)
        return out, glow


def sym(c, h, pts, col):
    """Plot points on the top side and their mirror on the bottom."""
    for x, y in pts:
        c.set(x, y, col)
        c.set(x, mirror_y(h, y), col)


# ----------------------------------------------------------------------------- 1. Glitch

class Glitch(Bug):
    """The basic bug: a little green shield beetle with a couple of corrupted pixels."""
    id = "bug"
    w = h = 13
    shell = ramp("0e2a12", "1f5f1a", "3a9a2c", "6fd34c", "c6ff8f")
    leg = hexc("3b6a2c")

    def legs(self, c, f):
        self.pair_legs(c, [((8, 5), (9, 3), (11, 2)), ((6, 4), (6, 2), (6, 1)), ((4, 5), (3, 3), (1, 2))], self.leg, f)

    def body(self, c, f):
        c.dome(5.6, 6.5, 3.8, 3.5, self.shell)
        c.ellipse(9.5, 6.5, 1.6, 1.7, hexc("1d2a14"))

    def details(self, c, f):
        c.line(2, 6, 6, 6, self.shell[1])
        sym(c, self.h, [(11, 4), (12, 3)], self.leg)
        c.points([(10, 5), (10, 7)], hexc("ff4d4d"))
        c.set(4, 4, hexc("ff4dd8"))
        c.set(5, 8, hexc("3df2ff"))

    def glow(self, c, f):
        c.points([(10, 5), (10, 7)], hexc("ff4d4d"))
        c.set(4, 4, hexc("ff4dd8", 170))
        c.set(5, 8, hexc("3df2ff", 170))


# ----------------------------------------------------------------------------- 2. Spider

class Spider(Bug):
    """Eight long legs, a velvet abdomen with a red hourglass and a cluster of red eyes."""
    id = "spider"
    w = h = 15
    abdomen = ramp("140810", "33121f", "55203a", "7d3355", "b0607f")
    head = ramp("120a0e", "24141b", "3a222c", "54333f", "7a5260")
    leg = hexc("4f2a3c")
    red = hexc("ff2d55")

    def legs(self, c, f):
        self.pair_legs(c, [
            ((10, 6), (12, 3), (14, 2)),
            ((9, 6), (10, 2), (11, 0)),
            ((8, 6), (7, 2), (6, 0)),
            ((8, 6), (5, 3), (3, 1)),
        ], self.leg, f, knee_swing=True)

    def body(self, c, f):
        c.dome(4.7, 7.5, 4.0, 3.6, self.abdomen)
        c.dome(9.6, 7.5, 2.4, 2.2, self.head)

    def details(self, c, f):
        # Hourglass, lying along the body.
        c.points([(3, 6), (3, 8), (4, 7), (5, 7), (6, 6), (6, 8)], self.red)
        c.points([(11, 6), (11, 8), (12, 7)], self.red)
        jaw = self.jaw(f)
        c.points([(12, 6 - jaw), (12, 8 + jaw)], self.leg)

    def glow(self, c, f):
        c.points([(11, 6), (11, 8), (12, 7)], self.red)
        c.points([(4, 7), (5, 7)], hexc("ff2d55", 140))


# ----------------------------------------------------------------------------- 3. Beetle

class Beetle(Bug):
    """An orange scarab: split wing cases, a shiny shield and clubbed antennae."""
    id = "beetle"
    w = h = 13
    shell = ramp("3a1a04", "7a3d06", "c4650f", "ff9a2e", "ffd89a")
    shield = ramp("2a1404", "5a2c06", "8c470c", "c46a1c", "f0a050")
    leg = hexc("6a3a0c")

    def legs(self, c, f):
        self.pair_legs(c, [((9, 5), (10, 3), (11, 2)), ((6, 4), (6, 2), (6, 1)), ((4, 4), (3, 2), (1, 1))], self.leg, f)

    def body(self, c, f):
        c.dome(5.2, 6.5, 4.5, 3.9, self.shell)
        c.dome(9.6, 6.5, 1.9, 2.5, self.shield)
        c.ellipse(11.4, 6.5, 1.1, 1.4, hexc("2a1604"))

    def details(self, c, f):
        c.line(1, 6, 7, 6, self.shell[1])
        sym(c, self.h, [(12, 4), (12, 3)], self.leg)
        c.points([(11, 5), (11, 7)], hexc("fff04d"))

    def glow(self, c, f):
        c.points([(11, 5), (11, 7)], hexc("fff04d"))


# ----------------------------------------------------------------------------- 4. Armored worm

class Armored(Bug):
    """A slow armoured pill bug: overlapping steel plates and lots of tiny legs."""
    id = "tank"
    w = h = 15
    plate = ramp("15191e", "323a44", "56616d", "8a96a3", "d2dbe3")
    leg = hexc("56606c")

    def legs(self, c, f):
        for i, x in enumerate(range(3, 12, 2)):
            for side in (0, 1):
                s = swing(f, (i + side) % 2)
                y0, y1 = 3, 1
                if side:
                    y0, y1 = mirror_y(self.h, y0), mirror_y(self.h, y1)
                c.line(x, y0, x + s, y1, self.leg)

    def body(self, c, f):
        c.dome(6.8, 7.5, 6.2, 4.6, self.plate)
        c.ellipse(12.8, 7.5, 1.4, 2.5, hexc("2b3138"))

    def details(self, c, f):
        for x in (3, 5, 7, 9):
            for y in range(self.h):
                if c.get(x, y) in self.plate[1:]:
                    c.set(x, y, self.plate[1])
                    if c.get(x + 1, y) in self.plate[2:4]:
                        c.set(x + 1, y, self.plate[self.plate.index(c.get(x + 1, y)) + 1])
        c.points([(13, 6), (13, 9)], hexc("ff6b3d"))
        c.points([(14, 5), (14, 10)], self.leg)
        c.points([(0, 6), (0, 9)], self.leg)

    def glow(self, c, f):
        c.points([(13, 6), (13, 9)], hexc("ff6b3d"))


# ----------------------------------------------------------------------------- 5. Runner

class Runner(Bug):
    """A slim, fast silverfish with banded scales, long feelers and tail bristles."""
    id = "runner"
    w = h = 15
    body_ramp = ramp("2a0a22", "7a1462", "c22f96", "ff5fd2", "ffc2ee")
    leg = hexc("8a2a72")

    def legs(self, c, f):
        self.pair_legs(c, [((10, 6), (10, 4), (12, 2)), ((8, 6), (7, 4), (8, 2)), ((6, 6), (5, 4), (4, 2))], self.leg, f)

    def body(self, c, f):
        c.dome(5.0, 7.5, 4.3, 2.0, self.body_ramp)
        c.dome(9.3, 7.5, 2.0, 1.9, self.body_ramp)
        c.ellipse(11.8, 7.5, 1.3, 1.5, self.body_ramp[1])
        c.ellipse(1.4, 7.5, 1.2, 1.0, self.body_ramp[2])

    def details(self, c, f):
        for x in (3, 5, 7):
            for y in range(5, 10):
                if c.get(x, y) in self.body_ramp[2:]:
                    c.set(x, y, self.body_ramp[1])
        sym(c, self.h, [(13, 6), (14, 5), (14, 4), (14, 3)], self.leg)
        sym(c, self.h, [(0, 6), (0, 5)], self.leg)
        c.points([(12, 6), (12, 8)], hexc("ffffff"))

    def glow(self, c, f):
        c.points([(12, 6), (12, 8)], hexc("ffd6f4"))


# ----------------------------------------------------------------------------- 6. Brute

class Brute(Bug):
    """A heavy rhinoceros beetle with a forward horn and thick legs."""
    id = "brute"
    w = h = 17
    shell = ramp("150930", "34186e", "5b30b0", "8f5cff", "d4bcff")
    shield = ramp("120828", "28124e", "40207a", "5c34a6", "8a64d6")
    horn = ramp("2a1a08", "6b5230", "b49b6a", "e6d4a6", "fff6dc")
    leg = hexc("40207a")

    def legs(self, c, f):
        legs = [((11, 6), (12, 3), (14, 2)), ((8, 5), (8, 2), (8, 1)), ((5, 5), (4, 2), (2, 1))]
        self.pair_legs(c, legs, self.leg, f)
        # Second strand makes them thick.
        thick = [((h[0], h[1]), (k[0] + 1, k[1]), (ft[0] + 1, ft[1])) for h, k, ft in legs]
        self.pair_legs(c, thick, self.leg, f)

    def body(self, c, f):
        c.dome(6.6, 8.5, 5.8, 5.0, self.shell)
        c.dome(12.0, 8.5, 2.4, 3.3, self.shield)
        c.line(13, 8, 16, 8, self.horn[2])
        c.line(14, 7, 15, 7, self.horn[3])
        c.set(16, 7, self.horn[3])

    def details(self, c, f):
        c.line(1, 8, 9, 8, self.shell[1])
        c.points([(14, 8), (15, 8)], self.horn[2])
        c.points([(13, 6), (13, 10)], hexc("ffe14a"))

    def glow(self, c, f):
        c.points([(13, 6), (13, 10)], hexc("ffe14a"))


# ----------------------------------------------------------------------------- 7. Termite (new)

class Termite(Bug):
    """New: a pale termite with huge jaws. Weak, but it eats the board three times faster."""
    id = "termite"
    w = h = 13
    belly = ramp("4a3416", "8a6a3a", "c9a774", "eed9ad", "fff6e0")
    head = ramp("2a1204", "6a3008", "a85418", "dd8a36", "ffc27a")
    leg = hexc("9a7a44")
    jaw_col = hexc("3a1a06")

    def legs(self, c, f):
        self.pair_legs(c, [((8, 5), (8, 3), (9, 2)), ((7, 5), (6, 3), (6, 2)), ((6, 5), (5, 3), (3, 2))], self.leg, f)

    def body(self, c, f):
        c.dome(3.9, 6.5, 3.6, 2.9, self.belly)
        c.dome(7.2, 6.5, 1.5, 1.6, self.belly)
        c.dome(9.5, 6.5, 2.0, 2.1, self.head)

    def details(self, c, f):
        for x in (2, 4):
            for y in range(3, 10):
                if c.get(x, y) in self.belly[2:]:
                    c.set(x, y, self.belly[1])
        open_ = 1 if f in (1, 3, 4) else 0
        # Pincers: base, then the tips curl inwards (wider apart when open).
        c.points([(11, 5), (12, 5 - open_), (11, 7), (12, 7 + open_)], self.jaw_col)
        if not open_:
            c.points([(12, 6)], self.jaw_col)
        c.points([(10, 5), (10, 7)], hexc("ffe0a0"))

    def glow(self, c, f):
        c.points([(10, 5), (10, 7)], hexc("ffb347", 200))


# ----------------------------------------------------------------------------- 8. Roach (new)

class Roach(Bug):
    """New: a glossy cockroach. Tough enough and the fastest thing on the board."""
    id = "roach"
    w = h = 15
    wings = ramp("1f0f05", "4a2510", "7a4019", "a8622b", "e0a466")
    shield = ramp("3a2410", "7a5228", "b07e44", "dcb070", "fff0c8")
    leg = hexc("5a3018")

    def legs(self, c, f):
        self.pair_legs(c, [((10, 5), (11, 3), (13, 2)), ((8, 5), (7, 2), (7, 1)), ((6, 5), (4, 3), (2, 1))], self.leg, f, knee_swing=True)

    def body(self, c, f):
        c.dome(6.0, 7.5, 5.6, 3.8, self.wings)
        c.dome(10.9, 7.5, 1.9, 3.0, self.shield)
        c.ellipse(12.9, 7.5, 1.0, 1.4, hexc("2a1408"))

    def details(self, c, f):
        c.line(1, 7, 8, 7, self.wings[1])
        c.points([(11, 6), (11, 8)], self.wings[0])
        sway = 1 if f in (0, 1) else 0
        sym(c, self.h, [(13, 6), (14, 5), (14, 4 - sway), (14, 3 - sway), (13, 2 - sway), (13, 1 - sway)], self.leg)
        c.points([(13, 7)], hexc("5a3018"))

    def glow(self, c, f):
        c.points([(13, 6), (13, 8)], hexc("ff8a3d", 160))


# ----------------------------------------------------------------------------- Boss

class KernelPanic(Bug):
    """Boss: a huge crimson beetle with a glowing core and serrated armour."""
    id = "boss"
    w = h = 29
    shell = ramp("1a0306", "4d0812", "8c1424", "c0263a", "ff7a86")
    shield = ramp("140205", "36060e", "5c0c18", "8a1424", "c43a4a")
    leg = hexc("5c0c18")
    gold = hexc("ffd34d")

    def legs(self, c, f):
        legs = [((20, 10), (23, 5), (26, 3)), ((16, 9), (16, 4), (17, 1)), ((12, 9), (11, 4), (10, 1)), ((8, 10), (5, 5), (3, 3))]
        self.pair_legs(c, legs, self.leg, f, knee_swing=True)
        self.pair_legs(c, [(h, (k[0] + 1, k[1]), (ft[0] + 1, ft[1])) for h, k, ft in legs], self.leg, f, knee_swing=True)

    def body(self, c, f):
        c.dome(11.5, 14.5, 10.0, 8.6, self.shell)
        c.dome(21.5, 14.5, 3.6, 5.6, self.shield)
        c.ellipse(25.3, 14.5, 2.0, 2.6, hexc("2a0408"))
        # Serrated edges.
        for x in range(4, 19, 3):
            for y, dy in ((6, -1), (22, 1)):
                c.set(x, y + dy, self.shell[2])
                c.set(x + 1, y + dy, self.shell[2])
                c.set(x, y + 2 * dy, self.shell[2])

    def details(self, c, f):
        c.line(2, 14, 17, 14, self.shell[1])
        # Glowing core plate.
        c.rect(9, 12, 5, 5, self.shell[0])
        c.rect(10, 13, 3, 3, self.gold)
        c.set(11, 14, hexc("ffffff"))
        jaw = self.jaw(f)
        for side in (0, 1):
            pts = [(26, 12), (27, 11 - jaw), (28, 12 - jaw), (28, 13)]
            if side:
                pts = [(x, mirror_y(self.h, y)) for x, y in pts]
            c.polyline(pts, self.leg)
        c.points([(25, 12), (25, 16), (24, 11), (24, 17)], self.gold)

    def glow(self, c, f):
        c.rect(10, 13, 3, 3, self.gold)
        c.points([(25, 12), (25, 16), (24, 11), (24, 17)], self.gold)


BUGS = [Glitch(), Spider(), Beetle(), Armored(), Runner(), Brute(), Termite(), Roach(), KernelPanic()]
