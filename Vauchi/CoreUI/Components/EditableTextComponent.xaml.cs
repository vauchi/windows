// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Text.Json;
using Vauchi.Helpers;

namespace Vauchi.CoreUI.Components;

public sealed partial class EditableTextComponent : UserControl, IRenderable
{
    private bool _eventsWired;

    public EditableTextComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        string componentId = data.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";
        string label = data.TryGetProperty("label", out var l) ? l.GetString() ?? "" : "";
        string value = data.TryGetProperty("value", out var v) ? v.GetString() ?? "" : "";
        bool editing = data.TryGetProperty("editing", out var e) && e.GetBoolean();

        LabelText.Text = label;

        if (editing)
        {
            DisplayPanel.Visibility = Visibility.Collapsed;
            EditPanel.Visibility = Visibility.Visible;
            EditBox.Text = value;
        }
        else
        {
            DisplayPanel.Visibility = Visibility.Visible;
            EditPanel.Visibility = Visibility.Collapsed;
            DisplayValue.Text = value.Length > 0 ? value : "(empty)";
        }

        // Validation error
        if (data.TryGetProperty("validation_error", out var ve) && ve.ValueKind == JsonValueKind.String)
        {
            ValidationError.Text = ve.GetString() ?? "";
            ValidationError.Visibility = Visibility.Visible;
        }
        else
        {
            ValidationError.Visibility = Visibility.Collapsed;
        }

        if (!_eventsWired && onAction != null && componentId.Length > 0)
        {
            string capturedId = componentId;

            EditButton.Click += (_, _) =>
                onAction(ActionJson.ActionPressed($"{capturedId}_edit"));

            SaveButton.Click += (_, _) =>
                onAction(ActionJson.TextChanged(capturedId, EditBox.Text));

            CancelButton.Click += (_, _) =>
                onAction(ActionJson.ActionPressed($"{capturedId}_cancel"));

            _eventsWired = true;
        }

        AutomationProperties.SetName(EditBox, label);
        AutomationProperties.SetName(EditButton, $"Edit {label}");

        if (data.TryGetProperty("a11y", out var a11yElem))
        {
            if (a11yElem.TryGetProperty("label", out var labelElem))
            {
                var a11yLabel = labelElem.GetString();
                if (!string.IsNullOrEmpty(a11yLabel))
                {
                    AutomationProperties.SetName(EditBox, a11yLabel);
                    AutomationProperties.SetName(EditButton, a11yLabel);
                }
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
