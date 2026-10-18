using Game.Core;
using Game.Shared;
using Gum.Forms.Controls;
using MonoGameGum;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;

namespace Game.MonoGameClient;

public sealed partial class WatchGame
{
    Game.Development.AnimationProfile? animationProfile;
    readonly List<Button> battleButtons = [];
    StackPanel? battlePanel;
    Gum.Forms.Controls.Label? battleInfo;
    readonly Queue<BattleCapturePoint> battleCaptures = new();
    readonly Dictionary<string, BattleShowcase> battleCache = [];
    BattleCapturePoint? capturePoint;
    int captureFrame;
    long drawStarted;
    void InitializeBattle()
    {
        battlePanel = new StackPanel { Width = SidebarWidth - 36, X = Window.ClientBounds.Width - SidebarWidth + 18, Y = 130 };
        battlePanel.AddToRoot();
        void Add(string text, Action action)
        {
            var button = new Button { Text = text, Width = SidebarWidth - 36, Height = 30 };
            button.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
            button.Click += (_, _) => { action(); button.IsFocused = false; }; battlePanel.AddChild(button); battleButtons.Add(button);
        }
        battleInfo = new Gum.Forms.Controls.Label { Width = SidebarWidth - 36, Height = 180 };
        battleInfo.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        Add("A: Swordsmen / raiders  [1]", () => SelectBattle("swordsman"));
        Add("B: Ranged / magic  [2]", () => SelectBattle("ranged"));
        Add("C: Poison  [3]", () => SelectBattle("status"));
        Add("C: Burn  [4]", () => SelectBattle("burn"));
        Add("C: Chill  [5]", () => SelectBattle("chill"));
        Add("C: Structures  [6]", () => SelectBattle("structures"));
        Add("Play / pause  [Space]", () => battle!.Paused = !battle.Paused);
        Add("Restart  [Backspace]", () => { battle!.Restart(); ObserveCombat(); });
        Add("Change speed  [Tab]", CycleBattleSpeed);
        Add("Sprites / debug  [T]", ToggleBattleArt);
        Add("Diagnostic overlay  [O]", () => options = options with { DebugOverlay = !options.DebugOverlay });
        Add("Topdown / angled  [V]", ToggleBattleView);
        battlePanel.AddChild(battleInfo);
        if (Arg("--battle-tick") is { } tick) SeekBattle(double.Parse(tick, System.Globalization.CultureInfo.InvariantCulture));
        if (Arg("--battle-zoom") is { } zoom) cameraZoom = float.Parse(zoom, System.Globalization.CultureInfo.InvariantCulture);
        cameraPan = new Vector2(float.Parse(Arg("--battle-pan-x") ?? "0", System.Globalization.CultureInfo.InvariantCulture), float.Parse(Arg("--battle-pan-y") ?? "0", System.Globalization.CultureInfo.InvariantCulture));
        if (Arg("--battle-profile") is { } profile)
        {
            animationProfile = new(profile); IsFixedTimeStep = true; TargetElapsedTime = TimeSpan.FromSeconds(animationProfile.Step);
            graphics.SynchronizeWithVerticalRetrace = false; graphics.ApplyChanges();
            battle!.Speed = animationProfile.Speed; battle.Paused = false;
        }
        if (Arg("--battle-capture") is not null)
        {
            foreach (string scenario in new[] { "swordsman", "ranged", "status", "burn", "chill", "structures" })
            {
                var recording = scenario == battle!.Scenario ? battle : new BattleShowcase(scenario);
                battleCache[scenario] = recording;
                foreach (var point in BattleCaptures.Plan(recording, !arguments.Contains("--no-sequence"))) battleCaptures.Enqueue(point);
            }
            NextBattleCapture();
        }
    }
    void SelectBattle(string scenario)
    {
        battle = battleCache.GetValueOrDefault(scenario) ?? new(scenario);
        battle.Restart(); ObserveCombat(); ResetCamera(); UpdateBattleHud();
    }
    void SeekBattle(double tick)
    {
        var entries = battle!.Seek(tick); battle.Paused = true; ObserveCombat();
        foreach (var entry in entries) motion.Observe(entry);
        motion.Trim(Playback.Tick);
    }
    void UpdateBattle(GameTime gameTime)
    {
        drawStarted = Stopwatch.GetTimestamp();
        animationProfile?.Begin(gameTime.ElapsedGameTime.TotalSeconds);
        var keys = Keyboard.GetState();
        foreach (var button in battleButtons) if (button.IsFocused) button.IsFocused = false;
        bool Press(Keys key) => keys.IsKeyDown(key) && previousKeyboard.IsKeyUp(key);
        if (Press(Keys.Space)) battle!.Paused = !battle.Paused;
        if (Press(Keys.Back)) { battle!.Restart(); ObserveCombat(); }
        if (Press(Keys.Tab)) CycleBattleSpeed();
        if (Press(Keys.T)) ToggleBattleArt();
        if (Press(Keys.O)) options = options with { DebugOverlay = !options.DebugOverlay };
        if (Press(Keys.V)) ToggleBattleView();
        if (Press(Keys.D1)) SelectBattle("swordsman");
        if (Press(Keys.D2)) SelectBattle("ranged");
        if (Press(Keys.D3)) SelectBattle("status");
        if (Press(Keys.D4)) SelectBattle("burn");
        if (Press(Keys.D5)) SelectBattle("chill");
        if (Press(Keys.D6)) SelectBattle("structures");
        battle!.Advance(animationProfile?.Step ?? gameTime.ElapsedGameTime.TotalSeconds);
    }
    void UpdateBattleHud()
    {
        sidebar.Visual.Visible = false; combatRoster.Visible = false;
        foreach (var panel in rewardPanels) panel.Visual.Visible = false;
        sessionMenu.Visual.Visible = false;
        title.Text = "THE COMMON WATCH  /  TABLETOP FIGURES"; title.X = 190; title.Width = 530;
        resources.Text = "One image per character · Default rules · Paid campaign replay";
        status.Text = $"MonoGame · {options.Mode} · {options.View} · Wave {battle!.Snapshot.Wave}";
        footer.Y = Window.ClientBounds.Height - 35;
        footer.Text = "Right drag: pan  ·  Wheel: zoom  ·  R: reset  ·  Space: pause  ·  1–6: scenario";
        if (battlePanel is not null) battlePanel.X = Window.ClientBounds.Width - SidebarWidth + 18;
        if (battleInfo is not null) battleInfo.Text = $"{(battle.Complete ? "Battle complete" : battle.Paused ? "Paused" : "Playing")} · {CombatSpeedRates.Label(battle.Speed)}\n{battle.Snapshot.Phase}\nSimulation {battle.Snapshot.Tick}\nPresentation {Playback.Tick:F2}\nEvent {Playback.EventCursor}\nActors {Playback.Units().Length} · Effects {motion.Events.Count}\nNative shader {(CombatShader ? "on" : "off")}";
    }
    void CycleBattleSpeed() => battle!.Speed = (CombatSpeed)(((int)battle.Speed + 1) % 3);
    void ToggleBattleArt()
    {
        options = options with { Mode = options.Mode == RenderMode.Debug ? RenderMode.Sprites : RenderMode.Debug };
        sprites.Configure(options);
    }
    void ToggleBattleView()
    {
        options = options with { View = options.View == ViewPreset.Topdown ? ViewPreset.Angled : ViewPreset.Topdown };
        combatProjection = new(31, options.GroundCompression); settlementProjection = new(66, options.GroundCompression);
    }
    void NextBattleCapture()
    {
        if (!battleCaptures.TryDequeue(out capturePoint)) { Console.WriteLine($"MONOGAME_ANIMATION_CAPTURE_PASS shader-uses={canvas.ShaderUses}"); Exit(); return; }
        if (battle!.Scenario != capturePoint.Scenario) SelectBattle(capturePoint.Scenario);
        SeekBattle(capturePoint.Tick); captureFrame = frames; UpdateBattleHud();
    }
    void AfterBattleDraw()
    {
        if (animationProfile is not null && animationProfile.End(battle!, AnimationTick, motion.Events.Count, Playback.Units().Length))
        {
            if (animationProfile.Done) Exit(); else { TargetElapsedTime = TimeSpan.FromSeconds(animationProfile.Step); ObserveCombat(); }
        }
        if (capturePoint is null || frames < captureFrame + 2 || Arg("--battle-capture") is not { } directory) return;
        double frameMs = Stopwatch.GetElapsedTime(drawStarted).TotalMilliseconds;
        string path = Path.GetFullPath(Path.Combine(directory, capturePoint.Name + ".png"));
        SaveCapture(path);
        BattleCaptures.Metadata(Path.ChangeExtension(path, ".json"), "monogame", battle!, options, frameMs, GC.GetTotalAllocatedBytes(), motion.Events.Count, CombatShader, cameraZoom, cameraPan.X, cameraPan.Y);
        Console.WriteLine($"BATTLE_CAPTURE {capturePoint.Name} tick={Playback.Tick:F2} event={Playback.EventCursor}");
        NextBattleCapture();
    }
}
