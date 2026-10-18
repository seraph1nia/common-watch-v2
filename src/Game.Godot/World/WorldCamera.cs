using Godot;

namespace Game.GodotClient;

/// <summary>Camera transforms only. Logical angled hex projection remains independent.</summary>
public partial class WorldCamera : Camera2D
{
    public float Magnification { get; private set; } = 1;
    public Vector2 PanPixels { get; private set; }
    private Vector2 _origin;
    private bool _dragging;
    private float _defaultMagnification = 1;
    public override void _Ready()
    {
        AnchorMode = AnchorModeEnum.FixedTopLeft;
        PositionSmoothingEnabled = false;
        Enabled = true;
    }
    public void Layout(Vector2 origin)
    {
        _origin = origin;
        Zoom = Vector2.One * Magnification;
        Position = -(origin + PanPixels) / Magnification;
        ForceUpdateScroll();
    }
    public Vector2 ToWorld(Vector2 screen) => GetViewport().CanvasTransform.AffineInverse() * screen;
    public Vector2 ToScreen(Vector2 world) => GetViewport().CanvasTransform * world;
    public void SetPose(float zoom, Vector2 pan)
    {
        Magnification = Math.Clamp(zoom, .55f, 2.2f);
        PanPixels = pan;
        Layout(_origin);
    }
    public void ConfigureDefault(float zoom)
    {
        if (_defaultMagnification == zoom) return;
        bool atDefault = Magnification == _defaultMagnification && PanPixels == Vector2.Zero;
        _defaultMagnification = zoom;
        if (atDefault) Reset();
    }
    public void Reset() { StopDrag(); SetPose(_defaultMagnification, Vector2.Zero); }
    public void StopDrag() => _dragging = false;
    public void ZoomAt(Vector2 cursor, bool inward)
    {
        Vector2 before = ToWorld(cursor);
        Magnification = Math.Clamp(Magnification * (inward ? 1.12f : 1 / 1.12f), .55f, 2.2f);
        PanPixels = cursor - _origin - before * Magnification;
        Layout(_origin);
    }
    public void Handle(InputEvent input, Rect2 worldArea)
    {
        if (input.IsActionPressed("camera_reset")) { Reset(); return; }
        if (input is InputEventMouseButton mouse)
        {
            if (!worldArea.HasPoint(mouse.Position)) return;
            if (mouse.ButtonIndex is MouseButton.Middle or MouseButton.Right) _dragging = mouse.Pressed;
            if (mouse.Pressed && mouse.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
                ZoomAt(mouse.Position, mouse.ButtonIndex == MouseButton.WheelUp);
        }
        if (_dragging && input is InputEventMouseMotion motion) SetPose(Magnification, PanPixels + motion.Relative);
    }
}
