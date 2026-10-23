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
    internal int SquadLabelCount { get; private set; }
    internal Rect2[] HudOccluders { get; set; } = [];
    private long _readabilityWave = -1;
    private readonly Dictionary<(Faction Faction, int Owner, int Squad), (double Health, int Members)> _observedSquads = [];
    private void ClearReadability() { ClearBossPresentation(); _readabilityWave = -1; _observedSquads.Clear(); }
    private static readonly Color[] OwnerColors = [new("85dfc2"), new("9ec6f7"), new("f2d984"), new("c3abef"), new("efb5cd"), new("9edddd")];
    private static Color OwnerColor(UnitState unit) => unit.Faction != Faction.Adventurers ? new("ee967b")
        : OwnerColors[(Math.Max(1, unit.Owner) - 1) % OwnerColors.Length];
    internal Vector2[] InspectionPoints()
    {
        var units = Playback.Units(); var center = ArenaCenter();
        Vector2 Ground(UnitState unit)
        {
            var p = CombatVisuals.Position(unit, CombatState, AnimationTick);
            return new(p.X - center.X, p.Y * _options.GroundCompression - center.Y);
        }
        Vector2[] Body(UnitState unit)
        {
            if (!_figures.TryGetValue(unit.Id, out var figure)) return [Ground(unit)];
            var bounds = figure.Bounds; var transform = figure.Transform;
            return [transform * bounds.Position, transform * new Vector2(bounds.End.X, bounds.Position.Y),
                transform * bounds.End, transform * new Vector2(bounds.Position.X, bounds.End.Y)];
        }
        if (units.FirstOrDefault(u => u.Id == SelectedUnit) is { } selected)
        {
            var points = new List<Vector2>(Body(selected));
            if (selected.Health > 0)
            {
                points.AddRange(CombatInspection.TargetMembers(selected, units).Where(u => u.Health > 0).SelectMany(Body));
                if (CombatInspection.TargetStructure(selected, Playback.Structures()) is { } target && FeedbackPoint(target.Cell) is { } at) points.Add(at);
            }
            return points.ToArray();
        }
        var structure = Playback.Structures().FirstOrDefault(s => SelectedStructure == (s.Owner, s.Kind, s.Slot));
        return structure is not null && FeedbackPoint(structure.Cell) is { } point ? [point] : [];
    }
    private void DrawReadability(UnitState[] units, Dictionary<int, Vector2> positions, Func<int, Vector2> cell, Rect2 visible)
    {
        SquadCueCount = SquadLabelCount = 0; HighlightedMembers = 0; HighlightedTarget = 0; SquadCuesFit = true;
        var selected = units.FirstOrDefault(u => u.Id == SelectedUnit && u.Health > 0);
        var targets = selected is null ? [] : CombatInspection.TargetMembers(selected, units).Where(u => u.Health > 0).ToArray();
        var targetStructure = selected is null ? null : CombatInspection.TargetStructure(selected, Playback.Structures());
        if (_readabilityWave != CombatState.WaveStartedTick) { _observedSquads.Clear(); _readabilityWave = CombatState.WaveStartedTick; }
        var labels = new List<Rect2>();
        var actorAreas = units.Where(u => _figures.TryGetValue(u.Id, out var visual) && visual.Visible).Select(u =>
        {
            var figure = _figures[u.Id]; var bounds = figure.ReadabilityBounds;
            Vector2[] corners = [bounds.Position, new(bounds.End.X, bounds.Position.Y), bounds.End, new(bounds.Position.X, bounds.End.Y)];
            var rectangle = new Rect2(figure.Transform * corners[0], Vector2.Zero);
            foreach (var corner in corners.Skip(1)) rectangle = rectangle.Expand(figure.Transform * corner);
            return rectangle;
        }).ToArray();
        float scale = GetGlobalTransformWithCanvas().Scale.Y;
        var inverse = GetGlobalTransformWithCanvas().AffineInverse();
        var protectedAreas = HudOccluders.Select(r => new Rect2(inverse * r.Position, r.Size / scale)).ToArray();
        float top = visible.Position.Y + 12 / scale;
        foreach (var group in units.Where(u => u.Health > 0).GroupBy(u => (u.Faction, u.Owner, Squad: u.SquadId == 0 ? -u.Id : u.SquadId))
            .OrderByDescending(g => selected is not null && g.Any(u => u.Id == selected.Id || selected.SquadId != 0 && u.SquadId == selected.SquadId && u.Owner == selected.Owner && u.Faction == selected.Faction))
            .ThenBy(g => g.Sum(u => (double)u.Health) / g.Sum(u => (double)Math.Max(1, u.Profile.Health))))
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
            bool chosenSquad = selected is not null && members.Any(u => u.Id == selected.Id || selected.SquadId != 0 && u.SquadId == selected.SquadId && u.Owner == selected.Owner && u.Faction == selected.Faction);
            bool healthCue = chosenSquad || fraction < .99f || leader.IsBoss;
            string owner = leader.Faction == Faction.Adventurers ? leader.Owner == _session.PlayerId ? "You" : $"Ally {leader.Owner}" : "Enemy";
            string cue = leader.IsBoss ? "Boss" : $"{owner} {members.Length}";
            if (chosenSquad) cue += " · " + CombatInspection.Role(leader);
            if (fraction <= .3f) cue += " · Low";
            int font = Math.Max(1, (int)Math.Round(12 / scale));
            float width = ThemeDB.FallbackFont.GetStringSize(cue, fontSize: font).X + 8 / scale;
            float height = (healthCue ? 23 : 17) / scale;
            float head = members.Where(u => _figures.ContainsKey(u.Id)).Select(u => _figures[u.Id].Position.Y + _figures[u.Id].ReadabilityBounds.Position.Y).DefaultIfEmpty(ground.Y - 40).Min();
            var rect = new Rect2(new Vector2(ground.X - width / 2, head - height - 4 / scale), new Vector2(width, height));
            rect.Position = new(Mathf.Clamp(rect.Position.X, visible.Position.X + 4, visible.End.X - width - 4),
                Mathf.Clamp(rect.Position.Y, top, visible.End.Y - height - 4 / scale));
            var start = rect.Position;
            bool Obstructed() => labels.Any(r => r.Grow(2 / scale).Intersects(rect)) || actorAreas.Any(r => r.Grow(2 / scale).Intersects(rect)) || protectedAreas.Any(r => r.Intersects(rect));
            for (int n = 1; n <= 48 && Obstructed(); n++)
                rect.Position = new(start.X, Mathf.Clamp(start.Y + (n % 2 == 0 ? n / 2 : -(n + 1) / 2) * (height + 3 / scale), top, visible.End.Y - height - 4 / scale));
            if (Obstructed() || !visible.Encloses(rect)) continue;
            SquadLabelCount++;
            SquadCuesFit &= visible.Encloses(rect);
            labels.Add(rect);
            Color color = leader.IsBoss ? new("f4d688") : fraction <= .3f ? new("ffc48d") : OwnerColor(leader);
            _combatOverlay!.Bar(rect, new Color(.025f, .045f, .06f, .88f));
            if (healthCue) _combatOverlay.Bar(new Rect2(rect.Position + new Vector2(3 / scale, 18 / scale), new Vector2((width - 6 / scale) * fraction, 3 / scale)), color);
            _combatOverlay.Text(cue, rect.Position + new Vector2(width / 2, 13 / scale), font, color);
        }
        foreach (var unit in units)
        {
            bool chosen = unit.Health > 0 && selected is not null && (unit.Id == selected.Id || selected.SquadId != 0 && unit.SquadId == selected.SquadId && unit.Faction == selected.Faction && unit.Owner == selected.Owner);
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
        var selected = Playback.Units().FirstOrDefault(u => u.Id == SelectedUnit && u.Health > 0);
        var hits = _motion.Events.Where(e => e.Damage > 0 && tick >= e.Tick && tick - e.Tick <= 35
            && e.Type is CombatEventType.Hit or CombatEventType.StructureHit or CombatEventType.StructureDestroyed)
            .Where(e => e.Structure is not null || e.Unit is { IsBoss: true } || selected is not null && e.Unit is { } victim
                && (victim.Id == selected.Id || selected.SquadId != 0 && victim.SquadId == selected.SquadId && victim.Owner == selected.Owner && victim.Faction == selected.Faction));
        foreach (var group in hits.GroupBy(e => (Bucket: e.Tick / 24, Squad: e.Unit?.SquadId ?? 0,
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
