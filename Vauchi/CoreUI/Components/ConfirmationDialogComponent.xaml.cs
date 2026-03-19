// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Text.Json;
using Vauchi.Helpers;

namespace Vauchi.CoreUI.Components;

public sealed partial class ConfirmationDialogComponent : UserControl, IRenderable
{
    public ConfirmationDialogComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        string componentId = data.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";

        if (data.TryGetProperty("title", out var title))
        {
            string titleText = title.GetString() ?? "";
            if (titleText.Length > 0)
            {
                DialogTitle.Text = titleText;
                DialogTitle.Visibility = Visibility.Visible;
            }
        }

        if (data.TryGetProperty("message", out var message))
        {
            DialogMessage.Text = message.GetString() ?? "";
        }

        // Use confirm_text (not confirm_label)
        if (data.TryGetProperty("confirm_text", out var confirmText))
        {
            ConfirmButton.Content = confirmText.GetString() ?? "Confirm";
        }

        if (data.TryGetProperty("cancel_label", out var cancelLabel))
        {
            CancelButton.Content = cancelLabel.GetString() ?? "Cancel";
        }

        // Apply destructive style (red confirm button)
        bool destructive = data.TryGetProperty("destructive", out var destEl) && destEl.GetBoolean();
        if (destructive)
        {
            ConfirmButton.Foreground = new SolidColorBrush(Colors.Red);
        }

        if (onAction != null && componentId.Length > 0)
        {
            ConfirmButton.Click += (_, _) =>
                onAction(ActionJson.ActionPressed($"{componentId}_confirm"));
            CancelButton.Click += (_, _) =>
                onAction(ActionJson.ActionPressed($"{componentId}_cancel"));
        }
    }
}
