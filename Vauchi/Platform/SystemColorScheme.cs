// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Windows.UI;

namespace Vauchi.Platform;

/// <summary>
/// The OS light/dark appearance, reported to Core so "Follow system"
/// paints the matching default theme (vauchi/private#814).
/// </summary>
public static class SystemColorScheme
{
    public static string From(Color osBackground) => "";
}
