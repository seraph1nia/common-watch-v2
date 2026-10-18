#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
root=$(pwd)
platform=${1:?Usage: tools/build-client.sh windows\|linux}
case "$platform" in
  windows) rid=win-x64; preset=Windows; godot_executable=CommonWatch.exe ;;
  linux) rid=linux-x64; preset=Linux; godot_executable=CommonWatch.x86_64 ;;
  *) echo "Unknown platform: $platform (expected windows or linux)" >&2; exit 2 ;;
esac
command -v dotnet >/dev/null || { echo 'Missing .NET SDK. Install pinned mise tools; see README.md.' >&2; exit 2; }
command -v godot >/dev/null || { echo 'Missing Godot .NET. Install pinned mise tools; see README.md.' >&2; exit 2; }
# This directory is owned entirely by this build task. Do not retain stale binaries/assets.
output="$root/artifacts/builds/godot/$platform"
rm -rf -- "$output"
mkdir -p "$output"
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
cp LICENSE "$output/LICENSE"
mkdir -p "$output/licenses"
cp src/Game.Godot/addons/godotsteam/license.md "$output/licenses/GodotSteam.LICENSE"
cp src/Game.Godot/addons/godotsteam/manifest.json "$output/licenses/GodotSteam.manifest.json"
mkdir -p "$output/docs"
cp docs/PROVENANCE.md "$output/docs/PROVENANCE.md"
echo "Built Godot ($rid): $output"
