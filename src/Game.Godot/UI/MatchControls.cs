using Game.Core;
using Godot;

namespace Game.GodotClient;

public partial class MatchControls : VBoxContainer
{
    private GodotSession _session = null!;
    private Label _status = null!, _consequence = null!;
    private Button _pause = null!;
    private HBoxContainer _speeds = null!;
    private readonly Dictionary<CombatSpeed, Button> _speedButtons = [];
    public Button ReadyButton { get; private set; } = null!;
    public void Initialize(HudSkin skin, Action submitted)
    {
        void Send(string action) { _session.Send(action); submitted(); }
        var ready = new HBoxContainer(); AddChild(ready);
        ReadyButton = skin.Button("Ready", () => Send(_session.City.Ready ? "unready" : "ready"), "ForestPrimaryButton");
        ReadyButton.CustomMinimumSize = new(110, 48); ready.AddChild(ReadyButton);
        _consequence = WrapLabel(); ready.AddChild(_consequence);
        var pace = new HBoxContainer(); AddChild(pace);
        _pause = skin.Button("Pause match", () => Send(_session.Snapshot.Paused ? "resume" : "pause"), "ForestArcaneButton"); pace.AddChild(_pause);
        _speeds = new HBoxContainer(); pace.AddChild(_speeds);
        foreach (CombatSpeed speed in Enum.GetValues<CombatSpeed>())
        {
            var button = skin.Button(CombatSpeedRates.Label(speed), () => Send(CombatSpeedRates.Action(speed))); _speeds.AddChild(button); _speedButtons.Add(speed, button);
        }
        _status = WrapLabel(); AddChild(_status);
    }
    public void Refresh(GodotSession session, bool finishingCombat = false)
    {
        _session = session; var state = session.Snapshot;
        bool ended = state.Phase is Phase.Defeat or Phase.Victory;
        bool combat = state.Phase == Phase.Combat || finishingCombat;
        bool preparing = !finishingCombat && state.Phase is Phase.Building or Phase.Preparation;
        ReadyButton.Visible = preparing; _speeds.Visible = combat;
        ReadyButton.Text = session.City.Ready ? "Unready" : "Ready";
        _consequence.Text = preparing ? $"Your city P{session.PlayerId} · settles production {state.Turn}/3" +
            (state.Phase == Phase.Preparation ? ", then battle." : ".") : ended ? state.Phase.ToString() : combat ? "Battle inspection available below." : "Choose a real reward to continue.";
        ReadyButton.Disabled = !session.Connected || session.Pending || state.Paused || session.City.Eliminated;
        _pause.Text = state.Paused ? "Resume match" : "Pause match";
        _pause.Disabled = !session.Connected || session.Pending || ended;
        foreach (var pair in _speedButtons)
        {
            pair.Value.Disabled = !session.Connected || session.Pending || ended || !session.IsHost;
            pair.Value.Text = (pair.Key == state.CombatSpeed ? "✓ " : "") + CombatSpeedRates.Label(pair.Key);
            pair.Value.TooltipText = session.IsHost ? "Authoritative combat pace" : "Only the original playing host controls combat speed.";
        }
        _status.Text = (state.Paused ? "PAUSED · " : "") +
            (ended ? $"{state.Phase} · {(state.Phase == Phase.Defeat ? PresentationLimits.DefeatText(state.DefeatReason) : "All waves held")}" :
            "Waiting: " + string.Join(", ", state.Players.Where(p => p.Connected && !p.Eliminated && !p.Ready).Select(p => $"P{p.Id}")));
        _status.Visible = state.Paused || ended;
        ReadyButton.TooltipText = "Ready settles one production. Food shortage permits readiness; unfunded field soldiers sit out.";
    }
}
