using System.Buffers.Binary;
using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace Game.Core;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UnitTiming(int Initiative, int MoveTicks, int DeathTicks);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DefenseTiming(int WindupTicks, int RecoveryTicks)
{
    public int CadenceTicks => checked(WindupTicks + RecoveryTicks);
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CombatSettings
{
    [JsonRequired]
    public int RulesVersion { get; init; } = SnapshotCompatibility.CurrentSquadRulesVersion;
    public StatusRules Statuses { get; init; } = new();
    public HexBoardDefinition Board { get; init; } = HexBoardDefinition.Default();
    public UnitTiming Swordsman { get; init; } = new(10, 30, 48);
    public UnitTiming Berserker { get; init; } = new(20, 27, 48);
    public UnitTiming Crossbowman { get; init; } = new(30, 30, 48);
    public UnitTiming Mage { get; init; } = new(40, 30, 48);
    public int BerserkerWindupTicks { get; init; } = 18;
    public int BerserkerRecoveryTicks { get; init; } = 54;
    public int MageWindupTicks { get; init; } = 24;
    public int MageRecoveryTicks { get; init; } = 66;
    public int MageHexRange { get; init; } = 3;
    public DefenseTiming Defender { get; init; } = new(12, 48);
    public DefenseTiming ArrowTower { get; init; } = new(12, 48);
    public DefenseTiming RapidArrowTower { get; init; } = new(6, 24);
    public DefenseTiming CatapultTower { get; init; } = new(30, 90);
    public DefenseTiming BombardTower { get; init; } = new(30, 90);
    public int ArrowLevelOneDamage { get; init; } = 5;
    public int ArrowLevelTwoDamage { get; init; } = 7;
    public int CatapultLevelOneDamage { get; init; } = 6;
    public int CatapultLevelTwoDamage { get; init; } = 8;
    public int RetryTicks { get; init; } = 6;
    public int NoHealthProgressTicks { get; init; } = 3600;
    public int MaximumWaveTicks { get; init; } = 18000;
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record HexCombatProfile(UnitType Type, int Health, int Damage, int Initiative,
    int HexRange, int MoveTicks, int WindupTicks, int RecoveryTicks, int DeathTicks)
{
    public int CadenceTicks => checked(WindupTicks + RecoveryTicks);
    public int SecondaryDamage { get; init; }
    public int StructureDamage { get; init; }
    public HexCombatProfile Researched(int rank) => this with
    {
        Health = HealthPoints.Ranked(Health, rank),
        Damage = HealthPoints.Ranked(Damage, rank),
        SecondaryDamage = HealthPoints.Ranked(SecondaryDamage, rank)
    };
    internal WeaponProfile Runtime() => new(Health, Damage, HexRange, MoveTicks, WindupTicks, CadenceTicks)
    {
        SecondaryDamage = SecondaryDamage,
        StructureDamage = StructureDamage,
        Initiative = Initiative,
        DeathTicks = DeathTicks
    };
}
public readonly record struct CombatFingerprint(ulong A, ulong B, ulong C, ulong D)
{
    public override string ToString() => $"{A:x16}{B:x16}{C:x16}{D:x16}";
}

// One authority freezes its rules once. Neither authoring arrays nor returned
// definitions can mutate the board/profiles used by an ongoing fight.
public sealed class CombatConfiguration
{
    private WorkCounters? _work;
    internal WorkCounters? Work
    {
        get => _work;
        set { _work = value; value?.Support(WorkMetric.ProfileResolutions, WorkMetric.ProfileCacheMisses, WorkMetric.ProfileCacheHits); }
    }
    private readonly Dictionary<(UnitType Type, int Rank, bool Boss, int Level, UnitCapabilities Capabilities, Faction Faction, int PrimaryBonus, int SecondaryBonus), HexCombatProfile> _resolved = [];
    internal int CachedProfiles => _resolved.Count;
    internal void ClearCache() { _resolved.Clear(); Work = null; }
    private readonly FrozenDictionary<UnitType, HexCombatProfile> _profiles;
    private readonly FrozenDictionary<(Building Type, int Level), DefenseProfile> _towerProfiles;
    internal DefenseProfile BuiltInDefense { get; }
    internal DefenseProfile Tower(Building type, int level) => _towerProfiles[(type, level)];
    public StatusRules Statuses { get; }
    public ArenaSettings Arena { get; }
    public int RulesVersion { get; }
    public HexBoard Board { get; }
    public ReadOnlyCollection<HexCombatProfile> Units { get; }
    public DefenseTiming Defender { get; }
    public ReadOnlyCollection<TowerDefinition> Towers { get; }
    public int DefenderDamage { get; }
    public int EnemyPowerPercent { get; }
    public int RetryTicks { get; }
    public int NoHealthProgressTicks { get; }
    public int MaximumWaveTicks { get; }
    public CombatFingerprint Fingerprint { get; }
    // Keep the landed base-actor decision isolation: editing optional upgrade
    // timing must not reroll unchanged squads/base weapons. Both namespaces now
    // contain only actual rules-v7 inputs; full compatibility covers all six.
    private readonly TowerDefinition[] _baseDecisionTowers;
    internal CombatFingerprint ActorDecisionFingerprint { get; }

    public CombatConfiguration(Rules rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        if (rules.EnemyPowerPercent is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(rules), "Enemy power must be 1..100 percent.");
        EnemyPowerPercent = rules.EnemyPowerPercent;
        CombatSettings settings = rules.Combat;
        ArgumentNullException.ThrowIfNull(settings);
        Arena = rules.Arena; ArgumentNullException.ThrowIfNull(Arena);
        if (Arena.SquadCapacity is < 1 or > 12 || Arena.WatchpostHealth <= 0 || Arena.RallyDurationTicks <= 0
            || Arena.RallyDamageReductionPercent is < 0 or > 100 || Arena.DefenderRange < 1 || Arena.ArrowRange < 1 || Arena.CatapultRange < 1
            || new[] { Arena.SwordsmanSecondaryDamage, Arena.BerserkerSecondaryDamage, Arena.CrossbowmanSecondaryDamage, Arena.MageSecondaryDamage,
                Arena.SwordsmanStructureDamage, Arena.BerserkerStructureDamage, Arena.CrossbowmanStructureDamage, Arena.MageStructureDamage, Arena.CatapultSecondaryDamage }.Any(d => d < 0))
            throw new ArgumentException("Invalid squad capacity, damage or arena objectives.", nameof(rules));
        _ = HealthPoints.FromWhole(Arena.WatchpostHealth);
        Statuses = settings.Statuses; ArgumentNullException.ThrowIfNull(Statuses); Statuses.Validate();
        if (settings.Board.Capacity != 1 || settings.Board.Id != "shared-watch-arena") throw new ArgumentException("Legacy individual combat board refused. Use the connected shared arena rules.", nameof(rules));
        Board = new(settings.Board); RulesVersion = settings.RulesVersion;
        RetryTicks = settings.RetryTicks; NoHealthProgressTicks = settings.NoHealthProgressTicks; MaximumWaveTicks = settings.MaximumWaveTicks;
        Defender = settings.Defender; DefenderDamage = HealthPoints.FromWhole(rules.DefenderDamage);
        if (RulesVersion != SnapshotCompatibility.CurrentSquadRulesVersion || RetryTicks <= 0 || NoHealthProgressTicks <= 0 || MaximumWaveTicks <= 0)
            throw new ArgumentException("Unsupported combat rules version or invalid retry/limit interval. Start a fresh rules-v7 match.", nameof(rules));
        Units = Array.AsReadOnly(Enum.GetValues<UnitType>().Select(type => Profile(rules, type)).ToArray());
        foreach (HexCombatProfile profile in Units)
        {
            if (profile.Health <= 0 || profile.Damage < 0 || profile.Initiative < 0 || profile.HexRange < 1
                || profile.MoveTicks <= 0 || profile.WindupTicks <= 0 || profile.RecoveryTicks <= 0 || profile.DeathTicks <= 0)
                throw new ArgumentException("Invalid combat damage, range or timing.", nameof(rules));
            _ = profile.Researched(2); _ = profile.CadenceTicks;
        }
        _profiles = Units.ToFrozenDictionary(p => p.Type);
        foreach (DefenseTiming timing in new[] { Defender, settings.ArrowTower, settings.RapidArrowTower, settings.CatapultTower, settings.BombardTower })
        {
            if (timing.WindupTicks <= 0 || timing.RecoveryTicks <= 0)
                throw new ArgumentException("Defense windup/recovery must be positive.", nameof(rules));
            _ = timing.CadenceTicks;
        }
        Towers = Array.AsReadOnly(TowerProfiles(rules));
        _baseDecisionTowers = Towers.Where(t => t.Type is Building.ArrowTower or Building.CatapultTower).ToArray();
        BuiltInDefense = new(DefenderDamage, Defender.WindupTicks, Defender.RecoveryTicks);
        _towerProfiles = Towers.ToFrozenDictionary(p => (p.Type, p.Level),
            p => new DefenseProfile(p.Damage, p.WindupTicks, checked(p.CadenceTicks - p.WindupTicks)));
        // Reject values that could overflow the shared same-tick accumulator
        // even if every deployed member plus all city/army defenses hit one victim.
        int maxActors = checked(SharedArena.SectorWidth * SharedArena.Depth * 4 * Arena.SquadCapacity + 4 * (ArmyConfiguration.TotalPlots + 1));
        if (Units.Any(p => p.Researched(2).Damage > int.MaxValue / maxActors)
            || Towers.Any(p => Math.Max(p.Damage, p.SecondaryDamage) > int.MaxValue / maxActors) || DefenderDamage > int.MaxValue / maxActors)
            throw new ArgumentException("Damage could overflow a simultaneous accumulator.", nameof(rules));
        long approachAllowance = (long)new SharedArena(Enumerable.Range(1, 4)).Board.Diameter * StatusPolicy.Duration(Units.Max(p => p.MoveTicks), 50)
            + Units.Max(p => (long)StatusPolicy.Duration(p.WindupTicks, 50) + StatusPolicy.Duration(p.RecoveryTicks, 50))
            + Units.Max(p => p.DeathTicks) + RetryTicks;
        if (NoHealthProgressTicks < approachAllowance)
            throw new ArgumentException("No-health-progress allowance is shorter than an ordinary approach/action/cleanup interval.", nameof(rules));
        foreach (UnitType type in Enum.GetValues<UnitType>())
            for (int level = 1; level <= 6; level++) _ = Unit(type, rank: 2, level: level);
        _ = Unit(UnitType.Swordsman, rank: 2, isBoss: true, level: 3);
        _ = Unit(UnitType.Swordsman, rank: 2, isBoss: true, level: 5);
        ActorDecisionFingerprint = ComputeFingerprint(actorNamespace: true);
        Fingerprint = ComputeFingerprint();
    }

    public HexCombatProfile Unit(UnitType type, int rank = 0, bool isBoss = false, int level = 1, UnitCapabilities capabilities = default, Faction faction = Faction.Adventurers, int primaryBonus = 0, int secondaryBonus = 0)
    {
        Work?.Add(WorkMetric.ProfileResolutions);
        capabilities.Validate();
        if (!Enum.IsDefined(faction)) throw new ArgumentOutOfRangeException(nameof(faction));
        var key = (type, rank, isBoss, level, capabilities, faction, primaryBonus, secondaryBonus);
        if (_resolved.TryGetValue(key, out HexCombatProfile? cached)) { Work?.Add(WorkMetric.ProfileCacheHits); return cached; }
        Work?.Add(WorkMetric.ProfileCacheMisses);
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
        HexCombatProfile original = _profiles[type];
        int health = Progression.ScaleWhole(original.Health / HealthPoints.Scale, level);
        int damage = Progression.ScaleWhole(original.Damage / HealthPoints.Scale, level);
        int secondary = Progression.ScaleWhole(original.SecondaryDamage / HealthPoints.Scale, level);
        int structural = Progression.ScaleWhole(original.StructureDamage / HealthPoints.Scale, level);
        if (isBoss)
        {
            health = checked(health * Progression.BossHealthMultiplier); damage = checked(damage * Progression.BossDamageMultiplier);
            secondary = checked(secondary * Progression.BossDamageMultiplier); structural = checked(structural * Progression.BossDamageMultiplier);
        }
        HexCombatProfile resolved = (original with
        {
            Health = HealthPoints.FromWhole(health),
            Damage = HealthPoints.FromWhole(damage),
            SecondaryDamage = HealthPoints.FromWhole(secondary),
            StructureDamage = HealthPoints.FromWhole(structural)
        }).Researched(rank);
        WeaponProfile modified = capabilities.Apply(resolved.Runtime(), primaryBonus, secondaryBonus);
        resolved = resolved with { Health = modified.Health, Damage = modified.Damage, SecondaryDamage = modified.SecondaryDamage, HexRange = modified.HexRange };
        // Resolve from the original leveled profile once, never from received HP.
        // Fixed-point stats retain quarter-HP precision at the default 75%.
        if (faction == Faction.Skeletons)
            resolved = resolved with
            {
                Health = Math.Max(1, EnemyScale(resolved.Health)),
                Damage = EnemyScale(resolved.Damage),
                SecondaryDamage = EnemyScale(resolved.SecondaryDamage),
                StructureDamage = EnemyScale(resolved.StructureDamage)
            };
        int maximumActors = checked(SharedArena.SectorWidth * SharedArena.Depth * 4 * Arena.SquadCapacity + 4 * (ArmyConfiguration.TotalPlots + 1));
        if (UnitCapabilities.Scale(UnitCapabilities.Scale(Math.Max(Math.Max(resolved.Damage, resolved.SecondaryDamage), resolved.StructureDamage), 10), 20) > int.MaxValue / maximumActors) throw new ArgumentException("Leveled damage could overflow the simultaneous accumulator.", nameof(level));
        _resolved.Add(key, resolved); return resolved;
    }
    private int EnemyScale(int amount) => checked((int)((long)amount * EnemyPowerPercent / 100));

    internal static HexCombatProfile Profile(Rules rules, UnitType type)
    {
        CombatSettings c = rules.Combat;
        UnitTiming timing = type switch
        { UnitType.Swordsman => c.Swordsman, UnitType.Berserker => c.Berserker, UnitType.Crossbowman => c.Crossbowman, UnitType.Mage => c.Mage, _ => throw new ArgumentOutOfRangeException(nameof(type)) };
        int health = type switch { UnitType.Swordsman => rules.SoldierHealth, UnitType.Berserker => rules.BerserkerHealth, UnitType.Crossbowman => rules.RangedHealth, _ => rules.MageHealth };
        int damage = type switch { UnitType.Swordsman => rules.SoldierDamage, UnitType.Berserker => rules.BerserkerDamage, UnitType.Crossbowman => rules.RangedDamage, _ => rules.MageDamage };
        if (!double.IsFinite(rules.RangedReach) || rules.RangedReach < 1 || rules.RangedReach > int.MaxValue || rules.RangedReach != Math.Truncate(rules.RangedReach))
            throw new ArgumentException("Ranged reach must be an integer hex distance.", nameof(rules));
        int range = type switch { UnitType.Swordsman or UnitType.Berserker => 1, UnitType.Crossbowman => (int)rules.RangedReach, _ => c.MageHexRange };
        int windup = type switch { UnitType.Swordsman => rules.MeleeWindupTicks, UnitType.Berserker => c.BerserkerWindupTicks, UnitType.Crossbowman => rules.RangedWindupTicks, _ => c.MageWindupTicks };
        int recovery = type switch { UnitType.Swordsman or UnitType.Crossbowman => checked(rules.AttackTicks - windup), UnitType.Berserker => c.BerserkerRecoveryTicks, _ => c.MageRecoveryTicks };
        return new(type, HealthPoints.FromWhole(health), HealthPoints.FromWhole(damage), timing.Initiative, range,
            timing.MoveTicks, windup, recovery, timing.DeathTicks)
        {
            SecondaryDamage = HealthPoints.FromWhole(type switch
            {
                UnitType.Swordsman => rules.Arena.SwordsmanSecondaryDamage,
                UnitType.Berserker => rules.Arena.BerserkerSecondaryDamage,
                UnitType.Crossbowman => rules.Arena.CrossbowmanSecondaryDamage,
                _ => rules.Arena.MageSecondaryDamage
            }),
            StructureDamage = HealthPoints.FromWhole(type switch
            {
                UnitType.Swordsman => rules.Arena.SwordsmanStructureDamage,
                UnitType.Berserker => rules.Arena.BerserkerStructureDamage,
                UnitType.Crossbowman => rules.Arena.CrossbowmanStructureDamage,
                _ => rules.Arena.MageStructureDamage
            })
        };
    }
    internal static TowerDefinition[] TowerProfiles(Rules rules)
    {
        CombatSettings c = rules.Combat;
        TowerDefinition Tower(Building type, int level, int damage, DefenseTiming timing) => new(type, level, HealthPoints.FromWhole(damage),
            timing.WindupTicks, timing.CadenceTicks)
        {
            SecondaryDamage = type is Building.CatapultTower or Building.BombardTower ? HealthPoints.FromWhole(rules.Arena.CatapultSecondaryDamage) : 0,
            HexRange = type is Building.CatapultTower or Building.BombardTower ? rules.Arena.CatapultRange : rules.Arena.ArrowRange
        };
        return [Tower(Building.ArrowTower, 1, c.ArrowLevelOneDamage, c.ArrowTower),
            Tower(Building.ArrowTower, 2, c.ArrowLevelTwoDamage, c.ArrowTower),
            Tower(Building.CatapultTower, 1, c.CatapultLevelOneDamage, c.CatapultTower), Tower(Building.CatapultTower, 2, c.CatapultLevelTwoDamage, c.CatapultTower),
            Tower(Building.RapidArrowTower, 2, c.ArrowLevelTwoDamage, c.RapidArrowTower),
            Tower(Building.BombardTower, 2, c.CatapultLevelTwoDamage, c.BombardTower)];
    }
    private CombatFingerprint ComputeFingerprint(bool actorNamespace = false)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            // BinaryWriter integers are little endian; fields/collections have
            // fixed order, explicit lengths, and no runtime object hashes.
            new ResearchCatalog().Write(writer, prices: false); Statuses.Write(writer);
            writer.Write(RulesVersion);
            // Balance changes compatibility, not seeded actor-choice namespaces.
            if (!actorNamespace) writer.Write(EnemyPowerPercent);
            writer.Write(SharedArena.SectorWidth); writer.Write(SharedArena.Depth); writer.Write(2); // squad arena plus optional army-plot weapons
            writer.Write(Arena.SquadCapacity); writer.Write(Arena.WatchpostHealth); writer.Write(Arena.RallyDamageReductionPercent); writer.Write(Arena.RallyDurationTicks);
            writer.Write(Arena.DefenderRange); writer.Write(Arena.ArrowRange); writer.Write(Arena.CatapultRange);
            writer.Write(Arena.SwordsmanSecondaryDamage); writer.Write(Arena.BerserkerSecondaryDamage); writer.Write(Arena.CrossbowmanSecondaryDamage); writer.Write(Arena.MageSecondaryDamage);
            writer.Write(Arena.SwordsmanStructureDamage); writer.Write(Arena.BerserkerStructureDamage); writer.Write(Arena.CrossbowmanStructureDamage); writer.Write(Arena.MageStructureDamage); writer.Write(Arena.CatapultSecondaryDamage);
            writer.Write(Progression.Growth); writer.Write(Progression.MaximumExponentLevel);
            writer.Write(Progression.BossHealthMultiplier); writer.Write(Progression.BossDamageMultiplier); writer.Write(1); // One squad per logical hex, including single-member bosses.
            writer.Write(Board.Id); writer.Write(Board.Version); writer.Write(Board.Capacity);
            writer.Write(Board.Cells.Count);
            foreach (HexCell cell in Board.Cells)
            {
                writer.Write(cell.Id); writer.Write(cell.Coordinate.Q); writer.Write(cell.Coordinate.R); writer.Write((int)cell.Affinity);
                writer.Write(cell.Neighbors.Count); foreach (int n in cell.Neighbors) writer.Write(n);
            }
            writer.Write(Board.Anchors.Count);
            foreach (HexAnchor f in Board.Anchors)
            { writer.Write(f.Id); writer.Write(f.AnchorX); writer.Write(f.AnchorForward); }
            foreach (Faction faction in Enum.GetValues<Faction>())
            {
                writer.Write((int)faction); writer.Write(Board.Rear(faction).Count); foreach (int c in Board.Rear(faction)) writer.Write(c);
                writer.Write(Board.Front(faction).Count); foreach (int c in Board.Front(faction)) writer.Write(c);
            }
            writer.Write(Board.SiegeCells.Count); foreach (int c in Board.SiegeCells) writer.Write(c);
            writer.Write(Board.Transitions.Count);
            foreach (HexTransition t in Board.Transitions)
            { writer.Write(t.Id); writer.Write(t.Source.Cell); writer.Write(t.Source.Anchor); writer.Write(t.Destination.Cell); writer.Write(t.Destination.Anchor); writer.Write(t.EdgeToken); }
            writer.Write(Units.Count);
            foreach (HexCombatProfile p in Units)
            {
                writer.Write((int)p.Type); writer.Write(p.Health); writer.Write(p.Damage); writer.Write(p.SecondaryDamage); writer.Write(p.StructureDamage); writer.Write(p.Initiative);
                writer.Write(p.HexRange); writer.Write(p.MoveTicks); writer.Write(p.WindupTicks); writer.Write(p.RecoveryTicks); writer.Write(p.DeathTicks);
            }
            writer.Write(DefenderDamage); writer.Write(Defender.WindupTicks); writer.Write(Defender.RecoveryTicks);
            TowerDefinition[] towerIdentity = actorNamespace ? _baseDecisionTowers : Towers.ToArray();
            writer.Write(towerIdentity.Length);
            foreach (TowerDefinition p in towerIdentity)
            { writer.Write((int)p.Type); writer.Write(p.Level); writer.Write(p.Damage); writer.Write(p.WindupTicks); writer.Write(p.CadenceTicks); writer.Write(p.SecondaryDamage); writer.Write(p.HexRange); }
            writer.Write(RetryTicks); writer.Write(NoHealthProgressTicks); writer.Write(MaximumWaveTicks);
            if (!actorNamespace)
            {
                writer.Write(ActorDecisionFingerprint.A); writer.Write(ActorDecisionFingerprint.B);
                writer.Write(ActorDecisionFingerprint.C); writer.Write(ActorDecisionFingerprint.D);
            }
        }
        byte[] hash = SHA256.HashData(stream.ToArray());
        return new(BinaryPrimitives.ReadUInt64LittleEndian(hash), BinaryPrimitives.ReadUInt64LittleEndian(hash.AsSpan(8)),
            BinaryPrimitives.ReadUInt64LittleEndian(hash.AsSpan(16)), BinaryPrimitives.ReadUInt64LittleEndian(hash.AsSpan(24)));
    }
}
