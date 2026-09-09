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
        byte[]? bytes = payload.TryGetProperty("data", out JsonElement data)
            ? PresentationJson.Bytes(data)
            : null;
        string shape = String(payload, "shape");
        string fallbackText = String(payload, "fallback_text");

        FrameworkElement? visual = PresentationImageShape.Visual(bytes, fallbackText) switch
        {
            PresentationImageVisual.Picture => Picture(bytes!, shape),
            PresentationImageVisual.Initials => Initials(fallbackText, shape),
            // Nothing to show shows nothing. An empty element still carrying
            // the node's accessibility name announced a picture that was not
            // there.
            _ => null,
        };
        if (visual is null)
        {
            return new TextBlock { Visibility = Visibility.Collapsed };
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

    private static FrameworkElement Picture(byte[] bytes, string shape)
    {
        var image = new Image
        {
            MaxWidth = 240,
            MaxHeight = 240,
            Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform,
        };
        LoadImage(image, bytes);
        if (!PresentationImageShape.IsCircular(shape))
        {
            return image;
        }

        return new Border
        {
            Child = image,
            Width = PresentationImageShape.AvatarSide,
            Height = PresentationImageShape.AvatarSide,
            CornerRadius = new CornerRadius(PresentationImageShape.CornerRadiusFor(shape)),
        };
    }

    /// <summary>
    /// The initials used to go into a bare <c>TextBlock</c>, which paints no
    /// body — so they read as a stray letter on the page rather than as an
    /// avatar. They now get the same square, filled, rounded ground the
    /// picture branch has always had.
    /// </summary>
    private static FrameworkElement Initials(string fallbackText, string shape)
    {
        double side = PresentationImageShape.AvatarSide;
        return new Border
        {
            Width = side,
            Height = side,
            CornerRadius = new CornerRadius(PresentationImageShape.CornerRadiusFor(shape)),
            Background = (Microsoft.UI.Xaml.Media.Brush)Microsoft.UI.Xaml.Application
                .Current.Resources["SystemControlBackgroundListLowBrush"],
            Child = new TextBlock
            {
                Text = fallbackText,
                FontSize = 44,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
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
