# Gameplay parity audit and completion checklist

Reference: local `odot-game` at `501a3de7c3f5cf8b5e1726e01dbaefad5ea82f35`,
including the requested gameplay/roguelite/Worldtree/shared-arena/army/battle-flow
docs, original HUD/panels/inspectors and gameplay verification policy/tests. V2's
local working tree was clean at audit. Core remains byte-for-byte unchanged.

“Already complete” in Current means implemented and reachable in the current
Godot client; it does not merely mean the rule exists in Core.

| Original player-facing feature | At audit | Current | Owner / evidence |
| --- | --- | --- | --- |
| Solo, LAN 1–4, authoritative ownership, reconnect | Already complete | Already complete | Preserved session coordinator/RPC path; existing process suites |
| All command parameters and contextual expectations | Partially implemented | Already complete | `GameplayIntent`, `SoloSession`, `GodotSession`; shared/native/remote checks |
| Exact retry with original identity/payload | Partially implemented | Already complete | Immutable local command, retained remote JSON; no duplicate spending |
| Nine settlement plots, selection, purchase, upgrade, sale | Partially implemented | Already complete | `BuildingInspector`; paid refunds/generation checks |
| Full categorized building catalog and quotes | Missing | Already complete | `ConstructionPanel`; all 14 catalog entries built/upgraded/sold in replay |
| Explicit zero-wood Lumbermill gold recovery | Already complete | Already complete | Preserved payment choice and paid opening |
| Six-resource stocks, synchronized income and upkeep | Partially implemented | Already complete | `ResourcePanel`; final-production/receipt/partial-funding checks |
| Market resources, bundle counts, food-sale preview | Missing | Already complete | `MarketPanel`; authoritative rates and accepted/rejected remote trades |
| Four recruits and trainer equipment levels | Partially implemented | Already complete | `RecruitmentPanel`; all four classes in legal mixed-army scenario |
| Persistent roster, squad inspection, reinforcement/regroup | Missing | Already complete | `ArmyRosterPanel`, shared `UnitInspector`; stable identity/XP/wounds |
| Six distinct purchased army homes and deployment | Missing | Already complete | `DeploymentPanel`; numbered land, squad/cell command and tower exclusion |
| Town Hall storage, paid recovery and separate upgrades | Missing | Already complete | `TownHallPanel`; stored veterans, return/store actions, independent tracks |
| Defense towers, branches, footprints, capabilities | Missing | Already complete | Plot/home inspectors; both branch lifecycles; canonical run-modified profiles |
| All Worldtree technologies and locked alternatives | Missing | Already complete | `ResearchTreePanel`; all 18 technologies purchased across six legal paths |
| Next-wave and shared-arena preparation inspection | Missing | Already complete | `ArenaPreview`, `BattleInspectionPanel`; no live preview actors |
| Unit, structure, capabilities and status inspection | Missing | Already complete | One `UnitInspector` for roster, forecast and battlefield picking |
| Ready/unready, pause/resume, original-host speed | Partially implemented | Already complete | `MatchControls`; native/host/guest authorization checks |
| Six explained doctrines, three actual reward offers | Partially implemented | Already complete | Lobby and retained draft cards; rarity/effects/stacks/eligibility |
| Acquired progression and inspection while drafting | Missing | Already complete | `RunProgressionPanel`; dismiss/reopen same offers without choosing |
| Twenty waves, production boundaries, Victory/Defeat | Partially implemented | Already complete | Paid default-rule wave-20 Victory with 60 productions; real defeat |
| Allied city, research, army and building inspection | Missing | Already complete | `CitySelector`; viewed identity separate from command identity; actual LAN capture |
| Native focus, camera, settings, audio, optional art | Already complete | Already complete | Preserved; modal/scroll/focus checks at 720p/1080p, original art/audio suites |
| Original 3D, artwork sockets, exact HUD/theme | Intentionally out of scope | Intentionally out of scope | Explicit user exclusions; no game artwork created |
| New mechanics, campaign saves, matchmaking, host migration | Intentionally out of scope | Intentionally out of scope | Absent from reference and explicitly excluded |

## Implementation gates

- Command infrastructure: complete argument shape for solo, playing host and remote
  delivery. `Command.FromSnapshot` captures all authoritative expectations once;
  retries preserve match, sequence, phase/turn, generation and purchase/track counts.
  `GameplayIntent` never accepts protocol identity/expectations from UI input.
- Economy: native catalog categories, missing-resource/producer explanations,
  construction, land, upgrades, actual refunds, market bundles and recovery.
- Army/defense: four trainers/roles, persistent individuals, compatible/new squads,
  regroup, retirement, homes, deployment, tower branches and Town Hall rotation.
- Research: synchronized points/progress, canonical IDs/effects, all nodes,
  prerequisites, exclusive choices, acquired and read-only states.
- Forecast/inspection: accepted `NextWave`, food participants/unfunded/stored split,
  Keep/Watchpost/towers, ordinary battle actors, status potency/duration/timing.
- Match/progression: three actual preparation settlements, readiness/pause/speed,
  explained doctrines/cards, acquired build, draft inspection and terminal summary.
- Cooperative presentation: foreign/fallen/away cities are inspectable, selection
  clears on city/session changes and commands retain authenticated local identity.

No remaining required gameplay-action gap is known from this inventory. This is
functional parity with a simple native interface, not original visual fidelity.
Windows/macOS runtime and live Steam account qualification remain unexecuted;
passing LAN/offline tests does not close those platform gates. The known intermittent
native .NET/JIT crash risk remains unresolved. See [verification](VERIFICATION.md)
and [playing guide](PLAYING.md) for results, captures and workflow commands.
