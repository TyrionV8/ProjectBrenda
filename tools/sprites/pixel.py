"""Tiny pixel-art toolkit used by make_sprites.py.

Coordinates are in art pixels: pixel (x, y) covers [x, x+1) x [y, y+1), so shapes are
tested against pixel centres (x + 0.5, y + 0.5). Colours are RGBA tuples.
"""
import math

from PIL import Image

CLEAR = (0, 0, 0, 0)


def hexc(s, a=255):
    s = s.lstrip("#")
    return (int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16), a)


def ramp(*colors):
    """Darkest to lightest."""
    return [hexc(c) for c in colors]


class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.px = [[CLEAR] * w for _ in range(h)]

    def inside(self, x, y):
        return 0 <= x < self.w and 0 <= y < self.h

    def get(self, x, y):
        return self.px[y][x] if self.inside(x, y) else CLEAR

    def set(self, x, y, c):
        if self.inside(x, y):
            self.px[y][x] = c

    def opaque(self, x, y):
        return self.get(x, y)[3] > 0

    def points(self, pts, c):
        for x, y in pts:
            self.set(x, y, c)

    def rect(self, x, y, w, h, c):
        for yy in range(y, y + h):
            for xx in range(x, x + w):
                self.set(xx, yy, c)

    def line(self, x0, y0, x1, y1, c):
        """Bresenham line between pixel coordinates (inclusive)."""
        dx, dy = abs(x1 - x0), -abs(y1 - y0)
        sx, sy = (1 if x0 < x1 else -1), (1 if y0 < y1 else -1)
        err = dx + dy
        while True:
            self.set(x0, y0, c)
            if x0 == x1 and y0 == y1:
                break
            e2 = 2 * err
            if e2 >= dy:
                err += dy
                x0 += sx
            if e2 <= dx:
                err += dx
                y0 += sy

    def polyline(self, pts, c):
        for a, b in zip(pts, pts[1:]):
            self.line(a[0], a[1], b[0], b[1], c)

    def ellipse_pixels(self, cx, cy, rx, ry):
        for y in range(self.h):
            for x in range(self.w):
                nx = (x + 0.5 - cx) / rx
                ny = (y + 0.5 - cy) / ry
                if nx * nx + ny * ny <= 1.0:
                    yield x, y, nx, ny

    def ellipse(self, cx, cy, rx, ry, c):
        for x, y, _, _ in list(self.ellipse_pixels(cx, cy, rx, ry)):
            self.set(x, y, c)

    def dome(self, cx, cy, rx, ry, colors, light=(-0.55, -0.6, 0.58), levels=(0.12, 0.42, 0.72, 0.93)):
        """Ellipse shaded like a dome lit from the top-left. colors: 5 tones, darkest first;
        the darkest one is left for outlines, so domes use colors[1..4]."""
        lx, ly, lz = light
        n = math.sqrt(lx * lx + ly * ly + lz * lz)
        lx, ly, lz = lx / n, ly / n, lz / n
        for x, y, nx, ny in list(self.ellipse_pixels(cx, cy, rx, ry)):
            nz = math.sqrt(max(0.0, 1 - nx * nx - ny * ny))
            i = nx * lx + ny * ly + nz * lz
            tone = 1
            for k, t in enumerate(levels):
                if i > t:
                    tone = k + 1
            self.set(x, y, colors[min(tone, len(colors) - 1)])

    def outline(self, c, diagonal=False):
        """Adds a 1 px outline around the opaque pixels (in place)."""
        add = []
        nbrs = [(1, 0), (-1, 0), (0, 1), (0, -1)]
        if diagonal:
            nbrs += [(1, 1), (-1, 1), (1, -1), (-1, -1)]
        for y in range(self.h):
            for x in range(self.w):
                if self.opaque(x, y):
                    continue
                if any(self.opaque(x + dx, y + dy) for dx, dy in nbrs):
                    add.append((x, y))
        for x, y in add:
            self.set(x, y, c)

    def paste(self, other, ox=0, oy=0):
        for y in range(other.h):
            for x in range(other.w):
                c = other.px[y][x]
                if c[3] > 0:
                    self.set(x + ox, y + oy, c)

    def mask(self, other):
        """Keep only pixels that are opaque in other."""
        for y in range(self.h):
            for x in range(self.w):
                if not other.opaque(x, y):
                    self.px[y][x] = CLEAR

    def copy(self):
        c = Canvas(self.w, self.h)
        c.px = [row[:] for row in self.px]
        return c

    def image(self):
        img = Image.new("RGBA", (self.w, self.h))
        img.putdata([c for row in self.px for c in row])
        return img


def sheet(frames_by_row):
    """Lays out rows of equally sized canvases into one image."""
    rows = len(frames_by_row)
    cols = max(len(r) for r in frames_by_row)
    w, h = frames_by_row[0][0].w, frames_by_row[0][0].h
    img = Image.new("RGBA", (w * cols, h * rows))
    for r, row in enumerate(frames_by_row):
        for c, canvas in enumerate(row):
            img.paste(canvas.image(), (c * w, r * h))
    return img
