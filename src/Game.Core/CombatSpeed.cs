namespace Game.Core;

// Tick durations and combat rules stay fixed. Only the authority's admission of
// wall time changes: 1x is half the former pace; 2x is the former pace.
public enum CombatSpeed { One, OneAndHalf, Two }
public static class CombatSpeedRates
{
    public static double Factor(CombatSpeed speed) => speed switch
    {
        CombatSpeed.One => .5,
        CombatSpeed.OneAndHalf => .75,
        CombatSpeed.Two => 1,
        _ => throw new ArgumentOutOfRangeException(nameof(speed))
    };
    public static string Label(CombatSpeed speed) => speed switch
    {
        CombatSpeed.One => "1x",
        CombatSpeed.OneAndHalf => "1.5x",
        CombatSpeed.Two => "2x",
        _ => throw new ArgumentOutOfRangeException(nameof(speed))
    };
    public static string Action(CombatSpeed speed) => "speed-" + Label(speed);
    public static bool TryAction(string action, out CombatSpeed speed)
    {
        speed = action switch { "speed-1x" => CombatSpeed.One, "speed-1.5x" => CombatSpeed.OneAndHalf, "speed-2x" => CombatSpeed.Two, _ => (CombatSpeed)(-1) };
        return Enum.IsDefined(speed);
    }
}
