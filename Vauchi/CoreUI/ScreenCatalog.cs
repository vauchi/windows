// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Vauchi.CoreUI;

/// <summary>
/// One screen of Core's catalog fixture, reduced through the production
/// <see cref="PresentationState"/> so the renderer shows exactly what the
/// app would after receiving the same batch. <see cref="Error"/> is set
/// when the batch did not reduce; the entry is kept so one bad screen in
/// a Core-main catalog cannot abort the whole run.
/// </summary>
public sealed record ScreenCatalogEntry(
    string CodeId,
    string Title,
    string Locale,
    PresentationState State,
    string? Error)
{
    /// <summary>
    /// The output file name is derived from a value Core controls, so
    /// anything outside a portable file-name alphabet becomes an
    /// underscore and the name can never leave the output directory.
    /// </summary>
    public string FileStem
    {
        get
        {
            var stem = new StringBuilder(CodeId.Length);
            foreach (char character in CodeId)
            {
                stem.Append(char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_'
                    ? character
                    : '_');
            }
            return stem.Length == 0 ? "screen" : stem.ToString().TrimStart('.');
        }
    }

    public string? SurfaceTitle =>
        State.ActiveSurfaceId is { } id
        && State.Surface(id) is { } surface
        && surface.TryGetProperty("title", out JsonElement title)
        && title.ValueKind == JsonValueKind.String
            ? title.GetString()
            : null;
}

/// <summary>
/// Parses <c>screen_catalog_v1.json</c> (schema_version 1: a
/// <c>screens</c> array of <c>code_id</c>, <c>title</c>, <c>locale</c>
/// and a <c>commands</c> batch serialized exactly as Core emits it).
/// Kept free of WinUI types so the unit tests cover it.
/// </summary>
public static class ScreenCatalog
{
    public const string RenderSwitch = "--render-catalog";
    private const int SupportedSchemaVersion = 1;

    public static IReadOnlyList<ScreenCatalogEntry> Parse(string json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new FormatException($"screen catalog is not JSON: {exception.Message}");
        }

        using (document)
        {
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new FormatException("screen catalog must be an object");
            if (!root.TryGetProperty("schema_version", out JsonElement version)
                || !version.TryGetInt32(out int schemaVersion)
                || schemaVersion != SupportedSchemaVersion)
            {
                throw new FormatException(
                    $"screen catalog schema_version must be {SupportedSchemaVersion}");
            }
            if (!root.TryGetProperty("screens", out JsonElement screens)
                || screens.ValueKind != JsonValueKind.Array)
            {
                throw new FormatException("screen catalog is missing screens");
            }

            var entries = new List<ScreenCatalogEntry>();
            int position = 0;
            foreach (JsonElement screen in screens.EnumerateArray())
                entries.Add(ParseEntry(screen, position++));
            return entries;
        }
    }

    public static bool TryParseLaunchArguments(
        string[] arguments,
        out string catalogPath,
        out string outputDirectory)
    {
        catalogPath = "";
        outputDirectory = "";
        int index = Array.IndexOf(arguments, RenderSwitch);
        if (index < 0 || index + 2 >= arguments.Length)
            return false;
        catalogPath = arguments[index + 1];
        outputDirectory = arguments[index + 2];
        return catalogPath.Length > 0 && outputDirectory.Length > 0;
    }

    private static ScreenCatalogEntry ParseEntry(JsonElement screen, int position)
    {
        string codeId = StringProperty(screen, "code_id") ?? $"screen-{position}";
        string title = StringProperty(screen, "title") ?? codeId;
        string locale = StringProperty(screen, "locale") ?? "en";
        var state = new PresentationState();
        string? error = null;

        if (screen.ValueKind != JsonValueKind.Object
            || !screen.TryGetProperty("commands", out JsonElement commands)
            || commands.ValueKind != JsonValueKind.Array)
        {
            error = "screen is missing its commands batch";
        }
        else if (!state.TryApplyEnvelope(
                     "{\"commands\":" + commands.GetRawText() + "}",
                     out _,
                     out string? reduceError))
        {
            error = reduceError ?? "batch did not reduce";
        }

        return new ScreenCatalogEntry(codeId, title, locale, state, error);
    }

    private static string? StringProperty(JsonElement value, string property) =>
        value.ValueKind == JsonValueKind.Object
        && value.TryGetProperty(property, out JsonElement element)
        && element.ValueKind == JsonValueKind.String
        && element.GetString() is { Length: > 0 } text
            ? text
            : null;
}
