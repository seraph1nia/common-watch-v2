using Godot;
using Game.Core;
using Game.Shared;
using NumVector = System.Numerics.Vector2;

namespace Game.GodotClient;

/// <summary>Godot presentation only: every edit goes through SoloSession.</summary>
public partial class Main : Node2D
{
    private SoloSession _session = null!;
    private RenderOptions _options = null!;
    private readonly HexProjection _settlement = new(65);
    private readonly HexProjection _arena = new(30);
    private readonly Dictionary<string, Texture2D?> _textures = [];
    private readonly List<(CombatEvent Event, double Until)> _effects = [];
    private readonly List<Button> _rewardButtons = [];
    private readonly Dictionary<Building, Button> _buildButtons = [];
    private Label _resources = null!, _phase = null!, _selection = null!, _production = null!, _message = null!, _combatInfo = null!;
    private VBoxContainer _actions = null!;
    private PanelContainer _draft = null!;
    private HBoxContainer _cards = null!;
    private Button _ready = null!;
    private Button? _recruit;
    private int _spriteDecodeAttempts;
    private string _uiKey = "";
    private Vector2 _pan;
    private float _zoom = 1;
    private bool _dragging;
    private bool _frozen;
    private double _time;
    private readonly Color _ink = new("e7edf2"), _muted = new("a9bcc7"), _gold = new("efd478");
    private const float SideWidth = 368, HeaderHeight = 100;
    private string[] _args = [];

    public override async void _Ready()
    {
        try
        {
            _args = OS.GetCmdlineUserArgs();
            if (Argument("--width") is { } width && Argument("--height") is { } height) GetWindow().Size = new Vector2I(int.Parse(width), int.Parse(height));
            _options = RenderOptions.Parse(_args);
            if (Argument("--assets-root") is null) _options = _options with { AssetsRoot = AssetDirectory("sprites") };
            InitializeMusic();
            string? checkpoint = Argument("--checkpoint");
            _session = checkpoint is null ? new SoloSession() : ScenarioRunner.CreateAt(checkpoint);
            if (checkpoint is null) _session.Start();
            _frozen = checkpoint is not null;
            CreateUi();
            RefreshUi(true);
            QueueRedraw();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (_args.Contains("--smoke-test")) await SmokeIntegration();
            if (_args.Contains("--capture-settings")) OpenSettings();
            if (Argument("--capture") is { } path)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                string absolute = System.IO.Path.GetFullPath(path);
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(absolute)!);
                Error result = GetViewport().GetTexture().GetImage().SavePng(absolute);
                if (result != Error.Ok) throw new InvalidOperationException($"Viewport capture failed: {result}");
                GD.Print($"GODOT_CAPTURE {absolute} phase={_session.Snapshot.Phase} mode={_options.Mode}");
                await QuitAfterAudioDrain();
            }
            else if (_args.Contains("--smoke-test")) await QuitAfterAudioDrain();
        }
        catch (Exception exception)
        {
            GD.PrintErr(exception);
            GetTree().Quit(1);
        }
    }

    private string? Argument(string key)
    {
        int at = Array.IndexOf(_args, key);
        return at >= 0 && at + 1 < _args.Length ? _args[at + 1] : null;
    }

    private static string AssetDirectory(string folder) => OS.HasFeature("editor")
        ? ProjectSettings.GlobalizePath("res://../../assets/" + folder)
        : System.IO.Path.Combine(System.IO.Path.GetDirectoryName(OS.GetExecutablePath())!, "assets", folder);

    private void CreateUi()
    {
        var layer = new CanvasLayer(); AddChild(layer);
        var root = new Control(); layer.AddChild(root); root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.MouseFilter = Control.MouseFilterEnum.Ignore;
        var theme = new Theme { DefaultFontSize = 16 }; root.Theme = theme;
        var header = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.AddChild(header); header.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide); header.OffsetBottom = HeaderHeight;
        var top = new VBoxContainer(); header.AddChild(Margin(top, 14));
        top.AddChild(new Label { Text = "THE COMMON WATCH  /  GODOT 2D", Modulate = _gold });
        _phase = new Label(); top.AddChild(_phase);
        _resources = new Label(); top.AddChild(_resources);
        var side = new PanelContainer(); root.AddChild(side);
        side.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.RightWide);
        side.OffsetLeft = -SideWidth; side.OffsetTop = HeaderHeight + 8; side.OffsetBottom = -50; side.OffsetRight = -8;
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; side.AddChild(Margin(scroll, 12));
        var column = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; scroll.AddChild(column);
        _selection = WrapLabel(); column.AddChild(_selection);
        _production = WrapLabel(); column.AddChild(_production);
        _actions = new VBoxContainer(); column.AddChild(_actions);
        column.AddChild(new HSeparator());
        _ready = Button("Advance preparation / Ready", () => Act(() => _session.Send("ready"))); column.AddChild(_ready);
        _combatInfo = WrapLabel(); column.AddChild(_combatInfo);
        column.AddChild(Button("Reset camera [R]", ResetCamera));
        column.AddChild(Button("New solo game", () => { ReplaceSession(new SoloSession()); _session.Start(); _frozen = false; RefreshUi(true); }));
        column.AddChild(WrapLabel("Select: left click • Pan: middle/right drag\nZoom: wheel • Reset: R\nOpening: Farm, Barracks, Metal Mine.\nAt zero wood, use Lumbermill recovery.\nReady, recruit two Swordsmen; Ready, recruit two more; Ready to fight.\nReady settles real resource production."));
        var footer = new PanelContainer(); root.AddChild(footer); footer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide); footer.OffsetTop = -44;
        _message = WrapLabel("Choose a settlement plot. All actions use the shared authoritative simulation."); footer.AddChild(Margin(_message, 10));
        _draft = new PanelContainer(); root.AddChild(_draft); _draft.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        _draft.AddThemeStyleboxOverride("panel", PanelStyle(new Color("14212c"), _gold));
        _draft.OffsetLeft = -560; _draft.OffsetRight = 560; _draft.OffsetTop = -210; _draft.OffsetBottom = 210;
        var draftColumn = new VBoxContainer(); _draft.AddChild(Margin(draftColumn, 20));
        draftColumn.AddChild(new Label { Text = "WAVE COMPLETE — CHOOSE ONE REWARD", Modulate = _gold });
        _cards = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill }; draftColumn.AddChild(_cards);
        _draft.Visible = false;
        CreateAudioUi(root);
    }

    private static StyleBoxFlat PanelStyle(Color background, Color border) => new()
    {
        BgColor = background, BorderColor = border,
        BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
        CornerRadiusTopLeft = 5, CornerRadiusTopRight = 5, CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5
    };
    private static MarginContainer Margin(Control child, int padding)
    {
        var margin = new MarginContainer();
        foreach (string side in new[] { "left", "top", "right", "bottom" }) margin.AddThemeConstantOverride("margin_" + side, padding);
        margin.AddChild(child); return margin;
    }
    private static Label WrapLabel(string text = "") => new() { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    private static Button Button(string text, Action clicked)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(0, 36) }; button.Pressed += clicked; return button;
    }
    private void Act(Func<CommandResult> action)
    {
        CommandResult result = action(); _message.Text = result.Message;
        _message.Modulate = result.Accepted ? _ink : new Color("ffb3a6");
        _frozen = false; RefreshUi(true); QueueRedraw();
    }
    private void ReplaceSession(SoloSession session) { _session.Dispose(); _session = session; if (_message is not null) _message.Text = "Select a plot to inspect its authoritative actions."; _effects.Clear(); _uiKey = ""; ResetCamera(); }
    private void ResetCamera() { _pan = Vector2.Zero; _zoom = 1; QueueRedraw(); }

    public override void _Process(double delta)
    {
        if (_session is null || _resources is null) return;
        _time += delta;
        if (!_frozen) _session.Advance(delta);
        foreach (CombatEvent entry in _session.Playback.Drain()) _effects.Add((entry, _time + .55));
        _effects.RemoveAll(e => e.Until < _time);
        RefreshUi(); QueueRedraw();
    }

    private void RefreshUi(bool force = false)
    {
        var state = _session.Snapshot; var city = _session.City;
        _phase.Text = $"Wave {state.Wave}/{state.TotalWaves}  •  {state.Phase}  •  preparation {state.Turn}  •  city HP {HealthPoints.Format(city.Health)}/{state.Rules.CityHealth}  •  {_options.Mode}";
        _resources.Text = $"Gold {city.Gold}   Wood {city.Wood}   Food {city.Food}   Stone {city.Stone}   Metal {city.Metal}   Cloth {city.Cloth}   •   Swordsmen {city.Soldiers.Count(u => u.Type == UnitType.Swordsman)}";
        _ready.Visible = state.Phase != Phase.Combat;
        _production.Visible = state.Phase != Phase.Combat; _actions.Visible = state.Phase != Phase.Combat;
        _ready.Disabled = state.Phase is not (Phase.Building or Phase.Preparation);
        _combatInfo.Text = state.Phase == Phase.Combat ? $"Automatic combat • tick {state.Tick}\nBlue: friendly  Red: enemy\nLiving friends: {city.Soldiers.Count(u => u.Health > 0)}  Enemies: {state.Enemies.Count(u => u.Health > 0)}"
            + "\n\n" + string.Join("\n", city.Soldiers.Concat(state.Enemies).Where(u => u.Health > 0)
                .Select(u => $"{(u.Faction == Faction.Adventurers ? "S" : "E")}{u.Id} {(u.Faction == Faction.Adventurers ? "" : "Enemy ")}{u.Type} HP: {HealthPoints.Format(u.Health)}/{HealthPoints.Format(u.Profile.Health)}"))
            : state.Phase is Phase.Defeat or Phase.Victory ? $"{state.Phase}. Use New solo game to restart." : "Preparing for the next wave.";
        _production.Text = "Next production: " + (city.ProductionIncome is { } income ? RunPresentation.Resources(income) : "None");
        string key = $"{state.Phase}|{_session.SelectedSlot}|{city.Resources}|{string.Join(',', city.Slots.Select(s => $"{s.Type}:{s.Level}:{s.Purchased}"))}|{string.Join(',', city.Run.Offers)}";
        if (!force && key == _uiKey) return; _uiKey = key;
        foreach (Node child in _actions.GetChildren()) { _actions.RemoveChild(child); child.QueueFree(); }
        _buildButtons.Clear(); _recruit = null;
        int slotIndex = _session.SelectedSlot;
        if (slotIndex is >= 0 and < 9)
        {
            var slot = city.Slots[slotIndex];
            _selection.Text = $"PLOT {slotIndex + 1}\n{(slot.Purchased ? slot.Type == Building.Empty ? "Empty plot" : $"{slot.Type} — level {slot.Level}" : "Unpurchased plot")}";
            bool editable = state.Phase is Phase.Building or Phase.Preparation;
            if (!editable) _actions.AddChild(WrapLabel("Settlement actions are available during preparation."));
            else if (!slot.Purchased) _actions.AddChild(Button("Buy plot", () => Act(() => _session.Send("buy-plot", slotIndex))));
            else if (slot.Type == Building.Empty)
            {
                foreach (Building type in new[] { Building.Lumbermill, Building.Farm, Building.Barracks, Building.MetalMine, Building.Mine })
                {
                    var definition = state.BuildingCatalog.First(b => b.Type == type);
                    var button = Button($"Build {type} ({RunPresentation.Resources(definition.Construction)})", () => Act(() => _session.Send("build", slotIndex, type)));
                    _buildButtons[type] = button; _actions.AddChild(button);
                }
                if (city.Wood == 0 && state.BuildingCatalog.First(b => b.Type == Building.Lumbermill).RecoveryConstruction is { } recovery) _actions.AddChild(Button("Lumbermill recovery (" + RunPresentation.Resources(recovery) + ")", () => Act(() => _session.Send("build", slotIndex, Building.Lumbermill, payment: ConstructionPayment.GoldRecovery))));
                _actions.AddChild(WrapLabel("If resources are insufficient, the authority explains the missing requirement when you choose an action."));
            }
            else
            {
                if (slot.UpgradeQuote is { } upgrade) _actions.AddChild(Button("Upgrade (" + RunPresentation.Resources(upgrade) + ")", () => Act(() => _session.Send("upgrade", slotIndex))));
                if (slot.Type == Building.Barracks)
                {
                    var quote = city.RecruitmentQuotes.First(q => q.Type == UnitType.Swordsman && q.Level == slot.Level);
                    _recruit = Button("Recruit Swordsman (" + RunPresentation.Resources(quote.Cost) + ")", () => Act(() => _session.Send("recruit", slotIndex)));
                    _actions.AddChild(_recruit);
                    _actions.AddChild(WrapLabel($"Swordsman upkeep: {quote.Upkeep} Food. Health: {HealthPoints.Format(quote.Profile.Health)}. Damage: {HealthPoints.Format(quote.Profile.Damage)}."));
                }
                _actions.AddChild(Button("Sell (refund " + RunPresentation.Resources(slot.Refund) + ")", () => Act(() => _session.Send("sell", slotIndex))));
            }
        }
        else _selection.Text = "SETTLEMENT\nSelect one of the nine hex plots to build, inspect or recruit.";
        if (state.Phase == Phase.Combat) _selection.Text = "BATTLEFIELD";
        _draft.Visible = state.Phase == Phase.RewardDraft;
        foreach (Node child in _cards.GetChildren()) { _cards.RemoveChild(child); child.QueueFree(); }
        _rewardButtons.Clear();
        if (_draft.Visible)
            foreach (string id in city.Run.Offers)
            {
                var card = RunCatalog.Card(id);
                var panel = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
                panel.AddThemeStyleboxOverride("panel", PanelStyle(new Color("20313e"), new Color("526a79")));
                var content = new VBoxContainer(); panel.AddChild(Margin(content, 15));
                content.AddChild(WrapLabel(card.Name)); content.AddChild(WrapLabel(card.Rarity.ToString()));
                content.AddChild(WrapLabel(card.Effect + (card.Multiplied ? $"\nResources multiplied by {RunEffects.Multiplier(city.Run.DraftWave)}." : "")));
                var choose = Button("Choose reward", () => Act(() => _session.ChooseReward(id))); content.AddChild(choose);
                _rewardButtons.Add(choose); _cards.AddChild(panel);
            }
    }

    private Vector2 WorldOrigin => new((GetViewportRect().Size.X - SideWidth) / 2, HeaderHeight + (GetViewportRect().Size.Y - HeaderHeight - 48) / 2);
    private Vector2 ArenaCenter()
    {
        var coordinates = _session.Snapshot.Arena?.Board.Cells.Select(c => V(_arena.Project(c.Coordinate))).ToArray() ?? [];
        return coordinates.Length == 0 ? Vector2.Zero : new Vector2((coordinates.Min(v => v.X) + coordinates.Max(v => v.X)) / 2, (coordinates.Min(v => v.Y) + coordinates.Max(v => v.Y)) / 2);
    }
    private Vector2 SettlementCenter => new(0, 65 * 1.5f * 3);
    private Vector2 SettlementScreen(NumVector point) => Screen(V(point) - SettlementCenter);
    private static Vector2 V(NumVector value) => new(value.X, value.Y);
    private Vector2 Screen(Vector2 world) => WorldOrigin + _pan + world * _zoom;
    private Vector2 World(Vector2 screen) => (screen - WorldOrigin - _pan) / _zoom;

    public override void _Draw()
    {
        if (_session is null) return;
        if (_session.Snapshot.Phase == Phase.Combat) DrawCombat(); else DrawSettlement();
    }
    private void DrawSettlement()
    {
        DrawString(ThemeDB.FallbackFont, new Vector2(22, HeaderHeight + 28), "SETTLEMENT • 9 PLOTS", HorizontalAlignment.Left, -1, 19, _muted);
        for (int i = 0; i < SettlementLayout.Coordinates.Count; i++)
        {
            var coordinate = SettlementLayout.Coordinates[i]; var slot = _session.City.Slots[i];
            var center = SettlementScreen(_settlement.Project(coordinate));
            DrawHex(_settlement.Corners(coordinate).Select(SettlementScreen).ToArray(), slot.Purchased ? new Color("304a40") : new Color("252e34"), i == _session.SelectedSlot ? _gold : new Color("62796f"), i == _session.SelectedSlot ? 4 : 2);
            Sprite("terrain/hex", center + new Vector2(0, 30) * _zoom, new Vector2(110, 110) * _zoom);
        }
        // Keep borders and selection readable above optional opaque terrain textures.
        for (int i = 0; i < SettlementLayout.Coordinates.Count; i++)
        {
            var corners = _settlement.Corners(SettlementLayout.Coordinates[i]).Select(SettlementScreen).ToArray();
            DrawPolyline(corners.Append(corners[0]).ToArray(), i == _session.SelectedSlot ? _gold : new Color("62796f"), i == _session.SelectedSlot ? 4 : 2, true);
        }
        // Complete terrain before drawing anchored objects, including future tall sprites.
        foreach (int i in Enumerable.Range(0, SettlementLayout.Coordinates.Count)
                     .OrderBy(i => _settlement.Project(SettlementLayout.Coordinates[i]).Y)
                     .ThenBy(i => _settlement.Project(SettlementLayout.Coordinates[i]).X))
        {
            var coordinate = SettlementLayout.Coordinates[i]; var slot = _session.City.Slots[i];
            var center = SettlementScreen(_settlement.Project(coordinate));
            if (slot.Type != Building.Empty)
            {
                if (!Sprite(VisualAssets.BuildingId(slot.Type), center, new Vector2(75, 82) * _zoom))
                {
                    DrawRect(new Rect2(center - new Vector2(24, 26) * _zoom, new Vector2(48, 43) * _zoom), new Color("92b5a5"));
                    DrawLine(center + new Vector2(-28, -26) * _zoom, center + new Vector2(0, -44) * _zoom, _gold, 3);
                    DrawLine(center + new Vector2(0, -44) * _zoom, center + new Vector2(28, -26) * _zoom, _gold, 3);
                }
                TextCentered(slot.Type.ToString(), center + new Vector2(0, 37) * _zoom, 15);
                TextCentered($"Lv {slot.Level}", center + new Vector2(0, 54) * _zoom, 12);
            }
            else
            {
                TextCentered(slot.Purchased ? "Empty" : "Buy plot", center + new Vector2(0, 6), 16);
                TextCentered($"{i + 1}", center + new Vector2(0, 26), 13);
            }
        }
    }
    private void DrawCombat()
    {
        var state = _session.Snapshot; var arena = state.Arena; if (arena is null) return;
        Vector2 center = ArenaCenter();
        DrawString(ThemeDB.FallbackFont, new Vector2(22, HeaderHeight + 28), "SHARED COMBAT ARENA", HorizontalAlignment.Left, -1, 19, _muted);
        foreach (var cell in arena.Board.Cells)
        {
            DrawHex(_arena.Corners(cell.Coordinate).Select(c => Screen(V(c) - center)).ToArray(), new Color("23343d"), new Color("3f555d"), 1);
            Sprite("terrain/hex", Screen(V(_arena.Project(cell.Coordinate)) - center) + new Vector2(0, 18) * _zoom,
                new Vector2(52, 60) * _zoom);
        }
        foreach (var cell in arena.Board.Cells)
        {
            var corners = _arena.Corners(cell.Coordinate).Select(c => Screen(V(c) - center)).ToArray();
            DrawPolyline(corners.Append(corners[0]).ToArray(), new Color("3f555d"), 1, true);
        }
        foreach (var structure in _session.Playback.Structures())
        {
            if (!structure.Active || structure.Health <= 0) continue;
            var cell = arena.Board.Cells.FirstOrDefault(c => c.Id == structure.Cell); if (cell is null) continue;
            Vector2 p = Screen(V(_arena.Project(cell.Coordinate)) - center);
            DrawRect(new Rect2(p - new Vector2(17, 17) * _zoom, new Vector2(34, 34) * _zoom), new Color("9b9872"));
            TextCentered(structure.Kind.ToString(), p + new Vector2(0, 28), 11);
            DrawRect(new Rect2(p + new Vector2(-22, -25), new Vector2(44, 5)), new Color("121a22"));
            DrawRect(new Rect2(p + new Vector2(-22, -25), new Vector2(44f * structure.Health / Math.Max(1, structure.MaximumHealth), 5)), new Color("69dbae"));
        }
        var units = _session.Playback.Units().OrderBy(u => CombatVisuals.Position(u, state, _session.Playback.Tick).Y).ToArray();
        var positions = units.ToDictionary(u => u.Id, u => Screen(V(CombatVisuals.Position(u, state, _session.Playback.Tick)) - center));
        foreach (var unit in units)
        {
            Vector2 p = positions[unit.Id]; bool friendly = unit.Faction == Faction.Adventurers;
            Color color = friendly ? new Color("63a9ed") : new Color("ed7778");
            if (!Sprite(VisualAssets.UnitId(unit), p + new Vector2(0, 7), new Vector2(30, 43) * _zoom)) DrawCircle(p, 8 * _zoom, color);
            if (unit.TargetId != 0 && unit.PendingImpact && positions.TryGetValue(unit.TargetId, out var target)) DrawLine(p, target, color.Lightened(.3f), 2);
            TextCentered((friendly ? "S" : "E") + unit.Id, p + new Vector2(0, 5), 9, new Color("0d1822"));
            float max = Math.Max(unit.Profile.Health, 1); float fraction = Math.Clamp(unit.Health / max, 0, 1);
            float healthY = -15;
            DrawLine(p + new Vector2(0, -9), p + new Vector2(0, healthY), color.Darkened(.3f), 1);
            DrawRect(new Rect2(p + new Vector2(-9, healthY), new Vector2(18, 4)), new Color("121a22"));
            DrawRect(new Rect2(p + new Vector2(-9, healthY), new Vector2(18 * fraction, 4)), friendly ? new Color("69dbae") : new Color("fa9a8c"));
        }
        foreach (var (entry, until) in _effects)
        {
            if (entry.Unit is null) continue;
            Vector2 p = positions.GetValueOrDefault(entry.Unit.Id, Screen(V(CombatVisuals.Position(entry.Unit, state, entry.Tick)) - center));
            if (entry.Type is CombatEventType.Hit or CombatEventType.StructureHit) TextCentered("-" + HealthPoints.Format(entry.Damage), p + new Vector2(0, -42 - (float)(.55 - (until - _time)) * 25), 18, _gold);
            if (entry.Type is CombatEventType.AttackStarted or CombatEventType.Impact && positions.TryGetValue(entry.TargetId, out var target)) DrawLine(p, target, _gold, 3);
            if (entry.Type == CombatEventType.Death) DrawArc(p, 18 + (float)(.55 - (until - _time)) * 20, 0, Mathf.Tau, 24, _muted, 2);
        }
    }
    private void DrawHex(Vector2[] corners, Color fill, Color border, float width)
    {
        DrawColoredPolygon(corners, fill); DrawPolyline(corners.Append(corners[0]).ToArray(), border, width, true);
    }
    private void TextCentered(string text, Vector2 position, int size, Color? color = null)
    {
        float width = ThemeDB.FallbackFont.GetStringSize(text, HorizontalAlignment.Left, -1, size).X;
        DrawString(ThemeDB.FallbackFont, position - new Vector2(width / 2, 0), text, HorizontalAlignment.Left, -1, size, color ?? _ink);
    }
    private bool Sprite(string id, Vector2 anchor, Vector2 size)
    {
        // Debug mode returns before path resolution or disk access, even if artwork exists.
        if (_options.Mode != RenderMode.Sprites) return false;
        if (!_textures.TryGetValue(id, out var texture))
        {
            texture = null;
            if (VisualAssets.Resolve(_options.AssetsRoot, id, _options.Mode) is { } path)
            {
                try
                {
                    _spriteDecodeAttempts++;
                    using var source = System.IO.File.OpenRead(path);
                    Span<byte> signature = stackalloc byte[8];
                    if (source.Read(signature) != 8 || !signature.SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
                        throw new InvalidDataException("Invalid PNG header");
                    using var image = new Image();
                    if (image.Load(path) == Error.Ok && !image.IsEmpty()) texture = ImageTexture.CreateFromImage(image);
                }
                catch (Exception exception) { GD.PushWarning($"Optional sprite {id}: {exception.Message}. Using shapes."); }
            }
            _textures[id] = texture;
        }
        if (texture is null) return false;
        DrawTextureRect(texture, new Rect2(anchor - new Vector2(size.X / 2, size.Y * .8f), size), false); return true;
    }

    public override void _Input(InputEvent input)
    {
        if (input is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
        {
            if (_settingsOverlay.Visible) CloseSettings(); else OpenSettings();
            GetViewport().SetInputAsHandled();
        }
        if (input is InputEventMouseButton { Pressed: false } mouse && mouse.ButtonIndex is MouseButton.Middle or MouseButton.Right) _dragging = false;
    }
    public override void _UnhandledInput(InputEvent input)
    {
        if (_session is null) return;
        if (_settingsOverlay.Visible) return;
        if (input is InputEventKey { Pressed: true, Keycode: Key.R }) ResetCamera();
        if (input is InputEventMouseButton mouse)
        {
            if (mouse.ButtonIndex is MouseButton.Middle or MouseButton.Right) _dragging = mouse.Pressed;
            if (mouse.Position.X >= GetViewportRect().Size.X - SideWidth || mouse.Position.Y <= HeaderHeight) return;
            if (mouse.Pressed && mouse.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
            {
                Vector2 before = World(mouse.Position);
                _zoom = Math.Clamp(_zoom * (mouse.ButtonIndex == MouseButton.WheelUp ? 1.12f : 1 / 1.12f), .55f, 2.2f);
                _pan = mouse.Position - WorldOrigin - before * _zoom;
            }
            if (mouse.Pressed && mouse.ButtonIndex == MouseButton.Left && _session.Snapshot.Phase != Phase.Combat && !_draft.Visible)
                SelectAt(mouse.Position);
        }
        if (_dragging && input is InputEventMouseMotion motion) _pan += motion.Relative;
    }
    private void SelectAt(Vector2 point)
    {
        var p = World(point) + SettlementCenter;
        var hex = _settlement.HitTest(new NumVector(p.X, p.Y), SettlementLayout.Coordinates);
        if (hex is not null) _session.SelectSlot(SettlementLayout.Coordinates.ToList().IndexOf(hex.Value));
        RefreshUi(true);
    }

    private async Task SmokeIntegration()
    {
        ulong musicPlayer = _music.GetInstanceId();
        string checkpoint = Argument("--checkpoint") ?? "settlement";
        ReplaceSession(new SoloSession()); _session.Start(); RefreshUi(true);
        SelectAt(SettlementScreen(_settlement.Project(SettlementLayout.Coordinates[0])));
        Require(_session.SelectedSlot == 0, "hex selection");
        Require(_buildButtons.TryGetValue(Building.Lumbermill, out var build), "construction UI");
        build!.EmitSignal(BaseButton.SignalName.Pressed);
        Require(_session.City.Slots[0].Type == Building.Lumbermill && _session.LastResult?.Accepted == true, "authoritative construction button");
        ReplaceSession(ScenarioRunner.CreateAt("building-selected")); RefreshUi(true);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        _ready.EmitSignal(BaseButton.SignalName.Pressed);
        Require(_session.LastResult?.Accepted == true, "ready button");
        Require(_recruit is not null, "recruitment UI");
        int roster = _session.City.Soldiers.Length;
        _recruit!.EmitSignal(BaseButton.SignalName.Pressed);
        Require(_session.LastResult?.Accepted == true && _session.City.Soldiers.Length == roster + 1, "recruitment button");
        ReplaceSession(ScenarioRunner.CreateAt("combat"));
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        long tick = _session.Snapshot.Tick;
        for (int i = 0; i < 60; i++) _session.Step();
        Require(_session.Snapshot.Tick > tick && _session.Playback.Units().Length > 0, "combat updates");
        ReplaceSession(ScenarioRunner.CreateAt("draft")); RefreshUi(true);
        Require(_rewardButtons.Count == 3 && _draft.Visible, "three reward cards");
        _rewardButtons[0].EmitSignal(BaseButton.SignalName.Pressed);
        Require(_session.Snapshot.Phase is Phase.Building or Phase.Preparation && _session.LastResult?.Accepted == true, "reward selection button");
        Vector2I original = GetWindow().Size;
        GetWindow().Size = new Vector2I(1920, 1080);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Require(GetWindow().Size == new Vector2I(1920, 1080), "1920x1080 resize");
        Require(GetViewportRect().Encloses(_resources.GetGlobalRect()), "resources remain in viewport after resize");
        Require(GetViewportRect().Encloses(_ready.GetGlobalRect()), "ready control remains in viewport after resize");
        Require(GetViewportRect().Encloses(_draft.GetGlobalRect()), "reward overlay remains in viewport after resize");
        await SmokeAudioSettings();
        GetWindow().Size = original;
        ReplaceSession(ScenarioRunner.CreateAt(checkpoint)); _frozen = true; RefreshUi(true);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await SmokeAudioSettings();
        Require(_options.Mode != RenderMode.Debug || _spriteDecodeAttempts == 0, "no-assets ignores sprite files");
        Require(_music.GetInstanceId() == musicPlayer, "music player persists across game resets and phases");
        GD.Print($"SMOKE PASS Godot mode={_options.Mode} checkpoint={checkpoint}; startup, drawing, selection, construction, recruitment, ready, combat, draft, resize; sprite_decode_attempts={_spriteDecodeAttempts}");
    }
    private static void Require(bool success, string check) { if (!success) throw new InvalidOperationException("Godot integration failed: " + check); }
    public override void _ExitTree()
    {
        _audioSettings?.Save();
        ShutdownMusic();
        _session?.Dispose();
        foreach (var texture in _textures.Values) texture?.Dispose();
    }
}
