// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml.Controls;
using System;
using System.Text.Json;

namespace Vauchi.CoreUI.Components;

public sealed partial class ToggleListComponent : UserControl, IRenderable
{
    public ToggleListComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        // TODO: Build toggle switches from data["items"]
        if (data.TryGetProperty("title", out var title))
        {
            Placeholder.Text = title.GetString() ?? "[ToggleList]";
        }
    }
}
