using Godot;
using Game.Core;

namespace Game.GodotClient;

/// <summary>Scrollable native lobby/menu; admission and match start stay with the session owner.</summary>
public partial class LobbyPanel : PanelContainer
{
    public VBoxContainer Content { get; private set; } = null!;
    public Label Roster { get; private set; } = null!;
    public LineEdit Endpoint { get; private set; } = null!;
    public Button SoloButton { get; private set; } = null!;
    public Button StartButton { get; private set; } = null!;
    public VBoxContainer Doctrines { get; private set; } = null!;
    private Button _host = null!, _join = null!, _fresh = null!;
    public event Action? SoloRequested, HostRequested, StartRequested, MenuRequested, QuitRequested;
    public event Action<string, bool>? JoinRequested;
    public event Action<string>? DoctrineSelected;

    public void Initialize(HudSkin skin)
    {
        Theme = skin.Theme;
        SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        OffsetLeft = -320; OffsetRight = 320; OffsetTop = -340; OffsetBottom = 340;
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        AddChild(Margin(scroll, 24));
        Content = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; scroll.AddChild(Content);
        Content.AddChild(new Label { Text = "THE COMMON WATCH" });
        Roster = WrapLabel(); Content.AddChild(Roster);
        SoloButton = skin.Button("Single player", () => SoloRequested?.Invoke()); Content.AddChild(SoloButton);
        _host = skin.Button("Host LAN", () => HostRequested?.Invoke()); Content.AddChild(_host);
        Endpoint = new LineEdit { Text = "127.0.0.1:27025", PlaceholderText = "host:port" }; Content.AddChild(Endpoint);
        _join = skin.Button("Join / reconnect LAN", () => JoinRequested?.Invoke(Endpoint.Text, false)); Content.AddChild(_join);
        _fresh = skin.Button("Join LAN as new player (forget resume)", () => JoinRequested?.Invoke(Endpoint.Text, true)); Content.AddChild(_fresh);
        Doctrines = new VBoxContainer(); Content.AddChild(Doctrines);
        foreach (var doctrine in RunCatalog.Doctrines)
        {
            string id = doctrine.Id;
            var button = skin.Button(doctrine.Name, () => DoctrineSelected?.Invoke(id));
            button.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            Doctrines.AddChild(button);
        }
        StartButton = skin.Button("Start match", () => StartRequested?.Invoke()); Content.AddChild(StartButton);
        Content.AddChild(skin.Button("Return to main menu", () => MenuRequested?.Invoke()));
        Content.AddChild(skin.Button("Quit", () => QuitRequested?.Invoke()));
    }
    public void Refresh(GodotSession? session)
    {
        Visible = session is null || !session.HasState || session.Snapshot.Phase == Phase.Lobby;
        _fresh.Visible = SoloButton.Visible = _host.Visible = _join.Visible = Endpoint.Visible = session is null;
        Doctrines.Visible = session is { HasState: true, Connected: true };
        StartButton.Visible = session is { HasState: true, IsHost: true };
        StartButton.Disabled = session is not { Connected: true, Pending: false };
        if (session is { HasState: true })
        {
            var view = RunPresentation.Project(session.Snapshot, session.City, session.PlayerId, session.Connected && !session.Pending, true);
            for (int i = 0; i < view.Cards.Length; i++)
            {
                var card = view.Cards[i]; var button = (Button)Doctrines.GetChild(i);
                button.Text = (card.Selected ? "✓ " : "") + card.Title + " — " + card.Subtitle + "\n" + card.Description + "\n" + card.Footer;
                button.Disabled = !card.Enabled;
            }
        }
        Roster.Text = session is null ? "Single player or LAN • 1–4 players" : !session.HasState ? session.Status :
            $"{session.Status} • {4 - session.Snapshot.Players.Length} slots\n" + string.Join("\n", session.Snapshot.Players.Select(p => $"Player {p.Id}{(p.Id == 1 ? " [host]" : "")}{(p.Id == session.PlayerId ? " [you]" : "")}: {RunCatalog.Doctrine(p.Run.Doctrine).Name} {(p.Connected ? "" : "offline")}"));
    }
}
