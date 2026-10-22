// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using Godot;

namespace Game.GodotClient;

public partial class SettlementView
{
    private readonly Dictionary<int, (string Text, Rect2 Bounds)> _levelCues = [];
    internal IReadOnlyDictionary<int, (string Text, Rect2 Bounds)> LevelCues => _levelCues;
    private void DrawBuildingProgress(int index, Vector2 ground)
    {
        var slot = _session.ViewedCity.Slots[index];
        if (slot.Type == Building.Empty) return;
        string text = "L" + slot.Level;
        var transform = GetGlobalTransformWithCanvas();
        float scale = Math.Max(.01f, Math.Abs(transform.Scale.X));
        var bounds = new Rect2(new Vector2(-17, 7), new Vector2(34, 20));
        _levelCues[index] = (text, new Rect2(transform * ground + bounds.Position, bounds.Size));
        // Screen-sized technical annotation; it changes no sprite, anchor or hit geometry.
        DrawSetTransform(ground, 0, Vector2.One / scale);
        DrawRect(bounds, new Color("102a29ed"));
        DrawRect(bounds, slot.Level > 1 ? _gold : new Color("9bb8a2"), false, 1, true);
        WorldArt.Text(this, text, new Vector2(0, 22), 13, slot.Level > 1 ? _gold : new Color("d8e3d5"));
        DrawSetTransform(Vector2.Zero);
    }
}
