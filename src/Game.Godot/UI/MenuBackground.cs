using Godot;

namespace Game.GodotClient;

/// <summary>Shared static backdrop, independent of the foreground menu's layout and input.</summary>
internal static class MenuBackground
{
    public static bool Attach(Control owner, Texture2D? texture, bool atmosphere = false)
    {
        if (texture is null) return false;
        // Containers must not resize the image to their content area.
        var backdrop = new Node2D { ShowBehindParent = true };
        owner.AddChild(backdrop);
        var image = new TextureRect
        {
            Texture = texture,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        backdrop.AddChild(image);
        EnvironmentView? environment = atmosphere
            ? owner.GetNodeOrNull<EnvironmentController>("/root/CommonWatch/Environment")?.Attach(backdrop, EnvironmentScene.Menu) : null;
        void FitBackground()
        {
            image.Position = -owner.GlobalPosition;
            image.Size = owner.GetViewportRect().Size;
            var bounds = new Rect2(image.Position, image.Size);
            environment?.SetArea(bounds, bounds);
        }
        owner.Resized += FitBackground;
        owner.TreeExiting += () => owner.Resized -= FitBackground;
        FitBackground();
        return true;
    }
}
