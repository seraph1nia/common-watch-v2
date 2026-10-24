// SPDX-License-Identifier: GPL-3.0-only
using System.Text.Json;
using Game.Core;
using Game.Shared;
using Godot;
namespace Game.GodotClient;

public partial class GameApplication
{
    private async Task OpenRecoveryHome(int squad)
    {
        await JourneyClick(_gameUi.ArmyNavigationButton);
        var member = _session.City.Soldiers.First(u => u.SquadId == squad && u.Assignment is { Stored: false });
        await JourneyClick(_gameUi.Details.Deployment.HomeButtons["Home" + (member.Assignment!.Tile + ArmyConfiguration.PlotOffset)]);
    }
    private async Task ReviewRecoveryNative(int squad)
    {
        await JourneyWait(() => _gameUi.ArmyNavigationButton.IsVisibleInTree() && !_gameUi.ArmyNavigationButton.Disabled, "planning navigation after local combat playback");
        await OpenRecoveryHome(squad); await JourneyClick(_gameUi.Details.Army.Recovery.Actions["Recover"]);
    }
    private string RecoveryRoster() => JsonSerializer.Serialize(new { _session.City.Resources, _session.City.Research,
        Members = _session.City.Soldiers.OrderBy(u => u.Id).Select(u => new { u.Id, u.Health, u.Level, u.Experience, u.Profile, u.Assignment, u.RecoveryEligible }) }, WireJson.Options);
    private async Task CheckSquadRecoveryChoices()
    {
        // Compare the retained individual route with the finite wrapper from the same earned checkpoint.
        ReplaceSession(SquadRecoveryReview.Create("obsolete-equipment")); _frozen = true; RefreshUi(true);
        int squad = SquadRecoveryReview.WoundedSquad(_session.City);
        var plan = SquadRecovery.Project(_session.Snapshot, _session.PlayerId, squad, _session.City.Army!.Halls[0].Slot, RecoveryMove.StoreWounded);
        await OpenRecoveryHome(squad); _gameUi.Details.Army.ShowRoster(); await JourneyFrames();
        int individualStart = _nativeJourneyActions;
        foreach (var member in plan.Moving)
        {
            await JourneyClick(_gameUi.Details.Army.MemberButtons["Member" + member.Before.Id]);
            await JourneyClick(_gameUi.Details.Inspector.Buttons["Store"]);
        }
        int individualActions = _nativeJourneyActions - individualStart;
        string individual = RecoveryRoster();
        ReplaceSession(SquadRecoveryReview.Create("obsolete-equipment")); _frozen = true; RefreshUi(true);
        squad = SquadRecoveryReview.WoundedSquad(_session.City); await OpenRecoveryHome(squad);
        var panel = _gameUi.Details.Army.Recovery; var stock = _session.City.Resources;
        int batchStart = _nativeJourneyActions; long revision = _session.Snapshot.Revision;
        await JourneyClick(panel.Actions["Recover"]);
        Require(_session.Snapshot.Revision == revision && _session.City.Resources == stock, "review never moves or pays");
        Require(panel.Copy.Contains("3/3") && panel.Copy.Contains("No HP now") && panel.Copy.Contains("Food"), "review exposes exact group, timing and burden");
        await GameplayCapture("squad-recovery-review");
        var stale = panel.Actions["Commit"];
        await JourneyClick(panel.Actions["Commit"]);
        await JourneyWait(() => panel.Sequence is { Finished: true }, "finite member sequence settles");
        int batchActions = _nativeJourneyActions - batchStart;
        Require(batchActions < individualActions, $"squad recovery reduces actual navigation: {individualActions}→{batchActions}");
        Require(RecoveryRoster() == individual, "native individual and finite group commands preserve identical persistent roster/resources");
        foreach (var member in plan.Moving) Require(_session.City.Soldiers.Single(u => u.Id == member.Before.Id).Health == member.Before.Health, "storage grants no instant healing");
        await GameplayCapture("squad-recovery-applied");
        _gameUi.Details.Close(); if (IsInstanceValid(stale)) stale.EmitSignal(BaseButton.SignalName.Pressed);
        long after = _session.Snapshot.Revision;
        _session.RetryLast(); Require(_session.Snapshot.Revision == after && _session.City.Resources == stock, "exact member retry does not repeat movement or cost");
        _session.Send("ready"); _frozen = true; RefreshUi(true); await JourneyFrames();
        foreach (var member in plan.Moving) Require(_session.City.Soldiers.Single(u => u.Id == member.Before.Id).Health == member.NextHealth, "actual eligible capped payment matches group preview");
        Require(_session.City.LastUpkeep!.Participating.Intersect(plan.Moving.Select(m => m.Before.Id)).Count() == 0, "stored group does not fight");

        ReplaceSession(SquadRecoveryReview.Create("partial-hall")); _frozen = true; RefreshUi(true);
        squad = SquadRecoveryReview.WoundedSquad(_session.City); await OpenRecoveryHome(squad); panel = _gameUi.Details.Army.Recovery;
        await JourneyClick(panel.Actions["Recover"]); Require(panel.Copy.Contains("1/3"), "partial hall never promises the whole squad");
        panel.Actions["Commit"].GrabFocus();
        GetViewport().PushInput(new InputEventKey { Keycode = Key.Enter, Pressed = true }, true);
        GetViewport().PushInput(new InputEventKey { Keycode = Key.Enter, Pressed = false }, true);
        await JourneyWait(() => panel.Sequence is { Finished: true }, "keyboard partial move");
        Require(panel.Sequence!.Completed == 1, "keyboard moves only reviewed capacity"); await GameplayCapture("squad-recovery-partial");

        ReplaceSession(SquadRecoveryReview.Create("obsolete-equipment")); _frozen = true; RefreshUi(true);
        squad = SquadRecoveryReview.WoundedSquad(_session.City); await OpenRecoveryHome(squad); panel = _gameUi.Details.Army.Recovery;
        await JourneyClick(panel.Actions["Recover"]); stale = panel.Actions["Commit"]; revision = _session.Snapshot.Revision;
        panel.Actions["Cancel"].GrabFocus();
        GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = JoyButton.A, Pressed = true }, true);
        GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = JoyButton.A, Pressed = false }, true);
        await JourneyFrames(); Require(_session.Snapshot.Revision == revision, "controller cancel keeps every soldier and resource");
        if (IsInstanceValid(stale)) stale.EmitSignal(BaseButton.SignalName.Pressed);
        Require(_session.Snapshot.Revision == revision, "cancelled confirmation cannot execute a newer plan");
        await GameplayCapture("squad-recovery-cancelled");

        ReplaceSession(SquadRecoveryReview.Create("recovered-return")); _frozen = true; RefreshUi(true);
        var returnBefore = _session.City; await JourneyClick(_gameUi.ArmyNavigationButton);
        var tabs = _gameUi.Details.Army.DetailTabs; var tabBar = tabs.GetTabBar();
        WorldtreeClick(tabBar.GlobalPosition + tabBar.GetTabRect(2).GetCenter()); await JourneyFrames();
        Require(tabs.CurrentTab == 2, "native storage tab opens existing hall context");
        await JourneyClick(_gameUi.Details.Army.HallPanel.Buttons["ReturnRecovered"]);
        panel = _gameUi.Details.Army.Recovery; await GameplayCapture("squad-recovery-return-review");
        await JourneyClick(panel.Actions["Commit"]);
        await JourneyWait(() => panel.Sequence is { Finished: true }, "finite recovered return");
        Require(panel.Sequence!.Completed > 0 && _session.City.Resources == returnBefore.Resources, "recovered return pays no recruitment cost");
        foreach (var old in returnBefore.Soldiers)
        {
            var current = _session.City.Soldiers.Single(u => u.Id == old.Id);
            Require(current.Health == old.Health && current.Level == old.Level && current.Experience == old.Experience, "native return preserves identity wounds equipment and history");
        }
        await GameplayCapture("squad-recovery-return-applied");

        ReplaceSession(SquadRecoveryReview.Create("early-healthy")); _frozen = true; RefreshUi(true);
        var healthy = _session.City.Soldiers.MaxBy(u => u.Id)!; await OpenRecoveryHome(healthy.SquadId);
        Require(!_gameUi.Details.Army.Recovery.IsVisibleInTree(), "healthy selected squad has no maintenance prompt");
        await GameplayCapture("squad-recovery-healthy");
        if (Argument("--gameplay-captures") is { } directory)
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "squad-recovery-actions.json"), JsonSerializer.Serialize(new { individualActions, batchActions, members = plan.Moving.Length, noHumanTiming = true }, WireJson.Options));
        GD.Print($"GODOT_SQUAD_RECOVERY_PASS individual={individualActions} group={batchActions} members=3 exact-roster hp food partial keyboard controller cancel no-healthy-prompt");
    }
}
