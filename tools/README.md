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

## Campaign waves

`make_waves.py` replaces only the `waves` JSON value in the eight built-in levels;
all surrounding bytes (including route, components, LEDs, music and money) stay intact.
Each spawn remains on its own line. Fixed per-board random seeds produce identical files
on repeated runs. This deliberately replaces hand-edited waves; the other level fields
remain the source of truth.

```bash
# From the project root: regenerate only waves, keeping every other level field.
python3 tools/make_waves.py

# Read-only verification; status 1 means the stored waves differ from the generator.
python3 tools/make_waves.py --check
```

An optional directory argument selects another folder of `level_1..8.json` files.
The boards have **3, 4, 6, 8, 11, 15, 20 and 25 waves**. Each group has 5–8 ordinary
bugs, spaced 1.5–2 seconds apart; the appropriate boss is the very last spawn, with
15 extra seconds of breathing room. Glitch/Spider start the progression, followed by
Termite on board 2, Beetle on 3, Roach on 4, Armored Worm on 5, Runner on 6 and Brute
on 7. Later groups favour variety and existing rewards, with no ordinary HP (hit points),
speed or reward inflation. The final boards mix light bugs with every heavy species.

Boss CPU (central processing unit) damage is 5, 7, 9, then 10 on boards 4–8. The latter
ends the match in the escape step, including Kernel Panic in versus. Packets still cause
zero damage; other minions keep their ordinary damage. `WaveTests` checks counts, order,
spacing, damage, immediate defeat and actual live populations, including boss-created
minions. It measures a reference defender, a completely idle player and an isolated
idle boss encounter, with a limit of 36 living creatures; versus population is outside
this campaign pacing check.

`SimTests.BotClearsEveryLevel` requires eight actual victories within an 80-minute
simulation ceiling and reports CPU, board integrity, duration, remaining money and
upgrade use. The reference bot uses normal construction prices, anticipates the public
campaign boss for support purchases, and adds damaging weapons after its initial freezer.
No campaign upgrade levels or extra match money are granted by this test. This confirms
that the boards are winnable; it is not a fine difficulty target. Long routes and 25
separate groups can produce long matches; fine balance remains deferred.
