using Godot;

namespace Game.GodotClient;

// Native Godot tooltip lifecycle and text, with the same optional NinePatchRect skin.
public partial class ArtTooltipLabel : Label
{
    public Func<string, Control>? MakeTooltip { get; init; }
    public override GodotObject _MakeCustomTooltip(string forText) => MakeTooltip?.Invoke(forText) ?? new Label { Text = forText };
}
