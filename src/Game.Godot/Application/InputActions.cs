using Godot;

namespace Game.GodotClient;

public static class InputActions
{
    public static void Register()
    {
        foreach (var (name, key) in new (string, Key)[]
        {
            ("camera_reset", Key.R), ("game_pause", Key.Space),
            ("show_debug_overlay", Key.O), ("toggle_art_mode", Key.T)
        })
        {
            if (InputMap.HasAction(name)) continue;
            InputMap.AddAction(name);
            InputMap.ActionAddEvent(name, new InputEventKey { PhysicalKeycode = key });
        }
    }
}
