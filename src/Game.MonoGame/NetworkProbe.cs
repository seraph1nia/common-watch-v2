using System.Text.Json;
using Game.Core;
using Game.Development;

namespace Game.MonoGameClient;

public sealed partial class WatchGame
{
    private bool _nativeResumeStarted, _nativeResumed;
    private int _nativePlayer;
    readonly OpeningProbe networkProbe = new();
    bool probeRecorded, probeCaptured;
    void RunNetworkProbe()
    {
        if (Arg("--network-test") is not { } count || !session.Connected) return;
        session.ProbeDuplicates = true;
        networkProbe.Tick(session.Snapshot, session.PlayerId, session.IsHost, int.Parse(count), session.Pending,
            session.LastResult, (action, slot, building, payment) => session.Send(action, slot, building, payment: payment));
        if (session.IsHost && session.Snapshot.Phase == Phase.Combat) for (int i = 0; i < 10; i++) session.Step();
        if (networkProbe.Complete && Arg("--native-verify") == "yes")
        {
            if (!session.IsSteam) throw new InvalidOperationException("Native verification requires Steam transport.");
            if (!session.IsHost && !_nativeResumeStarted)
            { _nativePlayer = session.PlayerId; _nativeResumeStarted = true; ReconnectCurrent(); return; }
            if (!session.IsHost)
            {
                if (session.PlayerId != _nativePlayer) throw new InvalidOperationException("Steam reconnect changed city.");
                _nativeResumed = true;
            }
        }
        if (networkProbe.Complete && Arg("--report") is { } path)
        {
            File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(new { player = session.PlayerId, steam = session.IsSteam, reconnected = _nativeResumed, animation = new { hits = totalAnimatedHits, generationHits = motion.ObservedHits, resets = visualResets, effects = motion.Events.Count, tick = session.Playback.Tick, generation = session.Playback.Generation }, state = session.Snapshot }, WireJson.Options));
            File.Move(path + ".tmp", path, true);
            if (!probeRecorded) Console.WriteLine("MONOGAME_NETWORK_PASS legal-opening combat individual-draft wave=2");
            probeRecorded = true;
        }
    }
}
