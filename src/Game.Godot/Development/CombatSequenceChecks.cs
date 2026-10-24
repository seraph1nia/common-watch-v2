// SPDX-License-Identifier: GPL-3.0-only
using System.Diagnostics;
using System.Text.Json;
using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class GameApplication
{
    private async Task SaveCombatSequenceSample(string path)
    {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path))!);
        System.IO.File.WriteAllText(path, JsonSerializer.Serialize(CombatSequenceSample(0), WireJson.Options));
    }
    private object CombatSequenceSample(double elapsed) => new
    {
        wallSeconds = elapsed, tick = _session.Snapshot.Tick,
        cameraMode = _camera.ArenaMode.ToString(),
        speed = CombatSpeedRates.Label(_session.Snapshot.CombatSpeed),
        factor = CombatSpeedRates.Factor(_session.Snapshot.CombatSpeed),
        geometry = _battlefield.MeasureCombat(),
        inspector = _gameUi.LiveInspector.Visible ? CombatMeasurements.Box(_gameUi.LiveInspector.GetGlobalRect()) : null,
        regions = _gameUi.Regions.Where(p => p.Value.IsVisibleInTree()).Select(p => new { name = p.Key, bounds = CombatMeasurements.Box(p.Value.GetGlobalRect()) }).ToArray()
    };
    private async Task CaptureCombatSequences(string directory, bool majorComparison = false)
    {
        directory = System.IO.Path.GetFullPath(directory); System.IO.Directory.CreateDirectory(directory);
        Engine.MaxFps = 60;
        foreach (string profile in majorComparison ? new[] { "w10-boss", "w10-elite", "w20-boss", "w20-elite" } : new[] { "early", "dense", "boss-10", "boss-20" })
        {
            ReplaceSession(majorComparison ? MajorEncounterComparison.CreateAt(profile.StartsWith("w10", StringComparison.Ordinal) ? 10 : 20, profile.EndsWith("elite", StringComparison.Ordinal), heavy: Argument("--major-formation") == "heavy")
                : profile == "early" ? ScenarioRunner.CreateAt("battle-start")
                : GameplayScenarios.CreateAt(profile == "dense" ? "army" : profile));
            _frozen = true;
            if (majorComparison)
            {
                RefreshWorld(); RefreshUi(true); await JourneyFrames();
                await GameplayCapture(profile + "-forecast");
                System.IO.File.WriteAllText(System.IO.Path.Combine(directory, profile + "-prepared.json"), JsonSerializer.Serialize(_session.Snapshot, WireJson.Options));
            }
            if (_session.Snapshot.Phase != Phase.Combat) _session.Send("ready");
            Require(_session.Snapshot.Phase == Phase.Combat, "normal sequence starts at a legal combat boundary");
            _session.Send("speed-1x"); Require(_session.LastResult!.Accepted, "ordinary 1x speed accepted");
            RefreshWorld(); RefreshUi(true);
            // The camera saves a requested default after native HUD layout.
            // Keep simulation frozen until that initial measured pose settles.
            await JourneyFrames();
            await JourneyFrames();
            var samples = new List<object>(); var watch = Stopwatch.StartNew();
            long startTick = _session.Snapshot.Tick; int frame = 0, screenshots = 0; double nextCapture = 0;
            bool inspected = false;
            long? resolvedTick = null, authorityCleanupTick = null;
            double? authorityResolvedWall = null, presentedResolvedWall = null, authorityCleanupWall = null;
            _frozen = false;
            while (_battlefield.Active)
            {
                Require(watch.Elapsed.TotalSeconds < 90, "normal sequence bounded at 90 seconds: " + profile);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                double elapsed = watch.Elapsed.TotalSeconds;
                if (resolvedTick is null && _session.Snapshot.Phase != Phase.Combat)
                { resolvedTick = _session.Snapshot.Tick; authorityResolvedWall = elapsed; }
                if (resolvedTick is not null && presentedResolvedWall is null && _session.Playback.Tick >= resolvedTick) presentedResolvedWall = elapsed;
                if (resolvedTick is not null && authorityCleanupTick is null && _session.Snapshot.DyingBodies.Length == 0)
                { authorityCleanupTick = _session.Snapshot.Tick; authorityCleanupWall = elapsed; }
                if (frame < 450 && elapsed >= nextCapture)
                {
                    string? path = null;
                    // PNG encoding stalls the main thread. Keep just three
                    // review stills; external desktop recording owns the video.
                    if (screenshots == 0 || screenshots == 1 && inspected || screenshots == 2 && _session.Snapshot.Tick - startTick >= 120)
                    {
                        path = System.IO.Path.Combine(directory, $"{profile}-still-{screenshots++}.png");
                        using var image = GetViewport().GetTexture().GetImage();
                        Require(image.SavePng(path) == Error.Ok, "normal-speed actual viewport still");
                    }
                    samples.Add(new { frame = frame++, image = path is null ? null : System.IO.Path.GetFileName(path), data = CombatSequenceSample(elapsed) });
                    nextCapture = elapsed + .2;
                }
                if (!inspected && _session.Snapshot.Tick - startTick >= 60 && _battlefield.Active)
                {
                    var actor = _session.Playback.Units().FirstOrDefault(u => u.Faction == Faction.Adventurers && u.Health > 0 && u.TargetId > 0);
                    if (actor is not null)
                    {
                        var layout = new ArenaLayout(_session.Snapshot.Arena!, new HexProjection(30, _options.GroundCompression));
                        var at = CombatVisuals.Position(actor, _session.Snapshot, _battlefield.AnimationTick);
                        var point = _camera.ToScreen(new(at.X - layout.Center.X, at.Y * _options.GroundCompression - layout.Center.Y));
                        if (GetViewport().GetVisibleRect().HasPoint(point)) { Click(point); inspected = _gameUi.LiveInspector.SelectedUnit == actor.Id; }
                    }
                }
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            _frozen = true; watch.Stop();
            var state = _session.Snapshot;
            var report = new
            {
                profile, match = state.MatchId, seed = state.CombatSeed, width = GetWindow().Size.X, height = GetWindow().Size.Y,
                mode = _options.Mode.ToString(), view = _options.View.ToString(), shader = _battlefield.ShaderEnabled,
                speed = "1x", factor = .5, startTick, endTick = state.Tick,
                simulationSeconds = (state.Tick - startTick) / (double)Match.StepsPerSecond,
                wallSeconds = watch.Elapsed.TotalSeconds,
                resolvedTick, authorityCleanupTick, authorityResolvedWall, authorityCleanupWall, presentedResolvedWall,
                presentedCleanupWall = presentedResolvedWall is { } settled ? watch.Elapsed.TotalSeconds - settled : (double?)null,
                pausedWallSeconds = 0, maxCaptureFrames = 3, sampleCadenceSeconds = .2, samples,
                measurementOverhead = "Three PNG readbacks, geometry sampling and optional external 15fps desktop recording; software rendering is not human/hardware qualification.",
                inspected, phase = state.Phase.ToString(),
                outcome = state.Players.Select(p => new { p.Id, p.Health, soldiers = p.Soldiers.Select(u => new { u.Id, u.Health, u.Type, u.Level }).ToArray() }).ToArray()
            };
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, profile + ".json"), JsonSerializer.Serialize(report, WireJson.Options));
            GD.Print($"GODOT_COMBAT_SEQUENCE_PASS {profile} ticks={state.Tick - startTick} wall={watch.Elapsed.TotalSeconds:0.00}s displayed=1x factor=0.5 samples={frame} stills={screenshots} inspected={inspected}");
        }
    }
}
