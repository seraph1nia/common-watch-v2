using Game.Core;
using Game.Shared;
using Microsoft.Xna.Framework;

namespace Game.MonoGameClient;

public sealed partial class WatchGame
{
    CombatPlayback? visualPlayback;
    int visualGeneration, visualResets, totalAnimatedHits;
    BattleShowcase? battle;
    bool finishingCombat;
    bool PresentingCombat => battle is not null || session.Snapshot.Phase == Phase.Combat || finishingCombat;
    MatchSnapshot CombatState => battle?.Snapshot ?? session.Snapshot;
    CombatPlayback Playback => battle?.Playback ?? session.Playback;
    double terminalTick = -1, terminalSeconds;
    double AnimationTick
    {
        get
        {
            if (CombatState.Phase == Phase.Combat || Playback.Tick < CombatState.Tick) { terminalTick = -1; return Playback.Tick; }
            if (terminalTick != Playback.Tick) { terminalTick = Playback.Tick; terminalSeconds = Playback.VisualSeconds; }
            return Playback.Tick + (Playback.VisualSeconds - terminalSeconds) * Match.StepsPerSecond;
        }
    }
    bool CombatShader => Arg("--combat-shader") != "off";
    void ObserveCombat()
    {
        if (visualPlayback != Playback || visualGeneration != Playback.Generation)
        { finishingCombat = CombatState.Phase == Phase.Combat; visualResets++; motion.Clear(); terminalTick = -1; visualPlayback = Playback; visualGeneration = Playback.Generation; }
        foreach (var entry in Playback.Drain())
        {
            motion.Observe(entry);
            if (entry.Type == CombatEventType.Hit && entry.Damage > 0) totalAnimatedHits++;
        }
        motion.Trim(AnimationTick);
        if (CombatState.Phase == Phase.Combat) finishingCombat = true;
        else if (Playback.Tick >= CombatState.Tick && !Playback.Units().Any(u => u.Hex?.Lifecycle == UnitLifecycle.Dying)) finishingCombat = false;
    }
    void DrawAnimatedCombat()
    {
        var snapshot = CombatState;
        if (snapshot.Arena is not { } arena) return;
        double tick = AnimationTick;
        Vector2 Project(System.Numerics.Vector2 p) { p.Y *= options.GroundCompression; return Screen(p); }
        Vector2 Ground(UnitState u) => Project(CombatVisuals.Position(u, snapshot, tick, combatProjection.Radius));
        Vector2 Cell(int cell) => arena.Board.Cells.FirstOrDefault(c => c.Id == cell) is { } c ? Screen(combatProjection.Project(c.Coordinate)) : Vector2.Zero;
        foreach (var cell in arena.Board.Cells)
        {
            canvas.Polygon(combatProjection.Corners(cell.Coordinate).Select(Screen).ToArray(), new(36, 56, 54), new(63, 77, 76), 1);
            WorldSprite("terrain/woodland_hex", Cell(cell.Id), 54 * cameraZoom, new Color(92, 110, 102));
        }
        foreach (var structure in Playback.Structures())
        {
            // Keep snapshots omit DestroyedTick; use the actual destruction event when present.
            long destroyedAt = structure.DestroyedTick > 0 ? structure.DestroyedTick : motion.Events.LastOrDefault(e => e.Type == CombatEventType.StructureDestroyed && e.Structure is { } hit && hit.Owner == structure.Owner && hit.Kind == structure.Kind && hit.Slot == structure.Slot)?.Tick ?? 0;
            float fade = structure.Health > 0 ? 1 : Math.Clamp(1 - (float)(tick - destroyedAt) / 30, 0, 1);
            if (!structure.Active && structure.Health > 0 || fade <= 0) continue;
            Vector2 p = Cell(structure.Cell);
            canvas.Rect(p.X - 12 * cameraZoom, p.Y - 18 * cameraZoom, 24 * cameraZoom, 23 * cameraZoom, new Color(168, 160, 132) * fade);
            WorldLabel(structure.Kind + " " + HealthPoints.Format(structure.Health), p + new Vector2(0, 17 * cameraZoom), 130, .55f);
        }
        var units = Playback.Units().Where(u => u.Hex?.Lifecycle is UnitLifecycle.Alive or UnitLifecycle.Dying).OrderBy(u => Ground(u).Y).ThenBy(u => u.Id).ToArray();
        var positions = units.ToDictionary(u => u.Id, Ground);
        Vector2 Aim(UnitState actor)
        {
            if (actor.TargetCity || actor.TargetKind is SquadTargetKind.Keep or SquadTargetKind.Watchpost)
            {
                var kind = actor.TargetKind == SquadTargetKind.Watchpost ? ArenaStructureKind.Watchpost : ArenaStructureKind.Keep;
                return Cell(Playback.Structures().FirstOrDefault(s => s.Owner == actor.TargetId && s.Kind == kind)?.Cell ?? actor.Hex!.Position.Cell);
            }
            if (actor.Hex?.FrozenAim is { } frozen) return Project(CombatVisuals.Point(frozen, snapshot, combatProjection.Radius));
            return positions.GetValueOrDefault(actor.TargetId, Ground(actor));
        }
        foreach (var unit in units)
        {
            Vector2 ground = positions[unit.Id], aim = Aim(unit);
            Vector2 direction = aim - ground;
            if (unit.Hex is { Action: UnitActionKind.Moving } move) direction = Cell(move.Destination.Cell) - Cell(move.Position.Cell);
            FigurePose pose = motion.Sample(unit, tick, direction.ToNumerics());
            canvas.Circle(ground, 9 * cameraZoom, new Color(28, 35, 35) * pose.Opacity);
            canvas.Ring(ground, 9 * cameraZoom, new Color(unit.Faction == Faction.Adventurers ? 84 : 136, unit.Faction == Faction.Adventurers ? 121 : 86, unit.Faction == Faction.Adventurers ? 140 : 73) * pose.Opacity);
            Vector2 p = ground + pose.Offset.ToXna() * cameraZoom;
            bool useShader = CombatShader && pose.Flash > .001f;
            if (useShader) canvas.HitShader(pose.Flash * .6f);
            canvas.Transform(p, pose.Rotation, cameraZoom * pose.Scale);
            Color tint = (useShader ? Color.White : Color.Lerp(Color.White, new Color(255, 170, 170), pose.Flash)) * pose.Opacity;
            string id = VisualAssets.UnitId(unit);
            var texture = sprites.Get(id);
            if (texture is not null) canvas.Figure(texture, sprites.Catalog.Metadata(id), tint, .86f);
            else DrawDebugFigure(unit, pose, useShader);
            canvas.ResetTransform();
            if (useShader) canvas.EndHitShader();
            Color color = unit.Faction == Faction.Adventurers ? new(128, 187, 171) : new(206, 147, 130);
            float hp = Math.Clamp((float)unit.Health / Math.Max(1, unit.Profile.Health), 0, 1);
            canvas.Rect(p.X - 10 * cameraZoom, p.Y - 45 * cameraZoom, 20 * cameraZoom, 3 * cameraZoom, new(17, 28, 36));
            canvas.Rect(p.X - 10 * cameraZoom, p.Y - 45 * cameraZoom, 20 * hp * cameraZoom, 3 * cameraZoom, color);
            WorldLabel((unit.Faction == Faction.Adventurers ? "S" : "E") + unit.Id, ground + new Vector2(0, 8 * cameraZoom), 60, .5f);
            DrawStatuses(unit, ground, tick);
            if (unit.Type == UnitType.Mage && tick >= unit.ActionStartTick && tick < unit.ImpactTick && unit.PendingImpact)
                canvas.Ring(p + new Vector2(0, -22) * cameraZoom, (5 + (float)(tick - unit.ActionStartTick) / Math.Max(1, unit.ImpactTick - unit.ActionStartTick) * 7) * cameraZoom, new(167, 156, 232), width: 1.5f);
            if (unit.Type is UnitType.Crossbowman or UnitType.Mage && tick >= unit.ActionStartTick && tick < unit.ImpactTick && unit.PendingImpact)
            {
                float travel = (float)(tick - unit.ActionStartTick) / Math.Max(1, unit.ImpactTick - unit.ActionStartTick);
                if (travel > .55f)
                {
                    float t = (travel - .55f) / .45f;
                    Vector2 from = ground + new Vector2(0, -20) * cameraZoom, to = aim + new Vector2(0, -18) * cameraZoom;
                    Vector2 shot = Vector2.Lerp(from, to, t);
                    Color shotColor = unit.Type == UnitType.Mage ? new(178, 162, 242) : new(242, 217, 162);
                    canvas.Line(shot - Vector2.Normalize(to - from) * 10 * cameraZoom, shot, shotColor, 2 * cameraZoom);
                    if (unit.Type == UnitType.Mage) canvas.Circle(shot, 3 * cameraZoom, shotColor);
                }
            }
            if (options.DebugOverlay)
            {
                canvas.Line(ground, aim, new Color(239, 212, 120) * .35f, 1);
                canvas.Ring(ground, 3, new(255, 130, 206));
                var meta = sprites.Catalog.Metadata(id);
                Vector2 size = texture is null ? new(26, 36) : new Vector2(texture.Width, texture.Height) * (meta.DisplaySize * .86f / Math.Max(texture.Width, texture.Height));
                Vector2 min = texture is null ? new(-12, -34) : -size * new Vector2(meta.Anchor[0], meta.Anchor[1]) + new Vector2(meta.Offset[0], meta.Offset[1]);
                Vector2 max = min + size;
                Vector2 At(Vector2 local) => p + Vector2.Transform(local * pose.Scale * cameraZoom, Matrix.CreateRotationZ(pose.Rotation));
                canvas.Line(At(min), At(new(max.X, min.Y)), color, 1); canvas.Line(At(new(max.X, min.Y)), At(max), color, 1);
                canvas.Line(At(max), At(new(min.X, max.Y)), color, 1); canvas.Line(At(new(min.X, max.Y)), At(min), color, 1);
                WorldLabel($"#{unit.Id} sq{unit.SquadId} {unit.Hex!.Action} >{unit.TargetId} a{unit.AttackSequence}", ground + new Vector2(0, 25 * cameraZoom), 210, .48f);
            }
        }
        foreach (var entry in motion.Events)
        {
            float age = (float)(tick - entry.Tick); if (age < 0 || age > 35) continue;
            Vector2 p = entry.Unit is { } u ? Project(CombatVisuals.Position(u, snapshot, entry.Tick, combatProjection.Radius)) : entry.ImpactPose is { } impact ? Project(CombatVisuals.Point(impact, snapshot, combatProjection.Radius)) : Vector2.Zero;
            if (entry.Type is CombatEventType.StructureHit or CombatEventType.StructureDestroyed && entry.Structure is { } structure) p = Cell(structure.Cell);
            if (entry.Type == CombatEventType.Impact && entry.Unit is { } attacker && age < 10)
            {
                p = Aim(attacker);
                Color color = (attacker.Type == UnitType.Mage ? new Color(187, 167, 239) : new Color(236, 213, 164)) * (1 - age / 10);
                canvas.Ring(p + new Vector2(0, -16) * cameraZoom, (8 + age * 1.2f) * cameraZoom, color, -.7f, 1.5f, 2 * cameraZoom);
                if (attacker.Type == UnitType.Mage) canvas.Ring(p, (6 + age * 2) * cameraZoom, color, width: 1.5f);
            }
            if (entry.Tower is { } weapon)
            {
                var footprint = arena.Footprints.FirstOrDefault(f => f.Owner == weapon.City && (weapon.Slot < 0 ? f.Kind == ArenaStructureKind.Watchpost : f.Slot == weapon.Slot));
                Vector2 source = Cell(footprint?.Cell ?? 0) + new Vector2(0, -18) * cameraZoom;
                Vector2 target = positions.GetValueOrDefault(entry.TargetId, p) + new Vector2(0, -18) * cameraZoom;
                if (entry.Type == CombatEventType.AttackStarted && tick < weapon.ImpactTick)
                {
                    float travel = (float)Math.Clamp((tick - weapon.ActionStartTick) / Math.Max(1, weapon.ImpactTick - weapon.ActionStartTick), 0, 1);
                    if (travel > .55f)
                    {
                        Vector2 shot = Vector2.Lerp(source, target, (travel - .55f) / .45f);
                        canvas.Line(shot - Vector2.Normalize(target - source) * 10 * cameraZoom, shot, new(239, 212, 120), 2 * cameraZoom);
                    }
                }
                if (entry.Type == CombatEventType.Impact && age < 9) canvas.Ring(target, (4 + age) * cameraZoom, new Color(239, 212, 120) * (1 - age / 9), width: 2);
            }
            if (entry.Type == CombatEventType.DefenderShot && entry.Unit is { } victim && age < 8)
            {
                Vector2 source = entry.Structure is { } tower ? Cell(tower.Cell) : Cell(arena.Footprints.First(f => f.Kind == ArenaStructureKind.Watchpost).Cell);
                canvas.Line(source + new Vector2(0, -18) * cameraZoom, positions.GetValueOrDefault(victim.Id, p) + new Vector2(0, -18) * cameraZoom, new Color(239, 212, 120) * (1 - age / 8), 2 * cameraZoom);
            }
            if ((entry.Type is CombatEventType.Hit or CombatEventType.StructureHit or CombatEventType.StructureDestroyed) && entry.Damage > 0)
            {
                Vector2 n = p + new Vector2((entry.Sequence % 3 - 1) * 9, -48 - age * .55f) * cameraZoom;
                WorldLabel("-" + HealthPoints.Format(entry.Damage), n, 90, .65f);
                if (age < 12) for (int i = 0; i < 5; i++)
                {
                    float angle = i * MathF.Tau / 5 + entry.Sequence;
                    canvas.Circle(p + (new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (2 + age * .9f) + new Vector2(0, -18)) * cameraZoom, 1.3f * cameraZoom, new Color(239, 212, 120) * (1 - age / 12));
                }
            }
            if (entry.Type == CombatEventType.StructureDestroyed && age < 30)
                for (int i = 0; i < 6; i++) canvas.Circle(p + new Vector2((i - 3) * age * .2f, -10 + age * .2f) * cameraZoom, 2 * cameraZoom, new Color(169, 188, 199) * (1 - age / 30));
        }
        if (options.DebugOverlay) WorldLabel($"sim {snapshot.Tick}   presentation {tick:F2}   event {Playback.EventCursor}   effects {motion.Events.Count}", new((Window.ClientBounds.Width - SidebarWidth) / 2f, 142), 800, .65f);
    }
    void DrawDebugFigure(UnitState unit, FigurePose pose, bool shader)
    {
        Color Tint(Color c) => (shader ? c : Color.Lerp(c, Color.White, pose.Flash)) * pose.Opacity;
        Color body = Tint(unit.Faction == Faction.Adventurers ? new(123, 170, 208) : new(187, 119, 102));
        canvas.Line(new(-4, -9), new(-5, -1), Tint(new(185, 177, 160)), 4);
        canvas.Line(new(4, -9), new(6, -1), Tint(new(185, 177, 160)), 4);
        canvas.Polygon([new(-8, -10), new(-6, -24), new(5, -24), new(8, -10)], body, body, 1);
        canvas.Circle(new(0, -28), 5, Tint(new(212, 197, 162)));
        canvas.Line(new(-4, -30), new(4, -30), Tint(new(228, 233, 232)), 3);
        if (unit.Type == UnitType.Mage)
        { canvas.Line(new(9, 0), new(9, -30), Tint(new(186, 173, 138)), 2); canvas.Circle(new(9, -31), 3, Tint(new(161, 146, 247))); }
        else if (unit.Type == UnitType.Crossbowman)
        { canvas.Line(new(-9, -19), new(10, -19), Tint(new(220, 190, 134)), 3); canvas.Line(new(7, -25), new(7, -13), Tint(new(243, 227, 189)), 2); }
        else
        { canvas.Line(new(8, -13), new(12, -32), Tint(new(225, 229, 223)), 2); canvas.Polygon([new(-10, -22), new(-3, -22), new(-3, -13), new(-7, -10), new(-11, -15)], body * .75f, body, 1); }
    }
    void DrawStatuses(UnitState unit, Vector2 p, double tick)
    {
        void Status(StatusEffect? effect, Color color, int index)
        {
            if (effect is null || tick < effect.AppliedTick || tick >= effect.ExpiresTick) return;
            for (int i = 0; i < 3; i++)
            {
                float phase = (float)((tick - effect.AppliedTick + i * 13 + unit.Id * 7) % 43) / 43;
                canvas.Circle(p + new Vector2(MathF.Sin(phase * 6 + i + index) * 10, -4 - phase * 26) * cameraZoom, (2 - phase) * cameraZoom, color * (1 - phase));
            }
            canvas.Ring(p, (11 + index * 2) * cameraZoom, color * .7f, 0, MathF.PI);
        }
        Status(unit.Statuses.Burn, new(233, 160, 93), 0);
        Status(unit.Statuses.Poison.FirstOrDefault(), new(154, 200, 110), 1);
        Status(unit.Statuses.Chill, new(143, 206, 231), 2);
    }
}
