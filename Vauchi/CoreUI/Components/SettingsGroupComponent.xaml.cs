// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Text.Json;
using Vauchi.CoreUI;
using Vauchi.Helpers;
using Vauchi.UI;

namespace Vauchi.CoreUI.Components;

public sealed partial class SettingsGroupComponent : UserControl, IRenderable
{
    public SettingsGroupComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        string componentId = data.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";

        GroupLabel.Text = data.TryGetProperty("label", out var labelEl)
            ? (labelEl.GetString() ?? "").ToUpperInvariant()
            : "";

        ItemsContainer.Children.Clear();

        if (!data.TryGetProperty("items", out var items))
            return;

        foreach (var item in items.EnumerateArray())
        {
            string itemId = item.TryGetProperty("id", out var iIdEl) ? iIdEl.GetString() ?? "" : "";
            string itemLabel = item.TryGetProperty("label", out var iLabelEl) ? iLabelEl.GetString() ?? "" : "";

            if (!item.TryGetProperty("kind", out var kind))
                continue;

            UIElement row = BuildItemRow(componentId, itemId, itemLabel, kind, onAction);
            ItemsContainer.Children.Add(row);
        }
    }

    private static UIElement BuildItemRow(
        string componentId,
        string itemId,
        string label,
        JsonElement kind,
        Action<string>? onAction)
    {
        var container = new StackPanel
        {
            Padding = new Thickness(Tokens.Spacing.Md, Tokens.SpacingDirection.ListItemInlineStart, Tokens.Spacing.Md, Tokens.SpacingDirection.ListItemInlineEnd),
        };

        // Determine kind by first property name (externally-tagged enum)
        foreach (var prop in kind.EnumerateObject())
        {
            switch (prop.Name)
            {
                case "Toggle":
                {
                    bool enabled = prop.Value.TryGetProperty("enabled", out var enEl) && enEl.GetBoolean();
                    var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = Tokens.Spacing.Sm };
                    var labelBlock = new TextBlock
                    {
                        Text = label,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Left,
                    };
                    var toggle = new ToggleSwitch
                    {
                        IsOn = enabled,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        OffContent = "",
                        OnContent = "",
                    };

                    AutomationProperties.SetName(toggle, label);

                    if (onAction != null)
                    {
                        string capturedComponent = componentId;
                        string capturedItem = itemId;
                        toggle.Toggled += (_, _) =>
                            onAction(ActionJson.SettingsToggled(capturedComponent, capturedItem));
                    }

                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    Grid.SetColumn(labelBlock, 0);
                    Grid.SetColumn(toggle, 1);
                    grid.Children.Add(labelBlock);
                    grid.Children.Add(toggle);
                    container.Children.Add(grid);
                    break;
                }

                case "Value":
                {
                    string value = prop.Value.TryGetProperty("value", out var valEl)
                        ? valEl.GetString() ?? ""
                        : "";
                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var labelBlock = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center };
                    var valueBlock = new TextBlock
                    {
                        Text = value,
                        Opacity = 0.6,
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    Grid.SetColumn(labelBlock, 0);
                    Grid.SetColumn(valueBlock, 1);
                    grid.Children.Add(labelBlock);
                    grid.Children.Add(valueBlock);
                    container.Children.Add(grid);

                    if (onAction != null)
                    {
                        string capturedComponent = componentId;
                        string capturedItem = itemId;
                        container.PointerPressed += (_, _) =>
                            onAction(ActionJson.ListItemSelected(capturedComponent, capturedItem));
                    }
                    break;
                }

                case "Link":
                {
                    string detail = prop.Value.TryGetProperty("detail", out var detEl)
                        ? detEl.GetString() ?? ""
                        : "";
                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var labelBlock = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center };
                    var detailBlock = new TextBlock
                    {
                        Text = detail,
                        Opacity = 0.6,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    var chevron = new TextBlock
                    {
                        Text = ">",
                        Opacity = 0.4,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(Tokens.Spacing.Xs, 0, 0, 0),
                    };

                    Grid.SetColumn(labelBlock, 0);
                    Grid.SetColumn(detailBlock, 1);
                    Grid.SetColumn(chevron, 2);
                    grid.Children.Add(labelBlock);
                    grid.Children.Add(detailBlock);
                    grid.Children.Add(chevron);
                    container.Children.Add(grid);

                    if (onAction != null)
                    {
                        string capturedComponent = componentId;
                        string capturedItem = itemId;
                        container.PointerPressed += (_, _) =>
                            onAction(ActionJson.ListItemSelected(capturedComponent, capturedItem));
                    }
                    break;
                }

                case "Destructive":
                {
                    string btnLabel = prop.Value.TryGetProperty("label", out var lblEl)
                        ? lblEl.GetString() ?? label
                        : label;
                    var btn = new Button
                    {
                        Content = btnLabel,
                        Foreground = new SolidColorBrush(ThemeColors.Destructive),
                        HorizontalAlignment = HorizontalAlignment.Left,
                    };
                    AutomationProperties.SetName(btn, btnLabel);

                    if (onAction != null)
                    {
                        string capturedComponent = componentId;
                        string capturedItem = itemId;
                        btn.Click += (_, _) =>
                            onAction(ActionJson.ListItemSelected(capturedComponent, capturedItem));
                    }

                    container.Children.Add(btn);
                    break;
                }
            }

            // Only process first property (externally-tagged)
            break;
        }

        return container;
    }
}
