namespace Game.Core;

public sealed record RunCardView(string Id, string Title, string Subtitle, string Description, string Footer, bool Selected, bool Enabled);
public sealed record RunView(string Title, string Status, string Forecast, RunCardView[] Cards);
public static class RunPresentation
{
    public static RunView Project(MatchSnapshot state, CityState city, int localPlayer, bool connected, bool doctrines)
    {
        bool owner = city.Id == localPlayer && connected && !city.Eliminated && !state.Paused;
        string forecast = "Next wave " + state.Wave + ": " + string.Join(" · ", state.NextWave.Where(u => u.Destination == city.Id)
            .GroupBy(u => (u.Type, u.Level, u.IsBoss)).Select(g => $"{g.Count()} {g.Key.Type} L{g.Key.Level}{(g.Key.IsBoss ? " BOSS" : "")}"));
        if (doctrines)
            return new("Starting doctrine", "Choose freely in the lobby. Personal effects lock at match start.", forecast,
                RunCatalog.Doctrines.Select(d =>
                {
                    bool safe = TryStarting(state.Rules, d, out ResourceCost allocation);
                    return new RunCardView(d.Id, d.Crest + " " + d.Name, d.Category,
                        d.Theme + "\n\nBenefits: " + d.Benefits + "\n\nDrawback: " + d.Drawback,
                        safe ? $"Starting: {Resources(allocation)} · Research {d.Research}" : "Unavailable: starting allocation exceeds configured bounds.",
                        city.Run.Doctrine == d.Id, owner && state.Phase == Phase.Lobby && safe);
                }).ToArray());
        string status = string.Join(" · ", state.Players.Where(p => !p.Eliminated).Select(p => $"P{p.Id}: {(p.Run.Choice is not null ? "selected" : p.Connected ? "choosing" : "away · deterministic fallback")}"));
        return new($"Wave {city.Run.DraftWave} reward · choose one", status + $"\nImmediate resource multiplier ×{RunEffects.Multiplier(city.Run.DraftWave)}; no extra production.", forecast,
            city.Run.Offers.Select(id =>
            {
                RewardCard card = RunCatalog.Card(id);
                return new RunCardView(id, card.Name, card.Rarity.ToString(), card.Effect,
                    $"Acquired {city.Run.Stacks(id)}/{card.MaxStacks}" + (card.Multiplied ? $" · resource reward ×{RunEffects.Multiplier(city.Run.DraftWave)}" : ""),
                    city.Run.Choice == id, owner && state.Phase == Phase.RewardDraft && city.Run.Choice is null);
            }).ToArray());
    }
    public static string Resources(ResourceCost cost) => string.Join(" · ", Enum.GetValues<Resource>().Where(r => cost.Amount(r) != 0).Select(r => $"{cost.Amount(r)} {r}"));
    public static ResourceCost Starting(Rules rules, DoctrineDefinition doctrine)
    {
        if (!TryStarting(rules, doctrine, out ResourceCost result)) throw new OverflowException();
        return result;
    }
    private static bool TryStarting(Rules rules, DoctrineDefinition doctrine, out ResourceCost result)
        => new ResourceCost(rules.StartingGold, rules.StartingWood).TryAdd(doctrine.StartingBonus, out result) && result.TryPay(doctrine.StartingPenalty, out result);
    public static string Inspection(CityState city) => RunCatalog.Doctrine(city.Run.Doctrine).Name + "\n" + RunCatalog.Doctrine(city.Run.Doctrine).Benefits + "\nDrawback: " + RunCatalog.Doctrine(city.Run.Doctrine).Drawback
        + "\nAcquired rewards:\n" + string.Join("\n", city.Run.Cards.Select(s => $"{RunCatalog.Card(s.Id).Name} ×{s.Count} · {RunCatalog.Card(s.Id).Effect}"));
}
