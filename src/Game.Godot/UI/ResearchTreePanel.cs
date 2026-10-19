using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

/// <summary>One catalog/inspection/command flow with illustrated and plain presentation.</summary>
public partial class ResearchTreePanel : Control
{
    private HudSkin _skin = null!;
    private GameplayContext _context = null!;
    private Label _summary = null!, _details = null!, _heading = null!;
    private ActionList _nodes = null!, _purchase = null!;
    private readonly Dictionary<string, Button> _sockets = [];
    private Control _canvas = null!, _info = null!;
    private TextureRect _background = null!;
    private TechnologyId _selected;
    public bool Illustrated => _skin.Worldtree is not null;
    public TechnologyId Selected => _selected;
    public Rect2 DetailsArea => _info.GetGlobalRect();
    public IReadOnlyDictionary<string, Button> TechnologyButtons => Illustrated ? _sockets : _nodes.Buttons;
    public IReadOnlyDictionary<string, Button> PurchaseButtons => _purchase.Buttons;

    public void Initialize(HudSkin skin)
    {
        _skin = skin; SizeFlagsHorizontal = SizeFlags.ExpandFill; SizeFlagsVertical = SizeFlags.ExpandFill;
        _heading = WrapLabel("WORLDTREE"); _heading.AddThemeFontSizeOverride("font_size", 23);
        _summary = WrapLabel(); _details = WrapLabel(); _purchase = new ActionList();
        if (skin.Worldtree is { } art)
        {
            MouseFilter = MouseFilterEnum.Ignore;
            _canvas = new Control { MouseFilter = MouseFilterEnum.Ignore }; AddChild(_canvas);
            _background = Decoration(art.Background); _canvas.AddChild(_background);
            _info = new Control { MouseFilter = MouseFilterEnum.Ignore }; _canvas.AddChild(_info);
            var backing = new Panel { MouseFilter = MouseFilterEnum.Ignore };
            backing.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color("102a29ef"), CornerRadiusTopLeft = 36,
                CornerRadiusTopRight = 36, CornerRadiusBottomLeft = 36, CornerRadiusBottomRight = 36 });
            _info.AddChild(backing); backing.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            backing.AnchorLeft = .11f; backing.AnchorRight = .89f; backing.AnchorTop = .12f; backing.AnchorBottom = .88f;
            var frame = Decoration(art.Details); _info.AddChild(frame); frame.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            var body = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            _info.AddChild(body); body.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            body.AnchorLeft = .22f; body.AnchorRight = .82f; body.AnchorTop = .22f; body.AnchorBottom = .74f;
            body.AddThemeConstantOverride("separation", 6);
            _heading.AddThemeFontSizeOverride("font_size", 20); _summary.AddThemeFontSizeOverride("font_size", 14);
            body.AddChild(_heading); body.AddChild(_summary);
            var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
                SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Stop };
            body.AddChild(scroll); scroll.AddChild(_details); body.AddChild(_purchase);
            foreach (var label in new[] { _heading, _summary, _details })
            { label.MouseFilter = MouseFilterEnum.Ignore; label.AddThemeColorOverride("font_color", new Color("f6e5c0")); }
            Resized += LayoutArt;
        }
        else
        {
            var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
            AddChild(scroll); scroll.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            var body = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; scroll.AddChild(body);
            body.AddChild(_heading); body.AddChild(_summary); body.AddChild(_details); body.AddChild(_purchase);
            _nodes = new ActionList(); body.AddChild(_nodes); _info = body;
        }
        ResetSelection();
    }
    private static TextureRect Decoration(Texture2D texture) => new() { Texture = texture,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.Scale,
        MouseFilter = MouseFilterEnum.Ignore };
    private void LayoutArt()
    {
        if (_skin.Worldtree is not { } art) return;
        float scale = Math.Min(Size.X / art.ImageSize.X, Size.Y / art.ImageSize.Y);
        Vector2 imageSize = art.ImageSize * scale;
        _canvas.Position = (Size - imageSize) / 2; _canvas.Size = imageSize; _background.Size = imageSize;
        _info.Position = art.DetailsRect.Position * scale; _info.Size = art.DetailsRect.Size * scale;
        foreach (var socket in art.Sockets)
            if (_sockets.TryGetValue("Tech" + socket.Id, out var button))
            { button.Size = Vector2.One * socket.Diameter * scale; button.Position = socket.Center * scale - button.Size / 2; }
    }
    public void ResetSelection()
    {
        _selected = TechnologyId.None;
        // Session replacement also calls this: never read the outgoing session here.
        _context = null!; _heading.Text = "WORLDTREE"; _details.Text = Introduction;
        _purchase.Begin(); _purchase.End();
        foreach (var button in _sockets.Values.Cast<WorldtreeNode>())
            button.Present(button.ResearchState, false, button.TooltipText);
    }
    private const string Introduction = "Select a node to inspect its effect and requirements.\n\nI · Foundation\nII · Specialization\nIII · Mastery / production\n\nChoose one specialization per class.";
    public void Refresh(GameplayContext context)
    {
        _context = context; var city = context.City;
        _summary.Text = $"P{city.Id} · {city.Research.Points} research points\nProgress {city.Research.Progress}/3";
        if (!Illustrated) _nodes.Begin();
        foreach (var node in context.State.TechnologyCatalog.OrderBy(n => n.Class).ThenBy(n => n.Tier).ThenBy(n => n.Id))
        {
            var id = node.Id; bool owned = city.Research.Has(id);
            var eligibility = city.Technologies.FirstOrDefault(t => t.Id == id);
            string status = owned ? "ACQUIRED" : eligibility?.Available == true ? "AVAILABLE" : "LOCKED";
            string text = $"{node.Name} · {node.Class} / tier {node.Tier} · {node.Cost} points\n{status}: {eligibility?.Reason}\nPrerequisite: {node.Prerequisite}; branch: {node.Branch}";
            void Select() { _selected = id; Refresh(_context); }
            if (_skin.Worldtree is { } art)
            {
                if (!_sockets.TryGetValue("Tech" + id, out var control))
                {
                    var socket = art.Sockets.Single(s => s.Id == id);
                    var button = new WorldtreeNode { Name = "Tech" + id };
                    button.Initialize(art, _skin.Icon(socket.Icon), node.Tier); button.Pressed += Select;
                    _canvas.AddChild(button); _sockets.Add("Tech" + id, button); control = button;
                }
                ((WorldtreeNode)control).Present(owned ? "owned" : eligibility?.Available == true ? "idle" : "locked", _selected == id, text);
            }
            else _nodes.Item(_skin, "Tech" + id, text, Select);
        }
        if (Illustrated) LayoutArt(); else _nodes.End();
        _purchase.Begin();
        if (context.State.TechnologyCatalog.FirstOrDefault(n => n.Id == _selected) is { } selected)
        {
            bool owned = city.Research.Has(_selected);
            var eligibility = city.Technologies.FirstOrDefault(t => t.Id == _selected);
            string reason = owned ? "Already acquired." : eligibility?.Available == true ? "" : eligibility?.Reason ?? "Unavailable.";
            string prerequisite = selected.Prerequisite == TechnologyId.None ? "None" : TechnologyIds.Name(selected.Prerequisite)
                + (city.Research.Has(selected.Prerequisite) ? " (acquired)" : " (required)");
            _heading.Text = selected.Name;
            _details.Text = $"{(owned ? "ACQUIRED" : eligibility?.Available == true ? "AVAILABLE" : "LOCKED")} · {selected.Cost} points\n"
                + $"{selected.Class} · tier {selected.Tier}\nRequires: {prerequisite}\n"
                + (selected.Branch == TechnologyId.None ? "" : $"Exclusive branch: {TechnologyIds.Name(selected.Branch)}\n")
                + (reason.Length > 0 ? reason + "\n" : "")
                + (selected.Production is { } production ? $"Each {GameplayContext.BuildingName(production.Building)}: +{production.ExtraPerProducer} {production.Resource}/production" : UnitInspector.Capabilities(selected.Benefit))
                + (context.EditReason.Length > 0 ? "\n\n" + context.EditReason : "");
            _purchase.Item(_skin, "Purchase", $"Research · {selected.Cost} points", () => _context.Submit(new(GameplayAction.Research, Technology: _selected)), context.Block(reason: reason));
        }
        else
        {
            _heading.Text = "WORLDTREE";
            _details.Text = Introduction + (context.EditReason.Length > 0 ? "\n\n" + context.EditReason : "");
        }
        _purchase.End();
        if (Illustrated && _purchase.Buttons.TryGetValue("Purchase", out var action))
            action.Visible = _selected != TechnologyId.None && !city.Research.Has(_selected)
                && city.Technologies.Any(t => t.Id == _selected && t.Available);
        if (Illustrated) LinkFocus();
    }
    private void LinkFocus()
    {
        if (_skin.Worldtree is not { } art) return;
        foreach (var socket in art.Sockets)
        {
            var button = _sockets["Tech" + socket.Id];
            NodePath Neighbor(Vector2 direction)
            {
                var target = art.Sockets.Where(s => s.Id != socket.Id && (s.Center - socket.Center).Dot(direction) > 1)
                    .OrderBy(s => (s.Center - socket.Center).LengthSquared() / (s.Center - socket.Center).Normalized().Dot(direction)).FirstOrDefault();
                if (target is null && direction == Vector2.Right && _purchase.Buttons.TryGetValue("Purchase", out var purchase)
                    && purchase.Visible && !purchase.Disabled) return button.GetPathTo(purchase);
                return button.GetPathTo(target is null ? button : _sockets["Tech" + target.Id]);
            }
            button.FocusNeighborLeft = Neighbor(Vector2.Left); button.FocusNeighborRight = Neighbor(Vector2.Right);
            button.FocusNeighborTop = Neighbor(Vector2.Up); button.FocusNeighborBottom = Neighbor(Vector2.Down);
        }
        if (_purchase.Buttons.TryGetValue("Purchase", out var action) && _sockets.TryGetValue("Tech" + _selected, out var selected))
        { action.FocusNeighborLeft = action.GetPathTo(selected); action.FocusNeighborTop = action.GetPathTo(selected); }
    }
}
