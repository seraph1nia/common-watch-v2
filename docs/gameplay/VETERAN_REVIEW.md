# Veteran retention decision — issue #8

## Decision and purpose

Keep service as **history**, with no hidden stat or equipment progression. It
counts successful clears actually fought while funded. Preserving a soldier
keeps identity, service, current wounds, equipment and the resources already
invested. Keeping or recovering it can avoid a new recruitment payment. It does
not entitle an older equipment tier to the stats of a new recruit. Trainer
upgrades apply to future recruits; research and acquired rewards still affect
eligible existing and future soldiers through Core's current rules.

The selected bounded adjustment is to the decision interface: explicit history
labels, a read-only keep/recover/replace comparison, and reviewed retirement with
a separate confirmation. There is no authoritative rules, protocol, snapshot,
RNG, XP, equipment or balance change. This is the historical-service alternative
explicitly allowed by #8, not a new progression system.

## Alternatives evaluated

A small capped service bonus would create mechanical progression but would not
resolve large equipment gaps. For an unmodified Swordsman, a hypothetical total
5% HP benefit changes 40 to 42; the existing L2 recruit has 54 HP and 14 primary
strike versus L1's 40/10. Increasing service enough to erase multiple equipment
tiers would change early recruitment, research/reward overlap and power budgets.
The replay evidence here does not justify that additional progression system.

Paid re-equipment could retain identity, but the price and wound policy would be
new economic rules. At the recorded W8 state, a full-price L4 re-equipment for
5 Metal while retaining 32.5 current HP would compete with a 5-Metal fresh recruit
at 107.8 HP. A discount or proportional heal would introduce a different economic
or healing benefit. Those are meaningful designs to test if equipment retention
becomes a desired mechanic; neither is silently implemented as a UI improvement.
No price/refund, automatic healing, resurrection or equipment action is invented.

This decision accepts that some old troops become expensive in space/Food
relative to their current combat profiles. Attachment is supported by honest
history and resource preservation, not an implied future stat reward. The
following situations demonstrate different consequences, not an optimal policy
or proof that every option is equally strong.

## Recorded legal situations

Reproduce the report with:

```sh
mise run scenario veteran-review artifacts/observations/veterans/review.json
mise run capture-veterans
```

The report contains five cases earned through ordinary commands, with seed,
match/wave identity, actual stocks, selected member, profiles, prices and recovery
projections. W2 uses both seeds 1 and 20261018; W8/W16 use the existing seed-1
paid campaign checkpoints. No authoritative state is injected.

| Situation | Existing soldier | Alternative / consequence |
| --- | --- | --- |
| Keep, W2 / seed 20261018 | L1, service 1, full 40 HP, strike 10, upkeep 1 | Current L1 recruit has identical profile and costs 2 Metal. Keeping saves that payment. |
| Wounded same tier, W2 / seed 1 | L1, service 1, 32.5/40 HP | A 2-Metal recruit starts at 40 HP; recovery requires a Town Hall and completed-battle Food eligibility. Wounds create a real difference despite equal equipment. |
| Recover or replace, W8 | Stored L1, service 7, 32.5/44 HP, strike 10 | The existing L3 healing track projects 39.1 HP on the next production. Two eligible payments reach full HP at that rate. A new L4 soldier costs 5 Metal, has 107.8 HP and strike 25; the hall occupies an individual storage place and stored upkeep must still be funded. |
| Upgrade trainer, W8 | Current Barracks L4 | Upgrade quote is 14 Gold + 2 Wood + 4 Stone; only future L5 recruits receive the higher equipment. Their separate recruitment quote is 7 Metal, 146.3 HP and strike 33. Existing soldiers retain tier/wounds. |
| Replace or retain, W16 | L3, service 11, 73/80.3 HP, strike 18 | The L5 recruit costs 7 Metal, has 146.3 HP and strike 33. All field homes are occupied in this checkpoint. Retirement frees one member place, gives no refund, and never automatically recruits a replacement. |
| Not yet recovery-eligible, W8 | New L4, service 0 | It can be stored, but storage alone does not immediately authorize healing. Funded stored upkeep in a completed battle can authorize later recovery; it still gains no service because it never fought. |

Numbers include the actual research/reward effects in each case; they are base
profiles, not predicted damage against every opponent. Food and home capacity can
make an affordable recruitment unavailable. A returned stored soldier pays no new
recruitment fee, but requires actual compatible field space.

## Food, recovery and history are distinct

Core's manifest funds field soldiers first, then stored soldiers. **Stored
soldiers never fight, but consume Food when funded.** Completed-battle funded
members can become recovery-eligible, including stored soldiers. Only living paid
participants in successful clears gain service history. A previously eligible stored soldier can heal
on an upcoming production; storage alone does not create that eligibility.
Future recovery still depends on subsequent Food funding. Healing uses
`ArmyConfiguration.HealedHealth`, clamps to the existing maximum, and changes
neither equipment nor service. Capacity and healing tracks remain separate.

The focused tests caught an incorrect assumption that stored members never pay.
The rations description/preview introduced during #7 is corrected to say **per
funded member, including storage**, matching Core's actual upkeep calculation.
The pinned Core catalog text remains intact; the Godot/Shared presentation
provides this explicit wording correction. The city reward inspection uses the
same correction. No Food or eligibility rule was changed.

## Interface and guards

Roster and inspector call the field service/history rather than XP. The inspector
says that it grants no equipment or stat gain. **Compare recovery / replacement**
shows current profiles, next eligible production HP, capacity/eligibility blocks,
current trainer recruit prices/profiles and the separate trainer-upgrade quote.
Allied comparisons are read-only. The native copy area scrolls; standalone
inspection uses the available width, while the Army pane retains its narrow
contextual layout.

**Review retirement** expands that comparison and reveals Confirm / Keep soldier.
Keep receives native focus, scrolled into view after container layout settles.
Cancellation preserves the member, stock, revision and focus. Confirmation reads
the current context and refuses stale session/match intent or any read-only,
ready, paused, pending or non-preparation context. Closing the inspector or changing selection/session cancels
the review without clearing the selected member on close. Successful confirmation removes exactly the selected soldier, then
restores visible focus. No replacement, refund or recovery command is bundled.
Existing store/return/regroup commands and their Core validation remain intact.

## Verification and limits

Six focused Shared facts cover same-tier profiles on two seeds; real paid
recovery versus its projection; funded stored members versus actual participants;
new stored soldiers' eligibility and zero service; paid replacement with new
identity; no-refund retirement; return preserving wounds/tier/history; full individual storage refusing a move; snapshot
round-trip; and retries without extra payment, healing, production or duplication.
Required Core/Shared checks and native session, artwork, ordinary/debug gameplay
and reconnect coverage are recorded in the issue closure audit.

Native review checks compare without submitting, exercise review by pointer,
Keep by keyboard and Confirm by controller action, verify exact next-production
healing, then test old confirmation signals after closing, selection, phase or session changes. Actual
PNG captures include `veteran-recovery-comparison`, `veteran-replacement-quote` and
`veteran-retirement-review` under `artifacts/captures/veterans/{1280,1920}/` and the
ordinary/debug gameplay capture directories. Logs are
`artifacts/test-logs/roadmap-08-*`; report JSON is under
`artifacts/observations/veterans/`.

Local tests, legal replay observations and actual screenshots are the user's
accepted evidence. They do not establish human attachment, fun, optimal strategy
or long-run balance. Live Steam, other-OS runtime and physical controller/audio
qualification remain unexecuted; the known intermittent native .NET/JIT risk
remains. Source art/audio, Core, pins, locks and preference formats are unchanged.
