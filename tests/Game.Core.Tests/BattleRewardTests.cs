// SPDX-License-Identifier: GPL-3.0-only
using System.Text.Json;
using Game.Core;
using Xunit;

namespace Game.Core.Tests;

public sealed class BattleRewardTests
{
    private static CityState EmptyCity(int keep = 100) => new(1, true, false, 0, 0,
        HealthPoints.FromWhole(keep), [], [], 0);

    [Fact]
    public void RecoveryQuotesExactFixedPointLivingHealingIncludingStoredWithoutResurrection()
    {
        // Detached pure-calculator fixture, never an authoritative/native checkpoint.
        var profile = new CombatConfiguration(new Rules()).Unit(UnitType.Swordsman).Runtime();
        UnitState Member(int id, int health, bool stored = false) => new(id, health)
        { Profile = profile, Owner = 1, Assignment = new(stored ? 4 : -1, 1, 0, 1) };
        var city = EmptyCity(97) with { Soldiers = [Member(1, profile.Health - 1),
            Member(2, profile.Health - 123, true), Member(3, 0), Member(4, profile.Health)] };
        var quote = BattleRewards.Quote(city, "loot-recovery", 1, HealthPoints.FromWhole(100));
        Assert.Equal(HealthPoints.FromWhole(3), quote.KeepHealing);
        Assert.Equal([1, 2], quote.Soldiers.Select(s => s.UnitId));
        Assert.Equal([1, 25], quote.Soldiers.Select(s => s.Amount));
        Assert.Equal(26, quote.SoldierHealing); Assert.True(quote.HasBenefit);
        var healthy = BattleRewards.Quote(EmptyCity(), "loot-recovery", 19, HealthPoints.FromWhole(100));
        Assert.False(healthy.HasBenefit); Assert.Empty(healthy.Soldiers);
        var nearFallen = BattleRewards.Quote(EmptyCity(1), "loot-recovery", 19, HealthPoints.FromWhole(100));
        Assert.Equal(HealthPoints.FromWhole(10), nearFallen.KeepHealing);
    }
    [Theory]
    [InlineData("loot-timber", Resource.Wood, 4)]
    [InlineData("loot-provisions", Resource.Food, 8)]
    [InlineData("loot-equipment", Resource.Metal, 3)]
    [InlineData("loot-equipment", Resource.Cloth, 2)]
    [InlineData("loot-treasury", Resource.Gold, 5)]
    [InlineData("loot-stone", Resource.Stone, 4)]
    public void SupplyUsesCompletedWaveAndQuotesPartialCapacityExactly(string id, Resource resource, int amount)
    {
        foreach (var (wave, multiplier) in new[] { (1, 1), (6, 1), (7, 2), (13, 2), (14, 3), (19, 3) })
        {
            var quote = BattleRewards.Quote(EmptyCity(), id, wave, HealthPoints.FromWhole(100));
            Assert.Equal(amount * multiplier, quote.Resources.Amount(resource));
            Assert.False(quote.ResourceCapped);
        }
        var capped = EmptyCity() with { Gold = int.MaxValue - 1, Wood = int.MaxValue - 1,
            Food = int.MaxValue - 1, Stone = int.MaxValue - 1, Metal = int.MaxValue - 1, Cloth = int.MaxValue - 1 };
        var partial = BattleRewards.Quote(capped, id, 19, HealthPoints.FromWhole(100));
        Assert.Equal(1, partial.Resources.Amount(resource)); Assert.True(partial.ResourceCapped);
        Assert.True(capped.Resources.TryAdd(partial.Resources, out _));
    }
    [Fact]
    public void FullyCappedStocksCannotMasqueradeAsThreePositiveSupplyChoices()
    {
        var full = new ResourceCost(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue);
        Assert.False(BattleRewards.HasSupplyCapacity(full, 19));
        Assert.True(BattleRewards.HasSupplyCapacity(full with { Wood = int.MaxValue - 1, Food = int.MaxValue - 1, Gold = int.MaxValue - 1 }, 19));
    }
    [Fact]
    public void AcceptedQuoteMatchesActualReceiptAndCannotApplyTwiceAfterSameFrameProgression()
    {
        using var paid = new PaidDraft(20261018);
        var before = paid.Session.Snapshot(); var city = before.Players.Single();
        var quote = city.Run.Quotes.First(q => q.Resources != default);
        var command = Command.FromSnapshot(before, paid.Next(), "reward:" + quote.Id, city.Id);
        var accepted = paid.Session.ExecuteLocal(command); Assert.True(accepted.Accepted, accepted.Message);
        var after = paid.Session.Snapshot(); var granted = after.Players.Single();
        Assert.Equal(Phase.Building, after.Phase); Assert.Equal(2, after.Wave);
        Assert.True(city.Resources.TryAdd(quote.Resources, out var expected)); Assert.Equal(expected, granted.Resources);
        var receipt = Assert.Single(granted.Run.Rewards); Assert.Equal(quote.Id, receipt.Id);
        Assert.Equal(quote.Resources, receipt.Resources); Assert.Equal(1, receipt.Wave);
        Assert.Equal(city.LastReward, granted.LastReward); Assert.Equal(city.LastBattleLoss, granted.LastBattleLoss);
        Assert.Equal(city.ProductionIncome, granted.ProductionIncome);
        Assert.Equal(city.Soldiers.Select(u => u.Profile), granted.Soldiers.Select(u => u.Profile));
        string settled = JsonSerializer.Serialize(after, WireJson.Options);
        Assert.Equal(accepted, paid.Session.ExecuteLocal(command));
        Assert.Equal(settled, JsonSerializer.Serialize(paid.Session.Snapshot(), WireJson.Options));
        Assert.False(paid.Session.ExecuteLocal(command with { Sequence = paid.Next() }).Accepted);
        Assert.Equal(settled, JsonSerializer.Serialize(paid.Session.Snapshot(), WireJson.Options));
    }
    [Fact]
    public void PausingAndDetailsCannotRerollOrConsumeTheQuotedBenefit()
    {
        using var paid = new PaidDraft(1);
        var initial = paid.Session.Snapshot();
        string quotes = JsonSerializer.Serialize(initial.Players.Single().Run.Quotes, WireJson.Options);
        for (int n = 0; n < 20; n++)
        {
            var state = paid.Session.Snapshot();
            _ = RunPresentation.Project(state, state.Players.Single(), 1, true, false);
        }
        paid.Send("pause"); var paused = paid.Session.Snapshot();
        var rejected = paid.Session.ExecuteLocal(Command.FromSnapshot(paused, paid.Next(), "reward:" + paused.Players.Single().Run.Offers[0], 1));
        Assert.False(rejected.Accepted); Assert.Empty(paid.Session.Snapshot().Players.Single().Run.Rewards);
        paid.Send("resume");
        Assert.Equal(quotes, JsonSerializer.Serialize(paid.Session.Snapshot().Players.Single().Run.Quotes, WireJson.Options));
    }
    [Fact]
    public void ActualRecoveryAppliesFrozenMemberAmountsAndPreservesTheBattleLossReceipt()
    {
        for (ulong seed = 1; seed <= 32; seed++)
        {
            using var paid = new PaidDraft(seed);
            var before = paid.Session.Snapshot().Players.Single();
            var quote = before.Run.Quotes.FirstOrDefault(q => q.Id == "loot-recovery");
            if (quote is null) continue;
            Assert.NotEmpty(quote.Soldiers);
            var loss = before.LastBattleLoss;
            int first = quote.Soldiers[0].UnitId;
            // Mutating a detached nested quote must not change authority.
            quote.Soldiers[0] = quote.Soldiers[0] with { Amount = int.MaxValue };
            var authoritative = paid.Session.Snapshot().Players.Single().Run.Quotes.Single(q => q.Id == quote.Id);
            Assert.Equal(first, authoritative.Soldiers[0].UnitId);
            Assert.Equal(RunEffects.Portion(authoritative.Soldiers[0].Maximum - authoritative.Soldiers[0].Before, 20), authoritative.Soldiers[0].Amount);
            paid.Send("reward:" + quote.Id);
            var after = paid.Session.Snapshot().Players.Single();
            Assert.Equal(loss, after.LastBattleLoss);
            Assert.Equal(authoritative.KeepHealing, after.Health - before.Health);
            foreach (var member in authoritative.Soldiers)
                Assert.Equal(member.Before + member.Amount, after.Soldiers.Single(u => u.Id == member.UnitId).Health);
            var receipt = Assert.Single(after.Run.Rewards);
            Assert.Equal(authoritative.SoldierHealing, receipt.SoldierHealing);
            Assert.Equal(authoritative.Soldiers.Length, receipt.SoldierCount);
            return;
        }
        Assert.Fail("No offered recovery found in the bounded legal opening search.");
    }
    [Fact]
    public void ExtremeLegalStocksRejectFinalReadyAtomicallyUntilOrdinarySpendingCreatesChoiceCapacity()
    {
        // Explicit custom arithmetic fixture; not a default campaign/balance capture.
        var defaults = new Rules(); var economy = new EconomyConfiguration(defaults);
        ResourceCost spent = default;
        foreach (Building type in new[] { Building.Farm, Building.Lumbermill })
        { Assert.True(spent.TryAdd(economy.Buildings().Single(b => b.Type == type).Construction, out var next)); spent = next; }
        WaveDefinition[] waves = defaults.Campaign.Waves.Take(2).Select(w => w with { Entries = w.Entries.ToArray() }).ToArray();
        waves[0] = waves[0] with { Reward = new(Food: 1) };
        var rules = defaults with { StartingGold = int.MaxValue - 150 + spent.Gold + 2,
            StartingWood = int.MaxValue - 150 + spent.Wood, BaseGold = 50, WoodOutput = 50,
            FarmOutput = (int.MaxValue - 1) / 3, EnemyPowerPercent = 1, Campaign = new(waves) };
        using var host = new AuthoritySession(AuthorityPolicy.Solo, rules: rules, matchId: "capacity-edge");
        long sequence = 0;
        CommandResult Send(string action, int slot = -1, Building building = Building.Empty)
            => host.ExecuteLocal(Command.FromSnapshot(host.Snapshot(), ++sequence, action, 1, slot, building));
        Assert.True(Send("start").Accepted); Assert.True(Send("build", 0, Building.Farm).Accepted);
        Assert.True(Send("build", 1, Building.Lumbermill).Accepted);
        Assert.True(Send("ready").Accepted); Assert.True(Send("ready").Accepted);
        string before = JsonSerializer.Serialize(host.Snapshot(), WireJson.Options);
        var rejected = Send("ready"); Assert.False(rejected.Accepted); Assert.Contains("three immediate", rejected.Message);
        Assert.Equal(before, JsonSerializer.Serialize(host.Snapshot(), WireJson.Options));
        Assert.True(Send("buy-home").Accepted); Assert.True(Send("ready").Accepted);
        Assert.Equal(Phase.Combat, host.Phase);
        for (int step = 0; step < 18000 && host.Phase == Phase.Combat; step++) host.Step();
        Assert.Equal(Phase.RewardDraft, host.Phase);
        var state = host.Snapshot(); SnapshotCompatibility.RequireSharedArena(state);
        Assert.Equal(3, state.Players.Single().Run.Quotes.Length);
        Assert.All(state.Players.Single().Run.Quotes, q => Assert.True(q.HasBenefit));
    }
    [Fact]
    public void PreviousFrozenResultRemainsValidWhileTheNextCombatHasANewUpkeepManifest()
    {
        using var paid = new PaidDraft(1);
        paid.Send("reward:" + paid.Session.Snapshot().Players.Single().Run.Offers[0]);
        var old = paid.Session.Snapshot().Players.Single().LastBattleLoss;
        paid.Send("ready"); paid.Send("ready"); paid.Send("ready");
        var fighting = paid.Session.Snapshot(); Assert.Equal(Phase.Combat, fighting.Phase);
        Assert.Equal(2, fighting.Players.Single().LastUpkeep!.Wave);
        Assert.Equal(old, fighting.Players.Single().LastBattleLoss);
        SnapshotCompatibility.RequireSharedArena(fighting);
    }
    [Theory]
    [InlineData("loot-timber")]
    [InlineData("loot-provisions")]
    [InlineData("loot-equipment")]
    [InlineData("loot-treasury")]
    [InlineData("loot-stone")]
    [InlineData("loot-recovery")]
    public void EveryFamilyIsAppliedByAnActualAuthoritativeChoiceWithItsExactReceipt(string id)
    {
        for (ulong seed = 1; seed <= 32; seed++)
        {
            using var paid = new PaidDraft(seed);
            var before = paid.Session.Snapshot().Players.Single();
            var quote = before.Run.Quotes.FirstOrDefault(q => q.Id == id);
            if (quote is null) continue;
            paid.Send("reward:" + id); var after = paid.Session.Snapshot().Players.Single();
            Assert.True(before.Resources.TryAdd(quote.Resources, out var stock)); Assert.Equal(stock, after.Resources);
            var actual = Assert.Single(after.Run.Rewards);
            Assert.Equal(new BattleRewardReceipt(1, id, quote.Resources, quote.KeepHealing,
                quote.SoldierHealing, quote.Soldiers.Length, quote.ResourceCapped), actual);
            Assert.Equal(before.Health + quote.KeepHealing, after.Health);
            foreach (var member in before.Soldiers)
            {
                int healing = quote.Soldiers.FirstOrDefault(h => h.UnitId == member.Id)?.Amount ?? 0;
                Assert.Equal(member.Health + healing, after.Soldiers.Single(u => u.Id == member.Id).Health);
            }
            return;
        }
        Assert.Fail("No offered family found in the bounded legal opening search: " + id);
    }
    private sealed class PaidDraft : IDisposable
    {
        public AuthoritySession Session { get; }
        private long _sequence;
        public long Next() => ++_sequence;
        public PaidDraft(ulong seed)
        {
            Session = new(AuthorityPolicy.Solo, matchId: "immediate-reward-test", combatSeed: seed);
            Send("start"); Send("build", 0, Building.Farm); Send("build", 1, Building.Barracks);
            Send("build", 2, Building.MetalMine); Send("build", 3, Building.Lumbermill, ConstructionPayment.GoldRecovery);
            for (int turn = 0; turn < 2; turn++) { Send("ready"); Send("recruit", 1); Send("recruit", 1); }
            Send("ready");
            for (int step = 0; step < 18000 && (Session.Phase == Phase.Combat || Session.Snapshot().DyingBodies.Length > 0); step++) Session.Step();
            Assert.Equal(Phase.RewardDraft, Session.Phase);
        }
        public void Send(string action, int slot = -1, Building building = Building.Empty, ConstructionPayment payment = ConstructionPayment.Standard)
        {
            var result = Session.ExecuteLocal(Command.FromSnapshot(Session.Snapshot(), Next(), action, 1, slot, building, payment: payment));
            Assert.True(result.Accepted, result.Message);
        }
        public void Dispose() => Session.Dispose();
    }
}
