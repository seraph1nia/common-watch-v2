# Campaign consequences — issue #25 design

Baseline41cc004 (#24), immediate rewards1, three actual preparations, combat8,
wire19/battleflow4. Original b4fa3f7 exact legal replay archive remains separate
and unchanged; see25_ORIGINAL_BASELINE.md. Preserve current source/raw receipts
and use distinct capture destinations. #24 exact-SHA CI83 is running.

Before any shipping Core tuning, build executable paid comparisons from fresh
Matches. Matched seeds1/20261018 and held-out seeds42/98765 share match identity
within each pair. Preserve full command histories, starting profiles/resources,
research/reward choices, actual forecast/upkeep, per-tick events and sampled
positions. Independently replay recorded commands/events without rerunning the
policy. No snapshot writes, free resources, injected battles or battle timing
changes. Record combat and cleanup ticks separately; headless actual presented
wall time stays null. Normal1x viewing uses the actual0.5 rate factor.

Compare same-army protected/exposed support placement; frontline versus damage
budget with residual resources; tower versus competing soldier home; recruitment
now versus paid producer/trainer investment over three waves; primary/secondary/
status research against actual formations. Include imperfect plausible choices,
not only optimized wins. Examine first impact/target access/support wounds,
casualties, objective damage, Food, replacement/recovery burden and later stocks.
At least three different decisions must show repeatable useful consequences;
report ineffective or seed-sensitive choices and remove unsupported counter copy.
Entity/action ordering can change RNG even when seed and costs match.

Prototype elite formations atW10/W20 with existing ordinary roles/levels and the
same clear reward. Explicitly define them as nonboss waves, respecting the existing
single-boss validator; no boss-plus-escort exception. Quantify resolved power,
health and damage instead of assuming equal counts mean equal strength. Compare
against the preserved single-boss campaign with the same earned prefix. A custom
campaign already changes configuration fingerprint; no schema/validator bypass
is needed for the experiment. Select shipping treatment only after measured
results and native normal-speed visibility/time review. Any default Core change
requires a separate provenance/version decision before implementation. No new
art/abilities or global HP inflation.

After selection, rerun final current-economy paths across six doctrines, five
families, matched and held-out seeds, marginal investments and1–6-city uneven
cooperation with weak/idle city, real elimination and continued group play.
Forecast profiles/entries and Food participants must equal actual spawns/upkeep;
frozen outcome and immediate grants must agree through retry/reconnect. Retain
negative paths rather than silently optimizing them away. Difficulty is not
established by correlated winning scripts or compulsory casualties.

Final delivery includes readable tradeoff tables, raw bounded replayable traces,
explicit selected/removed decisions, current native full sequences/terminal
screens, pause/death cleanup, compatibility, multiplayer/private reconnect,
animation, locked build/Core/Shared and Linux/Windows exports. Physical GPU/audio/
controller, live Steam/native Windows/macOS and known intermittent native.NET/JIT
remain qualified gaps; no human understanding/enjoyment is invented.

## First controlled results (before any shipping tuning)

All44 cases replay independently with exact snapshot/event checks. Python report
validation additionally checks identical common checkpoints, the same placement
army, actual target forecasts/upkeep, frozen outcomes and clear resources.
Matched seeds1/20261018 precede held-outs42/98765; ordering below follows that
fixed seed order. Resources and all other profiles/history are in raw receipts.

| Choice | Actual cost and consequence |
| --- | --- |
| W8 same Mage squad rear versus front | Both retire the same obsolete L1 front squad first, with no refund. Then only one ordinary deployment differs;14 identical IDs/health/profiles, same stock/research. Rear casualties1/2/4/1 versus front4/3/4/3. Held-out42 has equal4 losses but different duration/wounds; placement is not universally protective. |
| W8 frontline versus damage | Same15Metal ceiling:3L4 Swordsmen cost15,2L4 Berserkers cost14 and retain1Metal plus one free place. Casualties1/2/1/3 versus1/4/2/3. Equipment, health, Food and entity count differ by the chosen purchase, not hidden subsidies. |
| W8 tower versus soldiers in the same emptied front home | ArrowTower costs4Gold+3Wood;3L4 Swordsmen cost15Metal. Unequal resource vectors are explicit. Tower first-wave casualties improve in2/4seeds, but final W10 field survivors7/9/9/9 versus11/12/14/13. Tower remains, uses no soldier Food and retains15Metal; this is a multiwave space/supply tradeoff, not a universally superior tower counter. |
| W3 recruitment versus producer/trainer | Recruit-now spends6/9/6/6Metal. Metal producer upgrade costs2Wood+2Stone; trainer upgrade6Gold+2Wood+2Stone. Identical bounded continuation can catch up recruitment before battle, giving producer and recruit-now identical three-wave casualty sequences, with producer retaining36moreMetal byW5. Trainer pays more and W5 losses are0/0/0/0 versus2/3/2/2, leaving2–3more soldiers. Immediate shortage urgency is not supported in this case. |
| Grovekeeper second Metal producer versus Stone | Legal sale/build costs net1Wood, no grant. Three-wave losses are seed-sensitive; later Stone/trainer access and stock differ. Early switching is not a free or immediately superior correction, and original failures stay preserved. |

The common retirement in placement/budget/home experiments is explicit and
shared; it frees a home without introducing a different army between placement
arms. Action/entity order can affect RNG. Front placement does not always injure
the Mage: event traces show changed target access and frontline losses instead.
No inference of causality is made from the final win alone. Three different
choices have reproducible consequences, with held-out limits retained.

## Alpha major alternative

Alpha replaces a boss with3same-tier Swordsmen and one each Berserker,
Crossbowman/Mage one tier below. Six members, four squads, unchanged clear
4Gold+2Wood+10Food and1Research. Resolved powerW10:6.825→6.9125 (+1.28%);
W20:12.45→12.678125 (+1.83%). Score is a budget index, not a combat predictor.
56W10 research/context cases and8correctly verified W20 neutral cases replay.
The initial W20 verifier incorrectly demanded empty historical offers at Victory;
Core deliberately retains the previously accepted offer history. The corrected
check requires Victory, no new draft wave and exactly19accepted rewards; all8pass.
Old failed logs remain, without a gameplay/runtime defect claim.

Neutral W10 boss losses1/0/1/0 versus alpha2/1/2/0; friendly multi-member impacts
0throughout boss cases versus3/0/6/3. W20 losses2/1/1/1 versus4/1/2/3, with0 versus
6/5/6/6 multi-member impacts. Assault shortens the boss fight in some seeds;
Frost reduces alpha losses only in seed1, and Fire often ticks zero times before
resolution. These do not justify guaranteed-counter copy.

Actual1x/factor0.5 native sequences passed at720/1080, with recordings, three
viewport stills, inspection and separate observed resolution/cleanup intervals.
Seed1 total presented wall seconds: W10boss/alpha7.04/7.14 at720,8.03/8.33 at1080;
W20boss/alpha7.68/8.76 and8.67/9.94. Cleanup presentation is about0.8seconds.
These include PNG/geometry/recording overhead on llvmpipe, not human/hardware time.
Headless tick counts are kept separate. Native prepared snapshots can be checked
against the exact CLI checkpoints; no captured state is injected into authority.

Do not select alpha merely for producing more attrition. ItsW10 roster exactly
repeatsW9, whileW20 has fewer members thanW19. Inspected native scenes give no
new major identity beyond another ordinary mixed fight. Preserve alpha as an
executable comparison, then test one bounded composition change: two same-tier
Swordsmen plus two same-tier Berserkers, one Crossbowman two tiers lower and one
Mage one tier lower. This paired heavy frontline changes target/secondary/status
opportunities without new art or increased per-unit HP. Quantify its budget and
run matched/held-out neutral and relevant research arms before selection.

## Heavy formation and complete continuation experiment

The second formation has2Swordsmen and2Berserkers at the major's frontline tier,
1Crossbowman two tiers lower and1Mage one tier lower. Actual powerW10:6.98125
(+2.289%); W20:12.809375 (+2.887%). No per-profile HP or damage rule changes.
Both candidates stay within3% of the original score, with different HP/DPS/range
and target distributions explicitly retained. Two numerical/validator tests pass;
marking either multi-entry candidate as a boss still rejects. A frozen41cc004
campaign-data factory keeps both original boss comparisons runnable after later
shipping decisions. This is separate experimental data, never a runtime default.

All28heavy neutral/relevant research comparisons replay. In seed order
1/20261018/42/98765, W10 neutral losses0/1/2/0 against boss1/0/1/0; W20 heavy
5/0/2/3 against boss2/1/1/1. Neither option universally increases casualties.
Heavy multi-member impactsW10:0/0/3/6, W20:3/6/6/6; the boss has none. Frost and
Venom do not reliably change these short encounters; most periodic effects tick
zero times. Do not promote a guaranteed slowing/poison counter from these cases.
Assault changes some durations without changing casualties, another limited result.

All8heavy native prepared snapshots equal their corresponding CLI checkpoints
exactly. Both resolutions'1x sequences pass with native inspection, unchanged
hit timing and recordings. Seed1 W10boss/heavy wall7.04/7.14seconds at720,
8.00/8.37 at1080; W20boss/heavy7.73/7.94 and8.65/9.13. All include capture overhead.
Inspected heavy scenes use the existing figures and compact ownership/target cues;
no added artwork or battle-label panel. Final shipping captures remain required.

A240-run campaign continuation now compares the original bosses with only W10
heavy changed: six doctrines×five families×four seeds×two catalogs, exact legal
command/final-state replay per row. The normal opening has an explicit paid
ranged trainer conversion. The first attempt exposed a development-policy
recruitment against its now-empty slot; Core correctly rejected it. The policy
now builds the ArcheryRange before recruiting, retaining the failed log. Four
unaffected frontline rows are reused only after exact independent replay.
Grove's second Metal producer remains in the main matrix, preserving the known
constrained-Stone failures. Full marginal/uneven cooperation, final selected-rule
receipts/native gates/exports/CI are still required. No shipping Core catalog
change or completed campaign-calibration claim has been made yet.

Reproduction: [tools/campaign-review](../../tools/campaign-review/README.md).
Current locked build has zero warnings; Core74/Shared168 pass. #24's exact-SHA
CI83/40060474672 is now SUCCESS including both exports; its source hashes remain
41cc004. Original b4fa3f7 archive and historical qualifications remain unchanged.

## Shipping selection and rules gate (before Core implementation)

All240 complete campaign rows independently replay every accepted command and
final snapshot. Boss versus W10-heavy:2126/2139 battles,98/99Victories out of120.
Matched seeds50/60→51/60; held-outs48/60→48/60. Frontline24/24, towers22/24 and
research24/24 stay unchanged; mixed21/24→22/24; pure-ranged7/24 stays unchanged.
The17early pure-ranged failures precedeW10 in both catalogs: an explicit paid
conversion and a legal policy do not make that bow-only reinforcement investment generally
safe. Do not advertise that continuation as a universal counter. Mixed armies
retain actual ranged/magic members. No profile/cost subsidy is selected.

All120 pairs have identical first-nine-wave receipts. The103pairs reachingW10
also have identical ordinary command histories through final Ready; the catalog
is the sole chosen encounter dimension. W10 casualties78→128 across those pairs:
46increase,43tie,14decrease. Full campaign casualties3104→3249; Keep damage
2200→2100wholeHP across the cohort. One Grovekeeper mixed seed1 defeatW14 becomes
Victory; other known constrained-Stone mixed/tower defeats remain (some occur
later). These are deterministic correlated policies, not independent fairness or
human samples. NPC identity/order afterW10 changes later RNG and follow-up policy
purchases; do not attribute every later difference to direct extra damage.

Select **heavy W10, retain the W20 single boss**. W10 becomes twoL3 Swordsmen,
twoL3 Berserkers, oneL1 Crossbowman and oneL2 Mage, a valid nonboss formation.
Keep its existing4Gold/2Wood/10Food clear and1Research. W20 stays oneL5 boss with
its existing multipliers/reward. No other count/level/cost/healing/stage/status
change is selected. W10 provides paired targets and earlier high-tier bruisers,
with real next-wave losses/funding/recovery implications. It differs fromW9's
ordinary mixed roster. W20 retains a contrasting single-member primary-damage
problem: the tested formation raises some terminal losses without a next-wave
recovery decision or a reliable new status counter, and replacing both majors
would remove that distinct target relationship. This is an evidence-backed
bounded selection, not a human enjoyment/climax claim or a requirement to lose.

Implementation must change only the defaultW10 catalog entry and its truthful
purpose/checkpoints/native expectations. Existing single-boss validation remains
strict; do not flag a formation as a boss or add escorts. Keep the frozen original
campaign and both alpha/heavy alternatives executable. Retain all counterexamples.

Compatibility decision: no command, snapshot schema, combat hit rule or transport
shape changes. Keep combat8, reward1, battleflow4 and protocol19. The campaign's
existing full content fingerprint identifies the changed catalog and detects
mismatched/tampered received data. It is not an admission rule requiring default
catalog equality: current-schema authorities may intentionally declare either
valid catalog. Compatible19peers receive the actual complete rules; older18peers
remain refused. Running authorities own immutable catalogs, and reconnect resumes
that declared catalog. A fresh default match uses the newW10; no active match,
credential file, old reward or stored health is migrated/reinterpreted. No settings
format changes. Explicit old-catalog snapshots remain useful diagnostics, not
ordinary saved games. Verify codec/catalog integrity and old-version refusal.

NPC count/composition changes seeded entry/action ordering and later identities;
all authoritative preconditions/retries remain unchanged. Immediate recovery
quotes may differ with actual wounds; their exact frozen application and ordinary
healing eligibility/caps do not change. Final shipping campaign/marginal/uneven
cooperation and native/export/CI gates are still required before completion.

## Shipping implementation checkpoint

The defaultW10 catalog is now the selected heavy formation. Shared purpose,
current `major-10` development checkpoint, showcase selector and native tests
follow that data. Historical `boss-10` CLI spelling remains an alias to the actual
currentW10, not a fabricated boss; explicit original comparisons use the frozen
campaign factory. Boss-only animation contracts now use the retainedW20.
Historical same-rule R4 presentation comparisons can pass `--profiles early dense
boss-10 boss-20`; they must not pretend this rules change preserves old outcomes.

The compact planning summary names the elite formation in its existing first
line, with actual composition and funded participants. The first native check
caught the missing label; the label was added and both720/1080 native runs pass.
Ready consumes the exact six forecast profiles with no crowns/fake boss health;
W20 retains its real health/windup/death/pause/restart/ground-picking checks.
The720 forecast was visually inspected and fits. Locked build is zero-warning,
Core75/Shared169 pass, including catalog data integrity, old-rule refusal and
strict single-boss validation. Full final integration remains outstanding.

## Selected-rule continuation and integrity results

All120selected rows replay from fresh **default** authorities, with the exact
initial rules/roster, every command/state hash and final hash. New integrity
checks compare Core's actual preview with spawned profiles, group slots, queued/
deployed lifecycle and entry cells, plus exact Food funded/unfunded/participating
IDs and paid amounts. Immediate rewards must match the frozen resource/Keep/
individual-HP quote without changing equipment/service/eligibility. No parallel
combat model or state injection is used.

The36additional1–6-city campaigns use matched1/held-out42 and invested, capped
last-city or idle last-city policies. All36 replay their events/snapshots and
pass entry/funding/outcome/grant integrity. All12invested groups win. Capped solo
Warden fallsW8/W14, while the other10capped groups win with actual allied help.
Idle solo fallsW1 in both seeds;2-city seed1 loses its invested cityW1 then the
remaining cityW2. Six-city idle seed1 loses city6 atW10 and seed42 atW17; both
groups continue toW20Victory. Other idle groups sometimes rescue the inactive
city. No elimination, comeback or casualty is forced.

Twelve separate capped-equipment/home campaigns span all6doctrines×seeds1/42;
all fallW8–16 through real combat. Capped groups/duplicate policies are correlated,
not48independent trials. The original constrained-Stone and early pure-ranged
failures remain alongside these invested mistakes and optimized successes.

Ten final normal-policy trace archives cover all5families×matched seeds:172
battles,8Victories and2ranged Defeats. Full command hashes, per-tick events,
positions/statuses, before/after states, actual clear/reward/funding receipts and
separate headless combat/cleanup ticks independently reproduce. Actual presented
wall time remains null for these headless histories. The original b4fa3f7 archive
stays separate; original baseline/adaptive policies are also rerun separately
under the selected rules. Current recovery comparison:24campaigns/480battles,
12identical individual/group digest pairs and1668member moves. Those reserve
policies all win; this is equivalence coverage, not a universal recovery argument.

Real six-client W10 captures at720/1080 each consume108funded troops and36ordinary
elites, with no fake boss. Actual1x/factor0.5 tick totals300/239, measured wall
10.84/13.51seconds including capture/transport/rendering overhead. Concurrent
command/identity histories differ, so these are separate native observations,
not a causal comparison of resolution. Local/overview Controls, team context and
existing figure art were used; the720local still was visually inspected.

Finished generated traces are losslessly compressed with decoded-byte SHA checks:
5263642505→351228448bytes for cooperative raw traces and310430456→15966609 for
final baseline raw traces. Before/after/joined/final snapshots and NDJSON can be
read transparently by the verifier. Summaries and source evidence stay explicit.
A full six-city idle-to-Victory replay also passes after compression.

## Removal/consolidation and experiential validation

W10's old boss promise/crown-specific test is removed from the current default;
W20 keeps the real boss visual/animation contract. New forecast wording names
actual elite pairs in the existing summary instead of adding a panel. Boss-only
showcases/tests useW20; the oldW10 spelling is a documented current-major alias.
Old #9 campaign tables are labelled historical. The heavy hint no longer promotes
a slowing counter that the short-encounter comparison failed to support, and
ranged wording does not promise automatic protection from a rear placement.
No automatic investment, forced casualties, new currency/abilities, permanent
recovery automation or passive reward system is added.

Automated consequences: exact accepted commands, profile/entry/Food/reward
integrity, losses, costs, timed native playback and source/rendered geometry.
Inspected visuals: actual720/1080 formation/boss comparisons,720selected forecast,
and six-client720local field; these are not human-play observations.
Human evidence: no interviews, comprehension, enjoyment, perceived fairness,
physical controller/audio/GPU or native Windows/macOS execution was observed.
Live Steam and the known intermittent native.NET/JIT risk remain qualified gaps.
The rare living-owned deployed survivor at first city elimination still lacks a
positive natural native checkpoint; deferral fixtures prove the guard only.
Final full native regression/journeys, exports, exact-SHA CI and parent completion
audit remain required before marking this roadmap complete.
