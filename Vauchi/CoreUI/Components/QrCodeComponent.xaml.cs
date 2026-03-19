// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Text.Json;

namespace Vauchi.CoreUI.Components;

public sealed partial class QrCodeComponent : UserControl, IRenderable
{
    public QrCodeComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        string qrData = data.TryGetProperty("data", out var d) ? d.GetString() ?? "" : "";
        string mode = data.TryGetProperty("mode", out var m) ? m.GetString() ?? "Display" : "Display";
        string? label = data.TryGetProperty("label", out var l) ? l.GetString() : null;

        if (mode == "Scan")
        {
            QrDisplayBorder.Visibility = Visibility.Collapsed;
            ScanMessage.Visibility = Visibility.Visible;
        }
        else
        {
            // Display mode — placeholder until ZXing.Net is added
            QrDisplayBorder.Visibility = Visibility.Visible;
            ScanMessage.Visibility = Visibility.Collapsed;
            QrDataText.Text = qrData.Length > 60 ? qrData[..57] + "..." : qrData;
        }

        if (label != null)
        {
            QrLabel.Text = label;
            QrLabel.Visibility = Visibility.Visible;
        }

        AutomationProperties.SetName(this, label ?? "QR Code");
    }
}
