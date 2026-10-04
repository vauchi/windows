// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections.Generic;
using System.Text.Json;

namespace Vauchi.CoreUI;

/// <summary>
/// Which of Core's context-bar slots are present, in Core's order. The
/// host draws exactly these (vauchi/private#479): an absent slot takes no
/// space, and a bar with none hides the strip.
/// </summary>
public static class ContextBarLayout
{
    public static readonly IReadOnlyList<string> Roles =
        new[] { "back", "navigation", "primary", "secondary", "info" };

    public static IReadOnlyList<string> Slots(JsonElement? bar)
    {
        var present = new List<string>();
        if (bar is not { } value || value.ValueKind != JsonValueKind.Object)
            return present;
        foreach (string role in Roles)
        {
            if (value.TryGetProperty(role, out JsonElement action)
                && action.ValueKind == JsonValueKind.Object)
                present.Add(role);
        }
        return present;
    }
}
