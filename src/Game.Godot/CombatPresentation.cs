using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class Main
{
    CombatOverlay? _combatOverlay;
    bool _renderingCombat;
    readonly Shader _hitShader = new() { Code = CombatFigure.HitFlashCode };
    readonly Dictionary<int, CombatFigure> _figures = [];
    CombatPlayback? _visualPlayback;
    int _visualGeneration, _visualResets, _totalAnimatedHits;
    BattleShowcase? _battle;
    bool _finishingCombat;
    bool PresentingCombat => _battle is not null || _session.Snapshot.Phase == Phase.Combat || _finishingCombat;
    MatchSnapshot CombatState => _battle?.Snapshot ?? _session.Snapshot;
    CombatPlayback Playback => _battle?.Playback ?? _session.Playback;
    double _terminalTick = -1, _terminalSeconds;
    double AnimationTick
    {
        get
        {
            if (CombatState.Phase == Phase.Combat || Playback.Tick < CombatState.Tick) { _terminalTick = -1; return Playback.Tick; }
            if (_terminalTick != Playback.Tick) { _terminalTick = Playback.Tick; _terminalSeconds = Playback.VisualSeconds; }
            return Playback.Tick + (Playback.VisualSeconds - _terminalSeconds) * Match.StepsPerSecond;
        }
    }
    bool CombatShader => Argument("--combat-shader") != "off";
    void ObserveCombat()
    {
        if (_visualPlayback != Playback || _visualGeneration != Playback.Generation)
        {
            _finishingCombat = CombatState.Phase == Phase.Combat;
            _visualResets++; _motion.Clear(); _terminalTick = -1;
            foreach (var figure in _figures.Values) figure.QueueFree();
            _figures.Clear(); _visualPlayback = Playback; _visualGeneration = Playback.Generation;
        }
        foreach (var entry in Playback.Drain())
        {
            _motion.Observe(entry);
            if (entry.Type == CombatEventType.Hit && entry.Damage > 0) _totalAnimatedHits++;
        }
        _motion.Trim(AnimationTick);
        if (CombatState.Phase == Phase.Combat) _finishingCombat = true;
        else if (Playback.Tick >= CombatState.Tick && !Playback.Units().Any(u => u.Hex?.Lifecycle == UnitLifecycle.Dying)) _finishingCombat = false;
    }
    private void DrawAnimatedCombat()
    {
        MatchSnapshot state = CombatState;
        if (state.Arena is not { } arena) return;
        if (_combatOverlay is null) { _combatOverlay = new() { ZIndex = 4095 }; AddChild(_combatOverlay); }
        _combatOverlay.Clear(); _renderingCombat = true;
        double tick = AnimationTick;
        Vector2 center = ArenaCenter();
        Vector2 Project(System.Numerics.Vector2 p) => Screen(new Vector2(p.X, p.Y * _options.GroundCompression) - center);
        Vector2 Ground(UnitState u) => Project(CombatVisuals.Position(u, state, tick));
        Vector2 Cell(int cell) => arena.Board.Cells.FirstOrDefault(c => c.Id == cell) is { } c ? Screen(V(_arena.Project(c.Coordinate)) - center) : Vector2.Zero;
        foreach (var cell in arena.Board.Cells)
        {
            DrawHex(_arena.Corners(cell.Coordinate).Select(c => Screen(V(c) - center)).ToArray(), new Color("23343d"), new Color("3b4d50"), 1);
            Sprite("terrain/woodland_hex", Cell(cell.Id), new Vector2(53, 53) * _zoom, new Color(.36f, .43f, .4f));
        }
        foreach (var structure in Playback.Structures())
        {
            // Keep snapshots omit DestroyedTick; use the actual destruction event when present.
            long destroyedAt = structure.DestroyedTick > 0 ? structure.DestroyedTick : _motion.Events.LastOrDefault(e => e.Type == CombatEventType.StructureDestroyed && e.Structure is { } hit && hit.Owner == structure.Owner && hit.Kind == structure.Kind && hit.Slot == structure.Slot)?.Tick ?? 0;
            float fade = structure.Health > 0 ? 1 : Math.Clamp(1 - (float)(tick - destroyedAt) / 30, 0, 1);
            if (!structure.Active && structure.Health > 0 || fade <= 0) continue;
            Vector2 p = Cell(structure.Cell);
            DrawRect(new Rect2(p - new Vector2(12, 18) * _zoom, new Vector2(24, 23) * _zoom), new Color("a8a084") with { A = fade });
            TextCentered(structure.Kind + " " + HealthPoints.Format(structure.Health), p + new Vector2(0, 17 * _zoom), 10, _muted);
        }
        var units = Playback.Units().Where(u => u.Hex?.Lifecycle is UnitLifecycle.Alive or UnitLifecycle.Dying).OrderBy(u => Ground(u).Y).ThenBy(u => u.Id).ToArray();
        var positions = units.ToDictionary(u => u.Id, Ground);
        var visible = units.Select(u => u.Id).ToHashSet();
        foreach (int id in _figures.Keys.Where(id => !visible.Contains(id)).ToArray()) { _figures[id].QueueFree(); _figures.Remove(id); }
        Vector2 Aim(UnitState actor)
        {
            if (actor.TargetCity || actor.TargetKind is SquadTargetKind.Keep or SquadTargetKind.Watchpost)
            {
                var kind = actor.TargetKind == SquadTargetKind.Watchpost ? ArenaStructureKind.Watchpost : ArenaStructureKind.Keep;
                return Cell(Playback.Structures().FirstOrDefault(s => s.Owner == actor.TargetId && s.Kind == kind)?.Cell ?? actor.Hex!.Position.Cell);
            }
            if (actor.Hex?.FrozenAim is { } frozen) return Project(CombatVisuals.Point(frozen, state, 30));
            return positions.GetValueOrDefault(actor.TargetId, Ground(actor));
        }
        int depth = 1;
        foreach (var unit in units)
        {
            Vector2 ground = positions[unit.Id], aim = Aim(unit);
            Vector2 direction = aim - ground;
            if (unit.Hex is { Action: UnitActionKind.Moving } move) direction = Cell(move.Destination.Cell) - Cell(move.Position.Cell);
            FigurePose pose = _motion.Sample(unit, tick, new(direction.X, direction.Y));
            if (!_figures.TryGetValue(unit.Id, out var figure)) { figure = new(); _figures[unit.Id] = figure; AddChild(figure); }
            figure.ZIndex = depth++; figure.Visible = true;
            DrawSetTransform(ground, 0, new Vector2(_zoom, _zoom * .38f));
            DrawCircle(Vector2.Zero, 10, new Color(0, 0, 0, .28f * pose.Opacity), true, -1, true);
            DrawArc(Vector2.Zero, 9, 0, Mathf.Tau, 24, new Color(unit.Faction == Faction.Adventurers ? "54798c" : "885649") with { A = pose.Opacity }, 1, true);
            DrawSetTransform(Vector2.Zero);
            string id = VisualAssets.UnitId(unit);
            figure.Present(LoadArt(id), _catalog.Metadata(id), unit.Type, unit.Faction == Faction.Adventurers, pose, ground, _zoom, CombatShader, _hitShader);
            Vector2 p = ground + V(pose.Offset) * _zoom;
            float hp = Math.Clamp((float)unit.Health / Math.Max(1, unit.Profile.Health), 0, 1);
            _combatOverlay.Bar(new Rect2(p + new Vector2(-10, -45) * _zoom, new Vector2(20, 3) * _zoom), new Color("111c24"));
            _combatOverlay.Bar(new Rect2(p + new Vector2(-10, -45) * _zoom, new Vector2(20 * hp, 3) * _zoom), new Color(unit.Faction == Faction.Adventurers ? "80bbab" : "ce9382"));
            TextCentered((unit.Faction == Faction.Adventurers ? "S" : "E") + unit.Id, ground + new Vector2(0, 13 * _zoom), 10, _muted);
            DrawStatuses(unit, ground, tick);
            if (unit.Type == UnitType.Mage && tick >= unit.ActionStartTick && tick < unit.ImpactTick && unit.PendingImpact)
                DrawArc(p + new Vector2(0, -22) * _zoom, (5 + (float)(tick - unit.ActionStartTick) / Math.Max(1, unit.ImpactTick - unit.ActionStartTick) * 7) * _zoom, 0, Mathf.Tau, 24, new Color("a79ce8"), 1.5f, true);
            if (unit.Type is UnitType.Crossbowman or UnitType.Mage && tick >= unit.ActionStartTick && tick < unit.ImpactTick && unit.PendingImpact)
            {
                float travel = (float)(tick - unit.ActionStartTick) / Math.Max(1, unit.ImpactTick - unit.ActionStartTick);
                // Cosmetic streak occupies the final windup; arrival is exactly the authoritative impact.
                if (travel > .55f)
                {
                    float t = (travel - .55f) / .45f;
                    Vector2 from = ground + new Vector2(0, -20) * _zoom, to = aim + new Vector2(0, -18) * _zoom;
                    Vector2 shot = from.Lerp(to, t);
                    Color shotColor = new(unit.Type == UnitType.Mage ? "b2a2f2" : "f2d9a2");
                    DrawLine(shot - (to - from).Normalized() * 10 * _zoom, shot, shotColor, 2 * _zoom, true);
                    if (unit.Type == UnitType.Mage) DrawCircle(shot, 3 * _zoom, shotColor);
                }
            }
            if (_options.DebugOverlay)
            {
                DrawLine(ground, aim, _gold with { A = .35f }, 1);
                Rect2 bounds = figure.Bounds;
                Vector2[] corners = [bounds.Position, new(bounds.End.X, bounds.Position.Y), bounds.End, new(bounds.Position.X, bounds.End.Y), bounds.Position];
                DrawPolyline(corners.Select(c => figure.Transform * c).ToArray(), new Color("a9caed"), 1, true);
                DrawLine(ground - new Vector2(5, 0), ground + new Vector2(5, 0), new Color("ff82ce"), 1);
                DrawLine(ground - new Vector2(0, 5), ground + new Vector2(0, 5), new Color("ff82ce"), 1);
                TextCentered($"#{unit.Id} sq{unit.SquadId} {unit.Hex!.Action} →{unit.TargetId} a{unit.AttackSequence}", ground + new Vector2(0, 25 * _zoom), 10);
            }
        }
        foreach (var entry in _motion.Events)
        {
            float age = (float)(tick - entry.Tick); if (age < 0 || age > 35) continue;
            Vector2 p = entry.Unit is { } u ? Project(CombatVisuals.Position(u, state, entry.Tick)) : entry.ImpactPose is { } impact ? Project(CombatVisuals.Point(impact, state, 30)) : Vector2.Zero;
            if (entry.Type is CombatEventType.StructureHit or CombatEventType.StructureDestroyed && entry.Structure is { } structure) p = Cell(structure.Cell);
            if (entry.Type == CombatEventType.Impact && entry.Unit is { } attacker)
            {
                p = Aim(attacker);
                Color color = new(attacker.Type == UnitType.Mage ? "bba7ef" : "ecd5a4");
                if (age < 10)
                {
                    float opacity = 1 - age / 10;
                    DrawArc(p + new Vector2(0, -16) * _zoom, (8 + age * 1.2f) * _zoom, -.7f, 1.5f, 16, color with { A = opacity }, 2 * _zoom, true);
                    if (attacker.Type == UnitType.Mage) DrawArc(p, (6 + age * 2) * _zoom, 0, Mathf.Tau, 32, color with { A = opacity }, 1.5f, true);
                }
            }
            if (entry.Tower is { } weapon)
            {
                var footprint = arena.Footprints.FirstOrDefault(f => f.Owner == weapon.City && (weapon.Slot < 0 ? f.Kind == ArenaStructureKind.Watchpost : f.Slot == weapon.Slot));
                Vector2 source = Cell(footprint?.Cell ?? 0) + new Vector2(0, -18) * _zoom;
                Vector2 target = positions.GetValueOrDefault(entry.TargetId, p) + new Vector2(0, -18) * _zoom;
                if (entry.Type == CombatEventType.AttackStarted && tick < weapon.ImpactTick)
                {
                    float travel = (float)Math.Clamp((tick - weapon.ActionStartTick) / Math.Max(1, weapon.ImpactTick - weapon.ActionStartTick), 0, 1);
                    if (travel > .55f)
                    {
                        Vector2 shot = source.Lerp(target, (travel - .55f) / .45f);
                        DrawLine(shot - (target - source).Normalized() * 10 * _zoom, shot, _gold, 2 * _zoom, true);
                    }
                }
                if (entry.Type == CombatEventType.Impact && age < 9) DrawArc(target, (4 + age) * _zoom, 0, Mathf.Tau, 20, _gold with { A = 1 - age / 9 }, 2, true);
            }
            if (entry.Type == CombatEventType.DefenderShot && entry.Unit is { } victim && age < 8)
            {
                Vector2 source = entry.Structure is { } tower ? Cell(tower.Cell) : Cell(arena.Footprints.First(f => f.Kind == ArenaStructureKind.Watchpost).Cell);
                DrawLine(source + new Vector2(0, -18) * _zoom, positions.GetValueOrDefault(victim.Id, p) + new Vector2(0, -18) * _zoom, _gold with { A = 1 - age / 8 }, 2 * _zoom, true);
            }
            if ((entry.Type is CombatEventType.Hit or CombatEventType.StructureHit or CombatEventType.StructureDestroyed) && entry.Damage > 0)
            {
                // Every event retains its sequence, including simultaneous hits on one victim.
                Vector2 n = p + new Vector2((entry.Sequence % 3 - 1) * 9, -43 - age * .55f) * _zoom;
                TextCentered("−" + HealthPoints.Format(entry.Damage), n, 13, _gold with { A = Math.Clamp(1 - age / 35, 0, 1) });
                if (age < 12) for (int i = 0; i < 5; i++)
                {
                    float angle = i * Mathf.Tau / 5 + entry.Sequence;
                    DrawCircle(p + (new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (2 + age * .9f) + new Vector2(0, -18)) * _zoom, 1.3f * _zoom, _gold with { A = 1 - age / 12 });
                }
            }
            if (entry.Type == CombatEventType.StructureDestroyed && age < 30)
                for (int i = 0; i < 6; i++) DrawCircle(p + new Vector2((i - 3) * age * .2f, -10 + age * .2f) * _zoom, 2 * _zoom, _muted with { A = 1 - age / 30 });
        }
        _renderingCombat = false;
        if (_options.DebugOverlay) TextCentered($"sim {state.Tick}   presentation {tick:F2}   event {Playback.EventCursor}   effects {_motion.Events.Count}", new Vector2((GetViewportRect().Size.X - SideWidth) / 2, 151), 13);
    }
    void DrawStatuses(UnitState unit, Vector2 p, double tick)
    {
        void Status(StatusEffect? effect, Color color, int index)
        {
            if (effect is null || tick < effect.AppliedTick || tick >= effect.ExpiresTick) return;
            for (int i = 0; i < 3; i++)
            {
                float phase = (float)((tick - effect.AppliedTick + i * 13 + unit.Id * 7) % 43) / 43;
                Vector2 at = p + new Vector2(MathF.Sin(phase * 6 + i + index) * 10, -4 - phase * 26) * _zoom;
                DrawCircle(at, (1 + (1 - phase)) * _zoom, color with { A = 1 - phase });
            }
            DrawArc(p, (11 + index * 2) * _zoom, 0, Mathf.Pi, 16, color with { A = .7f }, 1, true);
        }
        Status(unit.Statuses.Burn, new Color("e9a05d"), 0);
        Status(unit.Statuses.Poison.FirstOrDefault(), new Color("9ac86e"), 1);
        Status(unit.Statuses.Chill, new Color("8fcee7"), 2);
    }
}
