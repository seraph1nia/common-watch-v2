> Historical first-pass review. The selected ordinary-payment flow and broader alternatives are documented in [Refinement II #23](../refinement-ii/23_NORMAL_OPENINGS.md). Its normal starts do not require Gold recovery.

# Opening economy and first-battle learning — issue #6

The first three waves have an optional contextual guide in the top HUD band.
It explains production, recruitment, Food participation, the six-home board and
reviewing the first result. Its links navigate existing controls; they never
build, spend, recruit or ready the city. Collapse is retained across session
replacement for the current application launch. Ready and advanced controls
remain available without completing an objective or reading the guide.

Doctrine cards lead with a short playstyle and an immediate resource implication.
**Full effects & stock** exposes the complete canonical benefits, drawbacks and
starting allocation in the native scroll area. Opening details does not select a
doctrine. Selecting and starting retain the existing explicit-choice rules.

Construction at zero Wood points to **Production → Lumbermill Gold recovery**.
The existing quote is four Gold; confirmation still spends it. Disabled building
cards include producer-specific shortage explanations. Recruitment explains that
it must happen before the last Ready: final income can fund Food, but cannot buy
additional troops before combat. The forecast's stock-now/final-income basis
continues to come from accepted snapshots. The first result links actual Food
payment and participants to the existing factual battle/preparation report.

## Reviewed paths and resource constraints

These are concrete reproducible options, not forced orders or optimal strategies.
Starting allocations and all costs come from the pinned Core catalog:

| Doctrine | Starting resources | First-army constraint and path |
| --- | --- | --- |
| Warden | 10 Gold, 6 Wood, 2 Stone | Reserve four Gold for recovery; the sturdier Watchpost supports a four-Swordsman opening. |
| Merchant | 16 Gold, 4 Wood | Build Metal Mine and Barracks, then the Gold-recovery Lumbermill. Add the Farm in preparation 3, before final income/Food; early stock-only forecasts correctly show no Food. |
| Warmaster | 12 Gold, 6 Wood, 4 Metal, 6 Food | Melee recruitment costs extra Metal. The starting Metal plus two pre-battle Metal payments pays for four Swordsmen. |
| Arcanist | 9 Gold, 6 Wood, 3 Stone, 3 Cloth; 1 Research | Keep Gold for recovery and the Arcanum. Three Swordsmen occupy one home; use the Cloth/Stone grant for one Mage in the second home. |
| Grovekeeper | 12 Gold, 8 Wood | Normal Wood payment covers the first Lumbermill. Add a second Metal Mine after the first income to pay the recruitment surcharge; Farm/Lumbermill doctrine income supports growth. |
| Huntsman | 12 Gold, 6 Wood, 2 Metal | Use a four-Swordsman bridge. Extra Wood production, an Archery Range and another home support Crossbowmen in waves 2/3; do not assume final income can buy a last-minute bow. |

For Warden/Warmaster/Huntsman, the tested first preparation builds Farm, Metal
Mine and Barracks (six Wood), followed by the four-Gold recovery Lumbermill.
Recruit twice after the first income and twice after the second. Merchant instead
starts Metal Mine/Barracks/recovery Lumbermill and builds its Farm after the second
income; the final Farm production funds the four fighters. Grovekeeper builds its
first Lumbermill normally and adds the extra Metal producer before the second
income. Arcanist spends two saved Wood, three Stone and five Gold on its Arcanum
in preparation 3, then recruits the Mage with the starting Cloth and one Gold.

Later review decisions use the existing paid producer/equipment/home controls.
Arcanist and Grovekeeper earn a Stonecutter on another settlement clearing rather
than overwriting the Arcanum/second Metal Mine. Huntsman adds a Lumbermill, Archery
Range and Stonecutter and reinforces six melee troops before building its ranged
squad. Each additional home/clearing uses the current escalating purchase quote.
The complete accepted action trail and stock receipts are in the reports below.

## Observed first-three-wave results

For seed `20261018`, these runs reached all three nonfinal rewards with no Keep
damage or unfunded soldiers. Numbers are funded participants before each battle:

| Doctrine | Wave 1 | Wave 2 | Wave 3 | Food paid (1 / 2 / 3) | Losses (1 / 2 / 3) |
| --- | --- | --- | --- | --- | --- |
| Warden | 4 Swordsmen | 8 Swordsmen | 12 Swordsmen | 4 / 8 / 12 | 0 / 2 / 1 |
| Merchant | 4 Swordsmen | 9 Swordsmen | 12 Swordsmen | 4 / 9 / 12 | 0 / 1 / 3 |
| Warmaster | 4 Swordsmen | 7 Swordsmen | 9 Swordsmen | 4 / 7 / 9 | 0 / 2 / 0 |
| Arcanist | 3 Swordsmen, 1 Mage | 6 Swordsmen, 1 Mage | 6 Swordsmen, 1 Mage | 5 / 8 / 8 | 0 / 3 / 0 |
| Grovekeeper | 4 Swordsmen | 10 Swordsmen | 12 Swordsmen | 4 / 10 / 12 | 0 / 0 / 3 |
| Huntsman | 4 Swordsmen | 6 Swordsmen, 1 Crossbowman | 6 Swordsmen, 3 Crossbowmen | 4 / 7 / 9 | 0 / 2 / 1 |

The headless report and both actual native-window reports agree on these receipts.
The Shared contracts also run every doctrine on seed 1 and require all three
rewards and funded armies. This is scripted evidence about those paths. It does
not measure novice comprehension, reaction time, human confusion, fun, balance
or the viability of every alternative opening.

No starting resource, cost, income, doctrine effect or later economy rule is
adjusted. The seven-Wood construction total versus six baseline starting Wood is
a real constraint with an existing recovery route; that arithmetic alone does not
establish that normal openings are unwinnable. The six successful reviewed paths
support explaining current options first. Any future balance change needs its
own cross-doctrine and later-economy comparison; it is not justified by these
scripts. The existing legal wave-20 Victory contracts remain part of Shared tests.

## Evidence and reproduction

- `mise run capture-opening`: six doctrines through three waves using actual
  doctrine cards, construction/confirmation, recruitment, purchases, upgrades,
  trades, Ready and reward controls, at 1280×720 and 1920×1080 with current defaults.
- `mise exec -- dotnet run --project tools/scenarios -- opening-review artifacts/observations/opening/review.json`:
  headless report including starting stock, match/seed, paid action trail and outcomes.
- `mise run test-core`, `test-shared`, `test-session`, `capture-settlement`,
  `capture-art` and `capture-gameplay-ui`: unchanged rules, UI/lifetime/focus,
  five-size nine-clearing native picking and ordinary/debug/cooperative regressions.

Actual PNGs and JSON are under `artifacts/captures/opening/{1280,1920}/`:
`opening-guide-*`, `opening-recovery-location`, `<doctrine>-doctrine-playstyle`,
`<doctrine>-doctrine-full-effects`, `<doctrine>-wave-{1,2,3}-funded-preparation`,
`<doctrine>-wave-{1,2,3}-outcome` and `opening-review.json`.
Executed successfully: zero-warning build, 46 Core tests, 108 Shared tests,
three native session facts, 14 art views, five-size nine-clearing picking,
all four ordinary/debug UI passes and both seven-wave cooperative workers.
Both opening-review windows completed six doctrines and 18 waves each through
ordinary production controls. Transcripts use `artifacts/test-logs/roadmap-06-*`.

Native review caught and fixed two layout problems: the initial lower-left guide
covered a clearing at 1280×800, and the new shortage hint clipped the recovery
card. The top HUD band preserves clearing access; the shorter hint and reserved
catalog row keep recovery, confirmation and all production cards visible.

The native probe checks collapse without a command or readiness gate, retained
collapse preference, relevant recovery navigation without spending, funded roster
agreement with the final Ready receipt, optional home navigation and the Food /
participant link in the first factual result. It verifies complete basic doctrine
copy and both actions fit before scrolling, and that canonical numeric detail
remains inspectable without selecting. Legal match IDs choose real offered cards;
no authoritative state or resources are injected.

Local tests, legal replays and actual Linux Xvfb/Mesa screenshots are the accepted
roadmap evidence. Human first-time playtest conclusions are unmeasured. Live
Steam, native Windows/macOS and physical GPU/input/audio remain unexecuted; the
known intermittent native .NET/JIT crash risk remains. Core, approved source
art/music, preferences format, dependency pins and lock files are preserved.
