using Godot;
using Game.Shared;

namespace Game.GodotClient;

public partial class GameApplication
{
    private async Task SmokeCameraAndInput()
    {
        var originalOptions = _options;
        var originalSize = GetWindow().Size;
        long revision = _session.Snapshot.Revision;
        foreach (var size in new[] { new Vector2I(1280, 720), new Vector2I(1920, 1080) })
        {
            GetWindow().Size = size;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            foreach (var view in new[] { ViewPreset.Topdown, ViewPreset.Angled })
            {
                _options = _options with { View = view };
                RefreshWorld();
                foreach (float zoom in new[] { .55f, 1, 2.2f })
                {
                    _camera.SetPose(zoom, new Vector2(37, -23));
                    for (int plot = 0; plot < SettlementLayout.Coordinates.Count; plot++)
                    {
                        Vector2 point = _camera.ToScreen(_settlementView.PlotPosition(plot));
                        Require(_settlementView.Pick(_camera.ToWorld(point)) == plot, $"native camera logical picking {view} zoom={zoom} size={size} plot={plot}");
                        IEnumerable<Vector2> edges;
                        if (_settlementView.MapLayout is { } map)
                        {
                            var spot = map.Spots[plot];
                            edges = Enumerable.Range(0, 12).Select(i =>
                                _settlementView.PlotPosition(plot) + new Vector2(Mathf.Cos(i * Mathf.Tau / 12) * spot.Radii.X,
                                    Mathf.Sin(i * Mathf.Tau / 12) * spot.Radii.Y) * .98f);
                            Vector2 outside = _settlementView.PlotPosition(plot) + new Vector2(spot.Radii.X, spot.Radii.Y) * .9f;
                            Require(_settlementView.Pick(_camera.ToWorld(_camera.ToScreen(outside))) != plot,
                                "ellipse corners are outside the painted pad");
                        }
                        else edges = _settlementView.Projection.Corners(SettlementLayout.Coordinates[plot]).Select(corner =>
                        {
                            var logicalCenter = _settlementView.Projection.Project(SettlementLayout.Coordinates[plot]);
                            return _settlementView.PlotPosition(plot) + new Vector2(corner.X - logicalCenter.X, corner.Y - logicalCenter.Y) * .85f;
                        });
                        foreach (var inside in edges)
                        {
                            Require(_settlementView.Pick(_camera.ToWorld(_camera.ToScreen(inside))) == plot,
                                $"native picking near plot edges {view} zoom={zoom} size={size} plot={plot}");
                        }
                    }
                }
            }
        }
        _options = originalOptions; GetWindow().Size = originalSize;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        RefreshWorld(); _camera.Reset();
        float expectedDefault = _settlementView.Framing(WorldArea.Size);
        Require(_camera.Magnification == expectedDefault && _camera.PanPixels == Vector2.Zero, "camera reset restores settlement framing");
        Vector2 target = _camera.ToScreen(_settlementView.PlotPosition(4));
        GetViewport().GuiReleaseFocus();
        Click(target);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Require(_session.SelectedSlot == 4, "native mouse event selects a logical settlement slot");
        int selected = _session.SelectedSlot;
        float before = _camera.Magnification;
        GetViewport().PushInput(new InputEventMouseButton { Position = _gameUi.Inspector.GetGlobalRect().GetCenter(), ButtonIndex = MouseButton.WheelUp, Pressed = true }, true);
        GetViewport().PushInput(new InputEventMouseButton { Position = _gameUi.Inspector.GetGlobalRect().GetCenter(), ButtonIndex = MouseButton.WheelUp, Pressed = false }, true);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Require(_camera.Magnification == before, "scrolling native inspector does not zoom world");
        _camera.SetPose(1.3f, new Vector2(7, -9)); before = _camera.Magnification;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Click(_settingsButton.GetGlobalRect().GetCenter());
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Require(_settings.Visible && _settings.Slider.HasFocus(), $"mouse opens native modal with keyboard focus (visible={_settings.Visible}, focus={GetViewport().GuiGetFocusOwner()?.Name}, button={_settingsButton.GetGlobalRect()})");
        Vector2 coveredPlot = Enumerable.Range(0, 9).Select(i => _camera.ToScreen(_settlementView.PlotPosition(i)))
            .First(p => !_settings.Panel.GetGlobalRect().HasPoint(p));
        Require(!_settings.Panel.GetGlobalRect().HasPoint(coveredPlot), "modal capture probe avoids its interactive controls");
        Click(coveredPlot);
        Require(_settings.Visible, "clicking modal shade keeps settings open");
        GetViewport().PushInput(new InputEventKey { PhysicalKeycode = Key.R, Keycode = Key.R, Pressed = true }, true);
        GetViewport().PushInput(new InputEventKey { PhysicalKeycode = Key.R, Keycode = Key.R, Pressed = false }, true);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Require(_session.SelectedSlot == selected && _camera.Magnification == before, "modal captures world clicks and reset shortcut");
        using var cancel = new InputEventKey { Keycode = Key.Escape, PhysicalKeycode = Key.Escape, Pressed = true };
        GetViewport().PushInput(cancel, true);
        GetViewport().PushInput(new InputEventKey { Keycode = Key.Escape, PhysicalKeycode = Key.Escape, Pressed = false }, true);
        Require(!_settings.Visible, "native cancel action closes settings");
        Require(_settingsButton.HasFocus(), "closing modal restores focus");
        _camera.Reset();
        Require(_session.Snapshot.Revision == revision, "camera and local UI do not issue authoritative commands");
        GD.Print("GODOT_CAMERA_PASS native-transform=true topdown/angled zoom=0.55/1/2.2 viewport=1280/1920 centers+edges mouse modal scroll focus reset");
    }
    private void Click(Vector2 position)
    {
        Input.WarpMouse(position);
        GetViewport().PushInput(new InputEventMouseMotion { Position = position, GlobalPosition = position }, true);
        GetViewport().PushInput(new InputEventMouseButton { Position = position, GlobalPosition = position, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true }, true);
        GetViewport().PushInput(new InputEventMouseButton { Position = position, GlobalPosition = position, ButtonIndex = MouseButton.Left, Pressed = false }, true);
    }
}
