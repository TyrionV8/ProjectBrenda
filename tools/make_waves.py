#!/usr/bin/env python3
"""Regenerate campaign waves only; retain all surrounding JSON bytes verbatim."""
import argparse
import json
from pathlib import Path
import random

COUNTS = (3, 4, 6, 8, 11, 15, 20, 25)
BOSSES = ('trojan', 'keylogger', 'mitm', 'ddos', 'rootkit', 'ransomware', 'worm', 'boss')
# Introductions are spread over boards and, within each board, over its waves.
POOLS = tuple(
    ('bug', 'spider', 'termite', 'beetle', 'roach', 'tank', 'runner', 'brute')[:size]
    for size in (2, 3, 4, 5, 6, 7, 8, 8)
)


def waves_for(level):
    rng = random.Random(4700 + level)
    count = COUNTS[level - 1]
    pool = POOLS[level - 1]
    waves = []
    for index in range(count):
        # At most eight ordinary bugs: extra waves add variety, not a growing swarm.
        size = 5 + min(3, index * 4 // count)
        available = pool[:min(len(pool), 2 + index * (len(pool) - 1) // count)]
        # The first two groups establish affordable defences, then variety and rewards
        # rise with existing species rather than adding bodies or inflated statistics.
        choices = available if index < 2 or level == 2 else available[1:]
        enemies = [rng.choice(choices) for _ in range(size)]
        # Ensure every newly introduced type is actually encountered.
        enemies[-1] = available[-1]
        interval = (2.0, 1.75, 1.5)[min(2, index * 3 // count)]
        wave = [dict(enemy=enemy, time=round(i * interval, 2)) for i, enemy in enumerate(enemies)]
        if index == count - 1:
            wave.append(dict(enemy=BOSSES[level - 1], time=round(size * interval + 15, 2)))
        waves.append(wave)
    return waves


def replace_waves(source, waves):
    # Locate the value with the JSON parser, rather than reformatting the whole file.
    marker = '"waves":'
    start = source.index(marker) + len(marker)
    while source[start].isspace():
        start += 1
    _, length = json.JSONDecoder().raw_decode(source[start:])
    compact = lambda value: json.dumps(value, separators=(', ', ': '))
    value = '[\n' + ',\n'.join('  [\n' + ',\n'.join('   ' + compact(s) for s in wave) + '\n  ]' for wave in waves) + '\n ]'
    return source[:start] + value + source[start + length:]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('directory', nargs='?', type=Path, default=Path(__file__).resolve().parent.parent / 'data/levels')
    parser.add_argument('--check', action='store_true', help='Verify generated waves without writing files')
    args = parser.parse_args()
    stale = False
    for level in range(1, 9):
        path = args.directory / f'level_{level}.json'
        source = path.read_bytes().decode('utf-8')
        result = replace_waves(source, waves_for(level))
        if source != result:
            if args.check:
                print(f'{path.name}: waves need regeneration')
                stale = True
            else:
                path.write_bytes(result.encode('utf-8'))
        print(f'Level {level}: {COUNTS[level-1]} waves, {sum(map(len, waves_for(level)))} scheduled bugs')
    return int(stale)


if __name__ == '__main__':
    raise SystemExit(main())
