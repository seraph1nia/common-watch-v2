using System.Text.Json;
using Game.Core;
using Game.Development;
using Godot;

namespace Game.GodotClient;

public partial class GameApplication
{
    private bool _nativeResumeStarted, _nativeResumed;
    private int _nativePlayer;
    private readonly OpeningProbe _networkProbe = new();
    private bool _probeRecorded, _probeCaptured;
    private async void CaptureLobby(string path)
    {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        if (GetViewport().GetTexture().GetImage().SavePng(path) != Error.Ok) GetTree().Quit(1);
    }
    private void RunNetworkProbe()
    {
        if (Argument("--network-test") is not { } count || !_session.Connected) return;
        try
        {
            _session.ProbeDuplicates = true;
            if (!_probeCaptured && _session.Snapshot.Phase == Phase.Lobby && _session.Snapshot.Players.Length == int.Parse(count)
                && Argument("--lobby-capture") is { } capture)
            { _probeCaptured = true; CaptureLobby(capture); }
            _networkProbe.Tick(_session.Snapshot, _session.PlayerId, _session.IsHost, int.Parse(count), _session.AllDoctrinesChosen, _session.Pending,
                _session.LastResult, (action, slot, building, payment) => _session.Send(action, slot, building, payment: payment));
            if (_session.IsHost && _session.Snapshot.Phase == Phase.Combat) for (int i = 0; i < 10; i++) _session.Step();
            if (_networkProbe.Complete && Argument("--native-verify") == "yes")
            {
                if (!_session.IsSteam) throw new InvalidOperationException("Native verification requires Steam transport.");
                if (!_session.IsHost && !_nativeResumeStarted)
                { _nativePlayer = _session.PlayerId; _nativeResumeStarted = true; ReconnectCurrent(); return; }
                if (!_session.IsHost)
                {
                    if (_session.PlayerId != _nativePlayer) throw new InvalidOperationException("Steam reconnect changed city.");
                    _nativeResumed = true;
                }
            }
            if (_networkProbe.Complete && Argument("--report") is { } path)
            {
                System.IO.File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(new { player = _session.PlayerId, steam = _session.IsSteam, reconnected = _nativeResumed, feedback = new { voices = _audio.Feedback.Voices, active = _audio.Feedback.Active, played = _audio.Feedback.Played, suppressed = _audio.Feedback.Suppressed }, animation = new { hits = _battlefield.TotalHits, generationHits = _battlefield.Motion.ObservedHits, resets = _battlefield.Resets, effects = _battlefield.Motion.Events.Count, tick = _session.Playback.Tick, generation = _session.Playback.Generation }, state = _session.Snapshot }, WireJson.Options));
                System.IO.File.Move(path + ".tmp", path, true);
                if (!_probeRecorded) GD.Print("GODOT_NETWORK_PASS legal-opening combat individual-draft wave=2");
                _probeRecorded = true;
            }
        }
        catch (Exception e) { GD.PrintErr(e); GetTree().Quit(1); }
    }
}
