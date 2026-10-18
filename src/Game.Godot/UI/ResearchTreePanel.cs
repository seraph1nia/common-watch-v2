using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class ResearchTreePanel : VBoxContainer
{
    private HudSkin _skin = null!;
    private GameplayContext _context = null!;
    private Label _summary = null!, _details = null!;
    private ActionList _nodes = null!, _purchase = null!;
    private TechnologyId _selected;
    public IReadOnlyDictionary<string, Button> TechnologyButtons => _nodes.Buttons;
    public IReadOnlyDictionary<string, Button> PurchaseButtons => _purchase.Buttons;
    public void Initialize(HudSkin skin)
    {
        _skin = skin; _summary = WrapLabel(); AddChild(_summary);
        _details = WrapLabel("Select a technology to inspect its canonical effect and prerequisites."); AddChild(_details);
        _purchase = new ActionList(); AddChild(_purchase);
        _nodes = new ActionList(); AddChild(_nodes);
    }
    public void ResetSelection() { _selected = TechnologyId.None; _details.Text = "Select a technology to inspect its canonical effect and prerequisites."; }
    public void Refresh(GameplayContext context)
    {
        _context = context; var city = context.City;
        _summary.Text = $"P{city.Id} WORLDTREE — {city.Research.Points} research points; progress {city.Research.Progress}/3\n{context.EditReason}\nFoundation → specialization → mastery / production offshoot. Sibling specializations are exclusive.";
        _nodes.Begin();
        foreach (var node in context.State.TechnologyCatalog.OrderBy(n => n.Class).ThenBy(n => n.Tier).ThenBy(n => n.Id))
        {
            var id = node.Id; bool owned = city.Research.Has(id);
            var eligibility = city.Technologies.FirstOrDefault(t => t.Id == id);
            _nodes.Item(_skin, "Tech" + id, $"{node.Class} / tier {node.Tier}: {node.Name} — {node.Cost} points\n" +
                (owned ? "ACQUIRED" : eligibility?.Available == true ? "Available" : eligibility?.Reason ?? "Unavailable") +
                $"; prerequisite: {node.Prerequisite}; branch: {node.Branch}", () => { _selected = id; Refresh(_context); });
        }
        _nodes.End(); _purchase.Begin();
        if (context.State.TechnologyCatalog.FirstOrDefault(n => n.Id == _selected) is { } selected)
        {
            _details.Text = $"{selected.Name} ({selected.Id})\nCost {selected.Cost}; prerequisite {selected.Prerequisite}; branch {selected.Branch}\n" +
                (selected.Production is { } production ? $"Each {GameplayContext.BuildingName(production.Building)}: +{production.ExtraPerProducer} {production.Resource}/production" : UnitInspector.Capabilities(selected.Benefit));
            string reason = city.Research.Has(_selected) ? "Already acquired." : city.Technologies.FirstOrDefault(t => t.Id == _selected) is { Available: false } locked ? locked.Reason : "";
            _purchase.Item(_skin, "Purchase", "Purchase selected technology", () => _context.Submit(new(GameplayAction.Research, Technology: _selected)), context.Block(reason: reason));
        }
        _purchase.End();
    }
}
