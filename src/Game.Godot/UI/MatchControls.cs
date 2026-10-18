using Game.Core;
using Godot;

namespace Game.GodotClient;

public partial class MatchControls : VBoxContainer
{
    private GodotSession _session = null!;
    private Label _status = null!;
    private Button _pause = null!;
    private readonly Dictionary<CombatSpeed, Button> _speeds = [];
    public Button ReadyButton { get; private set; } = null!;
    public void Initialize(HudSkin skin, Action submitted)
    {
        void Send(string action) { _session.Send(action); submitted(); }
        ReadyButton = skin.Button("Ready", () => Send(_session.City.Ready ? "unready" : "ready")); ReadyButton.AutowrapMode = TextServer.AutowrapMode.WordSmart; AddChild(ReadyButton);
        _pause = skin.Button("Pause whole match", () => Send(_session.Snapshot.Paused ? "resume" : "pause")); AddChild(_pause);
        var speeds = new HBoxContainer(); AddChild(speeds);
        foreach (CombatSpeed speed in Enum.GetValues<CombatSpeed>())
        {
            var button = skin.Button(CombatSpeedRates.Label(speed), () => Send(CombatSpeedRates.Action(speed))); speeds.AddChild(button); _speeds.Add(speed, button);
        }
        _status = WrapLabel(); AddChild(_status);
    }
    public void Refresh(GodotSession session)
    {
        _session = session; var state = session.Snapshot;
        bool ended = state.Phase is Phase.Defeat or Phase.Victory;
        ReadyButton.Visible = state.Phase is Phase.Building or Phase.Preparation;
        ReadyButton.Text = session.City.Ready ? "Unready — edit your city" : state.Phase == Phase.Preparation ? "Ready for battle — settle production 3" : $"Ready — settle production {state.Turn}";
        if (session.ViewedCity.Id != session.PlayerId) ReadyButton.Text = $"Your city P{session.PlayerId}: " + ReadyButton.Text;
        ReadyButton.Disabled = !session.Connected || session.Pending || state.Paused || session.City.Eliminated;
        _pause.Text = state.Paused ? "Resume whole match" : "Pause whole match";
        _pause.Disabled = !session.Connected || session.Pending || ended;
        foreach (var pair in _speeds)
        {
            pair.Value.Disabled = !session.Connected || session.Pending || ended || !session.IsHost;
            pair.Value.Text = (pair.Key == state.CombatSpeed ? "✓ " : "") + CombatSpeedRates.Label(pair.Key);
            pair.Value.TooltipText = session.IsHost ? "Authoritative combat pace" : "Only the original playing host controls combat speed.";
        }
        _status.Text = $"{(state.Paused ? "PAUSED" : "Match running")} · speed {CombatSpeedRates.Label(state.CombatSpeed)}\n" +
            (ended ? $"{state.Phase} — wave {state.Wave}/{state.TotalWaves}\n{(state.Phase == Phase.Defeat ? PresentationLimits.DefeatText(state.DefeatReason) : "All waves held")}" :
            "Waiting: " + string.Join(", ", state.Players.Where(p => p.Connected && !p.Eliminated && !p.Ready).Select(p => $"P{p.Id}")));
        ReadyButton.TooltipText = "Ready settles one production. Food shortage permits readiness; unfunded field soldiers sit out.";
    }
}
