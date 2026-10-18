namespace Game.Core;

public enum StatusKind { Burn, Poison, Chill }
public readonly record struct StatusIdentity(long AppliedTick, int SourceId, long AttackSequence) : IComparable<StatusIdentity>
{
    public static bool operator <(StatusIdentity a, StatusIdentity b) => a.CompareTo(b) < 0;
    public static bool operator >(StatusIdentity a, StatusIdentity b) => a.CompareTo(b) > 0;
    public static bool operator <=(StatusIdentity a, StatusIdentity b) => a.CompareTo(b) <= 0;
    public static bool operator >=(StatusIdentity a, StatusIdentity b) => a.CompareTo(b) >= 0;
    public int CompareTo(StatusIdentity other)
    { int c = AppliedTick.CompareTo(other.AppliedTick); if (c == 0) c = SourceId.CompareTo(other.SourceId); return c == 0 ? AttackSequence.CompareTo(other.AttackSequence) : c; }
}
public sealed record StatusEffect(StatusIdentity Identity, int Strength, long AppliedTick, long ExpiresTick, long NextDamageTick)
{
    public int PotencySourceId { get; init; } = Identity.SourceId;
    public long PotencyAttackSequence { get; init; } = Identity.AttackSequence;
}
public sealed record StatusState
{
    public StatusEffect? Burn { get; init; }
    public StatusEffect[] Poison { get; init; } = [];
    public StatusEffect? Chill { get; init; }
    public static StatusState Empty { get; } = new();
    public bool Equals(StatusState? other) => other is not null && Burn == other.Burn && Chill == other.Chill && Poison.SequenceEqual(other.Poison);
    public override int GetHashCode() { var hash = new HashCode(); hash.Add(Burn); hash.Add(Chill); foreach (StatusEffect effect in Poison) hash.Add(effect); return hash.ToHashCode(); }
    public bool Active => Burn is not null || Poison.Length > 0 || Chill is not null;
    public StatusState Detach() => this with { Poison = Poison.ToArray() };
    public int ChillAt(long tick) => Chill is { } c && tick < c.ExpiresTick ? c.Strength : 0;
}
public sealed record StatusRules
{
    public int BurnPeriod { get; init; } = 60;
    public int BurnDuration { get; init; } = 180;
    public int PoisonPeriod { get; init; } = 60;
    public int PoisonDuration { get; init; } = 360;
    public int PoisonCap { get; init; } = 3;
    public int ChillDuration { get; init; } = 180;
    public int MaximumChillPercent { get; init; } = 50;
    public void Validate()
    {
        if (BurnPeriod < 1 || BurnDuration < BurnPeriod || PoisonPeriod < 1 || PoisonDuration <= BurnDuration || PoisonDuration < PoisonPeriod
            || PoisonCap != 3 || ChillDuration < 1 || MaximumChillPercent is < 40 or > 50)
            throw new ArgumentException("Unsafe or unbounded status timing.");
    }
    internal void Write(BinaryWriter w)
    { w.Write(BurnPeriod); w.Write(BurnDuration); w.Write(PoisonPeriod); w.Write(PoisonDuration); w.Write(PoisonCap); w.Write(ChillDuration); w.Write(MaximumChillPercent); }
}
public sealed record StatusApplication(int VictimId, StatusKind Kind, int SourceId, long AttackSequence, long Tick, int Strength);
public sealed record PeriodicContribution(StatusKind Kind, StatusIdentity Identity, int Damage)
{
    public int SourceId { get; init; } = Identity.SourceId;
}
public static class StatusPolicy
{
    public static int Potency(int damage, int percent)
    { ArgumentOutOfRangeException.ThrowIfNegative(damage); ArgumentOutOfRangeException.ThrowIfNegative(percent); return RunEffects.Portion(damage, percent); }
    public static int Duration(int ticks, int chillPercent)
    { ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ticks); if (chillPercent is < 0 or > 50) throw new ArgumentOutOfRangeException(nameof(chillPercent)); return UnitCapabilities.Scale(ticks, chillPercent); }
    public static void Validate(StatusState state, StatusRules rules)
    {
        rules.Validate();
        if (state.Poison.Length > rules.PoisonCap || state.Poison.Select(p => p.Identity).Distinct().Count() != state.Poison.Length
            || !state.Poison.SequenceEqual(state.Poison.OrderBy(p => p.Identity))) throw new ArgumentException("Invalid poison stack ordering/cap.");
        foreach (var (effect, kind) in state.Poison.Select(p => ((StatusEffect?)p, StatusKind.Poison)).Concat([(state.Burn, StatusKind.Burn), (state.Chill, StatusKind.Chill)]))
        {
            if (effect is null) continue;
            if (effect.Strength <= 0 || effect.AppliedTick < 0 || effect.Identity.AppliedTick != effect.AppliedTick || effect.Identity.SourceId <= 0
                || effect.Identity.AttackSequence <= 0 || effect.PotencySourceId <= 0 || effect.PotencyAttackSequence <= 0 || effect.ExpiresTick <= effect.AppliedTick
                || kind != StatusKind.Chill && effect.NextDamageTick <= effect.AppliedTick
                || kind == StatusKind.Chill && (effect.NextDamageTick != 0 || effect.Strength > rules.MaximumChillPercent)) throw new ArgumentException("Invalid status restoration state.");
        }
    }
    public static StatusState Apply(StatusState state, StatusApplication application, StatusRules rules)
    {
        if (!Enum.IsDefined(application.Kind) || application.SourceId <= 0 || application.AttackSequence <= 0 || application.Tick < 0 || application.Strength < 0)
            throw new ArgumentException("Invalid status application.");
        if (application.Strength == 0) return state;
        int duration = application.Kind switch { StatusKind.Burn => rules.BurnDuration, StatusKind.Poison => rules.PoisonDuration, _ => rules.ChillDuration };
        int period = application.Kind switch { StatusKind.Burn => rules.BurnPeriod, StatusKind.Poison => rules.PoisonPeriod, _ => 0 };
        var effect = new StatusEffect(new(application.Tick, application.SourceId, application.AttackSequence), application.Strength, application.Tick,
            checked(application.Tick + duration), period == 0 ? 0 : checked(application.Tick + period));
        StatusEffect Refresh(StatusEffect existing) => existing with { Strength = Math.Max(existing.Strength, effect.Strength), ExpiresTick = Math.Max(existing.ExpiresTick, effect.ExpiresTick), PotencySourceId = effect.Strength > existing.Strength ? effect.PotencySourceId : existing.PotencySourceId, PotencyAttackSequence = effect.Strength > existing.Strength ? effect.PotencyAttackSequence : existing.PotencyAttackSequence };
        switch (application.Kind)
        {
            case StatusKind.Burn: return state with { Burn = state.Burn is { } b ? Refresh(b) : effect };
            case StatusKind.Chill:
                if (effect.Strength > rules.MaximumChillPercent) throw new ArgumentException("Chill exceeds its bound.");
                return state with { Chill = state.Chill is { } c ? Refresh(c) : effect };
            default:
                if (state.Poison.Any(p => p.Identity == effect.Identity)) return state;
                StatusEffect[] poison = state.Poison.ToArray();
                if (poison.Length < rules.PoisonCap) poison = [.. poison, effect];
                else { StatusEffect first = poison.OrderBy(p => p.ExpiresTick).ThenBy(p => p.Identity).First(); poison[Array.IndexOf(poison, first)] = Refresh(first); }
                return state with { Poison = poison.OrderBy(p => p.Identity).ToArray() };
        }
    }
    public static (StatusState State, PeriodicContribution[] Damage) Advance(StatusState state, long tick, StatusRules rules)
    {
        var damage = new List<PeriodicContribution>(4);
        StatusEffect? AdvanceEffect(StatusEffect? effect, StatusKind kind, int period)
        {
            if (effect is null) return null;
            if (effect.NextDamageTick <= tick && effect.NextDamageTick <= effect.ExpiresTick)
            {
                damage.Add(new(kind, effect.Identity, effect.Strength) { SourceId = effect.PotencySourceId });
                effect = effect with { NextDamageTick = checked(effect.NextDamageTick + period) };
            }
            return tick >= effect.ExpiresTick ? null : effect;
        }
        StatusEffect? burn = AdvanceEffect(state.Burn, StatusKind.Burn, rules.BurnPeriod);
        StatusEffect[] poison = state.Poison.Select(p => AdvanceEffect(p, StatusKind.Poison, rules.PoisonPeriod)).OfType<StatusEffect>().ToArray();
        return (state with { Burn = burn, Poison = poison, Chill = state.Chill is { } c && tick < c.ExpiresTick ? c : null }, damage.ToArray());
    }
}
