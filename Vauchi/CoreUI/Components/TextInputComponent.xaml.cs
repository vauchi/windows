// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using Microsoft.UI.Xaml.Controls;
using Vauchi.Helpers;

namespace Vauchi.CoreUI.Components;

public sealed partial class TextInputComponent : UserControl, IRenderable
{
    public TextInputComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        string componentId = data.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";

        if (data.TryGetProperty("placeholder", out var placeholder))
        {
            InputBox.PlaceholderText = placeholder.GetString() ?? "";
        }
        if (data.TryGetProperty("value", out var value))
        {
            InputBox.Text = value.GetString() ?? "";
        }

        if (onAction != null && componentId.Length > 0)
        {
            InputBox.TextChanged += (_, _) =>
                onAction(ActionJson.TextChanged(componentId, InputBox.Text));
        }
    }
}
