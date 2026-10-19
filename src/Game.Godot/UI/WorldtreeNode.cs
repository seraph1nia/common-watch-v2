using Godot;

namespace Game.GodotClient;

/// <summary>A native button with circular picking and purely visual ring states.</summary>
public partial class WorldtreeNode : Button
{
    private WorldtreeArt _art = null!;
    private Texture2D? _icon;
    private string _tier = "";
    private bool _hovered;
    public string ResearchState { get; private set; } = "locked";
    public string RingState => _hovered ? "hover" : ButtonPressed || HasFocus() ? "selected" : ResearchState;
    public bool Hovered => _hovered;
    public void Initialize(WorldtreeArt art, Texture2D? icon, int tier)
    {
        _art = art; _icon = icon; _tier = new string('I', tier);
        ToggleMode = true; FocusMode = FocusModeEnum.All; MouseDefaultCursorShape = CursorShape.PointingHand;
        MouseFilter = MouseFilterEnum.Stop;
        foreach (string state in new[] { "normal", "hover", "pressed", "hover_pressed", "focus" })
            AddThemeStyleboxOverride(state, new StyleBoxEmpty());
        MouseEntered += () => { _hovered = true; QueueRedraw(); };
        MouseExited += () => { _hovered = false; QueueRedraw(); };
        FocusEntered += QueueRedraw; FocusExited += QueueRedraw;
    }
    public void Present(string state, bool selected, string tooltip)
    {
        ResearchState = state; SetPressedNoSignal(selected); TooltipText = tooltip; QueueRedraw();
    }
    public override bool _HasPoint(Vector2 point) => ((point - Size / 2) / (Size * .37f)).LengthSquared() <= 1;
    public override void _Draw()
    {
        if (_art is null) return;
        var center = Size / 2;
        DrawCircle(center, Size.X * .29f, new Color("142e2b"));
        DrawTextureRect(_art.Rings[RingState], new Rect2(Vector2.Zero, Size), false);
        if (ResearchState == "owned") DrawArc(center, Size.X * .28f, 0, Mathf.Tau, 48, new Color("f7d47b"), 2, true);
        Color ink = RingState == "idle" ? new Color("234b3b") : new Color("f4deb1");
        if (_icon is not null)
        {
            Vector2 iconSize = Vector2.One * Size.X * .25f;
            DrawTextureRect(_icon, new Rect2(center - iconSize / 2 - new Vector2(0, Size.Y * .04f), iconSize), false, ink);
        }
        var font = GetThemeDefaultFont();
        int fontSize = Math.Max(10, (int)(Size.Y * .115f));
        string badge = ResearchState == "owned" ? "✓" : _tier;
        float width = font.GetStringSize(badge, fontSize: fontSize).X;
        DrawString(font, center + new Vector2(-width / 2, Size.Y * .20f), badge, fontSize: fontSize, modulate: ink);
        if (HasFocus()) DrawArc(center, Size.X * .39f, 0, Mathf.Tau, 64, new Color("e4faff"), 2, true);
    }
}
