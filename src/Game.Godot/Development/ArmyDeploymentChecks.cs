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
        army.SelectSquad(member.SquadId); army.ShowHomes();
        await GameplayCapture("six-home-selection");
        Require(army.DetailTabsFit, "all secondary disclosures, including quotes, fit without horizontal scrolling");
        Require(homes.HomeButtons.Count == 6 && homes.SelectedSquad == member.SquadId, "one shared squad selection and exactly six retained native home selectors");
        foreach (var button in homes.HomeButtons.Values)
            Require(WorldArea.Encloses(button.GetGlobalRect()) && army.HomeViewport.GetGlobalRect().Encloses(button.GetGlobalRect()), "all six native home inspection targets fit together");
        foreach (bool controller in new[] { false, true })
        {
            homes.HomeButtons["Home9"].GrabFocus(); await JourneyFrames();
            foreach (var (home, key, pad) in new[] { (10, Key.Right, JoyButton.DpadRight), (11, Key.Right, JoyButton.DpadRight),
                (14, Key.Down, JoyButton.DpadDown), (13, Key.Left, JoyButton.DpadLeft), (12, Key.Left, JoyButton.DpadLeft), (9, Key.Up, JoyButton.DpadUp) })
            {
                if (controller)
                {
                    GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = pad, Pressed = true }, true);
                    GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = pad, Pressed = false }, true);
                }
                else
                {
                    GetViewport().PushInput(new InputEventKey { Keycode = key, Pressed = true }, true);
                    GetViewport().PushInput(new InputEventKey { Keycode = key, Pressed = false }, true);
                }
                await JourneyFrames();
                Require(homes.HomeButtons["Home" + home].HasFocus(), $"native {(controller ? "D-pad" : "arrow")} navigation reaches Home {home - 8}, including locked inspection targets");
            }
        }
        long before = _session.Snapshot.Revision;
        await JourneyClick(homes.HomeButtons["Home10"]); await JourneyClick(army.DeployButton); _frozen = true;
        Require(_session.LastResult?.Accepted == true && _session.City.Soldiers.Single().Assignment!.Tile == 1 && _session.Snapshot.Revision > before, "native click confirms selected-home deployment");
        Require(army.SelectedSquad == member.SquadId, "accepted relocation retains selected squad");
        async Task Activate(Button button, bool controller)
        {
            button.GrabFocus(); await JourneyFrames();
            if (controller)
            {
                GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = JoyButton.A, Pressed = true }, true);
                GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = JoyButton.A, Pressed = false }, true);
            }
            else
            {
                GetViewport().PushInput(new InputEventKey { Keycode = Key.Enter, Pressed = true }, true);
                GetViewport().PushInput(new InputEventKey { Keycode = Key.Enter, Pressed = false }, true);
            }
            _frozen = true; RefreshUi(true); await JourneyFrames();
        }
        await Activate(homes.HomeButtons["Home9"], true); await Activate(army.DeployButton, true);
        Require(_session.City.Soldiers.Single().Assignment!.Tile == 0, "native controller selects and confirms home");
        await Activate(homes.HomeButtons["Home10"], false); await Activate(army.DeployButton, false);
        Require(_session.City.Soldiers.Single().Assignment!.Tile == 1, "native keyboard selects and confirms home");
        before = _session.Snapshot.Revision;
        await JourneyClick(homes.HomeButtons["Home11"]);
        Require(army.DeployButton.Disabled && army.DeployButton.TooltipText.Contains("Purchase this home first"), "locked home remains inspectable with true placement restriction");
        army.DeployButton.EmitSignal(BaseButton.SignalName.Pressed);
        Require(_session.Snapshot.Revision == before, "stale disabled confirmation cannot submit a placement");
        await JourneyClick(homes.HomeButtons["Home10"]);
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
        await JourneyClick(inspector.Buttons["Regroup"]); _frozen = true; RefreshUi(true);
        Require(_session.LastResult?.Accepted == true && _session.City.Soldiers.Single(u => u.Id == member.Id).SquadId == other.SquadId
            && inspector.SelectedUnit == member.Id && army.SelectedSquad == other.SquadId, "regroup follows the same member into its accepted squad");
        await GameplayCapture("member-regroup-retained");

        ReplaceSession(Game.Shared.ScenarioRunner.CreateAt("initial")); _frozen = true;
        _session.Send("build", 9, Building.ArrowTower); Require(_session.LastResult?.Accepted == true, "legal tower on army home");
        RefreshUi(true);
        OpenDetails(DetailsPage.Army); army = _gameUi.Details.Army; homes = army.Deployment; army.ShowHomes();
        await GameplayCapture("home-tower-locked-empty");
        Require(homes.HomeButtons["Home9"].Text.Contains("ArrowTower") && homes.HomeButtons["Home10"].Text.Contains("Empty")
            && homes.HomeButtons["Home11"].Text.Contains("Unpurchased"), "tower, empty and locked homes are visibly distinct");
        Press(homes.HomeButtons["Home11"]); await GameplayCapture("home-purchase-quote");
        int gold = _session.City.Gold; var quote = homes.Homes.Single(h => h.Home.Number == 3).Purchase!.Value;
        army.HomeInspector.Actions["Buy"].GrabFocus();
        Press(army.HomeInspector.Actions["Buy"]);
        Require(_session.LastResult?.Accepted == true && _session.City.Gold == gold - quote.Gold && _session.City.Army!.Homes[2].Purchased
            && _session.City.Slots.Length == 9, "native home purchase uses existing quote without changing settlement plots");
        await GameplayCapture("home-purchased");
        Require(GetViewport().GuiGetFocusOwner() is { } restored && restored.IsVisibleInTree(), "purchase restores native focus after its quoted action disappears");

        foreach (var branch in new[] { Building.RapidArrowTower, Building.BombardTower })
        {
            ReplaceSession(Game.Shared.ScenarioRunner.CreateAt("initial")); _frozen = true;
            _session.Send("build", 0, Building.Lumbermill); Require(_session.LastResult!.Accepted, "paid tower-test wood producer");
            _session.Send("build", 1, Building.Stonecutter); Require(_session.LastResult!.Accepted, "paid tower-test stone producer");
            if (branch == Building.BombardTower) { _session.Send("build", 2, Building.MetalMine); Require(_session.LastResult!.Accepted, "paid Bombard metal producer"); }
            _session.Send("ready"); Require(_session.LastResult!.Accepted, "earned tower construction materials"); RefreshUi(true); OpenDetails(DetailsPage.Army);
            if (branch == Building.BombardTower) { _session.Send("sell", 2); Require(_session.LastResult!.Accepted, "sell paid Metal Mine after its production to fund construction wood"); RefreshUi(true); }
            army = _gameUi.Details.Army; homes = army.Deployment;
            await JourneyClick(homes.HomeButtons["Home9"]);
            var stock = _session.City.Resources; var construction = _session.Snapshot.BuildingCatalog.Single(b => b.Type == Building.ArrowTower).Construction;
            await JourneyClick(army.HomeInspector.BuildButtons[Building.ArrowTower]);
            await JourneyClick(army.HomeInspector.Construction.ConfirmButton);
            CheckArmyPayment(stock, construction, "home tower construction");
            Require(_session.City.Army!.Homes[0].Plot.Type == Building.ArrowTower && homes.HomeButtons.Count == 6, "native tower shares the real home footprint");
            _session.Send("ready"); _frozen = true;
            if (branch == Building.BombardTower) { _session.Send("sell", 1); Require(_session.LastResult!.Accepted, "sell paid Stonecutter after both productions for the remaining branch wood"); }
            RefreshUi(true);
            var economy = new EconomyConfiguration(_session.Snapshot.Rules); Require(economy.TryTowerUpgrade(branch, out var cost), "authoritative tower branch quote"); stock = _session.City.Resources;
            await JourneyClick(army.HomeInspector.Actions["Branch" + branch]);
            CheckArmyPayment(stock, cost, "home tower branch");
            Require(_session.City.Army!.Homes[0].Plot.Type == branch, "selected native branch is accepted");
            var refund = _session.City.Army.Homes[0].Plot.Refund; stock = _session.City.Resources;
            await GameplayCapture("home-tower-" + branch);
            await JourneyClick(army.HomeInspector.Actions["Sell"]);
            Require(_session.LastResult!.Accepted && _session.City.Army!.Homes[0].Plot.Type == Building.Empty, "native tower sale frees its existing home");
            foreach (Resource resource in Enum.GetValues<Resource>()) Require(_session.City.Resources.Amount(resource) == stock.Amount(resource) + refund.Amount(resource), "exact home tower refund");
        }

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
    private void CheckArmyPayment(ResourceCost before, ResourceCost cost, string operation)
    {
        Require(_session.LastResult!.Accepted, operation + " accepted");
        foreach (Resource resource in Enum.GetValues<Resource>())
            Require(_session.City.Resources.Amount(resource) == before.Amount(resource) - cost.Amount(resource), operation + " exact " + resource + " payment");
    }
    private static void SelectArmyOption(OptionButton option, int id)
    {
        int index = option.GetItemIndex(id); Require(index >= 0, "native squad choice exists");
        option.Select(index); option.EmitSignal(OptionButton.SignalName.ItemSelected, (long)index);
    }
}
