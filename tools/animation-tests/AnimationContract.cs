using Game.Core;
using Game.Shared;
using System.Numerics;
using Xunit;
#if GODOT_ANIMATION
using Motion = Game.GodotClient.CombatMotion;
#else
using Motion = Game.MonoGameClient.CombatMotion;
#endif

namespace Game.Animation.Tests;

public class AnimationContract
{
    static readonly Lazy<BattleShowcase> recording = new(() => new("swordsman"));
    static UnitState Attacker => recording.Value.Frames.SelectMany(f => f.CombatEvents).First(e => e.Type == CombatEventType.AttackStarted && e.Unit?.Faction == Faction.Adventurers).Unit!;
    [Fact]
    public void IdleRemainsBoundedAndDeterministic()
    {
        var motion = new Motion();
        var unit = Attacker with { AttackSequence = 0, Hex = Attacker.Hex! with { Action = UnitActionKind.Waiting } };
        for (double tick = 0; tick < 1200; tick += .5)
        {
            var pose = motion.Sample(unit, tick, Vector2.UnitX);
            Assert.InRange(pose.Offset.Length(), 0, .56f);
            Assert.InRange(pose.Rotation, -.02f, .02f);
            Assert.InRange(pose.Scale, .99f, 1.01f);
            Assert.Equal(pose, motion.Sample(unit, tick, Vector2.UnitX));
        }
    }
    [Fact]
    public void MeleeAnticipatesThenLungesAtAuthoritativeImpactAndRecovers()
    {
        var motion = new Motion(); var unit = Attacker;
        double windup = unit.ImpactTick - unit.ActionStartTick;
        Assert.True(motion.Sample(unit, unit.ActionStartTick + windup * .4, Vector2.UnitX).Offset.X < 0);
        Assert.True(motion.Sample(unit, unit.ImpactTick, Vector2.UnitX).Offset.X > 7);
        Assert.Equal(0, motion.Sample(unit, unit.ReadyTick + 1, Vector2.UnitX).Offset.X);
        Assert.Equal(0, motion.Sample(unit, unit.ImpactTick - .01, Vector2.UnitX).Flash);
    }
    [Fact]
    public void SimultaneousHitsAreRetainedAndDeduplicatedAndFlashExpires()
    {
        var motion = new Motion(); var unit = Attacker;
        var hit = new CombatEvent(1, 100, CombatEventType.Hit, unit, Damage: 100);
        motion.Observe(hit); motion.Observe(hit);
        motion.Observe(hit with { Sequence = 2, Damage = 50 });
        Assert.Equal(2, motion.Events.Count);
        Assert.Equal(0, motion.Sample(unit, 99.99, Vector2.UnitX).Flash);
        Assert.True(motion.Sample(unit, 100, Vector2.UnitX).Flash > 0);
        Assert.Equal(0, motion.Sample(unit, 112, Vector2.UnitX).Flash);
        motion.Trim(161); Assert.Empty(motion.Events);
    }
    [Fact]
    public void DeathOverridesRecoveryAndFinishesAtCoreBoundary()
    {
        var death = recording.Value.Frames.SelectMany(f => f.CombatEvents).First(e => e.Type == CombatEventType.Death).Unit!;
        var motion = new Motion();
        Assert.Equal(1, motion.Sample(death, death.Hex!.DeathStartTick, Vector2.UnitX).Opacity);
        Assert.InRange(motion.Sample(death, (death.Hex.DeathEndTick + death.Hex.DeathStartTick) / 2.0, Vector2.UnitX).Opacity, .1f, .9f);
        Assert.Equal(0, motion.Sample(death, death.Hex.DeathEndTick, Vector2.UnitX).Opacity);
        motion.Clear(); Assert.Empty(motion.Events);
    }
    [Theory]
    [InlineData(30)] [InlineData(60)] [InlineData(120)]
    public void PoseAndPlaybackStayIndependentOfRenderingRate(int fps)
    {
        foreach (var speed in Enum.GetValues<CombatSpeed>())
        {
            var battle = new BattleShowcase("swordsman") { Speed = speed };
            var motion = new Motion();
            int guard = fps * 40;
            while (!battle.Complete && guard-- > 0)
            {
                battle.Advance(1.0 / fps);
                foreach (var entry in battle.Playback.Drain()) motion.Observe(entry);
                motion.Trim(battle.Playback.Tick);
            }
            Assert.True(battle.Complete);
            Assert.Equal(battle.Frames[^1].EventSequence, battle.Playback.EventCursor);
            motion.Trim(battle.Playback.Tick + 61); Assert.Empty(motion.Events);
        }
    }
    [Fact]
    public void PauseSpeedChangesRestartAndBaselineClearVisuals()
    {
        var battle = new BattleShowcase("swordsman");
        var events = battle.Seek(74); var motion = new Motion();
        foreach (var e in events) motion.Observe(e);
        var tick = battle.Playback.Tick;
        var pose = motion.Sample(Attacker, tick, Vector2.UnitX);
        for (int i = 0; i < 100; i++) battle.Advance(.016);
        Assert.Equal(tick, battle.Playback.Tick);
        Assert.Equal(pose, motion.Sample(Attacker, battle.Playback.Tick, Vector2.UnitX));
        battle.Speed = CombatSpeed.Two; battle.Paused = false; battle.Advance(.1);
        Assert.InRange(battle.Playback.Tick - tick, 5.9, 6.1);
        battle.Restart(); motion.Clear();
        Assert.Empty(motion.Events); Assert.Equal(0, battle.Playback.Tick);
    }
}
