using Game.Core;
using Godot;

namespace Game.GodotClient;

public partial class MatchControls : VBoxContainer
{
    private GodotSession _session = null!;
    private Label _status = null!;
    private Label _consequence = null!;
    private Button _pause = null!;
    private HBoxContainer _speeds = null!;
    private readonly Dictionary<CombatSpeed, Button> _speedButtons = [];
    private bool _illustrated;
    public Button ReadyButton { get; private set; } = null!;
    internal Button PauseButton => _pause;
    internal string TransitionText => _consequence.Text;
    public void Initialize(HudSkin skin, Action submitted)
    {
        _illustrated = skin.Illustrated;
        AddThemeConstantOverride("separation", 4);
        void Send(string action) { _session.Send(action); submitted(); }
        var ready = new HBoxContainer(); AddChild(ready);
        ReadyButton = skin.Button("Ready", () => Send(_session.City.Ready ? "unready" : "ready"), "ForestPrimaryButton");
        ReadyButton.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        ReadyButton.CustomMinimumSize = new(0, 44); ReadyButton.SizeFlagsHorizontal = SizeFlags.ExpandFill; ready.AddChild(ReadyButton);
        _pause = skin.Button("Pause", () => Send(_session.Snapshot.Paused ? "resume" : "pause"), "ForestArcaneButton");
        _pause.SizeFlagsHorizontal = SizeFlags.ExpandFill; _pause.CustomMinimumSize = new(0, 44); ready.AddChild(_pause);
        _consequence = WrapLabel(); _consequence.AddThemeFontSizeOverride("font_size", 14); AddChild(_consequence);
        _speeds = new HBoxContainer(); AddChild(_speeds);
        foreach (CombatSpeed speed in Enum.GetValues<CombatSpeed>())
        {
            var button = skin.Button(CombatSpeedRates.Label(speed), () => Send(CombatSpeedRates.Action(speed))); _speeds.AddChild(button); _speedButtons.Add(speed, button);
        }
        _status = WrapLabel(); AddChild(_status);
        MoveChild(ready, GetChildCount() - 1);
    }
    public void Refresh(GodotSession session, bool finishingCombat = false)
    {
        _session = session; var state = session.Snapshot;
        bool ended = state.Phase is Phase.Defeat or Phase.Victory;
        bool combat = state.Phase == Phase.Combat || finishingCombat;
        bool preparing = !finishingCombat && state.Phase is Phase.Building or Phase.Preparation;
        ReadyButton.Visible = preparing && !session.City.Eliminated; _speeds.Visible = combat;
        string next = state.Phase == Phase.Preparation ? "battle" : $"prep {state.Turn + 1}";
        ReadyButton.Text = session.City.Ready ? "Unready" : $"Ready · {next}";
        if (session.ViewedCity.Id != session.PlayerId) ReadyButton.Text = (session.City.Ready ? "Unready" : "Ready") + $" · P{session.PlayerId}";
        _consequence.Visible = preparing;
        _consequence.Text = (state.Paused ? "PAUSED · Next: " : "Next: ") + (state.Phase == Phase.Preparation
            ? "income + Food → battle."
            : $"income → prep {state.Turn + 1}/3.");
        if (session.City.Eliminated && !ended) _consequence.Text = "Observing · survivors advance the match.";
        if (state.Players.Length > 1)
        {
            int required = state.Players.Count(p => p.Connected && !p.Eliminated);
            int readyCount = state.Players.Count(p => p.Connected && !p.Eliminated && p.Ready);
            _consequence.Text += $"\nAll connected survivors Ready: {readyCount}/{required}.";
        }
        ReadyButton.ThemeTypeVariation = _illustrated ? "ForestPrimaryButton" : "";
        ReadyButton.Disabled = !session.Connected || session.Pending || state.Paused || session.City.Eliminated;
        _pause.Text = state.Paused ? "Resume" : "Pause";
        _pause.TooltipText = "Pause or resume the whole match through the existing authoritative command.";
        _pause.Visible = !ended;
        _pause.Disabled = !session.Connected || session.Pending || ended;
        foreach (var pair in _speedButtons)
        {
            pair.Value.Disabled = !session.Connected || session.Pending || ended || !session.IsHost;
            pair.Value.Text = (pair.Key == state.CombatSpeed ? "✓ " : "") + CombatSpeedRates.Label(pair.Key);
            pair.Value.TooltipText = session.IsHost ? "Authoritative combat pace" : "Only the original playing host controls combat speed.";
        }
        _status.Text = state.Paused && preparing ? "PAUSED" : (state.Paused ? "PAUSED · " : "") +
            (ended ? $"{state.Phase} · {(state.Phase == Phase.Defeat ? PresentationLimits.DefeatText(state.DefeatReason) : "All waves held")}" :
            "Waiting: " + string.Join(", ", state.Players.Where(p => p.Connected && !p.Eliminated && !p.Ready).Select(p => $"P{p.Id}")));
        _status.Visible = state.Paused && !preparing || ended;
        _status.AddThemeFontSizeOverride("font_size", ended ? 22 : 16);
        ReadyButton.TooltipText = $"Ready controls your city P{session.PlayerId}; Unready unlocks editing.\nAll connected, standing cities must be Ready\nto settle production {state.Turn}/3.\nEach settlement advances research and eligible paid recovery.\nOnly the final settlement pays battle Food.\nFood shortage permits Ready; unfunded field soldiers sit out.";
    }
}
