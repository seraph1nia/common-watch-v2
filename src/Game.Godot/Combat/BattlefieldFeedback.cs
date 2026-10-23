// SPDX-License-Identifier: GPL-3.0-only
using Godot;
using Game.Shared;

namespace Game.GodotClient;

public partial class BattlefieldView
{
    internal Vector2? FeedbackPoint(int cell)
    {
        if (CombatState.Arena is not { } arena) return null;
        var definition = arena.Board.Cells.FirstOrDefault(c => c.Id == cell);
        if (definition is null) return null;
        var layout = new ArenaLayout(arena, _arena); var p = _arena.Project(definition.Coordinate);
        return new(p.X - layout.Center.X, p.Y - layout.Center.Y);
    }
}
