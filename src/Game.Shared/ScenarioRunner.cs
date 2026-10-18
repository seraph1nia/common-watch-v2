using System.Text.Json;
using Game.Core;

namespace Game.Shared;

/// <summary>Paid command replay against unmodified upstream rules. Never writes authority state.</summary>
public static class ScenarioRunner
{
    public static IReadOnlyList<string> Checkpoints { get; } = Array.AsReadOnly<string>(
        ["initial", "settlement", "building-selected", "battle-start", "combat", "draft", "post-draft"]);
    public static SoloSession CreateAt(string checkpoint, ulong seed = SoloSession.DefaultSeed, string? doctrine = null)
    {
        if (!Checkpoints.Contains(checkpoint)) throw new ArgumentException("Unknown scenario checkpoint: " + checkpoint);
        var session = new SoloSession(seed);
        try
        {
            if (doctrine is not null) Require(session.Send("doctrine:" + doctrine));
            Require(session.Start());
            if (checkpoint == "initial") return session;
            Require(session.Send("build", 0, Building.Farm));
            Require(session.Send("build", 1, Building.Barracks));
            Require(session.Send("build", 2, Building.MetalMine));
            // Six starting wood pays for the three buildings. Upstream permits a
            // four-gold Lumbermill recovery purchase precisely when wood is zero.
            Require(session.Send("build", 3, Building.Lumbermill, payment: ConstructionPayment.GoldRecovery));
            if (checkpoint == "settlement") return session;
            session.SelectSlot(1);
            if (checkpoint == "building-selected") return session;
            for (int preparation = 0; preparation < 2; preparation++)
            {
                Require(session.Send("ready"));
                Require(session.Send("recruit", 1));
                Require(session.Send("recruit", 1));
            }
            Require(session.Send("ready"));
            if (session.Snapshot.Phase != Phase.Combat) throw new InvalidOperationException("Scenario did not enter combat.");
            if (checkpoint == "battle-start") { session.ResetPlayback(); return session; }
            if (checkpoint == "combat")
            {
                // Allow deployment to become an actual exchange before capturing.
                // Watchpost attacks can begin on tick one while soldiers still spawn.
                for (int i = 0; i < 1000 && session.Snapshot.Phase == Phase.Combat; i++)
                {
                    session.Step();
                    if (session.Snapshot.Tick >= 180 && session.Snapshot.CombatEvents.Any(e =>
                        e.Type == CombatEventType.AttackStarted && e.Unit is not null)) break;
                }
                if (session.Snapshot.Phase != Phase.Combat)
                    throw new InvalidOperationException("Combat checkpoint advanced beyond battle.");
                session.ResetPlayback();
                return session;
            }
            for (int i = 0; i < 10000 && session.Snapshot.Phase == Phase.Combat; i++) session.Step();
            if (session.Snapshot.Phase != Phase.RewardDraft || session.City.Run.Offers.Length != 3)
                throw new InvalidOperationException($"Scenario failed to win first wave: {session.Snapshot.Phase} at {session.Snapshot.Tick}.");
            session.ResetPlayback();
            if (checkpoint == "draft") return session;
            Require(session.ChooseReward(session.City.Run.Offers[0]));
            if (session.Snapshot.Phase != Phase.Building || session.Snapshot.Wave != 2)
                throw new InvalidOperationException("Reward selection did not enter wave two preparation.");
            return session;
        }
        catch { session.Dispose(); throw; }
    }
    public static string Record(string checkpoint, ulong seed = SoloSession.DefaultSeed)
    {
        using SoloSession session = CreateAt(checkpoint, seed);
        return JsonSerializer.Serialize(new { checkpoint, selectedSlot = session.SelectedSlot, state = session.Snapshot }, WireJson.Options);
    }
    private static void Require(CommandResult result)
    {
        if (!result.Accepted) throw new InvalidOperationException("Scenario command rejected: " + result.Message);
    }
}
