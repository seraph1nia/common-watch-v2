// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
namespace Game.Shared.Reviews;

/// <summary>Ordinary commands and exact detached observations; never writes a Match snapshot.</summary>
public sealed class ReviewSession : IDisposable
{
    private readonly Match _match;
    private readonly LegalRecorder? _recorder;
    private long _sequence;
    public MatchSnapshot State => _match.Snapshot();
    public int Player { get; }
    public CityState City => State.Players.Single(p => p.Id == Player);
    public ReviewSession(ulong seed, string identity, string doctrine = "warden", Rules? rules = null, string? directory = null)
    {
        _match = new(rules, identity, seed); Player = _match.Join()!.Id;
        if (directory is not null) _recorder = new(directory, State);
        Send("doctrine:" + doctrine); Send("start");
    }
    public void Act(GameplayIntent intent) => Apply(intent.Capture(State, ++_sequence, Player));
    public void Send(string action) => Apply(Command.FromSnapshot(State, ++_sequence, action, Player));
    private void Apply(Command command)
    {
        var before = State; long tick = before.Tick; var result = _match.Apply(Player, command);
        _recorder?.Command(tick, Player, command, result, State);
        if (result.Accepted && before.Phase != Phase.Combat && State.Phase == Phase.Combat)
        {
            string[] Manifest(IEnumerable<UnitState> units)=>units.Select(u=>$"{u.Type}/{u.Level}/{u.Rank}/{u.IsBoss}/{u.Origin}/{u.Destination}/{u.Profile}").Order(StringComparer.Ordinal).ToArray();
            if (!Manifest(before.NextWave).SequenceEqual(Manifest(State.Enemies)) || before.Players.Any(c => c.FoodForecast is { } f
                && (!f.Participating.SequenceEqual(State.Players.Single(p=>p.Id==c.Id).LastUpkeep!.Participating) || f.Paid!=State.Players.Single(p=>p.Id==c.Id).LastUpkeep!.Paid)))
                throw new InvalidOperationException("Forecast/upkeep differs from actual combat entry.");
        }
        if (!result.Accepted) throw new InvalidOperationException($"{State.MatchId}/{State.Wave}/P{State.Turn}: {command}: {result.Message}");
    }
    public void Prepare(string family, CampaignCalibrationPlan? calibration = null)
    {
        for (int n = 0; n < 150; n++)
        {
            var intent = calibration is not null ? CampaignCalibration.Next(State, Player, family, calibration) : State.Wave == 1 ? OpeningCampaign.Next(State, Player) : GameplayScenarios.NextPreparation(State, Player, family);
            if (intent is null) return;
            Act(intent);
        }
        throw new InvalidOperationException("Preparation command bound exceeded.");
    }
    public void Reach(int wave, string family, bool prepareFinal = false)
    {
        for (int n = 0; n < 70; n++)
        {
            if (State.Phase is Phase.Defeat or Phase.Victory) throw new InvalidOperationException($"Prefix ended at wave{State.Wave}/{State.Phase}");
            if (State.Phase == Phase.RewardDraft) Send("reward:" + GameplayScenarios.RewardChoice(City.Run));
            if (State.Wave == wave && (!prepareFinal || State.Turn == State.Rules.PreparationStages))
            { if (prepareFinal) Prepare(family); _recorder?.Checkpoint("decision", State); return; }
            Prepare(family); Send("ready"); if (State.Phase == Phase.Combat) Fight();
        }
        throw new InvalidOperationException("Prefix boundary bound exceeded.");
    }
    public BattleObservation Fight()
    {
        var before = State; if (before.Phase != Phase.Combat) throw new InvalidOperationException("Not in combat.");
        _recorder?.BeginEncounter(before); var events = new List<CombatEvent>(); long cursor = before.CombatEvents.Last(e => e.Type == CombatEventType.WaveStarted).Sequence - 1;
        long resolved = -1;
        for (int n = 0; n < 18000 && (State.Phase == Phase.Combat || State.DyingBodies.Length > 0); n++)
        {
            foreach (var e in State.CombatEvents.Where(e => e.Sequence > cursor)) { events.Add(e); cursor = e.Sequence; }
            _match.Step(); _recorder?.Observe(State);
            if (resolved < 0 && State.Phase != Phase.Combat) resolved = State.Tick;
        }
        foreach (var e in State.CombatEvents.Where(e => e.Sequence > cursor)) events.Add(e);
        var after = State; if (after.Phase == Phase.Combat || after.DyingBodies.Length != 0) throw new InvalidOperationException("Battle bound exceeded.");
        _recorder?.EndEncounter(after);
        foreach(var a in before.Players)
        {
            var b=after.Players.Single(c=>c.Id==a.Id);
            if(b.LastBattleLoss is not { } loss || loss.Wave!=before.Wave || loss.Participants!=a.LastUpkeep!.Participating.Length || loss.Survivors!=a.LastUpkeep.Participating.Count(id=>b.Soldiers.Any(u=>u.Id==id)))
                throw new InvalidOperationException("Frozen outcome disagrees with actual participants/survivors.");
            if(b.LastReward?.Wave==before.Wave && (!a.Resources.TryAdd(b.LastReward.Amount,out var expected)||b.Resources!=expected||b.Research.Points!=a.Research.Points+b.LastReward.Research))
                throw new InvalidOperationException("Clear receipt disagrees with actual resource/research grant.");
        }
        return BattleObservation.Create(before, after, resolved, events.ToArray());
    }
    public void Finish() => _recorder?.Complete(State);
    public void Dispose() { _recorder?.Dispose(); _match.Dispose(); }
}
public sealed record BattleObservation(int Wave, Phase Result, long CombatTicks, long CleanupTicks,
    CityState Before, CityState After, int Food, int Participants, int Lost, int Wounded, int KeepDamage,
    long? FirstFriendlyImpactTick, long? FirstSupportDamageTick, int MultiVictimImpacts, int StatusContributions,
    CombatEvent[] RelevantEvents)
{
    public int WatchpostDamage { get; init; }
    public int TowerImpacts { get; init; }
    public int FriendlyPeriodicDamage { get; init; }
    public int SupportLost { get; init; }
    public int MissingHp { get; init; }
    public ResourceCost LostCurrentRecruitQuote { get; init; }
    public static BattleObservation Create(MatchSnapshot before, MatchSnapshot after, long resolved, CombatEvent[] events)
    {
        var a = before.Players[0]; var b = after.Players[0]; var paid = a.LastUpkeep!;
        var support = a.Soldiers.Where(u => u.Class is UnitClass.Ranged or UnitClass.Magic).Select(u => u.Id).ToHashSet();
        var impacts = events.Where(e => e.Type == CombatEventType.Impact && e.Landed).ToArray();
        ResourceCost replacement=default;
        foreach(var lost in a.Soldiers.Where(u=>paid.Participating.Contains(u.Id)&&b.Soldiers.All(v=>v.Id!=u.Id)))
        {
            var quotes=b.RecruitmentQuotes.Where(q=>q.Type==lost.Type&&b.Slots.Any(s=>s.Level==q.Level&&after.BuildingCatalog.SingleOrDefault(d=>d.Type==s.Type)?.Recruits?.Contains(q.Type)==true));
            if(quotes.OrderByDescending(q=>q.Level).FirstOrDefault() is {} q && !replacement.TryAdd(q.Cost,out replacement))throw new InvalidOperationException("Replacement cost overflow.");
        }
        return new(before.Wave, after.Phase, resolved - before.Tick, after.Tick - resolved, a, b, paid.Paid, paid.Participating.Length,
            paid.Participating.Count(id => b.Soldiers.All(u => u.Id != id)), b.Soldiers.Count(u => u.Health < u.Profile.Health), a.Health - b.Health,
            impacts.Where(e => e.Unit?.Owner == a.Id).Select(e => (long?)(e.Tick-before.Tick)).FirstOrDefault(),
            events.Where(e => e.Type == CombatEventType.Hit && (e.Unit is { } hit && support.Contains(hit.Id))).Select(e => (long?)(e.Tick-before.Tick)).FirstOrDefault(),
            impacts.Count(e => e.Unit?.Owner == a.Id && e.Victims.Length > 1), events.Sum(e => e.Periodic.Length),
            events.Where(e => e.Type is CombatEventType.Impact or CombatEventType.StructureHit or CombatEventType.StructureDestroyed).ToArray())
        {
            WatchpostDamage=before.Structures.Where(s=>s.Owner==a.Id&&s.Kind==ArenaStructureKind.Watchpost).Sum(s=>s.Health)-after.Structures.Where(s=>s.Owner==a.Id&&s.Kind==ArenaStructureKind.Watchpost).Sum(s=>s.Health),
            TowerImpacts=impacts.Count(e=>e.Tower?.City==a.Id),
            FriendlyPeriodicDamage=events.Where(e=>e.Type==CombatEventType.Hit).Sum(e=>e.Periodic.Where(p=>a.Soldiers.Any(u=>u.Id==p.SourceId)).Sum(p=>p.Damage)),
            SupportLost=support.Count(id=>b.Soldiers.All(u=>u.Id!=id)),MissingHp=b.Soldiers.Sum(u=>u.Profile.Health-u.Health),LostCurrentRecruitQuote=replacement
        };
    }
}
