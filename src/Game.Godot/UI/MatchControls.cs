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
    public void Initialize(HudSkin skin, Action submitted)
    {
        _illustrated = skin.Illustrated;
        AddThemeConstantOverride("separation", 4);
        void Send(string action) { _session.Send(action); submitted(); }
        var ready = new HBoxContainer(); AddChild(ready);
        ReadyButton = skin.Button("Ready", () => Send(_session.City.Ready ? "unready" : "ready"), "ForestPrimaryButton");
        ReadyButton.CustomMinimumSize = new(0, 48); ReadyButton.SizeFlagsHorizontal = SizeFlags.ExpandFill; ready.AddChild(ReadyButton);
        _pause = skin.Button("Pause match", () => Send(_session.Snapshot.Paused ? "resume" : "pause")); ready.AddChild(_pause);
        _consequence = WrapLabel(); _consequence.AddThemeFontSizeOverride("font_size", 14); AddChild(_consequence);
        _speeds = new HBoxContainer(); AddChild(_speeds);
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
        _consequence.Visible = preparing;
        _consequence.Text = session.City.Ready ? "Waiting for allies · Unready to edit." :
            $"Ready: production {state.Turn}/3" + (state.Turn == 3 ? " → combat." : " → preparation.");
        if (preparing && session.City.FoodForecast is { Unfed.Length: > 0 } food)
            _consequence.Text += $"\n{food.Unfed.Length} unfunded sit out; Ready is allowed.";
        ReadyButton.ThemeTypeVariation = _illustrated && !session.City.Ready ? "ForestPrimaryButton" : "";
        ReadyButton.Disabled = !session.Connected || session.Pending || state.Paused || session.City.Eliminated;
        _pause.Text = state.Paused ? "Resume match" : "Pause match";
        _pause.Visible = !ended;
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
        _status.AddThemeFontSizeOverride("font_size", ended ? 22 : 16);
        ReadyButton.TooltipText = $"Ready settles production {state.Turn}/3. Food shortage permits readiness; unfunded field soldiers sit out.";
    }
}
