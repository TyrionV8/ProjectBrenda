# Level generator

`make_levels.py` is the script that laid out the 8 built-in boards (route, components,
waves). It is kept for reference: once generated, the JSON files in `data/levels` are the
source of truth — edit them with the in-game Level Editor (or by hand).

```
cd tools
python3 make_levels.py ../data/levels   # overwrites data/levels/level_1..8.json
```

`orig_1..3.json` hold the waves of the GameMaker prototype, reused by levels 1-3.
