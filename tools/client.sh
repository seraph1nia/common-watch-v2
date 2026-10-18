#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
command -v godot >/dev/null || { echo 'Missing Godot .NET. Install pinned mise tools; see README.md.' >&2; exit 2; }
bash tools/prepare-godot.sh
audio_options=()
if [[ ${COMMON_WATCH_TEST_AUDIO:-} == dummy ]]; then audio_options+=(--audio-driver Dummy); fi
exec godot --path src/Game.Godot --rendering-method gl_compatibility "${audio_options[@]}" -- --assets-root "$(pwd)/assets/sprites" --audio-root "$(pwd)/assets/audio" "$@"
