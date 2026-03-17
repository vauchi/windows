// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Vauchi.CoreUI.Components;

public sealed partial class InlineConfirmComponent : UserControl, IRenderable
{
    public event EventHandler<string>? ActionRequested;

    private string _componentId = "";
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

        // Initially collapsed; tap warning text to expand
        ButtonPanel.Visibility = Visibility.Collapsed;

        if (!_eventsWired)
        {
            WarningText.Tapped += OnWarningTapped;

            ConfirmButton.Click += (_, _) =>
            {
                ActionRequested?.Invoke(this,
                    JsonSerializer.Serialize(new
                    {
                        ActionPressed = new { action_id = $"{_componentId}_confirm" }
                    }));
            };

            CancelButton.Click += (_, _) =>
            {
                ButtonPanel.Visibility = Visibility.Collapsed;
                ActionRequested?.Invoke(this,
                    JsonSerializer.Serialize(new
                    {
                        ActionPressed = new { action_id = $"{_componentId}_cancel" }
                    }));
            };

            _eventsWired = true;
        }
    }

    private void OnWarningTapped(object sender, TappedRoutedEventArgs e)
    {
        ButtonPanel.Visibility = Visibility.Visible;
    }
}
