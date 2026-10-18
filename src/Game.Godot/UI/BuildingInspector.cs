using Godot;
using Game.Core;
using Game.Shared;

namespace Game.GodotClient;

public partial class BuildingInspector : VBoxContainer
{
    private HudSkin _skin = null!;
    private GodotSession _session = null!;
    private Action _submitted = null!;
    private Label _selection = null!, _production = null!;
    private VBoxContainer _actions = null!;
    private readonly Dictionary<Building, Button> _buildButtons = [];
    private Button? _recruit;
    public IReadOnlyDictionary<Building, Button> BuildButtons => _buildButtons;
    public Button? RecruitButton => _recruit;
    public void Initialize(HudSkin skin, Action submitted)
    {
        _skin = skin; _submitted = submitted;
        _selection = new ArtTooltipLabel
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MakeTooltip = text => { var panel = new PanelContainer(); _skin.Panel(panel, "hud/tooltip"); panel.AddChild(Margin(WrapLabel(text), 22)); return panel; }
        };
        AddChild(_selection);
        _production = WrapLabel(); AddChild(_production);
        _actions = new VBoxContainer(); AddChild(_actions);
    }
    private void Act(Action action) { action(); _submitted(); }
    public void Refresh(GodotSession session)
    {
        _session = session;
        var state = _session.Snapshot; var city = _session.City;
        _selection.TooltipText = _session.SelectedSlot >= 0 ? $"Selected plot {_session.SelectedSlot + 1}: inspect costs and production below." : "Choose a settlement plot to inspect its costs and production.";
        _production.Text = "Next production: " + (city.ProductionIncome is { } income ? RunPresentation.Resources(income) : "None");
        _production.Visible = _actions.Visible = state.Phase != Phase.Combat;
        foreach (Node child in _actions.GetChildren()) { _actions.RemoveChild(child); child.QueueFree(); }
        _buildButtons.Clear(); _recruit = null;
        int slotIndex = _session.SelectedSlot;
        if (slotIndex is >= 0 and < 9)
        {
            var slot = city.Slots[slotIndex];
            _selection.Text = $"PLOT {slotIndex + 1}\n{(slot.Purchased ? slot.Type == Building.Empty ? "Empty plot" : $"{slot.Type} — level {slot.Level}" : "Unpurchased plot")}";
            bool editable = _session.Connected && state.Phase is Phase.Building or Phase.Preparation;
            if (!editable) _actions.AddChild(WrapLabel("Settlement actions are available during preparation."));
            else if (!slot.Purchased) _actions.AddChild(_skin.Button("Buy plot", () => Act(() => _session.Send("buy-plot", slotIndex))));
            else if (slot.Type == Building.Empty)
            {
                foreach (Building type in new[] { Building.Lumbermill, Building.Farm, Building.Barracks, Building.MetalMine, Building.Mine })
                {
                    var definition = state.BuildingCatalog.First(b => b.Type == type);
                    var button = _skin.Button($"Build {type} ({RunPresentation.Resources(definition.Construction)})", () => Act(() => _session.Send("build", slotIndex, type)));
                    _buildButtons[type] = button; _actions.AddChild(button);
                }
                if (city.Wood == 0 && state.BuildingCatalog.First(b => b.Type == Building.Lumbermill).RecoveryConstruction is { } recovery) _actions.AddChild(_skin.Button("Lumbermill recovery (" + RunPresentation.Resources(recovery) + ")", () => Act(() => _session.Send("build", slotIndex, Building.Lumbermill, payment: ConstructionPayment.GoldRecovery))));
                _actions.AddChild(WrapLabel("If resources are insufficient, the authority explains the missing requirement when you choose an action."));
            }
            else
            {
                if (slot.UpgradeQuote is { } upgrade) _actions.AddChild(_skin.Button("Upgrade (" + RunPresentation.Resources(upgrade) + ")", () => Act(() => _session.Send("upgrade", slotIndex))));
                if (slot.Type == Building.Barracks)
                {
                    var quote = city.RecruitmentQuotes.First(q => q.Type == UnitType.Swordsman && q.Level == slot.Level);
                    _recruit = _skin.Button("Recruit Swordsman (" + RunPresentation.Resources(quote.Cost) + ")", () => Act(() => _session.Send("recruit", slotIndex)));
                    _actions.AddChild(_recruit);
                    _actions.AddChild(WrapLabel($"Swordsman upkeep: {quote.Upkeep} Food. Health: {HealthPoints.Format(quote.Profile.Health)}. Damage: {HealthPoints.Format(quote.Profile.Damage)}."));
                }
                _actions.AddChild(_skin.Button("Sell (refund " + RunPresentation.Resources(slot.Refund) + ")", () => Act(() => _session.Send("sell", slotIndex))));
            }
        }
        else _selection.Text = "SETTLEMENT\nSelect one of the nine hex plots to build, inspect or recruit.";
        if (state.Phase == Phase.Combat) _selection.Text = "BATTLEFIELD";
    }
}
