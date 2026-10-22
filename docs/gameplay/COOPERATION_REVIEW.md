# Cooperative awareness and falling cities — issue #10

The ordinary multiplayer HUD now keeps a compact two-column team panel visible
below Settings. Each city has its own Preparing/Ready, Fighting,
Choosing/Selected, Away or Fallen state, Keep HP and field-army count. The heading
adds pause and local reconnect status. Low Keep (at most 30% of configured maximum),
unfunded field members, unfunded storage and an empty field receive explicit
warnings. Food before the final preparation remains a stock-now projection;
hover details explain that later income/purchases can change it. The warning is
prioritized in the compact row; complete funding and contribution details stay
in its tooltip.

Click a city to use the existing allied inspection. This changes only the viewed
city. Ready, pause and all submitted commands retain the authenticated player;
foreign city editing stays blocked. A fallen local player has no Ready button,
gets a visible explanation and a shortcut to a living ally (lowest current Keep
health, then player ID). This is observation, without shared control or
resurrection. Current surviving soldiers may finish that battle, but later waves
exclude the fallen owner's army. The host must remain running for reconnect.
Fallen/terminal veteran inspection no longer advertises future recovery or
replacement purchases; current profiles and service history remain inspectable.

During battle, `@P#` means the observed logical sector occupied by that city's
soldiers. Team tooltip counts and cross-sector squad labels derive from playback
hex cells and the actual shared arena. `UnitState.Destination` is an objective
preference and is deliberately not treated as physical location. Owner rings,
P# identity and command ownership remain unchanged. Combat inspection and the
post-battle receipt move below the team panel, preserving the lower combat legend.
Solo retains its existing layout.

## Legal uneven-group comparison

```sh
mise run scenario cooperation-review artifacts/observations/cooperation/runs.json
mise run capture-cooperation
```

Twenty deterministic runs cover 2–6 players, seeds 1 and 20261018, and 220 actual
battles. Every group has one deliberately idle city. Other cities use either the
existing paid frontline policy or that policy with recruitment capped at three
living soldiers. The limited policy stops at a blocked recruit rather than
inventing a better purchase. All preparation, production, readiness, combat and
reward choices use normal Core commands. No health, resources, enemy allocation
or army state is injected. The report records Keep health, participants, fallen
owners, earned Gold, battle ticks and cross-sector observations per wave.

| Group | Full-policy result (both seeds) | Limited-policy ending waves | First fall → end, ticks (seed 1 / 20261018) |
| --- | --- | --- | --- |
| 2 | Victory, W20 | Defeat W1 / W1 | 1139 / 2640 |
| 3 | Victory, W20 | Defeat W2 / W2 | 2851 / 2287 |
| 4 | Victory, W20 | Defeat W3 / W2 | 2678 / 3660 |
| 5 | Victory, W20 | Defeat W2 / W3 | 5280 / 3834 |
| 6 | Victory, W20 | Defeat W2 / W2 | 2400 / 1289 |

Nine full-policy runs rescue the idle city through the campaign. In the four-city,
seed-1 run that city falls on W18; the other three still win. It receives no
W18–20 clear reward and has zero participation in W19–20. The other cities retain
18 participants each and receive normal 2/2/4 Gold clear rewards. The original
four-city enemy allocation remains in the campaign and is redistributed to the
three standing cities. Thus an actual post-fall victory is possible in this
corpus. The defeated groups end within the same battle or one later wave.

The limited-policy spectator interval is 19–88 seconds of simulation at 60 Hz / 1×,
excluding preparation and including final corpse cleanup. The successful late
last stand lasts 1101 ticks (18.35 simulated combat seconds) after the first fall.
These are replay intervals, not human session durations or proof that a loss was
inevitable at the fall. The 88-second tail remains a tradeoff to revisit. Keep
the current redistribution/failure curve: this bounded corpus does not establish
commonly prolonged inevitable loss, and changing it would weaken the observed
recoverable last stand. No Core rules, rewards, readiness or elimination semantics
change in this issue.

## Native evidence and limits

`capture-cooperation` supervises real ENet clients with private temporary settings
and resume credentials, an owned virtual display, bounded waits and process-group
cleanup. It captures the ordinary illustrated/angled HUD for each 2–6-player
roster at 1280×720 and 1920×1080. Native pointer input selects an ally and presses
Ready at both sizes; only the authenticated city changes readiness. It records
split Preparing/Ready rows and the deliberate wait used to take screenshots.
The two-player branch also checks pause, Leave/Away/private reconnect, a real
low-Keep warning, early elimination, the observer shortcut and terminal state.
The small army and idle city use the same paid policies/default rules as the
replay study. Screenshots and receipts are in `artifacts/captures/cooperation/`;
replay data is in `artifacts/observations/cooperation/runs.json`. These artifacts
are generated locally and gitignored.

Waiting receipts include deliberate screenshot time. Spectator receipts separate
the paused captures from the accelerated active interval; `--test-fast-combat`
advances real steps and is not a normal-play timing result. Local automated
observations and actual screenshots are the user's accepted evidence. They do
not establish social enjoyment, human comprehension, difficulty or balance.
Live Steam, physical controller/audio, Windows/native-platform and human group
qualification remain unexecuted; the repository's intermittent native .NET/JIT
crash risk remains applicable.

## Verification receipt

The final local pass built with zero warnings/errors and passed 46 Core,
135 Shared and 12 animation tests. Three actual session graphical facts, the
1–6-client multiplayer suite, reconnect/fault worker, four ordinary/debug UI
passes at both sizes, the seven-wave cooperative worker (30 actors/11 cues),
14 art views, animation phases and ordered battle sequences passed. The focused
team worker covers all five roster sizes at both target resolutions. Shell/Python
syntax and `git diff --check` pass. Core, approved art/audio and dependency pins
are unchanged. See [verification](../VERIFICATION.md) for the platform limits.
