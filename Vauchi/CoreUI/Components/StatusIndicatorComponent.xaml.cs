// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml.Controls;
using System;
using System.Text.Json;

namespace Vauchi.CoreUI.Components;

public sealed partial class StatusIndicatorComponent : UserControl, IRenderable
{
    public StatusIndicatorComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        if (data.TryGetProperty("label", out var label))
        {
            StatusText.Text = label.GetString() ?? "";
        }
        // TODO: Map status to color for StatusDot fill
    }
}
