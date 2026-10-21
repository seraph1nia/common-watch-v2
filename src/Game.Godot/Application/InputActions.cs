using Godot;

namespace Game.GodotClient;

public static class InputActions
{
    public static void Register()
    {
        foreach (var (name, key) in new (string, Key)[]
        {
            ("camera_reset", Key.R), ("game_pause", Key.Space), ("game_menu", Key.F10),
            ("show_debug_overlay", Key.O), ("toggle_art_mode", Key.T)
        })
        {
            if (InputMap.HasAction(name)) continue;
            InputMap.AddAction(name);
            InputMap.ActionAddEvent(name, new InputEventKey { PhysicalKeycode = key });
        }
        // Keep native keyboard defaults; supply controller activation/cancel explicitly.
        foreach (var (name, button) in new[] { ("ui_accept", JoyButton.A), ("ui_cancel", JoyButton.B) })
        {
            var input = new InputEventJoypadButton { ButtonIndex = button, Device = -1 };
            if (!InputMap.ActionHasEvent(name, input)) InputMap.ActionAddEvent(name, input);
        }
        var menu = new InputEventJoypadButton { ButtonIndex = JoyButton.Start, Device = -1 };
        if (!InputMap.ActionHasEvent("game_menu", menu)) InputMap.ActionAddEvent("game_menu", menu);
    }
}
