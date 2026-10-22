using Game.Shared;
using Godot;

namespace Game.GodotClient;

/// <summary>Expandable factual recap; choosing a reward never requires dismissing it.</summary>
public partial class BattleResultStrip : VBoxContainer
{
    private Label _summary = null!, _details = null!;
    private Button _expand = null!;
    private string _key = "";
    public bool Compact { get; init; }
    internal string Summary => _summary.Text;
    internal string Details => _details.Text;
    internal Button ExpandButton => _expand;
    public void Initialize(HudSkin skin)
    {
        BoxContainer row = Compact ? new VBoxContainer() : new HBoxContainer(); AddChild(row);
        _summary = WrapLabel(); _summary.AddThemeFontSizeOverride("font_size", 14); row.AddChild(_summary);
        _expand = skin.Button("Details ▾", () => { _details.Visible = !_details.Visible; _expand.Text = _details.Visible ? "Details ▴" : "Details ▾"; });
        _expand.SizeFlagsVertical = SizeFlags.ShrinkCenter; row.AddChild(_expand);
        _details = WrapLabel(); _details.AddThemeFontSizeOverride("font_size", 14); AddChild(_details); _details.Hide(); Hide();
    }
    public void Refresh(BattleOutcome? outcome)
    {
        Visible = outcome is not null;
        if (outcome is null) { _key = ""; return; }
        string key = $"{outcome.City}:{outcome.Wave}";
        if (_key != key) { _key = key; _details.Hide(); _expand.Text = "Details ▾"; }
        _summary.Text = outcome.Summary; _details.Text = outcome.Details;
    }
}
