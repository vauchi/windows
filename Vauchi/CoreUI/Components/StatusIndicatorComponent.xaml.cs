// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
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
        string title = data.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
        string? detail = data.TryGetProperty("detail", out var d) ? d.GetString() : null;
        string status = data.TryGetProperty("status", out var s) ? s.GetString() ?? "Pending" : "Pending";

        StatusText.Text = detail != null ? $"{title} — {detail}" : title;

        StatusDot.Fill = new SolidColorBrush(status switch
        {
            "InProgress" => Colors.DodgerBlue,
            "Success" => Colors.Green,
            "Failed" => Colors.Red,
            "Warning" => Colors.Orange,
            _ => Colors.Gray, // Pending and unknown
        });

        AutomationProperties.SetName(this, $"{title}: {status}");
    }
}
