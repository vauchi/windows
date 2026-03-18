// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;

namespace Vauchi.Helpers;

/// <summary>
/// Extracts only the top-level JSON type key for safe diagnostic logging.
/// Prevents PII leakage from action payloads or screen content.
/// </summary>
public static class JsonSanitizer
{
    /// <summary>
    /// Returns only the top-level type name from a JSON string.
    /// For objects: returns the first property name (e.g. "ActionPressed", "TextChanged").
    /// For strings: returns "String". For null: returns "null".
    /// Never returns PII — safe for logging.
    /// </summary>
    public static string SafeType(string? json)
    {
        if (json == null) return "null";
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in doc.RootElement.EnumerateObject())
                    return prop.Name;
                return "{}";
            }
            return doc.RootElement.ValueKind.ToString();
        }
        catch { return "unparseable"; }
    }
}
