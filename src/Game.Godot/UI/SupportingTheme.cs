// SPDX-License-Identifier: GPL-3.0-only
using Godot;

namespace Game.GodotClient;

/// <summary>Native typography and quiet secondary surfaces around the retained painted frames.</summary>
internal static class SupportingTheme
{
    public static void Typography(Theme theme)
    {
        theme.SetTypeVariation("WatchHeading", "Label"); theme.SetFontSize("font_size", "WatchHeading", 22);
        theme.SetTypeVariation("WatchDetail", "Label"); theme.SetFontSize("font_size", "WatchDetail", 14);
    }
    public static void Forest(Theme theme)
    {
        theme.SetColor("font_color", "WatchHeading", new Color("f6e8ca"));
        theme.SetColor("font_color", "WatchDetail", new Color("becbbe"));
        theme.SetTypeVariation("ForestQuietButton", "Button");
        foreach (var (state, fill, border) in new[] { ("normal", "122b2bed", "55756c"), ("hover", "24423deb", "b4c9ad"),
                     ("pressed", "2b4435f5", "efd478"), ("disabled", "202d2cd9", "4b5b54") })
            theme.SetStylebox(state, "ForestQuietButton", new StyleBoxFlat {
                BgColor = new Color(fill), BorderColor = new Color(border), BorderWidthLeft = state == "pressed" ? 2 : 1,
                BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
                ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 6, ContentMarginBottom = 6,
                CornerRadiusTopLeft = 5, CornerRadiusTopRight = 5, CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5 });
        theme.SetStylebox("focus", "ForestQuietButton", theme.GetStylebox("focus", "Button"));
        theme.SetColor("font_color", "ForestQuietButton", new Color("f6e8ca"));
        theme.SetColor("font_disabled_color", "ForestQuietButton", new Color("b1bfb0"));
        theme.SetFontSize("font_size", "ForestQuietButton", 16);
    }
}
