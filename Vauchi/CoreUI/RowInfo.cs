// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;

namespace Vauchi.CoreUI;

/// <summary>
/// The action that explains a row's item (vauchi/private#479), read from
/// Core's optional <c>info</c>. Null for a row from an older Core.
/// </summary>
public sealed record RowInfo(string InteractionId, string AccessibilityLabel, bool Enabled)
{
    public static RowInfo? Read(JsonElement row)
    {
        if (!row.TryGetProperty("info", out JsonElement info)
            || info.ValueKind != JsonValueKind.Object
            || !info.TryGetProperty("interaction_id", out JsonElement id)
            || id.GetString() is not { Length: > 0 } interactionId)
            return null;
        string label = info.TryGetProperty("accessibility_label", out JsonElement spoken)
            ? spoken.GetString() ?? ""
            : "";
        bool enabled = !info.TryGetProperty("enabled", out JsonElement on)
                       || on.ValueKind != JsonValueKind.False;
        return new RowInfo(interactionId, label, enabled);
    }
}
