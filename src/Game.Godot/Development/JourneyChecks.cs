// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class GameApplication
{
    private bool _nativeJourneyBusy;
    private string _nativeJourneyError = "";
    private int _nativeJourneyActions;
    private async Task JourneyFrames()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    private async Task JourneyClick(Button button)
    {
        Require(button.IsVisibleInTree() && !button.Disabled, "journey action is visible and enabled: " + button.Text);
        button.GrabFocus();
        for (Node? parent = button.GetParent(); parent is not null; parent = parent.GetParent())
            if (parent is ScrollContainer scroll) scroll.EnsureControlVisible(button);
        await JourneyFrames();
        Require(GetViewport().GetVisibleRect().HasPoint(button.GetGlobalRect().GetCenter()), "journey pointer target fits: " + button.Text);
        for (Node? parent = button.GetParent(); parent is not null; parent = parent.GetParent())
        {
            if (parent is not ScrollContainer scroll) continue;
            for (int attempt = 0; attempt < 30; attempt++)
            {
                var rect = button.GetGlobalRect(); var clip = scroll.GetGlobalRect();
                bool above = rect.Size.Y <= clip.Size.Y ? rect.Position.Y < clip.Position.Y : rect.GetCenter().Y < clip.Position.Y;
                bool below = rect.Size.Y <= clip.Size.Y ? rect.End.Y > clip.End.Y : rect.GetCenter().Y > clip.End.Y;
                if (!above && !below) break;
                var point = clip.GetCenter(); WorldtreePointer(point);
                var wheel = above ? MouseButton.WheelUp : MouseButton.WheelDown;
                GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = wheel, Pressed = true }, true);
                GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = wheel, Pressed = false }, true);
                _nativeJourneyActions++; await JourneyFrames();
                if (attempt == 29) throw new InvalidOperationException("Native wheel could not reveal journey action: " + button.Text);
            }
        }
        GD.Print($"JOURNEY_CLICK {button.Text.Split('\n')[0]} rect={button.GetGlobalRect()} parent={button.GetParent().Name}");
        WorldtreeClick(button.GetGlobalRect().GetCenter()); _frozen = _args.Contains("--journey-test");
        _nativeJourneyActions++; RefreshUi(true); await JourneyFrames();
    }
    private async Task JourneyWait(Func<bool> predicate, string reason)
    {
        for (int frame = 0; frame < 600; frame++)
        {
            if (predicate()) return;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        throw new InvalidOperationException("Native journey wait exceeded its frame bound: " + reason);
    }
    private async Task DriveNativeJourney(GameplayIntent intent)
    {
        var owner = _session;
        GD.Print("JOURNEY_INTENT " + intent);
        _gameUi.Details.Close(); _gameUi.Draft.Dismiss(); RefreshUi(true); await JourneyFrames();
        if (intent.Slot >= 0)
        {
            if (_session.SelectedSlot >= 0) await JourneyClick(_gameUi.ClearSelectionButton);
            WorldtreeClick(_camera.ToScreen(_settlementView.PlotPosition(intent.Slot)));
            RefreshUi(true); await JourneyFrames();
            Require(_session.SelectedSlot == intent.Slot, "journey uses actual clearing picking");
        }
        var before = _session.City;
        long sequence = _session.LastResult?.Sequence ?? 0;
        void Current() { Require(ReferenceEquals(owner, _session), "journey owner is still current"); }
        Current();
        switch (intent.Action)
        {
            case GameplayAction.Build:
                var definition = _session.Snapshot.BuildingCatalog.Single(b => b.Type == intent.Building);
                await JourneyClick(_gameUi.Inspector.Construction.Categories[ConstructionPanel.CategoryOf(definition)]); Current();
                await JourneyClick(intent.Payment == ConstructionPayment.GoldRecovery ? _gameUi.Inspector.Construction.RecoveryButton! : _gameUi.Inspector.BuildButtons[intent.Building]); Current();
                await JourneyClick(_gameUi.Inspector.Construction.ConfirmButton); break;
            case GameplayAction.Recruit: await JourneyClick(_gameUi.Inspector.Recruitment.Buttons["Recruit" + intent.SoldierType]); break;
            case GameplayAction.Upgrade: await JourneyClick(_gameUi.Inspector.Actions["Upgrade"]); break;
            case GameplayAction.Sell: await JourneyClick(_gameUi.Inspector.Actions["Sell"]); break;
            case GameplayAction.BuyPlot: await JourneyClick(_gameUi.Inspector.Actions["Buy"]); break;
            case GameplayAction.Trade:
                _gameUi.Inspector.Market.Bundles.Value = intent.Bundles;
                await JourneyClick(_gameUi.Inspector.Market.Buttons["Trade" + intent.Resource]); break;
            case GameplayAction.BuyHome:
                await JourneyClick(_gameUi.ArmyNavigationButton); Current();
                var home = _gameUi.Details.Deployment.Homes.First(h => !h.Home.Purchased);
                await JourneyClick(_gameUi.Details.Deployment.HomeButtons["Home" + home.Home.PlotId]); Current();
                await JourneyClick(_gameUi.Details.Army.HomeInspector.Actions["Buy"]); break;
            case GameplayAction.Retire:
                await JourneyClick(_gameUi.ArmyNavigationButton); Current();
                _gameUi.Details.Army.ShowRoster(); await JourneyFrames();
                await JourneyClick(_gameUi.Details.Army.MemberButtons["Member" + intent.UnitId]); Current();
                await JourneyClick(_gameUi.Details.Inspector.Buttons["Retire"]); Current();
                await JourneyClick(_gameUi.Details.Inspector.Buttons["ConfirmRetire"]); break;
            default: throw new InvalidOperationException("Unmapped native opening action: " + intent.Action);
        }
        await JourneyWait(() => !ReferenceEquals(owner, _session) || _session.LastResult is { } result && result.Sequence > sequence && !_session.Pending, $"command acknowledgment: {intent}; last={_session.LastResult}; sequence before={sequence}"); Current();
        Require(_session.LastResult!.Accepted, "native journey command accepted: " + intent);
        await JourneyWait(() => !ReferenceEquals(owner, _session) || (intent.Action switch
        {
            GameplayAction.Build => _session.City.Slots[intent.Slot].Type == intent.Building,
            GameplayAction.Recruit => _session.City.Soldiers.Length > before.Soldiers.Length,
            GameplayAction.Upgrade => _session.City.Slots[intent.Slot].Level > before.Slots[intent.Slot].Level,
            GameplayAction.BuyHome => _session.City.Army!.PurchasedHomes > before.Army!.PurchasedHomes,
            GameplayAction.BuyPlot => _session.City.Slots[intent.Slot].Purchased,
            GameplayAction.Sell => _session.City.Slots[intent.Slot].Type == Building.Empty,
            GameplayAction.Trade => _session.City.Resources.Amount(intent.Resource) < before.Resources.Amount(intent.Resource),
            GameplayAction.Retire => _session.City.Soldiers.All(u => u.Id != intent.UnitId),
            _ => false
        }), "accepted state effect"); Current();
        _gameUi.Details.Close(); RefreshUi(true);
    }
    private async void NativeRequest(Func<Task> action)
    {
        if (_nativeJourneyBusy) return;
        _nativeJourneyBusy = true; _nativeJourneyError = "";
        try { await action(); }
        catch (Exception error) { _nativeJourneyError = error.ToString(); GD.PrintErr(error); }
        finally { _nativeJourneyBusy = false; }
    }
    private void NativeOpeningStep() => NativeRequest(PrepareNativeOpening);
    private async Task PrepareNativeOpening()
    {
        var owner = _session;
        for (int n = 0; n < 100; n++)
            {
                if (!ReferenceEquals(owner, _session)) throw new OperationCanceledException("Session changed");
                if (OpeningCampaign.Next(owner.Snapshot, owner.PlayerId, ReviewOpeningOrder) is not { } intent) return;
                await DriveNativeJourney(intent);
        }
        throw new InvalidOperationException("Native opening decision bound exceeded");
    }
    private async Task OpenJourneyOverlay(string overlay)
    {
        if (overlay == "army") await JourneyClick(_gameUi.ArmyNavigationButton);
        else if (overlay == "worldtree") await JourneyClick(_gameUi.WorldtreeNavigationButton);
        else if (overlay == "menu") await JourneyClick(_gameUi.MenuButton);
        else if (overlay == "settings")
        { await JourneyClick(_gameUi.MenuButton); await JourneyClick(_menuPopup.SettingsButton); }
    }
    private async Task CheckJourneyTransitions()
    {
        foreach (string overlay in new[] { "army", "worldtree", "retirement", "menu", "settings", "exit" })
        {
            ReplaceSession(ScenarioRunner.CreateAt("post-draft")); _frozen = true;
            for (int tick = 0; tick < 180 && _session.Snapshot.DyingBodies.Length > 0; tick++) _session.Step();
            RefreshUi(true);
            for (int stage = 0; stage < 2; stage++) { _session.Send("ready"); Require(_session.LastResult!.Accepted, "legal overlay boundary setup"); }
            _session.SelectSlot(1); RefreshUi(true); await JourneyFrames();
            if (overlay is "army" or "retirement")
            {
                await JourneyClick(_gameUi.ArmyNavigationButton);
                if (overlay == "retirement")
                {
                    _gameUi.Details.Army.ShowRoster(); await JourneyFrames();
                    await JourneyClick(_gameUi.Details.Army.MemberButtons.Values.First(b => b.Visible));
                    await JourneyClick(_gameUi.Details.Inspector.Buttons["Retire"]);
                }
            }
            else if (overlay == "worldtree") await JourneyClick(_gameUi.WorldtreeNavigationButton);
            else
            {
                await JourneyClick(_gameUi.MenuButton);
                if (overlay == "settings") await JourneyClick(_menuPopup.SettingsButton);
                if (overlay == "exit") await JourneyClick(_menuPopup.ReturnToMenuButton!);
            }
            // Legal authority progression represents another participant completing readiness.
            // Opening/closing the actual native overlays above submitted no implicit command.
            var owner = _session; _session.Send("ready"); _frozen = true; RefreshUi(true); RefreshWorld(); await JourneyFrames();
            Require(_session.Snapshot.Phase == Phase.Combat && !_session.Snapshot.Paused, $"overlay does not pause or change authority: {overlay} {_session.Snapshot.Phase}/{_session.Snapshot.Turn} {_session.LastResult}");
            Require(!_gameUi.Details.Visible, "ordinary management suspends on combat entry: " + overlay);
            long revision = _session.Snapshot.Revision;
            await GameplayCapture("journey-entry-" + overlay);
            if (overlay == "settings")
            {
                Require(_settings.Visible && _settings.BattleButton.IsVisibleInTree(), "settings remains adjustable with a return route");
                Require(GetViewport().GetVisibleRect().Encloses(_settings.Panel.GetGlobalRect()), "live settings fits viewport");
                await JourneyClick(_settings.BattleButton); Require(!_settings.Visible, "native return closes settings safely");
            }
            else if (overlay == "exit")
            {
                Require(_exitConfirmation.Visible && _exitConfirmation.BattleButton.Visible, "exit consent is preserved, with safe cancel/return");
                _exitConfirmation.BattleButton.GrabFocus();
                _exitConfirmation.PushInput(new InputEventJoypadButton { ButtonIndex = JoyButton.A, Pressed = true }, true);
                _exitConfirmation.PushInput(new InputEventJoypadButton { ButtonIndex = JoyButton.A, Pressed = false }, true);
                Require(!_exitConfirmation.Visible && !_menuPopup.Visible, "controller return cancels exit; never confirms it");
            }
            else if (overlay == "menu")
            {
                Require(_menuPopup.Visible && _menuPopup.ContinueButton!.Text.Contains("battle"), "menu stays explicit about live combat");
                CancelUi();
            }
            Require(ReferenceEquals(owner, _session) && revision == _session.Snapshot.Revision, "return route submits no simulation command");
            RefreshUi(true);
            if (overlay == "retirement")
            {
                int count = _session.City.Soldiers.Length;
                _gameUi.Details.Inspector.Buttons["ConfirmRetire"].EmitSignal(BaseButton.SignalName.Pressed);
                Require(_session.City.Soldiers.Length == count && revision == _session.Snapshot.Revision, "stale retirement cannot survive combat entry");
            }
        }
        ReplaceSession(ScenarioRunner.CreateAt("draft")); _frozen = true; FinishOpeningBattle();
        var oldChoice = _gameUi.Draft.RewardButtons[0];
        _session.Send("pause"); _frozen = true; RefreshUi(true);
        long pausedSequence = _session.LastResult!.Sequence;
        oldChoice.EmitSignal(BaseButton.SignalName.Pressed);
        Require(_session.LastResult.Sequence == pausedSequence && _session.City.Run.Choice is null, "disabled paused choice never submits a command");
        ReplaceSession(ScenarioRunner.CreateAt("draft")); _frozen = true; FinishOpeningBattle();
        long newSequence = _session.LastResult?.Sequence ?? 0;
        if (IsInstanceValid(oldChoice)) oldChoice.EmitSignal(BaseButton.SignalName.Pressed);
        Require((_session.LastResult?.Sequence ?? 0) == newSequence && _session.City.Run.Choice is null, "old-session reward control cannot choose a current offer");
        ReplaceSession(ScenarioRunner.CreateAt("combat")); _frozen = true; RefreshUi(true);
        await JourneyClick(_gameUi.MenuButton); await JourneyClick(_menuPopup.ContinueButton!);
        FinishOpeningBattle();
        GetViewport().PushInput(new InputEventKey { Keycode = Key.F10, PhysicalKeycode = Key.F10, Pressed = true }, true);
        GetViewport().PushInput(new InputEventKey { Keycode = Key.F10, PhysicalKeycode = Key.F10, Pressed = false }, true);
        await JourneyFrames(); await JourneyClick(_menuPopup.SettingsButton);
        Require(GetViewport().GuiGetFocusOwner() == _settings.Slider, "settings owns focus above the existing chooser");
        // Legal acceptance represents an in-flight choice completing while the
        // player adjusts settings. It must not take accessibility focus back.
        _session.ChooseReward(_session.City.Run.Offers[0]); _frozen = true; RefreshUi(true);
        Require(_session.LastResult!.Accepted && _session.Snapshot.Phase == Phase.Building && _gameUi.Journey.ReceiptCount == 1, "legal acceptance advances and records one receipt");
        Require(_settings.Visible && GetViewport().GuiGetFocusOwner() == _settings.Slider, "accepted reward cannot steal focus from settings");
        _settings.Close();
        GD.Print("GODOT_JOURNEY_OVERLAYS_PASS army worldtree retirement menu settings exit native-cancel no-implicit-command");
    }
    private async Task CheckNativeSoloJourney()
    {
        ReturnToMenu(); _frozen = true; await JourneyFrames();
        await JourneyClick(_sessionMenu.SoloButton);
        await JourneyClick(_sessionMenu.Doctrines.GetChildren().OfType<DoctrineCard>().First().ChooseButton);
        await JourneyClick(_sessionMenu.StartButton);
        var history = new List<object>();
        var timing = new List<object>();
        for (int wave = 1; wave <= 3; wave++)
        {
            for (int stage = 1; stage <= _session.Snapshot.Rules.PreparationStages; stage++)
            {
                ulong stageStart = Time.GetTicksMsec();
                for (int n = 0; n < 100 && OpeningCampaign.Next(_session.Snapshot, _session.PlayerId, ReviewOpeningOrder) is { } intent; n++)
                { await DriveNativeJourney(intent); if (n == 99) throw new InvalidOperationException("Opening bound exceeded"); }
                ulong captureStart = Time.GetTicksMsec();
                await GameplayCapture($"journey-solo-w{wave}-prep-{stage}");
                ulong captureEnd = Time.GetTicksMsec();
                if (stage == 1)
                {
                    int payments = _session.Snapshot.ProductionCount;
                    _session.Send("pause"); RefreshUi(true); await JourneyFrames();
                    GetViewport().PushInput(new InputEventKey { Keycode = Key.Y, Pressed = true }, true);
                    Require(_session.Snapshot.ProductionCount == payments && !_session.City.Ready, "paused Y cannot Ready");
                    _session.Send("resume"); RefreshUi(true); await JourneyFrames();
                    GetViewport().PushInput(new InputEventKey { Keycode = Key.Y, Pressed = true }, true);
                    GetViewport().PushInput(new InputEventKey { Keycode = Key.Y, Pressed = true, Echo = true }, true);
                    GetViewport().PushInput(new InputEventKey { Keycode = Key.Y, Pressed = false }, true);
                    await JourneyFrames();
                    Require(_session.Snapshot.ProductionCount == payments + 1, "native Y commits exactly one payment; key repeat cannot advance again");
                }
                else await JourneyClick(_gameUi.ReadyButton);
                timing.Add(new { wave, stage, scriptedDecisionSeconds = (captureStart - stageStart) / 1000.0,
                    captureSeconds = (captureEnd - captureStart) / 1000.0,
                    boundaryProbeSeconds = (Time.GetTicksMsec() - captureEnd) / 1000.0,
                    alreadyReadyWaitSeconds = 0, disconnectSeconds = 0,
                    limitation = "Synchronous solo authority; captures and pause/key-repeat probe separated. Accelerated native script, not human time." });
            }
            Require(_session.Snapshot.Phase == Phase.Combat, "continuous journey enters actual combat");
            await GameplayCapture($"journey-solo-w{wave}-combat");
            for (int n = 0; n < 18000 && (_session.Snapshot.Phase == Phase.Combat || _session.Snapshot.DyingBodies.Length > 0); n++)
            {
                _session.Step();
                if (n % 60 == 0) { RefreshWorld(); RefreshUi(true); await JourneyFrames(); }
            }
            for (int frame = 0; frame < 120 && (_battlefield.Finishing || !_gameUi.Draft.Visible); frame++)
            { _session.Playback.Advance(.1, true); RefreshWorld(); RefreshUi(true); await JourneyFrames(); }
            Require(_session.Snapshot.Phase == Phase.RewardDraft && _gameUi.Draft.Visible, "playback finishes into the actual chooser");
            await GameplayCapture($"journey-solo-w{wave}-reward");
            var result = _gameUi.Result.Summary;
            int index = Array.IndexOf(_session.City.Run.Offers, GameplayScenarios.RewardChoice(_session.City.Run));
            await JourneyClick(_gameUi.Draft.RewardButtons[index]);
            Require(_session.Snapshot.Phase == Phase.Building && !_gameUi.Draft.Visible && !_gameUi.Details.Visible, "solo advances without mandatory dismissal or modal restoration");
            Require(_gameUi.Journey.ReceiptCount == wave && _gameUi.Journey.Receipt?.Wave == wave, "accepted receipt appears exactly once");
            Require(_gameUi.Result.Summary == result, "accepted reward does not rewrite the frozen battle result");
            await GameplayCapture($"journey-solo-w{wave}-accepted");
            history.Add(new { wave, receipt = _gameUi.Journey.Receipt, stock = _session.City.Resources, actions = _nativeJourneyActions, tick = _session.Snapshot.Tick, speed = _session.Snapshot.CombatSpeed });
        }
        if (Argument("--gameplay-captures") is { } directory)
        {
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "journey-solo.json"), System.Text.Json.JsonSerializer.Serialize(history, WireJson.Options));
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "solo-stage-timing.json"), System.Text.Json.JsonSerializer.Serialize(timing, WireJson.Options));
        }
        GD.Print("GODOT_JOURNEY_SOLO_PASS waves=3 native-doctrine purchases recruitment readiness rewards no-extra-continue scripted-combat-not-human-pacing");
    }
}
