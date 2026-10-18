using MonoGameGum;
using Game.Core;
using Game.Shared;
using Gum.Forms.Controls;
using Label = Gum.Forms.Controls.Label;

namespace Game.MonoGameClient;

public sealed partial class WatchGame
{
    StackPanel sessionMenu = null!;
    TextBox endpointInput = null!;
    Button hostButton = null!, joinButton = null!, freshJoinButton = null!;
    Label lobbyRoster = null!;
    Button soloButton = null!, startButton = null!;
    readonly List<Button> doctrineButtons = [];

    void CreateSessionMenu()
    {
        sessionMenu = new StackPanel { Width = 550, X = 70, Y = 110 };
        sessionMenu.AddToRoot();
        lobbyRoster = new Label { Width = 530, Height = 110 };
        lobbyRoster.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        sessionMenu.AddChild(lobbyRoster);
        Button MenuButton(string text, Action action)
        {
            var button = new Button { Text = text, Width = 530, Height = 30 };
            button.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
            button.Click += (_, _) => action(); sessionMenu.AddChild(button); return button;
        }
        soloButton = MenuButton("Single player", BeginSolo);
        hostButton = MenuButton("Host LAN", () => BeginHost(int.Parse(Arg("--port") ?? "27026")));
        endpointInput = new TextBox { Text = "127.0.0.1:27026", Width = 530, Height = 32 }; sessionMenu.AddChild(endpointInput);
        joinButton = MenuButton("Join / reconnect LAN", () => BeginJoin(endpointInput.Text));
        freshJoinButton = MenuButton("Join LAN as new player (forget resume)", () =>
        {
            string file = Path.Combine(SessionDataPath, "resume.json");
            if (File.Exists(file)) File.Delete(file);
            BeginJoin(endpointInput.Text ?? "127.0.0.1:27026");
        });
        foreach (var doctrine in RunCatalog.Doctrines)
        {
            string id = doctrine.Id;
            doctrineButtons.Add(MenuButton(doctrine.Name, () => { session.Send("doctrine:" + id); UpdateUi(); }));
        }
        startButton = MenuButton("Start match", () => { session.Start(); UpdateUi(); });
        MenuButton("Return to main menu", ReturnToMenu);
        MenuButton("Quit", Exit);
        CreateSteamMenu(MenuButton);
    }
    void BeginSolo() => ReplaceSession(new SoloSession(matchId: Guid.NewGuid().ToString("N")));
    void ReturnToMenu()
    {
        totalAnimatedHits = 0; battle = null; finishingCombat = false; animationProfile = null; if (battlePanel is not null) battlePanel.Visual.Visible = false;
        title.X = 24; title.Width = 800; motion.Clear();
        session?.Dispose(); session = null!; steamLobbies?.Leave(); publishedSteamPhase = "";
        motion.Clear(); checkpointFrozen = false; ResetCamera(); UpdateUi();
    }
    bool RefreshSessionMenu()
    {
        bool menu = session is null || !session.HasState || session.Snapshot.Phase == Phase.Lobby;
        sessionMenu.Visual.Visible = menu && !settingsMenu.IsOpen; sessionMenu.IsEnabled = menu;
        freshJoinButton.Visual.Visible = soloButton.Visual.Visible = hostButton.Visual.Visible = joinButton.Visual.Visible = endpointInput.Visual.Visible = session is null;
        soloButton.IsEnabled = session is null;
        foreach (var button in doctrineButtons) { button.Visual.Visible = session is { HasState: true }; button.IsEnabled = session is { Connected: true, HasState: true }; }
        startButton.Visual.Visible = startButton.IsEnabled = session is { HasState: true, IsHost: true, Connected: true };
        lobbyRoster.Text = session is null ? "THE COMMON WATCH\nSingle player or LAN • 1–4 players" : !session.HasState ? session.Status : $"{session.Status} • {4 - session.Snapshot.Players.Length} slots\n" + string.Join("\n", session.Snapshot.Players.Select(p => $"Player {p.Id}{(p.Id == 1 ? " [host]" : "")}{(p.Id == session.PlayerId ? " [you]" : "")}: {RunCatalog.Doctrine(p.Run.Doctrine).Name} {(p.Connected ? "" : "offline")}"));
        RefreshSteamMenu();
        if (!menu) return false;
        title.Text = "THE COMMON WATCH / MonoGame + Gum"; resources.Text = ""; status.Text = "Main menu / Pregame";
        footer.Text = ""; sidebar.Visual.Visible = false; sidebar.IsEnabled = false; combatRoster.Visible = false;
        foreach (var panel in rewardPanels) panel.Visual.Visible = false;
        foreach (var button in rewardButtons) button.IsEnabled = false;
        settingsMenu.Layout(Window.ClientBounds.Width, Window.ClientBounds.Height);
        return true;
    }
    string? Arg(string name) { int index = Array.IndexOf(arguments, name); return index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : null; }
    string SessionDataPath => Arg("--data-path") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "common-watch-v2", "monogame");
    void BeginHost(int port)
    {
        ReturnToMenu(); session = new MonoSession();
        try { session.Host(port); UpdateUi(); }
        catch (Exception e) { ReturnToMenu(); lobbyRoster.Text = e.Message; }
    }
    void BeginJoin(string endpoint)
    {
        ReturnToMenu(); session = new MonoSession();
        try { var parts = endpoint.Split(':'); session.Join(parts[0], parts.Length == 2 ? int.Parse(parts[1]) : 27026, SessionDataPath, int.Parse(Arg("--core-version") ?? WireJson.ProtocolVersion.ToString())); UpdateUi(); }
        catch (Exception e) { ReturnToMenu(); lobbyRoster.Text = e.Message; }
    }
    void ReconnectCurrent()
    {
        if (session is { IsHost: true }) return;
        ulong lobby = steamLobbies.Lobby?.Id ?? 0;
        if (lobby != 0) { ReturnToMenu(); steamLobbies.BeginJoin(lobby, Environment.TickCount64); }
        else BeginJoin(Arg("--join") ?? endpointInput.Text ?? "127.0.0.1:27026");
    }
    void SmokeSessionLifecycle()
    {
        void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        ReturnToMenu(); Check(session is null && sessionMenu.Visual.Visible, "menu owns no authority");
        soloButton.PerformClick(); var old = session!; string match = old.Snapshot.MatchId;
        Check(old.Snapshot.Phase == Phase.Lobby, "pregame selection");
        doctrineButtons[1].PerformClick(); Check(old.City.Run.Doctrine == RunCatalog.Doctrines[1].Id, "doctrine UI");
        startButton.PerformClick(); Check(old.Snapshot.Phase == Phase.Building, "start UI");
        ReturnToMenu(); soloButton.PerformClick();
        Check(session!.Snapshot.MatchId != match && session.City.Run.Doctrine == "warden", "replacement isolation");
        try { old.Send("ready"); throw new InvalidOperationException("Disposed session accepted input"); }
        catch (ObjectDisposedException) { }
        ReplaceSession(ScenarioRunner.CreateAt("combat")); ReturnToMenu(); BeginSolo();
        Check(session.Snapshot.Phase == Phase.Lobby && motion.Events.Count == 0, "combat cleanup");
        Console.WriteLine("MONOGAME_SESSION_PASS menu doctrine start replacement cleanup combat-leave offline");
    }
}
