using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

/// <summary>Actual native Controls and legal default-rule snapshots. No authoritative fixtures.</summary>
public partial class GameApplication
{
    private async Task GameplayUiChecks()
    {
        await CheckMenuPresentation();
        await CheckArenaFraming();
        _frozen = true;
        ReplaceSession(ScenarioRunner.CreateAt("initial")); _frozen = true;
        _session.SelectSlot(-1); RefreshUi(true); await GameplayCapture("hud-idle");
        _session.SelectSlot(0); RefreshUi(true); await GameplayCapture("hud-build");
        Require(_gameUi.Inspector.Construction.Categories.Count == 5, "five native construction categories");
        for (int category = 0; category < _gameUi.Inspector.Construction.Categories.Count; category++)
        {
            var tab = _gameUi.Inspector.Construction.Categories[category];
            Press(tab); await GameplayCapture("build-" + tab.Text.ToLowerInvariant());
            foreach (var definition in _session.Snapshot.BuildingCatalog)
            {
                var card = _gameUi.Inspector.BuildButtons[definition.Type];
                Require(card.Visible == (ConstructionPanel.CategoryOf(definition) == category), "category filters " + definition.Type);
                if (!card.Visible) continue;
                Require(_gameUi.ContextArea.Encloses(card.GetGlobalRect()), "complete construction option fits without scrolling: " + definition.Type);
            }
        }
        Press(_gameUi.Inspector.Construction.Categories[0]);
        Require(_gameUi.ContextArea.Encloses(_gameUi.Inspector.Construction.Categories[^1].GetGlobalRect()), "all five construction categories fit the floating panel");
        _session.SelectSlot(7); RefreshUi(true); await GameplayCapture("hud-locked");
        _session.SelectSlot(0); RefreshUi(true);
        Require(_gameUi.Inspector.BuildButtons.Count == _session.Snapshot.BuildingCatalog.Length, "complete construction catalog");
        foreach (var definition in _session.Snapshot.BuildingCatalog)
            Require(_gameUi.Inspector.BuildButtons.ContainsKey(definition.Type), "catalog entry " + definition.Type);
        long beforeSelection = _session.Snapshot.Revision;
        Press(_gameUi.Inspector.BuildButtons[Building.Lumbermill]);
        Require(_session.Snapshot.Revision == beforeSelection, "selecting a build card spends nothing");
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var selectedCard = _gameUi.Inspector.BuildButtons[Building.Lumbermill]; selectedCard.GrabFocus();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        MoveMouse(_gameUi.Inspector.BuildButtons[Building.Farm].GetGlobalRect().GetCenter());
        await GameplayCapture("hud-build-focus-hover");
        Require(selectedCard.HasFocus() && selectedCard.ButtonPressed, $"keyboard focus remains independent of pointer hover and card selection: focus={selectedCard.HasFocus()}, selected={selectedCard.ButtonPressed}, owner={GetViewport().GuiGetFocusOwner()?.Name}");
        Require(_gameUi.Inspector.Construction.CardArea.Encloses(selectedCard.GetGlobalRect()), $"complete focused card remains visible: area={_gameUi.Inspector.Construction.CardArea}, card={selectedCard.GetGlobalRect()}");
        var confirm = _gameUi.Inspector.Construction.ConfirmButton;
        var quote = _session.Snapshot.BuildingCatalog.Single(b => b.Type == Building.Lumbermill).Construction;
        Require(confirm.Text.Contains("Lumbermill") && confirm.Text.Contains(GameplayContext.Cost(quote)), "selection and real cost visible on construction confirmation");
        Require(_gameUi.ContextArea.Encloses(confirm.GetGlobalRect()), "construction confirmation fits the compact native dock");
        Rect2 selectedBounds = selectedCard.GetGlobalRect();
        RefreshUi(true);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Require(selectedCard.HasFocus() && selectedCard.GetGlobalRect() == selectedBounds, "selected card focus and position retained on snapshot refresh");
        Press(_gameUi.Inspector.Construction.ConfirmButton);
        Require(_session.City.Slots[0].Type == Building.Lumbermill, "native paid construction");
        await GameplayCapture("hud-occupied");
        // A paid recovery uses exactly the same explicit confirmation as ordinary cards.

        var button = _gameUi.Inspector.Actions["Sell"]; button.GrabFocus();
        RefreshUi(true); Require(button == _gameUi.Inspector.Actions["Sell"] && button.HasFocus(), "native control identity and keyboard focus survive snapshot refresh");
        Press(button); Require(_session.City.Slots[0].Type == Building.Empty, "native sale");
        await GameplayCapture("settlement-full-catalog"); _gameUi.Details.Close();

        ReplaceSession(ScenarioRunner.CreateAt("sprite-settlement")); _frozen = true; RefreshUi(true);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await GameplayCapture("hud-military");
        Require(_gameUi.ContextArea.Encloses(_gameUi.Inspector.ArmyButton.GetGlobalRect()), "compact Arcanum exposes the full Army management target");

        ReplaceSession(GameplayScenarios.CreateAt("economy")); _frozen = true; _session.SelectSlot(0); RefreshUi(true);
        await GameplayCapture("resources-and-upkeep");
        await GameplayCapture("building-upgrades"); _gameUi.Details.Close();
        int market = Array.FindIndex(_session.City.Slots, s => s.Type == Building.Market);
        _session.SelectSlot(market); RefreshUi(true);
        Require(_gameUi.Inspector.Market.Buttons.Count == 5, "every authoritative Market rate");
        var stock = _session.City.Resources;
        _gameUi.Inspector.Market.Bundles.Value = 2;
        Press(_gameUi.Inspector.Market.Buttons["TradeMetal"]);
        Require(_session.City.Metal == stock.Metal - 10 && _session.LastResult?.Accepted == true, "native bundle count and resource submission");
        await GameplayCapture("market-trading"); _gameUi.Details.Close();

        ReplaceSession(GameplayScenarios.CreateAt("army")); _frozen = true; RefreshUi(true);
        Require(Enum.GetValues<UnitType>().All(t => _session.City.Soldiers.Any(u => u.Type == t)), "all four legally recruited classes");
        OpenDetails(DetailsPage.Army);
        int member = _session.City.Soldiers.First(u => u.Type == UnitType.Swordsman).Id;
        Press(_gameUi.Details.Army.MemberButtons["Member" + member]);
        Require(_gameUi.Details.Inspector.SelectedUnit == member && _gameUi.Details.Inspector.Details.Contains("service XP"), "reusable persistent member inspector");
        await GameplayCapture("army-roster"); await GameplayCapture("squad-management"); await GameplayCapture("unit-inspector");
        OpenDetails(DetailsPage.Army);
        _gameUi.Details.Army.ShowHomes();
        Require(_gameUi.Details.Deployment.HomeButtons.Count == 12, "six numbered homes with inspect and deployment controls");
        var chosenSquad = _session.City.Soldiers.First(u => u.Assignment is { Stored: false });
        Press(_gameUi.Details.Deployment.HomeButtons["Deploy" + (chosenSquad.Assignment!.Tile + ArmyConfiguration.PlotOffset)]);
        Require(_session.LastResult?.Accepted == true, "native squad and arena-cell command");
        await GameplayCapture("army-deployment");
        _gameUi.Details.Close();

        ReplaceSession(GameplayScenarios.CreateAt("town-hall")); _frozen = true; RefreshUi(true); OpenDetails(DetailsPage.Army);
        var stored = _session.City.Soldiers.First(u => u.Assignment is { Stored: true });
        Press(_gameUi.Details.Army.MemberButtons["Member" + stored.Id]);
        Require(_gameUi.Details.Inspector.Details.Contains("paid recovery eligible"), "paid recovery shown from real roster");
        _gameUi.Details.Army.ShowManagement(); await GameplayCapture("town-hall");
        int health = stored.Health;
        Press(_gameUi.Details.Inspector.Buttons["Return"]);
        Require(_session.City.Soldiers.Single(u => u.Id == stored.Id).Assignment is { Stored: false }, "native veteran return");
        Press(_gameUi.Details.Inspector.Buttons["Store"]);
        Require(_session.City.Soldiers.Single(u => u.Id == stored.Id).Health == health, "native storage cannot heal independently");

        ReplaceSession(GameplayScenarios.CreateAt("research")); _frozen = true; RefreshUi(true); OpenDetails(DetailsPage.Worldtree);
        Require(_gameUi.Details.Research.TechnologyButtons.Count == _session.Snapshot.TechnologyCatalog.Length, "every canonical technology inspectable");
        Press(_gameUi.Details.Research.TechnologyButtons["TechRangedFoundation"]);
        Press(_gameUi.Details.Research.PurchaseButtons["Purchase"]);
        Require(_session.City.Research.Has(TechnologyId.RangedFoundation), "native technology parameter purchase");
        Press(_gameUi.Details.Research.TechnologyButtons["TechAssault"]);
        Require(_gameUi.Details.Research.PurchaseButtons["Purchase"].Disabled, "exclusive sibling locked by authority");
        await GameplayCapture("worldtree");
        _gameUi.Details.Close();
        var enemy = _session.Snapshot.NextWave[0]; _gameUi.Inspect(enemy, true);
        Require(_gameUi.Details.Inspector.Details.Contains("NEXT WAVE") && _gameUi.Details.Inspector.Buttons.Values.All(b => !b.Visible), "forecast inspector is read-only");
        await GameplayCapture("next-wave-preview");
        _gameUi.Details.Close();  await GameplayCapture("clean-settlement");

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
        Press(_gameUi.Inspector.Actions["BranchBombardTower"]);
        Require(_session.City.Slots[7].Type == Building.BombardTower, "native structure inspection and authoritative tower branch");
        await GameplayCapture("defense-tower");

        ReplaceSession(ScenarioRunner.CreateAt("initial")); _frozen = true;
        _session.Send("build", 9, Building.ArrowTower); RefreshUi(true);
        var homeTower = _session.Snapshot.Structures.Single(structure => structure.Slot == 9);
        _gameUi.Inspect(homeTower); Press(_gameUi.Details.Inspector.Buttons["ManageTower"]);
        Require(_gameUi.Details.Visible && _gameUi.Details.Deployment.SelectedPlot == 9, "army-home tower routes to contextual Homes, without a settlement plot override");
        Press(_gameUi.Details.Deployment.Inspector.Actions["Sell"]);
        Require(_session.LastResult?.Accepted == true && _session.City.Army!.Homes[0].Plot.Type == Building.Empty, "contextual army-home tower sale remains authoritative");
        _gameUi.Details.Close();

        ReplaceSession(ScenarioRunner.CreateAt("combat")); _frozen = true; RefreshUi(true);
        Require(!_gameUi.ReadyButton.Visible, "combat replaces readiness"); await GameplayCapture("hud-combat");
        ReplaceSession(GameplayScenarios.CreateAt("reward")); _frozen = true; RefreshUi(true);
        Require(_gameUi.Draft.RewardButtons.Count == 3, "actual three-card draft");
        long draftRevision = _session.Snapshot.Revision;
        _gameUi.Draft.Dismiss();
        Require(_session.Snapshot.Revision == draftRevision && _session.City.Run.Choice is null, "dismissing rewards does not choose or pause");
        _gameUi.Details.Close(); _gameUi.Draft.Open(); RefreshUi(true);
        await GameplayCapture("reward-draft");
        Press(_gameUi.Draft.RewardButtons[0]);
        Require(_session.City.Run.Cards.Length > 0 && _session.Snapshot.Phase == Phase.Building, "actual draft accepted");

        ReplaceSession(GameplayScenarios.CreateAt("victory")); _frozen = true; RefreshUi(true);
        Require(!_gameUi.Draft.Visible && _session.Snapshot.Wave == 20, "wave twenty terminal without new draft"); await GameplayCapture("victory");
        ReplaceSession(GameplayScenarios.CreateAt("defeat")); _frozen = true; RefreshUi(true);  await GameplayCapture("defeat");
        ReplaceSession(ScenarioRunner.CreateAt("settlement")); _frozen = true; _session.SelectSlot(4); RefreshUi(true);
        var recovery = _gameUi.Inspector.Construction.RecoveryButton!;
        await GameplayCapture("build-gold-recovery");
        Require(recovery.Visible && _gameUi.ContextArea.Encloses(recovery.GetGlobalRect()), "Gold recovery fits alongside every production option");
        long recoveryRevision = _session.Snapshot.Revision;
        Press(recovery); Require(_session.Snapshot.Revision == recoveryRevision, "recovery card selection spends nothing");
        await GameplayCapture("build-gold-recovery-selected");
        Require(_gameUi.ContextArea.Encloses(_gameUi.Inspector.Construction.ConfirmButton.GetGlobalRect()), "Gold recovery confirmation fits below the complete catalog");
        foreach (var card in _gameUi.Inspector.BuildButtons.Values.Append(recovery).Where(card => card.Visible))
            Require(_gameUi.ContextArea.Encloses(card.GetGlobalRect()), "every recovery/production option remains visible with confirmation");
        Press(_gameUi.Inspector.Construction.ConfirmButton);
        Require(_session.LastResult?.Accepted == true && _session.City.Slots[4].Type == Building.Lumbermill, "4 Gold recovery stays accessible and confirmed");
        await CheckGameplayInput();
        await CheckWorldtreePresentation();
        GD.Print($"GODOT_GAMEPLAY_PASS mode={_options.Mode} view={_options.View} hud={_options.Hud} catalog economy trades four-classes squads homes townhall research forecast inspector rewards victory defeat native-focus modal-scroll");
    }
    private void OpenDetails(DetailsPage page) => _gameUi.Details.Open(page, new GameplayContext(_session, ResumeAfterCommand));
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
            OpenDetails(DetailsPage.Worldtree); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            long revision = _session.Snapshot.Revision; float zoom = _camera.Magnification;
            int selection = _session.SelectedSlot;
            Click(new Vector2(10, 300));
            GetViewport().PushInput(new InputEventMouseButton { Position = new Vector2(300, 350), ButtonIndex = MouseButton.WheelUp, Pressed = true }, true);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(_camera.Magnification == zoom && _session.SelectedSlot == selection && _session.Snapshot.Revision == revision, "native modal blocks world input and never pauses authority");
            Require(GetViewport().GetVisibleRect().Encloses(_gameUi.Details.GetGlobalRect()), "details fits " + size);
            await GameplayCapture("worldtree-" + size.X);
            _settings.Open();
            CancelUi(); Require(!_settings.Visible && _gameUi.Details.Visible, "Esc closes nested settings before details");
            CancelUi(); Require(!_gameUi.Details.Visible, "Esc closes details");
            Require(GetViewport().GuiGetFocusOwner() is { } restored && restored.IsVisibleInTree(), "details restores a visible native focus target");
        }
    }
    private void CancelUi()
    {
        GetViewport().PushInput(new InputEventKey { Keycode = Key.Escape, PhysicalKeycode = Key.Escape, Pressed = true }, true);
        GetViewport().PushInput(new InputEventKey { Keycode = Key.Escape, PhysicalKeycode = Key.Escape, Pressed = false }, true);
    }
    private async Task GameplayCapture(string name)
    {
        RefreshUi(true); RefreshWorld();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (Argument("--gameplay-captures") is not { } directory) return;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        System.IO.Directory.CreateDirectory(directory);
        string path = System.IO.Path.Combine(directory, name + ".png");
        using var image = GetViewport().GetTexture().GetImage(); Require(image.SavePng(path) == Error.Ok, "gameplay capture " + name);
        GD.Print("GODOT_GAMEPLAY_CAPTURE " + path);
    }
}
