namespace Game.Core;

// One fixed numerical receiving encounter, not a simulator, save loader or
// network control plane. Only Game's owned headless playing-host bootstrap calls
// this internal seam; ordinary authority commands/RPCs cannot select a fixture.
// Synthetic tower materials, one explicit wound/Keep health and physical poses
// are deliberately NOT earned economy or full-game/network acceptance.
public sealed partial class AuthoritySession
{
    private bool _receivingPrepared, _receivingPlaced;

    internal bool ConfigureReceivingEncounter(string name) => name switch
    {
        "fallen-field" => ConfigureReceivingEncounter(false),
        "protected-field" => ConfigureReceivingEncounter(true),
        "fallen-stored" => ConfigureStoredReceivingEncounter(false),
        "protected-stored" => ConfigureStoredReceivingEncounter(true),
        _ => false
    };

    // Different declared numerical input, not earned campaign/balance history:
    // real paid W1 service/W2 Hall snapshots and W3 production remain intact;
    // only original enemy roster/physical poses/Keep threshold are controlled.
    private bool ConfigureStoredReceivingEncounter(bool protectedKeep)
    {
        if (IsEnded || _receivingPlaced || _match.Players.Count != 3 || _match.Wave != 3 || _match.Phase != Phase.Combat) return false;
        MatchSnapshot initial = _match.Snapshot();
        CityState storedOwner = initial.Players.Single(c => c.Id == 3);
        if (storedOwner.Soldiers.Length == 0 || storedOwner.Soldiers.Any(u => u.Experience < 1 || u.Assignment is not { Stored: true } || u.Participating)
            || !initial.Squads.Any(s => s.Owner != 3 && s.Faction == Faction.Adventurers && s.ParticipatingMembers.Length > 0))
            throw new InvalidOperationException("Controlled stored encounter requires actual paid served Hall veterans and another living field roster.");
        var arena = new SharedArena(_match.Players.Keys);
        int enemy = _match.Combat.Enemies().Last().Id;
        foreach (UnitState extra in _match.Combat.Enemies().Where(u => u.Id != enemy)) _match.Combat.Remove(extra.Id);
        foreach (SquadState squad in initial.Squads.Where(s => s.Faction == Faction.Adventurers && s.ParticipatingMembers.Length > 0))
        {
            int home = _match.Combat.Read(squad.Members[0]).Assignment!.Tile;
            int cell = arena.EnemyCells(squad.Owner)[home];
            foreach (int id in squad.Members)
                _match.Combat.Seed(_match.Combat.Inspect(id) with { Location = new(UnitLifecycle.Alive, new(cell, 1)) });
        }
        // The retained ranged actor prefers an in-range Watchpost. Choose a
        // real firing cell within its ACTUAL Keep range but outside Watchpost
        // range; do not forge a destroyed post/aim/impact or change its profile.
        int range = _match.Combat.Read(enemy).Profile.HexRange;
        int approach = arena.Board.Cells.Where(c => arena.CanOccupy(c.Id)
            && arena.Board.Distance(c.Id, arena.Keep(3)) <= range
            && arena.Board.Distance(c.Id, arena.Watchpost(3)) > range)
            .OrderBy(c => arena.Board.Distance(c.Id, arena.Keep(3))).ThenBy(c => c.Id).First().Id;
        _match.Combat.Seed(_match.Combat.Inspect(enemy) with { Location = new(UnitLifecycle.Alive, new(approach, 1)) });
        // A one-impact numerical threshold follows the actual enemy profile;
        // it is not an earned Keep injury or compensation in normal gameplay.
        _match.Players[3].Health = protectedKeep ? HealthPoints.FromWhole(100) : _match.Combat.Read(enemy).Profile.StructureDamage;
        _receivingPlaced = true;
        return true;
    }

    internal bool ConfigureReceivingEncounter(bool protectedKeep)
    {
        if (IsEnded || _match.Players.Count != 2) return false;
        if (!_receivingPrepared && _match.Phase == Phase.Preparation)
        {
            City city = _match.Players[2];
            city.Gold = 100; city.Wood = 100; city.Stone = 100;
            foreach (var (action, building) in new[] { ("buy-plot", Building.Empty), ("build", Building.ArrowTower) })
            {
                CommandResult result = _match.Apply(2, Command.FromSnapshot(_match.Snapshot(), 1, action, 2, 11, building));
                if (!result.Accepted) throw new InvalidOperationException("Controlled receiving tower input refused: " + result.Message);
            }
            _receivingPrepared = true;
            return true;
        }
        if (!_receivingPrepared || _receivingPlaced || _match.Phase != Phase.Combat || _match.Tick != 0) return false;
        MatchSnapshot initial = _match.Snapshot();
        if (initial.Players.Any(c => c.Soldiers.Length != 6 || c.Soldiers.Any(u => !u.Participating)))
            throw new InvalidOperationException("Controlled receiving encounter requires two identified six-member field rosters.");
        var arena = new SharedArena([1, 2], [(2, 11)]);
        int enemy = _match.Combat.Enemies().Last().Id;
        foreach (UnitState extra in _match.Combat.Enemies().Where(u => u.Id != enemy)) _match.Combat.Remove(extra.Id);
        foreach (SquadState squad in initial.Squads.Where(s => s.Faction == Faction.Adventurers))
        {
            int home = _match.Combat.Read(squad.Members[0]).Assignment!.Tile;
            int cell = squad.Owner == 1 ? arena.EnemyCells(1)[home] : arena.Deployment(1, home);
            foreach (int id in squad.Members)
                _match.Combat.Seed(_match.Combat.Inspect(id) with { Location = new(UnitLifecycle.Alive, new(cell, 1)) });
        }
        int approach = arena.Board.Cell(arena.Keep(2)).Neighbors.First(arena.CanOccupy);
        _match.Combat.Seed(_match.Combat.Inspect(enemy) with { Location = new(UnitLifecycle.Alive, new(approach, 1)) });
        int injured = _match.Players[2].Soldiers[0].Id;
        _match.Combat.Seed(_match.Combat.Inspect(injured) with { Health = HealthPoints.FromWhole(33) });
        _match.Players[2].Health = protectedKeep ? HealthPoints.FromWhole(100) : _match.Combat.Read(enemy).Profile.StructureDamage;
        _receivingPlaced = true;
        return true;
    }
}
