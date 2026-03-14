// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Vauchi.CoreUI.Components;

public sealed partial class ConfirmationDialogComponent : UserControl, IRenderable
{
    public event EventHandler<string>? ActionRequested;

    private string _componentId = "";
    private bool _eventsWired;

    public ConfirmationDialogComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data)
    {
        _componentId = data.TryGetProperty("id", out var id)
            ? id.GetString() ?? ""
            : "";

        if (data.TryGetProperty("title", out var title))
        {
            DialogTitle.Text = title.GetString() ?? "";
            DialogTitle.Visibility = string.IsNullOrEmpty(title.GetString())
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        if (data.TryGetProperty("message", out var message))
        {
            DialogMessage.Text = message.GetString() ?? "";
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

        if (!_eventsWired)
        {
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
                ActionRequested?.Invoke(this,
                    JsonSerializer.Serialize(new
                    {
                        ActionPressed = new { action_id = $"{_componentId}_cancel" }
                    }));
            };

            _eventsWired = true;
        }
    }
}
