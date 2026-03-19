// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Text.Json;
using Vauchi.Helpers;

namespace Vauchi.CoreUI.Components;

public sealed partial class QrCodeComponent : UserControl, IRenderable
{
    public QrCodeComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        DisplayBorder.Visibility = Visibility.Collapsed;
        ScanPanel.Visibility = Visibility.Collapsed;

        string componentId = data.TryGetProperty("id", out var cid)
            ? cid.GetString() ?? "" : "";

        // Key is "data", not "payload"
        if (data.TryGetProperty("data", out var qrData) &&
            qrData.GetString() is string payload && payload.Length > 0)
        {
            // Display mode: show the QR payload as monospace text
            // TODO: Render actual QR image (needs QRCoder NuGet package)
            DisplayBorder.Visibility = Visibility.Visible;
            QrDataText.Text = payload;
            return;
        }

        // Scan mode: show paste + submit UI
        if (data.TryGetProperty("mode", out var mode) &&
            mode.GetString() == "Scan")
        {
            ScanPanel.Visibility = Visibility.Visible;

            if (onAction != null)
            {
                string capturedComponentId = componentId;
                ScanSubmitButton.Click += (_, _) =>
                {
                    string scanned = ScanInput.Text?.Trim() ?? "";
                    if (scanned.Length > 0)
                    {
                        onAction(ActionJson.TextChanged(capturedComponentId, scanned));
                    }
                };
            }
        }
    }
}
