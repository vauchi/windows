// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Text.Json;

namespace Vauchi.CoreUI.Components;

public sealed partial class FieldListComponent : UserControl, IRenderable
{
    public FieldListComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        FieldContainer.Children.Clear();

        if (!data.TryGetProperty("fields", out var fields) ||
            fields.ValueKind != JsonValueKind.Array)
        {
            if (data.TryGetProperty("title", out var title))
            {
                FieldContainer.Children.Add(
                    new TextBlock { Text = title.GetString() ?? "[FieldList]" });
            }
            return;
        }

        // ReadOnly mode: render label+value pairs
        foreach (var field in fields.EnumerateArray())
        {
            string label = field.TryGetProperty("label", out var lbl)
                ? lbl.GetString() ?? "" : "";
            string value = field.TryGetProperty("value", out var val)
                ? val.GetString() ?? "" : "";

            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 12,
            };

            row.Children.Add(new TextBlock
            {
                Text = label,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Width = 120,
                VerticalAlignment = VerticalAlignment.Center,
            });

            row.Children.Add(new TextBlock
            {
                Text = value,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
            });

            FieldContainer.Children.Add(row);
        }
    }
}
