using Game.Core;

namespace Game.Shared;

public sealed record DoctrineIntroduction(string Playstyle, string Immediate)
{
    public static DoctrineIntroduction For(string id) => id switch
    {
        "warden" => new("Hold the line with a sturdier Watchpost.", "Start with Stone, less Gold. Build Wood income early."),
        "merchant" => new("Use trade for flexible spending.", "More Gold, only 4 Wood. Grow Wood income before a trainer."),
        "warmaster" => new("Hit harder with melee squads.", "Starting Metal and Food help; melee recruits need extra Metal."),
        "arcanist" => new("Support your frontline with magic and research.", "Cloth and Stone help magic; melee can open while you save."),
        "grovekeeper" => new("Grow through stronger Farm and Wood income.", "Recruits cost extra Metal or Cloth. Plan their supply."),
        "huntsman" => new("Keep longer-range archers behind a frontline.", "Archers need Wood, Metal and protection. Melee can open."),
        _ => throw new ArgumentOutOfRangeException(nameof(id))
    };
}
public enum OpeningDestination { Build, Military, Recruit, Homes, Outcome }
public sealed record OpeningHint(string Objective, string Message, OpeningDestination Destination, string Action);

/// <summary>Optional reading of accepted state. Never submits commands or gates Ready.</summary>
public static class OpeningGuidance
{
    public static string NextReady(MatchSnapshot state) => state.Phase == Phase.Preparation
        ? "Ready pays final income and Food, then starts battle."
        : $"Ready pays {(PreparationSchedule.ResourceMultiplier(state.Rules, state.Turn) == 2 ? "double resources, one Research/recovery event" : "one production")} and advances to preparation {state.Turn + 1}/{state.Rules.PreparationStages}.";
    public static OpeningHint? Project(MatchSnapshot state, CityState city)
    {
        if (state.Wave > 3 || state.Phase is Phase.Lobby or Phase.Combat or Phase.RewardDraft) return null;
        if (city.Eliminated || state.Phase is Phase.Defeat or Phase.Victory)
            return city.LastUpkeep is { } paid ? new("Battle result", $"{paid.Participating.Length} fought; {paid.Paid} Food paid. Review the actual result.", OpeningDestination.Outcome, "Review battle") : null;
        bool Has(Building type) => city.Slots.Any(s => s.Type == type);
        if (!Has(Building.Lumbermill))
        {
            if (city.Wood == 0 && state.BuildingCatalog.Single(b => b.Type == Building.Lumbermill).RecoveryConstruction is { } recovery)
                return new("Restore Wood income", $"Zero Wood: Lumbermill Gold recovery costs {RunPresentation.Resources(recovery)}.", OpeningDestination.Build, "See recovery");
            return new("Start Wood income", "A Lumbermill earns Wood on Ready. Build it before exhausting your starting supply.", OpeningDestination.Build, "Build Wood income");
        }
        if (state.Wave == 1 && state.Turn == 1 && city.Run.Doctrine == "merchant" && city.Slots.Count(s => s.Type == Building.Lumbermill) == 1)
            return new("Grow Wood supply", "Keep Wood coming for the Metal Mine and trainer. Another Lumbermill is a normal purchase.", OpeningDestination.Build, "See production");
        var recruits = city.Slots.SelectMany(s => state.BuildingCatalog.FirstOrDefault(b => b.Type == s.Type)?.Recruits ?? []).Distinct().ToArray();
        bool supplied = recruits.Length == 0 ? Has(Building.MetalMine) : recruits.Any(type => city.RecruitmentQuotes.Where(q => q.Type == type).Any(q =>
            q.Cost.Metal > 0 && Has(Building.MetalMine) || q.Cost.Cloth > 0 && Has(Building.Weaver)));
        if (!supplied)
            return new("Supply recruits", "Metal supplies melee and archers; Cloth supplies Mages. Producer cards show the next output.", OpeningDestination.Build, "See production");
        if (recruits.Length == 0)
            return new("Build a trainer", "A Barracks trains melee; specialist trainers need their own equipment supply.", OpeningDestination.Military, "See Military");
        if (!Has(Building.Farm))
            return new("Fund battle Food", "A Farm pays on Ready. Final income can fund upkeep, but cannot buy recruits before battle.", OpeningDestination.Build, "See Food income");
        var field = city.Soldiers.Where(u => u.Health > 0 && u.Assignment is { Stored: false }).ToArray();
        if (state.Phase == Phase.Preparation && city.FoodForecast is { Unfed.Length: > 0 } forecast)
            return new("Check Food", $"{forecast.Unfed.Length} members will sit out. Final income is already included in this forecast.", OpeningDestination.Build, "Inspect Food");
        int target = state.Wave == 1 ? 3 : 6;
        if (field.Length < target)
            return new("Grow the line", $"{field.Length} field members. Recruit into an available home before final Ready.", OpeningDestination.Recruit, "Recruit a member");
        return new("Commit when ready", NextReady(state), OpeningDestination.Homes, "Inspect homes");
    }
}
