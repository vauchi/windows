// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml.Controls;
using System;
using System.Text.Json;

namespace Vauchi.CoreUI.Components;

public sealed partial class ShowToastComponent : UserControl, IRenderable
{
    public ShowToastComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        if (data.TryGetProperty("message", out var message))
        {
            Toast.Message = message.GetString() ?? "";
        }
        if (data.TryGetProperty("title", out var title))
        {
            Toast.Title = title.GetString() ?? "";
        }

        string severity = data.TryGetProperty("severity", out var sev) ? sev.GetString() ?? "" : "";
        Toast.Severity = severity switch
        {
            "error" => InfoBarSeverity.Error,
            "warning" => InfoBarSeverity.Warning,
            "success" => InfoBarSeverity.Success,
            _ => InfoBarSeverity.Informational,
        };

        Toast.IsOpen = true;
    }
}
