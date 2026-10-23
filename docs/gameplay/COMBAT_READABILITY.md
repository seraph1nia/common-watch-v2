# Combat readability and outcomes — issue #4

Ordinary combat groups health/count information by squad. F, R and M identify
frontline, ranged and magic roles; P1–P6 identify allies and E→P identifies the
enemy destination. The lower-left legend explains those cues. Orange warning
marks flag squads at or below 30% of their observed total maximum health. The
observed denominator persists through casualties within a battle and resets on a
new playback generation or battle; reconnect does not invent an earlier roster.
Ownership rings, stronger ground shadows and subdued terrain separate actors
from the existing forest. Badges stay inside the viewport below menu controls, including
debug/topdown framing. Exact simulation IDs and individual bars remain in
debug presentation. Nearby damage labels aggregate actual positive damage over
short presentation buckets; they never cause damage or change timing.

Clicking a live actor opens a narrow, read-only panel beside the field. Health,
role, active effects, target and practical capabilities use the same playback
frame as the figures. Gold rings identify the selected squad; orange rings and
a line identify its current objective or committed target. Moving objectives
resolve squad IDs; committed attacks resolve the primary member ID. Structures
resolve their owner, kind and cell. Advanced details preserve raw identity and
timing behind a native button. Escape closes inspection before opening the menu.

The existing draft contains a three-line factual result and optional details.
Reward choice remains immediately available; no new dismissal is required.
Victory/defeat and a dismissed draft expose the same recap beside the settlement;
City details retains the last result. Participation/Food use the matching upkeep
receipt, casualties use the immutable end-of-battle roster, and rewards use only
a receipt from that battle. Keep damage requires an observed preparation baseline
and a result observed before that city chooses a potentially healing reward.
Later recruitment/retirement cannot rewrite a frozen result. Rejoining without
history explicitly reports unavailable casualties or Keep damage. These are
accounting statements, with no speculative causal advice.

## Local verification

The accepted evidence is legal default-rule replays, native Controls and actual
Linux Xvfb/Mesa screenshots. It does not establish human comprehension or balance.
`BattleOutcomeTests` covers actual clear, food shortage, defeat, wave-20 victory,
late observation, a cooperative reward-choice race, frozen results after retirement, effect boundaries and the
moving-objective/member-ID distinction. The native gameplay probe clicks a live
actor, checks nonmodal bounds and unchanged revision, toggles advanced detail by
pointer/controller, advances the next action, closes with Escape and chooses a
reward directly beside the recap. It asserts all three collapsed reward choices
fit at 720p and 1080p.

`mise run capture-gameplay-ui` captures ordinary angled/illustrated and
debug/plain views at both sizes. Relevant actual PNGs under
`artifacts/captures/gameplay/forest/` (illustrated) or
`artifacts/captures/gameplay/` (debug/plain), and their `1920/` subdirectories:
`combat-live-inspection`, `combat-advanced-inspection`, `combat-next-action`,
`battle-result-draft`, `battle-result-expanded`, `victory` and `defeat`.
The two-process worker pauses a legal wave-six cooperative encounter for
`network-{forest,plain}/dense-combat-{1280,1920}.png`, then resumes and finishes
seven paid waves. Both presentations passed with 30 live actors and 11 squad
cues, checking local/remote ownership and read-only inspection.

Reproduce with `mise run build`, `test-core`, `test-shared`, `test-session`,
`capture-art`, `test-animation`, `capture-battle` and `capture-gameplay-ui`.
Animation receipts verify playback impacts, death poses and lifecycle cleanup;
Core, its rules, playback policy, assets, audio and dependency pins are unchanged.
Build passed with zero warnings/errors; 46 Core tests, 94 Shared tests, three
native session facts and eight animation contracts passed. Fourteen art/fallback
captures passed. The ordered animation review verified 19 phases and 22 frames
in each renderer. Logs use `artifacts/test-logs/roadmap-04-*`.
Live Steam, native Windows/macOS, physical GPU/input and audio hardware remain
separate unexecuted qualifications. The known intermittent native .NET/JIT crash
risk remains unresolved.
