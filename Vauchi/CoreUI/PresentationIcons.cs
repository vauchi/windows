// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Text.Json;

namespace Vauchi.CoreUI;

/// <summary>The element that shows an <c>icon_token</c>.</summary>
public static class PresentationIcons
{
    /// <summary>The size a <see cref="FontIcon"/> draws at by default.</summary>
    public const double DefaultSize = 20;

    /// <summary>
    /// Vauchi's own drawing for a bundled pictogram token; otherwise the
    /// Segoe glyph, which falls back to <see cref="NavigationIcons.FallbackGlyph"/>.
    /// </summary>
    public static FrameworkElement Element(string? token, double size = DefaultSize) =>
        PictogramElement(token, size)
        ?? new FontIcon { Glyph = NavigationIcons.Glyph(token), FontSize = size };

    /// <summary>Null unless the token names a bundled pictogram.</summary>
    public static FrameworkElement? PictogramElement(string? token, double size) =>
        Pictograms.Resolve(token) is { } paths ? new PictogramIcon(paths, size) : null;

    /// <summary>
    /// The node's own content led by its pictogram, or the content alone
    /// when its <c>icon_token</c> names none: rows and statuses drew no icon
    /// before pictograms (#473), so no other token adds one.
    /// </summary>
    public static FrameworkElement LeadWithPictogram(JsonElement payload, FrameworkElement content)
    {
        if (Pictograms.ForNode(payload) is not { } paths)
            return content;
        var pictogram = new PictogramIcon(paths, Pictograms.CanvasSize)
        {
            VerticalAlignment = VerticalAlignment.Center,
        };
        var layout = new Grid { ColumnSpacing = 12 };
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(content, 1);
        layout.Children.Add(pictogram);
        layout.Children.Add(content);
        return layout;
    }
}
