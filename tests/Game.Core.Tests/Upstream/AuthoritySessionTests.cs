using System.Text.Json;
using Game.Core;
using Xunit;

namespace Game.Core.Tests;

public sealed class AuthoritySessionTests
{
    private static Command Cmd(AuthoritySession session, int player, long sequence, string action,
        int slot = -1, Building building = Building.Empty)
    {
        MatchSnapshot state = session.Snapshot();
        return new(sequence, state.MatchId, state.Phase, state.TurnSerial, action, player, slot, building, ExpectedGeneration: slot is >= 0 and < 9 ? state.Players.FirstOrDefault(p => p.Id == player)?.Slots[slot].Generation ?? 0 : 0, ExpectedExpansionCount: (state.Players.FirstOrDefault(p => p.Id == player)?.Slots.Count(s => s.Purchased) ?? 5) - 5);
    }
    private static CommandResult Remote(AuthoritySession session, int peer, Command command, ulong time = 1000)
        => session.Request(peer, JsonSerializer.Serialize(command, WireJson.Options), time)!;
    private static AdmissionResult Join(AuthoritySession session, int peer, string credential = "", string? identity = null)
        => session.Admit(peer, WireJson.ProtocolVersion, credential, 1000, identity);
    private static string State(AuthoritySession session) => JsonSerializer.Serialize(session.Snapshot(), WireJson.Options);

    [Theory]
    [InlineData(AuthorityPolicy.Solo)]
    [InlineData(AuthorityPolicy.PlayingHost)]
    [InlineData(AuthorityPolicy.Dedicated)]
    public void ExplicitCombatSeedSurvivesAdmissionAndEndedSnapshot(AuthorityPolicy policy)
    {
        using var session = new AuthoritySession(policy, combatSeed: ulong.MaxValue);
        Assert.Equal(ulong.MaxValue, session.Snapshot().CombatSeed);
        if (policy != AuthorityPolicy.Solo)
        {
            AdmissionResult admission = Join(session, 2);
            Assert.True(admission.Accepted); Assert.Equal(ulong.MaxValue, admission.State!.CombatSeed);
        }
        session.End(); Assert.Equal(ulong.MaxValue, session.Snapshot().CombatSeed);
    }

    [Fact]
    public void SoloBindsOneCityAndDedicatedHasNoLocalController()
    {
        using var solo = new AuthoritySession(AuthorityPolicy.Solo);
        Assert.Equal(1, solo.LocalPlayerId);
        Assert.Single(solo.Snapshot().Players);
        Assert.Equal(9, solo.Snapshot().Players[0].Slots.Length);
        Assert.False(Join(solo, 2).Accepted);
        Assert.True(solo.ExecuteLocal(Cmd(solo, 1, 1, "start")).Accepted);
        Assert.Equal(Phase.Building, solo.Snapshot().Phase);
        using var dedicated = new AuthoritySession(AuthorityPolicy.Dedicated);
        Assert.Equal(0, dedicated.LocalPlayerId);
        Assert.Empty(dedicated.Snapshot().Players);
        Assert.False(dedicated.ExecuteLocal(Cmd(dedicated, 0, 1, "start")).Accepted);
    }

    [Fact]
    public void HostedStartBelongsOnlyToOriginalLocalHostAndCapacityIsFour()
    {
        using var session = new AuthoritySession(AuthorityPolicy.PlayingHost);
        for (int peer = 2; peer <= 4; peer++) Assert.True(Join(session, peer).Accepted);
        Assert.Equal(4, session.Snapshot().Players.Length);
        Assert.False(Join(session, 5).Accepted);
        Assert.True(session.CanStart(session.LocalPlayerId));
        Assert.False(session.CanStart(2));
        string before = State(session);
        Assert.False(Remote(session, 2, Cmd(session, 2, 1, "start")).Accepted);
        Assert.Equal(before, State(session));
        Assert.True(session.ExecuteLocal(Cmd(session, 1, 1, "start")).Accepted);
        Assert.False(Join(session, 6).Accepted);
        Assert.False(session.CanStart(1));
        Assert.Equal(4, session.Snapshot().Players.Length);
    }

    [Fact]
    public void DedicatedConnectedMemberCanStartAndDisconnectedLobbyCityCannotResumeAfterRosterLocks()
    {
        using var session = new AuthoritySession(AuthorityPolicy.Dedicated);
        AdmissionResult first = Join(session, 2);
        AdmissionResult second = Join(session, 3);
        Assert.Equal(first.PlayerId, session.Disconnect(2));
        Assert.True(session.CanStart(second.PlayerId));
        Assert.True(Remote(session, 3, Cmd(session, second.PlayerId, 1, "start")).Accepted);
        Assert.Single(session.Snapshot().Players);
        Assert.False(Join(session, 4, first.Credential).Accepted);
        Assert.False(Join(session, 5).Accepted);
    }

    [Theory]
    [InlineData("foreign-city")]
    [InlineData("stale-turn")]
    [InlineData("stale-match")]
    [InlineData("invalid-building")]
    [InlineData("invalid-slot")]
    [InlineData("unknown-action")]
    [InlineData("unaffordable")]
    public void LocalAndRemoteRejectInvalidRequestsWithIdenticalFeedbackAndNoMutation(string defect)
    {
        CommandResult[] results = new CommandResult[2];
        for (int delivery = 0; delivery < 2; delivery++)
        {
            using var session = new AuthoritySession(AuthorityPolicy.PlayingHost);
            int guest = Join(session, 2).PlayerId;
            Assert.True(session.ExecuteLocal(Cmd(session, 1, 1, "start")).Accepted);
            int player = delivery == 0 ? 1 : guest;
            CommandResult Send(Command command) => delivery == 0 ? session.ExecuteLocal(command) : Remote(session, 2, command);
            if (defect == "unaffordable")
                for (int slot = 0; slot < 3; slot++) Assert.True(Send(Cmd(session, player, slot + 2, "build", slot, Building.Mine)).Accepted);
            Command invalid = Cmd(session, player, 10, "build", 3, Building.Mine);
            invalid = defect switch
            {
                "foreign-city" => invalid with { City = player == 1 ? guest : 1 },
                "stale-turn" => invalid with { TurnSerial = 0 },
                "stale-match" => invalid with { MatchId = "previous-match" },
                "invalid-building" => invalid with { Building = (Building)100 },
                "invalid-slot" => invalid with { Slot = 9 },
                "unknown-action" => invalid with { Action = "unknown" },
                _ => invalid
            };
            string before = State(session);
            results[delivery] = Send(invalid);
            Assert.False(results[delivery].Accepted);
            Assert.Equal(before, State(session));
        }
        Assert.Equal(results[0], results[1]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RecruitmentRetryKeepsOneChargeAndOneSoldierAcrossResume(bool remote)
    {
        using var session = new AuthoritySession(remote ? AuthorityPolicy.Dedicated : AuthorityPolicy.Solo);
        AdmissionResult? admitted = remote ? Join(session, 2) : null;
        int player = admitted?.PlayerId ?? session.LocalPlayerId;
        int peer = 2;
        CommandResult Send(Command command) => remote ? Remote(session, peer, command) : session.ExecuteLocal(command);
        Assert.True(Send(Cmd(session, player, 1, "start")).Accepted);
        Assert.True(Send(Cmd(session, player, 2, "build", 0, Building.Farm)).Accepted);
        Assert.True(Send(Cmd(session, player, 3, "build", 1, Building.Barracks)).Accepted);
        Assert.True(Send(Cmd(session, player, 4, "build", 2, Building.MetalMine)).Accepted);
        Assert.True(Send(Cmd(session, player, 5, "ready")).Accepted);
        Command request = Cmd(session, player, 6, "recruit", 1);
        CommandResult accepted = Send(request);
        Assert.True(accepted.Accepted);
        if (remote)
        {
            session.Disconnect(peer); peer = 3;
            Assert.Equal(player, Join(session, peer, admitted!.Credential).PlayerId);
        }
        string beforeRetry = State(session);
        Assert.Equal(accepted, Send(request));
        Assert.Equal(beforeRetry, State(session));
        CityState city = session.Snapshot().Players.Single();
        Assert.Equal(5, city.Food);
        Assert.Equal(new EconomyConfiguration(new Rules()).Buildings().Single(b => b.Type == Building.MetalMine).LevelOneOutput - new EconomyConfiguration(new Rules()).Recruitment(UnitType.Swordsman, 1).Metal, city.Metal);
        Assert.Single(city.Soldiers);
    }

    [Fact]
    public void InvalidConcurrentAndWrongIdentityResumeNeverReplaceTheController()
    {
        using var session = new AuthoritySession(AuthorityPolicy.PlayingHost, originalHostIdentity: "host", requireTrustedIdentity: true);
        Assert.False(Join(session, 2).Accepted);
        Assert.False(Join(session, 2, identity: "host").Accepted);
        AdmissionResult guest = Join(session, 2, identity: "guest");
        Assert.True(guest.Accepted);
        string before = State(session);
        Assert.False(Join(session, 3, "invalid", "guest").Accepted);
        Assert.False(Join(session, 3, guest.Credential, "guest").Accepted);
        Assert.False(Join(session, 3, guest.Credential, "other").Accepted);
        Assert.False(Join(session, 3, identity: "guest").Accepted);
        Assert.Equal(before, State(session));
        Assert.True(session.ExecuteLocal(Cmd(session, 1, 1, "start")).Accepted);
        Assert.True(Remote(session, 2, Cmd(session, guest.PlayerId, 1, "build", 0, Building.Mine)).Accepted);
        session.Disconnect(2);
        before = State(session);
        Assert.False(Join(session, 3, guest.Credential, "other").Accepted);
        Assert.Equal(before, State(session));
        Assert.Equal(guest.PlayerId, Join(session, 3, guest.Credential, "guest").PlayerId);
        Assert.Equal("host", session.OriginalHostIdentity);
        Assert.DoesNotContain(guest.Credential, State(session));
        Assert.DoesNotContain(guest.Credential, guest.ToString());
    }

    [Fact]
    public void ResumeDuringPauseAndAfterEliminationRetainsCityAndLedger()
    {
        using var session = new AuthoritySession(AuthorityPolicy.Dedicated, new Rules { CityHealth = 3, DefenderDamage = 0, Arena = new() { WatchpostHealth = 1 }, Campaign = CampaignFixture.Three(first: 1) });
        AdmissionResult player = Join(session, 2);
        Assert.True(Remote(session, 2, Cmd(session, player.PlayerId, 1, "start")).Accepted);
        for (int sequence = 2; sequence <= 4; sequence++) Assert.True(Remote(session, 2, Cmd(session, player.PlayerId, sequence, "ready")).Accepted);
        Assert.True(Remote(session, 2, Cmd(session, player.PlayerId, 6, "pause")).Accepted);
        session.Disconnect(2);
        Assert.Equal(player.PlayerId, Join(session, 3, player.Credential).PlayerId);
        Assert.True(session.Snapshot().Paused);
        long tick = session.Snapshot().Tick;
        session.Step(); Assert.Equal(tick, session.Snapshot().Tick);
        Assert.True(Remote(session, 3, Cmd(session, player.PlayerId, 7, "resume")).Accepted);
        for (int steps = 0; steps < 1200 && session.Snapshot().Phase == Phase.Combat; steps++) session.Step();
        Assert.True(session.Snapshot().Players[0].Eliminated);
        session.Disconnect(3);
        AdmissionResult observer = Join(session, 4, player.Credential);
        Assert.True(observer.Accepted);
        Assert.Equal(player.PlayerId, observer.PlayerId);
        Assert.True(observer.State!.Players[0].Eliminated);
        Assert.Single(observer.State.Players);
    }

    [Fact]
    public void AuthenticatedHostedResumeRetainsCityAndRetryLedgerDuringPauseAndAfterElimination()
    {
        // The delivery adapter supplies these trusted identities; SDK lookup itself remains
        // separate Steam acceptance. Accelerated rules reach elimination through normal steps.
        using var session = new AuthoritySession(AuthorityPolicy.PlayingHost,
            new Rules { CityHealth = 3, DefenderDamage = 0, Arena = new() { WatchpostHealth = 1 }, Campaign = CampaignFixture.Three(first: 1) },
            originalHostIdentity: "original-host", requireTrustedIdentity: true);
        AdmissionResult guest = Join(session, 2, identity: "guest-account");
        Assert.True(guest.Accepted);
        Assert.True(session.ExecuteLocal(Cmd(session, 1, 1, "start")).Accepted);
        Command purchase = Cmd(session, guest.PlayerId, 1, "build", 0, Building.Farm);
        CommandResult purchased = Remote(session, 2, purchase);
        Assert.True(purchased.Accepted);
        for (int turn = 0; turn < 3; turn++)
        {
            Assert.True(session.ExecuteLocal(Cmd(session, 1, turn + 2, "ready")).Accepted);
            Assert.True(Remote(session, 2, Cmd(session, guest.PlayerId, turn + 2, "ready")).Accepted);
        }
        Assert.Equal(Phase.Combat, session.Phase);
        Assert.True(session.ExecuteLocal(Cmd(session, 1, 6, "pause")).Accepted);
        session.Disconnect(2);
        string disconnected = State(session);
        Assert.False(Join(session, 3, guest.Credential, "different-account").Accepted);
        Assert.False(Join(session, 3, "wrong-token", "guest-account").Accepted);
        Assert.False(Join(session, 3, identity: "fresh-account").Accepted);
        Assert.Equal(disconnected, State(session));
        AdmissionResult resumed = session.Admit(4, WireJson.ProtocolVersion, guest.Credential, 1000,
            "guest-account", session.MatchId);
        Assert.True(resumed.Accepted);
        Assert.Equal(guest.PlayerId, resumed.PlayerId);
        Assert.Equal(guest.Credential, resumed.Credential);
        Assert.Equal(2, resumed.State!.Players.Length);
        CityState city = resumed.State.Players.Single(player => player.Id == guest.PlayerId);
        Assert.Equal(Building.Farm, city.Slots[0].Type); Assert.Equal(1, city.Slots[0].Level);
        Assert.True(city.Slots[0].Purchased); Assert.Equal(1, city.Slots[0].Generation);
        Assert.Equal(new ResourceCost(Wood: 2), city.Slots[0].Investment);
        Assert.Equal(new ResourceCost(Wood: 1), city.Slots[0].Refund);
        Assert.Equal(16, city.Gold); Assert.Equal(15, city.Food);
        string paused = State(session);
        Assert.Equal(purchased, Remote(session, 4, purchase));
        session.Step();
        Assert.Equal(paused, State(session));
        Assert.True(session.ExecuteLocal(Cmd(session, 1, 7, "resume")).Accepted);
        for (int steps = 0; steps < 1200 && session.Phase == Phase.Combat; steps++) session.Step();
        Assert.True(session.Snapshot().Players.Single(player => player.Id == guest.PlayerId).Eliminated);
        session.Disconnect(4);
        AdmissionResult observer = session.Admit(5, WireJson.ProtocolVersion, guest.Credential, 1000,
            "guest-account", session.MatchId);
        Assert.True(observer.Accepted);
        Assert.Equal(guest.PlayerId, observer.PlayerId);
        Assert.True(observer.State!.Players.Single(player => player.Id == guest.PlayerId).Eliminated);
        Assert.Equal(2, observer.State.Players.Length);
        string observed = State(session);
        Assert.Equal(purchased, Remote(session, 5, purchase));
        Assert.Equal(observed, State(session));
        Assert.Equal("original-host", session.OriginalHostIdentity);
    }

    [Fact]
    public void AuthenticatedReplacementAtSameHostRejectsOldMatchAndCredentialBeforeAllocatingCity()
    {
        using var original = new AuthoritySession(AuthorityPolicy.PlayingHost,
            originalHostIdentity: "same-original-host", requireTrustedIdentity: true);
        AdmissionResult previous = Join(original, 2, identity: "guest-account");
        Assert.True(previous.Accepted);
        original.End();
        using var replacement = new AuthoritySession(AuthorityPolicy.PlayingHost,
            originalHostIdentity: "same-original-host", requireTrustedIdentity: true);
        Assert.Equal(original.OriginalHostIdentity, replacement.OriginalHostIdentity);
        Assert.NotEqual(original.MatchId, replacement.MatchId);
        string before = State(replacement);
        Assert.False(replacement.Admit(2, WireJson.ProtocolVersion, previous.Credential, 1000,
            "guest-account", original.MatchId).Accepted);
        Assert.False(replacement.Admit(2, WireJson.ProtocolVersion, "", 1000,
            "guest-account", original.MatchId).Accepted);
        Assert.False(Join(replacement, 2, previous.Credential, "guest-account").Accepted);
        Assert.Equal(before, State(replacement));
        AdmissionResult current = replacement.Admit(2, WireJson.ProtocolVersion, "", 1000,
            "guest-account", replacement.MatchId);
        Assert.True(current.Accepted);
        Assert.NotEqual(previous.Credential, current.Credential);
        Assert.True(replacement.ExecuteLocal(Cmd(replacement, 1, 1, "start")).Accepted);
        Command stale = Cmd(replacement, current.PlayerId, 99999, "build", 0, Building.Farm)
            with
        { MatchId = original.MatchId };
        before = State(replacement);
        Assert.False(Remote(replacement, 2, stale).Accepted);
        Assert.Equal(before, State(replacement));
        Assert.True(Remote(replacement, 2, Cmd(replacement, current.PlayerId, 1, "build", 0, Building.Farm)).Accepted);
    }

    [Fact]
    public void ProtocolMalformedOversizedAndRateFailuresLeaveOtherControllersUsable()
    {
        using var session = new AuthoritySession(AuthorityPolicy.PlayingHost);
        string lobby = State(session);
        AdmissionResult incompatible = session.Admit(2, 11, "", 0);
        Assert.False(incompatible.Accepted); Assert.False(incompatible.Ignored);
        Assert.Equal("Protocol version mismatch.", incompatible.Message);
        Assert.Null(incompatible.State); Assert.Empty(incompatible.Credential); Assert.Equal(0, incompatible.PlayerId);
        Assert.Equal(lobby, State(session));
        AdmissionResult current = session.Admit(3, WireJson.ProtocolVersion, "", 0);
        Assert.True(current.Accepted);
        Assert.Contains(current.State!.TechnologyCatalog, node => node.Id == TechnologyId.Stonecraft);
        Assert.Contains(current.State.TechnologyCatalog, node => node.Id == TechnologyId.ManagedForestry);
        Assert.Contains(current.State.TechnologyCatalog, node => node.Id == TechnologyId.Irrigation);
        session.Disconnect(3);
        lobby = State(session);
        incompatible = session.Admit(2, 11, current.Credential, 0);
        Assert.False(incompatible.Accepted); Assert.Equal("Protocol version mismatch.", incompatible.Message);
        Assert.Null(incompatible.State); Assert.Empty(incompatible.Credential); Assert.Equal(lobby, State(session));
        AdmissionResult resumed = session.Admit(3, WireJson.ProtocolVersion, current.Credential, 0);
        Assert.True(resumed.Accepted); Assert.Equal(current.PlayerId, resumed.PlayerId);
        Assert.Equal(current.Credential, resumed.Credential);
        Assert.True(session.ExecuteLocal(Cmd(session, 1, 1, "start")).Accepted);
        string before = State(session);
        Assert.False(session.Request(3, "{", 0)!.Accepted);
        Assert.False(session.Request(3, new string(' ', 2049), 0)!.Accepted);
        for (int attempt = 0; attempt < 61; attempt++) Assert.False(session.Request(3, "null", 0)!.Accepted);
        Assert.Equal("Command rate exceeded.", session.Request(3, "null", 0)!.Message);
        Assert.Equal(before, State(session));
        Assert.True(session.ExecuteLocal(Cmd(session, 1, 2, "build", 0, Building.Farm)).Accepted);
        Assert.True(Remote(session, 3, Cmd(session, 2, 1, "build", 0, Building.Farm), 1000).Accepted);
        Assert.Null(session.Request(9, "null", 1000));
    }

    [Fact]
    public void EndFreezesAuthorityAndNewMatchRejectsOldCredentialsAndCommands()
    {
        using var original = new AuthoritySession(AuthorityPolicy.PlayingHost);
        AdmissionResult guest = Join(original, 2);
        Command start = Cmd(original, 1, 1, "start");
        Assert.True(original.ExecuteLocal(start).Accepted);
        original.End(); original.End(); Assert.True(original.CombatReleased);
        string before = State(original); original.Step(); Assert.Equal(before, State(original));
        Assert.False(original.ExecuteLocal(start).Accepted);
        Assert.False(Join(original, 3, guest.Credential).Accepted);
        Assert.Null(original.Request(2, "null", 1000));
        using var replacement = new AuthoritySession(AuthorityPolicy.PlayingHost);
        Assert.NotEqual(original.MatchId, replacement.MatchId);
        before = State(replacement);
        Assert.False(Join(replacement, 2, guest.Credential).Accepted);
        Assert.False(replacement.Admit(3, WireJson.ProtocolVersion, "", 1000, expectedMatchId: original.MatchId).Accepted);
        Assert.False(replacement.ExecuteLocal(start with { Sequence = 99999 }).Accepted);
        Assert.Equal(before, State(replacement));
        Assert.True(replacement.ExecuteLocal(Cmd(replacement, 1, 1, "start")).Accepted);
    }
}
