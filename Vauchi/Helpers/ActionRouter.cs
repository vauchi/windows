// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;

namespace Vauchi.Helpers;

/// <summary>
/// Routes JSON payloads to the correct native API based on variant name.
/// UserAction variants → AppHandleAction.
/// ExchangeHardwareEvent variants → AppHandleHardwareEvent.
/// </summary>
public static class ActionRouter
{
    /// <summary>
    /// Returns true if the JSON payload is an ExchangeHardwareEvent (ADR-031).
    /// Detection: externally-tagged serde enum — first property name is the variant.
    /// Strategy: match on the SMALLER, MORE STABLE UserAction set. Everything else
    /// is assumed to be a hardware event. This is forward-compatible — new hardware
    /// event variants in core are automatically routed correctly without C# changes.
    /// </summary>
    public static bool IsHardwareEvent(string json)
    {
        if (string.IsNullOrEmpty(json)) return false;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return false;
            using var enumerator = doc.RootElement.EnumerateObject();
            if (!enumerator.MoveNext()) return false;
            // Match UserAction variants (stable, rarely changes) — everything else
            // is a hardware event. This way, new ExchangeHardwareEvent variants
            // added in core are automatically routed correctly.
            return enumerator.Current.Name is not (
                "TextChanged" or "ItemToggled" or "ActionPressed"
                or "FieldVisibilityChanged" or "GroupViewSelected"
                or "SearchChanged" or "ListItemSelected"
                or "SettingsToggled" or "UndoPressed");
        }
        catch { return false; }
    }
}
