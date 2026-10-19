using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Game.Shared;

public sealed record MapPoint(float X, float Y)
{
    [JsonIgnore] public Vector2 Vector => new(X, Y);
    internal bool Finite => float.IsFinite(X) && float.IsFinite(Y);
}

public sealed record MapFrame(MapPoint Center, MapPoint Size);
public sealed record SettlementMapSpot(int Slot, MapPoint Center, MapPoint Radii,
    MapPoint BuildingAnchor, MapPoint LabelPosition, float BuildingSize)
{
    public bool Contains(Vector2 imagePoint)
    {
        Vector2 relative = (imagePoint - Center.Vector) / Radii.Vector;
        return relative.LengthSquared() <= 1;
    }
}

/// <summary>Measured image pixels only; no projection or gameplay ownership rules.</summary>
public sealed record SettlementMapLayout(int Version, int Width, int Height, MapFrame Frame, SettlementMapSpot[] Spots)
{
    public int? Pick(Vector2 imagePoint) => Spots.FirstOrDefault(s => s.Contains(imagePoint))?.Slot;

    public static SettlementMapLayout Read(string path)
    {
        using var file = File.OpenRead(path);
        if (file.Length > 32_768) throw new InvalidDataException("Settlement map metadata is too large.");
        var layout = JsonSerializer.Deserialize<SettlementMapLayout>(file, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidDataException("Missing settlement map layout.");
        bool InImage(MapPoint? p) => p is { Finite: true } && p.X >= 0 && p.Y >= 0 && p.X <= layout.Width && p.Y <= layout.Height;
        if (layout.Version != 1 || layout.Width is <= 0 or > 8192 || layout.Height is <= 0 or > 8192
            || layout.Frame is not { Size: { Finite: true, X: > 0, Y: > 0 } } || !InImage(layout.Frame.Center)
            || layout.Spots is not { Length: 9 } || layout.Spots.Any(s => s is null)
            || !layout.Spots.Select(s => s.Slot).Order().SequenceEqual(Enumerable.Range(0, 9))
            || layout.Spots.Any(s => !InImage(s.Center) || !InImage(s.BuildingAnchor) || !InImage(s.LabelPosition)
                || s.Radii is not { Finite: true, X: > 0, Y: > 0 } || s.Radii.X > layout.Width || s.Radii.Y > layout.Height
                || !float.IsFinite(s.BuildingSize) || s.BuildingSize is <= 0 or > 1024))
            throw new InvalidDataException("Invalid settlement map coordinates.");
        return layout with { Spots = layout.Spots.OrderBy(s => s.Slot).ToArray() };
    }
}
