#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
for prerequisite in xvfb-run Xvfb xauth timeout python3; do
  command -v "$prerequisite" >/dev/null || { echo "Missing prerequisite: $prerequisite" >&2; exit 2; }
done
export LIBGL_ALWAYS_SOFTWARE=1 ALSOFT_DRIVERS=null SDL_AUDIODRIVER=dummy COMMON_WATCH_TEST_AUDIO=dummy
exec timeout --kill-after=5s 180s xvfb-run -a -s '-screen 0 1280x720x24' python3 tools/reconnect.py "${1:?client}"
