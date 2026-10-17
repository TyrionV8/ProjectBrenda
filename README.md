# Hardware Defence

*(Codename during development: Project Brenda.)*

Tower defense on a circuit board: bugs follow the copper trace, sometimes leaving it to
chew nearby components before returning, while the player defends the CPU with turrets.
Made with **Godot 4.7 .NET** (C#);
it started as a GameMaker prototype, which was retired once the Godot version replaced it
(it is still in the git history, before the Godot 4.7 migration).

## Running

1. Install **Godot 4.7 .NET** and the **.NET 8 SDK** (or newer).
2. Open `project.godot` in the Godot editor and press F5 (the first run builds the C#
   project). Or open `HardwareDefence.sln` in Rider/VS Code/Visual Studio to edit the code.

Controls: pick a turret on the right (or `1`–`9`, in visible-list order) and click a free
cell; the laser asks for a second click to aim it. Right click / `Esc` / CANCEL stops placing. `F` changes game speed,
`G` shows the grid, `Esc` pauses. Everything also works with touch. In versus as the attacker,
click a bug (or `1`–`9`) to send it.

## Modes

* **Campaign** — 8 boards (Dev Board → Server Board). A victory earns 1–3 stars from board
  integrity and credits the remaining match money to the upgrade balance. Each board keeps
  its best remaining-money result: a worse replay gives no credit, a better replay gives
  only the difference. Spend this balance on progressively priced upgrades and permanent
  counter-turret unlocks. Each match still begins with that level's normal starting money;
  defeat, custom levels and versus give
  no campaign credit.
* **Versus** — one player defends and hosts, the other spends energy to send bugs. Play
  against the built-in AI, host a match or join one. Online play uses a small WebSocket/JSON
  protocol that AIs can use too: see [docs/PROTOCOL.md](docs/PROTOCOL.md) and
  [tools/ai_attacker.py](tools/ai_attacker.py).
* **Level Editor** — draw the route, place components from a palette, edit waves on a
  timeline, test play and save. Custom levels go to `user://levels` and show up under
  *Select Level → Custom levels* and in versus.
* **Bestiary** — open it from the main menu or pause menu. Bugs, bosses and turrets have
  animated portraits on the 3× pixel grid, descriptions and attributes read from the
  current catalog. Unknown entries show silhouettes and `???`. Encounter an enemy, build
  a turret or buy its unlock to discover it permanently. Returning from the pause-menu
  bestiary keeps the same match and its pause state; online matches continue running.

**Tips** in Settings controls short contextual hints. Board one introduces construction,
waves, detours, burnt-out LEDs and the victory balance; each boss also has a response hint.
Hints disappear after nine seconds or a click, without pausing play. They are recorded only
when shown, wait while the menu/bestiary covers the game, and are never repeated once saved.
The first board's victory explains that only improvement over the best remaining cash is
credited to upgrades.

## Code layout

```
  project.godot, HardwareDefence.csproj/.sln
  data/            turrets.json, enemies.json, versus.json, levels/level_N.json
  assets/          sprites (pixel art), fx textures, shaders, audio, fonts, ui/theme.tres
  scenes/          one scene per screen (the scripts build their nodes in code)
  src/
    Data/          Grid, LevelData, TurretDef, EnemyDef, ComponentTypes, Catalog, JsonIO
    Sim/           the game simulation: Match (+ turrets/projectiles), WaveRunner, VersusRules,
                   Snapshot, bots — plain C#, no scene tree
    Game/          GameScreen (renders snapshots, turns events into effects), feeds, BuildController
    View/          board, components, route trace, bugs, turrets, projectiles, CPU, FX
    Net/           versus protocol, WebSocket host (HostFeed/NetHost) and client (RemoteFeed)
    Editor/        level editor
    UI/            HUD, menus, theme
    Autoload/      Settings, Audio, Transition, PostFx, GameState (save), DevTools
    Tests/         tests run by DevTools
  tools/           level generator (used once for the built-in boards), AI client example
```

The simulation is independent of rendering: `Match.Step()` advances it with a fixed time
step and records `SimEvent`s; `Snapshot.From(match)` captures what to draw. The game screen
draws snapshots and plays events, whether they come from a local `Match`, a hosted one or
the network — which is what makes versus, AIs and headless tests share the same code.

### Data

* **Turrets** (`data/turrets.json`): `stats` + `upgrades`; each upgrade adds `per_level` to a
  stat per level bought, `limits` clamp the result. Seconds and pixels (the prototype ran at
  30 steps per second; values were converted). Upgrades cost `price + price_step * levels`
  where `levels` is the number already bought. Optional `unlock_price` requires a permanent
  purchase before upgrades become available. Initial auxiliary upgrades start at $5 and
  rise by $10 per bought level; damage, density and freezing start at $30 and rise by $15.
* **Bugs** (`data/enemies.json`): HP, speed, reward, size, effect colours, how fast they chew
  components, and their versus cost/unlock time. Each bug's art is the sprite sheet
  `assets/sprites/bugs/ID.png` (see *Art* below). `detour_chance` is rolled once per bug and
  nearby component; `detour_range` measures cells to the component rectangle,
  `detour_chew_time` limits chewing seconds, and `detour_cooldown` sets seconds on the route
  after returning. Missing fields safely disable detours. Current common bugs use a range
  of 3 cells and a 16-second cooldown, with 6 seconds chewing (8 for termites).
* **Levels** (`data/levels/level_N.json`): route waypoints, components, theme, money, waves.
  Edit them with the Level Editor (when run from the Godot editor it can overwrite built-in
  levels) or by hand; `level_9.json` would add a level.

Components take damage only when a bug walks to their edge and chews during a detour;
passing them on the route deals no automatic damage. The bug returns to its exact exit
point before continuing. It stays targetable and affected by slowing throughout the trip.
Bosses and DDoS packets never detour. Match-local seeded randomness keeps identical runs
reproducible; the `Match` constructor accepts an optional seed.

### Counter-turrets

The five original weapons are always available. Unlock these four supports permanently
from Upgrades; their construction prices use match money, while unlocking uses campaign
balance. Local campaign, versus and custom inventories follow the same saved unlocks;
the simulation and remote build commands accept all definitions.

| Turret (id) | Unlock | Build | Function |
|---|---|---|---|
| Antivirus Scanner (`antivirus`) | $60 | $25 | Exposes Rootkits while in range and permanently reveals disguised Trojans. |
| Firewall (`firewall`) | $70 | $25 | Protects shots fired inside or crossing its range from Man-in-the-Middle; deletes one DDoS packet per reload. |
| Backup Drive (`backup`) | $80 | $25 | Recovers nearby locked turrets at 4× speed and grants 1.5 seconds of lock immunity after recovery. Its own reboot also accelerates after a global panic. |
| Repair Bot (`technician`) | $90 | $30 | Repairs one damaged component at 0.6 HP per second, including LEDs; destroyed components return only after a full rebuild. |

All four can be locked by bosses; their external support is inactive until recovery.
Range and ability upgrades use the auxiliary $5 + $10-per-level price progression.
The defender bot reserves money after establishing its weapons, then buys the matching
support for an active/current-wave boss. Nine compact inventory entries leave room for
the boss health bar and footer at 1366×768.

### Art

Everything on the board is pixel art on one grid: **1 art pixel = 3 screen pixels** and a
board cell is 16 art pixels. The game screen, menu background and editor render the board in
a low-resolution `PixelWorld` (a `SubViewport` at 1/3 scale, upscaled with sharp pixels), so
sprites, lines, particles and lights all share that grid. Keep new art at 1:1 in art pixels.

* `assets/sprites/bugs/ID.png` — 15 columns: walk ×8 (0–7), chew ×6 (8–13), rest (14).
  Row 0 is colour; row 1 holds emissive eyes, markings and a subtle warm shell rim drawn
  additively. Walking follows distance travelled; chewing plays at 12 frames per second,
  and a stationary bug rests after 0.12 seconds without chewing. Frame sizes and collision
  radii are unchanged. Bugs face right and turn in 90° steps.
* Animated enemy miniatures also use the board's 3× pixel scale. Small list entries show
  clipped portraits aligned with the head; larger boxes show the whole animal. The pixel
  size stays fixed when a boss is too large for its entry.
* `assets/sprites/turrets/` — a shared 16×16 socket, one head per turret (head + emissive
  frame) and `ID_icon.png` for menus. Heads stay upright; the game draws the barrels as
  pixel lines pointing where the turret aims (the laser turns its sprite in 90° steps).
  Turrets have no dynamic lights or broad halos: the second head frame and a local status
  pixel stay emissive in the dark. Construction and firing flash those pixels; boss locks
  dim them, change the status colour and hide the laser beam. The beam remains additive.
  Antivirus Scanner, Firewall, Backup Drive and Repair Bot use green scan lines, orange
  shields, blue recovery pulses and warm repair beams on the same pixel grid. The generator
  has art defaults by id, overridden by `turrets.json` entries when available. Upgrade-screen preview sizes stay
  unchanged.
* `assets/sprites/icons/` — 10×10 white UI icons (tinted by the UI), shown at 3×.
  Includes `scan`, `shield`, `restore` and `repair` for the counter-turrets.
* Board, route, components and the CPU are painted pixel by pixel in code (`View/Pixel/PixelCanvas`,
  `ComponentArt`), with a 3×5 pixel font (`MicroFont`) for markings.
* `tools/sprites/make_sprites.py` generates the sprite sheets from the designs next to it
  (`--preview FILE.png` writes zoomed contact sheets, including all eight bosses and
  minions, and `_animation.gif` / `_bosses_animation.gif` lit/dark animation comparisons).
  The PNGs can also be edited by hand.

**Interface typography.** Use the constants in `UiTheme`: Silkscreen sizes are multiples
of 8 (16 for dense labels, 24 for a 3-pixel grid like the board, and 8 only for miniature
component captions). Descriptions, tooltips and editor fields use VT323 in multiples of
25. Its structural pixel is 80 of the font's 1000 units per em, so size 25 gives two screen
pixels per structural pixel. Both fonts are imported without antialiasing, hinting or
subpixel positioning, with oversampling 1. Upgrade cards and editor properties scroll
when their text needs more room; do not shrink the fonts to make them fit.

**Lighting.** LEDs (light-emitting diodes) are ordinary level components: `led` is a
surface-mount device (SMD) and `led_5mm` is a through-hole dome. Their coloured lenses stay
emissive while broad, gentle light pools illuminate the board. Damaged LEDs flicker softly;
destroyed LEDs turn off, reducing ambient light according to the fraction still alive.
Restoring an LED restores its light. Both packages occupy a cell, count towards board
integrity and award component energy to the versus attacker when destroyed.

The built-in levels store their irregular route-side positions in JSON.
`python3 tools/place_leds.py` appends a deterministic mix of both packages, preserving
existing components and turret space; `--check` reports a shortfall without writing files.
The editor can move or recolour them. Lights are visual only and do not change combat.
Big lit surfaces (board, route) are drawn in tiles because a 2D light reaches at most 16
lights per item; the white board uses gentler lighting.

The 8 bugs: Glitch, Spider, Termite (weak, eats the board fast), Beetle, Armored Worm,
Roach (fast), Runner and Brute.

### Bosses

Every level ends with a boss that plays out a real kind of attack (`ability` in
`data/enemies.json`, logic in `Sim/MatchBosses.cs`, effects in `View/BossFx.cs`):

| Level | Boss | Attack |
|---|---|---|
| 1 Dev Board | Trojan Horse | Arrives disguised as `FREE_RAM.EXE`; below half health it's found out, and when killed it drops a payload of bugs. |
| 2 Keyboard | Keylogger | Pulls keystrokes off the keys and steals $1 a second while alive. |
| 3 Wi-Fi Router | Man-in-the-Middle | Taps the turrets around it and intercepts half the shots fired near it. |
| 4 Sound Card | DDoS Botnet | Floods the route with junk packets that soak up fire (they don't hurt the CPU). |
| 5 Graphics Card | Rootkit | Hides every few seconds: turrets can't see or hurt it. |
| 6 Laptop | Ransomware | Encrypts nearby turrets (padlock, scrambled hex): they can't fire for a while. |
| 7 ATX Motherboard | Worm | Forks copies of itself at 2/3 and 1/3 health. |
| 8 Server Board | Kernel Panic | Blue-screens the board: every turret freezes for a moment. |

Bosses are announced, get a health bar in the HUD and cost more CPU when they get through
(`cpu_damage`). Only Kernel Panic can be sent in versus.

Each boss also has a short arrival vignette inside the board, without pausing play:
Trojan's fake download, Keylogger's typed login, Man-in-the-Middle's tapped network,
DDoS's overflowing traffic chart, Rootkit's disappearing processes, Ransomware's countdown
note, Worm's cascading fork windows and Kernel Panic's countdown with screen tears and a
brief blue-screen preview. Built-in level order increases duration from 1.93 to 4.24
seconds, panel size, particles, shake and glitch. Advanced bosses keep small process
brackets and echoes after the introduction.

Arrival windows draw below creatures and shots, clear of the interface columns, boss bar,
warning banner and build controls. Each boss id starts one introduction; removing it
clears its window and decoration. Existing intrusion warnings, process labels, ability
effects and the real Kernel Panic blue screen remain. The new board-only glitch follows
the screen-effects setting and clears when leaving the game. Terminal text uses uppercase
English in `MicroFont` on the same 3× pixel grid.

Saves: `user://save.json` (campaign), `user://settings.cfg`, `user://levels/` (custom levels).
Campaign save version 3 stores `balance`, `best_remaining_money`, `unlocked_turrets`,
`upgrades`, `stars`, unlocked levels, `seen_enemies`, `seen_turrets` and `shown_tips`.
Older saves safely default the discovery/hint sets; existing turret purchases also reveal
those entries. Version 2 points become money at 20 per point;
existing upgrades and stars are retained. New campaigns start with a zero upgrade balance.
Purchases and victory records persist before notifying the interface; the file is written
completely to a temporary path before replacing the previous save.

## Developer commands

```
# all tests (data, save, simulation, bots, editor, network, a headless run of the game screen)
godot --headless --path . -- --test

# versus balance report with the built-in bots
godot --headless --path . -- --balance

# dedicated versus server (see docs/PROTOCOL.md)
godot --headless --path . -- --server --level=3 --defender=bot --attacker=remote

# screenshots (needs a display; xvfb-run works); boss:N:SECONDS drops level N's boss on the board
godot --path . -- --capture=game:5:20 --out=/tmp/shots

# bestiary with some discovered entries; game:1:6 also demonstrates the first hint
godot --path . -- --capture=bestiary --out=/tmp/shots

# actual campaign victory and its cash record (the win:N mode remains a custom level)
godot --path . -- --capture=campaignwin:1 --out=/tmp/shots

# regenerate assets/ui/theme.tres after changing src/UI/UiTheme.cs
godot --headless --path . -- --build-theme
```

Run one screenshot mode per process, except `game:1:5,pause` for the pause overlay.
All screenshot preparations use their own disposable campaign save and restore in-memory
campaign state afterwards, including errors. They never replace the player's normal save.
`game:1` temporarily enables Tips in memory for reproducible screenshots, restoring the
preference afterwards without writing settings. `game` unlocks all four supports
for inventory inspection; board 4 starts with a damaged component and a Repair Bot.
`boss` uses a cloned board with a delayed dummy wave and places the appropriate support
beside real weapons to show its effect, without changing combat statistics. These are
demonstration setups, not unassisted campaign runs. For the full test suite, use isolated
user-data directories: some general tests intentionally write campaign files.

The same Linux test entry point is used locally and by GitHub Actions:

```bash
# Select the Godot .NET executable; the script compiles, imports and runs every test.
GODOT_BIN="/absolute/path/to/Godot_v4.7.2-stable_mono_linux.x86_64" \
  bash tools/ci/run-tests.sh
```

The script creates exclusive temporary user-data/configuration/cache directories,
preserves the first failed command's exit code and removes only its own temporary data.
The workflow in `.github/workflows/tests.yml` runs on pushes and pull requests with
.NET 8 and the official Godot 4.7.2 .NET archive, checked against its release checksum.
It compiles and tests only; it does not export or publish the game. See
[`tools/ci/README.md`](tools/ci/README.md) for setup and isolation details.

## Exporting

The **Compatibility** renderer runs on Windows, Linux and Android. `export_presets.cfg` has
Windows and Linux presets (install the .NET export templates from the editor first); builds
go to `builds/` (ignored by git). Level files are JSON, so the presets include
`data/*.json` and `data/levels/*.json` as non-resource files — keep that in new presets.
(Godot 4 can't export C# projects to the web yet.)

## Credits

* Music: Juhani Junkala, *Retro Game Music Pack* (CC0).
* Fonts: Silkscreen and VT323 (Open Font License 1.1, see `assets/fonts`). VT323 is by
  Peter Hull / The VT323 Project Authors, copyright 2011; its full licence is
  `assets/fonts/OFL-VT323.txt`. The original Share Tech Mono assets and licence are retained.
