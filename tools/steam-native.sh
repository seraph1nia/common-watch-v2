#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
if [[ ${COMMON_WATCH_STEAM_NATIVE_AUTHORIZED:-} != 1 ]]; then
  echo 'Native Steam verification is unexecuted: set COMMON_WATCH_STEAM_NATIVE_AUTHORIZED=1 only when authorized to use the logged-in eligible account. See docs/SESSIONS.md.' >&2
  exit 2
fi
if [[ ! ${COMMON_WATCH_STEAM_APP_ID:-} =~ ^[1-9][0-9]*$ ]]; then
  echo 'Set COMMON_WATCH_STEAM_APP_ID to an authorized AppID; no development AppID is assumed.' >&2
  exit 2
fi
role=${1:?Use host FRIEND_STEAM_ID, or guest LOBBY_ID}; shift
case "$role" in
  host) role_args=(--steam-host --steam-invite "${1:?Steam friend ID to invite}"); shift ;;
  guest) role_args=(--steam-join "${1:?Steam lobby ID}"); shift ;;
  *) echo 'Use host FRIEND_STEAM_ID, or guest LOBBY_ID.' >&2; exit 2 ;;
esac
# Automated native workers own their display, private credentials and preferences.
for prerequisite in xvfb-run Xvfb xauth timeout; do
  command -v "$prerequisite" >/dev/null || { echo "Missing prerequisite: $prerequisite" >&2; exit 2; }
done
worker_directory=$(mktemp -d)
trap 'rm -rf "$worker_directory"' EXIT
export LIBGL_ALWAYS_SOFTWARE=1 COMMON_WATCH_TEST_AUDIO=dummy
mkdir -p "artifacts/native-steam/godot"
report="$(pwd)/artifacts/native-steam/godot/$role.json"
rm -f "$report"
timeout --kill-after=5s 600s xvfb-run -a -s '-screen 0 1280x720x24' bash tools/client.sh --steam --render-mode debug --network-test 2 --native-verify yes --report "$report" --settings-path "$worker_directory/settings.json" --data-path "$worker_directory/private" "${role_args[@]}" "$@"
