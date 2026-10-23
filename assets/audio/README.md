# Echoes of Valhalla

Artist: **LUShvalleySound** (embedded metadata). Track: **Echoes of Valhalla**, 82 BPM.

The project owner supplied the `LVS04_11_Echoes_of_Valhalla_bpm82_loop` download on
2026-09-30 as free royalty-free music. The supplied folder contained WAV, Ogg,
MP3, and M4A versions, with no license document or source URL. No more specific
license or redistribution terms are asserted here; this music is separate from
the KayKit CC0 assets. Retain the publisher's original terms here if supplied.

The unmodified `LVS04_11_Echoes_of_Valhalla_bpm82_loop.wav` is 39,118,090 bytes.
SHA-256: `3783f4deed1ecd7933a17538a1d880ffb04319f6e11f34dc66f2df7a05ab70b4`.
Its hash matches the owner's downloaded WAV.

The source is 44,100 Hz, stereo, 16-bit PCM, 221.746667 seconds. Its WAV `smpl`
chunk specifies a forward loop beginning at sample **1,548,939** and ending at
sample **6,195,530** (35.123333–140.488209 seconds). Godot detects those bounds
and imports with Quite OK Audio compression, without trimming, normalization,
downsampling, or custom looping code. Playback includes the intro once, then
repeats the authored loop, excluding the later outro. The unchanged Ogg includes
that outro and would require trimming to reproduce the same end boundary with
Godot's Ogg loop-offset API.

Only the WAV and its import settings are needed at build time. Runtime uses the
bundled `res://Assets/Music/` resource, including in offline client exports.

## Event feedback

UI and SFX buses now play short technical earcons synthesized in memory from
GPL-3.0-only `src/Game.Shared/FeedbackTones.cs`. No external sound pack or new
audio asset is required. Six retained players and per-kind/rolling limits bound
repetition; a −1 dB Master limiter protects combined headroom. Music volume zero
mutes only Music, leaving these cues audible. The WAV, import, authored loop and
music settings format above are unchanged.

[Feedback review](../../docs/gameplay/FEEDBACK_REVIEW.md) records trigger identity,
pause/reconnect behavior, native PCM measurements and the separate unexecuted
human listening review. `mise run capture-feedback` followed by
`mise exec -- python3 tools/feedback-review.py` creates an ignored native WAV
audition at `artifacts/feedback/index.html`; it is not a game asset dependency.
