// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Vauchi.CoreUI;

public sealed class PresentationState
{
    private sealed record Revisioned(ulong Revision, JsonElement Value);

    private Dictionary<string, Revisioned> _surfaces = new();
    private Dictionary<string, Revisioned> _contextBars = new();
    private JsonElement? _profile;

    public string? ActiveSurfaceId =>
        ProfileString("active_surface");

    public JsonElement? ActiveContextBar =>
        ActiveSurfaceId is { } id && _contextBars.TryGetValue(id, out var entry)
            ? entry.Value
            : null;

    public JsonElement? PresentedOverlay { get; private set; }

    public IReadOnlyList<string> VisibleSurfaceIds
    {
        get
        {
            string? active = ActiveSurfaceId;
            string? primary = ProfileString("primary_surface");
            if (active is null || primary is null)
                return Array.Empty<string>();

            if (ProfileString("pane_layout") == "split"
                && ProfileString("detail_surface") is { } detail)
            {
                return new[] { primary, detail };
            }

            return new[] { active };
        }
    }

    public JsonElement? Surface(string surfaceId) =>
        _surfaces.TryGetValue(surfaceId, out var entry) ? entry.Value : null;

    public JsonElement? ContextBar(string surfaceId) =>
        _contextBars.TryGetValue(surfaceId, out var entry) ? entry.Value : null;

    public bool TryApplyEnvelope(
        string json,
        out IReadOnlyList<JsonElement> effects,
        out string? error)
    {
        effects = Array.Empty<JsonElement>();
        error = null;
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            error = exception.Message;
            return false;
        }

        using (document)
        {
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                error = "command envelope must be an object";
                return false;
            }
            if (root.TryGetProperty("error", out var coreError)
                && coreError.ValueKind == JsonValueKind.String)
            {
                error = coreError.GetString();
                return false;
            }
            if (!root.TryGetProperty("commands", out var commands)
                || commands.ValueKind != JsonValueKind.Array)
            {
                error = "command envelope is missing commands";
                return false;
            }

            var next = Copy();
            var nextEffects = new List<JsonElement>();
            foreach (JsonElement command in commands.EnumerateArray())
            {
                if (!next.TryApplyCommand(command, nextEffects, out error))
                    return false;
            }

            _surfaces = next._surfaces;
            _contextBars = next._contextBars;
            _profile = next._profile;
            PresentedOverlay = next.PresentedOverlay;
            effects = nextEffects;
            return true;
        }
    }

    public static string CommandName(JsonElement command)
    {
        if (command.ValueKind == JsonValueKind.String)
            return command.GetString() ?? "";
        if (command.ValueKind != JsonValueKind.Object)
            return "";
        using JsonElement.ObjectEnumerator properties = command.EnumerateObject();
        return properties.MoveNext() ? properties.Current.Name : "";
    }

    private PresentationState Copy() => new()
    {
        _surfaces = new Dictionary<string, Revisioned>(_surfaces),
        _contextBars = new Dictionary<string, Revisioned>(_contextBars),
        _profile = _profile?.Clone(),
        PresentedOverlay = PresentedOverlay?.Clone(),
    };

    private bool TryApplyCommand(
        JsonElement command,
        List<JsonElement> effects,
        out string? error)
    {
        error = null;
        string name = CommandName(command);
        if (name.Length == 0)
        {
            error = "command is missing a variant";
            return false;
        }
        if (command.ValueKind == JsonValueKind.String)
        {
            effects.Add(command.Clone());
            return true;
        }
        if (!command.TryGetProperty(name, out var payload))
        {
            error = $"{name} is missing its payload";
            return false;
        }

        switch (name)
        {
            case "ReplaceSurface":
                return TryReplaceSurface(payload, out error);
            case "SetContextBar":
                return TrySetContextBar(payload, out error);
            case "SetPresentationProfile":
                return TrySetProfile(payload, out error);
            case "PresentOverlay":
                if (!TryPresentOverlay(payload, out error))
                    return false;
                effects.Add(command.Clone());
                return true;
            default:
                effects.Add(command.Clone());
                return true;
        }
    }

    private bool TryReplaceSurface(JsonElement payload, out string? error)
    {
        error = null;
        if (!payload.TryGetProperty("surface", out var surface)
            || surface.ValueKind != JsonValueKind.Object)
        {
            error = "ReplaceSurface is missing surface";
            return false;
        }
        if (!TryIdentity(surface, out string id, out ulong revision, out error))
            return false;
        if (!_surfaces.TryGetValue(id, out var current) || revision >= current.Revision)
        {
            _surfaces[id] = new Revisioned(revision, surface.Clone());
            _contextBars.Remove(id);
            if (PresentedOverlay is { } presented
                && presented.TryGetProperty("surface_id", out var overlaySurface)
                && overlaySurface.GetString() == id)
            {
                PresentedOverlay = null;
            }
        }
        return true;
    }

    private bool TrySetContextBar(JsonElement payload, out string? error)
    {
        error = null;
        if (!TryIdentity(payload, out string id, out ulong revision, out error))
            return false;
        if (!_surfaces.TryGetValue(id, out var surface)
            || surface.Revision != revision)
        {
            error = "SetContextBar does not match the current surface revision";
            return false;
        }
        if (!payload.TryGetProperty("bar", out var bar)
            || bar.ValueKind != JsonValueKind.Object)
        {
            error = "SetContextBar is missing bar";
            return false;
        }
        if (!_contextBars.TryGetValue(id, out var current) || revision >= current.Revision)
            _contextBars[id] = new Revisioned(revision, bar.Clone());
        return true;
    }

    private bool TrySetProfile(JsonElement payload, out string? error)
    {
        error = null;
        if (!payload.TryGetProperty("profile", out var profile)
            || profile.ValueKind != JsonValueKind.Object)
        {
            error = "SetPresentationProfile is missing profile";
            return false;
        }
        foreach (string property in new[]
                 {
                     "window_class", "pane_layout", "primary_surface", "active_surface"
                 })
        {
            if (!profile.TryGetProperty(property, out var value)
                || value.ValueKind != JsonValueKind.String)
            {
                error = $"SetPresentationProfile is missing {property}";
                return false;
            }
        }
        _profile = profile.Clone();
        return true;
    }

    private bool TryPresentOverlay(JsonElement payload, out string? error)
    {
        error = null;
        if (!TryIdentity(payload, out string id, out ulong revision, out error))
            return false;
        if (!_surfaces.TryGetValue(id, out var surface)
            || surface.Revision != revision)
        {
            error = "PresentOverlay does not match the current surface revision";
            return false;
        }
        if (!payload.TryGetProperty("overlay", out var overlay)
            || overlay.ValueKind != JsonValueKind.Object)
        {
            error = "PresentOverlay has an invalid payload";
            return false;
        }
        PresentedOverlay = payload.Clone();
        return true;
    }

    private static bool TryIdentity(
        JsonElement value,
        out string id,
        out ulong revision,
        out string? error)
    {
        id = "";
        revision = 0;
        error = null;
        if (!value.TryGetProperty("surface_id", out var surfaceId)
            || surfaceId.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(surfaceId.GetString()))
        {
            error = "presentation command is missing surface_id";
            return false;
        }
        if (!value.TryGetProperty("revision", out var revisionValue)
            || !revisionValue.TryGetUInt64(out revision))
        {
            error = "presentation command is missing revision";
            return false;
        }
        id = surfaceId.GetString()!;
        return true;
    }

    private string? ProfileString(string property) =>
        _profile is { } profile
        && profile.TryGetProperty(property, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
