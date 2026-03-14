// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
using ZXing;
using ZXing.Common;

namespace Vauchi.CoreUI.Components;

public sealed partial class QrCodeComponent : UserControl, IRenderable, IDisposable
{
    private MediaCapture? _mediaCapture;
    private MediaFrameReader? _frameReader;
    private CancellationTokenSource? _cts;
    private bool _disposed;
    private string? _componentId;
    private int _processingFrame;

    public event EventHandler<string>? ActionRequested;

    public QrCodeComponent()
    {
        InitializeComponent();
        Unloaded += (_, _) => Dispose();
    }

    public void Render(JsonElement data)
    {
        // Stop any previous capture session
        StopCameraAsync().Wait(TimeSpan.FromSeconds(2));

        // Reset visibility
        QrLabel.Visibility = Visibility.Collapsed;
        QrImage.Visibility = Visibility.Collapsed;
        QrFallbackText.Visibility = Visibility.Collapsed;
        ScanPanel.Visibility = Visibility.Collapsed;

        // Read component ID
        if (data.TryGetProperty("id", out var idEl)
            && idEl.ValueKind == JsonValueKind.String)
        {
            _componentId = idEl.GetString();
        }

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
            ScanStatusText.Text = "Initializing camera...";
            ScanStatusText.Visibility = Visibility.Visible;
            _ = StartCameraScanAsync();
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

    private async Task StartCameraScanAsync()
    {
        _cts = new CancellationTokenSource();

        try
        {
            _mediaCapture = new MediaCapture();

            // Find a suitable camera (prefer rear/external)
            var frameSourceGroups = await MediaFrameSourceGroup.FindAllAsync();
            MediaCaptureInitializationSettings settings;

            var preferredGroup = frameSourceGroups
                .FirstOrDefault(g => g.SourceInfos.Any(
                    s => s.MediaStreamType == MediaStreamType.VideoPreview
                      || s.MediaStreamType == MediaStreamType.VideoRecord));

            if (preferredGroup != null)
            {
                settings = new MediaCaptureInitializationSettings
                {
                    SourceGroup = preferredGroup,
                    SharingMode = MediaCaptureSharingMode.ExclusiveControl,
                    StreamingCaptureMode = StreamingCaptureMode.Video,
                    MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                };
            }
            else
            {
                settings = new MediaCaptureInitializationSettings
                {
                    StreamingCaptureMode = StreamingCaptureMode.Video,
                    MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                };
            }

            await _mediaCapture.InitializeAsync(settings);

            CameraPreview.Visibility = Visibility.Visible;
            ScanStatusText.Text = "Point camera at QR code";

            // Set up frame reader for QR detection and preview
            var frameSource = _mediaCapture.FrameSources.Values
                .FirstOrDefault(s => s.Info.MediaStreamType == MediaStreamType.VideoRecord
                                  || s.Info.MediaStreamType == MediaStreamType.VideoPreview);

            if (frameSource != null)
            {
                _frameReader = await _mediaCapture.CreateFrameReaderAsync(
                    frameSource,
                    MediaEncodingSubtypes.Bgra8);

                _frameReader.FrameArrived += OnFrameArrived;
                await _frameReader.StartAsync();
            }
            else
            {
                CameraPreview.Visibility = Visibility.Collapsed;
                ScanStatusText.Text = "No compatible camera found";
            }
        }
        catch (UnauthorizedAccessException)
        {
            CameraPreview.Visibility = Visibility.Collapsed;
            ScanStatusText.Text = "Camera access denied. Please allow camera access in Settings.";
        }
        catch (Exception)
        {
            CameraPreview.Visibility = Visibility.Collapsed;
            ScanStatusText.Text = "No camera available";
        }
    }

    private void OnFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        if (_disposed || _cts?.IsCancellationRequested == true)
            return;

        // Skip if we're already processing a frame
        if (Interlocked.CompareExchange(ref _processingFrame, 1, 0) != 0)
            return;

        try
        {
            using var frameRef = sender.TryAcquireLatestFrame();
            if (frameRef?.VideoMediaFrame?.SoftwareBitmap == null)
            {
                Interlocked.Exchange(ref _processingFrame, 0);
                return;
            }

            var softwareBitmap = frameRef.VideoMediaFrame.SoftwareBitmap;

            // Ensure BGRA8 format for both preview and ZXing
            SoftwareBitmap? convertedBitmap = null;
            if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8
                || softwareBitmap.BitmapAlphaMode != BitmapAlphaMode.Premultiplied)
            {
                convertedBitmap = SoftwareBitmap.Convert(
                    softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            }

            var bitmapToUse = convertedBitmap ?? softwareBitmap;
            var width = bitmapToUse.PixelWidth;
            var height = bitmapToUse.PixelHeight;

            // Copy pixel data for ZXing
            var buffer = new Windows.Storage.Streams.Buffer((uint)(width * height * 4));
            bitmapToUse.CopyToBuffer(buffer);
            var bytes = new byte[buffer.Length];
            using (var stream = buffer.AsStream())
            {
                stream.Read(bytes, 0, bytes.Length);
            }

            // Update preview image on UI thread
            DispatcherQueue.TryEnqueue(() =>
            {
                if (_disposed || _cts?.IsCancellationRequested == true) return;
                try
                {
                    var previewBitmap = new SoftwareBitmapSource();
                    previewBitmap.SetBitmapAsync(bitmapToUse).AsTask().Wait(500);
                    CameraPreview.Source = previewBitmap;
                }
                catch
                {
                    // Preview update failed — non-critical
                }
            });

            // Try to decode QR code
            var reader = new BarcodeReaderGeneric
            {
                Options = new DecodingOptions
                {
                    PossibleFormats = new[] { BarcodeFormat.QR_CODE },
                    TryHarder = true,
                }
            };

            var luminanceSource = new RGBLuminanceSource(
                bytes, width, height, RGBLuminanceSource.BitmapFormat.BGRA32);
            var result = reader.Decode(luminanceSource);

            convertedBitmap?.Dispose();

            if (result != null && !string.IsNullOrEmpty(result.Text))
            {
                _cts?.Cancel();

                // Dispatch to UI thread
                DispatcherQueue.TryEnqueue(async () =>
                {
                    await StopCameraAsync();
                    ScanStatusText.Text = "QR code detected";
                    CameraPreview.Visibility = Visibility.Collapsed;

                    // Emit the scanned data as an action
                    var actionJson = JsonSerializer.Serialize(new
                    {
                        QrScanned = new { component_id = _componentId ?? "", data = result.Text }
                    });
                    ActionRequested?.Invoke(this, actionJson);
                });
                return;
            }
        }
        finally
        {
            Interlocked.Exchange(ref _processingFrame, 0);
        }
    }

    private async Task StopCameraAsync()
    {
        _cts?.Cancel();

        if (_frameReader != null)
        {
            _frameReader.FrameArrived -= OnFrameArrived;
            await _frameReader.StopAsync();
            _frameReader.Dispose();
            _frameReader = null;
        }

        if (_mediaCapture != null)
        {
            _mediaCapture.Dispose();
            _mediaCapture = null;
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

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopCameraAsync().Wait(TimeSpan.FromSeconds(2));
        _cts?.Dispose();
    }
}
