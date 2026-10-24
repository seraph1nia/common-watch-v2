using Godot;
using Game.Core;
using Game.Shared;

namespace Game.GodotClient;

public partial class DraftPanel : Control
{
    private HudSkin _skin = null!;
    private Action _submitted = null!;
    private HBoxContainer _cards = null!;
    private PanelContainer _panel = null!;
    private Label _summary = null!;
    public BattleResultStrip Result { get; private set; } = null!;
    private GodotSession _session = null!;
    private string _shape = "", _draftKey = "";
    private bool _dismissed, _awaitingChoiceState;
    private Control? _invoker;
    private Button _close = null!;
    internal Rect2[] FeedbackAreas => new Control[] { _close, Result, _summary, _cards }
        .Where(c => c.IsVisibleInTree()).Select(c => c.GetGlobalRect()).ToArray();
    public Control? FallbackFocus { get; set; }
    public void Dismiss() => Dismiss(true);
    public void Dismiss(bool restoreFocus)
    {
        _dismissed = true; bool wasVisible = Visible; Hide();
        if (wasVisible && restoreFocus) RestoreFocus();
    }
    private void RestoreFocus()
    {
        if (IsInstanceValid(_invoker) && _invoker!.IsVisibleInTree()) _invoker.GrabFocus();
        else if (IsInstanceValid(FallbackFocus) && FallbackFocus!.IsVisibleInTree()) FallbackFocus.GrabFocus();
    }
    public void Open(Control? invoker = null) { _invoker = invoker ?? GetViewport().GuiGetFocusOwner(); _dismissed = false; }
    public void Reset() { Hide(); _draftKey = _shape = ""; _dismissed = false; _invoker = null; }
    private readonly Dictionary<string, RewardCard> _views = [];
    internal IReadOnlyCollection<RewardCard> Cards => _views.Values;
    public IReadOnlyList<Button> RewardButtons => _views.Values.Select(v => v.ChooseButton).ToArray();
    public void Initialize(HudSkin skin, Action submitted)
    {
        _skin = skin; _submitted = submitted; SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var shade = new ColorRect { Color = new Color(0, 0, 0, .7f) }; AddChild(shade); shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _panel = new PanelContainer(); AddChild(_panel); _panel.SetAnchorsPreset(LayoutPreset.Center); skin.Panel(_panel, "modal");
        Resized += FitPanel; FitPanel();
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Reserve, FollowFocus = true }; _panel.AddChild(Margin(scroll, 20));
        var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill }; scroll.AddChild(column);
        _close = skin.Button("Close rewards — inspect your city / run before choosing", Dismiss, "ForestQuietButton"); column.AddChild(_close);
        Result = new BattleResultStrip(); column.AddChild(Result); Result.Initialize(skin);
        _summary = WrapLabel(); column.AddChild(_summary);
        _cards = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; column.AddChild(_cards); Hide();
    }
    private void FitPanel()
    {
        if (Size.X <= 0 || Size.Y <= 0) return;
        float width = Math.Min(1200, Math.Max(0, Size.X - 60));
        float height = Math.Min(580, Math.Max(0, Size.Y - 36));
        _panel.OffsetLeft = -width / 2; _panel.OffsetRight = width / 2;
        _panel.OffsetTop = -height / 2; _panel.OffsetBottom = height / 2;
    }
    private void Choose(RewardCard control, string shape, string id)
    {
        if (_shape != shape || !IsInstanceValid(control) || !control.IsVisibleInTree() || control.ChooseButton.Disabled
            || !IsVisibleInTree() || _awaitingChoiceState || !_session.Connected || _session.Pending
            || _session.Snapshot.Phase != Phase.RewardDraft || _session.City.Eliminated || _session.City.Run.Choice is not null) return;
        _session.ChooseReward(id); _submitted();
    }
    public void Refresh(GodotSession session, bool finishingCombat, BattleOutcome? outcome = null, bool allowed = true, bool awaitingChoiceState = false)
    {
        _session = session; _awaitingChoiceState = awaitingChoiceState;
        string draftKey = session.Snapshot.MatchId + ":" + session.City.Run.DraftWave;
        if (_draftKey != draftKey) { _draftKey = draftKey; _dismissed = false; }
        bool opening = !Visible;
        var focus = GetViewport().GuiGetFocusOwner();
        bool ownedFocus = focus is not null && IsAncestorOf(focus);
        Visible = allowed && !_dismissed && session.Snapshot.Phase == Phase.RewardDraft && !finishingCombat && !session.City.Eliminated && session.City.Run.Choice is null;
        if (!Visible) { if (!opening && ownedFocus) RestoreFocus(); return; }
        Result.Refresh(outcome);
        if (opening) { _invoker ??= GetViewport().GuiGetFocusOwner(); _close.GrabFocus(); }
        var view = RunPresentation.Project(session.Snapshot, session.City, session.PlayerId, session.Connected && !session.Pending && !_awaitingChoiceState, false);
        _summary.Text = view.Title + (view.Status.Length > 0 ? " · " + view.Status.Split('\n')[0] : "");
        if (_awaitingChoiceState) _summary.Text = "Reward accepted · waiting for the current inventory snapshot…";
        _summary.TooltipText = view.Status + "\n" + view.Forecast;
        string shape = session.Snapshot.MatchId + ":" + session.City.Run.DraftWave + ":" + string.Join(',', view.Cards.Select(c => c.Id));
        if (_shape != shape)
        {
            _shape = shape; foreach (Node child in _cards.GetChildren()) { _cards.RemoveChild(child); child.QueueFree(); } _views.Clear();
            foreach (var card in view.Cards)
            {
                string id = card.Id; var control = new RewardCard(); _cards.AddChild(control);
                control.Initialize(_skin, card, () => Choose(control, shape, id));
                _views.Add(id, control);
            }
        }
        foreach (var card in view.Cards)
        {
            _views[card.Id].Refresh(card);
            _views[card.Id].Present(BuildImpact.Reward(session.Snapshot, session.City, card.Id));
        }
    }
}
