using Godot;

namespace Game.GodotClient;

public partial class CitySelector : OptionButton
{
    private GodotSession _session = null!;
    public event Action? CityChanged;
    public void Initialize() => ItemSelected += _ => { _session.ViewCity(GetSelectedId()); CityChanged?.Invoke(); };
    public void Refresh(GodotSession session)
    {
        _session = session;
        NativeOptions.Update(this, session.Snapshot.Players.Select(p => (p.Id,
            $"P{p.Id} {(p.Id == session.PlayerId ? "Your city" : "Ally (read-only)")}{(p.Eliminated ? " · fallen" : "")}{(!p.Connected ? " · away" : "")}{(p.Ready ? " · ready" : "")}")));
        int index = GetItemIndex(session.ViewedCity.Id); if (index >= 0) Select(index);
    }
}
