using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class Main
{
    private AssetCatalog _catalog = null!;
    private readonly CombatMotion _motion = new();
    private readonly Dictionary<string, Texture2D?> _hudTextures = [];
    private int _ninePatches;

    private Texture2D? LoadArt(string id, bool hud = false)
    {
        if (hud ? _options.Hud != HudArt.Illustrated : _options.Mode != RenderMode.Sprites) return null;
        var cache = hud ? _hudTextures : _textures;
        if (cache.TryGetValue(id, out var cached)) return cached;
        Texture2D? texture = null;
        if (_catalog.Resolve(id, true) is { } path)
        {
            try
            {
                if (!hud) _spriteDecodeAttempts++;
                using var source = System.IO.File.OpenRead(path);
                Span<byte> signature = stackalloc byte[8];
                if (source.Read(signature) != 8 || !signature.SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
                    throw new InvalidDataException("Invalid PNG header");
                using var image = new Image();
                if (image.Load(path) == Error.Ok && !image.IsEmpty())
                {
                    var metadata = _catalog.Metadata(id);
                    if (metadata.Size.Length == 2 && (metadata.Size[0] != image.GetWidth() || metadata.Size[1] != image.GetHeight()))
                        throw new InvalidDataException("PNG dimensions differ from catalog");
                    texture = ImageTexture.CreateFromImage(image);
                }
            }
            catch (Exception e) { GD.PushWarning($"Optional art {id}: {e.Message}. Using plain rendering."); }
        }
        cache[id] = texture;
        return texture;
    }

    private NinePatchRect? NinePatch(Control parent, string id)
    {
        if (LoadArt(id, true) is not { } texture || _catalog.Metadata(id).Slice is not { } slice) return null;
        var meta = _catalog.Metadata(id);
        var patch = new NinePatchRect
        {
            Name = "IllustratedBackground",
            Texture = texture,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            PatchMarginLeft = slice.Left,
            PatchMarginTop = slice.Top,
            PatchMarginRight = slice.Right,
            PatchMarginBottom = slice.Bottom,
            Scale = Vector2.One * meta.BorderScale,
        };
        var background = new Node2D { Name = "ArtBackground", ShowBehindParent = true };
        parent.AddChild(background); background.AddChild(patch);
        void Layout() { patch.Position = Vector2.Zero; patch.Size = parent.Size / meta.BorderScale; }
        parent.Resized += Layout; Layout();
        _ninePatches++;
        return patch;
    }
    private void SkinPanel(PanelContainer panel, string id = "hud/panel")
    {
        bool illustrated = NinePatch(panel, id) is not null;
        var theme = new Theme();
        theme.SetColor("font_color", "Label", new Color(illustrated ? "30251c" : "e7edf2"));
        panel.Theme = theme;
        if (illustrated) panel.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
    }
    private void SkinButton(Button button)
    {
        var patch = NinePatch(button, "hud/button");
        if (patch is null) return;
        foreach (string state in new[] { "normal", "hover", "pressed", "disabled", "focus" }) button.AddThemeStyleboxOverride(state, new StyleBoxEmpty());
        button.MouseEntered += () => patch.Modulate = new Color(1.15f, 1.15f, 1.15f);
        button.MouseExited += () => patch.Modulate = Colors.White;
    }
    private void Overlay(Vector2 ground, Rect2 bounds)
    {
        if (!_options.DebugOverlay) return;
        DrawRect(bounds, new Color("a9caed"), false, 1);
        DrawLine(ground - new Vector2(5, 0), ground + new Vector2(5, 0), new Color("ff82ce"), 1);
        DrawLine(ground - new Vector2(0, 5), ground + new Vector2(0, 5), new Color("ff82ce"), 1);
    }

    private void CreateShowroom()
    {
        var layer = new CanvasLayer { Layer = 8 }; AddChild(layer);
        var root = new Control(); layer.AddChild(root);
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var background = new ColorRect { Color = new Color("101820"), MouseFilter = Control.MouseFilterEnum.Ignore };
        root.AddChild(background); background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var scroll = new ScrollContainer(); root.AddChild(scroll); scroll.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var content = new Control { CustomMinimumSize = new Vector2(1220, 1030) }; scroll.AddChild(content);
        void LabelAt(string text, Vector2 at, Vector2 size, bool parchment = false)
        {
            var label = new Label { Text = text, Position = at, Size = size, AutowrapMode = TextServer.AutowrapMode.WordSmart };
            label.AddThemeColorOverride("font_color", new Color(parchment ? "30251c" : "e7edf2")); content.AddChild(label);
        }
        LabelAt("ASSET SHOWROOM / GODOT — frozen presentation; pink = ground anchor; blue = canvas bounds", new(24, 16), new(1160, 40));
        string[] ids = ["building/lumbermill", "building/farm", "building/barracks", "unit/swordsman", "unit/enemy_swordsman", "terrain/woodland_hex", "resource/gold", "resource/wood", "resource/stone"];
        for (int i = 0; i < ids.Length; i++)
        {
            string id = ids[i]; var meta = _catalog.Metadata(id);
            Vector2 ground = new(115 + (i % 5) * 235, 220 + (i / 5) * 205);
            float size = meta.DisplaySize;
            Vector2 anchor = new(meta.Anchor[0], meta.Anchor[1]);
            if (LoadArt(id) is { } texture)
            {
                var art = new TextureRect
                {
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    Texture = texture,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    Position = ground - anchor * size,
                    Size = Vector2.One * size
                };
                content.AddChild(art);
            }
            else content.AddChild(new ColorRect { Position = ground - new Vector2(20, 40), Size = new(40, 40), Color = new Color("6385a1") });
            var border = new ReferenceRect { Position = ground - anchor * size, Size = Vector2.One * size, EditorOnly = false, BorderColor = new Color("a9caed") }; content.AddChild(border);
            content.AddChild(new ColorRect { Position = ground - new Vector2(5, 1), Size = new(10, 2), Color = new Color("ff82ce") });
            content.AddChild(new ColorRect { Position = ground - new Vector2(1, 5), Size = new(2, 10), Color = new Color("ff82ce") });
            LabelAt(id, ground + new Vector2(-105, 16), new(220, 45));
        }
        content.AddChild(new ColorRect { Position = new(1070, 342), Size = new(44, 35), Color = new Color("6385a1") });
        LabelAt("Missing asset\nDebug fallback", new(1050, 405), new(160, 55));
        foreach (var (id, pos, size, label) in new (string, Vector2, Vector2, string)[]
        {
            ("hud/button",new(560,480),new(160,48),"Dynamic button"),
            ("hud/panel",new(24,470),new(480,160),"Resource panel\nGold 12   Wood 3   Stone 2"),
            ("hud/panel",new(24,650),new(640,420),"Building inspector / 640 × 420\nOrdinary native Controls"),
            ("hud/card_frame",new(800,480),new(260,390),"WATCH REWARD\n\nDynamic card description\n\nChoose reward"),
            ("hud/tooltip",new(750,930),new(370,80),"Tooltip: inspect a settlement plot")
        })
        {
            var panel = new PanelContainer { Position = pos, Size = size }; content.AddChild(panel); SkinPanel(panel, id);
            var text = new Label { Text = label, AutowrapMode = TextServer.AutowrapMode.WordSmart, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            text.AddThemeColorOverride("font_color", new Color(id == "hud/button" ? "e7edf2" : "30251c")); panel.AddChild(Margin(text, id == "hud/button" ? 12 : 36));
        }
        content.CustomMinimumSize = new(1220, 1080);
        GD.Print($"GODOT_SHOWROOM assets={_catalog.Entries.Count} nine-patches={_ninePatches} authority=none");
    }
}
