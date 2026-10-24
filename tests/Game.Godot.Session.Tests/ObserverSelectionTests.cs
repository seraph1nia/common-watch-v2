// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using Game.Shared;
using Game.GodotClient;
using Xunit;

namespace Game.Godot.Session.Tests;

public sealed class ObserverSelectionTests
{
    // Presentation-only preferences, not campaign checkpoints or authoritative fixtures.
    private static TeamOverview View(int suggested = 2, bool observer = true) => new(false, observer, suggested, "", [],
        1, new(Phase.Combat, [], 0, 0));

    [Fact]
    public void AutomaticChoiceWaitsForOwnSurvivorsThenRunsOnlyOnce()
    {
        var selection = new ObserverSelection(); var session = new object();
        Assert.Null(selection.Observe(session, "match", View(), 1, mayFocus: false));
        Assert.Equal(2, selection.Observe(session, "match", View(), 1, mayFocus: true));
        Assert.Null(selection.Observe(session, "match", View(suggested: 3), 1, mayFocus: true));
        Assert.Null(selection.Observe(session, "match", View(suggested: 3), 2, mayFocus: true));
    }

    [Fact]
    public void ExplicitOwnOrAllyChoiceSurvivesUpdatesAndReconnectDeferral()
    {
        var selection = new ObserverSelection(); var session = new object();
        Assert.Null(selection.Observe(session, "match", View(), 1, mayFocus: false));
        selection.InspectCity(observing: true);
        Assert.Null(selection.Observe(session, "match", View(suggested: 3), 1, mayFocus: true));
        selection.Reset();
        Assert.Null(selection.Observe(session, "match", View(), 3, mayFocus: true));
        Assert.Null(selection.Observe(session, "match", View(), 1, mayFocus: true));
    }

    [Fact]
    public void NewSessionOrMatchAllowsOneNewChoiceButOrdinaryLivingNavigationDoesNotSuppressIt()
    {
        var selection = new ObserverSelection(); var session = new object();
        Assert.Null(selection.Observe(session, "match", View(observer: false), 1, mayFocus: true));
        selection.InspectCity(observing: false);
        Assert.Equal(2, selection.Observe(session, "match", View(), 1, mayFocus: true));
        Assert.Equal(3, selection.Observe(session, "replacement", View(suggested: 3), 1, mayFocus: true));
        Assert.Equal(2, selection.Observe(new object(), "replacement", View(), 1, mayFocus: true));
        Assert.Null(selection.Observe(session, "ended", View(suggested: 1), 1, mayFocus: true));
    }
}
