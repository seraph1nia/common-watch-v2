#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
for prerequisite in xvfb-run Xvfb xauth timeout python3; do
  command -v "$prerequisite" >/dev/null || { echo "Missing prerequisite: $prerequisite; no packages installed" >&2; exit 2; }
done
label=${COMMON_WATCH_COMBAT_REVIEW_LABEL:-after}
for width in 1280 1920; do
  timeout --kill-after=15s 300s xvfb-run -a -s '-screen 0 2560x1440x24' \
    python3 tools/combat-sequence-network.py --width "$width" \
    --output "$(pwd)/artifacts/captures/combat-review/$label/network-6p-$width" "$@"
done
