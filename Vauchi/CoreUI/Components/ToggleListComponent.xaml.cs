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

public sealed partial class ToggleListComponent : UserControl, IRenderable
{
    public ToggleListComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        // Keep only the header label, clear dynamic items
        while (ToggleContainer.Children.Count > 1)
            ToggleContainer.Children.RemoveAt(ToggleContainer.Children.Count - 1);

        string componentId = data.TryGetProperty("id", out var cid) ? cid.GetString() ?? "" : "";

        if (data.TryGetProperty("label", out var label) && label.GetString() is string lbl && lbl.Length > 0)
        {
            HeaderLabel.Text = lbl;
            HeaderLabel.Visibility = Visibility.Visible;
        }

        if (!data.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
            return;

        foreach (var item in items.EnumerateArray())
        {
            string itemId = item.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";
            string itemLabel = item.TryGetProperty("label", out var il) ? il.GetString() ?? "" : "";
            bool selected = item.TryGetProperty("selected", out var sel) && sel.GetBoolean();
            string? subtitle = item.TryGetProperty("subtitle", out var sub) ? sub.GetString() : null;

            var toggle = new ToggleSwitch
            {
                Header = itemLabel,
                IsOn = selected,
            };

            AutomationProperties.SetName(toggle, itemLabel);

            if (onAction != null)
            {
                string capturedItemId = itemId;
                string capturedComponentId = componentId;
                toggle.Toggled += (_, _) =>
                    onAction(ActionJson.ItemToggled(capturedComponentId, capturedItemId));
            }

            ToggleContainer.Children.Add(toggle);

            if (subtitle != null)
            {
                ToggleContainer.Children.Add(new TextBlock
                {
                    Text = subtitle,
                    Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
                    Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                    Margin = new Thickness(0, -Tokens.Spacing.Xs, 0, 0),
                });
            }
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
