// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

/// <summary>Six retained inspection targets in Core's front/rear order. The workspace owns actions.</summary>
public partial class DeploymentPanel : VBoxContainer
{
    private HudSkin _skin = null!;
    private Label _direction = null!, _timing = null!;
    private readonly Dictionary<string, Button> _buttons = [];
    public IReadOnlyDictionary<string, Button> HomeButtons => _buttons;
    internal IReadOnlyList<ArmyHomeView> Homes { get; private set; } = [];
    public int SelectedPlot { get; private set; } = -1;
    public int SelectedSquad { get; private set; }
    public event Action<int>? HomeSelected;
    public void Initialize(HudSkin skin)
    {
        _skin = skin; AddThemeConstantOverride("separation", 6);
        _direction = WrapLabel("Enemy approach ↓ · Front 1–3 / Rear 4–6 · Keep behind");
        _direction.AddThemeFontSizeOverride("font_size", 14); AddChild(_direction);
        var formation = new GridContainer { Columns = 3, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        formation.AddThemeConstantOverride("v_separation", 8); formation.AddThemeConstantOverride("h_separation", 8); AddChild(formation);
        for (int plot = 9; plot < 15; plot++)
        {
            int home = plot;
            var button = new Button { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new(156, 96),
                SizeFlagsHorizontal = SizeFlags.ExpandFill, ExpandIcon = true, ToggleMode = true };
            button.AddThemeFontSizeOverride("font_size", 14); button.AddThemeConstantOverride("icon_max_width", 34);
            button.Pressed += () => { if (button.IsVisibleInTree()) HomeSelected?.Invoke(home); };
            formation.AddChild(button); _buttons.Add("Home" + plot, button);
        }
        _timing = WrapLabel(); _timing.AddThemeFontSizeOverride("font_size", 14); AddChild(_timing);
    }
    public void Refresh(GameplayContext context, int selectedHome, int squad)
    {
        SelectedPlot = selectedHome; SelectedSquad = squad;
        Homes = ArmyDeploymentView.Homes(context.State, context.City.Id, squad);
        bool planning = context.State.Phase is Phase.Building or Phase.Preparation;
        _timing.Text = context.State.Phase == Phase.Preparation ? "Final-income Food projection · final Ready pays upkeep and starts combat."
            : context.State.Phase == Phase.Building ? "Stock-now Food projection · later income can change who fights."
            : "Last accepted battle · no next-wave funding prediction.";
        foreach (var home in Homes)
        {
            var button = _buttons["Home" + home.Home.PlotId];
            bool current = squad > 0 && home.Members.Any(u => u.SquadId == squad);
            string participation = home.Members.Length == 0 ? "" : $"{home.Participating} {(planning ? "projected to fight" : "last fought")}"
                + (home.Members.Length > home.Participating ? $" · {home.Members.Length - home.Participating} sit out" : "")
                + (home.Wounded > 0 ? $" · {home.Wounded} wounded" : "");
            button.Text = $"Home {home.Home.Number} · {home.Occupancy}\n{participation}"
                + (current ? "\nSelected squad" : squad > 0 && home.CanDeploy ? "\nLegal destination" : "");
            button.TooltipText = home.Restriction.Length > 0 ? home.Restriction : "Select this home; placement and management appear beside the board.";
            button.Icon = home.Members.FirstOrDefault() is { } unit ? _skin.UnitSprite(unit) : _skin.BuildingSprite(home.Home.Plot.Type);
            button.SetPressedNoSignal(selectedHome == home.Home.PlotId);
            var style = new StyleBoxFlat { BgColor = new Color("142825"), BorderColor = new Color(selectedHome == home.Home.PlotId ? "efd478" : current ? "a8b9df" : home.CanDeploy ? "85dfc2" : "59646a"),
                BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
                ContentMarginLeft = 6, ContentMarginRight = 6, ContentMarginTop = 6, ContentMarginBottom = 6 };
            foreach (string state in new[] { "normal", "hover", "pressed", "disabled" }) button.AddThemeStyleboxOverride(state, style);
        }
    }
    public override void _ExitTree() => HomeSelected = null;
}
