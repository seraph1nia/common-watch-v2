#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
client=${1:?Usage: tools/client.sh godot\|monogame [arguments]}
shift
case "$client" in
  godot)
    command -v godot >/dev/null || { echo 'Missing Godot .NET. Install pinned mise tools; see README.md.' >&2; exit 2; }
    audio_options=()
    if [[ ${COMMON_WATCH_TEST_AUDIO:-} == dummy ]]; then audio_options+=(--audio-driver Dummy); fi
    exec godot --path src/Game.Godot --rendering-method gl_compatibility "${audio_options[@]}" -- --assets-root "$(pwd)/assets/sprites" --audio-root "$(pwd)/assets/audio" "$@"
    ;;
  monogame)
    test -f src/Game.MonoGame/bin/Debug/net10.0/Game.MonoGame.dll || { echo 'Missing MonoGame build. Run mise run build first.' >&2; exit 2; }
    exec dotnet src/Game.MonoGame/bin/Debug/net10.0/Game.MonoGame.dll --assets-root "$(pwd)/assets/sprites" --audio-root "$(pwd)/assets/audio" "$@"
    ;;
  *) echo "Unknown client: $client" >&2; exit 2 ;;
esac
