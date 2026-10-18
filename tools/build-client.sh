#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
root=$(pwd)
client=${1:?Usage: tools/build-client.sh godot\|monogame windows\|linux}
platform=${2:?Missing target platform: windows or linux}
case "$client" in godot|monogame) ;; *) echo "Unknown client: $client" >&2; exit 2 ;; esac
case "$platform" in
  windows) rid=win-x64; preset=Windows; godot_executable=CommonWatch.exe ;;
  linux) rid=linux-x64; preset=Linux; godot_executable=CommonWatch.x86_64 ;;
  *) echo "Unknown platform: $platform (expected windows or linux)" >&2; exit 2 ;;
esac
command -v dotnet >/dev/null || { echo 'Missing .NET SDK. Install pinned mise tools; see README.md.' >&2; exit 2; }
if [[ "$client" == godot ]]; then
  command -v godot >/dev/null || { echo 'Missing Godot .NET. Install pinned mise tools; see README.md.' >&2; exit 2; }
fi
# This directory is owned entirely by this build task. Do not retain stale binaries/assets.
output="$root/artifacts/builds/$client/$platform"
rm -rf -- "$output"
mkdir -p "$output"
if [[ "$client" == godot ]]; then
  echo 'Godot export requires matching 4.7.2.stable.mono .NET export templates installed in Godot.'
  dotnet restore src/Game.Godot/Game.Godot.csproj --locked-mode -p:RuntimeIdentifier="$rid" -p:Configuration=ExportRelease -p:SelfContained=true
  # Godot's exporter requires a legacy .sln beside its managed tooling. The
  # repository's authoritative solution stays CommonWatch.slnx; this is generated.
  dotnet new sln --format sln -n Game.Godot -o src/Game.Godot/.godot --force
  dotnet sln src/Game.Godot/.godot/Game.Godot.sln add src/Game.Godot/Game.Godot.csproj --in-root
  godot --headless --path "$root/src/Game.Godot" --import
  godot --headless --path "$root/src/Game.Godot" --export-release "$preset" "$output/$godot_executable" 2>&1 | tee "$output/build.log"
  if grep -q '^ERROR:' "$output/build.log"; then
    echo "Godot reported export errors; see $output/build.log" >&2; exit 1
  fi
  test -s "$output/$godot_executable" || { echo 'Godot did not produce an executable.' >&2; exit 1; }
  find "$output" -type f -name Game.Godot.dll -print -quit | grep -q . || { echo 'Godot did not export the C# game assemblies.' >&2; exit 1; }
  cp -R assets "$output/assets"
else
  dotnet restore src/Game.MonoGame/Game.MonoGame.csproj --locked-mode -p:RuntimeIdentifier="$rid" -p:SelfContained=true
  dotnet publish src/Game.MonoGame/Game.MonoGame.csproj --no-restore -c Release -r "$rid" --self-contained true -p:PublishReadyToRun=false -o "$output"
fi
cp LICENSE "$output/LICENSE"
mkdir -p "$output/docs"
cp docs/PROVENANCE.md "$output/docs/PROVENANCE.md"
echo "Built $client ($rid): $output"
