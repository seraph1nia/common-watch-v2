using Game.Core;

namespace Game.Shared;

public sealed record DoctrineIntroduction(string Playstyle, string Immediate)
{
    public static DoctrineIntroduction For(string id) => id switch
    {
        "warden" => new("Hold the line with a sturdier Watchpost.", "Start with Stone but less Gold. Budget for Wood recovery."),
        "merchant" => new("Use trade for flexible spending.", "More Gold, less Wood: build Wood income early or use recovery at zero Wood."),
        "warmaster" => new("Hit harder with melee squads.", "Starting Metal and Food help; melee recruits need extra Metal."),
        "arcanist" => new("Support your frontline with magic and research.", "Starting Cloth and Stone help an early Arcanum; keep Food income."),
        "grovekeeper" => new("Grow through stronger Farm and Wood income.", "Recruits cost extra Metal or Cloth. Plan their supply."),
        "huntsman" => new("Keep longer-range archers behind a frontline.", "Save Wood and Metal for fragile archers; melee can open."),
        _ => throw new ArgumentOutOfRangeException(nameof(id))
    };
}
public enum OpeningDestination { Build, Recruit, Homes, Outcome }
public sealed record OpeningHint(string Objective, string Message, OpeningDestination Destination, string Action);

/// <summary>Optional reading of accepted state. Never submits commands or gates Ready.</summary>
public static class OpeningGuidance
{
    public static string NextReady(MatchSnapshot state) => state.Phase == Phase.Preparation
        ? "Ready pays final income and Food, then starts battle."
        : $"Ready pays {(PreparationSchedule.ResourceMultiplier(state.Rules, state.Turn) == 2 ? "double resources, one Research/recovery event" : "one production")} and advances to preparation {state.Turn + 1}/{state.Rules.PreparationStages}.";
    public static OpeningHint? Project(MatchSnapshot state, CityState city, bool reviewedHomes, BattleOutcome? outcome)
    {
        if (state.Wave > 3 || state.Phase is Phase.Lobby or Phase.Combat) return null;
        var field = city.Soldiers.Where(u => u.Health > 0 && u.Assignment is { Stored: false }).ToArray();
        if (city.LastUpkeep is { } paid && (state.Wave > 1 || state.Phase is Phase.Defeat or Phase.Victory))
            return new("Review your preparation", $"Wave {paid.Wave}: {paid.Participating.Length} fought; {paid.Paid} Food paid. "
                + (outcome?.Lost is { } lost ? $"{lost} lost. " : "")
                + "Compare funding and homes with the battle report.", OpeningDestination.Outcome, "Review battle & preparation");
        bool Has(Building type) => city.Slots.Any(s => s.Type == type);
        if (!Has(Building.Lumbermill) && city.Wood == 0 && state.BuildingCatalog.Single(b => b.Type == Building.Lumbermill).RecoveryConstruction is { } recovery)
            return new("1/4 · Restore Wood income", $"Zero Wood: Production offers Lumbermill Gold recovery ({RunPresentation.Resources(recovery)}). Build before Ready.", OpeningDestination.Build, "Find Gold recovery");
        if (!Has(Building.Farm))
            return new("1/4 · Establish Food income", "A Farm pays on Ready. Final income arrives before Food payment; only funded field members fight.", OpeningDestination.Build, "See production choices");
        var recruits = city.Slots.SelectMany(s => state.BuildingCatalog.FirstOrDefault(b => b.Type == s.Type)?.Recruits ?? []).Distinct().ToArray();
        bool supplied = recruits.Any(type => city.RecruitmentQuotes.Where(q => q.Type == type).Any(q =>
            q.Cost.Metal > 0 && Has(Building.MetalMine) || q.Cost.Cloth > 0 && Has(Building.Weaver)));
        if (!Has(Building.Lumbermill) || !supplied)
            return new("1/4 · Establish production", "Build a recruiter and its supply before Ready: Metal for melee/archers, Cloth for Mages.", OpeningDestination.Build, "See build choices");
        int largest = field.GroupBy(u => u.SquadId).Select(g => g.Count()).DefaultIfEmpty().Max();
        if (largest < Math.Min(3, city.Army?.SquadCapacity ?? 3))
            return new("2/4 · Recruit a squad", $"Largest squad: {largest}/{city.Army?.SquadCapacity}. Same-type recruits share a home; check materials and Food.", OpeningDestination.Recruit, "Recruit troops");
        int sitting = field.Count(u => city.FoodForecast?.Unfed.Contains(u.Id) == true);
        if (sitting > 0)
            return new("3/4 · Check Food funding", $"{sitting} field members sit out. Check the stock/income basis and Farm production before battle.", OpeningDestination.Build, "Inspect production options");
        if (!reviewedHomes)
            return new("3/4 · Fund and position", $"{city.FoodForecast?.Participating.Length ?? 0} projected to fight. Inspect six homes: front 1–3, rear 4–6.", OpeningDestination.Homes, "Review army homes");
        return new("4/4 · Defend the first wave", NextReady(state), OpeningDestination.Homes, "Inspect army / placement");
    }
}
