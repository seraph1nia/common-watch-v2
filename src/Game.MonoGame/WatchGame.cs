using Game.Core;
using Game.Shared;
using Gum.Forms;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using NumVector = System.Numerics.Vector2;
using Label = Gum.Forms.Controls.Label;

namespace Game.MonoGameClient;

public sealed partial class WatchGame : Microsoft.Xna.Framework.Game
{
    readonly GraphicsDeviceManager graphics;
    readonly RenderOptions options;
    readonly string? capturePath;
    readonly bool smoke;
    readonly string audioRoot, settingsPath;
    readonly bool captureSettings;
    readonly string[] arguments;
    AudioSettings audioSettings = null!;
    LoopingMusic music = null!;
    MusicSettingsMenu settingsMenu = null!;
    KeyboardState previousKeyboard;
    bool checkpointFrozen;
    MonoSession session = null!;
    ShapeCanvas canvas = null!;
    OptionalSprites sprites = null!;
    readonly HexProjection settlementProjection;
    readonly HexProjection combatProjection;
    readonly List<TextRuntime> worldLabels = [];
    readonly List<(Button Button, Action Action)> actions = [];
    readonly List<StackPanel> rewardPanels = [];
    readonly List<Label> rewardNames = [], rewardDescriptions = [];
    readonly List<Button> rewardButtons = [];
    StackPanel sidebar = null!;
    Label selectedText = null!, hintText = null!;
    TextRuntime title = null!, resources = null!, status = null!, footer = null!, combatRoster = null!;
    Button ready = null!;
    MouseState previousMouse;
    Vector2 cameraPan;
    float cameraZoom = 1;
    int labelCount, frames;
    Phase lastPhase;
    readonly List<(Vector2 Position, string Text, double Until)> effects = [];
    double elapsed;
    bool smokeCompleted;
    const int SidebarWidth = 340;
    GumService GumUI => GumService.Default;

    public WatchGame(string[] args)
    {
        arguments = args;
        options = RenderOptions.Parse(args);
        settlementProjection = new(66, options.GroundCompression);
        combatProjection = new(31, options.GroundCompression);
        string? Arg(string name)
        {
            int index = Array.IndexOf(args, name);
            if (index < 0) return null;
            if (index + 1 == args.Length) throw new ArgumentException(name + " requires a value.");
            return args[index + 1];
        }
        capturePath = Arg("--capture");
        smoke = args.Contains("--smoke-test");
        captureSettings = args.Contains("--capture-settings");
        audioRoot = Arg("--audio-root") ?? Path.Combine(AppContext.BaseDirectory, "assets", "audio");
        settingsPath = Arg("--settings-path") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "common-watch", "settings.json");
        string? checkpoint = Arg("--checkpoint");
        checkpointFrozen = checkpoint is not null;
        if (checkpoint is not null) session = new MonoSession(ScenarioRunner.CreateAt(checkpoint));
        graphics = new(this)
        {
            PreferredBackBufferWidth = int.Parse(Arg("--width") ?? "1280"),
            PreferredBackBufferHeight = int.Parse(Arg("--height") ?? "720"),
            SynchronizeWithVerticalRetrace = !smoke,
            GraphicsProfile = GraphicsProfile.Reach,
            PreferredDepthStencilFormat = DepthFormat.None,
        };
        Window.Title = "The Common Watch - MonoGame / Gum";
        Window.AllowUserResizing = true;
        IsMouseVisible = true;
        IsFixedTimeStep = false;
        Content.RootDirectory = "Content";
    }

    protected override void Initialize()
    {
        GumUI.Initialize(this, DefaultVisualsVersion.V3);
        canvas = new(GraphicsDevice);
        sprites = new(GraphicsDevice, options);
        CreateHudArt();
        // Text uses Gum's packaged technical font, without a content build or game art.
        for (int i = 0; i < 180; i++) worldLabels.Add(NewText(0, 0, 200));
        title = NewText(24, 14, 800); title.FontScale = 1.15f;
        resources = NewText(24, 46, 1100);
        status = NewText(24, 78, 900);
        footer = NewText(24, 0, 1100); footer.FontScale = .8f;
        combatRoster = NewText(0, 145, 310); combatRoster.FontScale = .8f; combatRoster.Height = 480;
        sidebar = new StackPanel { Width = SidebarWidth - (artPanels[1] is not null ? 56 : 24), X = Window.ClientBounds.Width - SidebarWidth + 12, Y = 125 };
        sidebar.AddToRoot();
        selectedText = new Label { Width = sidebar.Width, Height = 80 };
        selectedText.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        selectedText.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        sidebar.AddChild(selectedText);
        AddButton("Buy selected plot", () => Submit("buy-plot"));
        foreach (Building building in new[] { Building.Lumbermill, Building.Farm, Building.Barracks, Building.MetalMine, Building.Mine })
        {
            Building choice = building;
            AddButton("Build " + building, () => Submit("build", choice));
        }
        AddButton("Lumbermill recovery (4 Gold)", () => Submit("build", Building.Lumbermill, ConstructionPayment.GoldRecovery));
        AddButton("Recruit Swordsman", () => Submit("recruit"));
        AddButton("Upgrade selected building", () => Submit("upgrade"));
        ready = AddButton("Advance preparation / ready", () => Submit("ready", selected: false));
        AddButton("Reset camera [R]", ResetCamera);
        AddButton("Return to main menu", ReturnToMenu);
        hintText = new Label { Width = sidebar.Width, Height = 80 };
        hintText.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        hintText.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        sidebar.AddChild(hintText);
        for (int i = 0; i < 3; i++)
        {
            int choice = i;
            var panel = new StackPanel { Width = 275, Height = 380, Y = 240 };
            panel.AddToRoot();
            var name = new Label { Width = 235, Height = 70, X = 18 };
            var description = new Label { Width = 235, Height = 180, X = 18 };
            var choose = new Button { Text = "Choose reward", Width = 235, Height = 42, X = 18 };
            name.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
            description.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
            name.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
            description.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
            choose.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
            panel.AddChild(name); panel.AddChild(description); panel.AddChild(choose);
            choose.Click += (_, _) => ChooseReward(choice);
            SkinButton(choose);
            rewardPanels.Add(panel); rewardNames.Add(name); rewardDescriptions.Add(description); rewardButtons.Add(choose);
        }
        audioSettings = AudioSettings.Load(settingsPath);
        music = new LoopingMusic(audioRoot, audioSettings.MusicVolume);
        settingsMenu = new MusicSettingsMenu(audioSettings, music);
        SkinButton(settingsMenu.OpenButton);
        if (captureSettings) settingsMenu.SetOpen(true);
        var menuButton = new Button { Text = "Main menu", Width = 150, Height = 32, X = 740, Y = 15 };
        menuButton.AddToRoot(); SkinButton(menuButton); menuButton.Click += (_, _) => ReturnToMenu();
        var reconnectButton = new Button { Text = "Reconnect", Width = 175, Height = 32, X = 740, Y = 52 };
        reconnectButton.AddToRoot(); SkinButton(reconnectButton); reconnectButton.Click += (_, _) => ReconnectCurrent();
        CreateSessionMenu();
        if (Arg("--host") is { } hostPort) BeginHost(int.Parse(hostPort));
        else if (Arg("--join") is { } endpoint) BeginJoin(endpoint);
        lastPhase = session is { HasState: true } ? session.Snapshot.Phase : Phase.Lobby;
        UpdateUi();
        if (options.Showroom) { ReturnToMenu(); sessionMenu.Visual.Visible = false; menuButton.Visual.Visible = false; reconnectButton.Visual.Visible = false; settingsMenu.OpenButton.Visual.Visible = false; }
        base.Initialize();
        Console.WriteLine($"MonoGame initialized; rendering={options.Mode}; checkpoint={(session is { HasState: true } ? session.Snapshot.Phase : null)}; {Window.ClientBounds.Width}x{Window.ClientBounds.Height}");
    }

    TextRuntime NewText(float x, float y, float width)
    {
        var text = new TextRuntime { X = x, Y = y, Width = width, Height = 50, FontScale = .85f, Text = "" };
        text.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        text.AddToRoot();
        return text;
    }
    Button AddButton(string text, Action action)
    {
        var button = new Button { Text = text, Width = sidebar.Width, Height = 30 };
        button.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        SkinButton(button);
        button.Click += (_, _) => action();
        sidebar.AddChild(button);
        actions.Add((button, action));
        return button;
    }
    void Submit(string action, Building building = Building.Empty, ConstructionPayment payment = ConstructionPayment.Standard, bool selected = true)
    {
        checkpointFrozen = false;
        session.Send(action, selected ? session.SelectedSlot : -1, building, payment: payment);
        UpdateUi();
    }
    void ChooseReward(int index)
    {
        checkpointFrozen = false;
        if (session.City.Run.Offers.ElementAtOrDefault(index) is { } id) session.ChooseReward(id);
        UpdateUi();
    }
    void ReplaceSession(SoloSession next) { checkpointFrozen = false; session?.Dispose(); steamLobbies?.Leave(); session = new MonoSession(next); effects.Clear(); motion.Clear(); ResetCamera(); UpdateUi(); }
    void ResetCamera() { cameraPan = Vector2.Zero; cameraZoom = 1; }

    protected override void Update(GameTime gameTime)
    {
        frames++;
        elapsed += gameTime.ElapsedGameTime.TotalSeconds;
        if (options.Showroom) { GumUI.Update(gameTime); base.Update(gameTime); return; }
        ProcessSteam();
        GumUI.Update(gameTime);
        RunControlProbe();
        if (!checkpointFrozen && !smoke) session?.Advance(gameTime.ElapsedGameTime.TotalSeconds);
        MouseState mouse = Mouse.GetState();
        bool overWorld = session is { HasState: true } && session.Snapshot.Phase != Phase.Lobby && !settingsMenu.IsOpen && mouse.X < Window.ClientBounds.Width - SidebarWidth && mouse.Y > 112 && mouse.Y < Window.ClientBounds.Height - 42;
        if (overWorld)
        {
            if (mouse.RightButton == ButtonState.Pressed && previousMouse.RightButton == ButtonState.Pressed)
                cameraPan += new Vector2(mouse.X - previousMouse.X, mouse.Y - previousMouse.Y);
            if (mouse.ScrollWheelValue != previousMouse.ScrollWheelValue)
                cameraZoom = Math.Clamp(cameraZoom * MathF.Pow(1.12f, (mouse.ScrollWheelValue - previousMouse.ScrollWheelValue) / 120f), .5f, 2f);
            if (mouse.LeftButton == ButtonState.Pressed && previousMouse.LeftButton == ButtonState.Released && session!.Snapshot.Phase != Phase.Combat && session.Snapshot.Phase != Phase.RewardDraft)
                SelectAt(new(mouse.X, mouse.Y));
        }
        KeyboardState keys = Keyboard.GetState();
        if (keys.IsKeyDown(Keys.Escape) && previousKeyboard.IsKeyUp(Keys.Escape))
        { if (settingsMenu.IsOpen) settingsMenu.SetOpen(false); else ReturnToMenu(); }
        if (keys.IsKeyDown(Keys.F10) && previousKeyboard.IsKeyUp(Keys.F10)) settingsMenu.SetOpen(!settingsMenu.IsOpen);
        previousKeyboard = keys;
        if (!settingsMenu.IsOpen)
        {
            if (keys.IsKeyDown(Keys.R)) ResetCamera();
            float move = (float)gameTime.ElapsedGameTime.TotalSeconds * 250;
            if (keys.IsKeyDown(Keys.Left)) cameraPan.X += move;
            if (keys.IsKeyDown(Keys.Right)) cameraPan.X -= move;
            if (keys.IsKeyDown(Keys.Up)) cameraPan.Y += move;
            if (keys.IsKeyDown(Keys.Down)) cameraPan.Y -= move;
        }
        previousMouse = mouse;
        if (session is null || !session.HasState)
        {
            UpdateUi();
            if (smoke && frames == 4) RunSmoke();
            base.Update(gameTime); return;
        }
        if (lastPhase != session.Snapshot.Phase) { ResetCamera(); lastPhase = session.Snapshot.Phase; }
        foreach (CombatEvent entry in session.Playback.Drain())
        {
            motion.Observe(entry, elapsed);
            if (entry.Unit is { } unit && entry.Type is CombatEventType.Hit or CombatEventType.Death)
                effects.Add((CombatVisuals.Position(unit, session.Snapshot, session.Playback.Tick, combatProjection.Radius).ToXna(), entry.Type == CombatEventType.Death ? "fallen" : "-" + HealthPoints.Format(entry.Damage), elapsed + .9));
        }
        effects.RemoveAll(effect => effect.Until < elapsed);
        RunNetworkProbe();
        UpdateUi();
        if (smoke && frames == 4) RunSmoke();
        if (smokeCompleted && frames > 12) Exit();
        base.Update(gameTime);
    }

    void SelectAt(Vector2 screen)
    {
        NumVector world = ((screen - Origin()) / cameraZoom + Center()).ToNumerics();
        HexCoordinate? coordinate = settlementProjection.HitTest(world, SettlementLayout.Coordinates);
        if (coordinate is not null) session.SelectSlot(SettlementLayout.Coordinates.ToList().IndexOf(coordinate.Value));
    }
    Vector2 Center()
    {
        IEnumerable<NumVector> points = session.Snapshot.Phase == Phase.Combat && session.Snapshot.Arena is { } arena
            ? arena.Board.Cells.Select(cell => combatProjection.Project(cell.Coordinate))
            : SettlementLayout.Coordinates.Select(settlementProjection.Project);
        NumVector[] array = points.ToArray();
        return new(array.Average(p => p.X), array.Average(p => p.Y));
    }
    Vector2 Origin() => new Vector2((Window.ClientBounds.Width - SidebarWidth) / 2f, 130 + (Window.ClientBounds.Height - 180) / 2f) + cameraPan;
    Vector2 Screen(NumVector world) => (world.ToXna() - Center()) * cameraZoom + Origin();
    void WorldLabel(string text, Vector2 position, float width = 180, float scale = .8f)
    {
        if (labelCount >= worldLabels.Count) return;
        TextRuntime label = worldLabels[labelCount++];
        label.Visible = true; label.Text = text; label.X = position.X - width / 2; label.Y = position.Y; label.Width = width; label.FontScale = scale;
        label.HorizontalAlignment = RenderingLibrary.Graphics.HorizontalAlignment.Center;
    }

    void UpdateUi()
    {
        if (sidebar is null) return;
        if (RefreshSessionMenu()) return;
        MatchSnapshot snapshot = session.Snapshot; CityState city = session.City;
        int width = Window.ClientBounds.Width, height = Window.ClientBounds.Height;
        title.Text = "THE COMMON WATCH  /  MonoGame + Gum";
        resources.Text = $"Gold {city.Gold}    Wood {city.Wood}    Food {city.Food}    Stone {city.Stone}    Metal {city.Metal}    Cloth {city.Cloth}    Keep HP {HealthPoints.Format(city.Health)}";
        status.Text = $"Wave {snapshot.Wave}   |   {snapshot.Phase}   |   Preparation {snapshot.Turn}   |   Soldiers {city.Soldiers.Length}   |   {options.Mode}   |   {(music.Loaded ? "Music on" : "Music unavailable - see Settings")}";
        footer.Y = height - 35;
        footer.Text = $"{(!session.Connected ? session.Status : session.Pending ? "Waiting for authority…" : session.LastResult?.Message) ?? "Select a settlement hex. Build an economy, recruit, then ready for battle."}    /    Pan: right drag or arrows  |  Zoom: wheel  |  Reset: R";
        sidebar.X = width - SidebarWidth + (artPanels[1] is not null ? 28 : 12);
        sidebar.Y = artPanels[1] is not null ? 140 : 125;
        sidebar.IsEnabled = !settingsMenu.IsOpen && session.Connected;
        sidebar.Visual.Visible = !settingsMenu.IsOpen && snapshot.Phase is not (Phase.RewardDraft or Phase.Combat);
        combatRoster.Visible = !settingsMenu.IsOpen && snapshot.Phase == Phase.Combat;
        combatRoster.X = width - SidebarWidth + (artPanels[1] is not null ? 28 : 15);
        combatRoster.Width = artPanels[1] is not null ? SidebarWidth - 56 : 310;
        combatRoster.Text = "AUTOMATIC COMBAT\n\n" + string.Join("\n\n", session.Playback.Units().Where(unit => unit.Hex?.Lifecycle is UnitLifecycle.Alive or UnitLifecycle.Dying).Select(unit => $"{(unit.Faction == Faction.Adventurers ? "S" : "E")}{unit.Id} {unit.Type}\nHP: {HealthPoints.Format(unit.Health)}/{HealthPoints.Format(unit.Profile.Health)}"));
        SlotState? slot = city.Slots.ElementAtOrDefault(session.SelectedSlot);
        string selected = slot is null ? "Select a settlement hex" : $"Plot {session.SelectedSlot + 1}: {(slot.Purchased ? slot.Type == Building.Empty ? "Empty" : slot.Type.ToString() : "Unpurchased")}\nLevel {slot.Level}";
        BuildingDefinition? building = snapshot.BuildingCatalog.FirstOrDefault(definition => definition.Type == slot?.Type);
        if (building is not null && building.Produces is { } produced) selected += $"\nProduces {building.Output(slot!.Level)} {produced}";
        if (slot?.Type == Building.Barracks && city.RecruitmentQuotes.FirstOrDefault(quote => quote.Type == UnitType.Swordsman && quote.Level == slot.Level) is { } recruit)
            selected += "\nRecruit: " + RunPresentation.Resources(recruit.Cost);
        selectedText.Text = selected;
        hintText.Text = "Next production:\n" + (city.ProductionIncome is { } production ? RunPresentation.Resources(production) : "No production") + "\nUnavailable actions explain why below.";
        settingsMenu.Layout(width, height);
        ready.Text = snapshot.Phase == Phase.Combat ? "Combat runs automatically" : "Advance preparation / ready";
        ready.IsEnabled = snapshot.Phase is Phase.Building or Phase.Preparation;
        for (int i = 0; i < rewardPanels.Count; i++)
        {
            rewardButtons[i].IsEnabled = !settingsMenu.IsOpen && session.Connected;
            rewardPanels[i].Visual.Visible = !settingsMenu.IsOpen && snapshot.Phase == Phase.RewardDraft;
            rewardPanels[i].X = (width - 920) / 2f + i * 310 + 12;
            rewardPanels[i].Y = Math.Max(170, (height - 390) / 2f) + 40;
            if (city.Run.Offers.ElementAtOrDefault(i) is { } id)
            {
                RewardCard card = RunCatalog.Card(id);
                rewardNames[i].Text = card.Name + "\n" + card.Rarity;
                rewardDescriptions[i].Text = card.Effect + (card.Multiplied ? $"\n\nResource multiplier x{RunEffects.Multiplier(city.Run.DraftWave)}" : "");
            }
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(16, 24, 32));
        labelCount = 0;
        canvas.Begin();
        if (options.Showroom) DrawShowroom();
        else
        {
            if (!settingsMenu.IsOpen && session is { HasState: true } && session.Snapshot.Phase != Phase.Lobby)
            {
                if (session.Snapshot.Phase == Phase.Combat) DrawCombat(); else if (session.Snapshot.Phase != Phase.RewardDraft) DrawSettlement();
            }
            canvas.Rect(0, 0, Window.ClientBounds.Width, 111, new(23, 35, 47));
            canvas.Rect(Window.ClientBounds.Width - SidebarWidth, 112, SidebarWidth, Window.ClientBounds.Height - 112, new(25, 36, 49));
            canvas.Rect(0, Window.ClientBounds.Height - 42, Window.ClientBounds.Width, 42, new(12, 18, 25));
            if (!settingsMenu.IsOpen && session is { HasState: true } && session.Snapshot.Phase == Phase.RewardDraft)
            {
                canvas.Rect(0, 112, Window.ClientBounds.Width, Window.ClientBounds.Height - 154, new(15, 22, 34));
                for (int i = 0; i < 3; i++) if (artPanels[i + 2] is null) canvas.Rect(rewardPanels[i].X - 12, rewardPanels[i].Y - 15, 290, 340, new(42, 54, 72));
                WorldLabel("WAVE CLEARED - CHOOSE ONE REWARD", new(Window.ClientBounds.Width / 2f, 130), 650, 1.1f);
            }
            settingsMenu.Draw(canvas, Window.ClientBounds.Width, Window.ClientBounds.Height);
        }
        canvas.End();
        for (int i = labelCount; i < worldLabels.Count; i++) worldLabels[i].Visible = false;
        if (!options.Showroom) UpdateHudArt();
        GumUI.Draw();
        if (!probeCaptured && Arg("--network-test") is { } count && Arg("--lobby-capture") is { } lobbyCapture
            && session is { HasState: true } && session.Snapshot.Phase == Phase.Lobby && session.Snapshot.Players.Length == int.Parse(count))
        { probeCaptured = true; SaveCapture(lobbyCapture); }
        if (capturePath is not null && frames >= 8)
        {
            SaveCapture(capturePath);
            Exit();
        }
        base.Draw(gameTime);
    }

    void DrawSettlement()
    {
        foreach ((HexCoordinate coordinate, int index) in SettlementLayout.Coordinates.Select((coordinate, index) => (coordinate, index)))
        {
            SlotState slot = session.City.Slots[index];
            Vector2 point = Screen(settlementProjection.Project(coordinate));
            bool selected = index == session.SelectedSlot;
            canvas.Polygon(settlementProjection.Corners(coordinate).Select(Screen).ToArray(), slot.Purchased ? new(53, 91, 73) : new(43, 51, 50), selected ? new(250, 210, 92) : new(107, 140, 118), selected ? 4 : 2);
            WorldSprite("terrain/woodland_hex", point, 116 * cameraZoom);
        }
        // Terrain artwork cannot obscure logical borders or the selection outline.
        foreach ((HexCoordinate coordinate, int index) in SettlementLayout.Coordinates.Select((coordinate, index) => (coordinate, index)))
        {
            Vector2[] corners = settlementProjection.Corners(coordinate).Select(Screen).ToArray();
            for (int edge = 0; edge < corners.Length; edge++)
                canvas.Line(corners[edge], corners[(edge + 1) % corners.Length], index == session.SelectedSlot ? new(250, 210, 92) : new(107, 140, 118), index == session.SelectedSlot ? 4 : 2);
        }
        // Terrain finishes before any tall visual; anchors sort consistently by depth.
        foreach ((HexCoordinate coordinate, int index) in SettlementLayout.Coordinates.Select((coordinate, index) => (coordinate, index)).OrderBy(plot => settlementProjection.Project(plot.coordinate).Y))
        {
            SlotState slot = session.City.Slots[index];
            Vector2 point = Screen(settlementProjection.Project(coordinate));
            if (slot.Type != Building.Empty)
            {
                if (!WorldSprite(VisualAssets.BuildingId(slot.Type), point, sprites.Catalog.Metadata(VisualAssets.BuildingId(slot.Type)).DisplaySize * cameraZoom))
                    canvas.Rect(point.X - 24 * cameraZoom, point.Y - 27 * cameraZoom, 48 * cameraZoom, 33 * cameraZoom, new(32, 51, 51));
            }
            WorldLabel(slot.Purchased ? slot.Type == Building.Empty ? "Empty plot" : slot.Type.ToString() : "Unpurchased", point + new Vector2(0, 8 * cameraZoom), 150 * cameraZoom, .8f * cameraZoom);
            WorldLabel($"#{index + 1}" + (slot.Type != Building.Empty ? "  L" + slot.Level : ""), point + new Vector2(0, -38 * cameraZoom), 120, .7f * cameraZoom);
        }
        WorldLabel("SETTLEMENT - NINE PLOTS", new((Window.ClientBounds.Width - SidebarWidth) / 2f, 138), 500, .9f);
    }

    void DrawCombat()
    {
        MatchSnapshot snapshot = session.Snapshot;
        if (snapshot.Arena is not { } arena) return;
        foreach (HexCellDefinition cell in arena.Board.Cells)
        {
            canvas.Polygon(combatProjection.Corners(cell.Coordinate).Select(Screen).ToArray(), new(36, 56, 54), new(63, 83, 76), 1);
            WorldSprite("terrain/woodland_hex", Screen(combatProjection.Project(cell.Coordinate)), 54 * cameraZoom);
        }
        foreach (HexCellDefinition cell in arena.Board.Cells)
        {
            Vector2[] corners = combatProjection.Corners(cell.Coordinate).Select(Screen).ToArray();
            for (int edge = 0; edge < corners.Length; edge++)
                canvas.Line(corners[edge], corners[(edge + 1) % corners.Length], new(63, 83, 76), 1);
        }
        foreach (StructureState structure in session.Playback.Structures().Where(structure => structure.Active))
        {
            HexCellDefinition? cell = arena.Board.Cells.FirstOrDefault(cell => cell.Id == structure.Cell);
            if (cell is null) continue;
            Vector2 position = Screen(combatProjection.Project(cell.Coordinate));
            canvas.Rect(position.X - 21, position.Y - 18, 42, 35, new(105, 108, 89));
            WorldLabel(structure.Kind + "\n" + HealthPoints.Format(structure.Health) + " HP", position - new Vector2(0, 13), 115, .6f);
        }
        UnitState[] units = session.Playback.Units();
        Vector2 Ground(UnitState unit) { NumVector p = CombatVisuals.Position(unit, snapshot, session.Playback.Tick, combatProjection.Radius); p.Y *= options.GroundCompression; return Screen(p); }
        foreach (UnitState unit in units.OrderBy(unit => CombatVisuals.Position(unit, snapshot, session.Playback.Tick).Y))
        {
            if (unit.Hex?.Lifecycle is UnitLifecycle.Stored or UnitLifecycle.Reserve or UnitLifecycle.Queued) continue;
            Vector2 ground = Ground(unit);
            UnitState? targetUnit = units.FirstOrDefault(u => u.Id == unit.TargetId);
            var pose = motion.Sample(unit, session.Playback.Tick, elapsed, ground.ToNumerics(), targetUnit is null ? null : Ground(targetUnit).ToNumerics());
            Vector2 position = ground + pose.Offset.ToXna() * cameraZoom;
            Color color = unit.Faction == Faction.Adventurers ? new(74, 151, 233) : new(226, 89, 89);
            if (unit.TargetId > 0 && units.FirstOrDefault(target => target.Id == unit.TargetId) is { } target && unit.PendingImpact)
                canvas.Line(position, Screen(CombatVisuals.Position(target, snapshot, session.Playback.Tick, combatProjection.Radius)), new(245, 201, 91), 2);
            Color tint = Color.Lerp(Color.White, new Color(255, 80, 80), pose.Flash) * pose.Opacity;
            if (!WorldSprite(VisualAssets.UnitId(unit), position, sprites.Catalog.Metadata(VisualAssets.UnitId(unit)).DisplaySize * cameraZoom, tint)) canvas.Circle(position, 7 * cameraZoom, color * pose.Opacity);
            float ratio = Math.Clamp((float)unit.Health / Math.Max(1, unit.Profile.Health), 0, 1);
            canvas.Rect(position.X - 7, position.Y - 46 * cameraZoom, 14, 3, new(18, 24, 29));
            canvas.Rect(position.X - 7, position.Y - 46 * cameraZoom, 14 * ratio, 3, color);
            WorldLabel((unit.Faction == Faction.Adventurers ? "S" : "E") + unit.Id, position + new Vector2(0, 10), 60, .5f);
        }
        foreach (var effect in effects) WorldLabel(effect.Text, Screen(effect.Position.ToNumerics()) - new Vector2(0, 28), 90, .8f);
        WorldLabel("SHARED COMBAT ARENA  /  BLUE: SWORDSMEN  /  RED: ENEMIES", new((Window.ClientBounds.Width - SidebarWidth) / 2f, 118), 720, .78f);
    }

    void SaveCapture(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        int width = GraphicsDevice.PresentationParameters.BackBufferWidth, height = GraphicsDevice.PresentationParameters.BackBufferHeight;
        var pixels = new Color[width * height];
        GraphicsDevice.GetBackBufferData(pixels);
        using var texture = new Texture2D(GraphicsDevice, width, height);
        texture.SetData(pixels);
        using var file = File.Create(path);
        texture.SaveAsPng(file, width, height);
        Console.WriteLine($"CAPTURE {Path.GetFullPath(path)} {width}x{height} phase={session?.Snapshot.Phase}");
    }

    void RunSmoke()
    {
        void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException("SMOKE FAILED: " + message); }
        SmokeSessionLifecycle();
        ReplaceSession(ScenarioRunner.CreateAt("initial"));
        long settingsRevision = session.Snapshot.Revision, settingsTick = session.Snapshot.Tick;
        settingsMenu.OpenButton.PerformClick();
        Check(settingsMenu.IsOpen, "Gum settings open callback");
        UpdateUi();
        Check(!sidebar.Visual.Visible && !sidebar.IsEnabled && rewardPanels.All(panel => !panel.Visual.Visible) && rewardButtons.All(button => !button.IsEnabled), "settings block underlying UI");
        Check(music.Loaded && music.Playing && music.QueuedBuffers >= 2, "native audio decoded and playing");
        var boundary = LoopingMusic.SegmentAt(MusicTrack.LoopEnd - 8);
        Check(boundary.Frames == 9 && boundary.Next == MusicTrack.LoopStart, "exact authored loop boundary");
        int previousVolumeChanges = settingsMenu.VolumeChanges;
        foreach (int percent in new[] { 0, 50, 100 })
        {
            settingsMenu.VolumeSlider.Value = percent;
            Check(Math.Abs(audioSettings.MusicVolume - percent / 100f) < .001f, "slider settings callback");
            Check(Math.Abs(music.Volume - percent / 100f) < .001f && music.Playing, "live native gain and mute");
            Check(Math.Abs(AudioSettings.Load(settingsPath).MusicVolume - percent / 100f) < .001f, "persistent volume");
        }
        Check(settingsMenu.VolumeChanges >= previousVolumeChanges + 3, "Gum Slider.ValueChanged callbacks");
        settingsMenu.VolumeSlider.Value = 50;
        settingsMenu.CloseButton.PerformClick();
        Check(!settingsMenu.IsOpen && session.Snapshot.Revision == settingsRevision && session.Snapshot.Tick == settingsTick, "settings leave authority unchanged");
        LoopingMusic originalMusic = music;
        ReplaceSession(ScenarioRunner.CreateAt("initial"));
        Check(ReferenceEquals(originalMusic, music) && music.Playing, "one music player survives new game");
        Console.WriteLine("MONOGAME_AUDIO_PASS loaded=true playing=true authored-loop=true settings=0/50/100 persisted=true same-player=true");
        void Click(string text) => actions.Single(action => action.Button.Text == text).Button.PerformClick();
        void Select(int slot) { SelectAt(Screen(settlementProjection.Project(SettlementLayout.Coordinates[slot]))); Check(session.SelectedSlot == slot, "hex selection"); }
        Select(0); Click("Build Farm"); Check(session.City.Slots[0].Type == Building.Farm, "construction callback");
        Select(1); Click("Build Barracks"); Select(2); Click("Build MetalMine");
        Select(3); Click("Lumbermill recovery (4 Gold)"); Check(session.LastResult?.Accepted == true, "authoritative recovery construction");
        Select(1);
        for (int i = 0; i < 2; i++) { Click("Advance preparation / ready"); Click("Recruit Swordsman"); Click("Recruit Swordsman"); }
        Check(session.City.Soldiers.Length == 4, "recruitment callback");
        Click("Advance preparation / ready"); Check(session.Snapshot.Phase == Phase.Combat, "ready callback");
        for (int i = 0; i < 100 && session.Snapshot.Phase == Phase.Combat; i++) session.Step();
        Check(session.Playback.Units().Length > 0 && session.Snapshot.Tick > 0, "combat playback");
        for (int i = 0; i < 10000 && session.Snapshot.Phase == Phase.Combat; i++) session.Step();
        Check(session.Snapshot.Phase == Phase.RewardDraft && session.City.Run.Offers.Length == 3, "three reward cards");
        UpdateUi(); Check(rewardPanels.All(panel => panel.Visual.Visible), "Gum draft visibility");
        rewardButtons[0].PerformClick(); Check(session.Snapshot.Phase == Phase.Building && session.Snapshot.Wave == 2, "reward callback");
        Submit("build", Building.Farm); Check(session.LastResult?.Accepted == false && session.LastResult.Message.Length > 0, "authority rejection feedback");
        graphics.PreferredBackBufferWidth = 1920; graphics.PreferredBackBufferHeight = 1080; graphics.ApplyChanges(); UpdateUi();
        Check(Window.ClientBounds.Width == 1920 && sidebar.X + sidebar.Width <= 1920, "1920x1080 resize");
        graphics.PreferredBackBufferWidth = 1280; graphics.PreferredBackBufferHeight = 720; graphics.ApplyChanges(); UpdateUi();
        Check(sidebar.X + sidebar.Width <= 1280, "1280x720 resize");
        sprites.Get("building/lumbermill"); sprites.Get("building/farm"); sprites.Get("unit/swordsman");
        if (options.Mode == RenderMode.Debug) Check(sprites.LoadAttempts == 0, "debug ignores sprite files");
        if (arguments.Contains("--expect-installed-art"))
        {
            Check(sprites.Catalog.Entries.Count == 13 && nineSliceCount >= 6 && artPanels.All(p => p is not null), "installed native Gum NineSliceRuntime skins");
            Check(options.Mode != RenderMode.Sprites || sprites.Get("building/farm") is not null && sprites.Get("unit/swordsman") is not null, "native world PNG decoding");
            Check(sprites.Get("building/metalmine") is null, "missing building keeps geometric fallback");
            Console.WriteLine($"MONOGAME_ART_PASS view={options.View} hud={options.Hud} native-nine-slices={nineSliceCount} dynamic-text=true");
        }
        Console.WriteLine($"SMOKE PASS MonoGame: startup, Gum UI, selection, build, recruit, ready, combat, draft, reward, rejection, resize, mode={options.Mode}, sprite-load-attempts={sprites.LoadAttempts}");
        smokeCompleted = true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { session?.Dispose(); steamLobbies?.Dispose(); music?.Dispose(); sprites?.Dispose(); canvas?.Dispose(); }
        base.Dispose(disposing);
    }
}

internal static class VectorConversions
{
    public static Vector2 ToXna(this NumVector vector) => new(vector.X, vector.Y);
    public static NumVector ToNumerics(this Vector2 vector) => new(vector.X, vector.Y);
}
