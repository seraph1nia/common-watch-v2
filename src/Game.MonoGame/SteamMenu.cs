using Game.Core;
using Gum.Forms.Controls;
using MonoGameGum;

namespace Game.MonoGameClient;

public sealed partial class WatchGame
{
    readonly FacepunchPlatform steamPlatform = new();
    SteamLobbyService steamLobbies = null!;
    Button steamHostButton = null!, steamJoinButton = null!, steamInviteButton = null!, acceptInvite = null!, declineInvite = null!;
    TextBox steamTarget = null!;
    bool steamLaunchRead;
    string publishedSteamPhase = "";
    void CreateSteamMenu(Func<string, Action, Button> add)
    {
        steamLobbies = new SteamLobbyService(steamPlatform);
        steamLobbies.AdmittedLobby += (host, lobby, identity, match) =>
        {
            session = new MonoSession();
            try
            {
                if (host) { session.HostSteam(identity, lobby.Contains); Console.WriteLine($"MONOGAME_STEAM_LOBBY {lobby.Id}"); publishedSteamPhase = "";
                    steamLobbies.Publish(session.Snapshot.MatchId, false);
                    if (Arg("--steam-invite") is { } recipient && ulong.TryParse(recipient, out ulong friend))
                    {
                        if (!steamPlatform.InviteFriend(lobby.Id, friend)) throw new IOException("Steam friend invitation was refused.");
                        Console.WriteLine("MONOGAME_STEAM_INVITE_SUBMITTED");
                    } }
                else session.JoinSteam(identity, lobby.Id, steamPlatform.AppId, match, SessionDataPath);
                UpdateUi();
            }
            catch { session.Dispose(); session = null!; throw; }
        };
        steamLobbies.HostDeparted += ReturnToMenu;
        steamHostButton = add("Host private Steam lobby", () => { if (session is null) steamLobbies.BeginHost(Environment.TickCount64); UpdateUi(); });
        steamTarget = new TextBox { Width = 530, Height = 30, Text = "Steam lobby ID" }; sessionMenu.AddChild(steamTarget);
        steamJoinButton = add("Join Steam lobby", () => { if (session is null && ulong.TryParse(steamTarget.Text, out ulong id)) steamLobbies.BeginJoin(id, Environment.TickCount64); });
        steamInviteButton = add("Invite Steam friends", steamLobbies.Invite);
        acceptInvite = add("Accept invitation and leave current game", () =>
        {
            ulong target = steamLobbies.AcceptInvitation();
            if (target == 0) return;
            ReturnToMenu(); steamLobbies.BeginJoin(target, Environment.TickCount64);
        });
        declineInvite = add("Decline invitation", steamLobbies.DeclineInvitation);
        if (arguments.Contains("--steam") || arguments.Contains("--steam-host") || Arg("--steam-join") is not null || SteamLobbyRules.InvitationTargets(arguments).Length != 0) steamLobbies.Ensure();
    }
    void ProcessSteam()
    {
        if (steamLobbies is null) return;
        try
        {
            steamLobbies.Poll(Environment.TickCount64);
            if (!steamLaunchRead && steamPlatform.Ready)
            {
                steamLaunchRead = true;
                if (arguments.Contains("--steam-host")) steamLobbies.BeginHost(Environment.TickCount64);
                else if (Arg("--steam-join") is { } target && ulong.TryParse(target, out ulong lobby)) steamLobbies.BeginJoin(lobby, Environment.TickCount64);
                else if (SteamLobbyRules.InvitationTargets(arguments).FirstOrDefault() is > 0 and var invitation) steamLobbies.BeginJoin(invitation, Environment.TickCount64);
            }
            if (steamLobbies.Invitation != 0 && session is null)
            {
                ulong target = steamLobbies.AcceptInvitation();
                if (target != 0) steamLobbies.BeginJoin(target, Environment.TickCount64);
            }
            if (session is { HasState: true, IsSteam: true, IsHost: true })
            {
                string phase = session.Snapshot.Phase == Phase.Lobby ? "lobby" : "in-progress";
                if (phase != publishedSteamPhase) { steamLobbies.Publish(session.Snapshot.MatchId, phase != "lobby"); publishedSteamPhase = phase; }
            }
        }
        catch (Exception) { ReturnToMenu(); }
    }
    void RefreshSteamMenu()
    {
        steamHostButton.Visual.Visible = steamJoinButton.Visual.Visible = steamTarget.Visual.Visible = session is null;
        steamHostButton.IsEnabled = steamJoinButton.IsEnabled = session is null;
        steamInviteButton.Visual.Visible = steamLobbies.Hosting;
        acceptInvite.Visual.Visible = declineInvite.Visual.Visible = steamLobbies.Invitation != 0;
        if (steamLobbies.Invitation != 0) { sessionMenu.Visual.Visible = true; sessionMenu.IsEnabled = true; }
        lobbyRoster.Text += "\n" + steamLobbies.Feedback;
    }
}
