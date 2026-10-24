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
            if (_gameUi.Details.Army.IsVisibleInTree())
            {
                Require(_gameUi.Details.Army.DetailTabsFit, "all secondary disclosures remain directly visible");
                foreach (var target in _gameUi.Details.Deployment.HomeButtons.Values)
                    Require(GetViewport().GetVisibleRect().Encloses(target.GetGlobalRect()), "all six homes remain visible throughout task");
            }
            await GameplayCapture("army-task-" + name);
        }
        async Task PressNative(Button button) { gestures++; await JourneyClick(button); }
        await Step("open", true, () => PressNative(_gameUi.ArmyNavigationButton));
        Require(_gameUi.Details.Army.SelectedSquad == _session.City.Soldiers[0].SquadId, "occupied home is the default squad context");
        var quote = _session.City.RecruitmentQuotes.Single(q => q.Type == UnitType.Swordsman && q.Level == _session.City.Slots[1].Level);
        var stock = _session.City.Resources;
        await Step("reinforce", false, () => PressNative(_gameUi.Details.Army.Recruitment.Buttons["RecruitSwordsman"]), command: true);
        Require(_session.City.Soldiers.Length == 2 && _session.City.Soldiers.Select(u => u.SquadId).Distinct().Count() == 1, "compatible reinforcement keeps the real squad");
        foreach (Resource resource in Enum.GetValues<Resource>())
            Require(_session.City.Resources.Amount(resource) == stock.Amount(resource) - quote.Cost.Amount(resource), "actual recruitment payment");
        await Step("select-destination", false, () => PressNative(_gameUi.Details.Deployment.HomeButtons["Home10"]));
        await Step("deploy", false, () => PressNative(_gameUi.Details.Army.DeployButton), command: true);
        Require(_session.City.Soldiers.All(u => u.Assignment!.Tile == 1), "accepted placement uses the actual home");
        await Step("inspect-land", false, () => PressNative(_gameUi.Details.Deployment.HomeButtons["Home11"]));
        await Step("buy-home", false, () => PressNative(_gameUi.Details.Army.HomeInspector.Actions["Buy"]), command: true);
        await Step("preview", true, () => PressNative(_gameUi.Details.Army.PreviewButton));
        var expected = _arenaPreview.ParticipatingUnits.Where(u => u.Faction == Faction.Adventurers && u.Owner == _session.PlayerId).ToDictionary(u => u.Id, u => u.Hex!.Position);
        Require(expected.Count == 2, "actual funded preview of the reinforced squad");
        await Step("battle", false, () => PressNative(_gameUi.ReadyButton), command: true);
        Require(_session.Snapshot.Phase == Phase.Combat, "native final Ready starts real combat");
        foreach (var unit in _session.Snapshot.Players.SelectMany(p => p.Soldiers).Where(u => u.Deployed))
            Require(expected.TryGetValue(unit.Id, out var position) && unit.Hex!.Position == position, "preview and accepted battle use identical entry cells");
        var report = new { layout = "after", width = GetWindow().Size.X, height = GetWindow().Size.Y, gestures, navigations,
            inputQualification = "All task actions use native pointer input.",
            withinArmySectionChanges = 0, trace, limitation = "Native scripted controls and legal setup, not human speed/comprehension evidence." };
        if (Argument("--gameplay-captures") is { } directory)
        {
            System.IO.Directory.CreateDirectory(directory);
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "army-workspace-task.json"), JsonSerializer.Serialize(report, WireJson.Options));
        }
        GD.Print($"GODOT_ARMY_WORKSPACE_TASK_PASS layout=after gestures={gestures} navigations={navigations} sections=0 native-reinforce-deploy-purchase-preview-entry");
    }
}
