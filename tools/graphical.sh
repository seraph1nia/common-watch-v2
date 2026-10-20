#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
operation=${1:?Usage: tools/graphical.sh test\|capture\|capture-art\|capture-battle\|profile-battle [arguments]}
shift
# Tests/captures own their virtual display, including on a running desktop.
for prerequisite in xvfb-run Xvfb xauth timeout; do
  command -v "$prerequisite" >/dev/null || { echo "Missing graphical prerequisite: $prerequisite. See README.md; no packages installed automatically." >&2; exit 2; }
done
export LIBGL_ALWAYS_SOFTWARE=1
export COMMON_WATCH_TEST_AUDIO=dummy COMMON_WATCH_STEAM_DISABLED=1
# Automated checks use disposable preferences instead of changing the player's volume.
settings_directory=$(mktemp -d)
trap 'rm -rf "$settings_directory"' EXIT
run_client() {
  # The settlement probe includes a 2560-wide window and native pointer warps.
  timeout --kill-after=5s "${client_timeout:-90}s" xvfb-run -a -s '-screen 0 2560x1440x24' bash tools/client.sh --settings-path "$settings_directory/audio-settings.json" "$@"
}
case "$operation" in
  capture-environment)
    client_timeout=240
    output="$(pwd)/artifacts/captures/godot/environment"
    environment_log="$(pwd)/artifacts/test-logs/environment.log"
    mkdir -p "$(dirname "$environment_log")"
    : > "$environment_log"
    run_client --battle swordsman --world-art sprites --environment-captures "$output" "$@" 2>&1 | tee -a "$environment_log"
    run_client --battle swordsman --battle-tick 74 --world-art sprites --environment-effects full --environment-preset rain --environment-time 8 --assets-root "$settings_directory/missing-sprites" --capture "$output/missing-art.png" 2>&1 | tee -a "$environment_log"
    run_client --battle swordsman --battle-tick 74 --world-art debug --environment-effects full --environment-preset rain --environment-time 8 --capture "$output/debug.png" 2>&1 | tee -a "$environment_log"
    if rg -n 'SHADER ERROR:|ERROR:|Unhandled exception' "$environment_log"; then exit 1; fi
    rg -q 'GODOT_ENVIRONMENT_PASS' "$environment_log"
    ;;
  capture-arena)
    client_timeout=240
    run_client --battle swordsman --world-art sprites --arena-captures "$(pwd)/artifacts/captures/godot/modular-arena" "$@"
    ;;
  capture-worldtree)
    client_timeout=120
    run_client --world-art debug --hud-art illustrated --worldtree-test --gameplay-captures "$(pwd)/artifacts/captures/godot/worldtree" "$@"
    ;;
  capture-settlement)
    run_client --checkpoint initial --world-art sprites --hud-art plain --view angled --settlement-captures "$(pwd)/artifacts/captures/godot/settlement-map" "$@"
    ;;
  test)
    # --smoke-test runs actual native controls, commands, playback, resize and draft checks.
    for mode in debug sprites; do
      run_client --render-mode "$mode" --smoke-test --steam "$@"
    done
    ;;
  test-gameplay-network)
    timeout --kill-after=5s 180s xvfb-run -a -s '-screen 0 1920x1080x24' python3 tools/gameplay-network.py
    ;;
  test-gameplay|capture-gameplay)
    client_timeout=180
    output="$(pwd)/artifacts/captures/gameplay"
    capture_options=()
    presentation_options=(--world-art debug --hud-art plain)
    if [[ ${COMMON_WATCH_GAMEPLAY_ART:-} == illustrated ]]; then
      output="$output/forest"
      presentation_options=(--world-art sprites --hud-art illustrated --view angled)
    fi
    if [[ "$operation" == capture-gameplay ]]; then capture_options+=(--gameplay-captures "$output"); fi
    run_client "${presentation_options[@]}" --gameplay-test "${capture_options[@]}" "$@"
    if [[ "$operation" == capture-gameplay ]]; then
      run_client "${presentation_options[@]}" --gameplay-test --gameplay-captures "$output/1920" --width 1920 --height 1080 "$@"
    fi
    ;;
  capture)
    output="$(pwd)/artifacts/captures/godot"
    mkdir -p "$output"
    for checkpoint in settlement building-selected combat draft; do
      rm -f "$output/$checkpoint.png"
      run_client --render-mode debug --checkpoint "$checkpoint" --capture "$output/$checkpoint.png" "$@"
      test -s "$output/$checkpoint.png" || { echo "Capture not produced: $output/$checkpoint.png" >&2; exit 1; }
    done
    echo "Actual rendered captures: $output"
    ;;
  profile-battle)
    client_timeout=180
    output="$(pwd)/artifacts/profiles/godot-animation.json"
    run_client --battle swordsman --world-art sprites --battle-profile "$output" "$@"
    test -s "$output"
    ;;
  capture-battle)
    output="$(pwd)/artifacts/captures/godot/battle"
    for mode in debug sprites; do
      rm -rf "$output/$mode"
      run_client --battle swordsman --world-art "$mode" --battle-capture "$output/$mode" "$@"
      python3 tools/verify-battle-captures.py "$output/$mode"
    done
    run_client --battle swordsman --battle-tick 74 --world-art sprites --assets-root "$settings_directory/missing-sprites" --capture "$output/missing-sprites.png" "$@"
    run_client --battle swordsman --battle-tick 74 --world-art sprites --view angled --debug-overlay on --battle-zoom 1.6 --battle-pan-x 80 --battle-pan-y 20 --combat-shader off --capture "$output/camera-angled-fallback.png" "$@"
    test -s "$output/missing-sprites.png"
    test -s "$output/camera-angled-fallback.png"
    ;;
  capture-forest)
    output="$(pwd)/artifacts/detailed-forest-v2"
    mkdir -p "$output/comparison" "$output/after"
    for radius in 65 81.25 91; do
      run_client --checkpoint sprite-settlement --world-art sprites --hud-art illustrated --view angled --settlement-radius "$radius" --settlement-zoom 1 --capture "$output/comparison/radius-$radius.png"
    done
    for width in 1280 1920; do
      height=$((width * 9 / 16))
      close_zoom=1.3
      if [[ "$width" == 1920 ]]; then close_zoom=2.2; fi
      for view in angled topdown; do
        run_client --checkpoint sprite-settlement --world-art sprites --hud-art illustrated --view "$view" --width "$width" --height "$height" --capture "$output/after/settlement-$view-$width.png"
        run_client --checkpoint sprite-settlement --world-art sprites --hud-art illustrated --view "$view" --width "$width" --height "$height" --settlement-zoom "$close_zoom" --debug-overlay on --capture "$output/after/settlement-$view-$width-close-anchors.png"
      done
    done
    for page in 1 2 3; do
      run_client --asset-showroom --showroom-page "$page" --world-art sprites --hud-art illustrated --view angled --debug-overlay on --width 1280 --height 1080 --capture "$output/after/showroom-$page.png"
    done
    for scenario in ranged structures; do
      run_client --battle "$scenario" --battle-tick 74 --world-art sprites --view angled --capture "$output/after/combat-$scenario.png"
    done
    ;;
  capture-art)
    output="$(pwd)/artifacts/captures/godot/art"
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
    capture_art shrine-angled sprite-settlement --world-art sprites --hud-art plain --view angled
    capture_art shrine-anchors sprite-settlement --world-art sprites --hud-art plain --view angled --debug-overlay on
    capture_art shrine-topdown sprite-settlement --world-art sprites --hud-art plain --view topdown
    capture_art combat-sprites combat --world-art sprites --hud-art illustrated --view angled
    capture_art hud-plain draft --world-art sprites --hud-art plain --view angled
    capture_art hud-illustrated draft --world-art sprites --hud-art illustrated --view angled
    capture_art nine-slice-small building-selected --world-art sprites --hud-art illustrated --view angled --width 1280 --height 720
    capture_art nine-slice-large draft --world-art sprites --hud-art illustrated --view angled --width 1920 --height 1080
    # Showroom constructs no match, even though the ordinary capture helper accepts a checkpoint.
    run_client --asset-showroom --world-art sprites --hud-art illustrated --debug-overlay on --width 1280 --height 1080 --capture "$output/asset-showroom.png"
    test -s "$output/asset-showroom.png"
    for page in 2 3; do
      run_client --asset-showroom --showroom-page "$page" --world-art sprites --hud-art illustrated --view angled --debug-overlay on --width 1280 --height 1080 --capture "$output/asset-showroom-$page.png"
      test -s "$output/asset-showroom-$page.png"
    done
    echo "Actual art captures: $output"
    ;;
  *) echo "Unknown operation: $operation" >&2; exit 2 ;;
esac
