// SPDX-License-Identifier: GPL-3.0-only
using System.Text.Json;
using Game.Core;
namespace Game.Shared;

public enum RecoveryMove { StoreWounded, ReturnRecovered }
public sealed record RecoveryMember(UnitState Before, ArmyAssignment? Destination, int NextHealth, int Upkeep, int? PaymentsToFull, string Reason)
{
    public bool Moves => Destination is not null;
}
public sealed record SquadRecoveryPlan(RecoveryMove Move, string Match, int City, int Turn, int Hall, long HallGeneration,
    int Squad, RecoveryMember[] Members, BattleFoodForecast? Food, string Unavailable, string Stamp, RecruitmentQuote? Replacement = null)
{
    public RecoveryMember[] Moving => Members.Where(m => m.Moves).ToArray();
    public bool CanCommit => Unavailable.Length == 0 && Members.Any(m => m.Moves);
    public string Summary => Unavailable.Length > 0 ? Unavailable : Members.Length == 0 ? "No recovery move needed. Keep is no action."
        : $"{Moving.Length}/{Members.Length} members will {(Move == RecoveryMove.StoreWounded ? "enter storage" : "return to the field")}. Individual commands; partial results remain."
        + (Move == RecoveryMove.StoreWounded ? $"\nNo HP now. Next eligible payment: {HealthPoints.Format(Moving.Sum(m => m.Before.Health))} → {HealthPoints.Format(Moving.Sum(m => m.NextHealth))} HP."
            + $"\nStored Food demand {Moving.Sum(m => m.Upkeep)} per battle; field members fund first. No fighting while stored." : "\nNo heal, recruitment cost or equipment change. Destinations are shown below.")
        + (Move != RecoveryMove.StoreWounded || Moving.Length == 0 ? "" : Moving.Any(m => m.PaymentsToFull is null)
            ? "\nSome members need a funded completed battle before healing."
            : $"\nUp to {Moving.Max(m => m.PaymentsToFull)} eligible payments to full HP at this rate.")
        + (Replacement is null || Move != RecoveryMove.StoreWounded ? "" : $"\nNew L{Replacement.Level} recruit: {RunPresentation.Resources(Replacement.Cost)} each. Retirement is separate.")
        + (Food is null ? "" : $"\nCurrent funding projection after moves: {Moving.Count(m => Food.Funded.Contains(m.Before.Id))}/{Moving.Length} moved members funded.");
    public string Details => string.Join("\n", Members.Select(m => $"#{m.Before.Id} {m.Before.Type} L{m.Before.Level} · {HealthPoints.Format(m.Before.Health)}/{HealthPoints.Format(m.Before.Profile.Health)} HP · "
        + (m.Destination is null ? m.Reason : Move == RecoveryMove.StoreWounded
            ? $"Town Hall {Hall + 1}; {(m.Before.RecoveryEligible ? "eligible now" : "needs a completed funded battle before healing")}; next HP {HealthPoints.Format(m.NextHealth)}"
            : $"Home {m.Destination.Tile + 1}")))
        + "\nForecast uses the current stock/income basis, not guaranteed future funding. Equipment stays fixed; inspect replacement quotes for old gear.";
}

/// <summary>Detached previews use Core placement, funding and capped healing. Never edits a Match.</summary>
public static class SquadRecovery
{
    public static SquadRecoveryPlan Project(MatchSnapshot state, int player, int squad, int hallSlot, RecoveryMove move)
    {
        var city = state.Players.Single(p => p.Id == player);
        var hall = city.Army!.Halls.FirstOrDefault(h => h.Slot == hallSlot);
        string unavailable = !city.Connected ? "City disconnected." : city.Eliminated ? "Fallen city: no recovery actions."
            : state.Phase is not (Phase.Building or Phase.Preparation) ? "Recovery moves are available during planning."
            : state.Paused ? "Resume before moving members." : city.Ready ? "Unready before moving members."
            : hall is null ? "Build a Town Hall for storage recovery. Keep or recruit separately." : "";
        var candidates = city.Soldiers.Where(u => u.Health > 0 && (move == RecoveryMove.StoreWounded
            ? u.SquadId == squad && u.Assignment is { Stored: false } && u.Health < u.Profile.Health
            : u.Assignment is { Stored: true } a && a.HallSlot == hallSlot && a.HallGeneration == hall?.Generation && u.Health == u.Profile.Health))
            .OrderBy(u => u.Id).ToArray();
        var army = Configuration(state, city);
        var projected = city.Soldiers.ToArray(); var members = new List<RecoveryMember>();
        foreach (var unit in candidates)
        {
            var destination = unavailable.Length > 0 ? null : army.Find(projected, city.Army.PurchasedHomes,
                move == RecoveryMove.StoreWounded ? hallSlot : -1, move == RecoveryMove.StoreWounded ? hall!.Generation : 0,
                move == RecoveryMove.StoreWounded ? hall!.Capacity : 0, plots: city.Army.Homes.Select(h => h.Plot).ToArray(), type: unit.Type);
            string reason = unavailable.Length > 0 ? unavailable : destination is null ? move == RecoveryMove.StoreWounded ? "Hall full; stays in the field." : "No compatible field place; stays stored." : "";
            int next = destination is not null && move == RecoveryMove.StoreWounded && unit.RecoveryEligible
                ? ArmyConfiguration.HealedHealth(unit.Health, unit.Profile.Health, hall!.HealingPercent) : unit.Health;
            int upkeep = city.RecruitmentQuotes.First(q => q.Type == unit.Type).Upkeep;
            int? payments = move == RecoveryMove.StoreWounded && unit.RecoveryEligible && destination is not null ? 0 : null;
            if (payments is not null)
                for (int hp = unit.Health; hp < unit.Profile.Health && payments < 100; payments++) hp = ArmyConfiguration.HealedHealth(hp, unit.Profile.Health, hall!.HealingPercent);
            members.Add(new(unit, destination, next, upkeep, payments, reason));
            if (destination is not null)
                projected = projected.Select(u => u.Id == unit.Id ? u with { Assignment = destination } : u).ToArray();
        }
        var ids = members.Where(m => m.Moves).Select(m => m.Before.Id).ToHashSet();
        UnitType? type = candidates.FirstOrDefault()?.Type;
        var replacement = city.RecruitmentQuotes.Where(q => q.Type == type && city.Slots.Any(s => s.Level == q.Level
            && state.BuildingCatalog.SingleOrDefault(b => b.Type == s.Type)?.Recruits?.Contains(q.Type) == true)).OrderByDescending(q => q.Level).FirstOrDefault();
        return new(move, state.MatchId, player, state.TurnSerial, hallSlot, hall?.Generation ?? 0, squad, members.ToArray(),
            BattleFood.Project(city with { Soldiers = projected }, state.Phase == Phase.Preparation), unavailable, Stamp(state, city, ids), replacement);
    }
    public static bool IsCurrent(SquadRecoveryPlan plan, MatchSnapshot state)
    {
        var city = state.Players.FirstOrDefault(c => c.Id == plan.City);
        if (city is null || Stamp(state, city, plan.Moving.Select(m => m.Before.Id).ToHashSet()) != plan.Stamp) return false;
        return plan.Moving.All(m => city.Soldiers.FirstOrDefault(u => u.Id == m.Before.Id) is { } current
            && current.Assignment == m.Before.Assignment && current.SquadId == m.Before.SquadId);
    }
    internal static ArmyConfiguration Configuration(MatchSnapshot state, CityState city)
        => new(state.Rules.Army, new HexBoard(state.Arena!.Board), city.Army!.SquadCapacity);
    internal static string Stamp(MatchSnapshot state, CityState city, HashSet<int> moved)
        => JsonSerializer.Serialize(new { state.MatchId, state.ConfigurationFingerprint, state.Phase, state.TurnSerial, state.Paused,
            city.Id, city.Connected, city.Eliminated, city.Ready, city.Resources, city.ProductionIncome, city.Research,
            city.Army!.PurchasedHomes, city.Army.SquadCapacity, Halls = city.Army.Halls,
            Plots = city.Army.Homes.Select(h => h.Plot),
            Members = city.Soldiers.OrderBy(u => u.Id).Select(u => new { u.Id, u.Type, u.Level, u.Health, u.Profile, u.RecoveryEligible,
                Assignment = moved.Contains(u.Id) ? null : u.Assignment, Squad = moved.Contains(u.Id) ? 0 : u.SquadId }) }, WireJson.Options);
}
