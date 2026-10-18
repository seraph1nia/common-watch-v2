using System.Numerics;
using Game.Core;

namespace Game.MonoGameClient;

public readonly record struct FigurePose(Vector2 Offset, float Rotation, float Scale, float Opacity, float Flash);

// Explicit SpriteBatch pose sampling; all durations are in authoritative ticks.
public sealed class CombatMotion
{
    readonly List<CombatEvent> events = [];
    public IReadOnlyList<CombatEvent> Events => events;
    public int ObservedHits { get; private set; }
    long sequence;
    public void Clear() { events.Clear(); sequence = 0; ObservedHits = 0; }
    public void Observe(CombatEvent entry)
    {
        if (entry.Sequence <= sequence) return;
        sequence = entry.Sequence;
        if (entry.Type == CombatEventType.Hit && entry.Damage > 0) ObservedHits++;
        events.Add(entry);
        if (events.Count > 4096) events.RemoveAt(0);
    }
    public void Trim(double tick) => events.RemoveAll(e => tick - e.Tick > 60);
    public FigurePose Sample(UnitState unit, double tick, Vector2 aim)
    {
        float breathing = MathF.Sin((float)tick * .046f + unit.Id * 1.618f);
        Vector2 offset = new(0, breathing * .55f);
        float lean = breathing * .009f, scale = 1 + breathing * .006f, alpha = 1, flash = 0;
        if (aim.LengthSquared() > .0001f) aim = Vector2.Normalize(aim);
        if (unit.Hex is { Action: UnitActionKind.Moving } step && tick >= step.StartTick && tick < step.EndTick)
        {
            float p = (float)((tick - step.StartTick) / Math.Max(1, step.EndTick - step.StartTick));
            offset.Y -= 1.8f * MathF.Sin(p * MathF.PI); lean += aim.X * .035f * MathF.Sin(p * MathF.PI);
        }
        if (unit.Hex?.Action is UnitActionKind.Windup or UnitActionKind.Recovery && unit.AttackSequence > 0 && tick >= unit.ActionStartTick && tick < unit.ReadyTick)
        {
            float p = (float)Math.Clamp((tick - unit.ActionStartTick) / Math.Max(1, unit.ImpactTick - unit.ActionStartTick), 0, 1);
            float r = (float)Math.Clamp((tick - unit.ImpactTick) / Math.Max(1, unit.ReadyTick - unit.ImpactTick), 0, 1);
            float reach = tick < unit.ImpactTick ? p < .6f ? -2.5f * Ease(p / .6f) : -2.5f + 10.5f * Ease((p - .6f) / .4f) : 8 * (1 - Ease(Math.Min(1, r * 1.7f)));
            if (unit.Type is UnitType.Crossbowman or UnitType.Mage) reach *= -.27f;
            offset += aim * reach; lean += aim.X * reach * .009f;
        }
        foreach (CombatEvent hit in events)
            if (hit.Type == CombatEventType.Hit && hit.Damage > 0 && hit.Unit?.Id == unit.Id && tick >= hit.Tick)
            {
                float strength = Math.Clamp(1 - (float)(tick - hit.Tick) / 11, 0, 1);
                flash = Math.Max(flash, strength);
                offset.X += MathF.Sin((float)(tick - hit.Tick) * 2.2f) * strength * 1.2f;
            }
        if (unit.Hex is { Lifecycle: UnitLifecycle.Dying })
        {
            float fall = (float)CombatPlayback.DeathPose(unit, tick, 1);
            lean = .95f * Ease(fall); offset = new(fall * 3, fall * 4);
            alpha = 1 - Ease(Math.Clamp((fall - .15f) / .85f, 0, 1)); scale = 1 - fall * .08f;
        }
        return new(offset, lean, scale, alpha, flash);
    }
    static float Ease(float x) => x * x * (3 - 2 * x);
}
