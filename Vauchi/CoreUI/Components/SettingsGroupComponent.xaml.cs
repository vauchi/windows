// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Vauchi.CoreUI.Components;

public sealed partial class SettingsGroupComponent : UserControl, IRenderable
{
    public event EventHandler<string>? ActionRequested;

    private string _componentId = "";

    public SettingsGroupComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data)
    {
        _componentId = data.TryGetProperty("id", out var id)
            ? id.GetString() ?? ""
            : "";

        if (data.TryGetProperty("label", out var label))
        {
            GroupLabel.Text = label.GetString() ?? "";
        }

        ItemsContainer.Children.Clear();

        if (!data.TryGetProperty("items", out var items)
            || items.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in items.EnumerateArray())
        {
            var itemId = item.TryGetProperty("id", out var iid)
                ? iid.GetString() ?? ""
                : "";
            var itemLabel = item.TryGetProperty("label", out var lbl)
                ? lbl.GetString() ?? ""
                : "";

            if (!item.TryGetProperty("kind", out var kind)
                || kind.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            UIElement? element = null;

            foreach (var variant in kind.EnumerateObject())
            {
                element = variant.Name switch
                {
                    "Toggle" => CreateToggle(itemId, itemLabel, variant.Value),
                    "Value" => CreateValueRow(itemLabel, variant.Value),
                    "Link" => CreateLinkButton(itemId, itemLabel, variant.Value),
                    "Destructive" => CreateDestructiveButton(itemId, variant.Value),
                    _ => null
                };
                break; // externally-tagged: only one variant
            }

            if (element != null)
            {
                var container = new Border { Padding = new Thickness(16, 8, 16, 8) };
                container.Child = element;
                ItemsContainer.Children.Add(container);
            }
        }
    }

    private UIElement CreateToggle(string itemId, string label, JsonElement value)
    {
        var enabled = value.TryGetProperty("enabled", out var e) && e.GetBoolean();
        var toggle = new ToggleSwitch
        {
            Header = label,
            IsOn = enabled
        };

        var capturedId = itemId;
        toggle.Toggled += (_, _) =>
        {
            ActionRequested?.Invoke(this,
                JsonSerializer.Serialize(new
                {
                    SettingsToggled = new
                    {
                        component_id = _componentId,
                        item_id = capturedId
                    }
                }));
        };

        return toggle;
    }

    private static UIElement CreateValueRow(string label, JsonElement value)
    {
        var val = value.TryGetProperty("value", out var v)
            ? v.GetString() ?? ""
            : "";

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var labelBlock = new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(labelBlock, 0);
        grid.Children.Add(labelBlock);

        var valueBlock = new TextBlock
        {
            Text = val,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
        };
        Grid.SetColumn(valueBlock, 1);
        grid.Children.Add(valueBlock);

        return grid;
    }

    private UIElement CreateLinkButton(string itemId, string label, JsonElement value)
    {
        var hasDetail = value.TryGetProperty("detail", out var detail)
                        && detail.ValueKind != JsonValueKind.Null;

        var panel = new StackPanel { Spacing = 2 };
        panel.Children.Add(new TextBlock { Text = label });

        if (hasDetail)
        {
            panel.Children.Add(new TextBlock
            {
                Text = detail.GetString() ?? "",
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"]
            });
        }

        var button = new Button
        {
            Content = panel,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Background = new SolidColorBrush(Colors.Transparent)
        };

        var capturedId = itemId;
        button.Click += (_, _) =>
        {
            ActionRequested?.Invoke(this,
                JsonSerializer.Serialize(new
                {
                    ListItemSelected = new
                    {
                        component_id = _componentId,
                        item_id = capturedId
                    }
                }));
        };

        return button;
    }

    private UIElement CreateDestructiveButton(string itemId, JsonElement value)
    {
        var buttonLabel = value.TryGetProperty("label", out var lbl)
            ? lbl.GetString() ?? ""
            : "";

        var button = new Button
        {
            Content = buttonLabel,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Background = new SolidColorBrush(Colors.Transparent),
            Foreground = new SolidColorBrush(Colors.Red)
        };

        var capturedId = itemId;
        button.Click += (_, _) =>
        {
            ActionRequested?.Invoke(this,
                JsonSerializer.Serialize(new
                {
                    ListItemSelected = new
                    {
                        component_id = _componentId,
                        item_id = capturedId
                    }
                }));
        };

        return button;
    }
}
