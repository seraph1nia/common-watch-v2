// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;

namespace Game.GodotClient;

public sealed record AcceptedReward(int Wave, string Id, string Name, string Details);
public sealed record JourneyChange(bool Baseline, bool SuspendManagement, bool EnterCombat, bool EnterOutcome,
    bool EnterPlanning, AcceptedReward? Reward);

/// <summary>One bounded local transition journal. Observes authority; never submits commands or advances playback.</summary>
public sealed class HudJourney
{
    private object? _session;
    private string _match = "";
    private int _generation;
    private HudPresentation? _view;
    private CityState? _city;
    private long _battle, _acknowledged;
    private int _acceptedChoiceTurn = -1;
    public AcceptedReward? Receipt { get; private set; }
    public int ReceiptCount { get; private set; }
    public void Reset()
    {
        _session = null; _match = ""; _view = null; _city = null; _battle = 0;
        Receipt = null; ReceiptCount = 0; _acknowledged = 0; _acceptedChoiceTurn = -1;
    }
    public void Acknowledge(Command command, CommandResult result)
    {
        if (command.MatchId != _match || command.City != _city?.Id || result.Sequence != command.Sequence
            || result.Sequence <= _acknowledged || !command.Action.StartsWith("reward:", StringComparison.Ordinal)) return;
        _acknowledged = result.Sequence;
        if (result.Accepted) _acceptedChoiceTurn = command.TurnSerial;
    }
    public bool AwaitingChoiceSnapshot(MatchSnapshot state, int player) => state.MatchId == _match
        && state.Phase == Phase.RewardDraft && state.TurnSerial == _acceptedChoiceTurn
        && state.Players.Single(p => p.Id == player).Run.Choice is null;
    public JourneyChange Observe(object session, MatchSnapshot state, int player, int generation, HudPresentation view)
    {
        var city = state.Players.Single(p => p.Id == player);
        bool baseline = !ReferenceEquals(_session, session) || _match != state.MatchId || _generation != generation;
        bool combat = view.LiveCombat && (baseline || _view?.LiveCombat != true || _battle != state.WaveStartedTick);
        bool outcome = !view.LiveCombat && view.Moment is HudMoment.ChoosingReward or HudMoment.RewardWaiting or HudMoment.Terminal
            && (baseline || _view?.LiveCombat == true || _view?.Moment != view.Moment);
        bool planning = view.Moment is HudMoment.Planning or HudMoment.ReadyWaiting
            && (baseline || _view?.Moment is not (HudMoment.Planning or HudMoment.ReadyWaiting));
        bool observer = view.Moment == HudMoment.Observer && (baseline || _view?.Moment != HudMoment.Observer);
        AcceptedReward? accepted = null;
        if (baseline)
        {
            if (!ReferenceEquals(_session, session) || _match != state.MatchId) ReceiptCount = 0;
            _acknowledged = 0; _acceptedChoiceTurn = -1;
            Receipt = city.Run.Rewards.LastOrDefault() is { } previous
                ? new(previous.Wave, previous.Id, RunCatalog.Card(previous.Id).Name, RunPresentation.Receipt(previous)) : null;
        }
        else if (_city is { } before && city.Run.DraftWave > 0 && city.Run.Choice is { } id
            && (before.Run.DraftWave != city.Run.DraftWave || before.Run.Choice != id))
        {
            var receipt = city.Run.Rewards.Last(r => r.Wave == city.Run.DraftWave && r.Id == id);
            accepted = Receipt = new(receipt.Wave, id, RunCatalog.Card(id).Name, RunPresentation.Receipt(receipt));
            ReceiptCount++;
        }
        _session = session; _match = state.MatchId; _generation = generation; _view = view;
        _city = city; _battle = state.WaveStartedTick;
        return new(baseline, combat || outcome || planning || observer, combat, outcome, planning, accepted);
    }
}
