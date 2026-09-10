// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Globalization;
using Windows.UI;

namespace Vauchi.CoreUI;

/// <summary>
/// Semantic theme colors for Windows components. Mirrors the bundled
/// Catppuccin Mocha palette (themes/themes.json); a future ThemeService
/// will swap these at runtime based on the applied theme. Components
/// reference ThemeColors.Destructive / .Success / etc. instead of the
/// WinUI Colors.* table so the scripts/check-design-tokens.sh linter can
/// enforce palette consistency.
/// </summary>
public static class ThemeColors
{
    // Catppuccin Mocha palette — keep in sync with
    // themes/themes.json and vauchi-core's default_theme().
    public static readonly Color Destructive = ParseHex("#f38ba8");      // error — unifies prior Red/Tomato
    public static readonly Color Success = ParseHex("#a6e3a1");          // success
    public static readonly Color Warning = ParseHex("#fab387");          // warning
    public static readonly Color Info = ParseHex("#89b4fa");             // accent — status InProgress, pin-filled dot
    public static readonly Color Neutral = ParseHex("#a6adc8");          // text-secondary — status Neutral, pin-empty dot
    public static readonly Color OnColored = ParseHex("#cdd6f4");        // text-primary — light foreground text over saturated fills (avatar initials, button labels)

    // Alpha variants for overlays and emphasis — derived from the
    // palette above, never from the WinUI system accent.
    public static readonly Color SelectedBackground = WithAlpha(Info, 32);      // selected-row highlight overlay
    public static readonly Color ActiveSurfaceBorder = WithAlpha(Info, 160);    // border of the active presentation surface
    public static readonly Color Divider = WithAlpha(Neutral, 64);              // separators between presentation blocks
    public static readonly Color SecondaryContainer = WithAlpha(Neutral, 40);   // avatar-initials fill — mirrors Android's secondaryContainer / macOS's Color.secondary.opacity(0.15)

    // No dedicated focus-ring token crosses the C ABI yet (tokens.json's
    // "focus-ring" maps to the same blue as "accent", which is Info in
    // this palette) — reuse it rather than inventing a second blue to
    // keep in sync.
    public static readonly Color FocusRing = Info;

    private static Color WithAlpha(Color color, byte alpha) =>
        Color.FromArgb(alpha, color.R, color.G, color.B);

    // TODO(HUMBLE): W — hardcodes avatar fallback color synced with core; core should provide avatar fallback color in component/theme data (see _private/docs/problems/done/2026-07-06-desktop-tui-web-domain-shell-violations).
    // Avatar fallback when core doesn't provide a per-contact bg_color.
    // Verified 2026-09: neither themes/tokens.json nor the C ABI
    // (core/vauchi-cabi/src/design_tokens.rs) carries a color token today,
    // and core has no fixed avatar-fallback constant of its own — this hex
    // is a Windows-only placeholder, not a value core emits. Keep the hex
    // and the Color in sync until core adds an avatar-fallback color token.
    public const string AvatarFallbackHex = "#4682B4";
    public static readonly Color AvatarFallback = ParseHex(AvatarFallbackHex);

    private static Color ParseHex(string hex)
    {
        // "#rrggbb" → Color. Invalid input falls back to a visible
        // magenta so a stale theme key is obvious rather than silent.
        if (hex.Length == 7 && hex[0] == '#'
            && byte.TryParse(hex.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r)
            && byte.TryParse(hex.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g)
            && byte.TryParse(hex.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
        {
            return Color.FromArgb(255, r, g, b);
        }
        return Color.FromArgb(255, 255, 0, 255);
    }
}
