# The Common Watch — Godot 2D

Work in this repository. Use `mise run build`, `mise run test-core`, `mise run test-shared`, and the affected graphical checks. See README.md for setup and commands.

Platform release tasks are `build-linux` and `build-windows`. Their self-contained x64 outputs belong in gitignored `artifacts/builds/godot/<platform>/`. Include music/optional sprites and preserve executable-relative asset lookup. Runtime-specific NuGet locks are in `tools/nuget-locks`; do not regenerate the pinned upstream Core lockfile for publishing.

## Boundaries

- `src/Game.Core` is an unmodified pinned upstream import; provenance is in `docs/PROVENANCE.md`. It owns all gameplay rules and Arch ECS. Do not edit it for presentation needs.
- `src/Game.Shared` owns the minimal solo authority adapter, deterministic replay, projection and visual IDs. No graphical dependencies.
- `src/Game.Godot` owns Godot 2D drawing and native Controls.
- The client submits authoritative commands and read detached snapshots. Never write simulation state or duplicate rules.

## Workflow

Compile and run focused tests after coherent changes. Run the relevant actual graphical client when presentation changes; compilation alone is insufficient verification. Integration/capture tasks use an owned virtual display on Linux. Keep dependency versions and NuGet lockfiles pinned. Do not install system packages from tasks.

Default rendering is `--render-mode debug`: shapes and text, ignoring all sprite files. `--render-mode sprites` enables optional files with geometric fallbacks. Never create, generate or download game artwork. Technical font/shape rendering resources and actual application screenshots are allowed. Empty asset directories are intentional.

Background music is the unchanged pinned upstream WAV in `assets/audio`; preserve its provenance and authored loop markers. Interactive launches must use normal audio output; silent drivers belong only in automated graphical checks. Settings are local presentation preferences, never simulation commands or state. Automated checks use temporary `--settings-path` files to preserve player preferences.

The shared scenario replays legal commands under normal rules. Use its checkpoints for tests and captures rather than fabricating authoritative state. Document unexecuted checks accurately. Preserve upstream GPL-3.0-only notices and license.
