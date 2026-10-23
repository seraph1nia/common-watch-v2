# Visual army homes and deployment — issue #5

Historical issue #5 evidence follows. The current interface is the [spatial army workspace](../refinement-ii/19_ARMY_WORKSPACE.md), which replaces the squad selector and twelve home/manage controls with six retained inspection targets and one placement confirmation.

The army uses one retained squad selector across its roster and six-home board.
The board orders the six actual deployment cells by their authoritative row and
column: homes 1–3 in front, 4–6 behind, enemy approach above and Keep behind.
These army homes remain separate from the nine settlement clearings. Select a
squad, then click a legal outlined home to send the existing deployment command.
No battlefield cell ID needs to be read or entered. Manage buttons retain home
purchase and tower construction/upgrade/branch/sale controls.

Each native target shows squad/tower/empty/locked occupancy, member capacity,
full squads, wounds and projected fighting/sitting-out counts. Gold identifies
the current home; eligible alternatives have green outlines. Disabled targets
explain locked land, towers, another squad or the current editing restriction
before submission. A full squad may still move as a whole. Food labels distinguish
stock-now forecasts in preparations 1/2 from final-income forecasts in stage 3.
The board keeps all six targets and all six management actions visible together
at 720p, including allied/pending states. Authority status appears once at the
board header; selected-home management remains scrollable below it.

Recruitment shows its actual destination home and resulting occupancy alongside
the existing price, equipment and Food upkeep. It calls Core's existing placement
query rather than predicting a separate capacity rule. Member regroup/return
uses the same query, excluding the moving member. Selected member identity stays
stable through regrouping/storage/return; the roster follows a member into its
accepted destination squad. Native controls and scroll focus stay retained; completed home purchases restore
visible focus when the quoted Buy action disappears.

The arena preview draws Core's `BattleDeployment.Preview` and structure projection,
including its exact participating roster, formation slots and entry cells. It
uses approved existing unit sprites with the geometric fallback when unavailable.
Stored and unfunded members are excluded from the displayed fighting roster.
Foreground home labels show projected/assigned counts. Watchpost inspection uses
the same reset-health projection as its preparation figure.
A reward must be chosen before Core exposes the next funded deployment preview;
the draft-phase arena states that boundary explicitly rather than showing an
invented deployment. Next-threat text remains available in the reward flow.

## Local evidence and reproduction

`ArmyDeploymentViewTests` replays paid production and accepted commands. It
verifies cost-free relocation, matching persistent assignments and Core entry
cells, locked/tower restrictions, full-squad recruitment rejection, actual Food
and wound counts, return/storage without free healing, and foreign-sector cells
and quotes for two and six players. Core, economy and placement rules are intact.

The actual Godot probe verifies pointer, keyboard and controller deployment,
all twelve home/manage controls fitting together, no submission from disabled or
stale targets, existing purchase costs, recruitment destination/capacity, retained
member/squad after regrouping, and the exact Core-funded preview. Existing native
storage/return checks also assert selection retention. The settings/details
Escape check waits a rendered frame between separate closes so native focus can
settle before the next input.

Actual screenshots under `artifacts/captures/gameplay/forest/` (ordinary angled)
and `artifacts/captures/gameplay/` (debug/plain), with `1920/` subdirectories:

- `six-home-selection`, `six-home-relocated`, `six-home-food-shortage`;
- `member-regroup-destination`, `member-regroup-retained`;
- `home-tower-locked-empty`, `home-purchase-quote`, `home-purchased`,
  `funded-deployment-preview`, `deployment-watchpost-inspection`;
- existing `army-deployment`, `army-roster`, `town-hall` and `arena-preparation`.

The separate host/guest worker completes seven paid waves, then inspects allied
homes without submitting, deploys through the remote home button while authority
acknowledgment is held, and checks the accepted result retains its squad. A rejected
command leaves the same selection and readable rejection inside the board. Both
sizes are captured as `network-{forest,plain}/{allied,pending,rejected}-homes-{1280,1920}.png`.
All private worker settings/session paths and process cleanup remain bounded.

Reproduce with `mise run build`, `test-core`, `test-shared`, `test-session`,
`capture-art` and `capture-gameplay-ui`. To repeat the illustrated host/guest pass:
`COMMON_WATCH_GAMEPLAY_ART=illustrated mise exec -- bash tools/graphical.sh test-gameplay-network`.
Executed successfully: zero-warning build, 46 Core tests, 100 Shared tests,
three native session facts, 14 art views, all four ordinary/debug 720p/1080p
UI passes and both seven-wave cooperative workers. The final worker receipts
include `GODOT_HOME_NETWORK_PASS`; each native UI pass includes
`GODOT_ARMY_DEPLOYMENT_PASS`. Receipts use `artifacts/test-logs/roadmap-05-*`.
The illustrated home-state layout check caught and fixed a duplicate authority
message that clipped management controls by 17 pixels; final bounds and actual
captures pass for allied, pending and rejected states.

The accepted evidence is local tests, legal replays and actual screenshots, not
human comprehension, reaction-time, listening or balance findings. Live Steam,
native Windows/macOS and physical GPU/input/audio remain unexecuted. The known
intermittent native .NET/JIT risk remains unresolved. No approved source artwork,
music, dependency/version/lock or Core file is changed for this presentation work.
