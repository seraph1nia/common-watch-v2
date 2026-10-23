#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
# Runtime extension registration without starting the editor or initializing Steam.
mkdir -p src/Game.Godot/.godot
printf '%s\n' 'res://addons/godotsteam/godotsteam.gdextension' > src/Game.Godot/.godot/extension_list.cfg

# Native PNG resources need Godot's normal import once on a fresh checkout or asset update.
forest_import_stamp=src/Game.Godot/.godot/forest-hud-imported
if [[ ! -f "$forest_import_stamp" ]] || find src/Game.Godot/UI/ForestHud src/Game.Godot/UI/PaintedForestHud -type f \( -name '*.png' -o -name '*.svg' \) -newer "$forest_import_stamp" -print -quit | grep -q .; then
  timeout --kill-after=5s 90s godot --headless --path src/Game.Godot --import
  touch "$forest_import_stamp"
fi
