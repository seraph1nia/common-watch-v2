using Gum.Managers;
using MonoGameGum;
using RenderingLibrary.Graphics;
using Game.Shared;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Microsoft.Xna.Framework;
using MonoGameGum.GueDeriving;

namespace Game.MonoGameClient;

public sealed partial class WatchGame
{
    readonly SpriteMotion motion = new();
    readonly NineSliceRuntime?[] artPanels = new NineSliceRuntime?[6];
    readonly List<Button> skinnedButtons = [];
    readonly List<NineSliceRuntime> showroomPanels = [];
    int nineSliceCount;
    bool showroomCreated;
    TextRuntime? artTooltipText;

    NineSliceRuntime? Patch(string id)
    {
        if (sprites.Get(id) is not { } texture || sprites.Catalog.Metadata(id).Slice is not { } slice) return null;
        var meta = sprites.Catalog.Metadata(id);
        // Pinned Gum supports one source frame width for both axes, plus BorderScale.
        // Our measured crops intentionally use equal margins (validated before use).
        if (slice.Left != slice.Top || slice.Left != slice.Right || slice.Left != slice.Bottom) return null;
        var patch = new NineSliceRuntime
        {
            Texture = texture,
            CustomFrameTextureCoordinateWidth = slice.Left,
            TextureAddress = TextureAddress.EntireTexture,
            BorderScale = meta.BorderScale,
            Color = Color.White
        };
        patch.WidthUnits = DimensionUnitType.Absolute; patch.HeightUnits = DimensionUnitType.Absolute;
        nineSliceCount++;
        return patch;
    }
    void CreateHudArt()
    {
        string[] ids = ["hud/panel", "hud/panel", "hud/card_frame", "hud/card_frame", "hud/card_frame", "hud/tooltip"];
        for (int i = 0; i < ids.Length; i++)
        {
            var patch = Patch(ids[i]);
            if (patch is null) continue;
            patch.Visible = false; patch.AddToRoot(); artPanels[i] = patch;
        }
        if (artPanels[5] is not null) { artTooltipText = NewText(0, 0, 240); artTooltipText.Color = new(48, 37, 28); }
    }
    void SkinButton(Button button)
    {
        if (options.Hud == HudArt.Illustrated && !skinnedButtons.Contains(button)) skinnedButtons.Add(button);
    }
    void UpdateHudArt()
    {
        bool inGame = session is { HasState: true } && session.Snapshot.Phase != Game.Core.Phase.Lobby && !settingsMenu.IsOpen && !options.Showroom;
        foreach (var patch in artPanels) if (patch is not null) patch.Visible = false;
        void Place(int index, float x, float y, float width, float height, bool visible)
        {
            if (artPanels[index] is not { } p) return; p.X = x; p.Y = y; p.Width = width; p.Height = height; p.Visible = visible;
        }
        Place(0, 0, 0, Window.ClientBounds.Width, 112, inGame);
        Place(1, Window.ClientBounds.Width - SidebarWidth, 112, SidebarWidth, Window.ClientBounds.Height - 154, inGame && session.Snapshot.Phase != Game.Core.Phase.RewardDraft);
        for (int i = 0; i < rewardPanels.Count; i++)
            Place(i + 2, rewardPanels[i].X - 12, rewardPanels[i].Y - 55, 290, 400, inGame && rewardPanels[i].Visual.Visible);
        if (artTooltipText is not null) artTooltipText.Visible = false;
        var mouse = Microsoft.Xna.Framework.Input.Mouse.GetState();
        if (inGame && session.Snapshot.Phase is Game.Core.Phase.Building or Game.Core.Phase.Preparation && mouse.X < Window.ClientBounds.Width - SidebarWidth && mouse.Y > 112)
        {
            var world = ((new Vector2(mouse.X, mouse.Y) - Origin()) / cameraZoom + Center()).ToNumerics();
            var cell = settlementProjection.HitTest(world, Game.Shared.SettlementLayout.Coordinates);
            if (cell is not null)
            {
                int index = Game.Shared.SettlementLayout.Coordinates.ToList().IndexOf(cell.Value);
                var slot = session.City.Slots[index];
                float x = Math.Clamp(mouse.X + 14, 0, Window.ClientBounds.Width - 280), y = Math.Clamp(mouse.Y + 14, 112, Window.ClientBounds.Height - 110);
                Place(5, x, y, 270, 70, true);
                if (artTooltipText is not null)
                { artTooltipText.Visible = true; artTooltipText.X = x + 18; artTooltipText.Y = y + 18; artTooltipText.Text = $"Plot {index + 1}: {slot.Type} / Level {slot.Level}"; }
            }
        }
        Color ink = new(48, 37, 28);
        title.Color = resources.Color = status.Color = artPanels[0]?.Visible == true ? ink : Color.White;
        combatRoster.Color = artPanels[1]?.Visible == true ? ink : Color.White;
        void TextColor(Gum.Forms.Controls.Label label, Color color)
        {
            if (label.Visual is TextRuntime text) text.Color = color;
            else if (label.Visual.GetGraphicalUiElementByName("TextInstance") is TextRuntime nested) nested.Color = color;
        }
        foreach (var label in new[] { selectedText, hintText }) TextColor(label, artPanels[1]?.Visible == true ? ink : Color.White);
        for (int i = 0; i < rewardNames.Count; i++)
        { TextColor(rewardNames[i], artPanels[i + 2] is null ? Color.White : ink); TextColor(rewardDescriptions[i], artPanels[i + 2] is null ? Color.White : ink); }
        foreach (Button button in skinnedButtons.Concat(rewardButtons))
        {
            if (button.Visual is not ButtonVisual visual || sprites.Get("hud/button") is not { } texture) continue;
            var meta = sprites.Catalog.Metadata("hud/button");
            if (meta.Slice is not { } slice || slice.Left != slice.Top || slice.Left != slice.Right || slice.Left != slice.Bottom) continue;
            visual.Background.Texture = texture; visual.Background.TextureAddress = TextureAddress.EntireTexture;
            visual.Background.CustomFrameTextureCoordinateWidth = meta.Slice!.Left; visual.Background.BorderScale = meta.BorderScale;
            visual.BackgroundColor = button.IsEnabled ? Color.White : new Color(145, 145, 145);
            visual.ForegroundColor = Color.White;
        }
    }
    bool WorldSprite(string id, Vector2 ground, float extent, Color? tint = null)
    {
        Rectangle bounds;
        var texture = sprites.Get(id);
        bool found = texture is not null;
        if (texture is not null) bounds = canvas.Sprite(texture, ground, extent, sprites.Catalog.Metadata(id), tint);
        else bounds = new Rectangle((int)ground.X - 24, (int)ground.Y - 32, 48, 40);
        if (options.DebugOverlay)
        {
            Color color = new(169, 202, 237);
            canvas.Line(new(bounds.Left, bounds.Top), new(bounds.Right, bounds.Top), color, 1);
            canvas.Line(new(bounds.Right, bounds.Top), new(bounds.Right, bounds.Bottom), color, 1);
            canvas.Line(new(bounds.Right, bounds.Bottom), new(bounds.Left, bounds.Bottom), color, 1);
            canvas.Line(new(bounds.Left, bounds.Bottom), new(bounds.Left, bounds.Top), color, 1);
            canvas.Line(ground - new Vector2(5, 0), ground + new Vector2(5, 0), new(255, 130, 206), 1);
            canvas.Line(ground - new Vector2(0, 5), ground + new Vector2(0, 5), new(255, 130, 206), 1);
        }
        return found;
    }
    void DrawShowroom()
    {
        title.Visible = resources.Visible = status.Visible = footer.Visible = false;
        WorldLabel("ASSET SHOWROOM / MONOGAME + GUM — no authority; pink = ground anchor; blue = canvas bounds", new(625, 14), 1200, 1);
        string[] ids = ["building/lumbermill", "building/farm", "building/barracks", "unit/swordsman", "unit/enemy_swordsman", "terrain/woodland_hex", "resource/gold", "resource/wood", "resource/stone"];
        for (int i = 0; i < ids.Length; i++)
        {
            Vector2 ground = new(115 + (i % 5) * 235, 220 + (i / 5) * 205);
            if (!WorldSprite(ids[i], ground, sprites.Catalog.Metadata(ids[i]).DisplaySize)) canvas.Circle(ground, 18, new(99, 133, 161));
            WorldLabel(ids[i], ground + new Vector2(0, 16), 225, .85f);
        }
        canvas.Rect(1070, 342, 44, 35, new(99, 133, 161)); WorldLabel("Missing asset / fallback", new(1110, 420), 210, .75f);
        if (!showroomCreated)
        {
            showroomCreated = true;
            foreach (var (id, pos, size, label) in new (string, Vector2, Vector2, string)[]
            {
                ("hud/button",new(560,480),new(160,48),"Dynamic button"),
                ("hud/panel",new(24,470),new(480,160),"Gold 12   Wood 3   Stone 2"),
                ("hud/panel",new(24,650),new(640,420),"Building inspector / 640 x 420\nDynamic text"),
                ("hud/card_frame",new(800,480),new(260,390),"WATCH REWARD\n\nDynamic description\n\nChoose reward"),
                ("hud/tooltip",new(750,930),new(370,80),"Tooltip: inspect a plot")
            })
            {
                var patch = Patch(id);
                if (patch is not null) { patch.X = pos.X; patch.Y = pos.Y; patch.Width = size.X; patch.Height = size.Y; patch.AddToRoot(); showroomPanels.Add(patch); }
                var text = NewText(pos.X + (id == "hud/button" ? 16 : 30), pos.Y + size.Y / 2 - 10, size.X - (id == "hud/button" ? 32 : 60));
                text.Text = label; text.Height = size.Y / 2; text.Color = id == "hud/button" ? Color.White : new(48, 37, 28);
            }
            Console.WriteLine($"MONOGAME_SHOWROOM assets={sprites.Catalog.Entries.Count} nine-slices={nineSliceCount} authority=none");
        }
    }
}
