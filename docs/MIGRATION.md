# Godot-only migration record

Starting commit: `4ffabb2e29cfb00b4aa44865abc0acd9835bb4ed`.

## Removal baseline

The discarded MonoGame application, its session tests, Gum, LiteNetLib and Facepunch distribution, runtime locks and CI were removed first. Existing Godot behavior was retained without importing the other client's implementation. Scripts and tests now launch Godot directly; canonical mise tasks use generic names. These dependency names appear here solely to record the removal history.

The initial working tree contained a three-line Gum resize fix in the removed `WatchGame.cs`. Its exact binary diff was preserved before deletion at `/home/bart/.local/state/common-watch-v2-migration/preexisting.patch`; it was not silently discarded. No other initial changes existed.

Build/test results are recorded in VERIFICATION.md. This document is included in the dedicated removal commit, which leaves the original Godot implementation untouched. Native architecture work starts only after that verified commit.
