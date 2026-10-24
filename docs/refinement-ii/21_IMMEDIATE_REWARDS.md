# Immediate battle rewards — issue #21 design

This design precedes intentional Core edits. Gate A passed at `ce2d5c6`: [Godot run 71](https://github.com/seraph1nia/common-watch-v2/actions/runs/40044935480)
completed successfully, including both exports, before any Core rules edit. The original-rule
[command/event archive](25_ORIGINAL_BASELINE.md) is independently reproduced.
Original import and GPL notices remain; the reward redesign is explicitly local.

## Catalog audit and removal

Original definitions: `src/Game.Core/Roguelite.cs` at b4fa3f7. All old cards require
a standing recipient, W1–19, available stack capacity, safe resource/stat arithmetic.
Relics require W7+, at most two distinct relics. Rarity weights are Common/Uncommon/
Relic 75/25/0 (W1–4), 60/40/0 (5–6), 60/35/5 (7–9), 45/40/15 (10+).
An exhausted rarity falls back deterministically. Consumable shipments also had
stack caps, so historical acquisition changed later eligibility.

| Original ID / name | Rarity; stack cap | Effect / extra eligibility | New-run disposition |
| --- | --- | --- | --- |
| E01 · The Lumber Guild | Common; 1 | Every Lumbermill +1 Wood per production; additive with Worldtree. No additional eligibility rule. | Remove from normal pool and active modifiers |
| E02 · Bountiful Harvest | Common; 1 | Every Farm +1 Food per production; additive with research. No additional eligibility rule. | Remove from normal pool and active modifiers |
| E03 · Golden Veins | Common; 1 | Every Gold Mine +1 Gold per production. No additional eligibility rule. | Remove from normal pool and active modifiers |
| E04 · Master Stonecutters | Common; 1 | Every Stonecutter +1 Stone per production. No additional eligibility rule. | Remove from normal pool and active modifiers |
| E05 · Deep Mining | Uncommon; 1 | Every Metal Mine +1 Metal per production. No additional eligibility rule. | Remove from normal pool and active modifiers |
| E06 · The Weaver's Pact | Uncommon; 1 | Every Weaver +1 Cloth per production. No additional eligibility rule. | Remove from normal pool and active modifiers |
| A01 · Hardened Shields | Common; 2 | Swordsmen +10% maximum HP per stack; living and future recruits. No additional eligibility rule. | Remove from normal pool and active modifiers |
| A02 · Berserker's Fury | Common; 2 | Berserkers +12% primary damage per stack. No additional eligibility rule. | Remove from normal pool and active modifiers |
| A03 · Precision Fletching | Common; 2 | Crossbowmen +10% primary damage per stack. No additional eligibility rule. | Remove from normal pool and active modifiers |
| A04 · Arcane Barrage | Common; 2 | Mages +12% secondary damage per stack, within the engaged squad only. Remaining secondary targets required. | Remove from normal pool and active modifiers |
| A05 · Veteran Drills | Uncommon; 1 | All friendly soldiers +5% maximum HP, present and future. No additional eligibility rule. | Remove from normal pool and active modifiers |
| A06 · Efficient Rations | Uncommon; 1 | Berserker/Mage upkeep −1 Food per participating member (minimum 1). No additional eligibility rule. | Remove from normal pool and active modifiers |
| T01 · Reinforced Watchposts | Common; 2 | Watchpost +20% maximum HP per stack; resets next battle. No additional eligibility rule. | Remove from normal pool and active modifiers |
| T02 · Sharpened Shafts | Common; 2 | Arrow/Rapid Arrow towers +10% damage per stack. No additional eligibility rule. | Remove from normal pool and active modifiers |
| T03 · Heavy Ammunition | Common; 2 | Catapult/Bombard towers +10% primary and secondary damage per stack. No additional eligibility rule. | Remove from normal pool and active modifiers |
| T04 · Eagle's Eye | Uncommon; 1 | Arrow/Rapid Arrow towers +1 attack range. No additional eligibility rule. | Remove from normal pool and active modifiers |
| T05 · Rapid Mechanisms | Uncommon; 1 | Arrow/Rapid Arrow towers full attack cadence −10%; positive windup/recovery. No additional eligibility rule. | Remove from normal pool and active modifiers |
| T06 · Quick Winches | Uncommon; 1 | Catapult/Bombard towers full attack cadence −10%; positive windup/recovery. No additional eligibility rule. | Remove from normal pool and active modifiers |
| M01 · Spell Focus | Common; 2 | Mages +10% primary damage per stack. No additional eligibility rule. | Remove from normal pool and active modifiers |
| M02 · Arcane Endurance | Common; 2 | Mages +12% maximum HP per stack. No additional eligibility rule. | Remove from normal pool and active modifiers |
| M03 · Frost Runes | Common; 1 | Mage attacks +10 percentage points Chill, even without research; cap 50%, existing duration. No additional eligibility rule. | Remove from normal pool and active modifiers |
| M04 · Ember Sigil | Uncommon; 1 | Mage attacks +10 percentage points Burn, even without research; cap 40%, existing duration. No additional eligibility rule. | Remove from normal pool and active modifiers |
| M05 · Venom-Tipped Bolts | Uncommon; 1 | Crossbow attacks +5 percentage points Poison, even without research; cap 20%, existing stack cap/duration. No additional eligibility rule. | Remove from normal pool and active modifiers |
| M06 · Elemental Conduit | Uncommon; 1 | Friendly soldiers +8% primary damage against a squad already afflicted by Burn, Poison or Chill before impact; once only. No additional eligibility rule. | Remove from normal pool and active modifiers |
| F01 · Timber Shipment | Common; 2 | Immediately +4 Wood × reward multiplier. No additional eligibility rule. | Retain effect family as new one-shot quote; new ID |
| F02 · Emergency Provisions | Common; 2 | Immediately +8 Food × reward multiplier. No additional eligibility rule. | Retain effect family as new one-shot quote; new ID |
| F03 · Equipment Shipment | Common; 2 | Immediately +3 Metal and +2 Cloth × reward multiplier. No additional eligibility rule. | Retain effect family as new one-shot quote; new ID |
| F04 · Field Surgeons | Uncommon; 1 | Immediately restore up to 10 Keep HP and heal each living friendly soldier by 20% of missing HP, including storage/reserves. No resurrection. Keep or any living soldier below maximum. | Retain effect family as new one-shot quote; new ID |
| F05 · Forgotten Knowledge | Uncommon; 1 | Immediately +2 personal Worldtree Research points. Research addition must fit. | Remove from normal pool and active modifiers |
| F06 · Royal Treasury | Common; 2 | Immediately +5 Gold × reward multiplier. No additional eligibility rule. | Retain effect family as new one-shot quote; new ID |
| R01 · Echoing Warhorn | Relic; 1 | First committed attack action per friendly squad each battle +25% primary and secondary damage. No additional eligibility rule. | Remove from normal pool and active modifiers |
| R02 · Ashen Quiver | Relic; 1 | Crossbow hits also apply Burn equal to 8% of actual damaging hit; existing Burn timing. No additional eligibility rule. | Remove from normal pool and active modifiers |
| R03 · Iron Harvest | Relic; 1 | Each successful battle +1 Metal per 3 enemy members killed by this city's soldiers, maximum 5 Metal. A future allocation can yield ≥3 kills. | Remove from normal pool and active modifiers |
| R04 · Crystal Prism | Relic; 1 | Mages +20% secondary damage, within the engaged squad only. Remaining secondary targets required. | Remove from normal pool and active modifiers |
| R05 · Ancient Codex | Relic; 1 | +1 Research every second successful clear after acquisition; acquisition clear excluded. At least two later clears. | Remove from normal pool and active modifiers |
| R06 · Guardian Roots | Relic; 1 | Keep receives 15% less structural damage while its actual Watchpost is alive. No additional eligibility rule. | Remove from normal pool and active modifiers |

No removed card migrates to Worldtree. Its existing research structure and all
six doctrine identities remain. Original definitions remain reproducible from
the pinned archive, not as active shipping modifiers or silently reinterpreted
historical IDs. Consumed rewards are factual receipts, not a permanent build.

| Existing reader | Required change |
| --- | --- |
| Roguelite capabilities, upkeep, Watchpost and tower calculators | Retain doctrine/research effects; remove reward stacks/stat/cadence/status modifiers. |
| EconomyConfiguration Production / RunIdentity | Remove producer cards; fingerprint the immediate catalog and explicit reward version. |
| World.CanApplyRun / ResolveReady | Retain doctrine bound checks; remove reward hypothetical validation and Codex/Harvest projections; preserve all ordinary overflow preflight and reserve three positive supply choices before a nonfinal battle. |
| World.ResolveCombat / ApplyReward / ResolveDraft | Preserve standard clear payout + one Research; remove Harvest/Codex; freeze result and quotes, authenticate one atomic application, retain disconnected fallback. |
| CombatSimulation BeginWave / StartAttack / Impact / kills / structures / towers | Remove Warhorn, Conduit, kill tracking, Roots, reward range/damage and relic state. Doctrine, Worldtree, ordinary statuses and combat RNG stay. |
| SnapshotCompatibility / SnapshotPayload / Diagnostics | Require reward version, bounded valid quotes/receipts, protocol and fingerprint; reject old data explicitly. |
| RunPresentation / Shared BuildImpact / BuildDirection | Exact quoted one-shot effects and consumed receipts; remove stack/relic/synergy copy. Worldtree previews remain. |
| Development BuildIdentityReview / reward policies | Replace obsolete passive relevance experiment with immediate receipt evidence; update legal checkpoint choices. Historical reports remain labelled original. |
| DraftPanel / RewardCard / HudJourney / BattleOutcomeJournal | Three exact primary choices, optional arithmetic/cap detail, authoritative receipt once, frozen pre-recovery outcome even after reconnect/same-frame progression. |

## Frozen quotes and accounting

Core owns a small immediate catalog with new `loot-*` IDs: Timber, Provisions,
Equipment, Treasury and Recovery reuse the five existing families. One Stone
shipment supports defensive construction and capacity-edge choice coverage.
No research, passive buff, unit spawn, currency, inventory or new art.

| Chosen family | Base effect | Scaling / caps |
| --- | --- | --- |
| Timber | 4 Wood | × completed-wave multiplier; positive remaining resource capacity only |
| Provisions | 8 Food | same |
| Equipment | 3 Metal + 2 Cloth | same, saturate each component independently |
| Treasury | 5 Gold | same |
| Stone | 4 Stone | same; only added family |
| Recovery | Up to 10 Keep HP + ceiling 20% of each living soldier's missing HP | No wave multiplier; stored/reserve included, no resurrection; suppress if total benefit zero |

Multipliers remain 1 at completed W1–6, 2 at W7–13, 3 at W14–19. Bosses retain
only their existing standard clear payout; no extra chosen-bonus multiplier.
Standard Campaign.WaveRule.Reward and one Research are applied once before the
chooser. The chosen bonus is separately accounted and never repeats that payout.
These amounts are starting comparison values, not a claim that the old economy
is preserved. No enemy retuning belongs to this change.

Before incrementing Wave, freeze the factual casualties/Keep loss and each offer's
completed wave, resource delta, exact living member IDs/healing and Keep repair.
Snapshots detach every nested array. Draft phase has no spending, recruitment,
research, ordinary recovery or health-changing command; another player's choice
and corpse cleanup cannot change these subjects. Revalidate the frozen plan
before atomic application; mismatch rejects with the original offers intact,
without a client reroll or partial grant. A receipt records actual resources,
Keep healing, soldier total/count and meaningful capacity limits.

Select three distinct positive families by a stable independent RewardStream
shuffle. There are no rarity or stack caps. Repeating a consumable in later
waves never creates a passive modifier. Keep at most 19 receipts per city in the default twenty-wave campaign
(or 255 for the existing bounded custom-campaign maximum);
requesting details, resizing, pause or reconnect does not consume combat RNG.
No final Victory draft or eliminated-recipient grant.

Finite int stocks at every maximum plus full health cannot receive three positive
immediate benefits. Do not disguise this as three zero-benefit cards. Before
entering a nonfinal fight, the existing atomic Ready preflight checks projected
post-upkeep/post-clear balances leave at least three positive supply families.
An extreme configured/stock state lacking capacity is rejected before any Ready
side effect, with a resource-capacity reason; ordinary spending can create room.
Quotes saturate a partially capped resource with checked 64-bit intermediates.
Default bounded campaigns remain far below this limit. Final Victory needs no
reserved chooser capacity. Test both partial caps and explicit preflight refusal.

## Compatibility and authority

Wire protocol becomes 18. MatchSnapshot gains explicit RewardRulesVersion 1;
missing historical value is 0 and rejected by both payload decode and full
validation. Include version/catalog in RulesIdentity; retain combat rules 8 and
battle-flow 3. Old hosts, offers, snapshots and private resume sessions require a
compatible running authority or fresh match, never migration by reusing an ID.
Credentials remain private and absent from settings, snapshots and evidence.

The existing authenticated command sequence/match/phase/turn and retry ledger
remains the only grant route. Duplicate delivery reuses its original result;
reconnect before/after acceptance and independent player choices cannot pay
again. Freeze a per-city battle result before rewards so healing cannot rewrite
losses when acceptance/progression share a snapshot. HudJourney displays the
stored actual receipt once and retains compact syncing/waiting, no extra Continue.

## Native presentation and comparison plan

Primary card copy is the exact authoritative delta, including recovery totals,
count and Keep caps. All three Choose actions stay visible at 720p/1080p using
the existing painted cards. Remove mandatory Current effects and stack/rarity
counters; optional detail explains eligibility/caps. Selection waits for authority;
rejection retains stable offers. Inspection/reopen cannot select.

The matched legal economy policy runs every doctrine through its opening and
five full campaign families (frontline/ranged/mixed/towers/research), seeds 1 and
20261018: 72 runs. Both catalogs use ordinal offer choice, deliberately not a
new-rule optimizer. Preserve exact commands, resource movements by action,
clear/bonus grants, funded army, casualties/wounds/Keep damage and recovery,
research, reward variety and stopping-point shortages. Reproduce the before
report against isolated original sources using tools/reward-economy. This exposes
lost permanent power and extra purchasing without claiming fairness from wins.
Final selected-economy calibration and held-out seeds remain #25.

Required verification: locked build; Core/Shared and compatibility/replay tests;
full/partial/zero recovery, stored/dead members, all resources and capacity edges,
repeat choices through W19, stable RNG/offers, no W20/eliminated draft, duplicate
and remote delayed-ACK/reconnect; native exact three-card/receipt views both sizes,
session/art/animation and multiplayer. Report automated copy/input separately
from human understanding and known native/platform/Steam gaps.

Implementation and measured results follow in a separate commit after this design.
