using Godot;

namespace Game.GodotClient;

internal sealed class EnvironmentMotion
{
    private float? _time;
    private bool _sampled;
    public Vector2 Offset { get; private set; }

    public Vector2 Update(float time, Vector2 velocity, bool sampled)
    {
        if (sampled || _time is null) Offset = velocity * time;
        else if (!_sampled) Offset += velocity * (time - _time.Value);
        _time = time;
        _sampled = sampled;
        return Offset;
    }
}
