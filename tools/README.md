# Level generator

`make_levels.py` is the script that laid out the 8 built-in boards (route, components,
waves). It is kept for reference: once generated, the JSON files in `data/levels` are the
source of truth — edit them with the in-game Level Editor (or by hand).

```
cd tools
python3 make_levels.py ../data/levels   # overwrites data/levels/level_1..8.json
```

`orig_1..3.json` hold the waves of the GameMaker prototype, reused by levels 1-3.


## LED placement

`place_leds.py` appends `led` (surface-mount/SMD) and `led_5mm` components to the eight
built-in boards, using a fixed random seed per level. LED means light-emitting diode;
SMD means surface-mount device. The target is roughly one LED per three route cells,
including existing LEDs. Positions are irregular, 1–3 horizontal/vertical cell steps
from the route, clear of every existing component (including labels), route and 3×3
CPU footprint. At most 20% of the remaining cells immediately beside the route are used,
leaving the majority available for turret placement. Colours follow each board theme.

Run from the project root:

```bash
# Append only missing LEDs; preserve existing components and every other JSON field.
python3 tools/place_leds.py

# Read-only check; returns status 1 if a board still needs LEDs, otherwise status 0.
python3 tools/place_leds.py --check
```

The script preserves the existing one-component-per-line format and the waves verbatim.
Reruns keep the files byte-for-byte unchanged once the target is reached; hand-edited LEDs
remain the source of truth. To adjust a board afterwards, use the editor or edit its JSON.
Custom data directories can be passed as the first argument. Validate gameplay data with
the game's `DataTests.LevelsLoadAndValidate` and `LedTests` after changing the layout.
