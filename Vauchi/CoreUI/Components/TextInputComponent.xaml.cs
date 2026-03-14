// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using Microsoft.UI.Xaml.Controls;

namespace Vauchi.CoreUI.Components;

public sealed partial class TextInputComponent : UserControl, IRenderable
{
    public event EventHandler<string>? ActionRequested;

    public TextInputComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data)
    {
        if (data.TryGetProperty("placeholder", out var placeholder))
        {
            InputBox.PlaceholderText = placeholder.GetString() ?? "";
        }
        if (data.TryGetProperty("value", out var value))
        {
            InputBox.Text = value.GetString() ?? "";
        }
        // TODO: Wire up TextChanged to emit user action
    }
}
