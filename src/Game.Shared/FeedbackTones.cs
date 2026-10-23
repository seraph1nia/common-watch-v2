// SPDX-License-Identifier: GPL-3.0-only
namespace Game.Shared;

/// <summary>Short technical earcons, synthesized in memory. No sound-asset or simulation RNG dependency.</summary>
public static class FeedbackTones
{
    public const int Rate = 44100;
    private static readonly double[] ProductionNotes = [330, 392, 494], VictoryNotes = [523.25, 659.25, 784, 1046.5];
    public static double Duration(FeedbackKind kind) => kind switch
    {
        FeedbackKind.Victory => .48, FeedbackKind.Defeat => .42, FeedbackKind.BossArrival => .36,
        FeedbackKind.WaveClear => .32, FeedbackKind.BattleStart => .28, FeedbackKind.Production => .24,
        FeedbackKind.Research => .23, FeedbackKind.Upgrade => .19, FeedbackKind.Rejected => .17,
        FeedbackKind.Structure => .16, FeedbackKind.Construction => .13, FeedbackKind.Spell => .14,
        FeedbackKind.Recruitment => .11, FeedbackKind.Impact => .075, FeedbackKind.Ranged => .065, _ => .06
    };
    public static byte[] Pcm(FeedbackKind kind)
    {
        double duration = Duration(kind); int samples = (int)(duration * Rate); var data = new byte[samples * 2];
        double phase = 0; uint noise = 0x9e3779b9u + (uint)kind;
        for (int i = 0; i < samples; i++)
        {
            double t = i / (double)Rate, x = t / duration;
            double frequency = kind switch
            {
                FeedbackKind.Construction => 180 + 100 * x, FeedbackKind.Recruitment => 520 + 180 * x,
                FeedbackKind.Upgrade => 330 * Math.Pow(2, x), FeedbackKind.Research => 880,
                FeedbackKind.Rejected => 260 - 140 * x, FeedbackKind.Production => ProductionNotes[Math.Min(2, (int)(x * 3))],
                FeedbackKind.BattleStart => 180 - 100 * x, FeedbackKind.Impact => 105 - 50 * x,
                FeedbackKind.Ranged => 1600 - 1000 * x, FeedbackKind.Spell => 740 + 370 * x,
                FeedbackKind.Structure => 85 - 30 * x, FeedbackKind.BossArrival => 110 + 55 * x,
                FeedbackKind.WaveClear => 392, FeedbackKind.Defeat => 220 * Math.Pow(.5, x),
                FeedbackKind.Victory => VictoryNotes[Math.Min(3, (int)(x * 4))], _ => 660
            };
            phase += 2 * Math.PI * frequency / Rate;
            noise ^= noise << 13; noise ^= noise >> 17; noise ^= noise << 5;
            double random = noise / (double)uint.MaxValue * 2 - 1;
            double tone = Math.Sin(phase) * .7 + Math.Sin(phase * 2) * .17;
            if (kind is FeedbackKind.WaveClear or FeedbackKind.Research or FeedbackKind.BossArrival) tone += Math.Sin(phase * 1.5) * .2;
            if (kind is FeedbackKind.Impact or FeedbackKind.Structure or FeedbackKind.Ranged or FeedbackKind.Construction or FeedbackKind.BattleStart) tone = tone * .65 + random * .24;
            double envelope = Math.Min(1, t / .006) * Math.Exp(-4 * x) * Math.Min(1, (duration - t) / .012);
            short sample = (short)Math.Round(Math.Clamp(tone * envelope * .22, -.3, .3) * short.MaxValue);
            data[i * 2] = (byte)(sample & 255); data[i * 2 + 1] = (byte)(sample >> 8);
        }
        return data;
    }
}
