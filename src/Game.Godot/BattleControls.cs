using Game.Core;
using Game.Shared;
using Godot;
using System.Diagnostics;

namespace Game.GodotClient;

public partial class Main
{
    Label? _battleInfo;
    CanvasLayer? _battleLayer;
    OptionButton? _battleScenarios;
    Game.Development.AnimationProfile? _animationProfile;
    void InitializeBattle()
    {
        var layer = new CanvasLayer(); _battleLayer = layer; AddChild(layer);
        var panel = new PanelContainer(); layer.AddChild(panel);
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.RightWide);
        panel.OffsetLeft = -SideWidth + 8; panel.OffsetRight = -8; panel.OffsetTop = 112; panel.OffsetBottom = -12;
        var column = new VBoxContainer();
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; panel.AddChild(scroll); scroll.AddChild(Margin(column, 20));
        column.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        column.AddChild(new Label { Text = "ANIMATED TABLETOP FIGURES", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        var scenarios = new OptionButton(); _battleScenarios = scenarios;
        foreach (var text in new[] { "A · Swordsmen / raiders", "B · Ranged / magic", "C · Poison", "C · Burn", "C · Chill", "C · Structures" }) scenarios.AddItem(text);
        scenarios.Selected = _battle!.Scenario == "swordsman" ? 0 : _battle.Scenario == "ranged" ? 1 : 2;
        scenarios.ItemSelected += i => SelectBattle(new[] { "swordsman", "ranged", "status", "burn", "chill", "structures" }[(int)i]); column.AddChild(scenarios);
        column.AddChild(Button("Play / pause  [Space]", () => _battle!.Paused = !_battle.Paused));
        column.AddChild(Button("Restart  [Backspace]", () => { _battle!.Restart(); ObserveCombat(); }));
        column.AddChild(Button("Change speed  [Tab]", CycleBattleSpeed));
        column.AddChild(Button("Sprites / debug  [T]", ToggleBattleArt));
        column.AddChild(Button("Diagnostic overlay  [O]", () => _options = _options with { DebugOverlay = !_options.DebugOverlay }));
        column.AddChild(Button("Topdown / angled  [V]", ToggleBattleView));
        _battleInfo = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart }; column.AddChild(_battleInfo);
        column.AddChild(Button("Main menu", ReturnToMenu));
        column.AddChild(new Label { Text = "One image per character.\nDefault rules; paid campaign replay.\nRight drag: pan · Wheel: zoom\nR: reset · 1–6: scenario", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        if (Argument("--battle-tick") is { } tick) SeekBattle(double.Parse(tick, System.Globalization.CultureInfo.InvariantCulture));
        if (Argument("--battle-zoom") is { } zoom) _zoom = float.Parse(zoom, System.Globalization.CultureInfo.InvariantCulture);
        _pan = new Vector2(float.Parse(Argument("--battle-pan-x") ?? "0", System.Globalization.CultureInfo.InvariantCulture), float.Parse(Argument("--battle-pan-y") ?? "0", System.Globalization.CultureInfo.InvariantCulture));
        if (Argument("--battle-profile") is { } profile)
        {
            _animationProfile = new(profile); Engine.MaxFps = _animationProfile.Fps;
            _battle!.Speed = _animationProfile.Speed; _battle.Paused = false;
        }
        RefreshUi(true);
    }
    void SelectBattle(string scenario) { _battle = new(scenario); ObserveCombat(); ResetCamera(); if (_battleScenarios is not null) _battleScenarios.Selected = Array.IndexOf(new[] { "swordsman", "ranged", "status", "burn", "chill", "structures" }, scenario); UpdateBattleHud(); }
    void SeekBattle(double tick)
    {
        var entries = _battle!.Seek(tick); _battle.Paused = true; ObserveCombat();
        foreach (var entry in entries) _motion.Observe(entry);
        _motion.Trim(Playback.Tick); QueueRedraw();
    }
    void UpdateBattleHud()
    {
        if (_battleInfo is null || _battle is null) return;
        _battleInfo.Text = $"Godot · {_options.Mode} · {_options.View}\n{(_battle.Complete ? "Battle complete" : _battle.Paused ? "Paused" : "Playing")} · {CombatSpeedRates.Label(_battle.Speed)}\nWave {_battle.Snapshot.Wave} · {_battle.Snapshot.Phase}\nSimulation {_battle.Snapshot.Tick}\nPresentation {Playback.Tick:F2}\nEvent {Playback.EventCursor}\nActors {Playback.Units().Length} · Effects {_motion.Events.Count}\nNative shader {(CombatShader ? "on" : "off")}";
    }
    void CycleBattleSpeed() => _battle!.Speed = (CombatSpeed)(((int)_battle.Speed + 1) % 3);
    void ToggleBattleArt()
    {
        _options = _options with { Mode = _options.Mode == RenderMode.Debug ? RenderMode.Sprites : RenderMode.Debug };
        _catalog = new(_options.AssetsRoot, _options.Mode == RenderMode.Sprites || _options.Hud == HudArt.Illustrated);
    }
    void ToggleBattleView()
    {
        _options = _options with { View = _options.View == ViewPreset.Topdown ? ViewPreset.Angled : ViewPreset.Topdown };
        _arena = new(30, _options.GroundCompression); _settlement = new(65, _options.GroundCompression);
    }
    void HandleBattleKey(Key key)
    {
        switch (key)
        {
            case Key.Space: _battle!.Paused = !_battle.Paused; break;
            case Key.Backspace: _battle!.Restart(); ObserveCombat(); break;
            case Key.Tab: CycleBattleSpeed(); break;
            case Key.T: ToggleBattleArt(); break;
            case Key.O: _options = _options with { DebugOverlay = !_options.DebugOverlay }; break;
            case Key.V: ToggleBattleView(); break;
            case Key.Key1: SelectBattle("swordsman"); break;
            case Key.Key2: SelectBattle("ranged"); break;
            case Key.Key3: SelectBattle("status"); break;
            case Key.Key4: SelectBattle("burn"); break;
            case Key.Key5: SelectBattle("chill"); break;
            case Key.Key6: SelectBattle("structures"); break;
        }
    }
    async Task CaptureBattle(string directory)
    {
        directory = System.IO.Path.GetFullPath(directory); System.IO.Directory.CreateDirectory(directory);
        foreach (string scenario in new[] { "swordsman", "ranged", "status", "burn", "chill", "structures" })
        {
            SelectBattle(scenario); _battle!.Paused = true;
            foreach (var point in BattleCaptures.Plan(_battle, !_args.Contains("--no-sequence")))
            {
                SeekBattle(point.Tick); UpdateBattleHud();
                var watch = Stopwatch.StartNew();
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                double frameMs = watch.Elapsed.TotalMilliseconds;
                string path = System.IO.Path.Combine(directory, point.Name + ".png");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
                using var image = GetViewport().GetTexture().GetImage();
                if (image.SavePng(path) != Error.Ok) throw new IOException("Battle capture failed: " + path);
                BattleCaptures.Metadata(System.IO.Path.ChangeExtension(path, ".json"), "godot", _battle, _options, frameMs, GC.GetTotalAllocatedBytes(), _motion.Events.Count, CombatShader, _zoom, _pan.X, _pan.Y);
                GD.Print($"BATTLE_CAPTURE {point.Name} tick={Playback.Tick:F2} event={Playback.EventCursor} figures={_figures.Count}");
            }
        }
        GD.Print("GODOT_ANIMATION_CAPTURE_PASS");
    }
}
