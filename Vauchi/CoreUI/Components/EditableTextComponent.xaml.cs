// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using Microsoft.UI.Xaml.Controls;

namespace Vauchi.CoreUI.Components;

public sealed partial class EditableTextComponent : UserControl, IRenderable
{
    public event EventHandler<string>? ActionRequested;

    public EditableTextComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        // TODO: Wire up TextChanged to emit user action
        if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("value", out var value))
        {
            EditableBox.Text = value.GetString() ?? "";
        }
        if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("placeholder", out var placeholder))
        {
            EditableBox.PlaceholderText = placeholder.GetString() ?? "";
        }
    }
}
