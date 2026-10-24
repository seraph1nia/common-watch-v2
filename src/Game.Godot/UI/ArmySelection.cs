// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;

namespace Game.GodotClient;

/// <summary>Harmless per-city selection. Accepted assignments are observed, never executed here.</summary>
public sealed class ArmySelection
{
    private sealed class Choice { public int Home = -1, Squad, Member; }
    private readonly Dictionary<int, Choice> _cities = [];
    private object? _session;
    private string _match = "";
    private CityState? _city;
    private Choice _choice = new();
    public int Home => _choice.Home;
    public int Squad => _choice.Squad;
    public int Member => _choice.Member;
    public void Reset()
    {
        _cities.Clear(); _session = null; _match = ""; _city = null; _choice = new();
    }
    public void Observe(object session, MatchSnapshot state, int city)
    {
        if (!ReferenceEquals(_session, session) || _match != state.MatchId) Reset();
        _session = session; _match = state.MatchId;
        _city = state.Players.Single(p => p.Id == city);
        foreach (int old in _cities.Keys.Where(id => state.Players.All(p => p.Id != id)).ToArray()) _cities.Remove(old);
        if (!_cities.TryGetValue(city, out var choice)) _cities[city] = choice = new();
        _choice = choice;
        if (_city.Army is not { } army) return;
        if (!army.Homes.Any(h => h.PlotId == Home))
            _choice.Home = army.Homes.FirstOrDefault(h => h.Used > 0)?.PlotId ?? army.Homes.First().PlotId;
        if (Member != 0)
        {
            if (_city.Soldiers.FirstOrDefault(u => u.Id == Member) is { } member) Follow(member);
            else _choice.Member = 0;
        }
        if (Squad != 0 && !_city.Soldiers.Any(u => u.Health > 0 && u.SquadId == Squad && u.Assignment is { Stored: false })) _choice.Squad = 0;
        if (Member == 0 && AtHome(Home).FirstOrDefault() is { } occupant) _choice.Squad = occupant.SquadId;
    }
    public void SelectHome(int plot)
    {
        if (_city?.Army?.Homes.All(h => h.PlotId != plot) != false) return;
        _choice.Home = plot; _choice.Member = 0;
        if (AtHome(plot).FirstOrDefault() is { } occupant) _choice.Squad = occupant.SquadId;
    }
    public void SelectSquad(int squad)
    {
        if (_city?.Soldiers.FirstOrDefault(u => u.Health > 0 && u.SquadId == squad && u.Assignment is { Stored: false }) is not { } member) return;
        _choice.Member = 0; Follow(member);
    }
    public void SelectMember(int id)
    {
        if (_city?.Soldiers.FirstOrDefault(u => u.Id == id) is not { } member) return;
        _choice.Member = id; Follow(member);
    }
    private IEnumerable<UnitState> AtHome(int plot) => _city?.Soldiers.Where(u => u.Health > 0
        && u.Assignment is { Stored: false } a && a.Tile + ArmyConfiguration.PlotOffset == plot) ?? [];
    private void Follow(UnitState member)
    {
        if (member.Assignment is { Stored: false } field)
        { _choice.Squad = member.SquadId; _choice.Home = field.Tile + ArmyConfiguration.PlotOffset; }
        else _choice.Squad = 0;
    }
}
