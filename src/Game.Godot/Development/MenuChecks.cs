using Godot;

namespace Game.GodotClient;

public partial class GameApplication
{
    private async Task CheckMenuPresentation()
    {
        ReturnToMenu(); RefreshUi(true);
        await GameplayCapture("main-menu");
        Require(_sessionMenu.MainButtons.GetChildren().OfType<Button>().Count(b => b.IsVisibleInTree()) == 4, "four visible primary buttons");
        Press(_sessionMenu.MultiplayerButton); await GameplayCapture("multiplayer-menu");
        Require(_steamPanel.IsVisibleInTree() && _sessionMenu.Endpoint.IsVisibleInTree(), "LAN and Steam entry points in multiplayer submenu");
        ReturnToMenu(); Press(_sessionMenu.SettingsButton); await GameplayCapture("menu-settings");
        Require(_settings.Visible, "menu settings route"); _settings.Close();
        BeginSolo();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await GameplayCapture("doctrine-offers");
        Require(_sessionMenu.StartButton.Disabled, "unselected match cannot start");
        foreach (DoctrineCard card in _sessionMenu.Doctrines.GetChildren())
            Require(GetViewport().GetVisibleRect().Encloses(card.GetGlobalRect()), "portrait doctrine card fits viewport");
        Require(GetViewport().GetVisibleRect().Encloses(_sessionMenu.StartButton.GetGlobalRect()), "Start Match fits viewport");
        Require(GetViewport().GetVisibleRect().Encloses(_sessionMenu.BackButton.GetGlobalRect()), "setup Back fits viewport");
        var selected = (DoctrineCard)_sessionMenu.Doctrines.GetChild(0);
        Click(selected.ChooseButton.GetGlobalRect().GetCenter());
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Require(selected.ChooseButton.ButtonPressed && !_sessionMenu.StartButton.Disabled, "native doctrine click highlights selection and enables start");
        await GameplayCapture("doctrine-selected");
        ReturnToMenu();
    }
}
