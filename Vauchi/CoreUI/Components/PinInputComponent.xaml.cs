// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Text.Json;
using Vauchi.CoreUI;
using Vauchi.Helpers;
using Vauchi.Services;

namespace Vauchi.CoreUI.Components;

public sealed partial class PinInputComponent : UserControl, IRenderable
{
    private bool _eventWired;

    public PinInputComponent()
    {
        InitializeComponent();
        PinLabel.Text = Localizer.T("pin.enter");
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        string componentId = data.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";
        string defaultLabel = Localizer.T("pin.enter");
        string label = data.TryGetProperty("label", out var l) ? l.GetString() ?? defaultLabel : defaultLabel;
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
                Fill = new SolidColorBrush(i < filled ? ThemeColors.Info : ThemeColors.Neutral),
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

        if (data.TryGetProperty("a11y", out var a11yElem))
        {
            if (a11yElem.TryGetProperty("label", out var labelElem))
            {
                var a11yLabel = labelElem.GetString();
                if (!string.IsNullOrEmpty(a11yLabel))
                    AutomationProperties.SetName(PinBox, a11yLabel);
            }
            if (a11yElem.TryGetProperty("hint", out var hintElem))
            {
                var hint = hintElem.GetString();
                if (!string.IsNullOrEmpty(hint))
                    AutomationProperties.SetHelpText(PinBox, hint);
            }
        }
    }
}
