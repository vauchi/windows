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

public sealed partial class CardPreviewComponent : UserControl, IRenderable
{
    public CardPreviewComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data, Action<string>? onAction)
    {
        NameHeader.Text = data.TryGetProperty("name", out var nameEl)
            ? nameEl.GetString() ?? ""
            : "";

        // Render avatar image if present
        AvatarArea.Visibility = Visibility.Collapsed;
        if (data.TryGetProperty("avatar_data", out var avatarEl)
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

        string? selectedGroup = data.TryGetProperty("selected_group", out var sgEl)
            ? sgEl.ValueKind == JsonValueKind.String ? sgEl.GetString() : null
            : null;

        // Build group tab buttons if present
        var groupViews = new List<(string GroupName, string DisplayName, JsonElement Element)>();
        if (data.TryGetProperty("group_views", out var groupViewsEl))
        {
            foreach (var gv in groupViewsEl.EnumerateArray())
            {
                string groupName = gv.TryGetProperty("group_name", out var gnEl) ? gnEl.GetString() ?? "" : "";
                string displayName = gv.TryGetProperty("display_name", out var dnEl) ? dnEl.GetString() ?? groupName : groupName;
                groupViews.Add((groupName, displayName, gv));
            }
        }

        if (groupViews.Count > 0)
        {
            GroupTabBar.Visibility = Visibility.Visible;
            foreach (var (groupName, displayName, _) in groupViews)
            {
                bool isSelected = groupName == selectedGroup;
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
                    string capturedGroup = groupName;
                    btn.Click += (_, _) =>
                        onAction(ActionJson.GroupViewSelected(capturedGroup));
                }

                GroupTabBar.Children.Add(btn);
            }
        }

        // Render fields: use selected group's visible_fields if available, otherwise all fields
        FieldsContainer.Children.Clear();
        var fieldsToShow = ResolveFields(data, groupViews, selectedGroup);
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

    private static List<(string Label, string Value)> ResolveFields(
        JsonElement data,
        List<(string GroupName, string DisplayName, JsonElement Element)> groupViews,
        string? selectedGroup)
    {
        var result = new List<(string, string)>();

        // Try to find the selected group view and use its visible_fields
        if (selectedGroup != null)
        {
            foreach (var (groupName, _, gv) in groupViews)
            {
                if (groupName != selectedGroup)
                    continue;

                if (!gv.TryGetProperty("visible_fields", out var visFields))
                    break;

                // visible_fields is a list of field ids — match against data["fields"]
                var fieldIds = new HashSet<string>();
                foreach (var fId in visFields.EnumerateArray())
                {
                    string? id = fId.GetString();
                    if (id != null) fieldIds.Add(id);
                }

                if (data.TryGetProperty("fields", out var allFields))
                {
                    foreach (var field in allFields.EnumerateArray())
                    {
                        string fId = field.TryGetProperty("id", out var fIdEl) ? fIdEl.GetString() ?? "" : "";
                        if (fieldIds.Contains(fId))
                        {
                            string label = field.TryGetProperty("label", out var lblEl) ? lblEl.GetString() ?? "" : "";
                            string value = field.TryGetProperty("value", out var valEl) ? valEl.GetString() ?? "" : "";
                            result.Add((label, value));
                        }
                    }
                }
                return result;
            }
        }

        // Fall back to all fields
        if (data.TryGetProperty("fields", out var fields))
        {
            foreach (var field in fields.EnumerateArray())
            {
                string label = field.TryGetProperty("label", out var lblEl) ? lblEl.GetString() ?? "" : "";
                string value = field.TryGetProperty("value", out var valEl) ? valEl.GetString() ?? "" : "";
                result.Add((label, value));
            }
        }

        return result;
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
