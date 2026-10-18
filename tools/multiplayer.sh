#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
client=${1:?client}; operation=${2:?test or dev}; shift 2
if [[ $operation == dev ]]; then exec python3 tools/multiplayer.py "$client" dev "$@"; fi
for prerequisite in xvfb-run Xvfb xauth timeout python3; do
  command -v "$prerequisite" >/dev/null || { echo "Missing prerequisite: $prerequisite" >&2; exit 2; }
done
export LIBGL_ALWAYS_SOFTWARE=1 ALSOFT_DRIVERS=null SDL_AUDIODRIVER=dummy COMMON_WATCH_TEST_AUDIO=dummy
for players in 2 3 4; do
  timeout --kill-after=5s 180s xvfb-run -a -s '-screen 0 1280x720x24' python3 tools/multiplayer.py "$client" test --players "$players" "$@"
done
