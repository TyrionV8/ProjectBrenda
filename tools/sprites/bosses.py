"""Bosses (one per level) and their minions. Same sheet layout as the bugs: 6 frames
(4 walking, 2 chewing), row 0 the sprite, row 1 its emissive pixels.
"""
import math

from bugs import Bug, mirror_y, swing, sym
from pixel import Canvas, hexc, ramp


class TrojanHorse(Bug):
    """Level 1. A wooden horse on wheels, glowing cracks hinting at the payload inside.
    It arrives disguised as a harmless installer (see TrojanDisguise)."""
    id = "trojan"
    w = h = 27
    wood = ramp("2a1606", "5a3410", "8a5420", "b87a3a", "e6b878")
    dark = hexc("2a1606")
    red = hexc("ff3b3b")

    def legs(self, c, f):
        for i, x in enumerate((7, 17)):
            for side in (0, 1):
                s = swing(f, (i + side) % 2)
                y0, y1 = 9, 5
                if side:
                    y0, y1 = mirror_y(self.h, y0), mirror_y(self.h, y1)
                c.line(x, y0, x + s, y1, self.wood[1])
                c.line(x + 1, y0, x + 1 + s, y1, self.wood[1])
                # Wheel.
                wy = y1 - 1 if not side else y1
                c.rect(x - 1 + s, wy, 4, 2, self.dark)
                c.set(x + s, wy, hexc("6a6a6a"))

    def body(self, c, f):
        c.dome(12.0, 13.5, 8.0, 4.8, self.wood)
        c.dome(19.6, 13.5, 2.6, 2.3, self.wood)
        c.dome(23.0, 13.5, 2.8, 1.9, self.wood)
        c.line(1, 13, 4, 13, self.wood[2])

    def details(self, c, f):
        for x in (7, 10, 13, 16):
            for y in range(9, 18):
                if c.get(x, y) in self.wood[2:]:
                    c.set(x, y, self.wood[1])
        # Mane, ears, eyes.
        c.points([(18, 13), (19, 13), (20, 13), (21, 13)], self.dark)
        sym(c, self.h, [(21, 11)], self.wood[3])
        sym(c, self.h, [(24, 12)], self.red)
        # Cracks with the payload glowing through.
        c.points([(9, 12), (10, 13), (14, 15), (15, 14), (12, 11)], self.red)

    def glow(self, c, f):
        sym(c, self.h, [(24, 12)], self.red)
        c.points([(9, 12), (10, 13), (14, 15), (15, 14), (12, 11)], hexc("ff3b3b", 200))


class TrojanDisguise(Bug):
    """The trojan before it's found out: a shiny gift box labelled FREE (it doesn't walk, it slides)."""
    id = "trojan_disguise"
    w = h = 17
    box = ramp("0d3a20", "1d6b3a", "2fa157", "56d47e", "b8ffd0")
    ribbon = hexc("ffd34d")

    def body(self, c, f):
        c.rect(2, 2, 13, 13, self.box[2])
        c.rect(2, 2, 13, 2, self.box[3])
        c.rect(2, 2, 2, 13, self.box[3])
        c.rect(2, 13, 13, 2, self.box[1])
        c.rect(13, 4, 2, 11, self.box[1])
        c.set(3, 3, self.box[4])

    def details(self, c, f):
        c.rect(7, 2, 3, 13, self.ribbon)
        c.rect(2, 7, 13, 3, self.ribbon)
        c.points([(7, 1), (6, 0), (9, 1), (10, 0), (8, 2)], self.ribbon)
        c.rect(8, 8, 1, 1, hexc("fff6c0"))

    def glow(self, c, f):
        if f % 2 == 0:
            c.set(4, 4, hexc("ffffff", 160))


class Keylogger(Bug):
    """Level 2. A grey beetle wearing keycaps, one red recording eye and two cable feelers."""
    id = "keylogger"
    w = h = 25
    shell = ramp("15181c", "2c3138", "4a525c", "6f7a86", "aeb8c4")
    key = hexc("c9d0d8")
    red = hexc("ff2d3d")

    def legs(self, c, f):
        self.pair_legs(c, [((15, 7), (17, 4), (20, 3)), ((11, 6), (11, 3), (11, 1)), ((7, 7), (5, 4), (2, 3))], self.shell[1], f)

    def body(self, c, f):
        c.dome(10.5, 12.5, 8.4, 7.0, self.shell)
        c.dome(19.5, 12.5, 2.8, 3.4, self.shell)

    def details(self, c, f):
        for kx in (5, 9, 13):
            for ky in (8, 12, 16):
                c.rect(kx, ky, 3, 3, self.key)
                c.set(kx, ky, hexc("eef2f6"))
                c.rect(kx, ky + 2, 3, 1, hexc("8a939e"))
        c.points([(14, 9), (10, 13)], self.red)
        c.rect(20, 12, 2, 1, self.red)
        # Cable feelers.
        wiggle = 1 if f in (0, 1) else 0
        sym(c, self.h, [(22, 10), (23, 9), (23, 8 - wiggle), (24, 7 - wiggle)], hexc("1d2228"))

    def glow(self, c, f):
        c.rect(20, 12, 2, 1, self.red)
        if f % 2 == 0:
            c.rect(20, 11, 2, 3, hexc("ff2d3d", 120))


class Mitm(Bug):
    """Level 3. A spider with a router for a body: blinking status LEDs and Wi-Fi antennas."""
    id = "mitm"
    w = h = 25
    body_ramp = ramp("0f141a", "1e2731", "33414f", "4d6072", "7f95aa")
    leg = hexc("3a4856")

    def legs(self, c, f):
        self.pair_legs(c, [
            ((16, 10), (19, 5), (23, 3)),
            ((14, 10), (15, 4), (17, 1)),
            ((12, 10), (10, 4), (8, 1)),
            ((11, 10), (6, 6), (2, 4)),
        ], self.leg, f, knee_swing=True)

    def body(self, c, f):
        c.dome(8.5, 12.5, 6.8, 5.6, self.body_ramp)
        c.dome(16.5, 12.5, 3.4, 3.2, self.body_ramp)

    def details(self, c, f):
        # Router slots and status LEDs.
        for x in range(4, 13, 2):
            c.set(x, 15, self.body_ramp[0])
        c.points([(5, 10), (8, 10), (11, 10)], hexc("3dff6a"))
        c.set(11 if f % 2 else 8, 10, hexc("ffb43d"))
        # Antennas with tips.
        sym(c, self.h, [(19, 10), (20, 9), (21, 8), (22, 7), (23, 6)], hexc("c9d0d8"))
        sym(c, self.h, [(23, 5), (24, 6)], hexc("ff3d6e"))
        c.points([(19, 12), (19, 13)], hexc("ff3d6e"))

    def glow(self, c, f):
        c.points([(5, 10), (8, 10), (11, 10)], hexc("3dff6a"))
        c.set(11 if f % 2 else 8, 10, hexc("ffb43d"))
        sym(c, self.h, [(23, 5)], hexc("ff3d6e"))
        c.points([(19, 12), (19, 13)], hexc("ff3d6e"))


class Ddos(Bug):
    """Level 4. A bloated botnet queen, its back packed with data packets ready to flood the route."""
    id = "ddos"
    w = h = 27
    flesh = ramp("2a0610", "5e0f22", "9a1b36", "d9334f", "ff8aa0")
    leg = hexc("5e0f22")

    def legs(self, c, f):
        self.pair_legs(c, [((19, 8), (21, 5), (24, 4)), ((15, 6), (15, 3), (16, 1)), ((10, 6), (9, 3), (8, 1)), ((6, 8), (4, 5), (1, 4))], self.leg, f)

    def body(self, c, f):
        c.dome(12.0, 13.5, 9.6, 8.4, self.flesh)
        c.dome(22.4, 13.5, 2.6, 2.6, self.flesh)

    def details(self, c, f):
        pulse = f % 2
        for i, (px, py) in enumerate([(7, 9), (11, 8), (15, 9), (6, 13), (10, 12), (14, 13), (18, 12), (7, 17), (11, 16), (15, 17)]):
            on = (i + pulse) % 3 != 0
            c.rect(px, py, 2, 2, hexc("ffffff") if on else hexc("ff5a76"))
        jaw = self.jaw(f)
        sym(c, self.h, [(25, 12 - jaw), (26, 12 - jaw)], self.leg)
        sym(c, self.h, [(24, 12)], hexc("ffe14a"))

    def glow(self, c, f):
        pulse = f % 2
        for i, (px, py) in enumerate([(7, 9), (11, 8), (15, 9), (6, 13), (10, 12), (14, 13), (18, 12), (7, 17), (11, 16), (15, 17)]):
            if (i + pulse) % 3 != 0:
                c.rect(px, py, 2, 2, hexc("ff4d6a", 210))
        sym(c, self.h, [(24, 12)], hexc("ffe14a"))


class Rootkit(Bug):
    """Level 5. A black mantis that hides in the system: folded claws and green terminal markings."""
    id = "rootkit"
    w = h = 25
    shell = ramp("07080c", "14121f", "241f36", "3a3354", "62588a")
    green = hexc("3dff6a")

    def legs(self, c, f):
        self.pair_legs(c, [((11, 10), (10, 6), (13, 3)), ((8, 10), (6, 6), (3, 4))], self.shell[3], f, knee_swing=True)
        # Raptorial forelegs, folded forward.
        for side in (0, 1):
            pts = [(16, 10), (19, 7), (22, 8), (23, 10)]
            if side:
                pts = [(x, mirror_y(self.h, y)) for x, y in pts]
            c.polyline(pts, self.shell[3])

    def body(self, c, f):
        c.dome(8.0, 12.5, 7.4, 3.6, self.shell)
        c.dome(15.5, 12.5, 2.4, 2.0, self.shell)
        c.dome(19.5, 12.5, 2.0, 2.6, self.shell)

    def details(self, c, f):
        code = [(3, 12), (5, 11), (5, 13), (7, 12), (9, 11), (9, 13), (11, 12)]
        shift = f % 2
        for i, p in enumerate(code):
            if (i + shift) % 2 == 0:
                c.set(*p, self.green)
        sym(c, self.h, [(21, 11)], self.green)

    def glow(self, c, f):
        code = [(3, 12), (5, 11), (5, 13), (7, 12), (9, 11), (9, 13), (11, 12)]
        shift = f % 2
        for i, p in enumerate(code):
            if (i + shift) % 2 == 0:
                c.set(*p, hexc("3dff6a", 190))
        sym(c, self.h, [(21, 11)], self.green)


class Ransomware(Bug):
    """Level 6. A black and red beetle with a gold padlock on its back and chains across it."""
    id = "ransomware"
    w = h = 27
    shell = ramp("140306", "3a0a10", "6e1420", "a8243a", "e86a7c")
    gold = ramp("3a2404", "7a5212", "c48a1e", "f0c040", "fff0a0")
    chain = hexc("9aa2ab")

    def legs(self, c, f):
        legs = [((18, 7), (20, 4), (23, 3)), ((13, 6), (13, 3), (14, 1)), ((8, 7), (6, 4), (3, 3))]
        self.pair_legs(c, legs, self.shell[1], f)
        self.pair_legs(c, [(h, (k[0] + 1, k[1]), (ft[0] + 1, ft[1])) for h, k, ft in legs], self.shell[1], f)

    def body(self, c, f):
        c.dome(11.5, 13.5, 9.4, 7.6, self.shell)
        c.dome(21.0, 13.5, 2.6, 3.6, self.shell)
        c.ellipse(24.0, 13.5, 1.4, 1.8, self.shell[0])

    def details(self, c, f):
        # Chains.
        for x in range(3, 20, 2):
            c.set(x, 10 + (x // 2) % 2, self.chain)
            c.set(x, 16 + (x // 2) % 2, self.chain)
        # Padlock: shackle, body, keyhole.
        c.rect(9, 10, 1, 3, self.gold[3])
        c.rect(14, 10, 1, 3, self.gold[3])
        c.rect(10, 9, 4, 1, self.gold[3])
        c.rect(8, 13, 8, 6, self.gold[2])
        c.rect(8, 13, 8, 1, self.gold[4])
        c.rect(8, 18, 8, 1, self.gold[1])
        c.rect(11, 15, 2, 2, self.gold[0])
        sym(c, self.h, [(23, 12)], hexc("ff2d3d"))

    def glow(self, c, f):
        if f % 2 == 0:
            c.rect(8, 13, 8, 1, hexc("fff0a0", 160))
        sym(c, self.h, [(23, 12)], hexc("ff2d3d"))


class Worm(Bug):
    """Level 7. A long segmented worm, undulating; glowing code runs along it."""
    id = "worm"
    w, h = 31, 15
    segs = 6
    skin = ramp("06200c", "0f4a1c", "1f7d33", "3ec25a", "a8ff9a")
    seg_r = (3.8, 3.6, 3.3, 3.0, 2.6, 2.2)

    def offsets(self, f):
        phase = (f % 4) * math.pi / 2 if f < 4 else 0
        return [int(round(math.sin(i * 1.2 + phase) * 1.2)) for i in range(self.segs)]

    def legs(self, c, f):
        off = self.offsets(f)
        for i in range(1, self.segs):
            x = self.w - 6 - i * 4
            cy = self.h // 2 + off[i]
            c.set(x, cy - int(self.seg_r[i]) - 1, self.skin[1])
            c.set(x, cy + int(self.seg_r[i]) + 1, self.skin[1])

    def body(self, c, f):
        off = self.offsets(f)
        for i in reversed(range(self.segs)):
            x = self.w - 6 - i * 4 + 0.5
            c.dome(x, self.h / 2 + off[i], self.seg_r[i] + 0.4, self.seg_r[i], self.skin)

    def details(self, c, f):
        off = self.offsets(f)
        cy = self.h // 2 + off[0]
        jaw = self.jaw(f)
        x0 = self.w - 6
        c.points([(x0 + 3, cy - 1 - jaw), (x0 + 3, cy + 1 + jaw), (x0 + 4, cy - 2 - jaw), (x0 + 4, cy + 2 + jaw)], self.skin[0])
        c.points([(x0 + 1, cy - 2), (x0 + 1, cy + 2)], hexc("ff3b3b"))
        for i in range(1, self.segs):
            c.set(self.w - 6 - i * 4, self.h // 2 + off[i], hexc("d6ff4d"))

    def glow(self, c, f):
        off = self.offsets(f)
        cy = self.h // 2 + off[0]
        c.points([(self.w - 5, cy - 2), (self.w - 5, cy + 2)], hexc("ff3b3b"))
        for i in range(1, self.segs):
            c.set(self.w - 6 - i * 4, self.h // 2 + off[i], hexc("d6ff4d", 200))


class WormCopy(Worm):
    """A copy the worm forks off: shorter and paler."""
    id = "worm_copy"
    w, h = 21, 13
    segs = 4
    skin = ramp("0c2a12", "1a5a2a", "2f9448", "62d67a", "c8ffbe")
    seg_r = (3.2, 2.9, 2.5, 2.1)


class Packet(Bug):
    """A DDoS flood packet: a glowing data block scuttling on four little legs."""
    id = "packet"
    w = h = 9
    block = ramp("3a0610", "8c1424", "d9334f", "ff6a84", "ffd0d8")

    def legs(self, c, f):
        for i, x in enumerate((2, 5)):
            for side in (0, 1):
                s = swing(f, (i + side) % 2)
                y = 1 if not side else 7
                c.set(x + s, y, self.block[1])

    def body(self, c, f):
        c.rect(2, 2, 5, 5, self.block[2])
        c.rect(2, 2, 5, 1, self.block[3])
        c.rect(6, 2, 1, 5, self.block[1])

    def details(self, c, f):
        c.rect(3, 4, 3, 1, hexc("ffffff"))

    def glow(self, c, f):
        c.rect(3, 4, 3, 1, hexc("ff8aa0"))


BOSSES = [TrojanHorse(), TrojanDisguise(), Keylogger(), Mitm(), Ddos(), Rootkit(), Ransomware(), Worm(), WormCopy(), Packet()]
