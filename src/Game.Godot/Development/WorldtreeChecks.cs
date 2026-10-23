using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class GameApplication
{
    // Viewport events exercise native picking without racing asynchronous X11 warp events.
    private void WorldtreePointer(Vector2 point) => GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
    private void WorldtreeClick(Vector2 point)
    {
        WorldtreePointer(point);
        GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true }, true);
        GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false }, true);
    }
    private async Task CheckWorldtreePresentation()
    {
        var tree = _gameUi.Details.Research;
        if (!tree.Illustrated) return;
        foreach (var size in new[] { new Vector2I(1280, 720), new Vector2I(1920, 1080) })
        {
            ReplaceSession(GameplayScenarios.CreateAt("research")); _frozen = true;
            GetWindow().Size = size; RefreshUi(true); OpenDetails(DetailsPage.Worldtree);
            Require(!_session.Snapshot.Paused, "opening Worldtree does not pause the match");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            tree.ResetSelection(); RefreshUi(true); WorldtreePointer(new(10, 10));
            GetViewport().GuiGetFocusOwner()?.ReleaseFocus();
            await GameplayCapture($"worldtree-{size.X}-empty");
            Require(tree.TechnologyButtons.Count == 18, "18 illustrated canonical nodes");
            var catalog = _session.Snapshot.TechnologyCatalog;
            Require(tree.PrerequisiteEdges.ToHashSet().SetEquals(catalog.Where(n => n.Prerequisite != TechnologyId.None)
                .Select(n => (n.Prerequisite, n.Id))), "connector overlays include every real prerequisite, including production offshoots");
            foreach (var control in tree.TechnologyButtons.Values)
            {
                Require(tree.GetGlobalRect().Encloses(control.GetGlobalRect()), "Worldtree socket fits viewport");
                Require(!tree.DetailsArea.Intersects(control.GetGlobalRect()), "Worldtree frame leaves every socket clear");
            }
            var available = (WorldtreeNode)tree.TechnologyButtons["TechRangedFoundation"];
            var locked = (WorldtreeNode)tree.TechnologyButtons["TechAssault"];
            long revision = _session.Snapshot.Revision;
            foreach (var node in catalog)
            {
                WorldtreeClick(tree.TechnologyButtons["Tech" + node.Id].GetGlobalRect().GetCenter());
                Require(tree.Selected == node.Id && _session.Snapshot.Revision == revision, "every technology is inspectable without spending: " + node.Name);
            }
            WorldtreeClick(locked.GetGlobalRect().GetCenter());
            Require(tree.DetailsText.Contains("Permanently locked"), "canonical exclusive-branch denial is shown");
            tree.ResetSelection(); RefreshUi(true);
            WorldtreeClick(available.GetGlobalRect().Position + available.Size * .04f);
            Require(tree.Selected == TechnologyId.None, "transparent socket corners ignore input");
            WorldtreePointer(available.GetGlobalRect().GetCenter());
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(available.Hovered && available.RingState == "hover", $"native socket hover: hovered={GetViewport().GuiGetHoveredControl()?.GetPath()}, mouse={GetViewport().GetMousePosition()}, target={available.GetGlobalRect().GetCenter()}");
            await GameplayCapture($"worldtree-{size.X}-hover");
            WorldtreePointer(locked.GetGlobalRect().GetCenter());
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            WorldtreeClick(locked.GetGlobalRect().GetCenter()); WorldtreePointer(new(10, 10));
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(tree.Selected == TechnologyId.Assault && locked.ResearchState == "locked" && locked.RingState == "selected",
                $"native locked-node inspection: selected={tree.Selected}, state={locked.ResearchState}, ring={locked.RingState}, focus={locked.HasFocus()}, rect={locked.GetGlobalRect()}");
            Require(tree.PurchaseButtons["Purchase"].Disabled && !tree.PurchaseButtons["Purchase"].Visible, "locked node cannot purchase");
            await GameplayCapture($"worldtree-{size.X}-locked");
            WorldtreeClick(available.GetGlobalRect().GetCenter()); WorldtreePointer(new(10, 10));
            Require(tree.Selected == TechnologyId.RangedFoundation && available.HasFocus(), "native click selects and focuses available node");
            Require(tree.DetailsText.Contains("Pivot") || tree.DetailsText.Contains("field squads"), "Worldtree connects its selected effect to the actual roster or future recruits");
            Require(_session.Snapshot.Revision == revision, "inspection spends nothing");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var purchase = tree.PurchaseButtons["Purchase"];
            Require(purchase.Visible && !purchase.Disabled && tree.DetailsArea.Encloses(purchase.GetGlobalRect()), "eligible purchase fits details frame");
            await GameplayCapture($"worldtree-{size.X}-available");
            // Focus moves without selecting or spending, and pointer hover stays independent.
            GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = JoyButton.DpadRight, Pressed = true }, true);
            GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = JoyButton.DpadRight, Pressed = false }, true);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(GetViewport().GuiGetFocusOwner() is WorldtreeNode neighbor && neighbor != available
                && neighbor.RingState == neighbor.ResearchState, "controller focus moves without replacing research-state appearance");
            WorldtreePointer(locked.GetGlobalRect().GetCenter());
            Require(tree.Selected == TechnologyId.RangedFoundation && available.ButtonPressed && locked.Hovered
                && GetViewport().GuiGetFocusOwner() != locked, "selection, keyboard focus and hover remain independent");
            await GameplayCapture($"worldtree-{size.X}-focus");
            WorldtreePointer(new(10, 10));
            GetViewport().PushInput(new InputEventKey { Keycode = Key.Space, Pressed = true }, true);
            GetViewport().PushInput(new InputEventKey { Keycode = Key.Space, Pressed = false }, true);
            Require(tree.TechnologyButtons["Tech" + tree.Selected] == GetViewport().GuiGetFocusOwner(), "keyboard activation inspects focused technology");
            WorldtreeClick(available.GetGlobalRect().GetCenter());
            int points = _session.City.Research.Points;
            if (size.X == 1920)
            {
                for (int step = 0; step < 18 && !purchase.HasFocus(); step++)
                {
                    GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = JoyButton.DpadRight, Pressed = true }, true);
                    GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = JoyButton.DpadRight, Pressed = false }, true);
                }
                Require(purchase.HasFocus(), "controller can reach eligible purchase from sockets");
                GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = JoyButton.A, Pressed = true }, true);
                GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = JoyButton.A, Pressed = false }, true);
                _frozen = true;
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            else WorldtreeClick(purchase.GetGlobalRect().GetCenter());
            _frozen = true; RefreshUi(true);
            Require(_session.City.Research.Has(TechnologyId.RangedFoundation) && _session.LastResult?.Accepted == true,
                $"native purchase reaches authority: selected={tree.Selected}, focus={GetViewport().GuiGetFocusOwner()?.Name}, result={_session.LastResult}, accept={InputMap.ActionGetEvents("ui_accept").Count}");
            Require(_session.City.Research.Points == points - catalog.Single(n => n.Id == TechnologyId.RangedFoundation).Cost,
                "Research spends the authoritative quoted cost exactly once");
            WorldtreeClick(available.GetGlobalRect().GetCenter());
            Require(tree.Selected == TechnologyId.RangedFoundation && tree.DetailsText.Contains("ACQUIRED")
                && !tree.PurchaseButtons["Purchase"].Visible, "owned nodes remain inspectable without a second purchase");
            WorldtreePointer(new(10, 10));
            await GameplayCapture($"worldtree-{size.X}-owned-selected");
            GetViewport().GuiGetFocusOwner()?.ReleaseFocus(); WorldtreePointer(new(10, 10)); tree.ResetSelection();
            Require(available.ResearchState == "owned" && available.RingState == "owned", "acquired state reflects authority");
            await GameplayCapture($"worldtree-{size.X}-acquired");
            GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = JoyButton.B, Pressed = true }, true);
            GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = JoyButton.B, Pressed = false }, true);
            Require(!_gameUi.Details.Visible && GetViewport().GuiGetFocusOwner() is { } restored && restored.IsVisibleInTree(), "controller cancel closes modal and restores focus");
        }
        GD.Print("GODOT_WORLDTREE_PASS sockets=18 circular-picking hover selected locked available acquired native-purchase keyboard controller 720p/1080p");
    }
}
