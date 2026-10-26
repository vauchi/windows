// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Globalization;
using System.Text.Json;
using Windows.UI;

namespace Vauchi.CoreUI;

/// <summary>
/// The active theme's colours as Core sends them in a surface's
/// <c>tokens.colors</c> (vauchi/private#814). A role the theme leaves out
/// paints its nearest required role.
/// </summary>
public sealed record SurfaceColors(
    Color Background,
    Color Surface,
    Color Text,
    Color TextMuted,
    Color Accent,
    Color Border,
    Color Success,
    Color Warning,
    Color Error,
    Color SurfaceInput,
    Color SurfaceMuted,
    Color SurfaceElevated,
    Color TextOnAccent,
    Color FocusRing)
{
    public Color SelectedBackground => WithAlpha(Accent, 32);
    public Color ActiveSurfaceBorder => WithAlpha(Accent, 160);
    public Color Divider => WithAlpha(TextMuted, 64);
    public Color SecondaryContainer => WithAlpha(TextMuted, 40);

    /// <summary>
    /// Picks the WinUI theme whose stock controls contrast with the
    /// background Core paints. 0.179 is the luminance at which black and
    /// white text reach equal WCAG contrast.
    /// </summary>
    public bool IsLight => RelativeLuminance(Background) > 0.179;

    /// <summary>
    /// The palette in <paramref name="surface"/>, or <c>null</c> when it
    /// carries none or a required role is not <c>#rrggbb</c>; the shell then
    /// keeps the system brushes rather than painting half a theme.
    /// </summary>
    public static SurfaceColors? From(JsonElement surface)
    {
        if (surface.ValueKind != JsonValueKind.Object
            || !surface.TryGetProperty("tokens", out JsonElement tokens)
            || tokens.ValueKind != JsonValueKind.Object
            || !tokens.TryGetProperty("colors", out JsonElement colors)
            || colors.ValueKind != JsonValueKind.Object)
        {
            return null;
        }
        if (Role(colors, "background") is not { } background
            || Role(colors, "surface") is not { } surfaceColor
            || Role(colors, "text") is not { } text
            || Role(colors, "text_muted") is not { } textMuted
            || Role(colors, "accent") is not { } accent
            || Role(colors, "border") is not { } border
            || Role(colors, "success") is not { } success
            || Role(colors, "warning") is not { } warning
            || Role(colors, "error") is not { } error)
        {
            return null;
        }
        return new SurfaceColors(
            background,
            surfaceColor,
            text,
            textMuted,
            accent,
            border,
            success,
            warning,
            error,
            SurfaceInput: Role(colors, "surface_input") ?? surfaceColor,
            SurfaceMuted: Role(colors, "surface_muted") ?? surfaceColor,
            SurfaceElevated: Role(colors, "surface_elevated") ?? surfaceColor,
            TextOnAccent: Role(colors, "text_on_accent") ?? text,
            FocusRing: Role(colors, "focus_ring") ?? accent);
    }

    private static Color? Role(JsonElement colors, string name)
    {
        if (!colors.TryGetProperty(name, out JsonElement value)
            || value.ValueKind != JsonValueKind.String
            || value.GetString() is not { Length: 7 } hex
            || hex[0] != '#'
            || !byte.TryParse(hex.AsSpan(1, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out byte r)
            || !byte.TryParse(hex.AsSpan(3, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out byte g)
            || !byte.TryParse(hex.AsSpan(5, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out byte b))
        {
            return null;
        }
        return Color.FromArgb(255, r, g, b);
    }

    private static Color WithAlpha(Color color, byte alpha) =>
        Color.FromArgb(alpha, color.R, color.G, color.B);

    // WCAG 2.x relative luminance.
    private static double RelativeLuminance(Color color) =>
        0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);

    private static double Linear(byte channel)
    {
        double c = channel / 255.0;
        return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }
}
