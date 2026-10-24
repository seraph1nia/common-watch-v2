// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
namespace Game.Shared;

// Frozen41cc004 campaign DATA for the explicit comparison; never a shipping default.
internal static class CampaignExperimentBaseline
{
    public static CampaignDefinition Create()
    {
        SpawnEntry S(int count, int level) => new(UnitType.Swordsman, level, count);
        SpawnEntry B(int count, int level) => new(UnitType.Berserker, level, count);
        SpawnEntry C(int count, int level) => new(UnitType.Crossbowman, level, count);
        SpawnEntry M(int count, int level) => new(UnitType.Mage, level, count);
        WaveDefinition Wave(int number, params SpawnEntry[] entries) => new(number, false, entries, new(2, 1, 5));
        WaveDefinition Boss(int number, int level) => new(number, true, [new(UnitType.Swordsman, level, IsBoss: true)], new(4, 2, 10));
        return new([
            Wave(1, S(4, 1)), Wave(2, S(3, 1), B(1, 1)), Wave(3, S(3, 1), C(2, 1)),
            Wave(4, S(3, 1), B(1, 1), C(1, 1), M(1, 1)), Wave(5, S(3, 2), C(1, 1)),
            Wave(6, S(3, 2), B(1, 2), C(1, 1)), Wave(7, S(3, 2), C(2, 2), M(1, 1)),
            Wave(8, S(3, 2), B(2, 2), C(2, 2), M(1, 2)), Wave(9, S(3, 3), B(1, 2), C(1, 2), M(1, 2)),
            Boss(10, 3), Wave(11, S(3, 3), C(2, 2)), Wave(12, S(3, 3), B(1, 3), C(2, 3)),
            Wave(13, S(3, 3), C(2, 3), M(1, 3)), Wave(14, S(3, 3), B(2, 3), C(2, 3), M(1, 3)),
            Wave(15, S(3, 4), C(1, 3), M(1, 3)), Wave(16, S(3, 4), B(1, 4), C(2, 4)),
            Wave(17, S(3, 4), C(2, 4), M(1, 4)), Wave(18, S(3, 4), B(2, 4), C(2, 4), M(1, 4)),
            Wave(19, S(3, 5), B(1, 4), C(2, 4), M(1, 4)), Boss(20, 5)
        ]);
    }
}
