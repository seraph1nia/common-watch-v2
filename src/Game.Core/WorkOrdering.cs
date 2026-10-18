namespace Game.Core;

internal static class WorkOrdering
{
    // Instrument the input actually consumed by a compound OrderBy/ThenBy.
    // With counters disabled this returns the original enumerable unchanged.
    internal static IEnumerable<T> Input<T>(IEnumerable<T> source, WorkCounters? work)
        => work is null ? source : Count(source, work);
    private static IEnumerable<T> Count<T>(IEnumerable<T> source, WorkCounters work)
    {
        work.Add(WorkMetric.Sorts);
        foreach (T item in source) { work.Add(WorkMetric.SortElements); yield return item; }
    }
}
