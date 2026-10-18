using Godot;

namespace Game.GodotClient;

public static class Ui
{
    public static MarginContainer Margin(Control child, int padding)
    {
        var margin = new MarginContainer();
        foreach (string side in new[] { "left", "top", "right", "bottom" }) margin.AddThemeConstantOverride("margin_" + side, padding);
        margin.AddChild(child); return margin;
    }
    public static Label WrapLabel(string text = "") => new() { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
}
