#!/usr/bin/env python3
"""Validate the bilingual catalog without requiring Godot or additional libraries."""
import csv
import json
import re
from pathlib import Path

root = Path(__file__).resolve().parents[1]
if not (root / 'project.godot').exists():
    root = Path.cwd()
with (root / 'translations/strings.csv').open(newline='', encoding='utf-8') as source:
    reader = csv.DictReader(source)
    assert reader.fieldnames == ['keys', 'en', 'pt_BR'], 'Expected keys,en,pt_BR columns'
    rows = list(reader)
keys = [row['keys'] for row in rows]
assert len(keys) == len(set(keys)), 'Duplicate translation keys'
for row in rows:
    assert all(row.get(field, '').strip() for field in ['keys', 'en', 'pt_BR']), row
    slots = lambda text: sorted(re.findall(r'\{\d+\}', text))
    assert slots(row['en']) == slots(row['pt_BR']), row['keys']
required = set()
for file, prefix in [('turrets', 'TURRET'), ('enemies', 'ENEMY')]:
    for entry in json.loads((root / f'data/{file}.json').read_text()):
        required.update(f'{prefix}_{entry["id"].upper()}_{suffix}' for suffix in ['NAME', 'DESCRIPTION'])
        for upgrade in entry.get('upgrades', []):
            required.update(f'UPGRADE_{entry["id"].upper()}_{upgrade["id"].upper()}_{suffix}' for suffix in ['NAME', 'DESCRIPTION'])
for level in range(1, 9):
    required.update(f'LEVEL_{level}_{suffix}' for suffix in ['TITLE', 'SUBTITLE'])
assert not required.difference(keys), sorted(required.difference(keys))
print(f'{len(rows)} unique complete keys; compatible placeholders; {len(required)} required data keys present.')
