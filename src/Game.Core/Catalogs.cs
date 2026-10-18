using System.Globalization;

namespace Game.Core;

// Health and damage on the wire are integer hundredths; resources remain whole units.
public static class HealthPoints
{
    public const int Scale = 100;
    public static int FromWhole(int value) => value < 0 ? throw new ArgumentOutOfRangeException(nameof(value)) : checked(value * Scale);
    public static int Ranked(int baseValue, int rank)
    {
        if (baseValue < 0 || rank is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(rank));
        return checked((int)(checked((long)baseValue * (100 + 5 * rank)) / 100));
    }
    public static string Format(int value) => (value / (decimal)Scale).ToString("0.##", CultureInfo.InvariantCulture);
}
public enum Faction { Adventurers, Skeletons }
public enum UnitClass { Melee, Ranged, Magic }
public sealed record BuildingDefinition(Building Type, ResourceCost Construction, ResourceCost Upgrade, int LevelOneOutput = 0, int LevelTwoOutput = 0, UnitType[]? Recruits = null, int MaximumLevel = 2, Resource? Produces = null, ResourceCost? RecoveryConstruction = null)
{
    public int Output(int level) => level switch { 1 => LevelOneOutput, 2 => LevelTwoOutput, _ => 0 };
}
[System.Text.Json.Serialization.JsonUnmappedMemberHandling(System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow)]
public sealed record TowerDefinition(Building Type, int Level, int Damage, int WindupTicks, int CadenceTicks)
{
    public int SecondaryDamage { get; init; }
    public int HexRange { get; init; }
}
public sealed record UnitDefinition(UnitType Type, UnitClass Class, ResourceCost Recruitment, WeaponProfile Profile, int Upkeep = 1);
public static class Catalogs
{
    public static BuildingDefinition[] Buildings(Rules rules) =>
    [
        new(Building.Farm, new(Wood: 2), new(Wood: 2, Stone: 2), rules.FarmOutput, rules.FarmOutputLevelTwo, Produces: Resource.Food),
        new(Building.Mine, new(Wood: 2), new(Wood: 2, Stone: 2), rules.MineOutput, rules.MineOutputLevelTwo, Produces: Resource.Gold),
        new(Building.Lumbermill, new(Wood: 1), new(Wood: 2, Stone: 2), rules.WoodOutput, rules.WoodOutputLevelTwo, Produces: Resource.Wood, RecoveryConstruction: new(4)),
        new(Building.Barracks, new(Wood: 2), new(Wood: 2), Recruits: [UnitType.Swordsman, UnitType.Berserker], MaximumLevel: 5),
        new(Building.ArcheryRange, new(Wood: 2), new(Wood: 2), Recruits: [UnitType.Crossbowman], MaximumLevel: 5),
        new(Building.Arcanum, new(5, 2, Stone: 3), new(rules.UpgradeCost, 2), Recruits: [UnitType.Mage], MaximumLevel: 5),
        new(Building.ResearchTower, new(Wood: 2, Stone: 2), new(rules.UpgradeCost, 2, Stone: 2)),
        new(Building.ArrowTower, new(rules.BuildCost, 3), new(rules.UpgradeCost, 2, Stone: 2)),
        new(Building.CatapultTower, new(6, 4, Stone: 3, Metal: 2), new(rules.UpgradeCost, 2, Stone: 2, Metal: 1)),
        new(Building.Stonecutter, new(Wood: 2), new(Wood: 2, Stone: 2), rules.StoneOutput, rules.StoneOutputLevelTwo, Produces: Resource.Stone),
        new(Building.MetalMine, new(Wood: 2), new(Wood: 2, Stone: 2), rules.MetalOutput, rules.MetalOutputLevelTwo, Produces: Resource.Metal),
        new(Building.Weaver, new(Wood: 2), new(Wood: 2, Stone: 2), rules.ClothOutput, rules.ClothOutputLevelTwo, Produces: Resource.Cloth),
        new(Building.Market, new(Wood: 2, Stone: 2), default, MaximumLevel: 1),
        new(Building.TownHall, new(5, 2, Stone: 2), default, MaximumLevel: 1)
    ];
    public static TowerDefinition[] Towers(Rules? rules = null) => CombatConfiguration.TowerProfiles(rules ?? new());
    public static UnitDefinition[] Units(Rules rules) =>
    [
        new(UnitType.Swordsman, UnitClass.Melee, new(Metal: rules.SwordMetalCost), CombatConfiguration.Profile(rules, UnitType.Swordsman).Runtime(), rules.SwordUpkeep),
        new(UnitType.Berserker, UnitClass.Melee, new(Metal: rules.BerserkerMetalCost), CombatConfiguration.Profile(rules, UnitType.Berserker).Runtime(), rules.BerserkerUpkeep),
        new(UnitType.Crossbowman, UnitClass.Ranged, new(Wood: rules.CrossbowWoodCost, Metal: rules.CrossbowMetalCost), CombatConfiguration.Profile(rules, UnitType.Crossbowman).Runtime(), rules.CrossbowUpkeep),
        new(UnitType.Mage, UnitClass.Magic, new(rules.MageGoldCost, Cloth: rules.MageClothCost), CombatConfiguration.Profile(rules, UnitType.Mage).Runtime(), rules.MageUpkeep)
    ];
    public static UnitClass Class(UnitType type) => type switch { UnitType.Swordsman or UnitType.Berserker => UnitClass.Melee, UnitType.Crossbowman => UnitClass.Ranged, UnitType.Mage => UnitClass.Magic, _ => throw new ArgumentOutOfRangeException(nameof(type)) };

}
public sealed record TowerState(int City, int Slot, Building Type, int Level, long AttackSequence = 0, int TargetId = 0, long ActionStartTick = 0, long ImpactTick = 0, long ReadyTick = 0, bool PendingImpact = false);
