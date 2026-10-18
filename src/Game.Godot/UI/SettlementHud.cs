using Godot;
using Game.Core;
using Game.Shared;

namespace Game.GodotClient;

public partial class SettlementHud : Control
{
    private HudSkin _skin = null!;
    private GodotSession _session = null!;
    private Label _resources = null!, _phase = null!, _message = null!, _combatInfo = null!;
    private Button _ready = null!;
    private string _uiKey = "";
    public const float SideWidth = 368, HeaderHeight = 120;
    public BuildingInspector Inspector { get; private set; } = null!;
    public DraftPanel Draft { get; private set; } = null!;
    public Label Resources => _resources;
    public Label Message => _message;
    public Button ReadyButton => _ready;
    public event Action? CommandSubmitted;
    public event Action? CameraResetRequested;
    public event Action? MenuRequested;
    public event Action? ReconnectRequested;
    public void Reset() => _uiKey = "";
    private void Submitted() => CommandSubmitted?.Invoke();
    private void Act(Action action) { action(); Submitted(); }
    public void Initialize(HudSkin skin)
    {
        _skin = skin;
        var root = this; root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.MouseFilter = Control.MouseFilterEnum.Ignore;
        root.Theme = _skin.Theme;
        var header = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.AddChild(header); header.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide); header.OffsetBottom = HeaderHeight;
        _skin.Panel(header);
        var top = new VBoxContainer(); header.AddChild(Margin(top, 18));
        top.AddChild(new Label { Text = "THE COMMON WATCH  /  GODOT 2D" });
        _phase = WrapLabel(); top.AddChild(_phase);
        _resources = WrapLabel(); top.AddChild(_resources);
        var side = new PanelContainer(); root.AddChild(side);
        side.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.RightWide);
        side.OffsetLeft = -SideWidth; side.OffsetTop = HeaderHeight + 8; side.OffsetBottom = -60; side.OffsetRight = -8;
        _skin.Panel(side);
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; side.AddChild(Margin(scroll, 26));
        var column = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; scroll.AddChild(column);
        Inspector = new BuildingInspector(); column.AddChild(Inspector);
        Inspector.Initialize(_skin, Submitted);
        column.AddChild(new HSeparator());
        _ready = _skin.Button("Advance preparation / Ready", () => Act(() => _session.Send("ready"))); column.AddChild(_ready);
        _combatInfo = WrapLabel(); column.AddChild(_combatInfo);
        column.AddChild(_skin.Button("Reset camera [R]", () => CameraResetRequested?.Invoke()));
        column.AddChild(_skin.Button("Return to main menu", () => MenuRequested?.Invoke()));
        column.AddChild(WrapLabel("Select: left click • Pan: middle/right drag\nZoom: wheel • Reset: R\nOpening: Farm, Barracks, Metal Mine.\nAt zero wood, use Lumbermill recovery.\nReady, recruit two Swordsmen; Ready, recruit two more; Ready to fight.\nReady settles real resource production."));
        var footer = new PanelContainer(); root.AddChild(footer); footer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide); footer.OffsetTop = -52;
        _skin.Panel(footer, "hud/tooltip");
        _message = WrapLabel("Choose a settlement plot. All actions use the shared authoritative simulation."); var footerMargin = Margin(_message, 14); footerMargin.AddThemeConstantOverride("margin_left", 24); footer.AddChild(footerMargin);
        Draft = new DraftPanel(); root.AddChild(Draft); Draft.Initialize(_skin, Submitted);
        var menu = _skin.Button("Main menu", () => MenuRequested?.Invoke()); root.AddChild(menu);
        menu.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
        menu.OffsetLeft = -260; menu.OffsetRight = -140; menu.OffsetTop = 12; menu.OffsetBottom = 48;
        column.AddChild(_skin.Button("Reconnect", () => ReconnectRequested?.Invoke()));

    }

    public void Refresh(GodotSession session, RenderOptions options, bool finishingCombat, bool force = false)
    {
        _session = session;
        _message.Text = session.Pending ? "Waiting for authority…" : (!session.Connected ? session.Status : session.LastResult?.Message ?? session.Status);
        var state = _session.Snapshot; var city = _session.City;
        _phase.Text = $"Wave {state.Wave}/{state.TotalWaves}  •  {state.Phase}  •  preparation {state.Turn}  •  city HP {HealthPoints.Format(city.Health)}/{state.Rules.CityHealth}  •  {options.Mode}";
        _resources.Text = $"Gold {city.Gold}   Wood {city.Wood}   Food {city.Food}   Stone {city.Stone}   Metal {city.Metal}   Cloth {city.Cloth}   •   Swordsmen {city.Soldiers.Count(u => u.Type == UnitType.Swordsman)}";
        _ready.Visible = state.Phase != Phase.Combat;

        _ready.Disabled = !_session.Connected || state.Phase is not (Phase.Building or Phase.Preparation);
        _combatInfo.Text = state.Phase == Phase.Combat ? $"Automatic combat • tick {state.Tick}\nBlue: friendly  Red: enemy\nLiving friends: {city.Soldiers.Count(u => u.Health > 0)}  Enemies: {state.Enemies.Count(u => u.Health > 0)}"
            + "\n\n" + string.Join("\n", city.Soldiers.Concat(state.Enemies).Where(u => u.Health > 0)
                .Select(u => $"{(u.Faction == Faction.Adventurers ? "S" : "E")}{u.Id} {(u.Faction == Faction.Adventurers ? "" : "Enemy ")}{u.Type} HP: {HealthPoints.Format(u.Health)}/{HealthPoints.Format(u.Profile.Health)}"))
            : state.Phase is Phase.Defeat or Phase.Victory ? $"{state.Phase}. Use New solo game to restart." : "Preparing for the next wave.";
        string key = $"{state.MatchId}|{session.Connected}|{state.Phase}|{finishingCombat}|{_session.SelectedSlot}|{city.Resources}|{string.Join(',', city.Slots.Select(s => $"{s.Type}:{s.Level}:{s.Purchased}"))}|{string.Join(',', city.Run.Offers)}";
        if (!force && key == _uiKey) return; _uiKey = key;
        Inspector.Refresh(_session);
        Draft.Refresh(_session, finishingCombat);
    }

}
