# Action and milestone feedback review

Roadmap [#13](https://github.com/seraph1nia/common-watch-v2/issues/13),
issue [#12](https://github.com/seraph1nia/common-watch-v2/issues/12).
The user accepted local tests, legal replays and actual application captures as
evidence. Human listening, comprehension and enjoyment are not inferred from
these results.

## Trigger and presentation contract

| Source | Feedback |
| --- | --- |
| Accepted local construction / recruitment / upgrade / technology receipt | Separate short UI earcons and a factual gold label at the visible building, recruit control or technology control |
| Rejected local command receipt | Descending rejection earcon and coral label; complete reason remains in recent feedback |
| Trade / army assignment / retirement / Ready / pause / reward receipt | Short accepted-action label; previews and mere selection do not claim a purchase |
| Actual production-count increase | Three-note production cue, resource-ledger outline and collection label; multiple missed payments coalesce |
| Presented WaveStarted | Battle-start punctuation |
| Presented melee impact / ranged shot / spell impact | Separate restrained percussive, swish and bell-like earcons; existing deterministic visual combat effects remain the primary hit feedback |
| Presented Keep damage / structure destruction | Low impact and a brief factual structure label, anchored to the cell when inspected |
| First alive boss in presented playback | One boss-arrival cue per new wave; joining an existing boss fight does not replay arrival |
| Authoritative RewardDraft / Defeat / Victory after final presented combat | Distinct clear / defeat / victory cue and label; wave-clear text uses LastRewardedWave rather than the already incremented next wave |

Technology purchases acquire the technology immediately; labels say “Learned”.
Research-point production is represented by the production cue. Ready says
“Ready accepted”, because the final required Ready immediately advances the
phase and clears readiness. Wave-clear feedback does not promise a draft to a
fallen city. A stalled-battle defeat has its own accurate caption.

`FeedbackTimeline` only reads detached snapshots, exact command results and
presented Core combat events. `GodotSession.CommandAcknowledged` supplies the
local request/result identity, including remote acknowledgments. The sole
existing `BattlefieldView` playback drain fans events out to feedback after
motion observes them. There is no second drain, simulation mutation or rule
calculation. `FeedbackCoordinator` connects these owners; SessionCoordinator
continues to exclusively own sessions and the root RPC path is unchanged.

The command high-water mark rejects exact retries and duplicate refreshes.
Private reserved command sequence is used as the resumed baseline without
putting credentials into presentation snapshots, settings or logs. New sessions,
reconnects and playback generation changes clear old transient presentation and
baseline event/production/outcome state. Receipt closures retain epoch and
session-reference guards and disconnect on replacement/exit.

Combat cue onset follows presented events at 1× / 1.5× / 2×. Pausing freezes
retained SFX tails and playback-aged combat labels; UI responses remain usable.
Paused boundaries are consumed silently rather than queued into a resume burst.
Short sound envelopes keep their timbre at every speed; audio never advances
combat, damage, occupancy or the playback clock.

## Bounds and mix

There are six retained native players: two UI voices at −6 dB and four SFX voices
at −12 dB. A full pool drops equal/lower-priority arrivals; a higher-priority cue
may replace a quieter-priority event. Per-kind cooldowns are 100 ms melee,
140 ms ranged, 180 ms spell, 350 ms structure and one second boss arrival.
Ordinary cues also have rolling one-second limits of 10 UI / 12 SFX starts.
Milestones can bypass the ordinary window but retain their kind cooldown and
voice bound. The queues and per-kind history remain bounded. A native Master
hard limiter has a −1 dB ceiling for the combined mix.

At most four input-free labels remain for 1.5 seconds, coalesced by kind/city/
slot. Clear/defeat/victory replace earlier transaction labels. Native draft
controls/content and details Close/tab navigation reserve their actual bounds. They avoid the ledger, phase controls, team/preparation/guide/live inspector
and bottom navigation/boss band. Labels never intercept clicks; production also
briefly outlines the existing resource ledger. Ordinary high-frequency combat
hits add no text. Transaction fades use UI time; combat fades use playback time.
At a crowded anchor the label searches nearby clear space and can be omitted if
no safe location fits, leaving the persistent factual UI intact.

## Sound provenance and music preservation

The 16 short technical earcons are deterministic PCM synthesized in memory by
`Game.Shared/FeedbackTones.cs`, cached as native AudioStreamWav resources. They
use simple oscillators and a local fixed noise seed, never Core RNG. Durations
are 60–480 ms, with a 6 ms attack and 12 ms release. There is no downloaded sound
pack, external SFX license, new audio file dependency or generated game artwork.
These are GPL-3.0-only source code; exported audition WAVs are ignored development
artifacts, not assets added to the game.

The supplied Echoes of Valhalla WAV, import, compression and authored loop remain
unchanged. SHA-256 remains
`3783f4deed1ecd7933a17538a1d880ffb04319f6e11f34dc66f2df7a05ab70b4`;
loop samples remain 1,548,939–6,195,530. Music retains its existing application
player across menus/sessions, default 30% volume and saved JSON format. Zero
mutes Music only; UI/SFX remain independent. See [audio provenance](../../assets/audio/README.md).

## Reproduce and inspect

```sh
mise run capture-feedback
mise exec -- python3 tools/feedback-review.py
```

The first task builds and runs four actual native cases: sprites/illustrated/
angled and debug/plain/topdown at 1280×720 and 1920×1080. It verifies accepted and
rejected receipts, exact retry suppression, production, real pointer selection,
retained paused SFX position, supported speeds, resumed/replaced baselines (including an event arriving before the coordinator
update), boss arrival, wave clear, defeat and victory. All checkpoints earn their state through
legal commands. Captures are under `artifacts/captures/feedback/`.

The first native case also exports the actual cached native cue streams and
records the real Godot Master bus with Music muted. The second command validates
those WAVs and creates `artifacts/feedback/index.html` with audio audition controls
and technical amplitude plots, plus `review.json`. Sixteen distinct mono 44.1 kHz
PCM exports have quiet boundaries and no clipping. The actual stereo Master
sequence recorded 1,032,192 PCM bytes, peak 3,096 / −20.49 dBFS and RMS about
408.7. These measurements describe this sequential, muted-music recording;
they do not establish intelligibility of a dense mix on speakers.

The independent synthetic six-sector audio-budget check submits 12,000 attempts
across two seconds and verifies at most 24 ordinary SFX and 20 UI starts. Actual
1–6-client workers verify each instance retains six players and never exceeds
six active voices. Reconnect workers verify old receipt counters stay unchanged
through readmission, explicit retries and process restart; the seven-wave native
cooperative UI worker checks the real consumed retry request too.

## Evidence and limits

Focused native feedback, PCM review, real 1–6-client multiplayer, reconnect/fault
and seven-wave cooperative UI checks passed. The full suite passed 274 .NET/
process tests and 19 Pillow tests. Fourteen native art views and 19 battle phases
with 22 ordered frames per renderer passed, including fallback paths. Final
ordinary UI captures passed at both sizes. The feedback contracts add eight
Shared cases (145 total); Core remains 46. Full regression/capture results are
recorded in [verification](../VERIFICATION.md).

The automated audio driver is Dummy. Recording its actual nonzero mixer PCM
establishes functioning native routing, mute independence and measured headroom,
not physical output or human listening. The separately recorded human listening
review is **not executed**; the user explicitly accepted local test/screenshot
evidence as the substitute for roadmap closure. The audition page is available
for a later listening session. No claims about subjective pleasantness, human
recognition, physical hardware, live Steam or Windows/macOS runtime are made.
The known intermittent native .NET/JIT crash risk remains applicable.
