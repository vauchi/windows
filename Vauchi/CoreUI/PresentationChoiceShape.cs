// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections.Generic;
using System.Text.Json;

namespace Vauchi.CoreUI;

/// <summary>Which control a Core <c>Choice</c> node is drawn with.</summary>
public enum PresentationChoiceControl
{
    /// <summary>A row of segments, every option visible at once.</summary>
    Segmented,

    /// <summary>A drop-down list for choices too long to lay side by side.</summary>
    DropDown,
}

public readonly record struct PresentationChoiceOption(string Id, string Label);

/// <summary>
/// The control and selection decisions for a Core <c>Choice</c> node,
/// taken before any WinUI element exists so they can be asserted without a
/// XAML host. The design canvas (ADR-066) draws two- and three-option
/// choices as a segmented control; longer lists keep the drop-down.
/// </summary>
public static class PresentationChoiceShape
{
    public const int SegmentedMaximum = 3;

    public static PresentationChoiceControl Control(int optionCount) =>
        optionCount is >= 2 and <= SegmentedMaximum
            ? PresentationChoiceControl.Segmented
            : PresentationChoiceControl.DropDown;

    public static IReadOnlyList<PresentationChoiceOption> Options(JsonElement payload)
    {
        var options = new List<PresentationChoiceOption>();
        if (!payload.TryGetProperty("options", out JsonElement array)
            || array.ValueKind != JsonValueKind.Array)
            return options;
        foreach (JsonElement option in array.EnumerateArray())
        {
            options.Add(new PresentationChoiceOption(
                StringOrEmpty(option, "id"),
                StringOrEmpty(option, "label")));
        }
        return options;
    }

    public static int SelectedIndex(IReadOnlyList<PresentationChoiceOption> options, string? selected)
    {
        if (string.IsNullOrEmpty(selected))
            return -1;
        for (int index = 0; index < options.Count; index++)
        {
            if (options[index].Id == selected)
                return index;
        }
        return -1;
    }

    /// <summary>Core's option id is the value on the wire, never the label.</summary>
    public static string? EmittedValue(IReadOnlyList<PresentationChoiceOption> options, int index) =>
        index >= 0 && index < options.Count ? options[index].Id : null;

    private static string StringOrEmpty(JsonElement value, string property) =>
        value.TryGetProperty(property, out JsonElement element)
        && element.ValueKind == JsonValueKind.String
            ? element.GetString() ?? ""
            : "";
}
