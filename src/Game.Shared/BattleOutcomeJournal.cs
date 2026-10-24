using Game.Core;

namespace Game.Shared;

public sealed record BattleOutcome(int Wave, int City, string Result, int Participants,
    int? Survived, int? Lost, int? FieldUnfunded, int Unfunded, int FoodPaid,
    int? KeepDamage, ResourceCost Resources, int Research, bool ObservedEnd)
{
    public string RewardSummary => (RunPresentation.Resources(Resources) is { Length: > 0 } reward ? reward : "No clear resources") + $" · {Research} Research";
    public string Summary => $"Wave {Wave} · P{City} · {Result}\n"
        + (Survived is { } alive ? $"Army: {alive} survived · {Lost} lost" : "Army losses: earlier outcome unavailable")
        + (KeepDamage is { } damage ? $" · Keep −{HealthPoints.Format(damage)}" : " · Keep damage unavailable")
        + "\n" + (FieldUnfunded is { } fieldCount ? $"{fieldCount} sat out" : $"{Unfunded} unfunded") + " · Clear payout: " + RewardSummary;
    public string Details => (FieldUnfunded is { } fieldCount ? $"{fieldCount} field soldiers sat out" : $"{Unfunded} unfunded soldiers (field/storage breakdown unavailable)")
        + $" · {FoodPaid} Food paid\n{Participants} funded field participants\nClear reward: " + RewardSummary
        + (!ObservedEnd ? "\nRejoined after the battle; missing history is not inferred." : "");
}

/// <summary>Last factual outcome per city, bounded to the roster. Never changes rules or retains simulation objects.</summary>
public sealed class BattleOutcomeJournal
{
    private string _match = "";
    private int _planningWave;
    private readonly Dictionary<int, int> _keepBefore = [];
    private readonly Dictionary<int, BattleOutcome> _last = [];
    public BattleOutcome? For(int city) => _last.GetValueOrDefault(city);
    public void Reset() { _match = ""; _planningWave = 0; _keepBefore.Clear(); _last.Clear(); }
    public void Observe(MatchSnapshot state)
    {
        if (_match != state.MatchId) { Reset(); _match = state.MatchId; }
        if (state.Phase is Phase.Building or Phase.Preparation)
        {
            if (_planningWave != state.Wave) { _planningWave = state.Wave; _keepBefore.Clear(); }
            foreach (var city in state.Players) _keepBefore[city.Id] = city.Health;
        }
        bool ended = state.Phase is Phase.RewardDraft or Phase.Victory or Phase.Defeat;
        int completed = state.Phase == Phase.RewardDraft ? state.Wave - 1 : state.Wave;
        foreach (var city in state.Players)
        {
            if (city.LastUpkeep is not { } upkeep) continue;
            bool atEnd = ended && upkeep.Wave == completed;
            BattleLossReceipt? frozen = city.LastBattleLoss is { } loss && loss.Wave == upkeep.Wave ? loss : null;
            bool factual = atEnd || frozen is not null;
            if (!atEnd && state.Phase == Phase.Combat) continue;
            if (_last.TryGetValue(city.Id, out var previous) && previous.Wave >= upkeep.Wave
                && (previous.ObservedEnd || !factual)) continue;
            // Use the frozen Core result after reconnect/next planning; a later
            // edited roster cannot reconstruct casualties on its own.
            var living = city.Soldiers.Where(u => u.Health > 0).Select(u => u.Id).ToHashSet();
            int? survived = frozen?.Survivors ?? (atEnd ? upkeep.Participating.Count(living.Contains) : null);
            int storedUnfunded = city.Soldiers.Count(u => u.Assignment is { Stored: true } && upkeep.Unfed.Contains(u.Id));
            var reward = city.LastReward is { } receipt && receipt.Wave == upkeep.Wave ? receipt : null;
            string result = atEnd && state.Phase == Phase.Defeat ? PresentationLimits.DefeatText(state.DefeatReason)
                : city.Eliminated ? "City fell" : reward is not null ? state.Phase == Phase.Victory ? "Victory" : "Held" : "Outcome history unavailable";
            _last[city.Id] = new(upkeep.Wave, city.Id, result, upkeep.Participating.Length,
                survived, survived is { } alive ? upkeep.Participating.Length - alive : null,
                frozen?.FieldUnfunded ?? (atEnd ? upkeep.Unfed.Length - storedUnfunded : null), upkeep.Unfed.Length, upkeep.Paid,
                // The authoritative result freezes losses before recovery. The
                // fallback remains conservative for a detached incomplete fixture.
                frozen?.KeepDamage ?? (atEnd && !(state.Phase == Phase.RewardDraft && city.Run.Choice is not null)
                    && _planningWave == upkeep.Wave && _keepBefore.TryGetValue(city.Id, out int before)
                    ? Math.Max(0, before - city.Health) : null),
                reward?.Amount ?? default, reward?.Research ?? 0, factual);
        }
    }
}
