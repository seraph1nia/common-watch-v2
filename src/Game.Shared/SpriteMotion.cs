using System.Numerics;
using Game.Core;

namespace Game.Shared;

public readonly record struct SpritePose(Vector2 Offset, float Opacity, float Flash);

/// <summary>Samples detached playback and drained authoritative events; never writes simulation state.</summary>
public sealed class SpriteMotion
{
    readonly Dictionary<int, (double At, int Target)> attacks = [];
    readonly Dictionary<int, double> hits = [];
    public void Clear() { attacks.Clear(); hits.Clear(); }
    public void Observe(CombatEvent entry, double seconds)
    {
        if (entry.Type == CombatEventType.AttackStarted && entry.Unit is { } attacker) attacks[attacker.Id] = (seconds, entry.TargetId);
        if (entry.Type == CombatEventType.Hit && entry.Unit is { } victim) hits[victim.Id] = seconds;
        foreach (int id in attacks.Where(p => seconds - p.Value.At > 1).Select(p => p.Key).ToArray()) attacks.Remove(id);
        foreach (int id in hits.Where(p => seconds - p.Value > 1).Select(p => p.Key).ToArray()) hits.Remove(id);
    }
    public SpritePose Sample(UnitState unit, double tick, double seconds, Vector2 ground, Vector2? target)
    {
        float opacity = unit.Hex?.Lifecycle == UnitLifecycle.Dying ? 1 - (float)CombatPlayback.DeathPose(unit, tick, 1) : 1;
        float bob = unit.Hex?.Lifecycle == UnitLifecycle.Dying ? 0 : MathF.Sin((float)seconds * 3 + unit.Id * 1.7f) * 1.1f;
        Vector2 offset = new(0, bob);
        if (attacks.TryGetValue(unit.Id, out var attack) && target is { } end)
        {
            float age = (float)(seconds - attack.At);
            Vector2 direction = end - ground;
            if (age is >= 0 and < .28f && direction.LengthSquared() > .001f)
                offset += Vector2.Normalize(direction) * MathF.Sin(age / .28f * MathF.PI) * 6;
        }
        float flash = hits.TryGetValue(unit.Id, out var hit) ? Math.Clamp(1 - (float)(seconds - hit) / .2f, 0, 1) : 0;
        return new(offset, opacity, flash);
    }
}
