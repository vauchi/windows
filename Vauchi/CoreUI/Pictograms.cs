// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;

namespace Vauchi.CoreUI;

/// <summary>
/// One shape of a pictogram, in the 24x24 drawing space, drawn in the
/// foreground colour. <see cref="Data"/> is SVG path syntax, which is also
/// valid XAML path markup.
/// </summary>
public sealed record PictogramPath(string Data, bool Stroked, double StrokeThickness, bool Filled);

/// <summary>
/// Resolves a <c>pictogram.&lt;group&gt;.&lt;name&gt;</c> icon token into
/// Vauchi's own drawing, bundled from <c>Assets/Pictograms/&lt;group&gt;/&lt;name&gt;.svg</c>.
/// </summary>
/// <remarks>
/// Segoe Fluent Icons has no glyph for these drawings, so they ship as
/// embedded SVGs and are resolved by token alone: a new drawing needs no
/// code here (ADR-066).
/// </remarks>
public static class Pictograms
{
    public const double CanvasSize = 24;

    private const string ResourcePrefix = "Vauchi.Pictograms/";
    private const string TokenPrefix = "pictogram.";
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlyList<PictogramPath>>> Bundled =
        new(() => LoadBundled(typeof(Pictograms).Assembly));

    /// <summary>Null for every token that is not a bundled pictogram.</summary>
    public static IReadOnlyList<PictogramPath>? Resolve(string? token) =>
        token is not null && Bundled.Value.TryGetValue(token, out IReadOnlyList<PictogramPath>? paths)
            ? paths
            : null;

    /// <summary>The pictogram a presentation node's <c>icon_token</c> names, if any.</summary>
    public static IReadOnlyList<PictogramPath>? ForNode(JsonElement payload) =>
        payload.TryGetProperty("icon_token", out JsonElement token)
        && token.ValueKind == JsonValueKind.String
            ? Resolve(token.GetString())
            : null;

    private static IReadOnlyDictionary<string, IReadOnlyList<PictogramPath>> LoadBundled(Assembly assembly)
    {
        var byToken = new Dictionary<string, IReadOnlyList<PictogramPath>>(StringComparer.Ordinal);
        foreach (string resource in assembly.GetManifestResourceNames())
        {
            string? token = TokenFor(resource);
            if (token is null)
                continue;
            using Stream? stream = assembly.GetManifestResourceStream(resource);
            if (stream is not null)
                byToken[token] = Parse(stream);
        }
        return byToken;
    }

    // MSBuild's %(RecursiveDir) separator follows the build host, so the
    // logical name may carry either slash.
    private static string? TokenFor(string resource)
    {
        if (!resource.StartsWith(ResourcePrefix, StringComparison.Ordinal)
            || !resource.EndsWith(".svg", StringComparison.Ordinal))
            return null;
        string relative = resource[ResourcePrefix.Length..^".svg".Length].Replace('\\', '/');
        string[] parts = relative.Split('/');
        return parts.Length == 2 ? TokenPrefix + parts[0] + "." + parts[1] : null;
    }

    private static IReadOnlyList<PictogramPath> Parse(Stream svg)
    {
        XElement root = XDocument.Load(svg).Root
            ?? throw new InvalidDataException("pictogram has no root element");
        string rootFill = (string?)root.Attribute("fill") ?? "black";
        string rootStroke = (string?)root.Attribute("stroke") ?? "none";
        double rootStrokeWidth = Number((string?)root.Attribute("stroke-width"), 1);

        return root.Descendants(Svg + "path")
            .Select(path => new PictogramPath(
                (string?)path.Attribute("d") ?? "",
                Stroked: ((string?)path.Attribute("stroke") ?? rootStroke) != "none",
                StrokeThickness: Number((string?)path.Attribute("stroke-width"), rootStrokeWidth),
                Filled: ((string?)path.Attribute("fill") ?? rootFill) != "none"))
            .ToList();
    }

    private static double Number(string? value, double fallback) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double number)
            ? number
            : fallback;
}
