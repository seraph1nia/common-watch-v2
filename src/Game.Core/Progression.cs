namespace Game.Core;

public static class Progression
{
    public const int MaximumExponentLevel = 256;
    public const int BossHealthMultiplier = 8, BossDamageMultiplier = 2;
    public const decimal Growth = 1.35m;

    // Always scale the original base, then round once. No prior rounded level
    // participates in the next calculation; zero price components remain zero.
    public static int ScaleWhole(int baseAmount, int level, int multiple = 1)
    {
        if (baseAmount < 0 || level is < 1 or > MaximumExponentLevel || multiple < 1)
            throw new ArgumentOutOfRangeException(nameof(level));
        if (baseAmount == 0) return 0;
        decimal factor = 1;
        for (int exponent = 1; exponent < level; exponent++) factor = checked(factor * Growth);
        decimal rounded = decimal.Round(checked(baseAmount * factor) / multiple, 0, MidpointRounding.AwayFromZero);
        return checked((int)checked(rounded * multiple));
    }
}
