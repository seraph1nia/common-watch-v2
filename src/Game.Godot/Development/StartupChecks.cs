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
        foreach (string method in new[] { "steamInitEx", "run_callbacks", "initRelayNetworkAccess", "getFriendCount", "getFriendByIndex", "getFriendPersonaName", "getLobbyOwner", "setLobbyData", "getNumLobbyMembers", "getLobbyMemberByIndex", "inviteUserToLobby", "getFriendRelationship", "getLaunchCommandLine" })
            Require(api.HasMethod(method), "GodotSteam API: " + method);
        foreach (string signal in new[] { "lobby_created", "lobby_joined", "join_requested" }) Require(api.HasSignal(signal), "Steam signal: " + signal);
        GD.Print("GODOT_STEAM_ABI_PASS extension=4.22.1 engine=" + Engine.GetVersionInfo()["string"]);
    }
    private async Task RunDevelopmentActions()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (_args.Contains("--steam-friends-test")) { await CheckSteamFriendsPresentation(); await QuitAfterAudioDrain(); return; }
        if (Argument("--environment-captures") is { } environmentOutput) { await CaptureEnvironment(environmentOutput); await QuitAfterAudioDrain(); return; }
        if (Argument("--arena-captures") is { } arenaOutput) { await CaptureModularArena(arenaOutput); await QuitAfterAudioDrain(); return; }
        if (Argument("--major-encounter-review") is { } majorOutput) { await CaptureCombatSequences(majorOutput, majorComparison: true); await QuitAfterAudioDrain(); return; }
        if (Argument("--combat-sequences") is { } sequenceOutput) { await CaptureCombatSequences(sequenceOutput); await QuitAfterAudioDrain(); return; }
        // Presentation-only comparison controls. No scenario or authoritative state edits.
        if (Argument("--settlement-radius") is { } radius)
        {
            float value = float.Parse(radius, System.Globalization.CultureInfo.InvariantCulture);
            if (!float.IsFinite(value) || value < 40 || value > 120) throw new ArgumentException("Settlement comparison radius must be 40..120.");
            _settlementView.HexRadius = value;
            RefreshWorld();
        }
        if (Argument("--settlement-zoom") is { } zoom)
            _camera.SetPose(float.Parse(zoom, System.Globalization.CultureInfo.InvariantCulture), Vector2.Zero);
        if (Argument("--battle-capture") is { } battleOutput) { await CaptureBattle(battleOutput); await QuitAfterAudioDrain(); return; }
        if (_args.Contains("--feedback-review")) { await CheckFeedbackPresentation(); await QuitAfterAudioDrain(); return; }
        if (_args.Contains("--forest-review")) { await CheckForestPresentation(); await QuitAfterAudioDrain(); return; }
        if (_args.Contains("--opening-review")) { await CheckOpeningCampaigns(); await QuitAfterAudioDrain(); return; }
        if (_args.Contains("--army-workspace-test")) { await CheckArmyDeployment(); await QuitAfterAudioDrain(); return; }
        if (_args.Contains("--army-workspace-trace")) { await TraceArmyWorkspace(); await QuitAfterAudioDrain(); return; }
        if (_args.Contains("--journey-test")) { await CheckJourneyTransitions(); await CheckNativeSoloJourney(); await QuitAfterAudioDrain(); return; }
        if (_args.Contains("--squad-recovery-review")) { await CheckSquadRecoveryChoices(); GD.Print("GODOT_SQUAD_RECOVERY_DRAIN"); await QuitAfterAudioDrain(); GD.Print("GODOT_SQUAD_RECOVERY_QUIT"); return; }
        if (_args.Contains("--gameplay-test")) { await GameplayUiChecks(); await QuitAfterAudioDrain(); return; }
        if (_args.Contains("--boss-review")) { await CheckBossPresentation(); await QuitAfterAudioDrain(); return; }
        if (_args.Contains("--veteran-review")) { await CheckVeteranChoices(); await QuitAfterAudioDrain(); return; }
        if (_args.Contains("--worldtree-test")) { Require(_skin.Worldtree is not null, "Worldtree capture pack loaded"); await CheckWorldtreePresentation(); await QuitAfterAudioDrain(); return; }
        if (Argument("--settlement-captures") is { } settlementOutput) { await CheckSettlementMap(settlementOutput); await QuitAfterAudioDrain(); return; }
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
        else if (_args.Contains("--smoke-test"))
        {
            RequestQuit();
            Require(_exitConfirmation.Visible && !_quitting, "smoke exit requires confirmation");
            _exitConfirmation.GetOkButton().EmitSignal(BaseButton.SignalName.Pressed);
            Require(_quitting && _session is null, "confirmed exit closes the active session before draining audio");
            GD.Print("GODOT_EXIT_CONFIRMED_PASS");
        }
    }
}
