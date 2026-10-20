using Godot;

namespace Game.GodotClient;

public partial class LobbyPanel
{
    private void InitializeBackground(Texture2D? texture)
    {
        if (!MenuBackground.Attach(this, texture, atmosphere: true)) return;
        AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
        var labelBacking = new StyleBoxFlat
        {
            BgColor = new Color(0.045f, 0.065f, 0.09f, 0.9f),
            ContentMarginLeft = 12, ContentMarginRight = 12,
            ContentMarginTop = 6, ContentMarginBottom = 6
        };
        // Doctrine cards and buttons already have opaque faces; protect the loose text too.
        foreach (var label in _setup.GetChildren().OfType<Label>().Append(_title).Append(Roster).Append(MultiplayerStatus))
            label.AddThemeStyleboxOverride("normal", labelBacking);
    }
}
