// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Text.Json;

namespace Vauchi.CoreUI.Components;

public sealed partial class CardPreviewComponent : UserControl, IRenderable
{
    public CardPreviewComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        CardContent.Children.Clear();

        // Card title from "name"
        if (data.TryGetProperty("name", out var name))
        {
            CardContent.Children.Add(new TextBlock
            {
                Text = name.GetString() ?? "[CardPreview]",
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                FontSize = 16,
            });
        }

        // Field rows inside the bordered card
        if (data.TryGetProperty("fields", out var fields) &&
            fields.ValueKind == JsonValueKind.Array)
        {
            foreach (var field in fields.EnumerateArray())
            {
                string label = field.TryGetProperty("label", out var lbl)
                    ? lbl.GetString() ?? "" : "";
                string value = field.TryGetProperty("value", out var val)
                    ? val.GetString() ?? "" : "";

                var row = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                };

                row.Children.Add(new TextBlock
                {
                    Text = label,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Width = 100,
                    VerticalAlignment = VerticalAlignment.Center,
                });

                row.Children.Add(new TextBlock
                {
                    Text = value,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                });

                CardContent.Children.Add(row);
            }
        }
    }
}
