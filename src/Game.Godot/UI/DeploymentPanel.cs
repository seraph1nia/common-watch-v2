using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

/// <summary>Six native home targets in authoritative front/back order; no cell-ID translation.</summary>
public partial class DeploymentPanel : VBoxContainer
{
    private HudSkin _skin = null!;
    private GameplayContext _context = null!;
    private Label _summary = null!;
    private Label _direction = null!;
    private readonly Dictionary<string, Button> _buttons = [];
    private BuildingInspector _inspector = null!;
    private int _selected = -1, _squad;
    public int SelectedPlot => _selected;
    public int SelectedSquad => _squad;
    public BuildingInspector Inspector => _inspector;
    internal string Notice => _summary.Text;
    public IReadOnlyDictionary<string, Button> HomeButtons => _buttons;
    internal IReadOnlyList<ArmyHomeView> Homes { get; private set; } = [];
    public void InspectHome(int slot, GameplayContext context)
    {
        if (!ArmyConfiguration.IsArmyPlot(slot)) return;
        _selected = slot; Refresh(context, _squad);
        var action = _inspector.Actions.Values.FirstOrDefault(b => b.Visible && !b.Disabled)
            ?? _inspector.BuildButtons.Values.FirstOrDefault(b => b.Visible && !b.Disabled);
        action?.CallDeferred(Control.MethodName.GrabFocus);
    }
    public void Initialize(HudSkin skin, Action submitted)
    {
        _skin = skin; AddThemeConstantOverride("separation", 4);
        _summary = WrapLabel(); _summary.AddThemeFontSizeOverride("font_size", 14); AddChild(_summary);
        _direction = WrapLabel(); _direction.AddThemeFontSizeOverride("font_size", 14); AddChild(_direction);
        var formation = new GridContainer { Columns = 3, SizeFlagsHorizontal = SizeFlags.ExpandFill }; formation.AddThemeConstantOverride("v_separation", 6); AddChild(formation);
        for (int plot = 9; plot < 15; plot++)
        {
            int slot = plot;
            var card = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; card.AddThemeConstantOverride("separation", 4); formation.AddChild(card);
            var deploy = new Button { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new(0, 72),
                SizeFlagsHorizontal = SizeFlags.ExpandFill, ExpandIcon = true };
            deploy.AddThemeFontSizeOverride("font_size", 13); deploy.AddThemeConstantOverride("icon_max_width", 38);
            deploy.Pressed += () => Deploy(slot); card.AddChild(deploy); _buttons.Add("Deploy" + slot, deploy);
            var manage = skin.Button("Manage home " + (slot - 8), () => InspectHome(slot, _context));
            manage.SizeFlagsHorizontal = SizeFlags.ExpandFill; card.AddChild(manage); _buttons.Add("Inspect" + slot, manage);
        }
        _inspector = new BuildingInspector(); AddChild(_inspector); _inspector.Initialize(skin, submitted);
    }
    public void ResetSelection() { _selected = -1; _squad = 0; }
    public void Refresh(GameplayContext context, int squad = 0)
    {
        _context = context; _squad = squad;
        Homes = ArmyDeploymentView.Homes(context.State, context.City.Id, squad);
        string food = context.State.Phase == Phase.Preparation ? "includes final income" : context.State.Phase == Phase.Building ? "stock now" : "last paid battle";
        _direction.Text = "Enemy ↓ · Front 1–3 / Rear 4–6 · Keep behind · Food: " + food;
        _summary.Text = context.EditReason.Length > 0 ? context.EditReason
            : context.Session.LastResult is { Accepted: false } result ? "Rejected: " + result.Message
            : "Six army homes, separate from nine settlement clearings. Select a squad, then click an outlined home.";
        foreach (var home in Homes)
        {
            int plot = home.Home.PlotId; var button = _buttons["Deploy" + plot];
            bool current = home.Members.Any(u => u.SquadId == _squad);
            string restriction = context.Block(reason: home.Restriction);
            string action = restriction.Length > 0 ? home.Restriction : current ? "Selected squad · current home" : "Deploy selected squad here";
            string purchase = home.Purchase is { } cost ? " · " + GameplayContext.Cost(cost) : "";
            button.Text = $"Home {home.Home.Number} · {home.Occupancy}{purchase}\n{home.Participation}\n{action}";
            button.Disabled = restriction.Length > 0; button.TooltipText = restriction.Length > 0 ? restriction : action;
            button.Icon = home.Members.FirstOrDefault() is { } unit ? _skin.UnitSprite(unit) : _skin.BuildingSprite(home.Home.Plot.Type);
            var style = new StyleBoxFlat { BgColor = new Color("142825"), BorderColor = new Color(current ? "efd478" : home.CanDeploy ? "85dfc2" : "59646a"),
                BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
                ContentMarginLeft = 6, ContentMarginRight = 6, ContentMarginTop = 6, ContentMarginBottom = 6 };
            button.AddThemeStyleboxOverride("normal", style); button.AddThemeStyleboxOverride("disabled", style);
            button.AddThemeStyleboxOverride("hover", style);
        }
        var focus = GetViewport().GuiGetFocusOwner();
        _inspector.Visible = _selected >= 9; if (_inspector.Visible) _inspector.Refresh(context, _selected);
        if (focus is not null && _inspector.IsAncestorOf(focus) && !focus.IsVisibleInTree() && _buttons.TryGetValue("Inspect" + _selected, out var fallback))
            fallback.CallDeferred(Control.MethodName.GrabFocus);
    }
    private void Deploy(int plot)
    {
        // Resolve again from accepted state; stale/pending/foreign controls cannot submit.
        var home = ArmyDeploymentView.Homes(_context.State, _context.City.Id, _squad).SingleOrDefault(h => h.Home.PlotId == plot);
        if (home is { CanDeploy: true }) _context.Submit(new(GameplayAction.Deploy, SquadId: _squad, Cell: home.BattlefieldCell));
    }
}
