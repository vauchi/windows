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
    private string _componentId = "";
    private string _editActionId = "";
    private string _saveActionId = "";
    private string _cancelActionId = "";
    private Action<string>? _onAction;

    public EditableTextComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        _componentId = data.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";
        _onAction = onAction;
        _editActionId = data.GetProperty("edit_action_id").GetString() ?? "";
        _saveActionId = data.GetProperty("save_action_id").GetString() ?? "";
        _cancelActionId = data.GetProperty("cancel_action_id").GetString() ?? "";
        string label = data.TryGetProperty("label", out var l) ? l.GetString() ?? "" : "";
        string value = data.TryGetProperty("value", out var v) ? v.GetString() ?? "" : "";
        bool editing = data.TryGetProperty("editing", out var e) && e.GetBoolean();

        LabelText.Text = label;
        EditButton.Content = data.GetProperty("edit_text").GetString() ?? "";
        SaveButton.Content = data.GetProperty("save_text").GetString() ?? "";
        CancelButton.Content = data.GetProperty("cancel_text").GetString() ?? "";

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
            DisplayValue.Text = value;
        }

        if (data.TryGetProperty("validation_error", out var ve) && ve.ValueKind == JsonValueKind.String)
        {
            ValidationError.Text = ve.GetString() ?? "";
            ValidationError.Visibility = Visibility.Visible;
        }
        else
        {
            ValidationError.Visibility = Visibility.Collapsed;
        }

        if (!_eventsWired)
        {
            EditButton.Click += (_, _) =>
                _onAction?.Invoke(ActionJson.ActionPressed(_editActionId));

            SaveButton.Click += (_, _) =>
            {
                _onAction?.Invoke(ActionJson.TextChanged(_componentId, EditBox.Text));
                _onAction?.Invoke(ActionJson.ActionPressed(_saveActionId));
            };

            CancelButton.Click += (_, _) =>
                _onAction?.Invoke(ActionJson.ActionPressed(_cancelActionId));

            _eventsWired = true;
        }

        AutomationProperties.SetName(EditBox, label);
        AutomationProperties.SetName(EditButton, (string?)EditButton.Content ?? "");
        AutomationProperties.SetName(SaveButton, (string?)SaveButton.Content ?? "");
        AutomationProperties.SetName(CancelButton, (string?)CancelButton.Content ?? "");

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
