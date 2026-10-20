using Game.Core;
using Game.Shared;
using Xunit;

namespace Game.Shared.Tests;

public sealed class DoctrineOfferTests
{
    private static RunCardView[] Catalog => RunCatalog.Doctrines.Select(d => new RunCardView(d.Id, d.Name, "", "", "", false, true)).ToArray();
    [Fact]
    public void PersonalOffersAreDistinctStableEligibleAndDoNotConsumeSimulationRng()
    {
        string[] offers = DoctrineOffers.For("match-a", 1, Catalog);
        Assert.Equal(3, offers.Length); Assert.Equal(3, offers.Distinct().Count());
        Assert.Equal(offers, DoctrineOffers.For("match-a", 1, Catalog.Reverse()));
        Assert.Equal(offers, DoctrineOffers.For("match-a", 1, Catalog));
        Assert.DoesNotContain(offers[0], DoctrineOffers.For("match-a", 1, Catalog.Select(c => c with { Enabled = c.Id != offers[0] })));
        var combinations = Enumerable.Range(0, 30).Select(i => string.Join(',', DoctrineOffers.For("match-" + i, 1, Catalog))).Distinct();
        Assert.True(combinations.Count() > 10);
        Assert.True(Enumerable.Range(1, 6).Select(p => string.Join(',', DoctrineOffers.For("match-a", p, Catalog))).Distinct().Count() > 1);
    }
    [Fact]
    public void OldArenaSnapshotIsRejectedBeforeReceiving()
    {
        var snapshot = new SoloSession().Snapshot;
        var oldBoard = snapshot.Arena!.Board with { Version = 1 };
        Assert.Throws<InvalidDataException>(() => SnapshotPayload.DecodeForReceiving(SnapshotPayload.Encode(snapshot with { Arena = snapshot.Arena with { Board = oldBoard } })));
    }
}
