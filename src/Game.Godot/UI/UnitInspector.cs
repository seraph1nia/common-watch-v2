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
    internal (int Owner, ArenaStructureKind Kind, int Slot)? SelectedStructure => _structure;
    private OptionButton _hall = null!, _squad = null!;
    private ActionList _actions = null!;
    private int _id;
    private bool _preview;
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
        AdvancedButton = skin.Button("Advanced details ▾", () => { _advanced.Visible = !_advanced.Visible; AdvancedButton.Text = _advanced.Visible ? "Advanced details ▴" : "Advanced details ▾"; });
        AddChild(AdvancedButton);
        _advanced = WrapLabel(); _advanced.AddThemeFontSizeOverride("font_size", 13); AddChild(_advanced); _advanced.Hide();
        _hall = new OptionButton(); AddChild(_hall);
        _squad = new OptionButton(); AddChild(_squad);
        _squad.ItemSelected += _ => Refresh(_context);
        _hall.ItemSelected += _ => Refresh(_context);
        _actions = new ActionList(); AddChild(_actions);
    }
    private void ResetAdvanced() { _advanced.Hide(); AdvancedButton.Text = "Advanced details ▾"; }
    public void Clear() { _id = 0; _structure = null; _text.Text = "Select a soldier, enemy or structure."; ResetAdvanced(); AdvancedButton.Hide(); _hall.Hide(); _squad.Hide(); _actions.Begin(); _actions.End(); }
    public void Select(UnitState unit, bool preview, Phase phase) { if (_id != unit.Id) ResetAdvanced(); _structure = null; _id = unit.Id; _preview = preview; _selectedPhase = phase; }
    public void Select(StructureState structure) { if (_structure != (structure.Owner, structure.Kind, structure.Slot)) ResetAdvanced(); _id = 0; _structure = (structure.Owner, structure.Kind, structure.Slot); }
    public void Refresh(GameplayContext context, CombatPlayback? playback = null)
    {
        _context = context; _actions.Begin(); _hall.Hide(); _squad.Hide();
        AdvancedButton.Visible = _id != 0 || _structure is not null;
        var units = playback?.Units() ?? context.State.Players.SelectMany(p => p.Soldiers).Concat(context.State.Enemies).Concat(context.State.DyingBodies).ToArray();
        double tick = playback?.Tick ?? context.State.Tick;
        if (_structure is { } identity)
        {
            var structure = (playback?.Structures() ?? context.State.Structures).FirstOrDefault(s => s.Owner == identity.Owner && s.Kind == identity.Kind && s.Slot == identity.Slot);
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
        _text.Text = CombatInspection.Basic(unit, _preview ? context.State.NextWave : units, tick, _preview, playback?.Structures() ?? context.State.Structures);
        if (!ReadOnlyLive && city is not null) _text.Text += $"\nservice XP {unit.Experience}\n" +
            (unit.Assignment is { Stored: true } stored ? $"Stored: Town Hall plot {stored.HallSlot + 1}; never fights" : unit.Assignment is { } basicHome ? $"Army home {basicHome.Tile + 1}; {(unit.Deployed ? "field" : "queued")}" : "No persistent home")
            + $"\n{funding}; paid recovery {(unit.RecoveryEligible ? "eligible" : "not eligible")}";
        _text.Text += $"\nTraits: {Capabilities(unit.Capabilities)}";
        _advanced.Text = $"#{unit.Id} {unit.Type} · {unit.Class}\n{unit.Faction} · owner P{unit.Owner} · L{unit.Level}, rank {unit.Rank}{(unit.IsBoss ? " BOSS" : "")}\nHP {HealthPoints.Format(unit.Health)}/{HealthPoints.Format(p.Health)}; service XP {unit.Experience}\nDamage direct/secondary {HealthPoints.Format(p.Damage)}/{HealthPoints.Format(p.SecondaryDamage)}\nStructure damage {HealthPoints.Format(p.StructureDamage)}; range {p.HexRange}\nMovement {p.MoveTicks}, windup {p.WindupTicks}, cadence {p.CadenceTicks} ticks\nSquad #{unit.SquadId}, member slot {unit.FormationSlot}\n" +
            (unit.Assignment is { Stored: true } a ? $"Stored: Town Hall plot {a.HallSlot + 1}, place {a.Tile + 1}; never fights" : unit.Assignment is { } home ? $"Army home {home.Tile + 1}; {(unit.Deployed ? "field" : "queued")}" : "No persistent home") +
            $"\n{funding}; paid recovery {(unit.RecoveryEligible ? "eligible" : "not eligible")}\nCapabilities: {Capabilities(unit.Capabilities)}\n{Statuses(unit.Statuses, context.State.Tick)}\nTarget {unit.TargetKind} #{unit.TargetId}; action {unit.Hex?.Action}\nCommitted start {unit.ActionStartTick}, impact {unit.ImpactTick}, ready {unit.ReadyTick}";
        long rallyUntil = playback?.RallyDisruptions().GetValueOrDefault(unit.SquadId)
            ?? context.State.Squads.FirstOrDefault(s => s.Id == unit.SquadId)?.RallyDisruptedUntil ?? 0;
        if (rallyUntil > tick) { _text.Text += "\nRally disrupted"; _advanced.Text += $"\nRally disruption -{context.State.Rules.Arena.RallyDamageReductionPercent}% direct damage until tick {rallyUntil}"; }
        if (!ReadOnlyLive && !_preview && unit.Owner == context.Session.PlayerId && context.City.Id == unit.Owner && unit.Health > 0 && unit.Assignment is { } assignment)
        {
            _hall.Visible = city?.Army?.Halls.Length > 0; _squad.Show();
            NativeOptions.Update(_hall, city?.Army?.Halls.Select(h => (h.Slot, $"Town Hall plot {h.Slot + 1} — {city.Soldiers.Count(u => u.Assignment is { Stored: true } a && a.HallSlot == h.Slot)}/{h.Capacity}")) ?? []);
            NativeOptions.Update(_squad, new[] { (0, "Automatic compatible squad"), (int.MaxValue, "New squad / free home") }.Concat(city!.Soldiers
                .Where(u => u.Assignment is { Stored: false } && u.Type == unit.Type && u.Id != unit.Id).GroupBy(u => u.SquadId)
                .Select(g => (g.Key, $"Squad #{g.Key} {unit.Type} — {g.Count()}/{city.Army?.SquadCapacity}"))));
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
            _actions.Item(_skin, "Retire", "Retire veteran — no refund", () => ActMember(GameplayAction.Retire), context.Block());
        }
        else _text.Text += "\nInspection only.";
        _actions.End();
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
