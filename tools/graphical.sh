#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
client=${1:?Usage: tools/graphical.sh godot\|monogame test\|capture [arguments]}
operation=${2:?Missing operation}
shift 2
# Tests/captures own their virtual display, including on a running desktop.
for prerequisite in xvfb-run Xvfb xauth timeout; do
  command -v "$prerequisite" >/dev/null || { echo "Missing graphical prerequisite: $prerequisite. See README.md; no packages installed automatically." >&2; exit 2; }
done
export LIBGL_ALWAYS_SOFTWARE=1
export ALSOFT_DRIVERS=null
export SDL_AUDIODRIVER=dummy
export COMMON_WATCH_TEST_AUDIO=dummy
# Automated checks use disposable preferences instead of changing the player's volume.
settings_directory=$(mktemp -d)
trap 'rm -rf "$settings_directory"' EXIT
run_client() {
  timeout --kill-after=5s 90s xvfb-run -a -s '-screen 0 1920x1080x24' bash tools/client.sh "$client" --settings-path "$settings_directory/audio-settings.json" "$@"
}
case "$operation" in
  test)
    # --smoke-test runs actual native controls, commands, playback, resize and draft checks.
    for mode in debug sprites; do
      run_client --render-mode "$mode" --smoke-test "$@"
    done
    ;;
  capture)
    output="$(pwd)/artifacts/captures/$client"
    mkdir -p "$output"
    for checkpoint in settlement building-selected combat draft; do
      rm -f "$output/$checkpoint.png"
      run_client --render-mode debug --checkpoint "$checkpoint" --capture "$output/$checkpoint.png" "$@"
      test -s "$output/$checkpoint.png" || { echo "Capture not produced: $output/$checkpoint.png" >&2; exit 1; }
    done
    echo "Actual rendered captures: $output"
    ;;
  capture-art)
    output="$(pwd)/artifacts/captures/$client/art"
    mkdir -p "$output"
    capture_art() {
      name=$1; checkpoint=$2
      shift 2
      run_client --checkpoint "$checkpoint" --capture "$output/$name.png" "$@"
      test -s "$output/$name.png"
    }
    capture_art settlement-debug settlement --world-art debug --hud-art plain --view topdown
    capture_art settlement-angled settlement --world-art debug --hud-art plain --view angled --debug-overlay on
    capture_art settlement-sprites settlement --world-art sprites --hud-art plain --view angled
    capture_art combat-sprites combat --world-art sprites --hud-art illustrated --view angled
    capture_art hud-plain draft --world-art sprites --hud-art plain --view angled
    capture_art hud-illustrated draft --world-art sprites --hud-art illustrated --view angled
    capture_art nine-slice-small building-selected --world-art sprites --hud-art illustrated --view angled --width 1280 --height 720
    capture_art nine-slice-large draft --world-art sprites --hud-art illustrated --view angled --width 1920 --height 1080
    # Showroom constructs no match, even though the ordinary capture helper accepts a checkpoint.
    run_client --asset-showroom --world-art sprites --hud-art illustrated --debug-overlay on --width 1280 --height 1080 --capture "$output/asset-showroom.png"
    test -s "$output/asset-showroom.png"
    echo "Actual art captures: $output"
    ;;
  *) echo "Unknown operation: $operation" >&2; exit 2 ;;
esac
