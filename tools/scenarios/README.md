# Deterministic solo replay

Run from the project root:

```sh
mise run scenario
mise exec -- dotnet run --project tools/scenarios -- draft /tmp/draft.json
```

The CLI accepts a checkpoint and an optional JSON output path. Without a path,
it prints JSON. The default checkpoint is `post-draft`; the seed is `20261018`.
Both graphical clients use the same `ScenarioRunner.CreateAt` replay.

| Checkpoint | Authoritative state |
| --- | --- |
| `initial` | Started game, nine empty settlement plots, five purchased |
| `settlement` | Farm 0, Barracks 1, Metal Mine 2, Lumbermill 3 |
| `building-selected` | Same settlement, selected Barracks 1 |
| `combat` | Four Swordsmen in the shared 63-cell arena, a unit attack after at least 180 ticks |
| `draft` | Wave one won, three actual reward offers |
| `post-draft` | First offer selected, wave two preparation |

The opening spends six starting Wood on Farm/Barracks/Metal Mine, then buys the
Lumbermill using Core's explicit four-Gold recovery payment at zero Wood.
The first two preparation advances each produce four Metal, buying two Swordsmen.
The third advance produces food, pays actual upkeep and starts combat. The runner
steps the real authority until it wins, then selects a real offered reward. It
never writes simulation state, changes rules or fabricates offers.
