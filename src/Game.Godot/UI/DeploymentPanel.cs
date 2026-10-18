using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class DeploymentPanel : VBoxContainer
{
    private HudSkin _skin = null!;
    private GameplayContext _context = null!;
    private OptionButton _squad = null!;
    private Label _summary = null!;
    private ActionList _homes = null!;
    private BuildingInspector _inspector = null!;
    private int _selected = -1;
    public IReadOnlyDictionary<string, Button> HomeButtons => _homes.Buttons;
    public void Initialize(HudSkin skin, Action submitted)
    {
        _skin = skin; _summary = WrapLabel(); AddChild(_summary);
        _squad = new OptionButton(); AddChild(_squad); _squad.ItemSelected += _ => Refresh(_context);
        _homes = new ActionList(); AddChild(_homes);
        _inspector = new BuildingInspector(); AddChild(_inspector); _inspector.Initialize(skin, submitted);
    }
    public void ResetSelection() => _selected = -1;
    public void Refresh(GameplayContext context)
    {
        _context = context; var army = context.City.Army;
        _summary.Text = $"P{context.City.Id} — six numbered army homes (separate from nine settlement plots).\nSelect a squad, then deploy to an eligible home. Inspect any home to purchase land or build/upgrade/sell towers.\n{context.EditReason}";
        NativeOptions.Update(_squad, context.City.Soldiers.Where(u => u.Assignment is { Stored: false }).GroupBy(u => u.SquadId)
            .Select(g => (g.Key, $"Squad #{g.Key}: {g.First().Type} — {g.Count()}/{army?.SquadCapacity}, home {g.First().Assignment!.Tile + 1}")));
        _homes.Begin();
        foreach (var home in army?.Homes ?? [])
        {
            int plot = home.PlotId; int cell = context.State.Arena?.Sectors.FirstOrDefault(s => s.Owner == context.City.Id)?.DeploymentCells[home.Number - 1] ?? 0;
            _homes.Item(_skin, "Inspect" + plot, $"Inspect home {home.Number}: {home.State}; cell {cell}\n{GameplayContext.BuildingName(home.Plot.Type)}; members {home.Used}",
                () => { _selected = plot; Refresh(_context); });
            bool ownHome = context.City.Soldiers.Any(u => u.SquadId == _squad.GetSelectedId() && u.Assignment is { Stored: false } a && a.Tile == home.Cell);
            string reason = _squad.ItemCount == 0 ? "Select a field squad." : !home.Purchased ? "Purchase this home first." : home.Plot.Type != Building.Empty ? "Tower occupies this home." : home.Used > 0 && !ownHome ? "Another squad occupies this home." : "";
            _homes.Item(_skin, "Deploy" + plot, $"Deploy selected squad to home {home.Number}", () => Deploy(plot), context.Block(reason: reason));
        }
        _homes.End(); _inspector.Visible = _selected >= 9; if (_inspector.Visible) _inspector.Refresh(context, _selected);
    }
    private void Deploy(int plot)
    {
        int? cell = _context.State.Arena?.Sectors.FirstOrDefault(s => s.Owner == _context.City.Id)?.DeploymentCells[plot - ArmyConfiguration.PlotOffset];
        if (cell is { } target) _context.Submit(new(GameplayAction.Deploy, SquadId: _squad.GetSelectedId(), Cell: target));
    }
}
