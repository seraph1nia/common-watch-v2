// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;

namespace Game.Shared;

/// <summary>Bounded development review policy. Only ordinary paid intents; never ships as autoplay.</summary>
public static class OpeningCampaign
{
    public static GameplayIntent? Next(MatchSnapshot state, int player, string openingOrder = "canonical")
    {
        if (state.Phase is not (Phase.Building or Phase.Preparation)) return null;
        var city = state.Players.Single(p => p.Id == player);
        if (city.Ready || city.Eliminated) return null;
        GameplayIntent? Build(int slot, Building type)
        {
            if (city.Slots[slot].Type != Building.Empty) return null;
            if (!city.Slots[slot].Purchased)
            {
                int expansion = city.Slots.Count(s => s.Purchased) - 5;
                return city.Resources.TryPay(new(state.PlotPrices[expansion]), out _) ? new(GameplayAction.BuyPlot, slot) : null;
            }
            var definition = state.BuildingCatalog.Single(b => b.Type == type);
            if (city.Resources.TryPay(definition.Construction, out _)) return new(GameplayAction.Build, slot, type);
            return city.Wood == 0 && definition.RecoveryConstruction is { } recovery && city.Resources.TryPay(recovery, out _)
                ? new(GameplayAction.Build, slot, type, Payment: ConstructionPayment.GoldRecovery) : null;
        }
        GameplayIntent? Recruit(UnitType type, int target)
        {
            if (city.Soldiers.Count(u => u.Type == type && u.Assignment is { Stored: false }) >= target) return null;
            int trainer = Array.FindIndex(city.Slots, s => state.BuildingCatalog.SingleOrDefault(b => b.Type == s.Type)?.Recruits?.Contains(type) == true);
            if (trainer < 0) return null;
            var quote = city.RecruitmentQuotes.Single(q => q.Type == type && q.Level == city.Slots[trainer].Level);
            if (!city.Resources.TryPay(quote.Cost, out _)) return null;
            var army = new ArmyConfiguration(state.Rules.Army, new SharedArena(state.Players.Select(p => p.Id)).Board, city.Army!.SquadCapacity);
            if (army.Find(city.Soldiers, city.Army.PurchasedHomes, plots: city.Army.Homes.Select(h => h.Plot).ToArray(), type: type) is not null)
                return new(GameplayAction.Recruit, trainer, SoldierType: type);
            return army.HomeQuote(city.Army.PurchasedHomes) is { } cost && city.Resources.TryPay(cost, out _) ? new(GameplayAction.BuyHome) : null;
        }
        string doctrine = city.Run.Doctrine;
        if (state.Wave == 1)
        {
            var order = doctrine == "merchant"
                ? new[] { (1, Building.MetalMine), (2, Building.Barracks), (3, Building.Lumbermill) }
                : new[] { (0, Building.Farm), (1, Building.MetalMine), (2, Building.Barracks), (3, Building.Lumbermill) };
            order = openingOrder switch
            {
                "canonical" => order,
                "army-first" => doctrine == "merchant" ? order : [(2, Building.Barracks), (1, Building.MetalMine), (0, Building.Farm), (3, Building.Lumbermill)],
                "economy-first" => doctrine == "merchant" ? order : doctrine == "grovekeeper"
                    ? [(0, Building.Farm), (1, Building.MetalMine), (4, Building.MetalMine), (2, Building.Barracks), (3, Building.Lumbermill)]
                    : [(0, Building.Farm), (1, Building.MetalMine), (3, Building.Lumbermill), (2, Building.Barracks)],
                _ => throw new ArgumentException("Unknown paid opening order.", nameof(openingOrder))
            };
            foreach (var (slot, type) in order)
                if (city.Slots[slot].Type == Building.Empty) return Build(slot, type);
            if (doctrine == "merchant" && state.Turn == state.Rules.PreparationStages && city.Slots[0].Type == Building.Empty) return Build(0, Building.Farm);
            if (doctrine == "grovekeeper" && state.Turn >= 2 && city.Slots[4].Type == Building.Empty) return Build(4, Building.MetalMine);
            if (doctrine == "arcanist" && state.Turn == state.Rules.PreparationStages && city.Slots[4].Type == Building.Empty)
            {
                var magic = Build(4, Building.Arcanum);
                if (magic is not null || openingOrder == "canonical") return magic;
            }
            if (Recruit(UnitType.Swordsman, doctrine == "arcanist" ? 3 : 4) is { } melee) return melee;
            return doctrine == "arcanist" ? Recruit(UnitType.Mage, 1) : null;
        }
        if (doctrine == "huntsman")
        {
            foreach (var (slot, type) in new[] { (4, Building.Lumbermill), (5, Building.ArcheryRange), (6, Building.Stonecutter) })
                if (Build(slot, type) is { } build) return build;
            if (Recruit(UnitType.Swordsman, 6) is { } melee) return melee;
            if (Recruit(UnitType.Crossbowman, 3) is { } ranged) return ranged;
            var barracks = city.Slots[2];
            if (barracks.Level < 2 && barracks.UpgradeQuote is { } cost && city.Resources.TryPay(cost, out _)) return new(GameplayAction.Upgrade, 2);
            return null;
        }
        if (doctrine is "grovekeeper" or "arcanist" && Build(5, Building.Stonecutter) is { } stone) return stone;
        return GameplayScenarios.NextPreparation(state, player, "frontline");
    }

    public static OpeningRun Review(string doctrine, ulong seed = SoloSession.DefaultSeed)
    {
        using var solo = new SoloSession(seed, "opening-review-" + doctrine + "-" + seed);
        Accept(solo.Send("doctrine:" + doctrine)); Accept(solo.Start());
        var start = solo.City.Resources;
        var waves = new List<OpeningWave>();
        var actions = new List<string>();
        for (int wave = 1; wave <= 3 && solo.Snapshot.Phase is not (Phase.Defeat or Phase.Victory); wave++)
        {
            if (solo.Snapshot.Phase == Phase.RewardDraft) Accept(solo.ChooseReward(GameplayScenarios.RewardChoice(solo.City.Run)));
            int decisions = 0;
            for (int stage = 0; stage < solo.Snapshot.Rules.PreparationStages; stage++)
            {
                for (int n = 0; n < 100 && Next(solo.Snapshot, solo.PlayerId) is { } intent; n++)
                {
                    Accept(solo.Submit(intent)); decisions++;
                    actions.Add($"W{wave}/P{stage + 1}: {intent.Action} {intent.Building} {intent.SoldierType} plot {intent.Slot + 1} {intent.Payment}");
                    if (n == 99) throw new InvalidOperationException("Opening decision bound exceeded.");
                }
                Accept(solo.Send("ready"));
            }
            int before = solo.City.Health;
            var paid = solo.City.LastUpkeep!;
            var types = solo.City.Soldiers.Where(u => paid.Participating.Contains(u.Id)).GroupBy(u => u.Type).ToDictionary(g => g.Key.ToString(), g => g.Count());
            GameplayScenarios.FinishCombat(solo);
            waves.Add(new(wave, solo.Snapshot.Phase.ToString(), decisions, types, paid.Paid, paid.Unfed.Length,
                paid.Participating.Length - solo.City.Soldiers.Count(u => paid.Participating.Contains(u.Id)), before - solo.City.Health, solo.City.Resources));
        }
        return new(doctrine, seed, solo.Snapshot.MatchId, start, waves.ToArray(), actions.ToArray());
    }
    private static void Accept(CommandResult result) { if (!result.Accepted) throw new InvalidOperationException(result.Message); }
}
public sealed record OpeningWave(int Wave, string Phase, int Decisions, Dictionary<string, int> Participants, int FoodPaid, int Unfunded, int Lost, int KeepDamage, ResourceCost Stock);
public sealed record OpeningRun(string Doctrine, ulong Seed, string MatchId, ResourceCost Starting, OpeningWave[] Waves, string[] Actions);
