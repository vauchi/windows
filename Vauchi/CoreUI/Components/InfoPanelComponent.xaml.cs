// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Text.Json;

namespace Vauchi.CoreUI.Components;

public sealed partial class InfoPanelComponent : UserControl, IRenderable
{
    public InfoPanelComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        ItemsContainer.Children.Clear();

        PanelTitle.Text = data.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "";

        if (!data.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
            return;

        foreach (var item in items.EnumerateArray())
        {
            string itemTitle = item.TryGetProperty("title", out var it) ? it.GetString() ?? "" : "";
            string detail = item.TryGetProperty("detail", out var det) ? det.GetString() ?? "" : "";

            var row = new StackPanel { Spacing = 2 };
            row.Children.Add(new TextBlock
            {
                Text = itemTitle,
                Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"],
            });
            row.Children.Add(new TextBlock
            {
                Text = detail,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            });

            ItemsContainer.Children.Add(row);
        }

        if (data.TryGetProperty("a11y", out var a11yElem))
        {
            if (a11yElem.TryGetProperty("label", out var labelElem))
            {
                var a11yLabel = labelElem.GetString();
                if (!string.IsNullOrEmpty(a11yLabel))
                    AutomationProperties.SetName(this, a11yLabel);
            }
            if (a11yElem.TryGetProperty("hint", out var hintElem))
            {
                var hint = hintElem.GetString();
                if (!string.IsNullOrEmpty(hint))
                    AutomationProperties.SetHelpText(this, hint);
            }
        }
    }
}
