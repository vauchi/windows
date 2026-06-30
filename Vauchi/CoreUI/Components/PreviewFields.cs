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

        // Core's build_visible_fields already applies the selected variant and
        // drops Hidden fields, so render `visible_fields` directly. Reading the
        // raw `fields` list leaks Hidden values
        // (2026-05-21-component-preview-legacy-fields).
        if (data.TryGetProperty("visible_fields", out var visible)
            && visible.ValueKind == JsonValueKind.Array)
        {
            foreach (var field in visible.EnumerateArray())
                result.Add((LabelOf(field), ValueOf(field)));
        }

        return result;
    }

    private static string LabelOf(JsonElement field) =>
        field.TryGetProperty("label", out var el) ? el.GetString() ?? "" : "";

    private static string ValueOf(JsonElement field) =>
        field.TryGetProperty("value", out var el) ? el.GetString() ?? "" : "";
}
