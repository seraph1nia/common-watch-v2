using Godot;
using Game.Core;
using Game.Shared;
using NumVector = System.Numerics.Vector2;

namespace Game.GodotClient;

/// <summary>Godot presentation only: every edit goes through SoloSession.</summary>
public partial class Main : Node2D
{
    private GodotSession _session = null!;
    private RenderOptions _options = null!;
    private HexProjection _settlement = new(65);
    private HexProjection _arena = new(30);
    private readonly Dictionary<string, Texture2D?> _textures = [];
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
    private readonly Color _ink = new("e7edf2"), _muted = new("a9bcc7"), _gold = new("efd478");
    private const float SideWidth = 368, HeaderHeight = 100;
    private string[] _args = [];

    public override async void _Ready()
    {
        try
        {
            _args = OS.GetCmdlineUserArgs();
            if (_args.Contains("--check-steam-extension"))
            {
                Require(Engine.HasSingleton("Steam") && ClassDB.ClassExists("SteamMultiplayerPeer"), "pinned GodotSteam extension loaded");
                using var peer = ClassDB.Instantiate("SteamMultiplayerPeer").AsGodotObject() as MultiplayerPeer;
                Require(peer is not null, "native Steam peer converts to C# MultiplayerPeer");
                foreach (string method in new[] { "create_host", "create_client", "get_steam_id_for_peer_id" })
                    Require(peer!.HasMethod(method), "Steam peer API: " + method);
                var api = Engine.GetSingleton("Steam");
                foreach (string method in new[] { "steamInitEx", "run_callbacks", "initRelayNetworkAccess", "activateGameOverlayInviteDialog", "getLobbyOwner", "setLobbyData", "getNumLobbyMembers", "getLobbyMemberByIndex", "inviteUserToLobby", "getFriendRelationship", "getLaunchCommandLine" })
                    Require(api.HasMethod(method), "GodotSteam API: " + method);
                foreach (string signal in new[] { "lobby_created", "lobby_joined", "join_requested" }) Require(api.HasSignal(signal), "Steam signal: " + signal);
                GD.Print("GODOT_STEAM_ABI_PASS extension=4.22.1 engine=" + Engine.GetVersionInfo()["string"]);
                GetTree().Quit(); return;
            }
            if (Argument("--width") is { } width && Argument("--height") is { } height) GetWindow().Size = new Vector2I(int.Parse(width), int.Parse(height));
            _options = RenderOptions.Parse(_args);
            if (Argument("--assets-root") is null) _options = _options with { AssetsRoot = AssetDirectory("sprites") };
            _catalog = new AssetCatalog(_options.AssetsRoot, _options.Mode == RenderMode.Sprites || _options.Hud == HudArt.Illustrated);
            _settlement = new HexProjection(65, _options.GroundCompression);
            _arena = new HexProjection(30, _options.GroundCompression);
            InitializeMusic();
            string? checkpoint = Argument("--checkpoint");
            if (Argument("--battle") is { } scenario) { _battle = new(scenario); checkpoint = "initial"; }
            if (checkpoint is not null) _session = WrapSolo(ScenarioRunner.CreateAt(checkpoint));
            _frozen = checkpoint is not null;
            CreateUi();
            CreateSessionMenu();
            if (Argument("--host") is { } hostPort) BeginHost(int.Parse(hostPort));
            else if (Argument("--join") is { } endpoint) BeginJoin(endpoint);
            else if (_session is null) ReturnToMenu(); else RefreshUi(true);
            if (_options.Showroom) { ReturnToMenu(); _sessionMenu.Visible = false; CreateShowroom(); }
            if (_battle is not null) InitializeBattle();
            QueueRedraw();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (Argument("--battle-capture") is { } battleOutput) { await CaptureBattle(battleOutput); await QuitAfterAudioDrain(); return; }
            if (_args.Contains("--smoke-test")) await SmokeIntegration();
            if (_args.Contains("--capture-settings")) OpenSettings();
            if (Argument("--capture") is { } path)
            {
                var captureWatch = System.Diagnostics.Stopwatch.StartNew();
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                string absolute = System.IO.Path.GetFullPath(path);
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(absolute)!);
                Error result = GetViewport().GetTexture().GetImage().SavePng(absolute);
                if (result != Error.Ok) throw new InvalidOperationException($"Viewport capture failed: {result}");
                if (_battle is not null) BattleCaptures.Metadata(System.IO.Path.ChangeExtension(absolute, ".json"), "godot", _battle, _options, captureWatch.Elapsed.TotalMilliseconds, GC.GetTotalAllocatedBytes(), _motion.Events.Count, CombatShader, _zoom, _pan.X, _pan.Y);
                GD.Print($"GODOT_CAPTURE {absolute} phase={_session?.Snapshot.Phase} mode={_options.Mode}");
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
        var root = new Control(); _gameUi = root; layer.AddChild(root); root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.MouseFilter = Control.MouseFilterEnum.Ignore;
        var theme = new Theme { DefaultFontSize = 16 }; root.Theme = theme;
        var header = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.AddChild(header); header.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide); header.OffsetBottom = HeaderHeight;
        SkinPanel(header);
        var top = new VBoxContainer(); header.AddChild(Margin(top, 18));
        top.AddChild(new Label { Text = "THE COMMON WATCH  /  GODOT 2D" });
        _phase = new Label(); top.AddChild(_phase);
        _resources = new Label(); top.AddChild(_resources);
        var side = new PanelContainer(); root.AddChild(side);
        side.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.RightWide);
        side.OffsetLeft = -SideWidth; side.OffsetTop = HeaderHeight + 8; side.OffsetBottom = -60; side.OffsetRight = -8;
        SkinPanel(side);
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; side.AddChild(Margin(scroll, 26));
        var column = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; scroll.AddChild(column);
        _selection = new ArtTooltipLabel
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MakeTooltip = text => { var panel = new PanelContainer(); SkinPanel(panel, "hud/tooltip"); panel.AddChild(Margin(new Label { Text = text }, 22)); return panel; }
        };
        column.AddChild(_selection);
        _production = WrapLabel(); column.AddChild(_production);
        _actions = new VBoxContainer(); column.AddChild(_actions);
        column.AddChild(new HSeparator());
        _ready = Button("Advance preparation / Ready", () => Act(() => _session.Send("ready"))); column.AddChild(_ready);
        _combatInfo = WrapLabel(); column.AddChild(_combatInfo);
        column.AddChild(Button("Reset camera [R]", ResetCamera));
        column.AddChild(Button("Return to main menu", ReturnToMenu));
        column.AddChild(WrapLabel("Select: left click • Pan: middle/right drag\nZoom: wheel • Reset: R\nOpening: Farm, Barracks, Metal Mine.\nAt zero wood, use Lumbermill recovery.\nReady, recruit two Swordsmen; Ready, recruit two more; Ready to fight.\nReady settles real resource production."));
        var footer = new PanelContainer(); root.AddChild(footer); footer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide); footer.OffsetTop = -52;
        SkinPanel(footer, "hud/tooltip");
        _message = WrapLabel("Choose a settlement plot. All actions use the shared authoritative simulation."); var footerMargin = Margin(_message, 14); footerMargin.AddThemeConstantOverride("margin_left", 24); footer.AddChild(footerMargin);
        _draft = new PanelContainer(); root.AddChild(_draft); _draft.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        _draft.AddThemeStyleboxOverride("panel", PanelStyle(new Color("14212c"), _gold));
        SkinPanel(_draft);
        _draft.OffsetLeft = -560; _draft.OffsetRight = 560; _draft.OffsetTop = -210; _draft.OffsetBottom = 210;
        var draftColumn = new VBoxContainer(); _draft.AddChild(Margin(draftColumn, 20));
        draftColumn.AddChild(new Label { Text = "WAVE COMPLETE — CHOOSE ONE REWARD", Modulate = _gold });
        _cards = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill }; draftColumn.AddChild(_cards);
        _draft.Visible = false;
        var menu = Button("Main menu", ReturnToMenu); root.AddChild(menu);
        menu.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
        menu.OffsetLeft = -260; menu.OffsetRight = -140; menu.OffsetTop = 12; menu.OffsetBottom = 48;
        column.AddChild(Button("Reconnect", ReconnectCurrent));
        CreateAudioUi(root);
    }

    private static StyleBoxFlat PanelStyle(Color background, Color border) => new()
    {
        BgColor = background,
        BorderColor = border,
        BorderWidthLeft = 2,
        BorderWidthRight = 2,
        BorderWidthTop = 2,
        BorderWidthBottom = 2,
        CornerRadiusTopLeft = 5,
        CornerRadiusTopRight = 5,
        CornerRadiusBottomLeft = 5,
        CornerRadiusBottomRight = 5
    };
    private static MarginContainer Margin(Control child, int padding)
    {
        var margin = new MarginContainer();
        foreach (string side in new[] { "left", "top", "right", "bottom" }) margin.AddThemeConstantOverride("margin_" + side, padding);
        margin.AddChild(child); return margin;
    }
    private static Label WrapLabel(string text = "") => new() { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    private Button Button(string text, Action clicked)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(0, 36) }; button.Pressed += clicked; SkinButton(button); return button;
    }
    private void Act(Action action)
    {
        action();
        _frozen = false; RefreshUi(true); QueueRedraw();
    }
    private GodotSession WrapSolo(SoloSession solo) { var session = new GodotSession { Name = "Session" }; AddChild(session); session.Solo(solo); return session; }
    private void ReplaceSession(SoloSession session) { LeaveSteam(); _session?.Dispose(); _session = WrapSolo(session); _gameUi.Visible = true; _sessionMenu.Visible = false; if (_message is not null) _message.Text = "Select a plot to inspect its authoritative actions."; _motion.Clear(); _finishingCombat = false; _uiKey = ""; ResetCamera(); }
    private void ResetCamera() { _pan = Vector2.Zero; _zoom = 1; QueueRedraw(); }

    public override void _Process(double delta)
    {
        ProcessSteam();
        RunControlProbe();
        if (_session is null || _resources is null) return;
        _animationProfile?.Begin(delta);
        if (_battle is not null) _battle.Advance(_animationProfile?.Step ?? delta);
        else if (!_frozen) _session.Advance(delta);
        if (!_session.HasState) { RefreshSessionMenu(); return; }
        ObserveCombat();
        RunNetworkProbe();
        RefreshUi(); QueueRedraw();
    }

    private void RefreshUi(bool force = false)
    {
        if (_session is null || !_session.HasState) { RefreshSessionMenu(); return; }
        if (_battle is not null) { _gameUi.Visible = false; _sessionMenu.Visible = false; UpdateBattleHud(); return; }
        _message.Text = _session.Pending ? "Waiting for authority…" : (!_session.Connected ? _session.Status : _session.LastResult?.Message ?? _session.Status);
        _sessionMenu.Visible = _session.Snapshot.Phase == Phase.Lobby;
        _gameUi.Visible = !_sessionMenu.Visible;
        RefreshSessionMenu();
        var state = _session.Snapshot; var city = _session.City;
        _phase.Text = $"Wave {state.Wave}/{state.TotalWaves}  •  {state.Phase}  •  preparation {state.Turn}  •  city HP {HealthPoints.Format(city.Health)}/{state.Rules.CityHealth}  •  {_options.Mode}";
        _resources.Text = $"Gold {city.Gold}   Wood {city.Wood}   Food {city.Food}   Stone {city.Stone}   Metal {city.Metal}   Cloth {city.Cloth}   •   Swordsmen {city.Soldiers.Count(u => u.Type == UnitType.Swordsman)}";
        _ready.Visible = state.Phase != Phase.Combat;
        _production.Visible = state.Phase != Phase.Combat; _actions.Visible = state.Phase != Phase.Combat;
        _ready.Disabled = !_session.Connected || state.Phase is not (Phase.Building or Phase.Preparation);
        _combatInfo.Text = state.Phase == Phase.Combat ? $"Automatic combat • tick {state.Tick}\nBlue: friendly  Red: enemy\nLiving friends: {city.Soldiers.Count(u => u.Health > 0)}  Enemies: {state.Enemies.Count(u => u.Health > 0)}"
            + "\n\n" + string.Join("\n", city.Soldiers.Concat(state.Enemies).Where(u => u.Health > 0)
                .Select(u => $"{(u.Faction == Faction.Adventurers ? "S" : "E")}{u.Id} {(u.Faction == Faction.Adventurers ? "" : "Enemy ")}{u.Type} HP: {HealthPoints.Format(u.Health)}/{HealthPoints.Format(u.Profile.Health)}"))
            : state.Phase is Phase.Defeat or Phase.Victory ? $"{state.Phase}. Use New solo game to restart." : "Preparing for the next wave.";
        _selection.TooltipText = _session.SelectedSlot >= 0 ? $"Selected plot {_session.SelectedSlot + 1}: inspect costs and production below." : "Choose a settlement plot to inspect its costs and production.";
        _production.Text = "Next production: " + (city.ProductionIncome is { } income ? RunPresentation.Resources(income) : "None");
        string key = $"{state.Phase}|{_finishingCombat}|{_session.SelectedSlot}|{city.Resources}|{string.Join(',', city.Slots.Select(s => $"{s.Type}:{s.Level}:{s.Purchased}"))}|{string.Join(',', city.Run.Offers)}";
        if (!force && key == _uiKey) return; _uiKey = key;
        foreach (Node child in _actions.GetChildren()) { _actions.RemoveChild(child); child.QueueFree(); }
        _buildButtons.Clear(); _recruit = null;
        int slotIndex = _session.SelectedSlot;
        if (slotIndex is >= 0 and < 9)
        {
            var slot = city.Slots[slotIndex];
            _selection.Text = $"PLOT {slotIndex + 1}\n{(slot.Purchased ? slot.Type == Building.Empty ? "Empty plot" : $"{slot.Type} — level {slot.Level}" : "Unpurchased plot")}";
            bool editable = _session.Connected && state.Phase is Phase.Building or Phase.Preparation;
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
        _draft.Visible = state.Phase == Phase.RewardDraft && !_finishingCombat;
        foreach (Node child in _cards.GetChildren()) { _cards.RemoveChild(child); child.QueueFree(); }
        _rewardButtons.Clear();
        if (_draft.Visible)
            foreach (string id in city.Run.Offers)
            {
                var card = RunCatalog.Card(id);
                var panel = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
                panel.AddThemeStyleboxOverride("panel", PanelStyle(new Color("20313e"), new Color("526a79")));
                SkinPanel(panel, "hud/card_frame");
                var content = new VBoxContainer(); panel.AddChild(Margin(content, 36));
                content.AddChild(WrapLabel(card.Name)); content.AddChild(WrapLabel(card.Rarity.ToString()));
                content.AddChild(WrapLabel(card.Effect + (card.Multiplied ? $"\nResources multiplied by {RunEffects.Multiplier(city.Run.DraftWave)}." : "")));
                var choose = Button("Choose reward", () => Act(() => _session.ChooseReward(id))); choose.Disabled = !_session.Connected; content.AddChild(choose);
                _rewardButtons.Add(choose); _cards.AddChild(panel);
            }
    }

    private Vector2 WorldOrigin => new((GetViewportRect().Size.X - SideWidth) / 2, HeaderHeight + (GetViewportRect().Size.Y - HeaderHeight - 48) / 2);
    private Vector2 ArenaCenter()
    {
        var coordinates = CombatState.Arena?.Board.Cells.Select(c => V(_arena.Project(c.Coordinate))).ToArray() ?? [];
        return coordinates.Length == 0 ? Vector2.Zero : new Vector2((coordinates.Min(v => v.X) + coordinates.Max(v => v.X)) / 2, (coordinates.Min(v => v.Y) + coordinates.Max(v => v.Y)) / 2);
    }
    private Vector2 SettlementCenter => new(0, 65 * 1.5f * 3 * _options.GroundCompression);
    private Vector2 SettlementScreen(NumVector point) => Screen(V(point) - SettlementCenter);
    private static Vector2 V(NumVector value) => new(value.X, value.Y);
    private Vector2 Screen(Vector2 world) => WorldOrigin + _pan + world * _zoom;
    private Vector2 World(Vector2 screen) => (screen - WorldOrigin - _pan) / _zoom;

    public override void _Draw()
    {
        if (_combatOverlay is not null) _combatOverlay.Visible = false;
        if (_session is null || !_session.HasState || _session.Snapshot.Phase == Phase.Lobby) return;
        if (PresentingCombat)
        {
            DrawCombat();
            if (_animationProfile is not null && _animationProfile.End(_battle!, AnimationTick, _motion.Events.Count, _figures.Count))
            {
                if (_animationProfile.Done) _ = QuitAfterAudioDrain(); else { Engine.MaxFps = _animationProfile.Fps; ObserveCombat(); }
            }
        } else { foreach (var figure in _figures.Values) figure.Visible = false; DrawSettlement(); }
    }
    private void DrawSettlement()
    {
        DrawString(ThemeDB.FallbackFont, new Vector2(22, HeaderHeight + 28), "SETTLEMENT • 9 PLOTS", HorizontalAlignment.Left, -1, 19, _muted);
        for (int i = 0; i < SettlementLayout.Coordinates.Count; i++)
        {
            var coordinate = SettlementLayout.Coordinates[i]; var slot = _session.City.Slots[i];
            var center = SettlementScreen(_settlement.Project(coordinate));
            DrawHex(_settlement.Corners(coordinate).Select(SettlementScreen).ToArray(), slot.Purchased ? new Color("304a40") : new Color("252e34"), i == _session.SelectedSlot ? _gold : new Color("62796f"), i == _session.SelectedSlot ? 4 : 2);
            Sprite("terrain/woodland_hex", center, new Vector2(116, 116) * _zoom);
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
                if (!Sprite(VisualAssets.BuildingId(slot.Type), center, Vector2.One * _catalog.Metadata(VisualAssets.BuildingId(slot.Type)).DisplaySize * _zoom))
                {
                    DrawRect(new Rect2(center - new Vector2(24, 26) * _zoom, new Vector2(48, 43) * _zoom), new Color("92b5a5"));
                    DrawLine(center + new Vector2(-28, -26) * _zoom, center + new Vector2(0, -44) * _zoom, _gold, 3);
                    DrawLine(center + new Vector2(0, -44) * _zoom, center + new Vector2(28, -26) * _zoom, _gold, 3);
                }

            }
            else
            {
                TextCentered(slot.Purchased ? "Empty" : "Buy plot", center + new Vector2(0, 6), 16);
                TextCentered($"{i + 1}", center + new Vector2(0, 26), 13);
            }
        }
        // Labels finish after every tall sprite so the next row cannot cover them.
        for (int i = 0; i < SettlementLayout.Coordinates.Count; i++)
        {
            var slot = _session.City.Slots[i];
            if (slot.Type == Building.Empty) continue;
            Vector2 center = SettlementScreen(_settlement.Project(SettlementLayout.Coordinates[i]));
            DrawRect(new Rect2(center + new Vector2(-50, 17) * _zoom, new Vector2(100, 35) * _zoom), new Color(0.04f, .07f, .08f, .8f));
            TextCentered(slot.Type.ToString(), center + new Vector2(0, 32) * _zoom, 14);
            TextCentered($"Lv {slot.Level}", center + new Vector2(0, 47) * _zoom, 11);
        }
    }
    private void DrawCombat() => DrawAnimatedCombat();
    private void DrawHex(Vector2[] corners, Color fill, Color border, float width)
    {
        DrawColoredPolygon(corners, fill); DrawPolyline(corners.Append(corners[0]).ToArray(), border, width, true);
    }
    private void TextCentered(string text, Vector2 position, int size, Color? color = null)
    {
        if (_renderingCombat && _combatOverlay is not null) { _combatOverlay.Text(text, position, size, color ?? _ink); return; }
        float width = ThemeDB.FallbackFont.GetStringSize(text, HorizontalAlignment.Left, -1, size).X;
        DrawString(ThemeDB.FallbackFont, position - new Vector2(width / 2, 0), text, HorizontalAlignment.Left, -1, size, color ?? _ink);
    }
    private bool Sprite(string id, Vector2 anchor, Vector2 size, Color? tint = null)
    {
        if (LoadArt(id) is not { } texture) { Overlay(anchor, new Rect2(anchor - new Vector2(24, 32), new Vector2(48, 40) * _zoom)); return false; }
        float factor = Math.Min(size.X / texture.GetWidth(), size.Y / texture.GetHeight());
        size = new Vector2(texture.GetWidth(), texture.GetHeight()) * factor;
        var meta = _catalog.Metadata(id);
        var rect = new Rect2(anchor - new Vector2(meta.Anchor[0], meta.Anchor[1]) * size + new Vector2(meta.Offset[0], meta.Offset[1]) * _zoom, size);
        DrawTextureRect(texture, rect, false, tint ?? Colors.White);
        Overlay(anchor, rect); return true;
    }

    public override void _Input(InputEvent input)
    {
        if (_battle is not null && input is InputEventKey { Pressed: true, Echo: false } battleKey
            && battleKey.Keycode is Key.Space or Key.Backspace or Key.Tab or Key.T or Key.O or Key.V or Key.Key1 or Key.Key2 or Key.Key3 or Key.Key4 or Key.Key5 or Key.Key6)
        { HandleBattleKey(battleKey.Keycode); GetViewport().SetInputAsHandled(); return; }
        if (input is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
        {
            if (_settingsOverlay.Visible) CloseSettings(); else OpenSettings();
            GetViewport().SetInputAsHandled();
        }
        if (input is InputEventMouseButton { Pressed: false } mouse && mouse.ButtonIndex is MouseButton.Middle or MouseButton.Right) _dragging = false;
    }
    public override void _UnhandledInput(InputEvent input)
    {
        if (_session is null || !_session.HasState || _session.Snapshot.Phase == Phase.Lobby) return;
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
        SmokeSessionLifecycle();
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
        if (_args.Contains("--expect-installed-art"))
        {
            Require(_catalog.Entries.Count == 13 && _hudTextures.Values.Count(t => t is not null) == 4 && _ninePatches > 0, "installed native NinePatchRect skins");
            Require(_options.Mode != RenderMode.Sprites || _textures.Values.Any(t => t is not null), "native world PNG decoding");
            Require(LoadArt("building/metalmine") is null, "missing building keeps geometric fallback");
            GD.Print($"GODOT_ART_PASS view={_options.View} hud={_options.Hud} native-nine-slices={_ninePatches} dynamic-text=true");
        }
        GD.Print($"SMOKE PASS Godot mode={_options.Mode} checkpoint={checkpoint}; startup, drawing, selection, construction, recruitment, ready, combat, draft, resize; sprite_decode_attempts={_spriteDecodeAttempts}");
    }
    private static void Require(bool success, string check) { if (!success) throw new InvalidOperationException("Godot integration failed: " + check); }
    public override void _ExitTree()
    {
        _hitShader.Dispose();
        _audioSettings?.Save();
        ShutdownMusic();
        _session?.Dispose();
        DisposeSteam();
        foreach (var texture in _hudTextures.Values) texture?.Dispose();
        foreach (var texture in _textures.Values) texture?.Dispose();
    }
}
