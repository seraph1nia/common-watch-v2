# Pinned GodotSteam GDExtension

Official [v4.22.1-gde release](https://codeberg.org/godotsteam/godotsteam/releases/tag/v4.22.1-gde),
Steamworks SDK 1.65, inspected 2026-09-30. The canonical
[repository](https://codeberg.org/godotsteam/godotsteam) is active and not archived.
The release API reports non-prerelease, while the
[publisher](https://store.godotengine.org/asset/godotsteam/godotsteam-gdextension/)
marks this version **unstable**. The owner accepted this pin for development;
release stability and remote multiplayer acceptance remain open gates.

`manifest.json` records the archive URL/hash, metadata and hashes of unmodified
files. Only the Linux and Windows x86_64 debug/release libraries, Valve runtimes and upstream
license/readme are retained. The descriptor removes unsupported platform entries;
its entry point, retained paths and dependencies match the upstream descriptor.
No editor plugin/dock is enabled or needed. Ordinary stock Godot .NET templates
bundle the native dependency; no custom engine, generated C# bindings, beta
adapter, runtime download or fallback transport is used.

To refresh intentionally, download the exact archive URL in the manifest,
verify its SHA-256 before extraction, and compare each retained file hash. Never
replace the pin from a moving branch. Assess compatibility, stability, licenses
and source/export checks again for a version update. Source is available at
the matching release tag, including `register_types.cpp`,
`godotsteam_multiplayer_peer.cpp` and `steam_packet_peer.cpp`.

Windows payloads were retained from the same checksum-verified archive on
2026-09-30. Their hashes are recorded in the manifest. PE inspection confirms
x86_64 binaries and imports limited to Windows system DLLs and the included
`steam_api64.dll`. Matching stock Windows .NET templates exist in the pinned
template archive. Native Windows source and exported loading, typed C# callback
compatibility, bundled-runtime inventory and headless offline solo passed on a
GitHub `windows-2025` runner on 2026-09-30:
[verification run](https://github.com/seraph1nia/odot-game/actions/runs/36754528804).
This does not establish Windows graphics, installer behavior, real Steam
invitations/relay/cold launch or publisher release stability.

`license.md` is the upstream MIT license for GodotSteam. `libsteam_api.so` and `steam_api64.dll` are
Valve's Steamworks SDK redistributables and are **not** relicensed as MIT. Their use
and distribution are subject to the
[Steamworks SDK agreement](https://partner.steamgames.com/doc/sdk) and the
developer's Steamworks terms. Exports retain these notices alongside the native
libraries. Steam API initialization is opt-in; local roles require no login.
