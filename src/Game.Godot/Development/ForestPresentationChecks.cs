// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class GameApplication
{
    private async Task CheckForestPresentation()
    {
        ReplaceSession(ScenarioRunner.CreateAt("settlement")); _frozen = true; _session.SelectSlot(-1);
        await GameplayCapture("forest-levels-1"); CheckLevelCues();
        Require(!_gameUi.ReconnectButton.Visible && _gameUi.FeedbackButton.Visible, "connected utilities are quiet; feedback remains accessible");
        if (_skin.Illustrated)
            Require(_gameUi.FeedbackButton.ThemeTypeVariation == "ForestQuietButton" && _gameUi.ReadyButton.ThemeTypeVariation == "ForestPrimaryButton", "primary Ready hierarchy retains native secondary material");
        _session.Send("ready"); Require(_session.LastResult is { Accepted: true }, "first actual production");
        _session.Send("ready"); Require(_session.LastResult is { Accepted: true }, "second actual production");
        _session.SelectSlot(0); RefreshUi(true); await GameplayCapture("forest-upgrade-available");
        var before = _session.City.Resources; var quote = _session.City.Slots[0].UpgradeQuote!.Value;
        WorldtreeClick(_gameUi.Inspector.Actions["Upgrade"].GetGlobalRect().GetCenter()); _frozen = true;
        Require(_session.City.Slots[0].Level == 2 && before.TryPay(quote, out var paid) && _session.City.Resources == paid, "native upgrade consumes the quoted price once");
        _session.SelectSlot(-1); await GameplayCapture("forest-levels-2"); CheckLevelCues();
        Require(_settlementView.LevelCues[0].Text == "L2", "same Farm sprite gains factual level-two annotation");
        ReplaceSession(GameplayScenarios.CreateAt("late")); _frozen = true; _session.SelectSlot(-1);
        await GameplayCapture("forest-levels-late"); CheckLevelCues();
        Require(_settlementView.LevelCues.Values.Any(c => c.Text == "L5"), "earned tier-five progress visible without selecting a building");

        ReplaceSession(ScenarioRunner.CreateAt("post-draft")); _frozen = true;
        var enemy = _session.Snapshot.NextWave.Single(u => u.Type == UnitType.Berserker);
        Require(VisualAssets.UnitId(enemy) == "unit/enemy_berserker", "enemy Berserker retains its explicit catalog identity");
        bool sprites = _options.Mode == RenderMode.Sprites && !_args.Contains("--expect-forest-art-missing");
        Require((_assets.Load(VisualAssets.UnitId(enemy)) is not null) == sprites, "Berserker native decode or explicit debug fallback");
        Require((_assets.Load(VisualAssets.StructureId(ArenaStructureKind.Watchpost)) is not null) == sprites, "Watchpost uses supplied defense tower or explicit debug fallback");
        var revision = _session.Snapshot.Revision;
        _gameUi.OpenArena(); await GameplayCapture("forest-wave-2-preview");
        Require(_session.Snapshot.Revision == revision, "asset presentation does not mutate authority");
        for (int n = 0; n < 3; n++) { _session.Send("ready"); Require(_session.LastResult is { Accepted: true }, "legal wave-two readiness"); }
        for (int n = 0; n < 100; n++) _session.Step();
        await GameplayCapture("forest-berserker-watchpost-live");
        var presented = _session.Playback.Units().Single(u => u.Type == UnitType.Berserker && u.Faction == Faction.Skeletons);
        Require(_battlefield.FigureHasArtwork(presented.Id) == sprites, "actual retained Berserker figure uses approved texture in sprites and token in debug");
        Require(VisualAssets.StructureId(ArenaStructureKind.Watchpost) == "arena/defense_tower", "shared structure mapping reaches preview and live drawing");
        GD.Print("GODOT_FOREST_PRESENTATION_PASS native-upgrade L1/L2/L5 screen-size-cues mappings preview/live quiet-utilities artwork=" + sprites);
    }
    private void CheckLevelCues()
    {
        var expected = _session.ViewedCity.Slots.Select((s, i) => (s, i)).Where(p => p.s.Type != Building.Empty).ToArray();
        Require(_settlementView.LevelCues.Count == expected.Length, "one retained level cue per occupied settlement plot");
        foreach (var (slot, index) in expected)
        {
            var cue = _settlementView.LevelCues[index];
            Require(cue.Text == "L" + slot.Level && cue.Bounds.Size == new Vector2(34, 20), "cue reads accepted level and keeps native pixel dimensions");
            Require(GetViewport().GetVisibleRect().Encloses(cue.Bounds), "level cue remains inside viewport");
        }
    }
}
