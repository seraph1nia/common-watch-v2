namespace Game.Core;

// Presentation policy, shared by every transport; never advances game rules.
public sealed class CombatPlayback
{
    private WorkCounters? _work;
    public WorkCounters? Work
    {
        get => _work;
        set { _work = value; value?.Support(WorkMetric.PlaybackIndexBuilds, WorkMetric.PlaybackEventVisits, WorkMetric.Sorts, WorkMetric.SortElements); }
    }
    private sealed record AcceptedState(string MatchId, long Revision, long Tick, bool Paused, double Pace, UnitState[] Units, Dictionary<int, UnitState> ById, Dictionary<int, long> RallyUntil, StructureState[] Structures);
    private AcceptedState? _current;
    private readonly List<AcceptedState> _frames = [];
    private int[] _ids = [];
    private readonly Dictionary<int, int> _pendingDeaths = [];
    private readonly Queue<CombatEvent> _pending = new();
    private long _acceptedSequence;
    public long EventCursor { get; private set; }
    public double Tick { get; private set; }
    public double VisualSeconds { get; private set; }
    public int Generation { get; private set; }
    public bool Accept(MatchSnapshot state, bool baseline = false)
    {
        bool fresh = _current?.MatchId != state.MatchId;
        if (!fresh && !baseline && state.Revision <= _current!.Revision) return false;
        bool gap = !fresh && state.OldestEventSequence > _acceptedSequence + 1;
        if (fresh || baseline || gap)
        {
            _pending.Clear(); _pendingDeaths.Clear(); _frames.Clear(); _current = Index(state);
            _frames.Add(_current); _ids = _current.Units.Select(u => u.Id).ToArray();
            EventCursor = _acceptedSequence = state.EventSequence; Tick = state.Tick;
            if (fresh) VisualSeconds = 0;
            Generation++;
            return true;
        }
        Tick = state.Paused ? state.Tick : Math.Max(Tick, state.Tick - 3);
        _current = Index(state);
        if (_frames[^1].Tick == state.Tick) _frames[^1] = _current;
        else _frames.Add(_current);
        TrimFrames();
        _ids = WorkOrdering.Input(_frames.SelectMany(frame => frame.ById.Keys).Distinct(), _work).Order().ToArray();
        _work?.Add(WorkMetric.PlaybackEventVisits, state.CombatEvents.Length);
        foreach (CombatEvent entry in WorkOrdering.Input(state.CombatEvents.Where(e => e.Sequence > _acceptedSequence), _work).OrderBy(e => e.Sequence))
        {
            var owned = entry with { Unit = entry.Unit is null ? null : CombatProjection.Detach(entry.Unit), Victims = entry.Victims.ToArray() };
            _pending.Enqueue(owned);
            if (owned.Type == CombatEventType.Death && owned.Unit is { } dead) _pendingDeaths[dead.Id] = _pendingDeaths.GetValueOrDefault(dead.Id) + 1;
        }
        _acceptedSequence = state.EventSequence;
        return true;
    }
    public void Advance(double seconds, bool connected)
    {
        if (!connected || _current is null || _current.Paused) return;
        double elapsed = Math.Clamp(seconds, 0, 0.1) * _current.Pace;
        VisualSeconds += elapsed;
        Tick = Math.Min(_current.Tick, Tick + elapsed * Match.StepsPerSecond);
        TrimFrames();
    }
    private void TrimFrames()
    {
        int matured = 0;
        while (matured + 1 < _frames.Count && _frames[matured + 1].Tick <= Tick) matured++;
        if (matured == 0) return;
        _frames.RemoveRange(0, matured);
        _ids = _ids.Where(id => _frames.Any(frame => frame.ById.ContainsKey(id))).ToArray();
    }
    public CombatEvent[] Drain()
    {
        var events = new List<CombatEvent>();
        while (_pending.TryPeek(out CombatEvent? entry) && entry.Tick <= Tick)
        {
            _work?.Add(WorkMetric.PlaybackEventVisits); events.Add(_pending.Dequeue()); EventCursor = entry.Sequence;
            if (entry.Type == CombatEventType.Death && entry.Unit is { } dead)
            {
                if (_pendingDeaths[dead.Id] == 1) _pendingDeaths.Remove(dead.Id);
                else _pendingDeaths[dead.Id]--;
            }
        }
        return events.ToArray();
    }
    public UnitState[] Units()
    {
        if (_current is null) return [];
        return HexUnits();
    }
    public IReadOnlyDictionary<int, long> RallyDisruptions()
    {
        var sampled = _current is null ? new Dictionary<int, long>() : new Dictionary<int, long>(_current.RallyUntil);
        foreach (CombatEvent entry in _pending.Reverse())
            if (entry.Tick > Tick)
            {
                if (entry.Type == CombatEventType.RallyRestored) sampled[entry.SquadId] = entry.Tick;
                else if (entry.Type == CombatEventType.RallyDisrupted) sampled[entry.SquadId] = 0;
            }
        return sampled;
    }
    public StructureState[] Structures()
    {
        if (_current is null) return [];
        return _current.Structures.Select(structure =>
        {
            int health = structure.Health;
            long destroyed = structure.DestroyedTick;
            foreach (CombatEvent entry in _pending.Reverse())
                if (entry.Tick > Tick && entry.Structure is { } hit && hit.Owner == structure.Owner && hit.Kind == structure.Kind && hit.Slot == structure.Slot
                    && entry.Type is CombatEventType.StructureHit or CombatEventType.StructureDestroyed)
                { health += entry.Damage; destroyed = 0; }
            bool awaitingElimination = _pending.Any(e => e.Tick > Tick && e.Type == CombatEventType.StructureDestroyed
                && e.Structure is { Kind: ArenaStructureKind.Keep } keep && keep.Owner == structure.Owner);
            bool active = awaitingElimination ? _frames[0].Structures.FirstOrDefault(s => s.Owner == structure.Owner && s.Kind == structure.Kind && s.Slot == structure.Slot)?.Active ?? structure.Active : structure.Active;
            return structure with { Health = health, DestroyedTick = destroyed, Active = active };
        }).ToArray();
    }
    private UnitState[] HexUnits()
    {
        var sampled = new List<UnitState>(_ids.Length);
        var futureDamage = new Dictionary<int, int>();
        foreach (CombatEvent entry in _pending)
            if (entry.Tick > Tick && entry.Type == CombatEventType.Hit && entry.Unit is { } hit)
                futureDamage[hit.Id] = futureDamage.GetValueOrDefault(hit.Id) + entry.Damage;
        foreach (int id in _ids)
        {
            UnitState? selected = null;
            foreach (AcceptedState frame in _frames)
            {
                if (!frame.ById.TryGetValue(id, out UnitState? unit))
                {
                    if (selected?.Hex is not { Lifecycle: UnitLifecycle.Dying } priorDeath || Tick >= priorDeath.DeathEndTick) selected = null;
                    continue;
                }
                HexUnitState? hex = unit.Hex;
                if (selected?.Hex is { } prior && hex is not null
                    && (hex.Lifecycle == UnitLifecycle.Dying && Tick < hex.DeathStartTick
                        || hex.Lifecycle == UnitLifecycle.Alive && prior.Lifecycle == UnitLifecycle.Queued && Tick < hex.AdmittedTick
                        || prior.HoldsTransit && Tick < prior.EndTick && (hex.Action != UnitActionKind.Moving || hex.StartTick > Tick)
                        || hex.StartTick > Tick && hex.Lifecycle != UnitLifecycle.Dying)) continue;
                selected = unit;
            }
            if (selected is not null && (selected.Hex is not { Lifecycle: UnitLifecycle.Dying } death || Tick < death.DeathEndTick))
            {
                // Snapshots can arrive ahead of presentation. Undo only future
                // authoritative hits, using their effective damage, so a bar
                // and its rig agree even before a lethal boundary is sampled.
                UnitState? latest = _current!.ById.GetValueOrDefault(id);
                int damage = futureDamage.GetValueOrDefault(id);
                int health = latest is null || damage == 0 && latest.Hex?.Lifecycle == UnitLifecycle.Dying && selected.Hex?.Lifecycle != UnitLifecycle.Dying
                    ? selected.Health : latest.Health + damage;
                sampled.Add(CombatProjection.Detach(selected with { Health = health }));
            }
        }
        return sampled.ToArray();
    }
    private AcceptedState Index(MatchSnapshot state)
    {
        _work?.Add(WorkMetric.PlaybackIndexBuilds);
        UnitState[] ordered = WorkOrdering.Input(state.Players.SelectMany(p => p.Soldiers).Concat(state.Enemies).Concat(state.DyingBodies), _work)
            .OrderBy(u => u.Id).Select(u => CombatProjection.Detach(u)).ToArray();
        return new(state.MatchId, state.Revision, state.Tick, state.Paused,
            state.Phase == Phase.Combat ? CombatSpeedRates.Factor(state.CombatSpeed) : 1, ordered, ordered.ToDictionary(u => u.Id),
            state.Squads.ToDictionary(s => s.Id, s => s.RallyDisruptedUntil), state.Structures.ToArray());
    }
    public bool AwaitingDeath(int id) => _pendingDeaths.ContainsKey(id);
    public static double DeathPose(UnitState unit, double tick, double clipLength)
    {
        HexUnitState hex = unit.Hex ?? throw new ArgumentException("Death sampling requires authoritative hex state.", nameof(unit));
        return Math.Clamp((tick - hex.DeathStartTick) / Math.Max(1, hex.DeathEndTick - hex.DeathStartTick), 0, 1) * clipLength;
    }
    public static UnitState[] All(MatchSnapshot state, WorkCounters? work = null)
    {
        work?.Add(WorkMetric.Sorts); work?.Add(WorkMetric.SortElements, state.Players.Sum(p => p.Soldiers.Length) + state.Enemies.Length);
        return state.Players.SelectMany(p => p.Soldiers).Concat(state.Enemies).OrderBy(u => u.Id).ToArray();
    }
    // Imported clip sampling: axe hand descends at frame 23, cast hand reaches
    // maximum forward extension at frame 8 (30 fps); sword/shot retain their markers.
    public static double ImpactMarker(UnitType type) => type switch
    { UnitType.Crossbowman => .43, UnitType.Berserker => 23 / 30.0, UnitType.Mage => 8 / 30.0, _ => .40 };
    public static double AttackPose(UnitState unit, double tick, double clipLength)
    {
        double marker = ImpactMarker(unit.Type);
        double pose = tick <= unit.ImpactTick ? marker * (tick - unit.ActionStartTick) / Math.Max(1, unit.ImpactTick - unit.ActionStartTick)
            : marker + (clipLength - marker) * (tick - unit.ImpactTick) / Math.Max(1, unit.ReadyTick - unit.ImpactTick);
        return Math.Clamp(pose, 0, clipLength);
    }
}
