namespace Game.Core;

public sealed class ActionCues
{
    private string _match = "";
    private long _sequence;
    private readonly Queue<Command> _pending = new();
    public void Clear(string match) { _match = match; _sequence = 0; _pending.Clear(); }
    public void Baseline(string match, long sequence) { Clear(match); _sequence = sequence; }
    public void Observe(Command request, CommandResult result)
    {
        if (request.MatchId != _match) return;
        if (result.Sequence <= _sequence) return;
        _sequence = result.Sequence;
        if (!result.Accepted || request.Action is not ("build" or "upgrade" or "recruit" or "research-tech")) return;
        if (_pending.Count == 64) _pending.Dequeue();
        _pending.Enqueue(request);
    }
    public Command[] Drain() { Command[] result = _pending.ToArray(); _pending.Clear(); return result; }
}
