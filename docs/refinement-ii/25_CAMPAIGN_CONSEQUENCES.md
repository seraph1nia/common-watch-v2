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
