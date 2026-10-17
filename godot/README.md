# Project Brenda (Godot 4)

Tower defense on a circuit board: bugs crawl along the copper trace and the player
defends the CPU with turrets. This is the Godot port of the original GameMaker
prototype (kept in the repository root for reference).

## Running

1. Install **Godot 4.5** (standard build, no .NET needed).
2. Open `godot/project.godot` in the Godot editor and press F5.

Controls: click a turret on the right (or press `1`–`5`), click a free cell to build.
The laser asks for a second click to choose its direction. Right click / `Esc` /
the CANCEL button stops placing. `F` changes game speed, `G` shows the grid,
`Esc` pauses. Everything also works with touch.

## Project layout

```
godot/
  project.godot           settings, autoloads, input map
  assets/                 art (SVG), fx textures, shaders, audio, fonts
  data/
    turrets/*.tres        turret stats and upgrades (edit in the inspector)
    enemies/*.tres        bug stats and colours
    levels/level_N.json   route, components, start money and waves
  scenes/                 game, turrets and UI screens
  src/
    autoload/             DB, GameState (save), Settings, Audio, Transition, PostFx, DevTools
    core/                 Grid, LevelData, TurretDef, EnemyDef, UpgradeDef
    game/                 Game, Board, PathTrace, CpuCore, Enemy/BugBody, turrets, projectiles, fx
    ui/                   HUD, menus, theme (UiTheme builds assets/ui/theme.tres)
    dev/                  AutoPlayer bot used by tests and screenshots
  tests/                  headless tests
```

### Data

* **Turrets** (`data/turrets/*.tres`): `base_stats` + `upgrades`. Each upgrade adds
  `per_level` to one stat per level bought; `stat_limits` clamps the result.
  Units are seconds and pixels (the prototype used 30 steps per second; values
  were converted).
* **Bugs** (`data/enemies/*.tres`): HP, speed (px/s), reward, size, drawing style
  and colours. They are drawn procedurally (`src/game/bug_body.gd`), so a new bug
  type is just a new resource.
* **Levels** (`data/levels/level_N.json`): the board is 21×16 cells of 48 px.
  `path` lists waypoint cells (the first may be outside the board, where bugs come
  in; the last is the CPU). `components` are decorative parts that block building.
  `waves` lists `{enemy, time}` spawns in seconds. Levels are picked up by number,
  so `level_4.json` adds a level.

Save data lives in `user://save.json` and settings in `user://settings.cfg`.

## Tests and screenshots

```
# all tests (data, save, and full simulated levels played by a bot)
godot --headless --path godot --fixed-fps 60 -- --test

# screenshots (needs a display; xvfb-run works)
godot --path godot -- --capture=menu,levels,upgrades,game:1,pause,end --out=/tmp/shots
```

After changing `src/ui/ui_theme.gd`, regenerate the theme resource:

```
godot --headless --path godot -- --build-theme
```

## Exporting

The project uses the **Compatibility** renderer so it runs on Windows, Linux,
Android and the web. `export_presets.cfg` has Windows and Linux presets (install
the export templates from the editor first); builds go to `builds/` at the
repository root. Level files are JSON, so the presets include
`data/levels/*.json` as non-resource files — keep that filter in new presets.

## Credits

* Music: Juhani Junkala, *Retro Game Music Pack* (CC0).
* Fonts: Silkscreen and Share Tech Mono (SIL Open Font License, see `assets/fonts`).
