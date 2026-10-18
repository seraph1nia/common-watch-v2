using Godot;

namespace Game.GodotClient;

// A single retained foreground CanvasItem keeps health and event text above tall figures.
public partial class CombatOverlay : Node2D
{
    readonly List<(string Text, Vector2 Position, int Size, Color Color)> labels = [];
    readonly List<(Rect2 Rect, Color Color)> bars = [];
    public void Clear() { labels.Clear(); bars.Clear(); Visible = true; QueueRedraw(); }
    public void Text(string text, Vector2 position, int size, Color color) => labels.Add((text, position, size, color));
    public void Bar(Rect2 rectangle, Color color) => bars.Add((rectangle, color));
    public override void _Draw()
    {
        foreach (var bar in bars) DrawRect(bar.Rect, bar.Color);
        foreach (var label in labels)
        {
            float width = ThemeDB.FallbackFont.GetStringSize(label.Text, HorizontalAlignment.Left, -1, label.Size).X;
            DrawString(ThemeDB.FallbackFont, label.Position - new Vector2(width / 2, 0), label.Text, HorizontalAlignment.Left, -1, label.Size, label.Color);
        }
    }
}
