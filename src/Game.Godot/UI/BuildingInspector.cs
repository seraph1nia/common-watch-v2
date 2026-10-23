using Godot;
using Game.Core;
using Game.Shared;

namespace Game.GodotClient;

public partial class BuildingInspector : VBoxContainer
{
    private HudSkin _skin = null!;
    private GameplayContext _context = null!;
    private Action _submitted = null!;
    private Label _selection = null!, _details = null!;
    private ActionList _actions = null!;
    private ConstructionPanel _construction = null!;
    private RecruitmentPanel _recruitment = null!;
    private MarketPanel _market = null!;
    private TownHallPanel _hall = null!;
    private int _slot;
    private VBoxContainer _heading = null!;
    private VBoxContainer _text = null!;
    private Button _army = null!;
    private TextureRect _portrait = null!;
    public event Action? ArmyRequested;
    public bool Compact { get; set; }
    public bool SharedNotice { get; set; }
    public string EmptyHint { get; set; } = "Select a clearing to build or manage your settlement.";

    public IReadOnlyDictionary<Building, Button> BuildButtons => _construction.BuildButtons;
    public IReadOnlyDictionary<string, Button> Actions => _actions.Buttons;
    public ConstructionPanel Construction => _construction;
    internal Button ArmyButton => _army;
    public MarketPanel Market => _market;
    public RecruitmentPanel Recruitment => _recruitment;
    public Button? RecruitButton => _recruitment.Visible ? _recruitment.Buttons.GetValueOrDefault("RecruitSwordsman") : null;
    public void Initialize(HudSkin skin, Action submitted)
    {
        _skin = skin; _submitted = submitted;
        _heading = new VBoxContainer(); AddChild(_heading);
        var header = new HBoxContainer(); _heading.AddChild(header);
        _portrait = new TextureRect { CustomMinimumSize = new(48, 72), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = MouseFilterEnum.Ignore }; header.AddChild(_portrait);
        _text = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; header.AddChild(_text);
        _selection = WrapLabel(); _selection.AddThemeFontSizeOverride("font_size", 18); _text.AddChild(_selection); _details = WrapLabel(); _text.AddChild(_details);
        _actions = new ActionList { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ShrinkBegin, DefaultVariation = SharedNotice ? "ForestQuietButton" : "" }; _heading.AddChild(_actions);
        _army = skin.Button("Troops & homes", () => ArmyRequested?.Invoke()); _army.AutowrapMode = TextServer.AutowrapMode.WordSmart; _heading.AddChild(_army);
        _heading.MoveChild(_army, 1);
        _construction = new ConstructionPanel { Compact = Compact, SharedNotice = SharedNotice }; AddChild(_construction); _construction.Initialize(skin);
        _recruitment = new RecruitmentPanel(); AddChild(_recruitment); _recruitment.Initialize(skin);
        _market = new MarketPanel(); AddChild(_market); _market.Initialize(skin);
        _hall = new TownHallPanel(); AddChild(_hall); _hall.Initialize(skin);
    }
    public void Refresh(GodotSession session) => Refresh(new GameplayContext(session, _submitted), session.SelectedSlot);
    public void Refresh(GameplayContext context, int slotIndex)
    {
        _context = context; _slot = slotIndex;
        _actions.Vertical = true; _actions.Visible = true;
        _text.AddThemeConstantOverride("separation", Compact ? 4 : 10);
        _details.MaxLinesVisible = Compact && !SharedNotice ? 2 : -1;
        _details.Visible = true;
        SlotState? slot = ArmyConfiguration.IsArmyPlot(_slot) ? context.City.Army?.Homes.FirstOrDefault(h => h.PlotId == _slot)?.Plot
            : _slot is >= 0 and < 9 ? context.City.Slots[_slot] : null;
        _actions.Begin((context.Session, context.State.MatchId, context.City.Id, slotIndex, slot?.Generation));
        bool occupiedHome = ArmyConfiguration.IsArmyPlot(_slot) && context.City.Army!.Homes.Single(h => h.PlotId == _slot).Used > 0;
        _portrait.Texture = slot is null ? null : _skin.BuildingSprite(slot.Type);
        _portrait.Visible = _portrait.Texture is not null;
        _construction.Visible = slot is { Purchased: true, Type: Building.Empty } && !occupiedHome;
        if (!_construction.Visible) _construction.ResetSelection();
        _recruitment.Visible = slot is not null && context.State.BuildingCatalog.FirstOrDefault(b => b.Type == slot.Type)?.Recruits is not null;
        _market.Visible = slot?.Type == Building.Market;
        _hall.Visible = slot?.Type == Building.TownHall;
        _army.Visible = Compact && slot is not null && (slot.Type == Building.TownHall || context.State.BuildingCatalog.FirstOrDefault(b => b.Type == slot.Type)?.Recruits is not null);
        string? compactSummary = null;
        _details.Text = SharedNotice ? "" : context.EditReason;
        if (slot is null) { _selection.Text = EmptyHint; _details.Hide(); _actions.End(); return; }
        _selection.Text = $"{(ArmyConfiguration.IsArmyPlot(_slot) ? "ARMY HOME " + (_slot - 8) : "PLOT " + (_slot + 1))} · {(!slot.Purchased ? "Unpurchased land" : (occupiedHome ? "Squad home" : GameplayContext.BuildingName(slot.Type)))}";
        if (!slot.Purchased)
        {
            int count = ArmyConfiguration.IsArmyPlot(_slot) ? context.City.Army!.PurchasedHomes - 2 : context.City.Slots.Count(s => s.Purchased) - 5;
            var prices = ArmyConfiguration.IsArmyPlot(_slot) ? context.City.Army!.HomePrices : context.State.PlotPrices;
            ResourceCost? price = count >= 0 && count < prices.Length ? new(prices[count]) : null;
            _actions.Item(_skin, "Buy", "Purchase land — " + (price is { } quote ? GameplayContext.Cost(quote) : "unavailable"),
                () => _context.Submit(new(GameplayAction.BuyPlot, _slot)), context.Block(price, price is null ? "No quote." : ""));
        }
        else if (slot.Type == Building.Empty)
        {
            if (ArmyConfiguration.IsArmyPlot(_slot) && context.City.Army!.Homes.Single(h => h.PlotId == _slot).Used > 0) _details.Text += "\nSquad occupies this home. Move or store its members before building a tower.";
            else { _construction.Show(); _construction.Refresh(context, _slot); if (Compact) { _details.Visible = !SharedNotice && context.EditReason.Length > 0; _actions.Hide(); } }
        }
        else
        {
            var definition = context.State.BuildingCatalog.FirstOrDefault(b => b.Type == slot.Type);
            if (definition?.Recruits is { } recruits) compactSummary = $"Level {slot.Level} · Trains {string.Join(", ", recruits)}.";
            else if (slot.Type == Building.TownHall) compactSummary = $"Level {slot.Level} · Veteran storage & recovery.";
            _details.Text += (context.EditReason.Length > 0 ? "\n" : "") + $"Level {slot.Level} / {definition?.MaximumLevel.ToString() ?? "2"}\n" + (definition is null ? "Arrow Tower branch." : ConstructionPanel.Function(definition));
            if (definition?.Produces is { } resource)
            {
                int Output(int level) => EconomyConfiguration.ProducerOutput(definition, level, context.City.Research, context.State.TechnologyCatalog)
                    + (RunEffects.ProducerCard(definition.Type) is { } card ? RunCatalog.Amount(context.City.Run, card) : 0);
                string output = $"Current output {Output(slot.Level)} {resource}/production";
                if (slot.UpgradeQuote is not null) output += $" → {Output(slot.Level + 1)} after upgrade";
                _details.Text += "\n" + output; compactSummary = $"Level {slot.Level} · {output}";
                int doctrineBonus = RunCatalog.Doctrine(context.City.Run.Doctrine).ProductionBonus(context.City.Slots).Amount(resource);
                if (doctrineBonus > 0) _details.Text += $"\nCity-wide doctrine bonus +{doctrineBonus} {resource}/production (once per city; included in resource table).";
            }
            var tower = context.State.TowerCatalog.FirstOrDefault(t => t.Type == slot.Type && t.Level == slot.Level);
            if (tower is not null) tower = RunEffects.Tower(tower, context.City.Run);
            if (tower is not null) _details.Text += $"\nRange {tower.HexRange}; direct/secondary {HealthPoints.Format(tower.Damage)}/{HealthPoints.Format(tower.SecondaryDamage)}\nWindup {tower.WindupTicks} ticks; cadence {tower.CadenceTicks} ticks. Footprint: whole home.";
            if (slot.UpgradeQuote is { } upgrade)
                _actions.Item(_skin, "Upgrade", "Upgrade — " + GameplayContext.Cost(upgrade),
                    () => _context.Submit(new(GameplayAction.Upgrade, _slot)), context.Block(upgrade));
            if (slot.Type == Building.ArrowTower && slot.Level == 1)
            {
                var economy = new EconomyConfiguration(context.State.Rules);
                foreach (Building branch in new[] { Building.RapidArrowTower, Building.BombardTower })
                {
                    economy.TryTowerUpgrade(branch, out var quote);
                    var profile = RunEffects.Tower(context.State.TowerCatalog.Single(t => t.Type == branch && t.Level == 2), context.City.Run);
                    _actions.Item(_skin, "Branch" + branch, $"{GameplayContext.BuildingName(branch)} — {GameplayContext.Cost(quote)}\nDamage {HealthPoints.Format(profile.Damage)}/{HealthPoints.Format(profile.SecondaryDamage)}, range {profile.HexRange}, cadence {profile.CadenceTicks} ticks",
                        () => _context.Submit(new(GameplayAction.TowerBranch, _slot, branch)), context.Block(quote));
                }
            }
            bool stored = context.City.Soldiers.Any(u => u.Assignment is { Stored: true } a && a.HallSlot == _slot && a.HallGeneration == slot.Generation);
            _actions.Item(_skin, "Sell", "Sell — refund " + GameplayContext.Cost(slot.Refund),
                () => _context.Submit(new(GameplayAction.Sell, _slot)), context.Block(reason: stored ? "Return or retire stored veterans first." : ""));
            if (definition?.Recruits is not null) { _recruitment.Visible = true; _recruitment.Refresh(context, _slot); _army.Visible = Compact; }
            if (slot.Type == Building.Market) { _market.Show(); _market.Refresh(context, _slot); }
            if (slot.Type == Building.TownHall) { _hall.Visible = true; _hall.Refresh(context, _slot); _army.Visible = Compact; }
        }
        _details.TooltipText = _details.Text;
        if (Compact && compactSummary is not null)
            _details.Text = compactSummary + (!SharedNotice && context.EditReason.Length > 0 ? "\n" + context.EditReason : "");
        _actions.End();
    }
}
