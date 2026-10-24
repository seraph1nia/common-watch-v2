// SPDX-License-Identifier: GPL-3.0-only
using Game.Shared;

namespace Game.GodotClient;

/// <summary>One harmless automatic observer choice per session/match. No screen or authority state.</summary>
public sealed class ObserverSelection
{
    private object? _session;
    private string _match = "";
    private bool _chosen;
    public void Reset() { _session = null; _match = ""; _chosen = false; }
    public void InspectCity(bool observing) { if (observing) _chosen = true; }
    public int? Observe(object session, string match, TeamOverview team, int inspectedCity, bool mayFocus)
    {
        if (!ReferenceEquals(_session, session) || _match != match) Reset();
        _session = session; _match = match;
        if (!team.Observer || _chosen) return null;
        if (inspectedCity != team.LocalPlayer) { _chosen = true; return null; }
        if (!mayFocus || team.SuggestedCity == team.LocalPlayer) return null;
        _chosen = true; return team.SuggestedCity;
    }
}
