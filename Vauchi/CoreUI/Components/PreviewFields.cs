// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections.Generic;
using System.Text.Json;

namespace Vauchi.CoreUI.Components;

/// <summary>
/// Pure field-resolution for the card <c>Preview</c> component, extracted
/// from <see cref="PreviewComponent"/> so it is unit-testable without the
/// WinUI control (linked into Vauchi.UnitTests).
/// </summary>
internal static class PreviewFields
{
    public static List<(string Label, string Value)> Resolve(JsonElement data)
    {
        var result = new List<(string, string)>();

        string? selectedVariant =
            data.TryGetProperty("selected_variant", out var svEl)
            && svEl.ValueKind == JsonValueKind.String
                ? svEl.GetString()
                : null;

        if (selectedVariant != null && data.TryGetProperty("variants", out var variants))
        {
            foreach (var v in variants.EnumerateArray())
            {
                string variantId = v.TryGetProperty("variant_id", out var vidEl)
                    ? vidEl.GetString() ?? "" : "";
                if (variantId != selectedVariant)
                    continue;

                if (!v.TryGetProperty("visible_fields", out var visFields))
                    break;

                var fieldIds = new HashSet<string>();
                foreach (var fId in visFields.EnumerateArray())
                {
                    string? id = fId.GetString();
                    if (id != null) fieldIds.Add(id);
                }

                if (data.TryGetProperty("fields", out var allFields))
                {
                    foreach (var field in allFields.EnumerateArray())
                    {
                        string fId = field.TryGetProperty("id", out var fIdEl)
                            ? fIdEl.GetString() ?? "" : "";
                        if (fieldIds.Contains(fId))
                            result.Add((LabelOf(field), ValueOf(field)));
                    }
                }
                return result;
            }
        }

        if (data.TryGetProperty("fields", out var fields))
        {
            foreach (var field in fields.EnumerateArray())
                result.Add((LabelOf(field), ValueOf(field)));
        }

        return result;
    }

    private static string LabelOf(JsonElement field) =>
        field.TryGetProperty("label", out var el) ? el.GetString() ?? "" : "";

    private static string ValueOf(JsonElement field) =>
        field.TryGetProperty("value", out var el) ? el.GetString() ?? "" : "";
}
