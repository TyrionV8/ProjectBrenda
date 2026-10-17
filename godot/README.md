# Project Brenda (Godot 4 .NET)

Tower defense on a circuit board: bugs crawl along the copper trace, chewing the components
they pass, and the player defends the CPU with turrets. Godot 4.5 **.NET** (C#) port of the
original GameMaker prototype (kept in the repository root for reference).

## Running

1. Install **Godot 4.5 .NET** and the **.NET 8 SDK**.
2. Open `godot/project.godot` in the Godot editor and press F5 (the first run builds the C#
   project). Or open `ProjectBrenda.sln` in Rider/VS Code/Visual Studio to edit the code.

Controls: pick a turret on the right (or `1`–`5`) and click a free cell; the laser asks for a
second click to aim it. Right click / `Esc` / CANCEL stops placing. `F` changes game speed,
`G` shows the grid, `Esc` pauses. Everything also works with touch. In versus as the attacker,
click a bug (or `1`–`7`) to send it.

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
godot/
  project.godot, ProjectBrenda.csproj/.sln
  data/            turrets.json, enemies.json, versus.json, levels/level_N.json
  assets/          art (SVG), fx textures, shaders, audio, fonts, ui/theme.tres
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
* **Bugs** (`data/enemies.json`): HP, speed, reward, size, drawing style and colours, how fast
  they chew components, and their versus cost/unlock time. Bugs are drawn procedurally, so a
  new type is just a new entry.
* **Levels** (`data/levels/level_N.json`): route waypoints, components, theme, money, waves.
  Edit them with the Level Editor (when run from the Godot editor it can overwrite built-in
  levels) or by hand; `level_9.json` would add a level.

Saves: `user://save.json` (campaign), `user://settings.cfg`, `user://levels/` (custom levels).

## Developer commands

```
# all tests (data, save, simulation, bots, editor, network, a headless run of the game screen)
godot --headless --path godot -- --test

# versus balance report with the built-in bots
godot --headless --path godot -- --balance

# dedicated versus server (see docs/PROTOCOL.md)
godot --headless --path godot -- --server --level=3 --defender=bot --attacker=remote

# screenshots (needs a display; xvfb-run works)
godot --path godot -- --capture=menu,levels,game:5,attack:5,editor:7,waves:7 --out=/tmp/shots

# regenerate assets/ui/theme.tres after changing src/UI/UiTheme.cs
godot --headless --path godot -- --build-theme
```

## Exporting

The **Compatibility** renderer runs on Windows, Linux and Android. `export_presets.cfg` has
Windows and Linux presets (install the .NET export templates from the editor first); builds
go to `builds/` at the repository root. Level files are JSON, so the presets include
`data/*.json` and `data/levels/*.json` as non-resource files — keep that in new presets.
(Godot 4 can't export C# projects to the web yet.)

## Credits

* Music: Juhani Junkala, *Retro Game Music Pack* (CC0).
* Fonts: Silkscreen and Share Tech Mono (SIL Open Font License, see `assets/fonts`).
