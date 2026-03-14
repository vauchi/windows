// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Vauchi.CoreUI.Components;

public sealed partial class ConfirmationDialogComponent : UserControl, IRenderable
{
    public event EventHandler<string>? ActionRequested;

    private string _componentId = "";

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
            // Title is shown via the dialog message as a prefix if needed
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
    }
}
