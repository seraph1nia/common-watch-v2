using Game.Core;
using Xunit;

namespace Game.Shared.Tests;

public sealed class BattleOutcomeTests
{
    [Fact]
    public void ReceiptsAndObservedKeepBaselineProduceFrozenFactualResults()
    {
        using var solo = ScenarioRunner.CreateAt("settlement");
        var journal = new BattleOutcomeJournal(); journal.Observe(solo.Snapshot);
        int keep = solo.City.Health;
        for (int turn = 0; turn < 2; turn++)
        {
            Accept(solo.Send("ready")); Accept(solo.Send("recruit", 1)); Accept(solo.Send("recruit", 1));
            journal.Observe(solo.Snapshot);
        }
        Accept(solo.Send("ready")); journal.Observe(solo.Snapshot);
        var paid = solo.City.LastUpkeep!;
        GameplayScenarios.FinishCombat(solo); journal.Observe(solo.Snapshot);
        var outcome = Assert.IsType<BattleOutcome>(journal.For(solo.PlayerId));
        Assert.Equal(paid.Participating.Length, outcome.Participants);
        int alive = paid.Participating.Count(id => solo.City.Soldiers.Any(u => u.Id == id && u.Health > 0));
        Assert.Equal(alive, outcome.Survived); Assert.Equal(paid.Participating.Length - alive, outcome.Lost);
        Assert.Equal(keep - solo.City.Health, outcome.KeepDamage);
        Assert.Equal(solo.City.LastReward!.Amount, outcome.Resources); Assert.Equal(solo.City.LastReward.Research, outcome.Research);
        Assert.Equal(paid.Paid, outcome.FoodPaid); Assert.True(outcome.ObservedEnd);
        Assert.Contains("survived", outcome.Summary); Assert.DoesNotContain("because", outcome.Summary);
        Accept(solo.ChooseReward(solo.City.Run.Offers[0]));
        if (solo.City.Soldiers.FirstOrDefault() is { } veteran) Accept(solo.Send("retire", unitId: veteran.Id));
        journal.Observe(solo.Snapshot);
        Assert.Same(outcome, journal.For(solo.PlayerId));
        journal.Reset(); journal.Observe(solo.Snapshot);
        var rejoined = Assert.IsType<BattleOutcome>(journal.For(solo.PlayerId));
        Assert.Null(rejoined.Survived); Assert.Null(rejoined.Lost); Assert.Null(rejoined.KeepDamage);
        Assert.Contains("missing history", rejoined.Details);
    }
    [Fact]
    public void FoodShortageAndDefeatNeverReuseAnEarlierClearReward()
    {
        using var solo = GameplayScenarios.CreateAt("forecast");
        var journal = new BattleOutcomeJournal(); journal.Observe(solo.Snapshot);
        int satOut = solo.City.FoodForecast!.Unfed.Count(id => solo.City.Soldiers.Any(u => u.Id == id && u.Assignment is not { Stored: true }));
        Assert.True(satOut > 0);
        Accept(solo.Send("ready")); journal.Observe(solo.Snapshot);
        GameplayScenarios.FinishCombat(solo); journal.Observe(solo.Snapshot);
        var outcome = Assert.IsType<BattleOutcome>(journal.For(solo.PlayerId));
        Assert.Equal(satOut, outcome.FieldUnfunded);
        Assert.Equal(solo.City.LastReward?.Wave == 8 ? solo.City.LastReward.Amount : default, outcome.Resources);
        using var uninvested = ScenarioRunner.CreateAt("initial"); journal.Reset(); journal.Observe(uninvested.Snapshot);
        int keep = uninvested.City.Health;
        for (int i = 0; i < 3; i++) Accept(uninvested.Send("ready"));
        GameplayScenarios.FinishCombat(uninvested); journal.Observe(uninvested.Snapshot);
        outcome = Assert.IsType<BattleOutcome>(journal.For(uninvested.PlayerId));
        Assert.Equal(Phase.Defeat, uninvested.Snapshot.Phase); Assert.Equal(keep, outcome.KeepDamage);
        Assert.Equal(default, outcome.Resources); Assert.Equal(0, outcome.Research);
        Assert.Equal(0, outcome.Survived); Assert.Equal(0, outcome.Lost);
    }
    [Fact]
    public void TerminalBaselineCanReportCasualtiesWithoutInventingEarlierKeepDamage()
    {
        using var solo = GameplayScenarios.CreateAt("victory");
        var journal = new BattleOutcomeJournal(); journal.Observe(solo.Snapshot);
        var outcome = Assert.IsType<BattleOutcome>(journal.For(solo.PlayerId));
        Assert.Equal(20, outcome.Wave); Assert.Equal("Victory", outcome.Result);
        Assert.Null(outcome.KeepDamage); Assert.NotNull(outcome.Survived);
        Assert.Equal(solo.City.LastReward!.Amount, outcome.Resources);
    }
    [Fact]
    public void BasicInspectionUsesPresentedEffectsAndHumanRolesWithoutRawEntityTiming()
    {
        var battle = new BattleShowcase("burn");
        var frame = battle.Frames.First(s => s.Players.SelectMany(p => p.Soldiers).Concat(s.Enemies).Any(u => u.Statuses.Burn is not null));
        var units = frame.Players.SelectMany(p => p.Soldiers).Concat(frame.Enemies).ToArray();
        var unit = units.First(u => u.Statuses.Burn is not null); var burn = unit.Statuses.Burn!;
        Assert.DoesNotContain("Burning", CombatInspection.Effects(unit, burn.AppliedTick - 1));
        Assert.Contains("Burning", CombatInspection.Effects(unit, burn.AppliedTick));
        Assert.DoesNotContain("Burning", CombatInspection.Effects(unit, burn.ExpiresTick));
        string basic = CombatInspection.Basic(unit, units, burn.AppliedTick);
        Assert.Contains(CombatInspection.Role(unit), basic); Assert.Contains("HP", basic); Assert.Contains("Target:", basic);
        Assert.DoesNotContain("#" + unit.Id, basic); Assert.DoesNotContain("tick", basic); Assert.DoesNotContain("Committed", basic);
    }
    private static void Accept(CommandResult result) => Assert.True(result.Accepted, result.Message);
    [Fact]
    public void LateCooperativeResultDoesNotInferKeepDamageAfterARewardChoice()
    {
        using var host = new AuthoritySession(AuthorityPolicy.PlayingHost, combatSeed: SoloSession.DefaultSeed);
        Assert.True(host.Admit(2, WireJson.ProtocolVersion, "", 0).Accepted);
        long[] sequences = [0, 0, 0];
        void Send(int player, GameplayIntent intent)
        {
            var command = intent.Capture(host.Snapshot(), ++sequences[player], player);
            Accept(player == 1 ? host.ExecuteLocal(command) : host.Request(2,
                System.Text.Json.JsonSerializer.Serialize(command, WireJson.Options), (ulong)sequences[player] * 1000)!);
        }
        Send(1, new(GameplayAction.Start));
        var journal = new BattleOutcomeJournal();
        for (int decision = 0; decision < 200 && host.Phase is Phase.Building or Phase.Preparation; decision++)
        {
            journal.Observe(host.Snapshot());
            foreach (int player in new[] { 1, 2 })
                if (!host.Snapshot().Players.Single(p => p.Id == player).Ready)
                    Send(player, GameplayScenarios.NextPreparation(host.Snapshot(), player) ?? new(GameplayAction.Ready));
        }
        for (int step = 0; step < 18000 && host.Phase == Phase.Combat; step++) host.Step();
        Assert.Equal(Phase.RewardDraft, host.Phase);
        var city = host.Snapshot().Players.Single(p => p.Id == 1);
        Accept(host.ExecuteLocal(Command.FromSnapshot(host.Snapshot(), ++sequences[1], "reward:" + city.Run.Offers[0], 1)));
        Assert.Equal(Phase.RewardDraft, host.Phase); // Other city has not chosen yet.
        journal.Observe(host.Snapshot());
        var outcome = Assert.IsType<BattleOutcome>(journal.For(1));
        Assert.NotNull(outcome.Survived); Assert.Null(outcome.KeepDamage);
    }
    [Fact]
    public void MovingSquadObjectiveNeverResolvesAsAFriendlyMemberWithTheSameNumber()
    {
        var battle = new BattleShowcase("swordsman");
        var frame = battle.Frames.First(s => s.Players.SelectMany(p => p.Soldiers).Any(u => u.TargetKind == SquadTargetKind.None
            && u.Decision is { ObjectiveId: > 0, ObjectiveCity: false }));
        var units = frame.Players.SelectMany(p => p.Soldiers).Concat(frame.Enemies).ToArray();
        var actor = units.First(u => u.Faction == Faction.Adventurers && u.TargetKind == SquadTargetKind.None
            && u.Decision is { ObjectiveId: > 0, ObjectiveCity: false });
        var targets = CombatInspection.TargetMembers(actor, units);
        Assert.NotEmpty(targets); Assert.All(targets, u => Assert.NotEqual(actor.Faction, u.Faction));
        Assert.All(targets, u => Assert.Equal(actor.Decision!.ObjectiveId, u.SquadId));
        Assert.Contains("Approaching Enemy", CombatInspection.Target(actor, units, frame.Structures));
    }
}
