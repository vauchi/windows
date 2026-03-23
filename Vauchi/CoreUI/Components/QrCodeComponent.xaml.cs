// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Vauchi.Helpers;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using ZXing;
using ZXing.Common;

namespace Vauchi.CoreUI.Components;

public sealed partial class QrCodeComponent : UserControl, IRenderable
{
    private string _componentId = "";
    private Action<string>? _onAction;
    private bool _eventsWired;
    private MediaCapture? _mediaCapture;
    private DispatcherTimer? _scanTimer;
    private bool _scanning;

    public QrCodeComponent()
    {
        InitializeComponent();
        Unloaded += async (_, _) => await StopCameraAsync();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        _componentId = data.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";
        _onAction = onAction;
        string qrData = data.TryGetProperty("data", out var d) ? d.GetString() ?? "" : "";
        string mode = data.TryGetProperty("mode", out var m) ? m.GetString() ?? "Display" : "Display";
        string? label = data.TryGetProperty("label", out var l) ? l.GetString() : null;

        if (mode == "Scan")
        {
            QrDisplayBorder.Visibility = Visibility.Collapsed;
            ScanBorder.Visibility = Visibility.Visible;
            ScanButton.Visibility = Visibility.Visible;
            ScanStatus.Text = "Tap 'Scan QR Code' to start camera";
            ScanStatus.Visibility = Visibility.Visible;

            if (!_eventsWired)
            {
                ScanButton.Click += async (_, _) =>
                {
                    if (_scanning) await StopCameraAsync();
                    else await StartCameraAsync();
                };
                _eventsWired = true;
            }
        }
        else
        {
            QrDisplayBorder.Visibility = Visibility.Visible;
            ScanBorder.Visibility = Visibility.Collapsed;
            ScanButton.Visibility = Visibility.Collapsed;
            ScanStatus.Visibility = Visibility.Collapsed;

            if (qrData.Length > 0)
            {
                try { GenerateQrBitmap(qrData); }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Vauchi] QR generation failed: {ex.Message}");
                }
            }
        }

        if (label != null)
        {
            QrLabel.Text = label;
            QrLabel.Visibility = Visibility.Visible;
        }

        AutomationProperties.SetName(this, label ?? "QR Code");
    }

    private void GenerateQrBitmap(string data)
    {
        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new EncodingOptions { Width = 250, Height = 250, Margin = 2 }
        };

        var pixelData = writer.Write(data);
        var bitmap = new WriteableBitmap(pixelData.Width, pixelData.Height);
        using (var stream = bitmap.PixelBuffer.AsStream())
        {
            stream.Write(pixelData.Pixels, 0, pixelData.Pixels.Length);
        }
        QrImage.Source = bitmap;
    }

    private async Task StartCameraAsync()
    {
        if (_scanning) return;
        try
        {
            _mediaCapture = new MediaCapture();
            await _mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
            {
                StreamingCaptureMode = StreamingCaptureMode.Video,
            });
            // WinUI 3 doesn't expose CaptureElement for live preview.
            // Camera captures frames in the background for ZXing QR decoding;
            // ScanStatus text provides user feedback instead of a video preview.
            await _mediaCapture.StartPreviewAsync();
            _scanning = true;
            ScanButton.Content = "Stop Scanning";
            ScanStatus.Text = "Point camera at QR code...";

            _scanTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _scanTimer.Tick += async (_, _) => await TryDecodeFrameAsync();
            _scanTimer.Start();
        }
        catch (UnauthorizedAccessException)
        {
            ScanStatus.Text = "Camera access denied. Enable in Windows Settings > Privacy > Camera.";
            ScanBorder.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ScanStatus.Text = ex.Message.Contains("No capture devices")
                ? "No camera available. Use your phone to scan the QR code."
                : $"Camera error: {ex.Message}";
            ScanBorder.Visibility = Visibility.Collapsed;
        }
    }

    private async Task TryDecodeFrameAsync()
    {
        if (!_scanning || _mediaCapture == null) return;
        try
        {
            var previewProps = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(
                MediaStreamType.VideoPreview) as Windows.Media.MediaProperties.VideoEncodingProperties;
            if (previewProps == null) return;

            int w = (int)previewProps.Width;
            int h = (int)previewProps.Height;
            if (w == 0 || h == 0) return;

            var videoFrame = new Windows.Media.VideoFrame(BitmapPixelFormat.Bgra8, w, h);
            await _mediaCapture.GetPreviewFrameAsync(videoFrame);

            var softwareBitmap = videoFrame.SoftwareBitmap;
            if (softwareBitmap == null) { videoFrame.Dispose(); return; }

            // Convert SoftwareBitmap to byte array for ZXing
            var buffer = new Windows.Storage.Streams.Buffer((uint)(w * h * 4));
            softwareBitmap.CopyToBuffer(buffer);
            byte[] pixels = new byte[buffer.Length];
            using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer))
            {
                reader.ReadBytes(pixels);
            }

            videoFrame.Dispose();

            var luminanceSource = new RGBLuminanceSource(pixels, w, h, RGBLuminanceSource.BitmapFormat.BGRA32);
            var barcodeReader = new BarcodeReaderGeneric();
            barcodeReader.Options.PossibleFormats = new[] { BarcodeFormat.QR_CODE };
            var result = barcodeReader.Decode(luminanceSource);

            if (result != null)
            {
                await StopCameraAsync();
                ScanStatus.Text = "QR code detected!";
                _onAction?.Invoke(ExchangeHardwareEventJson.QrScanned(result.Text));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Vauchi] Frame decode error: {ex.Message}");
        }
    }

    private async Task StopCameraAsync()
    {
        _scanning = false;
        _scanTimer?.Stop();
        _scanTimer = null;
        if (_mediaCapture != null)
        {
            try { await _mediaCapture.StopPreviewAsync(); } catch { }
            _mediaCapture.Dispose();
            _mediaCapture = null;
        }
        ScanButton.Content = "Scan QR Code";
    }
}
