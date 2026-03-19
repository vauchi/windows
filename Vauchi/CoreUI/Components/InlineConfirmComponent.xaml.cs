// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Text.Json;
using Vauchi.Helpers;
using Microsoft.UI.Xaml.Controls;

namespace Vauchi.CoreUI.Components;

public sealed partial class InlineConfirmComponent : UserControl, IRenderable
{
    private string _componentId = "";
    private Action<string>? _onAction;
    private bool _eventsWired;

    public InlineConfirmComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        _componentId = data.TryGetProperty("id", out var id)
            ? id.GetString() ?? ""
            : "";
        _onAction = onAction;

        if (data.TryGetProperty("warning", out var warning))
        {
            WarningText.Text = warning.GetString() ?? "";
        }

        if (data.TryGetProperty("confirm_text", out var confirmText))
        {
            ConfirmButton.Content = confirmText.GetString() ?? "Confirm";
        }

        if (data.TryGetProperty("cancel_text", out var cancelText))
        {
            CancelButton.Content = cancelText.GetString() ?? "Cancel";
        }

        var destructive = data.TryGetProperty("destructive", out var d) && d.GetBoolean();
        if (destructive)
        {
            ConfirmButton.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
        }

        AutomationProperties.SetName(WarningText, WarningText.Text);
        AutomationProperties.SetName(ConfirmButton,
            (string?)ConfirmButton.Content ?? "Confirm");
        AutomationProperties.SetName(CancelButton,
            (string?)CancelButton.Content ?? "Cancel");

        // Initially collapsed; tap warning text to expand
        ButtonPanel.Visibility = Visibility.Collapsed;

        if (!_eventsWired)
        {
            WarningText.Tapped += OnWarningTapped;

            ConfirmButton.Click += (_, _) =>
            {
                _onAction?.Invoke(ActionJson.ActionPressed($"{_componentId}_confirm"));
            };

            CancelButton.Click += (_, _) =>
            {
                ButtonPanel.Visibility = Visibility.Collapsed;
                _onAction?.Invoke(ActionJson.ActionPressed($"{_componentId}_cancel"));
            };

            _eventsWired = true;
        }
    }

    private void OnWarningTapped(object sender, TappedRoutedEventArgs e)
    {
        ButtonPanel.Visibility = Visibility.Visible;
    }
}
