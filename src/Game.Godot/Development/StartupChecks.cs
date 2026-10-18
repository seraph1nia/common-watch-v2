using Godot;
using Game.Shared;

namespace Game.GodotClient;

public partial class GameApplication
{
    private void CheckSteamExtension()
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
    }
    private async Task RunDevelopmentActions()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (Argument("--battle-capture") is { } battleOutput) { await CaptureBattle(battleOutput); await QuitAfterAudioDrain(); return; }
        if (_args.Contains("--gameplay-test")) { await GameplayUiChecks(); await QuitAfterAudioDrain(); return; }
        if (_args.Contains("--smoke-test")) await SmokeIntegration();
        if (_args.Contains("--capture-settings")) _settings.Open();
        if (Argument("--capture") is { } path)
        {
            var captureWatch = System.Diagnostics.Stopwatch.StartNew();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            string absolute = System.IO.Path.GetFullPath(path);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(absolute)!);
            using var image = GetViewport().GetTexture().GetImage();
            Error result = image.SavePng(absolute);
            if (result != Error.Ok) throw new InvalidOperationException($"Viewport capture failed: {result}");
            if (_battle is not null) BattleCaptures.Metadata(System.IO.Path.ChangeExtension(absolute, ".json"), "godot", _battle, _options, captureWatch.Elapsed.TotalMilliseconds, GC.GetTotalAllocatedBytes(), _battlefield.Motion.Events.Count, CombatShader, _camera.Magnification, _camera.PanPixels.X, _camera.PanPixels.Y);
            GD.Print($"GODOT_CAPTURE {absolute} phase={_session?.Snapshot.Phase} mode={_options.Mode}");
            await QuitAfterAudioDrain();
        }
        else if (_args.Contains("--smoke-test")) await QuitAfterAudioDrain();
    }
}
