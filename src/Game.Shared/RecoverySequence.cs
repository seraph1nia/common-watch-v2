// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
namespace Game.Shared;

/// <summary>A finite reviewed sequence. ACK and assignment observation are separate; never retries or resumes itself.</summary>
public sealed class RecoverySequence(SquadRecoveryPlan plan)
{
    private readonly RecoveryMember[] _moves = plan.Moving;
    private int _completed;
    private bool _acknowledged;
    private Command? _issued;
    private (Command Command, CommandResult Result)? _earlyAck;
    public SquadRecoveryPlan Plan => plan;
    public int Completed => _completed;
    public int Total => _moves.Length;
    public int PendingMember { get; private set; }
    public string Stopped { get; private set; } = "";
    public bool Finished => _completed == Total && PendingMember == 0;
    public string Status => $"{Completed}/{Total} moves confirmed." + (PendingMember != 0 ? " Waiting for acknowledgment and roster update." : "")
        + (Stopped.Length > 0 ? " " + Stopped + " Accepted moves remain; review the current roster." : Finished ? " Complete. Keep or return members explicitly later." : "");
    public void Cancel(string reason = "Sequence stopped.") => Stopped = reason;
    private bool Matches(Command command) => PendingMember != 0 && command.MatchId == plan.Match && command.City == plan.City
        && command.UnitId == PendingMember && command.Slot == plan.Hall && command.TurnSerial == plan.Turn
        && command.Action == (plan.Move == RecoveryMove.StoreWounded ? "store" : "send");
    public void Submitted(Command command)
    {
        if (!Matches(command)) { SubmissionFailed(); return; }
        _issued = command;
        if (_earlyAck is { } early) { _earlyAck = null; Acknowledge(early.Command, early.Result); }
    }
    public void SubmissionFailed() { PendingMember = 0; _issued = null; _earlyAck = null; Cancel("Move was not submitted."); }
    public void Acknowledge(Command command, CommandResult result)
    {
        if (!Matches(command) || result.Sequence != command.Sequence) return;
        if (_issued is null) { _earlyAck = (command, result); return; }
        if (_issued != command) return;
        if (!result.Accepted) { PendingMember = 0; _issued = null; Cancel("Move rejected: " + result.Message); }
        else _acknowledged = true;
    }
    public GameplayIntent? Next(MatchSnapshot state)
    {
        var city = state.Players.FirstOrDefault(p => p.Id == plan.City);
        if (city is null || state.MatchId != plan.Match) { Cancel("Match changed."); return null; }
        // Record an accepted in-flight result even after cancellation; never launch another one.
        if (PendingMember != 0 && _acknowledged && city.Soldiers.FirstOrDefault(u => u.Id == PendingMember)?.Assignment == _moves[_completed].Destination)
        { _completed++; PendingMember = 0; _acknowledged = false; _issued = null; _earlyAck = null; }
        if (state.TurnSerial != plan.Turn) Cancel("Planning boundary changed.");
        if (Stopped.Length > 0 || Finished) return null;
        var ids = _moves.Select(m => m.Before.Id).ToHashSet();
        if (SquadRecovery.Stamp(state, city, ids) != plan.Stamp) { Cancel("Roster, funds, hall or planning state changed."); return null; }
        for (int i = 0; i < _moves.Length; i++)
        {
            var actual = city.Soldiers.FirstOrDefault(u => u.Id == _moves[i].Before.Id);
            bool expected = i < _completed ? actual?.Assignment == _moves[i].Destination
                : i == _completed && PendingMember != 0 ? actual?.Assignment == _moves[i].Before.Assignment || actual?.Assignment == _moves[i].Destination
                : actual?.Assignment == _moves[i].Before.Assignment && actual?.SquadId == _moves[i].Before.SquadId;
            if (!expected) { Cancel("A reviewed member moved; review remaining members."); return null; }
        }
        if (PendingMember != 0) return null;
        var member = _moves[_completed]; var hall = city.Army!.Halls.Single(h => h.Slot == plan.Hall);
        var destination = SquadRecovery.Configuration(state, city).Find(city.Soldiers, city.Army.PurchasedHomes,
            plan.Move == RecoveryMove.StoreWounded ? plan.Hall : -1, plan.Move == RecoveryMove.StoreWounded ? hall.Generation : 0,
            plan.Move == RecoveryMove.StoreWounded ? hall.Capacity : 0, plots: city.Army.Homes.Select(h => h.Plot).ToArray(), type: member.Before.Type);
        if (destination != member.Destination) { Cancel("Destination capacity changed."); return null; }
        PendingMember = member.Before.Id;
        return new(plan.Move == RecoveryMove.StoreWounded ? GameplayAction.Store : GameplayAction.Send, plan.Hall, UnitId: PendingMember);
    }
}
