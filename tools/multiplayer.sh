#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
operation=${1:-test}; if [[ $# -gt 0 ]]; then shift; fi
if [[ $operation == dev ]]; then exec python3 tools/multiplayer.py dev "$@"; fi
for prerequisite in xvfb-run Xvfb xauth timeout python3; do
  command -v "$prerequisite" >/dev/null || { echo "Missing prerequisite: $prerequisite" >&2; exit 2; }
done
export LIBGL_ALWAYS_SOFTWARE=1 COMMON_WATCH_TEST_AUDIO=dummy COMMON_WATCH_STEAM_DISABLED=1
for players in 1 2 3 4 5 6; do
  timeout --kill-after=5s 180s xvfb-run -a -s '-screen 0 1280x720x24' python3 tools/multiplayer.py test --players "$players" "$@"
done
