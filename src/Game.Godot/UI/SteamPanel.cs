using Godot;

namespace Game.GodotClient;

public partial class SteamPanel : VBoxContainer
{
    private Button _host = null!, _join = null!, _invite = null!;
    private LineEdit _target = null!;
    public void Initialize(HudSkin skin, Action host, Action<string> join, Action invite)
    {
        _host = skin.Button("Host private Steam lobby", host); AddChild(_host);
        _target = new LineEdit { PlaceholderText = "Steam lobby ID" }; AddChild(_target);
        _join = skin.Button("Join Steam lobby", () => join(_target.Text)); AddChild(_join);
        _invite = skin.Button("Invite Steam friends", invite); AddChild(_invite);
    }
    public void Refresh(bool menu, bool hosting)
    { _host.Visible = _join.Visible = _target.Visible = menu; _invite.Visible = hosting; }
}
