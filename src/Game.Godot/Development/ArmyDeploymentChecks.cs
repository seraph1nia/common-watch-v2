using Game.Core;
using Godot;

namespace Game.GodotClient;

public partial class GameApplication
{
    private async Task CheckArmyDeployment()
    {
        ReplaceSession(Game.Shared.ScenarioRunner.CreateAt("settlement")); _frozen = true;
        _session.Send("ready"); _session.Send("recruit", 1);
        Require(_session.LastResult?.Accepted == true, "legal single squad for direct deployment");
        var member = _session.City.Soldiers.Single();
        RefreshUi(true);
        OpenDetails(DetailsPage.Army);
        var army = _gameUi.Details.Army; var homes = army.Deployment;
        SelectArmyOption(army.SquadSelector, member.SquadId); army.ShowHomes();
        await GameplayCapture("six-home-selection");
        Require(homes.HomeButtons.Count == 12 && homes.SelectedSquad == member.SquadId, "one shared squad selection and exactly six native home targets/manage controls");
        foreach (var button in homes.HomeButtons.Values)
            Require(WorldArea.Encloses(button.GetGlobalRect()) && army.HomeViewport.GetGlobalRect().Encloses(button.GetGlobalRect()), "all six native home targets and manage actions fit together");
        long before = _session.Snapshot.Revision;
        Click(homes.HomeButtons["Deploy10"].GetGlobalRect().GetCenter()); _frozen = true;
        Require(_session.LastResult?.Accepted == true && _session.City.Soldiers.Single().Assignment!.Tile == 1 && _session.Snapshot.Revision > before, "native click deploys a selected squad without a cell identifier");
        Require(army.SelectedSquad == member.SquadId && homes.SelectedSquad == member.SquadId, "accepted relocation retains selected squad");
        var target = homes.HomeButtons["Deploy9"]; target.GrabFocus();
        GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = JoyButton.A, Pressed = true }, true);
        GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = JoyButton.A, Pressed = false }, true);
        Require(_session.City.Soldiers.Single().Assignment!.Tile == 0, "native controller deploys to focused home");
        homes.HomeButtons["Deploy10"].GrabFocus();
        GetViewport().PushInput(new InputEventKey { Keycode = Key.Enter, Pressed = true }, true);
        GetViewport().PushInput(new InputEventKey { Keycode = Key.Enter, Pressed = false }, true);
        Require(_session.City.Soldiers.Single().Assignment!.Tile == 1, "native keyboard deploys to focused home");
        before = _session.Snapshot.Revision;
        var locked = homes.HomeButtons["Deploy11"];
        Require(locked.Disabled && locked.Text.Contains("Purchase this home first"), "locked destination explains restriction before submission");
        Click(locked.GetGlobalRect().GetCenter()); locked.EmitSignal(BaseButton.SignalName.Pressed);
        Require(_session.Snapshot.Revision == before, "disabled or stale home target cannot submit an illegal placement");
        await GameplayCapture("six-home-relocated");
        army.ShowManagementFor(1, new(_session, ResumeAfterCommand)); RefreshUi(true);
        Require(army.Recruitment.Buttons["RecruitSwordsman"].Text.Contains("Home 2"), "recruitment shows selected squad destination and capacity");
        Press(army.Recruitment.Buttons["RecruitSwordsman"]);
        Require(_session.City.Soldiers.All(u => u.Assignment!.Tile == 1) && army.SelectedSquad == member.SquadId, "reinforcement arrives at visible destination with context retained");
        _session.Send("ready"); _session.Send("recruit", 1, squadId: -1); _frozen = true;
        Require(_session.LastResult?.Accepted == true, "second compatible squad earned through production");
        var other = _session.City.Soldiers.First(u => u.SquadId != member.SquadId);
        army.ShowRoster(); RefreshUi(true); Press(army.MemberButtons["Member" + member.Id]);
        var inspector = _gameUi.Details.Inspector;
        SelectArmyOption(inspector.SquadSelector, other.SquadId);
        Require(inspector.DestinationDetails.Contains("Home 1"), "regroup destination preview uses a visible home");
        inspector.Buttons["Regroup"].GrabFocus(); await GameplayCapture("member-regroup-destination");
        Click(inspector.Buttons["Regroup"].GetGlobalRect().GetCenter()); _frozen = true; RefreshUi(true);
        Require(_session.LastResult?.Accepted == true && _session.City.Soldiers.Single(u => u.Id == member.Id).SquadId == other.SquadId
            && inspector.SelectedUnit == member.Id && army.SelectedSquad == other.SquadId, "regroup follows the same member into its accepted squad");
        await GameplayCapture("member-regroup-retained");

        ReplaceSession(Game.Shared.ScenarioRunner.CreateAt("initial")); _frozen = true;
        _session.Send("build", 9, Building.ArrowTower); Require(_session.LastResult?.Accepted == true, "legal tower on army home");
        RefreshUi(true);
        OpenDetails(DetailsPage.Army); army = _gameUi.Details.Army; homes = army.Deployment; army.ShowHomes();
        await GameplayCapture("home-tower-locked-empty");
        Require(homes.HomeButtons["Deploy9"].Text.Contains("Tower occupies") && homes.HomeButtons["Deploy10"].Text.Contains("Empty")
            && homes.HomeButtons["Deploy11"].Text.Contains("Unpurchased"), "tower, empty and locked homes are visibly distinct");
        Press(homes.HomeButtons["Inspect11"]); await GameplayCapture("home-purchase-quote");
        int gold = _session.City.Gold; var quote = homes.Homes.Single(h => h.Home.Number == 3).Purchase!.Value;
        homes.Inspector.Actions["Buy"].GrabFocus();
        Press(homes.Inspector.Actions["Buy"]);
        Require(_session.LastResult?.Accepted == true && _session.City.Gold == gold - quote.Gold && _session.City.Army!.Homes[2].Purchased
            && _session.City.Slots.Length == 9, "native home purchase uses existing quote without changing settlement plots");
        await GameplayCapture("home-purchased");
        Require(GetViewport().GuiGetFocusOwner() is { } restored && restored.IsVisibleInTree(), "purchase restores native focus after its quoted action disappears");

        ReplaceSession(Game.Shared.GameplayScenarios.CreateAt("forecast")); _frozen = true; RefreshUi(true); OpenDetails(DetailsPage.Army);
        army = _gameUi.Details.Army; army.ShowHomes(); await GameplayCapture("six-home-food-shortage");
        Require(army.Deployment.Homes.Sum(h => h.Members.Length - h.Participating) > 0 && army.Deployment.HomeButtons.Values.Any(b => b.Text.Contains("FULL")), "full squads and Food sit-outs visible on home board");
        _gameUi.Details.Close(); Press(_gameUi.ArenaButton); await GameplayCapture("funded-deployment-preview");
        var expected = BattleDeployment.Preview(_session.Snapshot).Where(u => u.Deployed).ToArray();
        Require(expected.Select(u => u.Id).Order().SequenceEqual(_arenaPreview.ParticipatingUnits.Select(u => u.Id).Order()), "preview shows exactly Core's projected participating members");
        foreach (var unit in _arenaPreview.ParticipatingUnits)
            Require(expected.Single(u => u.Id == unit.Id).Hex!.Position == unit.Hex!.Position, "preview positions are Core battle-entry positions");
        var watchpost = BattleDeployment.Structures(_session.Snapshot).Single(s => s.Owner == _session.PlayerId && s.Kind == ArenaStructureKind.Watchpost);
        _gameUi.Inspect(watchpost); await GameplayCapture("deployment-watchpost-inspection");
        Require(_gameUi.Details.Inspector.Details.Contains($"HP {HealthPoints.Format(watchpost.Health)}/{HealthPoints.Format(watchpost.MaximumHealth)}"), "inspected Watchpost uses the same reset projection as the preparation figure");
        GD.Print("GODOT_ARMY_DEPLOYMENT_PASS six-homes native-pointer keyboard controller restrictions purchase recruitment regroup context food preview unchanged-rules");
    }
    private static void SelectArmyOption(OptionButton option, int id)
    {
        int index = option.GetItemIndex(id); Require(index >= 0, "native squad choice exists");
        option.Select(index); option.EmitSignal(OptionButton.SignalName.ItemSelected, (long)index);
    }
}
