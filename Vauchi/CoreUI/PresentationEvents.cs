// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections.Generic;
using System.Text.Json;

namespace Vauchi.CoreUI;

public static class PresentationEvents
{
    public static string SurfaceActivated(string surfaceId) =>
        Serialize("SurfaceActivated", new { surface_id = surfaceId });

    public static string ActionActivated(string surfaceId, string interactionId) =>
        Serialize(
            "ActionActivated",
            new { surface_id = surfaceId, interaction_id = interactionId });

    public static string BackRequested(string surfaceId) =>
        Serialize("BackRequested", new { surface_id = surfaceId });

    public static string OverlayDismissed(string surfaceId, string kind) =>
        Serialize("OverlayDismissed", new { surface_id = surfaceId, kind });

    /// <summary>Enter pressed in a field — the platform's submit gesture.</summary>
    public static string InputSubmitted(string surfaceId, string bindingId) =>
        Serialize(
            "InputSubmitted",
            new { surface_id = surfaceId, binding_id = bindingId });

    /// <summary>
    /// A field lost focus without the user having submitted, so Core can offer
    /// a way to commit text left behind rather than committing it for them.
    /// </summary>
    public static string InputFocusEnded(string surfaceId, string bindingId) =>
        Serialize(
            "InputFocusEnded",
            new { surface_id = surfaceId, binding_id = bindingId });

    public static string TextChanged(string surfaceId, string bindingId, string value) =>
        ValueChanged(surfaceId, bindingId, "Text", value);

    public static string BooleanChanged(string surfaceId, string bindingId, bool value) =>
        ValueChanged(surfaceId, bindingId, "Boolean", value);

    public static string ChoiceChanged(string surfaceId, string bindingId, string? value) =>
        ValueChanged(surfaceId, bindingId, "Choice", value);

    public static string NumberChanged(string surfaceId, string bindingId, double value) =>
        ValueChanged(surfaceId, bindingId, "Number", value);

    public static string EnvironmentChanged(
        int availableWidth,
        int availableHeight,
        bool reducedMotion) =>
        Serialize(
            "PresentationEnvironmentChanged",
            new
            {
                available_width = System.Math.Max(0, availableWidth),
                available_height = System.Math.Max(0, availableHeight),
                input_modes = new[] { "pointer", "keyboard" },
                motion = reducedMotion ? "reduced" : "full",
            });

    private static string ValueChanged(
        string surfaceId,
        string bindingId,
        string kind,
        object? value) =>
        Serialize(
            "ValueChanged",
            new
            {
                surface_id = surfaceId,
                binding_id = bindingId,
                value = new Dictionary<string, object?> { [kind] = value },
            });

    private static string Serialize(string variant, object payload) =>
        JsonSerializer.Serialize(
            new Dictionary<string, object> { [variant] = payload });
}
