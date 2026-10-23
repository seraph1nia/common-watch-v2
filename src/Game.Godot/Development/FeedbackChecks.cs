// SPDX-License-Identifier: GPL-3.0-only
using System.Buffers.Binary;
using System.Text.Json;
using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class GameApplication
{
    private async Task CheckFeedbackPresentation()
    {
        ulong music = _audio.Player.GetInstanceId();
        ReplaceSession(ScenarioRunner.CreateAt("initial")); _frozen = true;
        int transactions = _feedback.Transactions;
        _session.Send("build", 0, Building.Farm); _frozen = true;
        Require(_session.LastResult?.Accepted == true && _feedback.Transactions == transactions + 1, "accepted command receipt reaches feedback once");
        await GameplayCapture("feedback-construction"); CheckFeedbackBounds();
        Require(_gameUi.Feedback.Text.Contains("Built Farm"), "confirmed construction is labeled near its foundation");
        int played = _audio.Feedback.Played;
        _session.RetryLast(); RefreshUi(true); RefreshWorld();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Require(_feedback.Transactions == transactions + 1 && _audio.Feedback.Played == played, "exact retry and refresh do not repeat transaction feedback");
        _session.Send("build", 0, Building.Farm);
        Require(_session.LastResult?.Accepted == false && _feedback.Transactions == transactions + 2, "rejected action has distinct receipt");
        await GameplayCapture("feedback-rejected"); CheckFeedbackBounds();
        Require(_gameUi.Feedback.Text.Contains("rejected"), "rejection never claims a purchase succeeded");
        _session.SelectSlot(-1); _session.Send("ready");
        await GameplayCapture("feedback-production"); CheckFeedbackBounds();
        Require(_gameUi.Feedback.Text.Contains("Production collected"), "actual production boundary pulses resources and labels receipt");
        long revision = _session.Snapshot.Revision;
        Click(_camera.ToScreen(_settlementView.PlotPosition(2))); _frozen = true;
        Require(_session.SelectedSlot == 2 && _session.Snapshot.Revision == revision, "transient labels remain input-free over native world selection");

        ReplaceSession(ScenarioRunner.CreateAt("battle-start")); _frozen = true;
        for (int n = 0; n < 210; n++) _session.Step();
        await GameplayCapture("feedback-live");
        Require(_feedback.CombatCues > 0 && _audio.Feedback.Voices == 6 && _audio.Feedback.Active <= 6, "presented combat has bounded retained voices");
        _audio.Feedback.Reset();
        Require(_audio.Feedback.Play(new(FeedbackKind.BossArrival, "")), "start a retained SFX tail for native pause verification");
        _session.Send("pause"); _frozen = true;
        await GameplayCapture("feedback-paused");
        int combat = _feedback.CombatCues; double visual = _session.Playback.VisualSeconds;
        var tail = _audio.Feedback.CombatPositions;
        Require(tail.Length > 0, "pause retains an active native SFX tail");
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Require(tail.SequenceEqual(_audio.Feedback.CombatPositions) && _audio.Feedback.CombatPaused && _session.Playback.VisualSeconds == visual && _feedback.CombatCues == combat, "pause freezes SFX tails and presented combat clock without a replay burst");
        _session.Send("resume"); _frozen = true;
        foreach (string speed in new[] { "speed-1x", "speed-1.5x", "speed-2x" })
        {
            _session.Send(speed); Require(_session.LastResult?.Accepted == true, "supported feedback pace " + speed);
            for (int n = 0; n < 5; n++) _session.Step();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        _frozen = true;
        int cues = _feedback.CombatCues;
        _session.Playback.Accept(_session.Snapshot, baseline: true);
        await GameplayCapture("feedback-resumed-baseline");
        Require(_feedback.CombatCues == cues && _gameUi.Feedback.Count == 0, "resumed/gapped snapshot baselines do not replay old events or labels");
        _gameUi.Feedback.Show(new(FeedbackKind.Action, "Prior baseline"));
        _session.Playback.Accept(_session.Snapshot, baseline: true);
        long cursor = _session.Playback.EventCursor;
        for (int n = 0; n < 180 && _session.Playback.EventCursor == cursor; n++)
        { _session.Step(); RefreshWorld(); }
        Require(_session.Playback.EventCursor > cursor && _gameUi.Feedback.Count == 0,
            "an event observed before the coordinator update also clears prior-generation labels");
        await GameplayCapture("feedback-event-first-baseline");
        ReplaceSession(ScenarioRunner.CreateAt("combat")); _frozen = true;
        cues = _feedback.CombatCues; transactions = _feedback.Transactions;
        await GameplayCapture("feedback-session-baseline");
        Require(_feedback.CombatCues == cues && _feedback.Transactions == transactions, "replacement session does not replay checkpoint receipts/history");
        Require(_audio.Player.GetInstanceId() == music && _audio.Player.Playing, "feedback preserves application music across session replacement");
        await CheckFeedbackOutcome("wave-clear", FeedbackKind.WaveClear);
        await CheckFeedbackOutcome("defeat", FeedbackKind.Defeat);
        await CheckFeedbackOutcome("victory", FeedbackKind.Victory);
        if (Argument("--feedback-audition") is { } directory) await RecordFeedbackAudition(directory);
        GD.Print("GODOT_FEEDBACK_PASS receipts retry rejection production pointer pause speeds baseline replacement voices=6 sizes=" + GetWindow().Size);
    }
    private async Task CheckFeedbackOutcome(string name, FeedbackKind expected)
    {
        int before = _audio.Feedback.PlayedKinds.GetValueOrDefault(expected);
        if (expected == FeedbackKind.Victory)
        {
            ReplaceSession(GameplayScenarios.CreateAt("boss-20")); _frozen = true;
            int bosses = _audio.Feedback.PlayedKinds.GetValueOrDefault(FeedbackKind.BossArrival);
            _session.Send("ready"); _frozen = true;
            for (int n = 0; n < 160 && _audio.Feedback.PlayedKinds.GetValueOrDefault(FeedbackKind.BossArrival) == bosses; n++)
            { _session.Step(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
            Require(_audio.Feedback.PlayedKinds.GetValueOrDefault(FeedbackKind.BossArrival) == bosses + 1, "new presented boss arrival plays once");
            await GameplayCapture("feedback-boss-arrival");
        }
        else
        {
            ReplaceSession(ScenarioRunner.CreateAt(expected == FeedbackKind.Defeat ? "initial" : "battle-start")); _frozen = true;
            if (expected == FeedbackKind.Defeat) for (int n = 0; n < 3; n++) _session.Send("ready");
        }
        for (int n = 0; n < 18000 && (_session.Snapshot.Phase == Phase.Combat || _session.Snapshot.DyingBodies.Length > 0);)
        {
            for (int k = 0; k < 12 && (_session.Snapshot.Phase == Phase.Combat || _session.Snapshot.DyingBodies.Length > 0); k++, n++) _session.Step();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        for (int n = 0; n < 6; n++)
        { _session.Playback.Advance(.1, connected: true); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
        await GameplayCapture("feedback-" + name); CheckFeedbackBounds();
        Require(_audio.Feedback.PlayedKinds.GetValueOrDefault(expected) == before + 1, "presented outcome plays exactly once: " + expected);
    }
    private void CheckFeedbackBounds() => Require(_gameUi.Feedback.Count > 0 && _gameUi.Feedback.Fits && _gameUi.Feedback.AvoidsGameplay,
        $"short feedback labels fit and avoid gameplay controls: count={_gameUi.Feedback.Count}, fits={_gameUi.Feedback.Fits}, avoids={_gameUi.Feedback.AvoidsGameplay}, phase={_session.Snapshot.Phase}, finishing={_battlefield.Finishing}, presented={_session.Playback.Tick}, source={_session.Snapshot.Tick}");

    private async Task RecordFeedbackAudition(string directory)
    {
        ReturnToMenu(); System.IO.Directory.CreateDirectory(directory);
        float volume = _audio.Settings.MusicVolume; _audio.SetVolume(0);
        var recorder = new AudioEffectRecord { Format = AudioStreamWav.FormatEnum.Format16Bits };
        int effect = AudioServer.GetBusEffectCount(0); AudioServer.AddBusEffect(0, recorder);
        var palette = new List<object>();
        try
        {
            recorder.SetRecordingActive(true);
            foreach (var kind in Enum.GetValues<FeedbackKind>())
            {
                _audio.Feedback.Reset();
                var tone = _audio.Feedback.Tones[kind];
                string file = System.IO.Path.Combine(directory, kind + ".wav");
                Require(tone.SaveToWav(file) == Error.Ok, "export actual cached native cue stream " + kind);
                Require(_audio.Feedback.Play(new(kind, kind.ToString())), "native audible palette voice " + kind);
                palette.Add(new { kind, bus = new FeedbackCue(kind, "").Combat ? "SFX" : "UI", seconds = FeedbackTones.Duration(kind), file = kind + ".wav" });
                await ToSignal(GetTree().CreateTimer(FeedbackTones.Duration(kind) + .16), Godot.Timer.SignalName.Timeout);
            }
            recorder.SetRecordingActive(false);
            using var recording = recorder.GetRecording();
            Require(recording is not null && recording.Data.Length > 0, "native Master mixer records UI/SFX independently of muted music");
            Require(recording!.SaveToWav(System.IO.Path.Combine(directory, "native-mix.wav")) == Error.Ok, "save actual native mixed audition");
            int peak = 0; long energy = 0;
            byte[] pcm = recording.Data;
            for (int i = 0; i + 1 < pcm.Length; i += 2) { int sample = BinaryPrimitives.ReadInt16LittleEndian(pcm.AsSpan(i, 2)); peak = Math.Max(peak, Math.Abs(sample)); energy += (long)sample * sample; }
            Require(peak > 50 && peak < 32767 && energy > 0, "native mixed audition has audible nonzero PCM and unclipped headroom");
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "receipt.json"), JsonSerializer.Serialize(new {
                driver = AudioServer.GetDriverName(), musicMuted = AudioServer.IsBusMute(_audio.MusicBus), sourceMusic = MusicTrack.RelativePath,
                rate = recording.MixRate, recording.Stereo, bytes = pcm.Length, peak, rms = Math.Sqrt(energy / (double)(pcm.Length / 2)),
                voices = _audio.Feedback.Voices, palette, physicalListening = "Not executed; local PCM/native evidence accepted by user"
            }, new JsonSerializerOptions(WireJson.Options) { WriteIndented = true }));
            GD.Print($"GODOT_FEEDBACK_MIX_PASS driver={AudioServer.GetDriverName()} music-muted=true voices=6 peak={peak} native-pcm={pcm.Length}");
        }
        finally
        {
            recorder.SetRecordingActive(false); AudioServer.RemoveBusEffect(0, effect); recorder.Dispose();
            _audio.Feedback.Reset(); _audio.SetVolume(volume);
        }
    }
}
