using Arch.Core;
using EcsWorld = Arch.Core.World;

namespace Game.Core;

// ECS owns persistent individual health. Squads alone own combat decisions,
// movement and cycles. There is one arena; Destination is an objective preference.
internal sealed partial class CombatSimulation : IDisposable
{
    public const int HistoryTicks = 120, HistoryLimit = 4096;
    private readonly CombatConfiguration _configuration;
    private readonly ulong _seed;
    private readonly EcsWorld _world;
    private readonly Dictionary<int, Entity> _entities = [];
    private readonly SortedDictionary<int, CombatSquad> _squads = [];
    private readonly List<CombatEvent> _events = [];
    private readonly SortedSet<int> _healthProgress = [];
    private readonly HashSet<int> _eliminatedOwners = [];
    private readonly SortedDictionary<int, StructureState> _watchposts = [];
    private readonly QueryDescription _units = new QueryDescription().WithAll<CombatUnit>();
    private SharedArena _arena = new([1]);
    private int _nextId = 1, _nextSquadId = 1, _wave = 1;
    private long _tick;
    private WorkCounters? _work;
    private int _routeSearches;
    public CombatSimulation(Rules rules, CombatConfiguration? configuration = null, ulong seed = 0)
    { _configuration = configuration ?? new(rules); _seed = seed; _world = EcsWorld.Create(); }
    internal WorkCounters? Work
    {
        get => _work;
        set
        {
            _work = value; _configuration.Work = value;
            value?.Support(WorkMetric.WorldViews, WorkMetric.UnitVisits, WorkMetric.UnitProjections, WorkMetric.RouteElementsCopied, WorkMetric.VisitedElementsCopied,
                WorkMetric.BfsSearches, WorkMetric.BfsDequeues, WorkMetric.BfsEdges);
        }
    }
    public long EventSequence { get; private set; }
    public bool IsDisposed { get; private set; }
    public bool HasDeaths => _entities.Values.Any(e => _world.Get<CombatUnit>(e).Location.Lifecycle == UnitLifecycle.Dying);
    internal bool RegistryReleased => !ReferenceEquals(EcsWorld.Worlds[_world.Id], _world);
    internal HexBoard Board => _arena.Board;
    internal ArenaState Arena => _arena.Snapshot();
    internal int[] HealthProgressCities => _healthProgress.ToArray();
    internal int RouteSearches => _routeSearches;
    internal CombatReservations Reservations => new(_squads.Values.Where(s => s.Alive)
        .Select(s => new PositionReservation(1, s.Location.Position.Cell, 1, s.Id, s.Action.Sequence, s.Faction, 1)).ToArray(),
        _squads.Values.Where(s => s.Alive && s.Action is CombatAction.Moving).Select(s =>
        {
            var move = (CombatAction.Moving)s.Action;
            return new TransitReservation(1, s.Location.Position.Cell, move.Destination.Cell,
                _arena.Board.Transition(s.Location.Position, move.Destination).EdgeToken, s.Id, s.Action.Sequence);
        }).ToArray());
    public WeaponProfile Profile(UnitType type, int rank = 0, bool isBoss = false, int level = 1, Faction faction = Faction.Adventurers)
        => _configuration.Unit(type, rank, isBoss, level, faction: faction).Runtime();
    private ref CombatUnit Unit(int id) => ref _world.Get<CombatUnit>(_entities[id]);
    internal CombatUnit Inspect(int id) => ProjectMember(Unit(id));
    internal CombatUnit[] Units() => All().Select(ProjectMember).ToArray();
    private CombatDecisionKey Key(int id, int city, long sequence, int generation, CombatPurpose purpose, CombatActorKind kind = CombatActorKind.Unit)
        => new(_seed, _configuration.ActorDecisionFingerprint, _wave, city, kind, id, sequence, generation, purpose);
    private CombatDecisionState NewDecision(int id, int city, long sequence)
        => new(sequence, 0, SeededDecision.Rank(Key(id, city, sequence, 0, CombatPurpose.MovementRank)));
    internal void ConfigureArena(IEnumerable<int> owners, IEnumerable<City>? cities = null)
    {
        if (!HasDeaths) _arena = new(owners, ArmyWeapons(cities ?? []));
        RestoreHomes();
    }
    private static IEnumerable<(int Owner, int Slot)> ArmyWeapons(IEnumerable<City> cities)
        => cities.SelectMany(city => city.Towers.Keys.Where(ArmyConfiguration.IsArmyPlot).Select(slot => (city.Id, slot)));
    private static int HomeIndex(ArmyAssignment assignment)
    {
        if (assignment.Tile is < 0 or > 5) throw new ArgumentException("Legacy or invalid roster home refused.", nameof(assignment));
        return assignment.Tile;
    }
    private CombatSquad NewSquad(int id, CombatUnit member)
        => new(id, member.Owner, member.Destination, member.Faction, member.Type, member.Identity.IsBoss,
            new(UnitLifecycle.Queued, default), new CombatAction.Waiting(), NewDecision(id, member.Destination, 0));
    private int FindSquad(CombatUnit member)
    {
        CombatUnit[] living = Living();
        CombatUnit? compatible = living.FirstOrDefault(u => u.Faction == member.Faction && u.Owner == member.Owner && u.Type == member.Type
            && !u.Identity.IsBoss && !member.Identity.IsBoss && u.SquadId > 0 && _squads.ContainsKey(u.SquadId)
            && (member.Faction == Faction.Adventurers ? member.Assignment is { Stored: false } && u.Assignment == member.Assignment
                : BattleDeployment.CompatibleEnemy(member.Type, member.Origin, member.Destination, member.Identity.IsBoss,
                    u.Type, u.Origin, u.Destination, u.Identity.IsBoss) && _squads[u.SquadId].Location.Lifecycle == UnitLifecycle.Queued)
            && living.Count(m => m.SquadId == u.SquadId) < _configuration.Arena.SquadCapacity);
        if (compatible is not null) return compatible.SquadId;
        int id = _nextSquadId++; _squads.Add(id, NewSquad(id, member)); return id;
    }
    public int Create(UnitType type, int owner, int origin, int destination, Faction faction = Faction.Adventurers, int rank = 0, bool isBoss = false,
        int level = 1, UnitCapabilities capabilities = default, ArmyAssignment? assignment = null)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (destination <= 0 || origin <= 0 || !Enum.IsDefined(faction)) throw new ArgumentException("Invalid member identity.");
        WeaponProfile profile = _configuration.Unit(type, rank, isBoss, level, capabilities, faction).Runtime();
        int id = _nextId; _nextId = checked(_nextId + 1);
        var member = new CombatUnit(new(id, type, owner, origin, destination, faction, rank, isBoss, level), profile.Health, profile,
            new(assignment is { Stored: true } ? UnitLifecycle.Stored : UnitLifecycle.Queued, default), new CombatAction.Waiting(), NewDecision(id, destination, 0))
        { Capabilities = capabilities, Assignment = assignment };
        int squad = FindSquad(member);
        int[] slots = Living().Where(u => u.SquadId == squad).Select(u => u.FormationSlot).ToArray();
        member = member with { SquadId = squad, FormationSlot = Enumerable.Range(0, isBoss ? 1 : _configuration.Arena.SquadCapacity).First(slot => !slots.Contains(slot)) };
        _entities.Add(id, _world.Create(member)); _memberOwners[id] = faction == Faction.Adventurers ? owner : 0; return id;
    }
    private readonly Dictionary<int, int> _memberOwners = [];
    private readonly Dictionary<int, City> _owners = [];
    public void Research(int city, ResearchState research, ResearchCatalog catalog, RunState? run = null)
    {
        var updates = Living().Where(u => u.Owner == city && u.Faction == Faction.Adventurers).Select(unit =>
        {
            UnitCapabilities capabilities = RunEffects.Capabilities(catalog.Capabilities(research, unit.Type), run, unit.Type);
            WeaponProfile profile = _configuration.Unit(unit.Type, unit.Identity.Rank, unit.Identity.IsBoss, unit.Identity.Level, capabilities).Runtime();
            int health = Math.Min(unit.Health, profile.Health);
            return unit with { Health = health, Capabilities = capabilities, Profile = profile };
        }).ToArray();
        foreach (CombatUnit unit in updates) Unit(unit.Id) = unit;
    }
    private CombatUnit[] All()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        _work?.Add(WorkMetric.WorldViews); _work?.Add(WorkMetric.UnitVisits, _entities.Count);
        var values = new List<CombatUnit>(_entities.Count); _world.Query(in _units, (ref CombatUnit unit) => values.Add(unit));
        return values.OrderBy(u => u.Id).ToArray();
    }
    private CombatUnit[] Living() => All().Where(u => u.Health > 0 && u.Location.Lifecycle != UnitLifecycle.Dying).ToArray();
    private CombatUnit[] Members(CombatSquad squad) => Living().Where(u => u.SquadId == squad.Id && u.Location.Lifecycle == UnitLifecycle.Alive).ToArray();
    internal CombatUnit[] EnemyMembership() => Living().Where(u => u.Faction == Faction.Skeletons).Select(ProjectMember).ToArray();
    internal CombatUnit[] SoldierMembership(int city) => Living().Where(u => u.Faction == Faction.Adventurers && u.Owner == city).Select(ProjectMember).ToArray();
    internal bool HasParticipatingSoldierLevel(int level) => Living().Any(u => u.Faction == Faction.Adventurers && u.Location.Lifecycle == UnitLifecycle.Alive && u.Identity.Level == level);
    private CombatUnit ProjectMember(CombatUnit member)
    {
        if (member.Location.Lifecycle == UnitLifecycle.Dying || !_squads.TryGetValue(member.SquadId, out CombatSquad? squad)) return member;
        HexPosePoint? aim = squad.Location.FrozenTick is null ? squad.Location.FrozenAim : member.Location.FrozenAim;
        if (squad.Location.FrozenTick is null && member.StrikeAim is { } observed && observed.AttackSequence == squad.Action.AttackSequence
            && squad.Action.Attack?.Target is { Kind: CombatTargetKind.Unit } target && observed.SquadId == target.Id
            && (!_entities.TryGetValue(observed.MemberId, out Entity aimed) || _world.Get<CombatUnit>(aimed).Health <= 0)) aim = observed.Pose;
        // Live targets already have received poses. Do not repeat an unused
        // FrozenAim in every event/member payload and inflate bounded history.
        return member with
        {
            Location = squad.Location with
            {
                Lifecycle = member.Location.Lifecycle,
                FrozenAim = aim
            },
            Action = squad.Action,
            Decision = squad.Decision
        };
    }
    private UnitState Project(CombatUnit member)
    {
        CombatUnit projection = ProjectMember(member);
        UnitState state = CombatProjection.Snapshot(projection, _tick, _work);
        if (projection.Action.Attack?.Target is not { } target) return state;
        int primary = target.Kind != CombatTargetKind.Unit ? 0 : member.StrikeAim?.AttackSequence == projection.Action.AttackSequence && member.StrikeAim.SquadId == target.Id ? member.StrikeAim.MemberId
            : _squads.TryGetValue(target.Id, out CombatSquad? defender) ? Members(defender).FirstOrDefault()?.Id ?? 0 : 0;
        return state with
        {
            TargetId = target.Kind == CombatTargetKind.Unit ? primary : target.Id,
            TargetCity = target.Kind != CombatTargetKind.Unit,
            TargetKind = target.Kind switch { CombatTargetKind.Unit => SquadTargetKind.Squad, CombatTargetKind.City => SquadTargetKind.Keep, _ => SquadTargetKind.Watchpost }
        };
    }
    public UnitState[] Snapshot() => Living().Select(Project).ToArray();
    public UnitState[] Dying() => All().Where(u => u.Location.Lifecycle == UnitLifecycle.Dying).Select(Project).ToArray();
    public UnitState[] Soldiers(int city) => SoldierMembership(city).Select(Project).ToArray();
    public UnitState[] Enemies() => EnemyMembership().Select(Project).ToArray();
    internal (UnitState[] Living, UnitState[] Dying, SquadState[] Squads) ProjectAll()
    {
        CombatUnit[] all = All();
        return (all.Where(u => u.Health > 0).Select(Project).ToArray(), all.Where(u => u.Location.Lifecycle == UnitLifecycle.Dying).Select(Project).ToArray(), SquadSnapshots(all));
    }
    internal SquadState[] SquadSnapshots() => SquadSnapshots(All());
    private SquadState[] SquadSnapshots(CombatUnit[] all) => _squads.Values.Select(s =>
    {
        CombatUnit[] members = all.Where(u => u.Health > 0 && u.Location.Lifecycle != UnitLifecycle.Dying && u.SquadId == s.Id).ToArray();
        CombatUnit projection = members.FirstOrDefault() ?? all.First(u => u.SquadId == s.Id);
        UnitState pose = CombatProjection.Snapshot(projection with { Location = s.Location, Action = s.Action, Decision = s.Decision }, _tick);
        CombatTarget? target = s.Action.Attack?.Target;
        return new SquadState(s.Id, s.Owner, s.Destination, s.Faction, s.Type, s.IsBoss, members.Select(u => u.Id).ToArray(),
            members.Where(u => u.Location.Lifecycle == UnitLifecycle.Alive).Select(u => u.Id).ToArray(), pose.Hex! with { Id = s.Id, City = 1 }, s.Decision with { Route = s.Decision.Route.ToArray(), Visited = s.Decision.Visited.ToArray() })
        {
            TargetId = target?.Id ?? 0,
            TargetKind = target is null ? SquadTargetKind.None : target.Kind switch
            { CombatTargetKind.Unit => SquadTargetKind.Squad, CombatTargetKind.City => SquadTargetKind.Keep, _ => SquadTargetKind.Watchpost },
            AttackSequence = s.Action.AttackSequence,
            ImpactTick = target is null ? 0 : s.Action.Attack!.ImpactTick,
            ReadyTick = s.Action.ReadyTick,
            RallyDisruptedUntil = s.RallyDisruptedUntil,
            DeploymentCell = s.DeploymentCell
        };
    }).ToArray();
    internal StructureState[] StructureSnapshots(IEnumerable<City> cities) => cities.SelectMany(city =>
    {
        var structures = new List<StructureState> { new(city.Id, ArenaStructureKind.Keep, -1, _arena.Keep(city.Id), city.Health, HealthPoints.FromWhole(_cityHealth)) { Active = !city.Eliminated } };
        if (_watchposts.TryGetValue(city.Id, out StructureState? watchpost)) structures.Add(watchpost with { Active = !city.Eliminated });
        else structures.Add(new(city.Id, ArenaStructureKind.Watchpost, -1, _arena.Watchpost(city.Id), RunEffects.Watchpost(HealthPoints.FromWhole(_configuration.Arena.WatchpostHealth), city.Run), RunEffects.Watchpost(HealthPoints.FromWhole(_configuration.Arena.WatchpostHealth), city.Run)) { Active = !city.Eliminated });
        structures.AddRange(city.Towers.Values.Where(t => !ArmyConfiguration.IsArmyPlot(t.Slot)
            || _arena.Footprints.Any(f => f.Owner == city.Id && f.Slot == t.Slot)).OrderBy(t => t.Slot).Select(t => new StructureState(city.Id,
            t.Type is Building.CatapultTower or Building.BombardTower ? ArenaStructureKind.CatapultTower : ArenaStructureKind.ArrowTower, t.Slot, _arena.Tower(city.Id, t.Slot), 1, 1, t.Level)
            { Active = !city.Eliminated, TowerType = t.Type }));
        return structures;
    }).ToArray();
    private int _cityHealth = 100;
    internal void SetCityHealth(int health) => _cityHealth = health;
    public CombatEvent[] Events() => _events.Select(e => e with { Unit = e.Unit is null ? null : CombatProjection.Detach(e.Unit, _work), Victims = e.Victims.ToArray(), Periodic = e.Periodic.ToArray() }).ToArray();
    internal BattleFoodForecast FoodForecast(int city, int food, EconomyConfiguration economy, RunState? run = null) => BattleFood.Forecast(Soldiers(city), food, economy.RecruitmentQuotes(_configuration, run: run));
    public long OldestEventSequence => _events.Count == 0 ? EventSequence + 1 : _events[0].Sequence;
    internal UnitState Read(int id) => Project(Unit(id));
    internal void Seed(UnitState state)
    {
        WeaponProfile profile = state.Profile.Health > 0 ? state.Profile : Profile(state.Type, state.Rank, state.IsBoss, state.Level, state.Faction);
        Seed(CombatProjection.Restore(state, profile, state.Decision ?? NewDecision(state.Id, state.Destination, 0), _tick));
    }
    internal void Seed(CombatUnit member)
    {
        if (member.Id <= 0 || member.Health < 0 || member.Health > member.Profile.Health || member.Identity.Level < 1)
            throw new ArgumentException("Invalid restored member.", nameof(member));
        member.Capabilities.Validate(); StatusPolicy.Validate(member.Statuses, _configuration.Statuses);
        int squadId = member.SquadId > 0 ? member.SquadId : FindSquad(member);
        if (!_squads.ContainsKey(squadId)) _squads.Add(squadId, NewSquad(squadId, member));
        _nextSquadId = Math.Max(_nextSquadId, checked(squadId + 1));
        _squads[squadId] = _squads[squadId] with { Location = member.Location, Action = member.Action, Decision = member.Decision };
        if (_entities.Remove(member.Id, out Entity prior)) _world.Destroy(prior);
        _entities.Add(member.Id, _world.Create(member with { SquadId = squadId })); _memberOwners[member.Id] = member.Faction == Faction.Adventurers ? member.Owner : 0; _nextId = Math.Max(_nextId, checked(member.Id + 1));
    }
    internal void Remove(int id)
    {
        if (!_entities.Remove(id, out Entity entity)) return;
        int squad = _world.Get<CombatUnit>(entity).SquadId; _world.Destroy(entity);
        ReconcileSquad(squad);
    }
    private void ReconcileSquad(int id)
    {
        if (!_squads.TryGetValue(id, out CombatSquad? squad)) return;
        CombatUnit[] members = Living().Where(u => u.SquadId == id).ToArray();
        if (members.Length == 0) _squads.Remove(id);
        else if (squad.Alive && members.All(u => u.Location.Lifecycle != UnitLifecycle.Alive))
            _squads[id] = squad with
            {
                Location = squad.Location with { Lifecycle = UnitLifecycle.Reserve },
                Action = CombatActions.Cancel(squad.Action, _tick),
                Decision = NewDecision(squad.Id, squad.Destination, squad.Decision.Sequence)
            };
    }
    internal void Assign(int id, ArmyAssignment assignment)
    {
        CombatUnit member = Unit(id); int old = member.SquadId;
        if (member.Assignment == assignment) return;
        member = member with { Assignment = assignment, Location = new(assignment.Stored ? UnitLifecycle.Stored : UnitLifecycle.Queued, default), Statuses = StatusState.Empty, StrikeAim = null };
        // Hide the member's old claim while finding its destination group.
        Unit(id) = member with { SquadId = 0 };
        int squad = FindSquad(member);
        int[] slots = Living().Where(u => u.SquadId == squad).Select(u => u.FormationSlot).ToArray();
        Unit(id) = member with { SquadId = squad, FormationSlot = Enumerable.Range(0, _configuration.Arena.SquadCapacity).First(slot => !slots.Contains(slot)) };
        ReconcileSquad(old);
    }
    internal void RestoreHomes()
    {
        bool cleanup = HasDeaths;
        CombatUnit[] living = Living();
        foreach (CombatSquad squad in _squads.Values.ToArray())
        {
            CombatUnit[] members = living.Where(u => u.SquadId == squad.Id).ToArray();
            if (members.Length == 0) continue;
            if (cleanup && squad.Location.FrozenTick is not null)
            {
                foreach (CombatUnit member in members.Where(u => u.Location.Lifecycle == UnitLifecycle.Queued))
                    Unit(member.Id) = member with { Location = member.Location with { Lifecycle = UnitLifecycle.Reserve } };
                ReconcileSquad(squad.Id);
                continue;
            }
            CombatUnit first = members[0];
            // Hall slots count individuals and can exceed the six field-home
            // indexes. Stored actors have no deployment pose or occupancy.
            bool stored = first.Assignment is { Stored: true };
            int deployment = stored ? 0 : squad.DeploymentCell > 0 ? squad.DeploymentCell : _arena.Deployment(first.Owner > 0 ? first.Owner : 1, first.Assignment is null ? 0 : HomeIndex(first.Assignment));
            UnitLifecycle lifecycle = stored ? UnitLifecycle.Stored : cleanup ? UnitLifecycle.Queued : UnitLifecycle.Alive;
            foreach (CombatUnit member in members)
                Unit(member.Id) = member with { Location = new(lifecycle, default), Statuses = StatusState.Empty, StrikeAim = null };
            if (squad.RallyDisruptedUntil > 0) EmitExtra(CombatEventType.RallyRestored, squad.Id);
            _squads[squad.Id] = squad with
            {
                Location = new(lifecycle, stored ? default : new(deployment, 1)),
                Action = CombatActions.Cancel(squad.Action, _tick),
                Decision = NewDecision(squad.Id, squad.Destination, squad.Decision.Sequence),
                RallyDisruptedUntil = 0,
                DeploymentCell = deployment
            };
        }
    }
    internal bool Place(int owner, int squadId, int cell, int homes, IReadOnlyList<SlotState>? plots = null)
    {
        CombatUnit[] roster = Living();
        bool permitted = plots is null ? _arena.CanDeploy(owner, cell, homes) : _arena.CanDeploy(owner, cell, plots);
        if (!_squads.TryGetValue(squadId, out CombatSquad? squad) || squad.Owner != owner || !permitted
            || !roster.Any(u => u.SquadId == squadId && u.Assignment is { Stored: false })
            || _squads.Values.Any(s => s.Id != squadId && s.Owner == owner && s.DeploymentCell == cell && roster.Any(u => u.SquadId == s.Id && u.Assignment is { Stored: false }))) return false;
        int home = _arena.DeploymentHome(owner, cell);
        int tile = home;
        foreach (CombatUnit member in roster.Where(u => u.SquadId == squadId && u.Assignment is { Stored: false }))
            Unit(member.Id) = member with { Assignment = member.Assignment! with { Tile = tile } };
        _squads[squadId] = squad with
        {
            DeploymentCell = cell,
            Location = squad.Location.FrozenTick is not null ? squad.Location : squad.Location with { Position = new(cell, 1) }
        }; return true;
    }
    internal void AuthorizeRecovery(IEnumerable<int> funded)
    {
        var paid = funded.ToHashSet();
        foreach (CombatUnit member in Living().Where(u => u.Faction == Faction.Adventurers))
            Unit(member.Id) = member with { RecoveryEligible = member.Assignment is not null && paid.Contains(member.Id) };
    }
    internal Dictionary<int, int> RecoveryPlan(IEnumerable<City> cities)
    {
        var plan = new Dictionary<int, int>();
        foreach (City city in cities)
            foreach (CombatUnit member in SoldierMembership(city.Id).Where(u => u.RecoveryEligible && u.Assignment is { Stored: true }))
            {
                SlotState hall = city.Slots[member.Assignment!.HallSlot];
                if (hall.Type != Building.TownHall || hall.Generation != member.Assignment.HallGeneration) throw new InvalidOperationException("Stored member lost its Town hall.");
                plan.Add(member.Id, ArmyConfiguration.HealedHealth(member.Health, member.Profile.Health, ArmyConfiguration.HealingPercent(hall.HealingLevel)));
            }
        return plan;
    }
    internal void Heal(IReadOnlyDictionary<int, int> plan) { foreach ((int id, int health) in plan) Unit(id) = Unit(id) with { Health = health }; }
    internal void AwardExperience(IEnumerable<int> paidParticipants)
    {
        var eligible = paidParticipants.ToHashSet();
        foreach (CombatUnit member in Living().Where(u => u.Faction == Faction.Adventurers && u.Location.Lifecycle == UnitLifecycle.Alive && eligible.Contains(u.Id)))
            Unit(member.Id) = member with { Experience = checked(member.Experience + 1) };
    }
    public void BeginWave(int wave = 1, IEnumerable<int>? unfed = null) => BeginWave(wave, unfed, []);
    internal void BeginWave(int wave, IEnumerable<int>? unfed, IEnumerable<City> cities)
    {
        var reserves = (unfed ?? []).ToHashSet(); City[] owners = cities.ToArray();
        if (owners.Length > 0) _arena = new(owners.Select(c => c.Id), ArmyWeapons(owners));
        _wave = wave; _events.Clear();
        foreach (City city in owners)
        {
            _owners[city.Id] = city;
            city.Run = city.Run with { EnemyKills = 0, FirstAttackSquads = [], WarhornSquads = [] };
        }
        foreach (int owner in _watchposts.Keys.Except(owners.Select(c => c.Id)).ToArray()) _watchposts.Remove(owner);
        foreach (City city in owners.Where(c => !c.Eliminated))
            _watchposts[city.Id] = new(city.Id, ArenaStructureKind.Watchpost, -1, _arena.Watchpost(city.Id), RunEffects.Watchpost(HealthPoints.FromWhole(_configuration.Arena.WatchpostHealth), city.Run), RunEffects.Watchpost(HealthPoints.FromWhole(_configuration.Arena.WatchpostHealth), city.Run));
        foreach (CombatUnit member in Living())
            Unit(member.Id) = member with
            {
                Location = new(member.Assignment is { Stored: true } ? UnitLifecycle.Stored
                : reserves.Contains(member.Id) || member.Faction == Faction.Adventurers && (_eliminatedOwners.Contains(member.Owner) || owners.Any(c => c.Id == member.Owner && c.Eliminated)) ? UnitLifecycle.Reserve
                : member.Faction == Faction.Skeletons ? UnitLifecycle.Queued : UnitLifecycle.Alive, default),
                Statuses = StatusState.Empty,
                StrikeAim = null
            };
        foreach (CombatSquad squad in _squads.Values.ToArray())
        {
            CombatUnit[] all = Living().Where(u => u.SquadId == squad.Id).ToArray();
            if (all.Length == 0) continue;
            bool enemy = squad.Faction == Faction.Skeletons;
            bool stored = all.All(u => u.Location.Lifecycle == UnitLifecycle.Stored);
            int cell = stored ? 0 : squad.DeploymentCell > 0 ? squad.DeploymentCell : _arena.Deployment(squad.Owner > 0 ? squad.Owner : 1, all[0].Assignment is null ? 0 : HomeIndex(all[0].Assignment!));
            if (squad.RallyDisruptedUntil > 0) EmitExtra(CombatEventType.RallyRestored, squad.Id);
            _squads[squad.Id] = squad with
            {
                Location = new(stored ? UnitLifecycle.Stored : enemy ? UnitLifecycle.Queued : all.Any(u => u.Location.Lifecycle == UnitLifecycle.Alive) ? UnitLifecycle.Alive : UnitLifecycle.Reserve, stored ? default : new(cell, 1)),
                Action = new CombatAction.Waiting(squad.Action.Sequence, squad.Action.AttackSequence, _tick),
                Decision = NewDecision(squad.Id, squad.Destination, checked(squad.Decision.Sequence + 1)),
                RallyDisruptedUntil = 0
            };
        }
        AdmitEntries(initial: true); EmitExtra(CombatEventType.WaveStarted);
    }
    public void AdmitEntries(bool initial = false)
    {
        _ = initial;
        var occupied = _squads.Values.Where(s => s.Alive).Select(s => s.Location.Position.Cell)
            .Concat(_squads.Values.Select(s => s.Action).OfType<CombatAction.Moving>().Select(m => m.Destination.Cell)).ToHashSet();
        foreach (CombatSquad squad in _squads.Values.Where(s => s.Location.Lifecycle == UnitLifecycle.Queued).ToArray())
        {
            int cell = BattleDeployment.Entry(_arena.EnemyCells(squad.Destination), occupied);
            if (cell == 0) continue;
            occupied.Add(cell); _squads[squad.Id] = squad with { Location = new(UnitLifecycle.Alive, new(cell, 1), _tick) };
            foreach (CombatUnit member in Living().Where(u => u.SquadId == squad.Id)) Unit(member.Id) = member with { Location = new(UnitLifecycle.Alive, default) };
        }
    }
    public void Cleanup(long tick)
    {
        _tick = tick; _events.RemoveAll(e => e.Tick < tick - HistoryTicks);
        foreach (CombatUnit member in All().Where(u => u.Location.Lifecycle == UnitLifecycle.Dying && tick >= u.Location.DeathEndTick)) Remove(member.Id);
        TrimHistory();
    }
    public void Advance(long tick, IEnumerable<City> cities)
    {
        Cleanup(tick); _healthProgress.Clear(); City[] owners = cities.OrderBy(c => c.Id).ToArray();
        foreach (City city in owners) _owners[city.Id] = city;
        foreach (CombatSquad squad in _squads.Values.Where(s => s.Alive).ToArray())
        {
            if (squad.Action is CombatAction.Moving move && tick >= move.EndTick)
                _squads[squad.Id] = squad with { Location = squad.Location with { Position = move.Destination }, Action = CombatActions.Complete(move, tick), Decision = NewDecision(squad.Id, squad.Destination, checked(squad.Decision.Sequence + 1)) };
            else if (squad.Action is CombatAction.Recovery && tick >= squad.Action.ReadyTick)
                _squads[squad.Id] = squad with { Action = CombatActions.Complete(squad.Action, tick), Decision = NewDecision(squad.Id, squad.Destination, checked(squad.Decision.Sequence + 1)) };
            if (squad.RallyDisruptedUntil > 0 && tick >= squad.RallyDisruptedUntil)
            { _squads[squad.Id] = _squads[squad.Id] with { RallyDisruptedUntil = 0 }; EmitExtra(CombatEventType.RallyRestored, squad.Id); }
        }
        CombatUnit[] before = All();
        var damage = new SortedDictionary<int, int>(); var cityDamage = new SortedDictionary<int, int>(); var postDamage = new SortedDictionary<int, int>();
        var applications = new List<StatusApplication>();
        var credits = new List<(int Victim, int Attacker, int Damage)>();
        Dictionary<int, StatusState> statuses = GatherPeriodic(before, tick, damage);
        foreach (var periodic in _periodic) foreach (PeriodicContribution contribution in periodic.Value) credits.Add((periodic.Key, contribution.SourceId, contribution.Damage));
        // Gather all committed contributors against pre-impact health. Deaths are
        // applied only after squad, structure and defense strikes are gathered.
        foreach (CombatSquad squad in _squads.Values.Where(s => s.Alive && s.Action is CombatAction.Windup a && tick >= a.Attack!.ImpactTick).ToArray())
        {
            var action = (CombatAction.Windup)squad.Action; CombatTarget target = action.Attack!.Target;
            CombatUnit[] attackers = Members(squad); bool valid;
            if (target.Kind == CombatTargetKind.Unit)
            {
                CombatSquad? defending = _squads.GetValueOrDefault(target.Id);
                valid = defending is { Alive: true } && defending.Faction != squad.Faction && InRange(squad, defending.Location.Position.Cell);
                CombatUnit[] defenders = valid ? Members(defending!) : [];
                var profiles = attackers.Select(m =>
                {
                    RunState? run = _owners.GetValueOrDefault(squad.Owner)?.Run;
                    bool horn = run?.WarhornSquads.Contains(squad.Id) == true;
                    bool afflicted = defenders.Any(d => d.Statuses.ChillAt(tick) > 0 || d.Statuses.Burn is { ExpiresTick: var expiry } && expiry >= tick || d.Statuses.Poison.Any(p => p.ExpiresTick >= tick));
                    int first = horn ? RunCatalog.Amount(run, "R01") : 0;
                    int primaryBonus = first + (afflicted ? RunCatalog.Amount(run, "M06") : 0);
                    WeaponProfile profile = primaryBonus == 0 && first == 0 ? m.Profile : _configuration.Unit(m.Type, m.Identity.Rank,
                        m.Identity.IsBoss, m.Identity.Level, m.Capabilities, m.Faction, primaryBonus, first).Runtime();
                    GroupDamageProfile p = new(profile.Damage, profile.SecondaryDamage);
                    if (tick < squad.RallyDisruptedUntil) p = new(Rally(p.Primary), Rally(p.Secondary));
                    return new MemberStrike(m.Id, p);
                }).ToArray();
                StrikeContribution[] contributions = SquadDamage.Contributions(profiles, defenders.Select(m => m.Id), Key(squad.Id, squad.Destination, action.AttackSequence, 0, CombatPurpose.Splash));
                // A lost group still resolves the committed cycle. Publish its miss
                // so receiving playback can finish each member's strike explicitly.
                if (!valid)
                    foreach (CombatUnit attacker in attackers)
                        Emit(CombatEventType.Impact, ProjectMember(attacker), target.Id, landed: false);
                foreach (StrikeContribution contribution in contributions)
                {
                    CombatUnit attacker = attackers.Single(m => m.Id == contribution.AttackerMemberId);
                    CombatUnit primary = defenders.Single(m => m.Id == contribution.PrimaryMemberId);
                    attacker = attacker with { StrikeAim = new(action.AttackSequence, target.Id, primary.Id, ProjectMember(primary).Pose(tick)) };
                    Unit(attacker.Id) = attacker;
                    foreach (MemberDamage hit in contribution.Damage.Where(d => d.Damage > 0))
                    {
                        CombatUnit defender = defenders.Single(m => m.Id == hit.MemberId); int amount = defender.Capabilities.Reduce(hit.Damage);
                        Accumulate(damage, hit.MemberId, amount); credits.Add((hit.MemberId, attacker.Id, amount));
                        if (amount > 0) GatherApplications(attacker, hit.MemberId, tick, applications, hit.Damage, action.AttackSequence, Math.Min(amount, defender.Health));
                    }
                    Emit(CombatEventType.Impact, ProjectMember(attacker), contribution.PrimaryMemberId, damage: contribution.Damage.Sum(d => d.Damage), landed: valid,
                        primary: defenders.FirstOrDefault(m => m.Id == contribution.PrimaryMemberId), victims: contribution.Damage.Where(d => d.Damage > 0).Select(d => d.MemberId).ToArray());
                }
            }
            else
            {
                City? city = owners.FirstOrDefault(c => c.Id == target.Id && !c.Eliminated);
                StructureState? post = _watchposts.GetValueOrDefault(target.Id);
                int cell = target.Kind == CombatTargetKind.City ? _arena.Keep(target.Id) : _arena.Watchpost(target.Id);
                valid = city is not null && (target.Kind == CombatTargetKind.City || post is { Destroyed: false }) && InRange(squad, cell);
                if (valid) foreach (CombatUnit attacker in attackers)
                    Accumulate(target.Kind == CombatTargetKind.City ? cityDamage : postDamage, target.Id, attacker.Profile.StructureDamage);
                foreach (CombatUnit attacker in attackers) Emit(CombatEventType.Impact, ProjectMember(attacker), target.Id, true, attacker.Profile.StructureDamage, valid);
            }
            _squads[squad.Id] = squad with { Action = CombatActions.Impact(action, tick, valid) };
            if (_owners.TryGetValue(squad.Owner, out City? attackingCity) && attackingCity.Run.WarhornSquads.Contains(squad.Id))
                attackingCity.Run = attackingCity.Run with { WarhornSquads = attackingCity.Run.WarhornSquads.Where(id => id != squad.Id).ToArray() };
        }
        foreach (City city in owners.Where(c => !c.Eliminated))
            foreach (DefenseActor defense in Defenses(city)) ResolveDefense(city, defense, damage, credits);
        foreach ((int id, int amount) in damage)
        {
            CombatUnit member = Unit(id); int effective = Math.Min(member.Health, amount); Unit(id) = member with { Health = member.Health - effective };
            if (effective == member.Health && effective > 0 && member.Faction == Faction.Skeletons)
            {
                int attacker = KillAttribution.Winner(effective, credits.Where(c => c.Victim == id).Select(c => (c.Attacker, c.Damage)));
                if (_memberOwners.TryGetValue(attacker, out int owner) && _owners.TryGetValue(owner, out City? killer)) killer.Run = killer.Run with { EnemyKills = checked(killer.Run.EnemyKills + 1) };
            }
            if (effective > 0) { _healthProgress.Add(member.Destination); Emit(CombatEventType.Hit, ProjectMember(Unit(id)), damage: effective, landed: true); }
        }
        foreach (City city in owners)
        {
            int structural = cityDamage.GetValueOrDefault(city.Id);
            // Same-tick post destruction ceases protection immediately.
            bool protectedKeep = _watchposts.TryGetValue(city.Id, out StructureState? guardian) && guardian.Health > postDamage.GetValueOrDefault(city.Id);
            int amount = Math.Min(city.Health, protectedKeep ? RunEffects.Scale(structural, -RunCatalog.Amount(city.Run, "R06")) : structural);
            if (amount > 0)
            {
                city.Health -= amount; _healthProgress.Add(city.Id);
                var keep = new StructureState(city.Id, ArenaStructureKind.Keep, -1, _arena.Keep(city.Id), city.Health, HealthPoints.FromWhole(_cityHealth), DestroyedTick: city.Eliminated ? tick : 0);
                EmitExtra(city.Eliminated ? CombatEventType.StructureDestroyed : CombatEventType.StructureHit, structure: keep, damage: amount);
            }
            if (!_watchposts.TryGetValue(city.Id, out StructureState? post) || post.Destroyed) continue;
            int postHit = Math.Min(post.Health, postDamage.GetValueOrDefault(city.Id));
            if (postHit <= 0) continue;
            post = post with { Health = post.Health - postHit, DestroyedTick = postHit == post.Health ? tick : 0 }; _watchposts[city.Id] = post; _healthProgress.Add(city.Id);
            EmitExtra(post.Destroyed ? CombatEventType.StructureDestroyed : CombatEventType.StructureHit, structure: post, damage: postHit);
            if (post.Destroyed)
                foreach (CombatSquad squad in _squads.Values.Where(s => s.Alive && s.Owner == city.Id).ToArray())
                { _squads[squad.Id] = squad with { RallyDisruptedUntil = checked(tick + _configuration.Arena.RallyDurationTicks) }; EmitExtra(CombatEventType.RallyDisrupted, squad.Id, post); }
        }
        CommitStatuses(statuses, applications);
        foreach (CombatUnit member in before.Where(u => u.Location.Lifecycle != UnitLifecycle.Dying)) if (_entities.ContainsKey(member.Id) && Unit(member.Id).Health <= 0) Kill(Unit(member.Id));
        TrimHistory();
    }
    internal void FieldSurgeons(int owner, int percent)
    {
        foreach (CombatUnit member in Living().Where(m => m.Faction == Faction.Adventurers && m.Owner == owner))
            Unit(member.Id) = member with { Health = Math.Min(member.Profile.Health, checked(member.Health + RunEffects.Portion(member.Profile.Health - member.Health, percent))) };
    }
    internal void UpdateWatchpost(City city)
    {
        if (!_watchposts.TryGetValue(city.Id, out StructureState? post)) return;
        int maximum = RunEffects.Watchpost(HealthPoints.FromWhole(_configuration.Arena.WatchpostHealth), city.Run);
        _watchposts[city.Id] = post with { MaximumHealth = maximum, Health = post.Health == 0 ? 0 : Math.Clamp(checked(post.Health + maximum - post.MaximumHealth), 1, maximum) };
    }
    private int Rally(int damage) => checked((int)((long)damage * (100 - _configuration.Arena.RallyDamageReductionPercent) / 100));
    private bool InRange(CombatSquad squad, int cell) => Board.Distance(squad.Location.Position.Cell, cell) <= Members(squad).Select(m => m.Profile.HexRange).DefaultIfEmpty(1).Min();
    private IEnumerable<DefenseActor> Defenses(City city)
    {
        if (!_watchposts.TryGetValue(city.Id, out StructureState? post) || !post.Destroyed) yield return new(DefenseIdentity.Defender(city.Id), city.Defender, _configuration.BuiltInDefense);
        foreach (TowerState tower in city.Towers.Values.OrderBy(t => t.Slot)) yield return new(DefenseIdentity.Tower(city.Id, tower.Slot), tower, RunDefense(tower.Type, tower.Level, city.Run));
    }
    private int DefenseCell(DefenseActor defense) => defense.Identity.Kind == CombatActorKind.Defender ? _arena.Watchpost(defense.State.City) : _arena.Tower(defense.State.City, defense.State.Slot);
    private int DefenseRange(DefenseActor defense) => defense.Identity.Kind == CombatActorKind.Defender ? _configuration.Arena.DefenderRange
        : defense.State.Type is Building.CatapultTower or Building.BombardTower ? _configuration.Arena.CatapultRange : _configuration.Arena.ArrowRange + RunCatalog.Amount(_owners.GetValueOrDefault(defense.State.City)?.Run, "T04");
    private DefenseProfile RunDefense(Building type, int level, RunState run)
    {
        TowerDefinition tower = RunEffects.Tower(_configuration.Towers.Single(t => t.Type == type && t.Level == level), run);
        return new(tower.Damage, tower.WindupTicks, tower.CadenceTicks - tower.WindupTicks);
    }
    private static void Accumulate(SortedDictionary<int, int> damage, int id, int amount) => damage[id] = checked(damage.GetValueOrDefault(id) + amount);
    private void ResolveDefense(City city, DefenseActor defense, SortedDictionary<int, int> damage, List<(int Victim, int Attacker, int Damage)> credits)
    {
        TowerState tower = defense.State;
        if (!tower.PendingImpact || _tick < tower.ImpactTick) return;
        CombatSquad? target = _squads.GetValueOrDefault(tower.TargetId);
        bool valid = target is { Alive: true, Faction: Faction.Skeletons } && Board.Distance(DefenseCell(defense), target.Location.Position.Cell) <= DefenseRange(defense);
        CombatUnit[] victims = valid ? Members(target!) : [];
        int y = tower.Type is Building.CatapultTower or Building.BombardTower ? RunEffects.Scale(HealthPoints.FromWhole(_configuration.Arena.CatapultSecondaryDamage), RunCatalog.Amount(city.Run, "T03")) : 0;
        StrikeContribution[] strikes = SquadDamage.Contributions([new(defense.Identity.KeyId, new(defense.Profile.Damage, y))], victims.Select(m => m.Id), Key(defense.Identity.KeyId, city.Id, tower.AttackSequence, 0, CombatPurpose.Splash, defense.Identity.Kind));
        foreach (MemberDamage hit in SquadDamage.Accumulate(strikes))
        {
            int amount = Unit(hit.MemberId).Capabilities.Reduce(hit.Damage); Accumulate(damage, hit.MemberId, amount);
            credits.Add((hit.MemberId, int.MaxValue - city.Id * 32 - (defense.Identity.Kind == CombatActorKind.Defender ? 0 : tower.Slot + 1), amount));
        }
        EmitTower(CombatEventType.Impact, tower, victims.FirstOrDefault(m => strikes.Any(s => s.PrimaryMemberId == m.Id)), defense.Profile.Damage, valid,
            strikes.SelectMany(s => s.Damage).Where(d => d.Damage > 0).Select(d => d.MemberId).Distinct().ToArray());
        defense.Commit(city, tower with { PendingImpact = false });
    }
    public void StartActions(IEnumerable<City> cities)
    {
        City[] owners = cities.OrderBy(c => c.Id).ToArray();
        foreach (City city in owners) _owners[city.Id] = city;
        CombatSquad[] alive = _squads.Values.Where(s => s.Alive && s.Location.FrozenTick is null).ToArray();
        var moves = new List<SquadMoveRequest>(); var occupied = alive.ToDictionary(s => s.Location.Position.Cell);
        var pending = alive.Select(s => s.Action).OfType<CombatAction.Moving>().Select(m => m.Destination.Cell).ToHashSet();
        foreach (CombatSquad squad in alive.Where(s => s.Action is CombatAction.Waiting && _tick >= s.Action.ReadyTick))
        {
            CombatSquad[] opponents = alive.Where(s => s.Faction != squad.Faction).ToArray();
            var targets = opponents.Select(s => (Target: new CombatTarget(CombatTargetKind.Unit, s.Id), Cell: s.Location.Position.Cell, Priority: 0)).ToList();
            if (squad.Faction == Faction.Skeletons)
                foreach (City city in owners.Where(c => !c.Eliminated))
                {
                    int preference = city.Id == squad.Destination ? 1 : 3;
                    if (_watchposts.TryGetValue(city.Id, out StructureState? post) && !post.Destroyed) targets.Add((new(CombatTargetKind.Watchpost, city.Id), post.Cell, preference));
                    targets.Add((new(CombatTargetKind.City, city.Id), _arena.Keep(city.Id), preference + 1));
                }
            var inRange = targets.Where(t => InRange(squad, t.Cell)).OrderBy(t => t.Priority).ThenBy(t => Board.Distance(squad.Location.Position.Cell, t.Cell))
                .ThenBy(t => SeededDecision.Mix(SeededDecision.State(Key(squad.Id, squad.Destination, squad.Action.AttackSequence, 0, CombatPurpose.Target)) ^ (ulong)t.Target.Id ^ ((ulong)t.Target.Kind << 32))).ToArray();
            if (inRange.Length > 0) { StartAttack(squad, inRange[0].Target); continue; }
            if (targets.Count == 0) continue;
            int range = Members(squad).Min(m => m.Profile.HexRange);
            bool Passable(int cell) => !pending.Contains(cell) && (!occupied.TryGetValue(cell, out CombatSquad? other) || other.Faction == squad.Faction && other.Action is CombatAction.Waiting);
            _routeSearches++;
            IReadOnlyDictionary<int, int[]> routes = _arena.Routes(squad.Location.Position.Cell, targets.Select(t => t.Cell), range, Passable, _work);
            var approaches = targets.Select(t => (t.Target, t.Cell, t.Priority, Route: routes[t.Cell]))
                .Where(t => t.Route.Length > 0).OrderBy(t => t.Route.Length).ThenBy(t => t.Priority)
                .ThenBy(t => squad.Decision.ObjectiveId == t.Target.Id && squad.Decision.ObjectiveCity == (t.Target.Kind != CombatTargetKind.Unit) ? 0 : 1)
                .ThenBy(t => SeededDecision.Mix(SeededDecision.State(Key(squad.Id, squad.Destination, squad.Decision.Sequence, 0, CombatPurpose.Target)) ^ (ulong)t.Target.Id)).ToArray();
            if (approaches.Length == 0) continue;
            var chosen = approaches[0];
            _squads[squad.Id] = squad with
            {
                Decision = squad.Decision with
                {
                    ObjectiveId = chosen.Target.Id,
                    ObjectiveCity = chosen.Target.Kind != CombatTargetKind.Unit,
                    ObjectiveCell = chosen.Cell,
                    Visited = [],
                    Route = chosen.Route.Select(c => new HexPosition(c, 1)).ToArray()
                }
            };
            moves.Add(new(squad.Id, chosen.Route[0], StatusPolicy.Duration(Members(squad).Max(m => m.Profile.MoveTicks), Chill(squad))));
        }
        SquadMoveActor[] actors = alive.Select(s => new SquadMoveActor(s.Id, s.Faction, s.Location.Position.Cell, Members(s).Min(m => m.Profile.Initiative), s.Decision.SchedulingRank)).ToArray();
        foreach (SquadMoveCommit move in SquadMovement.Admit(_arena, actors, moves, pending, _tick))
        {
            CombatSquad squad = _squads[move.Id];
            _squads[move.Id] = squad with { Action = CombatActions.Move(squad.Action, Board.Transition(new(move.Source, 1), new(move.Destination, 1)), _tick, checked((int)(move.EndTick - move.StartTick))) };
        }
        foreach (City city in owners.Where(c => !c.Eliminated))
            foreach (DefenseActor defense in Defenses(city)) StartDefense(city, defense);
        TrimHistory();
    }
    private int Chill(CombatSquad squad) => SquadDamage.AggregateChill(Members(squad).Select(m => m.Statuses.ChillAt(_tick)));
    private void StartAttack(CombatSquad squad, CombatTarget target)
    {
        CombatUnit[] members = Members(squad);
        if (_owners.TryGetValue(squad.Owner, out City? owner) && !owner.Run.FirstAttackSquads.Contains(squad.Id))
            owner.Run = owner.Run with { FirstAttackSquads = owner.Run.FirstAttackSquads.Append(squad.Id).Order().ToArray(), WarhornSquads = owner.Run.WarhornSquads.Append(squad.Id).Order().ToArray() };
        int windup = StatusPolicy.Duration(members.Max(m => m.Profile.WindupTicks), Chill(squad));
        int recovery = StatusPolicy.Duration(members.Max(m => m.Profile.CadenceTicks - m.Profile.WindupTicks), Chill(squad));
        int cell = target.Kind == CombatTargetKind.Unit ? _squads[target.Id].Location.Position.Cell
            : target.Kind == CombatTargetKind.City ? _arena.Keep(target.Id) : _arena.Watchpost(target.Id);
        _squads[squad.Id] = squad with
        {
            Action = CombatActions.Attack(squad.Action, target, _tick, windup, recovery),
            Decision = squad.Decision with
            { ObjectiveId = target.Id, ObjectiveCity = target.Kind != CombatTargetKind.Unit, ObjectiveCell = cell, Route = [], Visited = [] }
        };
        CombatUnit[] defenders = target.Kind == CombatTargetKind.Unit && _squads.TryGetValue(target.Id, out CombatSquad? defender) ? Members(defender) : [];
        CombatAction committed = _squads[squad.Id].Action;
        foreach (CombatUnit member in members)
        {
            int primaryId = defenders.Length == 0 ? 0 : SquadDamage.Primary(defenders.Select(d => d.Id),
                Key(member.Id, squad.Destination, committed.AttackSequence, 0, CombatPurpose.Splash));
            CombatUnit? primary = defenders.FirstOrDefault(m => m.Id == primaryId);
            CombatUnit observed = member with { StrikeAim = primary is null ? null : new(committed.AttackSequence, target.Id, primary.Id, ProjectMember(primary).Pose(_tick)) };
            Unit(member.Id) = observed;
            Emit(CombatEventType.AttackStarted, ProjectMember(observed), primary?.Id ?? target.Id, target.Kind != CombatTargetKind.Unit, primary: primary);
        }
    }
    private void StartDefense(City city, DefenseActor defense)
    {
        TowerState tower = defense.State;
        if (tower.PendingImpact || _tick < tower.ReadyTick) return;
        CombatSquad? target = _squads.Values.Where(s => s.Alive && s.Faction == Faction.Skeletons && Board.Distance(DefenseCell(defense), s.Location.Position.Cell) <= DefenseRange(defense))
            .OrderBy(s => Board.Distance(DefenseCell(defense), s.Location.Position.Cell)).ThenBy(s => SeededDecision.Mix(SeededDecision.State(Key(defense.Identity.KeyId, city.Id, tower.AttackSequence, 0, CombatPurpose.Target, defense.Identity.Kind)) ^ (ulong)s.Id)).FirstOrDefault();
        if (target is null) return;
        tower = defense.Start(target.Id, _tick); defense.Commit(city, tower);
        EmitTower(CombatEventType.AttackStarted, tower, Members(target).FirstOrDefault(), defense.Profile.Damage, false, []);
    }
    private HexPosePoint? TargetPose(CombatUnit member)
    {
        if (member.Target is not { } target) return null;
        if (target.Kind != CombatTargetKind.Unit)
        {
            // Routing stores the exact physical objective cell; the old
            // ObjectiveCity flag alone cannot distinguish Keep from Watchpost.
            int cell = member.Action is CombatAction.Moving && member.Decision.ObjectiveCell > 0 ? member.Decision.ObjectiveCell
                : target.Kind == CombatTargetKind.City ? _arena.Keep(target.Id) : _arena.Watchpost(target.Id);
            return new(new(cell, 1));
        }
        if (member.StrikeAim?.AttackSequence == member.Action.AttackSequence && member.StrikeAim.SquadId == target.Id)
            return _entities.TryGetValue(member.StrikeAim.MemberId, out Entity aimed)
                ? ProjectMember(_world.Get<CombatUnit>(aimed)).Pose(_tick) ?? member.StrikeAim.Pose : member.StrikeAim.Pose;
        // Retained casualties are still available during simultaneous resolution.
        // Capture a value pose, never a live target reference or an occupancy lease.
        CombatUnit? defender = All().Where(u => u.SquadId == target.Id && u.Deployed)
            .OrderBy(u => u.Location.Lifecycle == UnitLifecycle.Dying ? 1 : 0).FirstOrDefault();
        return defender is null ? null : ProjectMember(defender).Pose(_tick);
    }
    private void Kill(CombatUnit member)
    {
        CombatUnit projected = ProjectMember(member); bool deployed = member.Location.Lifecycle == UnitLifecycle.Alive;
        Unit(member.Id) = projected with
        {
            Health = 0,
            Statuses = StatusState.Empty,
            Location = projected.Location with
            {
                Lifecycle = UnitLifecycle.Dying,
                DeathStartTick = _tick,
                DeathEndTick = checked(_tick + member.Profile.DeathTicks),
                FrozenTick = _tick,
                FrozenAim = TargetPose(projected),
                FrozenMoveTicks = projected.Action is CombatAction.Moving move ? checked((int)(_tick - move.StartTick)) : 0
            }
        };
        // A primary can die during someone else's pending windup. Retain its
        // exact physical death pose, not a new live member of the same group.
        foreach (CombatUnit observer in Living().Where(u => u.StrikeAim?.MemberId == member.Id))
            Unit(observer.Id) = observer with { StrikeAim = observer.StrikeAim! with { Pose = projected.Pose(_tick) } };
        Emit(CombatEventType.Death, Unit(member.Id));
        if (_squads.TryGetValue(member.SquadId, out CombatSquad? squad) && squad.Alive && Members(squad).Length == 0)
        {
            _squads[squad.Id] = squad with { Location = squad.Location with { Lifecycle = UnitLifecycle.Dying, DeathStartTick = _tick, DeathEndTick = checked(_tick + member.Profile.DeathTicks), FrozenTick = _tick } };
            EmitExtra(CombatEventType.SquadDeath, squad.Id);
        }
        if (!deployed) Remove(member.Id);
    }
    // Elimination affects future participation, never current survivors.
    public void EliminateArmy(int city) { _eliminatedOwners.Add(city); }
    public void Transfer(int id, int destination)
    {
        CombatUnit member = Unit(id);
        if (member.Health <= 0 || member.Location.Lifecycle == UnitLifecycle.Dying)
            throw new InvalidOperationException("A fallen member cannot redirect its squad.");
        CombatSquad squad = _squads[member.SquadId];
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(destination);
        _squads[squad.Id] = squad with { Destination = destination, Decision = NewDecision(squad.Id, destination, checked(squad.Decision.Sequence + 1)) };
        foreach (CombatUnit m in Living().Where(u => u.SquadId == squad.Id)) Unit(m.Id) = m with { Identity = m.Identity with { Destination = destination } };
    }
    public void Step(long tick, IEnumerable<City> cities) { City[] values = cities.ToArray(); Advance(tick, values); AdmitEntries(); StartActions(values); }
    public void StopActions(IEnumerable<City> cities)
    {
        foreach (CombatUnit member in Living())
        {
            CombatUnit projected = ProjectMember(member);
            Unit(member.Id) = member with
            {
                Location = member.Location with { FrozenAim = projected.Location.FrozenTick is null ? TargetPose(projected) : projected.Location.FrozenAim },
                Statuses = StatusState.Empty
            };
        }
        foreach (CombatSquad squad in _squads.Values.ToArray()) _squads[squad.Id] = squad with { Location = squad.Location with { FrozenTick = squad.Location.FrozenTick ?? _tick } };
        foreach (City city in cities)
        { city.Defender = city.Defender with { PendingImpact = false }; foreach (TowerState tower in city.Towers.Values.ToArray()) city.Towers[tower.Slot] = tower with { PendingImpact = false }; }
    }
    private void Emit(CombatEventType type, CombatUnit unit, int target = 0, bool city = false, int damage = 0, bool landed = false, CombatUnit? primary = null, int[]? victims = null)
    {
        EventSequence = checked(EventSequence + 1);
        _events.Add(new(EventSequence, _tick, type, Project(unit), target, city, damage, landed)
        {
            SquadId = unit.SquadId,
            ImpactPose = primary is not null ? ProjectMember(primary).Pose(_tick) : type is CombatEventType.Hit or CombatEventType.Death ? unit.Pose(_tick) : null,
            Periodic = type is CombatEventType.Hit or CombatEventType.Death ? _periodic.GetValueOrDefault(unit.Id)?.ToArray() ?? [] : [],
            Victims = victims ?? []
        });
    }
    private void EmitTower(CombatEventType type, TowerState tower, CombatUnit? target, int damage, bool landed, int[] victims)
    {
        EventSequence = checked(EventSequence + 1);
        _events.Add(new(EventSequence, _tick, type, null, target?.Id ?? 0, false, damage, landed) { Tower = tower, SquadId = target?.SquadId ?? 0, ImpactPose = target is null ? null : ProjectMember(target).Pose(_tick), Victims = victims });
    }
    private void EmitExtra(CombatEventType type, int squadId = 0, StructureState? structure = null, int damage = 0)
    { EventSequence = checked(EventSequence + 1); _events.Add(new(EventSequence, _tick, type, null, Damage: damage) { SquadId = squadId, Structure = structure }); }
    private void TrimHistory() { if (_events.Count > HistoryLimit) _events.RemoveRange(0, _events.Count - HistoryLimit); }
    public void Dispose()
    {
        if (IsDisposed) return;
        IsDisposed = true; _entities.Clear(); _squads.Clear(); _events.Clear(); _watchposts.Clear(); _healthProgress.Clear(); EcsWorld.Destroy(_world); GC.SuppressFinalize(this);
    }
}
