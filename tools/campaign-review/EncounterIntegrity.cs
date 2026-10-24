// SPDX-License-Identifier: GPL-3.0-only
using System.Text.Json;
using Game.Core;
namespace Game.Shared.Reviews;

/// <summary>Checks public projections/receipts against entry and result, without reproducing game rules.</summary>
public static class EncounterIntegrity
{
    public static void RequireCommand(MatchSnapshot before,MatchSnapshot after,Command command)
    {
        if(before.Phase!=Phase.Combat&&after.Phase==Phase.Combat)RequireEntry(before,after);
        if(!command.Action.StartsWith("reward:",StringComparison.Ordinal)||before.Revision==after.Revision)return;
        var city=before.Players.Single(c=>c.Id==command.City);var ended=after.Players.Single(c=>c.Id==command.City);
        var quote=city.Run.Quotes.Single(q=>q.Id==command.Action["reward:".Length..]);
        if(!city.Resources.TryAdd(quote.Resources,out var resources)||ended.Resources!=resources||ended.Health!=city.Health+quote.KeepHealing||ended.Research!=city.Research)
            throw new InvalidOperationException("Immediate reward differs from its frozen resource/Keep quote.");
        var healing=quote.Soldiers.ToDictionary(h=>h.UnitId,h=>h.Amount);
        if(city.Soldiers.Length!=ended.Soldiers.Length)throw new InvalidOperationException("Reward changed roster identity/count.");
        foreach(var old in city.Soldiers)
        {
            var current=ended.Soldiers.Single(u=>u.Id==old.Id);
            if(current.Health!=old.Health+healing.GetValueOrDefault(old.Id)||current.Profile!=old.Profile||current.Level!=old.Level||current.Experience!=old.Experience||current.RecoveryEligible!=old.RecoveryEligible)
                throw new InvalidOperationException("Reward healed twice or changed equipment/service/eligibility.");
        }
    }
    public static void RequireEntry(MatchSnapshot forecast,MatchSnapshot entered)
    {
        string[] Entries(IEnumerable<UnitState> units)=>units.Select(u=>JsonSerializer.Serialize(new
        {u.Type,u.Level,u.Rank,u.IsBoss,u.Origin,u.Destination,u.Profile,u.FormationSlot,u.Deployed,
            Cell=u.Hex!.Position.Cell,Lifecycle=u.Hex.Lifecycle},WireJson.Options)).Order(StringComparer.Ordinal).ToArray();
        var expected=BattleDeployment.Preview(forecast).Where(u=>u.Faction==Faction.Skeletons);
        if(!Entries(expected).SequenceEqual(Entries(entered.Enemies)))throw new InvalidOperationException("Forecast profiles/group slots/entry cells differ from actual spawns.");
        foreach(var city in forecast.Players.Where(c=>!c.Eliminated))
        {
            var projected=city.FoodForecast!;var paid=entered.Players.Single(c=>c.Id==city.Id).LastUpkeep!;
            if(projected.Paid!=paid.Paid||!projected.Participating.SequenceEqual(paid.Participating)||!projected.Funded.SequenceEqual(paid.Funded)||!projected.Unfed.SequenceEqual(paid.Unfed))
                throw new InvalidOperationException("Forecast Food/participation differs from actual upkeep.");
        }
    }
    public static void RequireOutcome(MatchSnapshot before,MatchSnapshot after)
    {
        foreach(var city in before.Players.Where(c=>c.LastUpkeep?.Wave==before.Wave))
        {
            var ended=after.Players.Single(c=>c.Id==city.Id);var paid=city.LastUpkeep!;var receipt=ended.LastBattleLoss;
            if(receipt is null||receipt.Wave!=before.Wave||receipt.Participants!=paid.Participating.Length
                ||receipt.Survivors!=paid.Participating.Count(id=>ended.Soldiers.Any(u=>u.Id==id))||receipt.KeepDamage!=Math.Max(0,city.Health-ended.Health))
                throw new InvalidOperationException("Frozen result differs from actual participants, survivors or Keep.");
            if(ended.LastReward?.Wave==before.Wave)
            {
                if(!city.Resources.TryAdd(ended.LastReward.Amount,out var expected)||ended.Resources!=expected||ended.Research.Points!=city.Research.Points+ended.LastReward.Research)
                    throw new InvalidOperationException("Clear receipt differs from actual resource/research grant.");
            }
            else if(city.Resources!=ended.Resources)throw new InvalidOperationException("Unawarded city received a resource change during combat.");
        }
    }
}
