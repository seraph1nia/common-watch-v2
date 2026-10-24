using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class GameApplication
{
    private async Task CheckOpeningGuide()
    {
        ReplaceSession(ScenarioRunner.CreateAt("initial")); _frozen = true; RefreshUi(true);
        var guide = _gameUi.OpeningGuide;
        if (!guide.ExpandedPreference) Press(guide.ToggleButton);
        await GameplayCapture("opening-guide-production");
        Require(guide.Visible && guide.Expanded && guide.ContentFits && WorldArea.Encloses(guide.GetGlobalRect()), $"optional opening objective fits and is readable: visible={guide.Visible} expanded={guide.Expanded} text={guide.ContentFits} rect={guide.GetGlobalRect()} {guide.LayoutDetails}");
        long revision = _session.Snapshot.Revision;
        Click(guide.ToggleButton.GetGlobalRect().GetCenter()); await GameplayCapture("opening-guide-collapsed");
        Require(!guide.Expanded && revision == _session.Snapshot.Revision && !_gameUi.ReadyButton.Disabled, "collapse never submits or gates Ready");
        Press(_gameUi.ReadyButton); Require(_session.LastResult?.Accepted == true && _session.Snapshot.Turn == 2, "experienced player can proceed without instructions or a prescribed build");
        ReplaceSession(ScenarioRunner.CreateAt("initial")); _frozen = true; RefreshUi(true);
        Require(!guide.ExpandedPreference, "collapsed guide preference survives session replacement without changing settings");
        Press(guide.ToggleButton);
        DriveOpening(new(GameplayAction.Build, 0, Building.Farm)); DriveOpening(new(GameplayAction.Build, 1, Building.MetalMine)); DriveOpening(new(GameplayAction.Build, 2, Building.Barracks));
        _session.SelectSlot(-1); RefreshUi(true); await GameplayCapture("opening-guide-recovery");
        Require(guide.Text.Contains("Gold recovery") && guide.Text.Contains("4 Gold") && guide.ContentFits, "Wood shortage points to existing paid recovery");
        revision = _session.Snapshot.Revision;
        Click(guide.ActionButton.GetGlobalRect().GetCenter()); await GameplayCapture("opening-recovery-location");
        Require(_session.Snapshot.Revision == revision && _session.SelectedSlot == 3 && _gameUi.Inspector.Construction.RecoveryButton!.Visible, "guide navigation reveals recovery without building or paying");
        DriveOpening(new(GameplayAction.Build, 3, Building.Lumbermill, Payment: ConstructionPayment.GoldRecovery));
        DriveOpening(new(GameplayAction.Ready)); DriveOpening(new(GameplayAction.Recruit, 2)); DriveOpening(new(GameplayAction.Recruit, 2));
        DriveOpening(new(GameplayAction.Ready)); DriveOpening(new(GameplayAction.Recruit, 2)); DriveOpening(new(GameplayAction.Recruit, 2));
        _session.SelectSlot(-1); RefreshUi(true); await GameplayCapture("opening-guide-fund-position");
        Require(guide.Text.Contains("4 projected to fight") && guide.ContentFits, "contextual objective names projected fighting members");
        revision = _session.Snapshot.Revision;
        Click(guide.ActionButton.GetGlobalRect().GetCenter()); await GameplayCapture("opening-guide-homes");
        Require(_gameUi.Details.Visible && _gameUi.Details.Army.HomesVisible && _session.Snapshot.Revision == revision, "placement guidance opens real homes without a command");
        CancelUi(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); RefreshUi(true);
        Require(guide.Text.Contains("4/4") && guide.Text.Contains("Food"), "home review leads to the actual final Ready transition");
        DriveOpening(new(GameplayAction.Ready)); FinishOpeningBattle();
        CancelUi(); RefreshUi(true); await GameplayCapture("opening-guide-outcome");
        Require(!guide.IsVisibleInTree(), "completed opening guidance leaves the reward view");
        Click(_gameUi.FeedbackButton.GetGlobalRect().GetCenter()); await GameplayCapture("opening-guide-report");
        Require(_gameUi.Details.Visible && _gameUi.Details.LastOutcome?.Participants == 4, "optional outcome link opens factual preparation and battle details");
        GD.Print("GODOT_OPENING_GUIDE_PASS optional collapse retained-preference no-gate recovery navigation funding placement outcome native-pointer");
    }

    private async Task CheckOpeningCampaigns()
    {
        Require(_options.Mode == RenderMode.Sprites && _options.View == ViewPreset.Angled && _options.Hud == HudArt.Illustrated && !_options.DebugOverlay, "opening review uses ordinary visual defaults");
        await CheckOpeningGuide();
        var reports = new List<OpeningRun>();
        var catalog = RunCatalog.Doctrines.Select(d => new RunCardView(d.Id, d.Name, "", "", "", false, true)).ToArray();
        foreach (var doctrine in RunCatalog.Doctrines)
        {
            string match = "";
            for (int attempt = 0; attempt < 100; attempt++)
            {
                string candidate = $"opening-native-{doctrine.Id}-{attempt}";
                if (DoctrineOffers.For(candidate, 1, catalog).Contains(doctrine.Id)) { match = candidate; break; }
            }
            Require(match.Length > 0, "bounded legal offer contains reviewed doctrine");
            ReplaceSession(new SoloSession(SoloSession.DefaultSeed, match)); _frozen = true; RefreshUi(true);
            await GameplayCapture(doctrine.Id + "-doctrine-playstyle");
            foreach (var offer in _sessionMenu.Doctrines.GetChildren().OfType<DoctrineCard>())
                Require(offer.IntroductionFits && WorldArea.Encloses(offer.ChooseButton.GetGlobalRect()) && WorldArea.Encloses(offer.DetailsButton.GetGlobalRect()), "playstyle and immediate implication fit before scrolling, with both native actions visible");
            int index = _sessionMenu.Offers.ToList().IndexOf(doctrine.Id);
            var card = _sessionMenu.Doctrines.GetChildren().OfType<DoctrineCard>().ElementAt(index);
            Require(!card.DetailsVisible && card.Introduction == DoctrineIntroduction.For(doctrine.Id).Playstyle + "\n" + DoctrineIntroduction.For(doctrine.Id).Immediate, "doctrine leads with playstyle and immediate implication");
            Click(card.DetailsButton.GetGlobalRect().GetCenter()); await GameplayCapture(doctrine.Id + "-doctrine-full-effects");
            var canonical = RunPresentation.Project(_session.Snapshot, _session.City, _session.PlayerId, true, true).Cards.Single(c => c.Id == doctrine.Id);
            Require(card.DetailsVisible && card.FullDetails.Contains(canonical.Description.Replace("\n\n", "\n")) && card.FullDetails.Contains(canonical.Footer)
                && _sessionMenu.StartButton.Disabled, "full numeric effects remain inspectable without selecting a doctrine");
            Click(card.DetailsButton.GetGlobalRect().GetCenter()); Press(card.ChooseButton); Press(_sessionMenu.StartButton);
            var starting = _session.City.Resources;
            var waves = new List<OpeningWave>(); var actions = new List<string>();
            for (int wave = 1; wave <= 3; wave++)
            {
                if (_session.Snapshot.Phase == Phase.RewardDraft)
                {
                    string reward = GameplayScenarios.RewardChoice(_session.City.Run);
                    int choice = Array.IndexOf(_session.City.Run.Offers, reward);
                    Press(_gameUi.Draft.RewardButtons[choice]);
                }
                int decisions = 0;
                for (int stage = 1; stage <= _session.Snapshot.Rules.PreparationStages; stage++)
                {
                    for (int n = 0; n < 100 && OpeningCampaign.Next(_session.Snapshot, _session.PlayerId) is { } intent; n++)
                    {
                        DriveOpening(intent); decisions++; actions.Add($"W{wave}/P{stage}: {intent}");
                        if (n == 99) throw new InvalidOperationException("Native opening decision bound exceeded.");
                    }
                    _gameUi.Details.Close(false); _session.SelectSlot(-1); RefreshUi(true);
                    string next = OpeningGuidance.NextReady(_session.Snapshot);
                    Require(next.Contains(stage < _session.Snapshot.Rules.PreparationStages ? (PreparationSchedule.ResourceMultiplier(_session.Snapshot.Rules, stage) == 2 ? "double resources" : "one production") : "Food") && _gameUi.TransitionText.Contains(stage < _session.Snapshot.Rules.PreparationStages ? $"prep {stage + 1}/{_session.Snapshot.Rules.PreparationStages}" : "battle"), "native next transition and contextual explanation agree");
                    int[] expected = _session.City.FoodForecast!.Participating;
                    if (stage == _session.Snapshot.Rules.PreparationStages) await GameplayCapture($"{doctrine.Id}-wave-{wave}-funded-preparation");
                    DriveOpening(new(GameplayAction.Ready));
                    if (stage == _session.Snapshot.Rules.PreparationStages) Require(expected.SequenceEqual(_session.City.LastUpkeep!.Participating), "displayed participating roster matches the accepted final Ready receipt");
                }
                int before = _session.City.Health; var paid = _session.City.LastUpkeep!;
                var types = _session.City.Soldiers.Where(u => paid.Participating.Contains(u.Id)).GroupBy(u => u.Type).ToDictionary(g => g.Key.ToString(), g => g.Count());
                FinishOpeningBattle(); await GameplayCapture($"{doctrine.Id}-wave-{wave}-outcome");
                Require(_session.Snapshot.Phase == Phase.RewardDraft && _gameUi.Draft.Visible, "native intuitive opening reaches the next actual reward without rule changes");
                waves.Add(new(wave, _session.Snapshot.Phase.ToString(), decisions, types, paid.Paid, paid.Unfed.Length,
                    paid.Participating.Length - _session.City.Soldiers.Count(u => paid.Participating.Contains(u.Id)), before - _session.City.Health, _session.City.Resources));
            }
            reports.Add(new(doctrine.Id, SoloSession.DefaultSeed, match, starting, waves.ToArray(), actions.ToArray()));
            GD.Print($"GODOT_OPENING_DOCTRINE_PASS {doctrine.Id} first-three-waves native-production ready forecast outcomes");
        }
        if (Argument("--gameplay-captures") is { } directory)
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "opening-review.json"), System.Text.Json.JsonSerializer.Serialize(reports, WireJson.Options));
        GD.Print("GODOT_OPENING_REVIEW_PASS doctrines=6 waves=18 ordinary-controls current-defaults no-rules-adjustment");
    }

    private void DriveOpening(GameplayIntent intent)
    {
        _gameUi.Details.Close(false); _gameUi.Draft.Dismiss(false);
        if (intent.Slot >= 0) _session.SelectSlot(intent.Slot);
        RefreshUi(true);
        switch (intent.Action)
        {
            case GameplayAction.Build:
                var definition = _session.Snapshot.BuildingCatalog.Single(b => b.Type == intent.Building);
                Press(_gameUi.Inspector.Construction.Categories[ConstructionPanel.CategoryOf(definition)]);
                Press(intent.Payment == ConstructionPayment.GoldRecovery ? _gameUi.Inspector.Construction.RecoveryButton! : _gameUi.Inspector.BuildButtons[intent.Building]);
                Press(_gameUi.Inspector.Construction.ConfirmButton); break;
            case GameplayAction.BuyPlot: Press(_gameUi.Inspector.Actions["Buy"]); break;
            case GameplayAction.Upgrade: Press(_gameUi.Inspector.Actions["Upgrade"]); break;
            case GameplayAction.Sell: Press(_gameUi.Inspector.Actions["Sell"]); break;
            case GameplayAction.Recruit: Press(_gameUi.Inspector.Recruitment.Buttons["Recruit" + intent.SoldierType]); break;
            case GameplayAction.BuyHome:
                OpenDetails(DetailsPage.Army); _gameUi.Details.Army.ShowHomes();
                var home = _gameUi.Details.Deployment.Homes.First(h => !h.Home.Purchased);
                Press(_gameUi.Details.Deployment.HomeButtons["Home" + home.Home.PlotId]); Press(_gameUi.Details.Army.HomeInspector.Actions["Buy"]); break;
            case GameplayAction.Trade:
                _gameUi.Inspector.Market.Bundles.Value = intent.Bundles; Press(_gameUi.Inspector.Market.Buttons["Trade" + intent.Resource]); break;
            case GameplayAction.Retire:
                _gameUi.Inspect(_session.City.Soldiers.Single(u => u.Id == intent.UnitId)); Press(_gameUi.Details.Inspector.Buttons["Retire"]); Press(_gameUi.Details.Inspector.Buttons["ConfirmRetire"]); break;
            case GameplayAction.Ready: Press(_gameUi.ReadyButton); break;
            default: throw new InvalidOperationException("Unmapped ordinary opening control: " + intent.Action);
        }
        Require(_session.LastResult?.Accepted == true, "ordinary production control accepted: " + intent);
    }
    private void FinishOpeningBattle()
    {
        for (int step = 0; step < 18000 && (_session.Snapshot.Phase == Phase.Combat || _session.Snapshot.DyingBodies.Length > 0); step++) _session.Step();
        for (int frame = 0; frame < 60; frame++) { _session.Playback.Advance(.1, true); RefreshWorld(); if (!_battlefield.Finishing) break; }
        _frozen = true; RefreshUi(true);
    }
}
