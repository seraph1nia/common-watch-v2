namespace Game.Core;

// Semantic work, never CPU instructions or a combined efficiency score. An
// owner registers supported sites; absent support remains null, not false zero.
public enum WorkMetric
{
    WorldViews, UnitVisits, Sorts, SortElements, UnitProjections, RouteElementsCopied, VisitedElementsCopied,
    ObservationBuilds, ObservationActorVisits, ObservationReservationVisits, OpponentVisits, RangeCandidates,
    SplashCandidates, BfsSearches, BfsDequeues, BfsEdges, OccupancyChecks, QueuedCandidates,
    ProfileResolutions, ProfileCacheMisses, ProfileCacheHits, MatchSnapshots, CodecEncodes, CodecDecodes,
    JsonBytes, CompressedBytes, PlaybackIndexBuilds, PlaybackEventVisits, FullPoseSamples, HudSectionRefreshes
}

public sealed class WorkCounters
{
    public const int SchemaVersion = 1;
    private readonly long[] _values = new long[Enum.GetValues<WorkMetric>().Length];
    private readonly bool[] _supported = new bool[Enum.GetValues<WorkMetric>().Length];
    public void Support(params WorkMetric[] metrics) { foreach (WorkMetric metric in metrics) _supported[(int)metric] = true; }
    public void Add(WorkMetric metric, long amount = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        if (!_supported[(int)metric]) throw new InvalidOperationException("Work metric has no registered site.");
        _values[(int)metric] = checked(_values[(int)metric] + amount);
    }
    public IReadOnlyDictionary<WorkMetric, long?> Snapshot() => Enum.GetValues<WorkMetric>().ToDictionary(m => m, m => _supported[(int)m] ? (long?)_values[(int)m] : null);
    public void Reset() => Array.Clear(_values);
}
