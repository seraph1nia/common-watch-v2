using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class GameApplication
{
    private async Task CheckCombatReadability()
    {
        ReplaceSession(ScenarioRunner.CreateAt("settlement")); _frozen = true; RefreshUi(true);
        for (int stage = 0; stage < 2; stage++)
        {
            _session.Send("ready"); Require(_session.LastResult?.Accepted == true, "legal combat readability production");
            _session.Send("recruit", 1); Require(_session.LastResult?.Accepted == true, "legal readability recruit");
            _session.Send("recruit", 1); Require(_session.LastResult?.Accepted == true, "legal readability recruit");
        }
        _session.Send("ready"); _frozen = true;
        for (int step = 0; step < 600 && _session.Snapshot.Phase == Phase.Combat
            && !_session.Playback.Units().Any(u => u.Faction == Faction.Adventurers && u.Health > 0 && u.TargetId > 0); step++) _session.Step();
        RefreshWorld(); RefreshUi(true);
        var units = _session.Playback.Units();
        var unit = units.First(u => u.Faction == Faction.Adventurers && u.Health > 0 && u.TargetId > 0);
        var layout = new ArenaLayout(_session.Snapshot.Arena!, new HexProjection(30, _options.GroundCompression));
        var p = CombatVisuals.Position(unit, _session.Snapshot, _battlefield.AnimationTick);
        Vector2 point = _camera.ToScreen(new(p.X - layout.Center.X, p.Y * _options.GroundCompression - layout.Center.Y));
        long revision = _session.Snapshot.Revision;
        Click(point); await GameplayCapture("combat-live-inspection");
        var live = _gameUi.LiveInspector;
        Require(live.Visible && live.SelectedUnit == unit.Id && !_gameUi.Details.Visible && !_gameUi.BlocksWorld, "native combat click opens nonmodal live inspector");
        Require(WorldArea.Encloses(live.GetGlobalRect()) && live.Size.X < WorldArea.Size.X * .3f && !live.GetGlobalRect().HasPoint(point), "compact inspection leaves the selected actor and central fight visible");
        Require(!live.Inspector.Details.Contains("ticks") && !live.Inspector.Details.Contains("Committed") && !live.Inspector.Details.Contains("#" + unit.Id), "basic combat inspection has roles and effects without entity IDs/ticks");
        Require(live.Inspector.Details.Contains("Target:") && live.Inspector.Details.Contains("Effects:") && !live.Inspector.AdvancedVisible, "basic live inspection exposes target and effects; advanced is opt-in");
        Require(live.Inspector.Buttons.Values.All(b => !b.Visible) && _battlefield.HighlightedMembers > 0 && _battlefield.HighlightedTarget != 0, "selected squad and actual target highlighted without edit controls");
        Require(_battlefield.SquadCueCount > 0 && _battlefield.SquadCuesFit && revision == _session.Snapshot.Revision, "squad cues fit and inspection never submits game commands");
        Press(live.Inspector.AdvancedButton); await GameplayCapture("combat-advanced-inspection");
        Require(live.Inspector.AdvancedVisible && live.Inspector.AdvancedDetails.Contains("#" + unit.Id), "advanced identity and timing remain inspectable");
        live.Inspector.AdvancedButton.GrabFocus();
        GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = JoyButton.A, Pressed = true }, true);
        GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = JoyButton.A, Pressed = false }, true);
        Require(!live.Inspector.AdvancedVisible, "native controller accept toggles advanced details");
        double tick = _battlefield.AnimationTick;
        for (int step = 0; step < 12; step++) _session.Step();
        _frozen = true; await GameplayCapture("combat-next-action");
        Require(_battlefield.AnimationTick > tick && !live.GetGlobalRect().HasPoint(point), "next authoritative action remains visible while inspected");
        CancelUi(); Require(!live.Visible && !_menuPopup.Visible, "Escape closes live inspection before opening menu");
        for (int step = 0; step < 18000 && (_session.Snapshot.Phase == Phase.Combat || _session.Snapshot.DyingBodies.Length > 0); step++) _session.Step();
        // Frozen probes still need to consume the final delayed presentation frames.
        for (int frame = 0; frame < 60; frame++)
        {
            _session.Playback.Advance(.1, connected: true); RefreshWorld();
            if (!_battlefield.Finishing) break;
        }
        _frozen = true; MoveMouse(new(10, 180)); await GameplayCapture("battle-result-draft");
        Require(_gameUi.Draft.Visible && _gameUi.Draft.Result.Summary.Contains("survived") && _gameUi.Draft.Result.Summary.Contains("Keep −"), "actual clear opens factual recap and existing draft together");
        foreach (var choice in _gameUi.Draft.RewardButtons)
            Require(!choice.Disabled && WorldArea.Encloses(choice.GetGlobalRect()), "all three native reward choices fit with collapsed recap");
        Press(_gameUi.Draft.Result.ExpandButton); await GameplayCapture("battle-result-expanded");
        Require(_gameUi.Draft.Result.Details.Contains($"{_session.City.LastUpkeep!.Paid} Food paid"), "expanded recap uses authoritative upkeep receipt");
        Press(_gameUi.Draft.RewardButtons[0]);
        Require(_session.Snapshot.Phase == Phase.Building, "result strip adds no mandatory dismissal before reward choice");
        GD.Print("GODOT_COMBAT_READABILITY_PASS nonmodal roles target effects advanced native-pointer controller escape recap draft unchanged-authority");
    }
}
