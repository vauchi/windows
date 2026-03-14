// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Vauchi.CoreUI.Components;

public sealed partial class FieldListComponent : UserControl, IRenderable
{
    public event EventHandler<string>? ActionRequested;

    public FieldListComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data)
    {
        FieldContainer.Children.Clear();

        var visibilityMode = data.TryGetProperty("visibility_mode", out var vm)
            ? vm.GetString() ?? ""
            : "";

        if (!data.TryGetProperty("fields", out var fields)
            || fields.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var field in fields.EnumerateArray())
        {
            var fieldId = field.TryGetProperty("id", out var fid)
                ? fid.GetString() ?? ""
                : "";
            var label = field.TryGetProperty("label", out var lbl)
                ? lbl.GetString() ?? ""
                : "";
            var value = field.TryGetProperty("value", out var val)
                ? val.GetString() ?? ""
                : "";

            var grid = new Grid { Padding = new Thickness(0, 4, 0, 4) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var labelBlock = new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 80,
                Margin = new Thickness(0, 0, 12, 0),
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            };
            Grid.SetColumn(labelBlock, 0);
            grid.Children.Add(labelBlock);

            var valueBlock = new TextBlock
            {
                Text = value,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(valueBlock, 1);
            grid.Children.Add(valueBlock);

            if (visibilityMode == "ShowHide")
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var eyeButton = new Button
                {
                    Content = new SymbolIcon(Symbol.View),
                    Padding = new Thickness(4),
                    Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
                };

                var capturedFieldId = fieldId;
                eyeButton.Click += (_, _) =>
                {
                    ActionRequested?.Invoke(this,
                        JsonSerializer.Serialize(new
                        {
                            FieldVisibilityChanged = new
                            {
                                field_id = capturedFieldId,
                                group_id = (string?)null,
                                visible = true
                            }
                        }));
                };

                Grid.SetColumn(eyeButton, 2);
                grid.Children.Add(eyeButton);
            }

            FieldContainer.Children.Add(grid);
        }
    }
}
