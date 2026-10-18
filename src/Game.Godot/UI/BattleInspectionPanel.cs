using Game.Core;
using Godot;

namespace Game.GodotClient;

/// <summary>Read-only native lists of the actual planned enemies, deployed army and structures.</summary>
public partial class BattleInspectionPanel : VBoxContainer
{
    private HudSkin _skin = null!;
    private GameplayContext _context = null!;
    private Label _summary = null!;
    private ActionList _units = null!;
    public event Action<UnitState, bool>? UnitSelected;
    public event Action<StructureState>? StructureSelected;
    public void Initialize(HudSkin skin)
    {
        _skin = skin; _summary = WrapLabel(); AddChild(_summary); _units = new ActionList(); AddChild(_units);
    }
    public void Refresh(GameplayContext context)
    {
        _context = context; bool preview = context.State.Phase is Phase.Building or Phase.Preparation or Phase.RewardDraft;
        var enemies = preview ? context.State.NextWave : context.State.Enemies;
        _summary.Text = $"{(preview ? "NEXT WAVE — READ-ONLY FORECAST" : "SHARED BATTLE")} {context.State.Wave}/{context.State.TotalWaves}\n" +
            string.Join(" · ", enemies.GroupBy(u => (u.Type, u.Level, u.IsBoss)).Select(g => $"{g.Count()} {g.Key.Type} L{g.Key.Level}{(g.Key.IsBoss ? " BOSS" : "")}")) +
            "\nSelect any member or structure to inspect. Preview actors never enter the combat simulation.";
        _units.Begin();
        foreach (var city in context.State.Players)
            foreach (var unit in city.Soldiers)
            {
                int id = unit.Id;
                _units.Item(_skin, "Friend" + id, $"P{city.Id} #{id} {unit.Type} L{unit.Level} · squad {unit.SquadId}\n" +
                    (unit.Assignment?.Stored == true ? "Stored — never fights" : preview ? city.FoodForecast?.Participating.Contains(id) == true ? "Funded field participant" : "Unfunded / sits out" : unit.Participating ? "Participating" : "Sits out"),
                    () => { if (_context.State.Players.SelectMany(p => p.Soldiers).FirstOrDefault(u => u.Id == id) is { } selected) UnitSelected?.Invoke(selected, false); });
            }
        foreach (var unit in enemies)
        {
            int id = unit.Id;
            _units.Item(_skin, (preview ? "Forecast" : "Enemy") + id, $"Enemy #{id} {unit.Type} L{unit.Level}{(unit.IsBoss ? " BOSS" : "")} · pressure on P{unit.Destination}\nHP {HealthPoints.Format(unit.Health)}/{HealthPoints.Format(unit.Profile.Health)}; damage {HealthPoints.Format(unit.Profile.Damage)}/{HealthPoints.Format(unit.Profile.SecondaryDamage)}",
                () => { var list = _context.State.Phase is Phase.Building or Phase.Preparation or Phase.RewardDraft ? _context.State.NextWave : _context.State.Enemies; if (list.FirstOrDefault(u => u.Id == id) is { } selected) UnitSelected?.Invoke(selected, _context.State.Phase != Phase.Combat); });
        }
        foreach (var structure in context.State.Structures)
        {
            var identity = (structure.Owner, structure.Kind, structure.Slot);
            _units.Item(_skin, $"Structure{identity.Owner}_{identity.Kind}_{identity.Slot}", $"P{structure.Owner} {structure.Kind} — cell {structure.Cell}\nHP {HealthPoints.Format(structure.Health)}/{HealthPoints.Format(structure.MaximumHealth)}",
                () => { if (_context.State.Structures.FirstOrDefault(s => (s.Owner, s.Kind, s.Slot) == identity) is { } selected) StructureSelected?.Invoke(selected); });
        }
        _units.End();
    }
}
