// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;
using Vauchi.Helpers;

namespace Vauchi.CoreUI.Components;

public sealed partial class PreviewComponent : UserControl, IRenderable
{
    public PreviewComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        NameHeader.Text = data.TryGetProperty("name", out var nameEl)
            ? nameEl.GetString() ?? ""
            : "";

        AvatarArea.Visibility = Visibility.Collapsed;
        if (data.TryGetProperty("image_data", out var avatarEl)
            && avatarEl.ValueKind == JsonValueKind.Array
            && avatarEl.GetArrayLength() > 0)
        {
            try
            {
                byte[] avatarBytes = ParseByteArray(avatarEl);
                LoadAvatarAsync(avatarBytes);
                AvatarArea.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Vauchi] CardPreview avatar load failed: {ex.Message}");
            }
        }

        GroupTabBar.Children.Clear();
        GroupTabBar.Visibility = Visibility.Collapsed;

        string? selectedVariant = data.TryGetProperty("selected_variant", out var svEl)
            ? svEl.ValueKind == JsonValueKind.String ? svEl.GetString() : null
            : null;

        var variants = new List<(string VariantId, string DisplayName, JsonElement Element)>();
        if (data.TryGetProperty("variants", out var variantsEl))
        {
            foreach (var v in variantsEl.EnumerateArray())
            {
                string variantId = v.TryGetProperty("variant_id", out var vidEl) ? vidEl.GetString() ?? "" : "";
                string displayName = v.TryGetProperty("display_name", out var dnEl) ? dnEl.GetString() ?? variantId : variantId;
                variants.Add((variantId, displayName, v));
            }
        }

        if (variants.Count > 0)
        {
            GroupTabBar.Visibility = Visibility.Visible;
            foreach (var (variantId, displayName, _) in variants)
            {
                bool isSelected = variantId == selectedVariant;
                var btn = new Button
                {
                    Content = displayName,
                    FontWeight = isSelected
                        ? Microsoft.UI.Text.FontWeights.SemiBold
                        : Microsoft.UI.Text.FontWeights.Normal,
                };
                AutomationProperties.SetName(btn, displayName);

                if (onAction != null)
                {
                    string capturedVariantId = variantId;
                    btn.Click += (_, _) =>
                        onAction(ActionJson.VariantSelected(capturedVariantId));
                }

                GroupTabBar.Children.Add(btn);
            }
        }

        FieldsContainer.Children.Clear();
        var fieldsToShow = PreviewFields.Resolve(data);
        foreach (var (label, value) in fieldsToShow)
        {
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var labelBlock = new TextBlock
            {
                Text = label,
                Opacity = 0.6,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
            };
            var valueBlock = new TextBlock
            {
                Text = value,
                VerticalAlignment = VerticalAlignment.Center,
            };

            Grid.SetColumn(labelBlock, 0);
            Grid.SetColumn(valueBlock, 1);
            row.Children.Add(labelBlock);
            row.Children.Add(valueBlock);
            FieldsContainer.Children.Add(row);
        }

        if (data.TryGetProperty("a11y", out var a11yElem))
        {
            if (a11yElem.TryGetProperty("label", out var labelElem))
            {
                var a11yLabel = labelElem.GetString();
                if (!string.IsNullOrEmpty(a11yLabel))
                    AutomationProperties.SetName(this, a11yLabel);
            }
            if (a11yElem.TryGetProperty("hint", out var hintElem))
            {
                var hint = hintElem.GetString();
                if (!string.IsNullOrEmpty(hint))
                    AutomationProperties.SetHelpText(this, hint);
            }
        }
    }

    private async void LoadAvatarAsync(byte[] imageBytes)
    {
        var bitmapImage = new BitmapImage();
        using var stream = new InMemoryRandomAccessStream();
        await stream.WriteAsync(imageBytes.AsBuffer());
        stream.Seek(0);
        await bitmapImage.SetSourceAsync(stream);
        AvatarBrush.ImageSource = bitmapImage;
    }

    private static byte[] ParseByteArray(JsonElement arrayEl)
    {
        var result = new byte[arrayEl.GetArrayLength()];
        int i = 0;
        foreach (var el in arrayEl.EnumerateArray())
            result[i++] = (byte)el.GetInt32();
        return result;
    }
}
