// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;

namespace Game.Shared;

public sealed record TeamMember(int Id, string State, int KeepHealth, bool Endangered, int Field, int UnfundedField, int UnfundedStored, string Contribution, string Details);
public sealed record TeamOverview(bool Paused, bool Observer, int SuggestedCity, string Explanation, TeamMember[] Members);

/// <summary>Team state and observed logical sectors from accepted snapshots/playback, never shared command authority.</summary>
public static class TeamAwareness
{
    public static int Sector(MatchSnapshot state, UnitState unit)
    {
        if (unit.Hex is not { Position.Cell: > 0 } hex || state.Arena is not { } arena) return 0;
        var cell = arena.Board.Cells.FirstOrDefault(c => c.Id == hex.Position.Cell);
        if (cell is null) return 0;
        int index = cell.Coordinate.Column / SharedArena.SectorWidth;
        return index >= 0 && index < arena.Sectors.Length ? arena.Sectors[index].Owner : 0;
    }
    public static TeamOverview Project(MatchSnapshot state, int localPlayer, bool connected = true, IReadOnlyList<UnitState>? presented = null)
    {
        bool combat = state.Phase == Phase.Combat;
        bool ended = state.Phase is Phase.Victory or Phase.Defeat;
        var local = state.Players.Single(p => p.Id == localPlayer);
        var units = presented ?? state.Players.SelectMany(p => p.Soldiers).ToArray();
        var members = state.Players.Select(city =>
        {
            bool away = !city.Connected || city.Id == localPlayer && !connected;
            string status = city.Eliminated ? "Fallen" : away ? "Away" : ended ? state.Phase.ToString()
                : state.Phase == Phase.RewardDraft ? city.Run.Choice is null ? "Choosing" : "Selected"
                : combat ? "Fighting" : city.Ready ? "Ready" : "Preparing";
            var active = units.Where(u => u.Owner == city.Id && u.Faction == Faction.Adventurers && u.Health > 0 && u.Hex?.Lifecycle == UnitLifecycle.Alive && u.Deployed).ToArray();
            int field = combat ? active.Length : city.Eliminated || ended ? 0 : city.FoodForecast?.Participating.Length ?? 0;
            var unfed = combat ? city.LastUpkeep?.Unfed ?? [] : city.FoodForecast?.Unfed ?? [];
            int storedUnfed = city.Soldiers.Count(u => u.Assignment is { Stored: true } && unfed.Contains(u.Id));
            int fieldUnfed = unfed.Length - storedUnfed;
            string contribution = combat ? string.Join(" · ", active.GroupBy(u => Sector(state, u)).Where(g => g.Key > 0).OrderBy(g => g.Key).Select(g => $"{g.Count()} @P{g.Key}")) : "";
            bool danger = !city.Eliminated && !ended && city.Health <= HealthPoints.FromWhole(state.Rules.CityHealth) * .3;
            string details = $"P{city.Id} · {status}{(state.Paused ? " · match paused" : "")}\nKeep {HealthPoints.Format(city.Health)}/{state.Rules.CityHealth}{(danger ? " · low Keep" : "")}\n"
                + (city.Eliminated ? combat ? "Current surviving soldiers can finish this battle; no future army participation." : "Observer: no future production, recruitment, Ready or reward choice."
                    : $"{field} {(combat ? "currently active" : "projected to fight")} · {fieldUnfed} field unfunded · {storedUnfed} stored unfunded")
                + (contribution.Length == 0 ? "" : "\nObserved logical sectors: " + contribution + ". Ownership stays with this city.")
                + (away ? "\nAway cities do not block readiness. Reconnect reclaims the same city; the host must remain running." : "")
                + (!combat && !ended && state.Turn < 3 ? "\nFood uses stock now; later income and purchases can change it." : "")
                + "\nInspect only. Your commands always belong to your authenticated city.";
            return new TeamMember(city.Id, status, city.Health, danger, field, fieldUnfed, storedUnfed, contribution, details);
        }).ToArray();
        int suggested = state.Players.Where(p => !p.Eliminated).OrderBy(p => p.Health).ThenBy(p => p.Id).Select(p => p.Id).FirstOrDefault(localPlayer);
        string explanation = ended ? $"Match {state.Phase}. No further production or recruitment."
            : local.Eliminated ? "Your Keep fell. Surviving soldiers can finish the current battle; later waves exclude your army. Original enemy allocations are redistributed to surviving cities. Inspect allies or watch the field; there is no shared city control or resurrection."
            : "Ready/choice states belong to each city. @P# reports observed logical sector, not command ownership.";
        return new(state.Paused, local.Eliminated && !ended, suggested, explanation, members);
    }
}
