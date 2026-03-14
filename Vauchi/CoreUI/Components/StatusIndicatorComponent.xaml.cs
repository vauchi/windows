// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Vauchi.CoreUI.Components;

public sealed partial class StatusIndicatorComponent : UserControl, IRenderable
{
    public event EventHandler<string>? ActionRequested;

    public StatusIndicatorComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data)
    {
        if (data.TryGetProperty("title", out var title))
        {
            TitleText.Text = title.GetString() ?? "";
        }

        if (data.TryGetProperty("detail", out var detail)
            && detail.ValueKind != JsonValueKind.Null)
        {
            DetailText.Text = detail.GetString() ?? "";
            DetailText.Visibility = Visibility.Visible;
        }
        else
        {
            DetailText.Visibility = Visibility.Collapsed;
        }

        if (data.TryGetProperty("status", out var status))
        {
            var color = status.GetString() switch
            {
                "Pending" => Colors.Gray,
                "InProgress" => Colors.DodgerBlue,
                "Success" => Colors.Green,
                "Failed" => Colors.Red,
                "Warning" => Colors.Orange,
                _ => Colors.Gray
            };
            StatusDot.Fill = new SolidColorBrush(color);
        }
    }
}
