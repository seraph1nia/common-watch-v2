using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class BattlefieldView
{
    public int SelectedUnit { get; set; }
    public (int Owner, ArenaStructureKind Kind, int Slot)? SelectedStructure { get; set; }
    internal int SquadCueCount { get; private set; }
    internal int HighlightedMembers { get; private set; }
    internal int HighlightedTarget { get; private set; }
    internal bool SquadCuesFit { get; private set; }
    private long _readabilityWave = -1;
    private readonly Dictionary<(Faction Faction, int Owner, int Squad), (double Health, int Members)> _observedSquads = [];
    private void ClearReadability() { ClearBossPresentation(); _readabilityWave = -1; _observedSquads.Clear(); }
    private static readonly Color[] OwnerColors = [new("85dfc2"), new("9ec6f7"), new("f2d984"), new("c3abef"), new("efb5cd"), new("9edddd")];
    private static Color OwnerColor(UnitState unit) => unit.Faction != Faction.Adventurers ? new("ee967b")
        : OwnerColors[(Math.Max(1, unit.Owner) - 1) % OwnerColors.Length];
    private void DrawReadability(UnitState[] units, Dictionary<int, Vector2> positions, Func<int, Vector2> cell, Rect2 visible)
    {
        SquadCueCount = 0; HighlightedMembers = 0; HighlightedTarget = 0; SquadCuesFit = true;
        var selected = units.FirstOrDefault(u => u.Id == SelectedUnit);
        var targets = selected is null ? [] : CombatInspection.TargetMembers(selected, units);
        var targetStructure = selected is null ? null : CombatInspection.TargetStructure(selected, Playback.Structures());
        if (_readabilityWave != CombatState.WaveStartedTick) { _observedSquads.Clear(); _readabilityWave = CombatState.WaveStartedTick; }
        var labels = new List<Rect2>();
        var bossAreas = units.Where(u => u.IsBoss && _figures.TryGetValue(u.Id, out var visual) && visual.Visible).Select(u =>
        {
            var figure = _figures[u.Id]; var bounds = figure.ReadabilityBounds;
            Vector2[] corners = [bounds.Position, new(bounds.End.X, bounds.Position.Y), bounds.End, new(bounds.Position.X, bounds.End.Y)];
            var rectangle = new Rect2(figure.Transform * corners[0], Vector2.Zero);
            foreach (var corner in corners.Skip(1)) rectangle = rectangle.Expand(figure.Transform * corner);
            return rectangle;
        }).ToArray();
        float top = visible.Position.Y + 76 / GetGlobalTransformWithCanvas().Scale.Y;
        foreach (var group in units.Where(u => u.Health > 0).GroupBy(u => (u.Faction, u.Owner, Squad: u.SquadId == 0 ? -u.Id : u.SquadId)))
        {
            var members = group.ToArray(); var leader = members[0];
            Vector2 ground = members.Aggregate(Vector2.Zero, (sum, u) => sum + positions[u.Id]) / members.Length;
            if (!visible.Grow(50).HasPoint(ground)) continue;
            SquadCueCount++;
            double health = members.Sum(u => (double)u.Health), maximum = members.Sum(u => (double)Math.Max(1, u.Profile.Health));
            var observed = _observedSquads.GetValueOrDefault(group.Key);
            observed = (Math.Max(observed.Health, maximum), Math.Max(observed.Members, members.Length));
            _observedSquads[group.Key] = observed; maximum = observed.Health;
            float fraction = (float)Math.Clamp(health / maximum, 0, 1);
            string role = leader.Class switch { UnitClass.Melee => "F", UnitClass.Ranged => "R", _ => "M" };
            int sector = TeamAwareness.Sector(CombatState, leader);
            string owner = leader.Faction == Faction.Adventurers ? $"P{leader.Owner}" + (sector > 0 && sector != leader.Owner ? $"@P{sector}" : "") : $"E→{leader.Destination}";
            string cue = leader.IsBoss ? $"BOSS →P{leader.Destination} · {HealthPoints.Format(leader.Health)} HP" : $"{owner} {role}{members.Length}/{observed.Members}{(fraction <= .3f ? " !" : "")}";
            float width = Mathf.Max(64, ThemeDB.FallbackFont.GetStringSize(cue, fontSize: 11).X + 8);
            var rect = new Rect2(ground + new Vector2(-width / 2, leader.IsBoss ? 18 : -65), new Vector2(width, 23));
            rect.Position = new(Mathf.Clamp(rect.Position.X, visible.Position.X + 4, visible.End.X - width - 4),
                Mathf.Clamp(rect.Position.Y, top, visible.End.Y - 27));
            var start = rect.Position;
            for (int n = 1; n <= 48 && (labels.Any(r => r.Grow(2).Intersects(rect)) || bossAreas.Any(r => r.Grow(2).Intersects(rect))); n++)
                rect.Position = new(start.X, Mathf.Clamp(start.Y + (n % 2 == 0 ? n / 2 : -(n + 1) / 2) * 25, top, visible.End.Y - 27));
            SquadCuesFit &= visible.Encloses(rect) && !bossAreas.Any(r => r.Intersects(rect));
            labels.Add(rect);
            Color color = leader.IsBoss ? new("f4d688") : fraction <= .3f ? new("ffc48d") : OwnerColor(leader);
            _combatOverlay!.Bar(rect, new Color(.025f, .045f, .06f, .88f));
            _combatOverlay.Bar(new Rect2(rect.Position + new Vector2(3, 18), new Vector2((width - 6) * fraction, 3)), color);
            _combatOverlay.Text(cue, rect.Position + new Vector2(width / 2, 13), 11, color);
            DrawLine(ground + new Vector2(0, -40), rect.GetCenter() + new Vector2(0, 10), color with { A = .4f }, 1, true);
        }
        foreach (var unit in units)
        {
            bool chosen = selected is not null && (unit.Id == selected.Id || selected.SquadId != 0 && unit.SquadId == selected.SquadId && unit.Faction == selected.Faction && unit.Owner == selected.Owner);
            bool target = targets.Any(t => t.Id == unit.Id);
            if (!chosen && !target) continue;
            if (chosen) HighlightedMembers++;
            if (target) HighlightedTarget = unit.Id;
            DrawSetTransform(positions[unit.Id], 0, new(1, .45f));
            DrawArc(Vector2.Zero, target ? 20 : 16, 0, Mathf.Tau, 40, new Color(target ? "ffad71" : "ffe08c"), 2.5f, true);
            DrawSetTransform(Vector2.Zero);
        }
        Vector2? targetPosition = targets.Length > 0 ? targets.Aggregate(Vector2.Zero, (sum, u) => sum + positions[u.Id]) / targets.Length : null;
        foreach (var structure in Playback.Structures())
        {
            bool target = targetStructure is not null && (structure.Owner, structure.Kind, structure.Slot) == (targetStructure.Owner, targetStructure.Kind, targetStructure.Slot);
            bool chosen = SelectedStructure == (structure.Owner, structure.Kind, structure.Slot);
            if (!target && !chosen) continue;
            var at = cell(structure.Cell);
            if (target) { targetPosition = at; HighlightedTarget = -structure.Owner; }
            DrawArc(at, 27, 0, Mathf.Tau, 40, new Color(target ? "ffad71" : "ffe08c"), 2.5f, true);
        }
        if (selected is not null && targetPosition is { } destination)
            DrawLine(positions[selected.Id], destination, new Color(1, .7f, .4f, .65f), 1.5f, true);
    }
    private void DrawDamageTotals(MatchSnapshot state, double tick, Func<System.Numerics.Vector2, Vector2> project, Func<int, Vector2> cell)
    {
        var hits = _motion.Events.Where(e => e.Damage > 0 && tick >= e.Tick && tick - e.Tick <= 35
            && e.Type is CombatEventType.Hit or CombatEventType.StructureHit or CombatEventType.StructureDestroyed);
        foreach (var group in hits.GroupBy(e => (Bucket: e.Tick / 12, Squad: e.Unit?.SquadId ?? 0,
            Unit: e.Unit?.SquadId > 0 ? 0 : e.Unit?.Id ?? 0, Owner: e.Structure?.Owner ?? 0, Kind: e.Structure?.Kind, Slot: e.Structure?.Slot ?? 0)))
        {
            var entry = group.OrderByDescending(e => e.Tick).First();
            Vector2 point = entry.Structure is { } structure ? cell(structure.Cell)
                : entry.Unit is { } unit ? project(CombatVisuals.Position(unit, state, entry.Tick)) : Vector2.Zero;
            float age = (float)(tick - entry.Tick);
            decimal damage = group.Sum(e => (long)e.Damage) / (decimal)HealthPoints.Scale;
            TextCentered("−" + damage.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture), point + new Vector2(0, -42 - age * .55f), 13, _gold with { A = 1 - age / 35 });
        }
    }
}
