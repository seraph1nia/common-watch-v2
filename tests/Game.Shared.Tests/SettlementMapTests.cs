using System.Numerics;
using System.Text.Json;
using Game.Shared;
using Xunit;

namespace Game.Shared.Tests;

public sealed class SettlementMapTests
{
    private static string LayoutPath
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CommonWatch.slnx"))) directory = directory.Parent;
            return Path.Combine(directory!.FullName, "assets/sprites/terrain/settlement_map.json");
        }
    }

    [Fact]
    public void MeasuredPadsPickIndependentlyAndExcludeEllipseCornersAndScenery()
    {
        var map = SettlementMapLayout.Read(LayoutPath);
        foreach (var spot in map.Spots)
        {
            Assert.Equal(spot.Slot, map.Pick(spot.BuildingAnchor.Vector));
            for (int i = 0; i < 32; i++)
            {
                float angle = i * MathF.Tau / 32;
                var offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * spot.Radii.Vector;
                Assert.Equal(spot.Slot, map.Pick(spot.Center.Vector + offset * .99f));
                Assert.False(spot.Contains(spot.Center.Vector + offset * 1.01f));
            }
            Assert.False(spot.Contains(spot.Center.Vector + spot.Radii.Vector * .8f));
        }
        // Painted shrine, forge, market, bridges and outside the image are scenery.
        foreach (var point in new[] { new Vector2(580, 240), new Vector2(975, 330), new Vector2(1340, 530), new Vector2(420, 480), new Vector2(-1, -1) })
            Assert.Null(map.Pick(point));
    }

    [Fact]
    public void InvalidOrDuplicateCoordinatesFailClosed()
    {
        var map = SettlementMapLayout.Read(LayoutPath);
        string temporary = Path.GetTempFileName();
        try
        {
            foreach (var invalid in new[] {
                map with { Width = 0 },
                map with { Spots = [.. map.Spots.Take(8)] },
                map with { Spots = [map.Spots[1], .. map.Spots.Skip(1)] },
                map with { Spots = [map.Spots[0] with { Radii = new(0, 40) }, .. map.Spots.Skip(1)] },
                map with { Spots = [map.Spots[0] with { BuildingAnchor = new(-1, 40) }, .. map.Spots.Skip(1)] }
            })
            {
                File.WriteAllText(temporary, JsonSerializer.Serialize(invalid));
                Assert.Throws<InvalidDataException>(() => SettlementMapLayout.Read(temporary));
            }
        }
        finally { File.Delete(temporary); }
    }
}
