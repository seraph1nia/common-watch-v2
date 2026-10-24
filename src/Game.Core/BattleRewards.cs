// SPDX-License-Identifier: GPL-3.0-only
namespace Game.Core;

public sealed record RewardCard(string Id, string Name, ResourceCost Resources = default,
    int KeepRepair = 0, int MissingHealthPercent = 0);
public sealed record SoldierRewardHealing(int UnitId, int Before, int Maximum, int Amount);
public sealed record BattleRewardQuote(string Id, ResourceCost Resources, int KeepBefore, int KeepHealing,
    SoldierRewardHealing[] Soldiers, bool ResourceCapped)
{
    public long SoldierHealing => Soldiers.Sum(s => (long)s.Amount);
    public bool HasBenefit => Resources != default || KeepHealing > 0 || Soldiers.Length > 0;
    public BattleRewardQuote Detach() => this with { Soldiers = Soldiers.ToArray() };
}
public sealed record BattleRewardReceipt(int Wave, string Id, ResourceCost Resources, int KeepHealing,
    long SoldierHealing, int SoldierCount, bool ResourceCapped);
public sealed record BattleLossReceipt(int Wave, int Participants, int Survivors, int KeepDamage, int FieldUnfunded);

/// <summary>One-shot authoritative quotes. No acquisition history can modify future rules.</summary>
public static class BattleRewards
{
    public const int RulesVersion = 1;
    public static IReadOnlyList<RewardCard> Catalog { get; } = Array.AsReadOnly<RewardCard>(
    [
        new("loot-timber", "Timber Shipment", new(Wood: 4)),
        new("loot-provisions", "Emergency Provisions", new(Food: 8)),
        new("loot-equipment", "Equipment Shipment", new(Metal: 3, Cloth: 2)),
        new("loot-treasury", "Royal Treasury", new(Gold: 5)),
        new("loot-stone", "Stone Shipment", new(Stone: 4)),
        new("loot-recovery", "Field Surgeons", KeepRepair: 10, MissingHealthPercent: 20)
    ]);
    public static RewardCard Card(string id) => Catalog.Single(c => c.Id == id);
    public static int Multiplier(int completedWave)
    {
        if (completedWave is < 1 or > 255) throw new ArgumentOutOfRangeException(nameof(completedWave));
        return completedWave <= 6 ? 1 : completedWave <= 13 ? 2 : 3;
    }
    public static BattleRewardQuote Quote(CityState city, string id, int completedWave, int keepMaximum)
        => Quote(city.Resources, city.Health, city.Soldiers, id, completedWave, keepMaximum);
    internal static BattleRewardQuote Quote(City city, string id, int completedWave, int keepMaximum)
        => Quote(city.Resources, city.Health, city.Soldiers, id, completedWave, keepMaximum);
    private static BattleRewardQuote Quote(ResourceCost balance, int keep, IEnumerable<UnitState> soldiers,
        string id, int wave, int keepMaximum)
    {
        RewardCard card = Card(id);
        var (resources, capped) = Supply(balance, card, wave);
        int heal = card.KeepRepair == 0 ? 0 : checked((int)Math.Min(Math.Max(0L, (long)keepMaximum - keep), HealthPoints.FromWhole(card.KeepRepair)));
        SoldierRewardHealing[] plan = card.MissingHealthPercent == 0 ? [] : soldiers
            .Where(u => u.Health > 0 && u.Health < u.Profile.Health)
            .OrderBy(u => u.Id).Select(u => new SoldierRewardHealing(u.Id, u.Health, u.Profile.Health,
                RunEffects.Portion(u.Profile.Health - u.Health, card.MissingHealthPercent))).ToArray();
        return new(id, resources, card.KeepRepair == 0 ? 0 : keep, heal, plan, capped);
    }
    private static (ResourceCost Resources, bool Capped) Supply(ResourceCost balance, RewardCard card, int wave)
    {
        if (!balance.IsValid) throw new ArgumentException("Invalid reward resource balance.", nameof(balance));
        if (!card.Resources.TryMultiply(Multiplier(wave), out ResourceCost wanted)) throw new OverflowException();
        ResourceCost gain = default; bool capped = false;
        foreach (Resource resource in Enum.GetValues<Resource>())
        {
            int amount = (int)Math.Min(wanted.Amount(resource), (long)int.MaxValue - balance.Amount(resource));
            capped |= amount != wanted.Amount(resource); gain = gain.With(resource, amount);
        }
        return (gain, capped);
    }
    // Ready preflights this against exact post-upkeep/post-clear balances before
    // any production/turn commit. A full-cap custom state cannot strand a draft.
    public static bool HasSupplyCapacity(ResourceCost balance, int completedWave)
        => Catalog.Count(c => c.Resources != default && Supply(balance, c, completedWave).Resources != default) >= 3;
    internal static BattleRewardQuote[] Offers(City city, int wave, ulong seed, int keepMaximum)
    {
        var candidates = Catalog.Select(c => Quote(city, c.Id, wave, keepMaximum))
            .Where(q => q.HasBenefit).OrderBy(q => q.Id, StringComparer.Ordinal).ToList();
        if (candidates.Count < 3) throw new InvalidOperationException("Reward capacity was not reserved before combat.");
        var offers = new List<BattleRewardQuote>();
        for (int slot = 0; slot < 3; slot++)
        {
            seed = SeededDecision.Mix(seed); int index = (int)(seed % (ulong)candidates.Count);
            offers.Add(candidates[index]); candidates.RemoveAt(index);
        }
        return offers.ToArray();
    }
    internal static bool ValidQuote(BattleRewardQuote? quote, int wave, int keepMaximum)
    {
        if (quote is null || quote.Soldiers is null || !quote.Resources.IsValid
            || !Catalog.Any(c => c.Id == quote.Id) || !quote.HasBenefit) return false;
        RewardCard card = Card(quote.Id);
        if (card.Resources != default)
            return quote.KeepBefore == 0 && quote.KeepHealing == 0 && quote.Soldiers.Length == 0
                && ValidSupply(quote.Resources, quote.ResourceCapped, card, wave);
        return quote.Resources == default && !quote.ResourceCapped && quote.KeepBefore > 0 && quote.KeepBefore <= keepMaximum
            && quote.KeepHealing == Math.Min(HealthPoints.FromWhole(card.KeepRepair), keepMaximum - quote.KeepBefore)
            && quote.Soldiers.All(s => s is not null && s.UnitId > 0 && s.Before > 0 && s.Before < s.Maximum
                && s.Amount == RunEffects.Portion(s.Maximum - s.Before, card.MissingHealthPercent))
            && quote.Soldiers.Select(s => s.UnitId).Distinct().Count() == quote.Soldiers.Length;
    }
    private static bool ValidSupply(ResourceCost actual, bool capped, RewardCard card, int wave)
    {
        if (!actual.IsValid || actual == default || !card.Resources.TryMultiply(Multiplier(wave), out ResourceCost wanted)) return false;
        return Enum.GetValues<Resource>().All(r => actual.Amount(r) <= wanted.Amount(r)) && capped == (actual != wanted);
    }
    internal static bool ValidReceipt(BattleRewardReceipt? receipt, int keepMaximum)
    {
        if (receipt is null || receipt.Wave is < 1 or > 255 || !Catalog.Any(c => c.Id == receipt.Id)) return false;
        RewardCard card = Card(receipt.Id);
        if (card.Resources != default)
            return receipt.KeepHealing == 0 && receipt.SoldierHealing == 0 && receipt.SoldierCount == 0
                && ValidSupply(receipt.Resources, receipt.ResourceCapped, card, receipt.Wave);
        return receipt.Resources == default && !receipt.ResourceCapped && receipt.KeepHealing >= 0
            && receipt.KeepHealing <= Math.Min(HealthPoints.FromWhole(card.KeepRepair), keepMaximum)
            && receipt.SoldierCount >= 0 && receipt.SoldierHealing >= receipt.SoldierCount
            && receipt.SoldierHealing <= (long)receipt.SoldierCount * int.MaxValue
            && (receipt.SoldierHealing > 0) == (receipt.SoldierCount > 0)
            && (receipt.KeepHealing > 0 || receipt.SoldierHealing > 0);
    }
    internal static bool CanApply(CityState city, BattleRewardQuote quote, int wave, int keepMaximum)
        => city.Resources.TryAdd(quote.Resources, out _) && SamePlan(quote, Quote(city, quote.Id, wave, keepMaximum));
    private static bool SamePlan(BattleRewardQuote quote, BattleRewardQuote current)
        => quote.HasBenefit && current.Id == quote.Id && current.Resources == quote.Resources
            && current.KeepBefore == quote.KeepBefore && current.KeepHealing == quote.KeepHealing
            && current.ResourceCapped == quote.ResourceCapped && current.Soldiers.SequenceEqual(quote.Soldiers);
    internal static bool CanApply(City city, BattleRewardQuote quote, int wave, int keepMaximum)
    {
        // The authoritative draft permits no intervening spending or healing.
        // Reject a violated frozen plan rather than partially granting/rerolling.
        if (!quote.HasBenefit || !city.Resources.TryAdd(quote.Resources, out _)) return false;
        return SamePlan(quote, Quote(city, quote.Id, wave, keepMaximum));
    }
}
