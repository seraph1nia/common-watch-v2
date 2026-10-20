using System.Text.Json;
using Game.Core;

namespace Game.Shared;

/// <summary>Legal cooperative command replay for the modular-arena graphical probe.</summary>
internal static class ArenaShowcase
{
    public static IReadOnlyList<MatchSnapshot> Record(int players)
    {
        if (players is < 1 or > Match.MaximumPlayers) throw new ArgumentOutOfRangeException(nameof(players));
        using var authority = new AuthoritySession(AuthorityPolicy.PlayingHost, matchId: $"arena-capture-{players}", combatSeed: SoloSession.DefaultSeed);
        for (int peer = 2; peer <= players; peer++)
            if (!authority.Admit(peer, WireJson.ProtocolVersion, "", (ulong)peer * 1000).Accepted) throw new InvalidOperationException("Arena probe admission failed");
        long[] sequences = new long[players + 1];
        ulong time = 10000;
        void Send(int owner, string action, int slot = -1, Building building = Building.Empty, ConstructionPayment payment = ConstructionPayment.Standard)
        {
            var command = Command.FromSnapshot(authority.Snapshot(), ++sequences[owner], action, owner, slot, building, payment: payment);
            var result = owner == 1 ? authority.ExecuteLocal(command) : authority.Request(owner, JsonSerializer.Serialize(command, WireJson.Options), time += 1000);
            if (result is not { Accepted: true }) throw new InvalidOperationException($"Arena probe P{owner} {action}: {result?.Message}");
        }
        for (int owner = 1; owner <= players; owner++) Send(owner, "doctrine:warden");
        Send(1, "start");
        // Asymmetric, paid defenses create real structure damage and cross-sector pursuit.
        // No injected units, modified rules, or fabricated arena snapshots.
        Send(1, "build", 9, Building.ArrowTower);
        for (int turn = 1; turn <= 3; turn++)
        {
            for (int owner = 1; owner <= players; owner++) Send(owner, "ready");
        }
        var first = authority.Snapshot();
        if (first.Phase != Phase.Combat) throw new InvalidOperationException("Arena probe did not reach combat");
        var frames = new List<MatchSnapshot> { first with { CombatEvents = [] } };
        long cursor = first.EventSequence;
        for (int step = 0; step < 18000; step++)
        {
            authority.Step(); var state = authority.Snapshot();
            frames.Add(state with { CombatEvents = state.CombatEvents.Where(e => e.Sequence > cursor).ToArray() });
            cursor = state.EventSequence;
            if (state.Phase != Phase.Combat && state.DyingBodies.Length == 0) return frames;
        }
        throw new InvalidOperationException("Arena probe exceeded battle limit");
    }
}
