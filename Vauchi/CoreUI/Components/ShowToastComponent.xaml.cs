// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Text.Json;

namespace Vauchi.CoreUI.Components;

public sealed partial class ShowToastComponent : UserControl, IRenderable
{
    private DispatcherTimer? _dismissTimer;

    public ShowToastComponent()
    {
        InitializeComponent();
        Unloaded += (_, _) =>
        {
            _dismissTimer?.Stop();
            _dismissTimer = null;
        };
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        _dismissTimer?.Stop();

        if (data.TryGetProperty("message", out var message))
        {
            Toast.Message = message.GetString() ?? "";
        }
        if (data.TryGetProperty("title", out var title))
        {
            Toast.Title = title.GetString() ?? "";
        }

        string severity = (data.TryGetProperty("severity", out var sev)
            ? sev.GetString() ?? "" : "").ToLowerInvariant();
        Toast.Severity = severity switch
        {
            "error" => InfoBarSeverity.Error,
            "warning" => InfoBarSeverity.Warning,
            "success" => InfoBarSeverity.Success,
            _ => InfoBarSeverity.Informational,
        };

        Toast.IsOpen = true;

        // Auto-dismiss non-error toasts after 4 seconds
        if (Toast.Severity != InfoBarSeverity.Error)
        {
            _dismissTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            _dismissTimer.Tick += (_, _) =>
            {
                _dismissTimer?.Stop();
                Toast.IsOpen = false;
            };
            _dismissTimer.Start();
        }
    }
}
