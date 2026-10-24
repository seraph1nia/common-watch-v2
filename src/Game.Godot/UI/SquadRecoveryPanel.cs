// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using Game.Shared;
using Godot;
namespace Game.GodotClient;

/// <summary>Selected-squad review and finite member commands. The session retains transport/retry ownership.</summary>
public partial class SquadRecoveryPanel : VBoxContainer
{
    private HudSkin _skin = null!;
    private GameplayContext? _context;
    private GodotSession? _session;
    private Action<Command, CommandResult>? _ack;
    private Action _submitted = null!;
    private OptionButton _halls = null!;
    private Label _summary = null!, _status = null!, _copy = null!;
    private ScrollContainer _copyScroll = null!;
    private ActionList _entry = null!, _reviewActions = null!;
    private SquadRecoveryPlan? _review;
    private RecoverySequence? _sequence;
    private object? _scope;
    private int _home, _squad;
    private bool _refreshing, _pumping;
    internal IReadOnlyDictionary<string, Button> Actions => _review is null ? _entry.Buttons : _reviewActions.Buttons;
    internal OptionButton HallSelector => _halls;
    internal string Copy => _summary.Text + "\n" + _status.Text + "\n" + _copy.Text;
    internal RecoverySequence? Sequence => _sequence;
    public bool Reviewing => _review is not null || Busy;
    private bool Busy => _sequence is { } s && (s.PendingMember != 0 || !s.Finished && s.Stopped.Length == 0);
    public void Initialize(HudSkin skin, Action submitted)
    {
        _skin = skin; _submitted = submitted; AddThemeConstantOverride("separation", 4);
        _summary = WrapLabel(); _summary.AddThemeFontSizeOverride("font_size", 14); AddChild(_summary);
        _halls = new OptionButton { FitToLongestItem = false, ClipText = true }; AddChild(_halls); _halls.ItemSelected += HallChanged;
        _entry = new ActionList(); AddChild(_entry);
        _status = WrapLabel(); _status.AddThemeFontSizeOverride("font_size", 14); AddChild(_status);
        _reviewActions = new ActionList(); AddChild(_reviewActions);
        _copyScroll = new ScrollContainer { CustomMinimumSize = new(0, 100), SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, VerticalScrollMode = ScrollContainer.ScrollMode.Reserve, FollowFocus = true };
        AddChild(_copyScroll); _copy = WrapLabel(); _copy.SizeFlagsHorizontal = SizeFlags.ExpandFill; _copy.AddThemeFontSizeOverride("font_size", 14); _copyScroll.AddChild(_copy); _copyScroll.Hide();
        SetProcess(false);
    }
    private void Bind(GodotSession session)
    {
        if (ReferenceEquals(session, _session)) return;
        if (_session is not null && _ack is not null) _session.CommandAcknowledged -= _ack;
        Cancel(); _sequence = null; _session = session;
        var owner = session;
        _ack = (command, result) =>
        {
            if (!ReferenceEquals(owner, _session)) return;
            _sequence?.Acknowledge(command, result);
            if (_sequence is not null) SetProcess(true);
        };
        session.CommandAcknowledged += _ack;
    }
    public void Cancel()
    {
        _sequence?.Cancel("Stopped by a context change or cancel."); _review = null; _copyScroll?.Hide();
        if (_sequence is { PendingMember: > 0 }) SetProcess(true); else SetProcess(false);
    }
    private void HallChanged(long _) { if (!_refreshing) { Cancel(); if (_context is { } context) Refresh(context, _home, _squad); } }
    public void Refresh(GameplayContext context, int home, int squad)
    {
        if (_refreshing) return;
        _refreshing = true;
        try
        {
            Bind(context.Session);
            object scope = (context.Session, context.State.MatchId, context.City.Id, home, context.State.TurnSerial);
            if (!Equals(_scope, scope)) { Cancel(); _sequence = null; _scope = scope; }
            _context = context; _home = home; _squad = squad;
            if (_review is not null && _sequence is null && !SquadRecovery.IsCurrent(_review, context.State))
            { Cancel(); _status.Text = "Review changed; choose recovery again."; }
            NativeOptions.Update(_halls, context.City.Army?.Halls.Select(h => (h.Slot, $"Town Hall {h.Slot + 1} · {context.City.Soldiers.Count(u => u.Assignment is { Stored: true } a && a.HallSlot == h.Slot)}/{h.Capacity}")) ?? []);
            var wounded = context.City.Soldiers.Where(u => u.SquadId == squad && u.Assignment is { Stored: false } && u.Health < u.Profile.Health).ToArray();
            Visible = context.City.Id == context.Session.PlayerId && !context.City.Eliminated
                && context.State.Phase is Phase.Building or Phase.Preparation && (wounded.Length > 0 || _review is not null || _sequence is not null);
            _halls.Visible = _halls.ItemCount > 0; _halls.Disabled = Busy || _review is not null;
            _summary.Text = _review?.Summary ?? (wounded.Length > 0 ? $"Selected squad: {wounded.Length} wounded. Keep is no action."
                + (_halls.ItemCount == 0 ? " A Town Hall is needed for storage recovery." : "") : "Recovered members can return explicitly; destinations are reviewed first.");
            if (_review is null && wounded.Any(u => context.City.RecruitmentQuotes.Any(q => q.Type == u.Type && q.Level > u.Level && context.City.Slots.Any(s => s.Level == q.Level && context.State.BuildingCatalog.SingleOrDefault(b => b.Type == s.Type)?.Recruits?.Contains(u.Type) == true))))
                _summary.Text += "\nNewer equipment is available. Recovery restores HP only; inspect Member replacement quotes.";
            _entry.Visible = _review is null && !Busy;
            _entry.Begin((scope, squad));
            string reason = context.EditReason.Length > 0 ? context.EditReason : _halls.ItemCount == 0 ? "Build a Town Hall first." : "";
            if (wounded.Length > 0) _entry.Item(_skin, "Recover", "Review wounded squad recovery", () => Review(RecoveryMove.StoreWounded), reason);
            _entry.End();
            if (_sequence is not null) _status.Text = _sequence.Status;
            else if (_review is null) _status.Text = "";
            _status.Visible = _status.Text.Length > 0;
            RenderReview();
        }
        finally { _refreshing = false; }
    }
    public void ReviewReturns(GameplayContext context, int home, int squad, int hall)
    {
        Refresh(context, home, squad);
        int index = _halls.GetItemIndex(hall); if (index < 0 || context.EditReason.Length > 0) return;
        _halls.Select(index); Visible = true; Review(RecoveryMove.ReturnRecovered);
    }
    private void Review(RecoveryMove move)
    {
        if (!IsVisibleInTree() || _context is not { } context || context.EditReason.Length > 0 || Busy) return;
        _sequence = null; _review = SquadRecovery.Project(context.State, context.City.Id, _squad, _halls.GetSelectedId(), move);
        _copyScroll.Hide(); Refresh(context, _home, _squad); _submitted();
        if (_reviewActions.Buttons.TryGetValue("Cancel", out var cancel)) cancel.GrabFocus();
    }
    private void RenderReview()
    {
        _reviewActions.Visible = _review is not null || Busy; _reviewActions.Begin(_review);
        if (_review is { } plan)
        {
            _copy.Text = plan.Details;
            _reviewActions.Item(_skin, "Commit", $"Move {plan.Moving.Length} reviewed members", () => Commit(plan), Busy ? "Moving members." : !plan.CanCommit ? "No reviewed move fits." : _context!.EditReason);
            _reviewActions.Item(_skin, "Details", _copyScroll.Visible ? "Hide member exceptions" : "Member exceptions & destinations", () => { if (ReferenceEquals(plan, _review)) _copyScroll.Visible = !_copyScroll.Visible; });
            _reviewActions.Item(_skin, "Cancel", Busy ? "Stop remaining moves" : "Keep / cancel", () => { if (ReferenceEquals(plan, _review)) { Cancel(); Refresh(_context!, _home, _squad); _submitted(); } });
        }
        _reviewActions.End();
    }
    private void Commit(SquadRecoveryPlan plan)
    {
        if (!IsVisibleInTree() || !ReferenceEquals(plan, _review) || _sequence is not null || _context is not { } context
            || context.EditReason.Length > 0 || !plan.CanCommit || !SquadRecovery.IsCurrent(plan, context.State)) return;
        _sequence = new(plan); SetProcess(true); Refresh(context, _home, _squad);
    }
    public override void _Process(double delta)
    {
        if (_pumping || _sequence is not { } sequence || _context is not { } context || _session is not { HasState: true } session) return;
        _pumping = true;
        try
        {
            if (!IsVisibleInTree() || !session.Connected || !ReferenceEquals(session, context.Session) || session.ViewedCity.Id != sequence.Plan.City) sequence.Cancel("View or connection changed.");
            var intent = sequence.Next(session.Snapshot);
            if (intent is not null)
            {
                if (context.EditReason.Length == 0 && ReferenceEquals(session, _session))
                {
                    long previous = session.ObservedCommand?.Sequence ?? 0;
                    context.Submit(intent);
                    if (ReferenceEquals(session, _session) && session.ObservedCommand is { } issued && issued.Sequence > previous) sequence.Submitted(issued);
                    else sequence.SubmissionFailed();
                }
                else sequence.SubmissionFailed();
            }
            if (sequence.Finished) { _review = null; _copyScroll.Hide(); }
            if (sequence.Finished || sequence.Stopped.Length > 0 && sequence.PendingMember == 0) SetProcess(false);
            Refresh(context, _home, _squad); _submitted();
        }
        finally { _pumping = false; }
    }
    public override void _ExitTree()
    {
        Cancel(); if (_session is not null && _ack is not null) _session.CommandAcknowledged -= _ack;
        _halls.ItemSelected -= HallChanged; _session = null; _ack = null;
    }
}
