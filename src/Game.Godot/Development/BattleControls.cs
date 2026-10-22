using Game.Core;
using Game.Shared;
using Godot;
using System.Diagnostics;

namespace Game.GodotClient;

public partial class GameApplication
{
    Label? _battleInfo;
    CanvasLayer? _battleLayer;
    OptionButton? _battleScenarios;
    Game.Development.AnimationProfile? _animationProfile;
    void InitializeBattle()
    {
        _battlefield.Present(_session, _battle);
        var layer = new CanvasLayer(); _battleLayer = layer; AddChild(layer);
        var panel = new PanelContainer { Theme = _skin.Theme }; layer.AddChild(panel);
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.RightWide);
        panel.OffsetLeft = -360; panel.OffsetRight = -8; panel.OffsetTop = 112; panel.OffsetBottom = -12;
        var column = new VBoxContainer();
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; panel.AddChild(scroll); scroll.AddChild(Margin(column, 20));
        column.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        column.AddChild(new Label { Text = "ANIMATED TABLETOP FIGURES", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        var scenarios = new OptionButton(); _battleScenarios = scenarios;
        string[] names = ["swordsman", "ranged", "status", "burn", "chill", "structures", "boss-10", "boss-20"];
        foreach (var text in new[] { "A · Swordsmen / raiders", "B · Ranged / magic", "C · Poison", "C · Burn", "C · Chill", "C · Structures", "D · Wave 10 boss", "D · Wave 20 boss" }) scenarios.AddItem(text);
        scenarios.Selected = Math.Max(0, Array.IndexOf(names, _battle!.Scenario));
        scenarios.ItemSelected += i => SelectBattle(names[(int)i]); column.AddChild(scenarios);
        column.AddChild(_skin.Button("Play / pause  [Space]", () => _battle!.Paused = !_battle.Paused));
        column.AddChild(_skin.Button("Restart  [Backspace]", () => { _battle!.Restart(); _battlefield.Present(_session, _battle); }));
        column.AddChild(_skin.Button("Change speed  [Tab]", CycleBattleSpeed));
        column.AddChild(_skin.Button("Sprites / debug  [T]", ToggleBattleArt));
        column.AddChild(_skin.Button("Diagnostic overlay  [O]", () => _options = _options with { DebugOverlay = !_options.DebugOverlay }));
        column.AddChild(_skin.Button("Topdown / angled  [V]", ToggleBattleView));
        _battleInfo = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart }; column.AddChild(_battleInfo);
        column.AddChild(_skin.Button("Main menu", ReturnToMenu));
        column.AddChild(new Label { Text = "One image per character.\nDefault rules; paid campaign replay.\nResponsive camera framing\n1–6: scenario", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        if (Argument("--battle-tick") is { } tick) SeekBattle(double.Parse(tick, System.Globalization.CultureInfo.InvariantCulture));
        if (Argument("--battle-profile") is { } profile)
        {
            _animationProfile = new(profile); Engine.MaxFps = _animationProfile.Fps;
            _battle!.Speed = _animationProfile.Speed; _battle.Paused = false;
        }
        RefreshUi(true);
    }
    void SelectBattle(string scenario) { _battle = new(scenario); _battlefield.Present(_session, _battle); ResetCamera(); if (_battleScenarios is not null) _battleScenarios.Selected = Array.IndexOf(new[] { "swordsman", "ranged", "status", "burn", "chill", "structures" }, scenario); UpdateBattleHud(); }
    void SeekBattle(double tick)
    {
        var entries = _battle!.Seek(tick); _battle.Paused = true; _battlefield.Present(_session, _battle);
        foreach (var entry in entries) _battlefield.Motion.Observe(entry);
        _battlefield.Motion.Trim(_battlefield.Playback.Tick); RefreshWorld();
    }
    void UpdateBattleHud()
    {
        if (_battleInfo is null || _battle is null) return;
        _battleInfo.Text = $"Godot · {_options.Mode} · {_options.View}\n{(_battle.Complete ? "Battle complete" : _battle.Paused ? "Paused" : "Playing")} · {CombatSpeedRates.Label(_battle.Speed)}\nWave {_battle.Snapshot.Wave} · {_battle.Snapshot.Phase}\nSimulation {_battle.Snapshot.Tick}\nPresentation {_battlefield.Playback.Tick:F2}\nEvent {_battlefield.Playback.EventCursor}\nActors {_battlefield.Playback.Units().Length} · Effects {_battlefield.Motion.Events.Count}\nNative shader {(CombatShader ? "on" : "off")}";
    }
    void CycleBattleSpeed() => _battle!.Speed = (CombatSpeed)(((int)_battle.Speed + 1) % 3);
    void ToggleBattleArt()
    {
        _options = _options with { Mode = _options.Mode == RenderMode.Debug ? RenderMode.Sprites : RenderMode.Debug };
        _assets.Configure(_options);
    }
    void ToggleBattleView()
    {
        _options = _options with { View = _options.View == ViewPreset.Topdown ? ViewPreset.Angled : ViewPreset.Topdown };
        RefreshWorld();
    }
    void HandleBattleKey(Key key)
    {
        switch (key)
        {
            case Key.Space: _battle!.Paused = !_battle.Paused; break;
            case Key.Backspace: _battle!.Restart(); _battlefield.Present(_session, _battle); break;
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
                BattleCaptures.Metadata(System.IO.Path.ChangeExtension(path, ".json"), "godot", _battle, _options, frameMs, GC.GetTotalAllocatedBytes(), _battlefield.Motion.Events.Count, CombatShader, _camera.Magnification, _camera.PanPixels.X, _camera.PanPixels.Y);
                GD.Print($"BATTLE_CAPTURE {point.Name} tick={_battlefield.Playback.Tick:F2} event={_battlefield.Playback.EventCursor} figures={_battlefield.FigureCount}");
            }
        }
        GD.Print("GODOT_ANIMATION_CAPTURE_PASS");
    }
}
