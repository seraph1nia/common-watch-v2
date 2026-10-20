using Godot;

namespace Game.GodotClient;

/// <summary>Shared static backdrop, independent of the foreground menu's layout and input.</summary>
internal static class MenuBackground
{
    public static bool Attach(Control owner, Texture2D? texture)
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
        void FitBackground()
        {
            image.Position = -owner.GlobalPosition;
            image.Size = owner.GetViewportRect().Size;
        }
        owner.Resized += FitBackground;
        FitBackground();
        return true;
    }
}
