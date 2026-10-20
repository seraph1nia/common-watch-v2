using System.Text.Json;
using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class GameApplication
{
    private async Task CaptureModularArena(string directory)
    {
        directory = System.IO.Path.GetFullPath(directory); System.IO.Directory.CreateDirectory(directory);
        _frozen = true; _gameUi.Hide(); if (_battleLayer is not null) _battleLayer.Hide();
        var receipts = new List<object>();
        for (int players = 1; players <= 6; players++)
        {
            _battle = new("arena", players) { Paused = true };
            var arena = _battle.Snapshot.Arena!;
            var cells = arena.Board.Cells.ToDictionary(c => c.Id);
            var topology = new ArenaLayout(arena, new HexProjection(30));
            var hit = _battle.Frames.SelectMany(f => f.CombatEvents).First(e => e.Type == CombatEventType.StructureHit);
            var destroyed = _battle.Frames.SelectMany(f => f.CombatEvents).First(e => e.Type == CombatEventType.StructureDestroyed);
            var crossing = _battle.Frames.FirstOrDefault(f => f.Enemies.Any(u => u.Hex is { Action: UnitActionKind.Moving } h
                && cells[h.Position.Cell].Coordinate.Column / topology.Columns != cells[h.Destination.Cell].Coordinate.Column / topology.Columns));
            if (players > 1) Require(crossing is not null, "legal cross-sector movement exists for " + players);
            foreach (var size in new[] { new Vector2I(1280, 720), new Vector2I(1920, 1080) })
            foreach (var view in new[] { ViewPreset.Angled, ViewPreset.Topdown })
            {
                GetWindow().Size = size; _options = _options with { View = view, Mode = RenderMode.Sprites };
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                _arenaCameraKey = ""; SeekBattle(74); RefreshWorld();
                var projection = new HexProjection(30, _options.GroundCompression);
                var layout = new ArenaLayout(arena, projection);
                string prefix = $"{players}-sectors-{view.ToString().ToLowerInvariant()}-{size.X}";
                // Exercise native input and ensure RefreshWorld never snaps pan/zoom back.
                var at = WorldArea.GetCenter(); float before = _camera.Magnification;
                GetViewport().GuiReleaseFocus();
                GetViewport().PushInput(new InputEventMouseButton { Position = at, ButtonIndex = MouseButton.WheelUp, Pressed = true }, true);
                GetViewport().PushInput(new InputEventMouseButton { Position = at, ButtonIndex = MouseButton.WheelUp, Pressed = false }, true);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                Require(_camera.Magnification > before, "arena native wheel zoom persists");
                GetViewport().PushInput(new InputEventMouseButton { Position = at, ButtonIndex = MouseButton.Middle, Pressed = true }, true);
                GetViewport().PushInput(new InputEventMouseMotion { Position = at + new Vector2(45, 10), Relative = new Vector2(45, 10), ButtonMask = MouseButtonMask.Middle }, true);
                GetViewport().PushInput(new InputEventMouseButton { Position = at, ButtonIndex = MouseButton.Middle, Pressed = false }, true);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                Require(_camera.PanPixels.Length() > 20, "arena native pan persists");
                _camera.Reset(); RefreshWorld(); await Capture("own-sector");
                GetViewport().PushInput(new InputEventKey { Keycode = Key.F, PhysicalKeycode = Key.F, Pressed = true }, true);
                GetViewport().PushInput(new InputEventKey { Keycode = Key.F, PhysicalKeycode = Key.F, Pressed = false }, true);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var bounds = _battlefield.RenderBounds;
                Require(WorldArea.Encloses(new Rect2(_camera.ToScreen(bounds.Position), bounds.Size * _camera.Magnification)), "whole field overview fits");
                foreach (var cell in cells.Values)
                {
                    var p = projection.Project(cell.Coordinate) - layout.Center;
                    Vector2 local = new(p.X, p.Y);
                    var roundtrip = _camera.ToWorld(_camera.ToScreen(local));
                    Require(projection.HitTest(new System.Numerics.Vector2(roundtrip.X, roundtrip.Y) + layout.Center, cells.Values.Select(c => c.Coordinate)) == cell.Coordinate, "all authoritative cells pick through native camera");
                }
                await Capture("overview");
                SeekBattle(hit.Tick + 2); CheckPicking(); await Capture("structure-hit");
                SeekBattle(destroyed.Tick + 8); CheckPicking(); await Capture("structure-destroyed");
                if (crossing is not null)
                {
                    SeekBattle(crossing.Tick + .5); CheckPicking(); await Capture("crossing");
                }
                receipts.Add(new { players, columns = layout.Columns, cells = cells.Count, view = view.ToString(), width = size.X, height = size.Y,
                    hitTick = hit.Tick, destroyedTick = destroyed.Tick, crossingTick = crossing?.Tick, nativePanZoom = true, picking = true });
                GD.Print($"GODOT_MODULAR_ARENA_PASS players={players} cells={cells.Count} view={view} size={size} cross={crossing?.Tick}");

                void CheckPicking()
                {
                    foreach (var structure in _battlefield.Playback.Structures())
                    {
                        var p = projection.Project(cells[structure.Cell].Coordinate) - layout.Center;
                        var local = _camera.ToWorld(_camera.ToScreen(new Vector2(p.X, p.Y)));
                        Require(_battlefield.PickStructure(local)?.Cell == structure.Cell, "structure picking at real cell");
                    }
                    foreach (var unit in _battlefield.Playback.Units().Where(u => u.Health > 0 && u.Hex?.Lifecycle == UnitLifecycle.Alive))
                    {
                        var raw = CombatVisuals.Position(unit, _battle.Snapshot, _battlefield.AnimationTick);
                        Vector2 ground = new(raw.X - layout.Center.X, raw.Y * _options.GroundCompression - layout.Center.Y);
                        Require(_battlefield.PickUnit(_camera.ToWorld(_camera.ToScreen(ground)))?.Id == unit.Id, "moving actor picking including sector seams");
                    }
                }
                async Task Capture(string name)
                {
                    RefreshWorld(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    using var image = GetViewport().GetTexture().GetImage();
                    Require(image.SavePng(System.IO.Path.Combine(directory, prefix + "-" + name + ".png")) == Error.Ok, "actual arena viewport capture");
                }
            }
        }
        System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "verification.json"), JsonSerializer.Serialize(receipts, new JsonSerializerOptions { WriteIndented = true }));
    }
}
