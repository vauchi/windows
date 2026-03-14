// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using Microsoft.UI.Xaml.Controls;

namespace Vauchi.CoreUI.Components;

public sealed partial class ConfirmationDialogComponent : UserControl, IRenderable
{
    public event EventHandler<string>? ActionRequested;

    public ConfirmationDialogComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data)
    {
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
        // TODO: Wire up button clicks to emit user actions
    }
}
