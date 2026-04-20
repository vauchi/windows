// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Text.Json;
using Vauchi.CoreUI;
using Vauchi.Helpers;
using Vauchi.Services;

namespace Vauchi.CoreUI.Components;

public sealed partial class InlineConfirmComponent : UserControl, IRenderable
{
    private string _componentId = "";
    private Action<string>? _onAction;
    private bool _eventsWired;
    private DispatcherTimer? _revertTimer;

    public InlineConfirmComponent()
    {
        InitializeComponent();
        ConfirmButton.Content = Localizer.T("action.confirm");
        CancelButton.Content = Localizer.T("action.cancel");
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
            ConfirmButton.Content = confirmText.GetString() ?? Localizer.T("action.confirm");
        }

        if (data.TryGetProperty("cancel_text", out var cancelText))
        {
            CancelButton.Content = cancelText.GetString() ?? Localizer.T("action.cancel");
        }

        var destructive = data.TryGetProperty("destructive", out var d) && d.GetBoolean();
        if (destructive)
        {
            ConfirmButton.Foreground = new SolidColorBrush(ThemeColors.Destructive);
        }

        AutomationProperties.SetName(WarningText, WarningText.Text);
        AutomationProperties.SetName(ConfirmButton,
            (string?)ConfirmButton.Content ?? Localizer.T("action.confirm"));
        AutomationProperties.SetName(CancelButton,
            (string?)CancelButton.Content ?? Localizer.T("action.cancel"));

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

        // Initially collapsed; tap warning text to expand
        ButtonPanel.Visibility = Visibility.Collapsed;

        if (!_eventsWired)
        {
            WarningText.Tapped += OnWarningTapped;

            ConfirmButton.Click += (_, _) =>
            {
                _revertTimer?.Stop();
                _onAction?.Invoke(ActionJson.ActionPressed($"{_componentId}_confirm"));
            };

            CancelButton.Click += (_, _) =>
            {
                _revertTimer?.Stop();
                ButtonPanel.Visibility = Visibility.Collapsed;
                _onAction?.Invoke(ActionJson.ActionPressed($"{_componentId}_cancel"));
            };

            _eventsWired = true;
        }
    }

    private void OnWarningTapped(object sender, TappedRoutedEventArgs e)
    {
        ButtonPanel.Visibility = Visibility.Visible;
        _revertTimer?.Stop();
        _revertTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _revertTimer.Tick += (_, _) =>
        {
            _revertTimer?.Stop();
            ButtonPanel.Visibility = Visibility.Collapsed;
        };
        _revertTimer.Start();
    }
}
