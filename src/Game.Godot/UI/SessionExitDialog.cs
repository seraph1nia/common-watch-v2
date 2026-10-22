using Godot;

namespace Game.GodotClient;

/// <summary>Explains session loss before an explicit leave or exit; cancel retains native focus.</summary>
public partial class SessionExitDialog : ConfirmationDialog
{
    private Action? _confirm;
    private Control? _invoker;

    public void Initialize(HudSkin skin)
    {
        Theme = (Theme)skin.Theme.Duplicate();
        if (skin.Illustrated) Theme.SetStylebox("panel", "AcceptDialog", skin.Theme.GetStylebox("panel", "ForestModal"));
        Exclusive = true;
        DialogAutowrap = true;
        Confirmed += Confirm;
        Canceled += Cancel;
    }

    public void Ask(bool exit, GodotSession? session, Action confirm)
    {
        if (Visible) return;
        _invoker = GetTree().Root.GuiGetFocusOwner();
        _confirm = confirm;
        Title = exit ? "Exit game?" : "Return to main menu?";
        GetOkButton().Text = exit ? "Exit game" : "Return to main menu";
        GetCancelButton().Text = "Cancel";
        string consequences = session switch
        {
            null => "No match is active. Your settings will be kept.",
            { IsNetwork: false } => "Your current single-player match will end. Progress is not saved and this match cannot be resumed.",
            { IsHost: true } => "You are hosting. Leaving will end the match for everyone and disconnect all players. Match progress is not saved and cannot be resumed.",
            _ => "You will disconnect from the current match. The host and other players can continue. You can reconnect only while the host keeps this same session running."
        };
        DialogText = (exit ? "The game will close.\n\n" : "You will leave this session and return to the main menu.\n\n")
            + consequences + (session is null ? "" : "\n\nYour settings will be kept.");
        PopupCentered(new(560, 260));
        GetCancelButton().GrabFocus();
    }

    public void Clear() { Hide(); _confirm = null; _invoker = null; }

    private void Confirm()
    {
        var confirm = _confirm;
        Clear();
        confirm?.Invoke();
    }

    private void Cancel()
    {
        var invoker = _invoker;
        Clear();
        if (IsInstanceValid(invoker) && invoker!.IsVisibleInTree()) invoker.GrabFocus();
    }

    public override void _Input(InputEvent input)
    {
        if (!Visible || !input.IsActionPressed("ui_cancel")) return;
        SetInputAsHandled();
        Cancel();
    }

    public override void _ExitTree()
    {
        Confirmed -= Confirm;
        Canceled -= Cancel;
        _confirm = null; _invoker = null;
    }
}
