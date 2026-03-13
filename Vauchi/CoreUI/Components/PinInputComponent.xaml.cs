// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using Microsoft.UI.Xaml.Controls;

namespace Vauchi.CoreUI.Components;

public sealed partial class PinInputComponent : UserControl, IRenderable
{
    public PinInputComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data)
    {
        if (data.TryGetProperty("label", out var label))
        {
            PinLabel.Text = label.GetString() ?? "Enter PIN";
        }
        if (data.TryGetProperty("length", out var length))
        {
            PinBox.MaxLength = length.GetInt32();
        }
        // TODO: Wire up PasswordChanged to emit user action
    }
}
