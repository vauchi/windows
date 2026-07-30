// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Text.Json;

namespace Vauchi.CoreUI;

public sealed partial class PresentationSurface
{
    private FrameworkElement RenderGroup(JsonElement payload)
    {
        bool horizontal = String(payload, "axis") == "horizontal";
        var children = new StackPanel
        {
            Orientation = horizontal ? Orientation.Horizontal : Orientation.Vertical,
            Spacing = 8,
        };
        string label = String(payload, "label");
        if (label.Length > 0)
        {
            children.Children.Add(new TextBlock
            {
                Text = label,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            });
        }
        if (payload.TryGetProperty("children", out JsonElement nodes)
            && nodes.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement node in nodes.EnumerateArray())
                children.Children.Add(RenderNode(node));
        }
        ApplyAccessibility(children, payload);
        return children;
    }

    private FrameworkElement RenderList(JsonElement payload)
    {
        var list = new StackPanel { Spacing = 6 };
        string label = String(payload, "label");
        if (label.Length > 0)
        {
            list.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 18,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            });
        }
        if (payload.TryGetProperty("rows", out JsonElement rows)
            && rows.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement row in rows.EnumerateArray())
                list.Children.Add(RenderRow(row));
        }
        ApplyAccessibility(list, payload);
        return list;
    }

    private FrameworkElement RenderRow(JsonElement row)
    {
        var layout = new Grid { ColumnSpacing = 6 };
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        FrameworkElement content = RowContent(row);
        if (row.TryGetProperty("activation", out JsonElement activation)
            && activation.ValueKind == JsonValueKind.Object)
        {
            var button = ActionButton(activation);
            button.Content = content;
            button.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            ApplyAccessibility(button, row);
            content = button;
        }
        else
        {
            ApplyAccessibility(content, row);
        }
        Grid.SetColumn(content, 0);
        layout.Children.Add(content);

        if (row.TryGetProperty("secondary_actions", out JsonElement secondary)
            && secondary.ValueKind == JsonValueKind.Array
            && secondary.GetArrayLength() > 0)
        {
            var more = new Button
            {
                Content = "⋯",
                MinWidth = _minimumTargetSize,
                MinHeight = _minimumTargetSize,
            };
            var flyout = new MenuFlyout();
            foreach (JsonElement action in secondary.EnumerateArray())
            {
                string interaction = String(action, "interaction_id");
                var item = new MenuFlyoutItem
                {
                    Text = String(action, "label"),
                    IsEnabled = Boolean(action, "enabled", true),
                };
                AutomationProperties.SetAutomationId(item, interaction);
                AutomationProperties.SetName(
                    item,
                    String(action, "accessibility_label", String(action, "label")));
                item.Click += (_, _) => EmitAction(interaction);
                flyout.Items.Add(item);
            }
            more.Flyout = flyout;
            ApplyAccessibility(more, row);
            Grid.SetColumn(more, 1);
            layout.Children.Add(more);
        }

        var result = new StackPanel { Spacing = 4 };
        result.Children.Add(new Border
        {
            Child = layout,
            Padding = new Thickness(8),
            CornerRadius = new CornerRadius(8),
            Background = Boolean(row, "selected")
                ? new SolidColorBrush(Windows.UI.Color.FromArgb(32, 0, 120, 212))
                : null,
        });
        if (row.TryGetProperty("controls", out JsonElement controls)
            && controls.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement control in controls.EnumerateArray())
                result.Children.Add(RenderNode(control));
        }
        return result;
    }

    private static FrameworkElement RowContent(JsonElement row)
    {
        var text = new StackPanel { Spacing = 2 };
        text.Children.Add(new TextBlock
        {
            Text = String(row, "title"),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        });
        foreach (string property in new[] { "subtitle", "detail" })
        {
            string value = String(row, property);
            if (value.Length > 0)
            {
                text.Children.Add(new TextBlock
                {
                    Text = value,
                    Opacity = 0.7,
                    TextWrapping = TextWrapping.Wrap,
                });
            }
        }
        return text;
    }
}
