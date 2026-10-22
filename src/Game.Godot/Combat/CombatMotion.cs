using System.Numerics;
using Game.Core;

namespace Game.GodotClient;

public readonly record struct FigurePose(Vector2 Offset, float Rotation, float Scale, float Opacity, float Flash);

// Godot samples transforms from playback ticks; no Tween owns combat lifetime.
public sealed class CombatMotion
{
    readonly List<CombatEvent> events = [];
    public IReadOnlyList<CombatEvent> Events => events;
    public int ObservedHits { get; private set; }
    long cursor;
    public void Clear() { events.Clear(); cursor = 0; ObservedHits = 0; }
    public void Observe(CombatEvent entry)
    {
        if (entry.Sequence <= cursor) return;
        cursor = entry.Sequence;
        if (entry.Type == CombatEventType.Hit && entry.Damage > 0) ObservedHits++;
        events.Add(entry);
        if (events.Count > 4096) events.RemoveAt(0);
    }
    public void Trim(double tick) => events.RemoveAll(e => tick - e.Tick > 60);
    public FigurePose Sample(UnitState unit, double tick, Vector2 direction)
    {
        float phase = (float)(tick / 60 * 2.8 + unit.Id * 1.618);
        Vector2 offset = new(0, MathF.Sin(phase) * .55f);
        float rotation = MathF.Sin(phase * .5f) * .012f, scale = 1 + MathF.Sin(phase) * .006f;
        if (direction.LengthSquared() > .0001f) direction = Vector2.Normalize(direction);
        if (unit.Hex is { Action: UnitActionKind.Moving } move && tick >= move.StartTick && tick < move.EndTick)
        {
            float step = (float)((tick - move.StartTick) / Math.Max(1, move.EndTick - move.StartTick));
            offset.Y -= MathF.Sin(step * MathF.PI) * 1.8f;
            rotation += direction.X * .035f * MathF.Sin(step * MathF.PI);
        }
        if (unit.Hex?.Action is UnitActionKind.Windup or UnitActionKind.Recovery && unit.AttackSequence > 0 && tick >= unit.ActionStartTick && tick < unit.ReadyTick)
        {
            float before = (float)Math.Clamp((tick - unit.ActionStartTick) / Math.Max(1, unit.ImpactTick - unit.ActionStartTick), 0, 1);
            float recovery = (float)Math.Clamp((tick - unit.ImpactTick) / Math.Max(1, unit.ReadyTick - unit.ImpactTick), 0, 1);
            float reach = tick < unit.ImpactTick ? before < .6f ? -2.5f * Smooth(before / .6f) : -2.5f + 10.5f * Smooth((before - .6f) / .4f) : 8 * (1 - Smooth(Math.Min(1, recovery * 1.7f)));
            if (unit.Type is UnitType.Crossbowman or UnitType.Mage) reach *= -.27f;
            if (unit.IsBoss) reach *= 1.25f;
            offset += direction * reach;
            rotation += direction.X * reach * .009f;
        }
        float flash = 0;
        foreach (var hit in events)
            if (hit.Type == CombatEventType.Hit && hit.Damage > 0 && hit.Unit?.Id == unit.Id && tick >= hit.Tick)
            {
                float amount = Math.Clamp(1 - (float)(tick - hit.Tick) / 11, 0, 1);
                flash = Math.Max(flash, amount);
                offset.X += MathF.Sin((float)(tick - hit.Tick) * 2.2f) * amount * 1.2f;
            }
        float opacity = 1;
        if (unit.Hex is { Lifecycle: UnitLifecycle.Dying })
        {
            float death = (float)CombatPlayback.DeathPose(unit, tick, 1);
            rotation = .95f * Smooth(death); offset = new(death * 3, death * 4);
            opacity = 1 - Smooth(Math.Clamp((death - .15f) / .85f, 0, 1)); scale = 1 - death * .08f;
        }
        return new(offset, rotation, scale * (unit.IsBoss ? 1.38f : 1), opacity, flash);
    }
    static float Smooth(float x) => x * x * (3 - 2 * x);
}
