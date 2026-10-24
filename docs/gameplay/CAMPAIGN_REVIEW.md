# Campaign purposes and boss presentation — issue #9

Historical #9 review at b4fa3f7: its tables, strategies and recordings describe the
original campaign, not the current default. Refinement II selects paired elites
at W10 while retaining the W20 boss. Current comparisons, counterexamples and
qualification status are in [the #25 report](../refinement-ii/25_CAMPAIGN_CONSEQUENCES.md).

## Selected curve

Retain the existing twenty-wave composition, levels, rewards and single-boss
constraint. Make its sequence explicit: introduce roles, alternate mixed pressure
with equipment checks, isolate two boss duels, then return to formations after
the first duel. The confirmed presentation gap is addressed with factual threat
briefs and boss-specific figures, health emphasis, anticipation and defeat cues.
No enemy mechanic, escort, asset, rule or compatibility change is introduced.

This is a deliberate retention decision after the comparisons below, not proof
that the current curve is balanced or challenging for humans. Increasing boss HP
only to force a longer encounter is not justified by this corpus. The short-duel
tradeoff remains visible in the data and is a useful target for later human
review. Local legal replays and actual screenshots are the user's accepted
roadmap evidence.

## Tactical purposes in the actual schedule

Counts below are allocations **per original city**. Entry ordering is seeded;
counts/levels remain fixed. Fallen-city allocations are redistributed, so the
accepted city forecast is authoritative. `EncounterBriefing` derives purposes
from catalog composition/equipment transitions rather than changing the catalog.
City details shows the current problem, possible decisions, all twenty purposes,
levels/counts and base clear rewards. Preparation shows purpose and equipment
levels; its tooltip has the full brief and actual boss HP/primary damage.

| Wave | Existing allocation | Problem / decision to inspect |
| --- | --- | --- |
| 1 | 4 Swordsmen L1 | Fund a frontline and use available squad capacity. |
| 2 | 3 Swordsmen L1 + Berserker L1 | First heavier primary/secondary melee hitter; compare protection. |
| 3 | 3 Swordsmen L1 + 2 Crossbowmen L1 | Ranged pressure; inspect screens and support homes. |
| 4 | 3 Swordsmen + Berserker + Crossbowman + Mage, all L1 | First four-role mix; magic adds within-squad secondary pressure. |
| 5 | 3 Swordsmen L2 + Crossbowman L1 | Fewer enemies, stronger frontline equipment; compare trainer quotes. |
| 6 | 3 Swordsmen + Berserker L2, Crossbowman L1 | Heavy melee with ranged cover; protection/slowing versus damage. |
| 7 | 3 Swordsmen + 2 Crossbowmen L2, Mage L1 | More ranged/support pressure; review funding before expansion. |
| 8 | 3 Swordsmen + 2 Berserkers + 2 Crossbowmen + Mage, all L2 | First eight-member mixed-pressure peak. |
| 9 | 3 Swordsmen L3, Berserker + Crossbowman + Mage L2 | Another frontline equipment check before the duel. |
| 10 | One Swordsman boss L3 | Durable single-member target; primary damage/protection versus secondary investment. |
| 11 | 3 Swordsmen L3 + 2 Crossbowmen L2 | Return to formations after doubled boss clear resources; opportunity to replenish, not a healing grant. |
| 12 | 3 Swordsmen + Berserker + 2 Crossbowmen, all L3 | Heavy hitters under ranged cover. |
| 13 | 3 Swordsmen + 2 Crossbowmen + Mage, all L3 | Mage support; compare current status sources and support exposure. |
| 14 | 3 Swordsmen + 2 Berserkers + 2 Crossbowmen + Mage, all L3 | Eight-member mixed pressure. |
| 15 | 3 Swordsmen L4, Crossbowman + Mage L3 | Lower volume with a new frontline tier. |
| 16 | 3 Swordsmen + Berserker + 2 Crossbowmen, all L4 | Heavy/ranged pressure at the new tier. |
| 17 | 3 Swordsmen + 2 Crossbowmen + Mage, all L4 | Support pressure at L4. |
| 18 | 3 Swordsmen + 2 Berserkers + 2 Crossbowmen + Mage, all L4 | Eight-member mixed pressure before the final equipment step. |
| 19 | 3 Swordsmen L5, Berserker + 2 Crossbowmen + Mage L4 | Final frontline equipment check. |
| 20 | One Swordsman boss L5 | Final duel; no escort or later reward draft. |

Bosses retain eightfold health and doubled primary/secondary/structural damage
before the existing enemy strength multiplier. The actual default solo forecast
is 438 HP / 27 primary for W10 and 798 HP / 49.5 primary for W20. Each boss has
one member in its own squad: secondary attacks have no *other member* to hit in
that squad. These statements do not promise a particular damage result against
armor, rally, statuses or an altered rules configuration.

## Reproducible comparisons

```sh
mise run scenario campaign-review artifacts/observations/campaign/review.json
mise run capture-bosses
mise run capture-gameplay-ui
mise run test-animation
```

`CampaignReview` records twenty runs: frontline, ranged, mixed, towers and
research on seeds 1/20261018, each with baseline and forecast-response variants.
All decisions are ordinary paid commands; there is no simulation state injection.
It records every wave's composition, purposes, actual command trail, responses,
participants, casualties, Keep damage, Food, settled duration and clear receipt.
Before every battle it compares the accepted forecast to the real spawned
profile/origin/destination multiset and checks the exact consumed Food manifest.
Death cleanup is drained before advancing.

All **400 battles** completed and all runs reached Victory with zero Keep damage.
Those are optimized development policies, not a representative human sample;
some response variants make no changes, so these are not 400 independent trials.
A simple no-investment defeat remains covered by the normal Shared scenarios.

| Policy / seed | Baseline ticks / lost | Response ticks / lost | Actual response |
| --- | ---: | ---: | --- |
| Frontline / 1 | 5306 / 26 | 5306 / 26 | None triggered. |
| Frontline / 20261018 | 5252 / 26 | 5252 / 26 | None triggered. |
| Ranged / 1 | 4938 / 17 | 4938 / 17 | None triggered. |
| Ranged / 20261018 | 5276 / 18 | 5276 / 18 | None triggered. |
| Mixed / 1 | 4758 / 24 | 4758 / 24 | Magic Foundation W6; Frost W12 when heavy melee is forecast. |
| Mixed / 20261018 | 4774 / 25 | 4636 / 22 | Same research decisions; different resulting losses/duration. |
| Towers / 1 | 5726 / 30 | 5726 / 30 | None triggered. |
| Towers / 20261018 | 5606 / 29 | 5606 / 29 | None triggered. |
| Research / 1 | 5400 / 25 | 5370 / 26 | Bank points for Assault at boss W10; Assault Mastery for L4 frontline W15, instead of Guardian. |
| Research / 20261018 | 5475 / 26 | 5399 / 28 | Same offensive branch; faster total with more casualties in this run. |

The response policy also considers moving exposed support to a free rear home;
no override was triggered in this corpus because the existing placements already
had support there. Do not describe those runs as proof of a deployment benefit.
These are sensitivity observations: altered casualties/recruitment can change
subsequent RNG identities and resources. The differences are not controlled
causal estimates or build rankings. Both offensive and defensive investments
remain understandable choices; one is not declared universally superior.

Across the ten baseline runs, settled W10 duration was **187–301 ticks
(3.12–5.02 s)** with **0–3 casualties**. W20 was **159–301 ticks (2.65–5.02 s)**
with **0–1 casualty**. Durations include authoritative corpse cleanup, so they
are not just time to the killing impact. Mixed W8 had **0–6 casualties** and W18
**0–4**. Bosses are brief, concentrated duels, not universally the largest
attrition spikes. Their double resource payout creates a replenishment option;
W11 still contains real ranged enemies and is not guaranteed safety or recovery.

## Playback-owned boss treatment

The existing figure is drawn at 1.38× visual scale with a technical gold crown.
Metadata/ground anchors, physical homes, unit profiles, logical picking radius,
depth order, targets, hit resolution and occupancy are unchanged. The motion
sampler amplifies the existing boss windup/lunge using the same authoritative
start/impact/ready ticks; it does not insert a delay or extra attack.

A fixed-screen-size health emphasis at the bottom center uses actual presented
boss health/maxima for the inspected destination. Individual gold boss badges
remain available, and squad label placement avoids the boss body/crown. The
health display was moved away from the head after actual captures exposed an
overlap. Gold ground anticipation follows the pending impact interval. A real
boss Death event produces an expanding broken-ring response and DEFEATED text,
using event tick age. It never awards damage/rewards or extends death lifetime.

No Tween, particle clock, extra actor, new asset or boss mechanic drives these
cues. Pause freezes the underlying sampled time. Event deduplication and
playback-generation reset remain in the existing owners; restart clears old
health/death presentation. The bar avoids labeling a dead W10 boss as wave 11
while the authoritative state has already advanced. The current wave number
remains available in the normal match HUD.

## Verification and evidence

Shared checks cover purpose distinctions, the real single-boss forecasts and
recordings, unchanged six-player budget scaling, legal forecast-responsive
research, exact rewards and terminal/no-future brief semantics. Animation
contracts cover actual boss anticipation/death ticks, one death at 30/60/120 FPS
and all three speeds, plus restart; ordinary contracts remain intact.

Native boss checks consume the forecast with the real Ready button, check the ordinary live HUD, then present
legal recorded W10/W20 frames for entrance, windup, wounded and death phases.
They check enlarged/crowned figures, actual HP, logical picking, pause and
restart without a detailed inspector. Ordinary/debug gameplay checks include
these probes at 720p/1080p. Settlement picking/fit, standard battle/animation,
artwork and session checks remain required; executed totals are in the closure
audit, not inferred from compilation.

Actual captures are under `artifacts/captures/bosses/{1280,1920}/` and ordinary
`artifacts/captures/gameplay/{forest/,}...` directories: `boss-{10,20}-forecast`,
`-live`, `-entrance`, `-windup`, `-wounded`, `-defeated`. Full command/metric receipts are
`artifacts/observations/campaign/review.json`; logs are
`artifacts/test-logs/roadmap-09-*`.

No human challenge/drama/comprehension or balance qualification is claimed.
Live Steam, other-OS runtime and physical controller/audio checks remain
unexecuted; the known intermittent native .NET/JIT risk remains. Core, source
art/audio, dependency/lock pins and preferences are unchanged.
