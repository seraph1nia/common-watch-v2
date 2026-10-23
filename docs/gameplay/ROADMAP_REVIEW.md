# Roadmap #13 completion review

The [roadmap](https://github.com/seraph1nia/common-watch-v2/issues/13) covers all
ten child issues, #3–#12, from reviewed baseline
`3b0c76dc4efb29fc67c63d8ca4689e9aea2e6ae1`. The implementation is on
[PR #14](https://github.com/seraph1nia/common-watch-v2/pull/14), which remains
unmerged. GitHub was audited on 2026-10-23: all ten children are closed with
reason **completed**, their 69 acceptance checkboxes are checked, and all ten
parent workstream entries are checked.

The user explicitly accepted local tests, legal replays and actual screenshots
as evidence instead of missing human playtest/listening observations. The
functional milestone below is complete under that acceptance. This does not
establish human comprehension, enjoyment, attachment, challenge or balance;
human/physical listening remains explicitly unexecuted.

## Child decisions and evidence

| Issue | Result and review |
| --- | --- |
| #3 | Next Ready transition, threat, funding and Food appear together. Three production stages are retained after recording empty-stage/waiting tradeoffs. [Preparation review](PREPARATION_REVIEW.md). |
| #4 | Squad/target/low-health cues, a nonmodal live inspector and factual clear/defeat results leave the fight and draft accessible. [Combat review](COMBAT_READABILITY.md). |
| #5 | Six native home targets expose occupancy/restrictions and retain squad/member context; Core supplies paid placement and participation. [Army review](ARMY_DEPLOYMENT_REVIEW.md). |
| #6 | Optional opening guidance and playstyle-led doctrine cards support all six legal three-wave paths on two seeds without changing income. [Opening review](OPENING_REVIEW.md). |
| #7 | Research/reward previews name current targets, exact effects and pivots. Twenty-six legal trajectories contain 286 retained drafts; sparse/capped experiments inform the documented decision to retain the draft algorithm. [Build review](BUILD_IDENTITY_REVIEW.md). |
| #8 | Historical service is distinguished from fixed equipment. Five legal comparisons show exact recovery, recruit and trainer quotes before guarded irreversible retirement. No service bonus/re-equipment rule is invented. [Veteran review](VETERAN_REVIEW.md). |
| #9 | Tactical purposes and crowned playback-timed bosses expose the existing schedule. Twenty trajectories / 400 legal battles support retaining the curve with duration and single-boss tradeoffs documented. [Campaign review](CAMPAIGN_REVIEW.md). |
| #10 | Team readiness, Keep/funding warnings, actual playback-sector contributions and fallen-city observation cover 2–6 players. Twenty uneven runs / 220 battles document early losses and recoverable late elimination. The failure curve is retained. [Cooperation review](COOPERATION_REVIEW.md). |
| #11 | A distinct approved-sheet enemy crop, explicit Watchpost mapping, native building-level annotations and consistent supporting materials preserve the painted forest identity. [Forest review](FOREST_PRESENTATION_REVIEW.md). |
| #12 | Six bounded native UI/SFX voices, 16 in-memory technical earcons and input-free labels consume exact receipts/presented events. Retries and baselines remain silent; music/loop/preferences persist. [Feedback review](FEEDBACK_REVIEW.md). |

## First-three-wave milestone

| Roadmap requirement | Authoritative implementation and inspected evidence |
| --- | --- |
| Understand the next threat and Ready transition | PreparationPanel/OpeningGuidance project the actual forecast, income, participation and Food; native `preparation-1/2/3` frames show “Ready · battle” and final-income/Food wording at ordinary framing. OpeningGuidanceTests runs every doctrine on both seeds. |
| Recruit, fund and position an army clearly | Native recruitment and the six-home board expose quoted costs, full/locked/tower occupancy and sit-outs. ArmyDeploymentViewTests compares an accepted relocation with actual assignments and entry cells. `six-home-relocated` and shortage frames were inspected. |
| Follow a fight while inspecting units | LiveCombatInspector leaves the arena visible, hides entity/tick detail behind Advanced, and uses presented health/action/target data. `combat-live-inspection` plus dense 30-actor / 11-cue cooperative captures and native selection/focus checks passed. |
| Read an outcome and identify a next adjustment | BattleOutcomeJournal/ResultStrip retain actual participation, losses, Keep damage and rewards beside the draft; missing baselines are labeled unavailable. Optional guidance links the factual result to preparation choices. `battle-result-draft`, terminal frames, opening contracts and seven paid cooperative waves verify that flow. This is a functional review, not a claim about first-time player understanding. |
| Recognize accepted purchases and milestones | Exact receipt identity triggers separate success/rejection responses. Presented battle start/boss/clear/defeat/victory have distinct short cues. Four native size/mode cases, actual muted-Music mixer PCM and inspected construction/production/outcome frames cover the local substitute for human recognition/listening. |

Ordinary frames are under `artifacts/captures/gameplay/forest/` and `1920/`;
debug/plain counterparts remain under `artifacts/captures/gameplay/`. Dedicated
opening, bosses, cooperation, forest and feedback directories retain the focused
legal-state captures. Review documents above record their reproduction commands
and numeric tradeoffs. No capture fabricates authoritative state.

Final screenshot review caught stale preparation labels over the draft Close
control. Outcome feedback now replaces earlier labels, and native modal geometry
reserves draft controls/content and details Close/tab navigation. Outcome probes
require one remaining label; native bounds checks now include modal areas. This
follow-up preserves simulation/audio triggering and is verified through the
focused feedback cases and ordinary UI/cooperative captures.

## Verification scope

| Evidence | Result |
| --- | --- |
| Locked solution build, Core, Shared | Zero warnings/errors; 46 Core / 145 Shared cases pass. |
| Full `mise run test` at the feature checkpoint | 274 .NET/process cases (46 Core, 145 Shared, 77 Godot contracts, six actual process integrations), 19 Pillow tests and native animation phases pass with no skips. |
| Final modal-label follow-up | Locked build, Core/Shared, four native feedback size/mode cases, actual PCM review, ordinary 720p/1080p UI, seven-wave cooperative worker, three native session facts and 14 refreshed art views verify the affected presentation. |
| Networking and lifecycle | Real 1–6-client admission/readiness/combat/draft; private reconnect/fault/process restart; exact retry counters; session replacement/music identity, modal/native focus and allied read-only guards pass. |
| Rendering | Fourteen art views, 19 animation phases and 22 ordered frames per renderer, missing-art/shader fallback; focused approved/corrupt asset and five-size clearing-picking gates are recorded in the child reviews. |
| Native audio | Sixteen distinct actual cached PCM exports; real Dummy-driver Master recording with Music muted peaks at −20.49 dBFS without clipping in that sequential recording. HTML/WAV audition remains local; human/physical listening is unexecuted. |

Latest local logs use `artifacts/test-logs/roadmap-12-*` and `roadmap-13-*`.
The [feature-checkpoint CI](https://github.com/seraph1nia/common-watch-v2/actions/runs/39879550536)
also passed native integration and Linux/Windows x64 cross-builds at `20343f7`.
The full-suite checkpoint precedes the final modal-label refinement; the final
focused checks above verify that changed path. The active PR workflow status is
reported on GitHub rather than treated as a local result. Release cross-builds
or CI do not qualify another operating system's native runtime.

## Preserved boundaries and qualification limits

A comparison to the reviewed baseline confirms Core, tracked source art, SDK/
Godot/Python pins, every NuGet lock and the vendored Steam dependency are intact.
All 46 previous asset records and installed PNGs are unchanged; the sole added
installed PNG is the approved enemy Berserker crop, catalog entry 47. No game
artwork is created, painted or downloaded. Procedural sound is technical runtime
code, with no external pack or additional shipped sound file dependency.

The original music WAV retains SHA-256
`3783f4deed1ecd7933a17538a1d880ffb04319f6e11f34dc66f2df7a05ab70b4`,
its import and authored loop. Settings/private credential formats remain intact.
SessionCoordinator alone creates/disposes sessions and retains the root
`/root/CommonWatch/Session` RPC path. UI reads detached snapshots and submits
commands; combat presentation samples Core playback. Native focus, authenticated
identity, allied read-only behavior, ordinary sprites/illustrated/angled defaults
and geometric/plain fallbacks remain covered.

No new balance, equipment, progression, economy, spawning, Food, refund, RNG or
compatibility rules ship. Deliberately retained mechanics and their observed
tradeoffs are decisions in the relevant child reviews, not unfinished hidden
rule changes. Physical audio/display/controller hardware, live Steam accounts,
Windows/macOS native runtime and human gameplay/listening qualification remain
unexecuted. The known intermittent native .NET/JIT crash risk remains documented.
