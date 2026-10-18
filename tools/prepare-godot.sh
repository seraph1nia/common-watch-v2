#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
# Runtime extension registration without starting the editor or initializing Steam.
mkdir -p src/Game.Godot/.godot
printf '%s\n' 'res://addons/godotsteam/godotsteam.gdextension' > src/Game.Godot/.godot/extension_list.cfg
