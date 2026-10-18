using Godot;

namespace Game.GodotClient;

/// <summary>Owns the only active session. Transport and presentation never own each other.</summary>
public partial class SessionCoordinator : Node
{
    public GodotSession? Current { get; private set; }
    public event Action? Changed;

    public GodotSession Open(Action<GodotSession> initialize)
    {
        Close();
        var session = new GodotSession { Name = "Session" };
        AddChild(session);
        Current = session;
        try { initialize(session); }
        catch { Close(); throw; }
        Changed?.Invoke();
        return session;
    }

    public void Close()
    {
        var previous = Current;
        Current = null;
        if (previous is null) return;
        previous.Dispose();
        Changed?.Invoke();
    }

    public override void _ExitTree()
    {
        Close();
        Changed = null;
    }
}
