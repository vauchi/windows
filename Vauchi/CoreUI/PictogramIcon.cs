// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Vauchi.CoreUI;

/// <summary>
/// Draws a bundled pictogram at icon size in the inherited foreground, so it
/// follows theme, disabled state and high contrast the way a font glyph does.
/// </summary>
/// <remarks>
/// A <see cref="PathIcon"/> only fills its geometry, and these drawings are
/// mostly strokes, hence one <see cref="Path"/> per SVG path. It is a
/// <see cref="UserControl"/> rather than a bare panel because
/// <c>Control.Foreground</c> inherits down the tree and a panel has none.
/// </remarks>
public sealed partial class PictogramIcon : UserControl
{
    public PictogramIcon(IReadOnlyList<PictogramPath> paths, double size)
    {
        var canvas = new Grid
        {
            Width = Pictograms.CanvasSize,
            Height = Pictograms.CanvasSize,
        };
        foreach (PictogramPath path in paths)
            canvas.Children.Add(DrawnPath(path));

        Content = new Viewbox { Width = size, Height = size, Child = canvas };
        IsTabStop = false;
        // The adjacent label already names the row or action.
        AutomationProperties.SetAccessibilityView(this, AccessibilityView.Raw);
    }

    private Path DrawnPath(PictogramPath path)
    {
        var shape = new Path
        {
            Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), path.Data),
            StrokeThickness = path.StrokeThickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
        };
        if (path.Stroked)
            shape.SetBinding(Shape.StrokeProperty, ForegroundBinding());
        if (path.Filled)
            shape.SetBinding(Shape.FillProperty, ForegroundBinding());
        return shape;
    }

    private Binding ForegroundBinding() => new()
    {
        Source = this,
        Path = new PropertyPath(nameof(Foreground)),
    };
}
