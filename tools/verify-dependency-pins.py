#!/usr/bin/env python3
"""Verify the vendored native/managed release files without downloading anything."""
import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parent.parent
for relative in ('src/Game.Godot/addons/godotsteam',):
    directory = root / relative
    manifest = json.loads((directory / 'manifest.json').read_text())
    for filename, expected in manifest['files'].items():
        actual = hashlib.sha256((directory / filename).read_bytes()).hexdigest()
        if actual != expected:
            raise SystemExit(f'Pinned dependency mismatch: {relative}/{filename}')
    print(f'Verified {relative}')
