// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Text.Json;
using Vauchi.Helpers;
using Vauchi.UI;

namespace Vauchi.CoreUI.Components;

public sealed partial class FieldListComponent : UserControl, IRenderable
{
    public FieldListComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        string visibilityMode = data.TryGetProperty("visibility_mode", out var modeEl)
            ? modeEl.GetString() ?? "ReadOnly"
            : "ReadOnly";

        FieldContainer.Children.Clear();

        if (!data.TryGetProperty("fields", out var fields))
            return;

        foreach (var field in fields.EnumerateArray())
        {
            string fieldId = field.TryGetProperty("id", out var fIdEl) ? fIdEl.GetString() ?? "" : "";
            string label = field.TryGetProperty("label", out var lblEl) ? lblEl.GetString() ?? "" : "";
            string value = field.TryGetProperty("value", out var valEl) ? valEl.GetString() ?? "" : "";

            bool currentlyVisible = IsFieldVisible(field);

            UIElement row = BuildFieldRow(fieldId, label, value, currentlyVisible, visibilityMode, onAction);
            FieldContainer.Children.Add(row);
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

    private static bool IsFieldVisible(JsonElement field)
    {
        if (!field.TryGetProperty("visibility", out var vis))
            return true;

        if (vis.ValueKind == JsonValueKind.String)
        {
            string visStr = vis.GetString() ?? "";
            return visStr == "Shown";
        }

        // Object: {"Groups": [...]} — treated as visible
        return true;
    }

    private static UIElement BuildFieldRow(
        string fieldId,
        string label,
        string value,
        bool currentlyVisible,
        string visibilityMode,
        Action<string>? onAction)
    {
        var grid = new Grid { Padding = new Thickness(0, Tokens.Spacing.Xs, 0, Tokens.Spacing.Xs) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var labelBlock = new TextBlock
        {
            Text = label,
            Opacity = 0.6,
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var valueBlock = new TextBlock
        {
            Text = value,
            VerticalAlignment = VerticalAlignment.Center,
        };

        Grid.SetColumn(labelBlock, 0);
        Grid.SetColumn(valueBlock, 1);
        grid.Children.Add(labelBlock);
        grid.Children.Add(valueBlock);

        if (visibilityMode == "ShowHide" && onAction != null)
        {
            bool isVisible = currentlyVisible;
            string capturedFieldId = fieldId;

            var eyeButton = new Button
            {
                Content = isVisible ? "👁" : "👁\u0338",
                Padding = new Thickness(Tokens.Spacing.Xs),
                VerticalAlignment = VerticalAlignment.Center,
            };

            AutomationProperties.SetName(eyeButton, isVisible ? $"Hide {label}" : $"Show {label}");

            eyeButton.Click += (_, _) =>
            {
                isVisible = !isVisible;
                eyeButton.Content = isVisible ? "👁" : "👁̸";
                AutomationProperties.SetName(eyeButton, isVisible ? $"Hide {label}" : $"Show {label}");
                onAction(ActionJson.FieldVisibilityChanged(capturedFieldId, null, isVisible));
            };

            Grid.SetColumn(eyeButton, 2);
            grid.Children.Add(eyeButton);
        }
        else if (visibilityMode == "PerGroup")
        {
            // Show which groups can see this field (read-only text)
            var groupsNote = new TextBlock
            {
                Text = "Groups",
                Opacity = 0.4,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(groupsNote, 2);
            grid.Children.Add(groupsNote);
        }

        return grid;
    }
}
