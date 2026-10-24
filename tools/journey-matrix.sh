#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
journey_output=${COMMON_WATCH_JOURNEY_NETWORK_OUTPUT:-"$(pwd)/artifacts/captures/journey"}
for presentation in default debug; do
  mode=()
  if [[ $presentation == debug ]]; then mode=(--debug); fi
  for width in 1280 1920; do
    bash tools/graphical.sh test-journey-network --players 2 --width "$width" "${mode[@]}" --output "$journey_output/network-2p-$presentation-$width" "$@"
  done
done
for players in 4 6; do
  for width in 1280 1920; do
    bash tools/graphical.sh test-journey-network --players "$players" --width "$width" --output "$journey_output/network-${players}p-default-$width" "$@"
  done
done
