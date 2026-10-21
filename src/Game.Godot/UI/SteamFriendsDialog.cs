using Godot;

namespace Game.GodotClient;

/// <summary>Explicit invitation selection, independent of Steam overlay injection.</summary>
public partial class SteamFriendsDialog : AcceptDialog
{
    private Func<SteamFriendList> _load = null!;
    private Func<ulong, SteamFriend, SteamInviteResult> _send = null!;
    private SteamFriendList _data = new(0, [], "");
    private Control? _invoker;
    internal ItemList Friends { get; private set; } = null!;
    internal Button InviteButton { get; private set; } = null!;
    internal Button RefreshButton { get; private set; } = null!;
    internal Label Status { get; private set; } = null!;
    private LineEdit _lobby = null!;
    private Button _copy = null!;

    public void Initialize(HudSkin skin, Func<SteamFriendList> load, Func<ulong, SteamFriend, SteamInviteResult> send)
    {
        _load = load; _send = send;
        Theme = (Theme)skin.Theme.Duplicate();
        if (skin.Illustrated) Theme.SetStylebox("panel", "AcceptDialog", skin.Theme.GetStylebox("panel", "ForestModal"));
        Title = "Invite Steam friends"; Exclusive = true;
        DialogText = ""; GetOkButton().Text = "Close";
        var body = new VBoxContainer { CustomMinimumSize = new(520, 0) }; AddChild(body);
        body.AddThemeConstantOverride("separation", 10);
        // A wrapping label needs a width before PopupCentered calculates the
        // Window minimum; otherwise its first one-character layout grows it tall.
        Status = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new(500, 68) };
        body.AddChild(Status);
        Friends = new ItemList { CustomMinimumSize = new(0, 240), SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            AllowReselect = true, AutoHeight = false };
        body.AddChild(Friends);
        Friends.ItemSelected += _ => InviteButton.Disabled = false;
        var actions = new HBoxContainer(); body.AddChild(actions);
        InviteButton = skin.Button("Invite selected", Send, "ForestPrimaryButton");
        InviteButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; actions.AddChild(InviteButton);
        RefreshButton = skin.Button("Refresh friends", RefreshFriends); actions.AddChild(RefreshButton);
        body.AddChild(new Label { Text = "Or share this ID for Join Steam lobby:" });
        var fallback = new HBoxContainer(); body.AddChild(fallback);
        _lobby = new LineEdit { Editable = false, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        fallback.AddChild(_lobby);
        _copy = skin.Button("Copy ID", () => { DisplayServer.ClipboardSet(_lobby.Text); Status.Text = "Lobby ID copied. Share it with your friend for Join Steam lobby."; });
        fallback.AddChild(_copy);
        CloseRequested += Close; Confirmed += RestoreFocus; Canceled += RestoreFocus;
    }
    public void Open()
    {
        _invoker = GetTree().Root.GuiGetFocusOwner();
        // Refresh is explicit; selection never submits an invitation.
        RefreshFriends(); PopupCentered(new(560, _data.Friends.Count == 0 ? 300 : 540));
        if (Friends.ItemCount > 0) Friends.GrabFocus(); else GetOkButton().GrabFocus();
    }
    private void RefreshFriends()
    {
        double scroll = Friends.GetVScrollBar().Value;
        ulong selected = Friends.GetSelectedItems().FirstOrDefault(-1) is var index && index >= 0
            ? _data.Friends[index].Id : 0;
        _data = _load(); Friends.Clear(); InviteButton.Disabled = true;
        Friends.Visible = _data.Friends.Count > 0;
        foreach (var friend in _data.Friends)
        {
            int item = Friends.AddItem(friend.Name);
            Friends.SetItemTooltip(item, friend.Name + "\n" + friend.Id);
            if (friend.Id == selected) { Friends.Select(item); InviteButton.Disabled = false; }
        }
        Status.Text = _data.Status; _lobby.Text = _data.Lobby == 0 ? "No active Steam lobby" : _data.Lobby.ToString();
        _copy.Disabled = _data.Lobby == 0;
        Friends.GetVScrollBar().SetDeferred(Godot.Range.PropertyName.Value, scroll);
    }
    private void Send()
    {
        var selection = Friends.GetSelectedItems();
        if (selection.Length != 1) return;
        var result = _send(_data.Lobby, _data.Friends[selection[0]]);
        Status.Text = result.Status;
        // Each further invitation requires explicit selection again.
        if (result.Submitted) { Friends.DeselectAll(); InviteButton.Disabled = true; }
    }
    public void Close() { Hide(); RestoreFocus(); }
    private void RestoreFocus()
    {
        if (_invoker is { } control && IsInstanceValid(control) && control.IsVisibleInTree()) control.GrabFocus();
        _invoker = null;
    }
    public override void _ExitTree() { CloseRequested -= Close; Confirmed -= RestoreFocus; Canceled -= RestoreFocus; _invoker = null; }
}
