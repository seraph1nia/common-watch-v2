using Godot;
using Game.Core;
using Game.Shared;

namespace Game.GodotClient;

/// <summary>Main navigation, multiplayer admission and personal match setup; lifetime belongs to the session owner.</summary>
public partial class LobbyPanel : PanelContainer
{
    public VBoxContainer MultiplayerContent { get; private set; } = null!;
    public Label MultiplayerStatus { get; private set; } = null!;
    public Label Roster { get; private set; } = null!;
    public LineEdit Endpoint { get; private set; } = null!;
    public Button SoloButton { get; private set; } = null!;
    public Button MultiplayerButton { get; private set; } = null!;
    public Button SettingsButton { get; private set; } = null!;
    public Button BackButton { get; private set; } = null!;
    public Button? ContinueButton { get; private set; }
    public Button? ReturnToMenuButton { get; private set; }
    public Button QuitButton { get; private set; } = null!;
    internal PanelContainer MenuCard { get; private set; } = null!;
    public Button StartButton { get; private set; } = null!;
    public HBoxContainer Doctrines { get; private set; } = null!;
    public VBoxContainer LobbyActions { get; private set; } = null!;
    private Label _livePhase = null!;
    private Label _title = null!;
    private Label _selectionHint = null!;
    public VBoxContainer MainButtons { get; private set; } = null!;
    public IReadOnlyList<string> Offers => _offers;
    private readonly Dictionary<string, string> _choices = [];
    private string _key = "";
    private string[] _offers = [];
    private readonly List<DoctrineCard> _cards = [];
    private HudSkin _skin = null!;
    private VBoxContainer _setup = null!;
    private bool _multiplayer;
    public bool MultiplayerOpen => _multiplayer;
    public event Action? SoloRequested, HostRequested, StartRequested, MenuRequested, QuitRequested, SettingsRequested;
    public event Action? ContinueRequested, NavigationChanged;
    public event Action<string, bool>? JoinRequested;
    public event Action<string>? DoctrineSelected;

    public void ShowMain() { _multiplayer = false; _key = ""; }
    internal void FocusDefault()
    {
        if (ContinueButton is { } resume && resume.IsVisibleInTree()) resume.GrabFocus();
        else if (Doctrines.IsVisibleInTree() && _cards.FirstOrDefault(c => !c.ChooseButton.Disabled) is { } card) card.ChooseButton.GrabFocus();
        else if (Endpoint.IsVisibleInTree()) Endpoint.GrabFocus();
        else if (SoloButton.IsVisibleInTree()) SoloButton.GrabFocus();
        else if (BackButton.IsVisibleInTree()) BackButton.GrabFocus();
    }
    public override void _ExitTree()
    {
        SoloRequested = HostRequested = StartRequested = MenuRequested = QuitRequested = SettingsRequested = null;
        ContinueRequested = NavigationChanged = null; JoinRequested = null; DoctrineSelected = null;
    }
    public void Initialize(HudSkin skin, bool popup = false)
    {
        _skin = skin; Theme = skin.Theme;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        OffsetLeft = 40; OffsetRight = -40; OffsetTop = 24; OffsetBottom = -24;
        if (popup) { OffsetLeft = OffsetRight = OffsetTop = OffsetBottom = 0; MouseFilter = MouseFilterEnum.Stop; }
        var center = new CenterContainer(); AddChild(center);
        MenuCard = new PanelContainer(); center.AddChild(MenuCard);
        var column = new VBoxContainer { CustomMinimumSize = new(410, 0) }; MenuCard.AddChild(column);
        if (popup)
        {
            AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
            skin.Panel(MenuCard);
        }
        else MenuCard.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
        var title = new Label { Text = "THE COMMON WATCH", HorizontalAlignment = HorizontalAlignment.Center }; _title = title; title.AddThemeFontSizeOverride("font_size", 30); column.AddChild(title);
        _livePhase = WrapLabel(); column.AddChild(_livePhase); _livePhase.Hide();
        MainButtons = new VBoxContainer(); column.AddChild(MainButtons);
        if (popup) { ContinueButton = skin.Button("Continue [Esc]", () => ContinueRequested?.Invoke()); MainButtons.AddChild(ContinueButton); }
        SoloButton = skin.Button("Single Player", () => SoloRequested?.Invoke(), "ForestPrimaryButton"); MainButtons.AddChild(SoloButton);
        MultiplayerButton = skin.Button("Multiplayer", () => { _multiplayer = true; Refresh(null); NavigationChanged?.Invoke(); }); MainButtons.AddChild(MultiplayerButton);
        SoloButton.Visible = MultiplayerButton.Visible = !popup;
        SettingsButton = skin.Button("Settings", () => SettingsRequested?.Invoke()); MainButtons.AddChild(SettingsButton);
        if (popup) { ReturnToMenuButton = skin.Button("Return to main menu", () => MenuRequested?.Invoke()); MainButtons.AddChild(ReturnToMenuButton); }
        QuitButton = skin.Button("Exit Game", () => QuitRequested?.Invoke()); MainButtons.AddChild(QuitButton);
        MultiplayerContent = new VBoxContainer(); column.AddChild(MultiplayerContent);
        MultiplayerContent.AddChild(skin.Button("Host LAN", () => HostRequested?.Invoke()));
        Endpoint = new LineEdit { Text = "127.0.0.1:27025", PlaceholderText = "host:port" }; MultiplayerContent.AddChild(Endpoint);
        MultiplayerContent.AddChild(skin.Button("Join / reconnect LAN", () => JoinRequested?.Invoke(Endpoint.Text, false)));
        MultiplayerContent.AddChild(skin.Button("Join LAN as new player (forget resume)", () => JoinRequested?.Invoke(Endpoint.Text, true)));
        MultiplayerStatus = WrapLabel(); MultiplayerStatus.MaxLinesVisible = 3; MultiplayerContent.AddChild(MultiplayerStatus);
        MultiplayerContent.AddChild(skin.Button("Back to main menu", () => MenuRequested?.Invoke()));
        _setup = new VBoxContainer { CustomMinimumSize = new(1040, 0) }; column.AddChild(_setup);
        _setup.AddChild(new Label { Text = "Choose your starting doctrine", HorizontalAlignment = HorizontalAlignment.Center });
        Doctrines = new HBoxContainer(); Doctrines.AddThemeConstantOverride("separation", 18); _setup.AddChild(Doctrines);
        _selectionHint = WrapLabel(); _selectionHint.HorizontalAlignment = HorizontalAlignment.Center; _setup.AddChild(_selectionHint);
        StartButton = skin.Button("Start Match", () => { if (!StartButton.Disabled) StartRequested?.Invoke(); }, "ForestPrimaryButton"); _setup.AddChild(StartButton);
        var footer = new HBoxContainer(); _setup.AddChild(footer);
        Roster = WrapLabel(); Roster.MaxLinesVisible = 1; footer.AddChild(Roster);
        LobbyActions = new VBoxContainer(); footer.AddChild(LobbyActions);
        BackButton = skin.Button("Back to main menu", () => MenuRequested?.Invoke()); _setup.AddChild(BackButton);
        if (!popup) InitializeBackground(skin.MenuBackground);
    }
    public void SetLivePhase(bool live, string notice)
    {
        _livePhase.Text = notice; _livePhase.Visible = notice.Length > 0;
        if (ContinueButton is not null) ContinueButton.Text = live ? "Return to battle [Esc]" : "Continue [Esc]";
    }
    public void ShowError(string message)
    {
        var dialog = new AcceptDialog { Title = "Unable to connect", DialogText = message }; AddChild(dialog);
        dialog.Confirmed += dialog.QueueFree; dialog.CloseRequested += dialog.QueueFree; dialog.PopupCentered();
    }
    public void Refresh(GodotSession? session)
    {
        Visible = session is null || !session.HasState || session.Snapshot.Phase == Phase.Lobby;
        if (!Visible) return;
        _title.Visible = session is null;
        MainButtons.Visible = session is null && !_multiplayer;
        MultiplayerContent.Visible = session is null && _multiplayer;
        _setup.Visible = session is not null;
        Doctrines.Visible = session is { HasState: true };
        StartButton.Visible = session is { HasState: true, IsHost: true };
        if (session is { HasState: true })
        {
            string key = $"{session.Snapshot.MatchId}:{session.PlayerId}";
            var view = RunPresentation.Project(session.Snapshot, session.City, session.PlayerId, session.Connected && !session.Pending, true);
            if (_key != key)
            {
                _key = key;
                if (_choices.Count > 16) _choices.Clear();
                // Eligibility is independent of temporary connection/pending/paused state.
                var eligible = RunPresentation.Project(session.Snapshot with { Paused = false }, session.City, session.PlayerId, true, true);
                _offers = DoctrineOffers.For(session.Snapshot.MatchId, session.PlayerId, eligible.Cards);
                foreach (Node child in Doctrines.GetChildren()) { Doctrines.RemoveChild(child); child.QueueFree(); }
                _cards.Clear();
                foreach (string id in _offers)
                {
                    var card = new DoctrineCard(); Doctrines.AddChild(card);
                    card.Initialize(_skin, view.Cards.Single(c => c.Id == id), () => { if (key != _key || !Visible || !Doctrines.Visible) return; _choices[key] = id; DoctrineSelected?.Invoke(id); }); _cards.Add(card);
                }
            }
            _choices.TryGetValue(key, out string? selected);
            _selectionHint.Text = selected is null ? "Select one doctrine to begin." : !session.AllDoctrinesChosen ? "Waiting for every connected player to choose a doctrine." : "Doctrine selected · ready to start.";
            for (int i = 0; i < _offers.Length; i++) _cards[i].Refresh(view.Cards.Single(c => c.Id == _offers[i]), selected == _offers[i]);
            // Disabling an enabled native button cancels an in-progress click.
            // Assign only the final eligibility so frame refreshes preserve held input.
            StartButton.Disabled = session is not { Connected: true, Pending: false } || session.Snapshot.Paused || selected is null || session.City.Run.Doctrine != selected || !session.AllDoctrinesChosen;
            StartButton.TooltipText = !session.AllDoctrinesChosen ? "Waiting for every connected player to choose a doctrine." : "Start the match with these personal doctrines.";
        }
        else StartButton.Disabled = true;
        Roster.Text = session is null ? "" : !session.HasState ? session.Status :
            string.Join(" · ", session.Snapshot.Players.Select(p => $"P{p.Id}{(p.Id == session.PlayerId ? " [you]" : "")}: {RunCatalog.Doctrine(p.Run.Doctrine).Name}{(p.Connected ? "" : " (offline)")}"));
        Roster.TooltipText = Roster.Text;
    }
}
