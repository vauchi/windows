// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using Windows.Storage.Streams;
using ZXing;
using ZXing.Common;

namespace Vauchi.CoreUI;

public sealed partial class PresentationSurface
{
    private FrameworkElement RenderStatus(JsonElement payload)
    {
        string title = String(payload, "title");
        string detail = String(payload, "detail");
        string badge = String(payload, "badge");
        string text = title;
        if (detail.Length > 0)
            text += "\n" + detail;
        if (badge.Length > 0)
            text += " · " + badge;

        FrameworkElement status;
        if (payload.TryGetProperty("activation", out JsonElement action)
            && action.ValueKind == JsonValueKind.Object)
        {
            var button = ActionButton(action);
            button.Content = text;
            status = button;
        }
        else
        {
            status = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap };
        }
        ApplyAccessibility(status, payload);
        return status;
    }

    private FrameworkElement RenderImage(JsonElement payload)
    {
        var image = new Image
        {
            MaxWidth = 240,
            MaxHeight = 240,
            Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform,
        };
        if (payload.TryGetProperty("data", out JsonElement data)
            && PresentationJson.Bytes(data) is { Length: > 0 } bytes)
        {
            LoadImage(image, bytes);
        }
        else
        {
            image.Visibility = Visibility.Collapsed;
        }

        FrameworkElement visual;
        if (image.Visibility == Visibility.Visible)
        {
            visual = String(payload, "shape") == "circle"
                ? new Border
                {
                    Child = image,
                    Width = 160,
                    Height = 160,
                    CornerRadius = new CornerRadius(80),
                }
                : image;
        }
        else
        {
            visual = new TextBlock
            {
                Text = String(payload, "fallback_text"),
                TextWrapping = TextWrapping.Wrap,
            };
        }

        if (payload.TryGetProperty("activation", out JsonElement action)
            && action.ValueKind == JsonValueKind.Object)
        {
            var button = ActionButton(action);
            button.Content = visual;
            visual = button;
        }
        ApplyAccessibility(visual, payload);
        return visual;
    }

    private FrameworkElement RenderQr(JsonElement payload)
    {
        string bindingId = String(payload, "id");
        string label = String(payload, "label");
        if (String(payload, "purpose") == "capture")
        {
            var captureContainer = FieldContainer(label);
            var input = new TextBox
            {
                PlaceholderText = label,
                MinHeight = _minimumTargetSize,
            };
            AutomationProperties.SetAutomationId(input, bindingId);
            ApplyAccessibility(input, payload);
            input.TextChanged += (_, _) => EmitText(bindingId, input.Text);
            captureContainer.Children.Add(input);
            return captureContainer;
        }

        string data = "";
        if (payload.TryGetProperty("payloads", out JsonElement payloads)
            && payloads.ValueKind == JsonValueKind.Array
            && payloads.GetArrayLength() > 0)
        {
            data = payloads[0].GetString() ?? "";
        }
        var image = new Image
        {
            Width = 250,
            Height = 250,
            Source = data.Length > 0 ? QrBitmap(data) : null,
        };
        AutomationProperties.SetAutomationId(image, bindingId);
        ApplyAccessibility(image, payload);
        var displayContainer = FieldContainer(label);
        displayContainer.Children.Add(image);
        return displayContainer;
    }

    private FrameworkElement RenderConfirmation(JsonElement payload)
    {
        var container = new StackPanel { Spacing = 8 };
        container.Children.Add(new TextBlock
        {
            Text = String(payload, "warning"),
            TextWrapping = TextWrapping.Wrap,
        });
        if (payload.TryGetProperty("confirm", out JsonElement confirm))
            container.Children.Add(ActionButton(confirm));
        if (payload.TryGetProperty("cancel", out JsonElement cancel))
            container.Children.Add(ActionButton(cancel));
        ApplyAccessibility(container, payload);
        return container;
    }

    private static async void LoadImage(Image image, byte[] bytes)
    {
        try
        {
            var bitmap = new BitmapImage();
            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(bytes.AsBuffer());
            stream.Seek(0);
            await bitmap.SetSourceAsync(stream);
            image.Source = bitmap;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[Vauchi] Generic image decode failed: {exception.Message}");
        }
    }

    private static WriteableBitmap QrBitmap(string data)
    {
        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new EncodingOptions { Width = 250, Height = 250, Margin = 2 },
        };
        var pixels = writer.Write(data);
        var bitmap = new WriteableBitmap(pixels.Width, pixels.Height);
        using var stream = bitmap.PixelBuffer.AsStream();
        stream.Write(pixels.Pixels, 0, pixels.Pixels.Length);
        return bitmap;
    }
}
