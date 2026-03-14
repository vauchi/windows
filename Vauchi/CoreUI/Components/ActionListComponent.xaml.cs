// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using Microsoft.UI.Xaml.Controls;

namespace Vauchi.CoreUI.Components;

public sealed partial class ActionListComponent : UserControl, IRenderable
{
    public event EventHandler<string>? ActionRequested;

    public ActionListComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data)
    {
        // TODO: Build action buttons from data["actions"]
        if (data.TryGetProperty("title", out var title))
        {
            Placeholder.Text = title.GetString() ?? "[ActionList]";
        }
    }
}
