// SPDX-License-Identifier: GPL-3.0-only
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;
using Game.Core;
using Xunit;

namespace Game.Shared.Tests;

public sealed class ImmediateRewardCompatibilityTests
{
    [Fact]
    public void MissingHistoricalRewardVersionAndOldProtocolRequireFreshCompatibleSessions()
    {
        using var solo = ScenarioRunner.CreateAt("draft");
        JsonObject json = JsonSerializer.SerializeToNode(solo.Snapshot, WireJson.Options)!.AsObject();
        Assert.True(json.Remove("RewardRulesVersion"));
        using var buffer = new MemoryStream();
        using (var compressor = new BrotliStream(buffer, CompressionLevel.Fastest, leaveOpen: true))
            compressor.Write(JsonSerializer.SerializeToUtf8Bytes(json, WireJson.Options));
        Assert.Throws<InvalidDataException>(() => SnapshotPayload.DecodeForReceiving(Convert.ToBase64String(buffer.ToArray())));
        Assert.Throws<InvalidDataException>(() => SnapshotPayload.DecodeForReceiving(SnapshotPayload.Encode(solo.Snapshot with { RewardRulesVersion = BattleRewards.RulesVersion + 1 })));
        using var host = new AuthoritySession(AuthorityPolicy.PlayingHost);
        Assert.False(host.Admit(2, 17, "", 0).Accepted);
        Assert.True(host.Admit(2, WireJson.ProtocolVersion, "", 0).Accepted);
    }
    [Fact]
    public void ReceivingRefusesReorderedOrChangedFrozenOffersRatherThanReinterpretingTheChoice()
    {
        using var solo = ScenarioRunner.CreateAt("draft"); var state = solo.Snapshot;
        MatchSnapshot Change(Func<RunState, RunState> change) => state with
        { Players = state.Players.Select(c => c with { Run = change(c.Run) }).ToArray() };
        var reordered = Change(r => r with { Offers = r.Offers.Reverse().ToArray() });
        Assert.Throws<InvalidDataException>(() => SnapshotPayload.DecodeForReceiving(SnapshotPayload.Encode(reordered)));
        var oldId = Change(r => r with { Quotes = r.Quotes.Select((q, index) => index == 0 ? q with { Id = "F01" } : q).ToArray() });
        Assert.Throws<InvalidDataException>(() => SnapshotPayload.DecodeForReceiving(SnapshotPayload.Encode(oldId)));
        var altered = Change(r => r with { Quotes = r.Quotes.Select((q, index) => index == 0 ? q with { Resources = q.Resources with { Food = int.MaxValue } } : q).ToArray() });
        Assert.Throws<InvalidDataException>(() => SnapshotPayload.DecodeForReceiving(SnapshotPayload.Encode(altered)));
        var roundtrip = SnapshotPayload.DecodeForReceiving(SnapshotPayload.Encode(state));
        Assert.Equal(JsonSerializer.Serialize(state, WireJson.Options), JsonSerializer.Serialize(roundtrip, WireJson.Options));
    }
    [Fact]
    public void ConsumedReceiptAndFrozenLossAreValidatedAfterImmediateNextPlanning()
    {
        using var solo = ScenarioRunner.CreateAt("draft");
        Assert.True(solo.ChooseReward(solo.City.Run.Offers[0]).Accepted);
        var state = solo.Snapshot;
        Assert.Equal(Phase.Building, state.Phase);
        var badReceipt = state with { Players = state.Players.Select(c => c with
            { Run = c.Run with { Rewards = c.Run.Rewards.Select(r => r with { Wave = 0 }).ToArray() } }).ToArray() };
        Assert.Throws<InvalidDataException>(() => SnapshotPayload.DecodeForReceiving(SnapshotPayload.Encode(badReceipt)));
        var badLoss = state with { Players = state.Players.Select(c => c with
            { LastBattleLoss = c.LastBattleLoss! with { KeepDamage = int.MaxValue } }).ToArray() };
        Assert.Throws<InvalidDataException>(() => SnapshotPayload.DecodeForReceiving(SnapshotPayload.Encode(badLoss)));
        SnapshotCompatibility.RequireSharedArena(SnapshotPayload.DecodeForReceiving(SnapshotPayload.Encode(state)));
    }
}
