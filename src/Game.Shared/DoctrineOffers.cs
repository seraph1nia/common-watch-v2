using System.Security.Cryptography;
using System.Text;
using Game.Core;

namespace Game.Shared;

/// <summary>Stable personal lobby offers, independent of simulation/reward RNG and local preferences.</summary>
public static class DoctrineOffers
{
    public static string[] For(string matchId, int playerId, IEnumerable<RunCardView> catalog)
        => catalog.Where(card => card.Enabled)
            .OrderBy(card => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"doctrine-offers-v1:{matchId}:{playerId}:{card.Id}"))), StringComparer.Ordinal)
            .Take(3).Select(card => card.Id).ToArray();
}
