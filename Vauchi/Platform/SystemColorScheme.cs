// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Windows.UI;

namespace Vauchi.Platform;

/// <summary>
/// The OS light/dark appearance, reported to Core so "Follow system"
/// paints the matching default theme (vauchi/private#814). Windows exposes
/// the app mode only as <c>UIColorType.Background</c>: black in dark mode,
/// white in light.
/// </summary>
public static class SystemColorScheme
{
    public static string From(Color osBackground) =>
        osBackground.R + osBackground.G + osBackground.B < 3 * 128 ? "dark" : "light";

    public static string Current()
    {
        try
        {
            return From(new Windows.UI.ViewManagement.UISettings()
                .GetColorValue(Windows.UI.ViewManagement.UIColorType.Background));
        }
        catch (System.Exception)
        {
            return "dark";
        }
    }
}
