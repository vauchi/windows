// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Vauchi.CoreUI.Components;

public sealed partial class CardPreviewComponent : UserControl, IRenderable
{
    public event EventHandler<string>? ActionRequested;

    public CardPreviewComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data)
    {
        CardContainer.Children.Clear();

        // Name
        if (data.TryGetProperty("name", out var name))
        {
            var nameBlock = new TextBlock
            {
                Text = name.GetString() ?? ""
            };
            if (Application.Current.Resources.TryGetValue(
                    "SubtitleTextBlockStyle", out var style)
                && style is Style s)
            {
                nameBlock.Style = s;
            }
            CardContainer.Children.Add(nameBlock);
        }

        // Group tabs
        if (data.TryGetProperty("group_views", out var groupViews)
            && groupViews.ValueKind == JsonValueKind.Array
            && groupViews.GetArrayLength() > 1)
        {
            var selectedGroup = data.TryGetProperty("selected_group", out var sg)
                                && sg.ValueKind != JsonValueKind.Null
                ? sg.GetString()
                : null;

            var tabBar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4
            };

            foreach (var group in groupViews.EnumerateArray())
            {
                var groupName = group.TryGetProperty("group_name", out var gn)
                    ? gn.GetString() ?? ""
                    : "";
                var displayName = group.TryGetProperty("display_name", out var dn)
                    ? dn.GetString() ?? ""
                    : "";

                var isSelected = groupName == selectedGroup;
                var tabButton = new Button
                {
                    Content = displayName,
                    Padding = new Thickness(12, 4, 12, 4),
                    FontWeight = isSelected
                        ? Microsoft.UI.Text.FontWeights.SemiBold
                        : Microsoft.UI.Text.FontWeights.Normal
                };

                var capturedGroupName = groupName;
                tabButton.Click += (_, _) =>
                {
                    ActionRequested?.Invoke(this,
                        JsonSerializer.Serialize(new
                        {
                            GroupViewSelected = new { group_name = capturedGroupName }
                        }));
                };

                tabBar.Children.Add(tabButton);
            }

            CardContainer.Children.Add(tabBar);
        }

        // Fields
        if (data.TryGetProperty("fields", out var fields)
            && fields.ValueKind == JsonValueKind.Array)
        {
            foreach (var field in fields.EnumerateArray())
            {
                var label = field.TryGetProperty("label", out var lbl)
                    ? lbl.GetString() ?? ""
                    : "";
                var value = field.TryGetProperty("value", out var val)
                    ? val.GetString() ?? ""
                    : "";

                var row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var labelBlock = new TextBlock
                {
                    Text = label,
                    MinWidth = 80,
                    Margin = new Thickness(0, 0, 12, 0),
                    Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
                };
                Grid.SetColumn(labelBlock, 0);
                row.Children.Add(labelBlock);

                var valueBlock = new TextBlock
                {
                    Text = value,
                    TextWrapping = TextWrapping.Wrap
                };
                Grid.SetColumn(valueBlock, 1);
                row.Children.Add(valueBlock);

                CardContainer.Children.Add(row);
            }
        }
    }
}
