// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Vauchi.CoreUI.Components;

public sealed partial class InfoPanelComponent : UserControl, IRenderable
{
    public event EventHandler<string>? ActionRequested;

    public InfoPanelComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data)
    {
        if (data.TryGetProperty("title", out var title))
        {
            var titleStr = title.GetString() ?? "";
            if (!string.IsNullOrEmpty(titleStr))
            {
                PanelTitle.Text = titleStr;
                PanelTitle.Visibility = Visibility.Visible;
            }
            else
            {
                PanelTitle.Visibility = Visibility.Collapsed;
            }
        }

        ItemsContainer.Children.Clear();

        if (data.TryGetProperty("items", out var items)
            && items.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in items.EnumerateArray())
            {
                var row = new StackPanel { Spacing = 2 };

                if (item.TryGetProperty("title", out var itemTitle))
                {
                    var titleBlock = new TextBlock
                    {
                        Text = itemTitle.GetString() ?? ""
                    };
                    if (Application.Current.Resources.TryGetValue(
                            "BodyStrongTextBlockStyle", out var strongStyle)
                        && strongStyle is Style style)
                    {
                        titleBlock.Style = style;
                    }
                    row.Children.Add(titleBlock);
                }

                if (item.TryGetProperty("detail", out var itemDetail)
                    && itemDetail.ValueKind != JsonValueKind.Null)
                {
                    var detailBlock = new TextBlock
                    {
                        Text = itemDetail.GetString() ?? "",
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = (Microsoft.UI.Xaml.Media.Brush)
                            Application.Current.Resources["TextFillColorSecondaryBrush"]
                    };
                    row.Children.Add(detailBlock);
                }

                ItemsContainer.Children.Add(row);
            }
        }
    }
}
