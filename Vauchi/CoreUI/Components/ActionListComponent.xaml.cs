// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Vauchi.CoreUI.Components;

public sealed partial class ActionListComponent : UserControl, IRenderable
{
    public event EventHandler<string>? ActionRequested;

    public ActionListComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data)
    {
        ActionContainer.Children.Clear();

        if (!data.TryGetProperty("items", out var items)
            || items.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in items.EnumerateArray())
        {
            var itemId = item.TryGetProperty("id", out var id)
                ? id.GetString() ?? ""
                : "";
            var label = item.TryGetProperty("label", out var lbl)
                ? lbl.GetString() ?? ""
                : "";
            var hasDetail = item.TryGetProperty("detail", out var detail)
                            && detail.ValueKind != JsonValueKind.Null;

            var button = new Button
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(16, 12, 16, 12)
            };
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(button, label);

            if (hasDetail)
            {
                var panel = new StackPanel { Spacing = 2 };
                panel.Children.Add(new TextBlock { Text = label });
                panel.Children.Add(new TextBlock
                {
                    Text = detail.GetString() ?? "",
                    Foreground = (Microsoft.UI.Xaml.Media.Brush)
                        Application.Current.Resources["TextFillColorSecondaryBrush"],
                    Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"]
                });
                button.Content = panel;
            }
            else
            {
                button.Content = label;
            }

            var capturedId = itemId;
            button.Click += (_, _) =>
            {
                ActionRequested?.Invoke(this,
                    JsonSerializer.Serialize(new
                    {
                        ActionPressed = new { action_id = capturedId }
                    }));
            };

            ActionContainer.Children.Add(button);
        }
    }
}
