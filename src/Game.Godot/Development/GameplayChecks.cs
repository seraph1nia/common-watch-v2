using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

/// <summary>Actual native Controls and legal default-rule snapshots. No authoritative fixtures.</summary>
public partial class GameApplication
{
    private async Task GameplayUiChecks()
    {
        _frozen = true;
        ReplaceSession(ScenarioRunner.CreateAt("initial")); _frozen = true;
        _session.SelectSlot(0); RefreshUi(true);
        Require(_gameUi.Inspector.BuildButtons.Count == _session.Snapshot.BuildingCatalog.Length, "complete construction catalog");
        foreach (var definition in _session.Snapshot.BuildingCatalog)
            Require(_gameUi.Inspector.BuildButtons.ContainsKey(definition.Type), "catalog entry " + definition.Type);
        Press(_gameUi.Inspector.BuildButtons[Building.Lumbermill]);
        Require(_session.City.Slots[0].Type == Building.Lumbermill, "native paid construction");
        var button = _gameUi.Inspector.Actions["Sell"]; button.GrabFocus();
        RefreshUi(true); Require(button == _gameUi.Inspector.Actions["Sell"] && button.HasFocus(), "native control identity and keyboard focus survive snapshot refresh");
        Press(button); Require(_session.City.Slots[0].Type == Building.Empty, "native sale");
        OpenDetails(5); await GameplayCapture("settlement-full-catalog"); _gameUi.Details.Close();

        ReplaceSession(GameplayScenarios.CreateAt("economy")); _frozen = true; _session.SelectSlot(0); RefreshUi(true);
        await GameplayCapture("resources-and-upkeep");
        OpenDetails(5); await GameplayCapture("building-upgrades"); _gameUi.Details.Close();
        int market = Array.FindIndex(_session.City.Slots, s => s.Type == Building.Market);
        _session.SelectSlot(market); RefreshUi(true);
        Require(_gameUi.Inspector.Market.Buttons.Count == 5, "every authoritative Market rate");
        var stock = _session.City.Resources;
        _gameUi.Inspector.Market.Bundles.Value = 2;
        Press(_gameUi.Inspector.Market.Buttons["TradeMetal"]);
        Require(_session.City.Metal == stock.Metal - 10 && _session.LastResult?.Accepted == true, "native bundle count and resource submission");
        OpenDetails(5); await GameplayCapture("market-trading"); _gameUi.Details.Close();

        ReplaceSession(GameplayScenarios.CreateAt("army")); _frozen = true; RefreshUi(true);
        Require(Enum.GetValues<UnitType>().All(t => _session.City.Soldiers.Any(u => u.Type == t)), "all four legally recruited classes");
        OpenDetails(0);
        int member = _session.City.Soldiers.First(u => u.Type == UnitType.Swordsman).Id;
        Press(_gameUi.Details.Army.MemberButtons["Member" + member]);
        Require(_gameUi.Details.Inspector.SelectedUnit == member && _gameUi.Details.Inspector.Details.Contains("service XP"), "reusable persistent member inspector");
        await GameplayCapture("army-roster"); await GameplayCapture("squad-management"); await GameplayCapture("unit-inspector");
        OpenDetails(1);
        Require(_gameUi.Details.Deployment.HomeButtons.Count == 12, "six numbered homes with inspect and deployment controls");
        var chosenSquad = _session.City.Soldiers.First(u => u.Assignment is { Stored: false });
        Press(_gameUi.Details.Deployment.HomeButtons["Deploy" + (chosenSquad.Assignment!.Tile + ArmyConfiguration.PlotOffset)]);
        Require(_session.LastResult?.Accepted == true, "native squad and arena-cell command");
        await GameplayCapture("army-deployment");
        _gameUi.Details.Close();

        ReplaceSession(GameplayScenarios.CreateAt("town-hall")); _frozen = true; RefreshUi(true); OpenDetails(0);
        var stored = _session.City.Soldiers.First(u => u.Assignment is { Stored: true });
        Press(_gameUi.Details.Army.MemberButtons["Member" + stored.Id]);
        Require(_gameUi.Details.Inspector.Details.Contains("paid recovery eligible"), "paid recovery shown from real roster");
        _gameUi.Details.Army.ShowManagement(); await GameplayCapture("town-hall");
        int health = stored.Health;
        Press(_gameUi.Details.Inspector.Buttons["Return"]);
        Require(_session.City.Soldiers.Single(u => u.Id == stored.Id).Assignment is { Stored: false }, "native veteran return");
        Press(_gameUi.Details.Inspector.Buttons["Store"]);
        Require(_session.City.Soldiers.Single(u => u.Id == stored.Id).Health == health, "native storage cannot heal independently");

        ReplaceSession(GameplayScenarios.CreateAt("research")); _frozen = true; RefreshUi(true); OpenDetails(2);
        Require(_gameUi.Details.Research.TechnologyButtons.Count == _session.Snapshot.TechnologyCatalog.Length, "every canonical technology inspectable");
        Press(_gameUi.Details.Research.TechnologyButtons["TechRangedFoundation"]);
        Press(_gameUi.Details.Research.PurchaseButtons["Purchase"]);
        Require(_session.City.Research.Has(TechnologyId.RangedFoundation), "native technology parameter purchase");
        Press(_gameUi.Details.Research.TechnologyButtons["TechAssault"]);
        Require(_gameUi.Details.Research.PurchaseButtons["Purchase"].Disabled, "exclusive sibling locked by authority");
        await GameplayCapture("worldtree");
        _gameUi.Details.Close(); OpenDetails(3);
        var enemy = _session.Snapshot.NextWave[0]; _gameUi.Inspect(enemy, true);
        Require(_gameUi.Details.Inspector.Details.Contains("NEXT WAVE") && _gameUi.Details.Inspector.Buttons.Values.All(b => !b.Visible), "forecast inspector is read-only");
        await GameplayCapture("next-wave-preview");
        _gameUi.Details.Close(); _gameUi.Details.Open(4, new(_session, ResumeAfterCommand)); await GameplayCapture("run-progression");

        ReplaceSession(GameplayScenarios.CreateAt("forecast")); _frozen = true; RefreshUi(true); RefreshWorld();
        Require(_session.City.FoodForecast is { Unfed.Length: > 0, Participating.Length: > 0 } && !_gameUi.ReadyButton.Disabled, "partial food funding never blocks readiness");
        _gameUi.OpenArena(); RefreshWorld();
        await GameplayCapture("arena-preparation");
        long forecastRevision = _session.Snapshot.Revision;
        var forecastEnemy = _session.Snapshot.NextWave[0];
        SelectAt(_camera.ToScreen(_arenaPreview.UnitPosition(forecastEnemy.Id)));
        Require(_gameUi.Details.Visible && _gameUi.Details.Inspector.SelectedUnit == forecastEnemy.Id, "native world forecast picking opens same inspector");
        Require(_session.Snapshot.Revision == forecastRevision, "forecast picking is read-only");
        _gameUi.Details.Close(); await GameplayCapture("food-shortage");
        var defense = _session.Snapshot.Structures.Single(s => s.Owner == _session.PlayerId && s.Slot == 7);
        _gameUi.Inspect(defense);
        Press(_gameUi.Details.Inspector.Buttons["ManageTower"]);
        Press(_gameUi.Details.Building.Actions["BranchBombardTower"]);
        Require(_session.City.Slots[7].Type == Building.BombardTower, "native structure inspection and authoritative tower branch");
        await GameplayCapture("defense-tower");

        ReplaceSession(GameplayScenarios.CreateAt("reward")); _frozen = true; RefreshUi(true);
        Require(_gameUi.Draft.RewardButtons.Count == 3, "actual three-card draft");
        long draftRevision = _session.Snapshot.Revision;
        _gameUi.Draft.Dismiss(); OpenDetails(4);
        Require(_session.Snapshot.Revision == draftRevision && _session.City.Run.Choice is null, "inspect run during draft without choosing or pausing");
        _gameUi.Details.Close(); _gameUi.Draft.Open(); RefreshUi(true);
        await GameplayCapture("reward-draft");
        Press(_gameUi.Draft.RewardButtons[0]);
        Require(_session.City.Run.Cards.Length > 0 && _session.Snapshot.Phase == Phase.Building, "actual draft accepted");

        ReplaceSession(GameplayScenarios.CreateAt("victory")); _frozen = true; RefreshUi(true); OpenDetails(4);
        Require(!_gameUi.Draft.Visible && _session.Snapshot.Wave == 20, "wave twenty terminal without new draft"); await GameplayCapture("victory");
        ReplaceSession(GameplayScenarios.CreateAt("defeat")); _frozen = true; RefreshUi(true); OpenDetails(4); await GameplayCapture("defeat");
        await CheckGameplayInput();
        GD.Print("GODOT_GAMEPLAY_PASS catalog economy trades four-classes squads homes townhall research forecast inspector rewards victory defeat native-focus modal-scroll");
    }
    private void OpenDetails(int tab) => _gameUi.Details.Open(tab, new GameplayContext(_session, ResumeAfterCommand));
    private void Press(Button button)
    {
        Require(!button.Disabled, "requested control enabled: " + button.Text);
        button.EmitSignal(BaseButton.SignalName.Pressed); _frozen = true; RefreshUi(true);
    }
    private async Task CheckGameplayInput()
    {
        ReplaceSession(ScenarioRunner.CreateAt("settlement")); _frozen = true; RefreshUi(true);
        foreach (var size in new[] { new Vector2I(1280, 720), new Vector2I(1920, 1080) })
        {
            GetWindow().Size = size; await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            OpenDetails(2); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            long revision = _session.Snapshot.Revision; float zoom = _camera.Magnification;
            int selection = _session.SelectedSlot;
            Click(new Vector2(10, 300));
            GetViewport().PushInput(new InputEventMouseButton { Position = new Vector2(300, 350), ButtonIndex = MouseButton.WheelUp, Pressed = true }, true);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(_camera.Magnification == zoom && _session.SelectedSlot == selection && _session.Snapshot.Revision == revision, "native modal blocks world input and never pauses authority");
            Require(GetViewport().GetVisibleRect().Encloses(_gameUi.Details.GetGlobalRect()), "details fits " + size);
            await GameplayCapture("worldtree-" + size.X);
            _gameUi.Details.Close();
        }
    }
    private async Task GameplayCapture(string name)
    {
        if (Argument("--gameplay-captures") is not { } directory) return;
        RefreshUi(true); RefreshWorld();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        System.IO.Directory.CreateDirectory(directory);
        string path = System.IO.Path.Combine(directory, name + ".png");
        using var image = GetViewport().GetTexture().GetImage(); Require(image.SavePng(path) == Error.Ok, "gameplay capture " + name);
        GD.Print("GODOT_GAMEPLAY_CAPTURE " + path);
    }
}
