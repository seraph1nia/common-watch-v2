# Combat framing and selective cues (#18)

Presentation baseline: `583ec3a` (#17). Core/Shared rules, action scheduling,
occupancy, combat playback, equipment, reward boundaries and supplied art/audio
remain unchanged. This issue changes the viewport and annotations, not encounters.

## Camera contract

`World/BattleFraming` takes detached authority and the existing hex projection.
At a documented framing boundary it bounds the focused city's anticipated
deployed soldiers/enemies and defense footprints, retaining actor/crown headroom.
If a queued actor lacks a position, the sector's legal cells provide the fallback.
Decorative arena caps do not determine this engagement frame.

The HUD supplies actual visible Menu, phase controls, Team and inspector rectangles.
The camera chooses free native canvas space by achievable content scale, avoiding
a wide but shallow rectangle that would shrink the fight. A small inset protects
figure edges. Settlement framing and picking retain their separate fixed policy.
The wheel/pan magnification ceiling is consistently 3.3, so zooming in cannot
shrink a new larger default pose.

| Cause | Camera behavior |
| --- | --- |
| Actual battle entry / new match or battle identity | Establish the engagement default after the phase-specific HUD settles. |
| Explicit inspected-city change, viewport size, projection or art fallback | Recompute that default from the relevant current sector and available native space. |
| Ordinary snapshots, damage, moving actors or unrelated HUD updates | Retain the current pose. There is no automatic actor chase or periodic correction. |
| Native pan / wheel zoom | Enter Manual mode; snapshot refresh does not override it. |
| Field overview / F | Enter Overview mode using the complete rendered field; retain the local default. |
| Native camera reset / R | Restore the default engagement pose. |
| Open live inspection | Place the inspector away from actual subject/target bodies; retain the camera pose. |

The live inspector is 280×300 pixels at 720p and 340×300 at 1080p, replacing
310×460 / 380×460. It chooses a side with room around the subject and its current
target. It retains that placement until selection/size changes or meaningful
collision; a 28-pixel margin and 120 presented-tick interval prevent rapid flips.
This local placement never drives authority or combat time. Basic inspection
keeps role, HP, action, active effects and target; profile numbers, identity and
timing remain under Advanced details. Death is factual, then selection closes
when playback no longer contains the body. Native close/focus behavior remains.

## Cue removal and retention

| Previous default | Current default / retained inspection |
| --- | --- |
| F/R/M/P#/E→# legend | Removed. Local circle bases, allied split circles and enemy triangular bases provide non-color distinctions; short You / Ally N / Enemy labels identify ownership. The optional inspector tooltip explains shapes. |
| Full role, destination and survivor fraction over every squad | Short ownership/member count. Selected squads expose the plain-language role; complete name/target/effects stay in inspection. |
| Healthy squad HP bars and leader lines | Bars appear for damage, selection, low health or bosses. Routine leader lines leave. Low health gets stronger color and a plain Low label. |
| Labels displacing only other labels/bosses | Priority labels avoid all actor bodies and native HUD rectangles. Congested labels may yield; ownership bases remain. |
| Every ordinary hit's aggregate number | Numbers are limited to selected squads, bosses and structures, grouped in 24-tick buckets. Existing sampled shot, spell, hit flash and impact motion remain. |
| Healthy Watchpost/tower labels and full HP bars | Routine furniture yields; Keep identification remains. Damage, selection and actual hits expose structure health. |
| Strong field grid in ordinary combat | Lower grid contrast with existing scenery intact. Explicit debug overlay retains strong diagnostic geometry. Placement preview is separate. |
| Boss health strip across the lower defense | Top status strip leaves the Keep visible. Existing crown, windup and defeat cues retain playback timing. |
| Selected target lines after loss/death | Only living selected actors and actual living targets draw those lines. Existing generation reset clears visual history. |

#17 already owns obsolete preparation-feedback cleanup, rejected/disconnected
notices and the outcome finishing boundary. This issue reuses that ownership.

## Native evidence and reproduction

```sh
COMMON_WATCH_COMBAT_REVIEW_LABEL=before mise run capture-combat-sequences
COMMON_WATCH_COMBAT_REVIEW_LABEL=before mise run capture-combat-cooperation
# Repeat against the changed presentation with label=after.
python3 tools/compare-combat-sequences.py
mise run capture-arena
mise run test-animation
mise run profile-animation
```

The before label was captured with #17's presentation and the observation harness
before presentation edits. The four solo fixtures are legal early, paid mixed
wave eight and W10/W20 campaigns. Each runs at actual displayed 1× (factor 0.5),
using normal application advancement and native selection. Reports separate
simulation ticks/seconds, wall time, actual transformed actor/text bounds,
occupied bounds, overlaps, inspector geometry and accepted final outcomes.
The comparison checks eight paired fixtures, their outcomes and duration, larger
figures, reduced initial overlaps and unoccluded selected subject/target bodies.

Six real ENet clients also earn a mixed wave-six encounter through legal commands,
then disable development fast stepping and run at ordinary 1×. Native F/R exercise
local framing and the complete cooperative field. A test-only match identity,
accepted only with an explicit control directory, stabilizes personal offers;
normal hosts retain fresh identities. Parallel network command ordering still
changes unit IDs/tie breaks, so these runs provide network/geometry coverage.
Exact outcome comparisons use the deterministic solo fixtures.

Artifacts live under `artifacts/captures/combat-review/{before,after,after-debug}`.
PNG stills and measured rectangles use native 1280×720 / 1920×1080 pixels.
Optional videos from preinstalled ffmpeg are **half-scale desktop proxies** with
unchanged time; they are not native-resolution pixel measurements. Recording
tools install nothing. Reports identify software rendering and observation
overhead. The initial PNG-heavy probe visibly stalled the main thread; those
diagnostics remain in `before-png-heavy` and are excluded from normal timing.
Three review stills plus lightweight geometry sampling replaced that probe.

Initial matched-frame measurements (median visible actor rectangle height):

| Fixture | 720p before → after | 1080p before → after | Initial text/actor bounds overlaps, before → after |
| --- | --- | --- | --- |
| Early | 58.1 → 66.8 px | 87.1 → 111.7 px | 3 → 0 |
| Dense mixed | 58.3 → 66.7 px | 87.5 → 111.5 px | 8 → 0 |
| W10 | 58.3 → 63.7 px | 87.5 → 106.5 px | 4 → 0 |
| W20 | 57.9 → 63.2 px | 86.9 → 105.7 px | 3 → 0 |

These are measured bounds, not opaque-pixel masks or human judgments. All eight
paired outcomes and authoritative durations match (301/302/271/272 ticks).
The comparison also passes 470 recorded selected-body/target checks without
inspector intersection. Inspector area falls by about 41% at both sizes. Native
stills show the complete basic summary and Advanced control inside the compact
panel. The boss strip leaves the Keep visible.

Executed gates so far: zero-warning locked build, Core 46 / Shared 145 /
session-model 102 tests, three native session process tests, all four full
gameplay profiles and seven cooperative waves, private reconnect, 24 native
sector/projection/size camera cases, animation captures including shader/art
fallback, and nine repeated battles targeting 30/60/120 FPS at all speeds.
All repeated-battle effects cleared. Software-rendered measured FPS ranged
29.6–98.9; 120 FPS is a requested target, not an achieved hardware qualification.
Default and debug/plain normal-speed recordings at both sizes also passed.
Changed-presentation six-player normal-speed captures passed at both sizes;
native overview remained selected through 6 / 14 later observations. Recorder
shutdown logs are clean. The six-player initial local median was 59.7 px at
720p and 106.3 px at 1080p (earlier coverage: 58.4 / 87.2 px). Those network
scenarios have different command interleavings and are descriptive coverage,
not deterministic outcome pairs.

The original maximum-group 720p gain was modest with the retained Team panel.
The later [#20 integration](20_COOPERATIVE_CONTEXT.md) resolves that Gate A
obligation: compact monitoring and grouped controls increase the six-player
720p median from 59.8 to 73.5 px (+22.9%); 1080p grows 4.2%. Initial collisions
remain zero. These are unpaired network geometry observations; exact unchanged
outcomes remain established by the eight solo fixtures.

No human comprehension, physical
GPU/audio/controller, native Windows/macOS or live Steam result is inferred from
these automated observations. In particular, geometry does not answer who a
human thinks is winning or whether a squad's contribution feels consequential;
the later controlled campaign work remains necessary.
