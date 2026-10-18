using System.Numerics;
using System.Text.Json;
using Game.Core;
using Game.Shared;
using Xunit;

namespace Game.Shared.Tests;

public sealed class ArtTests
{
    [Fact]
    public void OptionsAreLocalStrictAndDebugByDefault()
    {
        var defaults = RenderOptions.Parse([]);
        Assert.Equal(RenderMode.Debug, defaults.Mode); Assert.Equal(HudArt.Plain, defaults.Hud); Assert.Equal(1, defaults.GroundCompression);
        var illustrated = RenderOptions.Parse(["--world-art", "sprites", "--hud-art", "illustrated", "--view", "angled", "--debug-overlay", "on"]);
        Assert.Equal(RenderMode.Sprites, illustrated.Mode); Assert.Equal(HudArt.Illustrated, illustrated.Hud); Assert.Equal(.72f, illustrated.GroundCompression); Assert.True(illustrated.DebugOverlay);
        foreach (string option in new[] { "--world-art", "--hud-art", "--view", "--debug-overlay" })
        {
            Assert.Throws<ArgumentException>(() => RenderOptions.Parse([option]));
            Assert.Throws<ArgumentException>(() => RenderOptions.Parse([option, "wrong"]));
        }
    }

    [Theory]
    [InlineData(.72)]
    [InlineData(1)]
    public void ProjectedCellsCornersAndInverseSelectionAgree(double compression)
    {
        var projection = new HexProjection(65, compression);
        foreach (var cell in SettlementLayout.Coordinates)
        {
            Vector2 ground = projection.Project(cell);
            Assert.Equal(cell, projection.HitTest(ground, SettlementLayout.Coordinates));
            Assert.Equal(cell, projection.Unproject(ground + new Vector2(4, -3)));
            Vector2[] corners = projection.Corners(cell);
            foreach (Vector2 corner in corners)
                Assert.Equal(cell, projection.HitTest(Vector2.Lerp(ground, corner, .98f), SettlementLayout.Coordinates));
            Assert.InRange(corners.Max(p => p.Y) - corners.Min(p => p.Y), (float)(130 * compression) - .001f, (float)(130 * compression) + .001f);
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => new HexProjection(65, 0));
    }

    [Fact]
    public void CatalogResolvesReplacementAndMissingAssetsWithoutCodeChanges()
    {
        WithCatalog((root, spriteRoot) =>
        {
            WriteCatalog(root, "sprites/buildings/version-two.png");
            var catalog = new AssetCatalog(spriteRoot);
            Assert.Null(catalog.Error);
            Assert.Null(catalog.Resolve("building/farm", true));
            string replacement = Path.Combine(root, "sprites/buildings/version-two.png");
            Directory.CreateDirectory(Path.GetDirectoryName(replacement)!); File.WriteAllText(replacement, "native client validates PNG");
            Assert.Equal(replacement, catalog.Resolve("building/farm", true));
            Assert.Null(catalog.Resolve("building/farm", false));
            Assert.Equal(.92f, catalog.Metadata("building/farm").Anchor[1]);
            File.Delete(replacement); Assert.Null(catalog.Resolve("building/farm", true));
            Assert.Empty(new AssetCatalog(spriteRoot, false).Entries);
        });
    }

    [Theory]
    [InlineData("../outside.png", 512, .5f)]
    [InlineData("sprites/buildings/farm.png", 0, .5f)]
    [InlineData("sprites/buildings/farm.png", 512, 2f)]
    public void InvalidCatalogFallsBack(string path, int width, float anchor)
    {
        WithCatalog((root, spriteRoot) =>
        {
            WriteCatalog(root, path, width, anchor);
            var catalog = new AssetCatalog(spriteRoot);
            Assert.NotNull(catalog.Error); Assert.Empty(catalog.Entries);
            Assert.Null(catalog.Resolve("building/farm", true));
        });
        Assert.False(new SliceMargins(48, 48, 48, 48).Valid(90, 200));
        Assert.False(new SliceMargins(-1, 1, 1, 1).Valid(100, 100));
    }

    [Fact]
    public void InstalledCatalogHasEveryRequiredIdAndValidHudMargins()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "CommonWatch.slnx"))) dir = dir.Parent;
        Assert.NotNull(dir);
        var catalog = new AssetCatalog(Path.Combine(dir!.FullName, "assets/sprites"));
        Assert.Null(catalog.Error); Assert.Equal(29, catalog.Entries.Count);
        Assert.Equal(6, catalog.Metadata("terrain/woodland_hex").GroundUv!.Length);
        Assert.InRange(catalog.Metadata("building/arcanum").Anchor[1], .7f, .85f);
        foreach (var entry in catalog.Entries)
        {
            Assert.NotNull(catalog.Resolve(entry.Id, true));
            if (entry.Id.StartsWith("hud/")) Assert.True(entry.Slice!.Valid(entry.Size[0], entry.Size[1]));
        }
    }

    [Fact]
    public void InvalidTerrainUvFailsClosed()
    {
        WithCatalog((root, spriteRoot) =>
        {
            foreach (string uv in new[] { "[[0,0]]", "[[0,0],[1,0],[1,1],[0,1],[0,0],[-1,0]]", "[[0,0,1],[1,0],[1,1],[0,1],[0,0],[0,0]]" })
            {
                File.WriteAllText(Path.Combine(root, "catalog.json"), "{\"version\":1,\"assets\":[{\"id\":\"terrain/woodland_hex\",\"path\":\"sprites/terrain/woodland_hex.png\",\"size\":[256,256],\"ground_uv\":" + uv + "}]}");
                var catalog = new AssetCatalog(spriteRoot);
                Assert.NotNull(catalog.Error);
                Assert.Empty(catalog.Entries);
            }
        });
    }

    [Fact]
    public void ShrineCaptureEarnsResourcesThroughLegalProduction()
    {
        using var session = ScenarioRunner.CreateAt("sprite-settlement");
        Assert.Equal(Building.Arcanum, session.City.Slots[4].Type);
        Assert.Equal(4, session.SelectedSlot);
        Assert.Contains(session.City.Slots, s => s.Type == Building.Lumbermill);
        Assert.Contains(session.City.Slots, s => s.Type == Building.Barracks);
        Assert.True(session.Snapshot.Phase is Phase.Preparation or Phase.Building);
    }

    [Fact]
    public void AllArtSettingsPreserveTheAuthorityResult()
    {
        string Play(string[] args)
        {
            var options = RenderOptions.Parse(args);
            using var session = ScenarioRunner.CreateAt("combat");
            var projection = new HexProjection(65, options.GroundCompression);
            while (session.Snapshot.Phase == Phase.Combat)
            {
                foreach (var unit in session.Playback.Units()) _ = projection.Project(new HexCoordinate(0, 0));
                session.Step();
            }
            session.ChooseReward(session.City.Run.Offers[0]);
            return JsonSerializer.Serialize(session.Snapshot, WireJson.Options);
        }
        Assert.Equal(Play([]), Play(["--world-art", "sprites", "--hud-art", "illustrated", "--view", "angled", "--debug-overlay", "on"]));
    }

    static void WithCatalog(Action<string, string> action)
    {
        string root = Path.Combine(Path.GetTempPath(), "watch-art-" + Guid.NewGuid().ToString("N"));
        string sprites = Path.Combine(root, "sprites"); Directory.CreateDirectory(sprites);
        try { action(root, sprites); } finally { Directory.Delete(root, true); }
    }
    static void WriteCatalog(string root, string path, int width = 512, float anchor = .5f)
    {
        var data = new { version = 1, assets = new[] { new { id = "building/farm", path, size = new[] { width, 512 }, anchor = new[] { anchor, .92f }, display_size = 142 } } };
        File.WriteAllText(Path.Combine(root, "catalog.json"), JsonSerializer.Serialize(data));
    }
}
