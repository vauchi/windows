// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Text.Json;
using Vauchi.Helpers;

namespace Vauchi.CoreUI.Components;

public sealed partial class ActionListComponent : UserControl, IRenderable
{
    public ActionListComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        ActionContainer.Children.Clear();

        if (!data.TryGetProperty("items", out var items) ||
            items.ValueKind != JsonValueKind.Array)
            return;

        foreach (var item in items.EnumerateArray())
        {
            string itemId = item.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";
            string label = item.TryGetProperty("label", out var lbl) ? lbl.GetString() ?? "" : "";
            string? detail = item.TryGetProperty("detail", out var det) ? det.GetString() : null;

            var content = new StackPanel { Spacing = 2 };
            content.Children.Add(new TextBlock { Text = label });
            if (detail != null)
            {
                content.Children.Add(new TextBlock
                {
                    Text = detail,
                    Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
                    Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                });
            }

            var btn = new Button
            {
                Content = content,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
            };

            AutomationProperties.SetName(btn, label);

            if (onAction != null)
            {
                string capturedId = itemId;
                btn.Click += (_, _) => onAction(ActionJson.ActionPressed(capturedId));
            }

            ActionContainer.Children.Add(btn);
        }
    }
}
