using Game.Core;

namespace Game.Development;

// An opt-in legal-command driver used by actual client processes. It never writes authority state.
public sealed class OpeningProbe
{
    private int _next;
    private bool _awaiting, _doctrine, _reward;
    private long _lobbySince;
    private static readonly (int Turn, string Action, int Slot, Building Building, ConstructionPayment Payment)[] Opening =
    [
        (1, "build", 0, Building.Farm, ConstructionPayment.Standard),
        (1, "build", 1, Building.Barracks, ConstructionPayment.Standard),
        (1, "build", 2, Building.MetalMine, ConstructionPayment.Standard),
        (1, "build", 3, Building.Lumbermill, ConstructionPayment.GoldRecovery),
        (1, "ready", -1, Building.Empty, ConstructionPayment.Standard),
        (2, "recruit", 1, Building.Empty, ConstructionPayment.Standard),
        (2, "recruit", 1, Building.Empty, ConstructionPayment.Standard),
        (2, "ready", -1, Building.Empty, ConstructionPayment.Standard),
        (3, "recruit", 1, Building.Empty, ConstructionPayment.Standard),
        (3, "recruit", 1, Building.Empty, ConstructionPayment.Standard),
        (3, "ready", -1, Building.Empty, ConstructionPayment.Standard)
    ];
    public bool Complete { get; private set; }
    public bool SawCombat { get; private set; }
    public bool SawDraft { get; private set; }
    public void Tick(MatchSnapshot state, int playerId, bool host, int players, bool pending, CommandResult? result,
        Action<string, int, Building, ConstructionPayment> send)
    {
        if (Complete || pending) return;
        if (_awaiting)
        {
            if (result is null) return;
            if (!result.Accepted) throw new InvalidOperationException("Network opening rejected: " + result.Message);
            _awaiting = false;
        }
        void Submit(string action, int slot = -1, Building building = Building.Empty, ConstructionPayment payment = ConstructionPayment.Standard)
        { _awaiting = true; send(action, slot, building, payment); }
        if (state.Phase == Phase.Lobby)
        {
            if (!_doctrine) { _doctrine = true; Submit("doctrine:warden"); return; }
            if (!host || state.Players.Count(p => p.Connected) != players) return;
            if (_lobbySince == 0) _lobbySince = Environment.TickCount64;
            if (Environment.TickCount64 - _lobbySince > 2500) Submit("start");
            return;
        }
        if (_next < Opening.Length)
        {
            var command = Opening[_next];
            var city = state.Players.Single(p => p.Id == playerId);
            if (state.Phase is not (Phase.Building or Phase.Preparation) || state.Turn != command.Turn || city.Ready) return;
            _next++; Submit(command.Action, command.Slot, command.Building, command.Payment); return;
        }
        if (state.Phase == Phase.Combat) SawCombat = true;
        if (state.Phase == Phase.RewardDraft)
        {
            SawDraft = true;
            var city = state.Players.Single(p => p.Id == playerId);
            if (!_reward && city.Run.Offers.Length > 0) { _reward = true; Submit("reward:" + city.Run.Offers[0]); }
        }
        if (state.Wave == 2 && state.Phase == Phase.Building)
        {
            if (!SawCombat || !SawDraft || !_reward) throw new InvalidOperationException("Network exercise skipped combat/draft.");
            Complete = true;
        }
        if (state.Phase == Phase.Defeat) throw new InvalidOperationException("Network opening lost battle.");
    }
}
