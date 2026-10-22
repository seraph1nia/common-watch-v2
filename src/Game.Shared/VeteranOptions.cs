// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;

namespace Game.Shared;

public sealed record RecoveryOption(int HallSlot, bool StoredHere, bool CanStore, int HealthAfterProduction, int ProductionsToFull, string Reason);
public sealed record ReplacementOption(int TrainerSlot, RecruitmentQuote Quote, bool Affordable, bool SpaceNow,
    ResourceCost? TrainerUpgrade, RecruitmentQuote? AfterTrainerUpgrade);
public sealed record VeteranOptions(UnitState Member, int Upkeep, RecoveryOption[] Recovery, ReplacementOption[] Replacements)
{
    public const string ServiceMeaning = "Service history counts successful clears actually fought while funded; it grants no stats or equipment upgrades.";
    public string Summary => $"Service history: {Member.Experience} battles\nEquipment L{Member.Level} stays fixed. " + ServiceMeaning
        + $"\nKeep: HP {HealthPoints.Format(Member.Health)}/{HealthPoints.Format(Member.Profile.Health)}; strike {HealthPoints.Format(Member.Profile.Damage)}/{HealthPoints.Format(Member.Profile.SecondaryDamage)}; range {Member.Profile.HexRange}; {Upkeep} Food per funded battle, including storage."
        + "\nRetirement removes this soldier permanently, with no refund. Replacement is a separate paid recruit with new identity and zero service.";
    public string Details => string.Join("\n\n", Recovery.Select(r => $"Town Hall plot {r.HallSlot + 1}: " + (r.Reason.Length > 0 ? r.Reason :
            $"{(r.StoredHere ? "Stored" : "If stored")}: next production HP {HealthPoints.Format(Member.Health)} → {HealthPoints.Format(r.HealthAfterProduction)}; {r.ProductionsToFull} eligible production payments to full HP at the current rate (future recovery requires stored upkeep to be funded too). No extra resource fee; takes a storage place and never fights while stored.")))
        + (Recovery.Length == 0 ? "Recovery: build a Town Hall to store soldiers. Storage alone does not grant healing eligibility." : "")
        + "\n\n" + (Replacements.Length == 0 ? "Replacement: no compatible trainer. Build one before retiring if you need a replacement." : string.Join("\n\n", Replacements.Select(r =>
            $"Trainer plot {r.TrainerSlot + 1}: new L{r.Quote.Level} {Member.Type} · {RunPresentation.Resources(r.Quote.Cost)} ({(r.Affordable ? "affordable" : "insufficient stock")})\n"
            + $"HP {HealthPoints.Format(r.Quote.Profile.Health)}; strike {HealthPoints.Format(r.Quote.Profile.Damage)}/{HealthPoints.Format(r.Quote.Profile.SecondaryDamage)}; range {r.Quote.Profile.HexRange}; upkeep {r.Quote.Upkeep} Food. "
            + (r.SpaceNow ? "Compatible space available now." : "No compatible field space now; storage/retirement may free a place.")
            + (r.TrainerUpgrade is { } upgrade && r.AfterTrainerUpgrade is { } next ? $"\nTrainer upgrade costs {RunPresentation.Resources(upgrade)}; future L{next.Level} recruit costs {RunPresentation.Resources(next.Cost)}, HP {HealthPoints.Format(next.Profile.Health)}, strike {HealthPoints.Format(next.Profile.Damage)}/{HealthPoints.Format(next.Profile.SecondaryDamage)}. Existing soldiers keep their equipment and wounds." : ""))))
        + "\n\nResearch/reward effects already included. Strike values are base profiles, not guaranteed damage or a strength score. No automatic replacement or re-equipment.";
}

/// <summary>Current prices and Core recovery projections, without commands or simulated state.</summary>
public static class VeteranComparison
{
    public static VeteranOptions Project(MatchSnapshot state, CityState city, int memberId)
    {
        var member = city.Soldiers.Single(u => u.Id == memberId && u.Health > 0);
        var army = new ArmyConfiguration(state.Rules.Army, new SharedArena(state.Players.Select(p => p.Id)).Board, city.Army!.SquadCapacity);
        bool space = army.Find(city.Soldiers, city.Army.PurchasedHomes, plots: city.Army.Homes.Select(h => h.Plot).ToArray(), type: member.Type) is not null;
        var recovery = city.Army.Halls.Select(h =>
        {
            bool here = member.Assignment is { Stored: true } a && a.HallSlot == h.Slot && a.HallGeneration == h.Generation;
            bool room = army.Find(city.Soldiers, city.Army.PurchasedHomes, h.Slot, h.Generation, h.Capacity, type: member.Type) is not null;
            string reason = !member.RecoveryEligible ? "Not eligible: must be funded in a completed battle; storing alone grants no recovery."
                : member.Assignment is { Stored: true } && !here ? "Already stored in another hall; return before transferring."
                : !here && !room ? "Hall full; no available individual place." : "";
            int next = reason.Length == 0 ? ArmyConfiguration.HealedHealth(member.Health, member.Profile.Health, h.HealingPercent) : member.Health;
            int health = member.Health, payments = 0;
            if (reason.Length == 0)
                for (; health < member.Profile.Health && payments < 100; payments++) health = ArmyConfiguration.HealedHealth(health, member.Profile.Health, h.HealingPercent);
            return new RecoveryOption(h.Slot, here, !here && room && member.Assignment is { Stored: false }, next, payments, reason);
        }).ToArray();
        var replacements = city.Slots.Select((slot, i) => (slot, i)).Where(p => state.BuildingCatalog.SingleOrDefault(b => b.Type == p.slot.Type)?.Recruits?.Contains(member.Type) == true)
            .Select(p =>
            {
                var quote = city.RecruitmentQuotes.Single(q => q.Type == member.Type && q.Level == p.slot.Level);
                return new ReplacementOption(p.i, quote, city.Resources.TryPay(quote.Cost, out _), space, p.slot.UpgradeQuote,
                    p.slot.UpgradeQuote is null ? null : city.RecruitmentQuotes.SingleOrDefault(q => q.Type == member.Type && q.Level == p.slot.Level + 1));
            }).ToArray();
        int upkeep = city.RecruitmentQuotes.First(q => q.Type == member.Type && q.Level == member.Level).Upkeep;
        return new(member, upkeep, recovery, replacements);
    }
}
