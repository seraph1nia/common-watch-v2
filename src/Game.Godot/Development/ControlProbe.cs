using System.Text.Json;
using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class GameApplication
{
    private long _controlWrite;
    private int _controlRequests;
    private bool _pausedAtDeath, _pausedOnFall, _pausedOnLowKeep;
    private bool _gameplayPolicy;
    private int _captureCombatWave;
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
            System.IO.File.Delete(path); _controlRequests++;
            switch (Text("kind"))
            {
                case "single-player": _sessionMenu.SoloButton.EmitSignal(BaseButton.SignalName.Pressed); break;
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
                case "choose-doctrine" when _session is { HasState: true } && _session.Snapshot.Phase == Phase.Lobby:
                    ((DoctrineCard)_sessionMenu.Doctrines.GetChild(Number("index", 0))).ChooseButton.EmitSignal(BaseButton.SignalName.Pressed); RefreshUi(true); break;
                case "team-city" when _gameUi.Team.CityButtons.TryGetValue(Number("owner"), out var teamButton): teamButton.EmitSignal(BaseButton.SignalName.Pressed); break;
                case "team-pointer" when _gameUi.Team.CityButtons.TryGetValue(Number("owner"), out var pointerButton): WorldtreeClick(pointerButton.GetGlobalRect().GetCenter()); break;
                case "ready-pointer": WorldtreeClick(_gameUi.ReadyButton.GetGlobalRect().GetCenter()); break;
                case "watch-ally": _gameUi.Team.WatchButton.EmitSignal(BaseButton.SignalName.Pressed); break;
                case "policy": _gameplayPolicy = Text("enabled") == "yes"; break;
                case "hold-combat": _captureCombatWave = Number("wave", 6); break;
                case "show-homes" when _session is { HasState: true }:
                    _gameUi.Details.Open(DetailsPage.Army, new GameplayContext(_session, ResumeAfterCommand));
                    _gameUi.Details.Army.ShowHomes(); RefreshUi(true); break;
                case "select-squad": _gameUi.Details.Army.SelectSquad(Number("squadId")); break;
                case "deploy-home":
                    if (_gameUi.Details.Deployment.HomeButtons.TryGetValue("Deploy" + Number("slot"), out var target))
                        target.EmitSignal(BaseButton.SignalName.Pressed);
                    break;
                case "inspect-combat" when _session is { HasState: true }:
                    if (_session.Playback.Units().FirstOrDefault(u => u.Id == Number("unitId")) is { } combatant)
                        _gameUi.Inspect(combatant);
                    break;
                case "close-combat": _gameUi.LiveInspector.Close(); break;
                case "inspect" when _session is { HasState: true }:
                    _gameUi.Cities.Select(_gameUi.Cities.GetItemIndex(Number("owner")));
                    _gameUi.Cities.EmitSignal(OptionButton.SignalName.ItemSelected, (long)_gameUi.Cities.Selected);
                    if (Number("slot") >= 0) _session.SelectSlot(Number("slot"));
                    RefreshUi(true);
                    _gameUi.Details.Open(Enum.Parse<DetailsPage>(Text("page", "Army")), new GameplayContext(_session, ResumeAfterCommand));
                    if (Text("page") == "Worldtree" && _gameUi.Details.Research.TechnologyButtons.Values.FirstOrDefault() is { } technology)
                        technology.EmitSignal(BaseButton.SignalName.Pressed);
                    break;
                case "close-details": _gameUi.Details.Close(); _gameUi.Draft.Dismiss(); break;
                case "resize": GetWindow().Size = new Vector2I(Number("width", 1280), Number("height", 720)); break;
                case "capture": if (Text("path") is { Length: > 0 } capturePath) CaptureGameplayNetwork(capturePath); break;
                case "probe" when _session is { HasState: true }:
                    _session.ProbeCommand(Text("action"), Number("owner", _session.PlayerId), Text("duplicate") == "yes"); break;
            }
        }
        if (_gameplayPolicy && _session is { HasState: true, Connected: true, Pending: false } && !_session.City.Eliminated
            && (_session.Snapshot.Wave < int.Parse(Argument("--gameplay-stop-wave") ?? "8")
                || _session.Snapshot.Phase == Phase.RewardDraft && _session.Snapshot.Wave == int.Parse(Argument("--gameplay-stop-wave") ?? "8"))
            && !_session.Snapshot.Paused)
        {
            if (_session.Snapshot.Phase == Phase.RewardDraft && _session.City.Run.Choice is null)
                _session.ChooseReward(GameplayScenarios.RewardChoice(_session.City.Run));
            else if (_session.Snapshot.Phase is Phase.Building or Phase.Preparation && !_session.City.Ready)
            {
                if (PreparationIntent() is { } intent) _session.Submit(intent);
                else _session.Send("ready");
            }
        }
        if (!_pausedOnLowKeep && Argument("--hold-on-low-keep") == "yes" && _session is { HasState: true, IsHost: true }
            && _session.Snapshot.Phase == Phase.Combat && TeamAwareness.Project(_session.Snapshot, _session.PlayerId).Members.Any(m => m.Endangered))
        { _pausedOnLowKeep = true; _session.Send("pause"); }
        if (!_pausedOnFall && Argument("--hold-on-fall") == "yes" && _session is { HasState: true, IsHost: true }
            && _session.Snapshot.Players.Any(p => p.Eliminated) && _session.Snapshot.Phase == Phase.Combat)
        { _pausedOnFall = true; _session.Send("pause"); }
        if (!_pausedAtDeath && Argument("--pause-at-death") == "yes" && _session is { HasState: true, IsHost: true } && _session.Snapshot.DyingBodies.Length > 0)
        { _pausedAtDeath = true; _session.Send("pause"); }
        if (_captureCombatWave > 0 && _session is { HasState: true, IsHost: true } && _session.Snapshot.Phase == Phase.Combat
            && _session.Snapshot.Wave >= _captureCombatWave && _session.Snapshot.Tick - _session.Snapshot.WaveStartedTick >= 180)
        { _captureCombatWave = 0; _session.Send("pause"); }
        if (Argument("--test-fast-combat") == "yes" && _session is { HasState: true, IsHost: true } && _session.Snapshot.Phase == Phase.Combat)
            for (int i = 0; i < 10; i++) _session.Step();
        if (System.Environment.TickCount64 - _controlWrite < 80) return;
        _controlWrite = System.Environment.TickCount64;
        var status = new { processedRequests = _controlRequests, connected = _session?.Connected ?? false, player = _session?.PlayerId ?? 0,
            doctrineOffers = _sessionMenu.Offers, doctrineStartDisabled = _sessionMenu.StartButton.Disabled, doctrineStartVisible = _sessionMenu.StartButton.IsVisibleInTree(),
            viewed = _session is { HasState: true } ? _session.ViewedCity.Id : 0,
            readOnly = _session is { HasState: true } ? new GameplayContext(_session, () => { }).EditReason : "No session",
            disabledResearch = _gameUi.Details.Research.PurchaseButtons.Values.All(b => b.Disabled || !b.Visible),
            teamVisible = _gameUi.Team.IsVisibleInTree(), teamHeading = _gameUi.Team.Heading,
            teamRows = _gameUi.Team.CityButtons.Values.Select(b => b.Text).ToArray(), teamTargetsFit = _gameUi.Team.TargetsFit,
            teamObserver = _gameUi.Team.Overview?.Observer ?? false, readyVisible = _gameUi.ReadyButton.IsVisibleInTree(),
            teamBounds = _gameUi.Team.GetGlobalRect().ToString(),
            feedbackTransactions = _feedback.Transactions, feedbackCombat = _feedback.CombatCues, feedbackMilestones = _feedback.Milestones,
            feedbackPlayed = _audio.Feedback.Played, feedbackSuppressed = _audio.Feedback.Suppressed, feedbackActive = _audio.Feedback.Active,
            feedbackPulses = _gameUi.Feedback.Count, feedbackFits = _gameUi.Feedback.Fits && _gameUi.Feedback.AvoidsGameplay,
            hudNotice = _gameUi.SessionFeedback, readyDisabled = _gameUi.ReadyButton.Disabled,
            readyText = _gameUi.ReadyButton.Text, transitionText = _gameUi.TransitionText,
            preparationText = _gameUi.PreparationSummary.Text,
            preparationVisible = _gameUi.PreparationSummary.IsVisibleInTree(), viewportWidth = GetWindow().Size.X,
            homesVisible = _gameUi.Details.Visible && _gameUi.Details.Army.HomesVisible,
            homeSquad = _gameUi.Details.Army.SelectedSquad, homeNotice = _gameUi.Details.Deployment.Notice,
            homeTargetsDisabled = _gameUi.Details.Deployment.HomeButtons.Where(p => p.Key.StartsWith("Deploy")).All(p => p.Value.Disabled),
            homeTargetsFit = _gameUi.Details.Deployment.HomeButtons.Values.All(b => _gameUi.Details.Army.HomeViewport.GetGlobalRect().Encloses(b.GetGlobalRect()) && GetViewport().GetVisibleRect().Encloses(b.GetGlobalRect())),
            homeLayout = new {
                viewport = _gameUi.Details.Army.HomeViewport.GetGlobalRect().ToString(),
                targets = _gameUi.Details.Deployment.HomeButtons.ToDictionary(p => p.Key, p => p.Value.GetGlobalRect().ToString()) },
            finishingCombat = _battlefield.Finishing,
            liveUnit = _gameUi.LiveInspector.SelectedUnit, liveVisible = _gameUi.LiveInspector.Visible,
            liveText = _gameUi.LiveInspector.Inspector.Details,
            liveFits = GetViewport().GetVisibleRect().Encloses(_gameUi.LiveInspector.GetGlobalRect()),
            blocksWorld = _gameUi.BlocksWorld, squadCues = _battlefield.SquadCueCount,
            highlightedMembers = _battlefield.HighlightedMembers, highlightedTarget = _battlefield.HighlightedTarget,
            resultSummary = _gameUi.Result.Summary,
            readyFits = GetViewport().GetVisibleRect().Encloses(_gameUi.PhaseArea)
                && _gameUi.LedgerArea.End.Y + 8 <= _gameUi.PhaseArea.Position.Y
                && _gameUi.PhaseArea.Encloses(_gameUi.ReadyButton.GetGlobalRect()),
            readyLayout = new { ledger = _gameUi.LedgerArea.ToString(), phase = _gameUi.PhaseArea.ToString(), ready = _gameUi.ReadyButton.GetGlobalRect().ToString() },
            hudNoticeVisible = _gameUi.FeedbackButton.IsVisibleInTree(), hudNoticeText = _gameUi.FeedbackButton.Text,
            pending = _session?.Pending ?? false, status = _session?.Status ?? "Main menu",
            animation = new { hits = _battlefield.Motion.ObservedHits, effects = _battlefield.Motion.Events.Count, generation = _session?.Playback.Generation, resets = _battlefield.Resets, dying = _session?.Playback.Units().Count(u => u.Hex?.Lifecycle == UnitLifecycle.Dying) },
            result = _session?.LastResult, state = _session is { HasState: true } ? _session.Snapshot : null };
        string output = System.IO.Path.Combine(directory, "status.json");
        System.IO.File.WriteAllText(output + ".tmp", JsonSerializer.Serialize(status, new JsonSerializerOptions(WireJson.Options) { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never }));
        System.IO.File.Move(output + ".tmp", output, true);
    }
    private GameplayIntent? PreparationIntent()
    {
        string family = Argument("--gameplay-family") ?? "frontline";
        return family is "idle" or "limited" ? CooperationReview.Next(_session!.Snapshot, _session.PlayerId, family)
            : GameplayScenarios.NextPreparation(_session!.Snapshot, _session.PlayerId, family);
    }
}
