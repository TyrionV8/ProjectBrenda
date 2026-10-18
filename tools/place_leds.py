#!/usr/bin/env python3
"""Append irregular LED components without touching existing components or other JSON data.

The level files remain the source of truth: rerunning fills only a shortfall against the
route-length target. No generated marker, replacement or deletion of hand-edited LEDs.
"""
import argparse
import json
import math
import random
import re
from pathlib import Path

COLS, ROWS = 21, 16  # src/Data/Grid.cs; one component occupies one board cell.
LED_TYPES = {"led", "led_5mm"}
# Server Board's seed leaves its decisive turret sites free (reference bot verified).
SEED_OVERRIDES = {8: 0x1ED00B}
PALETTES = {
    "blue": ("6ca8ff", "3ef0ff", "ffe0aa", "ffb43d"),
    "white": ("ffb43d", "3dff6a", "6ca8ff", "ffe0aa"),
    "green": ("b4ff9a", "3dff6a", "ffd34d", "ffe0aa"),
    "red": ("ffc79a", "ffb43d", "ffe0aa", "3ef0ff"),
    "black": ("c8dcff", "6ca8ff", "ffb43d", "3ef0ff"),
    "purple": ("e6b8ff", "6ca8ff", "ffb43d", "ffe0aa"),
}


def distance(a, b):
    """Manhattan distance: minimum number of horizontal/vertical cell steps."""
    return abs(a[0] - b[0]) + abs(a[1] - b[1])


def route_cells(points):
    cells = set()
    for a, b in zip(points, points[1:]):
        if a[0] != b[0] and a[1] != b[1]:
            raise ValueError("Route must use horizontal/vertical segments")
        steps = distance(a, b)
        dx, dy = (b[0] - a[0]) // max(steps, 1), (b[1] - a[1]) // max(steps, 1)
        cells.update((a[0] + dx * n, a[1] + dy * n) for n in range(steps + 1)
                     if 0 <= a[0] + dx * n < COLS and 0 <= a[1] + dy * n < ROWS)
    return cells


def additions(level, seed):
    route = route_cells(level["path"])
    cpu = level["path"][-1]
    occupied = set(route)
    occupied.update((cpu[0] + dx, cpu[1] + dy) for dx in (-1, 0, 1) for dy in (-1, 0, 1))
    leds = [c for c in level["components"] if c["type"] in LED_TYPES]
    positions = [tuple(c["cell"]) for c in leds]
    # Even decorative labels are left unobscured, though they don't block turrets.
    for component in level["components"]:
        x, y = component["cell"]
        width, height = component.get("size", [1, 1])
        occupied.update((x + dx, y + dy) for dx in range(width) for dy in range(height))
    free = [(x, y) for y in range(ROWS) for x in range(COLS) if (x, y) not in occupied]
    distances = {cell: min(distance(cell, p) for p in route) for cell in free}
    candidates = [cell for cell in free if 1 <= distances[cell] <= 3]
    # Spend at most 20% of the remaining immediately adjacent cells (the prime turret sites).
    adjacent_budget = sum(distances[cell] == 1 for cell in free) // 5
    target = math.ceil(len(route) / 3)
    rng = random.Random(seed)
    added = []
    while len(leds) + len(added) < target and candidates:
        allowed = [cell for cell in candidates if distances[cell] > 1 or adjacent_budget > 0]
        if not allowed:
            raise ValueError(f"Not enough free LED cells for target {target}")
        # Random weighted sampling encourages coverage, without alternating sides, regular
        # spacing or a fixed row/column pattern. Existing LEDs influence coverage as well.
        weights = []
        for cell in allowed:
            gap = min((distance(cell, p) for p in positions), default=6)
            aligned = sum(cell[0] == p[0] or cell[1] == p[1] for p in positions)
            weights.append((1 + min(gap, 8)) ** 1.7 / (1 + aligned * 0.6))
        cell = rng.choices(allowed, weights=weights, k=1)[0]
        kind = "led_5mm" if not any(c["type"] == "led_5mm" for c in leds + added) else rng.choice(("led", "led_5mm"))
        added.append({"type": kind, "cell": list(cell), "size": [1, 1],
                      "color": rng.choice(PALETTES.get(level.get("theme"), PALETTES["blue"]))})
        positions.append(cell)
        candidates.remove(cell)
        adjacent_budget -= distances[cell] == 1
    if len(leds) + len(added) < target:
        raise ValueError(f"Not enough free cells for {target} LEDs")
    return added


def process(path, check):
    original = path.read_text()
    level = json.loads(original)
    number = int(re.search(r"level_(\d+)\.json$", path.name).group(1))
    added = additions(level, seed=SEED_OVERRIDES.get(number, 0x1ED000 + number))
    if added and not check:
        # Insert only at the end of the components block; preserve all existing bytes,
        # including the one-component-per-line layout and the complete waves block.
        match = re.search(r'("components"\s*:\s*\[)(.*?)(\n \],)', original, flags=re.S)
        if not match:
            raise ValueError(f"Unsupported components layout in {path}")
        block = match.group(2).rstrip() + ",\n" + ",\n".join("  " + json.dumps(c) for c in added)
        updated = original[:match.start(2)] + block + original[match.end(2):]
        after = json.loads(updated)
        assert after["components"] == level["components"] + added
        assert {k: v for k, v in after.items() if k != "components"} == {k: v for k, v in level.items() if k != "components"}
        path.write_text(updated)
    total = sum(c["type"] in LED_TYPES for c in level["components"]) + len(added)
    print(f"{path.name}: {'missing' if check else 'added'} {len(added)}, total {total}")
    return bool(added)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory", nargs="?", type=Path, default=Path(__file__).resolve().parent.parent / "data/levels")
    parser.add_argument("--check", action="store_true", help="Report missing LEDs without writing files; exit 1 if any are missing")
    args = parser.parse_args()
    missing = False
    for number in range(1, 9):
        missing |= process(args.directory / f"level_{number}.json", args.check)
    return int(args.check and missing)


if __name__ == "__main__":
    raise SystemExit(main())
