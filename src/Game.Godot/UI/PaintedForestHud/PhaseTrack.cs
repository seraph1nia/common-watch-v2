using Game.Core;
using Godot;

namespace Game.GodotClient;

/// <summary>The three actual production stages and combat, from the detached snapshot.</summary>
public partial class PhaseTrack : BoxContainer
{
    private readonly List<Label> _stages = [];
    private readonly List<TextureRect> _markers = [];
    private readonly Dictionary<string, Texture2D> _textures = [];
    private int _active = -1;
    private bool? _wide;
    public bool Illustrated { get; init; }
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        AddThemeConstantOverride("separation", 4);
        foreach (string state in Illustrated ? new[] { "upcoming", "active", "complete" } : Array.Empty<string>())
        {
            string path = $"res://UI/PaintedForestHud/textures/phase-{state}.svg";
            if (ResourceLoader.Exists(path)) _textures[state] = GD.Load<Texture2D>(path);
        }
        foreach (string name in new[] { "Prep 1", "Prep 2", "Prep 3", "Combat" })
        {
            var column = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore }; AddChild(column);
            column.AddThemeConstantOverride("separation", 2);
            var marker = new TextureRect { CustomMinimumSize = new(22, 22), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = MouseFilterEnum.Ignore }; column.AddChild(marker); _markers.Add(marker);
            marker.Visible = Illustrated;
            var label = new Label { Text = name, HorizontalAlignment = HorizontalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore };
            label.AddThemeFontSizeOverride("font_size", 14); column.AddChild(label); _stages.Add(label);
        }
    }
    public void Refresh(MatchSnapshot state, bool finishingCombat)
    {
        int active = state.Phase is Phase.Building or Phase.Preparation && !finishingCombat ? state.Turn - 1
            : state.Phase == Phase.Combat || finishingCombat ? 3 : 4;
        bool wide = GetViewportRect().Size.X >= 1600;
        if (_active == active && _wide == wide) return;
        _active = active;
        _wide = wide; Vertical = wide;
        for (int i = 0; i < _stages.Count; i++)
        {
            _markers[i].Texture = _textures.GetValueOrDefault(i < active ? "complete" : i == active ? "active" : "upcoming");
            _markers[i].CustomMinimumSize = wide ? new(22, 22) : new(14, 18);
            _stages[i].Text = i < 3 ? (wide ? $"Preparation {i+1}" : $"Prep {i+1}") : "Combat";
            if (!Illustrated) _stages[i].Text = (i < active ? "✓ " : i == active ? "● " : "○ ") + (i < 3 ? $"Prep {i+1}" : "Combat");
            _stages[i].AddThemeColorOverride("font_color", new Color(i == active ? "ffd27d" : i < active ? "97d6ba" : "b8b8c4"));
        }
    }
}
