using System.Collections.Frozen;

namespace Game.Core;

public sealed record SpawnEntry(UnitType Type, int Level, int Count = 1, int Rank = 0, bool IsBoss = false);
public sealed record WaveDefinition(int Number, bool IsBoss, SpawnEntry[] Entries, ResourceCost Reward)
{
    public int Allocation => Entries.Sum(e => e.Count);
}
public sealed record CampaignDefinition(WaveDefinition[] Waves)
{
    public static CampaignDefinition Default()
    {
        SpawnEntry S(int count, int level) => new(UnitType.Swordsman, level, count);
        SpawnEntry B(int count, int level) => new(UnitType.Berserker, level, count);
        SpawnEntry C(int count, int level) => new(UnitType.Crossbowman, level, count);
        SpawnEntry M(int count, int level) => new(UnitType.Mage, level, count);
        WaveDefinition Wave(int number, params SpawnEntry[] entries) => new(number, false, entries, new(2, 1, 5));
        WaveDefinition Boss(int number, int level) => new(number, true, [new(UnitType.Swordsman, level, IsBoss: true)], new(4, 2, 10));
        return new([
            Wave(1, S(4, 1)), Wave(2, S(3, 1), B(1, 1)), Wave(3, S(3, 1), C(2, 1)),
            Wave(4, S(3, 1), B(1, 1), C(1, 1), M(1, 1)), Wave(5, S(3, 2), C(1, 1)),
            Wave(6, S(3, 2), B(1, 2), C(1, 1)), Wave(7, S(3, 2), C(2, 2), M(1, 1)),
            Wave(8, S(3, 2), B(2, 2), C(2, 2), M(1, 2)), Wave(9, S(3, 3), B(1, 2), C(1, 2), M(1, 2)),
            Boss(10, 3), Wave(11, S(3, 3), C(2, 2)), Wave(12, S(3, 3), B(1, 3), C(2, 3)),
            Wave(13, S(3, 3), C(2, 3), M(1, 3)), Wave(14, S(3, 3), B(2, 3), C(2, 3), M(1, 3)),
            Wave(15, S(3, 4), C(1, 3), M(1, 3)), Wave(16, S(3, 4), B(1, 4), C(2, 4)),
            Wave(17, S(3, 4), C(2, 4), M(1, 4)), Wave(18, S(3, 4), B(2, 4), C(2, 4), M(1, 4)),
            Wave(19, S(3, 5), B(1, 4), C(2, 4), M(1, 4)), Boss(20, 5)
        ]);
    }
}

public sealed class CampaignConfiguration
{
    private readonly FrozenDictionary<int, WaveDefinition> _waves;
    private readonly WaveDefinition[] _catalog;
    private readonly int _squadCapacity;
    public int TotalWaves => _waves.Count;
    public CombatFingerprint Fingerprint { get; }
    public CampaignConfiguration(CampaignDefinition definition, CombatConfiguration combat)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (definition.Waves is null || definition.Waves.Length is < 1 or > 256) throw new ArgumentException("Invalid campaign length.", nameof(definition));
        long spawns = 0;
        foreach ((WaveDefinition wave, int index) in definition.Waves.Select((w, i) => (w, i)))
        {
            if (wave is null || wave.Number != index + 1 || wave.Entries is null || wave.Entries.Length == 0 || wave.Entries.Any(e => e is null) || !wave.Reward.IsValid
                || wave.Reward.Stone != 0 || wave.Reward.Metal != 0 || wave.Reward.Cloth != 0)
                throw new ArgumentException("Invalid ordered wave/reward definition.", nameof(definition));
            if (wave.IsBoss && (wave.Entries.Length != 1 || wave.Entries[0].Count != 1 || !wave.Entries[0].IsBoss)
                || !wave.IsBoss && wave.Entries.Any(e => e.IsBoss)) throw new ArgumentException("Boss waves must contain one boss without escorts.", nameof(definition));
            foreach (SpawnEntry entry in wave.Entries)
            {
                if (!Enum.IsDefined(entry.Type) || entry.Count < 1) throw new ArgumentException("Invalid spawn identity/count.", nameof(definition));
                _ = combat.Unit(entry.Type, entry.Rank, entry.IsBoss, entry.Level);
                spawns = checked(spawns + entry.Count);
            }
        }
        if (spawns > int.MaxValue / 4) throw new ArgumentException("Campaign spawn identities exceed roster bounds.", nameof(definition));
        _squadCapacity = combat.Arena.SquadCapacity;
        _waves = definition.Waves.Select(Copy).ToFrozenDictionary(w => w.Number);
        _catalog = _waves.Values.OrderBy(w => w.Number).ToArray();
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(1); writer.Write(TotalWaves);
            foreach (WaveDefinition wave in _waves.Values.OrderBy(w => w.Number))
            {
                writer.Write(wave.Number); writer.Write(wave.IsBoss); writer.Write(wave.Entries.Length);
                foreach (SpawnEntry entry in wave.Entries)
                { writer.Write((int)entry.Type); writer.Write(entry.Level); writer.Write(entry.Count); writer.Write(entry.Rank); writer.Write(entry.IsBoss); }
                foreach (Resource resource in Enum.GetValues<Resource>()) writer.Write(wave.Reward.Amount(resource));
            }
        }
        Fingerprint = RulesIdentity.Hash(stream.ToArray());
    }
    private static WaveDefinition Copy(WaveDefinition wave) => wave with { Entries = wave.Entries.ToArray() };
    public WaveDefinition Wave(int number) => Copy(_waves[number]);
    internal WaveDefinition WaveRule(int number) => _waves[number];
    internal bool HasRemainingSecondaryTargets(int clearedWave) => _squadCapacity > 1 && _catalog.Skip(clearedWave)
        .Any(w => w.Entries.Where(e => !e.IsBoss).GroupBy(e => e.Type).Any(g => g.Sum(e => (long)e.Count) > 1));
    public CampaignDefinition Definition() => new(_catalog.Select(Copy).ToArray());
}

public sealed record WaveClearReceipt(int Wave, bool IsBoss, ResourceCost Amount)
{ public int Research { get; init; } }
