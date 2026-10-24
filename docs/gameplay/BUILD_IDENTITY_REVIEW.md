# Existing build identity and draft review — issue #7

This is historical evidence for the original passive catalog. Its relevance-only
experiment and active reward-stack descriptions were superseded by
[issue #21's immediate rewards](../refinement-ii/21_IMMEDIATE_REWARDS.md).
Original source remains reproducible; ordinary play does not ship that catalog.


Local legal replays, native input checks and actual application screenshots are
the accepted evidence for this roadmap. These establish visible information and
functional choices; no human comprehension or balance result is claimed.

## What the player can inspect

City details and the reward header name the actual doctrine, army composition,
research count and acquired rewards. City details names purchased technologies
and rewards beside four existing directions:

| Direction | Existing choices | Decision changed |
| --- | --- | --- |
| Fortified defense | Warden, Guardian, tower cards, Guardian Roots | Protect the Watchpost; compare a tower home with soldier space; choose reduction over Assault. |
| Protected ranged | Huntsman, Precision or Venom, fletching, Quiver | Pay for bows and range, screen their reduced health with melee homes, select an exclusive ranged branch. |
| Status combinations | Arcanist, Fire/Frost, Venom, Conduit, Prism | Acquire an actual status source before Conduit; secondary damage stays within the engaged squad. Warhorn changes the first committed action. |
| Economic investment | Merchant, Grovekeeper, production cards/offshoots, Codex | Time the first sale, invest in producers against recruitment surcharges, pay prerequisites and wait for later clears. |

These are relationships, not exclusive classes or recommended power rankings.
By wave five the same factual inventory is available without a guessed build
score. Rewards without compatible purchases explicitly name a pivot. Conduit
also reports whether current allied soldiers can supply a status. Current-target
labels establish possible use, not usefulness against the next encounter.

`BuildImpact` in Shared queries detached state and Core's pure production,
capability, profile, tower, upkeep and reward arithmetic. Hypothetical modifiers
never become authoritative state or commands. It covers all 36 cards and 18
technologies: per-plot output, settlement/home towers, grouped existing soldier
level/rank profiles, range, status percentages/caps, upkeep, immediate stocks,
Watchpost reset and conditional/delayed effects. HP uses Core fixed-point format;
maximum-HP gains do not heal veterans. Locked research retains canonical benefits
and targets but does not pretend it can be bought. Available research previews
use Core's real purchase calculation, including prerequisites and exclusivity.

Reward descriptions/effects have a minimum scrollable copy area; current targets,
optional inspection and all three Choose buttons remain native Controls. The
720p layout was corrected after captures exposed a zero-height copy area with an
expanded recap. Details remain optional, and inspecting/toggling spends nothing.

## Legal comparisons

Reproduce with:

```sh
mise run scenario build-review artifacts/observations/build-identity/review.json
mise run test-shared
mise run capture-gameplay-ui
```

The report contains match/seed identity, actual paid action trails, per-wave
compositions, home occupancy, technologies, acquired cards and retained offers.
Six doctrines use the opening policy through five battles on seeds 1 and
20261018. Five existing policies (frontline, ranged, mixed, towers, research)
use Warden through twenty battles on both seeds. Relics are preferred when
actually offered, so the cap is exercised. This policy is a development probe,
not player automation. All decisions use ordinary commands and real prices.
Dying bodies are drained before the next preparation; every numbered entry must
reach its actual numbered battle.

Examples at wave five, seed 1:

| Replay | Army / research | Distinct paid decision evidence |
| --- | --- | --- |
| Warden / opening | 15 Swordsmen | Five occupied melee homes. |
| Arcanist / opening | 9 Swordsmen + 1 Mage | Early Arcanum and Mage equipment; four occupied mixed-role homes. |
| Huntsman / opening | 6 Swordsmen + 3 Crossbowmen | Extra Lumbermill, Archery Range and two melee homes plus one bow home. |
| Warden / ranged | 4 Crossbowmen | Ranged producer/equipment opening and two ranged homes instead of melee expansion. |
| Warden / mixed | 9 Swordsmen + 3 Berserkers | Mixed recruitment and utility/training purchases. |
| Warden / towers | 10 Swordsmen | Paid settlement Arrow/Catapult investments; fewer soldiers than frontline. |
| Warden / research | 15 Swordsmen + Melee Foundation | Paid Research Tower; later Guardian and Guardian Mastery, permanently excluding Assault. |

Merchant delays the first Farm; Grovekeeper pays extra recruitment resources and
adds production; Warmaster pays its melee surcharge. Those starting tradeoffs
are also recorded in the [opening review](OPENING_REVIEW.md). A successful
scripted twenty-wave run proves neither equal viability nor optimal choices.

## Relevance experiment and retained rules

The final report contains **286 real retained drafts** across 26 trajectories,
including the paths used to reach sparse checkpoints. Trajectories deliberately
share early policies/seeds; these are coverage receipts, not independent samples
for a statistical estimate. A current target means an existing compatible
producer/soldier/tower, usable stock/research, Watchpost protection, or a current
status source for Conduit. It is not a utility score.

| Coverage | Drafts / samples | No current target in all three offers | After one-slot experiment |
| --- | ---: | ---: | ---: |
| Actual early clears 1–4 | 98 | 11 | 0 |
| Actual late clears 14–19 | 68 | 4 | 0 |
| All actual drafts | 286 | 30 | 0 |
| Sparse early, after clear 1 | 256 | 64 | 0 |
| Sparse late, after clear 17 | 256 | 49 | 0 |

Sparse samples start from legally earned preparations, retire all surviving
soldiers with real commands, then evaluate the previous completed-wave pool for
128 seeds per checkpoint/trajectory. They do not reroll retained offers or
pretend to be cleared empty-army battles. Future-unit and producer cards remain
eligible: those deliberate pivots are not mislabeled as current benefits.

The experiment changes at most one slot only when all three lack current
targets. It selects a current-target eligible card of the same rarity as the
replaced slot using the deterministic seed. It preserves three distinct eligible
cards, rarity composition and caps. Every baseline draft is generated twice and
compared, and every actual offer matches the retained authoritative offer seed.
164 actual samples have capped cards; 92 have two acquired relics. Both sparse
late trajectories have capped cards and two relics across all 256 samples.
Single- and two-stack caps, early relic exclusion, late payoff exclusion and the
two-relic cap stay under Core's existing eligibility calculation.

**Decision:** retain the current draft algorithm and all numeric bonuses. The
experiment demonstrates a way to reduce disconnected offers, but also biases
away from deliberate future purchases and cannot distinguish a weak current
bonus from a valuable pivot. Its zero-miss result is constructed by the filter,
not proof of better decisions, fun or balance. No catalog expansion, rules
version, saved offers, reconnect protocol, RNG stream or rarity weighting changes
ship. Existing range/fragility, exclusive branches, status timing, first-strike,
structural protection and economic delays remain intact.

## Verification and limits

Focused Shared checks inspect all cards/technologies on a legally paid mixed
roster without mutations, check empty-roster pivots and fixed-point Keep/Watchpost
values, compare accepted research and offered production/bow rewards with real
income/profiles, inspect settlement plus home towers, and repeat multi-family
legal drafts for determinism. Native checks click effect details, toggle with
keyboard, inspect actual Worldtree targets and verify all three reward choices
fit with the collapsed recap at both sizes.

Actual PNGs are under `artifacts/captures/gameplay/forest/` and its `1920/`
subdirectory: `city-details`, `reward-draft`, `reward-current-effects`,
`battle-result-draft`, `battle-result-expanded`, `worldtree-*-available` and
`worldtree-*-owned-selected`. The final target-first Worldtree captures also live
under `artifacts/captures/godot/worldtree/`.
Plain/debug counterparts are under `artifacts/captures/gameplay/`. The same
native purchase, focus, authenticated pending/foreign state and menu flows run
with legal snapshots. Logs and receipts live in `artifacts/test-logs/roadmap-07-*`
and `artifacts/observations/build-identity/review.json`.

Executed-check totals are recorded in the issue closure checkpoint. No human
playtesting, live Steam session, other-OS runtime or physical-controller/listening
qualification was performed. The known intermittent native .NET/JIT crash risk
remains. Core, approved artwork/audio, pins and preference formats are unchanged.
