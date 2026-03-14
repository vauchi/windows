// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using ZXing;
using ZXing.Common;

namespace Vauchi.CoreUI.Components;

public sealed partial class QrCodeComponent : UserControl, IRenderable
{
    public event EventHandler<string>? ActionRequested;

    public QrCodeComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data)
    {
        // Reset visibility
        QrLabel.Visibility = Visibility.Collapsed;
        QrImage.Visibility = Visibility.Collapsed;
        QrFallbackText.Visibility = Visibility.Collapsed;
        ScanPanel.Visibility = Visibility.Collapsed;

        // Read label
        if (data.TryGetProperty("label", out var labelEl)
            && labelEl.ValueKind == JsonValueKind.String)
        {
            QrLabel.Text = labelEl.GetString() ?? "";
            QrLabel.Visibility = Visibility.Visible;
        }

        // Read mode (default: Display)
        var mode = "Display";
        if (data.TryGetProperty("mode", out var modeEl)
            && modeEl.ValueKind == JsonValueKind.String)
        {
            mode = modeEl.GetString() ?? "Display";
        }

        if (mode == "Scan")
        {
            ScanPanel.Visibility = Visibility.Visible;
            return;
        }

        // Display mode — read data
        var qrData = "";
        if (data.TryGetProperty("data", out var dataEl)
            && dataEl.ValueKind == JsonValueKind.String)
        {
            qrData = dataEl.GetString() ?? "";
        }

        if (string.IsNullOrEmpty(qrData))
            return;

        try
        {
            RenderQrCode(qrData);
        }
        catch
        {
            // ZXing not available or generation failed — show text fallback
            QrFallbackText.Text = qrData;
            QrFallbackText.Visibility = Visibility.Visible;
        }
    }

    private void RenderQrCode(string data)
    {
        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new EncodingOptions
            {
                Width = 250,
                Height = 250,
                Margin = 1,
            }
        };

        var pixelData = writer.Write(data);
        var width = pixelData.Width;
        var height = pixelData.Height;

        var bitmap = new WriteableBitmap(width, height);

        // Copy pixel data (BGRA format) into the WriteableBitmap
        using (var stream = bitmap.PixelBuffer.AsStream())
        {
            stream.Write(pixelData.Pixels, 0, pixelData.Pixels.Length);
        }

        bitmap.Invalidate();
        QrImage.Source = bitmap;
        QrImage.Visibility = Visibility.Visible;
    }
}
