// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections.Generic;
using System.Text.Json;

namespace Vauchi.CoreUI;

/// <summary>
/// Which of Core's context-bar slots are present. The surface's own title
/// row draws exactly these, grouped at its leading and trailing ends
/// (vauchi/private#534): an absent slot takes no space, Primary moves to a
/// full-width button under the surface's content instead.
/// </summary>
public static class ContextBarLayout
{
    public static readonly IReadOnlyList<string> Roles =
        new[] { "back", "navigation", "primary", "secondary", "info" };

    /// Leading end of the title row: Back first, a navigation launcher only
    /// where the sidebar or tab bar isn't already showing destinations.
    public static readonly IReadOnlyList<string> LeadingRoles =
        new[] { "back", "navigation" };

    /// Trailing end of the title row: Info beside Actions, the menu button
    /// at the very end (diagram: `[‹] Title … [ⓘ] [⋯]`).
    public static readonly IReadOnlyList<string> TrailingRoles =
        new[] { "info", "secondary" };

    public static IReadOnlyList<string> Slots(JsonElement? bar) =>
        PresentRoles(bar, Roles);

    public static IReadOnlyList<string> LeadingSlots(JsonElement? bar) =>
        PresentRoles(bar, LeadingRoles);

    public static IReadOnlyList<string> TrailingSlots(JsonElement? bar) =>
        PresentRoles(bar, TrailingRoles);

    private static IReadOnlyList<string> PresentRoles(
        JsonElement? bar,
        IReadOnlyList<string> order)
    {
        var present = new List<string>();
        if (bar is not { } value || value.ValueKind != JsonValueKind.Object)
            return present;
        foreach (string role in order)
        {
            if (value.TryGetProperty(role, out JsonElement action)
                && action.ValueKind == JsonValueKind.Object)
                present.Add(role);
        }
        return present;
    }
}
