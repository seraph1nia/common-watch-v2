using Game.Core;

namespace Game.Shared;

/// <summary>Recorded legal campaign commands, sampled through the upstream playback policy.</summary>
public sealed class BattleShowcase
{
    public IReadOnlyList<MatchSnapshot> Frames { get; }
    public CombatPlayback Playback { get; private set; } = new();
    public MatchSnapshot Snapshot { get; private set; }
    public string Scenario { get; }
    public bool Paused { get; set; }
    public CombatSpeed Speed { get; set; } = CombatSpeed.One;
    public bool Complete => Playback.Tick >= Frames[^1].Tick;
    int frame;
    double credit;
    public BattleShowcase(string scenario, int players = 1)
    {
        Scenario = scenario;
        Frames = scenario == "arena" ? ArenaShowcase.Record(players) : Record(scenario);
        Snapshot = Frames[0];
        Playback.Accept(Snapshot, true);
    }
    public void Restart()
    {
        frame = 0; credit = 0; Snapshot = Frames[0];
        // New playback identity ensures both engine visual stores clear on restart.
        Playback = new(); Playback.Accept(Snapshot, true);
    }
    public void Advance(double seconds)
    {
        if (Paused) return;
        if (Complete) { Playback.Advance(seconds, true); return; }
        credit += Math.Clamp(seconds, 0, .1) * Match.StepsPerSecond * CombatSpeedRates.Factor(Speed);
        while (credit >= 1 && frame + 1 < Frames.Count)
        {
            Snapshot = Frames[++frame]; Playback.Accept(Snapshot); credit--;
        }
        Playback.Advance(seconds * CombatSpeedRates.Factor(Speed) / CombatSpeedRates.Factor(Snapshot.CombatSpeed), true);
    }
    /// <summary>Replay every intervening authoritative event; seeking never fabricates poses or hits.</summary>
    public CombatEvent[] Seek(double tick)
    {
        Paused = true; Restart();
        tick = Math.Clamp(tick, Frames[0].Tick, Frames[^1].Tick);
        var events = new List<CombatEvent>();
        while (frame + 1 < Frames.Count && Snapshot.Tick < tick)
        {
            Snapshot = Frames[++frame]; Playback.Accept(Snapshot);
            double steps = Math.Min(Snapshot.Tick, tick) - Playback.Tick;
            Playback.Advance(steps / Match.StepsPerSecond / CombatSpeedRates.Factor(Snapshot.CombatSpeed), true);
            events.AddRange(Playback.Drain());
        }
        credit = tick - Math.Floor(tick);
        return events.ToArray();
    }
    static IReadOnlyList<MatchSnapshot> Record(string scenario)
    {
        if (scenario is not ("swordsman" or "ranged" or "status" or "burn" or "chill" or "structures")) throw new ArgumentException("Scenario: swordsman, ranged, status, burn, chill or structures");
        bool mageStatuses = scenario is "burn" or "chill";
        using var solo = ScenarioRunner.CreateAt(scenario == "structures" ? "initial" : "battle-start", doctrine: mageStatuses ? "arcanist" : null);
        if (scenario == "structures")
        {
            Require(solo.Send("build", 9, Building.ArrowTower));
            for (int n = 0; n < 3; n++) Require(solo.Send("ready"));
        }
        if (scenario is not ("swordsman" or "structures"))
        {
            // Default rules, economy, army capacity and research prices. No seed/state injection.
            for (int wave = 1; wave < 12 && !ReadyForScenario(solo, scenario); wave++)
            {
                while (solo.Snapshot.Phase == Phase.Combat) solo.Step();
                if (solo.Snapshot.Phase != Phase.RewardDraft) throw new InvalidOperationException($"Legal showcase opening lost before advanced wave: wave {solo.Snapshot.Wave}, {solo.Snapshot.Phase}, city HP {solo.City.Health}, roster {solo.City.Soldiers.Length}.");
                solo.ChooseReward(solo.City.Run.Offers.Contains("F05") ? "F05" : solo.City.Run.Offers[0]);
                while (solo.Snapshot.DyingBodies.Length > 0) solo.Step();
                for (int preparation = 0; preparation < 3; preparation++)
                {
                    // Rejected optional purchases do not change state. Accepted ones use normal commands.
                    if (mageStatuses)
                    {
                        solo.Send("build", 4, Building.Arcanum);
                        solo.Send("buy-plot", 11);
                        solo.Send("buy-plot", 5); solo.Send("build", 5, Building.Weaver);
                        solo.Send("buy-plot", 6); solo.Send("build", 6, Building.Stonecutter);
                        solo.Send("buy-plot", 7); solo.Send("build", 7, Building.ResearchTower);
                        for (int n = 0; n < 3; n++) solo.Send("recruit", 4, soldierType: UnitType.Mage);
                        if (solo.City.Slots[1].Level < 2) solo.Send("upgrade", 1);
                        for (int n = 0; n < 3; n++) solo.Send("recruit", 1);
                        solo.Send("research-tech", technology: TechnologyId.MagicFoundation);
                        solo.Send("research-tech", technology: scenario == "burn" ? TechnologyId.Fire : TechnologyId.Frost);
                    }
                    else
                    {
                        solo.Send("build", 4, Building.ArcheryRange);
                        solo.Send("buy-plot", 11);
                        solo.Send("buy-plot", 5);
                        solo.Send("build", 5, Building.ResearchTower);
                        if (solo.City.Slots[1].Level < 2) solo.Send("upgrade", 1);
                        for (int n = 0; n < 3; n++) solo.Send("recruit", 4, soldierType: UnitType.Crossbowman);
                        for (int n = 0; n < 3; n++) solo.Send("recruit", 1);
                        solo.Send("research-tech", technology: TechnologyId.RangedFoundation);
                        solo.Send("research-tech", technology: TechnologyId.Venom);
                    }
                    Require(solo.Send("ready"));
                }
            }
        }
        if (scenario is not ("swordsman" or "structures") && !ReadyForScenario(solo, scenario)) throw new InvalidOperationException("Could not earn required showcase capabilities through the legal opening: " + scenario);
        var frames = new List<MatchSnapshot>();
        long cursor = solo.Snapshot.EventSequence;
        frames.Add(solo.Snapshot with { CombatEvents = [] });
        for (int i = 0; i < 18000 && (solo.Snapshot.Phase == Phase.Combat || solo.Snapshot.DyingBodies.Length > 0); i++)
        {
            solo.Step();
            MatchSnapshot state = solo.Snapshot;
            frames.Add(state with { CombatEvents = state.CombatEvents.Where(e => e.Sequence > cursor).ToArray() });
            cursor = state.EventSequence;
        }
        if (frames[^1].Phase == Phase.Combat) throw new InvalidOperationException("Showcase exceeded authoritative battle limit.");
        return frames;
    }
    static bool ReadyForScenario(SoloSession solo, string scenario) => scenario switch
    {
        "ranged" => solo.Snapshot.Wave >= 4,
        "status" => solo.City.Soldiers.Any(u => u.Capabilities.PoisonPercent > 0),
        "burn" => solo.City.Soldiers.Any(u => u.Capabilities.BurnPercent > 0),
        "chill" => solo.City.Soldiers.Any(u => u.Capabilities.ChillPercent > 0),
        _ => true
    };
    static void Require(CommandResult result) { if (!result.Accepted) throw new InvalidOperationException(result.Message); }
}
