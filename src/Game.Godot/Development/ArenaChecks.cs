using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class GameApplication
{
    private async Task CheckArenaFraming()
    {
        foreach (var size in new[] { new Vector2I(1280, 720), new Vector2I(1920, 1080), new Vector2I(1600, 1000), new Vector2I(2560, 1080) })
        {
            GetWindow().Size = size; await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            foreach (int players in Enumerable.Range(1, 6))
                foreach (double compression in new[] { 1.0, .58 })
                {
                    var bounds = ArenaPresentation.RenderBounds(new SharedArena(Enumerable.Range(1, players)).Snapshot(), new HexProjection(30, compression));
                    FrameArena(bounds);
                    var screen = new Rect2(_camera.ToScreen(bounds.Position), bounds.Size * _camera.Magnification);
                    Vector2 viewport = GetViewport().GetVisibleRect().Size;
                    Require(GetViewport().GetVisibleRect().Encloses(screen), "complete rendered arena visible");
                    Require(screen.Size.X <= viewport.X * .801f && screen.Size.Y <= viewport.Y * .801f &&
                        (Mathf.Abs(screen.Size.X / viewport.X - .8f) < .001f || Mathf.Abs(screen.Size.Y / viewport.Y - .8f) < .001f), "uniform 80 percent arena fit");
                    Require(screen.GetCenter().DistanceTo(viewport / 2) < .01f, "arena centered after resize");
                }
        }
        GetWindow().Size = new Vector2I(int.Parse(Argument("--width") ?? "1280"), int.Parse(Argument("--height") ?? "720"));
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); RefreshWorld();
    }
}
