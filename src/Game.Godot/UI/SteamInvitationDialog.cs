using Godot;

namespace Game.GodotClient;

public partial class SteamInvitationDialog : ConfirmationDialog
{
    private Action? _accept, _decline;
    public override void _Ready()
    {
        DialogText = "Leave this session and join your friend's game?";
        Title = "Steam invitation";
        Confirmed += Accept; Canceled += Decline;
    }
    public void Ask(Action accept, Action decline) { _accept = accept; _decline = decline; PopupCentered(); }
    public void Clear() { Hide(); _accept = _decline = null; }
    private void Accept() { var callback = _accept; Clear(); callback?.Invoke(); }
    private void Decline() { var callback = _decline; Clear(); callback?.Invoke(); }
    public override void _ExitTree() { Confirmed -= Accept; Canceled -= Decline; _accept = _decline = null; }
}
