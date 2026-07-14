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

namespace Vauchi.CoreUI.Components;

public sealed partial class InlineConfirmComponent : UserControl, IRenderable
{
    private string _confirmActionId = "";
    private string _cancelActionId = "";
    private Action<string>? _onAction;
    private bool _eventsWired;

    public InlineConfirmComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        _onAction = onAction;
        _confirmActionId = data.GetProperty("confirm_action_id").GetString() ?? "";
        _cancelActionId = data.GetProperty("cancel_action_id").GetString() ?? "";

        if (data.TryGetProperty("warning", out var warning))
        {
            WarningText.Text = warning.GetString() ?? "";
        }

        ConfirmButton.Content = data.GetProperty("confirm_text").GetString() ?? "";
        CancelButton.Content = data.GetProperty("cancel_text").GetString() ?? "";

        var destructive = data.TryGetProperty("destructive", out var d) && d.GetBoolean();
        if (destructive)
        {
            ConfirmButton.Foreground = new SolidColorBrush(ThemeColors.Destructive);
        }

        AutomationProperties.SetName(WarningText, WarningText.Text);
        AutomationProperties.SetName(ConfirmButton, (string?)ConfirmButton.Content ?? "");
        AutomationProperties.SetName(CancelButton, (string?)CancelButton.Content ?? "");

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

        if (!_eventsWired)
        {
            ConfirmButton.Click += (_, _) =>
            {
                _onAction?.Invoke(ActionJson.ActionPressed(_confirmActionId));
            };

            CancelButton.Click += (_, _) =>
            {
                _onAction?.Invoke(ActionJson.ActionPressed(_cancelActionId));
            };

            _eventsWired = true;
        }
    }
}
