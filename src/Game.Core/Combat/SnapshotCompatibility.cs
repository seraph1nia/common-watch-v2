namespace Game.Core;

// A deliberate refusal, not migration by inventing squads from old soldier
// fields. Reconnect credentials are separate; this validates combat projections.
public static class SnapshotCompatibility
{
    public const int CurrentSquadRulesVersion = 7;
    public static void RequireSharedArena(MatchSnapshot state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.BattleFlowVersion != MatchSnapshot.CurrentBattleFlowVersion || !Enum.IsDefined(state.CombatSpeed)
            || state.Phase == Phase.Building && state.Turn is not (1 or 2)
            || state.Phase == Phase.Preparation && state.Turn != 3 || state.NextWave is null
            || state.NextWave.Any(u => u is null || u.Id >= 0 || u.Faction != Faction.Skeletons || u.Health != u.Profile.Health)
            || state.NextWave.Select(u => u.Id).Distinct().Count() != state.NextWave.Length)
            throw new ArgumentException("Legacy battle-flow snapshot refused: three preparations and authoritative wave forecasts are required. Rejoin a compatible authority or start a fresh match.", nameof(state));
        if (state.CombatRulesVersion != CurrentSquadRulesVersion || state.Rules?.Combat is null || state.Rules.Arena is null
            || state.Rules.Combat.RulesVersion != state.CombatRulesVersion || state.Rules.Combat.Board is not { Capacity: 1, Id: "shared-watch-arena" }
            || state.Arena is null || state.Arena.Board is not { Capacity: 1 } || state.Squads is null || state.Structures is null)
            throw new ArgumentException("Legacy combat snapshot refused: this client requires shared arena squad rules. Rejoin a compatible authority or start a fresh match.", nameof(state));
        if (state.Players is null || state.Enemies is null || state.DyingBodies is null || state.Players.Length > 4
            || state.Players.Any(p => p is null || p.Soldiers is null) || state.Enemies.Any(m => m is null)
            || state.Players.SelectMany(p => p.Soldiers).Any(m => m is null)
            || state.DyingBodies.Any(m => m is null) || state.Structures.Any(s => s is null)
            || state.CombatEvents is null || state.CombatEvents.Any(e => e is null || e.Victims is null || e.Periodic is null || e.Periodic.Any(p => p is null))
            || state.Squads.Any(s => s is null || s.Hex is null || s.Members is null || s.ParticipatingMembers is null || s.Decision is null)
            || state.Arena.Board.Cells is null || state.Arena.Footprints is null || state.Arena.Sectors is null
            || state.Arena.Board.Cells.Any(c => c is null) || state.Arena.Footprints.Any(f => f is null)
            || state.Arena.Sectors.Any(s => s is null || s.DeploymentCells is null || s.EnemyCells is null)
            || state.Players.Any(p => p.Id <= 0) || state.Players.Select(p => p.Id).Distinct().Count() != state.Players.Length
            || state.Players.Length == 0 && state.Phase != Phase.Lobby || state.Rules.Arena.SquadCapacity is < 1 or > 12)
            throw new ArgumentException("Malformed shared arena roster.", nameof(state));
        CombatSettings combat = state.Rules.Combat;
        if (new[] { combat.Swordsman, combat.Berserker, combat.Crossbowman, combat.Mage }.Any(t => t is null)
            || new[] { combat.Defender, combat.ArrowTower, combat.RapidArrowTower, combat.CatapultTower, combat.BombardTower }.Any(t => t is null)
            || combat.Statuses is null || state.Rules.Army?.HomePrices is null || state.Rules.Research is null || state.Rules.Campaign is null)
            throw new ArgumentException("Missing nested authoritative rules.", nameof(state));
        var configuration = new CombatConfiguration(state.Rules);
        var army = new ArmyConfiguration(state.Rules.Army, configuration.Board, configuration.Arena.SquadCapacity);
        var economy = new EconomyConfiguration(state.Rules);
        var campaign = new CampaignConfiguration(state.Rules.Campaign, configuration);
        RequireCatalogs(state, configuration, economy, campaign);
        foreach (CityState city in state.Players)
        {
            if (city.Slots is not { Length: ArmyConfiguration.PlotOffset } || city.Slots.Any(s => s is null)
                || city.Army is not { Homes: not null, HomePrices: not null, Halls: not null } roster
                || roster.Homes.Length != army.HomeCells.Count || roster.Homes.Any(h => h is null || h.Plot is null)
                || roster.Halls.Any(h => h is null) || roster.PurchasedHomes is < 2 or > ArmyConfiguration.PlotCount
                || roster.Homes.Count(home => home.Purchased) != roster.PurchasedHomes
                || roster.SquadCapacity != configuration.Arena.SquadCapacity || !roster.HomePrices.SequenceEqual(army.Prices())
                || roster.Homes.Where((home, index) => home.Cell != army.HomeCells[index]
                    || home.PlotId != ArmyConfiguration.PlotOffset + index || home.Purchased != home.Plot.Purchased
                    || home.Used < 0
                    || home.Used > roster.SquadCapacity).Any())
                throw new ArgumentException("Malformed city deployment or construction projection.", nameof(state));
            RequireCity(city, state.Phase, configuration, economy, army);
        }
        if (RulesIdentity.Combine(configuration.Fingerprint, economy.Fingerprint,
            campaign.Fingerprint) != state.ConfigurationFingerprint)
            throw new ArgumentException("Combat configuration identity refused: received rules do not match the full authoritative fingerprint.", nameof(state));
        if (state.NextWave.Any(u => u.Profile.Health <= 0 || u.Statuses is not { Poison: not null } || u.Statuses.Poison.Any(p => p is null)
            || !state.Players.Any(p => p.Id == u.Origin) || !state.Players.Any(p => p.Id == u.Destination)
            || u.Profile != configuration.Unit(u.Type, u.Rank, u.IsBoss, u.Level, faction: Faction.Skeletons).Runtime()
            || u.Hex is not null || u.TargetCity || u.TargetId != 0 || u.TargetKind != SquadTargetKind.None
            || u.Decision is not null || u.Assignment is not null || u.SquadId != 0 || u.FormationSlot != 0
            || u.AttackSequence != 0 || u.ActionStartTick != 0 || u.ImpactTick != 0 || u.ReadyTick != 0
            || u.PendingImpact || u.AttackLanded is not null || u.Capabilities != default || u.Cooldown != 0
            || u.Statuses.Active))
            throw new ArgumentException("Authoritative wave forecast profile refused: received actors do not match configured identities.", nameof(state));
        var arena = new SharedArena(state.Players.Length == 0 ? [1] : state.Players.Select(p => p.Id),
            state.Arena.Footprints.Where(f => ArmyConfiguration.IsArmyPlot(f.Slot)).Select(f => (f.Owner, f.Slot)));
        ArenaState topology = arena.Snapshot();
        HexBoardDefinition expected = topology.Board, received = state.Arena.Board;
        if (received.Id != expected.Id || received.Version != expected.Version || received.Capacity != expected.Capacity
            || received.Cells.Length != expected.Cells.Length || received.Anchors is null || received.Entries is null || received.SiegeCells is null
            || !received.Anchors.SequenceEqual(expected.Anchors) || !received.SiegeCells.SequenceEqual(expected.SiegeCells)
            || received.Entries.Length != expected.Entries.Length
            || received.Entries.Where((e, i) => e is null || e.Faction != expected.Entries[i].Faction || e.RearCells is null || e.FrontCells is null
                || !e.RearCells.SequenceEqual(expected.Entries[i].RearCells) || !e.FrontCells.SequenceEqual(expected.Entries[i].FrontCells)).Any()
            || received.Cells.Where((c, i) => c.Id != expected.Cells[i].Id || c.Coordinate != expected.Cells[i].Coordinate
                || c.Affinity != expected.Cells[i].Affinity || c.Neighbors is null || !c.Neighbors.SequenceEqual(expected.Cells[i].Neighbors)).Any()
            || !state.Arena.Footprints.SequenceEqual(arena.Footprints)
            || state.Arena.Sectors.Length != topology.Sectors.Length
            || state.Arena.Sectors.Where((s, i) => s.Owner != topology.Sectors[i].Owner
                || !s.DeploymentCells.SequenceEqual(topology.Sectors[i].DeploymentCells)
                || !s.EnemyCells.SequenceEqual(topology.Sectors[i].EnemyCells)).Any())
            throw new ArgumentException("Malformed shared arena topology or footprints.", nameof(state));
        CombatReservations claims = CombatReservations.Reconstruct(arena, state.Squads);
        if (state.Reservations is null || state.Reservations.Positions is null || state.Reservations.Transit is null
            || !state.Reservations.Positions.SequenceEqual(claims.Positions) || !state.Reservations.Transit.SequenceEqual(claims.Transit))
            throw new ArgumentException("Shared arena reservation projection mismatch.", nameof(state));
        UnitState[] members = state.Players.SelectMany(p => p.Soldiers).Concat(state.Enemies).ToArray();
        foreach (UnitState member in members.Concat(state.DyingBodies).Concat(state.CombatEvents.Where(e => e.Unit is not null).Select(e => e.Unit!)))
            RequireProjection(member, arena, configuration);
        foreach (UnitState body in state.DyingBodies)
            if (body.Health != 0 || body.Hex!.Lifecycle != UnitLifecycle.Dying)
                throw new ArgumentException("Invalid retained casualty.", nameof(state));
        foreach (CombatEvent entry in state.CombatEvents)
        {
            if (!Enum.IsDefined(entry.Type) || entry.Type == CombatEventType.AttackStarted && entry.Unit is null && entry.Tower is null)
                throw new ArgumentException("Invalid combat event actor.", nameof(state));
            if (entry.ImpactPose is { } pose) RequirePose(pose, arena.Board);
            if (entry.TargetCity) RequireStructureTarget(entry.TargetId, entry.Unit?.TargetKind ?? SquadTargetKind.None, arena);
            if (entry.Structure is { } structure && !arena.Board.Cells.Any(c => c.Id == structure.Cell))
                throw new ArgumentException("Invalid historical structure pose.", nameof(state));
        }
        if (state.Structures.Select(s => (s.Owner, s.Kind, s.Slot)).Distinct().Count() != state.Structures.Length
            || state.Structures.Select(s => s.Cell).Distinct().Count() != state.Structures.Length
            || state.Structures.Any(s => !arena.Footprints.Any(f => f.Owner == s.Owner && f.Slot == s.Slot && f.Cell == s.Cell
            && (f.Kind == s.Kind || f.Kind == ArenaStructureKind.ArrowTower && s.Kind == ArenaStructureKind.CatapultTower))))
            throw new ArgumentException("Invalid structure footprint.", nameof(state));
        if (members.Concat(state.DyingBodies).Select(m => m.Id).Distinct().Count() != members.Length + state.DyingBodies.Length
            || state.Squads.Select(s => s.Id).Distinct().Count() != state.Squads.Length)
            throw new ArgumentException("Duplicate member or squad identity.", nameof(state));
        var byId = members.ToDictionary(m => m.Id);
        foreach (SquadState squad in state.Squads)
        {
            if (squad.Id <= 0 || squad.Hex.Id != squad.Id || squad.Members.Length > (squad.IsBoss ? 1 : state.Rules.Arena.SquadCapacity)
                || squad.Members.Distinct().Count() != squad.Members.Length || squad.ParticipatingMembers.Distinct().Count() != squad.ParticipatingMembers.Length
                || squad.ParticipatingMembers.Any(id => !squad.Members.Contains(id))
                || (squad.ParticipatingMembers.Length > 0) != (squad.Hex.Lifecycle == UnitLifecycle.Alive)
                || squad.Decision.Route is null || squad.Decision.Visited is null || !Enum.IsDefined(squad.TargetKind)
                || squad.AttackSequence < 0 || squad.ReadyTick < 0 || squad.ImpactTick < 0
                || squad.Hex.Action == UnitActionKind.Windup && (squad.TargetId <= 0 || squad.TargetKind == SquadTargetKind.None
                    || squad.AttackSequence <= 0 || squad.Hex.StartTick < 0 || squad.ImpactTick <= squad.Hex.StartTick
                    || squad.ReadyTick <= squad.ImpactTick || squad.Hex.EndTick != squad.ReadyTick))
                throw new ArgumentException("Invalid squad population or actor identity.", nameof(state));
            if (squad.TargetKind is SquadTargetKind.Keep or SquadTargetKind.Watchpost)
                RequireStructureTarget(squad.TargetId, squad.TargetKind, arena);
            foreach (int id in squad.Members)
                if (!byId.TryGetValue(id, out UnitState? member) || member.SquadId != squad.Id || member.Type != squad.Type || member.Faction != squad.Faction
                    || member.Owner != squad.Owner || member.IsBoss != squad.IsBoss || member.Experience < 0 || member.Health <= 0 || member.Health > member.Profile.Health
                    || member.FormationSlot < 0 || member.FormationSlot >= (squad.IsBoss ? 1 : state.Rules.Arena.SquadCapacity))
                    throw new ArgumentException("Invalid persistent squad member.", nameof(state));
                else if ((member.Hex! with { Id = squad.Id, City = 1, Lifecycle = squad.Hex.Lifecycle, FrozenAim = squad.Hex.FrozenAim }) != squad.Hex
                    || member.AttackSequence != squad.AttackSequence || member.ImpactTick != squad.ImpactTick || member.ReadyTick != squad.ReadyTick
                    || member.Hex.Lifecycle == UnitLifecycle.Dying
                    || member.Hex.Lifecycle == UnitLifecycle.Reserve && squad.Hex.Lifecycle is not (UnitLifecycle.Reserve or UnitLifecycle.Alive or UnitLifecycle.Dying)
                    || member.Hex.Lifecycle != UnitLifecycle.Reserve && member.Hex.Lifecycle != squad.Hex.Lifecycle
                    || squad.ParticipatingMembers.Contains(id) != (member.Hex.Lifecycle == UnitLifecycle.Alive)
                    || member.TargetKind != squad.TargetKind || member.TargetCity != (squad.TargetKind is SquadTargetKind.Keep or SquadTargetKind.Watchpost
                        || squad.TargetKind == SquadTargetKind.None && squad.Decision.ObjectiveCity)
                    || squad.TargetKind is SquadTargetKind.Keep or SquadTargetKind.Watchpost && member.TargetId != squad.TargetId
                    || squad.TargetKind == SquadTargetKind.None && member.TargetId != squad.Decision.ObjectiveId)
                    throw new ArgumentException("Member projection disagrees with its squad actor.", nameof(state));
            if (squad.Members.Select(id => byId[id].FormationSlot).Distinct().Count() != squad.Members.Length)
                throw new ArgumentException("Squad members have overlapping formation slots.", nameof(state));
        }
        if (members.Any(m => !state.Squads.Any(s => s.Id == m.SquadId && s.Members.Contains(m.Id))))
            throw new ArgumentException("A persistent member has no squad identity.", nameof(state));
    }
    private static void RequireCatalogs(MatchSnapshot state, CombatConfiguration combat, EconomyConfiguration economy, CampaignConfiguration campaign)
    {
        BuildingDefinition[] buildings = economy.Buildings();
        WaveDefinition[] waves = campaign.Definition().Waves;
        if (state.BuildingCatalog is null || state.BuildingCatalog.Length != buildings.Length
            || state.BuildingCatalog.Where((b, i) => b is null || (b with { Recruits = null }) != (buildings[i] with { Recruits = null })
                || (b.Recruits is null) != (buildings[i].Recruits is null)
                || b.Recruits is not null && !b.Recruits.SequenceEqual(buildings[i].Recruits!)).Any()
            || state.UnitCatalog is null || !state.UnitCatalog.SequenceEqual(Catalogs.Units(state.Rules))
            || state.TowerCatalog is null || !state.TowerCatalog.SequenceEqual(combat.Towers)
            || state.TechnologyCatalog is null || !state.TechnologyCatalog.SequenceEqual(economy.Research.Definitions())
            || state.MarketRates is null || !state.MarketRates.SequenceEqual(economy.MarketRates())
            || state.PlotPrices is null || !state.PlotPrices.SequenceEqual(economy.PlotPrices())
            || state.TotalWaves != campaign.TotalWaves || state.WaveCatalog is null || state.WaveCatalog.Length != waves.Length
            || state.WaveCatalog.Where((w, i) => w is null || w.Entries is null
                || (w with { Entries = waves[i].Entries }) != waves[i] || !w.Entries.SequenceEqual(waves[i].Entries)).Any())
            throw new ArgumentException("Malformed authoritative catalog projection.", nameof(state));
    }
    private static void RequireCity(CityState city, Phase phase, CombatConfiguration combat, EconomyConfiguration economy, ArmyConfiguration army)
    {
        RequireRun(city.Run, phase);
        economy.Research.ValidateState(city.Research);
        if (!city.Resources.IsValid || city.Soldiers.Any(u => !Enum.IsDefined(u.Type))
            || city.RecruitmentQuotes is null || !city.RecruitmentQuotes.SequenceEqual(economy.RecruitmentQuotes(combat, city.Research, city.Run))
            || city.Technologies is null || !city.Technologies.SequenceEqual(economy.Research.Definitions().Select(n => economy.Research.Eligibility(city.Research, n.Id))))
            throw new ArgumentException("Malformed city recruitment or research projection.", nameof(city));
        BuildingDefinition[] buildings = economy.Buildings();
        if (city.Army!.Homes.Any(h => h.Plot.Type != Building.Empty && !ArmyConfiguration.IsTower(h.Plot.Type)))
            throw new ArgumentException("Invalid army plot construction.", nameof(city));
        foreach (SlotState slot in city.Slots.Concat(city.Army.Homes.Select(h => h.Plot)))
        {
            BuildingDefinition? definition = buildings.SingleOrDefault(b => b.Type == slot.Type);
            int maximum = definition?.MaximumLevel ?? (slot.Type is Building.RapidArrowTower or Building.BombardTower ? 2 : 0);
            if (!Enum.IsDefined(slot.Type) || slot.Generation < 0 || !slot.Investment.IsValid
                || slot.Type == Building.Empty && slot.Level != 0 || slot.Type != Building.Empty && (!slot.Purchased || slot.Level < 1 || slot.Level > maximum)
                || slot.Type == Building.TownHall && (slot.CapacityLevel is < 1 or > 3 || slot.HealingLevel is < 1 or > 3)
                || slot.Type != Building.TownHall && (slot.CapacityLevel != 0 || slot.HealingLevel != 0)
                || slot.UpgradeQuote != (economy.TryUpgrade(slot.Type, slot.Level, out ResourceCost quote) ? quote : null))
                throw new ArgumentException("Malformed construction instance projection.", nameof(city));
        }
        TownHallState[] halls = army.Snapshot(city.Soldiers, city.Army!.PurchasedHomes, city.Slots).Halls;
        if (!city.Army.Halls.SequenceEqual(halls))
            throw new ArgumentException("Malformed Town Hall projection.", nameof(city));
        ResourceCost? production = phase is Phase.Victory or Phase.Defeat || city.Eliminated ? default(ResourceCost) : economy.ProjectedProduction(city.Slots, city.Research, city.Run);
        if (city.ProductionIncome != production)
            throw new ArgumentException("Malformed production projection.", nameof(city));
        BattleFoodForecast? expected = phase is Phase.Victory or Phase.Defeat ? null : BattleFood.Project(city, phase == Phase.Preparation);
        BattleFoodForecast? food = city.FoodForecast;
        if ((food is null) != (expected is null) || food is not null && (food.Participating is null || food.Unfed is null || food.Funded is null
            || (food with { Participating = expected!.Participating, Unfed = expected.Unfed, Funded = expected.Funded }) != expected
            || !food.Participating.SequenceEqual(expected.Participating) || !food.Unfed.SequenceEqual(expected.Unfed) || !food.Funded.SequenceEqual(expected.Funded)))
            throw new ArgumentException("Malformed food forecast projection.", nameof(city));
        if (city.LastUpkeep is { } receipt && (receipt.Participating is null || receipt.Unfed is null || receipt.Funded is null
            || receipt.Wave <= 0 || receipt.Paid < 0 || receipt.Funded.Any(id => id <= 0)
            || receipt.Funded.Distinct().Count() != receipt.Funded.Length || receipt.Unfed.Any(id => id <= 0 || receipt.Funded.Contains(id))
            || receipt.Unfed.Distinct().Count() != receipt.Unfed.Length || receipt.Participating.Any(id => !receipt.Funded.Contains(id))
            || receipt.Participating.Distinct().Count() != receipt.Participating.Length))
            throw new ArgumentException("Malformed upkeep receipt projection.", nameof(city));
    }
    private static void RequireRun(RunState run, Phase phase)
    {
        if (run is null || !RunCatalog.Doctrines.Any(d => d.Id == run.Doctrine) || run.Cards is null || run.Offers is null
            || run.FirstAttackSquads is null || run.WarhornSquads is null || run.DraftWave is < 0 or > 19
            || run.CodexClears is < 0 or > 20 || run.EnemyKills < 0 || run.MerchantWave is < 0 or > 20
            || run.Started == (phase == Phase.Lobby) || !run.Started && (run.Cards.Length > 0 || run.DraftWave != 0 || run.Choice is not null)
            || run.FirstAttackSquads.Any(s => s <= 0) || run.FirstAttackSquads.Distinct().Count() != run.FirstAttackSquads.Length
            || run.WarhornSquads.Distinct().Count() != run.WarhornSquads.Length
            || run.Cards.Any(c => c is null || !RunCatalog.Cards.Any(d => d.Id == c.Id && c.Count > 0 && c.Count <= d.MaxStacks))
            || run.Cards.Select(c => c.Id).Distinct().Count() != run.Cards.Length
            || run.Cards.Count(c => RunCatalog.Card(c.Id).Rarity == CardRarity.Relic) > 2
            || run.Offers.Length != (run.DraftWave == 0 ? 0 : 3) || run.Offers.Distinct().Count() != run.Offers.Length
            || run.Offers.Any(id => !RunCatalog.Cards.Any(c => c.Id == id)) || run.Choice is not null && !run.Offers.Contains(run.Choice)
            || run.Choice is not null && run.Stacks(run.Choice) == 0
            || run.DraftWave < 7 && run.Offers.Any(id => RunCatalog.Card(id).Rarity == CardRarity.Relic)
            || run.WarhornSquads.Any(s => !run.FirstAttackSquads.Contains(s))) throw new ArgumentException("Malformed authoritative run state.");
    }
    private static void RequireProjection(UnitState member, SharedArena arena, CombatConfiguration configuration)
    {
        HexBoard board = arena.Board;
        if (member.Id <= 0 || !Enum.IsDefined(member.Type) || !Enum.IsDefined(member.Faction) || !Enum.IsDefined(member.TargetKind)
            || member.Hex is not { } hex || !Enum.IsDefined(hex.Lifecycle) || !Enum.IsDefined(hex.Action)
            || member.Health < 0 || member.Health > member.Profile.Health || member.Profile.Health <= 0
            || member.Experience < 0 || member.FormationSlot < 0 || member.FormationSlot >= (member.IsBoss ? 1 : configuration.Arena.SquadCapacity)
            || member.Deployed != (hex.Lifecycle is UnitLifecycle.Alive or UnitLifecycle.Dying)
            || member.Decision is not { Route: not null, Visited: not null } || member.Statuses is not { Poison: not null }
            || member.Statuses.Poison.Any(p => p is null)
            || hex.Id != member.Id || hex.City != member.Destination || hex.Faction != member.Faction || hex.Size != 1
            || hex.ActionSequence < 0 || member.AttackSequence < 0 || member.ReadyTick < 0 || member.Cooldown < 0
            || member.PendingImpact != (hex.Action == UnitActionKind.Windup && hex.Lifecycle == UnitLifecycle.Alive && hex.FrozenTick is null)
            || hex.Action == UnitActionKind.Windup && (hex.ActionSequence <= 0 || member.AttackSequence <= 0
                || member.ActionStartTick != hex.StartTick || hex.StartTick < 0 || member.ImpactTick <= hex.StartTick
                || member.ReadyTick <= member.ImpactTick || hex.EndTick != member.ReadyTick || member.AttackLanded is not null)
            || hex.HoldsTransit && (hex.ActionSequence <= 0 || hex.StartTick < 0 || hex.EndTick <= hex.StartTick
                || member.ReadyTick > hex.StartTick || hex.Transition <= 0)
            || !member.TargetCity && member.TargetKind is SquadTargetKind.Keep or SquadTargetKind.Watchpost)
            throw new ArgumentException("Malformed combat member projection.", nameof(member));
        member.Capabilities.Validate();
        StatusPolicy.Validate(member.Statuses, configuration.Statuses);
        if (member.Profile != configuration.Unit(member.Type, member.Rank, member.IsBoss, member.Level, member.Capabilities, member.Faction).Runtime())
            throw new ArgumentException("Invalid authoritative member profile.", nameof(member));
        if (member.TargetCity) RequireStructureTarget(member.TargetId, member.TargetKind, arena);
        if (member.Deployed || hex.HoldsTransit) RequirePose(new(hex.Position, hex.HoldsTransit ? hex.Destination : default,
            hex.HoldsTransit ? hex.Transition : 0, hex.Lifecycle == UnitLifecycle.Dying ? hex.FrozenMoveTicks : 0,
            hex.HoldsTransit ? checked((int)(hex.EndTick - hex.StartTick)) : 0), board);
        if (hex.FrozenAim is { } aim) RequirePose(aim, board);
    }
    private static void RequireStructureTarget(int id, SquadTargetKind kind, SharedArena arena)
    {
        if (kind is not (SquadTargetKind.None or SquadTargetKind.Keep or SquadTargetKind.Watchpost)
            || !arena.Footprints.Any(f => f.Owner == id && f.Kind == (kind == SquadTargetKind.Watchpost ? ArenaStructureKind.Watchpost : ArenaStructureKind.Keep)))
            throw new ArgumentException("Invalid structure target reference.");
    }
    private static void RequirePose(HexPosePoint pose, HexBoard board)
    {
        if (pose.Position.Anchor != 1 || !board.Cells.Any(c => c.Id == pose.Position.Cell)
            || pose.ElapsedTicks < 0 || pose.DurationTicks < 0 || pose.ElapsedTicks > pose.DurationTicks
            || pose.Transition != 0 && (pose.DurationTicks <= 0 || pose.Destination.Anchor != 1
                || !board.Cell(pose.Position.Cell).Neighbors.Contains(pose.Destination.Cell)
                || board.Transition(pose.Position, pose.Destination).Id != pose.Transition))
            throw new ArgumentException("Invalid combat pose.", nameof(pose));
    }
}
