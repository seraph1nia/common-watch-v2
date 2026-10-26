using Godot;
using Game.Core;
using Game.Shared;

namespace Game.GodotClient;

public partial class GameApplication
{
    private async Task CheckSettlementMap(string directory)
    {
        ReplaceSession(ScenarioRunner.CreateAt("initial")); _frozen = true; RefreshWorld();
        Require(_settlementView.HasMap, "repository-local settlement map loaded");
        foreach (var size in new[] { new Vector2I(1280, 720), new Vector2I(1920, 1080), new Vector2I(1280, 800), new Vector2I(1600, 1000), new Vector2I(2560, 1080) })
        {
            GetWindow().Size = size; GetWindow().Position = Vector2I.Zero;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            RefreshWorld(); _camera.Reset();
            _session.SelectSlot(-1); MoveMouse(new Vector2(10, 10));
            await CaptureMap("empty");
            long revision = _session.Snapshot.Revision;
            for (int slot = 0; slot < 9; slot++)
            {
                // Empty-plot construction panels intentionally cover parts of the map.
                // Expose each target before its native click; never click through the HUD.
                if (_session.SelectedSlot >= 0)
                {
                    Click(_gameUi.ClearSelectionButton.GetGlobalRect().GetCenter());
                    Require(_session.SelectedSlot == -1, "native clear-selection control exposes every painted pad");
                }
                RefreshUi(true);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Vector2 point = _camera.ToScreen(_settlementView.PlotPosition(slot));
                Require(WorldArea.HasPoint(point), "default framing exposes slot " + slot);
                Click(point);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                Require(_session.SelectedSlot == slot && _settlementView.HoveredSlot == slot,
                    $"native painted-pad click and hover {slot}: selected={_session.SelectedSlot}, hovered={_settlementView.HoveredSlot}, mouse={GetViewport().GetMousePosition()}, target={point}, control={GetViewport().GuiGetHoveredControl()?.GetPath()}, bounds={GetViewport().GuiGetHoveredControl()?.GetGlobalRect()}");
                Require(slot < 5 ? _gameUi.Inspector.BuildButtons.Values.Any(b => b.Visible)
                    : _gameUi.Inspector.Actions.ContainsKey("Buy"), "existing construction/purchase inspector " + slot);
            }
            Require(_session.Snapshot.Revision == revision, "map selection and hover never mutate authority");
            _session.SelectSlot(-1); MoveMouse(_camera.ToScreen(_settlementView.PlotPosition(7)));
            await CaptureMap("hover-locked");
            Click(_camera.ToScreen(_settlementView.PlotPosition(4)));
            MoveMouse(new Vector2(10, 10)); await CaptureMap("selected");
            // Native drag and wheel input followed by an actual click, beyond transform-only checks.
            Vector2 drag = _camera.ToScreen(_settlementView.PlotPosition(4));
            float beforeZoom = _camera.Magnification;
            GetViewport().PushInput(new InputEventMouseButton { Position = drag, ButtonIndex = MouseButton.Middle, Pressed = true }, true);
            GetViewport().PushInput(new InputEventMouseMotion { Position = drag + new Vector2(37, -23), Relative = new Vector2(37, -23), ButtonMask = MouseButtonMask.Middle }, true);
            GetViewport().PushInput(new InputEventMouseButton { Position = drag + new Vector2(37, -23), ButtonIndex = MouseButton.Middle, Pressed = false }, true);
            GetViewport().PushInput(new InputEventMouseButton { Position = drag, ButtonIndex = MouseButton.WheelUp, Pressed = true }, true);
            GetViewport().PushInput(new InputEventMouseButton { Position = drag, ButtonIndex = MouseButton.WheelUp, Pressed = false }, true);
            Require(_camera.PanPixels == Vector2.Zero && _camera.Magnification == beforeZoom, "native pan and wheel leave fixed settlement unchanged");
            _session.SelectSlot(-1); RefreshUi(true);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            GetViewport().GuiReleaseFocus();
            Click(_camera.ToScreen(_settlementView.PlotPosition(2)));
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(_session.SelectedSlot == 2, $"native painted-pad selection after attempted pan and zoom: selected={_session.SelectedSlot}, hovered={_settlementView.HoveredSlot}");
            await CaptureMap("fixed-camera"); _camera.Reset();
        }
        // Paid scenarios, and a real locked-plot purchase through the existing native button.
        ReplaceSession(ScenarioRunner.CreateAt("initial")); _frozen = true; RefreshWorld();
        Click(_camera.ToScreen(_settlementView.PlotPosition(7))); RefreshUi(true);
        Press(_gameUi.Inspector.Actions["Buy"]);
        Require(_session.LastResult?.Accepted == true && _session.City.Slots[7].Purchased, "painted locked pad buys authoritative land");
        Press(_gameUi.Inspector.BuildButtons[Building.Farm]);
        Press(_gameUi.Inspector.Construction.ConfirmButton);
        Require(_session.LastResult?.Accepted == true && _session.City.Slots[7].Type == Building.Farm, "construction on purchased upper-right pad");
        MoveMouse(new Vector2(10, 10)); await CaptureMap("purchased-building");
        ReplaceSession(ScenarioRunner.CreateAt("sprite-settlement")); _frozen = true; RefreshWorld();
        MoveMouse(new Vector2(10, 10)); await CaptureMap("buildings");
        GetWindow().Size = new Vector2I(1280, 720);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); RefreshWorld(); _camera.Reset();
        await CaptureMap("buildings");
        await SmokeCameraAndInput();
        await CheckLivingSettlementMap(directory);
        GD.Print("GODOT_SETTLEMENT_MAP_PASS nine-pads ellipses native-hover selection fixed-pan fixed-zoom resize cover purchase construction depth-sort");

        async Task CaptureMap(string name)
        {
            RefreshUi(true); RefreshWorld();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            System.IO.Directory.CreateDirectory(directory);
            string path = System.IO.Path.Combine(directory, name + "-" + GetWindow().Size.X + (GetWindow().Size.Y == GetWindow().Size.X * 9 / 16 ? "" : "-" + GetWindow().Size.Y) + ".png");
            using var image = GetViewport().GetTexture().GetImage();
            Require(image.SavePng(path) == Error.Ok, "settlement capture " + path);
            GD.Print("GODOT_SETTLEMENT_CAPTURE " + path);
        }
    }
    private void MoveMouse(Vector2 position)
    {
        Input.WarpMouse(position);
        GetViewport().PushInput(new InputEventMouseMotion { Position = position, GlobalPosition = position }, true);
    }
}
