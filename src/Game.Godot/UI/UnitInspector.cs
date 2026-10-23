using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

/// <summary>The same inspector serves roster, preparation forecasts and live battlefield selection.</summary>
public partial class UnitInspector : VBoxContainer
{
    private HudSkin _skin = null!;
    private GameplayContext _context = null!;
    private Label _text = null!;
    private Label _advanced = null!;
    public bool ReadOnlyLive { get; init; }
    internal Button AdvancedButton { get; private set; } = null!;
    internal string AdvancedDetails => _advanced.Text;
    internal bool AdvancedVisible => _advanced.Visible;
    internal OptionButton SquadSelector => _squad;
    internal string DestinationDetails => _destination.Text;
    internal (int Owner, ArenaStructureKind Kind, int Slot)? SelectedStructure => _structure;
    private OptionButton _hall = null!, _squad = null!;
    private ActionList _actions = null!;
    private int _id;
    private bool _preview;
    private int? _preferredSquad;
    private Label _destination = null!;
    private Label _veteran = null!;
    private ScrollContainer _veteranScroll = null!;
    internal ScrollContainer VeteranViewport => _veteranScroll;
    internal Rect2 VeteranCopyArea => _veteranScroll.GetGlobalRect();
    public Control? FallbackFocus { get; set; }
    internal Button VeteranButton { get; private set; } = null!;
    internal string VeteranDetails => _veteran.Text;
    private bool _reviewRetirement;
    private string _retireMatch = "";
    private GodotSession? _retireSession;
    private Button? _retireInvoker;
    private int _retireFocusFrames;
    private Phase _selectedPhase;
    private (int Owner, ArenaStructureKind Kind, int Slot)? _structure;
    public event Action<int>? PlotRequested;
    public int SelectedUnit => _id;
    public string Details => _text.Text;
    public IReadOnlyDictionary<string, Button> Buttons => _actions.Buttons;
    public void Initialize(HudSkin skin)
    {
        _skin = skin; _text = WrapLabel("Select a soldier, enemy or structure."); AddChild(_text);
        if (ReadOnlyLive) _text.AddThemeFontSizeOverride("font_size", 14);
        AdvancedButton = skin.Button("Advanced details ▾", () => { _advanced.Visible = !_advanced.Visible; AdvancedButton.Text = _advanced.Visible ? "Advanced details ▴" : "Advanced details ▾"; }, "ForestQuietButton");
        AddChild(AdvancedButton);
        _advanced = WrapLabel(); _advanced.AddThemeFontSizeOverride("font_size", 13); AddChild(_advanced); _advanced.Hide();
        _hall = new OptionButton(); AddChild(_hall);
        _squad = new OptionButton(); AddChild(_squad);
        _destination = WrapLabel(); AddChild(_destination); _destination.Hide();
        _squad.ItemSelected += _ => Refresh(_context);
        _hall.ItemSelected += _ => Refresh(_context);
        VeteranButton = skin.Button("Compare recovery / replacement", () => { _veteranScroll.Visible = !_veteranScroll.Visible; _veteranScroll.ScrollVertical = 0; Refresh(_context); }, "ForestQuietButton"); AddChild(VeteranButton);
        _veteranScroll = new ScrollContainer { CustomMinimumSize = new(0, 190), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true, FocusMode = FocusModeEnum.All }; AddChild(_veteranScroll);
        _veteran = WrapLabel(); _veteran.AddThemeFontSizeOverride("font_size", 14); _veteranScroll.AddChild(_veteran); _veteranScroll.Hide(); VeteranButton.Hide();
        _actions = new ActionList(); AddChild(_actions); SetProcess(false);
    }
    public override void _Process(double delta)
    {
        if (_retireFocusFrames-- > 0) return;
        SetProcess(false);
        if (_reviewRetirement && IsVisibleInTree() && _actions.Buttons.TryGetValue("CancelRetire", out var cancel) && cancel.IsVisibleInTree())
        { cancel.GrabFocus(); GetParentOrNull<ScrollContainer>()?.EnsureControlVisible(cancel); }
    }
    private void ResetAdvanced() { SetProcess(false); _reviewRetirement = false; _retireSession = null; _veteranScroll.Hide(); _advanced.Hide(); AdvancedButton.Text = "Advanced details ▾"; }
    public void Clear() { _preferredSquad = null; _destination.Hide(); _id = 0; _structure = null; _text.Text = "Select a soldier, enemy or structure."; ResetAdvanced(); AdvancedButton.Hide(); VeteranButton.Hide(); _hall.Hide(); _squad.Hide(); _actions.Begin(); _actions.End(); }
    public void Select(UnitState unit, bool preview, Phase phase) { if (_id != unit.Id) { ResetAdvanced(); _preferredSquad = unit.Assignment is { Stored: false } ? unit.SquadId : 0; } _structure = null; _id = unit.Id; _preview = preview; _selectedPhase = phase; }
    public void Select(StructureState structure) { if (_structure != (structure.Owner, structure.Kind, structure.Slot)) ResetAdvanced(); _veteranScroll.Hide(); _id = 0; _structure = (structure.Owner, structure.Kind, structure.Slot); }
    public void Refresh(GameplayContext context, CombatPlayback? playback = null)
    {
        _context = context; _veteranScroll.Visible &= !_preview && !ReadOnlyLive && _structure is null; _actions.Begin((context.Session, context.State.MatchId, context.City.Id, _id, _structure, _preview)); _hall.Hide(); _squad.Hide(); _destination.Hide(); VeteranButton.Hide();
        AdvancedButton.Visible = _id != 0 || _structure is not null;
        var units = playback?.Units() ?? context.State.Players.SelectMany(p => p.Soldiers).Concat(context.State.Enemies).Concat(context.State.DyingBodies).ToArray();
        double tick = playback?.Tick ?? context.State.Tick;
        if (_structure is { } identity)
        {
            var structure = (playback?.Structures() ?? BattleDeployment.Structures(context.State)).FirstOrDefault(s => s.Owner == identity.Owner && s.Kind == identity.Kind && s.Slot == identity.Slot);
            if (structure is null) { Clear(); return; }
            var tower = context.State.TowerCatalog.FirstOrDefault(t => t.Type == structure.TowerType && t.Level == structure.Level);
            _text.Text = $"P{structure.Owner} · {structure.Kind}\nHP {HealthPoints.Format(structure.Health)}/{HealthPoints.Format(structure.MaximumHealth)} · L{structure.Level}\n{(structure.Active ? "Active" : "Inactive / destroyed")}";
            _advanced.Text = $"Cell {structure.Cell}, plot {structure.Slot + 1}";
            if (tower is not null) tower = RunEffects.Tower(tower, context.State.Players.Single(p => p.Id == structure.Owner).Run);
            if (tower is not null) { _text.Text += $"\n{structure.TowerType}\nBase damage {HealthPoints.Format(tower.Damage)}/{HealthPoints.Format(tower.SecondaryDamage)} · Reach {tower.HexRange}"; _advanced.Text += $"\nWindup {tower.WindupTicks}; cadence {tower.CadenceTicks} ticks"; }
            if (structure.Kind == ArenaStructureKind.Watchpost) { _text.Text += $"\nReach {context.State.Rules.Arena.DefenderRange} · Base damage {context.State.Rules.DefenderDamage}\nWatchpost resets for the next battle."; _advanced.Text += $"\nWindup {context.State.Rules.Combat.Defender.WindupTicks}; cadence {context.State.Rules.Combat.Defender.CadenceTicks} ticks"; }
            if (!ReadOnlyLive && structure.Slot >= 0 && structure.Owner == context.City.Id)
                _actions.Item(_skin, "ManageTower", "Inspect tower plot / available actions", () => { if (_structure is { } selected) PlotRequested?.Invoke(selected.Slot); });
            _actions.End(); return;
        }
        if (_id == 0) { _actions.End(); return; }
        if (_preview && context.State.Phase != _selectedPhase) { Clear(); return; }
        var unit = (_preview ? context.State.NextWave : units)
            .FirstOrDefault(u => u.Id == _id);
        if (unit is null) { Clear(); return; }
        var city = context.State.Players.FirstOrDefault(p => p.Id == unit.Owner);
        bool planning = context.State.Phase is Phase.Building or Phase.Preparation;
        var funded = planning ? city?.FoodForecast?.Funded : city?.LastUpkeep?.Funded;
        string funding = funded is null ? "Funding unavailable" : funded.Contains(unit.Id) ? "Funded" : "Unfunded";
        var p = unit.Profile;
        _text.Text = ReadOnlyLive
            ? $"{CombatInspection.Owner(unit)} · {unit.Type}{(unit.IsBoss ? " · BOSS" : "")}\n{CombatInspection.Role(unit)} · L{unit.Level}"
                + $"\nHP {HealthPoints.Format(unit.Health)}/{HealthPoints.Format(unit.Profile.Health)}"
                + $"\nNow: {(unit.Health <= 0 ? "Fallen" : unit.Hex?.Action.ToString() ?? "Preparing")} · Effects: {CombatInspection.Effects(unit, tick)}"
                + (TeamAwareness.Sector(context.State, unit) is > 0 and var sector ? $"\nLocation: P{sector} sector" : "")
                + "\nTarget: " + LiveTarget(unit, units, playback?.Structures() ?? context.State.Structures, context.Session.PlayerId)
            : CombatInspection.Basic(unit, _preview ? context.State.NextWave : units, tick, _preview, playback?.Structures() ?? context.State.Structures);
        if (!ReadOnlyLive && city is not null) _text.Text += $"\nService history: {unit.Experience} battles (no stat or equipment gain)\n" +
            (unit.Assignment is { Stored: true } stored ? $"Stored: Town Hall plot {stored.HallSlot + 1}; never fights" : unit.Assignment is { } basicHome ? $"Army home {basicHome.Tile + 1}; {(unit.Deployed ? "field" : "queued")}" : "No persistent home")
            + $"\n{funding}; paid recovery {(unit.RecoveryEligible ? "eligible" : "not eligible")}";
        if (!ReadOnlyLive) _text.Text += $"\nTraits: {Capabilities(unit.Capabilities)}";
        _advanced.Text = $"#{unit.Id} {unit.Type} · {unit.Class}\n{unit.Faction} · owner P{unit.Owner} · L{unit.Level}, rank {unit.Rank}{(unit.IsBoss ? " BOSS" : "")}\nHP {HealthPoints.Format(unit.Health)}/{HealthPoints.Format(p.Health)}; Service history: {unit.Experience} battles (no stat or equipment gain)\nDamage direct/secondary {HealthPoints.Format(p.Damage)}/{HealthPoints.Format(p.SecondaryDamage)}\nStructure damage {HealthPoints.Format(p.StructureDamage)}; range {p.HexRange}\nMovement {p.MoveTicks}, windup {p.WindupTicks}, cadence {p.CadenceTicks} ticks\nSquad #{unit.SquadId}, member slot {unit.FormationSlot}\n" +
            (unit.Assignment is { Stored: true } a ? $"Stored: Town Hall plot {a.HallSlot + 1}, place {a.Tile + 1}; never fights" : unit.Assignment is { } home ? $"Army home {home.Tile + 1}; {(unit.Deployed ? "field" : "queued")}" : "No persistent home") +
            $"\n{funding}; paid recovery {(unit.RecoveryEligible ? "eligible" : "not eligible")}\nCapabilities: {Capabilities(unit.Capabilities)}\n{Statuses(unit.Statuses, context.State.Tick)}\nTarget {unit.TargetKind} #{unit.TargetId}; action {unit.Hex?.Action}\nCommitted start {unit.ActionStartTick}, impact {unit.ImpactTick}, ready {unit.ReadyTick}";
        long rallyUntil = playback?.RallyDisruptions().GetValueOrDefault(unit.SquadId)
            ?? context.State.Squads.FirstOrDefault(s => s.Id == unit.SquadId)?.RallyDisruptedUntil ?? 0;
        if (rallyUntil > tick) { _text.Text += "\nRally disrupted"; _advanced.Text += $"\nRally disruption -{context.State.Rules.Arena.RallyDamageReductionPercent}% direct damage until tick {rallyUntil}"; }
        if (!ReadOnlyLive && !_preview && unit.Owner == context.City.Id && unit.Health > 0 && unit.Assignment is not null)
        {
            VeteranButton.Show(); VeteranButton.Text = _veteranScroll.Visible ? "Hide recovery / replacement" : "Compare recovery / replacement";
            var comparison = VeteranComparison.Project(context.State, context.City, unit.Id);
            _veteran.Text = comparison.Summary + "\n\n" + comparison.Details;
        }
        else _veteranScroll.Hide();
        if (_reviewRetirement && (_retireMatch != context.State.MatchId || !ReferenceEquals(_retireSession, context.Session))) _reviewRetirement = false;
        if (!ReadOnlyLive && !_preview && unit.Owner == context.Session.PlayerId && context.City.Id == unit.Owner && unit.Health > 0 && unit.Assignment is { } assignment)
        {
            _hall.Visible = city?.Army?.Halls.Length > 0; _squad.Show();
            NativeOptions.Update(_hall, city?.Army?.Halls.Select(h => (h.Slot, $"Town Hall plot {h.Slot + 1} — {city.Soldiers.Count(u => u.Assignment is { Stored: true } a && a.HallSlot == h.Slot)}/{h.Capacity}")) ?? []);
            NativeOptions.Update(_squad, new[] { (0, "Automatic compatible squad"), (int.MaxValue, "New squad / free home") }.Concat(city!.Soldiers
                .Where(u => u.Assignment is { Stored: false } && u.Type == unit.Type && u.Id != unit.Id).GroupBy(u => u.SquadId)
                .Select(g => (g.Key, ArmyDeploymentView.SquadLabel(city.Soldiers.Where(u => u.SquadId == g.Key && u.Assignment is { Stored: false }), city.Army?.SquadCapacity ?? 3)))));
            if (_preferredSquad is { } preferred) { int index = _squad.GetItemIndex(preferred); if (index >= 0) _squad.Select(index); _preferredSquad = null; }
            _destination.Text = ArmyDeploymentView.Destination(city, context.Army, unit.Type, NativeOptions.Squad(_squad), unit.Id); _destination.Show();
            string destination = context.Destination(unit.Type, NativeOptions.Squad(_squad), unit.Id) is null ? "No compatible squad space or free home." : "";
            if (assignment.Stored)
                _actions.Item(_skin, "Return", "Return veteran to selected field squad", () => ActMember(GameplayAction.Send), context.Block(reason: destination));
            else
            {
                _actions.Item(_skin, "Regroup", "Transfer member to selected squad", () => ActMember(GameplayAction.Regroup), context.Block(reason: destination));
                var hall = city.Army?.Halls.FirstOrDefault(h => h.Slot == _hall.GetSelectedId());
                string hallReason = hall is null ? "Build a Town Hall first." : city.Soldiers.Count(u => u.Assignment is { Stored: true } a && a.HallSlot == hall.Slot && a.HallGeneration == hall.Generation) >= hall.Capacity ? "Town Hall is full." : "";
                _actions.Item(_skin, "Store", "Store veteran in selected Town Hall", () => ActMember(GameplayAction.Store), context.Block(reason: hallReason));
            }
            _actions.Item(_skin, "Retire", "Review retirement — no refund", ReviewRetirement, context.Block());
            if (_reviewRetirement)
            {
                _veteranScroll.Show();
                _actions.Item(_skin, "ConfirmRetire", $"Confirm retire {unit.Type} L{unit.Level} permanently — no refund", ConfirmRetirement, context.Block());
                _actions.Item(_skin, "CancelRetire", "Keep soldier / cancel retirement", () => { _reviewRetirement = false; Refresh(_context); _retireInvoker?.GrabFocus(); });
            }
        }
        else if (!ReadOnlyLive) _text.Text += "\nInspection only.";
        _actions.End();
    }
    public void CancelRetirement()
    {
        _reviewRetirement = false; _retireSession = null; SetProcess(false);
        foreach (string key in new[] { "ConfirmRetire", "CancelRetire" })
            if (_actions.Buttons.TryGetValue(key, out var button)) button.Hide();
    }
    private static string LiveTarget(UnitState unit, UnitState[] units, StructureState[] structures, int local)
    {
        string Ally(int owner) => owner == local ? "Your" : "Ally " + owner;
        if (CombatInspection.TargetMembers(unit, units).FirstOrDefault(u => u.Health > 0) is { } target)
            return $"{(target.Faction == Faction.Adventurers ? Ally(target.Owner) : "Enemy")} {target.Type}";
        if (CombatInspection.TargetStructure(unit, structures) is { } structure) return $"{Ally(structure.Owner)} {structure.Kind}";
        return "None";
    }
    private void ReviewRetirement()
    {
        if (!IsVisibleInTree() || _context.Block().Length != 0) return;
        _reviewRetirement = true; _retireMatch = _context.State.MatchId; _retireSession = _context.Session;
        _retireInvoker = _actions.Buttons["Retire"]; Refresh(_context);
        _actions.Buttons["CancelRetire"].GrabFocus();
        _retireFocusFrames = 1; SetProcess(true);
    }
    private void ConfirmRetirement()
    {
        if (!IsVisibleInTree() || !_reviewRetirement || _retireMatch != _context.State.MatchId || !ReferenceEquals(_retireSession, _context.Session) || _context.Block().Length != 0) return;
        _reviewRetirement = false; ActMember(GameplayAction.Retire); Refresh(_context);
        if (IsInstanceValid(FallbackFocus) && FallbackFocus!.IsVisibleInTree()) FallbackFocus.GrabFocus();
    }
    private void ActMember(GameplayAction action)
    {
        var unit = _context.City.Soldiers.FirstOrDefault(u => u.Id == _id); if (unit is null) return;
        int slot = action == GameplayAction.Store ? _hall.GetSelectedId() : action == GameplayAction.Send ? unit.Assignment?.HallSlot ?? -1 : -1;
        _context.Submit(new(action, slot, UnitId: unit.Id, SquadId: NativeOptions.Squad(_squad)));
    }
    public static string Capabilities(UnitCapabilities c)
    {
        var parts = new List<string>();
        void Add(string label, int value) { if (value != 0) parts.Add($"{label} {value:+0;-0}%"); }
        Add("Foundation HP/damage", c.FoundationPercent); Add("Direct damage", c.DirectDamagePercent); Add("Damage reduction", c.ReductionPercent);
        Add("Burn strength", c.BurnPercent); Add("Poison strength/stack", c.PoisonPercent); Add("Chill duration", c.ChillPercent);
        Add("HP", c.HealthPercent); Add("Primary", c.PrimaryPercent); Add("Secondary", c.SecondaryPercent);
        if (c.RangeBonus != 0) parts.Add($"Range +{c.RangeBonus}");
        return parts.Count > 0 ? string.Join("; ", parts) : "None";
    }
    private static string Statuses(StatusState state, long tick)
    {
        var text = new List<string>();
        void Add(string kind, StatusEffect? effect, bool percent = false)
        {
            if (effect is null || effect.ExpiresTick <= tick) return;
            text.Add($"{kind}: {(percent ? effect.Strength + "%" : HealthPoints.Format(effect.Strength) + "/tick")}, {effect.ExpiresTick - tick} ticks left; next damage {effect.NextDamageTick}");
        }
        Add("Burn", state.Burn); foreach (var poison in state.Poison) Add("Poison stack", poison); Add("Chill", state.Chill, true);
        return text.Count == 0 ? "Statuses: none" : string.Join("\n", text);
    }
}
