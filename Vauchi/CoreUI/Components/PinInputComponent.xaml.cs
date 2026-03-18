// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml.Controls;
using System;
using System.Text.Json;
using Vauchi.Helpers;

namespace Vauchi.CoreUI.Components;

public sealed partial class PinInputComponent : UserControl, IRenderable
{
    public PinInputComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        string componentId = data.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";

        if (data.TryGetProperty("label", out var label))
        {
            PinLabel.Text = label.GetString() ?? "Enter PIN";
        }
        if (data.TryGetProperty("length", out var length))
        {
            PinBox.MaxLength = length.GetInt32();
        }

        if (onAction != null && componentId.Length > 0)
        {
            PinBox.PasswordChanged += (_, _) =>
                onAction(ActionJson.TextChanged(componentId, PinBox.Password));
        }
    }
}
