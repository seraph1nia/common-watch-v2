using System.Text.Json;
using Game.Core;
using Xunit;

namespace Game.Core.Tests;

public sealed class VerticalSliceTests
{
    private static string Replay(ulong seed)
    {
        using var authority = new AuthoritySession(AuthorityPolicy.Solo, matchId: "vertical-slice-test", combatSeed: seed);
        long sequence = 0;
        void Act(string action, int slot = -1, Building building = Building.Empty,
            ConstructionPayment payment = ConstructionPayment.Standard)
        {
            Command command = Command.FromSnapshot(authority.Snapshot(), ++sequence, action, authority.LocalPlayerId, slot, building, payment: payment);
            CommandResult result = authority.ExecuteLocal(command);
            Assert.True(result.Accepted, result.Message);
        }
        MatchSnapshot initial = authority.Snapshot();
        Assert.Equal(Phase.Lobby, initial.Phase);
        Assert.Equal(9, initial.Players[0].Slots.Length);
        Act("start");
        Act("build", 0, Building.Farm); Act("build", 1, Building.Barracks); Act("build", 2, Building.MetalMine);
        Act("build", 3, Building.Lumbermill, ConstructionPayment.GoldRecovery);
        Assert.Equal(0, authority.Snapshot().Players[0].Wood);
        for (int preparation = 1; preparation <= 2; preparation++)
        {
            Act("ready");
            Assert.Equal(preparation, authority.Snapshot().ProductionCount);
            Assert.Equal(preparation * 5, authority.Snapshot().Players[0].Food);
            Act("recruit", 1); Act("recruit", 1);
        }
        Act("ready");
        Assert.Equal(Phase.Combat, authority.Phase);
        Assert.Equal(3, authority.Snapshot().ProductionCount);
        bool damage = false, death = false, movement = false;
        for (int i = 0; i < 10000 && authority.Phase == Phase.Combat; i++)
        {
            authority.Step();
            MatchSnapshot frame = authority.Snapshot();
            damage |= frame.CombatEvents.Any(e => e.Type == CombatEventType.Hit);
            death |= frame.CombatEvents.Any(e => e.Type == CombatEventType.Death);
            movement |= frame.Squads.Any(s => s.Hex.HoldsTransit);
        }
        Assert.True(damage && death && movement);
        Assert.Equal(Phase.RewardDraft, authority.Phase);
        string[] offers = authority.Snapshot().Players[0].Run.Offers;
        Assert.Equal(3, offers.Length);
        Act("reward:" + offers[0]);
        MatchSnapshot final = authority.Snapshot();
        Assert.Equal(Phase.Building, final.Phase);
        Assert.Equal(2, final.Wave);
        Assert.Contains(final.Players[0].Run.Rewards, c => c.Id == offers[0] && c.Wave == 1);
        return JsonSerializer.Serialize(final, WireJson.Options);
    }

    [Fact]
    public void PaidSoloOpeningWinsAndDraftsDeterministically()
    {
        Assert.Equal(Replay(20261018), Replay(20261018));
    }
}
