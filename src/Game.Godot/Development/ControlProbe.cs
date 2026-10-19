using System.Text.Json;
using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class GameApplication
{
    private long _controlWrite;
    private bool _pausedAtDeath;
    private bool _gameplayPolicy;
    private async void CaptureGameplayNetwork(string path)
    {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        using var image = GetViewport().GetTexture().GetImage();
        if (image.SavePng(path) != Error.Ok) GetTree().Quit(1);
    }
    private void RunControlProbe()
    {
        if (Argument("--control-dir") is not { } directory) return;
        System.IO.Directory.CreateDirectory(directory);
        foreach (string path in System.IO.Directory.GetFiles(directory, "request-*.json").Order(StringComparer.Ordinal))
        {
            using var request = JsonDocument.Parse(System.IO.File.ReadAllText(path));
            var root = request.RootElement;
            string Text(string name, string fallback = "") => root.TryGetProperty(name, out var value) ? value.GetString() ?? fallback : fallback;
            int Number(string name, int fallback = -1) => root.TryGetProperty(name, out var value) ? value.GetInt32() : fallback;
            System.IO.File.Delete(path);
            switch (Text("kind"))
            {
                case "leave": ReturnToMenu(); break;
                case "reconnect": BeginJoin(Argument("--join") ?? ""); break;
                case "quit": _ = QuitAfterAudioDrain(); break;
                case "retry": _session?.RetryLast(); break;
                case "send" when _session is { HasState: true }:
                    _session.Send(Text("action"), Number("slot"), Enum.Parse<Building>(Text("building", "Empty")),
                        soldierType: Enum.Parse<UnitType>(Text("soldierType", "Swordsman")),
                        payment: Enum.Parse<ConstructionPayment>(Text("payment", "Standard")),
                        technology: Enum.Parse<TechnologyId>(Text("technology", "None")), resource: Enum.Parse<Resource>(Text("resource", "Wood")),
                        bundles: Number("bundles", 0), unitId: Number("unitId", 0), squadId: Number("squadId", 0), cell: Number("cell", 0)); break;
                case "policy": _gameplayPolicy = Text("enabled") == "yes"; break;
                case "inspect" when _session is { HasState: true }:
                    _gameUi.Cities.Select(_gameUi.Cities.GetItemIndex(Number("owner")));
                    _gameUi.Cities.EmitSignal(OptionButton.SignalName.ItemSelected, (long)_gameUi.Cities.Selected);
                    if (Number("slot") >= 0) _session.SelectSlot(Number("slot"));
                    RefreshUi(true);
                    _gameUi.Details.Open(Number("tab", 0), new GameplayContext(_session, ResumeAfterCommand));
                    if (Number("tab", 0) == 2 && _gameUi.Details.Research.TechnologyButtons.Values.FirstOrDefault() is { } technology)
                        technology.EmitSignal(BaseButton.SignalName.Pressed);
                    break;
                case "close-details": _gameUi.Details.Close(); _gameUi.Draft.Dismiss(); break;
                case "resize": GetWindow().Size = new Vector2I(Number("width", 1280), Number("height", 720)); break;
                case "capture": if (Text("path") is { Length: > 0 } capturePath) CaptureGameplayNetwork(capturePath); break;
                case "probe" when _session is { HasState: true }:
                    _session.ProbeCommand(Text("action"), Number("owner", _session.PlayerId), Text("duplicate") == "yes"); break;
            }
        }
        if (_gameplayPolicy && _session is { HasState: true, Connected: true, Pending: false }
            && (_session.Snapshot.Wave < int.Parse(Argument("--gameplay-stop-wave") ?? "8")
                || _session.Snapshot.Phase == Phase.RewardDraft && _session.Snapshot.Wave == int.Parse(Argument("--gameplay-stop-wave") ?? "8"))
            && !_session.Snapshot.Paused)
        {
            if (_session.Snapshot.Phase == Phase.RewardDraft && _session.City.Run.Choice is null)
                _session.ChooseReward(GameplayScenarios.RewardChoice(_session.City.Run));
            else if (_session.Snapshot.Phase is Phase.Building or Phase.Preparation && !_session.City.Ready)
            {
                if (GameplayScenarios.NextPreparation(_session.Snapshot, _session.PlayerId, Argument("--gameplay-family") ?? "frontline") is { } intent) _session.Submit(intent);
                else _session.Send("ready");
            }
        }
        if (!_pausedAtDeath && Argument("--pause-at-death") == "yes" && _session is { HasState: true, IsHost: true } && _session.Snapshot.DyingBodies.Length > 0)
        { _pausedAtDeath = true; _session.Send("pause"); }
        if (Argument("--test-fast-combat") == "yes" && _session is { HasState: true, IsHost: true } && _session.Snapshot.Phase == Phase.Combat)
            for (int i = 0; i < 10; i++) _session.Step();
        if (System.Environment.TickCount64 - _controlWrite < 80) return;
        _controlWrite = System.Environment.TickCount64;
        var status = new { connected = _session?.Connected ?? false, player = _session?.PlayerId ?? 0,
            viewed = _session is { HasState: true } ? _session.ViewedCity.Id : 0,
            readOnly = _session is { HasState: true } ? new GameplayContext(_session, () => { }).EditReason : "No session",
            disabledResearch = _gameUi.Details.Research.PurchaseButtons.Values.All(b => b.Disabled || !b.Visible),
            hudNotice = _gameUi.Message.Text, readyDisabled = _gameUi.ReadyButton.Disabled,
            pending = _session?.Pending ?? false, status = _session?.Status ?? "Main menu",
            animation = new { hits = _battlefield.Motion.ObservedHits, effects = _battlefield.Motion.Events.Count, generation = _session?.Playback.Generation, resets = _battlefield.Resets, dying = _session?.Playback.Units().Count(u => u.Hex?.Lifecycle == UnitLifecycle.Dying) },
            result = _session?.LastResult, state = _session is { HasState: true } ? _session.Snapshot : null };
        string output = System.IO.Path.Combine(directory, "status.json");
        System.IO.File.WriteAllText(output + ".tmp", JsonSerializer.Serialize(status, new JsonSerializerOptions(WireJson.Options) { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never }));
        System.IO.File.Move(output + ".tmp", output, true);
    }
}
