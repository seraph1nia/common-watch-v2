#!/usr/bin/env bash
# Actual desktop capture only. ffmpeg is optional and is never installed here.
set -euo pipefail
video=${1:?Video output required}; shift
mkdir -p "$(dirname "$video")"
recorder=''
group_signal=''
cleanup() {
  if [[ -n $recorder ]] && kill -0 "$recorder" 2>/dev/null; then
    if [[ -z $group_signal ]]; then kill -INT "$recorder" 2>/dev/null || true; fi
    for attempt in {1..50}; do
      kill -0 "$recorder" 2>/dev/null || break
      sleep .1
    done
    kill -KILL "$recorder" 2>/dev/null || true
  fi
  if [[ -n $recorder ]]; then wait "$recorder" || true; fi
}
trap cleanup EXIT
trap 'group_signal=1; exit 143' TERM
trap 'group_signal=1; exit 130' INT
if command -v ffmpeg >/dev/null; then
  ffmpeg -nostdin -hide_banner -loglevel error -y -f x11grab -framerate 15 -video_size 2560x1440 -i "$DISPLAY" \
    -an -c:v libx264 -preset ultrafast -crf 24 -vf scale=1280:720 "$video" > "${video%.mkv}.recording.log" 2>&1 &
  recorder=$!
else
  echo 'COMBAT_VIDEO_UNAVAILABLE ffmpeg absent; native measurements and stills remain available'
fi
bash tools/client.sh "$@"
