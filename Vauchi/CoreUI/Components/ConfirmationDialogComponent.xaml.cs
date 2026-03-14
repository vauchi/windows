// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using Microsoft.UI.Xaml.Controls;
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

        if (data.TryGetProperty("message", out var message))
        {
            DialogMessage.Text = message.GetString() ?? "";
        }
        if (data.TryGetProperty("confirm_label", out var confirmLabel))
        {
            ConfirmButton.Content = confirmLabel.GetString() ?? "Confirm";
        }
        if (data.TryGetProperty("cancel_label", out var cancelLabel))
        {
            CancelButton.Content = cancelLabel.GetString() ?? "Cancel";
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
