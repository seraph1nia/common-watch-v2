// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using Xunit;

namespace Game.Shared.Tests;

public sealed class VisualPresentationTests
{
    [Fact]
    public void LegalEnemyBerserkerUsesRegisteredApprovedOrcCrop()
    {
        using var solo = ScenarioRunner.CreateAt("post-draft");
        var enemy = Assert.Single(solo.Snapshot.NextWave, u => u.Type == UnitType.Berserker);
        var catalog = Catalog();
        string id = VisualAssets.UnitId(enemy);
        Assert.Equal("unit/enemy_berserker", id);
        Assert.NotNull(catalog.Resolve(id, true));
        Assert.Equal("sprites/units/enemy_berserker.png", catalog.Metadata(id).Path);
        Assert.NotEqual(catalog.Metadata("unit/berserker").Path, catalog.Metadata(id).Path);
        long revision = solo.Snapshot.Revision;
        Assert.Null(VisualAssets.Resolve("unused", id, RenderMode.Debug, _ => throw new InvalidOperationException("debug decoded art")));
        Assert.Equal(revision, solo.Snapshot.Revision);
    }
    [Fact]
    public void EveryStructureHasAnExplicitExistingApprovedMapping()
    {
        var catalog = Catalog();
        foreach (var kind in Enum.GetValues<ArenaStructureKind>())
            Assert.NotNull(catalog.Resolve(VisualAssets.StructureId(kind), true));
        Assert.Equal("arena/defense_tower", VisualAssets.StructureId(ArenaStructureKind.Watchpost));
        Assert.Equal(VisualAssets.StructureId(ArenaStructureKind.ArrowTower), VisualAssets.StructureId(ArenaStructureKind.Watchpost));
    }
    private static AssetCatalog Catalog()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(root.FullName, "CommonWatch.slnx"))) root = root.Parent ?? throw new DirectoryNotFoundException("Repository root");
        var catalog = new AssetCatalog(Path.Combine(root.FullName, "assets/sprites"));
        Assert.Null(catalog.Error); return catalog;
    }
}
