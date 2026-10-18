namespace Game.Core;

// Match owns shared session/editability guards and revisions. This in-process
// module owns only validated city edits; combat remains the sole unit store.
internal sealed class SettlementCommands(EconomyConfiguration economy, ArmyConfiguration army, CombatSimulation combat)
{
    internal CommandResult Apply(City city, Command command)
    {
        CommandResult Reject(string message) => new(command.Sequence, false, message);
        CommandResult Accept() => new(command.Sequence, true, $"{command.Action} accepted.");
        if (command.Action is "recruit" or "regroup-member" or "send" or "store" && command.SquadId < -1)
            return Reject("Choose a valid squad selector.");
        if (command.Action == "research-tech")
        {
            TechnologyEligibility eligibility;
            try { eligibility = economy.Research.Eligibility(city.Research, command.Technology); }
            catch (ArgumentException) { return Reject("Invalid research state."); }
            if (!eligibility.Available) return Reject(eligibility.Reason);
            ResearchState next = city.Research.Purchase(command.Technology, economy.Research);
            try { combat.Research(city.Id, next, economy.Research, city.Run); }
            catch (Exception e) when (e is OverflowException or ArgumentException) { return Reject("Unsafe technology profile."); }
            city.Research = next;
            return Accept();
        }
        if (command.Action == "deploy-squad")
        {
            if (!combat.Place(city.Id, command.SquadId, command.Cell, city.PurchasedHomes, city.ArmyPlots)) return Reject("Choose an owned squad and an available purchased deployment cell.");
            return Accept();
        }
        if (command.Action == "regroup-member")
        {
            UnitState? member = city.Soldiers.FirstOrDefault(u => u.Id == command.UnitId);
            if (member?.Assignment is not { Stored: false }) return Reject("Choose a deployed owned member.");
            ArmyAssignment? destination = army.Find(city.Soldiers.Where(u => u.Id != member.Id), city.PurchasedHomes, plots: city.ArmyPlots, type: member.Type, squadId: command.SquadId);
            if (destination is null) return Reject("No compatible squad or free home.");
            combat.Assign(member.Id, destination); combat.RestoreHomes();
            return Accept();
        }
        // Both actions buy the same persistent land; arbitrary numbered purchase
        // order is authoritative, never inferred from the total home count.
        if (command.Action == "buy-home" || command.Action == "buy-plot" && ArmyConfiguration.IsArmyPlot(command.Slot))
        {
            int id = command.Slot;
            if (command.Action == "buy-home" && id == -1)
                id = Array.FindIndex(city.ArmyPlots, p => !p.Purchased) + ArmyConfiguration.PlotOffset;
            if (!ArmyConfiguration.IsArmyPlot(id)) return Reject("Choose an army plot.");
            if (city.Plot(id).Purchased) return Reject("Plot already purchased.");
            if (command.ExpectedHomeCount != city.PurchasedHomes) return Reject("Stale home purchase price.");
            if (army.HomeQuote(city.PurchasedHomes) is not ResourceCost price || !city.Resources.TryPay(price, out ResourceCost balance)) return Reject("No affordable battlefield home available.");
            city.Resources = balance; city.SetPlot(id, city.Plot(id) with { Purchased = true });
            return Accept();
        }
        if (command.Action is "retire" or "store" or "send")
        {
            UnitState? unit = city.Soldiers.FirstOrDefault(u => u.Id == command.UnitId);
            if (unit?.Assignment is not { } current) return Reject("Choose a living owned roster unit.");
            if (command.Action == "retire")
            {
                combat.Remove(unit.Id);
                return Accept();
            }
            if (command.Slot is < 0 or >= 9) return Reject("Choose a Town hall.");
            SlotState hall = city.Slots[command.Slot];
            if (hall.Type != Building.TownHall || command.ExpectedGeneration != hall.Generation) return Reject("Stale or missing Town hall.");
            if (command.Action == "send" && (!current.Stored || current.HallSlot != command.Slot || current.HallGeneration != hall.Generation)
                || command.Action == "store" && current.Stored) return Reject("Unit is not in the requested source roster.");
            ArmyAssignment? next = army.Find(city.Soldiers, city.PurchasedHomes,
                command.Action == "store" ? command.Slot : -1, command.Action == "store" ? hall.Generation : 0,
                command.Action == "store" ? ArmyConfiguration.Capacity(hall.CapacityLevel) : 0, plots: city.ArmyPlots, type: unit.Type, squadId: command.SquadId);
            if (next is null) return Reject("Destination has no available individual slot or compatible squad.");
            combat.Assign(unit.Id, next); combat.RestoreHomes();
            return Accept();
        }
        if (command.Slot is < 0 or >= ArmyConfiguration.TotalPlots) return Reject("Choose a city or army plot.");
        bool armyPlot = ArmyConfiguration.IsArmyPlot(command.Slot);
        SlotState slot = city.Plot(command.Slot);
        BuildingDefinition? definition = economy.BuildingRule(slot.Type);
        void Pay(ResourceCost cost)
        {
            if (!city.Resources.TryPay(cost, out ResourceCost balance)) throw new InvalidOperationException("Invalid validated payment.");
            city.Resources = balance;
        }
        bool CanPay(ResourceCost cost) => city.Resources.TryPay(cost, out _);
        if (command.Action is "upgrade" or "upgrade-capacity" or "upgrade-healing" or "upgrade-tower" or "recruit" or "sell" or "trade"
            && (slot.Type == Building.Empty || command.ExpectedGeneration != slot.Generation)) return Reject("Stale building instance.");
        switch (command.Action)
        {
            case "buy-plot":
                if (slot.Purchased) return Reject("Plot already purchased.");
                if (command.ExpectedExpansionCount != city.ExpansionCount) return Reject("Stale expansion price.");
                if (!economy.TryPlotPrice(city.ExpansionCount, out ResourceCost plotPrice) || !CanPay(plotPrice)) return Reject("Not enough gold for this plot.");
                Pay(plotPrice); city.Slots[command.Slot] = slot with { Purchased = true };
                break;
            case "sell":
                if (slot.Type == Building.TownHall && city.Soldiers.Any(u => u.Assignment is { Stored: true } a && a.HallSlot == command.Slot && a.HallGeneration == slot.Generation)) return Reject("Send or retire stored units before selling this Town hall.");
                if (!city.Resources.TryAdd(slot.Refund, out ResourceCost refunded)) return Reject("Refund would overflow resource balances.");
                city.Resources = refunded; city.SetPlot(command.Slot, new(Building.Empty, 0) { Purchased = slot.Purchased });
                city.Towers.Remove(command.Slot);
                break;
            case "trade":
                if (slot.Type != Building.Market) return Reject("Select your Market.");
                if (!economy.TryMarketQuote(command.Resource, command.Bundles, out ResourceCost stock, out ResourceCost proceeds)) return Reject("Choose a valid resource and positive whole bundles.");
                int merchantBonus = RunEffects.MerchantBonus(city.Run, city.CurrentWave);
                if (!proceeds.TryAdd(new(Gold: merchantBonus), out proceeds)) return Reject("Trade would overflow.");
                if (!city.Resources.TryPay(stock, out ResourceCost afterStock)) return Reject("Not enough stock.");
                if (!afterStock.TryAdd(proceeds, out ResourceCost traded)) return Reject("Trade would overflow resource balances.");
                city.Resources = traded;
                if (merchantBonus > 0) city.Run = city.Run with { MerchantWave = city.CurrentWave };
                break;
            case "build":
                BuildingDefinition? construction = economy.BuildingRule(command.Building);
                if (construction is null) return Reject("Unknown building.");
                if (!slot.Purchased) return Reject("Purchase this plot first.");
                if (slot.Type != Building.Empty) return Reject("Slot occupied.");
                if (armyPlot && (!ArmyConfiguration.IsTower(command.Building)
                    || city.Soldiers.Any(u => u.Assignment is { Stored: false } a && a.Tile == army.HomeCells[command.Slot - ArmyConfiguration.PlotOffset])))
                    return Reject("Army plots require an empty whole plot and a defense tower.");
                ResourceCost payment = construction.Construction;
                if (command.Payment == ConstructionPayment.GoldRecovery)
                {
                    if (city.Wood != 0 || construction.RecoveryConstruction is not ResourceCost recovery) return Reject("Gold recovery requires zero wood and a Lumbermill.");
                    payment = recovery;
                }
                if (!CanPay(payment)) return Reject("Not enough resources.");
                if (city.NextBuildingGeneration == long.MaxValue) return Reject("Building identity exhausted.");
                Pay(payment); city.SetPlot(command.Slot, new(command.Building, 1)
                {
                    Purchased = true,
                    Generation = city.NextBuildingGeneration++,
                    Investment = payment,
                    CapacityLevel = command.Building == Building.TownHall ? 1 : 0,
                    HealingLevel = command.Building == Building.TownHall ? 1 : 0
                });
                if (ArmyConfiguration.IsTower(command.Building))
                    city.Towers[command.Slot] = new(city.Id, command.Slot, command.Building, 1);
                break;
            case "upgrade-capacity":
            case "upgrade-healing":
                bool healing = command.Action == "upgrade-healing";
                if (slot.Type != Building.TownHall) return Reject("Select your Town hall.");
                if (command.ExpectedTrackLevel != (healing ? slot.HealingLevel : slot.CapacityLevel)) return Reject("Stale Town hall track quote.");
                if (ArmyConfiguration.Upgrade(healing ? slot.HealingLevel : slot.CapacityLevel, healing) is not ResourceCost hallQuote) return Reject("Town hall track is complete.");
                if (!CanPay(hallQuote) || !slot.Investment.TryAdd(hallQuote, out ResourceCost hallInvestment)) return Reject("Not enough resources or unsafe investment.");
                Pay(hallQuote); city.Slots[command.Slot] = slot with
                {
                    Investment = hallInvestment,
                    CapacityLevel = slot.CapacityLevel + (healing ? 0 : 1),
                    HealingLevel = slot.HealingLevel + (healing ? 1 : 0)
                };
                break;
            case "upgrade-tower":
                if (slot.Type != Building.ArrowTower || slot.Level != 1 || command.ExpectedTrackLevel != slot.Level
                    || command.Building is not (Building.RapidArrowTower or Building.BombardTower)) return Reject("Choose a current base arrow tower and an upgrade path.");
                if (!economy.TryTowerUpgrade(command.Building, out ResourceCost towerQuote)
                    || !CanPay(towerQuote) || !slot.Investment.TryAdd(towerQuote, out ResourceCost towerInvestment)) return Reject("Not enough resources or unsafe investment.");
                Pay(towerQuote);
                city.SetPlot(command.Slot, slot with { Type = command.Building, Level = 2, Investment = towerInvestment });
                city.Towers[command.Slot] = city.Towers[command.Slot] with { Type = command.Building, Level = 2 };
                break;
            case "upgrade":
                if (definition is null || !economy.TryUpgrade(slot.Type, slot.Level, out ResourceCost upgrade)) return Reject("Building is at its maximum level.");
                if (!CanPay(upgrade)) return Reject("Not enough resources.");
                if (!slot.Investment.TryAdd(upgrade, out ResourceCost investment)) return Reject("Investment would overflow resource bounds.");
                Pay(upgrade); city.SetPlot(command.Slot, slot with { Level = slot.Level + 1, Investment = investment });
                if (city.Towers.TryGetValue(command.Slot, out TowerState? tower)) city.Towers[command.Slot] = tower with { Level = slot.Level + 1 };
                break;
            case "recruit":
                if (definition?.Recruits?.Contains(command.SoldierType) != true) return Reject("This building cannot recruit that role.");
                ResourceCost cost = economy.Recruitment(command.SoldierType, slot.Level, city.Run);
                if (!CanPay(cost)) return Reject("Not enough resources.");
                ArmyAssignment? home = army.Find(city.Soldiers, city.PurchasedHomes, plots: city.ArmyPlots, type: command.SoldierType, squadId: command.SquadId);
                if (home is null) return Reject("No compatible squad space. Reinforce a matching squad, buy a plot, store or retire a member.");
                Pay(cost); combat.Create(command.SoldierType, city.Id, city.Id, city.Id, level: slot.Level, capabilities: RunEffects.Capabilities(economy.Research.Capabilities(city.Research, command.SoldierType), city.Run, command.SoldierType), assignment: home); combat.RestoreHomes();
                break;
            default: return Reject("Unknown action.");
        }
        return Accept();
    }
}
