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
        int waiting = state.Players.Count(p => !p.Eliminated && p.Connected && p.Run.Choice is null);
        return new($"Wave {city.Run.DraftWave} reward · choose one", state.Players.Length > 1 ? $"{waiting} {(waiting == 1 ? "city" : "cities")} choosing" : "", forecast,
            city.Run.Quotes.Select(quote =>
            {
                RewardCard card = RunCatalog.Card(quote.Id);
                return new RunCardView(quote.Id, card.Name,
                    card.MissingHealthPercent > 0 ? "Living soldiers, including storage · no resurrection" : "",
                    Benefit(quote.Resources, quote.KeepHealing, quote.SoldierHealing, quote.Soldiers.Length),
                    quote.ResourceCapped ? "At resource capacity: the exact quoted shipment is reduced." : "",
                    city.Run.Choice == quote.Id, owner && state.Phase == Phase.RewardDraft && city.Run.Choice is null);
            }).ToArray());
    }
    public static string Benefit(ResourceCost resources, int keepHealing, long soldierHealing, int soldiers)
    {
        var parts = new List<string>();
        foreach (Resource resource in Enum.GetValues<Resource>())
            if (resources.Amount(resource) > 0) parts.Add($"+{resources.Amount(resource)} {resource} now");
        if (keepHealing > 0) parts.Add($"Restore {HealthPoints.Format(keepHealing)} Keep HP");
        if (soldierHealing > 0) parts.Add($"Restore {(soldierHealing / (decimal)HealthPoints.Scale).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)} HP across {soldiers} living soldiers");
        return string.Join("\n", parts);
    }
    public static string Receipt(BattleRewardReceipt receipt)
        => $"Wave {receipt.Wave} · {RunCatalog.Card(receipt.Id).Name} · "
            + Benefit(receipt.Resources, receipt.KeepHealing, receipt.SoldierHealing, receipt.SoldierCount).Replace('\n', ' ')
            + (receipt.ResourceCapped ? " · shipment limited by resource capacity" : "");
    public static string Resources(ResourceCost cost) => string.Join(" · ", Enum.GetValues<Resource>().Where(r => cost.Amount(r) != 0).Select(r => $"{cost.Amount(r)} {r}"));
    public static ResourceCost Starting(Rules rules, DoctrineDefinition doctrine)
    {
        if (!TryStarting(rules, doctrine, out ResourceCost result)) throw new OverflowException();
        return result;
    }
    private static bool TryStarting(Rules rules, DoctrineDefinition doctrine, out ResourceCost result)
        => new ResourceCost(rules.StartingGold, rules.StartingWood).TryAdd(doctrine.StartingBonus, out result) && result.TryPay(doctrine.StartingPenalty, out result);
    public static string Inspection(CityState city) => RunCatalog.Doctrine(city.Run.Doctrine).Name + "\n" + RunCatalog.Doctrine(city.Run.Doctrine).Benefits + "\nDrawback: " + RunCatalog.Doctrine(city.Run.Doctrine).Drawback
        + "\nConsumed battle rewards (history, no active modifiers):\n" + string.Join("\n", city.Run.Rewards.Select(Receipt));
}
