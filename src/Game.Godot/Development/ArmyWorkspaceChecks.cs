// SPDX-License-Identifier: GPL-3.0-only
using System.Text.Json;
using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class GameApplication
{
    private async Task TraceArmyWorkspace()
    {
        ReplaceSession(ScenarioRunner.CreateAt("settlement")); _frozen = true;
        _session.Send("ready"); _session.Send("recruit", 1); _session.Send("ready");
        Require(_session.LastResult!.Accepted && _session.City.Soldiers.Length == 1, "legal paid army task baseline");
        RefreshUi(true); await JourneyFrames();
        var trace = new List<object>(); int navigations = 0, gestures = 0;
        async Task Step(string name, bool navigation, Func<Task> action, bool command = false)
        {
            long revision = _session.Snapshot.Revision; var resources = _session.City.Resources;
            await action(); _frozen = true; RefreshUi(true); await JourneyFrames();
            if (navigation) navigations++;
            if (command) Require(_session.LastResult!.Accepted && _session.Snapshot.Revision > revision, "native task command accepted: " + name);
            else Require(_session.Snapshot.Revision == revision, "local task step submits no command: " + name);
            trace.Add(new { name, navigation, before = resources, after = _session.City.Resources,
                command = command ? _session.LastResult : null, gestures, soldiers = _session.City.Soldiers.Select(u => new { u.Id, u.SquadId, u.Assignment }).ToArray() });
            await GameplayCapture("army-task-" + name);
        }
        async Task PressNative(Button button) { gestures++; await JourneyClick(button); }
        async Task TabNative(int index)
        {
            var tabs = _gameUi.Details.Army.SectionTabs;
            WorldtreeClick(tabs.GlobalPosition + tabs.GetTabRect(index).GetCenter()); gestures++;
            await JourneyFrames();
        }
        await Step("open", true, () => PressNative(_gameUi.ArmyNavigationButton));
        await Step("select-squad", false, async () =>
        {
            var selector = _gameUi.Details.Army.SquadSelector;
            await PressNative(selector);
            var popup = selector.GetPopup();
            // The legacy native popup runs in a separate window. Record its
            // actual OptionButton choice through the existing native selection
            // seam; this step is not claimed as OS pointer/keyboard coverage.
            popup.Hide(); SelectArmyOption(selector, _session.City.Soldiers[0].SquadId);
            gestures++; await JourneyFrames();
            Require(_gameUi.Details.Army.SelectedSquad == _session.City.Soldiers[0].SquadId, "native squad choice");
        });
        await Step("recruit-section", true, () => TabNative(1));
        var quote = _session.City.RecruitmentQuotes.Single(q => q.Type == UnitType.Swordsman && q.Level == _session.City.Slots[1].Level);
        var stock = _session.City.Resources;
        await Step("reinforce", false, () => PressNative(_gameUi.Details.Army.Recruitment.Buttons["RecruitSwordsman"]), command: true);
        Require(_session.City.Soldiers.Length == 2 && _session.City.Soldiers.Select(u => u.SquadId).Distinct().Count() == 1, "compatible reinforcement keeps the real squad");
        foreach (Resource resource in Enum.GetValues<Resource>())
            Require(_session.City.Resources.Amount(resource) == stock.Amount(resource) - quote.Cost.Amount(resource), "actual recruitment payment");
        await Step("homes-section", true, () => TabNative(3));
        await Step("deploy", false, () => PressNative(_gameUi.Details.Deployment.HomeButtons["Deploy10"]), command: true);
        Require(_session.City.Soldiers.All(u => u.Assignment!.Tile == 1), "accepted placement uses the actual home");
        await Step("inspect-land", false, () => PressNative(_gameUi.Details.Deployment.HomeButtons["Inspect11"]));
        await Step("buy-home", false, () => PressNative(_gameUi.Details.Deployment.Inspector.Actions["Buy"]), command: true);
        await Step("settlement", true, () => PressNative(VisibleHudActions(_gameUi.Details).First(b => b.Text.StartsWith("Close [Esc]"))));
        await Step("preview", false, () => PressNative(_gameUi.ArenaButton));
        var expected = _arenaPreview.ParticipatingUnits.Where(u => u.Faction == Faction.Adventurers && u.Owner == _session.PlayerId).ToDictionary(u => u.Id, u => u.Hex!.Position);
        Require(expected.Count == 2, "actual funded preview of the reinforced squad");
        await Step("battle", false, () => PressNative(_gameUi.ReadyButton), command: true);
        Require(_session.Snapshot.Phase == Phase.Combat, "native final Ready starts real combat");
        foreach (var unit in _session.Snapshot.Players.SelectMany(p => p.Soldiers).Where(u => u.Deployed))
            Require(expected.TryGetValue(unit.Id, out var position) && unit.Hex!.Position == position, "preview and accepted battle use identical entry cells");
        var report = new { layout = "before", width = GetWindow().Size.X, height = GetWindow().Size.Y, gestures, navigations,
            inputQualification = "Pointer controls except legacy squad OptionButton choice via native selection signal; popup OS input is unqualified.",
            withinArmySectionChanges = 2, trace, limitation = "Native scripted controls and legal setup, not human speed/comprehension evidence." };
        if (Argument("--gameplay-captures") is { } directory)
        {
            System.IO.Directory.CreateDirectory(directory);
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "army-workspace-task.json"), JsonSerializer.Serialize(report, WireJson.Options));
        }
        GD.Print($"GODOT_ARMY_WORKSPACE_TASK_PASS layout=before gestures={gestures} navigations={navigations} sections=2 native-reinforce-deploy-purchase-preview-entry");
    }
}
