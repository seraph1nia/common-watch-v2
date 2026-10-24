// SPDX-License-Identifier: GPL-3.0-only
using System.Text.Json;
using Game.Core;
using Xunit;

namespace Game.Shared.Tests;

public sealed class ImmediateRewardAuthorityTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IndependentChoicesAndExactRemoteRetryGrantOnceAcrossReconnect(bool reconnectBeforeChoice)
    {
        using var host = new AuthoritySession(AuthorityPolicy.PlayingHost, matchId: "reward-retry", combatSeed: 1);
        var guest = host.Admit(2, WireJson.ProtocolVersion, "", 0); Assert.True(guest.Accepted);
        long[] sequences = [0, 0, 0]; int peer = 2;
        Command Capture(int player, string action) => Command.FromSnapshot(host.Snapshot(), ++sequences[player], action, player);
        CommandResult Execute(int player, Command command) => player == 1 ? host.ExecuteLocal(command)
            : host.Request(peer, JsonSerializer.Serialize(command, WireJson.Options), (ulong)command.Sequence * 1000)!;
        void Accept(int player, Command command) { var result = Execute(player, command); Assert.True(result.Accepted, result.Message); }
        Accept(1, Capture(1, "start"));
        for (int n = 0; n < 300 && host.Phase is Phase.Building or Phase.Preparation; n++)
            foreach (int player in new[] { 1, 2 })
            {
                var state = host.Snapshot(); var city = state.Players.Single(c => c.Id == player);
                if (!city.Ready)
                {
                    var intent = GameplayScenarios.NextPreparation(state, player) ?? new GameplayIntent(GameplayAction.Ready);
                    Accept(player, intent.Capture(state, ++sequences[player], player));
                }
            }
        Assert.Equal(Phase.Combat, host.Phase);
        for (int tick = 0; tick < 18000 && (host.Phase == Phase.Combat || host.Snapshot().DyingBodies.Length > 0); tick++) host.Step();
        Assert.Equal(Phase.RewardDraft, host.Phase);
        var before = host.Snapshot();
        if (reconnectBeforeChoice)
        {
            host.Disconnect(peer); peer = 3;
            var resumed = host.Admit(peer, WireJson.ProtocolVersion, guest.Credential, 1000);
            Assert.True(resumed.Accepted); Assert.Equal(2, resumed.PlayerId);
        }
        foreach (int player in new[] { 1, 2 })
        {
            var original = before.Players.Single(c => c.Id == player);
            var current = host.Snapshot().Players.Single(c => c.Id == player);
            Assert.Equal(JsonSerializer.Serialize(original.Run.Quotes, WireJson.Options), JsonSerializer.Serialize(current.Run.Quotes, WireJson.Options));
        }
        var local = before.Players.Single(c => c.Id == 1); var remote = before.Players.Single(c => c.Id == 2);
        Accept(1, Capture(1, "reward:" + local.Run.Offers[0]));
        Assert.Equal(Phase.RewardDraft, host.Phase);
        var remoteCommand = Capture(2, "reward:" + remote.Run.Offers[0]);
        var result = Execute(2, remoteCommand); Assert.True(result.Accepted, result.Message);
        Assert.Equal(Phase.Building, host.Phase);
        if (!reconnectBeforeChoice)
        {
            host.Disconnect(peer); peer = 3;
            Assert.True(host.Admit(peer, WireJson.ProtocolVersion, guest.Credential, 2000).Accepted);
        }
        var after = host.Snapshot(); string settled = JsonSerializer.Serialize(after, WireJson.Options);
        Assert.Equal(result, Execute(2, remoteCommand));
        Assert.Equal(settled, JsonSerializer.Serialize(host.Snapshot(), WireJson.Options));
        foreach (var original in before.Players)
        {
            var actual = after.Players.Single(c => c.Id == original.Id);
            var quote = original.Run.Quotes[0]; var receipt = Assert.Single(actual.Run.Rewards);
            Assert.True(original.Resources.TryAdd(quote.Resources, out var stock)); Assert.Equal(stock, actual.Resources);
            Assert.Equal(quote.Id, receipt.Id); Assert.Equal(quote.Resources, receipt.Resources);
            Assert.Equal(quote.KeepHealing, receipt.KeepHealing); Assert.Equal(quote.SoldierHealing, receipt.SoldierHealing);
        }
    }
}
