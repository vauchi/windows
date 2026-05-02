// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Vauchi.Helpers;

namespace Vauchi.CoreUI.Components;

public sealed partial class DropdownComponent : UserControl, IRenderable
{
    public DropdownComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        string componentId = data.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";

        if (data.TryGetProperty("label", out var labelEl))
        {
            string? label = labelEl.GetString();
            if (!string.IsNullOrEmpty(label))
            {
                DropdownLabel.Text = label;
                DropdownLabel.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                AutomationProperties.SetName(DropdownControl, label);
            }
        }

        var optionIds = new List<string>();
        var optionLabels = new List<string>();
        if (data.TryGetProperty("options", out var optsEl) && optsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var opt in optsEl.EnumerateArray())
            {
                string id = opt.TryGetProperty("id", out var oIdEl) ? oIdEl.GetString() ?? "" : "";
                string label = opt.TryGetProperty("label", out var oLabelEl) ? oLabelEl.GetString() ?? "" : "";
                optionIds.Add(id);
                optionLabels.Add(label);
            }
        }

        DropdownControl.ItemsSource = optionLabels;

        string? selectedId = data.TryGetProperty("selected", out var selEl) && selEl.ValueKind == JsonValueKind.String
            ? selEl.GetString()
            : null;
        if (selectedId != null)
        {
            int idx = optionIds.IndexOf(selectedId);
            if (idx >= 0) DropdownControl.SelectedIndex = idx;
        }

        if (onAction != null && componentId.Length > 0)
        {
            string capturedId = componentId;
            DropdownControl.SelectionChanged += (_, args) =>
            {
                int idx = DropdownControl.SelectedIndex;
                if (idx >= 0 && idx < optionIds.Count)
                {
                    onAction(ActionJson.ListItemSelected(capturedId, optionIds[idx]));
                }
            };
        }

        if (data.TryGetProperty("a11y", out var a11yElem)
            && a11yElem.TryGetProperty("label", out var a11yLabelEl))
        {
            var a11yLabel = a11yLabelEl.GetString();
            if (!string.IsNullOrEmpty(a11yLabel))
                AutomationProperties.SetName(DropdownControl, a11yLabel);
        }
    }
}
