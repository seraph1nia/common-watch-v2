using System.Numerics;
using System.Text.Json;
using Game.Core;
using Game.Shared;
using Xunit;

namespace Game.Shared.Tests;

public sealed class SharedTests
{
    [Fact]
    public void EveryCheckpointReplaysDeterministicallyAndRecordsRealState()
    {
        foreach (string checkpoint in ScenarioRunner.Checkpoints)
        {
            string first = ScenarioRunner.Record(checkpoint);
            Assert.Equal(first, ScenarioRunner.Record(checkpoint));
            using JsonDocument record = JsonDocument.Parse(first);
            Assert.Equal(checkpoint, record.RootElement.GetProperty("checkpoint").GetString());
        }
    }

    [Fact]
    public void ScenarioUsesPaidBuildingsRecruitsCombatAndActualReward()
    {
        using var initial = ScenarioRunner.CreateAt("initial");
        Assert.Equal(9, initial.City.Slots.Length);
        Assert.Equal(5, initial.City.Slots.Count(s => s.Purchased));
        Assert.All(initial.City.Slots, s => Assert.Equal(Building.Empty, s.Type));
        using var selected = ScenarioRunner.CreateAt("building-selected");
        Assert.Equal(1, selected.SelectedSlot);
        Assert.Equal([Building.Farm, Building.Barracks, Building.MetalMine, Building.Lumbermill], selected.City.Slots.Take(4).Select(s => s.Type));
        Assert.Equal(new ResourceCost(6, Stone: 2), selected.City.Resources);
        using var combat = ScenarioRunner.CreateAt("combat");
        Assert.Equal(Phase.Combat, combat.Snapshot.Phase);
        Assert.Equal(4, combat.City.Soldiers.Length);
        Assert.NotEmpty(combat.Snapshot.Enemies);
        Assert.True(combat.Snapshot.Tick >= 180);
        Assert.Contains(combat.Snapshot.CombatEvents, e => e.Type == CombatEventType.AttackStarted && e.Unit is not null);
        using var deployment = ScenarioRunner.CreateAt("battle-start");
        for (int i = 0; i < 1000 && !CombatPlayback.All(deployment.Snapshot).Any(u => u.Hex?.HoldsTransit == true); i++) deployment.Step();
        Assert.Contains(CombatPlayback.All(deployment.Snapshot), u => u.Hex?.HoldsTransit == true);
        Assert.Contains(CombatPlayback.All(combat.Snapshot), u => u.Health < u.Profile.Health);
        Assert.NotNull(combat.Snapshot.Arena);
        Assert.Equal(54, combat.Snapshot.Arena!.Board.Cells.Length);
        Assert.NotEmpty(combat.Playback.Units());
        using var draft = ScenarioRunner.CreateAt("draft");
        Assert.Equal(Phase.RewardDraft, draft.Snapshot.Phase);
        Assert.Equal(3, draft.City.Run.Offers.Length);
        Assert.All(draft.City.Run.Offers, id => Assert.NotEmpty(RunCatalog.Card(id).Effect));
        string chosen = draft.City.Run.Offers[0];
        Assert.True(draft.ChooseReward(chosen).Accepted);
        Assert.Equal(2, draft.Snapshot.Wave);
        Assert.Equal(Phase.Building, draft.Snapshot.Phase);
        Assert.Equal(1, draft.City.Run.Stacks(chosen));
    }

    [Fact]
    public void CommandsReturnAuthorityReasonsAndDoNotMutateRejectedState()
    {
        using var session = ScenarioRunner.CreateAt("initial");
        string before = JsonSerializer.Serialize(session.Snapshot, WireJson.Options);
        CommandResult result = session.Send("build", 8, Building.Farm);
        Assert.False(result.Accepted);
        Assert.Contains("Purchase", result.Message);
        Assert.Equal(before, JsonSerializer.Serialize(session.Snapshot, WireJson.Options));
        Assert.Same(result, session.LastResult);
        Assert.False(session.Send("build", 100000, Building.Farm).Accepted);
        Assert.True(session.Send("build", 0, Building.Barracks).Accepted);
        Assert.False(session.Send("recruit", 0).Accepted); // No metal, authoritative rejection.
        Assert.True(session.Send("sell", 0).Accepted); // Generation captured by the adapter.
        Assert.Equal(Building.Empty, session.City.Slots[0].Type);
    }

    [Fact]
    public void AxialProjectionRoundTripsAndHitTestingRejectsOutsideBoard()
    {
        var projection = new HexProjection(52);
        for (int q = -10; q <= 10; q++)
            for (int r = -10; r <= 10; r++)
            {
                var cell = new HexCoordinate(q, r);
                Assert.Equal(cell, projection.Unproject(projection.Project(cell)));
                Assert.Equal(cell, projection.Unproject(projection.Project(cell) + new Vector2(4, -7)));
                Assert.All(projection.Corners(cell), corner => Assert.InRange(Vector2.Distance(corner, projection.Project(cell)), 51.99f, 52.01f));
            }
        foreach (HexCoordinate cell in SettlementLayout.Coordinates)
            Assert.Equal(cell, projection.HitTest(projection.Project(cell), SettlementLayout.Coordinates));
        Assert.Null(projection.HitTest(new Vector2(10000, 10000), SettlementLayout.Coordinates));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HexProjection(0));
    }

    [Fact]
    public void ModesAndIdentifiersKeepSpritesIndependentFromGameplay()
    {
        Assert.Equal(RenderMode.Debug, RenderOptions.Parse([]).Mode);
        Assert.Equal(RenderMode.Sprites, RenderOptions.Parse(["--render-mode", "sprites"]).Mode);
        Assert.Throws<ArgumentException>(() => RenderOptions.Parse(["--render-mode", "unknown"]));
        Assert.Throws<ArgumentException>(() => RenderOptions.Parse(["--assets-root"]));
        Assert.Equal("building/lumbermill", VisualAssets.BuildingId(Building.Lumbermill));
        Assert.Equal(Path.Combine("buildings", "lumbermill.png"), VisualAssets.RelativePath("building/lumbermill"));
        Assert.Equal(Path.Combine("terrain", "hex.png"), VisualAssets.RelativePath("terrain/hex"));
        Assert.Throws<ArgumentException>(() => VisualAssets.RelativePath("../../private"));
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Assert.Null(VisualAssets.Resolve(root, "building/lumbermill", RenderMode.Sprites));
        Assert.Null(VisualAssets.Resolve(root, "building/lumbermill", RenderMode.Debug,
            _ => throw new Exception("No-assets mode inspected a sprite.")));
        // Inject existence to verify routing without manufacturing an image fixture.
        Assert.Equal(Path.Combine(root, "buildings", "lumbermill.png"),
            VisualAssets.Resolve(root, "building/lumbermill", RenderMode.Sprites, _ => true));
        using var session = ScenarioRunner.CreateAt("combat");
        string before = JsonSerializer.Serialize(session.Snapshot, WireJson.Options);
        foreach (RenderMode mode in Enum.GetValues<RenderMode>())
            foreach (UnitState unit in CombatPlayback.All(session.Snapshot))
                _ = VisualAssets.Resolve(root, VisualAssets.UnitId(unit), mode);
        Assert.StartsWith("unit/enemy_", VisualAssets.UnitId(session.Snapshot.Enemies[0]));
        Assert.Equal(before, JsonSerializer.Serialize(session.Snapshot, WireJson.Options));
    }

    [Fact]
    public void CombatInterpolationUsesAuthorityCellsAndPlaybackTick()
    {
        using var session = ScenarioRunner.CreateAt("battle-start");
        UnitState? moving = null;
        for (int i = 0; i < 1000 && session.Snapshot.Phase == Phase.Combat; i++)
        {
            moving = CombatPlayback.All(session.Snapshot).FirstOrDefault(u => u.Hex?.HoldsTransit == true);
            if (moving is not null) break;
            session.Step();
        }
        Assert.NotNull(moving);
        HexUnitState hex = moving!.Hex!;
        Vector2 start = CombatVisuals.Position(moving, session.Snapshot, hex.StartTick);
        Vector2 end = CombatVisuals.Position(moving, session.Snapshot, hex.EndTick);
        Vector2 midpoint = CombatVisuals.Position(moving, session.Snapshot, (hex.StartTick + hex.EndTick) / 2.0);
        Assert.True(Vector2.Distance(start, end) > 20);
        Assert.InRange(Vector2.Distance(midpoint, (start + end) / 2), 0, .001f);
    }

    [Fact]
    public void ClientMutationsOfDetachedSnapshotsCannotChangeTheAuthority()
    {
        foreach (string checkpoint in new[] { "building-selected", "combat" })
        {
            using var session = ScenarioRunner.CreateAt(checkpoint);
            MatchSnapshot detached = session.Snapshot;
            string before = JsonSerializer.Serialize(detached, WireJson.Options);
            detached.Players[0].Slots[0] = new(Building.ArrowTower, 5);
            detached.BuildingCatalog.Single(b => b.Type == Building.Barracks).Recruits![0] = UnitType.Mage;
            detached.Rules.Army.HomePrices[0] = int.MaxValue;
            if (detached.NextWave.Length > 0)
                detached.NextWave[0] = detached.NextWave[0] with { Health = 1 };
            if (detached.Players[0].Soldiers.Length > 0)
                detached.Players[0].Soldiers[0] = detached.Players[0].Soldiers[0] with { Health = 1 };
            detached.Players[0] = detached.Players[0] with { Gold = int.MaxValue, Health = 1 };
            // Zero admitted time refreshes the detached view without ticking gameplay.
            session.Advance(0);
            Assert.Equal(before, JsonSerializer.Serialize(session.Snapshot, WireJson.Options));
        }
    }

    [Fact]
    public void ProductionAndRecruitmentSettleExactAuthoritativeResources()
    {
        using var session = ScenarioRunner.CreateAt("settlement");
        Assert.Equal(new ResourceCost(Gold: 6, Stone: 2), session.City.Resources);
        Assert.Equal(new ResourceCost(Gold: 2, Wood: 1, Food: 5, Metal: 4), session.City.ProductionIncome);
        RecruitmentQuote quote = Assert.Single(session.City.RecruitmentQuotes,
            q => q.Type == UnitType.Swordsman && q.Level == 1);
        Assert.Equal(new ResourceCost(Metal: 2), quote.Cost);
        Assert.Equal(1, quote.Upkeep);
        for (int preparation = 1; preparation <= 2; preparation++)
        {
            Assert.True(session.Send("ready").Accepted);
            Assert.Equal(new ResourceCost(Gold: 6 + 2 * preparation, Wood: preparation,
                Food: 5 * preparation, Stone: 2, Metal: 4), session.City.Resources);
            Assert.True(session.Send("recruit", 1).Accepted);
            Assert.Equal(2, session.City.Metal);
            Assert.True(session.Send("recruit", 1).Accepted);
            Assert.Equal(0, session.City.Metal);
            Assert.Equal(2 * preparation, session.City.Soldiers.Length);
            Assert.All(session.City.Soldiers, u => Assert.Equal(UnitType.Swordsman, u.Type));
        }
        Assert.True(session.Send("ready").Accepted);
        Assert.Equal(new ResourceCost(Gold: 12, Wood: 3, Food: 11, Stone: 2, Metal: 4), session.City.Resources);
        Assert.Equal(4, session.City.LastUpkeep!.Paid);
        Assert.Equal(4, session.City.LastUpkeep.Funded.Length);
    }

    [Fact]
    public void BothPresentationModesProduceTheSameCompleteGameplayResult()
    {
        string Play(string renderMode)
        {
            RenderOptions options = RenderOptions.Parse(["--render-mode", renderMode,
                "--assets-root", Path.Combine(Path.GetTempPath(), "missing-common-watch-sprites")]);
            using var session = ScenarioRunner.CreateAt("combat");
            for (int i = 0; i < 10000 && session.Snapshot.Phase == Phase.Combat; i++)
            {
                // Both render paths read the same IDs/poses between the same authority steps.
                foreach (UnitState unit in session.Playback.Units())
                {
                    _ = VisualAssets.Resolve(options.AssetsRoot, VisualAssets.UnitId(unit), options.Mode,
                        _ => options.Mode == RenderMode.Sprites);
                    _ = CombatVisuals.Position(unit, session.Snapshot, session.Playback.Tick);
                }
                session.Step();
            }
            Assert.Equal(Phase.RewardDraft, session.Snapshot.Phase);
            Assert.True(session.ChooseReward(session.City.Run.Offers[0]).Accepted);
            Assert.Equal(2, session.Snapshot.Wave);
            return JsonSerializer.Serialize(session.Snapshot, WireJson.Options);
        }
        Assert.Equal(Play("debug"), Play("sprites"));
    }

    [Fact]
    public void DeathAndFrozenMovementKeepTheAuthoritativeTransitPose()
    {
        using var session = ScenarioRunner.CreateAt("battle-start");
        for (int i = 0; i < 1000 && !CombatPlayback.All(session.Snapshot).Any(u => u.Hex?.HoldsTransit == true); i++) session.Step();
        UnitState moving = CombatPlayback.All(session.Snapshot).First(u => u.Hex?.HoldsTransit == true);
        HexUnitState hex = moving.Hex!;
        int elapsed = Math.Max(1, (int)(hex.EndTick - hex.StartTick) / 3);
        Vector2 expected = CombatVisuals.Position(moving, session.Snapshot, hex.StartTick + elapsed);
        UnitState dying = moving with { Hex = hex with { Lifecycle = UnitLifecycle.Dying, FrozenMoveTicks = elapsed } };
        UnitState frozen = moving with { Hex = hex with { FrozenTick = hex.StartTick + elapsed } };
        Assert.Equal(expected, CombatVisuals.Position(dying, session.Snapshot, hex.EndTick + 1000));
        Assert.Equal(expected, CombatVisuals.Position(frozen, session.Snapshot, hex.EndTick + 1000));
    }


    [Fact]
    public void SquadMembersHaveDistinctReadablePresentationOffsets()
    {
        using var session = ScenarioRunner.CreateAt("combat");
        UnitState[] cohort = session.City.Soldiers.GroupBy(u => u.SquadId)
            .OrderByDescending(g => g.Count()).First().ToArray();
        Assert.Equal(3, cohort.Length);
        Assert.Single(cohort.Select(u => u.Hex!.Position.Cell).Distinct());
        Vector2[] positions = cohort.Select(u => CombatVisuals.Position(u, session.Snapshot, session.Snapshot.Tick)).ToArray();
        for (int a = 0; a < positions.Length; a++)
            for (int b = a + 1; b < positions.Length; b++)
                Assert.True(Vector2.Distance(positions[a], positions[b]) >= 23.99f);
    }

}
