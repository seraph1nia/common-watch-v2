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
                    }
                }
            }
        }
        _options = originalOptions; GetWindow().Size = originalSize;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        RefreshWorld(); _camera.Reset();
        Vector2 target = _camera.ToScreen(_settlementView.PlotPosition(4));
        GetViewport().GuiReleaseFocus();
        Click(target);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Require(_session.SelectedSlot == 4, "native mouse event selects a logical world hex");
        int selected = _session.SelectedSlot;
        float before = _camera.Magnification;
        GetViewport().PushInput(new InputEventMouseButton { Position = new Vector2(GetViewport().GetVisibleRect().Size.X - 80, 260), ButtonIndex = MouseButton.WheelUp, Pressed = true }, true);
        GetViewport().PushInput(new InputEventMouseButton { Position = new Vector2(GetViewport().GetVisibleRect().Size.X - 80, 260), ButtonIndex = MouseButton.WheelUp, Pressed = false }, true);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Require(_camera.Magnification == before, "scrolling native inspector does not zoom world");
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Click(_settingsButton.GetGlobalRect().GetCenter());
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Require(_settings.Visible && _settings.Slider.HasFocus(), $"mouse opens native modal with keyboard focus (visible={_settings.Visible}, focus={GetViewport().GuiGetFocusOwner()?.Name}, button={_settingsButton.GetGlobalRect()})");
        Click(target);
        GetViewport().PushInput(new InputEventKey { PhysicalKeycode = Key.R, Keycode = Key.R, Pressed = true }, true);
        GetViewport().PushInput(new InputEventKey { PhysicalKeycode = Key.R, Keycode = Key.R, Pressed = false }, true);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Require(_session.SelectedSlot == selected && _camera.Magnification == before, "modal captures world clicks and reset shortcut");
        _settings.Close();
        Require(_settingsButton.HasFocus(), "closing modal restores focus");
        Require(_session.Snapshot.Revision == revision, "camera and local UI do not issue authoritative commands");
        GD.Print("GODOT_CAMERA_PASS native-transform=true topdown/angled zoom=0.55/1/2.2 viewport=1280/1920 mouse modal scroll focus");
    }
    private void Click(Vector2 position)
    {
        Input.WarpMouse(position);
        GetViewport().PushInput(new InputEventMouseMotion { Position = position, GlobalPosition = position }, true);
        GetViewport().PushInput(new InputEventMouseButton { Position = position, GlobalPosition = position, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true }, true);
        GetViewport().PushInput(new InputEventMouseButton { Position = position, GlobalPosition = position, ButtonIndex = MouseButton.Left, Pressed = false }, true);
    }
}
