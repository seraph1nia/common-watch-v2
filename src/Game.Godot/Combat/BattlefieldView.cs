using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class BattlefieldView : Node2D
{
    CombatOverlay? _combatOverlay;
    private readonly ArenaScenery _scenery = new();
    readonly Shader _hitShader = GD.Load<Shader>("res://Combat/HitFlash.gdshader");
    readonly Dictionary<int, CombatFigure> _figures = [];
    readonly Dictionary<(int Owner, ArenaStructureKind Kind, int Slot), CombatStructure> _structures = [];
    CombatPlayback? _visualPlayback;
    int _visualGeneration, _visualResets, _totalAnimatedHits;
    BattleShowcase? _battle;
    private GodotSession _session = null!;
    private AssetLoader _assets = null!;
    private EnvironmentView _environmentView = null!;
    private EnvironmentController _environment = null!;
    private WorldArt _art = null!;
    private RenderOptions _options => _assets.Options;
    private HexProjection _arena = new(30);
    private float _compression = 1;
    private readonly CombatMotion _motion = new();
    private static readonly Color _gold = new("efd478"), _muted = new("a9bcc7");
    private Label _diagnostics = null!;
    public event Action? FramePresented;
    internal EnvironmentView Atmosphere => _environmentView;
    public CombatMotion Motion => _motion;
    public int Resets => _visualResets;
    public int TotalHits => _totalAnimatedHits;
    public int FigureCount => _figures.Count;
    public bool Finishing => _finishingCombat;
    public bool Active => _session is { HasState: true } && PresentingCombat;
    public bool ShaderEnabled { get; set; } = true;
    public void Initialize(AssetLoader assets, EnvironmentController environment)
    {
        _assets = assets; _art = new(assets);
        AddChild(_scenery);
        _environment = environment; _environmentView = environment.Attach(this, EnvironmentScene.Arena); _environmentView.ShowBehindParent = true;
        var layer = new CanvasLayer { Layer = 1 }; AddChild(layer);
        _diagnostics = new Label { Position = new Vector2(22, 130), MouseFilter = Control.MouseFilterEnum.Ignore }; layer.AddChild(_diagnostics);
        Hide();
    }
    public void Present(GodotSession session, BattleShowcase? battle)
    {
        _session = session; _battle = battle;
        if (_compression != _options.GroundCompression)
        { _compression = _options.GroundCompression; _arena = new(30, _compression); }
        ObserveCombat(); Visible = Active;
        _scenery.AnimatedEnvironment = _environment.Settings.Quality != EnvironmentQuality.Off;
        if (Active) _environmentView.SetArena(new ArenaLayout(CombatState.Arena!, _arena));
        _scenery.Present(_assets, Active ? CombatState.Arena : null, _arena);
        _scenery.Modulate = _options.Mode == RenderMode.Sprites ? new Color(.78f, .83f, .8f) : Colors.White;
        _diagnostics.Visible = Active && _options.DebugOverlay;
        if (!Active) { foreach (var figure in _figures.Values) figure.Hide(); foreach (var structure in _structures.Values) structure.Hide(); if (_combatOverlay is not null) _combatOverlay.Hide(); }
        QueueRedraw();
    }
    public void Reset()
    {
        foreach (var figure in _figures.Values) figure.QueueFree(); _figures.Clear();
        foreach (var structure in _structures.Values) structure.QueueFree(); _structures.Clear();
        _motion.Clear(); _visualPlayback = null; _finishingCombat = false; _totalAnimatedHits = 0;
        ClearReadability();
        _session = null!; _battle = null;
        Hide(); _diagnostics?.Hide();
    }
    public override void _Draw() { if (Active) { DrawAnimatedCombat(); FramePresented?.Invoke(); } }
    public override void _ExitTree() { Reset(); _hitShader.Dispose(); FramePresented = null; }
    bool _finishingCombat;
    bool PresentingCombat => _battle is not null || _session.Snapshot.Phase == Phase.Combat || _finishingCombat;
    MatchSnapshot CombatState => _battle?.Snapshot ?? _session.Snapshot;
    public CombatPlayback Playback => _battle?.Playback ?? _session.Playback;
    double _terminalTick = -1, _terminalSeconds;
    public double AnimationTick
    {
        get
        {
            if (CombatState.Phase == Phase.Combat || Playback.Tick < CombatState.Tick) { _terminalTick = -1; return Playback.Tick; }
            if (_terminalTick != Playback.Tick) { _terminalTick = Playback.Tick; _terminalSeconds = Playback.VisualSeconds; }
            return Playback.Tick + (Playback.VisualSeconds - _terminalSeconds) * Match.StepsPerSecond;
        }
    }
    private bool CombatShader => ShaderEnabled;
    private static Vector2 V(System.Numerics.Vector2 value) => new(value.X, value.Y);
    public Rect2 RenderBounds => CombatState.Arena is { } arena ? ArenaPresentation.RenderBounds(arena, _arena, _options.Mode == RenderMode.Sprites) : new Rect2(-200, -200, 400, 400);
    private Vector2 ArenaCenter()
    {
        var coordinates = CombatState.Arena?.Board.Cells.Select(c => V(_arena.Project(c.Coordinate))).ToArray() ?? [];
        return coordinates.Length == 0 ? Vector2.Zero : new Vector2((coordinates.Min(v => v.X) + coordinates.Max(v => v.X)) / 2, (coordinates.Min(v => v.Y) + coordinates.Max(v => v.Y)) / 2);
    }
    private void TextCentered(string text, Vector2 position, int size, Color? color = null) => _combatOverlay!.Text(text, position, size, color ?? new Color("e7edf2"));
    void ObserveCombat()
    {
        if (_visualPlayback != Playback || _visualGeneration != Playback.Generation)
        {
            _finishingCombat = CombatState.Phase == Phase.Combat;
            ClearReadability();
            _visualResets++; _motion.Clear(); _terminalTick = -1;
            foreach (var figure in _figures.Values) figure.QueueFree();
            foreach (var structure in _structures.Values) structure.QueueFree(); _structures.Clear();
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
        _combatOverlay.Clear();
        double tick = AnimationTick;
        Vector2 center = ArenaCenter();
        Vector2 Project(System.Numerics.Vector2 p) => new Vector2(p.X, p.Y * _options.GroundCompression) - center;
        Vector2 Ground(UnitState u) => Project(CombatVisuals.Position(u, state, tick));
        Vector2 Cell(int cell) => arena.Board.Cells.FirstOrDefault(c => c.Id == cell) is { } c ? V(_arena.Project(c.Coordinate)) - center : Vector2.Zero;
        bool modular = _scenery.Available;
        if (!modular) ArenaPresentation.Draw(this, _assets, arena, _arena);
        Rect2 visibleArea = WorldArt.VisibleArea(this);
        var units = Playback.Units().Where(u => u.Hex?.Lifecycle is UnitLifecycle.Alive or UnitLifecycle.Dying).OrderBy(u => Ground(u).Y).ThenBy(u => u.Id).ToArray();
        var positions = units.ToDictionary(u => u.Id, Ground);
        // Exact common ground order: animation bounce/rotation never changes occlusion.
        var depths = units.Select(u => (Key: (Unit: u.Id, Owner: 0, Kind: ArenaStructureKind.Keep, Slot: 0), Y: positions[u.Id].Y))
            .Concat(Playback.Structures().Select(s => (Key: (Unit: 0, s.Owner, s.Kind, s.Slot), Y: Cell(s.Cell).Y)))
            .OrderBy(a => a.Y).ThenBy(a => a.Key).Select((a, index) => (a.Key, Depth: index + 1)).ToDictionary(a => a.Key, a => a.Depth);
        foreach (var cell in arena.Board.Cells)
        {
            if (modular) break;
            var corners = _arena.Corners(cell.Coordinate).Select(c => V(c) - center).ToArray();
            DrawColoredPolygon(corners, new Color("45553b"));
            _art.Ground(this, "terrain/woodland_hex", corners, new Color(.78f, .83f, .68f));
        }
        // Borders finish after all terrain so opaque texture edges cannot hide neighboring outlines.
        foreach (var cell in arena.Board.Cells)
        {
            var corners = _arena.Corners(cell.Coordinate).Select(c => V(c) - center).ToArray();
            if (visibleArea.Grow(30).HasPoint(Cell(cell.Id))) DrawPolyline(corners.Append(corners[0]).ToArray(), modular ? new Color(.65f, .73f, .78f, .22f) : new Color("6d7955"), 1, true);
        }
        foreach (var visual in _structures.Values) visual.Hide();
        var structureKeys = Playback.Structures().Select(s => (s.Owner, s.Kind, s.Slot)).ToHashSet();
        foreach (var key in _structures.Keys.Where(k => !structureKeys.Contains(k)).ToArray()) { _structures[key].QueueFree(); _structures.Remove(key); }
        foreach (var structure in Playback.Structures())
        {
            // Keep snapshots omit DestroyedTick; use the actual destruction event when present.
            long destroyedAt = structure.DestroyedTick > 0 ? structure.DestroyedTick : _motion.Events.LastOrDefault(e => e.Type == CombatEventType.StructureDestroyed && e.Structure is { } hit && hit.Owner == structure.Owner && hit.Kind == structure.Kind && hit.Slot == structure.Slot)?.Tick ?? 0;
            float fade = structure.Health > 0 ? 1 : Math.Clamp(1 - (float)(tick - destroyedAt) / 30, 0, 1);
            if (!structure.Active && structure.Health > 0 || fade <= 0) continue;
            Vector2 p = Cell(structure.Cell);
            if (!visibleArea.Grow(120).HasPoint(p)) continue;
            var key = (structure.Owner, structure.Kind, structure.Slot);
            if (!_structures.TryGetValue(key, out var visual)) { visual = new(); _structures[key] = visual; AddChild(visual); }
            var hit = _motion.Events.LastOrDefault(e => e.Type == CombatEventType.StructureHit && e.Structure is { } s && (s.Owner, s.Kind, s.Slot) == key && e.Tick <= tick);
            float flash = hit is null ? 0 : Math.Clamp(1 - (float)(tick - hit.Tick) / 12, 0, 1);
            visual.Present(_assets, structure, p, depths[(0, structure.Owner, structure.Kind, structure.Slot)], fade, flash);
            float hp = Math.Clamp((float)structure.Health / Math.Max(1, structure.MaximumHealth), 0, 1);
            _combatOverlay.Bar(new Rect2(p + new Vector2(-17, -53), new Vector2(34, 4)), new Color("111c24") with { A = fade });
            _combatOverlay.Bar(new Rect2(p + new Vector2(-17, -53), new Vector2(34 * hp, 4)), new Color("80bbab") with { A = fade });
            TextCentered($"P{structure.Owner} {structure.Kind}", p + new Vector2(0, 17), 10, _muted with { A = fade });
        }
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
        foreach (var unit in units)
        {
            Vector2 ground = positions[unit.Id], aim = Aim(unit);
            if (!visibleArea.Grow(120).HasPoint(ground)) { if (_figures.TryGetValue(unit.Id, out var hidden)) hidden.Hide(); continue; }
            Vector2 direction = aim - ground;
            if (unit.Hex is { Action: UnitActionKind.Moving } move) direction = Cell(move.Destination.Cell) - Cell(move.Position.Cell);
            FigurePose pose = _motion.Sample(unit, tick, new(direction.X, direction.Y));
            if (!_figures.TryGetValue(unit.Id, out var figure)) { figure = new(); _figures[unit.Id] = figure; AddChild(figure); }
            figure.ZIndex = depths[(unit.Id, 0, ArenaStructureKind.Keep, 0)]; figure.Visible = true;
            DrawSetTransform(ground, 0, new Vector2(1, .38f));
            DrawCircle(Vector2.Zero, unit.IsBoss ? 16 : 11, new Color(0, 0, 0, .48f * pose.Opacity), true, -1, true);
            DrawArc(Vector2.Zero, unit.IsBoss ? 15 : 10, 0, Mathf.Tau, 24, OwnerColor(unit) with { A = pose.Opacity }, 1.6f, true);
            DrawSetTransform(Vector2.Zero);
            string id = VisualAssets.UnitId(unit);
            figure.Present(_assets.Load(id), _assets.Catalog.Metadata(id), unit.Type, unit.Faction == Faction.Adventurers, pose, ground, CombatShader, _hitShader, unit.IsBoss);
            Vector2 p = ground + V(pose.Offset);
            float hp = Math.Clamp((float)unit.Health / Math.Max(1, unit.Profile.Health), 0, 1);
            if (_options.DebugOverlay)
            {
                _combatOverlay.Bar(new Rect2(p + new Vector2(-10, -45), new Vector2(20, 3)), new Color("111c24"));
                _combatOverlay.Bar(new Rect2(p + new Vector2(-10, -45), new Vector2(20 * hp, 3)), OwnerColor(unit));
                TextCentered((unit.Faction == Faction.Adventurers ? "S" : "E") + unit.Id, ground + new Vector2(0, 13), 10, _muted);
            }
            DrawStatuses(unit, ground, tick);
            if (unit.Type == UnitType.Mage && tick >= unit.ActionStartTick && tick < unit.ImpactTick && unit.PendingImpact)
                DrawArc(p + new Vector2(0, -22), (5 + (float)(tick - unit.ActionStartTick) / Math.Max(1, unit.ImpactTick - unit.ActionStartTick) * 7), 0, Mathf.Tau, 24, new Color("a79ce8"), 1.5f, true);
            if (unit.Type is UnitType.Crossbowman or UnitType.Mage && tick >= unit.ActionStartTick && tick < unit.ImpactTick && unit.PendingImpact)
            {
                float travel = (float)(tick - unit.ActionStartTick) / Math.Max(1, unit.ImpactTick - unit.ActionStartTick);
                // Cosmetic streak occupies the final windup; arrival is exactly the authoritative impact.
                if (travel > .55f)
                {
                    float t = (travel - .55f) / .45f;
                    Vector2 from = ground + new Vector2(0, -20), to = aim + new Vector2(0, -18);
                    Vector2 shot = from.Lerp(to, t);
                    Color shotColor = new(unit.Type == UnitType.Mage ? "b2a2f2" : "f2d9a2");
                    DrawLine(shot - (to - from).Normalized() * 10, shot, shotColor, 2, true);
                    if (unit.Type == UnitType.Mage) DrawCircle(shot, 3, shotColor);
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
                TextCentered($"#{unit.Id} sq{unit.SquadId} {unit.Hex!.Action} →{unit.TargetId} a{unit.AttackSequence}", ground + new Vector2(0, 25), 10);
            }
        }
        DrawReadability(units, positions, Cell, visibleArea);
        DrawBossPresentation(units, positions, visibleArea, state, tick, Project);
        DrawDamageTotals(state, tick, Project, Cell);
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
                    DrawArc(p + new Vector2(0, -16), (8 + age * 1.2f), -.7f, 1.5f, 16, color with { A = opacity }, 2, true);
                    if (attacker.Type == UnitType.Mage) DrawArc(p, (6 + age * 2), 0, Mathf.Tau, 32, color with { A = opacity }, 1.5f, true);
                }
            }
            if (entry.Tower is { } weapon)
            {
                var footprint = arena.Footprints.FirstOrDefault(f => f.Owner == weapon.City && (weapon.Slot < 0 ? f.Kind == ArenaStructureKind.Watchpost : f.Slot == weapon.Slot));
                Vector2 source = Cell(footprint?.Cell ?? 0) + new Vector2(0, -18);
                Vector2 target = positions.GetValueOrDefault(entry.TargetId, p) + new Vector2(0, -18);
                if (entry.Type == CombatEventType.AttackStarted && tick < weapon.ImpactTick)
                {
                    float travel = (float)Math.Clamp((tick - weapon.ActionStartTick) / Math.Max(1, weapon.ImpactTick - weapon.ActionStartTick), 0, 1);
                    if (travel > .55f)
                    {
                        Vector2 shot = source.Lerp(target, (travel - .55f) / .45f);
                        DrawLine(shot - (target - source).Normalized() * 10, shot, _gold, 2, true);
                    }
                }
                if (entry.Type == CombatEventType.Impact && age < 9) DrawArc(target, (4 + age), 0, Mathf.Tau, 20, _gold with { A = 1 - age / 9 }, 2, true);
            }
            if (entry.Type == CombatEventType.DefenderShot && entry.Unit is { } victim && age < 8)
            {
                Vector2 source = entry.Structure is { } tower ? Cell(tower.Cell) : Cell(arena.Footprints.First(f => f.Kind == ArenaStructureKind.Watchpost).Cell);
                DrawLine(source + new Vector2(0, -18), positions.GetValueOrDefault(victim.Id, p) + new Vector2(0, -18), _gold with { A = 1 - age / 8 }, 2, true);
            }
            if ((entry.Type is CombatEventType.Hit or CombatEventType.StructureHit or CombatEventType.StructureDestroyed) && entry.Damage > 0)
            {
                // Every event retains its sequence, including simultaneous hits on one victim.
                if (age < 12) for (int i = 0; i < 5; i++)
                {
                    float angle = i * Mathf.Tau / 5 + entry.Sequence;
                    DrawCircle(p + (new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (2 + age * .9f) + new Vector2(0, -18)), 1.3f, _gold with { A = 1 - age / 12 });
                }
            }
            if (entry.Type == CombatEventType.StructureDestroyed && age < 30)
                for (int i = 0; i < 6; i++) DrawCircle(p + new Vector2((i - 3) * age * .2f, -10 + age * .2f), 2, _muted with { A = 1 - age / 30 });
        }
        _diagnostics.Text = $"sim {state.Tick}   presentation {tick:F2}   event {Playback.EventCursor}   effects {_motion.Events.Count}";
    }
    public UnitState? PickUnit(Vector2 point)
    {
        Vector2 center = ArenaCenter();
        return Playback.Units().Where(u => u.Health > 0 && u.Hex?.Lifecycle == UnitLifecycle.Alive)
            .Select(u => (Unit: u, Position: CombatVisuals.Position(u, CombatState, AnimationTick)))
            .Select(x => (x.Unit, Distance: (new Vector2(x.Position.X, x.Position.Y * _options.GroundCompression) - center - point).Length()))
            .Where(x => x.Distance < 24).OrderBy(x => x.Distance).Select(x => x.Unit).FirstOrDefault();
    }
    public StructureState? PickStructure(Vector2 point)
    {
        Vector2 center = ArenaCenter();
        return CombatState.Structures.FirstOrDefault(s => CombatState.Arena!.Board.Cells.FirstOrDefault(c => c.Id == s.Cell) is { } cell
            && (V(_arena.Project(cell.Coordinate)) - center).DistanceTo(point) < 24);
    }
    void DrawStatuses(UnitState unit, Vector2 p, double tick)
    {
        void Status(StatusEffect? effect, Color color, int index)
        {
            if (effect is null || tick < effect.AppliedTick || tick >= effect.ExpiresTick) return;
            for (int i = 0; i < 3; i++)
            {
                float phase = (float)((tick - effect.AppliedTick + i * 13 + unit.Id * 7) % 43) / 43;
                Vector2 at = p + new Vector2(MathF.Sin(phase * 6 + i + index) * 10, -4 - phase * 26);
                DrawCircle(at, (1 + (1 - phase)), color with { A = 1 - phase });
            }
            DrawArc(p, (11 + index * 2), 0, Mathf.Pi, 16, color with { A = .7f }, 1, true);
        }
        Status(unit.Statuses.Burn, new Color("e9a05d"), 0);
        Status(unit.Statuses.Poison.FirstOrDefault(), new Color("9ac86e"), 1);
        Status(unit.Statuses.Chill, new Color("8fcee7"), 2);
    }
}
