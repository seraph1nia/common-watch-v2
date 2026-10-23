using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

/// <summary>Read-only presentation context. Commands always use the session's authenticated player.</summary>
public sealed class GameplayContext(GodotSession session, Action submitted)
{
    private readonly int _viewedCity = session.HasState ? session.ViewedCity.Id : 0;
    public GodotSession Session => session;
    public MatchSnapshot State => session.Snapshot;
    public CityState City => session.ViewedCity;
    public string EditReason => !session.Connected ? "Disconnected — reconnect to act."
        : !session.HasState ? "Waiting for match state."
        : session.ViewedCity.Id != _viewedCity ? "Inspection changed — select an action again."
        : session.Pending ? "Waiting for authority acknowledgment."
        : City.Id != session.PlayerId ? "Allied city — read-only."
        : City.Eliminated ? "Fallen city — read-only."
        : !City.Connected ? "City disconnected — read-only."
        : State.Paused ? "Match paused — resume to edit."
        : State.Phase is not (Phase.Building or Phase.Preparation) ? "Available during preparation."
        : City.Ready ? "Unready before editing." : "";
    public void Submit(GameplayIntent intent)
    {
        // A stale button or a foreign inspector must never become an edit of our city.
        if (EditReason.Length != 0) return;
        session.Submit(intent); submitted();
    }
    public string CostReason(ResourceCost cost) => City.Resources.TryPay(cost, out _) ? "" : string.Join("; ", Enum.GetValues<Resource>()
        .Where(r => City.Resources.Amount(r) < cost.Amount(r)).Select(r => $"Need {cost.Amount(r) - City.Resources.Amount(r)} more {r} ({Producer(r)})."));
    public static string Producer(Resource resource) => resource switch
    {
        Resource.Gold => "Gold Mine / base production / Market", Resource.Wood => "Lumbermill", Resource.Food => "Farm",
        Resource.Stone => "Stonecutter", Resource.Metal => "Metal Mine", _ => "Weaver"
    };
    public string Block(ResourceCost? cost = null, string reason = "") => EditReason.Length > 0 ? EditReason
        : reason.Length > 0 ? reason : cost is { } quote ? CostReason(quote) : "";
    public ArmyConfiguration Army => session.ArmyConfiguration;
    public ArmyAssignment? Destination(UnitType type, int squad = 0, int except = 0)
        => City.Army is null || State.Arena is null ? null : Army.Find(City.Soldiers.Where(u => u.Id != except), City.Army.PurchasedHomes,
            plots: City.Army.Homes.Select(h => h.Plot).ToArray(), type: type, squadId: squad);
    public static string BuildingName(Building type) => type == Building.Mine ? "Gold Mine" : System.Text.RegularExpressions.Regex.Replace(type.ToString(), "(?<=[a-z])([A-Z])", " $1");
    public static string Cost(ResourceCost cost) => RunPresentation.Resources(cost) is { Length: > 0 } text ? text : "None";
}

/// <summary>Native buttons retained by stable action key. No simulation or dialog lifecycle here.</summary>
public partial class ActionList : BoxContainer
{
    private readonly Dictionary<string, Button> _buttons = [];
    private readonly HashSet<string> _used = [];
    private object? _frameKey;
    public string SharedRestriction { get; set; } = "";
    public bool ExplainRestrictions { get; set; } = true;
    public string DefaultVariation { get; set; } = "";
    public IReadOnlyDictionary<string, Button> Buttons => _buttons;
    public ActionList() { Vertical = true; }
    public void Begin(object? frameKey = null)
    {
        if (!Equals(frameKey, _frameKey))
        {
            foreach (var old in _buttons.Values) { old.Hide(); RemoveChild(old); old.QueueFree(); }
            _buttons.Clear(); _frameKey = frameKey;
        }
        _used.Clear();
    }
    public Button Item(HudSkin skin, string key, string text, Action action, string unavailable = "", string variation = "")
    {
        _used.Add(key);
        if (!_buttons.TryGetValue(key, out var button))
        {
            object? frame = _frameKey; Button? created = null;
            created = skin.Button(text, () =>
            {
                if (IsInstanceValid(created) && created!.IsVisibleInTree() && !created.Disabled && Equals(frame, _frameKey)) action();
            }, variation.Length > 0 ? variation : DefaultVariation);
            button = created; button.Name = key;
            button.AutowrapMode = TextServer.AutowrapMode.WordSmart; button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            AddChild(button); _buttons.Add(key, button);
        }
        button.Visible = true; button.Text = text + (ExplainRestrictions && unavailable.Length > 0 && unavailable != SharedRestriction ? "\n" + unavailable : "");
        button.Disabled = unavailable.Length > 0; button.TooltipText = unavailable;
        return button;
    }
    public void End() { foreach (var pair in _buttons) pair.Value.Visible = _used.Contains(pair.Key); }
}
