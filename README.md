# Hardware Defence

*(Codename during development: Project Brenda.)*

Tower defense on a circuit board: bugs crawl along the copper trace, chewing the components
they pass, and the player defends the CPU with turrets. Made with **Godot 4.7 .NET** (C#);
it started as a GameMaker prototype, which was retired once the Godot version replaced it
(it is still in the git history, before the Godot 4.7 migration).

## Running

1. Install **Godot 4.7 .NET** and the **.NET 8 SDK** (or newer).
2. Open `project.godot` in the Godot editor and press F5 (the first run builds the C#
   project). Or open `HardwareDefence.sln` in Rider/VS Code/Visual Studio to edit the code.

Controls: pick a turret on the right (or `1`–`5`) and click a free cell; the laser asks for a
second click to aim it. Right click / `Esc` / CANCEL stops placing. `F` changes game speed,
`G` shows the grid, `Esc` pauses. Everything also works with touch. In versus as the attacker,
click a bug (or `1`–`9`) to send it.

## Modes

* **Campaign** — 8 boards (Dev Board → Server Board). Clearing a level gives upgrade points
  the first time and 1–3 stars depending on how much of the board survived.
* **Versus** — one player defends and hosts, the other spends energy to send bugs. Play
  against the built-in AI, host a match or join one. Online play uses a small WebSocket/JSON
  protocol that AIs can use too: see [docs/PROTOCOL.md](docs/PROTOCOL.md) and
  [tools/ai_attacker.py](tools/ai_attacker.py).
* **Level Editor** — draw the route, place components from a palette, edit waves on a
  timeline, test play and save. Custom levels go to `user://levels` and show up under
  *Select Level → Custom levels* and in versus.

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
  30 steps per second; values were converted).
* **Bugs** (`data/enemies.json`): HP, speed, reward, size, effect colours, how fast they chew
  components, and their versus cost/unlock time. Each bug's art is the sprite sheet
  `assets/sprites/bugs/ID.png` (see *Art* below).
* **Levels** (`data/levels/level_N.json`): route waypoints, components, theme, money, waves.
  Edit them with the Level Editor (when run from the Godot editor it can overwrite built-in
  levels) or by hand; `level_9.json` would add a level.

### Art

Everything on the board is pixel art on one grid: **1 art pixel = 3 screen pixels** and a
board cell is 16 art pixels. The game screen, menu background and editor render the board in
a low-resolution `PixelWorld` (a `SubViewport` at 1/3 scale, upscaled with sharp pixels), so
sprites, lines, particles and lights all share that grid. Keep new art at 1:1 in art pixels.

* `assets/sprites/bugs/ID.png` — 6 frames per row (walk ×4, chew ×2); row 1 holds the
  emissive pixels (eyes) drawn additively. Bugs face right and turn in 90° steps.
* `assets/sprites/turrets/` — a shared 16×16 socket, one head per turret (head + emissive
  frame) and `ID_icon.png` for menus. Heads stay upright; the game draws the barrels as
  pixel lines pointing where the turret aims (the laser turns its sprite in 90° steps).
* `assets/sprites/icons/` — 10×10 white UI icons (tinted by the UI), shown at 3×.
* Board, components and the CPU are painted pixel by pixel in code (`View/Pixel/PixelCanvas`,
  `ComponentArt`), with a 3×5 pixel font (`MicroFont`) for markings.
* `tools/sprites/make_sprites.py` generates the sprite sheets from the designs next to it
  (`--preview FILE` writes a zoomed contact sheet). The PNGs can also be edited by hand.

**Lighting.** The board is dark; LEDs along the route (one every 3 cells, alternating sides,
derived from the route so every player agrees on them) light it with wide pools of light.
Bugs chew them as they pass, and the board goes darker as they burn out. Big lit surfaces
(board, route) are drawn in tiles because a 2D light reaches at most 16 lights per item.

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

Saves: `user://save.json` (campaign), `user://settings.cfg`, `user://levels/` (custom levels).

## Developer commands

```
# all tests (data, save, simulation, bots, editor, network, a headless run of the game screen)
godot --headless --path . -- --test

# versus balance report with the built-in bots
godot --headless --path . -- --balance

# dedicated versus server (see docs/PROTOCOL.md)
godot --headless --path . -- --server --level=3 --defender=bot --attacker=remote

# screenshots (needs a display; xvfb-run works); boss:N:SECONDS drops level N's boss on the board
godot --path . -- --capture=menu,levels,game:5,attack:5,editor:7,waves:7,boss:6:6 --out=/tmp/shots

# regenerate assets/ui/theme.tres after changing src/UI/UiTheme.cs
godot --headless --path . -- --build-theme
```

## Exporting

The **Compatibility** renderer runs on Windows, Linux and Android. `export_presets.cfg` has
Windows and Linux presets (install the .NET export templates from the editor first); builds
go to `builds/` (ignored by git). Level files are JSON, so the presets include
`data/*.json` and `data/levels/*.json` as non-resource files — keep that in new presets.
(Godot 4 can't export C# projects to the web yet.)

## Credits

* Music: Juhani Junkala, *Retro Game Music Pack* (CC0).
* Fonts: Silkscreen and Share Tech Mono (SIL Open Font License, see `assets/fonts`).
