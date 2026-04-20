// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Text.Json;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;
using Vauchi.CoreUI;
using Vauchi.Helpers;

namespace Vauchi.CoreUI.Components;

public sealed partial class AvatarPreviewComponent : UserControl, IRenderable
{
    public AvatarPreviewComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        string initials = data.TryGetProperty("initials", out var initEl)
            ? initEl.GetString() ?? ""
            : "";
        string bgColor = data.TryGetProperty("bg_color", out var bgEl)
            ? bgEl.GetString() ?? ThemeColors.AvatarFallbackHex
            : ThemeColors.AvatarFallbackHex;
        bool editable = data.TryGetProperty("editable", out var editEl) && editEl.GetBoolean();

        // Parse background color for initials circle
        InitialsCircle.Fill = ParseBrush(bgColor);
        InitialsText.Text = initials;

        // Try to load image data
        bool hasImage = false;
        if (data.TryGetProperty("image_data", out var imageDataEl)
            && imageDataEl.ValueKind == JsonValueKind.Array
            && imageDataEl.GetArrayLength() > 0)
        {
            try
            {
                byte[] imageBytes = ParseByteArray(imageDataEl);
                LoadImageAsync(imageBytes);
                hasImage = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Vauchi] AvatarPreview image load failed: {ex.Message}");
            }
        }

        // Show image circle or initials fallback
        if (hasImage)
        {
            ImageCircle.Visibility = Visibility.Visible;
            InitialsCircle.Visibility = Visibility.Collapsed;
            InitialsText.Visibility = Visibility.Collapsed;
        }
        else
        {
            ImageCircle.Visibility = Visibility.Collapsed;
            InitialsCircle.Visibility = Visibility.Visible;
            InitialsText.Visibility = Visibility.Visible;
        }

        // Editable overlay
        if (editable)
        {
            EditOverlay.Visibility = Visibility.Visible;
            AvatarContainer.PointerPressed += (_, _) =>
                onAction?.Invoke(ActionJson.ActionPressed("edit_avatar"));
        }
        else
        {
            EditOverlay.Visibility = Visibility.Collapsed;
        }

        AutomationProperties.SetName(this, editable ? "Edit avatar" : "Avatar");

        if (data.TryGetProperty("a11y", out var a11yElem))
        {
            if (a11yElem.TryGetProperty("label", out var labelElem))
            {
                var a11yLabel = labelElem.GetString();
                if (!string.IsNullOrEmpty(a11yLabel))
                    AutomationProperties.SetName(this, a11yLabel);
            }
        }
    }

    private async void LoadImageAsync(byte[] imageBytes)
    {
        var bitmapImage = new BitmapImage();
        using var stream = new InMemoryRandomAccessStream();
        await stream.WriteAsync(imageBytes.AsBuffer());
        stream.Seek(0);
        await bitmapImage.SetSourceAsync(stream);
        AvatarImageBrush.ImageSource = bitmapImage;
    }

    private static byte[] ParseByteArray(JsonElement arrayEl)
    {
        var result = new byte[arrayEl.GetArrayLength()];
        int i = 0;
        foreach (var el in arrayEl.EnumerateArray())
            result[i++] = (byte)el.GetInt32();
        return result;
    }

    private static SolidColorBrush ParseBrush(string hex)
    {
        try
        {
            if (hex.StartsWith('#') && hex.Length == 7)
            {
                byte r = Convert.ToByte(hex.Substring(1, 2), 16);
                byte g = Convert.ToByte(hex.Substring(3, 2), 16);
                byte b = Convert.ToByte(hex.Substring(5, 2), 16);
                return new SolidColorBrush(Windows.UI.Color.FromArgb(255, r, g, b));
            }
        }
        catch { }
        return new SolidColorBrush(ThemeColors.AvatarFallback);
    }
}
