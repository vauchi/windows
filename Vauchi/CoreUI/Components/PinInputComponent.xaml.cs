// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Text.Json;
using Vauchi.Helpers;

namespace Vauchi.CoreUI.Components;

public sealed partial class PinInputComponent : UserControl, IRenderable
{
    private bool _eventWired;

    public PinInputComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        string componentId = data.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";
        string label = data.TryGetProperty("label", out var l) ? l.GetString() ?? "Enter PIN" : "Enter PIN";
        int length = data.TryGetProperty("length", out var len) ? len.GetInt32() : 6;
        int filled = data.TryGetProperty("filled", out var f) ? f.GetInt32() : 0;

        PinLabel.Text = label;
        PinBox.MaxLength = length;

        // Render dots: filled = accent color, empty = gray
        DotContainer.Children.Clear();
        for (int i = 0; i < length; i++)
        {
            DotContainer.Children.Add(new Ellipse
            {
                Width = 14,
                Height = 14,
                Fill = new SolidColorBrush(i < filled ? Colors.DodgerBlue : Colors.Gray),
            });
        }

        // Validation error
        if (data.TryGetProperty("validation_error", out var ve) && ve.ValueKind == JsonValueKind.String)
        {
            ValidationError.Text = ve.GetString() ?? "";
            ValidationError.Visibility = Visibility.Visible;
        }
        else
        {
            ValidationError.Visibility = Visibility.Collapsed;
        }

        // Wire event once
        if (!_eventWired && onAction != null && componentId.Length > 0)
        {
            PinBox.PasswordChanged += (_, _) =>
                onAction(ActionJson.TextChanged(componentId, PinBox.Password));
            _eventWired = true;
        }

        AutomationProperties.SetName(PinBox, label);
    }
}
